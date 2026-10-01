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
    /// Buildings on the land and the two hero buildings (the visual review's items 10 and 11):
    /// - in every zone each house, inn, barn, mill and fallen house stands on a stone footing that reaches below the ground all
    ///   round it (no wall hangs over a slope), and a door's threshold is never under the grass;
    /// - in Oakhaven the rebuilt smithy can still be worked at both its places, and the inn can still be walked into past its
    ///   new porch, its door open and its lantern lit after dark.
    /// </summary>
    public class BuildingGroundTests
    {
        static readonly string[] Kinds = { "house", "inn", "barn", "mill", "ruined_house" };
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-buildings-" + Guid.NewGuid().ToString("N"));
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
        /// <summary>The built prop of that name nearest where it was placed (a fallen house may have been moved clear of a building).</summary>
        static Transform Built(ZoneBuilder zone, ZoneProp p)
        {
            string name = string.IsNullOrEmpty(p.name) ? p.kind : p.name; Transform best = null; float near = 6;
            foreach (var group in new[] { "Zone props", "Zone static scenery" })
            {
                var under = zone.transform.Find(group); if (under == null) continue;
                foreach (Transform c in under)
                {
                    if (c.name != name) continue;
                    float d = Vector2.Distance(new Vector2(c.position.x, c.position.z), p.at); if (d < near) { near = d; best = c; }
                }
            }
            return best;
        }
        static Vector2 Size(ZoneProp p) { return p.size.x > 0 ? p.size : p.kind == "house" ? new Vector2(7, 5) : p.kind == "mill" ? new Vector2(7, 6) : p.kind == "ruined_house" ? new Vector2(8, 6) : new Vector2(12, 8); }

        [UnityTest] public IEnumerator Every_building_stands_on_stone_down_to_the_ground()
        {
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var zones = first.Zone.AllZones().Select(z => z.id).ToList(); var problems = new List<string>(); int seen = 0;
            foreach (var id in zones)
            {
                ZoneBuilder.RequestedZoneId = id;
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                for (int i = 0; i < 3; i++) yield return null;
                var zone = ZoneBuilder.Active; Assert.AreEqual(id, zone.Zone.id);
                foreach (var p in zone.Zone.props)
                {
                    if (p == null || Array.IndexOf(Kinds, p.kind) < 0) continue;
                    var t = Built(zone, p); string q = zone.Zone.id + ": " + p.kind + " '" + p.name + "': ";
                    if (t == null) { if (p.kind != "ruined_house") problems.Add(q + "not built"); continue; }   // a plain fallen house standing in a building is left out
                    var footing = t.Find("Footing"); if (footing == null) { problems.Add(q + "has no footing"); continue; }
                    seen++;
                    // The footing's foot against the lowest ground just outside its walls, round all four sides.
                    var size = Size(p); float out2 = p.kind == "inn" ? .35f : .25f, bottom = footing.GetComponent<Renderer>().bounds.min.y, low = float.MaxValue;
                    for (int sx = -1; sx <= 1; sx++) for (int sz = -1; sz <= 1; sz++)
                    {
                        if (sx == 0 && sz == 0) continue;
                        var w = t.TransformPoint(new Vector3(sx * (size.x / 2 + out2), 0, sz * (size.y / 2 + out2)));
                        low = Mathf.Min(low, zone.Ground(new Vector2(w.x, w.z)).y);
                    }
                    if (bottom > low - .1f) problems.Add(q + "its footing stops " + (bottom - low).ToString("0.00") + " m above the lowest ground round it");
                    // A door's threshold stone stands at the ground across the doorway, not under it (houses and barns: the inn's floor is level).
                    if (p.kind == "house" || p.kind == "barn" || p.kind == "mill")
                    {
                        var steps = t.Find("Door steps"); if (steps == null) { problems.Add(q + "has no threshold"); continue; }
                        var at = t.TransformPoint(new Vector3(0, 0, -size.y / 2 - .3f)); float ground = zone.Ground(new Vector2(at.x, at.z)).y, sill = steps.GetComponent<Renderer>().bounds.max.y;
                        if (sill < ground - .03f && ground - t.position.y < .6f) problems.Add(q + "its threshold is " + (ground - sill).ToString("0.00") + " m under the ground at the door");
                    }
                }
            }
            Assert.Greater(seen, 30, "Every zone's buildings were found.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [UnityTest] public IEnumerator The_smithy_and_the_inn_still_work_after_their_rebuild()
        {
            var zone = ZoneBuilder.Active; var path = new NavMeshPath(); var problems = new List<string>();
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player), out var start, 2.5f, NavMesh.AllAreas), "The player's start is walkable.");
            // The smith's two places, at the anvil and the hearth: on the navmesh where the builder put them, and reachable.
            var stands = zone.Workplaces.Where(w => w.kind == "forge").ToList();
            Assert.AreEqual(2, stands.Count, "The smithy has its two places.");
            foreach (var w in stands)
            {
                string q = w.name + " at " + w.stand + ": ";
                if (!NavMesh.SamplePosition(w.stand, out var hit, 1.5f, NavMesh.AllAreas)) { problems.Add(q + "no navmesh within 1.5 m"); continue; }
                if (Vector2.Distance(new Vector2(hit.position.x, hit.position.z), new Vector2(w.stand.x, w.stand.z)) > .75f) problems.Add(q + "the nearest navmesh is too far off");
                if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) problems.Add(q + "can't be walked to from the start");
            }
            // The inn: in past the porch posts (solid, each cutting its own navmesh hole), the door open, the lantern lit after dark.
            var innProp = zone.Zone.props.First(p => p != null && p.kind == "inn"); var inn = Built(zone, innProp);
            Assert.IsNotNull(inn, "The Golden Cask is built.");
            float d = innProp.size.y > 0 ? innProp.size.y : 8;
            foreach (var (local, what) in new[] { (new Vector3(0, 0, -d / 2 - 1.3f), "under the porch"), (Vector3.zero, "in the taproom") })
            {
                var at = inn.TransformPoint(local); at.y = zone.Ground(new Vector2(at.x, at.z)).y;
                if (!NavMesh.SamplePosition(at, out var hit, .6f, NavMesh.AllAreas) || Vector2.Distance(new Vector2(hit.position.x, hit.position.z), new Vector2(at.x, at.z)) > .4f) { problems.Add("The inn: no navmesh " + what); continue; }
                if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) problems.Add("The inn: can't walk " + what + " from the start");
            }
            Assert.IsTrue(zone.Doors.Any(x => x.name == innProp.name && x.openable && x.Open), "The inn keeps its door open.");
            Assert.AreEqual(2, inn.GetComponentsInChildren<BoxCollider>().Count(c => c.GetComponent<MeshFilter>() != null && c.GetComponent<NavBlocker>() != null && c.transform.localPosition.z < -d / 2 - 1), "The porch's two posts are solid.");
            var lantern = zone.NightLights.Find(n => n.light != null && n.light.name == "Inn lantern" && n.light.transform.IsChildOf(inn));
            Assert.IsNotNull(lantern, "The inn's lantern is one of the zone's night lights.");
            Assert.Greater(lantern.nightIntensity, lantern.dayIntensity, "It burns brighter after dark.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }
    }
}
#endif
