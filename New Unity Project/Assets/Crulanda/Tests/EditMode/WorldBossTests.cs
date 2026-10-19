using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;

namespace Crulanda.Tests
{
    /// <summary>
    /// The world boss's clock, its save and her rank (Docs/WORLD_BOSS_1.md 1.4, 3.1, 5.1; build step 3):
    /// - the clock: she rises once in every 3 hours of play, at a moment of the half hour after them; the warning comes 5 minutes before; she
    ///   stays 45 minutes, or until killed, or until she has wiped the raid twice; each ending starts the next 3 hours; paused time is not counted;
    /// - the world slot keeps the clock (format 3) and a format-2 world (or a format-1 one) migrates with a fresh clock, everything else as it was;
    /// - her rank: "Raid (10)", the +5 clamp kept (x1.6 hit, x0.7 taken at 30 against 20), her health 160,000 after the clamp, and no blow over
    ///   60% of a member's health unless it carries the cap-passing flag.
    /// </summary>
    public class WorldBossTests
    {
        const float Step = 1;
        /// <summary>Ticks a second at a time until a beat comes (or <paramref name="limit"/> seconds pass): the beat and when it came.</summary>
        static (WorldBossClock.Beat beat, float at) RunTo(WorldBossState s, int seed, float limit)
        {
            for (float t = Step; t <= limit; t += Step) { var b = WorldBossClock.Tick(s, Step, seed); if (b != WorldBossClock.Beat.None) return (b, t); }
            return (WorldBossClock.Beat.None, limit);
        }

