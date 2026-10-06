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
    /// Named loot (step L2): the loot files are item files, read again by LoadLoot for their drop lists, sets and effects. A camp
    /// body rolls the named drops too (signature lists give a piece you do not hold yet; a unique you hold never drops again and
    /// cannot be taken twice), and what you wear adds its effects and set bonuses (ApplyGearEffects, OnKillEffects, RestRegen,
    /// coins and luck on the roll), worked out again from the equipment whenever it changes, so nothing new is saved.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The named loot (DESIGN.md 3), or null when the loot files are missing or invalid; then a body holds the base roll (ItemDatabase.RollLoot).</summary>
        public LootDatabase Loot { get; private set; }
        static LootDatabase lootCache; static ItemDatabase lootCacheItems; static bool lootCacheBad;
        /// <summary>
        /// Reads the drop lists, sets and effects of the item files (the loot files among them) against the items, after LoadItems,
        /// and registers the named looks with the gear looks. Cached while the items are the same. A bad file is logged once and
        /// leaves the named loot off (the items, vendors and base roll stay).
        /// </summary>
        void LoadLoot()
        {
            Loot = null;
            if (Items == null || content == null || content.itemFiles == null) return;
            if (lootCacheItems != Items)
            {
                lootCache = null; lootCacheBad = false; lootCacheItems = Items;
                try { var texts = new List<string>(); foreach (var f in content.itemFiles) if (f != null) texts.Add(f.text); lootCache = LootDatabase.Parse(texts, Items, GearLooks.Load()); }
                catch (System.ArgumentException e) { Debug.LogError("Loot content invalid:\n" + e.Message); lootCacheBad = true; }
            }
            if (lootCacheBad || lootCache == null || lootCache.Gear.Count == 0) return;
            Loot = lootCache;
            Loot.ZoneName = id => { var z = Zone != null ? Zone.AllZones().Find(d => d.id == "zone." + id) : null; return z != null ? z.displayName : null; };
        }
        /// <summary>A unique piece you already carry or wear: it never drops again and cannot be taken from a body.</summary>
        public bool HoldsUnique(string item) { return Loot != null && Loot.IsUnique(item) && Inventory.Has(Progress, item); }
        /// <summary>What taking a unique piece you already hold says.</summary>
        public static string UniqueLine(string name) { return "You already have " + name + ". It is unique."; }

        /// <summary>What worn named gear and sets add up to now (empty without named loot); worked out again by ApplyGearEffects.</summary>
        public GearEffectTotals GearFx { get; private set; } = new GearEffectTotals();
        readonly object gearEffectSource = new object();
        /// <summary>
        /// The effects of the named gear worn and the set bonuses switched on (GearEffects.Compute): stat effects become modifiers
        /// from their own source, cleared and added again each time (the end of ApplyEquipment calls it); the rest is kept in GearFx.
        /// </summary>
        void ApplyGearEffects()
        {
            if (Player == null) return;
            Player.Stats.RemoveModifiersFromSource(gearEffectSource);
            GearFx = GearEffects.Compute(Progress, Items, Loot, gearEffectSource);
            if (GearFx.modifiers.Count > 0) Player.Stats.AddModifiers(GearFx.modifiers);
        }
        /// <summary>A kill's gear effects: health and resource back, while you stand.</summary>
        void OnKillEffects()
        {
            if (Player == null || !Player.IsAlive) return;
            if (GearFx.onKillHeal > 0) Player.Health.ApplyHealing(GearFx.onKillHeal);
            if (GearFx.onKillPower > 0 && Player.Resource != null) Player.Resource.Pool.Change(GearFx.onKillPower);
        }
        /// <summary>Health back each second out of combat: 7, and what worn gear adds (capped at +20).</summary>
        int RestRegen { get { return 7 + GearFx.restRegen; } }
        /// <summary>
        /// A new character in the zones starts with the Tempered Trailblade in hand (loot step L2, the owner's call), so the empty
        /// hands of the gear looks are never the first thing seen. Saved characters are never changed; the old Quiet Trail keeps
        /// its blade as the sentries' reward.
        /// </summary>
        void ArmNewCharacter()
        {
            if (Zone == null || Progress == null || string.IsNullOrEmpty(content.itemId)) return;
            Inventory.Ensure(Progress);
            // The class's own first weapon (Round 22, note 48: "mage should start with wand or staff"): a wand for the Mage, a bow for the Ranger, the blade for the rest.
            string starter = ClassDef != null && ClassDef.id == "class.mage" ? "item.apprentice_wand" : ClassDef != null && ClassDef.id == "class.ranger" ? "item.hunting_bow" : content.itemId;
            if (Progress.equipment[(int)EquipSlot.MainHand].Empty) Progress.equipment[(int)EquipSlot.MainHand] = new ItemStack { item = starter, count = 1 };
        }
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
        /// Rolls a camp body's loot as it dies: coins by level (three times as many from an elite, more with worn coin effects), and
        /// the kind's drops with a chance of gear (ItemDatabase.RollLoot, or LootDatabase.Roll once the named loot is loaded: owned
        /// is what the Armoury has found or you carry or wear (step L3), held what you carry or wear, luck is your gear's, and the
        /// epic pity counters are the save's lootLuck); then lights its beacon. The kill counts toward the Armoury (ArmouryLog.Killed).
        /// </summary>
        public void RollCorpse(EncounterEnemy corpse)
        {
            if (corpse == null) return;
            int coins = 1 + corpse.actor.Level * 2 + Random.Range(0, corpse.actor.Level + 2);
            if (corpse.Elite) coins *= 3;
            if (GearFx.coins > 0) coins = Mathf.RoundToInt(coins * (1 + GearFx.coins));
            var drops = new List<LootDrop>();
            if (Items != null)
            {
                var rng = new System.Random(Random.Range(0, int.MaxValue));
                System.Func<string, bool> has = id => Inventory.Has(Progress, id);
                System.Func<string, bool> owned = id => (Armoury != null && Armoury.IsFound(id)) || has(id);
                if (Loot != null)
                {
                    var context = LootContext.From(corpse.persistentId, Zone != null ? Zone.Zone.camps : null, corpse.actor.Level, corpse.Elite);
                    if (Armoury != null) { Armoury.Killed(context); armouryTallies = null; }
                    drops = Loot.Roll(context, Items, owned, GearFx.luck, Progress.lootLuck, rng, has);
                }
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
            foreach (var d in drops) { var i = Items.Get(d.item); if (i != null) best = Mathf.Max(best, Mathf.Clamp(i.quality, 0, ItemDatabase.MaxQuality)); }
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
        bool CanTakeAny(EncounterEnemy e) { return e.Drops == null || e.Coins > 0 || Items == null || e.Drops.Exists(d => Inventory.Room(Progress, Items, d.item) > 0 && !HoldsUnique(d.item)); }
        public void CloseLoot() { lootBody = null; }
        /// <summary>Take all [E]: the coins, then each thing in order; what does not fit stays on the body. The window shuts either way.</summary>
        public void TakeAllLoot()
        {
            var body = LootBody; if (body == null || body.Drops == null || body.actor.IsAlive) { CloseLoot(); return; }
            TakeCoins(body); int kept = 0;
            for (int i = 0; i < body.Drops.Count; i++) kept += TakeDrop(body, i, out _);
            body.Drops.RemoveAll(d => d.count <= 0);
            if (kept > 0) Message(BodyKeepsLine);
            CloseLoot(); AfterTake(body);
        }
        /// <summary>A click on one row of the window: that thing into the bags. False when it stayed on the body (or there is no such row).</summary>
        public bool TakeLoot(int index)
        {
            var body = LootBody; if (body == null || body.Drops == null || index < 0 || index >= body.Drops.Count) return false;
            int left = TakeDrop(body, index, out bool refused); body.Drops.RemoveAll(d => d.count <= 0);
            if (left > 0) Message(BodyKeepsLine);
            AfterTake(body); return left == 0 && !refused;
        }
        /// <summary>A click on the coins row.</summary>
        public void TakeLootCoins() { var body = LootBody; if (body == null || body.Drops == null || body.actor.IsAlive) { CloseLoot(); return; } TakeCoins(body); AfterTake(body); }
        void TakeCoins(EncounterEnemy body)
        {
            if (body.Coins <= 0) return;
            Progress.gold += body.Coins; Message("Looted " + body.Coins + " crowns."); body.Coins = 0;
        }
        /// <summary>
        /// One thing off the body into the bags (Inventory.Add: onto its stacks, then a worn trade bag of its class, then the bags).
        /// Returns how many stayed on the body for want of room. A unique piece you already hold is <paramref name="refused"/>: it
        /// stays on the body and says why.
        /// </summary>
        int TakeDrop(EncounterEnemy body, int i, out bool refused)
        {
            refused = false;
            var drop = body.Drops[i]; if (drop.count <= 0) return 0;
            if (HoldsUnique(drop.item)) { refused = true; Message(UniqueLine(ItemName(drop.item))); return 0; }
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
        // ---------- treasure chests (2026-10-05) ----------
        /// <summary>How long an emptied chest takes to fill again, in seconds (not saved: a restart fills them all).</summary>
        public const float ChestRefill = 1200;
        static readonly Dictionary<string, float> chestReadyAt = new Dictionary<string, float>();
        /// <summary>Tests only: every chest is full again.</summary>
        public static void RefillChests() { chestReadyAt.Clear(); }
        public const string ChestEmptyLine = "The chest is empty. Someone has been here before you.";
        public const string BagsFullForChestLine = "Your bags are full: what was in the chest stays in it.";
        /// <summary>
        /// E on a treasure chest: it opens (ChestLid) and what is in it goes into the bags, with the loot call-outs: crowns by the
        /// zone's level, a piece of gear of the zone's top level (uncommon 55%, rare 34%, epic 11%; never legendary: those are the
        /// bosses' alone) and sometimes a potion. Empty, it says so until it fills again (<see cref="ChestRefill"/>). Not in a fight.
        /// </summary>
        public void OpenChest(Crulanda.World.ZoneInteractable i)
        {
            if (InCombat) { Message(FightingLine); return; }
            string key = i.Key(Zone.Zone.id);
            if (chestReadyAt.TryGetValue(key, out float at) && Time.time < at) { Message(ChestEmptyLine); return; }
            chestReadyAt[key] = Time.time + ChestRefill;
            var lid = i.root != null ? i.root.GetComponentInChildren<Crulanda.World.ChestLid>() : null;
            if (lid != null) { lid.Open(); StartCoroutine(ShutLater(lid, ChestRefill)); }
            int level = Mathf.Max(1, Zone.Zone.levelMax);
            int coins = level * 4 + gatherRng.Next(level * 4 + 1); Progress.gold += coins; Message("Looted " + coins + " crowns.");
            if (Items != null)
            {
                double roll = gatherRng.NextDouble(); int q = roll < .11 ? 4 : roll < .45 ? 3 : 2;
                var gear = ItemDatabase.GearId(ItemDatabase.SlotIds[gatherRng.Next(ItemDatabase.SlotIds.Length)], level, q, gatherRng.Next(10000));
                if (Inventory.Add(Progress, Items, gear, 1) == 0) Received(gear, 1); else Message(BagsFullForChestLine);
                if (gatherRng.NextDouble() < .4 && Items.Get("potion.healing") != null && Inventory.Add(Progress, Items, "potion.healing", 1) == 0) Received("potion.healing", 1);
            }
            Save(false);
        }
        System.Collections.IEnumerator ShutLater(Crulanda.World.ChestLid lid, float seconds) { yield return new WaitForSeconds(seconds); if (lid != null) lid.Close(); }

        /// <summary>The rarity call-out: a chat line in the item's quality colour, and a "RARE" or "EPIC" toast over its name.</summary>
        void Received(string item, int count)
        {
            var d = Items.Get(item); int q = Mathf.Clamp(d.quality, 0, ItemDatabase.MaxQuality);
            LootLine("Looted: " + d.name + (count > 1 ? " x" + count : "") + ".", q == 0 ? ItemDatabase.QualityColors[0] : LootBeacon.Colour(q));
            if (q >= 3) ShowToast(q >= 5 ? "LEGENDARY" : q >= 4 ? "EPIC" : "RARE", d.name);
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
        /// <summary>The window shuts when you walk off (beyond LootReach), die, or the body is gone or back on its feet. The Armoury sweeps here too, twice a second.</summary>
        void TickLoot()
        {
            SweepArmoury();
            if (lootBody == null) { lootBody = null; return; }   // a body destroyed by a load reads as null: forget it
            if (!Player.IsAlive || lootBody.actor == null || lootBody.actor.IsAlive || lootBody.Drops == null || Vector3.Distance(Player.transform.position, lootBody.transform.position) > LootReach
                || Conversation != null || VendorNpc != null || TradesOpen || QuestBookOpen || MapOpen) CloseLoot();   // a window drawn under it opened: it gives way
        }
    }
}
