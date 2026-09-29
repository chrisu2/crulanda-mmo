using System;
using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    public class DruidRulesTests
    {
        [Test] public void Druid_tree_loads_four_branches_and_all_implemented_talents()
        {
            var t = TestTalents.Druid();
            Assert.AreEqual(4, t.Branches.Count);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(DruidKit.ImplementedIds.Length, count);
            var p = EncounterSession.FreshProgress("class.druid"); p.experience = EncounterProgress.XpForLevel(10);
            // 5 in Barkhide unlocks Heartwood Brace; a Warrior id is foreign to the Druid tree.
            foreach (var id in new[] { "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-ringed-hide", "bh-heartwood-brace" })
            { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var why), id + ": " + why); p.talents = next; }
            Assert.IsFalse(t.Propose(p, "tk-intercept", 1, out _, out _));
            var warrior = EncounterSession.FreshProgress();
            Assert.IsFalse(t.Validate(warrior, out _), "A Warrior save must not validate against the Druid tree.");
        }
        [Test] public void Each_class_has_its_own_save_slot_and_warrior_keeps_the_legacy_one()
        {
            Assert.AreEqual("encounter", EncounterSave.SlotFor("class.warrior"));
            Assert.AreEqual("encounter-druid", EncounterSave.SlotFor("class.druid"));
            Assert.IsTrue(Crulanda.Persistence.SaveFileStore.IsValidSlotName(EncounterSave.SlotFor("class.druid")));
        }
        [Test] public void Periodic_effects_tick_refresh_without_stacking_and_report_completion()
        {
            var fx = new PeriodicEffects(); var target = new object(); var ticks = new List<int>(); bool? completed = null;
            fx.Add("hot", target, 0, 2, 3, 5, (e, i) => { ticks.Add(e.value); return true; }, (e, done) => completed = done);
            fx.Tick(1.9f); Assert.AreEqual(0, ticks.Count);
            fx.Tick(2); Assert.AreEqual(1, ticks.Count);
            // Re-applying refreshes (old one cancelled, new one starts over); it does not add a second copy.
            fx.Add("hot", target, 2, 2, 3, 7, (e, i) => { ticks.Add(e.value); return true; }, (e, done) => completed = done);
            Assert.AreEqual(false, completed, "Refresh cancels the previous copy.");
            Assert.AreEqual(1, fx.Count);
            fx.Tick(100); // catches up missed ticks but never beyond its count
            CollectionAssert.AreEqual(new[] { 5, 7, 7, 7 }, ticks);
            Assert.AreEqual(true, completed); Assert.AreEqual(0, fx.Count);
        }
        [Test] public void Periodic_effect_can_stop_early_and_tolerates_removal_during_tick()
        {
            var fx = new PeriodicEffects(); var a = new object(); var b = new object(); int hits = 0; bool? done = null;
            fx.Add("bleed", a, 0, 1, 5, 1, (e, i) => { hits++; return i < 1; }, (e, c) => done = c);
            fx.Add("bleed", b, 0, 1, 5, 1, (e, i) => { fx.RemoveTarget(a); return true; });
            fx.Tick(10);
            Assert.LessOrEqual(hits, 2); Assert.AreEqual(false, done);
            Assert.IsFalse(fx.Has("bleed", a));
            Assert.Throws<ArgumentException>(() => fx.Add("x", a, 0, 0, 1, 1, (e, i) => true));
        }
    }
}
