# check:colour

Colour pass for the four older zones, proposed from code and the six captures; nothing was built or run, so every value needs one capture tour before commit.

Measured from the captures (mean RGB, mean saturation):
- Oakhaven: sky is near-grey (156,161,155, sat .06); land is olive-sepia (119,108,61). The beige fog #A39F91, the olive grassA and the warm grade tint stack into a dusty yellow cast.
- Verdant (the target look): land (93,116,71), sat .50.
- Peaks: whole frame sat .13; grey fog swallows the mid-distance.
- Khaven: lower frame is dark (median lum .22) and everything is washed one mauve.
- Ash Rim: sat .06 (correct), but the ground's value range is flat (lum .26-.42).

Approach: data-only lighting edits in the four zone JSONs, four Grade lines in ZonePost.cs, and three small ZoneBuilder.cs edits (a meadow ground block, alpine turf, grass-tuft tints), plus one optional ash plate-variation line. Oakhaven and Peaks get a blue sky, cool fog against a warm sun, and higher saturation (1.28 and 1.2, both under Verdant's 1.3). Khaven and Ash Rim keep hue and saturation and gain contrast through darker, cooler ambient and a steeper grade.

What I would NOT change:
- sunPitch, sunYaw, sunHigh: shadow direction and Khaven's permanent evening are mood and gameplay-readable.
- WorldClock dawn/dusk/night Looks, and ZonePost's night saturation (.62) and dusk tint: shared by all zones including Verdant.
- Verdant's lighting and grade: it is the reference.
- Peaks sunIntensity 1.2, grade exposure .95, and the rock/scree colours (ZoneBuilder.cs lines 496-497): tuned against near-white crags (code comment aims for 110-130 of 255).
- Khaven ground, dirt, rut and dry-grass colours: tuned so roads read under the rose light; nothing green by design.
- Ash base greys, rust stain, fog colour and saturation .7: canon grey with a violet cast; an earlier sepia tint turned it desert tan.
- The baked art.grass and flower colours in ZoneSceneBuilder.cs (lines 173, 175): the assets are generated only when missing and shared by every zone, so tint at runtime per biome instead.
- The default Grade arm stays the meadow arm; no new biome case.
- Fog distances in Khaven: the closed-in view is the mood.

Checks:
- WeatherTests asserts daytime ambientSky grayscale above .3 under all weather. The darkest proposed is Khaven #62608E at about .40 (about .375 under rain), so it passes on paper.
- The minimap is rendered in daylight from the same paint, so Oakhaven's and the Peaks' maps turn greener.
- The painted draft (ZoneSceneBuilder.Painted.cs) has no matches for sky, grass, _Color or saturation settings, so nothing here should collide with it.

Scratch: C:\Users\chris\AppData\Local\Temp\claude\D--code-mmo\be58a4b3-4c77-45ef-9182-17eb205878fc\scratchpad\painted-review\colour\s.py (capture statistics).

## [high] Oakhaven lighting: grey sky and beige fog give a dusty sepia cast
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Zones\oakhaven.json, "lighting" block (lines 45-56)
Problem: There is no skyTint/skyExposure/skyHaze, so the shared skybox applies (tint .56,.58,.62, thickness 1.15), which renders near-grey (capture sky 156,161,155, sat .06). Fog #A39F91 is beige, so distance fades to tan and there is no warm/cool separation against the warm sun. Ambient is grey-brown, so shadows are muddy.
Fix:
Replace the block's colour fields (keep sunPitch 27, sunYaw -38):
"sunIntensity": 1.15,
"sunColor": "#FFE1B4",
"ambientSky": "#8AA3C4",
"ambientEquator": "#86886E",
"ambientGround": "#3E4030",
"fogColor": "#AFC0C6",
"fogStart": 70,
"fogEnd": 240,
"skyTint": "#788FB8",
"skyExposure": 1.2,
"skyHaze": 1.0
A higher blue channel in _SkyTint and thickness 1.0 (instead of 1.15) give a clear blue zenith with a pale horizon that meets the cool fog. Blue skylight plus green bounce gives coloured shadows. If the horizon looks too white in the capture, drop skyExposure to 1.1 before touching the tint.

## [high] Meadow grade: lift saturation toward Verdant, drop the yellow tint
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZonePost.cs line 37 (default arm of ForBiome)
Problem: Tint (1.03, 1, .95) removes 5% blue from the whole frame on top of beige fog and olive ground. Saturation 1.18 is well under Verdant's 1.3.
Fix:
Current:
default: return new Grade { saturation = 1.18f, contrast = 1.14f, exposure = 1f, vignette = .6f, bloom = .55f, threshold = 1f, tint = new Color(1.03f, 1, .95f) };
Replace with:
default: return new Grade { saturation = 1.28f, contrast = 1.12f, exposure = 1.03f, vignette = .55f, bloom = .62f, threshold = .95f, tint = new Color(1.02f, 1.01f, .98f) };   // Oakhaven: a clear pastoral day, warm light and cool distance

## [high] Meadow ground paint is olive-brown; add a meadow-only palette
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs, PaintGround, after line 441
Problem: grassA (.30,.38,.20) and grassB (.42,.43,.22) are olive/khaki (capture ground 119,108,61). The defaults at lines 430-431 are also the base for other biomes (ash roads use the default dirt; mud, soil, furrow and stubble are shared), so editing them in place has side effects.
Fix:
Leave lines 430-432 unchanged. Insert after the line
if (Zone.biome == "mountain") { dirt = new Color(.4f, .35f, .28f); rut = new Color(.31f, .27f, .21f); }   // yards and roads a shade darker in the hard light
this new line:
if (Zone.biome == "meadow") { grassA = new Color(.22f, .40f, .16f); grassB = new Color(.35f, .47f, .19f); grassC = new Color(.50f, .44f, .20f); dirt = new Color(.49f, .39f, .25f); rut = new Color(.36f, .28f, .19f); }   // Oakhaven: fresh pasture green with late-summer gold patches; warm trodden earth
grassC stays a golden dry patch (late summer; the autumn trees stay). Soil, furrow, stubble and mud are unchanged.

## [medium] Grass tufts: per-biome runtime tint for meadow and mountain
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs line 76
Problem: Tufts use the baked art.grass tints (.42,.52,.26 / .52,.54,.28 / .36,.45,.24), which are yellow-olive and will clash with a greener meadow ground. In the Peaks they read as yellow-green against grey. The baked materials are shared and only generated when missing, so the tint must be applied at runtime, as Verdant already does.
Fix:
Current:
var lush = Zone.biome == "verdant" ? art.grass.Select(m => new Material(m) { name = m.name + " (lush)", color = Color.Lerp(m.color, new Color(.3f, .62f, .22f), .55f), enableInstancing = true }).ToArray() : grass;
Replace with:
var lush = Zone.biome == "verdant" ? art.grass.Select(m => new Material(m) { name = m.name + " (lush)", color = Color.Lerp(m.color, new Color(.3f, .62f, .22f), .55f), enableInstancing = true }).ToArray()
    : Zone.biome == "meadow" ? art.grass.Select(m => new Material(m) { name = m.name + " (meadow)", color = Color.Lerp(m.color, new Color(.34f, .6f, .2f), .4f), enableInstancing = true }).ToArray()
    : Zone.biome == "mountain" ? art.grass.Select(m => new Material(m) { name = m.name + " (alpine)", color = Color.Lerp(m.color, new Color(.3f, .5f, .25f), .35f), enableInstancing = true }).ToArray()
    : grass;
Gloom and ash still fall through to `grass`. Flower materials are untouched.

## [high] Peaks lighting: grey haze where it should be crisp alpine blue
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Zones\peaks.json, "lighting" block (lines 41-52)
Problem: The capture is nearly monochrome (sat .13). Sun #EEF2FF is blue-white and ambient is blue-grey, so lit and shadowed faces have the same hue. Fog #A7B4C3 from 60 m greys out the mid-distance ridges. There is no sky tint, so the shared hazy sky applies.
Fix:
Replace the colour fields (keep sunPitch 42, sunYaw -28, and sunIntensity 1.2 because the rock values are tuned to it):
"sunIntensity": 1.2,
"sunColor": "#FFF3DC",
"ambientSky": "#7FA0CC",
"ambientEquator": "#74808F",
"ambientGround": "#363A40",
"fogColor": "#A6BEDA",
"fogStart": 80,
"fogEnd": 300,
"skyTint": "#6B87B8",
"skyExposure": 1.15,
"skyHaze": 0.9
A warm-white sun against blue skylight gives warm lit faces and blue shadows. Thinner atmosphere (.9) deepens the zenith. Bluer, farther fog turns far ridges blue instead of grey. Check that fogEnd 300 does not expose the backdrop edge; if it does, fall back to 270. Weather still closes the fog in through its own multiplier.

## [medium] Mountain grade: more colour, cool cast moved to the light
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZonePost.cs line 31
Problem: Saturation 1.08 is the lowest of the living zones. Tint (.97, 1, 1.04) cools the whole frame, including sunlit faces, which cancels the warm/cool split the new lighting creates.
Fix:
Current:
case "mountain": return new Grade { saturation = 1.08f, contrast = 1.2f, exposure = .95f, vignette = .55f, bloom = .42f, threshold = 1.1f, tint = new Color(.97f, 1, 1.04f) };
Replace with:
case "mountain": return new Grade { saturation = 1.2f, contrast = 1.2f, exposure = .95f, vignette = .5f, bloom = .42f, threshold = 1.1f, tint = new Color(.99f, 1, 1.02f) };
Contrast, exposure, bloom and threshold are unchanged; they guard against the near-white crags.

## [medium] Alpine turf is khaki; make it greener (turf only)
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs line 498
Problem: alp runs (.27,.31,.18) to (.38,.36,.22), a dull khaki that reads grey-green under the grade (capture lower frame 100,102,95).
Fix:
Current:
Color alp = Color.Lerp(new Color(.27f, .31f, .18f), new Color(.38f, .36f, .22f), n1);
Replace with:
Color alp = Color.Lerp(new Color(.24f, .36f, .17f), new Color(.38f, .41f, .2f), n1);
Luminance is about the same, so the value balance against rock holds. Leave rockC and scree (lines 496-497) alone.

## [medium] Khaven lighting: keep the dusk, split warm sun from violet shadow
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Zones\khaven.json, "lighting" block (lines 43-58)
Problem: Sunlit and shadowed surfaces are the same mauve (capture mean 103,81,82). Ambient sky #6C6588 and equator #68545E are close in hue to the rose fog and grade, so the form is flat. The mood is right; the depth is missing.
Fix:
Change four fields only:
"sunIntensity": 1.15,   (was 1.05)
"ambientSky": "#62608E",   (was #6C6588: same value, bluer violet)
"ambientEquator": "#5C4E60",   (was #68545E: a little darker and cooler)
"ambientGround": "#282228",   (was #2C2428)
Keep sunColor #F08A5A, fogColor #7A5F66, fogStart 24, fogEnd 112, sunPitch 12, sunYaw -62, sunHigh 17, skyTint #A67884, skyExposure 0.8, skyHaze 1.4. Orange-lit faces now sit against blue-violet shadow sides.

## [medium] Gloom grade: more contrast, slightly less global rose
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZonePost.cs line 34
Problem: Tint (1.05, .95, 1.03) removes 5% green from every pixel, so local colours (the red coat, gold windows, blue slate roof) all converge on mauve. Contrast 1.12 is the lowest of the old zones.
Fix:
Current:
case "gloom": return new Grade { saturation = .82f, contrast = 1.12f, exposure = 1.04f, vignette = .85f, bloom = .6f, threshold = .95f, tint = new Color(1.05f, .95f, 1.03f) };   // Khaven: drained, rose-violet dusk
Replace with:
case "gloom": return new Grade { saturation = .86f, contrast = 1.2f, exposure = 1.04f, vignette = .85f, bloom = .65f, threshold = .9f, tint = new Color(1.04f, .96f, 1.02f) };   // Khaven: drained, rose-violet dusk; deeper darks so the lit windows and the low sun carry it
The ground was tuned under the old tint, so check that roads still read as ways. If they turn greenish, restore the tint to (1.05, .95, 1.03) and keep the contrast change.

## [medium] Ash Rim lighting: same grey, deeper shadow and a little more distance
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Zones\ashrim.json, "lighting" block (lines 37-49)
Problem: The capture ground spans only lum .26-.42. Ambient (sky #7C7B82, equator #69676C) is high relative to a .9 sun, so ruins and dunes have little form. Fog from 16 m flattens everything past the first ruin.
Fix:
Change these fields (keep sunPitch 16, sunYaw -48, sunColor #D8D0C6, fogColor #A3A1A8, skyTint #9987A1; add no skyExposure or skyHaze):
"sunIntensity": 1.0,   (was 0.9)
"ambientSky": "#72717C",   (was #7C7B82)
"ambientEquator": "#5E5C64",   (was #69676C)
"ambientGround": "#262428",   (was #2D2B2D)
"fogStart": 22,   (was 16)
"fogEnd": 105,   (was 90)
No hue change. The faint violet stays in the shadows only. The lit/shadow ratio rises by roughly 25%.

## [medium] Ash grade: contrast up, saturation unchanged
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZonePost.cs line 33
Problem: Bloom .6 with threshold .95 on a pale, foggy scene lifts the whole frame into a milky mid-grey. Contrast 1.16 does not offset it.
Fix:
Current:
case "ash": return new Grade { saturation = .7f, contrast = 1.16f, exposure = .95f, vignette = .8f, bloom = .6f, threshold = .95f, tint = new Color(.99f, .985f, 1.02f) };
Replace with:
case "ash": return new Grade { saturation = .7f, contrast = 1.24f, exposure = .97f, vignette = .85f, bloom = .45f, threshold = 1.05f, tint = new Color(.99f, .985f, 1.02f) };
Lamps and embers still bloom because they are well above 1.05 in HDR.

## [low] Ash plates: widen plate-to-plate value variation (optional)
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs line 515
Problem: Plates differ by only +/-3% in value, so the Voronoi crust reads as a uniform tiled floor in the capture rather than blocked-in brushwork.
Fix:
Current:
Color ashC = Color.Lerp(new Color(.46f, .46f, .47f), new Color(.60f, .59f, .59f), n1) * (.97f + .06f * Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1));
Replace with:
Color ashC = Color.Lerp(new Color(.44f, .44f, .46f), new Color(.61f, .60f, .60f), n1) * (.93f + .14f * Mathf.Repeat(sx[near] * 7.31f + sz[near] * 3.17f, 1));
Same mean value and still grey. Do this last and only if the plates still look flat after the lighting and grade edits.

## [low] Verification after applying (nothing here was rendered)
Where: Validation copy capture tour; D:\code\mmo\New Unity Project\Assets\Crulanda\Tests\PlayMode\WeatherTests.cs lines 92-94
Problem: All values are derived from code and six stills. The procedural skybox's response to _SkyTint and thickness, and the fogEnd increases (Oakhaven 240, Peaks 300, Ash Rim 105), cannot be judged without a render. The fog-end test range follows the JSON, so it adapts.
Fix:
After the running test finishes: apply the edits, sync to the validation copy, run WeatherTests, then re-capture oakhaven-01, oakhaven-12, peaks-03, khaven-04 and ashrim-01 at the same hour. Check:
1. Oakhaven sky reads blue, not white; if white, set skyExposure to 1.1.
2. Peaks sunlit crags stay at or below about 130 of 255.
3. Peaks and Oakhaven backdrop edges are not visible through the thinner fog.
4. Khaven roads still read against the earth.
5. Daytime ambientSky grayscale stays above .3 in every weather (computed minimum is about .375, Khaven in rain).

# check:geometry

patch_buildings.py will apply cleanly and should compile once `ZoneArt.masonry` exists, but several trim parts land wrong: shutters float off the wall, rafter ends are buried in the roof slab, the ridge cap overhangs with a gap, the door ring floats, and the inn gets doubled glass. Nothing was compiled or run; this is from reading the code and working the numbers by hand.

**(1) Anchors.** All 10 anchors (1 in ZoneMeshes.cs, 9 in ZoneBuilder.cs) occur exactly once, applied sequentially in memory. Both targets are LF-only with no BOM, and the patch text is LF with no tabs, so there is no newline mismatch. In-memory patched copies are in the scratch dir for diffing.

**(2) Compile.** No errors found by reading.
- `using System` is present in ZoneMeshes.cs for `Func<>`; the Box locals and lambdas don't clash.
- `BoxPart` returns `GameObject` via `MeshPart`; `null` for `Quaternion?` and int `1` for `float tile` are fine.
- `WallPiece` and the hearth still get a fitted `BoxCollider`, because `MeshPart` adds the `MeshFilter` before `AddComponent<BoxCollider>`. `NavBlocker` uses renderer bounds, so unit scale is fine.
- No existing members are named `Eaves`, `Window`, `PlankDoor`, `Shutter` or `BoxPart`. No code looks objects up by the name "Cube".
- Prerequisites:
  - `public Material masonry;` on ZoneArt.
  - EnsureArt must have run (`PaintedTextures` in ZoneSceneBuilder.Painted.cs line 110) so the baked ZoneArt asset has masonry. Otherwise `Tint(art.masonry, ...)` throws a NullReferenceException in House and Inn.
  - That same file also references `art.rock`, which is a second new ZoneArt field belonging to the rock part.

**(3) ZoneMeshes.Box.** Correct on all three counts.
- Winding: `cross(b-a, c-a)` gives -z, +z, +x, -x, +y, -y for the six faces in order, which is clockwise from outside and matches the existing `Quad` convention in GableRoof.
- Normals: four separate vertices per face with `RecalculateNormals` gives flat outward normals.
- UVs: metres divided by `tile`, u runs left to right seen from outside on all four sides (not mirrored), v = y + hy from the box's own foot. Top and bottom use (x, z). No tangents are generated, which is fine because Masonry has no normal map.

**(4) Placement.** Default house: top 3.6, roofH 2.75, roof x ±4.1, z ±3.2, underside y 3.35. Inn 12 x 8: H 6.4, roofH 4, roof z ±4.7, wall outer face at d/2 + .15.

| Part | Where it lands | Verdict |
|---|---|---|
| Fascia | y 3.34..3.58, z 3.17..3.27, x ±4.15 | Fine; caps the eave edge. |
| Rafter ends | y 3.33..3.47 against underside 3.35 | Buried; only 2 cm shows. |
| Rafter spacing | first .2 m from the left corner, last .5 m from the right | Asymmetric. |
| Ridge cap | bottom at ridge - .04; roof at z ±.17 is ridge - .146 | 10.6 cm gap under both edges. |
| Chimney cap | y top + 1.3 roofH - .02..+.10 | Fine; overlaps the stack by 2 cm, clear of the ridge cap. |
| Window frame and sill | back face 2 cm off the house wall | Floating. |
| Shutters | back face 6.5 cm off the house wall, 3.5 cm off the inn wall | Floating. |
| Shutter at x = 2.85 on the 7.5 m house | runs 6 cm into the corner post | Overlap. |
| Inn window glass | second pane 1 cm in front of the existing through-wall glass | Coplanar within 1 cm, and redundant. |
| Door planks and bands | bands front at -d/2 - .245, frame front at -.24 | Fine; bands stop short of the jambs. |
| Door ring | back at -.245, planks at -.22 to -.23 | Floats 1.5 to 2.5 cm; not on a band. |

- **Clearances that hold:** the sill rail, knee braces, door frame, inn sign strut, barrels, CrackedHearth stack and inn door (which swings inward at -105 degrees) are all clear of the new parts, across every house, inn and barn size in the zone JSONs.
- **Existing bug, not from the patch:** the inn's upper-storey diagonal braces sit at d/2 + .03 ± .05, inside the .3 m wall, so they are invisible.
- **Left unpainted by the patch:** the Inn variant 0 chimney (ZoneBuilder.cs line 1137), the hinged inn door and the inn stone step.

**(5) Determinism.** The new code makes no `R01`, `rng` or `Random` calls. All loops are deterministic and the zone random's draw order is unchanged.

## [high] Shutters, window frames and sills float off the wall; inn gets a second glass pane
Where: patch_buildings.py Window() (lines 79-91) and its calls at lines 132, 134, 163, 164
Problem: Window() offsets everything from the glass centre, which is d/2+.05 on the house but d/2+.17 on the inn. Shutter back face ends up 6.5 cm off the house wall and 3.5 cm off the inn wall. Frame and sill backs are 2 cm off the house wall. On the inn, Window() adds a second glass pane whose front (d/2+.21) is 1 cm in front of the existing through-wall glass (d/2+.20), with exactly coplanar side faces.
Fix:
Take the point on the OUTER WALL FACE and add a glass flag:
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
                Part(PrimitiveType.Cube, t, face + new Vector3(s * (wide / 2 + .24f), 0, sz * .02f), new Vector3(.3f, high + .1f, .06f), Tint(art.timber, Shutter[Mathf.Abs(variant) % Shutter.Length]));   // on the wall: face-.01 .. face+.05
        }
House calls (glass stays at d/2+.05 as before):
                    Window(t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * d / 2), .8f, .7f, sz, variant, Mathf.Abs(x) + .79f <= w / 2 - .15f && Mathf.Abs(x) - .79f >= .78f);
                    Window(t, new Vector3(x, .6f + wallHeight * .78f, sz * d / 2), .8f, .7f, sz, variant, false);
Inn calls (keep the existing through-wall glass, add no second pane):
                    Window(t, new Vector3(x, 1.5f, sz * (d / 2 + wall / 2)), .9f, .8f, sz, variant, true, false);
                    Window(t, new Vector3(x, storey + 1.3f, sz * (d / 2 + wall / 2)), .8f, .7f, sz, variant, false, false);

## [high] Rafter ends buried in the roof slab and unevenly spaced
Where: patch_buildings.py Eaves() line 75
Problem: Rafters span y top-.27..top-.13, but the roof underside is at top-.25, so only 2 cm shows below the soffit; the rest is inside the slab. The x series starts .2 m from the left corner and ends .5 m from the right on a 7 m house.
Fix:
Hang them under the soffit, centre the series, and deepen the fascia to cover their ends. Replace the foreach body in Eaves:
            int n = Mathf.Max(1, Mathf.FloorToInt((w - .5f) / .9f));
            foreach (int sz in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, top - .2f, sz * (d / 2 + .72f)), new Vector3(w + 1.3f, .38f, .1f), dark);   // fascia: top-.39 .. top-.01
                for (int i = 0; i <= n; i++) Part(PrimitiveType.Cube, t, new Vector3((i - n / 2f) * .9f, top - .31f, sz * (d / 2 + .33f)), new Vector3(.12f, .14f, .7f), dark);   // top-.38 .. top-.24 (1 cm into the soffit), z d/2-.02 .. d/2+.68 (1 cm into the fascia)
            }

## [medium] Ridge cap overhangs the ridge with a 10 cm gap under each edge
Where: patch_buildings.py Eaves() line 77
Problem: Cap bottom is at ridge-.04, but at its edges (z = ±.17) the roof surface is at ridge-.146 on the house and ridge-.145 on the inn. A flat board balances on the ridge line with an open wedge under both sides.
Fix:
Size the cap to the slope so its bottom edges sink 2 cm into the roof:
            float sag = roofH * .17f / (d / 2 + .7f);
            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .05f - (sag + .07f) / 2, 0), new Vector3(w + 1.3f, sag + .07f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));

## [medium] Door ring floats off the door
Where: patch_buildings.py PlankDoor() line 97
Problem: Ring centre is at -.07 with half-length .015, so its back is at -.055. The door face is at -.03 and the seams at -.04. The ring is not on a band, so it floats 1.5 to 2.5 cm in front of the planks.
Fix:
            Part(PrimitiveType.Cylinder, t, at + new Vector3(wide * .3f, -high * .05f, -.04f), new Vector3(.14f, .015f, .14f), art.metal, Quaternion.Euler(90, 0, 0));   // the ring: -.055 .. -.025, 5 mm into the planks

## [high] art.masonry is a hard prerequisite; a null asset throws
Where: ZoneArt.cs; patch_buildings.py lines 105, 139, 140, 143, 145, 175
Problem: ZoneArt has no `masonry` field, so the patch will not compile without it. If the baked ZoneArt asset has not been regenerated by EnsureArt/PaintedTextures (for example in the validation copy), `Tint(art.masonry, ...)` throws a NullReferenceException on `baseMat.name` in House and Inn, and BoxPart renders magenta.
Fix:
ZoneArt.cs:
        [Tooltip("Painted dressed stone for plinths, chimneys, footings and hearths (art.stone stays natural rock).")]
        public Material masonry;
ZoneBuilder.cs, next to Tint:
        Material Masonry { get { return art.masonry != null ? art.masonry : art.stone; } }
Then replace every `art.masonry` in patch_buildings.py with `Masonry`. Run the art build (EnsureArt) in the validation copy before tests.

## [medium] Shutter runs into the corner post on the 7.5 m house
Where: House window loop, patch line 132
Problem: For w = 7.5 the window at x = 2.85 has its outer shutter reaching x = 3.66, while the corner post starts at 3.6. For w = 6 the inner shutter edge is only 3 cm from the door jamb.
Fix:
Covered by the shutters condition in the Window fix: `Mathf.Abs(x) + .79f <= w / 2 - .15f && Mathf.Abs(x) - .79f >= .78f`. That drops the shutters on the 7.5 m house's x = 2.85 window (both walls); every other size in the zone data keeps them.

## [low] Painted texture restarts on each inn wall piece
Where: ZoneMeshes.Box UV maps; Inn WallPiece
Problem: u and v start at each box's own corner. The inn's south wall is three pieces (two sides and the lintel from y = 2.4), so the plaster pattern jumps at the door edges and above the door. Plinth and wall also restart v independently.
Fix:
Optional: give Box a UV origin in the building's local space and have BoxPart pass localPos.
        public static Mesh Box(Vector3 size, float tile = 2, Vector3? origin = null)
        { ... var o = origin ?? new Vector3(hx, hy, hz);
          // -z: p => new Vector2(p.x + o.x, p.y + o.y)   +z: p => new Vector2(-(p.x + o.x), p.y + o.y)
          // +x: p => new Vector2(p.z + o.z, p.y + o.y)   -x: p => new Vector2(-(p.z + o.z), p.y + o.y)
          // top and bottom: p => new Vector2(p.x + o.x, p.z + o.z)
        GameObject BoxPart(...) { return MeshPart(ZoneMeshes.Box(size, tile, localPos), parent, localPos, m, rot); }
Winding and normals are unchanged; v then runs up from the building root's y = 0.

## [low] Inn chimney, hinged door and step are left unpainted
Where: ZoneBuilder.cs lines 1137, 1140, 1142 (not touched by the patch)
Problem: The variant 0 inn chimney is still a stretched Cube in art.stone (1 x 4.4 x 1 m, texture stretched 4:1) with no cap. The hinged door is a flat dark Cube and the step is art.stone. They will look inconsistent beside the painted house.
Fix:
Add a patch pair for line 1137:
            else { BoxPart(t, new Vector3(w / 2 - 1.1f, H + roofH * .75f, d * .15f), new Vector3(1, roofH * 1.1f, 1), Masonry, null, 1); Part(PrimitiveType.Cube, t, new Vector3(w / 2 - 1.1f, H + roofH * 1.3f + .04f, d * .15f), new Vector3(1.2f, .12f, 1.2f), Tint(Masonry, new Color(.45f, .44f, .4f))); }
Smoke at H + roofH * 1.4 stays above the cap. Step at line 1142: change art.stone to Masonry.

## [low] Existing bug: inn upper-storey braces are inside the wall
Where: ZoneBuilder.cs line 1124
Problem: Braces sit at z = sz * (d/2 + .03) with thickness .1, but the inn wall is d/2 ± .15, so they are fully buried and invisible. Not caused by the patch. If fixed, each brace would cross the upper window frames (x = -4.5 + 2.6k against braces at -4.4 + 2.8k).
Fix:
Leave as is for the painted pass, or delete the loop. If the braces are wanted, move them to z = sz * (d / 2 + wall / 2 + .03f) and x = window x + 1.3f so they sit between windows.

## [low] One new Mesh per BoxPart call
Where: patch_buildings.py BoxPart
Problem: Every wall, plinth and chimney allocates its own Mesh (about 70 per village). This is harmless for static batching, but the meshes are never shared or destroyed.
Fix:
Optional cache:
        readonly Dictionary<(Vector3, float), Mesh> boxes = new Dictionary<(Vector3, float), Mesh>();
        GameObject BoxPart(Transform parent, Vector3 localPos, Vector3 size, Material m, Quaternion? rot = null, float tile = 2)
        {
            if (!boxes.TryGetValue((size, tile), out var mesh)) boxes[(size, tile)] = mesh = ZoneMeshes.Box(size, tile);
            return MeshPart(mesh, parent, localPos, m, rot);
        }
Not compatible with the per-position UV origin option above; pick one.

# check:rock

Painted rock review. Nothing was compiled or run (Unity forbidden); nothing under D:\code\mmo was touched. Texture numbers come from measuring stone_blotch.png and a numpy port of RockPixel (approximate Perlin).

VERDICT
- PaintedRock.shader: should compile as written and is correct for static batching, shadows, fog and Tint. Two real look problems: zone-wide parallel stripes on flat tops, and a warm _Top cast that is wrong for ash/gloom.
- patch_rock.py: all 9 anchors occur exactly once (ZoneBuilder.cs lines 822, 2036, 2718, 2887, 3030, 3635, 3929, 4091; ZoneBuilder.Verdant.cs 208; both files LF, no BOM). But the patched code will NOT compile: the new class method Rock(Color) is hidden inside Cliff() by its local function Rock(float x, ...) at ZoneBuilder.cs:2042, so line 2036 gives CS1501. Rename the helper RockMat.
- Neither patch adds `rock` (or `masonry`) to ZoneArt.cs; patch_buildings.py has no ZoneArt edit.

SHADER CHECKS
- Custom Input member `wNormal` with vertex:vert and UNITY_INITIALIZE_OUTPUT is valid; no reserved-name clash. Built-in `float3 worldNormal` is the better choice: surf never writes o.Normal, so no INTERNAL_DATA is needed, and vertex:vert and addshadow can go (FallBack "Diffuse" supplies the shadow caster).
- Precision: `half _Scale` divides world position, so make it float. `fixed4 _Top` at 1.18 is within fixed range, but float4 is safer.
- Triplanar: weights pow(abs(n),4) normalised are right; projections zy / xz / xy are right, with v = world Y on both side projections so strata lie level. The weight sum is unguarded.
- Colour space is Gamma (ProjectSettings m_ActiveColorSpace: 0), so _Color and _Top multiply albedo literally. Rendering path is forward (m_RenderingPath: 1); a surface shader also generates the deferred pass.
- Tint: `new Material(base) { color = c }` works (_Color and _MainTex exist). The cache key becomes "Painted rock"+hex, so it never collides with Stone tints of the same colour.
- TreeFade: components are added only on tree roots (ZoneBuilder.cs 1295, 1363, 1635, 1728, 2211; Verdant 79); no switched rock is parented under one. The only shader-name check in Scripts is "Crulanda/Leaf" (TreeFade.cs:128). There are no replacement shaders (RenderMap uses a plain camera), and WorldWeather only touches GroundMaterial. Nothing mishandles the new shader.

SWITCHED SITES (all genuinely natural rock; none shared with masonry or bones)
- 2036 Cliff lumps and scree; 2718 Perch boulders; 2887 cave knoll (roots variant stays soil); 3030 loose rock at the knoll foot; 3929 EdgeRock; 4091 backdrop tors (mountain/ash only); Verdant 208 waterfall rock and mossy.
- 3635 SecretStone: its callers are SecretCairn, SecretCamp ring, SecretRocks and SecretFlatStone, all boulder lumps. At 0.13-0.75 m against a 5 m tile they will read as near-flat colour; acceptable.

SHOULD ALSO SWITCH / MUST NOT: see issues.

BRIGHTNESS (the premise is wrong)
- stone_blotch.png measures mean 0.833, not 0.68 (0.68 is the formula's base before the Perlin terms add about 0.16). rock_painted simulates at about 0.81.
- Net change against the old stone: side faces x0.97; flat tops x(1.14, 1.11, 0.99), about +10% luminance; undersides x0.58; average over a boulder's upper half about x1.01.
- So do NOT scale tints by 0.85; that would darken every crag about 15%. Keep all zone tints unchanged.
- Control the top per biome through _Top instead: meadow/verdant/default (1.18, 1.14, 1.02); mountain (1.08, 1.06, 1.00), tops net about +3%, so sunlit crags stay under the fog; ash and gloom neutral (1.08, 1.08, 1.09), since the warm default turns grey ash rock sandstone on top.
- If _Top is left global instead, the tint-only fallback is mountain Cliff (.35,.335,.31) -> (.32,.305,.28) and MountainStone (.36,.35,.335) -> (.33,.32,.305), which also darkens the sides 9%; ash cannot be fixed by tint.

Files are in C:\Users\chris\AppData\Local\Temp\claude\D--code-mmo\be58a4b3-4c77-45ef-9182-17eb205878fc\scratchpad\painted-review\rock\ ; PaintedRock.fixed.shader is the proposed replacement (uncompiled).

## [high] Helper name Rock collides with Cliff's local function: compile error
Where: patch_rock.py helper + D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs:2036 and :2042
Problem: Cliff() declares a local function `void Rock(float x, float bottom, float z, float sx, float sy, float sz, float lean, float roll, float yaw)` at line 2042. A local function is in scope for the whole method body and hides the class method, so the patched line 2036 `var stone = Rock(Zone.biome == ...)` binds to the local and fails with CS1501 (no overload takes 1 argument).
Fix:
Rename the helper and every call in patch_rock.py from `Rock(` to `RockMat(` (8 call sites plus the definition; the Verdant line becomes `var rock = RockMat(new Color(.44f, .44f, .42f)); var mossy = RockMat(new Color(.3f, .38f, .22f));`). Leave Cliff's local Rock alone. Definition: `Material RockMat(Color color) { return Tint(art.rock != null ? art.rock : art.stone, color); }` or the per-biome version in the _Top issue.

## [high] ZoneArt has no rock (or masonry) field and no patch adds one
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneArt.cs; tools\wip\painted\patch_buildings.py, patch_rock.py, ZoneSceneBuilder.Painted.cs:110-116
Problem: `art.rock` and `art.masonry` are used by Painted.cs and both patches, but ZoneArt.cs declares neither and neither patch script edits it (patch_rock only says 'apply after ZoneArt has rock'). Also Painted.cs:115 calls `new Material(Shader.Find("Crulanda/PaintedRock"))`, which throws ArgumentNullException and aborts EnsureArt if the shader is not yet imported.
Fix:
Add to ZoneArt.cs: `[Tooltip("Coursed masonry for built stone (walls, footings, chimneys).")] public Material masonry;` and `[Tooltip("Crulanda/PaintedRock: natural rock, world-projected strata.")] public Material rock;`. In Painted.cs: `var rockShader = Shader.Find("Crulanda/PaintedRock"); if (art.rock == null && rockShader != null) { art.rock = new Material(rockShader) { ... }; ... } else if (rockShader == null) Debug.LogWarning("Crulanda/PaintedRock not imported yet: rock stays on art.stone");`

## [high] Brightness premise is wrong: do not darken tints
Where: task note (rock ~.8 vs stone_blotch ~.68); Assets\Crulanda\World\Art\stone_blotch.png; Editor\ZoneSceneBuilder.cs:100
Problem: stone_blotch.png measures mean 0.833 (p5/50/95 = .725/.839/.918); .68 is only the formula's base before +Perlin*.25 +Perlin*.1. RockPixel simulates at about 0.81. The project is Gamma colour space, so multipliers are literal. Net against today: sides x0.97, flat tops x(1.14, 1.11, 0.99), undersides x0.58, upper-hemisphere average about x1.01. Scaling tints by .68/.8 = 0.85 would darken every crag about 15%.
Fix:
Leave every zone tint as it is. Control the tops with _Top per biome (see the _Top issue). Tint-only fallback for mountain if _Top stays global: Cliff `new Color(.32f, .305f, .28f)`, MountainStone `new Color(.33f, .32f, .305f)`.

## [medium] _Top is one global warm value: wrong for ash/gloom, over-bright for mountain
Where: PaintedRock.shader:13; patch_rock.py helper
Problem: The shader comment says the top light is 'by zone', but nothing sets _Top per zone. (1.18, 1.14, 1.02) on neutral grey ash rock (.37,.365,.37) gives tops (.437,.416,.377), a sandstone cast, against the existing comment 'ash: grey, not sandstone'. In mountain zones tops gain about 10%, against 'darker, so a sunlit crag stays under the fog'.
Fix:
Per-zone base clone in ZoneBuilder: `Material rockBase; Material RockMat(Color color) { if (art.rock == null) return Tint(art.stone, color); if (rockBase == null) { rockBase = new Material(art.rock) { name = "Painted rock" }; rockBase.SetColor("_Top", Zone.biome == "ash" || Gloom ? new Color(1.08f, 1.08f, 1.09f) : Zone.biome == "mountain" ? new Color(1.08f, 1.06f, 1f) : new Color(1.18f, 1.14f, 1.02f)); } return Tint(rockBase, color); }`. If the shader's _Top becomes a Vector (as in the fixed shader), use SetVector instead.

## [medium] Top projection shows zone-wide parallel stripes
Where: PaintedRock.shader:40 (`tex2D(_MainTex, p.xz) * w.y`)
Problem: The texture is horizontal strata (simulated row-mean std .048 against column-mean std .014). Projected from above it becomes stripes about 0.55 m apart running along world X on every flat rock top in the zone, all aligned; that reads as planks or ploughing, not 'drifts'.
Fix:
Use two turned samples for the top, as in scratch PaintedRock.fixed.shader: `float2 t = float2(p.x*0.8 + p.z*0.6, p.z*0.8 - p.x*0.6) * 0.6; float2 t2 = float2(t.x*0.34 - t.y*0.94, t.x*0.94 + t.y*0.34) * 1.37 + 0.31; half3 top = (tex2D(_MainTex, t).rgb + tex2D(_MainTex, t2).rgb) * 0.5;` and blend `top * w.y`.

## [low] Shader clean-ups: built-in worldNormal, float precision, guarded weights
Where: PaintedRock.shader:23-37
Problem: Compiles as written, but: the custom wNormal interpolator plus vertex function duplicates the world normal the surface shader already carries; addshadow is redundant (no vertex displacement; FallBack supplies the caster); `half _Scale` is used on world position (precision on mobile); the weight sum divides unguarded (a zero normal gives NaN); _Top is a non-HDR Color with values above 1, which the inspector picker clamps when edited.
Fix:
`#pragma surface surf Standard fullforwardshadows`; `struct Input { float3 worldPos; float3 worldNormal; };`; delete vert; `float _Scale; float4 _Top;`; `_Top ("Top light", Vector) = (1.18, 1.14, 1.02, 1)`; `float3 w = abs(n); w *= w; w *= w; w /= max(w.x + w.y + w.z, 1e-4); float3 p = IN.worldPos / max(_Scale, 0.01);`. Full file: scratch PaintedRock.fixed.shader (uncompiled).

## [medium] RockPixel does not tile: straight seam every 5 m, cracks cut at tile edges
Where: D:\code\mmo\tools\wip\painted\ZoneSceneBuilder.Painted.cs:124-131
Problem: (1) The bed value is Hash(bed, 21) with bed = floor(by); the wobble pushes rows near y=0 into bed -1 and rows near y=T into bed 9, whose values (.753, .812) differ from beds 8 and 0 (.887, .759). That gives a dead-straight value step of up to .13 at the tile edge, every 5 m of height on every cliff. (2) The cracks call Fbm(x*1.6f, y*.35f, ...): Tiled only tiles for arguments in [0,T) with period T, so x*1.6 extrapolates (weights -0.6/1.6) and y*.35 never wraps; cracks are cut at both tile edges and are denser on the right 37%. (3) Simulated crack coverage is about 7% with a hard edge (x.79 jumps to x1), which is more than 'a few'.
Fix:
`float v = .8f + (Hash(((bed % 9) + 9) % 9, 21) - .5f) * .22f;` and `float crack = Mathf.Abs(Tiled((px, py) => Perlin(px * 1.6f, py * .35f, .013f) * .65f + Perlin(px * 1.6f, py * .35f, .027f) * .35f, x, y) - .5f); v *= Mathf.Lerp(.55f, 1, Smooth(0, .008f, crack));`. Delete rock_painted.png if it was already generated, because Tex() never regenerates. The same Fbm misuse is in ThatchPixel line 52 (`y * .18f`), which will not tile vertically.

## [medium] Natural-rock sites the patch misses (should switch)
Where: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs:916, 918, 2738, 2772, 3567 (optional 3346, 3774)
Problem: These are boulder lumps or rock faces that stay on the old Stone while their neighbours go painted. Worst cases: the plain 'rock' prop (every placed boulder) at 916/918; the ash Cave face at 2772, whose tint is deliberately identical to the ash Cliff 'so the face runs on into the Ridge' and would now be a different material beside it; and the cairn's greyed top stone at 3567 sitting on painted lower stones.
Fix:
916: `float s = 1 + p.variant * .6f; var stone = RockMat(new Color(.52f, .51f, .48f));` (Stone.mat's own colour). 918: replace `stone = Tint(art.stone, MountainStone);` with `stone = RockMat(MountainStone);`. 2738: `var stone = RockMat(new Color(.42f, .4f, .37f));` (used only for the two rubbing boulders at 2758). 2772: `var rock = RockMat(new Color(.37f, .365f, .37f));` and leave `dark` and `salt` on art.stone. 3567: `RockMat(new Color(.6f, .6f, .62f))`. Optional for consistency with SecretCamp: 3346 `var stone = RockMat(new Color(.34f, .32f, .3f));`; 3774 `var burnt = RockMat(new Color(.28f, .26f, .24f));` (keep `soot` on art.stone; it also paints the flat charred disc).

## [low] Sites that must NOT switch
Where: ZoneBuilder.cs and ZoneBuilder.Verdant.cs, remaining Tint(art.stone, ...) / art.stone uses
Problem: These are built, carved or non-rock uses where world-projected strata, sun-lit tops and x0.6 undersides would look wrong, or cave interiors whose darkness is tuned.
Fix:
Keep on art.stone (or art.masonry via patch_buildings): 913 graves; 1035/1069/1110/1137/1142/1162/1188/1217/1230/1866 footings, chimneys, hearth, steps, well; 1644-1645 oak bench ring; 1790 water pan; 1816/1820 bridge deck and parapet; 1830 Ruin blocks; 1840-1853 shrine/statue pieces; 1901 Wall; 1924-1925 Monolith; 1959 Tower; 1990 Crypt; 2292 exit waystone; 2407-2412, 2490, 2643, 2684, 2686 built stone; 2469/2472/2489 clay and salt; 3136/3328 puddles; 3173, 3409, 3499, 3689, 3838 dark holes; 3387, 3663, 3789 salt; 3442/3450 bone; 3474, 3774 soot, 3803 ash; Verdant.cs:95 building stone. Cave interiors stay Stone this pass: 2886 Cavern shell (UV-mapped faceted shell), 3174 choke heap, 3180 stalagmites/stalactites, 3368 throne dais; if they are switched later, give them a clone with _Top = (1,1,1).

## [low] Backdrop tors: 5 m tiles vanish at distance
Where: ZoneBuilder.cs:4091, 4149-4150
Problem: Tors are 14-31 m wide and 34-73 m tall, seen from 40 m and beyond. At 5 m per tile (beds about 0.55 m) the paint mips to a flat colour; only the top/underside shading survives. In mountain zones rockMat is also the same cached Tint instance as Perch/EdgeRock/rock props (same MountainStone colour), so _Scale cannot be changed on it directly.
Fix:
`var rockMat = new Material(RockMat(ash ? new Color(.36f, .355f, .36f) : mountain ? MountainStone : new Color(.40f, .39f, .38f))) { name = "Backdrop rock" }; if (rockMat.HasProperty("_Scale")) rockMat.SetFloat("_Scale", 22);` (beds about 2.4 m). The backdrop's parts dictionary keys on the material instance, so this still merges to one mesh per side.

# check:textures

Ported all six pixel functions to numpy (classic Perlin scaled to 0..1), rendered raw, 2x2 tiled and tinted sheets, and looked at every one. Nothing was built or run in Unity; the C# below is a hand translation of the Python that produced the v2 images and has not been compiled.

**v1 as written (v1_*.png):** no NaN or negative values, and `Hash` is fine (0..1, unchecked int wrap). But five of six textures are not seamless, and none reads as painted:
- **Plaster:** hard horizontal line every 2 m from the foot grime; the sine strokes give regular diagonal corduroy.
- **Thatch:** reads as clapboard (flat cream bands with a 1 px black line); clips above 1; shadow is on the wrong side of each layer for the roof UVs (v runs up-slope).
- **Slate:** reads as bathroom tile; visible vertical seam strip on odd rows; lit edge is below the joint (inverted); moss invisible.
- **Stone:** vertical seam strip at the tile edge, and block identity changes mid-block because the row jitter moves the mortar but not the row index.
- **Timber:** grain is an 11 px sine moire; knots can be cut by the tile edge.
- **Rock:** cracks are noise-contour scribbles and loops; bed values jump at the y seam.

**v2 (v2_*.png, the issues below):** all six tile; edge-versus-interior pixel differences match except where a real feature (plank joint, slate row) sits on the edge. Feature sizes at the texel scales used:
- **Thatch:** 7 ragged layers, 0.36 m each.
- **Slate:** 6 x 7 slates, 0.42 x 0.36 m.
- **Stone:** 5 uneven courses of 0.34-0.46 m, blocks 0.5-1 m with rounded corners.
- **Timber:** 8 planks of 0.25 m.
- **Rock:** 6 uneven beds of 0.6-1.3 m.

Tinted previews: plaster, thatch, masonry and timber look right; slate and rock come out dark (about 0.25 and 0.30 mean).

Caveats:
- My Perlin's amplitude may differ slightly from Unity's; if the posterised patches look too flat or too hard in Unity, adjust the `Paint` gain (2.2).
- Slate uses 7 rows with a half-stagger on odd rows, so rows 6 and 0 are both unstaggered at the wrap; the per-row jitter hides it in the render, but 6 or 8 rows would be cleaner (not rendered).
- Rock still shows an occasional small loop where a joint line warps back on itself.

Everything is in C:\Users\chris\AppData\Local\Temp\claude\D--code-mmo\be58a4b3-4c77-45ef-9182-17eb205878fc\scratchpad\painted-review\textures\ (port.py is the faithful v1 port, v2.py the proposed functions; per texture, `_single.png` is one raw tile and the sheet is 2x2 raw plus 2x2 per tint).

## [high] Tiled/Fbm: only seamless when called with the raw pixel x,y; three callers pass shifted or scaled coordinates. The blend also flattens the tile centre.
Where: ZoneSceneBuilder.Painted.cs: Tiled, Fbm (callers: ThatchPixel `Fbm(x + row * 97, ...)` and `Fbm(x, y * .18f, ...)`, TimberPixel `Fbm(x + plank * 53, ...)`, RockPixel `Fbm(x * 1.6f, y * .35f, ...)`)
Problem: Tiled derives its blend weights u,v from the x,y it is given. With shifted or scaled arguments the weights leave 0..1 (extrapolation, values outside 0..1) and the result is not periodic. Measured: thatch seamY 0.45 against 0.014 interior; rock seamY 0.10 against 0.016. Separately, blending four uncorrelated samples halves the noise contrast at the tile centre compared with the corners. Fix: keep x,y raw and put any offset or stretch inside the sampled function; renormalise the blend by the weight norm. Paint is a new helper (soft posterise) that gives the visible flat brush patches; all replacement bodies below use it.
Fix:
static float Tiled(Func<float, float, float> p, float x, float y)
{
    float u = x / T, v = y / T, a = (1 - u) * (1 - v), b = u * (1 - v), c = (1 - u) * v, d = u * v;
    float n = p(x, y) * a + p(x - T, y) * b + p(x, y - T) * c + p(x - T, y - T) * d;
    return .5f + (n - .5f) / Mathf.Sqrt(a * a + b * b + c * c + d * d);   // keep the contrast even across the tile
}
/// x,y must be the raw pixel (0..T). Stretch (sx, sy) and offset (ox, oy, in pixels) are applied inside the sample, so the result still tiles.
static float Fbm(float x, float y, float f, int octaves = 3, float sx = 1, float sy = 1, float ox = 0, float oy = 0)
{
    float v = 0, a = .5f, sum = 0;
    for (int o = 0; o < octaves; o++) { float ff = f; v += Tiled((px, py) => Perlin(px * sx + ox, py * sy + oy, ff), x, y) * a; sum += a; f *= 2.07f; a *= .55f; }
    return v / sum;
}
/// Soft posterise: noise into a few flat values with soft edges (the brush patches).
static float Paint(float n, int levels, float gain = 2.2f, float soft = .2f)
{
    float q = Mathf.Clamp01((n - .5f) * gain + .5f) * levels, fl = Mathf.Floor(q);
    return Mathf.Clamp01((fl + Smooth(.5f - soft, .5f + soft, q - fl)) / levels);
}

## [high] PlasterPixel: foot grime makes a hard line every 2 m; sine strokes do not tile and look like corduroy
Where: ZoneSceneBuilder.Painted.cs: PlasterPixel
Problem: The foot darkening is 22% at y=0 and 0 at y=T, so every wall taller than 2 m (or whose UV does not start at v=0) gets a hard horizontal line per tile (seamY 0.20 against 0.003 interior). The stroke is sin((x*.7+y)*.09): 5.13 cycles across x and 7.33 across y, so it does not tile, and it reads as regular diagonal stripes. Hash(x/3, y/3) also does not divide 512. Overall it is fine noise, not paint. Replacement: posterised broad patches, two layers of elongated brush strokes (rotated and stretched noise sampled inside Tiled), sparse ochre stains, no foot. If a grimed foot is wanted, do it outside the tiling texture (vertex colour, the masonry footing, or a separate decal strip).
Fix:
static Color PlasterPixel(int x, int y)
{
    float broad = Paint(Fbm(x, y, .007f, 2), 4) - .5f;
    float s1 = Paint(Tiled((px, py) => Perlin(px * .8f + py * .6f, (py * .8f - px * .6f) * 4, .011f), x, y), 3) - .5f;   // strokes about 35 cm long, 8 cm wide
    float s2 = Paint(Tiled((px, py) => Perlin(px * .5f - py * .87f + 300, (py * .5f + px * .87f) * 4, .016f), x, y), 3) - .5f;
    float fine = Fbm(x, y, .09f, 2) - .5f;
    float v = .86f + broad * .09f + s1 * .07f + s2 * .035f + fine * .03f;
    float stain = Smooth(.62f, .74f, Fbm(x, y, .009f, 2, 1, 1, 900, 400));
    v *= 1 - .07f * stain;
    var c = new Color(v, v * (.975f - .02f * stain), v * (.93f - .07f * stain));
    if (Hash(x / 4, y / 4) > .992f) c = Mul(c, .9f);   // speckle (4 divides 512)
    return c;
}

## [high] ThatchPixel: reads as clapboard, not seamless, clips, and the shadow is on the wrong side of each layer
Where: ZoneSceneBuilder.Painted.cs: ThatchPixel
Problem: Rendered v1 is flat cream bands with a 1 px black line: lap siding, not thatch. Fbm(x + row*97, ...) and Fbm(x, y*.18f, ...) break tiling; sin(x*.11) is 8.96 cycles; v reaches 1.04 (clipped) and the mean is 0.88, so the tinted result is flat orange. GableRoof UV v runs up the slope, so each layer's ragged tip is at the bottom of its row (f=0) and the shadow of the layer above should fall at the top (f near 1); v1 darkens f near 0. Replacement: 7 layers (0.36 m) with a ragged tufted edge from a row warp (row index wrapped, so it tiles), light tips, shadow under the layer above, clumps and strands as stretched noise offset per row, dark notches between strands at the tips, warmer and more saturated in shadow.
Fix:
static Color ThatchPixel(int x, int y)
{
    const float rows = 7;
    float ry = y / (float)T * rows + (Fbm(x, y, .03f, 2) - .5f) * .5f + (Fbm(x, y, .13f, 1, 1, .15f) - .5f) * .4f;   // ragged, tufted layer edge
    float rr = Mathf.Floor(ry), f = ry - rr, row = rr - Mathf.Floor(rr / rows) * rows;   // f: 0 at the layer's tips, 1 under the layer above; row wraps so the tile is seamless
    float clump = Paint(Fbm(x, y, .05f, 2, 1, .12f, row * 61), 3) - .5f;
    float strand = Fbm(x, y, .22f, 2, 1, .1f, row * 37);
    float v = .64f + .16f * Smooth(.55f, 0, f) + clump * .16f + (strand - .5f) * .3f;   // tips lighter
    v *= 1 - .45f * Smooth(.6f, 1, f);   // shadow cast by the layer above
    v *= 1 - .4f * Smooth(.14f, 0, f) * Smooth(.52f, .38f, strand);   // dark gaps between strands at the tips
    v = Mathf.Clamp01(v);
    return new Color(v, v * (.7f + .22f * v), v * (.4f + .36f * v));   // shadows warmer and more saturated
}

## [high] SlatePixel: seam on odd rows, inverted edge lighting, reads as tile grid
Where: ZoneSceneBuilder.Painted.cs: SlatePixel
Problem: On odd rows xs = x + colW/2 gives col 0..5, and Hash(5,row) differs from Hash(0,row), so the half-slate at the tile edge changes value and hue (visible vertical strip; seamX 0.056 against 0.008 interior). The lit edge (fy .86-.93) is below the dark joint (fy > .93); with v up-slope the lit lip belongs to the bottom edge of the slate above, so it must be above the dark. The h > .8 slates are cream and read as bathroom tiles; moss is invisible. Replacement: 7 x 6 slates (0.36 x 0.42 m), column wrapped, per-row stagger jitter, each slate with its own crooked bottom edge height, dark gap and cast shadow at the row top, lit lip at the slate's bottom, hue from a separate hash (blue, violet-grey, green-grey), moss in the shadowed tops. Tinted with (.4,.44,.5) the roof comes out dark (about 0.25 mean); consider raising the slate tint to about (.5,.55,.62).
Fix:
static Color SlatePixel(int x, int y)
{
    const float rows = 7, cols = 6; float colW = T / cols;
    float ry = y / (float)T * rows; int row = Mathf.FloorToInt(ry); float fy = ry - row;
    float cx = x / (float)T * cols + (row % 2) * .5f + (Hash(row, 3) - .5f) * .24f, cr = Mathf.Floor(cx), fx = cx - cr;
    int col = (int)(cr - Mathf.Floor(cr / cols) * cols);   // wrapped: the slate cut by the tile edge is one slate
    float h = Hash(col, row), hue = Hash(col + 41, row + 23);
    float bottom = .03f + Hash(col + 17, row + 5) * .09f + (fx - .5f) * (Hash(col + 3, row + 9) - .5f) * .1f;   // each slate's own, slightly crooked, bottom edge
    float v = .66f + (h - .5f) * .3f + (Paint(Fbm(x, y, .02f, 2), 3) - .5f) * .1f + (Fbm(x, y, .09f, 2) - .5f) * .06f;
    v += .08f * Smooth(.6f, 0, fy);
    v *= 1 - .42f * Smooth(.7f, 1, fy);                 // shadow of the row above
    v *= 1 + .18f * Smooth(.08f, .01f, fy - bottom);    // lit lip at the slate's bottom edge
    float jx = Mathf.Min(fx, 1 - fx) * colW;
    if (jx < 2) v *= .5f; else if (fx < .5f && jx < 5) v *= 1.1f;   // joint, and its lit side
    if (fy < bottom) v = .26f;                          // the gap under the edge
    var c = hue < .4f ? new Color(v * .9f, v * .95f, v * 1.02f) : hue < .75f ? new Color(v * .97f, v * .94f, v) : new Color(v * .94f, v * .98f, v * .95f);
    float moss = Smooth(.55f, .7f, Fbm(x, y, .012f, 2)) * Smooth(.35f, 1, fy) * .7f;
    return Color.Lerp(c, new Color(v * .72f, v * .95f, v * .5f), moss);
}

## [high] StonePixel: x seam, and row jitter splits blocks mid-height
Where: ZoneSceneBuilder.Painted.cs: StonePixel
Problem: (1) cx = x/T*cols + Hash(row,11) gives col 0..cols; the block cut by the tile edge gets two different hashes (visible vertical strip at every tile edge; seamX 0.128 against 0.006 interior). (2) fy = Repeat(fy + jitter, 1) moves the mortar line but row, cols, the column offset and the block hash still change at the integer row boundary, so blocks change colour and layout mid-height and the mortar grid is offset from the block grid (visible in v1_stone.png). (3) The look is ruler-straight bricks with 1 px bevels. Replacement: a fixed table of uneven course heights (no jitter), wrapped column, rounded corners and wobbly mortar via a rounded-box distance plus noise, soft top-left light and bottom-right shade, posterised facets, warm mortar. Add the static field alongside the function.
Fix:
static readonly float[] StoneCourses = { 0, .17f, .39f, .57f, .8f, 1 };
static Color StonePixel(int x, int y)
{
    float fyT = y / (float)T; int row = 0; while (row < 4 && fyT >= StoneCourses[row + 1]) row++;
    float rowH = (StoneCourses[row + 1] - StoneCourses[row]) * T, py = (fyT - StoneCourses[row]) * T;
    float cols = 2 + Mathf.Floor(Hash(row, 3) * 2.99f), colW = T / cols;
    float cx = x / (float)T * cols + Hash(row, 11), cr = Mathf.Floor(cx), fx = cx - cr;
    int col = (int)(cr - Mathf.Floor(cr / cols) * cols);   // wrapped
    float wob = (Fbm(x, y, .05f, 2) - .5f) * 6;
    float dx = Mathf.Min(fx, 1 - fx) * colW, dy = Mathf.Min(py, rowH - py); const float r = 13;
    float d = (dx < r && dy < r ? r - Mathf.Sqrt((r - dx) * (r - dx) + (r - dy) * (r - dy)) : Mathf.Min(dx, dy)) + wob;   // rounded corners, wobbly edge
    if (d < 3.5f) { float m = .38f + Fbm(x, y, .1f, 2) * .1f; return new Color(m * 1.06f, m, m * .9f); }   // mortar
    float h = Hash(col + row * 31, row);
    float v = .7f + (h - .5f) * .26f + (Paint(Fbm(x, y, .018f, 2), 3) - .5f) * .14f + (Fbm(x, y, .08f, 2) - .5f) * .06f;
    float lit = Smooth(12, 4, Mathf.Min(rowH - py, fx * colW) + wob), shade = Smooth(12, 4, Mathf.Min(py, (1 - fx) * colW) + wob);
    v *= 1 + .16f * lit - .22f * shade;   // lit top and left, shadowed bottom and right
    float warm = (Hash(col * 7 + 1, row * 3 + 2) - .5f) * .1f;
    return new Color(Mathf.Clamp01(v * (1 + warm)), Mathf.Clamp01(v), Mathf.Clamp01(v * (1 - warm)));
}

## [medium] TimberPixel: sine grain reads as moire, knots can be cut by the tile edge, shifted Fbm extrapolates
Where: ZoneSceneBuilder.Painted.cs: TimberPixel
Problem: Grain is sin(x*.55), an 11 px period: fine regular stripes that will shimmer at distance and do not read as painted. Fbm(x + plank*53, ...) feeds u up to 1.7 into Tiled (extrapolated weights). ky = Hash*T can put a knot across the y seam, where it is clipped. Planks are flat with no edge light. Replacement: long posterised grain streaks (stretched noise, offset per plank inside the sample), knot kept inside the tile with a dark halo, a butt joint on some planks (wrapped distance, so it tiles), lit left edge and shaded right edge per plank.
Fix:
static Color TimberPixel(int x, int y)
{
    float plankW = T / 8f; int plank = Mathf.FloorToInt(x / plankW); float px = x - plank * plankW;
    float streak = Paint(Fbm(x, y, .06f, 2, 1, .08f, plank * 53), 3) - .5f;   // long grain streaks
    float fine = Fbm(x, y, .3f, 1, 1, .05f, plank * 31) - .5f;
    float v = .72f + (Hash(plank, 5) - .5f) * .16f + streak * .16f + fine * .1f;
    if (Hash(plank, 13) > .45f)
    {   // a knot, kept clear of the tile's top and bottom
        float ky = (.12f + .76f * Hash(plank, 9)) * T, kx = (plank + .3f + .4f * Hash(plank, 17)) * plankW;
        float d = Mathf.Sqrt(Mathf.Pow((x - kx) / 9, 2) + Mathf.Pow((y - ky) / 14, 2));
        v *= d < 1 ? .55f + .3f * Mathf.Abs(Mathf.Sin(d * 7)) : 1 - .14f * Smooth(2.2f, 1, d);
    }
    if (Hash(plank, 29) > .4f)
    {   // a butt joint (distance wraps, so it tiles)
        float jd = Mathf.Abs(y - Hash(plank, 23) * T); jd = Mathf.Min(jd, T - jd);
        v *= jd < 1.5f ? .55f : 1 - .1f * Smooth(7, 1.5f, jd);
    }
    if (px < 1.5f || px > plankW - 1.5f) v *= .5f;   // the seam
    else if (px < 5) v *= 1.1f;                       // lit edge
    else if (px > plankW - 7) v *= .88f;              // shaded edge
    return new Color(v, v * .88f, v * .74f);
}

## [high] RockPixel: cracks are contour scribbles, bed values jump at the y seam, crack noise does not tile
Where: ZoneSceneBuilder.Painted.cs: RockPixel
Problem: abs(Fbm - .5) < .012 draws the noise's 0.5 iso-contour: closed loops and squiggles, with fat blobs where the gradient is flat (see v1_rock.png). It is the dominant feature and looks like scribble, not cracks. Fbm(x*1.6f, y*.35f, ...) is not seamless. Hash(bed, 21) uses the unwrapped bed index (0 at the bottom, 9 or -1 across the top via wobble), so bed values jump at the y seam (seamY 0.10 against 0.016 interior). Nine equal beds read as lined paper. Replacement: 6 beds of uneven thickness from a table (0.6-1.3 m at 5 m a tile), wobbled and wrapped, each bed split into 1-3 blocks by slanted, warped joints, per-block value, posterised patches plus a diagonal brush-plane layer, shadow at the bed's foot and a lit lip at its top. Known remainder: a joint line occasionally warps into a small loop. Tinted with (.4,.38,.35) the mean is about 0.30, dark; lift the zone tints if cliffs look murky in the build.
Fix:
static readonly float[] RockBeds = { 0, .13f, .36f, .48f, .74f, .86f, 1 };
static Color RockPixel(int x, int y)
{
    float wob = (Fbm(x, y, .006f, 2) - .5f) * 70 + (Fbm(x, y, .03f, 2) - .5f) * 14;
    float by = Mathf.Repeat((y + wob) / T, 1); int bed = 0; while (bed < 5 && by >= RockBeds[bed + 1]) bed++;
    float h = (RockBeds[bed + 1] - RockBeds[bed]) * T, f = (by - RockBeds[bed]) * T / h;   // h: bed thickness in px; f: 0 at its foot, 1 at its top
    float cols = 1 + Mathf.Floor(Hash(bed, 3) * 2.99f);
    float cx = x / (float)T * cols + Hash(bed, 11) + (Fbm(x, y, .02f, 2) - .5f) * .6f / cols + (f - .5f) * (Hash(bed, 31) - .5f) * .25f;   // warped, slanted joints
    float cr = Mathf.Floor(cx), fx = cx - cr; int col = (int)(cr - Mathf.Floor(cr / cols) * cols);
    float v = .78f + (Hash(col + bed * 31, bed) - .5f) * .14f + (Hash(bed, 21) - .5f) * .12f;
    float plane = Paint(Tiled((px, py) => Perlin(px * .9f + py * .44f, (py * .9f - px * .44f) * 3, .009f), x, y), 3) - .5f;   // broad diagonal brush planes
    v += (Paint(Fbm(x, y, .012f, 2), 4) - .5f) * .14f + plane * .1f + (Fbm(x, y, .07f, 2) - .5f) * .06f;
    v *= 1 - .34f * Smooth(14, 0, f * h);          // the shadow line at a bed's foot
    v *= 1 + .1f * Smooth(22, 3, (1 - f) * h);     // its worn, lit lip
    float jd = Mathf.Min(fx, 1 - fx) * T / cols;
    v *= 1 - .38f * Smooth(4, 1, jd);              // the joint
    if (fx < .5f && jd > 4 && jd < 9) v *= 1.06f;  // its lit side
    float warm = (Fbm(x, y, .004f, 2) - .5f) * .1f;
    return new Color(Mathf.Clamp01(v * (1 + warm)), Mathf.Clamp01(v), Mathf.Clamp01(v * (1 - warm * 1.2f)));
}

## [low] Tex() reuses an existing PNG, so changed pixel functions will not regenerate
Where: ZoneSceneBuilder.cs Tex (line 479: `if (!File.Exists(path))`), called from PaintedTextures in ZoneSceneBuilder.Painted.cs
Problem: The painted pass has never been built, so the first build is fine. But once any *_painted.png exists, later edits to the pixel functions are silently ignored. Iterating on the look in Unity will appear to do nothing.
Fix:
While iterating, delete ArtRoot/{plaster,thatch,slate,stone,timber,rock}_painted.png before running EnsureArt, or version the names when the functions change, e.g. Tex("plaster_painted2", T, PlasterPixel).

# refute:geometry:Door ring floats off the door -> isReal=True
Confirmed by reading D:\code\mmo\tools\wip\painted\patch_buildings.py lines 92-98 and Part() in ZoneBuilder.cs:842; nothing was built or run.

- Part() sets localScale directly. A Unity cylinder is 2 units tall, so scale y .015 gives half-length .015. Rotated 90 degrees about X, its axis lies along Z.
- Ring: centre z -.07, so it spans -.085 .. -.055.
- Door slab: centre `at`, depth .06, so its front face is at -.03.
- Seams: centre -.035, depth .01, so they span -.04 .. -.03.
- Bands: centre -.045, depth .02, so they span -.055 .. -.035. They sit at y = -.28*high and +.25*high, each .09 tall.
- For the house door (wide 1.2, high about 2.7) the ring is at y about -.135 +/- .07, and the nearest band is at -.756 +/- .045, so the ring is not on a band.

Result: the ring's back face is 2.5 cm in front of the planks. It overlaps the x = .3 seam in x (ring x .29 .. .43), and is 1.5 cm in front of that seam. It is a floating disc; small, but it will show as a gap and a detached shadow at a glancing view.

The proposed fix is correct. Centre z -.04 makes the ring span -.055 .. -.025: 5 mm into the planks (no z-fight with the face at -.03), and its front is level with the band fronts at -.055. The ring and bands do not overlap in y, so that causes no coplanar fight.
Corrected fix:
In patch_buildings.py PlankDoor (line 97), change the ring's z offset from -.07f to -.04f:
            Part(PrimitiveType.Cylinder, t, at + new Vector3(wide * .3f, -high * .05f, -.04f), new Vector3(.14f, .015f, .14f), art.metal, Quaternion.Euler(90, 0, 0));   // the ring: z -.055 .. -.025, 5 mm into the planks

# refute:geometry:Ridge cap overhangs the ridge with a -> isReal=True
Confirmed by reading the files, low severity (cosmetic, nothing was built or rendered). patch_buildings.py line 77: the cap is a cube centred at top+roofH+.04, 0.16 high, 0.34 deep, so its flat bottom is at ridge-.04 and its edges are at z=+-.17. ZoneMeshes.GableRoof (ZoneMeshes.cs:11-19) has its ridge at +height and eaves at z=+-depth/2; every caller passes depth d+1.4, so the slope surface at z=.17 is ridge - roofH*.17/(d/2+.7). House roofH=max(2.2,.55d): d=6 gives .152 below the ridge; inn roofH=max(2.4,.5d) and barn .5d give about .13-.15. So there is an open wedge about 9-11 cm high and 17 cm wide under each side of the cap. It shows from the gable ends, where the cap (w+1.3) overhangs the roof (w+1.2) by 5 cm. The proposed fix is geometrically correct for all three callers (house, inn, barn all use depth d+1.4): cap top at ridge+.05, bottom at ridge-sag-.02, so the bottom edges sink 2 cm into the slopes. The cap becomes about 0.2 m of visible side at its edges, a chunky ridge beam, which suits the style.
Corrected fix:
Replace line 77 of D:\code\mmo\tools\wip\painted\patch_buildings.py (inside Eaves) with:
            float sag = roofH * .17f / (d / 2 + .7f);   // how far the slope has dropped at the cap's edge
            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .05f - (sag + .07f) / 2, 0), new Vector3(w + 1.3f, sag + .07f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));

# refute:geometry:Rafter ends buried in the roof slab -> isReal=True
Confirmed by reading the files (nothing built or run).

- **Roof slab:** `ZoneMeshes.GableRoof` (ZoneMeshes.cs:11-24) is a closed slab with default thickness .25. Its flat underside is at y = top-.25 across the full depth, and its eave edge is at z = ±(d/2+.7), spanning top-.25..top.
- **Rafters buried:** `Eaves` in patch_buildings.py line 75 puts each rafter at centre top-.2 with height .14, so top-.27..top-.13. Its z range is d/2-.03..d/2+.71, which is under the overhang. Only the bottom 2 cm shows below the soffit; the other 12 cm is inside the slab.
- **Fascia:** line 74 spans top-.26..top-.02, so it only just covers the eave edge.
- **Spacing:** the loop is `x = -w/2+.2; x < w/2; x += .9`. For w=7 that gives x = -3.3 .. 3.0, which is .2 m from the left corner and .5 m from the right. It also accumulates float error.

The proposed fix checks out arithmetically:
- **Rafters:** top-.38..top-.24, so 13 cm visible and 1 cm into the soffit. z runs d/2-.02..d/2+.68, 1 cm into the fascia (inner face at d/2+.67).
- **Fascia:** top-.39..top-.01, which covers the rafter ends and the eave edge.
- **Series:** for w=7, n=7 gives 8 rafters at ±3.15, symmetric and inside the wall corners.

All three `Eaves` callers (House, Inn, Barn with w-.2) work with it.
Corrected fix:
In D:\code\mmo\tools\wip\painted\patch_buildings.py, in Eaves(), replace the `foreach (int sz ...) { ... }` block (lines 72-76) with:

            int n = Mathf.Max(1, Mathf.FloorToInt((w - .5f) / .9f));
            foreach (int sz in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, new Vector3(0, top - .2f, sz * (d / 2 + .72f)), new Vector3(w + 1.3f, .38f, .1f), dark);   // fascia: top-.39 .. top-.01
                for (int i = 0; i <= n; i++) Part(PrimitiveType.Cube, t, new Vector3((i - n / 2f) * .9f, top - .31f, sz * (d / 2 + .33f)), new Vector3(.12f, .14f, .7f), dark);   // rafter ends: top-.38 .. top-.24, hung under the soffit (top-.25)
            }

The ridge-cap line that follows stays unchanged.

# refute:geometry:art.masonry is a hard prerequisite; -> isReal=True
Confirmed by reading; could not refute.

1. Compile error (certain). ZoneArt.cs (Scripts\World\ZoneArt.cs:13-45) declares no `masonry` field, and a grep of all *.cs under Assets\Crulanda finds no "masonry". Nothing in tools\wip\painted adds it: patch_buildings.py patches only ZoneMeshes.cs and ZoneBuilder.cs, and patch_rock.py only notes "Apply after ZoneArt has `rock`". `art.masonry` is used in patch_buildings.py lines 105, 139, 140, 143, 145, 175 and in ZoneSceneBuilder.Painted.cs line 110, so both the runtime and editor assemblies fail to compile (CS1061).

2. Null at runtime (real once the field exists). `masonry` is assigned only in PaintedTextures(art) (Painted.cs:110), an editor step on the baked ZoneArt.asset. Until that runs and the asset is saved, the field is null. ZoneBuilder.Tint (ZoneBuilder.cs:816-821) dereferences `baseMat.name` with no null check, so lines 140 (house chimney cap) and 143 (inn footing) throw and abort the zone build. Lines 105, 139, 145, 175 pass null to BoxPart -> MeshPart, which renders magenta.

Related, same root cause, not in the report:
- `art.rock` (Painted.cs:113-116) also has no ZoneArt field.
- Painted.cs declares `public static partial class ZoneSceneBuilder`, but Editor\ZoneSceneBuilder.cs:16 is `public static class` (not partial) -> CS0260.
- Nothing in the prepared files calls PaintedTextures from EnsureArt, as far as the greps show; that hook must be added or masonry stays null forever.

The proposed fix is correct and minimal. The fallback to art.stone keeps unregenerated assets working.
Corrected fix:
ZoneArt.cs (Scripts\World), add after line 13:
        [Tooltip("Painted dressed stone for plinths, chimneys, footings and hearths (art.stone stays natural rock).")]
        public Material masonry;
(Add `public Material rock;` here too if part 2 / patch_rock is applied.)

ZoneBuilder.cs, directly above `Material Tint(Material baseMat, Color color)` (line 816):
        /// <summary>Dressed stone; falls back to natural stone until the art asset has been regenerated.</summary>
        Material Masonry { get { return art.masonry != null ? art.masonry : art.stone; } }

patch_buildings.py: replace all 6 occurrences of `art.masonry` (lines 105, 139, 140, 143, 145, 175) with `Masonry`. Leave Painted.cs:110 as `art.masonry` (it is the assignment).

Also required for the pass to compile and take effect:
- Editor\ZoneSceneBuilder.cs:16: `public static class` -> `public static partial class`.
- Call `PaintedTextures(art);` in EnsureArt after the base materials are ensured.
- Run Crulanda/World/Build Oakhaven (EnsureArt) in the validation copy before tests so ZoneArt.asset has masonry assigned.

# refute:geometry:Shutter runs into the corner post on -> isReal=True
Confirmed by arithmetic, but it is minor and affects exactly one house in the zone data.

- **Shutter extent:** `Window` (patch line 90) places each shutter at `at.x ± (wide/2 + .26)` with width .3, so for `wide = .8` its outer edge is |x| + .81.
- **Corner post:** `ZoneBuilder.cs:1041` puts a .3 m post at ±w/2, so its inner face is at w/2 − .15.
- **7.5 m house:** the window loop (patch line 128) yields x = −2.35 and +2.85. The +2.85 window's outer shutter spans x 3.36..3.66 against a post face at 3.60, so it sits 6 cm inside the post. In z the shutter (d/2+.065..d/2+.115) lies within the post (up to d/2+.15), so the outer 6 cm is buried, on both the front and back walls. There are no coplanar faces, so no z-fighting; the shutter just looks 20% narrower than its twin.
- **Scope:** house sizes in the zone JSON are 6, 6.5, 7 (also the default and the mill), 7.5, 8 and 9 wide. Only the single 7.5 x 5 house is hit; every other size clears the post by 0.44 m.
- **6 m house claim:** the inner shutter edge is at 0.79 and the door jamb's outer edge at 0.76, a 3 cm gap. That is not an overlap, and the jamb stands proud at z −d/2−.12±.12, so there is no defect there.
- **Inn:** `Inn` uses its own window loop (patch lines 163-164); I did not check it.

The reviewer's proposed condition uses .79 where the real shutter half-extent is .81, and its second clause (door clearance) is never false for any size in the data.
Corrected fix:
In D:\code\mmo\tools\wip\painted\patch_buildings.py line 132 (replacement text for the House window loop), change the last argument from `true` to a post-clearance test:

    Window(t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * (d / 2 + .05f)), .8f, .7f, sz, variant, Mathf.Abs(x) + .81f <= w / 2 - .15f);

.81 = wide/2 + .26 + .15, the shutter's outer edge. This drops the shutters only on the x = +2.85 window of the 7.5 m house (front and back); sizes 6, 6.5, 7, 8 and 9 keep all of theirs. No door-side clause is needed: the minimum inner edge is 0.79 against a jamb edge of 0.76.

Alternative if the lopsided look (one window shuttered, one not) is unwanted: keep `true` and narrow the house shutters instead, which needs a Window signature change, so the one-line condition above is the smallest fix.

# refute:geometry:Shutters, window frames and sills fl -> isReal=True
Confirmed by arithmetic on patch_buildings.py Window() (lines 79-91) against ZoneBuilder.cs; nothing was built or run.

HOUSE (wall is a w x wallHeight x d box, outer face at z = d/2; call at d/2+.05):
- Glass: d/2+.01 .. +.09, same as today.
- Frame: centre +.08, thickness .12, so +.02 .. +.14. Back is 2 cm off the wall.
- Sill: centre +.13, thickness .22, so +.02 .. +.24. Back is 2 cm off the wall.
- Shutter: centre +.09, thickness .05, so +.065 .. +.115. Back is 6.5 cm off the wall, attached to nothing (it sits beside the frame in x, inner edge wide/2+.11 vs frame outer wide/2+.08). This is the visible defect: a see-through gap at glancing angles plus a detached shadow.

INN (ZoneBuilder.cs:1091 wall = .3; walls centred at +-d/2, so outer face at d/2+.15; existing through-wall glass at lines 1131-1132 is wall+.1 = .4 thick, front at d/2+.20; calls at d/2+.17):
- Second glass pane: +.13 .. +.21, front 1 cm proud of the existing glass, same width and height (.9x.8 and .8x.7), so its side faces are coplanar over z .13 .. .20. Same material, so mostly invisible, but it is a redundant duplicate pane.
- Frame: +.14 .. +.26, embedded 1 cm in the wall. Fine.
- Shutter: centre +.21, so +.185 .. +.235. Back is 3.5 cm off the wall.

All the reviewer's numbers check out. Severity is low to moderate: the frame and sill gaps are hairline, the floating shutters are the real problem.

The proposed fix is correct as given: house frame face-.02 .. +.12, sill face-.02 .. +.24, shutter face-.01 .. +.05, glass unchanged at d/2+.01 .. +.09. On the inn (face = d/2+.15, glass:false) the frame is +.13 .. +.27 around the existing glass front at +.20, shutter +.14 .. +.20, no second pane. Shutter inner edge (wide/2+.09) clears the frame outer edge (wide/2+.08).

The fix's extra house shutter condition is also justified: the loop allows x up to w/2-.8, so with the draft's unconditional `true` a shutter's outer edge (|x|+.79) can run into the .3 corner post (inner face w/2-.15) or the door jamb (outer edge .76).

One thing the fix does not cover: the inn ground-floor call still passes shutters=true unconditionally. Shutter outer edge is |x|+.84; the loop allows x up to w/2-.8 (corner post inner face at w/2-.17), and on the door side |x| >= 1.6 gives an inner edge at .76 against the .8 door opening. Add the same kind of guard there.
Corrected fix:
Apply the reviewer's fix unchanged for Window() and the two House calls. For the Inn calls, additionally guard the ground-floor shutters against the corner posts and the door opening:

        void Window(Transform t, Vector3 face, float wide, float high, int sz, int variant, bool shutters, bool glass = true)
        {
            var frame = Tint(art.timber, new Color(.24f, .16f, .1f));
            var at = face + new Vector3(0, 0, sz * .05f);
            if (glass) Part(PrimitiveType.Cube, t, at, new Vector3(wide, high, .08f), art.glass);
            foreach (int s in new[] { -1, 1 })
            {
                Part(PrimitiveType.Cube, t, at + new Vector3(0, s * (high / 2 + .04f), 0), new Vector3(wide + .16f, .08f, .14f), frame);
                Part(PrimitiveType.Cube, t, at + new Vector3(s * (wide / 2 + .04f), 0, 0), new Vector3(.08f, high, .14f), frame);
            }
            Part(PrimitiveType.Cube, t, face + new Vector3(0, -high / 2 - .1f, sz * .11f), new Vector3(wide + .3f, .08f, .26f), frame);   // the sill
            if (shutters) foreach (int s in new[] { -1, 1 })
                Part(PrimitiveType.Cube, t, face + new Vector3(s * (wide / 2 + .24f), 0, sz * .02f), new Vector3(.3f, high + .1f, .06f), Tint(art.timber, Shutter[Mathf.Abs(variant) % Shutter.Length]));
        }

House calls (patch lines 132, 134):
                    Window(t, new Vector3(x, .6f + wallHeight * (inn ? .3f : .55f), sz * d / 2), .8f, .7f, sz, variant, Mathf.Abs(x) + .79f <= w / 2 - .15f && Mathf.Abs(x) - .79f >= .78f);
                    Window(t, new Vector3(x, .6f + wallHeight * .78f, sz * d / 2), .8f, .7f, sz, variant, false);

Inn calls (patch lines 163, 164; keep the existing through-wall glass Parts, add no second pane):
                    Window(t, new Vector3(x, 1.5f, sz * (d / 2 + wall / 2)), .9f, .8f, sz, variant, Mathf.Abs(x) + .84f <= w / 2 - .17f && (sz > 0 || Mathf.Abs(x) - .84f >= doorW / 2 + .05f), false);
                    Window(t, new Vector3(x, storey + 1.3f, sz * (d / 2 + wall / 2)), .8f, .7f, sz, variant, false, false);

# refute:rock:ZoneArt has no rock (or masonry) fie -> isReal=True
Confirmed by reading the files. D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneArt.cs (lines 13-45) declares no `masonry` and no `rock` field. The prepared pass uses both: ZoneSceneBuilder.Painted.cs:110 (art.masonry), :113-116 (art.rock); patch_buildings.py:105,139,140,143,145,175 (art.masonry); patch_rock.py:18 (art.rock). A case-insensitive grep of all six files in D:\code\mmo\tools\wip\painted (docs_world.py, docs_rest.py, patch_buildings.py, patch_rock.py, PaintedRock.shader, ZoneSceneBuilder.Painted.cs) finds no edit targeting ZoneArt.cs; patch_rock.py:3 only states the precondition "Apply after ZoneArt has `rock`". As prepared, this is a compile error (CS1061) in both the Editor and runtime assemblies. Second part also real: Painted.cs:115 passes Shader.Find("Crulanda/PaintedRock") straight into new Material(...); if the shader is not imported/compiled yet, Shader.Find returns null and the constructor throws ArgumentNullException, aborting EnsureArt. Not run or built (per the rules); verified by reading only.
Corrected fix:
1) ZoneArt.cs: add after line 45 (`public Material fern, broadLeaf, reeds;`):
        [Tooltip("Painted coursed masonry for built stone (footings, chimneys, hearths). art.stone stays natural rock.")]
        public Material masonry;
        [Tooltip("Crulanda/PaintedRock: natural rock (crags, cliffs, boulders), world-projected strata with lit tops. Null = ZoneBuilder falls back to stone.")]
        public Material rock;
