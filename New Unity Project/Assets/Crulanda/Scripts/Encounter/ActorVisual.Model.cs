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
        /// <summary>The figure's model (null on a smooth, block or beast figure).</summary>
        public ModelFigure Model { get { return model; } }
        Transform headFrame, faceFrame, chestFrame, hipsFrame, handleR;
        struct Follow { public Transform frame, bone; public Quaternion rot; public Vector3 pos; public bool place; }
        readonly List<Follow> follows = new List<Follow>();
        Crulanda.Gameplay.Actor actorRef; float armWeight, swimLift; bool dropped, leaned; Vector3 poseArmL, poseArmR; float poseElbowL, poseElbowR;
        /// <summary>What the hands carry, for a model's arms (WorldLife sets it with the load): none; beside the right hip (a basket,
        /// a bucket, a hide, herbs, game); in front in both arms (bread, goods); on the right shoulder (a sack, logs).</summary>
        public enum Carrying { None, Side, Front, Shoulder }
        public Carrying Carry;
        /// <summary>Where a thing carried in the right hand hangs (a tankard): its (0, -.62, 0) is the hand, as on the right arm's
        /// pivot, but it rides the forearm, so it stays in the hand as the elbow bends.</summary>
        public Transform RightHandle { get { return handleR != null ? handleR : armR; } }

        static bool Person(ActorLook look) { return !IsBeast(look) && look != ActorLook.Pale && look != ActorLook.Keeper && look != ActorLook.WeaveEater; }

        static readonly string[] FemaleNames = { "Ama", "Hedda", "Sel", "Lisbet", "Ilse", "Maud", "Edda", "Nettie", "Tamsin", "Grete", "Wenna", "Goody", "Hettie", "Nan", "Mother", "Old Sorrel", "Sorrel", "Mistress", "Dame", "Sister", "Widow" };
        static readonly string[] MaleNames = { "Brannoc", "Wil", "Garet", "Old Tobin", "Tobin", "Osk", "Corwin", "Aldo", "Hob", "Fen", "Jory", "Pim", "Chieftain", "Brother", "Master", "Father" };
        /// <summary>Whether a person is a woman: by the name (first word, or a title) when it says, else by the trade, else by the variant.</summary>
        bool Female(ActorLook look)
        {
            switch (look)
            {
                case ActorLook.Druid: case ActorLook.Healer: return true;
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
                case ActorLook.Healer: s.hood = true; s.hair = 2; break;
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
            return s;
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
            var headAt = L(m.HeadBone) + new Vector3(0, .12f * s, .01f * s) * hs;   // a little high, so a helmet's rim clears the eyes
            headFrame = Region("Head frame", m.HeadBone, headAt - new Vector3(0, .8f * sh.y, 0), sh);
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
            var kit = new HashSet<Transform>();   // what is carried (in hand, slung, the class kit) is never left off
            foreach (var set in new[] { held, stowed, classKit }) if (set != null) foreach (var t in set) if (t != null) kit.Add(t);
            foreach (var c in parts)
            {
                bool onHead = c.localPosition.y >= .62f;
                // A round ball over the head was the old figure's hood or head-wrap: the model wears the outfit's hood in its colour.
                var ball = c.GetComponent<MeshFilter>() != null && c.GetComponent<MeshFilter>().sharedMesh != null && c.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Sphere");
                if (onHead && ball && !kit.Contains(c) && c.localScale.x >= .3f && c.localScale.y >= c.localScale.x * .85f && c.localPosition.y < .95f)
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
            }
        }

        /// <summary>What a pose does to a model this moment: the clip's slot (null: the walk cycle), the arms' old pivot angles when the
        /// pose works them, a lean of the whole figure, a drop (swimming) and a bend of the back.</summary>
        struct PoseState { public string slot; public bool arms; public Vector3 armL, armR; public float elbowL, elbowR, lean, drop, bend; }
        PoseState PoseOf(ActorPose pose, float speed, float t)
        {
            var p = new PoseState();
            switch (pose)
            {
                case ActorPose.Sit: p.slot = "sit"; break;
                case ActorPose.Swim: p.slot = speed > .3f ? "swim" : "swimidle"; break;
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
                case ActorPose.Work:     // both arms down together, like hoeing or hauling a bucket
                {
                    float chop = Mathf.Abs(Mathf.Sin(t * 2.4f)) * 70;
                    p.arms = true; p.armL = new Vector3(-chop, 0, 10); p.armR = new Vector3(-chop, 0, -10); p.elbowL = p.elbowR = -25; break;
                }
                case ActorPose.Drink:    // seated, the right arm lifting the tankard to the mouth every few seconds
                {
                    p.slot = "sit";
                    float k = Mathf.Repeat(t * .28f, 1), lift = k < .18f ? Mathf.SmoothStep(0, 1, k / .18f) : k < .45f ? 1 : k < .6f ? 1 - Mathf.SmoothStep(0, 1, (k - .45f) / .15f) : 0;
                    p.arms = true; p.armL = new Vector3(-35, 0, 8); p.armR = new Vector3(-40 - lift * 85, 0, -8 + lift * 18); p.elbowL = -40; p.elbowR = -30 - lift * 60; break;
                }
                case ActorPose.Slump:    // passed out over the table: the back folds forward, the arms out on the table
                    p.slot = "sit"; p.arms = true; p.armL = new Vector3(-110, 0, 22); p.armR = new Vector3(-105, 0, -26); p.elbowL = p.elbowR = -15;
                    p.bend = 40 + Mathf.Sin(t * .6f) * 1.5f; break;
            }
            return p;
        }
        /// <summary>The arms for what is carried (over the walk or the idle): the right arm out to the load at the hip, both arms
        /// forward under a load in front, the right hand up at the shoulder to steady a sack.</summary>
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
        /// <summary>A model's frame: the clip for the pose (walking wins over a standing pose), the arms for the poses without a
        /// clip, the back, the lean, then the frames.</summary>
        void ModelLate()
        {
            bool stow = Pose == ActorPose.Swim;
            if (held != null && stow != gearStowed)
            {
                gearStowed = stow;
                foreach (var g in held) if (g != null) g.gameObject.SetActive(!stow);
                if (stowed != null) foreach (var g in stowed) if (g != null) g.gameObject.SetActive(stow);
            }
            var delta = transform.position - lastPosition; delta.y = 0; lastPosition = transform.position;
            float dt = Time.deltaTime, target = dt > 0 ? delta.magnitude / dt : 0;
            speed = Mathf.Lerp(speed, target, dt * 8); phase += dt * (2.2f + speed * 1.6f);
            bool dead = actorRef != null && !actorRef.IsAlive, moving = speed > 1.1f, travel = Pose == ActorPose.Swim || Pose == ActorPose.Sneak;
            var p = PoseOf(moving && !travel ? ActorPose.None : Pose, speed, Time.time + variant * .7f);
            if (!p.arms && p.slot == null && Carry != Carrying.None) CarryArms(ref p);
            if (LyingLow) p = new PoseState { slot = "crouch" };   // an ambusher in wait (EncounterEnemy.Hide), crouched in the grass
            if (dead) { p = new PoseState { slot = "death" }; body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; }
            model.Drive(speed, p.slot, dt);
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
            if (arms > 0 && fresh)
            {
                PoseArm(armL, foreL, model.UpperArmL, model.LowerArmL, poseArmL, poseElbowL, arms);
                PoseArm(armR, foreR, model.UpperArmR, model.LowerArmR, poseArmR, poseElbowR, arms);
            }
            // An elder's stoop and a sleeper's slump bend the back, not the legs.
            float bend = fresh ? stoop + p.bend : 0;
            if (bend != 0 && model.Spine != null) model.Spine.rotation = Quaternion.AngleAxis(bend * .5f, body.right) * model.Spine.rotation;
            if (bend != 0 && model.Chest != null) model.Chest.rotation = Quaternion.AngleAxis(bend * .5f, body.right) * model.Chest.rotation;
            // The figure as a whole moves only for a pose that leans or drops it, or a drinker's reel, and is put back once after:
            // otherwise the body is left to others (an elite's wind-up lean, an ambusher's crouch, a death).
            if (!dead)
            {
                if (p.drop != 0 || LyingLow) { body.localPosition = new Vector3(0, p.drop, 0); dropped = true; }
                else if (dropped) { body.localPosition = Vector3.zero; dropped = false; }
                if (Stagger > 0) { staggering = true; body.localEulerAngles = new Vector3(p.lean + Mathf.Sin(phase * .5f) * 6 * Stagger, 0, Mathf.Sin(phase * .5f + 1.1f) * 14 * Stagger); }
                else if (p.lean != 0) { body.localEulerAngles = new Vector3(p.lean, 0, 0); leaned = true; }
                else if (staggering || leaned) { staggering = leaned = false; body.localEulerAngles = Vector3.zero; }
            }
            SyncFrames();
        }
        /// <summary>
        /// Edit mode (captures, tests): the model as it stands <paramref name="time"/> seconds into <paramref name="pose"/>, walking at
        /// <paramref name="walk"/> m/s when the pose is none (the walk cycle's clip by speed), its frames following. Play mode drives
        /// itself (ModelLate).
        /// </summary>
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
        void PoseArm(Transform arm, Transform fore, Transform upperBone, Transform lowerBone, Vector3 shoulder, float elbow, float w)
        {
            var fa = FollowOf(arm); var ff = FollowOf(fore); if (fa.frame == null || ff.frame == null) return;
            var want = body.rotation * Quaternion.Euler(shoulder);
            upperBone.rotation = Quaternion.Slerp(upperBone.rotation, want * Quaternion.Inverse(fa.rot), w);
            lowerBone.rotation = Quaternion.Slerp(lowerBone.rotation, want * Quaternion.Euler(elbow, 0, 0) * Quaternion.Inverse(ff.rot), w);
        }
        void OnDestroy() { if (model != null) model.StopMotion(); }
    }
}
