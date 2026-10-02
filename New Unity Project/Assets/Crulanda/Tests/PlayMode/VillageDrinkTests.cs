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
    /// The out of work at the inn (Oakhaven's two drinkers): they come in by midday and drink, a tankard in the hand, off the day's cask
    /// (the village's stock, "inn.ale"); when the cask is dry they call it a day and go home; one who has had enough folds over the
    /// table and snores, another says goodnight while he can.
    /// </summary>
    public class VillageDrinkTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-drink-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();   // the purses are kept for the play session
            Time.timeScale = 1; WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }
        static string Tell(VillageLife life) { return string.Join(", ", life.Villagers.Where(v => v.Role == "drinker").Select(v => v.Name + ": " + v.Activity + " drunk " + v.Drunk.ToString("0.00") + (v.Spent ? " spent" : "") + (v.Visible ? "" : " indoors"))); }

        [UnityTest] public IEnumerator The_out_of_work_drink_at_the_inn_until_the_ale_is_gone_then_go_home()
        {
            yield return Open(11.1f);
            var life = VillageLife.Active; var drinkers = life.Villagers.Where(v => v.Role == "drinker").ToList();
            Assert.GreaterOrEqual(drinkers.Count, 2, "Oakhaven has its out-of-work.");
            // What is left in yesterday's cask, less a round for anyone who was sat at the inn already when the scene began.
            Assert.That(life.Count("inn.ale"), Is.InRange(VillageLife.MorningAle - drinkers.Count, VillageLife.MorningAle), "The inn starts the day with what is left in yesterday's cask.");
            life.Stock["inn.ale"] = 3;   // nearly dry, so the test need not sit through a whole cask
            Time.timeScale = 4;
            var before = drinkers.ToDictionary(d => d, d => d.Drunk);   // one may have had a round already, sat at the inn when the scene began
            Villager first = null;
            yield return WaitUntil(() => (first = drinkers.FirstOrDefault(d => d.Drunk > before[d])) != null, 90);
            Assert.NotNull(first, "A drinker reaches the inn and has one (" + Tell(life) + ").");
            Assert.AreEqual("inn", first.Activity); Assert.Less(life.Count("inn.ale"), 3, "It comes off the cask.");
            Assert.AreEqual(ActorPose.Drink, first.GetComponent<ActorVisual>().Pose, "Sat drinking.");
            Assert.IsTrue(first.GetComponentsInChildren<Transform>().Any(t => t.name == "Tankard" && t.gameObject.activeInHierarchy), "A tankard in the hand.");
            yield return WaitUntil(() => life.Count("inn.ale") == 0 && drinkers.Any(d => d.Spent), 150);
            Assert.AreEqual(0, life.Count("inn.ale"), "They drink the cask dry (" + Tell(life) + ").");
            var done = drinkers.FirstOrDefault(d => d.Spent); Assert.NotNull(done, "Dry: someone calls it a day (" + Tell(life) + ").");
            yield return WaitUntil(() => !done.Visible, 120);
            Assert.IsFalse(done.Visible, "and goes home to sleep it off (" + Tell(life) + ").");
            Assert.IsFalse(done.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Tankard" && t.gameObject.activeInHierarchy), "The tankard stays at the inn.");
        }

        [UnityTest] public IEnumerator One_who_has_had_enough_passes_out_over_the_table_and_another_says_goodnight()
        {
            yield return Open(11.1f);
            var life = VillageLife.Active; var drinkers = life.Villagers.Where(v => v.Role == "drinker").ToList();
            foreach (var d in drinkers) d.Tolerance = .01f;   // one is enough for anybody today
            life.Stock["inn.ale"] = 40;
            Time.timeScale = 4;
            Villager out1 = null;
            yield return WaitUntil(() => (out1 = drinkers.FirstOrDefault(d => d.PassedOut)) != null, 150);
            Assert.NotNull(out1, "One folds over the table (" + Tell(life) + ").");
            Assert.AreEqual(ActorPose.Slump, out1.GetComponent<ActorVisual>().Pose);
            Assert.AreEqual("Zzz...", life.LineFor(out1, true), "Dead to the world: no talk out of him.");
            Assert.IsTrue(out1.Visible, "Still there at the table.");
            Villager left = null;
            yield return WaitUntil(() => (left = drinkers.FirstOrDefault(d => d.Spent && !d.PassedOut)) != null, 150);
            Assert.NotNull(left, "Another says goodnight while he can (" + Tell(life) + ").");
            Assert.Greater(left.GetComponent<ActorVisual>().Stagger, .5f, "and reels home.");
        }
    }
}
#endif
