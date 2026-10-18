using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Hallow's Creek as the books write it (Docs/REWORK_LOWTOWNS_HALLOWS.md picks 1, 2, 3 and 12, Chris's "keep all" of 2026-10-18;
    /// Docs/DUNGEON2_DESIGN.md "The town as the books write it"). Citations are b1 ch.N:L, D:\code\crulanda\book1\chapter_N.md at line L.
    /// - White and perfect, not grey (CANON, ch.17:269; ch.21:7-19): a prop of style "white" is <see cref="Whitewash"/>ed: its plaster
    ///   lime-white, no smoke from its chimney, no lamp lit; a fence a white picket fence "without a speck of peeling paint" (ch.21:11); a
    ///   house a box of roses "turned to flakes of red quartz" under each front window and a rose bush in its garden (ch.21:11). Only the
    ///   people and the beasts are grey stone (ch.21:25). The fossilised farm outside the town stays grey (ch.17:45).
    /// - The laundry "as rigid as steel wire" (ch.21:23): the lines taut, the sheets hanging straight down; no wind (ch.21:19).
    /// - The Square (ch.21:120-230; ch.19:41-53): the town hall "a perfect hemisphere of missing reality", smooth glassy obsidian curving
    ///   fifty feet down (<see cref="Crater"/>, ZoneBuilder.Hallows) to a mirror floor whose reflection does not move (a reflection probe
    ///   rendered once, <see cref="StillMirror"/>); the red door on a slab of floating marble, its brass knocker a face with its lips sewn
    ///   shut; chairs, desks, bricks, a stone horse, a vault's corner with its rebar, a bench and a lamppost orbiting it slowly (ch.21:136,
    ///   188), foundation stones in the lower orbit (ch.21:200); the chalked equations on the paving (ch.19:49, <see cref="ChalkArray"/>); the
    ///   weeping willow (ch.19:41, <see cref="WeepingWillow"/>) and by it the dug-out hollow the stone girl bends over (ch.19:53, ch.19:39);
    ///   the chapel with its stained glass burst and hanging in the air (ch.19:53, <see cref="Chapel"/>).
    /// - The book's bicycle frozen mid-spin an inch off the ground (ch.21:11) as a handcart (no machines): <see cref="Handcart"/>.
    /// - A dog leaping, stopped in the air (ch.21:122): paused variant 6.
    /// The small parts of a thing are joined into one mesh a material (<see cref="Parts"/>): the frame cost stays where it was.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>Small parts gathered by material and joined into one mesh each (<see cref="Flush"/>): a picket fence is a mesh, not sixty objects.</summary>
        sealed class Parts
        {
            public readonly Dictionary<Material, List<CombineInstance>> ByMaterial = new Dictionary<Material, List<CombineInstance>>();
            public void Add(PrimitiveType type, Vector3 at, Vector3 scale, Material m, Quaternion? rot = null)
            {
                if (m == null) return;
                if (!ByMaterial.TryGetValue(m, out var list)) ByMaterial[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = PrimitiveMesh(type), transform = Matrix4x4.TRS(at, rot ?? Quaternion.identity, scale) });
            }
        }
        /// <summary>The gathered parts built under <paramref name="t"/> (their positions are its local ones), one object a material.</summary>
        void Flush(Parts parts, Transform t, string name)
        {
            foreach (var kv in parts.ByMaterial)
            {
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                MeshPart(mesh, t, Vector3.zero, kv.Key);
            }
        }

        // ---------- the town's colours (ch.21:11) ----------
        /// <summary>The houses' lime-wash: clean white, a breath warm.</summary>
        Material LimeWash { get { return Tint(art.plaster, new Color(.97f, .96f, .93f)); } }
        /// <summary>White paint on wood (the picket fences, the laundry's posts, the flower boxes).</summary>
        Material WhitePaint { get { return Tint(art.plaster, new Color(1f, 1f, .98f)); } }
        /// <summary>Red quartz: the roses' petals (ch.21:11), glossy, with a little light of their own so they glint in the grey.</summary>
        Material QuartzRed
        {
            get
            {
                var m = Glowing(new Color(.8f, .1f, .12f), .3f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .9f);
                return m;
            }
        }
        /// <summary>The roses' leaves, gone to a cold green quartz with them.</summary>
        Material QuartzLeaf { get { return Tint(art.stone, new Color(.4f, .5f, .43f)); } }

        /// <summary>
        /// A prop of style "white" (Hallow's Creek's houses, fences, gardens and yards): white and perfect, as the books have the town
        /// (ch.17:269 "Picket fences. White houses."; ch.21:11-19). The plaster lime-white; no smoke, no lamp (the minute is 11:59 at
        /// noon, and nothing moves); a fence rebuilt as a white picket fence (its draws from the zone's stream already taken); a house
        /// given its quartz roses.
        /// </summary>
        void Whitewash(Transform t, ZoneProp p)
        {
            foreach (var l in t.GetComponentsInChildren<Light>(true)) l.enabled = false;
            if (p.kind == "fence") { PicketFence(t, p.size.x > 0 ? p.size.x : 8); return; }
            string plaster = art.plaster != null ? art.plaster.name : null; var lime = LimeWash;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                // No smoke from a chimney in a town where nothing moves.
                if (r is ParticleSystemRenderer) { r.enabled = false; continue; }
                if (plaster == null) continue;
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i] != lime && mats[i].name.StartsWith(plaster, System.StringComparison.Ordinal)) { mats[i] = lime; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
            if (p.kind == "house") QuartzRoses(t, p.size.x > 0 ? p.size : new Vector2(7, 5), 3f);
        }
        /// <summary>
        /// A white picket fence along the prop's x (the old rail fence's parts taken away, its collider kept): posts every two metres
        /// with caps, two rails, and pointed pickets a hand apart on the -z face, all one white, all straight.
        /// </summary>
        void PicketFence(Transform t, float length)
        {
            for (int i = t.childCount - 1; i >= 0; i--) DestroyImmediate(t.GetChild(i).gameObject);
            var parts = new Parts(); var paint = WhitePaint;
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2) + 1); float step = length / (posts - 1);
            for (int i = 0; i < posts; i++)
            {
                float x = -length / 2 + step * i;
                parts.Add(PrimitiveType.Cube, new Vector3(x, .52f, 0), new Vector3(.11f, 1.04f, .11f), paint);
                parts.Add(PrimitiveType.Cube, new Vector3(x, 1.06f, 0), new Vector3(.15f, .04f, .15f), paint);
            }
            foreach (float y in new[] { .32f, .76f }) parts.Add(PrimitiveType.Cube, new Vector3(0, y, -.02f), new Vector3(length, .07f, .035f), paint);
            for (float x = -length / 2 + .1f; x <= length / 2 - .1f; x += .15f)
            {
                parts.Add(PrimitiveType.Cube, new Vector3(x, .46f, -.055f), new Vector3(.075f, .84f, .022f), paint);
                // The point.
                parts.Add(PrimitiveType.Cube, new Vector3(x, .88f, -.055f), new Vector3(.053f, .053f, .022f), paint, Quaternion.Euler(0, 0, 45));
            }
            Flush(parts, t, "Picket fence");
        }
        /// <summary>
        /// A house's roses (ch.21:11, "The flower boxes were full of roses that would never wilt, their petals turned to flakes of red
        /// quartz"): a white box under each front window (the windows House puts on the -z face, 2.6 m apart, none over the door), each
        /// rose three flakes of red quartz set against each other over quartz leaves; and a rose bush either side of the door.
        /// </summary>
        void QuartzRoses(Transform t, Vector2 size, float wallHeight)
        {
            float w = size.x, d = size.y; var parts = new Parts(); var box = WhitePaint; var earth = Tint(art.stone, new Color(.3f, .27f, .24f)); var red = QuartzRed; var leaf = QuartzLeaf;
            var rr = new System.Random(Mathf.RoundToInt(t.position.x * 31 + t.position.z * 17) ^ 0x5eed);
            float R() { return (float)rr.NextDouble(); }
            void Rose(Vector3 c)
            {
                for (int f = 0; f < 3; f++) parts.Add(PrimitiveType.Cube, c, Vector3.one * (.075f - f * .012f), red, Quaternion.Euler(R() * 360, R() * 360, R() * 360));
            }
            for (float x = -w / 2 + 1.4f; x < w / 2 - .8f; x += 2.6f)
            {
                if (Mathf.Abs(x) < 1.2f) continue;
                // Under the front window's sill (its bottom at .6 + .55 of the wall less .45), standing a little out from the wall.
                float y = .6f + wallHeight * .55f - .63f, z = -d / 2 - .27f;
                parts.Add(PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(.95f, .22f, .26f), box);
                parts.Add(PrimitiveType.Cube, new Vector3(x, y + .1f, z), new Vector3(.88f, .03f, .2f), earth);
                foreach (int s in new[] { -1, 1 }) parts.Add(PrimitiveType.Cube, new Vector3(x + s * .36f, y - .16f, z + .1f), new Vector3(.05f, .14f, .26f), box);
                for (int k = 0; k < 6; k++)
                {
                    float u = x - .38f + k * .152f;
                    parts.Add(PrimitiveType.Cube, new Vector3(u, y + .17f, z + (R() - .5f) * .08f), new Vector3(.11f, .08f, .11f), leaf, Quaternion.Euler(R() * 40, R() * 90, R() * 40));
                    Rose(new Vector3(u + (R() - .5f) * .04f, y + .26f + R() * .06f, z - .02f));
                }
            }
            // A rose bush either side of the door, in the front garden.
            foreach (int s in new[] { -1, 1 })
            {
                var c = new Vector3(s * 2.1f, 0, -d / 2 - .75f);
                for (int k = 0; k < 6; k++) parts.Add(PrimitiveType.Cube, c + new Vector3((R() - .5f) * .5f, .2f + R() * .45f, (R() - .5f) * .35f), new Vector3(.2f, .16f, .2f), leaf, Quaternion.Euler(R() * 40, R() * 90, R() * 40));
                for (int k = 0; k < 5; k++) Rose(c + new Vector3((R() - .5f) * .55f, .45f + R() * .35f, (R() - .5f) * .38f));
            }
            Flush(parts, t, "Quartz roses");
        }
        /// <summary>
        /// The laundry yard (ch.21:23, "A woman hung laundry on a line that was as rigid as steel wire"; Mrs Miller's, ch.21:376): three
        /// lines on white posts, each line taut and dead straight, the sheets hanging straight down from their pegs, white and still. No
        /// wind (ch.21:19): nothing on them has moved in fifty years.
        /// </summary>
        void Laundry(Transform t)
        {
            var parts = new Parts(); var post = WhitePaint; var wire = Tint(art.stone, new Color(.82f, .82f, .8f));
            var linen = Tint(art.cloth != null ? art.cloth : art.plaster, new Color(.95f, .95f, .93f)); var peg = Tint(art.timber, new Color(.55f, .45f, .33f));
            for (int line = 0; line < 3; line++)
            {
                float z = -3 + line * 3;
                foreach (int side in new[] { -1, 1 })
                {
                    parts.Add(PrimitiveType.Cube, new Vector3(side * 4, 1.1f, z), new Vector3(.14f, 2.2f, .14f), post);
                    parts.Add(PrimitiveType.Cube, new Vector3(side * 4, 2.12f, z), new Vector3(.12f, .08f, .5f), post);
                }
                parts.Add(PrimitiveType.Cube, new Vector3(0, 2.1f, z), new Vector3(8, .014f, .014f), wire);
                for (int k = 0; k < 4; k++)
                {
                    float x = -3 + k * 2;
                    parts.Add(PrimitiveType.Cube, new Vector3(x, 2.1f - .58f, z), new Vector3(1.5f, 1.15f, .02f), linen);
                    foreach (int s in new[] { -1, 1 }) parts.Add(PrimitiveType.Cube, new Vector3(x + s * .66f, 2.09f, z), new Vector3(.03f, .09f, .04f), peg);
                }
            }
            Flush(parts, t, "Laundry");
        }

        // ---------- the beasts, stopped (ch.21:122) ----------
        /// <summary>A dog leaping, stopped in the air (paused variant 6; "motionless dogs suspended in mid-air", ch.21:122): stretched out a
        /// metre off the gravel, forelegs reaching, hind legs thrown back, ears flat, tail up. Grey stone; no collider (you walk under it).</summary>
        void StoneDogLeap(Transform t)
        {
            var s = Tint(art.stone, PausedStone * .9f); var parts = new Parts();
            parts.Add(PrimitiveType.Capsule, new Vector3(0, 1.02f, 0), new Vector3(.3f, .38f, .3f), s, Quaternion.Euler(84, 0, 0));
            parts.Add(PrimitiveType.Sphere, new Vector3(0, 1.2f, .42f), new Vector3(.22f, .21f, .28f), s);
            parts.Add(PrimitiveType.Capsule, new Vector3(0, 1.16f, .6f), new Vector3(.1f, .1f, .14f), s, Quaternion.Euler(80, 0, 0));
            foreach (int side in new[] { -1, 1 })
            {
                parts.Add(PrimitiveType.Cube, new Vector3(side * .07f, 1.31f, .34f), new Vector3(.05f, .04f, .12f), s, Quaternion.Euler(-30, 0, 0));
                parts.Add(PrimitiveType.Capsule, new Vector3(side * .09f, .9f, .4f), new Vector3(.07f, .17f, .07f), s, Quaternion.Euler(-55, 0, 0));
                parts.Add(PrimitiveType.Capsule, new Vector3(side * .09f, .92f, -.38f), new Vector3(.08f, .18f, .08f), s, Quaternion.Euler(60, 0, 0));
            }
            parts.Add(PrimitiveType.Capsule, new Vector3(0, 1.22f, -.5f), new Vector3(.05f, .16f, .05f), s, Quaternion.Euler(-40, 0, 0));
            Flush(parts, t, "A dog in mid-leap");
        }

        // ---------- the handcart (ch.21:11's bicycle; no machines) ----------
        /// <summary>
        /// "A bicycle leaned against a post, its wheels frozen mid-spin, hovering an inch off the ground" (ch.21:11), made a handcart (the
        /// game has no machines): a painted box on two wheels and two long handles, leaning against a fence post with one wheel an inch
        /// off the gravel, stopped mid-turn. Solid.
        /// </summary>
        void Handcart(Transform t)
        {
            var board = Tint(art.timber, new Color(.3f, .46f, .38f)); var dark = Tint(art.timber, new Color(.25f, .19f, .14f)); var iron = Tint(art.metal, new Color(.22f, .22f, .24f));
            var cart = new GameObject("The handcart").transform; cart.SetParent(t, false);
            cart.localPosition = new Vector3(0, .03f, 0); cart.localRotation = Quaternion.Euler(0, 0, -2.2f);
            BoxPart(cart, new Vector3(0, .62f, 0), new Vector3(1.1f, .07f, 1.5f), board, null, 1);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, cart, new Vector3(s * .55f, .82f, 0), new Vector3(.05f, .36f, 1.5f), board);
                MeshPart2(Wheel, cart, new Vector3(s * .7f, .45f, .15f), dark, iron, Quaternion.Euler(0, 0, 90)).transform.localScale = Vector3.one * .75f;
                Bar(cart, new Vector3(s * .42f, .66f, -.7f), new Vector3(s * .38f, .78f, -2.1f), .07f, .06f, dark);
            }
            Part(PrimitiveType.Cube, cart, new Vector3(0, .82f, .74f), new Vector3(1.1f, .36f, .05f), board);
            Part(PrimitiveType.Cube, cart, new Vector3(0, .82f, -.74f), new Vector3(1.1f, .36f, .05f), board);
            Rod(cart, new Vector3(-.78f, .45f, .15f), new Vector3(.78f, .45f, .15f), .06f, dark);
            Solid(t, new Vector3(0, .5f, -.3f), new Vector3(1.6f, 1, 2.6f));
        }

        // ---------- the Square (ch.19:41-53; ch.21:120-230) ----------
        /// <summary>
        /// The chalked equations (ch.19:49: "Aethelgard left his complex, unstable equations chalked onto the town square paving stones";
        /// Elara touched the central glyph, ch.19:49; the void "bloomed directly from the center", ch.13:21). The middle of the array went
        /// with the town hall; what is left runs up to the crater's rim on the south-west of the Square, paused with the town: the outer
        /// rings of the array cut off by the rim, spokes running in to where the centre was, glyphs on the rings and rows of working chalked
        /// between them. Flat on the paving, no collider. CANON-EXPANDED (the chalk kept by the pause; its look GAME-ONLY).
        /// </summary>
        void ChalkArray(Transform t, Vector2 c)
        {
            var parts = new Parts(); var chalk = Tint(art.stone, new Color(.95f, .96f, 1f)); var rr = new System.Random(1949);
            float R() { return (float)rr.NextDouble(); }
            var root = t.position;
            Vector3 On(float bearing, float r) { var p = Bearing(c, bearing, r); return new Vector3(p.x, HeightAt(p.x, p.y) + .012f, p.y) - root; }
            void Stroke(Vector3 a, Vector3 b, float wide)
            {
                var d = b - a; float len = new Vector2(d.x, d.z).magnitude; if (len < .02f) return;
                parts.Add(PrimitiveType.Cube, (a + b) / 2, new Vector3(wide, .006f, len), chalk, Quaternion.LookRotation(new Vector3(d.x, 0, d.z)));
            }
            // The rings, broken where the chalk has scuffed or a foot came down fifty years ago.
            foreach (float r in new[] { 26.2f, 27.3f, 29.6f, 31.8f })
                for (float b = 158; b < 252; b += 2.2f)
                {
                    if (R() < .07f) continue;
                    Stroke(On(b, r), On(b + 2.2f, r), r > 31 ? .04f : .06f);
                }
            // Spokes in toward the centre the crater took, every nine degrees.
            for (float b = 160; b <= 250; b += 9) Stroke(On(b, 25.6f), On(b, 32.4f), .05f);
            // Glyphs on the rings: little triangles, circles of four strokes and crosses.
            for (float b = 164; b < 250; b += 6.5f)
            {
                float r = R() < .5f ? 28.45f : 30.7f; var g = On(b, r); int kind = rr.Next(3); float s = .32f;
                if (kind == 0) { var a0 = g + new Vector3(0, 0, s); var a1 = g + new Vector3(s * .87f, 0, -s * .5f); var a2 = g + new Vector3(-s * .87f, 0, -s * .5f); Stroke(a0, a1, .035f); Stroke(a1, a2, .035f); Stroke(a2, a0, .035f); }
                else if (kind == 1) for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3, a2 = (k + 1) * Mathf.PI / 3; Stroke(g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * s, g + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * s, .035f); }
                else { Stroke(g + new Vector3(-s, 0, 0), g + new Vector3(s, 0, 0), .035f); Stroke(g + new Vector3(0, 0, -s), g + new Vector3(0, 0, s), .035f); }
            }
            // Rows of working between the outer rings: short marks in lines, as a hand writes.
            for (float b = 196; b < 246; b += 3.1f)
                for (int row = 0; row < 4; row++)
                {
                    float r = 32.6f + row * .45f, x = 0;
                    var start = On(b, r); var along = On(b + 2.6f, r) - start; along.y = 0; var dir = along.normalized;
                    while (x < along.magnitude)
                    {
                        float len = .08f + R() * .22f; if (R() < .2f) { x += .2f; continue; }
                        Stroke(start + dir * x, start + dir * (x + len), .03f); x += len + .06f;
                    }
                }
            Flush(parts, t, "The chalked equations");
        }
        /// <summary>
        /// The weeping willow by the Square (ch.19:41, "We were playing down by the weeping willow near the town square when the sky broke"):
        /// a leaning trunk, its limbs arching out, and the long fronds hanging straight down to a man's shoulder all round, gone to pale
        /// grey-green quartz like the roses; no wind moves them (ch.21:19). The trunk is solid; the fronds are walked through.
        /// </summary>
        void WeepingWillow(Transform t)
        {
            var bark = Tint(art.bark != null ? art.bark : art.timber, new Color(.5f, .48f, .45f)); var frond = Tint(art.stone, new Color(.62f, .7f, .63f)); var parts = new Parts();
            var rr = new System.Random(1941); float R() { return (float)rr.NextDouble(); }
            parts.Add(PrimitiveType.Cylinder, new Vector3(0, 1.4f, 0), new Vector3(.75f, 1.4f, .75f), bark, Quaternion.Euler(4, 0, 6));
            parts.Add(PrimitiveType.Cylinder, new Vector3(.25f, 3.6f, .1f), new Vector3(.55f, 1.0f, .55f), bark, Quaternion.Euler(-6, 0, 12));
            for (int k = 0; k < 7; k++)
            {
                float a = k * 51 + R() * 20; var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var from = new Vector3(.3f, 4.2f, .1f); var mid = from + dir * 1.6f + Vector3.up * .9f; var end = mid + dir * 1.5f + Vector3.down * .3f;
                foreach (var (p, q) in new[] { (from, mid), (mid, end) })
                {
                    var d = q - p; parts.Add(PrimitiveType.Cylinder, (p + q) / 2, new Vector3(.22f, d.magnitude / 2, .22f), bark, Quaternion.FromToRotation(Vector3.up, d));
                }
            }
            // The fronds: hung from the crown's rim, ring on ring, straight down.
            for (int ring = 0; ring < 3; ring++)
            {
                float r = 2.1f + ring * 1.15f, top = 5.6f - ring * .45f; int n = 26 + ring * 12;
                for (int k = 0; k < n; k++)
                {
                    float a = (k + R() * .6f) / n * Mathf.PI * 2, len = 2.6f + R() * 1.4f + ring * .4f;
                    var at = new Vector3(Mathf.Cos(a) * (r + R() * .3f), top - len / 2, Mathf.Sin(a) * (r + R() * .3f));
                    parts.Add(PrimitiveType.Cube, at, new Vector3(.12f, len, .05f), frond, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
                }
            }
            // The crown's mass over the fronds.
            for (int k = 0; k < 14; k++) parts.Add(PrimitiveType.Sphere, new Vector3((R() - .5f) * 5, 5.4f + R() * .9f, (R() - .5f) * 5), new Vector3(1.9f, .7f, 1.9f), frond);
            Flush(parts, t, "The weeping willow");
            Solid(t, new Vector3(0, 2, 0), new Vector3(1, 4, 1));
        }
        /// <summary>The hollow in the ash by the willow (ch.19:39, 53: the stone girl threw herself over her little brother, and Gorm dug him
        /// out ten years before the books): a child-sized scoop of dark ash under where she bends, the ash heaped back on either side by big
        /// hands, long ago. Paint and lumps only: no collider, no change to the ground.</summary>
        void DugHollow(Transform t)
        {
            var dark = Tint(art.stone, new Color(.2f, .2f, .21f)); var ash = Tint(art.stone, new Color(.5f, .5f, .5f)); var parts = new Parts();
            var rr = new System.Random(1953); float R() { return (float)rr.NextDouble(); }
            parts.Add(PrimitiveType.Cylinder, new Vector3(0, .012f, 0), new Vector3(1.15f, .012f, .7f), dark);
            for (int k = 0; k < 11; k++)
            {
                float a = k / 11f * Mathf.PI * 2; var at = new Vector3(Mathf.Cos(a) * .72f, .05f, Mathf.Sin(a) * .5f) * (1 + R() * .25f);
                parts.Add(PrimitiveType.Sphere, at, new Vector3(.45f + R() * .3f, .14f + R() * .1f, .35f + R() * .2f), ash, Quaternion.Euler(0, R() * 180, 0));
            }
            Flush(parts, t, "The dug-out hollow");
        }
        /// <summary>
        /// The chapel by the Square (ch.19:53: the Weave fractured "with a sound that physically shattered the stained glass windows of the
        /// local chapel"): a small white nave (the town's lime-wash), a slate roof and a bell-cote; its front (-z) toward the Square with a
        /// tall pointed window over the door, three windows down each side. Every window is empty: its glass burst outward with the sound
        /// and hangs in the air before it, shards of red, blue, gold, green and violet stopped where they flew, a hand to two metres out,
        /// a few still caught in the frame. Solid; not entered. CANON (the chapel and its windows); its place and look GAME-ONLY.
        /// </summary>
        void Chapel(Transform t, Vector2 size)
        {
            float w = size.x, d = size.y, H = 5.2f; var lime = LimeWash; var slate = Tint(art.slate != null ? art.slate : art.stone, new Color(.36f, .36f, .4f));
            var dark = Tint(art.stone, new Color(.05f, .05f, .07f)); var trim = Dressed(new Color(.86f, .85f, .82f));
            float drop = FootDrop(t, w, d);
            BoxPart(t, new Vector3(0, (H - drop) / 2, 0), new Vector3(w, H + drop, d), lime, null, 1.75f);
            Solid(t, new Vector3(0, H / 2, 0), new Vector3(w, H, d));
            MeshPart(ZoneMeshes.GableRoof(d + .8f, w + .7f, w * .55f, .25f, .2f), t, new Vector3(0, H, 0), slate, Quaternion.Euler(0, 90, 0));
            foreach (int sz in new[] { -1, 1 }) MeshPart(ZoneMeshes.Gable(w, w * .55f, .3f, 2), t, new Vector3(0, H, sz * (d / 2 - .15f)), lime);
            // The bell-cote over the front gable (its bell hangs still), and the door.
            BoxPart(t, new Vector3(0, H + w * .55f + .9f, -d / 2 + .4f), new Vector3(1.3f, 1.8f, .8f), lime, null, 1.5f);
            Part(PrimitiveType.Cube, t, new Vector3(0, H + w * .55f + 1.05f, -d / 2 + .4f), new Vector3(.75f, .95f, .85f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, H + w * .55f + 1.9f, -d / 2 + .4f), new Vector3(1.5f, .14f, 1), slate);
            PlankDoor(t, new Vector3(0, 1.25f, -d / 2 - .05f), 1.5f, 2.5f, Tint(art.timber, new Color(.3f, .22f, .15f)));
            // The windows: each an empty opening (dark), a stone surround, a pointed head; the glass in the air before it.
            var colours = new[] { new Color(.8f, .12f, .12f), new Color(.15f, .3f, .85f), new Color(.95f, .75f, .2f), new Color(.2f, .65f, .3f), new Color(.55f, .25f, .75f) };
            var glass = new Material[colours.Length];
            for (int i = 0; i < colours.Length; i++)
            {
                var m = new Material(ShardGlass) { name = "Stained glass " + i, color = new Color(colours[i].r, colours[i].g, colours[i].b, .62f) };
                m.SetColor("_EmissionColor", colours[i] * .35f); glass[i] = m;
            }
            var shards = new Parts(); var stone = new Parts(); var rr = new System.Random(1953 * 7); float R() { return (float)rr.NextDouble(); }
            void Window(Vector3 at, Quaternion face, float wide, float high)
            {
                // at: the window's middle on the wall's outer face; face turns local +z out of the wall.
                Vector3 W(Vector3 local) { return at + face * local; }
                shards.Add(PrimitiveType.Cube, W(new Vector3(0, 0, -.02f)), new Vector3(wide, high, .06f), dark, face);
                stone.Add(PrimitiveType.Cube, W(new Vector3(0, -high / 2 - .08f, .06f)), new Vector3(wide + .3f, .14f, .2f), trim, face);
                foreach (int s in new[] { -1, 1 }) stone.Add(PrimitiveType.Cube, W(new Vector3(s * (wide / 2 + .08f), 0, .03f)), new Vector3(.16f, high, .12f), trim, face);
                // The pointed head.
                foreach (int s in new[] { -1, 1 }) stone.Add(PrimitiveType.Cube, W(new Vector3(s * wide * .26f, high / 2 + wide * .22f, .03f)), new Vector3(.16f, wide * .62f, .12f), trim, face * Quaternion.Euler(0, 0, s * -48));
                int n = Mathf.RoundToInt(wide * high * 7) + 6;
                for (int k = 0; k < n; k++)
                {
                    // Stopped where it flew: out from the opening, spreading as it went, a few still in the frame.
                    float outward = R() < .15f ? .02f : .15f + R() * R() * 2.2f, spread = 1 + outward * .55f;
                    var p = W(new Vector3((R() - .5f) * wide * spread, (R() - .45f) * high * spread, outward));
                    float s = .12f + R() * .22f;
                    shards.Add(PrimitiveType.Cube, p, new Vector3(s, s * (.6f + R()), .012f), glass[rr.Next(glass.Length)], face * Quaternion.Euler((R() - .5f) * 120, (R() - .5f) * 120, R() * 360));
                }
            }
            var front = Quaternion.Euler(0, 180, 0);
            Window(new Vector3(0, 3.75f, -d / 2), front, 1.3f, 1.6f);
            for (int k = 0; k < 3; k++)
                foreach (int sx in new[] { -1, 1 })
                    Window(new Vector3(sx * w / 2, 2.6f, -d / 2 + 2.6f + k * (d - 4) / 2), Quaternion.Euler(0, sx * 90, 0), .9f, 1.7f);
            Window(new Vector3(0, 3.2f, d / 2), Quaternion.identity, 1.1f, 1.8f);
            Flush(stone, t, "The chapel's window surrounds");
            Flush(shards, t, "The chapel's glass, in the air");
        }
        // ---------- the crater's bowl (ch.21:124-136, 224) ----------
        /// <summary>
        /// The bowl where the town hall stood (ch.21:126, "a perfect hemisphere of missing reality. The ground was smooth, glassy obsidian,
        /// curving down into darkness"; "the fifty-foot drop to the glassy bottom", ch.21:134): a skin of black glass laid over the sunk
        /// ground, a hemisphere from the rim down <paramref name="deep"/> metres (never under the land, so the land never shows through),
        /// and in its middle the mirror (ch.21:224, "The bottom of the crater wasn't just glass. it was a mirror"). The skin's points are
        /// in the crater's own frame (<paramref name="t"/> stands on the bowl's floor). No collider: nobody goes down (decision 7).
        /// </summary>
        void ObsidianBowl(Transform t, float radius, float deep)
        {
            const int rings = 24, segs = 56; const int mirrorRings = 7;
            var obsidian = new Material(Tint(art.stone, new Color(.06f, .055f, .08f))) { name = "Obsidian" };
            if (obsidian.HasProperty("_Glossiness")) obsidian.SetFloat("_Glossiness", .93f);
            if (obsidian.HasProperty("_Metallic")) obsidian.SetFloat("_Metallic", .25f);
            if (obsidian.HasProperty("_MainTex")) obsidian.mainTexture = null;
            var mirror = new Material(obsidian) { name = "The mirror floor", color = new Color(.1f, .1f, .12f) };
            if (mirror.HasProperty("_Glossiness")) mirror.SetFloat("_Glossiness", .99f);
            if (mirror.HasProperty("_Metallic")) mirror.SetFloat("_Metallic", .8f);
            var c = t.position;
            Vector3 At(int ring, int seg)
            {
                float r = radius * ring / rings, a = seg * Mathf.PI * 2 / segs;
                float x = c.x + Mathf.Cos(a) * r, z = c.z + Mathf.Sin(a) * r;
                float hemi = Datum - deep * Mathf.Sqrt(Mathf.Max(0, 1 - (r / radius) * (r / radius)));
                return new Vector3(x, Mathf.Max(hemi, HeightAt(x, z) + .05f), z) - c;
            }
            Mesh Band(int from, int to, string name)
            {
                var v = new List<Vector3>(); var tri = new List<int>();
                for (int ring = from; ring <= to; ring++) for (int seg = 0; seg < segs; seg++) v.Add(At(ring, seg));
                int I(int ring, int seg) { return (ring - from) * segs + (seg % segs); }
                void Tri(int a, int b, int d) { if (Vector3.Cross(v[b] - v[a], v[d] - v[a]).y < 0) { int s = b; b = d; d = s; } tri.Add(a); tri.Add(b); tri.Add(d); }
                for (int ring = from; ring < to; ring++)
                    for (int seg = 0; seg < segs; seg++)
                    {
                        Tri(I(ring, seg), I(ring + 1, seg), I(ring + 1, seg + 1));
                        if (ring > 0) Tri(I(ring, seg), I(ring + 1, seg + 1), I(ring, seg + 1));
                    }
                var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(tri, 0); m.SetUVs(0, new Vector2[v.Count]); m.RecalculateNormals(); m.RecalculateBounds();
                return m;
            }
            MeshPart(Band(0, mirrorRings, "The mirror floor"), t, Vector3.zero, mirror);
            MeshPart(Band(mirrorRings, rings, "The obsidian bowl"), t, Vector3.zero, obsidian);
            // The mirror's reflection: rendered once, a little after the town is built, and never again. It does not move (ch.21:224).
            var probe = new GameObject("The still reflection").AddComponent<ReflectionProbe>(); probe.transform.SetParent(t, false); probe.transform.localPosition = new Vector3(0, 2, 0);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing; probe.resolution = 128; probe.hdr = false; probe.importance = 2;
            probe.size = new Vector3(radius * 2 + 4, deep + 10, radius * 2 + 4); probe.center = new Vector3(0, (deep + 10) / 2 - 2, 0); probe.farClipPlane = 400;
            probe.gameObject.AddComponent<StillMirror>();
        }
    }

    /// <summary>
    /// Hallow's Creek's mirror floor (b1 ch.21:224, "He could see his own reflection looking up at him. But the reflection wasn't
    /// moving."): its reflection probe is rendered once, a second and a half after the zone is up, and never again, so the town in the
    /// glass stands as it stood then, whatever moves above it.
    /// </summary>
    public sealed class StillMirror : MonoBehaviour
    {
        float at;
        /// <summary>No graphics device (-nographics batch runs): rendering the probe's cube there crashes Unity natively
        /// (GfxDevice::DrawSharedGeometryJobs under ReflectionProbes::UpdateRealtimeProbes, r203), so the probe is switched off and never rendered.</summary>
        static bool NoGraphics => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
        void Start()
        {
            at = Time.time + 1.5f;
            if (NoGraphics) { var probe = GetComponent<ReflectionProbe>(); if (probe != null) probe.enabled = false; enabled = false; }
        }
        void Update()
        {
            if (Time.time < at) return;
            enabled = false;
            if (NoGraphics) return;
            var probe = GetComponent<ReflectionProbe>(); if (probe != null && probe.isActiveAndEnabled) probe.RenderProbe();
        }
    }
}
