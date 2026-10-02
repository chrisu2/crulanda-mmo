using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The trades' daily schedules (<see cref="VillageWork"/>): every trade has a day that covers its working hours, and every
    /// errand runs between places a village can have, in a window, carrying a named good.</summary>
    public class VillageWorkTests
    {
        static readonly string[] Trades = { "farmer", "miller", "baker", "blacksmith", "lumberjack", "hunter", "skinner", "leatherworker", "herbalist", "merchant", "gossip", "child", "elder", "drinker", "henwife", "innkeeper" };
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
            foreach (var key in new[] { "mill.grain", "oven.flour", "inn.bread", "stall.bread", "forge.wood", "inn.wood", "tannery.hides", "inn.meat", "stall.herbs", "inn.herbs", "stall.goods", "inn.eggs", "stall.eggs", "home.eggs", "pan.water",
                "kitchen.water", "inn.dinner", "leathershop.leather", "dryhut.herbs" })
                Assert.IsTrue(goods.Contains(key), key + " is delivered by someone");
        }

        [Test] public void The_innkeeper_has_a_day_from_first_light_to_the_last_table()
        {
            Assert.AreEqual("Innkeeper", VillageLife.TitleFor("innkeeper"));
            var day = VillageWork.DayFor("innkeeper"); Assert.NotNull(day);
            // Up before the hen-wives' eggs, abed after the last table: a shift for every hour between, none after.
            Assert.Less(VillageWork.InnkeeperUp, 5.7f, "Up before the hen-wives."); Assert.Greater(VillageWork.InnkeeperBed, 22.5f, "Abed after the last table.");
            Assert.Less(VillageWork.InnkeeperBed, 23.5f, "Abed by 23:30.");
            for (float h = VillageWork.InnkeeperUp; h < VillageWork.InnkeeperBed - .01f; h += .1f) Assert.NotNull(VillageWork.ShiftFor("innkeeper", h), "Hob is somewhere at " + h);
            Assert.IsNull(VillageWork.ShiftFor("innkeeper", VillageWork.InnkeeperBed + .05f), "Abed.");
            Assert.IsNull(VillageWork.ShiftFor("innkeeper", 3), "Abed at night.");
            CollectionAssert.AreEqual(new[] { "kitchen" }, VillageWork.ShiftFor("innkeeper", 6).places, "The kitchen at first light.");
            Assert.Greater(VillageWork.ShiftFor("innkeeper", 19).places.Count(p => p == "bar"), 1, "The bar through the evening.");
            Assert.IsTrue(day.shifts.All(s => s.places.All(p => p == "kitchen" || p == "bar" || p == "well")), "Kitchen, bar and the well; never the green or a seat.");
            var ids = day.errands.Select(e => e.id).ToArray();
            CollectionAssert.AreEqual(new[] { "water for the pot", "the pot on", "dinner to the tables" }, ids);
            var water = day.errands[0]; Assert.AreEqual("well", water.from); Assert.AreEqual("kitchen", water.to); Assert.AreEqual(Load.Bucket, water.load);
            var pot = day.errands[1]; Assert.AreEqual("kitchen", pot.from); Assert.IsNull(pot.to, "The pot is put on in the kitchen."); Assert.AreEqual(14f, pot.work);
            var dinner = day.errands[2]; Assert.AreEqual("kitchen", dinner.from); Assert.AreEqual("inn", dinner.to); Assert.AreEqual("dinner", dinner.good);
            Assert.NotNull(VillageWork.Reply("dinner", .2f), "Somebody at the tables answers.");
            // The deliveries to the inn go round the back to the kitchen's door; the cask goes behind the bar. Stock keys are unchanged.
            foreach (var (role, id) in new[] { ("henwife", "eggs to the inn"), ("baker", "first loaves to the inn"), ("hunter", "hares to the inn") })
            {
                var e = VillageWork.DayFor(role).errands.Single(x => x.id == id);
                Assert.AreEqual(VillageWork.KitchenDoor, e.door, id + " goes to the kitchen's door."); Assert.AreEqual("inn", e.to, id + " still stocks the inn.");
            }
            var cask = VillageWork.DayFor("merchant").errands.Single(x => x.id == "a cask for the inn");
            Assert.AreEqual(VillageWork.Bar, cask.door); Assert.AreEqual("inn", cask.to); Assert.AreEqual("ale", cask.good);
            // The trades' wares go to a merchant's stall; the baker takes her loaves to her own.
            foreach (var (role, id) in new[] { ("miller", "flour to the stall"), ("blacksmith", "ironwork to the stall"), ("leatherworker", "leather to the stall"), ("herbalist", "herbs to the stall"), ("henwife", "eggs to the stall") })
                Assert.AreEqual("merchant", VillageWork.DayFor(role).errands.Single(x => x.id == id).toRole, id);
            Assert.IsNull(VillageWork.DayFor("baker").errands.Single(x => x.id == "loaves to the stall").toRole, "The baker's own stall.");
            // The leatherworker fetches the tanned hides from the yard to her shop before she opens; the herbalist hangs her herbs at her hut.
            var hides = VillageWork.DayFor("leatherworker").errands[0];
            Assert.AreEqual("tanned hides from the yard", hides.id); Assert.AreEqual("tannery", hides.from); Assert.AreEqual("leathershop", hides.to); Assert.LessOrEqual(hides.until, 9, "Before the counter opens.");
            Assert.AreEqual("dryhut", VillageWork.DayFor("herbalist").errands.Single(x => x.id == "herbs to dry").to);
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

        [Test] public void A_hunter_without_a_lodge_keeps_the_inn_and_the_green()
        {
            var dawn = VillageWork.ShiftFor("hunter", 7); var evening = VillageWork.ShiftFor("hunter", 19);
            CollectionAssert.Contains(VillageWork.PlacesFor(dawn, "hunter", true), "lodge"); CollectionAssert.Contains(VillageWork.PlacesFor(evening, "hunter", true), "lodge");
            CollectionAssert.AreEqual(new[] { "woods", "woods", "woods", "meadow", "meadow" }, VillageWork.PlacesFor(dawn, "hunter", false), "No lodge to pass by at dawn.");
            CollectionAssert.AreEqual(new[] { "inn", "inn", "green" }, VillageWork.PlacesFor(evening, "hunter", false), "The evening at the inn and on the green.");
            var smith = VillageWork.ShiftFor("blacksmith", 10); Assert.AreSame(smith.places, VillageWork.PlacesFor(smith, "blacksmith", false), "Other trades are untouched.");
            Assert.IsNull(VillageWork.PlacesFor(null, "hunter", false));
        }
    }
}
