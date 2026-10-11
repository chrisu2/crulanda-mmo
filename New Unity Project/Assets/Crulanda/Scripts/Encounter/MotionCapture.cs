using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Frames of the player's own motions, for looking at what the tests cannot see (2026-10-10: Chris on the jump, "still feel floaty
    /// and when you land feet twist together", and "still no bow animations on ranger"). Run the player windowed, on a temp save:
    /// Crulanda.exe --crulanda-motion-capture &lt;folder&gt; --crulanda-class class.ranger. On a road, game time slowed, a camera beside
    /// the figure: a standing jump and a running jump from take-off to after the landing (jump-stand-NN, jump-run-NN), then an aimed
    /// shot drawn and loosed and the auto-shot after it at a stunned boar, from the archer's left (bow-NN) and from behind as you play
    /// (bow-back-NN). What the motor reported at each frame goes in motion.txt. MOTION_CAPTURE_DONE in the log.
    /// </summary>
    public sealed class MotionCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-motion-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        Camera side; Transform watch; Vector3 offset; readonly StringBuilder report = new StringBuilder();
        // Kevin Iglesias's own figure (his HumanF or HumanM model, on its own avatar) running his run clip a stride ahead of ours, so the
        // frames show whether a foot is his clip's or comes from our body (2026-10-10, Chris: "feet backwards and screwed up").
        Transform kevin; PlayableGraph kevinGraph;
        /// <summary>A figure a stride ahead of ours: <paramref name="src"/> on <paramref name="avatar"/> (its own when null) playing
        /// <paramref name="clip"/> as made, nothing of ours on it (no foot correction, no foot IK).</summary>
        void Beside(string label, GameObject src, Avatar avatar, AnimationClip clip, Material look, bool tint)
        {
            if (kevin != null) Destroy(kevin.gameObject);
            if (kevinGraph.IsValid()) kevinGraph.Destroy();
            if (src == null || clip == null) { report.AppendLine("no " + label + ": model " + (src != null) + " clip " + (clip != null)); return; }
            kevin = Instantiate(src).transform; kevin.name = label;
            var anim = kevin.GetComponentInChildren<Animator>(); if (anim == null) anim = kevin.gameObject.AddComponent<Animator>();
            if (avatar != null) anim.avatar = avatar;
            anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; anim.runtimeAnimatorController = null;
            report.AppendLine(label + ": avatar " + (anim.avatar != null ? anim.avatar.name + " human " + anim.avatar.isHuman : "none") + ", clip " + clip.name);
            if (look != null)
                foreach (var r in kevin.GetComponentsInChildren<Renderer>())
                {
                    var m = new Material(look); var c = tint ? new Color(.78f, .72f, .62f) : Color.white;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                    var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = m; r.sharedMaterials = ms;
                }
            var p = AnimationClipPlayable.Create(kevinGraph = PlayableGraph.Create(label), clip); p.SetApplyFootIK(false);
            AnimationPlayableOutput.Create(kevinGraph, "out", anim).SetSourcePlayable(p); kevinGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime); kevinGraph.Play();
        }
        static AnimationClip UnityClip(string file)
        {
            foreach (var c in Resources.LoadAll<AnimationClip>("Characters/Animations/Unity/" + file + ".anim")) if (c != null && !c.name.StartsWith("__preview__")) return c;
            return null;
        }
        void OnDestroy() { if (kevinGraph.IsValid()) kevinGraph.Destroy(); }
        void LateUpdate()
        {
            if (kevin != null && watch != null)
            {
                var f = watch.forward; f.y = 0; f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
                var at = watch.position + f * 1.3f; var z = ZoneBuilder.Active; float g = z != null ? z.Ground(new Vector2(at.x, at.z)).y : at.y - 1;
                kevin.SetPositionAndRotation(new Vector3(at.x, g, at.z), Quaternion.LookRotation(-f));   // his model faces -z, as ours does
            }
            if (side == null || watch == null) return;
            side.transform.position = watch.position + watch.rotation * offset;
            side.transform.LookAt(watch.position + Vector3.down * (offset.y < -.3f ? .6f : .1f));   // the actor's origin is about a metre up: head to feet in frame, or the feet close
        }
        IEnumerator Shot(string directory, string name, AdventurerMotor motor, float since)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
            var fig = motor.GetComponent<ActorVisual>()?.Figure; string feet = "";
            if (fig != null && fig.FootL != null && fig.FootR != null)
            {
                var z = Crulanda.World.ZoneBuilder.Active; var p = motor.transform.position; float ground = z != null ? z.Ground(new Vector2(p.x, p.z)).y : 0;
                feet = " feet L" + (fig.FootL.position.y - ground).ToString("0.00") + " R" + (fig.FootR.position.y - ground).ToString("0.00") + " hips " + (fig.Hips.position.y - ground).ToString("0.00");
            }
            report.AppendLine(name + "  t=" + since.ToString("0.00") + "  airborne=" + motor.Airborne + " rising=" + motor.Rising + " landedAge=" + motor.LandedAge.ToString("0.00") + " y=" + motor.transform.position.y.ToString("0.00") + feet + "  mix " + (fig != null ? fig.Mix : ""));
            yield return null;
        }
        /// <summary>A jump, framed at these game-time offsets from the jump.</summary>
        IEnumerator Jump(string directory, string prefix, AdventurerMotor motor, float[] at)
        {
            motor.Jump(); yield return null;
            float t0 = Time.time; int n = 1;
            foreach (var t in at)
            {
                while (Time.time - t0 < t) yield return null;
                yield return Shot(directory, prefix + "-" + (n < 10 ? "0" : "") + n++, motor, Time.time - t0);
            }
        }
        public IEnumerator Run(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            WorldClock.Hour = 11;
            var weather = WorldWeather.Active; if (weather != null) weather.Force(WeatherKind.Clear, true);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 100000); s.Player.Health.ApplyHealing(100000);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            yield return new WaitForSeconds(3);
            // A straight stretch of road with room to run: the longest leg of the zone's first long road.
            Vector2 from = Vector2.zero, to = Vector2.up; float best = 0;
            foreach (var r in s.Zone.Zone.roads)
                if (r != null && r.points != null)
                    for (int i = 0; i + 1 < r.points.Length; i++) { float d = Vector2.Distance(r.points[i], r.points[i + 1]); if (d > best) { best = d; from = r.points[i]; to = r.points[i + 1]; } }
            var dir = (to - from).normalized; float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            motor.Teleport(s.Zone.Ground(from + dir * best * .25f, 1.1f)); motor.SetView(yaw, 12, 6);
            yield return new WaitForSeconds(1.5f);
            var main = Camera.main; watch = s.Player.transform; offset = new Vector3(4.8f, .1f, .4f);   // on the figure's right
            side = new GameObject("Motion camera").AddComponent<Camera>(); side.CopyFrom(main); side.depth = main.depth + 1; side.fieldOfView = 40;
            float[] frames = { .02f, .08f, .14f, .2f, .26f, .32f, .38f, .44f, .5f, .55f, .6f, .66f, .74f, .85f, 1.0f };
            Time.timeScale = .25f;
            yield return Jump(directory, "jump-stand", motor, frames);
            Time.timeScale = 1; yield return new WaitForSeconds(1);
            // The running jump: a second's run-up along the road, then the jump, the camera keeping beside.
            AdventurerMotor.Running = true; EncounterInput.Scripted = new Vector2(0, 1);
            yield return new WaitForSeconds(1.2f);
            Time.timeScale = .25f;
            for (int b = 1; b <= 3; b++) { yield return new WaitForSeconds(.03f); yield return Shot(directory, "run-before-0" + b, motor, 0); }   // the run before, to set beside the run after
            yield return Jump(directory, "jump-run", motor, new[] { .02f, .08f, .14f, .2f, .26f, .32f, .38f, .44f, .5f, .55f, .6f, .66f, .74f, .85f, 1.0f, 1.2f, 1.4f, 1.7f, 2.0f });
            // The run from in front and from behind, low and close on the feet (2026-10-10, Chris: "still lots of foot errors"): whether
            // the feet turn in or twist shows here, not from the side.
            Time.timeScale = .35f;
            foreach (var (view, at) in new[] { ("run-front", new Vector3(0, -.55f, 3.2f)), ("run-back", new Vector3(.3f, -.5f, -3.2f)) })
            {
                offset = at; yield return new WaitForSeconds(.2f);
                for (int k = 1; k <= 4; k++) { yield return new WaitForSeconds(.09f); yield return Shot(directory, view + "-0" + k, motor, 0); }
            }
            // The stride from the side, close on the feet, with foot IK on and then off (2026-10-10: the lifted foot's toes pulled up).
            var fig = motor.GetComponent<ActorVisual>()?.Figure;
            foreach (var (tag, ik) in new[] { ("stride-ik", true), ("stride-noik", false) })
            {
                fig?.SetFootIK(ik); offset = new Vector3(3.4f, -.55f, .2f); yield return new WaitForSeconds(.25f);
                for (int k = 1; k <= 6; k++) { yield return new WaitForSeconds(.07f); yield return Shot(directory, tag + "-0" + k, motor, 0); }
            }
            fig?.SetFootIK(true);
            // The same stride with a figure a stride ahead playing a clip as made: Kevin's own figure on his run (kevin-NN), then our
            // own Ranger outfit (the body's avatar, as the game builds a figure) on Unity's Starter Assets run and walk (unity-run-NN,
            // unity-walk-NN; Chris chose them, 2026-10-10, after Kevin's own figure ran on its toes too).
            if (fig != null)
            {
                var look = motor.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMaterial;
                string sex = fig.Female ? "Female" : "Male";
                var bodyAvatar = Resources.Load<GameObject>("Characters/Bodies/Superhero_" + sex + "_FullBody")?.GetComponent<Animator>()?.avatar;
                var outfit = Resources.Load<GameObject>("Characters/Outfits/" + sex + "_Ranger");
                var kevinModel = Resources.Load<GameObject>("Characters/Animations/KI/Models/" + (fig.Female ? "HumanF_Model" : "HumanM_Model"));
                var passes = new[] {
                    ("kevin", "Kevin's figure", kevinModel, (Avatar)null, ModelFigure.KiClip(fig.Female, "Run01_Forward"), true),
                    ("unity-run", "Our outfit on Unity's run", outfit, bodyAvatar, UnityClip("Locomotion--Run_N"), false),
                    ("unity-walk", "Our outfit on Unity's walk", outfit, bodyAvatar, UnityClip("Locomotion--Walk_N"), false) };
                foreach (var (tag, label, src, avatar, clip, tint) in passes)
                {
                    Beside(label, src, avatar, clip, look, tint); offset = new Vector3(3.4f, -.55f, .85f); yield return new WaitForSeconds(.25f);
                    for (int k = 1; k <= 8; k++) { yield return new WaitForSeconds(.06f); yield return Shot(directory, tag + "-0" + k, motor, 0); }
                }
                if (kevin != null) Destroy(kevin.gameObject);
                if (kevinGraph.IsValid()) kevinGraph.Destroy();
            }
            offset = new Vector3(4.8f, .1f, .4f);
            Time.timeScale = 1; EncounterInput.Scripted = null; AdventurerMotor.Running = false;
            yield return new WaitForSeconds(1);
            // The bow: the nearest boar or wolf, stunned where it stands, 13 m in front of the archer; seen from her left.
            EncounterEnemy mark = null; float near = float.MaxValue;
            foreach (var e in s.Enemies) if (e != null && e.actor.IsAlive && !e.Game) { float d = Vector3.Distance(e.transform.position, s.Player.transform.position); if (d < near) { near = d; mark = e; } }
            if (mark == null || s.Kit == null || !s.Kit.RangedAutoAttacks) { report.AppendLine("no bow shots: no mark, or not a Ranger"); Done(directory); yield break; }
            mark.actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 100000); mark.actor.Health.ApplyHealing(100000);
            mark.Apply("stun", s.Player, 120);
            var toMark = mark.transform.position - s.Player.transform.position; toMark.y = 0;
            var stand = mark.transform.position - toMark.normalized * 13; motor.Teleport(s.Zone.Ground(new Vector2(stand.x, stand.z), 1.1f));
            motor.SetView(Mathf.Atan2(toMark.x, toMark.z) * Mathf.Rad2Deg, 10, 5);
            s.Select(mark); s.BeginAutoAttack();
            offset = new Vector3(-3.8f, .2f, 1.6f);   // on her left, a little ahead: the bow arm toward the camera
            yield return new WaitForSeconds(2.5f);   // the bow comes out, an auto-shot or two
            Time.timeScale = .5f;
            float a0 = Time.time; s.UseAbility(1);   // Aimed Shot: drawn 1.5 s, then loosed
            int n = 1;
            foreach (var t in new[] { .15f, .5f, 1.2f, 1.52f, 1.56f, 1.6f, 1.7f, 1.85f, 2.1f, 2.5f, 3.0f })
            {
                while (Time.time - a0 < t) yield return null;
                yield return Shot(directory, "bow-" + (n < 10 ? "0" : "") + n++, motor, Time.time - a0);
            }
            // From behind, as it is played: the game's own camera.
            Destroy(side.gameObject); side = null;
            Time.timeScale = 1; yield return new WaitForSeconds(1);
            for (int i = 1; i <= 6; i++) { yield return new WaitForSeconds(.35f); yield return Shot(directory, "bow-back-0" + i, motor, Time.time - a0); }
            Done(directory);
        }
        void Done(string directory)
        {
            Time.timeScale = 1; EncounterInput.Scripted = null;
            File.WriteAllText(Path.Combine(directory, "motion.txt"), report.ToString());
            Debug.Log("MOTION_CAPTURE_DONE"); Application.Quit(0);
        }
    }
}
