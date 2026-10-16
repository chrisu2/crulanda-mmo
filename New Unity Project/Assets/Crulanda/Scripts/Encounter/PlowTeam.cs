using System.Collections.Generic;
using Crulanda.World;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A plough team (playtest note 150, Chris 2026-10-12: he needs to see the field ploughed): a horse drawing a wooden hand plough,
    /// a ploughman behind it with both hands on the handles, slowly up and down the furrows of a bare (or just reaped) field, turning on
    /// the headland at each end, the share leaving a dark furrow behind it. Horse, wood and a man's arms: no machine (the canon's line).
    /// Out from seven in the morning to six in the evening; at other hours, or with no field bare, the team is put away.
    /// VillageLife tends it (<see cref="Tend"/>).
    /// </summary>
    public sealed class PlowTeam : MonoBehaviour
    {
        /// <summary>The walking pace, m/s (a ploughing horse's slow walk).</summary>
        public const float Pace = .75f;
        public const float FromHour = 7, ToHour = 18;
        public static PlowTeam Active { get; private set; }
        /// <summary>Plough the first field whatever the hour or its crop (captures and tests).</summary>
        public static bool Forced;
        public CropField Field { get; private set; }
        public Transform Horse { get; private set; }
        public Transform Plough { get; private set; }
        public ActorVisual Ploughman { get; private set; }
        public bool Out { get { return Field != null && Horse != null && Horse.gameObject.activeSelf; } }
        CritterBody horseBody; Transform gripL, gripR, hitch, trace; TrailRenderer furrow; ZoneBuilder zone;
        readonly List<Vector3> path = new List<Vector3>(); readonly List<float> lengths = new List<float>(); float total, s, nextCheck;
        const float PloughBack = 2.4f, ManBack = 3.4f;
        Animator manAnim; Vector3 manFix;

        /// <summary>Each frame from VillageLife: the team made once, then out on a bare field in its hours, else put away.</summary>
        public static void Tend(VillageLife life)
        {
            if (Active == null)
            {
                if (CropField.All.Count == 0) return;
                var go = new GameObject("Plough team"); go.transform.SetParent(life.transform, false);
                Active = go.AddComponent<PlowTeam>(); Active.zone = life.Zone; Active.Make();
            }
        }
        void OnDestroy() { if (Active == this) Active = null; }

        static Material Mat(Color c) { var m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", .1f); return m; }
        static Transform Bar(Transform parent, Vector3 a, Vector3 b, float thick, Material m, string name)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(o.GetComponent<Collider>()); o.name = name;
            o.transform.SetParent(parent, false); o.transform.localPosition = (a + b) / 2; o.transform.localRotation = Quaternion.LookRotation(b - a);
            o.transform.localScale = new Vector3(thick, thick, (b - a).magnitude); o.GetComponent<Renderer>().sharedMaterial = m; return o.transform;
        }
        void Make()
        {
            var horse = new GameObject("Plough horse"); horse.transform.SetParent(transform, false); Horse = horse.transform;
            horseBody = CritterBody.Build(Horse, "horse", .35f, 4.2f, 0);
            // The plough: its share at the origin on the ground, the beam forward and up to the hitch, two handles back to the man.
            var wood = Mat(new Color(.42f, .3f, .18f)); var iron = Mat(new Color(.3f, .3f, .32f)); var rope = Mat(new Color(.55f, .46f, .3f));
            Plough = new GameObject("Plough").transform; Plough.SetParent(transform, false);
            Bar(Plough, new Vector3(0, .02f, -.15f), new Vector3(0, .12f, .3f), .09f, iron, "Share");
            Bar(Plough, new Vector3(0, .1f, -.1f), new Vector3(0, .58f, 1.65f), .1f, wood, "Beam");
            Bar(Plough, new Vector3(0, .05f, -.2f), new Vector3(0, .45f, .1f), .08f, wood, "Stilt");
            foreach (int sd in new[] { -1, 1 }) Bar(Plough, new Vector3(sd * .12f, .2f, -.05f), new Vector3(sd * .28f, 1.05f, -.7f), .055f, wood, sd < 0 ? "Handle L" : "Handle R");
            Bar(Plough, new Vector3(-.24f, .9f, -.55f), new Vector3(.24f, .9f, -.55f), .04f, wood, "Rung");
            gripL = new GameObject("Grip L").transform; gripL.SetParent(Plough, false); gripL.localPosition = new Vector3(-.28f, 1.05f, -.7f);
            gripR = new GameObject("Grip R").transform; gripR.SetParent(Plough, false); gripR.localPosition = new Vector3(.28f, 1.05f, -.7f);
            hitch = new GameObject("Hitch").transform; hitch.SetParent(Plough, false); hitch.localPosition = new Vector3(0, .58f, 1.65f);
            trace = Bar(transform, Vector3.zero, Vector3.forward, .035f, rope, "Trace");
            // The furrow: a dark band laid behind the share, fading over a few minutes.
            var f = new GameObject("Furrow"); f.transform.SetParent(Plough, false); f.transform.localPosition = new Vector3(0, .04f, -.2f); f.transform.localRotation = Quaternion.Euler(90, 0, 0);
            furrow = f.AddComponent<TrailRenderer>(); furrow.time = 240; furrow.minVertexDistance = .3f; furrow.widthMultiplier = .32f; furrow.alignment = LineAlignment.TransformZ;
            furrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; var sd2 = Shader.Find("Sprites/Default");
            if (sd2 != null) furrow.sharedMaterial = new Material(sd2); furrow.startColor = new Color(.24f, .17f, .1f, .95f); furrow.endColor = new Color(.3f, .22f, .14f, 0);
            // The ploughman: a villager's figure (no trade tool: his hands are on the plough).
            var man = new GameObject("Ploughman"); man.transform.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(man.transform, false);
            Ploughman = ActorVisual.Attach(man, ActorLook.Villager, 23);
            foreach (var t in man.GetComponentsInChildren<Transform>(true)) if (t.name == "Trade tool") Destroy(t.gameObject);   // his hands are on the plough
            Show(false);
        }
        void Show(bool on)
        {
            Horse.gameObject.SetActive(on); Plough.gameObject.SetActive(on); trace.gameObject.SetActive(on); Ploughman.gameObject.SetActive(on);
            if (!on && furrow != null) furrow.Clear();
        }
        CropField Pick()
        {
            CropField best = null;
            foreach (var c in CropField.All)
            {
                if (c == null || c.Field == null) continue;
                if (Forced) { if (best == null || c.Field.size.sqrMagnitude > best.Field.size.sqrMagnitude) best = c; continue; }
                if (c.Stage == "bare" || c.Stage == "reaped") { best = c; break; }
            }
            return best;
        }
        /// <summary>Puts the team on <paramref name="f"/> (null: away) and lays out its round: up and down every other furrow,
        /// turning on the headlands, then back along the near headland to the first.</summary>
        public void Begin(CropField f)
        {
            Field = f; path.Clear(); lengths.Clear(); total = 0; s = 0;
            if (f == null) { Show(false); return; }
            float hx = f.Field.size.x / 2 - 1.3f, hz = f.Field.size.y / 2 - 1.6f;
            var rows = new List<float>();
            for (float x = Mathf.Ceil((-hx - .275f) / 3.2f) * 3.2f + .275f; x <= hx; x += 3.2f) rows.Add(x);
            if (rows.Count < 2) { rows.Clear(); for (float x = Mathf.Ceil((-hx - .275f) / 1.6f) * 1.6f + .275f; x <= hx; x += 1.6f) rows.Add(x); }
            if (rows.Count % 2 == 1) rows.RemoveAt(rows.Count - 1);
            if (rows.Count < 2) { Field = null; Show(false); return; }
            var local = new List<Vector2>();
            void Arc(Vector2 c, float r, float a0, float a1) { int n = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(a1 - a0) * r / .25f)); for (int i = 1; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n); local.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); } }
            for (int i = 0; i < rows.Count; i++)
            {
                bool upward = i % 2 == 0; float z0 = upward ? -hz : hz, z1 = -z0;
                for (float z = 0; z <= 2 * hz; z += .25f) local.Add(new Vector2(rows[i], z0 + (upward ? z : -z)));
                if (i + 1 < rows.Count) { float r = (rows[i + 1] - rows[i]) / 2; Arc(new Vector2(rows[i] + r, z1), r, Mathf.PI, upward ? 0 : 2 * Mathf.PI); }
            }
            float xl = rows[rows.Count - 1], x0 = rows[0];
            Arc(new Vector2(xl - 1, -hz), 1, 0, -Mathf.PI / 2);
            for (float x = xl - 1; x >= x0 + 1; x -= .25f) local.Add(new Vector2(x, -hz - 1));
            Arc(new Vector2(x0 + 1, -hz), 1, -Mathf.PI / 2, -Mathf.PI);
            foreach (var p in local)
            {
                var w = f.transform.TransformPoint(new Vector3(p.x, 0, p.y));
                w.y = zone != null ? zone.HeightAt(w.x, w.z) : f.transform.position.y;
                if (path.Count > 0) total += Vector3.Distance(path[path.Count - 1], w);
                path.Add(w); lengths.Add(total);
            }
            total += Vector3.Distance(path[path.Count - 1], path[0]);
            Show(true); Place(0);
            if (furrow != null) furrow.Clear();
        }
        /// <summary>The point <paramref name="at"/> metres round the path (wrapping), and the way it goes there.</summary>
        public Vector3 At(float at, out Vector3 dir)
        {
            at = Mathf.Repeat(at, total);
            int lo = 0, hi = lengths.Count - 1;
            while (lo < hi) { int m = (lo + hi + 1) / 2; if (lengths[m] <= at) lo = m; else hi = m - 1; }
            var a = path[lo]; var b = lo + 1 < path.Count ? path[lo + 1] : path[0];
            float seg = (lo + 1 < lengths.Count ? lengths[lo + 1] : total) - lengths[lo];
            dir = b - a; dir.y = 0; if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward; dir.Normalize();
            return Vector3.Lerp(a, b, seg > 1e-4f ? (at - lengths[lo]) / seg : 0);
        }
        void Place(float dt)
        {
            s += Pace * dt;
            var h = At(s, out var hd); Horse.position = h; Horse.rotation = Quaternion.LookRotation(hd);
            if (horseBody != null) { if (dt > 0) horseBody.Stride(Pace); else horseBody.Rest(); }
            var share = At(s - PloughBack, out _); var to = h - share; to.y = 0; if (to.sqrMagnitude < 1e-4f) to = hd;
            Plough.position = share; Plough.rotation = Quaternion.LookRotation(to.normalized);
            var man = At(s - ManBack, out _); var look = Plough.TransformPoint(new Vector3(0, 0, -.78f)) - man; look.y = 0;
            // The figure's shoulders are kept a short reach behind the handles (measured off its bones, last frame's pose).
            if (manAnim == null) manAnim = Ploughman.GetComponentInChildren<Animator>();
            var gripMid = (gripL.position + gripR.position) / 2;
            if (manAnim != null && manAnim.isHuman && manAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm) != null)
            {
                var sh = (manAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm).position + manAnim.GetBoneTransform(HumanBodyBones.RightUpperArm).position) / 2;
                var want = gripMid - Plough.forward * .28f; var err = want - sh; err.y = 0;
                var feet = Mathf.Min(manAnim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, manAnim.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
                err.y = (man.y + .09f) - feet;   // the ankles a hand over the furrow
                manFix = Vector3.Lerp(manFix, manFix + err, dt > 0 ? Mathf.Min(1, dt * 6) : 1); manFix = Vector3.ClampMagnitude(manFix, 1.5f);
            }
            Ploughman.transform.position = man + Vector3.up + manFix; var face = Plough.forward; face.y = 0; if (face.sqrMagnitude > 1e-4f) Ploughman.transform.rotation = Quaternion.LookRotation(face.normalized);
            Ploughman.HandsOn(gripL.position, gripR.position);
            // The trace from the plough's hitch to the horse's collar.
            var collar = Horse.position + Horse.forward * .55f + Vector3.up * 1.05f; var hp = hitch.position;
            trace.position = (hp + collar) / 2; var span = collar - hp; if (span.sqrMagnitude > 1e-4f) trace.rotation = Quaternion.LookRotation(span);
            trace.localScale = new Vector3(.035f, .035f, span.magnitude);
        }
        void Update()
        {
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + 10;
                bool hours = Forced || WorldClock.Between(FromHour, ToHour);
                var want = hours ? (Field != null && (Forced || Field.Stage == "bare" || Field.Stage == "reaped") ? Field : Pick()) : null;
                if (want != Field || (want != null && !Out)) Begin(want);
            }
            if (Out) Place(Time.deltaTime);
        }
        /// <summary>For tests: checks the path at once (as the next ten-second check would).</summary>
        public void CheckNow() { nextCheck = 0; Update(); }
    }
}