using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The purses (tools/wip/professions/ADDENDUM.md C.3), run over Oakhaven's households from its data with no scene: with no player
    /// only the Tanners go without (bread every day, firewood every third), one worn bag keeps them warm, needs are bought in order and
    /// stop at the first the purse cannot cover, no purse passes forty, a purchase pays the seller's household, a household never buys
    /// what its own trade makes, and a claim let go frees its coin. A day here is: the morning's stipend and plan, then every claim
    /// paid at pick-up (and whatever the sellers' new coin lets them claim, until nothing more is bought).
    /// </summary>
    public class VillageEconomyTests
    {
        [TearDown] public void Reset() { VillageEconomy.ResetAll(); }

        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Zones/" + file + ".json"))); }
        /// <summary>The trade of everyone VillageLife.Init spawns in a zone, by name: villagers by index, hen-wives, residents.</summary>
        static Dictionary<string, string> Roles(ZoneDefinition z)
        {
            var roles = new Dictionary<string, string>();
            var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : VillageLife.DefaultNames;
            for (int i = 0; i < z.life.villagers; i++) roles[names[i % names.Length]] = VillageLife.DefaultRoles[i % VillageLife.DefaultRoles.Length];
            foreach (var k in VillageLife.KeeperNamesList) roles[k] = "henwife";
            foreach (var r in z.life.residents) if (r != null && !string.IsNullOrEmpty(r.name)) roles[r.name] = r.role;
            return roles;
        }
        static VillageEconomy Oakhaven(int bags = 0)
        {
            var z = Zone("oakhaven"); var roles = Roles(z); var e = new VillageEconomy();
            foreach (var h in z.life.households)
                e.Open(h.name, h.stipend, h.needs, h.members.Select(m => roles.TryGetValue(m.name, out var r) ? r : null), h.name == "Tanner" ? bags : 0);
            e.PlanAll();
            return e;
        }
        /// <summary>Everything claimed is picked up and paid for, and what the sellers can then afford too.</summary>
        static void PickUp(VillageEconomy e)
        {
            bool any;
            do { any = false; foreach (var p in e.Purses) foreach (var need in VillageEconomy.Goods) if (p.claimed.Contains(need) && e.Settle(p, need)) any = true; }
            while (any);
        }
        /// <summary>The needs a household buys that went unmet today.</summary>
        static List<string> Without(VillageEconomy e, Purse p) { return VillageEconomy.Goods.Where(n => e.Live(p, n) && !p.met.Contains(n)).ToList(); }
        /// <summary>Day 1 is the session's first (the purse starts at its stipend); each later day starts with the morning's stipend.</summary>
        static void Day(VillageEconomy e, int day) { if (day > 1) e.NewDay(); PickUp(e); }

        [Test] public void With_no_player_only_the_Tanners_go_without()
        {
            var e = Oakhaven();
            Assert.AreEqual(16, e.Purses.Count, "A purse per household.");
            Assert.AreEqual(6, e.PurseOf("Vell").stipend, "The default stipend comes through the data.");
            CollectionAssert.AreEqual(VillageEconomy.Goods, e.PurseOf("Vell").needs, "The default needs too.");
            Assert.AreEqual(3, e.PurseOf("Tanner").stipend); Assert.AreEqual(2, e.PurseOf("Jory").stipend); CollectionAssert.AreEqual(new[] { "bread" }, e.PurseOf("Jory").needs);
            int tannerCold = 0;
            for (int day = 1; day <= 30; day++)
            {
                Day(e, day);
                foreach (var p in e.Purses)
                {
                    var without = Without(e, p);
                    if (p.household == "Tanner") { if (without.Contains("firewood")) tannerCold++; Assert.IsFalse(without.Contains("bread"), "Day " + day + ": the Tanners have bread."); }
                    else Assert.IsEmpty(without, "Day " + day + ": " + p.household + " has everything it buys (coin " + p.coin + ").");
                }
            }
            Assert.AreEqual(20, tannerCold, "The Tanners go without firewood two days in three.");
            Assert.IsEmpty(e.Purses.Where(p => p.household == "The Golden Cask").SelectMany(p => VillageEconomy.Goods.Where(n => e.Live(p, n))), "The inn does not shop.");
        }

        [Test] public void The_Tanners_have_bread_daily_and_firewood_every_third_day()
        {
            var e = Oakhaven(); var tanner = e.PurseOf("Tanner");
            Assert.AreEqual(3, tanner.coin, "A purse starts at its stipend.");
            var coins = new List<int>();
            for (int day = 1; day <= 30; day++)
            {
                Day(e, day); coins.Add(tanner.coin);
                Assert.IsTrue(tanner.met.Contains("bread"), "Bread on day " + day + ".");
                Assert.AreEqual(day % 3 == 0, tanner.met.Contains("firewood"), "Firewood on day " + day + " only every third day.");
                Assert.IsFalse(tanner.met.Contains("eggs"), "No eggs on day " + day + ": firewood comes first and they never get past it.");
            }
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, coins.Take(3), "The purse at the end of days 1, 2 and 3.");
        }

        [Test] public void One_worn_bag_keeps_the_Tanners_warm_every_day()
        {
            var e = Oakhaven(1); var tanner = e.PurseOf("Tanner");
            Assert.AreEqual(5, tanner.Stipend, "A worn bag adds two a day.");
            Assert.AreEqual(5 + VillageEconomy.BagStart, tanner.coin, "and six to the session's first purse.");
            for (int day = 1; day <= 30; day++)
            {
                Day(e, day);
                Assert.IsTrue(tanner.met.Contains("bread") && tanner.met.Contains("firewood"), "Bread and firewood on day " + day + ".");
            }
            Assert.AreEqual(0, tanner.coin, "The steady state spends all five.");
            Assert.IsFalse(tanner.met.Contains("eggs"), "Eggs only while the session's first purse lasts.");
            // A restart: a fresh table, the bag still worn (it is saved), and they are warm from the first day.
            VillageEconomy.ResetAll(); e = Oakhaven(1); Day(e, 1);
            Assert.IsTrue(e.PurseOf("Tanner").met.Contains("firewood"), "Warm on the first day after a restart.");
        }

        [Test] public void Needs_are_bought_in_order_and_stop_at_the_first_unaffordable()
        {
            var e = Oakhaven(); var tanner = e.PurseOf("Tanner");
            tanner.claimed.Clear(); tanner.met.Clear(); tanner.coin = 4;
            var claimed = e.Plan(tanner);
            CollectionAssert.AreEqual(new[] { "bread" }, claimed, "Bread (2) is claimed; firewood (3) is more than the 2 left, and eggs (1) are not reached.");
            Assert.AreEqual("firewood", e.ShortOf(tanner), "Firewood is what they are short of.");
            Assert.AreEqual(2, tanner.Free);
            e.Earn(tanner, 1, true, out claimed);
            CollectionAssert.AreEqual(new[] { "firewood" }, claimed, "One more coin and firewood is next.");
            Assert.AreEqual(0, tanner.Free);
            Assert.AreEqual("eggs", e.ShortOf(tanner), "Eggs now wait on the next coin.");
            e.Earn(tanner, 1, true, out claimed);
            CollectionAssert.AreEqual(new[] { "eggs" }, claimed);
        }

        [Test] public void Stipend_then_cap_keeps_every_purse_at_or_under_forty()
        {
            var e = Oakhaven();
            for (int day = 1; day <= 60; day++)
            {
                Day(e, day);
                foreach (var p in e.Purses) { Assert.LessOrEqual(p.coin, VillageEconomy.Cap, p.household + " on day " + day); Assert.GreaterOrEqual(p.coin, 0, p.household + " on day " + day); }
            }
            foreach (var name in new[] { "Thorne", "Farrow", "Rusk", "The Golden Cask" }) Assert.AreEqual(VillageEconomy.Cap, e.PurseOf(name).coin, name + " fills its purse to the cap.");
            var cask = e.PurseOf("The Golden Cask");
            Assert.AreEqual(100, e.Earn(cask, 100, true, out _), "What is paid past the cap goes to the tithe-man.");
            Assert.AreEqual(VillageEconomy.Cap, cask.coin);
        }

        [Test] public void A_purchase_reaches_the_sellers_household()
        {
            var e = Oakhaven(); var tanner = e.PurseOf("Tanner"); var thorne = e.PurseOf("Thorne"); var farrow = e.PurseOf("Farrow"); var rusk = e.PurseOf("Rusk");
            Assert.AreSame(thorne, e.SellerOf("bread")); Assert.AreSame(farrow, e.SellerOf("firewood")); Assert.AreSame(rusk, e.SellerOf("eggs"), "Eggs are Ama Rusk's, the first merchant's.");
            tanner.claimed.Clear(); tanner.coin = 0;
            e.Earn(tanner, 12, true, out var claimed);   // the wallet's price, paid by the player
            CollectionAssert.AreEqual(VillageEconomy.Goods, claimed, "Twelve gold claims all three.");
            int t = thorne.coin, f = farrow.coin, r = rusk.coin;
            Assert.IsTrue(e.Settle(tanner, "bread")); Assert.AreEqual(t + 2, thorne.coin, "The loaf's two coppers go to the Thornes.");
            Assert.IsTrue(e.Settle(tanner, "firewood")); Assert.AreEqual(f + 3, farrow.coin, "The bundle's three to the Farrows.");
            Assert.IsTrue(e.Settle(tanner, "eggs")); Assert.AreEqual(r + 1, rusk.coin, "The eggs' one to the Rusks.");
            Assert.AreEqual(6, tanner.coin, "Twelve less six.");
            Assert.IsFalse(e.Settle(tanner, "bread"), "Bought once a day.");
        }

        [Test] public void A_household_never_buys_its_own_trade()
        {
            var e = Oakhaven();
            void Never(string household, string need) { var p = e.PurseOf(household); Assert.IsFalse(e.Live(p, need), household + " never buys " + need + "."); }
            Never("Thorne", "bread"); Never("Farrow", "firewood"); Never("Moss", "firewood"); Never("Rusk", "eggs"); Never("Reed", "eggs");
            Never("Harrow farm", "eggs"); Never("Carder farm", "eggs"); Never("Brook farm", "eggs");
            foreach (var need in VillageEconomy.Goods) Never("The Golden Cask", need);
            Assert.IsTrue(e.Live(e.PurseOf("Vell"), "bread") && e.Live(e.PurseOf("Vell"), "firewood") && e.Live(e.PurseOf("Vell"), "eggs"), "The smith buys all three.");
            for (int day = 1; day <= 10; day++)
            {
                Day(e, day);
                Assert.IsTrue(e.PurseOf("Thorne").met.Contains("bread"), "Bread counts as met for the baker.");
            }
            // A zone with no hens sells no eggs, so nobody there buys them (Khaven).
            var bare = new VillageEconomy();
            bare.Open("Grane", 6, null, new[] { "blacksmith" }); bare.Open("Jenn", 6, null, new[] { "merchant" });
            Assert.IsNull(bare.SellerOf("eggs")); Assert.IsNull(bare.SellerOf("bread")); Assert.IsNull(bare.SellerOf("firewood"));
            bare.PlanAll(); Assert.IsEmpty(bare.PurseOf("Grane").claimed, "Nothing to buy where nobody sells it."); Assert.AreEqual(3, bare.PurseOf("Grane").met.Count);
        }

        [Test] public void A_cancelled_errand_releases_its_claim()
        {
            var e = Oakhaven(); var tanner = e.PurseOf("Tanner");
            Assert.IsTrue(tanner.claimed.Contains("bread"), "Bread is claimed in the morning.");
            Assert.AreEqual(1, tanner.Free);
            e.Release(tanner, "bread");
            Assert.IsFalse(tanner.claimed.Contains("bread")); Assert.AreEqual(3, tanner.Free, "Its coin is free again.");
            Assert.IsFalse(e.Settle(tanner, "bread"), "A released claim is not paid at pick-up."); Assert.AreEqual(3, tanner.coin);
            Assert.IsFalse(tanner.met.Contains("bread"));
            CollectionAssert.AreEqual(new[] { "bread" }, e.Plan(tanner), "Planned again, it is claimed again.");
        }

        [Test] public void The_economy_is_kept_per_character_and_zone_and_forgotten_for_another()
        {
            var a = VillageEconomy.For("save|encounter", "zone.oakhaven");
            Assert.AreSame(a, VillageEconomy.For("save|encounter", "zone.oakhaven"), "Back in the zone: the same purses.");
            Assert.AreNotSame(a, VillageEconomy.For("save|encounter", "zone.khaven"), "Each zone its own.");
            a.Open("Tanner", 3, null, new[] { "leatherworker" });
            Assert.AreNotSame(a, VillageEconomy.For("save|encounter-druid", "zone.oakhaven"), "Another character: a fresh table.");
            Assert.AreEqual(0, VillageEconomy.For("save|encounter", "zone.oakhaven").Purses.Count, "and the first character's purses are gone.");
            VillageEconomy.StartingCoin = 0; var b = new VillageEconomy();
            Assert.AreEqual(0, b.Open("Vell", 6, null, new[] { "blacksmith" }).coin, "The test hook starts a purse empty.");
            VillageEconomy.ResetAll(); Assert.IsNull(VillageEconomy.StartingCoin);
            Assert.IsFalse(b.Turned(14)); Assert.IsFalse(b.Turned(18)); Assert.IsTrue(b.Turned(.2f), "Past midnight is a new day."); Assert.IsFalse(b.Turned(1));
        }

        [Test] public void Shopping_runs_in_daylight_between_known_places_and_carries_what_it_buys()
        {
            foreach (var e in VillageWork.Shopping)
            {
                Assert.IsNotNull(e.need, e.id); Assert.Greater(VillageEconomy.Price(e.need), 0, e.id + " has a price.");
                Assert.AreEqual("home", e.to, e.id + " is carried home."); Assert.IsNull(e.good, e.id + " delivers no stock.");
                Assert.AreNotEqual(Load.None, e.load, e.id + " shows what it carries.");
                Assert.Less(e.at, e.until); Assert.GreaterOrEqual(e.at, 4.5f); Assert.LessOrEqual(e.until, 21, e.id);
                Assert.IsNotNull(VillageWork.ShortLine(e.need, false), e.id + " has a line for going without.");
            }
            CollectionAssert.AreEquivalent(VillageEconomy.Goods, VillageWork.Shopping.Select(e => e.need), "One errand for each thing a household buys.");
            Assert.AreEqual("bread", VillageWork.DayFor("child").errands.Single(x => x.id == "a loaf for Mum").need, "The child's loaf is the household's bread.");
            Assert.AreEqual("Mum says there's no loaf today.", VillageWork.ShortLine("bread", true));
            Assert.LessOrEqual(VillageWork.ShopFor("firewood").at, VillageLife.ColdFrom, "Firewood can be fetched before the fire goes out.");
        }
    }
}
