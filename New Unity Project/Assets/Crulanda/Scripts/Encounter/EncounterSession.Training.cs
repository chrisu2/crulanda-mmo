using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Combat;
using Crulanda.Core;
using Crulanda.Gameplay;
using EntityId = Crulanda.Core.EntityId;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The training field (Chris, 2026-10-13: "put a few test dummies in town so the player can test their builds maybe just one for dps
    /// and one for heals", "put out in a field near town", "these dummies should be targetable and hitable / healable of course"). GAME-ONLY.
    /// Zone props of kind training_dummy and mending_dummy (Oakhaven's field south-east of town) get, at their feet:
    /// - the training dummy: a foe you target and strike. It never moves or strikes back, has no armour, stands at your level with five
    ///   million health, and is whole again five seconds after the last blow. The balance harness's dummies are the same.
    /// - the mending dummy: a friend you click and heal. While you are within 25 m it loses 4% of its health a second (never below a
    ///   fifth), and every healer's party (each kit's Party(), the Druid's and Paladin's heal target, the spirits) counts it while you
    ///   are within 40 m. The harness's wounded dummy is the same.
    /// Striking the one or mending the other counts as being in a fight, so the meter (/meter, /meter reset) times it.
    /// </summary>
    public partial class EncounterSession
    {
        public const float MendingNear = 25, MendingReach = 40, MendingLoss = .04f, MendingFloor = .2f, TrainingQuiet = 5;
        public const int TrainingDummyHealth = 5000000, MendingHealthPerLevel = 200;
        /// <summary>The field's foe (null where the zone has none).</summary>
        public EncounterEnemy TrainingDummy { get; private set; }
        /// <summary>The field's wounded friend (null where the zone has none).</summary>
        public Actor MendingDummy { get; private set; }
        public bool FocusMending { get; private set; }
        float trainingTickAt, mendedAt = -100; int mendingLast;

        void SpawnTrainingField()
        {
            TrainingDummy = null; MendingDummy = null; FocusMending = false;
            if (Zone == null || Zone.Zone.props == null) return;
            string zoneShort = Zone.Zone.id.Replace("zone.", "");
            foreach (var p in Zone.Zone.props)
            {
                if (p == null) continue;
                var point = Zone.Ground(p.at) + Vector3.up;   // the transform rides 1 m over the feet
                var facing = Quaternion.Euler(0, p.rotation, 0);
                if (p.kind == "training_dummy" && TrainingDummy == null) TrainingDummy = SpawnTrainingDummy("training." + zoneShort + ".dummy", point, facing);
                else if (p.kind == "mending_dummy" && MendingDummy == null) MendingDummy = SpawnMendingDummy("training." + zoneShort + ".mending", point, facing);
            }
        }
        EncounterEnemy SpawnTrainingDummy(string id, Vector3 point, Quaternion facing)
        {
            var go = new GameObject("Training dummy"); go.SetActive(false);
            go.transform.SetParent(actorsRoot.transform); go.transform.position = point; go.transform.rotation = facing;
            var a = go.AddComponent<Actor>(); a.Initialize(content.enemy, "Training dummy", Progress.Level, new EntityId(id));
            go.AddComponent<Combatant>();
            var box = go.AddComponent<BoxCollider>(); box.size = new Vector3(.8f, 1.9f, .8f); box.center = new Vector3(0, -1 + .95f, 0);   // clicks and tab
            var agent = go.AddComponent<NavMeshAgent>(); agent.radius = .35f; agent.height = 1.9f; agent.baseOffset = 1; agent.speed = 0; agent.angularSpeed = 0;
            var e = go.AddComponent<EncounterEnemy>(); e.actor = a; e.persistentId = id; e.session = this; e.Camp = false;
            go.SetActive(true); e.Initialize(); e.enabled = false;   // no thought, no step, no blow: only a body to strike
            // Switching on, its agent snaps it to the nearest navmesh, which can be metres off (the 13:27 frames): back to its spot, on the
            // navmesh there if there is one within 3 m, else at the spot with the agent off (it never moves).
            if ((go.transform.position - point).sqrMagnitude > 1.5f * 1.5f)
            {
                if (NavMesh.SamplePosition(point - Vector3.up, out var hit, 3, NavMesh.AllAreas) && agent.isOnNavMesh) agent.Warp(hit.position);
                else { agent.enabled = false; go.transform.position = point; }
            }
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            if (Zone != null) Zone.TrainingDummyLook(go.transform, Vector3.down, false);
            DressTrainingDummy(e); Enemies.Add(e);
            return e;
        }
        void DressTrainingDummy(EncounterEnemy e)
        {
            e.Elite = false; e.actor.SetLevel(Progress.Level); e.actor.Stats.SetBase(StatType.Armor, 0);
            e.actor.Health.Pool.SetMax(TrainingDummyHealth, false); e.actor.Health.Pool.SetCurrent(TrainingDummyHealth);
        }
        Actor SpawnMendingDummy(string id, Vector3 point, Quaternion facing)
        {
            var go = new GameObject("Mending dummy"); go.SetActive(false);
            go.transform.SetParent(actorsRoot.transform); go.transform.position = point; go.transform.rotation = facing;
            var a = go.AddComponent<Actor>(); a.Initialize(content.player, "Mending dummy", Progress.Level, new EntityId(id));
            go.AddComponent<Combatant>();
            go.SetActive(true); DressMendingDummy(a);
            if (Zone != null) Zone.TrainingDummyLook(go.transform, Vector3.down, true);
            return a;
        }
        void DressMendingDummy(Actor a)
        {
            a.SetLevel(Progress.Level); int max = MendingHealthPerLevel * Mathf.Max(1, Progress.Level);
            a.Stats.SetBase(StatType.MaxHealth, max); a.Health.Pool.SetMax(max, false); a.Health.Pool.SetCurrent(max); mendingLast = max;
        }
        /// <summary>Healable by your party's healers though not in your party: the mending dummy while you are within 40 m of it.</summary>
        public IEnumerable<Actor> Healables()
        {
            if (MendingDummy != null && MendingDummy.IsAlive && Player != null && Vector3.Distance(Player.transform.position, MendingDummy.transform.position) <= MendingReach)
                yield return MendingDummy;
        }
        /// <summary>Striking the training dummy or mending the mending dummy in the last few seconds: a fight, for the meter.</summary>
        public bool TrainingBusy { get { return (TrainingDummy != null && !TrainingDummy.Quiet) || Time.time - mendedAt < TrainingQuiet; } }
        void TendTraining()
        {
            if (Player == null || Time.time < trainingTickAt) return; trainingTickAt = Time.time + 1;
            if (TrainingDummy != null && TrainingDummy.actor != null)
            {
                var pool = TrainingDummy.actor.Health.Pool;
                if (TrainingDummy.actor.Level != Progress.Level || (TrainingDummy.Quiet && pool.Current < pool.Max) || pool.Current < pool.Max / 5) DressTrainingDummy(TrainingDummy);
            }
            if (MendingDummy != null)
            {
                var pool = MendingDummy.Health.Pool;
                if (MendingDummy.Level != Progress.Level) DressMendingDummy(MendingDummy);
                if (pool.Current > mendingLast) mendedAt = Time.time;   // someone mended it since the last second
                bool near = Vector3.Distance(Player.transform.position, MendingDummy.transform.position) <= MendingNear;
                if (near && pool.Ratio > MendingFloor) MendingDummy.Health.ApplyDamage(Mathf.Min(pool.Current - Mathf.CeilToInt(pool.Max * MendingFloor), Mathf.CeilToInt(pool.Max * MendingLoss)));
                else if (!near && pool.Current < pool.Max) pool.SetCurrent(pool.Max);   // nobody practising: whole again
                mendingLast = pool.Current;
            }
        }
        public void SelectMending() { FocusMending = true; FocusVillager = null; FocusMira = false; FocusSimId = null; Target = null; AutoAttack = false; }
    }
}
