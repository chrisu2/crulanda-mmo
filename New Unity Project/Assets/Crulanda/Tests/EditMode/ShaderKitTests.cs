using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>The shaders rewritten from the packs Chris downloaded (2026-10-15: water, grass, candle flicker) and their hooks.</summary>
    public class ShaderKitTests
    {
        /// <summary>The flame shader compiled (a shader with an error is unsupported, and CrulandaShaders then leaves the material alone).</summary>
        [Test] public void Candle_flame_shader_compiles()
        {
            Assert.NotNull(Shader.Find(CrulandaShaders.FlameName), "Crulanda/CandleFlame is missing from Resources/Shaders");
            Assert.NotNull(CrulandaShaders.Find(CrulandaShaders.FlameName), "Crulanda/CandleFlame did not compile (see the console)");
        }
        /// <summary>A Glowing material's tint and glow carry over to its flame copy; the source is untouched; an unknown shader changes nothing.</summary>
        [Test] public void Flame_copy_keeps_the_glow()
        {
            var glowing = new Material(Shader.Find("Standard")) { color = new Color(1, .74f, .32f), name = "Glow" };
            glowing.SetColor("_EmissionColor", new Color(2.6f, 1.9f, .8f));
            var flame = CrulandaShaders.Flame(glowing);
            Assert.AreSame(flame, CrulandaShaders.Flame(glowing), "one copy per source");
            Assert.AreEqual("Standard", glowing.shader.name, "the shared Glowing material keeps its shader");
            if (flame != glowing)
            {
                Assert.AreEqual(CrulandaShaders.FlameName, flame.shader.name);
                Assert.AreEqual(glowing.color, flame.color); Assert.AreEqual(glowing.GetColor("_EmissionColor"), flame.GetColor("_EmissionColor"));
            }
            Assert.AreSame(glowing, CrulandaShaders.With(glowing, "Crulanda/NoSuchShader"));
            Object.DestroyImmediate(glowing); if (flame != glowing) Object.DestroyImmediate(flame);
        }
        /// <summary>The light's waver stays a waver (about 0.85 to 1.15, never still), so a flame on the same curve is never out or blinding.</summary>
        [Test] public void Flicker_curve_wavers_within_bounds()
        {
            float lo = 2, hi = 0;
            for (int i = 0; i < 2000; i++) { float f = Flicker.Curve(i * .01f, 37.2f); lo = Mathf.Min(lo, f); hi = Mathf.Max(hi, f); }
            Assert.Greater(lo, .8f); Assert.Less(hi, 1.2f); Assert.Greater(hi - lo, .05f, "it moves");
        }
        /// <summary>The grass bends for the player wherever they are, then for the seven nearest others, and never for more than eight.</summary>
        [Test] public void Grass_bends_for_the_player_and_the_nearest_seven()
        {
            var at = new List<Vector3> { new Vector3(100, 0, 100) };   // the player, far from the camera
            for (int i = 1; i <= 12; i++) at.Add(new Vector3(i, 0, 0));   // walkers at 1..12 m
            var chosen = new int[GrassBend.Slots];
            int n = GrassBend.Choose(at, Vector3.zero, 1, chosen);
            Assert.AreEqual(8, n);
            Assert.AreEqual(0, chosen[0], "the player first");
            for (int i = 1; i < n; i++) Assert.AreEqual(i, chosen[i], "the nearest next");
            Assert.AreEqual(3, GrassBend.Choose(at.GetRange(0, 3), Vector3.zero, 1, chosen), "fewer than the slots: all of them");
        }
    }
}
