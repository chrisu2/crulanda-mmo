using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The sims' trades and purse (Phase 5.3c, 2026-10-06; playtest note 63: "sell their goods or sell their resources to upgrade
    /// themselves ... or they make their own gear with tradeskills and recipes"). Each class gathers one thing (Warriors and
    /// Paladins mine, Rangers, Rogues and Scoundrels cut wood, the rest pick herbs) and crafts one (Warriors and Paladins smith;
    /// Rangers, Rogues and Scoundrels work leather from the hides of what they hunt; Mages, Archivists and Clerics weave; Druids,
    /// Shamans and Salt-Menders make simples: Phase 8.6); what they gather goes in their goods (SimAdventurer.goodIds/goodCounts),
    /// sold at the merchant's stall for the items' values; a crafter with the makings for a recipe makes the piece and wears it
    /// (wornSlots/wornIds), or keeps a spare it already wears better to sell to you (never at the stall); a brewer or cook sells what
    /// it makes; and coin buys better gear (gearBonus raises the quality of what SimGear makes). The merchant sells charcoal, vials,
    /// fleeces and salt. Found recipes (a leaf's) are the player's: the sims make only what their skill teaches. Each keeps a pack of food
    /// and simples for the bosses, made or bought (Stock, Kept; BossBuffs), eaten and drunk at the buff-up before a boss. The Exchange
    /// (p9-exchange2): the meat of a hunt is cooked at the hearth (Cook), a leaf turns up now and then (LeafFor; never learned, never sold
    /// at the stall), and a piece bought there is put on (PutOn), the old one kept as a spare to sell.
    /// </summary>
    public static class SimEconomy
    {
        public static string GatherTrade(string classId) { return classId == "class.warrior" || classId == "class.paladin" ? "mining" : Leatherworker(classId) ? "woodcutting" : "herbalism"; }
        public static string CraftTrade(string classId)
        {
            if (classId == "class.warrior" || classId == "class.paladin") return "blacksmithing";
            if (Leatherworker(classId)) return "leatherworking";
            if (classId == "class.mage" || classId == "class.archivist" || classId == "class.cleric") return "weaving";
            return "alchemy";
        }
        /// <summary>The leather-wearers who work leather (the plan's kits: Ranger, Rogue, Scoundrel).</summary>
        static bool Leatherworker(string classId) { return classId == "class.ranger" || classId == "class.rogue" || classId == "class.scoundrel"; }
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
        /// <summary>Its goods less its pack for the bosses (<see cref="Kept"/>) and any leaves it found (kept for the Exchange): what sends it to the stall.</summary>
        public static int SellableCount(SimAdventurer s, ItemDatabase items) { int n = 0; for (int i = 0; i < s.goodIds.Count; i++) if (!IsLeaf(items?.Get(s.goodIds[i]))) n += Mathf.Max(0, s.goodCounts[i] - Kept(s, items, s.goodIds[i])); return n; }
        /// <summary>A found recipe's leaf (kind "recipe"): a sim never learns one (found recipes are the player's), so every leaf it finds is a spare for the Exchange.</summary>
        public static bool IsLeaf(ItemDef d) { return d != null && d.kind == "recipe"; }
        public static int Value(ItemDatabase items, string id) { var d = items?.Get(id); return d != null && d.value > 0 ? d.value : 1; }
        /// <summary>Sells everything it carries (materials, potions and food, never gear: a spare piece is kept for trading with you and the Exchange,
        /// nor a leaf: that is for the Exchange too) for the items' values; its pack for the bosses (<see cref="Kept"/>: a dish, an elixir, a draught,
        /// a feast) is kept too.</summary>
        public static int SellAll(SimAdventurer s, ItemDatabase items)
        {
            int coin = 0;
            for (int i = s.goodIds.Count - 1; i >= 0; i--)
            {
                var d = items?.Get(s.goodIds[i]); if (d != null && (d.kind == "gear" || IsLeaf(d))) continue;
                int keep = Mathf.Min(s.goodCounts[i], Kept(s, items, s.goodIds[i])), sell = s.goodCounts[i] - keep;
                coin += Value(items, s.goodIds[i]) * sell;
                if (keep > 0) s.goodCounts[i] = keep; else { s.goodIds.RemoveAt(i); s.goodCounts.RemoveAt(i); }
            }
            s.coin += coin; return coin;
        }

        // ---------- the pack for the bosses (Chris, 2026-10-15: "we should use food buffs, alchemy buffs, class buffs,etc to beat the bosses") ----------
        /// <summary>How many of an item a sim keeps for its buff-up before a boss (BossBuffs.Pack at its level's tier, and one feast of its tier):
        /// never sold at the stall.</summary>
        public static int Kept(SimAdventurer s, ItemDatabase items, string item)
        {
            if (s == null || items == null || string.IsNullOrEmpty(item)) return 0;
            var d = items.Get(item); if (d == null || d.kind != "consumable") return 0;
            if (BossBuffs.IsFeast(d)) { var f = BossBuffs.FeastFor(s.level, items); return f != null && f.id == item ? 1 : 0; }
            foreach (var (id, count) in BossBuffs.Pack(s.classId, s.level, items)) if (id == item) return count;
            if (s.level >= RaidGroup.MinSimLevel) foreach (var (id, count) in RaidCall.Kit) if (id == item) return count;   // its salt and harpoons for the raid (WORLD_BOSS_1 2.3)
            return 0;
        }
        /// <summary>
        /// Fills its pack for the bosses (at the stall, and after an hour's work away): what it made itself counts (an alchemist's tinctures and
        /// draughts); what is short it buys from other adventurers at a fair price (BossBuffs.FairPrice, twice the value) while its coin
        /// lasts, its part's elixir first, then its dish, then its draught; a feast of its tier only when it has <see cref="BossBuffs.FeastPurse"/>
        /// times the feast's price to spare after. So a richer sim party is better buffed. The coin spent.
        /// </summary>
        public static int Stock(SimAdventurer s, ItemDatabase items)
        {
            if (s == null || items == null) return 0;
            int spent = 0; var pack = BossBuffs.Pack(s.classId, s.level, items);
            // The elixir first (the pack lists dish, elixir, draught).
            pack.Sort((a, b) => Order(items.Get(a.id)).CompareTo(Order(items.Get(b.id))));
            foreach (var (id, count) in pack)
            {
                var d = items.Get(id); int price = BossBuffs.FairPrice(d);
                while (Count(s, id) < count && s.coin >= price) { s.coin -= price; spent += price; Add(s, id, 1); }
            }
            var feast = BossBuffs.FeastFor(s.level, items);
            if (feast != null && Count(s, feast.id) == 0 && s.coin >= BossBuffs.FairPrice(feast) * BossBuffs.FeastPurse) { int price = BossBuffs.FairPrice(feast); s.coin -= price; spent += price; Add(s, feast.id, 1); }
            return spent;
        }
        static int Order(ItemDef d) { return d == null ? 9 : d.buff != null && d.buff.kind != "food" ? 0 : d.food ? 1 : 2; }
        public const int MaxGearBonus = 2;
        /// <summary>What a step up in gear costs at the merchant: more at higher levels and for each step already taken.</summary>
        public static int UpgradeCost(SimAdventurer s) { return 25 * (s.gearBonus + 1) * Mathf.Max(1, s.level); }
        public static bool CanUpgrade(SimAdventurer s) { return s.gearBonus < MaxGearBonus && s.coin >= UpgradeCost(s); }
        public static bool Upgrade(SimAdventurer s) { if (!CanUpgrade(s)) return false; s.coin -= UpgradeCost(s); s.gearBonus++; return true; }
        /// <summary>What the merchant sells a crafter as needed: charcoal and vials (as before), fleeces for the weavers and salt for the tanning.</summary>
        public static bool MerchantSells(string item) { return item == "mat.charcoal" || item == "mat.vial" || item == "mat.fleece" || item == "mat.salt"; }
        /// <summary>The most spare pieces of gear a sim keeps for trading with you.</summary>
        public const int MaxSpares = 2;
        static int Spares(SimAdventurer s, ItemDatabase items) { int n = 0; foreach (var id in s.goodIds) { var d = items?.Get(id); if (d != null && d.kind == "gear") n++; } return n; }
        /// <summary>The recipe it can make now at its craft trade: a taught one (found recipes are the player's), inputs in hand (the merchant's
        /// charcoal, vials, fleeces and salt bought as needed), skill enough; gear it does not already wear better first, then bars, then
        /// potions and dishes to sell, then a spare piece to sell to you (none while it holds <see cref="MaxSpares"/>, or one of that piece).</summary>
        public static RecipeDef Craftable(SimAdventurer s, ProfessionDatabase db, ItemDatabase items)
        {
            string trade = CraftTrade(s.classId); if (trade == null || db == null) return null;
            RecipeDef best = null; int bestScore = int.MinValue; int skill = Skill(s);
            foreach (var r in db.Recipes)
            {
                if (r.profession != trade || r.skill > skill || r.found) continue;
                bool ok = true; int buy = 0;
                foreach (var i in r.inputs)
                {
                    int have = Count(s, i.item);
                    if (have >= i.count) continue;
                    if (MerchantSells(i.item)) { buy += (i.count - have) * Value(items, i.item); continue; }   // the merchant's
                    ok = false; break;
                }
                if (!ok || buy > s.coin) continue;
                var outDef = items?.Get(r.output); int score;
                if (outDef != null && outDef.kind == "gear")
                {
                    int slot = ItemDatabase.SlotIndex(outDef.slot); string worn = Worn(s, outDef.slot);
                    var wornDef = worn != null ? items.Get(worn) : null;
                    if (wornDef != null && wornDef.level >= outDef.level)
                    {
                        // No better than what it wears: a spare to sell, now and then.
                        if (Spares(s, items) >= MaxSpares || Count(s, r.output) > 0) continue;
                        score = 5 + r.skill / 10;
                    }
                    else score = 100 + outDef.level * 5 + (slot == 7 ? 3 : 0);
                }
                else if (r.output.StartsWith("mat.")) score = 10 + r.skill / 10;   // a bar for the next piece
                else score = 20 + r.skill / 10;   // a potion or a dish to sell
                if (score > bestScore) { best = r; bestScore = score; }
            }
            return best;
        }
        /// <summary>Makes it: inputs out (bought ones paid for), the output in; a gear piece is worn in its slot unless what it wears there is as
        /// good, when it goes in the goods as a spare to sell to you.</summary>
        public static string Craft(SimAdventurer s, RecipeDef r, ItemDatabase items)
        {
            foreach (var i in r.inputs)
            {
                int have = Count(s, i.item), need = i.count - have;
                if (need > 0) s.coin -= need * Value(items, i.item);   // the merchant's charcoal, vial, fleece or salt
                Add(s, i.item, -Mathf.Min(have, i.count));
            }
            var outDef = items?.Get(r.output);
            if (outDef != null && outDef.kind == "gear")
            {
                var worn = items.Get(Worn(s, outDef.slot));
                if (worn == null || worn.level < outDef.level) Wear(s, outDef.slot, r.output); else Add(s, r.output, 1);
            }
            else Add(s, r.output, Mathf.Max(1, r.count));
            return r.output;
        }
        /// <summary>Puts on a piece it bought on the Exchange (or won): worn in its slot; the piece it made or bought before for that slot goes
        /// in its goods as a spare to sell (while it holds fewer than <see cref="MaxSpares"/>), else to the stall for its value.</summary>
        public static void PutOn(SimAdventurer s, ItemDef d, ItemDatabase items)
        {
            if (s == null || d == null || d.kind != "gear") return;
            var old = Worn(s, d.slot);
            Wear(s, d.slot, d.id);
            if (old == null || old == d.id || items?.Get(old) == null) return;
            if (Spares(s, items) < MaxSpares) Add(s, old, 1); else s.coin += Value(items, old);
        }
        public static string Worn(SimAdventurer s, string slot) { int i = s.wornSlots.IndexOf(slot); return i < 0 ? null : s.wornIds[i]; }
        public static void Wear(SimAdventurer s, string slot, string id)
        {
            int i = s.wornSlots.IndexOf(slot);
            if (i < 0) { s.wornSlots.Add(slot); s.wornIds.Add(id); } else s.wornIds[i] = id;
        }
        /// <summary>The ids of the materials a sim gathers (NodeDef.item) that its gather trade covers at its skill.</summary>
        public static bool Gathers(SimAdventurer s, NodeDef n) { return n != null && n.profession == GatherTrade(s.classId) && n.skill <= Skill(s); }
        /// <summary>Where in the village a recipe is made: the forge for a forge's work, the herbalist's bench for the rest of the crafts
        /// (simples, leather, cloth, a bowyer's whittling), the inn's hearth for a fire's alone (VillageLife places).</summary>
        public static string Workplace(RecipeDef r)
        {
            var stations = ProfessionDatabase.Stations(r?.station);
            if (System.Array.IndexOf(stations, "forge") >= 0) return "forge";
            if (System.Array.IndexOf(stations, "bench") >= 0) return "bench";
            return "inn";
        }
        /// <summary>The hide a kill of this tag gives a leatherworker (the first hide on its loot tables at that level), or null.</summary>
        public static string HideFrom(ItemDatabase items, string tag, int level)
        {
            if (items == null || string.IsNullOrEmpty(tag)) return null;
            foreach (var t in items.Loot)
            {
                if (t.tag != tag || t.elite || level < t.levelMin || level > t.levelMax) continue;
                foreach (var e in t.entries) if (e != null && Inventory.IsHide(items.Get(e.item))) return e.item;
            }
            return null;
        }
        /// <summary>The meat a hunting sim brings home from an hour away (SimPopulation.LiveAway), by its level's beasts: boar, wolf, bear, hound, venison.</summary>
        public static string MeatFor(int level) { return level <= 5 ? "junk.boar_meat" : level <= 9 ? "mat.wolf_haunch" : level <= 12 ? "mat.bear_haunch" : level <= 13 ? "mat.hound_flank" : "mat.venison"; }
        /// <summary>What the inn sells a cook at its value: flour and salt.</summary>
        public static bool CookBuys(string item) { return item == "mat.flour" || item == "mat.salt"; }
        /// <summary>
        /// Cooks a dish at the inn's hearth from the meat it brought home (any sim: a cook's fire needs no trade): the hardest taught cooking
        /// recipe within its skill whose makings it has (flour and salt bought at the inn as needed, while its coin lasts). The dish goes in its
        /// goods: its pack's dish first (<see cref="Kept"/>), the rest to sell at the stall or on the Exchange. The dish's id, or null.
        /// </summary>
        public static string Cook(SimAdventurer s, ProfessionDatabase db, ItemDatabase items)
        {
            if (s == null || db == null) return null;
            RecipeDef best = null; int skill = Skill(s);
            foreach (var r in db.Recipes)
            {
                if (r.profession != "cooking" || r.found || r.skill > skill || items?.Get(r.output) == null) continue;
                bool ok = true; int buy = 0;
                foreach (var i in r.inputs) { int have = Count(s, i.item); if (have >= i.count) continue; if (CookBuys(i.item)) buy += (i.count - have) * Value(items, i.item); else { ok = false; break; } }
                if (!ok || buy > s.coin) continue;
                if (best == null || r.skill > best.skill) best = r;
            }
            if (best == null) return null;
            foreach (var i in best.inputs) { int have = Count(s, i.item); if (have < i.count) s.coin -= (i.count - have) * Value(items, i.item); Add(s, i.item, -Mathf.Min(have, i.count)); }
            Add(s, best.output, Mathf.Max(1, best.count));
            return best.output;
        }
        /// <summary>A leaf a hunting sim turns up now and then (from the bosses and caches it beats away from you): one whose recipe is within a
        /// skill of its level's, picked by <paramref name="pick"/>. Null when the leaves are not loaded.</summary>
        public static string LeafFor(int level, ProfessionDatabase db, ItemDatabase items, int pick)
        {
            if (db == null || items == null) return null;
            var found = new List<string>(); int skill = 8 * Mathf.Max(1, level) + 10;
            foreach (var d in items.Items.Values)
            {
                if (!IsLeaf(d) || string.IsNullOrEmpty(d.teaches)) continue;
                var r = db.Recipes.Find(x => x.id == d.teaches); if (r != null && r.skill <= skill) found.Add(d.id);
            }
            if (found.Count == 0) return null;
            found.Sort(string.CompareOrdinal);
            return found[Mathf.Abs(pick) % found.Count];
        }
        /// <summary>The hide a leatherworker sim brings home from an hour's hunting away (SimPopulation.LiveAway), by its level's beasts.</summary>
        public static string HideFor(int level) { return level <= 5 ? "hide.boar" : level <= 9 ? "junk.wolf_pelt" : level <= 12 ? "hide.bear" : level <= 13 ? "junk.ash_hide" : "junk.moss_hide"; }
    }
}
