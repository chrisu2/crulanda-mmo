using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The sims' gear (Phase 5.2 round 2): the same every time, at their level, mail, leather or cloth by class, weapons only for the Warrior and the Paladin.</summary>
    public class SimGearTests
    {
        /// <summary>The cap of 20 (2026-10-16): a sim past the Shore has a home band (the Lowtowns, 15-20) and wears the 16-20 band's gear.</summary>
        [Test] public void Sims_have_a_home_and_gear_to_the_cap()
        {
            var top = SimRoster.Homes[SimRoster.Homes.Length - 1];
            Assert.AreEqual("zone.lowtowns", top.zone); Assert.AreEqual("Lowtown folk", top.folk); Assert.AreEqual(15, top.lo); Assert.AreEqual(EncounterProgress.LevelCap, top.hi);
            for (int level = 1; level <= EncounterProgress.LevelCap; level++) Assert.IsTrue(SimRoster.Homes.Any(h => level >= h.lo && level <= h.hi), "a home for level " + level);
            var items = new ItemDatabase(); var s = Sim("class.warrior", 19, 4471);
            var chest = SimGear.For(s, items).Select(items.Get).First(d => d.slot == "chest");
            Assert.AreEqual(18, chest.level); Assert.IsTrue(new[] { "Soot-stained", "Tar-sealed", "Lamp-lit" }.Any(w => chest.name.StartsWith(w)), chest.name);
        }
        static SimAdventurer Sim(string classId, int level, int seed) { return new SimAdventurer { id = "sim.t", name = "Test", classId = classId, level = level, gearSeed = seed }; }

        [Test] public void Gear_suits_the_class_and_the_level_and_never_changes()
        {
            var items = new ItemDatabase();
            foreach (var classId in SimRoster.ClassIds)
                foreach (int level in new[] { 1, 6, 12, 18, EncounterProgress.LevelCap })   // to the cap of 20 (2026-10-16)
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
            Assert.Greater(Rares(EncounterProgress.LevelCap), 0, "and at the cap of 20");
        }
    }
}
