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
        Material chemicalWater, oilSheen;
        /// <summary>The river's water: the zone's water, all of it black (a hint of green in the shallows), a dull scum at the banks, an
        /// oily slant reflection, slow and heavy, no light through it and none on the bed.</summary>
        Material ChemicalWater()
        {
            if (chemicalWater != null) return chemicalWater;
            var baseMat = art.waterSurface != null ? art.waterSurface : art.water;
            var m = new Material(baseMat) { name = "The Chemical River" };
            if (m.HasProperty("_DeepColor"))
            {
                m.SetColor("_DeepColor", new Color(.012f, .012f, .016f, 1)); m.SetColor("_MidColor", new Color(.025f, .028f, .026f, 1)); m.SetColor("_ShallowColor", new Color(.05f, .06f, .045f, 1));
                m.SetColor("_FoamColor", new Color(.24f, .27f, .16f, .38f)); m.SetColor("_BedTint", new Color(.2f, .22f, .18f, 1));
                void F(string key, float v) { if (m.HasProperty(key)) m.SetFloat(key, v); }
                F("_MidDepth", .15f); F("_DeepDepth", .5f); F("_Murk", 6); F("_Glow", .04f); F("_Caustics", 0); F("_SkyTint", .6f); F("_Sparkle", .55f);
                F("_FoamWidth", .1f); F("_FoamWaves", .15f); F("_Bump", .1f); F("_FlowSpeed", .1f); F("_Wave", .012f); F("_Refract", .004f); F("_Glare", .9f);
            }
            else m.color = new Color(.03f, .03f, .035f, baseMat.color.a);
            return chemicalWater = m;
        }
        /// <summary>
        /// The oil film over the chemical water: the creek's own surface, 3 cm up, in a faint see-through rainbow (bands of hue that drift
        /// slowly downstream), glossy so the sky slides over it: the books' iridescence (ch.16:5). Visual only.
        /// </summary>
        void OilSheen(Transform water, Mesh surface)
        {
            if (oilSheen == null)
            {
                var shader = Shader.Find("Standard"); if (shader == null) return;
                var m = new Material(shader) { name = "Oil sheen", mainTexture = OilFilm(128) };
                m.SetFloat("_Mode", 2); m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.color = new Color(1, 1, 1, .55f); m.SetFloat("_Glossiness", .92f); m.SetFloat("_Metallic", .2f);
                m.mainTextureScale = new Vector2(1 / 9f, 1 / 15f);
                oilSheen = m;
            }
            var go = new GameObject("Oil sheen"); go.transform.SetParent(water, false); go.transform.localPosition = Vector3.up * .03f;
            go.AddComponent<MeshFilter>().sharedMesh = surface;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = oilSheen; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.AddComponent<WaterDrift>();   // the film drifts downstream with the sludge
        }
        /// <summary>A tiling oil film: thin-film hues (magenta, gold, green, blue) in swirled bands, see-through between them.</summary>
        static Texture2D OilFilm(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Oil film", wrapMode = TextureWrapMode.Repeat };
            var px = new Color[size * size]; const float Tau = Mathf.PI * 2;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    // Whole periods across the tile, so it repeats without a seam.
                    float swirl = Mathf.Sin(Tau * (u * 2 + .35f * Mathf.Sin(Tau * (v * 3 + u))) ) + .6f * Mathf.Sin(Tau * (v * 2 + .4f * Mathf.Sin(Tau * (u * 3))));
                    float hue = Mathf.Repeat(swirl * .35f + u + v * .5f, 1);
                    var c = Color.HSVToRGB(hue, .65f, 1);
                    float film = Mathf.Clamp01(.25f + .45f * (.5f + .5f * Mathf.Sin(Tau * (u + v * 2) + swirl * 2)));
                    c.a = film * .5f;
                    px[y * size + x] = c;
                }
            t.SetPixels(px); t.Apply(true, true); return t;
        }
    }
}
