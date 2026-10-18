using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Chemical River (Lowtowns pick 6; CANON, book1 ch.15-16): "the iridescent waterway that bisected Argentis like an infected artery"
    /// (ch.16:5), "a thick, ugly ribbon of bubbling black sludge" (ch.15:335), "entirely black, agonizingly thick as curdled molasses" (ch.15:391),
    /// an "oily scum layer" on it (ch.16:5). A creek of kind "chemical" (<see cref="ZonePath.kind"/>) is drawn in its own water: black all
    /// the way down, a dull green-grey scum at the banks, slow, and over it a thin oil film that shows the rainbow where the light slants
    /// (<see cref="ChemicalWater"/>, <see cref="OilSheen"/>). <see cref="ChemicalAt"/> says who stands in it: the game's rules for it (it
    /// poisons, it hides you from magical tracking) are Encounter.ChemicalRiver's.
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The kind of water that is the Chemical River.</summary>
        public const string ChemicalKind = "chemical";
        static bool Chemical(ZonePath w) { return w != null && w.kind == ChemicalKind; }
        /// <summary>Whether this zone has a chemical river (the Lowtowns).</summary>
        public bool HasChemical { get { return Water != null && Water.Creeks.Exists(c => Chemical(c.def)); } }
        /// <summary>
        /// Whether an actor (its transform a metre over its feet, as every actor here stands) is in the chemical water: inside a chemical
        /// creek's drawn water, its feet under the surface. A bridge's deck over it is dry.
        /// </summary>
        public bool ChemicalAt(Vector3 p)
        {
            if (Water == null) return false;
            var q = new Vector2(p.x, p.z); float ground = float.NaN;
            foreach (var c in Water.Creeks)
            {
                if (!Chemical(c.def) || c.drawHalf <= 0 || !c.box.Contains(q)) continue;
                ZoneWater.Project(c, q, out float d, out float level, out float wide);
                if (d > c.DrawHalf(wide)) continue;
                if (float.IsNaN(ground)) ground = HeightAt(q.x, q.y);
                if (level - ground > .05f && p.y - 1 < level - .05f) return true;
            }
            return false;
        }
        Material chemicalWater, oilSheen, sludgeBubble;
        /// <summary>The river's water: the zone's water, all of it black (a hint of green in the shallows), a dull green-grey scum at the
        /// banks, a calm surface (no painted sparkle, hardly a ripple: "thick as curdled molasses", ch.15:391), slow, no light through it
        /// and none on the bed.</summary>
        Material ChemicalWater()
        {
            if (chemicalWater != null) return chemicalWater;
            var baseMat = art.waterSurface != null ? art.waterSurface : art.water;
            var m = new Material(baseMat) { name = "The Chemical River" };
            if (m.HasProperty("_DeepColor"))
            {
                m.SetColor("_DeepColor", new Color(.008f, .008f, .01f, 1)); m.SetColor("_MidColor", new Color(.016f, .018f, .017f, 1)); m.SetColor("_ShallowColor", new Color(.04f, .048f, .036f, 1));
                m.SetColor("_FoamColor", new Color(.2f, .23f, .15f, .5f)); m.SetColor("_BedTint", new Color(.16f, .18f, .14f, 1));
                void F(string key, float v) { if (m.HasProperty(key)) m.SetFloat(key, v); }
                F("_MidDepth", .15f); F("_DeepDepth", .5f); F("_Murk", 6); F("_Glow", .02f); F("_Caustics", 0); F("_SkyTint", .45f); F("_Sparkle", .08f);
                F("_FoamWidth", .2f); F("_FoamWaves", .04f); F("_Bump", .035f); F("_FlowSpeed", .04f); F("_Wave", .006f); F("_Refract", .001f); F("_Glare", .6f);
            }
            else m.color = new Color(.02f, .02f, .024f, baseMat.color.a);
            return chemicalWater = m;
        }
        /// <summary>
        /// The oil film over the chemical water: the creek's own surface, 3 cm up, near-black and glossy, so the sky slides over it at a
        /// slant and the books' iridescence (ch.16:5) is only a faint, slow, low-coloured sheen in the highlights: large smooth swirls,
        /// tens of metres across, drifting downstream at a crawl. A few slow sludge bubbles stand on it, glossy black, catching a glint.
        /// Visual only.
        /// </summary>
        void OilSheen(Transform water, Mesh surface)
        {
            if (oilSheen == null)
            {
                var shader = Shader.Find("Standard"); if (shader == null) return;
                var m = new Material(shader) { name = "Oil sheen", mainTexture = OilFilm(128) };
                // Standard's Transparent mode: the colour is faint, the reflection stays whole (the sheen at a slant).
                m.SetFloat("_Mode", 3); m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.color = new Color(1, 1, 1, .7f); m.SetFloat("_Glossiness", .86f); m.SetFloat("_Metallic", 0);
                m.mainTextureScale = new Vector2(1 / 34f, 1 / 52f);
                oilSheen = m;
            }
            var go = new GameObject("Oil sheen"); go.transform.SetParent(water, false); go.transform.localPosition = Vector3.up * .03f;
            go.AddComponent<MeshFilter>().sharedMesh = surface;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = oilSheen; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            // The film drifts downstream with the sludge, at a crawl (a tile is 52 m: about 15 cm a second).
            go.AddComponent<WaterDrift>().speed = .003f;
            SludgeBubbles(water, surface);
        }
        /// <summary>A sparse scatter of sludge bubbles over the deeper water (vertex alpha under .6), flattened glossy black domes from
        /// 8 to 30 cm in little clusters, joined into one mesh: "bubbling black sludge" (ch.15:335). Visual only; its own stream.</summary>
        void SludgeBubbles(Transform water, Mesh surface)
        {
            var verts = surface.vertices; var cols = surface.colors; if (verts.Length == 0) return;
            if (sludgeBubble == null)
            {
                sludgeBubble = new Material(oilSheen.shader) { name = "Sludge bubble", color = new Color(.02f, .022f, .02f) };
                sludgeBubble.SetFloat("_Glossiness", .93f); sludgeBubble.SetFloat("_Metallic", .1f);
            }
            var r = new System.Random(verts.Length * 31 + 7); float R() { return (float)r.NextDouble(); }
            var dome = ZoneMeshes.Blob(5, .05f, true, 5, 8);
            var list = new List<CombineInstance>();
            for (int i = 0, n = Mathf.Min(220, verts.Length / 14); i < n; i++)
            {
                int k = (int)(R() * verts.Length); if (cols.Length == verts.Length && cols[k].a > .6f) continue;
                float size = .08f + R() * R() * .22f;
                var at = verts[k] + new Vector3((R() - .5f) * .8f, .02f, (R() - .5f) * .8f);
                // One big bubble and a few small ones beside it.
                for (int j = 0, m = 1 + (int)(R() * 3); j < m; j++)
                {
                    float sj = j == 0 ? size : size * (.3f + R() * .4f);
                    var p = at + (j == 0 ? Vector3.zero : new Vector3((R() - .5f) * size * 3, 0, (R() - .5f) * size * 3));
                    list.Add(new CombineInstance { mesh = dome, transform = Matrix4x4.TRS(p, Quaternion.Euler(0, R() * 360, 0), new Vector3(sj, sj * .55f, sj)) });
                }
            }
            if (list.Count == 0) return;
            var mesh = new Mesh { name = "Sludge bubbles", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(list.ToArray(), true); mesh.RecalculateBounds();
            var go = new GameObject("Sludge bubbles"); go.transform.SetParent(water, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = sludgeBubble; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        /// <summary>A tiling oil film: near-black, a thin-film hue (dull violet, bronze, bottle green, slate blue) at low saturation in a
        /// few large smooth swirls a tile, mostly see-through; no fine noise.</summary>
        static Texture2D OilFilm(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Oil film", wrapMode = TextureWrapMode.Repeat };
            var px = new Color[size * size]; const float Tau = Mathf.PI * 2;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    // Whole periods across the tile, so it repeats without a seam; one or two swirls each way.
                    float warp = .18f * Mathf.Sin(Tau * (v + .3f * Mathf.Sin(Tau * u)));
                    float swirl = Mathf.Sin(Tau * (u + warp)) * Mathf.Cos(Tau * (v - .5f * warp)) + .5f * Mathf.Sin(Tau * (u + 2 * v + warp));
                    float hue = Mathf.Repeat(.72f + swirl * .22f, 1);
                    var c = Color.HSVToRGB(hue, .32f, .42f);
                    // The film thick in the swirls' bands and gone between them.
                    float film = Mathf.SmoothStep(0, 1, Mathf.Clamp01(.5f + .6f * Mathf.Sin(Tau * (u * 2 + v) + swirl * 1.6f)));
                    c.a = .05f + film * .2f;
                    // Premultiplied, for Standard's Transparent mode.
                    c.r *= c.a; c.g *= c.a; c.b *= c.a;
                    px[y * size + x] = c;
                }
            t.SetPixels(px); t.Apply(true, true); return t;
        }
    }
}
