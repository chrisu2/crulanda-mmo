"""The painted style pass, worklist item 12 (ruins with broken silhouettes): patch. Usage: python patch_ruins.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Scripts\\World\\ZoneBuilder.cs only.
     shared         RuinHash (a fraction made from a value the prop already drew), Overgrown (where green still grows on
                    old stone: not the ash, not the gloom), Ivy (leaf cards up a wall face).
     Ruin           a wall of stubs was a row of square-topped boxes. Now each stub is a body of masonry under courses
                    that step down toward its lower neighbour (heights eased toward the neighbours, so the row reads as
                    one wall that has come down, not battlements), all one mesh; where most has fallen a toppled block
                    and a heap of broken rock lie at the foot; ivy on some stubs where the biome allows. A ruin of one
                    stub (the Reach-stones) is a single leaning stone of the zone's rock, set at the middle of its
                    collider (it stood half a metre to one side of it).
     RuinedHouse    the wall stubs were primitive cubes of random height. Now each wall is a run of plaster with a ragged
                    sloping skyline cut from the same draws (metre UVs), a scorched band along the broken top, a stone
                    footing course, charred corner posts, a standing door frame with its lintel where the doorway was,
                    a window opening with a charred lintel and sill in the back wall when it stands high enough, one
                    stretch of wall leaning out between two breaks, the +x end standing as a broken gable with its
                    charred rafters, rubble of painted rock at the foot of the low stretches and ivy where the biome
                    allows. Walls end at the corners (the last stub used to poke past). The ash plaster is a step paler.
                    The floor slab, the fallen roof (its GableRoof line is not touched), the post and the joists are as
                    they were.

   Draws from the zone's stream: the same number in the same order (Ruin: three a stub; RuinedHouse: one a wall stub and
   none for the doorway's, then the joists' six each). Everything new is laid out by RuinHash of those drawn values or
   by fixed numbers. No Solid, collider or NavBlocker changes; nothing new has a collider.

   Everything is checked before anything is written: every old block must occur exactly once in its file. So a second
   run, or a run on a tree that has moved on, fails loudly and changes nothing."""
import io, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
ZB = r'Scripts\World\ZoneBuilder.cs'

