using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The dungeons' checkpoints in the data (Chris, 2026-10-16: "maybe 3 checkpoints in dungeons"; ZoneDefinition.checkpoints): every dungeon
    /// zone has exactly three and no other zone has any; each has a name of its own and a look (a cairn, a lamp); one in a cave names a cave
    /// of the zone and stands inside its walls; each stands clear of the camps (a released camp's too), the patrols' roads, the secrets, the
    /// pools and the creek, off a road's middle, out of the chasm; and they come further from the zone's start in their order. Pure data:
    /// the caves are drawn on flat land for where things stand on the ground plane. Every offender is listed, not only the first.
    /// </summary>
    public class CheckpointDataTests
    {
        const float FromCamp = 10, FromRoute = 5, FromSecret = 6, OffRoadMiddle = 3, FromWater = 2;
        static string Content { get { return Path.Combine(Application.dataPath, "Crulanda/EncounterContent"); } }
        static IEnumerable<(string file, ZoneDefinition zone)> Zones()
        {
            foreach (var f in Directory.GetFiles(Path.Combine(Content, "Zones"), "*.json").OrderBy(x => x))
            {
                var z = JsonUtility.FromJson<ZoneDefinition>(File.ReadAllText(f)); if (z != null) yield return (Path.GetFileNameWithoutExtension(f), z);
            }
        }
        static Dictionary<string, Hollow> Caves(ZoneDefinition zone)
        {
            var caves = new Dictionary<string, Hollow>();
            foreach (var p in zone.props)
            {
                if (p == null || p.kind != "cavern" || string.IsNullOrEmpty(p.name) || p.plan == null || p.plan.Length < 10 || caves.ContainsKey(p.name)) continue;
                var rows = new List<float[]>(); for (int i = 0; i + 4 < p.plan.Length; i += 5) rows.Add(new[] { p.plan[i], p.plan[i + 1], p.plan[i + 2], p.plan[i + 3], p.plan[i + 4] });
                caves[p.name] = new Hollow(p.name, p.at, p.rotation, rows, (x, z) => 0f);
            }
            return caves;
        }
        static bool Dungeon(ZoneDefinition z) { return z.dungeon || z.wings != null && z.wings.Length > 0; }
        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = ab.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
        static Vector2 Flat(Vector3 v) { return new Vector2(v.x, v.z); }
        /// <summary>Where a checkpoint stands on the ground plane (in its cave, or at its <c>at</c>), and the cave's half-width there.</summary>
        static Vector2 Where(ZoneCheckpoint c, Dictionary<string, Hollow> caves, out Hollow cave, out float half)
        {
            half = 0; cave = null;
            if (string.IsNullOrEmpty(c.cave)) return c.at;
            if (!caves.TryGetValue(c.cave, out cave)) return c.at;
            int i = 0; while (i + 1 < cave.Along.Count && cave.Along[i + 1] < c.along) i++;
            half = cave.Half[i];
            return Flat(cave.At(c.along, c.aside));
        }

        [Test] public void Every_dungeon_has_three_checkpoints_and_no_other_zone_has_any()
        {
            var bad = new List<string>(); int dungeons = 0;
            foreach (var (file, z) in Zones())
            {
                int n = z.checkpoints == null ? 0 : z.checkpoints.Count(c => c != null);
                if (Dungeon(z)) { dungeons++; if (n != 3) bad.Add(file + ": " + n + " checkpoints (a dungeon has three)"); }
                else if (n != 0) bad.Add(file + ": " + n + " checkpoints in a zone that is not a dungeon");
                if (z.checkpoints == null) continue;
                var names = new HashSet<string>();
                foreach (var c in z.checkpoints)
                {
                    if (c == null) continue;
                    if (string.IsNullOrEmpty(c.name) || !names.Add(c.name)) bad.Add(file + ": a checkpoint without a name of its own (" + c.name + ")");
                    if (c.look != "cairn" && c.look != "lamp") bad.Add(file + ": " + c.name + " has the look '" + c.look + "' (a cairn or a lamp)");
                }
            }
            Assert.GreaterOrEqual(dungeons, 2, "the Sealed Adit and Hallow's Creek at the least");
            Assert.IsEmpty(bad, string.Join("\n  ", bad));
        }

        [Test] public void Each_checkpoint_stands_clear_of_camps_patrols_secrets_water_and_the_road_middle()
        {
            var bad = new List<string>();
            foreach (var (file, z) in Zones())
            {
                if (z.checkpoints == null || z.checkpoints.Length == 0) continue;
                var caves = Caves(z); var start = z.spawns.player; float last = -1;
                foreach (var c in z.checkpoints)
                {
                    if (c == null) continue; string who = file + ": " + c.name;
                    var p = Where(c, caves, out var cave, out float half);
                    if (!string.IsNullOrEmpty(c.cave))
                    {
                        if (cave == null) { bad.Add(who + " names the cave '" + c.cave + "' the zone has not got"); continue; }
                        if (c.along <= 1 || c.along >= cave.Length - 1) bad.Add(who + " is " + c.along + " m into a cave " + cave.Length.ToString("0") + " m long");
                        if (Mathf.Abs(c.aside) > half * .8f) bad.Add(who + " stands " + c.aside + " m aside where the cave's half-width is " + half.ToString("0.0") + " (in the wall)");
                    }
                    else if (c.at == Vector2.zero) bad.Add(who + " has no place");
                    foreach (var cp in z.camps)
                    {
                        if (cp == null) continue;
                        float d = Vector2.Distance(p, cp.center); if (d < FromCamp) bad.Add(who + " is " + d.ToString("0.0") + " m from the camp " + cp.name);
                        if (cp.route != null) for (int k = 0; k + 1 < cp.route.Length; k++) { float r = Seg(p, cp.route[k], cp.route[k + 1]); if (r < FromRoute) { bad.Add(who + " is " + r.ToString("0.0") + " m from " + cp.name + "'s round"); break; } }
                    }
                    foreach (var s in z.secrets) if (s != null) { float d = Vector2.Distance(p, s.at); if (d < FromSecret) bad.Add(who + " is " + d.ToString("0.0") + " m from the secret " + s.name); }
                    if (z.caveWater != null)
                        foreach (var w in z.caveWater)
                        {
                            if (w == null || !caves.TryGetValue(w.cave ?? "", out var wc) || w.flood) continue;
                            float lo = Mathf.Min(w.from, w.to), hi = Mathf.Max(w.from, w.to), reach = w.radius > 0 ? w.radius : w.half;
                            for (float s = lo; s <= hi + 1.99f; s += 2)
                            {
                                float d = Vector2.Distance(p, Flat(wc.At(Mathf.Min(s, hi), w.aside)));
                                if (d < reach + FromWater) { bad.Add(who + " is in or at the edge of " + w.name); break; }
                            }
                        }
                    if (string.IsNullOrEmpty(c.cave))
                    {
                        foreach (var r in z.roads) if (r != null) for (int k = 0; k + 1 < r.points.Length; k++) { float d = Seg(p, r.points[k], r.points[k + 1]); if (d < OffRoadMiddle) { bad.Add(who + " stands " + d.ToString("0.0") + " m from the middle of " + r.name); break; } }
                        foreach (var w in z.water) if (w != null) for (int k = 0; k + 1 < w.points.Length; k++) { float d = Seg(p, w.points[k], w.points[k + 1]); if (d < w.width / 2 + FromWater) { bad.Add(who + " is in or at the edge of " + w.name); break; } }
                        if (z.pits != null) foreach (var pit in z.pits) if (pit != null && Mathf.Abs(p.x - pit.center.x) < pit.size.x / 2 + 3 && Mathf.Abs(p.y - pit.center.y) < pit.size.y / 2 + 3) bad.Add(who + " is over " + pit.name);
                    }
                    // In their order, each further from where the zone is entered (the first a quarter or so of the way in, the last near the end).
                    float far = Vector2.Distance(p, start);
                    if (far <= last) bad.Add(who + " is nearer the zone's start than the checkpoint before it (" + far.ToString("0") + " m against " + last.ToString("0") + ")");
                    if (far < 60) bad.Add(who + " is only " + far.ToString("0") + " m from the zone's start");
                    last = far;
                }
            }
            Assert.IsEmpty(bad, bad.Count + " checkpoints out of place:\n  " + string.Join("\n  ", bad));
        }
    }
}
