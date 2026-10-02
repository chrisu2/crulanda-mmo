using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The rules of social aggro and the elites' moves against the five zones' data (playtest notes 2 and 3): every camp has a
    /// sensible kind (by its data or its look), the reaches are what the design says, a call has words for every people that
    /// calls, the guard camps are the ones that stand beside their elite, and each of the twelve elites has its own move.
    /// Pure logic and files: no scene, no session and no save.
    /// </summary>
    public class SocialRulesTests
    {
        static List<ZoneDefinition> zones;
        [OneTimeSetUp] public void Load() { zones = LootTestData.Zones(); }
        static IEnumerable<(ZoneDefinition zone, ZoneCamp camp, int index)> Camps()
        {
            foreach (var z in zones) for (int c = 0; c < (z.camps ?? new ZoneCamp[0]).Length; c++) if (z.camps[c] != null) yield return (z, z.camps[c], c);
        }
        // Every camp tag in the five zones and the kind it must come out as. A new tag has to be put here: nothing is left to chance.
        static readonly Dictionary<string, SocialKind> KindByTag = new Dictionary<string, SocialKind> {
            { "wolf", SocialKind.Pack }, { "hound", SocialKind.Pack }, { "weaveeater", SocialKind.Pack }, { "brood", SocialKind.Pack }, { "spider", SocialKind.Pack }, { "bramble", SocialKind.Pack },
            { "boar", SocialKind.Solitary }, { "mossboar", SocialKind.Solitary }, { "stag", SocialKind.Solitary }, { "ninebranch", SocialKind.Solitary },
            { "deserter", SocialKind.Call }, { "banditking", SocialKind.Call }, { "quartermaster", SocialKind.Call }, { "outrider", SocialKind.Call }, { "tollguard", SocialKind.Call },
            { "captain", SocialKind.Call }, { "cultist", SocialKind.Call }, { "deacon", SocialKind.Call }, { "hollow", SocialKind.Call }, { "sexton", SocialKind.Call }, { "pale", SocialKind.Call },
            { "paleshadow", SocialKind.Call }, { "mistwalker", SocialKind.Call }, { "deepwalker", SocialKind.Call }, { "withered", SocialKind.Call }, { "deepwithered", SocialKind.Call },
            { "greyheart", SocialKind.Call }, { "rootwarden", SocialKind.Call }
        };

        [Test] public void Every_camp_in_the_five_zones_has_a_sensible_kind()
        {
            Assert.AreEqual(5, zones.Count, "Five zones.");
            int camps = 0;
            foreach (var (zone, camp, _) in Camps())
            {
                camps++; string where = zone.id + " / " + camp.name;
                Assert.IsTrue(SocialAggro.ValidSocial(camp.social), where + ": social is '" + camp.social + "' (pack, call, solitary or empty).");
                Assert.IsTrue(KindByTag.TryGetValue(camp.tag ?? "", out var expected), where + ": tag '" + camp.tag + "' has no kind in this test's table. Decide whether it packs, calls or stays single.");
                Assert.AreEqual(expected, SocialAggro.KindFor(camp), where + " (look " + camp.look + ").");
            }
            Assert.GreaterOrEqual(camps, 50, "All the camps were read.");
            // Wolves and hounds pack, boar and stags stay single, people call: the owner's three examples.
            Assert.AreEqual(SocialKind.Pack, SocialAggro.KindFor(null, "wolf")); Assert.AreEqual(SocialKind.Solitary, SocialAggro.KindFor(null, "boar"));
            Assert.AreEqual(SocialKind.Solitary, SocialAggro.KindFor(null, "stag")); Assert.AreEqual(SocialKind.Call, SocialAggro.KindFor(null, "deserter"));
            Assert.AreEqual(SocialKind.Call, SocialAggro.KindFor(null, "cultist")); Assert.AreEqual(SocialKind.Call, SocialAggro.KindFor("", null), "No look at all is a Concord collector: people.");
        }

        [Test] public void The_data_can_say_a_camps_kind_and_its_look_decides_otherwise()
        {
            Assert.AreEqual(SocialKind.Solitary, SocialAggro.KindFor("solitary", "wolf"), "A lone wolf, if the data says so.");
            Assert.AreEqual(SocialKind.Pack, SocialAggro.KindFor("Pack", "boar")); Assert.AreEqual(SocialKind.Call, SocialAggro.KindFor(" call ", "boar"));
            Assert.AreEqual(SocialKind.Pack, SocialAggro.KindFor("nonsense", "wolf"), "An unknown word falls back to the look.");
            Assert.IsFalse(SocialAggro.ValidSocial("nonsense")); Assert.IsTrue(SocialAggro.ValidSocial(null)); Assert.IsTrue(SocialAggro.ValidSocial("pack"));
            var camp = new ZoneCamp { look = "wolf", social = "solitary" };
            Assert.AreEqual(SocialKind.Solitary, SocialAggro.KindFor(camp));
        }

        [Test] public void Reach_by_kind_and_a_sneaking_puller_peels_the_edge()
        {
            Assert.AreEqual(0, SocialAggro.Reach(SocialKind.Solitary, false)); Assert.AreEqual(0, SocialAggro.Reach(SocialKind.Solitary, true));
            Assert.AreEqual(SocialAggro.PackReach, SocialAggro.Reach(SocialKind.Pack, false)); Assert.AreEqual(SocialAggro.CallReach, SocialAggro.Reach(SocialKind.Call, false));
            Assert.AreEqual(SocialAggro.SneakReach, SocialAggro.Reach(SocialKind.Pack, true)); Assert.AreEqual(SocialAggro.SneakReach, SocialAggro.Reach(SocialKind.Call, true));
            Assert.Less(SocialAggro.SneakReach, 5, "Shorter than the 5 m a mob notices you from: a mob beside the one you were noticed by can stay asleep.");
            Assert.Greater(SocialAggro.PackReach, 5); Assert.Greater(SocialAggro.CallReach, SocialAggro.PackReach, "A shout carries further than a pack's glance.");
            Assert.Greater(SocialAggro.GuardReach, SocialAggro.CallReach, "An elite's guards come from further than a call carries.");
            Assert.Greater(SocialAggro.CallBeat, .5f, "The beat after a shout is long enough to read.");
        }

        [Test] public void Every_people_that_calls_has_words_and_kin_are_shared_across_camps()
        {
            Assert.AreEqual(SocialAggro.Kin("deserter"), SocialAggro.Kin("banditking"), "Caddock hears his deserters.");
            Assert.AreNotEqual(SocialAggro.Kin("deserter"), SocialAggro.Kin("outrider"), "Deserters are no longer Sandthrone's.");
            Assert.AreEqual(SocialAggro.Kin("collector"), SocialAggro.Kin("warden")); Assert.AreEqual(SocialAggro.Kin(null), SocialAggro.Kin("collector"));
            var plain = SocialAggro.Shout("nobody at all", 0);
            foreach (var (zone, camp, _) in Camps())
            {
                if (SocialAggro.KindFor(camp) != SocialKind.Call) continue;
                string kin = SocialAggro.Kin(camp.look);
                for (int pick = 0; pick < 4; pick++)
                {
                    var (chat, over) = SocialAggro.Shout(kin, pick);
                    Assert.AreNotEqual(plain.chat, chat, zone.id + " / " + camp.name + " (kin " + kin + ") has its own call, not the plain one.");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(over), "A word to float over the caller."); Assert.IsTrue(chat.StartsWith(" "), "The chat line follows the caller's name.");
                }
            }
            Assert.AreEqual(SocialAggro.Shout("deserters", 1), SocialAggro.Shout("deserters", 1 + 3 * 7), "Any number picks one of the lines.");
        }

        [Test] public void Guard_camps_are_the_ones_beside_their_elite()
        {
            var pairs = new List<string>();
            foreach (var z in zones)
            {
                var guards = SocialAggro.GuardCamps(z.camps);
                Assert.AreEqual(z.camps.Length, guards.Length);
                for (int e = 0; e < guards.Length; e++)
                {
                    if (guards[e] == null) continue;
                    Assert.IsTrue(z.camps[e].elite, "Only an elite's camp has guards.");
                    foreach (int c in guards[e]) { Assert.IsFalse(z.camps[c].elite, "An elite guards nobody."); pairs.Add(z.camps[e].name + " <- " + z.camps[c].name); }
                }
            }
            // The king and the guard before his throne, the quartermaster and his store's men, the Deacon and his pit, Greyheart
            // and the Greying, the Root-Warden and the Heart's withered. The other seven elites stand alone or with their own camp.
            CollectionAssert.AreEquivalent(new[] {
                "Caddock's hall <- King's guard", "The Quartermaster's desk <- Store Caves", "The Ash-Deacon <- Ash-pit cultists",
                "Greyheart <- The Greying", "The Root-Warden <- The Heart's withered" }, pairs,
                "If a camp moved, pair it in the data: \"guards\" on the elite's camp names its guard camps.");
        }

        [Test] public void The_data_can_pair_or_unpair_guards()
        {
            ZoneCamp Camp(string name, float x, bool elite = false, string guards = null) { return new ZoneCamp { name = name, center = new Vector2(x, 0), radius = 3, elite = elite, guards = guards }; }
            // By nearness: the camp whose edge is within GuardPairing of the elite's centre.
            var near = SocialAggro.GuardCamps(new[] { Camp("lord", 0, true), Camp("close", 3 + SocialAggro.GuardPairing - .5f), Camp("far", 3 + SocialAggro.GuardPairing + .5f) });
            CollectionAssert.AreEqual(new[] { 1 }, near[0]); Assert.IsNull(near[1]); Assert.IsNull(near[2]);
            // By name: the far camp guards, the close one does not.
            var named = SocialAggro.GuardCamps(new[] { Camp("lord", 0, true, "far, another"), Camp("close", 5), Camp("far", 40) });
            CollectionAssert.AreEqual(new[] { 2 }, named[0]);
            // "none": nobody.
            Assert.IsNull(SocialAggro.GuardCamps(new[] { Camp("lord", 0, true, "none"), Camp("close", 5) })[0]);
            Assert.AreEqual(0, SocialAggro.GuardCamps(null).Length);
        }

        [Test] public void Each_of_the_twelve_elites_has_its_own_move()
        {
            var elites = Camps().Where(x => x.camp.elite).ToList();
            Assert.AreEqual(12, elites.Count, "Twelve camp elites in the five zones.");
            Assert.AreEqual(12, EliteMoves.All.Length);
            var plain = EliteMoves.For("nobody at all", false);
            foreach (var (zone, camp, _) in elites)
            {
                var m = EliteMoves.For(camp.mob, false);
                Assert.AreNotSame(plain, m, zone.id + " / " + camp.mob + " has a move of its own.");
                Assert.AreEqual(camp.mob, m.mob);
                Assert.IsFalse(string.IsNullOrWhiteSpace(m.name)); Assert.IsFalse(string.IsNullOrWhiteSpace(m.enrage), camp.mob + " has a line for its enrage.");
                Assert.That(m.windup, Is.InRange(1.4f, 3f), camp.mob + ": long enough to see and step out of, short enough to matter.");
                Assert.Greater(m.reach, 3.3f, camp.mob + ": the mark reaches past the player's own 3.2 m, so the answer is to step away, not to stand at arm's length.");
                Assert.Less(m.reach, 5.2f * (m.windup - .5f), camp.mob + ": a player at 5.2 m/s gets out with half a second to react.");
                Assert.Greater(m.blow, 3f, camp.mob + ": a blow worth answering."); Assert.Greater(m.every, m.windup + 5, camp.mob + ": swings between the blows.");
                Assert.That(m.enrageAt, Is.InRange(.2f, .4f)); Assert.Less(m.enrageHaste, .8f, "Enraged, it swings faster.");
                Assert.Greater(m.callAt, m.enrageAt, "It calls before it enrages.");
            }
            Assert.AreEqual(12, EliteMoves.All.Select(m => m.name).Distinct().Count(), "Twelve different moves.");
            // Dungeon end bosses are a step harder than outdoor named elites: Caddock and the Hollow Root-Warden, and only they.
            CollectionAssert.AreEquivalent(new[] { "Caddock, the Bandit King", "The Hollow Root-Warden" }, EliteMoves.All.Where(m => m.boss).Select(m => m.mob).ToList());
            foreach (var m in EliteMoves.All) { Assert.AreEqual(m.boss ? EliteMoves.BossHealth : 1, m.health, m.mob); Assert.AreEqual(m.boss ? EliteMoves.BossHit : 1, m.hit, m.mob); }
            Assert.Greater(EliteMoves.BossHealth, 1.1f); Assert.Greater(EliteMoves.BossHit, 1.05f);
            // Solitary beasts call nobody; the rest have a call to make.
            foreach (var (_, camp, _) in elites)
                Assert.AreEqual(SocialAggro.KindFor(camp) == SocialKind.Solitary, string.IsNullOrEmpty(EliteMoves.For(camp.mob, false).call), camp.mob + ": a call only if it is not a solitary beast.");
            // An elite the table does not know still has a blow: a lunge for a beast.
            Assert.AreNotSame(plain, EliteMoves.For("nobody at all", true)); Assert.IsFalse(string.IsNullOrEmpty(plain.name));
        }

        [Test] public void Overmatch_only_counts_levels_above_the_player()
        {
            Assert.AreEqual(1, EncounterEnemy.OvermatchHit(5, 5)); Assert.AreEqual(1, EncounterEnemy.OvermatchHit(5, 9)); Assert.AreEqual(1, EncounterEnemy.OvermatchTaken(5, 7));
            Assert.AreEqual(1 + 2 * EncounterEnemy.OverHit, EncounterEnemy.OvermatchHit(7, 5), 1e-5f); Assert.AreEqual(1 - 2 * EncounterEnemy.OverTough, EncounterEnemy.OvermatchTaken(7, 5), 1e-5f);
            Assert.AreEqual(EncounterEnemy.OvermatchHit(10, 5), EncounterEnemy.OvermatchHit(13, 1), 1e-5f, "Five levels is the most it counts.");
            Assert.Greater(EncounterEnemy.OvermatchTaken(13, 1), .5f);
        }

        [Test] public void Normal_mobs_keep_their_numbers_and_elites_have_new_ones()
        {
            // "Normal mobs alone stay as they are": the level curve is the one the game had before the notes.
            Assert.AreEqual(120, EncounterEnemy.MobHealth(1, false, false, false)); Assert.AreEqual(240, EncounterEnemy.MobHealth(5, false, false, false));
            Assert.AreEqual(204, EncounterEnemy.MobHealth(5, false, false, true), "Beasts a little lighter.");
            Assert.AreEqual(9, EncounterEnemy.MobHit(1, false, false), 1e-4f); Assert.AreEqual(17.8f, EncounterEnemy.MobHit(5, false, false), 1e-4f);
            // Story enemies and their veterans too.
            Assert.AreEqual(Mathf.RoundToInt(240 * 1.6f * 1.4f), EncounterEnemy.MobHealth(5, true, true, false)); Assert.AreEqual(17.8f * 1.2f * 1.4f, EncounterEnemy.MobHit(5, true, true), 1e-3f);
            // A camp elite was 2.2 times the health and 1.4 times the hit of a normal mob of its level.
            for (int level = 1; level <= EncounterProgress.LevelCap; level++)
            {
                Assert.Greater(EncounterEnemy.MobHealth(level, false, true, false), 2.2f * 2 * EncounterEnemy.MobHealth(level, false, false, false), "Level " + level + ": more than twice the old elite's health.");
                Assert.Greater(EncounterEnemy.MobHit(level, false, true), 1.4f * 2 * EncounterEnemy.MobHit(level, false, false), "Level " + level + ": more than twice the old elite's hit.");
            }
            // Mira keeps pace: her heal at the cap is well over twice her heal at level 1.
            Assert.AreEqual(42, HealerCompanion.HealFor(42, 1)); Assert.Greater(HealerCompanion.HealFor(42, EncounterProgress.LevelCap), 2 * 42);
        }
    }
}