# (file, what, old, new)
EDITS = [
(ZB, 'Ruin (and the shared ruin helpers before it)',
r'''        void Ruin(Transform t, float length)
        {
            var stone = Dressed(new Color(.42f, .41f, .38f));
            for (float x = -length / 2; x < length / 2; x += 1.1f)
            {
                float h = .6f + R01 * 2.4f, g = LocalGround(t, x, 0) - .15f;   // each block stands on (and a little into) the ground under it
                BoxPart(t, new Vector3(x, g + (h + .15f) / 2, 0), new Vector3(1.05f, h + .15f, .8f), stone, Quaternion.Euler(0, R01 * 6 - 3, R01 * 6 - 3), 1.5f);
            }
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(length, 2.4f, 1));
        }
''',
r'''        /// <summary>A fraction (0 to 1) made from a value a prop has already drawn and a slot number: a ruin's extra shapes are laid out by these, so nothing more is drawn from the zone's stream.</summary>
        static float RuinHash(float drawn, int slot) { return Mathf.Repeat(drawn * 91.7f + slot * .618f, 1); }
        /// <summary>Whether green still grows on old stone here: not on the ash and not in the gloom, and only with leaf-card art.</summary>
        bool Overgrown { get { return Zone.biome != "ash" && !Gloom && art.leafCards != null && art.leafCards.Length > 0; } }
        /// <summary>
        /// Ivy up a wall face: leaf cards lying a finger off the wall from <paramref name="foot"/>, their tips no higher than
        /// <paramref name="tall"/> above it, wide at the root and thinning as it climbs. <paramref name="outward"/> is the way the face looks and
        /// <paramref name="along"/> runs along the wall. Laid out by <paramref name="seed"/>, a value the wall has drawn.
        /// </summary>
        void Ivy(ZoneMeshes.Cards cards, Vector3 foot, Vector3 outward, Vector3 along, float tall, float seed)
        {
            int n = 3 + (int)(tall * 2.5f);
            for (int k = 0; k < n; k++)
            {
                float a = RuinHash(seed, 40 + k), b = RuinHash(seed * 1.7f, 60 + k), leaf = .55f + b * .35f, up = Mathf.Max(0, tall - leaf) * k / n;   // the tips stop at tall
                var at = foot + along * ((a - .5f) * (1 - .6f * k / n)) + Vector3.up * up + outward * (.03f + .02f * (k % 3));
                var climb = (Vector3.up + along * ((b - .5f) * 1.4f)).normalized;
                cards.Add(at, climb, Vector3.Cross(climb, outward), leaf, leaf, at - outward, .15f, new Color(.62f, .74f, .56f) * (.85f + a * .25f), 0, .1f);
            }
        }
        /// <summary>
        /// A length of fallen wall (it runs along x): a stub every 1.1 m, each a body of masonry under courses that step down
        /// toward its lower neighbour, the lot one mesh. Where most has come down a toppled block and a heap of broken rock lie
        /// at the foot, and ivy climbs some stubs where the biome allows. A ruin of one stub is a single leaning stone of the
        /// zone's rock. Three draws a stub, as the row of blocks took.
        /// </summary>
        void Ruin(Transform t, float length)
        {
            var stone = Dressed(new Color(.42f, .41f, .38f));
            var xs = new List<float>(); var hs = new List<float>(); var yaws = new List<float>(); var rolls = new List<float>();
            for (float x = -length / 2; x < length / 2; x += 1.1f) { xs.Add(x); hs.Add(.6f + R01 * 2.4f); yaws.Add(R01 * 6 - 3); rolls.Add(R01 * 6 - 3); }
            int n = xs.Count;
            if (n == 1)
            {
                // One stone standing alone: a crag lump drawn up tall, sunk a little and leaning the way the block was turned.
                float tall = hs[0] + .55f, sy = tall / .68f, wide = .8f + RuinHash(hs[0], 1) * .3f;
                MeshPart(CragRock((int)(RuinHash(hs[0], 2) * 6)), t, new Vector3(0, LocalGround(t, 0, 0) - .35f + .2f * sy, 0), SecretStone(1.05f), Quaternion.Euler(yaws[0] * 2.5f, RuinHash(hs[0], 3) * 360, rolls[0] * 2.5f)).transform.localScale = new Vector3(wide, sy, wide * .62f);
            }
            else
            {
                var blocks = new List<CombineInstance>(); var rubble = SecretStone(.92f); var ivy = new ZoneMeshes.Cards(); float green = !Overgrown ? 0 : Zone.biome == "mountain" ? .2f : .4f;
                for (int i = 0; i < n; i++)
                {
                    float x = xs[i], h = hs[i], g = LocalGround(t, x, 0) - .15f;   // each stub stands on (and a little into) the ground under it
                    float before = i > 0 ? hs[i - 1] : h * .5f, after = i + 1 < n ? hs[i + 1] : h * .5f, top = h * .5f + (before + after) * .25f + .15f;
                    int courses = Mathf.Clamp(Mathf.RoundToInt(top * 1.5f), 1, 4), toward = after > before ? 1 : -1; float body = top - courses * .3f;
                    var turn = Quaternion.Euler(0, yaws[i], rolls[i]);
                    blocks.Add(Ashlar(new Vector3(x, g + body / 2, 0), new Vector3(1.12f, body, .8f), 1.5f, turn));
                    for (int j = 0; j < courses; j++)
                    {
                        float len = 1.12f * (1 - (j + .5f + RuinHash(h, j) * .5f) / (courses + .6f));
                        blocks.Add(Ashlar(new Vector3(x + toward * (1.12f - len) / 2, g + body + j * .3f + .13f, (RuinHash(h, j + 5) - .5f) * .08f), new Vector3(len, .34f, .78f - j * .04f), 1.5f, turn));
                    }
                    int side = RuinHash(h, 9) < .5f ? -1 : 1;
                    if (h < 1.9f)
                    {
                        float bx = x + (RuinHash(h, 10) - .5f) * .6f, bz = side * (.62f + RuinHash(h, 11) * .16f);
                        blocks.Add(Ashlar(new Vector3(bx, LocalGround(t, bx, bz) + .1f, bz), new Vector3(.55f, .3f, .38f), 1.5f, Quaternion.Euler(RuinHash(h, 12) * 24 - 12, RuinHash(h, 13) * 360, RuinHash(h, 14) * 30 - 15)));
                        for (int k = 0; k < 2; k++)
                        {
                            float s = .32f + RuinHash(h, 15 + k) * .26f, rx = x + (RuinHash(h, 17 + k) - .5f) * 1f, rz = (k == 0 ? -side : side) * (.5f + RuinHash(h, 19 + k) * .2f);
                            Lump(BoulderAt((int)(RuinHash(h, 21 + k) * 6)), t, new Vector3(rx, LocalGround(t, rx, rz) + .18f * s * .7f - .05f, rz), new Vector3(s * 1.25f, s * .7f, s), rubble, RuinHash(h, 23 + k) * 360);
                        }
                    }
                    else if (RuinHash(h, 8) < green) Ivy(ivy, new Vector3(x, g + .1f, side * .43f), new Vector3(0, 0, side), Vector3.right, body + .25f, h);
                }
                Stonework("Ruin stone", t, stone, blocks.ToArray());
                if (ivy.Count > 0) MeshPart(ivy.Build("Ivy"), t, Vector3.zero, LeafMaterial(art.leafCards[0], new Color(.5f, .62f, .44f)));
            }
            Solid(t, new Vector3(0, 1.2f, 0), new Vector3(length, 2.4f, 1));
        }
'''),

(ZB, 'RuinedHouse: the walls',
r'''            // A fallen building: broken wall stubs of uneven height, a charred remnant of roof slumped inside, fallen beams.
            float w = size.x, d = size.y; var plaster = Tint(art.plaster, Zone.biome == "ash" ? new Color(.46f, .455f, .45f) : new Color(.52f, .48f, .42f)); var charred = Tint(art.timber, new Color(.13f, .11f, .1f));
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the floor slab reaches down to the lowest ground under it
            BoxPart(t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), Dressed(new Color(.52f, .51f, .48f)), null, 1.5f);
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f)
                {
                    float h = sz < 0 && Mathf.Abs(x) < 1 ? .2f : .8f + R01 * 2.4f;
                    Part(PrimitiveType.Cube, t, new Vector3(x, .6f + h / 2, sz * d / 2), new Vector3(1.2f, h, .35f), plaster);
                }
            foreach (int sx in new[] { -1, 1 })
                for (float z = -d / 2 + .6f; z < d / 2; z += 1.2f) { float h = .8f + R01 * 2.8f; Part(PrimitiveType.Cube, t, new Vector3(sx * w / 2, .6f + h / 2, z), new Vector3(.35f, h, 1.2f), plaster); }
''',
r'''            // A fallen building: broken walls with a ragged, scorched skyline on a stone footing, charred corner posts, the door
            // frame still standing, one end left as a broken gable, rubble at the foot; a charred remnant of roof slumped
            // inside and fallen beams.
            float w = size.x, d = size.y; var plaster = Tint(art.plaster, Zone.biome == "ash" ? new Color(.56f, .555f, .55f) : new Color(.52f, .48f, .42f)); var charred = Tint(art.timber, new Color(.13f, .11f, .1f));
            float drop = FootDrop(t, w + .3f, d + .3f);   // on a slope the floor slab reaches down to the lowest ground under it
            BoxPart(t, new Vector3(0, .3f - drop / 2, 0), new Vector3(w + .3f, .6f + drop, d + .3f), Dressed(new Color(.52f, .51f, .48f)), null, 1.5f);
            // The heights the wall stubs drew, run by run (front, back, the -x end, the +x end): the same draws in the same
            // order as the stubs took, and none for the doorway's (-1 marks them).
            var runs = new[] { new List<float>(), new List<float>(), new List<float>(), new List<float>() };
            foreach (int sz in new[] { -1, 1 })
                for (float x = -w / 2 + .6f; x < w / 2; x += 1.2f) runs[sz < 0 ? 0 : 1].Add(sz < 0 && Mathf.Abs(x) < 1 ? -1 : .8f + R01 * 2.4f);
            foreach (int sx in new[] { -1, 1 })
                for (float z = -d / 2 + .6f; z < d / 2; z += 1.2f) runs[sx < 0 ? 2 : 3].Add(.8f + R01 * 2.8f);
            var walls = new List<CombineInstance>(); var scorch = new List<CombineInstance>(); var footing = new List<CombineInstance>(); var ends = new float[4, 2];
            var rubble = SecretStone(.95f); var ivy = new ZoneMeshes.Cards(); float green = !Overgrown ? 0 : Zone.biome == "mountain" ? .2f : .45f;
            Mesh Slab(Vector2[] outline) { var m = Cutout(outline, .35f); var uv = m.uv; for (int k = 0; k < uv.Length; k++) uv[k] *= .5f; m.uv = uv; return m; }   // the plaster at the houses' scale (2 m a tile)
            // One wall: it runs along its own x from at (the middle of its foot, on the floor slab), turned about y; outward is
            // the side of its own z that looks out of the house. Each 1.2 m stub gives the skyline three points (its two
            // ends and a peak or dip between), so the top slopes and steps raggedly instead of standing in square teeth.
            void Run(int run, Vector3 at, float turn, int outward, float span, bool gable)
            {
                var hs = runs[run]; int n = hs.Count, keep = RuinHash(hs[0], 6) < .5f ? -1 : 1; float reach = span / 2, win = -99, leanL = 0, leanR = 0, lean = 0;
                var face = Quaternion.Euler(0, turn, 0); var top = new List<Vector2>();
                Vector3 P(float x, float y, float z = 0) { return at + face * new Vector3(x, y, z); }
                float E(int i) { return i >= n ? reach : Mathf.Min(-reach + i * 1.2f, reach); }                    // where stub i starts; the last one ends at the corner
                float G(float x) { return 2.3f + (1 - Mathf.Abs(x) / reach) * span * .4f; }                         // the gable's line
                bool Split(float a, float b) { return RuinHash(a + b, 4) < .4f; }                                   // a break between two stubs: each ends at its own height
                // The back wall keeps a window if a stub away from the corners stands high enough to hold its head.
                if (run == 1) { int best = -1; for (int i = 1; i + 1 < n; i++) if (hs[i] >= 2.6f && (best < 0 || hs[i] > hs[best])) best = i; if (best >= 0) win = (E(best) + E(best + 1)) / 2; }
                void Quad(float xa, float xb, float y0, float ya, float yb, Quaternion rot, bool burnt)
                {
                    walls.Add(Piece(Slab(new[] { new Vector2(xa, y0), new Vector2(xa, ya), new Vector2(xb, yb), new Vector2(xb, y0) }), at, rot));
                    if (burnt) scorch.Add(Piece(Cutout(new[] { new Vector2(xa, Mathf.Max(y0, ya - .26f)), new Vector2(xa, ya + .012f), new Vector2(xb, yb + .012f), new Vector2(xb, Mathf.Max(y0, yb - .26f)) }, .39f), at, rot));
                }
                // A stretch of wall between corners and the doorway: its footing course, then the plaster under each span of the skyline.
                void Flush(float to)
                {
                    if (top.Count == 0) return;
                    float from = top[0].x, a = from + (from < -reach + .01f ? (run < 2 ? -.235f : .235f) : 0), b = to + (to > reach - .01f ? (run < 2 ? .235f : -.235f) : 0);   // front and back take the corners
                    footing.Add(Ashlar(P((a + b) / 2, .2f), new Vector3(b - a, .4f, .47f), 1.5f, face));
                    if (win > -90)
                        foreach (float x in new[] { win - .38f, win + .38f })
                            for (int k = 0; k + 1 < top.Count; k++)
                                if (top[k].x < x - .02f && top[k + 1].x > x + .02f) { top.Insert(k + 1, new Vector2(x, Mathf.Lerp(top[k].y, top[k + 1].y, Mathf.InverseLerp(top[k].x, top[k + 1].x, x)))); break; }
                    for (int k = 0; k + 1 < top.Count; k++)
                    {
                        Vector2 p = top[k], q2 = top[k + 1]; if (q2.x - p.x < .02f) continue;
                        float m = (p.x + q2.x) / 2; var rot = m > leanL && m < leanR ? face * Quaternion.Euler(lean, 0, 0) : face;
                        if (win > -90 && Mathf.Abs(m - win) < .38f) { Quad(p.x, q2.x, 0, .9f, .9f, rot, false); if (p.y > 2.1f && q2.y > 2.1f) Quad(p.x, q2.x, 1.9f, p.y, q2.y, rot, true); }   // under the sill, over the head
                        else Quad(p.x, q2.x, 0, p.y, q2.y, rot, true);
                    }
                    top.Clear();
                }
                for (int i = 0; i < n; i++)
                {
                    float h = hs[i], l = E(i), r = E(i + 1);
                    if (h < 0) { Flush(l); continue; }   // the doorway
                    bool joinL = i > 0 && hs[i - 1] >= 0, joinR = i + 1 < n && hs[i + 1] >= 0, cutL = !joinL || Split(hs[i - 1], h), cutR = !joinR || Split(h, hs[i + 1]);
                    float mid = (l + r) / 2 + (RuinHash(h, 1) - .5f) * (r - l) * .5f, hm = h;
                    float hl = cutL ? h * (.6f + RuinHash(h, 3) * .3f) : Mathf.Lerp(Mathf.Min(hs[i - 1], h), Mathf.Max(hs[i - 1], h), .35f);
                    float hr = cutR ? h * (.62f + RuinHash(h, 5) * .3f) : Mathf.Lerp(Mathf.Min(h, hs[i + 1]), Mathf.Max(h, hs[i + 1]), .35f);
                    if (win >= l && win <= r) { hl = Mathf.Max(hl, 2.2f); hr = Mathf.Max(hr, 2.2f); mid = (l + r) / 2; }   // the window's stub keeps its head
                    if (gable)
                    {
                        // The gable stands whole on one side of the ridge and a little past it; the rest is down to what the stubs drew.
                        if (l < 0 && r > 0) mid = 0;
                        hl = l * keep <= .9f ? G(l) : Mathf.Min(hl, G(l)); hm = mid * keep <= .9f ? G(mid) : Mathf.Min(hm, G(mid)); hr = r * keep <= .9f ? G(r) : Mathf.Min(hr, G(r));
                    }
                    else if (lean == 0 && cutL && cutR && i > 0 && i < n - 1 && h > 1.4f && (win < l || win > r)) { leanL = l; leanR = r; lean = outward * (4 + RuinHash(h, 7) * 6); }   // broken free at both ends: it leans out
                    if (i == 0) ends[run, 0] = hl;
                    if (i == n - 1) ends[run, 1] = hr;
                    top.Add(new Vector2(l, hl)); top.Add(new Vector2(mid, hm)); top.Add(new Vector2(r, hr));
                    float xc = (l + r) / 2;
                    if (!gable && h < 1.7f)
                        for (int k = 0; k < 2; k++)
                        {
                            // Where the wall is low, what fell lies in a heap against its foot outside.
                            float s = .34f + RuinHash(h, 10 + k) * .2f; var p = P(xc + (RuinHash(h, 12 + k) - .5f) * .9f, 0, outward * (.4f + k * .08f));
                            Lump(BoulderAt((int)(RuinHash(h, 14 + k) * 6)), t, new Vector3(p.x, LocalGround(t, p.x, p.z) + .18f * s * .7f - .05f, p.z), new Vector3(s * 1.25f, s * .7f, s), rubble, RuinHash(h, 16 + k) * 360);
                        }
                    else if (RuinHash(h, 8) < green && (win < l || win > r)) Ivy(ivy, P(xc, -.35f, outward * .26f), face * new Vector3(0, 0, outward), face * Vector3.right, (gable ? 2.3f : h) * .85f + .35f, h);
                }
                Flush(reach);
                if (win > -90)
                {
                    Part(PrimitiveType.Cube, t, P(win, 1.97f), new Vector3(1, .14f, .43f), charred, face);
                    Part(PrimitiveType.Cube, t, P(win, .93f), new Vector3(.9f, .06f, .45f), charred, face);
                }
                if (gable)
                {
                    // The rafters of the gable's standing side, charred, on its inner face; a stub of the other past the ridge.
                    var ridge = P(0, G(0) - .12f, -outward * .26f);
                    Bar(t, P(-keep * (reach - .1f), 2.2f, -outward * .26f), ridge, .18f, .14f, charred);
                    Bar(t, ridge, P(keep * 1.1f, G(1.1f) - .12f, -outward * .26f), .18f, .14f, charred);
                }
            }
            Run(0, new Vector3(0, .6f, -d / 2), 0, -1, w, false); Run(1, new Vector3(0, .6f, d / 2), 0, 1, w, false);
            Run(2, new Vector3(-w / 2, .6f, 0), -90, 1, d, false); Run(3, new Vector3(w / 2, .6f, 0), -90, -1, d, true);
            Stonework("Ruined walls", t, plaster, walls.ToArray());
            Stonework("Scorched wall tops", t, Tint(art.plaster, Zone.biome == "ash" ? new Color(.2f, .2f, .2f) : new Color(.19f, .16f, .13f)), scorch.ToArray());
            Stonework("Ruined footing", t, Dressed(new Color(.5f, .49f, .46f)), footing.ToArray());
            if (ivy.Count > 0) MeshPart(ivy.Build("Ivy"), t, Vector3.zero, LeafMaterial(art.leafCards[0], new Color(.5f, .62f, .44f)));
            // A charred post at each corner, a little above the taller of the two walls that meet there.
            for (int k = 0; k < 4; k++)
            {
                int cx = k < 2 ? 0 : 1, cz = k % 2; float tall = Mathf.Max(ends[cz, cx], ends[2 + cx, cz]) + .22f;
                Part(PrimitiveType.Cube, t, new Vector3((cx * 2 - 1) * w / 2, .6f + tall / 2, (cz * 2 - 1) * d / 2), new Vector3(.44f, tall, .44f), charred);
            }
            // The door frame still stands where the doorway was: two charred posts and the lintel, a little askew.
            int doorFrom = runs[0].IndexOf(-1), doorTo = runs[0].LastIndexOf(-1);
            if (doorFrom >= 0)
            {
                float gl = -w / 2 + doorFrom * 1.2f, gr = Mathf.Min(-w / 2 + (doorTo + 1) * 1.2f, w / 2);
                foreach (float px in new[] { gl + .11f, gr - .11f }) Part(PrimitiveType.Cube, t, new Vector3(px, 1.65f, -d / 2), new Vector3(.22f, 2.1f, .42f), charred);
                Part(PrimitiveType.Cube, t, new Vector3((gl + gr) / 2, 2.79f, -d / 2), new Vector3(gr - gl + .36f, .22f, .46f), charred, Quaternion.Euler(0, 0, 2));
            }
'''),
]

