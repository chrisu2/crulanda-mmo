using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Camps released later (the Adit rework, Docs/ADIT_REWORK.md section 4, E1, E2 and E4; written 2026-10-15; GAME-ONLY). A camp whose data
    /// says what brings it (ZoneCamp.after) is not spawned with the others (SpawnCamps skips it); it comes when that happens, once a visit:
    /// - "kill:&lt;mob&gt;": that mob's death. EncounterEnemy.Boss.DeathCall calls <see cref="ReleaseAfter"/> first, and the camp stands in for the
    ///   kin search: Lusk's relief shift appears in the bowl a few seconds on (<see cref="ReleaseDelay"/>) and walks in behind you, with the
    ///   move's deathText as its line.
    /// - "loose:&lt;elite&gt;": that elite's call (EncounterEnemy.Elite.TickElite at the move's callAt: Hollowsong's choir at half, the Whisperer's
    ///   flock at two fifths), with the call's line; or a "loose" phase of its move naming the camp (BossPhase.camp, <see cref="Loose"/>: Sabel's
    ///   urn-risen at two thirds and a third). A "loose" phase naming a camp that is there already (Corvane's hound) sends its mobs in instead.
    /// - "event:&lt;id&gt;&lt;n&gt;": wave n of the zone's event of that id (ZoneEvent: the Wake), a timed sequence (<see cref="StartEvent"/>, <see cref="RunEvent"/>)
    ///   started by the singing door's opening (UseLiftGate) or, with its wing open (its after wings done this visit), by the player coming to its
    ///   anchor (<see cref="StartEvents"/>: a later visit with the door open from the start, or a Wake tried again). Its waves come in order at
    ///   their seconds; it is over when the last wave is down; with the whole party down the living waves go back into the dark and it can start
    ///   again (WeaverFell's pattern).
    /// - "event:powder": the code's own, raised by taking the keg (EncounterSession.Adit.cs, E4): Hobb and his sappers down the stair behind you.
    /// A released camp spawns as it would have at the start (SpawnCampMobs: the same ids, so deeds, quests and loot read the camp from them), from a
    /// seeded stream of its own, never respawns (RespawnSeconds 1e6) and is sent at the foe: answering the boss who brought it, dead or alive (its
    /// leash is then the boss's fight, where the party is), or as a band of its own anchored at the event (EncounterEnemy.Sent). Nothing here is
    /// saved: camps never are (every camp is back on the next visit, the bosses with them, and so are their releases).
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string WakeEvent = "wake";
        /// <summary>Seconds between a death (or the keg taken) and the camp it releases walking in. Tests shorten it.</summary>
        public static float ReleaseDelay = 3;
        /// <summary>How fast an event's clock runs (1: its waves at their written seconds; tests raise it).</summary>
        public static float EventSpeed = 1;
        /// <summary>How near its anchor the player must come to start an event whose wing is open (metres; <see cref="StartEvents"/>).</summary>
        public static float EventReach = 5;
        /// <summary>The camps released this visit (indices into the zone's camps): each comes once.</summary>
        readonly HashSet<int> released = new HashSet<int>();
        /// <summary>The events over this visit, by id; and the one running, or null.</summary>
        readonly HashSet<string> eventsDone = new HashSet<string>();
        EventRun running;
        Dictionary<string, List<int>> afterCamps; ZoneDefinition afterCampsOf;
        /// <summary>An event under way: its data, its clock (seconds in), the next wave to raise, and every camp released while it ran.</summary>
        sealed class EventRun { public ZoneEvent def; public float clock; public int next; public readonly HashSet<int> camps = new HashSet<int>(); }

        /// <summary>The id of the event running now, or null; how many of its waves have come; its clock in seconds.</summary>
        public string EventRunning { get { return running != null ? running.def.id : null; } }
        public int EventWave { get { return running != null ? running.next : 0; } }
        public float EventClock { get { return running != null ? running.clock : 0; } }
        /// <summary>Whether the event of that id is over this visit (its last wave down).</summary>
        public bool EventDone(string id) { return eventsDone.Contains(id); }
        /// <summary>Whether the camp (by index) has been released this visit.</summary>
        public bool Released(int camp) { return released.Contains(camp); }

        /// <summary>The camps whose after is <paramref name="key"/>, in the zone's order (none for a key no camp waits on), looked up once a zone.</summary>
        List<int> CampsAfter(string key)
        {
            var zone = Zone != null ? Zone.Zone : null;
            if (afterCamps == null || afterCampsOf != zone)
            {
                afterCampsOf = zone; afterCamps = new Dictionary<string, List<int>>();
                var camps = zone != null ? zone.camps : null;
                for (int c = 0; camps != null && c < camps.Length; c++)
                {
                    var cp = camps[c]; if (cp == null || string.IsNullOrEmpty(cp.after)) continue;
                    if (!afterCamps.TryGetValue(cp.after, out var list)) afterCamps[cp.after] = list = new List<int>();
                    list.Add(c);
                }
            }
            return afterCamps.TryGetValue(key ?? "", out var found) ? found : null;
        }
        int CampNamed(string name)
        {
            var camps = Zone != null ? Zone.Zone.camps : null; if (camps == null || string.IsNullOrEmpty(name)) return -1;
            for (int c = 0; c < camps.Length; c++) if (camps[c] != null && camps[c].name == name) return c;
            return -1;
        }
        /// <summary>A camp a "loose" phase of the caller's move names: the phase brings it at its own mark, not the call (Sabel's second urn at a third).</summary>
        bool ClaimedByPhase(EncounterEnemy caller, int c)
        {
            if (caller == null || caller.Move == null || caller.Move.phases == null) return false;
            string name = Zone.Zone.camps[c].name;
            foreach (var p in caller.Move.phases) if (p != null && p.kind == "loose" && p.camp == name) return true;
            return false;
        }

        /// <summary>
        /// Releases every camp not yet released whose after is <paramref name="key"/> ("kill:Gang-Boss Haddo Lusk", "loose:Brother Hollowsong",
        /// "event:wake2", "event:powder"), now or <paramref name="delay"/> seconds on: its mobs spawned where its data stands them and sent at
        /// <paramref name="foe"/>, answering <paramref name="caller"/> when there is one (the boss whose death or call it is), and
        /// <paramref name="line"/> said as they come. Returns how many camps are released (the caller then skips its own kin search).
        /// </summary>
        public int ReleaseAfter(string key, Actor foe, EncounterEnemy caller, string line = null, float delay = 0)
        {
            if (restoring || Zone == null || Zone.Zone.camps == null || string.IsNullOrEmpty(key)) return 0;
            var waiting = CampsAfter(key); if (waiting == null) return 0;
            var list = new List<int>();
            foreach (int c in waiting) { if (released.Contains(c) || ClaimedByPhase(caller, c)) continue; list.Add(c); }
            if (list.Count == 0) return 0;
            foreach (int c in list) { released.Add(c); if (running != null) running.camps.Add(c); }
            if (delay > 0) StartCoroutine(ReleaseLater(list, foe, caller, line, delay)); else ReleaseNow(list, foe, caller, line);
            return list.Count;
        }
        IEnumerator ReleaseLater(List<int> camps, Actor foe, EncounterEnemy caller, string line, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Zone != null) ReleaseNow(camps, foe, caller, line);
        }
        void ReleaseNow(List<int> camps, Actor foe, EncounterEnemy caller, string line)
        {
            if (foe == null || !foe.IsAlive) foe = LiveFoe();
            int n = 0; FightGroup band = null;
            foreach (int c in camps) n += ReleaseCamp(c, foe, caller, ref band);
            if (n > 0 && !string.IsNullOrEmpty(line)) Message(line);
        }
        /// <summary>
        /// One released camp: its mobs from a stream of their own (the zone's seed and the camp's number, so a release draws nothing from the
        /// camps' stream and moves nobody else), kept for the visit, and sent at the foe a beat apart: after the caller when there is one, else as
        /// one band (made here on the first, shared by the camps of one release: Hobb and his sappers). Returns how many stood.
        /// </summary>
        int ReleaseCamp(int c, Actor foe, EncounterEnemy caller, ref FightGroup band)
        {
            var camps = Zone.Zone.camps; if (c < 0 || c >= camps.Length || camps[c] == null) return 0;
            var rng = new System.Random(Zone.Zone.seed * 31 + 5 + 7919 * (c + 1));
            var mobs = SpawnCampMobs(c, rng, Zone.Zone.id.Replace("zone.", ""));
            for (int k = 0; k < mobs.Count; k++)
            {
                var e = mobs[k]; e.RespawnSeconds = 1e6f;   // once a visit (E2)
                if (foe == null) continue;
                float beat = SocialAggro.CallBeat + SocialAggro.CallStagger * k;
                if (caller != null) e.Answer(caller, foe, beat);
                else { if (band == null) band = new FightGroup { anchor = BandAnchor(foe) }; e.Sent(band, foe, beat); }
            }
            return mobs.Count;
        }
        /// <summary>Where a band with no caller leashes from: the running event's anchor while the foe holds it (within a mob's far leash), else where the foe stands.</summary>
        Vector3 BandAnchor(Actor foe)
        {
            if (running != null && !string.IsNullOrEmpty(running.def.anchor) && Zone.Anchors.TryGetValue(running.def.anchor, out var at) && Vector3.Distance(at, foe.transform.position) <= 40) return at;
            return foe.transform.position;
        }
        /// <summary>Whom a release is sent at: you, else a living party sim, else Mira; you when nobody stands (they then wait where they spawned).</summary>
        Actor LiveFoe()
        {
            if (Player != null && Player.IsAlive) return Player;
            foreach (var c in PartySims) if (c != null && c.actor != null && c.actor.IsAlive) return c.actor;
            if (Companion != null && Progress != null && Progress.recruited && Companion.actor != null && Companion.actor.IsAlive) return Companion.actor;
            return Player;
        }
        bool PartyDown()
        {
            if (Player != null && Player.IsAlive) return false;
            foreach (var c in PartySims) if (c != null && c.actor != null && c.actor.IsAlive) return false;
            if (Companion != null && Progress != null && Progress.recruited && Companion.actor != null && Companion.actor.IsAlive) return false;
            return true;
        }

        /// <summary>
        /// A boss's "loose" phase (BossPhase.camp; EncounterEnemy.Boss.RunPhase): the camp named is released if it waits on him (ZoneCamp.after),
        /// else its mobs that stand idle come in as if he had called them (Corvane's "Loose the hound!": if the Vent-Hound still lives it leaves
        /// the kennel and joins). Returns how many come.
        /// </summary>
        public int Loose(EncounterEnemy lord, string campName, Actor foe)
        {
            if (restoring || Zone == null || lord == null) return 0;
            int c = CampNamed(campName); if (c < 0) return 0;
            if (foe == null || !foe.IsAlive) foe = LiveFoe();
            var camp = Zone.Zone.camps[c];
            if (!string.IsNullOrEmpty(camp.after))
            {
                if (released.Contains(c)) return 0;
                released.Add(c); if (running != null) running.camps.Add(c);
                FightGroup band = null; return ReleaseCamp(c, foe, lord, ref band);
            }
            int n = 0;
            foreach (var e in Enemies) if (e != null && e != lord && e.CampIndex == c && e.CanAnswer) { e.Answer(lord, foe, SocialAggro.CallBeat + SocialAggro.CallStagger * n); n++; }
            return n;
        }

        // ---------- events (E1: the Wake) ----------
        /// <summary>On arrival (StartAdit): this visit's releases forgotten, and the zone's events watched (<see cref="WatchEvents"/>).</summary>
        void StartEvents()
        {
            released.Clear(); eventsDone.Clear(); running = null;
            if (Zone != null && Zone.Zone.events != null && Zone.Zone.events.Length > 0) StartCoroutine(WatchEvents());
        }
        /// <summary>
        /// An event not yet over starts when you come within <see cref="EventReach"/> of its anchor, alive, with its wing open (the wings its
        /// data says come before it done this visit): a later visit with the singing door open from the start, or a Wake that failed and is
        /// tried again by walking back onto the island. The first time, the door's opening starts it (UseLiftGate).
        /// </summary>
        IEnumerator WatchEvents()
        {
            var wait = new WaitForSeconds(.5f);
            while (true)
            {
                yield return wait;
                if (Zone == null || Player == null || !Player.IsAlive || running != null || Paused) continue;
                foreach (var ev in Zone.Zone.events)
                {
                    if (ev == null || eventsDone.Contains(ev.id) || string.IsNullOrEmpty(ev.anchor) || !Zone.Anchors.TryGetValue(ev.anchor, out var at)) continue;
                    if (ev.kind == "phase" || ev.kind == "escort") continue;   // (Hallow's Creek) a boss's phase or a gate's opening starts these, never a step onto the anchor
                    if (Vector3.Distance(Player.transform.position, at) > EventReach || !WingOpen(ev.wing)) continue;
                    StartEvent(ev.id); break;
                }
            }
        }
        /// <summary>Starts the zone's event of that id (ZoneEvent; the Wake). False when the zone has none, it is over this visit, or one runs already.</summary>
        public bool StartEvent(string id)
        {
            var ev = EventNamed(id); if (ev == null || running != null || eventsDone.Contains(id)) return false;
            running = new EventRun { def = ev };
            Message(EventLine(ev, "start"));
            StartCoroutine(RunEvent(running));
            return true;
        }
        ZoneEvent EventNamed(string id)
        {
            if (Zone == null || Zone.Zone.events == null || string.IsNullOrEmpty(id)) return null;
            foreach (var e in Zone.Zone.events) if (e != null && e.id == id) return e;
            return null;
        }
        /// <summary>The event's clock: each wave at its seconds in order, the end when the last wave is down, the fall when the whole party is.</summary>
        IEnumerator RunEvent(EventRun run)
        {
            var waves = run.def.waves ?? new ZoneWave[0];
            while (running == run)
            {
                yield return null;
                if (Paused || Zone == null) continue;
                if (PartyDown()) { FailEvent(run); yield break; }
                run.clock += Time.deltaTime * EventSpeed;
                while (run.next < waves.Length && run.clock >= waves[run.next].at) { RaiseWave(run, run.next); run.next++; }
                if (run.next >= waves.Length && !AnyAlive(run.camps)) { FinishEvent(run); yield break; }
            }
        }
        void RaiseWave(EventRun run, int n)
        {
            var wave = run.def.waves[n]; if (wave == null) return;
            if (!string.IsNullOrEmpty(wave.unless) && !MobStanding(wave.unless)) return;   // (Hallow's Creek, N8) its boss is down: the wave never comes
            ReleaseAfter("event:" + run.def.id + (n + 1), LiveFoe(), null, !string.IsNullOrEmpty(wave.text) ? wave.text : WaveLine(wave.camp), 0);
        }
        bool AnyAlive(HashSet<int> camps)
        {
            foreach (var e in Enemies) if (e != null && e.Camp && e.actor != null && e.actor.IsAlive && camps.Contains(e.CampIndex)) return true;
            return false;
        }
        void FinishEvent(EventRun run)
        {
            running = null; eventsDone.Add(run.def.id);
            Message(EventLine(run.def, "end"));
            EventOver(run.def.id);   // (Hallow's Creek) the Waking Hour's loop snaps: the Examiner stands stone (EncounterSession.Hallows.cs)
        }
        /// <summary>The whole party down: the living of its waves (and whatever they brought) go back into the dark, the fallen lie where they fell to be
        /// searched, and the event may start again (WeaverFell's pattern).</summary>
        void FailEvent(EventRun run)
        {
            foreach (var e in Enemies.ToArray())
            {
                if (e == null || !run.camps.Contains(e.CampIndex) || e.actor == null || !e.actor.IsAlive) continue;
                if (Target == e) { Target = null; AutoAttack = false; }
                Enemies.Remove(e); Destroy(e.gameObject);
            }
            foreach (int c in run.camps) released.Remove(c);
            running = null;
            Message(EventLine(run.def, "fail"));
        }
        /// <summary>A wing is open when the wings (or groups: one side of a fork is enough) its data names in after are done this visit.</summary>
        public bool WingOpen(string wingId)
        {
            var w = WingNamed(wingId); if (w == null || string.IsNullOrEmpty(w.after)) return true;
            foreach (var raw in w.after.Split(',')) { var id = raw.Trim(); if (id.Length > 0 && !AfterDone(id)) return false; }
            return true;
        }
        bool AfterDone(string id)
        {
            if (WingNamed(id) != null) return WingDone(id);
            bool any = false;
            foreach (var w in Zone.Zone.wings) if (w != null && w.group == id) { any = true; if (WingDone(w.id)) return true; }
            return !any;   // a name that is no wing and no group holds nothing up (AditDataTests says there are none)
        }
        ZoneWing WingNamed(string id)
        {
            if (Zone == null || Zone.Zone.wings == null || string.IsNullOrEmpty(id)) return null;
            foreach (var w in Zone.Zone.wings) if (w != null && w.id == id) return w;
            return null;
        }
        /// <summary>A wing's fights are done this visit: no live mob of a camp that is a step of its run (DungeonPlan.RequiredCamp: not a patrol, not
        /// optional, not one released later).</summary>
        public bool WingDone(string wingId)
        {
            var camps = Zone != null ? Zone.Zone.camps : null; if (camps == null) return true;
            foreach (var e in Enemies)
            {
                if (e == null || !e.Camp || e.actor == null || !e.actor.IsAlive || e.CampIndex < 0 || e.CampIndex >= camps.Length) continue;
                var cp = camps[e.CampIndex]; if (cp != null && cp.wing == wingId && DungeonPlan.RequiredCamp(cp)) return false;
            }
            return true;
        }
        /// <summary>What the chat says as an event starts, ends or falls (the Wake's own words; a plain line for any other).</summary>
        static string EventLine(ZoneEvent ev, string moment)
        {
            if (ev.id == WakeEvent)
                switch (moment)
                {
                    case "start": return "The door's three notes carry the length of the Gallery, and the lamp rings answer them one after another. Something in the dark has heard: the Wake is coming.";
                    case "end": return "The last note dies in the door. The north neck is quiet, and the way down the shaft stair is yours.";
                    default: return "The song falls silent with nobody standing to hear it. What was coming melts back into the dark; it will come again when you do.";
                }
            var hallows = HallowsEventLine(ev.id, moment); if (hallows != null) return hallows;
            string name = string.IsNullOrEmpty(ev.id) ? "the event" : ev.id;
            return moment == "start" ? name + " begins." : moment == "end" ? name + " is over." : name + " goes quiet; it will come again.";
        }
        /// <summary>The line a wave comes with when its data gives none (ZoneWave.text): the Wake's by camp name, else a plain one.</summary>
        string WaveLine(string campName)
        {
            switch (campName)
            {
                case "Wake porters": return "Down the trestle from the amber ring, lamps swinging: the Wake's porters. You see them coming.";
                case "Wake zealots": return "Up the ash steps from the red ring: zealots of the Cult, chanting the door's note back at it.";
                case "Wake crawlers": return "Out of the river's mouth under the grey ring: crawlers, more of them than you would like.";
                case "Brother Hollowsong": return "The singing door cracks from inside. Brother Hollowsong walks out of the north neck toward the stone, and the song comes with him.";
            }
            int c = CampNamed(campName); var camp = c >= 0 ? Zone.Zone.camps[c] : null;
            return camp == null ? campName + " come." : camp.count == 1 ? camp.mob + " comes." : camp.name + ": " + camp.count + " come.";
        }
    }
}
