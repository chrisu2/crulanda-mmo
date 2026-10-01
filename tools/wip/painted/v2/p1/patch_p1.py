"""The painted style pass, part 1 (buildings and building textures). Usage: python patch_p1.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   ZoneArt.cs          gains `masonry`.
   ZoneSceneBuilder.cs becomes a partial class and EnsureArt calls PaintedTextures(art) (ZoneSceneBuilder.Painted.cs, which
                       install_p1.py copies into place; run this script through install_p1.py).
   ZoneMeshes.cs       gains Box (metre UVs).
   ZoneBuilder.cs      gains Masonry, BoxPart, Eaves, Window and PlankDoor, and House, Inn and Barn use them.

   Anchor based: every anchor must occur exactly once in its file, and every file is checked before any file is written, so a
   second run (or a run against a tree that has drifted) fails on the asserts and leaves the tree as it was."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'

PATCHES = []
def patch(rel, reps): PATCHES.append((os.path.join(ROOT, *rel.split('/')), reps))

# ---------------------------------------------------------------- ZoneArt.cs
patch('Scripts/World/ZoneArt.cs', [
("""        public Material fern, broadLeaf, reeds;
""",
"""        public Material fern, broadLeaf, reeds;
        [Tooltip("Painted dressed stone for plinths, chimneys, footings and hearths (art.stone stays natural rock).")]
        public Material masonry;
"""),
])

# ---------------------------------------------------------------- ZoneSceneBuilder.cs
patch('Editor/ZoneSceneBuilder.cs', [
("""    public static class ZoneSceneBuilder
""",
 """    public static partial class ZoneSceneBuilder
