using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Archivist's data (Docs/CC_DESIGN.md section 0): its tree loads the implemented talents, and its class and seven moves are in Encounter.asset.</summary>
    public class ArchivistRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/archivist.json")); } }
        [Test] public void Tree_loads_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.archivist", Json, 15, ArchivistKit.ImplementedIds);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(3, t.Branches.Count); Assert.AreEqual(ArchivistKit.ImplementedIds.Length, count);
            foreach (var id in ArchivistKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            var only = new List<string>(ArchivistKit.ImplementedIds); only.Remove("ar-full-jar");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.archivist", Json, 15, only));
        }
        [Test] public void The_class_and_its_seven_songs_are_in_the_content()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            var arch = content.FindClass("class.archivist"); Assert.NotNull(arch);
            Assert.AreEqual("Archivist", arch.definition.displayName); Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, arch.definition.resource);
            Assert.NotNull(arch.talentTree); StringAssert.Contains("\"id\": \"archivist\"", arch.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in ArchivistKit.BarIds) Assert.IsTrue(ids.Contains(id), id);
            CollectionAssert.AreEqual(ArchivistKit.BarIds, arch.definition.unlocks.Select(u => u.abilityId).ToArray(), "The unlocks are the bar, in order.");
            Assert.AreEqual(content.abilities.Length, ids.Count, "No duplicate ability ids.");
        }
    }
}
