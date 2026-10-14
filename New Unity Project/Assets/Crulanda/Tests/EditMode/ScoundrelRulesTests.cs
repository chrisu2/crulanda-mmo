using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Scoundrel's data (Docs/CLASS_SCOUNDREL.md, 2026-10-14): its tree loads the implemented talents, three paths of 17
    /// ranks; every path ability is learnt by its own path's talent, so a build owns only its own (Oct 13: the Cleric's and Shaman's bars
    /// locked nothing); the class and its twelve are in Encounter.asset on Focus, called Nerve; the charm at the merchant's counter.</summary>
    public class ScoundrelRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/scoundrel.json")); } }
        static EncounterContent Content { get { return UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset"); } }

        [Test] public void Tree_loads_exactly_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.scoundrel", Json, 15, ScoundrelKit.ImplementedIds);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(25, count); Assert.AreEqual(ScoundrelKit.ImplementedIds.Length, count);
            CollectionAssert.AreEqual(new[] { "cutthroat", "silver-tongue", "fixer" }, t.Branches.Select(b => b.id).ToArray(), "three paths for now (Chris, 2026-10-13)");
            CollectionAssert.AreEqual(new[] { "dps", "control", "support" }, t.Branches.Select(b => b.role).ToArray());
            foreach (var b in t.Branches) Assert.AreEqual(17, b.nodes.Sum(n => n.max), b.id + ": 17 ranks");
            var only = new List<string>(ScoundrelKit.ImplementedIds); only.Remove("st-glimmer");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.scoundrel", Json, 15, only));
        }
        [Test] public void Every_path_ability_is_learnt_by_its_own_paths_talent()
        {
            var cls = Content.FindClass("class.scoundrel"); Assert.NotNull(cls, "class.scoundrel is a selectable class");
            var t = TalentTree.Parse("class.scoundrel", Json, 15, ScoundrelKit.ImplementedIds);
            foreach (var u in cls.definition.unlocks)
            {
                if (!ScoundrelKit.PathOf.TryGetValue(u.abilityId, out var path)) { Assert.IsTrue(string.IsNullOrEmpty(u.talentId), u.abilityId + " is every path's"); continue; }
                Assert.IsFalse(string.IsNullOrEmpty(u.talentId), u.abilityId + " waits on a talent");
                Assert.AreEqual(path, t.BranchOf(u.talentId).id, u.abilityId + " is learnt in its own path");
                var n = t.Find(u.talentId); Assert.AreEqual("active", n.kind, u.talentId); Assert.AreEqual(1, n.max, u.talentId);
            }
            CollectionAssert.AreEquivalent(new[] { "scoundrel.quick_cut", "scoundrel.jeer" }, cls.definition.unlocks.Where(u => string.IsNullOrEmpty(u.talentId)).Select(u => u.abilityId).ToArray());
        }
        /// <summary>The balance harness's buy (every point in one path at level 15): that build owns its own path's abilities and no other.</summary>
        [Test] public void Each_build_owns_only_its_own_paths_abilities()
        {
            var cls = Content.FindClass("class.scoundrel"); var t = TalentTree.Parse("class.scoundrel", Json, 15, ScoundrelKit.ImplementedIds);
            foreach (var b in t.Branches)
            {
                var p = EncounterSession.FreshProgress("class.scoundrel"); p.experience = EncounterProgress.XpForLevel(15);
                var ids = b.nodes.Select(n => n.id).ToList();
                for (int pass = 0; pass < 40 && t.Available(p) > 0; pass++)
                    foreach (var id in ids) if (t.Propose(p, id, 1, out var next, out _)) p.talents = next;
                Assert.AreEqual(0, t.Available(p), b.id + ": every point spent");
                foreach (var u in cls.definition.unlocks)
                {
                    bool mine = string.IsNullOrEmpty(u.talentId) || ids.Contains(u.talentId);
                    bool owned = string.IsNullOrEmpty(u.talentId) || TalentTree.Rank(p, u.talentId) > 0;
                    Assert.AreEqual(mine, owned, b.id + " / " + u.abilityId);
                }
            }
        }
        [Test] public void Rows_follow_the_calculator()
        {
            var t = TalentTree.Parse("class.scoundrel", Json, 15, ScoundrelKit.ImplementedIds);
            var p = EncounterSession.FreshProgress("class.scoundrel"); p.experience = EncounterProgress.XpForLevel(10);
            Assert.IsFalse(t.Propose(p, "ct-poisoned-wine", 1, out _, out _), "row 1 needs 5 points in the Cutthroat");
            foreach (var id in new[] { "ct-settle", "ct-keen-dirk", "ct-keen-dirk", "ct-keen-dirk", "ct-keen-dirk", "ct-poisoned-wine" })
            { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var why), id + ": " + why); p.talents = next; }
            Assert.IsFalse(t.Propose(p, "ct-mark-debt", 1, out _, out _), "row 2 needs 10");
        }
        [Test] public void The_class_and_its_twelve_are_in_the_content_on_Nerve()
        {
            var content = Content; var cls = content.FindClass("class.scoundrel"); Assert.NotNull(cls);
            Assert.AreEqual("Scoundrel", cls.definition.displayName);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, cls.definition.resource, "Nerve is a Focus pool, as the Rogue's");
            Assert.AreEqual("Nerve", cls.definition.resourceName); Assert.AreEqual("Nerve", EncounterHud.ResourceName(cls.definition));
            Assert.AreEqual(100, cls.definition.maxResource);
            Assert.NotNull(cls.talentTree); StringAssert.Contains("\"id\": \"scoundrel\"", cls.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in ScoundrelKit.BarIds) Assert.IsTrue(ids.Contains(id), id);
            CollectionAssert.AreEqual(ScoundrelKit.BarIds, cls.definition.unlocks.Select(u => u.abilityId).ToArray(), "the unlocks are the bar, in order");
            Assert.AreEqual(content.abilities.Length, ids.Count, "no duplicate ability ids");
            Assert.AreEqual(ActorLook.Ranger, EncounterSession.LookForClass("class.scoundrel"), "in leathers, as the Rogue");
            Assert.IsTrue(Races.Allows(Races.Human, "class.scoundrel")); Assert.IsTrue(Races.Allows(Races.Orc, "class.scoundrel")); Assert.IsNull(Races.Requires("class.scoundrel"), "races open");
            Assert.AreEqual("encounter-scoundrel", EncounterSave.SlotFor("class.scoundrel"), "its own save");
        }
        [Test] public void A_Scoundrel_buys_a_tenth_cheaper_and_sells_a_tenth_dearer()
        {
            var db = ItemDatabase.Parse(Content.itemFiles.Select(f => f.text).ToList());
            var me = EncounterSession.FreshProgress("class.scoundrel"); var other = EncounterSession.FreshProgress("class.warrior");
            Assert.AreEqual(36, Charm.Ask(me, 40)); Assert.AreEqual(40, Charm.Ask(other, 40));
            Assert.AreEqual(11, Charm.Pay(me, 10)); Assert.AreEqual(10, Charm.Pay(other, 10));
            Assert.AreEqual(1, Charm.Ask(me, 1), "never under 1");
            int price = Inventory.Price(db.Get("potion.minor"));
            me.gold = 100; Assert.IsTrue(Inventory.Buy(me, db, "potion.minor", out _)); Assert.AreEqual(100 - Charm.Ask(me, price), me.gold);
            int before = me.gold; Assert.AreEqual(0, Inventory.Add(me, db, "junk.static_glass", 3));
            int i = me.bag.FindIndex(s => s.item == "junk.static_glass");
            Assert.AreEqual(Charm.Pay(me, 3 * db.Get("junk.static_glass").value), Inventory.Sell(me, db, i)); Assert.Greater(me.gold, before);
        }
        [Test] public void A_Scoundrel_carries_one_dirk()
        {
            var content = Content; var items = ItemDatabase.Parse(content.itemFiles.Select(f => f.text).ToList());
            var dirk = items.Get("item.scoundrels_dirk"); Assert.NotNull(dirk); Assert.AreEqual("mainhand", dirk.slot);
            var looks = File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/Resources/Gear/looks.json"));
            StringAssert.Contains("\"item.scoundrels_dirk\"", looks, "the dirk has a look");
            Assert.AreEqual("leather", SimGear.Weight("class.scoundrel")); Assert.IsTrue(SimGear.CarriesWeapon("class.scoundrel"));
            var worn = SimGear.For(new SimAdventurer { id = "t", name = "Test", classId = "class.scoundrel", level = 5, gearSeed = 3 }, items);
            Assert.Contains("item.scoundrels_dirk", worn, "a sim Scoundrel carries the dirk");
            Assert.IsFalse(worn.Any(id => items.Get(id).slot == "offhand"), "and nothing in the other hand");
        }
    }
}
