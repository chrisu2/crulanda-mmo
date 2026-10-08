using System;
using System.IO;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    public class EncounterRulesTests
    {
        [Test]
        public void Kill_and_loot_credit_cannot_be_duplicated()
        {
            var p = EncounterSession.FreshProgress();
            Assert.IsFalse(p.Loot("a"));
            Assert.IsTrue(p.AwardKill("a", 150)); Assert.IsFalse(p.AwardKill("a", 150));
            Assert.IsTrue(p.Loot("a")); Assert.IsFalse(p.Loot("a"));
            Assert.AreEqual(150, p.experience); Assert.AreEqual(8, p.gold);
           Assert.AreEqual(1, p.Level); p.AwardKill("b", 450); Assert.AreEqual(2, p.Level);   // 400 to level 2 (round 29)
        }
        [Test]
        public void Taunt_expires_and_dead_targets_are_ignored()
        {
            var t = new EncounterThreat();
            t.Add("healer", 100); t.Add("player", 10);
            Assert.AreEqual("healer", t.Choose(0, id => true));
            t.Taunt("player", 0); t.Add("healer", 500);
            Assert.AreEqual("player", t.Choose(2, id => true));
            Assert.AreEqual("healer", t.Choose(4, id => true));
            Assert.AreEqual("player", t.Choose(4, id => id != "healer"));
        }
        [Test]
        public void Save_round_trip_preserves_identity_party_equipment_and_rewards()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-" + Guid.NewGuid().ToString("N"));
            try
            {
                var p = EncounterSession.FreshProgress(); p.recruited = true; p.relationship = 4;
                p.AwardKill("a", 28); p.Loot("a"); p.equipment[(int)EquipSlot.MainHand] = new ItemStack { item = "blade", count = 1 };
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(save.Read(out var loaded, out var error), error);
                Assert.AreEqual(p.playerId, loaded.playerId); Assert.AreEqual(p.companionId, loaded.companionId);
                Assert.AreEqual("blade", loaded.equipment[(int)EquipSlot.MainHand].item); Assert.AreEqual(4, loaded.relationship);
                Assert.IsTrue(loaded.recruited); Assert.IsFalse(loaded.AwardKill("a", 28)); Assert.IsFalse(loaded.Loot("a"));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        [Test]
        public void Level_curve_kill_experience_and_old_save_migration()
        {
            Assert.AreEqual(0, EncounterProgress.XpForLevel(1)); Assert.AreEqual(400, EncounterProgress.XpForLevel(2)); Assert.AreEqual(9720, EncounterProgress.XpForLevel(10));   // round 29: 400 + 170 a level
            var p = EncounterSession.FreshProgress(); p.experience = 969; Assert.AreEqual(2, p.Level); p.experience = 970; Assert.AreEqual(3, p.Level);
            p.experience = 999999; Assert.AreEqual(EncounterProgress.LevelCap, p.Level);
            Assert.AreEqual(28, EncounterProgress.KillXp(1, 1, false));
            Assert.Greater(EncounterProgress.KillXp(5, 3, false), EncounterProgress.KillXp(5, 5, false), "Higher mobs give more.");
            Assert.AreEqual(0, EncounterProgress.KillXp(1, 5, false), "Grey mobs give nothing.");
            Assert.AreEqual(2 * EncounterProgress.KillXp(4, 4, false), EncounterProgress.KillXp(4, 4, true));
            // Old 60-XP levels keep their level and fraction: 540 was level 10, 90 was halfway through level 2.
            Assert.AreEqual(EncounterProgress.XpForLevel(10), EncounterProgress.MigrateExperience(540));
            Assert.AreEqual(EncounterProgress.XpForLevel(2) + EncounterProgress.XpToNext(2) / 2, EncounterProgress.MigrateExperience(90));
        }
    }
}