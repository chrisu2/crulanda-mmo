#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>Where the reworked Adit's navmesh is and is not (its first run, ADIT_REWORK.md 16: every camp nobody can walk to has its
    /// nearest reachable point a floor above or below it, so the Geode Foundry's floor and the decks seem to carry no navmesh). For
    /// each cave: its rows Hollow marks as giving way, and the share of floor samples (every 2 m along, three abreast) the navmesh
    /// covers; for each deck: whether it was built and the share of its planks on the navmesh. A report in the log and in
    /// D:/crulanda-work/encounter-validation/adit-nav-diagnostics.txt; it never fails.</summary>
    public class AditNavDiagnostics
    {
        string root;
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-aditdiag-" + Guid.NewGuid().ToString("N"));
            ZoneBuilder.RequestedZoneId = "zone.adit";
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            SceneManager.sceneLoaded -= OnLoaded; yield return null;
        }
        void OnLoaded(Scene scene, LoadSceneMode mode) { UnityEngine.Object.FindFirstObjectByType<EncounterSession>().SaveDirectoryOverride = root; }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ZoneBuilder.RequestedZoneId = null;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        static bool OnMesh(Vector3 p, float reach) { return NavMesh.SamplePosition(p, out var hit, reach, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - p.y) < reach; }

        [UnityTest] public IEnumerator Where_the_navmesh_is()
        {
            var zone = ZoneBuilder.Active; Assert.NotNull(zone); Assert.AreEqual("zone.adit", zone.Zone.id);
            yield return null;
            var sb = new StringBuilder();
            sb.AppendLine("cave | rows | giving way | floor samples on navmesh | parent (mouth rise) | join (rise)");
            foreach (var h in Hollow.All)
            {
                h.ComputeFloored();
                int rows = h.Centre.Count, unfloored = 0; for (int i = 0; i < rows; i++) if (!h.Floored(i)) unfloored++;
                int on = 0, total = 0, noFloor = 0;
                for (float along = 1; along < h.Length; along += 2)
                {
                    int i = Mathf.Clamp(Mathf.RoundToInt(along / Mathf.Max(.01f, h.Length) * (rows - 1)), 0, rows - 1);
                    foreach (var k in new[] { -.5f, 0, .5f })
                    {
                        var p = h.At(along, h.Half[i] * k); var flat = new Vector2(p.x, p.z);
                        if (!h.FloorAt(flat, out float y)) { noFloor++; continue; }
                        total++; if (OnMesh(new Vector3(p.x, y + .2f, p.z), 1.2f)) on++;
                    }
                }
                sb.AppendLine(h.Name + " | " + rows + " | " + unfloored + " | " + on + "/" + total + (noFloor > 0 ? " (" + noFloor + " with no floor)" : "") + " | "
                    + (h.Parent != null ? h.Parent.Name + " (" + h.MouthRise.ToString("0.0") + ")" : "-") + " | " + (h.Join != null ? h.Join.Name + " (" + h.JoinRise.ToString("0.0") + ")" : "-"));
            }
            sb.AppendLine(); sb.AppendLine("deck | cave | built | planks on navmesh | rise | high");
            foreach (var d in zone.DeckSpots)
            {
                int on = 0; for (int k = 0; k < d.mid.Length; k++) if (OnMesh(d.mid[k] + Vector3.up * .2f, 1f)) on++;
                sb.AppendLine((d.def != null ? d.def.name : "?") + " | " + (d.cave != null ? d.cave.Name : "-") + " | " + (d.root != null) + " | " + on + "/" + d.mid.Length + " | " + (d.def != null ? d.def.rise.ToString("0.0") : "?") + " | " + d.high.ToString("0.0"));
            }
            // The links (the breaks are here: every floor and deck carries mesh, 16:57): from each deck's middle down to its cave's floor
            // under its first and last plank, and from each joining cave's end to its target's floor at the join.
            sb.AppendLine(); sb.AppendLine("deck | end | plank end y | floor there | path from the deck to the floor there");
            foreach (var d in zone.DeckSpots)
            {
                if (d.mid == null || d.mid.Length == 0 || d.cave == null) continue;
                var middle = d.mid[d.mid.Length / 2] + Vector3.up * .2f;
                foreach (var (end, k) in new[] { ("first", 0), ("last", d.mid.Length - 1) })
                {
                    var e = d.mid[k]; float fy = 0; bool floor = Hollow.FloorUnder(new Vector2(e.x, e.z), ref fy);
                    string link = "no floor";
                    if (floor) { var path = new NavMeshPath(); bool ok = NavMesh.CalculatePath(middle, new Vector3(e.x, fy + .2f, e.z), NavMesh.AllAreas, path); link = ok && path.status == NavMeshPathStatus.PathComplete ? "complete" : ok ? path.status.ToString() : "none"; }
                    sb.AppendLine((d.def != null ? d.def.name : "?") + " | " + end + " | " + e.y.ToString("0.0") + " | " + (floor ? fy.ToString("0.0") : "-") + " | " + link);
                }
            }
            // Each deck end to end (its own planks joined?), and each pair of deck ends within 2 m of each other (the junction joined?).
            sb.AppendLine(); sb.AppendLine("deck | first end to last end | ends on mesh");
            string Path(Vector3 a, Vector3 b) { var p = new NavMeshPath(); bool ok = NavMesh.CalculatePath(a, b, NavMesh.AllAreas, p); return ok && p.status == NavMeshPathStatus.PathComplete ? "complete" : ok ? p.status.ToString() : "none"; }
            foreach (var d in zone.DeckSpots)
            {
                if (d.mid == null || d.mid.Length < 2) continue;
                var a = d.mid[0] + Vector3.up * .2f; var b = d.mid[d.mid.Length - 1] + Vector3.up * .2f;
                sb.AppendLine((d.def != null ? d.def.name : "?") + " | " + Path(a, b) + " | " + OnMesh(a, 1f) + "/" + OnMesh(b, 1f));
            }
            // Along each deck, every metre: where the mesh is missing over the planks, and what stands there (the colliders within a
            // box 1 m wide and 2 m high over the plank: a wall, a pillar, a post, a crate).
            sb.AppendLine(); sb.AppendLine("deck | samples off the mesh | first gap at | what stands there");
            foreach (var d in zone.DeckSpots)
            {
                if (d.mid == null || d.mid.Length < 2) continue;
                int off = 0; Vector3? first = null; string what = "";
                for (int k = 0; k + 1 < d.mid.Length; k++)
                {
                    float seg = Vector3.Distance(d.mid[k], d.mid[k + 1]); int steps = Mathf.Max(1, Mathf.CeilToInt(seg));
                    for (int s = 0; s < steps; s++)
                    {
                        var p = Vector3.Lerp(d.mid[k], d.mid[k + 1], s / (float)steps);
                        if (OnMesh(p + Vector3.up * .2f, .6f)) continue;
                        off++;
                        if (first == null)
                        {
                            first = p;
                            var hits = Physics.OverlapBox(p + Vector3.up * 1.1f, new Vector3(.5f, 1, .5f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                            what = string.Join("; ", hits.Where(h => h != null && (d.root == null || !h.transform.IsChildOf(d.root))).Select(h => h.transform.root.name + "/" + h.name).Distinct().Take(6));
                        }
                    }
                }
                if (off > 0) sb.AppendLine((d.def != null ? d.def.name : "?") + " | " + off + " | " + first.Value.ToString("0.0") + " | " + what);
            }
            // Deck ends of different caves within 8 m of each other (a landing that should meet a catwalk).
            sb.AppendLine(); sb.AppendLine("across caves | between | gap (m) | heights");
            var all = zone.DeckSpots.Where(d => d.mid != null && d.mid.Length >= 1).ToList();
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (all[i].cave == all[j].cave) continue;
                    foreach (var ea in new[] { all[i].mid[0], all[i].mid[all[i].mid.Length - 1] })
                        foreach (var eb in new[] { all[j].mid[0], all[j].mid[all[j].mid.Length - 1] })
                        { float gap = Vector3.Distance(ea, eb); if (gap <= 8) sb.AppendLine((all[i].def != null ? all[i].def.name : "?") + " <-> " + (all[j].def != null ? all[j].def.name : "?") + " | " + gap.ToString("0.00") + " | " + ea.ToString("0.0") + " / " + eb.ToString("0.0")); }
                }
            sb.AppendLine(); sb.AppendLine("junction | between | gap (m) | heights | path");
            var spots = zone.DeckSpots.Where(d => d.mid != null && d.mid.Length >= 1).ToList();
            for (int i = 0; i < spots.Count; i++)
                for (int j = i + 1; j < spots.Count; j++)
                    foreach (var ea in new[] { spots[i].mid[0], spots[i].mid[spots[i].mid.Length - 1] })
                        foreach (var eb in new[] { spots[j].mid[0], spots[j].mid[spots[j].mid.Length - 1] })
                        {
                            float gap = Vector3.Distance(ea, eb); if (gap > 2.5f) continue;
                            sb.AppendLine((spots[i].def != null ? spots[i].def.name : "?") + " <-> " + (spots[j].def != null ? spots[j].def.name : "?") + " | " + gap.ToString("0.00") + " | " + ea.y.ToString("0.0") + "/" + eb.y.ToString("0.0") + " | " + Path(ea + Vector3.up * .2f, eb + Vector3.up * .2f));
                        }
            var settings = NavMesh.GetSettingsByID(0);
            sb.AppendLine(); sb.AppendLine("bake: radius " + settings.agentRadius + " height " + settings.agentHeight + " slope " + settings.agentSlope + " climb " + settings.agentClimb + " voxel " + settings.voxelSize + " region " + settings.minRegionArea);
            sb.AppendLine(); sb.AppendLine("join | from (the cave's end) | to (the target's floor there) | path");
            foreach (var h in Hollow.All)
            {
                if (h.Join == null) continue;
                var from = h.At(h.Length - 1.5f); var to = h.Join.At(h.JoinAlong); float ty = 0;
                if (!h.Join.FloorAt(new Vector2(to.x, to.z), out ty)) { sb.AppendLine(h.Name + " | " + from + " | no floor in " + h.Join.Name + " at " + to + " | -"); continue; }
                var path = new NavMeshPath(); bool ok = NavMesh.CalculatePath(from + Vector3.up * .2f, new Vector3(to.x, ty + .2f, to.z), NavMesh.AllAreas, path);
                sb.AppendLine(h.Name + " | " + from.ToString("0.0") + " | " + new Vector3(to.x, ty, to.z).ToString("0.0") + " | " + (ok && path.status == NavMeshPathStatus.PathComplete ? "complete" : ok ? path.status.ToString() : "none") + (h.JoinRaised ? " (a lip " + h.JoinRise.ToString("0") + " m up)" : ""));
            }
            sb.AppendLine(); sb.AppendLine("mouth | the child's first row | the parent's floor there | path");
            foreach (var h in Hollow.All)
            {
                if (h.Parent == null) continue;
                var from = h.At(1.5f); var to = h.Parent.At(h.ParentAlong); float ty = 0;
                if (!h.Parent.FloorAt(new Vector2(to.x, to.z), out ty)) { sb.AppendLine(h.Name + " | " + from + " | no floor | -"); continue; }
                var path = new NavMeshPath(); bool ok = NavMesh.CalculatePath(from + Vector3.up * .2f, new Vector3(to.x, ty + .2f, to.z), NavMesh.AllAreas, path);
                sb.AppendLine(h.Name + " | " + from.ToString("0.0") + " | " + new Vector3(to.x, ty, to.z).ToString("0.0") + " | " + (ok && path.status == NavMeshPathStatus.PathComplete ? "complete" : ok ? path.status.ToString() : "none") + (h.MouthRaised ? " (a mouth " + h.MouthRise.ToString("0") + " m up)" : ""));
            }
            // The camps the nav test could not reach, and what is under each.
            sb.AppendLine(); sb.AppendLine("camp | centre | floor under | navmesh within 1.2 m | within 6 m (nearest y)");
            foreach (var c in zone.Zone.camps)
            {
                if (c == null) continue;
                float y = 0; bool floor = Hollow.FloorUnder(c.center, ref y); var p = new Vector3(c.center.x, floor ? y + .2f : 0, c.center.y);
                bool near = NavMesh.SamplePosition(p, out var hit, 6, NavMesh.AllAreas);
                sb.AppendLine(c.name + " | " + c.center + " | " + (floor ? y.ToString("0.0") : "none") + " | " + (floor && OnMesh(p, 1.2f)) + " | " + (near ? hit.position.y.ToString("0.0") : "no"));
            }
            string text = sb.ToString(); Debug.Log("ADIT NAV DIAGNOSTICS\n" + text);
            try { File.WriteAllText(@"D:\crulanda-work\encounter-validation\adit-nav-diagnostics.txt", text); } catch (Exception) { }
            Assert.Pass("see the log");
        }
    }
}
#endif
