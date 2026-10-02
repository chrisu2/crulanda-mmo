#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Tests
{
    /// <summary>
    /// Whether one point can be walked to from another on the navmesh. NavMesh.CalculatePath gives up on a long search and returns
    /// a partial path even where the way is open (across Oakhaven at 560 m to the far end of Crowsfoot Hollow, 2026-10-02), so a
    /// partial path is followed on from its last corner, leg after leg; only a leg that gets no nearer means the way is cut.
    /// </summary>
    public static class NavReach
    {
        public static bool Walkable(Vector3 from, Vector3 to, List<Vector3> corners = null)
        {
            var path = new NavMeshPath(); corners?.Clear();
            for (int leg = 0; leg < 16; leg++)
            {
                if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.corners.Length == 0) return false;
                corners?.AddRange(path.corners);
                if (path.status == NavMeshPathStatus.PathComplete) return true;
                var end = path.corners[path.corners.Length - 1];
                if ((end - from).sqrMagnitude < 1 || (end - to).sqrMagnitude >= (from - to).sqrMagnitude - .25f) return false;   // no nearer: cut off
                from = end;
            }
            return false;
        }
    }
}
#endif
