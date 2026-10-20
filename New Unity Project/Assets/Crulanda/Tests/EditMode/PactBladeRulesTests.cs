using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Pact-Blade's data (Docs/CLASSES_9_2.md section 4; Chris, 2026-10-19, play note 298): its tree loads the implemented
    /// talents, three paths of 22 ranks to tier 4; every path ability is learnt by its own path's talent; the class and its fifteen are in
    /// Encounter.asset on Focus, called Vital-Aether; two swords, heavy armour, human; the numbers of its rules.</summary>
    public class PactBladeRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/pactblade.json")); } }
        static EncounterContent Content { get { return UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset"); } }

        [Test] public void Tree_loads_exactly_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.pactblade", Json, 20, PactBladeKit.ImplementedIds);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(33, count); Assert.AreEqual(PactBladeKit.ImplementedIds.Length, count);
            CollectionAssert.AreEqual(new[] { "twin-blade", "white-whirl", "sentinel" }, t.Branches.Select(b => b.id).ToArray());
            CollectionAssert.AreEqual(new[] { "dps", "dps", "control" }, t.Branches.Select(b => b.role).ToArray());
            foreach (var b in t.Branches)
            {
                Assert.AreEqual(22, b.nodes.Sum(n => n.max), b.id + ": 22 ranks, so 21 points at 20 leave one choice");
                Assert.AreEqual(1, b.nodes.Count(n => n.tier == 4), b.id + ": one capstone at tier 4 (20 points in the path, level 19)");
            }
            var only = new List<string>(PactBladeKit.ImplementedIds); only.Remove("ww-whirl");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.pactblade", Json, 20, only));
        }
        [Test] public void Every_path_ability_is_learnt_by_its_own_paths_talent()
        {
            var cls = Content.FindClass("class.pactblade"); Assert.NotNull(cls, "class.pactblade is a selectable class");
            var t = TalentTree.Parse("class.pactblade", Json, 20, PactBladeKit.ImplementedIds);
            foreach (var u in cls.definition.unlocks)
            {
                Assert.That(u.level, Is.InRange(1, 10), u.abilityId + ": base unlocks at 1-10, the rest by talent");
                if (!PactBladeKit.PathOf.TryGetValue(u.abilityId, out var path)) { Assert.IsTrue(string.IsNullOrEmpty(u.talentId), u.abilityId + " is every path's"); continue; }
                Assert.AreEqual(path, t.BranchOf(u.talentId).id, u.abilityId + " is learnt in its own path");
                var n = t.Find(u.talentId); Assert.AreEqual("active", n.kind, u.talentId); Assert.AreEqual(1, n.max, u.talentId);
            }
            CollectionAssert.AreEquivalent(new[] { "pactblade.measured_cut", "pactblade.second_blade", "pactblade.minimum_movement", "pactblade.hilt_blow", "pactblade.iron_vow", "pactblade.sentinels_stop" },
                cls.definition.unlocks.Where(u => string.IsNullOrEmpty(u.talentId)).Select(u => u.abilityId).ToArray());
        }
        [Test] public void Each_build_owns_only_its_own_paths_abilities_and_reaches_its_capstone_at_20()
        {
            var cls = Content.FindClass("class.pactblade"); var t = TalentTree.Parse("class.pactblade", Json, 20, PactBladeKit.ImplementedIds);
            foreach (var b in t.Branches)
            {
                var p = EncounterSession.FreshProgress("class.pactblade"); p.experience = EncounterProgress.XpForLevel(20);
                var ids = b.nodes.OrderBy(n => n.tier).ThenBy(n => n.col).Select(n => n.id).ToList();
                var cap = b.nodes.First(n => n.tier == 4).id;
                // 20 points in the path's tiers 0-3, then the capstone.
                for (int pass = 0; pass < 40 && TalentTree.Spent(p) < 20; pass++)
                    foreach (var id in ids) if (id != cap && TalentTree.Spent(p) < 20 && t.Propose(p, id, 1, out var next, out _)) p.talents = next;
                Assert.IsTrue(t.Propose(p, cap, 1, out var withCap, out var why), b.id + ": the capstone with the 21st point: " + why); p.talents = withCap;
                Assert.AreEqual(0, t.Available(p), b.id + ": every point spent");
                foreach (var u in cls.definition.unlocks)
                {
                    bool mine = string.IsNullOrEmpty(u.talentId) || ids.Contains(u.talentId);
                    bool owned = string.IsNullOrEmpty(u.talentId) || TalentTree.Rank(p, u.talentId) > 0;
                    Assert.AreEqual(mine, owned, b.id + " / " + u.abilityId);
                }
            }
        }
        [Test] public void The_class_and_its_fifteen_are_in_the_content_on_Vital_Aether()
        {
            var content = Content; var cls = content.FindClass("class.pactblade"); Assert.NotNull(cls);
            Assert.AreEqual("Pact-Blade", cls.definition.displayName);
            Assert.AreEqual(Crulanda.Core.ResourceKind.Focus, cls.definition.resource);
            Assert.AreEqual("Vital-Aether", cls.definition.resourceName); Assert.AreEqual(100, cls.definition.maxResource);
            Assert.NotNull(cls.talentTree); StringAssert.Contains("\"id\": \"pactblade\"", cls.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in PactBladeKit.BarIds) Assert.IsTrue(ids.Contains(id), id);
            CollectionAssert.AreEqual(PactBladeKit.BarIds, cls.definition.unlocks.Select(u => u.abilityId).ToArray(), "the unlocks are the bar, in order");
            Assert.AreEqual(content.abilities.Length, ids.Count, "no duplicate ability ids");
            Assert.IsFalse(EncounterSession.Hybrid("class.pactblade"), "a pure damage class");
            Assert.IsTrue(Races.Allows(Races.Human, "class.pactblade")); Assert.IsFalse(Races.Allows(Races.Orc, "class.pactblade"), "the North's Iron-Born: human");
            Assert.AreEqual("heavy", SimGear.Weight("class.pactblade"), "heavy armour");
            Assert.AreEqual(Crulanda.Core.StatType.Strength, BossBuffs.MainStat("class.pactblade"));
            Assert.AreEqual("encounter-pactblade", EncounterSave.SlotFor("class.pactblade"), "its own save");
        }
        [Test] public void Two_swords_one_for_each_hand()
        {
            var db = ItemDatabase.Parse(Content.itemFiles.Select(f => f.text).ToList());
            Assert.AreEqual("mainhand", db.Get("item.pact_sword").slot); Assert.AreEqual("offhand", db.Get("item.pact_second_sword").slot);
            var looks = GearLooks.Load();
            Assert.AreEqual("sword.arming", looks.Resolve(db.Get("item.pact_sword")).family);
            Assert.AreEqual("sword.off", looks.Resolve(db.Get("item.pact_second_sword")).family, "the second sword drawn in the left fist");
            Assert.IsTrue(SimGear.CarriesWeapon("class.pactblade"));
        }
        [Test] public void Vital_Aether_burns_and_spends_by_the_numbers()
        {
            Assert.AreEqual(1f, PactBladeKit.BurningScale(80, 0), 1e-4, "80 is not yet burning");
            Assert.AreEqual(1.1f, PactBladeKit.BurningScale(81, 0), 1e-4, "above 80: a tenth harder");
            Assert.AreEqual(1.16f, PactBladeKit.BurningScale(100, 2), 1e-4, "Hot Blood: 3% a rank more");
            Assert.AreEqual(2 * (8 + 50), PactBladeKit.EdgeBlow(8, 100, 60), 1e-3, "Aether Edge: (power + half the weapon) for each 30 spent");
            Assert.AreEqual(.9f, PactBladeKit.AfterimageShare(3), 1e-4, "Bright Wake: a tenth more a rank");
            Assert.AreEqual(.75f, PactBladeKit.DisarmScale(false, 0), 1e-4); Assert.AreEqual(.84f, PactBladeKit.DisarmScale(true, 2), 1e-4, "a boss's a tenth, Heavy Hand 3% a rank");
            var kit = (PactBladeKit)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(PactBladeKit));
            Assert.IsFalse(kit.RestingResourceRegen, "Vital-Aether is a fight's own: it does not fill out of combat");
        }
        [Test] public void Sims_and_the_group_finder_know_it()
        {
            Assert.Contains("class.pactblade", SimRoster.ClassIds);
            Assert.AreEqual("Pact-Blade", SimRoster.ClassName("class.pactblade"));
            Assert.AreEqual(SimCompanion.PartyRole.Damage, BossBuffs.NominalRole("class.pactblade"));
            Assert.AreEqual("blacksmithing", SimEconomy.CraftTrade("class.pactblade"));
            Assert.NotNull(ClassBuffs.For("class.pactblade"));
        }
    }
}
