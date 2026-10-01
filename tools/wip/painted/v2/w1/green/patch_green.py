"""The painted style pass, worklist items 2, 3 and 7 (green things): patch. Usage: python patch_green.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Scripts\\World\\ZoneBuilder.cs
     Broadleaf, LeafCrown   a leaf-coloured lump at every clump spot (the family's deep, mid and light: LeafMass), the cards a
                            ragged fringe a little bigger (.95 of the clump, was .8), the core smaller (2.6 x 1.5 x 2.6, was
                            4 x 2.3 x 4) and in the family's shade, never a darkened brown. LeafCrown's new parameters are
                            optional and last: the giant trees' call (ZoneBuilder.Verdant.cs) builds what it did.
     the orchard            the same mass in fresh green under its cards.
     BuildBackdrop          a broadleaf silhouette gets two leaf-coloured lumps inside its three cards.
     PaintGround            meadow turf lifted to the tufts' colour (the gold patches kept); the alp a step greener.
     Awake                  meadow tufts 3 a square metre (were 2.3).
     hedge                  Hedge(): a row of overlapping leafy lumps in three greens with sprays of leaf; the same collider.
     haystack               Haystack(): a turned, tiered rick in thatch with a pole and loose hay at its foot; the same collider.
     Cart                   its load is a heaped, tiered mound in the same thatch.
   Scripts\\World\\GrassField.cs
     Build                  its own copies of the materials, which fade (Grass.shader's _FadeFar) over the 20 m before the
                            nearest cell that may be off; tall patches with a wobbling rim, thinner and shorter toward it, a
                            few clumps past it, a rolling height and drifts of bleached tufts.
   World\\Shaders\\Grass.shader
     _FadeFar               tufts shrink into the ground over the 20 m before it (0, the default: no fade, so PlantField's
                            cards and the assets are as they were).

   No draw is added to or taken from the zone's random stream: the lumps, the hedge and the rick draw from streams keyed to
   where they stand (TreeRandom), the tall grass from GrassField's own stream and position hashes. No collider, navmesh
   blocker, light or registered object changes.

   Everything is checked before anything is written: every old block must occur exactly once in its file. So a second
   run, or a run on a tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
ZB = r'Scripts\World\ZoneBuilder.cs'; GF = r'Scripts\World\GrassField.cs'; GS = r'World\Shaders\Grass.shader'

# (file, what, old, new)
EDITS = [
# ---------------------------------------------------------------- ZoneBuilder.cs: the meadow ----------------------------------------------------------------
(ZB, 'the meadow tuft density',
r'''Openness, Zone.seed + 99, Zone.biome == "meadow" ? 2.3f : Zone.biome == "verdant" ? 2.8f : 1.6f, TallGrassPatches(),''',
r'''Openness, Zone.seed + 99, Zone.biome == "meadow" ? 3f : Zone.biome == "verdant" ? 2.8f : 1.6f, TallGrassPatches(),'''),
(ZB, 'the meadow turf paint',
r'''            if (Zone.biome == "meadow") { grassA = new Color(.22f, .40f, .16f); grassB = new Color(.35f, .47f, .19f); grassC = new Color(.50f, .44f, .20f);''',
r'''            if (Zone.biome == "meadow") { grassA = new Color(.25f, .48f, .17f); grassB = new Color(.37f, .58f, .21f); grassC = new Color(.50f, .44f, .20f);'''),
(ZB, 'the alp paint',
r'''                        Color alp = Color.Lerp(new Color(.24f, .36f, .17f), new Color(.38f, .41f, .2f), n1);
''',
r'''                        Color alp = Color.Lerp(new Color(.26f, .41f, .18f), new Color(.38f, .46f, .21f), n1);   // a step toward the alpine tufts' green, so they sit in turf
'''),
# ---------------------------------------------------------------- ZoneBuilder.cs: hedge, haystack, cart ----------------------------------------------------------------
(ZB, 'the hedge and haystack props',
r'''                    case "hedge": Part(PrimitiveType.Cube, t, new Vector3(0, .7f, 0), new Vector3(p.size.x > 0 ? p.size.x : 6, 1.4f, 1.2f), Tint(art.foliage, new Color(.22f, .30f, .16f))); Solid(t, new Vector3(0, .7f, 0), new Vector3(p.size.x > 0 ? p.size.x : 6, 1.4f, 1.2f)); break;
                    case "haystack": Part(PrimitiveType.Sphere, t, new Vector3(0, .9f, 0), new Vector3(2.6f, 2.2f, 2.6f), art.hay); Solid(t, new Vector3(0, 1, 0), new Vector3(2.4f, 2, 2.4f)); break;
''',
r'''                    case "hedge": Hedge(t, p.size.x > 0 ? p.size.x : 6); break;
                    case "haystack": Haystack(t); break;
'''),
(ZB, "the cart's load",
r'''            Part(PrimitiveType.Sphere, t, new Vector3(0, 1.28f, .05f), new Vector3(1.42f, .86f, 2.4f), art.hay);
            Solid(t, new Vector3(0, .8f, -.4f), new Vector3(2.1f, 1.6f, 3.8f));
''',
r'''            // The load: a heaped mound of hay in three tiers, the rick's thatch, as long and wide and high as the old round one.
            MeshPart(PropMesh("Hay load", () => Turned(new[] { new Vector2(.47f, -.1f), new Vector2(.5f, .1f), new Vector2(.43f, .3f), new Vector2(.47f, .28f), new Vector2(.27f, .52f), new Vector2(0, .62f) }, 12, 1.8f)), t, new Vector3(0, .98f, .05f), Tint(art.thatch, new Color(.84f, .72f, .42f)))
                .transform.localScale = new Vector3(1.42f, 1.15f, 2.4f);
            Solid(t, new Vector3(0, .8f, -.4f), new Vector3(2.1f, 1.6f, 3.8f));
'''),
# ---------------------------------------------------------------- ZoneBuilder.cs: crowns ----------------------------------------------------------------
(ZB, "Broadleaf's crown",
r'''            // A full crown: three cards a cluster, ten to fourteen clusters, three or four lying under it, and a dark core about
            // 60% of the crown's radius (the crown reaches about 3.4 m from its heart), so from below it is leaf mass, not sky.
            LeafCrown(t, tr, lean + new Vector3(0, h + 1.5f, 0), spots, ends, LeafMaterial(family), bark, Tint(art.foliage, Wither(Color.Lerp(Leaf[family], Color.black, .45f))), 1.5f, new Vector3(4, 2.3f, 4), 3, 10, 14, 3, 4);
''',
r'''            // A full crown: a leaf-coloured lump at every clump spot, in the family's own deep, mid and light (the Great Oak's way:
            // a billowed mass, never a brown ball), three cards a cluster as its ragged fringe, ten to fourteen clusters, three or
            // four lying under it, and a small core in the family's shade at the heart, so from below it is leaf mass, not sky.
            var core = Tint(art.foliage, Wither(Color.Lerp(LeafMass[family][0], new Color(.14f, .22f, .1f), .5f)));
            LeafCrown(t, tr, lean + new Vector3(0, h + 1.5f, 0), spots, ends, LeafMaterial(family), bark, core, 1.5f, new Vector3(2.6f, 1.5f, 2.6f), 3, 10, 14, 3, 4, LeafMassOf(family), .95f);
'''),
(ZB, "LeafCrown's summary and signature",
r'''        /// are as before. One mesh for the cards and one for the boughs. Draws only from <paramref name="tr"/>, the tree's own
        /// stream, after its trunk and limbs, the extras' draws after the ones a plain crown takes: the zone's is untouched.
        /// </summary>
        void LeafCrown(Transform t, System.Random tr, Vector3 heart, List<(Vector3 at, float size, bool light)> spots, List<Vector3> ends, Material leaves, Material bark, Material core, float rim, Vector3 coreSize,
            int cardsPer = 2, int crownMin = 0, int crownMax = 0, int underMin = 0, int underMax = 0)
        {
''',
r'''        /// are as before. One mesh for the cards and one for the boughs. Draws only from <paramref name="tr"/>, the tree's own
        /// stream, after its trunk and limbs, the extras' draws after the ones a plain crown takes: the zone's is untouched.
        /// With <paramref name="mass"/> (deep, mid and light leaf, LeafMassOf) the crown has a body: a rounded lump at every clump
        /// spot, <paramref name="massSize"/> of the clump across and drawn a fifth of the way in to the heart, so they run together
        /// into one billowed mass in the leaf's own colour and the cards (<paramref name="cardSize"/> of the clump) are its
        /// ragged edge. The top and the light spots take the light leaf, the low ones the deep. Drawn last of all from the stream.
        /// </summary>
        void LeafCrown(Transform t, System.Random tr, Vector3 heart, List<(Vector3 at, float size, bool light)> spots, List<Vector3> ends, Material leaves, Material bark, Material core, float rim, Vector3 coreSize,
            int cardsPer = 2, int crownMin = 0, int crownMax = 0, int underMin = 0, int underMax = 0, Material[] mass = null, float cardSize = .8f, float massSize = .68f)
        {
'''),
(ZB, "LeafCrown's cluster size",
r'''            foreach (var spot in spots) Cluster(spot.at, spot.size * .8f, Shade(spot.light ? 2 : 1, T()), true);
''',
r'''            foreach (var spot in spots) Cluster(spot.at, spot.size * cardSize, Shade(spot.light ? 2 : 1, T()), true);
'''),
(ZB, "LeafCrown's core",
r'''            if (coreMesh != null) Lump(coreMesh, t, heart - new Vector3(0, .3f, 0), coreSize, core, T() * 360);
        }
''',
r'''            if (coreMesh != null) Lump(coreMesh, t, heart - new Vector3(0, .3f, 0), coreSize, core, T() * 360);
            // The leaf mass: a lump at every clump spot (the first is the crown's top).
            if (mass == null || mass.Length < 3) return;
            for (int i = 0; i < spots.Count; i++)
            {
                var spot = spots[i]; float wide = spot.size * (massSize + T() * .1f);
                var size = new Vector3(wide, spot.size * (massSize * .65f + T() * .08f), wide * (.92f + T() * .16f));
                Lump(LeafLumpAt((int)(T() * 6)), t, Vector3.Lerp(spot.at, heart, .2f), size, mass[i == 0 || spot.light ? 2 : spot.at.y < heart.y ? 0 : 1], T() * 360).name = "Leaf mass";
            }
        }
        /// <summary>A crown's leaf mass by leaf family (Leaf[]): deep, mid and light, taken from the family's painted card (fresh green,
        /// yellow-green, autumn orange, dull gold), so a crown's body is the colour of its leaves.</summary>
        static readonly Color[][] LeafMass = {
            new[] { new Color(.20f, .33f, .12f), new Color(.27f, .42f, .15f), new Color(.34f, .50f, .18f) },
            new[] { new Color(.33f, .38f, .11f), new Color(.46f, .48f, .13f), new Color(.56f, .56f, .16f) },
            new[] { new Color(.50f, .22f, .07f), new Color(.64f, .30f, .08f), new Color(.76f, .42f, .11f) },
            new[] { new Color(.34f, .34f, .11f), new Color(.45f, .44f, .13f), new Color(.54f, .52f, .16f) } };
        /// <summary>A leaf family's mass materials (deep, mid, light), greyed in gloom.</summary>
        Material[] LeafMassOf(int family) { var c = LeafMass[Mathf.Abs(family) % LeafMass.Length]; return new[] { Tint(art.foliage, Wither(c[0])), Tint(art.foliage, Wither(c[1])), Tint(art.foliage, Wither(c[2])) }; }
        Mesh[] leafLumps;
        /// <summary>A rounded leafy lump by index, no zone draw (crowns' masses, hedges, loose hay): a blob a little lumpier and lighter
        /// (8 x 12) than a canopy, since a crown takes five to seven.</summary>
        Mesh LeafLumpAt(int i) { if (leafLumps == null) { leafLumps = new Mesh[6]; for (int k = 0; k < 6; k++) leafLumps[k] = ZoneMeshes.Blob(Zone.seed + 950 + k * 19, .6f, false, 8, 12); } return leafLumps[((i % 6) + 6) % 6]; }
        /// <summary>
        /// A hedge along x: a row of overlapping rounded leafy lumps in two greens, smaller and lighter ones along its top and a few
        /// sprays of painted leaf standing out of it, inside the box it always was (<paramref name="length"/> x 1.4 x 1.2 m: its
        /// collider, unchanged). Its shape comes from a stream keyed to where it stands: the zone's draws are untouched.
        /// </summary>
        void Hedge(Transform t, float length)
        {
            var hr = TreeRandom(t.position); float H() { return (float)hr.NextDouble(); }
            var greens = new[] { Tint(art.foliage, Wither(new Color(.19f, .32f, .12f))), Tint(art.foliage, Wither(new Color(.25f, .39f, .14f))), Tint(art.foliage, Wither(new Color(.31f, .45f, .17f))) };
            bool cardArt = art.leafCards != null && art.leafCards.Length > 0; var cards = new ZoneMeshes.Cards();
            int n = Mathf.Max(2, Mathf.RoundToInt(length / .8f)); float step = Mathf.Max(0, length - 1.6f) / (n - 1);
            for (int i = 0; i < n; i++)
            {
                float x = -length / 2 + .8f + i * step, wide = 1.45f + H() * .3f, high = 1.3f + H() * .3f;
                if (length < 1.6f) x = 0;
                Lump(LeafLumpAt((int)(H() * 6)), t, new Vector3(x, high * .46f, (H() - .5f) * .14f), new Vector3(wide, high, 1.1f + H() * .2f), greens[i % 2], H() * 360).name = "Hedge";
                if (i % 2 == 0) continue;
                // Every other lump: a small light one on top, and a spray of leaf leaning out of it.
                Lump(LeafLumpAt((int)(H() * 6)), t, new Vector3(x + (H() - .5f) * .3f, 1.1f + H() * .12f, (H() - .5f) * .3f), new Vector3(.85f + H() * .25f, .6f, .8f + H() * .2f), greens[2], H() * 360).name = "Hedge";
                var along = new Vector3((H() - .5f) * .8f, 1, (H() - .5f) * 1.2f).normalized; var across = Quaternion.AngleAxis(H() * 180, along) * Vector3.Cross(along, Vector3.forward).normalized;
                float size = .6f + H() * .25f; var foot = new Vector3(x + (H() - .5f) * .4f, 1.02f, (H() - .5f) * .5f);
                if (cardArt) cards.AddCross(foot, along, across, size, size * .95f, new Vector3(x, .5f, 0), .3f, new Color(.8f, .86f, .78f) * (.9f + H() * .15f));
            }
            if (cards.Count > 0) MeshPart(cards.Build("Hedge leaves"), t, Vector3.zero, LeafMaterial(art.leafCards[0], new Color(.72f, .8f, .64f)));
            Solid(t, new Vector3(0, .7f, 0), new Vector3(length, 1.4f, 1.2f));
        }
        /// <summary>
        /// A hayrick: a round stack built up in four tiers, each standing a little proud of the one below (a flared skirt, two
        /// shoulders, a cap), in pale thatch whose painted layers run round it, with the stack pole out of its top and loose hay
        /// at its foot. About the size of the old round one (2.6 m across, a little over 2 m high); its collider is unchanged.
        /// Height, girth, turn and the pole's lean come from a stream keyed to where it stands: no zone draw.
        /// </summary>
        void Haystack(Transform t)
        {
            var hr = TreeRandom(t.position); float H() { return (float)hr.NextDouble(); }
            var rick = PropMesh("Hayrick", () => Turned(new[] { new Vector2(1.2f, -.35f), new Vector2(1.27f, .12f), new Vector2(1.16f, .62f), new Vector2(1.24f, .58f), new Vector2(1.04f, 1.12f), new Vector2(1.11f, 1.08f), new Vector2(.74f, 1.6f), new Vector2(.8f, 1.56f), new Vector2(.36f, 1.98f), new Vector2(0, 2.2f) }, 14, 2.5f));
            float girth = .95f + H() * .07f, high = .92f + H() * .16f;
            MeshPart(rick, t, Vector3.zero, Tint(art.thatch, new Color(.84f, .72f, .42f)), Quaternion.Euler(0, H() * 360, 0)).transform.localScale = new Vector3(girth, high, girth);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, 2.2f * high + .12f, 0), new Vector3(.07f, .4f, .07f), Tint(art.timber, new Color(.3f, .21f, .13f)), Quaternion.Euler((H() - .5f) * 10, 0, (H() - .5f) * 10));   // the stack pole
            for (int k = 0; k < 3; k++)
            {
                float a = (k + H() * .7f) * 2.1f, x = Mathf.Cos(a) * 1.25f, z = Mathf.Sin(a) * 1.25f;
                Lump(LeafLumpAt(k), t, new Vector3(x, LocalGround(t, x, z) + .08f, z), new Vector3(.8f + H() * .3f, .34f, .6f + H() * .25f), art.hay, H() * 360).name = "Loose hay";
            }
            Solid(t, new Vector3(0, 1, 0), new Vector3(2.4f, 2, 2.4f));
        }
'''),
(ZB, "the orchard's crown",
r'''                        LeafCrown(t, tr, lean + new Vector3(0, h + .85f, 0), spots, ends, orchardLeaf, bark, Tint(art.foliage, new Color(.19f, .29f, .12f)), 1, new Vector3(1.8f, 1.1f, 1.8f));
''',
r'''                        // Its leaf mass: small fresh-green lumps (the fruit still shows round them) under the cards.
                        LeafCrown(t, tr, lean + new Vector3(0, h + .85f, 0), spots, ends, orchardLeaf, bark, Tint(art.foliage, new Color(.19f, .29f, .12f)), 1, new Vector3(1.5f, .9f, 1.5f), mass: LeafMassOf(0), cardSize: .9f, massSize: .54f);
'''),
(ZB, "the backdrop's broadleaf silhouettes",
r'''                                float len = c * 1.2f, wide = c * 1.1f; var foot = centre - axis * len * .5f;
''',
r'''                                float len = c * 1.2f, wide = c * 1.1f; var foot = centre - axis * len * .5f;
                                // Inside them a leaf-coloured mass, two lumps in the family's mid and deep: a crown with a ragged edge, not cards on a stick.
                                var mass = LeafMassOf(fam);
                                Put(side, crown, mass[1], centre, Quaternion.Euler(0, yaw, 0), new Vector3(c * .8f, c * .6f, c * .8f));
                                Put(side, crown, mass[0], centre + new Vector3(c * .22f, -c * .2f, c * .15f), Quaternion.Euler(0, yaw + 90, 0), new Vector3(c * .62f, c * .46f, c * .62f));
'''),
# ---------------------------------------------------------------- GrassField.cs ----------------------------------------------------------------
(GF, "GrassField's summary",
r'''    /// Instanced grass tufts and wildflowers on open ground (not roads, fields, clearings, water, groves or the unmade
    /// east). Tufts are baked into 24 m cells; only cells near the camera are drawn, via Graphics.DrawMeshInstanced.
    /// No shadows, no colliders: pure dressing. Materials come from ZoneArt (instancing-enabled cutout assets).
''',
r'''    /// Instanced grass tufts and wildflowers on open ground (not roads, fields, clearings, water, groves or the unmade
    /// east). Tufts are baked into 24 m cells; only cells near the camera are drawn, via Graphics.DrawMeshInstanced.
    /// No shadows, no colliders: pure dressing. Materials come from ZoneArt (instancing-enabled cutout assets); the field
    /// draws its own copies of them, which shrink the tufts into the ground over the 20 m before the nearest cell that may
    /// be off (Grass.shader's _FadeFar), so the grass thins out into the turf's paint and no cell is seen switching.
'''),
(GF, "Build's start",
r'''            tuft = TuftMesh();
            var rng = new System.Random(seed);
''',
r'''            tuft = TuftMesh();
            // Its own copies: fading by distance (a cell is drawn while its centre is within DrawDistance + Cell on both axes, so
            // every tuft within DrawDistance + Cell / 2 of the camera is drawn: the fade ends there); a bleached one for tall drifts.
            var own = new Dictionary<Material, Material>(); var bleached = new Dictionary<Material, Material>();
            Material Own(Material m)
            {
                if (!own.TryGetValue(m, out var c)) { own[m] = c = new Material(m) { name = m.name + " (field)", enableInstancing = true }; c.SetFloat("_FadeFar", DrawDistance + Cell / 2); }
                return c;
            }
            Material Bleached(Material m)
            {
                if (!bleached.TryGetValue(m, out var c)) { var k = m.color; float g = k.grayscale; bleached[m] = c = new Material(Own(m)) { name = m.name + " (bleached)", color = Color.Lerp(k, new Color(g * 1.25f, g * 1.2f, g * .75f, k.a), .35f), enableInstancing = true }; }
                return c;
            }
            var rng = new System.Random(seed);
'''),
(GF, "the tuft's material",
r'''                var mat = flower ? flowers[rng.Next(flowers.Length)] : grass[rng.Next(grass.Length)];
''',
r'''                var mat = Own(flower ? flowers[rng.Next(flowers.Length)] : grass[rng.Next(grass.Length)]);
'''),
(GF, 'the tall grass loop',
r'''            // Tall grass patches: dense, knee-to-waist high, enough to hide a crouching wolf.
            if (tall != null)
                foreach (var (center, radius) in tall)
                {
                    int n = Mathf.RoundToInt(Mathf.PI * radius * radius * 5 * tallDensity);
                    for (int i = 0; i < n; i++)
                    {
                        float a = (float)rng.NextDouble() * Mathf.PI * 2, r = Mathf.Sqrt((float)rng.NextDouble()) * radius;
                        var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        if (openness(p) <= 0 && Vector2.Distance(p, center) > radius * .3f) continue;
                        int cx = Mathf.Clamp((int)((p.x + half) / Cell), 0, perAxis - 1), cz = Mathf.Clamp((int)((p.y + half) / Cell), 0, perAxis - 1);
                        var cell = grid[cx, cz];
                        if (cell == null) { cell = grid[cx, cz] = new CellData { center = new Vector3(-half + (cx + .5f) * Cell, 0, -half + (cz + .5f) * Cell) }; cells.Add(cell); }
                        var mat = tallGrass[rng.Next(tallGrass.Length)];
                        float s = 1.25f + (float)rng.NextDouble() * .7f;
                        var m = Matrix4x4.TRS(zone.Ground(p, -.02f), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(s, s * (1.3f + (float)rng.NextDouble() * .5f) * tallHeight, s));
''',
r'''            // Tall grass patches: dense, knee-to-waist high, enough to hide a crouching wolf. A patch is no disc: its rim wobbles in
            // and out by bearing (.78 to 1.15 of the radius), the grass thins and shortens over the outer 40% (to half height at the
            // rim), every sixteenth tuft stands out past the rim, the height rolls a little across the patch and drifts of
            // bleached tufts run through it. The inner 60% is as dense and tall as it was (an ambusher lies within 30%).
            if (tall != null)
                foreach (var (center, radius) in tall)
                {
                    int n = Mathf.RoundToInt(Mathf.PI * radius * radius * 5 * tallDensity);
                    for (int i = 0; i < n; i++)
                    {
                        float a = (float)rng.NextDouble() * Mathf.PI * 2, u = Mathf.Sqrt((float)rng.NextDouble());
                        float rim = radius * (.78f + .37f * Mathf.Clamp01(Mathf.PerlinNoise(center.x * .31f + Mathf.Cos(a) * 1.3f + 40, center.y * .31f + Mathf.Sin(a) * 1.3f + 40)));
                        bool outlier = i % 16 == 7; float r = outlier ? rim * (1.02f + .28f * u) : u * rim;
                        var p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        if (openness(p) <= 0 && Vector2.Distance(p, center) > radius * .3f) continue;
                        float edge = Mathf.Clamp01((r / rim - .6f) / .4f);   // 0 over the inner 60%, 1 at the rim and past it
                        if (!outlier && Hash(p) < edge * .7f) continue;       // thinner toward the rim
                        int cx = Mathf.Clamp((int)((p.x + half) / Cell), 0, perAxis - 1), cz = Mathf.Clamp((int)((p.y + half) / Cell), 0, perAxis - 1);
                        var cell = grid[cx, cz];
                        if (cell == null) { cell = grid[cx, cz] = new CellData { center = new Vector3(-half + (cx + .5f) * Cell, 0, -half + (cz + .5f) * Cell) }; cells.Add(cell); }
                        var pick = tallGrass[rng.Next(tallGrass.Length)];
                        var mat = Mathf.PerlinNoise(p.x * .3f + 17, p.y * .3f + 3) > .62f ? Bleached(pick) : Own(pick);
                        float s = (1.25f + (float)rng.NextDouble() * .7f) * Mathf.Lerp(1, .8f, edge);
                        float roll = (.9f + .3f * Mathf.Clamp01(Mathf.PerlinNoise(p.x * .22f + 9, p.y * .22f + 31))) * Mathf.Lerp(1, .5f, edge);
                        var m = Matrix4x4.TRS(zone.Ground(p, -.02f), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(s, s * (1.3f + (float)rng.NextDouble() * .5f) * tallHeight * roll, s));
'''),
(GF, 'TuftMesh (the hash goes before it)',
r'''        /// <summary>Three crossed quads, ~0.6 m tall, with UVs spanning the blade texture.</summary>
''',
r'''        /// <summary>A steady 0..1 from a spot on the ground (to 6 cm), for a choice that must not draw from a stream.</summary>
        static float Hash(Vector2 p)
        {
            uint h = (uint)(Mathf.RoundToInt(p.x * 16) * 73856093) ^ (uint)(Mathf.RoundToInt(p.y * 16) * 19349663);
            h ^= h >> 15; h *= 2654435761u; h ^= h >> 13; return (h & 0xffffff) / 16777216f;
        }
        /// <summary>Three crossed quads, ~0.6 m tall, with UVs spanning the blade texture.</summary>
'''),
# ---------------------------------------------------------------- Grass.shader ----------------------------------------------------------------
(GS, 'the _FadeFar property',
r'''        _Wither ("Wither (drains the paint's green)", Range(0, 1)) = 0
''',
r'''        _Wither ("Wither (drains the paint's green)", Range(0, 1)) = 0
        _FadeFar ("Fade end in metres from the camera (0: no fade)", Float) = 0
'''),
(GS, 'the _FadeFar variable',
r'''        float _Wind, _WindSpeed, _Glow, _Wither;
''',
r'''        float _Wind, _WindSpeed, _Glow, _Wither;
        // GrassField's tufts (its own material copies set this): a tuft shrinks into the ground over the 20 m before _FadeFar, so
        // the field thins out into the turf's paint instead of whole cells switching. 0 (plants, the assets): no fade.
        float _FadeFar;
'''),
(GS, 'the fade in vert',
r'''            v.vertex.xyz += normalize(down + float3(1e-5, 0, 0)) * (_Wind * w * (0.9 + 2.1 * wave) * tip) + float3(0, -0.12, 0) * (w * wave * tip);
        }
''',
r'''            v.vertex.xyz += normalize(down + float3(1e-5, 0, 0)) * (_Wind * w * (0.9 + 2.1 * wave) * tip) + float3(0, -0.12, 0) * (w * wave * tip);
            // The distance fade: the tuft's root is the object's origin, so scaling the vertex draws it down into its root.
            float fade = _FadeFar > 0 ? saturate((_FadeFar - distance(wp.xz, _WorldSpaceCameraPos.xz)) * 0.05) : 1;
            v.vertex.xyz *= fade;
        }
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
    for full, text in files.items():   # a tree this patch has already been applied to
        for mark in ('LeafLumpAt', '_FadeFar'):
            if mark in text: problems.append('%s: already has %s' % (full, mark))
    if problems:
        sys.exit('patch_green: NOTHING WRITTEN.\n  ' + '\n  '.join(problems))
    for full, text in out.items():
        io.open(full, 'w', encoding='utf-8', newline='').write(text)
        print('patched %s (%d edits)' % (full, sum(1 for e in EDITS if os.path.join(ROOT, e[0]) == full)))
    print('patch_green: OK')

if __name__ == '__main__':
    main()
