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
            Refresh();
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
        /// <summary>Figures for those here and online; none for those gone.</summary>
        public void Refresh()
        {
            string zone = ZoneId; if (zone == null || World == null) return;
            float hour = WorldClock.Hour;
            foreach (var s in World.sims)
            {
                bool here = s.zone == zone && s.IsOnlineAt(hour) && !Session.InParty(s.id);   // one in your party is with you, not standing about
                var f = Find(s.id);
                if (here && f == null) Figures.Add(SimFigure.Spawn(this, s));
                else if (!here && f != null) { Figures.Remove(f); Destroy(f.gameObject); }
            }
            Figures.RemoveAll(f => f == null);
        }
        /// <summary>Writes the figures' places back and saves the world slot.</summary>
        public void Persist()
        {
            foreach (var f in Figures) if (f != null) { f.sim.x = f.transform.position.x; f.sim.z = f.transform.position.z; }
            foreach (var c in Session.PartySims) if (c != null) { c.sim.x = c.transform.position.x; c.sim.z = c.transform.position.z; }
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
