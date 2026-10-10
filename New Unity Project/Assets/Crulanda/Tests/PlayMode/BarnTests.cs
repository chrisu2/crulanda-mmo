#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// The walk-in barns (2026-10-10, Chris: "barn door should be open enough to walk into, for farm animals to sleep"): the doorway stands
    /// open with its sliding doors slid aside and every stall can be walked to from in front of it; at night the farm beasts are in their
    /// stalls and by day back on their pastures; one walks in through the doorway at dusk and back out in the morning. Then ("this barn is
    /// backwards", "missing straw", "sheep dont have a place to sleep"): the doors are in the gable end, the stalls down both sides of an
    /// aisle, and the Old Fold's flock has the Fold barn. And the windmill turns with the wind ("should be animated when wind blowing").
    /// </summary>
    public class BarnTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(float hour)
        {
            WorldClock.Hour = hour;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-barn-" + Guid.NewGuid().ToString("N"));
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
        static IEnumerator WaitUntil(Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }
        static List<Critter> Beasts() { return UnityEngine.Object.FindObjectsByType<Critter>(FindObjectsSortMode.None).Where(c => c.Barn != null).OrderBy(c => c.name).ThenBy(c => c.transform.position.x).ToList(); }
        static Vector2 Flat(Vector3 p) { return new Vector2(p.x, p.z); }
        static bool InStall(Critter c) { return c.Barn.stalls.Any(s => Vector2.Distance(Flat(s), Flat(c.transform.position)) < .8f); }   // a stall's middle, or a side of it for a sheep

        [UnityTest] public IEnumerator A_barn_is_walked_into_through_its_open_doors()
        {
            yield return Load(10);
            var zone = ZoneBuilder.Active; Assert.Greater(zone.Barns.Count, 0, "Oakhaven has walk-in barns");
            Assert.AreEqual(2 * zone.Barns.Count, UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.StartsWith("Barn door ")), "two sliding doors to a barn");
            var path = new NavMeshPath(); var problems = new List<string>();
            foreach (var b in zone.Barns)
            {
                Assert.Greater(b.stalls.Length, 3, b.name + " has stalls");
                if (!NavMesh.SamplePosition(b.door, out var front, 2.5f, NavMesh.AllAreas)) { problems.Add(b.name + ": no ground in front of its doorway"); continue; }
                foreach (var s in b.stalls)
                    if (!NavMesh.SamplePosition(s, out var st, 1.2f, NavMesh.AllAreas) || !NavMesh.CalculatePath(front.position, st.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                        problems.Add(b.name + ": the stall at " + s + " cannot be walked to from the doorway");
                // Nothing solid across the doorway at chest height, between the ground in front and the floor just inside.
                var from = zone.Ground(Flat(b.door), 1.2f); var to = zone.Ground(Flat(b.inside), 1.2f);
                if (Physics.Linecast(from, to, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) problems.Add(b.name + ": the doorway is blocked by " + hit.collider.name);
                if (!zone.InBuilding(Flat(b.inside))) problems.Add(b.name + ": the point inside is not inside it");
                // The doorway is in the gable end: the ridge runs in from it, and the stalls face out to either side of the aisle.
                var axis = b.inside - b.door; axis.y = 0; axis.Normalize();
                var roof = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Barn roof" && t.parent != null && t.parent.name == b.name);
                if (roof == null || Mathf.Abs(Vector3.Dot(roof.right, axis)) < .99f) problems.Add(b.name + ": its ridge does not run in from the doorway");
                if (b.faces.Length != b.stalls.Length || b.faces.Any(f => Mathf.Abs(Vector3.Dot(f.normalized, axis)) > .05f)) problems.Add(b.name + ": its stalls do not face the side walls");
                if (!b.faces.Any(f => Vector3.Cross(axis, f).y > 0) || !b.faces.Any(f => Vector3.Cross(axis, f).y < 0)) problems.Add(b.name + ": stalls only on one side of the aisle");
                if (b.aisle.Length != b.stalls.Length || b.aisle.Any(a => !zone.InBuilding(Flat(a)))) problems.Add(b.name + ": an aisle point outside it");
                foreach (var s in b.stalls) if (!zone.InBuilding(Flat(s))) problems.Add(b.name + ": the stall at " + s + " is outside its footprint (the grass grows there)");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            var fold = UnityEngine.Object.FindObjectsByType<Critter>(FindObjectsSortMode.None).Where(c => c.Kind == "sheep" && Vector2.Distance(Flat(c.transform.position), new Vector2(-156, 64)) < 25).ToList();
            Assert.Greater(fold.Count, 0, "the Old Fold's flock");
            foreach (var c in fold) { Assert.NotNull(c.Barn, c.name + " at the Old Fold has a barn"); Assert.AreEqual("Fold barn", c.Barn.name); }
        }

        [UnityTest] public IEnumerator A_windmill_turns_with_the_wind()
        {
            yield return Load(10);
            var sails = UnityEngine.Object.FindObjectsByType<ZoneBuilder.Sails>(FindObjectsSortMode.None);
            Assert.Greater(sails.Length, 0, "Oakhaven has a windmill");
            foreach (var s in sails) Assert.IsFalse(s.GetComponent<MeshRenderer>().isPartOfStaticBatch, "its sails are not frozen in a static batch");
            var weather = WorldWeather.Active; Assert.NotNull(weather);
            weather.Force(WeatherKind.Clear, true); Time.timeScale = 4;
            yield return WaitUntil(() => Mathf.Abs(sails[0].degreesPerSecond - ZoneBuilder.Sails.SpeedFor(WorldWeather.Now.wind)) < 3, 10);
            float calm = sails[0].degreesPerSecond; var was = sails[0].transform.rotation;
            yield return new WaitForSeconds(1);
            Assert.Greater(Quaternion.Angle(was, sails[0].transform.rotation), 1, "the sails turn");
            weather.Force(WeatherKind.Windy, true);
            yield return WaitUntil(() => sails[0].degreesPerSecond > calm * 3, 12);
            Assert.Greater(sails[0].degreesPerSecond, calm * 3, "faster in a wind (" + calm + " calm)");
        }

        [UnityTest] public IEnumerator At_night_the_farm_beasts_sleep_in_their_barns_and_by_day_they_are_out()
        {
            yield return Load(23);
            var zone = ZoneBuilder.Active; var beasts = Beasts();
            Assert.Greater(beasts.Count, 0, "some farm beasts have a barn near their pasture");
            // Stand far from every barn and beast: out of sight, each is simply where it should be.
            var points = zone.Barns.Select(b => Flat(b.door)).Concat(beasts.Select(c => Flat(c.transform.position))).ToList();
            Vector2 far = Vector2.zero; float farD = -1;
            for (float x = -zone.Half + 20; x <= zone.Half - 20; x += 20) for (float z = -zone.Half + 20; z <= zone.Half - 20; z += 20)
            {
                var p = new Vector2(x, z); if (zone.WaterAt(p, out _, out _)) continue;
                float d = points.Min(q => Vector2.Distance(p, q)); if (d > farD) { farD = d; far = p; }
            }
            Assert.Greater(farD, 95, "somewhere out of sight of them all");
            UnityEngine.Object.FindFirstObjectByType<EncounterSession>().Player.GetComponent<AdventurerMotor>().Teleport(zone.Ground(far, 1.1f));
            for (int i = 0; i < 3; i++) yield return null;
            foreach (var c in beasts) { Assert.IsTrue(c.Asleep, c.name + " is asleep"); Assert.IsTrue(InStall(c), c.name + " is in a stall"); Assert.IsTrue(zone.InBuilding(Flat(c.transform.position)), c.name + " is inside"); }
            foreach (var b in zone.Barns) Assert.LessOrEqual(b.Sleepers, 2 * b.stalls.Length, b.name + ": a beast a stall, or two sheep");
            WorldClock.Hour = 9; for (int i = 0; i < 3; i++) yield return null;
            foreach (var c in beasts) { Assert.IsFalse(c.Asleep, c.name + " is up"); Assert.IsFalse(zone.InBuilding(Flat(c.transform.position)), c.name + " is out on its pasture"); }
            foreach (var b in zone.Barns) Assert.AreEqual(0, b.Sleepers, b.name + " is empty by day");
        }

        [UnityTest] public IEnumerator A_beast_walks_in_through_the_doorway_at_dusk_and_out_in_the_morning()
        {
            yield return Load(10);
            var zone = ZoneBuilder.Active; var c = Beasts().FirstOrDefault(); Assert.NotNull(c, "a farm beast with a barn");
            var b = c.Barn; var outward = b.door - b.inside; outward.y = 0; outward.Normalize();
            // You stand off to one side of the barn's front, in sight; the beast stands just in front of its doorway.
            var side = Vector3.Cross(Vector3.up, outward);
            UnityEngine.Object.FindFirstObjectByType<EncounterSession>().Player.GetComponent<AdventurerMotor>().Teleport(zone.Ground(Flat(b.door + side * 14 + outward * 4), 1.1f));
            c.transform.position = zone.Ground(Flat(b.door + outward * 3));
            Time.timeScale = 3; WorldClock.Hour = 22;
            yield return WaitUntil(() => c.Asleep, 40);
            Assert.IsTrue(c.Asleep, c.name + " walked in and lay down");
            Assert.IsTrue(InStall(c), "in its stall"); Assert.IsTrue(zone.InBuilding(Flat(c.transform.position)), "inside the barn");
            WorldClock.Hour = 7;
            yield return WaitUntil(() => !c.InBarnRoutine, 40);
            Assert.IsFalse(c.InBarnRoutine, c.name + " walked out");
            Assert.IsFalse(zone.InBuilding(Flat(c.transform.position)), "and is out of the barn");
            Assert.IsFalse(b.Holds(c), "its stall is free (others may still be on their way out)");
        }
    }
}
#endif
