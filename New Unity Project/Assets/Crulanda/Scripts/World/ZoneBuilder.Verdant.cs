using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The lush props of the Verdant Shore (CANON, Book 3: "a forest that didn't know when to stop", "giant trees, their trunks as
    /// wide as houses"): giant trees, the Veridian Keepers' dwellings built into them, waterfalls, clusters of mushrooms (some that
    /// glow after dark) and rope-and-post footbridges. Each draws from its own stream (TreeRandom, seeded by where it stands), so
    /// adding one never moves anything else in a zone.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>
        /// A giant tree, about 28 m tall at scale 1: a bole about 4.5 m across rising from a flare of buttresses that spreads about
        /// 9 m and is green with moss, surface roots running out into the ground, six or seven great limbs parting at about two
        /// thirds of the bole's height and sweeping out and up, each with two lesser branches, and a broad layered crown of painted
        /// leaf from about 16 m to 30 m, reaching about 12 m out. Variant 0 deep green, 1 yellow-green, 2 autumn gold. Solid trunk,
        /// a navmesh obstacle, and it fades when it hides you (TreeFade). Returns the bole's height.
        /// </summary>
        float GiantTree(Transform t, int variant)
        {
            LeafTrees.Add(t.position);
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            var bark = Tint(art.bark, Wither(Color.Lerp(new Color(.34f, .27f, .2f), new Color(.4f, .34f, .27f), T())));
            var moss = Tint(art.bark, Wither(Color.Lerp(new Color(.2f, .27f, .13f), new Color(.26f, .33f, .15f), T())));   // the flare and roots, mossed over
            float h = 15 + T() * 3, r0 = 2.05f + T() * .4f;
            float lx = (T() - .5f) * .03f, lz = (T() - .5f) * .03f, bend = (T() - .5f) * .8f, ph = T() * 6.3f, seed = T() * 60;
            const int Buttresses = 7; var bAt = new float[Buttresses]; var bW = new float[Buttresses];
            for (int i = 0; i < Buttresses; i++) { bAt[i] = (i + (T() - .5f) * .55f) * 2 * Mathf.PI / Buttresses; bW[i] = .7f + T() * .5f; }
            Vector3 Axis(float y) { float s = Mathf.Sin(y / h * 2.6f + ph) * bend; return new Vector3(lx * y + s, y, lz * y + s * .6f); }
            float Girth(float y, float a)
            {
                float r = r0 * (1 - .38f * Mathf.Clamp01(y / h)) * (1 + .12f * (Mathf.PerlinNoise(Mathf.Cos(a) * 1.3f + seed, Mathf.Sin(a) * 1.3f + y * .35f) - .5f)) + .04f * r0 * Mathf.Sin(a * 11 + y * .7f);
                float flare = Mathf.Exp(-Mathf.Max(0, y + .2f) / 2.4f), ridge = 0;   // a long concave swell into the buttresses
                for (int i = 0; i < Buttresses; i++) ridge += bW[i] * Mathf.Pow(Mathf.Max(0, Mathf.Cos(a - bAt[i] - y * .05f)), 7);
                return (r + r0 * flare * (.25f + 1.1f * ridge)) * Mathf.Clamp01((h + 1.5f - y) / (r0 * 1.2f));
            }
            // Two parts of the one surface (they meet without a seam): the mossy flare, low and wide, and the bark of the bole.
            MeshPart(ZoneMeshes.Tube(Axis, Girth, new[] { -.9f, -.5f, -.2f, .1f, .4f, .8f, 1.3f, 1.9f, 2.6f, 3.4f }, 28, Vector3.right, 4, .3f), t, Vector3.zero, moss).name = "Giant root flare";
            MeshPart(ZoneMeshes.Tube(Axis, Girth, new[] { 3.4f, 4.6f, 6, 7.5f, 9, 10.5f, 12, 13.5f, h, h + .8f, h + 1.5f }, 28, Vector3.right, 4, .3f), t, Vector3.zero, bark).name = "Giant bole";
            // Surface roots out of the buttresses, flattened, sinking into the ground as they thin.
            var roots = new List<CombineInstance>();
            for (int i = 0; i < Buttresses; i++)
            {
                float a = bAt[i], len = 3.2f + T() * 2.6f, rr = .55f * bW[i] + .2f, wig = (T() - .5f) * 1.2f, ph2 = T() * 6;
                var dir = new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a)); var across = new Vector3(-dir.z, 0, dir.x);
                Vector3 C(float s) { var p = dir * (r0 * 1.5f + len * s) + across * (wig * Mathf.Sin(s * 3 + ph2) * s); return new Vector3(p.x, LocalGround(t, p.x, p.z) + .45f - .8f * s, p.z); }
                float R(float s, float ang) { return Mathf.Lerp(rr, .1f, s) * (1 - .4f * Mathf.Abs(Mathf.Cos(ang))); }
                roots.Add(new CombineInstance { mesh = ZoneMeshes.Tube(C, R, new[] { 0, .12f, .25f, .4f, .55f, .7f, .85f, 1 }, 10, Vector3.up, 1, len * .4f), transform = Matrix4x4.identity });
            }
            var rootMesh = new Mesh { name = "Giant roots" }; rootMesh.CombineMeshes(roots.ToArray(), true, false); rootMesh.RecalculateBounds();
            foreach (var piece in roots) DestroyImmediate(piece.mesh);
            MeshPart(rootMesh, t, Vector3.zero, moss);
            // Great limbs out of the bole's upper third, sweeping out and up; each ends in a cluster spot and carries two branches.
            var ends = new List<Vector3> { Axis(h + .5f) }; var spots = new List<(Vector3 at, float size, bool light)> { (Axis(h) + Vector3.up * 9, 7.5f, true) };   // the crown's top first
            int limbs = 6 + (int)(T() * 2); float turn = T() * 360;
            for (int i = 0; i < limbs; i++)
            {
                float yaw = turn + i * 360f / limbs + (T() - .5f) * 22, y0 = h * (.6f + T() * .28f);
                var outward = Quaternion.Euler(0, yaw, 0) * Vector3.right; float reach = 6.5f + T() * 3.5f, rise = 4.5f + T() * 4;
                var from = Axis(y0); var to = from + outward * reach + Vector3.up * rise;
                Limb(t, from, to, r0 * (.34f + T() * .08f), .28f, bark, .14f, 12);
                ends.Add(to); spots.Add((to + Vector3.up * 1.6f, 6 + T() * 2, i % 3 == 1));
                for (int k = 0; k < 2; k++)
                {
                    var p = Vector3.Lerp(from, to, .45f + k * .25f);
                    var dir = (Quaternion.Euler(0, (k == 0 ? 1 : -1) * (30 + T() * 25), 0) * outward + Vector3.up * (.45f + T() * .4f)).normalized;
                    var end = p + dir * (3.6f + T() * 2.2f);
                    Limb(t, p, end, r0 * .13f, .07f, bark, .1f, 8); ends.Add(end);
                    spots.Add((end + Vector3.up * .8f, 4.4f + T() * 1.6f, k == 1));
                }
            }
            int family = variant == 1 ? 1 : variant == 2 ? 2 : 0;
            var core = Tint(art.foliage, Wither(Color.Lerp(Leaf[family], Color.black, .5f)));
            LeafCrown(t, tr, Axis(h) + Vector3.up * 6, spots, ends, LeafMaterial(family), bark, core, 4.5f, new Vector3(18, 8, 18), 3, 34, 44, 10, 14);
            var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h / 2, 0); cap.height = h + 2; cap.radius = r0 * 1.05f;
            var foot = t.gameObject.AddComponent<CapsuleCollider>(); foot.center = new Vector3(0, .9f, 0); foot.height = 1.8f + r0 * 3.4f; foot.radius = r0 * 1.7f; foot.direction = 1;   // the buttresses
            t.gameObject.AddComponent<NavBlocker>(); t.gameObject.AddComponent<TreeFade>();
            return h;
        }

        /// <summary>
        /// A Veridian Keeper dwelling (CANON-EXPANDED: the Keepers are Book 3's native wood-beings; their houses are GAME-ONLY): a giant
        /// tree with a round house of timber and wattle grown into the front of its flare (door, porch and lantern to its -Z, round
        /// windows lit warm), a platform of planks round the trunk at about nine metres on struts, with a smaller house on it under a
        /// tiled cone of a roof, a rail, lanterns, and a stair of planks winding up the trunk to it. The houses are scenery (not
        /// entered); the round house is solid.
        /// </summary>
        void Treehouse(Transform t, int variant)
        {
            GiantTree(t, variant);
            var tr = new System.Random(TreeRandom(t.position).Next() ^ 0x5eed); float T() { return (float)tr.NextDouble(); }
            var wood = Tint(art.timber, new Color(.36f, .25f, .16f)); var dark = Tint(art.timber, new Color(.22f, .15f, .1f));
            var wattle = Tint(art.plaster, new Color(.74f, .68f, .54f)); var tile = Tint(art.slate, new Color(.62f, .3f, .19f)); var stone = Tint(art.stone, new Color(.5f, .48f, .44f));
            if (robeCone == null) robeCone = ZoneMeshes.Cone(1, 1, 14);
            // The round house, in the flare's front: wattle walls between timber posts, a tiled cone of a roof with a deep eave.
            var house = new Vector3(0, 0, -3.6f); float hr = 2.7f, wall = 3;
            float floor = Mathf.Max(LocalGround(t, house.x, house.z), LocalGround(t, house.x, house.z - hr)) - .1f;
            Part(PrimitiveType.Cylinder, t, house + new Vector3(0, floor + .25f, 0), new Vector3(hr * 2 + .3f, .3f, hr * 2 + .3f), stone);   // the footing
            Part(PrimitiveType.Cylinder, t, house + new Vector3(0, floor + .4f + wall / 2, 0), new Vector3(hr * 2, wall / 2, hr * 2), wattle);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4 + Mathf.PI / 8; Part(PrimitiveType.Cylinder, t, house + new Vector3(Mathf.Cos(a) * hr, floor + .4f + wall / 2, Mathf.Sin(a) * hr), new Vector3(.24f, wall / 2 + .1f, .24f), wood); }
            Part(PrimitiveType.Cylinder, t, house + new Vector3(0, floor + .45f + wall, 0), new Vector3(hr * 2 + .25f, .12f, hr * 2 + .25f), wood);   // the wall plate
            MeshPart(robeCone, t, house + new Vector3(0, floor + .5f + wall, 0), tile).transform.localScale = new Vector3(hr + .9f, 2.6f, hr + .9f);
            // The door (round-headed, plank, in a frame) and the porch: two posts, a little tiled hood, a step and a lantern.
            Part(PrimitiveType.Cube, t, house + new Vector3(0, floor + 1.45f, -hr - .02f), new Vector3(1.3f, 2.1f, .12f), dark);
            Part(PrimitiveType.Cylinder, t, house + new Vector3(0, floor + 2.5f, -hr - .03f), new Vector3(1.3f, .07f, 1.3f), dark, Quaternion.Euler(90, 0, 0));
            Part(PrimitiveType.Cube, t, house + new Vector3(0, floor + 1.4f, -hr - .08f), new Vector3(1.05f, 1.9f, .08f), wood);
            Part(PrimitiveType.Cube, t, house + new Vector3(0, floor + .5f, -hr - .8f), new Vector3(1.8f, .25f, .9f), stone);
            foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Cylinder, t, house + new Vector3(s * 1.05f, floor + 1.55f, -hr - .95f), new Vector3(.18f, 1.2f, .18f), wood);
            MeshPart(ZoneMeshes.GableRoof(2.6f, 1.6f, .6f, .1f), t, house + new Vector3(0, floor + 2.8f, -hr - .7f), tile, Quaternion.Euler(0, 90, 0));
            var lantern = house + new Vector3(.95f, floor + 2.15f, -hr - .95f);
            Part(PrimitiveType.Cube, t, lantern, new Vector3(.22f, .3f, .22f), art.glass);
            Glow(t, lantern, 7, .3f, new Color(1, .74f, .42f), 1.4f);
            foreach (int s in new[] { -1, 1 })
            {
                var w = house + new Vector3(Mathf.Sin(s * .7f) * hr, floor + 2.1f, -Mathf.Cos(s * .7f) * hr);   // round windows either side of the door
                Part(PrimitiveType.Cylinder, t, w, new Vector3(.75f, .05f, .75f), art.glass, Quaternion.LookRotation(new Vector3(w.x - house.x, 0, w.z - house.z)) * Quaternion.Euler(90, 0, 0));
            }
            Glow(t, house + new Vector3(0, floor + 2, -hr - 1.5f), 6, 0, new Color(1, .7f, .4f), .9f);   // the windows' light on the porch at night
            var solid = new GameObject("Round house").transform; solid.SetParent(t, false); solid.localPosition = house + new Vector3(0, floor + 1.8f, 0);
            solid.gameObject.AddComponent<CapsuleCollider>().radius = hr + .1f; solid.GetComponent<CapsuleCollider>().height = 4; solid.gameObject.AddComponent<NavBlocker>();
            // The platform round the trunk on struts, its rail and lanterns; the small house on it; the stair up.
            float py = 8.6f + T() * 1.2f, inner = 2.6f, outer = 5.6f; int segs = 18;
            for (int i = 0; i < segs; i++)
            {
                float a = i * Mathf.PI * 2 / segs, mid = (inner + outer) / 2;
                var at = new Vector3(Mathf.Cos(a) * mid, py, Mathf.Sin(a) * mid); var face = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                Part(PrimitiveType.Cube, t, at, new Vector3(outer - inner, .16f, mid * 2 * Mathf.PI / segs + .06f), i % 2 == 0 ? wood : dark, face);
                if (i % 3 == 0)
                {
                    var under = new Vector3(Mathf.Cos(a) * inner, py - 2.4f, Mathf.Sin(a) * inner); var top = new Vector3(Mathf.Cos(a) * (outer - .4f), py - .1f, Mathf.Sin(a) * (outer - .4f));
                    var d = top - under; Part(PrimitiveType.Cylinder, t, (under + top) / 2, new Vector3(.2f, d.magnitude / 2, .2f), dark, Quaternion.FromToRotation(Vector3.up, d));   // a strut
                }
                var post = new Vector3(Mathf.Cos(a) * (outer - .1f), py + .55f, Mathf.Sin(a) * (outer - .1f));
                Part(PrimitiveType.Cylinder, t, post, new Vector3(.12f, .55f, .12f), wood);
                float a2 = (i + 1) * Mathf.PI * 2 / segs; var post2 = new Vector3(Mathf.Cos(a2) * (outer - .1f), py + 1.05f, Mathf.Sin(a2) * (outer - .1f)); var r1 = post + Vector3.up * .5f; var dr = post2 - r1;
                Part(PrimitiveType.Cylinder, t, (r1 + post2) / 2, new Vector3(.06f, dr.magnitude / 2, .06f), wood, Quaternion.FromToRotation(Vector3.up, dr));   // the rail
                if (i % 6 == 3) { var l = post + Vector3.up * .75f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .25f; Part(PrimitiveType.Cube, t, l, new Vector3(.2f, .28f, .2f), art.glass); Glow(t, l, 6, .2f, new Color(1, .76f, .45f), 1.1f); }
            }
            float sa = T() * Mathf.PI * 2; var small = new Vector3(Mathf.Cos(sa) * (outer - 1.6f), py + .08f, Mathf.Sin(sa) * (outer - 1.6f));
            Part(PrimitiveType.Cylinder, t, small + new Vector3(0, 1.2f, 0), new Vector3(2.6f, 1.2f, 2.6f), wattle);
            MeshPart(robeCone, t, small + new Vector3(0, 2.4f, 0), tile).transform.localScale = new Vector3(2.1f, 2.2f, 2.1f);
            var sw = small + new Vector3(Mathf.Cos(sa) * 1.32f, 1.4f, Mathf.Sin(sa) * 1.32f);
            Part(PrimitiveType.Cylinder, t, sw, new Vector3(.6f, .05f, .6f), art.glass, Quaternion.LookRotation(new Vector3(Mathf.Cos(sa), 0, Mathf.Sin(sa))) * Quaternion.Euler(90, 0, 0));
            int steps = 26; float s0 = T() * Mathf.PI * 2;
            for (int i = 0; i < steps; i++)
            {
                float u = (i + .5f) / steps, a = s0 + u * Mathf.PI * 1.6f, r = 3.3f + T() * .1f;
                var at = new Vector3(Mathf.Cos(a) * r, Mathf.Lerp(.5f, py - .2f, u), Mathf.Sin(a) * r);
                Part(PrimitiveType.Cube, t, at, new Vector3(1.5f, .12f, .55f), i % 2 == 0 ? wood : dark, Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
            }
        }
        Mesh robeCone;

        /// <summary>
        /// A giant tree lying where it fell (the Fallen Ghost-Oak): its trunk along local +x, <paramref name="length"/> long and about
        /// 4.5 m thick at scale 1, tapering toward the crown end; the upturned root-plate at -x, a wall of earth and roots; the broken
        /// crown at +x, a heap of dead boughs; moss along the top, mushrooms in its lee. Solid: you walk round it (or over the
        /// root-plate's rubble). Draws from its own stream (BuildProps).
        /// </summary>
        void FallenGiant(Transform t, float length)
        {
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            var bark = Tint(art.bark, Wither(new Color(.36f, .3f, .22f))); var moss = Tint(art.bark, Wither(new Color(.22f, .32f, .14f)));
            var earth = Tint(art.soil, new Color(.22f, .17f, .11f)); var dead = Tint(art.bark, Wither(new Color(.42f, .38f, .32f)));
            float r0 = 2.2f, ph = T() * 6.3f, seed = T() * 50;
            // The trunk: a tube along x, resting on the ground (its underside in the soil), tapering to the crown end, a little bent.
            Vector3 Axis(float x) { return new Vector3(x, r0 * .72f + Mathf.Sin(x * .06f + ph) * .3f, Mathf.Sin(x * .08f + ph) * .9f); }
            float Girth(float x, float a) { float u = Mathf.Clamp01(x / length); return r0 * (1 - .45f * u) * (1 + .1f * (Mathf.PerlinNoise(Mathf.Cos(a) * 1.4f + seed, x * .18f) - .5f)) * Mathf.Clamp01((length + 1 - x) / 2.5f); }
            var rings = new List<float>(); for (float x = 0; x <= length + 1.01f; x += 2.2f) rings.Add(x);
            MeshPart(ZoneMeshes.Tube(Axis, Girth, rings, 20, Vector3.up, 6, .25f), t, Vector3.zero, bark).name = "Fallen trunk";
            // Moss along its top: a strip of flattened spheres.
            for (float x = 2; x < length - 3; x += 3.5f + T() * 2) { var c = Axis(x); Part(PrimitiveType.Sphere, t, c + new Vector3(0, Girth(x, Mathf.PI / 2) * .85f, (T() - .5f) * .8f), new Vector3(3 + T() * 2.5f, .6f, 2 + T() * 1.5f), moss); }
            // The root-plate: a disc of earth standing on edge at the butt, roots reaching out of it every way.
            var plate = Axis(0) + new Vector3(-.6f, r0 * .5f, 0);
            Part(PrimitiveType.Cylinder, t, plate, new Vector3(r0 * 4.2f, .9f, r0 * 4.2f), earth, Quaternion.Euler(0, 0, 90));
            Part(PrimitiveType.Cylinder, t, plate + new Vector3(.5f, 0, 0), new Vector3(r0 * 3.2f, .5f, r0 * 3.2f), bark, Quaternion.Euler(0, 0, 90));
            for (int i = 0; i < 14; i++)
            {
                float a = T() * Mathf.PI * 2, r = r0 * (1.2f + T() * 1.3f), len = 1.5f + T() * 3; var from = plate + new Vector3(-.3f, Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                var dir = (new Vector3(-.6f - T(), Mathf.Cos(a), Mathf.Sin(a))).normalized;
                Limb(t, from, from + dir * len, .22f + T() * .18f, .04f, bark, .05f, 7);
            }
            // The broken crown: a heap of dead boughs past the trunk's end.
            var crown = Axis(length); crown.y = .4f;
            for (int i = 0; i < 12; i++)
            {
                var from = crown + new Vector3(-2 + T() * 3, T() * 1.2f, (T() - .5f) * 4); var dir = (new Vector3(.4f + T(), (T() - .3f) * .8f, (T() - .5f) * 1.6f)).normalized;
                float len = 3 + T() * 5; var mid = from + dir * len * .5f + Vector3.up * (T() - .5f);
                Limb(t, from, mid, .25f + T() * .15f, .14f, dead, .04f, 6); Limb(t, mid, mid + (Quaternion.Euler(0, (T() - .5f) * 60, 0) * dir) * len * .5f, .14f, .03f, dead, .08f, 5);
            }
            // Mushrooms in its lee, ferns come on their own (PlantField).
            for (int i = 0; i < 3; i++) { var at = Axis(length * (.2f + i * .3f)); at.y = 0; at.z += r0 * 1.1f + T(); var m = new GameObject("Mushrooms").transform; m.SetParent(t, false); m.localPosition = at; Mushrooms(m, 1.2f, i == 1 ? 1 : 2); }
            Solid(t, new Vector3(length / 2, r0 * .75f, 0), new Vector3(length, r0 * 1.5f, r0 * 1.7f));
            Solid(t, plate, new Vector3(1.6f, r0 * 4, r0 * 4));
        }

        /// <summary>
        /// A waterfall (its foot at the root; the water falls toward -Z, the rock face rises behind at +Z): a face of mossy boulders
        /// <paramref name="height"/> high and wider than the fall, the falling water <paramref name="width"/> wide (two sheets of
        /// streaked white-blue, sliding down at different speeds), foam where it lands, and mist drifting off the pool. Pair it with
        /// a pool (a lake) in front and land (a shape) on top at the fall's height. The rock face is solid; the water is not.
        /// </summary>
        void Waterfall(Transform t, float width, float height)
        {
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            var rock = RockTint(new Color(.44f, .44f, .42f)); var mossy = RockTint(new Color(.3f, .38f, .22f));
            // The face: rows of boulders either side of the fall and behind it, the top row capped with moss.
            for (float y = -.5f; y < height + 1; y += 1.9f)
                for (float x = -width / 2 - 4.5f; x <= width / 2 + 4.5f; x += 2.1f)
                {
                    bool behind = Mathf.Abs(x) < width / 2 + .4f;
                    float z = behind ? 2.2f + T() * .5f : .6f + T() * 1.4f, size = 2 + T() * 1.3f;
                    Lump(BoulderAt((int)(T() * 6)), t, new Vector3(x + (T() - .5f) * .8f, y + T() * .5f, z), Vector3.one * size, y > height - 1.5f || T() < .3f ? mossy : rock, T() * 360);
                }
            Solid(t, new Vector3(-width / 2 - 3, height / 2, 1.6f), new Vector3(4, height + 1, 2.6f));
            Solid(t, new Vector3(width / 2 + 3, height / 2, 1.6f), new Vector3(4, height + 1, 2.6f));
            Solid(t, new Vector3(0, height / 2, 2.8f), new Vector3(width + 1, height + 1, 1.4f));
            // The water: two sheets of streaks sliding down, the front one quicker and fainter, curving out a little over the lip.
            foreach (var (z, speed, alpha) in new[] { (1.25f, .55f, .9f), (.95f, .9f, .55f) })
            {
                var sheet = new GameObject("Falling water").transform; sheet.SetParent(t, false);
                var mf = sheet.gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = FallMesh(width, height, z);
                var mr = sheet.gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = FallMaterial(alpha); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sheet.gameObject.AddComponent<FallingWater>().speed = speed;
            }
            // Foam where it lands, and mist off the pool.
            var foam = FallMaterial(.95f);
            for (int i = 0; i < 9; i++) Part(PrimitiveType.Sphere, t, new Vector3((i - 4) * width / 9 + (T() - .5f) * .6f, .05f, .6f + T() * .6f), new Vector3(1.4f + T(), .25f, 1.1f + T() * .5f), foam);
            var ps = new GameObject("Waterfall mist").AddComponent<ParticleSystem>(); ps.transform.SetParent(t, false); ps.transform.localPosition = new Vector3(0, .5f, -.2f);
            var main = ps.main; main.startLifetime = 3.5f; main.startSpeed = .7f; main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.2f); main.maxParticles = 160;
            main.startColor = new Color(.92f, .97f, 1, .22f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -.03f;
            var emission = ps.emission; emission.rateOverTime = 26 + width * 2;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(width, .4f, 1.2f);
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .25f), new GradientAlphaKey(0, 1) }); col.color = g;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = art.particle;
        }
        /// <summary>The fall's sheet: a strip from the lip (at its height, a little forward) down to the pool, rows bowed out at the top.</summary>
        static Mesh FallMesh(float width, float height, float z)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); int rows = 8;
            for (int r = 0; r <= rows; r++)
            {
                float u = r / (float)rows, y = height * (1 - u), out_ = -.9f * Mathf.Sin(u * Mathf.PI * .5f) * (1 - u) - .15f;   // over the lip, then straight down
                v.Add(new Vector3(-width / 2, y, z + out_)); v.Add(new Vector3(width / 2, y, z + out_));
                uv.Add(new Vector2(0, u * height / 3)); uv.Add(new Vector2(width / 3, u * height / 3));
                if (r < rows) { int b = r * 2; tri.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            }
            var m = new Mesh { name = "Waterfall sheet" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
        static Texture2D fallStreaks;
        /// <summary>Streaked white-blue water (unlit, alpha-blended particles shader, both faces), its streaks tiling down the sheet.</summary>
        Material FallMaterial(float alpha)
        {
            if (fallStreaks == null)
            {
                fallStreaks = new Texture2D(64, 128, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Waterfall streaks" };
                var px = new Color32[64 * 128];
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float streak = Mathf.PerlinNoise(x * .35f, y * .02f), fleck = Mathf.PerlinNoise(x * .9f + 40, y * .25f);
                        float a = Mathf.Clamp01(.42f + streak * .5f + (fleck > .7f ? .3f : 0)), b = .82f + streak * .18f;
                        px[y * 64 + x] = new Color(b * .9f, b * .97f, b, a);
                    }
                fallStreaks.SetPixels32(px); fallStreaks.Apply(true, true);
            }
            var m = new Material(art.particle) { name = "Falling water", mainTexture = fallStreaks };
            m.SetColor("_TintColor", new Color(.62f, .74f, .8f, alpha * .55f));
            return m;
        }

        /// <summary>
        /// A cluster of mushrooms within <paramref name="radius"/>: variant 0 red caps flecked white, 1 teal caps that glow (and light
        /// the ground a little) after dark, 2 pale toadstools. Each a stem and a domed cap, some leaning, a few small ones round
        /// the big. The prop's scale makes giant ones; from scale 2 their stems are solid.
        /// </summary>
        void Mushrooms(Transform t, float radius, int variant)
        {
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            var stem = Tint(art.plaster, new Color(.86f, .82f, .7f));
            var cap = variant == 1 ? Glowing(new Color(.25f, .85f, .78f), 1.1f) : variant == 2 ? Tint(art.plaster, new Color(.84f, .8f, .7f)) : Tint(art.plaster, new Color(.72f, .16f, .1f));
            var gill = Tint(art.plaster, new Color(.62f, .56f, .46f)); var fleck = Tint(art.plaster, new Color(.95f, .93f, .86f));
            int n = 4 + (int)(T() * 5);
            for (int i = 0; i < n; i++)
            {
                float a = T() * Mathf.PI * 2, r = i == 0 ? 0 : radius * (.3f + T() * .7f), size = i == 0 ? 1 : .35f + T() * .5f, lean = (T() - .5f) * 16;
                var at = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); at.y = LocalGround(t, at.x, at.z) - .02f;
                var q = Quaternion.Euler(lean, T() * 360, (T() - .5f) * 10);
                float sh = .55f * size, cr = .42f * size;
                Part(PrimitiveType.Cylinder, t, at + q * new Vector3(0, sh / 2, 0), new Vector3(.12f * size, sh / 2, .12f * size), stem, q);
                Part(PrimitiveType.Sphere, t, at + q * new Vector3(0, sh - .02f * size, 0), new Vector3(cr * 2, .16f * size, cr * 2), gill, q);
                Part(PrimitiveType.Sphere, t, at + q * new Vector3(0, sh + .05f * size, 0), new Vector3(cr * 2.05f, cr * 1.15f, cr * 2.05f), cap, q);
                if (variant == 0) for (int k = 0; k < 5; k++) { float fa = T() * Mathf.PI * 2, fr = cr * (.25f + T() * .55f); Part(PrimitiveType.Sphere, t, at + q * new Vector3(Mathf.Cos(fa) * fr, sh + .05f * size + cr * .5f * Mathf.Sqrt(1 - (fr / cr) * (fr / cr) * .8f), Mathf.Sin(fa) * fr), Vector3.one * .07f * size, fleck, q); }
            }
            if (variant == 1) Glow(t, new Vector3(0, .6f, 0), 3.5f + radius, 0, new Color(.35f, .95f, .85f), .7f);
            if (t.localScale.x >= 2) { var c = t.gameObject.AddComponent<CapsuleCollider>(); c.radius = .14f; c.height = .6f; c.center = new Vector3(0, .3f, 0); t.gameObject.AddComponent<NavBlocker>(); }
        }

        /// <summary>
        /// A rope-and-post footbridge along local Z, <paramref name="length"/> long: a deck of planks on a shallow arch, posts at the
        /// ends and every few metres either side, and rope rails sagging between them. The deck is walkable (hidden boxes under the
        /// planks in the navmesh); the rails are not solid.
        /// </summary>
        void RopeBridge(Transform t, float length)
        {
            var tr = TreeRandom(t.position); float T() { return (float)tr.NextDouble(); }
            var plank = Tint(art.timber, new Color(.4f, .29f, .18f)); var plank2 = Tint(art.timber, new Color(.34f, .24f, .15f)); var post = Tint(art.timber, new Color(.27f, .19f, .12f));
            var rope = Tint(art.hay, new Color(.58f, .5f, .34f)); float width = 2.4f, rise = .55f;
            float Deck(float z) { float a = z / length + .5f; return .25f + Mathf.Sin(a * Mathf.PI) * rise; }
            int planks = Mathf.RoundToInt(length / .42f);
            for (int i = 0; i < planks; i++)
            {
                float z = -length / 2 + (i + .5f) * length / planks, slope = Mathf.Cos((z / length + .5f) * Mathf.PI) * rise * Mathf.PI / length;
                Part(PrimitiveType.Cube, t, new Vector3((T() - .5f) * .06f, Deck(z), z), new Vector3(width + (T() - .5f) * .2f, .08f, length / planks - .05f), i % 3 == 0 ? plank2 : plank, Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, (T() - .5f) * 3, 0));
            }
            for (int i = 0; i < 8; i++)   // the walkable deck: hidden boxes in the navmesh
            {
                float a = (i + .5f) / 8, z = -length / 2 + length * a, slope = Mathf.Cos(a * Mathf.PI) * rise * Mathf.PI / length;
                var deck = Part(PrimitiveType.Cube, t, new Vector3(0, Deck(z) - .12f, z), new Vector3(width, .25f, length / 8 + .08f), plank, Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0, 0));
                deck.GetComponent<Renderer>().enabled = false; deck.AddComponent<BoxCollider>(); deck.AddComponent<NavWalkable>();
            }
            int spans = Mathf.Max(1, Mathf.RoundToInt(length / 3.2f));
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 Post(int k) { float z = -length / 2 + k * length / spans; return new Vector3(s * (width / 2 + .1f), Deck(z), z); }
                for (int k = 0; k <= spans; k++) { var p = Post(k); float tall = k == 0 || k == spans ? 1.5f : 1.2f; Part(PrimitiveType.Cylinder, t, p + new Vector3(0, tall / 2 - .35f, 0), new Vector3(.17f, tall / 2 + .35f, .17f), post); }
                for (int k = 0; k < spans; k++)
                {
                    Vector3 a = Post(k) + Vector3.up * 1.05f, b = Post(k + 1) + Vector3.up * 1.05f;
                    for (int j = 0; j < 3; j++)
                    {
                        float u0 = j / 3f, u1 = (j + 1) / 3f;
                        var p0 = Vector3.Lerp(a, b, u0) + Vector3.down * .22f * 4 * u0 * (1 - u0); var p1 = Vector3.Lerp(a, b, u1) + Vector3.down * .22f * 4 * u1 * (1 - u1); var d = p1 - p0;
                        Part(PrimitiveType.Cylinder, t, (p0 + p1) / 2, new Vector3(.05f, d.magnitude / 2, .05f), rope, Quaternion.FromToRotation(Vector3.up, d));
                    }
                }
            }
        }
    }

    /// <summary>Slides a waterfall sheet's streaks down (the texture's offset), so the water falls.</summary>
    public sealed class FallingWater : MonoBehaviour
    {
        public float speed = .6f; Material m;
        void Start() { var r = GetComponent<Renderer>(); if (r != null) m = r.material; }
        void Update() { if (m != null) m.mainTextureOffset = new Vector2(0, -Time.time * speed); }
    }
}
