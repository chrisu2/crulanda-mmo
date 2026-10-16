using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The hand-written shaders that stand in for the Shader Graph packs Chris downloaded (2026-10-15 01:20: "i have downloaded
    /// some shaders: stylized water, interactive grass, and candle flicker. use these where appropriate"; the project is on the
    /// built-in pipeline, so each graph was read and rewritten by hand). Found by name once, and used only when Unity compiled
    /// them: a shader with an error is unsupported, and the material keeps the look it had, so a slip in a shader never turns a
    /// flame pink. The water and the grass needed no new shader: Crulanda/Water and Crulanda/Grass already did what those packs
    /// did, and took the one thing each pack added in place (caustics on the bed; blades parting round feet, see GrassBend).
    /// </summary>
    public static class CrulandaShaders
    {
        public const string FlameName = "Crulanda/CandleFlame";
        static readonly Dictionary<string, Shader> found = new Dictionary<string, Shader>();
        static readonly Dictionary<(Material, string), Material> copies = new Dictionary<(Material, string), Material>();

        /// <summary>The named shader, or null when it is missing or failed to compile.</summary>
        public static Shader Find(string name)
        {
            if (found.TryGetValue(name, out var s) && s != null) return s;
            s = Shader.Find(name); if (s != null && !s.isSupported) s = null;
            found[name] = s; return s;
        }
        /// <summary>
        /// A copy of <paramref name="source"/> drawn with the named shader (properties of the same name keep their values: a Glowing
        /// material's tint and glow carry over), one copy per source; <paramref name="source"/> itself when the shader is missing.
        /// </summary>
        public static Material With(Material source, string shaderName)
        {
            if (source == null) return null;
            var shader = Find(shaderName); if (shader == null || source.shader == shader) return source;
            if (!copies.TryGetValue((source, shaderName), out var m) || m == null)
            {
                m = new Material(source) { name = source.name + " (" + shaderName.Substring(shaderName.LastIndexOf('/') + 1) + ")" };
                m.shader = shader; copies[(source, shaderName)] = m;
            }
            return m;
        }
        /// <summary>The flame shader on a Glowing material (CandleFlame.Attach puts it on a flame part).</summary>
        public static Material Flame(Material source) { return With(source, FlameName); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { found.Clear(); copies.Clear(); }
    }
}