"""),
# Once every base material exists, and before the palette is marked dirty and saved.
("""            if (art.reeds == null) art.reeds = PlantCard("Reeds", reedTex, .12f);
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            return art;""",
"""            if (art.reeds == null) art.reeds = PlantCard("Reeds", reedTex, .12f);
            PaintedTextures(art);   // the painted style pass: plaster, thatch, slate and timber repainted, and the masonry material (ZoneSceneBuilder.Painted.cs)
            EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            return art;"""),
])

# ---------------------------------------------------------------- ZoneMeshes.cs
patch('Scripts/World/ZoneMeshes.cs', [
("""        /// <summary>Upright cone with its base centred at the origin.</summary>
        public static Mesh Cone(float radius, float height, int sides = 10)""",
"""        /// <summary>
        /// A box with planar UVs in metres on every face (<paramref name="tile"/> metres a tile), so a painted wall texture holds
        /// its scale on any size of wall. On the four sides u runs left to right seen from outside and v runs up; the top and
        /// bottom use (x, z). Centred like a cube primitive. <paramref name="origin"/> is where the box sits in its building, so
        /// the texture runs on unbroken from one box to the next on the same wall; without it the texture starts at the box's
        /// own corner and foot.
        /// </summary>
        public static Mesh Box(Vector3 size, float tile = 2, Vector3? origin = null)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float hx = size.x / 2, hy = size.y / 2, hz = size.z / 2; var o = origin ?? new Vector3(hx, hy, hz);
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Func<Vector3, Vector2> map)
            {
                Quad(v, t, a, b, c, d);
                foreach (var p in new[] { a, b, c, d }) uv.Add(map(p + o) / tile);
            }
            // Each face is wound clockwise seen from outside, like the roof's quads: -z, +z, +x, -x, top, bottom.
            Face(new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, -hy, -hz), p => new Vector2(p.x, p.y));
            Face(new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, -hy, hz), p => new Vector2(-p.x, p.y));
            Face(new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(hx, -hy, hz), p => new Vector2(p.z, p.y));
            Face(new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz), new Vector3(-hx, -hy, -hz), p => new Vector2(-p.z, p.y));
            Face(new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), p => new Vector2(p.x, p.z));
            Face(new Vector3(-hx, -hy, hz), new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), p => new Vector2(p.x, p.z));
            var m = Build("Box", v, t); m.SetUVs(0, uv); return m;
        }
        /// <summary>Upright cone with its base centred at the origin.</summary>
        public static Mesh Cone(float radius, float height, int sides = 10)"""),
])

# ---------------------------------------------------------------- ZoneBuilder.cs
patch('Scripts/World/ZoneBuilder.cs', [
# Masonry, next to Tint.
("""        Material Tint(Material baseMat, Color color)
        {""",
"""        /// <summary>Dressed stone; falls back to natural stone until the art asset has been regenerated.</summary>
        Material Masonry { get { return art.masonry != null ? art.masonry : art.stone; } }
        Material Tint(Material baseMat, Color color)
        {"""),
# BoxPart, next to MeshPart.
("""        void Solid(Transform root, Vector3 center, Vector3 size)
        {
            var box = root.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;""",
"""        /// <summary>A box whose painted texture tiles per metre (walls, plinths, chimneys) and runs on unbroken from one box of a
        /// building to the next; see <see cref="ZoneMeshes.Box"/>.</summary>
        GameObject BoxPart(Transform parent, Vector3 localPos, Vector3 size, Material m, Quaternion? rot = null, float tile = 2)
        {
            return MeshPart(ZoneMeshes.Box(size, tile, localPos), parent, localPos, m, rot);
        }
        void Solid(Transform root, Vector3 center, Vector3 size)
        {
            var box = root.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;"""),
# The trim, and the house's plinth and walls.
("""        static readonly Color[] Plaster = { new Color(.78f, .72f, .60f), new Color(.70f, .66f, .58f), new Color(.74f, .64f, .52f) };
        void House(Transform t, Vector2 size, float wallHeight, int variant, bool inn)
        {
            float w = size.x, d = size.y;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var roofMat = variant % 2 == 0 ? art.thatch : art.slate;
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the plinth reaches down to the lowest ground under it
            Part(PrimitiveType.Cube, t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f + wallHeight / 2, 0), new Vector3(w, wallHeight, d), plaster);""",
"""        static readonly Color[] Plaster = { new Color(.88f, .82f, .68f), new Color(.8f, .76f, .66f), new Color(.86f, .72f, .54f) };
        static readonly Color[] Shutter = { new Color(.3f, .38f, .3f), new Color(.42f, .28f, .18f), new Color(.32f, .3f, .4f) };
        /// <summary>
        /// The eaves of a gable roof (the painted style pass, 2026-10-01): a fascia board along each eave, rafter ends hung under
        /// the soffit from the wall out to the fascia, and a ridge cap seated on the ridge. <paramref name="w"/> and
        /// <paramref name="d"/> are the walls' footprint, <paramref name="top"/> the wall top (the roof's eave line); the roof
        /// reaches .7 m past the walls front and back and its slab is .25 m thick.
        /// </summary>
        void Eaves(Transform t, float w, float d, float top, float roofH, Material roofMat)
        {
            var dark = Tint(art.timber, new Color(.26f, .17f, .11f));
            int n = Mathf.Max(1, Mathf.FloorToInt((w - .5f) / .9f));
            foreach (int sz in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, top - .2f, sz * (d / 2 + .72f)), new Vector3(w + 1.3f, .38f, .1f), dark);   // fascia: top-.39 .. top-.01, over the eave edge and the rafter ends
                for (int i = 0; i <= n; i++) Part(PrimitiveType.Cube, t, new Vector3((i - n / 2f) * .9f, top - .31f, sz * (d / 2 + .33f)), new Vector3(.12f, .14f, .7f), dark);   // rafter ends: top-.38 .. top-.24, 1 cm into the soffit (top-.25), from 2 cm inside the wall face to 1 cm into the fascia
            }
            float sag = roofH * .17f / (d / 2 + .7f);   // how far the slope has dropped at the cap's edge
            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .05f - (sag + .07f) / 2, 0), new Vector3(w + 1.3f, sag + .07f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));   // ridge cap: its bottom edges sink 2 cm into the slopes
        }
        /// <summary>
        /// A framed window on a wall: <paramref name="face"/> is the point on the OUTER WALL FACE at the window's centre and
        /// <paramref name="sz"/> the way that face looks (+1 or -1 along z). Frame, sill and shutters are all set into the wall
        /// by 1 to 2 cm. <paramref name="glass"/> is false where the wall already has its pane (the inn's passes through the wall).
        /// </summary>
        void Window(Transform t, Vector3 face, float wide, float high, int sz, int variant, bool shutters, bool glass = true)
        {
            var frame = Tint(art.timber, new Color(.24f, .16f, .1f));
            var at = face + new Vector3(0, 0, sz * .05f);
            if (glass) Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, .08f), art.glass);   // face+.01 .. face+.09
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, at + new Vector3(0, s * (high / 2 + .04f), 0), new Vector3(wide + .16f, .08f, .14f), frame);   // face-.02 .. face+.12
                Part(PrimitiveType.Cube, t, at + new Vector3(s * (wide / 2 + .04f), 0, 0), new Vector3(.08f, high, .14f), frame);
            }
            Part(PrimitiveType.Cube, t, face + new Vector3(0, -high / 2 - .1f, sz * .11f), new Vector3(wide + .3f, .08f, .26f), frame);   // the sill: face-.02 .. face+.24
            if (shutters) foreach (int s in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, face + new Vector3(s * (wide / 2 + .24f), 0, sz * .02f), new Vector3(.3f, high + .1f, .06f), Tint(art.timber, Shutter[Mathf.Abs(variant) % Shutter.Length]));   // on the wall: face-.01 .. face+.05, 1 cm clear of the frame
        }
        /// <summary>A plank door facing -z: the slab (returned), three plank seams, two iron bands and a ring, the iron set 5 mm into the planks.</summary>
        GameObject PlankDoor(Transform t, Vector3 at, float wide, float high, Material planks, float thick = .06f)
        {
            var slab = Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, thick), planks);
            for (int i = -1; i <= 1; i++) Part(PrimitiveType.Cube, t, at + new Vector3(i * wide / 4, 0, -thick / 2 - .005f), new Vector3(.025f, high, .01f), Tint(art.timber, new Color(.1f, .07f, .05f)));
            foreach (float y in new[] { -high * .28f, high * .25f }) Part(PrimitiveType.Cube, t, at + new Vector3(0, y, -thick / 2 - .01f), new Vector3(wide * .92f, .09f, .03f), art.metal);
            Part(PrimitiveType.Cylinder, t, at + new Vector3(wide * .3f, -high * .05f, -thick / 2 - .01f), new Vector3(.14f, .015f, .14f), art.metal, Quaternion.Euler(90, 0, 0));   // the ring, between the bands
            return slab;
        }
        void House(Transform t, Vector2 size, float wallHeight, int variant, bool inn)
        {
            float w = size.x, d = size.y;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var roofMat = variant % 2 == 0 ? art.thatch : art.slate;
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the plinth reaches down to the lowest ground under it
            BoxPart(t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), Masonry, null, 1.5f);
            BoxPart(t, new Vector3(0, .6f + wallHeight / 2, 0), new Vector3(w, wallHeight, d), plaster);"""),
# The house: plank door, framed windows, eaves, a masonry chimney with a cap.
("""            float foot = Mathf.Min(0, LocalGround(t, 0, -d / 2 - .19f)) - .05f;
            Part(PrimitiveType.Cube, t, new Vector3(0, (foot + 2.7f) / 2, -d / 2 - .19f), new Vector3(1.2f, 2.7f - foot, .06f), Tint(art.timber, new Color(.22f, .14f, .09f)));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .68f, (foot + 2.86f) / 2, -d / 2 - .12f), new Vector3(.16f, 2.86f - foot, .24f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.78f, -d / 2 - .12f), new Vector3(1.52f, .16f, .24f), art.timber);
            if (!inn) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .1f)) });
            for (float x = -w / 2 + 1.4f; x < w / 2 - .8f; x += 2.6f)
            {
                if (Mathf.Abs(x) < 1.2f) continue;
                foreach (int sz in new[] { -1, 1 })
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * (d / 2 + .05f)), new Vector3(.8f, .7f, .08f), art.glass);
                if (inn) foreach (int sz in new[] { -1, 1 })
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + wallHeight * .78f, sz * (d / 2 + .05f)), new Vector3(.8f, .7f, .08f), art.glass);
            }
            float roofH = Mathf.Max(2.2f, d * .55f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, top, 0), roofMat);
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, top + roofH * .75f, d * .15f), new Vector3(.9f, roofH * 1.1f, .9f), art.stone);""",
"""            float foot = Mathf.Min(0, LocalGround(t, 0, -d / 2 - .19f)) - .05f;
            PlankDoor(t, new Vector3(0, (foot + 2.7f) / 2, -d / 2 - .19f), 1.2f, 2.7f - foot, Tint(art.timber, new Color(.3f, .2f, .12f)));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .68f, (foot + 2.86f) / 2, -d / 2 - .12f), new Vector3(.16f, 2.86f - foot, .24f), art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.78f, -d / 2 - .12f), new Vector3(1.52f, .16f, .24f), art.timber);
            if (!inn) Doors.Add(new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .1f)) });
            for (float x = -w / 2 + 1.4f; x < w / 2 - .8f; x += 2.6f)
            {
                if (Mathf.Abs(x) < 1.2f) continue;
                // Shutters only where they clear the corner post (inner face at w/2 - .15) and, on the door's wall, the door jamb (outer edge at .76).
                foreach (int sz in new[] { -1, 1 })
                    Window(t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * d / 2), .8f, .7f, sz, variant, Mathf.Abs(x) + .79f <= w / 2 - .15f && (sz > 0 || Mathf.Abs(x) - .79f >= .78f));
                if (inn) foreach (int sz in new[] { -1, 1 })
                    Window(t, new Vector3(x, .6f + wallHeight * .78f, sz * d / 2), .8f, .7f, sz, variant, false);
            }
            float roofH = Mathf.Max(2.2f, d * .55f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, top, 0), roofMat);
            Eaves(t, w, d, top, roofH, roofMat);
            BoxPart(t, new Vector3(w / 2 - 1.1f, top + roofH * .75f, d * .15f), new Vector3(.9f, roofH * 1.1f, .9f), Masonry, null, 1);
            BoxPart(t, new Vector3(w / 2 - 1.1f, top + roofH * 1.3f + .04f, d * .15f), new Vector3(1.1f, .12f, 1.1f), Tint(Masonry, new Color(.45f, .44f, .4f)), null, 1);   // the chimney's cap, 2 cm down over the stack"""),
