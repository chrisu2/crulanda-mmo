#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The generated world: Oakhaven builds, and walking to its west exit travels to Khaven with the save following.</summary>
    public class ZoneTravelTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root;
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-zones-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        [UnityTest] public IEnumerator Oakhaven_builds_and_west_road_travels_to_khaven()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session.Zone); Assert.AreEqual("zone.oakhaven", session.Zone.Zone.id);
            Assert.AreEqual(session.Zone.Zone.spawns.enemies.Length, session.StoryEnemies.Count);
            var p = session.Player.transform.position;
            Assert.Less(Mathf.Abs(p.y - (session.Zone.HeightAt(p.x, p.z) + 1.1f)), 1.5f, "Player stands on the generated ground.");
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(session.Enemies[0].transform.position, out _, 2, UnityEngine.AI.NavMesh.AllAreas), "Zone navmesh exists under enemies.");

            session.Player.GetComponent<AdventurerMotor>().Teleport(session.Zone.Ground(session.Zone.Zone.exits[0].at + new Vector2(1, 0), 1.1f));
            var exit = session.NearbyExit; Assert.NotNull(exit); Assert.AreEqual("zone.khaven", exit.to);
            Assert.IsTrue(session.TravelTo(exit));
            for (int i = 0; i < 20; i++) yield return null;

            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.NotNull(session.Zone); Assert.AreEqual("zone.khaven", session.Zone.Zone.id);
            Assert.AreEqual("zone.khaven", session.Progress.zoneId);
            var arrived = session.Player.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(arrived.x, arrived.z), exit.arrive), 1.5f, "Arrives at the linked road.");
            Assert.AreEqual("Sandthrone outrider", session.Enemies[0].actor.DisplayName);
            Assert.IsTrue(File.ReadAllText(Path.Combine(root, "encounter.save.json")).Contains("zone.khaven") ||
                Directory.GetFiles(root, "encounter*").Length > 0);
        }
    }
}
#endif
