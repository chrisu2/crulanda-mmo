using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A figure on GanzSe's body (Phase 8.5, step 1; <see cref="GanzSeKit"/>): the pack's skeleton copied bone by bone from its
    /// model (no instance of the whole file, which holds over three hundred pieces), its Humanoid avatar (so our clips play on it
    /// as they do on the Quaternius kit), the body, a face (eyes, brows, nose, ears, hair, a beard) and one piece per armour slot,
    /// each a skinned mesh from the file bound to the copied bones by name, so every piece fits by construction. The model faces
    /// +Z in its file (the demo's camera looks at it from +Z), so it is not turned. Bones are exposed in the same fields as the
    /// Quaternius kit's, so ActorVisual's frames and our weapons hang on it unchanged.
    /// </summary>
    public sealed partial class ModelFigure
    {
        /// <summary>Built on GanzSe's body.</summary>
        public bool GanzSe { get { return spec.ganzse; } }
        static GameObject gzModel; static Material gzMat; static Dictionary<string, SkinnedMeshRenderer> gzParts; static bool gzChecked;
        /// <summary>True when the pack's model and material are in the build (Resources/Characters/GanzSe).</summary>
        public static bool GanzSeAvailable
        {
            get
            {
                if (!gzChecked)
                {
                    gzChecked = true;
                    gzModel = Resources.Load<GameObject>(GanzSeKit.ModelPath); var asset = Resources.Load<Material>(GanzSeKit.MaterialPath);
                    // Matte like the Quaternius kit's skin and cloth (.25): the pack's .5 gloss shone its flat faces pale in the noon light.
                    if (asset != null) { gzMat = new Material(asset) { name = "GanzSe palette" }; gzMat.SetFloat("_Glossiness", GanzSeKit.Gloss); }
                    gzParts = new Dictionary<string, SkinnedMeshRenderer>();
                    if (gzModel != null) foreach (var r in gzModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (!gzParts.ContainsKey(r.name)) gzParts[r.name] = r;
                }
                return gzModel != null && gzMat != null && gzParts.ContainsKey(GanzSeKit.Body) && gzModel.transform.Find(GanzSeKit.SkeletonRoot) != null;
            }
        }
        /// <summary>The names of the skinned pieces in the pack's model as imported (for the tests: they must be <see cref="GanzSeKit.AllPieces"/>).</summary>
        public static ICollection<string> GanzSePieces { get { return GanzSeAvailable ? gzParts.Keys : (ICollection<string>)new string[0]; } }
        static readonly Dictionary<string, Material> gzMats = new Dictionary<string, Material>();
        /// <summary>The pack's palette material times a tint (white: the material itself); one per tint, shared.</summary>
        static Material GzMat(Color tint)
        {
            if (tint == Color.white || gzMat == null) return gzMat;
            string key = tint.r.ToString("0.###", CultureInfo.InvariantCulture) + "," + tint.g.ToString("0.###", CultureInfo.InvariantCulture) + "," + tint.b.ToString("0.###", CultureInfo.InvariantCulture);
            if (gzMats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(gzMat) { color = new Color(tint.r, tint.g, tint.b, 1), name = "GanzSe " + key }; gzMats[key] = m; return m;
        }

        /// <summary>The piece shown in each of the pack's slots, what the figure's own clothes put there, and what gear put there.</summary>
        readonly SkinnedMeshRenderer[] gzWorn = new SkinnedMeshRenderer[GanzSeKit.SlotCount];
        readonly string[] gzBase = new string[GanzSeKit.SlotCount], gzGear = new string[GanzSeKit.SlotCount];
        /// <summary>The body (skin, and the pack's own underclothes).</summary>
        public SkinnedMeshRenderer GzBody;
        /// <summary>The piece shown in a slot now (null: bare).</summary>
        public string Worn(GanzSeKit.Slot slot) { var r = gzWorn[(int)slot]; return r != null ? r.name : null; }
        /// <summary>What the figure's own clothes put in a slot (null: bare).</summary>
        public string BaseOf(GanzSeKit.Slot slot) { return gzBase[(int)slot]; }
        /// <summary>Gear's piece in a slot (null takes it off: the figure's own clothes show again).</summary>
        public void Wear(GanzSeKit.Slot slot, string piece) { if (!spec.ganzse) return; gzGear[(int)slot] = piece; ShowSlot(slot); }
        void ShowSlot(GanzSeKit.Slot slot)
        {
            int i = (int)slot; string want = gzGear[i] ?? gzBase[i]; var now = gzWorn[i];
            if (now != null && now.name == want) return;
            if (now != null) { Renderers.Remove(now); Kill(now.gameObject); gzWorn[i] = null; }
            if (want != null) gzWorn[i] = AddPiece(want, Color.white);
        }

        /// <summary>Builds a figure on GanzSe's body under <paramref name="body"/> (the actor's "Body", 1 m above the soles); null if the pack is missing.</summary>
        public static ModelFigure BuildGanzSe(Transform body, Spec s)
        {
            if (!GanzSeAvailable) return null;
            s.ganzse = true;
            var f = new ModelFigure { spec = s };
            var go = new GameObject("Model"); f.Model = go.transform; f.Model.SetParent(body, false);
            f.Model.localPosition = new Vector3(0, -1, 0); f.Model.localRotation = Quaternion.identity; f.Model.localScale = Vector3.one * (s.scale > 0 ? s.scale : 1);
            // The skeleton, copied (names and rest transforms), so the avatar's paths ("Base Character Root/spine_01/...") are the file's.
            Copy(gzModel.transform.Find(GanzSeKit.SkeletonRoot), f.Model);
            f.bones = new Dictionary<string, Transform>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (!f.bones.ContainsKey(t.name)) f.bones[t.name] = t;
            // The body and the face. The skin: the figure's tone (SpecFor's, measured from the old palette's fairest) times what takes
            // the pack's pale skin swatch to the Quaternius kit's skin, so both kits show a person in the same tone (wardrobe-ganzse-1:
            // the swatch untinted read near white).
            var skin = GanzSeKit.Skin(s.skin);
            f.GzBody = f.AddPiece(GanzSeKit.Body, skin);   // not the Torso: nothing dyes the pack's body (Cover); its pieces are the cover
            int seed = Mathf.Abs(s.seed), hairColour = GanzSeKit.HairColour(s.hairColour);
            f.AddPiece(GanzSeKit.Nose(1 + seed % GanzSeKit.Noses), skin);
            f.AddPiece(GanzSeKit.EarsOf(1 + seed / 5 % GanzSeKit.Ears), skin);
            f.AddPiece(GanzSeKit.Eyes(1 + seed / 3 % GanzSeKit.FaceTypes, GanzSeKit.EyeColour(seed)), Color.white);
            var brows = f.AddPiece(GanzSeKit.Brows(1 + seed / 2 % GanzSeKit.FaceTypes, hairColour), Color.white); if (brows != null) f.Hair.Add(brows);
            int style = GanzSeKit.HairStyle(s.hair);
            if (style > 0) { var h = f.AddPiece(GanzSeKit.Hair(style, hairColour), Color.white); if (h != null) f.Hair.Add(h); }
            if (s.beard && !s.female) { var b = f.AddPiece(GanzSeKit.Beard(GanzSeKit.BeardStyle(seed), hairColour), Color.white); if (b != null) f.Hair.Add(b); }
            // The head alone, never drawn: what hats and hoods are measured against (ActorVisual.Outline), as the old kit's cut head is.
            var head = f.AddPiece(GanzSeKit.Body, skin);
            if (head != null) { head.name = "Head (measure)"; head.sharedMesh = HeadOnly(head); head.forceRenderingOff = true; head.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; f.Head = head; }
            // Their own clothes: what shows in a slot no gear covers.
            if (s.outfit != null) foreach (var w in s.outfit) f.gzBase[(int)w.slot] = w.Piece;
            for (int i = 0; i < GanzSeKit.SlotCount; i++) f.ShowSlot((GanzSeKit.Slot)i);
            // The Animator last, once the renderers are under it (wardrobe-ganzse-1, every figure in a T-pose while the Quaternius ones
            // idled): an Animator culls by the renderers it found when it was bound, and added to a bare skeleton it found none, so with
            // CullUpdateTransforms it counted itself never seen and never wrote a pose. The Quaternius figure's Animator comes with
            // its file, renderers and all. Rebind after the avatar, so the bindings and the renderer list are this hierarchy's.
            f.Animator = go.AddComponent<Animator>();
            var a = gzModel.GetComponent<Animator>(); if (a != null && a.avatar != null) f.Animator.avatar = a.avatar;
            f.Animator.applyRootMotion = false; f.Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; f.Animator.runtimeAnimatorController = null;
            f.Animator.Rebind();
            f.FindBonesGanzSe();
            f.Cloud = null;   // nothing is fitted to a cloud: the pack's pieces fit; what stays drawn hangs rigid on the frames
            return f;
        }
        static void Copy(Transform src, Transform parent)
        {
            if (src == null) return;
            var t = new GameObject(src.name).transform; t.SetParent(parent, false);
            t.localPosition = src.localPosition; t.localRotation = src.localRotation; t.localScale = src.localScale;
            for (int i = 0; i < src.childCount; i++) Copy(src.GetChild(i), t);
        }
        /// <summary>A piece of the pack's model onto this figure: its mesh, bound to this skeleton's bones by name, in the palette
        /// times <paramref name="tint"/> (a Hollow Man's apparition instead). Null when the file has no such piece.</summary>
        SkinnedMeshRenderer AddPiece(string name, Color tint)
        {
            if (name == null || gzParts == null || !gzParts.TryGetValue(name, out var src) || src.sharedMesh == null) return null;
            var g = new GameObject(name); var t = g.transform; t.SetParent(Model, false);
            t.localPosition = src.transform.localPosition; t.localRotation = src.transform.localRotation; t.localScale = src.transform.localScale;
            var r = g.AddComponent<SkinnedMeshRenderer>(); r.sharedMesh = src.sharedMesh;
            var from = src.bones; var to = new Transform[from.Length];
            for (int i = 0; i < from.Length; i++) to[i] = from[i] != null && bones.TryGetValue(from[i].name, out var b) ? b : Model;
            r.bones = to; r.rootBone = src.rootBone != null && bones.TryGetValue(src.rootBone.name, out var rb) ? rb : Model;
            r.localBounds = src.localBounds; r.updateWhenOffscreen = false;
            var mats = new Material[Mathf.Max(1, src.sharedMesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = spec.stone ? Ghost(name.StartsWith("Eyes")) : GzMat(tint);
            r.sharedMaterials = mats;
            if (spec.stone) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            Renderers.Add(r);
            return r;
        }
        /// <summary>The pack's bones in the kit's fields: spine_01 is its hips (the legs hang from it), spine_02 the waist, spine_04 the
        /// top of the chest; forearm and shin for the lower limbs, toes for the balls of the feet.</summary>
        void FindBonesGanzSe()
        {
            Hips = B("spine_01", HumanBodyBones.Hips); Spine = B("spine_02", HumanBodyBones.Spine); Chest = B("spine_04", HumanBodyBones.UpperChest); Neck = B("neck", HumanBodyBones.Neck); HeadBone = B("head", HumanBodyBones.Head);
            UpperArmL = B("upperarm_l", HumanBodyBones.LeftUpperArm); UpperArmR = B("upperarm_r", HumanBodyBones.RightUpperArm); LowerArmL = B("forearm_l", HumanBodyBones.LeftLowerArm); LowerArmR = B("forearm_r", HumanBodyBones.RightLowerArm);
            HandL = B("hand_l", HumanBodyBones.LeftHand); HandR = B("hand_r", HumanBodyBones.RightHand); MiddleL = B("middle_01_l", HumanBodyBones.LeftMiddleProximal); MiddleR = B("middle_01_r", HumanBodyBones.RightMiddleProximal);
            UpperLegL = B("upperleg_l", HumanBodyBones.LeftUpperLeg); UpperLegR = B("upperleg_r", HumanBodyBones.RightUpperLeg); LowerLegL = B("shin_l", HumanBodyBones.LeftLowerLeg); LowerLegR = B("shin_r", HumanBodyBones.RightLowerLeg);
            FootL = B("foot_l", HumanBodyBones.LeftFoot); FootR = B("foot_r", HumanBodyBones.RightFoot); BallL = B("toes_l", HumanBodyBones.LeftToes); BallR = B("toes_r", HumanBodyBones.RightToes);
            var fn = new[] { "index", "middle", "ring", "pinky" };
            var hb = new[,] { { HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal }, { HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal }, { HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal }, { HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal } };
            FingersR = new Transform[4, 3]; FingersL = new Transform[4, 3];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 3; j++)
                {
                    FingersR[i, j] = B(fn[i] + "_0" + (j + 1) + "_r", hb[i, j]);
                    FingersL[i, j] = B(fn[i] + "_0" + (j + 1) + "_l", (HumanBodyBones)((int)hb[i, j] - 15));   // the left hand's bones are 15 before the right's
                }
        }
    }
}
