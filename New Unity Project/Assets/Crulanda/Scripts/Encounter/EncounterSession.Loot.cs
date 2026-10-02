using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Loot that feels like loot (loot DESIGN.md 4, step L1). A camp mob's loot is rolled as it dies (RollCorpse), so its body can
    /// show what it holds (LootBeacon). E on a body that holds gear, or anything better than common, opens the loot window
    /// (OpenLoot); Take all [E] or a click on a row takes things through Inventory.Add (a worn trade bag of the item's class
    /// first), and what does not fit stays on the body until there is room. A body with only coins, junk and common things is
    /// emptied with the one press. Everything taken is a chat line in its quality's colour, and a rare or epic piece raises a
    /// "RARE" or "EPIC" toast over its name. Nothing here is saved: a body keeps its loot until it is emptied or the mob respawns.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The named loot (DESIGN.md 3). Null until step L2 loads it; while null a body holds the base roll (ItemDatabase.RollLoot).</summary>
        public LootDatabase Loot { get; private set; }
        /// <summary>What taking says when the bags could not hold everything.</summary>
        public const string BodyKeepsLine = "Your bags are full. The rest stays on the body.";
        /// <summary>The loot window shuts when you are further than this from the body.</summary>
        public const float LootReach = 5;
        static readonly List<LootDrop> NoDrops = new List<LootDrop>();
        EncounterEnemy lootBody;
        /// <summary>The body whose loot window is open (null when it is shut).</summary>
        public EncounterEnemy LootBody { get { return lootBody != null ? lootBody : null; } }
        public bool LootOpen { get { return lootBody != null; } }
        /// <summary>What lies on the open body, in order (empty when the window is shut).</summary>
        public IReadOnlyList<LootDrop> LootItems { get { return lootBody != null && lootBody.Drops != null ? lootBody.Drops : NoDrops; } }
        /// <summary>The coins on the open body.</summary>
        public int LootCoins { get { return lootBody != null ? lootBody.Coins : 0; } }

        /// <summary>
        /// Rolls a camp body's loot as it dies: coins by level (three times as many from an elite), and the kind's drops with a
        /// chance of gear (ItemDatabase.RollLoot, or LootDatabase.Roll once the named loot is loaded); then lights its beacon.
        /// </summary>
        public void RollCorpse(EncounterEnemy corpse)
        {
            if (corpse == null) return;
            int coins = 1 + corpse.actor.Level * 2 + Random.Range(0, corpse.actor.Level + 2);
            if (corpse.Elite) coins *= 3;
            var drops = new List<LootDrop>();
            if (Items != null)
            {
                var rng = new System.Random(Random.Range(0, int.MaxValue));
                if (Loot != null) drops = Loot.Roll(LootContext.From(corpse.persistentId, Zone != null ? Zone.Zone.camps : null, corpse.actor.Level, corpse.Elite), Items, id => Inventory.Has(Progress, id), 0, null, rng);
                else
                {
                    var parts = corpse.persistentId.Split('.'); string tag = parts.Length > 1 ? parts[1] : "any";
                    foreach (var (item, count) in Items.RollLoot(tag, corpse.actor.Level, corpse.Elite, rng)) drops.Add(new LootDrop(item, count));
                }
            }
            PutLoot(corpse, coins, drops);
        }
        /// <summary>Lays coins and things on a camp body (items the database does not know are left off) and lights its beacon. Tests and the loot capture set a body's loot with it.</summary>
        public void PutLoot(EncounterEnemy corpse, int coins, IEnumerable<LootDrop> drops)
        {
            if (corpse == null) return;
            corpse.Coins = Mathf.Max(0, coins); corpse.Drops = new List<LootDrop>(); corpse.Looted = false;
            if (drops != null) foreach (var d in drops) if (d.count > 0 && Items != null && Items.Get(d.item) != null) corpse.Drops.Add(d);
            ShowBeacon(corpse);
        }
        /// <summary>The best quality among these drops (0 for none, coins only, or junk).</summary>
        public int BestQuality(IReadOnlyList<LootDrop> drops)
        {
            int best = 0; if (drops == null || Items == null) return best;
            foreach (var d in drops) { var i = Items.Get(d.item); if (i != null) best = Mathf.Max(best, Mathf.Clamp(i.quality, 0, 4)); }
            return best;
        }
        void ShowBeacon(EncounterEnemy body)
        {
            bool empty = body.Coins <= 0 && (body.Drops == null || body.Drops.Count == 0);
            if (empty || body.actor.IsAlive) LootBeacon.Clear(body); else LootBeacon.Show(body, BestQuality(body.Drops));
        }
        /// <summary>A body opens the window when it holds gear or anything better than common; coins, junk and common things are taken at once.</summary>
        bool WorthAWindow(List<LootDrop> drops)
        {
            foreach (var d in drops) { var i = Items?.Get(d.item); if (i != null && (i.kind == "gear" || i.quality >= 2)) return true; }
            return false;
        }
        /// <summary>E on a camp body: the loot window when it holds something worth a look, else everything into the bags with the one press.</summary>
        public void OpenLoot(EncounterEnemy corpse)
        {
            if (corpse == null || !corpse.Camp || !CanLoot(corpse)) return;
            if (corpse.Drops == null) RollCorpse(corpse);   // it died while the party was being restored: roll now
            lootBody = corpse;
            if (!WorthAWindow(corpse.Drops)) { TakeAllLoot(); return; }
            // The windows drawn before it would take its clicks, so they shut while it is open (TickLoot shuts it if one opens).
            Conversation = null; if (VendorNpc != null) CloseVendor(); TradesOpen = false; QuestBookOpen = false; ReadingDocument = null; MapOpen = false;
        }
        /// <summary>Whether E on this camp body would take anything now: coins, a thing the bags have room for, or loot not yet rolled. A body with nothing that fits leaves E to Mira, villagers, nodes and doors.</summary>
        bool CanTakeAny(EncounterEnemy e) { return e.Drops == null || e.Coins > 0 || Items == null || e.Drops.Exists(d => Inventory.Room(Progress, Items, d.item) > 0); }
        public void CloseLoot() { lootBody = null; }
        /// <summary>Take all [E]: the coins, then each thing in order; what does not fit stays on the body. The window shuts either way.</summary>
        public void TakeAllLoot()
        {
            var body = LootBody; if (body == null || body.Drops == null || body.actor.IsAlive) { CloseLoot(); return; }
            TakeCoins(body); int kept = 0;
            for (int i = 0; i < body.Drops.Count; i++) kept += TakeDrop(body, i);
            body.Drops.RemoveAll(d => d.count <= 0);
            if (kept > 0) Message(BodyKeepsLine);
            CloseLoot(); AfterTake(body);
        }
        /// <summary>A click on one row of the window: that thing into the bags. False when it stayed on the body (or there is no such row).</summary>
        public bool TakeLoot(int index)
        {
            var body = LootBody; if (body == null || body.Drops == null || index < 0 || index >= body.Drops.Count) return false;
            int left = TakeDrop(body, index); body.Drops.RemoveAll(d => d.count <= 0);
            if (left > 0) Message(BodyKeepsLine);
            AfterTake(body); return left == 0;
        }
        /// <summary>A click on the coins row.</summary>
        public void TakeLootCoins() { var body = LootBody; if (body == null || body.Drops == null || body.actor.IsAlive) { CloseLoot(); return; } TakeCoins(body); AfterTake(body); }
        void TakeCoins(EncounterEnemy body)
        {
            if (body.Coins <= 0) return;
            Progress.gold += body.Coins; Message("Looted " + body.Coins + " gold."); body.Coins = 0;
        }
        /// <summary>One thing off the body into the bags (Inventory.Add: onto its stacks, then a worn trade bag of its class, then the bags). Returns how many stayed on the body.</summary>
        int TakeDrop(EncounterEnemy body, int i)
        {
            var drop = body.Drops[i]; if (drop.count <= 0) return 0;
            int left = Items != null ? Inventory.Add(Progress, Items, drop.item, drop.count) : drop.count;
            if (drop.count - left > 0) Received(drop.item, drop.count - left);
            body.Drops[i] = new LootDrop(drop.item, left);
            return left;
        }
        /// <summary>An emptied body is done with (looted, its beacon out, its window shut); otherwise its beacon shows the best of what is left.</summary>
        void AfterTake(EncounterEnemy body)
        {
            if (body.Coins <= 0 && (body.Drops == null || body.Drops.Count == 0)) { body.Looted = true; LootBeacon.Clear(body); if (lootBody == body) CloseLoot(); }
            else ShowBeacon(body);
        }
        /// <summary>The rarity call-out: a chat line in the item's quality colour, and a "RARE" or "EPIC" toast over its name.</summary>
        void Received(string item, int count)
        {
            var d = Items.Get(item); int q = Mathf.Clamp(d.quality, 0, 4);
            LootLine("Looted: " + d.name + (count > 1 ? " x" + count : "") + ".", q == 0 ? ItemDatabase.QualityColors[0] : LootBeacon.Colour(q));
            if (q >= 3) ShowToast(q >= 4 ? "EPIC" : "RARE", d.name);
        }
        /// <summary>Chat lines shown in a colour of their own (the loot call-outs), by their text.</summary>
        readonly Dictionary<string, Color> lineColours = new Dictionary<string, Color>();
        void LootLine(string text, Color colour)
        {
            Message(text); lineColours[text] = colour;
            if (lineColours.Count > 24) { var gone = new List<string>(); foreach (var k in lineColours.Keys) if (!Messages.Contains(k)) gone.Add(k); foreach (var k in gone) lineColours.Remove(k); }
        }
        /// <summary>The colour a chat line is drawn in: its loot colour, or <paramref name="plain"/>.</summary>
        public Color LineColour(string line, Color plain) { return line != null && lineColours.TryGetValue(line, out var c) ? c : plain; }
        /// <summary>The window shuts when you walk off (beyond LootReach), die, or the body is gone or back on its feet.</summary>
        void TickLoot()
        {
            if (lootBody == null) { lootBody = null; return; }   // a body destroyed by a load reads as null: forget it
            if (!Player.IsAlive || lootBody.actor == null || lootBody.actor.IsAlive || lootBody.Drops == null || Vector3.Distance(Player.transform.position, lootBody.transform.position) > LootReach
                || Conversation != null || VendorNpc != null || TradesOpen || QuestBookOpen || MapOpen) CloseLoot();   // a window drawn under it opened: it gives way
        }
    }
}
