using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;

namespace Crulanda.Tests
{
    public class SaveMigratorTests
    {
        sealed class AppendMigration : ISaveMigration
        {
            readonly string _suffix;
            public AppendMigration(int from, int to, string suffix) { FromVersion = from; ToVersion = to; _suffix = suffix; }
            public int FromVersion { get; private set; }
            public int ToVersion { get; private set; }
            public string Migrate(string payloadJson) { return payloadJson + _suffix; }
        }

        static SaveEnvelope Envelope(int version, string payload)
        {
            return new SaveEnvelope { formatVersion = version, payloadType = "Test", payloadJson = payload };
        }

        [Test]
        public void Current_version_is_returned_unchanged()
        {
            var m = new SaveMigrator();
            var result = m.MigrateToVersion(Envelope(3, "x"), 3);
            Assert.AreEqual(3, result.formatVersion);
            Assert.AreEqual("x", result.payloadJson);
        }

        [Test]
        public void Migrations_are_chained_in_order()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "+a"));
            m.Register(new AppendMigration(2, 3, "+b"));

            var result = m.MigrateToVersion(Envelope(1, "x"), 3);
            Assert.AreEqual(3, result.formatVersion);
            Assert.AreEqual("x+a+b", result.payloadJson);
        }

        [Test]
        public void Input_envelope_is_not_mutated()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "+a"));
            var input = Envelope(1, "x");
            m.MigrateToVersion(input, 2);
            Assert.AreEqual(1, input.formatVersion);
            Assert.AreEqual("x", input.payloadJson);
        }

        [Test]
        public void Missing_step_throws()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "+a"));
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(Envelope(1, "x"), 3));
        }

        [Test]
        public void Newer_save_than_game_throws()
        {
            var m = new SaveMigrator();
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(Envelope(5, "x"), 3));
        }

        [Test]
        public void Overshooting_migration_throws()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 4, "+jump"));
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(Envelope(1, "x"), 3));
        }

        [Test]
        public void Duplicate_or_non_increasing_registrations_are_rejected()
        {
            var m = new SaveMigrator();
            m.Register(new AppendMigration(1, 2, "a"));
            Assert.Throws<System.ArgumentException>(() => m.Register(new AppendMigration(1, 3, "b")));
            Assert.Throws<System.ArgumentException>(() => m.Register(new AppendMigration(4, 4, "c")));
        }

        [Test]
        public void Encounter_format_6_to_7_adds_an_empty_discoveries_list_and_keeps_everything_else()
        {
            var step = new AddDiscoveriesMigration();
            Assert.AreEqual(6, step.FromVersion); Assert.AreEqual(7, step.ToVersion);
            string v6 = SaveFixtures.V6Payload, v7 = step.Migrate(v6);
            Assert.AreEqual(v6.Substring(0, v6.Length - 1) + ",\"discoveries\":[]}", v7, "Every character of the v6 payload is kept; the list goes in before the closing brace.");
            Assert.AreEqual(v7, step.Migrate(v7), "A payload that has the list is left alone.");
            Assert.AreEqual("{\"discoveries\":[]}", step.Migrate("{}"));
            Assert.AreEqual("{ \"a\" : 1,\"discoveries\":[]}", step.Migrate("  { \"a\" : 1 }\n"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate("[1,2]"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate(""));

            var m = new SaveMigrator(); m.Register(step);
            var input = new SaveEnvelope { formatVersion = 6, payloadType = "CrulandaEncounter", payloadJson = v6 };
            var result = m.MigrateToVersion(input, 7);
            Assert.AreEqual(7, result.formatVersion); Assert.AreEqual(v7, result.payloadJson);
            Assert.AreEqual(6, input.formatVersion); Assert.AreEqual(v6, input.payloadJson);
            Assert.Throws<SaveMigrationException>(() => m.MigrateToVersion(new SaveEnvelope { formatVersion = 5, payloadJson = v6 }, 7), "Formats before 6 are not in this chain (EncounterSave upgrades them in memory).");
        }

        // ---------- format 8: trades (professions, pouches) ----------
        static string Temp() { return Path.Combine(Path.GetTempPath(), "Crulanda-format8-" + System.Guid.NewGuid().ToString("N")); }
        const string Tail8 = ",\"professions\":[],\"pouches\":[]}";

        [Test] public void V7Payload_MigratesTo8_AddsProfessionsAndPouches_KeepsEveryOtherCharacter()
        {
            var step = new AddProfessionsMigration();
            Assert.AreEqual(7, step.FromVersion); Assert.AreEqual(8, step.ToVersion); Assert.AreEqual(8, EncounterSave.FormatVersion);
            string v7 = SaveFixtures.V7Payload, v8 = step.Migrate(v7);
            Assert.AreEqual(v7.Substring(0, v7.Length - 1) + Tail8, v8, "Every character of the v7 payload is kept; the two lists go in before the closing brace.");
            Assert.AreEqual(v8, step.Migrate(v8), "A payload that has both lists is left alone.");
            Assert.AreEqual("{\"professions\":[],\"pouches\":[]}", step.Migrate("{}"));
            Assert.AreEqual("{ \"a\" : 1,\"professions\":[],\"pouches\":[]}", step.Migrate("  { \"a\" : 1 }\n"));
            Assert.AreEqual("{\"professions\" : [{\"id\":\"mining\",\"skill\":3}],\"pouches\":[]}", step.Migrate("{\"professions\" : [{\"id\":\"mining\",\"skill\":3}]}"), "Only the missing list is added.");
            Assert.AreEqual("{\"pouches\":[\"bag.ore_poke\"],\"professions\":[]}", step.Migrate("{\"pouches\":[\"bag.ore_poke\"]}"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate("[1,2]"));
            Assert.Throws<SaveMigrationException>(() => step.Migrate(""));
            Assert.Throws<SaveMigrationException>(() => step.Migrate(null));

            // The whole chain: a format-6 payload gains format 7's list, then format 8's two, and nothing else moves.
            var m = new SaveMigrator(); m.Register(new AddDiscoveriesMigration()); m.Register(step);
            string v6 = SaveFixtures.V6Payload;
            var from6 = m.MigrateToVersion(new SaveEnvelope { formatVersion = 6, payloadType = "CrulandaEncounter", payloadJson = v6 }, EncounterSave.FormatVersion);
            Assert.AreEqual(8, from6.formatVersion); Assert.AreEqual(v6.Substring(0, v6.Length - 1) + ",\"discoveries\":[]" + Tail8, from6.payloadJson);
            var input = new SaveEnvelope { formatVersion = 7, payloadType = "CrulandaEncounter", payloadJson = v7 };
            var from7 = m.MigrateToVersion(input, EncounterSave.FormatVersion);
            Assert.AreEqual(8, from7.formatVersion); Assert.AreEqual(v8, from7.payloadJson);
            Assert.AreEqual(7, input.formatVersion); Assert.AreEqual(v7, input.payloadJson, "The envelope that was read is not touched.");

            // And through the save itself: a format-7 file loads with everything it held, both new lists empty, and the file as it was.
            string root = Temp();
            try
            {
                var store = new SaveFileStore(root);
                store.Write("encounter", new SaveEnvelope { formatVersion = 7, payloadType = "CrulandaEncounter", gameVersion = "0.3.0", savedAtUtc = "2026-10-01T08:15:00Z", payloadJson = v7 });
                string onDisk = File.ReadAllText(store.PathFor("encounter"));
                var save = new EncounterSave(root, TestTalents.Warrior());
                Assert.IsTrue(save.Read(out var p, out var message), message);
                Assert.AreEqual(onDisk, File.ReadAllText(store.PathFor("encounter")), "Reading migrates in memory: the file is untouched until the next save.");
                Assert.IsFalse(File.Exists(store.PathFor("encounter") + ".bak"), "Reading writes nothing.");
                Assert.NotNull(p.professions); Assert.IsEmpty(p.professions); Assert.NotNull(p.pouches); Assert.IsEmpty(p.pouches);
                // Everything format 7 held, as it was.
                Assert.AreEqual("class.warrior", p.classId); Assert.AreEqual("zone.oakhaven", p.zoneId);
                Assert.AreEqual(1, p.talents.Count); Assert.AreEqual("tk-tempered-armor", p.talents[0].id); Assert.AreEqual(1, p.talents[0].rank);
                Assert.AreEqual("5d0c1f3e8b2a4c6d9e7f0a1b2c3d4e5f", p.playerId); Assert.AreEqual("a1b2c3d4e5f60718293a4b5c6d7e8f90", p.companionId);
                Assert.AreEqual(1329, p.experience); Assert.AreEqual(4, p.Level);
                Assert.AreEqual(255, p.health); Assert.AreEqual(118, p.mana); Assert.AreEqual(130, p.companionHealth);
                Assert.AreEqual(14.25f, p.x); Assert.AreEqual(2.5f, p.y); Assert.AreEqual(-31.75f, p.z);
                Assert.IsTrue(p.recruited); Assert.AreEqual(8, p.relationship); Assert.AreEqual(57, p.gold);
                Assert.AreEqual(Inventory.BagSize, p.bag.Count);
                Assert.AreEqual("junk.wolf_pelt", p.bag[0].item); Assert.AreEqual(3, p.bag[0].count);
                Assert.AreEqual("potion.minor", p.bag[1].item); Assert.AreEqual(2, p.bag[1].count);
                for (int i = 2; i < p.bag.Count; i++) Assert.IsTrue(p.bag[i].Empty, "bag slot " + i);
                Assert.AreEqual(ItemDatabase.SlotIds.Length, p.equipment.Count);
                Assert.AreEqual("item.training_blade", p.equipment[(int)EquipSlot.MainHand].item);
                Assert.AreEqual(2, p.enemies.Count); Assert.IsTrue(p.enemies[0].dead && p.enemies[0].looted); Assert.IsFalse(p.enemies[1].dead);
                Assert.AreEqual(2, p.quests.Count); Assert.AreEqual("main.oakhaven.1", p.quests[0].id); Assert.AreEqual(1, p.quests[0].step); CollectionAssert.AreEqual(new[] { 4 }, p.quests[0].counts);
                CollectionAssert.AreEqual(new[] { "npc.goody.eggs" }, p.questsDone);
                Assert.AreEqual(1, p.reputation.Count); Assert.AreEqual(100, p.reputation[0].value);
                CollectionAssert.AreEqual(new[] { "doc.oakhaven.ledger" }, p.documents);
                CollectionAssert.AreEqual(new[] { "item.yarrow" }, p.questItems);
                CollectionAssert.AreEqual(new[] { "zone.oakhaven|Tithe crate|55|45" }, p.usedInteractables);
                CollectionAssert.AreEqual(new[] { "secret.oakhaven.mill-cache", "secret.khaven.bell-tower" }, p.discoveries);
                // The next save is format 8 with the two lists at the end, and reads back the same.
                save.Write(p);
                Assert.IsTrue(store.TryRead("encounter", out var envelope, out _));
                Assert.AreEqual(8, envelope.formatVersion); StringAssert.EndsWith(",\"discoveries\":[\"secret.oakhaven.mill-cache\",\"secret.khaven.bell-tower\"]" + Tail8, envelope.payloadJson);
                Assert.IsTrue(save.Read(out var again, out message), message);
                Assert.AreEqual(JsonUtility.ToJson(p), JsonUtility.ToJson(again));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void V8_RoundTrips()
        {
            string root = Temp();
            try
            {
                var p = EncounterSession.FreshProgress(); p.gold = 31;
                p.professions.Add(new ProfessionSkill { id = "mining", skill = 37 });
                p.professions.Add(new ProfessionSkill { id = "blacksmithing", skill = 12 });
                p.professions.Add(new ProfessionSkill { id = "fletching", skill = 100 });   // a trade this build does not have
                p.professions.Add(new ProfessionSkill { id = "", skill = 9 });
                p.professions.Add(new ProfessionSkill { id = "mining", skill = 80 });       // a second entry for a trade already listed
                p.professions.Add(new ProfessionSkill { id = "herbalism", skill = 0 });
                p.pouches.Add("bag.ore_poke"); p.pouches.Add(""); p.pouches.Add("bag.simples_wallet"); p.pouches.Add("bag.ore_poke"); p.pouches.Add("bag.of_holding");
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                var store = new SaveFileStore(root);
                Assert.IsTrue(store.TryRead("encounter", out var envelope, out _));
                Assert.AreEqual(8, envelope.formatVersion);
                StringAssert.Contains("\"professions\":[{\"id\":\"mining\",\"skill\":37},{\"id\":\"blacksmithing\",\"skill\":12},", envelope.payloadJson);
                StringAssert.EndsWith(",\"pouches\":[\"bag.ore_poke\",\"\",\"bag.simples_wallet\",\"bag.ore_poke\",\"bag.of_holding\"]}", envelope.payloadJson);
                Assert.AreEqual(envelope.payloadJson, new AddProfessionsMigration().Migrate(envelope.payloadJson), "A format-8 payload needs nothing from the step.");
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                Assert.AreEqual(31, loaded.gold);
                Assert.AreEqual(4, loaded.professions.Count, "The blank id and the second mining entry are dropped; the unknown trade is kept.");
                Assert.AreEqual("mining", loaded.professions[0].id); Assert.AreEqual(37, loaded.professions[0].skill);
                Assert.AreEqual("blacksmithing", loaded.professions[1].id); Assert.AreEqual(12, loaded.professions[1].skill);
                Assert.AreEqual("fletching", loaded.professions[2].id); Assert.AreEqual(100, loaded.professions[2].skill);
                Assert.AreEqual("herbalism", loaded.professions[3].id); Assert.AreEqual(0, loaded.professions[3].skill, "0 and 100 are both inside the range a save may hold.");
                CollectionAssert.AreEqual(new[] { "bag.ore_poke", "bag.simples_wallet", "bag.of_holding" }, loaded.pouches, "Kept in order; a blank and a repeat are dropped; an unknown bag is kept.");
                // What was read writes and reads back unchanged.
                save.Write(loaded);
                Assert.IsTrue(save.Read(out var again, out message), message);
                Assert.AreEqual(JsonUtility.ToJson(loaded), JsonUtility.ToJson(again));
                // A character with no trades saves two empty lists.
                var bare = EncounterSession.FreshProgress(); save.Write(bare);
                Assert.IsTrue(store.TryRead("encounter", out envelope, out _)); StringAssert.EndsWith(Tail8, envelope.payloadJson);
                Assert.IsTrue(save.Read(out var none, out message), message); Assert.IsEmpty(none.professions); Assert.IsEmpty(none.pouches);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void SkillOutOfRange_RefusesSave_FileUnchanged()
        {
            foreach (int skill in new[] { 101, -1, 5000 })
            {
                string root = Temp();
                try
                {
                    var p = EncounterSession.FreshProgress();
                    p.professions.Add(new ProfessionSkill { id = "mining", skill = 12 }); p.professions.Add(new ProfessionSkill { id = "woodcutting", skill = skill });
                    var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                    string path = new SaveFileStore(root).PathFor("encounter"); string onDisk = File.ReadAllText(path);
                    Assert.IsFalse(save.Read(out var loaded, out var message), "skill " + skill);
                    Assert.IsNull(loaded); Assert.AreEqual("Invalid profession data.", message);
                    Assert.AreEqual(onDisk, File.ReadAllText(path), "A refused save is left exactly as it was.");
                    Assert.AreEqual(1, Directory.GetFiles(root).Length, "Nothing else is written beside it.");
                }
                finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
            }
        }

        [Test] public void Too_many_pouches_refuses_the_save_file_unchanged()
        {
            string root = Temp();
            try
            {
                var save = new EncounterSave(root, TestTalents.Warrior()); string path = new SaveFileStore(root).PathFor("encounter");
                // Eight worn bags and a 96-slot bag list are the most a save may hold.
                var p = EncounterSession.FreshProgress();
                for (int i = 0; i < EncounterSave.MaxPouches; i++) p.pouches.Add("bag.test_" + i);
                while (p.bag.Count < Inventory.BagSize * 4) p.bag.Add(new ItemStack());
                save.Write(p);
                Assert.IsTrue(save.Read(out var most, out var message), message);
                Assert.AreEqual(EncounterSave.MaxPouches, most.pouches.Count); Assert.AreEqual(Inventory.BagSize * 4, most.bag.Count);
                // A ninth is refused.
                p.pouches.Add("bag.test_8"); save.Write(p);
                string onDisk = File.ReadAllText(path);
                Assert.IsFalse(save.Read(out var loaded, out message)); Assert.IsNull(loaded); Assert.AreEqual("Invalid item data.", message);
                Assert.AreEqual(onDisk, File.ReadAllText(path), "A refused save is left exactly as it was.");
                // Nine entries of which one is a repeat are eight bags: the repeat is dropped before the count.
                p.pouches[8] = "bag.test_0"; save.Write(p);
                Assert.IsTrue(save.Read(out var eight, out message), message); Assert.AreEqual(EncounterSave.MaxPouches, eight.pouches.Count);
                // A bag list longer than 96 slots is refused too.
                p.pouches.RemoveAt(8); p.bag.Add(new ItemStack()); save.Write(p);
                onDisk = File.ReadAllText(path);
                Assert.IsFalse(save.Read(out loaded, out message)); Assert.AreEqual("Invalid item data.", message);
                Assert.AreEqual(onDisk, File.ReadAllText(path));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        // ---------- trade bags in the save (the bags step) ----------
        static ItemDatabase Items()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return ItemDatabase.Parse(texts);
        }

        [Test] public void Pouches_round_trip_with_their_slots()
        {
            string root = Temp();
            try
            {
                var db = Items(); var p = EncounterSession.FreshProgress();
                foreach (var b in new[] { "bag.simples_wallet", "bag.ore_poke" })
                {
                    Inventory.Add(p, db, b, 1); Assert.IsTrue(Inventory.Wear(p, db, p.bag.FindIndex(s => s.item == b), out var why), why);
                }
                Inventory.Add(p, db, "mat.yarrow", 23); Inventory.Add(p, db, "mat.copper_ore", 4); Inventory.Add(p, db, "potion.minor", 1);
                Assert.AreEqual(Inventory.BagSize + 6 + 8, p.bag.Count);
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                CollectionAssert.AreEqual(new[] { "bag.simples_wallet", "bag.ore_poke" }, loaded.pouches, "Worn, in the order put on.");
                Assert.AreEqual(Inventory.BagSize + 14, loaded.bag.Count, "The trade bags' slots come back with the 24.");
                Assert.AreEqual("mat.yarrow", loaded.bag[Inventory.BagSize].item); Assert.AreEqual(20, loaded.bag[Inventory.BagSize].count);
                Assert.AreEqual("mat.yarrow", loaded.bag[Inventory.BagSize + 1].item); Assert.AreEqual(3, loaded.bag[Inventory.BagSize + 1].count);
                Assert.AreEqual("mat.copper_ore", loaded.bag[Inventory.BagSize + 6].item); Assert.AreEqual(4, loaded.bag[Inventory.BagSize + 6].count);
                Assert.AreEqual("potion.minor", loaded.bag[0].item);
                // Brought up to date after a load: nothing is added or lost, and the rules hold where they were.
                Inventory.EnsurePouches(loaded, db); Assert.AreEqual(Inventory.BagSize + 14, loaded.bag.Count);
                Assert.AreSame(db.Get("bag.ore_poke"), Inventory.PouchAt(loaded, db, Inventory.BagSize + 6));
                Assert.AreEqual(0, Inventory.Add(loaded, db, "mat.charcoal", 2)); Assert.AreEqual("mat.charcoal", loaded.bag[Inventory.BagSize + 7].item);
                save.Write(loaded);
                Assert.IsTrue(save.Read(out var again, out message), message);
                Assert.AreEqual(JsonUtility.ToJson(loaded), JsonUtility.ToJson(again));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void An_unknown_pouch_is_kept_and_ignored()
        {
            string root = Temp();
            try
            {
                var db = Items();
                // A save from a build that had a bag this one does not: its eight slots came first and still hold ore.
                var p = EncounterSession.FreshProgress();
                p.pouches.Add("bag.of_holding"); p.pouches.Add("bag.simples_wallet");
                while (p.bag.Count < Inventory.BagSize + 8 + 6) p.bag.Add(new ItemStack());
                p.bag[Inventory.BagSize] = new ItemStack { item = "mat.copper_ore", count = 5 };
                p.bag[Inventory.BagSize + 9] = new ItemStack { item = "potion.minor", count = 1 };
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                CollectionAssert.AreEqual(new[] { "bag.of_holding", "bag.simples_wallet" }, loaded.pouches, "The unknown bag is kept in the save.");
                Inventory.EnsurePouches(loaded, db);
                Assert.AreEqual(Inventory.BagSize + 14, loaded.bag.Count, "Never shortened.");
                // Ignored: only the wallet has slots, and they start straight after the 24.
                var ranges = Inventory.Pouches(loaded, db);
                Assert.AreEqual(1, ranges.Count); Assert.AreEqual("bag.simples_wallet", ranges[0].bag.id); Assert.AreEqual(Inventory.BagSize, ranges[0].start);
                Assert.IsFalse(Inventory.Accepts(loaded, db, Inventory.BagSize + 9, db.Get("mat.yarrow")), "Past every known bag a slot takes nothing new.");
                Assert.IsNull(Inventory.PouchAt(loaded, db, Inventory.BagSize + 9));
                Assert.AreEqual(0, Inventory.Add(loaded, db, "mat.yarrow", 3));
                Assert.AreNotEqual("mat.yarrow", loaded.bag[Inventory.BagSize + 9].item); Assert.AreEqual("mat.yarrow", loaded.bag[Inventory.BagSize + 1].item, "Yarrow goes in the wallet's first free slot.");
                // What is there can still be taken out, and counts.
                Assert.AreEqual(5, Inventory.Count(loaded, "mat.copper_ore")); Assert.AreEqual(1, Inventory.Count(loaded, "potion.minor"));
                Assert.IsTrue(Inventory.Move(loaded, db, Inventory.BagSize + 9, 2)); Assert.AreEqual("potion.minor", loaded.bag[2].item); Assert.IsTrue(loaded.bag[Inventory.BagSize + 9].Empty);
                Assert.IsFalse(Inventory.Move(loaded, db, 2, Inventory.BagSize + 9, out var why), "It doesn't go back."); Assert.AreEqual("Nothing more goes in there.", why);
                // And it writes back as it was read.
                save.Write(loaded); Assert.IsTrue(save.Read(out var again, out message), message);
                CollectionAssert.AreEqual(loaded.pouches, again.pouches); Assert.AreEqual(loaded.bag.Count, again.bag.Count);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
