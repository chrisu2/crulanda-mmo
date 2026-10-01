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
    /// What the player sells feeds the village's day (VillageLife.Stock, cleared at 04:00 like every delivery): ore and timber
    /// go to the forge, herbs to the stall, and the trades notice: Brannoc Vell talks of the Crowsfoot ore, the merchant of the
    /// fresh herbs. Anything with no trade key is only sold.
    /// </summary>
    public class VillageSupplyTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 10;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-supply-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator SellingOre_DeliversForgeOre_AndVellRemarks()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var life = VillageLife.Active; var p = s.Progress;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var vell = life.Villagers.First(v => v.Role == "blacksmith");
            Assert.AreEqual(0, life.Count("forge.ore")); Assert.AreEqual(0, life.Count("stall.herbs"));
            // Nobody has brought ore today, so Vell has nothing to say of it.
            for (int i = 0; i < 60; i++) StringAssert.DoesNotContain("with a pick", life.LineFor(vell, true));
            Assert.AreEqual(0, Inventory.Add(p, s.Items, "mat.copper_ore", 7)); Assert.AreEqual(0, Inventory.Add(p, s.Items, "mat.yarrow", 3)); Assert.AreEqual(0, Inventory.Add(p, s.Items, "potion.minor", 1));
            // All in one frame: a merchant's window shuts when you are not beside them.
            s.OpenVendor(vell); Assert.AreEqual(vell.Name, s.VendorNpc, "The smith buys and sells.");
            int gold = p.gold;
            s.SellBag(p.bag.FindIndex(x => x.item == "mat.copper_ore"));
            Assert.AreEqual(gold + 7, p.gold, "Copper ore sells at 1 gold a piece."); Assert.AreEqual(7, life.Count("forge.ore"), "The ore is the forge's delivery today.");
            s.SellBag(p.bag.FindIndex(x => x.item == "mat.yarrow"));
            Assert.AreEqual(3, life.Count("stall.herbs"), "Herbs go to the stall, whoever buys them.");
            int keys = life.Stock.Count; s.SellBag(p.bag.FindIndex(x => x.item == "potion.minor"));
            Assert.AreEqual(keys, life.Stock.Count, "A potion is no trade's delivery.");
            s.CloseVendor();
            // Vell remarks on it (a third of what he says to you is the day's news).
            bool remarked = false;
            for (int i = 0; i < 300 && !remarked; i++) remarked = life.LineFor(vell, true) == "Someone's been up the Crowsfoot with a pick. First ore I've not had to beg for.";
            Assert.IsTrue(remarked, "Vell talks of the ore.");
            // And the merchant of the herbs (the hen-wife's eggs, when in, are her first news).
            life.Stock.Remove("stall.eggs");
            var merchant = life.Villagers.First(v => v.Role == "merchant"); bool herbs = false;
            for (int i = 0; i < 300 && !herbs; i++) herbs = life.LineFor(merchant, true) == "Fresh-cut herbs on the stall. Somebody's been in the meadow.";
            Assert.IsTrue(herbs, "The merchant talks of the herbs.");
            yield return null;
        }
    }
}
#endif
