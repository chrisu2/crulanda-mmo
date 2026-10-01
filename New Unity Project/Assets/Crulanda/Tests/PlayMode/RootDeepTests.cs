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
    /// The Root-Mother's Deep, the Verdant Shore's dungeon under the Veridian Temple (GAME-ONLY; the Temple and the Keepers are CANON):
    /// - walkable from the player's start down the root-stair to the Heart, on the ground and the passage floor all the way;
    /// - earth and root overhead and underfoot the whole way (no land shows inside), and it runs 16 m down;
    /// - dark inside, lit from the Heart's sap-light; the Root-Warden stands in the Heart on the passage floor, with the deep's camps;
    /// - the cold in the root is a usable prop the finale quest sends you to.
    /// </summary>
    public class RootDeepTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-deep-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.verdant";
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
        static Hollow Deep { get { return Hollow.All.Find(h => h.Name == "The Root-Mother's Deep"); } }
        static Transform DeepRoot { get { return ZoneBuilder.Active.transform.Find("Zone props/The Root-Mother's Deep"); } }
        static bool OfTheDeep(Collider c) { return c != null && DeepRoot != null && c.transform.IsChildOf(DeepRoot); }

        [UnityTest] public IEnumerator The_deep_is_walkable_from_the_start_down_to_the_heart()
        {
            var h = Deep; Assert.IsNotNull(h, "The Verdant Shore has its deep."); Assert.IsNotNull(DeepRoot, "and it is built");
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.verdant", zone.Zone.id);
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player, .2f), out var a, 2.5f, NavMesh.AllAreas), "The start is on the navmesh.");
            var path = new NavMeshPath(); var from = a.position;
            for (float s = 3; s < h.Length - 7 + 6; s += 6)
            {
                var at = h.At(Mathf.Min(s, h.Length - 7)) + Vector3.up * .2f;
                Assert.IsTrue(NavMesh.SamplePosition(at, out var next, 2, NavMesh.AllAreas), "The floor " + s + " m in is on the navmesh (" + at + ").");
                Assert.IsTrue(NavMesh.CalculatePath(from, next.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Walkable from " + from + " to " + next.position + " (" + s + " m in).");
                from = next.position;
            }
            Assert.Less(h.Centre[h.Centre.Count - 1].y, h.Centre[0].y - 15, "The Heart lies fifteen metres and more below the stair's top.");
            Assert.Greater(h.Length, 90, "A dungeon, not a cellar.");
            yield return null;
        }

        [UnityTest] public IEnumerator Earth_and_root_all_the_way_and_no_land_inside()
        {
            var h = Deep;
            for (float s = 2; s < h.Length - 2; s += 1.3f)
            {
                var mid = h.At(s) + Vector3.up * 1.2f;
                var up = Physics.RaycastAll(mid, Vector3.up, 30).Where(x => x.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null).OrderBy(x => x.distance).FirstOrDefault();
                var down = Physics.RaycastAll(mid, Vector3.down, 30).Where(x => x.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null).OrderBy(x => x.distance).FirstOrDefault();
                Assert.IsTrue(up.collider != null && OfTheDeep(up.collider), "Overhead " + s + " m in: " + (up.collider != null ? up.collider.name : "sky"));
                Assert.IsTrue(down.collider != null && OfTheDeep(down.collider), "Underfoot " + s + " m in: " + (down.collider != null ? down.collider.name : "nothing"));
                Assert.AreEqual(h.At(s).y, down.point.y, .35f, "The floor is where the passage says, " + s + " m in.");
            }
            yield return null;
        }

        [UnityTest] public IEnumerator Dark_inside_the_warden_in_the_heart_and_the_cold_root_to_salt()
        {
            var h = Deep; var session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var motor = session.Player.GetComponent<AdventurerMotor>(); motor.enabled = false;
            var cam = Camera.main.transform;
            for (int f = 0; f < 4; f++) { cam.position = h.At(0) + (h.At(0) - h.At(3)).normalized * 8 + Vector3.up * 2; yield return null; }
            float outside = RenderSettings.ambientSkyColor.grayscale;
            for (int f = 0; f < 4; f++) { cam.position = h.At(h.Length - 8) + Vector3.up * 1.7f; yield return null; }
            Assert.Greater(Hollow.CameraDepth, .95f, "Deep in the Heart is deep.");
            Assert.Less(RenderSettings.ambientSkyColor.grayscale, outside * .35f, "The Heart is dark but for the sap-light.");
            motor.enabled = true;
            var inside = session.Enemies.FindAll(e => e != null && e.actor.IsAlive && h.Depth(e.transform.position + Vector3.up) > .3f);
            Assert.GreaterOrEqual(inside.Count, 10, "The stair, the Gallery, the Sap Well, the Cold Stair and the Heart are all held.");
            foreach (var e in inside)
            {
                int i = h.Nearest(new Vector2(e.transform.position.x, e.transform.position.z), out _);
                Assert.AreEqual(h.Centre[i].y, e.transform.position.y, 1.6f, e.actor.DisplayName + " stands on the passage floor");
            }
            var warden = session.Enemies.Find(e => e != null && e.persistentId != null && e.persistentId.StartsWith("mob.rootwarden."));
            Assert.IsNotNull(warden, "The Hollow Root-Warden keeps the Heart."); Assert.IsTrue(warden.Elite, "and is an elite");
            Assert.Greater(h.Depth(warden.transform.position + Vector3.up), .9f, "down in the Heart");
            var cold = session.Zone.Interactables.Find(i => i.name == "The cold in the root");
            Assert.NotNull(cold, "'The cold in the root' is a usable prop."); Assert.Greater(h.Depth(cold.position + Vector3.up), .9f, "in the Heart");
            var q = session.Quests.Def("main.verdant.5"); Assert.NotNull(q, "The finale quest exists.");
            Assert.IsTrue(q.steps.SelectMany(st => st.objectives).Any(o => o.type == "interact" && o.target == cold.name), "and sends you to salt it.");
        }
    }
}
#endif
