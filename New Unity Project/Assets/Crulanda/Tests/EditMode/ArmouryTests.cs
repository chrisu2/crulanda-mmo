using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Saves shaped like the owner's own, key for key and in the order the game wrote them; the ids that name a character are this
    /// test's own. On the morning of 2026-10-01 his save was format 6 (a Warrior at 1,329 XP in Oakhaven, the Training Blade in hand,
    /// one wolf pelt in the bags, eight story enemies and four quests); that evening he played the trades build and it became format
    /// 8 (2,137 XP, Caddock's Tin Crown and two rare generated pieces worn, the blade and a generated mantle in the bags).
    /// </summary>
    public static class OwnerSaveFixtures
    {
        static string Slots(int n) { var s = new string[n]; for (int i = 0; i < n; i++) s[i] = "{\"item\":\"\",\"count\":0}"; return string.Join(",", s); }
        /// <summary>His format-8 save of the evening of 2026-10-01.</summary>
        public static readonly string V8Payload =
            "{\"classId\":\"class.warrior\",\"zoneId\":\"zone.oakhaven\",\"talents\":[],\"playerId\":\"0c6f2b8e4d1a4e9f8a7b3c2d1e0f9a8b\",\"companionId\":\"7e6d5c4b3a2f41e09d8c7b6a5f4e3d2c\"," +
            "\"experience\":2137,\"health\":355,\"mana\":120,\"companionHealth\":130,\"x\":-6.805179119110107,\"y\":1.0799998044967651,\"z\":18.035306930541992," +
            "\"recruited\":true,\"relationship\":8,\"gold\":306,\"equippedItem\":\"\",\"inventory\":[]," +
            "\"bag\":[{\"item\":\"junk.wolf_pelt\",\"count\":1},{\"item\":\"junk.company_badge\",\"count\":10},{\"item\":\"potion.minor\",\"count\":3},{\"item\":\"gen.shoulders.5.1.9496\",\"count\":1}," +
            "{\"item\":\"junk.company_badge\",\"count\":2}," + Slots(1) + ",{\"item\":\"item.training_blade\",\"count\":1}," + Slots(17) + "]," +
            "\"equipment\":[{\"item\":\"item.tin_crown\",\"count\":1}," + Slots(5) + ",{\"item\":\"gen.feet.4.3.2421\",\"count\":1},{\"item\":\"gen.mainhand.5.3.5107\",\"count\":1}," + Slots(1) + "]," +
            "\"enemies\":[{\"id\":\"sentry.0\",\"dead\":true,\"looted\":true},{\"id\":\"sentry.1\",\"dead\":true,\"looted\":true},{\"id\":\"sentry.2\",\"dead\":true,\"looted\":true}," +
            "{\"id\":\"oakhaven.collector.0\",\"dead\":true,\"looted\":false},{\"id\":\"oakhaven.collector.1\",\"dead\":true,\"looted\":false},{\"id\":\"oakhaven.warden.0\",\"dead\":false,\"looted\":false}," +
            "{\"id\":\"oakhaven.collector.2\",\"dead\":true,\"looted\":true},{\"id\":\"oakhaven.collector.3\",\"dead\":true,\"looted\":true}]," +
            "\"quests\":[{\"id\":\"main.oakhaven.1\",\"step\":1,\"counts\":[4],\"tracked\":true},{\"id\":\"npc.hedda.flour\",\"step\":1,\"counts\":[0],\"tracked\":true}," +
            "{\"id\":\"npc.lisbet.yarrow\",\"step\":0,\"counts\":[0],\"tracked\":true},{\"id\":\"npc.garet.tracks\",\"step\":0,\"counts\":[0],\"tracked\":true}]," +
            "\"questsDone\":[\"npc.goody.eggs\"],\"reputation\":[{\"faction\":\"oakhaven\",\"value\":100}],\"documents\":[],\"questItems\":[],\"usedInteractables\":[]," +
            "\"discoveries\":[],\"professions\":[{\"id\":\"herbalism\",\"skill\":1},{\"id\":\"cooking\",\"skill\":1}],\"pouches\":[]}";
        /// <summary>His format-6 save of the morning of 2026-10-01.</summary>
        public static readonly string V6Payload =
            "{\"classId\":\"class.warrior\",\"zoneId\":\"zone.oakhaven\",\"talents\":[],\"playerId\":\"0c6f2b8e4d1a4e9f8a7b3c2d1e0f9a8b\",\"companionId\":\"7e6d5c4b3a2f41e09d8c7b6a5f4e3d2c\"," +
            "\"experience\":1329,\"health\":255,\"mana\":120,\"companionHealth\":130,\"x\":-20.41796875,\"y\":3.575676679611206,\"z\":120.16324615478516," +
            "\"recruited\":true,\"relationship\":8,\"gold\":49,\"equippedItem\":\"\",\"inventory\":[]," +
            "\"bag\":[{\"item\":\"junk.wolf_pelt\",\"count\":1}," + Slots(23) + "]," +
            "\"equipment\":[" + Slots(7) + ",{\"item\":\"item.training_blade\",\"count\":1}," + Slots(1) + "]," +
            "\"enemies\":[{\"id\":\"sentry.0\",\"dead\":true,\"looted\":true},{\"id\":\"sentry.1\",\"dead\":true,\"looted\":true},{\"id\":\"sentry.2\",\"dead\":true,\"looted\":true}," +
            "{\"id\":\"oakhaven.collector.0\",\"dead\":true,\"looted\":false},{\"id\":\"oakhaven.collector.1\",\"dead\":true,\"looted\":false},{\"id\":\"oakhaven.warden.0\",\"dead\":false,\"looted\":false}," +
            "{\"id\":\"oakhaven.collector.2\",\"dead\":true,\"looted\":true},{\"id\":\"oakhaven.collector.3\",\"dead\":true,\"looted\":true}]," +
            "\"quests\":[{\"id\":\"main.oakhaven.1\",\"step\":1,\"counts\":[4],\"tracked\":true},{\"id\":\"npc.hedda.flour\",\"step\":1,\"counts\":[0],\"tracked\":true}," +
            "{\"id\":\"npc.lisbet.yarrow\",\"step\":0,\"counts\":[0],\"tracked\":true},{\"id\":\"npc.garet.tracks\",\"step\":0,\"counts\":[0],\"tracked\":true}]," +
            "\"questsDone\":[\"npc.goody.eggs\"],\"reputation\":[{\"faction\":\"oakhaven\",\"value\":100}],\"documents\":[],\"questItems\":[],\"usedInteractables\":[]}";
    }

    /// <summary>
    /// The Armoury (loot DESIGN.md 4 rows 8-10, 7; step L3): named gear is recorded once, the first time it is held; generated gear
    /// adds a look and no entry; the book gives an unknown piece's slot and kind of source and nothing more; killing a boss names
    /// the pieces on its list; a hidden find's piece never names its place; on the first bind what is held and what the finds
    /// already found gave count, quietly; the epic pity count lives in the save. Pure logic: no scene and no session; the only save
    /// is written to a temporary folder of its own.
    /// </summary>
    public class ArmouryTests
    {
        static ItemDatabase items; static GearLooks looks; static LootDatabase loot; static List<ZoneDefinition> zones; static List<ZoneSecret> secrets;
        [OneTimeSetUp] public void Load()
        {
            items = LootTestData.Items(); looks = LootTestData.Looks(); loot = LootTestData.Loot(items, looks); zones = LootTestData.Zones();
            secrets = zones.SelectMany(z => z.secrets ?? new ZoneSecret[0]).Where(s => s != null).ToList();
        }
        /// <summary>A log on a fresh character (or the one given), with what it raises collected.</summary>
        static ArmouryLog Log(out List<string> newItems, out List<string> newLooks, EncounterProgress p = null)
        {
            var log = new ArmouryLog(p ?? EncounterSession.FreshProgress(), items, loot, looks, secrets);
            var a = new List<string>(); var b = new List<string>(); log.NewItem = a.Add; log.NewLook = b.Add; newItems = a; newLooks = b;
            return log;
        }
        /// <summary>The elite (or a normal mob) of the camp of this mob name, as LootContext.From reads a camp mob's id.</summary>
        static LootContext Mob(string zone, string mob, bool elite = true)
        {
            var z = zones.First(x => x.id == "zone." + zone); int c = Array.FindIndex(z.camps, x => x != null && x.mob == mob);
            Assert.GreaterOrEqual(c, 0, mob + " has a camp in " + zone + ".");
            return LootContext.From("mob." + LootContext.CampTag(z.camps[c]) + "." + zone + "." + c + "." + (elite ? 0 : 1), z.camps, z.camps[c].levelMax, elite && z.camps[c].elite);
        }
        static string Kind(GearMeta g) { return LootDatabase.SourceKind(g.source); }
        const string Mantle = "loot.oak.whitefoot_mantle", Cleaver = "loot.oak.due_cleaver", Coat = "loot.oak.due_coat", Crown = "item.tin_crown", Sabre = "loot.oak.broken_oath_sabre", Blade = "item.training_blade";

        [Test] public void Sweep_records_named_gear_once()
        {
            var log = Log(out var newItems, out var newLooks); var p = log.Progress;
            Assert.IsEmpty(p.armoury); Assert.AreEqual(0, log.Sweep(), "Nothing held, nothing new.");
            Assert.AreEqual(0, Inventory.Add(p, items, Mantle, 1));
            Assert.AreEqual(2, log.Sweep(), "The mantle: a new entry and a new look.");
            CollectionAssert.AreEqual(new[] { Mantle }, p.armoury); CollectionAssert.AreEqual(new[] { Mantle }, newItems); CollectionAssert.AreEqual(new[] { Mantle }, newLooks);
            Assert.AreEqual(ArmouryLog.State.Found, log.StateOf(Mantle)); Assert.IsTrue(log.IsFound(Mantle));
            Assert.AreEqual(0, log.Sweep(), "Swept again: nothing new."); Assert.AreEqual(1, newItems.Count); Assert.AreEqual(1, newLooks.Count);
            // Sold or destroyed, it stays found; held again, it is not new.
            Inventory.Destroy(p, p.bag.FindIndex(s => s.item == Mantle)); log.Sweep();
            Assert.IsTrue(log.IsFound(Mantle), "Found stays found.");
            Inventory.Add(p, items, Mantle, 1); Assert.AreEqual(0, log.Sweep()); Assert.AreEqual(1, p.armoury.Count(id => id == Mantle), "Listed once.");
            // Worn counts as held: a piece put straight into the equipment.
            p.equipment[ItemDatabase.SlotIndex(items.Get(Cleaver).slot)] = new ItemStack { item = Cleaver, count = 1 };
            log.Sweep(); CollectionAssert.AreEqual(new[] { Mantle, Cleaver }, p.armoury, "In the order found.");
            // A new log on the same character (a load) knows both and says nothing.
            var again = Log(out var items2, out var looks2, p); Assert.AreEqual(0, again.Sweep());
            Assert.IsTrue(again.IsFound(Mantle) && again.IsFound(Cleaver)); Assert.IsEmpty(items2); Assert.IsEmpty(looks2); Assert.AreEqual(2, p.armoury.Count);
            // Something that is not a named piece of gear is never an entry.
            Inventory.Add(p, items, "junk.wolf_pelt", 2); Inventory.Add(p, items, "potion.minor", 1); int seen = p.looks.Count;
            Assert.AreEqual(0, again.Sweep()); Assert.AreEqual(2, p.armoury.Count); Assert.AreEqual(seen, p.looks.Count, "Junk and potions have no look.");
        }

        [Test] public void Generated_gear_adds_a_look_not_an_entry()
        {
            var log = Log(out var newItems, out var newLooks); var p = log.Progress;
            string gen = ItemDatabase.GearId("mainhand", 4, 2, 77);
            Inventory.Add(p, items, gen, 1);
            Assert.AreEqual(1, log.Sweep());
            Assert.IsEmpty(p.armoury, "No entry: it is not a named piece."); Assert.IsEmpty(newItems);
            CollectionAssert.AreEqual(new[] { gen }, newLooks, "A new look.");
            CollectionAssert.AreEqual(new[] { log.LookOf(items.Get(gen)) }, p.looks); Assert.IsTrue(log.HasSeenLook(p.looks[0])); Assert.AreEqual(1, log.LooksSeen);
            Assert.AreEqual(ArmouryLog.State.Unknown, log.StateOf(gen), "Not in the Armoury at all.");
            // Two pieces that look the same are one look: the second says nothing.
            string[] twins = null;
            foreach (var group in Enumerable.Range(0, 400).Select(seed => ItemDatabase.GearId("chest", 6, 1, seed)).GroupBy(id => log.LookOf(items.Get(id))))
                if (group.Count() > 1 && group.Key != p.looks[0]) { twins = group.Take(2).ToArray(); break; }
            Assert.NotNull(twins, "Some generated chests look alike.");
            Inventory.Add(p, items, twins[0], 1); Inventory.Add(p, items, twins[1], 1);
            Assert.AreEqual(1, log.Sweep(), "One new look for the pair.");
            CollectionAssert.AreEqual(new[] { gen, twins[0] }, newLooks); Assert.AreEqual(2, p.looks.Count);
            // A different quality of the same piece is a different look (its trim and glow differ).
            string rare = ItemDatabase.GearId("mainhand", 4, 3, 77);
            Assert.AreNotEqual(log.LookOf(items.Get(gen)), log.LookOf(items.Get(rare)));
            Assert.IsNull(log.LookOf(items.Get("junk.wolf_pelt"))); Assert.IsNull(log.LookOf(null));
        }

        [Test] public void Unknown_entries_give_slot_and_source_kind_only()
        {
            var log = Log(out _, out _);
            var tallies = ArmouryLog.ZoneShorts.Select(z => log.TallyOf(z.zone)).Concat(new[] { log.TallyOf(ArmouryLog.World) }).ToList();
            Assert.AreEqual(loot.Gear.Count, tallies.Sum(t => t.Total), "Every named piece is listed once: under one of the five zones or the world.");
            Assert.IsTrue(tallies.All(t => t.Total > 0), "Every zone and the world has named pieces.");
            Assert.AreEqual(log.TallyOf("oakhaven").Total, log.TallyOf("zone.oakhaven", "Oakhaven").Total, "A zone id or its short name.");
            int unknown = 0;
            foreach (var t in tallies)
                foreach (var e in t.entries)
                {
                    Assert.GreaterOrEqual(ItemDatabase.SlotIndex(e.slot), 0, "Every entry has its slot.");
                    Assert.IsTrue(ArmouryLog.Kinds.Any(k => k.kind == e.sourceKind), "And its kind of source: " + e.sourceKind);
                    if (e.state != ArmouryLog.State.Unknown) { Assert.AreEqual("vendor", e.sourceKind, "A fresh character knows only what merchants sell."); Assert.NotNull(e.name); continue; }
                    unknown++;
                    Assert.IsNull(e.id, "No id."); Assert.IsNull(e.name, "No name."); Assert.IsNull(e.source, "No place."); Assert.AreEqual(-1, e.quality, "No quality.");
                    Assert.IsFalse(string.IsNullOrEmpty(ArmouryLog.UnknownLine(e.sourceKind)));
                }
            Assert.Greater(unknown, 100, "Nearly everything starts unknown.");
            Assert.AreEqual(0, tallies.Sum(t => t.found));
            // Where each piece is listed: its camp's zone, its boss's zone, its find's zone, its id's zone; world drops under the world.
            Assert.AreEqual("oakhaven", ArmouryLog.ZoneOf(loot.Meta(Crown), loot)); Assert.AreEqual("oakhaven", ArmouryLog.ZoneOf(loot.Meta(Blade), loot), "A quest reward by its quest's zone.");
            Assert.AreEqual("oakhaven", ArmouryLog.ZoneOf(loot.Meta("item.poachers_hood"), loot)); Assert.AreEqual("verdant", ArmouryLog.ZoneOf(loot.Meta("item.wardens_crown"), loot));
            Assert.AreEqual(ArmouryLog.World, ArmouryLog.ZoneOf(loot.Meta("loot.world.golden_cask_tankard"), loot));
        }

        [Test] public void A_killed_boss_reveals_the_names_on_its_list()
        {
            var log = Log(out var newItems, out _); var p = log.Progress;
            var caddock = Mob("oakhaven", "Caddock, the Bandit King");
            foreach (var id in new[] { Crown, Cleaver, Coat, Sabre }) Assert.AreEqual(ArmouryLog.State.Unknown, log.StateOf(id), id + " before.");
            log.Killed(caddock);
            Assert.AreEqual(1, log.KillsOf("drop.oak.caddock"));
            foreach (var id in new[] { Crown, Cleaver, Coat, Sabre })
            {
                Assert.AreEqual(ArmouryLog.State.Known, log.StateOf(id), id + " after.");
                var e = log.EntryOf(loot.Meta(id));
                Assert.AreEqual(id, e.id); Assert.AreEqual(items.Get(id).name, e.name); Assert.AreEqual("Dropped by Caddock, the Bandit King", e.source); Assert.AreEqual(items.Get(id).quality, e.quality);
            }
            Assert.IsEmpty(newItems, "Known is not found."); Assert.IsEmpty(p.armoury);
            Assert.AreEqual(ArmouryLog.State.Unknown, log.StateOf("loot.oak.whitefoot_mantle"), "Another boss's pieces stay unknown.");
            Assert.AreEqual(ArmouryLog.State.Unknown, log.StateOf("loot.world.golden_cask_tankard"), "A world drop is never named by a kill.");
            Assert.IsFalse(p.lootLuck.Any(l => l.source.StartsWith("drop.world.", StringComparison.Ordinal)), "World lists count no kills.");
            log.Killed(caddock); Assert.AreEqual(2, log.KillsOf("drop.oak.caddock")); Assert.AreEqual(1, p.lootLuck.Count(l => l.source == "drop.oak.caddock"), "One counter.");
            // A camp's normal mobs name the pieces of the camp tables they roll.
            var z = zones.First(x => x.id == "zone.oakhaven");
            LootContext Normal(int index) { return LootContext.From("mob." + LootContext.CampTag(z.camps[index]) + ".oakhaven." + index + ".1", z.camps, z.camps[index].levelMax, false); }
            var (camp, at) = (from g in loot.GearOrder where Kind(g) == "mob" && g.source.EndsWith("@oakhaven", StringComparison.Ordinal)
                              from i in Enumerable.Range(0, z.camps.Length) where z.camps[i] != null && !z.camps[i].elite
                              where loot.Drops.Any(d => !string.IsNullOrEmpty(d.tag) && LootDatabase.Matches(d, Normal(i)) && d.groups.Any(gr => gr.pick.Any(k => k.item == g.id)))
                              select (g, i)).First();
            var fresh = Log(out _, out _);
            Assert.AreEqual(ArmouryLog.State.Unknown, fresh.StateOf(camp.id));
            fresh.Killed(Normal(at));
            Assert.AreEqual(ArmouryLog.State.Known, fresh.StateOf(camp.id), camp.id + " is named once a " + z.camps[at].mob + " has died.");
            // Every boss and camp piece is on a list that a kill can name it by.
            var all = Log(out _, out _);
            foreach (var d in loot.Drops) if (!string.IsNullOrEmpty(d.zone) || !string.IsNullOrEmpty(d.tag) || !string.IsNullOrEmpty(d.mob)) all.Progress.lootLuck.Add(new LootLuck { source = d.id, kills = 1 });
            foreach (var g in loot.GearOrder) if (Kind(g) == "boss" || Kind(g) == "mob") Assert.AreEqual(ArmouryLog.State.Known, all.StateOf(g.id), g.id);
            // A quest reward is named once the quest is taken.
            Assert.AreEqual(ArmouryLog.State.Unknown, log.StateOf(Blade));
            p.quests.Add(new QuestState { id = "main.oakhaven.1" });
            Assert.AreEqual(ArmouryLog.State.Known, log.StateOf(Blade)); Assert.AreEqual("A quest reward", log.EntryOf(loot.Meta(Blade)).source);
        }

        [Test] public void Hidden_find_items_never_name_their_place()
        {
            var log = Log(out _, out _); var p = log.Progress;
            // Every list killed and every quest done: still nothing names a hidden find's piece or a world drop.
            foreach (var d in loot.Drops) p.lootLuck.Add(new LootLuck { source = d.id, kills = 5 });
            foreach (var g in loot.GearOrder) if (Kind(g) == "quest") p.questsDone.Add(g.source.Substring(6));
            var hidden = loot.GearOrder.Where(g => Kind(g) == "secret").ToList(); Assert.AreEqual(7, hidden.Count, "Seven pieces lie in hidden finds.");
            foreach (var g in hidden.Concat(loot.GearOrder.Where(g => Kind(g) == "world")))
            {
                var e = log.EntryOf(g);
                Assert.AreEqual(ArmouryLog.State.Unknown, e.state, g.id); Assert.IsNull(e.id); Assert.IsNull(e.name); Assert.IsNull(e.source);
            }
            foreach (var g in hidden)
            {
                string secretId = g.source.Substring("secret:".Length); var s = secrets.First(x => x.id == secretId);
                Assert.AreEqual("Hidden somewhere", ArmouryLog.UnknownLine("secret"));
                Assert.AreEqual(secretId.Split('.')[1], ArmouryLog.ZoneOf(g, loot), "Listed under its zone, no closer.");
                // Found: its name shows, and its source still says only that it was hidden.
                Inventory.Add(p, items, g.id, 1); log.Sweep();
                var e = log.EntryOf(g);
                Assert.AreEqual(ArmouryLog.State.Found, e.state); Assert.AreEqual("Hidden somewhere", e.source);
                foreach (var text in new[] { e.name, e.source, loot.TooltipLines(g.id, p) })
                {
                    StringAssert.DoesNotContain(secretId, text); StringAssert.DoesNotContain(s.name, text, g.id + " never names " + s.name + ".");
                }
                Inventory.Destroy(p, p.bag.FindIndex(x => x.item == g.id));
            }
        }

        [Test] public void Found_secrets_and_worn_gear_count_on_first_bind()
        {
            var p = EncounterSession.FreshProgress();
            p.equipment[(int)EquipSlot.MainHand] = new ItemStack { item = Blade, count = 1 };
            Inventory.Add(p, items, Mantle, 1); Inventory.Add(p, items, ItemDatabase.GearId("feet", 3, 2, 12), 1); Inventory.Add(p, items, "junk.wolf_pelt", 1);
            p.discoveries.Add("secret.oakhaven.poachers-camp");              // its piece (the Poacher's hood) has a secret source
            p.discoveries.Add("secret.khaven.bell-tower");                   // a find this content does not have: nothing
            var test = new ZoneSecret { id = "secret.test.stash", item = Coat }; // a find whose item is a named piece with another source
            p.discoveries.Add(test.id);
            p.armoury = null; p.looks = null; p.lootLuck = null;            // as JsonUtility might leave them
            var said = new List<string>();
            var log = new ArmouryLog(p, items, loot, looks, secrets.Concat(new[] { test }));
            log.NewItem = said.Add; log.NewLook = said.Add;
            Assert.NotNull(p.lootLuck); Assert.IsEmpty(p.lootLuck);
            CollectionAssert.AreEquivalent(new[] { Mantle, Blade, "item.poachers_hood", Coat }, p.armoury, "Held, worn, and what finds already found gave.");
            Assert.AreEqual(3, p.looks.Count, "The looks of what is held: the mantle, the blade and the generated boots (not the hood or the coat, which are not here).");
            CollectionAssert.AreEquivalent(new[] { Mantle, Blade, ItemDatabase.GearId("feet", 3, 2, 12) }.Select(id => log.LookOf(items.Get(id))), p.looks);
            // Quietly, and only once.
            log.Bind(p); Assert.IsEmpty(said, "Binding raises nothing."); Assert.AreEqual(4, p.armoury.Count); Assert.AreEqual(3, p.looks.Count);
            Assert.AreEqual(0, log.Sweep()); Assert.IsEmpty(said);
            // Without the zones' secrets the coat would not count; the hood still does (its source names the find).
            var q = EncounterSession.FreshProgress(); q.discoveries.Add("secret.oakhaven.poachers-camp"); q.discoveries.Add(test.id);
            new ArmouryLog(q, items, loot, looks); CollectionAssert.AreEqual(new[] { "item.poachers_hood" }, q.armoury);
            // Without the named loot nothing is an entry, and looks are still seen.
            var r = EncounterSession.FreshProgress(); Inventory.Add(r, items, Mantle, 1);
            var bare = new ArmouryLog(r, items, null, looks); Assert.IsEmpty(r.armoury); Assert.AreEqual(1, r.looks.Count);
            Assert.AreEqual(ArmouryLog.State.Unknown, bare.StateOf(Mantle)); Assert.AreEqual(0, bare.TallyOf("oakhaven").Total);
        }

        [Test] public void The_tenth_dry_Caddock_kill_gives_the_sabre_and_the_count_is_saved()
        {
            var log = Log(out _, out _); var p = log.Progress; var caddock = Mob("oakhaven", "Caddock, the Bandit King");
            Func<string, bool> has = id => Inventory.Has(p, id), owned = id => log.IsFound(id) || has(id);
            // Nine kills without the sabre (seeds where it does not drop), counted in the character's own list.
            int seed = 0, dry = 0;
            while (dry < 9)
            {
                var copy = p.lootLuck.Select(l => new LootLuck { source = l.source, kills = l.kills, dry = l.dry }).ToList();
                if (loot.Roll(caddock, items, owned, 0, copy, new System.Random(seed)).Any(d => d.item == Sabre)) { seed++; continue; }
                loot.Roll(caddock, items, owned, 0, p.lootLuck, new System.Random(seed++)); dry++;
                Assert.Less(seed, 100000);
            }
            var counter = p.lootLuck.Single(l => l.source == "drop.oak.caddock#1"); Assert.AreEqual(9, counter.kills); Assert.AreEqual(9, counter.dry);
            // Saved and read back, the count holds, and the tenth kill gives the sabre whatever the dice say.
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-armoury-" + Guid.NewGuid().ToString("N"));
            try
            {
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                var c = loaded.lootLuck.Single(l => l.source == "drop.oak.caddock#1"); Assert.AreEqual(9, c.kills); Assert.AreEqual(9, c.dry);
                for (int s = 0; s < 50; s++)
                {
                    var copy = loaded.lootLuck.Select(l => new LootLuck { source = l.source, kills = l.kills, dry = l.dry }).ToList();
                    Assert.IsTrue(loot.Roll(caddock, items, null, 0, copy, new System.Random(s)).Any(d => d.item == Sabre), "Seed " + s + ": the tenth dry kill gives it.");
                }
                loot.Roll(caddock, items, null, 0, loaded.lootLuck, new System.Random(1));
                Assert.AreEqual(10, c.kills); Assert.AreEqual(0, c.dry, "The run starts again.");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
            // Found once (even if sold since), the sabre is owned: no more pity, and a quarter of the chance.
            Inventory.Add(p, items, Sabre, 1); log.Sweep(); Inventory.Destroy(p, p.bag.FindIndex(x => x.item == Sabre));
            Assert.IsFalse(has(Sabre)); Assert.IsTrue(owned(Sabre));
            for (int s = 0; s < 30; s++) loot.Roll(caddock, items, owned, 0, p.lootLuck, new System.Random(s));
            Assert.AreEqual(9, counter.kills, "An epic you have found keeps no pity count.");
        }
    }
}
