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
        float groundFix; bool groundBaseSet; float groundBaseY;
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
        }
    }
}