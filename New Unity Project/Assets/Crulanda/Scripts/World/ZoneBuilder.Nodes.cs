using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>What the zone builder needs to know of a kind of node to build one: its name and E prompt, and its look (ore,
    /// ore_rich, windfall, herb) with a variant (the tier's colours). It comes from the trades' content (the encounter's).</summary>
    public sealed class ZoneNodeKind { public string id, name, look, prompt; public int variant; }
    /// <summary>Knows the kinds of node (the encounter's session, from the trades' content). ZoneBuilder asks the one on its own
    /// game object, or failing that any in the scene, when it builds the zone's nodes.</summary>
    public interface IZoneNodeKinds { ZoneNodeKind NodeKind(string id); }

    /// <summary>
    /// Things to gather (GAME-ONLY): ore seams, windfall timber and herbs, from the zone's <see cref="ZoneDefinition.nodes"/>.
    /// Built after the map and the secrets, each from a stream of its own keyed on where it stands, with no colliders and nothing in
    /// the navmesh, so every tree, rock, prop and creek stands where it stood and nothing is barred. Each is registered in
    /// <see cref="Interactables"/> with its node kind; what vanishes while it rests is its <see cref="ZoneInteractable.part"/>.
    /// - ore: an outcrop of the zone's crag stone (the crags' faceted lumps, sunk into the lowest ground under it), one shoulder
    ///   stained the ore's colour; the seam itself (child "full") is veins of ore lumps, crystal shards and flecks on its front
    ///   (-Z). ore_rich: the same, larger, with more ore. Mined out, the bare rock stays.
    /// - windfall: a tree snapped off by the wind: the splintered stump at the -X end stays; the trunk lying along +X on the
    ///   ground with its stub limbs (child "full") is what is cut. It turns (in 30 degree steps from its rotation) until it lies
    ///   clear of standing trunks, rocks, roads, water and buildings.
    /// - herb: a herb patch (Herb, variants 3-5 the trades' herbs).
    /// A node that stands in a trunk, a rock, a road, water, a building, a camp's spread or a cave's furnishings, by a secret or
    /// another node is moved clear (up to 3 m), with a warning when nothing near is clear. One under a cave (under) stands on its
    /// floor, its rock turned to the wall.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>Where the zone's caves have their furnishings (a fire, bedrolls, stores, rail posts and treads, a desk, the
        /// throne), each with how far round it reaches, on the ground plane (world). They have no colliders, so a node keeps clear of
        /// them by these (NodeClear). Filled by Cavern; nothing random.</summary>
        readonly List<(Vector2 at, float r)> keepClear = new List<(Vector2 at, float r)>();
        /// <summary>The cave furnishings' spots that a node keeps clear of (for the tests).</summary>
        public IReadOnlyList<(Vector2 at, float r)> KeepClearSpots { get { return keepClear; } }
        /// <summary>Marks a cave furnishing at <paramref name="at"/> (local to the cave's root) as reaching <paramref name="r"/> metres round.</summary>
        void KeepClear(Transform t, Vector3 at, float r) { var w = t.TransformPoint(at); keepClear.Add((new Vector2(w.x, w.z), r)); }
        /// <summary>How far round its root a node reaches on the ground (a seam's shoulders and loose stone, a windfall's length, a
        /// herb's leaves): it stands that far from a camp's spread and a cave's furnishings.</summary>
        public static float NodeFootprint(string look) { return look == "ore" ? 2.1f : look == "ore_rich" ? 2.1f * 1.35f : look == "windfall" ? 3 : .6f; }
        void BuildNodes()
        {
            if (Zone.nodes == null || Zone.nodes.Length == 0) return;
            var kinds = GetComponent<IZoneNodeKinds>();
            if (kinds == null) foreach (var m in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) if (m is IZoneNodeKinds k) { kinds = k; break; }
            if (kinds == null) { Debug.LogWarning(Zone.id + ": no trades content to say what its " + Zone.nodes.Length + " nodes are; none is built."); return; }
            var all = new GameObject("Zone nodes").transform; all.SetParent(transform, false);
            Physics.SyncTransforms();   // this frame's colliders, for keeping a node out of a rock or a wall
            var placed = new List<Vector3>(); foreach (var i in Interactables) if (i.node != null) placed.Add(i.position);   // herb props worked as nodes
            foreach (var n in Zone.nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.node)) continue;
                var kind = kinds.NodeKind(n.node);
                if (kind == null) { Debug.LogWarning(Zone.id + ": node '" + n.node + "' at " + n.at + " is no kind of node the trades know; left out."); continue; }
                var nr = new System.Random(Zone.seed ^ (Mathf.RoundToInt(n.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(n.at.y * 8) * 19349663) ^ 0x2f6e2b);
                float R() { return (float)nr.NextDouble(); }
                bool wood = kind.look == "windfall";
                var at = NodeClear(n.at, n.under, wood ? .9f : .5f, NodeFootprint(kind.look), placed, "'" + kind.name + "'");
                float yaw = n.under ? WallYaw(at, n.rotation) : wood ? WindfallYaw(at, n.rotation) : n.rotation;
                var t = new GameObject(kind.name).transform; t.SetParent(all, false);
                t.position = n.under ? StandAt(at, float.NegativeInfinity) : Ground(at); t.rotation = Quaternion.Euler(0, yaw, 0);
                bool under = n.under;
                // Ground under a point of the node (local x, z) relative to its root: the cave's floor under it (eased between its
                // rings, as its mesh runs on a slope), or the lower of the land and the drawn ground (so nothing hangs over a dip the
                // mesh cuts across).
                float G(float x, float z)
                {
                    var w = t.TransformPoint(new Vector3(x, 0, z)); float y;
                    if (under) { y = t.position.y; Hollow.FloorSmoothUnder(new Vector2(w.x, w.z), ref y); }
                    else y = Mathf.Min(HeightAt(w.x, w.z), MeshY(w.x, w.z));
                    return y - t.position.y;
                }
                Transform part = null;
                switch (kind.look)
                {
                    case "ore": part = OreSeam(t, kind.variant, false, R, G); break;
                    case "ore_rich": part = OreSeam(t, kind.variant, true, R, G); break;
                    case "windfall": part = Windfall(t, kind.variant, R, G); break;
                    case "herb": Herb(t, kind.variant); break;
                    default: Debug.LogWarning(Zone.id + ": node '" + n.node + "' has an unknown look '" + kind.look + "'."); DestroyImmediate(t.gameObject); continue;
                }
                placed.Add(t.position);
                Interactables.Add(new ZoneInteractable { name = kind.name, prompt = kind.prompt, item = string.IsNullOrEmpty(n.item) ? null : n.item, kind = "node", node = n.node, position = t.position, root = t, part = part });
            }
        }
        /// <summary>
        /// Where a node may stand at or near at (up to 3 m away, rings of half a metre): on a cave's floor (under) or else off the
        /// roads, out of the water and buildings and not over a cave near the surface; 2.2 m from every standing trunk; its
        /// footprint (<paramref name="foot"/>) clear of every camp's spread (a camp's mobs stand anywhere in the square of its
        /// radius, so to its corners) and of every cave furnishing (<see cref="keepClear"/>); 5.5 m from every secret (on the ground
        /// plane) and 5.2 m from every node already placed; nothing solid within r of it but the ground. At itself, with a warning,
        /// when nothing near is clear. Draws nothing random.
        /// </summary>
        Vector2 NodeClear(Vector2 at, bool under, float r, float foot, List<Vector3> placed, string what)
        {
            var ground = GroundMesh != null ? GroundMesh.GetComponent<Collider>() : null;
            bool Clear(Vector2 q)
            {
                float floor = 0;
                if (under ? !Hollow.FloorUnder(q, ref floor) : NearRoad(q, 1) || Water.NearWater(q, 1) || InBuilding(q) || Hollow.CoverAt(q, 1) > 0) return false;
                if (!under && !TrunkClear(q, 2.2f)) return false;
                if (Zone.camps != null) foreach (var cp in Zone.camps) if (cp != null && Vector2.Distance(cp.center, q) < cp.radius * 1.42f + foot) return false;
                foreach (var k in keepClear) if (Vector2.Distance(k.at, q) < k.r + foot) return false;
                var c = under ? StandAt(q, float.NegativeInfinity) : Ground(q);
                foreach (var s in Secrets) if (s != null && Vector2.Distance(new Vector2(s.position.x, s.position.z), q) < 5.5f) return false;   // on the ground plane: the secrets' tests measure so
                foreach (var p in placed) if (Vector3.Distance(p, c) < 5.2f) return false;
                foreach (var col in Physics.OverlapSphere(c + Vector3.up * (r + .5f), r, ~0, QueryTriggerInteraction.Ignore)) if (col != ground) return false;
                return true;
            }
            if (Clear(at)) return at;
            for (float d = .5f; d <= 3.01f; d += .5f)
                for (int k = 0; k < 8; k++) { var q = at + new Vector2(Mathf.Cos(k * Mathf.PI / 4), Mathf.Sin(k * Mathf.PI / 4)) * d; if (Clear(q)) return q; }
            Debug.LogWarning(Zone.id + ": node " + what + " at " + at + " has no clear ground within 3 m; it stands where it was put.");
            return at;
        }
        /// <summary>A node on a cave floor turns its rock (+Z) to the nearer wall, so its face looks into the passage; one in the
        /// middle of the passage keeps its rotation.</summary>
        static float WallYaw(Vector2 at, float yaw)
        {
            foreach (var h in Hollow.All)
            {
                if (!h.FloorAt(at, out _)) continue;
                int i = h.Nearest(at, out float off); if (off < .3f) return yaw;
                var away = at - new Vector2(h.Centre[i].x, h.Centre[i].z);
                return Mathf.Atan2(away.x, away.y) * Mathf.Rad2Deg;
            }
            return yaw;
        }
        /// <summary>
        /// Which way a windfall lies: its rotation, or the nearest turn of it in 30 degree steps that keeps the whole of it (the
        /// stump at -2.9 m to the trunk's top at +2.5 m along its X) off the roads, out of the water, buildings and caves, clear
        /// of anything solid, and 1.3 m from every standing trunk; failing that, the turn farthest from the trunks.
        /// </summary>
        float WindfallYaw(Vector2 at, float yaw)
        {
            var ground = GroundMesh != null ? GroundMesh.GetComponent<Collider>() : null;
            float best = yaw, bestGap = -1;
            for (int k = 0; k < 12; k++)
            {
                float y = yaw + (k % 2 == 0 ? 1 : -1) * ((k + 1) / 2) * 30, rad = y * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));   // its local +X on the ground
                Vector2 a = at - dir * 2.9f, b = at + dir * 2.5f;
                bool ok = true;
                foreach (var e in new[] { a, (a + b) / 2, b }) if (NearRoad(e, .5f) || Water.NearWater(e, .5f) || InBuilding(e) || Hollow.CoverAt(e, .5f) > 0) ok = false;
                if (ok) foreach (var col in Physics.OverlapCapsule(Ground(a) + Vector3.up * .75f, Ground(b) + Vector3.up * .75f, .35f, ~0, QueryTriggerInteraction.Ignore)) if (col != ground) { ok = false; break; }
                if (!ok) continue;
                float gap = float.MaxValue; foreach (var tr in trunks) gap = Mathf.Min(gap, Segment(tr, a, b));
                if (gap >= 1.3f) return y;
                if (gap > bestGap) { bestGap = gap; best = y; }
            }
            return best;
        }

        // ---------- ore seams ----------
        /// <summary>Each tier's ore: the ore's own colour, the stain it leaves in the rock, and its flecks (glowing when glow > 0).</summary>
        static readonly (Color ore, Color stain, Color fleck, float glow)[] OreLooks = {
            (new Color(.52f, .28f, .15f), new Color(.5f, .37f, .29f), new Color(.3f, .55f, .46f), 0),     // copper: deep red-brown, a verdigris bloom
            (new Color(.46f, .32f, .2f), new Color(.45f, .35f, .27f), new Color(.66f, .36f, .16f), 0),    // bog-iron: brown, rust
            (new Color(.27f, .27f, .3f), new Color(.33f, .32f, .32f), new Color(.78f, .78f, .82f), 0),    // Adit iron: dark iron, a bright glint
            (new Color(.8f, .4f, .14f), new Color(.31f, .24f, .21f), new Color(1, .5f, .16f), .7f),        // cinder: ember orange, glowing (a colour of its own: Glowing shares one material per colour, and the flames' (1, .55, .2) burn at 2.2)
            (new Color(.2f, .58f, .54f), new Color(.29f, .42f, .4f), new Color(.45f, .95f, .85f), .5f) };  // Veridian: blue-green, glowing
        Material oreBase;   // matte ore: the metal drawn non-metallic, so it keeps its colour in a cave (no reflections there) and at night
        /// <summary>
        /// An ore seam: an outcrop of the crags' stone about a metre high, leaning back, a shoulder each side (the right one stained
        /// the ore's colour), sunk into the lowest ground under it; on its front (-Z) the seam (child "full", returned): veins of ore
        /// lumps set half into the rock's face (found from the lumps' own vertices, so none floats off a hollow in it), crystal shards
        /// standing out of it, flecks, and broken ore at the foot. Rich: a third larger, with more ore. Loose stone lies round the
        /// foot. No colliders. Draws only from R.
        /// </summary>
        Transform OreSeam(Transform t, int variant, bool rich, Func<float> R, Func<float, float, float> G)
        {
            float s = rich ? 1.35f : 1;
            var stone = RockTint(Zone.biome == "ash" ? new Color(.37f, .365f, .37f) : Zone.biome == "mountain" ? new Color(.35f, .335f, .31f) : new Color(.4f, .38f, .35f));   // the crags' stone
            var look = OreLooks[Mathf.Clamp(variant, 0, OreLooks.Length - 1)];
            if (oreBase == null) { oreBase = new Material(art.metal) { name = "Ore" }; oreBase.SetFloat("_Metallic", 0); oreBase.SetFloat("_Glossiness", .35f); }   // Tint keys on the name: kept apart from the metal's tints
            var stain = RockTint(look.stain); var ore = Tint(oreBase, look.ore); var fleck = look.glow > 0 ? Glowing(look.fleck, look.glow) : Tint(oreBase, look.fleck);
            // A crag lump w x h x d with its flat base a quarter of its height under the lowest ground beneath it, leaning back. Its
            // vertices (in the seam's frame) are kept, so the ore can be set into the rock's real face.
            var rock = new List<Vector3>();
            void Rock(float x, float z, float w, float h, float d, Material m, float lean, float yaw)
            {
                float low = Mathf.Min(G(x, z), Mathf.Min(G(x - w * .4f, z - d * .4f), Mathf.Min(G(x + w * .4f, z - d * .4f), G(x, z + d * .4f))));
                var lump = MeshPart(CragRock((int)(R() * 6)), t, new Vector3(x, low - h * .25f + .2f * h, z), m, Quaternion.Euler(lean, yaw, (R() - .5f) * 8)).transform;
                lump.localScale = new Vector3(w, h, d);
                var to = Matrix4x4.TRS(lump.localPosition, lump.localRotation, lump.localScale);
                foreach (var v in lump.GetComponent<MeshFilter>().sharedMesh.vertices) rock.Add(to.MultiplyPoint3x4(v));
            }
            Rock(0, .5f * s, 2.1f * s, 2f * s, 1.6f * s, stone, 5 + R() * 6, (R() - .5f) * 20);
            Rock(-1.05f * s, .7f * s, 1.25f * s, 1.3f * s, 1.15f * s, stone, 4 + R() * 8, 20 + R() * 30);
            Rock(1.05f * s, .55f * s, 1.1f * s, 1.05f * s, 1.05f * s, stain, R() * 10, -20 - R() * 30);
            // Where the rock's face is at (x, y): the foremost (-Z) of its vertices near there; null where no rock is.
            float? Face(float x, float y)
            {
                float best = float.MaxValue;
                foreach (var v in rock) if (Mathf.Abs(v.x - x) < .2f * s && Mathf.Abs(v.y - y) < .16f * s && v.z < best) best = v.z;
                return best < float.MaxValue ? best : (float?)null;
            }
            for (int k = 0; k < 4; k++)
            {
                // Loose stone at the foot, round the sides and front: no colliders.
                float a = (k * 70 + 120 + R() * 40) * Mathf.Deg2Rad, r = (1.3f + R() * .5f) * s, x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r * .8f, size = (.22f + R() * .2f) * s;
                Lump(BoulderAt((int)(R() * 6)), t, new Vector3(x, G(x, z) + .18f * size * .8f - .06f, z), new Vector3(size * 1.2f, size * .8f, size), k % 2 == 0 ? stone : stain, R() * 360);
            }
            var full = new GameObject("full").transform; full.SetParent(t, false);
            // Veins of ore set half into the face across the big lump, flecks on them.
            int veins = rich ? 8 : 5;
            for (int k = 0; k < veins; k++)
            {
                float x = ((k + .5f) / veins - .5f) * 1.5f * s + (R() - .5f) * .2f, y = G(x, 0) + (.2f + R() * .45f) * s, w = (.3f + R() * .25f) * s;
                var at = Face(x, y); if (at == null) continue;
                // Faceted lumps (a crag's shape, not a boulder's: rounded ones read as potatoes), and on most a fleck: flat on the face, a
                // bloom of verdigris or rust or a glint (as round beads they read as peas). The draws are the ones they always took.
                Lump(CragRock((int)(R() * 6)), full, new Vector3(x, y, at.Value + .03f), new Vector3(w, w * (.6f + R() * .3f), w * .75f), ore, R() * 360);
                if (R() < .7f) { float fx = (R() - .5f) * w * .6f, fy = (R() - .3f) * w * .4f, fs = (.05f + R() * .05f) * s; Part(PrimitiveType.Sphere, full, new Vector3(x + fx, y + fy, at.Value - w * .3f), new Vector3(fs * 2.6f, fs * 1.8f, fs * .35f), fleck, Quaternion.Euler(0, 0, (fx + fy) * 900)); }
            }
            int shards = rich ? 7 : 4;
            for (int k = 0; k < shards; k++)
            {
                // Crystal shards standing out of the face, their feet in it, tipped toward you (-Z) and up.
                float x = (R() - .5f) * 1.3f * s, y = G(x, 0) + (.25f + R() * .4f) * s, h = (.16f + R() * .18f) * s;
                var at = Face(x, y); if (at == null) continue;
                Part(PrimitiveType.Cube, full, new Vector3(x, y + h * .25f, at.Value - h * .2f), new Vector3(.07f * s, h, .07f * s), ore, Quaternion.Euler(-(25 + R() * 30), (R() - .5f) * 50, (R() - .5f) * 40));
            }
            for (int k = 0; k < (rich ? 4 : 2); k++)
            {
                // Broken ore at the foot, ready for the sack.
                float x = (R() - .5f) * 1.3f * s, size = (.14f + R() * .12f) * s, z = (Face(x, G(x, 0) + .12f * s) ?? -.4f * s) - .2f - R() * .35f;
                Lump(BoulderAt((int)(R() * 6)), full, new Vector3(x, G(x, z) + .18f * size * .7f - .02f, z), new Vector3(size * 1.2f, size * .7f, size), ore, R() * 360);
            }            return full;
        }

        // ---------- windfall timber ----------
        /// <summary>Each tier's wood: bark and the pale wood of the break (oak, black pine, stone-pine, the ash's charred snag, ghost-oak).</summary>
        static readonly (Color bark, Color wood)[] WoodLooks = {
            (new Color(.34f, .29f, .24f), new Color(.62f, .5f, .34f)),
            (new Color(.2f, .18f, .17f), new Color(.6f, .5f, .38f)),
            (new Color(.42f, .38f, .33f), new Color(.7f, .6f, .45f)),
            (new Color(.15f, .13f, .12f), new Color(.3f, .25f, .2f)),
            (new Color(.72f, .72f, .68f), new Color(.8f, .78f, .7f)) };
        /// <summary>
        /// A windfall: a tree the wind snapped off. At its -X end the stump (a short bole on a root flare, its top a splintered
        /// break of pale wood) stays; from just past it the trunk (child "full", returned) lies along +X on the ground for 4.4-5 m,
        /// tapering, its broken end splintered, with four stub limbs; it follows the ground and never hangs over a dip. No
        /// colliders. Draws only from R.
        /// </summary>
        Transform Windfall(Transform t, int variant, Func<float> R, Func<float, float, float> G)
        {
            var look = WoodLooks[Mathf.Clamp(variant, 0, WoodLooks.Length - 1)];
            var bark = Tint(art.bark, Wither(look.bark)); var heart = Tint(art.timber, Wither(look.wood));
            float r0 = .27f + R() * .05f, len = 4.4f + R() * .6f, sx = -2.7f;
            // The stump: its top is the break, splinters standing round the rim; the root flare sunk into the ground round its foot.
            float gs = Mathf.Min(G(sx, 0), Mathf.Min(G(sx - r0, 0), G(sx + r0, 0))), sh = .45f + R() * .2f;
            Part(PrimitiveType.Cylinder, t, new Vector3(sx, gs + sh / 2 - .1f, 0), new Vector3(r0 * 2.3f, sh / 2 + .1f, r0 * 2.3f), bark);
            Part(PrimitiveType.Cylinder, t, new Vector3(sx, gs + sh - .08f, 0), new Vector3(r0 * 2.1f, .09f, r0 * 2.1f), heart);
            for (int k = 0; k < 6; k++)
            {
                var q = Quaternion.Euler(0, k * 60 + R() * 25, 0); var p = new Vector3(sx, 0, 0) + q * new Vector3(0, 0, r0 * .8f);
                Part(PrimitiveType.Cube, t, new Vector3(p.x, gs + sh + .02f + R() * .08f, p.z), new Vector3(.05f + R() * .04f, .14f + R() * .22f, .04f), k % 2 == 0 ? heart : bark, q * Quaternion.Euler(-8 - R() * 16, 0, (R() - .5f) * 24));
            }
            for (int k = 0; k < 4; k++)
            {
                var q = Quaternion.Euler(0, k * 90 + 45 + R() * 20, 0); var p = new Vector3(sx, 0, 0) + q * new Vector3(0, 0, r0 * 1.15f);
                Part(PrimitiveType.Sphere, t, new Vector3(p.x, G(p.x, p.z) + .03f, p.z), new Vector3(.2f, .15f, .46f) * (r0 / .27f), bark, q * Quaternion.Euler(14, 0, 0));
            }
            // The trunk, lying on the ground from the break to its top (narrower there), lowered until no stretch of it floats.
            var full = new GameObject("full").transform; full.SetParent(t, false);
            float ax = sx + .4f, bx = ax + len, r1 = r0 * .58f;
            var a = new Vector3(ax, G(ax, 0) + r0 * .7f, 0); var b = new Vector3(bx, G(bx, 0) + r1 * .55f, 0);
            float lift = 0;
            for (int k = 1; k < 8; k++) { float f = k / 8f; var c = Vector3.Lerp(a, b, f); lift = Mathf.Max(lift, c.y - Mathf.Lerp(r0, r1, f) * .75f - G(c.x, 0)); }
            if (lift > 0) { a.y -= lift; b.y -= lift; }
            var dir = (b - a).normalized;
            Limb(full, a, b, r0, r1, bark, .02f, 10);
            // The break: the trunk's flared, open end closed by pale torn wood, splinters reaching back toward the stump.
            Part(PrimitiveType.Cylinder, full, a - dir * .01f, new Vector3(r0 * 2.6f, .02f, r0 * 2.6f), heart, Quaternion.FromToRotation(Vector3.up, dir));
            for (int k = 0; k < 5; k++)
            {
                var round = Quaternion.AngleAxis(k * 72 + R() * 30, dir) * Vector3.Cross(dir, Vector3.forward).normalized;
                var p = a + round * r0 * (.3f + R() * .7f) - dir * .1f;
                Part(PrimitiveType.Cube, full, p, new Vector3(.04f + R() * .03f, .04f, .18f + R() * .2f), k % 2 == 0 ? heart : bark, Quaternion.LookRotation(-dir + round * (R() - .5f) * .3f));
            }
            // Stub limbs: two up, two out to the sides, snapped short.
            for (int k = 0; k < 4; k++)
            {
                float f = .3f + k * .17f + R() * .08f; var from = Vector3.Lerp(a, b, f);
                var side = new Vector3(0, 0, k % 2 == 0 ? 1 : -1); var up = k < 2 ? .9f + R() * .5f : .2f + R() * .2f;
                var to = from + (dir * (.3f + R() * .3f) + side * (k < 2 ? .35f : 1) + Vector3.up * up).normalized * (.55f + R() * .55f);
                to.y = Mathf.Max(to.y, G(to.x, to.z) + .08f);
                Limb(full, from, to, r0 * (.36f - f * .12f), .025f, bark, .1f, 6);
            }
            return full;
        }

        // ---------- the trades' herbs ----------
        /// <summary>
        /// The trades' herbs, as Herb variants (fixed tables, nothing random): 3 tarnwort (a rosette of broad blue-green leaves under
        /// tall stems of pale blue heads), 4 cinder-thistle (spiny grey leaves and grey globes tufted ember orange), 5 dewfern (seven
        /// fronds arching out and down, beaded with dew that catches the light).
        /// </summary>
        void HerbOfTheTrades(Transform t, int variant)
        {
            if (variant == 5)
            {
                var frond = Tint(art.foliage, Wither(new Color(.28f, .58f, .3f))); var dark = Tint(art.foliage, Wither(new Color(.2f, .42f, .22f))); var dew = Glowing(new Color(.85f, .95f, 1), .25f);
                for (int i = 0; i < 7; i++)
                {
                    var q = Quaternion.Euler(0, i * 51 + 10, 0); var p = new Vector3(0, .03f, 0); float pitch = 62 - (i % 3) * 6;
                    for (int k = 0; k < 4; k++)
                    {
                        // Each piece a little flatter than the last: up from the crown, then out and over.
                        var r = q * Quaternion.Euler(pitch - k * 26, 0, 0); var along = r * Vector3.forward; float l = .17f - k * .02f;
                        Part(PrimitiveType.Cube, t, p + along * l / 2, new Vector3(.11f - k * .018f, .012f, l), k % 2 == 0 ? frond : dark, r);
                        p += along * l;
                    }
                    if (i % 2 == 0) Part(PrimitiveType.Sphere, t, p + Vector3.up * .01f, Vector3.one * .025f, dew);
                }
                return;
            }
            bool thistle = variant == 4;
            var stem = Tint(art.foliage, thistle ? new Color(.42f, .45f, .38f) : new Color(.2f, .36f, .32f));
            var head = Tint(art.foliage, thistle ? new Color(.56f, .52f, .47f) : new Color(.62f, .78f, .95f));
            var tuft = Tint(art.foliage, thistle ? new Color(.88f, .48f, .22f) : new Color(.78f, .88f, 1));
            for (int i = 0; i < 6; i++)
            {
                // Leaves round the foot: broad and flat for tarnwort, narrow spines for thistle.
                var q = Quaternion.Euler(0, i * 60 + 15, 0);
                Part(PrimitiveType.Sphere, t, q * new Vector3(0, .03f, .13f), thistle ? new Vector3(.05f, .03f, .3f) : new Vector3(.12f, .025f, .28f), stem, q * Quaternion.Euler(thistle ? -18 : -6, 0, 0));
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72 + 20, r = .06f + (i % 2) * .1f, h = (thistle ? .38f : .48f) + (i % 3) * .07f;
                var at = Quaternion.Euler(0, a, 0) * new Vector3(r, 0, 0);
                Part(PrimitiveType.Cube, t, at + new Vector3(0, h / 2, 0), new Vector3(.025f, h, .025f), stem, Quaternion.Euler(6, a, 5));
                if (thistle)
                {
                    Part(PrimitiveType.Sphere, t, at + new Vector3(0, h + .03f, 0), new Vector3(.1f, .09f, .1f), head);
                    Part(PrimitiveType.Sphere, t, at + new Vector3(0, h + .08f, 0), new Vector3(.07f, .05f, .07f), tuft);
                }
                else
                {
                    for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, t, at + new Vector3((k - 1) * .035f, h + .02f + (k % 2) * .03f, (k % 2) * .02f), new Vector3(.06f, .05f, .06f), k == 1 ? tuft : head);
                }
            }
        }
    }
}
