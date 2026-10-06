#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>Inviting sims (Phase 5.2b): one joins, follows, fights your target and can be turned on; the busy and the far-off decline; Leave party sends it back to the world.</summary>
    public class SimPartyTests
    {
        string root; EncounterSession session; float hourWas;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-party-" + Guid.NewGuid().ToString("N"));
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
        /// <summary>An Oakhaven sim, online now, made willing and of the player's level, with a figure standing.</summary>
        SimAdventurer Willing(string classId = null)
        {
            var pop = SimPopulation.Active;
            var s = pop.World.sims.First(x => x.zone == "zone.oakhaven" && (classId == null || x.classId == classId) || x == pop.World.sims.Last());
            s.zone = "zone.oakhaven"; s.onlineFrom = 0; s.onlineHours = 24; s.friendly = .9f; s.level = session.Progress.Level;
            pop.Refresh(); Assert.NotNull(pop.Find(s.id), s.name + " stands in Oakhaven"); return s;
        }

        [UnityTest] public IEnumerator A_sim_joins_follows_and_leaves_again()
        {
            var s = Willing(); yield return null;
            Assert.IsNull(session.InviteRefusal(s));
            Assert.IsTrue(session.Invite(s.id), "joins");
            var c = session.PartySim(s.id); Assert.NotNull(c); Assert.IsNull(SimPopulation.Active.Find(s.id), "no second figure in the world");
            Assert.AreSame(c.actor, session.PartyActor(s.id), "a party member the enemies can turn on");
            Assert.AreEqual(SimCompanion.MaxHealthFor(s), c.actor.Health.Pool.Max);
            // Follows: the player steps away, the sim comes after.
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(c.transform.position + Vector3.forward * 12);
            float before = Vector3.Distance(c.transform.position, session.Player.transform.position);
            yield return new WaitForSeconds(4);
            Assert.Less(Vector3.Distance(c.transform.position, session.Player.transform.position), before - 3, "it follows");
            session.LeaveParty(s.id);
            Assert.IsNull(session.PartySim(s.id)); Assert.NotNull(SimPopulation.Active.Find(s.id), "back in the world as a figure");
        }

        [UnityTest] public IEnumerator The_busy_and_the_far_off_decline_and_the_party_holds_three()
        {
            var s = Willing(); yield return null;
            s.friendly = .05f; StringAssert.Contains("declines", session.InviteRefusal(s)); Assert.IsFalse(session.Invite(s.id));
            s.friendly = .9f; s.level = session.Progress.Level + EncounterSession.InviteLevelGap + 1; StringAssert.Contains("too far apart", session.InviteRefusal(s));
            s.level = session.Progress.Level;
            var pop = SimPopulation.Active; int joined = 0;
            foreach (var x in pop.World.sims.Take(5)) { x.zone = "zone.oakhaven"; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = session.Progress.Level; }
            pop.Refresh(); yield return null;
            foreach (var x in pop.World.sims.Take(5)) if (session.Invite(x.id)) joined++;
            Assert.AreEqual(EncounterSession.MaxPartySims, joined); Assert.AreEqual("Your party is full.", session.InviteRefusal(pop.World.sims[4]));
        }

        [UnityTest] public IEnumerator A_party_sim_fights_your_target()
        {
            var s = Willing("class.ranger"); yield return null;
            Assert.IsTrue(session.Invite(s.id)); var c = session.PartySim(s.id);
            var e = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Game); Assert.NotNull(e);
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 12);
            c.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(e.transform.position + Vector3.back * 11 + Vector3.left * 2);
            session.Select(e); e.Receive(1, session.Player);   // the fight is on
            for (float w = 0; w < 2 && e.GroupShares <= 0; w += Time.deltaTime) yield return null;   // scaled to the group as it starts (EncounterEnemy.GroupScale)
            int hp = e.actor.Health.Pool.Current;
            float t = 0; while (t < 6 && e.actor.Health.Pool.Current > hp - c.Hit) { t += Time.deltaTime; yield return null; }
            Assert.LessOrEqual(e.actor.Health.Pool.Current, hp - c.Hit, s.name + " (" + c.Activity + ") shot at it");
        }

        [UnityTest] public IEnumerator Mobs_grow_with_the_group_by_its_levels()
        {
            var e = session.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Game); Assert.NotNull(e);
            int mobLevel = e.actor.Level, alone = e.actor.Health.Pool.Max;
            Assert.AreEqual(0, EncounterEnemy.SharesFor(session, mobLevel), 1e-4f, "alone (with Mira): as tuned");
            var pop = SimPopulation.Active; var two = pop.World.sims.Take(2).ToList();
            foreach (var x in two) { x.zone = "zone.oakhaven"; x.onlineFrom = 0; x.onlineHours = 24; x.friendly = .9f; x.level = session.Progress.Level; }
            pop.Refresh(); yield return null;
            foreach (var x in two) Assert.IsTrue(session.Invite(x.id), x.name);
            // Each sim's share is its level over the mob's, a quarter to one and a quarter.
            float shares = two.Sum(x => Mathf.Clamp((float)x.level / mobLevel, EncounterEnemy.MinShare, EncounterEnemy.MaxShare));
            Assert.AreEqual(shares, EncounterEnemy.SharesFor(session, mobLevel), 1e-4f);
            two[1].level = 1; Assert.Less(EncounterEnemy.SharesFor(session, Mathf.Max(8, mobLevel)), shares + .001f, "a low sim adds little");
            two[1].level = session.Progress.Level;
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 3); session.Select(e);
            e.Receive(1, session.Player);
            float t = 0; while (t < 3 && e.GroupShares <= 0) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(shares, e.GroupShares, 1e-3f, "scaled when the fight starts");
            Assert.AreEqual(Mathf.Round(alone * (1 + EncounterEnemy.HealthPerShare * shares)), e.actor.Health.Pool.Max, 1, "health grows by the shares");
            StringAssert.Contains("group of 3", e.GroupNote);
            e.ResetFight();
            Assert.AreEqual(alone, e.actor.Health.Pool.Max, "and is itself again when the fight resets");
        }
    }
}
#endif
