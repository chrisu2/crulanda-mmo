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
        ZoneBuilder zone; ParticleSystem ps; ParticleSystem.EmissionModule emission; float nextCheck; bool ash;
        public void Init(ZoneBuilder builder)
        {
            zone = builder; ash = zone.Zone.biome == "ash";
            var mat = ash ? zone.art.ashFlake : zone.art.leaf;
            if (mat == null) { enabled = false; return; }
            ps = new GameObject(ash ? "Falling ash" : "Falling leaves").AddComponent<ParticleSystem>();
            ps.transform.SetParent(transform, false);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 5; main.loop = true; main.startLifetime = ash ? 14 : 9;
            main.startSpeed = 0; main.startSize = ash ? new ParticleSystem.MinMaxCurve(.05f, .12f) : new ParticleSystem.MinMaxCurve(.12f, .22f);
            main.gravityModifier = ash ? .012f : .035f; main.maxParticles = 700; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var colors = new Gradient();
            if (ash) colors.SetKeys(new[] { new GradientColorKey(new Color(.55f, .54f, .53f), 0), new GradientColorKey(new Color(.35f, .33f, .32f), 1) }, new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(.9f, 1) });
            else colors.SetKeys(new[] { new GradientColorKey(new Color(.85f, .45f, .12f), 0), new GradientColorKey(new Color(.75f, .22f, .1f), .35f), new GradientColorKey(new Color(.85f, .7f, .2f), .7f), new GradientColorKey(new Color(.45f, .3f, .15f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(44, 1, 44);
            // Flutter: turbulence plus a slow tumble.
            var noise = ps.noise; noise.enabled = true; noise.strength = ash ? .25f : .55f; noise.frequency = .35f; noise.scrollSpeed = .2f; noise.separateAxes = false;
            var rot = ps.rotationOverLifetime; rot.enabled = !ash; rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f); rot.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); rot.z = new ParticleSystem.MinMaxCurve(-3, 3);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(.2f, .6f); vel.y = new ParticleSystem.MinMaxCurve(-.35f, -.15f); vel.z = new ParticleSystem.MinMaxCurve(-.1f, .25f);
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var alpha = new Gradient(); alpha.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .1f), new GradientAlphaKey(1, .8f), new GradientAlphaKey(0, 1) });
            fade.color = alpha;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard; r.alignment = ParticleSystemRenderSpace.Local;
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
            ps.transform.position = new Vector3(focus.x, zone.HeightAt(focus.x, focus.z) + 11, focus.z);
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 1;
            if (ash) { emission.rateOverTime = 18; return; }
            int near = 0;
            foreach (var t in zone.LeafTrees) if ((new Vector2(t.x - focus.x, t.z - focus.z)).sqrMagnitude < 26 * 26) near++;
            emission.rateOverTime = Mathf.Min(38, near * 1.3f);
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
            spray = Make("Splash spray", mat, false); rings = Make("Ripple rings", mat, true);
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
