#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The Crown's halls stood on and the High Councilor on his throne (2026-10-16, Chris in the audience hall: "falling through the
    /// floor. he needs more regal attire and sitting on a throne"; Docs/CAPITAL_HUB.md). Every hall's floor is solid where you walk:
    /// a ray down over a grid inside each hall finds the hall's own floor (not only the open ground under it) within 0.3 m of the
    /// floor's height, on ground levelled to the hall's foot; the dais's steps and top are solid at their heights. Aethelgard sits on
    /// the throne facing the hall in his robes of state, stays sat when spoken to, and is in talk reach from the foot of the dais.
    /// </summary>
    public class CrownThronePlayTests
    {
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = "zone.crown";
            root = Path.Combine(Path.GetTempPath(), "Crulanda-throne-" + Guid.NewGuid().ToString("N"));
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
        static readonly string[] HallKinds = { "council_keep", "dome_hall", "archive", "mansion", "guild_hall", "inn" };
        static Transform Built(ZoneBuilder zone, string name) { return zone.transform.Find("Zone props/" + name) ?? zone.transform.Find("Zone static scenery/" + name); }
        /// <summary>The heights of everything solid a ray down meets at a hall's local point (x, z), from 2.5 m over its floor.</summary>
        static RaycastHit[] Down(Transform hall, float x, float z) { return Physics.RaycastAll(hall.TransformPoint(new Vector3(x, 2.5f, z)), Vector3.down, 4, ~0, QueryTriggerInteraction.Ignore); }

        [UnityTest] public IEnumerator Every_hall_floor_is_solid_at_its_height_everywhere_inside()
        {
            var zone = ZoneBuilder.Active; Assert.AreEqual("zone.crown", zone.Zone.id);
            var ground = zone.GroundMesh.GetComponent<Collider>(); var problems = new List<string>();
            foreach (var p in zone.Zone.props.Where(p => p != null && HallKinds.Contains(p.kind)))
            {
                var t = Built(zone, p.name); if (t == null) { problems.Add(p.name + " is not built"); continue; }
                var size = ZoneBuilder.CitySize(p); if (size.x <= 0) size = p.size;
                float floor = t.position.y + .05f; int bare = 0, sunk = 0, cells = 0; string first = null;
                // A grid of points 0.7 m in from the walls, about every metre.
                float hx = size.x / 2 - .7f, hz = size.y / 2 - .7f; int nx = Mathf.CeilToInt(hx * 2), nz = Mathf.CeilToInt(hz * 2);
                for (int i = 0; i <= nx; i++)
                    for (int k = 0; k <= nz; k++)
                    {
                        float x = -hx + i * hx * 2 / nx, z = -hz + k * hz * 2 / nz; cells++;
                        var at = t.TransformPoint(new Vector3(x, 0, z));
                        // The ground under the hall levelled to its foot (a terrace's edge or a slope under it would leave the floor in the air).
                        if (Mathf.Abs(zone.HeightAt(at.x, at.z) - t.position.y) > .3f) { sunk++; first = first ?? "the ground is at " + zone.HeightAt(at.x, at.z).ToString("0.00") + " under (" + x.ToString("0.0") + ", " + z.ToString("0.0") + ")"; }
                        // The hall's own floor there: something solid other than the open ground, at the floor's height.
                        if (!Down(t, x, z).Any(h => h.collider != ground && Mathf.Abs(h.point.y - floor) <= .3f)) { bare++; first = first ?? "no floor of its own at (" + x.ToString("0.0") + ", " + z.ToString("0.0") + ")"; }
                    }
                if (bare > 0 || sunk > 0) problems.Add(p.name + ": " + bare + " of " + cells + " points with no solid floor, " + sunk + " over ground not levelled to it (first: " + first + ")");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }

        [UnityTest] public IEnumerator The_dais_steps_and_top_are_solid_and_walked()
        {
            var zone = ZoneBuilder.Active; var keepProp = zone.Zone.props.First(p => p.kind == "council_keep"); var keep = Built(zone, keepProp.name); Assert.IsNotNull(keep, "The keep is built.");
            float d = ZoneBuilder.CitySize(keepProp).y, front = ZoneBuilder.DaisFront(d), back = d / 2 - .25f; var seat = ZoneBuilder.ThroneSeat(d); var problems = new List<string>();
            for (int k = 0; k < ZoneBuilder.DaisSteps; k++)
            {
                float z = front + k * ZoneBuilder.DaisTread + ZoneBuilder.DaisTread / 2, tread = keep.position.y + (k + 1) * ZoneBuilder.DaisRise;
                if (k == ZoneBuilder.DaisSteps - 1) z = front + k * ZoneBuilder.DaisTread + .6f;
                foreach (float x in new[] { -4.5f, -2f, 0f, 2f, 4.5f })
                {
                    var hits = Down(keep, x, z); float top = hits.Length > 0 ? hits.Max(h => h.point.y) : float.MinValue;
                    if (Mathf.Abs(top - tread) > .06f) problems.Add("step " + (k + 1) + " at x " + x + ": solid at " + top.ToString("0.00") + ", its tread at " + tread.ToString("0.00"));
                }
            }
            // The top, clear of the throne, out to the back.
            for (float z = front + ZoneBuilder.DaisTread * (ZoneBuilder.DaisSteps - 1) + .3f; z < back - .4f; z += .5f)
                foreach (float x in new[] { -4.6f, -3f, -1.6f, 1.6f, 3f, 4.6f })
                {
                    var hits = Down(keep, x, z); float top = hits.Length > 0 ? hits.Max(h => h.point.y) : float.MinValue, want = keep.position.y + ZoneBuilder.DaisTop;
                    if (Mathf.Abs(top - want) > .06f && top < want) problems.Add("the dais's top at (" + x + ", " + z.ToString("0.0") + "): solid at " + top.ToString("0.00") + ", not " + want.ToString("0.00"));
                }
            // Walked on: on the navmesh, and the throne's seat (his post) too.
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(keep.TransformPoint(seat), out var hit, .4f, UnityEngine.AI.NavMesh.AllAreas), "The dais's top under the throne's seat is in the navmesh (his post).");
            Assert.That(hit.position.y, Is.EqualTo(keep.position.y + ZoneBuilder.DaisTop).Within(.15f), "On the dais's top, not the floor under it.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
            yield return null;
        }

        [UnityTest] public IEnumerator Aethelgard_sits_his_throne_in_robes_of_state_and_is_spoken_to_from_the_foot_of_the_dais()
        {
            for (int i = 0; i < 10; i++) yield return null;
            var zone = ZoneBuilder.Active; var life = VillageLife.Active; Assert.IsNotNull(life, "The Crown's people are spawned.");
            var keepProp = zone.Zone.props.First(p => p.kind == "council_keep"); var keep = Built(zone, keepProp.name); float d = ZoneBuilder.CitySize(keepProp).y;
            var him = life.Find("High Councilor Aethelgard"); Assert.IsNotNull(him, "The High Councilor receives in the keep.");
            var look = him.GetComponent<ActorVisual>(); Assert.IsNotNull(look);
            Assert.IsTrue(him.Seated, "A seated post."); Assert.AreEqual(ActorPose.Sit, look.Pose, "He sits.");
            var seat = keep.TransformPoint(ZoneBuilder.ThroneSeat(d)); var at = him.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(at.x, at.z), new Vector2(seat.x, seat.z)), .35f, "On the throne's seat (" + seat + "), not beside it: he is at " + at + ".");
            Assert.Less(Vector3.Dot(him.transform.forward, keep.forward), -.95f, "Facing the hall and its door.");
            // His robes of state (ActorVisual.Dress "councilor"): the robe, the collar, the chain of office and the rings are worn, model or not.
            var worn = him.GetComponentsInChildren<Transform>(false).Select(c => c.name).ToList();
            foreach (var part in new[] { "Robe", "Collar", "Chain of office", "Medallion", "Ring" }) Assert.Contains(part, worn, "He wears his " + part.ToLowerInvariant() + ".");
            Assert.IsFalse(worn.Any(n => n.Contains("Crown")), "No crown: a councillor, not a king.");
            // Spoken to, he talks from his seat: never stands, never turns from the hall.
            him.Hold(6); for (int i = 0; i < 20; i++) yield return null;
            Assert.AreEqual(ActorPose.Sit, look.Pose, "Spoken to, he stays seated.");
            Assert.Less(Vector3.Dot(him.transform.forward, keep.forward), -.95f, "Spoken to, he still faces the hall.");
            // The foot of the dais (you there, 0.45 m in front of its first step on the floor) is in talk reach, with nothing solid between.
            var you = keep.TransformPoint(new Vector3(0, .05f + 1, ZoneBuilder.DaisFront(d) - .45f));
            Assert.Less(Vector3.Distance(you, at), EncounterSession.TalkRange - .1f, "In talk reach from the foot of the dais.");
            Vector3 eye = you + Vector3.up * .55f, head = at + Vector3.up * .55f;
            var between = Physics.RaycastAll(eye, (head - eye).normalized, (head - eye).magnitude, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.GetComponentInParent<Villager>() == null && h.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null).Select(h => h.collider.name).ToList();
            Assert.IsEmpty(between, "Nothing solid between the foot of the dais and his head.");
            // His clerk in a scholar's dark robe, at his side.
            var clerk = life.Find("Clerk Odo Vantry"); Assert.IsNotNull(clerk);
            Assert.Contains("Ledger", clerk.GetComponentsInChildren<Transform>(false).Select(c => c.name).ToList(), "The clerk carries his ledger.");
            Assert.IsFalse(clerk.Seated, "The clerk stands.");
        }
    }
}
#endif
