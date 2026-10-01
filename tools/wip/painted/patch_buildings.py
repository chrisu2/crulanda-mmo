"""The painted style pass, part 1 (buildings): ZoneMeshes.Box (metre UVs), ZoneBuilder.BoxPart, and houses, inns and barns
   rebuilt with painted walls, eaves with fascia and rafter ends, a ridge, framed windows with shutters, plank doors with iron."""
import io
W = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World'
def patch(path, reps):
    s = io.open(path, encoding='utf-8', newline='').read()
    for a, b in reps:
        assert s.count(a) == 1, (path, a[:80])
        s = s.replace(a, b)
    io.open(path, 'w', encoding='utf-8', newline='').write(s); print('patched', path.split('\\')[-1])

patch(W + r'\ZoneMeshes.cs', [
("""        /// <summary>Upright cone with its base centred at the origin.</summary>
        public static Mesh Cone(float radius, float height, int sides = 10)""",
"""        /// <summary>
        /// A box with planar UVs in metres on every face (<paramref name="tile"/> metres a tile; v runs up from the foot), so painted
        /// wall textures hold their scale on any size of wall. Centred like a cube primitive.
        /// </summary>
        public static Mesh Box(Vector3 size, float tile = 2)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float hx = size.x / 2, hy = size.y / 2, hz = size.z / 2;
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Func<Vector3, Vector2> map)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                foreach (var p in new[] { a, b, c, d }) uv.Add(map(p) / tile);
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            // Each face wound clockwise seen from outside; u along the face, v up (along z on the top and bottom).
            Face(new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, -hy, -hz), p => new Vector2(p.x + hx, p.y + hy));
            Face(new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, -hy, hz), p => new Vector2(hx - p.x, p.y + hy));
            Face(new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(hx, -hy, hz), p => new Vector2(p.z + hz, p.y + hy));
            Face(new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz), new Vector3(-hx, -hy, -hz), p => new Vector2(hz - p.z, p.y + hy));
            Face(new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), p => new Vector2(p.x + hx, p.z + hz));
            Face(new Vector3(-hx, -hy, hz), new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), p => new Vector2(p.x + hx, p.z + hz));
            var m = new Mesh { name = "Box" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        /// <summary>Upright cone with its base centred at the origin.</summary>
        public static Mesh Cone(float radius, float height, int sides = 10)"""),
])

patch(W + r'\ZoneBuilder.cs', [
# BoxPart next to MeshPart.
("""        void Solid(Transform root, Vector3 center, Vector3 size)
        {
            var box = root.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;""",
"""        /// <summary>A box whose painted texture tiles per metre (walls, plinths, chimneys); see <see cref="ZoneMeshes.Box"/>.</summary>
        GameObject BoxPart(Transform parent, Vector3 localPos, Vector3 size, Material m, Quaternion? rot = null, float tile = 2)
        {
            return MeshPart(ZoneMeshes.Box(size, tile), parent, localPos, m, rot);
        }
        void Solid(Transform root, Vector3 center, Vector3 size)
        {
            var box = root.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;"""),
# The house: painted walls, trim.
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
        /// <summary>The trim that makes a painted house: fascia boards and rafter ends under the eaves, a ridge cap, framed windows with
        /// shutters and sills, and a plank door with iron bands and a ring. (The painted style pass, 2026-10-01.)</summary>
        void Eaves(Transform t, float w, float d, float top, float roofH, Material roofMat)
        {
            var dark = Tint(art.timber, new Color(.26f, .17f, .11f));
            foreach (int sz in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, top - .14f, sz * (d / 2 + .72f)), new Vector3(w + 1.3f, .24f, .1f), dark);
                for (float x = -w / 2 + .2f; x < w / 2; x += .9f) Part(PrimitiveType.Cube, t, new Vector3(x, top - .2f, sz * (d / 2 + .34f)), new Vector3(.12f, .14f, .74f), dark);
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .04f, 0), new Vector3(w + 1.3f, .16f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));
        }
        void Window(Transform t, Vector3 at, float wide, float high, int sz, int variant, bool shutters)
        {
            var frame = Tint(art.timber, new Color(.24f, .16f, .1f));
            Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, .08f), art.glass);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, at + new Vector3(0, s * (high / 2 + .04f), sz * .03f), new Vector3(wide + .16f, .08f, .12f), frame);
                Part(PrimitiveType.Cube, t, at + new Vector3(s * (wide / 2 + .04f), 0, sz * .03f), new Vector3(.08f, high, .12f), frame);
            }
            Part(PrimitiveType.Cube, t, at + new Vector3(0, -high / 2 - .1f, sz * .08f), new Vector3(wide + .3f, .08f, .22f), frame);   // the sill
            if (shutters) foreach (int s in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, at + new Vector3(s * (wide / 2 + .26f), 0, sz * .04f), new Vector3(.3f, high + .1f, .05f), Tint(art.timber, Shutter[Mathf.Abs(variant) % Shutter.Length]));
        }
        void PlankDoor(Transform t, Vector3 at, float wide, float high, Material planks)
        {
            Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, .06f), planks);
            for (float x = -wide / 2 + wide / 4; x < wide / 2 - .05f; x += wide / 4) Part(PrimitiveType.Cube, t, at + new Vector3(x, 0, -.035f), new Vector3(.025f, high, .01f), Tint(art.timber, new Color(.1f, .07f, .05f)));
            foreach (float y in new[] { -high * .28f, high * .25f }) Part(PrimitiveType.Cube, t, at + new Vector3(0, y, -.045f), new Vector3(wide * .92f, .09f, .02f), art.metal);
            Part(PrimitiveType.Cylinder, t, at + new Vector3(wide * .3f, -high * .05f, -.07f), new Vector3(.14f, .015f, .14f), art.metal, Quaternion.Euler(90, 0, 0));   // the ring
        }
        void House(Transform t, Vector2 size, float wallHeight, int variant, bool inn)
        {
            float w = size.x, d = size.y;
            var plaster = Tint(art.plaster, Plaster[Mathf.Abs(variant) % Plaster.Length]);
            var roofMat = variant % 2 == 0 ? art.thatch : art.slate;
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the plinth reaches down to the lowest ground under it
            BoxPart(t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), art.masonry, null, 1.5f);
            BoxPart(t, new Vector3(0, .6f + wallHeight / 2, 0), new Vector3(w, wallHeight, d), plaster);"""),
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
                foreach (int sz in new[] { -1, 1 })
                    Window(t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * (d / 2 + .05f)), .8f, .7f, sz, variant, true);
                if (inn) foreach (int sz in new[] { -1, 1 })
                    Window(t, new Vector3(x, .6f + wallHeight * .78f, sz * (d / 2 + .05f)), .8f, .7f, sz, variant, false);
            }
            float roofH = Mathf.Max(2.2f, d * .55f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, top, 0), roofMat);
            Eaves(t, w, d, top, roofH, roofMat);
            BoxPart(t, new Vector3(w / 2 - 1.1f, top + roofH * .75f, d * .15f), new Vector3(.9f, roofH * 1.1f, .9f), art.masonry, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, top + roofH * 1.3f + .04f, d * .15f), new Vector3(1.1f, .12f, 1.1f), Tint(art.masonry, new Color(.45f, .44f, .4f)));"""),
