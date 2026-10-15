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
            for (int leg = 0; leg < 64; leg++)   // sixty-four legs: the way from the yard to the Pitch Pit is some 1,200 m of winding passage
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

        /// <summary>How far the way is from one point to another on the navmesh (the areas in <paramref name="mask"/>), leg after leg where the
        /// search gives up part way; +infinity where a leg gets no nearer (the way is cut, or winds back on itself too far to follow).</summary>
        public static float Length(Vector3 from, Vector3 to, int mask = NavMesh.AllAreas)
        {
            var path = new NavMeshPath(); float total = 0; var start = from;
            for (int leg = 0; leg < 64; leg++)   // sixty-four legs: the way from the yard to the Pitch Pit is some 1,200 m of winding passage
            {
                if (!NavMesh.CalculatePath(from, to, mask, path) || path.corners.Length == 0) return float.PositiveInfinity;
                for (int i = 1; i < path.corners.Length; i++) total += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                // Complete in one leg: the length. Complete after a partial leg: that leg may have wandered (round the chapel's lake, the
                // cloister stair came out 236 m where it is about 100), so the hops' length is taken where it is the shorter.
                if (path.status == NavMeshPathStatus.PathComplete) return leg == 0 || mask != NavMesh.AllAreas || Hollow.All == null || Hollow.All.Count == 0 ? total : Mathf.Min(total, HopLength(start, to));
                var end = path.corners[path.corners.Length - 1];
                // Each leg must get somewhere (not: nearer as the crow flies; a leg round the Gallery or down a winding drift ends farther
                // off than it began, and the search gave up on the whole reworked Adit for that: the floor map of 2026-10-14 21:12).
                if ((end - from).sqrMagnitude < 1) break;
                from = end;
            }
            // The search gave up: the way may still be there (a leg that ends in a pocket the straight-line heuristic prefers; the floor
            // map of 2026-10-14 reached every floor of the Adit by hops where this search did not). Breadcrumb to breadcrumb, then,
            // each hop a complete short path, the hops' lengths summed: +infinity only when no chain of hops arrives.
            return Hollow.All != null && Hollow.All.Count > 0 && mask == NavMesh.AllAreas ? HopLength(start, to) : float.PositiveInfinity;
        }
        /// <summary>The way's length by hops over the caves' breadcrumbs (see <see cref="CaveCrumbs"/>): from <paramref name="from"/> to the
        /// crumbs it reaches, crumb to crumb by complete short paths (the shortest chain found first), and from the nearest reached crumb
        /// to <paramref name="to"/>; +infinity when no chain arrives.</summary>
        static float HopLength(Vector3 from, Vector3 to)
        {
            var crumbs = CaveCrumbs(from, out var reach, out var dist); if (crumbs.Count == 0) return float.PositiveInfinity;
            var path = new NavMeshPath(); float best = float.PositiveInfinity;
            foreach (var i in Enumerable.Range(0, crumbs.Count).Where(i => reach.Contains(i)).OrderBy(i => (crumbs[i] - to).sqrMagnitude).Take(8))
                if (Complete(crumbs[i], to, path)) best = Mathf.Min(best, dist[i] + PathLength(path));
            return best;
        }
        static float PathLength(NavMeshPath path) { float l = 0; for (int i = 1; i < path.corners.Length; i++) l += Vector3.Distance(path.corners[i - 1], path.corners[i]); return l; }

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
        public static List<Vector3> CaveCrumbs(Vector3 from, out HashSet<int> reach) { return CaveCrumbs(from, out reach, out _); }
        /// <summary>As above, with each reached crumb's way length from <paramref name="from"/> (the hops' complete paths summed).</summary>
        public static List<Vector3> CaveCrumbs(Vector3 from, out HashSet<int> reach, out Dictionary<int, float> dist)
        {
            var crumbs = new List<Vector3>(); var cave = new List<int>(); var caves = Hollow.All; reach = new HashSet<int>(); dist = new Dictionary<int, float>();
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
                if (Complete(from, crumbs[i], path)) { reach.Add(i); dist[i] = PathLength(path); queue.Enqueue(i); }
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (int j in Near(i)) if (!reach.Contains(j) && Complete(crumbs[i], crumbs[j], path)) { reach.Add(j); dist[j] = dist[i] + PathLength(path); queue.Enqueue(j); }
            }
            return crumbs;
        }
    }
}
#endif