using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// An epic piece's glowing accents breathe: their emission swells and fades at 0.6 Hz. Done with a property block on each
    /// renderer, so the shared gear material is never changed and nothing needs cleaning up when the part goes.
    /// </summary>
    public sealed class GearGlow : MonoBehaviour
    {
        public const float Hertz = .6f;
        Renderer[] parts; Color emission; MaterialPropertyBlock block; float offset;
        public void Init(Renderer[] renderers, Color baseEmission)
        {
            parts = renderers; emission = baseEmission; block = new MaterialPropertyBlock(); offset = Random.value * 6.28f;
        }
        void Update()
        {
            if (parts == null) return;
            float k = .7f + .3f * Mathf.Sin(Time.time * Hertz * 2 * Mathf.PI + offset);
            block.SetColor("_EmissionColor", emission * k);
            foreach (var r in parts) if (r != null) r.SetPropertyBlock(block);
        }
    }
}
