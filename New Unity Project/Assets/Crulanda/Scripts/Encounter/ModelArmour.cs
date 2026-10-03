using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Armour that follows the form (playtest note 12, part two: "armor is way too blocky. needs to feel flowing"). The armour's
    /// designs (ActorVisual.GearArmor) were drawn round the old figure's body; on a model each piece is warped from that body's
    /// surface onto the model's and skinned to the model's own bones, so it hugs the real torso and limbs and bends and sways
    /// with them.
    ///
    /// The warp is a cage, one per frame the armour hangs on. The old body is known exactly (SmoothBody's torso cross-sections,
    /// arm and leg radii, the mitten hand, the boot's foot); the model's body is its clothes in the bind pose, as a convex outline
    /// per height (so armour never dips into a hollow, and below the hips the legs make one round outline for a skirt). A point
    /// of armour keeps its angle round the body and its distance from it as a share of the old body's radius there: a mail
    /// shirt that stood 8% outside the old torso stands 8% outside the model's shirt. The hand and the foot, below the wrist and
    /// ankle, are boxes mapped axis by axis. Then each vertex takes its skin weights from the clothes nearest it (four nearest,
    /// by distance), so it moves exactly as the cloth under it; a skirt's hem, well clear of the legs, keeps some of the hips'.
    /// The head's pieces (helmets, hoods) stay rigid on the head's frame.
    /// </summary>
    public static class ModelArmour
    {
        /// <summary>Which frame a piece hangs on, and so which cage warps it.</summary>
        public enum Frame { Body, ArmL, ArmR, LegL, LegR }
        internal enum Part : byte { Torso, Head, ArmL, ArmR, LegL, LegR }

        /// <summary>A model's clothes (and head) in its bind pose, in the model root's space: points, normals, skin weights on one bone
        /// list, the bones' bind matrices (root-relative), and which part of the body each point is.</summary>
        public sealed class Cloud
        {
            public Vector3[] p; public BoneWeight[] w; internal Part[] part;
            public string[] bones; public Matrix4x4[] bind, bindInverse;
            internal readonly Dictionary<string, Cage> cages = new Dictionary<string, Cage>();
            internal readonly Dictionary<(Mesh, string), Mesh> fitted = new Dictionary<(Mesh, string), Mesh>();
        }
        static readonly Dictionary<string, Cloud> clouds = new Dictionary<string, Cloud>();

        static Part PartOf(string bone)
        {
            string b = bone.ToLowerInvariant(); bool l = b.EndsWith("_l"), r = b.EndsWith("_r");
            if (b == "head" || b.StartsWith("neck")) return Part.Head;
            if (l || r)
            {
                if (b.StartsWith("thigh") || b.StartsWith("calf") || b.StartsWith("foot") || b.StartsWith("ball")) return l ? Part.LegL : Part.LegR;
                if (!b.StartsWith("clavicle")) return l ? Part.ArmL : Part.ArmR;   // upper arm, forearm, hand, fingers
            }
            return Part.Torso;   // pelvis, spine, clavicles, root
        }
        /// <summary>What a piece of the clothes is, for the parts its points belong to.</summary>
        public enum Piece { Torso, Arms, Legs, Head }
        /// <summary>The cloud of a model of this kind (built once per kind, from a figure in its bind pose).</summary>
        public static Cloud CloudOf(string kind, Transform root, IList<SkinnedMeshRenderer> pieces, IList<Piece> what)
        {
            if (clouds.TryGetValue(kind, out var c)) return c;
            c = new Cloud();
            var canon = new List<Transform>(); var index = new Dictionary<string, int>();
            foreach (var smr in pieces) if (smr != null) foreach (var b in smr.bones) if (b != null && !index.ContainsKey(b.name)) { index[b.name] = canon.Count; canon.Add(b); }
            var toRoot = root.worldToLocalMatrix;
            c.bones = canon.ConvertAll(b => b.name).ToArray();
            c.bind = canon.ConvertAll(b => toRoot * b.localToWorldMatrix).ToArray();
            c.bindInverse = System.Array.ConvertAll(c.bind, m => m.inverse);
            var P = new List<Vector3>(); var W = new List<BoneWeight>(); var parts = new List<Part>();
            for (int pi = 0; pi < pieces.Count; pi++)
            {
                var smr = pieces[pi]; var mesh = smr != null ? smr.sharedMesh : null; if (mesh == null || !mesh.isReadable) continue;
                var v = mesh.vertices; var bw = mesh.boneWeights; var bp = mesh.bindposes; var bones = smr.bones;
                if (bw.Length != v.Length || bp.Length != bones.Length) continue;
                var skin = new Matrix4x4[bones.Length]; var map = new int[bones.Length];
                for (int i = 0; i < bones.Length; i++) { skin[i] = toRoot * bones[i].localToWorldMatrix * bp[i]; map[i] = bones[i] != null && index.TryGetValue(bones[i].name, out int k) ? k : 0; }
                // Only the triangles drawn count (the base character's skin is cut to the head).
                var used = new bool[v.Length]; foreach (int t in mesh.triangles) used[t] = true;
                for (int i = 0; i < v.Length; i++)
                {
                    if (!used[i]) continue;
                    var b = bw[i];
                    var p = skin[b.boneIndex0].MultiplyPoint3x4(v[i]) * b.weight0 + skin[b.boneIndex1].MultiplyPoint3x4(v[i]) * b.weight1
                          + skin[b.boneIndex2].MultiplyPoint3x4(v[i]) * b.weight2 + skin[b.boneIndex3].MultiplyPoint3x4(v[i]) * b.weight3;
                    P.Add(p);
                    W.Add(new BoneWeight { boneIndex0 = map[b.boneIndex0], weight0 = b.weight0, boneIndex1 = map[b.boneIndex1], weight1 = b.weight1,
                        boneIndex2 = map[b.boneIndex2], weight2 = b.weight2, boneIndex3 = map[b.boneIndex3], weight3 = b.weight3 });
                    int dom = b.weight0 >= b.weight1 && b.weight0 >= b.weight2 && b.weight0 >= b.weight3 ? b.boneIndex0 : b.weight1 >= b.weight2 && b.weight1 >= b.weight3 ? b.boneIndex1 : b.weight2 >= b.weight3 ? b.boneIndex2 : b.boneIndex3;
                    var byBone = PartOf(bones[dom] != null ? bones[dom].name : ""); bool left = p.x > 0;   // the model's left is +x in its root
                    switch (what[pi])
                    {
                        case Piece.Arms: parts.Add(byBone == Part.ArmL || byBone == Part.ArmR ? byBone : left ? Part.ArmL : Part.ArmR); break;
                        case Piece.Legs: parts.Add(byBone == Part.LegL || byBone == Part.LegR ? byBone : left ? Part.LegL : Part.LegR); break;
                        case Piece.Head: parts.Add(Part.Head); break;
                        default: parts.Add(byBone == Part.ArmL || byBone == Part.ArmR ? byBone : byBone); break;
                    }
                }
            }
            c.p = P.ToArray(); c.w = W.ToArray(); c.part = parts.ToArray();
            clouds[kind] = c; return c;
        }

        // ------------------------------------------------------------------------------------------------ the old body
        // SmoothBody's torso: y, half-width, half-depth, centre z, squareness (body space).
        static readonly float[,] OldTorso = {
            { -.14f, .17f, .112f, -.004f, 2.4f }, { -.1f, .2f, .128f, -.005f, 2.5f }, { -.02f, .21f, .134f, 0, 2.6f }, { .033f, .2f, .13f, .004f, 2.5f },
            { .098f, .2f, .132f, .006f, 2.5f }, { .16f, .182f, .122f, .004f, 2.4f }, { .25f, .195f, .134f, .012f, 2.5f }, { .34f, .222f, .152f, .024f, 2.6f },
            { .43f, .242f, .158f, .024f, 2.7f }, { .5f, .252f, .146f, .01f, 2.9f }, { .553f, .238f, .124f, -.006f, 3.1f }, { .595f, .172f, .1f, -.012f, 2.6f },
            { .625f, .1f, .078f, -.012f, 2.2f }, { .645f, .066f, .066f, -.008f, 2 } };
        // The arm, down from its shoulder pivot (y in the pivot's space) with its radius; the leg, down from its hip pivot.
        static readonly float[] ArmY = { .08f, .065f, .035f, -.03f, -.11f, -.2f, -.285f, -.36f, -.44f, -.51f, -.53f, -.565f };
        static readonly float[] ArmR = { .03f, .062f, .082f, .08f, .066f, .06f, .055f, .062f, .058f, .048f, .043f, .04f };
        static readonly float[] LegY = { .11f, .04f, -.04f, -.17f, -.3f, -.43f, -.52f, -.56f, -.62f, -.72f, -.82f };
        static readonly float[] LegR = { .098f, .118f, .114f, .1f, .086f, .068f, .074f, .073f, .07f, .056f, .05f };
        /// <summary>The old wrist and ankle (pivot space): below them the hand and the foot, mapped as boxes.</summary>
        const float Wrist = -.565f, Ankle = -.8f, Blend = .03f;
        static readonly Bounds OldHand = new Bounds(new Vector3(0, -.645f, .025f), new Vector3(.108f, .16f, .09f));
        static readonly Bounds OldFoot = new Bounds(new Vector3(0, -.85f, .062f), new Vector3(.122f, .1f, .285f));

        static float Lerp(float[] ys, float[] vs, float y)
        {
            if (y >= ys[0]) return vs[0]; int n = ys.Length; if (y <= ys[n - 1]) return vs[n - 1];
            for (int i = 0; i + 1 < n; i++) if (y <= ys[i] && y >= ys[i + 1]) return Mathf.Lerp(vs[i], vs[i + 1], (ys[i] - y) / (ys[i] - ys[i + 1]));
            return vs[n - 1];
        }
        static float LegRadius(float bodyY) { return Lerp(LegY, LegR, bodyY + .08f); }
        /// <summary>The old body's radius from its axis along angle a (0 forward, + toward +x) at body height y; and that axis's z.</summary>
        static float OldBodyRadius(float y, float a, out float zc)
        {
            float hw, hd, p;
            if (y < -.1f)
            {
                // Below the belt the legs part: the outline round both (the hips' ring above, the two legs below).
                float lr = LegRadius(y), u = Mathf.InverseLerp(-.1f, -.2f, y);
                hw = Mathf.Lerp(.2f, .12f + lr, u); hd = Mathf.Lerp(.128f, lr * 1.05f, u); p = 2.4f; zc = 0;
            }
            else
            {
                int n = OldTorso.GetLength(0), i = 0;
                while (i + 1 < n - 1 && y > OldTorso[i + 1, 0]) i++;
                float t = Mathf.Clamp01((y - OldTorso[i, 0]) / Mathf.Max(.0001f, OldTorso[i + 1, 0] - OldTorso[i, 0]));
                hw = Mathf.Lerp(OldTorso[i, 1], OldTorso[i + 1, 1], t); hd = Mathf.Lerp(OldTorso[i, 2], OldTorso[i + 1, 2], t);
                zc = Mathf.Lerp(OldTorso[i, 3], OldTorso[i + 1, 3], t); p = Mathf.Lerp(OldTorso[i, 4], OldTorso[i + 1, 4], t);
            }
            float s = Mathf.Abs(Mathf.Sin(a)), c = Mathf.Abs(Mathf.Cos(a));
            return 1 / Mathf.Pow(Mathf.Pow(s / hw, p) + Mathf.Pow(c / hd, p), 1 / p);
        }

        // ------------------------------------------------------------------------------------------------ the model's body
        const int NA = 48; const float DY = .015f;
        /// <summary>The model's outline in a frame's space: per height band, its radius at each angle round the axis; for a limb,
        /// the box of its hand or foot.</summary>
        internal sealed class Cage
        {
            public Frame frame; public float y0; public int ny; public float[,] r; public Bounds box; public bool hasBox;
            public float R(float y, float a)
            {
                float fy = Mathf.Clamp((y - y0) / DY, 0, ny - 1.001f); int iy = (int)fy; float ty = fy - iy;
                float fa = Mathf.Repeat(a / (2 * Mathf.PI), 1) * NA; int ia = (int)fa % NA; float ta = fa - (int)fa; int ib = (ia + 1) % NA;
                float r0 = Mathf.Lerp(r[iy, ia], r[iy, ib], ta), r1 = Mathf.Lerp(r[iy + 1, ia], r[iy + 1, ib], ta);
                return Mathf.Lerp(r0, r1, ty);
            }
        }
        static bool Limb(Frame f) { return f != Frame.Body; }
        static bool Arm(Frame f) { return f == Frame.ArmL || f == Frame.ArmR; }
        static Part PartFor(Frame f) { return f == Frame.ArmL ? Part.ArmL : f == Frame.ArmR ? Part.ArmR : f == Frame.LegL ? Part.LegL : f == Frame.LegR ? Part.LegR : Part.Torso; }
        static bool Takes(Frame f, Part p) { return f == Frame.Body ? p == Part.Torso || p == Part.Head || p == Part.LegL || p == Part.LegR : p == PartFor(f); }

        static Cage MakeCage(Cloud c, Frame f, Matrix4x4 rootToFrame)
        {
            var cage = new Cage { frame = f };
            float lo = f == Frame.Body ? -.75f : Arm(f) ? -.8f : -1.0f, hi = f == Frame.Body ? .75f : .15f;
            cage.y0 = lo; cage.ny = Mathf.CeilToInt((hi - lo) / DY) + 1;
            var slices = new List<Vector2>[cage.ny]; for (int i = 0; i < cage.ny; i++) slices[i] = new List<Vector2>();
            var boxPts = new List<Vector3>(); float under = Arm(f) ? Wrist : Ankle;
            for (int i = 0; i < c.p.Length; i++)
            {
                if (!Takes(f, c.part[i])) continue;
                var q = rootToFrame.MultiplyPoint3x4(c.p[i]);
                if (Limb(f) && q.y < under) boxPts.Add(q);
                int iy = Mathf.RoundToInt((q.y - lo) / DY); if (iy < 0 || iy >= cage.ny) continue;
                float zc = 0; if (f == Frame.Body) OldBodyRadius(q.y, 0, out zc);
                slices[iy].Add(new Vector2(q.x, q.z - zc));
            }
            cage.r = new float[cage.ny, NA]; var have = new bool[cage.ny];
            for (int iy = 0; iy < cage.ny; iy++)
            {
                // A band's points with its neighbours' (a thin band can miss a side), as one convex outline.
                var pts = new List<Vector2>(slices[iy]); if (iy > 0) pts.AddRange(slices[iy - 1]); if (iy + 1 < cage.ny) pts.AddRange(slices[iy + 1]);
                if (pts.Count < 6) continue;
                var hull = Hull(pts); if (hull.Count < 3) continue;
                have[iy] = true;
                for (int ia = 0; ia < NA; ia++) cage.r[iy, ia] = Ray(hull, 2 * Mathf.PI * ia / NA);
            }
            // Bands with nothing take the nearest band that has something (above and below the clothes the outline holds).
            for (int iy = 0; iy < cage.ny; iy++)
            {
                if (have[iy]) continue;
                int up = iy, dn = iy; while (up < cage.ny && !have[up]) up++; while (dn >= 0 && !have[dn]) dn--;
                int from = up < cage.ny && (dn < 0 || up - iy <= iy - dn) ? up : dn; if (from < 0 || from >= cage.ny) continue;
                for (int ia = 0; ia < NA; ia++) cage.r[iy, ia] = cage.r[from, ia];
            }
            if (boxPts.Count > 8)
            {
                var b = new Bounds(boxPts[0], Vector3.zero); foreach (var p in boxPts) b.Encapsulate(p);
                cage.box = b; cage.hasBox = true;
            }
            return cage;
        }
        /// <summary>The convex hull of points (monotone chain), counter-clockwise.</summary>
        internal static List<Vector2> Hull(List<Vector2> pts)
        {
            pts.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new List<Vector2>();
            float Cross(Vector2 o, Vector2 a, Vector2 b) { return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x); }
            foreach (var p in pts) { while (h.Count >= 2 && Cross(h[h.Count - 2], h[h.Count - 1], p) <= 0) h.RemoveAt(h.Count - 1); h.Add(p); }
            int lower = h.Count + 1;
            for (int i = pts.Count - 2; i >= 0; i--) { var p = pts[i]; while (h.Count >= lower && Cross(h[h.Count - 2], h[h.Count - 1], pts[i]) <= 0) h.RemoveAt(h.Count - 1); h.Add(p); }
            if (h.Count > 1) h.RemoveAt(h.Count - 1);
            return h;
        }
        /// <summary>How far from the axis (the origin) the outline lies along angle a (0 toward +z, + toward +x).</summary>
        internal static float Ray(List<Vector2> hull, float a)
        {
            var d = new Vector2(Mathf.Sin(a), Mathf.Cos(a)); float best = 0;
            for (int i = 0; i < hull.Count; i++)
            {
                Vector2 p = hull[i], q = hull[(i + 1) % hull.Count], e = q - p;
                float den = d.x * e.y - d.y * e.x; if (Mathf.Abs(den) < 1e-7f) continue;
                float t = (p.x * e.y - p.y * e.x) / den, s = (p.x * d.y - p.y * d.x) / den;
                if (s >= -1e-4f && s <= 1.0001f && t > best) best = t;
            }
            if (best <= 0) { foreach (var p in hull) best = Mathf.Max(best, Vector2.Dot(p, d)); best = Mathf.Max(best, .01f); }   // the axis outside the outline: its far side
            return best;
        }

        // ------------------------------------------------------------------------------------------------ the warp
        /// <summary>A point of armour from the old body onto the model (frame space).</summary>
        static Vector3 Warp(Cage cage, Vector3 v)
        {
            if (cage.frame == Frame.Body)
            {
                float a = Mathf.Atan2(v.x, v.z - Z(v.y)), zc; float old = OldBodyRadius(v.y, a, out zc);
                float r = new Vector2(v.x, v.z - zc).magnitude, rn = Close(cage.R(v.y, a), r - old, BodyGap);
                return new Vector3(Mathf.Sin(a) * rn, v.y, zc + Mathf.Cos(a) * rn);
            }
            bool arm = Arm(cage.frame); float under = arm ? Wrist : Ankle;
            Vector3 tube = v;
            if (v.y > under - Blend || !cage.hasBox)
            {
                float a = Mathf.Atan2(v.x, v.z), r = new Vector2(v.x, v.z).magnitude;
                float old = arm ? Lerp(ArmY, ArmR, Mathf.Max(v.y, Wrist)) : Lerp(LegY, LegR, Mathf.Max(v.y, Ankle));
                float rn = Close(cage.R(Mathf.Max(v.y, under), a), r - old, LimbGap);
                tube = new Vector3(Mathf.Sin(a) * rn, v.y, Mathf.Cos(a) * rn);
                if (v.y >= under || !cage.hasBox) return tube;
            }
            // The hand or the foot: the old box onto the model's, axis by axis (the hand's flat side turns with it).
            var ob = arm ? OldHand : OldFoot; var nb = cage.box;
            Vector3 u = new Vector3((v.x - ob.min.x) / ob.size.x, (v.y - ob.min.y) / ob.size.y, (v.z - ob.min.z) / ob.size.z);
            var box = new Vector3(nb.min.x + u.x * nb.size.x, nb.min.y + u.y * nb.size.y, nb.min.z + u.z * nb.size.z);
            float w = Mathf.Clamp01((under - v.y) / Blend);
            return Vector3.Lerp(tube, box, w);
        }
        static float Z(float y) { float zc; OldBodyRadius(y, 0, out zc); return zc; }
        /// <summary>How much of its old distance from the body a piece keeps on a model (the old armour was drawn chunky, a
        /// hand's breadth off the body in places).</summary>
        const float BodyGap = .62f, LimbGap = .55f;
        /// <summary>The new radius: the model's outline plus the old distance off the body, cut by <paramref name="keep"/> when it
        /// stood clear (inside the old body, as a shell's inner wall can, it is kept as it was).</summary>
        static float Close(float outline, float gap, float keep) { return Mathf.Max(.004f, outline + (gap > 0 ? gap * keep : gap)); }

        // ------------------------------------------------------------------------------------------------ the fitted piece
        /// <summary>
        /// A piece of armour (its mesh in its frame's space) warped onto the model and skinned to its bones: the mesh in the model
        /// root's space with bone weights and bind poses for <see cref="Cloud.bones"/>. Made once per mesh, frame and model kind.
        /// </summary>
        public static Mesh Fit(Cloud c, Mesh src, Frame f, Matrix4x4 frameToRoot, string frameKey)
        {
            if (c.fitted.TryGetValue((src, frameKey), out var done) && done != null) return done;
            if (!c.cages.TryGetValue(frameKey, out var cage)) c.cages[frameKey] = cage = MakeCage(c, f, frameToRoot.inverse);
            var m = Object.Instantiate(src); m.name = src.name + " (fitted)";
            var v = m.vertices; var root = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++) root[i] = frameToRoot.MultiplyPoint3x4(Warp(cage, v[i]));
            m.vertices = root; m.RecalculateNormals(); if (m.uv != null && m.uv.Length == root.Length) m.RecalculateTangents(); m.RecalculateBounds();
            m.boneWeights = Weights(c, f, root); m.bindposes = c.bindInverse;
            c.fitted[(src, frameKey)] = m; return m;
        }
        /// <summary>Each vertex's weights from the four nearest points of the clothes it lies on (by inverse distance); a vertex well
        /// clear of them (a skirt's hem) leans toward the hips.</summary>
        static BoneWeight[] Weights(Cloud c, Frame f, Vector3[] pts)
        {
            var grid = new Dictionary<long, List<int>>(); const float cell = .04f;
            long Key(int x, int y, int z) { return ((long)(x + 512) << 20) ^ ((long)(y + 512) << 10) ^ (long)(z + 512); }
            for (int i = 0; i < c.p.Length; i++)
            {
                if (!Takes(f, c.part[i]) || f == Frame.Body && c.part[i] == Part.Head) continue;   // the body's armour does not turn with the head
                var q = c.p[i]; long k = Key(Mathf.FloorToInt(q.x / cell), Mathf.FloorToInt(q.y / cell), Mathf.FloorToInt(q.z / cell));
                if (!grid.TryGetValue(k, out var list)) grid[k] = list = new List<int>(); list.Add(i);
            }
            int pelvis = System.Array.IndexOf(c.bones, "pelvis");
            var res = new BoneWeight[pts.Length]; var near = new int[4]; var dist = new float[4]; var acc = new Dictionary<int, float>();
            for (int i = 0; i < pts.Length; i++)
            {
                var q = pts[i]; int cx = Mathf.FloorToInt(q.x / cell), cy = Mathf.FloorToInt(q.y / cell), cz = Mathf.FloorToInt(q.z / cell);
                int found = 0; for (int k = 0; k < 4; k++) { near[k] = -1; dist[k] = float.MaxValue; }
                for (int ring = 1; ring <= 6 && (found < 4 || ring <= 1); ring++)
                {
                    for (int x = -ring; x <= ring; x++) for (int y = -ring; y <= ring; y++) for (int z = -ring; z <= ring; z++)
                    {
                        if (ring > 1 && Mathf.Abs(x) < ring && Mathf.Abs(y) < ring && Mathf.Abs(z) < ring) continue;   // only the new shell of cells
                        if (!grid.TryGetValue(Key(cx + x, cy + y, cz + z), out var list)) continue;
                        foreach (int j in list)
                        {
                            float d = (c.p[j] - q).sqrMagnitude;
                            if (d >= dist[3]) continue;
                            int s = 3; while (s > 0 && dist[s - 1] > d) { dist[s] = dist[s - 1]; near[s] = near[s - 1]; s--; }
                            dist[s] = d; near[s] = j; found = Mathf.Min(4, found + 1);
                        }
                    }
                    if (found >= 4 && ring >= 2) break;
                }
                acc.Clear(); float total = 0;
                for (int k = 0; k < 4; k++)
                {
                    if (near[k] < 0) continue;
                    float wgt = 1 / (Mathf.Sqrt(dist[k]) + .005f); var b = c.w[near[k]];
                    void Add(int bone, float bw) { if (bw <= 0) return; acc.TryGetValue(bone, out float o); acc[bone] = o + bw * wgt; total += bw * wgt; }
                    Add(b.boneIndex0, b.weight0); Add(b.boneIndex1, b.weight1); Add(b.boneIndex2, b.weight2); Add(b.boneIndex3, b.weight3);
                }
                // Well clear of the clothes (a skirt's hem between the legs): part held by the hips, so it does not tear between them.
                if (f == Frame.Body && pelvis >= 0 && near[0] >= 0)
                {
                    float clear = Mathf.Clamp01((Mathf.Sqrt(dist[0]) - .03f) / .1f) * .6f;
                    if (clear > 0 && total > 0) { foreach (var key in new List<int>(acc.Keys)) acc[key] *= (1 - clear); acc.TryGetValue(pelvis, out float o); acc[pelvis] = o + clear * total; }
                }
                res[i] = Top4(acc);
            }
            return res;
        }
        static BoneWeight Top4(Dictionary<int, float> acc)
        {
            var list = new List<KeyValuePair<int, float>>(acc); list.Sort((a, b) => b.Value.CompareTo(a.Value));
            float sum = 0; for (int k = 0; k < Mathf.Min(4, list.Count); k++) sum += list[k].Value;
            if (sum <= 0) return new BoneWeight { boneIndex0 = 0, weight0 = 1 };
            var w = new BoneWeight();
            if (list.Count > 0) { w.boneIndex0 = list[0].Key; w.weight0 = list[0].Value / sum; }
            if (list.Count > 1) { w.boneIndex1 = list[1].Key; w.weight1 = list[1].Value / sum; }
            if (list.Count > 2) { w.boneIndex2 = list[2].Key; w.weight2 = list[2].Value / sum; }
            if (list.Count > 3) { w.boneIndex3 = list[3].Key; w.weight3 = list[3].Value / sum; }
            return w;
        }
    }
}
