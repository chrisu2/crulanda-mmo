using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Rogue's data (Docs/CC_DESIGN.md section 0): its tree loads the implemented talents, and its class and seven moves are in Encounter.asset.</summary>
    public class RogueRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/rogue.json")); } }
        [Test] public void Tree_loads_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.rogue", Json, 15, RogueKit.ImplementedIds);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(4, t.Branches.Count, "Thief's Grace, Shardwork, Locksmith, the Swashbuckler"); Assert.AreEqual(RogueKit.ImplementedIds.Length, count);
            Assert.AreEqual("support", t.Branches[3].role);
            foreach (var written in new[] { "bk-panache", "bk-riposte", "bk-grand-finale" }) Assert.IsNull(t.Find(written), written + " is written, not built");
            var p = EncounterSession.FreshProgress("class.rogue"); p.experience = EncounterProgress.XpForLevel(15);
            Assert.IsFalse(t.Propose(p, "bk-tumble", 1, out _, out _), "Tumble is a row-2 talent");
            foreach (var id in new[] { "bk-showmanship", "bk-showmanship", "bk-showmanship", "bk-light-blade", "bk-light-blade", "bk-light-blade", "bk-from-behind", "bk-from-behind", "bk-from-behind",
                "bk-long-flourish", "bk-long-flourish", "bk-tumble", "bk-wide-open", "bk-wide-open", "bk-wide-open", "bk-encore" })
            { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var why), id + ": " + why); p.talents = next; }
            Assert.AreEqual(0, t.Available(p), "rows 0-3 within a level-15 Rogue's sixteen points");
            foreach (var id in RogueKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            var only = new List<string>(RogueKit.ImplementedIds); only.Remove("lk-dust");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.rogue", Json, 15, only));
        }
        /// <summary>The level-20 row (2026-10-16): seven more nodes, and each one's words carry the numbers its code uses.</summary>
        [Test] public void The_level_twenty_nodes_say_what_the_code_does()
        {
            var t = TalentTree.Parse("class.rogue", Json, 20, RogueKit.ImplementedIds);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(36, count, "29, and Gone Clean, Unremarked, Spare Motion, Close Work, Practised Hands, Pocket of Dust, Crowd Pleaser");
            StringAssert.Contains(RogueKit.GoneCleanFocus + " Focus per rank", t.Find("tg-gone-clean").desc);
            StringAssert.Contains("1 metre closer per rank", t.Find("tg-unremarked").desc);
            StringAssert.Contains(RogueKit.SpareMotionCost + " less Focus per rank", t.Find("sw-spare-motion").desc);
            StringAssert.Contains(Mathf.RoundToInt(RogueKit.CloseWorkBelow * 100) + "% health", t.Find("sw-close-work").desc);
            StringAssert.Contains(RogueKit.PractisedHandsCost + " less Focus per rank", t.Find("lk-practised-hands").desc);
            StringAssert.Contains(RogueKit.PocketDustSeconds + " seconds sooner per rank", t.Find("lk-pocket-dust").desc);
            StringAssert.Contains(RogueKit.CrowdPleaserFocus + " Focus per rank", t.Find("bk-crowd-pleaser").desc);
            Assert.AreEqual(3, t.Find("bk-crowd-pleaser").tier, "moved up to row 3 for the cap of 20");
        }
        /// <summary>The Rogue's pair of dirks (Chris, 2026-10-11: "rogue should have daggers. and maybe a crossbow to pull but mainly melee"):
        /// a knife for each hand in the items, the second drawn in the left fist (knife.off), on every sim Rogue; and Throw Knife on the bar.</summary>
        [Test] public void A_pair_of_dirks_and_a_thrown_knife()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            var items = ItemDatabase.Parse(content.itemFiles.Select(f => f.text).ToList());
            var main = items.Get("item.rogues_dirk"); var off = items.Get("item.rogues_second_dirk");
            Assert.NotNull(main, "a dirk"); Assert.AreEqual("mainhand", main.slot); Assert.NotNull(off, "its pair"); Assert.AreEqual("offhand", off.slot);
            var looks = File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/Resources/Gear/looks.json"));
            StringAssert.Contains("\"item.rogues_dirk\",            \"look\": \"knife:", looks, "the dirk's look, a knife"); StringAssert.Contains("\"item.rogues_second_dirk\",     \"look\": \"knife.off:", looks, "its pair's, in the left fist");
            Assert.IsTrue(SimGear.CarriesWeapon("class.rogue"));
            var sim = new SimAdventurer { id = "t", name = "Test", classId = "class.rogue", level = 5, gearSeed = 3 };
            var worn = SimGear.For(sim, items);
            Assert.Contains("item.rogues_dirk", worn, "a sim Rogue carries the dirk"); Assert.Contains("item.rogues_second_dirk", worn, "and its pair");
            Assert.AreEqual("rogue.throw_knife", RogueKit.BarIds.Last(), "Throw Knife, the pull, on the bar");
        }
        [Test] public void The_class_and_its_seven_moves_are_in_the_content()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            var rogue = content.FindClass("class.rogue"); Assert.NotNull(rogue);
            Assert.AreEqual("Rogue", rogue.definition.displayName); Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, rogue.definition.resource);
            Assert.NotNull(rogue.talentTree); StringAssert.Contains("\"id\": \"rogue\"", rogue.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in RogueKit.BarIds) Assert.IsTrue(ids.Contains(id), id);
            CollectionAssert.AreEqual(RogueKit.BarIds, rogue.definition.unlocks.Select(u => u.abilityId).ToArray(), "The unlocks are the bar, in order.");
            Assert.AreEqual(content.abilities.Length, ids.Count, "No duplicate ability ids.");
        }
    }
}
