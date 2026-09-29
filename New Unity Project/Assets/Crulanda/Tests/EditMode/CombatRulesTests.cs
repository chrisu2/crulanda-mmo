using NUnit.Framework;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    public class CombatRulesTests
    {
        [TestCase(13, 0, 1, 13)]
        [TestCase(13, 0, .4f, 5)]
        [TestCase(13, 100, 1, 7)]
        [TestCase(13, 0, 2, 26)]
        [TestCase(0, 100, 1, 0)]
        public void Damage_uses_armor_and_status_multiplier(int amount, float armor, float multiplier, int expected)
        { Assert.AreEqual(expected, CombatMath.Damage(amount, armor, multiplier)); }

        [Test]
        public void Derived_stats_combine_level_attributes_and_equipment()
        {
            var values = DerivedStatCalculator.Calculate(new DerivedStatRules(), 2, 2, 3, 4, 5, 7);
            Assert.AreEqual(215, values.health); Assert.AreEqual(24, values.attackPower);
            Assert.AreEqual(4, values.spellPower); Assert.AreEqual(5, values.armor);
        }
        [Test]
        public void Status_refresh_extends_time_without_stacking_modifiers()
        {
            var stats = new StatBlock(); stats.SetBase(StatType.AttackPower, 12);
            var effects = new StatusEffectRuntime(stats);
            var definition = new StatusEffectDefinition { id = "test.power", duration = 5,
                modifiers = new[] { new StatusModifierDefinition { stat = StatType.AttackPower, value = 5 } } };
            Assert.IsTrue(effects.Apply(definition, 0)); Assert.AreEqual(17, stats.Get(StatType.AttackPower));
            Assert.IsTrue(effects.Apply(definition, 4)); Assert.AreEqual(17, stats.Get(StatType.AttackPower));
            effects.Tick(5); Assert.AreEqual(1, effects.Count);
            effects.Tick(9); Assert.AreEqual(0, effects.Count); Assert.AreEqual(12, stats.Get(StatType.AttackPower));
        }
        [Test]
        public void Removing_one_effect_preserves_other_modifier_sources()
        {
            var stats = new StatBlock(); var equipment = new object();
            stats.AddModifier(new StatModifier(StatType.Strength, ModifierOp.Flat, 7, equipment));
            var effects = new StatusEffectRuntime(stats);
            effects.Apply(new StatusEffectDefinition { id = "buff", modifiers = new[] {
                new StatusModifierDefinition { stat = StatType.Strength, value = 3 } } }, 0);
            effects.Clear(); Assert.AreEqual(7, stats.Get(StatType.Strength));
        }
        [Test]
        public void Runtime_snapshots_status_magnitude_and_does_not_mutate_definition()
        {
            var effects = new StatusEffectRuntime(new StatBlock());
            var definition = new StatusEffectDefinition { id = "guard", incomingDamageMultiplier = .4f };
            effects.Apply(definition, 0); definition.incomingDamageMultiplier = 1;
            Assert.AreEqual(.4f, effects.IncomingDamageMultiplier);
            effects.Clear(); Assert.AreEqual(1, effects.IncomingDamageMultiplier);
            Assert.AreEqual(1, definition.incomingDamageMultiplier);
        }
        [Test]
        public void Invalid_status_is_rejected_atomically()
        {
            var effects = new StatusEffectRuntime(new StatBlock());
            Assert.IsFalse(effects.Apply(new StatusEffectDefinition { id = "bad", duration = float.NaN }, 0));
            Assert.AreEqual(0, effects.Count);
        }
        [Test]
        public void Modifier_removal_callbacks_observe_a_consistent_stat_snapshot()
        {
            var stats = new StatBlock(); var source = new object();
            stats.AddModifiers(new[] {
                new StatModifier(StatType.Strength, ModifierOp.Flat, 3, source),
                new StatModifier(StatType.Stamina, ModifierOp.Flat, 4, source) });
            Assert.AreEqual(3, stats.Get(StatType.Strength)); Assert.AreEqual(4, stats.Get(StatType.Stamina));
            stats.Changed += changed => {
                Assert.AreEqual(0, stats.Get(StatType.Strength)); Assert.AreEqual(0, stats.Get(StatType.Stamina));
            };
            stats.RemoveModifiersFromSource(source);
        }
        [Test]
        public void Proximity_threat_is_independent_of_frame_count()
        {
            foreach (int frames in new[] { 10, 60, 1000 })
            {
                var threat = new EncounterThreat();
                for (int i = 0; i < frames; i++) threat.AddProximity("player", 1f / frames);
                threat.Add("healer", 2);
                Assert.AreEqual("healer", threat.Choose(1, id => true));
                threat.Add("player", 2);
                Assert.AreEqual("player", threat.Choose(1, id => true));
            }
        }
    }
}
