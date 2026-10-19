using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Every dead tree in the game is one of DigitalAntichrist's Dry Trees (Asset Store EULA; Resources/Props/DryTrees, play note 248,
    /// Chris 2026-10-19: "replace all dead trees with this pack in myassets - dry trees"): seven leafless trees, each named for its
    /// triangles (Dry3333 to Dry8872), with three lower-detail copies in the same file. In the world only the full one is kept (the
    /// static merge would draw all four at once; the full one costs what the nature kit's dead tree did, some 6,000 triangles); the
    /// backdrop past the edge takes the lowest copy (500 to 1,100). The pack's HDRP material is Built-in Standard here (Bark.tif and
    /// Normal.tif, the pack's GUIDs kept); the HDRP demo scene, the prefabs and the mask map stay out.
    /// <see cref="DeadTree"/> keeps the heights, collider and draws the old dead trees had, so nothing else in a zone moves: which of
    /// the seven and its turn come from the tree's own stream (TreeRandom of its spot), so a zone grows the same trees every time.
    /// GAME-ONLY dressing.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The seven dry trees under Resources/Props (ModelProp paths).</summary>
        public static readonly string[] DryTreeModels = {
            "DryTrees/Model/Dry3333", "DryTrees/Model/Dry4195", "DryTrees/Model/Dry4910", "DryTrees/Model/Dry5818",
            "DryTrees/Model/Dry6524", "DryTrees/Model/Dry7509", "DryTrees/Model/Dry8872" };
        /// <summary>The three stoutest (Dry8872, Dry6524, Dry5818: the thickest trunks and broadest crowns, measured from the files), for
        /// the massive dead oaks; and the Gallows Tree's own, the stoutest.</summary>
        public static readonly int[] StoutDryTrees = { 6, 4, 3 };
        public const int GallowsDryTree = 6;
        /// <summary>Each tree's trunk radius low down, as a share of its height (the files' vertices 6-12 per cent up).</summary>
        public static readonly float[] DryTrunk = { .033f, .031f, .031f, .033f, .028f, .018f, .046f };
        /// <summary>Which of the seven a draw in [0, 1] picks (1 itself the last).</summary>
        public static int DryTreePick(float u) { int n = DryTreeModels.Length; return Mathf.Clamp((int)(u * n), 0, n - 1); }
        /// <summary>
        /// A dead tree's height, as the old ones stood: a common one (the woods' snags, the groves, the Rim's husks, the gallows) 5.2 m a
        /// unit of scale times 1.35, a massive one (a dead_oak prop) 4.2 m times 2.1, either nine tenths to eleven tenths of that by
        /// <paramref name="u"/>.
        /// </summary>
        public static float DryTreeHeight(float scale, bool massive, float u)
        {
            float trunkH = (massive ? 4.2f : 5.2f) * scale;
            return trunkH * (massive ? 2.1f : 1.35f) * (.9f + Mathf.Clamp01(u) * .2f);
        }
        /// <summary>Whether the pack is in the project.</summary>
        public static bool DryTreesThere { get { return HasProp(DryTreeModels[0]); } }
        static Material dryBark;
        /// <summary>The pack's bark, Built-in Standard (Resources/Props/DryTrees/Materials/Bark).</summary>
        public static Material DryBark { get { if (dryBark == null) dryBark = Resources.Load<Material>("Props/DryTrees/Materials/Bark"); return dryBark; } }
        /// <summary>The bark's shade in this zone, as the kit's dead trees had it: charred darker in the ash, greyed in a gloom.</summary>
        Color DryShade() { return Zone.biome == "ash" ? new Color(.55f, .52f, .5f) : Gloom ? new Color(.8f, .78f, .76f) : Color.white; }

        static readonly Dictionary<string, Vector3> dryBases = new Dictionary<string, Vector3>();
        /// <summary>
        /// One dry tree under <paramref name="parent"/>, <paramref name="height"/> tall (before the parent's scale), turned
        /// <paramref name="turn"/>, the middle of its trunk's foot on <paramref name="foot"/> (parent-local): the trees are not centred on
        /// their files' origins, and centring the whole crown (ModelProp) set a trunk up to a metre off its collider. ModelProp drops
        /// the lower-detail copies. Null when the pack is missing.
        /// </summary>
        public static GameObject DryTreeModel(Transform parent, int pick, Vector3 foot, Quaternion turn, float height, Color shade, float broad = 1)
        {
            string path = DryTreeModels[Mathf.Clamp(pick, 0, DryTreeModels.Length - 1)];
            var go = ModelProp(parent, path, Vector3.zero, 0, height); if (go == null) return null;
            var t = go.transform; var trunkFoot = DryBase(path, t);
            if (broad != 1) { float s = t.localScale.y; t.localScale = new Vector3(s * broad, s, s * broad); }   // a landmark's girth and spread
            t.localRotation = turn; t.localPosition = foot - turn * Vector3.Scale(t.localScale, trunkFoot);
            var bark = DryBark;
            if (bark != null)
            {
                var m = shade == Color.white ? bark : KitShade(bark, shade);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = m; r.sharedMaterials = mats; }
            }
            return go;
        }
        /// <summary>The middle of a model's trunk at its foot, in the holder's own space: the lowest vertex's height, and the middle (x, z) of
        /// the vertices in the lowest 4 per cent of the tree. Once a model.</summary>
        static Vector3 DryBase(string path, Transform t)
        {
            if (dryBases.TryGetValue(path, out var known)) return known;
            var toT = t.worldToLocalMatrix; var pts = new List<Vector3>(); float lo = float.MaxValue, hi = float.MinValue;
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null || !mesh.isReadable) continue; var m = toT * mf.transform.localToWorldMatrix;
                foreach (var v in mesh.vertices) { var p = m.MultiplyPoint3x4(v); pts.Add(p); lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y); }
            }
            Vector3 foot;
            if (pts.Count == 0) { var b = PropBounds(t); foot = new Vector3(b.center.x, b.min.y, b.center.z); }
            else
            {
                float band = lo + (hi - lo) * .04f, x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                foreach (var p in pts) if (p.y <= band) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z); }
                foot = new Vector3((x0 + x1) / 2, lo, (z0 + z1) / 2);
            }
            dryBases[path] = foot; return foot;
        }
        /// <summary>
        /// How far (in <paramref name="root"/>'s own units) a trunk's foot sinks so no side of it hangs over a slope: to just under the
        /// lowest ground (the height function or the drawn ground, whichever is lower) round it at its radius and twice that, as SeatPine
        /// sets a mountain pine. On the flat a few centimetres.
        /// </summary>
        float DrySink(Transform root, float radius)
        {
            var c = root.position; float low = Mathf.Min(HeightAt(c.x, c.z), MeshY(c.x, c.z)), r = Mathf.Max(.25f, radius);
            for (int k = 0; k < 12; k++)
                foreach (float f in new[] { r, r * 2 })
                {
                    float x = c.x + Mathf.Cos(k * Mathf.PI / 6) * f, z = c.z + Mathf.Sin(k * Mathf.PI / 6) * f;
                    low = Mathf.Min(low, Mathf.Min(HeightAt(x, z), MeshY(x, z)));
                }
            return (c.y - low + .06f) / Mathf.Max(.01f, root.lossyScale.y);
        }

        /// <summary>
        /// The Gallows Tree's limb for the crossbeam, in <paramref name="space"/> (the gallows' root; the trunk on its origin): of the
        /// tree's vertices 38 to 62 per cent of <paramref name="height"/> up and 18 to 40 per cent of it out from the trunk, those in the
        /// sixteenth of the round that holds the most (the heaviest limb carries the most of the mesh), averaged: the limb's middle there.
        /// </summary>
        public static bool GallowsLimb(Transform model, Transform space, float height, out Vector3 limb)
        {
            limb = Vector3.zero; var bins = new List<Vector3>[16]; for (int i = 0; i < 16; i++) bins[i] = new List<Vector3>();
            var toSpace = space.worldToLocalMatrix;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null || !mesh.isReadable) continue; var m = toSpace * mf.transform.localToWorldMatrix;
                foreach (var v in mesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v); float r = new Vector2(p.x, p.z).magnitude;
                    if (p.y < height * .38f || p.y > height * .62f || r < height * .18f || r > height * .4f) continue;
                    int b = Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(p.z, p.x) / (2 * Mathf.PI), 1) * 16) % 16; bins[b].Add(p);
                }
            }
            int best = 0; for (int i = 1; i < 16; i++) if (bins[i].Count > bins[best].Count) best = i;
            if (bins[best].Count < 6) return false;
            foreach (var p in bins[best]) limb += p; limb /= bins[best].Count; return true;
        }

        static readonly Dictionary<int, (Mesh mesh, Matrix4x4 unit)> dryFar = new Dictionary<int, (Mesh, Matrix4x4)>();
        /// <summary>
        /// The lowest-detail copy of dry tree <paramref name="pick"/> for the backdrop's merged meshes, with the matrix that stands it up as
        /// ModelProp would (the file's own turn), 1 m tall, its trunk's foot on the origin: scale it by the height wanted. False when the
        /// pack is missing or the mesh cannot be read.
        /// </summary>
        public static bool DryTreeFar(int pick, out Mesh mesh, out Matrix4x4 unit)
        {
            pick = Mathf.Clamp(pick, 0, DryTreeModels.Length - 1);
            if (dryFar.TryGetValue(pick, out var known) && known.mesh != null) { mesh = known.mesh; unit = known.unit; return true; }
            mesh = null; unit = Matrix4x4.identity;
            var src = PropSource(DryTreeModels[pick]); if (src == null) return false;
            MeshFilter least = null;
            foreach (var mf in src.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null && mf.sharedMesh.isReadable && (least == null || mf.sharedMesh.vertexCount < least.sharedMesh.vertexCount)) least = mf;
            if (least == null) return false;
            var m = Matrix4x4.identity;
            for (var t = least.transform; t != null && t != src.transform; t = t.parent) m = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * m;
            m = Matrix4x4.TRS(Vector3.zero, src.transform.localRotation, src.transform.localScale) * m;
            var verts = least.sharedMesh.vertices; float lo = float.MaxValue, hi = float.MinValue;
            var pts = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++) { pts[i] = m.MultiplyPoint3x4(verts[i]); lo = Mathf.Min(lo, pts[i].y); hi = Mathf.Max(hi, pts[i].y); }
            if (hi - lo < .001f) return false;
            float band = lo + (hi - lo) * .04f, x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var p in pts) if (p.y <= band) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z); }
            unit = Matrix4x4.Scale(Vector3.one / (hi - lo)) * Matrix4x4.Translate(-new Vector3((x0 + x1) / 2, lo, (z0 + z1) / 2)) * m;
            mesh = least.sharedMesh; dryFar[pick] = (mesh, unit); return true;
        }
    }
}
