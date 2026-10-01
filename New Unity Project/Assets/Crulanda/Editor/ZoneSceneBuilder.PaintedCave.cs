using UnityEditor;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The painted style pass: cave interiors. Two materials on the Crulanda/PaintedCave shader (world-projected paint under the
    /// shading ZoneBuilder.CaveLining paints into the lining's vertices): art.cave, the bedded rock of part 2 at a closer scale
    /// (Crowsfoot Hollow), and art.caveEarth, packed earth with roots running through it (the Root-Mother's Deep).
    /// </summary>
    public static partial class ZoneSceneBuilder
    {
        /// <summary>The root strands of the painted earth: (across, up, half-width in pixels, offset). Across and up are whole
        /// numbers, so a strand that leaves the tile comes back in on the other side.</summary>
        static readonly Vector4[] RootRuns = { new Vector4(1, 2, 7, .1f), new Vector4(2, -1, 5, .42f), new Vector4(-1, 3, 3.5f, .7f), new Vector4(3, 1, 3, .23f) };
        /// <summary>Packed earth with roots: loam in broad posterised patches and spade-flat planes, damp seams where one layer lies
        /// on the next, pale pebbles, and four families of pale root winding through it (each thick in places and gone in others,
        /// lit along one side, a dark line of earth where it lies). Seamless: the noise tiles and the strands wrap.</summary>
        static Color EarthPixel(int x, int y)
        {
            float v = .6f + (Paint(Fbm(x, y, .012f, 2), 4) - .5f) * .2f + (Fbm(x, y, .06f, 2) - .5f) * .1f;
            v += (Paint(Tiled((px, py) => Perlin(px * .8f + py * .6f, (py * .8f - px * .6f) * 2.6f, .01f), x, y), 3) - .5f) * .1f;   // spade-flat planes
            float lay = Mathf.Repeat((y + (Fbm(x, y, .007f, 2) - .5f) * 80) / T * 4, 1);
            v *= 1 - .18f * Smooth(.1f, 0, Mathf.Min(lay, 1 - lay));   // the damp seams
            float stone = Fbm(x, y, .05f, 2, 1, 1, 310, 97), pebble = Smooth(.66f, .72f, stone);
            v *= 1 + .22f * pebble - .12f * Smooth(.6f, .66f, stone) * (1 - pebble);   // pebbles, each in its dark socket
            float root = 0, lit = 1;
            for (int r = 0; r < RootRuns.Length; r++)
            {
                var run = RootRuns[r]; float len = Mathf.Sqrt(run.x * run.x + run.y * run.y);
                float f = Mathf.Repeat((run.x * x + run.y * y) / T + run.w + (Fbm(x, y, .009f, 2, 1, 1, r * 211, r * 97) - .5f) * .5f, 1) - .5f;
                float d = Mathf.Abs(f) * T / len;   // pixels from the strand's middle
                float w = run.z * (.55f + .9f * Fbm(x, y, .02f, 2, 1, 1, r * 53, r * 131)), there = Smooth(.4f, .5f, Fbm(x, y, .006f, 2, 1, 1, r * 401 + 50, r * 173));
                float mask = Smooth(w, w - 2.5f, d) * there;
                v *= 1 - .22f * Smooth(w + 6, w, d) * there * (1 - mask);   // the earth dark where the root lies in it
                if (mask > root) { root = mask; lit = 1 + .16f * Mathf.Clamp(f * T / len / Mathf.Max(1, w), -1, 1); }
            }
            float warm = (Fbm(x, y, .004f, 2) - .5f) * .1f;
            var earth = new Color(v * (1.05f + warm), v * .98f, v * (.88f - warm));
            float bark = lit * (.9f + .2f * Fbm(x, y, .09f, 2, 1, 1, 77, 19));
            var pale = new Color(.86f * bark, .74f * bark, .56f * bark);
            var c = Color.Lerp(earth, pale, root);
            return new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b));
        }
        /// <summary>
        /// Make the two cave materials (art.cave, art.caveEarth), or point existing ones at the shader and their textures. The rock
        /// is whatever texture art.rock wears (EnsurePaintedRock runs first); the earth PNG's name ends in a number because Tex
        /// writes a PNG once and then reuses it. If the shader has not been imported yet, or there is no painted rock, both stay
        /// null and ZoneBuilder keeps a cave's faceted shell on stone or bark; run the art build again.
        /// </summary>
        static void EnsurePaintedCave(ZoneArt art)
        {
            var shader = Shader.Find("Crulanda/PaintedCave");
            if (shader == null || art.rock == null || art.rock.mainTexture == null) { Debug.LogWarning("Crulanda/PaintedCave is not imported yet (or there is no painted rock): caves stay on art.stone and art.bark (run the art build again)."); return; }
            Material Ensure(Material m, string name, Texture tex, float scale)
            {
                if (m == null)
                {
                    m = new Material(shader) { name = name, mainTexture = tex }; m.SetFloat("_Scale", scale);
                    AssetDatabase.CreateAsset(m, ArtRoot + "/" + name + ".mat"); EditorUtility.SetDirty(art);
                }
                else if (m.shader != shader || m.mainTexture != tex) { m.shader = shader; m.mainTexture = tex; m.SetFloat("_Scale", scale); EditorUtility.SetDirty(m); }
                return m;
            }
            art.cave = Ensure(art.cave, "Painted cave", art.rock.mainTexture, 3.6f);
            art.caveEarth = Ensure(art.caveEarth, "Painted cave earth", Tex("earth_roots1", T, EarthPixel), 3);
        }
    }
}
