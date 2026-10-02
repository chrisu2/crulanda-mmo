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
    /// <summary>The day/night cycle in Oakhaven: hens roost at night, the hen-wife opens and feeds by day, and rounds them up at dusk.</summary>
    public class VillageDayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root;
        }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-day-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();   // the purses are kept for the play session
            Time.timeScale = 1; WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest] public IEnumerator At_night_hens_roost_behind_shut_doors_and_the_village_sleeps()
        {
            yield return Load(23.5f);
            var life = VillageLife.Active; Assert.NotNull(life);
            Assert.AreEqual(3, life.Zone.Coops.Count, "Oakhaven has three hen coops.");
            Assert.IsTrue(life.Zone.Coops.All(c => !c.Open), "Coops are shut at night.");
            var hens = life.Critters.Where(c => c.Coop != null).ToList();
            Assert.AreEqual(14, hens.Count, "Every Oakhaven chicken belongs to a coop.");
            Assert.IsTrue(hens.All(h => h.Roosting), "Hens roost at night.");
            Assert.AreEqual(3, life.Villagers.Count(v => v.Role == "henwife"));
            Assert.IsTrue(life.Villagers.Where(v => v.Role != "drinker" && !v.Resident).All(v => !v.Visible), "Everyone but the last drinkers (and residents at their posts) is abed.");
            Assert.IsTrue(life.Villagers.Where(v => v.Resident).All(v => v.Visible), "Residents keep their posts at night.");
            Assert.Greater(WorldClock.Darkness, .9f);
        }

        [UnityTest] public IEnumerator By_day_the_hen_wife_opens_up_and_scatters_feed()
        {
            yield return Load(8.6f);
            var life = VillageLife.Active;
            Assert.IsTrue(life.Zone.Coops.All(c => c.Open), "Coops are open by day.");
            Assert.IsTrue(life.Critters.Where(c => c.Coop != null).All(h => !h.Roosting), "Hens are out by day.");
            Time.timeScale = 3;
            var harrow = life.Zone.Coops[0];
            yield return WaitUntil(() => harrow.Feeding, 30);
            Assert.IsTrue(harrow.Feeding, "The hen-wife scatters feed in the morning.");
        }

        [UnityTest] public IEnumerator At_dusk_the_hens_go_in_and_the_door_is_shut()
        {
            yield return Load(17.5f);
            var life = VillageLife.Active; var coop = life.Zone.Coops[0];
            yield return new WaitForSeconds(.5f);
            var hens = life.HensOf(coop).ToList();
            Assert.Greater(hens.Count, 0); Assert.IsTrue(hens.All(h => !h.Roosting));
            WorldClock.Hour = 19.6f; Time.timeScale = 3;
            yield return WaitUntil(() => hens.All(h => h.Roosting) && !coop.Open, 45);
            Assert.IsTrue(hens.All(h => h.Roosting), "Every hen is in by nightfall (" + hens.Count(h => h.Roosting) + "/" + hens.Count + ").");
            Assert.IsFalse(coop.Open, "The hen-wife shuts the coop once they're all in.");
        }
    }
}
#endif