(Append at the end; new serialized fields default to null on the existing ZoneArt asset, which the `== null` creation guards in Painted.cs rely on.)

2) ZoneSceneBuilder.Painted.cs lines 113-117, replace with:
            if (art.rock == null)
            {
                var rockShader = Shader.Find("Crulanda/PaintedRock");
                if (rockShader == null) Debug.LogWarning("Crulanda/PaintedRock is not imported yet: rock stays on art.stone (run the art build again).");
                else
                {
                    art.rock = new Material(rockShader) { name = "Painted rock", mainTexture = Tex("rock_painted", T, RockPixel) };
                    AssetDatabase.CreateAsset(art.rock, ArtRoot + "/Painted rock.mat"); EditorUtility.SetDirty(art);
                }
            }
patch_rock.py's Rock() already falls back to art.stone when art.rock is null, so no change there.

3) patch_buildings.py uses art.masonry unguarded (lines 105, 139, 140, 143, 145, 175). If ZoneBuilder can run before EnsureArt has been re-run (art.masonry null on the existing asset), Tint(art.masonry, ...) / BoxPart(..., art.masonry, ...) get a null material. Smallest guard: add a ZoneBuilder helper `Material Masonry => art.masonry != null ? art.masonry : art.stone;` and use it in those six places, mirroring Rock().

