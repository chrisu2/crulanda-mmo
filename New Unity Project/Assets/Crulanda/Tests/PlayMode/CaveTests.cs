#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    /// Crowsfoot Hollow, Oakhaven's walk-in cave (GAME-ONLY):
    /// - walkable from the village green to the King's hall, through the mouth, not over the knoll;
    /// - walled and roofed all the way in, with nothing solid standing in the passage;
    /// - dark and dry inside, lit and rained on outside;
    /// - nothing grows in it, and the deserters hold it: the lookouts at the mouth, the rest inside, Caddock in the hall.
    /// </summary>
    public class CaveTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-cave-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (WorldWeather.Active != null) WorldWeather.Active.Release(true);
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static Hollow Crowsfoot { get { return Hollow.All.Find(h => h.Name == "Crowsfoot Hollow"); } }
        static Transform CaveRoot { get { return ZoneBuilder.Active.transform.Find("Zone props/Crowsfoot Hollow"); } }
        static bool OfTheCave(Collider c) { return c != null && CaveRoot != null && c.transform.IsChildOf(CaveRoot); }

        [UnityTest] public IEnumerator The_hollow_is_walkable_from_the_green_to_the_kings_hall()
        {
            var h = Crowsfoot; Assert.IsNotNull(h, "Oakhaven has its cave."); Assert.IsNotNull(CaveRoot, "and it is built");
            var zone = ZoneBuilder.Active;
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.recovery, .2f), out var a, 2, NavMesh.AllAreas));
            Assert.IsTrue(NavMesh.SamplePosition(h.At(h.Length - 7) + Vector3.up * .2f, out var b, 2, NavMesh.AllAreas), "The hall's floor is on the navmesh.");
            var path = new NavMeshPath(); NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "A way in from the green.");
            foreach (var corner in path.corners) Assert.Less(corner.y, zone.HeightAt(corner.x, corner.z) + 1.2f, "On the ground all the way (through the mouth, not over the knoll): " + corner);
            yield return null;
        }

        [UnityTest] public IEnumerator The_passage_is_walled_and_roofed_and_clear_down_the_middle()
        {
            var h = Crowsfoot;
            for (float s = 2; s < h.Length - 3; s += 1.5f)
            {
                var mid = h.At(s) + Vector3.up * 1.2f; int i = h.Nearest(new Vector2(mid.x, mid.z), out _);
                var side = h.At(s, 1) - h.At(s); side.y = 0; side.Normalize();
                // (Anyone standing in the way, a deserter by the fire, is looked past.)
                Assert.IsTrue(Physics.RaycastAll(mid, Vector3.up, 12).Any(x => OfTheCave(x.collider)), "A roof over " + s + " m in.");
                Assert.IsTrue(Physics.RaycastAll(mid, side, h.Half[i] * 1.4f + 1).Any(x => OfTheCave(x.collider)), "The right wall " + s + " m in.");
                Assert.IsTrue(Physics.RaycastAll(mid, -side, h.Half[i] * 1.4f + 1).Any(x => OfTheCave(x.collider)), "The left wall " + s + " m in.");
                // Nothing of the cave's stands in the way down the middle: a lump of the knoll poking in, a prop in the path.
                var next = h.At(s + 1.5f) + Vector3.up * 1.2f;
                foreach (var hit in Physics.RaycastAll(mid, next - mid, Vector3.Distance(mid, next)))
                    Assert.IsFalse(OfTheCave(hit.collider), "Rock in the passage between " + s + " and " + (s + 1.5f) + " m: " + hit.collider.name);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator Inside_is_dark_and_dry_and_outside_is_not()
        {
            var h = Crowsfoot; var weather = WorldWeather.Active;
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var motor = session.Player.GetComponent<AdventurerMotor>(); motor.enabled = false;
            var cam = Camera.main.transform;
            weather.Force(WeatherKind.Rain, true);
            var outward = h.At(0) - h.At(3); outward.y = 0; outward.Normalize();
            for (int f = 0; f < 4; f++) { cam.position = h.At(0) + outward * 8 + Vector3.up * 2; yield return null; }   // on the road below the mouth
            float outside = RenderSettings.ambientSkyColor.grayscale;
            Assert.AreEqual(0, Hollow.CameraDepth, 1e-3f, "Outside the mouth is outside.");
            var rain = weather.transform.Find("Rain").GetComponent<ParticleSystem>();
            Assert.Greater(rain.emission.rateOverTime.constant, 100, "It rains on the road.");
            for (int f = 0; f < 4; f++) { cam.position = h.At(h.Length - 7) + Vector3.up * 1.7f; yield return null; }   // deep in the hall
            Assert.Greater(Hollow.CameraDepth, .95f);
            Assert.Less(RenderSettings.ambientSkyColor.grayscale, outside * .35f, "The hall is dark but for its fires.");
            Assert.Less(RenderSettings.fogEndDistance, 40, "Smoky air closes in.");
            Assert.AreEqual(0, rain.emission.rateOverTime.constant, 1e-3f, "No rain in the hall.");
            motor.enabled = true;
        }

        [UnityTest] public IEnumerator Nothing_grows_in_the_hollow_and_the_deserters_hold_it()
        {
            var h = Crowsfoot;
            foreach (var tree in TreeFade.All)
                if (tree != null) Assert.AreEqual(0, h.Cover(new Vector2(tree.transform.position.x, tree.transform.position.z), 0), 1e-4f, "A tree in the hollow at " + tree.transform.position);
            var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var band = session.Enemies.FindAll(e => e != null && e.persistentId != null && (e.persistentId.StartsWith("mob.deserter.") || e.persistentId.StartsWith("mob.banditking.")));
            Assert.GreaterOrEqual(band.Count, 9, "Two lookouts, four in the camp, two guards and Caddock.");
            var king = band.Find(e => e.persistentId.StartsWith("mob.banditking."));
            Assert.IsNotNull(king, "Caddock is there."); Assert.IsTrue(king.Elite, "and elite");
            Assert.Greater(h.Depth(king.transform.position + Vector3.up), .9f, "Caddock holds the deep hall.");
            Assert.GreaterOrEqual(band.Count(e => h.Depth(e.transform.position + Vector3.up) > .5f), 7, "All but the lookouts are inside.");
            yield return null;
        }
    }
}
#endif
