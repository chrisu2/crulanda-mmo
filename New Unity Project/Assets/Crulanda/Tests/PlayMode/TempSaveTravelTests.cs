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
    /// A throwaway-save run (--crulanda-temp-save) keeps its character across zone travel. Travel reloads the scene, and the
    /// new zone's session must find the folder the last one wrote to; a fresh folder per zone silently threw the character
    /// away (Chris lost his talents, level and gear arriving in the Ashland Rim).
    /// </summary>
    public class TempSaveTravelTests
    {
        [UnitySetUp] public IEnumerator Setup()
        {
            EncounterSession.ResetTempSave(); EncounterSession.UseTempSave = true;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            string root = EncounterSession.TempSaveRoot;
            EncounterSession.ResetTempSave(); ZoneBuilder.RequestedZoneId = null;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        [UnityTest] public IEnumerator Talents_level_and_gear_survive_travel_on_a_temp_save()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.IsNotNull(EncounterSession.TempSaveRoot, "The temp-save run made a throwaway folder.");
            string folder = EncounterSession.TempSaveRoot;
            // Earn a level, spend the point, pick up some gold: things that must all come along.
            s.Progress.experience = EncounterProgress.XpForLevel(3); s.Progress.gold = 77;
            var first = s.Talents.Branches.SelectMany(b => b.nodes).First();   // every playable node is a level-1 node in this tree
            Assert.IsTrue(s.ChangeTalent(first.id, 1), "One talent point spent.");
            Assert.AreEqual(1, s.Progress.talents.Count);
            // Take the west road (the same call E makes).
            s.Player.GetComponent<AdventurerMotor>().Teleport(s.Zone.Ground(s.Zone.Zone.exits[0].at + new Vector2(1, 0), 1.1f));
            var exit = s.NearbyExit; Assert.NotNull(exit);
            Assert.IsTrue(s.TravelTo(exit));
            for (int i = 0; i < 25; i++) yield return null;
            var t = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.AreEqual(exit.to, t.Zone.Zone.id, "Arrived in the linked zone.");
            Assert.AreEqual(folder, EncounterSession.TempSaveRoot, "The new zone uses the same throwaway folder.");
            Assert.AreEqual(3, t.Progress.Level, "Level came along.");
            Assert.AreEqual(77, t.Progress.gold, "Gold came along.");
            Assert.AreEqual(1, t.Progress.talents.Count, "The talent point came along.");
            Assert.AreEqual(first.id, t.Progress.talents[0].id);
        }
    }
}
#endif
