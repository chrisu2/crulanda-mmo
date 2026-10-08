using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Ranger's data (Phase 5.1b): its tree loads exactly the implemented talents with the calculator's gates, and its content is in Encounter.asset.</summary>
    public class RangerRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/ranger.json")); } }
        static TalentTree Tree() { return TalentTree.Parse("class.ranger", Json, 10, RangerKit.ImplementedIds); }
        static EncounterProgress AtLevel(int level) { var p = EncounterSession.FreshProgress("class.ranger"); p.experience = EncounterProgress.XpForLevel(level); return p; }
        static void Buy(TalentTree t, EncounterProgress p, params string[] ids)
        { foreach (var id in ids) { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var reason), id + ": " + reason); p.talents = next; } }

        [Test] public void Data_tree_loads_exactly_the_implemented_talents()
        {
            var t = Tree();
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(3, t.Branches.Count); Assert.AreEqual(RangerKit.ImplementedIds.Length, count); Assert.AreEqual(21, count);
            foreach (var id in RangerKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            CollectionAssert.AreEqual(new[] { "marksman", "beastbond", "pathfinder" }, t.Branches.Select(b => b.id).ToArray());
        }
        [Test] public void Implemented_flag_without_code_is_rejected()
        {
            var only = new List<string>(RangerKit.ImplementedIds); only.Remove("pf-pin");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.ranger", Json, 10, only));
        }
        [Test] public void Tier_gates_and_prerequisites_follow_the_calculator()
        {
            var t = Tree(); var p = AtLevel(10);
            Assert.IsFalse(t.Propose(p, "pf-pin", 1, out _, out _), "Row 1 needs 5 points in Pathfinder.");
            Buy(t, p, "pf-fleet", "pf-fleet", "pf-fleet", "pf-fleet", "pf-fleet", "pf-pin");
            Assert.IsFalse(t.Propose(p, "pf-trapper", 1, out _, out _), "Row 2 needs 10 points in Pathfinder.");
            Buy(t, p, "pf-keen-eye", "pf-keen-eye", "pf-keen-eye", "pf-tangling-snare", "pf-trapper");
            Assert.AreEqual(0, t.Available(p));
            var q = AtLevel(10);
            Buy(t, q, "mk-quick-draw", "mk-quick-draw", "mk-quick-draw", "mk-steady-hand", "mk-steady-hand", "mk-piercing", "mk-piercing", "mk-piercing", "mk-snap-shot", "mk-bleeding-wounds");
            Assert.IsFalse(t.Propose(q, "mk-headshot", 1, out _, out _), "Headshot needs Steady Hand fully ranked.");
        }
        [Test] public void The_class_and_its_nine_abilities_are_in_the_content()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content);
            var ranger = content.FindClass("class.ranger"); Assert.NotNull(ranger, "class.ranger is a selectable class.");
            Assert.AreEqual("Ranger", ranger.definition.displayName); Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, ranger.definition.resource);
            Assert.AreEqual(ClassRole.Ranged, ranger.definition.role);
            Assert.NotNull(ranger.talentTree); StringAssert.Contains("\"id\": \"ranger\"", ranger.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in RangerKit.BarIds) Assert.IsTrue(ids.Contains(id), id + " is in the ability catalog.");
            CollectionAssert.AreEqual(RangerKit.BarIds, ranger.definition.unlocks.Select(u => u.abilityId).ToArray(), "The unlocks are the bar, in order.");
            Assert.AreEqual("pf-pin", ranger.definition.unlocks.Last().talentId, "Pin is the talent action.");
            Assert.AreEqual(content.abilities.Length, ids.Count, "No duplicate ability ids.");
            Assert.AreEqual(6, content.AllClasses().Count(), "Warrior, Druid, Paladin, Ranger, Mage, Rogue.");
        }
    }
}
