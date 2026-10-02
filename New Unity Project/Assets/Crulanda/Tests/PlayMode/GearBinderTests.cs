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
    /// <summary>
    /// The gear binder in play (Oakhaven, a throwaway save): a new character holds the Tempered Trailblade (loot step L2);
    /// equipping another blade puts it in the hand and unequipping empties it; a load (F9) makes a new player figure and the binder dresses it in what was saved; a chest
    /// piece put on then shows on the body.
    /// (DESIGN.md lists this with the edit mode visual tests; it needs a running session, so it lives here.)
    /// </summary>
    public class GearBinderTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-gear-" + Guid.NewGuid().ToString("N"));
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
        /// <summary>Waits until the condition holds (or a few seconds pass), so the binder's next frame has come round.</summary>
        static IEnumerator Until(Func<bool> done, float seconds = 3) { for (float t = 0; t < seconds && !done(); t += Time.unscaledDeltaTime) yield return null; yield return null; }

        [UnityTest] public IEnumerator Binder_redresses_the_player_after_equip_unequip_and_load()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            yield return Until(() => s.Player != null && s.Player.GetComponent<ActorVisual>().GearDriven);
            Assert.NotNull(UnityEngine.Object.FindFirstObjectByType<GearBinder>(), "The binder is running.");
            Assert.NotNull(s.Items, "The game has its items.");
            StringAssert.StartsWith(Path.GetTempPath(), s.SaveDirectoryOverride ?? "", "A throwaway save.");
            var look = s.Player.GetComponent<ActorVisual>();
            Assert.IsTrue(look.GearDriven, "Gear drives the player's figure.");
            Assert.AreEqual(s.content.itemId, s.Progress.equipment[(int)EquipSlot.MainHand].item, "A new character starts with the Tempered Trailblade (loot step L2).");
            yield return Until(() => look.GearParts(EquipSlot.MainHand) > 0);
            Assert.Greater(look.GearParts(EquipSlot.MainHand), 0, "It is in the hand.");
            if (s.Druid == null) Assert.AreEqual(0, look.ClassKitParts, "No class sword, shield or pads: the hand holds the Trailblade's own look.");

            string blade = ItemDatabase.GearId("mainhand", 1, 1, 3);   // wearable at level 1
            Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, blade, 1));
            Assert.IsTrue(s.EquipFromBag(s.Progress.bag.FindIndex(b => b.item == blade)), "Equipped (and saved).");
            yield return Until(() => look.GearParts(EquipSlot.MainHand) > 0);
            Assert.Greater(look.GearParts(EquipSlot.MainHand), 0, "It is in the hand.");
            Assert.AreSame(look.RightArm, look.GearRoot(EquipSlot.MainHand).parent);

            Assert.IsTrue(Inventory.Unequip(s.Progress, (int)EquipSlot.MainHand), "Taken off, not saved.");
            yield return Until(() => look.GearParts(EquipSlot.MainHand) == 0);
            Assert.AreEqual(0, look.GearParts(EquipSlot.MainHand), "The hand is empty again.");

            s.Load();
            yield return Until(() => s.Player != null && s.Player.GetComponent<ActorVisual>().GearParts(EquipSlot.MainHand) > 0);
            var after = s.Player.GetComponent<ActorVisual>();
            Assert.AreNotSame(look, after, "The load made a new player figure.");
            Assert.AreEqual(blade, s.Progress.equipment[(int)EquipSlot.MainHand].item, "The save had the blade on.");
            Assert.Greater(after.GearParts(EquipSlot.MainHand), 0, "The binder dressed the new figure in it.");

            // Armour too: a chest piece shows on the body.
            string tunic = ItemDatabase.GearId("chest", 1, 1, 5);
            Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, tunic, 1));
            Assert.IsTrue(s.EquipFromBag(s.Progress.bag.FindIndex(b => b.item == tunic)), "A chest piece on.");
            yield return Until(() => after.GearParts(EquipSlot.Chest) > 0);
            Assert.Greater(after.GearParts(EquipSlot.Chest), 0, "The chest piece shows on the body.");
            Assert.AreEqual("Body", after.GearRoot(EquipSlot.Chest).parent.name);
        }
    }
}
#endif