# The inn: painted wall pieces.
("""            GameObject WallPiece(Vector3 pos, Vector3 scale)
            {
                var o = Part(PrimitiveType.Cube, t, pos, scale, plaster);
                o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o;
            }""",
"""            GameObject WallPiece(Vector3 pos, Vector3 scale)
            {
                var o = BoxPart(t, pos, scale, plaster);
                o.AddComponent<BoxCollider>(); o.AddComponent<NavBlocker>(); return o;
            }"""),
# The inn: masonry footing strips with metre UVs.
("""            var footing = Tint(art.stone, new Color(.45f, .44f, .41f));
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (w / 2 + .12f), .15f, 0), new Vector3(.25f, .3f, d + .5f), footing);
            Part(PrimitiveType.Cube, t, new Vector3(0, .15f, d / 2 + .12f), new Vector3(w + .5f, .3f, .25f), footing);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * (doorW / 2 + side / 2 + .1f), .15f, -d / 2 - .12f), new Vector3(side + .3f, .3f, .25f), footing);""",
"""            var footing = Tint(Masonry, new Color(.5f, .48f, .44f));
            foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(s * (w / 2 + .12f), .15f, 0), new Vector3(.25f, .3f, d + .5f), footing, null, 1.5f);
            BoxPart(t, new Vector3(0, .15f, d / 2 + .12f), new Vector3(w + .5f, .3f, .25f), footing, null, 1.5f);
            foreach (int s in new[] { -1, 1 }) BoxPart(t, new Vector3(s * (doorW / 2 + side / 2 + .1f), .15f, -d / 2 - .12f), new Vector3(side + .3f, .3f, .25f), footing, null, 1.5f);"""),
