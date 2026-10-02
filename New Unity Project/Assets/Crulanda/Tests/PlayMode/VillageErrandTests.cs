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
    /// The trades' errands in Oakhaven (<see cref="VillageWork"/>): the hen-wife collects her eggs and carries them, in a basket you can
    /// see, to the inn's kitchen, and the merchant then sells them on; water from the well fills the hens' pan; the farmer's barley
    /// reaches the mill on his shoulder.
    /// </summary>
    public class VillageErrandTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-errand-" + Guid.NewGuid().ToString("N"));
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

        [UnityTest] public IEnumerator The_hen_wife_carries_her_eggs_to_the_inn_and_the_merchant_sells_them_on()
        {
            yield return Open(8.45f);   // the morning feed is done by now in her day; the eggs are due
            var life = VillageLife.Active; var session = life.Session;
            foreach (var c in life.Zone.Coops) { c.Eggs = 4; c.FedAt = Time.time; }
            Time.timeScale = 4;
            Villager carrier = null;
            yield return WaitUntil(() => (carrier = life.Villagers.FirstOrDefault(v => v.Role == "henwife" && v.Carrying && v.Carried == Load.Eggs)) != null, 60);
            Assert.NotNull(carrier, "A hen-wife collects the eggs and carries them (" + string.Join(", ", life.Villagers.Where(v => v.Role == "henwife").Select(v => v.Name + ": " + v.Activity)) + ").");
            Assert.NotNull(carrier.Errand); Assert.AreEqual("eggs to the inn", carrier.Errand.id);
            Assert.IsTrue(carrier.GetComponentsInChildren<Renderer>().Any(r => r.transform.parent != null && r.transform.parent.name == "Load Eggs"), "The basket of eggs shows on her arm.");
            yield return WaitUntil(() => life.Count("inn.eggs") > 0 && !carrier.Carrying, 120);   // she herself has handed over (three hen-wives are at it)
            Assert.Greater(life.Count("inn.eggs"), 0, "The eggs reach the inn's kitchen (" + carrier.Name + ": " + carrier.Activity + ", carrying " + carrier.Carrying + ").");
            Assert.IsFalse(carrier.Carrying, "Handed over.");
            Assert.IsTrue(carrier.Done("eggs to the inn"));

            // The afternoon's eggs go to the stall, and the merchant sells them while they last.
            Time.timeScale = 1; WorldClock.Hour = 15.45f;
            foreach (var c in life.Zone.Coops) c.Eggs = 3;
            Time.timeScale = 4;
            yield return WaitUntil(() => life.Count("stall.eggs") > 0, 200);   // the afternoon's water and feed come first in her day
            Assert.Greater(life.Count("stall.eggs"), 0, "Eggs reach the produce stall.");
            Time.timeScale = 1;
            var merchant = life.Villagers.First(v => v.Role == "merchant");
            session.OpenVendor(merchant);
            Assert.AreEqual(merchant.Name, session.VendorNpc); Assert.AreEqual(EncounterSession.FreshEggs, session.VendorStock[0], "Fresh eggs head the merchant's wares.");
            int had = life.Count("stall.eggs"); session.Progress.gold += 1000;
            session.Buy(EncounterSession.FreshEggs);
            Assert.AreEqual(had - 1, life.Count("stall.eggs"), "Each sale takes an egg-basket off the stall.");
            Assert.AreEqual(1, Inventory.Count(session.Progress, EncounterSession.FreshEggs));
            for (int i = 0; i < 40 && life.Count("stall.eggs") > 0; i++) session.Buy(EncounterSession.FreshEggs);
            Assert.AreEqual(0, life.Count("stall.eggs"), "The stall can be bought out.");
            Assert.IsFalse(session.VendorStock.Contains(EncounterSession.FreshEggs), "Sold out: off the list.");
        }

        [UnityTest] public IEnumerator Water_from_the_well_fills_the_hens_pan_and_the_farmer_takes_his_barley_to_the_mill()
        {
            yield return Open(9.65f);
            var life = VillageLife.Active;
            foreach (var c in life.Zone.Coops) c.FedAt = Time.time;   // fed already this morning
            Assert.IsTrue(life.Zone.Coops.All(c => c.Level <= 0 && c.water != null && !c.water.gameObject.activeSelf), "The pan starts dry.");
            Time.timeScale = 4;
            Villager carrier = null;
            yield return WaitUntil(() => (carrier = life.Villagers.FirstOrDefault(v => v.Role == "henwife" && v.Carried == Load.Bucket)) != null, 90);
            Assert.NotNull(carrier, "A hen-wife draws water at the well and carries the bucket.");
            yield return WaitUntil(() => carrier.Coop.Level > .9f, 150);
            Assert.Greater(carrier.Coop.Level, .9f, "She fills the pan (" + carrier.Activity + ").");
            Assert.IsTrue(carrier.Coop.water.gameObject.activeSelf, "and the water shows in it");
            Assert.IsTrue(carrier.Coop.Feeding, "The hens come to drink.");
            Assert.AreEqual(carrier.Coop.pan, carrier.Coop.FeedSpot);

            Time.timeScale = 1; WorldClock.Hour = 10.05f; Time.timeScale = 4;
            Villager farmer = null;
            yield return WaitUntil(() => life.Count("mill.grain") > 0 || (farmer = life.Villagers.FirstOrDefault(v => v.Role == "farmer" && v.Carried == Load.Grain)) != null, 90);
            Assert.IsTrue(farmer != null || life.Count("mill.grain") > 0, "A farmer shoulders a sack of barley in his field (" + string.Join(", ", life.Villagers.Where(v => v.Role == "farmer").Select(v => v.Name + ": " + v.Activity)) + ").");
            yield return WaitUntil(() => life.Count("mill.grain") > 0, 150);
            Assert.Greater(life.Count("mill.grain"), 0, "and it reaches the mill (" + (farmer != null ? farmer.Activity : "already there") + ").");
        }
    }
}
#endif
