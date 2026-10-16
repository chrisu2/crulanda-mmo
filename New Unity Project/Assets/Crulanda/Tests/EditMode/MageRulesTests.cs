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
            Assert.IsNull(t.Find("cw-hot-jaws"), "the Cinderwright is gone");
            foreach (var id in MageKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            CollectionAssert.AreEqual(new[] { "combustion", "heatweaver", "spellbinder", "bonecaller" }, t.Branches.Select(b => b.id).ToArray());
        }
        /// <summary>The Bonecaller (the necromancer, reworked from the Cinderwright, Chris 2026-10-12; GAME-ONLY; no machines): the fourth
        /// path, a bone thrall; tiers 0-3 built (19 ranks), 4-6 written and not loaded; Bone Spikes behind row 2, and rows 0-3 within a
        /// level-15 Mage's sixteen points.</summary>
        [Test] public void The_Bonecaller_is_the_fourth_path_and_its_rows_follow_the_calculator()
        {
            var t = TalentTree.Parse("class.mage", File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/mage.json")), 15, MageKit.ImplementedIds);
            Assert.AreEqual("bonecaller", t.Branches[3].id); Assert.AreEqual("pet", t.Branches[3].role);
            Assert.GreaterOrEqual(t.Branches[3].nodes.Sum(n => n.max), 16, "at least sixteen ranks in rows 0-3");
            foreach (var written in new[] { "bc-twin-thralls", "bc-ossuary-coat", "bc-rise-again", "bc-the-bone-lord" }) Assert.IsNull(t.Find(written), written + " is written, not built");
            var p = EncounterSession.FreshProgress("class.mage"); p.experience = EncounterProgress.XpForLevel(15);
            Assert.IsFalse(t.Propose(p, "bc-bone-spikes", 1, out _, out _), "Bone Spikes is a row-2 talent");
            Buy(t, p, "bc-hard-bone", "bc-hard-bone", "bc-hard-bone", "bc-sharp-marrow", "bc-sharp-marrow", "bc-sharp-marrow", "bc-grave-bond", "bc-grave-bond", "bc-grave-bond",
                "bc-long-drain", "bc-long-drain", "bc-bone-spikes", "bc-death-echo", "bc-death-echo", "bc-death-echo", "bc-bone-armour");
            Assert.AreEqual(0, t.Available(p));
            foreach (var line in File.ReadAllLines(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/mage.json")))
                if (line.Contains("\"bc-")) foreach (var word in new[] { "golem", "forge", "engine", "machine", "gear", "piston" }) StringAssert.DoesNotContain(word, line.ToLowerInvariant(), "no machines (Chris, 2026-10-11)");
        }
        /// <summary>A save with points in a retired path (the Cinderwright, the Archive, the Silent Vow) loads with them refunded and every
        /// other pick kept (2026-10-12).</summary>
        [Test] public void A_retired_paths_points_are_refunded_and_the_rest_kept()
        {
            var p = AtLevel(10);
            p.talents = new List<TalentRank> { new TalentRank { id = "cw-hot-jaws", rank = 2 }, new TalentRank { id = "cb-kindling", rank = 3 }, new TalentRank { id = "sv-quick-hush", rank = 1 }, new TalentRank { id = "ar-resonance", rank = 1 } };
            var said = TalentMigration.DropRetired(p);
            StringAssert.Contains("4 points refunded", said);
            Assert.AreEqual(1, p.talents.Count); Assert.AreEqual("cb-kindling", p.talents[0].id); Assert.AreEqual(3, p.talents[0].rank);
            Assert.IsTrue(Tree().Validate(p, out var why), why);
            Assert.IsNull(TalentMigration.DropRetired(p), "nothing more to drop");
            Assert.IsFalse(TalentMigration.Retired("ca-chorus")); Assert.IsFalse(TalentMigration.Retired("bc-hard-bone"));
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
            Assert.AreEqual(11, content.AllClasses().Count(), "Warrior, Druid, Paladin, Ranger, Mage, Rogue, Archivist, Salt-Mender, Cleric, Shaman, Scoundrel.");
        }
    }
}
