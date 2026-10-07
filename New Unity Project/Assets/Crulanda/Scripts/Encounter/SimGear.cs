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
            return classId == "class.warrior" || classId == "class.paladin" ? "heavy" : classId == "class.ranger" ? "leather" : "cloth";
        }
        public static bool CarriesWeapon(string classId) { return classId == "class.warrior" || classId == "class.paladin"; }
        /// <summary>Whether a piece by its name is of the class's weight (a Hauberk for the mail classes, a Jerkin for leather, a Tunic for cloth).</summary>
        public static bool Suits(string classId, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var w in Wanted[Weight(classId)]) if (name.Contains(w)) return true;
            return false;
        }
        public static int QualityFor(SimAdventurer s, int slot)
        {
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
                if (i <= 2 && Mathf.Abs(s.gearSeed + i * 13) % 5 == 0) continue;
                var id = Pick(items, ArmourSlots[i], s, i, d => d.name.Contains(want[i]));
                if (id != null) ids.Add(id);
            }
            if (CarriesWeapon(s.classId))
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
        /// <summary>Dresses a figure in the sim's gear.</summary>
        public static void Dress(ActorVisual visual, SimAdventurer s, ItemDatabase items)
        {
            if (visual == null || items == null) return;
            var looks = GearLooks.Load(); if (looks == null) return;
            visual.ApplyGearIds(For(s, items).ToArray(), items, looks);
        }
    }
}
