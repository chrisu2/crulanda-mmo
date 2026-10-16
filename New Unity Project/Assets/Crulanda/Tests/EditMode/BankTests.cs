using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;

namespace Crulanda.Tests
{
    /// <summary>The Treasury Vault (CAPITAL_HUB.md; TreasuryVault, CharacterProfile format 2): putting in and taking out, stacks merging,
    /// quest items refused, rows bought, one vault for every character, the profile's round trip and its migration from format 1.</summary>
    public class BankTests
    {
        const string QuestItemFile = "{\"items\":[{\"id\":\"quest.sealed_letter\",\"name\":\"Sealed Letter\",\"kind\":\"quest\",\"quality\":1}]}";
        static ItemDatabase Db(bool withQuestItem = false)
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            if (withQuestItem) texts.Add(QuestItemFile);
            return ItemDatabase.Parse(texts);
        }
        static EncounterProgress Character(string classId, int gold = 0) { var p = EncounterSession.FreshProgress(); p.classId = classId; p.gold = gold; Inventory.Ensure(p); return p; }
        static string TempRoot() { return Path.Combine(Path.GetTempPath(), "Crulanda-bank-" + Guid.NewGuid().ToString("N")); }

        [Test] public void A_new_vault_has_48_slots()
        {
            var v = TreasuryVault.Ensure(null);
            Assert.AreEqual(48, TreasuryVault.Size(v)); Assert.AreEqual(48, v.slots.Count); Assert.AreEqual(0, TreasuryVault.Used(v));
        }

        [Test] public void Deposit_merges_stacks_and_withdraw_brings_them_back()
        {
            var db = Db(); var p = Character("class.warrior"); var v = TreasuryVault.Ensure(null);
            Assert.AreEqual(0, Inventory.Add(p, db, "junk.wolf_pelt", 12));   // a full stack of 10 and 2
            int two = p.bag.FindIndex(s => s.item == "junk.wolf_pelt" && s.count == 2), ten = p.bag.FindIndex(s => s.item == "junk.wolf_pelt" && s.count == 10);
            Assert.AreEqual(2, TreasuryVault.Deposit(v, p, db, two, -1, out var why), why);
            Assert.AreEqual(10, TreasuryVault.Deposit(v, p, db, ten, -1, out why), why);
            Assert.AreEqual(12, TreasuryVault.Count(v, "junk.wolf_pelt")); Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_pelt"), "the bags gave all of it");
            Assert.AreEqual(10, v.slots[0].count, "the first stack was topped up to a full 10"); Assert.AreEqual(2, v.slots[1].count, "the rest in the next slot");
            Assert.IsTrue(p.bag[two].Empty && p.bag[ten].Empty);
            // Onto a chosen slot.
            Inventory.Add(p, db, "potion.minor", 3);
            int potion = p.bag.FindIndex(s => s.item == "potion.minor");
            Assert.AreEqual(3, TreasuryVault.Deposit(v, p, db, potion, 30, out why)); Assert.AreEqual("potion.minor", v.slots[30].item);
            // Out again: into a chosen bag slot, and wherever it fits.
            Assert.AreEqual(3, TreasuryVault.Withdraw(v, p, db, 30, 5, out why), why); Assert.AreEqual("potion.minor", p.bag[5].item); Assert.IsTrue(v.slots[30].Empty);
            Assert.AreEqual(10, TreasuryVault.Withdraw(v, p, db, 0, -1, out why)); Assert.AreEqual(2, TreasuryVault.Withdraw(v, p, db, 1, -1, out why));
            Assert.AreEqual(12, Inventory.Count(p, "junk.wolf_pelt")); Assert.AreEqual(0, TreasuryVault.Used(v));
        }

        [Test] public void Withdraw_into_full_bags_leaves_it_in_the_vault()
        {
            var db = Db(); var p = Character("class.warrior"); var v = TreasuryVault.Ensure(null);
            v.slots[0] = new ItemStack { item = "potion.minor", count = 4 };
            for (int i = 0; i < Inventory.BagSize; i++) p.bag[i] = new ItemStack { item = ItemDatabase.GearId("feet", 2, 1, i), count = 1 };
            Assert.AreEqual(0, TreasuryVault.Withdraw(v, p, db, 0, -1, out var why)); Assert.AreEqual("Your bags are full.", why);
            Assert.AreEqual(4, v.slots[0].count, "nothing lost");
        }

