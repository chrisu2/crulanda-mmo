using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// People as models (playtest note 12; <see cref="ModelFigure"/>). The figure is the kit's, moved by its clips; what it wears
    /// and carries is the game's own and keeps its numbers: the old figure's pivots stay, as frames on the model's bones.
    ///
    /// Frames: "Arm L/R" at the shoulder joint with "Forearm" at the elbow and "Hand" at the wrist under it, "Leg L/R" at the hip
    /// with "Shin" and "Foot", each turned so its -Y runs down the bone and +Z faces front (the old pivots' rest), each limb scaled
    /// by its length against the old one (.57 shoulder to wrist, .84 hip to ankle), so a hand-held weapon, a vambrace or a greave
    /// built for the old figure sits on the model's limb. "Head frame", "Chest frame" and "Hips frame" carry the old body's space
    /// onto the model's head, chest and hips (the old head was a ball .3 across; the model's head is half as wide), for what hangs
    /// on the body: armour on the head, chest, shoulders and neck, the class and trade kit, slung weapons. Every frame copies its
    /// bone each frame, after the clips have played (LateUpdate).
    ///
    /// Poses: sitting, swimming, sneaking, talking, kneeling to gather and dying are the kit's clips; hammering, chopping, hoeing,
    /// kneading, cowering, drinking and slumping put the old pose's arm angles onto the model's arms over the standing idle.
    /// </summary>
    public sealed partial class ActorVisual
    {
        /// <summary>People are built as models when the kit is in the build; off, the smooth figure (read when a figure is built).</summary>
        public static bool Models = true;
        ModelFigure model;
        /// <summary>The modelled figure, for captures and tests (null on a primitive figure).</summary>
        public ModelFigure Figure { get { return model; } }
        /// <summary>The figure's model (null on a smooth, block or beast figure).</summary>
        public ModelFigure Model { get { return model; } }
        Transform headFrame, faceFrame, chestFrame, hipsFrame, handleR;
        struct Follow { public Transform frame, bone; public Quaternion rot; public Vector3 pos; public bool place; }
        /// <summary>Each frame's place in the model root's space in the bind pose: armour built on a frame is fitted from there (ModelArmour).</summary>
        readonly Dictionary<Transform, Matrix4x4> frameBind = new Dictionary<Transform, Matrix4x4>();
        readonly List<Follow> follows = new List<Follow>();
        Crulanda.Gameplay.Actor actorRef; float armWeight, swimLift; bool dropped, leaned; Vector3 poseArmL, poseArmR; float poseElbowL, poseElbowR;
        /// <summary>What the hands carry, for a model's arms (WorldLife sets it with the load): none; beside the right hip (a basket,
        /// a bucket, a hide, herbs, game); in front in both arms (bread, goods); on the right shoulder (a sack, logs).</summary>
        public enum Carrying { None, Side, Front, Shoulder }
        public Carrying Carry;
        /// <summary>Where a thing carried in the right hand hangs (a tankard): its (0, -.62, 0) is the hand, as on the right arm's
        /// pivot, but it rides the forearm, so it stays in the hand as the elbow bends.</summary>
        public Transform RightHandle { get { return handleR != null ? handleR : armR; } }

        static bool Person(ActorLook look) { return !IsBeast(look) && look != ActorLook.Pale && look != ActorLook.Keeper && look != ActorLook.WeaveEater && (look != ActorLook.Skeleton || !ModelBeast.Available("Skeleton")); }

        static readonly string[] FemaleNames = { "Ama", "Hedda", "Sel", "Lisbet", "Ilse", "Maud", "Edda", "Nettie", "Tamsin", "Grete", "Wenna", "Goody", "Hettie", "Nan", "Mother", "Old Sorrel", "Sorrel", "Mistress", "Dame", "Sister", "Widow" };
        static readonly string[] MaleNames = { "Brannoc", "Wil", "Garet", "Old Tobin", "Tobin", "Osk", "Corwin", "Aldo", "Hob", "Fen", "Jory", "Pim", "Chieftain", "Brother", "Master", "Father" };
        /// <summary>Whether a person is a woman: by the name (first word, or a title) when it says, else by the trade, else by the variant.</summary>
        bool Female(ActorLook look)
        {
            switch (look)
            {
                case ActorLook.Druid: case ActorLook.Healer: case ActorLook.Mage: return true;
                case ActorLook.Villager: break;
                case ActorLook.Deserter: return (FigureSeed() & 7) == 3;
                default: return false;
            }
            if (role == "henwife") return true;
            if (role == "blacksmith" || role == "lumberjack" || role == "miller" || role == "elder" && variant % 2 == 0) return false;
            string n = name ?? "";
            foreach (var f in FemaleNames) if (n == f || n.StartsWith(f + " ")) return true;
            foreach (var m in MaleNames) if (n == m || n.StartsWith(m + " ")) return false;
            return (Mathf.Abs(variant * 7 + 3) % 2) == 1;
        }
        static Color Gain(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k); }
        /// <summary>A cloth tint from a colour: the hue as a dye (<see cref="ModelFigure.Dye"/>), and dark colours darken the cloth.</summary>
        static Color ClothTint(Color c, float strength)
        {
            float k = Mathf.Clamp(Mathf.Lerp(1, c.maxColorComponent / .7f, .8f), .3f, 1.1f);   // soot black and charcoal come out dark on bleached cloth
            return Gain(ModelFigure.Dye(c, strength), k);
        }
        /// <summary>What this person's model is: the outfit, sex, hair, hood and dyes, from the look, the trade and the variant.</summary>
        ModelFigure.Spec SpecFor(ActorLook look, Color clothC, Color accentC, Color legC, Color skinC)
        {
            var s = new ModelFigure.Spec { female = Female(look), hair = Mathf.Abs(variant) % 5, kit = ModelFigure.Kit.Peasant, scale = 1 };
            if (look == ActorLook.Villager) s.scale = .96f + Mathf.Abs(variant * 37 + 11) % 9 * .01f;   // a village is not all one height
            // Skin: the kit's texture times the old palette's tone against its fairest (so the four tones keep their spread).
            var fair = new Color(.86f, .72f, .6f);
            s.skin = new Color(Mathf.Min(1.05f, skinC.r / fair.r), Mathf.Min(1.05f, skinC.g / fair.g), Mathf.Min(1.05f, skinC.b / fair.b));
            // Hair: the kit's grey hair times the old colour, lifted (the texture is about half grey).
            s.hairColour = Gain(HairColors[Mathf.Abs(variant * 3 + 1) % HairColors.Length], 1.7f);
            s.shirt = ClothTint(clothC, .7f); s.breeches = ClothTint(legC, .45f); s.hoodColour = ClothTint(accentC, .75f);
            s.beard = !s.female && Mathf.Abs(variant * 5 + 1) % 4 == 0;
            switch (look)
            {
                case ActorLook.Warrior: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.hair = 0; s.beard = false; break;   // his blue
                case ActorLook.Druid: s.kit = ModelFigure.Kit.Ranger; s.hood = true; s.hoodColour = ClothTint(clothC, .8f); s.hair = 1; break;
                case ActorLook.Paladin: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.hair = 3; s.beard = true; break;
                case ActorLook.Ranger: s.kit = ModelFigure.Kit.Ranger; s.hood = true; s.hoodColour = ClothTint(clothC, .8f); s.hair = 2; break;
                case ActorLook.SaltMender: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.shirt = ClothTint(clothC, .2f); s.hood = true; s.hoodColour = ClothTint(accentC, .75f); s.hair = 0; s.beard = true; break;   // an elder of the West, salt-white
                case ActorLook.Mage: s.kit = ModelFigure.Kit.Peasant; s.hood = true; s.hoodColour = ClothTint(accentC, .75f); s.hair = 1; break;
                case ActorLook.Healer: s.hood = true; s.hoodColour = ClothTint(clothC, .8f); s.hair = 2; break;
                case ActorLook.Collector: case ActorLook.Warden: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.shirt = ClothTint(new Color(.93f, .93f, .95f), .15f); break;   // Concord white, for the tabard
                case ActorLook.Sentry: s.kit = ModelFigure.Kit.Ranger; break;
                case ActorLook.Outrider: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.hood = true; s.hoodColour = ClothTint(clothC, .7f); break;
                case ActorLook.Cultist: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.hood = true; s.hoodColour = ClothTint(clothC, .8f); break;
                case ActorLook.Hollow: s.stone = true; break;
                case ActorLook.Deserter: s.kit = (FigureSeed() & 1) == 0 ? ModelFigure.Kit.Ranger : ModelFigure.Kit.Peasant; s.bleach = true; s.hood = (FigureSeed() & 6) == 2; s.hoodColour = ClothTint(clothC, .6f); break;
                case ActorLook.BanditKing: s.kit = ModelFigure.Kit.Ranger; s.bleach = true; s.beard = true; s.hair = 3; break;
                case ActorLook.Villager: s = TradeSpec(s, variant); break;
            }
            // On a peasant's outfit only the hood is the ranger's: bleached, it takes its colour true (Mira's cream, a hen-wife's
            // red kerchief), not the ranger's green under a tint.
            if (s.kit == ModelFigure.Kit.Peasant) s.bleach = true;
            if (orc) Orcish(ref s);
            return s;
        }
        /// <summary>Orc skin: the kit's pale skin times a green (moss, sage, olive, a darker fen green), by the variant.</summary>
        static readonly Color[] OrcSkins = { new Color(.74f, 1.18f, .9f), new Color(.7f, 1.1f, .84f), new Color(.78f, 1.2f, .92f), new Color(.66f, 1.04f, .8f) };   // the kit's pale skin to the orc head's olive (OrcHead)
        /// <summary>How much taller an orc is drawn than a man (a woman: <see cref="OrcWomanScale"/>). The canon's orcs stand about
        /// seven feet (world_bible: Gorm); the kit's man is about six.</summary>
        public const float OrcScale = 1.15f, OrcWomanScale = 1.1f;
        /// <summary>
        /// An orc (Chris, 2026-10-13: "shaman is not an orc"; Docs/RACES.md, step 3): CANON as a people (green, tusked, about seven feet:
        /// Gorm, Krunk). Built on the same humanoid body and clips as everyone else, so it wears the same gear: green skin, taller, no
        /// beard, the head shaved or cropped (a woman's tied back), dark hair; the tusks are hung on the head after (<see cref="Tusks"/>).
        /// </summary>
        void Orcish(ref ModelFigure.Spec s)
        {
            s.skin = OrcSkins[Mathf.Abs(variant) % OrcSkins.Length]; s.scale = (s.female ? OrcWomanScale : OrcScale) * (s.scale > 0 ? s.scale : 1);
            s.beard = false; s.hair = s.female ? 2 : (Mathf.Abs(variant) % 2 == 0 ? 4 : 3); s.hairColour = new Color(.42f, .38f, .34f);
            if (OrcKit) s.hood = false;   // the orc's own head and topknot (OrcHead): a man's hood does not go over it
        }
        static Mesh orcHead, orcHair; static Material orcSkin; static bool orcLoaded;
        /// <summary>The orc's head from the pack, baked (Editor/OrcHeadBake.cs; Resources/Characters/Orc): head, eyes, teeth and tusks, and
        /// its hair, in the frame of its Head bone; the skin recoloured green from the pack's pink.</summary>
        public static bool OrcKit
        {
            get
            {
                if (!orcLoaded) { orcLoaded = true; orcHead = Resources.Load<Mesh>("Characters/Orc/OrcHead"); orcHair = Resources.Load<Mesh>("Characters/Orc/OrcHair"); orcSkin = Resources.Load<Material>("Characters/Orc/OrcSkin"); }
                return orcHead != null && orcSkin != null;
            }
        }
        /// <summary>The pack's head to a man's: its crown .257 above its Head bone, a man's about .215, so about the same height, but broader
        /// and deeper, with the brow and jaw forward.</summary>
        public const float OrcHeadFit = .7f, OrcHeadWide = 1.2f, OrcHeadLift = .045f;   // its jaw is long: smaller, and lifted so the chin clears the collar (22:20 frames)
        /// <summary>
        /// The orc's own head (2026-10-13, Chris chose "Orc face" after the first pass's tusks on a man's head): the pack's head, eyes, teeth,
        /// tusks and topknot hung on the Head bone at its place, turned with the body, sized by <see cref="OrcHeadFit"/>; the man's head,
        /// eyes, brows and hair are kept from drawing (forceRenderingOff, which nothing that shows or hides a figure touches). False without
        /// the kit (the tusks then).
        /// </summary>
        bool OrcHead(ModelFigure m)
        {
            if (!OrcKit) return false;
            float s = m.Model.localScale.y;
            foreach (var (mesh, label) in new[] { (orcHead, "Orc head"), (orcHair, "Orc hair") })
            {
                if (mesh == null) continue;
                var go = new GameObject(label); go.transform.SetParent(body, false);
                go.transform.SetPositionAndRotation(m.HeadBone.position + body.up * OrcHeadLift * s, body.rotation); go.transform.localScale = Vector3.one * OrcHeadFit * s;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); var mats = new Material[mesh.subMeshCount]; for (int i = 0; i < mats.Length; i++) mats[i] = orcSkin; r.sharedMaterials = mats;
                go.transform.SetParent(m.HeadBone, true);
                m.Renderers.Add(r);
            }
            if (m.Head != null) m.Head.forceRenderingOff = true;
            foreach (var h in m.Hair) if (h != null) h.forceRenderingOff = true;
            foreach (var r in m.Renderers) if (r != null && r.name.Contains("Eye")) r.forceRenderingOff = true;
            return true;
        }
        static Material tuskMat;
        /// <summary>An orc's two tusks: ivory points standing up from the corners of the lower jaw, a little forward and out, hung on the
        /// Head bone. Made in the bind pose, like the frames (the jaw about 3 cm above the Head bone, the face about 9 cm in front).</summary>
        void Tusks(ModelFigure m)
        {
            if (tuskMat == null) { tuskMat = new Material(Shader.Find("Standard")) { color = new Color(.93f, .89f, .76f), name = "Orc tusk" }; tuskMat.SetFloat("_Glossiness", .35f); }
            float s = m.Model.localScale.y; var jaw = body.InverseTransformPoint(m.HeadBone.position);
            foreach (float side in new[] { -1f, 1f })
            {
                var t = GameObject.CreatePrimitive(PrimitiveType.Capsule); t.name = "Tusk " + (side < 0 ? "L" : "R");
                var c = t.GetComponent<Collider>(); if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
                t.transform.SetParent(body, false);
                t.transform.localPosition = jaw + new Vector3(side * .03f, .035f, .088f) * s;
                t.transform.localRotation = Quaternion.Euler(-18, 0, side * -12);
                t.transform.localScale = new Vector3(.013f, .022f, .013f) * s;
                t.transform.SetParent(m.HeadBone, true);
                var r = t.GetComponent<MeshRenderer>(); r.sharedMaterial = tuskMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m.Renderers.Add(r);
            }
        }
        /// <summary>A trade's model: the outfit and its dyes (the old outfit's colours), a hood for the hooded trades, a beard,
        /// grey hair for an elder.</summary>
        ModelFigure.Spec TradeSpec(ModelFigure.Spec spec, int v)
        {
            var s = spec;
            Color C(float r, float g, float b) { return new Color(r, g, b); }
            void Wear(Color shirt, Color legs) { s.shirt = ClothTint(shirt, .7f); s.breeches = ClothTint(legs, .45f); }
            void Hood(Color c) { s.hood = true; s.hoodColour = ClothTint(c, .8f); }
            switch (role)
            {
                case "blacksmith": Wear(C(.24f, .22f, .22f), C(.2f, .17f, .15f)); s.beard = true; s.hair = 3; break;
                case "merchant": Wear(C(.5f, .05f, .12f), C(.16f, .12f, .2f)); break;
                case "baker": Wear(C(.92f, .9f, .84f), C(.3f, .42f, .62f)); break;
                case "henwife": Wear(C(.58f, .24f, .2f), C(.17f, .29f, .54f)); Hood(C(.82f, .14f, .1f)); break;
                case "farmer": Wear(v % 2 == 0 ? C(.74f, .52f, .2f) : C(.3f, .52f, .26f), C(.17f, .28f, .52f)); break;
                case "hunter": s.kit = ModelFigure.Kit.Ranger; Wear(C(.18f, .42f, .17f), C(.36f, .24f, .13f)); Hood(C(.12f, .36f, .15f)); break;
                case "leatherworker": Wear(C(.76f, .52f, .24f), C(.36f, .24f, .14f)); break;
                case "skinner": Wear(C(.44f, .3f, .2f), C(.27f, .2f, .15f)); break;
                case "lumberjack": Wear(C(.64f, .1f, .08f), C(.18f, .26f, .46f)); s.beard = true; break;
                case "herbalist": Wear(C(.3f, .52f, .26f), C(.2f, .42f, .22f)); Hood(C(.16f, .42f, .2f)); break;
                case "miller": Wear(C(.86f, .82f, .72f), C(.5f, .36f, .2f)); break;
                case "elder": Wear(C(.4f, .22f, .48f), C(.3f, .25f, .2f)); s.hairColour = Gain(C(.8f, .8f, .78f), 1.25f); s.beard = !s.female; break;
                case "stranger": s.kit = ModelFigure.Kit.Ranger; s.bleach = true; Wear(C(.2f, .2f, .22f), C(.16f, .15f, .15f)); Hood(C(.17f, .17f, .19f)); break;
                case "pilgrim": Wear(C(.86f, .58f, .16f), C(.7f, .56f, .34f)); Hood(C(.84f, .62f, .24f)); break;
                case "warden": s.kit = ModelFigure.Kit.Ranger; Wear(C(.2f, .46f, .18f), C(.36f, .24f, .13f)); Hood(C(.16f, .38f, .14f)); break;
                case "innkeeper": Wear(C(.9f, .86f, .74f), C(.26f, .2f, .17f)); s.beard = !s.female; break;
                case "drinker": if (v % 2 == 0) Hood(VillagerCloth[Mathf.Abs(v * 3 + 2) % VillagerCloth.Length]); break;
                default: if (v % 4 == 2) Hood(VillagerCloth[Mathf.Abs(v * 3 + 2) % VillagerCloth.Length]); break;   // plain folk: a headscarf
            }
            return s;
        }

        /// <summary>Builds the model and its frames (false when the kit is missing: the smooth figure is built instead).</summary>
        bool BuildModel(ActorLook look, Color clothC, Color accentC, Color legC, Color skinC)
        {
            if (!Models || !Person(look) || !ModelFigure.Available) return false;
            var m = ModelFigure.Build(body, SpecFor(look, clothC, accentC, legC, skinC));
            if (m == null) return false;
            if (!m.Complete) { var g = m.Model.gameObject; if (Application.isPlaying) Destroy(g); else DestroyImmediate(g); return false; }
            model = m; actorRef = GetComponent<Crulanda.Gameplay.Actor>();
            if (child) m.HeadBone.localScale = Vector3.one * ChildHead;   // a child's head is large for the body (the clips turn bones, they never scale them)
            Frames();
            if (orc && !OrcHead(m)) Tusks(m);
            // What armour recolours (Cover): the shirt, the sleeves (and, for gloves, the bare hands), the breeches, the shoes.
            baseChest = new[] { m.Torso }; baseSleeves = new[] { m.Arms }; baseHands = new[] { m.Arms }; baseLegs = new[] { m.Legs }; baseBoots = new[] { m.Feet };
            baseBelt = null;   // the outfit's belts go under a chest piece (ModelFigure.ShowExtras)
            var hair = new List<Transform>(); foreach (var r in m.Hair) if (r.name.Contains("Hair_") && !r.name.Contains("Beard")) hair.Add(r.transform);
            hairParts = hair.ToArray();
            head = headFrame; torso = null;
            m.StartMotion(name);
            return true;
        }
        /// <summary>How much larger a child's head is drawn (the body is the adult's, scaled down with the Body).</summary>
        public const float ChildHead = 1.22f;
        /// <summary>The frames, made in the model's bind pose (arms out, legs straight).</summary>
        void Frames()
        {
            var m = model; float s = m.Model.localScale.y;
            Vector3 L(Transform t) { return body.InverseTransformPoint(t.position); }
            (armL, foreL, handL) = Limb("L", "Arm", "Forearm", "Hand", m.UpperArmL, m.LowerArmL, m.HandL, .57f);
            (armR, foreR, handR) = Limb("R", "Arm", "Forearm", "Hand", m.UpperArmR, m.LowerArmR, m.HandR, .57f);
            (legL, shinL, footL) = Limb("L", "Leg", "Shin", "Foot", m.UpperLegL, m.LowerLegL, m.FootL, .84f);
            (legR, shinR, footR) = Limb("R", "Leg", "Shin", "Foot", m.UpperLegR, m.LowerLegR, m.FootR, .84f);
            // The tankard's handle: on the right forearm, where the shoulder would be with the arm straight.
            handleR = new GameObject("Handle R").transform; handleR.SetParent(foreR, false); handleR.localPosition = -foreR.localPosition;
            // Head: the old head's middle (.8 up) onto the model's (about .09 above its Head bone); the old head was a ball .3
            // across and .32 tall, the model's is about .17 across, .25 tall and .21 deep.
            float hs = m.HeadBone.localScale.y;   // a child's larger head
            var sh = new Vector3(.58f, .8f, .72f) * hs;
            if (orc && OrcKit) { sh.x *= OrcHeadWide; sh.z *= OrcHeadWide; }   // what an orc wears on its head, as broad as its head
            var headAt = L(m.HeadBone) + new Vector3(0, .12f * s, .01f * s) * hs;   // a little high, so a helmet's rim clears the eyes
            headFrame = Region("Head frame", m.HeadBone, headAt - new Vector3(0, .8f * sh.y, 0), sh);
            headOutline = Outline(false); hairOutline = Outline(true);
            // Face: the old face (eyes at .835, mouth at .745) onto the model's (eyes about .1 above the Head bone, mouth .04), for
            // what is worn over it: a mask, a scarf over the nose and mouth, a tear.
            var fs = new Vector3(.58f, .67f, .72f) * hs;
            faceFrame = Region("Face frame", m.HeadBone, L(m.HeadBone) + new Vector3(0, .077f * s, .01f * s) * hs - new Vector3(0, .8f * fs.y, 0), fs);
            // Chest: the old shoulder line (.53) and belt (.06) onto the model's shoulder joints and lower spine; the width from
            // the shoulders (the old pivots at .31 onto the outside of the model's).
            var shL = L(m.UpperArmL); var shR = L(m.UpperArmR); float shoulderY = (shL.y + shR.y) / 2, waistY = L(m.Spine).y;
            float cx = ((Mathf.Abs(shL.x) + Mathf.Abs(shR.x)) / 2 + .02f * s) / .31f, cy = (shoulderY - waistY) / .47f;
            var sc = new Vector3(cx, cy, Mathf.Min(cy, cx * 1.1f));
            chestFrame = Region("Chest frame", m.Chest, new Vector3(0, shoulderY - .53f * cy, L(m.Chest).z), sc);
            // Hips: the old hip pivots (-.08) onto the model's hip joints, as tall as the legs' scale.
            float ky = legL.localScale.y; var thigh = (L(m.UpperLegL) + L(m.UpperLegR)) / 2;
            hipsFrame = Region("Hips frame", m.Hips, new Vector3(0, thigh.y + .08f * ky, thigh.z), new Vector3(cx, ky, sc.z));
        }
        (Transform, Transform, Transform) Limb(string side, string a, string b, string c, Transform upper, Transform lower, Transform end, float oldLength)
        {
            Vector3 pa = upper.position, pb = lower.position, pc = end.position;
            float len = (body.InverseTransformPoint(pb) - body.InverseTransformPoint(pa)).magnitude + (body.InverseTransformPoint(pc) - body.InverseTransformPoint(pb)).magnitude;
            var fwd = body.forward;
            var pivot = Frame(a + " " + side, body, upper, pa, Quaternion.LookRotation(fwd, pa - pb), Vector3.one * (len / oldLength), true);
            var mid = Frame(b + " " + side, pivot, lower, pb, Quaternion.LookRotation(fwd, pb - pc), Vector3.one, false);
            var e = Frame(c + " " + side, mid, end, pc, Quaternion.LookRotation(fwd, pb - pc), Vector3.one, false);
            return (pivot, mid, e);
        }
        /// <summary>A frame on <paramref name="bone"/>, now at <paramref name="at"/> turned <paramref name="rot"/> (world), keeping that place on the bone.</summary>
        Transform Frame(string name, Transform parent, Transform bone, Vector3 at, Quaternion rot, Vector3 localScale, bool place)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localScale = localScale; t.SetPositionAndRotation(at, rot);
            frameBind[t] = model.Model.worldToLocalMatrix * t.localToWorldMatrix;
            follows.Add(new Follow { frame = t, bone = bone, rot = Quaternion.Inverse(bone.rotation) * rot, pos = bone.InverseTransformPoint(at), place = place });
            return t;
        }
        /// <summary>A frame carrying the old body's space onto a region of the model: at <paramref name="localPos"/> in the body, unturned, scaled.</summary>
        Transform Region(string name, Transform bone, Vector3 localPos, Vector3 scale)
        {
            return Frame(name, body, bone, body.TransformPoint(localPos), body.rotation, scale, true);
        }
        /// <summary>The frames copy their bones (after the clips and the pose's arms).</summary>
        void SyncFrames()
        {
            for (int i = 0; i < follows.Count; i++)
            {
                var f = follows[i];
                if (f.place) f.frame.SetPositionAndRotation(f.bone.TransformPoint(f.pos), f.bone.rotation * f.rot);
                else f.frame.rotation = f.bone.rotation * f.rot;
            }
        }
        Follow FollowOf(Transform frame) { foreach (var f in follows) if (f.frame == frame) return f; return default; }
        /// <summary>Where a body slot's gear root hangs: the head frame for the head, the chest frame for the rest (the body on other figures).</summary>
        Transform SlotParent(EquipSlot slot)
        {
            if (model == null) return body;
            return slot == EquipSlot.Head ? headFrame : slot == EquipSlot.Legs || slot == EquipSlot.Feet ? hipsFrame : chestFrame;
        }
        /// <summary>
        /// After the look's kit is built: what it hung on the body moves to the frame of its region (by its height in the old
        /// body: the head above .62, the hips below -.1, else the chest), keeping its place in the old body's space.
        /// </summary>
        void Remap()
        {
            var parts = new List<Transform>();
            for (int i = 0; i < body.childCount; i++)
            {
                var c = body.GetChild(i);
                if (c == model.Model || c == headFrame || c == faceFrame || c == chestFrame || c == hipsFrame || c == armL || c == armR || c == legL || c == legR) continue;
                parts.Add(c);
            }
            var hats = new List<Transform>();
            var kit = new HashSet<Transform>();   // what is carried (in hand, slung, the class kit) is never left off
            foreach (var set in new[] { held, stowed, classKit }) if (set != null) foreach (var t in set) if (t != null) kit.Add(t);
            foreach (var c in parts)
            {
                bool onHead = c.localPosition.y >= .62f;
                // A round ball over the head was the old figure's hood or head-wrap: the model wears the outfit's hood in its colour.
                var ball = c.GetComponent<MeshFilter>() != null && c.GetComponent<MeshFilter>().sharedMesh != null && c.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Sphere");
                if (onHead && ball && !kit.Contains(c) && Mathf.Abs(c.localPosition.x) < .1f && c.localScale.x >= .3f && c.localScale.y >= c.localScale.x * .85f && c.localPosition.y < .95f)
                {
                    var r = c.GetComponent<Renderer>(); model.AddHood(ClothTint(r != null && r.sharedMaterial != null ? r.sharedMaterial.color : Color.gray, .8f));
                    Kill(c); continue;   // hidden and unparented now (a villager caches its renderers next)
                }
                // A flat board (an apron, a tabard, a cape, a shawl) or a skirt's drum would stand off the model's own clothes:
                // left off; what is small (a pouch, a sigil, a strap, keys, a quiver, a bow) stays.
                var mesh = c.GetComponent<MeshFilter>() != null ? c.GetComponent<MeshFilter>().sharedMesh : null; var k = c.localScale;
                bool board = mesh != null && (mesh.name.StartsWith("Cube") && (k.x >= .45f || k.x >= .3f && k.y >= .3f) || mesh.name.StartsWith("Cylinder") && k.x >= .45f
                    || mesh.name.StartsWith("Sphere") && k.x >= .2f);   // and a ball on the body (a pelt, a bark or leather pauldron, a belly)
                if (!onHead && board && !kit.Contains(c)) { Kill(c); continue; }
                bool onFace = onHead && c.localPosition.y < .87f && c.localPosition.z >= .09f;   // in front of the face: a mask, a scarf, a tear
                c.SetParent(onFace ? faceFrame : onHead ? headFrame : c.localPosition.y < -.1f ? hipsFrame : chestFrame, false);
                if (onHead && !onFace && !kit.Contains(c) && c.localPosition.y >= .84f && (Mathf.Abs(c.localPosition.x) < .14f || c.localPosition.y >= .95f)) hats.Add(c);
            }
            string Kind(Transform c) { var f = c.GetComponent<MeshFilter>(); return f != null && f.sharedMesh != null ? f.sharedMesh.name : ""; }
            bool brimmed = hats.Exists(c => Kind(c).StartsWith("Cylinder") && c.localScale.x >= .4f && c.localScale.y <= .03f);   // a brim disc: a hat with a crown
            // No brimmed hat on a model (2026-10-06, Chris: "this hat on any npc is terrible looking"): the straw hat and its kind are
            // a flat disc on a drum, never right on a modelled head. The wearer goes bareheaded.
            if (brimmed) { foreach (var c in hats) Kill(c); hats.Clear(); brimmed = false; }
            bool puffed = hats.Exists(c => Kind(c).StartsWith("Sphere") && c.localPosition.y >= .98f);                               // a baker's puff on its band
            var caps = hats.FindAll(c => Kind(c).StartsWith("Sphere") && c.localPosition.y < .98f && c.localScale.x >= .25f
                || !brimmed && !puffed && Kind(c).StartsWith("Cylinder") && c.localScale.x < .4f && c.localScale.y <= .1f);
            foreach (var c in caps) Cap(c);
            hats.RemoveAll(c => caps.Contains(c));
            if (caps.Count > 0)
            {
                // A cap's peak stands out from the cap's rim in front.
                var o = hairOutline ?? headOutline;
                foreach (var c in hats.FindAll(c => Kind(c).StartsWith("Cube") && c.localPosition.z > .08f && Mathf.Abs(c.localPosition.x) < .15f))
                {
                    float rim = o.eyes + HatAboveEyes, front = OutlineRadius(o, rim, 0) + .02f;
                    c.localPosition = new Vector3(c.localPosition.x, rim + .01f, o.z0 + front + c.localScale.z * .35f);
                    hats.Remove(c);
                }
            }
            // A brim no wider than 1.65 crowns (playtest note 17, "hats too big"): the fit below grows both, so the crown still clears.
            var crown = hats.Find(c => Kind(c).StartsWith("Cylinder") && c.localScale.y > .03f);
            if (crown != null) foreach (var b in hats)
                if (Kind(b).StartsWith("Cylinder") && b.localScale.y <= .03f && b.localScale.x > crown.localScale.x * 1.65f)
                    b.localScale = new Vector3(crown.localScale.x * 1.65f, b.localScale.y, crown.localScale.z * 1.65f);
            if (hats.Count > 0 && HatFit(hats, out float drop, out float grow))
            {
                var hat = new GameObject("Hat").transform; hat.SetParent(headFrame, false); WearHat(hat, drop, grow);
                foreach (var c in hats) c.SetParent(hat, false);
            }
        }

        // ------------------------------------------------------------------------------------------------- hats
        /// <summary>The model's head and hair in the head frame's space (bind pose): its outline per height round the head's axis,
        /// and its eye line.</summary>
        sealed class HeadOutline { public float y0, z0, eyes, top; public int ny; public float[,] r; }
        HeadOutline headOutline, hairOutline;
        const float HeadStep = .01f; const int HeadAngles = 36;
        /// <summary>How far above the eyes a hat's rim sits (old head units, about 5 cm on a model), and how far its crown stands
        /// clear of the head and hair (about 7 mm).</summary>
        const float HatAboveEyes = .065f, HatClear = .03f;
        /// <summary>The outline of the head alone, or with its hair (not the brows or the beard).</summary>
        HeadOutline Outline(bool withHair)
        {
            var pts = new List<Vector3>(); var eyes = new List<Vector3>(); var toFrame = headFrame.worldToLocalMatrix;
            foreach (var r in model.Renderers)
            {
                if (!(r is SkinnedMeshRenderer smr)) continue;
                bool isEyes = r.name.Contains("Eye") && !r.name.Contains("brow"), head = r == model.Head || withHair && model.Hair.Contains(r) && r.name.Contains("Hair_") && !r.name.Contains("Beard");
                if (isEyes) eyes.AddRange(BindPoints(smr, toFrame)); else if (head) pts.AddRange(BindPoints(smr, toFrame));
            }
            if (pts.Count < 20) return null;
            var o = new HeadOutline { y0 = .55f, ny = 70 }; o.r = new float[o.ny, HeadAngles];
            float z0 = 0; foreach (var p in pts) z0 += p.z; o.z0 = z0 / pts.Count;
            float ey = 0; foreach (var p in eyes) ey += p.y; o.eyes = eyes.Count > 0 ? ey / eyes.Count : .78f;
            var slices = new List<Vector2>[o.ny]; for (int i = 0; i < o.ny; i++) slices[i] = new List<Vector2>();
            foreach (var p in pts) { int iy = Mathf.RoundToInt((p.y - o.y0) / HeadStep); if (iy >= 0 && iy < o.ny) slices[iy].Add(new Vector2(p.x, p.z - o.z0)); }
            for (int iy = 0; iy < o.ny; iy++)
            {
                var band = new List<Vector2>(slices[iy]); if (iy > 0) band.AddRange(slices[iy - 1]); if (iy + 1 < o.ny) band.AddRange(slices[iy + 1]);
                if (band.Count < 4) continue;
                var hull = ModelArmour.Hull(band); if (hull.Count < 3) continue;
                for (int ia = 0; ia < HeadAngles; ia++) o.r[iy, ia] = ModelArmour.Ray(hull, 2 * Mathf.PI * ia / HeadAngles);
            }
            float top = float.MinValue; foreach (var q in pts) top = Mathf.Max(top, q.y); o.top = top;
            return o;
        }
        float HeadRadius(float y, float a) { return OutlineRadius(headOutline, y, a); }
        static float OutlineRadius(HeadOutline o, float y, float a)
        {
            float fy = (y - o.y0) / HeadStep; if (fy < 0 || fy > o.ny - 1) return 0;
            int iy = Mathf.Min((int)fy, o.ny - 2); float ty = fy - iy;
            float fa = Mathf.Repeat(a / (2 * Mathf.PI), 1) * HeadAngles; int ia = (int)fa % HeadAngles, ib = (ia + 1) % HeadAngles; float ta = fa - (int)fa;
            return Mathf.Lerp(Mathf.Lerp(o.r[iy, ia], o.r[iy, ib], ta), Mathf.Lerp(o.r[iy + 1, ia], o.r[iy + 1, ib], ta), ty);
        }
        /// <summary>The points a skinned mesh draws, as the figure stands now (its bind pose, at build), in a space.</summary>
        static List<Vector3> BindPoints(SkinnedMeshRenderer smr, Matrix4x4 toSpace)
        {
            var list = new List<Vector3>(); var mesh = smr.sharedMesh; if (mesh == null || !mesh.isReadable) return list;
            var v = mesh.vertices; var w = mesh.boneWeights; var bp = mesh.bindposes; var bones = smr.bones;
            if (w.Length != v.Length || bp.Length != bones.Length) return list;
            var skin = new Matrix4x4[bones.Length]; for (int i = 0; i < bones.Length; i++) skin[i] = toSpace * (bones[i] != null ? bones[i].localToWorldMatrix : Matrix4x4.identity) * bp[i];
            var used = new bool[v.Length]; foreach (int t in mesh.triangles) used[t] = true;
            for (int i = 0; i < v.Length; i++)
            {
                if (!used[i]) continue; var b = w[i];
                list.Add(skin[b.boneIndex0].MultiplyPoint3x4(v[i]) * b.weight0 + skin[b.boneIndex1].MultiplyPoint3x4(v[i]) * b.weight1 + skin[b.boneIndex2].MultiplyPoint3x4(v[i]) * b.weight2 + skin[b.boneIndex3].MultiplyPoint3x4(v[i]) * b.weight3);
            }
            return list;
        }
        /// <summary>
        /// A hat on a model (Chris: "the hats definitely do not fit properly on the heads"). The old figure's hats perched on top of
        /// its big round head; on a model a hat comes down until its rim sits just above the brows (<see cref="HatAboveEyes"/>),
        /// and its crown grows (or narrows) round the head's axis until its walls stand just clear of the head and hair at every
        /// height they reach (<see cref="HatClear"/>). Walls are the faces that look sideways; a brim's flat faces and a crown's
        /// top do not count, so a brim may pass through the head (hidden inside it). Gives the drop and the crown's scale.
        /// </summary>
        bool HatFit(IEnumerable<Transform> parts, out float dy, out float k, float atDy = float.NaN, HeadOutline against = null, float wallsBelow = float.MaxValue, float wallsAbove = float.MinValue, float minK = .6f, float maxK = 1.9f)
        {
            dy = 0; k = 1; if (headOutline == null) return false; var o = against ?? headOutline;
            var side = new List<Vector3>(); float rim = float.MaxValue; var toFrame = headFrame.worldToLocalMatrix;
            foreach (var part in parts)
                foreach (var mf in part.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = mf.sharedMesh; if (mesh == null || !mesh.isReadable) continue;
                    var M = toFrame * mf.transform.localToWorldMatrix; var v = mesh.vertices; var n = mesh.normals;
                    for (int i = 0; i < v.Length; i++)
                    {
                        if (n.Length == v.Length && Mathf.Abs(M.MultiplyVector(n[i]).normalized.y) > .55f) continue;
                        var p = M.MultiplyPoint3x4(v[i]); rim = Mathf.Min(rim, p.y);
                        if (p.y <= wallsBelow && p.y >= wallsAbove) side.Add(p);   // which faces count as walls (a crown's band, not its spikes)
                    }
                }
            if (side.Count == 0) return false;
            dy = float.IsNaN(atDy) ? headOutline.eyes + HatAboveEyes - rim : atDy;   // (or at a height given: a crown set higher)
            float need = 0; bool any = false;
            foreach (var p in side)
            {
                var d = new Vector2(p.x, p.z - headOutline.z0); float r = d.magnitude; if (r < .02f) continue;
                float head = OutlineRadius(o, p.y + dy, Mathf.Atan2(d.x, d.y)); if (head <= 0) continue;
                need = Mathf.Max(need, (head + HatClear) / r); any = true;
            }
            k = any ? Mathf.Clamp(need, minK, maxK) : 1;
            return true;
        }
        /// <summary>
        /// A cap that hugs the head (a skullcap, a knit cap, a fur hat, the Concord's helmet): the old figure's ball made over as a
        /// shell of the model's head and hair from just above the brows to the crown, standing clear of the hair by an amount set by
        /// how far the old ball rose over the old head (a fur hat is fuller than a skullcap), with a rounded top. The ball's own
        /// material; built in the head frame's space, so the part sits at the frame's origin.
        /// </summary>
        void Cap(Transform part)
        {
            var o = hairOutline ?? headOutline; if (o == null) return;
            var mf = part.GetComponent<MeshFilter>();
            float oldTop = part.localPosition.y + part.localScale.y, full = Mathf.Clamp((oldTop - .96f) * .6f, 0, .06f) + .012f;   // the ball's rise over the old head's top
            float rim = o.eyes + HatAboveEyes, top = o.top + full * .8f;
            var v = new List<Vector3>(); var t = new List<int>(); int rings = 0;
            for (float y = rim; y < o.top - .004f; y += HeadStep, rings++)
                for (int ia = 0; ia < HeadAngles; ia++)
                {
                    float a = 2 * Mathf.PI * ia / HeadAngles, r = OutlineRadius(o, y, a) + full * Mathf.Lerp(.75f, 1.15f, Mathf.InverseLerp(rim, o.top, y));
                    v.Add(new Vector3(Mathf.Sin(a) * r, y, o.z0 + Mathf.Cos(a) * r));
                }
            if (rings < 2) return;
            // The crown: the last ring drawn in to the top.
            var last = v.GetRange(v.Count - HeadAngles, HeadAngles); float y1 = last[0].y;
            foreach (float u in new[] { .55f, .82f, .95f })
            {
                foreach (var q in last) { var d = new Vector3(q.x, 0, q.z - o.z0); v.Add(new Vector3(d.x * Mathf.Sqrt(1 - u * u), Mathf.Lerp(y1, top, u), o.z0 + d.z * Mathf.Sqrt(1 - u * u))); }
                rings++;
            }
            for (int i = 0; i + 1 < rings; i++)
                for (int ia = 0; ia < HeadAngles; ia++)
                {
                    int a0 = i * HeadAngles + ia, a1 = i * HeadAngles + (ia + 1) % HeadAngles, b0 = a0 + HeadAngles, b1 = a1 + HeadAngles;
                    t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1);
                }
            int apex = v.Count; v.Add(new Vector3(0, top, o.z0)); int lastRing = (rings - 1) * HeadAngles;
            for (int ia = 0; ia < HeadAngles; ia++) { t.Add(lastRing + ia); t.Add(apex); t.Add(lastRing + (ia + 1) % HeadAngles); }
            var mesh = new Mesh { name = "Cap (fitted)" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            // Wound to face out: flip if the first triangle faces the axis.
            Vector3 p0 = v[t[0]], p1 = v[t[1]], p2 = v[t[2]];
            if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), new Vector3(p0.x, 0, p0.z - o.z0)) < 0) { for (int i = 0; i < t.Count; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; } mesh.SetTriangles(t, 0); }
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mf.sharedMesh = mesh; part.localPosition = Vector3.zero; part.localRotation = Quaternion.identity; part.localScale = Vector3.one;
        }
        /// <summary>Puts a hat's holder where <see cref="HatFit"/> says: dropped, its crown scaled round the head's axis.</summary>
        void WearHat(Transform holder, float dy, float k)
        {
            holder.localPosition = new Vector3(0, dy, headOutline.z0 * (1 - k)); holder.localScale = new Vector3(k, 1, k);
        }

        /// <summary>What a pose does to a model this moment: the clip's slot (null: the walk cycle), the arms' old pivot angles when the
        /// pose works them, a lean of the whole figure, a drop (swimming) and a bend of the back.</summary>
        /// <summary>The sitting clip puts the hips this far behind where the figure stands: a seated body is moved forward by it, so a
        /// villager placed on a stool sits on it and not beside it (playtest note 30).</summary>
        const float SitShift = .42f;
        struct PoseState { public string slot; public bool arms; public Vector3 armL, armR; public float elbowL, elbowR, lean, drop, bend, shift, twist, weight; }
        PoseState PoseOf(ActorPose pose, float speed, float t)
        {
            var p = new PoseState();
            switch (pose)
            {
                case ActorPose.Sit: p.slot = "sit"; p.shift = SitShift; break;
                case ActorPose.Swim: p.slot = "swim"; break;   // still or moving: the stroke (slowed when still); the tread clip read as no pose at all
                case ActorPose.Sneak: p.slot = speed > .3f ? "sneak" : "crouch"; break;
                case ActorPose.Talk: p.slot = "talk"; break;
                case ActorPose.Gather: p.slot = "gather"; break;
                case ActorPose.Cower: p.slot = "crouch"; p.arms = true; p.armL = new Vector3(-150, 0, 25); p.armR = new Vector3(-150, 0, -25); p.elbowL = p.elbowR = -70; break;
                case ActorPose.Hammer:   // steady strikes with the right hand; the left holds the work
                {
                    float k = Mathf.Repeat(t * 1.3f, 1), lift = k < .7f ? Mathf.SmoothStep(0, 1, k / .7f) : 1 - (k - .7f) / .3f;
                    p.arms = true; p.armR = new Vector3(-20 - lift * 110, 0, -8); p.armL = new Vector3(-45, 0, 12); p.elbowR = -20 - lift * 30; p.elbowL = -60; p.lean = 8; break;
                }
                case ActorPose.Chop:     // a big overhead swing, then a pause to set the next log
                {
                    float k = Mathf.Repeat(t * .8f, 1), axe = k < .55f ? Mathf.SmoothStep(0, 1, k / .55f) * 160 : Mathf.Max(0, 160 - (k - .55f) * 900);
                    p.arms = true; p.armL = new Vector3(-axe, 0, 6); p.armR = new Vector3(-axe, 0, -6); p.elbowL = p.elbowR = -10; p.lean = axe < 40 ? 14 : 0; break;
                }
                case ActorPose.Knead:    // both hands pushing forward in turn
                    p.arms = true; p.armL = new Vector3(-60 - Mathf.Max(0, Mathf.Sin(t * 3)) * 25, 0, 6); p.armR = new Vector3(-60 - Mathf.Max(0, Mathf.Sin(t * 3 + Mathf.PI)) * 25, 0, -6);
                    p.elbowL = p.elbowR = -35; p.lean = 10; break;
                case ActorPose.Work:     // hoeing (or hauling a bucket): a lift and a stroke down, leaning into it (playtest note 76: it read as standing still)
                {
                    if (HoeReady && speed < 1.1f) { p.bend = HoeBend(); break; }   // a long tool: the two-handed stroke (FieldArms, note 150)
                    float k = Mathf.Repeat(t * .9f, 1), lift = k < .55f ? Mathf.SmoothStep(0, 1, k / .55f) : 1 - Mathf.SmoothStep(0, 1, (k - .55f) / .2f);
                    float chop = 25 + lift * 85;
                    p.arms = true; p.armL = new Vector3(-chop, 0, 12); p.armR = new Vector3(-chop + 8, 0, -12); p.elbowL = p.elbowR = -20 - lift * 20; p.lean = 16 - lift * 8; break;
                }
                case ActorPose.Drink:    // seated, the right arm lifting the tankard to the mouth every few seconds
                {
                    p.slot = "sit"; p.shift = SitShift;
                    float k = Mathf.Repeat(t * .28f, 1), lift = k < .18f ? Mathf.SmoothStep(0, 1, k / .18f) : k < .45f ? 1 : k < .6f ? 1 - Mathf.SmoothStep(0, 1, (k - .45f) / .15f) : 0;
                    p.arms = true; p.armL = new Vector3(-18, 0, 10); p.armR = new Vector3(-40 - lift * 25, 0, -8 + lift * 22); p.elbowL = -72; p.elbowR = -30 - lift * 95; break;   // hand to the mouth (playtest note 29: it went over the head)
                }
                case ActorPose.Slump:    // passed out over the table: the back folds forward, the arms out on the table
                    p.slot = "sit"; p.shift = SitShift; p.arms = true; p.armL = new Vector3(-110, 0, 22); p.armR = new Vector3(-105, 0, -26); p.elbowL = p.elbowR = -15;
                    p.bend = 40 + Mathf.Sin(t * .6f) * 1.5f; break;
            }
            return p;
        }
        /// <summary>The arms for what is carried (over the walk or the idle): the right arm out to the load at the hip, both arms
        /// forward under a load in front, the right hand up at the shoulder to steady a sack.</summary>
        // ---------- the bow (2026-10-10, Chris: "still no bow animations on ranger") ----------
        // The game has no archery clips (the animation sets are locomotion, idles, a sword's swing and spell casts), so a shot is posed
        // here over whatever the legs are doing: the bow arm out at the mark, the string hand drawn back to the jaw and held there, loosed
        // with a follow-through past the ear, then back to the string to draw again. An aimed shot holds the full draw while it is drawn.
        float shotAt = -9; bool drawing;
        /// <summary>What is held is a bow: an equipped one (IsBow), or with the main hand empty the Ranger's own.</summary>
        public bool BowReady { get { return model != null && (gearDriven && gearRoots[(int)EquipSlot.MainHand] != null ? mainBow : built == ActorLook.Ranger); } }
        /// <summary>The bow is in the left hand now (not slung on the back).</summary>
        public bool HoldsBow { get { return BowReady && held != null && held.Length > 0 && !gearStowed && held[0] != null && held[0].gameObject.activeInHierarchy; } }
        /// <summary>Shooting, or a shot drawn: the bow stays out (not slung) till three seconds after the last arrow.</summary>
        bool BowBusy { get { return BowReady && (drawing || Time.time - shotAt < 3.2f); } }
        /// <summary>An arrow loosed now (CastRelease does this for a figure with a bow in hand).</summary>
        public void Shoot() { shotAt = Time.time; if (model != null && model.HasBow && model.Bow) model.BowLoose(); }
        /// <summary>Shooting, or a shot drawn: the arms stay on the bow until three seconds after the last arrow.</summary>
        bool Bowing { get { return BowBusy && HoldsBow; } }
        public const float BowRelease = .12f, BowRenock = .45f, BowDrawn = 1.05f;
        static readonly Quaternion Nocked = Quaternion.Euler(-82, -12, 0), FullDraw = Quaternion.Euler(-4, 12, -84), Loosed = Quaternion.Euler(-14, 42, -78);
        void BowArms(ref PoseState p)
        {
            float since = drawing ? BowDrawn : Time.time - shotAt;
            Quaternion q; float elbow;
            if (since < BowRelease) { float u = since / BowRelease; q = Quaternion.Slerp(FullDraw, Loosed, u); elbow = Mathf.Lerp(-125, -70, u); bowDraw = 0; }                      // loosed: the hand flies back past the ear, the string snaps straight
            else if (since < BowRenock) { float u = Mathf.SmoothStep(0, 1, (since - BowRelease) / (BowRenock - BowRelease)); q = Quaternion.Slerp(Loosed, Nocked, u); elbow = Mathf.Lerp(-70, -25, u); bowDraw = 0; }   // back to the string
            else { float u = Mathf.SmoothStep(0, 1, (since - BowRenock) / (BowDrawn - BowRenock)); q = Quaternion.Slerp(Nocked, FullDraw, u); elbow = Mathf.Lerp(-25, -125, u); bowDraw = Mathf.Max(.15f, u); }   // nocked and drawn to the jaw, and held
            p.arms = true; p.armL = new Vector3(-86, 0, 6); p.elbowL = -6; p.armR = q.eulerAngles; p.elbowR = elbow; p.twist = 35;
        }
        void CarryArms(ref PoseState p)
        {
            p.arms = true;
            switch (Carry)
            {
                case Carrying.Side: p.armR = new Vector3(-12, 0, -22); p.elbowR = -12; p.armL = new Vector3(Mathf.Sin(phase) * 18 * Mathf.Clamp01(speed / 2), 0, 6); p.elbowL = -15; break;
                case Carrying.Front: p.armL = new Vector3(-55, 0, 10); p.armR = new Vector3(-55, 0, -10); p.elbowL = p.elbowR = -45; break;
                case Carrying.Shoulder: p.armR = new Vector3(-25, 0, -18); p.elbowR = -140; p.armL = new Vector3(Mathf.Sin(phase) * 18 * Mathf.Clamp01(speed / 2), 0, 6); p.elbowL = -15; break;
            }
        }
        /// <summary>Whether this figure is in a fight (EncounterSession sets it: an enemy while engaged, anyone else while the player
        /// fights). A tall weapon stays in hand then, running or not.</summary>
        public static System.Func<GameObject, bool> Fighting;
        bool runSlung; Transform[] tallOf; bool tall;
        /// <summary>Whether what is held reaches past 1.3 m (a staff, a polearm) and has a slung copy to go on the back.</summary>
        bool HeldTall()
        {
            if (held == null || stowed == null || stowed.Length == 0) return false;
            if (tallOf == held) return tall;
            tallOf = held; tall = false;
            foreach (var g in held)
            {
                if (g == null) continue; if (!g.gameObject.activeInHierarchy) { tallOf = null; return false; }   // measured in hand (put away, its bounds are empty)
                var rs = g.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) > 1.3f) tall = true;
            }
            return tall;
        }
        /// <summary>A model's frame: the clip for the pose (walking wins over a standing pose), the arms for the poses without a
        /// clip, the back, the lean, then the frames.</summary>
        Vector2 heading = new Vector2(0, 1);
        AdventurerMotor motor; bool motorChecked;
        void ModelLate()
        {
            // Running out of a fight with a staff or a polearm: slung on the back, not whipping about in the fist (playtest note 19,
            // "she runs awkwardly with the stave"); in hand again to stop or to fight. A little give either side so it does not flicker.
            if (speed > 3.2f) runSlung = true; else if (speed < 2.2f) runSlung = false;
            // Playtest note 79 (2026-10-07): "characters shouldn't run with sword/weapons in hand unless fighting": out of a fight every
            // weapon is on the back or at the hip; it comes out when a fight starts, or to cast.
            bool fighting = Fighting != null && Fighting(gameObject);
            bool stow = Pose == ActorPose.Swim || (!fighting && !Casting && !BowBusy && Fighting != null) || runSlung && HeldTall() && !fighting;
            if (held != null && stow != gearStowed)
            {
                gearStowed = stow;
                foreach (var g in held) if (g != null) g.gameObject.SetActive(!stow);
                if (stowed != null) foreach (var g in stowed) if (g != null) g.gameObject.SetActive(stow);
            }
            var delta = transform.position - lastPosition; delta.y = 0; lastPosition = transform.position;
            float dt = Time.deltaTime, target = dt > 0 ? delta.magnitude / dt : 0;
            speed = Mathf.Lerp(speed, target, dt * 8); if (Riding) speed = 0;   // carried in a saddle: seated however fast its mount goes
            phase += dt * (2.2f + speed * 1.6f);
            // Which way it is moving against the way it faces (strafing, backing): the figure's walk cycle follows it.
            if (target > .2f) { var local = transform.InverseTransformDirection(delta); var h = new Vector2(local.x, local.z).normalized; heading = Vector2.Lerp(heading, h, Mathf.Min(1, dt * 10)); }
            else heading = Vector2.Lerp(heading, new Vector2(0, 1), Mathf.Min(1, dt * 4));
            bool dead = actorRef != null && !actorRef.IsAlive, moving = speed > 1.1f, travel = Pose == ActorPose.Swim || Pose == ActorPose.Sneak;
            var p = PoseOf(moving && !travel ? ActorPose.None : Pose, speed, Time.time + variant * .7f);
            if (!p.arms && p.slot == null && Carry != Carrying.None) CarryArms(ref p);
            if (LyingLow) p = new PoseState { slot = "crouch" };   // an ambusher in wait (EncounterEnemy.Hide), crouched in the grass
            bool bowOn = Bowing && !dead;
            if (model.HasBow) { model.Bow = bowOn; if (bowOn) bowDraw = model.BowDraw; }   // the archer's clips (Kevin Iglesias)
            else if (bowOn) BowArms(ref p);                                                // posed arms where a figure has none
            // The player's jump (Round 22, note 50; 2026-10-10, Chris: "still feel floaty and when you land feet twist together"): the
            // take-off clip from its push-off while going up, the fall's loop coming down (the whole standing jump, crouch, air and
            // landing, had played in the air), and the landing's knee-dip on touching down, a moment of it running and longer standing.
            // ModelFigure blends into and out of them four times quicker than other motions, so the legs never sit half in one pose.
            if (!motorChecked) { motor = GetComponent<AdventurerMotor>(); motorChecked = true; }
            if (motor != null && !dead && Pose != ActorPose.Swim)
            {
                // A hop (up from a jump, under 0.8 s in the air) keeps the take-off's tuck, four-fifths over the walk or run, from leaving
                // the ground to touching down; only a long fall (off a ledge, or a jump that goes on) plays the fall's flung-up arms. The
                // landing's dip is the clip at half weight standing and a third running: whole, it squats to the ground (the frames of
                // 2026-10-10), which read as the legs folding and the feet twisting together.
                bool hop = motor.Jumped && motor.AirAge < .8f && model.Has("k.jumpstart");
                if (motor.Airborne && model.Has("k.fall")) p = new PoseState { slot = hop ? "k.jumpstart" : "k.fall", weight = hop ? .8f : 1, arms = p.arms, armL = p.armL, armR = p.armR, elbowL = p.elbowL, elbowR = p.elbowR, twist = p.twist };
                else if (!motor.Airborne && !moving && motor.LandedAge < .4f && model.Has("k.land")) p = new PoseState { slot = "k.land", weight = .5f, arms = p.arms, armL = p.armL, armR = p.armR, elbowL = p.elbowL, elbowR = p.elbowR, twist = p.twist };   // running, none: the landing clip under the run turned the feet in (Chris, 2026-10-10)
            }
            if (dead) { p = new PoseState { slot = "death" }; body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; }
            // A trade's tool (CarryTool): put away while seated, carrying a load, swimming or dead; at work only standing at it; a long
            // one otherwise held like a staff in the right fist (StaffArm), whatever the clip's arm is doing (walking, talking).
            toolAway = dead || p.slot == "sit" || p.slot == "swim" || p.slot == "swimidle" || Carry != Carrying.None;
            ToolAtWork = !toolAway && !moving && WorkPose;
            staffHold = Mathf.MoveTowards(staffHold, tradeTool != null && tradeToolLong && !toolAway && !ToolAtWork && !p.arms ? 1 : 0, dt * 5);
            if (!dead) p.drop -= GroundGap(dt);
            model.Drive(speed, p.slot, dt, heading, p.weight > 0 ? p.weight : 1);
            if (p.arms) { poseArmL = p.armL; poseArmR = p.armR; poseElbowL = p.elbowL; poseElbowR = p.elbowR; }
            armWeight = Mathf.MoveTowards(armWeight, p.arms ? 1 : 0, dt * 4);
            // Off screen the Animator leaves the bones as they were (CullUpdateTransforms): bending them again would bend them twice.
            Shape(p, armWeight, dead, dt, model.Animator.cullingMode == AnimatorCullingMode.AlwaysAnimate || model.Visible);
        }
        /// <summary>After the clip: the pose's arms (blended by <paramref name="arms"/>), the back's bend, the figure's lean and drop
        /// (and a drinker's reel), then the frames follow.</summary>
        void Shape(PoseState p, float arms, bool dead, float dt, bool fresh)
        {
            // Swimming: the clip lies the figure along the ground; it is lifted so the face rides just over the water (the motor
            // floats the actor .3 under the surface).
            if (p.slot == "swim" || p.slot == "swimidle")
            {
                float lift = .38f - body.InverseTransformPoint(model.HeadBone.position).y;
                swimLift = dt > 0 && swimLift != 0 ? Mathf.Lerp(swimLift, lift, Mathf.Min(1, dt * 6)) : lift;
                p.drop = swimLift;
            }
            else swimLift = 0;
            // A bow drawn: the chest turned side-on to the mark, the left shoulder leading, and the head kept on the mark; before the
            // arms, which are set in the body's frame.
            // The archer's clips (Kevin Iglesias) are made for an archer standing side-on, so their bow arm points to the figure's left.
            // Standing, the whole figure turns side-on to its mark by the angle between the bow arm (in the body's frame, as the clip has
            // it this frame) and the way it faces, legs and all, as the clip is made; moving, the legs keep to the way it goes and the
            // spine and chest take the turn. (2026-10-10, Chris: "bow still twisted": the chest alone had turned, 90 degrees over the hips.)
            float still = 1 - Mathf.InverseLerp(.3f, 1.5f, speed);
            if (fresh && model.HasBow && model.Bow && model.UpperArmL != null && model.HandL != null && model.Spine != null && model.Chest != null)
            {
                var aim = body.InverseTransformDirection(model.HandL.position - model.UpperArmL.position); aim.y = 0;
                float off = aim.sqrMagnitude > .0004f ? Vector3.SignedAngle(aim, Vector3.forward, Vector3.up) : 0;
                aimTwist = Mathf.MoveTowardsAngle(aimTwist, off, Mathf.Max(dt, .001f) * 400);
                float chest = aimTwist * (1 - still);
                model.Spine.rotation = Quaternion.AngleAxis(chest * .4f, body.up) * model.Spine.rotation;
                model.Chest.rotation = Quaternion.AngleAxis(chest * .6f, body.up) * model.Chest.rotation;
            }
            else aimTwist = Mathf.MoveTowardsAngle(aimTwist, 0, Mathf.Max(dt, .001f) * 400);
            float archerTurn = dead ? 0 : aimTwist * still; ArcherTurn = archerTurn;
            float twist = fresh ? p.twist * arms : 0;
            if (twist != 0 && model.Spine != null && model.Chest != null)
            {
                model.Spine.rotation = Quaternion.AngleAxis(twist * .4f, body.up) * model.Spine.rotation;
                model.Chest.rotation = Quaternion.AngleAxis(twist * .6f, body.up) * model.Chest.rotation;
                var neck = model.Neck != null ? model.Neck : model.HeadBone; if (neck != null) neck.rotation = Quaternion.AngleAxis(-twist * .85f, body.up) * neck.rotation;
            }
            if (arms > 0 && fresh)
            {
                PoseArm(armL, foreL, model.UpperArmL, model.LowerArmL, poseArmL, poseElbowL, arms);
                PoseArm(armR, foreR, model.UpperArmR, model.LowerArmR, poseArmR, poseElbowR, arms);
            }
            if (staffHold > 0 && fresh && !dead) StaffArm(staffHold);
            // An elder's stoop and a sleeper's slump bend the back, not the legs.
            float bend = fresh ? stoop + p.bend : 0;
            if (bend != 0 && model.Spine != null) model.Spine.rotation = Quaternion.AngleAxis(bend * .5f, body.right) * model.Spine.rotation;
            if (bend != 0 && model.Chest != null) model.Chest.rotation = Quaternion.AngleAxis(bend * .5f, body.right) * model.Chest.rotation;
            // The figure as a whole moves only for a pose that leans or drops it, or a drinker's reel, and is put back once after:
            // otherwise the body is left to others (an elite's wind-up lean, an ambusher's crouch, a death).
            if (!dead)
            {
                if (p.drop != 0 || p.shift != 0 || LyingLow) { body.localPosition = new Vector3(0, p.drop, p.shift); dropped = true; }
                else if (dropped) { body.localPosition = Vector3.zero; dropped = false; }
                if (Stagger > 0) { staggering = true; body.localEulerAngles = new Vector3(p.lean + Mathf.Sin(phase * .5f) * 6 * Stagger, archerTurn, Mathf.Sin(phase * .5f + 1.1f) * 14 * Stagger); }
                else if (p.lean != 0 || Mathf.Abs(archerTurn) > .01f) { body.localEulerAngles = new Vector3(p.lean, archerTurn, 0); leaned = true; }
                else if (staggering || leaned) { staggering = leaned = false; body.localEulerAngles = Vector3.zero; }
            }
            if (fresh && !dead) FieldArms(dt, ToolAtWork && Pose == ActorPose.Work); else hoeActive = false;   // after the body is placed
            SyncFrames();
            bool clipBow = model.HasBow && model.Bow;
            BowUpright(fresh && (clipBow || p.twist != 0 || bowHeld != null) ? (clipBow ? 1 : arms) : 0);   // and while the arms come down after
            BowString(fresh && (clipBow || p.twist != 0 && arms > .6f));
        }
        float aimTwist;
        /// <summary>How far the figure is turned side-on for the bow this frame (degrees; the motion capture logs it).</summary>
        public float ArcherTurn { get; private set; }
        // The player's capsule rests on its skin (CharacterController.skinWidth, 8 cm), and the model hung from the capsule's bottom, so
        // the whole figure stood 8-9 cm above the ground (FootTests, 2026-10-10: capsule bottom .087 above it, the toes .09-.12 standing).
        // On the ground the model is lowered by the gap a ray finds between the capsule's bottom and the ground under it; in the air the
        // last gap is kept, so it never pops.
        CharacterController capsule; bool capsuleChecked; float groundGap;
        float GroundGap(float dt)
        {
            if (!capsuleChecked) { capsule = GetComponent<CharacterController>(); capsuleChecked = true; }
            if (capsule == null || motor == null || motor.Airborne || motor.Swimming) return groundGap;
            var bottom = transform.position + Vector3.up * (capsule.center.y - capsule.height / 2);
            float best = float.MinValue;
            foreach (var h in Physics.RaycastAll(bottom + Vector3.up * .5f, Vector3.down, 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!h.collider.transform.IsChildOf(transform) && h.point.y > best) best = h.point.y;
            float want = best > float.MinValue ? Mathf.Clamp(bottom.y - best, 0, .25f) : 0;
            groundGap = Mathf.MoveTowards(groundGap, want, dt * .8f);
            return groundGap;
        }
        // ---------- the bowstring (2026-10-10, Chris: "doesn't even pull the string on bow shots") ----------
        // The bows' strings are part of their meshes (one mesh, one material). When a bow comes into a hand, a copy of its mesh without
        // the string is put on it (the string is the thin geometry on the tips' side running the bow's length), and a string of our own
        // runs from nock to nock: straight at rest, pulled back by the drawing hand, with an arrow on it while it is drawn.
        static readonly Dictionary<Mesh, (Mesh bare, Vector3 top, Vector3 bottom)> stringless = new Dictionary<Mesh, (Mesh, Vector3, Vector3)>();
        static Material stringMat, shaftMat, headMat, fletchMat;
        MeshFilter bowMesh; LineRenderer bowString; Transform bowArrow; Vector3 stringTop, stringBottom; float bowDraw;
        /// <summary>The string's two nocks on the bow (mesh space) and the mesh without it; null bare when it cannot be found (or the mesh
        /// is not readable), and the bow keeps its own.</summary>
        static (Mesh, Vector3, Vector3) CutString(Mesh m)
        {
            if (m == null || !m.isReadable) return (null, default, default);
            var v = m.vertices; if (v.Length < 8) return (null, default, default);
            var e = m.bounds.extents; int ax = e.y >= e.x && e.y >= e.z ? 1 : e.x >= e.z ? 0 : 2;   // the bow's length
            int dp = ax == 1 ? (e.x >= e.z ? 0 : 2) : ax == 0 ? (e.y >= e.z ? 1 : 2) : (e.x >= e.y ? 0 : 1);   // its depth: grip to string
            int hi = 0, lo = 0; for (int i = 1; i < v.Length; i++) { if (v[i][ax] > v[hi][ax]) hi = i; if (v[i][ax] < v[lo][ax]) lo = i; }
            float side = (v[hi][dp] + v[lo][dp]) / 2, half = (v[hi][ax] - v[lo][ax]) / 2, mid = (v[hi][ax] + v[lo][ax]) / 2, band = half * .06f;
            bool On(Vector3 q) { return Mathf.Abs(q[dp] - side) < band; }
            var bare = Object.Instantiate(m); bare.name = m.name + " (stringless)";
            float top = float.MinValue, bottom = float.MaxValue; int cut = 0;
            for (int sub = 0; sub < m.subMeshCount; sub++)
            {
                var tri = m.GetTriangles(sub); var keep = new List<int>(tri.Length);
                for (int k = 0; k + 2 < tri.Length; k += 3)
                {
                    Vector3 a = v[tri[k]], b = v[tri[k + 1]], c = v[tri[k + 2]];
                    bool inner = Mathf.Abs(a[ax] - mid) < half * .85f || Mathf.Abs(b[ax] - mid) < half * .85f || Mathf.Abs(c[ax] - mid) < half * .85f;
                    if (On(a) && On(b) && On(c) && inner)
                    { cut++; foreach (var q in new[] { a, b, c }) { top = Mathf.Max(top, q[ax]); bottom = Mathf.Min(bottom, q[ax]); } continue; }
                    keep.Add(tri[k]); keep.Add(tri[k + 1]); keep.Add(tri[k + 2]);
                }
                bare.SetTriangles(keep, sub);
            }
            if (cut == 0) { Object.Destroy(bare); return (null, default, default); }
            var t = Vector3.zero; t[dp] = side; t[ax] = top; var u = Vector3.zero; u[dp] = side; u[ax] = bottom;
            return (bare, t, u);
        }
        void BowString(bool drawn)
        {
            var b = held != null && held.Length > 0 && HoldsBow ? held[0] : null;
            var mf = b != null ? b.GetComponentInChildren<MeshFilter>() : null;
            if (mf != bowMesh)
            {
                bowMesh = mf; if (bowString != null) { Destroy(bowString.gameObject); bowString = null; }
                if (mf != null)
                {
                    var src = mf.sharedMesh;
                    if (src != null && !stringless.TryGetValue(src, out var found)) { found = CutString(src); stringless[src] = found; if (found.bare != null) stringless[found.bare] = found; }
                    if (src != null && stringless.TryGetValue(src, out var cut) && cut.bare != null)
                    {
                        mf.sharedMesh = cut.bare; stringTop = cut.top; stringBottom = cut.bottom;
                        if (stringMat == null) stringMat = Mat(new Color(.82f, .78f, .66f), .3f);
                        var go = new GameObject("Bowstring"); go.transform.SetParent(transform, false);
                        bowString = go.AddComponent<LineRenderer>(); bowString.positionCount = 3; bowString.useWorldSpace = true; bowString.widthMultiplier = .012f;
                        bowString.sharedMaterial = stringMat; bowString.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; bowString.numCapVertices = 2;
                    }
                }
            }
            if (bowString == null || bowMesh == null) { if (bowArrow != null) bowArrow.gameObject.SetActive(false); return; }
            var top = bowMesh.transform.TransformPoint(stringTop); var bottom = bowMesh.transform.TransformPoint(stringBottom); var rest = (top + bottom) / 2;
            float pull = drawn && model.HandR != null ? bowDraw : 0;
            var nock = pull > 0 ? Vector3.Lerp(rest, model.HandR.position, pull) : rest;
            bowString.SetPosition(0, top); bowString.SetPosition(1, nock); bowString.SetPosition(2, bottom);
            // The arrow on the string while it is drawn: nocked at the hand, its head out past the bow.
            bool arrow = pull > .1f;
            if (arrow && bowArrow == null)
            {
                if (shaftMat == null) { shaftMat = Mat(new Color(.55f, .38f, .22f)); headMat = Mat(new Color(.3f, .3f, .32f), .5f, .6f); fletchMat = Mat(new Color(.7f, .16f, .12f)); }
                bowArrow = new GameObject("Nocked arrow").transform; bowArrow.SetParent(transform, false);
                Part(PrimitiveType.Cylinder, bowArrow, new Vector3(0, .36f, 0), new Vector3(.014f, .36f, .014f), shaftMat);
                Part(PrimitiveType.Cube, bowArrow, new Vector3(0, .74f, 0), new Vector3(.03f, .07f, .012f), headMat, new Vector3(0, 0, 45));
                foreach (float a in new[] { 0f, 120f, 240f }) Part(PrimitiveType.Cube, bowArrow, new Vector3(0, .07f, 0), new Vector3(.003f, .09f, .045f), fletchMat, new Vector3(0, a, 0));
            }
            if (bowArrow == null) return;
            bowArrow.gameObject.SetActive(arrow);
            if (!arrow) return;
            var grip = bowMesh.transform.position; var dir = grip - nock; if (dir.sqrMagnitude < .0001f) dir = body.forward;
            bowArrow.position = nock; bowArrow.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }
        Transform bowHeld; Quaternion bowRest;
        /// <summary>While a bow is drawn its limbs stand up and down, as they hang at rest: the fist carries the grip, but the arm raised to
        /// the mark would otherwise lay the bow along it like a spear (the motion capture of 2026-10-10).</summary>
        void BowUpright(float w)
        {
            var b = w > 0 && held != null && held.Length > 0 ? held[0] : null;
            if (bowHeld != null && bowHeld != b) { bowHeld.localRotation = bowRest; bowHeld = null; }
            if (b == null) return;
            if (bowHeld != b) { bowHeld = b; bowRest = b.localRotation; }
            // In the plane of the bow arm (the arrow's line), not the body's: an archer stands side-on to the mark (the clips).
            var along = transform.forward;
            if (model.HandL != null && model.UpperArmL != null) { var a = model.HandL.position - model.UpperArmL.position; a.y = 0; if (a.sqrMagnitude > .0004f) along = a.normalized; }
            along.y = 0; if (along.sqrMagnitude < 1e-4f) along = body.forward;
            along.Normalize();
            // The bow set by its own mesh (2026-10-10, Chris: "bow is backwards", "rounded edge to mob and string towards shooter"):
            // the length from bottom nock to top nock stands up, and the depth from the grip to the string points back along the
            // arrow at the shooter, so the bow's rounded back faces the mark and its string the archer. (Turning the holder by
            // LookRotation set the bow's thickness along the arrow: from behind its flat face showed, a quarter turn out.)
            var mf = bowMesh != null && bowMesh.transform.IsChildOf(b) ? bowMesh : null;
            Quaternion want;
            if (mf != null && mf.sharedMesh != null && stringTop != stringBottom)
            {
                var lenL = (stringTop - stringBottom).normalized;
                var depthL = (stringTop + stringBottom) / 2 - mf.sharedMesh.bounds.center; depthL -= Vector3.Dot(depthL, lenL) * lenL;
                if (depthL.sqrMagnitude < 1e-8f) depthL = Vector3.Cross(lenL, Vector3.forward);
                var meshWant = Quaternion.LookRotation(-along, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(depthL.normalized, lenL));
                var rel = Quaternion.Inverse(b.rotation) * mf.transform.rotation;   // the mesh in its holder, fixed
                want = meshWant * Quaternion.Inverse(rel);
            }
            else want = Quaternion.LookRotation(along, Vector3.up);   // before the string is found (the first frame)
            b.rotation = Quaternion.Slerp(b.parent.rotation * bowRest, want, w);
        }
        /// <summary>
        /// Edit mode (captures, tests): the model as it stands <paramref name="time"/> seconds into <paramref name="pose"/>, walking at
        /// <paramref name="walk"/> m/s when the pose is none (the walk cycle's clip by speed), its frames following. Play mode drives
        /// itself (ModelLate).
        /// </summary>
        /// <summary>Edit mode (captures): a modelled person <paramref name="time"/> seconds into an upper-body action ("swing", "jab",
        /// "castshot", "hit"...), whole body, the frames (and what is held) following.</summary>
        public void PreviewAct(string slot, float time) { if (model == null) return; model.Sample(slot, time); SyncFrames(); }
        public void Preview(ActorPose pose, float walk, float time)
        {
            if (model == null) return;
            Pose = pose; speed = walk; phase = time * (2.2f + walk * 1.6f);
            var p = PoseOf(pose, walk, time);
            if (!p.arms && p.slot == null && Carry != Carrying.None) CarryArms(ref p);
            string slot = p.slot ?? (walk > 5.2f ? "sprint" : walk > 2.2f ? "run" : walk > .15f ? "walk" : "idle");
            model.Sample(slot, time);
            if (p.arms) { poseArmL = p.armL; poseArmR = p.armR; poseElbowL = p.elbowL; poseElbowR = p.elbowR; }
            Shape(p, p.arms ? 1 : 0, false, 0, true);
        }
        /// <summary>Edit mode: the model lying dead (the death clip's last frame).</summary>
        public void PreviewDead() { if (model == null) return; model.Sample("death", 99); body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; SyncFrames(); }
        /// <summary>Turns a model arm so its frames stand as the old pivots would at these angles (the shoulder's Euler in the body,
        /// the elbow about the forearm's X), blended over the clip by <paramref name="w"/>.</summary>
        float staffHold;
        /// <summary>The right arm holding a staff (a trade's long tool, CarryTool): the upper arm down, a little forward and out, the
        /// elbow bent so the forearm points ahead, the fist at the belt a hand's breadth out from the body, the shaft upright through it
        /// and clear of the arm, the body and the legs (playtest notes 95, 109 and 119).</summary>
        void StaffArm(float w)
        {
            if (model.UpperArmR == null || model.LowerArmR == null || model.HandR == null) return;
            Vector3 up = body.up, ahead = body.forward, outward = body.right;
            if (model.Chest != null && Vector3.Dot(model.UpperArmR.position - model.Chest.position, outward) < 0) outward = -outward;   // the right arm's own side
            // Chris, 2026-10-12 (note 119): "standing weird", the fork floating in front of an open hand. Now the upper arm hangs
            // nearly straight down by the side, the forearm reaches down and forward (about 35 degrees under level, an elbow of some
            // 55), the knuckles point ahead with the knuckle row upright, and the fingers close round the shaft (palm in).
            PointBone(model.UpperArmR, model.LowerArmR.position - model.UpperArmR.position, (-up + outward * .1f + ahead * .1f).normalized, w);
            PointBone(model.LowerArmR, model.HandR.position - model.LowerArmR.position, (-up * .58f + ahead * .8f + outward * .04f).normalized, w);
            if (model.MiddleR == null) return;
            PointBone(model.HandR, model.MiddleR.position - model.HandR.position, (ahead - outward * .1f - up * .08f).normalized, w);
            if (!model.HasFingersR) return;
            var F = model.FingersR;
            var len = (model.MiddleR.position - model.HandR.position).normalized;
            var line = Vector3.ProjectOnPlane(F[0, 0].position - F[3, 0].position, len); var upright = Vector3.ProjectOnPlane(up, len);
            if (line.sqrMagnitude > 1e-8f && upright.sqrMagnitude > 1e-8f) model.HandR.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(line, upright), w) * model.HandR.rotation;
            var knuckles = (F[0, 0].position - F[3, 0].position).normalized;
            float s = Mathf.Sign(Vector3.Dot(knuckles, Vector3.Cross(F[1, 1].position - F[1, 0].position, -outward))); if (s == 0) s = 1;
            for (int i = 0; i < 4; i++) for (int j = 0; j < 3; j++) F[i, j].rotation = Quaternion.AngleAxis(s * FingerCurl[j] * w, knuckles) * F[i, j].rotation;
        }
        /// <summary>How far each joint of a finger bends round a staff's shaft (knuckle, middle, tip), in degrees.</summary>
        static readonly float[] FingerCurl = { 72, 88, 55 };
        /// <summary>Where the closed right hand holds a staff: amid the middle finger's knuckle and its two joints, which ring the
        /// shaft. False on a rig without fingers.</summary>
        bool StaffGrip(out Vector3 at)
        {
            at = default;
            if (model == null || !model.HasFingersR || model.MiddleR == null) return false;
            var F = model.FingersR; at = (model.MiddleR.position + F[1, 1].position + F[1, 2].position) / 3; return true;
        }
        /// <summary>For tests (playtest note 119): with a long tool held like a staff, how far the shaft's axis passes from the
        /// middle knuckle and from the middle fingertip joint, how far the upper arm is from hanging straight down and the elbow's
        /// bend, in degrees. False when no staff is held.</summary>
        public bool StaffHoldReport(out float knuckleGap, out float tipGap, out float shoulderFromDown, out float elbowBend)
        {
            knuckleGap = tipGap = shoulderFromDown = elbowBend = 0;
            if (model == null || tradeTool == null || !tradeToolLong || !tradeTool.gameObject.activeInHierarchy || ToolAtWork || staffHold < .99f || !model.HasFingersR) return false;
            Vector3 o = tradeTool.position, d = tradeTool.up;
            float Gap(Vector3 p) { var v = p - o; return (v - Vector3.Dot(v, d) * d).magnitude; }
            knuckleGap = Gap(model.MiddleR.position); tipGap = Gap(model.FingersR[1, 2].position);
            var ua = model.LowerArmR.position - model.UpperArmR.position; var fa = model.HandR.position - model.LowerArmR.position;
            shoulderFromDown = Vector3.Angle(ua, -body.up); elbowBend = Vector3.Angle(ua, fa);
            return true;
        }
        /// <summary>Turns <paramref name="bone"/> so its limb (now pointing <paramref name="now"/>) points <paramref name="want"/>, by <paramref name="w"/>.</summary>
        static void PointBone(Transform bone, Vector3 now, Vector3 want, float w)
        {
            if (now.sqrMagnitude < 1e-6f) return;
            bone.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(now, want), w) * bone.rotation;
        }
        void PoseArm(Transform arm, Transform fore, Transform upperBone, Transform lowerBone, Vector3 shoulder, float elbow, float w)
        {
            var fa = FollowOf(arm); var ff = FollowOf(fore); if (fa.frame == null || ff.frame == null) return;
            var want = body.rotation * Quaternion.Euler(shoulder);
            upperBone.rotation = Quaternion.Slerp(upperBone.rotation, want * Quaternion.Inverse(fa.rot), w);
            lowerBone.rotation = Quaternion.Slerp(lowerBone.rotation, want * Quaternion.Euler(elbow, 0, 0) * Quaternion.Inverse(ff.rot), w);
        }
        /// <summary>
        /// A piece of armour on a model (all but the head's): fitted to the model's form and skinned to its bones (ModelArmour). It
        /// hangs under its slot's root (so it goes when the slot is cleared) but moves with the bones, not the frame.
        /// </summary>
        Transform ModelPart(Transform root, int at, Mesh mesh, Material m)
        {
            var frame = root.parent;   // a limb's root hangs on its pivot frame, a body slot's on its region's frame
            var kind = at == OnArmL ? ModelArmour.Frame.ArmL : at == OnArmR ? ModelArmour.Frame.ArmR : at == OnLegL ? ModelArmour.Frame.LegL : at == OnLegR ? ModelArmour.Frame.LegR : ModelArmour.Frame.Body;
            var go = new GameObject("Gear part"); go.transform.SetParent(root, false);
            if (model.Cloud == null || !frameBind.TryGetValue(frame, out var bind)) { go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m; return go.transform; }
            var smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = ModelArmour.Fit(model.Cloud, mesh, kind, bind, frame.name);
            smr.bones = model.BonesFor(model.Cloud.bones); smr.rootBone = model.Hips; smr.sharedMaterial = m;
            smr.quality = SkinQuality.Bone4; smr.updateWhenOffscreen = false; smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 2.6f);
            return go.transform;
        }
        void OnDestroy() { if (model != null) model.StopMotion(); }
    }
}
