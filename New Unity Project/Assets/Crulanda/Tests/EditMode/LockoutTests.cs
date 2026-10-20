using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.Persistence;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Lockouts (Chris, 2026-10-15: "bosses/elites will not respawn so no one is trying to exploit the gear/loot drops"; "Until a lockout
    /// resets", "Everywhere"; the dungeon queue's "every 2 days for loot"; "open world boss is once per week"; "raids once per week"): the
    /// table of kinds (a raid's week, a dungeon's two days, an open-world boss's week, an elite's day), the six o'clock reset, a kill inside a
    /// lockout not pushing it back, the save round trip and the format-11 step with the new list, a locked boss rolling no loot, and the quest
    /// exception (a boss a quest you hold still needs stays, without loot). Pure logic: no scene.
    /// </summary>
    public class LockoutTests
    {
        /// <summary>A fixed zone five hours behind UTC (no clock changes), so the tests do not hang on the machine's own time zone.</summary>
        static readonly TimeZoneInfo East = TimeZoneInfo.CreateCustomTimeZone("Crulanda-Test-5", TimeSpan.FromHours(-5), "Test -5", "Test -5");
        /// <summary>A local time in <see cref="East"/> as UTC.</summary>
        static DateTime At(int day, int hour, int minute = 0) { return new DateTime(2026, 10, day, hour, minute, 0, DateTimeKind.Utc).AddHours(5); }
        static DateTime LocalOf(DateTime utc) { return TimeZoneInfo.ConvertTimeFromUtc(utc, East); }
        Func<DateTime> clock; TimeZoneInfo zone;
        [SetUp] public void Keep() { clock = Lockouts.UtcNow; zone = Lockouts.Local; Lockouts.Local = East; }
        [TearDown] public void Restore() { Lockouts.UtcNow = clock; Lockouts.Local = zone; }

        [Test] public void The_table_of_kinds()
        {
            Assert.AreEqual(7, Lockouts.Raid.days, "raids once a week");
            Assert.AreEqual(2, Lockouts.Dungeon.days, "a dungeon every two days");
            Assert.AreEqual(7, Lockouts.WorldBoss.days, "an open-world boss once a week");
            Assert.AreEqual(1, Lockouts.WorldElite.days, "an open-world elite once a day");
            CollectionAssert.AreEqual(new[] { Lockouts.Raid, Lockouts.Dungeon, Lockouts.WorldBoss, Lockouts.WorldElite }, Lockouts.Kinds);
            Assert.AreSame(Lockouts.Raid, Lockouts.KindFor(new ZoneDefinition { id = "zone.fakeraid", raid = true, dungeon = true }, false), "a raid zone's flag wins over its dungeon flag");
            Assert.AreSame(Lockouts.Raid, Lockouts.KindFor(new ZoneDefinition { id = "zone.fakeraid", raid = true }, true));
            Assert.AreSame(Lockouts.Dungeon, Lockouts.KindFor(new ZoneDefinition { id = "zone.adit", dungeon = true }, true), "a dungeon's boss and elites alike");
            Assert.AreSame(Lockouts.Dungeon, Lockouts.KindFor(new ZoneDefinition { id = "zone.adit", dungeon = true }, false));
            Assert.AreSame(Lockouts.WorldBoss, Lockouts.KindFor(new ZoneDefinition { id = "zone.oakhaven" }, true), "Caddock: a boss's move out in the world");
            Assert.AreSame(Lockouts.WorldElite, Lockouts.KindFor(new ZoneDefinition { id = "zone.peaks" }, false), "the Sandthrone captain");
            Assert.AreSame(Lockouts.WorldElite, Lockouts.KindFor(null, false));
            Assert.IsTrue(JsonUtility.FromJson<ZoneDefinition>("{\"id\":\"zone.fakeraid\",\"raid\":true}").raid, "the zone flag reads from the zone's data");
            // Every boss and elite the game ships has a kind: every dungeon's is a dungeon's, and only a boss's move makes a world boss.
            foreach (var m in EliteMoves.All) Assert.NotNull(Lockouts.KindFor(false, false, m.boss), m.mob);
        }

        /// <summary>The zones of Phase 9 lock by their own flags and moves, with no table of their own (the session's LockKindOf): Hallow's
        /// Creek a dungeon (two days, bosses and elites alike), the Lowtowns' three open-world elites a day (none has a boss's move), the
        /// Crown none (a city: no camps); and any open-world elite with a boss's move a week.</summary>
        [Test] public void The_zones_of_phase_nine_lock_by_their_kind()
        {
            var zones = new Dictionary<string, ZoneDefinition>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Crulanda", "EncounterContent", "Zones"), "*.json")) { var z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(f)); zones[z.id] = z; }
            LockoutKind Kind(ZoneDefinition z, ZoneCamp c) { return Lockouts.KindFor(z, EliteMoves.For(c.mob, false).boss); }
            var hallows = zones["zone.hallows"]; Assert.IsTrue(hallows.dungeon, "Hallow's Creek is a dungeon");
            int n = 0; foreach (var c in hallows.camps) if (c != null && c.elite) { n++; Assert.AreSame(Lockouts.Dungeon, Kind(hallows, c), c.mob + ": two days"); }
            Assert.AreEqual(15, n, "Hallow's Creek's fifteen");
            var low = zones["zone.lowtowns"]; Assert.IsFalse(low.dungeon || low.raid, "the Lowtowns are open world");
            n = 0; foreach (var c in low.camps) if (c != null && c.elite) { n++; Assert.AreSame(Lockouts.WorldElite, Kind(low, c), c.mob + ": a day"); }
            Assert.AreEqual(3, n, "the Mother of Drains, Knuckle Brannoc and the Lantern-Eater");
            Assert.IsEmpty(zones["zone.crown"].camps, "the Crown: nothing to lock");
            foreach (var z in zones.Values)
                if (!z.dungeon && !z.raid)
                    foreach (var c in z.camps) if (c != null && c.elite && EliteMoves.For(c.mob, false).boss) Assert.AreSame(Lockouts.WorldBoss, Kind(z, c), c.mob + ": an open-world boss, a week");
        }

        [Test] public void Keys_are_the_camps()
        {
            Assert.AreEqual("mob.captain.peaks.3", Lockouts.KeyOf("mob.captain.peaks.3.0"), "the camp's elite, as the deeds count it");
            Assert.AreEqual("mob.rockeater.adit.m21", Lockouts.KeyOf("mob.rockeater.adit.m21.0"), "the beast under Nix: its own");
            Assert.IsNull(Lockouts.KeyOf("sentry.2"), "a story enemy is saved dead for good, not locked");
            Assert.IsNull(Lockouts.KeyOf(null)); Assert.IsNull(Lockouts.KeyOf(""));
            Assert.AreEqual("peaks", Lockouts.ZoneOf("mob.captain.peaks.3")); Assert.AreEqual("fakeraid", Lockouts.ZoneOf("mob.warlord.fakeraid.2"));
        }

        /// <summary>The first six o'clock after the kill, local, then a day for each further day of the kind.</summary>
        [Test] public void Lockouts_reset_at_six_in_the_morning()
        {
            // A day's lockout.
            Assert.AreEqual(At(16, 6), Lockouts.ResetAfter(At(15, 23), 1, East), "killed at 23:00: back at 06:00 the next morning");
            Assert.AreEqual(At(16, 6), Lockouts.ResetAfter(At(15, 7), 1, East), "killed at 07:00: back at 06:00 the morning after");
            Assert.AreEqual(At(15, 6), Lockouts.ResetAfter(At(15, 5, 59), 1, East), "killed a minute before six: back at six");
            Assert.AreEqual(At(16, 6), Lockouts.ResetAfter(At(15, 6), 1, East), "killed on the stroke of six: the next six");
            // A dungeon's two days: a Monday evening's kill (2026-10-12 is a Monday) comes back on Wednesday at six.
            Assert.AreEqual(DayOfWeek.Monday, new DateTime(2026, 10, 12).DayOfWeek);
            var wed = Lockouts.ResetAfter(At(12, 20), 2, East); Assert.AreEqual(At(14, 6), wed); Assert.AreEqual(DayOfWeek.Wednesday, LocalOf(wed).DayOfWeek);
            // A week: seven resets on.
            Assert.AreEqual(At(19, 6), Lockouts.ResetAfter(At(12, 20), 7, East));
            Assert.AreEqual(6, LocalOf(Lockouts.ResetAfter(At(12, 20), 7, East)).Hour, "always at six, local");
            // Kept in UTC: six in the morning five hours behind is eleven UTC.
            Assert.AreEqual(11, Lockouts.ResetAfter(At(15, 23), 1, East).Hour); Assert.AreEqual(DateTimeKind.Utc, Lockouts.ResetAfter(At(15, 23), 1, East).Kind);
            Assert.AreEqual(new DateTime(2026, 10, 16, 6, 0, 0, DateTimeKind.Utc), Lockouts.ResetAfter(new DateTime(2026, 10, 15, 22, 0, 0, DateTimeKind.Utc), 1, TimeZoneInfo.Utc));
        }

        [Test] public void A_lockout_holds_until_it_resets_and_a_kill_inside_it_does_not_push_it_back()
        {
            var p = EncounterSession.FreshProgress(); string key = "mob.lusk.adit.5";
            Assert.IsFalse(Lockouts.Locked(p, key, At(15, 20))); Assert.IsEmpty(p.lockouts, "a new character has none");
            Assert.IsTrue(Lockouts.Lock(p, key, Lockouts.Dungeon, At(15, 20), East));
            Assert.AreEqual(1, p.lockouts.Count); Assert.AreEqual(At(17, 6).Ticks, p.lockouts[0].until, "two days: Saturday's six from a Thursday evening");
            Assert.IsTrue(Lockouts.Locked(p, key, At(16, 23))); Assert.IsTrue(Lockouts.Locked(p, key, At(17, 5, 59)));
            Assert.IsFalse(Lockouts.Locked(p, key, At(17, 6)), "free at six");
            Assert.AreEqual(TimeSpan.FromHours(34), Lockouts.Left(p, key, At(15, 20)));
            Assert.AreEqual("1 d 10 h", Lockouts.Words(Lockouts.Left(p, key, At(15, 20))));
            Assert.AreEqual("2 h 30 m", Lockouts.Words(TimeSpan.FromMinutes(150))); Assert.AreEqual("9 m", Lockouts.Words(TimeSpan.FromMinutes(9))); Assert.AreEqual("under a minute", Lockouts.Words(TimeSpan.FromSeconds(20)));
            // Killed again inside it (a queued run's fresh dungeon): the lockout stays as it was.
            Assert.IsFalse(Lockouts.Lock(p, key, Lockouts.Dungeon, At(16, 20), East)); Assert.AreEqual(At(17, 6).Ticks, p.lockouts[0].until);
            // An elite out in the world, a world boss, and a raid's boss under a fake raid's id.
            Assert.IsTrue(Lockouts.Lock(p, "mob.captain.peaks.3", Lockouts.WorldElite, At(15, 20), East));
            Assert.IsTrue(Lockouts.Lock(p, "mob.caddock.oakhaven.8", Lockouts.WorldBoss, At(16, 7), East));
            Assert.IsTrue(Lockouts.Lock(p, "mob.warlord.fakeraid.2", Lockouts.KindFor(new ZoneDefinition { id = "zone.fakeraid", raid = true }, true), At(15, 20), East));
            Assert.AreEqual(At(16, 6).Ticks, Lockouts.Find(p, "mob.captain.peaks.3", At(15, 20)).until, "a day");
            Assert.AreEqual(At(23, 6).Ticks, Lockouts.Find(p, "mob.caddock.oakhaven.8", At(15, 20)).until, "a week (killed the next morning, after six)");
            Assert.AreEqual(At(22, 6).Ticks, Lockouts.Find(p, "mob.warlord.fakeraid.2", At(15, 20)).until, "a raid's week");
            var running = Lockouts.Running(p, At(15, 20));
            CollectionAssert.AreEqual(new[] { "mob.captain.peaks.3", "mob.lusk.adit.5", "mob.warlord.fakeraid.2", "mob.caddock.oakhaven.8" }, running.ConvertAll(r => r.key), "soonest first");
            // Run out, then pruned; after it ends a kill locks it afresh.
            Assert.AreEqual(1, Lockouts.Prune(p, At(16, 7)), "the captain's day is over"); Assert.AreEqual(3, p.lockouts.Count);
            Assert.AreEqual(1, Lockouts.Prune(p, At(18, 7)), "Lusk's two days are over"); Assert.AreEqual(2, p.lockouts.Count, "the weeks run on");
            Assert.IsTrue(Lockouts.Lock(p, key, Lockouts.Dungeon, At(18, 7), East)); Assert.AreEqual(At(20, 6).Ticks, Lockouts.Find(p, key, At(18, 7)).until);
            // The session's clock and zone are the statics.
            Lockouts.UtcNow = () => At(19, 0); Assert.IsTrue(Lockouts.Locked(p, key)); Lockouts.UtcNow = () => At(20, 6); Assert.IsFalse(Lockouts.Locked(p, key));
        }

        /// <summary>A boss or elite locked for you rolls no loot (a queued run's fresh dungeon, a quest's kill); an ordinary mob always does,
        /// and so does the boss once its lockout has run out.</summary>
        [Test] public void A_locked_boss_rolls_no_loot()
        {
            var p = EncounterSession.FreshProgress(); string boss = "mob.lusk.adit.5.0", pack = "mob.picket.adit.5.1";
            Assert.IsTrue(Lockouts.RollsLoot(p, boss, true, At(15, 20)), "not locked yet: its loot");
            Lockouts.Lock(p, Lockouts.KeyOf(boss), Lockouts.Dungeon, At(15, 20), East);
            Assert.IsFalse(Lockouts.RollsLoot(p, boss, true, At(15, 21)), "locked: nothing");
            Assert.IsTrue(Lockouts.RollsLoot(p, pack, false, At(15, 21)), "his guards are ordinary mobs: their loot, and their 20-minute respawn");
            Assert.IsTrue(Lockouts.RollsLoot(p, boss, true, At(17, 6)), "the lockout over: his loot again");
            Assert.IsTrue(Lockouts.RollsLoot(p, "mob.captain.peaks.3.0", true, At(15, 21)), "another boss's lockout is its own");
        }

        /// <summary>The lockouts ride the save (format 11): written, read back the same; a blank key, an expiry of zero and a repeat dropped on
        /// load; a key this build does not know kept.</summary>
        [Test] public void Lockouts_round_trip_through_the_save()
        {
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-lockouts-" + Guid.NewGuid().ToString("N"));
            try
            {
                var p = EncounterSession.FreshProgress();
                Lockouts.Lock(p, "mob.lusk.adit.5", Lockouts.Dungeon, At(15, 20), East);
                Lockouts.Lock(p, "mob.captain.peaks.3", Lockouts.WorldElite, At(15, 20), East);
                p.lockouts.Add(new Lockout { id = "", until = 5 }); p.lockouts.Add(new Lockout { id = "mob.gone.nowhere.1", until = 0 });
                p.lockouts.Add(new Lockout { id = "mob.lusk.adit.5", until = 9 }); p.lockouts.Add(new Lockout { id = "mob.unknown.fakeraid.2", until = At(22, 6).Ticks });
                var save = new EncounterSave(root, TestTalents.Warrior()); save.Write(p);
                Assert.IsTrue(new SaveFileStore(root).TryRead("encounter", out var envelope, out _));
                Assert.AreEqual(11, envelope.formatVersion, "format 11, with the recipes: no twelfth");
                StringAssert.Contains("\"lockouts\":[{\"id\":\"mob.lusk.adit.5\",\"until\":" + At(17, 6).Ticks + "}", envelope.payloadJson, "UTC ticks");
                StringAssert.Contains("],\"recipes\":[],\"discoveries\":[]", envelope.payloadJson, "before the recipes, so the recipes still come before the discoveries");
                StringAssert.EndsWith("\"lootLuck\":[]}", envelope.payloadJson, "a payload still ends as it did");
                Assert.IsTrue(save.Read(out var loaded, out var message), message);
                Assert.AreEqual(3, loaded.lockouts.Count, "the blank, the zero and the repeat dropped");
                Assert.AreEqual("mob.lusk.adit.5", loaded.lockouts[0].id); Assert.AreEqual(At(17, 6).Ticks, loaded.lockouts[0].until, "the first of a repeat is kept");
                Assert.AreEqual("mob.captain.peaks.3", loaded.lockouts[1].id); Assert.AreEqual("mob.unknown.fakeraid.2", loaded.lockouts[2].id, "a key this content does not know is kept");
                Assert.IsTrue(Lockouts.Locked(loaded, "mob.lusk.adit.5", At(16, 12)));
                save.Write(loaded); Assert.IsTrue(save.Read(out var again, out message), message);
                Assert.AreEqual(JsonUtility.ToJson(loaded), JsonUtility.ToJson(again), "and it reads back the same");
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        /// <summary>Format 10 → 11 still works with the new list: the step adds the recipes and the lockouts (only what is missing), the
        /// owner's format-10 save loads with both empty and everything else as it was, and a format-11 payload needs nothing from it.</summary>
        [Test] public void A_format_10_save_migrates_to_11_with_the_lockouts()
        {
            var step = new AddRecipesMigration(); Assert.AreEqual(10, step.FromVersion); Assert.AreEqual(11, step.ToVersion); Assert.AreEqual(11, EncounterSave.FormatVersion);
            string v10 = new SteeperCurveMigration().Migrate(new AddArmouryMigration().Migrate(OwnerSaveFixtures.V8Payload)), v11 = step.Migrate(v10);
            Assert.AreEqual(v10.Substring(0, v10.Length - 1) + ",\"recipes\":[],\"lockouts\":[]}", v11, "every character of the v10 payload kept");
            Assert.AreEqual(v11, step.Migrate(v11));
            Assert.AreEqual("{\"lockouts\":[],\"recipes\":[]}", step.Migrate("{\"lockouts\":[]}"), "a payload with the lockouts but not the recipes gains only the recipes");
            string root = Path.Combine(Path.GetTempPath(), "Crulanda-lockouts-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveFileStore(root);
                store.Write("encounter", new SaveEnvelope { formatVersion = 10, payloadType = "CrulandaEncounter", gameVersion = "0.3.0", savedAtUtc = "2026-10-15T20:00:00Z", payloadJson = v10 });
                string onDisk = File.ReadAllText(store.PathFor("encounter"));
                var save = new EncounterSave(root, TestTalents.Warrior());
                Assert.IsTrue(save.Read(out var p, out var message), message);
                Assert.AreEqual(onDisk, File.ReadAllText(store.PathFor("encounter")), "read in memory: the file untouched");
                Assert.NotNull(p.lockouts); Assert.IsEmpty(p.lockouts); Assert.NotNull(p.recipes); Assert.IsEmpty(p.recipes);
                Lockouts.Lock(p, "mob.lusk.adit.5", Lockouts.Dungeon, At(15, 20), East);
                save.Write(p); Assert.IsTrue(save.Read(out var again, out message), message);
                Assert.AreEqual(1, again.lockouts.Count); Assert.AreEqual(At(17, 6).Ticks, again.lockouts[0].until);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        /// <summary>
        /// The quest exception (the safe way): a boss whose kill a quest you hold still needs (a kill objective of the step you are on, or of
        /// a step still to come; a "collect ... from kill:" item you lack) stays in the world though it is locked, and drops no loot: the quest
        /// can always be done, the loot cannot be farmed. Once the quest has its kill, the boss is kept away again.
        /// </summary>
        [Test] public void A_boss_a_quest_still_needs_stays_without_loot()
        {
            var db = QuestDatabase.Parse(new[] { "{\"items\":[{\"id\":\"qi.captains_seal\",\"name\":\"The captain's seal\"}],\"quests\":[" +
                "{\"id\":\"side.peaks.captain\",\"title\":\"The Captain\",\"kind\":\"side\",\"giver\":\"Warden\",\"turnIn\":\"Warden\",\"canonStatus\":\"GAME-ONLY\",\"steps\":[" +
                "{\"objectives\":[{\"type\":\"talk\",\"target\":\"Scout\",\"count\":1}]}," +
                "{\"objectives\":[{\"type\":\"kill\",\"target\":\"mob.captain.peaks.3.*\",\"count\":1}]}]}," +
                "{\"id\":\"side.peaks.seal\",\"title\":\"The Seal\",\"kind\":\"side\",\"giver\":\"Warden\",\"turnIn\":\"Warden\",\"canonStatus\":\"GAME-ONLY\",\"steps\":[" +
                "{\"objectives\":[{\"type\":\"collect\",\"target\":\"kill:mob.scree.peaks.8.*\",\"item\":\"qi.captains_seal\",\"count\":1}]}]}]}" });
            var p = EncounterSession.FreshProgress(); var log = new QuestLog(db, p);
            string captain = "mob.captain.peaks.3.0", scree = "mob.scree.peaks.8.0";
            Lockouts.Lock(p, Lockouts.KeyOf(captain), Lockouts.WorldElite, At(15, 20), East); Lockouts.Lock(p, Lockouts.KeyOf(scree), Lockouts.WorldElite, At(15, 20), East);
            Assert.IsFalse(log.StillNeedsKill(captain), "no quest held: kept away by its lockout");
            p.quests.Add(new QuestState { id = "side.peaks.captain", step = 0 });
            Assert.IsTrue(log.StillNeedsKill(captain), "a step still to come wants him: he stands");
            Assert.IsFalse(log.StillNeedsKill("mob.picket.peaks.4.0"), "another camp's mob is nothing to the quest");
            log.TalkTo("Scout"); Assert.AreEqual(1, log.State("side.peaks.captain").step);
            Assert.IsTrue(log.StillNeedsKill(captain), "the step you are on wants him");
            Assert.IsFalse(Lockouts.RollsLoot(p, captain, true, At(15, 21)), "he stands for the quest, but his loot stays locked");
            log.Notify("kill", captain);
            Assert.IsFalse(log.StillNeedsKill(captain), "the quest has its kill: he is kept away again");
            // A quest item from a locked kill: needed while you lack it, and given by the quest (QuestLog.LootFrom), not the loot.
            p.quests.Add(new QuestState { id = "side.peaks.seal", step = 0 });
            Assert.IsTrue(log.StillNeedsKill(scree)); Assert.IsFalse(Lockouts.RollsLoot(p, scree, true, At(15, 21)));
            Assert.AreEqual("qi.captains_seal", log.LootFrom(scree), "the quest's item still comes off the locked kill");
            Assert.IsFalse(log.StillNeedsKill(scree), "held: not needed any more");
        }
    }
}
