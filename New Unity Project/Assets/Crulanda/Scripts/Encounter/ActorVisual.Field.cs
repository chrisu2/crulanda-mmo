using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Field work on a modelled figure (playtest note 150, Chris 2026-10-12: he needs to see farmers digging and ploughing). A farmer
    /// at work with a long tool hoes: a repeated two-handed stroke (raise, strike the soil, pull back, a pause to step along the row),
    /// both hands closed on the shaft (two-bone reach for each arm, then the shaft laid through both fists), the head into the soil at
    /// the strike with a puff of earth. There were no two-handed downward strikes among the game's clips (Kevin Iglesias's sets here
    /// are locomotion, idles, a sword, a bow and spells), so the stroke is posed here. A ploughman's hands are put on the handles the
    /// same way (<see cref="HandsOn"/>, PlowTeam).
    /// </summary>
    public sealed partial class ActorVisual
    {
        /// <summary>One hoe stroke, in seconds.</summary>
        public const float StrokePeriod = 1.8f;
        /// <summary>Where in the stroke the head meets the soil (0-1 of the period).</summary>
        const float StrikeAt = .47f;
        float hoeTime; bool hoeActive; Vector3 hoeButt, hoeDir; float tradeToolHeadReach = .05f;
        /// <summary>Strokes struck so far (the farmer steps along the row after each: Villager).</summary>
        public int StrokeCount { get; private set; }
        /// <summary>The tool head's lowest point over the ground at the last strike, in metres (negative: into the soil).</summary>
        public float StrikeGap { get; private set; } = float.NaN;
        /// <summary>Hoeing this frame (a modelled farmer at work with a long tool, standing).</summary>
        public bool Hoeing { get { return hoeActive; } }
        /// <summary>Fist to fist along the hoe shaft this frame, in metres (45-60 cm wanted).</summary>
        public float HandSpread { get; private set; }
        /// <summary>The hoer's pose this frame (note 164): the higher fist over the lower shoulder, the nearer fist ahead of the chest, the higher elbow over its shoulder (m).</summary>
        public float PoseHandOverShoulder { get; private set; }
        public float PoseHandAhead { get; private set; }
        public float PoseElbowOverShoulder { get; private set; }
        public const float HoeHandsMin = .47f, HoeHandsMax = .58f, HoeRearBack = .22f, HoeTopBelowChest = 0f, HoeTopAhead = .3f;
        Vector3? handsL, handsR; int handsFrame = -1;
        /// <summary>How far the farther hand ended from its mark (<see cref="HandsOn"/>), as last posed, in metres.</summary>
        public float HandsGap { get; private set; } = float.NaN;
        ParticleSystem puff;
        bool HoeReady { get { return model != null && tradeTool != null && tradeToolLong && model.UpperArmL != null && model.HandL != null && model.HandR != null; } }
        /// <summary>For a ploughman: puts the hands on these points this frame (call before LateUpdate, each frame).</summary>
        public void HandsOn(Vector3 left, Vector3 right) { handsL = left; handsR = right; handsFrame = Time.frameCount; }

        // The draw hoe's stroke (Docs/HOE_TECHNIQUE.md; Chris 09:47: "looks like they are just banging into onto the ground"): reach
        // forward and lift a little, set the blade in at an angle a little past the mark, draw it back toward the feet just under the
        // surface (the soil going with it), then recover. Keys in the body's frame: the rear (right) hand and the blade's edge, as
        // (ahead, height over the ground).
        static readonly Vector2 RecoverHand = new Vector2(.18f, 1.08f), LiftHand = new Vector2(.3f, 1.15f), EnterHand = new Vector2(.26f, 1.1f), DrawnHand = new Vector2(.06f, 1.04f);
        static readonly Vector2 RecoverEdge = new Vector2(.62f, .12f), LiftEdge = new Vector2(1.12f, .36f), EnterEdge = new Vector2(1.05f, -.03f), DrawnEdge = new Vector2(.5f, -.03f);
        /// <summary>The stroke at <paramref name="k"/> (0-1): the rear hand and the blade's edge (ahead, height), and whether the blade is in the soil.</summary>
        static void StrokeAt(float k, out Vector2 hand, out Vector2 edge, out float draw)
        {
            if (k < .35f) { float u = Mathf.SmoothStep(0, 1, k / .35f); hand = Vector2.Lerp(RecoverHand, LiftHand, u); edge = Vector2.Lerp(RecoverEdge, LiftEdge, u); draw = 0; }
            else if (k < StrikeAt) { float u = (k - .35f) / (StrikeAt - .35f); u *= u; hand = Vector2.Lerp(LiftHand, EnterHand, u); edge = Vector2.Lerp(LiftEdge, EnterEdge, u); draw = 0; }   // set in, quickening
            else if (k < .8f) { float u = Mathf.SmoothStep(0, 1, (k - StrikeAt) / (.8f - StrikeAt)); hand = Vector2.Lerp(EnterHand, DrawnHand, u); edge = Vector2.Lerp(EnterEdge, DrawnEdge, u); draw = 1; }   // drawn back to the feet
            else { float u = Mathf.SmoothStep(0, 1, (k - .8f) / .2f); hand = Vector2.Lerp(DrawnHand, RecoverHand, u); edge = Vector2.Lerp(DrawnEdge, RecoverEdge, u); draw = 0; }   // out of the soil; the step
        }
        /// <summary>Where in the stroke the hoe is (0-1; set in at <see cref="StrikeAt"/>), for captures.</summary>
        public float StrokePhase { get { return HoeK; } }
        float HoeK { get { return Mathf.Repeat(hoeTime / StrokePeriod, 1); } }
        /// <summary>The back's slight bend for the stroke (a draw hoe is worked nearly upright), a little more as it draws.</summary>
        float HoeBend() { StrokeAt(HoeK, out _, out _, out float draw); return 22 + draw * 8; }   // bent a little more over the shaft, so the front arm reaches 45-60 cm down it with relaxed shoulders (note 168)
        /// <summary>The blade's edge this frame, and whether it is in the soil (the draw), for tests.</summary>
        public Vector3 BladeEdge { get; private set; }
        public bool BladeInSoil { get; private set; }
        Transform clod;

        float Ground()
        {
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) return transform.position.y - agent.baseOffset;
            if (model.BallL != null && model.BallR != null) return Mathf.Min(model.BallL.position.y, model.BallR.position.y) - .03f;
            return transform.position.y - 1;
        }

        /// <summary>After the back is bent: the hoe stroke (both arms), or the hands on a ploughman's handles.</summary>
        void FieldArms(float dt, bool hoe)
        {
            hoeActive = false; BladeInSoil = false;
            if (handsFrame >= Time.frameCount - 1 && handsL.HasValue && model.UpperArmL != null && model.HandL != null && model.UpperArmR != null && model.HandR != null)
            {
                Vector3 o = body.right;
                Reach(model.UpperArmL, model.LowerArmL, model.HandL, handsL.Value, -body.up - o * .7f);
                Reach(model.UpperArmR, model.LowerArmR, model.HandR, handsR.Value, -body.up + o * .7f);
                HandsGap = Mathf.Max(Vector3.Distance(model.HandL.position, handsL.Value), Vector3.Distance(model.HandR.position, handsR.Value));
                CurlRound(model.FingersR, model.HandR, model.MiddleR, handsR.Value, body.right, .8f);
                CurlRound(model.FingersL, model.HandL, model.MiddleL, handsL.Value, body.right, .8f);
                return;
            }
            if (!hoe || !HoeReady) { hoeTime = 0; if (clod != null) clod.gameObject.SetActive(false); return; }
            float before = HoeK; hoeTime += dt; float k = HoeK;
            StrokeAt(k, out var hand, out var edge, out float draw);
            Vector3 up = Vector3.up, ahead = body.forward, side = body.right; ahead.y = 0; ahead.Normalize();
            if (model.Chest != null && Vector3.Dot(model.UpperArmR.position - model.Chest.position, side) < 0) side = -side;
            float ground = Ground(); var foot = transform.position; foot.y = ground;
            // The shaft runs up the body's middle line, in front of it (held like a broom, thumbs up the handle).
            var line = foot + side * .02f;
            Vector3 rear = line + ahead * hand.x + up * hand.y, edgeAt = line + ahead * edge.x + up * edge.y;
            // The back hand sits further up the shaft's line (toward the hip and chest), so the front hand can close on it a full 45-60 cm lower down (note 157).
            { var d0 = edgeAt - rear; if (d0.sqrMagnitude > 1e-4f) rear -= d0.normalized * HoeRearBack; }
            // The top hand at the waist to the chest and well in front (Chris 13:01: "his arms are out of place or awkward", the fist was at his chin).
            { var chestP = model.Chest != null ? model.Chest.position : foot + up * 1.3f; if (rear.y > chestP.y - HoeTopBelowChest) rear.y = chestP.y - HoeTopBelowChest;
              float fwd = Vector3.Dot(rear - chestP, ahead); if (fwd < HoeTopAhead) rear += ahead * (HoeTopAhead - fwd); }
            // The rear (top) hand first; the shaft from its fist to the blade's mark; the front hand where the shaft passes nearest
            // its own shoulder (between a hand's width and two feet down the shaft), so a short arm still closes on it.
            Reach(model.UpperArmR, model.LowerArmR, model.HandR, rear - (Grip(model.HandR, model.MiddleR) - model.HandR.position), -up + side * .8f - ahead * .3f);
            Vector3 gR = Grip(model.HandR, model.MiddleR);
            var dir = (edgeAt - gR); if (dir.sqrMagnitude < 1e-4f) dir = ahead - up; dir.Normalize();
            // Hands wide (Chris 10:34, note 157: "widen the hand spacing"): the back hand at the shaft's end, the front one 45-58 cm down it, about the top third.
            float t = Mathf.Clamp(Vector3.Dot(model.UpperArmL.position - gR, dir), HoeHandsMin, HoeHandsMax);
            var front = gR + dir * t;
            // The front hand turned across the shaft (knuckles up its line, the palm on it), then reached twice so the fist, not the wrist, lands on it.
            for (int pass = 0; pass < 2; pass++)
            {
                Reach(model.UpperArmL, model.LowerArmL, model.HandL, front - (Grip(model.HandL, model.MiddleL) - model.HandL.position), -up - side * .6f - ahead * .3f);
                var across = Vector3.ProjectOnPlane(model.MiddleL.position - model.HandL.position, dir); var want = Vector3.ProjectOnPlane(front - model.HandL.position, dir);
                if (across.sqrMagnitude > 1e-6f && want.sqrMagnitude > 1e-6f) PointBone(model.HandL, model.MiddleL.position - model.HandL.position, (model.MiddleL.position - model.HandL.position) + (want.normalized - across.normalized) * .03f, 1);
            }
            Vector3 gL = Grip(model.HandL, model.MiddleL); HandSpread = Vector3.Distance(gR, gL);
            { var ch = model.Chest != null ? model.Chest.position : foot + up * 1.3f; float sh = Mathf.Min(model.UpperArmL.position.y, model.UpperArmR.position.y);
              PoseHandOverShoulder = Mathf.Max(gL.y, gR.y) - sh; PoseHandAhead = Mathf.Min(Vector3.Dot(gL - ch, ahead), Vector3.Dot(gR - ch, ahead));
              PoseElbowOverShoulder = Mathf.Max(model.LowerArmL.position.y - model.UpperArmL.position.y, model.LowerArmR.position.y - model.UpperArmR.position.y); }
            CurlRound(model.FingersR, model.HandR, model.MiddleR, gR, dir, 1);
            CurlRound(model.FingersL, model.HandL, model.MiddleL, gL, dir, 1);
            float L = tradeToolShaft * tradeTool.lossyScale.y;
            var face = Vector3.Cross(body.right, dir); if (face.sqrMagnitude < 1e-4f) face = -up; face.Normalize();   // the blade hangs from the shaft's end, edge to the user
            float headAt = dir.y < -.15f ? (edgeAt.y - gR.y - face.y * tradeToolHeadReach) / dir.y : L - .08f;
            headAt = Mathf.Clamp(headAt, Vector3.Distance(gR, gL) + .25f, L - .02f);
            hoeButt = gR + dir * (headAt - L); hoeDir = dir; hoeActive = true;
            var tip = gR + dir * headAt + face * tradeToolHeadReach; BladeEdge = tip;
            BladeInSoil = draw > 0 && tip.y < ground + .05f;
            if (before < StrikeAt && k >= StrikeAt)
            {
                StrokeCount++; StrikeGap = tip.y - ground;
                Puff(new Vector3(tip.x, ground + .03f, tip.z));
            }
            // The soil the blade draws: a clod pushed ahead of the edge toward the feet, left where the draw ends.
            if (clod == null && Application.isPlaying)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Sphere); c.name = "Clod"; Destroy(c.GetComponent<Collider>());
                c.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.3f, .21f, .13f)); clod = c.transform; clod.SetParent(transform, true);
            }
            if (clod != null)
            {
                bool show = draw > 0 || k >= .8f && k < .92f;
                if (clod.gameObject.activeSelf != show) clod.gameObject.SetActive(show);
                if (draw > 0) { var toBody = foot - tip; toBody.y = 0; clod.position = new Vector3(tip.x, ground + .04f, tip.z) + toBody.normalized * .07f; clod.localScale = new Vector3(.2f, .09f, .14f) * Mathf.Lerp(.5f, 1.1f, Mathf.InverseLerp(StrikeAt, .8f, k)); clod.rotation = Quaternion.LookRotation(toBody.sqrMagnitude > 1e-4f ? toBody : ahead); }
            }
        }        /// <summary>The middle of a closed hand: a little past the knuckles' root toward them (the palm).</summary>
        static Vector3 Grip(Transform hand, Transform middle) { return middle != null ? Vector3.Lerp(hand.position, middle.position, .75f) : hand.position; }
        /// <summary>Two-bone reach: the upper arm and forearm turned so the wrist lands on <paramref name="target"/> (or as near as the
        /// arm reaches), the elbow bending toward <paramref name="pole"/>. The wrist is put where the hand's grip lands on the mark.</summary>
        static void Reach(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 pole)
        {
            if (upper == null || lower == null || hand == null) return;
            var S = upper.position; float a = Vector3.Distance(S, lower.position), b = Vector3.Distance(lower.position, hand.position);
            var to = target - S; float dist = to.magnitude; if (dist < 1e-3f || a < 1e-3f || b < 1e-3f) return;
            var n = to / dist; float c = Mathf.Clamp(dist, Mathf.Abs(a - b) + .01f, a + b - .002f);
            float cosA = Mathf.Clamp((a * a + c * c - b * b) / (2 * a * c), -1, 1), sinA = Mathf.Sqrt(1 - cosA * cosA);
            var pp = Vector3.ProjectOnPlane(pole, n); if (pp.sqrMagnitude < 1e-6f) pp = Vector3.ProjectOnPlane(Vector3.down, n); pp.Normalize();
            var elbow = S + (n * cosA + pp * sinA) * a;
            PointBone(upper, lower.position - S, elbow - S, 1);
            PointBone(lower, hand.position - lower.position, S + n * c - lower.position, 1);
        }
        /// <summary>Closes a hand's fingers round a bar through <paramref name="at"/> along <paramref name="bar"/> (by <paramref name="w"/>):
        /// each finger curled about the knuckle row, the way that brings the fingers toward the bar.</summary>
        static void CurlRound(Transform[,] F, Transform hand, Transform middle, Vector3 at, Vector3 bar, float w)
        {
            if (F == null || middle == null) return; foreach (var t in F) if (t == null) return;
            var knuckles = (F[0, 0].position - F[3, 0].position); if (knuckles.sqrMagnitude < 1e-8f) return; knuckles.Normalize();
            var seg = F[1, 1].position - F[1, 0].position;
            var toBar = Vector3.ProjectOnPlane(at - F[1, 0].position, bar);
            var plus = Quaternion.AngleAxis(60, knuckles) * seg; var minus = Quaternion.AngleAxis(-60, knuckles) * seg;
            float s = Vector3.Dot(plus, toBar) >= Vector3.Dot(minus, toBar) ? 1 : -1;
            for (int i = 0; i < 4; i++) for (int j = 0; j < 3; j++) F[i, j].rotation = Quaternion.AngleAxis(s * HoeCurl[j] * w, knuckles) * F[i, j].rotation;
        }
        static readonly float[] HoeCurl = { 70, 85, 50 };
        /// <summary>A small brown puff where the head strikes the soil.</summary>
        void Puff(Vector3 at)
        {
            if (!Application.isPlaying) return;
            if (puff == null)
            {
                var go = new GameObject("Soil puff"); go.transform.SetParent(transform, false);
                puff = go.AddComponent<ParticleSystem>(); puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = puff.main; main.playOnAwake = false; main.loop = false; main.startLifetime = .7f; main.startSpeed = .9f; main.startSize = .09f;
                main.gravityModifier = .8f; main.startColor = new Color(.36f, .26f, .16f, .9f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 40;
                var em = puff.emission; em.enabled = false;
                var sh = puff.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = .08f;
                var col = puff.colorOverLifetime; col.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(new Color(.36f, .26f, .16f), 0), new GradientColorKey(new Color(.42f, .33f, .22f), 1) }, new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) }); col.color = g;
                var r = go.GetComponent<ParticleSystemRenderer>(); var sd = Shader.Find("Sprites/Default"); if (sd != null) r.sharedMaterial = new Material(sd);
            }
            puff.transform.position = at; puff.transform.rotation = Quaternion.LookRotation(Vector3.up); puff.Emit(10);
        }
        /// <summary>For tests (note 150): the shaft's line while hoeing (butt and direction), and each hand's knuckle gap to it.</summary>
        public bool HoeReport(out float leftGap, out float rightGap)
        {
            leftGap = rightGap = float.NaN; if (!hoeActive || tradeTool == null) return false;
            Vector3 o = tradeTool.position, d = tradeTool.up;
            float Gap(Vector3 p) { var v = p - o; return (v - Vector3.Dot(v, d) * d).magnitude; }
            leftGap = Gap(model.MiddleL != null ? model.MiddleL.position : model.HandL.position); rightGap = Gap(model.MiddleR != null ? model.MiddleR.position : model.HandR.position);
            return true;
        }
    }
}