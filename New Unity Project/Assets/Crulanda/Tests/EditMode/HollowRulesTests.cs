using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Hallow's Creek pick 6 (Docs/REWORK_LOWTOWNS_HALLOWS.md; Chris, 2026-10-18: "keep all"): the Hollow as Book 1 chapter 4 shows them in
    /// Oakhaven (b1 ch.N:L is D:\code\crulanda\book1\chapter_N.md at line L), HollowRules and the zone's data:
    /// - out of the cellar doors (ch.4:161): the Cellars' trapdoor camps wait on a Hollow camp's pull, and each pulled camp with its trapdoor
    ///   is the three it was before (the fight's size kept);
    /// - the shift (ch.4:109): a glide toward a foe more than a few metres off, never past it, not while held or busy;
    /// - the dust (ch.4:177): a puff only at close quarters; a blinded blow misses a quarter of the time, an unblinded one never;
    /// - the halves (ch.4:181): a split now and then, never of a half;
    /// - the well (ch.4:191-245): it raises a Hollow near it once, never a half, never once broken; it stands in the Cellars by the far stair.
    /// Only Hallow's Creek's ordinary Hollow. Pure rules and data: no scene.
    /// </summary>
    public class HollowRulesTests
    {
        static ZoneDefinition Hallows() { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", "hallows.json"))); }

        [Test] public void Only_Hallows_Creeks_ordinary_Hollow_fight_as_chapter_4_shows_them()
        {
            Assert.IsTrue(HollowRules.BookHollow("zone.hallows", "Hallow's Creek Hollow"));
            Assert.IsFalse(HollowRules.BookHollow("zone.hallows", "First-Cut"), "not the elite");
            Assert.IsFalse(HollowRules.BookHollow("zone.peaks", "Hollow Man"), "the Hollow Men elsewhere keep their settled fights");
            Assert.IsFalse(HollowRules.BookHollow("zone.lowtowns", "Hallow's Creek Hollow"));
        }
        [Test] public void They_shift_close_without_a_step()
        {
            Assert.IsTrue(HollowRules.Glides(8, false, false, 10, 9), "a foe eight metres off: it glides");
            Assert.IsFalse(HollowRules.Glides(3, false, false, 10, 9), "close: it walks the last of it");
            Assert.IsFalse(HollowRules.Glides(8, true, false, 10, 9), "not while held");
            Assert.IsFalse(HollowRules.Glides(8, false, true, 10, 9), "not while drawing up dust or casting");
            Assert.IsFalse(HollowRules.Glides(8, false, false, 10, 11), "not before its glide is ready");
            Assert.AreEqual(HollowRules.GlideReach, HollowRules.GlideStep(30), 1e-4f, "never more than its reach");
            Assert.AreEqual(8 - HollowRules.GlideStop, HollowRules.GlideStep(8), 1e-4f, "stopping short of its foe, not past it");
            Assert.Greater(HollowRules.GlideSeconds, .5f, "slow, not a jerk (the Weave-Eaters' blink is instant)");
            Assert.Greater(HollowRules.GlideEvery, WeaveEaterRules.BlinkEvery, "and rarer than a Weave-Eater's blink");
        }
        [Test] public void Their_dust_blinds_at_close_quarters_and_is_warned()
        {
            Assert.IsTrue(HollowRules.Puffs(2, false, false, 10, 9));
            Assert.IsFalse(HollowRules.Puffs(HollowRules.DustReach + .5f, false, false, 10, 9), "only at close quarters");
            Assert.IsFalse(HollowRules.Puffs(2, true, false, 10, 9), "not while held");
            Assert.GreaterOrEqual(HollowRules.DustWindup, 1, "warned on the ground for a second or more");
            Assert.IsTrue(HollowRules.Misses(true, .1f)); Assert.IsFalse(HollowRules.Misses(true, .5f), "a quarter of a blinded one's blows go wide");
            Assert.IsFalse(HollowRules.Misses(false, 0), "never an unblinded one's");
            Assert.LessOrEqual(HollowRules.BlindMiss, .25f); Assert.LessOrEqual(HollowRules.BlindSeconds, 5, "short: trash's numbers");
        }
        [Test] public void Their_halves_crawl_on_but_a_half_does_not_split()
        {
            Assert.IsTrue(HollowRules.Splits(false, false, 0));
            Assert.IsFalse(HollowRules.Splits(false, false, .99f), "now and then, not always");
            Assert.IsFalse(HollowRules.Splits(true, false, 0), "a half does not split again");
            Assert.IsFalse(HollowRules.Splits(false, true, 0), "one the well will raise lies whole");
            Assert.LessOrEqual(HollowRules.HalfHealth, .3f); Assert.LessOrEqual(HollowRules.HalfHit, .5f); Assert.Less(HollowRules.HalfPace, 1, "low, weak and slow");
            Assert.AreEqual(25, HollowRules.Share(100, HollowRules.HalfHealth));
        }
        [Test] public void The_well_raises_a_hollow_once_until_it_is_broken()
        {
            Assert.IsTrue(HollowRules.WellRaises(true, 5, false, false));
            Assert.IsFalse(HollowRules.WellRaises(false, 5, false, false), "broken: no more");
            Assert.IsFalse(HollowRules.WellRaises(true, HollowRules.WellReach + 1, false, false), "too far from it");
            Assert.IsFalse(HollowRules.WellRaises(true, 5, true, false), "once a visit each");
            Assert.IsFalse(HollowRules.WellRaises(true, 5, false, true), "never a half");
            Assert.Less(HollowRules.WellRiseHealth, .5f, "back weaker");
        }
        [Test] public void The_cellar_trapdoors_wait_on_a_pull_and_keep_the_fights_their_size()
        {
            var z = Hallows();
            var doors = z.camps.Where(c => c != null && c.after != null && c.after.StartsWith(HollowRules.PullKey)).ToList();
            Assert.GreaterOrEqual(doors.Count, 2, "Hollow up cellar trapdoors");
            foreach (var d in doors)
            {
                var pulled = z.camps.FirstOrDefault(c => c != null && c.name == d.after.Substring(HollowRules.PullKey.Length));
                Assert.NotNull(pulled, d.name + " waits on a camp that is there");
                Assert.IsTrue(string.IsNullOrEmpty(pulled.after), d.name + ": the camp it waits on is there from the start");
                Assert.AreEqual("The Cellars", d.cave); Assert.AreEqual(pulled.cave, d.cave, "in the same cellars");
                Assert.AreEqual(HollowRules.Mob, d.mob); Assert.AreEqual("hollow", d.look);
                Assert.Less(d.along, pulled.along, d.name + " comes up behind the party (nearer the way in than the camp pulled)");
                Assert.Less(pulled.along - d.along, 15, d.name + ": near enough to join the fight");
                Assert.AreEqual(3, pulled.count + d.count, d.name + ": the pulled camp and its trapdoor are the three the camp was");
                StringAssert.Contains("ch.4:161", d.canonStatus);
            }
            // Appended after the rare camps: no camp's index (its mobs' ids, its lockout) moved.
            int firstDoor = System.Array.FindIndex(z.camps, c => c != null && c.after != null && c.after.StartsWith(HollowRules.PullKey));
            Assert.IsFalse(z.camps.Skip(firstDoor).Any(c => c.rare > 0), "the trapdoor camps come after every other camp");
        }
        [Test] public void The_anchor_well_stands_in_the_cellars_by_the_far_stair()
        {
            var z = Hallows();
            var well = z.anchors.FirstOrDefault(a => a != null && a.name == HollowRules.WellAnchor); Assert.NotNull(well, "the anchor-well");
            Assert.AreEqual("The Cellars", well.cave);
            var stair = z.camps.First(c => c.name == "Hollow at the far stair");
            Assert.Less(Mathf.Abs(well.along - stair.along), HollowRules.WellReach, "the far stair's Hollow fall within its reach");
            Assert.AreEqual(2, stair.count, "two at the far stair: the well raises them (the fight about its old size of three)");
            var store = z.anchors.First(a => a.name == "the Cut-Store");
            Assert.Greater(Mathf.Abs(well.along - store.along), HollowRules.WellReach, "First-Cut's fight is out of its reach");
        }
    }
}
