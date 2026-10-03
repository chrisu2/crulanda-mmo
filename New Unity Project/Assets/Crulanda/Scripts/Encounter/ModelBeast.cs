using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Crulanda.Encounter
{
    /// <summary>
    /// An animal as a model (2026-10-03: Chris chose real animal models, after the people of playtest note 12). Quaternius's
    /// Ultimate Animated Animal Pack (CC0, July 2021; Resources/Creatures, brought in by tools/wip/animals/animals_import.py): one
    /// low-poly skinned mesh per animal on its own armature, with its clips (idles, a head-low idle, eating, walk, gallop, attack,
    /// two hit reactions, death, a jump) played through a PlayableGraph as a person's are (<see cref="ModelFigure"/>). Its colours
    /// are the file's flat ones on matte Standard materials, or a <see cref="Coat"/>'s (an ash hound's charcoal hide and burning
    /// eyes). It is turned to face +Z and scaled so the top of its head (its ears; never the antlers) stands at the height asked
    /// for, its feet on the ground. Worn by beasts (ActorVisual.Beasts.cs: wolves and ash hounds, stags and does) and by game deer
    /// (CritterBody).
    /// </summary>
    public sealed class ModelBeast
    {
        /// <summary>Off: beasts and game animals keep their primitive bodies (read when one is built).</summary>
        public static bool Enabled = true;
        /// <summary>A hide: colours by the file's material names (Main, Main_Light, Nose, Eyes_Black...), and the names that shine
        /// with <see cref="Glow"/>. A name not given keeps the file's colour.</summary>
        public sealed class Coat
        {
            public readonly Dictionary<string, Color> Colours = new Dictionary<string, Color>();
            public readonly List<string> Glowing = new List<string>();
            public Color Glow;
            public Coat Set(string material, Color c) { Colours[material] = c; return this; }
            public Coat Shine(string material, Color c, Color glow) { Colours[material] = c; Glowing.Add(material); Glow = glow; return this; }
            public string Key
            {
                get
                {
                    var keys = new List<string>(Colours.Keys); keys.Sort(System.StringComparer.Ordinal);
                    var s = new System.Text.StringBuilder();
                    foreach (var k in keys) s.Append(k).Append('=').Append(Hex(Colours[k])).Append(Glowing.Contains(k) ? "*" : "").Append(';');
                    return s.ToString();
                }
            }
        }
        public string Kind { get; private set; }
        public Transform Model;
        public Animator Animator;
        public readonly List<Renderer> Renderers = new List<Renderer>();
        /// <summary>The armature's hips (Back: the pelvis over the hind legs; Body, its root, sits low under the belly), the head
        /// and the first neck bone.</summary>
        public Transform Hips, Head, Neck;
        /// <summary>Its size in the game over its size in the file.</summary>
        public float Scale { get; private set; }
        /// <summary>Ground to the top of its head (its ears; not the antlers), standing in its rest pose, in metres.</summary>
        public float Height { get; private set; }
        /// <summary>Nose to rump (or tail) in its rest pose, in metres.</summary>
        public float Length { get; private set; }
        Dictionary<string, Transform> bones;
        /// <summary>One of its bones by the file's name (null if it has none such).</summary>
        public Transform Bone(string name) { return bones != null && bones.TryGetValue(name, out var t) ? t : null; }

        static readonly Dictionary<string, GameObject> sources = new Dictionary<string, GameObject>();
        static GameObject Source(string kind)
        {
            if (!sources.TryGetValue(kind, out var go)) { go = Resources.Load<GameObject>("Creatures/" + kind); sources[kind] = go; }
            return go;
        }
        /// <summary>True when the animal is in the build (and models are on).</summary>
        public static bool Available(string kind) { return Enabled && !string.IsNullOrEmpty(kind) && Source(kind) != null; }

        // ------------------------------------------------------------------------------------------------- materials
        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        /// <summary>The file's colours are Blender's, linear, and come in times its diffuse factor (.8): shown as they are in this
        /// gamma-space project they come out dark and muddy, so they are taken back to the colours the pack was painted in.</summary>
        static Color FileColour(Material m)
        {
            var c = m != null && m.HasProperty("_Color") ? m.color : Color.grey;
            float G(float x) { return Mathf.LinearToGammaSpace(Mathf.Clamp01(x / .8f)); }
            return new Color(G(c.r), G(c.g), G(c.b), 1);
        }
        static bool Shiny(string name) { var n = name.ToLowerInvariant(); return n.Contains("eye") || n.Contains("nose") || n == "material.011"; }
        /// <summary>A matte Standard material in this colour (eyes and noses a little glossy), or glowing; one per colour, shared.</summary>
        static Material Mat(string name, Color c, bool glowing, Color glow)
        {
            string key = name + "/" + Hex(c) + (glowing ? "/" + Hex(glow) : "");
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Shader.Find("Standard")) { color = c, name = "Creature " + key };
            m.SetFloat("_Glossiness", Shiny(name) ? .55f : .1f); m.SetFloat("_Metallic", 0);
            if (glowing) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            mats[key] = m; return m;
        }
        /// <summary>The hide an animal wears when none is given: the stag a russet coat over the file's pale tan, the doe a warm
        /// brown over its salmon (the pack's colours read washed out among the painted ones); others the file's own.</summary>
        static Coat Native(string kind)
        {
            switch (kind)
            {
                case "Stag": return new Coat().Set("Material", new Color(.5f, .33f, .2f)).Set("Material.003", new Color(.82f, .76f, .64f)).Set("Material.010", new Color(.3f, .21f, .13f));
                case "Deer": return new Coat().Set("Main", new Color(.53f, .37f, .25f)).Set("Main_Light", new Color(.82f, .75f, .63f)).Set("Main_Dark", new Color(.36f, .25f, .17f));
                default: return null;
            }
        }
        void Colour(Coat coat, float shade)
        {
            if (coat == null) coat = Native(Kind);
            foreach (var r in Renderers)
            {
                var own = r.sharedMaterials;
                for (int i = 0; i < own.Length; i++)
                {
                    string n = own[i] != null ? own[i].name.Replace(" (Instance)", "") : "";
                    Color c = coat != null && coat.Colours.TryGetValue(n, out var set) ? set : FileColour(own[i]);
                    bool glowing = coat != null && coat.Glowing.Contains(n);
                    if (!glowing && !Shiny(n)) c = new Color(c.r * shade, c.g * shade, c.b * shade, 1);   // no two of a herd quite alike
                    own[i] = Mat(n, c, glowing, coat != null ? coat.Glow : Color.black);
                }
                r.sharedMaterials = own;
            }
        }

        // ------------------------------------------------------------------------------------------------- rounding
        /// <summary>Off: the pack's meshes as they are (read when one is built).</summary>
        public static bool Round = true;
        static readonly Dictionary<Mesh, Mesh> rounded = new Dictionary<Mesh, Mesh>();
        /// <summary>
        /// The mesh with every triangle split in four and each new corner set out on the curved surface its edge's corners and their
        /// normals describe (a PN triangle's edge midpoint), so the pack's low-poly animals round out in outline and in light; the old
        /// corners stay where they were (nothing shrinks). Corners the file splits by face and colour count as one for the curve, so
        /// no seam opens; bone weights blend along each edge; each colour keeps to its own triangles, its normals smooth through its
        /// patch and hard where two colours meet. Made once per mesh.
        /// </summary>
        public static Mesh Rounded(Mesh src)
        {
            if (src == null || !src.isReadable) return src;
            if (rounded.TryGetValue(src, out var done) && done != null) return done;
            var pos = src.vertices; var uv = src.uv; var bw = src.boneWeights; int n = pos.Length;
            bool hasUv = uv != null && uv.Length == n, hasW = bw != null && bw.Length == n;
            int Weld(Dictionary<Vector3Int, int> map, Vector3 p) { var k = Vector3Int.RoundToInt(p * 20000); if (!map.TryGetValue(k, out int w)) { w = map.Count; map[k] = w; } return w; }
            var key = new Dictionary<Vector3Int, int>(); var weld = new int[n];
            for (int i = 0; i < n; i++) weld[i] = Weld(key, pos[i]);
            var smooth = new Vector3[key.Count];
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var t = src.GetTriangles(s);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    var fn = Vector3.Cross(pos[t[i + 1]] - pos[t[i]], pos[t[i + 2]] - pos[t[i]]);
                    smooth[weld[t[i]]] += fn; smooth[weld[t[i + 1]]] += fn; smooth[weld[t[i + 2]]] += fn;
                }
            }
            for (int i = 0; i < smooth.Length; i++) smooth[i] = Unit(smooth[i]);
            var P = new List<Vector3>(pos); var U = hasUv ? new List<Vector2>(uv) : null; var W = hasW ? new List<BoneWeight>(bw) : null;
            var mid = new Dictionary<long, int>();
            int Mid(int a, int c)
            {
                long k = a < c ? ((long)a << 32) | (uint)c : ((long)c << 32) | (uint)a;
                if (mid.TryGetValue(k, out int m)) return m;
                Vector3 p1 = pos[a], p2 = pos[c], n1 = smooth[weld[a]], n2 = smooth[weld[c]];
                Vector3 b210 = (2 * p1 + p2 - Vector3.Dot(p2 - p1, n1) * n1) / 3, b120 = (2 * p2 + p1 - Vector3.Dot(p1 - p2, n2) * n2) / 3;
                P.Add((p1 + 3 * b210 + 3 * b120 + p2) / 8);
                if (hasUv) U.Add((uv[a] + uv[c]) / 2);
                if (hasW) W.Add(Blend(bw[a], bw[c]));
                m = P.Count - 1; mid[k] = m; return m;
            }
            var subs = new List<int[]>();
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var t = src.GetTriangles(s); var o = new int[t.Length * 4]; int j = 0;
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    int a = t[i], b = t[i + 1], c = t[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    o[j++] = a; o[j++] = ab; o[j++] = ca; o[j++] = ab; o[j++] = b; o[j++] = bc;
                    o[j++] = ca; o[j++] = bc; o[j++] = c; o[j++] = ab; o[j++] = bc; o[j++] = ca;
                }
                subs.Add(o);
            }
            var r = new Mesh { name = src.name + " (rounded)" };
            if (P.Count > 65000) r.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            r.SetVertices(P); if (hasUv) r.SetUVs(0, U);
            if (hasW) { r.boneWeights = W.ToArray(); r.bindposes = src.bindposes; }
            r.subMeshCount = subs.Count; for (int s = 0; s < subs.Count; s++) r.SetTriangles(subs[s], s);
            // Normals: each corner the sum of the faces round its place in its own colour.
            var sum = new Dictionary<(int, int), Vector3>(); var place = new Dictionary<Vector3Int, int>(); var at = new int[P.Count]; var colour = new int[P.Count];
            for (int i = 0; i < P.Count; i++) { at[i] = Weld(place, P[i]); colour[i] = -1; }
            for (int s = 0; s < subs.Count; s++)
            {
                var t = subs[s];
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    var fn = Vector3.Cross(P[t[i + 1]] - P[t[i]], P[t[i + 2]] - P[t[i]]);
                    for (int k = 0; k < 3; k++) { int v = t[i + k]; if (colour[v] < 0) colour[v] = s; var id = (at[v], s); sum[id] = (sum.TryGetValue(id, out var acc) ? acc : Vector3.zero) + fn; }
                }
            }
            var normals = new Vector3[P.Count];
            for (int i = 0; i < P.Count; i++) normals[i] = Unit(colour[i] >= 0 && sum.TryGetValue((at[i], colour[i]), out var acc) ? acc : Vector3.up);
            r.SetNormals(normals); r.RecalculateBounds();
            rounded[src] = r; return r;
        }
        /// <summary>A direction of length one. (Vector3.normalized gives zero under 1e-5, and the pack's meshes are a hundredth of
        /// their size in the file: every face's cross product is under it, and the first rounding drew black.)</summary>
        static Vector3 Unit(Vector3 v) { float m = v.magnitude; return m > 1e-30f ? v / m : Vector3.up; }
        /// <summary>Halfway along an edge: both corners' bone weights, half each, the strongest four kept.</summary>
        static BoneWeight Blend(BoneWeight a, BoneWeight b)
        {
            var w = new Dictionary<int, float>();
            void Add(int i, float x) { if (x <= 0) return; w[i] = (w.TryGetValue(i, out var y) ? y : 0) + x * .5f; }
            Add(a.boneIndex0, a.weight0); Add(a.boneIndex1, a.weight1); Add(a.boneIndex2, a.weight2); Add(a.boneIndex3, a.weight3);
            Add(b.boneIndex0, b.weight0); Add(b.boneIndex1, b.weight1); Add(b.boneIndex2, b.weight2); Add(b.boneIndex3, b.weight3);
            var top = new List<KeyValuePair<int, float>>(w); top.Sort((x, y) => y.Value.CompareTo(x.Value));
            float total = 0; for (int i = 0; i < top.Count && i < 4; i++) total += top[i].Value;
            var o = new BoneWeight(); if (total <= 0) return a;
            if (top.Count > 0) { o.boneIndex0 = top[0].Key; o.weight0 = top[0].Value / total; }
            if (top.Count > 1) { o.boneIndex1 = top[1].Key; o.weight1 = top[1].Value / total; }
            if (top.Count > 2) { o.boneIndex2 = top[2].Key; o.weight2 = top[2].Value / total; }
            if (top.Count > 3) { o.boneIndex3 = top[3].Key; o.weight3 = top[3].Value / total; }
            return o;
        }

        // ------------------------------------------------------------------------------------------------- building
        /// <summary>Its rest pose in the file, measured once: the feet's and the head's heights, its length and which way it faces.</summary>
        struct Shape { public float feet, top, back, front; public bool turned; }
        static readonly Dictionary<string, Shape> shapes = new Dictionary<string, Shape>();
        /// <summary>The stag's antlers (their own mesh in the file): never counted in its height.</summary>
        public static bool Antlers(Renderer r) { return r != null && r.name.IndexOf("Horn", System.StringComparison.OrdinalIgnoreCase) >= 0; }
        Shape Measure()
        {
            if (shapes.TryGetValue(Kind, out var s)) return s;
            // Its skin in the rest pose, vertex by vertex, in the model's space (a renderer's bounds carry a margin: measured by
            // them, a wolf stood 6 cm off the ground); the antlers are left out of the height.
            Bounds b = default; bool any = false; Bounds all = default; bool anyAll = false;
            var inv = Model.worldToLocalMatrix; var baked = new Mesh();
            foreach (var r in Renderers)
            {
                Vector3[] verts; Matrix4x4 w = inv * r.transform.localToWorldMatrix;   // BakeMesh(true) keeps the renderer's scale out
                if (r is SkinnedMeshRenderer sk) { sk.BakeMesh(baked, true); verts = baked.vertices; }
                else { var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue; verts = mf.sharedMesh.vertices; }
                foreach (var v in verts)
                {
                    var p = w.MultiplyPoint3x4(v);
                    if (!anyAll) { all = new Bounds(p, Vector3.zero); anyAll = true; } else all.Encapsulate(p);
                    if (Antlers(r)) continue;
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            if (Application.isPlaying) Object.Destroy(baked); else Object.DestroyImmediate(baked);
            if (!any) b = all;
            s.feet = b.min.y; s.top = b.max.y; s.back = all.min.z; s.front = all.max.z;
            // Facing: the head is ahead of the hips.
            if (Head != null && Hips != null) s.turned = inv.MultiplyPoint3x4(Head.position).z < inv.MultiplyPoint3x4(Hips.position).z;
            if (s.turned) { float f = -s.back; s.back = -s.front; s.front = f; }
            shapes[Kind] = s; return s;
        }

        /// <summary>
        /// Builds the animal under <paramref name="parent"/>: its feet at <paramref name="ground"/> (in the parent's space), facing
        /// +Z, the top of its head <paramref name="height"/> m above its feet; coloured by <paramref name="coat"/> (null: the file's
        /// colours) a shade lighter or darker (<paramref name="shade"/>, 1 as it is). Null if the animal is not in the build.
        /// </summary>
        public static ModelBeast Build(Transform parent, string kind, float ground, float height, Coat coat = null, float shade = 1)
        {
            var src = Available(kind) ? Source(kind) : null; if (src == null) return null;
            var b = new ModelBeast { Kind = kind };
            var go = Object.Instantiate(src, parent, false); go.name = "Model"; b.Model = go.transform;
            b.Model.localPosition = Vector3.zero; b.Model.localRotation = Quaternion.identity; b.Model.localScale = Vector3.one;
            b.Animator = go.GetComponent<Animator>(); if (b.Animator == null) b.Animator = go.AddComponent<Animator>();
            b.Animator.applyRootMotion = false; b.Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; b.Animator.runtimeAnimatorController = null;
            b.bones = new Dictionary<string, Transform>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (!b.bones.ContainsKey(t.name)) b.bones[t.name] = t;
            b.Hips = b.Bone("Back") ?? b.Bone("Body"); b.Head = b.Bone("Head"); b.Neck = b.Bone("Neck1");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
                if (r is SkinnedMeshRenderer sk) { sk.updateWhenOffscreen = false; sk.skinnedMotionVectors = false; if (Round) sk.sharedMesh = Rounded(sk.sharedMesh); }
                else if (Round) { var mf = r.GetComponent<MeshFilter>(); if (mf != null) mf.sharedMesh = Rounded(mf.sharedMesh); }
                b.Renderers.Add(r);
            }
            var shape = b.Measure();
            float fileHeight = Mathf.Max(.01f, shape.top - shape.feet);
            b.Scale = height / fileHeight; b.Height = height; b.Length = (shape.front - shape.back) * b.Scale;
            b.Model.localRotation = Quaternion.Euler(0, shape.turned ? 180 : 0, 0);
            b.Model.localScale = Vector3.one * b.Scale;
            b.Model.localPosition = new Vector3(0, ground - shape.feet * b.Scale, 0);
            b.Colour(coat, shade);
            return b;
        }

        // ------------------------------------------------------------------------------------------------- motion
        /// <summary>Its motions, a mixer slot each, and the clips that can fill them (the first the file has): standing, grazing
        /// and the head-low idle loop; walk and gallop loop with the ground; the rest are one-shots.</summary>
        static readonly (string slot, string[] clips, bool loop)[] Motions = {
            ("idle", new[] { "Idle" }, true), ("idle2", new[] { "Idle_2" }, true), ("headlow", new[] { "Idle_2_HeadLow", "Idle_Headlow" }, true),
            ("eat", new[] { "Eating" }, true), ("walk", new[] { "Walk" }, true), ("run", new[] { "Gallop" }, true),
            ("attack", new[] { "Attack", "Attack_Headbutt" }, false), ("kick", new[] { "Attack_Kick", "Attack" }, false),
            ("hitL", new[] { "Idle_HitReact_Left" }, false), ("hitR", new[] { "Idle_HitReact_Right", "Idle_HitReact_Left" }, false),
            ("death", new[] { "Death" }, false), ("jump", new[] { "Gallop_Jump", "Jump_toIdle", "Jump_ToIdle" }, false) };
        static readonly Dictionary<string, AnimationClip[]> clipsOf = new Dictionary<string, AnimationClip[]>();
        /// <summary>The kind's clip of this name (as imported: CreatureImport names them plainly), or null.</summary>
        public static AnimationClip Clip(string kind, string name)
        {
            if (!clipsOf.TryGetValue(kind, out var all)) { all = Resources.LoadAll<AnimationClip>("Creatures/" + kind); clipsOf[kind] = all; }
            foreach (var c in all)
            {
                if (c == null || c.name.StartsWith("__preview__")) continue;   // the editor's raw takes
                var n = c.name; int bar = n.LastIndexOf('|'); if ((bar >= 0 ? n.Substring(bar + 1) : n) == name) return c;
            }
            return null;
        }
        /// <summary>The clip a slot plays for this kind (null if the file has none of its clips).</summary>
        public static AnimationClip SlotClip(string kind, string slot)
        {
            foreach (var (s, names, _) in Motions) if (s == slot) foreach (var n in names) { var c = Clip(kind, n); if (c != null) return c; }
            return null;
        }

        PlayableGraph graph; AnimationMixerPlayable mixer;
        readonly List<(AnimationClipPlayable p, AnimationClip c, bool loop)> slots = new List<(AnimationClipPlayable, AnimationClip, bool)>();
        readonly Dictionary<string, int> slotOf = new Dictionary<string, int>(); float[] weights, want;
        string shot, rest = "idle"; float shotTime, shotWeight, pace; bool dead;
        /// <summary>The pace (m/s) its walk and its gallop are made for at this size: their rates follow the ground speed from there.</summary>
        public float WalkPace = 1.3f, RunPace = 6;
        /// <summary>Ticks the model's weights every frame, whoever moves it (a dead game animal's own behaviour stops, its death goes on).</summary>
        sealed class Driver : MonoBehaviour
        {
            public ModelBeast beast;
            void LateUpdate() { if (beast != null) beast.Tick(Time.deltaTime); }
            void OnDestroy() { if (beast != null) beast.StopMotion(); }
        }
        /// <summary>Starts its graph (play mode only; in edit mode it keeps its rest pose until <see cref="Sample"/>d).</summary>
        public void StartMotion(string label)
        {
            if (!Application.isPlaying || graph.IsValid() || Animator == null) return;
            Model.gameObject.AddComponent<Driver>().beast = this;
            graph = PlayableGraph.Create("Beast " + label); graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Beast", Animator);
            var found = new List<(string, AnimationClip, bool)>();
            foreach (var (slot, _, loop) in Motions) { var c = SlotClip(Kind, slot); if (c != null) found.Add((slot, c, loop)); }
            mixer = AnimationMixerPlayable.Create(graph, found.Count); weights = new float[found.Count]; want = new float[found.Count];
            for (int i = 0; i < found.Count; i++)
            {
                var p = AnimationClipPlayable.Create(graph, found[i].Item2); p.SetApplyFootIK(false);
                graph.Connect(p, 0, mixer, i); slots.Add((p, found[i].Item2, found[i].Item3)); slotOf[found[i].Item1] = i;
                if (found[i].Item3) p.SetTime(Stagger(label, i) * found[i].Item2.length);   // no two of a pack step in time
            }
            if (slotOf.TryGetValue("idle", out int idle)) { weights[idle] = 1; mixer.SetInputWeight(idle, 1); }
            output.SetSourcePlayable(mixer); graph.Play();
        }
        static float Stagger(string label, int slot)
        {
            uint h = 2166136261u; foreach (char ch in label ?? "") { h ^= ch; h *= 16777619u; }
            h ^= (uint)slot * 2654435761u; h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            return (h & 0xffff) / 65536f;
        }
        public void StopMotion() { if (graph.IsValid()) graph.Destroy(); }
        public bool Moving { get { return graph.IsValid(); } }
        public bool Has(string slot) { return slotOf.ContainsKey(slot); }
        /// <summary>Dead: its death played and its last frame held (<see cref="Die"/>) until <see cref="Revive"/>.</summary>
        public bool Dead { get { return dead; } }
        /// <summary>A one-shot (an attack, a flinch) is playing.</summary>
        public bool Busy { get { return shot != null; } }
        /// <summary>Plays a one-shot over whatever it is doing (attack, kick, hitL, hitR, jump), easing in and out.</summary>
        public void Play(string slot)
        {
            if (dead || !slotOf.TryGetValue(slot, out int i)) return;
            shot = slot; shotTime = 0; slots[i].p.SetTime(0); slots[i].p.SetSpeed(1);
        }
        /// <summary>It falls: its death, played once, its last frame held.</summary>
        public void Die()
        {
            if (dead) return;
            dead = true;
            if (slotOf.TryGetValue("death", out int i)) { shot = "death"; shotTime = 0; slots[i].p.SetTime(0); slots[i].p.SetSpeed(1); }
        }
        /// <summary>On its feet again (respawned).</summary>
        public void Revive() { dead = false; shot = null; shotWeight = 0; }
        /// <summary>What it is doing now: going at <paramref name="speed"/> m/s, and when it stands, <paramref name="standing"/>
        /// (idle, idle2, headlow, eat). Kept until the next call; the model eases toward it every frame.</summary>
        public void Drive(float speed, string standing) { pace = speed; rest = standing != null && slotOf.ContainsKey(standing) ? standing : "idle"; }
        /// <summary>
        /// One frame: the pace blends standing, the walk and the gallop, each cycle's rate matched to the ground; a one-shot eases in
        /// over it and out at its end; dead, the death holds. Weights ease over a quarter second, so nothing pops.
        /// </summary>
        void Tick(float dt)
        {
            if (!graph.IsValid()) return;
            float speed = pace;
            System.Array.Clear(want, 0, want.Length);
            float walk = Mathf.InverseLerp(.12f, WalkPace * .6f, speed), run = Mathf.InverseLerp(WalkPace * 1.5f, RunPace * .6f, speed);
            Set(want, rest, 1 - walk); Set(want, "walk", walk * (1 - run)); Set(want, "run", walk * run);
            Rate("walk", Mathf.Clamp(speed / WalkPace, .6f, 1.7f)); Rate("run", Mathf.Clamp(speed / RunPace, .7f, 1.5f));
            // The one-shot: in over a tenth of a second, out over the last fifth (a death never goes out).
            if (shot != null && slotOf.TryGetValue(shot, out int si))
            {
                shotTime += dt; float len = Mathf.Max(.05f, slots[si].c.length);
                float target = dead ? 1 : Mathf.Clamp01(Mathf.Min(shotTime / .1f, (len - shotTime) / .2f));
                shotWeight = dead ? Mathf.MoveTowards(shotWeight, 1, dt * 8) : target;
                if (!dead && shotTime >= len) { shot = null; shotWeight = 0; }
                else { for (int i = 0; i < want.Length; i++) want[i] *= 1 - shotWeight; want[si] = shotWeight; }
            }
            float sum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                bool oneShot = shot != null && slotOf.TryGetValue(shot, out int s2) && s2 == i;
                weights[i] = oneShot ? want[i] : Mathf.MoveTowards(weights[i], want[i], dt * 4);   // a one-shot sets its own weight
                sum += weights[i];
            }
            for (int i = 0; i < weights.Length; i++)
            {
                mixer.SetInputWeight(i, sum > 0 ? weights[i] / sum : 0);
                var (p, c, loop) = slots[i];
                double t = p.GetTime();
                if (c.length > 0 && t > c.length) p.SetTime(loop ? t % c.length : c.length);
            }
        }
        void Set(float[] want, string slot, float w) { if (slotOf.TryGetValue(slot, out int i)) want[i] = Mathf.Max(want[i], w); }
        void Rate(string slot, float r) { if (slotOf.TryGetValue(slot, out int i)) slots[i].p.SetSpeed(r); }
        /// <summary>Edit mode (captures, tests): poses it at <paramref name="time"/> seconds into a slot's clip (a one-shot holds
        /// its last frame). A PlayableGraph does not pose an Animator outside play mode.</summary>
        public void Sample(string slot, float time)
        {
            var c = SlotClip(Kind, slot); if (c == null || Model == null || c.length <= 0) return;
            bool loop = false; foreach (var (s, _, l) in Motions) if (s == slot) loop = l;
            c.SampleAnimation(Model.gameObject, loop ? time % c.length : Mathf.Min(time, c.length));
        }
        /// <summary>The length of a slot's clip for this kind, in seconds (0 if it has none).</summary>
        public float ClipLength(string slot) { var c = SlotClip(Kind, slot); return c != null ? c.length : 0; }
    }
}
