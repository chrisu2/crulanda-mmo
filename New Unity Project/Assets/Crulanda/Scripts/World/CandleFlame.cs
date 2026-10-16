using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A flame that wavers with its light. From the "Candle Shader" Chris downloaded (2026-10-15 01:20: "... candle flicker. use
    /// these where appropriate"): its graph flickered the emission and its script breathed the flame's height; here the shader
    /// (Crulanda/CandleFlame) does both, and this feeds it the lamp light's own waver (Flicker.Factor) each frame, so the flame
    /// and the pool of light it throws rise and fall together; a flame with no light wavers on the same curve by itself. Put on
    /// a flame part by <see cref="Attach"/>, which also swaps the part's Glowing material for its flame-shader copy
    /// (CrulandaShaders.Flame): the Glowing material itself is shared with other glowing parts, so it is never changed.
    /// </summary>
    public sealed class CandleFlame : MonoBehaviour
    {
        static readonly int FlickerId = Shader.PropertyToID("_Flicker");
        Flicker flicker; Renderer rend; MaterialPropertyBlock block; float seed;

        /// <summary>
        /// Makes <paramref name="flame"/> (a part on a Glowing material) a wavering flame burning with <paramref name="glow"/>, its lamp
        /// light (which gets a Flicker if it has none; null for a flame with no light of its own). Returns the flame.
        /// </summary>
        public static GameObject Attach(GameObject flame, Light glow = null)
        {
            if (flame == null) return null;
            var r = flame.GetComponent<Renderer>(); if (r == null || r.sharedMaterial == null) return flame;
            r.sharedMaterial = CrulandaShaders.Flame(r.sharedMaterial);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // a flame throws no shadow
            var f = flame.GetComponent<CandleFlame>(); if (f == null) f = flame.AddComponent<CandleFlame>();
            if (glow != null) { f.flicker = glow.GetComponent<Flicker>(); if (f.flicker == null) f.flicker = glow.gameObject.AddComponent<Flicker>(); }
            return flame;
        }
        void Start() { rend = GetComponent<Renderer>(); block = new MaterialPropertyBlock(); seed = Random.value * 100; }
        void LateUpdate()
        {
            if (rend == null) return;
            block.SetFloat(FlickerId, flicker != null ? flicker.Factor : Flicker.Curve(Time.time, seed));
            rend.SetPropertyBlock(block);
        }
    }
}
