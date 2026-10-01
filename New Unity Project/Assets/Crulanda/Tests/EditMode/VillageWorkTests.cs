using System.Collections.Generic;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The trades' daily schedules (<see cref="VillageWork"/>): every trade has a day that covers its working hours, and every
    /// errand runs between places a village can have, in a window, carrying a named good.</summary>
    public class VillageWorkTests
    {
        static readonly string[] Trades = { "farmer", "miller", "baker", "blacksmith", "lumberjack", "hunter", "skinner", "leatherworker", "herbalist", "merchant", "gossip", "child", "elder", "drinker", "henwife" };
        static readonly HashSet<string> Places = new HashSet<string> { "field", "well", "green", "inn", "mill", "wander", "forge", "stall", "oven", "tannery", "woodpile", "woods", "meadow", "home", "yard", "nest", "trough", "pan",
            "leathershop", "dryhut", "kitchen", "kitchendoor", "bar", "lodge" };

        [Test] public void Every_trade_has_a_day_that_covers_its_working_hours()
        {
            foreach (var role in Trades)
            {
                var day = VillageWork.DayFor(role); Assert.NotNull(day, role + " has a day.");
                for (float h = 7.5f; h < 19; h += .25f) Assert.NotNull(VillageWork.ShiftFor(role, h), role + " is somewhere at " + h);
                foreach (var s in day.shifts) { Assert.Less(s.from, s.to, role); Assert.Greater(s.places.Length, 0, role); foreach (var p in s.places) Assert.IsTrue(Places.Contains(p), role + " works at '" + p + "'"); }
            }
            Assert.IsNull(VillageWork.ShiftFor("farmer", 23), "Night: nobody's shift.");
        }

        [Test] public void Errands_run_between_known_places_in_a_window_and_carry_a_good()
        {
            int carried = 0; var goods = new HashSet<string>();
            foreach (var role in Trades)
                foreach (var e in VillageWork.DayFor(role).errands)
                {
                    Assert.IsTrue(Places.Contains(e.from), role + ": '" + e.id + "' starts at '" + e.from + "'");
                    if (e.to != null) { Assert.IsTrue(Places.Contains(e.to), role + ": '" + e.id + "' ends at '" + e.to + "'"); Assert.AreNotEqual(e.from, e.to, e.id); Assert.NotNull(e.good, e.id + " delivers a good"); goods.Add(e.to + "." + e.good); carried++; }
                    Assert.Less(e.at, e.until, e.id); Assert.GreaterOrEqual(e.at, 4.5f, e.id); Assert.LessOrEqual(e.until, 21, e.id);
                    Assert.IsTrue(e.load != Load.None || e.to == null, e.id + " shows what it carries");
                }
            Assert.GreaterOrEqual(carried, 20, "The trades are tied together by their errands.");
            foreach (var key in new[] { "mill.grain", "oven.flour", "inn.bread", "stall.bread", "forge.wood", "inn.wood", "tannery.hides", "inn.meat", "stall.herbs", "inn.herbs", "stall.goods", "inn.eggs", "stall.eggs", "home.eggs", "pan.water" })
                Assert.IsTrue(goods.Contains(key), key + " is delivered by someone");
        }

        [Test] public void The_hen_wife_feeds_at_first_light_waters_through_the_day_and_takes_her_eggs_three_ways()
        {
            var day = VillageWork.DayFor("henwife"); var ids = new List<string>(); foreach (var e in day.errands) ids.Add(e.id);
            CollectionAssert.AreEqual(new[] { "the morning feed", "eggs to the inn", "water for the hens", "the afternoon's water", "the afternoon feed", "eggs to the stall", "eggs for the pot" }, ids);
            Assert.LessOrEqual(day.errands[0].at, 6, "Feed as soon as the coop is open.");
            Assert.AreEqual("trough", day.errands[0].from); Assert.IsNull(day.errands[0].to, "Feeding is done at the trough.");
            Assert.AreEqual("well", day.errands[2].from); Assert.AreEqual("pan", day.errands[2].to); Assert.AreEqual(Load.Bucket, day.errands[2].load);
            Assert.AreEqual("inn", day.errands[1].to); Assert.AreEqual("stall", day.errands[5].to); Assert.AreEqual("home", day.errands[6].to);
            Assert.Greater(day.errands[5].at, day.errands[1].until, "The stall's eggs come after the inn's.");
            Assert.NotNull(VillageWork.Reply("eggs", .2f)); Assert.IsNull(VillageWork.Reply("water", .2f), "The hens don't answer.");
        }
    }
}
