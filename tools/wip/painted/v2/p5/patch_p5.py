"""The painted style pass, part 5 (painted masonry on built stone): patch. Usage: python patch_p5.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Scripts\\World\\ZoneMeshes.cs
     Spire          a cone roof with UVs: each facet is unfolded flat (metres along the eave, metres up the slope).
   Scripts\\World\\ZoneBuilder.cs
     shared         Dressed (the masonry in a zone's tint, lifted so a wall keeps the colour it had), Ashlar and Stonework
                    (boxes with metre UVs joined into one mesh), Headstone (a slab with a rounded head).
     Tower          one turned drum (plinth course, shaft, a corbelled ring under the battlements), the merlons one mesh,
                    a slate roof with UVs, three arrow slits. The capsule collider is untouched.
     Wall           body, plinth course, a coping under the battlements and the merlons: one mesh a segment.
     Gate           span, a string course, stepped corbels (were two cubes on edge) and merlons: one mesh.
     Stronghold     footing; body, string course, merlons, door surround and step in one mesh.
     Crypt          the vault, a cornice, pillars and steps in one mesh; the barrow's megaliths and the grave markers are
                    painted rock (single stones, not coursed), the markers headstones.
     Ruin           each block a box with metre UVs.
     RuinedHouse    the floor slab.
     Wayshrine      steps, pillar, a cap course and the niche house in one mesh.
     Shrine         the dais step is masonry; the altar-stone and the stele are painted rock. Soot stays.
     CrackedHearth  breast, upper breast, stack, cap and the fallen stones. The breast's collider is set to the old box.
     Forge, Oven    back wall, hearth, hood, chimney; the oven's plinth.
     grave, exits   a headstone; a waystone with chamfered shoulders (painted rock).
   Scripts\\World\\ZoneBuilder.Verdant.cs
     Treehouse      the round house's footing (a turned drum) and its step.

   Nothing here draws from the zone's random stream that did not before, and every draw is made in the old order (Ruin:
   three a block; Crypt: five a grave marker; the grave prop: two; Wayshrine: fifteen). No Solid, collider, light,
   workplace or named child changes size or place.

   Everything is checked before anything is written: every old block must occur exactly once in its file. So a second
   run, or a run on a tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
ZB = r'Scripts\World\ZoneBuilder.cs'

# (file, what, old, new)
EDITS = [
(r'Scripts\World\ZoneMeshes.cs', 'Spire (after Cone)',
r'''            return Build("Cone", v, t);
        }
''',
r'''            return Build("Cone", v, t);
        }
        /// <summary>
        /// A cone roof with UVs (a tower's slate cap), its base centred at the origin: each facet is unfolded flat, u along
        /// the eave and v up the slope, both in metres (<paramref name="tile"/> metres a tile), so the rows of slate lie level
        /// and hold their size. Flat-shaded facets and a closed underside, like Cone.
        /// </summary>
        public static Mesh Spire(float radius, float height, int sides = 10, float tile = 2.5f)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            var tip = Vector3.up * height; float edge = 2 * radius * Mathf.Sin(Mathf.PI / sides), inner = radius * Mathf.Cos(Mathf.PI / sides);
            float slant = Mathf.Sqrt(height * height + inner * inner);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                Tri(v, t, p1, p0, tip); uv.Add(new Vector2((i + 1) * edge, 0) / tile); uv.Add(new Vector2(i * edge, 0) / tile); uv.Add(new Vector2((i + .5f) * edge, slant) / tile);
                Tri(v, t, p0, p1, Vector3.zero); uv.Add(new Vector2(p0.x, p0.z) / tile); uv.Add(new Vector2(p1.x, p1.z) / tile); uv.Add(Vector2.zero);
            }
            var m = Build("Spire", v, t); m.SetUVs(0, uv); return m;
        }
'''),

(ZB, 'shared helpers (after BoxPart)',
r'''            return MeshPart(ZoneMeshes.Box(size, tile, localPos), parent, localPos, m, rot);
        }
''',
r'''            return MeshPart(ZoneMeshes.Box(size, tile, localPos), parent, localPos, m, rot);
        }
        /// <summary>
        /// Dressed stone in a zone's own tint: the painted masonry. Its texture is darker than the plain stone that built stone
        /// used to wear (a mean of about .63 against .83), so the tint is lifted by a little less than that and a wall keeps
        /// the colour it had, never brighter. Plain stone in the tint as given until the art has a masonry.
        /// </summary>
        Material Dressed(Color c) { return art.masonry != null ? Tint(art.masonry, new Color(c.r * 1.25f, c.g * 1.25f, c.b * 1.25f)) : Tint(art.stone, c); }
        /// <summary>One block of a piece of stonework (see Stonework): a box at <paramref name="at"/> in its prop, its texture
        /// measured in metres from the prop's own origin, so the courses run on from block to block.</summary>
        static CombineInstance Ashlar(Vector3 at, Vector3 size, float tile = 1.75f, Quaternion? rot = null) { return Piece(ZoneMeshes.Box(size, tile, at), at, rot); }
        /// <summary>Blocks joined into one mesh of one material (a wall with its merlons, a gate's span and corbels): one object
        /// for the lot. The blocks' own meshes are used up.</summary>
        GameObject Stonework(string name, Transform parent, Material m, params CombineInstance[] blocks)
        {
            var mesh = Joined(blocks); mesh.name = name;
            foreach (var b in blocks) DestroyImmediate(b.mesh);
            return MeshPart(mesh, parent, Vector3.zero, m);
        }
        /// <summary>A headstone: one slab with a rounded head, .6 wide, a metre tall and .15 thick, centred like a cube.</summary>
        Mesh Headstone { get { return PropMesh("Headstone", () => Cutout(new[] { new Vector2(-.3f, -.5f), new Vector2(-.3f, .26f), new Vector2(-.22f, .41f), new Vector2(-.09f, .5f), new Vector2(.09f, .5f), new Vector2(.22f, .41f), new Vector2(.3f, .26f), new Vector2(.3f, -.5f) }, .15f)); } }
'''),

(ZB, 'the grave prop',
r'''Part(PrimitiveType.Cube, t, new Vector3(0, p.variant == 1 ? .36f : .46f, 0), new Vector3(.6f, 1, .15f), art.stone, Quaternion.Euler(a, 0, b)); break; }''',
r'''MeshPart(Headstone, t, new Vector3(0, p.variant == 1 ? .36f : .46f, 0), RockTint(new Color(.52f, .51f, .48f)), Quaternion.Euler(a, 0, b)); break; }'''),

(ZB, 'CrackedHearth: the breast',
r'''            var stone = Tint(art.stone, new Color(.42f, .4f, .37f)); float face = w / 2 + 1.05f, z = .8f, low = H * .62f;
            var breast = Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .6f, low / 2, z), new Vector3(.9f, low, 2.2f), stone);
            breast.AddComponent<BoxCollider>(); breast.AddComponent<NavBlocker>();
            Part(PrimitiveType.Cube, t, new Vector3(w / 2 + .65f, (low + H + .4f) / 2, z), new Vector3(.8f, H + .4f - low, 1.2f), stone);
''',
r'''            var stone = Dressed(new Color(.42f, .4f, .37f)); float face = w / 2 + 1.05f, z = .8f, low = H * .62f;
            var breast = BoxPart(t, new Vector3(w / 2 + .6f, low / 2, z), new Vector3(.9f, low, 2.2f), stone, null, 1);
            var solid = breast.AddComponent<BoxCollider>(); solid.center = Vector3.zero; solid.size = new Vector3(.9f, low, 2.2f); breast.AddComponent<NavBlocker>();
            BoxPart(t, new Vector3(w / 2 + .65f, (low + H + .4f) / 2, z), new Vector3(.8f, H + .4f - low, 1.2f), stone, null, 1);
'''),

(ZB, 'CrackedHearth: the stack and the fallen stones',
r'''            Part(PrimitiveType.Cube, stack, new Vector3(0, tall / 2, 0), new Vector3(.8f, tall, 1.2f), stone);
            Part(PrimitiveType.Cube, stack, new Vector3(0, tall + .1f, 0), new Vector3(1, .2f, 1.4f), stone);
            if (art.particle != null) Smoke(stack, new Vector3(0, tall + .4f, 0));
            // Stones fallen from the crack.
            Part(PrimitiveType.Cube, t, new Vector3(face + .5f, .1f, z + .9f), new Vector3(.4f, .25f, .3f), stone, Quaternion.Euler(0, 30, 0));
            Part(PrimitiveType.Cube, t, new Vector3(face + .35f, .08f, z - .7f), new Vector3(.3f, .2f, .35f), stone, Quaternion.Euler(0, -20, 0));
''',
r'''            BoxPart(stack, new Vector3(0, tall / 2, 0), new Vector3(.8f, tall, 1.2f), stone, null, 1);
            BoxPart(stack, new Vector3(0, tall + .1f, 0), new Vector3(1, .2f, 1.4f), stone, null, 1);
            if (art.particle != null) Smoke(stack, new Vector3(0, tall + .4f, 0));
            // Stones fallen from the crack.
            BoxPart(t, new Vector3(face + .5f, .1f, z + .9f), new Vector3(.4f, .25f, .3f), stone, Quaternion.Euler(0, 30, 0));
            BoxPart(t, new Vector3(face + .35f, .08f, z - .7f), new Vector3(.3f, .2f, .35f), stone, Quaternion.Euler(0, -20, 0));
'''),

(ZB, 'Ruin',
r'''        void Ruin(Transform t, float length)
        {
            for (float x = -length / 2; x < length / 2; x += 1.1f)
            {
                float h = .6f + R01 * 2.4f, g = LocalGround(t, x, 0) - .15f;   // each block stands on (and a little into) the ground under it
                Part(PrimitiveType.Cube, t, new Vector3(x, g + (h + .15f) / 2, 0), new Vector3(1.05f, h + .15f, .8f), Tint(art.stone, new Color(.42f, .41f, .38f)), Quaternion.Euler(0, R01 * 6 - 3, R01 * 6 - 3));
            }
''',
r'''        void Ruin(Transform t, float length)
        {
            var stone = Dressed(new Color(.42f, .41f, .38f));
            for (float x = -length / 2; x < length / 2; x += 1.1f)
            {
                float h = .6f + R01 * 2.4f, g = LocalGround(t, x, 0) - .15f;   // each block stands on (and a little into) the ground under it
                BoxPart(t, new Vector3(x, g + (h + .15f) / 2, 0), new Vector3(1.05f, h + .15f, .8f), stone, Quaternion.Euler(0, R01 * 6 - 3, R01 * 6 - 3), 1.5f);
            }
'''),

(ZB, 'Wayshrine',
r'''            var stone = Tint(art.stone, new Color(.6f, .57f, .5f)); var dark = Tint(art.stone, new Color(.12f, .11f, .1f));
            Part(PrimitiveType.Cube, t, new Vector3(0, .02f, 0), new Vector3(2.2f, .5f, 2.2f), stone);   // deep enough to sit on a slope
            Part(PrimitiveType.Cube, t, new Vector3(0, .35f, .15f), new Vector3(1.5f, .22f, 1.5f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 1.23f, .25f), new Vector3(.5f, 1.6f, .5f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.4f, .25f), new Vector3(.9f, .8f, .75f), stone);
''',
r'''            var stone = Tint(art.stone, new Color(.6f, .57f, .5f)); var dark = Tint(art.stone, new Color(.12f, .11f, .1f));
            // Two steps (the lower deep enough to sit on a slope), the pillar, a cap course on it and the niche house: small dressed stone, one mesh.
            Stonework("Wayshrine stone", t, Dressed(new Color(.6f, .57f, .5f)), Ashlar(new Vector3(0, .02f, 0), new Vector3(2.2f, .5f, 2.2f), 1), Ashlar(new Vector3(0, .35f, .15f), new Vector3(1.5f, .22f, 1.5f), 1),
                Ashlar(new Vector3(0, 1.23f, .25f), new Vector3(.5f, 1.6f, .5f), 1), Ashlar(new Vector3(0, 1.95f, .25f), new Vector3(.66f, .12f, .66f), 1), Ashlar(new Vector3(0, 2.4f, .25f), new Vector3(.9f, .8f, .75f), 1));
'''),

(ZB, "RuinedHouse: the floor slab",
r'''            Part(PrimitiveType.Cube, t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), art.stone);
''',
r'''            BoxPart(t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), Dressed(new Color(.52f, .51f, .48f)), null, 1.5f);
'''),

(ZB, 'Wall',
r'''        /// <summary>Stone curtain wall along a polyline, with a walkway lip and crenellations.</summary>
        void Wall(Vector2[] pts, Transform parent)
        {
            if (pts == null || pts.Length < 2) return;
            var stone = Tint(art.stone, new Color(.46f, .45f, .42f));
''',
r'''        /// <summary>Stone curtain wall along a polyline: a plinth course at its foot, a coping under the battlements and
        /// crenellations, each run one mesh of dressed stone.</summary>
        void Wall(Vector2[] pts, Transform parent)
        {
            if (pts == null || pts.Length < 2) return;
            var stone = Dressed(new Color(.46f, .45f, .42f));
'''),
(ZB, "Wall: a run's stone",
r'''                Part(PrimitiveType.Cube, seg, new Vector3(0, (3.9f - sink) / 2, 0), new Vector3(1.2f, 3.9f + sink, len + .6f), stone);
                for (float z = -len / 2 + .5f; z < len / 2; z += 1.4f)
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, seg, new Vector3(s * .45f, 4.2f, z), new Vector3(.35f, .6f, .7f), stone);
''',
r'''                var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, (3.9f - sink) / 2, 0), new Vector3(1.2f, 3.9f + sink, len + .6f)),
                    Ashlar(new Vector3(0, (.55f - sink) / 2, 0), new Vector3(1.4f, .55f + sink, len + .7f)),   // the plinth course, as wide as the collider
                    Ashlar(new Vector3(0, 3.84f, 0), new Vector3(1.4f, .2f, len + .7f)) };                       // the coping the merlons stand on
                for (float z = -len / 2 + .5f; z < len / 2; z += 1.4f)
                    foreach (int s in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(s * .45f, 4.2f, z), new Vector3(.35f, .6f, .7f)));
                Stonework("Wall stone", seg, stone, blocks.ToArray());
'''),

(ZB, 'Tower',
r'''            float h = 7.5f; var stone = Tint(art.stone, new Color(.44f, .43f, .41f));
            float drop = FootDrop(t, diameter * .8f, diameter * .8f);   // on a slope the tower's foot reaches the lowest ground at its base
            Part(PrimitiveType.Cylinder, t, new Vector3(0, (h - drop) / 2, 0), new Vector3(diameter, (h + drop) / 2, diameter), stone);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 * Mathf.Deg2Rad;
                Part(PrimitiveType.Cube, t, new Vector3(Mathf.Cos(a) * diameter * .46f, h + .35f, Mathf.Sin(a) * diameter * .46f), new Vector3(.6f, .7f, .6f), stone, Quaternion.Euler(0, -i * 36, 0));
            }
            if (cone == null) cone = ZoneMeshes.Cone(1, 1);
            var roof = MeshPart(cone, t, new Vector3(0, h + .2f, 0), art.slate); roof.transform.localScale = new Vector3(diameter * .55f, 3.2f, diameter * .55f);
            Part(PrimitiveType.Cube, t, new Vector3(0, 4.5f, -diameter / 2 - .02f), new Vector3(.4f, .9f, .1f), art.glass);
''',
r'''            float h = 7.5f, r = diameter / 2; var stone = Dressed(new Color(.44f, .43f, .41f)); var slit = Tint(art.timber, new Color(.05f, .04f, .03f));
            float drop = FootDrop(t, diameter * .8f, diameter * .8f);   // on a slope the tower's foot reaches the lowest ground at its base
            // The drum, turned in one piece: a plinth course at the foot, the shaft (the old cylinder's girth), and a corbelled
            // ring under the battlements. The stone tiles in metres round it and up it.
            var drum = Turned(new[] { new Vector2(r + .14f, -drop), new Vector2(r + .14f, .5f), new Vector2(r, .68f), new Vector2(r, h - 1), new Vector2(r + .16f, h - .8f), new Vector2(r + .16f, h), new Vector2(0, h) }, 20, 1.75f);
            drum.name = "Tower drum"; MeshPart(drum, t, Vector3.zero, stone);
            var merlons = new CombineInstance[10];
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36 * Mathf.Deg2Rad;
                merlons[i] = Piece(ZoneMeshes.Box(new Vector3(.6f, .7f, .6f), 1.75f), new Vector3(Mathf.Cos(a) * diameter * .46f, h + .35f, Mathf.Sin(a) * diameter * .46f), Quaternion.Euler(0, -i * 36, 0));
            }
            Stonework("Tower battlements", t, stone, merlons);
            MeshPart(PropMesh("Tower roof " + Mathf.RoundToInt(diameter * 100), () => ZoneMeshes.Spire(diameter * .55f, 3.2f)), t, new Vector3(0, h + .2f, 0), art.slate);
            Part(PrimitiveType.Cube, t, new Vector3(0, 4.5f, -diameter / 2 - .02f), new Vector3(.4f, .9f, .1f), art.glass);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * r, 3.3f, 0), new Vector3(.1f, 1, .16f), slit);   // arrow slits to the sides and the back
            Part(PrimitiveType.Cube, t, new Vector3(0, 3.3f, r), new Vector3(.16f, 1, .1f), slit);
'''),

(ZB, 'Crypt: its stone',
r'''            var stone = Tint(art.stone, new Color(.36f, .36f, .37f));
            if (variant == 1)
''',
r'''            var stone = Dressed(new Color(.36f, .36f, .37f));
            if (variant == 1)
'''),
(ZB, "Crypt: the barrow's megaliths",
r'''                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.05f, e + 1.1f, -1.2f), new Vector3(.5f, 2.3f, 2.6f), stone);   // jambs run back into the mound
''',
r'''                stone = RockTint(new Color(.36f, .36f, .37f));   // a barrow is single great stones, not coursed work
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.05f, e + 1.1f, -1.2f), new Vector3(.5f, 2.3f, 2.6f), stone);   // jambs run back into the mound
'''),
(ZB, 'Crypt: the vault',
r'''                Part(PrimitiveType.Cube, t, new Vector3(0, 1.2f - drop / 2, 1.5f), new Vector3(6, 2.4f + drop, 6), stone);
                MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, 1, -1.52f), new Vector3(1.8f, 2, .1f), Tint(art.metal, new Color(.14f, .14f, .16f)));
                foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * 1.4f, 1.3f, -1.7f), new Vector3(.6f, 2.6f, .6f), stone);
                for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, t, new Vector3(0, .1f + i * .12f, -2.6f + i * .35f), new Vector3(2.6f, .2f, .4f), stone);
            }
            // Grave markers before it (the same five draws each, in the same order), set on the ground where they stand.
            for (int i = 0; i < 6; i++) { float gx = R01 * 10 - 5, gz = -4 - R01 * 4; Part(PrimitiveType.Cube, t, new Vector3(gx, LocalGround(t, gx, gz) + .46f, gz), new Vector3(.55f, 1, .15f), stone, Quaternion.Euler(R01 * 10 - 5, R01 * 30 - 15, R01 * 10 - 5)); }
''',
r'''                // The vault, a cornice under the eaves, the door's pillars and three steps: one mesh of dressed stone.
                var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, 1.2f - drop / 2, 1.5f), new Vector3(6, 2.4f + drop, 6)), Ashlar(new Vector3(0, 2.09f, 1.5f), new Vector3(6.3f, .22f, 6.3f)) };
                foreach (int s in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(s * 1.4f, 1.3f, -1.7f), new Vector3(.6f, 2.6f, .6f)));
                for (int i = 0; i < 3; i++) blocks.Add(Ashlar(new Vector3(0, .1f + i * .12f, -2.6f + i * .35f), new Vector3(2.6f, .2f, .4f)));
                Stonework("Crypt stone", t, stone, blocks.ToArray());
                MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));
                Part(PrimitiveType.Cube, t, new Vector3(0, 1, -1.52f), new Vector3(1.8f, 2, .1f), Tint(art.metal, new Color(.14f, .14f, .16f)));
            }
            // Grave markers before it (the same five draws each, in the same order), set on the ground where they stand: headstones, each one slab.
            var slab = RockTint(new Color(.36f, .36f, .37f));
            for (int i = 0; i < 6; i++) { float gx = R01 * 10 - 5, gz = -4 - R01 * 4; MeshPart(Headstone, t, new Vector3(gx, LocalGround(t, gx, gz) + .46f, gz), slab, Quaternion.Euler(R01 * 10 - 5, R01 * 30 - 15, R01 * 10 - 5)); }
'''),

(ZB, 'the exit waystone',
r'''                Part(PrimitiveType.Cube, t, new Vector3(0, 1.1f, 0), new Vector3(.8f, 2.2f, .5f), Tint(art.stone, new Color(.5f, .48f, .44f)), Quaternion.Euler(0, 15, 0));
''',
r'''                MeshPart(PropMesh("Waystone", () => Cutout(new[] { new Vector2(-.4f, -1.1f), new Vector2(-.4f, .78f), new Vector2(-.24f, 1.1f), new Vector2(.24f, 1.1f), new Vector2(.4f, .78f), new Vector2(.4f, -1.1f) }, .5f)), t, new Vector3(0, 1.1f, 0), RockTint(new Color(.5f, .48f, .44f)), Quaternion.Euler(0, 15, 0));   // one standing stone, its shoulders chamfered
'''),

(ZB, "Forge: the back wall and the hearth",
r'''            Part(PrimitiveType.Cube, t, new Vector3(0, .7f, 1.95f), new Vector3(5, 1.4f, .3f), art.stone);                        // back wall
            // Hearth, coals, hood and chimney.
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, .5f, 1.1f), new Vector3(1.8f, 1, 1.3f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.02f, 1.05f), new Vector3(1.3f, .08f, .9f), art.glass);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 2.1f, 1.35f), new Vector3(1.3f, .9f, .9f), art.stone);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 3.4f, 1.5f), new Vector3(.55f, 1.8f, .55f), art.stone);
''',
r'''            BoxPart(t, new Vector3(0, .7f, 1.95f), new Vector3(5, 1.4f, .3f), Masonry, null, 1);                                  // back wall
            // Hearth, coals, hood and chimney.
            BoxPart(t, new Vector3(1.2f, .5f, 1.1f), new Vector3(1.8f, 1, 1.3f), Masonry, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.02f, 1.05f), new Vector3(1.3f, .08f, .9f), art.glass);
            BoxPart(t, new Vector3(1.2f, 2.1f, 1.35f), new Vector3(1.3f, .9f, .9f), Masonry, null, 1);
            BoxPart(t, new Vector3(1.2f, 3.4f, 1.5f), new Vector3(.55f, 1.8f, .55f), Masonry, null, 1);
'''),

(ZB, "Oven: the plinth",
r'''            Part(PrimitiveType.Cube, t, new Vector3(0, .45f, .3f), new Vector3(2.2f, .9f, 2.2f), art.stone);
''',
r'''            BoxPart(t, new Vector3(0, .45f, .3f), new Vector3(2.2f, .9f, 2.2f), Masonry, null, 1);
'''),

(ZB, 'Gate: span, corbels and merlons',
r'''            var stone = Tint(art.stone, new Color(.44f, .43f, .41f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f)); var planks = Tint(art.timber, new Color(.3f, .21f, .14f));
            var sand = Tint(art.cloth, new Color(.78f, .63f, .38f)); var rust = Tint(art.cloth, new Color(.5f, .15f, .1f));
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .7f, 0), new Vector3(width + 3, 1.4f, 2.6f), stone);   // the span, reaching into both towers
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * half, top, 0), new Vector3(1.2f, 1.2f, 2.5f), stone, Quaternion.Euler(0, 0, 45));   // corbels round the arch
            for (int i = 0; i <= 6; i++) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(-half + i * step, top + 1.75f, sz * 1.05f), new Vector3(.6f, .7f, .45f), stone);
''',
r'''            var stone = Dressed(new Color(.44f, .43f, .41f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f)); var planks = Tint(art.timber, new Color(.3f, .21f, .14f));
            var sand = Tint(art.cloth, new Color(.78f, .63f, .38f)); var rust = Tint(art.cloth, new Color(.5f, .15f, .1f));
            // One mesh of dressed stone: the span, reaching into both towers, a string course under its battlements, two stepped
            // corbels a side round the arch (level courses, where two cubes stood on edge), and the merlons.
            var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, top + .7f, 0), new Vector3(width + 3, 1.4f, 2.6f)), Ashlar(new Vector3(0, top + 1.32f, 0), new Vector3(width + 3, .2f, 2.84f)) };
            foreach (int s in new[] { -1, 1 }) { blocks.Add(Ashlar(new Vector3(s * (half - .05f), top - .16f, 0), new Vector3(1.3f, .36f, 2.5f))); blocks.Add(Ashlar(new Vector3(s * (half + .2f), top - .5f, 0), new Vector3(.8f, .36f, 2.5f))); }   // their outer ends run into the towers
            for (int i = 0; i <= 6; i++) foreach (int sz in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(-half + i * step, top + 1.75f, sz * 1.05f), new Vector3(.6f, .7f, .45f)));
            Stonework("Gate stone", t, stone, blocks.ToArray());
'''),
(ZB, "Gate: the boom's counterweight",
r'''            Part(PrimitiveType.Cube, t, pivot + boom * new Vector3(-.75f, 0, 0), new Vector3(.5f, .45f, .4f), stone, boom);
''',
r'''            Part(PrimitiveType.Cube, t, pivot + boom * new Vector3(-.75f, 0, 0), new Vector3(.5f, .45f, .4f), RockTint(new Color(.44f, .43f, .41f)), boom);   // one rough stone
'''),

(ZB, 'Stronghold: footing, body and battlements',
r'''            var stone = Tint(art.stone, new Color(.47f, .45f, .42f)); var dark = Tint(art.timber, new Color(.2f, .14f, .1f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var sand = Tint(art.cloth, new Color(.78f, .63f, .38f)); var rust = Tint(art.cloth, new Color(.5f, .15f, .1f)); var slit = Tint(art.timber, new Color(.05f, .04f, .03f));
            Part(PrimitiveType.Cube, t, new Vector3(0, -.75f, 0), new Vector3(w + .7f, 1.7f, d + .7f), Tint(art.stone, new Color(.4f, .39f, .37f)));   // footing: a low plinth, deep where the perch falls away
            Part(PrimitiveType.Cube, t, new Vector3(0, h / 2, 0), new Vector3(w, h, d), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, h + .1f, 0), new Vector3(w + .3f, .2f, d + .3f), stone);   // string course
            int nx = Mathf.Max(2, Mathf.RoundToInt(w / 1.15f)), nz = Mathf.Max(2, Mathf.RoundToInt(d / 1.15f));
            for (int i = 0; i < nx; i++) foreach (int sz in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(Mathf.Lerp(-w / 2 + .3f, w / 2 - .3f, i / (nx - 1f)), h + .6f, sz * (d / 2 - .1f)), new Vector3(.6f, .8f, .45f), stone);
            for (int i = 1; i < nz - 1; i++) foreach (int sx in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx * (w / 2 - .1f), h + .6f, Mathf.Lerp(-d / 2 + .3f, d / 2 - .3f, i / (nz - 1f))), new Vector3(.45f, .8f, .6f), stone);
''',
r'''            var stone = Dressed(new Color(.47f, .45f, .42f)); var dark = Tint(art.timber, new Color(.2f, .14f, .1f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var sand = Tint(art.cloth, new Color(.78f, .63f, .38f)); var rust = Tint(art.cloth, new Color(.5f, .15f, .1f)); var slit = Tint(art.timber, new Color(.05f, .04f, .03f));
            BoxPart(t, new Vector3(0, -.75f, 0), new Vector3(w + .7f, 1.7f, d + .7f), Dressed(new Color(.4f, .39f, .37f)), null, 1.75f);   // footing: a low plinth, deep where the perch falls away
            // The hall's stone is one mesh: the body, the string course, the merlons, and the door's surround and step below.
            var blocks = new List<CombineInstance> { Ashlar(new Vector3(0, h / 2, 0), new Vector3(w, h, d)), Ashlar(new Vector3(0, h + .1f, 0), new Vector3(w + .3f, .2f, d + .3f)) };
            int nx = Mathf.Max(2, Mathf.RoundToInt(w / 1.15f)), nz = Mathf.Max(2, Mathf.RoundToInt(d / 1.15f));
            for (int i = 0; i < nx; i++) foreach (int sz in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(Mathf.Lerp(-w / 2 + .3f, w / 2 - .3f, i / (nx - 1f)), h + .6f, sz * (d / 2 - .1f)), new Vector3(.6f, .8f, .45f)));
            for (int i = 1; i < nz - 1; i++) foreach (int sx in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(sx * (w / 2 - .1f), h + .6f, Mathf.Lerp(-d / 2 + .3f, d / 2 - .3f, i / (nz - 1f))), new Vector3(.45f, .8f, .6f)));
'''),
(ZB, "Stronghold: the door's surround",
r'''            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(s * .95f, 1.3f, -d / 2 - .1f), new Vector3(.4f, 2.6f, .2f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.7f, -d / 2 - .1f), new Vector3(2.3f, .4f, .2f), stone);
            Part(PrimitiveType.Cube, t, new Vector3(0, .1f, -d / 2 - .45f), new Vector3(2.2f, .2f, .7f), stone);
''',
r'''            foreach (int s in new[] { -1, 1 }) blocks.Add(Ashlar(new Vector3(s * .95f, 1.3f, -d / 2 - .1f), new Vector3(.4f, 2.6f, .2f), 1));
            blocks.Add(Ashlar(new Vector3(0, 2.7f, -d / 2 - .1f), new Vector3(2.3f, .4f, .2f), 1));
            blocks.Add(Ashlar(new Vector3(0, .1f, -d / 2 - .45f), new Vector3(2.2f, .2f, .7f), 1));
            Stonework("Keep stone", t, stone, blocks.ToArray());
'''),

(ZB, 'Shrine: dais step, altar-stone and stele',
r'''            Part(PrimitiveType.Cube, t, new Vector3(0, .1f, .35f), new Vector3(3.2f, .16f, 2.6f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f, 0), new Vector3(2, .9f, 1.1f), dark);   // the altar-stone
''',
r'''            BoxPart(t, new Vector3(0, .1f, .35f), new Vector3(3.2f, .16f, 2.6f), Dressed(new Color(.25f, .24f, .24f)), null, 1.5f);   // laid stone
            dark = RockTint(new Color(.25f, .24f, .24f));   // the altar and the stele are each one stone
            Part(PrimitiveType.Cube, t, new Vector3(0, .6f, 0), new Vector3(2, .9f, 1.1f), dark);   // the altar-stone
'''),

(r'Scripts\World\ZoneBuilder.Verdant.cs', "Treehouse: its stone",
r'''var tile = Tint(art.slate, new Color(.62f, .3f, .19f)); var stone = Tint(art.stone, new Color(.5f, .48f, .44f));
''',
r'''var tile = Tint(art.slate, new Color(.62f, .3f, .19f)); var stone = Dressed(new Color(.5f, .48f, .44f));
'''),
(r'Scripts\World\ZoneBuilder.Verdant.cs', "Treehouse: the round house's footing",
r'''            Part(PrimitiveType.Cylinder, t, house + new Vector3(0, floor + .25f, 0), new Vector3(hr * 2 + .3f, .3f, hr * 2 + .3f), stone);   // the footing
''',
r'''            MeshPart(PropMesh("Round footing", () => Turned(new[] { new Vector2(hr + .15f, 0), new Vector2(hr + .15f, .6f), new Vector2(0, .6f) }, 20, 1.5f)), t, house + new Vector3(0, floor - .05f, 0), stone);   // the footing
'''),
(r'Scripts\World\ZoneBuilder.Verdant.cs', "Treehouse: the step",
r'''            Part(PrimitiveType.Cube, t, house + new Vector3(0, floor + .5f, -hr - .8f), new Vector3(1.8f, .25f, .9f), stone);
''',
r'''            BoxPart(t, house + new Vector3(0, floor + .5f, -hr - .8f), new Vector3(1.8f, .25f, .9f), stone, null, 1);
'''),
]

def main():
    files = {}
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path)
        if full not in files:
            if not os.path.isfile(full): sys.exit('patch_p5: NOTHING WRITTEN.\n  missing file: ' + full)
            files[full] = io.open(full, 'r', encoding='utf-8', newline='').read()
    out = dict(files); problems = []
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path); text = files[full]
        if '\r\n' in text: old, new = old.replace('\n', '\r\n'), new.replace('\n', '\r\n')   # the file's own line ends
        n = text.count(old)
        if n != 1: problems.append('%s: %s: the old block occurs %d times, not once' % (path, what, n)); continue
        if out[full].count(old) != 1: problems.append('%s: %s: overlaps another edit' % (path, what)); continue
        out[full] = out[full].replace(old, new)
    if problems:
        sys.exit('patch_p5: NOTHING WRITTEN.\n  ' + '\n  '.join(problems))
    for full, text in out.items():
        io.open(full, 'w', encoding='utf-8', newline='').write(text)
        print('patched %s (%d edits)' % (full, sum(1 for e in EDITS if os.path.join(ROOT, e[0]) == full)))
    print('patch_p5: OK')

if __name__ == '__main__':
    main()
