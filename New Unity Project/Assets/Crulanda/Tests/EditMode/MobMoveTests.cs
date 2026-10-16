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
    /// Every ordinary mob has a signature move (Chris, 2026-10-15: "also most mobs dont seem to be cc or aoe still a thing?"; Docs/CC_DESIGN.md,
    /// "Signature moves for every mob"; MobCasts in EncounterEnemy.Casts.cs; GAME-ONLY): each non-elite camp mob in every zone has exactly
    /// one MobCast, its numbers are trash's (a short cast to interrupt, a hold of 1.5 to 3 s, no more damage than the sapper's fuse), every
    /// kind is one the cast code lands, and the party's holds from mobs diminish as a mob's do. Pure data and files: no scene, no session.
    /// </summary>
    public class MobMoveTests
    {
        static List<ZoneDefinition> zones;
        [OneTimeSetUp] public void Load() { zones = LootTestData.Zones(); }
        /// <summary>The Adit's seven casters from before the signature moves (2026-10-08 to 10-14), on their own older clocks.</summary>
        static readonly HashSet<string> Casters = new HashSet<string> { "Ash mender", "Ash initiate", "Sandthrone sapper", "Hollow Man", "Sandthrone picket", "Sandthrone carriage gunner", "Sandthrone harbour netter" };
        /// <summary>Looks that are beasts (and the grey crawlers, on the spider's body): their move is a wind-up of under a second, not a spell.</summary>
        static readonly HashSet<string> BeastLooks = new HashSet<string> { "wolf", "boar", "bear", "spider", "stag" };
        static IEnumerable<ZoneCamp> Ordinary() { return zones.SelectMany(z => z.camps ?? new ZoneCamp[0]).Where(c => c != null && !c.elite && !string.IsNullOrEmpty(c.mob)); }

        [Test] public void Every_zone_is_read()
        {
            var ids = zones.Select(z => z.id).ToList();
            foreach (var id in new[] { "zone.oakhaven", "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant", "zone.adit" }) CollectionAssert.Contains(ids, id);
        }
        [Test] public void Every_ordinary_camp_mob_has_exactly_one_move()
        {
            var bad = new List<string>();
            foreach (var mob in Ordinary().Select(c => c.mob).Distinct())
            {
                int n = MobCasts.All.Count(c => c.mob == mob);
                if (n != 1) bad.Add(mob + " has " + n);
                else if (!ReferenceEquals(MobCasts.For(mob), MobCasts.All.First(c => c.mob == mob))) bad.Add(mob + ": For finds another");
            }
            Assert.IsEmpty(bad, string.Join("; ", bad));
        }
        [Test] public void Every_move_names_a_camp_mob_that_is_there()
        {
            var mobs = new HashSet<string>(zones.SelectMany(z => z.camps ?? new ZoneCamp[0]).Where(c => c != null && !string.IsNullOrEmpty(c.mob)).Select(c => c.mob));
            var bad = MobCasts.All.Where(c => !mobs.Contains(c.mob)).Select(c => c.mob).ToList();
            Assert.IsEmpty(bad, "moves for nobody: " + string.Join(", ", bad));
            Assert.AreEqual(MobCasts.All.Length, MobCasts.All.Select(c => c.mob).Distinct().Count(), "one entry a mob");
        }
        [Test] public void The_numbers_are_trash_numbers()
        {
            var bad = new List<string>();
            void Check(bool ok, MobCast c, string what) { if (!ok) bad.Add(c.mob + " (" + c.name + "): " + what); }
            float fuse = MobCasts.For("Sandthrone sapper").power;
            foreach (var c in MobCasts.All)
            {
                Check(!string.IsNullOrEmpty(c.name), c, "a name");
                Check(MobCasts.Kinds.Contains(c.kind), c, "kind " + c.kind);
                Check(c.cast >= .6f && c.cast <= 3, c, "cast " + c.cast);
                Check(c.first >= 1 && c.first <= 6, c, "first " + c.first);
                Check(c.every > c.cast + 3, c, "every " + c.every + " leaves no time between casts");
                Check(Casters.Contains(c.mob) ? c.every >= 7 && c.every <= 18 : c.every >= 12 && c.every <= 18, c, "every " + c.every);
                Check(c.reach > 0 && c.reach <= 25, c, "reach " + c.reach);
                Check(c.knock >= 0 && c.knock <= 4, c, "knock " + c.knock);
                Check(c.knock == 0 || c.kind == "bolt" || c.kind == "blast", c, "a knock only on a bolt or a blast");
                if (c.kind == "net")
                {
                    Check(MobCasts.Holds.Contains(c.cc), c, "hold " + c.cc);
                    Check(c.seconds >= 1.5f && c.seconds <= 3, c, "held " + c.seconds + " s");
                    Check(c.area >= 0 && c.area <= 6, c, "area " + c.area);
                    Check(c.power == 0, c, "a net does no damage");
                }
                else
                {
                    Check(string.IsNullOrEmpty(c.cc) && c.seconds == 0 && c.area == 0, c, "a hold's numbers on a " + c.kind);
                    Check(c.kind == "heal" ? c.power > 0 && c.power <= .5f : c.power > 0 && c.power <= fuse, c, "power " + c.power);
                }
                if (c.kind == "blast") Check(c.reach <= 4.5f, c, "a blast's reach " + c.reach);
            }
            foreach (var camp in Ordinary().Where(x => BeastLooks.Contains(x.look)))
            {
                var c = MobCasts.For(camp.mob); if (c == null) continue;   // (Every_ordinary_camp_mob_has_exactly_one_move says so)
                Check(c.cast >= .6f && c.cast <= 1, c, "a beast's wind-up " + c.cast + " s");
            }
            Assert.IsEmpty(bad.Distinct().ToList(), string.Join("\n", bad.Distinct()));
        }
        /// <summary>Every kind a move names is one EncounterEnemy.Land has a case for, and every hold one EncounterSession.HoldPlayer takes
        /// (read from the code itself: a kind with no case would only log a warning in the game).</summary>
        [Test] public void Every_kind_is_landed_by_the_cast_code()
        {
            string scripts = Path.Combine(Application.dataPath, "Crulanda", "Scripts", "Encounter");
            string casts = File.ReadAllText(Path.Combine(scripts, "EncounterEnemy.Casts.cs")), stuns = File.ReadAllText(Path.Combine(scripts, "EncounterSession.Stuns.cs"));
            string land = casts.Substring(casts.IndexOf("void Land()", System.StringComparison.Ordinal));
            foreach (var k in MobCasts.Kinds) StringAssert.Contains("case \"" + k + "\":", land, "Land lands " + k);
            foreach (var k in MobCasts.Holds) StringAssert.Contains("case \"" + k + "\":", stuns, "HoldPlayer takes " + k);
            foreach (var c in MobCasts.All) CollectionAssert.Contains(MobCasts.Kinds, c.kind, c.mob);
        }
        /// <summary>CC_DESIGN.md section 1's rule, on the party: the same hold within 18 s of the last that took is half as long, a third time nothing.</summary>
        [Test] public void The_party_diminishes_a_mobs_hold()
        {
            const string me = "test:mob-moves:me", you = "test:mob-moves:you";
            Assert.AreEqual(2f, MobCasts.Diminished(me, "root", 2, 1000), 1e-5f, "the first: whole");
            Assert.AreEqual(1f, MobCasts.Diminished(me, "root", 2, 1005), 1e-5f, "again soon: half");
            Assert.AreEqual(0f, MobCasts.Diminished(me, "root", 2, 1010), 1e-5f, "a third time: nothing");
            Assert.AreEqual(2f, MobCasts.Diminished(me, "stun", 2, 1010), 1e-5f, "another kind is its own");
            Assert.AreEqual(2f, MobCasts.Diminished(you, "root", 2, 1010), 1e-5f, "another of the party is their own");
            Assert.AreEqual(2f, MobCasts.Diminished(me, "root", 2, 1005 + EncounterEnemy.DrWindow + .5f), 1e-5f, "past the window from the last that took: whole again");
        }
    }
}
