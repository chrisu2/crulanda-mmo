using System;
using UnityEditor;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.EditorTools
{
    /// <summary>
    /// The painted style pass (GAME_BRIEF: a hand-painted classic-MMO look). Building textures in colour with visible brushwork,
    /// tiling per metre (walls are <see cref="ZoneMeshes.Box"/> with metre UVs, 2 m a tile; roofs 2.5 m): mottled plaster grimed
    /// at the foot, layered thatch, big painted slates, coursed stone with mortar, planked timber with knots. Seamless (every
    /// noise is blended across the tile's edges). Generated once as PNGs like the rest of the art, and put on the existing
    /// materials (so every zone, old and new, picks them up on its next build).
    /// </summary>
    public static partial class ZoneSceneBuilder
    {
        const int T = 512;
        /// <summary>Noise that tiles: the four corner-shifted samples blended by position.</summary>
        static float Tiled(Func<float, float, float> p, float x, float y)
        {
            float u = x / T, v = y / T;
            return p(x, y) * (1 - u) * (1 - v) + p(x - T, y) * u * (1 - v) + p(x, y - T) * (1 - u) * v + p(x - T, y - T) * u * v;
        }
        static float Fbm(float x, float y, float f, int octaves = 3)
        {
            float v = 0, a = .5f, sum = 0;
            for (int o = 0; o < octaves; o++) { float ff = f; v += Tiled((px, py) => Perlin(px, py, ff), x, y) * a; sum += a; f *= 2.07f; a *= .55f; }
            return v / sum;
        }
        static float Hash(int a, int b) { uint h = (uint)(a * 374761393 + b * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
        static Color Mul(Color c, float v) { return new Color(Mathf.Clamp01(c.r * v), Mathf.Clamp01(c.g * v), Mathf.Clamp01(c.b * v), 1); }
        static float Smooth(float a, float b, float x) { x = Mathf.Clamp01((x - a) / (b - a)); return x * x * (3 - 2 * x); }

        /// <summary>Plaster: a warm near-white with soft mottling, faint diagonal brush strokes, speckle, and a grimed, greened foot.</summary>
        static Color PlasterPixel(int x, int y)
        {
            float mottle = Fbm(x, y, .011f) - .5f, fine = Fbm(x, y, .06f, 2) - .5f;
            float stroke = Mathf.Sin((x * .7f + y) * .09f + Fbm(x, y, .03f, 2) * 5) * .02f;
            float foot = 1 - .22f * Smooth(.3f, 0, y / (float)T) * (.7f + Fbm(x, y, .04f, 2) * .6f);   // y up: the bottom third darkens
            float v = .9f + mottle * .22f + fine * .07f + stroke; v *= foot;
            var c = new Color(v, v * (.985f - .03f * Mathf.Clamp01((.33f - y / (float)T) * 3)), v * .95f);   // a touch greener down low
            if (Hash(x / 3, y / 3) > .985f) c = Mul(c, .9f);   // speckle
            return c;
        }
        /// <summary>Thatch: layers 0.35 m deep (the roof tiles 2.5 m), each lit at the top and shadowed at its ragged bottom edge,
        /// striated with straw, a bundle lighter here and there.</summary>
        static Color ThatchPixel(int x, int y)
        {
            float rows = 7; float ry = (y / (float)T) * rows; int row = Mathf.FloorToInt(ry); float f = ry - row;   // f: 0 at the row's bottom
            float ragged = .06f + .05f * Mathf.Sin(x * .11f + row * 1.7f) + .04f * Fbm(x + row * 97, y, .05f, 2);
            float shade = Smooth(0, ragged + .1f, f);   // dark under the overlapping layer above
            float straw = Fbm(x * 1f, y * .18f, .25f, 2) - .5f;   // vertical striations
            float bundle = Fbm(x, y, .02f, 2) - .5f;
            float v = .62f + shade * .3f + straw * .22f + bundle * .16f;
            v *= 1 - .35f * Smooth(.08f, 0, f);   // the shadow line
            return new Color(v, v * .9f, v * .72f);
        }
        /// <summary>Slate: shingles 0.5 by 0.42 m in staggered rows, each its own value and a little of its own hue, a bright top
        /// edge and a dark joint, moss creeping on a few.</summary>
        static Color SlatePixel(int x, int y)
        {
            float rowH = T / 6f, colW = T / 5f; int row = Mathf.FloorToInt(y / rowH);
            float xs = x + (row % 2) * colW / 2; int col = Mathf.FloorToInt(xs / colW);
            float fy = y / rowH - row, fx = xs / colW - col;
            float h = Hash(col, row), v = .74f + (h - .5f) * .24f + (Fbm(x, y, .05f, 2) - .5f) * .12f;
            if (fy > .93f || fx < .03f || fx > .985f) v *= .55f;   // the joint
            else if (fy > .86f) v *= 1.18f;   // the lit top edge
            var c = h < .3f ? new Color(v * .92f, v * .95f, v) : h > .8f ? new Color(v, v * .98f, v * .94f) : new Color(v * .96f, v * .97f, v);
            float moss = Smooth(.6f, .85f, Fbm(x, y, .02f, 2)) * .5f;
            return Color.Lerp(c, new Color(v * .8f, v, v * .7f), moss);
        }
        /// <summary>Coursed stone: rows of blocks of uneven height and width, dark mortar, each block its own value and warmth,
        /// lit at the top-left, weathered by a slow noise.</summary>
        static Color StonePixel(int x, int y)
        {
            // Rows: five courses a tile, their heights jittered; blocks: two to four a row, offset per row.
            float rows = 5, ry = y / (float)T * rows; int row = Mathf.FloorToInt(ry); float fy = ry - row;
            float rowH = T / rows, jitter = (Hash(row, 7) - .5f) * .25f; fy = Mathf.Repeat(fy + jitter, 1);
            float cols = 2 + Mathf.Floor(Hash(row, 3) * 3); float cx = x / (float)T * cols + Hash(row, 11); int col = Mathf.FloorToInt(cx); float fx = cx - col;
            float colW = T / cols;
            float mortarY = 5 / rowH, mortarX = 5 / colW;
            float h = Hash(col + row * 31, row), v = .72f + (h - .5f) * .28f + (Fbm(x, y, .03f, 3) - .5f) * .2f;
            bool mortar = fy < mortarY || fx < mortarX;
            if (mortar) v = .36f + Fbm(x, y, .1f, 2) * .15f;
            else { if (fy > 1 - mortarY * 1.6f || fx < mortarX * 2.2f) v *= 1.14f; if (fy < mortarY * 2.4f || fx > 1 - mortarX * 1.6f) v *= .86f; }   // lit top and left, shadowed bottom and right
            float warm = (Hash(col * 7 + 1, row * 3 + 2) - .5f) * .08f;
            return new Color(v * (1 + warm), v, v * (1 - warm));
        }
        /// <summary>Timber: planks 0.25 m wide up the tile, wavy grain, a knot now and then, a dark seam between planks.</summary>
        static Color TimberPixel(int x, int y)
        {
            float plankW = T / 8f; int plank = Mathf.FloorToInt(x / plankW); float fx = x / plankW - plank;
            float wave = Fbm(x + plank * 53, y, .012f, 2) * 14;
            float grain = Mathf.Sin((x + wave) * .55f + plank) * .5f + .5f;
            float v = .64f + grain * .18f + (Hash(plank, 5) - .5f) * .16f + (Fbm(x, y, .08f, 2) - .5f) * .1f;
            // A knot: an ellipse of dark rings in some planks.
            float ky = Hash(plank, 9) * T, kx = (plank + .5f) * plankW; float d = Mathf.Sqrt(Mathf.Pow((x - kx) / 10, 2) + Mathf.Pow((y - ky) / 16, 2));
            if (Hash(plank, 13) > .5f && d < 1) v *= .62f + .3f * Mathf.Abs(Mathf.Sin(d * 9));
            if (fx < .05f || fx > .96f) v *= .66f;   // the seam
            return new Color(v, v * .9f, v * .78f);
        }

        /// <summary>Put the painted textures on the materials that build the villages (new PNGs; the materials keep their tints).</summary>
        static void PaintedTextures(ZoneArt art)
        {
            Retexture(art.plaster, Tex("plaster_painted", T, PlasterPixel), new Color(.86f, .8f, .68f), .06f);
            Retexture(art.thatch, Tex("thatch_painted", T, ThatchPixel), new Color(.78f, .64f, .36f), .02f);
            Retexture(art.slate, Tex("slate_painted", T, SlatePixel), new Color(.4f, .44f, .5f), .15f);
            // Masonry is its own material: art.stone stays the natural rock of crags, boulders and cave walls.
            if (art.masonry == null) { art.masonry = Standard("Masonry", new Color(.62f, .6f, .55f), Tex("stone_painted", T, StonePixel), .08f); EditorUtility.SetDirty(art); }
            Retexture(art.timber, Tex("timber_painted", T, TimberPixel), new Color(.44f, .3f, .19f), .08f);
        }
        static void Retexture(Material m, Texture2D tex, Color color, float smoothness)
        {
            if (m == null || (m.mainTexture == tex && m.color == color)) return;
            m.mainTexture = tex; m.color = color; m.SetFloat("_Glossiness", smoothness); EditorUtility.SetDirty(m);
        }
    }
}
