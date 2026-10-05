using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Crulanda's twin moons, The Eye and The Tear (CANON: "The twin moons, The Eye and The Tear, hung low and pale", book1 ch.1;
    /// rising "clear and bright" in book3 ch.12). Chris, 2026-10-05: "be aware of day/night cycle and implement moons". The Eye,
    /// the larger, pale with a faint grey ring round a brighter middle like an iris, rises in the east-south-east at dusk, climbs
    /// to the south and sets in the west at dawn; its light is the night's (WorldClock follows it). The Tear, smaller, a cool
    /// silver-blue, follows an hour and a half behind it on a lower arc. Their looks are GAME-ONLY (the books name them only). Each
    /// is a painted disc (made here, once) on a quad far out in the sky, faced to the camera, fading in with the dark and out at
    /// dawn; the clouds pass over them (Resources/Shaders/Moon).
    /// </summary>
    public sealed class TwinMoons : MonoBehaviour
    {
        Transform eye, tear; Material eyeMat, tearMat;

        /// <summary>Where The Eye stands at this hour: its elevation and its compass bearing (degrees; below 0 it is down).</summary>
        public static void Eye(float hour, out float elevation, out float bearing) { Arc(hour, 18.6f, 54, 105, 255, out elevation, out bearing); }
        /// <summary>Where The Tear stands: an hour and a half behind The Eye, on a lower arc a little further south.</summary>
        public static void Tear(float hour, out float elevation, out float bearing) { Arc(hour, 20.1f, 38, 118, 248, out elevation, out bearing); }
        static void Arc(float hour, float rise, float top, float from, float to, out float elevation, out float bearing)
        {
            float p = Mathf.Repeat(hour - rise, 24) / 12f;   // 0 at its rising, 1 at its setting, twelve hours later
            elevation = p <= 1 ? Mathf.Sin(p * Mathf.PI) * top : -20; bearing = Mathf.Lerp(from, to, Mathf.Clamp01(p));
        }
        /// <summary>A direction in the world for an elevation and a bearing (0 north, 90 east).</summary>
        public static Vector3 Direction(float elevation, float bearing)
        {
            return Quaternion.Euler(-elevation, bearing, 0) * Vector3.forward;
        }

        public static TwinMoons Ensure(Transform parent)
        {
            var existing = parent.GetComponentInChildren<TwinMoons>(); if (existing != null) return existing;
            var shader = Resources.Load<Shader>("Shaders/Moon"); if (shader == null) return null;
            var go = new GameObject("Twin moons"); go.transform.SetParent(parent, false);
            var m = go.AddComponent<TwinMoons>();
            m.eyeMat = new Material(shader) { name = "The Eye", mainTexture = Disc(256, false) };
            m.tearMat = new Material(shader) { name = "The Tear", mainTexture = Disc(128, true) };
            m.eye = Quad(go.transform, "The Eye", m.eyeMat); m.tear = Quad(go.transform, "The Tear", m.tearMat);
            return m;
        }
        static Transform Quad(Transform parent, string name, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = name; q.transform.SetParent(parent, false);
            Object.Destroy(q.GetComponent<Collider>());
            var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return q.transform;
        }

        /// <summary>A moon's painted disc: limb-darkened, mottled with darker seas; The Eye with its grey iris ring, The Tear cooler and smoother.</summary>
        static Texture2D Disc(int size, bool tearMoon)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = tearMoon ? "The Tear disc" : "The Eye disc" };
            var px = new Color[size * size]; float seed = tearMoon ? 41.3f : 7.7f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size * 2 - 1, v = (y + .5f) / size * 2 - 1, r = Mathf.Sqrt(u * u + v * v);
                    // The disc, its edge a pixel soft, with a faint glow just outside it.
                    float disc = Mathf.Clamp01((.86f - r) * size * .25f), halo = Mathf.Clamp01(1 - (r - .86f) / .14f) * .18f * (r > .86f ? 1 : 0);
                    float limb = Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow(r / .86f, 2)));
                    float seas = Mathf.PerlinNoise(seed + u * 2.4f, seed + v * 2.4f) * .6f + Mathf.PerlinNoise(seed * 2 + u * 7, v * 7) * .4f;
                    float shade = .72f + .28f * limb - Mathf.Max(0, seas - .52f) * .55f;
                    Color c;
                    if (tearMoon) c = new Color(.78f, .86f, 1f) * shade;
                    else
                    {
                        // The iris: a soft grey ring about half way out, the middle a little brighter.
                        float ring = Mathf.Exp(-Mathf.Pow((r - .42f) / .07f, 2)) * .16f, pupil = Mathf.Exp(-Mathf.Pow(r / .2f, 2)) * .06f;
                        c = new Color(1f, .97f, .9f) * (shade - ring + pupil);
                    }
                    c.a = Mathf.Max(disc, halo);
                    if (disc <= 0) c = new Color(c.r, c.g, c.b, halo);
                    px[y * size + x] = c;
                }
            t.SetPixels(px); t.Apply(true, true); return t;
        }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null || eye == null) return;
            float dist = cam.farClipPlane * .8f, hour = WorldClock.Hour;
            // They show as the dark comes, and not at all deep in a cave.
            float night = Mathf.Clamp01(WorldClock.Darkness * 1.6f - .1f) * (1 - Hollow.CameraDepth);
            Place(eye, eyeMat, cam, dist, 4.6f, night, Eye);
            Place(tear, tearMat, cam, dist, 2.7f, night * .95f, Tear);
        }
        delegate void Where(float hour, out float elevation, out float bearing);
        static void Place(Transform q, Material m, Camera cam, float dist, float degrees, float show, Where where)
        {
            where(WorldClock.Hour, out float elev, out float bearing);
            float fade = show * Mathf.Clamp01((elev + 2) / 6f);   // sinking under the horizon
            q.gameObject.SetActive(fade > .01f);
            if (fade <= .01f) return;
            var dir = Direction(elev, bearing);
            q.position = cam.transform.position + dir * dist; q.rotation = Quaternion.LookRotation(dir, Vector3.up);
            q.localScale = Vector3.one * (2 * dist * Mathf.Tan(degrees * .5f * Mathf.Deg2Rad));
            m.color = new Color(1, 1, 1, fade);
        }
    }
}
