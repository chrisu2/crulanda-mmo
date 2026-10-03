using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The notice boards (2026-10-03; Bounties.cs, Quests/bounties.json): every zone has a pool of postings and one rare one; the
    /// board draws three a day, the same three on a reload, different ones another day; the rare posting turns up about one slot
    /// in sixty-seven; a bounty handed in leaves the board for the day and is never "done" for good; every kill target names a
    /// camp of its zone and every bring item is a material no vendor sells; the rare postings pay the Aether-Geode shard and the
    /// tier above's bars; the tempered recipes need the shard and keep the recipe value rule.
    /// </summary>
    public class BountyTests
    {
        static List<string> Texts(string folder)
        {
            var dir = Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", folder);
            return Directory.GetFiles(dir, "*.json").OrderBy(p => p, System.StringComparer.Ordinal).Select(File.ReadAllText).ToList();
        }
        static readonly string[] Zones = { "zone.oakhaven", "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant" };
        static (QuestLog log, Bounties boards, ItemDatabase items) Fresh()
        {
            var items = ItemDatabase.Parse(Texts("Items")); var db = QuestDatabase.Parse(Texts("Quests"));
            var log = new QuestLog(db, EncounterSession.FreshProgress()) { Items = items, Say = (t, s) => { } };
            return (log, new Bounties(log), items);
        }

        [Test] public void Every_zone_has_a_pool_and_one_rare_posting_that_name_real_things()
        {
            var (log, boards, items) = Fresh();
            var zoneFiles = Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"), "*.json");
            var sold = new HashSet<string>(items.Vendors.SelectMany(v => v.items ?? new string[0]));
            foreach (var zone in Zones)
            {
                boards.Pool(zone, out var common, out var rare);
                Assert.GreaterOrEqual(common.Count, 3, zone + " has at least three common postings.");
                Assert.NotNull(rare, zone + " has a rare posting."); Assert.IsTrue(rare.rare);
                var z = JsonUtility.FromJson<Crulanda.World.ZoneDefinition>(File.ReadAllText(zoneFiles.First(f => Path.GetFileNameWithoutExtension(f) == zone.Replace("zone.", ""))));
                var tags = new HashSet<string>(z.camps.Select(c => string.IsNullOrEmpty(c.tag) ? c.look : c.tag));
                foreach (var q in common.Append(rare))
                {
                    Assert.AreEqual("board", q.giver, q.id); Assert.AreEqual("board", q.turnIn, q.id); Assert.AreEqual("bounty", q.kind, q.id);
                    Assert.IsFalse(string.IsNullOrEmpty(q.canonStatus), q.id + " says its lore status.");
                    Assert.Greater(q.rewards.gold, 0, q.id + " pays silver crowns.");
                    foreach (var o in q.steps.SelectMany(s => s.objectives))
                    {
                        if (o.type == "kill")
                        {
                            string tag = o.target.Split('.')[1];
                            if (tag == "courier") Assert.IsTrue(q.rare, q.id + ": only the rare posting hunts the courier");
                            else Assert.IsTrue(tags.Contains(tag), q.id + ": a camp of " + zone + " is tagged '" + tag + "'");
                        }
                        if (o.type == "bring") { var d = items.Get(o.item); Assert.NotNull(d, q.id + " brings " + o.item); Assert.AreEqual("material", d.kind, q.id); Assert.IsFalse(sold.Contains(o.item), q.id + ": " + o.item + " is gathered or hunted, never bought (no coin loop)."); }
                        if (o.type == "visit") Assert.IsTrue(o.night, q.id + ": a visit posting is a walk after dark.");
                    }
                }
                CollectionAssert.Contains(rare.rewards.bagItems, "mat.geode_shard", zone + "'s rare posting pays the shard.");
                Assert.IsTrue(rare.rewards.bagItems.Any(b => b.EndsWith("_bar")), zone + "'s rare posting pays bars.");
                Assert.AreEqual("mob.courier." + zone.Replace("zone.", "") + ".*", rare.steps[0].objectives[0].target);
            }
            Assert.IsNull(items.Vendors.FirstOrDefault(v => (v.items ?? new string[0]).Contains("mat.geode_shard")), "Nobody sells the shard.");
        }

        [Test] public void The_board_draws_three_a_day_the_same_on_a_reload_and_rarely_the_rare_one()
        {
            var (log, boards, _) = Fresh();
            var first = boards.Board("zone.khaven");
            Assert.AreEqual(Bounties.Slots, first.ids.Count); Assert.AreEqual(0, first.day);
            Assert.AreEqual(first.ids.Count, first.ids.Distinct().Count(), "No posting twice.");
            var again = Fresh(); var same = again.boards.Board("zone.khaven");
            CollectionAssert.AreEqual(first.ids, same.ids, "A reload (the same day) shows the same notices.");
            int rareSlots = 0, slots = 0; var seen = new HashSet<string>();
            for (int day = 0; day < 2000; day++)
            {
                log.Progress.days = day; var b = boards.Board("zone.khaven");
                Assert.AreEqual(day, b.day); slots += b.ids.Count;
                foreach (var id in b.ids) { seen.Add(id); if (log.Def(id).rare) rareSlots++; }
            }
            float rate = rareSlots / (float)slots;
            Assert.That(rate, Is.InRange(.008f, .025f), "The rare posting fills about one slot in sixty-seven (1-2%): " + rate);
            Assert.GreaterOrEqual(seen.Count, 5, "Over many days every posting comes up.");
        }

        [Test] public void A_bounty_is_paid_leaves_the_board_for_the_day_and_comes_back_another_day()
        {
            var (log, boards, _) = Fresh();
            log.Progress.days = 3; var b = boards.Board("zone.khaven");
            var q = b.ids.Select(log.Def).First(d => !d.rare);
            var entries = boards.Entries("zone.khaven", 5);
            Assert.IsTrue(entries.Any(e => e.quest == q && e.status == QuestStatus.Available));
            Assert.IsTrue(log.Accept(q, "zone.khaven"));
            Assert.AreEqual(QuestStatus.Active, boards.Entries("zone.khaven", 5).First(e => e.quest == q).status);
            // Done: the step's objectives all met (the log is told as the game would).
            var s = log.State(q.id); s.step = q.steps.Length;
            Assert.AreEqual(QuestStatus.ReadyToTurnIn, boards.Entries("zone.khaven", 5)[0].status, "Ready to hand in: first on the board.");
            int crowns = log.Progress.gold;
            Assert.IsTrue(log.TurnIn(q, out _)); boards.Finished("zone.khaven", q.id);
            Assert.AreEqual(crowns + q.rewards.gold, log.Progress.gold, "Paid in silver crowns.");
            Assert.IsFalse(log.IsDone(q.id), "A bounty is never done for good.");
            Assert.IsFalse(boards.Entries("zone.khaven", 5).Any(e => e.quest == q), "Off the board for today.");
            Assert.AreEqual(QuestStatus.Available, log.Status(q, "zone.khaven"), "But it can be taken again.");
            log.Progress.days = 4; boards.Board("zone.khaven");
            Assert.IsTrue(boards.Entries("zone.khaven", 5).All(e => e.status != QuestStatus.ReadyToTurnIn));
            // A posting taken on and not done stays up when the day turns.
            var keep = boards.Board("zone.khaven").ids.Select(log.Def).First(d => !d.rare); log.Accept(keep, "zone.khaven");
            log.Progress.days = 5; Assert.Contains(keep.id, boards.Board("zone.khaven").ids, "Still yours the next day.");
            Assert.IsTrue(Bounties.DayTurned(5.9f, 6.1f)); Assert.IsFalse(Bounties.DayTurned(6.1f, 6.2f)); Assert.IsFalse(Bounties.DayTurned(23.9f, .1f));
        }

        [Test] public void The_tempered_recipes_need_the_shard_and_keep_the_value_rule()
        {
            var items = ItemDatabase.Parse(Texts("Items"));
            var db = ProfessionDatabase.Parse(Texts("Professions").Concat(new string[0]), items);
            var tempered = db.Recipes.Where(r => r.id.StartsWith("recipe.tempered_")).ToList();
            Assert.AreEqual(5, tempered.Count, "One resonance-tempered weapon a tier.");
            foreach (var r in tempered)
            {
                Assert.IsTrue(r.inputs.Any(i => i.item == "mat.geode_shard"), r.id + " needs the shard.");
                var made = items.Get(r.output); Assert.NotNull(made, r.id); Assert.AreEqual(3, made.quality, r.id + " makes rare gear."); Assert.AreEqual("mainhand", made.slot, r.id);
                int cost = r.inputs.Sum(i => items.Get(i.item).value * Mathf.Max(1, i.count));
                Assert.LessOrEqual(made.value, 1.5f * cost, r.id + " keeps the recipe value rule.");
            }
            var shard = items.Get("mat.geode_shard"); Assert.AreEqual("material", shard.kind); Assert.AreEqual("CANON-EXPANDED", shard.canonStatus);
        }
    }
}
