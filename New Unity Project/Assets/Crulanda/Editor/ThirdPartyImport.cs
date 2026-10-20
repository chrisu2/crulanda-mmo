using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// Import settings for the Asset Store packs under Assets/Crulanda/ThirdParty (tools/wip/weapons/weapons_import.py): their
    /// textures capped (a weapon in the hand is a few dozen pixels; the packs ship 2048 px maps), and <see cref="Report"/>, which
    /// writes each weapon prefab's shape (its longest axis, where its pivot lies along it, its size, its materials and which glow)
    /// to WeaponReport.txt beside the project, for ActorVisual's model weapons.
    /// </summary>
    public sealed class ThirdPartyImport : AssetPostprocessor
    {
        const string Root = "Assets/Crulanda/ThirdParty/";
        /// <summary>The prop kits (2026-10-07): Quaternius's Fantasy Props MegaKit (CC0; FBX with four trim sheets) and Lukas Bobor's
        /// Medieval props (Asset Store EULA; prefabs with their own materials), under Resources so ZoneBuilder.ModelProp can load them by name.</summary>
        const string Props = "Assets/Crulanda/Resources/Props/";
        bool Ours { get { return assetPath.StartsWith(Root) || assetPath.StartsWith(Props); } }
        void OnPreprocessTexture()
        {
            // The Stylized Skyboxes (play note 254; StylizedSky): lat-long HDR pictures read by Crulanda/StylizedSky as flat 2D maps,
            // 2048 wide, no mip levels (no seam where the picture wraps round), wrapping round but not over the poles.
            if (assetPath.StartsWith("Assets/Crulanda/Resources/Sky/") && assetPath.EndsWith(".exr"))
            {
                var sky = (TextureImporter)assetImporter; sky.textureShape = TextureImporterShape.Texture2D; sky.maxTextureSize = 2048; sky.mipmapEnabled = false;
                sky.wrapModeU = TextureWrapMode.Repeat; sky.wrapModeV = TextureWrapMode.Clamp; return;
            }
            if (!Ours) return;
            var t = (TextureImporter)assetImporter;
            if (assetPath.Contains("/MedievalVillage1/")) { t.maxTextureSize = VillageCap(assetPath); t.mipmapEnabled = true; return; }
            t.maxTextureSize = assetPath.Contains("/Chest/") || assetPath.Contains("/Fantasy/") || assetPath.Contains("/Nature/") || assetPath.Contains("/Megapack/") || assetPath.Contains("/HQRocks/") || assetPath.Contains("/Village/") || assetPath.Contains("/DryTrees/") || assetPath.Contains("/Alchemy/") || assetPath.Contains("/WoodenCanopy/") ? 1024 : 512; t.mipmapEnabled = true;   // DryTrees: the Dry Trees pack (play note 248); Alchemy (note 253)
            if ((assetPath.Contains("/Fantasy/") || assetPath.Contains("/Nature/") || assetPath.Contains("/Village/")) && assetPath.EndsWith("_Normal.png")) t.textureType = TextureImporterType.NormalMap;   // (ends with: Bark_NormalTree.png and Leaves_NormalTree_C.png are colour)
            // The Free Alchemy and Magic Pack (StormBringer Studios, Asset Store EULA; Resources/Props/Alchemy, play note 253): 1024 px
            // maps, kept at 1024 (the 512 the other packs fall to would blur the mortar's stone and the flask's brass).
            // The Simple Wood Barrel (Kraken XR, Asset Store EULA; Resources/Props/WoodBarrel, play note 247, 2026-10-19): three 4096 px maps
            // (17-19 MB each) for a prop a metre tall, capped at 1024; its normal map a normal map, its metallic-smoothness map linear.
            if (assetPath.StartsWith(Props + "WoodBarrel/"))
            {
                t.maxTextureSize = 1024;
                if (assetPath.EndsWith("_Normal.png")) t.textureType = TextureImporterType.NormalMap;
                if (assetPath.EndsWith("_MetallicSmoothness.png")) t.sRGBTexture = false;
            }
        }
        /// <summary>
        /// The Medieval Village Pack Vol.1 (MyDreamGameStudio, Asset Store EULA; Resources/Props/MedievalVillage1, 2026-10-19, play note 246:
        /// the Lowtowns' houses, ZoneBuilder.LowtownVillage). It ships 2048 px maps, three a material (colour, normal, metallic-smoothness):
        /// the colour of a house's big parts (walls, roofs, planks, stone) at 1024, everything else (normal and metallic maps, doors, glass,
        /// chimneys, the small props) at 512, so its hundred-odd materials stay a sane size in the build. The files were brought in at those
        /// sizes already; the pack's own .meta files (normal maps marked) are kept.
        /// </summary>
        public static int VillageCap(string path)
        {
            string f = Path.GetFileNameWithoutExtension(path);
            bool house = f.StartsWith("House_") || f.StartsWith("Blacksmith_Wall") || f.StartsWith("Blacksmith_Roof") || f.StartsWith("Blacksmith_Plank") || f.StartsWith("Blacksmith_Wood");
            bool small = f.Contains("_Door") || f.Contains("_Glass") || f.Contains("_Smoker") || f.Contains("_Window") || f.Contains("_Rope");
            return house && !small && f.EndsWith("_BaseColor") ? 1024 : 512;
        }
        /// <summary>alkakrab's Free 25 Fantasy RPG Tracks Vol. 3 (2026-10-19, play note 251; tools/import_music_vol3.py writes the same
        /// into the metas): Vorbis at quality .5, streamed from disk, not preloaded, so 25 WAVs (~480 MB) cost the build a few MB each
        /// and no memory until ZoneMusic plays one.</summary>
        const string MusicVol3 = "Assets/Crulanda/Resources/Music/Fantasy3/";
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(MusicVol3)) return;
            var a = (AudioImporter)assetImporter; var s = a.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .5f; s.preloadAudioData = false;
            a.defaultSampleSettings = s; a.loadInBackground = true; a.forceToMono = false;
        }
        void OnPreprocessModel()
        {
            if (!Ours) return;
            var m = (ModelImporter)assetImporter; m.isReadable = true; m.importCameras = false; m.importLights = false;
        }
        /// <summary>The Fantasy kit's FBX name their materials after the trim sheet they use (MI_Trim_Metal, MI_Trim_Furniture,
        /// MI_Trim_Props, MI_Trim_Cloth, MI_Banner, MI_Page_Empty; "_Vertex" variants are vertex-painted over the same sheet): each
        /// gets that sheet's base colour and normal map on the Standard shader, wood and cloth matte, metal a little glossier.</summary>
        void OnPostprocessMaterial(Material m)
        {
            if (assetPath.StartsWith(Props + "Nature/")) { NatureMaterial(m); return; }
            if (assetPath.StartsWith(Props + "Village/")) { VillageMaterial(m); return; }
            if (assetPath.StartsWith(Props + "Food/")) { FoodMaterial(m); return; }
            if (!assetPath.StartsWith(Props + "Fantasy/")) return;
            string sheet = m.name.Contains("Metal") ? "Metal" : m.name.Contains("Furniture") ? "Furniture" : m.name.Contains("Cloth") || m.name.Contains("Banner") ? "Cloth" : "Props";
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(Props + "Fantasy/Textures/T_Trim_" + sheet + "_BaseColor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Props + "Fantasy/Textures/T_Trim_" + sheet + "_Normal.png");
            m.shader = Shader.Find("Standard"); m.color = Color.white;
            if (baseMap != null) m.mainTexture = baseMap;
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Glossiness", sheet == "Metal" ? .45f : .2f); m.SetFloat("_Metallic", sheet == "Metal" ? .35f : 0);
        }
        /// <summary>The food models (Resources/Props/Food, 2026-10-18, playtest note 235: Quaternius's food pack, the medieval ones only) carry
        /// no textures, only a flat colour on each material, written by Blender in linear values (an apple's red is 0.13, 0.03, 0.02): the project
        /// is in gamma space, so each is brought to gamma, or every loaf and apple comes in near black. Matte, a little sheen.</summary>
        static void FoodMaterial(Material m)
        {
            var c = m.HasProperty("_Color") ? m.color : Color.white;
            m.shader = Shader.Find("Standard"); m.mainTexture = null;
            m.color = new Color(c.r, c.g, c.b, 1).gamma;
            m.SetFloat("_Glossiness", .18f); m.SetFloat("_Metallic", 0);
        }
        /// <summary>The nature kit (Stylized Nature MegaKit, 2026-10-07) names a material after its texture: Bark_NormalTree,
        /// Leaves_NormalTree, Leaves_Pine (Leaf_Pine.png), Rocks (Rocks_Diffuse.png), Grass, Flowers, Mushrooms. Bark gets its normal
        /// map; grass, flowers, petals and plants are cut out; leaves are solid clumps and stay opaque; all matte.</summary>
        static void NatureMaterial(Material m)
        {
            string dir = Props + "Nature/Textures/", n = m.name;
            // The leaf sheets are alpha cards: the "_C" sheet carries the colour (the plain one is a grey mask for vertex-coloured use).
            bool leaf = n.StartsWith("Leaves") || n.StartsWith("Leaf");
            bool cutout = leaf || n.StartsWith("Grass") || n.StartsWith("Flower") || n.StartsWith("Petal") || n.StartsWith("Plant") || n.StartsWith("Fern") || n.StartsWith("Clover") || n.StartsWith("Mushroom");
            string tex = n == "Leaves_Pine" ? "Leaf_Pine_C" : n == "Leaves_NormalTree" ? "Leaves_NormalTree_C" : n == "Leaves_TwistedTree" ? "Leaves_TwistedTree_C" : n == "Leaves_GiantPine" ? "Leaves_GiantPine_C"
                : n == "Rocks" ? "Rocks_Diffuse" : n == "PathRocks" ? "PathRocks_Diffuse" : n == "Material" ? "Bark_TwistedTree" : n;
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + tex + ".png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + tex + "_Normal.png");
            if (cutout)
            {
                // The game's own painted-leaf shader: cut out, both faces, swaying in the wind, faded by TreeFade like the painted crowns.
                var leafShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Crulanda/World/Shaders/Leaf.shader") ?? Shader.Find("Crulanda/Leaf");
                m.shader = leafShader != null ? leafShader : Shader.Find("Standard"); m.color = Color.white;
                if (leafShader == null) Debug.LogWarning("NATURE_KIT the painted-leaf shader was not found at import; ZoneBuilder.DressNature sets it at run time.");
                if (baseMap != null) m.mainTexture = baseMap; else Debug.LogWarning("NATURE_KIT no texture for material " + n);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", .4f);
                if (m.HasProperty("_VertexTint")) m.SetFloat("_VertexTint", 0);
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0);
                if (m.HasProperty("_Wind")) m.SetFloat("_Wind", leaf ? .05f : .03f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .05f);
                return;
            }
            m.shader = Shader.Find("Standard"); m.color = Color.white;
            if (baseMap != null) m.mainTexture = baseMap; else Debug.LogWarning("NATURE_KIT no texture for material " + n);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Glossiness", .08f); m.SetFloat("_Metallic", 0);
        }
        /// <summary>The Medieval Village MegaKit (Quaternius, CC0, 2026-10-07): MI_Plaster, MI_WoodTrim(_Wear), MI_Brick, MI_RedBrick,
        /// MI_UnevenBrick, MI_RockTrim, MI_RoundTiles, MI_MetalOrnaments take their T_..._BaseColor and T_..._Normal (the Unity-style
        /// normals); window glass is a pale, glossy blue; the vine leaves are cut-out cards on the painted-leaf shader.</summary>
        static void VillageMaterial(Material m)
        {
            string dir = Props + "Village/Textures/", n = m.name.Replace("MI_", "").Replace("_Wear", "");
            if (n == "Vine")
            {
                var leaf = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Crulanda/World/Shaders/Leaf.shader") ?? Shader.Find("Crulanda/Leaf");
                m.shader = leaf != null ? leaf : Shader.Find("Standard"); m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "T_VineLeaf.png"); m.color = Color.white;
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", .4f); if (m.HasProperty("_VertexTint")) m.SetFloat("_VertexTint", 0); if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0);
                return;
            }
            m.shader = Shader.Find("Standard");
            if (n == "WindowGlass") { m.color = new Color(.55f, .68f, .78f); m.SetFloat("_Glossiness", .8f); m.SetFloat("_Metallic", .1f); return; }
            m.color = Color.white;
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "T_" + n + "_BaseColor.png"); var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "T_" + n + "_Normal.png");
            if (baseMap != null) m.mainTexture = baseMap; else Debug.LogWarning("VILLAGE_KIT no texture for material " + m.name);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Glossiness", n == "MetalOrnaments" ? .4f : .12f); m.SetFloat("_Metallic", n == "MetalOrnaments" ? .5f : 0);
        }
        public override uint GetVersion() { return 6; }   // 2: the prop kits, 3-5: the nature kit, 6: the village kit (2026-10-07); not raised for Vol. 3's music and the Wooden Canopy (2026-10-19): they are new, so their first import runs the rules, and a raise would re-import every texture and model

        public static void Report()
        {
            AssetDatabase.Refresh();
            var sb = new StringBuilder();
            foreach (var go in Resources.LoadAll<GameObject>("Weapons").OrderBy(g => g.name))
            {
                var b = new Bounds(); bool any = false; var mats = new System.Collections.Generic.List<string>();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mf = r.GetComponent<MeshFilter>(); var mesh = mf != null ? mf.sharedMesh : (r as SkinnedMeshRenderer)?.sharedMesh; if (mesh == null) continue;
                    var m = go.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                    foreach (var c in new[] { -1, 1 }) foreach (var d in new[] { -1, 1 }) foreach (var e in new[] { -1, 1 })
                    {
                        var p = m.MultiplyPoint3x4(mesh.bounds.center + Vector3.Scale(mesh.bounds.extents, new Vector3(c, d, e)));
                        if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                    }
                    foreach (var mat in r.sharedMaterials) if (mat != null) mats.Add(mat.name + (mat.IsKeywordEnabled("_EMISSION") ? "*" : "") + "[" + mat.shader.name + "]");
                }
                if (!any) { sb.AppendLine(go.name + " no mesh"); continue; }
                var s = b.size; int ax = s.x >= s.y && s.x >= s.z ? 0 : s.y >= s.z ? 1 : 2;
                float lo = b.min[ax], len = s[ax], pivotAt = len > 0 ? (0 - lo) / len : 0;
                sb.AppendLine(go.name + "  size " + s.ToString("0.000") + "  long " + "XYZ"[ax] + "  pivot at " + pivotAt.ToString("0.00") + " of it  centre " + b.center.ToString("0.000") + "  mats " + string.Join(", ", mats.Distinct()));
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../WeaponReport.txt"), sb.ToString());
            Debug.Log("WEAPON_REPORT_DONE");
        }
    }
}
