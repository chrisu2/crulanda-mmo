using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hunting for hides (BUILD_PLAN step 11; ADDENDUM G): which animals are game, and what a game animal is worth. Deer and rabbits
    /// are game: targetable, killable, skinned through the loot window for their hide, back about four minutes after they fall.
    /// Hens, sheep, the village cats and crows are never game. A game animal gives no experience and no coin. All names GAME-ONLY.
    /// </summary>
    public static class GameAnimals
    {
        /// <summary>The kinds of a zone's critter groups (ZoneLife.critters) that are game.</summary>
        public static readonly string[] Kinds = { "deer", "rabbit" };
        /// <summary>The animals that are never hunted: the farm's hens, sheep, cows, horses and donkeys, the village cats, and the
        /// crows (no leather).</summary>
        public static readonly string[] NeverHunted = { "chicken", "sheep", "cat", "crow", "horse", "donkey", "cow" };
        public static bool IsGame(string kind) { return kind != null && Array.IndexOf(Kinds, kind) >= 0; }
        /// <summary>Health: a rabbit 15, a deer 70, whatever the zone's level.</summary>
        public static int Health(string kind) { return kind == "deer" ? 70 : 15; }
        /// <summary>The name over it.</summary>
        public static string Name(string kind) { return kind == "deer" ? "Hill deer" : "Rabbit"; }
        /// <summary>Seconds a game animal lies before it is back, somewhere in its group's circle.</summary>
        public const float RespawnSeconds = 240;
        /// <summary>What E offers at the body of game or of a beast with a hide or pelt (EncounterEnemy.Skinnable).</summary>
        public const string SkinPrompt = "Skin the body";
        /// <summary>A badly hurt animal (under this share of its health) bolts at only <see cref="WoundedPace"/> of its pace: it can be run down.</summary>
        public const float Wounded = .35f, WoundedPace = .6f;
        /// <summary>
        /// Seconds of the player moving inside its notice distance before an animal bolts: a little while when they sneak (a short
        /// stalk is possible), a moment when they walk or run. Standing still lets it settle again.
        /// </summary>
        public const float WarySneaking = 2.5f, WaryWalking = .6f;
        /// <summary>How near the player may come before an animal of this kind starts to watch them: its critter's flee distance (deer 16 m,
        /// rabbits 7 m), half that when the player sneaks (Ctrl).</summary>
        public static float Notice(float fleeRadius, bool sneaking) { return sneaking ? fleeRadius * .5f : fleeRadius; }
        /// <summary>
        /// What lies on a game animal's body: the entries of the loot tables tagged exactly <paramref name="tag"/> (rabbit, deer) whose
        /// level band holds <paramref name="level"/>. Never generated gear, and never coins (the body holds none).
        /// </summary>
        public static List<LootDrop> RollHide(ItemDatabase items, string tag, int level, System.Random rng)
        {
            var drops = new List<LootDrop>(); if (items == null || rng == null) return drops;
            foreach (var t in items.Loot)
            {
                if (t == null || t.tag != tag || level < t.levelMin || level > t.levelMax) continue;
                foreach (var e in t.entries ?? new LootEntry[0])
                    if (e != null && items.Get(e.item) != null && rng.NextDouble() < e.chance) drops.Add(new LootDrop(e.item, e.min + rng.Next(Mathf.Max(1, e.max - e.min + 1))));
            }
            return drops;
        }
    }

    /// <summary>
    /// A game animal's behaviour: it grazes and wanders inside its group's circle, wearing the critter's body (CritterBody) on an
    /// EncounterEnemy that never fights. A player moving inside its notice distance makes it look up and watch; kept up, it bolts
    /// away on the navmesh, and a blow makes it bolt at once. It never steps into water by choice.
    /// </summary>
    public sealed class GameAnimal : MonoBehaviour
    {
        enum State { Graze, Walk, Wary, Bolt }
        public EncounterEnemy Enemy { get; private set; }
        public CritterBody Body { get; private set; }
        public string Kind { get { return Body.Kind; } }
        /// <summary>The centre and radius of its group's circle (it wanders there, and comes back there after its respawn).</summary>
        public Vector2 Home { get; private set; }
        public float Radius { get; private set; }
        /// <summary>Tests only: the player counts as sneaking (true) or not (false); null asks the player's motor.</summary>
        public static bool? SneakingOverride;
        public bool Bolting { get { return state == State.Bolt; } }
        /// <summary>Watching the player, head up (it bolts if they keep coming).</summary>
        public bool Watching { get { return state == State.Wary; } }
        /// <summary>How alert it is: seconds of the player moving inside its notice distance (it bolts at WarySneaking or WaryWalking).</summary>
        public float Alertness { get { return alert; } }
        NavMeshAgent agent; State state; float until, alert, boltUntil; Vector3 lastPlayer = new Vector3(float.NaN, 0, 0);
        NavMeshPath boltPath;   // made on the first bolt (a NavMeshPath cannot be made in a field initialiser)

        /// <summary>Gives a game animal (an EncounterEnemy made by EncounterSession.SpawnGame) its body and behaviour.</summary>
        public static GameAnimal Attach(EncounterEnemy enemy, string kind, Vector2 home, float radius, float look, float seed)
        {
            var g = enemy.gameObject.AddComponent<GameAnimal>(); g.Enemy = enemy; g.Home = home; g.Radius = radius;
            g.Body = CritterBody.Build(enemy.transform, kind, look, seed, -1);   // its transform rides 1 m up, on its agent's base offset
            g.agent = enemy.GetComponent<NavMeshAgent>(); g.until = Time.time + look * 4;
            return g;
        }
        /// <summary>How near the player may come before it starts to watch them, as they move now.</summary>
        public float NoticeDistance(bool sneaking) { return GameAnimals.Notice(Body.FleeRadius, sneaking); }
        bool PlayerSneaking(EncounterSession s)
        {
            if (SneakingOverride.HasValue) return SneakingOverride.Value;
            var motor = s.Player.GetComponent<AdventurerMotor>(); return motor != null && motor.Sneaking;
        }
        // Timed for the performance probe (playtest note 23).
        static readonly Unity.Profiling.ProfilerMarker perfMark = new Unity.Profiling.ProfilerMarker("PERF.GameAnimal");
        void Update() { using (perfMark.Auto()) UpdateTimed(); }
        void UpdateTimed()
        {
            var s = Enemy != null ? Enemy.session : null;
            if (s == null || s.Paused || s.Player == null || agent == null || !Enemy.actor.IsAlive) return;
            float pace = state == State.Bolt ? Body.FleeSpeed * (Enemy.actor.Health.Pool.Ratio < GameAnimals.Wounded ? GameAnimals.WoundedPace : 1) : Body.Speed;
            agent.speed = Enemy.Rooted ? 0 : pace * Enemy.SlowFactor;
            // Noticing the player: inside the notice distance a moving player keeps it watching and it grows alert; past the limit it
            // bolts. A player standing still (or gone) lets it settle.
            var me = transform.position; var player = s.Player.transform.position;
            float d = new Vector2(me.x - player.x, me.z - player.z).magnitude;
            if (state == State.Bolt) lastPlayer = player;
            else
            {
                bool sneaking = PlayerSneaking(s), near = s.Player.IsAlive && d < NoticeDistance(sneaking);
                // Moving: going over the ground faster than a shuffle, however (walking, sneaking, swimming, a charge).
                bool moving = !float.IsNaN(lastPlayer.x) && Time.deltaTime > 0 && new Vector2(player.x - lastPlayer.x, player.z - lastPlayer.z).magnitude / Time.deltaTime > .3f;
                alert = near && moving ? alert + Time.deltaTime : Mathf.Max(0, alert - Time.deltaTime * .5f);
                if (alert >= (sneaking ? GameAnimals.WarySneaking : GameAnimals.WaryWalking)) { Bolt(player); }
                else if (near && state != State.Wary) { state = State.Wary; Halt(); }
                else if (!near && state == State.Wary && alert <= 0) { state = State.Graze; until = Time.time + 1 + UnityEngine.Random.value * 2; }
                lastPlayer = player;
            }
            switch (state)
            {
                case State.Graze:
                    Body.Rest();
                    if (Time.time > until)
                    {
                        var p = PickPoint(Home, Radius);
                        if (p.HasValue && agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(p.Value); state = State.Walk; } else until = Time.time + 2;
                    }
                    break;
                case State.Wary:
                    Body.Alert();
                    var look = player - me; look.y = 0;
                    if (look.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 4);
                    break;
                case State.Walk: case State.Bolt:
                    Body.Stride(agent.velocity.magnitude);
                    if (Arrived() || (state == State.Bolt && Time.time > boltUntil))
                    {
                        // Hurt, with the hunter still close: off again, not grazing within reach (playtest note 22).
                        if (state == State.Bolt && s.Player.IsAlive && d < HurtFleeDistance && Enemy.actor.Health.Pool.Ratio < 1) { Bolt(player); break; }
                        // After a bolt it stands a while longer before it wanders again.
                        float rest = state == State.Bolt ? 4 : 1.5f; Halt();
                        state = State.Graze; until = Time.time + rest + UnityEngine.Random.value * 5;
                    }
                    break;
            }
        }
        /// <summary>A hurt animal that pulls up with the hunter nearer than this bolts again.</summary>
        public const float HurtFleeDistance = 35;
        bool Arrived() { return agent.isOnNavMesh && !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + .2f); }
        void Halt() { if (agent.isOnNavMesh) { agent.ResetPath(); agent.velocity = Vector3.zero; } }
        /// <summary>
        /// Off and away from <paramref name="from"/> (the player, or whoever hit it): 12 to 18 m for a deer, 8 to 12 m for a rabbit, on
        /// dry ground it can reach; at full pace, or limping when badly hurt.
        /// </summary>
        public void Bolt(Vector3 from)
        {
            if (Enemy == null || !Enemy.actor.IsAlive || agent == null || !agent.isOnNavMesh) return;
            var me = transform.position; var away = me - from; away.y = 0;
            away = away.sqrMagnitude > .01f ? away.normalized : transform.forward;
            float run = Kind == "deer" ? 25 + UnityEngine.Random.value * 10 : 12 + UnityEngine.Random.value * 4;   // out of a bolt's reach (playtest note 22)
            // Somewhere it can truly run to: a whole path there (not the far bank of a creek, not a ledge the navmesh does not join),
            // found now and handed to the agent, so the bolt starts this frame. Away first, swinging wider each try; then half as
            // far; only with no such ground at all does it stand and watch. (A rabbit by the Brook pond once stood and watched a
            // walking player: its flight was given up, or ended the frame it began.)
            Vector3? to = null; if (boltPath == null) boltPath = new NavMeshPath();
            for (int pass = 0; pass < 2 && !to.HasValue; pass++)
                for (int tries = 0; tries < 8 && !to.HasValue; tries++)
                {
                    var dir = Quaternion.Euler(0, (tries % 2 == 0 ? 1 : -1) * tries * 22, 0) * away; float far = pass == 0 ? run : run * .5f;
                    var p = PickPoint(new Vector2(me.x + dir.x * far, me.z + dir.z * far), 2.5f);
                    if (p.HasValue && NavMesh.CalculatePath(agent.nextPosition, p.Value, NavMesh.AllAreas, boltPath) && boltPath.status == NavMeshPathStatus.PathComplete) to = p;
                }
            alert = 0;
            if (!to.HasValue || !agent.SetPath(boltPath)) { state = State.Wary; return; }   // nowhere to run: it stands its ground and watches
            agent.isStopped = false; state = State.Bolt; boltUntil = Time.time + 9;
        }
        /// <summary>A dry point on the navmesh near <paramref name="around"/> (within <paramref name="range"/>), inside the zone, or null.</summary>
        Vector3? PickPoint(Vector2 around, float range)
        {
            var zone = Enemy.session.Zone; if (zone == null) return null;
            for (int i = 0; i < 6; i++)
            {
                var p = around + new Vector2(UnityEngine.Random.value - .5f, UnityEngine.Random.value - .5f) * range * 2;
                if (Mathf.Abs(p.x) > zone.Half - 4 || Mathf.Abs(p.y) > zone.Half - 4 || zone.WaterAt(p, out _, out _)) continue;
                if (NavMesh.SamplePosition(zone.Ground(p), out var hit, 2, NavMesh.AllAreas) && !zone.WaterAt(new Vector2(hit.position.x, hit.position.z), out _, out _)) return hit.position;
            }
            return null;
        }
        /// <summary>It fell: it lies on its side where it dropped and stops moving.</summary>
        public void Fall() { if (agent != null && agent.isOnNavMesh) agent.ResetPath(); state = State.Graze; alert = 0; Body.LieDown(); }
        /// <summary>Back after its respawn: on its feet, grazing; moved out of the water if its new spot is wet (somewhere dry in its
        /// group's circle, else the nearest dry ground: a circle that takes in a pond can miss the dry part six times running).</summary>
        public void Stand()
        {
            Body.StandUp(); state = State.Graze; alert = 0; until = Time.time + 1 + UnityEngine.Random.value * 3;
            var zone = Enemy.session.Zone; var at = transform.position;
            if (zone == null || agent == null || !zone.WaterAt(new Vector2(at.x, at.z), out _, out _)) return;
            var p = PickPoint(Home, Radius) ?? NearestDry(new Vector2(at.x, at.z));
            if (p.HasValue) agent.Warp(p.Value);
        }
        /// <summary>The nearest dry point on the navmesh to <paramref name="from"/>, in rings of twelve every 3 m out to 30 m, or null.</summary>
        Vector3? NearestDry(Vector2 from)
        {
            var zone = Enemy.session.Zone;
            for (float r = 3; r <= 30; r += 3)
                for (int k = 0; k < 12; k++)
                {
                    float a = k * Mathf.PI / 6; var p = from + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (Mathf.Abs(p.x) > zone.Half - 4 || Mathf.Abs(p.y) > zone.Half - 4 || zone.WaterAt(p, out _, out _)) continue;
                    if (NavMesh.SamplePosition(zone.Ground(p), out var hit, 2, NavMesh.AllAreas) && !zone.WaterAt(new Vector2(hit.position.x, hit.position.z), out _, out _)) return hit.position;
                }
            return null;
        }
    }
}
