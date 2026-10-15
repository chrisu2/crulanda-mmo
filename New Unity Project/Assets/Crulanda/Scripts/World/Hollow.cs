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
        Rect box, surfaceBox; float low = float.MaxValue, high = float.MinValue, widest;
        Vector2 mouthAt, mouthOut;   // the mouth ring's centre, and the level way out of the mouth: nothing in front of it is inside
        readonly List<int> surface = new List<int>();   // the rings near the surface: only they can cover the land

        /// <summary>
        /// The passage from its prop: <paramref name="plan"/> rows are (x, z, half-width, height, drop) in the prop's frame (mouth at
        /// the origin, facing -z, turned by <paramref name="yaw"/>): x, z, half-width and height smoothed (Catmull-Rom), the floor's
        /// drop below the mouth's ground straight between rows (the steepest row sets the steepest floor), rings about every 0.7 m. The land's height comes from <paramref name="ground"/>.
        /// </summary>
        public Hollow(string name, Vector2 at, float yaw, IList<float[]> plan, Func<float, float, float> ground, float? mouthY = null)
        {
            Name = name;
            var turn = Quaternion.Euler(0, yaw, 0); float along = 0, mouth = mouthY ?? ground(at.x, at.y); Vector3 last = Vector3.zero; bool first = true;
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
                    low = Mathf.Min(low, c.y - 2); high = Mathf.Max(high, c.y + Mathf.Max(.3f, q.w) * 1.2f + 1); widest = Mathf.Max(widest, Mathf.Max(.2f, q.z));
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
            // The rings by 16 m cells first (the Adit rework's caves run to some 3 km, and the walls ask this of every face of every cave):
            // the same ring as the plain scan below gives, ties to the lowest index as it does; the plain scan when none is near enough.
            if (!gridMade) MakeGrid();
            if (gridStart != null)
            {
                int cx = Mathf.FloorToInt((p.x - gridX0) / GridCell), cz = Mathf.FloorToInt((p.y - gridZ0) / GridCell);
                if (cx >= -GridReach && cx < gridW + GridReach && cz >= -GridReach && cz < gridH + GridReach)
                {
                    int found = -1; float foundD = float.MaxValue;
                    for (int r = 1; r <= GridReach; r++)
                    {
                        // The block of cells within r of p's cell (all nine at r = 1, then only the new ring of cells round them): a ring in no
                        // cell of it is more than r cells away, so one found nearer than that is the nearest of all.
                        for (int gz = cz - r; gz <= cz + r; gz++)
                        {
                            if (gz < 0 || gz >= gridH) continue;
                            for (int gx = cx - r; gx <= cx + r; gx++)
                            {
                                if (gx < 0 || gx >= gridW || (r > 1 && Mathf.Max(Mathf.Abs(gx - cx), Mathf.Abs(gz - cz)) < r)) continue;
                                int cell = gz * gridW + gx;
                                for (int q = gridStart[cell]; q < gridStart[cell + 1]; q++)
                                {
                                    int i = gridItems[q]; float dx = p.x - Centre[i].x, dz = p.y - Centre[i].z, d = dx * dx + dz * dz;
                                    if (d < foundD || (d == foundD && i < found)) { foundD = d; found = i; }
                                }
                            }
                        }
                        float sure = r * GridCell - .05f;
                        if (found >= 0 && foundD < sure * sure) { flat = Mathf.Sqrt(foundD); return found; }
                    }
                }
            }
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < Centre.Count; i++) { float dx = p.x - Centre[i].x, dz = p.y - Centre[i].z, d = dx * dx + dz * dz; if (d < bestD) { bestD = d; best = i; } }
            flat = Mathf.Sqrt(bestD); return best;
        }
        const float GridCell = 16; const int GridReach = 4;
        bool gridMade; int[] gridStart, gridItems; int gridW, gridH; float gridX0, gridZ0;
        /// <summary>The rings filed by 16 m cells, each cell's in ring order (made at the first <see cref="Nearest"/>; a short cave keeps the plain scan).</summary>
        void MakeGrid()
        {
            gridMade = true; int n = Centre.Count; if (n < 64) return;
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (var c in Centre) { x0 = Mathf.Min(x0, c.x); z0 = Mathf.Min(z0, c.z); x1 = Mathf.Max(x1, c.x); z1 = Mathf.Max(z1, c.z); }
            gridX0 = x0; gridZ0 = z0; gridW = Mathf.FloorToInt((x1 - x0) / GridCell) + 1; gridH = Mathf.FloorToInt((z1 - z0) / GridCell) + 1;
            if ((long)gridW * gridH > 1 << 20) return;   // (never in a zone's caves)
            var cellOf = new int[n]; var count = new int[gridW * gridH + 1];
            for (int i = 0; i < n; i++)
            {
                int gx = Mathf.Clamp(Mathf.FloorToInt((Centre[i].x - gridX0) / GridCell), 0, gridW - 1), gz = Mathf.Clamp(Mathf.FloorToInt((Centre[i].z - gridZ0) / GridCell), 0, gridH - 1);
                cellOf[i] = gz * gridW + gx; count[cellOf[i] + 1]++;
            }
            for (int k = 1; k < count.Length; k++) count[k] += count[k - 1];
            var start = (int[])count.Clone(); var items = new int[n];
            for (int i = 0; i < n; i++) items[start[cellOf[i]]++] = i;   // ascending within each cell
            gridStart = count; gridItems = items;
        }
        /// <summary>
        /// A ground point in the passage's own terms (<see cref="At"/> turned round): how far in from the mouth, along the nearest ring's
        /// way, and how far to the right of the middle.
        /// </summary>
        public void Local(Vector2 p, out float along, out float aside)
        {
            along = aside = 0; if (Centre.Count == 0) return;
            int i = Nearest(p, out _); along = Along[i]; if (Centre.Count < 2) return;
            int j = i + 1 < Centre.Count ? i + 1 : i - 1;
            var fwd = new Vector2(Centre[j].x - Centre[i].x, Centre[j].z - Centre[i].z); if (j < i) fwd = -fwd;
            if (fwd.sqrMagnitude < 1e-8f) return;
            fwd.Normalize(); var d = p - new Vector2(Centre[i].x, Centre[i].z);
            along = Along[i] + Vector2.Dot(d, fwd); aside = d.x * fwd.y - d.y * fwd.x;
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
        /// <summary>The passage floor under a ground point, if the point is within the passage's walls (and the ring there has a floor of its
        /// own: <see cref="Floored"/>).</summary>
        public bool FloorAt(Vector2 p, out float y)
        {
            y = 0; if (Centre.Count == 0 || !box.Contains(p)) return false;
            int i = Nearest(p, out float d); if (d > Half[i] || !Floored(i)) return false;
            y = Centre[i].y; return true;
        }
        bool[] floored;
        /// <summary>
        /// Whether ring i has a floor of its own: not where it runs through another cave's open air (a branch's first metres in its parent,
        /// a joining cave's last metres out in the cave it joins), where the floor is the other's, or nothing (a lip over a hall: the hall's
        /// floor far below). Every ring until <see cref="ComputeFloored"/> has run.
        /// </summary>
        public bool Floored(int i) { return floored == null || i < 0 || i >= floored.Length || floored[i]; }
        /// <summary>Works out <see cref="Floored"/> once all the zone's caves are made (ZoneBuilder.PrepareHollows): the test the floor's mesh
        /// makes to leave a stretch out (ZoneBuilder.Cavern), at each ring.</summary>
        public void ComputeFloored()
        {
            floored = new bool[Centre.Count];
            for (int i = 0; i < Centre.Count; i++) floored[i] = !FloorGivesWay(this, Centre[i] + Vector3.up * .3f);
        }
        /// <summary>
        /// Whether a point just over cave <paramref name="h"/>'s floor is another cave's open air, so that h's floor gives way there to the
        /// other's (ZoneBuilder.Cavern leaves that stretch of h's floor out): as <see cref="OpenInOther"/> at .8 of the way to the walls, but a
        /// branch of h, or a cave that joins h, does not take h's floor where its own floor is out in h's air (its mouth, its end): there
        /// it is h's floor that holds. (Without that, a joining cave's end reaching h's middle left neither floor there.)
        /// </summary>
        public static bool FloorGivesWay(Hollow h, Vector3 p)
        {
            foreach (var o in All)
            {
                if (o == h || !o.Open(p, .8f)) continue;
                if ((o.Parent == h || o.Join == h) && o.Centre.Count > 0)
                {
                    int j = o.Nearest(new Vector2(p.x, p.z), out _);
                    if (h.Open(o.Centre[j] + Vector3.up * .3f, .8f)) continue;   // the branch's own floor is out in h's air here: h's floor holds
                }
                return true;
            }
            return false;
        }
        /// <summary>Every cave floor under a ground point, one for each cave whose walls it is within and which has a floor of its own there
        /// (<paramref name="into"/> is cleared first). Caves may lie one over another where one opens high in the other's wall.</summary>
        public static void FloorsUnder(Vector2 p, List<float> into)
        {
            into.Clear();
            foreach (var h in All) if (h.FloorAt(p, out float f)) into.Add(f);
        }
        /// <summary>
        /// The passage floor under a ground point, if the point is within the passage's walls, eased between the nearest ring and
        /// the next one on its side (along the line between their centres), as the floor's mesh runs: on a sloping stretch a thing
        /// set on it neither floats nor sinks, where <see cref="FloorAt"/> gives the nearest ring's level.
        /// </summary>
        public bool FloorSmooth(Vector2 p, out float y)
        {
            y = 0; if (Centre.Count == 0 || !box.Contains(p)) return false;
            int i = Nearest(p, out float d); if (d > Half[i] || !Floored(i)) return false;
            y = Centre[i].y; if (Centre.Count < 2) return true;
            var a = new Vector2(Centre[i].x, Centre[i].z);
            int j = i == 0 ? 1 : i == Centre.Count - 1 ? i - 1 : Vector2.Dot(p - a, new Vector2(Centre[i + 1].x, Centre[i + 1].z) - a) >= 0 ? i + 1 : i - 1;
            var b = new Vector2(Centre[j].x, Centre[j].z);
            float f = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / Mathf.Max(1e-6f, (b - a).sqrMagnitude));
            y = Mathf.Lerp(Centre[i].y, Centre[j].y, f); return true;
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
        /// <summary>The floor of whichever passage a point stands in, eased between its rings (<see cref="FloorSmooth"/>); <paramref name="y"/> unchanged when none.</summary>
        public static bool FloorSmoothUnder(Vector2 p, ref float y) { foreach (var h in All) if (h.FloorSmooth(p, out float f)) { y = f; return true; } return false; }
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
        /// <summary>The cave this one opens out of (null for one with its own mouth on the land), and how far into it.</summary>
        public Hollow Parent; public float ParentAlong;
        /// <summary>How far up its parent's wall its mouth opens (ZoneProp.rise; 0: on the parent's floor).</summary>
        public float MouthRise;
        /// <summary>The cave its far end opens into (ZoneProp.joins; null: it closes), how far into that cave, and how far its end stands
        /// over that cave's floor (about 0: a level way through; more: a lip over it).</summary>
        public Hollow Join; public float JoinAlong, JoinRise;
        /// <summary>How the Sealed Adit dresses it (ZoneProp.style; empty: by its name).</summary>
        public string Style;
        /// <summary>Its mouth opens high in its parent's wall: their walls are opened by the whole of each face there (ZoneBuilder.Cavern).</summary>
        public bool MouthRaised { get { return Parent != null && MouthRise > .5f; } }
        /// <summary>Its end opens high over the cave it joins (a lip): as <see cref="MouthRaised"/>.</summary>
        public bool JoinRaised { get { return Join != null && JoinRise > .5f; } }
        /// <summary>
        /// Whether a point is in the open air of the passage, well inside its walls and under its roof (<paramref name="shrink"/> of the
        /// way to them): another cave's wall standing there is cut away (ZoneBuilder.Cavern), so one passage opens into the next. Not
        /// in front of the mouth or past the end.
        /// </summary>
        public bool Open(Vector3 p, float shrink = .86f)
        {
            var q = new Vector2(p.x, p.z); if (Centre.Count < 2 || !box.Contains(q)) return false;
            int i = Nearest(q, out _); int j = i + 1 < Centre.Count ? i + 1 : i - 1;
            var fwd = new Vector2(Centre[j].x - Centre[i].x, Centre[j].z - Centre[i].z); if (j < i) fwd = -fwd;
            if (fwd.sqrMagnitude < 1e-8f) return false; fwd.Normalize();
            var d = q - new Vector2(Centre[i].x, Centre[i].z);
            if (Mathf.Abs(Vector2.Dot(d, fwd)) > .75f) return false;
            float u = Mathf.Abs(d.x * fwd.y - d.y * fwd.x) / Half[i]; if (u >= shrink) return false;
            float up = p.y - Centre[i].y; if (up < -.6f) return false;
            return up < Height[i] * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Pow(u, 8f / 3)), .425f) * shrink;
        }
        /// <summary>
        /// Whether any of a triangle is in the passage's open air (<see cref="Open"/> at <paramref name="shrink"/>), sampled about every half
        /// metre over it (2026-10-15, S7): a wall's or a knoll's face metres across can straddle a narrow passage that runs through it, its
        /// corners above and below the passage and its middle to one side, so no corner or middle is in the air and the face stood across
        /// the passage by its wall (the fixture's loop, where it comes through the hall's knoll to its lip: AditCaveBuildTests "the loop
        /// opens over the hall"). Only a face near the passage is sampled.
        /// </summary>
        public bool OpenOnFace(Vector3 a, Vector3 b, Vector3 d, float shrink = .86f)
        {
            if (Centre.Count < 2) return false;
            float reach = Mathf.Max(Vector3.Distance(a, b), Mathf.Max(Vector3.Distance(b, d), Vector3.Distance(d, a)));
            var m = (a + b + d) / 3; var q = new Vector2(m.x, m.z);
            if (q.x < box.xMin - reach || q.x > box.xMax + reach || q.y < box.yMin - reach || q.y > box.yMax + reach) return false;
            if (m.y < low - reach || m.y > high + reach) return false;
            Nearest(q, out float flat); if (flat > widest + reach + .5f) return false;
            int steps = Mathf.Clamp(Mathf.CeilToInt(reach / .5f), 2, 48);
            for (int u = 0; u <= steps; u++)
                for (int v = 0; u + v <= steps; v++)
                    if (Open(a + (b - a) * (u / (float)steps) + (d - a) * (v / (float)steps), shrink)) return true;
            return false;
        }
        /// <summary>Whether a point is open air in any cave but <paramref name="not"/>.</summary>
        public static bool OpenInOther(Hollow not, Vector3 p, float shrink = .86f) { foreach (var h in All) if (h != not && h.Open(p, shrink)) return true; return false; }
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
    /// frame and this, running after it, only ever scales that. A CandleFlame on the flame reads <see cref="Factor"/> so the flame
    /// wavers with its light (2026-10-15, the candle pack).
    /// </summary>
    public sealed class Flicker : MonoBehaviour
    {
        Light glow; float seed;
        /// <summary>How the light stands now against its set intensity (about 0.85 to 1.15).</summary>
        public float Factor { get; private set; } = 1;
        void Start() { glow = GetComponent<Light>(); seed = UnityEngine.Random.value * 100; }
        void LateUpdate()
        {
            if (glow == null) return;
            Factor = Curve(Time.time, seed);
            glow.intensity *= Factor;
        }
        /// <summary>The waver at a time, for a seed: a slow Perlin wander with a quicker tremble on it.</summary>
        public static float Curve(float time, float seed)
        {
            float t = time * 6.5f + seed;
            return 1 + .22f * (Mathf.PerlinNoise(t, seed) - .5f) + .05f * Mathf.Sin(t * 2.9f);
        }
    }
}
