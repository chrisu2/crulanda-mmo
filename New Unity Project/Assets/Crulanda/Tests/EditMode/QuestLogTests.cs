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

        // ---------- the leatherworker's bag quests (bring, bag rewards, unlessWorn) ----------
        static ItemDatabase RealItems()
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Items");
            var texts = new List<string>(); foreach (var f in Directory.GetFiles(dir, "*.json")) texts.Add(File.ReadAllText(f));
            return ItemDatabase.Parse(texts);
        }
        const string Wallet = "npc.leatherworker.wallet", Maud = "Maud Tanner", Pelt = "junk.wolf_pelt", WalletBag = "bag.simples_wallet";
        /// <summary>A fresh character with the item database bound, the wallet quest accepted.</summary>
        static QuestLog WithWalletQuest(out List<string> said)
        {
            var log = Fresh(out said); log.Items = RealItems();
            Assert.IsTrue(log.Accept(log.Def(Wallet), "zone.oakhaven"), "Maud offers the wallet at level 1.");
            return log;
        }

        [Test] public void Bring_hands_over_bag_items_on_talk()
        {
            var items = RealItems(); var db = RealContent();
            CollectionAssert.IsEmpty(db.CheckItems(items), "Every bring item, bag reward and unlessWorn names a real item (and the last two a trade bag).");
            var q = db.Quests[Wallet];
            Assert.AreEqual(Maud, q.giver); Assert.AreEqual(Maud, q.turnIn); Assert.AreEqual(WalletBag, q.unlessWorn); CollectionAssert.AreEqual(new[] { WalletBag }, q.rewards.bagItems);
            var o = q.steps[0].objectives[0]; Assert.AreEqual("bring", o.type); Assert.AreEqual(Pelt, o.item); Assert.AreEqual(3, o.count); Assert.AreEqual(Maud, o.target);

            var log = Fresh(out var said, db); log.Items = items;
            Assert.AreEqual('!', log.Marker(Maud, "zone.oakhaven", 1, out bool grey)); Assert.IsFalse(grey);
            Assert.IsTrue(log.Accept(q, "zone.oakhaven"));
            var s = log.State(Wallet); var p = log.Progress;
            Assert.AreEqual(0, log.Count(s, 0)); Assert.AreEqual("Grey wolf pelts for Maud Tanner: 0/3", log.ObjectiveLine(s, 0));
            // Pelts in the bags count, but carrying them is not handing them over.
            Inventory.Add(p, items, Pelt, 4); Inventory.Add(p, items, "junk.wolf_fang", 2);
            log.Notify("item", Pelt); log.Reconcile(f => false, t => 0);
            Assert.AreEqual(3, log.Count(s, 0)); Assert.IsFalse(log.ObjectiveDone(s, 0)); Assert.AreEqual(QuestStatus.Active, log.Status(q, "zone.oakhaven"));
            Assert.AreEqual('?', log.Marker(Maud, "zone.oakhaven", 1, out grey)); Assert.IsFalse(grey, "A gold ? over Maud: you carry enough.");
            Assert.IsFalse(log.TalkTo("Fen Walker"), "Only Maud takes them."); Assert.AreEqual(4, Inventory.Count(p, Pelt));
            // Talking to Maud hands over three, and the quest is ready to turn in.
            Assert.IsTrue(log.TalkTo(Maud));
            Assert.AreEqual(1, Inventory.Count(p, Pelt), "Three pelts handed over; the fourth is still yours."); Assert.AreEqual(2, Inventory.Count(p, "junk.wolf_fang"));
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, "zone.oakhaven"));
            Assert.IsTrue(said.Exists(l => l.StartsWith(Maud + ": Lay them on the counter")), "Maud's line as she takes them.");
            Assert.IsFalse(log.TalkTo(Maud), "Nothing more to hand over.");
            // Turned in: the wallet goes in the bags (not the quest bag), with the experience and the standing.
            int xp = p.experience;
            Assert.IsTrue(log.TurnIn(q, out int gained)); Assert.AreEqual(20, gained); Assert.AreEqual(xp + 20, p.experience);
            Assert.AreEqual(1, Inventory.Count(p, WalletBag)); Assert.AreEqual(0, log.ItemCount(WalletBag)); Assert.IsTrue(log.IsDone(Wallet));
            Assert.AreEqual(75, log.Standing("oakhaven"));
            Assert.IsTrue(said.Exists(l => l.StartsWith("Received: Simples-wallet.")));
            Assert.AreEqual(QuestStatus.Done, log.Status(q, "zone.oakhaven")); Assert.AreEqual(' ', log.Marker(Maud, "zone.oakhaven", 1, out _));
        }

        [Test] public void Bring_without_enough_changes_nothing()
        {
            var log = WithWalletQuest(out _); var p = log.Progress; var s = log.State(Wallet);
            Inventory.Add(p, log.Items, Pelt, 2);
            Assert.AreEqual('?', log.Marker(Maud, "zone.oakhaven", 1, out bool grey)); Assert.IsTrue(grey, "A grey ?: not enough yet.");
            Assert.IsFalse(log.TalkTo(Maud));
            Assert.AreEqual(2, Inventory.Count(p, Pelt), "Two pelts are not handed over."); Assert.AreEqual(2, log.Count(s, 0)); Assert.IsFalse(log.ObjectiveDone(s, 0));
            Assert.AreEqual(QuestStatus.Active, log.Status(log.Def(Wallet), "zone.oakhaven"));
            Assert.IsFalse(log.TurnIn(log.Def(Wallet), out _)); Assert.AreEqual(0, Inventory.Count(p, WalletBag));
            // Quest-bag items of the same name don't count: only the bags.
            p.questItems.Add(Pelt); Assert.IsFalse(log.TalkTo(Maud)); Assert.AreEqual(2, Inventory.Count(p, Pelt));
        }

        [Test] public void A_bag_quest_is_not_offered_once_the_bag_is_worn_or_carried()
        {
            var items = RealItems();
            // Carried (bought from her, not yet worn).
            var log = Fresh(out _); log.Items = items; var q = log.Def(Wallet);
            Assert.AreEqual(QuestStatus.Available, log.Status(q, "zone.oakhaven"));
            Inventory.Add(log.Progress, items, WalletBag, 1);
            Assert.AreEqual(QuestStatus.Unavailable, log.Status(q, "zone.oakhaven"));
            Assert.IsFalse(log.For(Maud, "zone.oakhaven", 1).Exists(e => e.quest.id == Wallet)); Assert.AreEqual(' ', log.Marker(Maud, "zone.oakhaven", 1, out _));
            Assert.IsFalse(log.Accept(q, "zone.oakhaven"));
            // Worn.
            Assert.IsTrue(Inventory.Wear(log.Progress, items, log.Progress.bag.FindIndex(x => x.item == WalletBag), out _));
            Assert.AreEqual(0, Inventory.Count(log.Progress, WalletBag));
            Assert.AreEqual(QuestStatus.Unavailable, log.Status(q, "zone.oakhaven")); Assert.IsFalse(log.Accept(q, "zone.oakhaven"));
            // Another character, without one, is offered it.
            var other = Fresh(out _); other.Items = items; Assert.AreEqual(QuestStatus.Available, other.Status(q, "zone.oakhaven"));
        }

        [Test] public void TurnIn_with_the_bag_already_worn_pays_gold()
        {
            var log = WithWalletQuest(out var said); var p = log.Progress; var q = log.Def(Wallet);
            // Taken on, then the wallet bought and worn before the pelts came in.
            Inventory.Add(p, log.Items, WalletBag, 1); Assert.IsTrue(Inventory.Wear(p, log.Items, p.bag.FindIndex(x => x.item == WalletBag), out _));
            Assert.AreEqual(QuestStatus.Active, log.Status(q, "zone.oakhaven"), "A quest already taken on stays.");
            Inventory.Add(p, log.Items, Pelt, 3); Assert.IsTrue(log.TalkTo(Maud));
            int gold = p.gold, pouches = p.pouches.Count, slots = p.bag.Count;
            Assert.IsTrue(log.TurnIn(q, out _));
            Assert.AreEqual(gold + log.Items.Get(WalletBag).value, p.gold, "The wallet's worth in gold instead.");
            Assert.AreEqual(0, Inventory.Count(p, WalletBag), "No second wallet."); Assert.AreEqual(pouches, p.pouches.Count); Assert.AreEqual(slots, p.bag.Count);
            Assert.IsTrue(said.Contains(Maud + ": " + QuestLog.HaveOneLine)); Assert.IsTrue(log.IsDone(Wallet));
        }

        [Test] public void TurnIn_with_full_bags_is_refused_and_nothing_is_lost()
        {
            var log = WithWalletQuest(out var said); var p = log.Progress; var q = log.Def(Wallet);
            Inventory.Add(p, log.Items, Pelt, 3); Assert.IsTrue(log.TalkTo(Maud));
            for (int i = 0; i < Inventory.BagSize; i++) if (p.bag[i].Empty) p.bag[i] = new ItemStack { item = "potion.minor", count = 1 };
            Assert.AreEqual(0, Inventory.FreeSlots(p));
            int xp = p.experience, gold = p.gold;
            Assert.IsFalse(log.TurnIn(q, out _));
            Assert.IsTrue(said.Contains(QuestLog.MakeRoomLine)); Assert.AreEqual("Make room in your bags first.", QuestLog.MakeRoomLine);
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(q, "zone.oakhaven"), "Still waiting, the pelts still counted as handed over.");
            Assert.AreEqual(xp, p.experience); Assert.AreEqual(gold, p.gold); Assert.AreEqual(0, log.Standing("oakhaven")); Assert.IsFalse(log.IsDone(Wallet));
            // A slot freed, it goes through.
            p.bag[3] = new ItemStack();
            Assert.IsTrue(log.TurnIn(q, out _)); Assert.AreEqual(WalletBag, p.bag[3].item); Assert.AreEqual(xp + 20, p.experience);
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
