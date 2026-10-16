using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The capital's halls (CAPITAL_HUB.md step 4; CapitalHalls.cs): the hall roles read from a resident's role; Aethelgard's audience
    /// picks its lines by your level, your deeds and the Council's standing, never names what comes after Book 1's time and labels every
    /// line, and no blow can touch him; the Council's errands are real, repeatable once a day, and refusing one costs standing once a
    /// day; the Archive lists the pages found, counts the lost, and gives clues only of finds not found and not shut; a guild's window
    /// lists its members, those about first; a paid talent reset refunds every point and takes the coin, its price rising.
    /// </summary>
    public class CapitalHallTests
    {
        static List<string> Texts(string folder)
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", folder);
            return Directory.GetFiles(dir, "*.json").OrderBy(p => p, System.StringComparer.Ordinal).Select(File.ReadAllText).ToList();
        }

        [Test] public void Hall_roles_are_read_from_the_residents_role()
        {
            Assert.IsTrue(HallRoles.Parse("guildmaster:lantern-watch", out var kind, out var arg)); Assert.AreEqual(HallRoles.Guildmaster, kind); Assert.AreEqual("lantern-watch", arg);
            Assert.IsTrue(HallRoles.Parse("trainer", out kind, out arg)); Assert.AreEqual(HallRoles.Trainer, kind); Assert.AreEqual("", arg);
            Assert.IsTrue(HallRoles.Parse("profession-trainer:mining", out kind, out arg)); Assert.AreEqual("mining", arg);
            Assert.IsFalse(HallRoles.Parse("blacksmith", out _, out _)); Assert.IsFalse(HallRoles.Parse(null, out _, out _));
            foreach (var g in SimGuilds.All) Assert.AreEqual(g.name, GuildHalls.FromSlug(GuildHalls.Slug(g.name)), g.name + " round-trips through its slug.");
            Assert.AreEqual("lantern-watch", GuildHalls.Slug("The Lantern Watch")); Assert.AreEqual("oak-and-ember", GuildHalls.Slug("Oak and Ember"));
            Assert.AreEqual("The Lantern Watch", GuildHalls.FromSlug("the-lantern-watch"));
            Assert.AreEqual("hall.scree-hounds", GuildHalls.HallPlace("Scree Hounds"));
            Assert.AreEqual("High Councilor", HallRoles.Title(HallRoles.Councilor)); Assert.AreEqual("Master of The Long Road", HallRoles.Title("guildmaster:long-road"));
            Assert.IsNull(HallRoles.Title("farmer"));
        }

        static CouncilAudience.Context Ctx(int level, int standing = CouncilAudience.StartStanding, int quests = 0, int zones = 1, bool ledger = false)
        { return new CouncilAudience.Context { level = level, standing = standing, questsDone = quests, zonesReached = zones, readLedger = ledger }; }

        [Test] public void Aethelgard_picks_his_lines_by_level_standing_and_deeds()
        {
            string G(CouncilAudience.Context c) { return CouncilAudience.Say(CouncilAudience.Greeting, c).rule; }
            Assert.AreEqual("low", G(Ctx(1)), "A level 1 is a petitioner from the roads.");
            Assert.AreEqual("mid", G(Ctx(7)), "A level 7 is a name heard twice.");
            Assert.AreEqual("high", G(Ctx(12)), "A level 12 is becoming useful.");
            Assert.AreEqual("trusted", G(Ctx(3, 1500)), "Trusted with the Council at level 3.");
            Assert.AreEqual("honoured", G(Ctx(3, 3500)), "Honoured with the Council.");
            Assert.AreEqual("distrusted", G(Ctx(12, -100)), "Below Neutral, whatever the level.");
            Assert.AreNotEqual(CouncilAudience.Say(CouncilAudience.Greeting, Ctx(1)).text, CouncilAudience.Say(CouncilAudience.Greeting, Ctx(12)).text, "A level 1 and a level 12 hear different words.");
            Assert.AreEqual("few", CouncilAudience.Say(CouncilAudience.Deeds, Ctx(5, quests: 2)).rule);
            Assert.AreEqual("some", CouncilAudience.Say(CouncilAudience.Deeds, Ctx(5, quests: 8, zones: 3)).rule);
            StringAssert.Contains("8 tasks", CouncilAudience.Say(CouncilAudience.Deeds, Ctx(5, quests: 8, zones: 3)).text);
            Assert.AreEqual("many", CouncilAudience.Say(CouncilAudience.Deeds, Ctx(5, quests: 25, zones: 5)).rule);
            Assert.AreEqual("travelled", CouncilAudience.Say(CouncilAudience.Grey, Ctx(5, zones: 4)).rule);
            Assert.AreEqual("untravelled", CouncilAudience.Say(CouncilAudience.Grey, Ctx(5, zones: 2)).rule);
            Assert.AreEqual("ledger", CouncilAudience.Say(CouncilAudience.Bureau, Ctx(5, ledger: true)).rule, "He knows when you have read the Bureau's ledger.");
            Assert.AreEqual("distrusted", CouncilAudience.Say(CouncilAudience.Errands, Ctx(5, -10)).rule);
            // From the progress: provinces from the places explored, deeds from the quests done, standing from the log.
            var p = EncounterSession.FreshProgress(); p.explored.Add("zone.oakhaven|The Green"); p.explored.Add("zone.khaven|The Gate"); p.explored.Add("lore|met|zone.peaks|X"); p.explored.Add("hall|told|x");
            p.questsDone.Add("a"); p.questsDone.Add("b");
            var c = CouncilAudience.Of(p, null, "zone.crown");
            Assert.AreEqual(3, c.zonesReached, "Oakhaven, Khaven and the Crown he stands in; the book's and the halls' keys are not places.");
            Assert.AreEqual(2, c.questsDone); Assert.AreEqual(CouncilAudience.StartStanding, c.standing);
        }

        [Test] public void Aethelgard_never_names_what_comes_after_and_every_line_says_its_lore_status()
        {
            var forbidden = new[] { "Spoke", "Prism", "Kaelen", "Spire", "machine", "engine", "golem" };
            foreach (var level in new[] { 1, 6, 12 })
                foreach (var standing in new[] { -500, 200, 1500, 4000 })
                    foreach (var quests in new[] { 0, 6, 30 })
                        foreach (var t in new[] { CouncilAudience.Greeting }.Concat(CouncilAudience.Topics.Select(x => x.topic)))
                        {
                            var line = CouncilAudience.Say(t, Ctx(level, standing, quests, 4, quests > 0));
                            Assert.IsFalse(string.IsNullOrEmpty(line.text), t);
                            foreach (var f in forbidden) StringAssert.DoesNotContain(f.ToLowerInvariant(), line.text.ToLowerInvariant(), t + ": \"" + line.text + "\"");
                            Assert.IsTrue(LoreBook.Labels.Any(l => line.label.StartsWith(l)), t + " is labelled: " + line.label);
                        }
            Assert.IsTrue(HallRoles.Untouchable(HallRoles.Councilor), "Aethelgard is never fought.");
            Assert.IsFalse(HallRoles.Untouchable(HallRoles.Archivist));
        }

        static (QuestLog log, ItemDatabase items) Quests()
        {
            var items = ItemDatabase.Parse(Texts("Items")); var db = QuestDatabase.Parse(Texts("Quests"));
            Assert.IsEmpty(db.CheckItems(items), "The halls' quests name real items.");
            return (new QuestLog(db, EncounterSession.FreshProgress()) { Items = items, Say = (t, s) => { } }, items);
        }

        [Test] public void The_Councils_errands_are_real_repeatable_and_move_its_standing()
        {
            var (log, items) = Quests(); var p = log.Progress;
            Assert.IsTrue(log.Db.Factions.ContainsKey(CouncilAudience.Faction), "halls.json has the Council's court.");
            var errands = log.Db.Ordered.Where(CouncilErrands.IsErrand).ToList();
            Assert.GreaterOrEqual(errands.Count, 2, "Two or three Council errands."); Assert.LessOrEqual(errands.Count, 3);
            foreach (var q in errands)
            {
                Assert.AreEqual(CouncilAudience.Clerk, q.giver, q.id); Assert.AreEqual(CouncilAudience.Clerk, q.turnIn, q.id);
                Assert.IsTrue(string.IsNullOrEmpty(q.zone), q.id + " is offered wherever the clerk is, and on no notice board.");
                Assert.IsTrue(q.rewards.reputation.Any(r => r.faction == CouncilAudience.Faction && r.amount > 0), q.id + " raises the Council's standing.");
                Assert.LessOrEqual(q.level, 10, q.id + " is a low or mid level errand.");
            }
            int start = log.Standing(CouncilAudience.Faction);
            Assert.AreEqual(CouncilAudience.StartStanding, start);
            // Refused: down once, and only once that day.
            var tithe = log.Def("errand.council.tithe"); Assert.NotNull(tithe);
            Assert.IsTrue(CouncilErrands.Declined(log, tithe)); Assert.AreEqual(start - CouncilErrands.DeclineCost, log.Standing(CouncilAudience.Faction));
            Assert.IsFalse(CouncilErrands.Declined(log, tithe), "A second no the same day costs nothing more.");
            p.days++; Assert.IsTrue(CouncilErrands.Declined(log, tithe), "Another day, another refusal counted.");
            int afterNo = log.Standing(CouncilAudience.Faction);
            // Done: the ore brought, the errand handed in, the standing up, and off the clerk's list until tomorrow.
            Assert.IsTrue(log.Accept(tithe, "zone.crown"));
            Assert.AreEqual(0, Inventory.Add(p, items, "mat.copper_ore", 5));
            Assert.IsTrue(log.TalkTo(CouncilAudience.Clerk), "The clerk takes the ore.");
            Assert.IsTrue(log.TurnIn(tithe, out _));
            Assert.AreEqual(afterNo + tithe.rewards.reputation[0].amount, log.Standing(CouncilAudience.Faction));
            CouncilErrands.MarkDone(p, tithe.id);
            var list = log.For(CouncilAudience.Clerk, "zone.crown", 10); Assert.IsTrue(list.Exists(e => e.quest == tithe), "Repeatable: the quest log offers it again.");
            CouncilErrands.Filter(list, p); Assert.IsFalse(list.Exists(e => e.quest == tithe), "Not again today.");
            p.days++; list = log.For(CouncilAudience.Clerk, "zone.crown", 10); CouncilErrands.Filter(list, p);
            Assert.IsTrue(list.Exists(e => e.quest == tithe), "Tomorrow it is back.");
            Assert.IsFalse(p.explored.Any(k => k.StartsWith("hall|errand|") && !k.EndsWith("|" + (p.days - 1))), "Old days' marks are dropped as new ones are made.");
        }

        [Test] public void Every_guild_has_a_notice_board_of_real_asks()
        {
            var (log, _) = Quests();
            foreach (var (name, _) in SimGuilds.All)
            {
                var asks = GuildHalls.Asks(log.Db, name);
                Assert.GreaterOrEqual(asks.Count, 2, name + " has asks on its board.");
                foreach (var q in asks) { Assert.AreEqual(GuildHalls.BoardName(name), q.turnIn, q.id); Assert.IsTrue(q.steps.SelectMany(s => s.objectives).All(o => o.type == "kill" || o.type == "bring"), q.id + " is a run or a gathering."); }
            }
        }

        static ZoneSecret Secret(string id, string doc = null, string needs = null, string hint = null)
        { return new ZoneSecret { id = id, name = id, kind = "note", document = doc, needs = needs, hint = hint, clue = hint != null ? "Clue of " + id : null }; }

        [Test] public void The_Archive_lists_found_pages_and_tells_only_of_finds_not_found_and_not_shut()
        {
            var db = QuestDatabase.Parse(new[] { "{\"documents\":[{\"id\":\"doc.a\",\"title\":\"Page A\",\"text\":\"a\",\"kind\":\"lore\"},{\"id\":\"doc.b\",\"title\":\"Page B\",\"text\":\"b\",\"kind\":\"lore\"},{\"id\":\"doc.c\",\"title\":\"Loose letter\",\"text\":\"c\"}]}" });
            var z = new ZoneDefinition { id = "zone.archivetest", displayName = "Testmoor",
                secrets = new[] { Secret("secret.archivetest.a", "doc.a", hint: "hint a"), Secret("secret.archivetest.b", "doc.b", hint: "hint b"),
                    Secret("secret.archivetest.chest", needs: "secret.archivetest.key", hint: "hint chest"), Secret("secret.archivetest.key", hint: "hint key") } };
            var zones = new List<ZoneDefinition> { z };
            var p = EncounterSession.FreshProgress(); var log = new DiscoveryLog(p, null, db);
            p.discoveries.Add("secret.archivetest.a"); p.documents.Add("doc.a"); p.documents.Add("doc.c");
            var shelves = ArchiveRoom.Shelves(db, zones, p, log);
            var shelf = shelves.First(s => s.zoneId == z.id);
            CollectionAssert.AreEqual(new[] { "doc.a" }, shelf.pages.Select(d => d.id), "The page found at Testmoor's find is on its shelf.");
            Assert.AreEqual(1, shelf.pagesLost, "Page B is still lost there.");
            Assert.AreEqual(3, shelf.findsHidden);
            Assert.AreEqual("Clue of secret.archivetest.b", shelf.clue, "The clue is of a find that can be found now.");
            Assert.IsTrue(shelves.Any(s => s.zoneId == ArchiveRoom.Elsewhere && s.pages.Any(d => d.id == "doc.c")), "A page of no zone is shelved Elsewhere.");
            // The trade: each found page once, for a word of a find not found and not shut, never told twice.
            Assert.IsFalse(ArchiveRoom.CanCopy(p, "doc.b"), "A page not found cannot be lent.");
            var word = ArchiveRoom.Copy(p, "doc.a", zones, log, out _); StringAssert.Contains("hint b", word); StringAssert.Contains("Testmoor", word);
            Assert.IsNull(ArchiveRoom.Copy(p, "doc.a", zones, log, out var why), "Copied once only."); Assert.AreEqual(ArchiveRoom.CopiedLine, why);
            word = ArchiveRoom.Copy(p, "doc.c", zones, log, out _);
            StringAssert.Contains("hint key", word, "The chest is shut until its key is found: its key is told of instead.");
            Assert.IsNull(ArchiveRoom.NextClue(zones, p, log).secret, "Nothing left that can be found now and was not told.");
            p.discoveries.Add("secret.archivetest.key");
            Assert.AreEqual("secret.archivetest.chest", ArchiveRoom.NextClue(zones, p, log).secret.id, "With the key found, the chest can be told of.");
            CollectionAssert.AreEqual(new[] { "hint b" }, ArchiveRoom.Shelves(db, zones, p, log).First(s => s.zoneId == z.id).told, "The words bought stay on the shelf while their finds are still to find.");
        }

        [Test] public void A_guild_window_lists_its_members_those_about_first()
        {
            var w = new WorldSave();
            SimAdventurer Sim(string id, string guild, int level, float from, float hours) { var s = new SimAdventurer { id = id, name = "Sim " + id, guild = guild, level = level, onlineFrom = from, onlineHours = hours, classId = "class.warrior" }; w.sims.Add(s); return s; }
            Sim("a", "The Lantern Watch", 5, 8, 8); Sim("b", "The Lantern Watch", 9, 20, 2); Sim("c", "The Lantern Watch", 3, 9, 6); Sim("d", "Scree Hounds", 12, 0, 24); Sim("e", "", 7, 0, 24);
            var roster = GuildHalls.Roster(w, "The Lantern Watch", 10);
            CollectionAssert.AreEqual(new[] { "a", "c", "b" }, roster.Select(m => m.sim.id), "Members only: those about at ten first, by level, then the rest.");
            Assert.IsTrue(roster.First(m => m.sim.id == "b").master, "The highest level is the guild master.");
            Assert.IsFalse(roster.First(m => m.sim.id == "b").online);
            roster = GuildHalls.Roster(w, "The Lantern Watch", 10, id => id == "b");
            Assert.IsTrue(roster.First(m => m.sim.id == "b").online, "One in your party is about.");
            Assert.IsEmpty(GuildHalls.Roster(w, "Oak and Ember", 10));
        }

        [Test] public void A_paid_talent_reset_refunds_every_point_and_takes_the_coin()
        {
            var p = EncounterSession.FreshProgress(); p.experience = EncounterProgress.XpForLevel(5);
            Assert.IsFalse(TrainerRules.PaidReset(p, out _, out var why)); Assert.AreEqual(TrainerRules.NothingLine, why);
            p.talents.Add(new TalentRank { id = "t1", rank = 2 }); p.talents.Add(new TalentRank { id = "t2", rank = 1 });
            int price = TrainerRules.ResetPrice(p); Assert.AreEqual(35, price, "10 crowns and 5 a level at level 5.");
            p.gold = price - 1; Assert.IsFalse(TrainerRules.PaidReset(p, out _, out why), "Short of coin: " + why);
            Assert.AreEqual(3, TalentTree.Spent(p), "Nothing changed."); Assert.AreEqual(price - 1, p.gold);
            p.gold = 100; Assert.IsTrue(TrainerRules.PaidReset(p, out int paid, out _));
            Assert.AreEqual(price, paid); Assert.AreEqual(100 - price, p.gold); Assert.AreEqual(0, TalentTree.Spent(p), "Every point back.");
            Assert.AreEqual(1, TrainerRules.Resets(p)); Assert.AreEqual(price * 2, TrainerRules.ResetPrice(p), "The next costs more.");
            Assert.AreEqual(TrainerRules.ResetPrice(5, 0) * 8, TrainerRules.ResetPrice(5, 30), "At most eight times the first.");
        }

        [Test] public void A_trainer_lists_the_class_abilities_by_level()
        {
            var c = new ClassDefinition { unlocks = new[] { new ClassAbilityUnlock { abilityId = "ab.late", level = 6 }, new ClassAbilityUnlock { abilityId = "ab.first", level = 1 }, new ClassAbilityUnlock { abilityId = "ab.gift", level = 3, talentId = "t.gift" } } };
            var catalog = new[] { new Crulanda.Abilities.AbilityDefinition { id = "ab.first", name = "First" }, new Crulanda.Abilities.AbilityDefinition { id = "ab.late", name = "Late" } };
            var list = TrainerRules.Abilities(c, catalog);
            CollectionAssert.AreEqual(new[] { 1, 3, 6 }, list.Select(a => a.level));
            Assert.AreEqual("First", list[0].name); Assert.AreEqual("ab.gift", list[1].name, "An ability not in the catalogue goes by its id."); Assert.AreEqual("t.gift", list[1].talent);
        }
    }
}
