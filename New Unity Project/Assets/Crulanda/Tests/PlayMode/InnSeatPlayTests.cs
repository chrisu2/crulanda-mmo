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
    /// Everyone sat in an inn sits on a seat (play note 237, 2026-10-18, Chris: "inn still not polished. this guy not even sitting on
    /// a chair"; the Golden Cask's drinker sat in the air at the table's end, his stool behind him). For every inn of every zone that
    /// has one (the Golden Cask, the Cracked Hearth, Pilgrims' Rest, Wain's Rest, the Hearth-Tree's hearth-house, the Rusty Spoon,
    /// the Gilded Lantern):
    /// - every seat the room registers (the tables' seats, the bar's stools, the bench by the hearth) has a seat built there: a seat
    ///   mark (ZoneBuilder.SeatMark, one per stool, chair, keg or stump, along a bench) within 0.25 m of it along the floor, its top
    ///   within 0.1 m of the seat's registered top (what the sitter is lifted to);
    /// - a villager put at each seat as the inn's drinkers are (PoseAtInn) sits with the hips (the model's pelvis, measured) within
    ///   0.25 m of a seat mark along the floor, facing the seat's table, bar or fire within 25 degrees, and at the same height over
    ///   the seat as every other sitter (within 0.1 m of their median, which is between 0 and 0.3 m: the hip joint over the seat).
    /// Before the fix the hips stood 0.4 m in front of every seat (the pose's fixed 0.42 m shift forward), and a bar stool's sitter
    /// sat 0.17 m lower on it than a table's.
    /// </summary>
    public class InnSeatPlayTests
    {
        static readonly string[] Zones = { "zone.oakhaven", "zone.khaven", "zone.peaks", "zone.ashrim", "zone.verdant", "zone.lowtowns", "zone.crown" };
        string root;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        IEnumerator Open(string zoneId)
        {
            WorldClock.Hour = 11; ZoneBuilder.RequestedZoneId = zoneId;
            root = Path.Combine(Path.GetTempPath(), "Crulanda-innseat-" + Guid.NewGuid().ToString("N"));
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
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }
        static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

        [UnityTest] public IEnumerator Every_sitter_in_every_inn_sits_on_a_seat([ValueSource(nameof(Zones))] string zoneId)
        {
            yield return Open(zoneId);
            var zone = ZoneBuilder.Active; Assert.AreEqual(zoneId, zone.Zone.id);
            Assert.IsNotEmpty(zone.Inns, zoneId + " has a walk-in inn.");
            var marks = zone.GetComponentsInChildren<Transform>(true).Where(t => t.name == ZoneBuilder.SeatMarkName).Select(t => t.position).ToList();
            var problems = new List<string>();
            // The seats as registered, each with a seat built under it at its top.
            var seats = new List<(string inn, string kind, InnSeat seat)>();
            foreach (var inn in zone.Inns)
                foreach (var (kind, list) in new[] { ("table", inn.seats), ("bar", inn.bar), ("bench", inn.bench) })
                    foreach (var s in list) seats.Add((inn.name, kind, s));
            foreach (var (inn, kind, s) in seats)
            {
                var near = marks.Where(m => Flat(m, s.at) < .25f).ToList();
                if (near.Count == 0) { problems.Add(inn + ", " + kind + " seat at " + s.at.ToString("0.00") + ": nothing to sit on within 0.25 m (nearest " + (marks.Count > 0 ? marks.Min(m => Flat(m, s.at)).ToString("0.00") : "none") + " m)"); continue; }
                float top = near.OrderBy(m => Flat(m, s.at)).First().y - s.at.y;
                if (s.top <= 0 || Mathf.Abs(top - s.top) > .1f) problems.Add(inn + ", " + kind + " seat at " + s.at.ToString("0.00") + ": its seat's top is " + top.ToString("0.00") + " m, the sitter is lifted for " + s.top.ToString("0.00"));
            }
            // Then a villager sat at each, as the drinkers are, the hips measured.
            var life = VillageLife.Active; Assert.NotNull(life, zoneId + " has village life.");
            var places = life.Places["inn"];
            Assert.IsNotEmpty(places, zoneId + ": the inn's seats are places to sit.");
            var sitters = life.Villagers.Where(v => v != null && v.Visible && !v.Resident && v.GetComponent<ActorVisual>() != null && v.GetComponent<ActorVisual>().Model != null).Take(4).ToList();
            // A zone with too few folk of its own to sit (the Ash Rim's Wain's Rest, the Hearth-Tree's Keepers): sitters made for the test.
            var made = new List<Villager>();
            for (int k = sitters.Count; k < 4; k++) { var v = Villager.Spawn(life, "Seat tester " + k, "drinker", 900 + k, null, places[0]); made.Add(v); if (v.GetComponent<ActorVisual>().Model != null) sitters.Add(v); }
            if (sitters.Count == 0) { foreach (var v in made) UnityEngine.Object.Destroy(v.gameObject); Assert.Inconclusive(zoneId + ": no modelled figure to sit (models off)"); yield break; }
            foreach (var v in sitters) v.GetComponent<ActorVisual>().Model.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var heights = new List<(string what, float h)>();
            Assert.AreEqual(seats.Count, places.Count, zoneId + ": every registered seat is a place to sit (on the navmesh).");
            for (int from = 0; from < places.Count; from += sitters.Count)
            {
                int n = Mathf.Min(sitters.Count, places.Count - from);
                for (int k = 0; k < n; k++) Assert.IsTrue(sitters[k].PoseAtInn(from + k, false), "Seated at place " + (from + k));
                yield return new WaitForSeconds(1.2f);
                for (int k = 0; k < n; k++)
                {
                    var v = sitters[k]; var look = v.GetComponent<ActorVisual>(); var place = places[from + k];
                    var reg = seats.OrderBy(s => Flat(s.seat.at, place)).First(); string what = reg.inn + ", " + reg.kind + " seat at " + reg.seat.at.ToString("0.00");
                    var hips = look.HipsPoint.Value;
                    var mark = marks.Count > 0 ? marks.OrderBy(m => Flat(m, hips)).First() : hips;
                    float off = Flat(mark, hips);
                    if (off > .25f) problems.Add(what + ": the hips are " + off.ToString("0.00") + " m from the nearest seat (sat " + Flat(v.transform.position, reg.seat.at).ToString("0.00") + " m from the place)");
                    else heights.Add((what, hips.y - mark.y));
                    var face = reg.seat.face - reg.seat.at; face.y = 0; var fwd = v.transform.forward; fwd.y = 0;
                    if (face.sqrMagnitude > .01f && Vector3.Angle(face, fwd) > 25) problems.Add(what + ": faces " + Vector3.Angle(face, fwd).ToString("0") + " degrees off its " + (reg.kind == "table" ? "table" : reg.kind == "bar" ? "bar" : "fire"));
                }
            }
            if (heights.Count > 0)
            {
                var sorted = heights.Select(h => h.h).OrderBy(h => h).ToList(); float median = sorted[sorted.Count / 2];
                Debug.Log(zoneId + ": hips over the seat's top, median " + median.ToString("0.000") + " m, " + sorted.First().ToString("0.000") + " to " + sorted.Last().ToString("0.000"));
                if (median < 0 || median > .3f) problems.Add("The hips sit " + median.ToString("0.00") + " m over the seats' tops (median): not on them (0 to 0.3 m)");
                foreach (var (what, h) in heights) if (Mathf.Abs(h - median) > .1f) problems.Add(what + ": the hips " + h.ToString("0.00") + " m over the seat, the others " + median.ToString("0.00") + " (sunk into it or over it)");
            }
            for (int k = 0; k < sitters.Count; k++) if (!made.Contains(sitters[k])) sitters[k].Release(sitters[k].transform.position);
            foreach (var v in made) UnityEngine.Object.Destroy(v.gameObject);
            Assert.IsEmpty(problems, zoneId + ":\n" + string.Join("\n", problems));
        }
    }
}
#endif
