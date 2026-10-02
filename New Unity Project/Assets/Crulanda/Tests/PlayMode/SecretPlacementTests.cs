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
    /// Hidden finds (ZoneSecret) in every zone, as ZoneBuilder builds them:
    /// - every secret in a zone's data stands in the world (ZoneBuilder.Secrets) with a prop to see (a lookout may be bare), and
    ///   nothing of theirs is solid or in the navmesh (no colliders, no NavBlocker or NavWalkable), so the navmesh is as it was;
    /// - each can be walked to from the player's start: a searchable to within reach of E (EncounterSession.UseRange), a lookout
    ///   to inside its radius;
    /// - none stands in a building's footprint or in the water, on a road, inside a camp, on a quest prop, at an exit, where you
    ///   start or where you arrive from another zone;
    /// - each zone has a lookout and at least two things to search;
    /// - ids are unique across the zones and name their own zone, every needs names a secret in the same zone (a key no chest
    ///   needs is an error), and every item and Chronicle page a secret gives exists.
    /// </summary>
    public class SecretPlacementTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-secrets-" + Guid.NewGuid().ToString("N"));
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
        static bool Searchable(ZoneSecret s) { return s.kind != "vista"; }
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

        [UnityTest] public IEnumerator Every_zone_builds_its_secrets_where_they_can_be_found()
        {
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var all = first.Zone.AllZones();
            var zones = all.Select(z => z.id).ToList();
            Assert.GreaterOrEqual(zones.Count, 4, "Oakhaven, Khaven, the Peaks and the Ashland Rim are registered.");
            var arrivals = new Dictionary<string, List<Vector2>>();
            foreach (var z in all) foreach (var e in z.exits) { if (!arrivals.ContainsKey(e.to)) arrivals[e.to] = new List<Vector2>(); arrivals[e.to].Add(e.arrive); }
            var problems = new List<string>();
            foreach (var id in zones)
            {
                ZoneBuilder.RequestedZoneId = id;
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                for (int i = 0; i < 4; i++) yield return null;
                var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = s.Zone; var z = zone.Zone;
                string p = z.displayName + ": ";
                if (z.id != id) { problems.Add(p + "built " + z.id + " instead of " + id); continue; }
                var defs = z.secrets ?? new ZoneSecret[0];
                if (!defs.Any(d => d != null && d.kind == "vista")) problems.Add(p + "has no lookout");
                if (defs.Count(d => d != null && Searchable(d)) < 2) problems.Add(p + "has fewer than two things to search");
                // Nothing of the secrets is solid or in the navmesh.
                var container = zone.transform.Find("Zone secrets");
                if (defs.Length > 0 && container == null) problems.Add(p + "built no secrets at all");
                if (container != null)
                {
                    foreach (var c in container.GetComponentsInChildren<Collider>(true)) problems.Add(p + "a secret's '" + c.name + "' has a collider");
                    if (container.GetComponentsInChildren<NavBlocker>(true).Length > 0 || container.GetComponentsInChildren<NavWalkable>(true).Length > 0) problems.Add(p + "a secret is in the navmesh");
                }
                if (!NavMesh.SamplePosition(zone.Ground(z.spawns.player), out var start, 2.5f, NavMesh.AllAreas)) { problems.Add(p + "player start is not walkable"); continue; }
                var path = new NavMeshPath();
                foreach (var d in defs)
                {
                    if (d == null) continue;
                    string q = p + "secret '" + d.id + "' ";
                    var spot = zone.Secrets.Find(x => x != null && x.def == d);
                    if (spot == null) { problems.Add(q + "is not built (ZoneBuilder.Secrets)"); continue; }
                    if (spot.root == null) problems.Add(q + "has no root");
                    else if (Searchable(d) && spot.root.GetComponentsInChildren<Renderer>().Length == 0) problems.Add(q + "has nothing to see");
                    // Walkable to: a navmesh point near it, within reach of E (or inside a lookout's radius), on a complete path from the start.
                    float reach = Searchable(d) ? EncounterSession.UseRange - .2f : d.radius;
                    // The nearest navmesh may be an island on a boulder's top: try the ground round it too, the way you'd walk up to it.
                    bool near = false, walked = false;
                    var tries = new List<Vector3> { spot.position };
                    for (int k = 0; k < 8; k++) foreach (float r in new[] { 1.2f, Mathf.Min(2.2f, reach) })
                    {
                        var round = new Vector2(spot.position.x, spot.position.z) + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * r;
                        tries.Add(zone.StandAt(round, spot.position.y, .2f));
                    }
                    for (int k = 0; k < tries.Count && !walked; k++)
                    {
                        if (!NavMesh.SamplePosition(tries[k], out var hit, k == 0 ? Mathf.Max(2.5f, reach + 1) : 1, NavMesh.AllAreas) || Flat(hit.position, spot.position) > reach) continue;
                        near = true;
                        walked = NavReach.Walkable(start.position, hit.position);
                    }
                    if (!near) problems.Add(q + "has no walkable ground within reach (" + reach + " m) of " + spot.position);
                    else if (!walked) problems.Add(q + "can't be walked to from the start");
                    // Where it stands.
                    var at = new Vector2(spot.position.x, spot.position.z);
                    if (zone.InBuilding(at)) problems.Add(q + "stands in a building");
                    if (zone.WaterAt(at, out _, out _)) problems.Add(q + "stands in the water");
                    foreach (var r in z.roads) if (r != null && r.points.Length > 1 && ToPath(at, r.points) < r.width / 2 + (Searchable(d) ? 1 : .5f)) problems.Add(q + "stands on " + r.name);
                    foreach (var c in z.camps) if (c != null && Vector2.Distance(at, c.center) < c.radius) problems.Add(q + "stands in the camp '" + c.name + "'");
                    foreach (var i in zone.Interactables) if (Flat(i.position, spot.position) < 5) problems.Add(q + "sits on the quest prop '" + i.name + "'");
                    foreach (var e in z.exits) if (Vector2.Distance(at, e.at) < e.radius + 2) problems.Add(q + "stands at the exit '" + e.name + "'");
                    if (Vector2.Distance(at, z.spawns.player) < 5) problems.Add(q + "stands where you start");
                    if (arrivals.TryGetValue(id, out var ins)) foreach (var a in ins) if (Vector2.Distance(at, a) < 5) problems.Add(q + "stands where you arrive at " + a);
                }
                foreach (var spot in zone.Secrets) if (spot != null && spot.def != null && Array.IndexOf(defs, spot.def) < 0) problems.Add(p + "built a secret '" + spot.def.id + "' that isn't in its data");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [UnityTest] public IEnumerator Secret_ids_needs_items_and_pages_hold_together()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            Assert.IsNotNull(s.Items, "Items load."); Assert.IsNotNull(s.Quests, "Quest content (the Chronicle pages) loads.");
            var problems = new List<string>(); var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            var kinds = new[] { "vista", "cache", "note", "herb", "chest", "key" };
            var zones = s.Zone.AllZones();
            foreach (var z in zones)
                foreach (var d in z.secrets ?? new ZoneSecret[0])
                {
                    if (d == null || string.IsNullOrEmpty(d.id)) { problems.Add(z.id + ": a secret has no id"); continue; }
                    if (seen.TryGetValue(d.id, out var other)) problems.Add("'" + d.id + "' is used in " + other + " and " + z.id); else seen[d.id] = z.id;
                }
            foreach (var z in zones)
            {
                string prefix = "secret." + z.id.Replace("zone.", "") + ".";
                var landmarks = new HashSet<string>(z.landmarks.Where(l => l != null).Select(l => l.name));
                foreach (var d in z.secrets ?? new ZoneSecret[0])
                {
                    if (d == null || string.IsNullOrEmpty(d.id)) continue;
                    string q = "'" + d.id + "': ";
                    if (!d.id.StartsWith(prefix, StringComparison.Ordinal) || d.id.Length == prefix.Length) problems.Add(q + "should be " + prefix + "<slug>");
                    if (Array.IndexOf(kinds, d.kind) < 0) problems.Add(q + "unknown kind '" + d.kind + "'");
                    if (string.IsNullOrEmpty(d.name) || string.IsNullOrEmpty(d.text) || string.IsNullOrEmpty(d.canonStatus)) problems.Add(q + "needs a name, a text and a canonStatus");
                    if (d.kind == "vista" ? !(d.radius > 0) : string.IsNullOrEmpty(d.prompt)) problems.Add(q + (d.kind == "vista" ? "a lookout needs a radius" : "needs a prompt for E"));
                    if (landmarks.Contains(d.name)) problems.Add(q + "shares its name with a landmark (the map would give it away)");
                    if (!string.IsNullOrEmpty(d.needs))
                    {
                        if (d.needs == d.id) problems.Add(q + "needs itself");
                        else if (!seen.TryGetValue(d.needs, out var where)) problems.Add(q + "needs '" + d.needs + "', which doesn't exist");
                        else if (where != z.id) problems.Add(q + "needs '" + d.needs + "' from another zone");
                    }
                    if (d.kind == "key" && !zones.Any(o => (o.secrets ?? new ZoneSecret[0]).Any(c => c != null && c.kind == "chest" && c.needs == d.id))) problems.Add(q + "is a key no chest needs");
                    if (!string.IsNullOrEmpty(d.item) && s.Items.Get(d.item) == null) problems.Add(q + "gives the unknown item '" + d.item + "'");
                    if (!string.IsNullOrEmpty(d.document) && !s.Quests.Db.Documents.ContainsKey(d.document)) problems.Add(q + "gives the unknown Chronicle page '" + d.document + "'");
                    if (d.xp < 0 || d.gold < 0) problems.Add(q + "negative rewards");
                }
            }
            Assert.Greater(seen.Count, 0, "The zones have secrets.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }
    }
}
#endif
