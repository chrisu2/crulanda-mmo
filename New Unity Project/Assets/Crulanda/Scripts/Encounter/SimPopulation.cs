using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The sims in the world you are in (Phase 5.2 round 1): loads the roster (SimRoster, the world slot), and keeps a figure
    /// (SimFigure) for each sim whose zone is this one and who is online by the world clock; one who logs off walks away in
    /// the mind only: the figure goes. Round 1 behaviour is standing about the zone's named places and wandering between
    /// them (the mocap idles when standing); questing, gathering, travelling and chat come in 5.3-5.5. Where each one was is
    /// written back to the profile and saved with the character's autosave (Persist).
    /// </summary>
    public sealed class SimPopulation : MonoBehaviour
    {
        public static SimPopulation Active { get; private set; }
        public EncounterSession Session { get; private set; }
        public WorldSave World { get; private set; }
        public string Root { get; private set; }
        public readonly List<SimFigure> Figures = new List<SimFigure>();
        float nextCheck;

        /// <summary>Whether sims live their day (hunt, gather, the inn: Phase 5.3a) or only stand about. Off in editor test runs and
        /// the HUD captures, so no sim tags a mob a test or a capture is fighting; a test of their life sets <see cref="LifeOverride"/>.</summary>
        public bool Lively { get; private set; }
        public static bool? LifeOverride;
        public void Init(EncounterSession session, string root)
        {
            Session = session; Root = root; Active = this;
            bool testRun = Application.isEditor && (Application.isBatchMode || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-runTests") >= 0);
            Lively = LifeOverride ?? !(testRun || EncounterCapture.Requested && !LifeCapture.Requested);   // the life capture watches them live
            World = SimRoster.LoadOrCreate(root);
            gameObject.AddComponent<SimChatter>().Init(this);   // the sims talking (zone chat, playtest note 62)
            Refresh();
            RestoreParty(World.party);
        }
        /// <summary>
        /// The party you had (round 27: it travels with you and survives a save): each sim comes to this zone at your side and joins
        /// again without asking; one who is offline now has logged off since, and that is said. Then the list is the party as it stands.
        /// </summary>
        public void RestoreParty(IList<string> ids)
        {
            if (ids == null || ids.Count == 0 || Session == null || Session.Player == null) return;
            float hour = WorldClock.Hour; var back = new List<string>(); int k = 0;
            foreach (var id in new List<string>(ids))
            {
                var s = World.sims.Find(x => x.id == id); if (s == null || Session.InParty(id)) continue;
                if (!s.IsOnlineAt(hour)) { Session.Message(s.name + " has logged off since."); continue; }
                var at = Session.Player.transform.position - Session.Player.transform.forward * (2 + k) + Session.Player.transform.right * (k % 2 == 0 ? 1.5f : -1.5f); k++;
                s.zone = ZoneId; s.x = at.x; s.z = at.z; Refresh();
                if (Session.Invite(id, true)) back.Add(s.name);
            }
            if (back.Count > 0) Session.Message("Your party is with you: " + string.Join(", ", back) + ".");
            World.party.Clear(); foreach (var c in Session.PartySims) if (c != null) World.party.Add(c.sim.id);
        }
        void OnDestroy() { if (Active == this) Active = null; }
        public string ZoneId { get { return Session != null && Session.Zone != null ? Session.Zone.Zone.id : null; } }
        public SimFigure Find(string simId) { return Figures.Find(f => f != null && f.sim.id == simId); }
        public SimFigure FindByName(string name) { return Figures.Find(f => f != null && f.sim.name == name); }
        /// <summary>A sim standing in the world, as a fighter (its Actor) for the mobs' threat (EncounterSession.CombatActor).</summary>
        public Crulanda.Gameplay.Actor FighterActor(string simId) { var f = Find(simId); return f != null && !f.Hidden ? f.actor : null; }

        void Update()
        {
            if (Session == null || Session.Paused || Time.time < nextCheck) return;
            nextCheck = Time.time + 1; Refresh();
        }
        /// <summary>How well a zone's levels suit a sim: 0 inside its band, negative by how far out (SimRoster.Homes).</summary>
        public static int Suits(string zoneId, int level)
        {
            foreach (var h in SimRoster.Homes) if (h.zone == zoneId) return level < h.lo ? level - h.lo : level > h.hi ? h.hi - level : 0;
            return -3;
        }
        /// <summary>A sim steps over the zone line (SimFigure.Travel): in the other zone at its arrival point; its figure here goes.</summary>
        public void Depart(SimFigure f, ZoneExit exit)
        {
            if (f == null || exit == null) return;
            f.sim.zone = exit.to; f.sim.x = exit.arrive.x; f.sim.z = exit.arrive.y; f.sim.nextTravelHour = Mathf.Repeat(WorldClock.Hour + 1.5f, 24);
            Figures.Remove(f); Destroy(f.gameObject);
        }
        /// <summary>
        /// The unseen (5.3b, a first step of 5.4): a sim in another zone, online, now and then takes a road to a zone that suits its
        /// level (home weighted), arriving at that road's end; one arriving here is spawned there by <see cref="Refresh"/>.
        /// </summary>
        void TickAway(SimAdventurer s, float hour)
        {
            if (s.nextTravelHour < 0) { s.nextTravelHour = Mathf.Repeat(hour + 1 + Mathf.Abs(s.variant % 5) * .5f, 24); return; }
            if (!WorldClock.Between(s.nextTravelHour, Mathf.Repeat(s.nextTravelHour + .5f, 24))) return;
            s.nextTravelHour = Mathf.Repeat(hour + 1.5f + Mathf.Abs(s.variant % 4), 24);
            var zone = Session.Zone != null ? Session.Zone.FindZone(s.zone) : null; if (zone == null || zone.exits == null || zone.exits.Length == 0) return;
            if (Suits(s.zone, s.level) == 0 && (s.variant + Mathf.FloorToInt(hour)) % 3 != 0) return;   // content where it is, mostly
            ZoneExit best = null; int bestScore = int.MinValue;
            foreach (var e in zone.exits) { if (e == null) continue; int sc = Suits(e.to, s.level) + (e.to == s.homeZone ? 2 : 0); if (sc > bestScore) { best = e; bestScore = sc; } }
            if (best == null || bestScore < Suits(s.zone, s.level)) return;
            s.zone = best.to; s.x = best.arrive.x; s.z = best.arrive.y;
        }
        /// <summary>
        /// The unseen living on (5.4, 2026-10-07): each world-clock hour a sim in another zone and online hunts, gathers and trades
        /// in the rough, by its personality: the bold take experience as from a few kills of their level (a level when it adds
        /// up: no ding is heard from another zone), the careful gather their trade's first material and sell it now and then,
        /// coin buys the gear upgrades (SimEconomy), and the smiths forge what they can. Nothing happens while offline, and a
        /// sim here, seen, lives for real instead. Called from <see cref="Refresh"/>.
        /// </summary>
        public static void LiveAway(SimAdventurer s, float hour, ItemDatabase items, ProfessionDatabase db)
        {
            if (s.lastAwayHour < 0) { s.lastAwayHour = hour; return; }
            float hours = Mathf.Repeat(hour - s.lastAwayHour, 24); if (hours < 1) return;
            if (hours > 6) hours = 6;   // a long gap (a night away from the game) counts as a working evening, no more
            s.lastAwayHour = hour;
            var r = new Crulanda.Core.SeededRandom(s.variant * 131 + Mathf.FloorToInt(hour * 7));
            for (int h = 0; h < Mathf.FloorToInt(hours); h++)
            {
                if (r.NextFloat() < .35f + .5f * s.bold)
                {
                    int kills = 2 + r.NextInt(0, 4);
                    if (s.level < EncounterProgress.LevelCap)
                    {
                        s.experience = Mathf.Max(s.experience, EncounterProgress.XpForLevel(s.level)) + kills * EncounterProgress.KillXp(s.level, s.level, false) / 2;
                        while (s.level < EncounterProgress.LevelCap && s.experience >= EncounterProgress.XpForLevel(s.level + 1)) s.level++;
                    }
                }
                else
                {
                    string trade = SimEconomy.GatherTrade(s.classId); string mat = trade == "mining" ? "mat.copper_ore" : trade == "woodcutting" ? "mat.oak_log" : "mat.yarrow";
                    SimEconomy.Add(s, mat, 2 + r.NextInt(0, 4));
                }
                if (SimEconomy.GoodsCount(s) >= 8 && r.NextFloat() < .5f)
                {
                    var recipe = SimEconomy.Craftable(s, db, items);
                    if (recipe != null) SimEconomy.Craft(s, recipe, items); else SimEconomy.SellAll(s, items);
                    SimEconomy.Upgrade(s);
                }
            }
        }
        /// <summary>Figures for those here and online; none for those gone.</summary>
        public void Refresh()
        {
            string zone = ZoneId; if (zone == null || World == null) return;
            float hour = WorldClock.Hour;
            if (Lively) foreach (var s in World.sims) if (s.zone != zone && s.IsOnlineAt(hour)) { LiveAway(s, hour, Session.Items, Session.Professions?.Db); TickAway(s, hour); }
            foreach (var s in World.sims)
            {
                bool here = s.zone == zone && s.IsOnlineAt(hour) && !Session.InParty(s.id);   // one in your party is with you, not standing about
                var f = Find(s.id);
                if (here && f == null) { Figures.Add(SimFigure.Spawn(this, s)); SimChatter.Active?.Arrived(s); }
                else if (!here && f != null) { if (!s.IsOnlineAt(hour)) SimChatter.Active?.LoggedOff(s); Figures.Remove(f); Destroy(f.gameObject); }
            }
            Figures.RemoveAll(f => f == null);
        }
        /// <summary>Writes the figures' places back and saves the world slot.</summary>
        public void Persist()
        {
            foreach (var f in Figures) if (f != null) { f.sim.x = f.transform.position.x; f.sim.z = f.transform.position.z; }
            foreach (var c in Session.PartySims) if (c != null) { c.sim.x = c.transform.position.x; c.sim.z = c.transform.position.z; }
            World.party.Clear(); foreach (var c in Session.PartySims) if (c != null) World.party.Add(c.sim.id);   // the party, kept (round 27)
            try { SimRoster.Save(Root, World); } catch (System.Exception e) { Debug.LogWarning("World save failed: " + e.Message); }
        }
        /// <summary>A place to stand in this zone: near one of its named places (a landmark), on the NavMesh.</summary>
        public Vector3 Spot(SimAdventurer s, int pick)
        {
            var zb = Session.Zone; var marks = zb != null ? zb.Zone.landmarks : null;
            Vector2 at = marks != null && marks.Length > 0 ? marks[Mathf.Abs(s.variant * 7 + pick) % marks.Length].at : Vector2.zero;
            var r = new Crulanda.Core.SeededRandom(s.variant * 31 + pick);
            var p = new Vector3(at.x + (r.NextFloat() - .5f) * 10, 0, at.y + (r.NextFloat() - .5f) * 10);
            // Never in a doorway (a villager going home to bed must not find a sim stood on the step): 4 m clear of every door.
            if (zb != null)
                foreach (var d in zb.Doors)
                {
                    var off = new Vector3(p.x - d.position.x, 0, p.z - d.position.z);
                    if (off.sqrMagnitude < 16) p = new Vector3(d.position.x, 0, d.position.z) + (off.sqrMagnitude > .01f ? off.normalized : Vector3.right) * 4.5f;
                }
            p.y = zb != null ? zb.HeightAt(p.x, p.z) : 0;
            if (NavMesh.SamplePosition(p, out var hit, 8, NavMesh.AllAreas)) return hit.position;
            return p;
        }
    }
}
