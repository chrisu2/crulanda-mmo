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
        /// the massive dead oaks.</summary>
        public static readonly int[] StoutDryTrees = { 6, 4, 3 };
        /// <summary>The Gallows Tree's own tree (Chris, 2026-10-19 on the art2 frame: "tree trunk too low"): Dry4910, whose trunk runs a
        /// quarter of its height whole before the first fork (Dry8872, the stoutest, forks at a twenty-fifth: under half a metre) and which
        /// carries one heavy limb at under half its height; at GallowsTall that clear trunk is some 2.25 m.</summary>
        public const int GallowsDryTree = 2;
        public const float GallowsTall = 9, GallowsBroad = 1.4f;
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
            else foot = TrunkFoot(pts);
            dryBases[path] = foot; return foot;
        }
        /// <summary>
        /// The trunk's own foot among a tree's vertices: the middle (x, z) of the lowest 4 per cent, and the lowest vertex of the trunk
        /// there (within the nearer half of that band's vertices to the middle), not a root tip that dips below it to one side (three of
        /// the trees' roots reach 0.5 to 3 per cent of their height under the trunk: standing on those lifted the trunk, and the old
        /// bounds-foot read the whole tree as sunk or floating by them).
        /// </summary>
        public static Vector3 TrunkFoot(IList<Vector3> pts)
        {
            float lo = float.MaxValue, hi = float.MinValue; foreach (var p in pts) { lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y); }
            float band = lo + (hi - lo) * .04f, x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var p in pts) if (p.y <= band) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z); }
            float cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
            var near = new List<(float d, float y)>(); foreach (var p in pts) if (p.y <= band) near.Add((new Vector2(p.x - cx, p.z - cz).magnitude, p.y));
            near.Sort((a, b) => a.d.CompareTo(b.d)); float y = float.MaxValue;
            for (int i = 0; i < Mathf.Max(1, near.Count / 2); i++) y = Mathf.Min(y, near[i].y);
            return new Vector3(cx, y, cz);
        }
        /// <summary>How far a foot sinks: to the lowest ground at the trunk's edge, and 5 cm more (only a few centimetres on the flat).</summary>
        public static float FootSink(float centre, float lowestAtTrunk) { return Mathf.Max(0, centre - lowestAtTrunk) + .05f; }
        /// <summary>
        /// Where a tree first forks, in <paramref name="space"/>'s height (its foot at <paramref name="foot"/>): slices of 2.5 per cent of
        /// its height from the foot up; in each, the mesh's triangles that cross it joined where they share a corner (corners at one place
        /// are one, across the UV seams), and the pieces counted; the first slice with more pieces than the trunk's slice near the foot (a
        /// tree may carry a loose root piece from the ground up). Joined by the mesh, not by distance, so a broadened trunk still reads
        /// whole. (Read from the files: Dry4910 forks at a quarter of its height, Dry8872 at a twenty-fifth.)
        /// </summary>
        public static float DryForkHeight(Transform model, Transform space, float foot)
        {
            var pos = new List<Vector3>(); var tris = new List<int>(); var weld = new Dictionary<Vector3Int, int>(); var id = new List<int>();
            var toSpace = space.worldToLocalMatrix; float hi = float.MinValue;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null || !mesh.isReadable) continue; var m = toSpace * mf.transform.localToWorldMatrix;
                int start = pos.Count;
                foreach (var v in mesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v); pos.Add(p); hi = Mathf.Max(hi, p.y);
                    var key = new Vector3Int(Mathf.RoundToInt(p.x * 500), Mathf.RoundToInt(p.y * 500), Mathf.RoundToInt(p.z * 500));
                    if (!weld.TryGetValue(key, out int w)) weld[key] = w = weld.Count; id.Add(w);
                }
                foreach (var t in mesh.triangles) tris.Add(start + t);
            }
            float h = hi - foot; if (pos.Count == 0 || h <= 0) return 0;
            int Pieces(float a, float b)
            {
                var parent = new Dictionary<int, int>();
                int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
                for (int t = 0; t + 2 < tris.Count; t += 3)
                {
                    float y0 = pos[tris[t]].y, y1 = pos[tris[t + 1]].y, y2 = pos[tris[t + 2]].y;
                    if (Mathf.Min(y0, Mathf.Min(y1, y2)) > b || Mathf.Max(y0, Mathf.Max(y1, y2)) < a) continue;
                    int r = -1;
                    for (int c = 0; c < 3; c++)
                    {
                        int w = id[tris[t + c]]; if (!parent.ContainsKey(w)) parent[w] = w;
                        if (r < 0) r = Find(w); else parent[Find(w)] = r;
                    }
                }
                var roots = new HashSet<int>(); foreach (var k in new List<int>(parent.Keys)) roots.Add(Find(k)); return roots.Count;
            }
            int trunk = Pieces(foot + h * .025f, foot + h * .05f);
            for (int k = 3; k < 30; k++) if (Pieces(foot + h * k * .0125f, foot + h * (k + 2) * .0125f) > trunk) return h * (k + 1) * .0125f;
            return h * .4f;
        }
        /// <summary>
        /// How far (in <paramref name="root"/>'s own units) a trunk's foot sinks so no side of it hangs over a slope: to just under the
        /// lowest ground (the height function or the drawn ground, whichever is lower) round it at its radius and twice that, as SeatPine
        /// sets a mountain pine. On the flat a few centimetres.
        /// </summary>
        float DrySink(Transform root, float radius)
        {
            // At the trunk's own edge only (it reached twice as far, which on a slope sank a broad trunk half a metre: "tree trunk too low").
            var c = root.position; float low = Mathf.Min(HeightAt(c.x, c.z), MeshY(c.x, c.z)), r = Mathf.Max(.2f, radius);
            for (int k = 0; k < 12; k++)
            {
                float x = c.x + Mathf.Cos(k * Mathf.PI / 6) * r, z = c.z + Mathf.Sin(k * Mathf.PI / 6) * r;
                low = Mathf.Min(low, Mathf.Min(HeightAt(x, z), MeshY(x, z)));
            }
            return FootSink(c.y, low) / Mathf.Max(.01f, root.lossyScale.y);
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

        /// <summary>The gallows' rope (4 cm through, so it reads from the camera), the noose's loop (radius), the platform's deck height and
        /// its boards' width.</summary>
        public const float GallowsRope = .04f, GallowsLoop = .17f, GallowsDeck = .575f, GallowsBoards = 2.4f;
        /// <summary>The gallows laid out from its limb (the gallows' space, the tree turned so the limb lies along +x).</summary>
        public struct GallowsLayout { public Vector3 beamFrom, beamTo, ropeTop, knot, noose, platform; }
        /// <summary>
        /// The crossbeam from the trunk along the limb at its height to 0.7 m past it; the rope from just under the beam's free end (0.1 m
        /// short of it) to its knot 2.1 m over the deck, so the noose's foot hangs some 1.75 m over the boards (head height); the platform
        /// centred under the rope (its near edge well clear of the trunk). Pure: the test reads it.
        /// </summary>
        public static GallowsLayout GallowsPlan(Vector3 limb)
        {
            float reach = Mathf.Max(1.6f, new Vector2(limb.x, limb.z).magnitude), y = Mathf.Max(3.2f, limb.y);
            var g = new GallowsLayout { beamFrom = new Vector3(.2f, y, 0), beamTo = new Vector3(reach + .7f, y, 0) };
            float x = reach + .6f; g.ropeTop = new Vector3(x, y - .08f, 0); g.knot = new Vector3(x, Mathf.Min(y - .5f, GallowsDeck + 2.1f), 0);
            g.noose = g.knot + Vector3.down * 2 * GallowsLoop; g.platform = new Vector3(x, 0, 0);
            return g;
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
            var foot = TrunkFoot(pts);
            unit = Matrix4x4.Scale(Vector3.one / (hi - foot.y)) * Matrix4x4.Translate(-foot) * m;
            mesh = least.sharedMesh; dryFar[pick] = (mesh, unit); return true;
        }
    }
}
