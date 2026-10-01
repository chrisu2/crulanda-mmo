# Painted pass, visual review item 8 (the Ash Rim): the ground no longer reads as paving, the ash falls as soft pale
# flakes, and the zone has a value structure (pale drifts, mid plates, dark scorched ground, a paler distance).
# - ZoneBuilder.PaintGround, ash branch: uneven plates (a warped Voronoi with most edges left out), pale drifted ash over
#   them and in their joints, a few broken crack runs, scorched patches, a broad value drift, licked-smooth landmark ground.
# - ZoneBuilder.AshDetail: powdery grain and a few fading hairlines instead of a net of 0.6 m plates (the paving).
# - ZoneLabel.ground ("licked") and the Unwoven Flats in ashrim.json; ashrim.json lighting (fog start, a cooler air).
# - NatureFx.FallingLeaves, ash: a soft torn flake sprite made at load, small sizes, colours taken from the fog each second
#   (never black against the sky), more and faster ash in a squall.
# - WorldWeather: the ash squall closes the fog further and its dust is thicker and more dun (two lines).
# Usage: patch_ash.py "<root>"   (default: D:\code\mmo\New Unity Project\Assets\Crulanda)
# Anchor based: every old block must occur exactly once. All checks run before any write; a second run fails without writing.
import json, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
BUILDER = r'Scripts\World\ZoneBuilder.cs'
DEFINITION = r'Scripts\World\ZoneDefinition.cs'
NATURE = r'Scripts\World\NatureFx.cs'
WEATHER = r'Scripts\World\WorldWeather.cs'
ASHRIM = r'EncounterContent\Zones\ashrim.json'

