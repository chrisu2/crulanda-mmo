using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Abilities;
using Crulanda.Combat;
using EntityId = Crulanda.Core.EntityId;

namespace Crulanda.Encounter
{
    public sealed class EncounterSession : MonoBehaviour
    {
        public EncounterContent content;
        [NonSerialized] public string SaveDirectoryOverride;
        /// <summary>The throwaway save folder of a --crulanda-temp-save run (static: it must outlive the scene reload that travel does).</summary>
        static string tempSaveRoot;
        /// <summary>Same as the --crulanda-temp-save flag (tests set it). <see cref="ResetTempSave"/> forgets the folder.</summary>
        public static bool UseTempSave;
        public static string TempSaveRoot { get { return tempSaveRoot; } }
        public static void ResetTempSave() { UseTempSave = false; tempSaveRoot = null; }
        public EncounterProgress Progress { get; private set; }
        public Actor Player { get; private set; }
        public HealerCompanion Companion { get; private set; }
        public readonly List<EncounterEnemy> Enemies = new List<EncounterEnemy>();
        public EncounterEnemy Target { get; private set; }
        public Camera View { get; private set; }
        public bool Paused { get; private set; }
        public bool InventoryOpen;
        public bool BuildOpen;
        /// <summary>M: zone map; scrolling out switches to the world map.</summary>
        public bool MapOpen { get { return mapOpen; } set { mapOpen = value; if (!value) MapWorld = false; } }
        bool mapOpen;
        public bool MapWorld;
        /// <summary>Tests/tools: force the class played this session instead of the remembered character.</summary>
        public static string StartClassOverride;
        public ClassDefinition ClassDef { get; private set; }
        public TalentTree Talents { get; private set; }
        public ClassKit Kit { get; private set; }
        public WarriorKit Warrior { get { return Kit as WarriorKit; } }
        // ---------- zone (generated world) or the legacy Quiet Trail defaults ----------
        public Crulanda.World.ZoneBuilder Zone { get { return Crulanda.World.ZoneBuilder.Active; } }
        public string ZoneTitle { get { return Zone != null ? Zone.Zone.displayName : "The Quiet Trail"; } }
        public float Leash { get { return Zone != null && Zone.Zone.spawns.leash > 0 ? Zone.Zone.spawns.leash : 17; } }
        public Vector3 RecoveryPoint { get { return Zone != null ? Zone.Ground(Zone.Zone.spawns.recovery, 1.1f) : new Vector3(0, 1.1f, -13); } }
        Vector3 StartPoint { get { return Zone != null ? Zone.Ground(Zone.Zone.spawns.player, 1.1f) : new Vector3(0, 1.1f, -13); } }
        Vector3 CompanionPoint { get { return Zone != null ? Zone.Ground(Zone.Zone.spawns.companion, 1.05f) : new Vector3(3, 1.05f, -12); } }
        public string Objective(int index, string fallback)
        { var o = Zone?.Zone.objectives; return o != null && index < o.Length && !string.IsNullOrEmpty(o[index]) ? o[index] : fallback; }
        public DruidKit Druid { get { return Kit as DruidKit; } }
        // ---------- quests ----------
        /// <summary>The player's quest log (null when quest content is missing or invalid).</summary>
        public QuestLog Quests { get; private set; }
        public bool QuestBookOpen;
        /// <summary>Open quest conversation: who, and what they offer. Null when closed.</summary>
        public QuestConversation Conversation;
        /// <summary>Chronicle page being read in the quest book (null = none).</summary>
        [NonSerialized] public string ReadingDocument;   // not serialized: Unity would start it as "" instead of null
        public sealed class QuestConversation { public string npc; public QuestDef selected; public List<(QuestDef quest, QuestStatus status)> entries; public Vector3 where; }
        public string ZoneId { get { return Zone != null ? Zone.Zone.id : null; } }
        static QuestDatabase questCache; static EncounterContent questCacheFor;
        void StartQuests()
        {
            if (content.questFiles == null || content.questFiles.Length == 0) return;
            try
            {
                if (questCache == null || questCacheFor != content)
                {
                    var texts = new List<string>(); foreach (var f in content.questFiles) if (f != null) texts.Add(f.text);
                    questCache = QuestDatabase.Parse(texts); questCacheFor = content;
                }
                Quests = new QuestLog(questCache, Progress);
                Quests.Say = (text, speaker) => {
                    if (speaker == null) { Message(text); return; }
                    Message(speaker + ": " + text);
                    if (speaker == "Mira") return;
                    VillageLife.Active?.Find(speaker)?.Say(text, 7);
                };
                Quests.Revealed = id => { ReadingDocument = id; QuestBookOpen = true; };
            }
            catch (ArgumentException e) { Debug.LogError("Quest content invalid:\n" + e.Message); Quests = null; }
        }
        /// <summary>Starts this zone's automatic quests and catches every active step up with the world (flags, kills, items).</summary>
        public void ReconcileQuests()
        {
            if (Quests == null) return;
            if (Zone != null) Quests.StartAutomatic(Zone.Zone.id);
            Quests.Reconcile(QuestFlag, target => {
                int n = 0; foreach (var e in Progress.enemies) if (e.dead && QuestLog.Matches(target, e.id)) n++; return n;
            });
        }
        bool QuestFlag(string flag)
        {
            if (flag == "recruited") return Progress.recruited;
            if (flag != null && flag.StartsWith("equipped:")) return Inventory.IsEquipped(Progress, flag.Substring(9));
            return false;
        }
        /// <summary>Talk to someone about quests. Returns true if they had quest business (so no small talk).</summary>
        public bool QuestTalk(string npc, Vector3 where)
        {
            if (Quests == null || Zone == null) return false;
            bool handled = Quests.TalkTo(npc);
            ReconcileQuests();
            var list = Quests.For(npc, Zone.Zone.id, Progress.Level);
            // Merchants open on the list (so "Browse wares" is always one click away); others go straight to a single quest.
            bool shop = IsVendor(VillageLife.Active?.Find(npc));
            if (list.Count > 0) { Conversation = new QuestConversation { npc = npc, entries = list, selected = list.Count == 1 && !shop ? list[0].quest : null, where = where }; return true; }
            return handled;
        }
        public void AcceptQuest(QuestDef q)
        {
            if (Quests == null || Zone == null || !Quests.Accept(q, Zone.Zone.id)) return;
            ReconcileQuests(); Save(false); ReopenConversation();
        }
        public void CompleteQuest(QuestDef q)
        {
            if (Quests == null) return;
            int before = Progress.Level;
            if (!Quests.TurnIn(q, out _)) return;
            if (Progress.Level > before) { ApplyLevel(); Player.Health.ApplyHealing(Player.Health.Pool.Max); Message("Level " + Progress.Level + "! Talent points are waiting [B]."); }
            ReconcileQuests(); Save(false); ReopenConversation();
        }
        void ReopenConversation()
        {
            if (Conversation == null) return;
            var list = Quests.For(Conversation.npc, ZoneId, Progress.Level);
            if (list.Count == 0) { Conversation = null; return; }
            Conversation.entries = list; Conversation.selected = list.Count == 1 ? list[0].quest : null;
        }
        // ---------- items: bags, equipment, loot, merchants ----------
        public ItemDatabase Items { get; private set; }
        public bool CharacterOpen;
        /// <summary>Merchant whose wares are open (null = none), and what they sell.</summary>
        public string VendorNpc { get; private set; }
        public List<string> VendorStock { get; private set; } = new List<string>();
        static ItemDatabase itemCache; static EncounterContent itemCacheFor;
        readonly object equipmentSource = new object();
        float potionReadyAt, foodUntil, foodPerSecond;
        void LoadItems()
        {
            if (content.itemFiles == null || content.itemFiles.Length == 0) return;
            try
            {
                if (itemCache == null || itemCacheFor != content)
                {
                    var texts = new List<string>(); foreach (var f in content.itemFiles) if (f != null) texts.Add(f.text);
                    itemCache = ItemDatabase.Parse(texts); itemCacheFor = content;
                }
                Items = itemCache;
            }
            catch (ArgumentException e) { Debug.LogError("Item content invalid:\n" + e.Message); Items = null; }
        }
        /// <summary>Worn gear feeds the stat system: primary stats and armor as modifiers, weapon damage as the equipment bonus.</summary>
        public void ApplyEquipment()
        {
            if (Player == null || playerStats == null) return;
            Inventory.Ensure(Progress);
            Player.Stats.RemoveModifiersFromSource(equipmentSource);
            if (Items == null) { playerStats.SetEquipmentBonus(Progress.equipment[(int)EquipSlot.MainHand].item == content.itemId ? content.weaponBonus : 0); return; }
            var t = Inventory.Totals(Progress, Items);
            var mods = new List<StatModifier>();
            void Add(StatType s, int v) { if (v != 0) mods.Add(new StatModifier(s, ModifierOp.Flat, v, equipmentSource)); }
            Add(StatType.Stamina, t.stamina); Add(StatType.Strength, t.strength); Add(StatType.Agility, t.agility);
            Add(StatType.Intellect, t.intellect); Add(StatType.Spirit, t.spirit); Add(StatType.Armor, t.armor);
            Player.Stats.AddModifiers(mods);
            playerStats.SetEquipmentBonus(t.weaponDamage);
        }
        public string ItemName(string id) { var d = Items?.Get(id); return d != null ? d.name : id; }
        /// <summary>Camp corpses: coins by level, the kind's drops and a chance of gear, straight into the bags.</summary>
        void LootCamp(EncounterEnemy corpse)
        {
            corpse.Looted = true;
            int coins = 1 + corpse.actor.Level * 2 + UnityEngine.Random.Range(0, corpse.actor.Level + 2);
            if (corpse.Elite) coins *= 3;
            Progress.gold += coins;
            var found = new List<string>(); var lost = new List<string>();
            if (Items != null)
            {
                string tag = corpse.persistentId.Split('.').Length > 1 ? corpse.persistentId.Split('.')[1] : "any";
                foreach (var (item, count) in Items.RollLoot(tag, corpse.actor.Level, corpse.Elite, new System.Random(UnityEngine.Random.Range(0, int.MaxValue))))
                {
                    int left = Inventory.Add(Progress, Items, item, count);
                    var d = Items.Get(item); string n = d != null ? d.name : item;
                    if (left < count) found.Add(n + (count - left > 1 ? " x" + (count - left) : ""));
                    if (left > 0) lost.Add(n);
                }
            }
            Message("Looted " + coins + " gold" + (found.Count > 0 ? ", " + string.Join(", ", found) : "") + ".");
            if (lost.Count > 0) Message("Your bags are full. Left behind: " + string.Join(", ", lost) + ".");
        }
        public bool EquipFromBag(int bagIndex)
        {
            if (Items == null && Progress.bag[bagIndex].item != content.itemId) return false;
            var d = Items?.Get(Progress.bag[bagIndex].item);
            if (d != null && d.kind == "consumable") return UseItem(bagIndex);
            string why;
            bool ok = Items != null ? Inventory.Equip(Progress, Items, bagIndex, Progress.Level, out why) : LegacyEquip(bagIndex, out why);
            if (!ok) { if (why != null) Message(why); return false; }
            ApplyEquipment();
            Message((d != null ? d.name : content.itemName) + " equipped.");
            ReconcileQuests(); Save(false); return true;
        }
        bool LegacyEquip(int bagIndex, out string why)
        {
            why = null; var old = Progress.equipment[(int)EquipSlot.MainHand];
            Progress.equipment[(int)EquipSlot.MainHand] = new ItemStack { item = content.itemId, count = 1 };
            Progress.bag[bagIndex] = old.Empty ? new ItemStack() : old; return true;
        }
        public bool UnequipSlot(int slot, int toBag = -1)
        {
            if (!Inventory.Unequip(Progress, slot, toBag)) { Message(Inventory.FreeSlots(Progress) == 0 ? "Your bags are full." : "Can't put that there."); return false; }
            ApplyEquipment(); Save(false); return true;
        }
        public void MoveBag(int a, int b) { if (Items != null) { Inventory.Move(Progress, Items, a, b); } }
        public void DestroyBag(int i)
        {
            if (i < 0 || i >= Progress.bag.Count || Progress.bag[i].Empty) return;
            Message("Destroyed " + ItemName(Progress.bag[i].item) + "."); Inventory.Destroy(Progress, i); Save(false);
        }
        /// <summary>Potions heal at once (30 s cooldown); food heals over 10 s and only out of combat.</summary>
        public bool UseItem(int i)
        {
            var d = Items?.Get(Progress.bag[i].item); if (d == null || d.kind != "consumable") return false;
            if (d.food)
            {
                if (InCombat) { Message("You can't eat while fighting."); return false; }
                foodUntil = Time.time + 10; foodPerSecond = d.heal / 10f;
            }
            else
            {
                if (Time.time < potionReadyAt) { Message("Potion not ready (" + Mathf.CeilToInt(potionReadyAt - Time.time) + "s)."); return false; }
                potionReadyAt = Time.time + 30; Player.Health.ApplyHealing(d.heal);
            }
            Message("Used " + d.name + "."); Inventory.Remove(Progress, d.id, 1); return true;
        }
        float foodCarry;
        void TickItems()
        {
            if (Time.time < foodUntil)
            {
                if (InCombat) { foodUntil = 0; return; }
                foodCarry += foodPerSecond * Time.deltaTime; int whole = Mathf.FloorToInt(foodCarry);
                if (whole > 0) { foodCarry -= whole; Player.Health.ApplyHealing(whole); }
            }
            if (VendorNpc != null && Vector3.Distance(Player.transform.position, vendorAt) > 6) CloseVendor();   // walked away
        }
        public float PotionCooldown { get { return Mathf.Max(0, potionReadyAt - Time.time); } }
        // Merchants.
        Vector3 vendorAt;
        public bool IsVendor(Villager v) { return v != null && Items != null && Items.StockFor(v.Name, v.Role, 1).Count > 0; }
        public void OpenVendor(Villager v)
        {
            if (Items == null || v == null) return;
            int band = Zone != null ? Zone.Zone.levelMax : 2;
            VendorStock = Items.StockFor(v.Name, v.Role, band); if (VendorStock.Count == 0) return;
            VendorNpc = v.Name; vendorAt = v.transform.position; InventoryOpen = true; Conversation = null; v.Hold(30);
        }
        public void CloseVendor() { VendorNpc = null; VendorStock = new List<string>(); }
        public void SellBag(int i)
        {
            if (VendorNpc == null || i < 0 || i >= Progress.bag.Count || Progress.bag[i].Empty) return;
            string n = ItemName(Progress.bag[i].item); int count = Progress.bag[i].count;
            int gold = Inventory.Sell(Progress, Items, i);
            Message("Sold " + n + (count > 1 ? " x" + count : "") + " for " + gold + " gold."); Save(false);
        }
        public void SellJunk()
        {
            if (VendorNpc == null) return;
            int total = 0;
            for (int i = 0; i < Progress.bag.Count; i++) { var d = Items.Get(Progress.bag[i].item); if (d != null && (d.kind == "junk" || d.quality == 0)) total += Inventory.Sell(Progress, Items, i); }
            Message(total > 0 ? "Sold your junk for " + total + " gold." : "Nothing worth selling as junk."); Save(false);
        }
        public void Buy(string item)
        {
            if (VendorNpc == null) return;
            if (Inventory.Buy(Progress, Items, item, out var why)) { Message("Bought " + ItemName(item) + "."); Save(false); }
            else Message(why);
        }

