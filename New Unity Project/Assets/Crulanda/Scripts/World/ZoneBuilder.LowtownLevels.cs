using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Lowtowns built downhill (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 1; Docs/ZONE_LOWTOWNS.md "Built downhill"). The ground is
    /// the zone's terraces (<see cref="ZoneTerrace"/>, styles "shelf", "stair" and "ramp"): the Rust Market high against the crumbling stone
    /// walls of the Forge District (CANON, book1 ch.3:7), the Lower Market and the Shard-Burners' Yard a shelf under it, the river, the Gate
    /// Ward, the docks and the Rows at the bottom; on the west bank the High Rows over the Drain Courts, sunk among high brick walls (book1
    /// ch.1:237); stairs and causeways between the levels ("a narrow, immensely twisting causeway that led ... down into the deep, dark
    /// slums", book1 ch.3:257). The levels and their names are GAME-ONLY.
    /// - Retaining walls (<see cref="LowtownWalls"/>): wherever a terrace's edge stands a metre or more over the ground beside it, soot brick
    ///   fills the slope from under the low ground to the high (a parapet a metre over a drop of more than 1.2 m), a coping of dark stone on
    ///   it, the neon moss along the top in places (CANON, book1 ch.7:17: "vibrant, unnatural neon moss") and, where the low side is sunk under
    ///   the streets (the Drain Courts), green mould glowing at the foot (CANON, book1 ch.6:129, ch.9:55). Each run of wall is solid and cut
    ///   out of the navmesh; no wall stands across a ramp or a stair (their own sides are walled instead).
    /// - Step-streets (<see cref="LowtownStair"/>): treads of wet setts over a "stair" ramp, a .4 m going; the ramp under them is what is
    ///   walked (a foot sinks a step's rise at most; the navmesh never sees them).
    /// - <see cref="LowSteep"/>: whether a point is on a slope, so the streets' clutter, puddles, washing lines and lamp posts keep off the
    ///   walls and stairs.
    /// Each draws from a stream of its own; the zone's stream is never touched. No machines.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        // ---------- the books' colours (Lowtowns pick 2) ----------
        /// <summary>"vibrant, unnatural neon moss" on the slate (CANON, book1 ch.7:17): a sick bright green that glows a little by day and
        /// shows up at night.</summary>
        static readonly Color NeonMossColour = new Color(.3f, .74f, .13f), NeonMossGlow = new Color(.1f, .36f, .05f);
        /// <summary>The moss's yellow-green, mixed in so a patch is never one flat colour.</summary>
        static readonly Color NeonMossPaleColour = new Color(.52f, .76f, .14f), NeonMossPaleGlow = new Color(.2f, .3f, .04f);
        /// <summary>The green bioluminescent mould of the under-ways (CANON, book1 ch.6:129: "the faint, greenish glow of bioluminescent mold
        /// clinging to the archways"; ch.9:55).</summary>
        static readonly Color MouldColour = new Color(.2f, .6f, .35f), MouldGlow = new Color(.2f, 1f, .5f);
        readonly Dictionary<string, Material> lowGlows = new Dictionary<string, Material>();
        /// <summary>A material of <paramref name="baseMat"/>'s look tinted <paramref name="c"/> that glows <paramref name="glow"/> (the
        /// neon moss, the drain mould): one a key, so a thousand patches are one material.</summary>
        Material LowGlow(Material baseMat, Color c, Color glow, string key)
        {
            if (lowGlows.TryGetValue(key, out var m)) return m;
            m = new Material(baseMat) { color = c, name = "Lowtowns " + key };
            if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            lowGlows[key] = m; return m;
        }
        Material NeonMoss { get { return LowGlow(art.stone, NeonMossColour, NeonMossGlow, "neon moss"); } }
        Material NeonMossPale { get { return LowGlow(art.stone, NeonMossPaleColour, NeonMossPaleGlow, "neon moss pale"); } }
        Material DrainMould { get { return LowGlow(art.stone, MouldColour, MouldGlow * 1.3f, "drain mould"); } }
        Material WallCoping { get { return Dressed(new Color(.29f, .28f, .27f)); } }

        /// <summary>Whether the ground within <paramref name="r"/> of a point climbs or falls more than a hand (a terrace's edge, a ramp's
        /// side): the streets' clutter, puddles and lamp posts keep off it. Always false in a zone without terraces.</summary>
        public bool LowSteep(Vector2 p, float r)
        {
            if (Zone.terraces == null || Zone.terraces.Length == 0) return false;
            float lo = float.MaxValue, hi = float.MinValue;
            for (int k = 0; k < 9; k++)
            {
                var q = p + (k == 0 ? Vector2.zero : new Vector2(Mathf.Cos(k * .785f), Mathf.Sin(k * .785f)) * r);
                float h = HeightAt(q.x, q.y, false); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
            }
            return hi - lo > .25f;
        }

        /// <summary>Whether a point lies within <paramref name="pad"/> of a ramp or a stair between the levels.</summary>
        bool OnRampOrStair(Vector2 p, float pad)
        {
            if (Zone.terraces == null) return false;
            foreach (var t in Zone.terraces)
            {
                if (t == null || !t.ramp) continue;
                TerraceFrame(t, p.x, p.y, out float lx, out float lz);
                if (Mathf.Abs(lx) <= t.size.x / 2 + pad && Mathf.Abs(lz) <= t.size.y / 2 + pad) return true;
            }
            return false;
        }

        /// <summary>The Lowtowns' levels made solid and seen: the retaining walls along the terraces' edges and the step-streets' treads.</summary>
        void BuildLowtownLevels()
        {
            if (Zone.terraces == null || Zone.terraces.Length == 0) return;
            var root = new GameObject("Lowtown levels").transform; root.SetParent(statics, false);
            int runs = LowtownWalls(root), stairs = 0;
            foreach (var t in Zone.terraces) if (t != null && t.ramp && t.style == "stair") { LowtownStair(root, t); stairs++; }
            Debug.Log("Lowtowns levels: " + Zone.terraces.Length + " terraces, " + runs + " runs of retaining wall, " + stairs + " step-streets.");
        }

        /// <summary>Whether a point lies on a ramp terrace other than <paramref name="self"/>, or within <paramref name="pad"/> of its sides
        /// (its own side walls stand there: a shelf's wall stops at them).</summary>
        static bool OnRamp(List<ZoneTerrace> ramps, ZoneTerrace self, Vector2 p, float pad)
        {
            foreach (var t in ramps)
            {
                if (t == self) continue;
                TerraceFrame(t, p.x, p.y, out float lx, out float lz);
                if (Mathf.Abs(lx) <= t.size.x / 2 + pad && Mathf.Abs(lz) <= t.size.y / 2 + .2f) return true;
            }
            return false;
        }

        /// <summary>
        /// The retaining walls: along each terrace's four edges (the latest terrace first: its edges are the ones the ground keeps), in 1 m
        /// pieces, a wall wherever the ground falls or climbs more than .8 m across the edge's blend band and the slope is really in that band
        /// (a neighbour's edge in the same place is walled once). Pieces of one height join into runs: one solid box a run, the brick of all
        /// of them joined into a mesh a material. The pieces' bodies fill the band from .6 m under the low ground to the high ground, or a
        /// metre over it (the parapet) where the drop is more than 1.2 m. Returns how many runs were built.
        /// </summary>
        int LowtownWalls(Transform root)
        {
            var r = new System.Random(Zone.seed + 9191); float R() { return (float)r.NextDouble(); }
            var brick = new List<CombineInstance>(); var cope = new List<CombineInstance>(); var moss = new List<CombineInstance>(); var mould = new List<CombineInstance>();
            var ramps = new List<ZoneTerrace>(); foreach (var t in Zone.terraces) if (t != null && t.ramp) ramps.Add(t);
            var done = new List<Vector2>(); float inside = Half - 1.5f; int runs = 0;
            Vector3 L(Vector3 world) { return root.InverseTransformPoint(world); }
            var toLocal = Quaternion.Inverse(root.rotation);
            var mossPale = new List<CombineInstance>(); var grime = new List<CombineInstance>(); var seep = new List<CombineInstance>(); var drain = new List<CombineInstance>();
            var bills = new List<CombineInstance>(); var iron = new List<CombineInstance>(); var glass = new List<CombineInstance>(); int lamps = 0;
            // A run of wall dressed (visual only: nothing here is solid, nothing stands more than a hand off the face but the lamps'
            // brackets and the rubble at the foot, which keeps off ramps and stairs). centre, yaw: the run's box; top, foot: its top and
            // bottom; low: the low ground; fs: which side of the box is the face toward the low ground (+1 the outward side).
            void DressWall(Vector3 centre, Quaternion yaw, Quaternion q, float runLen, float top, float foot, float low, float thick, int fs)
            {
                // A point on the face: x along the run, y the world height, off how far out from the brick.
                Vector3 F(float x, float y, float off) { return centre + yaw * new Vector3(x, y - centre.y, fs * (thick / 2 + off)); }
                void Add(List<CombineInstance> to, Vector3 world, Vector3 size, float tile = 1) { to.Add(Ashlar(L(world), size, tile, q)); }
                float tall = top - low, half = runLen / 2;
                // The neon moss on the coping (CANON, book1 ch.7:17), in ragged strips: pads of their own size and green, some hanging over
                // the face's edge, a drip or two down the brick.
                for (float x = -half + .3f; x < half - .3f; x += 1.2f + R() * 2.6f)
                {
                    if (R() > .4f) continue;
                    float len = Mathf.Min(half - .2f - x, .8f + R() * 2.4f);
                    for (float px = x; px < x + len; px += .25f + R() * .35f)
                    {
                        float s = .18f + R() * .32f, across = (R() - .3f) * (thick / 2 + .08f);
                        var at = centre + yaw * new Vector3(px, top + .14f + .02f - centre.y, fs * across);
                        Add(R() < .7f ? moss : mossPale, at, new Vector3(s * (.7f + R() * .8f), .03f + R() * .05f, s * (.6f + R() * .6f)));
                        if (R() < .18f && tall > 1) { float dh = .12f + R() * .35f; Add(moss, F(px, top - dh / 2, .015f), new Vector3(.06f + R() * .12f, dh, .03f)); }
                    }
                }
                if (tall < 1.6f) return;
                // Brick pilasters about every 7 m, each on a stone plinth: the long face broken into bays.
                int bays = Mathf.Max(1, Mathf.RoundToInt(runLen / 7)); float bay = runLen / bays;
                var piers = new List<float>();
                for (int i = 0; i <= bays; i++)
                {
                    float x = -half + bay * i; x = Mathf.Clamp(x, -half + .4f, half - .4f);
                    if (runLen < 3 && (i == 0 || i == bays)) continue;
                    piers.Add(x);
                    Add(brick, F(x, (top + foot) / 2, .1f), new Vector3(.7f, top - foot - .02f, .2f), 2);
                    Add(cope, F(x, low + .2f, .14f), new Vector3(.84f, .4f, .28f), 1.75f);
                }
                bool Clear(float x, float pad) { foreach (var p in piers) if (Mathf.Abs(p - x) < pad) return false; return true; }
                // A string course of stone two-thirds up a tall face.
                if (tall > 2.2f) Add(cope, F(0, low + tall * .62f, .05f), new Vector3(runLen, .12f, .1f), 1.75f);
                // Drain mouths: a dark arch with a stone surround, the green mould glowing round it (book1 ch.6:129) and a dark seepage
                // streak down the brick under it.
                if (tall > 2.4f && runLen > 4)
                    for (int k = 0, n = runLen > 14 ? 2 : 1; k < n; k++)
                    {
                        float x = (R() - .5f) * (runLen - 2); if (!Clear(x, 1)) continue;
                        float y = low + .8f + R() * Mathf.Min(.8f, tall - 2.2f), wd = .6f + R() * .3f;
                        Add(drain, F(x, y, .005f), new Vector3(wd, wd * 1.1f, .04f));
                        Add(cope, F(x, y + wd * .6f, .04f), new Vector3(wd + .34f, .14f, .09f), 1.75f);
                        Add(cope, F(x, y + wd * .7f, .05f), new Vector3(.18f, .22f, .1f), 1.75f);
                        foreach (int sd in new[] { -1, 1 }) Add(cope, F(x + sd * (wd / 2 + .08f), y, .04f), new Vector3(.14f, wd * 1.1f + .1f, .09f), 1.75f);
                        for (int m = 0, mn = 3 + (int)(R() * 4); m < mn; m++)
                        {
                            float a = R() * Mathf.PI * 2, rr = wd * (.6f + R() * .3f), s = .08f + R() * .22f;
                            Add(mould, F(x + Mathf.Cos(a) * rr, y + Mathf.Sin(a) * rr * .9f, .05f), new Vector3(s, s * (.5f + R()), .03f));
                        }
                        float sh = y - wd * .55f - low + .05f;
                        Add(seep, F(x + (R() - .5f) * .1f, low + sh / 2 - .05f, .012f), new Vector3(wd * (.5f + R() * .3f), sh, .02f));
                        Add(mould, F(x, low + .02f, .3f), new Vector3(wd * 1.2f, .04f, .5f));
                    }
                // Soot and grime run down from the coping in streaks of their own length.
                for (float x = -half + .3f; x < half - .3f; x += .8f + R() * 2.2f)
                {
                    if (R() > .6f) continue;
                    float h = .5f + R() * Mathf.Min(2.5f, tall * .7f);
                    Add(grime, F(x, top - h / 2, .008f), new Vector3(.12f + R() * .5f, h, .015f));
                }
                // Bills pasted at eye height between the piers.
                if (tall > 2.2f)
                    for (float x = -half + 1; x < half - 1; x += 3 + R() * 4)
                    {
                        if (R() > .4f || !Clear(x, .9f)) continue;
                        float bw = .45f + R() * .3f, bh = .6f + R() * .35f;
                        Add(bills, F(x, low + 1.4f + R() * .5f, .012f), new Vector3(bw, bh, .015f));
                        if (R() < .4f) Add(bills, F(x + bw * .6f, low + 1.3f + R() * .6f, .018f), new Vector3(bw * .8f, bh * .9f, .015f));
                    }
                // An iron wall lamp on a long run now and then (a dozen at most: they are lights).
                if (tall > 2.8f && runLen > 9 && lamps < 12 && R() < .45f)
                {
                    float x = (R() - .5f) * (runLen - 3); if (!Clear(x, .6f)) x = Mathf.Clamp(x + 1.2f, -half + .5f, half - .5f);
                    float y = low + 2.7f;
                    Add(iron, F(x, y, .03f), new Vector3(.18f, .32f, .05f));
                    Add(iron, F(x, y + .1f, .32f), new Vector3(.05f, .05f, .6f));
                    Add(iron, F(x, y - .02f, .58f), new Vector3(.3f, .06f, .3f));
                    Add(glass, F(x, y - .22f, .58f), new Vector3(.22f, .32f, .22f));
                    var l = new GameObject("Wall lamp").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.position = F(x, y - .25f, .58f);
                    l.type = LightType.Point; l.range = 8; l.intensity = 1.1f; l.color = new Color(1, .68f, .36f); l.shadows = LightShadows.None;
                    NightLights.Add(new NightLight { light = l, dayIntensity = 0, nightIntensity = 1.5f });
                    lamps++;
                }
                // Rubble fallen at the foot: broken brick and a stone or two, never on a ramp or a stair.
                for (float x = -half + .5f; x < half - .5f; x += 1.5f + R() * 3)
                {
                    if (R() > .45f) continue;
                    for (int k = 0, n = 2 + (int)(R() * 4); k < n; k++)
                    {
                        float px = x + (R() - .5f) * 1.2f, off = .08f + R() * R() * .9f; var s = new Vector3(.12f + R() * .3f, .07f + R() * .14f, .1f + R() * .22f);
                        var at = F(px, low + s.y / 2 - .03f, off);
                        if (OnRampOrStair(new Vector2(at.x, at.z), .4f)) continue;
                        (R() < .7f ? brick : cope).Add(Ashlar(L(at), s, 1, toLocal * yaw * Quaternion.Euler((R() - .5f) * 20, R() * 90, (R() - .5f) * 20)));
                    }
                }
            }
            for (int ti = Zone.terraces.Length - 1; ti >= 0; ti--)
            {
                var t = Zone.terraces[ti]; if (t == null) continue;
                float blend = Mathf.Max(.25f, t.blend), rr = t.rotation * Mathf.Deg2Rad, c = Mathf.Cos(rr), s = Mathf.Sin(rr), hx = t.size.x / 2, hz = t.size.y / 2;
                // The terrace's frame to the world's (the inverse of TerraceFrame).
                Vector2 W(Vector2 l) { return new Vector2(t.center.x + l.x * c + l.y * s, t.center.y - l.x * s + l.y * c); }
                Vector2 Dir(Vector2 l) { return new Vector2(l.x * c + l.y * s, -l.x * s + l.y * c); }
                var edges = new[] { (new Vector2(-hx, -hz), new Vector2(hx, -hz), new Vector2(0, -1)), (new Vector2(hx, -hz), new Vector2(hx, hz), new Vector2(1, 0)),
                    (new Vector2(hx, hz), new Vector2(-hx, hz), new Vector2(0, 1)), (new Vector2(-hx, hz), new Vector2(-hx, -hz), new Vector2(-1, 0)) };
                foreach (var (a, b, n) in edges)
                {
                    float len = Vector2.Distance(a, b); if (len < .5f) continue;
                    int count = Mathf.Max(1, Mathf.CeilToInt(len)); float piece = len / count;
                    Vector2 along = Dir((b - a) / len), outward = Dir(n);
                    var yaw = Quaternion.LookRotation(new Vector3(outward.x, 0, outward.y));   // the box's z across the band, its x along the edge
                    // A run: from piece k0 to k1 (inclusive), its heights.
                    int k0 = -1, before = done.Count; float runHigh = 0, runLow = 0;   // before: the pieces of other edges, which this one may not repeat
                    void Flush(int k1)
                    {
                        if (k0 < 0) return;
                        float from = piece * k0, to = piece * (k1 + 1), runLen = to - from;
                        var mid = W(a) + along * ((from + to) / 2);
                        bool raised = runHigh - runLow > 1.2f; float top = runHigh + (raised ? 1 : .06f), foot = runLow - .6f, thick = blend + .3f;
                        var centre = new Vector3(mid.x + outward.x * blend / 2, (top + foot) / 2, mid.y + outward.y * blend / 2);
                        var q = toLocal * yaw;
                        brick.Add(Ashlar(L(centre), new Vector3(runLen + .02f, top - foot, thick), 2, q));
                        cope.Add(Ashlar(L(new Vector3(centre.x, top + .07f, centre.z)), new Vector3(runLen + .06f, .14f, thick + .14f), 1.75f, q));
                        Blocker(root, L(centre), new Vector3(runLen, top - foot, thick), q);
                        runs++;
                        // The face toward the low ground: the band's far side when this terrace stands high, its near side when it is sunk.
                        bool terraceHigh = HeightAt(mid.x - outward.x * .4f, mid.y - outward.y * .4f, false) > runLow + .5f;
                        DressWall(centre, yaw, q, runLen, top, foot, runLow, thick, terraceHigh ? 1 : -1);
                        // Green mould glowing at the foot of a wall whose low side is sunk under the streets (the Drain Courts).
                        if (runLow < -1)
                        {
                            float face = terraceHigh ? thick / 2 + .02f : -thick / 2 - .02f;
                            for (float x = -runLen / 2 + .3f; x < runLen / 2 - .3f; x += .9f + R() * 1.6f)
                            {
                                if (R() > .55f) continue;
                                float w = .4f + R() * 1.1f, h = .3f + R() * 1.1f;
                                var at = centre + yaw * new Vector3(x, runLow - (top + foot) / 2 + h / 2 + R() * .4f, face);
                                mould.Add(Ashlar(L(at), new Vector3(w, h, .03f), 1, q));
                            }
                        }
                        k0 = -1;
                    }
                    for (int k = 0; k < count; k++)
                    {
                        var edge = W(a) + along * (piece * (k + .5f));
                        var band = edge + outward * (blend / 2);
                        bool want = false; float high = 0, low = 0;
                        if (Mathf.Abs(band.x) < inside && Mathf.Abs(band.y) < inside && (t.ramp || !OnRamp(ramps, t, band, blend + .2f)))
                        {
                            float near = HeightAt(edge.x + outward.x * .05f, edge.y + outward.y * .05f, false), far = HeightAt(edge.x + outward.x * (blend - .05f), edge.y + outward.y * (blend - .05f), false);
                            float hin = HeightAt(edge.x - outward.x * .4f, edge.y - outward.y * .4f, false), hout = HeightAt(edge.x + outward.x * (blend + .4f), edge.y + outward.y * (blend + .4f), false);
                            if (Mathf.Abs(near - far) > .6f && Mathf.Abs(hin - hout) > .8f)
                            {
                                bool twice = false; for (int i = 0; i < before; i++) if ((done[i] - band).sqrMagnitude < 1.1f) { twice = true; break; }
                                if (!twice) { want = true; high = Mathf.Max(Mathf.Max(hin, hout), Mathf.Max(near, far)); low = Mathf.Min(Mathf.Min(hin, hout), Mathf.Min(near, far)); done.Add(band); }
                            }
                        }
                        if (want && k0 >= 0 && Mathf.Abs(high - runHigh) < .25f && Mathf.Abs(low - runLow) < .25f) { runHigh = Mathf.Max(runHigh, high); runLow = Mathf.Min(runLow, low); continue; }
                        Flush(k - 1);
                        if (want) { k0 = k; runHigh = high; runLow = low; }
                    }
                    Flush(count - 1);
                }
            }
            Joined32("Retaining walls", root, SootBrick(2), brick);
            Joined32("Retaining walls' coping", root, WallCoping, cope);
            Joined32("Neon moss on the walls", root, NeonMoss, moss);
            Joined32("Mould at the walls' foot", root, DrainMould, mould);
            Joined32("Pale neon moss on the walls", root, NeonMossPale, mossPale);
            Joined32("Soot on the walls", root, Dressed(new Color(.075f, .068f, .062f)), grime);
            Joined32("Seepage under the drains", root, Dressed(new Color(.045f, .065f, .045f)), seep);
            Joined32("Drain mouths", root, Dressed(new Color(.02f, .02f, .02f)), drain);
            Joined32("Bills on the walls", root, Tint(art.cloth, new Color(.68f, .62f, .5f)), bills);
            Joined32("Wall lamps' iron", root, SootIron, iron);
            Joined32("Wall lamps' glass", root, art.glass, glass);
            return runs;
        }

        /// <summary>A step-street over a "stair" ramp (GAME-ONLY): treads of wet setts a .4 m going across its width, each a block from under
        /// the ground at its low edge to the ground at its high edge, and a worn iron nosing on every fourth.</summary>
        void LowtownStair(Transform root, ZoneTerrace t)
        {
            var treads = new List<CombineInstance>(); var nosing = new List<CombineInstance>();
            float rr = t.rotation * Mathf.Deg2Rad, c = Mathf.Cos(rr), s = Mathf.Sin(rr);
            Vector2 W(float lx, float lz) { return new Vector2(t.center.x + lx * c + lz * s, t.center.y - lx * s + lz * c); }
            var q = Quaternion.Inverse(root.rotation) * Quaternion.Euler(0, t.rotation, 0);
            int n = Mathf.Max(1, Mathf.RoundToInt(t.size.y / .4f)); float go = t.size.y / n, width = Mathf.Max(1, t.size.x - .3f);
            for (int k = 0; k < n; k++)
            {
                float z0 = -t.size.y / 2 + k * go, z1 = z0 + go;
                var a = W(0, z0); var b = W(0, z1); var mid = W(0, (z0 + z1) / 2);
                float ha = HeightAt(a.x, a.y, false), hb = HeightAt(b.x, b.y, false), low = Mathf.Min(ha, hb) - .3f, high = Mathf.Max(ha, hb) + .02f;
                treads.Add(Ashlar(root.InverseTransformPoint(new Vector3(mid.x, (low + high) / 2, mid.y)), new Vector3(width, high - low, go + .01f), 1.25f, q));
                if (k % 4 == 3)
                {
                    var edge = W(0, z1 - .04f);
                    nosing.Add(Ashlar(root.InverseTransformPoint(new Vector3(edge.x, high + .01f, edge.y)), new Vector3(width - .1f, .03f, .08f), 1, q));
                }
            }
            Joined32("Step-street " + t.name, root, WetSetts, treads);
            Joined32("Step-street nosings " + t.name, root, SootIron, nosing);
        }
    }
}
