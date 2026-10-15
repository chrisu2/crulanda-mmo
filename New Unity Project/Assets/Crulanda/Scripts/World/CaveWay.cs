using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The way through a zone's caves by their middle lines (2026-10-15, the adit-final group run; GAME-ONLY tooling): a node every 6 m down
    /// each cave, joined to the next, and each cave's mouth and end joined to the cave it opens out of or into. A raised mouth or lip that
    /// is left by a one-way drop (the chute, the falls: ZoneBuilder.Drops) is joined one way, down. Gates are not in it (a shut one is the
    /// run's to wait at).
    /// What it is for: NavMesh.CalculatePath gives up part way on a long way through a drawn-out dungeon (its search budget; ADIT_REWORK
    /// section 18, R87), and an agent sent at a camp 400 m off walks to the end of that partial path, often a pocket the straight line
    /// prefers, and stands there. A leader whose path comes back partial walks to <see cref="Next"/>'s node instead, leg after leg.
    /// </summary>
    public static class CaveWay
    {
        sealed class Node { public Hollow cave; public Vector3 at; public float along; public readonly List<(int to, float cost)> links = new List<(int, float)>(); }
        const float Spacing = 6;
        static List<Node> nodes; static ZoneBuilder builtFor; static int builtCaves = -1;

        static void Build(ZoneBuilder zone)
        {
            nodes = new List<Node>(); builtFor = zone; builtCaves = Hollow.All.Count;
            var byCave = new Dictionary<Hollow, List<int>>();
            foreach (var h in Hollow.All)
            {
                var list = new List<int>(); byCave[h] = list; float next = 0;
                for (int i = 0; i < h.Centre.Count; i++)
                {
                    bool last = i == h.Centre.Count - 1;
                    if (h.Along[i] < next && !last) continue;
                    next = h.Along[i] + Spacing;
                    list.Add(nodes.Count); nodes.Add(new Node { cave = h, at = h.Centre[i], along = h.Along[i] });
                }
                for (int k = 1; k < list.Count; k++) Link(list[k - 1], list[k], true);
            }
            foreach (var h in Hollow.All)
            {
                var list = byCave[h]; if (list.Count == 0) continue;
                if (h.Parent != null && byCave.TryGetValue(h.Parent, out var p)) Join(zone, h, list[0], p, h.MouthRaised);
                if (h.Join != null && byCave.TryGetValue(h.Join, out var j)) Join(zone, h, list[list.Count - 1], j, h.JoinRaised);
            }
        }
        static void Link(int a, int b, bool both)
        {
            var d = nodes[b].at - nodes[a].at; float cost = d.magnitude + 2 * Mathf.Abs(d.y);
            nodes[a].links.Add((b, cost)); if (both) nodes[b].links.Add((a, cost));
        }
        /// <summary>A cave's mouth or end node joined to the nearest node of the cave it opens into: one way (down) where a drop leaves that
        /// cave (<paramref name="h"/>, the high side) there: the drop's lip within h's walls, at its floor's height, within 30 m of the opening (the chute is the
        /// Foundry's, not the Cutting Drift's, whose mouth is 15 m from it).</summary>
        static void Join(ZoneBuilder zone, Hollow h, int mine, List<int> other, bool raised)
        {
            int best = -1; float bestD = float.MaxValue; var at = nodes[mine].at;
            foreach (int o in other) { float d = (nodes[o].at - at).sqrMagnitude; if (d < bestD) { bestD = d; best = o; } }
            if (best < 0) return;
            bool oneWay = false;
            if (raised && zone != null)
                foreach (var drop in zone.Drops)
                {
                    var f = new Vector2(drop.from.x, drop.from.z);
                    int r = h.Nearest(f, out float flat); if (Vector2.Distance(f, new Vector2(at.x, at.z)) < 30 && flat <= h.Half[r] + 1 && Mathf.Abs(drop.from.y - h.Centre[r].y) < 3) oneWay = true;
                }
            Link(mine, best, !oneWay);
        }
        static int Nearest(Vector3 p)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++) { var d = nodes[i].at - p; float s = d.x * d.x + d.z * d.z + 4 * d.y * d.y; if (s < bestD) { bestD = s; best = i; } }
            return best;
        }
        /// <summary>Forgets the graph (a zone rebuilt).</summary>
        public static void Reset() { nodes = null; builtFor = null; builtCaves = -1; }

        /// <summary>
        /// The way from <paramref name="from"/> to <paramref name="to"/> through the zone's caves: <paramref name="waypoint"/> the first node at
        /// least <paramref name="ahead"/> metres on along it (or <paramref name="to"/> when that is nearer), <paramref name="length"/> the way's
        /// length. False when the zone has no caves or there is no way (a one-way drop the wrong way round).
        /// </summary>
        public static bool Next(ZoneBuilder zone, Vector3 from, Vector3 to, float ahead, out Vector3 waypoint, out float length)
        {
            waypoint = to; length = Vector3.Distance(from, to);
            if (Hollow.All.Count == 0) return false;
            if (nodes == null || builtFor != zone || builtCaves != Hollow.All.Count) Build(zone);
            int s = Nearest(from), g = Nearest(to); if (s < 0 || g < 0) return false;
            // A shut gate cuts the way down its cave between the nodes either side of it (the sump bar from the Workings' side, a boss's door).
            var shut = new List<(Hollow cave, float along)>();
            if (zone != null) foreach (var gate in zone.Gates) if (gate != null && !gate.Open && !string.IsNullOrEmpty(gate.Cave)) { var gc = Hollow.All.Find(o => o.Name == gate.Cave); if (gc != null) shut.Add((gc, gate.Along)); }
            bool Cut(Node a, Node b)
            {
                if (a.cave != b.cave) return false;
                foreach (var (cave, along) in shut) if (cave == a.cave && along >= Mathf.Min(a.along, b.along) && along <= Mathf.Max(a.along, b.along)) return true;
                return false;
            }
            int n = nodes.Count; var dist = new float[n]; var prev = new int[n]; var done = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
            dist[s] = 0;
            var open = new SortedSet<(float d, int i)>(); open.Add((0, s));
            while (open.Count > 0)
            {
                var top = open.Min; open.Remove(top); int u = top.i; if (done[u]) continue; done[u] = true;
                if (u == g) break;
                foreach (var (v, c) in nodes[u].links)
                {
                    float nd = dist[u] + c; if (nd >= dist[v] || Cut(nodes[u], nodes[v])) continue;
                    if (dist[v] < float.MaxValue) open.Remove((dist[v], v));
                    dist[v] = nd; prev[v] = u; open.Add((nd, v));
                }
            }
            if (dist[g] == float.MaxValue) return false;
            var way = new List<int>(); for (int v = g; v >= 0; v = prev[v]) way.Add(v); way.Reverse();
            length = Vector3.Distance(from, nodes[s].at) + dist[g] + Vector3.Distance(nodes[g].at, to);
            float walked = Vector3.Distance(from, nodes[s].at);
            for (int k = 0; k < way.Count; k++)
            {
                if (k > 0) walked += Vector3.Distance(nodes[way[k - 1]].at, nodes[way[k]].at);
                if (walked >= ahead) { waypoint = nodes[way[k]].at; return true; }
            }
            waypoint = to; return true;
        }
    }
}
