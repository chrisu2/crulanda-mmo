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
    /// The Crown's Crafters' Court built and walked (p9-court; ZoneBuilder.CrownCourt): every workshop stands with its roof that fades where
    /// it hides you; every trade's station is built inside its workshop as the trade's own (turned with it, plenty to see, nothing solid)
    /// and is worked from the court; the Court's ways (ZoneBuilder.CourtWays) are on the navmesh and walked from the Gilt Approach;
    /// every trainer and stall-keeper faces out of the workshop to the court, and the apprentices are at the workshops' benches.
    /// </summary>
    public class CrownCourtPlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.crown";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-court-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static Vector3 Start(ZoneBuilder zone) { Assert.IsTrue(NavMesh.SamplePosition(zone.Ground(zone.Zone.spawns.player), out var hit, 2.5f, NavMesh.AllAreas), "The Gilt Approach is walkable."); return hit.position; }
        static Transform Built(ZoneBuilder zone, string name) { return zone.transform.Find("Zone static scenery/" + name) ?? zone.transform.Find("Zone props/" + name); }
        static float Flat(Vector3 a, Vector3 b) { return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); }

        [UnityTest] public IEnumerator Every_workshop_stands_with_its_station_inside_worked_from_the_court()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.crown", zone.Zone.id); var start = Start(zone); var problems = new List<string>();
            var stations = zone.transform.Find("Zone stations");
            foreach (var p in zone.Zone.props.Where(p => ZoneBuilder.CourtTradeOf(p) != null))
            {
                var tr = ZoneBuilder.CourtTradeOf(p); var t = Built(zone, p.name);
                if (t == null) { problems.Add(p.name + " is not built"); continue; }
                if (t.GetComponent<RoofFade>() == null) problems.Add(p.name + " does not fade where it hides you");
                // Built in the static scenery, its plain parts are merged into one mesh a material (ZoneBuilder.MergeStatics): so it is
                // counted by its materials and its vertices, not its renderers (the Smithy showed 28 renderers merged from hundreds of parts).
                var looks = t.GetComponentsInChildren<Renderer>(); int materials = looks.SelectMany(x => x.sharedMaterials).Where(m => m != null).Distinct().Count();
                int verts = looks.Select(x => x.GetComponent<MeshFilter>()).Where(f => f != null && f.sharedMesh != null).Sum(f => f.sharedMesh.vertexCount);
                if (materials < 12 || verts < 3000) problems.Add(p.name + " has next to nothing in it (" + looks.Length + " renderers, " + materials + " materials, " + verts + " vertices)");
                if (t.GetComponentsInChildren<Collider>().Length < 5) problems.Add(p.name + ": its walls and posts are not solid");
                // Its front: walked to from the Gilt Approach.
                var before = zone.Ground(ZoneBuilder.CourtWorld(p, new Vector2(0, -tr.d / 2 - 1)));
                if (!NavMesh.SamplePosition(before, out var fh, 1, NavMesh.AllAreas) || !NavReach.Walkable(start, fh.position)) problems.Add(p.name + ": its open front can't be walked to");
                if (tr.station == null) continue;
                var spot = zone.Stations.Find(s => s.kind == tr.station && Flat(s.position, zone.Ground(ZoneBuilder.CourtWorld(p, tr.stationAt))) < .3f);
                if (spot == null) { problems.Add(p.name + ": no " + tr.station + " on its station spot"); continue; }
                if (stations == null || !spot.root.IsChildOf(stations)) problems.Add(p.name + ": its station is not one of the zone's own");
                if (Mathf.Abs(Mathf.DeltaAngle(spot.root.eulerAngles.y, p.rotation)) > 1) problems.Add(p.name + ": its station is not turned with it");
                if (spot.root.GetComponentsInChildren<Renderer>().Length < 15) problems.Add(p.name + ": its station has little to see");
                if (spot.root.GetComponentsInChildren<Collider>().Length > 0) problems.Add(p.name + ": its station has a collider of its own");
                // Worked from the court: walked to, within reach, from before the open front.
                if (!NavMesh.SamplePosition(zone.Ground(ZoneBuilder.CourtWorld(p, new Vector2(tr.stationAt.x, -tr.d / 2 - .6f))), out var wh, 1, NavMesh.AllAreas) || Flat(wh.position, spot.position) > EncounterSession.StationRange - .5f || !NavReach.Walkable(start, wh.position))
                    problems.Add(p.name + ": its " + tr.station + " can't be worked from before its front");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }

        [UnityTest] public IEnumerator The_Courts_ways_are_walked_and_nothing_stands_in_them()
        {
            var zone = ZoneBuilder.Active; var start = Start(zone); var problems = new List<string>();
            foreach (var (name, at) in ZoneBuilder.CourtWayPoints().Where((w, i) => i % 8 == 0))
                if (!NavMesh.SamplePosition(zone.Ground(at), out var hit, .6f, NavMesh.AllAreas) || Flat(hit.position, zone.Ground(at)) > .4f || !NavReach.Walkable(start, hit.position))
                    problems.Add(name + " is blocked at " + at);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }

        [UnityTest] public IEnumerator The_trainers_and_keepers_face_the_court_and_the_apprentices_work()
        {
            yield return new WaitForSeconds(4);   // their first activity begun, turned to it
            var zone = ZoneBuilder.Active; var life = VillageLife.Active; Assert.IsNotNull(life); var start = Start(zone); var problems = new List<string>();
            foreach (var p in zone.Zone.props.Where(p => ZoneBuilder.CourtTradeOf(p) != null))
            {
                var tr = ZoneBuilder.CourtTradeOf(p); var out3 = ZoneBuilder.CourtWorld(p, new Vector2(0, -1)) - p.at;
                foreach (var r in zone.Zone.life.residents.Where(r => r != null && r.at != Vector2.zero && (Vector2.Distance(r.at, ZoneBuilder.CourtWorld(p, tr.trainerAt)) < .1f || Vector2.Distance(r.at, ZoneBuilder.CourtWorld(p, tr.vendorAt)) < .1f)))
                {
                    var v = life.Find(r.name); if (v == null) { problems.Add(r.name + " is not spawned"); continue; }
                    if (!zone.Workplaces.Any(w => w.kind == "lodge" && Flat(w.stand, zone.Ground(r.at)) < .1f)) problems.Add(r.name + "'s post in " + p.name + " is not registered");
                    var f = v.transform.forward; if (Vector2.Dot(new Vector2(f.x, f.z).normalized, out3.normalized) < .5f) problems.Add(r.name + " faces " + f + ", not out of " + p.name + " to the court");
                }
            }
            foreach (var r in zone.Zone.life.residents.Where(r => r != null && r.role == "apprentice"))
            {
                var v = life.Find(r.name); if (v == null) { problems.Add(r.name + " is not spawned"); continue; }
                if (!NavMesh.SamplePosition(v.transform.position, out var hit, 2, NavMesh.AllAreas) || !NavReach.Walkable(start, hit.position)) problems.Add(r.name + " can't be walked to");
                if (!zone.Zone.props.Any(p => ZoneBuilder.CourtTradeOf(p) != null && Vector2.Distance(new Vector2(v.transform.position.x, v.transform.position.z), p.at) < 12)) problems.Add(r.name + " is not at the workshops (" + v.transform.position + ")");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
#endif
