using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The smooth figure (playtest note 12, Chris: "time to retire the block characters"; he chose to build them in code). One
    /// skeleton and one set of skinned meshes lofted along it, shared by every person: a shaped torso (hips, waist, chest,
    /// shoulders), a neck, tapered limbs with elbows and knees, hands with thumbs and boots; the head, hair and face ride the
    /// Head bone. tools/wip/characters/body.py is the reference and draws its previews (mesh_view.py).
    ///
    /// Body space is ActorVisual's: origin 1 m above the soles, +Z forward, +X the figure's right. The shoulder pivots "Arm L/R"
    /// (at -/+.31, .53, 0) and the hip pivots "Leg L/R" (-/+.12, -.08, 0) stay where they were and swing as before; under them
    /// the elbows ("Forearm"), wrists ("Hand"), knees ("Shin") and ankles ("Foot") are new bones, and the spine is "Spine",
    /// "Chest", "Neck", "Head". Each region the worn gear covers is its own renderer (<see cref="Region"/>), with the material
    /// ActorVisual gives it, so armour still recolours a region by swapping its material.
    /// </summary>
    public static class SmoothBody
    {
        /// <summary>The bare figure's regions, one skinned renderer each: what worn gear covers or recolours.</summary>
        public enum Region { Chest, Belt, Legs, Boots, Sleeves, Hands, Neck }
        public const int RegionCount = 7;
        /// <summary>The bones, parents before children. Body is the actor's own "Body".</summary>
        public static readonly string[] BoneNames = { "Body", "Spine", "Chest", "Neck", "Head", "Arm L", "Forearm L", "Hand L", "Arm R", "Forearm R", "Hand R",
            "Leg L", "Shin L", "Foot L", "Leg R", "Shin R", "Foot R" };
        public static readonly int[] Parents = { -1, 0, 1, 2, 3, 0, 5, 6, 0, 8, 9, 0, 11, 12, 0, 14, 15 };
        /// <summary>Each bone's rest position in body space (rest rotations are all identity).</summary>
        public static readonly Vector3[] Rest = {
            Vector3.zero, new Vector3(0, .08f, 0), new Vector3(0, .34f, 0), new Vector3(0, .62f, -.01f), new Vector3(0, .72f, 0),
            new Vector3(-.31f, .53f, 0), new Vector3(-.315f, .245f, -.01f), new Vector3(-.315f, -.04f, .01f),
            new Vector3(.31f, .53f, 0), new Vector3(.315f, .245f, -.01f), new Vector3(.315f, -.04f, .01f),
            new Vector3(-.12f, -.08f, 0), new Vector3(-.12f, -.51f, .015f), new Vector3(-.12f, -.92f, -.005f),
            new Vector3(.12f, -.08f, 0), new Vector3(.12f, -.51f, .015f), new Vector3(.12f, -.92f, -.005f) };
        public const int Body = 0, Spine = 1, Chest = 2, Neck = 3, Head = 4, ArmL = 5, ForearmL = 6, HandL = 7, ArmR = 8, ForearmR = 9, HandR = 10,
            LegL = 11, ShinL = 12, FootL = 13, LegR = 14, ShinR = 15, FootR = 16;

        static Mesh[] regions; static Mesh head, hairCap, hairCropped, hairFall;
        /// <summary>The skinned meshes, one per region (body space, bound to <see cref="BoneNames"/>), built once and shared.</summary>
        public static Mesh[] Regions { get { if (regions == null) Make(); return regions; } }
        /// <summary>The head (in the Head bone's space).</summary>
        public static Mesh HeadMesh { get { if (regions == null) Make(); return head; } }
        /// <summary>A cap of hair over the skull (Head bone space): the full cap, or cropped short.</summary>
        public static Mesh HairMesh(bool cropped) { if (regions == null) Make(); return cropped ? hairCropped : hairCap; }
        /// <summary>Long hair falling down the back from under the cap to the shoulders (Head bone space).</summary>
        public static Mesh HairFallMesh { get { if (regions == null) Make(); return hairFall; } }

        // ------------------------------------------------------------------------------------------------- building
        sealed class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<int> t = new List<int>(); public readonly List<BoneWeight> w = new List<BoneWeight>();
            /// <summary>Rings of equal size joined into a band, capped at either end on request. weights(i) per ring.</summary>
            public void Loft(List<Vector3[]> rings, System.Func<int, BoneWeight> weights, bool capStart, bool capEnd)
            {
                int n = rings[0].Length, b = v.Count;
                for (int i = 0; i < rings.Count; i++) { var wt = weights(i); foreach (var p in rings[i]) { v.Add(p); w.Add(wt); } }
                for (int i = 0; i + 1 < rings.Count; i++)
                    for (int j = 0; j < n; j++)
                    {
                        int a = b + i * n + j, c = b + i * n + (j + 1) % n, d = b + (i + 1) * n + j, e = b + (i + 1) * n + (j + 1) % n;
                        t.Add(a); t.Add(d); t.Add(c); t.Add(c); t.Add(d); t.Add(e);
                    }
                if (capStart) Cap(rings[0], b, weights(0), false);
                if (capEnd) Cap(rings[rings.Count - 1], b + (rings.Count - 1) * n, weights(rings.Count - 1), true);
            }
            void Cap(Vector3[] ring, int start, BoneWeight wt, bool end)
            {
                var c = Vector3.zero; foreach (var p in ring) c += p; c /= ring.Length; int k = v.Count; v.Add(c); w.Add(wt);
                for (int j = 0; j < ring.Length; j++) { int a = start + j, b = start + (j + 1) % ring.Length; if (end) { t.Add(k); t.Add(b); t.Add(a); } else { t.Add(k); t.Add(a); t.Add(b); } }
            }
            public Mesh Done(string name, bool skinned)
            {
                var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(t, 0);
                Orient(m);
                if (skinned)
                {
                    m.boneWeights = w.ToArray();
                    var bind = new Matrix4x4[BoneNames.Length];
                    for (int i = 0; i < bind.Length; i++) bind[i] = Matrix4x4.Translate(-Rest[i]);   // rest rotations are identity
                    m.bindposes = bind;
                }
                m.RecalculateNormals(); m.RecalculateBounds(); return m;
            }
        }
        /// <summary>Winds every triangle to face away from the part's centre line (lofts are wound one way, caps the other).</summary>
        static void Orient(Mesh m)
        {
            var v = m.vertices; var t = m.triangles; var c = Vector3.zero; foreach (var p in v) c += p; c /= Mathf.Max(1, v.Length);
            float sum = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], d = v[t[i + 2]];
                sum += Vector3.Dot(Vector3.Cross(b - a, d - a), (a + b + d) / 3 - c);
            }
            if (sum < 0) { for (int i = 0; i < t.Length; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; } m.triangles = t; }
        }
        static BoneWeight W(int a, float wa = 1, int b = 0, float wb = 0)
        {
            float s = wa + wb; if (s <= 0) { s = 1; wa = 1; }
            return new BoneWeight { boneIndex0 = a, weight0 = wa / s, boneIndex1 = b, weight1 = wb / s };
        }
        /// <summary>Bone a blending smoothly into bone b as x runs from x0 to x1.</summary>
        static BoneWeight Blend(float x, int a, int b, float x0, float x1)
        {
            float u = Mathf.Clamp01((x - x0) / (x1 - x0)); u = u * u * (3 - 2 * u); return W(a, 1 - u, b, u);
        }
        static Vector3[] SuperRing(int n, float y, float a, float b, float zc, float p)
        {
            var r = new Vector3[n];
            for (int j = 0; j < n; j++)
            {
                float t = 2 * Mathf.PI * j / n, c = Mathf.Cos(t), s = Mathf.Sin(t);
                r[j] = new Vector3(a * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2 / p), y, zc + b * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2 / p));
            }
            return r;
        }
        /// <summary>Rings round a polyline (body space) with a radius at each point; squash flattens them across the second axis.</summary>
        static List<Vector3[]> TubeRings(Vector3[] pts, float[] radii, int n, float squash = 1)
        {
            var list = new List<Vector3[]>();
            for (int k = 0; k < pts.Length; k++)
            {
                var d = (pts[Mathf.Min(k + 1, pts.Length - 1)] - pts[Mathf.Max(k - 1, 0)]).normalized;
                var refv = Mathf.Abs(d.z) < .9f ? Vector3.forward : Vector3.right;
                var u = Vector3.Cross(d, refv).normalized; var w = Vector3.Cross(d, u);
                var ring = new Vector3[n];
                for (int j = 0; j < n; j++) { float t = 2 * Mathf.PI * j / n; ring[j] = pts[k] + u * (Mathf.Cos(t) * radii[k]) + w * (Mathf.Sin(t) * radii[k] * squash); }
                list.Add(ring);
            }
            return list;
        }

        // the torso's profile: y, half-width, half-depth, centre z, squareness (body.py v2)
        static readonly float[,] Torso = {
            { -.14f, .17f, .112f, -.004f, 2.4f }, { -.1f, .2f, .128f, -.005f, 2.5f }, { -.02f, .21f, .134f, 0, 2.6f }, { .033f, .2f, .13f, .004f, 2.5f },
            { .033f, .207f, .136f, .004f, 2.5f }, { .098f, .2f, .132f, .006f, 2.5f }, { .098f, .193f, .126f, .006f, 2.5f }, { .16f, .182f, .122f, .004f, 2.4f },
            { .25f, .195f, .134f, .012f, 2.5f }, { .34f, .222f, .152f, .024f, 2.6f }, { .43f, .242f, .158f, .024f, 2.7f }, { .5f, .252f, .146f, .01f, 2.9f },
            { .553f, .238f, .124f, -.006f, 3.1f }, { .595f, .172f, .1f, -.012f, 2.6f }, { .625f, .1f, .078f, -.012f, 2.2f }, { .645f, .066f, .066f, -.008f, 2 } };

        static void Make()
        {
            var parts = new Builder[RegionCount]; for (int i = 0; i < RegionCount; i++) parts[i] = new Builder();
            // Torso: legs (below the belt), the belt band, the chest (above); the rings at .033 and .098 are doubled for hard seams.
            const int tn = 24;
            System.Func<float, BoneWeight> torsoW = y => y < .08f ? Blend(y, Body, Spine, -.05f, .1f) : y < .4f ? Blend(y, Spine, Chest, .15f, .36f) : Blend(y, Chest, Neck, .6f, .66f);
            var lower = new List<Vector3[]>(); var belt = new List<Vector3[]>(); var upper = new List<Vector3[]>();
            for (int i = 0; i < Torso.GetLength(0); i++)
            {
                var ring = SuperRing(tn, Torso[i, 0], Torso[i, 1], Torso[i, 2], Torso[i, 3], Torso[i, 4]); float y = Torso[i, 0];
                if (i <= 3) lower.Add(ring); if (i >= 4 && i <= 5) belt.Add(ring); if (i >= 6) upper.Add(ring);
            }
            parts[(int)Region.Legs].Loft(lower, i => torsoW(lower[i][0].y), true, false);
            parts[(int)Region.Belt].Loft(belt, i => torsoW(belt[i][0].y), false, false);
            parts[(int)Region.Chest].Loft(upper, i => torsoW(upper[i][0].y), false, true);
            // Neck, skinned to the neck and head.
            var neck = TubeRings(new[] { new Vector3(0, .6f, -.012f), new Vector3(0, .66f, -.008f), new Vector3(0, .72f, 0) }, new[] { .06f, .056f, .056f }, 14);
            parts[(int)Region.Neck].Loft(neck, i => i == 0 ? W(Neck) : i == 1 ? W(Neck, .6f, Head, .4f) : W(Head), false, false);
            foreach (int s in new[] { -1, 1 })
            {
                int arm = s < 0 ? ArmL : ArmR, fore = s < 0 ? ForearmL : ForearmR, hand = s < 0 ? HandL : HandR;
                int leg = s < 0 ? LegL : LegR, shin = s < 0 ? ShinL : ShinR, foot = s < 0 ? FootL : FootR;
                float x = .31f * s;
                // The arm: a rounded deltoid, the upper arm, the elbow, a sturdy forearm; the last 12% (the wrist) is skin.
                var ap = new[] { new Vector3(x - .012f * s, .61f, -.006f), new Vector3(x - .01f * s, .595f, -.005f), new Vector3(x - .006f * s, .565f, -.004f), new Vector3(x + .002f * s, .5f, 0),
                    new Vector3(x + .005f * s, .42f, 0), new Vector3(x + .006f * s, .33f, -.005f), new Vector3(x + .005f * s, .245f, -.01f), new Vector3(x + .004f * s, .17f, -.005f),
                    new Vector3(x + .004f * s, .09f, .002f), new Vector3(x + .003f * s, .02f, .008f), new Vector3(x + .003f * s, -.0f, .009f), new Vector3(x + .003f * s, -.035f, .01f) };
                var ar = new[] { .03f, .062f, .082f, .08f, .066f, .06f, .055f, .062f, .058f, .048f, .043f, .04f };
                var rings = TubeRings(ap, ar, 14, .9f);
                System.Func<int, BoneWeight> armW = i => { float y = ap[i].y; return y > .29f ? W(arm) : y > .2f ? Blend(-y, arm, fore, -.29f, -.2f) : y > 0 ? W(fore) : Blend(-y, fore, hand, 0, .04f); };
                parts[(int)Region.Sleeves].Loft(rings.GetRange(0, 10), armW, false, false);
                var wrist = rings.GetRange(9, 3); parts[(int)Region.Hands].Loft(wrist, i => armW(9 + i), false, true);
                // The hand: a mitten hung from the wrist, fingers together and a little curled forward; a thumb.
                var hp = new[] { new Vector3(x, -.03f, .01f), new Vector3(x, -.065f, .013f), new Vector3(x, -.105f, .017f), new Vector3(x, -.145f, .024f), new Vector3(x, -.175f, .034f), new Vector3(x, -.192f, .042f) };
                parts[(int)Region.Hands].Loft(TubeRings(hp, new[] { .04f, .05f, .054f, .05f, .038f, .018f }, 12, .55f), i => W(hand), true, true);
                var tp = new[] { new Vector3(x - .01f * s, -.055f, .032f), new Vector3(x - .012f * s, -.09f, .056f), new Vector3(x - .012f * s, -.118f, .066f) };
                parts[(int)Region.Hands].Loft(TubeRings(tp, new[] { .019f, .018f, .013f }, 8), i => W(hand), true, true);
                // The leg: thigh over the hips, knee, calf, ankle; trousers to 64%, then the boot's shaft.
                x = .12f * s;
                var lp = new[] { new Vector3(x - .01f * s, .03f, -.004f), new Vector3(x - .006f * s, -.04f, 0), new Vector3(x, -.12f, .004f), new Vector3(x + .002f * s, -.25f, .012f),
                    new Vector3(x + .002f * s, -.38f, .016f), new Vector3(x, -.51f, .016f), new Vector3(x, -.6f, .004f), new Vector3(x, -.64f, 0), new Vector3(x, -.7f, -.006f),
                    new Vector3(x, -.8f, -.004f), new Vector3(x, -.9f, -.004f) };
                var lr = new[] { .098f, .118f, .114f, .1f, .086f, .068f, .074f, .073f, .07f, .056f, .05f };
                var legRings = TubeRings(lp, lr, 16);
                System.Func<int, BoneWeight> legW = i =>
                {
                    float y = lp[i].y;
                    if (y > -.08f) return y > 0 ? W(Body, .55f, leg, .45f) : W(Body, .4f, leg, .6f);
                    if (y > -.44f) return W(leg); if (y > -.56f) return Blend(-y, leg, shin, .44f, .56f);
                    if (y > -.86f) return W(shin); return Blend(-y, shin, foot, .86f, .92f);
                };
                parts[(int)Region.Legs].Loft(legRings.GetRange(0, 8), legW, false, false);
                var shaft = legRings.GetRange(7, 4); parts[(int)Region.Boots].Loft(shaft, i => legW(7 + i), false, true);
                // The boot's foot: heel to toe along +z, flat on the sole.
                float[,] fp = { { -.08f, .05f, .054f, -.952f }, { -.05f, .057f, .07f, -.942f }, { 0, .06f, .076f, -.938f }, { .06f, .061f, .058f, -.954f },
                    { .12f, .059f, .045f, -.966f }, { .17f, .053f, .036f, -.972f }, { .205f, .038f, .028f, -.974f } };
                var footRings = new List<Vector3[]>();
                for (int i = 0; i < fp.GetLength(0); i++)
                {
                    var r = new Vector3[14];
                    for (int j = 0; j < 14; j++)
                    {
                        float a = 2 * Mathf.PI * j / 14, c = Mathf.Cos(a), sn = Mathf.Sin(a);
                        float xx = fp[i, 1] * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2 / 2.6f), yy = fp[i, 2] * Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 2 / 2.6f);
                        r[j] = new Vector3(x + xx, Mathf.Max(yy + fp[i, 3], -1), fp[i, 0]);
                    }
                    footRings.Add(r);
                }
                parts[(int)Region.Boots].Loft(footRings, i => W(foot), true, true);
            }
            regions = new Mesh[RegionCount];
            for (int i = 0; i < RegionCount; i++) regions[i] = parts[i].Done("Smooth " + ((Region)i).ToString().ToLowerInvariant(), true);
            head = MakeHead(); hairCap = MakeHair(false); hairCropped = MakeHair(true); hairFall = MakeHairFall();
        }
        /// <summary>The head (Head bone space): a sphere a little narrower than deep, the jaw narrowing to a forward chin, the back of the skull full.</summary>
        static Mesh MakeHead()
        {
            var b = new Builder(); const int nu = 22, nv = 16; float cy = .812f - Rest[Head].y, cz = .012f, r = .156f; var rings = new List<Vector3[]>();
            for (int i = 1; i < nv; i++)
            {
                float v = Mathf.PI * i / nv, y = Mathf.Cos(v); var ring = new Vector3[nu];
                for (int j = 0; j < nu; j++)
                {
                    float u = 2 * Mathf.PI * j / nu, x = Mathf.Sin(v) * Mathf.Cos(u), z = Mathf.Sin(v) * Mathf.Sin(u), sx = .9f, sz = 1;
                    if (y < 0) { sx *= 1 - .22f * Mathf.Pow(-y, 1.5f); if (z > 0) z *= 1 + .08f * -y; }
                    if (z < 0) sz *= 1.06f;
                    ring[j] = new Vector3(x * r * sx, cy + y * r * 1.08f, cz + z * r * sz);
                }
                rings.Add(ring);
            }
            b.Loft(rings, i => W(Head), true, true); return b.Done("Smooth head", false);
        }
        /// <summary>The long fall: a thick, gently curved sheet of hair from the back of the crown to the shoulders (Head bone space).</summary>
        static Mesh MakeHairFall()
        {
            var b = new Builder(); float o = -Rest[Head].y;
            var pts = new[] { new Vector3(0, .9f + o, -.1f), new Vector3(0, .82f + o, -.165f), new Vector3(0, .72f + o, -.17f), new Vector3(0, .63f + o, -.16f), new Vector3(0, .56f + o, -.15f) };
            var rings = new List<Vector3[]>();
            foreach (var (c, half, thick) in new[] { (pts[0], .1f, .03f), (pts[1], .15f, .045f), (pts[2], .155f, .05f), (pts[3], .145f, .045f), (pts[4], .12f, .02f) })
            {
                var ring = new Vector3[16];
                for (int j = 0; j < 16; j++) { float a = 2 * Mathf.PI * j / 16; ring[j] = c + new Vector3(Mathf.Cos(a) * half, 0, Mathf.Sin(a) * thick); }
                rings.Add(ring);
            }
            b.Loft(rings, i => W(Head), true, true); return b.Done("Smooth hair fall", false);
        }
        /// <summary>Hair over the skull (Head bone space), the head's shape a little larger, kept where hair grows; cropped is shorter.</summary>
        static Mesh MakeHair(bool cropped)
        {
            var b = new Builder(); const int nu = 26, nv = 18; float cy = .812f - Rest[Head].y + .012f, cz = .006f, r = cropped ? .163f : .17f;
            var grid = new (Vector3 p, float y, float z)[nv, nu];
            for (int i = 0; i < nv; i++)
                for (int j = 0; j < nu; j++)
                {
                    float v = Mathf.PI * i / nv * .78f, u = 2 * Mathf.PI * j / nu, x = Mathf.Sin(v) * Mathf.Cos(u), y = Mathf.Cos(v), z = Mathf.Sin(v) * Mathf.Sin(u);
                    grid[i, j] = (new Vector3(x * r * .93f, cy + y * r * 1.08f, cz + z * r * 1.05f), y, z);
                }
            bool Keep(float y, float z) { return cropped ? y > .32f || (z < -.2f && y > -.05f) : y > .18f || (z < -.1f && y > -.45f) || (y > -.15f && z < .25f); }
            for (int i = 0; i + 1 < nv; i++)
                for (int j = 0; j < nu; j++)
                {
                    var q = new[] { grid[i, j], grid[i, (j + 1) % nu], grid[i + 1, j], grid[i + 1, (j + 1) % nu] };
                    bool ok = true; foreach (var e in q) if (!Keep(e.y, e.z)) ok = false; if (!ok) continue;
                    int k = b.v.Count; foreach (var e in q) { b.v.Add(e.p); b.w.Add(W(Head)); }
                    b.t.Add(k); b.t.Add(k + 2); b.t.Add(k + 1); b.t.Add(k + 1); b.t.Add(k + 2); b.t.Add(k + 3);
                }
            return b.Done(cropped ? "Smooth hair (cropped)" : "Smooth hair", false);
        }

        /// <summary>
        /// Builds the smooth figure under <paramref name="body"/>: the bones (reusing "Arm L/R" and "Leg L/R" if the caller made
        /// them), a skinned renderer per region with <paramref name="mats"/> (indexed by <see cref="Region"/>), and the head on the
        /// Head bone. Returns the bones in <see cref="BoneNames"/> order (index 0 is body).
        /// </summary>
        public static Transform[] Build(Transform body, Material[] mats, Material skin, out Renderer[] renderers, out Transform headPart)
        {
            var bones = new Transform[BoneNames.Length]; bones[0] = body;
            for (int i = 1; i < bones.Length; i++)
            {
                var parent = bones[Parents[i]]; var existing = parent.Find(BoneNames[i]);
                var t = existing != null ? existing : new GameObject(BoneNames[i]).transform;
                if (existing == null) t.SetParent(parent, false);
                t.localPosition = Rest[i] - Rest[Parents[i]]; t.localRotation = Quaternion.identity; bones[i] = t;
            }
            renderers = new Renderer[RegionCount];
            var bounds = new Bounds(new Vector3(0, -.02f, .02f), new Vector3(1.1f, 2.1f, .9f));
            for (int i = 0; i < RegionCount; i++)
            {
                var o = new GameObject("Smooth " + ((Region)i).ToString().ToLowerInvariant()); o.transform.SetParent(body, false);
                var smr = o.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = Regions[i]; smr.bones = bones; smr.rootBone = body;
                smr.localBounds = bounds; smr.sharedMaterial = mats[i]; smr.quality = SkinQuality.Bone2; renderers[i] = smr;
            }
            var h = new GameObject("Smooth head"); h.transform.SetParent(bones[Head], false);
            h.AddComponent<MeshFilter>().sharedMesh = HeadMesh; h.AddComponent<MeshRenderer>().sharedMaterial = skin; headPart = h.transform;
            return bones;
        }
    }
}
