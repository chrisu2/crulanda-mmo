using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Salt-Mender's data (Phase 9.2, 2026-10-11; CLASS_BUILD_MATRIX.md "Phase 9.2 and 9.3 from the canon"): its tree loads the
    /// implemented talents, its class and eight workings are in Encounter.asset on Salt, and it is not a healer (no salt-magic heals in the books).</summary>
    public class SaltMenderRulesTests
    {
        static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/saltmender.json")); } }
        static void Buy(TalentTree t, EncounterProgress p, params string[] ids)
        { foreach (var id in ids) { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var reason), id + ": " + reason); p.talents = next; } }

        [Test] public void Tree_loads_exactly_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.saltmender", Json, 15, SaltMenderKit.ImplementedIds);
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(21, count); Assert.AreEqual(SaltMenderKit.ImplementedIds.Length, count);
            CollectionAssert.AreEqual(new[] { "salt-speaker", "mason", "sifter" }, t.Branches.Select(b => b.id).ToArray());
            CollectionAssert.AreEqual(new[] { "control", "tank", "dps" }, t.Branches.Select(b => b.role).ToArray());
            var only = new List<string>(SaltMenderKit.ImplementedIds); only.Remove("ss-calcify");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.saltmender", Json, 15, only));
        }
        [Test] public void Rows_follow_the_calculator()
        {
            var t = TalentTree.Parse("class.saltmender", Json, 15, SaltMenderKit.ImplementedIds);
            var p = EncounterSession.FreshProgress("class.saltmender"); p.experience = EncounterProgress.XpForLevel(10);
            Assert.IsFalse(t.Propose(p, "ss-calcify", 1, out _, out _), "row 1 needs 5 points in the Salt-Speaker");
            Buy(t, p, "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-long-binding", "ss-calcify");
            Assert.IsFalse(t.Propose(p, "ss-deep-crystal", 1, out _, out _), "row 2 needs 10");
            Buy(t, p, "ss-white-crust", "ss-white-crust", "ss-white-crust", "ss-first-sea", "ss-deep-crystal");
            Assert.AreEqual(0, t.Available(p));
        }
        [Test] public void The_class_and_its_eight_workings_are_in_the_content_on_Salt()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            var salt = content.FindClass("class.saltmender"); Assert.NotNull(salt, "class.saltmender is a selectable class");
            Assert.AreEqual("Salt-Mender", salt.definition.displayName); Assert.AreEqual(Crulanda.Core.ResourceKind.Salt, salt.definition.resource);
            Assert.NotNull(salt.talentTree); StringAssert.Contains("\"id\": \"saltmender\"", salt.talentTree.text);
            var ids = new HashSet<string>(content.abilities.Select(a => a.id));
            foreach (var id in SaltMenderKit.BarIds) Assert.IsTrue(ids.Contains(id), id);
            CollectionAssert.AreEqual(SaltMenderKit.BarIds, salt.definition.unlocks.Select(u => u.abilityId).ToArray(), "the unlocks are the bar, in order");
            foreach (var a in content.abilities.Where(a => a.id.StartsWith("salt.")))
                Assert.AreNotEqual(Crulanda.Abilities.AbilityEffect.Heal, a.effect, a.id + ": not a healer (no salt-magic heals in the books)");
            Assert.AreEqual(content.abilities.Length, ids.Count, "no duplicate ability ids");
            Assert.AreEqual(ActorLook.Mage, EncounterSession.LookForClass("class.saltmender"), "the robe figure until its own is made");
        }
    }
}
