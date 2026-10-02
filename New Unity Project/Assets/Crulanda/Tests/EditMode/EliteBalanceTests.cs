using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Crulanda.Combat;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The tuning of elites and packs, shown with numbers (playtest note 2). EliteBalance fights each of the twelve camp elites on
    /// paper with the class kits' own numbers (read from Encounter.asset), for a Warrior and a Druid (Barkhide and Thornclaw) in
    /// on-curve uncommon gear, with and without Mira, careless (standing in the heavy blow) and careful (stepping out or guarding).
    /// The targets:
    /// - an elite of the player's level kills a careless player who is alone, whatever the class;
    /// - with Mira and careful play it falls, with health to spare;
    /// - a careful Warrior alone does not get it down: it wants Mira;
    /// - an elite two levels above the player is not a solo kill for any kit, however careful;
    /// - a dungeon's end boss is a step harder than an outdoor named elite of its level;
    /// - a normal mob alone is as easy as it was, and four of them at once are a real pull.
    /// The whole table goes to the log (ELITE_BALANCE) so it can be read after a run. Pure logic: no scene and no save.
    /// </summary>
    public class EliteBalanceTests
    {
        static readonly EliteBalance.Kit[] Kits = { EliteBalance.Kit.Warrior, EliteBalance.Kit.Barkhide, EliteBalance.Kit.Thornclaw };
        static EncounterContent content; static EliteBalance.Numbers numbers; static List<(ZoneCamp camp, EliteMove move, bool beast)> elites;
        [OneTimeSetUp] public void Load()
        {
            content = UnityEditor.AssetDatabase.LoadAssetAtPath<EncounterContent>("Assets/Crulanda/EncounterContent/Encounter.asset");
            Assert.NotNull(content, "Encounter.asset is there.");
            numbers = EliteBalance.Numbers.From(content);
            elites = new List<(ZoneCamp, EliteMove, bool)>();
            foreach (var z in LootTestData.Zones())
                foreach (var c in z.camps ?? new ZoneCamp[0])
                    if (c != null && c.elite) { bool beast = Beast(c.look); elites.Add((c, EliteMoves.For(c.mob, beast), beast)); }
            Assert.AreEqual(12, elites.Count);
        }
        // ActorVisual.IsBeast by look name: beasts and Weave-Eaters are a little lighter in health.
        static bool Beast(string look) { return look == "wolf" || look == "boar" || look == "weaveeater" || look == "stag" || look == "spider" || look == "bramble"; }
        static DerivedStatRules Rules(EliteBalance.Kit kit)
        {
            var c = content.FindClass(kit == EliteBalance.Kit.Warrior ? "class.warrior" : "class.druid");
            Assert.NotNull(c, "The class of " + kit + " is in the content."); return c.definition.stats;
        }
        static EliteBalance.Result Fight(EliteBalance.Kit kit, int playerLevel, ZoneCamp camp, EliteMove move, bool beast, bool mira, bool careful)
        {
            return EliteBalance.Fight(EliteBalance.Geared(kit, Rules(kit), playerLevel), new[] { EliteBalance.CampMob(camp.levelMax, beast, move) }, mira, careful, numbers);
        }
        static EliteBalance.Mob[] Pack(int level, int count) { return Enumerable.Range(0, count).Select(_ => EliteBalance.CampMob(level, false)).ToArray(); }

        [Test] public void The_paper_fight_uses_the_kits_real_numbers()
        {
            // Numbers.From reads the content; the defaults are what it read on 2026-10-01. A retuned ability shows here first.
            var d = new EliteBalance.Numbers();
            Assert.AreEqual(d.playerSwing, numbers.playerSwing); Assert.AreEqual(d.enemySwing, numbers.enemySwing); Assert.AreEqual(d.globalCooldown, numbers.globalCooldown);
            Assert.AreEqual(d.strikePower, numbers.strikePower); Assert.AreEqual(d.strikeCost, numbers.strikeCost); Assert.AreEqual(d.strikeCooldown, numbers.strikeCooldown);
            Assert.AreEqual(d.guardCost, numbers.guardCost); Assert.AreEqual(d.guardCooldown, numbers.guardCooldown); Assert.AreEqual(d.guardSeconds, numbers.guardSeconds); Assert.AreEqual(d.guardMultiplier, numbers.guardMultiplier);
            Assert.AreEqual(d.challengeCost, numbers.challengeCost); Assert.AreEqual(d.challengeCooldown, numbers.challengeCooldown);
            Assert.AreEqual(d.vigorMax, numbers.vigorMax); Assert.AreEqual(d.vigorRegen, numbers.vigorRegen);
            Assert.AreEqual(d.boughPower, numbers.boughPower); Assert.AreEqual(d.boughCooldown, numbers.boughCooldown); Assert.AreEqual(d.rakePower, numbers.rakePower); Assert.AreEqual(d.tearPower, numbers.tearPower);
            Assert.AreEqual(d.healPower, numbers.healPower); Assert.AreEqual(d.healCost, numbers.healCost); Assert.AreEqual(d.healCooldown, numbers.healCooldown); Assert.AreEqual(d.healCast, numbers.healCast);
            Assert.AreEqual(d.boltPower, numbers.boltPower); Assert.AreEqual(d.boltCooldown, numbers.boltCooldown);
            Assert.AreEqual(HealerCompanion.HealPerLevel, numbers.healPerLevel);
            // The classes' stat rules and the gear curve give the player on paper: a level 5 Warrior in uncommon gear.
            var f = EliteBalance.Geared(EliteBalance.Kit.Warrior, Rules(EliteBalance.Kit.Warrior), 5);
            Assert.That(f.health, Is.InRange(380, 470)); Assert.That(f.armor, Is.InRange(70, 115)); Assert.That(f.attackPower, Is.InRange(38, 54));
            var bark = EliteBalance.Geared(EliteBalance.Kit.Barkhide, Rules(EliteBalance.Kit.Barkhide), 5); var claw = EliteBalance.Geared(EliteBalance.Kit.Thornclaw, Rules(EliteBalance.Kit.Thornclaw), 5);
            Assert.AreEqual(claw.armor + 30, bark.armor, "Barkhide's 30 armour."); Assert.Greater(bark.health, claw.health); Assert.AreEqual(bark.attackPower + 6, claw.attackPower, "Thornclaw's 6 attack power.");
        }

        [Test] public void The_table()
        {
            var t = new StringBuilder("ELITE_BALANCE (on-curve uncommon gear, base kits, no talents; EliteBalance.Fight)\n");
            foreach (var kit in Kits)
            {
                t.Append("== ").Append(kit).Append('\n');
                foreach (var (camp, move, beast) in elites)
                {
                    int level = camp.levelMax, below = Math.Max(1, level - 2); var mob = EliteBalance.CampMob(level, beast, move);
                    t.Append(camp.mob).Append(" (level ").Append(level).Append(", health ").Append(mob.health).Append(", swing ").Append(mob.hit.ToString("0")).Append(", ").Append(move.name).Append(" x").Append(move.blow).Append(")\n");
                    t.Append("   level ").Append(level).Append(": alone careless ").Append(EliteBalance.Cell(Fight(kit, level, camp, move, beast, false, false)))
                        .Append(" | alone careful ").Append(EliteBalance.Cell(Fight(kit, level, camp, move, beast, false, true)))
                        .Append(" | Mira careless ").Append(EliteBalance.Cell(Fight(kit, level, camp, move, beast, true, false)))
                        .Append(" | Mira careful ").Append(EliteBalance.Cell(Fight(kit, level, camp, move, beast, true, true))).Append('\n');
                    t.Append("   level ").Append(below).Append(": alone careful ").Append(EliteBalance.Cell(Fight(kit, below, camp, move, beast, false, true)))
                        .Append(" | Mira careful ").Append(EliteBalance.Cell(Fight(kit, below, camp, move, beast, true, true))).Append('\n');
                }
                t.Append("-- normal mobs of the player's level: one, and four at once\n");
                foreach (int level in new[] { 1, 2, 3, 5, 8, 10, 13 })
                {
                    var f = EliteBalance.Geared(kit, Rules(kit), level);
                    t.Append("   level ").Append(level).Append(": one ").Append(EliteBalance.Cell(EliteBalance.Fight(f, Pack(level, 1), false, true, numbers)))
                        .Append(" | four alone ").Append(EliteBalance.Cell(EliteBalance.Fight(f, Pack(level, 4), false, true, numbers)))
                        .Append(" | four with Mira ").Append(EliteBalance.Cell(EliteBalance.Fight(f, Pack(level, 4), true, true, numbers))).Append('\n');
                }
            }
            Debug.Log(t.ToString());
            Assert.Pass(t.ToString());
        }

        [Test] public void An_elite_of_your_level_kills_the_careless_and_falls_to_care_and_Mira()
        {
            foreach (var kit in Kits)
                foreach (var (camp, move, beast) in elites)
                {
                    int level = camp.levelMax; string who = kit + " level " + level + " against " + camp.mob + ": ";
                    var careless = Fight(kit, level, camp, move, beast, false, false);
                    Assert.IsFalse(careless.won, who + "alone and careless is a death (" + EliteBalance.Cell(careless) + ").");
                    var cared = Fight(kit, level, camp, move, beast, true, true);
                    Assert.IsTrue(cared.won, who + "with Mira and careful play it falls (" + EliteBalance.Cell(cared) + ").");
                    Assert.GreaterOrEqual(cared.playerLeft, .15f, who + "with something to spare (" + EliteBalance.Cell(cared) + ").");
                    Assert.That(cared.seconds, Is.InRange(20f, 100f), who + "a fight, not a slog (" + EliteBalance.Cell(cared) + ").");
                }
        }

        [Test] public void A_careful_Warrior_alone_does_not_get_an_elite_of_his_level_down()
        {
            foreach (var (camp, move, beast) in elites)
            {
                var alone = Fight(EliteBalance.Kit.Warrior, camp.levelMax, camp, move, beast, false, true);
                Assert.IsFalse(alone.won, camp.mob + ": it wants Mira (" + EliteBalance.Cell(alone) + ").");
                Assert.Less(alone.mobsLeft, .6f, camp.mob + ": but it is a fight, not a wall (" + EliteBalance.Cell(alone) + ").");
            }
        }

        [Test] public void An_elite_two_levels_above_is_not_a_solo_kill()
        {
            foreach (var kit in Kits)
                foreach (var (camp, move, beast) in elites)
                {
                    int level = Math.Max(1, camp.levelMax - 2);
                    var alone = Fight(kit, level, camp, move, beast, false, true);
                    Assert.IsFalse(alone.won, kit + " level " + level + " against " + camp.mob + " (level " + camp.levelMax + "), alone and careful: " + EliteBalance.Cell(alone));
                }
        }

        [Test] public void A_dungeons_end_boss_is_a_step_harder_than_a_named_elite_of_its_level()
        {
            foreach (var (camp, move, beast) in elites.Where(e => e.move.boss))
            {
                var boss = EliteBalance.CampMob(camp.levelMax, beast, move); var named = EliteBalance.CampMob(camp.levelMax, beast, EliteMoves.For("nobody at all", beast));
                Assert.Greater(boss.health, named.health * 1.15f, camp.mob); Assert.Greater(boss.hit, named.hit * 1.05f, camp.mob);
                // And in the fight: a Warrior with Mira, careful, ends lower against the boss than against any outdoor elite of that level.
                var against = Fight(EliteBalance.Kit.Warrior, camp.levelMax, camp, move, beast, true, true);
                foreach (var (other, otherMove, otherBeast) in elites.Where(e => !e.move.boss && e.camp.levelMax == camp.levelMax))
                    Assert.Less(against.playerLeft, Fight(EliteBalance.Kit.Warrior, other.levelMax, other, otherMove, otherBeast, true, true).playerLeft, camp.mob + " is harder than " + other.mob + ".");
            }
        }

        [Test] public void One_normal_mob_is_easy_and_four_are_a_real_pull()
        {
            foreach (int level in new[] { 1, 2, 3, 5, 8, 10, 13 })
            {
                var f = EliteBalance.Geared(EliteBalance.Kit.Warrior, Rules(EliteBalance.Kit.Warrior), level);
                var one = EliteBalance.Fight(f, Pack(level, 1), false, true, numbers);
                Assert.IsTrue(one.won, "Level " + level + ": one mob."); Assert.GreaterOrEqual(one.playerLeft, .85f, "Level " + level + ": one normal mob alone stays easy (" + EliteBalance.Cell(one) + ").");
                var four = EliteBalance.Fight(f, Pack(level, 4), false, true, numbers);
                Assert.LessOrEqual(four.playerLeft, .65f, "Level " + level + ": four at once cost a Warrior alone at least a third of his health (" + EliteBalance.Cell(four) + ").");
                var helped = EliteBalance.Fight(f, Pack(level, 4), true, true, numbers);
                Assert.IsTrue(helped.won, "Level " + level + ": with Mira the four fall (" + EliteBalance.Cell(helped) + ").");
            }
            // A new character with the starting blade and no armour: four wolves at once are too many without her.
            var bare = EliteBalance.Bare(EliteBalance.Kit.Warrior, Rules(EliteBalance.Kit.Warrior), 1, content.weaponBonus);
            Assert.IsFalse(EliteBalance.Fight(bare, Pack(1, 4), false, true, numbers).won, "Level 1, bare: four at once is a death alone.");
            Assert.IsTrue(EliteBalance.Fight(bare, Pack(1, 1), false, true, numbers).won, "Level 1, bare: one is a fight you win.");
        }
    }
}
