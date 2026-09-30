using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A walk-in cave's passage (a "cavern" prop, such as Crowsfoot Hollow): rings along its floor from the mouth inward, each a
    /// centre on the floor, a half-width and a height. Made before the ground is painted (ZoneBuilder.PrepareHollows), so the
    /// paint, the grass, the trees, the rain and the light can all ask whether a point is inside and how deep:
    /// - the floor is painted bare earth and grows no grass; trees and bushes that land in it are dropped;
    /// - nothing falls inside (ZoneBuilder.UnderRoof, WorldWeather);
    /// - the light and the air darken with depth (WorldClock sets <see cref="CameraDepth"/>; ZonePost reads it).
    /// </summary>
    public sealed class Hollow
    {
        /// <summary>The zone's caves (cleared and refilled when a zone is built).</summary>
        public static readonly List<Hollow> All = new List<Hollow>();
        /// <summary>How deep the camera is in a cave this frame: 0 outside, 1 from about ten metres in (set by WorldClock).</summary>
        public static float CameraDepth;
        public readonly string Name;
        /// <summary>Ring centres on the floor (world), half-widths, heights, and the distance in from the mouth along the floor.</summary>
        public readonly List<Vector3> Centre = new List<Vector3>();
        public readonly List<float> Half = new List<float>(), Height = new List<float>(), Along = new List<float>();
        Rect box;

        /// <summary>
        /// The passage from its prop: <paramref name="plan"/> holds control points (x, z, half-width, height) in the prop's frame
        /// (mouth at the origin, facing -z, turned by <paramref name="yaw"/>), smoothed (Catmull-Rom) into rings about every 0.7 m.
        /// Floor heights come from <paramref name="ground"/>.
        /// </summary>
        public Hollow(string name, Vector2 at, float yaw, IList<Vector4> plan, Func<float, float, float> ground)
        {
            Name = name;
            var turn = Quaternion.Euler(0, yaw, 0); float along = 0; Vector3 last = Vector3.zero; bool first = true;
            for (int seg = 0; seg + 1 < plan.Count; seg++)
            {
                Vector4 p0 = plan[Mathf.Max(0, seg - 1)], p1 = plan[seg], p2 = plan[seg + 1], p3 = plan[Mathf.Min(plan.Count - 1, seg + 2)];
                int steps = Mathf.Max(1, Mathf.CeilToInt(new Vector2(p2.x - p1.x, p2.y - p1.y).magnitude / .7f));
                for (int k = 0; k < steps || (seg + 2 == plan.Count && k == steps); k++)
                {
                    float u = k / (float)steps; var q = CatmullRom(p0, p1, p2, p3, u);
                    var w = turn * new Vector3(q.x, 0, q.y); float x = at.x + w.x, z = at.y + w.z;
                    var c = new Vector3(x, ground(x, z), z);
                    if (!first) along += new Vector2(c.x - last.x, c.z - last.z).magnitude;
                    Centre.Add(c); Half.Add(Mathf.Max(.2f, q.z)); Height.Add(Mathf.Max(.3f, q.w)); Along.Add(along);
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
        }
        static Vector4 CatmullRom(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3);
        }
        /// <summary>The ring nearest a ground point, and how far the point is from its centre on the ground plane.</summary>
        public int Nearest(Vector2 p, out float flat)
        {
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < Centre.Count; i++) { float dx = p.x - Centre[i].x, dz = p.y - Centre[i].z, d = dx * dx + dz * dz; if (d < bestD) { bestD = d; best = i; } }
            flat = Mathf.Sqrt(bestD); return best;
        }
        /// <summary>How much of a ground point the passage covers: 1 well inside the walls, easing to 0 at <paramref name="margin"/> past them (up to 6 m).</summary>
        public float Cover(Vector2 p, float margin)
        {
            if (Centre.Count == 0 || !box.Contains(p)) return 0;
            int i = Nearest(p, out float d);
            return 1 - Mathf.SmoothStep(0, 1, (d - (Half[i] - .6f)) / (margin + .6f));
        }
        /// <summary>How deep a point is in the passage: 0 outside it (or above its roof), else from 0 at the mouth to 1 ten metres in.</summary>
        public float Depth(Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            if (Centre.Count == 0 || !box.Contains(q)) return 0;
            int i = Nearest(q, out float d);
            if (d > Half[i] + .3f || p.y > Centre[i].y + Height[i] + .5f || p.y < Centre[i].y - 2) return 0;
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.2f, 10, Along[i]));
        }
        public static float DepthAt(Vector3 p) { float best = 0; foreach (var h in All) best = Mathf.Max(best, h.Depth(p)); return best; }
        public static float CoverAt(Vector2 p, float margin) { float best = 0; foreach (var h in All) best = Mathf.Max(best, h.Cover(p, margin)); return best; }
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
    }

    /// <summary>Firelight: a light (and a flame) wavering like fire. Runs after the clock has set this frame's lamp light.</summary>
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