# (file, old, new). Written with LF; a file that uses CRLF throughout (NatureFx.cs) is matched and written with CRLF.
EDITS = [
    # ---- ZoneBuilder.BuildGround: the ash detail is grain now, and licked ground takes little of it ----
    (BUILDER,
     '''                m.SetTexture("_DetailAlbedoMap", AshDetail(Zone.seed)); m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 5);   // fine crazing, sharp up close
                m.SetTexture("_DetailMask", DetailMask(256, (x, z) => 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .95f, Unmade(x, z)))));   // none on the unmade
''',
     '''                m.SetTexture("_DetailAlbedoMap", AshDetail(Zone.seed)); m.SetTextureScale("_DetailAlbedoMap", Vector2.one * Zone.size / 5);   // powdery grain and a few hairlines, sharp up close
                m.SetTexture("_DetailMask", DetailMask(256, (x, z) => (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .95f, Unmade(x, z)))) * (1 - .75f * Licked(x, z))));   // none on the unmade, little on licked ground
'''),
    # ---- ZoneBuilder.PaintGround: what the Voronoi is for ----
    (BUILDER,
     '''            // Ash crust: petrified-ash plates split by angular cracks, a Voronoi network with one jittered site per 5 m cell
            // (sx/sz in cell units, three cells of margin), plus each site's 8 bisectors with its neighbours (unit normal bx/bz,
            // offset bo, neighbour bn), so a pixel's distance to its nearest crack is 8 dot products.
''',
     '''            // Ash crust: petrified-ash plates, a Voronoi network with one jittered site per 5 m cell (sx/sz in cell units, three
            // cells of margin), plus each site's 8 bisectors with its neighbours (unit normal bx/bz, offset bo, neighbour bn), so
            // a pixel's distance to its nearest plate edge is 8 dot products. The ash branch below decides which edges show.
'''),
    # ---- ZoneBuilder.PaintGround, ash branch ----
    (BUILDER,
     '''                        // Ashland: pale grey petrified ash in plates a shade apart, split by angular cracks (Voronoi cell edges, some
                        // left faint so the network looks broken, not paved), and the odd copper-rust stain (canon: the dust tastes of
                        // copper). The edges bend a little. The paint is magnified up close, where a texel-wide line blurs into a soft
                        // dark band: so these lines are thin and faint, and the crisp crazing detail carries the cracks near the camera.
                        float u = (x + half) / plate + 3 + (n2 - .5f) * .1f + (n3 - .5f) * .03f;
                        float v = (z + half) / plate + 3 + (Mathf.PerlinNoise(x * .45f + 31, z * .45f + 17) - .5f) * .1f + (n3 - .5f) * .03f;
''',
     '''                        // Ashland: a crust of petrified ash in plates of uneven size, soft pale ash drifted over it in long tongues
                        // and lying in its joints, scorched ground showing dark between, and the odd copper-rust stain (canon: the
                        // dust tastes of copper). The plates are Voronoi cells in a warped field (so their sizes differ); six edges in ten are left
                        // out, so cells join into larger plates whose tones run into each other. The rest are joints of pale ash, a
                        // few with a dark crack along them, and both fade in and out along their run: broken runs, never a net.
                        // Slopes shed the drift. Licked ground (the Unwoven Flats) has neither plate, joint nor crack. Values: drifts
                        // pale, plates mid, scorched ground and cracks dark, and the whole a shade lighter or darker across 80 m.
                        float u = (x + half) / plate + 3 + (Mathf.PerlinNoise(x * .05f + 91, z * .05f + 13) - .5f) * .7f + (n2 - .5f) * .1f + (n3 - .5f) * .03f;
                        float v = (z + half) / plate + 3 + (Mathf.PerlinNoise(x * .05f + 37, z * .05f + 59) - .5f) * .7f + (Mathf.PerlinNoise(x * .45f + 31, z * .45f + 17) - .5f) * .1f + (n3 - .5f) * .03f;
'''),
    (BUILDER,
     '''                        float line = Mathf.Clamp01((.03f + .04f * n1 - edge * plate) / texel + .5f) * (pair >> 24 < 64 ? .35f : 1);
                        Color ashC = Color.Lerp(new Color(.46f, .46f, .47f), new Color(.60f, .59f, .59f), n1) * (.97f + .06f * Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1));
                        ashC = Color.Lerp(ashC, new Color(.52f, .44f, .40f), Mathf.Clamp01((Mathf.PerlinNoise(x * .02f + 60, z * .02f) - .66f) * 2.2f));
                        c = Color.Lerp(ashC, new Color(.17f, .165f, .165f), line * .24f) * (.93f + n3 * .1f);
''',
     '''                        int joint = (int)(pair >> 24);   // under 150: no joint (the two cells are one plate); under 195: a joint of drifted ash; above: a crack as well
                        float d = edge * plate, lick = Licked(x, z), steep = Mathf.Clamp01((1 - UpAt(x, z) - .02f) * 9);
                        // A plate's tone runs into its neighbour's across the edge, so joined cells shade into each other softly.
                        float tone = Mathf.Lerp(Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1), Mathf.Repeat(sx[other] * 7.31f + sz[other] * 3.17f, 1), .5f * (1 - Mathf.SmoothStep(0, 1, d / .6f)));
                        // Drifts lie in long soft tongues, all drawn out the same way (la along them, lb across), feathered at the rim.
                        float la = x * .8f + z * .6f, lb = z * .8f - x * .6f;
                        float drift = Mathf.SmoothStep(0, 1, (Mathf.PerlinNoise(la * .03f + 140, lb * .085f + 71) * .7f + Mathf.PerlinNoise(la * .09f + 19, lb * .22f + 47) * .22f + n2 * .08f - .47f) * 3.4f) * (1 - steep);
                        float scorch = Mathf.SmoothStep(0, 1, (Mathf.PerlinNoise(x * .028f + 210, z * .028f + 33) + (n2 - .5f) * .1f - .56f) * 3.2f);
                        float seam = joint < 150 ? 0 : (1 - Mathf.SmoothStep(0, 1, d / (.55f + .4f * n1))) * Mathf.Clamp01((Mathf.PerlinNoise(x * .19f + 9, z * .19f + 77) - .3f) * 2.4f);
                        float runs = Mathf.Clamp01((Mathf.PerlinNoise(x * .035f + 7, z * .035f + 83) - .4f) * 4) * Mathf.Clamp01((Mathf.PerlinNoise(x * .23f + 51, z * .23f + 5) - .32f) * 2.6f);
                        float line = joint < 195 ? 0 : Mathf.Clamp01((.05f + .05f * n1 - d) / texel + .5f) * runs * (1 - drift);
                        Color crustC = Color.Lerp(new Color(.36f, .36f, .375f), new Color(.5f, .495f, .5f), Mathf.Clamp01(n1 * .45f + tone * .55f));
                        crustC = Color.Lerp(crustC, new Color(.52f, .44f, .40f), Mathf.Clamp01((Mathf.PerlinNoise(x * .02f + 60, z * .02f) - .66f) * 2.2f));
                        crustC = Color.Lerp(crustC, Color.Lerp(new Color(.25f, .245f, .245f), new Color(.31f, .3f, .295f), n2), scorch * .75f);
                        crustC = Color.Lerp(crustC, new Color(.15f, .145f, .145f), line * .34f);
                        Color driftC = Color.Lerp(new Color(.57f, .565f, .57f), new Color(.66f, .655f, .645f), n2);
                        c = Color.Lerp(crustC, driftC, Mathf.Max(drift, seam * .36f) * (1 - .5f * scorch));
                        // Licked: one smooth cold grey with faint long streaks, a darker lip where the crust breaks off around it.
                        if (lick > 0) c = Color.Lerp(c, Color.Lerp(new Color(.52f, .52f, .535f), new Color(.565f, .565f, .58f), Mathf.PerlinNoise(x * .05f + 41, z * .5f + 3)), lick) * (1 - .1f * Mathf.Clamp01(1 - Mathf.Abs(lick - .5f) * 2.5f));
                        c *= (.9f + .2f * Mathf.PerlinNoise(x * .012f + 300, z * .012f + 17)) * (.95f + n3 * .08f);
'''),
    # ---- ZoneBuilder.AshDetail: grain, not plates; the tiling noise and the licked ground it and the paint ask about ----
    (BUILDER,
     '''        /// Ash ground detail (x2 over the paint, 5 m repeat): fine angular crazing, the edges of a tileable Voronoi of 8x8
        /// plates (~.6 m), some left faint, over powdery grain; averages mid-grey. Sharp up close, where the paint blurs.
        /// </summary>
        static Texture2D AshDetail(int seed)
        {
            const int n = 512, cells = 8; float per = (float)n / cells; var rnd = new System.Random(seed + 31);
            var sx = new float[cells * cells]; var sz = new float[sx.Length]; var tone = new float[sx.Length];
            for (int k = 0; k < sx.Length; k++) { sx[k] = .15f + .7f * (float)rnd.NextDouble(); sz[k] = .15f + .7f * (float)rnd.NextDouble(); tone[k] = (float)rnd.NextDouble() - .5f; }
''',
     '''        /// Ash ground detail (x2 over the paint, 5 m repeat): powdery grain in four sizes (3 cm to 1 m, tiling noise) and a few
        /// hairline crazes, short runs along the edges of a tileable Voronoi of 8x8 cells that fade out along their length.
        /// No closed plates: at this size they read as paving. Averages mid-grey. Sharp up close, where the paint blurs.
        /// </summary>
        static Texture2D AshDetail(int seed)
        {
            const int n = 512, cells = 8; float per = (float)n / cells; var rnd = new System.Random(seed + 31);
            var sx = new float[cells * cells]; var sz = new float[sx.Length];
            for (int k = 0; k < sx.Length; k++) { sx[k] = .15f + .7f * (float)rnd.NextDouble(); sz[k] = .15f + .7f * (float)rnd.NextDouble(); }
'''),
    (BUILDER,
     '''                    float g = .52f + tone[near] * .04f + ((h & 1023) / 1023f - .5f) * .07f;
                    g = Mathf.Lerp(g, .34f, Mathf.Clamp01(1.7f - edge * per) * (pair >> 24 < 80 ? .35f : 1));   // ~2.5 px (2.5 cm) line
                    byte c = (byte)(Mathf.Clamp01(g) * 255); px[y * n + x] = new Color32(c, c, c, 255);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true) { name = "Ash crazing", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px); tex.Apply(true, true); return tex;
        }
''',
     '''                    float s = (x + .5f) / n, t = (y + .5f) / n; int craze = (int)(pair >> 24);
                    float g = .51f + ((h & 1023) / 1023f - .5f) * .05f + (TileNoise(s * 160, t * 160, 160, 1) - .5f) * .09f + (TileNoise(s * 48, t * 48, 48, 2) - .5f) * .1f
                        + (TileNoise(s * 14, t * 14, 14, 3) - .5f) * .1f + (TileNoise(s * 5, t * 5, 5, 4) - .5f) * .06f;
                    // One edge in four is a hairline (~1.5 cm), and only where the run noise lets it show: it fades out along its length.
                    g = Mathf.Lerp(g, .36f, Mathf.Clamp01(1.3f - edge * per) * (craze < 190 ? 0 : craze < 232 ? .25f : .5f) * Mathf.Clamp01((TileNoise(s * 6, t * 6, 6, 5) - .45f) * 4));
                    byte c = (byte)(Mathf.Clamp01(g) * 255); px[y * n + x] = new Color32(c, c, c, 255);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true) { name = "Ash grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px); tex.Apply(true, true); return tex;
        }
        /// <summary>Smooth value noise, 0..1, that repeats every <paramref name="period"/> lattice cells (for textures that tile).</summary>
        static float TileNoise(float u, float v, int period, uint salt)
        {
            int x0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(v); float tx = u - x0, ty = v - y0; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float At(int a, int b)
            {
                uint h = (uint)((a % period + period) % period) * 374761393u + (uint)((b % period + period) % period) * 668265263u + salt * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 1023) / 1023f;
            }
            return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
        }
        /// <summary>
        /// How far the ground at a point is licked smooth (0..1): inside a landmark whose ground is "licked" (the Unwoven Flats),
        /// right across its inner two thirds and gone by its radius, with a wandering rim. Paint only: the land's shape, the
        /// colliders and the navmesh do not ask.
        /// </summary>
        float Licked(float x, float z)
        {
            var places = lickedPlaces ?? (lickedPlaces = Zone.landmarks.Where(l => l != null && l.ground == "licked").ToArray());   // PaintGround's rows ask in parallel: built once, read only
            float best = 0;
            foreach (var l in places)
            {
                float reach = Mathf.Max(1, l.radius), dx = x - l.at.x, dz = z - l.at.y; if (dx * dx + dz * dz > reach * reach * 1.7f) continue;
                float d = Mathf.Sqrt(dx * dx + dz * dz) + (Mathf.PerlinNoise(x * .09f + 5, z * .09f + 61) - .5f) * reach * .3f;
                best = Mathf.Max(best, 1 - Mathf.SmoothStep(0, 1, (d - reach * .68f) / (reach * .3f)));
            }
            return best;
        }
        ZoneLabel[] lickedPlaces;
'''),
    # ---- ZoneDefinition: a landmark can say what its ground is ----
    (DEFINITION,
     '''    /// (unset: from its south-west); viewPitch and viewZoom set that shot's camera (0: 17 degrees, 11 m).
''',
     '''    /// (unset: from its south-west); viewPitch and viewZoom set that shot's camera (0: 17 degrees, 11 m). ground (optional):
    /// "licked" paints the place's ground smooth and uncracked in an ash zone (the Unwoven Flats); paint only.
'''),
    (DEFINITION,
     '''public Vector2 view; public float viewPitch, viewZoom; }''',
     '''public Vector2 view; public float viewPitch, viewZoom; public string ground; }'''),
    # ---- NatureFx.FallingLeaves: soft pale ash ----
    (NATURE,
     '''    /// Leaves drifting down around the player wherever broadleaf trees stand (more under denser woods). Ash zones get
    /// slow grey ash flakes everywhere instead. One particle system follows the camera's focus; emission is retuned
    /// once a second from the number of broadleaf trees nearby.
''',
     '''    /// Leaves drifting down around the player wherever broadleaf trees stand (more under denser woods). Ash zones get
    /// slow ash flakes everywhere instead: small, soft and torn, mostly a little paler than the air and a few a shade
    /// darker (their colours come from the fog, hour by hour), thicker and faster in a squall. One particle system follows
    /// the camera's focus; emission is retuned once a second from the number of broadleaf trees nearby.
'''),
    (NATURE,
     '''            if (mat == null) { enabled = false; return; }
            ps = new GameObject(ash ? "Falling ash" : "Falling leaves").AddComponent<ParticleSystem>();
''',
     '''            if (mat == null) { enabled = false; return; }
            if (ash) mat = new Material(mat) { name = "Ash flake (soft)", mainTexture = FlakeTexture() };   // the art's flake has a hard rim: at a few pixels it drew as a dot
            ps = new GameObject(ash ? "Falling ash" : "Falling leaves").AddComponent<ParticleSystem>();
'''),
    (NATURE,
     '''            // Ash: bigger, slower flakes drifting on the wind through the whole air column (small grey ones vanished in the haze).
''',
     '''            // Ash: slow flakes drifting on the wind through the whole air column, small and of many sizes (at one size they read as dots).
'''),
    (NATURE,
     '''main.startSize = ash ? new ParticleSystem.MinMaxCurve(.1f, .22f) : new ParticleSystem.MinMaxCurve(.12f, .22f);''',
     '''main.startSize = ash ? new ParticleSystem.MinMaxCurve(.03f, .12f) : new ParticleSystem.MinMaxCurve(.12f, .22f);'''),
    (NATURE,
     '''main.maxParticles = ash ? 1200 : 700;''',
     '''main.maxParticles = ash ? 4000 : 700;'''),
    (NATURE,
     '''            // Ash: mostly pale flakes (they show against the ground and trees), a quarter charred flecks (against the sky and haze).
            if (ash) colors.SetKeys(new[] { new GradientColorKey(new Color(.92f, .91f, .9f), 0), new GradientColorKey(new Color(.78f, .77f, .77f), .6f), new GradientColorKey(new Color(.24f, .23f, .23f), .75f), new GradientColorKey(new Color(.2f, .19f, .19f), 1) }, new[] { new GradientAlphaKey(.95f, 0), new GradientAlphaKey(.95f, 1) });
            else if''',
     '''            // Ash: its colours come from the air (AshColours, kept up in Update).
            if (ash) colors = AshColours();
            else if'''),
    (NATURE,
     '''            if (ash) r.maxParticleSize = .011f;   // a flake passing the lens stays a small flake (~10 px), not a soft blot
''',
     '''            if (ash) r.maxParticleSize = .009f;   // a flake passing the lens stays a small soft flake (~8 px), not a blot
'''),
    (NATURE,
     '''            ps.Play();
        }
        void Update()
        {
            if (ps == null) return;
''',
     '''            ps.Play();
        }
        /// <summary>
        /// Ash flake colours, taken from the air (the fog at this hour, half greyed): most a little paler than it, about one in
        /// six a shade darker, all half transparent. So a flake is never a black or white dot against the sky or the haze, by
        /// day or by night.
        /// </summary>
        static Gradient AshColours()
        {
            var fog = RenderSettings.fogColor; float g = fog.grayscale; var air = Color.Lerp(fog, new Color(g, g, g), .5f);
            Color pale = air * 1.28f, mid = air * 1.1f, dark = air * .72f; var colours = new Gradient();
            colours.SetKeys(new[] { new GradientColorKey(pale, 0), new GradientColorKey(mid, .7f), new GradientColorKey(dark, .86f), new GradientColorKey(dark, 1) },
                new[] { new GradientAlphaKey(.62f, 0), new GradientAlphaKey(.5f, .7f), new GradientAlphaKey(.42f, 1) });
            return colours;
        }
        /// <summary>A soft, torn ash flake: a flattened blot with a ragged outline that fades to nothing at its rim (built once per zone at load).</summary>
        static Texture2D FlakeTexture()
        {
            const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Ash flake (soft)", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0, i = 0; y < n; y++)
                for (int x = 0; x < n; x++, i++)
                {
                    float u = (x + .5f) / n * 2 - 1, v = (y + .5f) / n * 2 - 1;
                    float r = Mathf.Sqrt(u * u + v * v * 2.2f) + (Mathf.PerlinNoise(x * .21f + 3, y * .21f + 8) - .5f) * .3f;
                    float a = Mathf.Clamp01(1 - r / .85f); px[i] = new Color32(255, 255, 255, (byte)(a * a * (3 - 2 * a) * 255));
                }
            t.SetPixels32(px); t.Apply(true, true); return t;
        }
        void Update()
        {
            if (ps == null) return;
'''),
    (NATURE,
     '''{ ps.transform.position = at; ps.Clear(); ps.Emit(700); filled = true; }''',
     '''{ ps.transform.position = at; ps.Clear(); ps.Emit(900); filled = true; }'''),
    (NATURE,
     '''var wind = WorldWeather.WindDirection * (weather.wind * (ash ? 4.5f : 3f));''',
     '''var wind = WorldWeather.WindDirection * (weather.wind * (ash ? 4.5f + 2.5f * weather.ash : 3f));'''),
    (NATURE,
     '''            if (ash) { emission.rateOverTime = 60 + 260 * weather.ash; return; }
''',
     '''            if (ash)
            {
                // A squall is visibly more ash, not the same flakes; and the flakes keep to the air's value as the hour and the weather turn.
                emission.rateOverTime = 70 + 420 * weather.ash;
                var main = ps.main; main.startColor = new ParticleSystem.MinMaxGradient(AshColours()) { mode = ParticleSystemGradientMode.RandomColor };
                return;
            }
'''),
    # ---- WorldWeather: the ash squall is not an overcast (the tests' fog floor is .5) ----
    (WEATHER,
     '''case WeatherKind.AshSquall: return new WeatherLook { clouds = .88f, dim = .52f, ash = 1, wind = .92f, fog = .72f };''',
     '''case WeatherKind.AshSquall: return new WeatherLook { clouds = .88f, dim = .58f, ash = 1, wind = .92f, fog = .56f };'''),
    (WEATHER,
     '''            tint = dust ? Color.Lerp(fog, new Color(g * 1.08f, g, g * .9f), .6f) : Color.Lerp(fog, Color.white, Mathf.Lerp(.3f, .1f, dark));
            float amount = Mathf.Max(mist, ash); tint.a = (dust ? .22f : .34f) * amount;
''',
     '''            tint = dust ? Color.Lerp(fog, new Color(g * 1.08f, g, g * .9f), .8f) : Color.Lerp(fog, Color.white, Mathf.Lerp(.3f, .1f, dark));
            float amount = Mathf.Max(mist, ash); tint.a = (dust ? .3f : .34f) * amount;   // a squall's dust is thick and dun
'''),
]

