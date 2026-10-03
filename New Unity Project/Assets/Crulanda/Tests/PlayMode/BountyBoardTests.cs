#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Oakhaven's notice board in play (2026-10-03): the board stands by the Golden Cask and opens with E; a kill bounty is taken
    /// on, its wolves killed, and it is paid in silver crowns at the board; the rare posting puts a Bureau courier on the North
    /// road, and his death pays the Aether-Geode shard and the bars into the bags. Saves to its own folder, never Chris's.
    /// </summary>
    public class BountyBoardTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        IEnumerator Open()
        {
            WorldClock.Hour = 10;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-board-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreEqual(root, Session().SaveDirectoryOverride, "This test saves to its own folder.");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; VillageEconomy.ResetAll();
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator The_board_by_the_Cask_pays_a_wolf_bounty_in_crowns_and_the_rare_courier_pays_the_shard()
        {
            yield return Open(); var s = Session();
            var board = s.Zone.Interactables.FirstOrDefault(i => i.kind == "board");
            Assert.NotNull(board, "Oakhaven has a notice board."); Assert.AreEqual("Read the notices", board.prompt);
            var inn = s.Zone.Zone.props.First(p => p.kind == "inn");
            Assert.Less(Vector2.Distance(new Vector2(board.position.x, board.position.z), inn.at), 20, "It stands by the Golden Cask.");
            s.UseInteractable(board);
            Assert.NotNull(s.Conversation, "Reading the board opens the postings."); Assert.AreEqual(Bounties.BoardName, s.Conversation.npc);
            Assert.AreEqual(Bounties.Slots, s.Conversation.entries.Count, "Three postings a day.");
            // Take on the wolf bounty (put it on the board for the test if today's draw left it off).
            var wolves = s.Quests.Def("bounty.oakhaven.wolves"); var state = s.Boards.Board(s.ZoneId);
            if (!state.ids.Contains(wolves.id)) { state.ids[0] = wolves.id; s.UseInteractable(board); }
            s.AcceptQuest(wolves);
            Assert.AreEqual(QuestStatus.Active, s.Quests.Status(wolves, s.ZoneId));
            Assert.IsTrue(s.Messages.Any(m => m.StartsWith("Bounty taken: ")), "Told in the bounty's words.");
            int killed = 0;
            foreach (var e in s.Enemies.Where(e => e.persistentId.StartsWith("mob.wolf.oakhaven.") && e.actor.IsAlive).Take(6)) { e.actor.Health.ApplyDamage(100000); killed++; }
            Assert.AreEqual(6, killed, "Six wolves to kill."); yield return null;
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, s.Quests.Status(wolves, s.ZoneId), "Six wolves dead: ready to hand in.");
            s.UseInteractable(board);
            Assert.AreEqual(wolves, s.Conversation.entries[0].quest, "The finished posting is first on the board.");
            int crowns = s.Progress.gold; s.CompleteQuest(wolves);
            Assert.AreEqual(crowns + wolves.rewards.gold, s.Progress.gold, "Paid thirty silver crowns.");
            Assert.IsTrue(s.Messages.Any(m => m.StartsWith("Bounty paid: ") && m.Contains("crowns")), "Said in crowns, not gold.");
            Assert.IsFalse(s.Quests.IsDone(wolves.id), "Never done for good.");
            Assert.IsFalse(s.Boards.Entries(s.ZoneId, s.Progress.Level).Any(e => e.quest == wolves), "Off the board for today.");
            // The rare posting: taken on, the Bureau courier walks the North road; killed, the shard and the bars are paid into the bags.
            var rare = s.Quests.Def("bounty.oakhaven.courier"); Assert.IsTrue(rare.rare);
            state.ids[0] = rare.id; s.UseInteractable(board); s.AcceptQuest(rare); yield return null;
            var courier = s.Enemies.FirstOrDefault(e => e.persistentId == Bounties.CourierId(s.ZoneId));
            Assert.NotNull(courier, "A Bureau courier is abroad."); Assert.IsFalse(courier.Camp); Assert.AreEqual(s.Zone.Zone.levelMax + 2, courier.actor.Level, "Two levels over the zone.");
            Assert.IsTrue(courier.actor.DisplayName.Contains("courier"));
            courier.actor.Health.ApplyDamage(100000); yield return null;
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, s.Quests.Status(rare, s.ZoneId));
            int shards = Inventory.Count(s.Progress, "mat.geode_shard"), bars = Inventory.Count(s.Progress, "mat.bogiron_bar");
            s.UseInteractable(board); s.CompleteQuest(rare);
            Assert.AreEqual(shards + 1, Inventory.Count(s.Progress, "mat.geode_shard"), "The Aether-Geode shard is in the bags.");
            Assert.AreEqual(bars + 2, Inventory.Count(s.Progress, "mat.bogiron_bar"), "And two bog-iron bars, the tier above Oakhaven's copper.");
            yield return null;
            Assert.IsFalse(s.Enemies.Any(e => e.persistentId == Bounties.CourierId(s.ZoneId)), "The courier is gone from the road.");
            // The day turns at six: the board draws again.
            int day = s.Progress.days; WorldClock.Hour = 5.9f; yield return null; WorldClock.Hour = 6.1f; yield return null; yield return null;
            Assert.AreEqual(day + 1, s.Progress.days, "A new day."); Assert.AreEqual(day + 1, s.Boards.Board(s.ZoneId).day, "The board drew again.");
            s.Save(false); s.Load(); yield return null;
            Assert.AreEqual(day + 1, Session().Progress.days, "The day count is saved.");
        }
    }
}
#endif
