using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A walk-in cave's passage (a "cavern" prop, such as Crowsfoot Hollow): rings along its floor from the mouth inward, each a
    /// centre on the floor, a half-width and a height. The floor is the passage's own: it starts at the ground at the mouth and
    /// drops as the plan says, down under the land (the ground mesh is cut away wherever it would run through the passage, and
    /// the rock shell covers the cut). Made before the ground is built (ZoneBuilder.PrepareHollows), so the ground, its paint,
    /// the grass, the trees, the rain and the light can all ask about it:
    /// - <see cref="Inside"/>: whether a point is in the passage's volume (the ground is cut there);
    /// - <see cref="Cover"/>: how much of a ground point the passage covers where it runs near the surface (bare floor, no
    ///   grass or trees, under a roof); a passage deep under the land leaves the land above alone;
    /// - <see cref="FloorAt"/>: the passage floor under a point (camps and props inside are set on it, not on the land above);
    /// - <see cref="Depth"/>: 0 at the mouth to 1 from ten metres in (WorldClock sets <see cref="CameraDepth"/>; ZonePost reads it).
    /// </summary>
    public sealed class Hollow
    {
        /// <summary>The zone's caves (cleared and refilled when a zone is built).</summary>
        public static readonly List<Hollow> All = new List<Hollow>();
        /// <summary>How deep the camera is in a cave this frame: 0 outside, 1 from about ten metres in (set by WorldClock).</summary>
        public static float CameraDepth;
        public readonly string Name;
        /// <summary>Ring centres on the floor (world), half-widths, heights, the distance in from the mouth along the floor, and the land's height over each.</summary>
        public readonly List<Vector3> Centre = new List<Vector3>();
        public readonly List<float> Half = new List<float>(), Height = new List<float>(), Along = new List<float>(), Land = new List<float>();
        Rect box, surfaceBox; float low = float.MaxValue, high = float.MinValue;
        Vector2 mouthAt, mouthOut;   // the mouth ring's centre, and the level way out of the mouth: nothing in front of it is inside
        readonly List<int> surface = new List<int>();   // the rings near the surface: only they can cover the land

        /// <summary>
        /// The passage from its prop: <paramref name="plan"/> rows are (x, z, half-width, height, drop) in the prop's frame (mouth at
        /// the origin, facing -z, turned by <paramref name="yaw"/>): x, z, half-width and height smoothed (Catmull-Rom), the floor's
        /// drop below the mouth's ground straight between rows (the steepest row sets the steepest floor), rings about every 0.7 m. The land's height comes from <paramref name="ground"/>.
        /// </summary>
        public Hollow(string name, Vector2 at, float yaw, IList<float[]> plan, Func<float, float, float> ground)
        {
            Name = name;
            var turn = Quaternion.Euler(0, yaw, 0); float along = 0, mouth = ground(at.x, at.y); Vector3 last = Vector3.zero; bool first = true;
            Vector4 P(int i) { var r = plan[Mathf.Clamp(i, 0, plan.Count - 1)]; return new Vector4(r[0], r[1], r[2], r[3]); }
            float D(int i) { var r = plan[Mathf.Clamp(i, 0, plan.Count - 1)]; return r.Length > 4 ? r[4] : 0; }
            for (int seg = 0; seg + 1 < plan.Count; seg++)
            {
                Vector4 p0 = P(seg - 1), p1 = P(seg), p2 = P(seg + 1), p3 = P(seg + 2);
                int steps = Mathf.Max(1, Mathf.CeilToInt(new Vector2(p2.x - p1.x, p2.y - p1.y).magnitude / .7f));
                for (int k = 0; k < steps || (seg + 2 == plan.Count && k == steps); k++)
                {
                    float u = k / (float)steps; var q = CatmullRom(p0, p1, p2, p3, u);
                    var w = turn * new Vector3(q.x, 0, q.y); float x = at.x + w.x, z = at.y + w.z;
                    var c = new Vector3(x, mouth + Mathf.Lerp(D(seg), D(seg + 1), u), z);   // linear: an eased drop steepens mid-row past what feet and agents climb
                    if (!first) along += new Vector2(c.x - last.x, c.z - last.z).magnitude;
                    Centre.Add(c); Half.Add(Mathf.Max(.2f, q.z)); Height.Add(Mathf.Max(.3f, q.w)); Along.Add(along); Land.Add(ground(x, z));
                    low = Mathf.Min(low, c.y - 2); high = Mathf.Max(high, c.y + Mathf.Max(.3f, q.w) * 1.2f + 1);
                    last = c; first = false;
                }
            }
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < Centre.Count; i++)
            {
                float r = Half[i] + 6;
                minX = Mathf.Min(minX, Centre[i].x - r); maxX = Mathf.Max(maxX, Centre[i].x + r);
                minZ = Mathf.Min(minZ, Centre[i].z - r); maxZ = Mathf.Max(maxZ, Centre[i].z + r);
            }
            box = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            float sx0 = float.MaxValue, sz0 = float.MaxValue, sx1 = float.MinValue, sz1 = float.MinValue;
            for (int i = 0; i < Centre.Count; i++)
            {
                if (!NearSurface(i)) continue;
                surface.Add(i); float r = Half[i] + 6;
                sx0 = Mathf.Min(sx0, Centre[i].x - r); sx1 = Mathf.Max(sx1, Centre[i].x + r); sz0 = Mathf.Min(sz0, Centre[i].z - r); sz1 = Mathf.Max(sz1, Centre[i].z + r);
            }
            surfaceBox = surface.Count > 0 ? Rect.MinMaxRect(sx0, sz0, sx1, sz1) : Rect.zero;
            if (Centre.Count > 1)
            {
                int k = 1; while (k + 1 < Centre.Count && Along[k] < 1) k++;
                mouthAt = new Vector2(Centre[0].x, Centre[0].z); mouthOut = (mouthAt - new Vector2(Centre[k].x, Centre[k].z)).normalized;
            }
        }
        /// <summary>The old form: rows (x, z, half-width, height) with a level floor on the land.</summary>
        public Hollow(string name, Vector2 at, float yaw, IList<Vector4> plan, Func<float, float, float> ground)
            : this(name, at, yaw, ToRows(plan), ground) { }
        static List<float[]> ToRows(IList<Vector4> plan) { var rows = new List<float[]>(); foreach (var v in plan) rows.Add(new[] { v.x, v.y, v.z, v.w, 0f }); return rows; }
        static Vector4 CatmullRom(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3);
        }
        /// <summary>The top of the passage's rock at ring i (the roof and a little over): the land counts as covering it only above this.</summary>
        public float Roof(int i) { return Centre[i].y + Height[i] * 1.19f + .15f; }
        /// <summary>Whether ring i runs near the surface: less than three metres of rock between its roof and the land.</summary>
        public bool NearSurface(int i) { return Land[i] - Roof(i) < 3; }
        /// <summary>The ring nearest a ground point, and how far the point is from its centre on the ground plane.</summary>
        public int Nearest(Vector2 p, out float flat)
        {
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < Centre.Count; i++) { float dx = p.x - Centre[i].x, dz = p.y - Centre[i].z, d = dx * dx + dz * dz; if (d < bestD) { bestD = d; best = i; } }
            flat = Mathf.Sqrt(bestD); return best;
        }
        /// <summary>
        /// How much of a ground point the passage covers where it runs near the surface: 1 well inside the walls, easing to 0 at
        /// <paramref name="margin"/> past them (up to 6 m). Deep under the land it covers nothing: the land above is the land.
        /// </summary>
        public float Cover(Vector2 p, float margin)
        {
            if (surface.Count == 0 || !surfaceBox.Contains(p)) return 0;   // (the ground paint asks this for every pixel)
            float best = 0;
            foreach (int i in surface)
            {
                float d = Vector2.Distance(p, new Vector2(Centre[i].x, Centre[i].z));
                if (d > Half[i] + margin + .1f) continue;
                best = Mathf.Max(best, 1 - Mathf.SmoothStep(0, 1, (d - (Half[i] - .6f)) / (margin + .6f)));
            }
            return best;
        }
        /// <summary>Whether a point is inside the passage's volume (its walls taken at their widest, <paramref name="margin"/> added): the ground is cut there.</summary>
        public bool Inside(Vector3 p, float margin)
        {
            var q = new Vector2(p.x, p.z);
            if (Centre.Count == 0 || !box.Contains(q) || p.y < low - margin || p.y > high + margin) return false;
            if (Vector2.Dot(q - mouthAt, mouthOut) > 0) return false;   // out in front of the mouth: the land there is the way in
            for (int i = 0; i < Centre.Count; i++)
            {
                float d = Vector2.Distance(q, new Vector2(Centre[i].x, Centre[i].z));
                if (d < Half[i] * 1.2f + margin && p.y > Centre[i].y - .4f - margin && p.y < Roof(i) + margin) return true;
            }
            return false;
        }
        /// <summary>The passage floor under a ground point, if the point is within the passage's walls.</summary>
        public bool FloorAt(Vector2 p, out float y)
        {
            y = 0; if (Centre.Count == 0 || !box.Contains(p)) return false;
            int i = Nearest(p, out float d); if (d > Half[i]) return false;
            y = Centre[i].y; return true;
        }
        /// <summary>How deep a point is in the passage: 0 outside it, else from 0 at the mouth to 1 ten metres in.</summary>
        public float Depth(Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            if (Centre.Count == 0 || !box.Contains(q)) return 0;
            int i = Nearest(q, out float d);
            if (d > Half[i] + .3f || p.y > Roof(i) + .5f || p.y < Centre[i].y - 2) return 0;
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.2f, 10, Along[i]));
        }
        public static float DepthAt(Vector3 p) { float best = 0; foreach (var h in All) best = Mathf.Max(best, h.Depth(p)); return best; }
        public static float CoverAt(Vector2 p, float margin) { float best = 0; foreach (var h in All) best = Mathf.Max(best, h.Cover(p, margin)); return best; }
        public static bool InsideAny(Vector3 p, float margin) { foreach (var h in All) if (h.Inside(p, margin)) return true; return false; }
        /// <summary>The floor of whichever passage a point stands in (<paramref name="y"/> unchanged when none).</summary>
        public static bool FloorUnder(Vector2 p, ref float y) { foreach (var h in All) if (h.FloorAt(p, out float f)) { y = f; return true; } return false; }
        /// <summary>The world point <paramref name="along"/> metres in from the mouth, on the floor, <paramref name="aside"/> metres to the right.</summary>
        public Vector3 At(float along, float aside = 0)
        {
            int i = 0; while (i + 1 < Along.Count && Along[i + 1] < along) i++;
            int j = Mathf.Min(i + 1, Centre.Count - 1);
            float f = j > i ? Mathf.InverseLerp(Along[i], Along[j], along) : 0;
            var c = Vector3.Lerp(Centre[i], Centre[j], f);
            var fwd = Centre[j] - Centre[i]; fwd.y = 0; if (fwd.sqrMagnitude < 1e-6f && i > 0) { fwd = Centre[i] - Centre[i - 1]; fwd.y = 0; }
            fwd.Normalize(); return c + new Vector3(fwd.z, 0, -fwd.x) * aside;
        }
        public float Length { get { return Along.Count > 0 ? Along[Along.Count - 1] : 0; } }
        /// <summary>The passage's deepest floor.</summary>
        public float Bottom { get { return low + 2; } }
        /// <summary>How low anything may stand in the zone: 5 m under the land's floor (y 0), or under the deepest cave floor.</summary>
        public static float Lowest { get { float y = 0; foreach (var h in All) y = Mathf.Min(y, h.Bottom); return y - 5; } }
        /// <summary>Everything the passages reach, for the navmesh's bounds (they may run out under the hills past the zone's edge).</summary>
        public static Bounds Reach(Bounds zone)
        {
            foreach (var h in All) { zone.Encapsulate(new Vector3(h.box.xMin, h.low - 2, h.box.yMin)); zone.Encapsulate(new Vector3(h.box.xMax, h.high + 2, h.box.yMax)); }
            return zone;
        }
    }

    /// <summary>
    /// Firelight: a light (and a flame) wavering like fire. Only on lamp lights (ZoneBuilder.Glow): the clock sets their light each
    /// frame and this, running after it, only ever scales that.
    /// </summary>
    public sealed class Flicker : MonoBehaviour
    {
        Light glow; float seed;
        void Start() { glow = GetComponent<Light>(); seed = UnityEngine.Random.value * 100; }
        void LateUpdate()
        {
            if (glow == null) return;
            float t = Time.time * 6.5f + seed;
            glow.intensity *= 1 + .22f * (Mathf.PerlinNoise(t, seed) - .5f) + .05f * Mathf.Sin(t * 2.9f);
        }
    }
}
