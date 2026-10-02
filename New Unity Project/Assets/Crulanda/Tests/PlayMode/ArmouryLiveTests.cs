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
    /// The Armoury in the running game (Oakhaven; loot DESIGN.md 9, step L3): a new character's Trailblade is found without a toast;
    /// a piece with a look not seen before raises "NEW LOOK" and counts on the Armoury tab; killing Caddock names his pieces, starts
    /// his epic's pity count, and both come back from a save and a load. Each test saves to its own folder (SaveDirectoryOverride),
    /// never the real one.
    /// </summary>
    public class ArmouryLiveTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-armoury-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = null;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            StringAssert.StartsWith(Path.GetTempPath(), s.SaveDirectoryOverride ?? "", "This test saves to its own folder.");
            Assert.NotNull(s.Loot, "The named loot is loaded."); Assert.NotNull(s.Armoury, "The Armoury is bound.");
            return s;
        }
        /// <summary>Lets any toast already up (a zone's own) run out.</summary>
        static IEnumerator ToastsClear(EncounterSession s)
        {
            float until = Time.time + 3 * EncounterSession.ToastSeconds + 1;
            while (s.ToastName != null && Time.time < until) yield return null;
            Assert.IsNull(s.ToastName, "The toast queue runs out.");
        }

        [UnityTest] public IEnumerator A_new_look_raises_a_toast_and_counts_on_the_tab()
        {
            var s = Session(); yield return null;
            Assert.IsTrue(s.Armoury.IsFound(s.content.itemId), "The Trailblade in hand is found from the start.");
            Assert.Contains(s.Armoury.LookOf(s.Items.Get(s.content.itemId)), s.Progress.looks, "Its look is seen."); Assert.AreEqual(0, s.ArmouryUnseen, "Quietly: nothing to see on the tab.");
            Assert.AreNotEqual("NEW LOOK", s.ToastKicker);
            yield return ToastsClear(s);
            int looks = s.Progress.looks.Count, unseen = s.ArmouryUnseen;
            string piece = ItemDatabase.GearId("legs", 3, 2, 41);
            Assert.AreEqual(0, Inventory.Add(s.Progress, s.Items, piece, 1));
            for (float t = 0; t < 2 && s.ToastKicker != "NEW LOOK"; t += Time.deltaTime) yield return null;
            Assert.AreEqual("NEW LOOK", s.ToastKicker); Assert.AreEqual(s.Items.Get(piece).name, s.ToastName, "The toast names the piece.");
            Assert.AreEqual(looks + 1, s.Progress.looks.Count); Assert.AreEqual(unseen + 1, s.ArmouryUnseen, "One more on the tab.");
            // The same piece again is nothing new.
            yield return new WaitForSeconds(EncounterSession.ToastSeconds + .2f);
            Inventory.Add(s.Progress, s.Items, piece, 1);
            yield return new WaitForSeconds(1.1f);
            Assert.IsNull(s.ToastName, "No toast the second time."); Assert.AreEqual(looks + 1, s.Progress.looks.Count); Assert.AreEqual(unseen + 1, s.ArmouryUnseen);
            // A named piece: an entry and its look.
            Inventory.Add(s.Progress, s.Items, "loot.oak.whitefoot_mantle", 1);
            yield return new WaitForSeconds(1.1f);
            Assert.IsTrue(s.Armoury.IsFound("loot.oak.whitefoot_mantle")); Assert.AreEqual("NEW LOOK", s.ToastKicker); Assert.AreEqual(unseen + 2, s.ArmouryUnseen);
            // The book's tab: this zone first, then by level, then the land at large; the count clears once the tab is seen.
            var tallies = s.ArmouryTallies();
            Assert.AreEqual("oakhaven", tallies[0].zone); Assert.IsTrue(tallies[0].here); Assert.AreEqual(ArmouryLog.World, tallies[tallies.Count - 1].zone);
            Assert.AreEqual(s.Loot.Gear.Count, tallies.Sum(t => t.Total));
            Assert.GreaterOrEqual(tallies[0].found, 2, "The Trailblade and the mantle at least.");
            s.SeenArmoury(); Assert.AreEqual(0, s.ArmouryUnseen);
        }

        [UnityTest] public IEnumerator Killing_Caddock_names_his_pieces_and_the_counts_survive_a_load()
        {
            var s = Session(); yield return null;
            var caddock = s.Enemies.FirstOrDefault(x => x.Camp && x.Elite && x.actor.IsAlive && LootContext.From(x.persistentId, s.Zone.Zone.camps, 1, true).mob == "Caddock, the Bandit King");
            Assert.NotNull(caddock, "Caddock is in Oakhaven.");
            foreach (var x in s.Enemies) if (x != caddock) x.enabled = false;
            caddock.RespawnSeconds = 600;
            Assert.AreEqual(ArmouryLog.State.Unknown, s.Armoury.StateOf("loot.oak.due_cleaver"));
            caddock.actor.Health.ApplyDamage(1000000);
            yield return null;
            Assert.IsFalse(caddock.actor.IsAlive);
            Assert.AreEqual(1, s.Armoury.KillsOf("drop.oak.caddock"), "The kill is counted.");
            Assert.AreEqual(ArmouryLog.State.Known, s.Armoury.StateOf("loot.oak.due_cleaver"), "His pieces are named.");
            Assert.AreEqual(ArmouryLog.State.Known, s.Armoury.StateOf("loot.oak.broken_oath_sabre"));
            bool sabre = caddock.Drops.Any(d => d.item == "loot.oak.broken_oath_sabre");
            var pity = s.Progress.lootLuck.FirstOrDefault(l => l.source == "drop.oak.caddock#1");
            Assert.NotNull(pity, "His epic's run is counted in the save's list."); Assert.AreEqual(1, pity.kills); Assert.AreEqual(sabre ? 0 : 1, pity.dry);
            yield return new WaitForSeconds(.2f);
            Assert.IsFalse(s.InCombat, "Out of combat, so the game saves.");
            s.Save(false);
            string file = Path.Combine(root, EncounterSave.SlotFor(s.ClassDef.id) + ".save.json");
            Assert.IsTrue(File.Exists(file), "Saved to the test's folder."); StringAssert.Contains("\"source\":\"drop.oak.caddock#1\"", File.ReadAllText(file));
            s.Progress.lootLuck.Clear();
            Assert.AreEqual(ArmouryLog.State.Unknown, s.Armoury.StateOf("loot.oak.due_cleaver"), "Forgotten in memory");
            s.Load(); yield return null;
            Assert.AreEqual(1, s.Armoury.KillsOf("drop.oak.caddock"), "and back from the save.");
            Assert.AreEqual(ArmouryLog.State.Known, s.Armoury.StateOf("loot.oak.due_cleaver"));
            Assert.AreEqual(1, s.Progress.lootLuck.First(l => l.source == "drop.oak.caddock#1").kills);
        }
    }
}
#endif
