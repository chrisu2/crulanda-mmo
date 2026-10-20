using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Crulanda.EditorTools
{
    /// <summary>Kevin Iglesias's run, walk and idle sampled on his own model, on our bare body (its own avatar) and on our outfits (the
    /// body's avatar, as the game builds a figure), with each foot's angle to its shin, its pitch and its height over the model's feet
    /// (2026-10-10, Chris: "feet backwards and screwed up"; the capture showed Kevin's own figure running flat-footed beside ours on its
    /// toes). Also each leg bone's rest rotation in the outfit against the body. <c>-executeMethod Crulanda.EditorTools.FootDiag.Run</c>;
    /// the table goes in foot-diag.txt beside the project.</summary>
    public static class FootDiag
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            foreach (bool female in new[] { true, false })
            {
                string sex = female ? "Female" : "Male", ki = female ? "HumanF" : "HumanM";
                var bodySrc = Resources.Load<GameObject>("Characters/Bodies/Superhero_" + sex + "_FullBody");
                var kevinSrc = Resources.Load<GameObject>("Characters/Animations/KI/Models/" + ki + "_Model");
                var bodyAvatar = bodySrc != null ? bodySrc.GetComponent<Animator>()?.avatar : null;
                sb.AppendLine("==== " + sex + ": body " + (bodySrc != null) + " avatar " + (bodyAvatar != null ? bodyAvatar.name + " human " + bodyAvatar.isHuman : "none") + ", Kevin " + (kevinSrc != null));
                foreach (var kit in new[] { "Ranger", "Peasant" })
                {
                    var outfit = Resources.Load<GameObject>("Characters/Outfits/" + sex + "_" + kit);
                    if (outfit == null || bodySrc == null) continue;
                    sb.AppendLine("  rest rotations, " + kit + " outfit against the body (degrees apart; local positions apart, m):");
                    foreach (var n in new[] { "pelvis", "thigh_l", "calf_l", "foot_l", "ball_l", "thigh_r", "calf_r", "foot_r", "ball_r" })
                    {
                        var a = Find(bodySrc.transform, n); var b = Find(outfit.transform, n);
                        if (a == null || b == null) { sb.AppendLine("    " + n + ": missing in " + (a == null ? "body" : "outfit")); continue; }
                        sb.AppendLine("    " + n.PadRight(8) + " " + Quaternion.Angle(a.localRotation, b.localRotation).ToString("0.0").PadLeft(6) + "   " + Vector3.Distance(a.localPosition, b.localPosition).ToString("0.000"));
                    }
                }
                var who = new System.Collections.Generic.List<(string, GameObject, Avatar)>();
                var unitySrc = Resources.Load<GameObject>("Characters/Animations/Unity/Armature");
                if (kevinSrc != null) who.Add(("Kevin's own", kevinSrc, kevinSrc.GetComponent<Animator>()?.avatar));
                if (unitySrc != null) who.Add(("Unity's own", unitySrc, unitySrc.GetComponent<Animator>()?.avatar));
                if (bodySrc != null) who.Add(("our body", bodySrc, bodyAvatar));
                foreach (var kit in new[] { "Ranger", "Peasant" })
                {
                    var o = Resources.Load<GameObject>("Characters/Outfits/" + sex + "_" + kit);
                    if (o != null) who.Add(("our " + kit, o, bodyAvatar));
                }
                foreach (var clipName in new[] { "KI/" + sex + "/" + ki + "@Idle01", "KI/" + sex + "/" + ki + "@Walk01_Forward", "KI/" + sex + "/" + ki + "@Run01_Forward", "Unity/Stand--Idle.anim", "Unity/Locomotion--Walk_N.anim", "Unity/Locomotion--Run_N.anim" })
                {
                    AnimationClip clip = null;
                    foreach (var c in Resources.LoadAll<AnimationClip>("Characters/Animations/" + clipName)) if (c != null && !c.name.StartsWith("__preview__")) clip = c;
                    if (clip == null) { sb.AppendLine("  no clip " + clipName); continue; }
                    sb.AppendLine("  " + clipName + " (" + clip.length.ToString("0.00") + " s), left foot: pitch below level / yaw against the hips' forward / knee against it / ankle over the lowest point, at each tenth of the clip");
                    foreach (var (label, src, avatar) in who)
                    {
                        if (avatar == null || !avatar.isHuman) { sb.AppendLine("    " + label + ": no humanoid avatar"); continue; }
                        {
                            var go = Object.Instantiate(src); var anim = go.GetComponent<Animator>(); if (anim == null) anim = go.AddComponent<Animator>();
                            anim.avatar = avatar; anim.applyRootMotion = false; anim.runtimeAnimatorController = null; anim.Rebind();
                            var line = new StringBuilder("    " + label.PadRight(24));
                            for (int k = 0; k < 10; k++)
                            {
                                clip.SampleAnimation(go, clip.length * k / 10f);
                                var calf = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg); var foot = anim.GetBoneTransform(HumanBodyBones.LeftFoot); var toes = anim.GetBoneTransform(HumanBodyBones.LeftToes);
                                if (calf == null || foot == null || toes == null) { line.Append(" (no bones)"); break; }
                                float low = float.MaxValue; foreach (var t in go.GetComponentsInChildren<Transform>()) low = Mathf.Min(low, t.position.y);
                                var shin = foot.position - calf.position; var fv = toes.position - foot.position;
                                float pitch = Mathf.Atan2(-fv.y, new Vector2(fv.x, fv.z).magnitude) * Mathf.Rad2Deg;
                                // Which way the foot points against the hips' forward (the thighs' line turned a quarter), and the knee.
                                var thL = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var thR = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                                var fwd = Vector3.Cross(thR.position - thL.position, Vector3.up); fwd.y = 0; fwd.Normalize();
                                var flat = new Vector3(fv.x, 0, fv.z); float yaw = flat.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(fwd, flat, Vector3.up) : 0;
                                var kneeOut = calf.position - (thL.position + foot.position) / 2; kneeOut.y = 0; float knee = kneeOut.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(fwd, kneeOut, Vector3.up) : 0;
                                line.Append("  " + pitch.ToString("0") + "/y" + yaw.ToString("0") + "/k" + knee.ToString("0") + "/" + (foot.position.y - low).ToString("0.00"));
                            }
                            sb.AppendLine(line.ToString());
                            Object.DestroyImmediate(go);
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "foot-diag.txt"), sb.ToString());
            Debug.Log("FOOT_DIAG_DONE");
        }
        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
