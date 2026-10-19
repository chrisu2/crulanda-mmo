using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The sims' choices in the Unraveller's fight (Docs/WORLD_BOSS_1.md 3.5; RaidJobs, GroundMarks), each without a scene: who throws the
    /// harpoon and when, the answer to the Feeding (a kick, else a fresh harpoon), the tank swap at four Unravel stacks, the three salts on the
    /// swallowed (two sims and you when you are near), who takes the brood, the mistakes on purpose, and the step out of marked ground (any
    /// mark the game draws, shared with the dungeons). Today's sims do none of it: no sim throws, none leaves a mark.
    /// </summary>
    public class RaidJobsTests
    {
        static UnravellerTuning T { get { return new UnravellerTuning(); } }

        [Test] public void A_harpooner_throws_on_its_turn_as_her_pin_runs_out()
        {
            float lead = T.harpoonLead;
            Assert.IsTrue(RaidJobs.HarpoonNow(true, 2, true, true, lead), "its turn, 2 s left: throw");
            Assert.IsTrue(RaidJobs.HarpoonNow(true, 0, true, true, lead), "she is free: throw");
            Assert.IsFalse(RaidJobs.HarpoonNow(true, 5, true, true, lead), "5 s left: not yet (a harpoon wasted)");
            Assert.IsFalse(RaidJobs.HarpoonNow(false, 0, true, true, lead), "not its turn");
            Assert.IsFalse(RaidJobs.HarpoonNow(true, 0, false, true, lead), "no harpoon");
            Assert.IsFalse(RaidJobs.HarpoonNow(true, 0, true, false, lead), "out of throwing reach");
        }

        [Test] public void The_Feeding_is_kicked_and_a_harpoon_breaks_it_when_the_kick_will_not_do()
        {
            Assert.AreEqual(RaidJobs.Answer.Interrupt, RaidJobs.FeedingAnswer(true, true, false, true), "the kick on turn first");
            Assert.AreEqual(RaidJobs.Answer.Harpoon, RaidJobs.FeedingAnswer(true, true, true, true), "she is immune (a kick within 10 s): a fresh harpoon");
            Assert.AreEqual(RaidJobs.Answer.Harpoon, RaidJobs.FeedingAnswer(true, false, false, true), "the kicker's kick is spent: a harpoon");
            Assert.AreEqual(RaidJobs.Answer.None, RaidJobs.FeedingAnswer(true, false, false, false), "nobody can: she feeds");
            Assert.AreEqual(RaidJobs.Answer.None, RaidJobs.FeedingAnswer(false, true, false, true), "no Feeding, nothing to answer");
        }

        [Test] public void The_tanks_swap_at_four_stacks()
        {
            // Run 1, with the file's Unravel of 10 s: the holder's stacks fall off while the other holds her, so the swap goes round.
            var t = T; var u = new UnravelStacks(); float now = 0; string holder = "a", other = "b"; int swaps = 0, most = 0;
            for (int blow = 0; blow < 60; blow++, now += t.swingEvery)
            {
                int n = u.Add(holder, now, t); most = Mathf.Max(most, n);
                if (RaidJobs.TauntHer(u.Count(other, now), n, false, true, false, t.swapAt, t.erasedAt)) { var x = holder; holder = other; other = x; swaps++; }
            }
            Assert.Less(most, t.erasedAt, "two tanks swapping at four: nobody is erased in two minutes");
            Assert.Greater(swaps, 6, "the swap goes round");
            int swap = T.swapAt, erased = T.erasedAt;
            Assert.IsFalse(RaidJobs.TauntHer(4, 0, true, false, true, swap, erased), "never off itself");
            Assert.IsTrue(RaidJobs.TauntHer(1, 4, false, true, false, swap, erased), "the other tank has four, this one one: taunt");
            Assert.IsFalse(RaidJobs.TauntHer(0, 3, false, true, false, swap, erased), "three: not yet");
            Assert.IsFalse(RaidJobs.TauntHer(4, 4, false, true, false, swap, erased), "as many as the other: let them fall off first");
            Assert.IsTrue(RaidJobs.TauntHer(5, 6, false, true, false, swap, erased), "the other one blow from erased: take her");
            Assert.IsTrue(RaidJobs.TauntHer(0, 0, false, false, true, swap, erased), "on a healer: the tank meant to hold her takes her back");
            Assert.IsFalse(RaidJobs.TauntHer(0, 0, false, false, false, swap, erased), "and only that one (the other is not dragged off its brood)");
        }

        [Test] public void The_three_nearest_with_salt_throw_and_you_are_told_when_you_are_one()
        {
            var near = new List<(string, float, bool, bool)> { ("a", 6, true, false), ("b", 3, true, false), ("you", 4, true, true), ("c", 2, false, false), ("d", 9, true, false) };
            var (sims, you) = RaidJobs.SaltThrowers(near, 3);
            Assert.IsTrue(you, "you are one of the three nearest with salt: the warning tells you");
            CollectionAssert.AreEqual(new[] { "b", "a" }, sims, "two sims throw; c carries none");
            near[2] = ("you", 20, true, true);
            (sims, you) = RaidJobs.SaltThrowers(near, 3);
            Assert.IsFalse(you); CollectionAssert.AreEqual(new[] { "b", "a", "d" }, sims, "you far off: three sims");
            (sims, _) = RaidJobs.SaltThrowers(near, 1);
            CollectionAssert.AreEqual(new[] { "b" }, sims, "two salted already: one more");
        }

        [Test] public void The_brood_are_killed_in_order_the_free_one_then_Cross_then_Moon()
        {
            var order = RaidJobs.KillOrder(new List<(string, bool, RaidMark, float)> {
                ("moon", true, RaidMark.Moon, 4), ("cross", true, RaidMark.Cross, 12), ("free", false, RaidMark.None, 0), ("held", true, RaidMark.None, 2), ("cross2", true, RaidMark.Cross, 6) });
            CollectionAssert.AreEqual(new[] { "free", "cross2", "cross", "moon", "held" }, order, "the free one (on the healers), then Cross (soonest to break first), Moon, the rest");
            CollectionAssert.IsEmpty(RaidJobs.KillOrder(null));
        }

        [Test] public void The_Shaman_raises_once_a_fight_the_tank_first_never_in_the_danger()
        {
            // Play note 272, Chris: "the shaman needs 1 in raid resurection".
            var w = new RezCharge(); Assert.IsTrue(w.Ready); Assert.IsTrue(w.Spend(), "once"); Assert.IsFalse(w.Spend(), "not twice in a fight");
            w.Reset(); Assert.IsTrue(w.Spend(), "ready again after the fight (or the wipe)");
            var t = SimCompanion.PartyRole.Tank; var h = SimCompanion.PartyRole.Healer; var d = SimCompanion.PartyRole.Damage;
            var fallen = new List<(string, SimCompanion.PartyRole, bool, bool, float)> { ("rogue", d, true, false, 5), ("cleric", h, true, false, 20), ("warrior", t, true, false, 28), ("you", t, false, false, 2) };
            Assert.AreEqual("warrior", RaidJobs.RezPick(fallen, 30), "a tank first");
            fallen[2] = ("warrior", t, true, true, 28);
            Assert.AreEqual("cleric", RaidJobs.RezPick(fallen, 30), "not one lying in the danger: then a healer");
            Assert.AreEqual("rogue", RaidJobs.RezPick(fallen, 10), "out of reach left: then damage");
            Assert.IsNull(RaidJobs.RezPick(new List<(string, SimCompanion.PartyRole, bool, bool, float)> { ("you", t, false, false, 1) }, 30), "nobody fallen");
            CollectionAssert.Contains(ShamanKit.BarIds, "shaman.barrow_wake", "on the Shaman's bar (slot 13, from level 10, no talent)");
        }

        [Test] public void The_raids_control_holds_the_brood_the_Archivist_first()
        {
            var d = SimCompanion.PartyRole.Damage;
            Assert.AreEqual(0, RaidJobs.BroodHolder("class.archivist", d), "the Archivist's raid part: holding the brood");
            Assert.Less(RaidJobs.BroodHolder("class.rogue", d), RaidJobs.BroodHolder("class.mage", d));
            Assert.GreaterOrEqual(RaidJobs.BroodHolder("class.scoundrel", d), 0);
            Assert.AreEqual(-1, RaidJobs.BroodHolder("class.ranger", d), "the Ranger shoots");
            Assert.AreEqual(-1, RaidJobs.BroodHolder("class.druid", SimCompanion.PartyRole.Healer), "a healing Druid heals");
            Assert.AreEqual(-1, RaidJobs.BroodHolder("class.archivist", SimCompanion.PartyRole.Tank));
        }

        [Test] public void The_off_tank_and_the_damage_dealers_take_the_brood_the_main_tank_keeps_her()
        {
            Assert.IsTrue(RaidJobs.TakesBrood(SimCompanion.PartyRole.Tank, 0, false, true), "the tank not holding her");
            Assert.IsFalse(RaidJobs.TakesBrood(SimCompanion.PartyRole.Tank, 1, true, true), "the tank holding her keeps her");
            Assert.IsTrue(RaidJobs.TakesBrood(SimCompanion.PartyRole.Damage, 1, false, false), "Party 2's damage from range");
            Assert.IsTrue(RaidJobs.TakesBrood(SimCompanion.PartyRole.Damage, 1, false, true), "Party 2's melee too, one brood at a time in the kill order (raid 5)");
            Assert.IsTrue(RaidJobs.TakesBrood(SimCompanion.PartyRole.Damage, 0, false, false), "every damage-dealer on the adds while a wave is up (raid 5: \"off tank and dps kill adds\")");
            Assert.IsFalse(RaidJobs.TakesBrood(SimCompanion.PartyRole.Healer, 1, false, false), "healers heal");
        }

        [Test] public void A_careless_eighteen_misses_more_than_a_careful_twenty()
        {
            var t = T;
            Assert.IsTrue(RaidJobs.Misses(18, .5f, .1f, t), "at 18 one time in eight or so");
            Assert.IsFalse(RaidJobs.Misses(20, .5f, .1f, t), "at 20 one time in twenty-five");
            Assert.IsTrue(RaidJobs.Misses(20, .9f, .09f, t), "a rash one at 20 more often");
            int miss18 = 0, miss20 = 0; var rng = new System.Random(5);
            for (int i = 0; i < 4000; i++) { float r = (float)rng.NextDouble(); if (RaidJobs.Misses(18, .9f, r, t)) miss18++; if (RaidJobs.Misses(20, .2f, r, t)) miss20++; }
            Assert.Greater(miss18, 2 * miss20, "the careless eighteen misses well over twice as often");
        }

        [Test] public void A_sim_in_marked_ground_steps_to_the_nearest_clear_spot()
        {
            var marks = new List<(Vector2 c, float r)> { (new Vector2(0, 0), 5) };
            Assert.AreEqual(0, GroundMarks.Inside(new Vector2(1, 0), marks), "in the hunt's five metres");
            Assert.AreEqual(-1, GroundMarks.Inside(new Vector2(6, 0), marks), "out of it");
            Assert.IsNull(GroundMarks.ClearSpot(new Vector2(6, 0), marks), "nothing to do out of it");
            var to = GroundMarks.ClearSpot(new Vector2(1, 0), marks); Assert.IsTrue(to.HasValue);
            Assert.Greater(to.Value.magnitude, 5, "past the edge"); Assert.Greater(to.Value.x, 0, "straight away from the middle");
            // Two marks side by side (her unmade ground): the spot is out of both.
            marks.Add((new Vector2(8, 0), 4));
            to = GroundMarks.ClearSpot(new Vector2(3, 0), marks); Assert.IsTrue(to.HasValue);
            Assert.AreEqual(-1, GroundMarks.Inside(to.Value, marks), "clear of both");
            // The way straight out is not walkable (the river's deep water, a wall): it turns.
            to = GroundMarks.ClearSpot(new Vector2(1, 0), marks, p => p.y > 1);
            Assert.IsTrue(to.HasValue); Assert.Greater(to.Value.y, 1, "a walkable spot"); Assert.AreEqual(-1, GroundMarks.Inside(to.Value, marks));
            Assert.IsNull(GroundMarks.ClearSpot(new Vector2(1, 0), marks, p => false), "nowhere clear: it stays");
            Assert.AreEqual(-1, GroundMarks.Inside(new Vector2(4.8f, 0), new List<(Vector2, float)> { (Vector2.zero, 5) }), "on the edge's last hand's breadth is not in it");
        }
    }
}
