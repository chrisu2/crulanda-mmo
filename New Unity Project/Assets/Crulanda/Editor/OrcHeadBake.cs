using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The orc's head (Chris, 2026-10-13: "shaman is not an orc"; asked for a proper orc face over the first pass's tusks on a man's head).
    /// Batch, in a validation copy only, with the pack staged at Assets/OrcStaging (D:\crulanda-work\imports\orcs, "3D Stylized Orc"; the
    /// pack's own model never goes into the repo): its head, eyes, teeth and tusks (head_low, Eyes_low, Teeth_low, BigTeeth_low) baked in
    /// their rest pose into one mesh in the frame of the orc's Head bone (origin at the bone, +Y up, +Z the way the face looks), its hair
    /// (hair_low, Hair2_low) into another, the colour map copied at 1024. Writes Resources/Characters/Orc and orc-bake.txt (the parts, the
    /// sizes the figure fits by, the skin's mean colour). ActorVisual.Model.cs (OrcHead) hangs it on a figure's Head bone.
    /// </summary>
    public static class OrcHeadBake
    {
        const string Stage = "Assets/OrcStaging", Out = "Assets/Crulanda/Resources/Characters/Orc";
        static readonly string[] HeadParts = { "head_low", "Eyes_low", "Teeth_low", "BigTeeth_low" }, HairParts = { "hair_low", "Hair2_low" };

        public static void Bake()
        {
            AssetDatabase.Refresh();
            var log = new StringBuilder();
            var fbx = AssetDatabase.FindAssets("t:Model", new[] { Stage }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.Contains("skin and rig"));
            var model = fbx != null ? AssetDatabase.LoadAssetAtPath<GameObject>(fbx) : null;
            if (model == null) { Debug.LogError("ORC_BAKE_FAILED: no model under " + Stage); return; }
            Directory.CreateDirectory(Out);
            var go = Object.Instantiate(model); go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                log.AppendLine("skinned " + smr.name + ": " + smr.sharedMesh.vertexCount + " verts, " + smr.bones.Length + " bones, root " + (smr.rootBone ? smr.rootBone.name : "-") + ", materials " + string.Join("/", smr.sharedMaterials.Select(m => m ? m.name + "(" + (m.mainTexture ? m.mainTexture.name : "no tex") + ")" : "null")));
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) log.AppendLine("static " + mf.name + ": " + mf.sharedMesh.vertexCount + " verts");
            var head = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Head_M") ?? go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Head");
            if (head == null) { Debug.LogError("ORC_BAKE_FAILED: no Head bone"); return; }
            log.AppendLine("head bone " + head.name + " at " + head.position.ToString("0.000") + " (model scale " + go.transform.lossyScale + ")");

            // Which way the face looks: the eyes in front of the bone.
            var eyes = Find(go, "Eyes_low"); var eyeMesh = new Mesh(); Vector3 eyeAt = head.position + Vector3.forward;
            if (eyes != null) { eyes.BakeMesh(eyeMesh, true); eyeAt = eyes.transform.TransformPoint(eyeMesh.bounds.center); }
            float facing = Vector3.Dot(eyeAt - head.position, Vector3.forward) >= 0 ? 0 : 180;   // turned so +Z is the face
            var frame = Matrix4x4.TRS(head.position, Quaternion.Euler(0, facing, 0), Vector3.one).inverse;
            log.AppendLine("eyes " + eyeAt.ToString("0.000") + ", facing turn " + facing);

            var skinTex = Save(log, BaseColour(fbx));
            var skin = Mat("OrcSkin", skinTex, new Color(1, 1, 1));
            var headMesh = Combine(go, HeadParts, frame, log, "OrcHead"); var hairMesh = Combine(go, HairParts, frame, log, "OrcHair");
            if (headMesh != null)
            {
                var b = headMesh.bounds;
                log.AppendLine("OrcHead bounds (Head bone frame): centre " + b.center.ToString("0.000") + " size " + b.size.ToString("0.000") + "; top " + b.max.y.ToString("0.000") + ", bottom " + b.min.y.ToString("0.000") + ", front " + b.max.z.ToString("0.000"));
                log.AppendLine("submeshes " + headMesh.subMeshCount);
                // The skin's mean colour over the head (what an orc's hands and arms are tinted toward).
                if (skinTex != null)
                {
                    var path = AssetDatabase.GetAssetPath(skinTex); var ti = (TextureImporter)AssetImporter.GetAtPath(path); ti.isReadable = true; ti.SaveAndReimport();
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path); var uv = headMesh.uv; var tris = headMesh.GetTriangles(0); Color sum = Color.black; int n = 0;
                    foreach (int i in tris.Distinct()) { sum += t.GetPixelBilinear(uv[i].x, uv[i].y); n++; }
                    if (n > 0) log.AppendLine("skin mean " + (sum / n).ToString("0.000"));
                    ti.isReadable = false; ti.SaveAndReimport();
                }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("orc-bake.txt", log.ToString());
            Object.DestroyImmediate(go);
            Debug.Log("ORC_BAKE_DONE\n" + log);
        }
        /// <summary>
        /// The stylized orc's clips for the whole orc of the martial classes (2026-10-13, Chris: "we had whole orc assets"): each of the
        /// pack's clip files carries the whole model again (14 MB each), so only its clip is kept, as a .anim under
        /// Resources/Creatures/OrcBruteClips named as ModelBeast's motions look for it (Idle, Walk, Run, Attack1, Attack2, Die); the
        /// cycles loop. Staged at Assets/OrcStaging/Clips. Writes orc-clips.txt.
        /// </summary>
        public static void BakeBruteClips()
        {
            AssetDatabase.Refresh();
            const string dst = "Assets/Crulanda/Resources/Creatures/OrcBruteClips"; Directory.CreateDirectory(dst);
            var log = new StringBuilder();
            var names = new System.Collections.Generic.Dictionary<string, string> { { "Atack1", "Attack1" }, { "Atack2", "Attack2" }, { "Die", "Die" }, { "Idle", "Idle" }, { "Running", "Run" }, { "Walk", "Walk" } };
            foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { Stage + "/Clips" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                string file = Path.GetFileNameWithoutExtension(path), name = names.Where(kv => file.StartsWith(kv.Key)).Select(kv => kv.Value).FirstOrDefault();
                if (name == null) { log.AppendLine("skipped " + file); continue; }
                // As the model is imported under Resources/Creatures (CreatureImport): a generic rig, so the curves are its bones' own.
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                if (mi.animationType != ModelImporterAnimationType.Generic || !mi.preserveHierarchy)
                { log.AppendLine(file + ": was " + mi.animationType); mi.animationType = ModelImporterAnimationType.Generic; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; mi.preserveHierarchy = true; mi.SaveAndReimport(); }   // its top node kept, as the model's (CreatureImport)
                var clip =AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
                if (clip == null) { log.AppendLine("no clip in " + file); continue; }
                var copy = Object.Instantiate(clip); copy.name = name;
                var set = AnimationUtility.GetAnimationClipSettings(copy); set.loopTime = name == "Idle" || name == "Walk" || name == "Run"; AnimationUtility.SetAnimationClipSettings(copy, set);
                var outPath = dst + "/" + name + ".anim"; AssetDatabase.DeleteAsset(outPath); AssetDatabase.CreateAsset(copy, outPath);
                log.AppendLine(file + " -> " + name + " (" + copy.length.ToString("0.00") + " s, " + AnimationUtility.GetCurveBindings(copy).Length + " curves, legacy " + copy.legacy + ")");
            }
            AssetDatabase.SaveAssets(); File.WriteAllText("orc-clips.txt", log.ToString());
            Debug.Log("ORC_CLIPS_DONE\n" + log);
        }
        static SkinnedMeshRenderer Find(GameObject go, string name) { return go.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == name); }
        /// <summary>The parts baked in rest pose into the Head bone's frame, one submesh each, saved as <paramref name="asset"/>.</summary>
        static Mesh Combine(GameObject go, string[] parts, Matrix4x4 frame, StringBuilder log, string asset)
        {
            var list = new System.Collections.Generic.List<CombineInstance>();
            foreach (var name in parts)
            {
                var r = Find(go, name); if (r == null) { log.AppendLine(asset + ": no " + name); continue; }
                var m = new Mesh(); r.BakeMesh(m, true);   // in the renderer's space, its scale applied: so placed by its position and turn only
                list.Add(new CombineInstance { mesh = m, transform = frame * Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one) });
                log.AppendLine(asset + ": " + name + " " + m.vertexCount + " verts, scale " + r.transform.lossyScale.ToString("0.###"));
            }
            if (list.Count == 0) return null;
            var mesh = new Mesh { name = asset, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(list.ToArray(), false, true); mesh.RecalculateBounds();
            var path = Out + "/" + asset + ".asset"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        static string BaseColour(string fbx)
        {
            var dir = Path.GetDirectoryName(fbx).Replace('\\', '/');
            return AssetDatabase.FindAssets("Orc_Orc_BaseColor t:Texture2D", new[] { Stage }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        }
        /// <summary>The colour map copied into Resources at 1024 (the pack's is 4096).</summary>
        static Texture2D Save(StringBuilder log, string src)
        {
            if (src == null) { log.AppendLine("no colour map"); return null; }
            var dst = Out + "/OrcSkin.png"; AssetDatabase.DeleteAsset(dst); AssetDatabase.CopyAsset(src, dst);
            var ti = (TextureImporter)AssetImporter.GetAtPath(dst); ti.maxTextureSize = 1024; ti.sRGBTexture = true; ti.mipmapEnabled = true; ti.SaveAndReimport();
            log.AppendLine("colour map " + src + " -> " + dst);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
        }
        static Material Mat(string name, Texture2D tex, Color tint)
        {
            var path = Out + "/" + name + ".mat"; AssetDatabase.DeleteAsset(path);
            var m = new Material(Shader.Find("Standard")) { name = name, mainTexture = tex, color = tint }; m.SetFloat("_Glossiness", .18f);
            AssetDatabase.CreateAsset(m, path); return m;
        }
    }
}
