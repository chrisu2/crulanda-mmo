using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The shipped quests read against the shipped items: what they ask you to bring and the bags they give are real; the Sealed
    /// Adit's seven quests name the people, bosses and things that are really there, and play through.</summary>
    public class QuestDataTests
    {
        static List<string> Texts(string folder)
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", folder);
            return Directory.GetFiles(dir, "*.json").OrderBy(p => p, System.StringComparer.Ordinal).Select(File.ReadAllText).ToList();
        }

        /// <summary>
        /// Every bring objective names a real item, every bag reward and unlessWorn a trade bag (QuestDatabase.CheckItems). Maud's four
        /// bag quests (ADDENDUM D.4) each bring one kind of hide or pelt that some beast drops, and give the bag they are not offered
        /// beside; between them they give each bag she sells exactly once.
        /// </summary>
        [Test] public void Bring_and_bag_rewards_name_real_items()
        {
            var items = ItemDatabase.Parse(Texts("Items")); var db = QuestDatabase.Parse(Texts("Quests"));
            CollectionAssert.IsEmpty(db.CheckItems(items), "Every bring item, bag reward and unlessWorn names a real item (and the last two a trade bag).");
            var bagQuests = new[] {
                ("npc.leatherworker.wallet", "A Wallet for Simples", 1, "junk.wolf_pelt", 3, "bag.simples_wallet", 20),
                ("npc.leatherworker.sling", "A Strap for the Woodyard", 1, "hide.hill_deer", 3, "bag.log_sling", 25),
                ("npc.leatherworker.scrip", "The Cook's Scrip", 4, "hide.coney", 5, "bag.larder_scrip", 25),
                ("npc.leatherworker.poke", "Ore Wants a Stout Bag", 4, "hide.boar", 3, "bag.ore_poke", 30) };   // levels 4 since round 29 (Oakhaven 1-5)
            var dropped = new HashSet<string>(items.Loot.SelectMany(t => t.entries).Where(e => e.chance > 0).Select(e => e.item));
            foreach (var (id, title, level, hide, count, bag, xp) in bagQuests)
            {
                Assert.IsTrue(db.Quests.ContainsKey(id), id); var q = db.Quests[id];
                Assert.AreEqual(title, q.title, id); Assert.AreEqual(level, q.level, id); Assert.AreEqual("GAME-ONLY", q.canonStatus, id);
                Assert.AreEqual("Maud Tanner", q.giver, id); Assert.AreEqual("Maud Tanner", q.turnIn, id); Assert.AreEqual("zone.oakhaven", q.zone, id);
                Assert.AreEqual(1, q.steps.Length, id); Assert.AreEqual(1, q.steps[0].objectives.Length, id);
                var o = q.steps[0].objectives[0];
                Assert.AreEqual("bring", o.type, id); Assert.AreEqual("Maud Tanner", o.target, id); Assert.AreEqual(hide, o.item, id); Assert.AreEqual(count, o.count, id);
                Assert.IsTrue(Inventory.IsHide(items.Get(hide)), id + ": " + hide + " is a hide (a material the leatherworker works).");
                Assert.IsTrue(dropped.Contains(hide), id + ": some beast drops " + hide + ", so it can be got.");
                CollectionAssert.AreEqual(new[] { bag }, q.rewards.bagItems, id); Assert.AreEqual(bag, q.unlessWorn, id + " is not offered beside its own bag.");
                Assert.AreEqual("bag", items.Get(bag).kind, id);
                Assert.AreEqual(xp, q.rewards.xp, id); Assert.AreEqual(0, q.rewards.gold, id + ": no coin; the bag is the reward.");
                Assert.AreEqual(1, q.rewards.reputation.Length, id); Assert.AreEqual("oakhaven", q.rewards.reputation[0].faction, id); Assert.AreEqual(75, q.rewards.reputation[0].amount, id);
                foreach (var text in new[] { q.summary, q.offer, q.progress, q.complete, o.text, o.say })
                {
                    Assert.IsFalse(string.IsNullOrEmpty(text), id + " has all its lines.");
                    Assert.IsFalse(text.Contains("—"), id + ": no em-dashes in the village's voice.");
                }
            }
            CollectionAssert.AreEquivalent(items.StockFor("Maud Tanner", "leatherworker", 2), bagQuests.Select(b => b.Item6), "Each bag she sells is earned by exactly one quest.");
        }
        static ZoneDefinition Zone(string file) { return JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones", file + ".json"))); }
        /// <summary>The Sealed Adit's quests (DUNGEON_DESIGN.md 6, Quests/adit.json): id, title, giver, the zone they are given in, level.</summary>
        static readonly (string id, string title, string giver, string zone, int level)[] AditQuests = {
            ("side.adit.toll", "Under the Toll", "Yara Quell", "zone.peaks", 11), ("side.adit.pressed", "The Pressed", "Pib", "zone.peaks", 11),
            ("side.adit.echoes", "Echoes in the Stone", "Brother Cael", "zone.peaks", 11), ("side.adit.embers", "Embers Below", "Tamsin Rook", "zone.khaven", 11),
            ("side.adit.grey", "What the Grey Takes", "Lisle Tamber", "zone.peaks", 11), ("side.adit.dead_line", "The Dead Line", "Yara Quell", "zone.peaks", 12),
            ("side.adit.letter", "A Letter Under Seal", "Yara Quell", "zone.peaks", 12), ("side.adit.weaver", "The Weaver's Lock", "Pib", "zone.peaks", 12) };

        /// <summary>
        /// The Sealed Adit's seven quests are given outside, in the Peaks and Khaven, by people who live there, from level 10; every kill
        /// (and every "collect from a kill") names one of the Adit's elite camps; the cages and the echo-jars are the ones its builder
        /// makes (ZoneBuilder.AditCage, AditEchoJar); The Dead Line follows Under the Toll and the letter follows The Dead Line; The
        /// Dead Line's three rewards are rares in the loot file.
        /// </summary>
        [Test] public void The_Sealed_Adit_quests_are_given_outside_and_name_what_is_inside()
        {
            var db = QuestDatabase.Parse(Texts("Quests"));
            var zones = new Dictionary<string, ZoneDefinition> { { "zone.peaks", Zone("peaks") }, { "zone.khaven", Zone("khaven") } };
            var adit = Zone("adit");
            CollectionAssert.AreEquivalent(AditQuests.Select(a => a.id), db.Ordered.Where(q => q.id.StartsWith("side.adit.")).Select(q => q.id), "Eight quests for the Adit.");
            foreach (var (id, title, giver, zone, level) in AditQuests)
            {
                var q = db.Quests[id];
                Assert.AreEqual(title, q.title, id); Assert.AreEqual("side", q.kind, id); Assert.AreEqual(giver, q.giver, id); Assert.AreEqual(zone, q.zone, id);
                Assert.AreEqual(level, q.level, id); Assert.AreEqual(10, q.minLevel, id + " waits for level 10 (the Adit is 10-12).");
                foreach (var who in new[] { q.giver, q.turnIn }) Assert.IsTrue(zones[q.zone].life.residents.Any(r => r.name == who), id + ": " + who + " lives in " + q.zone + ".");
                Assert.IsFalse(string.IsNullOrEmpty(q.canonStatus), id + " says its lore status.");
                foreach (var text in new[] { q.summary, q.offer, q.progress, q.complete })
                {
                    Assert.IsFalse(string.IsNullOrEmpty(text), id + " has all its lines.");
                    Assert.IsFalse(text.Contains("—"), id + ": no em-dashes.");
                }
                foreach (var o in q.steps.SelectMany(st => st.objectives))
                {
                    Assert.IsFalse(string.IsNullOrEmpty(o.text), id + ": every objective has its tracker line.");
                    bool fromKill = o.type == "collect" && o.target.StartsWith("kill:");
                    if (o.type == "kill" || fromKill)
                    {
                        string target = fromKill ? o.target.Substring(5) : o.target;
                        Assert.AreEqual("zone.adit", o.zone, id + ": " + target + " is in the Adit.");
                        Assert.IsTrue(adit.camps.Any(c => c.elite && QuestLog.Matches(target, "mob." + c.tag + ".adit.0.0")), id + ": " + target + " is one of the Adit's bosses.");
                    }
                    else if (o.type == "interact") { Assert.AreEqual(ZoneBuilder.AditCage, o.target, id); Assert.AreEqual(5, o.count, id + ": the five cages."); }
                    else if (o.type == "collect") { Assert.AreEqual(ZoneBuilder.AditEchoJar, o.target, id); Assert.AreEqual(6, o.count, id + ": six jars of the seven."); }
                    else if (o.type == "flag") Assert.AreEqual("key:" + EncounterSession.WeaverLock, o.target, id + ": the Weaver's lock, matched for good.");
                    else if (o.type == "talk" || o.type == "deliver") Assert.IsTrue(zones[o.zone].life.residents.Any(r => r.name == o.target), id + ": " + o.target + " lives in " + o.zone + ".");
                    else Assert.Fail(id + ": an objective of type " + o.type + " was not expected.");
                }
            }
            CollectionAssert.AreEqual(new[] { "side.adit.toll" }, db.Quests["side.adit.dead_line"].requires, "The Dead Line follows Under the Toll.");
            CollectionAssert.AreEqual(new[] { "side.adit.dead_line" }, db.Quests["side.adit.letter"].requires, "The letter follows The Dead Line.");
            CollectionAssert.AreEqual(new[] { EncounterSession.PressedQuest }, db.Quests[EncounterSession.WeaverQuest].requires, "The Weaver's Lock follows The Pressed.");
            Assert.AreEqual("Lisle Tamber", db.Quests["side.adit.letter"].turnIn, "The Salt-Mender reads the letter.");
            var items = LootTestData.Items(); var loot = LootTestData.Loot(items);
            var rewards = loot.GearOrder.Where(g => g.source == "quest:side.adit.dead_line").Select(g => items.Get(g.id)).ToList();
            Assert.AreEqual(3, rewards.Count, "The Dead Line: a choice of three rare pieces.");
            Assert.IsTrue(rewards.All(d => d.quality == 3 && d.level == 12), "Rares at 12.");
        }

        /// <summary>
        /// The seven quests in order on one character, as the session drives the log: kills by enemy id (the Adit's camps' ids, mob.tag.adit.c.n),
        /// the item a boss drops for a "collect from a kill", the cages opened, the jars taken, hand-ins by talking, turn-ins with their
        /// pages; then The Dead Line and the letter open up and carry Danner's letter to the Salt-Mender.
        /// </summary>
        [Test] public void The_Sealed_Adit_quests_play_through_from_the_pass_to_the_letter()
        {
            var log = new QuestLog(QuestDatabase.Parse(Texts("Quests")), EncounterSession.FreshProgress()) { Say = (t, s) => { } };
            var pages = new List<string>(); log.Revealed = id => pages.Add(id);
            log.Progress.experience = EncounterProgress.XpForLevel(9);
            Assert.IsFalse(log.For("Yara Quell", "zone.peaks", log.Progress.Level).Any(o => o.quest.id.StartsWith("side.adit.")), "Nothing for the Adit at level 9.");
            Assert.IsFalse(log.Accept(log.Def("side.adit.toll"), "zone.peaks"), "And it can't be taken.");
            log.Progress.experience = EncounterProgress.XpForLevel(11);
            Assert.IsFalse(log.Accept(log.Def("side.adit.embers"), "zone.peaks"), "Embers Below is Khaven's.");
            foreach (var (id, _, _, zone, _) in AditQuests.Where(a => a.id != "side.adit.dead_line" && a.id != "side.adit.letter" && a.id != "side.adit.weaver")) Assert.IsTrue(log.Accept(log.Def(id), zone), id + " is taken.");
            Assert.IsFalse(log.Accept(log.Def("side.adit.dead_line"), "zone.peaks"), "The Dead Line waits for Under the Toll.");
            Assert.IsFalse(log.Accept(log.Def("side.adit.weaver"), "zone.peaks"), "The Weaver's Lock waits for The Pressed.");

            // Under the Toll: Lusk, his book, Yara.
            log.Notify("kill", "mob.gangboss.adit.5.0"); Assert.AreEqual("item.adit_tally_book", log.LootFrom("mob.gangboss.adit.5.0"));
            Assert.Contains("doc.adit.tally_book", pages, "The tally book's page is read as it is taken.");
            Assert.IsTrue(log.TalkTo("Yara Quell")); Assert.IsTrue(log.TurnIn(log.Def("side.adit.toll"), out _));
            // The Pressed: five cages, then Pib.
            for (int k = 0; k < 4; k++) log.Notify("interact", ZoneBuilder.AditCage);
            Assert.AreEqual(QuestStatus.Active, log.Status(log.Def("side.adit.pressed"), "zone.peaks"), "Four cages are not five.");
            log.Notify("interact", ZoneBuilder.AditCage); Assert.IsTrue(log.TalkTo("Pib")); Assert.IsTrue(log.TurnIn(log.Def("side.adit.pressed"), out _));
            // The Weaver's Lock: Mother Quillet walked to the carriage and its lock matched (kept for good, "key:adit.lock"), then Pib.
            Assert.IsTrue(log.Accept(log.Def("side.adit.weaver"), "zone.peaks"));
            log.Reconcile(f => false, null); Assert.AreEqual(QuestStatus.Active, log.Status(log.Def("side.adit.weaver"), "zone.peaks"), "The lock is not matched yet.");
            log.Reconcile(f => f == "key:" + EncounterSession.WeaverLock, null);
            Assert.IsTrue(log.TalkTo("Pib")); Assert.IsTrue(log.TurnIn(log.Def("side.adit.weaver"), out int weaverXp)); Assert.AreEqual(760, weaverXp);
            // Echoes in the Stone: six jars, given to Brother Cael.
            for (int k = 0; k < 7; k++) if (log.Wants("item.adit_echo_jar", ZoneBuilder.AditEchoJar)) log.GiveItem("item.adit_echo_jar");
            Assert.AreEqual(6, log.ItemCount("item.adit_echo_jar"), "Six jars, and the seventh is left.");
            Assert.IsTrue(log.TalkTo("Brother Cael")); Assert.AreEqual(0, log.ItemCount("item.adit_echo_jar"), "All six handed over.");
            Assert.IsTrue(log.TurnIn(log.Def("side.adit.echoes"), out _)); Assert.Contains("doc.adit.echoes", pages);
            // Embers Below and What the Grey Takes: Ysolt's brand to Khaven, the Foreman's shard to the Salt-Mender.
            log.Notify("kill", "mob.ysolt.adit.26.0"); log.LootFrom("mob.ysolt.adit.26.0"); Assert.IsTrue(log.TalkTo("Tamsin Rook")); Assert.IsTrue(log.TurnIn(log.Def("side.adit.embers"), out _));
            log.Notify("kill", "mob.foreman.adit.31.0"); log.LootFrom("mob.foreman.adit.31.0"); Assert.IsTrue(log.TalkTo("Lisle Tamber")); Assert.IsTrue(log.TurnIn(log.Def("side.adit.grey"), out _));
            Assert.AreEqual(250, log.Standing("saltmenders"));

            // The Dead Line: the Quartermaster and the Rail-Captain, and Danner's letter; then the letter to Lisle.
            Assert.IsTrue(log.Accept(log.Def("side.adit.dead_line"), "zone.peaks"));
            Assert.IsFalse(log.Accept(log.Def("side.adit.letter"), "zone.peaks"), "No letter before Danner.");
            log.Notify("kill", "mob.railcaptain.adit.16.0"); Assert.AreEqual("item.adit_sealed_letter", log.LootFrom("mob.railcaptain.adit.16.0"));
            Assert.AreEqual(QuestStatus.Active, log.Status(log.Def("side.adit.dead_line"), "zone.peaks"), "The Quartermaster still stands.");
            log.Notify("kill", "mob.railquartermaster.adit.13.0");
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, log.Status(log.Def("side.adit.dead_line"), "zone.peaks"));
            Assert.IsTrue(log.TurnIn(log.Def("side.adit.dead_line"), out int xp)); Assert.AreEqual(900, xp);
            Assert.AreEqual(1, log.ItemCount("item.adit_sealed_letter"), "The letter stays in the quest bag.");
            Assert.IsTrue(log.Accept(log.Def("side.adit.letter"), "zone.peaks"));
            Assert.IsTrue(log.TalkTo("Lisle Tamber")); Assert.Contains("doc.adit.sealed_letter", pages);
            Assert.AreEqual(0, log.ItemCount("item.adit_sealed_letter"));
            Assert.IsTrue(log.TurnIn(log.Def("side.adit.letter"), out _));
            Assert.IsTrue(AditQuests.All(a => log.IsDone(a.id)), "All eight done.");
            Assert.AreEqual(-500 - 150 - 300, log.Standing("sandthrone"), "The Company likes you less for it.");
        }
    }
}