# The inn: frames, sills and shutters round the through-wall glass (no second pane), and eaves.
("""                    Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, sz * d / 2), new Vector3(.9f, .8f, wall + .1f), art.glass);
                    Part(PrimitiveType.Cube, t, new Vector3(x, storey + 1.3f, sz * d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                }
            float roofH = Mathf.Max(2.4f, d * .5f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            if (variant == 1) CrackedHearth(t, w, H, roofH);   // the Cracked Hearth: its chimney breast split, the fire showing through
            else Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, H + roofH * .75f, d * .15f), new Vector3(1, roofH * 1.1f, 1), art.stone);""",
"""                    Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, sz * d / 2), new Vector3(.9f, .8f, wall + .1f), art.glass);
                    Part(PrimitiveType.Cube, t, new Vector3(x, storey + 1.3f, sz * d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                    // Frames and sills round that glass on the outer wall face; shutters downstairs where they clear the corner post (inner face at w/2 - .17) and the door opening.
                    Window(t, new Vector3(x, 1.5f, sz * (d / 2 + wall / 2)), .9f, .8f, sz, variant, Mathf.Abs(x) + .84f <= w / 2 - .17f && (sz > 0 || Mathf.Abs(x) - .84f >= doorW / 2 + .05f), false);
                    Window(t, new Vector3(x, storey + 1.3f, sz * (d / 2 + wall / 2)), .8f, .7f, sz, variant, false, false);
                }
            float roofH = Mathf.Max(2.4f, d * .5f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            Eaves(t, w, d, H, roofH, variant % 2 == 0 ? art.thatch : art.slate);
            if (variant == 1) CrackedHearth(t, w, H, roofH);   // the Cracked Hearth: its chimney breast split, the fire showing through
            else
            {
                BoxPart(t, new Vector3(w / 2 - 1.1f, H + roofH * .75f, d * .15f), new Vector3(1, roofH * 1.1f, 1), Masonry, null, 1);
                BoxPart(t, new Vector3(w / 2 - 1.1f, H + roofH * 1.3f + .04f, d * .15f), new Vector3(1.2f, .12f, 1.2f), Tint(Masonry, new Color(.45f, .44f, .4f)), null, 1);   // its cap
            }"""),
