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
        /// <summary>
        /// The party's tank is its toughest member (2026-10-16, the Hallow's Creek run at 19: the Warrior tank had 568 health and 329 armour
        /// to the Paladin player's 607 and 375). Its health is a player Warrior's at its level (Encounter.asset) x1.09 (Scarred Veteran),
        /// over any class's own at every level; with its gear's Stamina counted (Chris, 2026-10-16: "Fix it now") it is over every other
        /// class's sim of the same seed and purse; it owns every piece, rare from level 8, and with its edge its armour is over a Paladin's
        /// pieces of the same seed, and over the run's player (a Paladin dressed as a sim of seed 7 with two upgrades bought) whatever its
        /// seed, at 11-20.
        /// </summary>
        [Test] public void The_warrior_tank_is_the_toughest_of_a_party()
        {
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset"); Assert.NotNull(content);
            var classes = content.AllClasses().Select(c => c.definition).ToList(); var warrior = classes.First(c => c.id == "class.warrior");
            var items = new ItemDatabase();
            for (int level = 1; level <= EncounterProgress.LevelCap; level++)
            {
                int tank = SimCompanion.MaxHealthFor(Sim("class.warrior", level, 3));
                Assert.AreEqual(UnityEngine.Mathf.RoundToInt((warrior.stats.baseHealth + warrior.stats.healthPerLevel * (level - 1)) * SimCompanion.TankHealthEdge), tank, "a player Warrior's health at " + level + ", with its edge");
                foreach (var c in classes) Assert.GreaterOrEqual(tank, c.stats.baseHealth + c.stats.healthPerLevel * (level - 1), c.id + " at " + level);
                foreach (int seed in new[] { 0, 5, 7, 13, 4471, 98123 })
                    foreach (int bonus in new[] { 0, 2 })
                    {
                        var w = Sim("class.warrior", level, seed); w.gearBonus = bonus;
                        Assert.AreEqual(9, SimGear.For(w, items).Count, "the tank owns all seven pieces, a weapon and a shield");
                        int wHealth = SimCompanion.MaxHealthFor(w, items);
                        Assert.Greater(wHealth, tank, "its gear's Stamina counts at " + level);
                        foreach (var id in SimRoster.ClassIds)
                        {
                            if (id == "class.warrior") continue;
                            var o = Sim(id, level, seed); o.gearBonus = bonus;
                            Assert.Greater(wHealth, SimCompanion.MaxHealthFor(o, items), id + " of seed " + seed + " at " + level);
                        }
                        var p = Sim("class.paladin", level, seed); p.gearBonus = bonus;
                        Assert.Greater(SimCompanion.ArmourFor(w, items), SimCompanion.PiecesArmour(p, items), "over a Paladin's pieces at " + level);
                    }
                if (level < 11) continue;
                var player = Sim("class.paladin", level, 7); player.gearBonus = 2; int you = SimCompanion.PiecesArmour(player, items);
                for (int seed = 0; seed < 200; seed++) { var w = Sim("class.warrior", level, seed); w.gearBonus = 2; Assert.GreaterOrEqual(SimCompanion.ArmourFor(w, items), you, "seed " + seed + " at " + level); }
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
