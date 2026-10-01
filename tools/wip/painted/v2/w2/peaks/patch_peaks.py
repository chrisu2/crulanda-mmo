"""The painted style pass, worklist item 13 (the Peaks: steep ground and the crags' seating): patch.
   Usage: python patch_peaks.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Scripts\\World\\ZoneBuilder.cs (the only file)
     MountainBare, CragApron,   new, after MountainRock. MountainBare: how bare a point is (rocky patches, steep ground from 27
     MountainGround             degrees, the scree run-out under a steep face, the apron round a crag); the paint and the grass
                                both read it. CragApron: 1 within 3.5 m of a cliff prop's line, 0 from 8 m. MountainGround: the
                                paint under the turf: warm scree with stones and grit, bedded grey rock on ground over about 36
                                degrees (strata keyed on the drawn ground's height, so they run level round a hump: beds .8 to
                                1.8 m, a shadow line at each foot, a lit lip, joints, a tone a bed), darker gullies and lighter
                                crests (the ground's curvature), darker scree in the rock's own grey round a crag's foot.
     BuildGround                the mountain detail mask: half grain on turf (as before), nearly full on rock and scree.
     PaintGround                the mountain branch only: the comment and the steep / rock / shade / rockC / scree lines above
                                the alp line, and the blend line below it. THE ALP LINE ITSELF IS NOT TOUCHED (the green batch
                                edits it). Turf now ends on a ragged, crisp edge and only on the gentler ground.
     Openness                   the mountain turf line: grass thins by MountainBare, so none grows on the new scree or aprons.
     Cliff                      mountains: a step's base lump is sunk till its flat base is .4 m under the lowest ground beneath
                                its footprint (it grows down, its top stays); a free crag gets scree behind it too and more in
                                front (.8, was .5). Other biomes build what they did.
     EdgeRock, the rock prop    mountains: the boulder's lumps sit a seventh deeper (the collider stays) inside a skirt of
     RockSkirt (new)            small half-sunk stones.

   No draw is added to or taken from the zone's random stream: Cliff's new choices use the crag's own stream (after everything
   it drew before), RockSkirt a stream keyed to where the boulder stands, the paint position noise and texel hashes. No
   collider, navmesh source, light or registered object changes: every new mesh is render-only.

   Everything is checked before anything is written: every old block must occur exactly once in its file. So a second
   run, or a run on a tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
ZB = r'Scripts\World\ZoneBuilder.cs'

# (file, what, old, new)
EDITS = [
# ---------------------------------------------------------------- the helpers, after MountainRock ----------------------------------------------------------------
(ZB, 'MountainBare, CragApron and MountainGround (after MountainRock)',
r'''            return (Mathf.PerlinNoise(x * .045f + 90, z * .045f + 17) - .45f) * 2.4f + edge * .6f;
        }
''',
r'''            return (Mathf.PerlinNoise(x * .045f + 90, z * .045f + 17) - .45f) * 2.4f + edge * .6f;
        }
        /// <summary>Mountain ground: how bare a point is before the paint's ragged edge (under .5 turf, over it scree and rock):
        /// the rocky patches (MountainRock), steep ground (half way at 27 degrees), the scree run-out under a steep face
        /// (<paramref name="fan"/>: 1 where the ground 3 m uphill, or less so 7 m uphill, is a face) and the apron round a crag.
        /// The paint and the grass both read it. Draws nothing random.</summary>
        float MountainBare(float x, float z, out float fan)
        {
            float steep = Mathf.Clamp01((1 - UpAt(x, z) - .035f) * 7); var n = NormalAt(x, z); float len = Mathf.Sqrt(n.x * n.x + n.z * n.z); fan = 0;
            if (len > .03f)
            {
                float ux = -n.x / len, uz = -n.z / len;   // uphill
                fan = Mathf.Max(Mathf.Clamp01((1 - UpAt(x + ux * 3, z + uz * 3) - .13f) * 8), Mathf.Clamp01((1 - UpAt(x + ux * 7, z + uz * 7) - .13f) * 8) * .8f);
            }
            return MountainRock(x, z) + Mathf.Max(steep, fan * .62f) + CragApron(x, z) * 1.2f;
        }
        (Vector2 a, Vector2 b)[] cragFeet;   // each mountain cliff prop's line through the middle of its lumps
        /// <summary>Mountains: how close a point is to a crag (a cliff prop): 1 within 3.5 m of the line through its lumps, 0 from
        /// 8 m and in every other biome. The ground there is painted bare, as scree in the rock's grey, and grows no grass, so a
        /// crag stands in its own fall of rock. BuildGround's detail mask asks first, on the main thread, so the lines are built
        /// before the paint's rows ask in parallel.</summary>
        float CragApron(float x, float z)
        {
            if (cragFeet == null)
            {
                var feet = new List<(Vector2, Vector2)>();
                if (Zone.biome == "mountain")
                    foreach (var p in Zone.props)
                    {
                        if (p == null || p.kind != "cliff") continue;
                        float r = p.rotation * Mathf.Deg2Rad, s = p.scale <= 0 ? 1 : p.scale;
                        Vector2 along = new Vector2(Mathf.Cos(r), -Mathf.Sin(r)) * ((p.size.x > 0 ? p.size.x : 20) / 2 * s), mid = p.at + new Vector2(Mathf.Sin(r), Mathf.Cos(r)) * ((p.lift != 0 ? Mathf.Sign(p.lift) * .8f : 1.6f) * s);
                        feet.Add((mid - along, mid + along));
                    }
                cragFeet = feet.ToArray();
            }
            float best = 0; var q = new Vector2(x, z);
            foreach (var (a, b) in cragFeet)
            {
                if (Mathf.Abs(x - (a.x + b.x) / 2) > Mathf.Abs(a.x - b.x) / 2 + 8 || Mathf.Abs(z - (a.y + b.y) / 2) > Mathf.Abs(a.y - b.y) / 2 + 8) continue;
                best = Mathf.Max(best, 1 - Mathf.SmoothStep(0, 1, (Segment(q, a, b) - 3.5f) / 4.5f));
            }
            return best;
        }
        static readonly float[] RockBedTops = { 1f, 2.6f, 3.4f, 5.2f, 6.3f, 8f };   // the tops of the six beds in an 8 m run of strata (.8 to 1.8 m thick)
        /// <summary>
        /// Mountain ground paint under the turf at a point (texel i, j; n1, n2, n3 are the paint's three noises): scree, or bedded
        /// rock where the ground is a face (half way at 36 degrees, the edge broken by noise). <paramref name="rock"/> is how much
        /// of it shows through the turf: 0 or 1 but for a narrow ragged edge about MountainBare = .5, broken at 2 m, half a metre
        /// and 25 cm. <paramref name="hollow"/> is the turf's shade (darker in a gully, a touch lighter on a crest).
        /// Scree: the warm gravel, with pale stones, dark gaps and 25 cm grit; darker and in the rock's own grey round a crag's
        /// foot. Rock: strata keyed on the drawn ground's height, so on the top-down paint they run level round every hump and
        /// keep their thickness on any slope: beds .8 to 1.8 m thick that dip a little and wander, each with its own tone, a
        /// shadow line at its foot (faint along some stretches), a worn lit lip and a dark joint every few metres. Gullies (the ground lower than its surroundings 3 m out) are
        /// darker, crests lighter. Reads only the ground grid and the zone's data, so the paint's rows may ask in parallel.
        /// </summary>
        Color MountainGround(float x, float z, int i, int j, float n1, float n2, float n3, out float rock, out float hollow)
        {
            float y = MeshY(x, z), slope = 1 - UpAt(x, z), apron = CragApron(x, z), bare = MountainBare(x, z, out float fan);
            uint blk = (uint)((i >> 1) * 83492791) ^ (uint)((j >> 1) * 29765729); blk = (blk ^ (blk >> 13)) * 0x5bd1e995u; float speck = ((blk ^ (blk >> 15)) & 1023) / 1023f;
            rock = Mathf.Clamp01((bare + (n2 - .5f) * .5f + (n3 - .5f) * .4f + (speck - .5f) * .2f - .5f) * 4 + .5f);
            float lap = (MeshY(x + 3, z) + MeshY(x - 3, z) + MeshY(x, z + 3) + MeshY(x, z - 3)) / 4 - y;
            float gully = Mathf.Clamp01(lap * 1.5f - .08f + (n2 - .5f) * .2f), crest = Mathf.Clamp01(-lap * 1.5f - .1f);
            hollow = 1 - .16f * gully + .05f * crest;
            var grey = new Color(.25f, .238f, .222f);   // the crags' tint (Cliff) times their painted stone's mean, a little under
            float stone = Mathf.Clamp01((n3 - .6f) * 7), gap = Mathf.Clamp01((.36f - n3) * 7);
            Color scree = Color.Lerp(new Color(.288f, .262f, .22f), new Color(.346f, .312f, .262f), n2) * (1 + .16f * stone - .2f * gap + (speck - .5f) * .16f + fan * .06f);
            scree = Color.Lerp(scree, grey * (.85f + speck * .2f + stone * .12f), apron * .55f);
            float bedY = y + x * .045f - z * .03f + (Mathf.PerlinNoise(x * .09f + 51, z * .09f + 23) - .5f) * 1.8f + (n2 - .5f) * .3f;
            float run = Mathf.Floor(bedY / 8), by = bedY - run * 8; int bed = 0; while (bed < 5 && by >= RockBedTops[bed]) bed++;
            float foot = bed == 0 ? 0 : RockBedTops[bed - 1], thick = RockBedTops[bed] - foot, up = by - foot;   // up: metres above the bed's foot
            uint id = (uint)((int)run * 6 + bed + 4096) * 2654435761u; id ^= id >> 15; float tone = (id & 1023) / 1023f;
            float joint = Mathf.Abs(Mathf.PerlinNoise(x * .21f + tone * 37, z * .21f + bed * 7.3f) * 2 - 1), strong = .35f + .65f * Mathf.Clamp01(Mathf.PerlinNoise(x * .13f + tone * 91, z * .13f + bed * 3.1f) * 2.2f - .35f);
            float v = (.8f + tone * .4f) * (.93f + n1 * .14f) * (.95f + speck * .1f);
            v *= 1 - .42f * strong * (1 - Mathf.SmoothStep(0, 1, up / .32f));       // the shadow line at the bed's foot, faint along some stretches
            v *= 1 + .16f * Mathf.SmoothStep(0, 1, (up - thick + .28f) / .28f);     // its worn, lit lip
            v *= 1 - .26f * Mathf.Clamp01((.06f - joint) / .03f);                   // a joint, every few metres
            float warm = (Mathf.Repeat(tone * 3, 1) - .5f) * .08f;
            var bedded = new Color(grey.r * v * (1 + warm), grey.g * v, grey.b * v * (1 - warm * 1.2f));
            float face = Mathf.Clamp01(((slope - .1275f) * 8 + (n1 - .5f) * .5f + (n3 - .5f) * .5f - .5f) * 3 + .5f);
            return Color.Lerp(scree, bedded, face) * (1 - .26f * gully + .07f * crest) * (1 - .14f * apron);
        }
'''),
# ---------------------------------------------------------------- BuildGround: the detail mask ----------------------------------------------------------------
(ZB, "the mountain detail mask",
r'''            if (Zone.biome == "mountain") m.SetTexture("_DetailMask", DetailMask(4, (x, z) => .5f));   // half grain: in hard alpine light it was a harsh speckle
''',
r'''            if (Zone.biome == "mountain") m.SetTexture("_DetailMask", DetailMask(256, (x, z) => Mathf.Lerp(.5f, .9f, Mathf.Clamp01(MountainBare(x, z, out _) * 2 - .5f))));   // half grain on the turf (in hard alpine light it was a harsh speckle), nearly full on rock and scree
'''),
# ---------------------------------------------------------------- PaintGround: the mountain branch, above the alp line ----------------------------------------------------------------
(ZB, 'the mountain paint above the alp line',
r'''                        // Alpine: patches of thin dry turf between warm scree and grey rock. Rock takes the steep ground (crags,
                        // scarps, gorge walls) and the high edges, scree the rocky flats. Mid-dark values, so it never reads as snow.
                        // Rock and scree are warm and a shade darker, and counter-shaded toward the day sun (faces turned to it a
                        // little darker, faces turned away lifted), so a sunlit crag stays well under the fog's value and the relief
                        // reads as a gradient, not near-white against near-black. Close-up grain (n3) is kept soft.
                        float steep = Mathf.Clamp01((1 - UpAt(x, z) - .06f) * 6);
                        float rock = Mathf.Clamp01(MountainRock(x, z) + steep + (n3 - .5f) * .3f);
                        var nrm = NormalAt(x, z); float turn = nrm.x * toSun.x + nrm.z * toSun.z, shade = 1 - Mathf.Clamp(turn, -.6f, .6f) * (turn < 0 ? .5f : .22f);   // horizontal turn toward the midday sun (55 deg, an average)
                        // A sixth darker again than step 2's (sunlit crags still read ~140 of 255 in the tour; the aim is 110-130).
                        Color rockC = Color.Lerp(new Color(.195f, .186f, .178f), new Color(.278f, .262f, .245f), n2) * shade;
                        Color scree = Color.Lerp(new Color(.288f, .262f, .22f), new Color(.346f, .312f, .262f), (n2 + n3) * .5f) * shade;
''',
r'''                        // Alpine: thin dry turf on the gentler ground only, ending on a ragged edge; warm scree on the rocky flats,
                        // under the faces and round the crags' feet; bedded grey rock on the steep ground (crags, scarps, gorge
                        // walls) and the high edges (MountainGround). Mid-dark values, so it never reads as snow. Rock and scree are
                        // counter-shaded toward the day sun (faces turned to it a little darker, faces turned away lifted), so a
                        // sunlit crag stays well under the fog's value and the relief reads as a gradient, not near-white against
                        // near-black.
                        var nrm = NormalAt(x, z); float turn = nrm.x * toSun.x + nrm.z * toSun.z, shade = 1 - Mathf.Clamp(turn, -.6f, .6f) * (turn < 0 ? .5f : .22f);   // horizontal turn toward the midday sun (55 deg, an average)
                        Color rockC = MountainGround(x, z, i, j, n1, n2, n3, out float rock, out float hollow) * shade;
'''),
# ---------------------------------------------------------------- PaintGround: the mountain branch, below the alp line ----------------------------------------------------------------
(ZB, 'the mountain paint below the alp line',
r'''                        c = Color.Lerp(alp, Color.Lerp(scree, rockC, Mathf.Clamp01(steep * 1.4f + (n1 - .5f) * .8f)), rock) * (.91f + n3 * .12f);
''',
r'''                        c = Color.Lerp(alp * hollow, rockC, rock) * (.91f + n3 * .12f);
'''),
# ---------------------------------------------------------------- Openness: the mountain turf ----------------------------------------------------------------
(ZB, "the mountain grass's turf",
r'''                // Thin alpine turf in tufts: none on rock, scree or steep ground, and clumped where it does grow.
                float turf = 1 - Mathf.Clamp01(MountainRock(p.x, p.y) + Mathf.Clamp01((1 - UpAt(p.x, p.y) - .06f) * 6));
''',
r'''                // Thin alpine turf in tufts: none on rock, scree or steep ground, under a face or round a crag's foot (the
                // paint's own measure, MountainBare; its turf ends about .5), and clumped where it does grow.
                float turf = 1 - Mathf.Clamp01(MountainBare(p.x, p.y, out _) * 1.5f);
'''),
# ---------------------------------------------------------------- Cliff: seated base lumps, scree behind a free crag ----------------------------------------------------------------
(ZB, "Cliff: the steps' backs",
r'''            var fronts = new List<Vector2>();   // each step's (x, front plane z), for the scree
''',
r'''            var fronts = new List<Vector2>(); var backs = new List<Vector2>();   // each step's (x, front plane z), and a free crag's (x, back plane z), for the scree
            // Mountains: how far a base lump must sink (at most 3 m) so its flat base lies .4 m under the lowest ground beneath
            // its w x d footprint (the height function and the drawn ground both), not only under its foot line.
            float Sink(float x, float bottom, float z, float w, float d)
            {
                float lowest = float.MaxValue;
                for (int k = 0; k < 5; k++)
                {
                    var q = t.TransformPoint(new Vector3(x + (k == 4 ? 0 : (k % 2 * 2 - 1) * w * .35f), 0, z + (k == 4 ? 0 : (k / 2 * 2 - 1) * d * .35f)));
                    lowest = Mathf.Min(lowest, Mathf.Min(HeightAt(q.x, q.z), MeshY(q.x, q.z)) - t.position.y);
                }
                return Mathf.Clamp(bottom - (lowest - .4f), 0, 3);
            }
'''),
(ZB, "Cliff: a step's base lump",
r'''                fronts.Add(new Vector2(x, zf));
                // The base: wide and low (half the height), bulging .3 m in front of the step's front plane, sunk .3 m under the foot.
                float hA = Mathf.Max(2.5f, h * (.5f + O() * .15f)), szA = d * .85f + O() * .5f;
                Rock(x + (O() - .5f) * .8f, foot - .3f, zf + f * (szA / 2 - .3f), w + 1.8f + O() * .8f, hA, szA, scarp ? f * (1 + O() * 3) : (O() - .5f) * 8, (O() - .5f) * 4, (O() - .5f) * 30);
''',
r'''                fronts.Add(new Vector2(x, zf)); if (!scarp) backs.Add(new Vector2(x, zf + f * d));
                // The base: wide and low (half the height), bulging .3 m in front of the step's front plane, sunk .3 m under the foot;
                // in the mountains it grows down further wherever the ground falls away under its footprint (Sink), its top where it was.
                float hA = Mathf.Max(2.5f, h * (.5f + O() * .15f)), szA = d * .85f + O() * .5f;
                float xA = x + (O() - .5f) * .8f, zA = zf + f * (szA / 2 - .3f), wA = w + 1.8f + O() * .8f, sink = seat ? Sink(xA, foot - .3f, zA, wA, szA) : 0;
                Rock(xA, foot - .3f - sink, zA, wA, hA + sink * 1.4f, szA, scarp ? f * (1 + O() * 3) : (O() - .5f) * 8, (O() - .5f) * 4, (O() - .5f) * 30);
'''),
(ZB, "Cliff: the scree",
r'''            Scree(t, fronts, rise > 0 ? side : 1, rise > 0 ? 1 : .5f, stone, O);
''',
r'''            Scree(t, fronts, rise > 0 ? side : 1, rise > 0 ? 1 : seat ? .8f : .5f, stone, O);
            if (seat && rise <= 0) Scree(t, backs, -1, .5f, stone, O);   // mountains: a free crag stands in its fall on both sides
'''),
# ---------------------------------------------------------------- the boulders ----------------------------------------------------------------
(ZB, "the rock prop's skirt",
r'''Solid(t, new Vector3(0, .5f * s, 0), new Vector3(1.6f * s, 1f * s, 1.4f * s)); break;
''',
r'''Solid(t, new Vector3(0, .5f * s, 0), new Vector3(1.6f * s, 1f * s, 1.4f * s)); if (Zone.biome == "mountain" && string.IsNullOrEmpty(p.interact)) RockSkirt(t, s * .8f, stone); break;
'''),
(ZB, "EdgeRock's lumps",
r'''            Lump(b0, t, new Vector3(0, .45f * s, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s), mat, y0);
            Lump(b1, t, new Vector3(.8f * s, .25f * s, .45f * s), new Vector3(1.2f * s, .9f * s, 1.1f * s), mat, y1);
            Solid(t, new Vector3(0, .6f * s, 0), new Vector3(1.8f * s, 1.2f * s, 1.5f * s));
''',
r'''            float deep = Zone.biome == "mountain" ? .14f * s : 0;   // mountains: the lumps a seventh deeper in the ground (the collider is where it was)
            Lump(b0, t, new Vector3(0, .45f * s - deep, 0), new Vector3(2.1f * s, 1.5f * s, 1.8f * s), mat, y0);
            Lump(b1, t, new Vector3(.8f * s, .25f * s - deep, .45f * s), new Vector3(1.2f * s, .9f * s, 1.1f * s), mat, y1);
            Solid(t, new Vector3(0, .6f * s, 0), new Vector3(1.8f * s, 1.2f * s, 1.5f * s));
            if (Zone.biome == "mountain") RockSkirt(t, s, mat);
'''),
(ZB, 'RockSkirt (after SinkBySlope)',
r'''            t.position += Vector3.down * (Mathf.Clamp(t.position.y - low, 0, .6f * s) + .15f * s);
        }
''',
r'''            t.position += Vector3.down * (Mathf.Clamp(t.position.y - low, 0, .6f * s) + .15f * s);
        }
        /// <summary>Mountains: 4-7 small stones round the foot of a boulder of size s (.22-.52 of it, .95-1.65 of it out, each sunk a
        /// fifth to two fifths of its height into the lower of the height function and the drawn ground), so the boulder lies in
        /// its own fall of rock, not alone on the turf. No colliders, so the navmesh is as it was; none where loose rock may not
        /// lie (RockMayLie) or in a cave. Draws only from its own stream, keyed on where the boulder stands.</summary>
        void RockSkirt(Transform t, float s, Material mat)
        {
            var own = TreeRandom(t.position); float O() { return (float)own.NextDouble(); }
            int n = 4 + (int)(O() * 4); float up = Mathf.Max(.01f, t.lossyScale.y);
            for (int k = 0; k < n; k++)
            {
                float a = (k + O()) / n * Mathf.PI * 2, r = s * (.95f + O() * .7f), size = s * (.22f + O() * .3f), h = size * .8f, sunk = .2f + O() * .2f, yaw = O() * 360;
                var at = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r * .9f); var w = t.TransformPoint(at); var q = new Vector2(w.x, w.z);
                if (!RockMayLie(q) || Hollow.CoverAt(q, 1) > 0) continue;
                at.y = (Mathf.Min(HeightAt(w.x, w.z), MeshY(w.x, w.z)) - t.position.y) / up + .18f * h - sunk * h;
                Lump(BoulderAt(k + (int)yaw), t, at, new Vector3(size * 1.25f, h, size * 1.1f), mat, yaw);
            }
        }
'''),
]

def main():
    files, out, problems = {}, {}, []
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path)
        if full not in files:
            if not os.path.isfile(full): problems.append('%s: file not found' % path); continue
            files[full] = io.open(full, 'r', encoding='utf-8', newline='').read()
            out[full] = files[full]
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path)
        if full not in files: continue
        if '\r' in files[full]: old, new = old.replace('\n', '\r\n'), new.replace('\n', '\r\n')
        n = files[full].count(old)
        if n != 1: problems.append('%s: %s: the old block occurs %d times (expected 1)' % (path, what, n)); continue
        if out[full].count(old) != 1: problems.append('%s: %s: overlaps another edit' % (path, what)); continue
        out[full] = out[full].replace(old, new)
    # The green batch's anchor must be left exactly as it found it (before or after that batch): one alp line.
    for full, text in out.items():
        if full.endswith('ZoneBuilder.cs') and text.count('                        Color alp = Color.Lerp(') != 1: problems.append('ZoneBuilder.cs: the alp line is not there exactly once')
        if 'float MountainBare(' in files[full] or 'void RockSkirt(' in files[full]: problems.append('%s: already patched' % os.path.basename(full))
    if problems:
        print('NOT APPLIED. Nothing was written.'); [print('  ' + p) for p in problems]; sys.exit(1)
    for full, text in out.items():
        io.open(full, 'w', encoding='utf-8', newline='').write(text)
        print('patched %s' % full)
    print('OK: %d edits in %d file(s).' % (len(EDITS), len(out)))

if __name__ == '__main__':
    main()
