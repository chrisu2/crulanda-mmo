using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Leaves drifting down around the player wherever broadleaf trees stand (more under denser woods). Ash zones get
    /// slow grey ash flakes everywhere instead. One particle system follows the camera's focus; emission is retuned
    /// once a second from the number of broadleaf trees nearby.
    /// </summary>
    public sealed class FallingLeaves : MonoBehaviour
    {
        ZoneBuilder zone; ParticleSystem ps; ParticleSystem.EmissionModule emission; float nextCheck; bool ash, filled;
        public void Init(ZoneBuilder builder)
        {
            zone = builder; ash = zone.Zone.biome == "ash";
            var mat = ash ? zone.art.ashFlake : zone.art.leaf;
            if (mat == null) { enabled = false; return; }
            ps = new GameObject(ash ? "Falling ash" : "Falling leaves").AddComponent<ParticleSystem>();
            ps.transform.SetParent(transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            // Ash: bigger, slower flakes drifting on the wind through the whole air column (small grey ones vanished in the haze).
            main.duration = 5; main.loop = true; main.startLifetime = ash ? new ParticleSystem.MinMaxCurve(10, 18) : new ParticleSystem.MinMaxCurve(9);
            main.startSpeed = 0; main.startSize = ash ? new ParticleSystem.MinMaxCurve(.1f, .22f) : new ParticleSystem.MinMaxCurve(.12f, .22f);
            main.gravityModifier = ash ? .003f : .035f; main.maxParticles = ash ? 1200 : 700; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation3D = !ash;   // ash faces the camera: flat quads tilted edge-on read as rain streaks
            main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var colors = new Gradient();
            // Ash: mostly pale flakes (they show against the ground and trees), a quarter charred flecks (against the sky and haze).
            if (ash) colors.SetKeys(new[] { new GradientColorKey(new Color(.92f, .91f, .9f), 0), new GradientColorKey(new Color(.78f, .77f, .77f), .6f), new GradientColorKey(new Color(.24f, .23f, .23f), .75f), new GradientColorKey(new Color(.2f, .19f, .19f), 1) }, new[] { new GradientAlphaKey(.95f, 0), new GradientAlphaKey(.95f, 1) });
            else if (zone.Zone.biome == "gloom") colors.SetKeys(new[] { new GradientColorKey(new Color(.44f, .38f, .29f), 0), new GradientColorKey(new Color(.34f, .3f, .26f), .5f), new GradientColorKey(new Color(.52f, .47f, .38f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });   // withered, not autumn
            else colors.SetKeys(new[] { new GradientColorKey(new Color(.85f, .45f, .12f), 0), new GradientColorKey(new Color(.75f, .22f, .1f), .35f), new GradientColorKey(new Color(.85f, .7f, .2f), .7f), new GradientColorKey(new Color(.45f, .3f, .15f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(44, ash ? 10 : 1, 44);   // ash is born through the air column, so it shows at once
            // Flutter: turbulence plus a slow tumble.
            var noise = ps.noise; noise.enabled = true; noise.strength = ash ? .4f : .55f; noise.frequency = .35f; noise.scrollSpeed = .2f; noise.separateAxes = false;
            var rot = ps.rotationOverLifetime; rot.enabled = !ash; rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f); rot.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); rot.z = new ParticleSystem.MinMaxCurve(-3, 3);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = ash ? new ParticleSystem.MinMaxCurve(.35f, 1f) : new ParticleSystem.MinMaxCurve(.2f, .6f); vel.y = ash ? new ParticleSystem.MinMaxCurve(-.5f, -.25f) : new ParticleSystem.MinMaxCurve(-.35f, -.15f); vel.z = new ParticleSystem.MinMaxCurve(-.1f, .25f);
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var alpha = new Gradient(); alpha.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, ash ? .03f : .1f), new GradientAlphaKey(1, .8f), new GradientAlphaKey(0, 1) });   // ash is born in view: a quick fade-in
            fade.color = alpha;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard; r.alignment = ash ? ParticleSystemRenderSpace.View : ParticleSystemRenderSpace.Local;
            if (ash) r.maxParticleSize = .011f;   // a flake passing the lens stays a small flake (~10 px), not a soft blot
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            emission = ps.emission; emission.rateOverTime = 0;
            ps.Play();
        }
        void Update()
        {
            if (ps == null) return;
            var cam = Camera.main; if (cam == null) return;
            // Centre on the ground ahead of the camera, a little above head height.
            var focus = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 12;
            var at = new Vector3(focus.x, zone.HeightAt(focus.x, focus.z) + (ash ? 6.5f : 11), focus.z);
            // Ash hangs everywhere: fill the air at once on arrival and after a teleport (the old flakes stay behind; turning
            // the camera moves the focus 24 m at most, so a turn never refills).
            if (ash && (!filled || (at - ps.transform.position).sqrMagnitude > 30 * 30)) { ps.transform.position = at; ps.Clear(); ps.Emit(700); filled = true; }
            ps.transform.position = at;
            // The weather's wind carries them downwind (an ash squall hard); gusts strip more leaves and whip up more ash.
            var weather = WorldWeather.Now; var wind = WorldWeather.WindDirection * (weather.wind * (ash ? 4.5f : 3f));
            var vel = ps.velocityOverLifetime;
            vel.x = ash ? new ParticleSystem.MinMaxCurve(.35f + wind.x, 1f + wind.x) : new ParticleSystem.MinMaxCurve(.2f + wind.x, .6f + wind.x);
            vel.z = new ParticleSystem.MinMaxCurve(-.1f + wind.y, .25f + wind.y);
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 1;
            var noise = ps.noise; noise.strength = (ash ? .4f : .55f) * (1 + weather.wind);
            if (ash) { emission.rateOverTime = 60 + 260 * weather.ash; return; }
            int near = 0;
            foreach (var t in zone.LeafTrees) if ((new Vector2(t.x - focus.x, t.z - focus.z)).sqrMagnitude < 26 * 26) near++;
            emission.rateOverTime = Mathf.Min(38, near * 1.3f) * (1 + 1.5f * weather.wind);
        }
    }

    /// <summary>
    /// Water spray: bursts at a point (entering water, swimming strokes, wading). One pooled particle system per zone.
    /// </summary>
    public sealed class Splashes : MonoBehaviour
    {
        public static Splashes Active { get; private set; }
        ParticleSystem spray, rings;
        public static void Ensure(ZoneBuilder zone)
        {
            if (Active != null || zone == null || zone.art.splash == null) return;
            Active = zone.gameObject.AddComponent<Splashes>(); Active.Build(zone.art.splash);
        }
        void Build(Material mat)
        {
            spray = Make("Splash spray", mat, false);
            // Rings need a ring sprite: with the spray's soft dot they drew as flat white discs on the water.
            rings = Make("Ripple rings", new Material(mat) { name = "Ripple ring", mainTexture = RingTexture() }, true);
        }
        ParticleSystem Make(string name, Material mat, bool ring)
        {
            var ps = new GameObject(name).AddComponent<ParticleSystem>(); ps.transform.SetParent(transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 400;
            main.startLifetime = ring ? 1.4f : .7f; main.startSpeed = ring ? 0 : new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = ring ? .6f : new ParticleSystem.MinMaxCurve(.12f, .3f); main.gravityModifier = ring ? 0 : 1.1f;
            main.startColor = new Color(.85f, .92f, .95f, ring ? .5f : .8f);
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 25; shape.radius = .3f; shape.rotation = new Vector3(-90, 0, 0);
            if (ring) { var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .3f, 1, 3.5f)); }
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (ring) { r.renderMode = ParticleSystemRenderMode.HorizontalBillboard; }
            return ps;
        }
        /// <summary>A thin bright ring on a transparent square (built once per zone at load).</summary>
        static Texture2D RingTexture()
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Ripple ring", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0, i = 0; y < n; y++)
                for (int x = 0; x < n; x++, i++)
                {
                    float r = new Vector2(x + .5f - n / 2f, y + .5f - n / 2f).magnitude / (n / 2f);
                    float a = Mathf.Clamp01(1 - Mathf.Abs(r - .8f) / .1f); px[i] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true); return t;
        }
        /// <summary>Just a ripple ring (standing or treading water), <paramref name="size"/> metres across to start.</summary>
        public void Ring(Vector3 surfacePoint, float size) { rings.Emit(new ParticleSystem.EmitParams { position = surfacePoint + Vector3.up * .03f, startSize = size }, 1); }
        /// <summary>A raindrop's ring: small, and gone in <paramref name="life"/> seconds (WorldWeather).</summary>
        public void Drop(Vector3 surfacePoint, float size, float life) { rings.Emit(new ParticleSystem.EmitParams { position = surfacePoint + Vector3.up * .03f, startSize = size, startLifetime = life }, 1); }
        /// <summary>A splash of <paramref name="drops"/> droplets and a ripple ring at a surface point.</summary>
        public void At(Vector3 surfacePoint, int drops)
        {
            var p = new ParticleSystem.EmitParams { position = surfacePoint, applyShapeToPosition = true };
            if (drops > 0) spray.Emit(p, drops);
            rings.Emit(new ParticleSystem.EmitParams { position = surfacePoint + Vector3.up * .03f }, 1);
        }
        void OnDestroy() { if (Active == this) Active = null; }
    }
}
