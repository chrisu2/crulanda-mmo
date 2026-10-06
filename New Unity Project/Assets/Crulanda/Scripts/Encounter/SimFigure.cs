using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A sim in the world (Phase 5.2; its life Phase 5.3a, 2026-10-06): its class's look and gear, an Actor the mobs can fight, and
    /// a day of its own. When one thing is done it chooses the next by its personality and its state (<see cref="Choose"/>):
    /// hunting the camp mobs of its level (bold ones more), working a herb or ore node (cautious ones more), resting at the inn
    /// (when hurt, or now and then), or standing about the zone's named places (chatty ones more). It fights in its class's way
    /// (melee, or arrows, fire and thorns from range; healers mend themselves), falls when beaten and gets up after a while.
    /// A mob it hits first is its kill: you get nothing for it (EncounterEnemy.TappedBy).
    /// </summary>
    public sealed class SimFigure : MonoBehaviour
    {
        public enum Doing { Loiter, Hunt, Gather, Inn, Down }
        public SimAdventurer sim; public SimPopulation population; public ActorVisual visual; public Actor actor; NavMeshAgent agent;
        public Doing Activity { get; private set; } = Doing.Loiter;
        /// <summary>Inside the inn: not drawn, not targetable, no plate.</summary>
        public bool Hidden { get; private set; }
        public EncounterEnemy Quarry { get; private set; }
        public ZoneInteractable Node { get; private set; }
        public int Kills { get; private set; }
        float until, nextAct, nextHeal, stuckSince, workUntil, downSince; int pick, goal; Vector3 dest; Renderer[] renderers;
        EncounterSession Session { get { return population.Session; } }
        public bool Walking { get { return agent != null && agent.isOnNavMesh && !agent.isStopped && agent.remainingDistance > agent.stoppingDistance + .1f; } }
        public bool Melee { get { return sim.classId == "class.warrior" || sim.classId == "class.paladin"; } }
        public bool Healer { get { return sim.classId == "class.druid" || sim.classId == "class.paladin"; } }
        public string Doings { get { return Activity == Doing.Hunt && Quarry != null ? "fighting " + Quarry.actor.DisplayName : Activity == Doing.Gather ? "gathering" : Activity == Doing.Inn ? "at the inn" : Activity == Doing.Down ? "fallen" : "about"; } }

        public static SimFigure Spawn(SimPopulation pop, SimAdventurer s)
        {
            var go = new GameObject("Sim " + s.name); go.SetActive(false); go.transform.SetParent(pop.transform, false);
            var start = s.x != 0 || s.z != 0 ? new Vector3(s.x, 0, s.z) : pop.Spot(s, 0);
            if (s.x != 0 || s.z != 0) { start.y = pop.Session.Zone != null ? pop.Session.Zone.HeightAt(start.x, start.z) : 0; if (NavMesh.SamplePosition(start, out var hit, 6, NavMesh.AllAreas)) start = hit.position; }
            go.transform.position = start + Vector3.up;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var f = go.AddComponent<SimFigure>(); f.sim = s; f.population = pop;
            // A fighter: an Actor of its level with its sim id (the mobs' threat names it), health as a party sim's (SimCompanion).
            f.actor = go.AddComponent<Actor>(); f.actor.Initialize(pop.Session.content.player, s.name, s.level, new Crulanda.Core.EntityId(s.id)); go.AddComponent<Combatant>();
            f.agent = go.AddComponent<NavMeshAgent>(); f.agent.speed = 1.7f; f.agent.angularSpeed = 360; f.agent.acceleration = 8; f.agent.stoppingDistance = .5f; f.agent.radius = .3f; f.agent.height = 2; f.agent.baseOffset = 1;
            f.agent.avoidancePriority = 80;   // villagers (60) go first: a sim steps aside
            f.visual = ActorVisual.Attach(go, EncounterSession.LookForClass(s.classId), s.variant);
            go.SetActive(true);   // ActorVisual hides the placeholder capsule itself; the figure is built under "Body", so nothing else is touched here
            f.actor.Stats.SetBase(StatType.MaxHealth, SimCompanion.MaxHealthFor(s)); f.actor.Health.ApplyHealing(f.actor.Health.Pool.Max);
            SimGear.Dress(f.visual, s, pop.Session.Items);   // its own gear by its level and class (Phase 5.2 round 2)
            f.renderers = go.GetComponentsInChildren<Renderer>();
            f.until = Time.time + 2 + (s.variant % 7);
            return f;
        }

        // ---------- choosing ----------
        /// <summary>A camp mob of its level (two under to one over), alive, not already fought by someone else, nearest first, within reach of a walk.</summary>
        public EncounterEnemy FindPrey(float within = 160)
        {
            EncounterEnemy best = null; float bestD = within;
            foreach (var e in Session.Enemies)
            {
                if (e == null || !e.Camp || e.Game || !e.actor.IsAlive || e.Hidden || e.Engaged) continue;
                int lv = e.actor.Level; if (lv < sim.level - 2 || lv > sim.level + 1) continue;
                float d = Vector3.Distance(e.transform.position, transform.position); if (d < bestD) { best = e; bestD = d; }
            }
            return best;
        }
        /// <summary>A herb or ore node, ready, not being worked by another sim, nearest first.</summary>
        public ZoneInteractable FindNode(float within = 140)
        {
            var zb = Session.Zone; if (zb == null) return null;
            ZoneInteractable best = null; float bestD = within;
            foreach (var i in zb.Interactables)
            {
                if (i == null || i.node == null || Time.time < i.hiddenUntil || population.Figures.Exists(o => o != null && o != this && o.Node == i)) continue;
                float d = Vector3.Distance(i.position, transform.position); if (d < bestD) { best = i; bestD = d; }
            }
            return best;
        }
        ZoneDoor Inn { get { var zb = Session.Zone; return zb == null ? null : zb.Doors.Find(d => d != null && d.kind == "inn"); } }
        /// <summary>What next, by score: its personality, its health, what is near, and a little chance.</summary>
        public void Choose()
        {
            Quarry = null; Node = null; float health = actor.Health.Pool.Ratio;
            if (!population.Lively) { Begin(Doing.Loiter); return; }
            float Noise() { return Random.value * .3f; }
            var prey = health > .6f ? FindPrey() : null; var node = FindNode(); var inn = Inn;
            float hunt = prey != null ? .35f + .6f * sim.bold + Noise() : -1;
            float gather = node != null ? .3f + .5f * (1 - sim.bold) + Noise() : -1;
            float rest = inn != null ? (health < .6f ? 1.2f : .12f + .2f * sim.chatty) + Noise() : -1;
            float loiter = .25f + .45f * sim.chatty + Noise();
            float top = Mathf.Max(Mathf.Max(hunt, gather), Mathf.Max(rest, loiter));
            if (top == hunt) Begin(Doing.Hunt, prey: prey);
            else if (top == gather) Begin(Doing.Gather, node: node);
            else if (top == rest) Begin(Doing.Inn);
            else Begin(Doing.Loiter);
        }
        /// <summary>Starts an activity (tests and the choice).</summary>
        public void Begin(Doing what, EncounterEnemy prey = null, ZoneInteractable node = null)
        {
            Activity = what; Quarry = prey; Node = node; stuckSince = Time.time; goal = 0;
            switch (what)
            {
                case Doing.Hunt: goal = 2 + Mathf.Abs(sim.gearSeed) % 3; until = Time.time + 240; if (prey == null) Quarry = FindPrey(); if (Quarry == null) { Activity = Doing.Loiter; until = Time.time + 20; } break;
                case Doing.Gather: goal = 1 + Mathf.Abs(sim.gearSeed / 7) % 3; until = Time.time + 200; if (node == null) Node = FindNode(); if (Node == null) { Activity = Doing.Loiter; until = Time.time + 20; } else Go(Node.position); break;
                case Doing.Inn: { var inn = Inn; until = Time.time + 150; if (inn == null) { Activity = Doing.Loiter; until = Time.time + 20; } else Go(inn.position); break; }
                default: until = Time.time + 30 + (sim.variant % 5) * 6 + sim.chatty * 30; pick++; Go(population.Spot(sim, pick)); break;
            }
        }
        void Go(Vector3 to)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(to, out var hit, 4, NavMesh.AllAreas)) to = hit.position;
            dest = to; agent.isStopped = false; agent.speed = 1.9f; agent.SetDestination(to); stuckSince = Time.time;
        }

        // ---------- each frame ----------
        void Update()
        {
            if (population == null || Session == null || Session.Paused || agent == null) return;
            if (!actor.IsAlive) { Fallen(); return; }
            if (Activity == Doing.Down) { Activity = Doing.Loiter; until = Time.time; }
            switch (Activity)
            {
                case Doing.Hunt: Hunt(); break;
                case Doing.Gather: Gather(); break;
                case Doing.Inn: AtInn(); break;
                default: Loiter(); break;
            }
            if (!Session.InCombat && !InFight && Time.time >= nextHeal) { nextHeal = Time.time + 1; actor.Health.ApplyHealing(Mathf.Max(2, actor.Health.Pool.Max / 60)); }
        }
        public bool InFight { get { return Quarry != null && Quarry.actor.IsAlive && Quarry.Victim == actor; } }
        void Loiter()
        {
            if (Walking && Time.time - stuckSince > 25) agent.ResetPath();
            if (sim.friendly > .5f && !Walking)
            {
                var player = Session.Player;
                if (player != null)
                {
                    var to = player.transform.position - transform.position; to.y = 0;
                    if (to.sqrMagnitude < 9 && to.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 3);
                }
            }
            if (Time.time >= until) Choose();
        }
        void Hunt()
        {
            var q = Quarry;
            if (q == null || !q.actor.IsAlive)
            {
                if (q != null && !q.actor.IsAlive && q.TappedBy == sim.id) Kills++;
                if (q != null) goal--;
                Quarry = null;
                // Another, while it has the health and the will; then a rest.
                if (goal > 0 && actor.Health.Pool.Ratio > .45f && Time.time < until) Quarry = FindPrey(60);
                if (Quarry == null) { if (actor.Health.Pool.Ratio < .6f) Begin(Doing.Inn); else Choose(); }
                return;
            }
            // Someone else has it (you, or another sim): leave it to them.
            if (q.Engaged && q.Victim != actor && !(q.TappedBy == sim.id)) { Quarry = null; return; }
            if (actor.Health.Pool.Ratio < .3f && !Healer) { Quarry = null; goal = 0; return; }   // breaks off: the mob may follow (its leash)
            float reach = Melee ? 2.4f : 18, d = Vector3.Distance(transform.position, q.transform.position);
            if (Healer && actor.Health.Pool.Ratio < .5f && Time.time >= nextHeal)
            {
                nextHeal = Time.time + 8; nextAct = Mathf.Max(nextAct, Time.time + 1.2f);
                int healed = actor.GetComponent<Combatant>().Heal(Mathf.RoundToInt(12 + 5 * sim.level)); visual?.CastRelease();
                if (healed > 0) Session.FloatText(transform.position, "+" + healed, new Color(.3f, 1, .7f));
                return;
            }
            if (d > reach) { if (Time.time - stuckSince > .5f || !Walking) { Go(q.transform.position); agent.speed = Melee ? 4.6f : 3.2f; stuckSince = Time.time; } return; }
            agent.isStopped = true;
            var face = q.transform.position - transform.position; face.y = 0;
            if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8);
            if (Time.time < nextAct) return;
            nextAct = Time.time + (Melee ? 2.0f : 2.4f);
            int dmg = Mathf.RoundToInt((Melee ? 6 : 5) + (Melee ? 2.4f : 2.2f) * sim.level);
            if (Melee) visual?.Strike();
            else
            {
                visual?.CastRelease();
                var hand = visual != null ? visual.RightHandle : null; var from = hand != null ? hand.position : transform.position + Vector3.up * 1.3f;
                bool arrow = sim.classId == "class.ranger";
                var colour = sim.classId == "class.mage" ? new Color(1, .55f, .15f) : sim.classId == "class.druid" ? new Color(.45f, .85f, .3f) : new Color(.9f, .88f, .8f);
                Bolt.Fire(from, q.transform, colour, arrow ? .1f : .18f, arrow ? .22f : .28f, arrow);
            }
            q.Receive(dmg, actor);
            if (sim.classId == "class.warrior") q.threat.Add(actor.EntityId.Value, dmg * 1.5f);
        }
        void Gather()
        {
            var n = Node;
            if (n == null || Time.time >= until) { Choose(); return; }
            if (workUntil > 0)
            {
                if (Time.time < workUntil) return;
                workUntil = 0; visual.Pose = ActorPose.None; Session.SimGathered(n); goal--; Node = null;
                if (goal > 0) { Node = FindNode(80); if (Node != null) { Go(Node.position); return; } }
                Choose(); return;
            }
            if (Time.time < n.hiddenUntil) { Node = FindNode(80); if (Node != null) Go(Node.position); else Choose(); return; }   // someone took it first
            float d = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(n.position.x, 0, n.position.z));
            if (d > 1.8f) { if (!Walking && Time.time - stuckSince > 1) Go(n.position); if (Time.time - stuckSince > 40) Choose(); return; }
            agent.isStopped = true;
            var face = n.position - transform.position; face.y = 0; if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(face);
            visual.Pose = ActorPose.Gather; workUntil = Time.time + 4 + (sim.variant % 3);
        }
        void AtInn()
        {
            var inn = Inn; if (inn == null) { Choose(); return; }
            if (Hidden)
            {
                actor.Health.ApplyHealing(Mathf.Max(3, actor.Health.Pool.Max / 30));
                if (Time.time >= until) { Show(true); Choose(); }
                return;
            }
            float d = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(inn.position.x, 0, inn.position.z));
            if (d < 2.6f) { Show(false); until = Time.time + 60 + (sim.variant % 5) * 15; return; }
            if (!Walking && Time.time - stuckSince > 1) Go(inn.position);
            if (Time.time - stuckSince > 60) Choose();
        }
        /// <summary>Goes in (hidden: no body, no plate, no NavMesh) or comes out at the door.</summary>
        void Show(bool on)
        {
            Hidden = !on;
            foreach (var r in renderers) if (r != null && r.name != "Body") r.enabled = on;
            if (agent != null) agent.enabled = on;
        }
        void Fallen()
        {
            if (Activity != Doing.Down) { Activity = Doing.Down; downSince = Time.time; Quarry = null; if (agent.isOnNavMesh) agent.isStopped = true; visual.Pose = ActorPose.None; }
            // Up again a little later (the run back from a graveyard is Phase 5.3b), and off to rest.
            if (Time.time - downSince > 12) { actor.Health.Revive(Mathf.RoundToInt(actor.Health.Pool.Max * .35f)); Begin(Doing.Inn); }
        }
        void OnDisable() { if (visual != null && Activity == Doing.Gather) visual.Pose = ActorPose.None; }
    }
}
