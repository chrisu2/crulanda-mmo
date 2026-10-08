using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Mage's data (Phase 5.1c): its tree loads exactly the implemented talents with the calculator's gates, and its content is in Encounter.asset.</summary>
    public class MageRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/mage.json")); } }
        static TalentTree Tree() { return TalentTree.Parse("class.mage", Json, 10, MageKit.ImplementedIds); }
        static EncounterProgress AtLevel(int level) { var p = EncounterSession.FreshProgress("class.mage"); p.experience = EncounterProgress.XpForLevel(level); return p; }
        static void Buy(TalentTree t, EncounterProgress p, params string[] ids)
        { foreach (var id in ids) { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var reason), id + ": " + reason); p.talents = next; } }

        [Test] public void Data_tree_loads_exactly_the_implemented_talents()
        {
            var t = Tree();
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(3, t.Branches.Count); Assert.AreEqual(MageKit.ImplementedIds.Length, count); Assert.AreEqual(21, count);
            foreach (var id in MageKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            CollectionAssert.AreEqual(new[] { "combustion", "heatweaver", "spellbinder" }, t.Branches.Select(b => b.id).ToArray());
        }
        [Test] public void Implemented_flag_without_code_is_rejected()
        {
            var only = new List<string>(MageKit.ImplementedIds); only.Remove("sb-quench");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.mage", Json, 10, only));
        }
        [Test] public void Tier_gates_and_prerequisites_follow_the_calculator()
        {
            var t = Tree(); var p = AtLevel(10);
            Assert.IsFalse(t.Propose(p, "sb-quench", 1, out _, out _), "Row 1 needs 5 points in Spellbinder.");
            Buy(t, p, "sb-reservoir", "sb-reservoir", "sb-reservoir", "sb-reservoir", "sb-reservoir", "sb-quench");
            Assert.IsFalse(t.Propose(p, "sb-quickening", 1, out _, out _), "Row 2 needs 10 points in Spellbinder.");
            Buy(t, p, "sb-ward-weave", "sb-ward-weave", "sb-ward-weave", "sb-shared-flame", "sb-quickening");
            Assert.AreEqual(0, t.Available(p));
            var q = AtLevel(10);
            Buy(t, q, "cb-hot-hands", "cb-hot-hands", "cb-hot-hands", "cb-kindling", "cb-kindling", "cb-stoked", "cb-stoked", "cb-stoked", "cb-backdraft", "cb-flashpoint");
            Assert.IsFalse(t.Propose(q, "cb-inferno", 1, out _, out _), "Inferno needs Kindling fully ranked.");
        }
        [Test] public void The_class_and_its_nine_abilities_are_in_the_content()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content);
            var mage = content.FindClass("class.mage"); Assert.NotNull(mage, "class.mage is a selectable class.");
            Assert.AreEqual("Mage", mage.definition.displayName); Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, mage.definition.resource);
            Assert.AreEqual(ClassRole.Caster, mage.definition.role);
            Assert.NotNull(mage.talentTree); StringAssert.Contains("\"id\": \"mage\"", mage.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in MageKit.BarIds) Assert.IsTrue(ids.Contains(id), id + " is in the ability catalog.");
            CollectionAssert.AreEqual(MageKit.BarIds, mage.definition.unlocks.Select(u => u.abilityId).ToArray(), "The unlocks are the bar, in order.");
            Assert.AreEqual("sb-quench", mage.definition.unlocks.Last().talentId, "Quench is the talent action.");
            Assert.AreEqual(content.abilities.Length, ids.Count, "No duplicate ability ids.");
            Assert.AreEqual(7, content.AllClasses().Count(), "Warrior, Druid, Paladin, Ranger, Mage, Rogue, Archivist.");
        }
    }
}
