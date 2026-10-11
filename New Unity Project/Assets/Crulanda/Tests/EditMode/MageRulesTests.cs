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
            Assert.AreEqual(4, t.Branches.Count); Assert.AreEqual(MageKit.ImplementedIds.Length, count); Assert.AreEqual(29, count);
            foreach (var id in MageKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            CollectionAssert.AreEqual(new[] { "combustion", "heatweaver", "spellbinder", "cinderwright" }, t.Branches.Select(b => b.id).ToArray());
        }
        /// <summary>The Cinderwright (TALENT_DEPTH.md, 2026-10-11; no machines, so an ember hound and no forge-golem): the fourth path, pets;
        /// tiers 0-3 built, 4-6 written and not loaded; Cinder Burst behind row 2, and rows 0-3 within a level-15 Mage's sixteen points.</summary>
        [Test] public void The_Cinderwright_is_the_fourth_path_and_its_rows_follow_the_calculator()
        {
            var t = TalentTree.Parse("class.mage", File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/mage.json")), 15, MageKit.ImplementedIds);
            Assert.AreEqual("cinderwright", t.Branches[3].id); Assert.AreEqual("pet", t.Branches[3].role);
            foreach (var written in new[] { "cw-twin-embers", "cw-ash-coat", "cw-rekindle", "cw-the-great-hound" }) Assert.IsNull(t.Find(written), written + " is written, not built");
            var p = EncounterSession.FreshProgress("class.mage"); p.experience = EncounterProgress.XpForLevel(15);
            Assert.IsFalse(t.Propose(p, "cw-cinder-burst", 1, out _, out _), "Cinder Burst is a row-2 talent");
            Buy(t, p, "cw-banked-coals", "cw-banked-coals", "cw-banked-coals", "cw-hot-jaws", "cw-hot-jaws", "cw-hot-jaws", "cw-kinship", "cw-kinship", "cw-kinship",
                "cw-long-stoke", "cw-long-stoke", "cw-cinder-burst", "cw-shared-heat", "cw-shared-heat", "cw-shared-heat", "cw-white-coals");
            Assert.AreEqual(0, t.Available(p));
            foreach (var line in File.ReadAllLines(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/mage.json")))
                if (line.Contains("\"cw-")) foreach (var word in new[] { "golem", "forge", "engine", "machine", "gear", "piston" }) StringAssert.DoesNotContain(word, line.ToLowerInvariant(), "no machines (Chris, 2026-10-11)");
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
            Assert.AreEqual("sb-quench", mage.definition.unlocks.First(u => u.abilityId == "mage.quench").talentId, "Quench is the talent action.");
            Assert.AreEqual(content.abilities.Length, ids.Count, "No duplicate ability ids.");
            Assert.AreEqual(8, content.AllClasses().Count(), "Warrior, Druid, Paladin, Ranger, Mage, Rogue, Archivist, Salt-Mender.");
        }
    }
}
