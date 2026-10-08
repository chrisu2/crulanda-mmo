using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>The sims' roster (Phase 5.2 round 1): twenty, the same for a seed, named and levelled to their homes, saved in the world slot and read back whole.</summary>
    public class SimRosterTests
    {
        [Test] public void Forty_sims_the_same_for_a_seed_with_unique_names_and_every_class()
        {
            var a = SimRoster.Generate(7); var b = SimRoster.Generate(7); var c = SimRoster.Generate(8);
            Assert.AreEqual(SimRoster.Count, a.Count);
            Assert.AreEqual(a.Count, a.Select(s => s.id).Distinct().Count(), "ids unique");
            Assert.AreEqual(a.Count, a.Select(s => s.name).Distinct().Count(), "names unique");
            CollectionAssert.AreEqual(a.Select(s => s.name).ToArray(), b.Select(s => s.name).ToArray(), "the same seed gives the same twenty");
            Assert.AreNotEqual(string.Join(",", a.Select(s => s.name)), string.Join(",", c.Select(s => s.name)), "another seed gives others");
            foreach (var id in SimRoster.ClassIds) Assert.GreaterOrEqual(a.Count(s => s.classId == id), 3, id);
            foreach (var s in a)
            {
                var home = SimRoster.Homes.First(h => h.zone == s.homeZone);
                Assert.That(s.level, Is.InRange(home.lo, home.hi), s.name + " is levelled for " + s.homeZone);
                Assert.AreEqual(s.homeZone, s.zone, "starts at home");
                Assert.That(s.onlineHours, Is.InRange(4, 24)); Assert.That(s.bold, Is.InRange(0, 1));
            }
            Assert.GreaterOrEqual(a.Count(s => s.homeZone == "zone.oakhaven"), 6, "most of them where the player starts");
        }
        /// <summary>Round 27: a world of twenty (made before) grows to forty, the old ones kept as they are, the new mostly higher; every sim gets its guild once.</summary>
        [Test] public void An_old_world_grows_to_forty_and_each_sim_gets_its_guild_once()
        {
            var old = new WorldSave { seed = 11, sims = SimRoster.Generate(11).Take(20).ToList() };
            old.sims[0].level = 9; old.sims[0].name = old.sims[0].name + "x";
            Assert.IsTrue(SimRoster.Grow(old)); Assert.AreEqual(SimRoster.Count, old.sims.Count);
            Assert.AreEqual(9, old.sims[0].level, "the old twenty are kept as they are");
            Assert.AreEqual(old.sims.Count, old.sims.Select(s => s.id).Distinct().Count(), "ids unique");
            Assert.AreEqual(old.sims.Count, old.sims.Select(s => s.name).Distinct().Count(), "names unique");
            Assert.GreaterOrEqual(old.sims.Skip(20).Count(s => s.level >= 7), 12, "the new twenty mostly at the higher levels");
            Assert.IsFalse(SimRoster.Grow(old), "grown once");
            Assert.IsTrue(SimGuilds.Assign(old)); var first = old.sims.Select(s => s.guild).ToArray();
            Assert.IsFalse(SimGuilds.Assign(old), "assigned once"); CollectionAssert.AreEqual(first, old.sims.Select(s => s.guild).ToArray());
            Assert.That(old.sims.Count(s => s.guild != ""), Is.InRange(16, 36), "about two in three in a guild");
            foreach (var s in old.sims) Assert.IsTrue(s.guild == "" || SimGuilds.Exists(s.guild), s.guild);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int seed = 1; seed <= 10; seed++) { var w = new WorldSave { seed = seed, sims = SimRoster.Generate(seed) }; SimGuilds.Assign(w); foreach (var s in w.sims) seen.Add(s.guild); }
            foreach (var g in SimGuilds.All) Assert.IsTrue(seen.Contains(g.name), g.name + " has members");
            Assert.NotNull(SimGuilds.Leader(old, old.sims.First(s => s.guild != "").guild));
        }
        [Test] public void Online_hours_wrap_past_midnight()
        {
            var s = new SimAdventurer { onlineFrom = 20, onlineHours = 6 };
            Assert.IsTrue(s.IsOnlineAt(21)); Assert.IsTrue(s.IsOnlineAt(1)); Assert.IsFalse(s.IsOnlineAt(3)); Assert.IsFalse(s.IsOnlineAt(12));
            Assert.IsTrue(new SimAdventurer { onlineFrom = 0, onlineHours = 24 }.IsOnlineAt(13));
        }
        [Test] public void The_world_slot_round_trips_and_rejects_rubbish()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-world-" + Guid.NewGuid().ToString("N"));
            try
            {
                Assert.IsNull(SimRoster.Load(root, out _), "nothing yet");
                var w = SimRoster.LoadOrCreate(root);
                Assert.AreEqual(SimRoster.Count, w.sims.Count);
                w.sims[3].x = 12.5f; w.sims[3].z = -4; w.sims[3].level = 9; SimRoster.Save(root, w);
                var back = SimRoster.Load(root, out var msg); Assert.NotNull(back, msg);
                Assert.AreEqual(w.seed, back.seed); Assert.AreEqual(w.sims[3].name, back.sims[3].name); Assert.AreEqual(9, back.sims[3].level); Assert.AreEqual(12.5f, back.sims[3].x, 1e-4f);
                Assert.AreEqual(back.sims[3].name, SimRoster.LoadOrCreate(root).sims[3].name, "LoadOrCreate keeps what is saved");
                // Rubbish in the file: the store falls back to its .bak; with that gone too, nothing is read and a message says why.
                File.WriteAllText(Path.Combine(root, "world.save.json"), "{not json");
                Assert.NotNull(SimRoster.Load(root, out msg), "the backup still reads");
                File.WriteAllText(Path.Combine(root, "world.save.json.bak"), "{not json either");
                Assert.IsNull(SimRoster.Load(root, out msg)); Assert.IsFalse(string.IsNullOrEmpty(msg));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
