using System.Linq;
using Crulanda.Encounter;
using NUnit.Framework;

namespace Crulanda.Tests
{
    /// <summary>
    /// Points of interest and achievements (Achievements, 2026-10-03): over the real zones and quests, without a scene. Every
    /// landmark is a place to explore, once, paying more in the higher zones; each zone gets its explore, secrets, quests and
    /// elites achievements where it has any; earning is once, quiet on a loaded save, and titles can only be worn once earned.
    /// </summary>
    public sealed class AchievementTests
    {
        static Achievements Fresh(out EncounterProgress p)
        {
            p = new EncounterProgress();
            return new Achievements(p, LootTestData.Zones(), LootTestData.Quests());
        }

        [Test] public void Every_landmark_is_a_place_to_explore_once()
        {
            var a = Fresh(out var p);
            Assert.AreEqual(5, a.Zones.Count, "The five zones.");
            var oak = a.Zones.First(z => z.id == "zone.oakhaven"); var place = Achievements.Pois(oak).First();
            Assert.Greater(a.PoisIn(oak), 30, "Oakhaven's landmarks are its places.");
            Assert.IsFalse(a.Explored(oak, place));
            Assert.IsTrue(a.Explore(oak, place)); Assert.IsFalse(a.Explore(oak, place), "Once.");
            Assert.IsTrue(a.Explored(oak, place)); Assert.AreEqual(1, a.ExploredIn(oak));
            Assert.AreEqual("zone.oakhaven|" + place.name, p.explored[0]);
            var peaks = a.Zones.First(z => z.id == "zone.peaks");
            Assert.Greater(Achievements.PoiXp(peaks), Achievements.PoiXp(oak), "The higher zone pays more.");
            foreach (var z in a.Zones) foreach (var l in Achievements.Pois(z)) Assert.That(Achievements.Reach(l), Is.InRange(6f, 18f), z.id + " " + l.name);
        }

        [Test] public void Each_zone_has_its_deeds_and_the_world_its_titles()
        {
            var a = Fresh(out _);
            Assert.AreEqual(a.All.Count, a.All.Select(x => x.id).Distinct().Count(), "Ids are unique.");
            foreach (var z in a.Zones)
            {
                string s = z.id.Substring(5);
                Assert.IsTrue(a.All.Any(x => x.id == "explore." + s), z.id + " explore");
                Assert.IsTrue(a.All.Any(x => x.id == "quests." + s), z.id + " quests");
            }
            Assert.IsTrue(a.All.Any(x => x.id == "elites.peaks"), "The Peaks have camp elites (the captain, Old Scree-Tusk).");
            Assert.AreEqual(Achievements.EliteKeys(a.Zones.First(z => z.id == "zone.peaks")).Count, a.All.First(x => x.id == "elites.peaks").goal);
            Assert.IsTrue(a.All.Where(x => x.group == "Crulanda").All(x => x.goal > 0));
            Assert.IsNotEmpty(a.All.Where(x => x.title != null), "Some give titles.");
            Assert.Greater(a.Total, 200);
        }

        [Test] public void Earned_once_quietly_on_load_and_titles_worn_only_when_earned()
        {
            var a = Fresh(out var p); int earned = 0; a.Earned = x => earned++;
            Assert.IsEmpty(a.Check(), "Nothing done, nothing earned.");
            Assert.IsFalse(a.Wear("Bounty Hunter"), "Not earned: not worn."); Assert.IsNull(p.title);
            p.bounties = 10; var got = a.Check();
            Assert.AreEqual(1, got.Count); Assert.AreEqual("bounty.10", got[0].id); Assert.AreEqual(1, earned); Assert.AreEqual(10, a.Points);
            Assert.IsEmpty(a.Check(), "Once.");
            p.bounties = 50; p.rares = 1; a.Check(true);
            Assert.AreEqual(1, earned, "Quiet: recorded, no toast."); Assert.IsTrue(a.Has("bounty.50") && a.Has("rare.1"));
            Assert.IsTrue(a.Wear("Bounty Hunter")); Assert.AreEqual("Bounty Hunter", p.title);
            Assert.IsTrue(a.Wear(null)); Assert.IsNull(p.title);
            // Exploring every place in a zone earns its explorer.
            var oak = a.Zones.First(z => z.id == "zone.oakhaven");
            foreach (var l in Achievements.Pois(oak)) a.Explore(oak, l);
            Assert.IsTrue(a.Check().Any(x => x.id == "explore.oakhaven"));
            // A camp elite's kill is kept by its camp, whatever the mob's number.
            Assert.AreEqual("mob.captain.peaks.3", Achievements.EliteKey("mob.captain.peaks.3.0"));
            a.Slain("mob.captain.peaks.3.0"); a.Slain("mob.captain.peaks.3.1"); Assert.AreEqual(1, p.elitesSlain.Count);
        }
    }
}
