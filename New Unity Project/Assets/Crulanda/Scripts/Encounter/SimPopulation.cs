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

        public void Init(EncounterSession session, string root)
        {
            Session = session; Root = root; Active = this;
            World = SimRoster.LoadOrCreate(root);
            Refresh();
        }
        void OnDestroy() { if (Active == this) Active = null; }
        public string ZoneId { get { return Session != null && Session.Zone != null ? Session.Zone.Zone.id : null; } }
        public SimFigure Find(string simId) { return Figures.Find(f => f != null && f.sim.id == simId); }
        public SimFigure FindByName(string name) { return Figures.Find(f => f != null && f.sim.name == name); }

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
                bool here = s.zone == zone && s.IsOnlineAt(hour);
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
            try { SimRoster.Save(Root, World); } catch (System.Exception e) { Debug.LogWarning("World save failed: " + e.Message); }
        }
        /// <summary>A place to stand in this zone: near one of its named places (a landmark), on the NavMesh.</summary>
        public Vector3 Spot(SimAdventurer s, int pick)
        {
            var zb = Session.Zone; var marks = zb != null ? zb.Zone.landmarks : null;
            Vector2 at = marks != null && marks.Length > 0 ? marks[Mathf.Abs(s.variant * 7 + pick) % marks.Length].at : Vector2.zero;
            var r = new Crulanda.Core.SeededRandom(s.variant * 31 + pick);
            var p = new Vector3(at.x + (r.NextFloat() - .5f) * 10, 0, at.y + (r.NextFloat() - .5f) * 10);
            p.y = zb != null ? zb.HeightAt(p.x, p.z) : 0;
            if (NavMesh.SamplePosition(p, out var hit, 8, NavMesh.AllAreas)) return hit.position;
            return p;
        }
    }

    /// <summary>A sim's figure in the zone: the class's look, standing about and wandering between the zone's places.</summary>
    public sealed class SimFigure : MonoBehaviour
    {
        public SimAdventurer sim; public SimPopulation population; public ActorVisual visual; NavMeshAgent agent;
        float nextMove, stuckSince; int pick;
        public bool Walking { get { return agent != null && agent.isOnNavMesh && !agent.isStopped && agent.remainingDistance > agent.stoppingDistance + .1f; } }

        public static SimFigure Spawn(SimPopulation pop, SimAdventurer s)
        {
            var go = new GameObject("Sim " + s.name); go.SetActive(false); go.transform.SetParent(pop.transform, false);
            var start = s.x != 0 || s.z != 0 ? new Vector3(s.x, 0, s.z) : pop.Spot(s, 0);
            if (s.x != 0 || s.z != 0) { start.y = pop.Session.Zone != null ? pop.Session.Zone.HeightAt(start.x, start.z) : 0; if (NavMesh.SamplePosition(start, out var hit, 6, NavMesh.AllAreas)) start = hit.position; }
            go.transform.position = start + Vector3.up;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var f = go.AddComponent<SimFigure>(); f.sim = s; f.population = pop;
            f.agent = go.AddComponent<NavMeshAgent>(); f.agent.speed = 1.7f; f.agent.angularSpeed = 360; f.agent.acceleration = 8; f.agent.stoppingDistance = .5f; f.agent.radius = .3f; f.agent.height = 2; f.agent.baseOffset = 1; f.agent.avoidancePriority = 55;
            f.visual = ActorVisual.Attach(go, EncounterSession.LookForClass(s.classId), s.variant);
            go.SetActive(true);
            foreach (var r in body.GetComponentsInChildren<Renderer>()) r.enabled = false;
            f.nextMove = Time.time + 4 + (s.variant % 7) * 2;
            return f;
        }
        void Update()
        {
            if (population == null || population.Session == null || population.Session.Paused || agent == null || !agent.isOnNavMesh) return;
            if (Time.time >= nextMove)
            {
                pick++; var to = population.Spot(sim, pick);
                agent.isStopped = false; agent.SetDestination(to);
                nextMove = Time.time + 14 + (sim.variant % 5) * 4 + (sim.chatty > .6f ? 6 : 0);
                stuckSince = Time.time;
            }
            if (Walking && Time.time - stuckSince > 25) { agent.ResetPath(); nextMove = Time.time + 2; }   // a path that never ends: give it up
            // Facing the player when they stand close (friendly ones): a nod of attention, nothing more yet.
            if (sim.friendly > .5f && !Walking)
            {
                var player = population.Session.Player; if (player == null) return;
                var to = player.transform.position - transform.position; to.y = 0;
                if (to.sqrMagnitude < 9 && to.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 3);
            }
        }
    }
}
