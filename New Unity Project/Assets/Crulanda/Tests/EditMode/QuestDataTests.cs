using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The shipped quests read against the shipped items: what they ask you to bring and the bags they give are real.</summary>
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
                ("npc.leatherworker.scrip", "The Cook's Scrip", 2, "hide.coney", 5, "bag.larder_scrip", 25),
                ("npc.leatherworker.poke", "Ore Wants a Stout Bag", 2, "hide.boar", 3, "bag.ore_poke", 30) };
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
    }
}
