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
    /// <summary>Saves as the game wrote them, for migration tests.</summary>
    public static class SaveFixtures
    {
        /// <summary>
        /// A format-6 payload as the game wrote it: the keys and shapes of a real v6 save, in its order (the values are this test's
        /// own). 24 bag slots, 9 equipment slots, story enemies, quests, standing, a Chronicle page, a quest item, an emptied crate.
        /// </summary>
        public static readonly string V6Payload =
            "{\"classId\":\"class.warrior\",\"zoneId\":\"zone.oakhaven\",\"talents\":[{\"id\":\"tk-tempered-armor\",\"rank\":1}]," +
            "\"playerId\":\"5d0c1f3e8b2a4c6d9e7f0a1b2c3d4e5f\",\"companionId\":\"a1b2c3d4e5f60718293a4b5c6d7e8f90\"," +
            "\"experience\":1329,\"health\":255,\"mana\":118,\"companionHealth\":130,\"x\":14.25,\"y\":2.5,\"z\":-31.75," +
            "\"recruited\":true,\"relationship\":8,\"gold\":57,\"equippedItem\":\"\",\"inventory\":[]," +
            "\"bag\":[{\"item\":\"junk.wolf_pelt\",\"count\":3},{\"item\":\"potion.minor\",\"count\":2}," + Slots(22) + "]," +
            "\"equipment\":[" + Slots(7) + ",{\"item\":\"item.training_blade\",\"count\":1}," + Slots(1) + "]," +
            "\"enemies\":[{\"id\":\"oakhaven.collector.0\",\"dead\":true,\"looted\":true},{\"id\":\"oakhaven.warden.0\",\"dead\":false,\"looted\":false}]," +
            "\"quests\":[{\"id\":\"main.oakhaven.1\",\"step\":1,\"counts\":[4],\"tracked\":true},{\"id\":\"npc.lisbet.yarrow\",\"step\":0,\"counts\":[0],\"tracked\":false}]," +
            "\"questsDone\":[\"npc.goody.eggs\"],\"reputation\":[{\"faction\":\"oakhaven\",\"value\":100}],\"documents\":[\"doc.oakhaven.ledger\"]," +
            "\"questItems\":[\"item.yarrow\"],\"usedInteractables\":[\"zone.oakhaven|Tithe crate|55|45\"]}";
        static string Slots(int n) { var s = new string[n]; for (int i = 0; i < n; i++) s[i] = "{\"item\":\"\",\"count\":0}"; return string.Join(",", s); }
        /// <summary>A format-7 payload as the game wrote it: the format-6 one with its list of discoveries, two of them found.</summary>
        public static readonly string V7Payload = V6Payload.Substring(0, V6Payload.Length - 1) + ",\"discoveries\":[\"secret.oakhaven.mill-cache\",\"secret.khaven.bell-tower\"]}";
    }

    /// <summary>
    /// Discoveries: the save keeps them (format 7; a real v6 save migrates with nothing lost), and the rules of finding: pays once,
    /// a chest waits for its key, full bags keep the find for later, the book counts every secret but names only the found, and
    /// the zones' secrets are sound. Phase 8.6 (step 8): the five new kinds and their rules, a rare herb that grows back a game day
    /// after it is picked (picked with Herbalism at its level), a wayshrine that takes a crown for its blessing, and the count by
    /// region (decision 6).
    /// </summary>
    public class DiscoveryLogTests
    {
        static string Temp() { return Path.Combine(Path.GetTempPath(), "Crulanda-discoveries-" + System.Guid.NewGuid().ToString("N")); }
        static List<string> Texts(string folder)
        {
            var texts = new List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", folder), "*.json")) texts.Add(File.ReadAllText(f));
            return texts;
        }
        static ItemDatabase Items() { return ItemDatabase.Parse(Texts("Items")); }
        static QuestDatabase Quests() { return QuestDatabase.Parse(Texts("Quests")); }
        static DiscoveryLog Fresh(out List<string> said, out List<ZoneSecret> toasts)
        {
            var log = new DiscoveryLog(EncounterSession.FreshProgress(), Items(), Quests());
            var lines = new List<string>(); var found = new List<ZoneSecret>();
            log.Say = lines.Add; log.Found = found.Add; said = lines; toasts = found;
            return log;
        }
        static ZoneSecret Secret(string slug, string kind, int xp = 0, int gold = 0, string item = null, string document = null, string needs = null)
        {
            return new ZoneSecret { id = "secret.oakhaven." + slug, name = "The " + slug, kind = kind, prompt = "Search", text = "Something was left here.", xp = xp, gold = gold,
                item = item, document = document, needs = needs, canonStatus = "GAME-ONLY (test)" };
        }

        // ---------- the save ----------
        [Test] public void A_real_v6_save_loads_as_the_current_format_with_nothing_lost()
        {
            string root = Temp();
            try
            {
                var store = new SaveFileStore(root);
                store.Write("encounter", new SaveEnvelope { formatVersion = 6, payloadType = "CrulandaEncounter", gameVersion = "0.3.0", savedAtUtc = "2026-09-29T19:31:00Z", payloadJson = SaveFixtures.V6Payload });
                string onDisk = File.ReadAllText(store.PathFor("encounter"));
                var save = new EncounterSave(root, TestTalents.Warrior());
                Assert.IsTrue(save.Read(out var p, out var message), message);
                Assert.AreEqual(onDisk, File.ReadAllText(store.PathFor("encounter")), "Reading migrates in memory: the file is untouched until the next save.");
                // Everything format 6 held, as it was.
                Assert.AreEqual("class.warrior", p.classId); Assert.AreEqual("zone.oakhaven", p.zoneId);
                Assert.AreEqual(1, p.talents.Count); Assert.AreEqual("tk-tempered-armor", p.talents[0].id); Assert.AreEqual(1, p.talents[0].rank);
                Assert.AreEqual("5d0c1f3e8b2a4c6d9e7f0a1b2c3d4e5f", p.playerId); Assert.AreEqual("a1b2c3d4e5f60718293a4b5c6d7e8f90", p.companionId);
                Assert.AreEqual(2599, p.experience, "Format 6 keeps its level under the cap-15 curve (round 29: 1329 -> 2599, level 4)."); Assert.AreEqual(4, p.Level);
                Assert.AreEqual(255, p.health); Assert.AreEqual(118, p.mana); Assert.AreEqual(130, p.companionHealth);
                Assert.AreEqual(14.25f, p.x); Assert.AreEqual(2.5f, p.y); Assert.AreEqual(-31.75f, p.z);
                Assert.IsTrue(p.recruited); Assert.AreEqual(8, p.relationship); Assert.AreEqual(57, p.gold);
                Assert.IsTrue(string.IsNullOrEmpty(p.equippedItem)); Assert.IsEmpty(p.inventory);
                Assert.AreEqual(Inventory.BagSize, p.bag.Count);
                Assert.AreEqual("junk.wolf_pelt", p.bag[0].item); Assert.AreEqual(3, p.bag[0].count);
                Assert.AreEqual("potion.minor", p.bag[1].item); Assert.AreEqual(2, p.bag[1].count);
                for (int i = 2; i < p.bag.Count; i++) Assert.IsTrue(p.bag[i].Empty, "bag slot " + i);
                Assert.AreEqual(ItemDatabase.SlotIds.Length, p.equipment.Count);
                Assert.AreEqual("item.training_blade", p.equipment[(int)EquipSlot.MainHand].item);
                for (int i = 0; i < p.equipment.Count; i++) if (i != (int)EquipSlot.MainHand) Assert.IsTrue(p.equipment[i].Empty, "equipment slot " + i);
                Assert.AreEqual(2, p.enemies.Count);
                Assert.IsTrue(p.enemies[0].id == "oakhaven.collector.0" && p.enemies[0].dead && p.enemies[0].looted);
                Assert.IsTrue(p.enemies[1].id == "oakhaven.warden.0" && !p.enemies[1].dead && !p.enemies[1].looted);
                Assert.AreEqual(2, p.quests.Count);
                Assert.AreEqual("main.oakhaven.1", p.quests[0].id); Assert.AreEqual(1, p.quests[0].step); CollectionAssert.AreEqual(new[] { 4 }, p.quests[0].counts); Assert.IsTrue(p.quests[0].tracked);
                Assert.AreEqual("npc.lisbet.yarrow", p.quests[1].id); Assert.AreEqual(0, p.quests[1].step); CollectionAssert.AreEqual(new[] { 0 }, p.quests[1].counts); Assert.IsFalse(p.quests[1].tracked);
                CollectionAssert.AreEqual(new[] { "npc.goody.eggs" }, p.questsDone);
                Assert.AreEqual(1, p.reputation.Count); Assert.AreEqual("oakhaven", p.reputation[0].faction); Assert.AreEqual(100, p.reputation[0].value);
                CollectionAssert.AreEqual(new[] { "doc.oakhaven.ledger" }, p.documents);
                CollectionAssert.AreEqual(new[] { "item.yarrow" }, p.questItems);
                CollectionAssert.AreEqual(new[] { "zone.oakhaven|Tithe crate|55|45" }, p.usedInteractables);
                // And the new lists (format 7's discoveries, format 8's trades, format 9's Armoury), empty.
                Assert.NotNull(p.discoveries); Assert.IsEmpty(p.discoveries);
                Assert.NotNull(p.professions); Assert.IsEmpty(p.professions); Assert.NotNull(p.pouches); Assert.IsEmpty(p.pouches);
                Assert.IsEmpty(p.armoury); Assert.IsEmpty(p.looks); Assert.IsEmpty(p.lootLuck);
                // The next save is the current format, and reads back the same.
                save.Write(p);
                Assert.IsTrue(store.TryRead("encounter", out var envelope, out _));
                Assert.AreEqual(EncounterSave.FormatVersion, envelope.formatVersion); StringAssert.EndsWith(",\"discoveries\":[],\"professions\":[],\"pouches\":[],\"armoury\":[],\"looks\":[],\"lootLuck\":[]}", envelope.payloadJson);
                Assert.IsTrue(save.Read(out var again, out message), message);
                Assert.AreEqual(JsonUtility.ToJson(p), JsonUtility.ToJson(again), "A round trip in the current format changes nothing.");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void Discoveries_survive_a_save_round_trip()
        {
            string root = Temp();
            try
            {
                var p = EncounterSession.FreshProgress();
                p.discoveries.Add("secret.oakhaven.mill-cache"); p.discoveries.Add("secret.khaven.bell-tower"); p.discoveries.Add("");
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(new SaveFileStore(root).TryRead("encounter", out var envelope, out _));
                Assert.AreEqual(EncounterSave.FormatVersion, envelope.formatVersion); Assert.GreaterOrEqual(EncounterSave.FormatVersion, 7);
                StringAssert.Contains("\"discoveries\":[\"secret.oakhaven.mill-cache\",\"secret.khaven.bell-tower\",\"\"]", envelope.payloadJson);
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                CollectionAssert.AreEqual(new[] { "secret.oakhaven.mill-cache", "secret.khaven.bell-tower" }, loaded.discoveries, "Kept in order; a blank id is no find and is dropped.");
                var log = new DiscoveryLog(loaded, null, null);
                Assert.IsTrue(log.IsFound(new ZoneSecret { id = "secret.khaven.bell-tower" }));
                Assert.AreEqual(DiscoveryLog.Result.AlreadyFound, log.Discover(new ZoneSecret { id = "secret.oakhaven.mill-cache", xp = 50 }), "Found before the reload: never again.");
                Assert.AreEqual(0, loaded.experience);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void Saves_older_than_format_6_load_with_no_discoveries()
        {
            string root = Temp();
            try
            {
                var p = EncounterSession.FreshProgress(); p.gold = 12;
                string json = JsonUtility.ToJson(p).Replace(",\"discoveries\":[]", "");
                Assert.IsFalse(json.Contains("discoveries"), "A format-5 payload has no list.");
                new SaveFileStore(root).Write("encounter", new SaveEnvelope { formatVersion = 5, payloadType = "CrulandaEncounter", payloadJson = json });
                Assert.IsTrue(new EncounterSave(root, TestTalents.Warrior()).Read(out var loaded, out var message), message);
                Assert.NotNull(loaded.discoveries); Assert.IsEmpty(loaded.discoveries); Assert.AreEqual(12, loaded.gold);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        // ---------- finding ----------
        [Test] public void A_find_pays_its_xp_gold_item_and_page_once()
        {
            var log = Fresh(out var said, out var toasts); var p = log.Progress;
            var cache = Secret("mill-cache", "cache", xp: 40, gold: 12, item: "potion.minor", document: "doc.oakhaven.lullaby");
            Assert.AreEqual(DiscoveryLog.Result.Found, log.Discover(cache));
            Assert.AreEqual(40, p.experience); Assert.AreEqual(12, p.gold);
            Assert.AreEqual(1, Inventory.Count(p, "potion.minor")); Assert.Contains("doc.oakhaven.lullaby", p.documents);
            CollectionAssert.AreEqual(new[] { cache.id }, p.discoveries);
            Assert.AreEqual(1, toasts.Count); Assert.AreSame(cache, toasts[0]);
            Assert.AreEqual("Discovered: The mill-cache. Something was left here.  +40 XP  +12 crowns  +" + log.Items.Get("potion.minor").name, said[0]);
            StringAssert.StartsWith("New page in your Chronicle: ", said[1]);
            Assert.AreEqual(DiscoveryLog.Result.AlreadyFound, log.Discover(cache), "Once only.");
            Assert.AreEqual(40, p.experience); Assert.AreEqual(12, p.gold); Assert.AreEqual(1, Inventory.Count(p, "potion.minor")); Assert.AreEqual(1, toasts.Count);
            // A lookout pays in experience alone; a find with no rewards still counts.
            Assert.AreEqual(DiscoveryLog.Result.Found, log.Discover(new ZoneSecret { id = "secret.oakhaven.tor", kind = "vista", name = "The Tor", xp = 25 }));
            Assert.AreEqual(65, p.experience); Assert.AreEqual("Discovered: The Tor.  +25 XP", said[said.Count - 1]);
            Assert.IsTrue(DiscoveryLog.IsVista(new ZoneSecret { kind = "vista" })); Assert.IsFalse(DiscoveryLog.IsVista(cache));
            Assert.IsTrue(DiscoveryLog.Pocketed(Secret("page", "note"))); Assert.IsFalse(DiscoveryLog.Pocketed(cache));
        }

        [Test] public void A_chest_waits_for_its_key()
        {
            var log = Fresh(out _, out var toasts); var p = log.Progress;
            var key = Secret("stump-key", "key", xp: 10);
            var chest = Secret("iron-chest", "chest", xp: 60, gold: 25, needs: key.id);
            Assert.AreEqual(DiscoveryLog.Result.Locked, log.Discover(chest));
            Assert.AreEqual("Locked. The key must be somewhere near.", DiscoveryLog.ShutLine(chest));
            Assert.IsFalse(log.IsFound(chest)); Assert.AreEqual(0, p.experience); Assert.AreEqual(0, p.gold); Assert.IsEmpty(toasts, "A locked chest is no discovery.");
            Assert.AreEqual(DiscoveryLog.Result.Found, log.Discover(key));
            Assert.IsTrue(log.Unlocked(chest));
            Assert.AreEqual(DiscoveryLog.Result.Found, log.Discover(chest));
            Assert.AreEqual(70, p.experience); Assert.AreEqual(25, p.gold); Assert.AreEqual(2, toasts.Count);
        }

        [Test] public void Full_bags_leave_the_find_where_it_is_until_there_is_room()
        {
            var log = Fresh(out _, out var toasts); var p = log.Progress;
            for (int i = 0; i < Inventory.BagSize; i++) Inventory.Add(p, log.Items, ItemDatabase.GearId("feet", 2, 1, i), 1);
            Assert.AreEqual(0, Inventory.FreeSlots(p)); Assert.AreEqual(0, Inventory.Room(p, log.Items, "potion.minor"));
            var cache = Secret("hollow-oak", "cache", xp: 30, gold: 5, item: "potion.minor");
            Assert.AreEqual(DiscoveryLog.Result.BagsFull, log.Discover(cache));
            Assert.IsFalse(log.IsFound(cache)); Assert.AreEqual(0, p.experience); Assert.AreEqual(0, p.gold); Assert.IsEmpty(toasts, "Nothing is taken.");
            Inventory.Destroy(p, 3);
            Assert.AreEqual(DiscoveryLog.Result.Found, log.Discover(cache));
            Assert.AreEqual(1, Inventory.Count(p, "potion.minor")); Assert.AreEqual(30, p.experience);
        }

        [Test] public void The_book_counts_every_secret_but_names_only_the_found()
        {
            var log = Fresh(out _, out _);
            var a = Secret("a", "vista"); var b = Secret("b", "cache"); var c = Secret("c", "herb");
            log.Discover(b);
            var t = log.TallyOf("zone.oakhaven", "Oakhaven", new[] { a, b, c, b, null, new ZoneSecret { id = "" } }, true);
            Assert.AreEqual(3, t.total, "Each secret once; blanks don't count."); Assert.AreEqual(2, t.Hidden);
            CollectionAssert.AreEqual(new[] { b }, t.found, "Only the found are listed.");
            Assert.IsTrue(t.here); Assert.AreEqual("Oakhaven", t.zoneName);
            var none = log.TallyOf("zone.khaven", "Khaven Village", null);
            Assert.AreEqual(0, none.total); Assert.IsEmpty(none.found);
        }

        // ---------- content ----------
        [Test] public void The_zones_secrets_are_sound_and_bad_ones_are_caught()
        {
            var zones = new List<ZoneDefinition>();
            foreach (var json in Texts("Zones")) zones.Add(JsonUtility.FromJson<ZoneDefinition>(json));
            var problems = DiscoveryLog.Validate(zones, Items(), Quests());
            Assert.IsEmpty(problems, string.Join("\n", problems));

            var bad = new ZoneDefinition { id = "zone.test", landmarks = new[] { new ZoneLabel { name = "The Old Well" } } };
            bad.secrets = new[] {
                new ZoneSecret { id = "secret.test.well", name = "The Old Well", kind = "cache", prompt = "Search", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.well", name = "Twice", kind = "cache", prompt = "Search", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.elsewhere.x", name = "Misfiled", kind = "urn", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.chest", name = "Chest", kind = "chest", prompt = "Open", needs = "secret.test.nowhere", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.key", name = "Key", kind = "key", prompt = "Search", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.loop-a", name = "A", kind = "cache", prompt = "Search", needs = "secret.test.loop-b", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.loop-b", name = "B", kind = "cache", prompt = "Search", needs = "secret.test.loop-a", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.view", name = "View", kind = "vista", radius = 0, item = "no.such.item", document = "no.such.page" },
                new ZoneSecret { id = "secret.test.told", name = "Told", kind = "cache", prompt = "Search", canonStatus = "GAME-ONLY", clue = "Look behind the old well.", hint = "Jory says it's by the OLD WELL." },   // 2026-10-15: a clue or hint that names a landmark
            };
            string all = string.Join("\n", DiscoveryLog.Validate(new[] { bad }, Items(), Quests()));
            foreach (var expected in new[] { "Duplicate secret id 'secret.test.well'", "its id should be secret.test.<slug>", "unknown kind 'urn'", "no prompt for E",
                "needs unknown secret 'secret.test.nowhere'", "a key that no chest needs", "go round in a circle", "a lookout needs a radius", "no canonStatus",
                "unknown item 'no.such.item'", "unknown Chronicle page 'no.such.page'", "a landmark has its name", "Secret 'secret.test.told': its clue or hint names the landmark 'The Old Well'" })
                StringAssert.Contains(expected, all);
        }

        // ---------- Phase 8.6: the new kinds, the herbs that grow back, the wayshrines, the count by region ----------
        [Test] public void The_new_kinds_are_known_and_their_rules_checked()
        {
            foreach (var k in new[] { "carving", "read", "grave", "remains", "shrine" }) CollectionAssert.Contains(DiscoveryLog.Kinds, k);
            Assert.IsTrue(DiscoveryLog.Pocketed(Secret("bones", "remains")), "The satchel is taken; the bones stay.");
            foreach (var k in new[] { "carving", "read", "grave", "shrine" }) Assert.IsFalse(DiscoveryLog.Pocketed(Secret(k, k)), k + " stays where it is.");
            Assert.IsTrue(DiscoveryLog.Lingers(new ZoneSecret { kind = "herb", item = "herb.moonbell" })); Assert.IsTrue(DiscoveryLog.Lingers(Secret("s", "shrine")));
            Assert.IsFalse(DiscoveryLog.Lingers(Secret("c", "cache"))); Assert.IsFalse(DiscoveryLog.Lingers(Secret("h", "herb")), "A herb with no item has nothing to grow back.");
            var bad = new ZoneDefinition { id = "zone.test" };
            bad.secrets = new[] {
                new ZoneSecret { id = "secret.test.book", name = "Book", kind = "read", prompt = "Read", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.bones", name = "Bones", kind = "remains", prompt = "Search", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.mark", name = "Mark", kind = "carving", prompt = "Look", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.stone", name = "Stone", kind = "grave", prompt = "Read", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.shrine", name = "Shrine", kind = "shrine", prompt = "Kneel", item = "potion.minor", canonStatus = "GAME-ONLY" },
                new ZoneSecret { id = "secret.test.bells", name = "Bells", kind = "herb", prompt = "Pick", canonStatus = "GAME-ONLY" },
            };
            string all = string.Join("\n", DiscoveryLog.Validate(new[] { bad }));
            foreach (var expected in new[] { "'secret.test.book': a read with no page", "'secret.test.bones': a remains with no page", "'secret.test.mark': a carving with nothing written",
                "'secret.test.stone': a grave with nothing written", "'secret.test.shrine': a shrine gives its blessing", "'secret.test.bells': a herb with no item" })
                StringAssert.Contains(expected, all);
            StringAssert.DoesNotContain("unknown kind", all);
        }

        [Test] public void A_found_rare_herb_grows_back_a_day_after_it_is_picked()
        {
            var log = Fresh(out _, out _); var p = log.Progress; p.days = 4;
            var bells = Secret("bells", "herb", xp: 10, item: "herb.moonbell");
            Assert.IsFalse(log.Regrown(bells), "Not found yet: nothing to grow back.");
            Assert.AreEqual(DiscoveryLog.Result.Found, log.Discover(bells));
            Assert.AreEqual(1, Inventory.Count(p, "herb.moonbell"));
            Assert.IsFalse(log.Regrown(bells), "Picked today.");
            p.days = 5; Assert.IsTrue(log.Regrown(bells), "The clock past six in the morning: it has grown back.");
            log.Picked(bells); Assert.IsFalse(log.Regrown(bells), "Picked again.");
            p.days = 6; Assert.IsTrue(log.Regrown(bells));
            log.Bind(p); Assert.IsFalse(log.Regrown(bells), "After a load it is counted as picked that day.");
            p.days = 7; Assert.IsTrue(log.Regrown(bells));
            // Picked with Herbalism, at the herb's level (no node of the trades' for it: one is made).
            var items = Items(); var node = DiscoveryLog.HerbNode(bells, null, items);
            Assert.AreEqual("herbalism", node.profession); Assert.AreEqual("herb.moonbell", node.item); Assert.AreEqual(10, node.skill); Assert.AreEqual(1, node.max);
            Assert.AreEqual(75, DiscoveryLog.HerbNode(Secret("moss", "herb", item: "herb.lantern_moss"), null, items).skill, "Lantern-moss (level 14) asks Herbalism 75.");
            var trades = ProfessionDatabase.Parse(Texts("Professions").Concat(new[] { "{\"nodes\":[{\"id\":\"node.test_moonbell\",\"name\":\"Moonbell\",\"profession\":\"herbalism\",\"skill\":25,\"item\":\"herb.moonbell\",\"min\":1,\"max\":2}]}" }), items);
            var own = DiscoveryLog.HerbNode(bells, trades, items);
            Assert.AreEqual("herb.moonbell", own.item); Assert.AreNotEqual("node.rare.bells", own.id, "The trades' own node for it, when the content has one.");
            Assert.IsNull(DiscoveryLog.HerbNode(Secret("bare", "herb"), null, items), "No item, no node.");
        }

        [Test] public void A_wayshrine_blesses_for_a_crown_once_found()
        {
            var log = Fresh(out _, out _); var p = log.Progress; p.gold = 0;
            var shrine = Secret("shrine", "shrine", xp: 20);
            Assert.IsFalse(log.Offer(shrine, out _), "Not found yet.");
            log.Discover(shrine);
            Assert.IsFalse(log.Offer(shrine, out var why)); Assert.AreEqual(DiscoveryLog.NoCrownLine, why);
            p.gold = 3; Assert.IsTrue(log.Offer(shrine, out _)); Assert.AreEqual(3 - DiscoveryLog.Offering, p.gold, "One crown in the dish.");
            Assert.IsFalse(log.Offer(Secret("cache", "cache"), out _), "Only a shrine takes an offering.");
            var b = DiscoveryLog.Blessing();
            Assert.AreEqual(DiscoveryLog.BlessingId, b.id); Assert.AreEqual(DiscoveryLog.BlessingName, b.name); Assert.AreEqual(600, b.duration);
            Assert.That(b.incomingDamageMultiplier, Is.InRange(.9f, .99f), "Small: a little less from every blow.");
        }

        [Test] public void The_count_by_region_names_the_nearest_great_place()
        {
            var landmarks = new[] {
                new ZoneLabel { name = "The Mastwood", at = new Vector2(0, 100), radius = 18 }, new ZoneLabel { name = "Thornshaw", at = new Vector2(100, 0), radius = 16 },
                new ZoneLabel { name = "The Old Orchard", at = new Vector2(-100, 0), radius = 14 }, new ZoneLabel { name = "A small well", at = new Vector2(0, 92), radius = 6 } };
            ZoneSecret At(string slug, float x, float y) { var s = Secret(slug, "cache"); s.at = new Vector2(x, y); return s; }
            var hidden = new[] { At("a", 2, 95), At("b", -10, 110), At("c", 90, 5), At("d", -95, 2), At("e", 5, 80) };
            var regions = DiscoveryLog.Regions(hidden, landmarks);
            CollectionAssert.AreEqual(new[] { ("The Mastwood", 3), ("The Old Orchard", 1), ("Thornshaw", 1) }, regions, "By the great places only (the well is small), most first.");
            Assert.AreEqual("3 hidden about the Mastwood", DiscoveryLog.RegionLine("The Mastwood", 3)); Assert.AreEqual("1 hidden about Thornshaw", DiscoveryLog.RegionLine("Thornshaw", 1));
            var log = Fresh(out _, out _); log.Discover(hidden[0]); log.Discover(hidden[1]);
            var t = log.TallyOf("zone.oakhaven", "Oakhaven", hidden, true, landmarks);
            Assert.AreEqual(t.Hidden, t.regions.Sum(r => r.count), "The regions count the hidden, and only them.");
            Assert.AreEqual(("The Mastwood", 1), t.regions.Find(r => r.place == "The Mastwood"));
            Assert.IsEmpty(log.TallyOf("zone.oakhaven", "Oakhaven", hidden).regions, "No places given, no regions.");
            var few = DiscoveryLog.Regions(hidden, new[] { landmarks[3], new ZoneLabel { name = "Hut", at = new Vector2(90, 0), radius = 6 } });
            Assert.AreEqual(2, few.Count, "A zone with few great places counts by all its places.");
        }

        [Test] public void Every_zone_has_its_target_of_secrets_with_the_new_kinds_spread_among_them()
        {
            var zones = new List<ZoneDefinition>(); foreach (var json in Texts("Zones")) zones.Add(JsonUtility.FromJson<ZoneDefinition>(json));
            int Count(string id) { return zones.Find(z => z.id == id).secrets.Length; }
            // Decision 7 (2026-10-15): about 90 in all; Oakhaven up to 20 (it had 25 already), Khaven, the Peaks and the Rim 8-14 each, the
            // Shore 6-14, the Sealed Adit's (13 since its rework) kept, the Root-Mother's Deep up to three, Crowsfoot Hollow its two.
            Assert.GreaterOrEqual(Count("zone.oakhaven"), 20);
            foreach (var id in new[] { "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant", "zone.lowtowns" }) Assert.That(Count(id), Is.InRange(8, 14), id);
            int total = zones.Sum(z => z.secrets.Length); Assert.That(total, Is.InRange(99, 115), "About ninety in all, and the Lowtowns' fourteen (Phase 9.1).");
            var shore = zones.Find(z => z.id == "zone.verdant");
            Assert.AreEqual(3, shore.secrets.Count(s => s.cave == "The Root-Mother's Deep"), "Three in the Root-Mother's Deep.");
            var oak = zones.Find(z => z.id == "zone.oakhaven");
            Assert.AreEqual(2, oak.secrets.Count(s => Vector2.Distance(s.at, new Vector2(-75, 246)) < 20), "Crowsfoot Hollow keeps its two (the key and the strongbox).");
            foreach (var k in new[] { "carving", "read", "grave", "remains", "shrine" }) Assert.GreaterOrEqual(zones.Sum(z => z.secrets.Count(s => s.kind == k)), 2, "At least two of kind " + k + ".");
            foreach (var id in new[] { "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant" })
                Assert.GreaterOrEqual(zones.Find(z => z.id == id).secrets.Count(s => System.Array.IndexOf(new[] { "carving", "read", "grave", "remains", "shrine" }, s.kind) >= 0), 3, id + " has three of the new kinds.");
        }
    }
}
