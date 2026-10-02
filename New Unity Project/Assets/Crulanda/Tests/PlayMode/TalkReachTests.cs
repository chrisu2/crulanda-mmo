#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Talking is kept by walls, as working at a station is (EncounterSession.InTalkReach): Mira, waiting unrecruited in the Golden
    /// Cask's taproom beside the hearth, is not offered to someone standing outside the inn's end wall, though she is within talk
    /// range there, not even when she is selected; from inside the taproom E reads "Recruit Mira". In Oakhaven by day, saving to
    /// its own folder.
    /// </summary>
    public class TalkReachTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(float hour)
        {
            WorldClock.Hour = hour; ZoneBuilder.RequestedZoneId = null;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-talk-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; WorldClock.Hour = 8.5f;
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static EncounterSession Session() { return UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); }
        /// <summary>Puts the player at a spot (ground, or the floor under it) with no villager near enough to talk to.</summary>
        static IEnumerator StandAt(EncounterSession s, Vector3 ground)
        {
            s.Player.GetComponent<AdventurerMotor>().Teleport(ground + Vector3.up * 1.1f);
            if (VillageLife.Active != null) foreach (var v in VillageLife.Active.Villagers) if (Vector2.Distance(new Vector2(v.transform.position.x, v.transform.position.z), new Vector2(ground.x, ground.z)) < 10) v.Park();
            s.SelectFriendly(null, false);
            for (int i = 0; i < 3; i++) yield return null;
        }
        static bool OffersMira(EncounterSession s) { return s.InteractPrompt != null && s.InteractPrompt.Contains("Mira"); }

        [UnityTest] public IEnumerator Mira_NotThroughTheGoldenCasksEndWall_FromTheTaproomYes()
        {
            yield return Open(14);
            var s = Session(); Assert.AreEqual(root, s.SaveDirectoryOverride, "This test saves to its own folder.");
            Assert.IsFalse(s.Progress.recruited, "A new game: Mira waits in the inn to be recruited.");
            var inn = s.Zone.transform.Find("Zone props/The Golden Cask"); Assert.NotNull(inn, "The Golden Cask is built.");
            var light = inn.Find("Hearth fire"); Assert.NotNull(light);
            float half = light.localPosition.x + 1.6f;   // the hearth's end wall (its fire burns 1.6 m in from it)
            var mira = s.Companion.transform; var at = inn.InverseTransformPoint(mira.position);
            Assert.Less(at.x, half, "Mira waits inside the end wall."); Assert.Greater(at.x, half - EncounterSession.TalkRange, "Within talk range of the wall's far side.");

            // Outside the end wall, level with her and within talk range of her by distance alone: the wall is between, so no Mira.
            bool found = false;
            foreach (float dz in new[] { 0, -.5f, .5f, -1, 1 })
                foreach (float lx in new[] { half + .7f, half + 1, half + 1.3f })
                {
                    if (found || !NavMesh.SamplePosition(inn.TransformPoint(new Vector3(lx, 0, at.z + dz)), out var hit, 1, NavMesh.AllAreas) || inn.InverseTransformPoint(hit.position).x < half + .4f) continue;
                    yield return StandAt(s, hit.position);
                    found = Vector3.Distance(s.Player.transform.position, mira.position) < EncounterSession.TalkRange - .15f;
                }
            Assert.IsTrue(found, "Walkable ground outside the Golden Cask's end wall, within talk range of Mira by distance alone.");
            Assert.IsFalse(s.CompanionInReach, "Mira is not talked to through the inn's end wall.");
            Assert.IsFalse(OffersMira(s), "E offers nothing of Mira through the wall: " + s.InteractPrompt);
            s.SelectFriendly(null, true); yield return null;
            Assert.IsFalse(s.CompanionInReach, "Not even with her selected.");
            Assert.IsFalse(OffersMira(s), "Selecting her does not reach through the wall either: " + s.InteractPrompt);
            Assert.IsFalse(s.FocusInTalkReach, "The friend frame does not offer [E] talk through the wall.");

            // In the taproom, a little way in front of the hearth (about 1.8 m from her): E asks her to join.
            var spot = inn.TransformPoint(new Vector3(3.2f, 0, .8f)); spot.y = inn.position.y;
            yield return StandAt(s, spot);
            Assert.Less(Vector3.Distance(s.Player.transform.position, mira.position), EncounterSession.TalkRange, "Near her in the taproom.");
            Assert.IsTrue(s.CompanionInReach, "Mira is talked to from inside the taproom.");
            Assert.AreEqual("Recruit Mira", s.InteractPrompt);
            s.SelectFriendly(null, true); yield return null;
            Assert.IsTrue(s.FocusInTalkReach, "Selected in the taproom, the friend frame offers [E] talk.");
        }
    }
}
#endif