        // ---------- routing between zones (maps and breadcrumbs) ----------
        /// <summary>The exit out of this zone on the shortest road to <paramref name="zoneId"/> (by number of zones crossed), or null.</summary>
        public Crulanda.World.ZoneExit ExitToward(string zoneId)
        {
            if (Zone == null || string.IsNullOrEmpty(zoneId) || zoneId == Zone.Zone.id) return null;
            var all = Zone.AllZones(); var byId = new Dictionary<string, Crulanda.World.ZoneDefinition>();
            foreach (var z in all) byId[z.id] = z;
            // Breadth-first from each of our exits; the first exit whose road reaches the goal wins.
            var firstHop = new Dictionary<string, Crulanda.World.ZoneExit>(); var queue = new Queue<string>();
            foreach (var e in Zone.Zone.exits) if (byId.ContainsKey(e.to) && !firstHop.ContainsKey(e.to)) { firstHop[e.to] = e; queue.Enqueue(e.to); }
            firstHop[Zone.Zone.id] = null;
            while (queue.Count > 0)
            {
                var id = queue.Dequeue(); if (id == zoneId) return firstHop[id];
                foreach (var e in byId[id].exits) if (byId.ContainsKey(e.to) && !firstHop.ContainsKey(e.to)) { firstHop[e.to] = firstHop[id]; queue.Enqueue(e.to); }
            }
            return null;
        }
        /// <summary>Which zone a quest person lives in (residents and named villagers of every zone), or null for Mira and unknowns.</summary>
        public string ZoneOfPerson(string name)
        {
            if (Zone == null || string.IsNullOrEmpty(name) || name == "Mira" || name == "auto") return null;
            foreach (var z in Zone.AllZones())
            {
                if (z.life == null) continue;
                if (z.life.residents != null) foreach (var r in z.life.residents) if (r != null && r.name == name) return z.id;
                var names = z.life.names != null && z.life.names.Length > 0 ? z.life.names : VillageLife.DefaultNames;
                if (z.life.villagers > 0 && Array.IndexOf(names, name) >= 0 && Array.IndexOf(names, name) < Math.Max(z.life.villagers, 0)) return z.id;
                if (z.id == "zone.oakhaven" && Array.IndexOf(VillageLife.KeeperNamesList, name) >= 0) return z.id;
            }
            return null;
        }
        /// <summary>Where the current step of a quest wants you, if that is another zone (for tracker hints and map pins).</summary>
        public string QuestZoneElsewhere(QuestDef q, QuestState s)
        {
            if (Quests == null || Zone == null) return null;
            var step = Quests.CurrentStep(s);
            if (step == null) { var z = ZoneOfPerson(q.turnIn); return z != null && z != Zone.Zone.id ? z : null; }
            for (int i = 0; i < step.objectives.Length; i++)
            {
                if (Quests.ObjectiveDone(s, i)) continue;
                var o = step.objectives[i];
                string z = !string.IsNullOrEmpty(o.zone) ? o.zone : (o.type == "talk" || o.type == "deliver") ? ZoneOfPerson(o.target) : null;
                if (z != null && z != Zone.Zone.id) return z;
            }
            return null;
        }
        public string ZoneName(string id) { var z = Zone != null ? Zone.FindZone(id) : null; return z != null ? z.displayName : id; }
        public const float UseRange = 2.8f;
        /// <summary>A usable prop within reach (not emptied, not regrowing).</summary>
        public Crulanda.World.ZoneInteractable NearbyInteractable
        {
            get
            {
                if (Zone == null || Player == null) return null;
                Crulanda.World.ZoneInteractable best = null; float bestD = UseRange;
                foreach (var i in Zone.Interactables)
                {
                    // A hidden find registered as a prop too is searched through SecretSpots (DiscoveryLog), never as a quest prop.
                    if (i.kind == "secret" || Time.time < i.hiddenUntil || (i.once && Progress.usedInteractables.Contains(i.Key(Zone.Zone.id)))) continue;
                    var d = GroundDistance(i.position); if (d < bestD) { best = i; bestD = d; }   // the prop's origin is at its foot, the player's at the waist
                }
                return best;
            }
        }
        public void UseInteractable(Crulanda.World.ZoneInteractable i)
        {
            if (Quests == null) { Message("Nothing here you need."); return; }
            bool mattered = false;
            int before = CountQuestProgress();
            if (!string.IsNullOrEmpty(i.item) && Quests.Wants(i.item, i.name)) { Quests.GiveItem(i.item); mattered = true; }
            Quests.Notify("interact", i.name); ReconcileQuests();
            if (CountQuestProgress() != before) mattered = true;
            if (!mattered) { Message(string.IsNullOrEmpty(i.item) ? "You look it over, but find nothing you need right now." : "You don't need any of this right now."); return; }
            // Used up: once-only things stay done (saved); herbs regrow after a while.
            if (i.once) { Progress.usedInteractables.Add(i.Key(Zone.Zone.id)); if (i.Vanishes && i.root != null) HideProp(i.root); }
            else if (i.Vanishes) { i.hiddenUntil = Time.time + 90; if (i.root != null) StartCoroutine(HideFor(i.root, 90)); }
            Save(false);
        }
        int CountQuestProgress() { int n = 0; foreach (var s in Progress.quests) { n += s.step * 100; foreach (var c in s.counts) n += c; } return n + Progress.questsDone.Count * 10000; }
        static void HideProp(Transform t) { foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; }
        System.Collections.IEnumerator HideFor(Transform t, float seconds)
        {
            var rs = Array.FindAll(t.GetComponentsInChildren<Renderer>(), r => r.enabled); foreach (var r in rs) r.enabled = false;
            yield return new WaitForSeconds(seconds);
            foreach (var r in rs) if (r != null) r.enabled = true;
        }
        float nextVisitCheck;
        void TickQuests()
        {
            if (Quests == null || Zone == null || Time.time < nextVisitCheck) return;
            nextVisitCheck = Time.time + .5f;
            var p = Player.transform.position;
            Quests.CheckVisits(o => (string.IsNullOrEmpty(o.zone) || o.zone == Zone.Zone.id) && (!o.night || Crulanda.World.WorldClock.Darkness > .5f) && Vector2.Distance(new Vector2(p.x, p.z), o.at) <= o.radius);
            if (Conversation != null && Vector3.Distance(p, Conversation.where) > 6) Conversation = null;   // walked away
            // Emptied props stay empty after a reload.
            if (!emptiedHidden) { emptiedHidden = true; foreach (var i in Zone.Interactables) if (i.once && i.Vanishes && i.root != null && Progress.usedInteractables.Contains(i.Key(Zone.Zone.id))) HideProp(i.root); }
        }
        bool emptiedHidden;

