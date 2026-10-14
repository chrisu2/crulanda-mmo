using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Import settings for the character kits under Resources/Characters (Quaternius, CC0; playtest note 12, brought in by
    /// tools/wip/characters/quaternius_import.py): every model a Humanoid with its own avatar, no materials of its own (the
    /// game makes them, ModelFigure), bodies, hair and outfits readable (their bounds fit the armour); the animation library
    /// imports its clips. Textures: normal maps as normal maps, the MetalSmooth and Occlusion maps linear, at most 2048.
    /// <see cref="Report"/> writes what came in (bones, meshes, clips) to CharacterReport.txt beside the project, for the code that uses them.
    /// </summary>
    public sealed class CharacterImport : AssetPostprocessor
    {
        const string Root = "Assets/Crulanda/Resources/Characters/";
        /// <summary>Bumped when the import settings change, so Unity imports the models and clips again.</summary>
        public override uint GetVersion() { return 10; }
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var m = (ModelImporter)assetImporter; bool anim = assetPath.Contains("/Animations/");
            m.animationType = ModelImporterAnimationType.Human; m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            m.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;   // for their names: ModelFigure maps them to the kit's materials
            m.importCameras = false; m.importLights = false; m.importBlendShapes = false; m.importVisibility = false;
            m.importAnimation = anim; m.isReadable = !anim;
            // Kevin's and Unity's clips are made on their own characters and must take their avatar from them, as their vendors set
            // them up ("Copy From Other Avatar"). Each made from its own file, a clip's avatar was the skeleton as that file holds it:
            // Unity's run files hold it mid-stride, so the left leg's twist was taken from a running pose and every clip turned the
            // left foot out and round (2026-10-10, Chris: "the left foot is always pointing backwards while running").
            string src = SourceModel(assetPath);
            if (src != null)
            {
                context.DependsOnArtifact(src);
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(src);
                if (avatar != null && avatar.isHuman) { m.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; m.sourceAvatar = avatar; }
                else Debug.LogWarning("CharacterImport: no avatar yet in " + src + " for " + assetPath);
            }
        }
        /// <summary>The character a clip was made on (its avatar's source), or null for a model or a clip made on ours.</summary>
        static string SourceModel(string path)
        {
            const string ki = Root + "Animations/KI/", unity = Root + "Animations/Unity/";
            if (path.StartsWith(ki + "Male/")) return ki + "Models/HumanM_Model.fbx";
            if (path.StartsWith(ki + "Female/")) return ki + "Models/HumanF_Model.fbx";
            if (path.StartsWith(unity) && !path.EndsWith("/Armature.fbx")) return unity + "Armature.fbx";
            // Morro Motion's idles on Morro's own man (note 112b, 2026-10-14: Chris downloaded the pack again): each made from its own
            // file, a clip's avatar was its first frame's skeleton, and the idles stood with a foot turned out.
            if (path.StartsWith(Root + "Animations/Morro/") && !path.Contains("/Model/")) return Root + "Animations/Morro/Model/MorroMan_Idle_MoCap.fbx";
            return null;
        }
        /// <summary>The library's clips: the "_Loop" ones loop, and every one keeps its root where it was made (turn, height and
        /// ground position baked into the pose), so a sitting or dying figure goes down instead of hovering (the game moves the
        /// figure itself; ModelFigure plays no root motion).</summary>
        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.Contains("/Animations/")) return;
            var m = (ModelImporter)assetImporter; var clips = m.defaultClipAnimations;
            // Morro Motion's idles come trimmed (their clip starts after the capture's calibration T-pose, frame 85-166): keep the
            // vendor's range. From the take's start every idling figure stood in a T for seconds each loop (2026-10-06, "people t
            // pose too much").
            if (assetPath.Contains("/Animations/Morro/") && m.clipAnimations != null && m.clipAnimations.Length > 0) clips = m.clipAnimations;
            // Kevin Iglesias's motions (Animations/KI, "HumanM@Walk01_Forward"): the cycles loop (idles, walk, run, sprint, talk, the
            // jump's middle and the fall); the turns, the jump's start and landing and the idles' crossings play once.
            string file = Path.GetFileNameWithoutExtension(assetPath); bool ki = assetPath.Contains("/Animations/KI/");
            bool kiLoop = ki && (file.Contains("@Walk01") || file.Contains("@Run01") || file.Contains("@Sprint01") || file.Contains("@Talk01") || file.Contains("@Fall01")
                || file.EndsWith("@Idle01") || file.EndsWith("@Idle02") || file.EndsWith("@Jump01")
                // his melee, spellcasting, throwing and crafting sets (2026-10-14, Chris: "we need more spell animations and melee
                // animations"): the combat stances, the cast held, the work's loops, the gathering and the pick's strokes loop
                || file.Contains("@CombatIdle") || file.Contains("@CastingIdle") || file.EndsWith(" - Loop") || file.Contains("@Gathering") || file.Contains("@MiningOneHand"));
            // Unity's Starter Assets third-person clips (Animations/Unity, 2026-10-10; the forward walk and run since Kevin's go on their
            // toes): the walk, run, idle and in-air loop; they face the other way from ours, as Kevin's do (without the turn everyone ran
            // backwards: Chris, "still running backwards").
            bool unity = assetPath.Contains("/Animations/Unity/");
            bool unityLoop = unity && !file.Contains("_Land") && (file.Contains("Walk_N") || file.Contains("Run_N") || file.Contains("Stand--Idle") || file.Contains("InAir"));
            foreach (var c in clips)
            {
                c.loopTime = c.name.EndsWith("_Loop") || kiLoop || unityLoop || assetPath.Contains("/Animations/Morro/");   // Morro Motion's idles all loop
                // Kevin's strafes and diagonals face forward by the body's own heading, not the root's as authored (as authored, a
                // strafe to the left turned the whole figure a quarter round).
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                if (ki || unity || assetPath.Contains("/Animations/Morro/")) c.rotationOffset = 180;   // the KI and Morro files face the other way from the Quaternius figures (2026-10-05: everyone walked backwards)
                c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
            }
            m.clipAnimations = clips;
        }
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var t = (TextureImporter)assetImporter; string n = Path.GetFileNameWithoutExtension(assetPath);
            t.maxTextureSize = 2048; t.mipmapEnabled = true;
            if (n.EndsWith("_Normal")) t.textureType = TextureImporterType.NormalMap;
            else { t.textureType = TextureImporterType.Default; t.sRGBTexture = !(n.EndsWith("_MetalSmooth") || n.EndsWith("_Occlusion")); t.alphaSource = n.EndsWith("_MetalSmooth") ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; }
        }

        /// <summary>The kit's texture sets, each a Standard material asset (so its normal-map and metallic-map variants ship in a
        /// build): skin light and dark for each body, the outfits, the outfits' own skin, hair and the eyes.</summary>
        static readonly string[] Sets = { "Superhero_Male_Light", "Superhero_Male_Dark", "Superhero_Female_Light", "Superhero_Female_Dark", "Regular_Male_Dark", "Regular_Female_Dark",
            "Peasant", "Ranger", "Ranger_White", "Hair_1", "Hair_2", "Eye_Brown" };
        static void Materials()
        {
            string dir = Root + "Materials"; if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Materials");
            Texture2D T(string name) { return AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/T_" + name + ".png"); }
            foreach (var set in Sets)
            {
                // Ranger_White is the Ranger's colours with its green cloth bleached (tools/wip/characters/concord), its other maps the Ranger's.
                string body = set == "Ranger_White" ? "Ranger" : set.Replace("_Light", "").Replace("_Dark", "").Replace("_Brown", "");
                var path = dir + "/" + set + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
                var albedo = T(set + "_BaseColor") ?? T(set); m.mainTexture = albedo; m.color = Color.white;
                var normal = T(body + "_Normal"); if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
                var ms = T(body + "_MetalSmooth"); if (ms != null) { m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICGLOSSMAP"); m.SetFloat("_GlossMapScale", .75f); }
                else { m.SetFloat("_Glossiness", set.StartsWith("Eye") ? .85f : .25f); m.SetFloat("_Metallic", 0); }
                var occ = T(body + "_Occlusion"); if (occ != null) { m.SetTexture("_OcclusionMap", occ); m.SetFloat("_OcclusionStrength", .8f); }
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }
        /// <summary>Before the kits are used in a batch run: the materials, and the clip library imported with its clip settings
        /// (OnPreprocessAnimation) if it came in before them.</summary>
        public static void Prepare()
        {
            AssetDatabase.Refresh(); Materials();
            const string lib = Root + "Animations/UAL1_Standard.fbx";
            var m = AssetImporter.GetAtPath(lib) as ModelImporter;
            if (m != null && (m.clipAnimations == null || m.clipAnimations.Length == 0)) AssetDatabase.ImportAsset(lib, ImportAssetOptions.ForceUpdate);
        }
        /// <summary>Batch: the materials (<see cref="Materials"/>), then what the character kits are, for the code (bones,
        /// renderers and their materials, bounds, avatars, clips).</summary>
        public static void Report()
        {
            AssetDatabase.Refresh(); Materials();
            var sb = new StringBuilder();
            foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { Root.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (go == null) continue;
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                sb.AppendLine("== " + path + "  avatar " + (avatar == null ? "none" : (avatar.isValid ? "valid" : "INVALID") + (avatar.isHuman ? " human" : " generic")));
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var sk = r as SkinnedMeshRenderer; var mesh = sk != null ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    sb.AppendLine("   renderer " + PathOf(go.transform, r.transform) + " mats [" + string.Join(", ", r.sharedMaterials.Select(x => x == null ? "-" : x.name)) + "]" + (sk != null ? " skinned, root " + (sk.rootBone ? sk.rootBone.name : "-") + ", bones " + sk.bones.Length : "") +
                        (mesh != null ? ", " + mesh.vertexCount + " verts, submeshes " + mesh.subMeshCount + ", bounds " + mesh.bounds.center.ToString("0.000") + " " + mesh.bounds.size.ToString("0.000") : ""));
                }
                if (avatar != null && avatar.isHuman)
                {
                    var hd = avatar.humanDescription; sb.Append("   human bones:");
                    foreach (var hb in hd.human.OrderBy(h => h.humanName)) sb.Append(" " + hb.humanName + "=" + hb.boneName + ";");
                    sb.AppendLine();
                }
                var all = go.GetComponentsInChildren<Transform>(true);
                sb.AppendLine("   transforms " + all.Length + ": " + string.Join(", ", all.Take(80).Select(t => t.name + "@" + t.position.ToString("0.000"))));
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
                    sb.AppendLine("   clip " + clip.name + " " + clip.length.ToString("0.00") + "s" + (clip.isHumanMotion ? " human" : "") + (clip.isLooping ? " loop" : ""));
            }
            File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../CharacterReport.txt"), sb.ToString());   // beside the project, not in the build
            Debug.Log("CHARACTER_REPORT_DONE");
        }
        static string PathOf(Transform root, Transform t) { var s = t.name; for (var p = t.parent; p != null && p != root; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
