using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The camps' tricks (Chris, 2026-10-18: the fights "feel samey"; Docs/REWORK_LOWTOWNS_HALLOWS.md Lowtowns pick 7, Hallow's Creek pick 8;
    /// MobTricks, EncounterEnemy.Tricks): the brief's ten are there on the camps it names, each with a line that tells its answer, a canon
    /// label and a cite, trash numbers, a case in the code that lands it, and the rules each answer rests on. Pure data and files.
    /// </summary>
    public class MobTrickTests
    {
        static List<ZoneDefinition> zones;
        [OneTimeSetUp] public void Load() { zones = LootTestData.Zones(); }
        static IEnumerable<ZoneCamp> Camps(string zone) { return zones.First(z => z.id == zone).camps.Where(c => c != null && !c.elite && !string.IsNullOrEmpty(c.mob)); }

        [Test] public void The_briefs_ten_are_on_their_camps()
        {
            var want = new[] {
                ("zone.lowtowns", "Hired warder", "shieldline"), ("zone.lowtowns", "Iron Teeth sifter", "slate"), ("zone.lowtowns", "Gutter-hound", "circle"),
                ("zone.lowtowns", "Dock-rat cutter", "hook"), ("zone.lowtowns", "Grey-touched", "flash"),
                ("zone.hallows", "Bureau cutter", "clock"), ("zone.hallows", "Paused ringer", "echo"), ("zone.hallows", "Iron Pact crossbowman", "roof"),
                ("zone.hallows", "Iron Pact soldier", "shieldline"), ("zone.hallows", "Bureau clerk", "strike") };
            foreach (var (zone, mob, kind) in want)
            {
                var t = MobTricks.For(mob);
                Assert.IsNotNull(t, mob + " has a trick");
                Assert.AreEqual(kind, t.kind, mob);
                Assert.IsTrue(Camps(zone).Any(c => c.mob == mob), mob + " camps in " + zone);
            }
            Assert.AreEqual(want.Length, MobTricks.All.Length, "the ten and no more");
            Assert.Greater(MobTricks.For("Iron Pact soldier").metres, 0, "the Pact's spears reach further than the warders' cudgels");
            Assert.AreEqual(0, MobTricks.For("Hired warder").metres, "the warders have no spears");
        }
        [Test] public void One_a_mob_with_a_line_a_label_and_a_cite()
        {
            Assert.AreEqual(MobTricks.All.Length, MobTricks.All.Select(t => t.mob).Distinct().Count(), "one trick a mob");
            var ordinary = new HashSet<string>(zones.SelectMany(z => z.camps ?? new ZoneCamp[0]).Where(c => c != null && !c.elite && !string.IsNullOrEmpty(c.mob)).Select(c => c.mob));
            string[] machines = { "machine", "engine", "gear", "clockwork", "mechan", "contraption", "drill" };
            foreach (var t in MobTricks.All)
            {
                Assert.IsTrue(ordinary.Contains(t.mob), t.mob + " is an ordinary camp mob");
                CollectionAssert.Contains(MobTricks.Kinds, t.kind, t.mob);
                CollectionAssert.Contains(MobTricks.Statuses, t.canonStatus, t.mob);
                StringAssert.Contains("book 1 ch.", t.cite, t.mob + " cites the book");
                Assert.IsFalse(string.IsNullOrEmpty(t.name), t.mob);
                Assert.Greater(t.line != null ? t.line.Length : 0, 40, t.mob + "'s line tells what it is and its answer");
                foreach (var m in machines) Assert.IsFalse((t.name + " " + t.line).ToLowerInvariant().Contains(m), t.mob + ": no machines (" + m + ")");
            }
        }
        [Test] public void The_numbers_are_trash_numbers()
        {
            var bad = new List<string>();
            void Check(bool ok, MobTrick t, string what) { if (!ok) bad.Add(t.mob + " (" + t.name + "): " + what); }
            float fuse = MobCasts.For("Sandthrone sapper").power;
            foreach (var t in MobTricks.All)
            {
                Check(t.power >= 0 && t.power <= Mathf.Min(fuse, 1.6f), t, "power " + t.power);
                Check(t.share >= 0 && t.share <= .5f, t, "share " + t.share);
                Check(t.metres >= 0 && t.metres <= 3, t, "metres " + t.metres);
                Check(t.area <= 2.5f, t, "area " + t.area);
                Check(t.seconds <= (t.kind == "clock" ? 25 : 4), t, "seconds " + t.seconds);
                bool winds = t.kind == "slate" || t.kind == "hook" || t.kind == "flash" || t.kind == "strike" || t.kind == "roof";
                if (winds)
                {
                    Check(t.windup >= .6f && t.windup <= 3, t, "a wind-up to see coming " + t.windup);
                    Check(t.every >= 4 && t.every <= 20 && t.every > t.windup + 2, t, "every " + t.every);
                    Check(t.first >= 1 && t.first <= 8, t, "first " + t.first);
                }
            }
            Check(MobTricks.For("Bureau cutter").seconds >= 12, MobTricks.For("Bureau cutter"), "time enough to stop the cutters");
            Check(MobTricks.For("Paused ringer").windup >= 1.5f, MobTricks.For("Paused ringer"), "an echo late enough to step out of");
            Check(MobTricks.For("Gutter-hound").windup >= 1.5f, MobTricks.For("Gutter-hound"), "time to turn to a circling hound");
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }
        [Test] public void Every_kind_is_landed_by_the_trick_code()
        {
            string code = File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "Scripts", "Encounter", "EncounterEnemy.Tricks.cs"));
            string tick = code.Substring(code.IndexOf("bool TickTrick()", System.StringComparison.Ordinal));
            foreach (var k in MobTricks.Kinds) StringAssert.Contains("case \"" + k + "\":", tick, "TickTrick takes " + k);
            string enemy = File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "Scripts", "Encounter", "EncounterEnemy.cs"));
            StringAssert.Contains("TickTrick()", enemy, "the fight ticks the tricks");
            StringAssert.Contains("TrickTaken(damage, source)", enemy, "a blow taken goes past the shield and the dazzle");
            StringAssert.Contains("TrickReach", enemy, "the spear's reach");
            string social = File.ReadAllText(Path.Combine(Application.dataPath, "Crulanda", "Scripts", "Encounter", "EncounterSession.Social.cs"));
            StringAssert.Contains("MobTricks.For(camp.mob)", social, "a camp mob is given its trick");
        }
        [Test] public void The_shield_turns_only_a_frontal_blow_on_a_locked_line()
        {
            var fwd = Vector3.forward;
            Assert.IsTrue(MobTricks.Frontal(fwd, new Vector3(0, 0, 3)), "straight ahead");
            Assert.IsTrue(MobTricks.Frontal(fwd, new Vector3(1, 0, 3)), "a little aside");
            Assert.IsFalse(MobTricks.Frontal(fwd, new Vector3(3, 0, 0)), "the flank");
            Assert.IsFalse(MobTricks.Frontal(fwd, new Vector3(0, 0, -3)), "behind");
            Assert.AreEqual(50, MobTricks.ShieldTaken(100, true, true, .5f), "locked, frontal: half");
            Assert.AreEqual(100, MobTricks.ShieldTaken(100, true, false, .5f), "locked, from the flank: all");
            Assert.AreEqual(100, MobTricks.ShieldTaken(100, false, true, .5f), "alone, its shield unlocked: all");
            Assert.AreEqual(1, MobTricks.ShieldTaken(1, true, true, .5f), "never nothing");
        }
        [Test] public void A_hound_bites_only_from_behind_and_the_flash_misses_whoever_looks_away()
        {
            var fwd = Vector3.forward;
            Assert.IsTrue(MobTricks.Behind(fwd, new Vector3(0, 0, -2)), "at your back");
            Assert.IsFalse(MobTricks.Behind(fwd, new Vector3(0, 0, 2)), "in front: no back to bite");
            Assert.IsFalse(MobTricks.Behind(fwd, new Vector3(2, 0, 0)), "at your side");
            Assert.IsTrue(MobTricks.LookedAway(fwd, new Vector3(0, 0, -5)), "the flash behind you");
            Assert.IsFalse(MobTricks.LookedAway(fwd, new Vector3(0, 0, 5)), "facing the flash");
        }
        [Test] public void The_cutters_clock_stops_while_every_cutter_is_held()
        {
            Assert.AreEqual(17f, MobTricks.ClockLeft(18, 1, 2), 1e-5f, "a free cutter cuts");
            Assert.AreEqual(18f, MobTricks.ClockLeft(18, 1, 0), 1e-5f, "all held: the chisels stop");
            Assert.AreEqual(0f, MobTricks.ClockLeft(.5f, 1, 1), 1e-5f, "never below nothing");
        }
        [Test] public void Struck_from_the_record_takes_only_what_helps()
        {
            Assert.IsTrue(MobTricks.Harmful(EncounterEnemy.GreyId, 1.1f), "the grey");
            Assert.IsTrue(MobTricks.Harmful("status.anything", 1.2f), "anything that makes you take more");
            Assert.IsTrue(MobTricks.Harmful("status.river_poison", 1), "a poison");
            Assert.IsFalse(MobTricks.Harmful(Consumables.FedId, 1), "a meal");
            Assert.IsFalse(MobTricks.Harmful("status.blessing", .9f), "a ward");
        }
        [Test] public void The_docs_name_every_trick()
        {
            string docs = Path.Combine(Application.dataPath, "..", "Docs");
            // The validation copies hold only Assets (the docs live in the repo): read the repo's when the copy has none.
            if (!Directory.Exists(docs)) docs = @"D:\code\mmo\New Unity Project\Docs";
            if (!File.Exists(Path.Combine(docs, "ZONE_LOWTOWNS.md"))) Assert.Ignore("No Docs folder beside this copy of the project.");
            string lowtowns = File.ReadAllText(Path.Combine(docs, "ZONE_LOWTOWNS.md")), hallows = File.ReadAllText(Path.Combine(docs, "DUNGEON2_DESIGN.md"));
            foreach (var t in MobTricks.All)
                StringAssert.Contains(t.name, Camps("zone.lowtowns").Any(c => c.mob == t.mob) ? lowtowns : hallows, t.mob + "'s " + t.name + " is written up");
        }
    }
}
