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
    /// The groundwork of the trades in the running game (Oakhaven): the session reads the profession content, a merchant sells the
    /// tools and the makings, a pick used from the bags hangs at the belt and teaches Mining, a second one is kept, materials are
    /// not gear and not junk, the Trades window opens beside the bags, and the skill is in the save.
    /// </summary>
    public class TradesTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-trades-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator A_pick_bought_from_the_merchant_hangs_at_the_belt_and_the_trades_window_opens()
        {
            yield return Open(10);
            var life = VillageLife.Active; var session = life.Session; var p = session.Progress;
            Assert.AreEqual(root, session.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.NotNull(session.Professions, "The session read the profession content (Encounter.asset lists it: Crulanda > World > Build Oakhaven).");
            var trades = session.Professions;
            Assert.IsTrue(trades.Has("herbalism")); Assert.IsTrue(trades.Has("cooking")); Assert.IsFalse(trades.Has("mining")); Assert.IsFalse(trades.Has("woodcutting"));

            // The merchant sells the tools and the makings. (All in one frame: a merchant's window shuts when you are not beside it.)
            var merchant = life.Villagers.First(v => v.Role == "merchant");
            p.gold = 100;
            session.OpenVendor(merchant);
            Assert.AreEqual(merchant.Name, session.VendorNpc);
            foreach (var id in new[] { "tool.pick", "tool.hatchet", "mat.flour", "mat.salt", "mat.vial" }) Assert.Contains(id, session.VendorStock);
            session.Buy("tool.pick");
            Assert.AreEqual(92, p.gold, "A pick costs 8 gold."); Assert.AreEqual(1, Inventory.Count(p, "tool.pick"));
            session.CloseVendor();

            // Used from the bags, it hangs at the belt.
            Assert.IsTrue(session.EquipFromBag(p.bag.FindIndex(s => s.item == "tool.pick")));
            Assert.IsTrue(trades.Has("mining")); Assert.AreEqual(1, trades.Skill("mining")); Assert.AreEqual(0, Inventory.Count(p, "tool.pick"));
            Assert.Contains("You hang the pick at your belt. You can now mine.", session.Messages);
            Assert.IsNull(p.equipment.Find(s => s.item == "tool.pick"), "It is not worn as gear.");
            // The skill is in the save file.
            Assert.IsFalse(session.InCombat, "Nothing is fighting at the village gate, so the tool's save went through.");
            string file = Path.Combine(root, EncounterSave.SlotFor(session.ClassDef.id) + ".save.json");
            Assert.IsTrue(File.Exists(file), "The save was written.");
            string text = File.ReadAllText(file);   // the payload is a JSON string inside the envelope, so its quotes are escaped
            StringAssert.Contains("\"formatVersion\": " + EncounterSave.FormatVersion, text);
            StringAssert.Contains("{\\\"id\\\":\\\"mining\\\",\\\"skill\\\":1}", text, "Mining is saved.");
            StringAssert.Contains("{\\\"id\\\":\\\"herbalism\\\",\\\"skill\\\":1}", text); StringAssert.Contains("{\\\"id\\\":\\\"cooking\\\",\\\"skill\\\":1}", text);
            StringAssert.Contains("\\\"pouches\\\":[]", text);

            // A second pick is refused and kept.
            session.OpenVendor(merchant); session.Buy("tool.pick"); session.CloseVendor();
            int second = p.bag.FindIndex(s => s.item == "tool.pick"); Assert.GreaterOrEqual(second, 0);
            Assert.IsFalse(session.EquipFromBag(second));
            Assert.AreEqual(1, Inventory.Count(p, "tool.pick")); Assert.Contains("You already carry one.", session.Messages); Assert.AreEqual(1, trades.Skill("mining"));

            // A material is not gear, and "Sell junk" leaves it.
            Assert.AreEqual(0, Inventory.Add(p, session.Items, "mat.copper_ore", 4)); Assert.AreEqual(0, Inventory.Add(p, session.Items, "junk.wolf_pelt", 2));
            Assert.IsFalse(session.EquipFromBag(p.bag.FindIndex(s => s.item == "mat.copper_ore")));
            Assert.Contains(EncounterSession.MaterialLine, session.Messages);
            session.OpenVendor(merchant); session.SellJunk();
            Assert.AreEqual(4, Inventory.Count(p, "mat.copper_ore")); Assert.AreEqual(1, Inventory.Count(p, "tool.pick")); Assert.AreEqual(0, Inventory.Count(p, "junk.wolf_pelt"));

            // The Trades window: it takes the character sheet's place, shuts the merchant and opens the bags; a merchant shuts it.
            session.CharacterOpen = true; session.InventoryOpen = false;
            session.ShowTrades(true);
            Assert.IsTrue(session.TradesOpen); Assert.IsFalse(session.CharacterOpen); Assert.IsTrue(session.InventoryOpen); Assert.IsNull(session.VendorNpc);
            session.OpenVendor(merchant); Assert.IsFalse(session.TradesOpen);
            session.CloseVendor(); session.ShowTrades(true); session.ShowTrades(false); Assert.IsFalse(session.TradesOpen);

            // A load keeps the skill.
            session.Save(false); session.Load();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.AreNotSame(p, session.Progress, "The load read the save.");
            Assert.IsTrue(session.Professions.Has("mining")); Assert.AreEqual(1, session.Professions.Skill("mining"));
            Assert.AreEqual(4, Inventory.Count(session.Progress, "mat.copper_ore"));
        }
    }
}
#endif
