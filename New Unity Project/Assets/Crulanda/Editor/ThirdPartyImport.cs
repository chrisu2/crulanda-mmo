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
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var t = (TextureImporter)assetImporter; t.maxTextureSize = assetPath.Contains("/Chest/") ? 1024 : 512; t.mipmapEnabled = true;
        }
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var m = (ModelImporter)assetImporter; m.isReadable = true; m.importCameras = false; m.importLights = false;
        }

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