# The Ash Rim's air (lighting block only): the fog starts further off, so the near ground keeps its own darker values against
# a paler distance, and the air and the skylight are a little cooler (violet) than the sunlit ash. (key, old, new)
LIGHTING = [
    ('fogColor', '#A3A1A8', '#A4A1AE'),
    ('fogStart', 22, 32),
    ('ambientSky', '#72717C', '#6F6E82'),
]
LICKED = 'The Unwoven Flats'
MARKS = {BUILDER: 'Licked(', DEFINITION: 'public string ground;', NATURE: 'AshColours', WEATHER: "a squall's dust is thick and dun"}


def fail(msg):
    print('FAILED: ' + msg + ' (nothing written)')
    sys.exit(1)


def dump(d):
    return (json.dumps(d, indent=2, ensure_ascii=False) + '\n').encode('utf-8')


def main():
    texts, out, crlf = {}, {}, {}
    for rel in sorted(set(e[0] for e in EDITS)):
        path = os.path.join(ROOT, rel)
        if not os.path.isfile(path): fail('missing ' + path)
        raw = open(path, 'rb').read()
        crlf[rel] = b'\r' in raw
        if crlf[rel] and raw.count(b'\r\n') != raw.count(b'\n') or raw.count(b'\r\n') != raw.count(b'\r'): fail(rel + ': mixed line endings')
        texts[rel] = out[rel] = raw.decode('utf-8')
        if MARKS[rel] in texts[rel]: fail(rel + ': already patched')
    for i, (rel, old, new) in enumerate(EDITS):
        if '\r' in old or '\r' in new: fail('edit %d carries a CR' % i)
        if crlf[rel]: old, new = old.replace('\n', '\r\n'), new.replace('\n', '\r\n')
        if old == new: fail('edit %d is empty' % i)
        n = texts[rel].count(old)
        if n != 1: fail('%s: edit %d anchor occurs %d times: %r' % (rel, i, n, old[:90]))
        if out[rel].count(old) != 1: fail('%s: edit %d overlaps an earlier edit' % (rel, i))
        out[rel] = out[rel].replace(old, new)
    for rel in out:
        if MARKS[rel] not in out[rel]: fail(rel + ': the patch mark is missing from the result')
        if '\u2014' in out[rel]: fail(rel + ': em-dash')
    jpath = os.path.join(ROOT, ASHRIM)
    if not os.path.isfile(jpath): fail('missing ' + jpath)
    jraw = open(jpath, 'rb').read()
    zone = json.loads(jraw.decode('utf-8'))
    if dump(zone) != jraw: fail('ashrim.json does not round-trip byte for byte')
    light = zone.get('lighting')
    if not isinstance(light, dict): fail('ashrim.json: no lighting block')
    for key, old, new in LIGHTING:
        if key not in light or light[key] != old or type(light[key]) is not type(old): fail('ashrim.json lighting.%s is %r, expected %r' % (key, light.get(key), old))
    places = [l for l in zone.get('landmarks', []) if isinstance(l, dict) and l.get('name') == LICKED]
    if len(places) != 1: fail('ashrim.json: %d landmarks named %r' % (len(places), LICKED))
    if any('ground' in l for l in zone['landmarks']): fail('ashrim.json: a landmark already has a ground (already patched)')
    for key, old, new in LIGHTING: light[key] = new
    places[0]['ground'] = 'licked'   # appended after the landmark's last field
    jnew = dump(zone)
    if json.loads(jnew.decode('utf-8')) != zone: fail('ashrim.json: the result does not parse back')
    # Only the three lighting lines change in place; the landmark gains one line and a comma on the line before it.
    a, b = jraw.split(b'\n'), jnew.split(b'\n')
    at = b.index(b'      "ground": "licked"') if b.count(b'      "ground": "licked"') == 1 else -1
    if at < 1 or len(b) != len(a) + 1: fail('ashrim.json: the landmark edit is not one added line')
    b2 = b[:at] + b[at + 1:]; b2[at - 1] = b2[at - 1][:-1] if b2[at - 1].endswith(b',') else b2[at - 1]
    if sum(1 for x, y in zip(a, b2) if x != y) != len(LIGHTING): fail('ashrim.json: the edit touched more than the lighting lines and the landmark')
    # All checks passed: write.
    for rel in sorted(out):
        with open(os.path.join(ROOT, rel), 'wb') as f: f.write(out[rel].encode('utf-8'))
        print('patched ' + rel)
    with open(jpath, 'wb') as f: f.write(jnew)
    print('patched ' + ASHRIM)
    print('OK: %d code edits, %d lighting fields, 1 landmark ground' % (len(EDITS), len(LIGHTING)))


main()
