using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Water in a dungeon's caves (the Adit rework, Docs/ADIT_REWORK.md; GAME-ONLY; ZoneCaveWater): pools and streams sunk into a cave's
    /// floor (Cavern lays the floor down into each basin, its banks easing up), and flooded stretches where the water stands from wall to
    /// wall until the floor climbs out of it. Each has a flat surface of the zone's water, rubble along a basin's banks, and the volumes
    /// the navmesh marks as shallows (wading costs) or swim-deep (not walked); <see cref="WaterAtPoint"/> is what you feel. None of it is
    /// there without water in the data: every cave's floor is as it was.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>A cave's water as worked out: kind 0 a pool, 1 a stream, 2 a flood; a pool's centre; the surface (a stream's follows the
        /// floor, .12 under it); how deep; how wide its banks ease; the stretch it lies along.</summary>
        sealed class CaveWaterSpot
        {
            public ZoneCaveWater def; public Hollow cave; public int kind;
            public Vector2 centre; public float level, depth, bank, s0, s1, radius, half, aside;
        }
        readonly List<CaveWaterSpot> caveWaters = new List<CaveWaterSpot>();
        /// <summary>The caves' water for the navmesh (EncounterNavigation): volumes, each its middle, its turn and its size, and whether it is
        /// swim-deep there (else wading).</summary>
        public readonly List<(Vector3 centre, Quaternion rot, Vector3 size, bool swim)> CaveWaterAreas = new List<(Vector3 centre, Quaternion rot, Vector3 size, bool swim)>();

        /// <summary>Whether a cave has a pool or a stream sunk into its floor (Cavern lays its floor in columns, let down into them).</summary>
        bool HasBasins(Hollow h) { foreach (var w in caveWaters) if (w.cave == h && w.kind != 2) return true; return false; }
        /// <summary>How far a cave's floor is let down at a point into the pools and streams sunk in it (0 on dry floor): .12 at the
        /// waterline, easing to the full depth (and the .12) over the bank, and back up to the floor within .4 m outside it.</summary>
        float BasinDip(Hollow h, Vector2 p)
        {
            if (caveWaters.Count == 0 || h == null) return 0;
            float dip = 0;
            foreach (var w in caveWaters)
            {
                if (w.cave != h || w.kind == 2) continue;
                float e;   // how far out of the water's edge (less than 0: in it)
                if (w.kind == 0) e = Vector2.Distance(p, w.centre) - w.radius;
                else { h.Local(p, out float s, out float a); e = Mathf.Max(Mathf.Abs(a - w.aside) - w.half, Mathf.Max(w.s0 - s, s - w.s1)); }
                dip = Mathf.Max(dip, Dip(e, w.depth, w.bank));
            }
            return dip;
        }
        static float Dip(float e, float depth, float bank)
        {
            if (e >= .4f) return 0;
            if (e >= 0) return .12f * (1 - e / .4f);
            return .12f + depth * Mathf.SmoothStep(0, 1, -e / bank);
        }
        /// <summary>Whether a point in a cave is in its water, or within <paramref name="margin"/> of a pool's or a stream's edge (nothing
        /// is set down there: ZoneBuilder.Adit's Free).</summary>
        bool InCaveWater(Hollow h, Vector3 w, float margin)
        {
            if (caveWaters.Count == 0) return false;
            var p = new Vector2(w.x, w.z);
            foreach (var cw in caveWaters)
            {
                if (cw.cave != h) continue;
                if (cw.kind == 0) { if (Vector2.Distance(p, cw.centre) < cw.radius + .4f + margin) return true; continue; }
                h.Local(p, out float s, out float a);
                if (cw.kind == 1) { if (Mathf.Max(Mathf.Abs(a - cw.aside) - cw.half, Mathf.Max(cw.s0 - s, s - cw.s1)) < .4f + margin) return true; continue; }
                if (s >= cw.s0 && s <= cw.s1 && h.At(s).y < cw.level - .1f) return true;
            }
            return false;
        }
        /// <summary>The surface of a cave's water at a point (a stream's follows its cave's floor).</summary>
        float WaterLevel(CaveWaterSpot w, Vector2 p)
        {
            if (w.kind != 1) return w.level;
            w.cave.Local(p, out float s, out _); return w.cave.At(Mathf.Clamp(s, w.s0, w.s1)).y - .12f;
        }

        void PrepareCaveWater()
        {
            if (Zone.caveWater == null) return;
            foreach (var d in Zone.caveWater)
            {
                if (d == null) continue;
                var h = CaveNamed(d.cave);
                if (h == null || h.Centre.Count < 2) { Debug.LogWarning(Zone.id + ": the cave water '" + d.name + "' names a cave the zone has not got ('" + d.cave + "')."); continue; }
                var w = new CaveWaterSpot { def = d, cave = h, depth = Mathf.Clamp(d.depth > 0 ? d.depth : 1, .1f, 8), aside = d.aside, half = d.half > 0 ? d.half : 2.5f, radius = d.radius };
                float a = Mathf.Clamp(Mathf.Min(d.from, d.to), 0, h.Length), b = Mathf.Clamp(Mathf.Max(d.from, d.to), 0, h.Length);
                if (d.flood)
                {
                    // Standing water: over the lowest floor of the stretch, and on either way until the floor climbs out of it.
                    w.kind = 2; float lowest = float.MaxValue;
                    for (float s = a; s <= b + .01f; s += .5f) lowest = Mathf.Min(lowest, h.At(s).y);
                    w.level = lowest + w.depth; w.s0 = a; w.s1 = b;
                    while (w.s0 > 0 && a - w.s0 < 40 && h.At(w.s0 - .5f).y < w.level - .02f) w.s0 -= .5f;
                    while (w.s1 < h.Length && w.s1 - b < 40 && h.At(w.s1 + .5f).y < w.level - .02f) w.s1 += .5f;
                }
                else if (d.radius > 0)
                {
                    w.kind = 0; var c = h.At(d.from, d.aside); w.centre = new Vector2(c.x, c.z); w.level = c.y - .12f;
                    w.bank = Mathf.Clamp(w.depth * 2, 1.2f, Mathf.Max(1.2f, d.radius * .6f));
                }
                else
                {
                    w.kind = 1; w.s0 = a; w.s1 = b;
                    w.bank = Mathf.Clamp(w.depth * 2, .8f, Mathf.Max(.8f, w.half * .7f));
                }
                caveWaters.Add(w);
            }
        }

        /// <summary>
        /// Water at a world point, a cave's or the land's: its surface and how deep it is over the bed there. A cave's water counts only for a
        /// point in its cave from a metre under its bed to three over its surface (a bridge high over a pool is dry). Elsewhere, as
        /// <see cref="WaterAt"/> at the point's x and z.
        /// </summary>
        public bool WaterAtPoint(Vector3 p, out float surface, out float depth)
        {
            var q = new Vector2(p.x, p.z);
            foreach (var w in caveWaters)
            {
                if (!w.cave.FloorAt(q, out float floor)) continue;
                float level, bed;
                if (w.kind == 2)
                {
                    w.cave.Local(q, out float s, out _); if (s < w.s0 || s > w.s1) continue;
                    level = w.level; bed = w.cave.FloorSmooth(q, out float smooth) ? smooth : floor;
                }
                else
                {
                    float dip = BasinDip(w.cave, q); if (dip <= .12f) continue;
                    level = WaterLevel(w, q); bed = floor - dip;
                }
                if (level - bed < .02f || p.y < bed - 1 || p.y > level + 3) continue;
                surface = level; depth = level - bed; return true;
            }
            return WaterAt(q, out surface, out depth);
        }

        /// <summary>The caves' water built: surfaces, the rubble along the basins' banks, and the navmesh's volumes.</summary>
        void BuildCaveWater(Transform root, Transform look)
        {
            if (caveWaters.Count == 0) return;
            var mat = WaterMaterial(); var rim = RockTint(new Color(.36f, .34f, .31f));
            void Area(Vector3 centre, Vector3 along, Vector3 size, bool swim)
            {
                along.y = 0; if (along.sqrMagnitude < 1e-6f) along = Vector3.forward;
                CaveWaterAreas.Add((centre, Quaternion.LookRotation(along.normalized), size, swim));
            }
            foreach (var w in caveWaters)
            {
                var h = w.cave; var fm = new FaceMesh();
                var wr = TreeRandom(h.At(w.def.from, w.aside) + new Vector3(8.3f, 0, 1.9f)); float W() { return (float)wr.NextDouble(); }
                bool Bank(Vector3 at) { return h.FloorAt(new Vector2(at.x, at.z), out _) && !InDeckWay(at, .3f); }
                float swimIn = w.depth > 1.45f ? w.bank * 1.45f / w.depth : -1;   // how far in from the edge it is swim-deep (about)
                if (w.kind == 0)
                {
                    const int Seg = 40; var c = new Vector3(w.centre.x, w.level, w.centre.y); float r = w.radius + .1f;
                    for (int k = 0; k < Seg; k++)
                    {
                        float a0 = k * Mathf.PI * 2 / Seg, a1 = (k + 1) * Mathf.PI * 2 / Seg;
                        var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r;
                        fm.Tri(c, p0, p1, Vector3.up, new Vector2(c.x, c.z) / 4, new Vector2(p0.x, p0.z) / 4, new Vector2(p1.x, p1.z) / 4);
                    }
                    for (float arc = 0; arc < Mathf.PI * 2 * (w.radius + .4f); arc += 1.7f)   // rubble round its rim
                    {
                        float ang = arc / (w.radius + .4f), size = .35f + W() * .45f, yaw = W() * 360;
                        var at = new Vector3(w.centre.x + Mathf.Cos(ang) * (w.radius + .35f + size * .3f), 0, w.centre.y + Mathf.Sin(ang) * (w.radius + .35f + size * .3f));
                        at.y = w.level + .12f - size * .35f; if (!Bank(at)) continue;
                        Lump(BoulderAt((int)(yaw / 60)), look, at, new Vector3(size * 1.3f, size, size * 1.1f), rim, yaw);
                    }
                    const int Wedges = 32; float bottom = w.level - w.depth - 1, height = w.level + .05f - bottom;
                    for (int k = 0; k < Wedges; k++)
                    {
                        float ang = (k + .5f) * Mathf.PI * 2 / Wedges; var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                        foreach (var (reach, swim) in new[] { (w.radius + .1f, false), (w.radius - swimIn, true) })
                        {
                            if (reach < .3f || (swim && swimIn < 0)) continue;
                            var mid = new Vector3(w.centre.x, (bottom + w.level + .05f) / 2, w.centre.y) + dir * reach / 2;
                            Area(mid, dir, new Vector3(reach * Mathf.PI * 2 / Wedges + .3f, height, reach), swim);
                        }
                    }
                }
                else if (w.kind == 1)
                {
                    int steps = Mathf.Max(1, Mathf.CeilToInt((w.s1 - w.s0) / 1f));
                    for (int k = 0; k < steps; k++)
                    {
                        float sa = Mathf.Lerp(w.s0, w.s1, k / (float)steps), sb = Mathf.Lerp(w.s0, w.s1, (k + 1) / (float)steps);
                        float ya = h.At(sa).y - .12f, yb = h.At(sb).y - .12f;
                        var la = h.At(sa, w.aside - w.half - .1f); var ra = h.At(sa, w.aside + w.half + .1f); var lb = h.At(sb, w.aside - w.half - .1f); var rb = h.At(sb, w.aside + w.half + .1f);
                        la.y = ra.y = ya; lb.y = rb.y = yb;
                        fm.Quad(la, ra, rb, lb, Vector3.up, new Vector2(la.x, la.z) / 4, new Vector2(ra.x, ra.z) / 4, new Vector2(rb.x, rb.z) / 4, new Vector2(lb.x, lb.z) / 4);
                    }
                    for (float s = w.s0; s <= w.s1; s += 1.8f)   // rubble banks
                        foreach (int side in new[] { -1, 1 })
                        {
                            float size = .3f + W() * .4f, yaw = W() * 360;
                            var at = h.At(s, w.aside + side * (w.half + .35f + size * .3f)); at.y += -size * .35f; if (!Bank(at)) continue;
                            Lump(BoulderAt((int)(yaw / 60)), look, at, new Vector3(size * 1.3f, size, size * 1.1f), rim, yaw);
                        }
                    for (float s = w.s0; s < w.s1; s += 1.5f)
                    {
                        float sm = Mathf.Min(w.s1, s + .75f), len = Mathf.Min(1.5f, w.s1 - s) + .3f, level = h.At(sm).y - .12f, bottom = level - w.depth - 1;
                        var mid = h.At(sm, w.aside); mid.y = (bottom + level + .05f) / 2; var along = h.At(sm + .5f) - h.At(sm - .5f);
                        Area(mid, along, new Vector3(w.half * 2 + .2f, level + .05f - bottom, len), false);
                        if (swimIn >= 0 && w.half - swimIn > .3f) Area(mid, along, new Vector3((w.half - swimIn) * 2, level + .05f - bottom, len), true);
                    }
                }
                else
                {
                    // From wall to wall at the surface's height (the rock's lining stands a little in from the shell).
                    int steps = Mathf.Max(1, Mathf.CeilToInt((w.s1 - w.s0) / .7f));
                    float Right(float s) { return WallOut(h, RingIndex(h, s), 1, Mathf.Max(0, w.level - h.At(s).y)) - .05f; }
                    float Left(float s) { return WallOut(h, RingIndex(h, s), -1, Mathf.Max(0, w.level - h.At(s).y)) - .05f; }
                    for (int k = 0; k < steps; k++)
                    {
                        float sa = Mathf.Lerp(w.s0, w.s1, k / (float)steps), sb = Mathf.Lerp(w.s0, w.s1, (k + 1) / (float)steps);
                        var la = h.At(sa, -Left(sa)); var ra = h.At(sa, Right(sa)); var lb = h.At(sb, -Left(sb)); var rb = h.At(sb, Right(sb));
                        la.y = ra.y = lb.y = rb.y = w.level;
                        fm.Quad(la, ra, rb, lb, Vector3.up, new Vector2(la.x, la.z) / 4, new Vector2(ra.x, ra.z) / 4, new Vector2(rb.x, rb.z) / 4, new Vector2(lb.x, lb.z) / 4);
                    }
                    for (float s = w.s0; s < w.s1; s += 1.5f)
                    {
                        float sm = Mathf.Min(w.s1, s + .75f), len = Mathf.Min(1.5f, w.s1 - s) + .3f, floor = h.At(sm).y, bottom = floor - 1;
                        float l = Left(sm), r = Right(sm); var mid = h.At(sm, (r - l) / 2); mid.y = (bottom + w.level + .05f) / 2;
                        Area(mid, h.At(sm + .5f) - h.At(sm - .5f), new Vector3(l + r + .2f, w.level + .05f - bottom, len), w.level - floor > 1.45f);
                    }
                }
                if (fm.Count == 0) continue;
                var go = new GameObject("Cave water: " + (string.IsNullOrEmpty(w.def.name) ? h.Name : w.def.name)); go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = fm.Make(go.name);
                // (the drowned vaults) The black canal is the Chemical River's water.
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = w.def.kind == ChemicalKind ? ChemicalWater() : mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                if (art.waterSurface == null) go.AddComponent<WaterDrift>();
            }
        }
    }
}
