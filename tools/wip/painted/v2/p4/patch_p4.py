"""The painted style pass, part 4 (chunky props): patch. Usage: python patch_p4.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Scripts\\World\\ZoneBuilder.cs
     shared shapes  PropMesh (a cache), Turned (a lathe), Cutout (a board cut to an outline), Piece/Joined/TwoTone
                    (one mesh of one or two materials), MeshPart2, Rod, Bar, Stake (a hewn, capped post), Barrel, Crate,
                    Wheel, Signpost.
     Fence          hewn capped posts of uneven height, two lapped rails a span that follow them.
     Cart           plank bed, flared side boards, head and tail boards, spoked wheels with iron tyres, an axle, a bigger load.
     Lamp           hewn post on a stone footing, a pegged arm on an iron brace, a roofed lantern. The light is untouched.
     Well           a turned masonry ring (plinth, coping, a real shaft), a windlass with rope, bucket and crank, a tie beam.
     Stall          stout posts, two rails, a scalloped valance, a plank counter, bigger goods (baskets and pots turned),
                    a battened crate and a tied sack. Variant 3 (the hide stall) keeps its poles and ragged valance.
     Woodpile       logs with pale cut ends between stakes, a one-piece chopping block, a sawhorse with a log on it.
     Bridge         dressed stone, a coping course, four capped piers. Deck and parapet boxes are the size they were.
     barrels, crates, signpost (in BuildProps)  hooped barrels, battened crates, a fingerpost with a pointed board.
   Scripts\\World\\ZoneBuilder.Verdant.cs
     RopeBridge     its posts are hewn and capped (the same count, the same reach into the bank).

   Nothing here draws from the zone's random stream that did not before: Fence takes its two draws a post in the old
   order, and every other shape varies by fixed tables or by the index of the part. No collider, workplace, light or
   registered object changes size or place.

   Everything is checked before anything is written: every old block must occur exactly once in its file. So a second
   run, or a run on a tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'

# (file, what, old, new)
EDITS = [
(r'Scripts\World\ZoneBuilder.cs', 'the barrels prop',
r'''                    case "barrels": for (int i = 0; i <= p.variant % 3; i++) Part(PrimitiveType.Cylinder, t, new Vector3(i * .75f - .4f, .5f, (i % 2) * .6f), new Vector3(.7f, .5f, .7f), art.timber); Solid(t, new Vector3(0, .5f, .3f), new Vector3(2.2f, 1, 1.4f)); break;
''',
r'''                    case "barrels": for (int i = 0; i <= p.variant % 3; i++) Barrel(t, new Vector3(i * .75f - .4f, 0, (i % 2) * .6f), i == 1 ? .9f : 1, i * 70); Solid(t, new Vector3(0, .5f, .3f), new Vector3(2.2f, 1, 1.4f)); break;
'''),
(r'Scripts\World\ZoneBuilder.cs', 'the crates prop',
r'''                    case "crates": for (int i = 0; i <= p.variant % 3; i++) Part(PrimitiveType.Cube, t, new Vector3(i == 2 ? -.05f : i * .9f - .5f, i == 2 ? 1.35f : .45f, 0), Vector3.one * .9f, Tint(art.timber, new Color(.45f, .33f, .21f))); Solid(t, new Vector3(0, .5f, 0), new Vector3(2.2f, 1, 1)); break;   // the third crate rests across the two below, not on air
''',
r'''                    case "crates": for (int i = 0; i <= p.variant % 3; i++) Crate(t, new Vector3(i == 2 ? -.05f : i * .9f - .5f, i == 2 ? 1.19f : .41f, 0), i == 2 ? .7f : .82f, i == 2 ? 24 : i * 9 - 4); Solid(t, new Vector3(0, .5f, 0), new Vector3(2.2f, 1, 1)); break;   // the third crate rests across the two below, not on air
'''),
(r'Scripts\World\ZoneBuilder.cs', 'the signpost prop',
r'''                    case "signpost": Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(.15f, 2.2f, .15f), art.timber); Part(PrimitiveType.Cube, t, new Vector3(.45f, 1.8f, 0), new Vector3(.9f, .28f, .06f), art.timber); break;
''',
r'''                    case "signpost": Signpost(t); break;
'''),
(r'Scripts\World\ZoneBuilder.cs', 'Well',
r'''        void Well(Transform t, int variant = 0)
        {
            // variant 1: a blood-stone well (Khaven) - reddened stone and dark water.
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .45f, 0), new Vector3(2.2f, .45f, 2.2f), variant == 1 ? Tint(art.stone, new Color(.45f, .3f, .28f)) : art.stone);
            var water = art.water; if (variant == 1) { water = new Material(art.water); water.color = new Color(.28f, .06f, .05f, .9f); }
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .88f, 0), new Vector3(1.7f, .03f, 1.7f), water);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1, 1.6f, 0), new Vector3(.18f, 2.3f, .18f), art.timber);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 2.3f, 0), new Vector3(.15f, 1.05f, .15f), art.timber, Quaternion.Euler(0, 0, 90));
            MeshPart(ZoneMeshes.GableRoof(2.8f, 2.2f, 1f, .1f), t, new Vector3(0, 2.75f, 0), art.slate, Quaternion.Euler(0, 90, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(.2f, 1.5f, 0), new Vector3(.35f, .22f, .35f), art.timber);
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(2.4f, 2.4f, 2.4f));
        }
''',
r'''        /// <summary>
        /// A village well: a round ring of dressed stone on a plinth course under a coping, the water down in the shaft, and a
        /// roofed windlass on two posts with its rope, bucket and crank. variant 1: a blood-stone well (Khaven), reddened stone
        /// and dark water.
        /// </summary>
        void Well(Transform t, int variant = 0)
        {
            var dark = Tint(art.timber, new Color(.26f, .18f, .12f)); var rope = Tint(art.hay, new Color(.58f, .5f, .34f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var ring = PropMesh("Well ring", () => Turned(new[] { new Vector2(1.2f, -.3f), new Vector2(1.2f, .16f), new Vector2(1.1f, .22f), new Vector2(1.08f, .72f), new Vector2(1.2f, .76f), new Vector2(1.2f, .9f), new Vector2(1.15f, .94f), new Vector2(.84f, .94f), new Vector2(.8f, .9f), new Vector2(.8f, .2f) }, 20, 1.5f));
            MeshPart(ring, t, Vector3.zero, variant == 1 ? Tint(Masonry, new Color(.5f, .32f, .3f)) : Masonry);
            var water = art.water; if (variant == 1) { water = new Material(art.water); water.color = new Color(.28f, .06f, .05f, .9f); }
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .6f, 0), new Vector3(1.62f, .02f, 1.62f), water);   // down in the shaft
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1, 1.55f, 0), new Vector3(.22f, 2.4f, .22f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.68f, 0), new Vector3(2.5f, .14f, .2f), dark);         // tie beam, its ends past the posts
            MeshPart(ZoneMeshes.GableRoof(2.8f, 2.2f, 1f, .1f), t, new Vector3(0, 2.75f, 0), art.slate, Quaternion.Euler(0, 90, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 1.9f, 0), new Vector3(.24f, .89f, .24f), art.timber, Quaternion.Euler(0, 0, 90));   // windlass drum
            Part(PrimitiveType.Cylinder, t, new Vector3(.2f, 1.9f, 0), new Vector3(.33f, .15f, .33f), rope, Quaternion.Euler(0, 0, 90));       // the rope wound on it
            Rod(t, new Vector3(.2f, 1.78f, 0), new Vector3(.2f, 1.36f, 0), .04f, rope);
            MeshPart(PropMesh("Bucket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.2f, .36f), new Vector2(.175f, .36f), new Vector2(.13f, .04f), new Vector2(0, .04f) }, 10)), t, new Vector3(.2f, 1, 0), Tint(art.timber, new Color(.42f, .3f, .19f)));
            Rod(t, new Vector3(.01f, 1.36f, 0), new Vector3(.39f, 1.36f, 0), .03f, iron);                      // the bucket's bail
            Part(PrimitiveType.Cube, t, new Vector3(1.14f, 1.75f, 0), new Vector3(.05f, .38f, .06f), iron);    // crank
            Rod(t, new Vector3(1.14f, 1.6f, 0), new Vector3(1.36f, 1.6f, 0), .045f, dark);
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(2.4f, 2.4f, 2.4f));
        }
'''),
(r'Scripts\World\ZoneBuilder.cs', 'Fence, Cart and Lamp (and the shared shapes before them)',
r'''        void Fence(Transform t, float length)
        {
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2) + 1);
            for (int i = 0; i < posts; i++)
                Part(PrimitiveType.Cube, t, new Vector3(-length / 2 + length * i / (posts - 1), .55f, 0), new Vector3(.14f, 1.1f, .14f), art.timber, Quaternion.Euler(R01 * 6 - 3, 0, R01 * 6 - 3));
            foreach (float y in new[] { .45f, .85f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, 0), new Vector3(length, .08f, .06f), art.timber);
            Solid(t, new Vector3(0, .6f, 0), new Vector3(length, 1.2f, .3f));
        }
        void Cart(Transform t)
        {
            Part(PrimitiveType.Cube, t, new Vector3(0, .9f, 0), new Vector3(1.6f, .5f, 2.6f), art.timber);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, new Vector3(s * .95f, .6f, -.2f), new Vector3(1.2f, .06f, 1.2f), Tint(art.timber, new Color(.25f, .17f, .11f)), Quaternion.Euler(0, 0, 90));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .5f, .75f, -2.2f), new Vector3(.1f, .1f, 2), art.timber, Quaternion.Euler(-12, 0, 0));
            Part(PrimitiveType.Sphere, t, new Vector3(0, 1.3f, .3f), new Vector3(1.3f, .6f, 1.7f), art.hay);
            Solid(t, new Vector3(0, .8f, -.4f), new Vector3(2.1f, 1.6f, 3.8f));
        }
        void Lamp(Transform t, int variant)
        {
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.4f, 0), new Vector3(.14f, 2.8f, .14f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(.35f, 2.7f, 0), new Vector3(.7f, .1f, .1f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(.6f, 2.4f, 0), new Vector3(.3f, .38f, .3f), art.glass);
            // Every lamp is lit after dark; variant > 0 lamps also burn by day (inn yard, mill).
            var l = new GameObject("Lamp light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(.6f, 2.3f, 0);
            l.type = LightType.Point; l.range = variant > 0 ? 7 : 9; l.intensity = 1.2f; l.color = new Color(1, .7f, .38f);
            NightLights.Add(new NightLight { light = l, dayIntensity = variant > 0 ? 1.2f : 0, nightIntensity = 1.7f });
        }
''',
r'''        // ---------- chunky props (the painted style pass, part 4): shared shapes ----------
        readonly Dictionary<string, Mesh> propMeshes = new Dictionary<string, Mesh>();
        /// <summary>A prop mesh built once and shared by every prop that uses it (posts, barrels, crates, wheels).</summary>
        Mesh PropMesh(string key, Func<Mesh> make)
        {
            if (!propMeshes.TryGetValue(key, out var m)) { m = make(); m.name = key; propMeshes[key] = m; }
            return m;
        }
        /// <summary>
        /// A turned shape (well ring, barrel, bucket, pot, wheel rim): the profile (x radius, y height) swept round the y axis.
        /// The profile runs up the outside, in over the top and down the inside; each stretch of it is flat-shaded against the
        /// next, so hoops and rims keep a crisp edge. UVs tile every <paramref name="tile"/> metres.
        /// </summary>
        static Mesh Turned(Vector2[] profile, int sides, float tile = 1)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float along = 0, widest = 0; foreach (var p in profile) widest = Mathf.Max(widest, p.x);
            int wraps = Mathf.Max(1, Mathf.RoundToInt(2 * Mathf.PI * widest / tile));
            for (int k = 0; k + 1 < profile.Length; k++)
            {
                Vector2 a = profile[k], b = profile[k + 1], d = b - a; float len = d.magnitude; if (len < 1e-4f) continue;
                int at = v.Count;
                for (int s = 0; s <= sides; s++)
                {
                    float ang = 2 * Mathf.PI * (s % sides) / sides, c = Mathf.Cos(ang), sn = Mathf.Sin(ang), u = (float)s / sides * wraps;
                    var normal = new Vector3(c * d.y, -d.x, sn * d.y) / len;   // square to the stretch, away from the solid
                    v.Add(new Vector3(c * a.x, a.y, sn * a.x)); n.Add(normal); uv.Add(new Vector2(u, along / tile));
                    v.Add(new Vector3(c * b.x, b.y, sn * b.x)); n.Add(normal); uv.Add(new Vector2(u, (along + len) / tile));
                }
                for (int s = 0; s < sides; s++) { int i = at + s * 2; tri.AddRange(new[] { i, i + 1, i + 3, i, i + 3, i + 2 }); }
                along += len;
            }
            var m = new Mesh { name = "Turned" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
        /// <summary>A board cut to a convex outline (x, y), <paramref name="thick"/> deep along z: sign boards, an awning's scallops.</summary>
        static Mesh Cutout(Vector2[] outline, float thick)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            var o = (Vector2[])outline.Clone(); float area = 0, h = thick / 2;
            for (int i = 0; i < o.Length; i++) { Vector2 a = o[i], b = o[(i + 1) % o.Length]; area += a.x * b.y - b.x * a.y; }
            if (area > 0) Array.Reverse(o);   // clockwise seen from -z
            foreach (int face in new[] { -1, 1 })
            {
                int at = v.Count;
                foreach (var p in o) { v.Add(new Vector3(p.x, p.y, face * h)); n.Add(new Vector3(0, 0, face)); uv.Add(p); }
                for (int i = 1; i + 1 < o.Length; i++) tri.AddRange(face < 0 ? new[] { at, at + i, at + i + 1 } : new[] { at, at + i + 1, at + i });
            }
            for (int i = 0; i < o.Length; i++)
            {
                Vector2 a = o[i], b = o[(i + 1) % o.Length], e = b - a; var side = new Vector3(-e.y, e.x, 0).normalized; int at = v.Count;
                v.Add(new Vector3(a.x, a.y, -h)); v.Add(new Vector3(a.x, a.y, h)); v.Add(new Vector3(b.x, b.y, h)); v.Add(new Vector3(b.x, b.y, -h));
                for (int k = 0; k < 4; k++) n.Add(side);
                uv.Add(Vector2.zero); uv.Add(new Vector2(0, thick)); uv.Add(new Vector2(e.magnitude, thick)); uv.Add(new Vector2(e.magnitude, 0));
                tri.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
            }
            var m = new Mesh { name = "Cutout" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }
        static CombineInstance Piece(Mesh mesh, Vector3 at = default(Vector3), Quaternion? rot = null)
        {
            return new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(at, rot ?? Quaternion.identity, Vector3.one) };
        }
        /// <summary>Several pieces as one mesh of one material.</summary>
        static Mesh Joined(params CombineInstance[] pieces)
        {
            var m = new Mesh(); m.CombineMeshes(pieces, true); m.RecalculateBounds(); return m;
        }
        /// <summary>One mesh of two materials: the first takes <paramref name="a"/>, the second <paramref name="b"/> (wood and its iron, a log and its cut ends).</summary>
        static Mesh TwoTone(Mesh a, Mesh b)
        {
            var m = new Mesh(); m.CombineMeshes(new[] { Piece(a), Piece(b) }, false); m.RecalculateBounds(); return m;
        }
        GameObject MeshPart2(Mesh mesh, Transform parent, Vector3 localPos, Material first, Material second, Quaternion? rot = null)
        {
            var o = MeshPart(mesh, parent, localPos, first, rot); o.GetComponent<MeshRenderer>().sharedMaterials = new[] { first, second }; return o;
        }
        /// <summary>A round bar between two points (rope, iron braces, axles, handles).</summary>
        GameObject Rod(Transform t, Vector3 a, Vector3 b, float thick, Material m)
        {
            var d = b - a; return Part(PrimitiveType.Cylinder, t, (a + b) / 2, new Vector3(thick, d.magnitude / 2, thick), m, Quaternion.FromToRotation(Vector3.up, d));
        }
        /// <summary>A squared bar between two points that are not one above the other, its faces kept upright (rails, shafts, trestle legs).</summary>
        GameObject Bar(Transform t, Vector3 a, Vector3 b, float high, float thick, Material m)
        {
            var d = b - a; return Part(PrimitiveType.Cube, t, (a + b) / 2, new Vector3(thick, high, d.magnitude), m, Quaternion.LookRotation(d));
        }
        /// <summary>
        /// A hewn post standing on <paramref name="foot"/>: six-sided, thicker at the foot, with a chamfered cap proud of the
        /// shaft. Every post of one thickness and height shares a mesh (heights go in steps of 4 cm).
        /// </summary>
        GameObject Stake(Transform t, Vector3 foot, float thick, float tall, Material m, Quaternion? rot = null)
        {
            int w = Mathf.RoundToInt(thick * 100), h = Mathf.RoundToInt(tall * 25); float r = w / 200f, top = h / 25f;
            var mesh = PropMesh("Post " + w + "x" + h, () => Turned(new[] { new Vector2(r * 1.16f, 0), new Vector2(r, .22f), new Vector2(r * .94f, top - r * 1.5f), new Vector2(r * 1.3f, top - r * 1.3f), new Vector2(r * 1.3f, top - r * .45f), new Vector2(r * .85f, top - r * .1f), new Vector2(0, top) }, 6));
            return MeshPart(mesh, t, foot, m, rot);
        }
        /// <summary>A coopered barrel standing on <paramref name="foot"/>, a metre tall and .72 across the bilge at size 1: bulged staves, a recessed head and three iron hoops.</summary>
        GameObject Barrel(Transform t, Vector3 foot, float size = 1, float yaw = 0)
        {
            var mesh = PropMesh("Barrel", () => TwoTone(
                Turned(new[] { new Vector2(0, 0), new Vector2(.3f, 0), new Vector2(.345f, .28f), new Vector2(.36f, .5f), new Vector2(.345f, .72f), new Vector2(.3f, 1), new Vector2(.27f, 1), new Vector2(.27f, .95f), new Vector2(0, .95f) }, 12),
                Joined(Piece(Turned(new[] { new Vector2(.315f, .12f), new Vector2(.335f, .12f), new Vector2(.348f, .2f), new Vector2(.328f, .2f) }, 12)),
                    Piece(Turned(new[] { new Vector2(.355f, .46f), new Vector2(.376f, .46f), new Vector2(.376f, .54f), new Vector2(.355f, .54f) }, 12)),
                    Piece(Turned(new[] { new Vector2(.328f, .8f), new Vector2(.348f, .8f), new Vector2(.335f, .88f), new Vector2(.315f, .88f) }, 12)))));
            var o = MeshPart2(mesh, t, foot, art.timber, Tint(art.metal, new Color(.2f, .2f, .22f)), Quaternion.Euler(0, yaw, 0)); o.transform.localScale = Vector3.one * size;
            return o;
        }
        /// <summary>A packing crate <paramref name="size"/> a side, centred on <paramref name="at"/>: plank panels and a lid set in a frame of darker battens, a brace across each side.</summary>
        GameObject Crate(Transform t, Vector3 at, float size, float yaw = 0)
        {
            var mesh = PropMesh("Crate", () =>
            {
                var frame = new List<CombineInstance>();
                foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 })
                {
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.12f, 1, .12f), 1), new Vector3(a * .44f, 0, b * .44f)));        // corner posts
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.76f, .12f, .12f), 1), new Vector3(0, a * .44f, b * .44f)));     // rails along x
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.12f, .12f, .76f), 1), new Vector3(b * .44f, a * .44f, 0)));     // rails along z
                }
                foreach (int s in new[] { -1, 1 })
                {
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.1f, 1.05f, .04f), 1), new Vector3(0, 0, s * .47f), Quaternion.Euler(0, 0, s * 45)));
                    frame.Add(Piece(ZoneMeshes.Box(new Vector3(.04f, 1.05f, .1f), 1), new Vector3(s * .47f, 0, 0), Quaternion.Euler(s * 45, 0, 0)));
                }
                return TwoTone(Joined(Piece(ZoneMeshes.Box(new Vector3(.92f, .96f, .92f), .5f)), Piece(ZoneMeshes.Box(new Vector3(.8f, .05f, .8f), .5f), new Vector3(0, .5f, 0))), Joined(frame.ToArray()));
            });
            var o = MeshPart2(mesh, t, at, Tint(art.timber, new Color(.45f, .33f, .21f)), Tint(art.timber, new Color(.27f, .19f, .12f)), Quaternion.Euler(0, yaw, 0)); o.transform.localScale = Vector3.one * size;
            return o;
        }
        /// <summary>A spoked cart wheel, 1.23 m across, its axle along the part's y: wooden felloes, eight spokes and a hub in the first material, an iron tyre in the second.</summary>
        Mesh Wheel
        {
            get
            {
                return PropMesh("Cart wheel", () =>
                {
                    var wood = new List<CombineInstance> {
                        Piece(Turned(new[] { new Vector2(.48f, -.05f), new Vector2(.585f, -.05f), new Vector2(.585f, .05f), new Vector2(.48f, .05f), new Vector2(.48f, -.05f) }, 16)),
                        Piece(Turned(new[] { new Vector2(0, -.13f), new Vector2(.1f, -.13f), new Vector2(.13f, -.04f), new Vector2(.13f, .04f), new Vector2(.1f, .13f), new Vector2(0, .13f) }, 10)) };
                    for (int k = 0; k < 4; k++) wood.Add(Piece(ZoneMeshes.Box(new Vector3(1, .06f, .08f), 1), Vector3.zero, Quaternion.Euler(0, k * 45, 0)));
                    return TwoTone(Joined(wood.ToArray()), Turned(new[] { new Vector2(.585f, -.06f), new Vector2(.615f, -.06f), new Vector2(.615f, .06f), new Vector2(.585f, .06f) }, 16));
                });
            }
        }
        /// <summary>A fingerpost: a hewn post leaning a little, its board cut to a point and hung a touch crooked on two pegs.</summary>
        void Signpost(Transform t)
        {
            Stake(t, new Vector3(0, -.1f, 0), .2f, 2.44f, art.timber, Quaternion.Euler(0, 0, 2));
            var board = PropMesh("Sign board", () => Cutout(new[] { new Vector2(-.24f, -.17f), new Vector2(-.24f, .17f), new Vector2(.86f, .17f), new Vector2(1.08f, 0), new Vector2(.86f, -.17f) }, .07f));
            MeshPart(board, t, new Vector3(0, 1.82f, -.12f), Tint(art.timber, new Color(.5f, .37f, .22f)), Quaternion.Euler(0, 0, -5));
            foreach (float x in new[] { -.13f, .02f }) Part(PrimitiveType.Cube, t, new Vector3(x, 1.82f - x * .087f, -.16f), new Vector3(.05f, .05f, .06f), Tint(art.timber, new Color(.2f, .14f, .09f)));   // the pegs, on the board's own slant
        }
        /// <summary>
        /// A post-and-rail fence along x: hewn posts with chamfered caps, each leaning and standing a little taller or shorter
        /// by its own two draws (the same two a post always took, in the same order), and two rails a span, lapped past the
        /// posts on alternate faces and following the posts' uneven heights.
        /// </summary>
        void Fence(Transform t, float length)
        {
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 2) + 1); float step = length / (posts - 1), low = 0, high = 0, lowZ = 0, highZ = 0;
            var rail = Tint(art.timber, new Color(.4f, .29f, .19f));
            for (int i = 0; i < posts; i++)
            {
                float ax = R01 * 6 - 3, az = R01 * 6 - 3, x = -length / 2 + step * i;   // two draws a post: its lean, and from the same two its height and where the rails meet it
                Stake(t, new Vector3(x, -.12f, 0), i % 2 == 0 ? .2f : .18f, 1.26f + ax * .04f, art.timber, Quaternion.Euler(ax, 0, az));
                float l = .42f + ax * .012f, h = .86f + az * .015f, face = i % 2 == 0 ? .1f : -.1f, lean = Mathf.Sin(ax * Mathf.Deg2Rad), lz = lean * (l + .12f), hz = lean * (h + .12f);   // how far the leaning post stands off the line at each rail
                if (i > 0)
                {
                    Bar(t, new Vector3(x - step - .18f, low, face + lowZ), new Vector3(x + .18f, l, face + lz), .1f, .05f, rail);
                    Bar(t, new Vector3(x - step - .18f, high, face + highZ), new Vector3(x + .18f, h, face + hz), .1f, .05f, rail);
                }
                low = l; high = h; lowZ = lz; highZ = hz;
            }
            Solid(t, new Vector3(0, .6f, 0), new Vector3(length, 1.2f, .3f));
        }
        /// <summary>A farm cart, shafts to -z: a plank bed between flared side boards, two spoked wheels with iron tyres on an axle, and a heaped load of hay.</summary>
        void Cart(Transform t)
        {
            var board = Tint(art.timber, new Color(.42f, .3f, .19f)); var dark = Tint(art.timber, new Color(.25f, .17f, .11f));
            BoxPart(t, new Vector3(0, .8f, 0), new Vector3(1.5f, .12f, 2.6f), art.timber, null, 1);   // the bed
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(s * .8f, 1.08f, 0), new Vector3(.07f, .46f, 2.74f), board, Quaternion.Euler(0, 0, s * -9));   // side boards, flared out over the wheels
                MeshPart2(Wheel, t, new Vector3(s * .95f, .615f, -.2f), dark, Tint(art.metal, new Color(.2f, .2f, .22f)), Quaternion.Euler(0, 0, 90));
                Bar(t, new Vector3(s * .56f, .8f, -1.2f), new Vector3(s * .46f, .52f, -3.15f), .1f, .09f, art.timber);   // shafts
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.05f, -1.3f), new Vector3(1.56f, .4f, .07f), board);   // head board
            Part(PrimitiveType.Cube, t, new Vector3(0, 1, 1.3f), new Vector3(1.56f, .3f, .07f), board);        // tail board, lower
            Rod(t, new Vector3(-1.06f, .615f, -.2f), new Vector3(1.06f, .615f, -.2f), .11f, dark);            // axle
            Part(PrimitiveType.Cube, t, new Vector3(0, .7f, -.2f), new Vector3(1.3f, .14f, .16f), dark);       // bolster: the bed rests on the axle
            Part(PrimitiveType.Sphere, t, new Vector3(0, 1.28f, .05f), new Vector3(1.42f, .86f, 2.4f), art.hay);
            Solid(t, new Vector3(0, .8f, -.4f), new Vector3(2.1f, 1.6f, 3.8f));
        }
        /// <summary>A street lamp: a hewn post on a stone footing, an arm through it on an iron brace, and a roofed lantern hung round the light.</summary>
        void Lamp(Transform t, int variant)
        {
            var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            Stake(t, new Vector3(0, -.1f, 0), .2f, 3.16f, art.timber);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .1f, 0), new Vector3(.5f, .2f, .5f), Masonry);              // footing
            Part(PrimitiveType.Cube, t, new Vector3(.3f, 2.8f, 0), new Vector3(.86f, .11f, .11f), art.timber);          // the arm, its butt through the post
            Rod(t, new Vector3(.06f, 2.36f, 0), new Vector3(.5f, 2.76f, 0), .045f, iron);                              // brace
            MeshPart(PropMesh("Lantern roof", () => ZoneMeshes.Cone(.27f, .2f, 4)), t, new Vector3(.6f, 2.56f, 0), iron, Quaternion.Euler(0, 45, 0));   // its peak meets the arm
            Part(PrimitiveType.Cube, t, new Vector3(.6f, 2.4f, 0), new Vector3(.28f, .36f, .28f), art.glass);
            // Every lamp is lit after dark; variant > 0 lamps also burn by day (inn yard, mill).
            var l = new GameObject("Lamp light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(.6f, 2.3f, 0);
            l.type = LightType.Point; l.range = variant > 0 ? 7 : 9; l.intensity = 1.2f; l.color = new Color(1, .7f, .38f);
            NightLights.Add(new NightLight { light = l, dayIntensity = variant > 0 ? 1.2f : 0, nightIntensity = 1.7f });
        }
'''),
(r'Scripts\World\ZoneBuilder.cs', 'Bridge',
r'''        void Bridge(Transform t, float length)
        {
            // Stone arch: deck segments follow a gentle arc; the deck is walkable, the parapets are not.
            int segs = 8; float rise = 1.2f, width = 4;
            for (int i = 0; i < segs; i++)
            {
                float a = (i + .5f) / segs, z = -length / 2 + length * a;
                float y = .15f + Mathf.Sin(a * Mathf.PI) * rise, slope = Mathf.Cos(a * Mathf.PI) * rise * Mathf.PI / length;
                var deck = Part(PrimitiveType.Cube, t, new Vector3(0, y, z), new Vector3(width, .45f, length / segs + .08f), art.stone, Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0));
                var col = deck.AddComponent<BoxCollider>(); deck.AddComponent<NavWalkable>();
                foreach (int s in new[] { -1, 1 })
                {
                    var para = Part(PrimitiveType.Cube, t, new Vector3(s * (width / 2 + .2f), y + .55f, z), new Vector3(.4f, .8f, length / segs + .08f), art.stone, Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0));
                    para.AddComponent<BoxCollider>();
                }
            }
        }
''',
r'''        void Bridge(Transform t, float length)
        {
            // Stone arch: deck segments follow a gentle arc; the deck is walkable, the parapets are not. Dressed stone with a
            // coping course on the parapets and a capped pier at each corner; the coping and the piers are for the eye only.
            int segs = 8; float rise = 1.2f, width = 4; var coping = Tint(Masonry, new Color(.62f, .6f, .55f));
            for (int i = 0; i < segs; i++)
            {
                float a = (i + .5f) / segs, z = -length / 2 + length * a;
                float y = .15f + Mathf.Sin(a * Mathf.PI) * rise, slope = Mathf.Cos(a * Mathf.PI) * rise * Mathf.PI / length;
                var tilt = Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0); var slab = new Vector3(width, .45f, length / segs + .08f); var block = new Vector3(.4f, .8f, length / segs + .08f);
                var deck = BoxPart(t, new Vector3(0, y, z), slab, Masonry, tilt, 1.5f);
                var col = deck.AddComponent<BoxCollider>(); col.center = Vector3.zero; col.size = slab; deck.AddComponent<NavWalkable>();   // the box the old cube had
                foreach (int s in new[] { -1, 1 })
                {
                    var at = new Vector3(s * (width / 2 + .2f), y + .55f, z);
                    var para = BoxPart(t, at, block, Masonry, tilt, 1.5f);
                    var wall = para.AddComponent<BoxCollider>(); wall.center = Vector3.zero; wall.size = block;
                    Part(PrimitiveType.Cube, t, at + tilt * new Vector3(0, .44f, 0), new Vector3(.54f, .12f, length / segs + .1f), coping, tilt);
                }
            }
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var at = new Vector3(sx * (width / 2 + .2f), .6f, sz * (length / 2 + .2f));
                BoxPart(t, at, new Vector3(.64f, 1.9f, .64f), Masonry, null, 1.5f);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, 1.01f, 0), new Vector3(.8f, .14f, .8f), coping);
            }
        }
'''),
(r'Scripts\World\ZoneBuilder.cs', 'Stall',
r'''        /// <summary>
        /// Market stall: an awning on four posts, a counter of goods (variant: 0 produce, 1 cloth and pots, 2 bread, all under
        /// dyed stripes; 3 Ash-Walker, GAME-ONLY look: two stitched hides on bone poles, a ragged hide valance, salt and bone).
        /// </summary>
        void Stall(Transform t, int variant)
        {
            bool hide = variant == 3;
            var colors = hide ? new[] { new Color(.56f, .43f, .29f), new Color(.64f, .52f, .36f) } : Awnings[Mathf.Abs(variant) % Awnings.Length];
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                if (hide) Part(PrimitiveType.Cylinder, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.13f, sz > 0 ? 1.25f : 1.1f, .13f), Bone);
                else Part(PrimitiveType.Cube, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.1f, sz > 0 ? 2.5f : 2.2f, .1f), art.timber);
            for (int i = 0; i < 6; i++)   // stripes, or one hide per half
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.35f, 0), new Vector3(.5f, .05f, 2.3f), Tint(art.cloth, colors[hide ? i / 3 : i % 2]), Quaternion.Euler(-9, 0, hide ? (i / 3) * 3 - 1.5f : 0));
            for (int i = 0; i < 6; i++)   // valance: striped, or ragged hide strips of uneven length
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, hide ? 2.14f - (i * 7 % 4) * .04f : 2.05f, -1.16f), new Vector3(.5f, hide ? .12f + (i * 7 % 4) * .08f : .3f, .03f), Tint(art.cloth, colors[hide ? 0 : i % 2]));
            Part(PrimitiveType.Cube, t, new Vector3(0, .5f, -.45f), new Vector3(2.8f, 1, .7f), Tint(art.timber, new Color(.45f, .32f, .2f)));
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.02f, -.45f), new Vector3(2.9f, .05f, .8f), Tint(art.timber, new Color(.35f, .25f, .16f)));
            var r = new System.Random(Zone.seed + (int)(t.position.x * 13 + t.position.z * 7));
            float R() { return (float)r.NextDouble(); }
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(-1.2f + i * .4f, 1.1f, -.45f + (R() - .5f) * .3f);
                switch (hide ? 3 : Mathf.Abs(variant) % 3)
                {
                    case 0:   // baskets of apples, turnips and cabbages
                        Part(PrimitiveType.Cylinder, t, at, new Vector3(.32f, .08f, .32f), art.hay);
                        var produce = new[] { new Color(.7f, .15f, .12f), new Color(.85f, .75f, .6f), new Color(.4f, .6f, .25f), new Color(.8f, .55f, .15f) }[i % 4];
                        for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, t, at + new Vector3((k - 1) * .08f, .1f, (k % 2) * .06f), Vector3.one * .11f, Tint(art.foliage, produce));
                        break;
                    case 1:   // bolts of cloth and clay pots
                        if (i % 2 == 0) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .08f, 0), new Vector3(.16f, .18f, .16f), Tint(art.cloth, Color.HSVToRGB(R(), .5f, .6f)), Quaternion.Euler(0, 0, 90));
                        else Part(PrimitiveType.Sphere, t, at + new Vector3(0, .12f, 0), new Vector3(.22f, .26f, .22f), Tint(art.stone, new Color(.62f, .4f, .28f)));
                        break;
                    case 3:   // salt blocks, bone harpoon heads, strips of dried meat
                        if (i % 3 == 0) Part(PrimitiveType.Cube, t, at + new Vector3(0, .1f, 0), new Vector3(.24f, .2f, .2f), Tint(art.stone, new Color(.94f, .94f, .91f)), Quaternion.Euler(0, R() * 40 - 20, 0));
                        else if (i % 3 == 1) Part(PrimitiveType.Cube, t, at + new Vector3(0, .03f, 0), new Vector3(.07f, .05f, .34f), Bone, Quaternion.Euler(0, R() * 50 - 25, 0));
                        else Part(PrimitiveType.Capsule, t, at + new Vector3(0, .05f, 0), new Vector3(.08f, .14f, .08f), Tint(art.cloth, new Color(.36f, .16f, .12f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                    default:  // loaves and rolls
                        Part(PrimitiveType.Capsule, t, at + new Vector3(0, .06f, 0), new Vector3(.14f, .14f, .14f), Tint(art.hay, new Color(.72f, .48f, .22f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                }
            }
            Part(PrimitiveType.Cube, t, new Vector3(1.9f, .3f, .2f), new Vector3(.6f, .6f, .6f), Tint(art.timber, new Color(.45f, .33f, .21f)));   // crate
            Part(PrimitiveType.Sphere, t, new Vector3(-1.9f, .35f, .3f), new Vector3(.5f, .6f, .45f), art.cloth);                                // sack
            Solid(t, new Vector3(0, .5f, -.45f), new Vector3(2.9f, 1, .8f));
            Workplace(t, "stall", new Vector3(0, 0, .4f), new Vector3(0, 1.1f, -1.5f));
        }
''',
r'''        /// <summary>
        /// Market stall: an awning on four stout posts and two rails, a scalloped valance, a plank counter of goods (variant:
        /// 0 baskets of produce, 1 bolts of cloth and pots, 2 bread, all under dyed stripes; 3 Ash-Walker, GAME-ONLY look: two
        /// stitched hides on bone poles, a ragged hide valance, salt and bone), a crate and a tied sack beside it.
        /// </summary>
        void Stall(Transform t, int variant)
        {
            bool hide = variant == 3;
            var colors = hide ? new[] { new Color(.56f, .43f, .29f), new Color(.64f, .52f, .36f) } : Awnings[Mathf.Abs(variant) % Awnings.Length];
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                if (hide) Part(PrimitiveType.Cylinder, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.13f, sz > 0 ? 1.25f : 1.1f, .13f), Bone);
                else Part(PrimitiveType.Cube, t, new Vector3(sx * 1.4f, sz > 0 ? 1.25f : 1.1f, sz * .9f), new Vector3(.16f, sz > 0 ? 2.5f : 2.2f, .16f), art.timber);
            if (!hide) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(0, sz > 0 ? 2.42f : 2.12f, sz * .9f), new Vector3(3.1f, .1f, .1f), art.timber);   // rails under the awning, their ends past the posts
            for (int i = 0; i < 6; i++)   // stripes, or one hide per half
                Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.35f, 0), new Vector3(.5f, .05f, 2.3f), Tint(art.cloth, colors[hide ? i / 3 : i % 2]), Quaternion.Euler(-9, 0, hide ? (i / 3) * 3 - 1.5f : 0));
            var scallop = PropMesh("Awning scallop", () =>
            {
                var o = new List<Vector2> { new Vector2(-.25f, 0), new Vector2(.25f, 0) };
                for (int k = 0; k <= 6; k++) o.Add(new Vector2(Mathf.Cos(k * 30 * Mathf.Deg2Rad) * .25f, -.1f - Mathf.Sin(k * 30 * Mathf.Deg2Rad) * .25f));
                return Cutout(o.ToArray(), .03f);
            });
            for (int i = 0; i < 6; i++)   // valance: a scallop a stripe, or ragged hide strips of uneven length
                if (hide) Part(PrimitiveType.Cube, t, new Vector3(-1.25f + i * .5f, 2.14f - (i * 7 % 4) * .04f, -1.16f), new Vector3(.5f, .12f + (i * 7 % 4) * .08f, .03f), Tint(art.cloth, colors[0]));
                else MeshPart(scallop, t, new Vector3(-1.25f + i * .5f, 2.2f, -1.16f), Tint(art.cloth, colors[i % 2]));
            BoxPart(t, new Vector3(0, .5f, -.45f), new Vector3(2.8f, 1, .7f), Tint(art.timber, new Color(.45f, .32f, .2f)), null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.03f, -.45f), new Vector3(3, .08f, .86f), Tint(art.timber, new Color(.35f, .25f, .16f)));
            var r = new System.Random(Zone.seed + (int)(t.position.x * 13 + t.position.z * 7));
            float R() { return (float)r.NextDouble(); }
            for (int i = 0; i < 7; i++)
            {
                var at = new Vector3(-1.2f + i * .4f, 1.1f, -.45f + (R() - .5f) * .3f);
                switch (hide ? 3 : Mathf.Abs(variant) % 3)
                {
                    case 0:   // baskets of apples, turnips and cabbages, heaped over the rim
                        MeshPart(PropMesh("Basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.13f, 0), new Vector2(.19f, .14f), new Vector2(.165f, .14f), new Vector2(.12f, .03f), new Vector2(0, .03f) }, 10, .25f)), t, at + new Vector3(0, -.03f, 0), art.hay);
                        var produce = new[] { new Color(.7f, .15f, .12f), new Color(.85f, .75f, .6f), new Color(.4f, .6f, .25f), new Color(.8f, .55f, .15f) }[i % 4];
                        for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, t, at + new Vector3((k - 1) * .09f, .12f + (k % 2) * .03f, (k % 2) * .08f - .04f), Vector3.one * .15f, Tint(art.foliage, produce));
                        break;
                    case 1:   // bolts of cloth laid across the counter, and clay pots
                        if (i % 2 == 0) Part(PrimitiveType.Cylinder, t, at + new Vector3(0, .07f, 0), new Vector3(.2f, .26f, .2f), Tint(art.cloth, Color.HSVToRGB(R(), .5f, .6f)), Quaternion.Euler(90, 0, 0));
                        else MeshPart(PropMesh("Pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.07f, 0), new Vector2(.13f, .1f), new Vector2(.13f, .16f), new Vector2(.07f, .24f), new Vector2(.09f, .28f), new Vector2(.06f, .28f), new Vector2(.06f, .25f), new Vector2(0, .25f) }, 10)), t, at + new Vector3(0, -.03f, 0), Tint(art.stone, new Color(.62f, .4f, .28f)));
                        break;
                    case 3:   // salt blocks, bone harpoon heads, strips of dried meat
                        if (i % 3 == 0) Part(PrimitiveType.Cube, t, at + new Vector3(0, .1f, 0), new Vector3(.24f, .2f, .2f), Tint(art.stone, new Color(.94f, .94f, .91f)), Quaternion.Euler(0, R() * 40 - 20, 0));
                        else if (i % 3 == 1) Part(PrimitiveType.Cube, t, at + new Vector3(0, .03f, 0), new Vector3(.07f, .05f, .34f), Bone, Quaternion.Euler(0, R() * 50 - 25, 0));
                        else Part(PrimitiveType.Capsule, t, at + new Vector3(0, .05f, 0), new Vector3(.08f, .14f, .08f), Tint(art.cloth, new Color(.36f, .16f, .12f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                    default:  // loaves and rolls
                        Part(PrimitiveType.Capsule, t, at + new Vector3(0, .07f, 0), new Vector3(.2f, .17f, .2f), Tint(art.hay, new Color(.72f, .48f, .22f)), Quaternion.Euler(90, R() * 60, 0));
                        break;
                }
            }
            Crate(t, new Vector3(1.9f, .3f, .2f), .6f, 12);
            Part(PrimitiveType.Sphere, t, new Vector3(-1.9f, .33f, .3f), new Vector3(.55f, .66f, .5f), art.cloth);                                // sack
            Part(PrimitiveType.Cylinder, t, new Vector3(-1.9f, .68f, .3f), new Vector3(.16f, .06f, .16f), art.cloth);                             // its tied neck
            Solid(t, new Vector3(0, .5f, -.45f), new Vector3(2.9f, 1, .8f));
            Workplace(t, "stall", new Vector3(0, 0, .4f), new Vector3(0, 1.1f, -1.5f));
        }
'''),
(r'Scripts\World\ZoneBuilder.cs', 'Woodpile',
r'''        /// <summary>Woodpile: stacked logs, a chopping block with an axe in it, split wood scattered about.</summary>
        void Woodpile(Transform t)
        {
            var bark = art.bark; var split = Tint(art.timber, new Color(.62f, .5f, .34f));
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 5 - row; i++)
                    Part(PrimitiveType.Cylinder, t, new Vector3(-1.4f + row * .25f + i * .5f, .25f + row * .42f, .6f), new Vector3(.45f, 1.1f, .45f), bark, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cylinder, t, new Vector3(1.4f, .3f, -.6f), new Vector3(.7f, .3f, .7f), bark);                                   // chopping block
            Part(PrimitiveType.Cylinder, t, new Vector3(1.4f, .61f, -.6f), new Vector3(.66f, .01f, .66f), split);
            Part(PrimitiveType.Cylinder, t, new Vector3(1.3f, .95f, -.6f), new Vector3(.04f, .35f, .04f), art.timber, Quaternion.Euler(0, 0, 25));  // axe handle
            Part(PrimitiveType.Cube, t, new Vector3(1.43f, .66f, -.6f), new Vector3(.2f, .06f, .14f), art.metal, Quaternion.Euler(0, 0, 25));
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Cube, t, new Vector3(.6f + (i % 3) * .5f, .08f, -1.3f + (i / 3) * .4f), new Vector3(.14f, .14f, .5f), split, Quaternion.Euler(0, i * 37, 90));
            Solid(t, new Vector3(-.4f, .6f, .6f), new Vector3(3, 1.3f, 1.2f));
            Workplace(t, "woodpile", new Vector3(1.4f, 0, -1.4f), new Vector3(1.4f, .6f, -.6f));
        }
''',
r'''        /// <summary>
        /// Woodpile: logs with pale cut ends stacked between stakes, a chopping block with an axe in it, split wood scattered
        /// about, and a sawhorse with a log on it in front of the stack (for the eye only: it has no collider).
        /// </summary>
        void Woodpile(Transform t)
        {
            var bark = art.bark; var split = Tint(art.timber, new Color(.62f, .5f, .34f)); var dark = Tint(art.timber, new Color(.3f, .21f, .13f));
            var log = PropMesh("Log", () => TwoTone(Turned(new[] { new Vector2(.225f, -1.1f), new Vector2(.225f, 1.1f) }, 9),
                Joined(Piece(Turned(new[] { new Vector2(0, -1.1f), new Vector2(.225f, -1.1f) }, 9)), Piece(Turned(new[] { new Vector2(.225f, 1.1f), new Vector2(0, 1.1f) }, 9)))));
            var longs = new[] { 1, .94f, 1.03f, .9f, .97f }; var skews = new[] { -2, 1.5f, 0, 2.5f, -1 };   // fixed tables: no two neighbours alike
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 5 - row; i++)
                    MeshPart2(log, t, new Vector3(-1.4f + row * .25f + i * .5f, .25f + row * .42f, .6f), bark, split, Quaternion.Euler(0, skews[(row * 2 + i) % 5], 0) * Quaternion.Euler(90, 0, 0)).transform.localScale = new Vector3(1, longs[(row * 3 + i) % 5], 1);
            foreach (float x in new[] { -1.71f, .91f }) foreach (float z in new[] { .1f, 1.1f }) Stake(t, new Vector3(x, -.1f, z), .14f, 1.24f, dark);   // the stakes that hold the stack
            MeshPart2(PropMesh("Chopping block", () => TwoTone(Turned(new[] { new Vector2(.39f, 0), new Vector2(.35f, .2f), new Vector2(.34f, .6f) }, 12), Turned(new[] { new Vector2(.34f, .6f), new Vector2(0, .6f) }, 12))), t, new Vector3(1.4f, 0, -.6f), bark, split);
            Part(PrimitiveType.Cylinder, t, new Vector3(1.3f, .95f, -.6f), new Vector3(.05f, .35f, .05f), art.timber, Quaternion.Euler(0, 0, 25));  // axe handle
            Part(PrimitiveType.Cube, t, new Vector3(1.43f, .66f, -.6f), new Vector3(.26f, .08f, .17f), art.metal, Quaternion.Euler(0, 0, 25));
            for (int i = 0; i < 6; i++) Part(PrimitiveType.Cube, t, new Vector3(.6f + (i % 3) * .5f, .08f, -1.3f + (i / 3) * .4f), new Vector3(.16f, .15f, .5f), split, Quaternion.Euler(0, i * 37, 90));
            // The sawhorse: two crossed trestles on a rail, a log lying in their forks. It stands clear of the stack's log ends (they reach z -.53).
            foreach (float x in new[] { -1.05f, .05f }) foreach (int s in new[] { -1, 1 })
                Bar(t, new Vector3(x, 0, -1 - s * .26f), new Vector3(x, .78f, -1 + s * .26f), .08f, .07f, dark);
            Part(PrimitiveType.Cube, t, new Vector3(-.5f, .34f, -1), new Vector3(1.3f, .08f, .08f), dark);
            MeshPart2(log, t, new Vector3(-.5f, .68f, -1), bark, split, Quaternion.Euler(0, 0, 90)).transform.localScale = new Vector3(.71f, .8f, .71f);
            Solid(t, new Vector3(-.4f, .6f, .6f), new Vector3(3, 1.3f, 1.2f));
            Workplace(t, "woodpile", new Vector3(1.4f, 0, -1.4f), new Vector3(1.4f, .6f, -.6f));
        }
'''),
(r'Scripts\World\ZoneBuilder.Verdant.cs', "the rope bridge's posts",
r'''                for (int k = 0; k <= spans; k++) { var p = Post(k); float tall = k == 0 || k == spans ? 1.5f : 1.2f; Part(PrimitiveType.Cylinder, t, p + new Vector3(0, tall / 2 - .35f, 0), new Vector3(.17f, tall / 2 + .35f, .17f), post); }
''',
r'''                for (int k = 0; k <= spans; k++) { var p = Post(k); float tall = k == 0 || k == spans ? 1.5f : 1.2f; Stake(t, p + new Vector3(0, -.7f, 0), .2f, tall + .7f, post); }   // hewn and capped, as deep in the bank as the old round ones
'''),
]

def main():
    files = {}
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path)
        if full not in files: files[full] = io.open(full, 'r', encoding='utf-8', newline='').read()
    out = dict(files); problems = []
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path); text = files[full]
        if '\r\n' in text: old, new = old.replace('\n', '\r\n'), new.replace('\n', '\r\n')   # the file's own line ends
        n = text.count(old)
        if n != 1: problems.append('%s: %s: the old block occurs %d times, not once' % (path, what, n)); continue
        if out[full].count(old) != 1: problems.append('%s: %s: overlaps another edit' % (path, what)); continue
        out[full] = out[full].replace(old, new)
    if problems:
        sys.exit('patch_p4: NOTHING WRITTEN.\n  ' + '\n  '.join(problems))
    for full, text in out.items():
        io.open(full, 'w', encoding='utf-8', newline='').write(text)
        print('patched %s (%d edits)' % (full, sum(1 for e in EDITS if os.path.join(ROOT, e[0]) == full)))
    print('patch_p4: OK')

if __name__ == '__main__':
    main()
