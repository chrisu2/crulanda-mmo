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
    public sealed partial class EncounterSession : MonoBehaviour, Crulanda.World.IZoneNodeKinds
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
        /// <summary>The zone's notice boards and their postings (Bounties.cs); null with no quest content.</summary>
        public Bounties Boards;
        float lastHour = -1; EncounterEnemy courier; bool courierDone;
        /// <summary>Reading a notice board (a "board" prop): the conversation window with today's postings on it.</summary>
        void OpenBoard(Crulanda.World.ZoneInteractable board)
        {
            if (Boards == null || Zone == null) { Message("Rain-marks and old nails. Nothing you can read."); return; }
            Quests.TalkTo("board"); ReconcileQuests();   // skins and herbs brought for a posting are pinned to the board
            var list = Boards.Entries(ZoneId, Progress.Level);
            Conversation = new QuestConversation { npc = Bounties.BoardName, entries = list, selected = null, where = board.position };
        }
        /// <summary>The rare posting's Bureau courier (CANON: the Council's Investigation Bureau), abroad on the zone's first road,
        /// two levels over the zone, while the posting is taken on and not yet paid.</summary>
        void SpawnCourier()
        {
            if (courier != null || Zone == null || Zone.Zone.roads == null || Zone.Zone.roads.Length == 0) return;
            var road = Zone.Zone.roads[0]; var spot = road.points[road.points.Length * 2 / 3];
            var point = Zone.Ground(spot);
            if (NavMesh.SamplePosition(point, out var hit, 4, NavMesh.AllAreas)) point = hit.position;
            string id = Bounties.CourierId(ZoneId);
            var a = SpawnActor("Bureau courier", content.enemy, point + Vector3.up, Color.grey, id, ActorLook.Collector, Zone.Zone.levelMax + 2);
            AddAgent(a.gameObject, 3.2f);
            var enemy = a.gameObject.AddComponent<EncounterEnemy>(); enemy.actor = a; enemy.persistentId = id; enemy.session = this; enemy.Camp = false;
            a.gameObject.SetActive(true); enemy.Initialize(); Enemies.Add(enemy); courier = enemy;
        }
        void DespawnCourier()
        {
            if (courier == null) return;
            Enemies.Remove(courier); if (Target == courier) Select(null);
            courier.gameObject.SetActive(false); Destroy(courier.gameObject); courier = null;
        }
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
                Quests = new QuestLog(questCache, Progress) { Items = Items }; Boards = new Bounties(Quests);
                if (Items != null) { var unknown = questCache.CheckItems(Items); if (unknown.Count > 0) Debug.LogError("Quest content names items that are not right:\n" + string.Join("\n", unknown)); }
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
            if (q.kind == "bounty" && q.rare) SpawnCourier();
            ReconcileQuests(); Save(false); ReopenConversation();
        }
        public void CompleteQuest(QuestDef q)
        {
            if (Quests == null) return;
            int before = Progress.Level;
            // A bag quest pays its maker what the bag would have cost: what's left of the hides is her profit (ADDENDUM C, step 7). It is
            // counted before the hand-in (afterwards the bag is owned), and a bag you already had is paid to you as coin, not made, so earns her nothing.
            var bagItems = q.rewards?.bagItems; int worth = 0;
            if (bagItems != null && Items != null) foreach (var id in bagItems) { var d = Items.Get(id); if (d != null && !(d.kind == "bag" && Inventory.Owns(Progress, id))) worth += Inventory.Price(d); }
            if (!Quests.TurnIn(q, out _)) return;
            if (q.kind == "bounty" && Boards != null) { Boards.Finished(ZoneId, q.id); if (q.rare) DespawnCourier(); }
            if (q.kind == "bounty") { Progress.bounties++; if (q.rare) Progress.rares++; }   // Achievements count them
            if (worth > 0) VillageLife.Active?.Paid(q.turnIn, worth);
            if (Progress.Level > before) { ApplyLevel(); Player.Health.ApplyHealing(Player.Health.Pool.Max); Message("Level " + Progress.Level + "! Talent points are waiting [B]."); }
            ReconcileQuests(); Save(false); ReopenConversation();
        }
        void ReopenConversation()
        {
            if (Conversation == null) return;
            var list = Conversation.npc == Bounties.BoardName && Boards != null ? Boards.Entries(ZoneId, Progress.Level) : Quests.For(Conversation.npc, ZoneId, Progress.Level);
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
            ApplyGearEffects();
        }
        public string ItemName(string id) { var d = Items?.Get(id); return d != null ? d.name : id; }
        public bool EquipFromBag(int bagIndex)
        {
            if (Items == null && Progress.bag[bagIndex].item != content.itemId) return false;
            var d = Items?.Get(Progress.bag[bagIndex].item);
            if (d != null && d.kind == "consumable") return UseItem(bagIndex);
            if (d != null && d.kind == "tool") return UseTool(bagIndex);
            if (d != null && d.kind == "bag") return WearBag(bagIndex);
            if (d != null && d.kind == "material") { Message(Inventory.IsHide(d) ? HideLine : MaterialLine); return false; }
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
        public void MoveBag(int a, int b) { if (Items != null && !Inventory.Move(Progress, Items, a, b, out var why)) Message(why); }
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
        /// <summary>Open a vendor's wares; <paramref name="at"/> is where the trade is done (their door, when you knocked), else where they stand.</summary>
        public void OpenVendor(Villager v, Vector3? at = null)
        {
            if (Items == null || v == null) return;
            int band = Zone != null ? Zone.Zone.levelMax : 2;
            var stock = Items.StockFor(v.Name, v.Role, band); if (stock.Count == 0) return;
            // A trade bag you wear or carry is off the list: one of each is all anyone makes you. Another merchant's open window keeps its list.
            stock.RemoveAll(id => Items.Get(id)?.kind == "bag" && Inventory.Owns(Progress, id));
            if (stock.Count == 0) { Message(v.Name + ": " + AllBagsLine); v.Say(AllBagsLine, 6); return; }
            VendorStock = stock;
            // What the village brought the stall today (the hen-wife's eggs): sold on while they last.
            if (v.Role == "merchant" && VillageLife.Active != null && VillageLife.Active.Count("stall.eggs") > 0 && Items.Get(FreshEggs) != null) VendorStock.Insert(0, FreshEggs);
            VendorNpc = v.Name; vendorAt = at ?? v.transform.position; InventoryOpen = true; Conversation = null; TradesOpen = false; v.Hold(30);
        }
        public const string FreshEggs = "food.fresh_eggs";
        public void CloseVendor() { VendorNpc = null; VendorStock = new List<string>(); }
        public void SellBag(int i)
        {
            if (VendorNpc == null || i < 0 || i >= Progress.bag.Count || Progress.bag[i].Empty) return;
            string n = ItemName(Progress.bag[i].item); int count = Progress.bag[i].count; var d = Items?.Get(Progress.bag[i].item);
            int gold = Inventory.Sell(Progress, Items, i);
            // A material sold in a village is that day's delivery to its trade (ore to the forge, herbs to the stall), and the trades
            // notice; "sold." + the trade marks it as the player's (the herbalist brings the stall herbs herself every day).
            if (d != null && !string.IsNullOrEmpty(d.trade) && VillageLife.Active != null) { VillageLife.Active.Deliver(d.trade, count); VillageLife.Active.Deliver("sold." + d.trade, count); }
            Message("Sold " + n + (count > 1 ? " x" + count : "") + " for " + gold + " crowns."); Save(false);
        }
        public void SellJunk()
        {
            if (VendorNpc == null) return;
            int total = Inventory.SellJunk(Progress, Items);
            Message(total > 0 ? "Sold your junk for " + total + " crowns." : "Nothing worth selling as junk."); Save(false);
        }
        public void Buy(string item)
        {
            if (VendorNpc == null) return;
            if (item == FreshEggs && (VillageLife.Active == null || VillageLife.Active.Count("stall.eggs") <= 0)) { VendorStock.Remove(item); Message("The eggs have all gone."); return; }
            var d = Items?.Get(item);
            if (d != null && d.kind == "bag" && Inventory.Owns(Progress, item)) { VendorStock.Remove(item); Message(Inventory.AlreadyWornLine); return; }
            if (Inventory.Buy(Progress, Items, item, out var why))
            {
                Message("Bought " + ItemName(item) + "." + (d != null && d.kind == "bag" ? " Use it from your bags to wear it." : "")); Save(false);
                if (item == FreshEggs) { VillageLife.Active.Take("stall.eggs"); if (VillageLife.Active.Count("stall.eggs") == 0) VendorStock.Remove(item); }
                if (d != null && d.kind == "bag") VendorStock.Remove(item);
                // The coin goes to the seller's household (ADDENDUM C: the purses, step 7, spend it).
                VillageLife.Active?.Paid(VendorNpc, Inventory.Price(d));
            }
            else Message(why);
        }

        // ---------- trades: gathering skills, Cooking and crafts (ProfessionLog; save format 8) ----------
        /// <summary>What the character knows of the trades (null when profession content is missing or invalid).</summary>
        public ProfessionLog Professions { get; private set; }
        /// <summary>The Trades window (K).</summary>
        public bool TradesOpen { get; private set; }
        /// <summary>What using a crafting material from the bags says.</summary>
        public const string MaterialLine = "A crafting material. Press K.";
        /// <summary>What a hide or pelt says when used from the bags, and on its tooltip (GAME-ONLY).</summary>
        public const string HideLine = "Leather. Maud Tanner in Oakhaven works it.";
        /// <summary>What the leatherworker says when every bag she makes is already yours (GAME-ONLY).</summary>
        public const string AllBagsLine = "That's one of every bag I cut, and all of them on you. Come back when one wears through.";
        static ProfessionDatabase professionCache; static EncounterContent professionCacheFor; static ItemDatabase professionCacheItems; static bool professionCacheBad;
        /// <summary>The trades' content read against the items (cached), or null when it is missing or invalid (logged once).</summary>
        ProfessionDatabase ProfessionContent()
        {
            if (content == null || content.professionFiles == null || content.professionFiles.Length == 0) return null;
            if (Items == null) LoadItems();
            if (Items == null) return null;
            if (professionCacheFor != content || professionCacheItems != Items)
            {
                professionCache = null; professionCacheBad = false; professionCacheFor = content; professionCacheItems = Items;
                try { var texts = new List<string>(); foreach (var f in content.professionFiles) if (f != null) texts.Add(f.text); professionCache = ProfessionDatabase.Parse(texts, Items); }
                catch (ArgumentException e) { Debug.LogError("Profession content invalid:\n" + e.Message); professionCacheBad = true; }
            }
            return professionCacheBad ? null : professionCache;
        }
        /// <summary>Reads the profession files against the items. A bad file is logged and leaves the trades off; the items stay.</summary>
        void LoadProfessions()
        {
            var db = ProfessionContent(); if (db == null) { Professions = null; return; }
            Professions = new ProfessionLog(db, Items, Progress);
            Professions.Say = Message;
            Professions.SkillUp = (id, skill) => {
                var trade = db.Profession(id); Message(trade.name + " " + skill + ".");
                // A new tier opens: at 20, 40, 60 and 80 the next kind of node can be worked.
                var tier = skill > 1 ? db.NodesFor(id).Find(n => n.skill == skill) : null;
                if (tier != null) ShowToast(trade.name.ToUpperInvariant(), "You can work " + tier.name.ToLowerInvariant() + "s now");
            };
        }
        /// <summary>The zone builder asks what a kind of node is (IZoneNodeKinds): from the trades' content, before the session starts.</summary>
        public Crulanda.World.ZoneNodeKind NodeKind(string id)
        {
            var n = ProfessionContent()?.Node(id);
            return n == null ? null : new Crulanda.World.ZoneNodeKind { id = n.id, name = n.name, look = n.look, prompt = n.prompt, variant = n.variant };
        }
        /// <summary>The zones (display names, this one first) that have a node of this kind, from their data, for the Trades window.</summary>
        public List<string> ZonesWithNode(string nodeId)
        {
            var list = new List<string>(); if (Zone == null || string.IsNullOrEmpty(nodeId)) return list;
            foreach (var z in Zone.AllZones())
            {
                bool has = false;
                if (z.nodes != null) foreach (var n in z.nodes) if (n != null && n.node == nodeId) has = true;
                if (z.props != null) foreach (var p in z.props) if (p != null && p.node == nodeId) has = true;
                if (has) { if (z.id == Zone.Zone.id) list.Insert(0, z.displayName); else list.Add(z.displayName); }
            }
            return list;
        }
        /// <summary>Raises a toast (the banner a find raises): a small kicker over a name. Toasts raised together queue up.</summary>
        public void ShowToast(string kicker, string name) { toasts.Enqueue((kicker, name)); AdvanceToast(); }
        /// <summary>Opens or closes the Trades window. Opening it closes the character sheet and a merchant, and opens the bags beside it.</summary>
        public void ShowTrades(bool open)
        {
            if (open && Professions == null) { Message("You have no trade to speak of yet."); return; }
            TradesOpen = open;
            if (open) { CharacterOpen = false; CloseVendor(); InventoryOpen = true; }
        }
        /// <summary>A trade bag used from the bags: worn for good, its slots added under the bags' 24, or refused and kept ("You already carry one.").</summary>
        bool WearBag(int bagIndex)
        {
            if (Items == null) return false;
            var d = Items.Get(Progress.bag[bagIndex].item);
            if (!Inventory.Wear(Progress, Items, bagIndex, out var why)) { if (why != null) Message(why); return false; }
            Message("You hang the " + char.ToLowerInvariant(d.name[0]) + d.name.Substring(1) + " at your hip: " + d.slots + " slots for " + Inventory.HoldsWords(d.holds) + ".");
            Save(false); return true;
        }
        /// <summary>A gathering tool used from the bags: it teaches its skill and hangs at the belt, or is refused and kept ("You already carry one.").</summary>
        bool UseTool(int bagIndex)
        {
            if (Professions == null) { Message("You have no use for that yet."); return false; }
            if (!Professions.UseTool(bagIndex, out var why)) { if (why != null) Message(why); return false; }
            Save(false); return true;
        }

        // ---------- gathering: ore seams, windfalls and herbs (ZoneBuilder's nodes; the Yarrow props are nodes too) ----------
        public const string FightingLine = "You can't do that while fighting.", WorkStoppedLine = "You stop working.";
        /// <summary>When each worked node (ZoneInteractable.Key) can be worked again, on Time.time. Static, so leaving a zone and
        /// coming back does not refill its nodes; not saved, so a restart does.</summary>
        static readonly Dictionary<string, float> nodeReadyAt = new Dictionary<string, float>();
        /// <summary>Tests only: every node comes back at once.</summary>
        public static void ForgetRestingNodes() { nodeReadyAt.Clear(); }
        readonly System.Random gatherRng = new System.Random();
        /// <summary>
        /// E on a node: the refusals (in a fight; the trade's tool not at the belt; bags full when no quest wants what it gives),
        /// then the work on the cast bar: 2 s for ore and timber and 1.5 s for herbs, twice that when the skill is under the
        /// node's (a node above the skill refuses: "Requires Mining 60."). Nothing starts while working or casting (the two share the cast
        /// bar). Moving, being hit, a fight or dying stops it.
        /// </summary>
        void TryGather(Crulanda.World.ZoneInteractable i)
        {
            if (Working || abilities.IsCasting) return;
            var def = Professions.Db.Node(i.node);
            if (def == null) { Message("You look it over, but find nothing you need right now."); return; }
            if (InCombat) { Message(FightingLine); return; }
            if (!Professions.CanGather(def, out bool hard, out string why)) { Message(why); return; }
            bool questWants = Quests != null && !string.IsNullOrEmpty(i.item) && Quests.Wants(i.item, i.name);
            if (Inventory.Room(Progress, Items, def.item) == 0 && !questWants) { Message(ProfessionLog.BagsFullLine); return; }
            var trade = Professions.Db.Profession(def.profession);
            StartWork(WorkLabel(trade.verb), Professions.WorkSeconds(def, hard), () => GatherNow(i));
        }
        /// <summary>"Mining", "Cutting", "Gathering": the work bar's label from the trade's verb.</summary>
        static string WorkLabel(string verb)
        {
            if (string.IsNullOrEmpty(verb)) return "Working";
            if (verb.EndsWith("e")) return verb.Substring(0, verb.Length - 1) + "ing";
            if (verb.Length == 3 && "aeiou".IndexOf(verb[1]) >= 0 && "aeiouwy".IndexOf(verb[2]) < 0) return verb + verb[2] + "ing";
            return verb + "ing";
        }
        /// <summary>
        /// A node worked to the end (the work bar's completion; tests call it straight): its yield into the bags ("+2 Crowsfoot
        /// copper ore") and the skill roll, then what a quest wants of it as before (a Yarrow gives a quest's yarrow while one is
        /// wanted). Only when something went into the bags or a quest took it does it rest until its respawn and the game save;
        /// otherwise (the bags filled while the work went on) it says why and the node stays. True when something went into the bags.
        /// </summary>
        public bool GatherNow(Crulanda.World.ZoneInteractable i)
        {
            if (i == null || i.node == null || Professions == null || Zone == null) return false;
            var def = Professions.Db.Node(i.node); if (def == null) return false;
            int got = Professions.Gather(def, gatherRng, out string why);
            if (got > 0) FloatText(i.position, "+" + got + " " + ItemName(def.item), new Color(.86f, .95f, .66f));
            bool quest = QuestUse(i);
            if (got == 0 && !quest) { if (why != null) Message(why); return false; }   // nothing went in and no quest took it: the node stays
            RestNode(i, def.respawn);
            Save(false);
            return got > 0;
        }
        /// <summary>A worked node rests: it cannot be used until then, and its part (or all of it) is hidden until it comes back.</summary>
        void RestNode(Crulanda.World.ZoneInteractable i, float seconds)
        {
            if (seconds <= 0 || Zone == null) return;
            i.hiddenUntil = Time.time + seconds; nodeReadyAt[i.Key(Zone.Zone.id)] = i.hiddenUntil;
            var hide = i.part != null ? i.part : i.root;
            if (hide != null) StartCoroutine(HideFor(hide, seconds));
        }
        bool nodesRested;
        /// <summary>After a zone is built or a save loaded: nodes worked a little while ago are still resting (nodeReadyAt).</summary>
        void TickNodes()
        {
            if (nodesRested || Zone == null) return;
            nodesRested = true;
            foreach (var i in Zone.Interactables)
                if (i.node != null && Time.time >= i.hiddenUntil && nodeReadyAt.TryGetValue(i.Key(Zone.Zone.id), out var at) && at > Time.time) RestNode(i, at - Time.time);
        }
        // The work bar: gathering (and later crafting) shares the cast bar with abilities.
        string workName; float workStart, workSeconds; Vector3 workAt; int workHealth; Action workDone; RecipeDef workRecipe;
        /// <summary>Whether the player is at work (a node being worked) on the cast bar.</summary>
        public bool Working { get { return workDone != null; } }
        void StartWork(string name, float seconds, Action done, RecipeDef recipe = null)
        {
            workRecipe = recipe; workName = name; workStart = Time.time; workSeconds = Mathf.Max(.1f, seconds); workAt = Player.transform.position; workHealth = Player.Health.Pool.Current; workDone = done;
        }
        /// <summary>Stops the work in hand, if any (nothing is gathered), saying so when <paramref name="say"/>.</summary>
        public void CancelWork(bool say = false) { if (workDone == null) return; workDone = null; workName = null; if (say) Message(WorkStoppedLine); }
        void TickWork()
        {
            if (workDone == null) return;
            var p = Player.transform.position; int health = Player.Health.Pool.Current;
            if (!Player.IsAlive) { CancelWork(); return; }
            if (new Vector2(p.x - workAt.x, p.z - workAt.z).magnitude > .3f || health < workHealth || InCombat) { CancelWork(true); return; }
            workHealth = health;   // it may climb (resting heals); only a fall stops the work
            if (Time.time - workStart < workSeconds) return;
            var done = workDone; workDone = null; workName = null; done();
        }

        // ---------- stations: making things (ZoneBuilder.Stations; DESIGN 6.1, BUILD_PLAN step 9) ----------
        /// <summary>How near a station must be (ground distance, as E measures) for its recipes to be made, and for E to offer it.</summary>
        public const float StationRange = 5;
        /// <summary>How long making one thing takes on the work bar.</summary>
        public const float CraftSeconds = 2;
        readonly System.Random craftRng = new System.Random();
        /// <summary>The nearest station of a kind (forge, bench, fire; null: any kind) within StationRange, or null. A smithy, a bake
        /// oven, the herbalist's drying hut, an inn's kitchen and hearth, and a zone's own anvils, benches and cookfires; nobody need
        /// be there. One behind walls counts only from its side of them (ZoneStationSpot.Reaches: the inn's hearth from the taproom).</summary>
        public Crulanda.World.ZoneStationSpot StationNear(string kind)
        {
            if (Zone == null || Player == null) return null;
            Crulanda.World.ZoneStationSpot best = null; float bestD = StationRange; var at = Player.transform.position;
            foreach (var s in Zone.Stations) { if (kind != null && s.kind != kind) continue; float d = GroundDistance(s.position); if (d <= bestD && s.Reaches(at)) { best = s; bestD = d; } }
            return best;
        }
        bool StationOk(string kind) { return StationNear(kind) != null; }
        /// <summary>The station a recipe would be made at now: the nearest of its kinds in reach, or null.</summary>
        public Crulanda.World.ZoneStationSpot StationFor(RecipeDef r)
        {
            Crulanda.World.ZoneStationSpot best = null;
            if (r == null || Player == null) return null;
            foreach (var kind in ProfessionDatabase.Stations(r.station)) { var s = StationNear(kind); if (s != null && (best == null || GroundDistance(s.position) < GroundDistance(best.position))) best = s; }
            return best;
        }
        /// <summary>Whether a recipe can be made here and now, and why not (ProfessionLog.CanCraft with the stations in reach).</summary>
        public bool CanCraft(RecipeDef r, out string why)
        {
            if (Professions == null) { why = "You have no trade to speak of yet."; return false; }
            return Professions.CanCraft(r, StationOk, out why);
        }
        /// <summary>
        /// The trade a station opens the Trades window on: the first (in the content's order) whose own station it is and that the
        /// character has, with recipes there (Cooking at a fire, Blacksmithing at a forge once taken up); else the first the character
        /// has with a recipe there (Woodcutting's charcoal at a forge or a fire); else the craft whose own station it is while a craft
        /// slot is free (Blacksmithing at a forge, to be taken up there); else the first with a recipe there; else the one whose station
        /// it is.
        /// </summary>
        public ProfessionDef StationTrade(string kind)
        {
            if (Professions == null) return null;
            var db = Professions.Db;
            bool Makes(ProfessionDef d) { return db.RecipesFor(d.id).Exists(r => Array.IndexOf(ProfessionDatabase.Stations(r.station), kind) >= 0); }
            bool Own(ProfessionDef d) { return Array.IndexOf(ProfessionDatabase.Stations(d.station), kind) >= 0; }
            return db.Order.Find(d => Own(d) && Professions.Has(d.id) && Makes(d)) ?? db.Order.Find(d => Professions.Has(d.id) && Makes(d))
                ?? db.Order.Find(d => Own(d) && d.kind == "craft" && Professions.CanLearn(d.id, out _)) ?? db.Order.Find(Makes) ?? db.Order.Find(Own);
        }
        /// <summary>What E offers at a station: "Work at the forge", "Work at the bench", or at a fire "Cook at the fire" when it opens
        /// on Cooking ("Work at the fire" while the fire's only use is another trade's, such as charcoal).</summary>
        public string StationPrompt(Crulanda.World.ZoneStationSpot s)
        {
            if (s == null) return null;
            if (s.kind == "fire") return StationTrade("fire")?.id == "cooking" ? "Cook at the fire" : "Work at the fire";
            return "Work at the " + s.kind;
        }
        /// <summary>E at a station: the Trades window opens on the trade it serves (StationTrade), at its recipes.</summary>
        public void WorkAtStation(Crulanda.World.ZoneStationSpot s)
        {
            if (s == null) return;
            if (Professions == null) { Message("You have no trade to speak of yet."); return; }
            var trade = StationTrade(s.kind);
            if (trade != null && trade.id != EncounterHud.TradesPage) { EncounterHud.TradesPage = trade.id; EncounterHud.TradesRecipe = null; }
            EncounterHud.TradesRecipes = true; ShowTrades(true);
        }
        /// <summary>
        /// Makes a recipe <paramref name="count"/> times (the Trades window's Make and Make all), one at a time on the work bar
        /// (CraftSeconds each, labelled with the recipe's name). Refused, and nothing starts, while working or casting, in a fight
        /// ("You can't do that while fighting."), or when CanCraft says why. Each one made: the inputs leave the bags, what it
        /// makes goes in ("You make Charcoal."), the skill is rolled and the game saves; the next starts while it still can be made,
        /// and when it cannot the rest stop with CanCraft's reason ("Your bags are full."). Moving, a blow, a fight or dying stops the
        /// rest (TickWork). With the trade's own person awake beside you each one takes half as long (CraftTime). True when the first
        /// was started.
        /// </summary>
        public bool Make(RecipeDef r, int count)
        {
            if (Professions == null || r == null || count < 1 || Player == null || !Player.IsAlive || Working || abilities.IsCasting) return false;
            if (InCombat) { Message(FightingLine); return false; }
            if (!CanCraft(r, out var why)) { Message(why); return false; }
            StartWork(r.name, StartCraft(r), () => MakeOne(r, count - 1), r);
            return true;
        }
        void MakeOne(RecipeDef r, int more)
        {
            if (!Professions.Craft(r, StationOk, craftRng, out var why)) { Message(why); return; }
            int n = Math.Max(1, r.count);
            Message("You make " + ItemName(r.output) + (n > 1 ? " x" + n : "") + ".");
            FloatText(Player.transform.position, "+" + n + " " + ItemName(r.output), new Color(.86f, .95f, .66f));
            Save(false);
            if (more > 0) { if (Professions.CanCraft(r, StationOk, out var stop)) StartWork(r.name, StartCraft(r), () => MakeOne(r, more - 1), r); else Message(stop); }
        }

        // ---------- crafts: taking one up, forgetting it, and the trade's own people (DESIGN 4, 6.2; BUILD_PLAN step 12) ----------
        /// <summary>How near a trainer (a villager of the craft's trainer role) must be to start you off at a craft away from its station.</summary>
        public const float TrainerRange = 6;
        /// <summary>How near the trade's own person must be, awake, to lend a hand at the work (it then goes twice as fast).</summary>
        public const float HelperRange = 8;
        /// <summary>What taking up a craft says when no trainer is near and the craft has no line of its own (GAME-ONLY).</summary>
        public const string TakeUpAloneLine = "You look over the tools and begin.";
        /// <summary>
        /// The nearest villager of a trade (blacksmith, herbalist) within <paramref name="range"/> (ground distance, as E measures) who is
        /// awake and about: in sight, not abed or on the way there, not slumped over a table. Null when there is none, or no village.
        /// </summary>
        public Villager TradeNpcNear(string role, float range)
        {
            if (string.IsNullOrEmpty(role) || Player == null || VillageLife.Active == null) return null;
            Villager best = null; float bestD = range;
            foreach (var v in VillageLife.Active.Villagers)
            {
                if (v == null || v.Role != role || !v.Visible || v.PassedOut || v.Activity == "sleep") continue;
                float d = GroundDistance(v.transform.position); if (d <= bestD) { best = v; bestD = d; }
            }
            return best;
        }
        /// <summary>
        /// Whether a craft can be taken up here and now, and why not: ProfessionLog.CanLearn (a craft, not yet taken up, a slot free:
        /// "Two crafts already. Forget one first."), then the place: a station of its kind within StationRange (a forge for
        /// Blacksmithing, a herbalist's bench for Alchemy; nobody need be there), or one of its trainers awake within TrainerRange.
        /// </summary>
        public bool CanTakeUp(string id, out string why)
        {
            if (Professions == null) { why = "You have no trade to speak of yet."; return false; }
            if (!Professions.CanLearn(id, out why)) return false;
            var d = Professions.Db.Profession(id);
            foreach (var kind in ProfessionDatabase.Stations(d.station)) if (StationNear(kind) != null) return true;
            if (TradeNpcNear(d.trainerRole, TrainerRange) != null) return true;
            why = d.name + " is taken up at " + ProfessionLog.StationWords(d.station) + (string.IsNullOrEmpty(d.trainerRole) ? "." : ", or from a " + d.trainerRole + ".");
            return false;
        }
        /// <summary>
        /// Takes up a craft (the Trades window's "Take up" button) when CanTakeUp allows it, at skill 1. A trainer near answers in their
        /// own words, turning to you ("Brannoc Vell: Mind the scale. Copper first; it forgives you."); with nobody there the craft's own
        /// line is said ("You look over the anvil, the tongs and the quench tub, and begin."). A toast says it ("BLACKSMITHING / Taken
        /// up") and the game saves. Refused with CanTakeUp's reason otherwise. True when it was taken up.
        /// </summary>
        public bool LearnCraft(string id)
        {
            if (!CanTakeUp(id, out var why)) { if (why != null) Message(why); return false; }
            var d = Professions.Db.Profession(id); var trainer = TradeNpcNear(d.trainerRole, TrainerRange);
            if (!Professions.Learn(id, out why)) { Message(why); return false; }
            var line = trainer != null ? ProfessionDatabase.LineFor(d.trainerLines, trainer.Name) : null;
            if (line != null) { Message(trainer.Name + ": " + line); trainer.Say(line, 7); trainer.FacePlayer(); }
            else Message(string.IsNullOrEmpty(d.takeUp) ? TakeUpAloneLine : d.takeUp);
            ShowToast(d.name.ToUpperInvariant(), "Taken up");
            Save(false);
            return true;
        }
        /// <summary>
        /// Forgets a craft that was taken up (the Trades window's Forget, after its confirm): the skill is lost, the slot is free, its own work
        /// in hand stops and the game saves ("You put Blacksmithing aside. Skill 47 is lost."). Cooking and the gathering skills are refused
        /// with why ("Cooking stays with you. It can't be forgotten."). True when it was forgotten.
        /// </summary>
        public bool ForgetCraft(string id)
        {
            if (Professions == null) return false;
            if (!Professions.CanForget(id, out var why)) { Message(why); return false; }
            var d = Professions.Db.Profession(id); int skill = Professions.Skill(id);
            if (Working && workRecipe != null && workRecipe.profession == id) CancelWork(true);   // only its own work: charcoal or a node goes on
            Professions.Forget(id);
            Message("You put " + d.name + " aside. Skill " + skill + " is lost.");
            Save(false);
            return true;
        }
        /// <summary>The trade's own person awake beside you for a recipe (a blacksmith at the forge for Blacksmithing's), or null: only
        /// a trade with a trainer role has one.</summary>
        public Villager CraftHelper(RecipeDef r)
        {
            var d = r == null || Professions == null ? null : Professions.Db.Profession(r.profession);
            return d == null || string.IsNullOrEmpty(d.trainerRole) ? null : TradeNpcNear(d.trainerRole, HelperRange);
        }
        /// <summary>How long making one of a recipe takes now: CraftSeconds, or half that with the trade's own person awake within HelperRange.</summary>
        public float CraftTime(RecipeDef r) { return CraftHelper(r) != null ? CraftSeconds / 2 : CraftSeconds; }
        string helpedBy; float helpedUntil;
        /// <summary>The work time for the next one made, and the helper's word: the first time they lend a hand on a visit, chat says so
        /// ("Brannoc Vell works the bellows for you.") and they say a line of their own. A visit lasts while you keep at the work; a
        /// minute away from it and the next one is a new visit.</summary>
        float StartCraft(RecipeDef r)
        {
            var helper = CraftHelper(r); if (helper == null) return CraftSeconds;
            if (helper.Name != helpedBy || Time.time > helpedUntil)
            {
                var d = Professions.Db.Profession(r.profession);
                if (!string.IsNullOrEmpty(d.helping)) Message(d.helping.Replace("{name}", helper.Name));
                var line = ProfessionDatabase.LineFor(d.helpLines, helper.Name); if (line != null) helper.Say(line, 5);
            }
            helpedBy = helper.Name; helpedUntil = Time.time + 60;
            return CraftSeconds / 2;
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
                string z = !string.IsNullOrEmpty(o.zone) ? o.zone : (o.type == "talk" || o.type == "deliver" || o.type == "bring") ? ZoneOfPerson(o.target) : null;
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
            if (i.kind == "board") { OpenBoard(i); return; }
            if (i.kind == "chest") { OpenChest(i); return; }   // EncounterSession.Loot.cs
            if (i.node != null && Professions != null) { TryGather(i); return; }   // a node (a seam, a windfall, a herb) is worked with its trade
            if (Quests == null) { Message("Nothing here you need."); return; }
            if (!QuestUse(i)) { Message(string.IsNullOrEmpty(i.item) ? "You look it over, but find nothing you need right now." : "You don't need any of this right now."); return; }
            // Used up: once-only things stay done (saved); herbs regrow after a while.
            if (i.once) { Progress.usedInteractables.Add(i.Key(Zone.Zone.id)); if (i.Vanishes && i.root != null) HideProp(i.root); }
            else if (i.Vanishes) { i.hiddenUntil = Time.time + 90; if (i.root != null) StartCoroutine(HideFor(i.root, 90)); }
            Save(false);
        }
        /// <summary>What a quest makes of using a prop: its item while a quest wants it, and the "interact" step. True when it mattered.</summary>
        bool QuestUse(Crulanda.World.ZoneInteractable i)
        {
            if (Quests == null) return false;
            bool mattered = false;
            int before = CountQuestProgress();
            if (!string.IsNullOrEmpty(i.item) && Quests.Wants(i.item, i.name)) { Quests.GiveItem(i.item); mattered = true; }
            Quests.Notify("interact", i.name); ReconcileQuests();
            return mattered || CountQuestProgress() != before;
        }
        int CountQuestProgress() { int n = 0; foreach (var s in Progress.quests) { n += s.step * 100; foreach (var c in s.counts) n += c; } return n + Progress.questsDone.Count * 10000; }
        static void HideProp(Transform t) { foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; foreach (var l in t.GetComponentsInChildren<Light>()) l.enabled = false; }   // its glow goes with it
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
            Quests.CheckVisits(o => (string.IsNullOrEmpty(o.zone) || o.zone == Zone.Zone.id) && (!o.night || Crulanda.World.WorldClock.Darkness > .5f) && Vector2.Distance(new Vector2(p.x, p.z), o.at) <= o.radius && OnItsLevel(o.at, p));
            if (Conversation != null && Vector3.Distance(p, Conversation.where) > 6) Conversation = null;   // walked away
            // Emptied props stay empty after a reload.
            if (!emptiedHidden) { emptiedHidden = true; foreach (var i in Zone.Interactables) if (i.once && i.Vanishes && i.root != null && Progress.usedInteractables.Contains(i.Key(Zone.Zone.id))) HideProp(i.root); }
        }
        bool emptiedHidden;
        /// <summary>A place down a cave is visited from down in the cave, not from the hill over it: where a passage floor lies under the
        /// place, the visitor must be standing within four metres of that floor's height. (At a cave's mouth the two are the same.)</summary>
        static bool OnItsLevel(Vector2 place, Vector3 visitor)
        {
            float floor = 0; return !Crulanda.World.Hollow.FloorUnder(place, ref floor) || Mathf.Abs(visitor.y - floor) < 4;
        }

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
        // ---------- points of interest and achievements (Achievements) ----------
        /// <summary>Points of interest and achievements: the zone's places, explored by walking into them, and the deeds counted.</summary>
        public Achievements Feats { get; private set; }
        /// <summary>Exploring and earning are on in play; off in test runs and capture tours (a toast in the middle of a test or a
        /// shot), where a test that wants them turns them on.</summary>
        [NonSerialized] public bool Exploring;
        float nextPoi, nextFeat; bool featsPrimed;
        void TickFeats()
        {
            if (Zone == null || Progress == null || Player == null) return;
            if (Feats == null) { Feats = new Achievements(Progress, Zone.AllZones(), Quests != null ? Quests.Db : null); Feats.Earned = OnEarned; featsPrimed = false; }
            if (!Exploring || Paused || !Player.IsAlive) return;
            if (!featsPrimed)
            {
                // A save from before achievements: what was already done is recorded at once, quietly, in one line.
                featsPrimed = true; var old = Feats.Check(true);
                if (old.Count > 0) Message("Achievements recorded from your past deeds: " + old.Count + " (" + Feats.Points + " points). See the quest book [L].");
            }
            if (Time.time >= nextPoi) { nextPoi = Time.time + .25f; ExplorePlaces(); }
            if (Time.time >= nextFeat) { nextFeat = Time.time + 1; Feats.Check(); }
        }
        /// <summary>Explores any place of this zone the player stands in: its name on the banner, its experience, and the maps name it.</summary>
        public void ExplorePlaces()
        {
            if (Feats == null || Zone == null) return;
            var z = Zone.Zone; var p = Player.transform.position;
            foreach (var l in Achievements.Pois(z))
            {
                if (Feats.Explored(z, l) || new Vector2(p.x - l.at.x, p.z - l.at.y).magnitude > Achievements.Reach(l)) continue;
                Feats.Explore(z, l); int before = Progress.Level, xp = Achievements.PoiXp(z);
                Progress.experience += xp;
                Message("Discovered: " + l.name + "  +" + xp + " XP");
                toasts.Enqueue(("DISCOVERED", l.name)); AdvanceToast();
                if (Progress.Level > before) { ApplyLevel(); Player.Health.ApplyHealing(40); Message("Level " + Progress.Level + "! Health and weapon damage increased."); }
            }
        }
        void OnEarned(AchievementDef a)
        {
            toasts.Enqueue((Achievements.Kicker + "  +" + a.points, a.name)); AdvanceToast();
            Message("Achievement: " + a.name + " (+" + a.points + " points)" + (a.title != null ? ". Title earned: \"" + a.title + "\" (wear it from the Achievements tab [L])." : "."));
        }
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
                if (Kit != null && !Kit.MeleeAutoAttacks) return abilities.IsCasting ? "Casting " + abilities.Casting.name : "No weapon swings in this form";
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
        /// <summary>A cast in progress, or work (gathering) on the same bar.</summary>
        public bool PlayerCasting { get { return abilities.IsCasting || Working; } }
        /// <summary>The player's figure (blows, flinches and casts on it: playtest note 25).</summary>
        ActorVisual PlayerFigure { get { if (playerFigure == null && Player != null) playerFigure = Player.GetComponent<ActorVisual>(); return playerFigure; } }
        ActorVisual playerFigure; bool castPosed;
        /// <summary>A cast being drawn holds the spell pose; when it ends standing (not moved off it), the spell is let fly.</summary>
        void TickCastPose()
        {
            var f = PlayerFigure; if (f == null) return;
            bool drawing = abilities.IsCasting;
            if (castPosed && !drawing && Player.IsAlive && !Player.GetComponent<AdventurerMotor>().Moving) f.CastRelease();
            if (drawing != castPosed) { f.Casting = drawing; castPosed = drawing; }
        }
        public float PlayerCastProgress { get { return abilities.IsCasting ? abilities.CastProgress(Time.time) : Working ? Mathf.Clamp01((Time.time - workStart) / workSeconds) : 0; } }
        public string PlayerCastName { get { return abilities.IsCasting ? abilities.Casting.name : workName; } }

        // ---------- helpers for class kits ----------
        /// <summary>Spends the player's class resource and starts the ability (casts complete later via Update).</summary>
        public AbilityStartResult StartAbility(AbilityDefinition a, Action effect)
        {
            var result = abilities.TryStart(a, Time.time, cost => {
                if (Player.Resource.Pool.Current < cost) return false;
                Player.Resource.Pool.Change(-cost); return true;
            }, effect);
            if (result == AbilityStartResult.InsufficientResource) Message("Not enough " + ClassDef.resource + ".");
            // An instant blow or spell on a target shows (playtest note 25): a swing in melee, a release at range. Casts are TickCastPose's.
            if (result == AbilityStartResult.Started && a.castTime <= 0 && Target != null && Target.actor.IsAlive)
            { if (Kit.MeleeAutoAttacks || Distance(Target) < 3.5f) PlayerFigure?.Strike(); else PlayerFigure?.CastRelease(); }
            return result;
        }
        public bool EnemyInRange(EncounterEnemy enemy, float range) { return enemy != null && enemy.actor.IsAlive && Distance(enemy) <= range; }
        /// <summary>How far past its range a cast or a blow still lands when it goes off (the target stepped back while it was
        /// drawn or cast): the classic leeway, so a bolt loosed at the edge of range is not lost (playtest note 20).</summary>
        public const float LandingLeeway = 5;
        /// <summary>Whether a cast or a blow begun on <paramref name="enemy"/> lands now: alive and within its range plus the leeway.
        /// One that is alive but out of reach says so, so a lost cast is never silent.</summary>
        public bool LandsOn(EncounterEnemy enemy, float range)
        {
            if (enemy == null || !enemy.actor.IsAlive) return false;
            if (Distance(enemy) <= range + LandingLeeway) return true;
            Message("Out of range."); return false;
        }
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
        public bool InCombat { get { return FightingTarget || Enemies.Exists(e => e != null && e.Engaged); } }
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
            // Who keeps a staff in hand on the run (playtest note 19): an enemy while engaged; anyone else while the player fights.
            ActorVisual.Fighting = go => { var e = go.GetComponent<EncounterEnemy>(); return e != null ? e.Engaged : InCombat; };
            Crulanda.World.WorldWeather.Turned += OnWeatherTurned;
            // An editor running tests from the command line (the validation copy) never reads or writes the real save folder, even
            // in a test that forgets to point the session elsewhere: the real folder is the player's, shared by every build.
            bool testRun = Application.isEditor && (Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(), "-runTests") >= 0);
            Exploring = !testRun && !UseTempSave && !EncounterCapture.Requested && SaveDirectoryOverride == null;
            if ((UseTempSave || testRun || Array.IndexOf(Environment.GetCommandLineArgs(), "--crulanda-temp-save") >= 0) && SaveDirectoryOverride == null)
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
                Talents = TalentTree.Parse(ClassDef.id, playable.talentTree != null ? playable.talentTree.text : null, EncounterProgress.TalentCap, Kit.ImplementedTalents);
            }
            catch (ArgumentException error) { Debug.LogError("Class content invalid: " + error.Message); enabled = false; return; }
            saves = new EncounterSave(root, Talents, EncounterSave.SlotFor(ClassDef.id));
            if (saves.Read(out var p, out var message)) { Progress = p; if (message != null) Message(message); }
            else
            {
                Progress = FreshProgress(ClassDef.id); ArmNewCharacter();
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
            LoadItems(); LoadLoot(); LoadProfessions();
            SpawnParty();
            // Villagers and critters live alongside the encounter (they survive load/respawn of the party).
            if (Zone != null && Zone.Zone.life != null) new GameObject("Village life").AddComponent<VillageLife>().Init(this);
            StartQuests(); StartDiscoveries(); StartArmoury();
            Message(Zone != null ? Zone.Zone.displayName + ". " + Objective(0, "") + "." : "Recruit the healer at camp [E], then follow the path to the sentries.");
            ReconcileQuests();
            nextSave = Time.time + 30;
        }
        public static EncounterProgress FreshProgress(string classId = "class.warrior")
        {
            var p = new EncounterProgress { classId = classId, playerId = EntityId.New().Value, companionId = EntityId.New().Value };
            Inventory.Ensure(p); return p;
        }
        /// <summary>A look's odd variant from the mob's name: ash hounds, withered or grey Keepers and briars, the Hollow Root-Warden, a doe.</summary>
        static int LookVariant(string label)
        {
            foreach (var word in new[] { "Ash", "Withered", "Greyheart", "Hollow Root", "doe" }) if (label != null && label.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return 0;
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
            ActorVisual.Attach(go, look, LookVariant(label));
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
            Inventory.Ensure(Progress); if (Items != null) Inventory.EnsurePouches(Progress, Items);
            playerStats.Configure(ClassDef.stats, 0);

            Player.ConfigureResource(ClassDef.resource, ClassDef.maxResource);
            ApplyLevel();
            Kit.ApplyStats();
            ApplyEquipment();
            SetHealth(Player, Progress.health);
            Player.Health.Died += h => {
                AutoAttack = false; abilities.Interrupt(); CancelWork(); Message("You fell. Press R to recover at camp. Your equipment is kept.");
                var look = Player.GetComponent<ActorVisual>(); if (look != null) look.Pose = ActorPose.None;   // no swimming or sneaking corpse
            };
            lastDry = Player.transform.position;
            var friendPoint = Progress.recruited ? Player.transform.position + Vector3.right * 2 : CompanionPoint;
            // A save made beside (or in) water must not put her in it: snap to the nearest walkable ground.
            if (NavMesh.SamplePosition(friendPoint, out var friendHit, 12, NavMesh.AllAreas)) friendPoint = friendHit.position + Vector3.up * 1.05f;
            var friend = SpawnActor("Mira · provisional healer", content.healer, friendPoint, new Color(.45f, .76f, .57f), Progress.companionId, ActorLook.Healer);
            AddAgent(friend.gameObject, 4.6f);
            Companion = friend.gameObject.AddComponent<HealerCompanion>(); Companion.actor = friend; Companion.session = this;
            friend.gameObject.SetActive(true); Companion.MatchLevel(Progress.Level); SetHealth(friend, CompanionHealthFromSave()); friend.Resource.Pool.SetCurrent(Progress.mana);
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
            SpawnCamps(); SpawnGame();
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
                case "keeper": return ActorLook.Keeper;
                case "stag": return ActorLook.Stag;
                case "spider": return ActorLook.Spider;
                case "bramble": return ActorLook.Bramble;
                case "bear": return ActorLook.Bear;
                case "skeleton": return ActorLook.Skeleton;
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
                    AddAgent(a.gameObject, look == ActorLook.Wolf ? 4.2f : look == ActorLook.Boar ? 3.8f : look == ActorLook.Bear ? 4f : look == ActorLook.WeaveEater ? 3.4f : 2.8f, beast ? .6f : .45f);
                    var enemy = a.gameObject.AddComponent<EncounterEnemy>(); enemy.actor = a; enemy.persistentId = id; enemy.session = this;
                    enemy.Camp = true; enemy.Elite = elite; enemy.RespawnSeconds = Mathf.Max(20, camp.respawn); enemy.CampCenter = camp.center; enemy.CampRadius = camp.radius;
                    enemy.Ambusher = camp.ambush; enemy.Skinnable = look == ActorLook.Wolf || look == ActorLook.Boar || look == ActorLook.Stag || look == ActorLook.Bear;
                    a.gameObject.SetActive(true); enemy.Initialize(); Enemies.Add(enemy);
                    if (camp.ambush) enemy.Hide();
                    a.Stats.SetBase(StatType.MaxHealth, EncounterEnemy.MobHealth(level, false, elite, beast));
                    a.Health.ApplyHealing(a.Health.Pool.Max);
                    enemy.HitBase = EncounterEnemy.MobHit(level, false, elite);
                    if (elite) a.transform.localScale = Vector3.one * 1.18f;
                    ConfigureSocial(enemy, camp, c, level, beast);   // its kind, its kin and, for the elite, its move (EncounterSession.Social)
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
        // Timed for the performance probe (playtest note 23).
        static readonly Unity.Profiling.ProfilerMarker perfMark = new Unity.Profiling.ProfilerMarker("PERF.Session");
        void Update() { using (perfMark.Auto()) UpdateTimed(); }
        void UpdateTimed()
        {
            if (Player == null) return;
            AdvanceToast();
            // The day turns at six in the morning: the boards draw again (Bounties). A rare posting taken on keeps its courier abroad.
            if (lastHour >= 0 && Bounties.DayTurned(lastHour, Crulanda.World.WorldClock.Hour)) Progress.days++;
            lastHour = Crulanda.World.WorldClock.Hour;
            if (courier == null && Boards != null && Zone != null && Boards.ActiveRare(ZoneId) != null && !courierDone) SpawnCourier();
            if (courier != null && !courier.actor.IsAlive) courierDone = true;
            if (EncounterInput.Press(KeyCode.Escape))
            {
                // Esc closes open windows (conversation, quest book, map) before it pauses.
                if (LootOpen && !Paused) CloseLoot();
                else if (Conversation != null && !Paused) Conversation = null;
                else if (VendorNpc != null && !Paused) CloseVendor();
                else if (TradesOpen && !Paused) TradesOpen = false;
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
            if (EncounterInput.Press(KeyCode.C)) { CharacterOpen = !CharacterOpen; if (CharacterOpen) TradesOpen = false; }
            if (EncounterInput.Press(KeyCode.K)) ShowTrades(!TradesOpen);
            if (EncounterInput.Press(KeyCode.F9)) { Load(); return; }
            if (EncounterInput.Press(KeyCode.F5)) Save();
            if (EncounterInput.Press(KeyCode.F10)) PrototypeLevelCap();
            if (Debug.isDebugBuild && EncounterInput.Press(KeyCode.F11)) { Crulanda.World.WorldClock.Advance(1); Message("Time skips ahead: " + Crulanda.World.WorldClock.Text + " (dev)."); }
            if (Debug.isDebugBuild && EncounterInput.Press(KeyCode.F8) && Crulanda.World.WorldWeather.Active != null) Message("Weather: " + Crulanda.World.WorldWeather.Active.CycleForced() + " (dev).");
            TickLoot();
            if (!Player.IsAlive) { if (EncounterInput.Press(KeyCode.R)) Recover(); return; }
            TickQuests(); TickItems(); TickDiscoveries(); TickPlaces(); TickFeats(); TickNodes(); TickWork(); TickCastPose();
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
            { nextSwing = Time.time + Kit.SwingInterval(content.playerSwingInterval); PlayerFigure?.Strike(); Target.Receive(WeaponDamage, Player); Kit.OnAutoHit(); }
            if (Time.time >= nextRegen)
            {
                nextRegen = Time.time + 1; Player.Resource.Pool.Change(InCombat ? ClassDef.combatRegen : ClassDef.restingRegen);
                if (!InCombat) { Player.Health.ApplyHealing(RestRegen); if (Companion.actor.IsAlive) Companion.actor.Health.ApplyHealing(7); }
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
        /// <summary>Who E would talk to: the selected friend when in reach, otherwise the nearest of Mira and the villagers. In reach
        /// is within talk range with nothing solid between (<see cref="InTalkReach"/>): nobody is talked to through a wall.</summary>
        (Villager villager, bool mira) TalkTarget()
        {
            var p = Player.transform.position;
            if (FocusMira && CompanionInReach) return (null, true);
            if (FocusVillager != null && FocusVillager.Visible && InTalkReach(FocusVillager.transform.position, FocusVillager.Role == "child")) return (FocusVillager, false);
            // Nothing selected: whoever is nearest, favouring the one you are facing when two are close.
            var fwd = Player.transform.forward;
            float Reach(Vector3 at) { var d = at - p; d.y = 0; return d.magnitude - .9f * Mathf.Max(0, Vector3.Dot(fwd, d.normalized)); }
            Villager best = null; float bestReach = float.MaxValue;
            if (VillageLife.Active != null)
                foreach (var cand in VillageLife.Active.Villagers)
                {
                    if (!cand.Visible || !InTalkReach(cand.transform.position, cand.Role == "child")) continue;
                    float r = Reach(cand.transform.position); if (r < bestReach) { bestReach = r; best = cand; }
                }
            // Once she walks with you, Mira answers E only when selected (playtest note 32: at your shoulder she took E from the
            // villager you meant and the seam you were mining); to recruit or revive her, being near is enough.
            bool hers = !Progress.recruited || Companion == null || !Companion.actor.IsAlive;
            if (hers && CompanionInReach && (best == null || Reach(Companion.transform.position) <= bestReach)) return (null, true);
            return (best, false);
        }
        public void CycleTarget()
        {
            var nearby = Enemies.FindAll(e => e.actor.IsAlive && !e.Hidden && Distance(e) < 25);
            nearby.Sort((a,b) => Distance(a).CompareTo(Distance(b)));
            if (nearby.Count == 0) nearby = NearbyGame();   // no enemy near: the game animals (hunting step)
            if (nearby.Count == 0) { Target = null; return; }
            Select(nearby[(nearby.IndexOf(Target) + 1) % nearby.Count]);
        }
        public bool UseAbility(int index)
        {
            if (Paused || Player == null || !Player.IsAlive || !ActionUnlocked(index)) return false;
            if (CooldownRemaining(index) > 0) return false;
            bool used = Kit.Use(index);
            if (used) CancelWork(true);   // an ability that goes off takes the hands off the work (the work only ends in TickWork, so it gathers nothing)
            return used;
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
            if (!Player.IsAlive || chaser != null || (FightingTarget && Distance(Target) < 40))
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
        /// <summary>Mira is near enough to talk to, with nothing solid between (<see cref="InTalkReach"/>).</summary>
        public bool CompanionInReach { get { return Companion != null && InTalkReach(Companion.transform.position); } }
        /// <summary>The selected friend (Mira or a villager) can be talked to now, by the same rule as TalkTarget: in talk range with
        /// nothing solid between (<see cref="InTalkReach"/>). The friend frame offers [E] only then.</summary>
        public bool FocusInTalkReach { get { return FocusMira ? CompanionInReach : FocusVillager != null && FocusVillager.Visible && InTalkReach(FocusVillager.transform.position, FocusVillager.Role == "child"); } }
        static readonly RaycastHit[] talkHits = new RaycastHit[16];
        /// <summary>
        /// Whether someone standing at <paramref name="at"/> (their root, about a metre over their feet; a child's lower) can be
        /// talked to: within talk range, and a clear line from the player's head to theirs, or to a hand either side of it, as a
        /// station's walls are kept by its room (ZoneStationSpot.Reaches). Walls, roofs, rock and the ground block it, cast both ways
        /// (a one-sided mesh blocks only from its front); people, trees and triggers don't, and a counter, a table or a door's step is
        /// below the line. The side lines keep a post or a pole from ending a conversation that a wall would.
        /// </summary>
        bool InTalkReach(Vector3 at, bool child = false)
        {
            if (Player == null) return false;
            var p = Player.transform.position; if (Vector3.Distance(p, at) >= TalkRange) return false;
            Vector3 eye = p + Vector3.up * .55f, head = at + Vector3.up * (child ? .35f : .55f), side = Vector3.Cross(Vector3.up, head - eye); side.y = 0;
            side = side.sqrMagnitude > .0001f ? side.normalized * .25f : Vector3.zero;
            return TalkLineClear(eye, head) || TalkLineClear(eye, head + side) || TalkLineClear(eye, head - side);
        }
        static bool TalkLineClear(Vector3 a, Vector3 b) { return TalkRayClear(a, b) && TalkRayClear(b, a); }
        static bool TalkRayClear(Vector3 from, Vector3 to)
        {
            var line = to - from; float length = line.magnitude; if (length < .01f) return true;
            int n = Physics.RaycastNonAlloc(from, line / length, talkHits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = talkHits[i].collider;
                if (c.GetComponentInParent<Actor>() == null && c.GetComponentInParent<Villager>() == null && c.GetComponentInParent<Crulanda.World.TreeFade>() == null) return false;
            }
            return true;
        }
        /// <summary>A body that still has something to take (camp mobs until looted; story enemies by their saved record).</summary>
        public bool CanLoot(EncounterEnemy e) { return e != null && !e.actor.IsAlive && (e.Camp ? !e.Looted : !Progress.FindEnemy(e.persistentId).looted); }
        EncounterEnemy LootableCorpse { get { return Enemies.Find(e => CanLoot(e) && Distance(e) < 3.6f && (!e.Camp || CanTakeAny(e))) ?? SkinnableBody; } }
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
                if (LootOpen) return "Take all";
                var exit = NearbyExit; if (exit != null) return exit.name;
                var body = LootableCorpse; if (body != null) return body.Game || body.Skinnable ? GameAnimals.SkinPrompt : "Search the body";
                var (villager, mira) = TalkTarget();
                if (mira) return !Progress.recruited ? "Recruit Mira" : !Companion.actor.IsAlive ? "Revive Mira" : "Talk to Mira";
                if (villager != null) return "Talk to " + villager.Name;
                var (usable, secret) = NearestUse();
                if (secret != null) return SearchPrompt(secret.def);
                if (usable != null) return usable.prompt;
                var door = NearbyDoor;
                if (door != null) return door.openable ? (door.Open ? "Close the door" : "Open the door") + " · " + door.name : (door.kind == "rooms" ? "Try the door · " : "Knock · ") + door.name;
                return StationPrompt(StationNear(null));   // last: a station reaches farther than a door, so a door at hand is knocked at first
            }
        }
        static readonly string[] BarredDoorLines = {
            "The door is barred. Someone inside holds their breath until you leave.",
            "No answer. Since the collectors came, Oakhaven opens its doors to no one.",
            "A voice through the planks: \"We've nothing left to give. Go away.\"",
            "Locked. Fresh scratches around the latch.",
        };
        /// <summary>What an inn's door to its rooms upstairs says when the player tries it (GAME-ONLY).</summary>
        const string RoomsDoorLine = "The stair up to the rooms. It's kept for the inn's lodgers.";
        public void Interact()
        {
            if (!Player.IsAlive || Paused) return;
            if (LootOpen) { TakeAllLoot(); return; }
            var exit = NearbyExit; if (exit != null) { TravelTo(exit); return; }
            var corpse = LootableCorpse;
            if (corpse != null)
            {
                if (corpse.Camp) { OpenLoot(corpse); return; }
                if (Progress.Loot(corpse.persistentId))
                {
                    if (!Inventory.Has(Progress, content.itemId) && Items != null && Inventory.Add(Progress, Items, content.itemId, 1) == 0)
                    { Message("Looted 8 crowns. " + content.itemName + " is in your bags [I]."); InventoryOpen = true; }
                    else Message("Looted 8 crowns.");
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
                else if (door.kind == "rooms") Message(door.name + ": " + RoomsDoorLine);
                else Knock(door);
                return;
            }
            var station = StationNear(null); if (station != null) { WorkAtStation(station); return; }
            Message("Nothing to interact with here.");
        }
        /// <summary>What answering a knock says when someone home has wares or quest business for you (GAME-ONLY).</summary>
        public const string ShutterLine = "A shutter opens. \"At this hour? Go on, then.\"";
        /// <summary>
        /// A knock at a barred door. Someone at home with wares or quest business for you opens up and deals with you at the door,
        /// as if you had found them; otherwise the household answers (who is abed, where the head of the house is), and a door nobody
        /// lives behind gives one of the old barred-door lines.
        /// </summary>
        void Knock(Crulanda.World.ZoneDoor door)
        {
            var life = VillageLife.Active;
            if (life != null)
                foreach (var v in life.AtHome(door))
                {
                    bool business = Quests != null && Zone != null && Quests.Marker(v.Name, Zone.Zone.id, Progress.Level, out bool grey) != ' ' && !grey;
                    if (!business && !IsVendor(v)) continue;
                    Message(door.name + ": " + ShutterLine);
                    if (!QuestTalk(v.Name, door.position)) OpenVendor(v, door.position);
                    return;
                }
            var line = life != null ? life.KnockLine(door) : null;
            Message(door.name + ": " + (line ?? BarredDoorLines[Mathf.Abs(door.name.GetHashCode() + Time.frameCount / 600) % BarredDoorLines.Length]));
        }
        public void EnemyDied(EncounterEnemy enemy)
        {
            if (restoring) return;
            if (enemy.Game) { GameDied(enemy); return; }   // no experience, no coin, no "zone clear" (EncounterSession.Hunt)
            if (enemy.Camp) RollCorpse(enemy);   // the loot is decided as it dies, so the body can show it
            if (enemy.Camp && enemy.Elite) { if (Feats != null) Feats.Slain(enemy.persistentId); else { var k = Achievements.EliteKey(enemy.persistentId); if (k != null && !Progress.elitesSlain.Contains(k)) Progress.elitesSlain.Add(k); } }
            OnKillEffects();
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
            if (!enemy.Camp && StoryEnemies.TrueForAll(e => !e.actor.IsAlive)) Message(ZoneTitle + " is clear for now. " + (Inventory.IsEquipped(Progress, content.itemId) ? "Save [F5]." : "Equip your reward [I], then save [F5]."));
        }
        void ApplyLevel() { Player.SetLevel(Progress.Level); if (Companion != null) Companion.MatchLevel(Progress.Level); }
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
            Progress = p; Target = null; AutoAttack = false; abilities.Reset(); CancelWork(); nodesRested = false;
            Floating.Clear(); SpawnParty(); Message(error ?? "Saved expedition restored.");
            if (Quests != null) { Quests.Bind(Progress); Conversation = null; emptiedHidden = false; ReconcileQuests(); }
            if (Discoveries != null) { Discoveries.Bind(Progress); pocketedSynced = false; vistaWaiting = null; }
            if (Professions != null) Professions.Bind(Progress);
            if (Armoury != null) Armoury.Bind(Progress);
            if (Feats != null) { Feats.Bind(Progress); featsPrimed = false; }
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







