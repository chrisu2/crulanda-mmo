#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;

namespace Crulanda.Tests
{
    public class CombatIntegrationTests
    {
        GameObject go;
        Actor actor;
        Combatant combat;
        [SetUp]
        public void Setup()
        {
            go = new GameObject("Combat test"); actor = go.AddComponent<Actor>();
            actor.Stats.SetBase(StatType.MaxHealth, 100); actor.Health.ApplyHealing(100);
            combat = go.AddComponent<Combatant>();
        }
        [TearDown]
        public void Cleanup() { Object.DestroyImmediate(go); }
        [UnityTest]
        public IEnumerator Guard_expires_and_death_removes_effects()
        {
            var guard = new StatusEffectDefinition { id = "guard", duration = .1f, incomingDamageMultiplier = .4f };
            Assert.IsTrue(combat.Apply(guard)); Assert.AreEqual(5, combat.Damage(13));
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(13, combat.Damage(13));
            combat.Apply(guard); actor.Health.ApplyDamage(1000);
            Assert.AreEqual(0, combat.Statuses.Count);
            actor.Health.Revive(100); Assert.AreEqual(13, combat.Damage(13));
        }
        [UnityTest]
        public IEnumerator Primary_stat_buffs_recalculate_and_expire_without_drifting()
        {
            var derived = go.AddComponent<DerivedStatsController>(); derived.Configure(new DerivedStatRules(), 7);
            Assert.AreEqual(19, actor.Stats.Get(StatType.AttackPower));
            combat.Apply(new StatusEffectDefinition { id = "strength", duration = .1f, modifiers = new[] {
                new StatusModifierDefinition { stat = StatType.Strength, value = 3 } } });
            Assert.AreEqual(22, actor.Stats.Get(StatType.AttackPower));
            actor.SetLevel(2); Assert.AreEqual(24, actor.Stats.Get(StatType.AttackPower));
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(21, actor.Stats.Get(StatType.AttackPower));
            actor.Initialize(null, "Rebuilt", 1);
            Assert.AreEqual(19, actor.Stats.Get(StatType.AttackPower));
            Assert.AreEqual(180, actor.Health.Pool.Max);
        }
        [UnityTest]
        public IEnumerator One_effect_applies_its_stat_changes_together()
        {
            actor.Health.ApplyDamage(99);
            Assert.IsTrue(combat.Apply(new StatusEffectDefinition { id = "balanced", modifiers = new[] {
                new StatusModifierDefinition { stat = StatType.MaxHealth, value = -99 },
                new StatusModifierDefinition { stat = StatType.MaxHealth, value = 99 } } }));
            Assert.IsTrue(actor.IsAlive); Assert.AreEqual(1, actor.Health.Pool.Current);
            yield return null;
        }
        [UnityTest]
        public IEnumerator Lethal_stat_change_clears_effects_without_leaving_modifiers()
        {
            actor.Health.ApplyDamage(99);
            var effect = new StatusEffectDefinition { id = "lethal", modifiers = new[] {
                new StatusModifierDefinition { stat = StatType.MaxHealth, value = -99 },
                new StatusModifierDefinition { stat = StatType.Strength, value = 3 } } };
            Assert.IsFalse(combat.Apply(effect));
            Assert.IsFalse(actor.IsAlive); Assert.AreEqual(0, combat.Statuses.Count);
            Assert.AreEqual(0, actor.Stats.Get(StatType.Strength));
            Assert.AreEqual(100, actor.Stats.Get(StatType.MaxHealth));
            yield return null;
        }
    }
}
#endif
