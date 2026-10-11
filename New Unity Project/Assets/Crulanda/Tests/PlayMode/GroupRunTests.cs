#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Combat;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>What the group runs of the Adit taught (Chris watching, 2026-10-11, PLAYTEST_NOTES 133-141): a party of five, a sim Ranger's
    /// wolf, sims in their class's armour hitting like a player, no single blow over three fifths of a member's health, the damage and
    /// healing meter with each member's class, and a mark that dies with its mob.</summary>
    public class GroupRunTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-group-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour; SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(SimPopulation.Active);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas; SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        /// <summary>Sims of these classes brought to Oakhaven, online, willing, at the player's level.</summary>
        SimAdventurer[] Willing(params string[] classIds)
        {
            var pop = SimPopulation.Active; var picked = new System.Collections.Generic.List<SimAdventurer>();
            foreach (var c in classIds) { var s = pop.World.sims.First(x => x.classId == c && !picked.Contains(x)); picked.Add(s); }
            foreach (var s in picked) { s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = session.Progress.Level; s.x = s.z = 0; }
            pop.Refresh(); foreach (var s in picked) Assert.NotNull(pop.Find(s.id), s.name + " stands in Oakhaven");
            return picked.ToArray();
        }

        [UnityTest] public IEnumerator A_party_of_five_and_the_sim_Rangers_wolf()
        {
            Assert.AreEqual(4, EncounterSession.MaxPartySims, "four sims with you: five (the dungeons are five-player)");
            var sims = Willing("class.warrior", "class.druid", "class.ranger", "class.rogue", "class.mage");
            for (int i = 0; i < 4; i++) Assert.IsTrue(session.Invite(sims[i].id), sims[i].name);
            Assert.AreEqual(4, session.PartySims.Count); Assert.AreEqual("Your party is full.", session.InviteRefusal(sims[4]));
            yield return null;
            Assert.AreEqual(1, session.SimPets.Count, "the Ranger's wolf came with it"); var wolf = session.SimPets[0];
            Assert.AreSame(session.PartySim(sims[2].id), wolf.Master); Assert.AreEqual("wolf", wolf.Kind);
            Assert.AreSame(wolf.actor, session.PartyActor(wolf.actor.EntityId.Value), "a party member the mobs can turn on");
            StringAssert.Contains("'s wolf", wolf.actor.DisplayName);
            session.LeaveParty(sims[2].id); yield return null;
            Assert.AreEqual(0, session.SimPets.Count, "and goes when its Ranger leaves");
        }
        [UnityTest] public IEnumerator Sims_wear_their_class_armour_and_hit_like_a_player()
        {
            var sims = Willing("class.warrior", "class.druid");
            foreach (var s in sims) Assert.IsTrue(session.Invite(s.id), s.name);
            var warrior = session.PartySim(sims[0].id); var druid = session.PartySim(sims[1].id);
            int wArmour = SimCompanion.ArmourFor(sims[0], session.Items), dArmour = SimCompanion.ArmourFor(sims[1], session.Items);
            Assert.Greater(wArmour, 0, "the Warrior's pieces have armour"); Assert.Greater(dArmour, 0, "the Druid's too");
            Assert.AreEqual(wArmour, warrior.actor.Stats.GetRounded(Crulanda.Core.StatType.Armor), "worn as a stat (it had none)");
            Assert.AreEqual(dArmour, druid.actor.Stats.GetRounded(Crulanda.Core.StatType.Armor));
            Assert.GreaterOrEqual(wArmour, dArmour, "mail over leather");
            Assert.GreaterOrEqual(warrior.Hit, 12 + 4 * sims[0].level, "a player's blow at its level (it hit 6 + 2.4 a level before)");
            yield return null;
        }
        [UnityTest] public IEnumerator No_single_blow_takes_more_than_three_fifths_of_a_members_health()
        {
            var e = session.Enemies.First(x => x != null && x.actor.IsAlive && !x.Game);
            int max = session.Player.Health.Pool.Max;
            Assert.AreEqual(Mathf.RoundToInt(max * EncounterEnemy.MaxBlowShare), e.CappedBlow(session.Player, max * 3), "a blow of three times your health is capped");
            Assert.AreEqual(10, e.CappedBlow(session.Player, 10), "a small one is itself");
            var sims = Willing("class.druid"); Assert.IsTrue(session.Invite(sims[0].id)); var c = session.PartySim(sims[0].id);
            Assert.AreEqual(Mathf.RoundToInt(c.actor.Health.Pool.Max * EncounterEnemy.MaxBlowShare), e.CappedBlow(c.actor, 100000), "a sim's too");
            yield return null;
        }
        [UnityTest] public IEnumerator The_meter_sums_damage_and_healing_by_member_with_its_class()
        {
            var sims = Willing("class.druid"); Assert.IsTrue(session.Invite(sims[0].id)); var druid = session.PartySim(sims[0].id);
            session.Meter.Reset();
            var e = session.Enemies.First(x => x != null && x.actor.IsAlive && !x.Game);
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 3); session.Select(e);
            int hp = e.actor.Health.Pool.Current; e.Receive(20, session.Player); int dealt = hp - e.actor.Health.Pool.Current;
            session.Player.Health.Pool.SetCurrent(10);
            int healed = session.Player.GetComponent<Combatant>().Heal(50, druid.actor);
            yield return null;
            var lines = session.Meter.Lines().ToList();
            Assert.IsTrue(lines.Any(l => l.StartsWith("You (" + session.ClassDef.displayName + ")") && l.Contains(dealt.ToString("N0") + " dmg")), "your blow, under your class: " + string.Join(" / ", lines));
            Assert.IsTrue(lines.Any(l => l.StartsWith(druid.actor.DisplayName + " (Druid)") && l.Contains(healed.ToString("N0") + " heal")), "the Druid's mend, under its class: " + string.Join(" / ", lines));
            session.Meter.Reset(); Assert.IsTrue(session.Meter.Empty, "/meter reset clears it");
        }
        /// <summary>The scorecard (Chris, 2026-10-11: "kinda like a scorecard when a dungeon is complete. maybe an mvp"): a run begun counts
        /// from there, deaths and loot land on it, the MVP is the best contributor, and it opens by itself when the dungeon's last boss
        /// falls or the leader calls the run cleared; /scorecard and Esc.</summary>
        [UnityTest] public IEnumerator The_scorecard_counts_the_run_and_opens_when_the_last_boss_falls()
        {
            var m = session.Meter; m.BeginRun("A test run"); Assert.AreEqual("A test run", m.RunName);
            var e = session.Enemies.First(x => x != null && x.actor.IsAlive && !x.Game && !x.Elite);
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 3); session.Select(e);
            e.Receive(30, session.Player); m.Loot("A test blade", e.Name, "You"); m.Death(session.Player);
            yield return null;
            var card = m.Scorecard().ToList();
            Assert.IsTrue(card.Any(l => l.Contains("MVP: You (")), "an MVP: " + string.Join(" / ", card));
            Assert.IsTrue(card.Any(l => l.Contains("A test blade (" + e.Name + ") to You")), "the loot, off whom, to whom");
            Assert.IsTrue(card.Any(l => l.Contains("1 death")), "the deaths");
            Assert.IsFalse(session.ScorecardOpen);
            session.ShowScorecard("A test run cleared"); Assert.IsTrue(session.ScorecardOpen); Assert.AreEqual("A test run cleared", session.ScorecardTitle);
            session.Escape(); Assert.IsFalse(session.ScorecardOpen, "Esc closes it"); Assert.IsFalse(session.Paused);
        }
        [UnityTest] public IEnumerator A_raid_mark_dies_with_its_mob()
        {
            var e = session.Enemies.First(x => x != null && x.actor.IsAlive && !x.Game && !x.Elite);
            e.RaidMarked = RaidMark.Moon; Assert.AreEqual(RaidMark.Moon, e.RaidMarked);
            e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 3); yield return null;
            Assert.IsFalse(e.actor.IsAlive); Assert.AreEqual(RaidMark.None, e.RaidMarked, "the icon goes with the mob (Chris: \"once the mob that was cc dies. take off the icon\")");
        }
    }
}
#endif
