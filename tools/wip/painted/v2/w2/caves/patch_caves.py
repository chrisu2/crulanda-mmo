#!/usr/bin/env python
"""Worklist item 14 (visual_review.md): caves read as painted rock and as earth packed with roots, and the two broken
Root-Mother's Deep capture shots stand inside the cave.

  patch_caves.py "<root>"      (default: D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda)

New files (installed beside this script's copies, each with a .meta):
  World\\Shaders\\PaintedCave.shader              Crulanda/PaintedCave: PaintedRock's world-projected paint under vertex shading
  Editor\\ZoneSceneBuilder.PaintedCave.cs         EarthPixel (packed earth with roots) and EnsurePaintedCave (art.cave, art.caveEarth)
Edits:
  Scripts\\World\\ZoneArt.cs                      the two materials
  Editor\\ZoneSceneBuilder.cs                     the art build calls EnsurePaintedCave after EnsurePaintedRock
  Scripts\\World\\ZoneBuilder.cs                  Cavern: the faceted shell stays the collider and the navmesh obstacle (its
                                                 renderer is switched off when there is cave art); CaveLining draws the walls
                                                 (smooth, scalloped, vertex-shaded, in stretches of ten rings); CaveDressing sets
                                                 rock, dripstone, roots and earth against them from its own stream; the Echoing
                                                 Hall's cones become dripstone (Drip) in the wall's own paint
  Scripts\\Encounter\\EncounterCapture.cs         a cave whose floor falls from the mouth itself (the deep's root-stair) takes its
                                                 first picture in its first chamber and its drop past that chamber

Anchor based: every old block must occur exactly once, all checks run before any write, and a second run fails loudly
without writing (the old blocks are gone, the new names are found and the new files are there).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
BUILDER = r'Scripts\World\ZoneBuilder.cs'
ART = r'Scripts\World\ZoneArt.cs'
SCENE = r'Editor\ZoneSceneBuilder.cs'
CAPTURE = r'Scripts\Encounter\EncounterCapture.cs'

SHADER_META = '''fileFormatVersion: 2
guid: feb8dca8cc874adabea5373ad060220d
ShaderImporter:
  externalObjects: {}
  defaultTextures: []
  nonModifiableTextures: []
  userData:
  assetBundleName:
  assetBundleVariant:
'''
SCRIPT_META = '''fileFormatVersion: 2
guid: 28b6ffe7745e4900973b43805e04bf7a
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'''
# (source beside this script, destination under the root, its .meta, a sibling whose .meta the format was copied from)
NEW = [('PaintedCave.shader', r'World\Shaders\PaintedCave.shader', SHADER_META, r'World\Shaders\PaintedRock.shader.meta'),
       ('ZoneSceneBuilder.PaintedCave.cs', r'Editor\ZoneSceneBuilder.PaintedCave.cs', SCRIPT_META, r'Editor\ZoneSceneBuilder.PaintedRock.cs.meta')]

EDITS = []   # (file, old, new)
def edit(f, old, new): EDITS.append((f, old, new))

# ---------------------------------------------------------------- ZoneArt: the two cave materials
edit(ART,
'''        public Material rock;
''',
'''        public Material rock;
        [Tooltip("Crulanda/PaintedCave: a cave's walls from inside (ZoneBuilder.CaveLining), world-projected paint under the lining's own vertex shading. cave is bedded rock (Crowsfoot Hollow), caveEarth packed earth with roots (the Root-Mother's Deep). Null = the faceted shell shows, on stone or bark.")]
        public Material cave, caveEarth;
''')

# ---------------------------------------------------------------- the art build
edit(SCENE,
'''            EnsurePaintedRock(art);   // and the painted natural rock (ZoneSceneBuilder.PaintedRock.cs)
''',
'''            EnsurePaintedRock(art);   // and the painted natural rock (ZoneSceneBuilder.PaintedRock.cs)
            EnsurePaintedCave(art);   // and the cave walls, rock and earth (ZoneSceneBuilder.PaintedCave.cs)
''')

# ---------------------------------------------------------------- Cavern: the lining and the dressing
edit(BUILDER,
'''            body.AddComponent<MeshCollider>().sharedMesh = shell; body.AddComponent<NavWalkable>();   // walls and roof: in the navmesh as obstacles
''',
'''            body.AddComponent<MeshCollider>().sharedMesh = shell; body.AddComponent<NavWalkable>();   // walls and roof: in the navmesh as obstacles
            // What shows inside is the painted lining over that shell (the shell stays the collider and the navmesh's obstacle, not
            // drawn), and against it the rock, dripstone, root and earth that break the walls up. With no cave art the shell shows.
            var paint = roots ? art.caveEarth : art.cave;
            var lining = paint != null ? Tint(paint, roots ? new Color(.5f, .4f, .27f) : new Color(.56f, .51f, .45f)) : null;
            if (lining != null) { body.GetComponent<MeshRenderer>().enabled = false; CaveLining(t, h, c, ring, n, P, seed, lining); }
            CaveDressing(t, h, c, right, ring, n, P, roots, lining ?? (roots ? knoll : rock));
''')

# ---------------------------------------------------------------- the Echoing Hall: dripstone, not cones (the same draws)
edit(BUILDER,
'''                var stone = Tint(art.stone, new Color(.4f, .37f, .34f));
                if (cone == null) cone = ZoneMeshes.Cone(1, 1);
                for (int i = 0; i < n; i++)
''',
'''                var stone = lining ?? Tint(art.stone, new Color(.4f, .37f, .34f));   // dripstone in the wall's own paint
                for (int i = 0; i < n; i++)
''')
edit(BUILDER, 'MeshPart(cone, t, baseAt, stone)', 'MeshPart(Drip(i), t, baseAt, stone)')
edit(BUILDER, 'MeshPart(cone, t, hang, stone, ', 'MeshPart(Drip(i + 1), t, hang, stone, ')

# ---------------------------------------------------------------- CaveLining, CaveDressing, Drip (before Torch)
edit(BUILDER,
'''        /// <summary>A pitch torch in a wall: a short stick leaning out of the rock, its glowing head and flame, a flickering light.</summary>
''',
'''        /// <summary>
        /// A cave's painted lining: what shows from inside, laid over the faceted shell (which stays the collider). The shell's own
        /// rings, with one more vertex between each two round the arch (curved through its neighbours and sunk a little into the
        /// rock, so the wall is scalloped where the shell is flat), smooth normals, and its shading painted into the vertices: dark
        /// at the wall's foot and in the hollows, lighter on what stands proud, in warm and cool patches. Straight at the mouth,
        /// where it meets the knoll's cut face. In stretches of ten rings, so each takes the lights near it; it casts shadow both
        /// ways, as the shell did (no sun reaches the floor). No colliders; draws from no stream.
        /// </summary>
        void CaveLining(Transform t, Hollow h, Vector3[] c, Vector3[,] ring, int n, int P, float seed, Material m)
        {
            int W = 2 * P - 1; var g = new Vector3[n, W]; var nrm = new Vector3[n, W]; var col = new Color[n, W];
            for (int i = 0; i < n; i++)
            {
                float s = h.Along[i], inside = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 2.6f, s));
                for (int k = 0; k < W; k++)
                {
                    int a = k / 2; if (k % 2 == 0) { g[i, k] = ring[i, a]; continue; }
                    var p = (ring[i, a] + ring[i, a + 1]) / 2;
                    if (a >= 1 && a + 1 <= P - 2)   // not the two feet under the floor
                    {
                        var curved = (9 * (ring[i, a] + ring[i, a + 1]) - ring[i, Mathf.Max(1, a - 1)] - ring[i, Mathf.Min(P - 2, a + 2)]) / 16;
                        var into = (p - (c[i] + Vector3.up * h.Height[i] * .35f)).normalized;
                        p = Vector3.Lerp(p, curved, inside) + into * (.03f + .2f * Mathf.PerlinNoise(s * .5f + seed + 7, p.y * 1.3f + k * .37f)) * inside;
                    }
                    g[i, k] = p;
                }
            }
            for (int i = 0; i < n; i++)
                for (int k = 0; k < W; k++)
                {
                    int i0 = Mathf.Max(0, i - 1), i1 = Mathf.Min(n - 1, i + 1), k0 = Mathf.Max(0, k - 1), k1 = Mathf.Min(W - 1, k + 1);
                    var p = g[i, k]; var face = Vector3.Cross(g[i1, k] - g[i0, k], g[i, k1] - g[i, k0]).normalized;
                    if (Vector3.Dot(face, c[i] + Vector3.up * h.Height[i] * .35f - p) < 0) face = -face;   // toward the air
                    nrm[i, k] = face;
                    float hollow = Vector3.Dot((g[i0, k] + g[i1, k] + g[i, k0] + g[i, k1]) / 4 - p, face);   // how far its neighbours stand proud of it
                    float shade = Mathf.Lerp(.56f, 1, Mathf.SmoothStep(0, 1, (p.y - c[i].y) / Mathf.Max(.5f, h.Height[i]) / .4f)) * Mathf.Clamp(.86f - hollow * 2.4f, .55f, 1);
                    float patch = Mathf.PerlinNoise(p.x * .19f + seed, p.z * .19f + p.y * .31f) - .5f;
                    col[i, k] = new Color(Mathf.Min(1, shade * (1 + patch * .14f)), Mathf.Min(1, shade * (1 + patch * .03f)), Mathf.Min(1, shade * (1 - patch * .13f)), 1);
                }
            bool turned = Vector3.Dot(Vector3.Cross(g[n / 2 + 1, W / 2] - g[n / 2, W / 2], g[n / 2, W / 2 + 1] - g[n / 2, W / 2]), nrm[n / 2, W / 2]) < 0;   // wound to face the air, whichever way the rings run
            for (int from = 0; from + 1 < n; from += 10)
            {
                int rows = Mathf.Min(n - 1, from + 10) - from + 1;
                var v = new List<Vector3>(rows * W); var vn = new List<Vector3>(rows * W); var vc = new List<Color>(rows * W); var tri = new List<int>((rows - 1) * (W - 1) * 6);
                for (int i = from; i < from + rows; i++) for (int k = 0; k < W; k++) { v.Add(g[i, k]); vn.Add(nrm[i, k]); vc.Add(col[i, k]); }
                for (int i = 0; i + 1 < rows; i++)
                    for (int k = 0; k + 1 < W; k++)
                    {
                        int a = i * W + k, b = a + W, d = a + 1, e = b + 1;
                        tri.AddRange(turned ? new[] { a, d, b, d, e, b } : new[] { a, b, d, d, b, e });
                    }
                var mesh = new Mesh { name = h.Name + " lining" }; mesh.SetVertices(v); mesh.SetNormals(vn); mesh.SetColors(vc); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
                MeshPart(mesh, t, Vector3.zero, m).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }
        }
        Mesh[] drips;
        /// <summary>Dripstone, its foot at the origin, a unit tall and about a unit in radius at the foot (a stalagmite as it
        /// stands, a stalactite turned over): a lumpy taper to a point, a little off plumb, one of three. Open at the foot, which
        /// sits in the rock.</summary>
        Mesh Drip(int i)
        {
            if (drips == null)
            {
                drips = new Mesh[3];
                for (int k = 0; k < 3; k++)
                {
                    float o = k * 1.7f;
                    drips[k] = ZoneMeshes.Tube(y => new Vector3(Mathf.Sin(y * 2.2f + o) * .07f * y, y, Mathf.Cos(y * 1.7f + o * 2) * .07f * y),
                        (y, a) => (Mathf.Pow(Mathf.Max(0, 1 - y), 1.25f) * (.8f + .2f * Mathf.Sin(a * 2 + o + y * 3)) + .35f * Mathf.Exp(-Mathf.Max(0, y) * 9)) * (1 + .1f * Mathf.Sin(y * 17 + o)) * Mathf.Clamp01((1 - y) / .04f),
                        new[] { 0, .05f, .14f, .28f, .44f, .6f, .76f, .9f, 1 }, 8, Vector3.right, 1, 1);
                    drips[k].name = "Dripstone";
                }
            }
            return drips[((i % 3) + 3) % 3];
        }
        /// <summary>
        /// What breaks a cave's walls up, for the eye only (no colliders, nothing in the navmesh, kept to the walls and the roof).
        /// In rock: fallen stone heaped at the wall's foot, ledges standing proud between knee and shoulder, buttresses up the
        /// wall (none where a torch stands), and dripstone hanging in clusters from the higher roofs, clear of a head. In the
        /// Root-Mother's Deep: roots as thick as an arm arched over the passage from floor to floor, runners along the walls and
        /// rootlets hanging from the roof (joined into one mesh a stretch), earth slumped at the wall's foot, and a few beads of
        /// amber sap, each with a small warm light of its own against the green. Its own stream (TreeRandom a little off the
        /// mouth): the zone's, the cave's and the deep's are not touched.
        /// </summary>
        void CaveDressing(Transform t, Hollow h, Vector3[] c, Vector3[] right, Vector3[,] ring, int n, int P, bool roots, Material wall)
        {
            var cr = TreeRandom(t.position + new Vector3(7.5f, 0, 3.25f)); float C() { return (float)cr.NextDouble(); }
            int RingAt(float s) { int i = 0; while (i + 1 < n && h.Along[i + 1] <= s) i++; return i; }
            Vector3 Inward(int i, int k) { var d = c[i] - ring[i, k]; d.y = 0; return d.sqrMagnitude > .01f ? d.normalized : Vector3.zero; }
            Vector3 ToAir(int i, int k) { return (c[i] + Vector3.up * h.Height[i] * .4f - ring[i, k]).normalized; }
            float Yaw(int i) { return Mathf.Atan2(-right[i].z, right[i].x) * Mathf.Rad2Deg; }   // the way the passage runs
            if (!roots)
            {
                for (float s = 2.4f; s < h.Length - 3; s += 1.7f)
                {
                    int i = RingAt(s), side = C() < .5f ? 1 : -1, foot = side > 0 ? 1 : P - 2; float pick = C(), size = C(), turn = C(), lift = C();
                    bool torch = Mathf.Abs(Mathf.Repeat(s - 3.5f + 3.3f, 6.6f) - 3.3f) < 1.3f;   // a torch stands in the wall about here (Cavern sets one every 6.6 m)
                    if (pick < .4f)
                    {   // fallen stone at the wall's foot, a big one and a small one beside it
                        var at = ring[i, foot] - Inward(i, foot) * .1f; at.y = c[i].y + .1f;
                        Lump(BoulderAt((int)(turn * 6)), t, at, new Vector3(1 + size * .9f, .6f + size * .7f, .9f + lift * .7f), wall, turn * 360);
                        Lump(BoulderAt((int)(lift * 6)), t, at + Inward(i, foot) * .3f + new Vector3(-right[i].z, 0, right[i].x) * (.75f + size * .35f), Vector3.one * (.35f + lift * .3f), wall, lift * 360);
                    }
                    else if (pick < .76f && !torch)
                    {   // a ledge: a bed of the rock standing proud of the wall, long the way the passage runs
                        int k = side > 0 ? 2 + (int)(lift * 2) : P - 3 - (int)(lift * 2);
                        Lump(CragRock((int)(turn * 6)), t, ring[i, k] - Inward(i, k) * .3f, new Vector3(1.3f + size * .4f, .5f + lift * .4f, 1.8f + size * 1.6f), wall, Yaw(i) + (turn - .5f) * 16);
                    }
                    else if (!torch && h.Height[i] > 3)
                    {   // a buttress up the wall, lost in it higher up where the wall leans in
                        var at = ring[i, foot] - Inward(i, foot) * .4f; at.y = c[i].y;
                        Lump(CragRock((int)(turn * 6)), t, at, new Vector3(1.1f + size * .6f, h.Height[i] * (1 + lift * .5f), 1.2f + size * .8f), wall, Yaw(i) + (turn - .5f) * 30);
                    }
                }
                for (float s = 5; s < h.Length - 3; s += 2.3f)
                {
                    int i = RingAt(s), k = P / 2 + (int)((C() - .5f) * 5), count = 2 + (int)(C() * 3); float go = C();
                    if (h.Height[i] < 3.7f || go < .4f) continue;   // a low roof stays bare
                    for (int d = 0; d < count; d++)
                    {
                        float len = .45f + C() * 1.2f, r = .12f + C() * .12f, yaw = C() * 360; var at = ring[i, k] + new Vector3((C() - .5f) * .8f, .25f, (C() - .5f) * .8f);
                        len = Mathf.Min(len, at.y - c[i].y - 2.5f); if (len < .35f) continue;
                        MeshPart(Drip(i + d), t, at, wall, Quaternion.Euler(180, yaw, 0)).transform.localScale = new Vector3(r + len * .08f, len, r + len * .08f);
                    }
                }
                return;
            }
            // The deep. Roots are lofted as a limb is (ZoneBuilder.Limb) and joined, one mesh of thick root and one of rootlets to
            // every nine metres of passage.
            int stretches = (int)(h.Length / 9) + 1; var thick = new List<CombineInstance>[stretches]; var thin = new List<CombineInstance>[stretches];
            for (int b = 0; b < stretches; b++) { thick[b] = new List<CombineInstance>(); thin[b] = new List<CombineInstance>(); }
            void Strand(List<CombineInstance> into, Vector3 from, Vector3 to, float r0, float tip, float bow, int sides)
            {
                var d = to - from; float len = d.magnitude; if (len < .05f) return; var dir = d / len;
                var side = Vector3.Cross(dir, Vector3.up); if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                var sag = Vector3.Cross(side.normalized, dir) * len * bow; if (sag.y > 0) sag = -sag;   // bowed down and out from the wall
                Vector3 Mid(float u) { return from + d * u + sag * (4 * u * (1 - u)); }
                float R(float u, float a) { return Mathf.Lerp(r0, tip, u) * (1 + .35f * Mathf.Exp(-u * 9)) * Mathf.Clamp01((1.05f - u) / .1f); }
                into.Add(Piece(ZoneMeshes.Tube(Mid, R, new[] { 0, .12f, .26f, .42f, .58f, .74f, .88f, .97f, 1.05f }, sides, side.normalized, 1, len * .5f)));
            }
            Vector3 OnWall(int i, int k, float r) { return ring[i, k] + ToAir(i, k) * (.04f + r * .5f); }   // half sunk in the earth
            // Arched over the passage from the foot of one wall to the foot of the other, wandering a ring or two as they climb.
            for (float s = 3; s < h.Length - 6; s += 5 + C() * 3.5f)
            {
                int i = RingAt(s), k = 1; float r = .13f + C() * .12f; var from = OnWall(i, 1, r) + Vector3.down * .3f;
                while (k < P - 2)
                {
                    int k2 = Mathf.Min(P - 2, k + 2), j = Mathf.Clamp(i + (int)((C() - .5f) * 4), 1, n - 2);
                    var to = OnWall(j, k2, r); if (k2 == P - 2) to += Vector3.down * .3f;
                    Strand(thick[(int)(s / 9)], from, to, r * (.85f + C() * .3f), r * .8f, .07f, 7);
                    from = to; k = k2;
                }
            }
            // Runners along the walls, two to four lengths each, thinning as they go.
            for (float s = 1.5f; s < h.Length - 5; s += 1.6f + C() * 1.6f)
            {
                int i = RingAt(s), side = C() < .5f ? 1 : -1, k = side > 0 ? 2 + (int)(C() * 5) : P - 3 - (int)(C() * 5), runs = 2 + (int)(C() * 3); float r = .06f + C() * .1f;
                var from = OnWall(i, k, r) - ToAir(i, k) * .15f;   // out of the earth
                for (int q = 0; q < runs; q++)
                {
                    i += 2 + (int)(C() * 2); k = Mathf.Clamp(k + (int)(C() * 3) - 1, 2, P - 3); if (i > n - 3) break;
                    var to = OnWall(i, k, r); Strand(thick[(int)(s / 9)], from, to, r, r * .75f, .05f, 6);
                    from = to; r *= .8f;
                }
            }
            // Rootlets hanging from the roof in bunches, none lower than a head.
            for (float s = 2; s < h.Length - 4; s += 1.3f + C() * 1.5f)
            {
                int i = RingAt(s), k = P / 2 + (int)((C() - .5f) * 5), count = 3 + (int)(C() * 4);
                for (int q = 0; q < count; q++)
                {
                    var top = ring[i, k] + new Vector3((C() - .5f) * .6f, .2f, (C() - .5f) * .6f); var sway = new Vector3((C() - .5f) * .3f, 0, (C() - .5f) * .3f);
                    float len = Mathf.Min(.35f + C() * 1.3f, top.y - c[i].y - 2.2f), r = .035f + C() * .03f;
                    if (len >= .3f) Strand(thin[(int)(s / 9)], top, top + Vector3.down * len + sway, r, .012f, .06f, 5);
                }
            }
            var bark = Tint(art.bark, new Color(.36f, .29f, .2f)); var pale = Tint(art.bark, new Color(.5f, .43f, .32f));
            for (int b = 0; b < stretches; b++)
            {
                if (thick[b].Count > 0) Stonework("Roots", t, bark, thick[b].ToArray());
                if (thin[b].Count > 0) Stonework("Rootlets", t, pale, thin[b].ToArray());
            }
            // Earth slumped at the wall's foot (not into the Sap Well's pool, which lies against the right wall).
            int well = -1; for (int i = 0; i < n; i++) if (h.Along[i] >= h.Length * .45f && h.Along[i] <= h.Length * .7f && (well < 0 || h.Half[i] > h.Half[well])) well = i;
            for (float s = 2.5f; s < h.Length - 4; s += 2.2f + C() * 2)
            {
                int i = RingAt(s), side = C() < .5f ? 1 : -1, foot = side > 0 ? 1 : P - 2; float size = C(), turn = C();
                if (well >= 0 && side > 0 && Mathf.Abs(s - h.Along[well]) < 3.4f) continue;
                var at = ring[i, foot] - Inward(i, foot) * .15f; at.y = c[i].y + .08f;
                Lump(BoulderAt((int)(turn * 6)), t, at, new Vector3(1 + size * .9f, .6f + size * .7f, 1.1f + turn * 1.1f), wall, turn * 360);
            }
            // Amber sap welling from the wall every fifteen metres, on alternate sides: a few beads and a small warm light.
            var amber = Glowing(new Color(1, .62f, .22f), 1.5f);
            for (float s = 9; s < h.Length - 12; s += 15)
            {
                int i = RingAt(s), k = (int)(s / 15) % 2 == 0 ? P - 4 : 3; var at = ring[i, k] + ToAir(i, k) * .05f;
                for (int q = 0; q < 4; q++) Part(PrimitiveType.Sphere, t, at + new Vector3((C() - .5f) * .5f, (C() - .5f) * .7f, (C() - .5f) * .5f), Vector3.one * (.07f + C() * .09f), amber);
                Glow(t, at + ToAir(i, k) * .6f, 6.5f, .55f, new Color(1, .66f, .3f), .55f);
            }
        }
        /// <summary>A pitch torch in a wall: a short stick leaning out of the rock, its glowing head and flame, a flickering light.</summary>
''')

# ---------------------------------------------------------------- the capture tour: the deep's two broken shots
edit(CAPTURE,
'''                    var views = new List<(float from, float toward, float pitch, string shot)> { (6.5f, 12.5f, 10, "85-hollow-camp" + hs) };
                    if (drop > 0) views.Add((drop - 2.5f, drop + 7, 24, "88-hollow-drop" + hs));
''',
'''                    // A cave whose floor falls away from the mouth itself (the Root-Mother's Deep, down its root-stair) has no camp by
                    // the way in, and the head of that stair is out in the daylight: both views stood at the mouth, the camera outside
                    // it. Its first picture is its first chamber (the widest ring of the first two fifths: the Root Gallery) from just
                    // inside it, and its drop the steepest stretch past that chamber (the Cold Stair), from its head.
                    float campAt = 6.5f, campTo = 12.5f, campPitch = 10, dropPitch = 24;
                    if (drop >= 0 && drop < 12.5f)
                    {
                        float first = -1, steep = .01f; widest = 0; drop = -1;
                        for (int i = 0; i < hollow.Centre.Count; i++)
                            if (hollow.Along[i] > hollow.Length * .12f && hollow.Along[i] < hollow.Length * .4f && hollow.Half[i] > widest) { widest = hollow.Half[i]; first = hollow.Along[i]; }
                        for (int i = 0; first > 0 && i + 3 < hollow.Centre.Count; i++)
                            if (hollow.Along[i] > first + 8 && hollow.Centre[i].y - hollow.Centre[i + 3].y > steep + .01f) { steep = hollow.Centre[i].y - hollow.Centre[i + 3].y; drop = hollow.Along[i]; }
                        if (first > 0) { campAt = first - 4; campTo = first + 4; campPitch = 16; }   // from the stair's foot, over the stair behind
                        dropPitch = 18;   // under the Sap Well's roof
                    }
                    var views = new List<(float from, float toward, float pitch, string shot)> { (campAt, campTo, campPitch, "85-hollow-camp" + hs) };
                    if (drop > 0) views.Add((drop - 2.5f, drop + 7, dropPitch, "88-hollow-drop" + hs));
''')

MARKERS = [('void CaveLining(', BUILDER), ('void CaveDressing(', BUILDER), ('Mesh Drip(int i)', BUILDER), ('public Material cave, caveEarth;', ART),
           ('EnsurePaintedCave(art);', SCENE), ('float campAt = 6.5f', CAPTURE)]

def main():
    problems = []; texts = {}; new = []
    for f in (BUILDER, ART, SCENE, CAPTURE):
        path = os.path.join(ROOT, f)
        if not os.path.isfile(path): problems.append('missing file: ' + path); continue
        with open(path, 'rb') as h: raw = h.read()
        if b'\r' in raw: problems.append(f + ': has CR line ends (expected LF)')
        texts[f] = raw.decode('utf-8')
    for src, dst, meta, sibling in NEW:
        s = os.path.join(HERE, src)
        if not os.path.isfile(s): problems.append('missing beside the script: ' + s); continue
        with open(s, 'rb') as h: body = h.read()
        if b'\r' in body: problems.append(src + ': has CR line ends (expected LF)')
        for p in (os.path.join(ROOT, dst), os.path.join(ROOT, dst + '.meta')):
            if os.path.exists(p): problems.append('already installed: ' + p)
        if not os.path.isdir(os.path.dirname(os.path.join(ROOT, dst))): problems.append('missing folder for ' + dst)
        sib = os.path.join(ROOT, sibling)
        if not os.path.isfile(sib): problems.append('missing sibling meta: ' + sib)
        else:
            with open(sib, 'rb') as h: lines = h.read().decode('utf-8').splitlines()
            mine = meta.splitlines()
            if [l for l in lines if not l.startswith('guid:')] != [l for l in mine if not l.startswith('guid:')]: problems.append(dst + '.meta: the format no longer matches ' + sibling)
            guid = [l for l in mine if l.startswith('guid:')][0].split()[1]
            if len(guid) != 32 or any(ch not in '0123456789abcdef' for ch in guid): problems.append(dst + '.meta: bad guid')
        new.append((dst, body, meta))
    if problems: fail(problems)
    for marker, f in MARKERS:
        if marker in texts[f]: problems.append('%s: already patched (found "%s")' % (f, marker))
    out = dict(texts)
    for i, (f, old, new_text) in enumerate(EDITS):
        n = texts[f].count(old)
        if n != 1: problems.append('%s: edit %d: old block found %d times, expected 1: %s' % (f, i + 1, n, old.strip().splitlines()[0][:90])); continue
        if out[f].count(old) != 1: problems.append('%s: edit %d: old block no longer unique after earlier edits' % (f, i + 1)); continue
        out[f] = out[f].replace(old, new_text)
    if problems: fail(problems)
    for f in out:
        with open(os.path.join(ROOT, f), 'wb') as h: h.write(out[f].encode('utf-8'))
        print('patched', f)
    for dst, body, meta in new:
        with open(os.path.join(ROOT, dst), 'wb') as h: h.write(body)
        with open(os.path.join(ROOT, dst + '.meta'), 'wb') as h: h.write(meta.encode('utf-8'))
        print('installed', dst, '(+ .meta)')
    print('caves: %d edits in %d files, %d new files' % (len(EDITS), len(out), len(new)))

def fail(problems):
    print('patch_caves: NOTHING WRITTEN')
    for p in problems: print('  ' + p)
    sys.exit(1)

if __name__ == '__main__': main()
