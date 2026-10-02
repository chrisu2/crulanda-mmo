using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// An epic piece's glowing accents breathe: their emission swells and fades at 0.6 Hz, from <see cref="Low"/> of its full
    /// strength up to all of it, so at its dimmest an epic accent still burns as bright as a rare one. Done with a property block on
    /// each renderer, so the shared gear material is never changed and nothing needs cleaning up when the part goes.
    /// </summary>
    public sealed class GearGlow : MonoBehaviour
    {
        public const float Hertz = .6f, Low = .6f;
        /// <summary>The accents this pulses.</summary>
        public int Accents { get { return parts == null ? 0 : parts.Length; } }
        Renderer[] parts; Color emission; MaterialPropertyBlock block; float offset;
        public void Init(Renderer[] renderers, Color baseEmission)
        {
            parts = renderers; emission = baseEmission; block = new MaterialPropertyBlock(); offset = Random.value * 6.28f;
        }
        void Update()
        {
            if (parts == null) return;
            float k = Low + (1 - Low) * (.5f + .5f * Mathf.Sin(Time.time * Hertz * 2 * Mathf.PI + offset));
            block.SetColor("_EmissionColor", emission * k);
            foreach (var r in parts) if (r != null) r.SetPropertyBlock(block);
        }
    }
}
