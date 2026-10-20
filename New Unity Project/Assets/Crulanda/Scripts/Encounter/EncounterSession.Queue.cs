using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The dungeon queue, "Gather a Party" (Chris, 2026-10-15, verbatim: "the dungeons should include an instance que that can be done every 2
    /// days for loot"; GAME-ONLY, and no other game's names). The window (J, or /queue) lists the dungeons with their levels and how they stand
    /// with your lockouts; you pick one, or "any for my level", and the part you take (tank, healer or damage: by your class until you choose).
    /// After a few seconds of finding a group the queue gathers sims of your level from the roster round you by role (a tank, a healer, the
    /// rest damage; online or not: a queued sim is summoned), and takes the party to the dungeon's door (the road's end into it). The dungeon
    /// is fresh for the run: every boss stands, the ones locked for you too, and those drop nothing (Lockouts; the window says so before you
    /// go). A sim leads the run (SimCompanion.LeadDungeon): the tank, or the boldest when you tank. When the run is done (its scorecard) the
    /// queued sims leave the party a minute later, back where they were; leaving the dungeon, or "Leave the party" in the window, does the
    /// same at once. Sims who were in your party before you queued stay. The run's state lives across the dungeon's load (a static), not in
    /// the save: a game quit mid-run comes back with the queued sims as an ordinary party, those offline logged off.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The window (J, /queue).</summary>
        public bool QueueOpen { get; set; }
        /// <summary>"Finding a group" lasts this long (seconds; tests shorten it).</summary>
        public static float QueueSeconds = 6;
        /// <summary>A queued run's leader sets out this long after the party comes to the door (the caves and their paths stand by then).</summary>
        public static float QueueLeadAfter = 3;
        /// <summary>After the run's scorecard the queued sims stay this long (to loot), then leave.</summary>
        public const float QueueBreakUpAfter = 60;
        /// <summary>The sims a queue may bring: within this many levels of yours, as an invitation's.</summary>
        public const int QueueLevelGap = InviteLevelGap;
        static readonly string[] TankClasses = { "class.warrior", "class.paladin" };   // a Paladin tanks when no Warrior is along (SimCompanion.Role)
        static readonly string[] HealerClasses = { "class.druid", "class.cleric", "class.shaman" };
        static readonly string[] DamageClasses = { "class.ranger", "class.mage", "class.rogue", "class.archivist", "class.scoundrel", "class.pactblade" };   // never one whose part shifts with the party (a Paladin, a Salt-Mender)

        SimCompanion.PartyRole? queueRole;
        /// <summary>The part you take in a queued party: your choice in the window, else your class's (<see cref="RoleForClass"/>).</summary>
        public SimCompanion.PartyRole QueueRole { get { return queueRole ?? RoleForClass(ClassDef != null ? ClassDef.id : null); } set { queueRole = value; } }
        /// <summary>A class's part by default: a Warrior tanks, a Druid, Cleric, Shaman or Salt-Mender heals, everyone else (a Paladin too: how
        /// Chris plays his) deals damage.</summary>
        public static SimCompanion.PartyRole RoleForClass(string classId)
        {
            if (classId == "class.warrior") return SimCompanion.PartyRole.Tank;
            if (classId == "class.druid" || classId == "class.cleric" || classId == "class.shaman" || classId == "class.saltmender") return SimCompanion.PartyRole.Healer;
            return SimCompanion.PartyRole.Damage;
        }
        public static string RoleWord(SimCompanion.PartyRole r) { return r == SimCompanion.PartyRole.Tank ? "tank" : r == SimCompanion.PartyRole.Healer ? "healer" : "damage"; }

        /// <summary>The dungeon being queued for (its zone id) while the queue is finding a group; null otherwise.</summary>
        public string QueuedFor { get; private set; }
        float queueFormAt; int queueCount;
        /// <summary>Seconds left of finding a group.</summary>
        public float QueueWait { get { return QueuedFor == null ? 0 : Mathf.Max(0, queueFormAt - Time.time); } }

        /// <summary>A queued run: its dungeon, its leader, the sims the queue brought and where each came from. Kept over the dungeon's load.</summary>
        sealed class QueueRun
        {
            public string zone, leader; public bool led; public float arrivedAt = -1, doneAt = -1;
            public readonly List<string> sims = new List<string>();
            public readonly Dictionary<string, (string zone, float x, float z)> from = new Dictionary<string, (string, float, float)>();
        }
        static QueueRun queueRun;
        /// <summary>Forgets any queued run (tests, between scenes).</summary>
        public static void ForgetQueue() { queueRun = null; }
        /// <summary>In a queued run's dungeon now: it is fresh for the run (every boss stands; the locked drop nothing).</summary>
        public bool QueueFresh { get { return queueRun != null && Zone != null && queueRun.zone == Zone.Zone.id; } }
        /// <summary>The dungeon of the queued run under way (its zone id), or null.</summary>
        public string QueueRunZone { get { return queueRun != null ? queueRun.zone : null; } }
        /// <summary>A sim the queue brought (it leaves when the run ends; offline, it still comes).</summary>
        public bool QueueSim(string simId) { return queueRun != null && simId != null && queueRun.sims.Contains(simId); }
        /// <summary>The run's leader (a sim id), or null.</summary>
        public string QueueLeader { get { return queueRun != null ? queueRun.leader : null; } }

        // ---------- the dungeons ----------
        /// <summary>The dungeons the queue offers (every dungeon or raid zone), lowest first.</summary>
        public List<ZoneDefinition> QueueDungeons()
        {
            if (Zone == null) return new List<ZoneDefinition>();
            return Zone.AllZones().Where(z => z != null && (z.dungeon || z.raid)).OrderBy(z => z.levelMin).ThenBy(z => z.id, StringComparer.Ordinal).ToList();
        }
        /// <summary>A dungeon fits a level from a level under its band (the leader's own rule, SimCompanion.LeadDungeon) to three over it.</summary>
        public static bool QueueFits(ZoneDefinition d, int level) { return d != null && level >= d.levelMin - 1 && level <= d.levelMax + 3; }
        /// <summary>"Any for my level": the fitting dungeon whose band is nearest yours (null when none fits).</summary>
        public ZoneDefinition QueueDungeonFor(int level)
        {
            ZoneDefinition best = null; int bestGap = int.MaxValue;
            foreach (var d in QueueDungeons())
            {
                if (!QueueFits(d, level)) continue;
                int gap = level < d.levelMin ? d.levelMin - level : level > d.levelMax ? level - d.levelMax : 0;
                if (gap < bestGap) { best = d; bestGap = gap; }
            }
            return best;
        }
        /// <summary>A dungeon's door: where the road into it from another zone arrives (the Sealed Adit's yard), else its own start.</summary>
        public Vector2 QueueDoor(ZoneDefinition d)
        {
            if (Zone != null) foreach (var z in Zone.AllZones()) if (z != null && z.exits != null) foreach (var e in z.exits) if (e != null && e.to == d.id) return e.arrive;
            return d.spawns != null ? d.spawns.player : Vector2.zero;
        }
        /// <summary>A dungeon's bosses and elites (its camps' elites, the rare one aside) and how many are locked for you, with the longest
        /// time left: what the window shows ("3 of 20 locked: no loot from them for 1 d 4 h").</summary>
        public (int bosses, int locked, TimeSpan left) QueueLockState(ZoneDefinition d)
        {
            int bosses = 0, locked = 0; var left = TimeSpan.Zero; if (d == null || d.camps == null || Progress == null) return (0, 0, left);
            string zoneShort = d.id.Replace("zone.", ""); var now = Lockouts.UtcNow();
            for (int c = 0; c < d.camps.Length; c++)
            {
                var cp = d.camps[c]; if (cp == null || !cp.elite || cp.rare > 0) continue; bosses++;
                string key = "mob." + LootContext.CampTag(cp) + "." + zoneShort + "." + c;
                var l = Lockouts.Left(Progress, key, now); if (l > TimeSpan.Zero) { locked++; if (l > left) left = l; }
            }
            return (bosses, locked, left);
        }

        // ---------- queueing ----------
        /// <summary>
        /// /queue &lt;dungeon&gt; or the window's Queue: starts finding a group for that dungeon (its id, its name or a word of it; "any" or
        /// empty: the one for your level). Refused while fighting, while a run is under way or already queued, and for a dungeon outside your
        /// levels. True when the queue has started.
        /// </summary>
        public bool JoinQueue(string dungeon)
        {
            if (Zone == null || Player == null) { Message("There is no dungeon to queue for here."); return false; }
            if (InCombat) { Message("Not while you are fighting."); return false; }
            if (QueuedFor != null) { Message("You are already queued for " + ZoneName(QueuedFor) + "."); return false; }
            if (queueRun != null) { Message("You are in a queued run of " + ZoneName(queueRun.zone) + ": leave its party first (the queue window, J)."); return false; }
            int level = Progress.Level; ZoneDefinition d;
            string want = (dungeon ?? "").Trim();
            if (want.Length == 0 || string.Equals(want, "any", StringComparison.OrdinalIgnoreCase))
            {
                d = QueueDungeonFor(level);
                if (d == null) { var all = QueueDungeons(); Message("No dungeon fits level " + level + (all.Count > 0 ? " (" + string.Join(", ", all.Select(x => x.displayName + " " + x.levelMin + "-" + x.levelMax)) + ")." : ".")); return false; }
            }
            else
            {
                d = QueueDungeons().Find(z => z.id == want || string.Equals(z.displayName, want, StringComparison.OrdinalIgnoreCase) || z.displayName.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0);
                if (d == null) { Message("No dungeon called " + want + ". /queue alone takes the one for your level."); return false; }
                if (!QueueFits(d, level)) { Message(d.displayName + " is for levels " + d.levelMin + "-" + d.levelMax + ": not for level " + level + " yet."); return false; }
            }
            QueuedFor = d.id; queueFormAt = Time.time + QueueSeconds; queueCount++;
            var st = QueueLockState(d);
            Message("Finding a group for " + d.displayName + " (levels " + d.levelMin + "-" + d.levelMax + ") as " + RoleWord(QueueRole) + "..." +
                (st.locked > 0 ? " " + st.locked + " of its " + st.bosses + " bosses and elites are locked for you: they will drop nothing this run." : ""));
            return true;
        }
        /// <summary>Stops finding a group.</summary>
        public void LeaveQueue() { if (QueuedFor == null) return; Message("You leave the queue for " + ZoneName(QueuedFor) + "."); QueuedFor = null; }

        /// <summary>Whether a sim can take a place: not with you already, not on its way, not a rival, within the queue's level gap.</summary>
        bool QueueCandidate(SimAdventurer s, int level)
        {
            return s != null && !InParty(s.id) && !coming.Contains(s.id) && !SimMemory.Refuses(s) && Mathf.Abs(s.level - level) <= QueueLevelGap;
        }
        /// <summary>The best sim of these classes for the run: nearest the dungeon's levels (at your level, within its band), online before
        /// offline, then by a turn of the roster each queue so the same few are not always picked.</summary>
        SimAdventurer QueuePick(List<SimAdventurer> pool, string[] classes, int want, HashSet<string> taken)
        {
            SimAdventurer best = null; int bestScore = int.MaxValue; float hour = WorldClock.Hour;
            foreach (var s in pool)
            {
                if (taken.Contains(s.id) || Array.IndexOf(classes, s.classId) < 0) continue;
                int score = Mathf.Abs(s.level - want) * 1000 + (s.IsOnlineAt(hour) ? 0 : 500) + Mathf.Abs(s.variant * 7 + queueCount * 31) % 97;
                if (score < bestScore) { best = s; bestScore = score; }
            }
            return best;
        }
        /// <summary>
        /// The group is found: the party filled round you and the sims with you already (a tank if none, a healer if none, damage for the
        /// rest, up to MaxPartySims), each summoned to your side and joined; then the party is taken to the dungeon's door. False (and the
        /// queue given up, with a word why) when no tank or healer of your level can be had, or the road cannot be taken now.
        /// </summary>
        bool FormQueueParty(string dungeonId)
        {
            var pop = SimPopulation.Active; var d = Zone != null ? Zone.FindZone(dungeonId) : null;
            if (pop == null || d == null) { Message("No group could be found."); return false; }
            if (InCombat) { Message("The group waits while you fight: queue again when you are clear."); return false; }
            int level = Progress.Level, want = Mathf.Clamp(level, d.levelMin, d.levelMax);
            bool haveTank = QueueRole == SimCompanion.PartyRole.Tank, haveHealer = QueueRole == SimCompanion.PartyRole.Healer;
            foreach (var c in PartySims) if (c != null) { haveTank |= c.Role == SimCompanion.PartyRole.Tank; haveHealer |= c.Role == SimCompanion.PartyRole.Healer; }
            int places = MaxPartySims - PartySims.Count;
            var pool = pop.World.sims.FindAll(s => QueueCandidate(s, level)); var taken = new HashSet<string>(); var picks = new List<SimAdventurer>();
            if (!haveTank && places > 0) { var t = QueuePick(pool, TankClasses, want, taken); if (t == null) { Message("No tank of your level could be found for " + d.displayName + ". Try again later."); return false; } picks.Add(t); taken.Add(t.id); places--; }
            if (!haveHealer && places > 0) { var h = QueuePick(pool, HealerClasses, want, taken); if (h == null) { Message("No healer of your level could be found for " + d.displayName + ". Try again later."); return false; } picks.Add(h); taken.Add(h.id); places--; }
            while (places > 0) { var x = QueuePick(pool, DamageClasses, want, taken); if (x == null) break; picks.Add(x); taken.Add(x.id); places--; }

            var run = new QueueRun { zone = d.id }; queueRun = run;
            var me = Player.transform; int k = 0;
            foreach (var s in picks)
            {
                run.from[s.id] = (s.zone, s.x, s.z); run.sims.Add(s.id);
                var at = me.position - me.forward * (2 + k) + me.right * (k % 2 == 0 ? 1.5f : -1.5f); k++;
                s.zone = ZoneId; s.x = at.x; s.z = at.z;
            }
            pop.Refresh();   // the summoned stand by you (SimPopulation counts a queued sim as here, online or not)
            foreach (var s in picks) if (!Invite(s.id, true)) { run.sims.Remove(s.id); SendQueueSimHome(s); }
            var lead = PartySims.Find(c => c != null && c.Role == SimCompanion.PartyRole.Tank) ?? PartySims.Find(c => c != null && c.sim.bold > .5f) ?? PartySims.Find(c => c != null);
            run.leader = lead != null ? lead.sim.id : null;
            Message("A group is found for " + d.displayName + ": " + string.Join(", ", PartySims.Where(c => c != null).Select(c => c.sim.name + " (" + RoleWord(c.Role) + ")")) + ", and you (" + RoleWord(QueueRole) + ").");
            if (lead != null) ChatSay(ChatChannel.Party, lead.sim.name, lead.sim.chatty > .65f ? "hi all, " + d.displayName + " then. i'll lead" : "Well met. " + d.displayName + ": I'll lead the way.");
            if (!TravelTo(new ZoneExit { to = d.id, name = "the dungeon queue", arrive = QueueDoor(d) })) { DisbandQueue("The party cannot set out now: it breaks up."); return false; }
            return true;
        }
        /// <summary>On a zone's load (Start): a queued run comes to its dungeon's door (the leader sets out a moment later); a load anywhere
        /// else ends the run (its sims were sent home by SimPopulation.RestoreParty).</summary>
        void ArriveQueued()
        {
            if (queueRun == null) return;
            if (!QueueFresh) { queueRun = null; return; }
            if (queueRun.arrivedAt < 0) { queueRun.arrivedAt = Time.time; Message("Your group stands at the door of " + Zone.Zone.displayName + ". It is fresh for the run."); }
        }
        /// <summary>The queue's clock: the group found when finding is done; the leader set out at the door; the queued sims gone a minute after
        /// the run is done, or at once when none of them is left with you.</summary>
        void TickQueue()
        {
            if (QueuedFor != null && Time.time >= queueFormAt) { var id = QueuedFor; QueuedFor = null; FormQueueParty(id); }
            if (queueRun == null || !QueueFresh || queueRun.arrivedAt < 0) return;
            if (!queueRun.led && Time.time >= queueRun.arrivedAt + QueueLeadAfter) { queueRun.led = true; LeadQueued(); }
            if (queueRun.doneAt >= 0 && Time.time >= queueRun.doneAt) { DisbandQueue("The run is over: the queued party breaks up."); return; }
            if (queueRun.led && !queueRun.sims.Exists(id => InParty(id))) { queueRun = null; }   // every queued sim has gone (kicked, or left)
        }
        /// <summary>The leader sets out: the run's tank (or the boldest when you tank), through the whole dungeon (the standard run).</summary>
        void LeadQueued()
        {
            var c = PartySim(queueRun.leader) ?? PartySims.Find(x => x != null && x.sim.bold > .5f) ?? PartySims.Find(x => x != null); if (c == null) return;
            queueRun.leader = c.sim.id;
            foreach (var p in PartySims) if (p != null && p != c) p.StopLeading();
            if (c.LeadDungeon(null, out var why) == null) { Message(c.sim.name + ": " + why); return; }
            Message(c.sim.name + " leads the way into " + c.Dungeon + " (" + c.CampsLeft + " camps).");
        }
        /// <summary>The run's scorecard came up (the last boss down, or the leader calling it cleared): the queued sims leave in a minute.</summary>
        void QueueRunDone()
        {
            if (queueRun == null || !QueueFresh || queueRun.doneAt >= 0) return;
            queueRun.doneAt = Time.time + QueueBreakUpAfter;
            Message("The run is done. The queued party breaks up in a minute: loot what is yours.");
        }
        /// <summary>The queued sims leave the party (a word of thanks each) and go back where they came from; the run is over. Sims who were
        /// with you before the queue stay.</summary>
        public void DisbandQueue(string line = null)
        {
            var run = queueRun; if (run == null) return; queueRun = null;
            var pop = SimPopulation.Active;
            foreach (var id in run.sims)
            {
                var c = PartySim(id);
                if (c != null) { ChatSay(ChatChannel.Party, c.sim.name, c.sim.chatty > .65f ? "ty for the run, gl" : "Good run. Fare well."); LeaveParty(id); }
                var s = pop != null ? pop.World.sims.Find(x => x.id == id) : null;
                if (s != null && run.from.TryGetValue(id, out var was)) { s.zone = was.zone; s.x = was.x; s.z = was.z; }
            }
            if (pop != null) { pop.Refresh(); pop.Persist(); }
            if (!string.IsNullOrEmpty(line)) Message(line);
        }
        /// <summary>A queued sim back where it came from (a load outside the run's dungeon: SimPopulation.RestoreParty leaves it out).</summary>
        public void SendQueueSimHome(SimAdventurer s)
        {
            if (s == null || queueRun == null) return;
            if (queueRun.from.TryGetValue(s.id, out var was)) { s.zone = was.zone; s.x = was.x; s.z = was.z; }
            queueRun.sims.Remove(s.id);
        }
    }
}