def main():
    files, problems = {}, []
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path)
        if full not in files:
            if not os.path.isfile(full): sys.exit('patch_ruins: NOTHING WRITTEN.\n  missing file: ' + full)
            files[full] = io.open(full, 'r', encoding='utf-8', newline='').read()
    out = dict(files)
    for path, what, old, new in EDITS:
        full = os.path.join(ROOT, path); text = files[full]
        crlf = '\r\n' in text
        o, n = (old.replace('\n', '\r\n'), new.replace('\n', '\r\n')) if crlf else (old, new)
        c = text.count(o)
        if c != 1: problems.append('%s: %s: old block found %d times (want 1)' % (path, what, c)); continue
        if out[full].count(o) != 1: problems.append('%s: %s: overlaps another edit' % (path, what)); continue
        out[full] = out[full].replace(o, n)
    # The fallen roof's line belongs to another batch's checks: it must be there, once, before and after.
    roof = 'MeshPart(ZoneMeshes.GableRoof(rw, rd, rh), t, roofAt, Tint(art.thatch, new Color(.24f, .2f, .15f)), fallen);'
    full = os.path.join(ROOT, ZB)
    for label, text in (('before', files[full]), ('after', out[full])):
        if text.count(roof) != 1: problems.append('%s: the fallen roof line is not there exactly once (%s)' % (ZB, label))
    for name in ('static float RuinHash(', 'bool Overgrown ', 'void Ivy('):
        if name in files[full]: problems.append('%s: %s is already there' % (ZB, name.strip()))
    if problems:
        sys.exit('patch_ruins: NOTHING WRITTEN.\n  ' + '\n  '.join(problems))
    for full, text in out.items():
        io.open(full, 'w', encoding='utf-8', newline='').write(text)
        print('patched', full)
    print('patch_ruins: %d edits in %d file(s).' % (len(EDITS), len(out)))

if __name__ == '__main__':
    main()
