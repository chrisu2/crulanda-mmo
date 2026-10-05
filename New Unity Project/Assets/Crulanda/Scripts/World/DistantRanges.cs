using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Hills and mountains on the horizon (Chris, 2026-10-05: "horizon looks blank": past the backdrop's ridge there was only the
    /// pale fog). Two rings of ranges far out round the zone, the nearer lower and a little darker, the farther higher and paler,
    /// each a ragged silhouette from layered noise: rolling hills for the meadows, the gloom and the green shore, peaks for the
    /// mountains, low swells for the Ash Rim; none toward the Wasting, where the world stops. Drawn flat in the haze's colours
    /// (Resources/Shaders/Distant): every frame the base takes the fog's colour and the crest a mix of it and the zone's ridge
    /// tint, so they darken at dusk, vanish in thick weather and stay in key with the sky.
    /// </summary>
    public sealed class DistantRanges : MonoBehaviour
    {
        Material near, far; Color tint; float strength = 1;

        public static void Build(Transform parent, ZoneDefinition zone, float reach)
        {
            var shader = Resources.Load<Shader>("Shaders/Distant"); if (shader == null) return;
            var go = new GameObject("Distant ranges"); go.transform.SetParent(parent, false);
            var d = go.AddComponent<DistantRanges>();
            string biome = zone.biome ?? "";
            bool mountain = biome == "mountain", ash = biome == "ash";
            d.tint = mountain ? new Color(.42f, .47f, .58f) : ash ? new Color(.4f, .37f, .36f) : biome == "gloom" ? new Color(.3f, .32f, .36f)
                : biome == "verdant" ? new Color(.26f, .4f, .38f) : new Color(.34f, .44f, .52f);
            float scale = mountain ? 2.2f : ash ? .55f : 1;
            int seed = zone.seed % 997;
            // The Wasting's side (Oakhaven's east): no land there.
            float wastingYaw = zone.wasting != null ? 90 : float.NaN;
            d.near = d.Ring(go.transform, shader, reach * 2.45f, 110 * scale, 50 * scale, seed, wastingYaw, .05f);
            d.far = d.Ring(go.transform, shader, reach * 2.75f, 175 * scale, 80 * scale, seed + 41, wastingYaw, .028f);
            d.LateUpdate();
        }

        Material Ring(Transform parent, Shader shader, float radius, float tall, float vary, int seed, float skipYaw, float freq)
        {
            const int N = 360; var v = new Vector3[(N + 1) * 2]; var t = new int[N * 6];
            for (int i = 0; i <= N; i++)
            {
                float a = i * 360f / N, r = a * Mathf.Deg2Rad;
                // Layered noise round the ring: broad masses, then shoulders, then a ragged edge.
                float x = Mathf.Cos(r) * 4, z = Mathf.Sin(r) * 4;
                float h = Mathf.PerlinNoise(seed + x * radius * freq * .25f, z * radius * freq * .25f + 17) * .65f
                        + Mathf.PerlinNoise(seed + 50 + x * radius * freq, z * radius * freq + 9) * .28f
                        + Mathf.PerlinNoise(seed + 90 + x * radius * freq * 4, z * radius * freq * 4) * .07f;
                float top = tall + (h - .45f) * vary * 2;
                if (!float.IsNaN(skipYaw)) { float off = Mathf.Abs(Mathf.DeltaAngle(a, skipYaw)); top *= Mathf.SmoothStep(0, 1, (off - 25) / 30f); }
                var dir = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));
                v[i * 2] = dir * radius + Vector3.up * -60; v[i * 2 + 1] = dir * radius + Vector3.up * Mathf.Max(-10, top);
                if (i < N) { int b = i * 2, k = i * 6; t[k] = b; t[k + 1] = b + 1; t[k + 2] = b + 2; t[k + 3] = b + 1; t[k + 4] = b + 3; t[k + 5] = b + 2; }
            }
            var m = new Mesh { name = "Distant range" }; m.vertices = v; m.triangles = t; m.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            var go = new GameObject("Range"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            var mat = new Material(shader) { name = "Distant range" }; mat.SetFloat("_Base", -5); mat.SetFloat("_Crest", Mathf.Max(20, tall * .7f));
            mr.sharedMaterial = mat; return mat;
        }

        void LateUpdate()
        {
            var fog = RenderSettings.fogColor;
            strength = Mathf.Clamp01((RenderSettings.fogEndDistance - 60) / 220);   // thick weather swallows them
            // Light on the land: the skylight's brightness against a clear noon, so the crests darken with the evening.
            float light = Mathf.Clamp(RenderSettings.ambientSkyColor.grayscale / .55f, .15f, 1.1f);
            var crest = new Color(tint.r * light, tint.g * light, tint.b * light);
            Set(near, fog, Color.Lerp(fog, crest, .55f * strength));
            Set(far, fog, Color.Lerp(fog, crest, .32f * strength));
        }
        static void Set(Material m, Color haze, Color crest) { if (m == null) return; m.SetColor("_Haze", haze); m.SetColor("_Color", crest); }
    }
}
