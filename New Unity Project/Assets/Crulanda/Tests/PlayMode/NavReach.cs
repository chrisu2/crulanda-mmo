#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// Whether one point can be walked to from another on the navmesh. NavMesh.CalculatePath gives up on a long search and returns
    /// a partial path even where the way is open (across Oakhaven at 560 m to the far end of Crowsfoot Hollow, 2026-10-02), so a
    /// partial path is followed on from its last corner, leg after leg; only a leg that gets no nearer means the way is cut.
    /// Underground that test misleads: the drawn-out Sealed Adit winds back on itself (1,353 m of cave), so a partial path can stop
    /// at a corner no nearer in a straight line though the way goes on (the Ember Vent's camps, 2026-10-11). Then the caves are
    /// walked as AditTests walks them: breadcrumbs every six metres down each cave, each leg a complete short path.
    /// </summary>
    public static class NavReach
    {
        public static bool Walkable(Vector3 from, Vector3 to, List<Vector3> corners = null)
        {
            var path = new NavMeshPath(); corners?.Clear(); var start = from;
            for (int leg = 0; leg < 16; leg++)
            {
                if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.corners.Length == 0) break;
                corners?.AddRange(path.corners);
                if (path.status == NavMeshPathStatus.PathComplete) return true;
                var end = path.corners[path.corners.Length - 1];
                if ((end - from).sqrMagnitude < 1 || (end - to).sqrMagnitude >= (from - to).sqrMagnitude - .25f) break;   // no nearer
                from = end;
            }
            return Hollow.All != null && Hollow.All.Count > 0 && ViaCaves(start, to);
        }

        static bool Complete(Vector3 a, Vector3 b, NavMeshPath path)
        { return NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete; }

        /// <summary>A search over breadcrumbs down every cave (a branch joined to its parent's crumbs near its mouth): true when a chain
        /// of complete short paths leads from <paramref name="from"/> to <paramref name="to"/>.</summary>
        static bool ViaCaves(Vector3 from, Vector3 to)
        {
            var crumbs = CaveCrumbs(from, out var reach); var path = new NavMeshPath();
            foreach (var i in Enumerable.Range(0, crumbs.Count).Where(i => reach.Contains(i)).OrderBy(i => (crumbs[i] - to).sqrMagnitude).Take(6))
                if (Complete(crumbs[i], to, path)) return true;
            return false;
        }

        /// <summary>Breadcrumbs every six metres down every cave on the navmesh, and in <paramref name="reach"/> those a chain of
        /// complete short paths joins to <paramref name="from"/>.</summary>
        public static List<Vector3> CaveCrumbs(Vector3 from, out HashSet<int> reach)
        {
            var crumbs = new List<Vector3>(); var cave = new List<int>(); var caves = Hollow.All; reach = new HashSet<int>();
            for (int c = 0; c < caves.Count; c++)
            {
                var h = caves[c];
                for (float s = 1; s < h.Length - 1; s += 6)
                {
                    float aside = h.Half[h.Nearest(new Vector2(h.At(s).x, h.At(s).z), out _)] * .62f;
                    foreach (var off in new[] { 0f, aside, -aside })
                        if (NavMesh.SamplePosition(h.At(s, off) + Vector3.up * .2f, out var hit, 2, NavMesh.AllAreas)) { crumbs.Add(hit.position); cave.Add(c); break; }
                }
            }
            if (crumbs.Count == 0) return crumbs;
            var path = new NavMeshPath();
            List<int> Near(int i)
            {
                var list = new List<int>();
                if (i > 0 && cave[i - 1] == cave[i]) list.Add(i - 1);
                if (i + 1 < crumbs.Count && cave[i + 1] == cave[i]) list.Add(i + 1);
                for (int j = 0; j < crumbs.Count; j++) if (cave[j] != cave[i] && (crumbs[j] - crumbs[i]).sqrMagnitude < 14 * 14) list.Add(j);
                return list;
            }
            var queue = new Queue<int>();
            foreach (var i in Enumerable.Range(0, crumbs.Count).OrderBy(i => (crumbs[i] - from).sqrMagnitude).Take(6))
                if (Complete(from, crumbs[i], path)) { reach.Add(i); queue.Enqueue(i); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (int j in Near(i)) if (!reach.Contains(j) && Complete(crumbs[i], crumbs[j], path)) { reach.Add(j); queue.Enqueue(j); }
            }
            return crumbs;
        }
    }
}
#endif