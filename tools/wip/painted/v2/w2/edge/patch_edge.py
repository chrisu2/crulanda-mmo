"""The painted style pass, worklist item 9 (the world edge at exits): patch. Usage: python patch_edge.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Scripts\\World\\ZoneBuilder.cs
     BuildExits             the waystone: an eight-sided standing stone, tapered and leaning, a carved band round its waist,
                            a lantern house through its head (four posts under a pointed cap, the glow set back inside) and
                            two loose stones at its foot. No collider (as before), one light (as before, now in the lantern).
     Awake                  the grass runs on past the edge (GrassField.BuildEdge); PlantField is given the edge too.
     EdgeOpenness, EdgeGround, EdgeDressing, skirtY   new: where grass may grow past the edge (the mirrored paint's own
                            openness, none on a steep skirt or toward the Wasting) and the skirt's drawn height there.
     BuildBackdrop          skirt rings every 1 to 4 m over the first 34 m (were 1.5 to 8 m), so the near slope is a slope and
                            not four long facets; the pine silhouettes of the rows by the edge have five tiers of five boughs
                            from low on the trunk (were three of four); a near wood from 3 to 21 m past the line: full-crowned
                            broadleaf (three leaf-coloured lumps and eight leaf clusters), full pines, dead wood and boulders
                            in the zone's own mix, clear of the roads' ways out.
   Scripts\\World\\GrassField.cs
     BuildEdge              new: the same tufts and flowers over the backdrop's near slope, full at the line, none by 26 m.
   Scripts\\World\\PlantField.cs
     Build                  the edge ferns and the flower drifts run on past the line and thin out the same way.

   The zone's random stream is untouched: BuildExits draws from a stream keyed to where the stone stands (TreeRandom), the
   near wood from a stream of its own (seed + 4545), GrassField.BuildEdge from its own (seed + 101), PlantField's new plants
   after every draw its stream made before. The backdrop's placing stream (rnd) makes the draws it made. No collider, navmesh
   source, light count or registered object changes; everything new is past the boundary wall or on the exit's own root.

   Everything is checked before anything is written: every old block must occur exactly once in its file. So a second
   run, or a run on a tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
ZB = r'Scripts\World\ZoneBuilder.cs'; GF = r'Scripts\World\GrassField.cs'; PF = r'Scripts\World\PlantField.cs'

# (file, what, old, new)
EDITS = [
# ---------------------------------------------------------------- ZoneBuilder.cs: Awake ----------------------------------------------------------------
(ZB, 'the grass past the edge',
r'''            }
            Lap("grass");
''',
r'''                // The grass runs on past the edge over the backdrop's near slope, thinning to nothing (the same tufts, at the field's density).
                GetComponent<GrassField>().BuildEdge(Half, lush, Gloom ? null : art.flowers, EdgeOpenness, EdgeGround, Zone.seed + 101, Zone.biome == "meadow" ? 3f : Zone.biome == "verdant" ? 2.8f : 1.6f, EdgeDressing);
            }
            Lap("grass");
'''),
(ZB, 'the plants past the edge',
r'''.Build(this, art, Gloom ? null : art.flowers, Openness, Zone.seed + 177);''',
r'''.Build(this, art, Gloom ? null : art.flowers, Openness, Zone.seed + 177, EdgeOpenness, EdgeGround, EdgeDressing);'''),
# ---------------------------------------------------------------- ZoneBuilder.cs: the waystone ----------------------------------------------------------------
(ZB, 'the waystone',
r'''                MeshPart(PropMesh("Waystone", () => Cutout(new[] { new Vector2(-.4f, -1.1f), new Vector2(-.4f, .78f), new Vector2(-.24f, 1.1f), new Vector2(.24f, 1.1f), new Vector2(.4f, .78f), new Vector2(.4f, -1.1f) }, .5f)), t, new Vector3(0, 1.1f, 0), RockTint(new Color(.5f, .48f, .44f)), Quaternion.Euler(0, 15, 0));   // one standing stone, its shoulders chamfered
                Part(PrimitiveType.Cube, t, new Vector3(0, 2.45f, 0), new Vector3(.35f, .45f, .35f), art.glass);
                var l = new GameObject("Waystone light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 2.5f, 0);
''',
r'''                // An eight-sided standing stone, tapered, leaning a little with the years (its lean and turn from where it stands: no
                // zone draw), set deep enough for a slope; a carved band round its waist; a lantern house cut through its head (four
                // posts under a pointed cap) with the light set back inside it; two loose stones at its foot. One mesh of painted rock.
                var wr = TreeRandom(t.position); float W() { return (float)wr.NextDouble(); }
                var rock = RockTint(new Color(.5f, .48f, .44f));
                var stone = new GameObject("Waystone").transform; stone.SetParent(t, false); stone.localRotation = Quaternion.Euler(2 + W() * 3, 15 + (W() - .5f) * 30, (W() - .5f) * 5);
                MeshPart(PropMesh("Waystone stone", () => Joined(
                    Piece(Turned(new[] { new Vector2(.41f, -.5f), new Vector2(.38f, .25f), new Vector2(.31f, .95f), new Vector2(.275f, .98f), new Vector2(.27f, 1.08f), new Vector2(.3f, 1.11f), new Vector2(.27f, 1.56f), new Vector2(.34f, 1.6f), new Vector2(.34f, 1.68f), new Vector2(0, 1.68f) }, 8, 1.2f)),
                    Piece(Turned(new[] { new Vector2(0, 2.02f), new Vector2(.37f, 2.02f), new Vector2(.37f, 2.1f), new Vector2(.12f, 2.38f), new Vector2(0, 2.42f) }, 8, 1.2f)),
                    Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(.2f, 1.85f, .2f)), Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(-.2f, 1.85f, .2f)),
                    Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(.2f, 1.85f, -.2f)), Piece(ZoneMeshes.Box(new Vector3(.1f, .36f, .1f)), new Vector3(-.2f, 1.85f, -.2f)))), stone, Vector3.zero, rock);
                Part(PrimitiveType.Cube, stone, new Vector3(0, 1.85f, 0), new Vector3(.27f, .32f, .27f), art.glass);
                Lump(BoulderAt(1), t, new Vector3(.46f, LocalGround(t, .46f, .14f) + .07f, .14f), new Vector3(.56f, .36f, .46f), rock, W() * 360);
                Lump(BoulderAt(4), t, new Vector3(-.32f, LocalGround(t, -.32f, -.36f) + .05f, -.36f), new Vector3(.38f, .25f, .32f), rock, W() * 360);
                var l = new GameObject("Waystone light").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(0, 1.9f, 0);
'''),
(ZB, "BuildExits' summary",
r'''        /// <summary>Exit markers: a waystone with a lantern where a road leaves the zone.</summary>
''',
r'''        /// <summary>Exit markers: a waystone with a lantern in its head where a road leaves the zone. Dressing only: no collider.</summary>
'''),
# ---------------------------------------------------------------- ZoneBuilder.cs: the edge's helpers ----------------------------------------------------------------
(ZB, 'the edge helpers (after BackdropWidth)',
r'''        const float BackdropWidth = 90;
''',
r'''        const float BackdropWidth = 90;
        /// <summary>How far past the edge the ground's dressing (grass, ferns, flowers) runs on, thinning out, in metres.</summary>
        const float EdgeDressing = 26;
        /// <summary>The backdrop skirt's drawn height at a point (BuildBackdrop sets it).</summary>
        Func<float, float, float> skirtY;
        /// <summary>The ground under a point for dressing: the zone's inside the edge, the backdrop's skirt as drawn past it.</summary>
        Vector3 EdgeGround(Vector2 p, float lift)
        {
            if (skirtY == null || Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) <= Half) return Ground(p, lift);
            return new Vector3(p.x, skirtY(p.x, p.y) + lift, p.y);
        }
        /// <summary>
        /// How much grass should grow at a point past the edge (Openness inside it). The skirt wears the ground's paint mirrored
        /// across the edge, so the grass follows the mirror image: none on the road's paint running on, the mirrored yards and
        /// fields. None past EdgeDressing, toward the Wasting, or where the skirt climbs steeper than 40 degrees. Draws nothing.
        /// </summary>
        float EdgeOpenness(Vector2 p)
        {
            float past = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Half; if (past <= 0) return Openness(p);
            if (past >= EdgeDressing || (Zone.wasting != null && p.x > Zone.wasting.x - 6)) return 0;
            if (skirtY != null && Mathf.Max(Mathf.Abs(skirtY(p.x + 1, p.y) - skirtY(p.x - 1, p.y)), Mathf.Abs(skirtY(p.x, p.y + 1) - skirtY(p.x, p.y - 1))) > 1.7f) return 0;
            return Openness(new Vector2(p.x > Half ? Zone.size - p.x : p.x < -Half ? -Zone.size - p.x : p.x, p.y > Half ? Zone.size - p.y : p.y < -Half ? -Zone.size - p.y : p.y));
        }
        /// <summary>The near wood's leaf mass by leaf family (0 to 2): deep, mid and light, the colours of the family's painted card.</summary>
        static readonly Color[] NearLeaf = {
            new Color(.20f, .33f, .12f), new Color(.27f, .42f, .15f), new Color(.34f, .50f, .18f),
            new Color(.33f, .38f, .11f), new Color(.46f, .48f, .13f), new Color(.56f, .56f, .16f),
            new Color(.50f, .22f, .07f), new Color(.64f, .30f, .08f), new Color(.76f, .42f, .11f) };
'''),
(ZB, "BuildBackdrop's summary",
r'''        /// the edge carries on (its mirrored paint) up a valley that closes into trees or a col. Visual only: no colliders,
        /// no nav sources, outside the navmesh bounds and the map. Its own random stream leaves the zone's layout unchanged.
        /// </summary>
''',
r'''        /// the edge carries on (its mirrored paint) up a valley that closes into trees or a col. By the edge a near wood
        /// (full crowns, pines, dead wood and boulders from 3 to 21 m out, in the same mix) stands between the silhouettes'
        /// first rows, so the land carries on at the line; the grass and plants run on over the same slope (EdgeOpenness).
        /// Visual only: no colliders, no nav sources, outside the navmesh bounds and the map. Its own random streams leave
        /// the zone's layout unchanged.
        /// </summary>
'''),
(ZB, "the skirt's rings",
r'''            float[] rings = { 0, 1.5f, 4, 8, 13, 19, 26, 34, 43, 53, 64, 76, BackdropWidth };
''',
r'''            // Rings close together by the edge (the slope the exits look up, and the dressing stands on it), wider further out.
            float[] rings = { 0, 1.5f, 2.5f, 4, 6, 8, 10.5f, 13, 16, 19, 22.5f, 26, 30, 34, 43, 53, 64, 76, BackdropWidth };
'''),
(ZB, 'skirtY (after Drawn)',
r'''                return u >= s ? ha + (hb - ha) * u + (hc - hb) * s : ha + (hd - ha) * s + (hc - hd) * u;   // the quad's two triangles, split from a to d+1
            }
''',
r'''                return u >= s ? ha + (hb - ha) * u + (hc - hb) * s : ha + (hd - ha) * s + (hc - hd) * u;   // the quad's two triangles, split from a to d+1
            }
            skirtY = Drawn;   // the grass and plants past the edge stand on it (EdgeGround)
'''),
(ZB, "the pine silhouette's tiers",
r'''                                // Three tiers of four drooping bough cards, widest at the foot, over the height the cones had, and two
                                // crossed upright fronds to the tip.
                                var set = CardsFor(boughMat, side); float turn = S() * 360;
                                for (int i = 0; i < 3; i++)
                                {
                                    float u = i / 2f, y = 1 + th * Mathf.Lerp(.2f, .62f, u), rad = th * Mathf.Lerp(.3f, .14f, u); var heart = at + Vector3.up * (y - rad * .5f);
                                    var tier = Color.Lerp(new Color(.6f, .68f, .6f), Color.white, u);
                                    for (int k = 0; k < 4; k++)
                                    {
                                        float droop = (22 + S() * 12) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + i * 45 + k * 90 + (S() - .5f) * 20, 0) * Vector3.right;
''',
r'''                                // Three tiers of four drooping bough cards, widest at the foot, over the height the cones had, and two
                                // crossed upright fronds to the tip. The rows by the edge are seen from a few metres: five tiers of five
                                // there, from lower on the trunk, so they stand as pines beside the zone's own and not as three ruffs on a pole.
                                var set = CardsFor(boughMat, side); float turn = S() * 360; int tiers = row < 20 ? 5 : 3, boughs = row < 20 ? 5 : 4;
                                for (int i = 0; i < tiers; i++)
                                {
                                    float u = i / (tiers - 1f), y = 1 + th * Mathf.Lerp(row < 20 ? .08f : .2f, .62f, u), rad = th * Mathf.Lerp(.3f, .14f, u); var heart = at + Vector3.up * (y - rad * .5f);
                                    var tier = Color.Lerp(new Color(.6f, .68f, .6f), Color.white, u);
                                    for (int k = 0; k < boughs; k++)
                                    {
                                        float droop = (22 + S() * 12) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + i * 45 + k * (360f / boughs) + (S() - .5f) * 20, 0) * Vector3.right;
'''),
(ZB, 'the near wood (before the silhouettes are merged)',
r'''            foreach (var kv in parts)
            {
                var mesh = new Mesh { name = "Backdrop " + kv.Key.Item1.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
''',
r'''            // The near wood: trees and boulders scattered from 3 to 21 m past the line, most of them close to it, in the zone's
            // own mix, so at an exit the wood carries on between the silhouettes' first rows and thins out up the slope. A
            // broadleaf has a body (three lumps in its leaf's deep, mid and light) under eight leaf clusters, five round its
            // waist and three over the top; a pine five tiers of five boughs; dead wood five limbs. Each stands on the lowest
            // drawn ground round it, none on a face, on a road's way out or toward the Wasting. A stream of its own: the
            // silhouettes above stand where they did.
            var near = new System.Random(Zone.seed + 4545); float N() { return (float)near.NextDouble(); }
            var nearRock = RockTint(ash ? new Color(.33f, .32f, .31f) : mountain ? MountainStone : new Color(.42f, .41f, .39f));   // the zone's own boulders' stone
            float rocky = mountain ? .35f : ash ? .5f : .08f;
            for (int i = 0, n = Mathf.RoundToInt(4 * (Zone.size + 24) / (ash ? 9 : 4.2f)); i < n; i++)
            {
                int side = Mathf.Min(3, (int)(N() * 4)); float a = (N() * 2 - 1) * (Half + 12), o = Half + 3 + 18 * Mathf.Pow(N(), 1.5f), yaw = N() * 360, th = 6.5f + N() * 5, kind = N(), pick = N() * (dead + pines + leafy);
                var q = side == 0 ? new Vector2(a, -o) : side == 1 ? new Vector2(o, a) : side == 2 ? new Vector2(-a, o) : new Vector2(-o, -a);
                if ((Zone.wasting != null && q.x > Zone.wasting.x - 6) || valleys.Exists(v => DistanceToPath(q, v) < 8)) continue;
                float lo = Drawn(q.x, q.y), hi = lo;
                for (int k = 0; k < 6; k++) { float y = Drawn(q.x + Mathf.Cos(k * 1.0472f) * 1.1f, q.y + Mathf.Sin(k * 1.0472f) * 1.1f); lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
                if (hi - lo > 2) continue;   // a face: nothing stands on it
                var at = new Vector3(q.x, lo - .3f - (hi - lo) * .3f, q.y);
                if (kind < rocky)
                {
                    // A boulder, half sunk, and a smaller one against it.
                    float s = .6f + N() * 1.2f;
                    Put(side, crag, nearRock, at + Vector3.up * .3f * s, Quaternion.Euler(0, yaw, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s));
                    Put(side, crag, nearRock, at + Quaternion.Euler(0, yaw, 0) * new Vector3(.8f * s, .15f * s, .45f * s), Quaternion.Euler(0, yaw + 140, 0), new Vector3(1.2f * s, .9f * s, 1.1f * s));
                }
                else if (pick < dead)
                {
                    Put(side, spire, deadMat, at, Quaternion.identity, new Vector3(.5f, th, .5f));
                    for (int j = 0; j < 5; j++) Put(side, spire, deadMat, at + Vector3.up * th * (.36f + j * .1f), Quaternion.Euler(0, yaw + j * 137, 32 + N() * 24), new Vector3(.19f, th * (.42f - j * .04f), .19f));
                }
                else if (pick < dead + pines)
                {
                    Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.45f, th * .55f, .45f));
                    if (cardArt)
                    {
                        var set = CardsFor(boughMat, side); float turn = N() * 360;
                        for (int j = 0; j < 5; j++)
                        {
                            float u = j / 4f, y = 1 + th * Mathf.Lerp(.08f, .62f, u), rad = th * Mathf.Lerp(.3f, .14f, u); var heart = at + Vector3.up * (y - rad * .5f);
                            var tier = Color.Lerp(new Color(.6f, .68f, .6f), Color.white, u);
                            for (int k = 0; k < 5; k++)
                            {
                                float droop = (22 + N() * 12) * Mathf.Deg2Rad; var outward = Quaternion.Euler(0, turn + j * 36 + k * 72 + (N() - .5f) * 20, 0) * Vector3.right;
                                var along = outward * Mathf.Cos(droop) - Vector3.up * Mathf.Sin(droop);
                                set.Add(at + Vector3.up * (y + th * .05f), along, Vector3.Cross(along, Vector3.up).normalized, rad * 1.1f, rad * 1.6f, heart, .35f, tier, 0, 1, tier * .6f);
                            }
                        }
                        float ty = 1 + th * .68f, tl = th * .4f, lead = N() * 180; var tip = at + Vector3.up * (ty - tl * .5f);
                        set.Add(at + Vector3.up * ty, Vector3.up, Quaternion.Euler(0, lead, 0) * Vector3.right, tl, tl * .36f, tip, .5f, Color.white, 0, 1, new Color(.68f, .7f, .68f));
                        set.Add(at + Vector3.up * ty, Vector3.up, Quaternion.Euler(0, lead + 90, 0) * Vector3.right, tl * .95f, tl * .34f, tip, .5f, new Color(.92f, .92f, .9f), 0, 1, new Color(.68f, .7f, .68f));
                    }
                    else
                    {
                        Put(side, spire, pineMat, at + Vector3.up * (1 + th * .16f), Quaternion.Euler(0, yaw, 0), new Vector3(th * .26f, th * .6f, th * .26f));
                        Put(side, spire, pineMat, at + Vector3.up * (1 + th * .52f), Quaternion.Euler(0, yaw + 30, 0), new Vector3(th * .18f, th * .56f, th * .18f));
                    }
                }
                else
                {
                    int fam = Mathf.Min(2, (int)(N() * 3)); float c = th * .46f; var centre = at + Vector3.up * th * .66f; var turned = Quaternion.Euler(0, yaw, 0);
                    Put(side, spire, art.bark, at, Quaternion.identity, new Vector3(.5f, th * .7f, .5f));
                    Put(side, crown, Tint(art.foliage, Wither(NearLeaf[fam * 3 + 1])), centre, turned, new Vector3(c, c * .72f, c));
                    Put(side, crown, Tint(art.foliage, Wither(NearLeaf[fam * 3])), centre + turned * new Vector3(c * .3f, -c * .22f, c * .1f), Quaternion.Euler(0, yaw + 90, 0), new Vector3(c * .78f, c * .52f, c * .78f));
                    Put(side, crown, Tint(art.foliage, Wither(NearLeaf[fam * 3 + 2])), centre + turned * new Vector3(-c * .2f, c * .26f, -c * .12f), Quaternion.Euler(0, yaw + 200, 0), new Vector3(c * .62f, c * .46f, c * .62f));
                    if (!cardArt) continue;
                    var set = CardsFor(LeafMaterial(fam), side);
                    for (int k = 0; k < 8; k++)
                    {
                        bool top = k >= 5; float round = yaw + (top ? (k - 5) * 120 + 40 : k * 72) + (N() - .5f) * 30, up = top ? 52 + N() * 22 : -6 + N() * 30, size = c * (.54f + N() * .14f);
                        var dir = Quaternion.Euler(0, round, 0) * (Quaternion.Euler(0, 0, up) * Vector3.right);
                        set.AddCross(centre + Vector3.Scale(dir, new Vector3(c * .3f, c * .2f, c * .3f)), dir, Vector3.Cross(dir, Vector3.up).normalized, size, size * .95f, centre, .3f, top ? new Color(1, 1, .95f) : new Color(.86f, .9f, .84f), 3);
                    }
                }
            }
            foreach (var kv in parts)
            {
                var mesh = new Mesh { name = "Backdrop " + kv.Key.Item1.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
'''),
# ---------------------------------------------------------------- GrassField.cs ----------------------------------------------------------------
(GF, 'BuildEdge (before Update)',
r'''        void Update()
        {
            var cam = Camera.main; if (cam == null || tuft == null) return;
''',
r'''        /// <summary>
        /// The grass past the zone's edge: the same tufts and wildflowers running on over the backdrop's near slope for
        /// <paramref name="reach"/> metres, as thick as the field at the line (<paramref name="density"/> a square metre) and
        /// thinning to nothing, the tufts a little bigger as they thin. Call after Build. Cells of its own, drawn with the
        /// rest; a stream of its own; its own copies of the materials, which fade with distance as the field's do
        /// (Grass.shader's _FadeFar, where the shader has it).
        /// </summary>
        public void BuildEdge(float half, Material[] grass, Material[] flowers, System.Func<Vector2, float> openness, System.Func<Vector2, float, Vector3> ground, int seed, float density, float reach)
        {
            if (tuft == null || grass == null || grass.Length == 0 || reach <= 0) return;
            var edgeRng = new System.Random(seed); float R() { return (float)edgeRng.NextDouble(); }
            float span = half * 2 + reach;   // each side's strip takes one corner
            var copies = new Dictionary<Material, Material>(); var edge = new Dictionary<(int, int), CellData>();
            Material Copy(Material m)
            {
                if (!copies.TryGetValue(m, out var c)) { copies[m] = c = new Material(m) { name = m.name + " (edge)", enableInstancing = true }; c.SetFloat("_FadeFar", DrawDistance + Cell / 2); }
                return c;
            }
            for (int i = 0, count = Mathf.RoundToInt(4 * span * reach * density); i < count; i++)
            {
                int side = edgeRng.Next(4); float a = R() * span, o = R() * reach, keep = R();
                var p = side == 0 ? new Vector2(half - a, -half - o) : side == 1 ? new Vector2(half + o, half - a) : side == 2 ? new Vector2(-half + a, half + o) : new Vector2(-half - o, -half + a);
                float past = Mathf.Clamp01((Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half) / reach), open = openness(p) * Mathf.SmoothStep(1, 0, past);
                if (open <= 0 || keep > open) continue;
                bool flower = flowers != null && flowers.Length > 0 && R() < .07f;
                var mat = Copy(flower ? flowers[edgeRng.Next(flowers.Length)] : grass[edgeRng.Next(grass.Length)]);
                float s = flower ? .45f + R() * .25f : (.55f + R() * .6f) * (1 + .4f * past);
                var m = Matrix4x4.TRS(ground(p, -.02f), Quaternion.Euler(0, R() * 360, 0), new Vector3(s, s * (.8f + R() * .5f), s));
                var key = (Mathf.FloorToInt((p.x + half) / Cell), Mathf.FloorToInt((p.y + half) / Cell));
                if (!edge.TryGetValue(key, out var cell)) { edge[key] = cell = new CellData { center = new Vector3(-half + (key.Item1 + .5f) * Cell, 0, -half + (key.Item2 + .5f) * Cell) }; cells.Add(cell); }
                if (!cell.byMaterial.TryGetValue(mat, out var list)) cell.byMaterial[mat] = list = new List<Matrix4x4>();
                list.Add(m);
            }
            foreach (var cell in edge.Values)
            {
                cell.baked = new List<(Material, Matrix4x4[])>();
                foreach (var kv in cell.byMaterial)
                    for (int start = 0; start < kv.Value.Count; start += 1023)
                        cell.baked.Add((kv.Key, kv.Value.GetRange(start, Mathf.Min(1023, kv.Value.Count - start)).ToArray()));
                cell.byMaterial.Clear();
            }
        }
        void Update()
        {
            var cam = Camera.main; if (cam == null || tuft == null) return;
'''),
# ---------------------------------------------------------------- PlantField.cs ----------------------------------------------------------------
(PF, "PlantField's summary",
r'''    /// glowing flowers at night); nothing grows on the ash. Its own random stream: the zone's layout is untouched.
''',
r'''    /// glowing flowers at night); nothing grows on the ash. The edge ferns and the flower drifts run on past the zone's edge
    /// over the backdrop's near slope and thin out to nothing there. Its own random stream: the zone's layout is untouched.
'''),
(PF, "Build's signature",
r'''        public void Build(ZoneBuilder zone, ZoneArt art, Material[] flowers, System.Func<Vector2, float> openness, int seed)
        {
''',
r'''        /// <summary>Bakes the plants. With <paramref name="edgeOpenness"/> and <paramref name="edgeGround"/> (what may grow at a point
        /// past the edge, and the ground there) the edge's plants run on <paramref name="edgeReach"/> metres past it.</summary>
        public void Build(ZoneBuilder zone, ZoneArt art, Material[] flowers, System.Func<Vector2, float> openness, int seed,
            System.Func<Vector2, float> edgeOpenness = null, System.Func<Vector2, float, Vector3> edgeGround = null, float edgeReach = 0)
        {
            Material[] glowing = null;
'''),
(PF, 'Plant: a plant past the edge',
r'''            void Plant(Mesh mesh, Material mat, Vector2 p, float size, float lift = -.03f)
            {
''',
r'''            void Plant(Mesh mesh, Material mat, Vector2 p, float size, float lift = -.03f, bool past = false)
            {
'''),
(PF, "Plant: the ground under it",
r'''                list.Add(Matrix4x4.TRS(zone.Ground(p, lift), Quaternion.Euler(0, R() * 360, 0), new Vector3(size, size * (.85f + R() * .3f), size)));
''',
r'''                list.Add(Matrix4x4.TRS(past ? edgeGround(p, lift) : zone.Ground(p, lift), Quaternion.Euler(0, R() * 360, 0), new Vector3(size, size * (.85f + R() * .3f), size)));
'''),
(PF, 'the glowing flowers, kept for the edge',
r'''                var glow = mix.glow > 0 ? Glowing(flowers) : null;
''',
r'''                var glow = glowing = mix.glow > 0 ? Glowing(flowers) : null;
'''),
(PF, 'the plants past the edge (before the bake)',
r'''            foreach (var cell in cells)
            {
                cell.baked = new List<(Mesh, Material, Matrix4x4[])>();
''',
r'''            // Past the edge: the forest edge's ferns (which stop 3 m short of the line inside) and the flower drifts run on over the
            // backdrop's near slope, thinning to nothing by edgeReach, so the dressing never stops on a line (the grass does the
            // same: GrassField.BuildEdge). They go in the edge cells. Drawn after everything else: every plant inside the zone
            // stands where it did.
            if (edgeOpenness != null && edgeGround != null && edgeReach > 0)
            {
                float span = z.size + edgeReach;   // each side's strip takes one corner
                Vector2 Spot(float from, out float thin)
                {
                    int side = (int)(R() * 4); float a = R() * span, o = from + R() * (edgeReach - from);
                    var p = side == 0 ? new Vector2(half - a, -half - o) : side == 1 ? new Vector2(half + o, half - a) : side == 2 ? new Vector2(-half + a, half + o) : new Vector2(-half - o, -half + a);
                    thin = Mathf.SmoothStep(1, 0, (Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half) / edgeReach); return p;
                }
                if (fern != null)
                    for (int k = 0, n = Mathf.RoundToInt(4 * span * (edgeReach + 3) * mix.fernShade); k < n; k++)
                    {
                        var p = Spot(-3, out float thin); float keep = R(), size = R();
                        if (keep < thin && edgeOpenness(p) > 0 && NotOnTrunk(zone, p)) { Plant(fernMesh, fern, p, .7f + size * .8f, -.03f, true); Ferns++; }
                    }
                if (mix.flowers && flowers != null && flowers.Length > 0 && mix.flowerDrift > 0)
                    for (int k = 0, n = Mathf.RoundToInt(4 * span * edgeReach * .12f); k < n; k++)
                    {
                        var p = Spot(0, out float thin); float keep = R(), size = R();
                        float drift = Mathf.PerlinNoise(p.x * .045f + 31, p.y * .045f + 17);
                        if (drift < .58f || keep > mix.flowerDrift * (drift - .58f) * 4 * thin || edgeOpenness(p) < .35f) continue;
                        int hue = Mathf.Min(flowers.Length - 1, (int)(Mathf.PerlinNoise(p.x * .012f + 7, p.y * .012f + 3) * flowers.Length * 1.2f));
                        bool lit = glowing != null && Mathf.PerlinNoise(p.x * .02f + 50, p.y * .02f + 60) > .62f;
                        Plant(tuftMesh, lit ? glowing[hue] : flowers[hue], p, .8f + size * .5f, -.03f, true); Flowers++;
                    }
            }
            foreach (var cell in cells)
            {
                cell.baked = new List<(Mesh, Material, Matrix4x4[])>();
'''),
]

MARKS = ('EdgeOpenness', 'BuildEdge', 'edgeReach')   # a tree this patch has already been applied to

def main():
    files = {}
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path)
        if full not in files:
            if not os.path.isfile(full): sys.exit('patch_edge: NOTHING WRITTEN.\n  missing ' + full)
            files[full] = io.open(full, 'r', encoding='utf-8', newline='').read()
    out = dict(files); problems = []
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path); text = files[full]
        if '\r\n' in text: old, new = old.replace('\n', '\r\n'), new.replace('\n', '\r\n')   # the file's own line ends
        n = text.count(old)
        if n != 1: problems.append('%s: %s: the old block occurs %d times, not once' % (path, what, n)); continue
        if out[full].count(old) != 1: problems.append('%s: %s: overlaps another edit' % (path, what)); continue
        out[full] = out[full].replace(old, new)
    for full, text in files.items():
        for mark in MARKS:
            if mark in text: problems.append('%s: already has %s' % (full, mark))
    if problems:
        sys.exit('patch_edge: NOTHING WRITTEN.\n  ' + '\n  '.join(problems))
    for full, text in out.items():
        io.open(full, 'w', encoding='utf-8', newline='').write(text)
        print('patched %s (%d edits)' % (full, sum(1 for e in EDITS if os.path.join(ROOT, e[0]) == full)))
    print('patch_edge: OK')

if __name__ == '__main__':
    main()
