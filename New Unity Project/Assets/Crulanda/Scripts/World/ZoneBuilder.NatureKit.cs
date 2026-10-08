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
                    var tex = sheetFor != null && sheet != null && (sheetFor == "*" ? n.StartsWith("Leaves") : n.StartsWith(sheetFor)) ? sheet : m.mainTexture;
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
        static Texture greenSheet, autumnMask, leafMask;
        /// <summary>The kit's grey leaf mask (white where a leaf is): every kit crown and bush is this, tinted (Chris, 2026-10-07: "gold is too
        /// light and too yellow" - the kit's own green sheet is a lime that reads yellow in the sun).</summary>
        static Texture LeafMask { get { if (leafMask == null) leafMask = Resources.Load<Texture2D>("Props/Nature/Textures/Leaves_NormalTree"); return leafMask; } }
        /// <summary>The kit crowns' greens by leaf family: deep green, a fresher green, (autumn is the twisted tree), and old gold brown.</summary>
        static readonly Color[] KitLeaf = { new Color(.3f, .47f, .2f), new Color(.38f, .53f, .21f), new Color(.5f, .4f, .2f), new Color(.5f, .43f, .22f) };
        static readonly Color KitBush = new Color(.32f, .48f, .23f), KitBushDark = new Color(.24f, .38f, .2f);
        /// <summary>The twisted tree's leaf mask (grey: it takes a tint whole), for the autumn trees in russet and red-brown (note 78).</summary>
        static Texture AutumnMask { get { if (autumnMask == null) autumnMask = Resources.Load<Texture2D>("Props/Nature/Textures/Leaves_TwistedTree"); return autumnMask; } }
        /// <summary>Autumn browns with some red in them (playtest note 78: "the red is too pink... should be more brown in it"); over a
        /// mask that is near white where a leaf is, so each shows about as it reads.</summary>
        static readonly Color[] Autumn = { new Color(.6f, .22f, .1f), new Color(.6f, .13f, .09f), new Color(.6f, .38f, .14f), new Color(.5f, .19f, .11f), new Color(.66f, .11f, .08f) };   // russet-red, crimson, old gold, rust, deep red (Chris: "add some red to the trees")   // russet, red-brown, dark old gold, rust: the mask is near white where a leaf is, so these show at full strength
        /// <summary>The kit's green leaf sheet (the bush model comes with the twisted tree's autumn-red one).</summary>
        static Texture GreenSheet { get { if (greenSheet == null) greenSheet = Resources.Load<Texture2D>("Props/Nature/Textures/Leaves_NormalTree_C"); return greenSheet; } }
        /// <summary>Whether rocks here are the kit's mossy ones (art round 3): the green zones; the mountains, the ash and gloom keep the
        /// painted rock (its moss-free top, the skirts and slope seating the crags need).</summary>
        bool KitRocks { get { return !Gloom && Zone.biome != "mountain" && Zone.biome != "ash" && HasProp("Nature/Rock_Medium_1"); } }
        /// <summary>A kit rock under a root: one of the three mossy rocks by <paramref name="pick"/>, <paramref name="height"/> tall and
        /// no wider than <paramref name="width"/>, a touch into the ground so no edge floats on a slope.</summary>
        GameObject KitRock(Transform t, int pick, float height, float width, float yaw)
        {
            return ModelProp(t, "Nature/Rock_Medium_" + (1 + ((pick % 3) + 3) % 3), Vector3.down * height * .12f, yaw, height, false, width);
        }
        static readonly System.Collections.Generic.Dictionary<string, Material> kitShades = new System.Collections.Generic.Dictionary<string, Material>();
        /// <summary>A kit material in a shade (multiplied), one copy per material and shade.</summary>
        static Material KitShade(Material m, Color shade)
        {
            if (m == null) return null; string key = m.name + "|" + shade;
            if (!kitShades.TryGetValue(key, out var k) || k == null) { k = new Material(m) { name = m.name + " (shade)" }; k.color = m.color * shade; kitShades[key] = k; }
            return k;
        }
        /// <summary>The mountains' rocks (art round 6): the Megapack's rock formations and standing stones, their pale stone tinted to the
        /// mountain's; a big rock is a formation, a small one a single stone.</summary>
        bool KitCrags { get { return Zone.biome == "mountain" && HasProp("Megapack/Models/Rock_Formation_1"); } }
        static readonly string[] CragModels = { "Megapack/Models/Rock_Formation_1", "Megapack/Models/Plants/Rodck_Formation_2", "Megapack/Models/Plants/Rock_Formation_3" };
        static readonly string[] StoneModels = { "Megapack/Models/Plants/Rock_2", "Megapack/Models/Plants/Rock_3", "Megapack/Models/Plants/Rock_4", "Megapack/Models/Plants/Stone_1" };
        GameObject KitCrag(Transform t, int pick, float s, float yaw, float deep)
        {
            bool big = s > 2.2f; var models = big ? CragModels : StoneModels; pick = ((pick % models.Length) + models.Length) % models.Length;
            var go = ModelProp(t, models[pick], Vector3.down * deep, yaw, (big ? 1.5f : 1.1f) * s, false, 2.4f * s); if (go == null) return null;
            var shade = MountainStone * 1.55f; shade.a = 1;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = KitShade(mats[i], shade); r.sharedMaterials = mats; }
            return go;
        }
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
