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
    /// <summary>
    /// Buffing up before the bosses (Chris, 2026-10-15: "we should use food buffs, alchemy buffs, class buffs,etc to beat the bosses";
    /// BossBuffs): a sim party led to an elite camp in Oakhaven stops short of it and says so; every one of it, you too (/buff, as the run
    /// bot does), ends well fed, with its elixir or salve and every class's standing buff; then the leader pulls. A trash camp is not stopped for.
    /// </summary>
    public class BossBuffTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-buffs-" + Guid.NewGuid().ToString("N"));
            hourWas = WorldClock.Hour;
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session); Assert.NotNull(SimPopulation.Active);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            WorldClock.Hour = hourWas;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        /// <summary>A sim of a class into the party at a level, carrying what it is given (its goods).</summary>
        SimCompanion Join(string classId, int level, params string[] carries)
        {
            var pop = SimPopulation.Active;
            var s = pop.World.sims.First(x => x.classId == classId && !session.InParty(x.id));
            s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = level; s.coin = 0;
            s.goodIds.Clear(); s.goodCounts.Clear(); foreach (var id in carries) SimEconomy.Add(s, id, 1);
            pop.Refresh(); Assert.NotNull(pop.Find(s.id), s.name + " stands in Oakhaven");
            Assert.IsTrue(session.Invite(s.id, true), session.Messages.LastOrDefault());
            var c = session.PartySim(s.id); c.MatchLevel(level); return c;
        }
        /// <summary>Somewhere on the paths <paramref name="far"/> metres from a camp, the camp reachable from it.</summary>
        static Vector3? Approach(Vector3 camp, float far)
        {
            var path = new NavMeshPath();
            for (int k = 0; k < 24; k++)
            {
                var at = camp + Quaternion.Euler(0, k * 15, 0) * Vector3.forward * far;
                if (NavMesh.SamplePosition(at, out var hit, 4, NavMesh.AllAreas) && NavMesh.CalculatePath(hit.position, camp, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) return hit.position;
            }
            return null;
        }
        static Combatant Body(Crulanda.Gameplay.Actor a) { return a.GetComponent<Combatant>(); }

        [UnityTest] public IEnumerator A_party_led_to_an_elite_camp_buffs_up_short_of_it_then_pulls()
        {
            EncounterEnemy elite = null; Vector3? start = null;
            foreach (var e in session.Enemies.Where(e => e != null && e.actor.IsAlive && e.Elite && e.Camp && !e.Game && !e.Hidden).OrderBy(e => e.actor.Level))
            { start = Approach(e.transform.position, 55); if (start != null) { elite = e; break; } }
            Assert.NotNull(elite, "an elite camp in Oakhaven with a way to it from 55 m");
            int level = Mathf.Max(2, elite.actor.Level);
            // The party: a Warrior to lead and tank, a Druid to heal, a Mage; each with its dish and its part's elixir; the Druid a feast.
            var tank = Join("class.warrior", level, "food.cask_stew", "salve.tallow");
            var healer = Join("class.druid", level, "food.cask_stew", "potion.tincture_hearth", "food.harvest_feast");
            var mage = Join("class.mage", level, "food.cask_stew", "potion.tincture_owl");
            // You (the run bot's part): a dish and the tinctures for whichever part is yours.
            foreach (var id in new[] { "food.cask_stew", "potion.tincture_ox", "potion.tincture_hare", "potion.tincture_owl", "potion.tincture_hearth", "salve.tallow" }) Inventory.Add(session.Progress, session.Items, id, 1);
            var motor = session.Player.GetComponent<AdventurerMotor>(); motor.Teleport(start.Value + Vector3.up * 1.1f);
            int k = 0; foreach (var c in session.PartySims) c.GetComponent<NavMeshAgent>().Warp(start.Value + Vector3.left * (1.5f + k++));
            yield return null;
            Assert.AreEqual("buff.class.warrior", ClassBuffs.For(tank.sim.classId).id);
            Assert.NotNull(tank.LeadTo(elite)); Assert.IsTrue(tank.BuffPending, "an elite camp: a buff-up owed");
            // Follow the leader (you keep up with it, as the run bot does), until it stops short.
            float until = Time.time + 90, nextStep = 0;
            while (Time.time < until && !tank.BuffingUp)
            {
                Assert.IsFalse(session.Enemies.Any(e => e != null && e.FightingParty && e.Camp && Vector3.Distance(e.transform.position, elite.transform.position) < 20), "the elite's camp not pulled on the way (" + tank.Activity + ")");   // a roamer on the road may be
                if (Time.time >= nextStep) { nextStep = Time.time + .5f; Keep(motor, tank); }
                yield return null;
            }
            Assert.IsTrue(tank.BuffingUp, "it stopped to buff up (" + tank.Activity + ")");
            float short_ = Vector3.Distance(tank.transform.position, tank.Leading.Value);
            Assert.That(short_, Is.InRange(BossBuffs.StopMin - 3, BossBuffs.StopShort + 3), "short of the camp, out of its reach");
            Assert.IsTrue(session.Chat.Any(l => l.channel == ChatChannel.Party && l.speaker == tank.sim.name && l.text.ToLower().Contains("buff up")), "said in Party");
            StringAssert.StartsWith("Buffing up", tank.Activity);
            // You buff (/buff) as the run bot does.
            var used = session.BuffUp(); Assert.Greater(used.Count, 0, "you used something: " + session.Messages.LastOrDefault());
            until = Time.time + BossBuffs.StopWait + 4;
            while (Time.time < until && tank.BuffingUp) yield return null;
            Assert.IsFalse(tank.BuffingUp, "the stop ends");
            // Everyone: well fed, an elixir or a salve, every class's standing buff (yours and the three sims').
            var classes = new List<string> { session.ClassDef.id }; classes.AddRange(session.PartySims.Select(c => c.sim.classId));
            var everyone = new List<Crulanda.Gameplay.Actor> { session.Player }; everyone.AddRange(session.PartySims.Select(c => c.actor));
            foreach (var a in everyone)
            {
                var st = Body(a).Statuses; string who = a == session.Player ? "you" : a.DisplayName;
                Assert.IsTrue(st.Has(Consumables.FedId), who + " is well fed");
                Assert.IsTrue(st.Has(Consumables.ElixirId) || st.Has(Consumables.SalveId), who + " has an elixir or a salve");
                foreach (var cid in classes.Distinct()) { var b = ClassBuffs.For(cid); if (b != null) Assert.IsTrue(st.Has(b.id), who + " has " + b.name); }
            }
            Assert.IsTrue(Body(tank.actor).Statuses.Has(Consumables.SalveId), "the tank took its salve");
            Assert.AreEqual(0, SimEconomy.Count(healer.sim, "food.harvest_feast"), "the feast was eaten");
            Assert.IsTrue(session.PartySims.All(c => c.BuffedStop == session.BuffStop), "each took its turn: " + string.Join("; ", session.PartySims.Select(c => c.sim.name + ": " + c.BuffNote)));
            Assert.Greater(mage.DamageEdge, 0, "the mage hits harder for it"); Assert.LessOrEqual(mage.DamageEdge, BossBuffs.MaxEdge);
            Assert.Greater(tank.actor.Health.Pool.Max, SimCompanion.MaxHealthFor(tank.sim, session.Items), "the tank's health is up with its dish");
            // Then the pull.
            until = Time.time + 40;
            while (Time.time < until && !session.Enemies.Any(e => e != null && e.actor.IsAlive && e.FightingParty && e.Camp && Vector3.Distance(e.transform.position, tank.Leading ?? e.transform.position) < 20))
            {
                if (Time.time >= nextStep) { nextStep = Time.time + .5f; Keep(motor, tank); }
                yield return null;
            }
            Assert.IsTrue(session.Enemies.Any(e => e != null && e.FightingParty && e.Camp), "the leader pulled (" + tank.Activity + ")");
            Assert.IsFalse(tank.BuffPending, "nothing owed now");
        }
        /// <summary>
        /// After a wipe the leader stops again (2026-10-18: dying clears the buffs, and the party pulled the boss bare the second time): the
        /// party buffs up short of the elite camp, wipes before the pull (everyone dies and wakes), is led back, and the leader stops again,
        /// says so ("again"), and everyone is buffed again from what it still carries; then no third stop is owed.
        /// </summary>
        [UnityTest] public IEnumerator After_a_wipe_the_party_buffs_up_again_before_the_boss()
        {
            EncounterEnemy elite = null; Vector3? start = null;
            foreach (var e in session.Enemies.Where(e => e != null && e.actor.IsAlive && e.Elite && e.Camp && !e.Game && !e.Hidden).OrderBy(e => e.actor.Level))
            { start = Approach(e.transform.position, 55); if (start != null) { elite = e; break; } }
            Assert.NotNull(elite, "an elite camp in Oakhaven with a way to it from 55 m");
            int level = Mathf.Max(2, elite.actor.Level);
            // Two of each: one for each stop.
            var tank = Join("class.warrior", level, "food.cask_stew", "food.cask_stew", "salve.tallow", "salve.tallow");
            var healer = Join("class.druid", level, "food.cask_stew", "food.cask_stew", "potion.tincture_hearth", "potion.tincture_hearth");
            var motor = session.Player.GetComponent<AdventurerMotor>();
            void Place() { motor.Teleport(start.Value + Vector3.up * 1.1f); int k = 0; foreach (var c in session.PartySims) c.GetComponent<NavMeshAgent>().Warp(start.Value + Vector3.left * (1.5f + k++)); }
            Place(); yield return null;
            Assert.NotNull(tank.LeadTo(elite)); Assert.IsTrue(tank.BuffPending, "an elite camp: a buff-up owed");
            // The first stop.
            float until = Time.time + 90, nextStep = 0;
            while (Time.time < until && !tank.BuffingUp) { if (Time.time >= nextStep) { nextStep = Time.time + .5f; Keep(motor, tank); } yield return null; }
            Assert.IsTrue(tank.BuffingUp, "it stopped to buff up (" + tank.Activity + ")"); Assert.IsFalse(tank.BuffAgain, "the first stop");
            session.BuffUp();
            until = Time.time + BossBuffs.StopWait + 4;
            while (Time.time < until && tank.BuffingUp) yield return null;
            Assert.IsFalse(tank.BuffingUp, "the stop ends"); Assert.IsFalse(tank.BuffPending, "nothing owed after it");
            string warriorBuff = ClassBuffs.For("class.warrior").id;
            Assert.IsTrue(Body(healer.actor).Statuses.Has(warriorBuff), "the healer has the warrior's buff");
            // The wipe, before the pull: everyone dies (the buffs go with it) and wakes.
            foreach (var c in session.PartySims) c.actor.Health.ApplyDamage(c.actor.Health.Pool.Max * 10);
            session.Player.Health.ApplyDamage(session.Player.Health.Pool.Max * 10);
            yield return null;
            Assert.IsFalse(Body(healer.actor).Statuses.Has(warriorBuff), "dying took the buffs");
            int wipes = session.Wipes; session.Recover(); Assert.AreEqual(wipes + 1, session.Wipes, "a wipe counted");
            Assert.IsTrue(session.PartySims.All(c => c.actor.IsAlive), "everyone woke");
            Assert.IsTrue(tank.BuffPending, "a buff-up owed again");
            // Led back: the leader stops again, says so, and the party buffs again.
            Place(); yield return null;
            int chats = session.Chat.Count;
            until = Time.time + 90;
            while (Time.time < until && !tank.BuffingUp) { if (Time.time >= nextStep) { nextStep = Time.time + .5f; Keep(motor, tank); } yield return null; }
            Assert.IsTrue(tank.BuffingUp, "it stopped again (" + tank.Activity + ")"); Assert.IsTrue(tank.BuffAgain, "again, after the wipe");
            Assert.IsTrue(session.Chat.Skip(chats).Any(l => l.channel == ChatChannel.Party && l.speaker == tank.sim.name && l.text.ToLower().Contains("again")), "said again in Party");
            var used = session.BuffUp(); Assert.Greater(used.Count, 0, "you buffed again: " + session.Messages.LastOrDefault());
            until = Time.time + BossBuffs.StopWait + 4;
            while (Time.time < until && tank.BuffingUp) yield return null;
            Assert.IsFalse(tank.BuffingUp, "the second stop ends");
            foreach (var c in session.PartySims)
            {
                var st = Body(c.actor).Statuses;
                Assert.IsTrue(st.Has(Consumables.FedId), c.sim.name + " fed again (" + c.BuffNote + ")");
                Assert.IsTrue(st.Has(warriorBuff), c.sim.name + " has the warrior's buff again");
            }
            Assert.AreEqual(0, SimEconomy.Count(tank.sim, "food.cask_stew"), "the tank ate its second stew");
            Assert.IsFalse(tank.BuffPending, "no third stop owed");
        }
        /// <summary>A wipe with nothing to gain (every buff still on, which no wipe leaves, as a stand-in for a party with nothing): no stop again.</summary>
        [UnityTest] public IEnumerator A_wipe_with_nothing_to_take_is_not_stopped_for_again()
        {
            EncounterEnemy elite = null; Vector3? start = null;
            foreach (var e in session.Enemies.Where(e => e != null && e.actor.IsAlive && e.Elite && e.Camp && !e.Game && !e.Hidden).OrderBy(e => e.actor.Level))
            { start = Approach(e.transform.position, 55); if (start != null) { elite = e; break; } }
            Assert.NotNull(elite);
            var tank = Join("class.warrior", Mathf.Max(2, elite.actor.Level));
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(start.Value + Vector3.up * 1.1f); tank.GetComponent<NavMeshAgent>().Warp(start.Value + Vector3.left * 1.5f); yield return null;
            Assert.NotNull(tank.LeadTo(elite));
            float until = Time.time + 90, nextStep = 0;
            while (Time.time < until && !tank.BuffingUp) { if (Time.time >= nextStep) { nextStep = Time.time + .5f; Keep(motor, tank); } yield return null; }
            Assert.IsTrue(tank.BuffingUp, "the first stop (" + tank.Activity + ")");
            session.BuffUp();
            until = Time.time + BossBuffs.StopWait + 4;
            while (Time.time < until && tank.BuffingUp) yield return null;
            Assert.IsFalse(tank.BuffingUp);
            // Counted as a wipe, but the buffs are all still on: nothing to take, no second stop.
            tank.GetComponent<NavMeshAgent>().Warp(start.Value + Vector3.left * 1.5f); motor.Teleport(start.Value + Vector3.up * 1.1f);
            session.CountWipe(); Assert.IsTrue(tank.BuffPending, "owed, until it sees there is nothing to take");
            Assert.IsFalse(session.PartyWantsBuffs(), "nothing to take");
            until = Time.time + 60; bool stopped = false;
            while (Time.time < until && tank.BuffPending) { stopped |= tank.BuffingUp; if (Time.time >= nextStep) { nextStep = Time.time + .5f; Keep(motor, tank); } yield return null; }
            Assert.IsFalse(stopped, "no second stop"); Assert.IsFalse(tank.BuffPending, "and nothing owed");
        }
        /// <summary>You keep up with the leader: a step behind it whenever it is more than five metres off.</summary>
        static void Keep(AdventurerMotor motor, SimCompanion leader)
        {
            if (Vector3.Distance(motor.transform.position, leader.transform.position) <= 5) return;
            var at = leader.transform.position - leader.transform.forward * 2.5f;
            if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) motor.Teleport(hit.position + Vector3.up * 1.1f);
        }

        /// <summary>Not on trash: a run to a camp without an elite owes no buff-up.</summary>
        [UnityTest] public IEnumerator A_trash_camp_is_not_stopped_for()
        {
            var trash = session.Enemies.First(e => e != null && e.actor.IsAlive && e.Camp && !e.Elite && !e.Game && !e.Hidden);
            var c = Join("class.warrior", Mathf.Max(1, trash.actor.Level), "food.cask_stew");
            yield return null;
            Assert.NotNull(c.LeadTo(trash)); Assert.IsFalse(c.BuffPending, "trash: straight in");
            c.StopLeading(); Assert.IsFalse(c.BuffingUp);
        }
    }
}
#endif
