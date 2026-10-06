using UnityEngine;

namespace Crulanda.Encounter
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class AdventurerMotor : MonoBehaviour
    {
        public EncounterSession session;
        public Camera view;
        CharacterController controller;
        float yaw, pitch = 32, distance = 10, vertical;
        Vector2 previousPointer;
        bool orbiting;
        /// <summary>True while the player is walking or airborne this frame; used to interrupt cast-time abilities.</summary>
        public bool Moving { get; private set; }
        /// <summary>In a jump or a fall (not swimming).</summary>
        public bool Airborne { get; private set; }
        float groundedAt, airSince = -9, landedAt = -9;
        public float AirAge { get { return Time.time - airSince; } }
        public float LandedAge { get { return Time.time - landedAt; } }
        /// <summary>In water deeper than about chest height: floating at the surface, slower, no jumping.</summary>
        public bool Swimming { get; private set; }
        /// <summary>Holding Ctrl: half speed, and hidden hunters only notice you from much closer.</summary>
        public bool Sneaking { get; private set; }
        /// <summary>Running (toggled with "/"); walking otherwise. Kept for the session.</summary>
        public static bool Running;
        /// <summary>A walk and a run on foot, in m/s (the walk at the walk cycle's own pace, ModelFigure).</summary>
        public const float WalkSpeed = 1.9f, RunSpeed = 5.2f;
        float nextSplash, leftWater = -10; bool wasInWater;
        void Awake() { controller = GetComponent<CharacterController>(); }
        void Update()
        {
            Moving = false;
            if (session == null || view == null) return;
            var pointer = EncounterInput.Pointer;
            if (!session.Paused && EncounterInput.Orbit && !EncounterHud.BlocksPointer(pointer))
            {
                if (orbiting) { var delta = pointer - previousPointer; yaw += delta.x * .2f; pitch = Mathf.Clamp(pitch - delta.y * .15f, MinPitch, 75); }
                orbiting = true;
            }
            else orbiting = false;
            previousPointer = pointer;
            if (!session.Paused && !EncounterHud.BlocksPointer(pointer)) distance = Mathf.Clamp(distance - EncounterInput.Zoom * 1.2f, 2.5f, 22);   // close over the shoulder to a wide view
            if (!session.Player.IsAlive) { Swimming = Sneaking = false; return; }   // the dead neither swim nor sneak
            if (session.Paused || session.BuildOpen) return;
            // Walking by default; "/" (or the keypad's) toggles running (Chris, 2026-10-05: the default movement "is not a run
            // animation but walk, unless explicit run is toggled on").
            if (EncounterInput.Press(KeyCode.Slash)) { Running = !Running; session.Message(Running ? "Running." : "Walking."); }
            var move = Vector2.ClampMagnitude(EncounterInput.Move, 1);
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(move.x, 0, move.y);
            // Water (surface and depth come from the same model the water is drawn with).
            var here = transform.position; float surface = 0, depth = 0;
            var zone = Crulanda.World.ZoneBuilder.Active;
            bool wet = zone != null && zone.WaterAt(new Vector2(here.x, here.z), out surface, out depth);
            float feetUnder = wet ? surface - (here.y - 1) : 0;                     // how far the feet are below the surface
            // Hysteresis so the state doesn't flicker along a depth line: in at 0.12 m, out at 0.04 m.
            bool inWater = wet && feetUnder > (wasInWater ? .04f : .12f);
            // Swim where it is deeper than about chest height; enter at 1.45 m, leave below 1.25 m.
            Swimming = inWater && depth > (Swimming ? 1.25f : 1.45f);
            Sneaking = EncounterInput.Sneak && !Swimming;
            float wade = inWater ? Mathf.Lerp(1, .6f, Mathf.Clamp01(depth / 1.3f)) : 1;
            float speed = (Running ? RunSpeed : WalkSpeed) * session.Kit.MoveSpeedMultiplier * (Swimming ? .55f : wade) * (Sneaking ? .5f : 1);
            // Classic MMO movement (Chris, 2026-10-04): on foot you face where the camera looks and walk any way from there; backing
            // up is slower. Swimming still turns you into the stroke.
            if (!Swimming && move.y < 0) speed *= Mathf.Lerp(1, .55f, Mathf.Clamp01(-move.y / Mathf.Max(.01f, move.magnitude)));
            // Creek current: a gentle push downstream, strongest mid-channel and in deeper water (you can always wade across).
            var flow = inWater ? zone.FlowAt(new Vector2(here.x, here.z)) : Vector2.zero;
            var current = new Vector3(flow.x, 0, flow.y) * (Swimming ? 1.2f : .9f * Mathf.Clamp01(depth / .8f));
            if (Swimming)
            {
                // Float: a damped spring toward riding just under the surface (head and shoulders out). A fall carries
                // you under before you bob back up.
                float target = surface - .3f + Mathf.Sin(Time.time * 2.1f) * .04f;
                vertical += ((target - here.y) * 22 - vertical * 5.5f) * Time.deltaTime;
                vertical = Mathf.Max(vertical, -7);
                // Climbing out: pushing into a bank lifts you onto it; Space hops out where it isn't too deep.
                bool pushingBank = (controller.collisionFlags & CollisionFlags.Sides) != 0 && direction.sqrMagnitude > .01f;
                if (pushingBank) vertical = Mathf.Max(vertical, 3.2f);
                if (EncounterInput.Press(KeyCode.Space) && depth < 2.2f) vertical = 5.5f;
            }
            else if (controller.isGrounded) { vertical = -2; groundedAt = Time.time; if (EncounterInput.Press(KeyCode.Space)) { vertical = 6; airSince = Time.time; } }
            else vertical -= 20 * Time.deltaTime;
            // In the air (Round 22, note 50): a jump, or a fall longer than a step off a kerb. The figure plays the jump clips on it.
            bool air = !Swimming && !controller.isGrounded && (vertical > .5f || Time.time - groundedAt > .15f);
            if (air && !Airborne && vertical <= .5f) airSince = Time.time;
            if (!air && Airborne) landedAt = Time.time;
            Airborne = air;
            Moving = direction.sqrMagnitude > .01f || vertical > .5f;
            float fallSpeed = -vertical;
            controller.Move((direction * speed + current + Vector3.up * vertical) * Time.deltaTime);
            // Splashes: entering scales with how hard you hit the water (and waits 0.4 s after leaving it), then
            // strokes or wading ripples while moving.
            if (inWater && Crulanda.World.Splashes.Active != null)
            {
                var at = new Vector3(here.x, surface + .02f, here.z);
                if (!wasInWater && Time.time - leftWater > .4f)
                {
                    float impact = Mathf.Max(fallSpeed, direction.magnitude * speed);
                    Crulanda.World.Splashes.Active.At(at, Mathf.RoundToInt(Mathf.Lerp(3, 18, Mathf.InverseLerp(1, 8, impact))));
                }
                else if (Time.time >= nextSplash && direction.sqrMagnitude > .01f) { nextSplash = Time.time + (Swimming ? .45f : .32f); Crulanda.World.Splashes.Active.At(at + direction * .5f, Swimming ? 6 : 3); }
                else if (Time.time >= nextSplash) { nextSplash = Time.time + (Swimming ? .9f : 1.4f); Crulanda.World.Splashes.Active.Ring(at, Swimming ? .7f : .45f); }   // still: slow rings
            }
            if (wasInWater && !inWater) leftWater = Time.time;
            wasInWater = inWater;
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Swimming ? Quaternion.LookRotation(direction) : Quaternion.Euler(0, yaw, 0), Time.deltaTime * 12);
            var p = transform.position;
            if (p.y < Crulanda.World.Hollow.Lowest && !(wet && p.y > surface - 12)) Teleport(session.RecoveryPoint);   // fell out of the world (a cave's deep floor is not that)
        }
        /// <summary>How far below level the orbit goes (Chris, 2026-10-04: "camera should also have a up and down"): under the horizon the
        /// camera runs along the ground and tilts up at the sky, as in the classic MMOs. It was 12 degrees above level at the lowest.</summary>
        const float MinPitch = -40;
        void LateUpdate()
        {
            if (view == null) return;
            var pivot = transform.position + Vector3.up * .6f;
            var zoneHere = Crulanda.World.ZoneBuilder.Active; bool inCave = false;
            foreach (var h in Crulanda.World.Hollow.All) if (h.Depth(transform.position) > .1f) { inCave = true; break; }
            var groundCollider = zoneHere != null && zoneHere.GroundMesh != null && !inCave ? zoneHere.GroundMesh.GetComponent<Collider>() : null;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            // Camera collision: pull in toward the player when scenery is in the way. Not characters, and not trees: the
            // camera looks through a tree instead (it fades, below), so turning among trees doesn't yank the view in.
            float allowed = distance;
            foreach (var hit in Physics.SphereCastAll(pivot, .3f, -(rotation * Vector3.forward), distance, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider != groundCollider && hit.collider.GetComponentInParent<Crulanda.Gameplay.Actor>() == null && hit.collider.GetComponentInParent<Crulanda.World.TreeFade>() == null && hit.distance > .05f)
                    allowed = Mathf.Min(allowed, hit.distance);
            var camAt = pivot - rotation * Vector3.forward * Mathf.Max(1.2f, allowed);
            // Out of doors the ground does not pull the camera in: it rides along the grass (looking up past you), never under it.
            if (groundCollider != null) { float g = zoneHere.HeightAt(camAt.x, camAt.z) + .35f; if (camAt.y < g) camAt.y = g; }
            // Keep the camera above any water surface (no looking up at the water from underneath).
            var zone = Crulanda.World.ZoneBuilder.Active;
            if (zone != null && zone.WaterAt(new Vector2(camAt.x, camAt.z), out float s, out _) && camAt.y < s + .2f) camAt.y = s + .2f;
            view.transform.position = camAt;
            view.transform.rotation = rotation;
            // Trees between the camera and you turn see-through.
            Crulanda.World.TreeFade.UpdateAll(camAt, transform.position + Vector3.up * .7f, transform.position);
            Crulanda.World.RoofFade.UpdateAll(camAt, transform.position + Vector3.up * 1.5f, transform.position + Vector3.up * .7f);
        }
        /// <summary>Orbit camera framing (capture tools and cutscene-style framing).</summary>
        public void SetView(float yawDegrees, float pitchDegrees, float zoom) { yaw = yawDegrees; pitch = Mathf.Clamp(pitchDegrees, MinPitch, 85); distance = Mathf.Clamp(zoom, 2, 60); }
        public void Teleport(Vector3 point)
        {
            controller.enabled = false; transform.position = point; controller.enabled = true; vertical = 0;
            // Arriving in water (spawn, load, travel) is not "entering" it: no splash.
            var zone = Crulanda.World.ZoneBuilder.Active;
            wasInWater = zone != null && zone.WaterAt(new Vector2(point.x, point.z), out float s, out _) && point.y - 1 < s;
        }
        void Start() { Teleport(transform.position); }
    }
}

