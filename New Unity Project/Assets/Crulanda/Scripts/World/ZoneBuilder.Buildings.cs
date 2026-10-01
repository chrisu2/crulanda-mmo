using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Buildings that sit into the land, and the village's two hero buildings (the visual review's items 10 and 11, 2026-10-01):
    /// the stepped masonry footing a house, barn, inn or fallen house stands on, the threshold and steps up to a door, the
    /// inn's jettied front with its porch, sign, window boxes and lantern, and the smithy. Nothing here draws from the zone's
    /// random stream (looks that vary take a stream keyed on where the building stands), so every tree, rock and prop keeps
    /// its place, and every collider, door and workplace the buildings had stands where it stood.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        // ---------- on a slope ----------
        /// <summary>The lower and the higher of the height function and the drawn ground at a point of a prop (local x, z),
        /// relative to its root: between the ground grid's vertices the drawn triangles sit above or below the height function.</summary>
        void GroundRange(Transform t, float x, float z, out float low, out float high)
        {
            var p = t.TransformPoint(new Vector3(x, 0, z)); float a = HeightAt(p.x, p.z), b = MeshY(p.x, p.z), s = Mathf.Max(.01f, t.lossyScale.y);
            low = (Mathf.Min(a, b) - t.position.y) / s; high = (Mathf.Max(a, b) - t.position.y) / s;
        }
        /// <summary>The highest ground across a doorway <paramref name="wide"/> across, centred on x, at local z, relative to the root.</summary>
        float DoorGround(Transform t, float x, float z, float wide)
        {
            float high = float.MinValue;
            foreach (float u in new[] { -wide / 2, 0, wide / 2 }) { GroundRange(t, x + u, z, out _, out float h); high = Mathf.Max(high, h); }
            return high;
        }
        /// <summary>
        /// A stepped masonry footing round a building's walls, as a painter sets a house into a hillside. It is a band
        /// <paramref name="thick"/> deep inside the outer faces of a <paramref name="w"/> x <paramref name="d"/> footprint
        /// (centred on the root), laid in runs of about 1.4 m, each reaching a quarter metre into the ground under it:
        /// - its top is <paramref name="top"/> on the level; where the ground rises against the wall the run climbs by whole
        ///   .3 m courses (at most <paramref name="climb"/>), so the stone steps up the slope and the grass never cuts the wall;
        /// - where a run stands well out of the ground on the downhill side its foot steps out in one or two wider courses (a
        ///   terraced base), and every other tall run has a dark cellar vent under a lintel, so no face is a blank wedge;
        /// - a darker coping course caps each run, stepping with it.
        /// The front (-z) leaves a gap of half-width <paramref name="gap"/> round x = 0 for a door, and the back (+z) the
        /// stretch <paramref name="backGap"/> (from, to along x) where a lean-to stands against it. In
        /// <paramref name="stone"/>, the masonry if none. For the eye only: no colliders.
        /// </summary>
        void Footing(Transform t, float w, float d, float top, float thick, float climb, float gap = 0, Vector2? backGap = null, Material stone = null)
        {
            const float course = .3f, length = 1.4f;
            var blocks = new List<CombineInstance>(); var caps = new List<CombineInstance>(); var vents = new List<CombineInstance>();
            float hw = w / 2, hd = d / 2;
            // The sides: along x or z, the face's plane, the way it looks, and the stretch along it (front and back take the corners).
            var sides = new List<(bool alongX, float plane, int outward, float from, float to)>();
            if (gap > 0) { sides.Add((true, -hd, -1, -hw, -gap)); sides.Add((true, -hd, -1, gap, hw)); } else sides.Add((true, -hd, -1, -hw, hw));
            if (backGap.HasValue) { sides.Add((true, hd, 1, -hw, Mathf.Max(-hw, backGap.Value.x))); sides.Add((true, hd, 1, Mathf.Min(hw, backGap.Value.y), hw)); } else sides.Add((true, hd, 1, -hw, hw));
            sides.Add((false, -hw, -1, -hd + thick, hd - thick)); sides.Add((false, hw, 1, -hd + thick, hd - thick));
            int index = 0;
            foreach (var (alongX, plane, outward, from, to) in sides)
            {
                float span = to - from; if (span < .1f) continue;
                int n = Mathf.Max(1, Mathf.RoundToInt(span / length)); float step = span / n;
                // A block from a to b along the face, standing out from it by proud and back into the wall by deep, from y0 to y1.
                void Block(List<CombineInstance> list, float a, float b, float y0, float y1, float proud, float deep)
                {
                    float off = (proud - deep) / 2, mid = (a + b) / 2;
                    var at = alongX ? new Vector3(mid, (y0 + y1) / 2, plane + outward * off) : new Vector3(plane + outward * off, (y0 + y1) / 2, mid);
                    list.Add(Ashlar(at, alongX ? new Vector3(b - a, y1 - y0, proud + deep) : new Vector3(proud + deep, y1 - y0, b - a), 1.5f));
                }
                for (int k = 0; k < n; k++, index++)
                {
                    float u0 = from + k * step, u1 = u0 + step, lo = float.MaxValue, hi = float.MinValue;
                    foreach (float u in new[] { u0 + .05f, (u0 + u1) / 2, u1 - .05f })
                    {
                        GroundRange(t, alongX ? u : plane + outward * .12f, alongX ? plane + outward * .12f : u, out float l, out float h);
                        lo = Mathf.Min(lo, l); hi = Mathf.Max(hi, h);
                    }
                    float crown = top; if (hi + .22f > top) crown = top + Mathf.Min(climb, Mathf.Ceil((hi + .22f - top) / course) * course);
                    float foot = Mathf.Min(lo - .25f, crown - .2f), tall = crown - lo;
                    bool first = alongX && k == 0 && from <= -hw + .01f, last = alongX && k == n - 1 && to >= hw - .01f;   // an outer corner: the courses wrap it
                    Block(blocks, u0 - .005f, u1 + .005f, foot, crown - .1f, 0, thick);
                    Block(caps, u0 - (first ? .05f : .005f), u1 + (last ? .05f : .005f), crown - .1f, crown, .05f, thick);
                    // Terraces: the foot of a tall run steps out a course or two, on whole courses, so neighbouring runs line up.
                    float shelf = lo;
                    if (tall > .85f)
                    {
                        float upper = Mathf.Floor((lo + tall * .45f) / course) * course, lower = Mathf.Floor((lo + tall * .22f) / course) * course;
                        if (upper - lo >= .2f) { Block(blocks, u0 - (first ? .12f : .005f), u1 + (last ? .12f : .005f), foot, upper, .12f, thick); shelf = upper; }
                        if (tall > 1.6f && lower - lo >= .2f && lower < upper - .2f) Block(blocks, u0 - (first ? .24f : .005f), u1 + (last ? .24f : .005f), foot, lower, .24f, thick);
                    }
                    // A cellar vent in every other tall run, clear of the ground and the coping, under a lintel stone.
                    float vy = (Mathf.Max(hi, shelf) + crown - .1f) / 2;
                    if (tall > 1.05f && index % 2 == 1 && step > .9f && vy - .12f > hi + .08f && vy + .2f < crown - .12f)
                    {
                        float mid = (u0 + u1) / 2;
                        Block(vents, mid - .24f, mid + .24f, vy - .12f, vy + .12f, .015f, .05f);
                        Block(caps, mid - .34f, mid + .34f, vy + .12f, vy + .2f, .03f, .05f);
                    }
                }
            }
            var body = stone != null ? stone : Masonry; var c = body.color;
            if (blocks.Count > 0) Stonework("Footing", t, body, blocks.ToArray());
            if (caps.Count > 0) Stonework("Footing coping", t, Tint(body, new Color(c.r * .78f, c.g * .78f, c.b * .78f, c.a)), caps.ToArray());
            if (vents.Count > 0) Stonework("Cellar vents", t, Tint(art.timber, new Color(.06f, .05f, .04f)), vents.ToArray());
        }
        /// <summary>
        /// The stone under a door and the steps up to it. A threshold slab across the doorway (<paramref name="wide"/> across,
        /// centred on x) runs from <paramref name="back"/> inside the wall face at local z = <paramref name="face"/> to .34 m out
        /// of it; its top is the door's <paramref name="sill"/> and its foot is below the lowest ground across it, so a door on
        /// a slope stands on stone, never over a gap. Then, while the ground in front still lies a step or more below, up to
        /// five steps go out from it (each .3 m deep, .2 m down and a little wider than the last) until the ground comes up to
        /// meet them. In <paramref name="stone"/>, the masonry if none. For the eye only: no colliders.
        /// </summary>
        void DoorSteps(Transform t, float x, float face, float sill, float wide, float back = .05f, Material stone = null)
        {
            float Low(float z, float half)
            {
                float low = float.MaxValue;
                foreach (float u in new[] { -half, 0, half }) { GroundRange(t, x + u, z, out float l, out _); low = Mathf.Min(low, l); }
                return low;
            }
            float z0 = face + back, z1 = face - .34f, under = Mathf.Min(Low((z0 + z1) / 2, wide / 2) - .2f, sill - .1f);
            var blocks = new List<CombineInstance> { Ashlar(new Vector3(x, (sill + under) / 2, (z0 + z1) / 2), new Vector3(wide, sill - under, z0 - z1), 1.5f) };
            float edge = z1;
            for (int k = 1; k <= 5; k++)
            {
                float tread = sill - k * .2f, half = wide / 2 + k * .05f, low = Low(edge - .15f, half);
                if (tread < low + .08f) break;   // the ground has come up to meet the stair
                float foot = Mathf.Min(low - .2f, tread - .1f);
                blocks.Add(Ashlar(new Vector3(x, (tread + foot) / 2, edge - .15f), new Vector3(half * 2, tread - foot, .32f), 1.5f));
                edge -= .3f;
            }
            Stonework("Door steps", t, stone != null ? stone : Masonry, blocks.ToArray());
        }

        // ---------- the inn's front ----------
        static readonly Color[] Blooms = { new Color(.86f, .24f, .2f), new Color(.95f, .8f, .3f), new Color(.92f, .9f, .86f), new Color(.6f, .42f, .78f) };
        /// <summary>
        /// A window box of planks on two brackets, <paramref name="wide"/> long, centred on <paramref name="at"/> with the wall
        /// behind it (+z): earth, leafy clumps with flowers in four colours, and leaves trailing over its front; in a gloom only
        /// dry stalks.
        /// </summary>
        void FlowerBox(Transform t, Vector3 at, float wide, Func<float> R)
        {
            var planks = Tint(art.timber, new Color(.36f, .25f, .16f)); var leaf = Tint(art.foliage, new Color(.26f, .44f, .2f));
            // Every piece of one material goes into one mesh (a box is a few objects, not a dozen spheres): the inn's front is
            // not static-batched (its door moves), so each object would be a draw call of its own.
            var parts = new Dictionary<Material, List<CombineInstance>>();
            void Add(PrimitiveType type, Vector3 pos, Vector3 scale, Material m, Quaternion? rot = null)
            {
                if (!parts.TryGetValue(m, out var list)) parts[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = PrimitiveMesh(type), transform = Matrix4x4.TRS(pos, rot ?? Quaternion.identity, scale) });
            }
            Add(PrimitiveType.Cube, at, new Vector3(wide, .2f, .24f), planks);
            Add(PrimitiveType.Cube, at + new Vector3(0, .095f, 0), new Vector3(wide - .06f, .02f, .18f), Tint(art.stone, new Color(.22f, .17f, .12f)));   // the earth in it
            foreach (int k in new[] { -1, 1 }) Add(PrimitiveType.Cube, at + new Vector3(k * (wide / 2 - .12f), -.16f, .09f), new Vector3(.06f, .14f, .3f), planks);   // brackets back to the wall
            int n = Mathf.Max(3, Mathf.RoundToInt(wide / .2f));
            for (int i = 0; i < n; i++)
            {
                float x = -wide / 2 + (i + .5f) * wide / n;
                if (Gloom) { Add(PrimitiveType.Cube, at + new Vector3(x, .2f, 0), new Vector3(.02f, .22f + R() * .12f, .02f), Tint(art.hay, new Color(.4f, .33f, .24f)), Quaternion.Euler(R() * 30 - 15, 0, R() * 30 - 15)); continue; }
                Add(PrimitiveType.Sphere, at + new Vector3(x, .15f, -.03f + R() * .05f), new Vector3(.2f, .14f, .2f), leaf);
                if (i % 3 != 2) Add(PrimitiveType.Sphere, at + new Vector3(x + R() * .06f - .03f, .22f, -.07f + R() * .05f), Vector3.one * (.08f + R() * .03f), Tint(art.foliage, Blooms[(int)(R() * Blooms.Length) % Blooms.Length]));
                if (i % 2 == 1) Add(PrimitiveType.Sphere, at + new Vector3(x, -.02f, -.13f), new Vector3(.12f, .2f, .06f), leaf);   // trailing over the front
            }
            foreach (var kv in parts) { var mesh = Joined(kv.Value.ToArray()); mesh.name = "Flower box"; MeshPart(mesh, t, Vector3.zero, kv.Key); }
        }
        static readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        /// <summary>Unity's own mesh for a primitive (shared and never destroyed), to join scaled copies of it into one mesh.</summary>
        static Mesh PrimitiveMesh(PrimitiveType type)
        {
            if (!primitives.TryGetValue(type, out var m) || m == null) { var o = GameObject.CreatePrimitive(type); m = o.GetComponent<MeshFilter>().sharedMesh; DestroyImmediate(o); primitives[type] = m; }
            return m;
        }
        /// <summary>How far an inn's porch reaches out from its front wall's face (InnFront), and half its posts' spacing.</summary>
        const float InnPorchDeep = 2.3f, InnPorchHalf = 1.3f;
        /// <summary>Whether a point stands under an inn's porch roof, out past the inn's footprint (UnderRoof: no rain falls there).</summary>
        bool UnderPorch(Vector2 p)
        {
            foreach (var b in Zone.props)
            {
                if (b == null || b.kind != "inn") continue;
                float d = b.size.x > 0 ? b.size.y : 8, face = d / 2 + .15f, r = b.rotation * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); var q = p - b.at;
                float lx = q.x * c - q.y * s, lz = q.x * s + q.y * c;   // along its local x and z (as InBuilding)
                if (Mathf.Abs(lx) <= InnPorchHalf + .6f && lz <= -face + .1f && lz >= -(face + InnPorchDeep + .1f)) return true;
            }
            return false;
        }
        /// <summary>
        /// The outside of a walk-in inn in the painted style (Inn keeps its walls, colliders, door and taproom):
        /// - timber framing on every face: studs in the clear stretches between windows, door and corner posts, sill rails
        ///   under each row of windows and a head plate under the rafter ends, so the plaster reads as panels;
        /// - the upper front jettied .35 m out over the street on joist ends and brackets, with a bressumer, corner posts and
        ///   four larger mullioned windows of its own, each with a window box;
        /// - a gabled porch on two posts over the door, its gable boarded, the lit lantern (Inn's "Inn lantern") hung in an
        ///   iron lantern under its ceiling;
        /// - a hanging sign twice the old one's size on a bracket under the jetty, chained to a crossbar, painted with the inn's device (the Golden
        ///   Cask: a gilded cask on green; the Cracked Hearth, variant 1: a black pot over a glowing crack);
        /// - window boxes under the ground-floor front windows (dry stalks in a gloom), a bench under the first of them and
        ///   barrels at the far corner; two dormers on the front slope.
        /// A kitchen lean-to on the back wall (<paramref name="kitchen"/>: its middle along x) keeps its stretch clear. Only the
        /// porch is solid: each post cuts its own small hole in the navigation mesh (the way in between them stays open), and
        /// its roof is a ceiling overhead (no rain falls under it; see UnderPorch).
        /// </summary>
        void InnFront(Transform t, float w, float d, float H, float storey, float doorW, float doorH, float roofH, int variant, float? kitchen, Material plaster, Material roofMat)
        {
            const float wall = .3f, J = .35f;
            float face = -(d / 2 + wall / 2), jf = face - J, s = (w - 1) / 4;   // the front wall's outer face, the jetty's face, its windows' spacing
            var beam = art.timber; var dark = Tint(art.timber, new Color(.24f, .16f, .1f)); var iron = Tint(art.metal, new Color(.2f, .2f, .22f));
            var boards = Tint(art.timber, new Color(.36f, .26f, .17f));
            var look = TreeRandom(t.position); float R() { return (float)look.NextDouble(); }
            // Openings on each face as (from, to) along it and (bottom, top): the framing stops short of them. The windows are
            // the ones Inn lays out (the shutters included); the upper front has the jetty's own.
            var front = new List<Vector4> { new Vector4(-doorW / 2 - .14f, doorW / 2 + .14f, -1, doorH + .2f) };
            var back = new List<Vector4>(); var upperBack = new List<Vector4>(); var upper = new List<Vector4>(); var ends = new List<Vector4>(); var hearthEnd = new List<Vector4>();
            var frontWindows = new List<float>(); var jetty = new[] { -1.5f * s, -.5f * s, .5f * s, 1.5f * s };
            for (float x = -w / 2 + 1.5f; x < w / 2 - .8f; x += 2.6f)
            {
                if (Mathf.Abs(x) >= doorW) { front.Add(new Vector4(x - .86f, x + .86f, 1.05f, 1.95f)); frontWindows.Add(x); }
                if (!(kitchen.HasValue && Mathf.Abs(x - kitchen.Value) < KitchenSize.x / 2 + .85f)) back.Add(new Vector4(x - .86f, x + .86f, 1.05f, 1.95f));
                upperBack.Add(new Vector4(x - .52f, x + .52f, storey + .9f, storey + 1.7f));
            }
            if (kitchen.HasValue) back.Add(new Vector4(kitchen.Value - KitchenSize.x / 2 - .5f, kitchen.Value + KitchenSize.x / 2 + .5f, -1, storey - .05f));   // the lean-to and its roof
            foreach (float x in jetty) upper.Add(new Vector4(x - .7f, x + .7f, storey + .86f, storey + 1.75f));
            if (variant == 1) hearthEnd.Add(new Vector4(-.45f, 2.05f, -1, H + 1));   // the Cracked Hearth's chimney breast on the +x end
            // Studs and rails on one face (0 front, 1 back, 2 the -x end, 3 the +x end) at its plane, half its length along it;
            // the studs fill the band y0..y1. Returns where the studs stand.
            List<float> Frame(int side, float plane, float half, float y0, float y1, float[] rails, List<Vector4> open)
            {
                bool alongX = side < 2; float outward = side == 0 || side == 2 ? -1 : 1; var studs = new List<float>();
                Vector3 At(float u, float y, float off) { return alongX ? new Vector3(u, y, plane + outward * off) : new Vector3(plane + outward * off, y, u); }
                Vector3 Size(float along, float tall, float deep) { return alongX ? new Vector3(along, tall, deep) : new Vector3(deep, tall, along); }
                List<Vector2> Free(float lo, float hi)   // the stretches of the face clear of every opening between heights lo and hi
                {
                    var free = new List<Vector2>(); float from = -half + .2f;
                    foreach (var o in open.Where(o => o.z < hi && o.w > lo).OrderBy(o => o.x)) { if (o.x > from) free.Add(new Vector2(from, o.x)); from = Mathf.Max(from, o.y); }
                    if (half - .2f > from) free.Add(new Vector2(from, half - .2f));
                    return free;
                }
                foreach (var g in Free(y0, y1))
                {
                    float span = g.y - g.x; if (span < .5f) continue;
                    int n = Mathf.Max(1, Mathf.FloorToInt(span / 2.2f));
                    for (int k = 1; k <= n; k++) { float u = g.x + span * k / (n + 1); studs.Add(u); Part(PrimitiveType.Cube, t, At(u, (y0 + y1) / 2, .03f), Size(.14f, y1 - y0, .08f), beam); }
                }
                foreach (float y in rails) foreach (var g in Free(y - .08f, y + .08f)) if (g.y - g.x > .2f) Part(PrimitiveType.Cube, t, At((g.x + g.y) / 2, y, .035f), Size(g.y - g.x, .12f, .09f), beam);
                return studs;
            }
            // The front and back carry a head plate just under the rafter ends (the roof's soffit hides the wall above them); on
            // the ends the gable's tie beam is the head.
            var below = Frame(0, face, w / 2, .3f, storey - .11f, new[] { .92f }, front);
            Frame(0, jf, w / 2, storey + .34f, H - .54f, new[] { storey + .78f, H - .48f }, upper);
            Frame(1, -face, w / 2, .3f, storey - .11f, new[] { .92f }, back);
            Frame(1, -face, w / 2, storey + .11f, H - .54f, new[] { storey + .78f, H - .48f }, upperBack);
            foreach (int side in new[] { 2, 3 })
            {
                var open = side == 3 ? hearthEnd : ends; float plane = (side == 2 ? -1 : 1) * (w / 2 + wall / 2);
                Frame(side, plane, d / 2, .3f, storey - .11f, new[] { .92f }, open);
                Frame(side, plane, d / 2, storey + .11f, H - .11f, new[] { storey + .78f }, open);
            }
            // The jetty: the upper front in plaster .35 m out, on joist ends, a bressumer along its foot and posts at its corners;
            // brackets from the ground-floor studs and corners up under it (none behind the sign's bracket).
            BoxPart(t, new Vector3(0, (storey + .12f + H) / 2, face - J / 2), new Vector3(w + .34f, H - storey - .12f, J), plaster);
            foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(k * (w / 2 + .02f), (storey + .12f + H) / 2, jf + .12f), new Vector3(.3f, H - storey - .12f, .3f), beam);
            Part(PrimitiveType.Cube, t, new Vector3(0, storey + .23f, jf + .04f), new Vector3(w + .4f, .22f, .16f), beam);
            for (float x = -w / 2 + .45f; x < w / 2 - .3f; x += .6f) Part(PrimitiveType.Cube, t, new Vector3(x, storey + .03f, face - J / 2 + .02f), new Vector3(.13f, .16f, J + .06f), dark);
            float sx = -w / 2 + 2.8f;   // the sign's bracket, clear of the porch
            foreach (float x in below.Concat(new[] { -w / 2 + .05f, w / 2 - .05f }))
                if (Mathf.Abs(x - sx) > .35f) Bar(t, new Vector3(x, storey - .8f, face - .02f), new Vector3(x, storey - .06f, jf + .08f), .1f, .1f, beam);
            foreach (float x in jetty)
            {
                float y = storey + 1.3f;
                Window(t, new Vector3(x, y, jf), 1.15f, .85f, -1, variant, false);
                Part(PrimitiveType.Cube, t, new Vector3(x, y, jf - .07f), new Vector3(.07f, .85f, .1f), dark);   // the mullion
                FlowerBox(t, new Vector3(x, y - .67f, jf - .25f), 1.2f, R);
            }
            foreach (float x in frontWindows) FlowerBox(t, new Vector3(x, .86f, face - .25f), 1.05f, R);
            // The porch: two posts and their plates back to the wall, a beam across, a small gable roof with its end boarded and
            // a king post and barge boards on it; the lantern hangs from its ridge round the inn's lantern light.
            // The posts and timbers stand under the roof's flat ceiling (eave - .12). Both inns take the same depth: the Cracked
            // Hearth's posts then stand clear of Khaven's barrels at (-7, 12).
            float deep = InnPorchDeep, px = InnPorchHalf, eave = 2.7f, rise = .85f, zp = face - deep + .3f, span = px * 2 + .9f;
            foreach (int k in new[] { -1, 1 })
            {
                float g = LocalGround(t, k * px, zp);
                var post = Stake(t, new Vector3(k * px, g - .1f, zp), .2f, eave - .12f - g + .1f, beam); post.AddComponent<BoxCollider>(); post.AddComponent<NavBlocker>();
                Part(PrimitiveType.Cube, t, new Vector3(k * px, eave - .2f, (face + zp) / 2), new Vector3(.14f, .16f, face - zp + .4f), beam);
            }
            Part(PrimitiveType.Cube, t, new Vector3(0, eave - .2f, zp), new Vector3(span - .2f, .16f, .16f), beam);
            MeshPart(ZoneMeshes.GableRoof(deep + .06f, span, rise, .12f), t, new Vector3(0, eave, face - deep / 2 + .03f), roofMat, Quaternion.Euler(0, 90, 0)).AddComponent<BoxCollider>();   // solid: WorldWeather looks up for a ceiling
            MeshPart(PropMesh("Porch gable", () => ZoneMeshes.Gable(span, rise, .04f, 1)), t, new Vector3(0, eave, face - deep - .025f), boards);
            Part(PrimitiveType.Cube, t, new Vector3(0, eave + (rise - .12f) / 2, face - deep - .06f), new Vector3(.1f, rise - .12f, .05f), dark);
            foreach (int k in new[] { -1, 1 }) Bar(t, new Vector3(k * (span / 2 + .02f), eave - .04f, face - deep - .06f), new Vector3(0, eave + rise + .02f, face - deep - .06f), .14f, .05f, dark);
            float lz = face - 1.05f;   // Inn's lantern light stands here, 2.2 m up, the lantern hung from the ceiling
            Rod(t, new Vector3(0, eave - .12f, lz), new Vector3(0, 2.5f, lz), .025f, iron);
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.2f, lz), new Vector3(.24f, .3f, .24f), art.glass);
            MeshPart(PropMesh("Lantern roof", () => ZoneMeshes.Cone(.27f, .2f, 4)), t, new Vector3(0, 2.35f, lz), iron, Quaternion.Euler(0, 45, 0));
            Part(PrimitiveType.Cube, t, new Vector3(0, 2.03f, lz), new Vector3(.3f, .04f, .3f), iron);
            foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(a * .13f, 2.2f, lz + b * .13f), new Vector3(.025f, .3f, .025f), iron);
            // The sign: an arm under the joists on a strut and a plate on the wall, an iron crossbar under the arm's end with two
            // chains from it, and the board in a frame with the inn's device on its face.
            float sy = storey - .2f, zb = face - 1.5f, by = sy - .8f;
            Part(PrimitiveType.Cube, t, new Vector3(sx, sy, face - .95f), new Vector3(.12f, .12f, 1.9f), beam);
            Bar(t, new Vector3(sx, sy - .75f, face - .03f), new Vector3(sx, sy - .06f, face - .8f), .1f, .1f, beam);
            Part(PrimitiveType.Cube, t, new Vector3(sx, sy - .4f, face - .04f), new Vector3(.22f, 1, .08f), beam);
            Part(PrimitiveType.Cube, t, new Vector3(sx, sy - .085f, zb), new Vector3(1.32f, .06f, .06f), iron);
            foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(sx + k * .6f, sy - .155f, zb), new Vector3(.03f, .2f, .03f), iron);
            Vector2[] Octagon(float hx, float hy, float cut) { return new[] { new Vector2(-hx, -hy + cut), new Vector2(-hx, hy - cut), new Vector2(-hx + cut, hy), new Vector2(hx - cut, hy), new Vector2(hx, hy - cut), new Vector2(hx, -hy + cut), new Vector2(hx - cut, -hy), new Vector2(-hx + cut, -hy) }; }
            bool cask = variant != 1;
            var gold = Tint(art.metal, new Color(.86f, .66f, .26f));
            MeshPart(PropMesh("Inn sign board", () => Cutout(Octagon(.8f, .55f, .1f), .07f)), t, new Vector3(sx, by, zb), Tint(art.timber, cask ? new Color(.2f, .32f, .22f) : new Color(.17f, .14f, .12f)));
            MeshPart(PropMesh("Inn sign frame", () => Cutout(Octagon(.86f, .61f, .12f), .05f)), t, new Vector3(sx, by, zb + .045f), cask ? gold : iron);
            if (cask)
            {
                var device = Barrel(t, new Vector3(sx - .36f, by, zb - .255f), .72f);   // a cask on its side, gilded
                device.transform.localRotation = Quaternion.Euler(0, 0, -90);
                device.GetComponent<MeshRenderer>().sharedMaterials = new[] { gold, Tint(art.metal, new Color(.5f, .38f, .16f)) };
            }
            else
            {
                MeshPart(PropMesh("Sign pot", () => Turned(new[] { new Vector2(0, 0), new Vector2(.12f, 0), new Vector2(.19f, .08f), new Vector2(.19f, .2f), new Vector2(.15f, .26f), new Vector2(.17f, .28f), new Vector2(.13f, .28f), new Vector2(.13f, .23f), new Vector2(0, .23f) }, 10)), t, new Vector3(sx, by - .12f, zb - .23f), iron);
                var crack = Glowing(new Color(1, .42f, .14f), 2.2f);
                for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3(sx - .45f + i * .3f, by - .3f + (i % 2) * .05f, zb - .04f), new Vector3(.33f, .035f, .012f), crack, Quaternion.Euler(0, 0, i % 2 == 0 ? 12 : -14));
            }
            // A bench under the first front window, and barrels at the far corner, clear of the window boxes.
            float bx = frontWindows.Count > 0 ? frontWindows[0] : -w / 2 + 1.5f, bz = face - .32f, bg = LocalGround(t, bx, bz);
            Part(PrimitiveType.Cube, t, new Vector3(bx, bg + .45f, bz), new Vector3(1.6f, .07f, .36f), boards);
            foreach (int k in new[] { -1, 1 }) { float lg = LocalGround(t, bx + k * .62f, bz); Part(PrimitiveType.Cube, t, new Vector3(bx + k * .62f, (lg - .05f + bg + .42f) / 2, bz), new Vector3(.08f, bg + .47f - lg, .3f), dark); }
            foreach (var (x, z, size) in new[] { (w / 2 - 1.1f, face - .75f, 1f), (w / 2 - 1.85f, face - .7f, .9f), (w / 2 - 1.45f, face - 1.45f, .8f) })
                Barrel(t, new Vector3(x, LocalGround(t, x, z), z), size, x * 40);
            // Two dormers on the front slope, between the jetty's window pairs: a plastered face with a window, cheeks running
            // back into the roof, corner posts and a little gabled roof with its end boarded.
            float hd = d / 2 + .7f, zf = -(hd - 1.1f), ys = H + roofH * 1.1f / hd, ye = ys + 1;   // the dormer's face, where the roof meets it, its eave
            foreach (int k in new[] { -1, 1 })
            {
                float x = k * s;
                BoxPart(t, new Vector3(x, (ys - .3f + ye) / 2, zf + .06f), new Vector3(1.3f, ye - ys + .3f, .12f), plaster);
                foreach (int c in new[] { -1, 1 })
                {
                    BoxPart(t, new Vector3(x + c * .6f, (ys - .4f + ye) / 2, zf + 1.05f), new Vector3(.1f, ye - ys + .4f, 2), plaster);
                    Part(PrimitiveType.Cube, t, new Vector3(x + c * .6f, (ys + ye) / 2, zf - .01f), new Vector3(.12f, ye - ys, .14f), beam);
                }
                Window(t, new Vector3(x, ys + .5f, zf), .6f, .55f, -1, variant, false);
                MeshPart(ZoneMeshes.GableRoof(2.4f, 1.6f, .55f, .1f), t, new Vector3(x, ye, zf + .95f), roofMat, Quaternion.Euler(0, 90, 0));
                MeshPart(PropMesh("Dormer gable", () => ZoneMeshes.Gable(1.6f, .55f, .04f, 1)), t, new Vector3(x, ye, zf - .27f), boards);
            }
        }

        // ---------- the smithy ----------
        /// <summary>
        /// A smithy in the painted style (front faces -Z; the hearth, the back wall and the anvil stand where they always did):
        /// - the hearth house: a slate gable roof (metre UVs, fascia, rafter ends and ridge cap) on a stone end wall over the
        ///   hearth side, a plank half wall and a corner post at the other end, and planks above the stone back wall; tie beams
        ///   and king posts in its boarded gables; a flagged floor;
        /// - a lean-to of slate on rafters over the anvil in front, on a beam and two posts;
        /// - the stone hearth with a heap of coals glowing in it, a mantel beam and a tapered hood, and the chimney stack up
        ///   through the roof with a cap, a pot and its smoke; leather bellows beside it, their nozzle in the fire;
        /// - the anvil (foot, waist, face and horn) on its stump with a hammer on its face and tongs against it; a stone quench
        ///   trough; tongs, hammers and files on a rail on the back wall;
        /// - outside the open end a rack of finished work (horseshoes, sickles, axe heads) and a barrel of bar iron and blades;
        ///   a grindstone on its frame by the front corner.
        /// The hearth, back wall, anvil, end walls and trough are solid; the smith stands at the anvil or the hearth.
        /// </summary>
        void Forge(Transform t)
        {
            var dark = Tint(art.timber, new Color(.25f, .17f, .11f)); var iron = Tint(art.metal, new Color(.22f, .22f, .24f));
            var boards = Tint(art.timber, new Color(.4f, .29f, .19f)); var hide = Tint(art.cloth, new Color(.42f, .29f, .18f));
            const float top = 3.2f, roofH = 1.6f, end = 2.4f, front = -.6f, rear = 2.1f;   // the eaves, the ridge's rise, the end walls' middles, the hearth house's front and back faces
            float G(float x, float z) { return LocalGround(t, x, z); }
            // The hearth house: the stone back wall (as before) with planks above it, the stone end wall, the plank half wall
            // with its rail, a corner post at each end of it, and plates under the roof's soffit (it hides anything above it).
            float low = Mathf.Min(G(-2.5f, 1.95f), Mathf.Min(G(0, 1.95f), G(2.5f, 1.95f)));
            BoxPart(t, new Vector3(0, (1.4f + Mathf.Min(0, low) - .2f) / 2, 1.95f), new Vector3(5, 1.4f - Mathf.Min(0, low) + .2f, .3f), Masonry, null, 1);
            BoxPart(t, new Vector3(0, (1.4f + top) / 2, 2.02f), new Vector3(5, top - 1.4f, .1f), boards, null, 1);
            float lowEnd = Mathf.Min(0, Mathf.Min(G(end, front), G(end, rear)));
            BoxPart(t, new Vector3(end, (top + lowEnd - .2f) / 2, (front + rear) / 2), new Vector3(.3f, top - lowEnd + .2f, rear - front), Masonry, null, 1);
            BoxPart(t, new Vector3(-end, .55f, .85f), new Vector3(.12f, 1.1f, 2.5f), boards, null, 1);
            Part(PrimitiveType.Cube, t, new Vector3(-end, 1.14f, .85f), new Vector3(.18f, .08f, 2.6f), dark);
            foreach (float z in new[] { front, 1.75f }) Stake(t, new Vector3(-end, G(-end, z) - .1f, z), .22f, top - .45f - G(-end, z) + .1f, dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, top - .35f, front), new Vector3(2 * end + .3f, .2f, .2f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, top - .35f, 1.91f), new Vector3(2 * end + .3f, .2f, .12f), dark);
            BoxPart(t, new Vector3(0, .025f, .75f), new Vector3(2 * end - .2f, .05f, 2.6f), Tint(Masonry, new Color(.56f, .54f, .5f)), null, 1.2f);   // the flagged floor
            var roof = new GameObject("Smithy roof").transform; roof.SetParent(t, false); roof.localPosition = new Vector3(0, 0, (front + rear) / 2);
            float d = rear - front, w = 2 * end + .3f;   // the hearth house's walls, outer faces
            MeshPart(ZoneMeshes.GableRoof(w + .9f, d + 1.4f, roofH, .25f, .45f), roof, new Vector3(0, top, 0), art.slate);
            Eaves(roof, w - .4f, d, top, roofH, art.slate);
            Gables(roof, w / 2, d, top, roofH, d / 2 + .7f, w / 2 + .45f, boards, 1.2f, .2f);
            // The lean-to over the anvil: slate from under the front plate down to a beam on two posts, on six rafters, a fascia at its foot.
            const float hiY = 2.68f, loY = 2.35f, z0 = -.55f, z1 = -2.4f; float slope = (hiY - loY) / (z0 - z1), pitch = Mathf.Atan(slope) * Mathf.Rad2Deg;
            float Under(float z) { return hiY - (z0 - z) * slope - .04f; }   // the lean-to slab's underside over local z
            BoxPart(t, new Vector3(0, (hiY + loY) / 2, (z0 + z1) / 2), new Vector3(5.8f, .08f, Mathf.Sqrt((z0 - z1) * (z0 - z1) + (hiY - loY) * (hiY - loY)) + .05f), art.slate, Quaternion.Euler(-pitch, 0, 0), 2.5f);
            foreach (float x in new[] { -2.5f, -1.5f, -.5f, .5f, 1.5f, 2.5f }) Bar(t, new Vector3(x, Under(-.62f) - .045f, -.62f), new Vector3(x, Under(-2.3f) - .045f, -2.3f), .09f, .07f, dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, Under(z1) - .04f, z1 - .03f), new Vector3(5.9f, .18f, .05f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(0, Under(-1.9f) - .17f, -1.9f), new Vector3(5.6f, .16f, .16f), dark);
            foreach (int k in new[] { -1, 1 }) Stake(t, new Vector3(k * end, G(k * end, -1.9f) - .1f, -1.9f), .2f, Under(-1.9f) - .25f - G(k * end, -1.9f) + .1f, dark);
            // The hearth: its stone, the fire's heart under a heap of coals (every third one glowing), a mantel beam, the
            // tapered hood and the stack up through the back slope with its cap, a pot and the smoke.
            BoxPart(t, new Vector3(1.2f, .5f, 1.1f), new Vector3(1.8f, 1, 1.3f), Masonry, null, 1);
            foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.04f, 1.05f + k * .42f), new Vector3(1.3f, .08f, .1f), Tint(Masonry, new Color(.4f, .38f, .35f)));   // the fire bed's kerbs
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.02f, 1.05f), new Vector3(1.0f, .03f, .7f), art.glass);
            var coal = Tint(art.stone, new Color(.11f, .1f, .1f)); var ember = Glowing(new Color(1, .42f, .12f), 2.4f);
            for (int i = 0; i < 16; i++)
            {
                float a = i * 2.39996f, r = .07f * Mathf.Sqrt(i + 1), size = .13f + (i % 3) * .03f;
                Lump(BoulderAt(i % 6), t, new Vector3(1.2f + Mathf.Cos(a) * r * 1.5f, 1.05f + (.3f - r) * .25f, 1.05f + Mathf.Sin(a) * r), new Vector3(size, size * .6f, size), i % 3 == 0 ? ember : coal, i * 47);
            }
            Part(PrimitiveType.Cube, t, new Vector3(1.2f, 1.62f, .52f), new Vector3(1.5f, .14f, .16f), dark);
            MeshPart(PropMesh("Forge hood", () => Turned(new[] { new Vector2(.99f, 0), new Vector2(.45f, .9f), new Vector2(.36f, .9f), new Vector2(.9f, 0) }, 4)), t, new Vector3(1.2f, 1.62f, 1.25f), Masonry, Quaternion.Euler(0, 45, 0));
            BoxPart(t, new Vector3(1.2f, 3.96f, 1.25f), new Vector3(.64f, 2.9f, .64f), Masonry, null, 1);
            BoxPart(t, new Vector3(1.2f, 5.46f, 1.25f), new Vector3(.84f, .12f, .84f), Tint(Masonry, new Color(.45f, .44f, .4f)), null, 1);
            Part(PrimitiveType.Cylinder, t, new Vector3(1.2f, 5.62f, 1.25f), new Vector3(.26f, .1f, .26f), Tint(art.stone, new Color(.62f, .4f, .28f)));
            if (art.particle != null) Smoke(t, new Vector3(1.2f, 5.8f, 1.25f));
            Glow(t, new Vector3(1.2f, 1.5f, .6f), 7, 1.4f, new Color(1, .5f, .2f), 2.2f);
            // The bellows, lying left of the hearth: two boards with the leather between, on a little trestle, the nozzle into
            // the fire's side and a pole at the wide end to work them.
            var leaf = PropMesh("Bellows board", () => Cutout(new[] { new Vector2(-.12f, -.45f), new Vector2(-.26f, -.1f), new Vector2(-.24f, .3f), new Vector2(-.12f, .45f), new Vector2(.12f, .45f), new Vector2(.24f, .3f), new Vector2(.26f, -.1f), new Vector2(.12f, -.45f) }, .04f));
            var lay = Quaternion.Euler(90, -90, 0);   // the board's narrow end toward the hearth (+x)
            MeshPart(leaf, t, new Vector3(-.4f, .72f, 1.15f), boards, lay);
            MeshPart(leaf, t, new Vector3(-.4f, .97f, 1.15f), boards, Quaternion.Euler(0, 0, -5) * lay);
            Part(PrimitiveType.Cube, t, new Vector3(-.45f, .845f, 1.15f), new Vector3(.75f, .22f, .42f), hide);
            Rod(t, new Vector3(.04f, .8f, 1.15f), new Vector3(.36f, .82f, 1.15f), .05f, iron);
            foreach (float x in new[] { -.75f, -.1f }) Part(PrimitiveType.Cube, t, new Vector3(x, .34f, 1.15f), new Vector3(.06f, .68f, .4f), dark);
            Rod(t, new Vector3(-.85f, .98f, 1.15f), new Vector3(-.95f, 1.6f, 1.15f), .05f, art.timber);
            // The anvil on its stump, a hammer on its face and tongs against the stump.
            MeshPart2(PropMesh("Anvil stump", () => TwoTone(Turned(new[] { new Vector2(.36f, 0), new Vector2(.32f, .15f), new Vector2(.3f, .55f) }, 10), Turned(new[] { new Vector2(.3f, .55f), new Vector2(0, .55f) }, 10))), t, new Vector3(-.6f, 0, -.2f), art.bark, Tint(art.timber, new Color(.62f, .5f, .34f)));
            Part(PrimitiveType.Cube, t, new Vector3(-.6f, .6f, -.2f), new Vector3(.34f, .1f, .5f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(-.6f, .71f, -.2f), new Vector3(.18f, .12f, .34f), iron);
            Part(PrimitiveType.Cube, t, new Vector3(-.6f, .83f, -.24f), new Vector3(.3f, .12f, .6f), iron);
            MeshPart(PropMesh("Anvil horn", () => ZoneMeshes.Cone(.07f, .3f, 8)), t, new Vector3(-.6f, .82f, .06f), iron, Quaternion.Euler(90, 0, 0));
            Rod(t, new Vector3(-.72f, .91f, -.42f), new Vector3(-.5f, .91f, -.12f), .03f, art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(-.74f, .925f, -.45f), new Vector3(.07f, .07f, .15f), iron, Quaternion.Euler(0, 36, 0));
            foreach (float x in new[] { -.24f, -.2f }) Bar(t, new Vector3(x, .02f, -.36f), new Vector3(x - .08f, .62f, -.26f), .03f, .02f, iron);
            // The quench trough in stone, its water just under the rim.
            Stonework("Quench trough", t, Masonry, Ashlar(new Vector3(-1.65f, .06f, .8f), new Vector3(1.1f, .12f, .5f), 1), Ashlar(new Vector3(-1.65f, .33f, .58f), new Vector3(1.1f, .42f, .06f), 1),
                Ashlar(new Vector3(-1.65f, .33f, 1.02f), new Vector3(1.1f, .42f, .06f), 1), Ashlar(new Vector3(-2.17f, .33f, .8f), new Vector3(.06f, .42f, .38f), 1), Ashlar(new Vector3(-1.13f, .33f, .8f), new Vector3(.06f, .42f, .38f), 1));
            Part(PrimitiveType.Cube, t, new Vector3(-1.65f, .47f, .8f), new Vector3(.98f, .02f, .38f), art.water);
            // Tongs, hammers and files hung on a rail on the back wall.
            Part(PrimitiveType.Cube, t, new Vector3(-1.05f, 1.95f, 1.93f), new Vector3(2.3f, .08f, .06f), dark);
            for (int i = 0; i < 6; i++)
            {
                float x = -1.95f + i * .36f;
                Part(PrimitiveType.Cube, t, new Vector3(x, 1.93f, 1.88f), new Vector3(.03f, .03f, .08f), iron);   // the peg
                switch (i % 3)
                {
                    case 0: foreach (int k in new[] { -1, 1 }) Part(PrimitiveType.Cube, t, new Vector3(x + k * .025f, 1.66f, 1.88f), new Vector3(.025f, .52f, .02f), iron, Quaternion.Euler(0, 0, k * 4)); break;   // tongs
                    case 1: Part(PrimitiveType.Cube, t, new Vector3(x, 1.72f, 1.88f), new Vector3(.035f, .4f, .035f), art.timber); Part(PrimitiveType.Cube, t, new Vector3(x, 1.5f, 1.88f), new Vector3(.15f, .065f, .065f), iron); break;   // a hammer
                    default: Part(PrimitiveType.Cube, t, new Vector3(x, 1.74f, 1.88f), new Vector3(.03f, .36f, .015f), iron); break;   // a file
                }
            }
            // Finished work on a rack outside the open end (horseshoes on the upper rail, sickles and axe heads on the lower),
            // and a barrel of bar iron and blades in front of it.
            float rx = -end - .55f;
            foreach (float z in new[] { .15f, 1.65f }) Stake(t, new Vector3(rx, G(rx, z) - .1f, z), .14f, 1.6f + .1f, dark);
            foreach (float y in new[] { 1.38f, .82f }) Part(PrimitiveType.Cube, t, new Vector3(rx, G(rx, .9f) + y, .9f), new Vector3(.06f, .06f, 1.65f), dark);
            var shoe = PropMesh("Horseshoe", () => ZoneMeshes.Arc(.055f, .085f, -40, 220, .02f));
            var sickle = PropMesh("Sickle blade", () => ZoneMeshes.Arc(.15f, .18f, 20, 200, .015f));
            var hang = Quaternion.Euler(-90, 0, 90);   // upright in the rack's plane, the open end down
            for (int i = 0; i < 4; i++) MeshPart(shoe, t, new Vector3(rx - .05f, G(rx, .9f) + 1.24f, .4f + i * .32f), iron, hang);
            for (int i = 0; i < 2; i++)
            {
                float z = .45f + i * .9f, y = G(rx, .9f) + .66f;
                MeshPart(sickle, t, new Vector3(rx - .05f, y, z), iron, hang);
                Part(PrimitiveType.Cube, t, new Vector3(rx - .06f, y - .02f, z + .2f), new Vector3(.035f, .035f, .16f), art.timber);   // its handle
                Part(PrimitiveType.Cube, t, new Vector3(rx - .06f, y - .04f, z + .45f), new Vector3(.04f, .12f, .16f), iron);              // an axe head beside it
            }
            float gx = -end - .5f, gz = -.6f;
            Barrel(t, new Vector3(gx, G(gx, gz), gz), .8f, 30);
            for (int i = 0; i < 5; i++) Part(PrimitiveType.Cube, t, new Vector3(gx + (i - 2) * .07f, G(gx, gz) + 1, gz + (i % 2) * .08f - .04f), new Vector3(.035f, 1.1f, i % 2 == 0 ? .035f : .07f), iron, Quaternion.Euler((i - 2) * 4, 0, (i - 2) * -5));
            // The grindstone by the front corner: the stone on its axle in an A-frame, a crank and a trough of water under it.
            float wx = -end - 1.3f, wz = -1.7f, wg = G(wx, wz);
            var stone = Tint(art.stone, new Color(.62f, .6f, .54f));
            MeshPart(PropMesh("Grindstone", () => Turned(new[] { new Vector2(0, -.07f), new Vector2(.34f, -.07f), new Vector2(.37f, -.03f), new Vector2(.37f, .03f), new Vector2(.34f, .07f), new Vector2(0, .07f) }, 16, .5f)), t, new Vector3(wx, wg + .62f, wz), stone, Quaternion.Euler(0, 0, 90));
            foreach (int k in new[] { -1, 1 }) foreach (int f in new[] { -1, 1 }) Bar(t, new Vector3(wx + k * .18f, wg - .02f, wz + f * .36f), new Vector3(wx + k * .18f, wg + .64f, wz), .07f, .06f, dark);
            Rod(t, new Vector3(wx - .3f, wg + .62f, wz), new Vector3(wx + .36f, wg + .62f, wz), .045f, iron);
            Part(PrimitiveType.Cube, t, new Vector3(wx + .38f, wg + .52f, wz), new Vector3(.04f, .22f, .04f), iron);
            Rod(t, new Vector3(wx + .38f, wg + .42f, wz), new Vector3(wx + .52f, wg + .42f, wz), .04f, art.timber);
            Part(PrimitiveType.Cube, t, new Vector3(wx, wg + .12f, wz), new Vector3(.3f, .24f, .56f), boards);
            Part(PrimitiveType.Cube, t, new Vector3(wx, wg + .23f, wz), new Vector3(.24f, .02f, .5f), art.water);
            // As before: the hearth, the back wall and the anvil; and now the two end walls and the trough.
            Solid(t, new Vector3(1.2f, 1, 1.2f), new Vector3(1.9f, 2, 1.5f));
            Solid(t, new Vector3(0, .7f, 1.95f), new Vector3(5, 1.4f, .3f));
            Solid(t, new Vector3(-.6f, .45f, -.2f), new Vector3(.6f, .9f, .75f));
            Solid(t, new Vector3(end, top / 2, (front + rear) / 2), new Vector3(.3f, top, rear - front));
            Solid(t, new Vector3(-end, .55f, .85f), new Vector3(.2f, 1.1f, 2.5f));
            Solid(t, new Vector3(-1.65f, .27f, .8f), new Vector3(1.1f, .54f, .5f));
            Workplace(t, "forge", new Vector3(-.6f, 0, -1.15f), new Vector3(-.6f, .9f, -.2f));
            Workplace(t, "forge", new Vector3(1.2f, 0, -.3f), new Vector3(1.2f, 1, 1.1f));
        }
    }
}
