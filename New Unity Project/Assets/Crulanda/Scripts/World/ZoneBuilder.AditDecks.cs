using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A deck as built (ZoneBuilder.AditDecks; not saved): its data, the cave it stands in, its middle line on its top sample by sample
    /// (world), the level way across it at each sample (to the right as it is walked from its start), the floor under each sample and
    /// whether it is built up from that floor there, and its half width. A round deck has one sample, its centre, and half is its radius.
    /// </summary>
    public sealed class ZoneDeckSpot
    {
        public ZoneDeck def; public Hollow cave;
        public Vector3[] mid, across; public float[] under; public bool[] solid;
        public float half; public bool round;
        /// <summary>Its ground-plane bounds, and the lowest floor under it and its highest top.</summary>
        public Rect box; public float low, high;
        /// <summary>What was built of it (its walkable mesh; set by ZoneBuilder.BuildCaveWorks).</summary>
        public Transform root;

        /// <summary>Works out <see cref="box"/>, <see cref="low"/> and <see cref="high"/> from the samples.</summary>
        public void Seal()
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue; low = float.MaxValue; high = float.MinValue;
            for (int k = 0; k < mid.Length; k++)
            {
                x0 = Mathf.Min(x0, mid[k].x - half); x1 = Mathf.Max(x1, mid[k].x + half); z0 = Mathf.Min(z0, mid[k].z - half); z1 = Mathf.Max(z1, mid[k].z + half);
                low = Mathf.Min(low, under[k] - .3f); high = Mathf.Max(high, mid[k].y);
            }
            box = Rect.MinMaxRect(x0, z0, x1, z1);
        }
        /// <summary>Its top under a ground point, if the point is on it.</summary>
        public bool Top(Vector2 p, out float y)
        {
            y = 0; if (mid == null || mid.Length == 0 || !box.Contains(p)) return false;
            if (round) { if ((p - new Vector2(mid[0].x, mid[0].z)).sqrMagnitude > half * half) return false; y = mid[0].y; return true; }
            bool found = false; float best = float.MaxValue;
            for (int k = 0; k + 1 < mid.Length; k++)
            {
                var a = new Vector2(mid[k].x, mid[k].z); var d = new Vector2(mid[k + 1].x, mid[k + 1].z) - a; float len2 = d.sqrMagnitude; if (len2 < 1e-6f) continue;
                float t = Vector2.Dot(p - a, d) / len2; if (t < -.05f || t > 1.05f) continue;
                float tc = Mathf.Clamp01(t), side = (p - (a + d * tc)).magnitude;
                if (side > half + .02f || side >= best) continue;
                best = side; y = Mathf.Lerp(mid[k].y, mid[k + 1].y, tc); found = true;
            }
            return found;
        }
        /// <summary>
        /// Whether a point is in its way (<paramref name="margin"/> metres round it): on it or just over it (a head's height and more), in the
        /// slab, or anywhere under it where it is less than 4 m over its floor (a ramp, a plinth: nothing stands under it or against it).
        /// </summary>
        public bool Holds(Vector3 p, float margin)
        {
            if (mid == null || mid.Length == 0) return false;
            if (p.x < box.xMin - margin || p.x > box.xMax + margin || p.z < box.yMin - margin || p.z > box.yMax + margin || p.y < low - margin || p.y > high + 2.6f) return false;
            var q = new Vector2(p.x, p.z);
            if (round) return (q - new Vector2(mid[0].x, mid[0].z)).magnitude <= half + margin && InBand(p.y, mid[0].y, under[0]);
            for (int k = 0; k + 1 < mid.Length; k++)
            {
                var a = new Vector2(mid[k].x, mid[k].z); var d = new Vector2(mid[k + 1].x, mid[k + 1].z) - a; float len = d.magnitude; if (len < 1e-3f) continue;
                float t = Vector2.Dot(q - a, d) / (len * len), reach = margin / len; if (t < -reach || t > 1 + reach) continue;
                float tc = Mathf.Clamp01(t); if ((q - (a + d * tc)).magnitude > half + margin) continue;
                if (InBand(p.y, Mathf.Lerp(mid[k].y, mid[k + 1].y, tc), Mathf.Lerp(under[k], under[k + 1], tc))) return true;
            }
            return false;
        }
        static bool InBand(float y, float top, float floor) { float bottom = top - floor < 4 ? floor - .3f : top - 1.2f; return y >= bottom && y <= top + 2.6f; }
    }

    /// <summary>A mesh of flat faces, each wound to face the way it is told (the decks, the cave water).</summary>
    sealed class FaceMesh
    {
        readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector2> uv = new List<Vector2>(); readonly List<int> tri = new List<int>();
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 want, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int o = v.Count; v.Add(a); v.Add(b); v.Add(c); uv.Add(ua); uv.Add(ub); uv.Add(uc);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), want) >= 0) { tri.Add(o); tri.Add(o + 1); tri.Add(o + 2); } else { tri.Add(o); tri.Add(o + 2); tri.Add(o + 1); }
        }
        /// <summary>A quad, its corners a, b, c, e in order round it.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e, Vector3 want, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ue)
        {
            int o = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(e); uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ue);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), want) >= 0) tri.AddRange(new[] { o, o + 1, o + 2, o, o + 2, o + 3 });
            else tri.AddRange(new[] { o, o + 2, o + 1, o, o + 3, o + 2 });
        }
        public int Count { get { return v.Count; } }
        public Mesh Make(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>
    /// The works in a dungeon's caves (the Adit rework, Docs/ADIT_REWORK.md; all GAME-ONLY): decks (ZoneDeck: trestles, catwalks, rope
    /// bridges, platforms, islands, scree ramps), one-way drops (ZoneDrop), and the named places the run and its events use (ZoneAnchor),
    /// all from the zone's data and none of them there without it. Worked out before the caves are dressed (PrepareCaveWorks: the caves'
    /// own rubble and furnishings keep out of a deck's way) and built after them (BuildCaveWorks). No machines (DUNGEON_DESIGN 0.05):
    /// timber, stone, rope and rubble. Their own streams: the zone's draws are untouched.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The zone's decks as worked out (empty without decks in the data).</summary>
        public readonly List<ZoneDeckSpot> DeckSpots = new List<ZoneDeckSpot>();
        /// <summary>The one-way drops agents may take (EncounterNavigation makes each a one-way link): where you step off, where you land,
        /// how wide.</summary>
        public readonly List<(Vector3 from, Vector3 to, float width)> Drops = new List<(Vector3 from, Vector3 to, float width)>();
        /// <summary>The named places in the caves (ZoneAnchor), where each stands.</summary>
        public readonly Dictionary<string, Vector3> Anchors = new Dictionary<string, Vector3>();
        readonly List<(ZoneDrop def, Vector3 from, Vector3 to)> dropLooks = new List<(ZoneDrop def, Vector3 from, Vector3 to)>();
        /// <summary>Under this a deck is built up from the floor (a plinth): there is no room for anyone under it.</summary>
        const float SolidUnder = 2.5f;

        Hollow CaveNamed(string name) { return string.IsNullOrEmpty(name) ? null : Hollow.All.Find(o => o.Name == name); }
        /// <summary>The zone's anchor of that name in a cave, or null.</summary>
        ZoneAnchor AnchorIn(Hollow h, string name)
        {
            if (Zone.anchors == null || h == null) return null;
            foreach (var a in Zone.anchors) if (a != null && a.name == name && a.cave == h.Name) return a;
            return null;
        }
        /// <summary>The zone's gate of that title in a cave (ZoneDefinition.gates), or null.</summary>
        ZoneGateDef GateIn(Hollow h, string title)
        {
            if (Zone.gates == null || h == null) return null;
            foreach (var g in Zone.gates) if (g != null && g.title == title && g.cave == h.Name) return g;
            return null;
        }
        static int RingIndex(Hollow h, float s) { int i = 0; while (i + 1 < h.Along.Count && h.Along[i + 1] <= s) i++; return i; }

        /// <summary>The cave works worked out from the data (after PrepareHollows, before anything is built): the water first (the decks'
        /// floors are the pools' beds), then the decks, the drops and the anchors.</summary>
        void PrepareCaveWorks()
        {
            DeckSpots.Clear(); Drops.Clear(); Anchors.Clear(); dropLooks.Clear(); caveWaters.Clear(); CaveWaterAreas.Clear();
            if (Hollow.All.Count == 0) return;
            PrepareCaveWater();
            if (Zone.decks != null)
                foreach (var d in Zone.decks) { if (d == null) continue; var spot = PrepareDeck(d); if (spot != null) DeckSpots.Add(spot); }
            if (Zone.drops != null)
                foreach (var d in Zone.drops)
                {
                    if (d == null) continue;
                    var fc = CaveNamed(d.fromCave); var tc = string.IsNullOrEmpty(d.toCave) ? fc : CaveNamed(d.toCave);
                    if (fc == null || tc == null) { Debug.LogWarning(Zone.id + ": the drop '" + d.name + "' names a cave the zone has not got."); continue; }
                    var from = CavePoint(fc, d.from, d.fromAside, d.fromRise); var to = CavePoint(tc, d.to, d.toAside, d.toRise);
                    if (from.y - to.y < .5f) { Debug.LogWarning(Zone.id + ": the drop '" + d.name + "' does not go down (" + from + " to " + to + ")."); continue; }
                    dropLooks.Add((d, from, to));
                    if (!d.scenery) Drops.Add((from, to, d.width > 0 ? d.width : 2));
                }
            if (Zone.anchors != null)
                foreach (var a in Zone.anchors)
                {
                    var h = a == null || string.IsNullOrEmpty(a.name) ? null : CaveNamed(a.cave); if (h == null) continue;
                    Anchors[a.name] = CavePoint(h, a.along, a.aside, a.rise);
                }
        }
        /// <summary>A point in a cave: along, aside, and rise over its floor there (the bed where a pool is sunk into it); on a deck's top
        /// when one is within a metre and a half of that height.</summary>
        Vector3 CavePoint(Hollow h, float along, float aside, float rise)
        {
            var w = h.At(along, aside); var p = new Vector2(w.x, w.z); float y = w.y - BasinDip(h, p) + rise;
            if (rise > .3f && DeckTop(p, y, out float top) && Mathf.Abs(top - y) < 1.5f) y = top;
            return new Vector3(w.x, y, w.z);
        }
        /// <summary>The highest cave floor at a point no higher than <paramref name="under"/> (a pool's bed where one is sunk), or
        /// <paramref name="fallback"/> when there is none.</summary>
        float FloorBelow(Vector2 p, float under, float fallback)
        {
            float best = float.NegativeInfinity;
            foreach (var c in Hollow.All) if (c.FloorAt(p, out float f)) { f -= BasinDip(c, p); if (f <= under && f > best) best = f; }
            return float.IsNegativeInfinity(best) ? fallback : best;
        }
        /// <summary>The highest thing to stand on at a point no higher than <paramref name="under"/>: a cave's floor or another deck's top
        /// (a post's foot). float.NegativeInfinity when there is none.</summary>
        float SurfaceBelow(Vector2 p, float under, ZoneDeckSpot not)
        {
            float best = FloorBelow(p, under, float.NegativeInfinity);
            foreach (var d in DeckSpots) if (d != not && d.Top(p, out float y) && y <= under && y > best) best = y;
            return best;
        }
        /// <summary>The top of a deck at a point nearest the height <paramref name="near"/>, if any deck is there.</summary>
        public bool DeckTop(Vector2 p, float near, out float y)
        {
            y = 0; bool found = false;
            foreach (var d in DeckSpots) if (d.Top(p, out float top) && (!found || Mathf.Abs(top - near) < Mathf.Abs(y - near))) { y = top; found = true; }
            return found;
        }
        /// <summary>Whether a world point is in any deck's way (see <see cref="ZoneDeckSpot.Holds"/>): the caves' rubble and furnishings
        /// keep out of it.</summary>
        bool InDeckWay(Vector3 w, float margin)
        {
            foreach (var d in DeckSpots) if (d.Holds(w, margin)) return true;
            return false;
        }
        /// <summary>Whether a thing with these bounds reaches into a deck's way (its middle and corners, a level every two metres up).</summary>
        bool DeckOverlaps(Bounds b, float margin)
        {
            if (DeckSpots.Count == 0) return false;
            int levels = Mathf.Clamp(Mathf.CeilToInt(b.size.y / 2), 1, 24);
            for (int l = 0; l <= levels; l++)
            {
                float y = Mathf.Lerp(b.min.y, b.max.y, l / (float)levels);
                if (InDeckWay(new Vector3(b.center.x, y, b.center.z), margin)) return true;
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                        if (InDeckWay(new Vector3(b.center.x + sx * b.extents.x, y, b.center.z + sz * b.extents.z), margin)) return true;
            }
            return false;
        }
        /// <summary>
        /// How far out from ring i's middle a cave's wall stands on one side (1 its right, -1 its left), <paramref name="up"/> metres over its
        /// floor: the rock as Cavern lofts it (its arch and its bumps), straight between the arch's corners; over the crown, the crown's.
        /// </summary>
        float WallOut(Hollow h, int i, int side, float up)
        {
            const int K = 14; int P = K + 3, crown = P / 2; float seed = (Zone.seed % 997) * .173f, s = h.Along[i];
            float Corner(int k, out float y)
            {
                float th = Mathf.Clamp(k - 1, 0, K) * Mathf.PI / K;
                float bump = (Mathf.PerlinNoise(s * .28f + seed, th * 1.6f + 3.1f) - .5f) * .26f + (Mathf.PerlinNoise(s * .9f + 11, th * 4.2f + seed) - .5f) * .12f;
                y = h.Height[i] * Mathf.Pow(Mathf.Max(0, Mathf.Sin(th)), .85f) * (1 + bump * .8f);
                return h.Half[i] * Mathf.Pow(Mathf.Abs(Mathf.Cos(th)), .75f) * (1 + bump);
            }
            int k0 = side > 0 ? 1 : P - 2, step = side > 0 ? 1 : -1;
            float lastX = Corner(k0, out float lastY); if (up <= lastY) return lastX;
            for (int k = k0 + step; side > 0 ? k <= crown : k >= crown; k += step)
            {
                float x = Corner(k, out float y);
                if (up <= y) return Mathf.Lerp(lastX, x, Mathf.InverseLerp(lastY, y, up));
                lastX = x; lastY = y;
            }
            return lastX;
        }

        /// <summary>A deck worked out: its samples every metre and a half along its middle line, each one's floor and whether it is built
        /// up from it. Null (and a warning) for a deck with no cave or no length.</summary>
        ZoneDeckSpot PrepareDeck(ZoneDeck d)
        {
            var h = CaveNamed(d.cave);
            if (h == null || h.Centre.Count < 2) { Debug.LogWarning(Zone.id + ": the deck '" + d.name + "' names a cave the zone has not got ('" + d.cave + "')."); return null; }
            string style = string.IsNullOrEmpty(d.style) ? "timber" : d.style;
            var spot = new ZoneDeckSpot { def = d, cave = h, half = Mathf.Max(.6f, (d.width > 0 ? d.width : 3) / 2) };
            if (d.radius > 0)
            {
                var c = h.At(d.from, d.aside); var cp = new Vector2(c.x, c.z); float floor = c.y - BasinDip(h, cp);
                spot.round = true; spot.half = d.radius;
                spot.mid = new[] { new Vector3(c.x, c.y + d.rise, c.z) }; spot.across = new[] { Vector3.right }; spot.under = new[] { floor };
                spot.solid = new[] { d.solid || style == "scree" || c.y + d.rise - floor < SolidUnder };
                spot.Seal(); return spot;
            }
            float y0 = h.At(d.from).y + d.rise, y1 = h.At(d.to).y + d.rise2;
            float S(float u) { return Mathf.Lerp(d.from, d.to, u); }
            Vector3 Point(float u)
            {
                float s = S(u), top = Mathf.Lerp(y0, y1, u), aside = Mathf.Lerp(d.aside, d.aside2, u);
                if (d.wall != 0)
                {
                    // Against the wall at its height: the wall at its top and at a head over it, whichever stands further in, and a hand's
                    // breadth off the rock's lining.
                    int i = RingIndex(h, s), side = d.wall > 0 ? 1 : -1; float up = top - h.At(s).y;
                    float wallAt = Mathf.Min(WallOut(h, i, side, up + .2f), WallOut(h, i, side, up + 2.2f));
                    aside = side * Mathf.Max(0, wallAt - .45f - spot.half);
                }
                var w = h.At(s, aside);
                // Inside the cave's air at a head over the planks, both edges: a deck laid by its aside alone ran into the rock where the
                // wall leans in at its height (the overseers' walk and stair, 16 and 11 samples in the shell: AditNavDiagnostics 18:45).
                for (int pass = 0; pass < 24 && Mathf.Abs(aside) > .01f; pass++)
                {
                    var c = h.At(s, 0); var outward = new Vector3(w.x - c.x, 0, w.z - c.z); if (outward.sqrMagnitude > 1e-6f) outward.Normalize();
                    var edge = new Vector3(w.x, top + 1.2f, w.z) + outward * spot.half;
                    if (h.Open(edge, .97f)) break;
                    aside = Mathf.MoveTowards(aside, 0, .25f); w = h.At(s, aside);
                }
                return new Vector3(w.x, top, w.z);
            }
            float length = 0; var last = Point(0);
            for (int k = 1; k <= 64; k++) { var p = Point(k / 64f); length += new Vector2(p.x - last.x, p.z - last.z).magnitude; last = p; }
            if (length < .5f) { Debug.LogWarning(Zone.id + ": the deck '" + d.name + "' has no length."); return null; }
            int N = Mathf.Max(1, Mathf.CeilToInt(length / 1.5f));
            spot.mid = new Vector3[N + 1]; spot.across = new Vector3[N + 1]; spot.under = new float[N + 1]; spot.solid = new bool[N + 1];
            for (int k = 0; k <= N; k++) spot.mid[k] = Point(k / (float)N);
            for (int k = 0; k <= N; k++)
            {
                var dir = spot.mid[Mathf.Min(N, k + 1)] - spot.mid[Mathf.Max(0, k - 1)]; dir.y = 0;
                if (dir.sqrMagnitude < 1e-6f) { var f = h.At(S(k / (float)N) + .5f) - h.At(S(k / (float)N) - .5f); dir = new Vector3(f.x, 0, f.z); }
                dir.Normalize(); spot.across[k] = new Vector3(dir.z, 0, -dir.x);
                var q = new Vector2(spot.mid[k].x, spot.mid[k].z);
                spot.under[k] = FloorBelow(q, spot.mid[k].y - .2f, h.At(S(k / (float)N)).y - BasinDip(h, q));
                spot.solid[k] = d.solid || style == "scree" || spot.mid[k].y - spot.under[k] < SolidUnder;
            }
            spot.Seal(); return spot;
        }

        /// <summary>The cave works built (after the caves, which kept out of their way): each deck, the cave water, the drops' lips and
        /// falls. What is only for the eye is static-batched; the decks' own meshes (the navmesh reads them) and the solid blocks are not.</summary>
        void BuildCaveWorks()
        {
            if (DeckSpots.Count == 0 && dropLooks.Count == 0 && caveWaters.Count == 0) return;
            var root = new GameObject("Cave works").transform; root.SetParent(props, false); root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var solidRoot = new GameObject("Decks").transform; solidRoot.SetParent(root, false);
            var look = new GameObject("Look").transform; look.SetParent(root, false);
            foreach (var spot in DeckSpots) BuildDeck(spot, solidRoot, look);
            JoinAprons(solidRoot);
            BuildCaveWater(root, look);
            foreach (var dl in dropLooks) BuildDrop(dl.def, dl.from, dl.to, root, look);
            if (Application.isPlaying && look.childCount > 0) StaticBatchingUtility.Combine(look.gameObject);
        }
        /// <summary>A short apron of planks from each raised lip or mouth of a cave to the nearest deck end within 5 m and 2 m of its
        /// height (the Cutting Drift's lip onto the Foundry's catwalk, its mouth onto the bridge tower; AditNavDiagnostics 2026-10-14:
        /// the landing stopped 2.5 m short and the navmesh never joined them). Walked on and in the navmesh like a deck.</summary>
        void JoinAprons(Transform solidRoot)
        {
            var plank = Tint(art.timber, new Color(.4f, .29f, .18f));
            foreach (var h in Hollow.All)
            {
                var ends = new List<(Vector3 at, string name)>();
                if (h.Join != null && h.JoinRaised) ends.Add((h.At(h.Length - .6f), h.Name + "'s lip"));
                if (h.Parent != null && h.MouthRaised) ends.Add((h.At(.6f), h.Name + "'s mouth"));
                foreach (var (lip, name) in ends)
                {
                    float y = lip.y; if (h.FloorAt(new Vector2(lip.x, lip.z), out float fy)) y = fy;
                    var from = new Vector3(lip.x, y, lip.z); Vector3? best = null; float bestD = 5;
                    foreach (var d in DeckSpots)
                    {
                        if (d.mid == null || d.mid.Length == 0) continue;
                        foreach (var e in new[] { d.mid[0], d.mid[d.mid.Length - 1] }) { float dd = Vector3.Distance(e, from); if (dd < bestD && Mathf.Abs(e.y - from.y) < 2) { bestD = dd; best = e; } }
                    }
                    if (best == null || bestD < .3f) continue;
                    var to = best.Value; var dir = to - from;
                    var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = "Apron: " + name; o.transform.SetParent(solidRoot, false);
                    o.transform.position = (from + to) / 2 - Vector3.up * .08f; o.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    o.transform.localScale = new Vector3(3, .16f, dir.magnitude + 1.2f);
                    o.GetComponent<MeshRenderer>().sharedMaterial = plank; o.AddComponent<NavWalkable>();
                    Debug.Log("Apron " + name + ": " + from.ToString("0.0") + " to " + to.ToString("0.0") + " (" + bestD.ToString("0.0") + " m).");
                }
            }
        }
        /// <summary>A solid box (a post, a rail) standing on <paramref name="foot"/>: in the way of you and every agent (NavBlocker).</summary>
        static void DeckBlock(Transform parent, Vector3 foot, Vector3 size, Quaternion rot, string what = null)
        {
            var o = new GameObject(what == null ? "Solid" : "Solid: " + what); o.transform.SetParent(parent, false);   // named for AditNavDiagnostics (what stands in a deck's way) o.transform.localPosition = foot + rot * new Vector3(0, size.y / 2, 0); o.transform.localRotation = rot;
            o.AddComponent<BoxCollider>().size = size; o.AddComponent<NavBlocker>();
        }

        /// <summary>
        /// A deck: one mesh, its top walked on (a collider, and in the navmesh), its sides down to the floor where it is built up and a slab's
        /// thickness where it stands on posts; posts down to the floor (or a deck below), solid; rails on its open edges, solid to 1.6 m
        /// (none where a floor or another deck carries on at its height, or where a drop goes off); and its look by its style.
        /// </summary>
        void BuildDeck(ZoneDeckSpot spot, Transform solidRoot, Transform look)
        {
            var d = spot.def; string style = string.IsNullOrEmpty(d.style) ? "timber" : d.style;
            var dr = TreeRandom(spot.mid[0] + new Vector3(1.3f, 0, 4.7f)); float D() { return (float)dr.NextDouble(); }
            var plank = Tint(art.timber, new Color(.4f, .29f, .18f)); var post = Tint(art.timber, new Color(.27f, .19f, .12f));
            var cut = Tint(art.stone, new Color(.42f, .4f, .37f)); var rubble = RockTint(new Color(.4f, .38f, .35f)); var rope = Tint(art.hay, new Color(.58f, .5f, .34f));
            var deckMat = style == "stone" ? cut : style == "scree" ? rubble : plank;
            float thick = style == "rope" ? .15f : style == "stone" ? .5f : .3f;
            int N = spot.mid.Length - 1;

            // The deck itself.
            var fm = new FaceMesh();
            if (spot.round)
            {
                const int Seg = 24; var c = spot.mid[0]; float bottom = spot.solid[0] ? spot.under[0] - .3f : c.y - thick; var cb = new Vector3(c.x, bottom, c.z);
                for (int k = 0; k < Seg; k++)
                {
                    float a0 = k * Mathf.PI * 2 / Seg, a1 = (k + 1) * Mathf.PI * 2 / Seg;
                    var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * spot.half; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * spot.half;
                    var b0 = new Vector3(p0.x, bottom, p0.z); var b1 = new Vector3(p1.x, bottom, p1.z); var outward = (p0 + p1) / 2 - c; outward.y = 0;
                    fm.Tri(c, p0, p1, Vector3.up, new Vector2(c.x, c.z) / 2, new Vector2(p0.x, p0.z) / 2, new Vector2(p1.x, p1.z) / 2);
                    float arc0 = a0 * spot.half / 2, arc1 = a1 * spot.half / 2;
                    fm.Quad(p0, p1, b1, b0, outward, new Vector2(arc0, p0.y / 2), new Vector2(arc1, p1.y / 2), new Vector2(arc1, bottom / 2), new Vector2(arc0, bottom / 2));
                    fm.Tri(cb, b0, b1, Vector3.down, new Vector2(c.x, c.z) / 2, new Vector2(p0.x, p0.z) / 2, new Vector2(p1.x, p1.z) / 2);
                }
            }
            else
            {
                // The planks run 0.55 m past each end (the same height), onto the deck or the floor they meet: with the strip ending
                // exactly at its data the navmesh did not join deck to deck (the tally stage to its ramps, the walkway to the far ramp)
                // nor a cave's lip to a deck (AditNavDiagnostics, 2026-10-14 17:14).
                var back = spot.mid[0] - spot.mid[1]; back.y = 0; var on = spot.mid[N] - spot.mid[N - 1]; on.y = 0;
                var ext0 = back.sqrMagnitude > 1e-6f ? back.normalized * .55f : Vector3.zero; var extN = on.sqrMagnitude > 1e-6f ? on.normalized * .55f : Vector3.zero;
                Vector3 Mid(int k) { return k == 0 ? spot.mid[0] + ext0 : k == N ? spot.mid[N] + extN : spot.mid[k]; }
                Vector3 L(int k) { return Mid(k) - spot.across[k] * spot.half; }
                Vector3 R(int k) { return Mid(k) + spot.across[k] * spot.half; }
                Vector3 Down(Vector3 p, int k) { return new Vector3(p.x, spot.solid[k] ? spot.under[k] - .3f : spot.mid[k].y - thick, p.z); }
                float run = 0;
                for (int k = 0; k < N; k++)
                {
                    float seg = Vector3.Distance(spot.mid[k], spot.mid[k + 1]), r0 = run / 2, r1 = (run + seg) / 2; run += seg;
                    var side = (spot.across[k] + spot.across[k + 1]) / 2;
                    fm.Quad(L(k), R(k), R(k + 1), L(k + 1), Vector3.up, new Vector2(0, r0), new Vector2(spot.half, r0), new Vector2(spot.half, r1), new Vector2(0, r1));
                    fm.Quad(R(k), R(k + 1), Down(R(k + 1), k + 1), Down(R(k), k), side, new Vector2(r0, R(k).y / 2), new Vector2(r1, R(k + 1).y / 2), new Vector2(r1, Down(R(k + 1), k + 1).y / 2), new Vector2(r0, Down(R(k), k).y / 2));
                    fm.Quad(L(k), L(k + 1), Down(L(k + 1), k + 1), Down(L(k), k), -side, new Vector2(r0, L(k).y / 2), new Vector2(r1, L(k + 1).y / 2), new Vector2(r1, Down(L(k + 1), k + 1).y / 2), new Vector2(r0, Down(L(k), k).y / 2));
                    fm.Quad(Down(L(k), k), Down(R(k), k), Down(R(k + 1), k + 1), Down(L(k + 1), k + 1), Vector3.down, new Vector2(0, r0), new Vector2(spot.half, r0), new Vector2(spot.half, r1), new Vector2(0, r1));
                }
                fm.Quad(L(0), R(0), Down(R(0), 0), Down(L(0), 0), back, new Vector2(0, L(0).y / 2), new Vector2(spot.half, R(0).y / 2), new Vector2(spot.half, Down(R(0), 0).y / 2), new Vector2(0, Down(L(0), 0).y / 2));
                fm.Quad(L(N), R(N), Down(R(N), N), Down(L(N), N), on, new Vector2(0, L(N).y / 2), new Vector2(spot.half, R(N).y / 2), new Vector2(spot.half, Down(R(N), N).y / 2), new Vector2(0, Down(L(N), N).y / 2));
            }
            var deck = new GameObject("Deck: " + (string.IsNullOrEmpty(d.name) ? d.cave : d.name)); deck.transform.SetParent(solidRoot, false); spot.root = deck.transform;
            var mesh = fm.Make(deck.name);
            deck.AddComponent<MeshFilter>().sharedMesh = mesh; deck.AddComponent<MeshRenderer>().sharedMaterial = deckMat;
            deck.AddComponent<MeshCollider>().sharedMesh = mesh; deck.AddComponent<NavWalkable>();

            // Posts (piers in stone) down to what is under them, solid to a hand under the deck so its top stays whole in the navmesh; a
            // rope bridge hangs from its ends and a scree ramp is rubble to the floor.
            void Post(Vector3 top, float under)
            {
                float foot = SurfaceBelow(new Vector2(top.x, top.z), under - .3f, spot); if (float.IsNegativeInfinity(foot) || under - foot < .4f) return;
                // No post where another deck lies in its span or under its foot (the cross-walk's posts went down through the north
                // ramp, the catwalk's through the walkway, and each carved the lower deck's navmesh: AditNavDiagnostics 18:45); that
                // stretch hangs from its braces instead.
                foreach (var other in DeckSpots)
                {
                    if (other == spot) continue;
                    // Under the post or within 0.8 m of it (the carve round a post reaches 0.7 m: the cross-walk's posts beside the north ramp's edge still cut it, 19:24).
                    foreach (var off in new[] { Vector2.zero, new Vector2(.8f, 0), new Vector2(-.8f, 0), new Vector2(0, .8f), new Vector2(0, -.8f) })
                        if (other.Top(new Vector2(top.x, top.z) + off, out float oy) && oy > foot - .1f && oy < under + .6f) return;
                }
                float tall = under - foot, w = style == "stone" ? .7f : .26f; var at = new Vector3(top.x, foot, top.z);
                Part(PrimitiveType.Cube, look, at + Vector3.up * tall / 2, new Vector3(w, tall, w), style == "stone" ? cut : post);
                DeckBlock(solidRoot, at, new Vector3(w + .05f, Mathf.Max(.1f, tall - .15f), w + .05f), Quaternion.identity, "post of " + d.name);
            }
            if (style == "timber" || style == "stone")
            {
                if (spot.round) { if (!spot.solid[0]) for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3; Post(spot.mid[0] + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * spot.half * .75f, spot.mid[0].y - thick); } }
                else
                    for (int k = 0; k <= N; k += 2)
                    {
                        if (spot.solid[k]) continue;
                        foreach (int side in new[] { -1, 1 }) Post(spot.mid[k] + spot.across[k] * side * (spot.half - .2f), spot.mid[k].y - thick);
                        if (style == "timber")
                        {
                            // A cross-beam under the planks, and a brace from one post's foot to the other's head every other pair.
                            var fwd = Vector3.Cross(spot.across[k], Vector3.up);
                            Part(PrimitiveType.Cube, look, spot.mid[k] + Vector3.down * (thick + .1f), new Vector3(spot.half * 2, .2f, .22f), post, Quaternion.LookRotation(fwd));
                            float foot = FloorBelow(new Vector2(spot.mid[k].x, spot.mid[k].z), spot.mid[k].y - thick - .3f, float.NegativeInfinity);
                            if (k % 4 == 0 && !float.IsNegativeInfinity(foot) && spot.mid[k].y - foot > 3)
                            {
                                var a = spot.mid[k] - spot.across[k] * (spot.half - .2f); a.y = foot + 1; var b = spot.mid[k] + spot.across[k] * (spot.half - .2f); b.y = spot.mid[k].y - thick - .3f;
                                Part(PrimitiveType.Cube, look, (a + b) / 2, new Vector3(.14f, .14f, Vector3.Distance(a, b)), post, Quaternion.LookRotation(b - a));
                            }
                        }
                    }
            }
            if (style == "rope" && !spot.round)
                foreach (int k in new[] { 0, N })   // tall posts at its two ends, the ropes made fast to them
                    foreach (int side in new[] { -1, 1 })
                    {
                        var at = spot.mid[k] + spot.across[k] * side * (spot.half + .12f);
                        Part(PrimitiveType.Cube, look, at + Vector3.up * .5f, new Vector3(.24f, 2.2f, .24f), post);
                        DeckBlock(solidRoot, at + Vector3.down * .6f, new Vector3(.28f, 2.2f, .28f), Quaternion.identity, "rope post of " + d.name);
                    }
            if (style == "scree")   // rubble along its flanks (round its rim)
            {
                int stones = spot.round ? 16 : (N + 1) * 2;
                for (int k = 0; k < stones; k++)
                {
                    float size = .5f + D() * .8f, yaw = D() * 360;
                    var at = spot.round ? spot.mid[0] + new Vector3(Mathf.Cos(k * Mathf.PI / 8), 0, Mathf.Sin(k * Mathf.PI / 8)) * (spot.half + size * .3f)
                        : spot.mid[k / 2] + spot.across[k / 2] * (k % 2 == 0 ? -1 : 1) * (spot.half + size * .3f);
                    Lump(BoulderAt((int)(yaw / 60)), look, at + Vector3.down * size * .5f, new Vector3(size * 1.3f, size, size * 1.1f), rubble, yaw);
                }
            }

            // Rails on the open edges.
            string rails = d.rails ?? "";
            float most = 0; bool allSolid = true; for (int k = 0; k <= N; k++) { most = Mathf.Max(most, spot.mid[k].y - spot.under[k]); allSolid &= spot.solid[k]; }
            bool Wanted(int side, int k)
            {
                if (rails == "none") return false;
                if (rails == "both") return true;
                if (rails == "left") return side < 0;
                if (rails == "right") return side > 0;
                if (style == "scree" || (allSolid && most < 1.2f)) return false;   // a rubble ramp, a low plinth: a step off it, no rail
                if (d.wall == 0 || spot.round) return true;
                float s = Mathf.Lerp(d.from, d.to, N > 0 ? k / (float)N : 0); var cr = spot.cave.At(s, 1) - spot.cave.At(s, 0); cr.y = 0;
                return Vector3.Dot(spot.across[k] * side, cr * d.wall) <= 0;   // not along the wall it hugs
            }
            bool CarriesOn(Vector3 edge, Vector3 outward, float top)
            {
                var probe = new Vector2(edge.x + outward.x * .8f, edge.z + outward.z * .8f);
                foreach (var o in DeckSpots) if (o != spot && o.Top(probe, out float y) && Mathf.Abs(y - top) < .7f) return true;
                foreach (var c in Hollow.All) if (c.FloorAt(probe, out float f) && Mathf.Abs(f - BasinDip(c, probe) - top) < .7f) return true;
                foreach (var dl in dropLooks)
                    if (!dl.def.scenery && Mathf.Abs(dl.from.y - top) < 1.5f && Vector2.Distance(new Vector2(dl.from.x, dl.from.z), new Vector2(edge.x, edge.z)) < (dl.def.width > 0 ? dl.def.width : 2) / 2 + 1.2f) return true;
                return false;
            }
            void Rail(Vector3 a, Vector3 b, Vector3 outward, bool postAtA)
            {
                var along = b - a; float len = along.magnitude; if (len < .05f) return; var rot = Quaternion.LookRotation(along, Vector3.up);
                var inset = -outward * .08f;
                DeckBlock(solidRoot, (a + b) / 2 + inset, new Vector3(.15f, 1.6f, len + .05f), rot, "rail of " + d.name);
                if (style == "stone") { Part(PrimitiveType.Cube, look, (a + b) / 2 + inset + Vector3.up * .48f, new Vector3(.3f, .95f, len + .02f), cut, rot); return; }
                if (style == "rope")
                {
                    foreach (float y in new[] { 1.0f, .55f }) Part(PrimitiveType.Cylinder, look, (a + b) / 2 + inset + Vector3.up * (y - .04f), new Vector3(.045f, len / 2 + .02f, .045f), rope, Quaternion.FromToRotation(Vector3.up, along));
                    if (postAtA) Part(PrimitiveType.Cube, look, a + inset + Vector3.up * .55f, new Vector3(.1f, 1.1f, .1f), post);
                    return;
                }
                foreach (float y in new[] { 1.0f, .5f }) Part(PrimitiveType.Cube, look, (a + b) / 2 + inset + Vector3.up * y, new Vector3(.08f, .1f, len + .04f), post, rot);
                if (postAtA) Part(PrimitiveType.Cube, look, a + inset + Vector3.up * .53f, new Vector3(.11f, 1.06f, .11f), post);
            }
            if (spot.round)
            {
                const int RSeg = 24;
                for (int k = 0; k < RSeg; k++)
                {
                    float a0 = k * Mathf.PI * 2 / RSeg, a1 = (k + 1) * Mathf.PI * 2 / RSeg;
                    var p0 = spot.mid[0] + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * spot.half; var p1 = spot.mid[0] + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * spot.half;
                    var outward = (p0 + p1) / 2 - spot.mid[0]; outward.y = 0; outward.Normalize();
                    if (Wanted(1, 0) && !CarriesOn((p0 + p1) / 2, outward, spot.mid[0].y)) Rail(p0, p1, outward, true);
                }
            }
            else
                for (int k = 0; k < N; k++)
                    foreach (int side in new[] { -1, 1 })
                    {
                        if (!Wanted(side, k)) continue;
                        var a = spot.mid[k] + spot.across[k] * side * spot.half; var b = spot.mid[k + 1] + spot.across[k + 1] * side * spot.half;
                        var outward = (spot.across[k] + spot.across[k + 1]).normalized * side;
                        if (CarriesOn((a + b) / 2, outward, (a.y + b.y) / 2)) continue;
                        Rail(a, b, outward, k % 2 == 0);
                    }
        }

        /// <summary>
        /// A drop's look: a stone either side of its lip (the way off is between them), and where a stream goes over it (falls) a sheet of
        /// water from the lip down to where it lands, foam and mist at its foot and a little cold light; underground its tint holds.
        /// </summary>
        void BuildDrop(ZoneDrop d, Vector3 from, Vector3 to, Transform root, Transform look)
        {
            var dir = to - from; dir.y = 0; if (dir.sqrMagnitude < .01f) dir = Vector3.forward; dir.Normalize();
            var side = new Vector3(dir.z, 0, -dir.x); float w = d.width > 0 ? d.width : 2;
            var rr = TreeRandom(from + new Vector3(5.9f, 0, 2.3f)); float Rr() { return (float)rr.NextDouble(); }
            var stone = RockTint(new Color(.38f, .36f, .33f));
            if (!d.scenery)
                foreach (int s in new[] { -1, 1 })
                {
                    var at = from + side * s * (w / 2 + .6f) + dir * .3f; float size = .45f + Rr() * .35f, yaw = Rr() * 360;
                    if (InDeckWay(at, .2f)) continue;
                    Lump(BoulderAt((int)(yaw / 60)), look, at + Vector3.down * .1f, Vector3.one * size, stone, yaw);
                }
            if (!d.falls) return;
            float height = from.y - to.y + .3f;
            var fall = new GameObject("Falls: " + (string.IsNullOrEmpty(d.name) ? "a drop" : d.name)).transform; fall.SetParent(root, false);
            fall.position = new Vector3(from.x, to.y, from.z) + dir * .4f; fall.rotation = Quaternion.LookRotation(-dir);
            foreach (var (z, speed, alpha) in new[] { (.25f, .55f, .9f), (-.05f, .9f, .55f) })
            {
                var sheet = new GameObject("Falling water").transform; sheet.SetParent(fall, false);
                sheet.gameObject.AddComponent<MeshFilter>().sharedMesh = FallMesh(w, height, z);
                var mr = sheet.gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = FallMaterial(alpha); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var fw = sheet.gameObject.AddComponent<FallingWater>(); fw.speed = speed; fw.steady = true;
            }
            var foam = FallMaterial(.95f);
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Sphere, fall, new Vector3((i - 2.5f) * w / 6 + (Rr() - .5f) * .4f, .05f, -.5f - Rr() * .6f), new Vector3(1.1f + Rr() * .6f, .22f, .9f + Rr() * .4f), foam);
            var ps = new GameObject("Falls mist").AddComponent<ParticleSystem>(); ps.transform.SetParent(fall, false); ps.transform.localPosition = new Vector3(0, .5f, -.6f);
            var main = ps.main; main.startLifetime = 3f; main.startSpeed = .6f; main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.6f); main.maxParticles = 120;
            main.startColor = new Color(.85f, .92f, 1, .2f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -.03f;
            var emission = ps.emission; emission.rateOverTime = 16 + w * 2;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(w, .4f, 1.2f);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
            Glow(fall, new Vector3(0, 1.4f, -1.6f), 10, .7f, new Color(.6f, .8f, .95f), .7f);
        }
    }
}
