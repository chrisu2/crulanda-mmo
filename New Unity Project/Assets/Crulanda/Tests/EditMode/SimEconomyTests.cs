using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The sims' trades and purse (Phase 5.3c): goods sold for their values, upgrades bought, a smith forging and wearing its own piece, the gear list honouring it.</summary>
    public class SimEconomyTests
    {
        static ItemDatabase Items() { var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items"); return ItemDatabase.Parse(Directory.GetFiles(dir, "*.json").Select(File.ReadAllText).ToList()); }
        static ProfessionDatabase Professions() { return ProfessionDatabase.Parse(new[] { File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Professions", "professions.json")) }, Items()); }
        static SimAdventurer Sim(string classId, int level) { return new SimAdventurer { id = "sim.t", name = "Test", classId = classId, level = level, gearSeed = 42 }; }

        [Test] public void Trades_follow_the_class_and_goods_sell_for_their_values()
        {
            Assert.AreEqual("mining", SimEconomy.GatherTrade("class.warrior")); Assert.AreEqual("herbalism", SimEconomy.GatherTrade("class.mage")); Assert.AreEqual("woodcutting", SimEconomy.GatherTrade("class.ranger"));
            Assert.AreEqual("blacksmithing", SimEconomy.CraftTrade("class.paladin")); Assert.AreEqual("alchemy", SimEconomy.CraftTrade("class.druid")); Assert.IsNull(SimEconomy.CraftTrade("class.ranger"));
            var items = Items(); var s = Sim("class.warrior", 3);
            SimEconomy.Add(s, "mat.copper_ore", 5); SimEconomy.Add(s, "mat.copper_ore", 2); SimEconomy.Add(s, "mat.oak_log", 1);
            Assert.AreEqual(7, SimEconomy.Count(s, "mat.copper_ore")); Assert.AreEqual(8, SimEconomy.GoodsCount(s));
            int coin = SimEconomy.SellAll(s, items);
            Assert.AreEqual(7 * items.Get("mat.copper_ore").value + items.Get("mat.oak_log").value, coin); Assert.AreEqual(coin, s.coin); Assert.AreEqual(0, SimEconomy.GoodsCount(s));
        }
        [Test] public void Coin_buys_better_gear_up_to_a_point()
        {
            var s = Sim("class.ranger", 4);
            Assert.IsFalse(SimEconomy.CanUpgrade(s)); s.coin = SimEconomy.UpgradeCost(s);
            Assert.IsTrue(SimEconomy.Upgrade(s)); Assert.AreEqual(1, s.gearBonus); Assert.AreEqual(0, s.coin);
            s.coin = 100000; Assert.IsTrue(SimEconomy.Upgrade(s)); Assert.IsFalse(SimEconomy.Upgrade(s), "two steps at most"); Assert.AreEqual(SimEconomy.MaxGearBonus, s.gearBonus);
            var items = Items(); var plain = Sim("class.ranger", 4); var rich = Sim("class.ranger", 4); rich.gearBonus = 2;
            int Q(SimAdventurer x) { return SimGear.For(x, items).Sum(id => items.Get(id).quality); }
            Assert.GreaterOrEqual(Q(rich), Q(plain), "bought upgrades never make the gear worse");
        }
        [Test] public void A_smith_forges_a_cudgel_from_bars_and_wears_it()
        {
            var items = Items(); var db = Professions(); var s = Sim("class.warrior", 2);
            Assert.IsNull(SimEconomy.Craftable(s, db, items), "nothing in hand, nothing to make");
            SimEconomy.Add(s, "mat.copper_ore", 4); s.coin = 10;
            var bar = SimEconomy.Craftable(s, db, items); Assert.NotNull(bar); Assert.AreEqual("mat.copper_bar", bar.output, "a bar first (charcoal bought from the merchant)");
            SimEconomy.Craft(s, bar, items); Assert.AreEqual(1, SimEconomy.Count(s, "mat.copper_bar")); Assert.Less(s.coin, 10, "the charcoal was paid for");
            SimEconomy.Add(s, "mat.copper_bar", 1); SimEconomy.Add(s, "mat.oak_log", 1);
            var cudgel = SimEconomy.Craftable(s, db, items); Assert.NotNull(cudgel); Assert.AreEqual("craft.copper_cudgel", cudgel.output, "gear before another bar");
            string made = SimEconomy.Craft(s, cudgel, items); Assert.AreEqual("craft.copper_cudgel", made);
            Assert.AreEqual("craft.copper_cudgel", SimEconomy.Worn(s, "mainhand")); Assert.AreEqual(0, SimEconomy.Count(s, "mat.copper_bar"));
            CollectionAssert.Contains(SimGear.For(s, items), "craft.copper_cudgel", "worn in its slot");
            Assert.AreNotEqual("craft.copper_cudgel", SimEconomy.Craftable(s, db, items)?.output, "not forged again while worn");
        }
        [Test] public void The_unseen_live_on_by_the_clock()
        {
            var items = Items(); var db = Professions();
            var bold = new SimAdventurer { id = "sim.b", name = "Bold", classId = "class.warrior", level = 3, bold = 1, variant = 5 };
            var careful = new SimAdventurer { id = "sim.c", name = "Careful", classId = "class.mage", level = 3, bold = 0, variant = 9 };
            SimPopulation.LiveAway(bold, 8, items, db); SimPopulation.LiveAway(careful, 8, items, db);   // the first look only marks the hour
            Assert.AreEqual(3, bold.level); Assert.AreEqual(0, SimEconomy.GoodsCount(careful));
            for (int hour = 9; hour <= 20; hour++) { SimPopulation.LiveAway(bold, hour, items, db); SimPopulation.LiveAway(careful, hour, items, db); }
            Assert.Greater(bold.experience, EncounterProgress.XpForLevel(3), "the bold hunted"); Assert.GreaterOrEqual(bold.level, 4, "and levelled over a day");
            Assert.Greater(careful.coin + SimEconomy.GoodsCount(careful), 0, "the careful gathered and sold");
            var idle = new SimAdventurer { id = "sim.i", name = "Idle", classId = "class.ranger", level = 2, variant = 1 };
            SimPopulation.LiveAway(idle, 3, items, db); SimPopulation.LiveAway(idle, 3.5f, items, db);
            Assert.AreEqual(0, idle.experience, "less than an hour: nothing yet");
        }
        [Test] public void Gathering_follows_the_trade_and_the_skill()
        {
            var db = Professions(); var low = Sim("class.paladin", 1); var high = Sim("class.paladin", 5);
            Assert.IsTrue(SimEconomy.Gathers(low, db.Node("node.copper"))); Assert.IsFalse(SimEconomy.Gathers(low, db.Node("node.bogiron")), "skill 20 needs level 3");
            Assert.IsTrue(SimEconomy.Gathers(high, db.Node("node.bogiron"))); Assert.IsFalse(SimEconomy.Gathers(high, db.Node("node.yarrow")), "a paladin mines, not picks");
        }
    }
}
