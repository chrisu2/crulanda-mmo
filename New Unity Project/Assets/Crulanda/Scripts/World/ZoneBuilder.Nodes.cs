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
    /// - herb: a herb patch (Herb, variants 3-5 the trades' herbs), leaning with a slope and sunk into it so nothing hangs over it.
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
                    case "herb": Herb(t, kind.variant, (x, z) => { if (!under) return LandY(x, z); float y = t.position.y; Hollow.FloorSmoothUnder(new Vector2(x, z), ref y); return y; }); break;   // laid on the ground under it (a slope, a cave's floor)
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
                // Silvery blue-green, not the meadow ferns' yellow-green, so a dewfern reads as one among them; the dew lit a little.
                var frond = Tint(art.foliage, Wither(new Color(.36f, .62f, .54f))); var dark = Tint(art.foliage, Wither(new Color(.24f, .46f, .4f))); var dew = Glowing(new Color(.8f, .95f, 1), .6f);
                for (int i = 0; i < 7; i++)
                {
                    var q = Quaternion.Euler(0, i * 51 + 10, 0); var p = new Vector3(0, .03f, 0); float pitch = 70 - (i % 3) * 6;
                    for (int k = 0; k < 4; k++)
                    {
                        // Each piece a little flatter than the last: up from the crown, then out and over (a pitch about X turns +Z down,
                        // so it is negated: drawn as written before, most of every frond ran into the ground and only its tip showed).
                        var r = q * Quaternion.Euler(-(pitch - k * 26), 0, 0); var along = r * Vector3.forward; float l = .26f - k * .03f;
                        Part(PrimitiveType.Cube, t, p + along * l / 2, new Vector3(.13f - k * .02f, .012f, l), k % 2 == 0 ? frond : dark, r);
                        p += along * l;
                        if (k == 1 && i % 2 == 1) Part(PrimitiveType.Sphere, t, p + Vector3.up * .012f, Vector3.one * .03f, dew);
                    }
                    Part(PrimitiveType.Sphere, t, p + Vector3.up * .012f, Vector3.one * .036f, dew);   // a bead at every frond's tip
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

        // ---------- the trades' stations (DESIGN 6.1, ADDENDUM B.5) ----------
        /// <summary>The kinds of station a recipe is made at.</summary>
        public static readonly string[] StationKinds = { "forge", "bench", "fire" };
        /// <summary>
        /// Where the trades' recipes are made, each with its kind, its name and where the thing worked stands: a smithy (forge), a
        /// bake oven and an inn's kitchen (fire), the herbalist's drying hut (bench), an inn's hearth (fire, named after the inn), and
        /// the zone's own stations (<see cref="ZoneDefinition.stations"/>). Never a workplace for villagers (VillageLife's places are
        /// the Workplaces, untouched).
        /// </summary>
        public readonly List<ZoneStationSpot> Stations = new List<ZoneStationSpot>();
        /// <summary>The player's station at a villager's workplace of this kind (forge: forge; oven, kitchen: fire; dryhut: bench), or
        /// null for a workplace that is none (a stall, the tannery, the woodpile, a kitchen's hand-over spots, the bar).</summary>
        public static string StationKind(string workplace)
        {
            switch (workplace) { case "forge": return "forge"; case "oven": case "kitchen": return "fire"; case "dryhut": return "bench"; default: return null; }
        }
        /// <summary>Registers a station: one of each kind for each prop (a smithy's second workplace adds none). Draws nothing random.</summary>
        void AddStation(string kind, string name, Vector3 at, Transform root)
        {
            if (kind == null || Stations.Exists(s => s.root == root && s.kind == kind)) return;
            Stations.Add(new ZoneStationSpot { kind = kind, name = name, position = at, root = root });
        }
        /// <summary>Walls in a prop's stations: they are worked only from this floor, in the prop's local x/z (ZoneStationSpot.room). Draws nothing random.</summary>
        void StationRoom(Transform root, Rect room) { foreach (var s in Stations) if (s.root == root) s.room = room; }
        /// <summary>
        /// The zone's own stations (GAME-ONLY): a field anvil (forge), a herbalist's bench (bench) or a cookfire (fire) where the data
        /// puts it, each from a stream of its own keyed on where it stands, under "Zone stations", with no colliders and nothing in
        /// the navmesh, so every tree, rock, prop, node and creek stands where it stood. Built after the nodes; one of an unknown kind is
        /// left out with a warning.
        /// </summary>
        void BuildStations()
        {
            if (Zone.stations == null || Zone.stations.Length == 0) return;
            var all = new GameObject("Zone stations").transform; all.SetParent(transform, false);
            foreach (var s in Zone.stations)
            {
                if (s == null) continue;
                if (Array.IndexOf(StationKinds, s.kind) < 0) { Debug.LogWarning(Zone.id + ": station '" + s.name + "' at " + s.at + " is of no kind the trades know ('" + s.kind + "'); left out."); continue; }
                var sr = new System.Random(Zone.seed ^ (Mathf.RoundToInt(s.at.x * 8) * 73856093) ^ (Mathf.RoundToInt(s.at.y * 8) * 19349663) ^ 0x51a7e5);
                float R() { return (float)sr.NextDouble(); }
                var t = new GameObject(string.IsNullOrEmpty(s.name) ? s.kind : s.name).transform; t.SetParent(all, false);
                t.position = Ground(s.at); t.rotation = Quaternion.Euler(0, s.rotation, 0);
                switch (s.kind)
                {
                    case "forge": FieldAnvil(t, s.variant, R); break;
                    case "bench": HerbBench(t, R); break;
                    default: Cookfire(t, R); break;
                }
                AddStation(s.kind, t.name, t.position, t);
            }
        }
        /// <summary>The lowest ground under a round footprint of radius r at local (x, z), relative to the root.</summary>
        float FootUnder(Transform t, float x, float z, float r)
        {
            float low = LocalGround(t, x, z);
            for (int k = 0; k < 6; k++) low = Mathf.Min(low, LocalGround(t, x + Mathf.Cos(k * 1.047f) * r, z + Mathf.Sin(k * 1.047f) * r));
            return low;
        }
        /// <summary>
        /// A field anvil (GAME-ONLY): the smith's iron anvil (foot, waist, face, horn), a hammer on its face, its top at .55 over the
        /// ground, on a block sunk into the ground under it: variant 0 an oak stump, 1 a block of pale stone bound with two bands of
        /// bone and two tusks leaning on it (the Ash-Walkers'), 2 a mossed stone (the Keepers'). Beside it the fire: variants 0 and 1 a
        /// pan of coals on three iron legs, every third coal glowing; variant 2 a stone basin of embers on the ground. Tongs lean on
        /// the block; a water cask and an open sack of charcoal stand by. No colliders. Draws only from R.
        /// </summary>
        void FieldAnvil(Transform t, int variant, Func<float> R)
        {
            var iron = Tint(art.metal, new Color(.22f, .22f, .24f)); var dark = Tint(art.timber, new Color(.25f, .17f, .11f));
            var coal = Tint(art.stone, new Color(.11f, .1f, .1f)); var ember = Glowing(new Color(1, .42f, .12f), 2.4f);
            float foot = FootUnder(t, 0, 0, .36f), top = Mathf.Max(0, foot) + .55f;
            switch (variant)
            {
                case 1:
                {
                    var pale = Tint(art.stone, new Color(.7f, .68f, .62f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, (foot - .12f + top) / 2, 0), new Vector3(.64f, top - foot + .12f, .52f), pale, Quaternion.Euler(0, (R() - .5f) * 8, 0));
                    foreach (float y in new[] { foot + .16f, top - .12f }) Part(PrimitiveType.Cube, t, new Vector3(0, y, 0), new Vector3(.68f, .07f, .56f), Bone);   // the bands
                    foreach (int s in new[] { -1, 1 }) Rod(t, new Vector3(s * .5f, LocalGround(t, s * .5f, .3f), .3f), new Vector3(s * .34f, top + .35f + R() * .1f, .18f), .07f, Bone);   // the tusks, their points over the block
                    break;
                }
                case 2:
                {
                    var stone = RockTint(Wither(new Color(.38f, .4f, .36f)));
                    Part(PrimitiveType.Sphere, t, new Vector3(0, top - .4f, 0), new Vector3(.95f, .8f, .82f), stone, Quaternion.Euler(0, R() * 360, 0));
                    Part(PrimitiveType.Sphere, t, new Vector3(-.12f, top - .08f, .14f), new Vector3(.62f, .16f, .5f), Tint(art.foliage, new Color(.26f, .46f, .2f)));   // moss on its crown, round the anvil's foot
                    break;
                }
                default:
                    MeshPart2(PropMesh("Anvil stump", () => TwoTone(Turned(new[] { new Vector2(.36f, 0), new Vector2(.32f, .15f), new Vector2(.3f, .55f) }, 10), Turned(new[] { new Vector2(.3f, .55f), new Vector2(0, .55f) }, 10))), t, new Vector3(0, top - .55f, 0), art.bark, Tint(art.timber, new Color(.62f, .5f, .34f)));
                    if (top - .55f > foot + .02f) Part(PrimitiveType.Cylinder, t, new Vector3(0, (foot - .1f + top - .55f) / 2, 0), new Vector3(.72f, (top - .55f - foot + .1f) / 2, .72f), art.bark);   // the stump's foot down to the ground on a slope
                    break;
            }
            // The anvil, as at a smithy, a hammer on its face.
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .05f, 0), new Vector3(.34f, .1f, .5f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .16f, 0), new Vector3(.18f, .12f, .34f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, top + .28f, -.04f), new Vector3(.3f, .12f, .6f), iron);
            MeshPart(PropMesh("Anvil horn", () => ZoneMeshes.Cone(.07f, .3f, 8)), t, new Vector3(0, top + .27f, .26f), iron, Quaternion.Euler(90, 0, 0));
            Rod(t, new Vector3(-.12f, top + .36f, -.22f), new Vector3(.1f, top + .36f, .08f), .03f, art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(-.14f, top + .375f, -.25f), new Vector3(.07f, .07f, .15f), iron, Quaternion.Euler(0, 36, 0));
            foreach (float x in new[] { -.36f, -.32f }) Bar(t, new Vector3(x, LocalGround(t, x, -.36f) + .02f, -.36f), new Vector3(x + .06f, top - .05f, -.22f), .03f, .02f, iron);   // tongs against the block
            // The fire beside it.
            float px = .95f, pz = .15f, pg = FootUnder(t, px, pz, .36f);
            if (variant == 2)
            {
                var stone = RockTint(Wither(new Color(.34f, .33f, .31f)));
                Part(PrimitiveType.Cylinder, t, new Vector3(px, pg + .08f, pz), new Vector3(.82f, .12f, .82f), stone);
                Part(PrimitiveType.Cylinder, t, new Vector3(px, pg + .2f, pz), new Vector3(.62f, .01f, .62f), Tint(art.stone, new Color(.16f, .14f, .13f)));
            }
            else
            {
                float panY = pg + .5f;
                for (int k = 0; k < 3; k++) { float a = k * 2.094f + .5f; Rod(t, new Vector3(px + Mathf.Cos(a) * .32f, LocalGround(t, px + Mathf.Cos(a) * .32f, pz + Mathf.Sin(a) * .32f), pz + Mathf.Sin(a) * .32f), new Vector3(px + Mathf.Cos(a) * .24f, panY, pz + Mathf.Sin(a) * .24f), .04f, iron); }
                Part(PrimitiveType.Cylinder, t, new Vector3(px, panY, pz), new Vector3(.72f, .04f, .72f), iron);
                Part(PrimitiveType.Cylinder, t, new Vector3(px, panY + .045f, pz), new Vector3(.76f, .012f, .76f), iron);   // its rim
            }
            float bed = variant == 2 ? pg + .2f : pg + .54f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * 2.39996f, r = .055f * Mathf.Sqrt(i + 1), size = .11f + (i % 3) * .025f;
                Lump(BoulderAt(i % 6), t, new Vector3(px + Mathf.Cos(a) * r, bed + .03f + (.2f - r) * .2f, pz + Mathf.Sin(a) * r), new Vector3(size, size * .6f, size), i % 3 == 0 ? ember : coal, i * 47 + R() * 30);
            }
            Glow(t, new Vector3(px, bed + .4f, pz), 4.5f, 1, new Color(1, .5f, .2f), 1.8f);
            // A water cask to quench in, and an open sack of charcoal.
            Barrel(t, new Vector3(-.85f, LocalGround(t, -.85f, .35f), .35f), .55f, R() * 360);
            Part(PrimitiveType.Cylinder, t, new Vector3(-.85f, LocalGround(t, -.85f, .35f) + .5f, .35f), new Vector3(.3f, .005f, .3f), art.water);
            var sacking = Tint(art.cloth, new Color(.5f, .44f, .34f)); float sg = LocalGround(t, -.75f, .95f);
            Part(PrimitiveType.Sphere, t, new Vector3(-.75f, sg + .2f, .95f), new Vector3(.46f, .44f, .42f), sacking);
            Part(PrimitiveType.Cylinder, t, new Vector3(-.75f, sg + .4f, .95f), new Vector3(.3f, .04f, .28f), sacking);   // the rolled-down mouth
            for (int i = 0; i < 4; i++) Lump(BoulderAt(i + 2), t, new Vector3(-.75f + (i % 2 - .5f) * .1f, sg + .42f + (i / 2) * .04f, .95f + (i / 2 - .5f) * .08f), new Vector3(.1f, .06f, .1f), coal, i * 61);
        }
        /// <summary>
        /// A herbalist's bench (GAME-ONLY): a plank bench with a shelf under it, on it a mortar and pestle, three jars, a row of
        /// stoppered vials and a cut bunch laid ready (on the Ash Rim two blocks of salt as well); behind it a rail on two posts hung
        /// with drying bunches (bone posts on the Rim); a basket of cut herbs at its end. No colliders. Draws only from R.
        /// </summary>
        void HerbBench(Transform t, Func<float> R)
        {
            bool ash = Zone.biome == "ash";
            var dark = Tint(art.timber, new Color(.3f, .21f, .14f)); var post = ash ? Bone : Tint(art.timber, new Color(.4f, .3f, .2f));
            const float top = .82f;
            Part(PrimitiveType.Cube, t, new Vector3(0, top, 0), new Vector3(1.6f, .07f, .55f), art.timber);
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                float x = sx * .7f, z = sz * .2f, g = LocalGround(t, x, z) - .05f;
                Part(PrimitiveType.Cube, t, new Vector3(x, (g + top) / 2, z), new Vector3(.08f, top - g, .08f), dark);
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, .3f, 0), new Vector3(1.44f, .04f, .44f), dark);   // the shelf
            float on = top + .035f;
            MeshPart(PropMesh("Mortar", () => Turned(new[] { new Vector2(0, 0), new Vector2(.08f, 0), new Vector2(.13f, .12f), new Vector2(.13f, .15f), new Vector2(.1f, .15f), new Vector2(.06f, .05f), new Vector2(0, .05f) }, 10)), t, new Vector3(-.55f, on, .02f), Tint(art.stone, new Color(.62f, .6f, .56f)));
            Rod(t, new Vector3(-.55f, on + .07f, .02f), new Vector3(-.47f, on + .22f, .1f), .035f, Tint(art.stone, new Color(.7f, .68f, .62f)));
            Jar(t, new Vector3(-.2f, on, .12f), new Color(.62f, .4f, .28f));
            Jar(t, new Vector3(.02f, on, .14f), new Color(.5f, .46f, .36f), .8f);
            Jar(t, new Vector3(.24f, on, .1f), new Color(.36f, .44f, .4f), 1.15f);
            var glass = Tint(art.stone, new Color(.34f, .5f, .4f)); var cork = Tint(art.timber, new Color(.6f, .46f, .3f));
            for (int k = 0; k < 4; k++) { var v = new Vector3(.45f + k * .075f, on, -.14f); Part(PrimitiveType.Cylinder, t, v + new Vector3(0, .06f, 0), new Vector3(.05f, .06f, .05f), glass); Part(PrimitiveType.Cylinder, t, v + new Vector3(0, .135f, 0), new Vector3(.025f, .015f, .025f), cork); }
            // A cut bunch laid ready, its heads to the front.
            Part(PrimitiveType.Capsule, t, new Vector3(-.05f, on + .03f, -.12f), new Vector3(.08f, .17f, .08f), Tint(art.foliage, Wither(new Color(.36f, .5f, .24f))), Quaternion.Euler(0, 75 + R() * 20, 90));
            for (int k = 0; k < 3; k++) Part(PrimitiveType.Sphere, t, new Vector3(-.2f + k * .04f, on + .05f, -.17f + (k % 2) * .03f), new Vector3(.07f, .05f, .07f), Tint(art.foliage, HerbHeads[k % HerbHeads.Length]));
            if (ash) foreach (float x in new[] { .52f, .66f }) Part(PrimitiveType.Cube, t, new Vector3(x, on + .07f, .12f), new Vector3(.12f, .14f, .1f), Tint(art.stone, new Color(.94f, .94f, .91f)), Quaternion.Euler(0, R() * 30 - 15, 0));
            // The drying rail behind it, hung with bunches.
            foreach (int sx in new[] { -1, 1 }) Rod(t, new Vector3(sx * .78f, LocalGround(t, sx * .78f, .48f) - .05f, .48f), new Vector3(sx * .78f, 1.78f, .48f), .07f, post);
            Rod(t, new Vector3(-.88f, 1.72f, .48f), new Vector3(.88f, 1.72f, .48f), .05f, post);
            for (int k = 0; k < 4; k++) HerbBundle(t, new Vector3(-.52f + k * .35f, 1.7f, .48f), k + (int)(R() * 3), .85f);
            // A basket of cut herbs at its end.
            float bg = LocalGround(t, 1.1f, -.1f);
            MeshPart(PropMesh("Herb basket", () => Turned(new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.27f, .26f), new Vector2(.24f, .26f), new Vector2(.18f, .04f), new Vector2(0, .04f) }, 12, .3f)), t, new Vector3(1.1f, bg + .005f, -.1f), art.hay);
            Part(PrimitiveType.Sphere, t, new Vector3(1.1f, bg + .23f, -.1f), new Vector3(.44f, .2f, .44f), Tint(art.foliage, Wither(new Color(.4f, .52f, .26f))));
        }
        /// <summary>
        /// A cookfire (GAME-ONLY): a ring of nine stones round a bed of ash and embers, three split logs burning in it with tongues
        /// of flame, a pot hung from an iron tripod over it, its smoke, and a light that glows brighter at night; a log to sit on and
        /// a few logs stacked by it. No colliders. Draws only from R.
        /// </summary>
        void Cookfire(Transform t, Func<float> R)
        {
            var iron = Tint(art.metal, new Color(.2f, .2f, .22f)); var stone = RockTint(Wither(new Color(.42f, .4f, .37f)));
            var coal = Tint(art.stone, new Color(.11f, .1f, .1f)); var ember = Glowing(new Color(1, .42f, .12f), 2.4f); var flame = Glowing(new Color(1, .55f, .2f), 2.2f);
            float g = FootUnder(t, 0, 0, .6f);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, LocalGround(t, 0, 0) + .015f, 0), new Vector3(1.05f, .015f, 1.05f), Tint(art.stone, new Color(.22f, .21f, .2f)));   // the ash bed
            for (int k = 0; k < 9; k++)
            {
                float a = k * 40 * Mathf.Deg2Rad + R() * .2f, r = .58f + R() * .06f, x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r, s = .24f + R() * .08f;
                Lump(BoulderAt((int)(R() * 6)), t, new Vector3(x, LocalGround(t, x, z) + s * .2f, z), new Vector3(s * 1.15f, s * .7f, s), stone, R() * 360);
            }
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39996f, r = .06f * Mathf.Sqrt(i + 1), size = .1f + (i % 3) * .025f;
                Lump(BoulderAt(i % 6), t, new Vector3(Mathf.Cos(a) * r, LocalGround(t, 0, 0) + .04f, Mathf.Sin(a) * r), new Vector3(size, size * .55f, size), i % 2 == 0 ? ember : coal, i * 53);
            }
            // Three split logs leaning in, their ends charred, and the flames between them.
            for (int k = 0; k < 3; k++)
            {
                var q = Quaternion.Euler(0, k * 120 + R() * 20, 0); var from = q * new Vector3(0, 0, .42f); from.y = LocalGround(t, from.x, from.z) + .05f;
                Rod(t, from, new Vector3(0, LocalGround(t, 0, 0) + .2f, 0) + q * new Vector3(0, 0, .06f), .11f, art.bark);
            }
            var tongue = PropMesh("Flame", () => ZoneMeshes.Cone(.1f, .34f, 6));
            for (int k = 0; k < 3; k++) MeshPart(tongue, t, new Vector3((k - 1) * .07f, LocalGround(t, 0, 0) + .06f, (k % 2) * .06f - .03f), flame, Quaternion.Euler((k - 1) * 8, k * 50, (k - 1) * -10)).transform.localScale = new Vector3(1, .8f + k % 2 * .45f, 1);
            // The tripod and the pot on its chain.
            var apex = new Vector3(0, g + 1.45f, 0);
            for (int k = 0; k < 3; k++) { float a = k * 2.094f + .3f; var fp = new Vector3(Mathf.Cos(a) * .82f, 0, Mathf.Sin(a) * .82f); fp.y = LocalGround(t, fp.x, fp.z) - .03f; Rod(t, fp, apex + (fp - apex).normalized * -.05f, .045f, iron); }
            Rod(t, apex, apex + Vector3.down * .62f, .015f, iron);
            var pot = PropMesh("Cook pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.23f, .1f), new Vector2(.23f, .24f), new Vector2(.19f, .3f), new Vector2(.21f, .33f), new Vector2(.17f, .33f), new Vector2(.17f, .27f), new Vector2(0, .27f) }, 10));
            float potY = apex.y - .62f - .33f;
            MeshPart(pot, t, new Vector3(0, potY, 0), iron);
            Part(PrimitiveType.Cylinder, t, new Vector3(0, potY + .285f, 0), new Vector3(.34f, .006f, .34f), Tint(art.cloth, new Color(.46f, .3f, .16f)));   // the stew
            foreach (int s in new[] { -1, 1 }) Rod(t, new Vector3(0, apex.y - .62f, 0), new Vector3(s * .2f, potY + .3f, 0), .015f, iron);   // its bail
            if (art.particle != null) Smoke(t, new Vector3(0, apex.y + .1f, 0));
            Glow(t, new Vector3(0, LocalGround(t, 0, 0) + .45f, 0), 5, 1.1f, new Color(1, .55f, .25f), 1.9f);
            // A log to sit on, and a few split logs stacked by it.
            float lx = 1.45f, lz = .35f;
            Part(PrimitiveType.Cylinder, t, new Vector3(lx, LocalGround(t, lx, lz) + .17f, lz), new Vector3(.34f, .55f, .34f), art.bark, Quaternion.Euler(90, 20 + R() * 20, 0));
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 3 - row; i++)
                {
                    float x = -1.35f + row * .1f + i * .22f, z = .55f;
                    Part(PrimitiveType.Cylinder, t, new Vector3(x, LocalGround(t, x, z) + .1f + row * .18f, z), new Vector3(.2f, .3f, .2f), art.bark, Quaternion.Euler(90, 0, 0));
                }
        }
    }
}
