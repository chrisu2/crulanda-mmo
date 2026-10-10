using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Paladin's data (Phase 5.1): its talent tree loads exactly the implemented talents in three branches with the calculator's
    /// gates, and its content (class profile, nine abilities, unlocks) is in Encounter.asset for the kit to build from.
    /// </summary>
    public class PaladinRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/paladin.json")); } }
        static TalentTree Tree() { return TalentTree.Parse("class.paladin", Json, 10, PaladinKit.ImplementedIds); }
        static EncounterProgress AtLevel(int level) { var p = EncounterSession.FreshProgress("class.paladin"); p.experience = EncounterProgress.XpForLevel(level); return p; }
        static void Buy(TalentTree t, EncounterProgress p, params string[] ids)
        { foreach (var id in ids) { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var reason), id + ": " + reason); p.talents = next; } }

        [Test] public void Data_tree_loads_exactly_the_implemented_talents()
        {
            var t = Tree();
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(4, t.Branches.Count, "Oathguard, Judicator, Sanctuary, Vanguard.");
            Assert.AreEqual(PaladinKit.ImplementedIds.Length, count); Assert.AreEqual(30, count, "21 of the first three paths, 9 of the Vanguard's tiers 0-3; its tiers 4-6 are written, not built.");
            foreach (var id in PaladinKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            CollectionAssert.AreEqual(new[] { "oathguard", "judicator", "sanctuary", "vanguard" }, t.Branches.Select(b => b.id).ToArray());
            Assert.AreEqual("support", t.Branches[3].role);
        }
        /// <summary>The Vanguard (TALENT_DEPTH.md, 2026-10-09): at the cap of 15 (16 points) a Paladin reaches its tier 3; Close Ranks is its talent action.</summary>
        [Test] public void The_Vanguard_reaches_tier_three_at_the_cap()
        {
            var t = TalentTree.Parse("class.paladin", Json, 15, PaladinKit.ImplementedIds); var p = AtLevel(15);   // the tree at the cap of 15 (round 29), not the Phase 5.1 cap of 10 the other tests keep
            Assert.AreEqual(16, t.Budget(p.Level));
            Assert.IsFalse(t.Propose(p, "va-close-ranks", 1, out _, out _), "Tier 1 needs 5 points in the Vanguard.");
            Buy(t, p, "va-colours-held", "va-colours-held", "va-colours-held", "va-colours-held", "va-colours-held");   // 5: tier 1 opens
            Buy(t, p, "va-close-ranks", "va-press-longer", "va-press-longer", "va-press-longer");                          // 9
            Assert.IsFalse(t.Propose(p, "va-comrades", 1, out _, out _), "Tier 2 needs 10 points.");
            Buy(t, p, "va-rampart");                                                                                     // 10: tier 2 opens
            Buy(t, p, "va-steady-ranks", "va-steady-ranks", "va-steady-ranks", "va-comrades");                           // 14
            Assert.IsFalse(t.Propose(p, "va-under-the-colours", 1, out _, out _), "Tier 3 needs 15 points.");
            Buy(t, p, "va-rampart");                                                                                     // 15: tier 3 opens
            Buy(t, p, "va-under-the-colours");                                                                            // 16
            Assert.AreEqual(0, t.Available(p), "Sixteen points: the Vanguard to its third tier, as far as the cap of 15 reaches.");
            Assert.IsNull(t.Find("va-oriflamme"), "Tier 4 is written, not built: not in the loaded tree.");
        }
        [Test] public void Implemented_flag_without_code_is_rejected()
        {
            var only = new List<string>(PaladinKit.ImplementedIds); only.Remove("og-censure");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.paladin", Json, 10, only));
        }
        [Test] public void Tier_gates_and_prerequisites_follow_the_calculator()
        {
            var t = Tree(); var p = AtLevel(10);
            Assert.AreEqual(11, t.Budget(p.Level));
            Assert.IsFalse(t.Propose(p, "og-censure", 1, out _, out _), "Row 1 needs 5 points in Oathguard.");
            Buy(t, p, "og-steadfast", "og-steadfast", "og-steadfast", "og-steadfast", "og-steadfast");
            Buy(t, p, "og-censure");
            Assert.IsFalse(t.Propose(p, "og-vigil", 1, out _, out _), "Row 2 needs 10 points in Oathguard.");
            Buy(t, p, "og-unbroken-oath", "og-unbroken-oath", "og-unbroken-oath", "og-bulwark-of-faith");
            Buy(t, p, "og-vigil");
            Assert.AreEqual(0, t.Available(p), "Eleven points: one row-2 talent, fully committed.");
            Assert.IsFalse(t.Propose(p, "ju-zeal", 1, out _, out var reason)); StringAssert.Contains("No points left", reason);
            // Executioner needs Weighted Judgement at 3/3, Mercy needs Grace at 3/3.
            var q = AtLevel(10);
            Buy(t, q, "ju-zeal", "ju-zeal", "ju-zeal", "ju-zeal", "ju-zeal", "ju-weighted-judgement", "ju-weighted-judgement", "ju-burning-light", "ju-burning-light", "ju-burning-light");
            Assert.IsFalse(t.Propose(q, "ju-executioner", 1, out _, out _), "Executioner needs Weighted Judgement fully ranked.");
        }
        [Test] public void The_class_and_its_nine_abilities_are_in_the_content()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content);
            var paladin = content.FindClass("class.paladin"); Assert.NotNull(paladin, "class.paladin is a selectable class.");
            Assert.AreEqual("Paladin", paladin.definition.displayName); Assert.AreEqual(Crulanda.Core.ResourceKind.Mana, paladin.definition.resource);
            Assert.NotNull(paladin.talentTree, "Its talent tree is wired."); StringAssert.Contains("\"id\": \"paladin\"", paladin.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in PaladinKit.BarIds) Assert.IsTrue(ids.Contains(id), id + " is in the ability catalog.");
            Assert.AreEqual(PaladinKit.BarIds.Length, paladin.definition.unlocks.Length, "One unlock per bar slot.");
            CollectionAssert.AreEqual(PaladinKit.BarIds, paladin.definition.unlocks.Select(u => u.abilityId).ToArray(), "The unlocks are the bar, in order.");
            Assert.AreEqual("og-censure", paladin.definition.unlocks.First(u => u.abilityId == "paladin.censure").talentId, "Censure is the talent action.");
            Assert.AreEqual(content.abilities.Length, ids.Count, "No duplicate ability ids.");
            Assert.AreEqual(7, content.AllClasses().Count(), "Warrior, Druid, Paladin, Ranger, Mage, Rogue, Archivist.");
        }
    }
}
