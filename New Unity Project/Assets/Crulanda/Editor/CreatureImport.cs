using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Import settings for the animals under Resources/Creatures (Quaternius's Ultimate Animated Animal Pack, CC0; brought in by
    /// tools/wip/animals/animals_import.py): each a Generic rig with its own avatar, its materials named as in the file (ModelBeast
    /// colours them by name), readable (ModelBeast measures it), its normals smoothed (the pack is flat-shaded: faceted animals stand
    /// out among the painted ones), and its clips. Every clip is in the file twice, with and without the armature's name: the
    /// armature's are kept, named plainly; the standing, walking, galloping and eating ones loop.
    /// <see cref="Report"/> writes what came in (renderers, materials, bones, clips) to CreatureReport.txt beside the project.
    /// </summary>
    public sealed class CreatureImport : AssetPostprocessor
    {
        const string Root = "Assets/Crulanda/Resources/Creatures/";
        /// <summary>How sharp an edge must be to stay hard when the normals are smoothed (degrees).</summary>
        public const float Smoothing = 70;
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var m = (ModelImporter)assetImporter;
            m.animationType = ModelImporterAnimationType.Generic; m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            m.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            m.importCameras = false; m.importLights = false; m.importBlendShapes = false; m.importVisibility = false;
            m.importAnimation = true; m.isReadable = true;
            m.importNormals = ModelImporterNormals.Calculate; m.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            m.normalSmoothingAngle = Smoothing; m.importTangents = ModelImporterTangents.None;
        }
        static readonly string[] Loops = { "Idle", "Idle_2", "Idle_2_HeadLow", "Idle_Headlow", "Walk", "Gallop", "Eating" };
        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Root)) return;
            var m = (ModelImporter)assetImporter; var clips = m.defaultClipAnimations;
            const string arm = "AnimalArmature|";
            var keep = clips.Where(c => c.takeName.StartsWith(arm)).ToList();
            if (keep.Count == 0) keep = clips.ToList();
            foreach (var c in keep)
            {
                c.name = c.takeName.StartsWith(arm) ? c.takeName.Substring(arm.Length) : c.takeName;
                c.loopTime = System.Array.IndexOf(Loops, c.name) >= 0;
            }
            m.clipAnimations = keep.ToArray();
        }

        /// <summary>Batch: what the animals are, for the code (renderers, their materials and colours, bounds, bones, clips).</summary>
        public static void Report()
        {
            AssetDatabase.Refresh();
            var sb = new StringBuilder();
            foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { Root.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (go == null) continue;
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                sb.AppendLine("== " + path + "  avatar " + (avatar == null ? "none" : (avatar.isValid ? "valid" : "INVALID")) + "  root rot " + go.transform.localEulerAngles.ToString("0.0") + " scale " + go.transform.localScale.ToString("0.000"));
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var sk = r as SkinnedMeshRenderer; var mesh = sk != null ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    sb.AppendLine("   renderer " + PathOf(go.transform, r.transform) + " pos " + r.transform.position.ToString("0.000") + " rot " + r.transform.eulerAngles.ToString("0.0") + " scale " + r.transform.lossyScale.ToString("0.000"));
                    sb.AppendLine("     mats [" + string.Join(", ", r.sharedMaterials.Select(x => x == null ? "-" : x.name + " " + x.color.ToString("0.000"))) + "]" + (sk != null ? " skinned, root " + (sk.rootBone ? sk.rootBone.name : "-") + ", bones " + sk.bones.Length : ""));
                    if (mesh != null)
                    {
                        sb.AppendLine("     mesh " + mesh.vertexCount + " verts, submeshes " + mesh.subMeshCount + ", bounds " + mesh.bounds.center.ToString("0.000") + " " + mesh.bounds.size.ToString("0.000") + " uv " + (mesh.uv != null && mesh.uv.Length > 0));
                        if (sk != null)
                        {
                            var baked = new Mesh(); sk.BakeMesh(baked, true); var w = r.transform.localToWorldMatrix; var b = new Bounds(w.MultiplyPoint3x4(baked.vertices[0]), Vector3.zero);
                            foreach (var v in baked.vertices) b.Encapsulate(w.MultiplyPoint3x4(v));
                            sb.AppendLine("     baked world bounds " + b.min.ToString("0.000") + " .. " + b.max.ToString("0.000") + "  renderer bounds " + r.bounds.min.ToString("0.000") + " .. " + r.bounds.max.ToString("0.000"));
                            Object.DestroyImmediate(baked);
                        }
                    }
                }
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Body" || t.name == "Head" || t.name == "Torso3" || t.name.StartsWith("Front") || t.name.StartsWith("Back") || t.name.StartsWith("Tail1") || t.name == "Neck1")
                        sb.AppendLine("   bone " + PathOf(go.transform, t) + " @" + t.position.ToString("0.000") + " rot " + t.eulerAngles.ToString("0.0"));
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
                    sb.AppendLine("   clip " + clip.name + " " + clip.length.ToString("0.00") + "s" + (clip.isLooping ? " loop" : "") + " curves " + AnimationUtility.GetCurveBindings(clip).Length);
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../CreatureReport.txt"), sb.ToString());   // beside the project, not in the build
            Debug.Log("CREATURE_REPORT_DONE");
        }
        static string PathOf(Transform root, Transform t) { var s = t.name; for (var p = t.parent; p != null && p != root; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