        [Test] public void Quest_items_are_refused()
        {
            var db = Db(true); var p = Character("class.warrior"); var v = TreasuryVault.Ensure(null);
            p.bag[0] = new ItemStack { item = "quest.sealed_letter", count = 1 };
            Assert.AreEqual(0, TreasuryVault.Deposit(v, p, db, 0, -1, out var why)); Assert.AreEqual(TreasuryVault.QuestLine, why);
            Assert.AreEqual("quest.sealed_letter", p.bag[0].item, "it stays in the bags"); Assert.AreEqual(0, TreasuryVault.Used(v));
        }

        [Test] public void A_full_vault_refuses_and_rows_are_bought_at_a_rising_price()
        {
            var db = Db(); var p = Character("class.warrior", 50); var v = TreasuryVault.Ensure(null);
            for (int i = 0; i < v.slots.Count; i++) v.slots[i] = new ItemStack { item = ItemDatabase.GearId("hands", 3, 1, i), count = 1 };
            Inventory.Add(p, db, "potion.minor", 1); int at = p.bag.FindIndex(s => s.item == "potion.minor");
            Assert.AreEqual(0, TreasuryVault.Deposit(v, p, db, at, -1, out var why)); Assert.AreEqual(TreasuryVault.FullLine, why);
            Assert.IsFalse(TreasuryVault.BuyRow(v, p, out why), "50 crowns is not enough"); Assert.AreEqual(50, p.gold);
            p.gold = 100000; int last = 0;
            for (int row = 0; row < TreasuryVault.MaxBoughtRows; row++)
            {
                int price = TreasuryVault.NextRowPrice(v); Assert.Greater(price, last, "each row dearer than the last"); last = price;
                int gold = p.gold; Assert.IsTrue(TreasuryVault.BuyRow(v, p, out why), why);
                Assert.AreEqual(gold - price, p.gold); Assert.AreEqual(48 + 12 * (row + 1), TreasuryVault.Size(v)); Assert.AreEqual(TreasuryVault.Size(v), v.slots.Count);
            }
            Assert.AreEqual(-1, TreasuryVault.NextRowPrice(v)); Assert.IsFalse(TreasuryVault.BuyRow(v, p, out why), "four rows at most");
            Assert.AreEqual(1, TreasuryVault.Deposit(v, p, db, at, -1, out why), "room now");
        }

