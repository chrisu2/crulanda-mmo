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
    /// kind: gear | junk | consumable | material | tool | bag. slot: head, neck, shoulders, chest, hands, legs, feet, mainhand, offhand (gear only).
    /// quality: 0 poor (grey), 1 common (white), 2 uncommon (green), 3 rare (blue), 4 epic (purple).
    /// value: sell price in gold (merchants charge 4x). heal: consumables restore this much health (potions) or over 10 s out of combat (food).
    /// material: something gathered or refined that recipes use (quality 1, so "Sell junk" leaves it). trade: the village stock a sale of
    /// it feeds ("forge.ore", "stall.herbs"). pouch: the class of trade bag that holds it (ore, timber, herb, larder).
    /// tool: used once from the bags, it teaches the trade named in teaches and takes no bag slot after that (ProfessionLog.UseTool).
    /// bag: a trade bag (the leatherworker's). Used once from the bags it is worn for good (Inventory.Wear) and adds slots more
    /// bag slots that take only items whose pouch is its holds class.
    /// </summary>
    [Serializable] public sealed class ItemDef
    {
        public string id, name, kind = "gear", slot, description, canonStatus;
        public string trade, teaches, pouch, holds;
        public int quality = 1, level = 1, value = 1, stack = 1, slots;
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
                    if (i.kind == "bag" && (string.IsNullOrEmpty(i.holds) || i.slots < 1 || i.slots > Inventory.BagSize)) errors.Add("Bag '" + i.id + "' needs holds and 1-" + Inventory.BagSize + " slots.");
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
            new[] { "Ash-hardened", "Salt-cured", "Emberbound" }, // 9-10
            new[] { "Veridian", "Root-bound", "Sap-steeped" }    // 11-13: the Verdant Shore's
        };
        static readonly Dictionary<string, string[]> Pieces = new Dictionary<string, string[]> {
            { "head", new[] { "Cap", "Hood", "Coif" } }, { "neck", new[] { "Pendant", "Cord", "Torc" } }, { "shoulders", new[] { "Mantle", "Pauldrons", "Spaulders" } },
            { "chest", new[] { "Jerkin", "Tunic", "Hauberk" } }, { "hands", new[] { "Gloves", "Wraps", "Gauntlets" } }, { "legs", new[] { "Breeches", "Leggings", "Greaves" } },
            { "feet", new[] { "Boots", "Shoes", "Sabatons" } }, { "mainhand", new[] { "Blade", "Hatchet", "Cudgel" } }, { "offhand", new[] { "Buckler", "Shield", "Lantern" } }
        };
        static readonly (string suffix, int sta, int str, int agi, int intel, int spi)[] Suffixes = {
            ("of the Oak", 2, 2, 0, 0, 0), ("of the Hare", 1, 0, 3, 0, 0), ("of the Owl", 1, 0, 0, 3, 1), ("of the Bear", 3, 1, 0, 0, 0), ("of the Hearth", 2, 0, 0, 1, 2), ("of Salt", 1, 1, 1, 1, 0)
        };
        static int BandOf(int level) { return level <= 2 ? 0 : level <= 5 ? 1 : level <= 8 ? 2 : level <= 10 ? 3 : 4; }
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
    /// Trade bags: the 24 ordinary slots come first, and each worn bag's slots follow them in the order the bags were put on
    /// (EncounterProgress.pouches). A trade bag's slot takes only what that bag holds; a slot past every bag the content knows (one
    /// it no longer has) takes nothing new but can be emptied.
    /// </summary>
    public static class Inventory
    {
        public const int BagSize = 24;
        /// <summary>The most bag slots a save may hold: the ordinary ones and every trade bag's.</summary>
        public const int MaxSlots = BagSize * 4;
        public const string AlreadyWornLine = "You already carry one.";
        /// <summary>What each class of trade bag takes, in words ("Only ore, bars and charcoal go in the ore-poke.").</summary>
        static readonly Dictionary<string, string> PouchWords = new Dictionary<string, string> {
            { "ore", "ore, bars and charcoal" }, { "timber", "logs" }, { "herb", "herbs and vials" }, { "larder", "meat, flour, salt, eggs and cheese" } };
        /// <summary>What a class of trade bag takes, in words.</summary>
        public static string HoldsWords(string pouch) { return pouch != null && PouchWords.TryGetValue(pouch, out var w) ? w : "the things it was cut for"; }
        /// <summary>The village stock a hide sold in a village feeds; it also marks an item as a hide.</summary>
        public const string HideTrade = "tannery.hides";
        /// <summary>A hide or pelt: a material the leatherworker works (and "Sell junk" leaves).</summary>
        public static bool IsHide(ItemDef d) { return d != null && d.kind == "material" && d.trade == HideTrade; }
        public static void Ensure(EncounterProgress p)
        {
            if (p.bag == null) p.bag = new List<ItemStack>();
            while (p.bag.Count < BagSize) p.bag.Add(new ItemStack());
            if (p.equipment == null) p.equipment = new List<ItemStack>();
            while (p.equipment.Count < ItemDatabase.SlotIds.Length) p.equipment.Add(new ItemStack());
            foreach (var s in p.bag) if (s.Empty) { s.item = ""; s.count = 0; }
            foreach (var s in p.equipment) if (s.Empty) { s.item = ""; s.count = 0; }
        }

        // ---------- trade bags ----------
        /// <summary>A worn trade bag and its slots: the first one's index in the bags, and how many.</summary>
        public struct PouchRange { public ItemDef bag; public int start, count; }
        /// <summary>The worn bags the content knows, in the order they were put on, with their slots. An unknown bag id has none.</summary>
        public static List<PouchRange> Pouches(EncounterProgress p, ItemDatabase db)
        {
            var list = new List<PouchRange>(); int at = BagSize;
            if (p.pouches == null || db == null) return list;
            foreach (var id in p.pouches)
            {
                var d = db.Get(id); if (d == null || d.kind != "bag" || d.slots < 1) continue;
                list.Add(new PouchRange { bag = d, start = at, count = d.slots }); at += d.slots;
            }
            return list;
        }
        /// <summary>Where the last worn bag's slots end (24 with none).</summary>
        static int PouchesEnd(List<PouchRange> pouches) { return pouches.Count == 0 ? BagSize : pouches[pouches.Count - 1].start + pouches[pouches.Count - 1].count; }
        /// <summary>After a load: the bags are made long enough for every worn bag's slots. Never shortened: slots past them keep what they hold.</summary>
        public static void EnsurePouches(EncounterProgress p, ItemDatabase db)
        {
            Ensure(p); if (p.pouches == null) p.pouches = new List<string>();
            int end = PouchesEnd(Pouches(p, db));
            while (p.bag.Count < end) p.bag.Add(new ItemStack());
        }
        /// <summary>The worn bag a slot belongs to, or null for an ordinary slot and for one past every known bag.</summary>
        public static ItemDef PouchAt(EncounterProgress p, ItemDatabase db, int index)
        {
            if (index < BagSize) return null;
            foreach (var r in Pouches(p, db)) if (index >= r.start && index < r.start + r.count) return r.bag;
            return null;
        }
        static bool Takes(List<PouchRange> pouches, int index, ItemDef d)
        {
            if (index < BagSize) return true;
            foreach (var r in pouches) if (index >= r.start && index < r.start + r.count) return d != null && !string.IsNullOrEmpty(d.pouch) && d.pouch == r.bag.holds;
            return false;
        }
        /// <summary>Whether a bag slot takes this item: an ordinary slot takes anything, a trade bag's only what that bag holds, and a
        /// slot past every known bag nothing.</summary>
        public static bool Accepts(EncounterProgress p, ItemDatabase db, int index, ItemDef d)
        {
            return p.bag != null && index >= 0 && index < p.bag.Count && d != null && Takes(Pouches(p, db), index, d);
        }
        /// <summary>What a slot that refused something says: "Only ore, bars and charcoal go in the ore-poke."</summary>
        public static string RefusedLine(EncounterProgress p, ItemDatabase db, int index)
        {
            var bag = PouchAt(p, db, index);
            return bag != null ? "Only " + HoldsWords(bag.holds) + " go in the " + LowerFirst(bag.name) + "." : "Nothing more goes in there.";
        }
        static string LowerFirst(string s) { return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1); }
        /// <summary>Whether a trade bag is worn.</summary>
        public static bool Wears(EncounterProgress p, string bag) { return p.pouches != null && p.pouches.Contains(bag); }
        /// <summary>Whether a trade bag is worn or carried: one of each is all anyone sells or gives you.</summary>
        public static bool Owns(EncounterProgress p, string bag) { return Wears(p, bag) || (p.bag != null && Count(p, bag) > 0); }
        /// <summary>The trade bag that holds this class of thing (ore, timber, herb, larder), or null.</summary>
        public static ItemDef BagFor(ItemDatabase db, string pouch)
        {
            if (db == null || string.IsNullOrEmpty(pouch)) return null;
            ItemDef best = null;
            foreach (var d in db.Items.Values) if (d.kind == "bag" && d.holds == pouch && (best == null || string.CompareOrdinal(d.id, best.id) < 0)) best = d;
            return best;
        }
        /// <summary>
        /// Puts on the trade bag in a bag slot, for good: it leaves the bags, its id joins the worn list and its slots are added after
        /// the last worn bag's. Refused, and the bag kept, when the item is not a trade bag, when that bag is already worn ("You already
        /// carry one."), or when no more bags can be carried.
        /// </summary>
        public static bool Wear(EncounterProgress p, ItemDatabase db, int bagIndex, out string why)
        {
            why = null; if (db == null) return false;
            EnsurePouches(p, db);
            if (bagIndex < 0 || bagIndex >= p.bag.Count || p.bag[bagIndex].Empty) return false;
            var d = db.Get(p.bag[bagIndex].item);
            if (d == null || d.kind != "bag" || d.slots < 1) { why = "That can't be worn."; return false; }
            if (Wears(p, d.id)) { why = AlreadyWornLine; return false; }
            int end = PouchesEnd(Pouches(p, db));
            if (p.pouches.Count >= EncounterSave.MaxPouches || end + d.slots > MaxSlots) { why = "You can't carry another bag."; return false; }
            var s = p.bag[bagIndex]; s.count--; if (s.count <= 0) { s.item = ""; s.count = 0; }
            p.pouches.Add(d.id);
            while (p.bag.Count < end + d.slots) p.bag.Add(new ItemStack());
            return true;
        }

        // ---------- bags ----------
        public static int Count(EncounterProgress p, string item) { int n = 0; foreach (var s in p.bag) if (s.item == item) n += s.count; return n; }
        public static bool Has(EncounterProgress p, string item) { return Count(p, item) > 0 || p.equipment.Exists(s => s.item == item); }
        public static bool IsEquipped(EncounterProgress p, string item) { return p.equipment.Exists(s => s.item == item); }
        /// <summary>Adds items: onto stacks of it first, then into empty slots of a worn bag that holds it, then into empty ordinary
        /// slots. Returns how many did not fit.</summary>
        public static int Add(EncounterProgress p, ItemDatabase db, string item, int count)
        {
            Ensure(p); var d = db.Get(item); if (d == null || count <= 0) return count;
            var pouches = Pouches(p, db);
            for (int i = 0; i < p.bag.Count; i++)
            {
                var s = p.bag[i]; if (s.item != item || s.count >= d.stack || !Takes(pouches, i, d)) continue;
                int take = Math.Min(count, d.stack - s.count); s.count += take; count -= take; if (count == 0) return 0;
            }
            for (int pass = 0; pass < 2; pass++)   // a worn bag's empty slots, then the ordinary ones
                for (int i = pass == 0 ? BagSize : 0; i < (pass == 0 ? p.bag.Count : Math.Min(BagSize, p.bag.Count)); i++)
                {
                    var s = p.bag[i]; if (!s.Empty || !Takes(pouches, i, d)) continue;
                    int take = Math.Min(count, d.stack); s.item = item; s.count = take; count -= take; if (count == 0) return 0;
                }
            return count;
        }
        /// <summary>Empty ordinary slots (a trade bag's slots are not counted: they take only their own class).</summary>
        public static int FreeSlots(EncounterProgress p) { int n = 0; for (int i = 0; i < p.bag.Count && i < BagSize; i++) if (p.bag[i].Empty) n++; return n; }
        static int FirstFree(EncounterProgress p) { return p.bag.FindIndex(0, Math.Min(BagSize, p.bag.Count), s => s.Empty); }
        /// <summary>How many of an item the bags could take now (room left on its stacks, then empty slots that take it). 0 for an unknown item.</summary>
        public static int Room(EncounterProgress p, ItemDatabase db, string item)
        {
            Ensure(p); var d = db?.Get(item); if (d == null) return 0;
            var pouches = Pouches(p, db); int n = 0;
            for (int i = 0; i < p.bag.Count; i++)
            {
                var s = p.bag[i]; if (!Takes(pouches, i, d)) continue;
                n += s.Empty ? d.stack : s.item == item ? Math.Max(0, d.stack - s.count) : 0;
            }
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
        /// <summary>Moves bag slot a onto b: merges matching stacks, otherwise swaps. False only when it is refused.</summary>
        public static bool Move(EncounterProgress p, ItemDatabase db, int a, int b) { return Move(p, db, a, b, out _); }
        /// <summary>
        /// Moves bag slot a onto b: merges matching stacks, otherwise swaps. Refused (false, with why) when either slot would end up
        /// holding something it does not take: a sword into an ore-poke's slot, or the poke's ore swapped out for the sword.
        /// </summary>
        public static bool Move(EncounterProgress p, ItemDatabase db, int a, int b, out string why)
        {
            why = null;
            if (a == b || a < 0 || b < 0 || a >= p.bag.Count || b >= p.bag.Count || p.bag[a].Empty) return true;
            var from = p.bag[a]; var to = p.bag[b]; var d = db.Get(from.item); var pouches = Pouches(p, db);
            if (!Takes(pouches, b, d)) { why = RefusedLine(p, db, b); return false; }
            if (!to.Empty && to.item != from.item && !Takes(pouches, a, db.Get(to.item))) { why = RefusedLine(p, db, a); return false; }
            if (!to.Empty && to.item == from.item && d != null && d.stack > 1)
            {
                int take = Math.Min(from.count, d.stack - to.count); to.count += take; from.count -= take;
                if (from.count <= 0) { from.item = ""; from.count = 0; }
                return true;
            }
            p.bag[a] = to; p.bag[b] = from;
            return true;
        }
        public static bool CanEquip(ItemDef d, int level, out string why)
        {
            why = null;
            if (d == null || d.kind != "gear") { why = "That can't be worn."; return false; }
            if (d.level > level) { why = "Requires level " + d.level + "."; return false; }
            return true;
        }
        /// <summary>Equips the item in a bag slot; whatever was in that equipment slot goes back to the same bag slot (to the first free
        /// ordinary slot when that one would not take it).</summary>
        public static bool Equip(EncounterProgress p, ItemDatabase db, int bagIndex, int level, out string why)
        {
            why = null; if (bagIndex < 0 || bagIndex >= p.bag.Count || p.bag[bagIndex].Empty) return false;
            var d = db.Get(p.bag[bagIndex].item); if (!CanEquip(d, level, out why)) return false;
            int slot = ItemDatabase.SlotIndex(d.slot);
            var old = p.equipment[slot]; int back = bagIndex;
            if (!old.Empty && bagIndex >= BagSize && !Accepts(p, db, bagIndex, db.Get(old.item))) { back = FirstFree(p); if (back < 0) { why = "Your bags are full."; return false; } }
            p.equipment[slot] = new ItemStack { item = d.id, count = 1 };
            p.bag[bagIndex] = new ItemStack();
            if (!old.Empty) p.bag[back] = new ItemStack { item = old.item, count = 1 };
            return true;
        }
        /// <summary>Takes off the item in an equipment slot into the first free ordinary bag slot (or a chosen one; never a trade bag's).</summary>
        public static bool Unequip(EncounterProgress p, int slot, int toBag = -1)
        {
            if (slot < 0 || slot >= p.equipment.Count || p.equipment[slot].Empty) return false;
            if (toBag < 0) toBag = FirstFree(p);
            if (toBag < 0 || toBag >= p.bag.Count || toBag >= BagSize) return false;
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
        /// <summary>What "Sell junk" takes: junk, and anything of poor quality. Materials and tools are common quality, so they stay.</summary>
        public static bool IsJunk(ItemDef d) { return d != null && (d.kind == "junk" || d.quality == 0); }
        /// <summary>Sells every stack of junk in the bags. Returns the gold earned.</summary>
        public static int SellJunk(EncounterProgress p, ItemDatabase db)
        {
            int total = 0;
            for (int i = 0; i < p.bag.Count; i++) if (IsJunk(db.Get(p.bag[i].item))) total += Sell(p, db, i);
            return total;
        }
        public static int Price(ItemDef d) { return Math.Max(1, d.value * 4); }
        public static bool Buy(EncounterProgress p, ItemDatabase db, string item, out string why)
        {
            why = null; var d = db.Get(item); if (d == null) { why = "Not for sale."; return false; }
            int price = Price(d);
            if (p.gold < price) { why = "You need " + price + " crowns."; return false; }
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
