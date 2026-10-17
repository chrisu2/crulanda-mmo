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
        Material wood, near, far; Color tint, woodTint; float strength = 1;

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
            // A far place on the skyline (DistantSkyline: Oakhaven's view of the Crown up the North road): the forest ring and the near
            // range dip toward it, a gap in the hills it stands in (2026-10-16). The far range stands behind it whole.
            if (zone.skyline != null && zone.skyline.Length > 0 && zone.skyline[0] != null) d.notchYaw = zone.skyline[0].bearing;
            // Wooded hills rising behind the backdrop's ridge, their crests a run of tree crowns (2026-10-10, Chris: "horizon still looks
            // empty", against Elwynn's), and the hills and mountains past them nearer and taller than they were: a forest ring at 1.5
            // reaches, tall enough to stand over the ridge and its trees (some 45 m at 350 m, seen from the zone), none on the Ash Rim.
            d.woodTint = mountain ? new Color(.17f, .27f, .27f) : biome == "gloom" ? new Color(.22f, .24f, .25f) : biome == "verdant" ? new Color(.13f, .3f, .2f) : new Color(.17f, .3f, .19f);
            // A walled city (the Lowtowns, 2026-10-16): no wood ring. Its crowns' notches read as trees hanging upside down from the
            // smog over the roofs; in their place the city itself runs on, roofs, gables and stacks, climbing toward the Crown.
            if (biome == "lowtowns") { d.woodTint = new Color(.2f, .18f, .18f); d.wood = d.CityRing(go.transform, shader, reach * 1.35f, 70, seed + 77, CrownSkyline.Bearing); }
            else if (!ash) d.wood = d.Ring(go.transform, shader, reach * 1.5f, mountain ? 95 : 82, 26, seed + 77, wastingYaw, .07f, 4320, 9, .3f);
            d.near = d.Ring(go.transform, shader, reach * (mountain ? 2.45f : 2.0f), mountain ? 110 * scale : ash ? 110 * scale : 150, mountain || ash ? 50 * scale : 60, seed, wastingYaw, .05f, 360, 0, .55f);
            d.far = d.Ring(go.transform, shader, reach * 2.75f, 175 * scale, 80 * scale, seed + 41, wastingYaw, .028f);
            d.LateUpdate();
        }

        /// <summary>A ring of <paramref name="N"/> columns; <paramref name="crowns"/> above 0 runs a row of rounded tree crowns that tall
        /// (about two every three degrees, each its own size, smaller ones between) along the crest: a wood's skyline, not a saw's.</summary>
        /// <summary>The bearing of a far place on the skyline the rings dip for (NaN: none); see <see cref="DistantSkyline"/>.</summary>
        float notchYaw = float.NaN;
        Material Ring(Transform parent, Shader shader, float radius, float tall, float vary, int seed, float skipYaw, float freq, int N = 360, float crowns = 0, float notch = 1)
        {
            var v = new Vector3[(N + 1) * 2]; var t = new int[N * 6];
            for (int i = 0; i <= N; i++)
            {
                float a = i * 360f / N, r = a * Mathf.Deg2Rad;
                // Layered noise round the ring: broad masses, then shoulders, then a ragged edge.
                float x = Mathf.Cos(r) * 4, z = Mathf.Sin(r) * 4;
                float h = Mathf.PerlinNoise(seed + x * radius * freq * .25f, z * radius * freq * .25f + 17) * .65f
                        + Mathf.PerlinNoise(seed + 50 + x * radius * freq, z * radius * freq + 9) * .28f
                        + Mathf.PerlinNoise(seed + 90 + x * radius * freq * 4, z * radius * freq * 4) * .07f;
                float top = tall + (h - .45f) * vary * 2;
                if (crowns > 0)
                {
                    float f = a * .7f + Mathf.PerlinNoise(seed + 130 + x * 3, z * 3) * 3, size = .55f + Mathf.PerlinNoise(seed + 170 + x * 9, z * 9);
                    float g = a * 1.9f + Mathf.PerlinNoise(seed + 150 + x * 5, z * 5) * 4, small = .4f + Mathf.PerlinNoise(seed + 190 + x * 11, z * 11);
                    top += crowns * (size * (Mathf.Pow(Mathf.Abs(Mathf.Sin(f * Mathf.PI)), .3f) - .65f) + .35f * small * (Mathf.Pow(Mathf.Abs(Mathf.Sin(g * Mathf.PI)), .4f) - .6f));
                }
                if (!float.IsNaN(skipYaw)) { float off = Mathf.Abs(Mathf.DeltaAngle(a, skipYaw)); top *= Mathf.SmoothStep(0, 1, (off - 25) / 30f); }
                if (!float.IsNaN(notchYaw) && notch < 1) { float off = Mathf.Abs(Mathf.DeltaAngle(a, notchYaw)); top *= Mathf.Lerp(notch, 1, Mathf.SmoothStep(0, 1, (off - 14) / 22f)); }
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

        /// <summary>
        /// A city's skyline as a ring (the Lowtowns): a run of blocks a few degrees wide, each its own height, with a gable over each
        /// and a chimney stack on one in two, the blocks taller toward <paramref name="crownYaw"/> (the High City's hill). Built like
        /// <see cref="Ring"/>: one column per step round the ring from 60 m under the ground to the roofline, so the steps between
        /// blocks stand upright. Draws only from its own stream.
        /// </summary>
        Material CityRing(Transform parent, Shader shader, float radius, float tall, int seed, float crownYaw)
        {
            const int N = 4320; var r = new System.Random(seed); float R() { return (float)r.NextDouble(); }
            var tops = new float[N + 1]; int i0 = 0;
            while (i0 < N)
            {
                // A block: 1.5 to 3.5 degrees wide (12 to 27 m out here), its eaves, its gable's rise and maybe a stack.
                int w = Mathf.Min(N - i0, 18 + (int)(R() * 24)); float a = (i0 + w / 2f) * 360f / N;
                float climb = Mathf.SmoothStep(0, 1, 1 - Mathf.Abs(Mathf.DeltaAngle(a, crownYaw)) / 45f) * 34;
                float eaves = tall + climb + (R() - .5f) * 18, rise = 3 + R() * 4; int stack = R() < .5f ? (int)(w * (.2f + R() * .6f)) : -1;
                for (int k = 0; k < w; k++)
                {
                    float u = (k + .5f) / w; float top = eaves + rise * (1 - Mathf.Abs(u * 2 - 1));
                    if (stack >= 0 && Mathf.Abs(k - stack) <= 1) top = Mathf.Max(top, eaves + rise + 3 + (stack % 3));
                    tops[i0 + k] = top;
                }
                i0 += w;
            }
            tops[N] = tops[0];
            var v = new Vector3[(N + 1) * 2]; var t = new int[N * 6];
            for (int i = 0; i <= N; i++)
            {
                float rad = i * 360f / N * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
                v[i * 2] = dir * radius + Vector3.up * -60; v[i * 2 + 1] = dir * radius + Vector3.up * tops[i];
                if (i < N) { int b = i * 2, k = i * 6; t[k] = b; t[k + 1] = b + 1; t[k + 2] = b + 2; t[k + 3] = b + 1; t[k + 4] = b + 3; t[k + 5] = b + 2; }
            }
            var m = new Mesh { name = "Distant city" }; m.vertices = v; m.triangles = t; m.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            var go = new GameObject("City"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            var mat = new Material(shader) { name = "Distant city" }; mat.SetFloat("_Base", -5); mat.SetFloat("_Crest", tall * .8f);
            mr.sharedMaterial = mat; return mat;
        }

        void LateUpdate()
        {
            var fog = RenderSettings.fogColor;
            strength = Mathf.Clamp01((RenderSettings.fogEndDistance - 60) / 220);   // thick weather swallows them
            // Light on the land: the skylight's brightness against a clear noon, so the crests darken with the evening.
            float light = Mathf.Clamp(RenderSettings.ambientSkyColor.grayscale / .55f, .15f, 1.1f);
            var crest = new Color(tint.r * light, tint.g * light, tint.b * light);
            // Paler the farther: the wood past the ridge is fainter than the ridge's own trees (some 40% their colour at 350 m), the
            // ranges fainter again (2026-10-10: a darker far band read as a forest floating over the mist).
            Set(wood, fog, Color.Lerp(fog, new Color(woodTint.r * light, woodTint.g * light, woodTint.b * light), .45f * strength));
            Set(near, fog, Color.Lerp(fog, crest, .36f * strength));
            Set(far, fog, Color.Lerp(fog, crest, .22f * strength));
        }
        static void Set(Material m, Color haze, Color crest) { if (m == null) return; m.SetColor("_Haze", haze); m.SetColor("_Color", crest); }
    }
}
