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
    /// <summary>Chris, 2026-10-15: "need mobs patrolling more instead of just standing in one spot". Oakhaven's camps have roamers walking
    /// loops outside their camps, and one sets off and covers ground within half a minute.</summary>
    public class RoamerTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-roamers-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        [UnityTest] public IEnumerator Oakhavens_camps_have_roamers_that_walk_their_loops()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            float until = Time.time + 20; while (s.Enemies.Count == 0 && Time.time < until) yield return null;
            var roamers = s.Enemies.Where(e => e != null && e.Roamer).ToList();
            Assert.GreaterOrEqual(roamers.Count, 5, "Oakhaven's open camps have roamers (" + roamers.Count + ")");
            foreach (var r in roamers)
            {
                float far = r.Patrol.Max(p => Vector2.Distance(new Vector2(p.x, p.z), r.CampCenter));
                Assert.Greater(far, r.CampRadius + 3, r.name + ": its loop goes well outside its camp");
            }
            // One near the player is stood beside and watched: it walks.
            var one = roamers.OrderBy(r => Vector3.Distance(r.transform.position, s.Player.transform.position)).First();
            s.Player.GetComponent<AdventurerMotor>().Teleport(one.transform.position + new Vector3(40, 0, 0));
            var start = one.transform.position; float walked = 0; var last = start;
            for (float t = 0; t < 30; t += Time.deltaTime) { walked += Vector3.Distance(last, one.transform.position); last = one.transform.position; yield return null; }
            Assert.Greater(walked, 8, one.name + " walked " + walked.ToString("0.0") + " m in 30 s");
        }
    }
}
#endif
