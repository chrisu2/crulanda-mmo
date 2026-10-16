using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Crown's surface detail (2026-10-16). Chris, of the Gilt Gate's stair: "will need to add more detail. search online for
    /// vines/ivy or something so it is not so white . maybe add some iron or something"; then "we need a real garden. need to use some of
    /// our texture packs". Laid over what ZoneBuilder.Crown built, after it (<see cref="BuildCrownDetail"/>, the last thing BuildPaving
    /// does in a city), each piece from a stream of its own keyed on its prop, so nothing else in the zone moves for it. All GAME-ONLY.
    /// - Ivy: clumps of strands from the ground up the curtain wall, its towers, the gate's towers, the parapets of the Grand Stair and the
    ///   Gilt Ramp and some house and hall walls: a stem wandering up, leaf cards (the painted leaf card, three greens in the vertex tint)
    ///   thick at the foot and thinning to the top, a spread of leaves on the ground. More on faces looking north (shaded), by the gardens
    ///   (where some flower) and round the Gilt Gate; a few a face, never everywhere.
    /// - Weathering: a damp band up from the ground along every wall's foot (mossy green on the stair's stringers), tinted panels over the
    ///   white stone (warm, grey, cool, a stretch or a storey at a time, in the masonry's own courses: <see cref="BoxUv"/>), rain streaks
    ///   under the string courses, ledges and window sills, moss on the stair treads' ends and risers, dirt along the paving's edges.
    /// - Ironwork (dark iron, some rust; no machines: hinges, studs, bars and chains only): strap hinges, studs and a ring on the gate
    ///   leaves; iron braziers at the gates; iron frames and brackets on the gate's lanterns; bands and a ring on house doors; grilles on
    ///   ground-floor windows; wrought lanterns on brackets beside doors and on posts up the stairs; a handrail on brackets up the Grand
    ///   Stair and the Gilt Ramp; a low cresting along the Crown terrace's edge and the ramp; rings and chains for the banners.
    /// - Trim: a string course on corbels under the curtain's wall walk with a dark slate drip, a slate cap on the parapets' coping and on
    ///   the curtain's battered foot; gilt stays the accent it was.
    /// - The gardens (<see cref="Garden"/>): clipped box hedges in leaf cards, gravel walks, beds of the packs' flowering plants edged in
    ///   dwarf box, lawns the zone's own grass grows in (<see cref="GardenLawn"/>), topiary, small trees, rose arches, stone benches, urns.
    /// Nothing here has a collider or a navmesh blocker: it is all on or against what is already solid, or stepped through (a flower, a
    /// bench). The static props' detail merges with them (MergeStatics) and static-batches; a walk-in hall's is grouped a face at a time so
    /// it fades with that wall (RoofFade). Cost, counted as built into <see cref="CrownDetail"/> ("triangles"; estimated when written, the
    /// Crown measured 7.46 M triangles a frame before it): ivy some 130 clumps of about 500 (65k), the walls' bands, panels and streaks
    /// about 6k, the iron about 30k, the gardens' hedges and cards about 15k plus some 350 of the packs' plants; well under the Adit's old
    /// prop load. The Crown has no caves, so PropCull does not run there: static batching and the camera's culling carry it.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>What the detail pass laid, by place and kind ("gate:iron", "wall:ivy clumps", "stair:moss", "garden:plants"), and
        /// "triangles" and "leaf cards" over all of it. The tests read it.</summary>
        public readonly Dictionary<string, int> CrownDetail = new Dictionary<string, int>();
        void Tally(string key, int n = 1) { CrownDetail.TryGetValue(key, out int v); CrownDetail[key] = v + n; }
        static int NameHash(string s) { int h = 17; if (s != null) foreach (char c in s) h = unchecked(h * 31 + c); return h; }

        // ---------- the pieces: flat meshes gathered a material at a time ----------
        /// <summary>Quads, boxes, rods and cones in world space, one list a material, made one mesh in a prop's frame when flushed.</summary>
        sealed class Bits
        {
            public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
            public int Triangles { get { return t.Count / 3; } }
            /// <summary>A quad a-b-c-d (in order round it) facing <paramref name="normal"/>, whichever way round it was given.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0) { var s = b; b = d; d = s; var su = ub; ub = ud; ud = su; }
                int at = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); for (int k = 0; k < 4; k++) n.Add(normal);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                t.Add(at); t.Add(at + 1); t.Add(at + 2); t.Add(at); t.Add(at + 2); t.Add(at + 3);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal) { Quad(a, b, c, d, normal, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)); }
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0) { var s = b; b = c; c = s; }
                int at = v.Count; v.Add(a); v.Add(b); v.Add(c); for (int k = 0; k < 3; k++) n.Add(normal);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); t.Add(at); t.Add(at + 1); t.Add(at + 2);
            }
            void Face(Vector3 c, Vector3 a, Vector3 b, Vector3 normal) { Quad(c - a - b, c + a - b, c + a + b, c - a + b, normal); }
            /// <summary>A box (12 triangles) at <paramref name="c"/>, <paramref name="size"/> along its own axes, turned <paramref name="q"/>.</summary>
            public void Box(Vector3 c, Vector3 size, Quaternion q)
            {
                Vector3 X = q * Vector3.right, Y = q * Vector3.up, Z = q * Vector3.forward, x = X * size.x / 2, y = Y * size.y / 2, z = Z * size.z / 2;
                Face(c + x, y, z, X); Face(c - x, y, z, -X); Face(c + y, x, z, Y); Face(c - y, x, z, -Y); Face(c + z, x, y, Z); Face(c - z, x, y, -Z);
            }
            /// <summary>A bar from a to b (square by default: wrought iron), no ends: 2 triangles a side.</summary>
            public void Rod(Vector3 a, Vector3 b, float r, int sides = 4)
            {
                var d = b - a; if (d.sqrMagnitude < 1e-6f) return; d.Normalize();
                var p1 = Vector3.Cross(d, Mathf.Abs(d.y) > .9f ? Vector3.right : Vector3.up).normalized; var p2 = Vector3.Cross(d, p1);
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                    Vector3 o0 = (p1 * Mathf.Cos(a0) + p2 * Mathf.Sin(a0)) * r, o1 = (p1 * Mathf.Cos(a1) + p2 * Mathf.Sin(a1)) * r;
                    Quad(a + o0, b + o0, b + o1, a + o1, (o0 + o1).normalized);
                }
            }
            /// <summary>An upright cone cut off (a bowl, a hood, a pot, a clipped cone of box): <paramref name="r0"/> at its foot,
            /// <paramref name="r1"/> at <paramref name="h"/> over it, its top and foot closed if asked.</summary>
            public void Frustum(Vector3 foot, float r0, float r1, float h, int sides, bool capTop, bool capFoot = false, float turn = 0)
            {
                var top = foot + Vector3.up * h;
                for (int i = 0; i < sides; i++)
                {
                    float a0 = turn + i * Mathf.PI * 2 / sides, a1 = turn + (i + 1) * Mathf.PI * 2 / sides;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)), mid = (d0 + d1).normalized;
                    var normal = (mid * h + Vector3.up * (r0 - r1)).normalized; if (normal.sqrMagnitude < .5f) normal = mid;
                    Quad(foot + d0 * r0, top + d0 * r1, top + d1 * r1, foot + d1 * r0, normal);
                    if (capTop && r1 > .001f) Tri(top, top + d0 * r1, top + d1 * r1, Vector3.up);
                    if (capFoot && r0 > .001f) Tri(foot, foot + d0 * r0, foot + d1 * r0, Vector3.down);
                }
            }
            /// <summary>A rough ball (a topiary's, three cut cones), <paramref name="r"/> round, its foot at <paramref name="foot"/>.</summary>
            public void Ball(Vector3 foot, float r, int sides = 8)
            {
                Frustum(foot, r * .42f, r, r * .58f, sides, false, true); Frustum(foot + Vector3.up * r * .58f, r, r, r * .84f, sides, false); Frustum(foot + Vector3.up * r * 1.42f, r, r * .42f, r * .58f, sides, true);
            }
        }
        /// <summary>The detail of one prop (or one face of a walk-in hall): its pieces a material at a time and its leaf cards, made in
        /// <see cref="frame"/>'s space when flushed. Everything is given in world space.</summary>
        sealed class DetailSet
        {
            public readonly Transform frame; public readonly string place, label;
            public readonly Dictionary<Material, Bits> bits = new Dictionary<Material, Bits>(); public readonly Dictionary<Material, ZoneMeshes.Cards> cards = new Dictionary<Material, ZoneMeshes.Cards>();
            public DetailSet(Transform frame, string place, string label) { this.frame = frame; this.place = place; this.label = label; }
            public Bits this[Material m] { get { if (!bits.TryGetValue(m, out var b)) bits[m] = b = new Bits(); return b; } }
            /// <summary>A leaf card (ZoneMeshes.Cards.Add, world space): a hair of sway at its tip, no more (it lies on a wall).</summary>
            public void Leaf(Material m, Vector3 at, Vector3 along, Vector3 across, float length, float width, Vector3 heart, float lift, Color tint)
            {
                if (!cards.TryGetValue(m, out var c)) cards[m] = c = new ZoneMeshes.Cards();
                c.Add(frame.InverseTransformPoint(at), frame.InverseTransformDirection(along), frame.InverseTransformDirection(across), length, width, frame.InverseTransformPoint(heart), lift, tint, 0, .08f);
            }
        }
        /// <summary>Makes a detail set's meshes under its frame, named "Detail ..." (the tests find them so), none with a collider. The flat
        /// overlays and the leaves cast no shadow; the iron and the trim do.</summary>
        void Flush(DetailSet d)
        {
            foreach (var kv in d.bits)
            {
                var b = kv.Value; if (b.Triangles == 0 || kv.Key == null) continue;
                var verts = new List<Vector3>(b.v.Count); var norms = new List<Vector3>(b.n.Count);
                for (int i = 0; i < b.v.Count; i++) { verts.Add(d.frame.InverseTransformPoint(b.v[i])); norms.Add(d.frame.InverseTransformDirection(b.n[i])); }
                var mesh = new Mesh { name = "Detail " + d.label }; if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, b.uv); mesh.SetTriangles(b.t, 0); mesh.RecalculateBounds();
                var o = MeshPart(mesh, d.frame, Vector3.zero, kv.Key); o.name = "Detail " + d.label;
                var r = o.GetComponent<MeshRenderer>(); r.shadowCastingMode = flatDetail.Contains(kv.Key) ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
                Tally("triangles", b.Triangles);
            }
            foreach (var kv in d.cards)
            {
                if (kv.Value.Count == 0 || kv.Key == null) continue;
                var o = MeshPart(kv.Value.Build("Detail " + d.label + " leaves"), d.frame, Vector3.zero, kv.Key); o.name = "Detail " + d.label + " leaves";
                o.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Tally("triangles", kv.Value.Count * 2); Tally("leaf cards", kv.Value.Count);
            }
        }

        // ---------- the palette ----------
        readonly HashSet<Material> flatDetail = new HashSet<Material>();
        Material Flat(Material m) { if (m != null) flatDetail.Add(m); return m; }
        Material Iron { get { return Tint(art.metal, new Color(.2f, .19f, .18f)); } }
        Material Rust { get { return Tint(art.metal, new Color(.46f, .29f, .18f)); } }
        Material TrimStone { get { return Dressed(new Color(.74f, .72f, .66f)); } }
        Material DarkSlate { get { return Tint(art.slate, new Color(.3f, .32f, .37f)); } }
        Material Damp { get { return Flat(Dressed(new Color(.6f, .58f, .53f))); } }
        Material DampFoot { get { return Flat(Dressed(new Color(.5f, .48f, .44f))); } }
        Material MossStone { get { return Flat(Dressed(new Color(.46f, .53f, .37f))); } }
        Material StreakStone { get { return Flat(Dressed(new Color(.67f, .65f, .6f))); } }
        Material RustStain { get { return Flat(Dressed(new Color(.66f, .52f, .41f))); } }
        /// <summary>The white stone a shade off: warm, grey, cool (over CityWhite's .83/.81/.76).</summary>
        Material StoneTint(int k) { return Flat(Dressed(k % 3 == 0 ? new Color(.87f, .81f, .7f) : k % 3 == 1 ? new Color(.75f, .74f, .71f) : new Color(.79f, .8f, .8f))); }
        /// <summary>The curtain's shaded foot a shade off: warmer, greyer.</summary>
        Material ShadeTint(int k) { return Flat(Dressed(k % 2 == 0 ? new Color(.72f, .67f, .58f) : new Color(.6f, .59f, .57f))); }
        Material Dirt { get { return Flat(Tint(art.soil != null ? art.soil : art.stone, new Color(.5f, .45f, .36f))); } }
        Material Gravel { get { return Flat(Tint(art.stone, new Color(.8f, .75f, .64f))); } }
        Material BedSoil { get { return Flat(Tint(art.soil != null ? art.soil : art.timber, new Color(.3f, .22f, .15f))); } }
        Material Lawn { get { return Flat(Tint(art.foliage, new Color(.3f, .46f, .2f))); } }
        Material HedgeBody { get { return Tint(art.foliage, Wither(new Color(.14f, .3f, .13f))); } }
        Material IvyStem { get { return Flat(Tint(art.bark != null ? art.bark : art.timber, new Color(.3f, .25f, .17f))); } }
        Material IvyLeaf { get { return LeafMaterial(art.leafCards[0], new Color(.62f, .75f, .52f)); } }
        Material HedgeLeaf { get { return LeafMaterial(art.leafCards[0], new Color(.5f, .64f, .42f)); } }
        Material Bloom(int k) { var c = new[] { new Color(.92f, .45f, .58f), new Color(.96f, .94f, .88f), new Color(.62f, .44f, .82f), new Color(.82f, .16f, .2f) }; return Flat(Tint(art.foliage, c[Mathf.Abs(k) % c.Length])); }
        Material Flame { get { return Flat(Glowing(new Color(1, .55f, .2f), 2.5f)); } }
        static readonly Color[] IvyGreens = { new Color(.86f, .95f, .8f), new Color(.66f, .8f, .6f), new Color(1f, 1f, .78f) };
        /// <summary>Whether there is leaf-card art for ivy and hedges (the tests' bare scenes may lack it: then no ivy, and the plain parterre).</summary>
        bool IvyArt { get { return art.leafCards != null && art.leafCards.Length > 0 && art.leafCards[0] != null; } }

        // ---------- the texture's own courses ----------
        /// <summary>
        /// The texture coordinates a painted box (ZoneMeshes.Box, laid by Ashlar or BoxPart: metres from the prop's origin over the tile)
        /// carries at a point on its face looking along <paramref name="normal"/>, so a stain or a panel over it keeps the courses running.
        /// <paramref name="centre"/> and <paramref name="rot"/> are the box's in <paramref name="frame"/>.
        /// </summary>
        static Func<Vector3, Vector2> BoxUv(Transform frame, Vector3 centre, Quaternion rot, Vector3 normal, float tile)
        {
            var inv = Quaternion.Inverse(rot); var ln = inv * frame.InverseTransformDirection(normal);
            int face = Mathf.Abs(ln.z) >= Mathf.Abs(ln.x) ? (ln.z < 0 ? 0 : 1) : (ln.x > 0 ? 2 : 3);
            return w =>
            {
                var q = inv * (frame.InverseTransformPoint(w) - centre) + centre;
                switch (face) { case 0: return new Vector2(q.x, q.y) / tile; case 1: return new Vector2(-q.x, q.y) / tile; case 2: return new Vector2(q.z, q.y) / tile; default: return new Vector2(-q.z, q.y) / tile; }
            };
        }
        /// <summary>Metres along a face and up it (the kit's flat-coloured stone, a plinth of a primitive: no courses to keep).</summary>
        static Func<Vector3, Vector2> PlainUv(Vector3 along) { return w => new Vector2(Vector3.Dot(w, along), w.y) / 1.75f; }

        // ---------- weathering ----------
        /// <summary>A band of damp up a face from the ground, a ragged top between <paramref name="lowH"/> and <paramref name="highH"/>
        /// over it, from a to b along the face (y ignored), never over <paramref name="cap"/> nor under <paramref name="floor"/>.</summary>
        void Band(DetailSet d, Material m, Vector3 a, Vector3 b, Vector3 n, float lowH, float highH, float cap, Func<Vector3, Vector2> uvOf, float floor = float.MinValue)
        {
            a.y = b.y = 0; float len = Vector3.Distance(a, b); if (len < .2f) return; int k = Mathf.Max(1, Mathf.CeilToInt(len / 1.1f));
            Vector3 pb = default(Vector3), pt = default(Vector3); bool any = false;
            for (int i = 0; i <= k; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)k) + n * .045f; float g = HeightAt(p.x + n.x * .35f, p.z + n.z * .35f);
                float h = Mathf.Lerp(lowH, highH, Mathf.PerlinNoise(p.x * .7f + 31, p.z * .7f + 7));
                var bot = new Vector3(p.x, Mathf.Max(floor, g - .25f), p.z); var top = new Vector3(p.x, Mathf.Max(bot.y + .05f, Mathf.Min(cap, g + h)), p.z);
                if (i > 0) { d[m].Quad(pb, pt, top, bot, n, uvOf(pb), uvOf(pt), uvOf(top), uvOf(bot)); any = true; }
                pb = bot; pt = top;
            }
            if (any) Tally(d.place + ":weathering");
        }
        /// <summary>A panel of stone a shade off over a face, <paramref name="half"/> either way of <paramref name="mid"/> along it, from y0 to y1.</summary>
        void Panel(DetailSet d, Material m, Vector3 mid, Vector3 n, Vector3 u, float half, float y0, float y1, Func<Vector3, Vector2> uvOf, float off = .03f)
        {
            if (y1 - y0 < .3f) return; mid.y = 0; var c = mid + n * off;
            Vector3 a = c - u * half + Vector3.up * y0, b = c - u * half + Vector3.up * y1, cc = c + u * half + Vector3.up * y1, dd = c + u * half + Vector3.up * y0;
            d[m].Quad(a, b, cc, dd, n, uvOf(a), uvOf(b), uvOf(cc), uvOf(dd)); Tally(d.place + ":weathering");
        }
        /// <summary>A rain streak hanging from <paramref name="top"/> (on the face), <paramref name="w"/> wide there, narrowing as it runs down.</summary>
        void Streak(DetailSet d, Material m, Vector3 top, Vector3 n, Vector3 u, float w, float len, Func<Vector3, Vector2> uvOf)
        {
            top += n * .038f; Vector3 tl = top - u * w / 2, tr = top + u * w / 2, bl = top - Vector3.up * len - u * w * .15f, br = top - Vector3.up * len + u * w * .2f;
            d[m].Quad(bl, tl, tr, br, n, uvOf(bl), uvOf(tl), uvOf(tr), uvOf(br)); Tally(d.place + ":weathering");
        }
        /// <summary>A strip lying on the ground from the line a-b out along <paramref name="n"/>, ragged on its far edge (dirt along a wall's
        /// foot or a road's edge), <paramref name="lift"/> over the ground (over the paving's flags).</summary>
        void GroundStrip(DetailSet d, Material m, Vector3 a, Vector3 b, Vector3 n, float wide, float lift)
        {
            a.y = b.y = 0; float len = Vector3.Distance(a, b); if (len < .2f) return; int k = Mathf.Max(1, Mathf.CeilToInt(len / 1.2f));
            Vector3 pi = default(Vector3), po = default(Vector3);
            for (int i = 0; i <= k; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)k); float w = wide * (.5f + Mathf.PerlinNoise(p.x * .5f + 3, p.z * .5f + 11));
                Vector3 inner = p + n * .02f, outer = p + n * w; inner.y = HeightAt(inner.x, inner.z) + lift; outer.y = HeightAt(outer.x, outer.z) + lift;
                if (i > 0) d[m].Quad(pi, po, outer, inner, Vector3.up, new Vector2(pi.x, pi.z) / 2, new Vector2(po.x, po.z) / 2, new Vector2(outer.x, outer.z) / 2, new Vector2(inner.x, inner.z) / 2);
                pi = inner; po = outer;
            }
            Tally(d.place + ":weathering");
        }

        // ---------- ivy ----------
        /// <summary>
        /// A clump of ivy up a face from <paramref name="foot"/> (on the ground at the face; <paramref name="n"/> the way the face looks,
        /// <paramref name="u"/> along it): three to six strands, each a stem wandering up to between half and all of <paramref name="tall"/>,
        /// fanning out as it climbs; leaf cards a finger off the wall, two a node near the foot, fewer and smaller toward the top; a spread of
        /// leaves on the ground; flowers among them when <paramref name="flowering"/> (a garden's wall). About 500 triangles for a 6 m clump.
        /// </summary>
        void IvyClump(DetailSet d, Vector3 foot, Vector3 n, Vector3 u, float tall, float spread, System.Random r, bool flowering)
        {
            if (!IvyArt || tall < 1.2f) return;
            float R() { return (float)r.NextDouble(); }
            // A mat, not a few strands (2026-10-16, Chris on the preview: "plants not even on the wall. only a few plants"): twice the
            // spread and more strands, each point set on the wall's real face (a ray back along -n: the curtain's battered foot leans, and a
            // stem 3 cm off the foot's line hung in the air above it), leaves lying flat on the stone, three or four a node low down.
            spread *= 1.8f;
            var leafM = IvyLeaf; var stem = d[IvyStem]; int strands = 6 + (int)(spread * 2.5f + R() * 3);
            Vector3 OnWall(Vector3 q) { return Physics.Raycast(q + n * 1.5f, -n, out var hit, 3f) ? hit.point + n * .035f : q; }
            for (int s = 0; s < strands; s++)
            {
                float x = (R() - .5f) * spread, h = tall * (.45f + .55f * R()), y = 0, drift = (R() - .5f) * .5f;
                var prev = OnWall(foot + u * x + n * .03f);
                while (y < h)
                {
                    y += .3f; float f = y / h; x += drift * .3f + (R() - .5f) * .18f; x = Mathf.Clamp(x, -spread * (.5f + f), spread * (.5f + f));
                    var p = OnWall(foot + u * x + Vector3.up * y + n * .03f);
                    var side = Vector3.Cross(n, (p - prev).normalized) * Mathf.Lerp(.035f, .014f, f);
                    stem.Quad(prev - side, p - side, p + side, prev + side, n);
                    int leaves = f < .3f ? 4 : f < .7f ? 3 : (R() < .7f ? 2 : 1);
                    for (int k = 0; k < leaves; k++)
                    {
                        float size = Mathf.Lerp(.55f, .3f, f) * (.8f + .4f * R());
                        var along = (Vector3.up * .9f + n * .08f + u * (R() - .5f) * 1.3f).normalized; var at = p + u * (R() - .5f) * .45f - along * size * .3f;
                        d.Leaf(leafM, at, along, Vector3.Cross(along, n).normalized, size, size, at - n, .2f, IvyGreens[r.Next(IvyGreens.Length)] * (.85f + .25f * R()));
                    }
                    if (flowering && f > .15f && R() < .22f) Blossom(d, p + n * .12f + u * (R() - .5f) * .3f, n, u, .07f + R() * .04f, r.Next(3));
                    prev = p;
                }
            }
            for (int k = 0; k < 3 + (int)(spread * 2); k++)   // the clump's foot: leaves spreading out over the ground
            {
                var along = (n * .7f + Vector3.up * .45f + u * (R() - .5f)).normalized; var at = foot + u * (R() - .5f) * spread * 1.3f + n * (.05f + R() * .2f) + Vector3.up * .03f;
                d.Leaf(leafM, at, along, Vector3.Cross(along, Vector3.up).normalized, .45f, .45f, at - Vector3.up, .5f, IvyGreens[r.Next(IvyGreens.Length)] * .85f);
            }
            Tally(d.place + ":ivy clumps"); Tally("ivy clumps");
        }
        /// <summary>A flower: a small diamond facing both ways (a rose on an arch, a bloom in the ivy of a garden wall).</summary>
        void Blossom(DetailSet d, Vector3 at, Vector3 n, Vector3 u, float size, int colour)
        {
            var b = d[Bloom(colour)]; Vector3 up = Vector3.up * size, side = u.normalized * size;
            b.Quad(at - up, at - side, at + up, at + side, n); b.Quad(at - up, at - side, at + up, at + side, -n);
        }

        // ---------- ironwork ----------
        /// <summary>A chain from a to b, sagging <paramref name="sag"/> in its middle: flat links a turn about from the last, 12 triangles a link.</summary>
        void Chain(DetailSet d, Vector3 a, Vector3 b, float sag)
        {
            float len = Vector3.Distance(a, b); int n = Mathf.Max(2, Mathf.RoundToInt(len / .085f)); var iron = d[Iron];
            for (int i = 0; i < n; i++)
            {
                float f0 = i / (float)n, f1 = (i + 1f) / n;
                Vector3 p0 = Vector3.Lerp(a, b, f0) - Vector3.up * sag * 4 * f0 * (1 - f0), p1 = Vector3.Lerp(a, b, f1) - Vector3.up * sag * 4 * f1 * (1 - f1), dir = (p1 - p0).normalized;
                var side = Vector3.Cross(dir, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right; side.Normalize(); if (i % 2 == 1) side = Vector3.Cross(dir, side).normalized;
                iron.Box((p0 + p1) / 2, new Vector3(.014f, (p1 - p0).magnitude * 1.15f, .045f), Quaternion.LookRotation(side, dir));
            }
            Tally(d.place + ":iron");
        }
        /// <summary>An iron ring (six bars) round <paramref name="axis"/>.</summary>
        void Ring(DetailSet d, Vector3 c, Vector3 axis, float radius)
        {
            var p1 = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized; var p2 = Vector3.Cross(axis, p1).normalized;
            for (int i = 0; i < 6; i++)
            {
                float a0 = i * Mathf.PI / 3, a1 = (i + 1) * Mathf.PI / 3;
                d[Iron].Rod(c + (p1 * Mathf.Cos(a0) + p2 * Mathf.Sin(a0)) * radius, c + (p1 * Mathf.Cos(a1) + p2 * Mathf.Sin(a1)) * radius, .014f);
            }
            Tally(d.place + ":iron");
        }
        /// <summary>A wrought lantern, its top at <paramref name="top"/>: a square hood, glass that glows after dark (the windows' glass), an
        /// iron bar up each corner, a base plate. About 70 triangles.</summary>
        void Lantern(DetailSet d, Vector3 top, float s)
        {
            var iron = d[Iron]; var body = top - Vector3.up * (.1f + s * .62f);
            iron.Frustum(top - Vector3.up * .1f, s * .66f, .03f, .12f, 4, false, true, Mathf.PI / 4);
            iron.Rod(top - Vector3.up * .02f, top + Vector3.up * .06f, .012f);
            d[art.glass].Box(body, new Vector3(s * .8f, s * 1.2f, s * .8f), Quaternion.identity);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                iron.Rod(body + new Vector3(sx * s * .42f, -s * .62f, sz * s * .42f), body + new Vector3(sx * s * .42f, s * .62f, sz * s * .42f), .012f);
            iron.Box(body - Vector3.up * s * .64f, new Vector3(s * .95f, .035f, s * .95f), Quaternion.identity);
            Tally(d.place + ":lanterns");
        }
        /// <summary>A lantern on a wrought bracket from a wall at <paramref name="at"/> (on the face looking along <paramref name="n"/>): a wall
        /// plate, an arm with its brace, the lantern hung from its tip; a faint rust stain run down the wall under the plate.</summary>
        void WallLantern(DetailSet d, Vector3 at, Vector3 n)
        {
            var iron = d[Iron]; var u = Vector3.Cross(Vector3.up, n).normalized;
            iron.Box(at + n * .02f, new Vector3(.14f, .34f, .04f), Quaternion.LookRotation(n));
            var tip = at + n * .5f + Vector3.up * .08f;
            iron.Rod(at + n * .03f + Vector3.up * .08f, tip, .018f);
            iron.Rod(at + n * .03f - Vector3.up * .14f, at + n * .3f + Vector3.up * .08f, .012f);
            iron.Rod(tip, tip + Vector3.up * .06f + n * .05f, .012f);   // the scroll's end
            iron.Rod(tip, tip - Vector3.up * .1f, .01f);   // the hook
            Lantern(d, tip - Vector3.up * .1f, .26f);
            Streak(d, RustStain, at - Vector3.up * .17f, n, u, .12f, .5f, PlainUv(u));
            Tally(d.place + ":iron");
        }
        /// <summary>A lantern on an iron post (up a stair's parapet), the post's foot at <paramref name="foot"/>.</summary>
        void PostLantern(DetailSet d, Vector3 foot, float height)
        {
            var iron = d[Iron]; iron.Box(foot + Vector3.up * .03f, new Vector3(.2f, .06f, .2f), Quaternion.identity);
            iron.Rod(foot, foot + Vector3.up * height, .03f); iron.Box(foot + Vector3.up * height, new Vector3(.14f, .04f, .14f), Quaternion.identity);
            Lantern(d, foot + Vector3.up * (height + .45f), .3f); Tally(d.place + ":iron");
        }
        /// <summary>An iron brazier, its fire lit (the fire's glow is the flame's own; no light of its own): a bowl on three legs, a ring
        /// round its rim, the flames. About 150 triangles.</summary>
        void Brazier(DetailSet d, Vector3 foot)
        {
            var iron = d[Iron];
            for (int k = 0; k < 3; k++) { var dir = Quaternion.Euler(0, k * 120, 0) * Vector3.forward; iron.Rod(foot + dir * .42f, foot + Vector3.up * .82f + dir * .24f, .025f); }
            iron.Frustum(foot + Vector3.up * .78f, .2f, .46f, .3f, 8, false, true);
            iron.Frustum(foot + Vector3.up * 1.06f, .48f, .48f, .05f, 8, false);
            iron.Frustum(foot + Vector3.up * .3f, .3f, .3f, .03f, 6, false);   // the tie round the legs
            d[Rust].Frustum(foot + Vector3.up * .98f, .38f, .34f, .06f, 8, true);   // the coals' bed
            d[Flame].Frustum(foot + Vector3.up * 1.02f, .32f, .04f, .5f, 6, false);
            d[Flame].Frustum(foot + Vector3.up * 1.02f + Vector3.right * .12f, .14f, .02f, .32f, 5, false, false, .4f);
            Tally(d.place + ":iron"); Tally(d.place + ":braziers");
        }
        /// <summary>A low cresting of iron along a coping's top (a to b on its middle line, y its top): posts .8 m apart with a diamond on
        /// each, two rails.</summary>
        void Cresting(DetailSet d, Vector3 a, Vector3 b, float y)
        {
            a.y = b.y = y; float len = Vector3.Distance(a, b); if (len < .3f) return; int n = Mathf.Max(1, Mathf.RoundToInt(len / .8f)); var iron = d[Iron];
            var dir = (b - a).normalized; var q = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, 45);
            iron.Rod(a + Vector3.up * .38f, b + Vector3.up * .38f, .016f); iron.Rod(a + Vector3.up * .1f, b + Vector3.up * .1f, .014f);
            for (int i = 0; i <= n; i++) { var p = Vector3.Lerp(a, b, i / (float)n); iron.Rod(p, p + Vector3.up * .44f, .014f); iron.Box(p + Vector3.up * .48f, new Vector3(.06f, .06f, .02f), q); }
            Tally(d.place + ":iron");
        }
        /// <summary>A handrail on brackets along a wall's face (a to b on the face, y ignored; <paramref name="n"/> the side walked), .92 m over
        /// the ground beside it all the way, so it follows a stair or a ramp.</summary>
        void Handrail(DetailSet d, Vector3 a, Vector3 b, Vector3 n)
        {
            a.y = b.y = 0; float len = Vector3.Distance(a, b); if (len < .5f) return; int k = Mathf.Max(1, Mathf.CeilToInt(len / 1.6f)); var iron = d[Iron];
            Vector3 prev = default(Vector3);
            for (int i = 0; i <= k; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)k); float g = HeightAt(p.x + n.x * .45f, p.z + n.z * .45f);
                var wall = new Vector3(p.x, g + .86f, p.z); var rail = wall + n * .13f + Vector3.up * .06f;
                iron.Box(wall + n * .015f, new Vector3(.08f, .16f, .03f), Quaternion.LookRotation(n)); iron.Rod(wall + n * .02f, rail, .013f);
                if (i > 0) iron.Rod(prev, rail, .024f, 6);
                prev = rail;
            }
            Tally(d.place + ":iron");
        }
        /// <summary>A grille over a window (its middle at <paramref name="c"/> on the face): three uprights, two bars across, .2 m proud.</summary>
        void Grille(DetailSet d, Vector3 c, Vector3 n, Vector3 u, float w, float h, bool rusty)
        {
            var iron = d[rusty ? Rust : Iron]; c += n * .2f;
            foreach (float x in new[] { -.34f, 0, .34f }) iron.Rod(c + u * x * w - Vector3.up * h / 2, c + u * x * w + Vector3.up * (h / 2 + .06f), .014f);
            foreach (float y in new[] { -.25f, .25f }) iron.Rod(c - u * (w / 2 + .04f) + Vector3.up * y * h, c + u * (w / 2 + .04f) + Vector3.up * y * h, .012f);
            Tally(d.place + ":iron");
        }

        // ---------- the pass ----------
        /// <summary>The prop's root as BuildProps made it (by its name where it stands), in the static scenery or the moving props.</summary>
        Transform BuiltProp(ZoneProp p)
        {
            string name = string.IsNullOrEmpty(p.name) ? p.kind : p.name;
            foreach (var parent in new[] { statics, props })
                foreach (Transform c in parent)
                    if (c.name == name && Mathf.Abs(c.position.x - p.at.x) < .3f && Mathf.Abs(c.position.z - p.at.y) < .3f) return c;
            return null;
        }
        /// <summary>
        /// The detail over the Crown (ZoneBuilder.Crown's hook: the last thing BuildPaving does in a city). Each prop's from a stream of its
        /// own; the gardens are their own kind (<see cref="Garden"/>, built with the props).
        /// </summary>
        void BuildCrownDetail()
        {
            Physics.SyncTransforms();   // the walls' colliders this frame, for the ivy to find their real faces (IvyClump)
            if (!Zone.city || Zone.props == null) return;   // (the gardens' counts are in already: they are built with the props)
            Vector2? giltGate = null; foreach (var p in Zone.props) if (p != null && p.kind == "city_gate" && p.variant == 0) giltGate = p.at;
            foreach (var p in Zone.props)
            {
                if (p == null || string.IsNullOrEmpty(p.kind)) continue;
                var t = BuiltProp(p); if (t == null) continue;
                switch (p.kind)
                {
                    case "city_wall": DressWall(t, p, giltGate); break;
                    case "city_gate": DressGate(t, p); break;
                    case "stair": DressStair(t, p); break;
                    case "gilt_house": DressHouse(t, p); break;
                    case "mansion": case "guild_hall": DressHall(t, p); break;
                    case "council_banner": DressBannerPole(t, p); break;
                }
            }
            DressRoadEdges();
        }
        /// <summary>How near the nearest garden is (1 by it, 0 from 14 m past its edge).</summary>
        float NearGarden(Vector3 at)
        {
            float best = 0;
            foreach (var p in Zone.props)
            {
                if (p == null || p.kind != "parterre") continue; var size = p.size.x > 0 ? p.size : new Vector2(12, 8);
                float d = Vector2.Distance(new Vector2(at.x, at.z), p.at) - Mathf.Max(size.x, size.y) / 2;
                best = Mathf.Max(best, 1 - Mathf.Clamp01(d / 14));
            }
            return best;
        }
        /// <summary>The chance of a clump of ivy on a stretch of face looking along <paramref name="n"/>: a little anywhere, more looking
        /// north (in shade), by a garden and round the Gilt Gate (Chris's frame).</summary>
        float IvyChance(float basis, Vector3 n, Vector3 at, Vector2? gate)
        {
            float c = basis * 1.8f + .35f * Mathf.Max(0, n.z) + .35f * NearGarden(at);   // more of it (Chris, 2026-10-16: "only a few plants")
            if (gate.HasValue && Vector2.Distance(new Vector2(at.x, at.z), gate.Value) < 30) c += .3f;
            return Mathf.Min(.85f, c);
        }

        /// <summary>
        /// A run of wall (ZoneBuilder.Crown's CityWall), read back from what it built: each stretch's blocker gives its middle, turn, length,
        /// height and thickness, each tower's its own. The curtain: damp bands and tinted panels on both faces (the battered foot's own, in
        /// its darker stone), a string course on corbels with a slate drip under the wall walk outside and a plainer course inside, rain
        /// streaks under them, a slate cap on the foot's edge, ivy; the towers: damp, ivy, the banner's chains. A parapet: a slate cap on its
        /// coping, damp both sides (moss on a stair's stringers), ivy; a handrail and post lanterns up a stair or ramp, a cresting along the
        /// Crown terrace's edge and the ramp.
        /// </summary>
        void DressWall(Transform t, ZoneProp p, Vector2? gate)
        {
            bool curtain = p.variant == 0, kit = HasCastleKit; float over = curtain ? 11 : 1.15f;
            string name = p.name ?? ""; bool stairish = name.Contains("Stair") || name.Contains("Ramp"), crest = name.Contains("terrace") || name.Contains("Ramp");
            var r = new System.Random(Zone.seed ^ NameHash(name) ^ 0x3a11); float R() { return (float)r.NextDouble(); }
            var d = new DetailSet(t, "wall", "wall"); var stretches = new List<(Transform b, BoxCollider c)>(); var towers = new List<(Transform b, BoxCollider c)>();
            foreach (Transform c in t)
            {
                if (c.name != "Blocker") continue; var box = c.GetComponent<BoxCollider>(); if (box == null) continue;
                if (Mathf.Abs(box.size.x - 7) < .01f && Mathf.Abs(box.size.z - 7) < .01f) towers.Add((c, box)); else stretches.Add((c, box));
            }
            bool NearTower(Vector3 at) { foreach (var tw in towers) if (Vector2.Distance(new Vector2(tw.b.position.x, tw.b.position.z), new Vector2(at.x, at.z)) < 4.8f) return true; return false; }
            int lastIn = -9, lastOut = -9;
            for (int i = 0; i < stretches.Count; i++)
            {
                var (b, c) = stretches[i]; Vector3 C = b.position; var q = b.rotation; float L = c.size.x, H = c.size.y, T = c.size.z;
                Vector3 dir = q * Vector3.right, side = q * Vector3.forward; float y0 = C.y - H / 2, y1 = C.y + H / 2, high = y1 - over;
                float hl = HeightAt(C.x - side.x * (T / 2 + .7f), C.z - side.z * (T / 2 + .7f)), hr = HeightAt(C.x + side.x * (T / 2 + .7f), C.z + side.z * (T / 2 + .7f));
                var outward = hl < hr ? -side : side; float gIn = Mathf.Max(hl, hr), low = Mathf.Min(hl, hr);
                if (curtain) foreach (int s in new[] { -1, 1 }) low = Mathf.Min(low, HeightAt(C.x + s * side.x * (T / 2 + 4.5f), C.z + s * side.z * (T / 2 + 4.5f)));
                bool foot = curtain && high - low > 2.5f;
                float kitH = curtain && kit ? Mathf.Min(L * 1.56f, y1 - y0) : 0, bodyTop = curtain ? y1 - kitH : y1;
                var local = t.InverseTransformPoint(C); var lq = Quaternion.Inverse(t.rotation) * q;

                // The face on the high side (the street, the stair, the terrace above).
                var nIn = -outward; var fIn = C + nIn * (T / 2); fIn.y = 0; var uvIn = BoxUv(t, local, lq, nIn, 1.75f);
                Band(d, stairish ? MossStone : Damp, fIn - dir * L / 2, fIn + dir * L / 2, nIn, .35f, 1.1f, bodyTop, uvIn);
                if (R() < .5f) Panel(d, StoneTint(r.Next(3)), fIn, nIn, dir, L / 2, gIn + .5f + R(), Mathf.Min(bodyTop - .05f, gIn + 2.2f + R() * 3), uvIn);
                if (curtain) for (int k = 0, n = r.Next(3); k < n; k++) Streak(d, StreakStone, new Vector3(fIn.x, bodyTop - .3f, fIn.z) + dir * (R() - .5f) * L * .8f, nIn, dir, .25f + R() * .35f, 1 + R() * 2.5f, uvIn);
                if (Paved(new Vector2(fIn.x + nIn.x, fIn.z + nIn.z))) GroundStrip(d, Dirt, fIn - dir * L / 2, fIn + dir * L / 2, nIn, .55f, .06f);
                var footIn = fIn + dir * (R() - .5f) * L * .6f; footIn.y = HeightAt(footIn.x + nIn.x * .3f, footIn.z + nIn.z * .3f);
                if (!NearTower(footIn) && i - lastIn > 1 && R() < IvyChance(curtain ? .1f : .05f, nIn, footIn, gate))
                { IvyClump(d, footIn, nIn, dir, Mathf.Min(curtain ? 8 : 2.6f, (bodyTop - footIn.y) * .8f), 1 + R(), r, NearGarden(footIn) > .5f); lastIn = i; }

                // The face on the low side: the curtain's battered foot down to the ground, the wall above it; a parapet's outer face.
                if (foot)
                {
                    float footTop = high - .35f; var ff = C + outward * (T / 2 + 3.5f); ff.y = 0; float gF = HeightAt(ff.x + outward.x * .6f, ff.z + outward.z * .6f);
                    var uvFoot = BoxUv(t, t.InverseTransformPoint(C + outward * (T / 2 + 1.75f)), lq, outward, 1.75f);
                    Band(d, DampFoot, ff - dir * L / 2, ff + dir * L / 2, outward, .5f, 1.6f, footTop, uvFoot);
                    if (R() < .45f) Panel(d, ShadeTint(r.Next(2)), ff, outward, dir, L / 2, gF + .8f + R() * 2, Mathf.Min(footTop - .1f, gF + 3 + R() * 6), uvFoot);
                    d[DarkSlate].Box(new Vector3(ff.x, footTop + .03f, ff.z) - outward * .25f, new Vector3(L + .02f, .06f, .62f), q);   // the slate cap on its edge
                    for (int k = 0, n = 1 + r.Next(2); k < n; k++) Streak(d, DampFoot, new Vector3(ff.x, footTop - .02f, ff.z) + dir * (R() - .5f) * L * .8f, outward, dir, .3f + R() * .4f, 1.5f + R() * 3, uvFoot);
                    var footOut = ff + dir * (R() - .5f) * L * .6f; footOut.y = gF;
                    if (!NearTower(footOut) && i - lastOut > 1 && footTop - gF > 1.5f && R() < IvyChance(.12f, outward, footOut, gate))
                    { IvyClump(d, footOut, outward, dir, Mathf.Min(9, (footTop - gF) * .85f), 1.1f + R() * 1.2f, r, NearGarden(footOut) > .5f); lastOut = i; }
                    var fw = C + outward * (T / 2); fw.y = 0; var uvOut = BoxUv(t, local, lq, outward, 1.75f);
                    if (R() < .5f) Panel(d, StoneTint(r.Next(3)), fw, outward, dir, L / 2, footTop + .3f, bodyTop - .4f - R() * 2, uvOut);
                }
                else
                {
                    var fo = C + outward * (T / 2); fo.y = 0; float gO = HeightAt(fo.x + outward.x * .6f, fo.z + outward.z * .6f); var uvOut = BoxUv(t, local, lq, outward, 1.75f);
                    Band(d, curtain ? DampFoot : Damp, fo - dir * L / 2, fo + dir * L / 2, outward, .45f, 1.4f, bodyTop, uvOut);
                    if (R() < .5f) Panel(d, StoneTint(r.Next(3)), fo, outward, dir, L / 2, gO + .6f + R(), Mathf.Min(bodyTop - .05f, gO + 2.5f + R() * 4), uvOut);
                    var footOut = fo + dir * (R() - .5f) * L * .6f; footOut.y = gO;
                    if (!NearTower(footOut) && i - lastOut > 1 && R() < IvyChance(curtain ? .12f : .08f, outward, footOut, gate))
                    { IvyClump(d, footOut, outward, dir, Mathf.Min(curtain ? 8 : 6, (bodyTop - gO) * .8f), 1 + R(), r, NearGarden(footOut) > .5f); lastOut = i; }
                }

                if (curtain)
                {
                    // The string course on its corbels under the wall walk (outside) with its slate drip, a plainer course inside.
                    var o = C + outward * (T / 2); o.y = bodyTop;
                    d[TrimStone].Box(o + outward * .16f - Vector3.up * .17f, new Vector3(L + .02f, .34f, .32f), q);
                    d[DarkSlate].Box(o + outward * .19f + Vector3.up * .025f, new Vector3(L + .02f, .05f, .38f), q);
                    for (float a = -L / 2 + .45f; a < L / 2 - .2f; a += 1.15f) d[TrimStone].Box(o + dir * a + outward * .14f - Vector3.up * .58f, new Vector3(.3f, .48f, .28f), q);
                    var o2 = C - outward * (T / 2); o2.y = bodyTop; d[TrimStone].Box(o2 - outward * .1f - Vector3.up * .12f, new Vector3(L + .02f, .24f, .2f), q);
                    var uvOut = BoxUv(t, local, lq, outward, 1.75f);
                    for (int k = 0, n = r.Next(3); k < n; k++) Streak(d, StreakStone, o - Vector3.up * .82f + dir * (R() - .5f) * L * .8f, outward, dir, .2f + R() * .3f, 1 + R() * 2.5f, uvOut);
                    Tally("wall:trim");
                }
                else
                {
                    d[DarkSlate].Box(new Vector3(C.x, y1 + .17f, C.z), new Vector3(L + .03f, .06f, T + .26f), q);   // the slate cap on the coping
                    Tally("wall:trim");
                    if (crest) Cresting(d, new Vector3(C.x, 0, C.z) - dir * (L / 2 - .05f), new Vector3(C.x, 0, C.z) + dir * (L / 2 - .05f), y1 + .2f);
                    if (stairish && i % 2 == 1) PostLantern(d, new Vector3(C.x, y1 + .2f, C.z), .5f);
                }
            }
            // Up a stair or a ramp: a handrail on brackets along the parapet's walked face, the whole run.
            if (!curtain && stairish && p.points != null)
                for (int i = 0; i + 1 < p.points.Length; i++)
                {
                    Vector3 a = new Vector3(p.points[i].x, 0, p.points[i].y), b = new Vector3(p.points[i + 1].x, 0, p.points[i + 1].y), dir = (b - a).normalized, side = new Vector3(-dir.z, 0, dir.x);
                    var mid = (a + b) / 2; var n = HeightAt(mid.x + side.x * 1.1f, mid.z + side.z * 1.1f) > HeightAt(mid.x - side.x * 1.1f, mid.z - side.z * 1.1f) ? side : -side;
                    Handrail(d, a + n * .35f + dir * .3f, b + n * .35f - dir * .3f, n);
                }
            foreach (var (b, c) in towers) DressTower(d, t, b, c, kit, r, gate);
            Flush(d);
        }
        /// <summary>A tower of the curtain wall: damp at its foot inside and down the cliff, ivy (more looking north or by a garden), the
        /// Council banner's rod chained to rings in its face.</summary>
        void DressTower(DetailSet d, Transform t, Transform b, BoxCollider c, bool kit, System.Random r, Vector2? gate)
        {
            float R() { return (float)r.NextDouble(); }
            Vector3 C = b.position; var q = b.rotation; Vector3 side = q * Vector3.forward, along = q * Vector3.right; float y1 = C.y + c.size.y / 2;
            float hl = HeightAt(C.x - side.x * 5, C.z - side.z * 5), hr = HeightAt(C.x + side.x * 5, C.z + side.z * 5), high = Mathf.Max(hl, hr);
            var outward = hl < hr ? -side : side; float face = kit ? 3.7f : 3.5f;
            var fIn = C - outward * face; fIn.y = 0; float gIn = HeightAt(fIn.x - outward.x * .6f, fIn.z - outward.z * .6f);
            Band(d, Damp, fIn - along * 3.6f, fIn + along * 3.6f, -outward, .5f, 1.3f, y1, PlainUv(along));
            var footIn = fIn + along * (R() - .5f) * 3.6f; footIn.y = gIn;
            if (R() < IvyChance(.3f, -outward, footIn, gate)) IvyClump(d, footIn, -outward, along, Mathf.Min(9, (y1 - gIn) * .55f), 1.4f + R(), r, NearGarden(footIn) > .5f);
            var fOut = C + outward * 3.5f; fOut.y = 0; float gOut = HeightAt(fOut.x + outward.x * .7f, fOut.z + outward.z * .7f), shaftTop = kit ? high - 2 : y1;
            var uvOut = BoxUv(t, t.InverseTransformPoint(C), Quaternion.Inverse(t.rotation) * q, outward, 1.75f);
            Band(d, DampFoot, fOut - along * 3.5f, fOut + along * 3.5f, outward, .6f, 1.8f, shaftTop, uvOut);
            var footOut = fOut + along * (R() - .5f) * 3.6f; footOut.y = gOut;
            if (shaftTop - gOut > 2 && R() < IvyChance(.3f, outward, footOut, gate)) IvyClump(d, footOut, outward, along, Mathf.Min(9, (shaftTop - gOut) * .8f), 1.4f + R(), r, false);
            // The banner (ZoneBuilder.Crown's WallTower: its rod 1.3 m either way, 2.2 m under the top, 3.8 m out) chained up to two rings.
            float bf = kit ? 3.8f : 3.56f; var rod = C + outward * bf; rod.y = y1 - 2.2f;
            foreach (int s in new[] { -1, 1 })
            {
                var ring = C + outward * (bf - .04f) + along * s * .75f; ring.y = y1 - 1.25f;
                Ring(d, ring, outward, .07f); Chain(d, rod + along * s * 1.25f, ring - Vector3.up * .07f, .04f);
            }
        }

        /// <summary>
        /// A gate (ZoneBuilder.Crown's CityGate, in its own frame: the passage along z, its front -Z out of the city, the towers either side):
        /// iron braziers before the towers, iron frames and brackets on its lanterns, strap hinges, studs and a ring on both leaves, the
        /// banners chained to rings, damp at the towers' feet and in the passage, rain streaks under the gilt band, dirt along the passage
        /// walls, ivy up the tower on the north side (a big clump) and a smaller one on the other.
        /// </summary>
        void DressGate(Transform t, ZoneProp p)
        {
            float w = p.size.x > 0 ? p.size.x : 6; const float D = 8, tw = 5, top = 12.5f; bool kit = HasCastleKit; float under = kit ? 5.8f : 7.2f, front = kit ? D / 2 + .1f : D / 2;
            var r = CityRandom(t, 0x6a7e); float R() { return (float)r.NextDouble(); }
            var d = new DetailSet(t, "gate", "gate"); Vector3 L(float x, float y, float z) { return t.TransformPoint(new Vector3(x, y, z)); }
            Vector3 fwd = t.TransformDirection(Vector3.forward), right = t.TransformDirection(Vector3.right);
            int north = t.TransformDirection(Vector3.right).z >= 0 ? 1 : -1;   // the tower on the north side, in shade, takes the big clump
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (w / 2 + tw / 2), x0 = s * (w / 2 - .02f), x1 = s * (w / 2 + tw + .2f);
                // the braziers, before the towers by the passage
                float bx = s * (w / 2 + .95f), bz = -front - 1.15f; Brazier(d, L(bx, LocalGround(t, bx, bz), bz));
                // the lantern (ZoneBuilder.Crown's: glass at (w/2 + .3, 4.2, -D/2 - .4) under a gilt cap): an iron frame and a bracket to the tower
                var lantern = L(s * (w / 2 + .3f), 4.2f, -D / 2 - .4f);
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) d[Iron].Rod(lantern + right * sx * .22f + fwd * sz * .22f - Vector3.up * .3f, lantern + right * sx * .22f + fwd * sz * .22f + Vector3.up * .32f, .014f);
                d[Iron].Rod(lantern + Vector3.up * .42f, L(s * (w / 2 + .3f), 4.62f, -front), .02f); d[Iron].Rod(L(s * (w / 2 + .3f), 4.2f, -front), lantern + Vector3.up * .3f - fwd * .1f, .013f);
                d[Iron].Box(L(s * (w / 2 + .3f), 4.45f, -front - .02f), new Vector3(.16f, .5f, .04f), t.rotation);
                Tally("gate:iron");
                // the towers' faces: damp at the foot, streaks under the gilt band
                foreach (int f in new[] { -1, 1 })
                {
                    var n = fwd * f; var u = right;
                    Band(d, DampFoot, L(x0, 0, f * front), L(x1, 0, f * front), n, .5f, 1.4f, L(0, top, 0).y, PlainUv(u));
                    for (int k = 0, m = 1 + r.Next(2); k < m; k++) Streak(d, StreakStone, L(x + (R() - .5f) * tw * .7f, top + .82f, f * front), n, u, .25f + R() * .3f, 1.2f + R() * 2.5f, PlainUv(u));
                    // the banners (ZoneBuilder.Crown's: their rods 1.3 m either way at top + 2.5) chained to rings in the face
                    foreach (int k in new[] { -1, 1 })
                    {
                        var ring = L(x + k * .8f, top + 3.35f, f * (front + .04f)); Ring(d, ring, n, .07f);
                        Chain(d, L(x + k * 1.25f, top + 2.5f, f * (D / 2 + .1f)), ring - Vector3.up * .07f, .05f);
                    }
                    // ivy: the north tower's front a big clump, the other's back a small one
                    bool big = s == north && f < 0, small = s != north && f > 0;
                    if (big || small)
                    {
                        float fx = s * (w / 2 + (big ? 2.2f : 1.8f)), fz = f * (front + .02f); var footAt = L(fx, LocalGround(t, fx, fz + f * .5f), fz);
                        IvyClump(d, footAt, n, u, big ? 7.5f : 3.6f, big ? 1.6f : .9f, r, false);
                    }
                }
                // the passage's walls: damp and dirt
                var pn = -right * s;
                Band(d, Damp, L(s * w / 2, 0, -D / 2), L(s * w / 2, 0, D / 2), pn, .4f, 1.2f, L(0, under, 0).y, PlainUv(fwd));
                GroundStrip(d, Dirt, L(s * w / 2, 0, -D / 2), L(s * w / 2, 0, D / 2), pn, .5f, .09f);
            }
            // the leaves: strap hinges with spear ends and studs outside, plain straps and studs inside, a ring to pull
            float leaf = Mathf.Min(6.6f, under - .4f), lw = w / 2 - .05f;
            foreach (Transform h in t)
            {
                if (h.name != "Gate hinge") continue;
                float s = h.localPosition.x < 0 ? -1 : 1; var hd = new DetailSet(h, "gate", "gate leaf");
                Vector3 H(float x, float y, float z) { return h.TransformPoint(new Vector3(x, y, z)); }
                var across = h.TransformDirection(Vector3.right * -s); var hq = h.rotation;
                for (int k = 0; k < 3; k++)
                {
                    float y = .8f + (k + .5f) * (leaf - 1.2f) / 3, strap = lw - .35f;
                    hd[Iron].Box(H(-s * strap / 2, y, -.14f), new Vector3(strap, .13f, .03f), hq);
                    hd[Iron].Box(H(-s * strap, y, -.14f) + across * .03f, new Vector3(.13f, .13f, .03f), hq * Quaternion.Euler(0, 0, 45));
                    hd[Iron].Rod(H(0, y - .22f, -.06f), H(0, y + .22f, -.06f), .055f, 6);   // the knuckle round the pin
                    hd[Iron].Box(H(-s * strap / 2, y, .14f), new Vector3(strap, .11f, .03f), hq);
                    foreach (float row in new[] { .25f, .75f })
                    {
                        float sy = .8f + (k + row) * (leaf - 1.2f) / 3;
                        for (int col = 0; col < 4; col++)
                        {
                            float sx = -s * (.35f + col * (lw - .7f) / 3);
                            hd[R() < .2f ? Rust : Iron].Box(H(sx, sy, -.135f), new Vector3(.07f, .07f, .04f), hq * Quaternion.Euler(0, 0, 45));
                            hd[Iron].Box(H(sx, sy, .135f), new Vector3(.06f, .06f, .03f), hq * Quaternion.Euler(0, 0, 45));
                        }
                    }
                }
                Ring(hd, H(-s * (lw - .4f), 1.3f, -.17f), h.TransformDirection(Vector3.back), .1f);
                hd[Iron].Box(H(-s * (lw - .4f), 1.42f, -.135f), new Vector3(.12f, .12f, .03f), hq);
                Tally("gate:iron"); Flush(hd);
            }
            Flush(d);
        }

        /// <summary>A stair (ZoneBuilder.Crown's Stair, its treads along its local z rising): moss on the treads' ends by the stringers and on
        /// some risers' corners, a little along a nosing here and there.</summary>
        void DressStair(Transform t, ZoneProp p)
        {
            var size = p.size.x > 0 ? p.size : new Vector2(10, 20); var r = CityRandom(t, 0x57a1); float R() { return (float)r.NextDouble(); }
            var d = new DetailSet(t, "stair", "stair"); var moss = MossStone; var up = t.TransformDirection(Vector3.up); var back = t.TransformDirection(Vector3.back);
            Vector3 L(float x, float y, float z) { return t.TransformPoint(new Vector3(x, y, z)); }
            int n = Mathf.Max(1, Mathf.RoundToInt(size.y / .45f)); float go = size.y / n, prevHigh = 0, hx = size.x / 2;
            for (int k = 0; k < n; k++)
            {
                float z0 = -size.y / 2 + k * go, z1 = z0 + go, high = Mathf.Max(LocalGround(t, 0, z1), LocalGround(t, 0, z0)) + .02f;
                foreach (int s in new[] { -1, 1 })
                {
                    if (R() < .5f)
                    {
                        float xi = s * (hx - .25f - R() * .55f), xo = s * (hx - .03f), zb = z0 + go * (.35f + R() * .6f), y = high + .012f;
                        d[moss].Quad(L(xi, y, z0 + .02f), L(xi, y, zb), L(xo, y, zb), L(xo, y, z0 + .02f), up, new Vector2(xi, z0), new Vector2(xi, zb), new Vector2(xo, zb), new Vector2(xo, z0));
                        Tally("stair:moss");
                    }
                    if (k > 0 && R() < .35f && high - prevHigh > .05f)
                    {
                        float xi = s * (hx - .2f - R() * .35f), xo = s * (hx - .03f);
                        d[moss].Quad(L(xi, prevHigh - .02f, z0 - .012f), L(xi, high, z0 - .012f), L(xo, high, z0 - .012f), L(xo, prevHigh - .02f, z0 - .012f), back);
                        Tally("stair:moss");
                    }
                }
                if (R() < .18f)
                {
                    float xa = (R() - .5f) * (size.x - 2), xb = xa + .5f + R(), y = high + .012f;
                    d[moss].Quad(L(xa, y, z0 + .01f), L(xa, y, z0 + .1f), L(xb, y, z0 + .1f), L(xb, y, z0 + .01f), up); Tally("stair:moss");
                }
                prevHigh = high;
            }
            Flush(d);
        }

        /// <summary>
        /// A gilt house (ZoneBuilder.Crown's GiltHouse; its faces look along -Z, +Z, +X, -X of its frame): damp on its plinth, a storey of
        /// stone a shade off here and there between its string courses, rain streaks under some sills, grilles over the ground-floor
        /// windows, iron bands and a ring on the door, a lantern on a bracket beside it, and on some an ivy clump up a side or a corner.
        /// </summary>
        void DressHouse(Transform t, ZoneProp p)
        {
            var size = CitySize(p); float w = size.x, dd = size.y, storey = 3.3f, walls = storey * (p.variant == 3 ? 4 : 3);
            var r = CityRandom(t, 0x5d3e); float R() { return (float)r.NextDouble(); }
            var d = new DetailSet(t, "house", "house"); Vector3 L(float x, float y, float z) { return t.TransformPoint(new Vector3(x, y, z)); }
            int ivyFace = R() < .4f + .3f * NearGarden(t.position) ? 2 + r.Next(2) : -1;
            for (int f = 0; f < 4; f++)
            {
                float face = f < 2 ? dd / 2 : w / 2, run = f < 2 ? w : dd, yaw = f == 0 ? 0 : f == 1 ? 180 : f == 2 ? 270 : 90; var q = Quaternion.Euler(0, yaw, 0);
                var n = t.TransformDirection(q * Vector3.back); var u = t.TransformDirection(q * Vector3.right); var mid = t.TransformPoint(q * new Vector3(0, 0, -face));
                var uv = BoxUv(t, new Vector3(0, walls / 2, 0), Quaternion.identity, n, 2);
                var plinth = mid + n * .15f; Band(d, Damp, plinth - u * (run / 2 + .15f), plinth + u * (run / 2 + .15f), n, .3f, .65f, L(0, .7f, 0).y, PlainUv(u));
                for (int s = 0; s * storey < walls - .1f; s++) if (R() < .4f) Panel(d, StoneTint(r.Next(3)), mid, n, u, run / 2, L(0, s * storey + .08f, 0).y, L(0, (s + 1) * storey - .08f, 0).y, uv, .02f);
                for (int s = 0; s * storey < walls - .1f; s++)
                    for (float x = -run / 2 + 1.5f; x <= run / 2 - 1.4f; x += 2.2f)
                    {
                        if (s == 0 && f == 0 && Mathf.Abs(x) < 1.3f) continue;   // the door
                        var win = t.TransformPoint(q * new Vector3(x, 0, -face) + Vector3.up * (s * storey + 1.8f));
                        if (R() < .35f) Streak(d, StreakStone, win - Vector3.up * .9f, n, u, .5f, .5f + R() * 1.1f, uv);
                        if (s == 0) Grille(d, win, n, u, .8f, 1.5f, R() < .2f);
                    }
                if (f == ivyFace)
                {
                    float x = (R() < .5f ? -1 : 1) * (run / 2 - .7f); var foot = t.TransformPoint(q * new Vector3(x, 0, -face)); foot.y = HeightAt(foot.x + n.x * .3f, foot.z + n.z * .3f);
                    IvyClump(d, foot, n, u, Mathf.Min(walls * .8f, 3 + R() * 6), .9f + R() * .6f, r, NearGarden(foot) > .5f);
                }
            }
            // The door: iron bands and a ring; a lantern on a bracket beside it.
            var front = t.TransformDirection(Vector3.back);
            foreach (float y in new[] { .55f, 1.3f, 2.05f }) d[Iron].Box(L(0, y, -dd / 2 - .135f), new Vector3(1.2f, .07f, .03f), t.rotation);
            Ring(d, L(.4f, 1.22f, -dd / 2 - .17f), front, .07f);
            WallLantern(d, L((R() < .5f ? -1 : 1) * 1.15f, 3.3f, -dd / 2), front);
            Tally("house:iron"); Flush(d);
        }

        /// <summary>
        /// A walk-in hall (ZoneBuilder.Crown's HallShell: the Exchange, the Treasury Vault, the Hall of Arms, the Armoury, the guild halls):
        /// damp on its plinth course (not across the doorway), a panel a shade off on the back and sides, lanterns on brackets either side of
        /// the door, an ivy clump up a side or the back. Grouped a face at a time, so each fades with its own wall (RoofFade) and nothing of
        /// it reaches into the room.
        /// </summary>
        void DressHall(Transform t, ZoneProp p)
        {
            var size = CitySize(p); float w = size.x, dd = size.y, wallH = p.kind == "mansion" ? 10 : 8.5f, doorW = 3.2f;
            var r = CityRandom(t, 0x4a11); float R() { return (float)r.NextDouble(); }
            int ivyFace = R() < .6f + .3f * NearGarden(t.position) ? 1 + r.Next(3) : -1;
            for (int f = 0; f < 4; f++)
            {
                float face = f < 2 ? dd / 2 : w / 2, run = f < 2 ? w : dd, yaw = f == 0 ? 0 : f == 1 ? 180 : f == 2 ? 270 : 90; var q = Quaternion.Euler(0, yaw, 0);
                var n = t.TransformDirection(q * Vector3.back); var u = t.TransformDirection(q * Vector3.right);
                var d = new DetailSet(t, "hall", "hall " + (f == 0 ? "front" : f == 1 ? "back" : "side " + f));
                var plinth = t.TransformPoint(q * new Vector3(0, 0, -face - .245f)); float cap = t.TransformPoint(new Vector3(0, .6f, 0)).y;
                if (f == 0) foreach (int s in new[] { -1, 1 }) Band(d, Damp, plinth + u * s * (doorW / 2 + .25f), plinth + u * s * (run / 2 + .15f), n, .25f, .55f, cap, PlainUv(u));
                else Band(d, Damp, plinth - u * (run / 2 + .15f), plinth + u * (run / 2 + .15f), n, .25f, .55f, cap, PlainUv(u));
                if (f > 0 && R() < .6f)
                {
                    var wallCentre = f == 1 ? new Vector3(0, wallH / 2, dd / 2) : new Vector3((f == 2 ? 1 : -1) * w / 2, wallH / 2, 0);
                    var mid = t.TransformPoint(q * new Vector3(0, 0, -face)); var bottom = t.TransformPoint(new Vector3(0, .7f + R() * 2, 0)).y;
                    Panel(d, StoneTint(r.Next(3)), mid, n, u, run / 2 - .1f, bottom, bottom + 2 + R() * 4, BoxUv(t, wallCentre, Quaternion.identity, n, 2), .28f);
                }
                if (f == 0) foreach (int s in new[] { -1, 1 }) WallLantern(d, t.TransformPoint(new Vector3(s * 2.1f, 3.6f, -dd / 2 - .25f)), n);
                if (f == ivyFace)
                {
                    var foot = t.TransformPoint(q * new Vector3((R() - .5f) * (run - 3), 0, -face - .3f)); foot.y = HeightAt(foot.x + n.x * .3f, foot.z + n.z * .3f);
                    IvyClump(d, foot, n, u, Mathf.Min(wallH * .6f, 3 + R() * 3), 1 + R() * .6f, r, NearGarden(foot) > .5f);
                }
                Flush(d);
            }
        }

        /// <summary>A Council banner's pole (ZoneBuilder.Crown's CouncilBannerPole): two iron collars, the rod chained up to the pole's head.</summary>
        void DressBannerPole(Transform t, ZoneProp p)
        {
            float height = p.size.y > 0 ? p.size.y : 7; var d = new DetailSet(t, "banner", "banner"); Vector3 L(float x, float y, float z) { return t.TransformPoint(new Vector3(x, y, z)); }
            foreach (float y in new[] { 1.4f, height - 1 }) d[Iron].Frustum(L(0, y, 0), .085f, .085f, .1f, 6, false);
            foreach (int s in new[] { -1, 1 }) Chain(d, L(s * .9f, height - .42f, -.14f), L(s * .1f, height - .06f, -.1f), .03f);
            Tally("banner:iron"); Flush(d);
        }

        /// <summary>Dirt along both edges of a city's roads where they run level (on the Gilt Ramp and the Grand Stair the stair carries it).</summary>
        void DressRoadEdges()
        {
            var root = new GameObject("Road edges").transform; root.SetParent(statics, false); var d = new DetailSet(root, "road", "road edges");
            foreach (var rd in Zone.roads)
            {
                if (rd == null || rd.points == null || rd.points.Length < 2) continue;
                var pts = Densify(rd.points, 1.25f);
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1], dir = (b - a).normalized, side = new Vector2(-dir.y, dir.x);
                    if (Mathf.Abs(HeightAt(a.x, a.y) - HeightAt(b.x, b.y)) > .15f) continue;
                    foreach (int s in new[] { -1, 1 })
                    {
                        Vector2 ea = a + side * s * (rd.width / 2 - .02f), eb = b + side * s * (rd.width / 2 - .02f);
                        GroundStrip(d, Dirt, new Vector3(ea.x, 0, ea.y), new Vector3(eb.x, 0, eb.y), new Vector3(-side.x * s, 0, -side.y * s), .3f, .085f);
                    }
                }
            }
            Flush(d);
        }

        // ---------- the gardens ----------
        /// <summary>A garden's plan, from its middle out: the clipped hedge round it (.6 m), a gravel walk inside it (1 m), the cross walks
        /// (1 m either way of the middle), and in each quarter its beds (.9 m) round a lawn.</summary>
        const float GardenHedge = .6f, GardenWalk = 1f, GardenCross = 1f, GardenBed = .9f;
        /// <summary>Whether a point in a garden's own frame (from its middle) lies on one of its lawns (none in a quarter too narrow for one).</summary>
        public static bool InGardenLawn(Vector2 size, Vector2 local)
        {
            float x = Mathf.Abs(local.x), z = Mathf.Abs(local.y), lo = GardenCross + GardenBed;
            float hiX = size.x / 2 - GardenHedge - GardenWalk - GardenBed, hiZ = size.y / 2 - GardenHedge - GardenWalk - GardenBed;
            return hiX - lo >= .8f && hiZ - lo >= .8f && x > lo && x < hiX && z > lo && z < hiZ;
        }
        List<(Vector2 at, float c, float s, Vector2 size)> gardenPlans;
        /// <summary>Whether a point lies on a garden's lawn: the zone's grass grows there though the terrace is paved (ZoneBuilder.Crown's Paved).</summary>
        bool GardenLawn(Vector2 p)
        {
            if (gardenPlans == null)
            {
                gardenPlans = new List<(Vector2, float, float, Vector2)>();
                if (Zone.props != null) foreach (var g in Zone.props)
                    if (g != null && g.kind == "parterre") { float a = g.rotation * Mathf.Deg2Rad; gardenPlans.Add((g.at, Mathf.Cos(a), Mathf.Sin(a), g.size.x > 0 ? g.size : new Vector2(12, 8))); }
            }
            foreach (var g in gardenPlans)
            {
                var d = p - g.at; float reach = (g.size.x + g.size.y) / 2; if (Mathf.Abs(d.x) > reach || Mathf.Abs(d.y) > reach) continue;
                if (InGardenLawn(g.size, new Vector2(d.x * g.c - d.y * g.s, d.x * g.s + d.y * g.c))) return true;
            }
            return false;
        }
        static readonly string[] GardenFlowers = { "Nature/Flower_3_Group", "Nature/Flower_4_Group", "Megapack/Models/Plants/Flower_1_Multi", "Megapack/Models/Plants/Flower_2_Multi" };
        /// <summary>One of the packs' plants in a garden (no collider), its triangles counted; false if the pack is not there.</summary>
        bool GardenPlant(Transform t, string path, Vector3 local, float yaw, float height)
        {
            if (!HasProp(path)) return false;
            var go = ModelProp(t, path, local, yaw, height); if (go == null) return false;
            if (!path.StartsWith("Nature/")) DressNature(go);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) Tally("triangles", (int)(mf.sharedMesh.GetIndexCount(k) / 3));
            Tally("garden:plants"); return true;
        }
        /// <summary>Leaf cards over a clipped shape round an upright axis (a topiary): <paramref name="count"/> of them on its skin, the
        /// radius at a height from <paramref name="radiusAt"/>.</summary>
        void LeafSkin(DetailSet d, Material m, Vector3 foot, float h, Func<float, float> radiusAt, int count, System.Random r)
        {
            for (int k = 0; k < count; k++)
            {
                float y = (float)r.NextDouble() * h, a = (float)r.NextDouble() * Mathf.PI * 2; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var axis = foot + Vector3.up * y; var at = axis + dir * (radiusAt(y) + .02f); var along = (Vector3.up * .6f + dir * .4f + Vector3.Cross(Vector3.up, dir) * ((float)r.NextDouble() - .5f)).normalized;
                d.Leaf(m, at - along * .1f, along, Vector3.Cross(Vector3.up, dir), .32f, .32f, axis, .3f, IvyGreens[r.Next(IvyGreens.Length)] * .8f);
            }
        }
        /// <summary>A clipped hedge: a box of green with leaf cards over its top and both sides (the painted leaf card), so it reads as box
        /// leaves; <paramref name="c"/> its middle at its foot, <paramref name="u"/> along it.</summary>
        void Hedge(DetailSet d, Vector3 c, Vector3 u, float len, float wide, float high, System.Random r)
        {
            var q = Quaternion.LookRotation(Vector3.Cross(u, Vector3.up).normalized); var across = q * Vector3.forward; u = q * Vector3.right;   // its x along u
            d[HedgeBody].Box(c + Vector3.up * high / 2, new Vector3(len, high, wide), q);
            var leaf = HedgeLeaf; int n = Mathf.Max(2, Mathf.RoundToInt(len * 3));
            for (int k = 0; k < n; k++)
            {
                float a = ((float)r.NextDouble() - .5f) * len, b = ((float)r.NextDouble() - .5f) * wide * .8f;
                var at = c + u * a + across * b + Vector3.up * (high + .01f); var along = (u * ((float)r.NextDouble() - .5f) * 2 + across * ((float)r.NextDouble() - .5f) + Vector3.up * .25f).normalized;
                d.Leaf(leaf, at - along * .12f, along, Vector3.Cross(along, Vector3.up).normalized, .3f, .3f, at - Vector3.up, .7f, IvyGreens[r.Next(IvyGreens.Length)] * .85f);
                foreach (int s in new[] { -1, 1 })
                {
                    float y = (float)r.NextDouble() * high * .85f + .05f; var sp = c + u * (((float)r.NextDouble() - .5f) * len) + across * s * (wide / 2 + .01f) + Vector3.up * y;
                    var sAlong = (Vector3.up * .7f + u * ((float)r.NextDouble() - .5f) + across * s * .25f).normalized;
                    d.Leaf(leaf, sp - sAlong * .12f, sAlong, u, .3f, .3f, sp - across * s, .2f, IvyGreens[r.Next(IvyGreens.Length)] * .8f);
                }
            }
        }
        /// <summary>
        /// A garden of the Crown (the kind "parterre"; Chris, 2026-10-16: "we need a real garden. need to use some of our texture packs").
        /// <paramref name="size"/> across (x) and deep (y). A clipped box hedge round it in leaf cards with a gap in each side's middle, a
        /// gravel walk inside it and two crossing; in each quarter beds of the packs' flowering plants (each quarter one kind, a drift of one
        /// colour, flowering bushes at its corners) edged in dwarf box round a lawn the zone's grass grows in, with a topiary on a stone pot
        /// or a small tree in its middle; an urn of flowers on a pedestal where the walks cross (four more by the side gaps of a big garden),
        /// stone benches on the end walks, a rose arch over each end gap with a vine of leaves and roses up and over it. Nothing solid: it is
        /// walked through. Without leaf-card art (the tests' bare scenes), the plain parterre.
        /// </summary>
        void Garden(Transform t, Vector2 size)
        {
            if (!IvyArt) { Parterre(t, size); return; }
            var r = CityRandom(t, 0x6a2d); float R() { return (float)r.NextDouble(); }
            float hx = size.x / 2, hz = size.y / 2, inX = hx - GardenHedge - GardenWalk, inZ = hz - GardenHedge - GardenWalk, lo = GardenCross + GardenBed;
            var d = new DetailSet(t, "garden", "garden"); var up = Vector3.up; var right = t.TransformDirection(Vector3.right); var fwd = t.TransformDirection(Vector3.forward);
            Vector3 W(float x, float y, float z) { return t.TransformPoint(new Vector3(x, LocalGround(t, x, z) + y, z)); }
            void Ground(Material m, float x0, float x1, float z0, float z1, float lift)
            {
                if (x1 - x0 < .05f || z1 - z0 < .05f) return;
                Vector3 a = W(x0, lift, z0), b = W(x0, lift, z1), c = W(x1, lift, z1), e = W(x1, lift, z0);
                d[m].Quad(a, b, c, e, up, new Vector2(a.x, a.z) / 2, new Vector2(b.x, b.z) / 2, new Vector2(c.x, c.z) / 2, new Vector2(e.x, e.z) / 2);
            }
            Ground(Gravel, -hx + .3f, hx - .3f, -hz + .3f, hz - .3f, .05f);
            // The hedge round it, a gap in each side's middle where the walks cross out.
            foreach (int s in new[] { -1, 1 })
            {
                foreach (int k in new[] { -1, 1 })
                {
                    float za = 1.1f, zb = hz; Hedge(d, W(s * (hx - .3f), 0, k * (za + zb) / 2), fwd, zb - za, .6f, .8f, r);
                    float xa = 1.1f, xb = hx - .6f; Hedge(d, W(k * (xa + xb) / 2, 0, s * (hz - .3f)), right, xb - xa, .6f, .8f, r);
                }
            }
            // The quarters: beds round a lawn, edged in dwarf box, planted one kind each.
            int scheme = r.Next(GardenFlowers.Length), qi = 0;
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                float x0 = GardenCross, x1 = inX, z0 = GardenCross, z1 = inZ; if (x1 - x0 < .6f || z1 - z0 < .6f) { qi++; continue; }
                float X(float x) { return sx * x; } float Z(float z) { return sz * z; }
                bool lawn = InGardenLawn(size, new Vector2(x0 + GardenBed + .1f, z0 + GardenBed + .1f));
                float lx0 = x0 + GardenBed, lx1 = x1 - GardenBed, lz0 = z0 + GardenBed, lz1 = z1 - GardenBed;
                if (lawn)
                {
                    Ground(BedSoil, Mathf.Min(X(x0), X(x1)), Mathf.Max(X(x0), X(x1)), Mathf.Min(Z(z0), Z(lz0)), Mathf.Max(Z(z0), Z(lz0)), .065f);
                    Ground(BedSoil, Mathf.Min(X(x0), X(x1)), Mathf.Max(X(x0), X(x1)), Mathf.Min(Z(lz1), Z(z1)), Mathf.Max(Z(lz1), Z(z1)), .065f);
                    Ground(BedSoil, Mathf.Min(X(x0), X(lx0)), Mathf.Max(X(x0), X(lx0)), Mathf.Min(Z(lz0), Z(lz1)), Mathf.Max(Z(lz0), Z(lz1)), .065f);
                    Ground(BedSoil, Mathf.Min(X(lx1), X(x1)), Mathf.Max(X(lx1), X(x1)), Mathf.Min(Z(lz0), Z(lz1)), Mathf.Max(Z(lz0), Z(lz1)), .065f);
                    Ground(Lawn, Mathf.Min(X(lx0), X(lx1)), Mathf.Max(X(lx0), X(lx1)), Mathf.Min(Z(lz0), Z(lz1)), Mathf.Max(Z(lz0), Z(lz1)), .07f);
                }
                else Ground(BedSoil, Mathf.Min(X(x0), X(x1)), Mathf.Max(X(x0), X(x1)), Mathf.Min(Z(z0), Z(z1)), Mathf.Max(Z(z0), Z(z1)), .065f);
                // the dwarf box round the quarter
                Hedge(d, W(X((x0 + x1) / 2), 0, Z(z0 + .1f)), right, x1 - x0, .2f, .28f, r); Hedge(d, W(X((x0 + x1) / 2), 0, Z(z1 - .1f)), right, x1 - x0, .2f, .28f, r);
                Hedge(d, W(X(x0 + .1f), 0, Z((z0 + z1) / 2)), fwd, z1 - z0 - .4f, .2f, .28f, r); Hedge(d, W(X(x1 - .1f), 0, Z((z0 + z1) / 2)), fwd, z1 - z0 - .4f, .2f, .28f, r);
                // the plants: round the bed's middle line (or over the whole of a quarter with no lawn), the corners a flowering bush
                string kind = GardenFlowers[(scheme + qi) % GardenFlowers.Length]; var spots = new List<(float x, float z, bool corner)>();
                if (lawn)
                {
                    float a0 = x0 + GardenBed / 2, a1 = x1 - GardenBed / 2, b0 = z0 + GardenBed / 2, b1 = z1 - GardenBed / 2;
                    foreach (var (x, z) in new[] { (a0, b0), (a1, b0), (a1, b1), (a0, b1) }) spots.Add((x, z, true));
                    int nx = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 1.4f)), nz = Mathf.Max(1, Mathf.RoundToInt((b1 - b0) / 1.4f));
                    for (int k = 1; k < nx; k++) { float x = Mathf.Lerp(a0, a1, k / (float)nx); spots.Add((x, b0, false)); spots.Add((x, b1, false)); }
                    for (int k = 1; k < nz; k++) { float z = Mathf.Lerp(b0, b1, k / (float)nz); spots.Add((a0, z, false)); spots.Add((a1, z, false)); }
                }
                else for (float x = x0 + .55f; x < x1 - .3f; x += 1.2f) for (float z = z0 + .55f; z < z1 - .3f; z += 1.2f) spots.Add((x, z, false));
                foreach (var (x, z, corner) in spots)
                {
                    var at = new Vector3(X(x), LocalGround(t, X(x), Z(z)) + .06f, Z(z));
                    if (corner && GardenPlant(t, "Nature/Bush_Common_Flowers", at, R() * 360, .8f + R() * .2f)) continue;
                    if (GardenPlant(t, kind, at, R() * 360, .5f + R() * .2f)) continue;
                    for (int k = 0; k < 5; k++) Blossom(d, W(X(x) + (R() - .5f) * .5f, .25f + R() * .2f, Z(z) + (R() - .5f) * .5f), right, fwd, .09f, qi);   // no pack: painted blooms
                }
                // the lawn's middle: a small tree in two quarters (a big enough lawn), a topiary on a stone pot in the others
                if (lawn)
                {
                    float cx = X((lx0 + lx1) / 2), cz = Z((lz0 + lz1) / 2); var foot = W(cx, .07f, cz);
                    if ((qi == 0 || qi == 3) && lx1 - lx0 >= 2.4f && lz1 - lz0 >= 2.4f && HasProp("Nature/CommonTree_1"))
                    {
                        var tree = ModelProp(t, "Nature/CommonTree_" + (1 + r.Next(5)), new Vector3(cx, LocalGround(t, cx, cz), cz), R() * 360, 3.2f + R());
                        if (tree != null)
                        {
                            DressNature(tree, Wither(KitLeaf[r.Next(2)]), "*", LeafMask);
                            foreach (var mf in tree.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null) for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) Tally("triangles", (int)(mf.sharedMesh.GetIndexCount(k) / 3));
                            Tally("garden:trees");
                        }
                    }
                    else
                    {
                        d[CityShade].Frustum(foot, .3f, .38f, .38f, 8, true); var body = foot + up * .38f;
                        if ((qi & 1) == 0) { d[HedgeBody].Frustum(body, .55f, .05f, 1.7f, 8, false, true); LeafSkin(d, HedgeLeaf, body, 1.6f, y => Mathf.Lerp(.55f, .05f, y / 1.7f), 22, r); }
                        else { d[HedgeBody].Ball(body, .5f); d[HedgeBody].Ball(body + up * 1.05f, .3f); LeafSkin(d, HedgeLeaf, body, 1.0f, y => .5f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(y / 1f)), 16, r); LeafSkin(d, HedgeLeaf, body + up * 1.05f, .6f, y => .3f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(y / .6f)), 8, r); }
                        Tally("garden:topiary");
                    }
                }
                qi++;
            }
            // Urns of flowers on pedestals: where the walks cross, and by the side gaps of a big garden.
            var urn = PropMesh("Garden urn", () => Turned(new[] { new Vector2(.15f, 0), new Vector2(.2f, .06f), new Vector2(.11f, .16f), new Vector2(.16f, .3f), new Vector2(.34f, .55f), new Vector2(.4f, .72f), new Vector2(.36f, .74f), new Vector2(0, .7f) }, 14, 1));
            var urnSpots = new List<Vector2> { Vector2.zero }; if (hx >= 8) foreach (int s in new[] { -1, 1 }) foreach (int k in new[] { -1, 1 }) urnSpots.Add(new Vector2(s * (hx - .3f), k * 1.45f));
            foreach (var u in urnSpots)
            {
                float big = u == Vector2.zero ? 1 : .7f, g = LocalGround(t, u.x, u.y);
                d[Marble].Box(W(u.x, .45f * big, u.y), new Vector3(.62f, .9f, .62f) * big, t.rotation); d[CityShade].Box(W(u.x, .06f, u.y), new Vector3(.8f, .12f, .8f) * big, t.rotation);
                var o = MeshPart(urn, t, new Vector3(u.x, g + .9f * big, u.y), Marble); o.name = "Detail garden urn"; o.transform.localScale = Vector3.one * big; Tally("triangles", (int)(urn.GetIndexCount(0) / 3));
                if (!GardenPlant(t, "Nature/Flower_4_Group", new Vector3(u.x, g + .9f * big + .66f * big, u.y), R() * 360, .5f * big)) Blossom(d, W(u.x, (.9f + .85f) * big, u.y), right, fwd, .12f, 0);
                Tally("garden:urns");
            }
            // Stone benches on the end walks, facing in.
            if (hx >= 5) foreach (int sz in new[] { -1, 1 }) foreach (int sx in new[] { -1, 1 })
            {
                float bx = sx * hx * .45f, bz = sz * (hz - GardenHedge - .3f);
                d[Marble].Box(W(bx, .44f, bz), new Vector3(1.6f, .1f, .44f), t.rotation);
                foreach (int k in new[] { -1, 1 }) d[CityShade].Box(W(bx + k * .58f, .2f, bz), new Vector3(.24f, .4f, .38f), t.rotation);
                Tally("garden:benches");
            }
            // A rose arch over each end gap: iron hoops, a vine of leaves and roses up the posts and over.
            foreach (int sz in new[] { -1, 1 }) RoseArch(d, t, sz * (hz - .3f), r);
            Flush(d);
        }
        /// <summary>A rose arch (iron, two hoops .44 m apart tied across) over a garden's gap at local z, 1.2 m either way of its middle, 3 m
        /// to its crown; a vine of leaf cards up both posts and over, roses (pink, red, white) in it.</summary>
        void RoseArch(DetailSet d, Transform t, float z, System.Random r)
        {
            const float half = 1.2f, rise = 2.2f; float R() { return (float)r.NextDouble(); }
            float g = LocalGround(t, 0, z); var hoops = new List<Vector3>[2]; var leafM = IvyLeaf;
            for (int f = 0; f < 2; f++)
            {
                float zz = z + (f - .5f) * .44f; var pts = new List<Vector3>(); Vector3 P(float x, float y) { return t.TransformPoint(new Vector3(x, g + y, zz)); }
                pts.Add(P(-half, 0)); pts.Add(P(-half, rise));
                for (int k = 1; k < 8; k++) { float a = Mathf.PI - k * Mathf.PI / 8; pts.Add(P(half * Mathf.Cos(a), rise + half * .7f * Mathf.Sin(a))); }
                pts.Add(P(half, rise)); pts.Add(P(half, 0));
                for (int k = 0; k + 1 < pts.Count; k++) d[Iron].Rod(pts[k], pts[k + 1], .022f);
                hoops[f] = pts;
            }
            for (int k = 1; k + 1 < hoops[0].Count; k++) d[Iron].Rod(hoops[0][k], hoops[1][k], .014f);
            var heart = t.TransformPoint(new Vector3(0, g + rise * .8f, z));
            foreach (var hoop in hoops)
                for (int k = 0; k + 1 < hoop.Count; k++)
                    for (float f = 0; f < 1; f += .3f)
                    {
                        var p = Vector3.Lerp(hoop[k], hoop[k + 1], f) + new Vector3(R() - .5f, R() - .5f, R() - .5f) * .12f;
                        if (k == 0 && f < .3f) continue;   // the vine starts a little up the post
                        var along = new Vector3(R() - .5f, R() * .8f, R() - .5f).normalized; var across = Vector3.Cross(along, Vector3.up); if (across.sqrMagnitude < .01f) across = Vector3.right;
                        d.Leaf(leafM, p, along, across.normalized, .34f, .34f, heart, .25f, IvyGreens[r.Next(IvyGreens.Length)] * .9f);
                        if (R() < .45f) { var n = new Vector3(R() - .5f, 0, R() - .5f).normalized; if (n.sqrMagnitude < .01f) n = Vector3.forward; Blossom(d, p + n * .08f, n, Vector3.Cross(Vector3.up, n), .07f + R() * .03f, new[] { 0, 3, 1 }[r.Next(3)]); }
                    }
            Tally("garden:arches"); Tally("garden:iron");
        }
    }
}
