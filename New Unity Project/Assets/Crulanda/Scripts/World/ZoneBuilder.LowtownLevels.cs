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
        static readonly Color NeonMossColour = new Color(.36f, .9f, .16f), NeonMossGlow = new Color(.22f, .75f, .1f);
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
                        // The neon moss along the top in places, a strip or two a run (CANON, book1 ch.7:17).
                        for (float x = -runLen / 2 + .4f; x < runLen / 2 - .4f; x += 1.6f + R() * 2.4f)
                        {
                            if (R() > .38f) continue;
                            float w = Mathf.Min(runLen / 2 - x, .8f + R() * 2.2f); if (w < .4f) continue;
                            var at = centre + yaw * new Vector3(x + w / 2, (top - foot) / 2 + .15f, (R() - .5f) * thick * .5f);
                            moss.Add(Ashlar(L(at), new Vector3(w, .05f, .25f + R() * thick * .5f), 1, q));
                        }
                        // Green mould glowing at the foot of a wall whose low side is sunk under the streets (the Drain Courts).
                        if (runLow < -1)
                        {
                            // The face toward the low ground: the band's far side when this terrace stands high, its near side when it is sunk.
                            bool terraceHigh = HeightAt(mid.x - outward.x * .4f, mid.y - outward.y * .4f, false) > runLow + .5f;
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
