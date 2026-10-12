using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Feet on the real ground (Chris 12:56, "only foot issue i see so far is it fall below the ground", and 12:57, "and these are hovering
    /// above the ground"): after the clips have posed a modelled figure, the lower foot's sole is measured against the ground under it
    /// (terrain, path or floor, by a ray that skips the figure's own colliders) and the whole model is raised or lowered to meet it.
    /// The correction eases in, is limited to 20 cm, and holds still in the air, swimming, riding or dead.
    /// </summary>
    public sealed partial class ActorVisual
    {
        /// <summary>The toe bone's height over the sole of the shoe.</summary>
        public const float ToeRest = 0f, GroundFixMax = .2f;
        /// <summary>Per-foot planting (WIP 2026-10-12): off until it stops lifting the player 77 cm in GroundingTests.</summary>
        public static bool PlantFeet = false;
        Vector3 plantLast; float groundFix; bool groundBaseSet; float groundBaseY;
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
        void GroundFeet()
        {
            if (model == null || model.Model == null || model.BallL == null || model.BallR == null) return;
            if (!groundBaseSet) { groundBaseY = model.Model.localPosition.y; groundBaseSet = true; }
            bool dead = actorRef != null && !actorRef.IsAlive;
            float gl = SoleGapOf(model.BallL), gr = SoleGapOf(model.BallR), gap = Mathf.Min(gl, gr);
            SoleGap = gap;
            if (dead || Riding || Pose == ActorPose.Swim || LyingLow || gap > .25f || gap < -.3f) return;   // in the air, in the water, in a saddle: leave it
            float scale = Mathf.Max(.01f, model.Model.lossyScale.y / Mathf.Max(.01f, Mathf.Abs(model.Model.localScale.y)));
            float want = Mathf.Clamp(groundFix - gap, -GroundFixMax, GroundFixMax);
            groundFix = Mathf.MoveTowards(groundFix, want, Mathf.Max(.02f, Mathf.Abs(want - groundFix)) * Mathf.Min(1, Time.deltaTime * 12));
            var lp = model.Model.localPosition; lp.y = groundBaseY + groundFix / scale; model.Model.localPosition = lp;
            SoleGap = gap + (want - groundFix) * 0;   // measured before this frame's move
            if (PlantFeet && Pose == ActorPose.None) { PlantFoot(model.UpperLegL, model.LowerLegL, model.FootL, model.BallL); PlantFoot(model.UpperLegR, model.LowerLegR, model.FootR, model.BallR); }
        }
        /// <summary>FootTests (bx, 13:27): the idle had the right foot turned 52 degrees and the left toes 7 cm up; the walk set a toe 4-9 cm
        /// into the ground. A foot that is down (its ball within 8 cm of the ground) is turned to point within 20 degrees of the body's
        /// heading, and its leg is reached so the ball rests on the ground, the knee bending forward.</summary>
        void PlantFoot(Transform thigh, Transform calf, Transform foot, Transform ball)
        {
            if (thigh == null || calf == null || foot == null || ball == null) return;
            float g = GroundUnder(ball.position, transform.position.y - 1), gap = ball.position.y - ToeRest - g;
            if (gap > .08f || gap < -.15f) return;
            var fwd = transform.forward; fwd.y = 0; if (fwd.sqrMagnitude < 1e-4f) return; fwd.Normalize();
            var along = ball.position - foot.position; along.y = 0;
            if (along.sqrMagnitude > 1e-5f)
            {
                float yaw = Vector3.SignedAngle(along, fwd, Vector3.up), lim = 18;
                if (Mathf.Abs(yaw) > lim) foot.rotation = Quaternion.AngleAxis(yaw - Mathf.Sign(yaw) * lim, Vector3.up) * foot.rotation;
            }
            // Down to (or up onto) the ground: the ankle moved by the ball's gap, the leg reached to it, the foot's turn kept.
            var still = transform.position - plantLast; still.y = 0; float speed = Time.deltaTime > 0 ? still.magnitude / Time.deltaTime : 0;
            float w = speed < .3f ? 1 : Mathf.InverseLerp(.08f, .03f, gap);   // standing: planted; moving: fading in as the foot comes down, so a lifting step is not pinned
            if (foot == model.FootR) plantLast = transform.position;
            // Flat on the ground, not up on its toes (FootTests: tilt 40-67 against 25 standing): the foot pitched back toward a 25 degree ankle-to-ball slope.
            { var d = ball.position - foot.position; float tilt = Mathf.Atan2(-d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
              if (tilt > 32) { var axis = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z)).normalized; if (axis.sqrMagnitude > .5f) foot.rotation = Quaternion.AngleAxis(-(tilt - 32) * w, axis) * foot.rotation; } }
            gap = ball.position.y - ToeRest - g;
            if (w <= 0) return;
            var keep = foot.rotation; var ankle = foot.position + Vector3.down * (gap * w);
            Reach(thigh, calf, foot, ankle, transform.forward);
            foot.rotation = keep;
        }
    }
}