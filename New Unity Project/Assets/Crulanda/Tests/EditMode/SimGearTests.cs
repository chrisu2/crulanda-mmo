using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The sims' gear (Phase 5.2 round 2): the same every time, at their level, mail, leather or cloth by class, weapons only for the Warrior and the Paladin.</summary>
    public class SimGearTests
    {
        static SimAdventurer Sim(string classId, int level, int seed) { return new SimAdventurer { id = "sim.t", name = "Test", classId = classId, level = level, gearSeed = seed }; }

        [Test] public void Gear_suits_the_class_and_the_level_and_never_changes()
        {
            var items = new ItemDatabase();
            foreach (var classId in SimRoster.ClassIds)
                foreach (int level in new[] { 1, 6, 12 })
                    foreach (int seed in new[] { 3, 4471, 98123 })
                    {
                        var s = Sim(classId, level, seed); var ids = SimGear.For(s, items);
                        CollectionAssert.AreEqual(ids, SimGear.For(s, items), "the same every time");
                        Assert.GreaterOrEqual(ids.Count, 4, classId + " " + level + " wears most slots");
                        var defs = ids.Select(items.Get).ToList();
                        Assert.IsTrue(defs.All(d => d != null && d.kind == "gear"), "every id is real gear");
                        Assert.IsTrue(defs.All(d => d.level >= level - 2 && d.level <= level), "at its level");
                        Assert.AreEqual(defs.Count, defs.Select(d => d.slot).Distinct().Count(), "one per slot");
                        bool weapon = defs.Any(d => d.slot == "mainhand");
                        Assert.AreEqual(SimGear.CarriesWeapon(classId), weapon, classId + ": a weapon only for the Warrior and the Paladin");
                        var chest = defs.FirstOrDefault(d => d.slot == "chest"); Assert.NotNull(chest, "a chest piece");
                        string want = SimGear.Weight(classId) == "heavy" ? "Hauberk" : SimGear.Weight(classId) == "leather" ? "Jerkin" : "Tunic";
                        StringAssert.Contains(want, chest.name, classId);
                    }
        }
        [Test] public void Quality_rises_with_level()
        {
            int Rares(int level) { int n = 0; for (int seed = 0; seed < 200; seed++) for (int slot = 0; slot < 7; slot++) if (SimGear.QualityFor(Sim("class.mage", level, seed), slot) >= 3) n++; return n; }
            Assert.AreEqual(0, Rares(3), "no rares low down");
            Assert.Greater(Rares(10), 0, "some rares high up");
        }
    }
}
