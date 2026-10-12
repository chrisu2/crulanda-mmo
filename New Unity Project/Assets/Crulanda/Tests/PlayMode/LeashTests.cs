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
{    /// <summary>Chris 13:05, "leash is too short on fighting boards. they keep reseting even while fighting": a boar fought 25 m from home
    /// keeps fighting; one drawn far off with no blows for a while gives up.</summary>
    public class LeashTests
    {        string root; EncounterSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-t-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("PlayableEncounter", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(session);
            session.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 100000); session.Player.Health.ApplyHealing(1000000);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("PlayableEncounter");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }        EncounterEnemy Boar()
        {
            var all = session.Enemies.Where(e => e != null && e.actor.IsAlive && !e.Elite && !e.Game && e.Group == null).ToList();
            var b = all.FirstOrDefault(e => e.actor.DisplayName.ToLower().Contains("boar")) ?? all.FirstOrDefault(); Assert.NotNull(b, "a lone mob"); return b;
        }
        [UnityTest] public IEnumerator A_boar_fought_25_m_from_home_does_not_reset()
        {
            var b = Boar(); var home = b.Home; var agent = b.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var dir = Vector3.forward; var at = home + dir * 25;
            UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 10, UnityEngine.AI.NavMesh.AllAreas); agent.Warp(hit.position);
            session.Player.GetComponent<AdventurerMotor>().Teleport(hit.position + dir * 8 + Vector3.up);   // ranged: kiting 8 m off
            b.Receive(1, session.Player); yield return null;
            for (float t = 0; t < 10; t += 1) { b.Receive(1, session.Player); yield return new WaitForSeconds(1); Assert.IsTrue(b.Engaged, "still fighting at " + t + " s, " + Vector3.Distance(b.transform.position, home).ToString("0") + " m from home"); }
        }
        [UnityTest] public IEnumerator A_boar_pulled_far_with_no_combat_resets()
        {
            var b = Boar(); var home = b.Home; var agent = b.GetComponent<UnityEngine.AI.NavMeshAgent>();
            b.Receive(1, session.Player); yield return null;
            UnityEngine.AI.NavMesh.SamplePosition(home + Vector3.forward * 70, out var hit, 20, UnityEngine.AI.NavMesh.AllAreas); agent.Warp(hit.position);
            session.Player.GetComponent<AdventurerMotor>().Teleport(hit.position + Vector3.forward * 6 + Vector3.up);
            Assert.Greater(Vector3.Distance(b.transform.position, home), b.FarLeash, "past the far leash");
            for (float t = 0; t < EncounterEnemy.QuietSeconds + 3 && b.Engaged; t += Time.deltaTime) yield return null;
            Assert.IsFalse(b.Engaged, "gave up after a quiet spell far from home");
        }
    }
}
#endif