# refute:rock:Helper name Rock collides with Cliff -> isReal=True
Confirmed by reading; not compiled. ZoneBuilder.cs:2034 `void Cliff(...)` declares the local function `void Rock(float x, float bottom, float z, float sx, float sy, float sz, float lean, float roll, float yaw)` at :2042, in the same method block as the line patch_rock.py rewrites (:2036 `var stone = Tint(art.stone, ...)` -> `var stone = Rock(...)`). A local function's name is in scope for the whole enclosing block, including before its declaration, and simple-name lookup stops at the block, so it never reaches the new class method `Material Rock(Color)`. The patched :2036 therefore binds to the 9-parameter void local with 1 argument: a compile error (CS7036/CS1501; it also returns void into `var`). The local also captures `stone`, so it cannot simply be moved. The other 7 patched call sites (ZoneBuilder.cs :2718, :2887, :3030, :3635, :3929, :4091 and ZoneBuilder.Verdant.cs) are outside Cliff and would compile; only Cliff breaks, but that breaks the assembly. `RockMat` is free: no `RockMat` identifier exists in any .cs or .py under D:\code\mmo (the local `rockMat` at :4091 differs by case and is a variable inside another method, so there is no conflict).
Corrected fix:
In D:\code\mmo\tools\wip\painted\patch_rock.py rename the helper and all its calls from `Rock(` to `RockMat(` in the replacement strings only (the search strings stay as they are). Leave Cliff's local `Rock` untouched.

