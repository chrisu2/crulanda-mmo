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
