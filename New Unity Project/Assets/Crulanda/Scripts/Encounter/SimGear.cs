using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// What a sim wears (Phase 5.2 round 2, 2026-10-06): generated gear (ItemDatabase "gen." ids) at its level, from its gear seed,
    /// so it is the same every time it is seen. Each slot's seed is tried until the piece suits the class: mail for the Warrior and
    /// the Paladin (Coif, Spaulders, Hauberk, Gauntlets, Greaves, Sabatons), leather for the Ranger (Cap, Mantle, Jerkin, Gloves,
    /// Breeches, Boots), cloth for the Mage and the Druid (Hood, Mantle, Tunic, Wraps, Leggings, Shoes). The Warrior and the
    /// Paladin carry a blade (or a hatchet or a cudgel) and a shield; the others keep their class's own staff, bow or wand.
    /// Quality by level: common at first, uncommon from level 4 or by luck, rare for some from level 8.
    /// </summary>
    public static class SimGear
    {
        static readonly Dictionary<string, string[]> Wanted = new Dictionary<string, string[]> {
            { "heavy", new[] { "Coif", "Torc", "Spaulders", "Hauberk", "Gauntlets", "Greaves", "Sabatons" } },
            { "leather", new[] { "Cap", "Cord", "Mantle", "Jerkin", "Gloves", "Breeches", "Boots" } },
            { "cloth", new[] { "Hood", "Pendant", "Mantle", "Tunic", "Wraps", "Leggings", "Shoes" } } };
        static readonly string[] ArmourSlots = { "head", "neck", "shoulders", "chest", "hands", "legs", "feet" };

        public static string Weight(string classId)
        {
            return classId == "class.warrior" || classId == "class.paladin" ? "heavy" : classId == "class.ranger" || classId == "class.rogue" || classId == "class.shaman" || classId == "class.scoundrel" ? "leather" : "cloth";
        }
        /// <summary>The class that always tanks a party (SimCompanion.Role, the Warrior).</summary>
        public static bool Tank(string classId) { return classId == "class.warrior"; }
        /// <summary>
        /// Whether the sim goes without the piece in armour slot <paramref name="i"/>: a few go bareheaded or without a neck piece or a
        /// mantle, but never the tank, which owns all seven (2026-10-16: the Warrior tank bareheaded had the least armour of the mail in the party).
        /// </summary>
        static bool Bare(SimAdventurer s, int i) { return i <= 2 && !Tank(s.classId) && !RaidGeared(s) && Mathf.Abs(s.gearSeed + i * 13) % 5 == 0; }
        /// <summary>A raid's gear (Chris: "all members should have lvl 20 gear", "or at least good gear", "that is the point of a raid"): a sim
        /// with this many bought upgrades wears a rare piece of its level in every slot, none bare.</summary>
        public const int RaidGear = 4;
        public static bool RaidGeared(SimAdventurer s) { return s != null && s.gearBonus >= RaidGear; }
        public static bool CarriesWeapon(string classId) { return classId == "class.warrior" || classId == "class.paladin" || classId == "class.rogue" || classId == "class.scoundrel"; }
        /// <summary>Whether a piece by its name is of the class's weight (a Hauberk for the mail classes, a Jerkin for leather, a Tunic for cloth).</summary>
        public static bool Suits(string classId, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var w in Wanted[Weight(classId)]) if (name.Contains(w)) return true;
            return false;
        }
        public static int QualityFor(SimAdventurer s, int slot)
        {
            // The tank buys its armour first (Chris, 2026-10-11: "give the warrior some better gear"; 2026-10-16, with the sims' health counting
            // their gear's Stamina, so the tank stays the sturdiest of a party): uncommon at once, rare from level 8.
            if (Tank(s.classId) || RaidGeared(s) && s.level >= 8) return s.level >= 8 ? 3 : 2;   // the tank, and a raid-geared sim: rare
            int roll = Mathf.Abs(s.gearSeed * 31 + slot * 977) % 100;
            roll -= 18 * s.gearBonus;   // bought upgrades (SimEconomy): better odds of the better piece
            if (s.level >= 8 && roll < 25) return 3;
            if (s.level >= 4 || roll < 20) return roll < 70 ? 2 : 1;
            return 1;
        }
        /// <summary>The item ids it wears, one per slot it fills (the first of a slot's seeds whose piece suits the class).</summary>
        public static List<string> For(SimAdventurer s, ItemDatabase items)
        {
            var ids = new List<string>(); if (s == null || items == null) return ids;
            var want = Wanted[Weight(s.classId)];
            for (int i = 0; i < ArmourSlots.Length; i++)
            {
                var forged = SimEconomy.Worn(s, ArmourSlots[i]); if (forged != null && items.Get(forged) != null) { ids.Add(forged); continue; }   // its own make (5.3c)
                // A few sims go bareheaded or without a neck piece or a mantle: not everyone owns all seven.
                if (Bare(s, i)) continue;
                var id = Pick(items, ArmourSlots[i], s, i, d => d.name.Contains(want[i]));
                if (id != null) ids.Add(id);
            }
            if (s.classId == "class.rogue" && items.Get("item.rogues_dirk") != null && items.Get("item.rogues_second_dirk") != null) { ids.Add("item.rogues_dirk"); ids.Add("item.rogues_second_dirk"); }   // a Rogue's pair of dirks (2026-10-11); a database without them falls through to a weapon at the level
            else if (s.classId == "class.scoundrel" && items.Get("item.scoundrels_dirk") != null) ids.Add("item.scoundrels_dirk");   // a Scoundrel's one dirk (2026-10-14)
            else if (CarriesWeapon(s.classId))
            {
                var fm = SimEconomy.Worn(s, "mainhand"); var main = fm != null && items.Get(fm) != null ? fm : Pick(items, "mainhand", s, 7, d => true); if (main != null) ids.Add(main);
                var fo = SimEconomy.Worn(s, "offhand"); var off = fo != null && items.Get(fo) != null ? fo : Pick(items, "offhand", s, 8, d => d.name.Contains("Shield") || d.name.Contains("Buckler")); if (off != null) ids.Add(off);
            }
            return ids;
        }
        static string Pick(ItemDatabase items, string slot, SimAdventurer s, int slotIndex, System.Func<ItemDef, bool> fits)
        {
            int q = QualityFor(s, slotIndex);
            for (int k = 0; k < 40; k++)
            {
                var id = ItemDatabase.GearId(slot, s.level, q, s.gearSeed + slotIndex * 101 + k * 7);
                var d = items.Get(id); if (d != null && fits(d)) return id;
            }
            return null;
        }
        // ---------- what a piece is worth to a sim (the Exchange: CAPITAL_HUB.md, "Sims list and buy") ----------
        /// <summary>The piece a sim wears in a slot as <see cref="For"/> dresses it (its own make or what it won or bought, else its
        /// generated piece), or null for a bare slot (a few go bareheaded; only the Warrior and the Paladin carry a weapon and a shield
        /// that can change: the Rogue's and the Scoundrel's dirks are their own).</summary>
        public static string InSlot(SimAdventurer s, string slot, ItemDatabase items)
        {
            if (s == null || items == null) return null;
            int i = System.Array.IndexOf(ArmourSlots, slot);
            var forged = SimEconomy.Worn(s, slot); if (forged != null && items.Get(forged) != null && (i >= 0 || TakesWeapons(s.classId))) return forged;
            if (i >= 0)
            {
                if (Bare(s, i)) return null;
                var want = Wanted[Weight(s.classId)]; return Pick(items, slot, s, i, d => d.name.Contains(want[i]));
            }
            if (slot == "mainhand" && TakesWeapons(s.classId)) return Pick(items, "mainhand", s, 7, d => true);
            if (slot == "offhand" && TakesWeapons(s.classId)) return Pick(items, "offhand", s, 8, d => d.name.Contains("Shield") || d.name.Contains("Buckler"));
            return null;
        }
        /// <summary>The classes whose weapon and shield are bought, made and won (the Warrior and the Paladin): the rest keep their own.</summary>
        public static bool TakesWeapons(string classId) { return classId == "class.warrior" || classId == "class.paladin"; }
        static ProfessionDatabase madeFor; static readonly Dictionary<string, string> madeBy = new Dictionary<string, string>();
        /// <summary>The weight of a piece: "heavy", "leather" or "cloth"; "" for a piece anyone may wear (a neck piece, a named find).
        /// A made one by its trade (blacksmithing heavy, leatherworking leather, weaving cloth), else by the words in its name (hide,
        /// pelt, skin: leather; wool, robe, woven: cloth). A generated piece goes by <see cref="Suits"/> instead.</summary>
        public static string WeightOf(ItemDef d, ProfessionDatabase trades)
        {
            if (d == null || d.slot == "neck") return "";
            if (trades != null)
            {
                if (madeFor != trades) { madeFor = trades; madeBy.Clear(); foreach (var r in trades.Recipes) if (r != null && !string.IsNullOrEmpty(r.output) && !madeBy.ContainsKey(r.output)) madeBy[r.output] = r.profession; }
                if (madeBy.TryGetValue(d.id, out var trade)) return trade == "blacksmithing" ? "heavy" : trade == "leatherworking" ? "leather" : trade == "weaving" ? "cloth" : "";
            }
            string n = (d.name ?? "").ToLowerInvariant();
            if (n.Contains("hide") || n.Contains("pelt") || n.Contains("skin") || n.Contains("leather")) return "leather";
            if (n.Contains("wool") || n.Contains("robe") || n.Contains("woven") || n.Contains("homespun")) return "cloth";
            return "";
        }
        /// <summary>
        /// Whether a sim may wear a piece: gear of its level or under, not another class's set piece; armour of its class's weight
        /// (a generated piece by its name, a made one by its trade; a named find anyone's, as it is yours); a neck piece anyone's; a
        /// weapon or a shield only for the Warrior and the Paladin (no bow, staff or wand: theirs are a blade, a hatchet or a cudgel).
        /// </summary>
        public static bool Fits(SimAdventurer s, ItemDef d, ProfessionDatabase trades)
        {
            if (s == null || d == null || d.kind != "gear" || d.level > s.level || !Inventory.ClassMayWear(d, s.classId)) return false;
            string n = (d.name ?? "").ToLowerInvariant();
            if (d.slot == "mainhand") return TakesWeapons(s.classId) && !n.Contains("bow") && !n.Contains("staff") && !n.Contains("stave") && !n.Contains("wand");
            if (d.slot == "offhand") return TakesWeapons(s.classId) && (n.Contains("shield") || n.Contains("buckler") || n.Contains("targe") || n.Contains("heater"));
            if (d.slot == "neck") return true;
            if (System.Array.IndexOf(ArmourSlots, d.slot) < 0) return false;
            if (d.id != null && d.id.StartsWith("gen.")) return Suits(s.classId, d.name);
            string w = WeightOf(d, trades);
            return w == "" || w == Weight(s.classId);
        }
        /// <summary>What a piece is worth on a body: its armour, twice its weapon damage and twice its stats.</summary>
        public static int Power(ItemDef d)
        {
            if (d == null) return 0;
            return d.armor + 2 * d.weaponDamage + 2 * (d.stamina + d.strength + d.agility + d.intellect + d.spirit);
        }
        /// <summary>Whether a piece fits a sim and is better than what it wears in that slot (by a twentieth and a point at least).</summary>
        public static bool IsUpgrade(SimAdventurer s, ItemDef d, ItemDatabase items, ProfessionDatabase trades)
        {
            if (!Fits(s, d, trades)) return false;
            var now = InSlot(s, d.slot, items); if (now == d.id) return false;
            int was = Power(items?.Get(now));
            return Power(d) > was + Mathf.Max(1, was / 20);
        }
        /// <summary>Dresses a figure in the sim's gear.</summary>
        public static void Dress(ActorVisual visual, SimAdventurer s, ItemDatabase items)
        {
            if (visual == null || items == null) return;
            var looks = GearLooks.Load(); if (looks == null) return;
            visual.ApplyGearIds(For(s, items).ToArray(), items, looks);
        }
    }
}
