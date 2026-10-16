#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
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
            Assert.GreaterOrEqual(warrior.BaseHit, 12 + 4 * sims[0].level, "a player's blow at its level (it hit 6 + 2.4 a level before)");
            Assert.Less(warrior.Hit, warrior.BaseHit, "a tank hits under its blow (Chris 2026-10-15: the tank out-damaged the damage-dealers)");
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

        // ---------- every class's hold and the area blow (SimCompanion.Fight; Chris, 2026-10-15: "also most mobs dont seem to be cc or aoe still a thing?") ----------
        /// <summary>Sims made these classes (any sims: the world need not have one of each free), brought to Oakhaven as <see cref="Willing"/> does.</summary>
        SimAdventurer[] As(params string[] classIds)
        {
            var pop = SimPopulation.Active; var picked = pop.World.sims.Where(x => !session.InParty(x.id)).Take(classIds.Length).ToArray();
            for (int i = 0; i < picked.Length; i++) { var s = picked[i]; s.classId = classIds[i]; s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = session.Progress.Level; s.x = s.z = 0; }
            pop.Refresh(); foreach (var s in picked) Assert.NotNull(pop.Find(s.id), s.name + " stands in Oakhaven");
            return picked;
        }
        /// <summary>Three of one camp (not elite, not lying in the grass, not on a road), the camp nearest you; the two others brought within a
        /// step and a half of the first, inside their own camp. Null when Oakhaven has no camp of three.</summary>
        EncounterEnemy[] PackOfThree()
        {
            var me = session.Player.transform.position; var flat = new Vector2(me.x, me.z);
            var camp = session.Enemies.Where(x => x != null && x.actor.IsAlive && x.Camp && !x.Elite && !x.Game && !x.Hidden && !x.Ambusher && x.Inside == null && x.Patrol == null)
                .GroupBy(x => x.CampCenter).Where(g => g.Count() >= 3).OrderBy(g => Vector2.Distance(g.Key, flat)).FirstOrDefault();
            if (camp == null) return null;
            var three = camp.Take(3).ToArray(); var at = three[0].transform.position;
            for (int i = 1; i < 3; i++)
                if (NavMesh.SamplePosition(at + Quaternion.Euler(0, i * 120, 0) * Vector3.forward * 1.5f, out var hit, 2, NavMesh.AllAreas)) three[i].GetComponent<NavMeshAgent>().Warp(hit.position);
            return three;
        }
        /// <summary>You beside the pack, the sims round you, the pack on you (and you kept whole); waits until all three fight the party.</summary>
        IEnumerator Engage(EncounterEnemy[] three, IEnumerable<SimCompanion> party)
        {
            session.Player.GetComponent<AdventurerMotor>().Teleport(three[0].transform.position + Vector3.back * 2.5f); yield return null;
            var p = session.Player.transform.position; int i = 0;
            foreach (var c in party) { var at = p + Quaternion.Euler(0, 90 * i++, 0) * Vector3.right * 2; if (NavMesh.SamplePosition(at, out var hit, 2, NavMesh.AllAreas)) at = hit.position; c.GetComponent<NavMeshAgent>().Warp(at); }
            session.Select(three[0]); string me = session.Player.EntityId.Value;
            foreach (var e in three) e.threat.Add(me, 50);
            for (float t = 0; t < 3 && !three.All(e => e.FightingParty); t += Time.deltaTime) { session.Player.Health.ApplyHealing(999); yield return null; }
            Assert.IsTrue(three.All(e => e.FightingParty), "the three are on the party");
        }

        /// <summary>A party of five against three: some class's hold takes one (said in Party), a damage-dealer's area blow goes across the
        /// pack, and no hold is broken by a blow before its time (the area blow leaves held mobs alone).</summary>
        [UnityTest] public IEnumerator A_party_of_five_holds_one_of_three_and_swings_across_the_rest()
        {
            session.Progress.recruited = false;   // Mira stays out: the holds and blows counted are the sims'
            var three = PackOfThree(); if (three == null) Assert.Inconclusive("no camp of three in Oakhaven");
            var sims = As("class.warrior", "class.druid", "class.mage", "class.ranger");
            foreach (var s in sims) Assert.IsTrue(session.Invite(s.id, true), s.name + " joins");
            var party = sims.Select(s => session.PartySim(s.id)).ToArray();
            Assert.AreEqual(SimCompanion.PartyRole.Tank, party[0].Role); Assert.AreEqual(SimCompanion.PartyRole.Healer, party[1].Role);
            yield return Engage(three, party);
            string me = session.Player.EntityId.Value; var heldLeft = new Dictionary<EncounterEnemy, float>(); var broke = new List<string>();
            float t = 0;
            while (t < 14 && !(t > 6 && party.Sum(c => c.Holds) > 0 && party.Sum(c => c.AreaSwings) > 0))
            {
                foreach (var e in session.Enemies)
                {
                    if (e == null || !e.FightingParty && !heldLeft.ContainsKey(e)) continue;
                    // A hold that ends with time still on it was broken by a blow (an incap breaks on any damage).
                    if (heldLeft.TryGetValue(e, out var left) && left > Time.deltaTime + .1f && e.actor.IsAlive && !e.Incapacitated) broke.Add(e.actor.DisplayName + " with " + left.ToString("0.0") + " s left");
                    heldLeft[e] = e.Incapacitated ? e.ControlSecondsLeft : 0;
                }
                foreach (var e in three) if (e.actor.IsAlive) e.threat.Add(me, 1);
                session.Player.Health.ApplyHealing(999); t += Time.deltaTime; yield return null;
            }
            string tally = string.Join(", ", party.Select(c => c.sim.classId.Replace("class.", "") + " " + c.Holds + " holds, " + c.AreaSwings + " wide"));
            Assert.Greater(party.Sum(c => c.Holds), 0, "a class's hold took (" + tally + ")");
            Assert.Greater(party.Sum(c => c.AreaSwings), 0, "an area blow went across the pack (" + tally + ")");
            CollectionAssert.IsEmpty(broke, "no hold broken by a blow (" + tally + ")");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && !l.text.StartsWith("kicked") && !l.text.StartsWith("Interrupted") && party.Any(c => c.Holds > 0 && c.sim.name == l.speaker)), "the hold said in Party");
            foreach (var c in party) if (c.LastHeld != null) Assert.IsFalse(c.LastHeld.Boss, "never a boss");
        }
        /// <summary>The area blow strikes the quarry and the others near it but never a held mob, which stays held; a healer has none.</summary>
        [UnityTest] public IEnumerator The_area_blow_leaves_a_held_mob_held()
        {
            session.Progress.recruited = false;
            var three = PackOfThree(); if (three == null) Assert.Inconclusive("no camp of three in Oakhaven");
            var sims = As("class.mage", "class.druid");
            foreach (var s in sims) Assert.IsTrue(session.Invite(s.id, true), s.name + " joins");
            var mage = session.PartySim(sims[0].id); var druid = session.PartySim(sims[1].id);
            Assert.AreEqual(SimCompanion.PartyRole.Damage, mage.Role); Assert.AreEqual(SimCompanion.PartyRole.Healer, druid.Role);
            yield return Engage(three, new[] { mage, druid });
            // Whatever the sims held on the way in let go, then one held by you: all in this frame, no sim acting between.
            foreach (var e in three) { e.Release(); e.RaidMarked = RaidMark.None; }
            var held = three[2]; Assert.IsNull(held.Apply("incap", session.Player, 30), "held by you");
            var foes = mage.WideTargets(three[0]);
            Assert.NotNull(foes, "three on the party within " + SimCompanion.AreaReach + " m of the quarry");
            CollectionAssert.DoesNotContain(foes, held, "the held one is left out"); CollectionAssert.Contains(foes, three[0]); CollectionAssert.Contains(foes, three[1]);
            Assert.IsNull(druid.WideTargets(three[0]), "a healer swings no area blow");
            var before = foes.ToDictionary(e => e, e => e.actor.Health.Pool.Current); int heldHp = held.actor.Health.Pool.Current, swings = mage.AreaSwings;
            Assert.AreEqual(foes.Count, mage.SwingWide(three[0]), "each struck");
            Assert.AreEqual(swings + 1, mage.AreaSwings);
            Assert.IsTrue(held.Incapacitated, "still held"); Assert.AreEqual(heldHp, held.actor.Health.Pool.Current, "and untouched");
            foreach (var kv in before) Assert.Less(kv.Key.actor.Health.Pool.Current, kv.Value, kv.Key.actor.DisplayName + " is struck");
            // The held one and one marked to be left alone: only the quarry to strike, and that is no area blow.
            three[1].RaidMarked = RaidMark.Cross; Assert.IsNull(mage.WideTargets(three[0]), "one to strike is a plain blow"); three[1].RaidMarked = RaidMark.None;
            yield return null;
        }
        /// <summary>No sim's hold goes on a boss (Caddock): not the long holds, and not a stun, which a boss would take; the same mob made no
        /// boss for a moment is the Warrior's to chain. In a few seconds of the fight nothing holds him.</summary>
        [UnityTest] public IEnumerator No_sim_holds_a_boss()
        {
            session.Progress.recruited = false;
            yield return null;
            var king = session.Enemies.FirstOrDefault(x => x != null && x.Elite && x.actor.DisplayName.Contains("Caddock"));
            Assert.NotNull(king, "Caddock is in Crowsfoot Hollow"); Assert.IsTrue(king.Boss, "a boss");
            var sims = As("class.warrior", "class.mage", "class.shaman", "class.paladin");
            foreach (var s in sims) Assert.IsTrue(session.Invite(s.id, true), s.name + " joins");
            var party = sims.Select(s => session.PartySim(s.id)).ToArray(); var warrior = party[0];
            var near = NavMesh.SamplePosition(king.transform.position - king.transform.forward * 3, out var stand, 4, NavMesh.AllAreas) ? stand.position + Vector3.up * 1.1f : king.transform.position + Vector3.back * 3;
            session.Player.GetComponent<AdventurerMotor>().Teleport(near); yield return null;
            var p = session.Player.transform.position; int i = 0;
            foreach (var c in party) { var at = p + Quaternion.Euler(0, 90 * i++, 0) * Vector3.right * 2; if (NavMesh.SamplePosition(at, out var hit, 2, NavMesh.AllAreas)) at = hit.position; c.GetComponent<NavMeshAgent>().Warp(at); }
            string me = session.Player.EntityId.Value; king.threat.Add(me, 50);
            // The first frame he is on the party he is on you (the Warrior's taunt turns him on a later frame).
            for (float w = 0; w < 3 && !king.FightingParty; w += Time.deltaTime) yield return null;
            Assert.IsTrue(king.FightingParty, "Caddock is on the party"); Assert.AreSame(session.Player, king.Victim, "on you");
            foreach (var c in party) Assert.IsFalse(c.MayHold(king), c.sim.classId + "'s " + (c.ClassMove != null ? c.ClassMove.name : "hold") + " is not for a boss");
            king.Move.boss = false;
            try { Assert.IsTrue(warrior.MayHold(king), "no boss, the Warrior would chain him off you: it is the boss rule that keeps the hold off"); }
            finally { king.Move.boss = true; }
            float t = 0; bool held = false;
            while (t < 4) { if (king.Controlled) held = true; king.threat.Add(me, 1); session.Player.Health.ApplyHealing(999); t += Time.deltaTime; yield return null; }
            Assert.IsFalse(held, "nothing held Caddock");
            foreach (var c in party) if (c.LastHeld != null) Assert.AreNotSame(king, c.LastHeld);
        }
        /// <summary>Every class has a hold: eight their own, the Rogue, the Archivist and the Scoundrel the moon (HoldTheMoon). Kinds that
        /// EncounterEnemy.Apply takes, waits of 20-30 s; the area blow is 60-80% of a blow.</summary>
        [Test] public void Every_class_has_a_hold_and_the_area_blow_is_a_share_of_a_blow()
        {
            foreach (var id in SimRoster.ClassIds)
            {
                var m = SimCompanion.HoldFor(id);
                if (id == "class.rogue" || id == "class.archivist" || id == "class.scoundrel") { Assert.IsNull(m, id + " holds the moon"); continue; }
                Assert.NotNull(m, id + " has a hold");
                CollectionAssert.Contains(new[] { "incap", "stun", "fear" }, m.kind, id);
                Assert.That(m.cooldown, Is.InRange(20f, 30f), id + "'s wait"); Assert.Greater(m.seconds, 0); Assert.Greater(m.reach, 0);
            }
            Assert.That(SimCompanion.AreaShare, Is.InRange(.6f, .8f)); Assert.That(SimCompanion.CleaveShare, Is.InRange(.6f, .8f));
        }
    }
}
#endif
