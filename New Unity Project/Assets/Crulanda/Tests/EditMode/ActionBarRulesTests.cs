using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The chosen bar (Chris, 2026-10-13: ten slots, chosen; alternate advancement past the cap buys more; Docs/ACTION_BAR.md).</summary>
    public class ActionBarRulesTests
    {
        static List<(string, bool)> Book(int yours, int toCome)
        {
            var b = new List<(string, bool)>();
            for (int i = 0; i < yours; i++) b.Add(("a" + i, true));
            for (int i = 0; i < toCome; i++) b.Add(("c" + i, false));
            return b;
        }

        [Test] public void A_new_bar_is_ten_slots_yours_first_then_what_is_to_come()
        {
            var bar = ActionBarRules.Reconcile(null, Book(7, 6), 10);
            Assert.AreEqual(10, bar.Count);
            CollectionAssert.AreEqual(new[] { "a0", "a1", "a2", "a3", "a4", "a5", "a6", "c0", "c1", "c2" }, bar);
        }
        [Test] public void A_book_of_thirteen_still_makes_a_bar_of_ten()
        {
            var bar = ActionBarRules.Reconcile(new List<string>(), Book(13, 0), 10);
            Assert.AreEqual(10, bar.Count); Assert.AreEqual(10, bar.Distinct().Count());
        }
        [Test] public void The_players_choices_are_kept_and_an_unlocked_ability_takes_a_locked_slot()
        {
            var chosen = new List<string> { "a5", "a0", "c0", "a3", "", "", "", "", "", "" };
            var bar = ActionBarRules.Reconcile(chosen, Book(6, 2), 10);
            Assert.AreEqual("a5", bar[0]); Assert.AreEqual("a0", bar[1]); Assert.AreEqual("a3", bar[3], "the player's order stays");
            Assert.IsFalse(bar.Contains("c0"), "a slot holding something not yet yours gives way when there is something yours");
            CollectionAssert.IsSubsetOf(new[] { "a1", "a2", "a4" }, bar, "the empty slots fill with what is yours");
        }
        [Test] public void A_key_the_book_no_longer_has_is_emptied_and_refilled()
        {
            var bar = ActionBarRules.Reconcile(new List<string> { "gone", "a0" }, Book(3, 0), 10);
            Assert.IsFalse(bar.Contains("gone")); CollectionAssert.IsSubsetOf(new[] { "a0", "a1", "a2" }, bar);
        }
        [Test] public void Placing_an_ability_already_on_the_bar_swaps_the_two_slots()
        {
            var bar = new List<string> { "a0", "a1", "a2" };
            ActionBarRules.Place(bar, 0, "a2");
            CollectionAssert.AreEqual(new[] { "a2", "a1", "a0" }, bar);
            ActionBarRules.Place(bar, 1, "a9");
            CollectionAssert.AreEqual(new[] { "a2", "a9", "a0" }, bar);
        }
        [Test] public void Alternate_advancement_starts_at_the_cap_and_slots_cost_one_two_three_four()
        {
            int cap = EncounterProgress.XpForLevel(EncounterProgress.LevelCap), per = ActionBarRules.AaPointXp;
            Assert.AreEqual(0, ActionBarRules.AaEarned(cap - 1)); Assert.AreEqual(0, ActionBarRules.AaEarned(cap));
            Assert.AreEqual(1, ActionBarRules.AaEarned(cap + per)); Assert.AreEqual(10, ActionBarRules.AaEarned(cap + 10 * per));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, Enumerable.Range(1, 4).Select(ActionBarRules.SlotCost));
            Assert.AreEqual(10, ActionBarRules.SpentOn(4)); Assert.AreEqual(3, ActionBarRules.SpentOn(2));
            Assert.AreEqual(14, ActionBarRules.Slots(4)); Assert.AreEqual(14, ActionBarRules.Slots(9), "never more than four extra");
            Assert.AreEqual(2, ActionBarRules.AaAvailable(cap + 5 * per, 2), "five earned, three spent on two slots");
        }
        [Test] public void Only_a_level_or_a_talent_still_to_come_counts_as_not_yours()
        {
            Assert.IsTrue(ActionBarRules.NotYetYours("Level 6")); Assert.IsTrue(ActionBarRules.NotYetYours("Talent"));
            Assert.IsFalse(ActionBarRules.NotYetYours(null)); Assert.IsFalse(ActionBarRules.NotYetYours("Active")); Assert.IsFalse(ActionBarRules.NotYetYours("Heat 30"));
        }
    }
}