        // ---------- discoveries: hidden finds on no map (ZoneSecret; save format 7) ----------
        /// <summary>What the character has found, and what finding pays (null until the session starts).</summary>
        public DiscoveryLog Discoveries { get; private set; }
        /// <summary>How long a "Discovered" toast shows, its fade in and out included.</summary>
        public const float ToastSeconds = 3.2f;
        /// <summary>The name on the "Discovered" toast showing now (null = none). Finds made together queue up behind it.</summary>
        public string ToastName { get; private set; }
        /// <summary>The small line over it: "DISCOVERED" for a find, a cave's levels as you walk in ("LEVELS 3-5"), or empty.</summary>
        public string ToastKicker { get; private set; }
        /// <summary>Seconds the current toast has been showing.</summary>
        public float ToastAge { get { return ToastName == null ? 0 : Time.time - toastSince; } }
        readonly Queue<(string kicker, string name)> toasts = new Queue<(string kicker, string name)>();
        float toastSince, nextSecretCheck; bool pocketedSynced; string vistaWaiting;
        static readonly Crulanda.World.ZoneSecret[] NoSecrets = new Crulanda.World.ZoneSecret[0];
        void StartDiscoveries()
        {
            Discoveries = new DiscoveryLog(Progress, Items, Quests != null ? Quests.Db : null);
            Discoveries.Say = Message;
            Discoveries.Found = s => { toasts.Enqueue(("DISCOVERED", DiscoveryLog.Name(s))); AdvanceToast(); };
        }
        void AdvanceToast()
        {
            if (ToastName != null && Time.time - toastSince < ToastSeconds) return;
            var next = toasts.Count > 0 ? toasts.Dequeue() : (null, null); ToastKicker = next.kicker; ToastName = next.name; toastSince = Time.time;
        }
        // ---------- places: a cave names itself as you walk in ----------
        Crulanda.World.Hollow placeIn; float placeSeen;
        /// <summary>
        /// A few metres into a cave (Hollow), its name comes up on the banner over the levels of the camps inside it ("LEVELS 3-5"),
        /// once each time you go in: step out and back and it stays quiet; out of it for 20 s or more, and it names itself again.
        /// </summary>
        void TickPlaces()
        {
            if (Zone == null) return;
            var p = Player.transform.position; Crulanda.World.Hollow now = null;
            foreach (var h in Crulanda.World.Hollow.All) if (h.Depth(p) > .3f) { now = h; break; }
            if (now == null) { if (placeIn != null && Time.time - placeSeen > 20) placeIn = null; return; }
            placeSeen = Time.time;
            if (now == placeIn) return;
            placeIn = now; toasts.Enqueue((PlaceBand(now), now.Name)); AdvanceToast();
        }
        /// <summary>"LEVELS 3-5" from the camps standing in the passage, or empty when none does.</summary>
        string PlaceBand(Crulanda.World.Hollow h)
        {
            int lo = int.MaxValue, hi = int.MinValue;
            foreach (var c in Zone.Zone.camps) if (c != null && h.FloorAt(c.center, out _)) { lo = Mathf.Min(lo, c.levelMin); hi = Mathf.Max(hi, c.levelMax); }
            return lo > hi ? "" : lo == hi ? "LEVEL " + lo : "LEVELS " + lo + "-" + hi;
        }
        readonly List<Crulanda.World.ZoneSecretSpot> spots = new List<Crulanda.World.ZoneSecretSpot>();
        Crulanda.World.ZoneBuilder spotsZone; Crulanda.World.ZoneSecret[] spotsDefs; int spotsBuilt = -1;
        /// <summary>
        /// This zone's secrets where they stand: the spots ZoneBuilder built (ZoneBuilder.Secrets), plus a bare spot on the ground at
        /// <c>at</c> for any secret in the zone's data without one, so every secret the book counts can be found.
        /// </summary>
        public List<Crulanda.World.ZoneSecretSpot> SecretSpots
        {
            get
            {
                var zone = Zone;
                if (zone == null) { spots.Clear(); spotsZone = null; return spots; }
                var defs = zone.Zone.secrets ?? NoSecrets;
                if (spotsZone == zone && spotsDefs == defs && spotsBuilt == zone.Secrets.Count) return spots;
                spotsZone = zone; spotsDefs = defs; spotsBuilt = zone.Secrets.Count; spots.Clear();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var spot in zone.Secrets) if (spot != null && spot.def != null && !string.IsNullOrEmpty(spot.def.id) && ids.Add(spot.def.id)) spots.Add(spot);
                foreach (var d in defs) if (d != null && !string.IsNullOrEmpty(d.id) && ids.Add(d.id)) spots.Add(new Crulanda.World.ZoneSecretSpot { def = d, position = zone.StandAt(d.at, float.NegativeInfinity, d.height) });
                return spots;
            }
        }
        /// <summary>
        /// Ground distance from you to a thing E can use, or out of reach when a cave's rock is between you: one of you in a passage
        /// (Hollow) and the other not, as on the hill over the Store Caves, sixteen metres above the strongbox.
        /// </summary>
        float GroundDistance(Vector3 at)
        {
            var p = Player.transform.position;
            if (Crulanda.World.Hollow.All.Count > 0 && InHollow(p) != Crulanda.World.Hollow.InsideAny(at + Vector3.up * .3f, 0)) return float.MaxValue;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(at.x, at.z));
        }
        Vector3 inHollowAt = new Vector3(float.NaN, 0, 0); bool inHollow;
        bool InHollow(Vector3 p) { if (p != inHollowAt) { inHollowAt = p; inHollow = Crulanda.World.Hollow.InsideAny(p, 0); } return inHollow; }   // asked for every prop in a frame
        /// <summary>A hidden find within reach that E would search: not a lookout, not found yet (a locked chest counts; E says so).</summary>
        public Crulanda.World.ZoneSecretSpot NearbySecret
        {
            get
            {
                if (Zone == null || Player == null || Discoveries == null) return null;
                Crulanda.World.ZoneSecretSpot best = null; float bestD = UseRange;
                foreach (var spot in SecretSpots)
                {
                    if (DiscoveryLog.IsVista(spot.def) || Discoveries.IsFound(spot.def)) continue;
                    float d = GroundDistance(spot.position); if (d < bestD) { best = spot; bestD = d; }
                }
                return best;
            }
        }
        /// <summary>What E uses among the props and hidden finds in reach: the nearer of the two (the other comes back null).</summary>
        (Crulanda.World.ZoneInteractable usable, Crulanda.World.ZoneSecretSpot secret) NearestUse()
        {
            var usable = NearbyInteractable; var secret = NearbySecret;
            if (usable != null && secret != null) { if (GroundDistance(secret.position) <= GroundDistance(usable.position)) usable = null; else secret = null; }
            return (usable, secret);
        }
        public static string SearchPrompt(Crulanda.World.ZoneSecret s) { return s == null || string.IsNullOrEmpty(s.prompt) ? "Search" : s.prompt; }
        /// <summary>
        /// Searching a hidden find (E): it is found and pays out, or you are told why not: a chest whose key isn't found yet stays
        /// locked, and a find whose item won't fit in full bags stays where it is until there is room.
        /// </summary>
        public DiscoveryLog.Result Search(Crulanda.World.ZoneSecretSpot spot)
        {
            if (spot == null || spot.def == null || Discoveries == null) return DiscoveryLog.Result.AlreadyFound;
            var r = Find(spot);
            if (r == DiscoveryLog.Result.Locked) Message(DiscoveryLog.ShutLine(spot.def));
            else if (r == DiscoveryLog.Result.BagsFull) Message(DiscoveryLog.BagsFullLine);
            return r;
        }
        /// <summary>Records a find and pays it out, levelling you up the way a quest reward does; what you take vanishes; saves.</summary>
        DiscoveryLog.Result Find(Crulanda.World.ZoneSecretSpot spot)
        {
            int before = Progress.Level;
            var r = Discoveries.Discover(spot.def);
            if (r != DiscoveryLog.Result.Found) return r;
            if (Progress.Level > before) { ApplyLevel(); Player.Health.ApplyHealing(Player.Health.Pool.Max); Message("Level " + Progress.Level + "! Talent points are waiting [B]."); }
            if (DiscoveryLog.Pocketed(spot.def)) Pocket(spot);
            Save(false);
            return r;
        }
        /// <summary>Lookouts are found by standing on them, checked every 0.5 s like quest places.</summary>
        void TickDiscoveries()
        {
            if (Discoveries == null || Zone == null || Time.time < nextSecretCheck) return;
            nextSecretCheck = Time.time + .5f;
            if (!pocketedSynced) { pocketedSynced = true; SyncPocketed(); }
            string waiting = null;
            foreach (var spot in SecretSpots)
            {
                var d = spot.def;
                if (!DiscoveryLog.IsVista(d) || Discoveries.IsFound(d) || GroundDistance(spot.position) > d.radius) continue;
                // A lookout whose find won't fit in the bags waits for room: said once while you stand there.
                if (Find(spot) == DiscoveryLog.Result.BagsFull) { waiting = d.id; if (vistaWaiting != d.id) Message(DiscoveryLog.BagsFullLine); }
            }
            vistaWaiting = waiting;
        }
        /// <summary>Renderers hidden on found things you took (a page, a key, a plant), so a load that un-finds them can show them again.</summary>
        readonly Dictionary<Transform, Renderer[]> pocketed = new Dictionary<Transform, Renderer[]>();
        void Pocket(Crulanda.World.ZoneSecretSpot spot)
        {
            if (spot.root == null || pocketed.ContainsKey(spot.root)) return;
            var shown = Array.FindAll(spot.root.GetComponentsInChildren<Renderer>(), r => r.enabled);
            foreach (var r in shown) r.enabled = false;
            pocketed[spot.root] = shown;
        }
        /// <summary>After a load: found things you took are gone, and ones this save hasn't found are back.</summary>
        void SyncPocketed()
        {
            foreach (var spot in SecretSpots)
            {
                if (spot.root == null || !DiscoveryLog.Pocketed(spot.def)) continue;
                if (Discoveries.IsFound(spot.def)) Pocket(spot);
                else if (pocketed.TryGetValue(spot.root, out var shown)) { foreach (var r in shown) if (r != null) r.enabled = true; pocketed.Remove(spot.root); }
            }
        }
        /// <summary>
        /// The quest book's Discoveries tab: every zone, this one first and the rest by level, with how many secrets each holds and
        /// the ones found (the rest are only counted). This zone's are the ones in play here (SecretSpots); other zones' come from
        /// their data.
        /// </summary>
        public List<DiscoveryLog.Tally> DiscoveryTallies()
        {
            var list = new List<DiscoveryLog.Tally>();
            if (Discoveries == null || Zone == null) return list;
            var here = new List<Crulanda.World.ZoneSecret>(); foreach (var spot in SecretSpots) here.Add(spot.def);
            list.Add(Discoveries.TallyOf(Zone.Zone.id, Zone.Zone.displayName, here, true));
            var others = Zone.AllZones().FindAll(z => z != null && z.id != Zone.Zone.id);
            others.Sort((a, b) => a.levelMin != b.levelMin ? a.levelMin.CompareTo(b.levelMin) : string.CompareOrdinal(a.id, b.id));
            foreach (var z in others) list.Add(Discoveries.TallyOf(z.id, z.displayName, z.secrets, false));
            return list;
        }

        /// <summary>Where the player last stood on dry ground (saves use it if you are in the water).</summary>
        Vector3 lastDry;
        public int TalentRank(string id) { return TalentTree.Rank(Progress, id); }
        public bool CanEditBuild { get { return Player != null && Player.IsAlive && !Paused && !InCombat; } }
        public bool ChangeTalent(string id, int delta)
        {
            if (!CanEditBuild || (delta != 1 && delta != -1)) return false;
            if (!Talents.Propose(Progress, id, delta, out var proposed, out var reason)) { Message(reason); return false; }
            Progress.talents = proposed; Kit.ClearTransient(); Kit.ApplyStats(); Save(false); return true;
        }
        public bool ResetTalents()
        {
            if (!CanEditBuild) return false;
            Progress.talents.Clear(); Kit.ClearTransient(); Kit.ApplyStats(); Save(false); Message("Talent points refunded."); return true;
        }
        public void AddHealingThreat(int healed)
        {
            foreach (var enemy in Enemies) if (enemy.Engaged) enemy.threat.Add(Player.EntityId.Value, healed * .5f);
        }
        public bool AutoAttack { get; private set; }
        public float SwingRemaining { get { return Mathf.Max(0, nextSwing - Time.time); } }
        public string AutoAttackStatus
        {
            get
            {
                if (Kit != null && !Kit.MeleeAutoAttacks) return PlayerCasting ? "Casting " + abilities.Casting.name : "No weapon swings in this form";
                return !AutoAttack ? "Auto attack: use a melee ability" : Target == null || !Target.actor.IsAlive ? "Auto attack: no target" :
                    Distance(Target) >= 3.2f ? "Auto attack: move closer" : "Next swing: " + SwingRemaining.ToString("0.0") + "s";
            }
        }
        public int ActionCount { get { return Kit == null ? 0 : Kit.ActionCount; } }
        public AbilityDefinition ActionAt(int slot) { return Kit == null || slot < 0 || slot >= Kit.ActionCount ? null : Kit.ActionAt(slot); }
        public bool ActionUnlocked(int slot) { return Player != null && ActionAt(slot) != null && Kit.ActionLockLabel(slot) == null; }
        /// <summary>Short HUD label explaining why an action is unavailable, or null when owned.</summary>
        public string ActionLockLabel(int slot) { return ActionAt(slot) == null ? "Unavailable" : Kit.ActionLockLabel(slot); }
        DerivedStatsController playerStats;
        readonly AbilityRuntime abilities = new AbilityRuntime();
        public float CooldownRemaining(int index) { return abilities.Remaining(ActionAt(index), Time.time); }
        public float CooldownRemaining(AbilityDefinition ability) { return abilities.Remaining(ability, Time.time); }
        public bool PlayerCasting { get { return abilities.IsCasting; } }
        public float PlayerCastProgress { get { return abilities.CastProgress(Time.time); } }
        public string PlayerCastName { get { return abilities.IsCasting ? abilities.Casting.name : null; } }

        // ---------- helpers for class kits ----------
        /// <summary>Spends the player's class resource and starts the ability (casts complete later via Update).</summary>
        public AbilityStartResult StartAbility(AbilityDefinition a, Action effect)
        {
            var result = abilities.TryStart(a, Time.time, cost => {
                if (Player.Resource.Pool.Current < cost) return false;
                Player.Resource.Pool.Change(-cost); return true;
            }, effect);
            if (result == AbilityStartResult.InsufficientResource) Message("Not enough " + ClassDef.resource + ".");
            return result;
        }
        public bool EnemyInRange(EncounterEnemy enemy, float range) { return enemy != null && enemy.actor.IsAlive && Distance(enemy) <= range; }
        public bool RequireEnemyInRange(float range)
        {
            if (EnemyInRange(Target, range)) return true;
            Message("Select a living enemy and move within " + range + " metres."); return false;
        }
        public void BeginAutoAttack()
        {
            if (Kit != null && !Kit.MeleeAutoAttacks) return;
            if (!AutoAttack) nextSwing = Time.time + Kit.SwingInterval(content.playerSwingInterval);
            AutoAttack = true;
        }
        public void StopAutoAttack() { AutoAttack = false; }
        public readonly List<string> Messages = new List<string>();
        public readonly List<CombatText> Floating = new List<CombatText>();
        public bool InCombat { get { return (AutoAttack && Target != null && Target.actor.IsAlive) || Enemies.Exists(e => e != null && e.Engaged); } }
        public int WeaponDamage { get { return Player == null ? 0 : Player.Stats.GetRounded(StatType.AttackPower); } }
        float nextSwing, nextRegen, nextSave;
        EncounterSave saves;
        GameObject actorsRoot;
        bool restoring;
        bool saveBlocked;
        public sealed class CombatText { public Vector3 position; public string text; public Color color; public float expires; }

        void Start()
        {
            if (content == null) { Debug.LogError("Encounter content missing."); enabled = false; return; }
            Crulanda.World.WorldWeather.Turned += OnWeatherTurned;
            if ((UseTempSave || Array.IndexOf(Environment.GetCommandLineArgs(), "--crulanda-temp-save") >= 0) && SaveDirectoryOverride == null)
            {
                // Test runs: a fresh throwaway character that never touches the real save folder. One folder for the whole
                // play session: travel reloads the scene, and a new folder per zone would lose the character at every road.
                if (tempSaveRoot == null) { tempSaveRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CrulandaTestSave-" + Guid.NewGuid().ToString("N")); Message("Test save: progress in this session is thrown away."); }
                SaveDirectoryOverride = tempSaveRoot;
            }
            if (EncounterCapture.Requested)
            {
                SaveDirectoryOverride = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CrulandaCapture-" + Guid.NewGuid().ToString("N"));
                gameObject.AddComponent<EncounterCapture>().session = this;
            }
            string root = SaveDirectoryOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "CrulandaEncounter");
            var args = Environment.GetCommandLineArgs(); int classArg = Array.IndexOf(args, "--crulanda-class");
            if (StartClassOverride == null && classArg >= 0 && classArg + 1 < args.Length) StartClassOverride = args[classArg + 1];
            string classId = StartClassOverride ?? CharacterProfile.LastClass(root) ?? content.playerClass.id;
            var playable = content.FindClass(classId) ?? content.FindClass(content.playerClass.id);
            try
            {
                ClassDef = playable.definition;
                Kit = ClassKit.Create(ClassDef.id, this, ClassDef, content.abilities);
                Talents = TalentTree.Parse(ClassDef.id, playable.talentTree != null ? playable.talentTree.text : null, 10, Kit.ImplementedTalents);
            }
            catch (ArgumentException error) { Debug.LogError("Class content invalid: " + error.Message); enabled = false; return; }
            saves = new EncounterSave(root, Talents, EncounterSave.SlotFor(ClassDef.id));
            if (saves.Read(out var p, out var message)) { Progress = p; if (message != null) Message(message); }
            else
            {
                Progress = FreshProgress(ClassDef.id);
                if (message != "File not found.") { Message("Save could not be loaded: " + message); saveBlocked = true; }
            }
            if (StartClassOverride == null) CharacterProfile.Remember(root, ClassDef.id);
            // The scene builds its opening zone first; if this character is saved elsewhere, rebuild that zone instead.
            if (Zone != null && !saveBlocked && !string.IsNullOrEmpty(Progress.zoneId) && Progress.zoneId != Zone.Zone.id && Zone.HasZone(Progress.zoneId))
            {
                Crulanda.World.ZoneBuilder.RequestedZoneId = Progress.zoneId;
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
                return;
            }
            View = Camera.main;
            LoadItems();
            SpawnParty();
            // Villagers and critters live alongside the encounter (they survive load/respawn of the party).
            if (Zone != null && Zone.Zone.life != null) new GameObject("Village life").AddComponent<VillageLife>().Init(this);
            StartQuests(); StartDiscoveries();
            Message(Zone != null ? Zone.Zone.displayName + ". " + Objective(0, "") + "." : "Recruit the healer at camp [E], then follow the path to the sentries.");
            ReconcileQuests();
            nextSave = Time.time + 30;
        }
        public static EncounterProgress FreshProgress(string classId = "class.warrior")
        {
            var p = new EncounterProgress { classId = classId, playerId = EntityId.New().Value, companionId = EntityId.New().Value };
            Inventory.Ensure(p); return p;
        }
        /// <summary>Saves this character, remembers the other class and reloads the scene as that character.</summary>
        public bool SwitchCharacter(string classId)
        {
            if (content.FindClass(classId) == null || classId == ClassDef.id) return false;
            if (InCombat) { Message("Leave combat before switching characters."); return false; }
            if (Player.IsAlive) Save(false);
            CharacterProfile.Remember(SaveDirectoryOverride ?? System.IO.Path.Combine(Application.persistentDataPath, "CrulandaEncounter"), classId);
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
            return true;
        }
        Actor SpawnActor(string label, Crulanda.Data.ActorArchetypeDefinition definition, Vector3 point, Color color, string id, ActorLook look, int level = 1)
        {
            var go = new GameObject(label);
            go.SetActive(false); go.transform.SetParent(actorsRoot.transform); go.transform.position = point;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body"; body.transform.SetParent(go.transform, false);
            Destroy(body.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Standard")); mat.color = color;
            body.GetComponent<Renderer>().sharedMaterial = mat;
            var actor = go.AddComponent<Actor>();
            actor.Initialize(definition, label, label == "You" ? Progress.Level : level, new EntityId(id));
            go.AddComponent<Combatant>();
            ActorVisual.Attach(go, look, label.IndexOf("Ash", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0);   // variant 1: ash hounds
            return actor;
        }
        void SpawnParty()
        {
            restoring = true;
            Kit.ResetState();
            if (Zone != null && Progress.zoneId != Zone.Zone.id)
            {
                // Arriving in a zone for the first time (or from the old test map): start at its entrance.
                Progress.zoneId = Zone.Zone.id; var start = StartPoint; Progress.x = start.x; Progress.y = start.y; Progress.z = start.z;
            }
            if (Zone != null) Progress.y = Zone.StandAt(new Vector2(Progress.x, Progress.z), Progress.y - 1.1f, 1.1f).y;   // on the generated ground, or the cave floor you saved on
            actorsRoot = new GameObject("Encounter Actors");
            var tint = ClassDef.id == "class.druid" ? new Color(.52f, .44f, .27f) : new Color(.2f, .58f, .72f);
            Player = SpawnActor("You", content.player, new Vector3(Progress.x, Progress.y, Progress.z), tint, Progress.playerId,
                ClassDef.id == "class.druid" ? ActorLook.Druid : ActorLook.Warrior);
            var controller = Player.gameObject.AddComponent<CharacterController>(); controller.height = 2; controller.radius = .4f; controller.stepOffset = .35f;
            controller.minMoveDistance = 0;
            var motor = Player.gameObject.AddComponent<AdventurerMotor>(); motor.session = this; motor.view = View;
            if (GetComponent<TargetRing>() == null) gameObject.AddComponent<TargetRing>().session = this;
            Player.gameObject.SetActive(true);
            playerStats = Player.gameObject.AddComponent<DerivedStatsController>();
            Inventory.Ensure(Progress);
            playerStats.Configure(ClassDef.stats, 0);

            Player.ConfigureResource(ClassDef.resource, ClassDef.maxResource);
            ApplyLevel();
            Kit.ApplyStats();
            ApplyEquipment();
            SetHealth(Player, Progress.health);
            Player.Health.Died += h => {
                AutoAttack = false; abilities.Interrupt(); Message("You fell. Press R to recover at camp. Your equipment is kept.");
                var look = Player.GetComponent<ActorVisual>(); if (look != null) look.Pose = ActorPose.None;   // no swimming or sneaking corpse
            };
            lastDry = Player.transform.position;
            var friendPoint = Progress.recruited ? Player.transform.position + Vector3.right * 2 : CompanionPoint;
            // A save made beside (or in) water must not put her in it: snap to the nearest walkable ground.
            if (NavMesh.SamplePosition(friendPoint, out var friendHit, 12, NavMesh.AllAreas)) friendPoint = friendHit.position + Vector3.up * 1.05f;
            var friend = SpawnActor("Mira · provisional healer", content.healer, friendPoint, new Color(.45f, .76f, .57f), Progress.companionId, ActorLook.Healer);
            AddAgent(friend.gameObject, 4.6f);
            Companion = friend.gameObject.AddComponent<HealerCompanion>(); Companion.actor = friend; Companion.session = this;
            friend.gameObject.SetActive(true); SetHealth(friend, Progress.companionHealth); friend.Resource.Pool.SetCurrent(Progress.mana);
            Enemies.Clear();
            foreach (var spawn in EnemySpawns())
            {
                // Concord collectors wear Council grey-blue; legacy sentries keep their rust red.
                var enemyTint = Zone != null ? (spawn.veteran ? new Color(.30f, .33f, .42f) : new Color(.45f, .48f, .55f)) : new Color(.68f, .29f, .2f);
                var a = SpawnActor(spawn.name, content.enemy, spawn.point, enemyTint, spawn.id, spawn.look, spawn.level);
                AddAgent(a.gameObject, 2.8f);
                var enemy = a.gameObject.AddComponent<EncounterEnemy>(); enemy.actor = a; enemy.persistentId = spawn.id; enemy.session = this;
                enemy.Elite = spawn.veteran;
                a.gameObject.SetActive(true); enemy.Initialize(); Enemies.Add(enemy);
                if (Zone != null)
                {
                    // Generated zones: health and hits scale with the enemy's level (story enemies are sturdier than camp mobs).
                    a.Stats.SetBase(StatType.MaxHealth, EncounterEnemy.MobHealth(spawn.level, true, spawn.veteran, false));
                    enemy.HitBase = EncounterEnemy.MobHit(spawn.level, true, spawn.veteran);
                }
                else if (spawn.veteran) a.Stats.SetBase(StatType.MaxHealth, content.veteranHealth);
                if (Progress.FindEnemy(spawn.id).dead) enemy.RestoreDead();
            }
            SpawnCamps();
            restoring = false;
        }
        struct EnemySpawn { public string id, name; public Vector3 point; public bool veteran; public ActorLook look; public int level; }
        IEnumerable<EnemySpawn> EnemySpawns()
        {
            if (Zone != null)
            {
                foreach (var e in Zone.Zone.spawns.enemies)
                    yield return new EnemySpawn { id = e.id, name = e.name, point = Zone.Ground(e.at, 1), veteran = e.veteran, look = LookFor(e.look, e.veteran), level = e.level > 0 ? e.level : Zone.Zone.levelMax };
                yield break;
            }
            Vector3[] points = { new Vector3(-5, 1, 1), new Vector3(5, 1, 7), new Vector3(0, 1, 17) };
            for (int i = 0; i < points.Length; i++)
                yield return new EnemySpawn { id = "sentry." + i, name = i == 2 ? "Veteran sentry" : "Trail sentry " + (i + 1), point = points[i], veteran = i == 2, look = ActorLook.Sentry, level = 1 };
        }
        static ActorLook LookFor(string look, bool veteran)
        {
            switch ((look ?? "").ToLowerInvariant())
            {
                case "pale": return ActorLook.Pale;
                case "outrider": return ActorLook.Outrider;
                case "warden": return ActorLook.Warden;
                case "collector": return ActorLook.Collector;
                case "hollow": return ActorLook.Hollow;
                case "cultist": return ActorLook.Cultist;
                case "wolf": return ActorLook.Wolf;
                case "boar": return ActorLook.Boar;
                case "weaveeater": return ActorLook.WeaveEater;
                case "deserter": return ActorLook.Deserter;
                case "banditking": return ActorLook.BanditKing;
                default: return veteran ? ActorLook.Warden : ActorLook.Collector;
            }
        }
        /// <summary>
        /// Levelling camps from the zone data: packs of mobs at levels inside the camp's band. They respawn somewhere in
        /// the camp after RespawnSeconds and are never saved. Ids: mob.&lt;tag&gt;.&lt;zone&gt;.&lt;camp&gt;.&lt;n&gt;.
        /// </summary>
        void SpawnCamps()
        {
            if (Zone == null || Zone.Zone.camps == null) return;
            var rng = new System.Random(Zone.Zone.seed * 31 + 5);
            string zoneShort = Zone.Zone.id.Replace("zone.", "");
            for (int c = 0; c < Zone.Zone.camps.Length; c++)
            {
                var camp = Zone.Zone.camps[c]; if (camp == null) continue;
                var look = LookFor(camp.look, camp.elite); bool beast = ActorVisual.IsBeast(look);
                string tag = string.IsNullOrEmpty(camp.tag) ? (camp.look ?? "mob") : camp.tag;
                for (int n = 0; n < camp.count; n++)
                {
                    var spot = camp.center + new Vector2((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f) * camp.radius * 2;
                    // Hunters in hiding lie where the tall grass is sure to be, not out at the corners of the camp.
                    if (camp.ambush) spot = camp.center + Vector2.ClampMagnitude(spot - camp.center, camp.radius) * EncounterEnemy.AmbushSpread(camp.radius);
                    var point = Zone.StandAt(spot, float.NegativeInfinity, 1);   // a camp in a cave stands on its floor
                    if (NavMesh.SamplePosition(point, out var hit, 3, NavMesh.AllAreas)) point = hit.position + Vector3.up; else continue;
                    int level = camp.levelMin + rng.Next(Mathf.Max(1, camp.levelMax - camp.levelMin + 1));
                    // Only the first mob of an elite camp is the elite (its pack stays normal).
                    bool elite = camp.elite && n == 0;
                    string id = "mob." + tag + "." + zoneShort + "." + c + "." + n;
                    var a = SpawnActor(elite ? camp.mob + " (elite)" : camp.mob, content.enemy, point, Color.grey, id, look, level);
                    AddAgent(a.gameObject, look == ActorLook.Wolf ? 4.2f : look == ActorLook.Boar ? 3.8f : look == ActorLook.WeaveEater ? 3.4f : 2.8f, beast ? .6f : .45f);
                    var enemy = a.gameObject.AddComponent<EncounterEnemy>(); enemy.actor = a; enemy.persistentId = id; enemy.session = this;
                    enemy.Camp = true; enemy.Elite = elite; enemy.RespawnSeconds = Mathf.Max(20, camp.respawn); enemy.CampCenter = camp.center; enemy.CampRadius = camp.radius;
                    enemy.Ambusher = camp.ambush;
                    a.gameObject.SetActive(true); enemy.Initialize(); Enemies.Add(enemy);
                    if (camp.ambush) enemy.Hide();
                    a.Stats.SetBase(StatType.MaxHealth, EncounterEnemy.MobHealth(level, false, elite, beast));
                    a.Health.ApplyHealing(a.Health.Pool.Max);
                    enemy.HitBase = EncounterEnemy.MobHit(level, false, elite);
                    if (elite) a.transform.localScale = Vector3.one * 1.18f;
                }
            }
        }
        /// <summary>Story enemies of this zone (saved, stay dead) as opposed to camp mobs.</summary>
        public List<EncounterEnemy> StoryEnemies { get { return Enemies.FindAll(e => !e.Camp); } }
        /// <summary>Collider and agent. Beasts and Weave-Eaters are bulkier than a person: wider apart, and they stop further off.</summary>
        static void AddAgent(GameObject go, float speed, float radius = .45f)
        {
            var collider = go.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = radius;
            var agent = go.AddComponent<NavMeshAgent>(); agent.speed = speed; agent.angularSpeed = 540;
            agent.acceleration = 16; agent.stoppingDistance = 1.8f + (radius - .45f) * 2; agent.radius = radius; agent.height = 2; agent.baseOffset = 1;
        }
        static void SetHealth(Actor actor, int value)
        {
            if (value <= 0) actor.Health.ApplyDamage(actor.Health.Pool.Max);
            else { if (!actor.IsAlive) actor.Health.Revive(value); actor.Health.Pool.SetCurrent(value); }
        }
        public Actor PartyActor(string id)
        {
            if (id == null) return null;
            if (Player != null && Player.EntityId.Value == id) return Player;
            if (Companion != null && Progress.recruited && Companion.actor.EntityId.Value == id) return Companion.actor;
            return null;
        }
        public bool IsLivingPartyMember(string id) { var a = PartyActor(id); return a != null && a.IsAlive; }
        void Update()
        {
            if (Player == null) return;
            AdvanceToast();
            if (EncounterInput.Press(KeyCode.Escape))
            {
                // Esc closes open windows (conversation, quest book, map) before it pauses.
                if (Conversation != null && !Paused) Conversation = null;
                else if (VendorNpc != null && !Paused) CloseVendor();
                else if ((CharacterOpen || InventoryOpen) && !Paused) { CharacterOpen = false; InventoryOpen = false; }
                else if (QuestBookOpen && !Paused) { QuestBookOpen = false; ReadingDocument = null; }
                else if (MapOpen && !Paused) MapOpen = false;
                else { Paused = !Paused; Time.timeScale = Paused ? 0 : 1; }
            }
            if (!Paused && EncounterInput.Press(KeyCode.M)) MapOpen = !MapOpen;
            if (!Paused && Quests != null && EncounterInput.Press(KeyCode.L)) { QuestBookOpen = !QuestBookOpen; if (!QuestBookOpen) ReadingDocument = null; }
            if (Paused) return;
            if (EncounterInput.Press(KeyCode.B)) BuildOpen = !BuildOpen;
            // Moving interrupts the player's own cast-time abilities (instant abilities are unaffected).
            abilities.Tick(Time.time, Player.IsAlive && !Player.GetComponent<AdventurerMotor>().Moving);
            if (EncounterInput.Press(KeyCode.I)) InventoryOpen = !InventoryOpen;
            if (EncounterInput.Press(KeyCode.C)) CharacterOpen = !CharacterOpen;
            if (EncounterInput.Press(KeyCode.F9)) { Load(); return; }
            if (EncounterInput.Press(KeyCode.F5)) Save();
            if (EncounterInput.Press(KeyCode.F10)) PrototypeLevelCap();
            if (Debug.isDebugBuild && EncounterInput.Press(KeyCode.F11)) { Crulanda.World.WorldClock.Advance(1); Message("Time skips ahead: " + Crulanda.World.WorldClock.Text + " (dev)."); }
            if (Debug.isDebugBuild && EncounterInput.Press(KeyCode.F8) && Crulanda.World.WorldWeather.Active != null) Message("Weather: " + Crulanda.World.WorldWeather.Active.CycleForced() + " (dev).");
            if (!Player.IsAlive) { if (EncounterInput.Press(KeyCode.R)) Recover(); return; }
            TickQuests(); TickItems(); TickDiscoveries(); TickPlaces();
            if (Zone != null && Player.GetComponent<CharacterController>().isGrounded && !Zone.WaterAt(new Vector2(Player.transform.position.x, Player.transform.position.z), out _, out _)) lastDry = Player.transform.position;
            var motor = Player.GetComponent<AdventurerMotor>(); var look = Player.GetComponent<ActorVisual>();
            if (look != null) look.Pose = motor.Swimming ? ActorPose.Swim : motor.Sneaking ? ActorPose.Sneak : ActorPose.None;
            if (!BuildOpen && EncounterInput.Press(KeyCode.Tab)) CycleTarget();
            if (EncounterInput.Click && !EncounterHud.BlocksPointer(EncounterInput.Pointer))
            {
                // Click through faded trees (you can see through them) and through yourself; the first thing left is what you hit.
                EncounterEnemy enemy = null;
                var hits = Physics.RaycastAll(View.ScreenPointToRay(EncounterInput.Pointer), 100, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.IsChildOf(Player.transform)) continue;
                    var tree = hit.collider.GetComponentInParent<Crulanda.World.TreeFade>(); if (tree != null && tree.Faded) continue;
                    enemy = hit.collider.GetComponentInParent<EncounterEnemy>(); break;
                }
                if (enemy != null && enemy.Hidden) enemy = null;
                if (enemy != null) Select(enemy);
                else PickFriendly(EncounterInput.Pointer);
            }
            if (!BuildOpen && EncounterInput.Press(KeyCode.E)) Interact();

            for (int slot = 0; slot < ActionCount; slot++)
                if (!BuildOpen && slot < 10 && EncounterInput.Press(EncounterInput.SlotKey(slot))) UseAbility(slot);
            Kit.Tick(InCombat);
            if (AutoAttack && !Kit.MeleeAutoAttacks) AutoAttack = false;
            if (AutoAttack && Target != null && Target.actor.IsAlive && Time.time >= nextSwing && Distance(Target) < 3.2f)
            { nextSwing = Time.time + Kit.SwingInterval(content.playerSwingInterval); Target.Receive(WeaponDamage, Player); Kit.OnAutoHit(); }
            if (Time.time >= nextRegen)
            {
                nextRegen = Time.time + 1; Player.Resource.Pool.Change(InCombat ? ClassDef.combatRegen : ClassDef.restingRegen);
                if (!InCombat) { Player.Health.ApplyHealing(7); if (Companion.actor.IsAlive) Companion.actor.Health.ApplyHealing(7); }
            }
            if (!InCombat && Time.time > nextSave) { Save(false); nextSave = Time.time + 30; }
            Floating.RemoveAll(f => f.expires < Time.time);
        }
        public float Distance(EncounterEnemy enemy) { return Vector3.Distance(Player.transform.position, enemy.transform.position); }
        public void Select(EncounterEnemy enemy) { Target = enemy; AutoAttack = false; if (enemy != null) { FocusVillager = null; FocusMira = false; } }
        // ---------- friendly target (left-click a villager or Mira) ----------
        /// <summary>The villager you clicked (null = none). E talks to the selected friend first when they are in reach.</summary>
        public Villager FocusVillager { get; private set; }
        public bool FocusMira { get; private set; }
        public bool HasFriendlyFocus { get { return FocusMira || (FocusVillager != null && FocusVillager.Visible); } }
        public string FocusName { get { return FocusMira ? "Mira" : FocusVillager != null ? FocusVillager.Name : null; } }
        public Vector3 FocusPosition { get { return FocusMira && Companion != null ? Companion.transform.position : FocusVillager != null ? FocusVillager.transform.position : Vector3.zero; } }
        public void SelectFriendly(Villager v, bool mira)
        {
            FocusVillager = mira ? null : v; FocusMira = mira;
            if (mira || v != null) { Target = null; AutoAttack = false; }
        }
        /// <summary>
        /// Villagers and Mira have no colliders, so clicks are matched on screen. A click hits someone whose body
        /// (feet to head) contains the pointer; the nearest such person wins. A click on empty ground clears the friend.
        /// </summary>
        void PickFriendly(Vector2 pointer)
        {
            float best = float.MaxValue; Villager pick = null; bool mira = false;
            bool Hit(Vector3 origin, float headUp, out float depth)
            {
                var feet = View.WorldToScreenPoint(origin - Vector3.up * .95f); var head = View.WorldToScreenPoint(origin + Vector3.up * headUp);
                depth = feet.z; if (feet.z <= 0 || head.z <= 0 || feet.z > 60) return false;
                float h = Mathf.Abs(head.y - feet.y), halfW = Mathf.Max(10, h * .22f);
                return pointer.y >= Mathf.Min(feet.y, head.y) && pointer.y <= Mathf.Max(feet.y, head.y) && Mathf.Abs(pointer.x - (feet.x + head.x) / 2) <= halfW;
            }
            var life = VillageLife.Active;
            if (life != null)
                foreach (var v in life.Villagers)
                    if (v.Visible && Hit(v.transform.position, v.Role == "child" ? .45f : .95f, out float d) && d < best) { best = d; pick = v; mira = false; }
            if (Companion != null && Hit(Companion.transform.position, .95f, out float md) && md < best) { best = md; pick = null; mira = true; }
            SelectFriendly(pick, mira);
        }
        /// <summary>Who E would talk to: the selected friend when in reach, otherwise the nearest of Mira and the villagers.</summary>
        (Villager villager, bool mira) TalkTarget()
        {
            var p = Player.transform.position;
            if (FocusMira && CompanionInReach) return (null, true);
            if (FocusVillager != null && FocusVillager.Visible && Vector3.Distance(p, FocusVillager.transform.position) < TalkRange) return (FocusVillager, false);
            // Nothing selected: whoever is nearest, favouring the one you are facing when two are close.
            var fwd = Player.transform.forward;
            float Reach(Vector3 at) { var d = at - p; d.y = 0; return d.magnitude - .9f * Mathf.Max(0, Vector3.Dot(fwd, d.normalized)); }
            Villager best = null; float bestReach = float.MaxValue;
            if (VillageLife.Active != null)
                foreach (var cand in VillageLife.Active.Villagers)
                {
                    if (!cand.Visible || Vector3.Distance(p, cand.transform.position) >= TalkRange) continue;
                    float r = Reach(cand.transform.position); if (r < bestReach) { bestReach = r; best = cand; }
                }
            if (CompanionInReach && (best == null || Reach(Companion.transform.position) <= bestReach)) return (null, true);
            return (best, false);
        }
        public void CycleTarget()
        {
            var nearby = Enemies.FindAll(e => e.actor.IsAlive && !e.Hidden && Distance(e) < 25);
            nearby.Sort((a,b) => Distance(a).CompareTo(Distance(b)));
            if (nearby.Count == 0) { Target = null; return; }
            Select(nearby[(nearby.IndexOf(Target) + 1) % nearby.Count]);
        }
        public bool UseAbility(int index)
        {
            if (Paused || Player == null || !Player.IsAlive || !ActionUnlocked(index)) return false;
            if (CooldownRemaining(index) > 0) return false;
            return Kit.Use(index);
        }
        /// <summary>The zone exit the player is standing at, if any.</summary>
        public Crulanda.World.ZoneExit NearbyExit
        {
            get
            {
                if (Zone == null || Player == null) return null;
                var p = new Vector2(Player.transform.position.x, Player.transform.position.z);
                foreach (var e in Zone.Zone.exits) if (Vector2.Distance(p, e.at) <= e.radius && Zone.HasZone(e.to)) return e;
                return null;
            }
        }
        public bool TravelTo(Crulanda.World.ZoneExit exit)
        {
            if (exit == null || Zone == null || !Zone.HasZone(exit.to)) return false;
            // Only a fight you are actually in stops you: something after you nearby. A mob stuck chasing you from the far side
            // of the zone must not bar every road out.
            var chaser = Enemies.Find(e => e != null && e.Engaged && Distance(e) < 40);
            if (!Player.IsAlive || chaser != null || (AutoAttack && Target != null && Target.actor.IsAlive && Distance(Target) < 40))
            { Message(chaser != null ? "You can't travel while fighting: " + chaser.actor.DisplayName + " is still after you." : "You can't travel while fighting."); return false; }
            if (saves == null || saveBlocked) { Message("Travel disabled to protect an unreadable save."); return false; }
            Save(false);
            Progress.zoneId = exit.to; Progress.x = exit.arrive.x; Progress.z = exit.arrive.y; Progress.y = 1.1f;
            try { saves.Write(Progress); } catch (Exception e) { Message("Travel failed: " + e.Message); return false; }
            Crulanda.World.ZoneBuilder.RequestedZoneId = exit.to; Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
            return true;
        }
        public const float TalkRange = 3.5f, DoorRange = 2.6f;
        public bool CompanionInReach { get { return Companion != null && Vector3.Distance(Player.transform.position, Companion.transform.position) < TalkRange; } }
        /// <summary>A body that still has something to take (camp mobs until looted; story enemies by their saved record).</summary>
        public bool CanLoot(EncounterEnemy e) { return e != null && !e.actor.IsAlive && (e.Camp ? !e.Looted : !Progress.FindEnemy(e.persistentId).looted); }
        EncounterEnemy LootableCorpse { get { return Enemies.Find(e => CanLoot(e) && Distance(e) < 3.6f); } }
        public Crulanda.World.ZoneDoor NearbyDoor
        {
            get
            {
                if (Zone == null || Player == null) return null;
                Crulanda.World.ZoneDoor best = null; float bestDistance = DoorRange;
                foreach (var door in Zone.Doors)
                {
                    float dist = Vector3.Distance(Player.transform.position, door.position);
                    if (dist < bestDistance) { best = door; bestDistance = dist; }
                }
                return best;
            }
        }
        /// <summary>What E would do right now (for the HUD prompt), or null when nothing is in reach.</summary>
        public string InteractPrompt
        {
            get
            {
                if (Player == null || !Player.IsAlive || Paused) return null;
                var exit = NearbyExit; if (exit != null) return exit.name;
                if (LootableCorpse != null) return "Search the body";
                var (villager, mira) = TalkTarget();
                if (mira) return !Progress.recruited ? "Recruit Mira" : !Companion.actor.IsAlive ? "Revive Mira" : "Talk to Mira";
                if (villager != null) return "Talk to " + villager.Name;
                var (usable, secret) = NearestUse();
                if (secret != null) return SearchPrompt(secret.def);
                if (usable != null) return usable.prompt;
                var door = NearbyDoor;
                if (door != null) return door.openable ? (door.Open ? "Close the door" : "Open the door") + " · " + door.name : "Knock · " + door.name;
                return null;
            }
        }
        static readonly string[] BarredDoorLines = {
            "The door is barred. Someone inside holds their breath until you leave.",
            "No answer. Since the collectors came, Oakhaven opens its doors to no one.",
            "A voice through the planks: \"We've nothing left to give. Go away.\"",
            "Locked. Fresh scratches around the latch.",
        };
        public void Interact()
        {
            if (!Player.IsAlive || Paused) return;
            var exit = NearbyExit; if (exit != null) { TravelTo(exit); return; }
            var corpse = LootableCorpse;
            if (corpse != null)
            {
                if (corpse.Camp) { LootCamp(corpse); return; }
                if (Progress.Loot(corpse.persistentId))
                {
                    if (!Inventory.Has(Progress, content.itemId) && Items != null && Inventory.Add(Progress, Items, content.itemId, 1) == 0)
                    { Message("Looted 8 gold. " + content.itemName + " is in your bags [I]."); InventoryOpen = true; }
                    else Message("Looted 8 gold.");
                }
                return;
            }
            var (talkTo, toMira) = TalkTarget();
            if (toMira)
            {
                if (!Progress.recruited) { Progress.recruited = true; Progress.relationship++; Message("Mira: I'll keep you standing. Lead the way."); ReconcileQuests(); }
                else if (!Companion.actor.IsAlive && !InCombat) { Companion.actor.Health.Revive(100); Message("Mira: Thank you. Let's be more careful."); }
                else if (!QuestTalk("Mira", Companion.transform.position)) Message("Mira: Stay close; my healing has a limited reach.");
                return;
            }
            if (talkTo != null)
            {
                SelectFriendly(talkTo, false);
                if (QuestTalk(talkTo.Name, talkTo.transform.position)) talkTo.FacePlayer();
                else if (IsVendor(talkTo)) { OpenVendor(talkTo); talkTo.FacePlayer(); }
                else VillageLife.Active.Talk(talkTo);
                return;
            }
            var (usable, secret) = NearestUse();
            if (secret != null) { Search(secret); return; }
            if (usable != null) { UseInteractable(usable); return; }
            var door = NearbyDoor;
            if (door != null)
            {
                if (door.openable) door.SetOpen(!door.Open);
                else Message(door.name + ": " + BarredDoorLines[Mathf.Abs(door.name.GetHashCode() + Time.frameCount / 600) % BarredDoorLines.Length]);
                return;
            }
            Message("Nothing to interact with here.");
        }
        public void EnemyDied(EncounterEnemy enemy)
        {
            if (restoring) return;
            int before = Progress.Level;
            int xp = EncounterProgress.KillXp(enemy.actor.Level, Progress.Level, enemy.Elite);
            if (enemy.Camp) Progress.experience += xp;
            else { if (!Progress.AwardKill(enemy.persistentId, xp)) return; if (Progress.recruited) Progress.relationship++; }
            Message(enemy.actor.DisplayName + " defeated · " + (xp > 0 ? "+" + xp + " XP" : "no experience (too weak)") + " · press E at the body to loot.");
            if (Progress.Level > before) { ApplyLevel(); Player.Health.ApplyHealing(40); Message("Level " + Progress.Level + "! Health and weapon damage increased."); }
            if (Target == enemy) AutoAttack = false;
            if (Quests != null)
            {
                Quests.Notify("kill", enemy.persistentId); Quests.LootFrom(enemy.persistentId); ReconcileQuests();
                if (Progress.Level > before) ApplyLevel();
            }
            if (!enemy.Camp && StoryEnemies.TrueForAll(e => !e.actor.IsAlive)) Message(ZoneTitle + " is clear for now. Equip your reward [I], then save [F5].");
        }
        void ApplyLevel() { Player.SetLevel(Progress.Level); }
        /// <summary>Development builds only: jump to the prototype level cap so the whole talent tree can be reviewed.</summary>
        public bool PrototypeLevelCap()
        {
            if (!Debug.isDebugBuild || Player == null || !Player.IsAlive || InCombat || Progress.Level >= 10) return false;
            Progress.experience = Math.Max(Progress.experience, EncounterProgress.XpForLevel(10)); ApplyLevel(); Player.Health.ApplyHealing(Player.Health.Pool.Max);
            Save(false); Message("Prototype shortcut: level 10 (development builds only). Open talents [B].");
            return true;
        }
        /// <summary>Equips the recovered blade from the bags (the first-quest shortcut used by tests and the old flow).</summary>
        public void Equip()
        {
            int i = Progress.bag.FindIndex(s => s.item == content.itemId);
            if (i >= 0) EquipFromBag(i);
        }
        public void Recover()
        {
            if (Player.IsAlive) return;
            Player.Health.Revive(Player.Health.Pool.Max);
            Player.GetComponent<AdventurerMotor>().Teleport(RecoveryPoint);
            Companion.actor.Health.Revive(Companion.actor.Health.Pool.Max);
            Companion.actor.Health.ApplyHealing(1000); Companion.actor.Resource.Pool.Fill();
            Companion.GetComponent<NavMeshAgent>().Warp(RecoveryPoint + Vector3.right * 2.5f - Vector3.up * .1f);
            foreach (var e in Enemies) e.ResetFight();
            Message("Recovered at camp. Defeated enemies and collected rewards remain recorded.");
        }
        public void Save(bool announce = true)
        {
            if (saves == null || saveBlocked) { if (announce) Message("Saving disabled to protect an unreadable save."); return; }
            if (InCombat || !Player.IsAlive) { if (announce) Message("Save when alive and out of combat."); return; }
            Progress.health = Player.Health.Pool.Current; Progress.companionHealth = Companion.actor.Health.Pool.Current;
            Progress.mana = Companion.actor.Resource.Pool.Current;
            // Saves keep the last dry footing: loading never starts you on a lake bed or mid-creek.
            var point = Player.transform.position;
            if (Zone != null && Zone.WaterAt(new Vector2(point.x, point.z), out _, out float wetDepth) && wetDepth > .3f) point = lastDry;
            Progress.x = point.x; Progress.y = point.y; Progress.z = point.z;
            try { saves.Write(Progress); if (announce) Message("Expedition saved."); }
            catch (Exception e) { Message("Save failed: " + e.Message); }
        }
        public void Load()
        {
            if (!saves.Read(out var p, out var error)) { Message("Load failed: " + error); return; }
            actorsRoot.SetActive(false); Destroy(actorsRoot);
            Progress = p; Target = null; AutoAttack = false; abilities.Reset();
            Floating.Clear(); SpawnParty(); Message(error ?? "Saved expedition restored.");
            if (Quests != null) { Quests.Bind(Progress); Conversation = null; emptiedHidden = false; ReconcileQuests(); }
            if (Discoveries != null) { Discoveries.Bind(Progress); pocketedSynced = false; vistaWaiting = null; }
        }
        public void RepeatTrail()
        {
            if (!Player.IsAlive || !StoryEnemies.TrueForAll(e => !e.actor.IsAlive)) return;
            actorsRoot.SetActive(false); Destroy(actorsRoot);
            Progress.enemies.Clear(); var back = RecoveryPoint; Progress.x = back.x; Progress.y = back.y; Progress.z = back.z;
            Progress.health = Player.Health.Pool.Max; Progress.companionHealth = Companion.actor.Health.Pool.Max; Progress.mana = Companion.actor.Resource.Pool.Max;
            Target = null; AutoAttack = false; abilities.Reset();
            SpawnParty(); Message("A new patrol has arrived. Your equipment, level and companion history are kept.");
        }
        public void Message(string text) { Messages.Add(text); if (Messages.Count > 6) Messages.RemoveAt(0); }
        public void FloatText(Vector3 point, string text, Color color) { Floating.Add(new CombatText { position = point + Vector3.up * 1.5f, text = text, color = color, expires = Time.time + 1.3f }); }
        public void Resume() { Paused = false; Time.timeScale = 1; }
        void OnDestroy() { Time.timeScale = 1; Crulanda.World.WorldWeather.Turned -= OnWeatherTurned; }
        /// <summary>The zone's weather turns: a line in the chat ("It starts to rain.").</summary>
        void OnWeatherTurned(Crulanda.World.WeatherKind from, Crulanda.World.WeatherKind to)
        {
            var line = Crulanda.World.WeatherSchedule.Herald(from, to);
            if (line != null) Message(line);
        }
        void OnApplicationQuit() { if (Player != null) Save(false); }
    }
}







