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
            Assert.AreEqual(4, t.Branches.Count, "the Still Vow, the Lull, the Unwriting, the Cantor"); Assert.AreEqual(ArchivistKit.ImplementedIds.Length, count);
            Assert.AreEqual("healer", t.Branches[3].role); Assert.IsNull(t.Find("ca-second-voice"), "the Cantor's tiers 4-6 are written, not built");
            foreach (var id in ArchivistKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            var only = new List<string>(ArchivistKit.ImplementedIds); only.Remove("uw-erasure");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.archivist", Json, 15, only));
        }
        /// <summary>The Cantor (TALENT_DEPTH.md, 2026-10-10): at the cap of 15 (16 points) an Archivist reaches its tier 3; the Chorus is its talent action.</summary>
        [Test] public void The_Cantor_reaches_tier_three_at_the_cap()
        {
            var t = TalentTree.Parse("class.archivist", Json, 15, ArchivistKit.ImplementedIds); var p = EncounterSession.FreshProgress(); p.classId = "class.archivist"; p.experience = EncounterProgress.XpForLevel(15);
            Assert.AreEqual(16, t.Budget(p.Level));
            void Buy(params string[] ids) { foreach (var id in ids) { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var why), id + ": " + why); p.talents = next; } }
            Assert.IsFalse(t.Propose(p, "ca-chorus", 1, out _, out _), "tier 1 needs 5 points in the Cantor");
            Buy("ca-clear-voice", "ca-clear-voice", "ca-clear-voice", "ca-clear-voice", "ca-clear-voice");          // 5
            Buy("ca-chorus", "ca-long-song", "ca-long-song", "ca-long-song", "ca-held-note");                        // 10
            Buy("ca-resonant-ward", "ca-resonant-ward", "ca-resonant-ward", "ca-quiet-mind", "ca-echoing-mend");     // 15
            Buy("ca-symphony");                                                                                      // 16
            Assert.AreEqual(0, t.Available(p)); Assert.IsTrue(t.Validate(p, out var reason), reason);
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
