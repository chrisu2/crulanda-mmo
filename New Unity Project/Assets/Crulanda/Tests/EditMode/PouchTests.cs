using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The leatherworker's trade bags in the bags (tools/wip/professions/ADDENDUM.md D): a bag used from the bags is worn for good and
    /// adds its slots after the 24, one of a kind; the materials it holds go into it before the ordinary slots, nothing else goes in;
    /// counting, taking out and room see its slots; "N free" counts the ordinary slots only.
    /// </summary>
    public class PouchTests
    {
        static ItemDatabase Db()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return ItemDatabase.Parse(texts);
        }
        /// <summary>A fresh character wearing these bags (bought, then used from the bags).</summary>
        static EncounterProgress Wearing(ItemDatabase db, params string[] bags)
        {
            var p = EncounterSession.FreshProgress();
            foreach (var b in bags)
            {
                Assert.AreEqual(0, Inventory.Add(p, db, b, 1), b);
                Assert.IsTrue(Inventory.Wear(p, db, p.bag.FindIndex(s => s.item == b), out var why), b + ": " + why);
            }
            return p;
        }

        [Test] public void The_four_bags_are_sold_by_the_leatherworker_at_twelve_to_twenty_four_gold()
        {
            var db = Db();
            var bags = new[] { ("bag.simples_wallet", "herb", 6, 12), ("bag.log_sling", "timber", 6, 16), ("bag.larder_scrip", "larder", 8, 20), ("bag.ore_poke", "ore", 8, 24) };
            var stock = db.StockFor("Maud Tanner", "leatherworker", 2);
            foreach (var (id, holds, slots, price) in bags)
            {
                var d = db.Get(id); Assert.NotNull(d, id);
                Assert.AreEqual("bag", d.kind, id); Assert.AreEqual(holds, d.holds, id); Assert.AreEqual(slots, d.slots, id); Assert.AreEqual(price, Inventory.Price(d), id);
                Assert.AreEqual(1, d.stack, id); Assert.AreEqual(1, d.quality, id); Assert.IsFalse(Inventory.IsJunk(d), id); Assert.AreEqual("GAME-ONLY", d.canonStatus, id);
                Assert.AreSame(d, Inventory.BagFor(db, holds), "The " + holds + " bag.");
                Assert.Contains(id, stock);
                foreach (var v in db.Vendors) if (v.role != "leatherworker") Assert.IsFalse(System.Array.IndexOf(v.items, id) >= 0, id + " is sold only by the leatherworker.");
            }
            // Every material that gathering gives has a bag (the trade bag classes), and a bag must say what it holds.
            foreach (var d in db.Items.Values) if (!string.IsNullOrEmpty(d.pouch)) Assert.NotNull(Inventory.BagFor(db, d.pouch), d.id + " goes in a " + d.pouch + " bag.");
            var e = Assert.Throws<System.ArgumentException>(() => ItemDatabase.Parse(new[] { "{\"items\":[{\"id\":\"bag.x\",\"name\":\"X\",\"kind\":\"bag\",\"slots\":0}]}" }));
            StringAssert.Contains("needs holds", e.Message);
        }

        [Test] public void Wearing_a_trade_bag_adds_its_slots_and_uses_up_the_item()
        {
            var db = Db(); var p = EncounterSession.FreshProgress();
            Inventory.Add(p, db, "bag.ore_poke", 1); int at = p.bag.FindIndex(s => s.item == "bag.ore_poke");
            Assert.IsTrue(Inventory.Wear(p, db, at, out var why), why);
            Assert.IsTrue(p.bag[at].Empty, "The bag leaves the bags."); Assert.AreEqual(0, Inventory.Count(p, "bag.ore_poke"));
            CollectionAssert.AreEqual(new[] { "bag.ore_poke" }, p.pouches);
            Assert.AreEqual(Inventory.BagSize + 8, p.bag.Count, "Eight more slots, after the 24.");
            Assert.IsTrue(Inventory.Wears(p, "bag.ore_poke")); Assert.IsTrue(Inventory.Owns(p, "bag.ore_poke"));
            Assert.AreSame(db.Get("bag.ore_poke"), Inventory.PouchAt(p, db, Inventory.BagSize)); Assert.AreSame(db.Get("bag.ore_poke"), Inventory.PouchAt(p, db, Inventory.BagSize + 7));
            Assert.IsNull(Inventory.PouchAt(p, db, Inventory.BagSize - 1), "The 24 are ordinary.");
            // A second bag goes after the first, in the order worn.
            Inventory.Add(p, db, "bag.simples_wallet", 1);
            Assert.IsTrue(Inventory.Wear(p, db, p.bag.FindIndex(s => s.item == "bag.simples_wallet"), out why), why);
            CollectionAssert.AreEqual(new[] { "bag.ore_poke", "bag.simples_wallet" }, p.pouches);
            Assert.AreEqual(Inventory.BagSize + 8 + 6, p.bag.Count);
            var ranges = Inventory.Pouches(p, db);
            Assert.AreEqual(2, ranges.Count); Assert.AreEqual(Inventory.BagSize, ranges[0].start); Assert.AreEqual(Inventory.BagSize + 8, ranges[1].start); Assert.AreEqual(6, ranges[1].count);
            // Ensuring again after a load never adds or takes slots away.
            Inventory.EnsurePouches(p, db); Assert.AreEqual(Inventory.BagSize + 14, p.bag.Count);
            // What isn't a bag can't be worn this way.
            Inventory.Add(p, db, "mat.copper_ore", 1);
            Assert.IsFalse(Inventory.Wear(p, db, p.bag.FindIndex(s => s.item == "mat.copper_ore"), out why)); Assert.AreEqual("That can't be worn.", why);
        }

        [Test] public void A_second_bag_of_a_kind_is_refused_and_kept()
        {
            var db = Db(); var p = Wearing(db, "bag.ore_poke");
            Inventory.Add(p, db, "bag.ore_poke", 1); int at = p.bag.FindIndex(s => s.item == "bag.ore_poke");
            Assert.IsFalse(Inventory.Wear(p, db, at, out var why));
            Assert.AreEqual(Inventory.AlreadyWornLine, why); Assert.AreEqual("You already carry one.", why);
            Assert.AreEqual("bag.ore_poke", p.bag[at].item, "The second poke stays in the bags."); Assert.AreEqual(1, p.bag[at].count);
            Assert.AreEqual(1, p.pouches.Count); Assert.AreEqual(Inventory.BagSize + 8, p.bag.Count, "No slots added.");
        }

        [Test] public void Materials_fill_their_trade_bag_before_the_ordinary_slots()
        {
            var db = Db(); var p = Wearing(db, "bag.simples_wallet", "bag.ore_poke");
            int wallet = Inventory.BagSize, poke = Inventory.BagSize + 6;
            Assert.AreEqual(0, Inventory.Add(p, db, "mat.yarrow", 3));
            Assert.AreEqual("mat.yarrow", p.bag[wallet].item, "Yarrow goes in the wallet's first slot."); Assert.AreEqual(3, p.bag[wallet].count);
            for (int i = 0; i < Inventory.BagSize; i++) Assert.IsTrue(p.bag[i].Empty, "ordinary slot " + i);
            Assert.AreEqual(0, Inventory.Add(p, db, "mat.copper_ore", 2)); Assert.AreEqual("mat.copper_ore", p.bag[poke].item, "Ore goes in the poke, not the wallet.");
            Assert.AreEqual(0, Inventory.Add(p, db, "mat.charcoal", 1)); Assert.AreEqual("mat.charcoal", p.bag[poke + 1].item);
            Assert.AreEqual(0, Inventory.Add(p, db, "mat.vial", 1)); Assert.AreEqual("mat.vial", p.bag[wallet + 1].item, "Vials go in the wallet.");
            // A stack already in the ordinary slots is topped up first.
            var q = EncounterSession.FreshProgress(); Inventory.Add(q, db, "mat.yarrow", 5);
            Inventory.Add(q, db, "bag.simples_wallet", 1); Assert.IsTrue(Inventory.Wear(q, db, q.bag.FindIndex(s => s.item == "bag.simples_wallet"), out _));
            Assert.AreEqual(0, Inventory.Add(q, db, "mat.yarrow", 4));
            Assert.AreEqual(9, q.bag[0].count, "The old stack takes them first."); Assert.IsTrue(q.bag[Inventory.BagSize].Empty);
            Assert.AreEqual(0, Inventory.Add(q, db, "mat.yarrow", 15)); Assert.AreEqual(20, q.bag[0].count); Assert.AreEqual(4, q.bag[Inventory.BagSize].count, "Then the wallet.");
            // The wallet full (six stacks of twenty), yarrow spills into the ordinary slots.
            Assert.AreEqual(0, Inventory.Add(q, db, "mat.yarrow", 16 + 5 * 20 + 7));
            for (int i = 0; i < 6; i++) Assert.AreEqual(20, q.bag[Inventory.BagSize + i].count, "wallet slot " + i);
            Assert.AreEqual("mat.yarrow", q.bag[1].item); Assert.AreEqual(7, q.bag[1].count);
        }

        [Test] public void Nothing_else_enters_a_trade_bag()
        {
            var db = Db(); var p = Wearing(db, "bag.ore_poke");
            int poke = Inventory.BagSize;
            string sword = ItemDatabase.GearId("mainhand", 2, 1, 5);
            // The 24 full of gear: a potion, a hide and the sword have nowhere to go, however empty the poke.
            for (int i = 0; i < Inventory.BagSize; i++) Assert.AreEqual(0, Inventory.Add(p, db, ItemDatabase.GearId("feet", 2, 1, i), 1));
            Assert.AreEqual(1, Inventory.Add(p, db, "potion.minor", 1)); Assert.AreEqual(1, Inventory.Add(p, db, "junk.wolf_pelt", 1)); Assert.AreEqual(1, Inventory.Add(p, db, "mat.yarrow", 1));
            Assert.AreEqual(0, Inventory.Room(p, db, "potion.minor")); Assert.AreEqual(0, Inventory.Room(p, db, "mat.yarrow"), "No wallet worn: herbs don't go in a poke.");
            for (int i = poke; i < p.bag.Count; i++) Assert.IsTrue(p.bag[i].Empty, "poke slot " + i);
            Assert.IsFalse(Inventory.Accepts(p, db, poke, db.Get("potion.minor"))); Assert.IsTrue(Inventory.Accepts(p, db, poke, db.Get("mat.copper_ore")));
            // Moving gear into the poke is refused, and so is swapping the poke's ore out for gear.
            p.bag[0] = new ItemStack { item = sword, count = 1 };
            Assert.IsFalse(Inventory.Move(p, db, 0, poke, out var why)); Assert.AreEqual("Only ore, bars and charcoal go in the ore-poke.", why);
            Assert.AreEqual(sword, p.bag[0].item); Assert.IsTrue(p.bag[poke].Empty, "Nothing moved.");
            Assert.AreEqual(0, Inventory.Add(p, db, "mat.copper_ore", 3)); Assert.AreEqual("mat.copper_ore", p.bag[poke].item);
            Assert.IsFalse(Inventory.Move(p, db, poke, 0, out why), "The sword would land in the poke."); Assert.AreEqual("Only ore, bars and charcoal go in the ore-poke.", why);
            Assert.AreEqual("mat.copper_ore", p.bag[poke].item); Assert.AreEqual(sword, p.bag[0].item);
            // Ore moves within the poke and out to an empty ordinary slot.
            Assert.IsTrue(Inventory.Move(p, db, poke, poke + 3)); Assert.AreEqual("mat.copper_ore", p.bag[poke + 3].item);
            p.bag[5] = new ItemStack();
            Assert.IsTrue(Inventory.Move(p, db, poke + 3, 5)); Assert.AreEqual("mat.copper_ore", p.bag[5].item); Assert.IsTrue(p.bag[poke + 3].Empty);
            // Worn gear never comes off into a trade bag's slot.
            Inventory.Equip(p, db, 0, 10, out _); Assert.AreEqual(sword, p.equipment[(int)EquipSlot.MainHand].item);
            for (int i = 0; i < Inventory.BagSize; i++) if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "potion.minor", count = 1 };
            Assert.IsFalse(Inventory.Unequip(p, (int)EquipSlot.MainHand), "The 24 are full; the poke is not for swords.");
            Assert.IsFalse(Inventory.Unequip(p, (int)EquipSlot.MainHand, poke + 1));
            Assert.AreEqual(sword, p.equipment[(int)EquipSlot.MainHand].item);
        }

        [Test] public void Count_Remove_and_Room_see_trade_bag_slots()
        {
            var db = Db(); var p = Wearing(db, "bag.simples_wallet");
            Assert.AreEqual(Inventory.BagSize * 20 + 6 * 20, Inventory.Room(p, db, "mat.yarrow"), "Every slot, the wallet's too.");
            Assert.AreEqual(Inventory.BagSize * 20, Inventory.Room(p, db, "mat.copper_ore"), "Ore: the ordinary slots only.");
            Inventory.Add(p, db, "mat.yarrow", 30);
            Assert.AreEqual(20, p.bag[Inventory.BagSize].count); Assert.AreEqual(10, p.bag[Inventory.BagSize + 1].count);
            Assert.AreEqual(30, Inventory.Count(p, "mat.yarrow")); Assert.IsTrue(Inventory.Has(p, "mat.yarrow"));
            Assert.AreEqual(Inventory.BagSize * 20 + 6 * 20 - 30, Inventory.Room(p, db, "mat.yarrow"));
            Inventory.Remove(p, "mat.yarrow", 12);
            Assert.AreEqual(18, Inventory.Count(p, "mat.yarrow")); Assert.IsTrue(p.bag[Inventory.BagSize + 1].Empty, "Taken from the last stack first."); Assert.AreEqual(18, p.bag[Inventory.BagSize].count);
            // Selling and destroying work on a trade bag's slot as on any.
            Assert.AreEqual(18 * db.Get("mat.yarrow").value, Inventory.Sell(p, db, Inventory.BagSize)); Assert.AreEqual(0, Inventory.Count(p, "mat.yarrow"));
        }

        [Test] public void FreeSlots_counts_ordinary_slots_only()
        {
            var db = Db(); var p = Wearing(db, "bag.ore_poke", "bag.log_sling");
            Assert.AreEqual(Inventory.BagSize, Inventory.FreeSlots(p), "The poke's and the sling's empty slots are not 'free'.");
            Inventory.Add(p, db, "mat.oak_log", 40); Assert.AreEqual(Inventory.BagSize, Inventory.FreeSlots(p), "Logs went in the sling.");
            Inventory.Add(p, db, "potion.minor", 1); Assert.AreEqual(Inventory.BagSize - 1, Inventory.FreeSlots(p));
            for (int i = 0; i < Inventory.BagSize; i++) Inventory.Add(p, db, ItemDatabase.GearId("feet", 2, 1, i), 1);
            Assert.AreEqual(0, Inventory.FreeSlots(p)); Assert.Greater(Inventory.Room(p, db, "mat.copper_ore"), 0, "Full bags, but the poke has room for ore.");
            Assert.AreEqual(0, Inventory.Add(p, db, "mat.copper_ore", 5), "And ore still goes in.");
        }
    }
}
