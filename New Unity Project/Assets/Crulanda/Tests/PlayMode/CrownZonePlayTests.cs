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
    /// The Crown of Argentis built and walked (zone.crown; Docs/CAPITAL_HUB.md): the ground rises from the Gilt Approach up the Gilt Ramp
    /// to the High City and up the Grand Stair to the Crown terrace; every hall's floor can be walked into from the Gilt Approach; every
    /// person stands on the navmesh and can be walked to (the High Councilor in his audience hall too); every hall fades where it hides
    /// you; the Low Stair is shut; the walls stand where the data has them. The general tests (ZoneContentTests, BuildingGroundTests,
    /// ZoneExitTests, SecretPlacementTests, NodePlacementTests' stations) take the Crown in with every zone.
    /// </summary>
    public class CrownZonePlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.crown";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-crown-" + Guid.NewGuid().ToString("N"));
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

        /// <summary>
        /// The Crown's frame rate (2026-10-17, Chris: "still laggy"; the capture rig 64 fps at noon, 24.7 M triangles, its figures half
        /// the frame): seen from the Gilt Approach most of the city's small detail (ivy, leaves, flowers, iron, the weathering; cut into
        /// cells, DetailCull) is not drawn, none of it a shadow but the trim's; every figure past 99 m of the camera is not drawn and
        /// none past 34 m casts a shadow (VillageLife.FigureLod); the walk-in halls' detail, which fades with its walls, is left alone.
        /// </summary>
        [UnityTest] public IEnumerator Far_detail_and_far_figures_are_not_drawn()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.crown", zone.Zone.id);
            var cull = zone.GetComponent<DetailCull>(); Assert.IsNotNull(cull, "The Crown's detail has its culler.");
            Assert.Greater(cull.Count, 200, "The detail is cut into cells, each culled on its own.");
            var life = VillageLife.Active; Assert.IsNotNull(life);
            var cam = Camera.main; Assert.IsNotNull(cam);
            for (int i = 0; i < 20 + life.Villagers.Count / 12 * 2; i++) yield return null;
            Assert.GreaterOrEqual(cull.Off, cull.Count / 2, "From the Gilt Approach at least half the city's detail cells are not drawn (" + cull.Off + " of " + cull.Count + ").");
            var problems = new List<string>(); var eye = cam.transform.position;
            foreach (var r in zone.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.name.StartsWith("Detail ") || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off) continue;
                if (r.name.EndsWith(" leaves")) problems.Add(r.name + " casts a shadow");
            }
            int far = 0;
            foreach (var v in life.Villagers)
            {
                if (v == null || !v.isActiveAndEnabled) continue;
                float d = Vector3.Distance(v.transform.position, eye);
                if (d > VillageLife.FigureDrawReach + 4 && v.LodLevel != 2) problems.Add(v.Name + " is drawn " + d.ToString("0") + " m off");
                if (d > VillageLife.FigureDrawReach + 4) far++;
                if (d > VillageLife.FigureShadowReach + 4 && v.GetComponentsInChildren<Renderer>().Any(x => x.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off && !x.forceRenderingOff)) problems.Add(v.Name + " casts a shadow " + d.ToString("0") + " m off");
            }
            Assert.Greater(far, 10, "Most of the city's people are far from the gate.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [UnityTest] public IEnumerator The_ground_climbs_from_the_approach_to_the_Crown()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.crown", zone.Zone.id);
            float approach = zone.HeightAt(146, 10), city = zone.HeightAt(60, 10), square = zone.HeightAt(0, 80), below = zone.HeightAt(0, -80);
            Assert.That(approach, Is.EqualTo(0).Within(.2f), "The Gilt Approach.");
            Assert.That(city, Is.EqualTo(8).Within(.2f), "The lower city on its plateau.");
            Assert.That(square, Is.EqualTo(14).Within(.2f), "The Crown Square above it.");
            Assert.Less(below, approach - 15, "The Lowtowns far below the south wall.");
            Assert.That(zone.HeightAt(126, 10), Is.EqualTo(4).Within(.3f), "Half-way up the Gilt Ramp.");
            Assert.That(zone.HeightAt(0, 45), Is.EqualTo(11).Within(.3f), "Half-way up the Grand Stair.");
            var start = Start(zone); var problems = new List<string>();
            foreach (var (what, at) in new[] { ("the Gilt Gate's passage", new Vector2(108, 10)), ("the Processional Way", new Vector2(40, 10)), ("the Grand Stair's head", new Vector2(0, 60)),
                ("the Crown Square", new Vector2(0, 70)), ("Guild Row", new Vector2(-60, 20)), ("the Crafters' Court", new Vector2(88, 78)), ("the Stair Yard", new Vector2(-36, -16)) })
                if (!NavMesh.SamplePosition(zone.Ground(at), out var hit, 2, NavMesh.AllAreas) || !NavReach.Walkable(start, hit.position)) problems.Add(what + " at " + at + " can't be walked to from the Gilt Approach");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }

        [UnityTest] public IEnumerator Every_hall_can_be_walked_into_and_fades_where_it_hides_you()
        {
            var zone = ZoneBuilder.Active; var start = Start(zone); var problems = new List<string>();
            foreach (var p in zone.Zone.props.Where(p => p != null && (p.kind == "council_keep" || p.kind == "dome_hall" || p.kind == "archive" || p.kind == "mansion" || p.kind == "guild_hall" || p.kind == "inn")))
            {
                var t = zone.transform.Find("Zone props/" + p.name); if (t == null) { problems.Add(p.name + " is not built"); continue; }
                if (p.kind != "inn" && t.GetComponent<RoofFade>() == null) problems.Add(p.name + " does not fade where it hides you");
                // The floor a little in from the door (the middle of a hall may hold its lectern or table) and just inside it.
                var size = ZoneBuilder.CitySize(p); if (size.x <= 0) size = p.size;
                foreach (var (local, what) in new[] { (new Vector3(0, 0, -size.y / 2 + 1.6f), "inside the door"), (new Vector3(0, 0, -size.y / 2 + 3f), "in the hall") })
                {
                    var at = t.TransformPoint(local);
                    if (!NavMesh.SamplePosition(at, out var hit, 1, NavMesh.AllAreas) || Vector2.Distance(new Vector2(hit.position.x, hit.position.z), new Vector2(at.x, at.z)) > .8f) { problems.Add(p.name + ": no floor " + what); continue; }
                    if (!NavReach.Walkable(start, hit.position)) problems.Add(p.name + ": can't walk " + what + " from the Gilt Approach");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }

        [UnityTest] public IEnumerator Every_person_stands_at_their_post_and_can_be_reached()
        {
            yield return null;
            var zone = ZoneBuilder.Active; var start = Start(zone); var life = VillageLife.Active; Assert.IsNotNull(life, "The Crown's people are spawned.");
            var problems = new List<string>();
            foreach (var r in zone.Zone.life.residents)
            {
                var v = life.Find(r.name); if (v == null) { problems.Add(r.name + " is not spawned"); continue; }
                if (r.at != Vector2.zero && Vector2.Distance(new Vector2(v.transform.position.x, v.transform.position.z), r.at) > 2) problems.Add(r.name + " stands " + v.transform.position + ", not at the post " + r.at);
                if (!NavMesh.SamplePosition(v.transform.position, out var hit, 2, NavMesh.AllAreas)) { problems.Add(r.name + " stands off the navmesh"); continue; }
                if (!NavReach.Walkable(start, hit.position)) problems.Add(r.name + " (" + r.role + ") can't be walked to from the Gilt Approach (stands " + v.transform.position.ToString("F1") + ", navmesh " + hit.position.ToString("F1") + ", doing " + v.name + ")");
            }
            var aethelgard = life.Find("High Councilor Aethelgard"); Assert.IsNotNull(aethelgard, "The High Councilor receives in the keep.");
            Assert.IsFalse(aethelgard.GetComponent<Collider>() != null && aethelgard.GetComponent<EncounterEnemy>() != null, "He is no enemy: visited, never fought.");
            Assert.IsFalse(zone.GetComponentsInChildren<EncounterEnemy>(true).Any(), "No enemies in the Crown.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [UnityTest] public IEnumerator The_Low_Stair_is_shut_and_the_walls_are_whole()
        {
            var zone = ZoneBuilder.Active;
            var gate = zone.transform.Find("Zone static scenery/The Low Stair") ?? zone.transform.Find("Zone props/The Low Stair");
            Assert.IsNotNull(gate, "The Low Stair is built.");
            Assert.IsTrue(gate.GetComponentsInChildren<BoxCollider>().Any(c => c.GetComponent<NavBlocker>() != null && c.transform.localPosition.z < 0 && c.size.x >= 5), "Its doors are barred across the passage.");
            // Nobody walks out of the city but by the Gilt Gate: the plateau's edge behind the south wall has no way down to the Lowtowns.
            var start = Start(zone);
            Assert.IsFalse(NavMesh.SamplePosition(zone.Ground(new Vector2(-36, -40)), out var below, 3, NavMesh.AllAreas) && NavReach.Walkable(start, below.position), "No way past the Low Stair.");
            var walls = zone.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Blocker").ToList();
            Assert.Greater(walls.Count, 150, "The curtain wall, its towers and the parapets stand in stretches, each solid.");
            yield return null;
        }
        /// <summary>
        /// The Crown's surface detail (ZoneBuilder.CrownDetail; Chris, 2026-10-16: "so it is not so white . maybe add some iron"; "we need a
        /// real garden"): ivy, ironwork and weathering on the Gilt Gate, the stair and the walls, iron on the houses, halls and banners, the
        /// gardens planted; ivy in clumps, not everywhere; the lot cheap (its triangles counted as built, estimated some 200k against the
        /// 7.46 M the Crown drew a frame before it); and none of it solid: no collider under any "Detail" part or in a garden, and the
        /// gate's passage, the ramp, the stair and the Way still walked.
        /// </summary>
        [UnityTest] public IEnumerator The_walls_gate_and_stair_wear_ivy_iron_and_weather_and_none_of_it_blocks_a_way()
        {
            var zone = ZoneBuilder.Active; var counts = zone.CrownDetail; var problems = new List<string>();
            int Get(string k) { return counts.TryGetValue(k, out var v) ? v : 0; }
            foreach (var key in new[] { "gate:ivy clumps", "gate:iron", "gate:weathering", "gate:braziers", "stair:moss", "wall:ivy clumps", "wall:weathering", "wall:iron", "wall:trim", "wall:lanterns",
                "house:iron", "house:lanterns", "house:weathering", "hall:lanterns", "banner:iron", "garden:plants", "garden:arches", "garden:benches", "garden:urns", "garden:topiary" })
                if (Get(key) <= 0) problems.Add("nothing counted for " + key);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.That(Get("ivy clumps"), Is.InRange(30, 2000), "Ivy in clumps here and there, not on everything.");
            // Raised 2026-10-16 with the ivy made dense mats (Chris: "only a few plants"); the city drew 7.46 M a frame before the detail:
            // a million more is the ceiling (the frame rate is the real check: crown-perf.txt in the capture).
            // 2026-10-17 (Chris: "still laggy"; 24.7 M triangles a frame): what is built stays under these, but far detail is no longer
            // drawn (DetailCull): Far_detail_and_far_figures_are_not_drawn holds the drawn share to half or less.
            Assert.Less(Get("leaf cards"), 800000, "Leaf cards within bounds.");
            Assert.Less(Get("triangles"), 3000000, "The detail stays cheap.");
            foreach (var col in zone.GetComponentsInChildren<Collider>(true))
                for (var p = col.transform; p != null && p != zone.transform; p = p.parent)
                    if (p.name.StartsWith("Detail ")) { problems.Add(col.name + " is solid under " + p.name); break; }
            foreach (var g in zone.Zone.props.Where(p => p != null && p.kind == "parterre"))
            {
                var t = zone.transform.Find("Zone static scenery/" + g.name); if (t == null) { problems.Add(g.name + " is not built"); continue; }
                if (t.GetComponentsInChildren<Collider>(true).Length > 0) problems.Add(g.name + " has a collider: a garden is walked through");
            }
            var start = Start(zone);
            foreach (var (what, at) in new[] { ("the Gilt Gate's passage", new Vector2(108, 10)), ("the Gilt Ramp", new Vector2(120, 10)), ("before the gate's braziers", new Vector2(115.5f, 10)),
                ("the Grand Stair", new Vector2(0, 45)), ("the Grand Stair by its handrail", new Vector2(5.6f, 45)), ("the Processional Way", new Vector2(60, 10)) })
                if (!NavMesh.SamplePosition(zone.Ground(at), out var hit, 1.5f, NavMesh.AllAreas) || !NavReach.Walkable(start, hit.position)) problems.Add(what + " at " + at + " can't be walked to");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }
    }
}
#endif
