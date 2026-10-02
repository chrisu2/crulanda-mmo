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
    /// fresh herbs (the player's sale, marked "sold.stall.herbs", not the herbalist's own daily herbs, which leave the stall's
    /// other news be). Anything with no trade key is only sold.
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
            VillageEconomy.ResetAll();   // the purses are kept for the play session
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
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
            Assert.AreEqual(3, life.Count("sold.stall.herbs"), "The sale is marked as the player's.");
            yield return null;
        }

        /// <summary>
        /// Meat sold in the village (BUILD_PLAN step 10: boar meat and the beasts' meats are materials whose trade is "inn.meat") goes
        /// to the inn's pot as the player's ("sold.inn.meat"), and the innkeeper and the village talk of somebody's hunting, not of the
        /// hunter's hares.
        /// </summary>
        [UnityTest] public IEnumerator SellingMeat_FeedsTheInnsPot_AndTheVillageRemarks()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var life = VillageLife.Active; var p = s.Progress;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            var keeper = life.Villagers.First(v => v.Role == "innkeeper");
            int pot = life.Count("inn.meat"); Assert.AreEqual(0, life.Count("sold.inn.meat"));
            Assert.AreEqual(0, Inventory.Add(p, s.Items, "junk.boar_meat", 4)); Assert.AreEqual(0, Inventory.Add(p, s.Items, "mat.wolf_haunch", 2));
            // All in one frame: a merchant's window shuts when you are not beside them.
            s.OpenVendor(keeper); Assert.AreEqual(keeper.Name, s.VendorNpc, "The innkeeper buys and sells.");
            int gold = p.gold;
            s.SellBag(p.bag.FindIndex(x => x.item == "junk.boar_meat")); s.SellBag(p.bag.FindIndex(x => x.item == "mat.wolf_haunch"));
            Assert.AreEqual(gold + 6, p.gold, "A gold a piece.");
            Assert.AreEqual(pot + 6, life.Count("inn.meat"), "The meat is the inn's pot today."); Assert.AreEqual(6, life.Count("sold.inn.meat"), "The sale is marked as the player's.");
            s.CloseVendor();
            life.Deliver("inn.ale", 1);   // the cask is in, so a dry inn is nobody's first news
            const string Hares = "Garet's hares are in the pot. Don't tell the out-of-work.";
            bool said = false;
            for (int i = 0; i < 300 && !said; i++)
            {
                var line = life.LineFor(keeper, true); Assert.AreNotEqual(Hares, line, "Meat the player sold is not the hunter's hares.");
                said = line == "Somebody's been selling meat in the village. It's all in the pot; the stew's not thin tonight.";
            }
            Assert.IsTrue(said, keeper.Name + " talks of the meat.");
            var neighbour = life.Villagers.FirstOrDefault(v => (v.Role == "farmer" || v.Role == "elder" || v.Role == "gossip") && !v.PassedOut);
            Assert.NotNull(neighbour, "Oakhaven has a farmer, an elder or a gossip to talk to.");
            said = false;
            for (int i = 0; i < 300 && !said; i++) said = life.LineFor(neighbour, true) == "Boar in the Cask's pot tonight. Somebody's been hunting.";
            Assert.IsTrue(said, neighbour.Name + " talks of it.");
            yield return null;
        }

        /// <summary>
        /// The Shattered Peaks has people (an elder, a merchant, and the rest talking as gossips) but no inn, so meat sold there is
        /// only sold: nobody talks of an inn's pot, or of any of the inn's news of the day.
        /// </summary>
        [UnityTest] public IEnumerator SellingMeat_WhereThereIsNoInn_NobodyTalksOfAPot()
        {
            ZoneBuilder.RequestedZoneId = "zone.peaks";
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var life = VillageLife.Active;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.AreEqual("zone.peaks", s.Zone.Zone.id); Assert.NotNull(life, "The Peaks has its people.");
            Assert.AreEqual(0, life.Places["inn"].Count, "The Peaks has no inn.");
            life.Deliver("inn.meat", 3); life.Deliver("sold.inn.meat", 3);   // as EncounterSession.SellBag delivers meat sold to any merchant
            var innNews = new[] { "Boar in the Cask's pot tonight. Somebody's been hunting.", "Meat in the pot at the inn tonight. Somebody's been hunting.",
                "Hare in the Cask's pot tonight. The hunter's doing.", "The cask's run dry at the inn. The out-of-work drank it by supper.",
                "Bread and eggs at the Cask today. Like old times, nearly.", "The Cask's got a fire going. Dry oak, for once." };
            var talkers = life.Villagers.Where(v => v.Role == "elder" || v.Role == "gossip" || v.Role == "drinker" || v.Role == "farmer").ToList();
            Assert.IsNotEmpty(talkers, "The Peaks has an elder or a gossip to talk to.");
            foreach (var v in talkers)
                for (int i = 0; i < 300; i++)
                {
                    var line = life.LineFor(v, true);
                    CollectionAssert.DoesNotContain(innNews, line, v.Name + " talks of an inn the Peaks hasn't got.");
                }
            yield return null;
        }

        [UnityTest] public IEnumerator HerbalistsOwnHerbs_DoNotSoundLikeTheSale_NorHideTheStallsGoods()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var life = VillageLife.Active;
            Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            // As every afternoon, the herbalist has brought her herbs to the stall ("herbs to the stall"); the player has sold none.
            life.Stock.Remove("stall.eggs"); life.Stock.Remove("stall.flour"); life.Stock.Remove("sold.stall.herbs");
            life.Deliver("stall.herbs", 2); life.Deliver("stall.goods", 1);
            var merchant = life.Villagers.First(v => v.Role == "merchant"); bool goods = false;
            for (int i = 0; i < 300; i++)
            {
                var line = life.LineFor(merchant, true);
                Assert.AreNotEqual("Fresh-cut herbs on the stall. Somebody's been in the meadow.", line, "The herbalist's own herbs are no news.");
                goods |= line == "Belts and nails and hinges, all village-made. Nothing from the east.";
            }
            Assert.IsTrue(goods, "The stall's goods are still talked of.");
            yield return null;
        }
    }
}
#endif
