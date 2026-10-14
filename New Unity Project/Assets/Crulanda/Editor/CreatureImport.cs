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
        /// <summary>Raised when these rules change, so Unity imports the creatures again (2026-10-13: the makers' normals for the new models,
        /// and the loop of an "Armature|" take by its own name; 2026-10-14: the Orc Shaman's clips cut to the pack's own frames).</summary>
        public override uint GetVersion() { return 5; }
        /// <summary>
        /// The Orc Shaman's clip files (the Orc Army Pack, 24 fps): each holds the pack's whole master take (frames 0-650, every clip in a
        /// row), and the pack cuts its own clip out of it by these frames. Its body (the skinned Character1 bones) is baked only inside
        /// those frames, while its staff, belt, eyes and teeth (rigid meshes on the control rig under "locomotion") play the whole take:
        /// taken whole, the orc stood still with its arms down while its staff acted out every other clip apart from its hands (2026-10-14,
        /// Chris: the staff floating by the ground, out to the side). Written here and not read from the meta: Unity writes this rule's
        /// output back into the meta on import.
        /// </summary>
        static readonly Dictionary<string, (int first, int last)> OrcShamanFrames = new Dictionary<string, (int first, int last)> {
            ["Walk"] = (10, 37), ["Run"] = (45, 63), ["Idle"] = (75, 96), ["BattleCry"] = (120, 200), ["Block"] = (228, 250), ["Attack01"] = (260, 281),
            ["Attack02"] = (290, 322), ["Attack03"] = (350, 366), ["CastSpell"] = (372, 436), ["Spell"] = (450, 484), ["Hit"] = (500, 520), ["Die"] = (550, 572) };
        /// <summary>DzenGames' Fantasy Skeleton (Khaven's drowned dead, 2026-10-14): one master take the pack slices into seven clips (its own
        /// meta's frames), named for the motions ModelBeast plays. (name, first, last, loops)</summary>
        static readonly (string name, int first, int last, bool loop)[] FantasySkeletonClips = {
            ("Idle", 0, 70, true), ("Attack", 71, 118, false), ("Walk", 119, 141, true), ("Hit", 142, 186, false), ("CastSpell", 186, 246, false),
            ("Idle_2", 246, 315, true), ("Death", 315, 346, false) };
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
            // The detailed models (2026-10-13: the orcs, the wild boar, the Toon RTS undead, the squirrel, the Living Birds) keep their makers'
            // normals: calculated, the axe orc came out faceted, and these are not rounded (ModelBeast: only the low-poly animals are).
            string file = System.IO.Path.GetFileName(assetPath);
            if (file.StartsWith("Orc") || file.StartsWith("WildBoar") || file.StartsWith("UndeadUnit") || file.StartsWith("Squirrel") || file.StartsWith("Bird") || file.StartsWith("FantasySkeleton"))
            { m.importNormals = ModelImporterNormals.Import; m.importTangents = ModelImporterTangents.CalculateMikk; }
            // A model and the files of its clips each keep their top node (2026-10-13: a clip file with one top node lost it on import, its
            // bones' paths no longer matched the model's, and only the staff, belt and teeth moved: Chris, "the only thing on orc that move").
            if (file.StartsWith("Orc") || file.StartsWith("UndeadUnit") || file.StartsWith("Bird")) m.preserveHierarchy = true;
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
        static readonly string[] Loops = { "Run", "Running", "Walking", "Idle2", "watch01", "watch02", "peck", "preen", "sing", "ruffle", "hopForward", "flyStraight", "flyLeft", "flyRight", "Idle", "Idle_2", "Idle_2_HeadLow", "Idle_Headlow", "Walk", "Gallop", "Eating", "WalkForward", "RunForward", "WalkBackward", "RunBackward", "IdleCombat", "Eat", "Sleep", "StunnedLoop" };
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
                // The Orc Shaman's: the pack's own frames out of its master take (its cycles' ends blended to meet, as the pack has them).
                if (file.StartsWith("OrcShaman_") && OrcShamanFrames.TryGetValue(only.name, out var frames))
                { only.firstFrame = frames.first; only.lastFrame = frames.last; only.loopPose = only.loopTime; }
                m.clipAnimations = new[] { only }; return;
            }
            if (Path.GetFileName(assetPath).StartsWith("FantasySkeleton") && clips.Length > 0)
            {
                var take = clips.OrderByDescending(c => c.lastFrame - c.firstFrame).First();
                m.clipAnimations = FantasySkeletonClips.Select(f => new ModelImporterClipAnimation { name = f.name, takeName = take.takeName, firstFrame = f.first, lastFrame = f.last, loopTime = f.loop, loopPose = f.loop }).ToArray();
                return;
            }
            var keep = clips.Where(c => c.takeName.StartsWith(arm)).ToList();
            if (keep.Count == 0) keep = clips.ToList();
            foreach (var c in keep)
            {
                c.name = c.takeName.StartsWith(arm) ? c.takeName.Substring(arm.Length) : c.takeName;
                int bar = c.name.LastIndexOf('|');   // a take named "Armature|Idle" loops by its own name (the squirrel's, 2026-10-13)
                c.loopTime = System.Array.IndexOf(Loops, bar >= 0 ? c.name.Substring(bar + 1) : c.name) >= 0;
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