# The inn: painted wall pieces and framed windows, eaves; its footing and hearth in masonry.
("""            var footing = Tint(art.stone, new Color(.45f, .44f, .41f));""",
 """            var footing = Tint(art.masonry, new Color(.5f, .48f, .44f));"""),
("""            var hearth = Part(PrimitiveType.Cube, t, new Vector3(w / 2 - .6f, 1, .8f), new Vector3(.9f, 2, 2.4f), art.stone);""",
 """            var hearth = BoxPart(t, new Vector3(w / 2 - .6f, 1, .8f), new Vector3(.9f, 2, 2.4f), art.masonry, null, 1);"""),
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
("""                    Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, sz * d / 2), new Vector3(.9f, .8f, wall + .1f), art.glass);
                    Part(PrimitiveType.Cube, t, new Vector3(x, storey + 1.3f, sz * d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                }
            float roofH = Mathf.Max(2.4f, d * .5f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);""",
"""                    Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, sz * d / 2), new Vector3(.9f, .8f, wall + .1f), art.glass);
                    Part(PrimitiveType.Cube, t, new Vector3(x, storey + 1.3f, sz * d / 2), new Vector3(.8f, .7f, wall + .1f), art.glass);
                    Window(t, new Vector3(x, 1.5f, sz * (d / 2 + wall / 2 + .02f)), .9f, .8f, sz, variant, true);
                    Window(t, new Vector3(x, storey + 1.3f, sz * (d / 2 + wall / 2 + .02f)), .8f, .7f, sz, variant, false);
                }
            float roofH = Mathf.Max(2.4f, d * .5f);
            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            Eaves(t, w, d, H, roofH, variant % 2 == 0 ? art.thatch : art.slate);"""),
# The barn: painted boards.
("""            var boards = Tint(art.timber, new Color(.36f, .22f, .15f));
            Part(PrimitiveType.Cube, t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
            float drop = FootDrop(t, w, d); if (drop > 0) Part(PrimitiveType.Cube, t, new Vector3(0, .1f - drop / 2, 0), new Vector3(w + .2f, drop + .2f, d + .2f), art.stone);   // stone footing on a slope""",
"""            var boards = Tint(art.timber, new Color(.4f, .25f, .16f));
            BoxPart(t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), boards);
            float drop = FootDrop(t, w, d); if (drop > 0) BoxPart(t, new Vector3(0, .1f - drop / 2, 0), new Vector3(w + .2f, drop + .2f, d + .2f), art.masonry, null, 1.5f);   // stone footing on a slope"""),
("""            float roofH = d * .5f;
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH), t, new Vector3(0, h, 0), art.thatch);
            Solid(t, new Vector3(0, (h + roofH) / 2, 0), new Vector3(w + .3f, h + roofH, d + .3f));""",
"""            float roofH = d * .5f;
            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH), t, new Vector3(0, h, 0), art.thatch);
            Eaves(t, w - .2f, d, h, roofH, art.thatch);
            Solid(t, new Vector3(0, (h + roofH) / 2, 0), new Vector3(w + .3f, h + roofH, d + .3f));"""),
])