# The inn: the hinged door is a plank door too, and the step is masonry.
("""            var door = Part(PrimitiveType.Cube, hinge, new Vector3(doorW / 2, doorH / 2, 0), new Vector3(doorW, doorH, .1f), dark);
            var doorCollider = door.AddComponent<BoxCollider>();
            Part(PrimitiveType.Cube, t, new Vector3(0, .06f, -d / 2 - .55f), new Vector3(2, .12f, .8f), art.stone);""",
"""            var door = PlankDoor(hinge, new Vector3(doorW / 2, doorH / 2, 0), doorW, doorH, dark, .1f);
            var doorCollider = door.AddComponent<BoxCollider>();
            BoxPart(t, new Vector3(0, .06f, -d / 2 - .55f), new Vector3(2, .12f, .8f), Masonry, null, 1);"""),
# The inn: the hearth in masonry.
("""            var hearth = Part(PrimitiveType.Cube, t, new Vector3(w / 2 - .6f, 1, .8f), new Vector3(.9f, 2, 2.4f), art.stone);""",
 """            var hearth = BoxPart(t, new Vector3(w / 2 - .6f, 1, .8f), new Vector3(.9f, 2, 2.4f), Masonry, null, 1);"""),
# The barn: painted boards, a masonry footing, eaves.
("""            var boards = Tint(art.timber, new Color(.36f, .22f, .15f));
            Part(PrimitiveType.Cube, t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
            float drop = FootDrop(t, w, d); if (drop > 0) Part(PrimitiveType.Cube, t, new Vector3(0, .1f - drop / 2, 0), new Vector3(w + .2f, drop + .2f, d + .2f), art.stone);   // stone footing on a slope""",
"""            var boards = Tint(art.timber, new Color(.4f, .25f, .16f));
            BoxPart(t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
            float drop = FootDrop(t, w, d); if (drop > 0) BoxPart(t, new Vector3(0, .1f - drop / 2, 0), new Vector3(w + .2f, drop + .2f, d + .2f), Masonry, null, 1.5f);   // stone footing on a slope"""),
("""            float roofH = d * .5f;
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH), t, new Vector3(0, h, 0), art.thatch);
            Solid(t, new Vector3(0, (h + roofH) / 2, 0), new Vector3(w + .3f, h + roofH, d + .3f));""",
"""            float roofH = d * .5f;
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH), t, new Vector3(0, h, 0), art.thatch);
            Eaves(t, w - .2f, d, h, roofH, art.thatch);   // the barn's roof is 1 m wider than its walls, not 1.2
            Solid(t, new Vector3(0, (h + roofH) / 2, 0), new Vector3(w + .3f, h + roofH, d + .3f));"""),
])

# Check everything, then write everything.
out = []
for path, reps in PATCHES:
    s = io.open(path, encoding='utf-8', newline='').read()
    assert '\r' not in s, (path, 'expected LF line endings')
    for a, b in reps:
        assert s.count(a) == 1, (path, 'anchor occurs %d times, expected exactly once' % s.count(a), a[:100])
        s = s.replace(a, b)
    out.append((path, s))
for path, s in out:
    io.open(path, 'w', encoding='utf-8', newline='').write(s); print('patched', os.path.basename(path))