Definition (line 18):
`        Material RockMat(Color color) { return Tint(art.rock != null ? art.rock : art.stone, color); }`

Calls (8, replacement side):
- line 22: `var stone = RockMat(Zone.biome == "ash" ? ...` (Cliff, :2036)
- line 24: `var mat = RockMat(Zone.biome == "mountain" ? MountainStone : ...` (:2718)
- line 26: `... : RockMat(Zone.biome == "meadow" ? ...` (:2887)
- line 28: `var loose = RockMat(new Color(.4f, .38f, .35f));` (:3030)
- line 30: `return RockMat(c * shade);` (:3635)
- line 32: `var mat = RockMat(Zone.biome == "ash" ? ...` (:3929)
- line 34: `var rockMat = RockMat(ash ? ...` (:4091; legal, local variable `rockMat` vs method `RockMat` differ by case)
- line 38 (ZoneBuilder.Verdant.cs): `var rock = RockMat(new Color(.44f, .44f, .42f)); var mossy = RockMat(new Color(.3f, .38f, .22f));`

Minimal alternative if only one line should change: keep the helper named `Rock` and write line 22 as `var stone = this.Rock(Zone.biome == ...)`, since `this.` bypasses the local function. Renaming to RockMat is cleaner and avoids the same trap in any later method with a local `Rock`.

