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
        /// <summary>A palette texture (CraftPix's swatches): no mipmaps or compression to bleed one swatch into the next.</summary>
        void OnPreprocessTexture()
        {
            // Painted skins (Resources/CreatureSkins: the treants): ordinary mipmapped, compressed textures; the "_Normal" ones normal maps.
            if (assetPath.StartsWith("Assets/Crulanda/Resources/CreatureSkins/"))
            {
                var s = (TextureImporter)assetImporter; s.mipmapEnabled = true; s.maxTextureSize = 1024;
                s.textureType = assetPath.EndsWith("_Normal.png") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                return;
            }
            if (!assetPath.StartsWith(Root)) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default; t.sRGBTexture = true; t.mipmapEnabled = false; t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp; t.textureCompression = TextureImporterCompression.Uncompressed; t.alphaSource = TextureImporterAlphaSource.None;
        }
        static readonly string[] Loops = { "Idle", "Idle_2", "Idle_2_HeadLow", "Idle_Headlow", "Walk", "Gallop", "Eating", "WalkForward", "RunForward", "WalkBackward", "RunBackward", "IdleCombat", "Eat", "Sleep", "StunnedLoop" };
        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Root)) return;
            var m = (ModelImporter)assetImporter; var clips = m.defaultClipAnimations;
            const string arm = "AnimalArmature|";
            // An animal with a file per clip (Blink's bear: Creatures/BearClips/Bear_Attack1.fbx...): the clip is named for its file,
            // after the animal's name ("Attack1").
            if (assetPath.Contains("Clips/") && clips.Length > 0)
            {
                string file = Path.GetFileNameWithoutExtension(assetPath); int u = file.IndexOf('_');
                var only = clips.OrderByDescending(c => c.lastFrame - c.firstFrame).First();   // the file's real take (each has a one-frame one too)
                only.name = u >= 0 ? file.Substring(u + 1) : file;
                only.loopTime = System.Array.IndexOf(Loops, only.name) >= 0;
                m.clipAnimations = new[] { only }; return;
            }
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
            // The files of a clip each come in again (cheap), so a change to how they are named takes without a full reimport.
            foreach (var clipFile in AssetDatabase.FindAssets("t:Model", new[] { Root.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("Clips/")))
                AssetDatabase.ImportAsset(clipFile, ImportAssetOptions.ForceUpdate);
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
                if (!go.GetComponentsInChildren<Transform>(true).Any(t => t.name == "AnimalArmature"))   // another maker's rig: every bone, by name
                    sb.AppendLine("   bones " + string.Join(" ", go.GetComponentsInChildren<Transform>(true).Select(t => t.name + "@" + t.position.ToString("0.00"))));
                // Each clip, and how far it carries the rig's root from its first frame to its last (a clip that walks off on its own).
                var inst = Object.Instantiate(go); var root = inst.GetComponentInChildren<SkinnedMeshRenderer>(true)?.rootBone;
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
                {
                    string moves = "";
                    if (root != null) { clip.SampleAnimation(inst, 0); var a = root.position; clip.SampleAnimation(inst, clip.length); moves = " root moves " + (root.position - a).ToString("0.000"); }
                    sb.AppendLine("   clip " + clip.name + " " + clip.length.ToString("0.00") + "s" + (clip.isLooping ? " loop" : "") + " curves " + AnimationUtility.GetCurveBindings(clip).Length + moves);
                }
                Object.DestroyImmediate(inst);
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../CreatureReport.txt"), sb.ToString());   // beside the project, not in the build
            Debug.Log("CREATURE_REPORT_DONE");
        }
        static string PathOf(Transform root, Transform t) { var s = t.name; for (var p = t.parent; p != null && p != root; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
