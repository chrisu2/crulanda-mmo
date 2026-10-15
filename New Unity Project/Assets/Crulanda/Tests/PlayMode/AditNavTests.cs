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
    /// The Sealed Adit's navigation mesh keeps to the floor (2026-10-11 group runs: an Ash mender at 17:37 and the leading Warrior
    /// at 18:44 ended up standing on a column's or plinth's top and the run stalled there). Nothing solid in the caves (every
    /// NavBlocker box: the kit's columns, plinths, urns, walls) may carry walkable mesh on or above its top, and no walkable
    /// triangle may stand more than a step above the cave floor inside a solid's footprint.
    /// </summary>
    public class AditNavTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditnav-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            ZoneBuilder.RequestedZoneId = "zone.adit";
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

        /// <summary>A solid's box in the world: its centre, rotation and half size.</summary>
        struct Solid { public Vector3 centre; public Quaternion rot; public Vector3 half; public string name; public float foot; public Bounds flat; }
        static List<Solid> TallSolids(float minHeight)
        {
            var list = new List<Solid>();
            foreach (var blocker in UnityEngine.Object.FindObjectsByType<NavBlocker>(FindObjectsSortMode.None))
                foreach (var box in blocker.GetComponents<BoxCollider>())
                {
                    var tr = box.transform; var sc = tr.lossyScale;
                    if (tr.parent != null && tr.parent.name == "Decks") continue;   // a deck's own posts and rails (the Adit rework): its planks stand on them
                    var size = Vector3.Scale(box.size, new Vector3(Mathf.Abs(sc.x), Mathf.Abs(sc.y), Mathf.Abs(sc.z)));
                    if (size.y < minHeight) continue;
                    var at = tr.TransformPoint(box.center); if (!Hollow.InsideAny(at, 0)) continue;   // the caves' solids, not the slopes' rocks outside
                    float foot = at.y - size.y / 2, floor = foot; if (Hollow.FloorUnder(new Vector2(at.x, at.z), ref floor)) foot = Mathf.Max(foot, floor);   // its foot on the cave floor
                    float r = new Vector2(size.x, size.z).magnitude / 2;
                    list.Add(new Solid { centre = at, rot = tr.rotation, half = size / 2, foot = foot, flat = new Bounds(at, new Vector3(r * 2, size.y + 6, r * 2)), name = tr.name + " under " + (tr.parent != null ? tr.parent.name : "-") });
                }
            return list;
        }
        static bool InFootprint(Solid s, Vector3 p, float shrink)
        {
            var l = Quaternion.Inverse(s.rot) * (p - s.centre);
            return Mathf.Abs(l.x) < s.half.x - shrink && Mathf.Abs(l.z) < s.half.z - shrink;
        }

        [UnityTest, Timeout(600000)] public IEnumerator No_walkable_mesh_on_top_of_columns_plinths_or_any_solid()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id);
            yield return null;
            var solids = TallSolids(.9f);
            Assert.Greater(solids.Count, 50, "the Adit's columns, walls and furnishings are solid");
            var tri = NavMesh.CalculateTriangulation(); Assert.Greater(tri.vertices.Length, 100, "the Adit has a navmesh");
            var problems = new List<string>();
            for (int i = 0; i + 2 < tri.indices.Length; i += 3)
            {
                var c = (tri.vertices[tri.indices[i]] + tri.vertices[tri.indices[i + 1]] + tri.vertices[tri.indices[i + 2]]) / 3;
                foreach (var s in solids)
                {
                    if (!s.flat.Contains(c)) continue; float bottom = s.foot;
                    // A walkable triangle inside a solid's footprint, more than a step (0.6 m) above its foot: on top of it.
                    if (c.y > bottom + .6f && c.y < s.centre.y + s.half.y + 2.5f && InFootprint(s, c, .05f))
                    { problems.Add(s.name + " at " + s.centre.ToString("0.0") + ": walkable at y " + c.y.ToString("0.00") + " (foot " + bottom.ToString("0.00") + ", top " + (s.centre.y + s.half.y).ToString("0.00") + ")"); break; }
                }
                if (problems.Count >= 25) break;
            }
            Assert.IsEmpty(problems, problems.Count + " walkable triangles on solids (first 25):\n" + string.Join("\n", problems));
        }

        /// <summary>Every camp's mobs can be walked to from the way in, or come down to it from a ledge, the gates open (2026-10-13: the evening
        /// group run's leader could not reach the Ember Vent's lava ledge and stood eleven minutes leading the party to it).</summary>
        [UnityTest] public IEnumerator Every_camp_can_be_walked_to_from_the_way_in()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); Assert.NotNull(s); Assert.AreEqual("zone.adit", s.ZoneId);
            s.Zone.OpenGates(); for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(NavMesh.SamplePosition(s.Player.transform.position, out var start, 4, NavMesh.AllAreas), "the way in is on the mesh");
            var bad = new List<string>(); var path = new NavMeshPath(); int n = 0, ledges = 0;
            foreach (var e in s.Enemies)
            {
                if (e == null || !e.Camp || e.Game) continue; n++;
                if (e.MobName == EncounterSession.NixName) continue;   // a rider: Nix sits in the Rock-Eater's saddle, up off the floor
                if (!NavMesh.SamplePosition(e.transform.position, out var at, 4f, NavMesh.AllAreas)) { bad.Add(e.Name + " at " + e.transform.position.ToString("0.0") + ": off the mesh"); continue; }
                if (!NavMesh.CalculatePath(start.position, at.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    // Up on a ledge it can come down from (a one-way drop: the Ember Vent's chapel, the lava ledge): pulled, it comes to the party
                    // (the evening run's 47:28 frame), so that is a way to it.
                    var back = new NavMeshPath();
                    if (NavMesh.CalculatePath(at.position, start.position, NavMesh.AllAreas, back) && back.status == NavMeshPathStatus.PathComplete) { ledges++; continue; }
                    var near = path.corners.Length > 0 ? path.corners[path.corners.Length - 1] : start.position;   // as near as a path comes
                    bad.Add(e.Name + " at " + e.transform.position.ToString("0.0") + ": no way there (" + path.status + "; the nearest reachable " + near.ToString("0.0") + ", " + Vector3.Distance(near, at.position).ToString("0.0") + " m off, " + (at.position.y - near.y).ToString("0.0") + " m up; " + WalkableNear(at.position, start.position) + ")");
                }
            }
            Assert.Greater(n, 60, "the Adit's camp mobs");
            Debug.Log("Adit camp mobs on ledges they come down from: " + ledges);
            Assert.IsEmpty(bad, "camp mobs no one can walk to, and that cannot come down");
        }
        /// <summary>
        /// (The Adit rework) The web joins up and its ways home run one way. Every cave that joins another (the Workings' lip 14 m up the Gallery,
        /// the Cutting Drift onto the Foundry's catwalk, the Flooded Working into the tally hall, the Dry Ledge back into the river, the Muster Yard
        /// into the chapel, the cloister onto the Gallery's floor, the Breach's lip over the falls) can be walked (or dropped) from its end into
        /// the cave it joins. The chute and the falls are drops: down in a few steps, never back up them (the way back up is the long way round),
        /// and a camp's mobs never take them. The cloister stair is the chapter-house's short way home.
        /// </summary>
        [UnityTest, Timeout(300000)] public IEnumerator The_web_joins_up_and_its_ways_home_run_one_way()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id);
            zone.OpenGates(); for (int i = 0; i < 60; i++) yield return null;   // a second: the gates' carves are lifted by the navmesh's own update
            Hollow Cave(string name) { var h = Hollow.All.Find(o => o.Name == name); Assert.NotNull(h, name); return h; }
            Vector3 On(Vector3 p, string what, float reach = 3) { Assert.IsTrue(NavMesh.SamplePosition(p, out var hit, reach, NavMesh.AllAreas), what + " is on the navmesh (" + p + ")"); return hit.position; }
            Vector3 Anchor(string name) { Assert.IsTrue(zone.Anchors.TryGetValue(name, out var a), "the anchor '" + name + "'"); return On(a + Vector3.up * .2f, "the " + name, 5); }   // (the altar's anchor is the altar's own place: the floor beside it)
            // Each join, from the joining cave's last stretch of floor to the floor of the cave it joins, under its end (in the doorway).
            int joins = 0;
            foreach (var h in Hollow.All)
            {
                if (h.Join == null) continue; joins++;
                int last = h.Centre.Count - 1; while (last > 0 && !h.Floored(last)) last--;
                var from = On(h.Centre[last] + Vector3.up * .2f, h.Name + "'s last floor (" + h.Along[last].ToString("0") + " m in)");
                var end = h.Centre[h.Centre.Count - 1]; if (!h.Join.FloorAt(new Vector2(end.x, end.z), out float under)) under = h.Join.At(h.JoinAlong).y;
                var to = On(new Vector3(end.x, under + .2f, end.z), h.Join.Name + "'s floor where " + h.Name + " joins it", 5);
                Assert.Less(NavReach.Length(from, to), float.PositiveInfinity, h.Name + " joins " + h.Join.Name + (h.JoinRaised ? " (a lip " + h.JoinRise.ToString("0") + " m up)" : "") + ": the way on from " + from + " to " + to);
                yield return null;
            }
            Assert.GreaterOrEqual(joins, 6, "the web's joins (loops and lips)");
            // The drops: down in a few steps, not back up them; mobs keep off them.
            int noDrops = NavMesh.AllAreas & ~(1 << EncounterNavigation.DropArea);
            foreach (var (top, pool) in new[] { (Cave("The Geode Foundry").At(10), "chute pool"), (Cave("The Grey Breach").At(206), "plunge pool") })
            {
                var lip = On(top + Vector3.up * .2f, "above the " + pool); var foot = Anchor(pool);
                Assert.Less(NavReach.Length(lip, foot), 60, "down to the " + pool + " by the drop");
                Assert.Greater(NavReach.Length(foot, lip), 120, "no way back up the drop from the " + pool + ": only the long way round");
                Assert.Greater(NavReach.Length(lip, foot, noDrops), 120, "a camp's mob does not take the drop to the " + pool);
                yield return null;
            }
            // The cloister stair: from the chapel's altar to the Gallery's floor, far shorter than back through the chapter-house.
            var altar = Anchor("altar"); var cloister = Anchor("cloister foot");
            float home = NavReach.Length(altar, cloister); Assert.Less(home, float.PositiveInfinity, "the cloister stair from the altar to the Gallery");
            var door = zone.Gates.Find(g => g != null && g.Title == "The cloister door"); Assert.NotNull(door, "the cloister door");
            door.SetOpen(false, true); door.Recarve(); for (int i = 0; i < 3; i++) yield return null;
            Assert.Greater(NavReach.Length(altar, cloister), home + 50, "with the cloister door shut it is the long way back through the chapter-house (" + home.ToString("0") + " m by the stair)");
            door.SetOpen(true, true);
        }
        /// <summary>The nearest spot on the same floor (within 2.5 m of its height) that a path from the way in reaches, out to 30 m.</summary>
        static string WalkableNear(Vector3 at, Vector3 start)
        {
            var p = new NavMeshPath();
            for (float r = 1; r <= 30; r += 1)
                for (int k = 0; k < 16; k++)
                {
                    var q = at + Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward * r;
                    if (NavMesh.SamplePosition(q, out var h, .8f, NavMesh.AllAreas) && Mathf.Abs(h.position.y - at.y) < 2.5f && NavMesh.CalculatePath(start, h.position, NavMesh.AllAreas, p) && p.status == NavMeshPathStatus.PathComplete)
                        return "walkable at x " + h.position.x.ToString("0.0") + " z " + h.position.z.ToString("0.0") + ", " + r + " m away";
                }
            return "nothing walkable on its floor within 30 m";
        }
        [UnityTest] public IEnumerator A_point_dropped_on_a_solids_top_snaps_to_the_floor_beside_it()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id);
            yield return null;
            var problems = new List<string>();
            foreach (var s in TallSolids(1.2f))
            {
                var top = s.centre + Vector3.up * s.half.y;
                if (NavMesh.SamplePosition(top + Vector3.up * .1f, out var hit, .8f, NavMesh.AllAreas) && hit.position.y > top.y - .4f)
                    problems.Add(s.name + " at " + s.centre.ToString("0.0") + ": its top is on the navmesh (y " + hit.position.y.ToString("0.00") + ")");
                if (problems.Count >= 25) break;
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>The caves' navmesh is one way through (a mob or the leader on a ledge, a column's top or behind a wall stalls a run;
        /// 2026-10-11, 17:37 and 18:44): on a 3 m grid of its triangles, each joined to the nearest walked breadcrumb by a complete short
        /// path, the pockets left over are few and logged, and no mob stands in one.</summary>
        [UnityTest, Timeout(600000)] public IEnumerator Every_mob_stands_on_the_way_through_and_pockets_are_few()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.adit", zone.Zone.id);
            zone.OpenGates(); for (int i = 0; i < 60; i++) yield return null;   // a second: the gates' carves are lifted by the navmesh's own update
            Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player, .2f), out var start, 2.5f, NavMesh.AllAreas), "the yard is on the navmesh");
            var crumbs = NavReach.CaveCrumbs(start.position, out var reach);
            Assert.Greater(reach.Count, 100, "the way through is walked (" + reach.Count + " of " + crumbs.Count + " breadcrumbs)");
            var walked = reach.Select(i => crumbs[i]).ToList();
            var tri = NavMesh.CalculateTriangulation(); var cells = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i + 2 < tri.indices.Length; i += 3)
            {
                var c = (tri.vertices[tri.indices[i]] + tri.vertices[tri.indices[i + 1]] + tri.vertices[tri.indices[i + 2]]) / 3;
                if (!Hollow.InsideAny(c + Vector3.up * .3f, 0)) continue;
                float under = float.NaN; if (!Hollow.FloorUnder(new Vector2(c.x, c.z), ref under) || c.y < under - 1 || c.y > under + 8) continue;   // in a cave, not the land over it
                var key = new Vector3Int(Mathf.FloorToInt(c.x / 3), Mathf.FloorToInt(c.y / 1.5f), Mathf.FloorToInt(c.z / 3));
                if (!cells.ContainsKey(key)) cells[key] = c;
            }
            Assert.Greater(cells.Count, 200, "the caves have navmesh");
            var path = new NavMeshPath(); int checkedCells = 0;
            bool Joined(Vector3 c)
            {
                foreach (var w in walked.OrderBy(w => (w - c).sqrMagnitude).Take(4))
                    if (NavMesh.CalculatePath(c, w, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) return true;
                return false;
            }
            // Pockets the way through never reaches (behind a hall's wall, a corner of rock) are logged: nothing may stand in one.
            var pockets = new List<string>();
            foreach (var c in cells.Values)
            {
                checkedCells++; if (checkedCells % 200 == 0) yield return null;
                if (Joined(c)) continue;
                var nearest = UnityEngine.Object.FindObjectsByType<NavBlocker>(FindObjectsSortMode.None).OrderBy(b => (b.transform.position - c).sqrMagnitude).FirstOrDefault();
                pockets.Add(c.ToString("0.0") + (nearest != null ? " (nearest solid '" + nearest.name + "' " + Vector3.Distance(nearest.transform.position, c).ToString("0.0") + " m)" : ""));
            }
            Debug.Log("AditNavTests: " + pockets.Count + " of " + cells.Count + " cave patches are pockets off the way through:\n" + string.Join("\n", pockets.Take(40)));
            // Every mob, where it stands, is on the way through: none spawns or strolls into a pocket, a column's top or a shelf.
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var problems = new List<string>();
            foreach (var e in s.Enemies.Where(e => e != null && e.actor.IsAlive && e.Inside == null))   // not Nix in the Rock-Eater's saddle
            {
                var p = e.transform.position; if (!Hollow.InsideAny(p + Vector3.up * .3f, 0)) continue;
                if (!NavMesh.SamplePosition(p, out var at, 1.5f, NavMesh.AllAreas)) { problems.Add(e.actor.DisplayName + " at " + p.ToString("0.0") + ": off the navmesh"); continue; }
                if (!Joined(at.position)) problems.Add(e.actor.DisplayName + " at " + p.ToString("0.0") + ": in a pocket off the way through");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(30)));
            Assert.Less(pockets.Count, cells.Count / 20, "pockets stay a few corners (" + pockets.Count + " of " + cells.Count + ")");
        }
    }
}
#endif