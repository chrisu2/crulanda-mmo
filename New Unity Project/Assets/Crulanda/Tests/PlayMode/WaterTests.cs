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
    /// Water in play, in every zone that has it:
    /// - what is drawn, what is carved and what the game feels all agree;
    /// - no floating edges; bridges stand clear and can be walked;
    /// - wading and swimming (head above water); critters and trees stay out;
    /// - a save made while swimming loads you on dry land.
    /// </summary>
    public class WaterTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-water-" + Guid.NewGuid().ToString("N"));
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

        [UnityTest] public IEnumerator Drawn_carved_and_felt_water_agree_in_every_zone()
        {
            var first = UnityEngine.Object.FindFirstObjectByType<EncounterSession>();
            var problems = new System.Collections.Generic.List<string>();
            foreach (var id in first.Zone.AllZones().Select(z => z.id).ToList())
            {
                ZoneBuilder.RequestedZoneId = id;
                yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
                for (int i = 0; i < 3; i++) yield return null;
                var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = s.Zone; var water = zone.Water; string p = zone.Zone.displayName + ": ";
                foreach (var c in water.Creeks)
                {
                    for (int i = 1; i < c.level.Length; i++) if (c.level[i] > c.level[i - 1] + 1e-4f) { problems.Add(p + c.def.name + " runs uphill at row " + i); break; }
                    int floating = 0, mismatched = 0, rows = 0;
                    for (int i = 0; i < c.pts.Length; i += 2)
                    {
                        var dir = i < c.pts.Length - 1 ? c.pts[i + 1] - c.pts[i] : c.pts[i] - c.pts[i - 1];
                        var side = new Vector2(-dir.y, dir.x).normalized; rows++;
                        foreach (int sgn in new[] { -1, 1 })
                        {
                            var edge = c.pts[i] + side * sgn * c.drawHalf;
                            if (zone.HeightAt(edge.x, edge.y) < c.level[i] - .03f) floating++;           // drawn edge must tuck into the bank
                        }
                        var mid = c.pts[i];
                        if (!zone.WaterAt(mid, out float surf, out float depth) || Mathf.Abs(surf - c.level[i]) > .02f) mismatched++;   // felt = drawn
                        else if (Mathf.Abs(depth - (c.depth - ZoneWater.BankDrop)) > .08f) mismatched++;                                  // carved = promised depth
                    }
                    if (floating > rows * .03f) problems.Add(p + c.def.name + ": drawn edge floats above the bank at " + floating + " of " + rows * 2 + " edge samples");
                    if (mismatched > 0) problems.Add(p + c.def.name + ": centre water disagrees with the drawn surface or depth at " + mismatched + " of " + rows + " rows");
                }
                foreach (var k in water.Lakes)
                {
                    int floating = 0;
                    for (int a = 0; a < 48; a++)
                    {
                        float ang = a * Mathf.PI / 24; var e = k.def.center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * k.drawRadius;
                        if (zone.HeightAt(e.x, e.y) < k.level - .03f) floating++;
                    }
                    if (floating > 0) problems.Add(p + k.def.name + ": lake edge floats at " + floating + "/48");
                    if (!zone.WaterAt(k.def.center, out float surf, out float depth) || Mathf.Abs(depth - k.def.depth) > .1f) problems.Add(p + k.def.name + ": centre depth " + depth + " vs " + k.def.depth);
                }
                // Bridges: the arch clears the water and agents can walk across end to end.
                foreach (var bridge in UnityEngine.Object.FindObjectsByType<NavWalkable>(FindObjectsSortMode.None).Select(w => w.transform.parent).Distinct())
                {
                    float len = zone.Zone.props.Where(pr => pr.kind == "bridge" && pr.name == bridge.name).Select(pr => pr.size.x > 0 ? pr.size.x : 12).DefaultIfEmpty(12).First();
                    Vector3 a = bridge.position + bridge.forward * (len / 2 + 1.5f), b = bridge.position - bridge.forward * (len / 2 + 1.5f);
                    if (!NavMesh.SamplePosition(a, out var ha, 3, NavMesh.AllAreas) || !NavMesh.SamplePosition(b, out var hb, 3, NavMesh.AllAreas)) { problems.Add(p + bridge.name + ": no navmesh at its ends"); continue; }
                    var path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) problems.Add(p + bridge.name + ": agents cannot cross it");
                    else if (TotalLength(path) > len * 2.2f) problems.Add(p + bridge.name + ": crossing takes a detour (" + TotalLength(path) + " m)");
                    if (zone.WaterAt(new Vector2(bridge.position.x, bridge.position.z), out float surface, out _) && bridge.position.y + 1.13f < surface + .5f) problems.Add(p + bridge.name + ": arch too close to the water");
                }
                // Critters and trees stay out of the water.
                if (VillageLife.Active != null) foreach (var c in VillageLife.Active.Critters) if (c.Kind != "crow" && zone.WaterAt(new Vector2(c.transform.position.x, c.transform.position.z), out _, out float cd) && cd > .15f) problems.Add(p + c.Kind + " stands in water");
                foreach (var t in zone.LeafTrees) if (water.NearWater(new Vector2(t.x, t.z), 0)) problems.Add(p + "tree planted in water at " + t);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
        static float TotalLength(NavMeshPath path) { float l = 0; for (int i = 1; i < path.corners.Length; i++) l += Vector3.Distance(path.corners[i - 1], path.corners[i]); return l; }

        [UnityTest] public IEnumerator Wading_and_swimming_in_Oakhaven()
        {
            var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); var zone = s.Zone;
            var motor = s.Player.GetComponent<AdventurerMotor>();
            // Wading: the middle of Oak creek, away from the bridges.
            var creek = zone.Water.Creeks[0]; var ford = creek.pts[creek.pts.Length / 3];
            motor.Teleport(zone.Ground(ford, 1.1f)); for (int i = 0; i < 30; i++) yield return null;
            Assert.IsTrue(zone.WaterAt(ford, out _, out float wadeDepth)); Assert.Less(wadeDepth, 1.2f);
            Assert.IsFalse(motor.Swimming, "Oak creek is for wading.");
            // Swimming: the middle of Brook pond. The body floats; the head stays above the water.
            var pond = zone.Water.Lakes[0];
            motor.Teleport(new Vector3(pond.def.center.x, pond.level - .3f, pond.def.center.y));
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(motor.Swimming, "Deep water: swimming.");
            float y = s.Player.transform.position.y;
            Assert.Less(Mathf.Abs(y - (pond.level - .3f)), .2f, "Floats just under the surface (root " + y + ", level " + pond.level + ").");

            Assert.Greater(s.Player.transform.position.y + .6f, pond.level, "Head and shoulders out of the water.");
            Assert.AreEqual(ActorPose.Swim, s.Player.GetComponent<ActorVisual>().Pose);
            // A save made in the pond remembers the last dry footing, so a reload never starts on the bed.
            s.Save(false);
            Assert.IsFalse(zone.WaterAt(new Vector2(s.Progress.x, s.Progress.z), out _, out float savedDepth) && savedDepth > .3f, "Saved on dry land.");
            // Shallow edge: back to walking.
            var edge = pond.def.center + new Vector2(pond.radius - .6f, 0);
            motor.Teleport(zone.Ground(edge, 1.1f)); for (int i = 0; i < 20; i++) yield return null;
            Assert.IsFalse(motor.Swimming, "At the pond's edge you stand.");
        }
    }
}
#endif
