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
{    /// <summary>Chris 12:56 "only foot issue i see so far is it fall below the ground" and 12:57 "and these are hovering above the ground":
    /// standing, the lower sole of the player, a sim and a villager is within 2 cm of the ground under it, on grass and on the road.</summary>
    public class GroundingTests
    {        string root; EncounterSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-t-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(session);
            session.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 100000); session.Player.Health.ApplyHealing(1000000);
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }        static IEnumerator Settle(float s) { for (float t = 0; t < s; t += Time.deltaTime) yield return null; }
        void Check(ActorVisual v, string who)
        {
            Assert.NotNull(v, who + " has a figure"); float gap = v.SoleGap;
            Assert.That(gap, Is.InRange(-.02f, .02f), who + ": the lower sole " + (gap * 100).ToString("0.0") + " cm off the ground (fix " + (v.GroundFix * 100).ToString("0.0") + " cm)");
        }
        [UnityTest] public IEnumerator Player_sim_and_villager_stand_on_the_ground()
        {
            var motor = session.Player.GetComponent<AdventurerMotor>(); var zone = ZoneBuilder.Active;
            // Grass: the open field by the spawn.
            motor.Teleport(zone.Ground(new Vector2(14, 14), 1.1f)); yield return Settle(1.5f);
            Check(session.Player.GetComponentInChildren<ActorVisual>(), "the player on grass");
            // The road: under the nearest villager on its round, the player beside them.
            var villagers = UnityEngine.Object.FindObjectsByType<Villager>(FindObjectsSortMode.None).Where(x => x.GetComponent<ActorVisual>() != null).ToList();
            Assert.Greater(villagers.Count, 0, "villagers");
            var v = villagers[0]; v.enabled = false; var ag = v.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.isOnNavMesh) ag.isStopped = true;
            motor.Teleport(v.transform.position + v.transform.right * 1.5f + Vector3.up); yield return Settle(1.5f);
            Check(v.GetComponentInChildren<ActorVisual>(), "a villager (" + v.Name + ")");
            Check(session.Player.GetComponentInChildren<ActorVisual>(), "the player by the villager");
            var pop = SimPopulation.Active;
            if (pop != null && pop.Figures.Count > 0) { var f = pop.Figures.First(x => x != null); yield return Settle(.5f); Check(f.GetComponent<ActorVisual>() ?? f.GetComponentInChildren<ActorVisual>(), "a sim (" + f.sim.name + ")"); }
        }
    }
}
#endif