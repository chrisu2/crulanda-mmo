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
    /// Every zone's nodes (seams, windfalls, herbs) as ZoneBuilder builds them, ten ore, eight windfalls and ten herbs in each:
    /// - each can be walked to from the player's start, to within reach of E (EncounterSession.UseRange);
    /// - none stands in the water, in a building or on a road; each is 5 m from every secret and every other node and 2 m from
    ///   every standing trunk; those the data puts under (Crowsfoot Hollow's four, the Root-Mother's Deep's four) are in the cave
    ///   (on its floor, not the hill over it) and no other is; none reaches (ZoneBuilder.NodeFootprint) into a camp's spread or a
    ///   cave's furnishings (KeepClearSpots);
    /// - nothing of theirs is solid or in the navmesh, and each has something to see (a seam's ore and a windfall's trunk as the
    ///   part that vanishes);
    /// - each is where it belongs: a windfall at the edge of a wood (an oak windfall at a broadleaf wood's), a herb from the
    ///   nodes array in the open (in no wood and on no field; a herb prop worked as a node stands where the zone always had it,
    ///   and on no field), a seam near a crag or rock or on a ridge's rocky crown.
    /// And every zone's stations (step 9; CheckStations): the kinds it should have, each walkable to within reach; its own (Oakhaven
    /// has none) built where the data puts them, clear of water, buildings, roads, nodes, secrets and trunks, with no colliders.
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

        [UnityTest, Timeout(600000)] public IEnumerator EveryNodeAndStation_IsReachable()
        {
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var zones = first.Zone.AllZones().Select(z => z.id).ToList();
            Assert.AreEqual(5, zones.Count, "Oakhaven, Khaven, the Peaks, the Ashland Rim and the Verdant Shore are registered.");
            var problems = new List<string>();
            foreach (var id in zones)
            {
                ZoneBuilder.RequestedZoneId = id;
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                for (int f = 0; f < 4; f++) yield return null;
                var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
                if (s.Zone.Zone.id != id) { problems.Add(id + ": built " + s.Zone.Zone.id + " instead"); continue; }
                Check(s, problems);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
        /// <summary>The nodes of the zone in play, each problem named with the zone.</summary>
        static void Check(EncounterSession s, List<string> problems)
        {
            var zone = s.Zone; var z = zone.Zone; string zp = z.displayName + ": ";
            Assert.NotNull(s.Professions, "The trades' content loaded (Encounter.asset lists it).");
            var nodes = zone.Interactables.Where(i => i.node != null).ToList();
            if (nodes.Count != 28) problems.Add(zp + nodes.Count + " nodes, not ten ore, eight windfalls and ten herbs");
            var container = zone.transform.Find("Zone nodes");
            if (container == null) { problems.Add(zp + "no nodes are built"); return; }
            if (z.id == "zone.oakhaven" && zone.KeepClearSpots.Count <= 20) problems.Add(zp + "Crowsfoot Hollow's furnishings are not marked for the nodes to keep clear of");
            foreach (var c in container.GetComponentsInChildren<Collider>(true)) problems.Add(zp + "a node's '" + c.name + "' has a collider");
            if (container.GetComponentsInChildren<NavBlocker>(true).Length > 0 || container.GetComponentsInChildren<NavWalkable>(true).Length > 0) problems.Add(zp + "a node is in the navmesh");
            foreach (var i in nodes.Where(i => i.kind == "herb")) foreach (var c in i.root.GetComponentsInChildren<Collider>(true)) problems.Add(zp + "the herb '" + i.name + "' has a collider");
            if (!NavMesh.SamplePosition(zone.Ground(z.spawns.player), out var start, 2.5f, NavMesh.AllAreas)) { problems.Add(zp + "the player's start is not walkable"); return; }
            var path = new NavMeshPath(); var keys = new HashSet<string>();
            int under = 0, wantUnder = z.nodes.Count(n => n != null && n.under);
            foreach (var i in nodes)
            {
                var def = s.Professions.Db.Node(i.node);
                string q = zp + "'" + i.name + "' at " + i.position + " ";
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
                // Clear of every camp's spread (its mobs stand anywhere in the square of its radius) and of the caves' furnishings
                // (the deserters' fire, bedrolls and stores, the Drop's treads and rail, the throne), by the node's footprint.
                float foot = ZoneBuilder.NodeFootprint(def.look) - .05f;
                foreach (var cp in z.camps) if (cp != null && Vector2.Distance(at, cp.center) < cp.radius * 1.42f + foot) problems.Add(q + "stands in the camp '" + cp.name + "'");
                foreach (var spot in zone.KeepClearSpots) if (Vector2.Distance(at, spot.at) < spot.r + foot) problems.Add(q + "stands in a cave's furnishings at " + spot.at);
                // Where it belongs.
                if (def.look == "windfall" && !z.groves.Any(g => g != null && g.kind != "orchard" && (def.id != "node.oak" || g.kind == "broadleaf") && OutsideRect(at, g.center, g.size) < 6 && OutsideRect(at, g.center, g.size) > -4))
                    problems.Add(q + (def.id == "node.oak" ? "is no windfall at a broadleaf wood's edge" : "is no windfall at a wood's edge"));
                if (def.look == "herb")
                {
                    if (i.kind == "node" && z.groves.Any(g => g != null && OutsideRect(at, g.center, g.size) < 0)) problems.Add(q + "is a herb in a wood, not in the open");
                    if (z.fields.Any(f => f != null && OutsideRect(at, f.center, f.size, f.rotation) < .5f)) problems.Add(q + "is a herb on a ploughed field");
                }
                if (def.look == "ore" && !z.props.Any(p => p != null && (p.kind == "cliff" || p.kind == "rock" || p.kind == "perch") && Vector2.Distance(p.at, at) < (p.kind == "cliff" ? (p.size.x > 0 ? p.size.x : 20) / 2 + 4 : 6))
                    && !z.shapes.Any(sh => sh != null && sh.height > 8 && Vector2.Distance(sh.center, at) < sh.radius))
                    problems.Add(q + "is a seam with no crag or rock by it, and not on a ridge's crown");
            }
            if (under != wantUnder) problems.Add(zp + under + " nodes in a cave, where the data puts " + wantUnder + " under (rich seams on a cave floor)");
            CheckStations(s, nodes, start.position, problems);
        }
        /// <summary>Each zone's stations (DESIGN 6.1): which kinds it has (the workplaces' and the inn's, and its own).</summary>
        static readonly Dictionary<string, string[]> StationKinds = new Dictionary<string, string[]> {
            { "zone.oakhaven", new[] { "bench", "fire", "forge" } }, { "zone.khaven", new[] { "bench", "fire" } }, { "zone.peaks", new[] { "fire", "forge" } },
            { "zone.ashrim", new[] { "bench", "fire", "forge" } }, { "zone.verdant", new[] { "bench", "fire", "forge" } } };
        /// <summary>
        /// The zone's stations (BUILD_PLAN step 9): the kinds DESIGN 6.1 gives it; every one can be walked to from the start, to
        /// within reach of it (EncounterSession.StationRange); its own (field anvils, benches, cookfires, under "Zone stations") one
        /// for each in the data where the data puts it, with something to see and nothing solid or in the navmesh, out of the water,
        /// buildings and off the roads, 3 m from every node and 5 m from every secret, and 2 m from every trunk.
        /// </summary>
        static void CheckStations(EncounterSession s, List<ZoneInteractable> nodes, Vector3 start, List<string> problems)
        {
            var zone = s.Zone; var z = zone.Zone; string zp = z.displayName + ": "; var path = new NavMeshPath();
            var kinds = new SortedSet<string>(zone.Stations.Select(x => x.kind), StringComparer.Ordinal);
            if (StationKinds.TryGetValue(z.id, out var want) && !kinds.SequenceEqual(want)) problems.Add(zp + "stations " + string.Join(", ", kinds) + ", where DESIGN 6.1 has " + string.Join(", ", want));
            foreach (var st in zone.Stations)
            {
                string q = zp + "the " + st.kind + " '" + st.name + "' at " + st.position + " ";
                bool walked = false;
                for (int k = 0; k <= 16 && !walked; k++)
                {
                    var c = k == 0 ? st.position : zone.StandAt(new Vector2(st.position.x, st.position.z) + new Vector2(Mathf.Cos(k * Mathf.PI / 8), Mathf.Sin(k * Mathf.PI / 8)) * (k % 2 == 0 ? 1.5f : 3f), st.position.y);
                    if (!NavMesh.SamplePosition(c, out var hit, 1.5f, NavMesh.AllAreas) || Flat(hit.position, st.position) > EncounterSession.StationRange - .5f || Mathf.Abs(hit.position.y - st.position.y) > 2) continue;
                    walked = NavMesh.CalculatePath(start, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
                }
                if (!walked) problems.Add(q + "can't be walked to within reach from the start");
            }
            var own = z.stations ?? new ZoneStation[0]; var container = zone.transform.Find("Zone stations");
            if (own.Length == 0) { if (container != null) problems.Add(zp + "builds stations it has none of"); return; }
            if (container == null) { problems.Add(zp + "its " + own.Length + " stations are not built"); return; }
            if (container.childCount != own.Length) problems.Add(zp + container.childCount + " of its " + own.Length + " stations are built");
            foreach (var c in container.GetComponentsInChildren<Collider>(true)) problems.Add(zp + "a station's '" + c.name + "' has a collider");
            if (container.GetComponentsInChildren<NavBlocker>(true).Length > 0 || container.GetComponentsInChildren<NavWalkable>(true).Length > 0) problems.Add(zp + "a station is in the navmesh");
            foreach (var d in own)
            {
                var st = zone.Stations.Find(x => x.name == d.name && x.kind == d.kind);
                string q = zp + "the " + d.kind + " '" + d.name + "' ";
                if (st == null || st.root == null || !st.root.IsChildOf(container)) { problems.Add(q + "is not built"); continue; }
                var at = new Vector2(st.position.x, st.position.z);
                if (Vector2.Distance(at, d.at) > .01f) problems.Add(q + "stands at " + at + ", not where the data puts it (" + d.at + ")");
                if (st.root.GetComponentsInChildren<Renderer>().Length < 10) problems.Add(q + "has next to nothing to see");
                if (zone.WaterAt(at, out _, out _)) problems.Add(q + "stands in the water");
                if (zone.InBuilding(at)) problems.Add(q + "stands in a building");
                foreach (var r in z.roads) if (r != null && r.points.Length > 1 && ToPath(at, r.points) < r.width / 2 + .5f) problems.Add(q + "stands on " + r.name);
                foreach (var n in nodes) if (Flat(n.position, st.position) < 3) problems.Add(q + "is within 3 m of the node '" + n.name + "' at " + n.position);
                foreach (var spot in s.SecretSpots) if (Flat(spot.position, st.position) < 5) problems.Add(q + "is within 5 m of the secret '" + spot.def.id + "'");
                if (!zone.TrunkClear(at, 2)) problems.Add(q + "is within 2 m of a tree trunk");
                if (Hollow.InsideAny(st.position + Vector3.up * .3f, 0)) problems.Add(q + "is in a cave");
            }
        }
    }
}
#endif
