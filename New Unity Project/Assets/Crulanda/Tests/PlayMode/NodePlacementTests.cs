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
    /// Oakhaven's nodes (seams, windfalls, herbs) as ZoneBuilder builds them:
    /// - each can be walked to from the player's start, to within reach of E (EncounterSession.UseRange);
    /// - none stands in the water, in a building or on a road; each is 5 m from every secret and every other node and 2 m from
    ///   every standing trunk; the four on Crowsfoot Hollow's floor are in the cave (on its floor, not the hill over it) and no
    ///   other is;
    /// - nothing of theirs is solid or in the navmesh, and each has something to see (a seam's ore and a windfall's trunk as the
    ///   part that vanishes);
    /// - each is where it belongs: a windfall at the edge of a broadleaf wood, a herb in the open (in no wood and on no field), a
    ///   seam near a crag or rock or on the ridge's rocky crown.
    /// Stations join this test with the stations step (9); Oakhaven needs none of its own.
    /// </summary>
    public class NodePlacementTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-nodes-" + Guid.NewGuid().ToString("N"));
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
        static float Flat(Vector3 a, Vector3 b) { return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); }
        static float ToPath(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
        /// <summary>How far a point lies outside a rectangle (negative inside, by how far in from its nearest edge).</summary>
        static float OutsideRect(Vector2 p, Vector2 center, Vector2 size, float rotation = 0)
        {
            float r = -rotation * Mathf.Deg2Rad; var d = p - center; d = new Vector2(d.x * Mathf.Cos(r) - d.y * Mathf.Sin(r), d.x * Mathf.Sin(r) + d.y * Mathf.Cos(r));
            float ox = Mathf.Abs(d.x) - size.x / 2, oz = Mathf.Abs(d.y) - size.y / 2;
            return ox > 0 || oz > 0 ? new Vector2(Mathf.Max(0, ox), Mathf.Max(0, oz)).magnitude : Mathf.Max(ox, oz);
        }

        [UnityTest] public IEnumerator EveryNodeAndStation_IsReachable()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = s.Zone; var z = zone.Zone;
            Assert.NotNull(s.Professions, "The trades' content loaded (Encounter.asset lists it).");
            var nodes = zone.Interactables.Where(i => i.node != null).ToList();
            Assert.AreEqual(28, nodes.Count, "Ten ore, eight windfalls, ten herbs.");
            var problems = new List<string>();
            var container = zone.transform.Find("Zone nodes"); Assert.NotNull(container, "The nodes are built.");
            foreach (var c in container.GetComponentsInChildren<Collider>(true)) problems.Add("a node's '" + c.name + "' has a collider");
            if (container.GetComponentsInChildren<NavBlocker>(true).Length > 0 || container.GetComponentsInChildren<NavWalkable>(true).Length > 0) problems.Add("a node is in the navmesh");
            foreach (var i in nodes.Where(i => i.kind == "herb")) foreach (var c in i.root.GetComponentsInChildren<Collider>(true)) problems.Add("the herb '" + i.name + "' has a collider");
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(z.spawns.player), out var start, 2.5f, NavMesh.AllAreas), "The player's start is walkable.");
            var path = new NavMeshPath(); var keys = new HashSet<string>();
            int under = 0;
            foreach (var i in nodes)
            {
                var def = s.Professions.Db.Node(i.node);
                string q = "'" + i.name + "' at " + i.position + " ";
                if (def == null) { problems.Add(q + "is no known node"); continue; }
                if (!keys.Add(i.Key(z.id))) problems.Add(q + "shares its respawn key with another node");
                if (i.root == null || i.root.GetComponentsInChildren<Renderer>().Length == 0) problems.Add(q + "has nothing to see");
                if ((def.look == "ore" || def.look == "ore_rich" || def.look == "windfall") && (i.part == null || i.part.GetComponentsInChildren<Renderer>().Length == 0)) problems.Add(q + "has no part to vanish while it rests");
                // Walkable to: navmesh within reach of E, on a complete path from the start (round it too, as you'd walk up to it).
                float reach = EncounterSession.UseRange - .2f; bool near = false, walked = false;
                var tries = new List<Vector3> { i.position };
                for (int k = 0; k < 8; k++) foreach (float r in new[] { 1.2f, 2.2f })
                    tries.Add(zone.StandAt(new Vector2(i.position.x, i.position.z) + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * r, i.position.y, .2f));
                for (int k = 0; k < tries.Count && !walked; k++)
                {
                    if (!NavMesh.SamplePosition(tries[k], out var hit, k == 0 ? 2.5f : 1, NavMesh.AllAreas) || Flat(hit.position, i.position) > reach || Mathf.Abs(hit.position.y - i.position.y) > 2) continue;
                    near = true;
                    walked = NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
                }
                if (!near) problems.Add(q + "has no walkable ground within reach (" + reach + " m)");
                else if (!walked) problems.Add(q + "can't be walked to from the start");
                // Where it stands.
                var at = new Vector2(i.position.x, i.position.z);
                bool inCave = Hollow.InsideAny(i.position + Vector3.up * .3f, 0);
                if (inCave) { under++; float floor = 0; if (!Hollow.FloorUnder(at, ref floor) || Mathf.Abs(floor - i.position.y) > .5f) problems.Add(q + "is in the cave but not on its floor"); if (def.look != "ore_rich") problems.Add(q + "is in the cave but is no rich seam"); }
                else if (def.look == "ore_rich") problems.Add(q + "is a rich seam out of the cave");
                if (!inCave && zone.WaterAt(at, out _, out _)) problems.Add(q + "stands in the water");
                if (zone.InBuilding(at)) problems.Add(q + "stands in a building");
                foreach (var r in z.roads) if (r != null && r.points.Length > 1 && ToPath(at, r.points) < r.width / 2 + .5f && !inCave) problems.Add(q + "stands on " + r.name);
                foreach (var spot in s.SecretSpots) if (Vector3.Distance(spot.position, i.position) < 5) problems.Add(q + "is within 5 m of the secret '" + spot.def.id + "'");
                foreach (var o in nodes) if (o != i && Vector3.Distance(o.position, i.position) < 5) problems.Add(q + "is within 5 m of '" + o.name + "' at " + o.position);
                if (!inCave && !zone.TrunkClear(at, 2)) problems.Add(q + "is within 2 m of a tree trunk");
                // Where it belongs.
                if (def.look == "windfall" && !z.groves.Any(g => g != null && g.kind == "broadleaf" && OutsideRect(at, g.center, g.size) < 6 && OutsideRect(at, g.center, g.size) > -4))
                    problems.Add(q + "is no windfall at a broadleaf wood's edge");
                if (def.look == "herb")
                {
                    if (z.groves.Any(g => g != null && OutsideRect(at, g.center, g.size) < 0)) problems.Add(q + "is a herb in a wood, not on the meadow");
                    if (z.fields.Any(f => f != null && OutsideRect(at, f.center, f.size, f.rotation) < .5f)) problems.Add(q + "is a herb on a ploughed field");
                }
                if (def.look == "ore" && !z.props.Any(p => p != null && (p.kind == "cliff" || p.kind == "rock" || p.kind == "perch") && Vector2.Distance(p.at, at) < (p.kind == "cliff" ? (p.size.x > 0 ? p.size.x : 20) / 2 + 4 : 6))
                    && !z.shapes.Any(sh => sh != null && sh.height > 8 && Vector2.Distance(sh.center, at) < sh.radius))
                    problems.Add(q + "is a seam with no crag or rock by it, and not on a ridge's crown");
            }
            Assert.AreEqual(4, under, "The four rich seams are in Crowsfoot Hollow.");
            // The stations step (9) adds its stations here; Oakhaven's forge, oven, inn hearth and drying bench are workplaces.
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }
    }
}
#endif
