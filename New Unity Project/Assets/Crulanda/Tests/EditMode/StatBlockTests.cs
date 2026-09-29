using NUnit.Framework;
using Crulanda.Core;
using Crulanda.Gameplay;

namespace Crulanda.Tests
{
    public class StatBlockTests
    {
        [Test]
        public void Unset_stat_is_zero()
        {
            Assert.AreEqual(0f, new StatBlock().Get(StatType.Strength));
        }

        [Test]
        public void Base_value_is_returned_without_modifiers()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Strength, 10f);
            Assert.AreEqual(10f, s.Get(StatType.Strength));
        }

        [Test]
        public void Flat_modifiers_add_to_base()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Armor, 100f);
            s.AddModifier(new StatModifier(StatType.Armor, ModifierOp.Flat, 20f, "a"));
            s.AddModifier(new StatModifier(StatType.Armor, ModifierOp.Flat, 5f, "b"));
            Assert.AreEqual(125f, s.Get(StatType.Armor));
        }

        [Test]
        public void PercentAdd_modifiers_sum_before_applying()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Strength, 100f);
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.PercentAdd, 0.10f, "a"));
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.PercentAdd, 0.20f, "b"));
            Assert.AreEqual(130f, s.Get(StatType.Strength), 0.0001f);
        }

        [Test]
        public void PercentMult_modifiers_multiply_each_other()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Strength, 100f);
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.PercentMult, 0.10f, "a"));
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.PercentMult, 0.10f, "b"));
            Assert.AreEqual(121f, s.Get(StatType.Strength), 0.0001f);
        }

        [Test]
        public void Formula_is_base_plus_flat_then_percentAdd_then_percentMult()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Strength, 100f);
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.Flat, 20f, "f"));
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.PercentAdd, 0.50f, "pa"));
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.PercentMult, 0.10f, "pm"));
            // (100 + 20) * 1.5 * 1.1 = 198
            Assert.AreEqual(198f, s.Get(StatType.Strength), 0.001f);
        }

        [Test]
        public void Modifiers_only_affect_their_own_stat()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Strength, 10f);
            s.SetBase(StatType.Agility, 10f);
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.Flat, 5f, "a"));
            Assert.AreEqual(10f, s.Get(StatType.Agility));
        }

        [Test]
        public void RemoveModifiersFromSource_removes_only_that_source()
        {
            var s = new StatBlock();
            var sword = new object();
            var buff = new object();
            s.SetBase(StatType.Strength, 10f);
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.Flat, 5f, sword));
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.Flat, 3f, sword));
            s.AddModifier(new StatModifier(StatType.Strength, ModifierOp.Flat, 2f, buff));

            Assert.AreEqual(20f, s.Get(StatType.Strength));
            Assert.AreEqual(2, s.RemoveModifiersFromSource(sword));
            Assert.AreEqual(12f, s.Get(StatType.Strength));
        }

        [Test]
        public void Cached_value_is_refreshed_after_changes()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Strength, 10f);
            Assert.AreEqual(10f, s.Get(StatType.Strength)); // primes the cache
            s.SetBase(StatType.Strength, 15f);
            Assert.AreEqual(15f, s.Get(StatType.Strength));
        }

        [Test]
        public void Changed_event_reports_the_stat()
        {
            var s = new StatBlock();
            StatType? seen = null;
            s.Changed += stat => seen = stat;
            s.AddModifier(new StatModifier(StatType.Armor, ModifierOp.Flat, 1f, "x"));
            Assert.AreEqual(StatType.Armor, seen);
        }

        [Test]
        public void GetRounded_rounds_halves_away_from_zero()
        {
            var s = new StatBlock();
            s.SetBase(StatType.Armor, 10.5f);
            Assert.AreEqual(11, s.GetRounded(StatType.Armor));
        }

        [Test]
        public void LoadBase_sets_all_values()
        {
            var s = new StatBlock();
            s.LoadBase(new[] { new StatValue(StatType.Strength, 7f), new StatValue(StatType.Agility, 9f) });
            Assert.AreEqual(7f, s.Get(StatType.Strength));
            Assert.AreEqual(9f, s.Get(StatType.Agility));
        }
    }
}
