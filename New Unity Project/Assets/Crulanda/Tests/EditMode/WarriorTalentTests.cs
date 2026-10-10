using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;
namespace Crulanda.Tests
{
    public static class TestTalents
    {
        public static string Json { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/warrior.json")); } }
        public static TalentTree Warrior() { return TalentTree.Parse("class.warrior", Json, 10, WarriorKit.ImplementedIds); }
        public static string DruidJson { get { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/druid.json")); } }
        public static TalentTree Druid() { return TalentTree.Parse("class.druid", DruidJson, 10, DruidKit.ImplementedIds); }
    }
    public class WarriorTalentTests
    {
        static EncounterProgress AtLevel(int level) { var p = EncounterSession.FreshProgress(); p.experience = EncounterProgress.XpForLevel(level); return p; }
        static void Buy(TalentTree t, EncounterProgress p, params string[] ids)
        { foreach (var id in ids) { Assert.IsTrue(t.Propose(p, id, 1, out var next, out var reason), id + ": " + reason); p.talents = next; } }

        [Test] public void Data_tree_loads_exactly_the_implemented_talents()
        {
            var t = TestTalents.Warrior();
            int count = 0; foreach (var b in t.Branches) count += b.nodes.Length;
            Assert.AreEqual(4, t.Branches.Count, "Tank, DPS, Support, Jailer."); Assert.AreEqual(WarriorKit.ImplementedIds.Length, count);
            Assert.AreEqual("jailer", t.Branches[3].id); Assert.AreEqual("control", t.Branches[3].role);
            Assert.NotNull(t.Find("jl-warden"), "The Jailer's tier 3 is built."); Assert.IsNull(t.Find("jl-manacles"), "Its tiers 4-6 are written, not built.");
            foreach (var id in WarriorKit.ImplementedIds) Assert.NotNull(t.Find(id), id);
            Assert.NotNull(t.Find("tk-held-line"), "Row 3 is implemented (2026-10-08)."); Assert.IsNull(t.Find("tk-unbroken-line"), "Rows 4-6 wait for the cap-30 levels.");
        }
        /// <summary>The Jailer (TALENT_DEPTH.md, 2026-10-10): at the cap of 15 (16 points) a Warrior reaches its tier 3; Weighted Net is its talent action.</summary>
        [Test] public void The_Jailer_reaches_tier_three_at_the_cap()
        {
            var t = TalentTree.Parse("class.warrior", TestTalents.Json, 15, WarriorKit.ImplementedIds); var p = AtLevel(15);
            Assert.AreEqual(16, t.Budget(p.Level));
            Assert.IsFalse(t.Propose(p, "jl-weighted-net", 1, out _, out _), "Tier 1 needs 5 points in the Jailer.");
            Buy(t, p, "jl-cord-and-hook", "jl-cord-and-hook", "jl-cord-and-hook", "jl-cord-and-hook", "jl-cord-and-hook");   // 5: tier 1 opens
            Assert.IsFalse(t.Propose(p, "jl-wide-mesh", 1, out _, out _), "Wide Mesh needs tier 2 and the Net.");
            Buy(t, p, "jl-weighted-net", "jl-tight-weave", "jl-tight-weave", "jl-heavy-iron", "jl-heavy-iron");                  // 10: tier 2 opens
            Buy(t, p, "jl-drag-chain", "jl-wide-mesh", "jl-keepers-due", "jl-keepers-due", "jl-turnkey");                       // 15: tier 3 opens
            Buy(t, p, "jl-warden");                                                                                             // 16
            Assert.AreEqual(0, t.Available(p), "Sixteen points: the Jailer to its third tier.");
            Assert.IsTrue(t.Validate(p, out var why), why);
        }
        [Test] public void Implemented_flag_without_code_is_rejected()
        {
            var only = new List<string>(WarriorKit.ImplementedIds); only.Remove("tk-intercept");
            Assert.Throws<ArgumentException>(() => TalentTree.Parse("class.warrior", TestTalents.Json, 10, only));
        }
        [Test] public void Fresh_character_has_two_points_in_the_first_row_only()
        {
            var t = TestTalents.Warrior(); var p = AtLevel(1);
            Assert.AreEqual(2, t.Budget(p.Level));
            Assert.IsFalse(t.Propose(p, "tk-intercept", 1, out _, out _), "Row 2 needs 5 points in Tank.");
            Buy(t, p, "dp-weapon-pressure", "tk-tempered-armor");
            Assert.IsFalse(t.Propose(p, "tk-tempered-armor", 1, out _, out var reason)); StringAssert.Contains("No points left", reason);
        }
        [Test] public void Tier_gates_prerequisites_and_refund_safety_match_the_calculator()
        {
            var t = TestTalents.Warrior(); var p = AtLevel(10);
            Assert.AreEqual(11, t.Budget(10));
            Buy(t, p, "tk-hardened-grip", "tk-hardened-grip", "tk-hardened-grip", "tk-tempered-armor");
            Assert.IsFalse(t.Propose(p, "tk-steady-challenge", 1, out _, out _), "4 points in Tank and armor not maxed.");
            Buy(t, p, "tk-tempered-armor", "tk-steady-challenge", "tk-intercept", "tk-timed-guard", "tk-timed-guard", "tk-timed-guard");
            Assert.AreEqual(10, TalentTree.Spent(p));
            Buy(t, p, "tk-bulwark");
            Assert.IsFalse(t.Propose(p, "tk-timed-guard", -1, out _, out _), "Refund would strand Bulwark's 10-point gate.");
            Assert.IsFalse(t.Propose(p, "tk-tempered-armor", -1, out _, out _), "Refund would orphan Steady Challenge.");
            Assert.IsTrue(t.Propose(p, "tk-bulwark", -1, out _, out _));
            Assert.IsTrue(t.Validate(p, out var why), why);
        }
        [Test] public void Hybrid_five_and_five_reaches_both_second_rows()
        {
            var t = TestTalents.Warrior(); var p = AtLevel(10);
            Buy(t, p, "tk-tempered-armor", "tk-tempered-armor", "tk-hardened-grip", "tk-hardened-grip", "tk-hardened-grip", "tk-intercept",
                "dp-weapon-pressure", "dp-weapon-pressure", "dp-read-the-opening", "dp-read-the-opening", "dp-read-the-opening");
            Assert.IsFalse(t.Propose(p, "dp-breaching-blow", 1, out _, out _), "Budget spent.");
            Assert.IsTrue(t.Validate(p, out var why), why);
        }
        [Test] public void Legacy_saves_migrate_and_v3_round_trips()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-talents-" + Guid.NewGuid().ToString("N"));
            try
            {
                var tree = TestTalents.Warrior(); var store = new SaveFileStore(root); var save = new EncounterSave(root, tree);
                // v1: no talents at all.
                var p = EncounterSession.FreshProgress(); p.experience = 240; p.gold = 41; p.recruited = true; p.equippedItem = "blade"; p.inventory.Add("blade");
                store.Write("encounter", new SaveEnvelope { formatVersion = 1, payloadType = "CrulandaEncounter", payloadJson = JsonUtility.ToJson(p) });
                Assert.IsTrue(save.Read(out var migrated, out var message), message);
                Assert.AreEqual(p.playerId, migrated.playerId); Assert.AreEqual(41, migrated.gold); Assert.AreEqual("blade", migrated.equipment[(int)EquipSlot.MainHand].item, "The old weapon moves to the main hand.");
                Assert.IsEmpty(migrated.talents); Assert.AreEqual(5, migrated.Level, "Old 240 XP was level 5 and stays level 5.");
                migrated.experience = 240;   // older formats store the old 60-XP-per-level scale
                // v2 with a still-legal prototype allocation: ids are renamed, ranks kept.
                migrated.talents = new List<TalentRank> { new TalentRank { id = "dps.power", rank = 2 } };
                store.Write("encounter", new SaveEnvelope { formatVersion = 2, payloadType = "CrulandaEncounter", payloadJson = JsonUtility.ToJson(migrated) });
                Assert.IsTrue(save.Read(out var carried, out message), message);
                Assert.AreEqual(2, TalentTree.Rank(carried, "dp-weapon-pressure")); StringAssert.Contains("carried over", message);
                // v2 allocation that the new tier rules forbid (old signature at level 5): points refunded, progress kept.
                carried.experience = 240;
                carried.talents = new List<TalentRank> { new TalentRank { id = "tank.armor", rank = 2 }, new TalentRank { id = "tank.challenge", rank = 1 }, new TalentRank { id = "tank.bulwark", rank = 1 } };
                store.Write("encounter", new SaveEnvelope { formatVersion = 2, payloadType = "CrulandaEncounter", payloadJson = JsonUtility.ToJson(carried) });
                Assert.IsTrue(save.Read(out var refunded, out message), message);
                Assert.IsEmpty(refunded.talents); Assert.AreEqual(41, refunded.gold); StringAssert.Contains("refunded", message);
                // v2 with an unknown id is rejected rather than guessed.
                carried.talents = new List<TalentRank> { new TalentRank { id = "foreign.node", rank = 1 } };
                store.Write("encounter", new SaveEnvelope { formatVersion = 2, payloadType = "CrulandaEncounter", payloadJson = JsonUtility.ToJson(carried) });
                Assert.IsFalse(save.Read(out _, out _));
                // v3 round trip, and v3 rejects ids outside the implemented tree.
                refunded.talents = new List<TalentRank> { new TalentRank { id = "sp-rally-reserve", rank = 1 } };
                save.Write(refunded);
                Assert.IsTrue(store.TryRead("encounter", out var envelope, out _)); Assert.AreEqual(EncounterSave.FormatVersion, envelope.formatVersion);
                Assert.IsTrue(save.Read(out var loaded, out message), message); Assert.AreEqual(1, TalentTree.Rank(loaded, "sp-rally-reserve"));
                loaded.talents.Add(new TalentRank { id = "tk-unbroken-line", rank = 1 }); loaded.experience = 240;
                store.Write("encounter", new SaveEnvelope { formatVersion = 3, payloadType = "CrulandaEncounter", payloadJson = JsonUtility.ToJson(loaded) });
                Assert.IsFalse(save.Read(out _, out _));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
