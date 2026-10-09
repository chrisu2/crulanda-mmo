using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// Quest gear (2026-10-09): the named pieces whose source is a quest are handed out at turn-in, one piece given, several a choice
    /// of one; a piece already held is paid in crowns, full bags wait, and the legacy Trailblade (a new character's) is not given again.
    /// </summary>
    public class QuestGearTests
    {
        const string Wenna = "npc.wenna.wolves", Jerkin = "loot.kha.wood_edge_jerkin", DeadLine = "side.adit.dead_line", Hook = "loot.adit.couplers_hook";

        static QuestLog Log(out List<string> said)
        {
            var items = LootTestData.Items();
            var log = new QuestLog(LootTestData.Quests(), EncounterSession.FreshProgress()) { Items = items, Loot = LootTestData.Loot(items) };
            var lines = new List<string>(); log.Say = (t, s) => lines.Add((s != null ? s + ": " : "") + t); said = lines;
            return log;
        }
        /// <summary>The quest taken on and every step done: ready to hand in.</summary>
        static QuestDef Ready(QuestLog log, string id)
        {
            var q = log.Def(id); Assert.NotNull(q, id);
            log.Progress.quests.Add(new QuestState { id = id, step = q.steps.Length });
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, q.zone));
            return q;
        }

        [Test] public void Every_quest_piece_has_its_quest_and_only_the_Dead_Line_offers_a_choice()
        {
            var log = Log(out _);
            var pieces = log.Loot.GearOrder.Where(g => g.source.StartsWith("quest:") && !g.legacy).ToList();
            Assert.AreEqual(18, pieces.Count, "The eighteen quest pieces (the Trailblade, legacy, comes with a new character).");
            foreach (var g in pieces) Assert.NotNull(log.Def(g.source.Substring(6)), g.id + "'s quest is real.");
            var byQuest = pieces.GroupBy(g => g.source).ToDictionary(x => x.Key.Substring(6), x => x.Count());
            Assert.AreEqual(3, byQuest[DeadLine], "The Dead Line: a choice of three.");
            Assert.IsTrue(byQuest.Where(x => x.Key != DeadLine).All(x => x.Value == 1), "Every other quest gives its one piece.");
            CollectionAssert.AreEqual(new[] { "loot.adit.sleeper_splitter", Hook, "loot.adit.weavers_tuning_drop" }, log.GearRewards(log.Def(DeadLine)).Select(d => d.id).ToArray(), "In the loot file's order.");
            Assert.AreEqual(0, log.GearRewards(log.Def("main.oakhaven.1")).Count, "The Trailblade is not given again.");
        }

        [Test] public void A_single_piece_goes_to_the_bags_at_turn_in()
        {
            var log = Log(out var said); var q = Ready(log, Wenna); var p = log.Progress;
            Assert.IsTrue(log.TurnIn(q, out _));
            Assert.AreEqual(1, Inventory.Count(p, Jerkin));
            Assert.IsTrue(said.Contains("Received: " + log.Items.Get(Jerkin).name + ". Right-click it in your bags [I] to wear it."));
            Assert.IsTrue(log.IsDone(Wenna));
        }

        [Test] public void A_choice_waits_for_a_pick_and_gives_only_that_piece()
        {
            var log = Log(out var said); var q = Ready(log, DeadLine); var p = log.Progress;
            int xp = p.experience, gold = p.gold;
            Assert.IsFalse(log.TurnIn(q, out _), "No pick yet.");
            Assert.IsTrue(said.Contains(QuestLog.ChooseLine));
            Assert.IsFalse(log.TurnIn(q, out _, Jerkin), "Not one of its pieces.");
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, q.zone)); Assert.AreEqual(xp, p.experience); Assert.AreEqual(gold, p.gold);
            Assert.IsTrue(log.TurnIn(q, out _, Hook));
            Assert.AreEqual(1, Inventory.Count(p, Hook));
            foreach (var d in log.GearRewards(q)) if (d.id != Hook) Assert.AreEqual(0, Inventory.Count(p, d.id), d.id + " was not picked.");
            Assert.IsTrue(log.IsDone(DeadLine));
        }

        [Test] public void A_piece_already_held_is_paid_in_crowns()
        {
            var log = Log(out var said); var q = Ready(log, Wenna); var p = log.Progress;
            Inventory.Add(p, log.Items, Jerkin, 1); int gold = p.gold;
            Assert.IsTrue(log.TurnIn(q, out _));
            Assert.AreEqual(1, Inventory.Count(p, Jerkin), "No second jerkin.");
            Assert.AreEqual(gold + (q.rewards?.gold ?? 0) + log.Items.Get(Jerkin).value, p.gold);
            Assert.IsTrue(said.Exists(l => l.EndsWith(QuestLog.HaveOneLine)));
        }

        [Test] public void Full_bags_wait_and_nothing_is_lost()
        {
            var log = Log(out var said); var q = Ready(log, Wenna); var p = log.Progress;
            for (int i = 0; i < Inventory.BagSize; i++) if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "potion.minor", count = 1 };
            int xp = p.experience;
            Assert.IsFalse(log.TurnIn(q, out _));
            Assert.IsTrue(said.Contains(QuestLog.MakeRoomLine)); Assert.AreEqual(xp, p.experience); Assert.IsFalse(log.IsDone(Wenna));
            p.bag[5] = new ItemStack();
            Assert.IsTrue(log.TurnIn(q, out _)); Assert.AreEqual(Jerkin, p.bag[5].item);
        }
    }
}
