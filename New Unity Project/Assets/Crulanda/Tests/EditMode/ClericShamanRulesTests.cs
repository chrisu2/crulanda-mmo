using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The Cleric's and the Shaman's trees (2026-10-16, the cap of 20): each loads exactly the talents its kit builds (Renewing
    /// Ward built at last), four paths of 21 ranks; tier 3's small conditional bonuses read as written (5% under 35% health; a broken
    /// Ward mends 4% of it a rank).</summary>
    public class ClericShamanRulesTests
    {
        static string Json(string cls) { return File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda/EncounterContent/Talents/" + cls + ".json")); }

        [Test] public void Cleric_tree_loads_exactly_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.cleric", Json("cleric"), 20, ClericKit.ImplementedIds);
            Assert.AreEqual(ClericKit.ImplementedIds.Length, t.Branches.Sum(b => b.nodes.Length));
            foreach (var b in t.Branches) Assert.AreEqual(21, b.nodes.Sum(n => n.max), b.id + ": 21 ranks");
            Assert.NotNull(t.Find("wd-renew"), "Renewing Ward is built");
        }
        [Test] public void Shaman_tree_loads_exactly_the_implemented_talents()
        {
            var t = TalentTree.Parse("class.shaman", Json("shaman"), 20, ShamanKit.ImplementedIds);
            Assert.AreEqual(ShamanKit.ImplementedIds.Length, t.Branches.Sum(b => b.nodes.Length));
            foreach (var b in t.Branches) Assert.AreEqual(21, b.nodes.Sum(n => n.max), b.id + ": 21 ranks");
        }
        [Test] public void Tier_three_bonuses_are_small_and_conditional()
        {
            Assert.AreEqual(1f, ClericKit.LampLow(1, .5f), 1e-5f); Assert.AreEqual(1.05f, ClericKit.LampLow(1, .3f), 1e-5f); Assert.AreEqual(1f, ClericKit.LampLow(0, .3f), 1e-5f);
            Assert.AreEqual(1f, ShamanKit.SpringTideOn(1, .35f), 1e-5f); Assert.AreEqual(1.05f, ShamanKit.SpringTideOn(1, .2f), 1e-5f);
            Assert.AreEqual(0, ClericKit.RenewMend(200, 0)); Assert.AreEqual(8, ClericKit.RenewMend(200, 1)); Assert.AreEqual(24, ClericKit.RenewMend(200, 3));
        }
    }
}
