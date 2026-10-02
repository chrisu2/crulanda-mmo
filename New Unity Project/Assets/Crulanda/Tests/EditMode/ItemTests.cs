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

        [Test] public void SellJunk_KeepsMaterials()
        {
            var db = Db(); var p = EncounterSession.FreshProgress();
            string worn = ItemDatabase.GearId("feet", 2, 0, 1), sound = ItemDatabase.GearId("feet", 2, 1, 1);
            Inventory.Add(p, db, "junk.wolf_fang", 3); Inventory.Add(p, db, "mat.copper_ore", 5); Inventory.Add(p, db, "mat.yarrow", 2);
            Inventory.Add(p, db, "mat.charcoal", 1); Inventory.Add(p, db, "tool.pick", 1); Inventory.Add(p, db, worn, 1); Inventory.Add(p, db, sound, 1); Inventory.Add(p, db, "potion.minor", 2);
            int expected = 3 * db.Get("junk.wolf_fang").value + db.Get(worn).value;
            Assert.AreEqual(expected, Inventory.SellJunk(p, db)); Assert.AreEqual(expected, p.gold);
            Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_fang")); Assert.AreEqual(0, Inventory.Count(p, worn), "Poor gear goes with the junk.");
            Assert.AreEqual(5, Inventory.Count(p, "mat.copper_ore")); Assert.AreEqual(2, Inventory.Count(p, "mat.yarrow")); Assert.AreEqual(1, Inventory.Count(p, "mat.charcoal"));
            Assert.AreEqual(1, Inventory.Count(p, "tool.pick")); Assert.AreEqual(1, Inventory.Count(p, sound)); Assert.AreEqual(2, Inventory.Count(p, "potion.minor"));
            Assert.AreEqual(0, Inventory.SellJunk(p, db), "Nothing left to sell as junk."); Assert.AreEqual(expected, p.gold);
            // Every material and tool in the game is safe from it, sells for something and can't be worn.
            int materials = 0, tools = 0;
            foreach (var d in db.Items.Values)
            {
                if (d.kind != "material" && d.kind != "tool") continue;
                if (d.kind == "material") materials++; else tools++;
                Assert.IsFalse(Inventory.IsJunk(d), d.id); Assert.AreEqual(1, d.quality, d.id); Assert.GreaterOrEqual(d.value, 1, d.id);
                Assert.IsFalse(Inventory.CanEquip(d, 13, out _), d.id); Assert.IsFalse(string.IsNullOrEmpty(d.canonStatus), d.id + " is labelled.");
                // Hides kept the stack of ten they had as junk (the bags step made them materials).
                if (d.kind == "material") Assert.AreEqual(Inventory.IsHide(d) ? 10 : 20, d.stack, d.id); else { Assert.AreEqual(1, d.stack, d.id); Assert.IsFalse(string.IsNullOrEmpty(d.teaches), d.id); }
            }
            Assert.GreaterOrEqual(materials, 19, "Fifteen raw materials, charcoal, flour, salt and vials."); Assert.AreEqual(2, tools);
            Assert.IsTrue(Inventory.IsJunk(db.Get("junk.wolf_fang"))); Assert.IsFalse(Inventory.IsJunk(null)); Assert.IsFalse(Inventory.IsJunk(db.Get("potion.minor")));
        }

        [Test] public void Hides_are_materials_and_SellJunk_keeps_them()
        {
            var db = Db(); var p = EncounterSession.FreshProgress();
            // The four hides the beasts drop today: materials now, their ids, names, values and stacks as they were.
            var hides = new[] { ("junk.wolf_pelt", "Grey wolf pelt", 2), ("junk.ash_hide", "Ash-matted hide", 5), ("junk.moss_hide", "Moss-matted hide", 7), ("junk.dappled_hide", "Dappled stag hide", 8) };
            foreach (var (id, name, value) in hides)
            {
                var d = db.Get(id); Assert.NotNull(d, id);
                Assert.AreEqual("material", d.kind, id); Assert.AreEqual(1, d.quality, id); Assert.AreEqual(name, d.name, id); Assert.AreEqual(value, d.value, id); Assert.AreEqual(10, d.stack, id);
                Assert.AreEqual(Inventory.HideTrade, d.trade, id + " is a delivery to the tannery when sold in a village.");
                Assert.IsTrue(Inventory.IsHide(d), id); Assert.IsFalse(Inventory.IsJunk(d), id); Assert.IsTrue(string.IsNullOrEmpty(d.pouch), id + " goes in no trade bag.");
            }
            Assert.IsFalse(Inventory.IsHide(db.Get("mat.copper_ore"))); Assert.IsFalse(Inventory.IsHide(db.Get("junk.wolf_fang")));
            // "Sell junk" sells the fangs and tusks and leaves every hide.
            foreach (var (id, _, _) in hides) Assert.AreEqual(0, Inventory.Add(p, db, id, 3), id);
            Inventory.Add(p, db, "junk.wolf_fang", 2); Inventory.Add(p, db, "junk.boar_tusk", 1);
            int expected = 2 * db.Get("junk.wolf_fang").value + db.Get("junk.boar_tusk").value;
            Assert.AreEqual(expected, Inventory.SellJunk(p, db));
            foreach (var (id, _, _) in hides) Assert.AreEqual(3, Inventory.Count(p, id), id + " is kept.");
            Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_fang")); Assert.AreEqual(0, Inventory.Count(p, "junk.boar_tusk"));
            // The wolf still drops its pelt as often as before.
            var wolf = db.Loot.Find(t => t.tag == "wolf"); Assert.NotNull(wolf);
            Assert.AreEqual(.7f, System.Array.Find(wolf.entries, e => e.item == "junk.wolf_pelt").chance, 1e-4f);
        }

        /// <summary>
        /// Boar meat is a cook's material now (BUILD_PLAN step 10; DESIGN 2.1, 8.1): its id, name, value and description as they were,
        /// kind material at quality 1 in stacks of 20, the larder-scrip's, and a delivery to the inn's pot when sold in a village. The
        /// four new meats are the same, each dropped by its beast at the design's chance; no vendor sells any of them; and "Sell junk"
        /// sells the tusks and fangs and keeps every meat.
        /// </summary>
        [Test] public void BoarMeat_IsMaterial()
        {
            var db = Db(); var p = EncounterSession.FreshProgress();
            var meats = new[] { ("junk.boar_meat", "Tough boar meat", 1, "boar", .5f), ("mat.wolf_haunch", "Lean wolf haunch", 1, "wolf", .4f), ("mat.hound_flank", "Ash-hound flank", 4, "hound", .45f),
                ("mat.mossback_chop", "Mossback chop", 5, "mossboar", .5f), ("mat.venison", "Shore venison", 6, "stag", .5f) };
            foreach (var (id, name, value, beast, chance) in meats)
            {
                var d = db.Get(id); Assert.NotNull(d, id);
                Assert.AreEqual("material", d.kind, id); Assert.AreEqual(1, d.quality, id); Assert.AreEqual(name, d.name, id); Assert.AreEqual(value, d.value, id); Assert.AreEqual(20, d.stack, id);
                Assert.AreEqual("inn.meat", d.trade, id + " goes to the inn's pot when sold in a village."); Assert.AreEqual("larder", d.pouch, id + " is the larder-scrip's.");
                Assert.IsFalse(Inventory.IsJunk(d), id); Assert.IsFalse(Inventory.IsHide(d), id); Assert.IsFalse(string.IsNullOrEmpty(d.canonStatus), id + " is labelled.");
                var table = db.Loot.Find(t => t.tag == beast); Assert.NotNull(table, beast);
                var drop = System.Array.Find(table.entries, e => e.item == id); Assert.NotNull(drop, "The " + beast + " drops " + id + ".");
                Assert.AreEqual(chance, drop.chance, 1e-4f, beast);
                foreach (var v in db.Vendors) Assert.IsFalse(System.Array.IndexOf(v.items, id) >= 0, id + " is sold by a vendor.");
            }
            Assert.AreEqual("Needs a long stew.", db.Get("junk.boar_meat").description);
            Assert.AreEqual("bag.larder_scrip", Inventory.BagFor(db, "larder").id, "The larder-scrip holds the meat.");
            // The beasts' other drops are as they were.
            var boar = db.Loot.Find(t => t.tag == "boar");
            Assert.AreEqual(.6f, System.Array.Find(boar.entries, e => e.item == "junk.boar_tusk").chance, 1e-4f);
            Assert.AreEqual(.7f, System.Array.Find(db.Loot.Find(t => t.tag == "hound").entries, e => e.item == "junk.ash_hide").chance, 1e-4f);
            Assert.AreEqual(.5f, System.Array.Find(db.Loot.Find(t => t.tag == "stag").entries, e => e.item == "junk.dappled_hide").chance, 1e-4f);
            // "Sell junk" sells the tusks and fangs and keeps the meat.
            foreach (var (id, _, _, _, _) in meats) Assert.AreEqual(0, Inventory.Add(p, db, id, 4), id);
            Inventory.Add(p, db, "junk.boar_tusk", 2); Inventory.Add(p, db, "junk.wolf_fang", 1);
            int expected = 2 * db.Get("junk.boar_tusk").value + db.Get("junk.wolf_fang").value;
            Assert.AreEqual(expected, Inventory.SellJunk(p, db)); Assert.AreEqual(expected, p.gold);
            foreach (var (id, _, _, _, _) in meats) Assert.AreEqual(4, Inventory.Count(p, id), id + " is kept.");
            Assert.AreEqual(0, Inventory.Count(p, "junk.boar_tusk")); Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_fang"));
            // A boar still gives its meat about half the time.
            var rng = new System.Random(11); int meat = 0;
            for (int n = 0; n < 400; n++) foreach (var (item, _) in db.RollLoot("boar", 2, false, rng)) if (item == "junk.boar_meat") meat++;
            Assert.That(meat, Is.InRange(160, 240), "About half of 400 boars (" + meat + ").");
        }

        [Test] public void Vendors_sell_tools_and_makings_but_never_what_is_gathered()
        {
            var db = Db();
            var merchant = db.StockFor("Ama Rusk", "merchant", 2);
            foreach (var id in new[] { "tool.pick", "tool.hatchet", "mat.flour", "mat.salt", "mat.vial" }) Assert.Contains(id, merchant, "merchant");
            var smith = db.StockFor("Brannoc Vell", "blacksmith", 2);
            foreach (var id in new[] { "tool.pick", "tool.hatchet", "mat.charcoal" }) Assert.Contains(id, smith, "blacksmith");
            Assert.IsTrue(smith.Exists(i => i.StartsWith("gen.")), "The smith still sells zone gear.");
            Assert.Contains("mat.vial", db.StockFor("Lisbet Crane", "herbalist", 2));
            Assert.AreEqual(8, Inventory.Price(db.Get("tool.pick"))); Assert.AreEqual(8, Inventory.Price(db.Get("tool.hatchet")));
            foreach (var id in new[] { "mat.flour", "mat.salt", "mat.vial", "mat.charcoal" }) Assert.AreEqual(4, Inventory.Price(db.Get(id)), id);
            // No gold loop: ore, logs and herbs come from the world only.
            string[] raw = { "mat.copper_ore", "mat.bogiron_ore", "mat.adit_ore", "mat.cinder_ore", "mat.veridian_ore", "mat.oak_log", "mat.blackpine_log", "mat.stonepine_log", "mat.snag_wood", "mat.ghostoak_log",
                "mat.yarrow", "mat.mourners_cap", "mat.tarnwort", "mat.cinder_thistle", "mat.dewfern" };
            foreach (var id in raw)
            {
                Assert.NotNull(db.Get(id), id); Assert.AreEqual("material", db.Get(id).kind, id);
                Assert.IsFalse(string.IsNullOrEmpty(db.Get(id).trade), id + " feeds a village stock when sold."); Assert.IsFalse(string.IsNullOrEmpty(db.Get(id).pouch), id + " has a trade bag.");
                foreach (var v in db.Vendors) Assert.IsFalse(System.Array.IndexOf(v.items, id) >= 0, id + " is sold by a vendor.");
            }
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 6 }, new[] { db.Get(raw[0]).value, db.Get(raw[1]).value, db.Get(raw[2]).value, db.Get(raw[3]).value, db.Get(raw[4]).value }, "Raw value by tier.");
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
