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
    /// <summary>
    /// The jump (2026-10-10, Chris: "jumping animation still borked. still feel floaty"): it tops out a little under a metre, is over in
    /// about half a second, and comes down quicker than it went up.
    /// </summary>
    public class JumpTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-jump-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator A_jump_is_quick_and_falls_faster_than_it_rises()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = ZoneBuilder.Active;
            var road = zone.Zone.roads.OrderByDescending(r => r.points.Length).First();
            var motor = session.Player.GetComponent<AdventurerMotor>();
            motor.Teleport(zone.Ground((road.points[0] + road.points[1]) / 2, 1.1f));
            for (int i = 0; i < 30; i++) yield return null;   // settled on the ground
            float y0 = motor.transform.position.y, top = y0, start = 0, apexAt = 0, landAt = 0;
            motor.Jump();
            float deadline = Time.time + 3;
            while (Time.time < deadline)
            {
                yield return null;
                if (motor.Airborne && start == 0) start = Time.time - Time.deltaTime;
                float y = motor.transform.position.y; if (y > top) { top = y; apexAt = Time.time; }
                if (start > 0 && !motor.Airborne) { landAt = Time.time; break; }
            }
            Assert.Greater(start, 0, "left the ground");
            Assert.Greater(landAt, 0, "came down");
            Assert.That(top - y0, Is.InRange(.75f, 1.05f), "tops out a little under a metre");
            Assert.That(landAt - start, Is.InRange(.42f, .65f), "over in about half a second");
            Assert.Less(landAt - apexAt, apexAt - start, "comes down quicker than it went up");
        }
    }
}
#endif
