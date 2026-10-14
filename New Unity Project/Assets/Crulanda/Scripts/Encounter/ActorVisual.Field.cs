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
        /// <summary>The hoe worked by Kevin's ploughing clip this frame (PoseOf), not the hand-set stroke.</summary>
        bool ploughing;
        /// <summary>One of Kevin's three gathering motions, fixed for the figure.</summary>
        string GatherSlot { get { if (gatherPick < 0) gatherPick = gatherNext++ % 3; return "k.gather" + (1 + gatherPick); } }
        int gatherPick = -1; static int gatherNext;
        /// <summary>The ploughing clip has the hands: the hoe's shaft runs from just behind the upper fist through the lower one, the
        /// blade on past it to the ground; the fingers close round it.</summary>
        void PloughHands()
        {
            Vector3 Grip(Transform hand, Transform middle) { return middle != null ? Vector3.Lerp(hand.position, middle.position, .55f) : hand.position; }
            Vector3 gl = Grip(model.HandL, model.MiddleL), gr = Grip(model.HandR, model.MiddleR);
            Vector3 top = gl.y >= gr.y ? gl : gr, low = gl.y >= gr.y ? gr : gl, d = low - top;
            if (d.sqrMagnitude < .01f) d = -body.up * .6f + body.forward * .4f;
            hoeDir = d.normalized; hoeButt = top - hoeDir * .1f; hoeActive = true;
            float scale = tradeTool != null ? tradeTool.lossyScale.y : 1;
            BladeEdge = hoeButt + hoeDir * (tradeToolShaft * scale + tradeToolHeadReach); BladeInSoil = BladeEdge.y < Ground() + .03f;
            CurlRound(model.FingersR, model.HandR, model.MiddleR, gr, body.right, .8f);
            CurlRound(model.FingersL, model.HandL, model.MiddleL, gl, body.right, .8f);
        }
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
        /// <summary>The hands' spread on the shaft: wanted, and the least and most it may be when the front arm is short (m).</summary>
        public const float HoeHands = .49f, HoeHandsMin = .47f, HoeHandsMax = .58f;
        /// <summary>The hoer's stance (2026-10-14): the shoulders turned this far from square, the leading (left) one forward; the
        /// leading collarbone swung this far forward as its arm reaches down the shaft; the stroke worked this far to the side of the
        /// feet (negative: the left), the shaft across the body and the blade out in front of the leading foot.</summary>
        public const float HoeTurn = 40, HoeReachOut = 28, HoeLine = -.45f;
        /// <summary>The top fist's limits (notes 164, 168): at least this far under the lower shoulder and in front of the chest, and
        /// this far clear of the leading shoulder; each arm is planned this much short of its full reach.</summary>
        public const float HoeUnderShoulder = .03f, HoeFistAhead = .28f, HoeFistClear = .16f, HoeReachSpare = .015f;
        Vector3? handsL, handsR; int handsFrame = -1;
        /// <summary>How far the farther hand ended from its mark (<see cref="HandsOn"/>), as last posed, in metres.</summary>
        public float HandsGap { get; private set; } = float.NaN;
        ParticleSystem puff;
        bool HoeReady { get { return model != null && tradeTool != null && tradeToolLong && model.UpperArmL != null && model.HandL != null && model.HandR != null; } }
        /// <summary>For a ploughman: puts the hands on these points this frame (call before LateUpdate, each frame).</summary>
        public void HandsOn(Vector3 left, Vector3 right) { handsL = left; handsR = right; handsFrame = Time.frameCount; }

        // The draw hoe's stroke (Docs/HOE_TECHNIQUE.md; Chris 09:47: "looks like they are just banging into onto the ground"): reach
        // forward and lift a little, set the blade in at an angle a little past the mark, draw it back toward the feet just under the
        // surface (the soil going with it), then recover. Keys in the body's frame: the blade's edge as (ahead, height over the ground);
        // the top (right) fist from the chest as (to the right, under the lower shoulder, ahead), forward and up a little for the
        // lift, back and down with the draw.
        static readonly Vector3 RecoverFist = new Vector3(.18f, .10f, .30f), LiftFist = new Vector3(.16f, .08f, .34f), EnterFist = new Vector3(.17f, .09f, .33f), DrawnFist = new Vector3(.20f, .12f, .29f);
        static readonly Vector2 RecoverEdge = new Vector2(.62f, .12f), LiftEdge = new Vector2(1.12f, .36f), EnterEdge = new Vector2(1.05f, -.03f), DrawnEdge = new Vector2(.5f, -.03f);
        /// <summary>The stroke at <paramref name="k"/> (0-1): the top fist's key, the blade's edge (ahead, height), and whether the blade is in the soil.</summary>
        static void StrokeAt(float k, out Vector3 fist, out Vector2 edge, out float draw)
        {
            if (k < .35f) { float u = Mathf.SmoothStep(0, 1, k / .35f); fist = Vector3.Lerp(RecoverFist, LiftFist, u); edge = Vector2.Lerp(RecoverEdge, LiftEdge, u); draw = 0; }
            else if (k < StrikeAt) { float u = (k - .35f) / (StrikeAt - .35f); u *= u; fist = Vector3.Lerp(LiftFist, EnterFist, u); edge = Vector2.Lerp(LiftEdge, EnterEdge, u); draw = 0; }   // set in, quickening
            else if (k < .8f) { float u = Mathf.SmoothStep(0, 1, (k - StrikeAt) / (.8f - StrikeAt)); fist = Vector3.Lerp(EnterFist, DrawnFist, u); edge = Vector2.Lerp(EnterEdge, DrawnEdge, u); draw = 1; }   // drawn back to the feet
            else { float u = Mathf.SmoothStep(0, 1, (k - .8f) / .2f); fist = Vector3.Lerp(DrawnFist, RecoverFist, u); edge = Vector2.Lerp(DrawnEdge, RecoverEdge, u); draw = 0; }   // out of the soil; the step
        }
        /// <summary>Where in the stroke the hoe is (0-1; set in at <see cref="StrikeAt"/>), for captures.</summary>
        public float StrokePhase { get { return HoeK; } }
        float HoeK { get { return Mathf.Repeat(hoeTime / StrokePeriod, 1); } }
        /// <summary>The back's slight bend for the stroke (a draw hoe is worked nearly upright), a little more as it draws.</summary>
        float HoeBend() { StrokeAt(HoeK, out _, out _, out float draw); return 36 + draw * 8; }   // bent a little more over the shaft, so the front arm reaches 45-60 cm down it with relaxed shoulders (note 168)
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
            if (hoe && ploughing && HoeReady) { PloughHands(); return; }
            if (!hoe || !HoeReady) { hoeTime = 0; if (clod != null) clod.gameObject.SetActive(false); return; }
            float before = HoeK; hoeTime += dt; float k = HoeK;
            StrokeAt(k, out var key, out var edge, out float draw);
            Vector3 up = Vector3.up, ahead = body.forward, side = body.right; ahead.y = 0; ahead.Normalize();
            if (model.Chest != null && Vector3.Dot(model.UpperArmR.position - model.Chest.position, side) < 0) side = -side;
            float ground = Ground(); var foot = transform.position; foot.y = ground;
            // The stroke is worked out in front of the leading (left) foot, the shaft across the body from the top fist (2026-10-14).
            // On the body's middle line the front hand's place on the shaft was 70-85 cm from its shoulder, out of an arm's 54-57 cm
            // reach, so it stopped short of the shaft (4-9 cm off it) and the fists came 20-45 cm apart.
            var edgeAt = foot + side * HoeLine + ahead * edge.x + up * edge.y;
            // The leading arm reaches down the shaft: its collarbone swung forward (the back is bent and turned by now: HoeStance).
            var clav = HoeClavicle(); if (clav != null) clav.rotation = Quaternion.AngleAxis(HoeReachOut * hoeStance * Mathf.Sign(Vector3.Dot(side, body.right)), up) * clav.rotation;
            Vector3 chest = model.Chest != null ? model.Chest.position : foot + up * 1.3f, shL = model.UpperArmL.position, shR = model.UpperArmR.position;
            float low = Mathf.Min(shL.y, shR.y);
            float reachL = ArmReach(model.UpperArmL, model.LowerArmL, model.HandL, model.MiddleL) - HoeReachSpare, reachR = ArmReach(model.UpperArmR, model.LowerArmR, model.HandR, model.MiddleR) - HoeReachSpare;
            // The top fist: its key in front of the chest, then moved the least it takes for both arms to reach (the front hand a full
            // spread further down the shaft toward the blade's mark), clear of the leading shoulder, under the shoulders and in front
            // of the chest (notes 157, 164, 168). It starts from the key every frame and each step is small, so it follows the stroke.
            Vector3 TopFist(float under, float minAhead)
            {
                var g = chest + side * key.x + ahead * key.z; g.y = low - key.y;
                for (int pass = 0; pass < 8; pass++)
                {
                    var v = g - shR; if (v.magnitude > reachR) g = shR + v.normalized * reachR;
                    var d = (edgeAt - g).normalized; v = g + d * HoeHands - shL; float over = v.magnitude - reachL; if (over > 0) g -= v.normalized * over;
                    v = g - shL; if (v.magnitude < HoeFistClear) g = shL + v.normalized * HoeFistClear;
                    if (g.y > low - under) g.y = low - under;
                    float fw = Vector3.Dot(g - chest, ahead); if (fw < minAhead) g += ahead * (minAhead - fw);
                }
                return g;
            }
            // Hands wide (Chris 10:34, note 157: "widen the hand spacing"): the front hand a spread down the shaft, or as far down as its
            // arm reaches (never nearer than the least spread).
            float Spread(Vector3 g, Vector3 d)
            {
                var w = g - shL; float b = Vector3.Dot(w, d), disc = b * b - w.sqrMagnitude + reachL * reachL, s = HoeHands;
                if (disc >= 0) s = Mathf.Clamp(s, -b - Mathf.Sqrt(disc), -b + Mathf.Sqrt(disc));
                return s;
            }
            var gR = TopFist(HoeUnderShoulder, HoeFistAhead);
            var dir = edgeAt - gR; if (dir.sqrMagnitude < 1e-4f) dir = ahead - up; dir.Normalize();
            float spread = Spread(gR, dir);
            if (spread < HoeHandsMin)
            {
                // Out of reach with the full margins (a short-armed figure, an idle that drops the leading shoulder): half of them.
                var g2 = TopFist(HoeUnderShoulder * .5f, HoeFistAhead - HoeUnderShoulder * .5f); var d2 = edgeAt - g2;
                if (d2.sqrMagnitude > 1e-4f) { d2.Normalize(); float s2 = Spread(g2, d2); if (s2 > spread) { gR = g2; dir = d2; spread = s2; } }
            }
            spread = Mathf.Clamp(spread, HoeHandsMin, HoeHandsMax);
            GripAt(model.UpperArmR, model.LowerArmR, model.HandR, model.MiddleR, gR, dir, -up + side * .8f - ahead * .3f, .6f);
            GripAt(model.UpperArmL, model.LowerArmL, model.HandL, model.MiddleL, gR + dir * spread, dir, -up - side * .6f - ahead * .3f, .3f);
            gR = Grip(model.HandR, model.MiddleR); Vector3 gL = Grip(model.HandL, model.MiddleL);
            // The shaft is laid through both fists as they closed (each is on it), and on to the blade.
            if ((gL - gR).sqrMagnitude > .04f) dir = (gL - gR).normalized;
            HandSpread = Vector3.Distance(gR, gL);
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
        /// <summary>How far an arm's grip reaches from its shoulder, straight (upper arm, forearm and three quarters of the hand).</summary>
        static float ArmReach(Transform upper, Transform lower, Transform hand, Transform middle)
        {
            return Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position) + (middle != null ? .75f * Vector3.Distance(hand.position, middle.position) : 0);
        }
        /// <summary>A hand closed on a bar at <paramref name="at"/>: the wrist on the forearm's line, turned <paramref name="square"/>
        /// of the way to square with the bar (the bar across the palm; a reaching arm keeps it nearly straight, as a bent wrist
        /// shortens the reach), and the arm reached so the grip, not the wrist, lands there; three times, as the hand turns with the
        /// forearm.</summary>
        static void GripAt(Transform upper, Transform lower, Transform hand, Transform middle, Vector3 at, Vector3 bar, Vector3 pole, float square)
        {
            if (upper == null || lower == null || hand == null) return;
            for (int i = 0; i < 3; i++)
            {
                if (middle != null)
                {
                    var fore = (hand.position - lower.position).normalized; var across = Vector3.ProjectOnPlane(fore, bar);
                    if (across.sqrMagnitude > 1e-4f) PointBone(hand, middle.position - hand.position, Vector3.Slerp(fore, across.normalized, square), 1);
                }
                Reach(upper, lower, hand, at - (Grip(hand, middle) - hand.position), pole);
            }
        }
        ModelFigure hoeClavOf; Transform hoeClav;
        /// <summary>The leading (left) collarbone of this figure, or null.</summary>
        Transform HoeClavicle()
        {
            if (hoeClavOf != model) { hoeClavOf = model; var t = model.BonesFor(new[] { "clavicle_l" })[0]; hoeClav = t != null && t.name == "clavicle_l" ? t : null; }
            return hoeClav;
        }
        float hoeStance;
        /// <summary>The hoer's stance (2026-10-14). The idles under the stroke turn the shoulders 15-35 degrees either way as they look
        /// about, and the back's bend about the body's right then tipped them 10-45 degrees, so the front hand's place on the shaft
        /// moved out of its reach from one stroke to the next. Before the back bends (<paramref name="turned"/> false) the shoulders
        /// are squared to the stroke, so the bend tips neither; once it is bent (true) they are turned <see cref="HoeTurn"/> about the
        /// upright, both staying level, so the leading (left) one is forward, as a hoer stands with the lower hand's side to the work,
        /// and the head is turned back onto the work. Taken and left at once with the grip on the shaft, as the arms go straight to
        /// it and straight back (a turned body under a staff-held tool is no pose of any clip).</summary>
        void HoeStance(bool on, bool apply, bool turned)
        {
            if (!turned) hoeStance = on ? 1 : 0;
            if (!apply || hoeStance <= 0 || model.Spine == null || model.Chest == null || model.UpperArmL == null || model.UpperArmR == null) return;
            Vector3 ahead = body.forward, side = body.right; ahead.y = 0; ahead.Normalize();
            float sgn = 1; if (Vector3.Dot(model.UpperArmR.position - model.Chest.position, side) < 0) { side = -side; sgn = -1; }   // turned toward the right arm's own side
            float turn;
            if (!turned)
            {
                var line = model.UpperArmR.position - model.UpperArmL.position;
                float yaw = Mathf.Atan2(-Vector3.Dot(line, ahead), Vector3.Dot(line, side)) * Mathf.Rad2Deg;   // positive: the left shoulder ahead
                turn = Mathf.DeltaAngle(yaw, 0) * hoeStance * sgn;
            }
            else turn = HoeTurn * hoeStance * sgn;
            model.Spine.rotation = Quaternion.AngleAxis(turn * .4f, body.up) * model.Spine.rotation;
            model.Chest.rotation = Quaternion.AngleAxis(turn * .6f, body.up) * model.Chest.rotation;
            // The head back onto the work, the turn shared by the neck and the head (all of it in the neck twisted the collar open).
            if (turned && model.Neck != null) model.Neck.rotation = Quaternion.AngleAxis(-turn * .45f, body.up) * model.Neck.rotation;
            if (turned && model.HeadBone != null) model.HeadBone.rotation = Quaternion.AngleAxis(-turn * (model.Neck != null ? .6f : 1.05f), body.up) * model.HeadBone.rotation;
        }
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