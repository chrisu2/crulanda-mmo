#if UNITY_EDITOR
using EntityId = Crulanda.Core.EntityId;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Core;
using Crulanda.Data;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    /// <summary>Exercises the MonoBehaviour wiring (Actor + Health + ResourcePool + registry + event bus).</summary>
    public class ActorSmokeTests
    {
        GameObject _go;
        ActorArchetypeDefinition _archetype;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ActorRegistry.Clear();

            _archetype = ScriptableObject.CreateInstance<ActorArchetypeDefinition>();
            _archetype.EditorSetIdentity(new ContentId("actor.smoke"), "Smoke Actor", CanonStatus.GameOnly);
            _archetype.EditorConfigure(3, Disposition.Hostile, ActorClassification.Normal, ResourceKind.Mana,
                new[]
                {
                    new StatValue(StatType.MaxHealth, 100), new StatValue(StatType.MaxPower, 40)
                });
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            Object.Destroy(_archetype);
            EventBus.Clear();
        }

        Actor Spawn()
        {
            _go = new GameObject("SmokeActor");
            var actor = _go.AddComponent<Actor>();
            actor.Initialize(_archetype, null, 0);
            return actor;
        }

        [UnityTest]
        public IEnumerator Reinitialize_clears_previous_stats_and_modifiers()
        {
            var actor = Spawn();
            actor.Stats.SetBase(StatType.Strength, 99);
            actor.Stats.AddModifier(new StatModifier(StatType.MaxHealth, ModifierOp.Flat, 100, this));
            actor.Initialize(null, "Reset", 1);
            Assert.AreEqual(0, actor.Stats.Get(StatType.Strength));
            Assert.AreEqual(1, actor.Health.Pool.Max);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Restored_identity_survives_activation()
        {
            _go = new GameObject("Restored");
            _go.SetActive(false);
            var actor = _go.AddComponent<Actor>();
            var id = EntityId.New();
            actor.Initialize(_archetype, "Restored", 3, id);
            _go.SetActive(true);
            yield return null;
            Assert.AreEqual(id, actor.EntityId);
            Assert.AreEqual(1, ActorRegistry.Count);
        }

        [UnityTest]
        public IEnumerator Maximum_reduction_to_zero_health_publishes_death_once()
        {
            var actor = Spawn();
            actor.Health.ApplyDamage(99);
            int deaths = 0;
            actor.Health.Died += h => deaths++;
            actor.Stats.SetBase(StatType.MaxHealth, 1);
            Assert.AreEqual(0, actor.Health.Pool.Current);
            Assert.IsFalse(actor.IsAlive);
            actor.Health.ApplyDamage(10);
            Assert.AreEqual(1, deaths);
            yield return null;
        }
        [UnityTest]
        public IEnumerator Actor_initializes_from_archetype_and_registers()
        {
            var actor = Spawn();
            yield return null;

            Assert.AreEqual("Smoke Actor", actor.DisplayName);
            Assert.AreEqual(3, actor.Level);
            Assert.AreEqual(Disposition.Hostile, actor.Disposition);
            Assert.AreEqual(100, actor.Health.Pool.Max);
            Assert.AreEqual(100, actor.Health.Pool.Current);
            Assert.AreEqual(40, actor.Resource.Pool.Max);
            Assert.IsTrue(actor.EntityId.IsValid);
            Assert.AreSame(actor, ActorRegistry.FindByName("smoke"));
        }

        [UnityTest]
        public IEnumerator Lethal_damage_kills_once_and_publishes_event()
        {
            var actor = Spawn();
            yield return null;

            int deaths = 0;
            EventBus.Subscribe<ActorDiedEvent>(e => { if (e.Actor == actor) deaths++; });

            Assert.AreEqual(60, actor.Health.ApplyDamage(60));
            Assert.IsTrue(actor.IsAlive);

            Assert.AreEqual(40, actor.Health.ApplyDamage(500)); // only 40 left to remove
            Assert.IsFalse(actor.IsAlive);
            Assert.AreEqual(0, actor.Health.ApplyDamage(10));   // already dead
            Assert.AreEqual(1, deaths);
        }

        [UnityTest]
        public IEnumerator Healing_is_capped_and_blocked_while_dead()
        {
            var actor = Spawn();
            yield return null;

            actor.Health.ApplyDamage(30);
            Assert.AreEqual(30, actor.Health.ApplyHealing(100));

            actor.Health.ApplyDamage(1000);
            Assert.AreEqual(0, actor.Health.ApplyHealing(10));

            actor.Health.Revive(50);
            Assert.IsTrue(actor.IsAlive);
            Assert.AreEqual(50, actor.Health.Pool.Current);
        }

        [UnityTest]
        public IEnumerator Raising_max_health_via_stats_preserves_the_ratio()
        {
            var actor = Spawn();
            yield return null;

            actor.Health.ApplyDamage(50); // 50/100
            actor.Stats.AddModifier(new Crulanda.Gameplay.StatModifier(
                StatType.MaxHealth, ModifierOp.Flat, 100f, this));

            Assert.AreEqual(200, actor.Health.Pool.Max);
            Assert.AreEqual(100, actor.Health.Pool.Current);
        }

        [UnityTest]
        public IEnumerator Disabled_actors_leave_the_registry()
        {
            var actor = Spawn();
            yield return null;
            Assert.AreEqual(1, ActorRegistry.Count);

            _go.SetActive(false);
            Assert.AreEqual(0, ActorRegistry.Count);
        }
    }
}

#endif
