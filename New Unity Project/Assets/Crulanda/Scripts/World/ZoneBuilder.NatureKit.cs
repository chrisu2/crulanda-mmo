using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Stylized Nature MegaKit as the zones' trees and bushes (art round 2, 2026-10-07): five broadleaf trees, five pines and a
    /// bush under Resources/Props/Nature, stood by <see cref="ModelProp"/>. A kit tree keeps everything a painted one had: the
    /// prop root's own turn and scale, a trunk collider the navmesh keeps clear, and TreeFade so it turns see-through between the
    /// camera and you. The builders take the same draws from the zone's stream as before, so nothing else in a zone moves. When
    /// the kit is not there (the tests' bare scenes) the painted trees are built as they always were.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        static readonly System.Collections.Generic.Dictionary<string, Material> kitLeafMaterials = new System.Collections.Generic.Dictionary<string, Material>();
        /// <summary>
        /// The kit's leaf, grass and flower sheets are alpha cards: on the painted-leaf shader (cut out, both faces, swaying, faded by
        /// TreeFade) they read as leaves; on an opaque shader the sheet's clear parts show as red. The importer sets the shader; this
        /// sets it again at run time for any material that missed it (a build made before the import caught up). One copy per sheet.
        /// </summary>
        public static void DressNature(GameObject go, Color? tint = null, string sheetFor = null, Texture sheet = null)
        {
            var leaf = Shader.Find("Crulanda/Leaf"); if (leaf == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (m == null) continue;
                    string n = m.name; int cut = n.IndexOf(" (", System.StringComparison.Ordinal); if (cut > 0) n = n.Substring(0, cut);   // a dressed copy's source name
                    bool card = n.StartsWith("Leaves") || n.StartsWith("Leaf") || n.StartsWith("Grass") || n.StartsWith("Flower") || n.StartsWith("Petal") || n.StartsWith("Plant") || n.StartsWith("Fern") || n.StartsWith("Clover") || n.StartsWith("Mushroom");
                    if (!card) continue;
                    var tex = sheetFor != null && n.StartsWith(sheetFor) && sheet != null ? sheet : m.mainTexture;
                    var colour = tint ?? Color.white; if (m.shader == leaf && !tint.HasValue && tex == m.mainTexture) continue;   // dressed already, nothing to change
                    string key = n + "|" + (tex != null ? tex.name : "") + "|" + colour;
                    if (!kitLeafMaterials.TryGetValue(key, out var k) || k == null)
                    {
                        k = new Material(leaf) { name = n + " (leaf " + colour + ")", mainTexture = tex, color = colour };
                        k.SetFloat("_Cutoff", .4f); k.SetFloat("_VertexTint", 0); k.SetFloat("_Cull", 0); k.SetFloat("_Wind", n.StartsWith("Leav") || n.StartsWith("Leaf") ? .05f : .03f); k.SetFloat("_Glossiness", .05f);
                        kitLeafMaterials[key] = k;
                    }
                    mats[i] = k; changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }
        static Texture greenSheet;
        /// <summary>The kit's green leaf sheet (the bush model comes with the twisted tree's autumn-red one).</summary>
        static Texture GreenSheet { get { if (greenSheet == null) greenSheet = Resources.Load<Texture2D>("Props/Nature/Textures/Leaves_NormalTree_C"); return greenSheet; } }
        /// <summary>Whether the nature kit is in the project (one look, cached by PropSource).</summary>
        public static bool KitTrees { get { return HasProp("Nature/CommonTree_1") && HasProp("Nature/Pine_1"); } }
        /// <summary>A kit tree under a prop root: the model to <paramref name="height"/> (before the root's own scale), a trunk collider
        /// of <paramref name="trunk"/> radius, a NavBlocker and a TreeFade on the root. Null when the model is missing.</summary>
        GameObject KitTree(Transform root, string path, float height, float trunk, float yaw = 0)
        {
            var go = ModelProp(root, path, Vector3.zero, yaw, height); if (go == null) return null;
            var cap = root.gameObject.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, height * .45f, 0); cap.height = height * .9f; cap.radius = trunk;
            root.gameObject.AddComponent<NavBlocker>(); root.gameObject.AddComponent<TreeFade>();
            return go;
        }
    }
}
