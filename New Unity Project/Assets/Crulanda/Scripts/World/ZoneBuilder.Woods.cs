using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The wild woods (ZoneDefinition.woods; Chris, 2026-10-14: "Filling the zones in the Elwynn style", with pictures, PLAYTEST_NOTES
    /// 172, 179 and 182). Lessons only, nothing copied: the zone's wild country is one canopy with the roads, water, camps, fields and
    /// sights cut into it, of the zone's own trees (its broadleaves and stands of pine, as the groves grow them: Chris, "id like to
    /// keep some of the trees we have now"). The great oaks (GiantTree at about half size: some 14 m, a flared, mossy bole 2 m across)
    /// grow only in a few patches of old forest, and there the wood is at its densest ("only the big tree in some areas maybe making
    /// it a dense forest area"). The floor stays open (grass, ferns, bushes, fallen trunks, stumps, mushrooms, a stone) so you see
    /// between the trunks. Glades open where the noise falls; the woods thin toward the village. A jittered grid, one tree a cell, the
    /// cells chosen by two octaves of noise so the wanted share of the open wild is wood, the old forest always wood.
    /// Every draw comes from a stream of its own: nothing else in the zone moves. GAME-ONLY dressing.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        readonly List<Vector2> woodsTrees = new List<Vector2>();
        readonly List<Transform> woodsFloor = new List<Transform>();
        bool[] woodsMask; float woodsCell; int woodsAxis;
        /// <summary>Where the wild woods' trees stand (empty in a zone with no woods).</summary>
        public IReadOnlyList<Vector2> WoodsTrees { get { return woodsTrees; } }
        /// <summary>True inside a cell of the wild woods.</summary>
        public bool InWoods(Vector2 p)
        {
            if (woodsMask == null) return false;
            int i = Mathf.FloorToInt((p.x + Half) / woodsCell), j = Mathf.FloorToInt((p.y + Half) / woodsCell);
            return i >= 0 && j >= 0 && i < woodsAxis && j < woodsAxis && woodsMask[i + j * woodsAxis];
        }

        void BuildWoods()
        {
            var w = Zone.woods; if (w == null || w.cover <= 0) return;
            var zoneRng = rng; rng = new System.Random(Zone.seed * 7919 + 104729 + w.seed);
            try { GrowWoods(w); } finally { rng = zoneRng; }
        }

        void GrowWoods(ZoneWoods w)
        {
            float cell = woodsCell = Mathf.Max(5, w.spacing); int n = woodsAxis = Mathf.CeilToInt(Zone.size / cell);
            woodsMask = new bool[n * n];
            float lim = Half - w.edge;
            float ox = R01 * 500, oz = R01 * 500, px = R01 * 500, pz = R01 * 500, gx = R01 * 500, gz = R01 * 500, dx = R01 * 500, dz = R01 * 500;
            bool Deep(Vector2 p) { return w.deepAbove < 1 && Mathf.PerlinNoise(p.x / (w.scale * 1.5f) + dx, p.y / (w.scale * 1.5f) + dz) > w.deepAbove; }
            float Lay(Vector2 p) { return Mathf.PerlinNoise(p.x / w.scale + ox, p.y / w.scale + oz) * .7f + Mathf.PerlinNoise(p.x / (w.scale * .38f) + gx, p.y / (w.scale * .38f) + gz) * .3f; }
            // Which cells are wood: of the open wild (clear of everything WoodsGround keeps clear), the share w.cover where the noise is
            // highest, the noise pulled down toward the village so the woods thin out to its fields.
            var cells = new List<(int k, Vector2 at, float v)>();
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    var c = new Vector2(-Half + (i + .5f) * cell, -Half + (j + .5f) * cell);
                    float d = c.magnitude; if (d < w.from || Mathf.Abs(c.x) > lim || Mathf.Abs(c.y) > lim || !WoodsGround(c, 0)) continue;
                    cells.Add((i + j * n, c, Lay(c) - Mathf.Clamp01((w.from + w.ramp - d) / Mathf.Max(1, w.ramp)) * .3f + (Deep(c) ? 1 : 0)));   // the old forest is always wood
                }
            if (cells.Count == 0) return;
            cells.Sort((a, b) => b.v.CompareTo(a.v));
            int keep = Mathf.Clamp(Mathf.RoundToInt(cells.Count * Mathf.Clamp01(w.cover)), 0, cells.Count);
            float cut = keep > 0 ? cells[keep - 1].v : float.MaxValue;
            for (int k = 0; k < keep; k++) woodsMask[cells[k].k] = true;
            cells.Sort((a, b) => a.k.CompareTo(b.k));   // grown in grid order, so the woods do not hang on the sort's ties
            int oaks = 0, broad = 0, pines = 0, snags = 0, floor = 0;
            foreach (var c in cells)
            {
                if (!woodsMask[c.k]) continue;
                // The cell's draws, all taken whatever grows, so a tree dropped never moves the next.
                float jx = R01, jz = R01, roll = R01, size = R01, look = R01, yaw = R01, f = R01, fa = R01, fr = R01, fy = R01, fs = R01;
                var at = c.at + new Vector2((jx - .5f) * cell * .8f, (jz - .5f) * cell * .8f);
                bool rim = c.v < cut + .035f;   // the wood's ragged rim: every third tree missing
                bool deep = Deep(c.at), pine = !deep && Mathf.PerlinNoise(at.x / (w.scale * .6f) + px, at.y / (w.scale * .6f) + pz) > w.pineAbove;
                if (!(rim && roll > .66f) && WoodsGround(at, 1.5f))
                {
                    Transform tree = null; var spot = Spot(at, pine ? 2.6f : 3.2f);
                    if (spot.HasValue)
                    {
                        at = spot.Value;
                        if (pine)
                        {
                            tree = Root(new ZoneProp { kind = "pine", at = at, rotation = yaw * 360, scale = .85f + size * .5f }, statics); Pine(tree);
                            if (!SeatPine(tree, at)) { DestroyImmediate(tree.gameObject); tree = null; } else pines++;
                        }
                        else if (roll < w.snags) { tree = DeadTree(at, .6f + size * .5f, DeadBark(), 3, statics); snags++; }
                        else if (roll < w.snags + (deep ? w.deepOaks : w.oaks) && TrunkClear(at, 7) && !NearRoad(at, 6) && WoodsGround(at, 5))
                        {
                            // A great oak of the old forest: the giant's own build at about half size; its leaf mostly deep or yellow green, now and then gold.
                            tree = Root(new ZoneProp { kind = "giant_tree", at = at, rotation = yaw * 360, scale = w.oakScale * (.85f + size * .35f) }, statics);
                            GiantTree(tree, look < .5f ? 0 : look < .85f ? 1 : 2); oaks++;
                        }
                        else
                        {
                            // The zone's own broadleaf, as a grove grows it (a little taller in the old forest).
                            tree = Root(new ZoneProp { kind = "tree", at = at, rotation = yaw * 360, scale = (deep ? 1.1f : .8f) + size * .5f }, statics);
                            Broadleaf(tree, (int)(look * 4)); broad++;
                        }
                    }
                    if (tree != null) { if (Hollow.CoverAt(at, 3) > 0) DestroyImmediate(tree.gameObject); else { trunks.Add(at); woodsTrees.Add(at); } }
                }
                // The floor, a step off the trunk.
                float a = fa * Mathf.PI * 2; var fp = c.at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (2.6f + fr * 2.4f);
                if (f >= w.floor * .62f || !WoodsGround(fp, 1) || !TrunkClear(fp, 1.9f)) continue;
                int before = statics.childCount;
                if (f < w.floor * .36f) Bush(fp, pine);
                else if (f < w.floor * .44f) FallenTrunk(fp, fy, fs);
                else if (f < w.floor * .5f) WoodsStump(fp, fy, fs);
                else if (f < w.floor * .57f) WoodsMushrooms(fp, fy, fs);
                else EdgeRock(fp);
                for (int k = before; k < statics.childCount; k++) { woodsFloor.Add(statics.GetChild(k)); floor++; }
            }
            Debug.Log("Woods of " + Zone.id + ": " + keep + " of " + cells.Count + " open cells (" + cell + " m), " + oaks + " great oaks, " + broad
                + " broadleaves, " + pines + " pines, " + snags + " snags, " + floor + " on the floor.");
        }

        /// <summary>Whether the wild woods may grow at a point: off the roads, water, caves and the grey; clear of every prop, clearing,
        /// sight, lake, tall-grass patch, ploughed field, camp, grove, wallow, exit, spawn, gathering node, secret, station and flock,
        /// each by its own margin plus <paramref name="more"/>.</summary>
        bool WoodsGround(Vector2 p, float more)
        {
            if (NearRoad(p, 3.5f + more) || Water.NearWater(p, 3 + more) || NearProp(p, 6 + more)) return false;
            if (Hollow.CoverAt(p, 4 + more) > 0 || Unmade(p.x, p.y) > 0) return false;
            if (Zone.wasting != null && p.x > Zone.wasting.x - 30) return false;
            foreach (var c in Zone.clearings) if (c != null && Vector2.Distance(p, c.center) < c.radius + 3 + more) return false;
            foreach (var l in Zone.landmarks) if (l != null && Vector2.Distance(p, l.at) < l.radius * .7f + 2 + more) return false;
            if (Zone.lakes != null) foreach (var l in Zone.lakes) if (l != null && Vector2.Distance(p, l.center) < l.radius + 4 + more) return false;
            if (Zone.tallGrass != null) foreach (var g in Zone.tallGrass) if (g != null && Vector2.Distance(p, g.center) < g.radius + 2 + more) return false;
            foreach (var f in Zone.fields)
            {
                if (f == null) continue; var local = Rotate(p - f.center, -f.rotation);
                if (Mathf.Abs(local.x) < f.size.x / 2 + 4 + more && Mathf.Abs(local.y) < f.size.y / 2 + 4 + more) return false;
            }
            if (Zone.camps != null) foreach (var c in Zone.camps) if (c != null && Vector2.Distance(p, c.center) < c.radius * 1.42f + 2 + more) return false;
            foreach (var g in Zone.groves)
                if (g != null && Mathf.Abs(p.x - g.center.x) < g.size.x / 2 + 3 + more && Mathf.Abs(p.y - g.center.y) < g.size.y / 2 + 3 + more) return false;
            if (Zone.shapes != null) foreach (var s in Zone.shapes) if (s != null && !string.IsNullOrEmpty(s.paint) && ShapeCover(s, p) > .2f) return false;
            foreach (var e in Zone.exits) if (e != null && Vector2.Distance(p, e.at) < e.radius + 10 + more) return false;
            if (Zone.spawns != null)
            {
                var s = Zone.spawns;
                if (Vector2.Distance(p, s.player) < 12 + more || Vector2.Distance(p, s.companion) < 12 + more || Vector2.Distance(p, s.recovery) < 12 + more) return false;
                if (s.enemies != null) foreach (var e in s.enemies) if (e != null && Vector2.Distance(p, e.at) < 5 + more) return false;
            }
            foreach (var o in Zone.nodes) if (o != null && Vector2.Distance(p, o.at) < 5 + more) return false;
            foreach (var s in Zone.secrets) if (s != null && Vector2.Distance(p, s.at) < s.radius + 4 + more) return false;
            if (Zone.stations != null) foreach (var s in Zone.stations) if (s != null && Vector2.Distance(p, s.at) < 6 + more) return false;
            if (Zone.life != null && Zone.life.critters != null) foreach (var c in Zone.life.critters) if (c != null && Vector2.Distance(p, c.center) < c.radius + 3 + more) return false;
            return true;
        }

        /// <summary>A fallen trunk, 4.5-7.5 m long and up to a metre through, lying along the ground with its torn root plate standing up
        /// at one end and its broken heartwood showing at the other, two snapped limbs on it. Solid, kept off the navmesh.</summary>
        void FallenTrunk(Vector2 at, float r1, float r2)
        {
            var t = Root(new ZoneProp { kind = "log", name = "Fallen trunk", at = at, rotation = r1 * 360 }, statics);
            var bark = Tint(art.bark, Wither(new Color(.36f, .31f, .26f))); var heart = Tint(art.timber, Wither(new Color(.56f, .46f, .33f)));
            var earth = Tint(art.bark, Wither(new Color(.24f, .2f, .16f)));
            float len = 4.5f + r2 * 3, rad = .36f + r2 * .16f;
            var a = new Vector3(-len / 2, LocalGround(t, -len / 2, 0) + rad * .75f, 0); var b = new Vector3(len / 2, LocalGround(t, len / 2, 0) + rad * .7f, 0);
            var d = b - a; var along = Quaternion.FromToRotation(Vector3.up, d.normalized);
            Part(PrimitiveType.Cylinder, t, (a + b) / 2, new Vector3(rad * 2, d.magnitude / 2, rad * 2), bark, along);
            Part(PrimitiveType.Cylinder, t, b + d.normalized * .01f, new Vector3(rad * 1.75f, .015f, rad * 1.75f), heart, along);   // the break
            Part(PrimitiveType.Cylinder, t, a - d.normalized * .12f + Vector3.up * rad * .3f, new Vector3(rad * 4.4f, .16f, rad * 4f), earth, along);   // the root plate
            for (int k = 0; k < 2; k++)
            {
                var from = a + d * (.3f + k * .35f) + Vector3.up * rad * .8f; var up = (Vector3.up + d.normalized * (k == 0 ? .5f : -.4f) + Vector3.forward * (k == 0 ? .3f : -.3f)).normalized;
                Part(PrimitiveType.Cylinder, t, from + up * .3f, new Vector3(.12f, .32f, .12f), bark, Quaternion.FromToRotation(Vector3.up, up));
            }
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.direction = 0; cap.center = (a + b) / 2; cap.height = len; cap.radius = rad;
            t.gameObject.AddComponent<NavBlocker>();
        }

        /// <summary>An old stump, sawn or broken, a metre or so across on its root flare.</summary>
        void WoodsStump(Vector2 at, float r1, float r2)
        {
            var t = Root(new ZoneProp { kind = "stump", name = "Stump", at = at, rotation = r1 * 360, scale = .9f + r2 * .5f }, statics);
            var sr = TreeRandom(t.position); SecretStump(t, () => (float)sr.NextDouble(), .35f);
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, .45f, .32f); cap.height = .9f; cap.radius = .32f;
            t.gameObject.AddComponent<NavBlocker>();
        }

        /// <summary>A ring of mushrooms on the leaf litter (the nature kit's; none when the kit is missing).</summary>
        void WoodsMushrooms(Vector2 at, float r1, float r2)
        {
            if (!HasProp("Nature/Mushroom_Common")) return;
            var t = Root(new ZoneProp { kind = "mushrooms", name = "Mushrooms", at = at, rotation = r1 * 360 }, statics);
            var mr = TreeRandom(t.position);
            for (int k = 0, m = 3 + (int)(r2 * 4); k < m; k++)
            {
                float a = (float)mr.NextDouble() * Mathf.PI * 2, r = .2f + (float)mr.NextDouble() * .7f;
                var go = ModelProp(t, (float)mr.NextDouble() < .3f && HasProp("Nature/Mushroom_Laetiporus") ? "Nature/Mushroom_Laetiporus" : "Nature/Mushroom_Common",
                    Vector3.zero, (float)mr.NextDouble() * 360, .18f + (float)mr.NextDouble() * .2f);
                if (go == null) continue;
                var lp = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); lp.y = LocalGround(t, lp.x, lp.z); go.transform.localPosition = lp;
                DressNature(go);
            }
        }

        /// <summary>After the merge: the woods' floor (bushes, trunks, stumps, mushrooms, stones) is not drawn once it is small on the
        /// screen, so the woods cost little past the near distance. The trees themselves stay (they make the skyline).</summary>
        void CullWoodsFloor()
        {
            foreach (var t in woodsFloor)
            {
                if (t == null) continue; var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var lod = t.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.012f, rs) }); lod.RecalculateBounds();
            }
        }
    }
}