        [Test] public void She_rises_once_in_three_hours_of_play_in_the_half_hour_after_them()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                float rise = WorldBossClock.RollRise(seed, 0);
                Assert.That(rise, Is.InRange(3 * 3600f, 3 * 3600f + 30 * 60f), "seed " + seed);
                Assert.AreEqual(rise, WorldBossClock.RollRise(seed, 0), "the same for a world's seed and cycle");
            }
            Assert.AreNotEqual(WorldBossClock.RollRise(5, 0), WorldBossClock.RollRise(5, 1), "a new moment each cycle");
            var s = new WorldBossState(); Assert.AreEqual(WorldBossState.Waiting, s.state);
            var (beat, at) = RunTo(s, 7, 4 * 3600);
            Assert.AreEqual(WorldBossClock.Beat.Warned, beat, "the warning first");
            Assert.AreEqual(s.riseAt - 5 * 60, at, 1.01f, "five minutes before she rises");
            Assert.GreaterOrEqual(at, 3 * 3600 - 5 * 60 - 1, "never before the 3 hours are near");
            Assert.AreEqual(WorldBossState.Warned, s.state);
            var (rose, after) = RunTo(s, 7, 600);
            Assert.AreEqual(WorldBossClock.Beat.Rose, rose); Assert.AreEqual(5 * 60, after, 1.01f, "she rises five minutes after the warning");
            Assert.AreEqual(WorldBossState.Up, s.state); Assert.AreEqual(0, WorldBossClock.UntilRise(s));
        }

        [Test] public void She_stays_forty_five_minutes_then_sinks_and_the_next_three_hours_start()
        {
            var s = new WorldBossState(); RunTo(s, 3, 4 * 3600); RunTo(s, 3, 600); Assert.AreEqual(WorldBossState.Up, s.state);
            Assert.AreEqual(45 * 60, WorldBossClock.UpLeft(s), 1.01f);
            var (beat, at) = RunTo(s, 3, 3600);
            Assert.AreEqual(WorldBossClock.Beat.Sank, beat); Assert.AreEqual(45 * 60, at, 1.01f, "45 minutes up");
            Assert.AreEqual(WorldBossState.Waiting, s.state); Assert.AreEqual(0, s.played, "the next 3 hours from nothing");
            Assert.AreEqual(1, s.cycles); Assert.AreEqual(WorldBossClock.RollRise(3, 1), s.riseAt, "the next cycle's moment");
            Assert.AreEqual(0, s.kills);
            Assert.AreEqual(WorldBossClock.Beat.Warned, RunTo(s, 3, 4 * 3600).beat, "and she comes again");
        }

        [Test] public void A_kill_or_two_wipes_end_her_rising_and_paused_time_is_not_counted()
        {
            var s = new WorldBossState(); RunTo(s, 9, 4 * 3600); RunTo(s, 9, 600);
            Assert.IsFalse(WorldBossClock.Wiped(s, 9), "one wipe: she stays"); Assert.AreEqual(WorldBossState.Up, s.state); Assert.AreEqual(1, s.wipes);
            Assert.IsTrue(WorldBossClock.Wiped(s, 9), "the second: she sinks, fed"); Assert.AreEqual(WorldBossState.Waiting, s.state); Assert.AreEqual(0, s.wipes);
            Assert.IsFalse(WorldBossClock.Wiped(s, 9), "no wipe counts while she is not up");
            RunTo(s, 9, 4 * 3600); RunTo(s, 9, 600); Assert.AreEqual(WorldBossState.Up, s.state);
            WorldBossClock.Tick(s, 600, 9);
            WorldBossClock.Killed(s, 9);
            Assert.AreEqual(WorldBossState.Waiting, s.state); Assert.AreEqual(1, s.kills); Assert.AreEqual(0, s.played); Assert.AreEqual(0, s.upFor);
            float before = s.played;
            Assert.AreEqual(WorldBossClock.Beat.None, WorldBossClock.Tick(s, 0, 9), "a paused frame counts nothing");
            Assert.AreEqual(before, s.played);
            // A long frame that passes the warning and the rising gives the warning now and the rising next.
            var t = new WorldBossState();
            Assert.AreEqual(WorldBossClock.Beat.Warned, WorldBossClock.Tick(t, 5 * 3600, 2));
            Assert.AreEqual(WorldBossClock.Beat.Rose, WorldBossClock.Tick(t, .1f, 2));
        }

        [Test] public void The_world_slot_keeps_her_clock()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-worldboss-" + Guid.NewGuid().ToString("N"));
            try
            {
                var w = SimRoster.LoadOrCreate(root);
                w.unraveller.state = WorldBossState.Up; w.unraveller.played = 11000.5f; w.unraveller.riseAt = 11500; w.unraveller.upFor = 600.25f;
                w.unraveller.wipes = 1; w.unraveller.cycles = 4; w.unraveller.kills = 2;
                SimRoster.Save(root, w);
                Assert.IsTrue(new SaveFileStore(root).TryRead("world", out var env, out _));
                Assert.AreEqual(3, env.formatVersion, "the world slot is format 3"); Assert.AreEqual(WorldSave.FormatVersion, env.formatVersion);
                var back = SimRoster.Load(root, out var msg); Assert.NotNull(back, msg);
                var u = back.unraveller;
                Assert.AreEqual(WorldBossState.Up, u.state); Assert.AreEqual(11000.5f, u.played, 1e-3f); Assert.AreEqual(11500, u.riseAt, 1e-3f);
                Assert.AreEqual(600.25f, u.upFor, 1e-3f); Assert.AreEqual(1, u.wipes); Assert.AreEqual(4, u.cycles); Assert.AreEqual(2, u.kills);
                Assert.AreEqual(w.sims.Count, back.sims.Count, "and the rest as it was");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void A_format_2_world_migrates_with_a_fresh_clock()
        {
            Assert.AreEqual("{\"seed\":5,\"unraveller\":{}}", new AddWorldBossMigration().Migrate("{\"seed\":5}"), "only the field added");
            Assert.AreEqual("{\"unraveller\":{}}", new AddWorldBossMigration().Migrate("{}"));
            Assert.AreEqual("{\"seed\":5,\"unraveller\":{}}", new AddWorldBossMigration().Migrate("{\"seed\":5,\"unraveller\":{}}"), "left alone when there");
            Assert.Throws<SaveMigrationException>(() => new AddWorldBossMigration().Migrate("[]"));
            Assert.AreEqual(2, new AddWorldBossMigration().FromVersion); Assert.AreEqual(3, new AddWorldBossMigration().ToVersion);
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-worldboss-" + Guid.NewGuid().ToString("N"));
            try
            {
                var w = SimRoster.LoadOrCreate(root); w.sims[2].level = 7; w.party.Add(w.sims[1].id); w.exchange.hours = 4.5f;
                // The world as a format-2 build wrote it: everything but the clock.
                string json = JsonUtility.ToJson(w); int cut = json.IndexOf(",\"unraveller\":", StringComparison.Ordinal); Assert.Greater(cut, 0);
                string v2 = json.Substring(0, cut) + "}";
                var store = new SaveFileStore(root);
                store.Write("world", new SaveEnvelope { formatVersion = 2, payloadType = WorldSave.PayloadType, gameVersion = "0.3.0", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = v2 });
                var back = SimRoster.Load(root, out var msg); Assert.NotNull(back, msg);
                Assert.AreEqual(w.sims.Count, back.sims.Count); Assert.AreEqual(7, back.sims[2].level); Assert.AreEqual(w.sims[1].id, back.party.Single()); Assert.AreEqual(w.seed, back.seed);
                Assert.AreEqual(4.5f, back.exchange.hours, 1e-4f, "the Exchange comes through");
                Assert.NotNull(back.unraveller); Assert.AreEqual(WorldBossState.Waiting, back.unraveller.state, "a fresh clock: waiting");
                Assert.AreEqual(0, back.unraveller.played); Assert.Less(back.unraveller.riseAt, 0, "its moment rolled at the first tick");
                Assert.AreEqual(WorldBossClock.Beat.None, WorldBossClock.Tick(back.unraveller, 1, back.seed));
                Assert.AreEqual(WorldBossClock.RollRise(back.seed, 0), back.unraveller.riseAt, "the first 3 hours start on the first play");
                // Saved again, it is format 3; a format-1 world comes up through both steps.
                SimRoster.Save(root, back); Assert.IsTrue(store.TryRead("world", out var env, out _)); Assert.AreEqual(3, env.formatVersion);
                int ex = json.IndexOf(",\"exchange\":", StringComparison.Ordinal); Assert.Greater(ex, 0);
                store.Write("world", new SaveEnvelope { formatVersion = 1, payloadType = WorldSave.PayloadType, gameVersion = "0.3.0", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = json.Substring(0, ex) + "}" });
                var old = SimRoster.Load(root, out msg); Assert.NotNull(old, msg); Assert.NotNull(old.exchange); Assert.AreEqual(WorldBossState.Waiting, old.unraveller.state);
                // A newer world than this build knows is refused, not read.
                store.Write("world", new SaveEnvelope { formatVersion = 4, payloadType = WorldSave.PayloadType, gameVersion = "9", savedAtUtc = DateTime.UtcNow.ToString("O"), payloadJson = json });
                Assert.IsNull(SimRoster.Load(root, out msg));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Test] public void Her_rank_is_a_raid_s_with_the_five_level_clamp_kept()
        {
            Assert.AreEqual("Raid (10)", EncounterEnemy.RankWords(Unraveller.RaidSize));
            Assert.AreEqual(30, Unraveller.Level); Assert.AreEqual(10, Unraveller.RaidSize);
            Assert.AreEqual(1.6f, EncounterEnemy.OvermatchHit(Unraveller.Level, EncounterProgress.LevelCap), 1e-4f, "+10 counts +5: she hits x1.6");
            Assert.AreEqual(.7f, EncounterEnemy.OvermatchTaken(Unraveller.Level, EncounterProgress.LevelCap), 1e-4f, "and takes x0.7");
            Assert.AreEqual(160000, Unraveller.EffectiveHealth);
            Assert.AreEqual(112000, Unraveller.HealthPool, "her pool: 160,000 after the clamp's x0.7");
            // No blow over 60% of a member's health, unless it carries the cap-passing flag; a blow on someone outside the party lands whole.
            Assert.AreEqual(600, EncounterEnemy.CapBlow(2500, 1000, true, false), "capped at three fifths");
            Assert.AreEqual(2500, EncounterEnemy.CapBlow(2500, 1000, true, true), "a blow meant to kill passes the cap");
            Assert.AreEqual(2500, EncounterEnemy.CapBlow(2500, 1000, false, false), "not one of your party: whole");
            Assert.AreEqual(400, EncounterEnemy.CapBlow(400, 1000, true, false), "under the cap: as it is");
        }
    }
}
