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
            if (!Ours) return;
            var t = (TextureImporter)assetImporter; t.maxTextureSize = assetPath.Contains("/Chest/") || assetPath.Contains("/Fantasy/") ? 1024 : 512; t.mipmapEnabled = true;
            if (assetPath.Contains("/Fantasy/") && assetPath.Contains("_Normal")) t.textureType = TextureImporterType.NormalMap;
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
            if (!assetPath.StartsWith(Props + "Fantasy/")) return;
            string sheet = m.name.Contains("Metal") ? "Metal" : m.name.Contains("Furniture") ? "Furniture" : m.name.Contains("Cloth") || m.name.Contains("Banner") ? "Cloth" : "Props";
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(Props + "Fantasy/Textures/T_Trim_" + sheet + "_BaseColor.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Props + "Fantasy/Textures/T_Trim_" + sheet + "_Normal.png");
            m.shader = Shader.Find("Standard"); m.color = Color.white;
            if (baseMap != null) m.mainTexture = baseMap;
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Glossiness", sheet == "Metal" ? .45f : .2f); m.SetFloat("_Metallic", sheet == "Metal" ? .35f : 0);
        }
        public override uint GetVersion() { return 2; }   // 2: the prop kits (2026-10-07)

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