        [Test] public void One_vault_is_shared_by_every_character_and_survives_a_save()
        {
            string root = TempRoot(); var db = Db();
            try
            {
                CharacterProfile.Remember(root, "class.warrior");
                var warrior = Character("class.warrior", 300); Inventory.Add(warrior, db, "mat.copper_ore", 15);
                var v = CharacterProfile.LoadVault(root, out var msg); Assert.NotNull(v, msg);
                Assert.AreEqual(15, TreasuryVault.Deposit(v, warrior, db, warrior.bag.FindIndex(s => s.item == "mat.copper_ore"), -1, out _));
                Assert.IsTrue(TreasuryVault.BuyRow(v, warrior, out _));
                Assert.IsTrue(CharacterProfile.SaveVault(root, v, out msg), msg);
                // The Mage is played next: choosing her keeps the vault, and she finds the ore and the bought row.
                CharacterProfile.Remember(root, "class.mage");
                Assert.AreEqual("class.mage", CharacterProfile.LastClass(root));
                var back = CharacterProfile.LoadVault(root, out msg); Assert.NotNull(back, msg);
                Assert.AreEqual(15, TreasuryVault.Count(back, "mat.copper_ore")); Assert.AreEqual(1, back.rowsBought); Assert.AreEqual(60, back.slots.Count);
                var mage = Character("class.mage");
                Assert.AreEqual(15, TreasuryVault.Withdraw(back, mage, db, back.slots.FindIndex(s => s.item == "mat.copper_ore"), -1, out _));
                Assert.IsTrue(CharacterProfile.SaveVault(root, back, out msg), msg);
                Assert.AreEqual(15, Inventory.Count(mage, "mat.copper_ore")); Assert.AreEqual(0, TreasuryVault.Used(CharacterProfile.LoadVault(root, out _)));
                Assert.AreEqual("class.mage", CharacterProfile.LastClass(root), "saving the vault keeps the character choice");
                Assert.IsTrue(new SaveFileStore(root).TryRead("profile", out var env, out _)); Assert.AreEqual(CharacterProfile.FormatVersion, env.formatVersion); Assert.AreEqual(2, env.formatVersion);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void No_profile_gives_an_empty_vault()
        {
            string root = TempRoot();
            try { var v = CharacterProfile.LoadVault(root, out var msg); Assert.NotNull(v, msg); Assert.AreEqual(48, v.slots.Count); Assert.AreEqual(0, TreasuryVault.Used(v)); }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void A_format_1_profile_migrates_with_an_empty_vault_and_keeps_its_character()
        {
            const string v1 = "{\"lastClass\":\"class.druid\"}";
            Assert.AreEqual("{\"lastClass\":\"class.druid\",\"vault\":[],\"vaultRows\":0}", new AddVaultMigration().Migrate(v1), "only the two fields added, the rest as it was");
            Assert.AreEqual("{\"vault\":[],\"vaultRows\":0}", new AddVaultMigration().Migrate("{}"));
            string done = "{\"lastClass\":\"class.druid\",\"vault\":[],\"vaultRows\":1}"; Assert.AreEqual(done, new AddVaultMigration().Migrate(done), "a payload that has them is left alone");
            Assert.Throws<SaveMigrationException>(() => new AddVaultMigration().Migrate("[1,2]"));
            string root = TempRoot();
            try
            {
                var store = new SaveFileStore(root);
                store.Write("profile", new SaveEnvelope { formatVersion = 1, payloadType = "CrulandaProfile", gameVersion = "0.3.0", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = v1 });
                Assert.AreEqual("class.druid", CharacterProfile.LastClass(root));
                var v = CharacterProfile.LoadVault(root, out var msg); Assert.NotNull(v, msg);
                Assert.AreEqual(48, v.slots.Count); Assert.AreEqual(0, TreasuryVault.Used(v)); Assert.AreEqual(0, v.rowsBought);
                Assert.IsTrue(store.TryRead("profile", out var still, out _)); Assert.AreEqual(1, still.formatVersion, "reading does not write: the file is format 2 from its next save");
                v.slots[3] = new ItemStack { item = "potion.minor", count = 2 };
                Assert.IsTrue(CharacterProfile.SaveVault(root, v, out msg), msg);
                Assert.IsTrue(store.TryRead("profile", out var now, out _)); Assert.AreEqual(2, now.formatVersion);
                Assert.AreEqual("class.druid", CharacterProfile.LastClass(root), "the character choice came through");
                Assert.AreEqual(2, TreasuryVault.Count(CharacterProfile.LoadVault(root, out _), "potion.minor"));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void A_newer_profile_is_not_opened_or_written_over()
        {
            string root = TempRoot();
            try
            {
                var store = new SaveFileStore(root); string payload = "{\"lastClass\":\"class.mage\",\"vault\":[],\"vaultRows\":0,\"later\":1}";
                store.Write("profile", new SaveEnvelope { formatVersion = 99, payloadType = "CrulandaProfile", gameVersion = "9", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = payload });
                Assert.IsNull(CharacterProfile.LoadVault(root, out var msg)); Assert.IsFalse(string.IsNullOrEmpty(msg));
                Assert.IsFalse(CharacterProfile.SaveVault(root, TreasuryVault.Ensure(null), out _));
                CharacterProfile.Remember(root, "class.warrior");
                Assert.IsTrue(store.TryRead("profile", out var env, out _)); Assert.AreEqual(99, env.formatVersion); Assert.AreEqual(payload, env.payloadJson, "left as it was");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
