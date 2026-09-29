using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>Quest content validates, and the quest log accepts, advances, delivers, pays out and saves.</summary>
    public class QuestLogTests
    {
        static QuestDatabase RealContent()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Quests");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return QuestDatabase.Parse(texts);
        }
        static QuestLog Fresh(out List<string> said, QuestDatabase db = null)
        {
            var log = new QuestLog(db ?? RealContent(), EncounterSession.FreshProgress());
            var lines = new List<string>(); log.Say = (t, s) => lines.Add((s != null ? s + ": " : "") + t); said = lines;
            return log;
        }

        [Test] public void Shipped_quest_content_is_valid_and_has_a_chronicle()
        {
            var db = RealContent();
            Assert.GreaterOrEqual(db.Quests.Count, 10);
            Assert.IsTrue(db.Quests.ContainsKey("main.oakhaven.1"));
            Assert.AreEqual("auto", db.Quests["main.oakhaven.1"].giver);
            Assert.IsTrue(db.Factions.ContainsKey("concord") && db.Factions["concord"].fixedStanding);
            foreach (var q in db.Ordered) Assert.IsFalse(string.IsNullOrEmpty(q.canonStatus), q.id + " must say its lore status.");
        }

        [Test] public void Bad_content_is_rejected_with_every_problem_listed()
        {
            var e = Assert.Throws<System.ArgumentException>(() => QuestDatabase.Parse(new[] {
                "{\"quests\":[{\"id\":\"a\",\"title\":\"A\",\"kind\":\"weird\",\"giver\":\"X\",\"turnIn\":\"X\",\"steps\":[{\"objectives\":[{\"type\":\"deliver\",\"target\":\"X\",\"item\":\"nope\"}]}]}," +
                "{\"id\":\"a\",\"title\":\"dup\",\"giver\":\"X\",\"turnIn\":\"X\",\"steps\":[]}]}" }));
            StringAssert.Contains("unknown kind", e.Message); StringAssert.Contains("known item", e.Message); StringAssert.Contains("Duplicate quest", e.Message);
        }

        [Test] public void Chronicle_starts_itself_and_catches_up_with_what_you_already_did()
        {
            var log = Fresh(out _);
            log.StartAutomatic("zone.oakhaven");
            var s = log.State("main.oakhaven.1"); Assert.NotNull(s); Assert.AreEqual(0, s.step);
            // An older save: Mira recruited, all five enemies dead, blade equipped.
            log.Progress.recruited = true; log.Progress.equippedItem = "item.training_blade";
            log.Reconcile(f => f == "recruited" || f == "equipped:item.training_blade", t => 5);
            Assert.AreEqual(3, s.step, "Skips straight to searching the wagon.");
            Assert.IsTrue(log.Wants("item.bureau_ledger", "Bureau wagon"));
            Assert.IsFalse(log.Wants("item.bureau_ledger", "Tithe crate"), "Only the wagon holds the ledger.");
        }

        [Test] public void Wagon_ledger_delivery_turn_in_and_the_next_chapter()
        {
            var log = Fresh(out var said); log.StartAutomatic("zone.oakhaven");
            var s = log.State("main.oakhaven.1");
            log.Reconcile(f => true, t => 5);
            string revealed = null; log.Revealed = id => revealed = id;
            log.GiveItem("item.bureau_ledger"); log.Reconcile(f => true, t => 5);
            Assert.AreEqual("doc.oakhaven.ledger", revealed); Assert.Contains("doc.oakhaven.ledger", log.Progress.documents);
            Assert.AreEqual(4, s.step);
            Assert.AreEqual('?', log.Marker("Corwin Ashby", "zone.oakhaven", 1, out bool grey)); Assert.IsFalse(grey);
            Assert.IsTrue(log.TalkTo("Corwin Ashby"));
            Assert.AreEqual(0, log.ItemCount("item.bureau_ledger"), "The ledger is handed over.");
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(log.Def("main.oakhaven.1"), "zone.oakhaven"));
            int xp = log.Progress.experience;
            Assert.IsTrue(log.TurnIn(log.Def("main.oakhaven.1"), out int gained));
            Assert.AreEqual(xp + gained, log.Progress.experience); Assert.AreEqual(250, log.Standing("oakhaven"));
            var offers = log.For("Corwin Ashby", "zone.oakhaven", 1);
            Assert.IsTrue(offers.Exists(o => o.quest.id == "main.oakhaven.2"), "Chapter two is offered next.");
            Assert.IsTrue(offers.Exists(o => o.quest.id == "npc.corwin.lullaby"));
        }

        [Test] public void Delivery_quest_needs_the_item_and_pays_the_right_person()
        {
            var log = Fresh(out _); var q = log.Def("npc.goody.eggs");
            Assert.AreEqual('!', log.Marker("Goody Marl", "zone.oakhaven", 1, out _));
            Assert.IsTrue(log.Accept(q, "zone.oakhaven")); Assert.AreEqual(1, log.ItemCount("item.egg_basket"));
            Assert.IsFalse(log.TalkTo("Goody Marl"), "The giver isn't the one who wants them.");
            Assert.IsTrue(log.TalkTo("Hedda Thorne"));
            Assert.IsTrue(log.TurnIn(q, out _)); Assert.IsTrue(log.IsDone(q.id));
            Assert.AreEqual(QuestStatus.Available, log.Status(log.Def("npc.hedda.flour"), "zone.oakhaven"), "Unlocks the baker's quest.");
            Assert.AreEqual(QuestStatus.Unavailable, log.Status(log.Def("npc.hedda.flour"), "zone.khaven"), "Quests belong to their zone.");
        }

        [Test] public void Collect_counts_come_from_the_bag_and_gathering_stops_when_full()
        {
            var log = Fresh(out _); var q = log.Def("npc.lisbet.yarrow");
            Assert.IsFalse(log.Wants("item.yarrow", "Yarrow"), "Nobody gathers yarrow for no reason.");
            log.Accept(q, "zone.oakhaven");
            for (int i = 0; i < 5; i++) { Assert.IsTrue(log.Wants("item.yarrow", "Yarrow")); log.GiveItem("item.yarrow"); }
            Assert.IsFalse(log.Wants("item.yarrow", "Yarrow"));
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, "zone.oakhaven"));
            Assert.AreEqual('?', log.Marker("Mira", "zone.oakhaven", 1, out _));
        }

        [Test] public void Interact_counts_repeat_and_kill_patterns_match_prefixes()
        {
            var log = Fresh(out var said); var q = log.Def("faction.preservationists.1");
            log.Accept(q, "zone.oakhaven");
            log.Notify("interact", "Grey-hearted oak"); log.Notify("interact", "Some other tree"); log.Notify("interact", "Grey-hearted oak");
            Assert.AreEqual(2, log.Count(log.State(q.id), 0));
            log.Notify("interact", "Grey-hearted oak");
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, "zone.oakhaven"));
            Assert.IsTrue(QuestLog.Matches("oakhaven.*", "oakhaven.warden.0")); Assert.IsFalse(QuestLog.Matches("oakhaven.*", "khaven.outrider.0"));
        }

        [Test] public void Main_quests_cannot_be_abandoned_but_side_quests_return_their_items()
        {
            var log = Fresh(out _); log.StartAutomatic("zone.oakhaven");
            Assert.IsFalse(log.Abandon("main.oakhaven.1"));
            log.Accept(log.Def("npc.goody.eggs"), "zone.oakhaven");
            Assert.IsTrue(log.Abandon("npc.goody.eggs")); Assert.AreEqual(0, log.ItemCount("item.egg_basket"));
            Assert.AreEqual(QuestStatus.Available, log.Status(log.Def("npc.goody.eggs"), "zone.oakhaven"));
        }

        [Test] public void Standing_tiers_and_fixed_factions()
        {
            var log = Fresh(out var said);
            Assert.AreEqual("Distrusted", QuestLog.TierNames[QuestLog.Tier(log.Standing("sandthrone"))]);
            Assert.AreEqual("Hostile", QuestLog.TierNames[QuestLog.Tier(log.Standing("concord"))]);
            log.ChangeStanding("concord", 5000); Assert.AreEqual(-4000, log.Standing("concord"), "The Concord can't be won over.");
            log.ChangeStanding("oakhaven", 1200); Assert.AreEqual("Trusted", QuestLog.TierNames[QuestLog.Tier(log.Standing("oakhaven"))]);
            Assert.IsTrue(said.Exists(l => l.Contains("You are now Trusted")));
        }

        [Test] public void Quest_state_survives_a_save_round_trip()
        {
            var root = Path.Combine(Path.GetTempPath(), "Crulanda-quests-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var log = Fresh(out _); log.StartAutomatic("zone.oakhaven");
                log.Accept(log.Def("npc.lisbet.yarrow"), "zone.oakhaven"); log.GiveItem("item.yarrow"); log.ChangeStanding("saltmenders", 300);
                log.Progress.usedInteractables.Add("zone.oakhaven|Tithe crate|55|45");
                var save = new EncounterSave(root, TestTalents.Warrior());
                save.Write(log.Progress);
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                Assert.AreEqual(2, loaded.quests.Count); Assert.AreEqual(1, loaded.questItems.Count);
                Assert.AreEqual(300, loaded.reputation.Find(r => r.faction == "saltmenders").value);
                Assert.Contains("zone.oakhaven|Tithe crate|55|45", loaded.usedInteractables);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
