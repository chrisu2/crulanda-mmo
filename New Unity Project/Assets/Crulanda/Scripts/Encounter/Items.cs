using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    // ============================================================================================
    // Items: gear, junk, consumables. Hand-made items live in EncounterContent/Items/*.json; most gear is generated
    // from an id ("gen.<slot>.<level>.<quality>.<seed>"), so loot can be varied without authoring thousands of entries.
    // ============================================================================================
    public enum EquipSlot { Head, Neck, Shoulders, Chest, Hands, Legs, Feet, MainHand, OffHand }

    [Serializable] public sealed class ItemFile
    {
        public ItemDef[] items = new ItemDef[0];
        public VendorDef[] vendors = new VendorDef[0];
        public LootTableDef[] loot = new LootTableDef[0];
    }
    /// <summary>
    /// kind: gear | junk | consumable. slot: head, neck, shoulders, chest, hands, legs, feet, mainhand, offhand (gear only).
    /// quality: 0 poor (grey), 1 common (white), 2 uncommon (green), 3 rare (blue), 4 epic (purple).
    /// value: sell price in gold (merchants charge 4x). heal: consumables restore this much health (potions) or over 10 s out of combat (food).
    /// </summary>
    [Serializable] public sealed class ItemDef
    {
        public string id, name, kind = "gear", slot, description, canonStatus;
        public int quality = 1, level = 1, value = 1, stack = 1;
        public int armor, stamina, strength, agility, intellect, spirit, weaponDamage, heal;
        public bool food;
    }
    /// <summary>What a merchant sells: by NPC name, or by role (merchant, blacksmith, baker, herbalist...) for anyone of that trade.</summary>
    [Serializable] public sealed class VendorDef { public string npc, role; public string[] items = new string[0]; public bool gearForZone; }
    /// <summary>Drops for mobs whose id tag matches (wolf, boar, collector... or "any"), within a level band.</summary>
    [Serializable] public sealed class LootTableDef { public string tag = "any"; public int levelMin = 1, levelMax = 10; public LootEntry[] entries = new LootEntry[0]; public float gearChance = .12f; }
    [Serializable] public sealed class LootEntry { public string item; public float chance = .5f; public int min = 1, max = 1; }

    /// <summary>One bag slot or equipment slot in the save ("" = empty).</summary>
    [Serializable] public sealed class ItemStack { public string item = ""; public int count; public bool Empty { get { return string.IsNullOrEmpty(item) || count <= 0; } } }

    public sealed class ItemDatabase
    {
        public readonly Dictionary<string, ItemDef> Items = new Dictionary<string, ItemDef>(StringComparer.Ordinal);
        public readonly List<VendorDef> Vendors = new List<VendorDef>();
        public readonly List<LootTableDef> Loot = new List<LootTableDef>();
        public static readonly string[] SlotIds = { "head", "neck", "shoulders", "chest", "hands", "legs", "feet", "mainhand", "offhand" };
        public static readonly string[] SlotNames = { "Head", "Neck", "Shoulders", "Chest", "Hands", "Legs", "Feet", "Main hand", "Off hand" };
        public static readonly string[] QualityNames = { "Poor", "Common", "Uncommon", "Rare", "Epic" };
        public static readonly Color[] QualityColors = { new Color(.62f, .62f, .62f), new Color(1, 1, 1), new Color(.12f, 1, 0), new Color(0, .44f, .87f), new Color(.64f, .21f, .93f) };

        public static ItemDatabase Parse(IEnumerable<string> jsonFiles)
        {
            var db = new ItemDatabase(); var errors = new List<string>();
            foreach (var json in jsonFiles)
            {
                if (string.IsNullOrWhiteSpace(json)) continue;
                ItemFile f; try { f = JsonUtility.FromJson<ItemFile>(json); } catch (Exception e) { errors.Add("Unreadable item file: " + e.Message); continue; }
                if (f == null) continue;
                foreach (var i in f.items ?? new ItemDef[0])
                {
                    if (i == null || string.IsNullOrEmpty(i.id)) { errors.Add("An item has no id."); continue; }
                    if (db.Items.ContainsKey(i.id)) { errors.Add("Duplicate item '" + i.id + "'."); continue; }
                    if (i.kind == "gear" && SlotIndex(i.slot) < 0) errors.Add("Item '" + i.id + "' has no valid slot.");
                    if (i.stack < 1) i.stack = 1;
                    db.Items[i.id] = i;
                }
                if (f.vendors != null) db.Vendors.AddRange(f.vendors);
                if (f.loot != null) db.Loot.AddRange(f.loot);
            }
            foreach (var v in db.Vendors) foreach (var id in v.items ?? new string[0]) if (db.Get(id) == null) errors.Add("Vendor sells unknown item '" + id + "'.");
            foreach (var t in db.Loot) foreach (var e in t.entries ?? new LootEntry[0]) if (db.Get(e.item) == null) errors.Add("Loot table drops unknown item '" + e.item + "'.");
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            return db;
        }
        public static int SlotIndex(string slot) { return Array.IndexOf(SlotIds, slot ?? ""); }

        readonly Dictionary<string, ItemDef> generated = new Dictionary<string, ItemDef>();
        /// <summary>An item by id: authored, or generated from a "gen." id.</summary>
        public ItemDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Items.TryGetValue(id, out var d)) return d;
            if (!id.StartsWith("gen.")) return null;
            if (!generated.TryGetValue(id, out d)) { d = Generate(id); if (d != null) generated[id] = d; }
            return d;
        }

        // ---------- generated gear ----------
        static readonly string[][] Materials = {
            new[] { "Homespun", "Frayed", "Patched" },          // 1-2
            new[] { "Tanned", "Riveted", "Stitched" },          // 3-5
            new[] { "Toll-road", "Ridge-forged", "Pilgrim's" }, // 6-8
            new[] { "Ash-hardened", "Salt-cured", "Emberbound" } // 9-10
        };
        static readonly Dictionary<string, string[]> Pieces = new Dictionary<string, string[]> {
            { "head", new[] { "Cap", "Hood", "Coif" } }, { "neck", new[] { "Pendant", "Cord", "Torc" } }, { "shoulders", new[] { "Mantle", "Pauldrons", "Spaulders" } },
            { "chest", new[] { "Jerkin", "Tunic", "Hauberk" } }, { "hands", new[] { "Gloves", "Wraps", "Gauntlets" } }, { "legs", new[] { "Breeches", "Leggings", "Greaves" } },
            { "feet", new[] { "Boots", "Shoes", "Sabatons" } }, { "mainhand", new[] { "Blade", "Hatchet", "Cudgel" } }, { "offhand", new[] { "Buckler", "Shield", "Lantern" } }
        };
        static readonly (string suffix, int sta, int str, int agi, int intel, int spi)[] Suffixes = {
            ("of the Oak", 2, 2, 0, 0, 0), ("of the Hare", 1, 0, 3, 0, 0), ("of the Owl", 1, 0, 0, 3, 1), ("of the Bear", 3, 1, 0, 0, 0), ("of the Hearth", 2, 0, 0, 1, 2), ("of Salt", 1, 1, 1, 1, 0)
        };
        static int BandOf(int level) { return level <= 2 ? 0 : level <= 5 ? 1 : level <= 8 ? 2 : 3; }
        /// <summary>A generated gear id for this slot, level and quality (the seed picks name and suffix).</summary>
        public static string GearId(string slot, int level, int quality, int seed) { return "gen." + slot + "." + Mathf.Clamp(level, 1, EncounterProgress.LevelCap) + "." + Mathf.Clamp(quality, 0, 4) + "." + Mathf.Abs(seed % 10000); }
        static ItemDef Generate(string id)
        {
            var parts = id.Split('.');
            if (parts.Length != 5 || SlotIndex(parts[1]) < 0 || !int.TryParse(parts[2], out int level) || !int.TryParse(parts[3], out int q) || !int.TryParse(parts[4], out int seed)) return null;
            string slot = parts[1]; var rng = new System.Random(seed * 7919 + level * 31 + q);
            string material = Materials[BandOf(level)][rng.Next(3)], piece = Pieces[slot][rng.Next(3)];
            float power = level * (q <= 0 ? .5f : q == 1 ? 1 : q == 2 ? 1.35f : q == 3 ? 1.7f : 2.1f);
            var d = new ItemDef { id = id, kind = "gear", slot = slot, level = Mathf.Max(1, level - 1), quality = q, canonStatus = "GAME-ONLY" };
            bool weapon = slot == "mainhand", jewel = slot == "neck";
            d.name = (q == 0 ? "Worn " : "") + material + " " + piece;
            if (weapon) d.weaponDamage = Mathf.Max(1, Mathf.RoundToInt(3 + power * 1.6f));
            else if (!jewel) d.armor = Mathf.Max(1, Mathf.RoundToInt(power * (slot == "chest" || slot == "legs" ? 2.2f : slot == "offhand" ? 2.6f : 1.4f)));
            if (q >= 2)
            {
                var s = Suffixes[rng.Next(Suffixes.Length)]; float k = Mathf.Max(1, power / 4);
                d.name += " " + s.suffix;
                d.stamina = Mathf.RoundToInt(s.sta * k); d.strength = Mathf.RoundToInt(s.str * k); d.agility = Mathf.RoundToInt(s.agi * k);
                d.intellect = Mathf.RoundToInt(s.intel * k); d.spirit = Mathf.RoundToInt(s.spi * k);
            }
            else if (q == 1 && (jewel || rng.Next(3) == 0)) d.stamina = Mathf.Max(1, Mathf.RoundToInt(power / 3));
            d.value = Mathf.Max(1, Mathf.RoundToInt(level * (1 + q) * (weapon ? 1.4f : 1)));
            d.description = q == 0 ? "It has seen better days." : null;
            return d;
        }

        // ---------- loot ----------
        /// <summary>Rolls drops for a mob: its tag's junk, a chance of level gear (elites drop gear far more often and better).</summary>
        public List<(string item, int count)> RollLoot(string tag, int level, bool elite, System.Random rng)
        {
            var drops = new List<(string, int)>(); float gearChance = .1f;
            foreach (var t in Loot)
            {
                if ((t.tag != "any" && t.tag != tag) || level < t.levelMin || level > t.levelMax) continue;
                gearChance = Mathf.Max(gearChance, t.gearChance);
                foreach (var e in t.entries) if (rng.NextDouble() < e.chance) drops.Add((e.item, e.min + rng.Next(Mathf.Max(1, e.max - e.min + 1))));
            }
            if (elite) gearChance = Mathf.Max(gearChance, .7f);
            if (rng.NextDouble() < gearChance)
            {
                int q = elite ? (rng.NextDouble() < .35f ? 3 : 2) : rng.NextDouble() < .18f ? 2 : rng.NextDouble() < .25f ? 0 : 1;
                drops.Add((GearId(SlotIds[rng.Next(SlotIds.Length)], level, q, rng.Next(10000)), 1));
            }
            return drops;
        }
        /// <summary>A merchant's stock: its listed items, plus common gear for this zone's levels when it deals in gear.</summary>
        public List<string> StockFor(string npc, string role, int zoneLevel)
        {
            var stock = new List<string>();
            foreach (var v in Vendors)
            {
                if (!(v.npc != null && v.npc == npc) && !(v.npc == null && v.role != null && v.role == role)) continue;
                foreach (var i in v.items) if (!stock.Contains(i)) stock.Add(i);
                if (v.gearForZone)
                    for (int s = 0; s < SlotIds.Length; s++) stock.Add(GearId(SlotIds[s], zoneLevel, 1, 100 + s * 13 + zoneLevel));
            }
            return stock;
        }
        public static string StatLines(ItemDef d)
        {
            var l = new List<string>();
            if (d.weaponDamage > 0) l.Add("+" + d.weaponDamage + " weapon damage");
            if (d.armor > 0) l.Add(d.armor + " armor");
            if (d.stamina > 0) l.Add("+" + d.stamina + " Stamina"); if (d.strength > 0) l.Add("+" + d.strength + " Strength");
            if (d.agility > 0) l.Add("+" + d.agility + " Agility"); if (d.intellect > 0) l.Add("+" + d.intellect + " Intellect"); if (d.spirit > 0) l.Add("+" + d.spirit + " Spirit");
            if (d.heal > 0) l.Add(d.food ? "Restores " + d.heal + " health over 10 s (out of combat)" : "Restores " + d.heal + " health");
            return string.Join("\n", l);
        }
    }

    /// <summary>
    /// Bag and equipment operations on the save: add with stacking, move/swap, split nothing (kept simple),
    /// equip/unequip with slot checks, destroy, sell and buy. Pure logic; the session applies stats afterwards.
    /// </summary>
    public static class Inventory
    {
        public const int BagSize = 24;
        public static void Ensure(EncounterProgress p)
        {
            if (p.bag == null) p.bag = new List<ItemStack>();
            while (p.bag.Count < BagSize) p.bag.Add(new ItemStack());
            if (p.equipment == null) p.equipment = new List<ItemStack>();
            while (p.equipment.Count < ItemDatabase.SlotIds.Length) p.equipment.Add(new ItemStack());
            foreach (var s in p.bag) if (s.Empty) { s.item = ""; s.count = 0; }
            foreach (var s in p.equipment) if (s.Empty) { s.item = ""; s.count = 0; }
        }
        public static int Count(EncounterProgress p, string item) { int n = 0; foreach (var s in p.bag) if (s.item == item) n += s.count; return n; }
        public static bool Has(EncounterProgress p, string item) { return Count(p, item) > 0 || p.equipment.Exists(s => s.item == item); }
        public static bool IsEquipped(EncounterProgress p, string item) { return p.equipment.Exists(s => s.item == item); }
        /// <summary>Adds items, filling stacks then empty slots. Returns how many did not fit.</summary>
        public static int Add(EncounterProgress p, ItemDatabase db, string item, int count)
        {
            Ensure(p); var d = db.Get(item); if (d == null || count <= 0) return count;
            foreach (var s in p.bag) if (s.item == item && s.count < d.stack) { int take = Math.Min(count, d.stack - s.count); s.count += take; count -= take; if (count == 0) return 0; }
            foreach (var s in p.bag) if (s.Empty) { int take = Math.Min(count, d.stack); s.item = item; s.count = take; count -= take; if (count == 0) return 0; }
            return count;
        }
        public static int FreeSlots(EncounterProgress p) { int n = 0; foreach (var s in p.bag) if (s.Empty) n++; return n; }
        /// <summary>How many of an item the bags could take now (room left on its stacks, then empty slots). 0 for an unknown item.</summary>
        public static int Room(EncounterProgress p, ItemDatabase db, string item)
        {
            Ensure(p); var d = db?.Get(item); if (d == null) return 0;
            int n = 0; foreach (var s in p.bag) n += s.Empty ? d.stack : s.item == item ? Math.Max(0, d.stack - s.count) : 0;
            return n;
        }
        public static void Remove(EncounterProgress p, string item, int count)
        {
            for (int i = p.bag.Count - 1; i >= 0 && count > 0; i--)
            {
                var s = p.bag[i]; if (s.item != item) continue;
                int take = Math.Min(count, s.count); s.count -= take; count -= take; if (s.count <= 0) { s.item = ""; s.count = 0; }
            }
        }
        /// <summary>Moves bag slot a onto b: merges matching stacks, otherwise swaps.</summary>
        public static void Move(EncounterProgress p, ItemDatabase db, int a, int b)
        {
            if (a == b || a < 0 || b < 0 || a >= p.bag.Count || b >= p.bag.Count || p.bag[a].Empty) return;
            var from = p.bag[a]; var to = p.bag[b]; var d = db.Get(from.item);
            if (!to.Empty && to.item == from.item && d != null && d.stack > 1)
            {
                int take = Math.Min(from.count, d.stack - to.count); to.count += take; from.count -= take;
                if (from.count <= 0) { from.item = ""; from.count = 0; }
                return;
            }
            p.bag[a] = to; p.bag[b] = from;
        }
        public static bool CanEquip(ItemDef d, int level, out string why)
        {
            why = null;
            if (d == null || d.kind != "gear") { why = "That can't be worn."; return false; }
            if (d.level > level) { why = "Requires level " + d.level + "."; return false; }
            return true;
        }
        /// <summary>Equips the item in a bag slot; whatever was in that equipment slot goes back to the same bag slot.</summary>
        public static bool Equip(EncounterProgress p, ItemDatabase db, int bagIndex, int level, out string why)
        {
            why = null; if (bagIndex < 0 || bagIndex >= p.bag.Count || p.bag[bagIndex].Empty) return false;
            var d = db.Get(p.bag[bagIndex].item); if (!CanEquip(d, level, out why)) return false;
            int slot = ItemDatabase.SlotIndex(d.slot);
            var old = p.equipment[slot];
            p.equipment[slot] = new ItemStack { item = d.id, count = 1 };
            p.bag[bagIndex] = old.Empty ? new ItemStack() : new ItemStack { item = old.item, count = 1 };
            return true;
        }
        /// <summary>Takes off the item in an equipment slot into the first free bag slot (or a chosen one).</summary>
        public static bool Unequip(EncounterProgress p, int slot, int toBag = -1)
        {
            if (slot < 0 || slot >= p.equipment.Count || p.equipment[slot].Empty) return false;
            if (toBag < 0) toBag = p.bag.FindIndex(s => s.Empty);
            if (toBag < 0 || toBag >= p.bag.Count) return false;
            if (!p.bag[toBag].Empty) return false;
            p.bag[toBag] = new ItemStack { item = p.equipment[slot].item, count = 1 };
            p.equipment[slot] = new ItemStack();
            return true;
        }
        public static void Destroy(EncounterProgress p, int bagIndex) { if (bagIndex >= 0 && bagIndex < p.bag.Count) p.bag[bagIndex] = new ItemStack(); }
        /// <summary>Sells a whole bag stack for its value x count. Returns the gold earned.</summary>
        public static int Sell(EncounterProgress p, ItemDatabase db, int bagIndex)
        {
            if (bagIndex < 0 || bagIndex >= p.bag.Count || p.bag[bagIndex].Empty) return 0;
            var s = p.bag[bagIndex]; var d = db.Get(s.item); int gold = (d != null ? d.value : 0) * s.count;
            p.gold += gold; p.bag[bagIndex] = new ItemStack(); return gold;
        }
        public static int Price(ItemDef d) { return Math.Max(1, d.value * 4); }
        public static bool Buy(EncounterProgress p, ItemDatabase db, string item, out string why)
        {
            why = null; var d = db.Get(item); if (d == null) { why = "Not for sale."; return false; }
            int price = Price(d);
            if (p.gold < price) { why = "You need " + price + " gold."; return false; }
            if (Add(p, db, item, 1) > 0) { why = "Your bags are full."; return false; }
            p.gold -= price; return true;
        }
        /// <summary>Total bonuses from everything worn.</summary>
        public static ItemDef Totals(EncounterProgress p, ItemDatabase db)
        {
            var t = new ItemDef();
            foreach (var s in p.equipment)
            {
                var d = s.Empty ? null : db.Get(s.item); if (d == null) continue;
                t.armor += d.armor; t.stamina += d.stamina; t.strength += d.strength; t.agility += d.agility; t.intellect += d.intellect; t.spirit += d.spirit; t.weaponDamage += d.weaponDamage;
            }
            return t;
        }
    }
}
