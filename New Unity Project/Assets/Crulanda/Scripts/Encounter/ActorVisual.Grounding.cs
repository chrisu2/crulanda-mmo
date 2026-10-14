using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Feet on the real ground (Chris 12:56, "only foot issue i see so far is it fall below the ground", and 12:57, "and these are hovering
    /// above the ground"): after the clips have posed a modelled figure, the lower foot's sole is measured against the ground under it
    /// (terrain, path or floor, by a ray that skips the figure's own colliders) and the whole model is raised or lowered to meet it.
    /// The correction eases in, is limited to 20 cm, and holds still in the air, swimming, riding or dead. Moving, the clip's floor is
    /// put on the ground under the body and each foot follows the ground under it (GroundMoving).
    /// </summary>
    public sealed partial class ActorVisual
    {
        /// <summary>The toe bone's height over the sole of the shoe.</summary>
        public const float ToeRest = 0f, GroundFixMax = .2f;
        /// <summary>Per-foot planting (WIP 2026-10-12): off until it stops lifting the player 77 cm in GroundingTests.</summary>
        public static bool PlantFeet = true;
        Vector3 plantLast; readonly Quaternion[] plantRot = new Quaternion[2], plantThigh = new Quaternion[2]; readonly bool[] plantWrote = new bool[2]; float groundFix; bool groundBaseSet; float groundBaseY;
        /// <summary>The lower foot's sole over the ground under it this frame (m; negative is sunk), after the fix.</summary>
        public float SoleGap { get; private set; }
        public float GroundFix { get { return groundFix; } }

        /// <summary>The highest ground under a point (within a metre above it and two below), not counting this figure.</summary>
        public float GroundUnder(Vector3 p, float fallback)
        {
            var hits = Physics.RaycastAll(p + Vector3.up, Vector3.down, 3, ~0, QueryTriggerInteraction.Ignore); float best = float.MinValue;
            foreach (var h in hits) { if (h.collider.transform.IsChildOf(transform.root)) continue; if (h.point.y > best) best = h.point.y; }
            return best > float.MinValue ? best : fallback;
        }
        public float SoleGapOf(Transform ball)
        {
            if (ball == null) return 0; var p = ball.position;
            return p.y - ToeRest - GroundUnder(p, transform.position.y - 1);
        }
        Vector3 groundLast;
        void GroundFeet()
        {
            if (model == null || model.Model == null || model.BallL == null || model.BallR == null) return;
            if (!groundBaseSet) { groundBaseY = model.Model.localPosition.y; groundBaseSet = true; }
            bool dead = actorRef != null && !actorRef.IsAlive;
            // The ground under each ball (as SoleGapOf), kept for PlantFoot this frame: the model only moves up or down before it.
            float fallback = transform.position.y - 1;
            ballGround[0] = GroundUnder(model.BallL.position, fallback); ballGround[1] = GroundUnder(model.BallR.position, fallback); ankleGround[0] = ankleGround[1] = float.NaN;
            float gl = model.BallL.position.y - ToeRest - ballGround[0], gr = model.BallR.position.y - ToeRest - ballGround[1], gap = Mathf.Min(gl, gr);
            // Standing, both feet are down: the body sits to the higher sole so the leg that cannot reach (the idle's straight left
            // leg, 5 cm up in FootTests) lands, and PlantFoot bends the other knee to its ground. Moving: GroundMoving.
            var mv = transform.position - groundLast; mv.y = 0; groundLast = transform.position; float sp = Time.deltaTime > 0 ? mv.magnitude / Time.deltaTime : 0;
            bool moving = sp >= .3f;
            if (!moving && Mathf.Max(gl, gr) < .08f) gap = Mathf.Max(gl, gr);
            SoleGap = gap;
            if (!motorChecked) { motor = GetComponent<AdventurerMotor>(); motorChecked = true; }
            // In the air (the player's jump or fall, from take-off: chasing the lifting feet for the first frames pulled the model
            // 12 cm down, and it landed with its knees folded), in the water, in a saddle: leave it.
            if (dead || Riding || Pose == ActorPose.Swim || LyingLow || (motor != null && motor.Airborne) || gap > .25f || gap < -.3f) { terrainSet[0] = terrainSet[1] = false; return; }
            float scale = Mathf.Max(.01f, model.Model.lossyScale.y / Mathf.Max(.01f, Mathf.Abs(model.Model.localScale.y)));
            if (moving) { GroundMoving(scale); return; }
            movingRestSet = false; terrainSet[0] = terrainSet[1] = false;
            float want = Mathf.Clamp(groundFix - gap, -GroundFixMax, GroundFixMax);
            groundFix = Mathf.MoveTowards(groundFix, want, Mathf.Max(.02f, Mathf.Abs(want - groundFix)) * Mathf.Min(1, Time.deltaTime * 12));
            var lp = model.Model.localPosition; lp.y = groundBaseY + groundFix / scale; model.Model.localPosition = lp;
            SoleGap = gap + (want - groundFix) * 0;   // measured before this frame's move
            if (PlantFeet && Pose == ActorPose.None) { PlantFoot(model.UpperLegL, model.LowerLegL, model.FootL, model.BallL); PlantFoot(model.UpperLegR, model.LowerLegR, model.FootR, model.BallR); }
        }
        // ---------- moving ----------
        // FootTests' "running after a jump" (2026-10-14; the jump had nothing to do with it: the run after it is the stretch of Mill road
        // that goes down a 4-13% slope, the run before it goes up one). Moving, the model was raised or lowered every frame to put the
        // lower ball on the ground under it, and the feet within 8 cm of the ground were pinned to it. That chased the gait: in the run's
        // flight (both feet up, as Kevin made it) the model sank 4-5 cm, so the foot leaving the ground was dragged back to it already
        // turned past upright (read as "down but turned 178 degrees"), PlantFoot spun that foot 160 degrees round to the heading and
        // flattened it, and the landing foot came down a tenth of a cycle early with its toes still up, the heel 2-4 cm into the ground.
        // On a slope the model sat on the uphill foot, so the downhill foot reaching forward hung 5-6 cm higher than on the flat with
        // its toes still up for the heel strike ("lifted with its toes pulled up toward the shin").
        // Now, moving: the clip's floor sits on the ground under the body (its stance ball, the lowest ball over the last cycle, on
        // that ground), so the gait keeps its timing (flight and all), and each foot is carried by the rise or fall of the ground under
        // it against the ground under the body (a slope, a kerb, a stone): the clip's foot heights laid on the real ground. PlantFoot
        // then settles a foot that is down as before, but leaves alone a foot stood upright or turned back past upright.
        float movingRest; bool movingRestSet; readonly float[] terrainGround = new float[2], ballGround = new float[2], ankleGround = new float[2]; readonly bool[] terrainSet = new bool[2];
        /// <summary>How far a foot is carried for the ground under it, at most (m).</summary>
        const float TerrainMax = .2f;
        void GroundMoving(float scale)
        {
            // A culled Animator wrote nothing this frame (CullUpdateTransforms): what the bones hold is our own last frame. Hold.
            if (model.Animator != null && model.Animator.cullingMode != AnimatorCullingMode.AlwaysAnimate && !model.Visible) { terrainSet[0] = terrainSet[1] = false; return; }
            float floor = model.Model.position.y, dt = Time.deltaTime;
            // The clip's stance ball over its own floor: the lower ball's height, held at its lowest over the cycle (it rises back
            // 3 cm a second, so a flight of a tenth of a second moves it 3 mm).
            float low = Mathf.Min(model.BallL.position.y, model.BallR.position.y) - floor;
            movingRest = movingRestSet ? Mathf.Min(low, movingRest + dt * .03f) : low; movingRestSet = true;
            // The ground under the body, from half a metre over the floor (an awning or a branch overhead is not ground).
            var p = transform.position;
            float under = GroundUnder(new Vector3(p.x, floor - .45f, p.z), floor + movingRest - ToeRest);
            float want = Mathf.Clamp(groundFix + (under + ToeRest - movingRest - floor), -GroundFixMax, GroundFixMax);
            groundFix = Mathf.MoveTowards(groundFix, want, Mathf.Max(.02f, Mathf.Abs(want - groundFix)) * Mathf.Min(1, dt * 12));
            var lp = model.Model.localPosition; lp.y = groundBaseY + groundFix / scale; model.Model.localPosition = lp;
            if (!PlantFeet || Pose != ActorPose.None) { terrainSet[0] = terrainSet[1] = false; return; }
            TerrainFoot(0, model.UpperLegL, model.LowerLegL, model.FootL, model.BallL, under, dt);
            TerrainFoot(1, model.UpperLegR, model.LowerLegR, model.FootR, model.BallR, under, dt);
            PlantFoot(model.UpperLegL, model.LowerLegL, model.FootL, model.BallL); PlantFoot(model.UpperLegR, model.LowerLegR, model.FootR, model.BallR);
        }
        /// <summary>A moving foot carried up or down by the ground under its heel (under the ankle: the heel strikes first) against the
        /// ground under the body, the leg reached to it in its own knee's plane, the foot's own turn kept; PlantFoot then sets the ball of
        /// a foot that is down on the ground under it and lays the foot on the slope. The ground under the foot may change at most 3 m/s,
        /// so a stone's or a kerb's edge does not jerk a stepping foot while a slope passes as it is (an easing lagged a swinging foot
        /// 2-3 cm behind the slope); a foot that is down stands on one spot, so its ground holds still and it stays on it however the
        /// body moves on over the slope. On the flat the foot is left as the clip has it.</summary>
        void TerrainFoot(int i, Transform thigh, Transform calf, Transform foot, Transform ball, float under, float dt)
        {
            if (thigh == null || calf == null || foot == null || ball == null) return;
            float g = GroundUnder(foot.position, under); ankleGround[i] = g;
            terrainGround[i] = terrainSet[i] ? Mathf.MoveTowards(terrainGround[i], g, 3 * dt) : g;
            terrainSet[i] = true;
            float lift = Mathf.Clamp(terrainGround[i] - under, -TerrainMax, TerrainMax);
            if (Mathf.Abs(lift) < .002f) return;
            var knee = calf.position - Vector3.Lerp(thigh.position, foot.position, .5f);
            var keep = foot.rotation;
            Reach(thigh, calf, foot, foot.position + Vector3.up * lift, knee.sqrMagnitude > 1e-4f ? knee : transform.forward);
            foot.rotation = keep;
        }
        /// <summary>FootTests (bx, 13:27): the idle had the right foot turned 52 degrees and the left toes 7 cm up; the walk set a toe 4-9 cm
        /// into the ground. A foot that is down (its ball within 8 cm of the ground) is turned to point within 20 degrees of the body's
        /// heading, and its leg is reached so the ball rests on the ground, the knee bending forward.</summary>
        void PlantFoot(Transform thigh, Transform calf, Transform foot, Transform ball)
        {
            if (thigh == null || calf == null || foot == null || ball == null) return;
            // Only over a pose the clips wrote this frame: a culled Animator leaves the bones as we left them, and planting on top of
            // our own last frame piles up (the player lifted 77 cm in GroundingTests).
            int fi = foot == model.FootL ? 0 : 1;
            if (plantWrote[fi] && Quaternion.Angle(foot.localRotation, plantRot[fi]) < .01f && Quaternion.Angle(thigh.localRotation, plantThigh[fi]) < .01f) return;
            float g = ballGround[fi], gap = ball.position.y - ToeRest - g;
            if (gap > .08f || gap < -.15f) return;
            var fwd = transform.forward; fwd.y = 0; if (fwd.sqrMagnitude < 1e-4f) return; fwd.Normalize();
            var along = ball.position - foot.position; along.y = 0;
            // A foot stood upright on its toes or turned back past upright (leaving the ground: Kevin's walk and run do it at toe-off,
            // on his own model too) has no heading to put right: turning it to the heading spun it 160 degrees round for a frame or two
            // (2026-10-14). Left as the clip has it.
            { var d = ball.position - foot.position; float up = Mathf.Atan2(-d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
              if (up >= 70 || Vector3.Dot(along, fwd) < 0) return; }
            if (along.sqrMagnitude > 1e-5f)
            {
                float yaw = Vector3.SignedAngle(along, fwd, Vector3.up), lim = 18;
                if (Mathf.Abs(yaw) > lim) foot.rotation = Quaternion.AngleAxis(yaw - Mathf.Sign(yaw) * lim, Vector3.up) * foot.rotation;
            }
            // Down to (or up onto) the ground: the ankle moved by the ball's gap, the leg reached to it, the foot's turn kept.
            var still = transform.position - plantLast; still.y = 0; float speed = Time.deltaTime > 0 ? still.magnitude / Time.deltaTime : 0;
            float w = speed < .3f ? 1 : Mathf.InverseLerp(.08f, .03f, gap);   // standing: planted; moving: fading in as the foot comes down, so a lifting step is not pinned
            if (foot == model.FootR) plantLast = transform.position;
            // Laid on the slope, walking or running (2026-10-14): the clips are made on level ground, so going up a hill a foot set down
            // level hung its heel over the falling ground (2 cm up a 17% road; the old grounding hid it by sinking the whole figure, the
            // heel 2-4 cm into the slope). A moving foot that is down is pitched by the ground's rise from under its ankle to under its
            // ball (toes up going uphill, down going downhill; not over a kerb or a step, which is no slope). Standing feet are left level.
            float slope = 0, slopeW = w * Mathf.InverseLerp(.3f, 1, speed);
            { var d = ball.position - foot.position; float run = new Vector2(d.x, d.z).magnitude;
              if (run > .03f && slopeW > 0) { float rise = g - (float.IsNaN(ankleGround[fi]) ? GroundUnder(foot.position, g) : ankleGround[fi]); if (Mathf.Abs(rise) < run * .5f) slope = Mathf.Atan2(rise, run) * Mathf.Rad2Deg; }
              var axis = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z)).normalized;
              if (Mathf.Abs(slope) > .5f && axis.sqrMagnitude > .5f) foot.rotation = Quaternion.AngleAxis(-slope * slopeW, axis) * foot.rotation; }
            // Flat on the ground, not up on its toes (FootTests: tilt 40-67 against 25 standing): the foot pitched back toward a 25 degree ankle-to-ball slope.
            { var d = ball.position - foot.position; float tilt = Mathf.Atan2(-d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
              if (tilt > 32) { var axis = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z)).normalized; if (axis.sqrMagnitude > .5f) foot.rotation = Quaternion.AngleAxis(-(tilt - 32) * w, axis) * foot.rotation; } }
            gap = ball.position.y - ToeRest - g;
            if (w <= 0) { plantRot[fi] = foot.localRotation; plantThigh[fi] = thigh.localRotation; plantWrote[fi] = true; return; }
            var keep = foot.rotation; var ankle = foot.position + Vector3.down * (gap * w);
            Reach(thigh, calf, foot, ankle, transform.forward);
            foot.rotation = keep;
            plantRot[fi] = foot.localRotation; plantThigh[fi] = thigh.localRotation; plantWrote[fi] = true;
        }
    }
}