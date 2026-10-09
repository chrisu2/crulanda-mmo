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

namespace Crulanda.Tests
{
    /// <summary>Boss mechanics (D2): a stamp stuns you and arms it heavier, a reset turns it on another, a knockback stays on the ground,
    /// a disarm stops your swings, and the Adit's bosses carry their moves.</summary>
    public class BossPhaseTests
    {
        string root; EncounterSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-boss-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        // An elite in the open (not a cave's): the nearest to the village square.
        EncounterEnemy Elite()
        {
            var e = session.Enemies.Where(x => x != null && x.Elite && x.actor.IsAlive && !x.Hidden && x.Move != null && !x.Move.boss)
                .OrderBy(x => Vector3.Distance(x.transform.position, session.Player.transform.position)).FirstOrDefault();
            Assert.NotNull(e, "an elite"); return e;
        }
        IEnumerator Until(EncounterEnemy e, Func<bool> done)
        { float t = 0; while (t < 3 && !done()) { e.threat.Add(session.Player.EntityId.Value, 10); t += Time.deltaTime; yield return null; } }
        IEnumerator Fight(EncounterEnemy e)
        {
            session.Player.GetComponent<AdventurerMotor>().Teleport(e.transform.position + Vector3.back * 2.5f); session.Select(e);
            float t = 0; while (t < 3 && e.Victim != session.Player) { e.threat.Add(session.Player.EntityId.Value, 10); t += Time.deltaTime; yield return null; }   // mobs think on a timer
            Assert.AreEqual(session.Player, e.Victim, "it fights you");
        }

        [Test] public void The_Adit_bosses_carry_their_moves()
        {
            Assert.AreEqual(2, EliteMoves.For("Quartermaster Brannigan Sorrel", false).phases.Count(p => p.kind == "stamp"));
            var foreman = EliteMoves.For("The Foreman Who Forgot", false);
            Assert.IsTrue(foreman.phases.Any(p => p.kind == "reset") && foreman.phases.Any(p => p.kind == "terrify")); Assert.Greater(foreman.blinkEvery, 0);
            var ysolt = EliteMoves.For("Cinder-Warden Ysolt", false); Assert.Greater(ysolt.knock, 0); Assert.Greater(ysolt.ringEvery, 0);
            Assert.Greater(EliteMoves.For("Nix, the turncoat", false).disarm, 0);
            Assert.Greater(EliteMoves.For(EncounterSession.RockEaterName, false).knock, 0, "the Rock-Eater's drill throws you back");
            Assert.IsTrue(EliteMoves.For("Rail-Captain Orsk Danner", false).callOnPull);
            Assert.AreEqual(3, EliteMoves.For("Gang-Boss Haddo Lusk", false).deathCall);
        }
        [UnityTest] public IEnumerator A_stamp_stuns_you_and_arms_it_heavier()
        {
            var e = Elite();
            e.ArmElite(new EliteMove { name = "Test Blow", every = 99, first = 99, phases = new[] { new BossPhase { at = .9f, kind = "stamp", seconds = 2 } } });
            yield return Fight(e);
            e.actor.Health.ApplyDamage(Mathf.CeilToInt(e.actor.Health.Pool.Max * .15f));
            yield return Until(e, () => session.PlayerStunned);
            Assert.IsTrue(session.PlayerStunned, "stunned by " + e.Name); Assert.Greater(e.WeaponUp, 1.2f, "a heavier weapon");
            Assert.IsFalse(session.UseAbility(0), "no abilities while stunned");
        }
        [UnityTest] public IEnumerator A_reset_and_a_knockback_and_a_disarm()
        {
            var e = Elite();
            e.ArmElite(new EliteMove { name = "Test Blow", every = 99, first = 99, phases = new[] { new BossPhase { at = .9f, kind = "reset" } } });
            yield return Fight(e);
            e.actor.Health.ApplyDamage(Mathf.CeilToInt(e.actor.Health.Pool.Max * .15f));
            yield return Until(e, () => e.Victim == session.Player);
            Assert.AreEqual(session.Player, e.Victim, "with only you to fight, " + e.Name + " comes back to you");
            // Stand beside it on open ground, room behind you to be thrown into (against a wall or an edge a knockback does nothing).
            var motor = session.Player.GetComponent<AdventurerMotor>(); bool open = false;
            UnityEngine.AI.NavMesh.SamplePosition(session.Player.transform.position, out var feet, 2.5f, UnityEngine.AI.NavMesh.AllAreas);
            float lift = session.Player.transform.position.y - feet.position.y;
            for (int i = 0; i < 8 && !open; i++)
            {
                var d = Quaternion.Euler(0, i * 45, 0) * Vector3.forward;
                if (!UnityEngine.AI.NavMesh.SamplePosition(e.transform.position + d * 2.5f, out var f, 1.5f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                if (UnityEngine.AI.NavMesh.Raycast(f.position, f.position + d * 5, out _, UnityEngine.AI.NavMesh.AllAreas)) continue;
                motor.Teleport(f.position + Vector3.up * lift); open = true;
            }
            Assert.IsTrue(open, "open ground beside " + e.Name);
            var before = session.Player.transform.position;
            e.Knock(session.Player, 4); yield return null;
            var moved = session.Player.transform.position - before; moved.y = 0;
            Assert.Greater(moved.magnitude, .4f, "thrown back"); Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(session.Player.transform.position, out _, 1.5f, UnityEngine.AI.NavMesh.AllAreas), "onto walkable ground");
            session.DisarmPlayer(4, "test"); Assert.IsTrue(session.PlayerDisarmed);
        }
    }
}
#endif
