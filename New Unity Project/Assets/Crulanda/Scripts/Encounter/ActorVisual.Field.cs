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
        public const float StrokePeriod = 1.7f;
        /// <summary>Where in the stroke the head meets the soil (0-1 of the period).</summary>
        const float StrikeAt = .62f;
        float hoeTime; bool hoeActive; Vector3 hoeButt, hoeDir; float tradeToolHeadReach = .05f;
        /// <summary>Strokes struck so far (the farmer steps along the row after each: Villager).</summary>
        public int StrokeCount { get; private set; }
        /// <summary>The tool head's lowest point over the ground at the last strike, in metres (negative: into the soil).</summary>
        public float StrikeGap { get; private set; } = float.NaN;
        /// <summary>Hoeing this frame (a modelled farmer at work with a long tool, standing).</summary>
        public bool Hoeing { get { return hoeActive; } }
        Vector3? handsL, handsR; int handsFrame = -1;
        /// <summary>How far the farther hand ended from its mark (<see cref="HandsOn"/>), as last posed, in metres.</summary>
        public float HandsGap { get; private set; } = float.NaN;
        ParticleSystem puff;
        bool HoeReady { get { return model != null && tradeTool != null && tradeToolLong && model.UpperArmL != null && model.HandL != null && model.HandR != null; } }
        /// <summary>For a ploughman: puts the hands on these points this frame (call before LateUpdate, each frame).</summary>
        public void HandsOn(Vector3 left, Vector3 right) { handsL = left; handsR = right; handsFrame = Time.frameCount; }

        /// <summary>The stroke at <paramref name="k"/> (0-1): how raised (1: the head up, 0: down at the soil) and how far pulled back.</summary>
        static void StrokeAt(float k, out float lift, out float pull)
        {
            if (k < .45f) { lift = Mathf.SmoothStep(0, 1, k / .45f); pull = 1 - lift; }
            else if (k < StrikeAt) { float u = (k - .45f) / (StrikeAt - .45f); lift = 1 - u * u; pull = 0; }   // down fast, quickening
            else if (k < .85f) { lift = 0; pull = Mathf.SmoothStep(0, 1, (k - StrikeAt) / (.85f - StrikeAt)); }   // drawn back through the soil
            else { lift = 0; pull = 1; }   // a pause: the step along the row
        }
        /// <summary>Where the hoe stroke is (0-1; the strike at 0.62), for captures.</summary>
        public float StrokePhase { get { return HoeK; } }
        float HoeK { get { return Mathf.Repeat(hoeTime / StrokePeriod, 1); } }
        /// <summary>The back's bend for the stroke: stooped to the soil, straighter with the tool raised.</summary>
        float HoeBend() { StrokeAt(HoeK, out float lift, out _); return 24 - lift * 14; }

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
            hoeActive = false;
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
            if (!hoe || !HoeReady) { hoeTime = 0; return; }
            float before = HoeK; hoeTime += dt; float k = HoeK;
            StrokeAt(k, out float lift, out float pull);
            Vector3 up = body.up, ahead = body.forward, side = body.right;
            if (model.Chest != null && Vector3.Dot(model.UpperArmR.position - model.Chest.position, side) < 0) side = -side;
            var mid = (model.UpperArmL.position + model.UpperArmR.position) / 2;
            // The shaft's slope and the hands' middle, between three keys: down at the strike, drawn back, raised.
            float down = Mathf.Lerp(-58, -46, pull), slope = Mathf.Lerp(down, 48, lift);
            var d = (Quaternion.AngleAxis(-slope, side) * ahead).normalized;   // tilted up by slope degrees about the right-hand side
            if (Vector3.Dot(d, up) * Mathf.Sign(slope) < 0) d = (Quaternion.AngleAxis(slope, side) * ahead).normalized;
            var centre = mid + side * .1f + ahead * Mathf.Lerp(Mathf.Lerp(.36f, .24f, pull), .3f, lift) - up * Mathf.Lerp(.44f, .06f, lift);
            Vector3 rear = centre - d * .18f, front = centre + d * .18f;
            Reach(model.UpperArmR, model.LowerArmR, model.HandR, rear, -up + side * .8f - ahead * .2f);
            Reach(model.UpperArmL, model.LowerArmL, model.HandL, front, -up - side * .5f - ahead * .2f);
            // The shaft through both fists as they landed (an arm too short for its mark stops short; the shaft follows the hands).
            Vector3 gR = Grip(model.HandR, model.MiddleR), gL = Grip(model.HandL, model.MiddleL);
            var dir = gL - gR; if (dir.sqrMagnitude < 1e-4f) dir = d; dir.Normalize();
            CurlRound(model.FingersR, model.HandR, model.MiddleR, gR, dir, 1);
            CurlRound(model.FingersL, model.HandL, model.MiddleL, gL, dir, 1);
            gR = Grip(model.HandR, model.MiddleR); gL = Grip(model.HandL, model.MiddleL); dir = gL - gR; if (dir.sqrMagnitude < 1e-4f) dir = d; dir.Normalize();
            float L = tradeToolShaft * tradeTool.lossyScale.y, reach = tradeToolHeadReach;
            // How far up the shaft from the rear fist the head's end is: near the end of the shaft when raised; slid so the head's
            // far point is just into the soil when it slopes down to it.
            float ground = Ground(), far = L - .08f;
            float toSoil = dir.y < -.2f ? (gR.y - (ground - .02f)) / -dir.y - reach : far;
            float soilW = Mathf.InverseLerp(-.2f, -.5f, dir.y);
            float headAt = Mathf.Clamp(Mathf.Lerp(far, toSoil, soilW), Vector3.Distance(gR, gL) + .25f, L - .02f);
            hoeButt = gR + dir * (headAt - L); hoeDir = dir; hoeActive = true;
            if (before < StrikeAt && k >= StrikeAt)
            {
                StrokeCount++;
                var tip = gR + dir * (headAt + reach); StrikeGap = tip.y - ground;
                Puff(new Vector3(tip.x, ground + .03f, tip.z));
            }
        }
        /// <summary>The middle of a closed hand: a little past the knuckles' root toward them (the palm).</summary>
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