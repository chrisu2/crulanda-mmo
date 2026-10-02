using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The modelled figure (playtest note 12, 2026-10-02: Chris chose real models over the code-built body). Quaternius kits (CC0;
    /// Resources/Characters, brought in by tools/wip/characters/quaternius_import.py): an outfit (Peasant or Ranger, male or
    /// female) on its own skeleton, the base character's head cut at the collar (the body below would show through the clothes,
    /// which are cut for a slighter body), its eyes and brows, a hairstyle and a beard, all skinned to the outfit's one skeleton;
    /// moved by the Universal Animation Library's humanoid clips through a PlayableGraph (<see cref="Drive"/>). Skin tone, hair
    /// colour and the cloth's dye are tints on the kit's textures. The model faces -Z in its file, so it is turned to face +Z.
    /// ActorVisual hangs its frames on the bones (ActorVisual.Model.cs).
    /// </summary>
    public sealed class ModelFigure
    {
        public enum Kit { Peasant, Ranger }
        /// <summary>
        /// What a figure is: sex, hairstyle (0 short, 1 long, 2 tied back, 3 cropped, 4 bald; each sex has its own cuts), a beard,
        /// the outfit, a hood (the Ranger's, on either outfit), the tints for skin, hair, shirt and breeches (white leaves the
        /// texture as it is), stone (a Hollow Man: everything grey) and a scale.
        /// </summary>
        public struct Spec
        {
            public bool female, beard, hood, stone; public int hair; public Kit kit;
            public Color skin, hairColour, shirt, breeches, hoodColour; public float scale;
        }

        public Transform Model;
        public Animator Animator;
        /// <summary>Every renderer of the figure.</summary>
        public readonly List<Renderer> Renderers = new List<Renderer>();
        /// <summary>The outfit's pieces: torso, arms (sleeves and bare forearms and hands), legs, feet; the hood when worn; the head.</summary>
        public Renderer Torso, Arms, Legs, Feet, Hood, Head;
        /// <summary>The hair (brows, hair and beard) renderers, and the outfit's extras (belts, bracers, a pauldron).</summary>
        public readonly List<Renderer> Hair = new List<Renderer>(), Extras = new List<Renderer>();
        public Transform Hips, Spine, Chest, Neck, HeadBone, UpperArmL, UpperArmR, LowerArmL, LowerArmR, HandL, HandR, UpperLegL, UpperLegR, LowerLegL, LowerLegR, FootL, FootR, MiddleL, MiddleR, BallL, BallR;
        Spec spec;
        Dictionary<string, Transform> bones;

        // ------------------------------------------------------------------------------------------------- materials
        static readonly Dictionary<string, Material> kitMats = new Dictionary<string, Material>();
        static string F(float x) { return x.ToString("0.###", CultureInfo.InvariantCulture); }
        /// <summary>A kit material (Resources/Characters/Materials: the asset keeps its shader variants in a build) times a tint;
        /// one per set and tint, shared.</summary>
        public static Material KitMat(string set, Color tint)
        {
            string key = set + "/" + F(tint.r) + "," + F(tint.g) + "," + F(tint.b);
            if (kitMats.TryGetValue(key, out var m) && m != null) return m;
            var asset = Resources.Load<Material>("Characters/Materials/" + set);
            m = asset != null ? new Material(asset) : new Material(Shader.Find("Standard"));
            m.color = new Color(tint.r, tint.g, tint.b, 1); m.name = "Kit " + key; kitMats[key] = m; return m;
        }
        /// <summary>A dye that shifts the cloth's hue to <paramref name="c"/> but keeps its light and dark: the colour scaled to
        /// full brightness, then eased toward white by (1 - <paramref name="strength"/>).</summary>
        public static Color Dye(Color c, float strength)
        {
            float top = Mathf.Max(c.r, Mathf.Max(c.g, Mathf.Max(c.b, .001f)));
            var full = new Color(c.r / top, c.g / top, c.b / top);
            return Color.Lerp(Color.white, full, strength);
        }
        static bool Bare(string material) { var n = material.ToLowerInvariant(); return n.Contains("regular") || n.Contains("superhero"); }
        /// <summary>The kit set and tint an imported material (by its name in the file) stands for, on a piece of the figure.</summary>
        (string set, Color tint) SetFor(string material, Renderer piece)
        {
            string n = material.ToLowerInvariant(); var s = spec; string sex = s.female ? "Female" : "Male";
            var stone = new Color(.62f, .62f, .64f);
            if (n.Contains("eye")) return ("Eye_Brown", s.stone ? stone : Color.white);
            if (n.Contains("hair") || n.Contains("brow") || n.Contains("beard")) return (n.Contains("2") ? "Hair_2" : "Hair_1", s.stone ? stone * .8f : s.hairColour);
            if (n.Contains("regular")) return ("Regular_" + sex + "_Dark", s.stone ? stone : s.skin);
            if (n.Contains("superhero")) return ("Superhero_" + sex + "_Light", s.stone ? stone : s.skin);
            string set = n.Contains("ranger") ? "Ranger" : "Peasant";
            if (s.stone) return (set, stone);
            if (piece == Hood) return (set, s.hoodColour);
            if (piece == Torso || piece == Arms) return (set, s.shirt);
            if (piece == Legs) return (set, s.breeches);
            return (set, Color.white);
        }
        void Retexture(Renderer r)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) { var (set, tint) = SetFor(mats[i] != null ? mats[i].name : "", r); mats[i] = KitMat(set, tint); }
            r.sharedMaterials = mats;
        }
        readonly Dictionary<Renderer, Material[]> undyed = new Dictionary<Renderer, Material[]>();
        /// <summary>
        /// Worn armour over a piece (ActorVisual.Cover): its cloth takes the armour's colour as a dye (the weave still shows), or,
        /// for gloves (<paramref name="hands"/>), the bare hands take the outfit's leather in that colour.
        /// </summary>
        public void Cover(Renderer r, Color c, bool hands)
        {
            if (r == null) return;
            if (!undyed.ContainsKey(r)) undyed[r] = r.sharedMaterials;
            var own = undyed[r]; var mats = r.sharedMaterials;
            var tint = Dye(c, .85f) * Mathf.Clamp(c.maxColorComponent * 1.6f, .35f, 1.1f);
            for (int i = 0; i < mats.Length && i < own.Length; i++)
            {
                string name = own[i] != null ? own[i].name : "";
                if (Bare(name) != hands) continue;   // sleeves dye the cloth, gloves the bare hands
                string set = hands ? (spec.kit == Kit.Ranger ? "Ranger" : "Peasant") : name.StartsWith("Kit ") && name.IndexOf('/') > 4 ? name.Substring(4, name.IndexOf('/') - 4) : "Peasant";
                mats[i] = KitMat(set, tint);
            }
            r.sharedMaterials = mats;
        }
        /// <summary>Gives a piece back its own cloth (or, for <paramref name="hands"/>, its bare hands).</summary>
        public void Uncover(Renderer r, bool hands)
        {
            if (r == null || !undyed.TryGetValue(r, out var own)) return;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length && i < own.Length; i++) if (Bare(own[i] != null ? own[i].name : "") == hands) mats[i] = own[i];
            r.sharedMaterials = mats;
        }

        // ------------------------------------------------------------------------------------------------- building
        static readonly string[] MaleHair = { "Hair_SimpleParted", "Hair_SimpleParted", "Hair_Buzzed", "Hair_Buzzed", null };
        static readonly string[] FemaleHair = { "Hair_Long", "Hair_Long", "Hair_Buns", "Hair_BuzzedFemale", "Hair_Buns" };
        /// <summary>True when the kit is in the build (the figure can be made).</summary>
        public static bool Available { get { if (!checkedKit) { checkedKit = true; available = Resources.Load<GameObject>("Characters/Outfits/Male_Peasant") != null; } return available; } }
        static bool checkedKit, available;

        /// <summary>Builds the figure under <paramref name="body"/> (the actor's "Body", 1 m above the soles); null if the kit is missing.</summary>
        public static ModelFigure Build(Transform body, Spec s)
        {
            string sex = s.female ? "Female" : "Male";
            var outfitSrc = Resources.Load<GameObject>("Characters/Outfits/" + sex + "_" + s.kit);
            var bodySrc = Resources.Load<GameObject>("Characters/Bodies/Superhero_" + sex + "_FullBody");
            if (outfitSrc == null || bodySrc == null) return null;
            var f = new ModelFigure { spec = s };
            var go = Object.Instantiate(outfitSrc, body, false); go.name = "Model"; f.Model = go.transform;
            f.Model.localPosition = new Vector3(0, -1, 0); f.Model.localRotation = Quaternion.Euler(0, 180, 0); f.Model.localScale = Vector3.one * (s.scale > 0 ? s.scale : 1);
            f.Animator = go.GetComponent<Animator>(); if (f.Animator == null) f.Animator = go.AddComponent<Animator>();
            if (f.Animator.avatar == null || !f.Animator.avatar.isHuman) { var a = bodySrc.GetComponent<Animator>(); if (a != null) f.Animator.avatar = a.avatar; }
            f.Animator.applyRootMotion = false; f.Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; f.Animator.runtimeAnimatorController = null;
            f.bones = new Dictionary<string, Transform>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (!f.bones.ContainsKey(t.name)) f.bones[t.name] = t;
            // The outfit's own pieces, by name.
            foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string n = r.name;
                if (n.Contains("_Head_Hood")) { if (s.hood) f.Hood = r; else { Kill(r.gameObject); continue; } }
                else if (n.Contains("_Belt") || n.Contains("_Bracer") || n.Contains("_Pauldron")) f.Extras.Add(r);
                else if (n.Contains("_Body")) f.Torso = r;
                else if (n.Contains("_Arms")) f.Arms = r;
                else if (n.Contains("_Legs")) f.Legs = r;
                else if (n.Contains("_Feet")) f.Feet = r;
                r.updateWhenOffscreen = false; f.Renderers.Add(r);
            }
            // A hood on an outfit without one: the Ranger's, from its own file.
            if (s.hood && f.Hood == null) f.Hood = f.Take(Resources.Load<GameObject>("Characters/Outfits/" + sex + "_Ranger"), "_Head_Hood");
            // The head (cut at the collar), the eyes and the brows, from the base character; hair and beard.
            var bodyCopy = Object.Instantiate(bodySrc);
            foreach (var r in bodyCopy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                bool eyes = r.name.Contains("Eye") && !r.name.Contains("brow"), brows = r.name.Contains("brow"), head = !eyes && !brows;
                if (head) r.sharedMesh = HeadOnly(r);
                if (f.Move(r)) { f.Renderers.Add(r); if (head) f.Head = r; else if (brows) f.Hair.Add(r); }
            }
            Kill(bodyCopy);
            var styles = s.female ? FemaleHair : MaleHair; string style = styles[Mathf.Clamp(s.hair, 0, styles.Length - 1)];
            if (style != null) { var h = f.Take(Resources.Load<GameObject>("Characters/Hair/" + style), null); if (h != null) f.Hair.Add(h); }
            if (s.beard && !s.female) { var b = f.Take(Resources.Load<GameObject>("Characters/Hair/Hair_Beard"), null); if (b != null) f.Hair.Add(b); }
            foreach (var r in f.Renderers) f.Retexture(r);
            f.FindBones();
            return f;
        }
        static void Kill(GameObject g) { if (Application.isPlaying) { g.SetActive(false); Object.Destroy(g); } else Object.DestroyImmediate(g); }
        /// <summary>Moves a skinned mesh onto this figure's skeleton, bone by bone by name. False (and the mesh is left) if a bone is missing.</summary>
        bool Move(SkinnedMeshRenderer smr)
        {
            var src = smr.bones; var mapped = new Transform[src.Length];
            for (int i = 0; i < src.Length; i++) if (src[i] == null || !bones.TryGetValue(src[i].name, out mapped[i])) return false;
            var root = smr.rootBone != null && bones.TryGetValue(smr.rootBone.name, out var rb) ? rb : Model;
            smr.transform.SetParent(Model, false); smr.bones = mapped; smr.rootBone = root; smr.updateWhenOffscreen = false;
            return true;
        }
        /// <summary>Takes the skinned mesh (the first, or the first whose name has <paramref name="part"/>) from a kit file onto this figure.</summary>
        SkinnedMeshRenderer Take(GameObject src, string part)
        {
            if (src == null) return null;
            var copy = Object.Instantiate(src); SkinnedMeshRenderer got = null;
            foreach (var r in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (got == null && (part == null || r.name.Contains(part)) && Move(r)) { got = r; Renderers.Add(r); }
            Kill(copy);
            return got;
        }
        static readonly Dictionary<Mesh, Mesh> heads = new Dictionary<Mesh, Mesh>();
        /// <summary>The base character's skin cut to the head and neck: the triangles whose corners are at least half weighted to
        /// the Head and neck bones. Made once per body.</summary>
        static Mesh HeadOnly(SkinnedMeshRenderer smr)
        {
            var src = smr.sharedMesh; if (src == null) return null;
            if (heads.TryGetValue(src, out var m) && m != null) return m;
            if (!src.isReadable) return src;
            var keep = new bool[smr.bones.Length];
            for (int i = 0; i < keep.Length; i++) { var n = smr.bones[i] != null ? smr.bones[i].name : ""; keep[i] = n == "Head" || n.StartsWith("neck"); }
            var w = src.boneWeights; var tris = src.triangles; var kept = new List<int>(tris.Length / 6);
            bool Up(int v)
            {
                var b = w[v]; float s = 0;
                if (b.boneIndex0 < keep.Length && keep[b.boneIndex0]) s += b.weight0; if (b.boneIndex1 < keep.Length && keep[b.boneIndex1]) s += b.weight1;
                if (b.boneIndex2 < keep.Length && keep[b.boneIndex2]) s += b.weight2; if (b.boneIndex3 < keep.Length && keep[b.boneIndex3]) s += b.weight3;
                return s >= .5f;
            }
            for (int t = 0; t + 2 < tris.Length; t += 3) if (Up(tris[t]) && Up(tris[t + 1]) && Up(tris[t + 2])) { kept.Add(tris[t]); kept.Add(tris[t + 1]); kept.Add(tris[t + 2]); }
            m = Object.Instantiate(src); m.name = src.name + " (head)"; m.SetTriangles(kept, 0); heads[src] = m; return m;
        }
        Transform B(string name, HumanBodyBones hb)
        {
            if (bones.TryGetValue(name, out var t)) return t;
            return Animator != null && Animator.avatar != null && Animator.avatar.isHuman ? Animator.GetBoneTransform(hb) : null;
        }
        void FindBones()
        {
            Hips = B("pelvis", HumanBodyBones.Hips); Spine = B("spine_01", HumanBodyBones.Spine); Chest = B("spine_03", HumanBodyBones.UpperChest); Neck = B("neck_01", HumanBodyBones.Neck); HeadBone = B("Head", HumanBodyBones.Head);
            UpperArmL = B("upperarm_l", HumanBodyBones.LeftUpperArm); UpperArmR = B("upperarm_r", HumanBodyBones.RightUpperArm); LowerArmL = B("lowerarm_l", HumanBodyBones.LeftLowerArm); LowerArmR = B("lowerarm_r", HumanBodyBones.RightLowerArm);
            HandL = B("hand_l", HumanBodyBones.LeftHand); HandR = B("hand_r", HumanBodyBones.RightHand); MiddleL = B("middle_01_l", HumanBodyBones.LeftMiddleProximal); MiddleR = B("middle_01_r", HumanBodyBones.RightMiddleProximal);
            UpperLegL = B("thigh_l", HumanBodyBones.LeftUpperLeg); UpperLegR = B("thigh_r", HumanBodyBones.RightUpperLeg); LowerLegL = B("calf_l", HumanBodyBones.LeftLowerLeg); LowerLegR = B("calf_r", HumanBodyBones.RightLowerLeg);
            FootL = B("foot_l", HumanBodyBones.LeftFoot); FootR = B("foot_r", HumanBodyBones.RightFoot); BallL = B("ball_l", HumanBodyBones.LeftToes); BallR = B("ball_r", HumanBodyBones.RightToes);
        }
        /// <summary>True when every bone the frames hang on was found.</summary>
        public bool Complete { get { return Hips && Spine && Chest && HeadBone && UpperArmL && UpperArmR && LowerArmL && LowerArmR && HandL && HandR && UpperLegL && UpperLegR && LowerLegL && LowerLegR && FootL && FootR; } }

        // ------------------------------------------------------------------------------------------------- motion
        PlayableGraph graph; AnimationMixerPlayable mixer;
        readonly List<(AnimationClipPlayable p, AnimationClip c, bool loop)> slots = new List<(AnimationClipPlayable, AnimationClip, bool)>();
        readonly Dictionary<string, int> slotOf = new Dictionary<string, int>(); float[] weights; string current;
        static AnimationClip[] library;
        static AnimationClip[] Library { get { if (library == null) library = Resources.LoadAll<AnimationClip>("Characters/Animations/UAL1_Standard"); return library; } }
        /// <summary>The library's clip of this name (as in the file, after "Armature|"), or null.</summary>
        public static AnimationClip Clip(string name)
        {
            foreach (var c in Library) { if (c == null) continue; var n = c.name; int bar = n.LastIndexOf('|'); if ((bar >= 0 ? n.Substring(bar + 1) : n) == name) return c; }
            return null;
        }
        /// <summary>The figure's motions, a mixer slot each: the walk cycle's (idle, walk, jog, sprint) and the poses'.</summary>
        static readonly (string slot, string clip)[] Motions = {
            ("idle", "Idle_Loop"), ("walk", "Walk_Loop"), ("run", "Jog_Fwd_Loop"), ("sprint", "Sprint_Loop"),
            ("sit", "Sitting_Idle_Loop"), ("sittalk", "Sitting_Talking_Loop"), ("swim", "Swim_Fwd_Loop"), ("swimidle", "Swim_Idle_Loop"),
            ("sneak", "Crouch_Fwd_Loop"), ("crouch", "Crouch_Idle_Loop"), ("talk", "Idle_Talking_Loop"), ("gather", "Fixing_Kneeling"),
            ("death", "Death01") };
        /// <summary>Starts the figure's graph (play mode only; in edit mode the figure keeps its bind pose).</summary>
        public void StartMotion(string label)
        {
            if (!Application.isPlaying || graph.IsValid() || Animator == null) return;
            graph = PlayableGraph.Create("Figure " + label); graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Figure", Animator);
            var found = new List<(string, AnimationClip)>();
            foreach (var (slot, clip) in Motions) { var c = Clip(clip); if (c != null) found.Add((slot, c)); }
            mixer = AnimationMixerPlayable.Create(graph, found.Count); weights = new float[found.Count];
            for (int i = 0; i < found.Count; i++)
            {
                var p = AnimationClipPlayable.Create(graph, found[i].Item2); p.SetApplyFootIK(true);
                graph.Connect(p, 0, mixer, i); slots.Add((p, found[i].Item2, found[i].Item1 != "death" && found[i].Item1 != "gather")); slotOf[found[i].Item1] = i;
                p.SetTime(Random.value * found[i].Item2.length);   // no two figures step in time
            }
            if (slotOf.TryGetValue("idle", out int idle)) { weights[idle] = 1; mixer.SetInputWeight(idle, 1); }
            output.SetSourcePlayable(mixer); graph.Play();
        }
        public void StopMotion() { if (graph.IsValid()) graph.Destroy(); }
        /// <summary>The clip a slot plays (null if the library lacks it).</summary>
        public static AnimationClip SlotClip(string slot) { foreach (var (s, clip) in Motions) if (s == slot) return Clip(clip); return null; }
        /// <summary>Edit mode (captures, tests): poses the figure at <paramref name="time"/> seconds into a slot's clip, once
        /// (AnimationClip.SampleAnimation: a PlayableGraph does not pose an Animator outside play mode).</summary>
        public void Sample(string slot, float time)
        {
            var c = SlotClip(slot); if (c == null || Model == null || c.length <= 0) return;
            bool once = slot == "death" || slot == "gather";
            c.SampleAnimation(Model.gameObject, once ? Mathf.Min(time, c.length) : time % c.length);
        }
        /// <summary>Puts the Ranger's hood on (once), in <paramref name="tint"/> (an old figure's round hood becomes this).</summary>
        public void AddHood(Color tint)
        {
            if (Hood != null) return;
            spec.hood = true; spec.hoodColour = tint;
            Hood = Take(Resources.Load<GameObject>("Characters/Outfits/" + (spec.female ? "Female" : "Male") + "_Ranger"), "_Head_Hood");
            if (Hood != null) Retexture(Hood);
        }
        /// <summary>Shows or hides the outfit's extras whose name has <paramref name="part"/> ("Belt", "Bracer"): worn armour covers them.</summary>
        public void ShowExtras(string part, bool on) { foreach (var r in Extras) if (r != null && r.name.Contains(part)) r.enabled = on; }
        public bool Moving { get { return graph.IsValid(); } }
        public bool Has(string slot) { return slotOf.ContainsKey(slot); }
        /// <summary>
        /// One frame: <paramref name="speed"/> (m/s) blends idle, walk, jog and sprint, each cycle's rate matched to the ground; a
        /// pose's slot (sit, swim, sneak, talk...) takes over when given. Weights ease over a quarter second, so nothing pops.
        /// A one-shot clip (a death, kneeling to work) starts from its beginning when its slot comes in and holds its last frame.
        /// </summary>
        public void Drive(float speed, string pose, float dt)
        {
            if (!graph.IsValid()) return;
            var want = new float[weights.Length];
            if (pose != null && slotOf.TryGetValue(pose, out int ps))
            {
                want[ps] = 1;
                if (pose != current && !slots[ps].loop) slots[ps].p.SetTime(0);
                if (pose == "swim" || pose == "sneak") slots[ps].p.SetSpeed(Mathf.Clamp(speed / (pose == "swim" ? 1.6f : 1.2f), .5f, 1.6f));
            }
            else
            {
                float walk = Mathf.InverseLerp(.15f, 1.2f, speed), run = Mathf.InverseLerp(2.2f, 3.6f, speed), sprint = Mathf.InverseLerp(5.2f, 6.8f, speed);
                Set(want, "idle", 1 - walk); Set(want, "walk", walk * (1 - run)); Set(want, "run", walk * run * (1 - sprint)); Set(want, "sprint", walk * run * sprint);
                // The walk cycle is made for about 1.4 m/s, the jog for about 3.6, the sprint for about 6.5: the feet keep to the ground.
                Rate("walk", Mathf.Clamp(speed / 1.4f, .7f, 1.6f)); Rate("run", Mathf.Clamp(speed / 3.6f, .75f, 1.5f)); Rate("sprint", Mathf.Clamp(speed / 6.5f, .8f, 1.4f));
            }
            current = pose;
            float sum = 0;
            for (int i = 0; i < weights.Length; i++) { weights[i] = Mathf.MoveTowards(weights[i], want[i], dt * 4); sum += weights[i]; }
            for (int i = 0; i < weights.Length; i++)
            {
                mixer.SetInputWeight(i, sum > 0 ? weights[i] / sum : 0);
                var (p, c, loop) = slots[i];
                double t = p.GetTime();
                if (c.length > 0 && t > c.length) p.SetTime(loop ? t % c.length : c.length);
            }
        }
        void Set(float[] want, string slot, float w) { if (slotOf.TryGetValue(slot, out int i)) want[i] = w; }
        void Rate(string slot, float r) { if (slotOf.TryGetValue(slot, out int i)) slots[i].p.SetSpeed(r); }
    }
}
