using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The sims' trades and purse (Phase 5.3c, 2026-10-06; playtest note 63: "sell their goods or sell their resources to upgrade
    /// themselves ... or they make their own gear with tradeskills and recipes"). Each class gathers one thing (Warriors and
    /// Paladins mine, Rangers cut wood, Druids and Mages pick herbs) and crafts one (Warriors and Paladins smith, Druids and Mages
    /// brew); what they gather goes in their goods (SimAdventurer.goodIds/goodCounts), sold at the merchant's stall for the items'
    /// values; a smith with the bars for a recipe forges the piece and wears it (wornSlots/wornIds); a brewer sells the potions;
    /// and coin buys better gear (gearBonus raises the quality of what SimGear makes). The merchant sells charcoal and vials.
    /// </summary>
    public static class SimEconomy
    {
        public static string GatherTrade(string classId) { return classId == "class.warrior" || classId == "class.paladin" ? "mining" : classId == "class.ranger" ? "woodcutting" : "herbalism"; }
        public static string CraftTrade(string classId) { return classId == "class.warrior" || classId == "class.paladin" ? "blacksmithing" : classId == "class.ranger" ? null : "alchemy"; }
        /// <summary>Its skill in a trade: by its level (a level-3 sim is at 24, past the second tier's 20).</summary>
        public static int Skill(SimAdventurer s) { return 8 * Mathf.Max(1, s.level); }
        public static int Count(SimAdventurer s, string item) { int i = s.goodIds.IndexOf(item); return i < 0 ? 0 : s.goodCounts[i]; }
        public static void Add(SimAdventurer s, string item, int n)
        {
            if (string.IsNullOrEmpty(item) || n == 0) return;
            int i = s.goodIds.IndexOf(item);
            if (i < 0) { if (n > 0) { s.goodIds.Add(item); s.goodCounts.Add(n); } return; }
            s.goodCounts[i] += n; if (s.goodCounts[i] <= 0) { s.goodIds.RemoveAt(i); s.goodCounts.RemoveAt(i); }
        }
        public static int GoodsCount(SimAdventurer s) { int n = 0; foreach (var c in s.goodCounts) n += c; return n; }
        public static int Value(ItemDatabase items, string id) { var d = items?.Get(id); return d != null && d.value > 0 ? d.value : 1; }
        /// <summary>Sells everything it carries (materials and potions, never gear) for the items' values.</summary>
        public static int SellAll(SimAdventurer s, ItemDatabase items)
        {
            int coin = 0;
            for (int i = s.goodIds.Count - 1; i >= 0; i--)
            {
                var d = items?.Get(s.goodIds[i]); if (d != null && d.kind == "gear") continue;
                coin += Value(items, s.goodIds[i]) * s.goodCounts[i]; s.goodIds.RemoveAt(i); s.goodCounts.RemoveAt(i);
            }
            s.coin += coin; return coin;
        }
        public const int MaxGearBonus = 2;
        /// <summary>What a step up in gear costs at the merchant: more at higher levels and for each step already taken.</summary>
        public static int UpgradeCost(SimAdventurer s) { return 25 * (s.gearBonus + 1) * Mathf.Max(1, s.level); }
        public static bool CanUpgrade(SimAdventurer s) { return s.gearBonus < MaxGearBonus && s.coin >= UpgradeCost(s); }
        public static bool Upgrade(SimAdventurer s) { if (!CanUpgrade(s)) return false; s.coin -= UpgradeCost(s); s.gearBonus++; return true; }
        /// <summary>The recipe it can make now at its craft trade: inputs in hand (the merchant's charcoal and vials bought as needed), skill enough;
        /// gear it does not already wear better first, then bars and potions.</summary>
        public static RecipeDef Craftable(SimAdventurer s, ProfessionDatabase db, ItemDatabase items)
        {
            string trade = CraftTrade(s.classId); if (trade == null || db == null) return null;
            RecipeDef best = null; int bestScore = int.MinValue; int skill = Skill(s);
            foreach (var r in db.Recipes)
            {
                if (r.profession != trade || r.skill > skill) continue;
                bool ok = true; int buy = 0;
                foreach (var i in r.inputs)
                {
                    int have = Count(s, i.item);
                    if (have >= i.count) continue;
                    if (i.item == "mat.charcoal" || i.item == "mat.vial") { buy += (i.count - have) * Value(items, i.item); continue; }   // the merchant's
                    ok = false; break;
                }
                if (!ok || buy > s.coin) continue;
                var outDef = items?.Get(r.output); int score;
                if (outDef != null && outDef.kind == "gear")
                {
                    int slot = ItemDatabase.SlotIndex(outDef.slot); string worn = Worn(s, outDef.slot);
                    var wornDef = worn != null ? items.Get(worn) : null;
                    if (wornDef != null && wornDef.level >= outDef.level) continue;   // no better than what it forged already
                    score = 100 + outDef.level * 5 + (slot == 7 ? 3 : 0);
                }
                else if (r.output.StartsWith("mat.")) score = 10 + r.skill / 10;   // a bar for the next piece
                else score = 20 + r.skill / 10;   // a potion to sell
                if (score > bestScore) { best = r; bestScore = score; }
            }
            return best;
        }
        /// <summary>Makes it: inputs out (bought ones paid for), the output in; a gear piece is worn in its slot.</summary>
        public static string Craft(SimAdventurer s, RecipeDef r, ItemDatabase items)
        {
            foreach (var i in r.inputs)
            {
                int have = Count(s, i.item), need = i.count - have;
                if (need > 0) s.coin -= need * Value(items, i.item);   // the merchant's charcoal or vial
                Add(s, i.item, -Mathf.Min(have, i.count));
            }
            var outDef = items?.Get(r.output);
            if (outDef != null && outDef.kind == "gear") Wear(s, outDef.slot, r.output); else Add(s, r.output, Mathf.Max(1, r.count));
            return r.output;
        }
        public static string Worn(SimAdventurer s, string slot) { int i = s.wornSlots.IndexOf(slot); return i < 0 ? null : s.wornIds[i]; }
        public static void Wear(SimAdventurer s, string slot, string id)
        {
            int i = s.wornSlots.IndexOf(slot);
            if (i < 0) { s.wornSlots.Add(slot); s.wornIds.Add(id); } else s.wornIds[i] = id;
        }
        /// <summary>The ids of the materials a sim gathers (NodeDef.item) that its gather trade covers at its skill.</summary>
        public static bool Gathers(SimAdventurer s, NodeDef n) { return n != null && n.profession == GatherTrade(s.classId) && n.skill <= Skill(s); }
    }
}
