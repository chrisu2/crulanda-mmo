using System;
using UnityEditor;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The painted style pass (GAME_BRIEF: a hand-painted classic-MMO look). Building textures in colour with visible brushwork,
    /// tiling per metre (walls are <see cref="ZoneMeshes.Box"/> with metre UVs, 2 m a tile; roofs 2.5 m): plaster in broad brush
    /// patches and strokes, ragged layers of thatch, crooked slates, coursed stone with rounded blocks and warm mortar, planked
    /// timber with knots. Every one is seamless: noise is blended across the tile's edges and every row, column and plank index
    /// wraps. Generated once as PNGs like the rest of the art, and put on the existing materials (so every zone, old and new,
    /// picks them up on its next build).
    /// </summary>
    public static partial class ZoneSceneBuilder
    {
        const int T = 512;
        /// <summary>Noise that tiles: the four corner-shifted samples blended by position, renormalised so the contrast stays even
        /// across the tile. x,y must be the raw pixel (0..T); any offset or stretch belongs inside <paramref name="p"/>.</summary>
        static float Tiled(Func<float, float, float> p, float x, float y)
        {
            float u = x / T, v = y / T, a = (1 - u) * (1 - v), b = u * (1 - v), c = (1 - u) * v, d = u * v;
            float n = p(x, y) * a + p(x - T, y) * b + p(x, y - T) * c + p(x - T, y - T) * d;
            return .5f + (n - .5f) / Mathf.Sqrt(a * a + b * b + c * c + d * d);
        }
        /// <summary>Tiling fractal noise. x,y must be the raw pixel (0..T). The stretch (sx, sy) and the offset (ox, oy, in pixels)
        /// are applied inside the sample, so the result still tiles.</summary>
        static float Fbm(float x, float y, float f, int octaves = 3, float sx = 1, float sy = 1, float ox = 0, float oy = 0)
        {
            float v = 0, a = .5f, sum = 0;
            for (int o = 0; o < octaves; o++) { float ff = f; v += Tiled((px, py) => Perlin(px * sx + ox, py * sy + oy, ff), x, y) * a; sum += a; f *= 2.07f; a *= .55f; }
            return v / sum;
        }
        /// <summary>Soft posterise: noise into a few flat values with soft edges (the brush patches). If the patches look too flat or
        /// too hard in Unity, the gain is the number to change.</summary>
        static float Paint(float n, int levels, float gain = 2.2f, float soft = .2f)
        {
            float q = Mathf.Clamp01((n - .5f) * gain + .5f) * levels, fl = Mathf.Floor(q);
            return Mathf.Clamp01((fl + Smooth(.5f - soft, .5f + soft, q - fl)) / levels);
        }
        static float Hash(int a, int b) { uint h = (uint)(a * 374761393 + b * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
        static Color Mul(Color c, float v) { return new Color(Mathf.Clamp01(c.r * v), Mathf.Clamp01(c.g * v), Mathf.Clamp01(c.b * v), 1); }
        static float Smooth(float a, float b, float x) { x = Mathf.Clamp01((x - a) / (b - a)); return x * x * (3 - 2 * x); }

        /// <summary>Plaster: a warm near-white in broad posterised patches, two layers of long brush strokes, sparse ochre stains and
        /// a speckle. It has no grimed foot: a foot in a tiling texture draws a line every 2 m up the wall.</summary>
        static Color PlasterPixel(int x, int y)
        {
            float broad = Paint(Fbm(x, y, .007f, 2), 4) - .5f;
            float s1 = Paint(Tiled((px, py) => Perlin(px * .8f + py * .6f, (py * .8f - px * .6f) * 4, .011f), x, y), 3) - .5f;   // strokes about 35 cm long, 8 cm wide
            float s2 = Paint(Tiled((px, py) => Perlin(px * .5f - py * .87f + 300, (py * .5f + px * .87f) * 4, .016f), x, y), 3) - .5f;
            float fine = Fbm(x, y, .09f, 2) - .5f;
            float v = .86f + broad * .09f + s1 * .07f + s2 * .035f + fine * .03f;
            float stain = Smooth(.62f, .74f, Fbm(x, y, .009f, 2, 1, 1, 900, 400));
            v *= 1 - .07f * stain;
            var c = new Color(v, v * (.975f - .02f * stain), v * (.93f - .07f * stain));
            if (Hash(x / 4, y / 4) > .992f) c = Mul(c, .9f);   // speckle (4 divides 512)
            return c;
        }
        /// <summary>Thatch: seven ragged layers 0.36 m deep (the roof tiles 2.5 m; v runs up the slope). Each layer has light tufted
        /// tips at its foot and lies in the shadow of the layer above at its top; clumps and strands run up the slope.</summary>
        static Color ThatchPixel(int x, int y)
        {
            const float rows = 7;
            float ry = y / (float)T * rows + (Fbm(x, y, .03f, 2) - .5f) * .5f + (Fbm(x, y, .13f, 1, 1, .15f) - .5f) * .4f;   // ragged, tufted layer edge
            float rr = Mathf.Floor(ry), f = ry - rr, row = rr - Mathf.Floor(rr / rows) * rows;   // f: 0 at the layer's tips, 1 under the layer above; row wraps so the tile is seamless
            float clump = Paint(Fbm(x, y, .05f, 2, 1, .12f, row * 61), 3) - .5f;
            float strand = Fbm(x, y, .22f, 2, 1, .1f, row * 37);
            float v = .64f + .16f * Smooth(.55f, 0, f) + clump * .16f + (strand - .5f) * .3f;   // tips lighter
            v *= 1 - .45f * Smooth(.6f, 1, f);   // shadow cast by the layer above
            v *= 1 - .4f * Smooth(.14f, 0, f) * Smooth(.52f, .38f, strand);   // dark gaps between strands at the tips
            v = Mathf.Clamp01(v);
            return new Color(v, v * (.7f + .22f * v), v * (.4f + .36f * v));   // shadows warmer and more saturated
        }
        /// <summary>Slate: seven rows of six slates (0.36 by 0.42 m), staggered, each its own value and hue (blue, violet-grey,
        /// green-grey) with its own crooked bottom edge: a dark gap under it, a lit lip on it, the shadow of the row above at its
        /// top, and moss in the shadowed tops.</summary>
        static Color SlatePixel(int x, int y)
        {
            const float rows = 7, cols = 6; float colW = T / cols;
            float ry = y / (float)T * rows; int row = Mathf.FloorToInt(ry); float fy = ry - row;
            float cx = x / (float)T * cols + (row % 2) * .5f + (Hash(row, 3) - .5f) * .24f, cr = Mathf.Floor(cx), fx = cx - cr;
            int col = (int)(cr - Mathf.Floor(cr / cols) * cols);   // wrapped: the slate cut by the tile edge is one slate
            float h = Hash(col, row), hue = Hash(col + 41, row + 23);
            float bottom = .03f + Hash(col + 17, row + 5) * .09f + (fx - .5f) * (Hash(col + 3, row + 9) - .5f) * .1f;   // each slate's own, slightly crooked, bottom edge
            float v = .66f + (h - .5f) * .3f + (Paint(Fbm(x, y, .02f, 2), 3) - .5f) * .1f + (Fbm(x, y, .09f, 2) - .5f) * .06f;
            v += .08f * Smooth(.6f, 0, fy);
            v *= 1 - .42f * Smooth(.7f, 1, fy);                 // shadow of the row above
            v *= 1 + .18f * Smooth(.08f, .01f, fy - bottom);    // lit lip at the slate's bottom edge
            float jx = Mathf.Min(fx, 1 - fx) * colW;
            if (jx < 2) v *= .5f; else if (fx < .5f && jx < 5) v *= 1.1f;   // joint, and its lit side
            if (fy < bottom) v = .26f;                          // the gap under the edge
            var c = hue < .4f ? new Color(v * .9f, v * .95f, v * 1.02f) : hue < .75f ? new Color(v * .97f, v * .94f, v) : new Color(v * .94f, v * .98f, v * .95f);
            float moss = Smooth(.55f, .7f, Fbm(x, y, .012f, 2)) * Smooth(.35f, 1, fy) * .7f;
            return Color.Lerp(c, new Color(v * .72f, v * .95f, v * .5f), moss);
        }
        /// <summary>Course boundaries of the dressed stone, as fractions of the tile (five uneven courses of 0.34 to 0.46 m at 2 m a tile).</summary>
        static readonly float[] StoneCourses = { 0, .17f, .39f, .57f, .8f, 1 };
        /// <summary>Coursed stone: five courses of uneven height, two to four blocks a course, with rounded corners and wobbly warm
        /// mortar; each block has its own value and warmth, posterised facets, a lit top and left and a shaded bottom and right.</summary>
        static Color StonePixel(int x, int y)
        {
            float fyT = y / (float)T; int row = 0; while (row < 4 && fyT >= StoneCourses[row + 1]) row++;
            float rowH = (StoneCourses[row + 1] - StoneCourses[row]) * T, py = (fyT - StoneCourses[row]) * T;
            float cols = 2 + Mathf.Floor(Hash(row, 3) * 2.99f), colW = T / cols;
            float cx = x / (float)T * cols + Hash(row, 11), cr = Mathf.Floor(cx), fx = cx - cr;
            int col = (int)(cr - Mathf.Floor(cr / cols) * cols);   // wrapped: the block cut by the tile edge is one block
            float wob = (Fbm(x, y, .05f, 2) - .5f) * 6;
            float dx = Mathf.Min(fx, 1 - fx) * colW, dy = Mathf.Min(py, rowH - py); const float r = 13;
            float d = (dx < r && dy < r ? r - Mathf.Sqrt((r - dx) * (r - dx) + (r - dy) * (r - dy)) : Mathf.Min(dx, dy)) + wob;   // rounded corners, wobbly edge
            if (d < 3.5f) { float m = .38f + Fbm(x, y, .1f, 2) * .1f; return new Color(m * 1.06f, m, m * .9f); }   // mortar
            float h = Hash(col + row * 31, row);
            float v = .7f + (h - .5f) * .26f + (Paint(Fbm(x, y, .018f, 2), 3) - .5f) * .14f + (Fbm(x, y, .08f, 2) - .5f) * .06f;
            float lit = Smooth(12, 4, Mathf.Min(rowH - py, fx * colW) + wob), shade = Smooth(12, 4, Mathf.Min(py, (1 - fx) * colW) + wob);
            v *= 1 + .16f * lit - .22f * shade;   // lit top and left, shadowed bottom and right
            float warm = (Hash(col * 7 + 1, row * 3 + 2) - .5f) * .1f;
            return new Color(Mathf.Clamp01(v * (1 + warm)), Mathf.Clamp01(v), Mathf.Clamp01(v * (1 - warm)));
        }
        /// <summary>Timber: eight planks 0.25 m wide up the tile, each with long posterised grain streaks, a lit left edge and a
        /// shaded right one, a dark seam between planks, a knot in about half and a butt joint in most.</summary>
        static Color TimberPixel(int x, int y)
        {
            float plankW = T / 8f; int plank = Mathf.FloorToInt(x / plankW); float px = x - plank * plankW;
            float streak = Paint(Fbm(x, y, .06f, 2, 1, .08f, plank * 53), 3) - .5f;   // long grain streaks
            float fine = Fbm(x, y, .3f, 1, 1, .05f, plank * 31) - .5f;
            float v = .72f + (Hash(plank, 5) - .5f) * .16f + streak * .16f + fine * .1f;
            if (Hash(plank, 13) > .45f)
            {   // a knot, kept clear of the tile's top and bottom
                float ky = (.12f + .76f * Hash(plank, 9)) * T, kx = (plank + .3f + .4f * Hash(plank, 17)) * plankW;
                float d = Mathf.Sqrt(Mathf.Pow((x - kx) / 9, 2) + Mathf.Pow((y - ky) / 14, 2));
                v *= d < 1 ? .55f + .3f * Mathf.Abs(Mathf.Sin(d * 7)) : 1 - .14f * Smooth(2.2f, 1, d);
            }
            if (Hash(plank, 29) > .4f)
            {   // a butt joint (the distance wraps, so it tiles)
                float jd = Mathf.Abs(y - Hash(plank, 23) * T); jd = Mathf.Min(jd, T - jd);
                v *= jd < 1.5f ? .55f : 1 - .1f * Smooth(7, 1.5f, jd);
            }
            if (px < 1.5f || px > plankW - 1.5f) v *= .5f;   // the seam
            else if (px < 5) v *= 1.1f;                       // lit edge
            else if (px > plankW - 7) v *= .88f;              // shaded edge
            return new Color(v, v * .88f, v * .74f);
        }

        /// <summary>
        /// Put the painted textures on the materials that build the villages, and make the masonry material. The PNG names end in
        /// "_painted2": Tex writes a PNG once and then reuses it, so a repaint of a pixel function takes a new name. The tints suit
        /// the new textures, which carry their own colour: the slate texture is mid-grey (mean about .57), so its tint is light
        /// (tinted mean about .45).
        /// </summary>
        static void PaintedTextures(ZoneArt art)
        {
            Retexture(art.plaster, Tex("plaster_painted2", T, PlasterPixel), new Color(.86f, .8f, .68f), .06f);
            Retexture(art.thatch, Tex("thatch_painted2", T, ThatchPixel), new Color(.78f, .64f, .36f), .02f);
            Retexture(art.slate, Tex("slate_painted2", T, SlatePixel), new Color(.72f, .79f, .88f), .15f);
            Retexture(art.timber, Tex("timber_painted2", T, TimberPixel), new Color(.44f, .3f, .19f), .08f);
            // Masonry is its own material: art.stone stays the natural rock of crags, boulders and cave walls.
            var stone = Tex("stone_painted2", T, StonePixel); var grey = new Color(.62f, .6f, .55f);
            if (art.masonry == null) { art.masonry = Standard("Masonry", grey, stone, .08f); EditorUtility.SetDirty(art); }
            else Retexture(art.masonry, stone, grey, .08f);
        }
        /// <summary>Point a material at a texture and a tint (nothing is touched when it already has both).</summary>
        static void Retexture(Material m, Texture2D tex, Color color, float smoothness)
        {
            if (m == null || (m.mainTexture == tex && m.color == color)) return;
            m.mainTexture = tex; m.color = color; m.SetFloat("_Glossiness", smoothness); EditorUtility.SetDirty(m);
        }
    }
}
