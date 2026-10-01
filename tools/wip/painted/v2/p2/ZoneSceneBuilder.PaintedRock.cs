using UnityEditor;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The painted style pass, part 2: natural rock. One seamless texture of bedded rock (5 m a tile in the world, see the
    /// Crulanda/PaintedRock shader, which projects it in world space) and the material ZoneBuilder tints for crags, cliffs and
    /// boulders. Built stone is the masonry of part 1; cave interiors stay on art.stone.
    /// </summary>
    public static partial class ZoneSceneBuilder
    {
        /// <summary>Bed boundaries of the painted rock, as fractions of the tile (six uneven beds of 0.6 to 1.3 m at 5 m a tile).</summary>
        static readonly float[] RockBeds = { 0, .13f, .36f, .48f, .74f, .86f, 1 };
        /// <summary>Bedded rock: six beds of uneven thickness, wobbled and wrapped, each split into one to three blocks by slanted,
        /// warped joints. Each block has its own value; posterised patches and broad diagonal brush planes lie over them, with a
        /// shadow line at a bed's foot and a worn, lit lip at its top. Seamless: the noise tiles and the bed and block indices wrap.</summary>
        static Color RockPixel(int x, int y)
        {
            float wob = (Fbm(x, y, .006f, 2) - .5f) * 70 + (Fbm(x, y, .03f, 2) - .5f) * 14;
            float by = Mathf.Repeat((y + wob) / T, 1); int bed = 0; while (bed < 5 && by >= RockBeds[bed + 1]) bed++;
            float h = (RockBeds[bed + 1] - RockBeds[bed]) * T, f = (by - RockBeds[bed]) * T / h;   // h: bed thickness in px; f: 0 at its foot, 1 at its top
            float cols = 1 + Mathf.Floor(Hash(bed, 3) * 2.99f);
            float cx = x / (float)T * cols + Hash(bed, 11) + (Fbm(x, y, .02f, 2) - .5f) * .6f / cols + (f - .5f) * (Hash(bed, 31) - .5f) * .25f;   // warped, slanted joints
            float cr = Mathf.Floor(cx), fx = cx - cr; int col = (int)(cr - Mathf.Floor(cr / cols) * cols);
            float v = .78f + (Hash(col + bed * 31, bed) - .5f) * .14f + (Hash(bed, 21) - .5f) * .12f;
            float plane = Paint(Tiled((px, py) => Perlin(px * .9f + py * .44f, (py * .9f - px * .44f) * 3, .009f), x, y), 3) - .5f;   // broad diagonal brush planes
            v += (Paint(Fbm(x, y, .012f, 2), 4) - .5f) * .14f + plane * .1f + (Fbm(x, y, .07f, 2) - .5f) * .06f;
            v *= 1 - .34f * Smooth(14, 0, f * h);          // the shadow line at a bed's foot
            v *= 1 + .1f * Smooth(22, 3, (1 - f) * h);     // its worn, lit lip
            float jd = Mathf.Min(fx, 1 - fx) * T / cols;
            v *= 1 - .38f * Smooth(4, 1, jd);              // the joint
            if (fx < .5f && jd > 4 && jd < 9) v *= 1.06f;  // its lit side
            float warm = (Fbm(x, y, .004f, 2) - .5f) * .1f;
            return new Color(Mathf.Clamp01(v * (1 + warm)), Mathf.Clamp01(v), Mathf.Clamp01(v * (1 - warm * 1.2f)));
        }
        /// <summary>
        /// Make the painted rock material (art.rock), or point an existing one at the shader and the texture. The PNG name ends in
        /// "2" for the same reason as the building textures: Tex writes a PNG once and then reuses it. If the shader has not been
        /// imported yet, art.rock stays null and ZoneBuilder keeps natural rock on art.stone; run the art build again.
        /// </summary>
        static void EnsurePaintedRock(ZoneArt art)
        {
            var shader = Shader.Find("Crulanda/PaintedRock");
            if (shader == null) { Debug.LogWarning("Crulanda/PaintedRock is not imported yet: rock stays on art.stone (run the art build again)."); return; }
            var tex = Tex("rock_painted2", T, RockPixel);
            if (art.rock == null)
            {
                art.rock = new Material(shader) { name = "Painted rock", mainTexture = tex };
                AssetDatabase.CreateAsset(art.rock, ArtRoot + "/Painted rock.mat"); EditorUtility.SetDirty(art);
            }
            else if (art.rock.shader != shader || art.rock.mainTexture != tex) { art.rock.shader = shader; art.rock.mainTexture = tex; EditorUtility.SetDirty(art.rock); }
        }
    }
}
