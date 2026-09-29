using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>Items: content validates, generated gear is stable, and bags/equipment/merchants behave.</summary>
    public class ItemTests
    {
        static ItemDatabase Db()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return ItemDatabase.Parse(texts);
        }

        [Test] public void Content_is_valid_and_generated_gear_is_deterministic()
        {
            var db = Db();
            Assert.NotNull(db.Get("item.training_blade")); Assert.AreEqual("mainhand", db.Get("item.training_blade").slot);
            string id = ItemDatabase.GearId("chest", 6, 2, 1234);
            var a = db.Get(id); var b = db.Get(id);
            Assert.NotNull(a); Assert.AreSame(a, b); Assert.AreEqual("chest", a.slot); Assert.Greater(a.armor, 0);
            StringAssert.Contains(" of ", a.name, "Uncommon gear has a suffix and attributes.");
            Assert.Greater(db.Get(ItemDatabase.GearId("mainhand", 9, 3, 7)).weaponDamage, db.Get(ItemDatabase.GearId("mainhand", 2, 1, 7)).weaponDamage);
            Assert.IsNull(db.Get("gen.nonsense"));
        }

        [Test] public void Bags_stack_move_and_fill_up()
        {
            var db = Db(); var p = EncounterSession.FreshProgress();
            Assert.AreEqual(Inventory.BagSize, p.bag.Count);
            Assert.AreEqual(0, Inventory.Add(p, db, "junk.wolf_pelt", 12));
            Assert.AreEqual(12, Inventory.Count(p, "junk.wolf_pelt")); Assert.AreEqual(Inventory.BagSize - 2, Inventory.FreeSlots(p), "10 + 2 in two stacks.");
            Inventory.Move(p, db, 1, 0); Assert.AreEqual(10, p.bag[0].count, "Merging tops up the full stack only.");
            Inventory.Move(p, db, 1, 5); Assert.IsTrue(p.bag[1].Empty); Assert.AreEqual(2, p.bag[5].count, "Moving to an empty slot relocates.");
            for (int i = 0; i < 40; i++) Inventory.Add(p, db, ItemDatabase.GearId("feet", 2, 1, i), 1);
            Assert.AreEqual(0, Inventory.FreeSlots(p));
            Assert.AreEqual(1, Inventory.Add(p, db, "potion.minor", 1), "A full bag refuses more.");
        }

        [Test] public void Equip_checks_level_swaps_back_and_totals_feed_stats()
        {
            var db = Db(); var p = EncounterSession.FreshProgress();
            string boots = ItemDatabase.GearId("feet", 5, 2, 3), better = ItemDatabase.GearId("feet", 6, 3, 4);
            Inventory.Add(p, db, boots, 1); Inventory.Add(p, db, better, 1);
            Assert.IsFalse(Inventory.Equip(p, db, 0, 1, out var why)); StringAssert.Contains("Requires level", why);
            Assert.IsTrue(Inventory.Equip(p, db, 0, 10, out _)); Assert.AreEqual(boots, p.equipment[(int)EquipSlot.Feet].item); Assert.IsTrue(p.bag[0].Empty);
            Assert.IsTrue(Inventory.Equip(p, db, 1, 10, out _)); Assert.AreEqual(better, p.equipment[(int)EquipSlot.Feet].item);
            Assert.AreEqual(boots, p.bag[1].item, "The old boots go back where the new ones came from.");
            Assert.AreEqual(db.Get(better).armor, Inventory.Totals(p, db).armor);
            Inventory.Add(p, db, "junk.boar_tusk", 1);
            Assert.IsFalse(Inventory.Equip(p, db, p.bag.FindIndex(s => s.item == "junk.boar_tusk"), 10, out why), "Junk can't be worn.");
            Assert.IsTrue(Inventory.Unequip(p, (int)EquipSlot.Feet)); Assert.IsTrue(p.equipment[(int)EquipSlot.Feet].Empty);
        }

        [Test] public void Merchants_buy_sell_and_stock_by_trade()
        {
            var db = Db(); var p = EncounterSession.FreshProgress(); p.gold = 20;
            var stock = db.StockFor("Ama Rusk", "merchant", 2);
            Assert.Contains("potion.minor", stock); Assert.IsTrue(stock.Exists(i => i.StartsWith("gen.")), "General merchants carry zone gear.");
            Assert.IsEmpty(db.StockFor("Quill", "stranger", 2), "Not everyone sells.");
            Assert.IsTrue(Inventory.Buy(p, db, "potion.minor", out _)); Assert.AreEqual(20 - Inventory.Price(db.Get("potion.minor")), p.gold);
            Inventory.Add(p, db, "junk.static_glass", 3);
            int before = p.gold; int i = p.bag.FindIndex(s => s.item == "junk.static_glass");
            Assert.AreEqual(3 * db.Get("junk.static_glass").value, Inventory.Sell(p, db, i)); Assert.AreEqual(before + 24, p.gold); Assert.IsTrue(p.bag[i].Empty);
            p.gold = 0; Assert.IsFalse(Inventory.Buy(p, db, "potion.healing", out var why)); StringAssert.Contains("gold", why);
        }

        [Test] public void Loot_rolls_use_the_kind_and_elites_drop_gear()
        {
            var db = Db(); var rng = new System.Random(5); int gear = 0, pelts = 0;
            for (int n = 0; n < 200; n++)
                foreach (var (item, count) in db.RollLoot("wolf", 2, false, rng)) { if (item.StartsWith("gen.")) gear++; if (item == "junk.wolf_pelt") pelts++; }
            Assert.Greater(pelts, 100); Assert.Greater(gear, 5); Assert.Less(gear, 60);
            int eliteGear = 0;
            for (int n = 0; n < 100; n++) foreach (var (item, _) in db.RollLoot("boar", 5, true, rng)) if (item.StartsWith("gen.")) eliteGear++;
            Assert.Greater(eliteGear, 50);
        }

        [Test] public void Old_saves_move_their_items_into_bags_and_the_main_hand()
        {
            var root = Path.Combine(Path.GetTempPath(), "Crulanda-items-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var p = EncounterSession.FreshProgress(); p.bag = new List<ItemStack>(); p.equipment = new List<ItemStack>();
                p.inventory.Add("item.training_blade"); p.inventory.Add("old.trinket"); p.equippedItem = "item.training_blade";
                var store = new Crulanda.Persistence.SaveFileStore(root);
                store.Write("encounter", new Crulanda.Persistence.SaveEnvelope { formatVersion = 5, payloadType = "CrulandaEncounter", payloadJson = JsonUtility.ToJson(p) });
                Assert.IsTrue(new EncounterSave(root, TestTalents.Warrior()).Read(out var loaded, out var msg), msg);
                Assert.AreEqual("item.training_blade", loaded.equipment[(int)EquipSlot.MainHand].item);
                Assert.AreEqual(1, Inventory.Count(loaded, "old.trinket")); Assert.IsEmpty(loaded.inventory); Assert.IsNull(loaded.equippedItem);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