# refute:rock:_Top is one global warm value: wrong -> isReal=True
Confirmed by reading the files; could not refute.

- PaintedRock.shader:13 declares `_Top = (1.18, 1.14, 1.02, 1)` and line 4's comment says the top light is "by zone: _Top".
- Nothing sets `_Top` anywhere: a grep of D:\code\mmo (*.cs, *.py, *.shader) finds it only in the shader itself. ZoneSceneBuilder.Painted.cs:115 creates a single `art.rock` asset with only mainTexture set. The patch_rock.py helper is `Material Rock(Color color) { return Tint(art.rock != null ? art.rock : art.stone, color); }`, and Tint (ZoneBuilder.cs:816) clones with only `color` changed. Every zone therefore gets the warm default.
- Shader line 42 is `c = lerp(c, c * _Top.rgb, pow(saturate(n.y), 2.5))`. On ash rock (.37, .365, .37) an upward face becomes (.437, .416, .377): R/B goes from 1.00 to 1.16, a warm sandstone cast. That contradicts the comment kept on ZoneBuilder.cs:2036, "ash: grey, not sandstone". Ash boulders (.33,.32,.31) and (.36,.355,.36) are affected the same way.
- Mountain: tops gain 18% R, 14% G and 2% B (about 14% luma; the report's "about 10%" understates it) on rock deliberately darkened so that "a sunlit crag stays under the fog" (ZoneBuilder.cs:2036, 490, 3919).
- Gloom (ZoneBuilder.cs:1301, greys living colour via Wither) also gets the warm top.

Caveats:
- This is a look issue in a draft that has never been rendered, so the severity is a judgement; the mechanism and the contradiction with the code's stated intent are concrete.
- Not checked: the project's colour space. If it is Linear, a non-[HDR] Color property is gamma-decoded and the 1.18 multiplier may be stronger than computed; making `_Top` a Vector avoids that.
- Not checked: whether the `tints` dictionary is per ZoneBuilder instance. The proposed fix reuses the name "Painted rock" for the per-zone clone, which would collide across biomes if the cache is shared; the corrected fix puts the biome in the name.
- The proposed fix names the helper `RockMat`, but patch_rock.py's call sites all use `Rock(...)`.
Corrected fix:
1. In D:\code\mmo\tools\wip\painted\patch_rock.py, replace the helper line (line 18) inside the first replacement string. Keep the name `Rock` so the existing call sites still match:

        Material rockBase;
        /// <summary>Natural rock in a zone's tint: the painted rock (world-projected strata, tops lit by biome), or plain stone if the art has none.</summary>
        Material Rock(Color color)
        {
            if (art.rock == null) return Tint(art.stone, color);
            if (rockBase == null)
            {
                rockBase = new Material(art.rock) { name = "Painted rock " + Zone.biome };   // the biome in the name keeps Tint's key apart per zone
                rockBase.SetVector("_Top", Zone.biome == "ash" || Gloom ? new Vector4(1.08f, 1.08f, 1.09f, 1) : Zone.biome == "mountain" ? new Vector4(1.08f, 1.06f, 1f, 1) : new Vector4(1.18f, 1.14f, 1.02f, 1));   // ash and gloom: neutral, grey stays grey; mountain: a small lift, the crag stays under the fog
            }
            return Tint(rockBase, color);
        }

2. In D:\code\mmo\tools\wip\painted\PaintedRock.shader:
   - line 13: `_Top ("Top light (multiplies upward faces; set per zone by ZoneBuilder.Rock)", Vector) = (1.18, 1.14, 1.02, 1)` (Vector, so there is no colour-space conversion and no clamp on values above 1)
   - lines 26-27: `fixed4 _Color; half4 _Top;`

The _Top values are the reviewer's proposals and are untested; tune them on the first build.

# refute:rock:Brightness premise is wrong: do not -> isReal=True
The facts are confirmed, but the prepared files already comply, so no code change is needed for tints.

Confirmed:
- `Assets\Crulanda\World\Art\stone_blotch.png` measures mean 0.8325, p5/50/95 = .725/.839/.918 (Pillow). The .68 at `Editor\ZoneSceneBuilder.cs:100` is only the base before `+Perlin*.25 +Perlin*.1`.
- `ProjectSettings.asset` has `m_ActiveColorSpace: 0` (Gamma), so multipliers are literal.
- `Tint()` at `Scripts\World\ZoneBuilder.cs:816` replaces the material colour (`new Material(baseMat) { color = color }`), so the zone tint is the whole multiplier for both the old stone and the new rock.
- `RockPixel` has base .8, with the bed shadow and lip roughly cancelling. I did not simulate it; about 0.8 against 0.833 is consistent with the reported x0.97 on sides.
- `PaintedRock.shader` defaults `_Top` to (1.18, 1.14, 1.02) and `_Shade` to 0.4, which gives tops x(1.14, 1.11, 0.99) and undersides x0.58, as reported.

So scaling tints by .68/.8 = 0.85 would darken every crag about 15% for no reason.

Nothing in the prepared files does that: `patch_rock.py` carries every tint over unchanged from `Tint(art.stone, X)` to `Rock(X)`, and there is no 0.85 or .68 scaling anywhere in `tools\wip\painted`. The issue is a correct warning against a premise in the task note, not a defect in the files.

The remaining risk is separate: mountain tops come out about 14% brighter against the "sunlit crag stays under the fog" comment. That belongs to the `_Top` issue.
Corrected fix:
No tint change: keep `patch_rock.py` as written (every `Rock(...)` colour identical to the old `Tint(art.stone, ...)` colour) and do not apply any .68/.8 scaling.

Handle mountain top brightness through `_Top` per biome (the separate `_Top` issue). Only if `_Top` stays global, use the reviewer's tint-only fallback for mountain, which is about x0.91 so tops land near today's value:
- Cliff: `new Color(.32f, .305f, .28f)` (replaces `.35f, .335f, .31f`)
- `MountainStone`: `new Color(.33f, .32f, .305f)` — I did not read its current value; check it before applying.

# refute:rock:Top projection shows zone-wide paral -> isReal=True
Confirmed by reading the code; nothing was run or rendered, and I did not reproduce the reviewer's std figures.

- D:\code\mmo\tools\wip\painted\ZoneSceneBuilder.Painted.cs, RockPixel (lines 121-134): the texture is horizontal beds by construction. `beds = 9` per tile, each with its own value (`Hash(bed,21)`, +/-0.11), a 30% dark shadow line at the bed foot and a 10% lit lip. All of these depend on y only, apart from a slow wobble (nominal +/-30 px, less in practice, against a 57 px bed). The only x-dependent structure is mottling and sparse cracks.
- D:\code\mmo\tools\wip\painted\PaintedRock.shader line 38-40: `p = worldPos / _Scale` with `_Scale = 5`, and the top sample is `tex2D(_MainTex, p.xz)` with no rotation. Texture v maps to world Z, so the bed lines are constant in Z and run along world X, 5 m / 9 = 0.556 m apart.
- Line 37: `w = pow(abs(n), 4)` normalised, so any mostly-upward face is almost purely the top projection.
- The projection uses world position only (the header says so, for static batching), so the stripes have the same direction and phase on every rock in every zone. patch_rock.py routes all natural rock through this material: cliffs, crags, boulders, edge rocks, knolls, cairns and the Verdant falls.

The shader comment shows the author expected "drifts", but dark parallel lines every 0.55 m aligned to one world axis will read as planks or furrows. The issue is real.

The proposed fix is sound: the first rotation is exact (0.8/0.6), the second is near-unit (0.34^2 + 0.94^2 = 0.9992), and averaging two samples at different angles and scales halves the line contrast and breaks the single direction. It leaves a faint cross-hatch of two line sets at about 0.93 m and 0.68 m spacing; if that still shows in a build, add a third sample or reduce the top's contrast.
Corrected fix:
In PaintedRock.shader surf(), replace lines 39-40 with:

            // Side projections keep world height as v, so the strata lie level. The top takes two turned samples, so the beds do not show as parallel stripes along world X.
            float2 t = float2(p.x * 0.8 + p.z * 0.6, p.z * 0.8 - p.x * 0.6) * 0.6;
            float2 t2 = float2(t.x * 0.34 - t.y * 0.94, t.x * 0.94 + t.y * 0.34) * 1.37 + 0.31;
            fixed3 top = (tex2D(_MainTex, t).rgb + tex2D(_MainTex, t2).rgb) * 0.5;
            fixed3 paint = tex2D(_MainTex, p.zy).rgb * w.x + top * w.y + tex2D(_MainTex, p.xy).rgb * w.z;

Four samples in total, which is fine for target 3.0. No other lines change.

# refute:rock:RockPixel does not tile: straight se -> isReal=True
Confirmed by reading D:\code\mmo\tools\wip\painted\ZoneSceneBuilder.Painted.cs:121-134 and PaintedRock.shader. I ran only the hash arithmetic in Python; no texture was rendered.

(1) Bed seam is real, and larger than reported. `wobble` is up to +/-30 px and tiles, so across the wrap (y=511 -> y=0) `by` is continuous but `bed` runs outside 0..8 and `Hash(bed, 21)` is not periodic. The reviewer's hash values are wrong; the actual ones (C# uint arithmetic reproduced) are:
- Hash(-1,21)=.288 vs Hash(8,21)=.897 where wobble<0: a step in v of .61*.22 = .134 (about 17% of the .8 base).
- Hash(9,21)=.554 vs Hash(0,21)=.314 where wobble>0: a step of .053.
The step lies exactly on the tile edge, so it is a dead-straight horizontal line. PaintedRock.shader samples `worldPos / _Scale` with `_Scale` = 5 and uses p.zy and p.xy, so it shows every 5 m of height on cliffs.

(2) Crack tiling failure is real. `Fbm(x*1.6f, y*.35f, ...)` passes scaled coordinates into `Tiled`, which computes u = x/T, v = y/T. u reaches 1.6 (extrapolation with negative weights) and v only reaches .35, so the function is not periodic over the 512 px tile in either axis. Cracks are cut at both edges.

(3) Crack coverage and hardness is a subjective tuning point; I did not simulate it. Line 131 does jump from x.79 to x1 at crack=.012, so the hard edge is real; the proposed Smooth removes it.

ThatchPixel line 52 (`Fbm(x * 1f, y * .18f, .25f, 2)`) has the same defect vertically: v only reaches .18, so there is a seam at the top and bottom of every 2.5 m roof tile. Line 50 `Fbm(x + row * 97, y, ...)` also pushes u out of range, but that only affects the ragged row edge and is minor.

The proposed fix is correct in form: `Tiled` wraps a lambda that does the anisotropic scaling inside, so the blend arguments stay in [0,T). `Tex()` (ZoneSceneBuilder.cs:479-493) only writes when the PNG is missing, so any rock_painted.png or thatch_painted.png already generated must be deleted. The pass has never been built, so they probably do not exist yet.
Corrected fix:
In D:\code\mmo\tools\wip\painted\ZoneSceneBuilder.Painted.cs:

Line 125 (wrap the bed index so beds -1 and 9 map to 8 and 0):
    float v = .8f + (Hash(((bed % 9) + 9) % 9, 21) - .5f) * .22f;

Lines 130-131 (scale inside the Tiled lambda; soft edge):
    float crack = Mathf.Abs(Tiled((px, py) => Perlin(px * 1.6f, py * .35f, .013f) * .65f + Perlin(px * 1.6f, py * .35f, .027f) * .35f, x, y) - .5f);
    v *= Mathf.Lerp(.55f, 1, Smooth(0, .008f, crack));

Line 52, ThatchPixel (same misuse):
    float straw = Tiled((px, py) => Perlin(px, py * .18f, .25f) * .65f + Perlin(px, py * .18f, .52f) * .35f, x, y) - .5f;

If rock_painted.png or thatch_painted.png already exist under ArtRoot, delete them (with their .meta only if the material is re-pointed), because Tex() never regenerates. Optional: narrow the crack threshold (.008 -> about .005) if a first render shows too many cracks.

# refute:rock:Natural-rock sites the patch misses -> isReal=True
Confirmed by reading patch_rock.py against ZoneBuilder.cs. The patch switches only 7 sites in ZoneBuilder.cs (2036 Cliff, 2718 EdgeRock boulders, 2887 knoll, 3030 loose, 3635 SecretStone, 3929, 4091) plus the Verdant falls. The reported sites all stay on art.stone:

- 916/918: the "rock" prop draws Boulder() lumps with `art.stone`, or `Tint(art.stone, MountainStone)` in mountain zones. The edge boulders at 2718/3929 use the same mesh and same MountainStone and do go painted.
- 2772: the Cave face uses `Tint(art.stone, (.37,.365,.37))`, the exact colour of the ash Cliff at 2036, and its own comment says this is so the face runs on into the Ridge. 2036 is patched, 2772 is not, so the two would differ in shader and texture side by side.
- 3567: the greyed cairn top is `Tint(art.stone, ...)`, while the lower two stones come from SecretStone() (3762), which is patched.
- 2738: the Wallow's `stone` is used only for the two Boulder() lumps at 2758.
- 3346 and 3774 (optional): Campfire ring stones and SecretBeacon `burnt` stones are BoulderAt lumps on art.stone. `soot` also paints the flat charred disc at 3776, so it should stay.

Two corrections to the reviewer's fix:
- The helper the patch adds is `Rock(Color)`, not `RockMat`; `RockMat` would not compile.
- The 2738 search string `var stone = Tint(art.stone, new Color(.42f, .4f, .37f));` also occurs at 1188 (a chimney, which must stay masonry), so patch_rock.py's `count == 1` assert would fail. It needs the `var wet = ...` prefix as context.

The base colour (.52,.51,.48) for 916 matches Stone.mat (Editor/ZoneSceneBuilder.cs:111). `Rock()` falls back to art.stone when art.rock is null, so the change is safe. Nothing was built or run; matching tint does not guarantee matching brightness, since the painted texture's luminance differs from the stone texture's.
Corrected fix:
Append these pairs to the ZoneBuilder.cs list in D:\code\mmo\tools\wip\painted\patch_rock.py (helper is Rock, not RockMat; each search string occurs exactly once in the current file):

("""float s = 1 + p.variant * .6f; var stone = art.stone;""",
 """float s = 1 + p.variant * .6f; var stone = Rock(new Color(.52f, .51f, .48f));"""),
("""stone = Tint(art.stone, MountainStone); if (string.IsNullOrEmpty(p.interact))""",
 """stone = Rock(MountainStone); if (string.IsNullOrEmpty(p.interact))"""),
("""var wet = Tint(art.soil, new Color(.16f, .12f, .085f)); var stone = Tint(art.stone, new Color(.42f, .4f, .37f));""",
 """var wet = Tint(art.soil, new Color(.16f, .12f, .085f)); var stone = Rock(new Color(.42f, .4f, .37f));"""),
("""var rock = Tint(art.stone, new Color(.37f, .365f, .37f)); var dark""",
 """var rock = Rock(new Color(.37f, .365f, .37f)); var dark"""),
("""Unmade(at.x, at.y) > .2f ? Tint(art.stone, new Color(.6f, .6f, .62f)) : null""",
 """Unmade(at.x, at.y) > .2f ? Rock(new Color(.6f, .6f, .62f)) : null"""),

Optional, for consistency with the painted SecretStone:
("""var stone = Tint(art.stone, new Color(.34f, .32f, .3f)); var wood""",
 """var stone = Rock(new Color(.34f, .32f, .3f)); var wood"""),
("""var burnt = Tint(art.stone, new Color(.28f, .26f, .24f));""",
 """var burnt = Rock(new Color(.28f, .26f, .24f));"""),

Leave `dark` and `salt` (2772) and `soot` (3774) on art.stone. Do NOT use the bare string `var stone = Tint(art.stone, new Color(.42f, .4f, .37f));` for the Wallow: it also matches line 1188 (chimney) and trips the count==1 assert.
