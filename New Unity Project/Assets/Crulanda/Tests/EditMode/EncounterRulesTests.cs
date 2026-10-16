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
        public void A_save_deep_in_the_adit_loads_and_one_off_the_world_does_not()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-" + Guid.NewGuid().ToString("N"));
            try
            {
                // The drawn-out Sealed Adit's Rail Hall lies 80-90 m down: a save there came back a new character (2026-10-11).
                var p = EncounterSession.FreshProgress(); p.experience = 9720; p.keys.Add("adit.lock"); p.x = 117; p.y = -95; p.z = 284;
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(save.Read(out var loaded, out var error), error);
                Assert.AreEqual(-95, loaded.y, .01f); Assert.AreEqual(9720, loaded.experience); CollectionAssert.Contains(loaded.keys, "adit.lock");
                foreach (var off in new[] { new[] { 0f, -EncounterSave.MaxDepth - 1, 0 }, new[] { 0f, EncounterSave.MaxHeight + 1, 0 }, new[] { EncounterSave.MaxAcross + 1, 0, 0 }, new[] { 0, 0, float.NaN } })
                {
                    p.x = off[0]; p.y = off[1]; p.z = off[2]; save.Write(p);
                    Assert.IsFalse(save.Read(out _, out _), "a place off the world (" + off[0] + ", " + off[1] + ", " + off[2] + ") is not a save this game wrote");
                }
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
        /// <summary>Full screen is borderless at the monitor's own size, never the exclusive mode, and a start-up puts right anything else
        /// that is full screen (playtest note 121, Chris, 2026-10-11: blurry at 3440x1440 and his desktop's text wrong after alt-tab).</summary>
        [Test]
        public void Full_screen_is_always_borderless_at_the_monitors_own_size()
        {
            CollectionAssert.DoesNotContain(EncounterHud.DisplayModes, UnityEngine.FullScreenMode.ExclusiveFullScreen, "no mode switches the monitor");
            Assert.AreEqual((3440, 1440, UnityEngine.FullScreenMode.FullScreenWindow), EncounterHud.StartupDisplay(UnityEngine.FullScreenMode.ExclusiveFullScreen, 1920, 1080, 3440, 1440));
            Assert.AreEqual((3440, 1440, UnityEngine.FullScreenMode.FullScreenWindow), EncounterHud.StartupDisplay(UnityEngine.FullScreenMode.FullScreenWindow, 1920, 1080, 3440, 1440));
            Assert.IsNull(EncounterHud.StartupDisplay(UnityEngine.FullScreenMode.FullScreenWindow, 3440, 1440, 3440, 1440), "already right");
            Assert.IsNull(EncounterHud.StartupDisplay(UnityEngine.FullScreenMode.Windowed, 1920, 1080, 3440, 1440), "a window is left as it is");
        }
        /// <summary>The chat's hold on the keys lapses when the chat is no longer drawn (the tester's note 115).</summary>
        [Test]
        public void Typing_holds_the_keys_only_while_the_chat_is_drawn()
        {
            EncounterInput.Typing = true; Assert.IsTrue(EncounterInput.Typing);
            EncounterInput.Typing = false; Assert.IsFalse(EncounterInput.Typing);
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