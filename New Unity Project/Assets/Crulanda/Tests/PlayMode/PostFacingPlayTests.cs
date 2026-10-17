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
    /// People at their posts face the room, and an inn's door is clear to walk up to (2026-10-17, Chris at /crown: "still people
    /// standing backwards at /crown" — the Hall of Arms' trainers faced their racks and the wall — and "people stuck getting into
    /// inn" — a knot of folk against the Gilded Lantern's column):
    /// - every trainer and vendor at a fixed post, and everyone at a fixed post in a city, stood at it faces open floor: 2.5 m clear
    ///   straight ahead at chest height, or 1.2 m with a way clear for 3 m within 60 degrees, wherever the post has any such way (VillageLife.ClearRun);
    /// - every inn's lane ends, its places in line (six going in, four going out) and the way up to the door from 4 m out are on the
    ///   navmesh with a way of 0.9 m round them, and from each straight to the lane's end the navmesh runs unbroken and nothing solid
    ///   stands in a body's width (a sphere of 0.3 m at 1 m up).
    /// </summary>
    public class PostFacingPlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Load(string zone)
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = zone;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-posts-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 4; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            VillageEconomy.ResetAll();
            Time.timeScale = 1; WorldClock.Hour = 8.5f; ZoneBuilder.RequestedZoneId = null;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static bool Minded(Villager v, bool city)
        {
            if (v.Post == null || v.Seated) return false;
            string r = v.Role ?? "";
            return city || r.StartsWith("trainer") || r.Contains("vendor") || r.StartsWith("profession");
        }

        [UnityTest] public IEnumerator Posts_face_the_open_side([Values("zone.crown", "zone.oakhaven", "zone.khaven", "zone.lowtowns")] string zoneId)
        {
            yield return Load(zoneId);
            var zone = ZoneBuilder.Active; Assert.AreEqual(zoneId, zone.Zone.id);
            var life = VillageLife.Active; Assert.IsNotNull(life);
            bool city = zone.Zone.city;
            // Long enough for everyone to step on to their post and settle (they are made at it).
            Time.timeScale = 4;
            for (float t = 0; t < 3; t += Time.unscaledDeltaTime) yield return null;
            Time.timeScale = 1; Physics.SyncTransforms();
            var problems = new List<string>(); int checkedPosts = 0;
            foreach (var v in life.Villagers)
            {
                if (!Minded(v, city) || !v.AtPost) continue;
                var agent = v.GetComponent<NavMeshAgent>();
                var feet = v.transform.position - Vector3.up * (agent != null ? agent.baseOffset : 1);
                float most = 0;
                for (int i = 0; i < 24; i++) most = Mathf.Max(most, VillageLife.ClearRun(feet, Quaternion.Euler(0, i * 15, 0) * Vector3.forward, 3));
                if (most < 2.9f) continue;
                checkedPosts++;
                // Facing open floor: 2.5 m clear straight ahead, or at least 1.2 m with a way clear for 3 m within 60 degrees of it
                // (2026-10-17: a court apprentice at her bench looked out past a post 2.8 m off, open floor beside it).
                float ahead = VillageLife.ClearRun(feet, v.transform.forward, 3);
                bool openNear = false;
                for (int a = -60; a <= 60; a += 15) if (VillageLife.ClearRun(feet, Quaternion.Euler(0, a, 0) * v.transform.forward, 3) >= 2.9f) openNear = true;
                if (ahead < 2.5f && !(ahead >= 1.2f && openNear)) problems.Add(v.Name + " <" + v.Title + "> faces a wall or a prop " + ahead.ToString("0.0") + " m ahead at " + feet + " (yaw " + v.transform.eulerAngles.y.ToString("0") + ")");
            }
            if (zoneId == "zone.crown") Assert.GreaterOrEqual(checkedPosts, 11, "The Hall of Arms' eleven trainers at least.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [UnityTest] public IEnumerator Inn_doors_are_clear_to_walk_up_to([Values("zone.crown", "zone.oakhaven", "zone.khaven", "zone.lowtowns", "zone.ashrim", "zone.peaks")] string zoneId)
        {
            yield return Load(zoneId);
            var zone = ZoneBuilder.Active; Assert.AreEqual(zoneId, zone.Zone.id);
            Physics.SyncTransforms();
            var problems = new List<string>(); int inns = 0;
            foreach (var inn in zone.Inns)
            {
                if (inn == null || inn.door == null || inn.door.Way == null) continue;
                inns++; var way = inn.door.Way;
                foreach (bool goingIn in new[] { true, false })
                {
                    var entry = way.Entry(goingIn);
                    if (!NavMesh.SamplePosition(entry, out var eh, .5f, NavMesh.AllAreas)) { problems.Add(inn.name + ": the lane's end " + (goingIn ? "outside" : "inside") + " is off the navmesh"); continue; }
                    var spots = new List<(string what, Vector3 at)>();
                    for (int k = 0; k < (goingIn ? 6 : 4); k++) spots.Add(((goingIn ? "in" : "out") + "-line place " + k, way.QueueSpot(goingIn, k)));
                    if (goingIn) spots.Add(("the way up, 4 m out", way.middle - way.inward * 4));
                    foreach (var (what, at) in spots)
                    {
                        if (!NavMesh.SamplePosition(at, out var h, .5f, NavMesh.AllAreas)) { problems.Add(inn.name + ": " + what + " at " + at + " is off the navmesh"); continue; }
                        if (NavMesh.FindClosestEdge(h.position, out var edge, NavMesh.AllAreas) && edge.distance < .15f) problems.Add(inn.name + ": " + what + " has under 0.9 m of floor round it (" + edge.distance.ToString("0.00") + " m to the navmesh's edge)");
                        if (NavMesh.Raycast(h.position, eh.position, out var cut, NavMesh.AllAreas)) problems.Add(inn.name + ": from " + what + " the floor to the lane's end is broken at " + cut.position);
                        var from = h.position + Vector3.up; var to = eh.position + Vector3.up; var dir = to - from;
                        foreach (var hit in Physics.SphereCastAll(from, .3f, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                        {
                            var c = hit.collider; if (c is CharacterController || c.GetComponentInParent<Villager>() != null || c.GetComponentInParent<Crulanda.Gameplay.Actor>() != null) continue;
                            if (c.transform.parent != null && c.transform.parent.name.StartsWith("Door hinge")) continue;
                            if (hit.distance <= 0 && hit.point == Vector3.zero) continue;
                            problems.Add(inn.name + ": " + c.name + " stands between " + what + " and the lane's end"); break;
                        }
                    }
                }
            }
            Assert.Greater(inns, 0, "The zone has an inn with a door.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
#endif
