using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    /// <summary>Whoever keeps a pet (the Ranger's wolf; the Mage's ember hound, the Cinderwright's, 2026-10-11): how hardy and how fierce
    /// their talents make it, and what its bites do for them.</summary>
    public interface IPetMaster
    {
        float PetHealthScale { get; }
        float PetBiteScale { get; }
        /// <summary>Its bites hold attention like a tank's blows (the wolf's Howl).</summary>
        bool PetHolds { get; }
        void OnPetBite(EncounterEnemy target, int bite);
    }

    /// <summary>A party sim Ranger as its wolf's keeper: a plain wolf, no talents.</summary>
    public sealed class SimPetMaster : IPetMaster
    {
        public float PetHealthScale { get { return 1; } }
        public float PetBiteScale { get { return 1; } }
        public bool PetHolds { get { return false; } }
        public void OnPetBite(EncounterEnemy target, int bite) { }
    }

    /// <summary>
    /// The Ranger's wolf (Phase 5.1b): a grey wolf on the pack's model (ActorLook.Wolf) that keeps to heel like Mira does, hunts
    /// joins whatever the Ranger fights (note 90), goes where the Ranger sets it (Sic; with Pack Sense whatever the Ranger shoots, the first bite harder), bites every two seconds with threat of its
    /// own, is a party member the enemies can turn on (EncounterSession.PartyActor) and falls in its own death. Its health and
    /// bite follow the Ranger's level and talents (Thick Coat, Sharp Teeth, Alpha, Howl). Not saved: Call Companion whistles it
    /// up each session.
    /// </summary>
    public sealed class RangerPet : MonoBehaviour
    {
        public Actor actor;
        public EncounterSession session;
        public IPetMaster kit;
        /// <summary>What it is called in what you are told ("wolf", "hound").</summary>
        public string Kind = "wolf";
        /// <summary>Whose it is: you (null) or a party sim's (a sim Ranger's wolf, 2026-10-11, Chris: "need a paladin, ranger pet, etc"): it
        /// heels to that sim and joins whatever that sim fights.</summary>
        public SimCompanion Master;
        Transform Heel { get { return Master != null ? Master.transform : session.Player.transform; } }
        NavMeshAgent agent; ActorVisual visual;
        float nextBite, nextCatchUp, fury = 1; int baseHealth;
        /// <summary>What it is hunting (null at heel).</summary>
        public EncounterEnemy Quarry { get; private set; }
        /// <summary>Its standing order (playtest note 93, the companion bar): Assist (at heel, joining every fight) or Stay (holding its
        /// ground, biting only what it is sent at or what bites it).</summary>
        public enum Order { Assist, Stay }
        public Order Mode { get; private set; } = Order.Assist;
        Vector3 stayAt;
        public void Command(Order o) { Mode = o; if (o == Order.Stay) { stayAt = transform.position; Quarry = null; fury = 1; } }
        public const float BiteInterval = 2, BiteReach = 2.2f;
        /// <summary>A sim's pet that has hunted this long without a bite is put beside its quarry (SimCompanion.Damage counts its bites in
        /// its keeper's damage, so a pet that never arrives leaves its keeper under its band).</summary>
        public const float NoBiteWarp = 5;
        float huntSince = -1, lastBiteAt = -1;
        /// <summary>Its agent's stopping distance: about its own width and its quarry's, well inside BiteReach.</summary>
        public const float HuntStop = .9f;
        /// <summary>Its agent's avoidance priority: less important than the party's and the mobs' (50), so it gives way and they are not shoved.</summary>
        public const int PetAvoidance = 70;
        /// <summary>Where a warped pet may stand: on a ring round its quarry, no nearer than this to anyone of the party (or you).</summary>
        public const float WarpRingNear = 1.2f, WarpRingFar = 1.6f, WarpClear = 1.1f;
        const int WarpAngles = 12;

        /// <summary>
        /// The spot a stuck pet is put on (2026-10-18): one of 12 angles at 1.2, 1.4 or 1.6 m round its quarry, on the floor
        /// (<paramref name="floor"/>: the navmesh point there, or null), none within WarpClear of <paramref name="others"/> (the party, you,
        /// the other pets), the far side from them first. The first fix (2026-10-17) put it 1.4 m off on the side it came from, which in
        /// SimDummyTests was about 1 m from the Scoundrel at the dummy: the two shoved each other and the Scoundrel was pushed out of reach.
        /// </summary>
        public static bool WarpSpot(Vector3 quarry, IList<Vector3> others, Func<Vector3, Vector3?> floor, out Vector3 spot)
        {
            spot = quarry;
            // Away from the party: from the middle of them to the quarry.
            var away = Vector3.zero; int n = 0;
            if (others != null) foreach (var o in others) { var d = quarry - o; d.y = 0; if (d.sqrMagnitude > .0001f) { away += d.normalized; n++; } }
            if (n > 0) away.Normalize();
            float best = float.NegativeInfinity; bool found = false;
            for (int i = 0; i < WarpAngles; i++)
            {
                var dir = Quaternion.Euler(0, i * 360f / WarpAngles, 0) * Vector3.forward;
                for (float r = WarpRingNear; r <= WarpRingFar + .001f; r += .2f)
                {
                    var on = floor(quarry + dir * r); if (!on.HasValue) continue;
                    var at = on.Value; bool crowded = false; float nearest = float.PositiveInfinity;
                    if (others != null) foreach (var o in others)
                    {
                        var d = at - o; d.y = 0; float m = d.magnitude;
                        if (m < WarpClear) { crowded = true; break; }
                        nearest = Mathf.Min(nearest, m);
                    }
                    if (crowded) continue;
                    // The far side first, then room from the nearest, then the nearer ring (a bite's reach is measured from the middle).
                    float score = Vector3.Dot(dir, away) * 10 + Mathf.Min(nearest, 3) - r * .5f;
                    if (score > best) { best = score; spot = at; found = true; }
                }
            }
            return found;
        }
        /// <summary>Everyone the warped pet keeps clear of: you, the party sims, Mira and the other pets.</summary>
        List<Vector3> Crowd()
        {
            var list = new List<Vector3>();
            if (session.Player != null) list.Add(session.Player.transform.position);
            if (session.Companion != null) list.Add(session.Companion.transform.position);
            foreach (var c in session.PartySims) if (c != null && c.actor != null && c.actor.IsAlive) list.Add(c.transform.position);
            foreach (var p in session.SimPets) if (p != null && p != this && p.actor != null && p.actor.IsAlive) list.Add(p.transform.position);
            if (session.Pet != null && session.Pet != this && session.Pet.actor != null && session.Pet.actor.IsAlive) list.Add(session.Pet.transform.position);
            return list;
        }

        public void Init(Actor a, EncounterSession s, IPetMaster k)
        {
            actor = a; session = s; kit = k; agent = GetComponent<NavMeshAgent>(); visual = GetComponent<ActorVisual>();
            baseHealth = 90 + 18 * Mathf.Max(1, session.Progress.Level);
            // Up close, not 1.9 m off (AddAgent's stop for its .5 m width): the agent stopped where the path ends, and where the quarry's
            // ground is cut away round it (a dummy's post, a boss's dais) that end is short of it, and it stood 2.3 m off its 2.2 m bite for
            // good (2026-10-18, a sim Bonecaller's thrall that never bit the dummy). It still halts at BiteReach (isStopped).
            if (agent != null) agent.stoppingDistance = HuntStop;
            // A pet steps round the party, not through it (2026-10-18, SimDummyTests: a Bonecaller's thrall walking in to the dummy shoved
            // the Scoundrel at its side out of reach, 3 s of 40 not striking): its avoidance yields to everyone at the default 50.
            if (agent != null) agent.avoidancePriority = PetAvoidance;
            Refit(); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            actor.Health.Died += h => session.Meter.Death(actor);   // the scorecard's deaths
        }
        /// <summary>Its health for the Ranger's level and talents (kept whole when it grows).</summary>
        public void Refit()
        {
            if (actor == null || kit == null) return;
            int max = Mathf.RoundToInt(baseHealth * kit.PetHealthScale);
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, max);
        }
        /// <summary>Its bite: its keeper's level's (a sim's wolf goes by the sim's level, 2026-10-17: it went by yours) and its keeper's talents.</summary>
        public int Bite { get { return BiteAt(Master != null && Master.sim != null ? Master.sim.level : session.Progress.Level, kit.PetBiteScale); } }
        /// <summary>A wolf's bite at a keeper's level and bite scale (SimCompanion.PetDps counts it in its keeper's damage).</summary>
        public static int BiteAt(int level, float scale) { return Mathf.Max(1, Mathf.RoundToInt((6 + 2 * Mathf.Max(1, level)) * scale)); }
        /// <summary>Sent at a target; its first bite lands <paramref name="firstBite"/> times as hard (Sic).</summary>
        public void Hunt(EncounterEnemy target, float firstBite)
        {
            if (target == null || !target.actor.IsAlive || !actor.IsAlive) return;
            if (Quarry != target) { Quarry = target; huntSince = Time.time; fury = Mathf.Max(fury, firstBite); } else fury = Mathf.Max(fury, firstBite);
        }
        /// <summary>
        /// At heel it joins the Ranger's fight unasked (playtest note 90, 2026-10-07: "I call a companion but he does nothing"): what the
        /// Ranger is fighting, else whatever is after the Ranger, the wolf or the party, nearest first, within 25 m.
        /// </summary>
        void Assist()
        {
            var t = Master != null ? Master.Quarry : session.Target;
            if (t != null && t.actor.IsAlive && !SimCompanion.Held(t) && (Master != null || session.FightingTarget) && Vector3.Distance(Heel.position, t.transform.position) < 30) { Hunt(t, 1); return; }
            EncounterEnemy best = null; float bestD = 25;
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.FightingParty || e.LeaveAlone || SimCompanion.Held(e)) continue;   // a held mob is left held
                float d = Vector3.Distance(transform.position, e.transform.position); if (d < bestD) { bestD = d; best = e; }
            }
            if (best != null) Hunt(best, 1);
        }
        /// <summary>Staying: only what is after the wolf itself.</summary>
        void Defend()
        {
            foreach (var e in session.Enemies) if (e != null && e.Engaged && e.Victim == actor && Vector3.Distance(transform.position, e.transform.position) < 12) { Hunt(e, 1); return; }
        }
        void Update()
        {
            if (session == null || session.Paused || actor == null || !actor.IsAlive) return;
            var player = Heel; if (player == null) return;
            if (Master != null && !Master.actor.IsAlive)
            {
                // Its keeper down: it still leaves harmful ground (tune-3: the Ranger's wolf stood 100 s a pull in the red while the Ranger lay dead).
                if (GroundMarks.InHarm(transform.position) && agent.isOnNavMesh) { session.CountHarm(actor, Time.deltaTime); var o = GroundMarks.StepOut(NavMesh.SamplePosition(transform.position, out var u0, 2, NavMesh.AllAreas) ? u0.position : transform.position - Vector3.up); if (o.HasValue) { agent.isStopped = false; agent.speed = 6; agent.SetDestination(o.Value); } }
                return;
            }
            if (Quarry != null && (!Quarry.actor.IsAlive || !Quarry.Engaged && Vector3.Distance(player.position, Quarry.transform.position) > 40)) { Quarry = null; fury = 1; }
            if (Quarry == null) { if (Mode == Order.Assist) Assist(); else Defend(); }
            float toPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (Time.time >= nextCatchUp)
            {
                nextCatchUp = Time.time + 1;
                bool stuck = !agent.isOnNavMesh || (toPlayer > 30 && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete);
                if (stuck && NavMesh.SamplePosition(player.transform.position - player.transform.forward * 2, out var near, 12, NavMesh.AllAreas)) agent.Warp(near.position);
            }
            if (!agent.isOnNavMesh) return;
            if (Quarry != null && Quarry.Brood && Quarry.LeaveAlone) { Quarry = null; fury = 1; }
            // Out of harmful ground first, as its keeper (Chris: "pets just standing in it").
            if (GroundMarks.InHarm(transform.position))
            {
                session.CountHarm(actor, Time.deltaTime);
                var feet = NavMesh.SamplePosition(transform.position, out var under, 2, NavMesh.AllAreas) ? under.position : transform.position - Vector3.up;
                var spot = Quarry != null ? GroundMarks.StrikeSpot(transform.position, Quarry.transform.position, Quarry.BodyRadius, BiteReach) : null;
                var go = spot ?? GroundMarks.StepOut(feet);
                if (go.HasValue && agent.isOnNavMesh) { agent.isStopped = false; agent.speed = 6; agent.SetDestination(go.Value); return; }
            }   // a held brood is not bitten: the bite breaks the raid's hold (raid 6)
            if (Quarry != null)
            {
                float reach = Vector3.Distance(transform.position, Quarry.transform.position);
                // A sim's pet with no bite for NoBiteWarp seconds of the hunt (2026-10-17, SimDummyTests: a sim Bonecaller's thrall never
                // bit the dummy and the Mage landed 37 of its 51.5): no way to its quarry, or held off by the press round it. It is put
                // beside the quarry, on the navmesh, and bites.
                if (Master != null && reach > BiteReach && Time.time - Mathf.Max(huntSince, lastBiteAt) > NoBiteWarp)
                {
                    // On a free spot of a ring round it, on its own floor, clear of the party (WarpSpot); its floor is the navmesh under it.
                    var q = Quarry.transform.position;
                    float floorY = NavMesh.SamplePosition(q, out var under, 3, NavMesh.AllAreas) ? under.position.y : q.y - 1;
                    Func<Vector3, Vector3?> floor = p =>
                    {
                        p.y = floorY;
                        if (!NavMesh.SamplePosition(p, out var h, .5f, NavMesh.AllAreas) || Mathf.Abs(h.position.y - floorY) > .8f) return null;
                        return h.position;
                    };
                    if (WarpSpot(q, Crowd(), floor, out var spot)) { agent.Warp(spot); reach = Vector3.Distance(transform.position, Quarry.transform.position); }
                    huntSince = Time.time;
                }
                agent.speed = 5.5f; agent.isStopped = reach <= BiteReach;
                if (!agent.isStopped) agent.SetDestination(Quarry.transform.position);
                else
                {
                    var face = Quarry.transform.position - transform.position; face.y = 0;
                    if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8);
                    if (Time.time >= nextBite)
                    {
                        nextBite = Time.time + BiteInterval; lastBiteAt = Time.time;
                        int bite = Mathf.RoundToInt(Bite * fury); fury = 1;
                        Quarry.Receive(bite, actor);
                        if (kit.PetHolds) Quarry.threat.Add(actor.EntityId.Value, bite);   // Howl: its bites hold attention like a tank's
                        kit.OnPetBite(Quarry, bite);
                        visual?.Strike();
                    }
                }
                return;
            }
            if (Mode == Order.Stay)
            {
                float off = Vector3.Distance(transform.position, stayAt);
                agent.isStopped = off < 1.2f; if (!agent.isStopped) { agent.SetDestination(stayAt); agent.speed = 4.5f; }
                return;
            }
            // At heel: a pace behind the Ranger's shoulder, at the Ranger's own pace.
            agent.isStopped = toPlayer < 2.5f;
            if (!agent.isStopped) { agent.SetDestination(player.transform.position - player.transform.forward * 1.5f + player.transform.right * 1.2f); agent.speed = AdventurerMotor.Running ? 5.8f : toPlayer > 9 ? 4.5f : 2.4f; }
        }
    }
}
