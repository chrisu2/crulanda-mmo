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
            Assert.AreEqual(3, t.Branches.Count); Assert.AreEqual(RogueKit.ImplementedIds.Length, count);
            foreach (var id in RogueKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            var only = new List<string>(RogueKit.ImplementedIds); only.Remove("lk-dust");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.rogue", Json, 15, only));
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
