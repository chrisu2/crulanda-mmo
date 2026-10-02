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
    /// - nothing grows in it, and the deserters hold it: the lookouts at the mouth, the rest inside, Caddock in the hall;
    /// - it lies 250 to 300 m from the green, inside the zone's edge, its mouth turned from the village and hidden by its hill
    ///   until the track's last bend (playtest note 1).
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
            var path = new NavMeshPath();
            // Leg by leg down the passage first, so a break says where it is: in at the mouth, then every 6 m to the hall.
            var outside = h.At(0) + (h.At(0) - h.At(3)).normalized * 6; outside.y = zone.HeightAt(outside.x, outside.z);
            Assert.IsTrue(NavMesh.SamplePosition(outside + Vector3.up * .2f, out var before, 2, NavMesh.AllAreas), "The road below the mouth is on the navmesh.");
            var from = before.position;
            for (float s = 3; s < h.Length - 7 + 6; s += 6)
            {
                var at = h.At(Mathf.Min(s, h.Length - 7)) + Vector3.up * .2f;
                Assert.IsTrue(NavMesh.SamplePosition(at, out var next, 2, NavMesh.AllAreas), "The floor " + s + " m in is on the navmesh (" + at + ").");
                Assert.IsTrue(NavMesh.CalculatePath(from, next.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Walkable from " + from + " to " + next.position + " (" + s + " m in).");
                from = next.position;
            }
            NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
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
        [UnityTest] public IEnumerator The_hollow_runs_deep_under_the_northern_hills()
        {
            var h = Crowsfoot; var zone = ZoneBuilder.Active;
            Assert.Less(h.Centre[h.Centre.Count - 1].y, h.Centre[0].y - 14, "The Echoing Hall lies fourteen metres and more below the mouth.");
            Assert.Greater(h.Length, 100, "A dungeon, not a hole in a hill.");
            // Past the Drop it runs deep under the northern hills: three metres of rock and more between its roof and the land.
            for (int i = 0; i < h.Centre.Count; i++)
                if (h.Along[i] > 45) Assert.Greater(h.Land[i] - h.Roof(i), 3, "Rock over the passage at " + h.Centre[i] + " (land " + h.Land[i] + ", roof " + h.Roof(i) + ").");
            // Where it passes under the zone's boundary (if it does), its roof is under the boundary wall's foot.
            for (int i = 0; i < h.Centre.Count; i++)
                if (Mathf.Abs(Mathf.Abs(h.Centre[i].z) - (zone.Half - 1)) < 1.2f || Mathf.Abs(Mathf.Abs(h.Centre[i].x) - (zone.Half - 1)) < 1.2f)
                    Assert.Less(h.Roof(i), -1, "Under the boundary at " + h.Centre[i]);
            yield return null;
        }
        [UnityTest] public IEnumerator The_hollow_lies_out_in_the_north_hills_with_its_back_to_the_village()
        {
            // Playtest note 1: the mouth stood 100 m from the green, 25 m past the Woodyard. It is 250 to 300 m out now, inside the
            // zone's edge all the way down, and turned from the village: from the green, from the North road's end and from the
            // first of the Crowsfoot track the hill stands between you and it, and from the country west of it two lower rises
            // do. You see it from the track's last bend.
            var h = Crowsfoot; var zone = ZoneBuilder.Active; var z = zone.Zone;
            var mouth = new Vector2(h.Centre[0].x, h.Centre[0].z); var green = z.spawns.recovery;
            Assert.That(Vector2.Distance(mouth, green), Is.InRange(250f, 300f), "The mouth, from the green.");
            var outward = h.At(0) - h.At(3); var facing = new Vector2(outward.x, outward.z).normalized;
            Assert.Less(Vector2.Dot(facing, (green - mouth).normalized), 0, "The mouth faces away from the village.");
            for (int i = 0; i < h.Centre.Count; i++)
                Assert.Less(Mathf.Max(Mathf.Abs(h.Centre[i].x), Mathf.Abs(h.Centre[i].z)) + h.Half[i] * 1.2f, zone.Half - 10, "The passage is inside the zone's edge at " + h.Centre[i]);
            var north = Array.Find(z.roads, r => r.name == "North road"); var track = Array.Find(z.roads, r => r.name == "Crowsfoot track");
            Assert.IsNotNull(north, "The North road."); Assert.IsNotNull(track, "The Crowsfoot track.");
            // How far the land stands over the straight line from a standing eye to a man's height in the mouth.
            float Hidden(Vector2 from)
            {
                float eye = zone.HeightAt(from.x, from.y) + 1.7f, mark = h.Centre[0].y + 1.5f, over = float.MinValue, d = Vector2.Distance(from, mouth);
                for (float s = 2; s < d - 2; s += 1) { var p = Vector2.Lerp(from, mouth, s / d); over = Mathf.Max(over, zone.HeightAt(p.x, p.y) - Mathf.Lerp(eye, mark, s / d)); }
                return over;
            }
            Assert.Greater(Hidden(green), 3, "From the green the hill hides the mouth.");
            Assert.Greater(Hidden(north.points[north.points.Length - 1]), 3, "From the North road's end the hill hides the mouth.");
            Assert.Greater(Hidden(track.points[1]), 3, "From the first of the track the hill hides the mouth.");
            // From the west and south-west two lower rises do it (the ridge's west spur and heel rise): the boar wood, the fold, the road.
            Assert.Greater(Hidden(new Vector2(-130, 170)), 1.5f, "From the Mastwood the heel hides the mouth.");
            Assert.Greater(Hidden(new Vector2(-132, 168)), 1.5f, "From the Mastwood boars' ground the heel hides the mouth.");
            Assert.Greater(Hidden(new Vector2(-156, 64)), 1.5f, "From the Old Fold the spur hides the mouth.");
            Assert.Greater(Hidden(new Vector2(-150, 16)), 1.5f, "From the West road the spur hides the mouth.");
            Assert.Less(Hidden(track.points[track.points.Length - 3]), 1, "From the track's last bend you see it.");
            yield return null;
        }

        [UnityTest] public IEnumerator No_land_shows_inside_the_passage()
        {
            var h = Crowsfoot;
            for (float s = 1; s < h.Length - 2; s += 1.1f)
            {
                var mid = h.At(s) + Vector3.up * 1.2f;
                // The nearest thing overhead and underfoot is the cave's own rock and floor, never the zone's ground mesh.
                var up = Physics.RaycastAll(mid, Vector3.up, 30).Where(x => x.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null).OrderBy(x => x.distance).FirstOrDefault();
                var down = Physics.RaycastAll(mid, Vector3.down, 30).Where(x => x.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null).OrderBy(x => x.distance).FirstOrDefault();
                Assert.IsTrue(up.collider != null && OfTheCave(up.collider), "Overhead " + s + " m in: " + (up.collider != null ? up.collider.name : "sky"));
                Assert.IsTrue(down.collider != null && OfTheCave(down.collider), "Underfoot " + s + " m in: " + (down.collider != null ? down.collider.name : "nothing"));
                Assert.AreEqual(h.At(s).y, down.point.y, .35f, "The floor is where the passage says, " + s + " m in.");
            }
            yield return null;
        }

        [UnityTest] public IEnumerator You_can_stand_on_every_floor_down_to_the_hall()
        {
            // Stood on the floor at each stage of the way down (the Drop, the Store Caves, the Deep Stair, the hall), you stay there:
            // no falling through, and the fall-out-of-the-world catch leaves a deep cave floor alone.
            var h = Crowsfoot; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            foreach (var e in session.Enemies) if (e != null) e.enabled = false;   // the band holds still
            var motor = session.Player.GetComponent<AdventurerMotor>();
            foreach (float f in new[] { .3f, .5f, .65f, .8f, 1 })
            {
                var at = h.At(Mathf.Min(h.Length * f, h.Length - 5)) + Vector3.up * 1.1f;
                motor.Teleport(at);
                for (float t = 0; t < 1; t += Time.deltaTime) yield return null;
                var p = session.Player.transform.position;
                Assert.Less(Vector3.Distance(p, at), 2, "Still standing " + Mathf.RoundToInt(h.Length * f) + " m in, " + (at.y - h.Centre[0].y).ToString("0.0") + " m down (now at " + p + ").");
            }
            foreach (var e in session.Enemies) if (e != null) e.enabled = true;
        }

        [UnityTest] public IEnumerator Every_camp_in_the_hollow_stands_on_its_floor()
        {
            var h = Crowsfoot; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var inside = session.Enemies.FindAll(e => e != null && e.actor.IsAlive && h.Depth(e.transform.position + Vector3.up) > .3f);
            Assert.GreaterOrEqual(inside.Count, 14, "The camp, the Drop, the Store Caves, the Deep Stair and the Hall are all manned.");
            foreach (var e in inside)
            {
                int i = h.Nearest(new Vector2(e.transform.position.x, e.transform.position.z), out _);
                Assert.AreEqual(h.Centre[i].y, e.transform.position.y, 1.6f, e.actor.DisplayName + " stands on the passage floor");
            }
            Assert.IsTrue(session.Enemies.Exists(e => e != null && e.persistentId != null && e.persistentId.StartsWith("mob.quartermaster.")), "Quartermaster Hesk keeps the Store Caves.");
            yield return null;
        }
    }
}
#endif
