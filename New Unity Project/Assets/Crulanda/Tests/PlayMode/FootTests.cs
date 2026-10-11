#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Crulanda.Encounter;
using Crulanda.World;

namespace Crulanda.Tests
{
    /// <summary>
    /// The feet (2026-10-10, Chris: "still lots of foot errors"; "it should not have passed with all these errors"): the jump's tests
    /// measured its height and its time, never the feet, so a run that floated on pointed toes passed. Here, standing, walking, running
    /// and after a running jump, every foot that is down must meet the ground (its toes and ankle where they are standing, not hanging
    /// above it or sunk into it) and point the way the body goes, the left foot on the left and the right on the right.
    /// </summary>
    public class FootTests
    {
        string root; EncounterSession session; AdventurerMotor motor; ActorVisual figure; ZoneBuilder zone;
        void OnLoaded(Scene scene, LoadSceneMode mode) { var s = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); if (s != null) s.SaveDirectoryOverride = root; }
        [UnitySetUp] public IEnumerator Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "Crulanda-feet-" + Guid.NewGuid().ToString("N"));
            SceneManager.sceneLoaded += OnLoaded;
            yield return SceneManager.LoadSceneAsync("Oakhaven", LoadSceneMode.Single);
            for (int i = 0; i < 3; i++) yield return null;
            session = UnityEngine.Object.FindFirstObjectByType<EncounterSession>(); zone = ZoneBuilder.Active;
            motor = session.Player.GetComponent<AdventurerMotor>(); figure = session.Player.GetComponent<ActorVisual>();
            Assert.NotNull(figure.Figure, "the player is a modelled figure");
            // Batch runs draw nothing, and a figure's Animator culls its bones when it is not drawn (CullUpdateTransforms): the feet
            // stood still in one pose and every measure below read the same whatever the clips did (2026-10-10). Always animate here.
            figure.Figure.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // The longest straight leg of road, faced along.
            Vector2 from = Vector2.zero, to = Vector2.up; float best = 0;
            foreach (var r in zone.Zone.roads)
                for (int i = 0; i + 1 < r.points.Length; i++) { float d = Vector2.Distance(r.points[i], r.points[i + 1]); if (d > best) { best = d; from = r.points[i]; to = r.points[i + 1]; } }
            var dir = (to - from).normalized;
            motor.Teleport(zone.Ground(from + dir * best * .2f, 1.1f)); motor.SetView(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 12, 6);
            for (int i = 0; i < 60; i++) yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            EncounterInput.Scripted = null; AdventurerMotor.Running = false;
            SceneManager.sceneLoaded -= OnLoaded;
            var empty = SceneManager.CreateScene("Empty-" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync("Oakhaven");
            if (root != null && Directory.Exists(root)) Directory.Delete(root, true);
        }

        struct Foot { public float ankle, toe, yaw, side, tilt; }
        /// <summary>A foot now: its ankle's and toes' heights above the ground under each, how far it turns from the body's heading
        /// (degrees, from the ankle to the toes), and which side of the body it is on (+ right).</summary>
        Foot Read(bool left)
        {
            var a = figure.Figure.Animator;
            var ankle = a.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
            var toe = a.GetBoneTransform(left ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
            Assert.NotNull(ankle); Assert.NotNull(toe, "the figure has toe bones");
            float G(Vector3 p) { return p.y - GroundUnder(p); }
            var along = toe.position - ankle.position; along.y = 0; var fwd = motor.transform.forward; fwd.y = 0;
            float yaw = along.sqrMagnitude > 1e-6f ? Vector3.Angle(along, fwd) : 0;
            var d = toe.position - ankle.position; float tilt = Mathf.Atan2(-d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;   // the ball below the ankle, degrees
            return new Foot { ankle = G(ankle.position), toe = G(toe.position), yaw = yaw, side = Vector3.Dot(ankle.position - motor.transform.position, motor.transform.right), tilt = tilt };
        }
        /// <summary>The visible ground under a point: the first collider a ray down from above meets that is not the player's own (the
        /// ground mesh, a bridge deck); the zone's height function where there is none.</summary>
        float GroundUnder(Vector3 p)
        {
            var hits = Physics.RaycastAll(new Vector3(p.x, p.y + 1.5f, p.z), Vector3.down, 6, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.MinValue;
            foreach (var h in hits) if (!h.collider.transform.IsChildOf(motor.transform) && h.point.y < p.y + 1.4f && h.point.y > best) best = h.point.y;
            return best > float.MinValue ? best : zone.Ground(new Vector2(p.x, p.z)).y;
        }
        /// <summary>Samples both feet for `seconds` (every frame), as (time, left, right).</summary>
        IEnumerator Sample(float seconds, List<(float t, Foot l, Foot r)> into)
        {
            float end = Time.time + seconds;
            while (Time.time < end) { yield return null; into.Add((Time.time, Read(true), Read(false))); }
        }
        static string F(float v) { return v.ToString("0.000"); }
        /// <summary>The checks over a stretch of moving: in every half second each foot comes down to where it stands (its lowest toe and
        /// ankle within 5 cm of standing, never 5 cm into the ground), and while it is down it points the way the body goes, on its own side.</summary>
        void Check(string what, List<(float t, Foot l, Foot r)> run, Foot standL, Foot standR, List<string> problems)
        {
            if (run.Count == 0) { problems.Add(what + ": no frames"); return; }
            float t0 = run[0].t;
            for (float w = t0; w + .5f <= run[run.Count - 1].t; w += .5f)
            {
                var win = run.Where(x => x.t >= w && x.t < w + .5f).ToList(); if (win.Count < 5) continue;
                foreach (var (name, stand, pick) in new[] { ("left", standL, (Func<(float t, Foot l, Foot r), Foot>)(x => x.l)), ("right", standR, x => x.r) })
                {
                    float lowToe = win.Min(x => pick(x).toe), lowAnkle = win.Min(x => pick(x).ankle);
                    if (lowToe > stand.toe + .05f) problems.Add(what + " " + name + " foot at " + F(w - t0) + "s: its toes never come down (lowest " + F(lowToe) + " m, standing " + F(stand.toe) + ")");
                    if (lowAnkle > stand.ankle + .035f) problems.Add(what + " " + name + " foot at " + F(w - t0) + "s: its heel never comes down, on its toes (ankle lowest " + F(lowAnkle) + " m, standing " + F(stand.ankle) + ")");
                    if (lowToe < stand.toe - .05f) problems.Add(what + " " + name + " foot at " + F(w - t0) + "s: sunk into the ground (toes " + F(lowToe) + " m, standing " + F(stand.toe) + ")");
                    foreach (var x in win)
                    {
                        var up = pick(x);
                        if (up.ankle > stand.ankle + .15f && up.tilt < stand.tilt - 15)
                        { problems.Add(what + " " + name + " foot at " + F(x.t - t0) + "s: lifted with its toes pulled up toward the shin (tilt " + up.tilt.ToString("0") + ", standing " + stand.tilt.ToString("0") + " degrees)"); break; }
                    }
                    foreach (var x in win)
                    {
                        var f = pick(x); if (f.toe > stand.toe + .04f) continue;   // only a foot that is down
                        if (f.ankle < stand.ankle + .05f && f.tilt > stand.tilt + 15) { problems.Add(what + " " + name + " foot at " + F(x.t - t0) + "s: down on its toes, pointed " + (f.tilt - stand.tilt).ToString("0") + " degrees more than standing (tilt " + f.tilt.ToString("0") + ")"); break; }
                        if (f.yaw > 30) { problems.Add(what + " " + name + " foot at " + F(x.t - t0) + "s: down but turned " + f.yaw.ToString("0") + " degrees from the way it goes"); break; }
                        if (name == "left" ? f.side > .02f : f.side < -.02f) { problems.Add(what + " " + name + " foot at " + F(x.t - t0) + "s: down on the wrong side of the body (crossed)"); break; }
                        if (Mathf.Abs(f.side) < .06f) { problems.Add(what + " " + name + " foot at " + F(x.t - t0) + "s: down on the middle line (" + F(f.side) + " m out): the feet land one in front of the other"); break; }
                    }
                }
            }
        }

        [UnityTest] public IEnumerator Feet_meet_the_ground_and_point_ahead_standing_walking_running_and_landing()
        {
            var problems = new List<string>();
            var stand = new List<(float t, Foot l, Foot r)>(); yield return Sample(1, stand);
            var standL = stand.Last().l; var standR = stand.Last().r;
            problems.Add("(standing: left toe " + F(standL.toe) + " ankle " + F(standL.ankle) + " yaw " + standL.yaw.ToString("0") + " tilt " + standL.tilt.ToString("0") + "; right toe " + F(standR.toe) + " ankle " + F(standR.ankle) + " yaw " + standR.yaw.ToString("0") + " tilt " + standR.tilt.ToString("0") + ")");
            var cc = session.Player.GetComponent<CharacterController>(); var me = motor.transform.position;
            float capsule = cc != null ? me.y + cc.center.y - cc.height / 2 - GroundUnder(me) : float.NaN;
            var model = figure.Figure.Animator.transform;
            problems[problems.Count - 1] += " (capsule bottom " + F(capsule) + " above the ground, skin " + (cc != null ? F(cc.skinWidth) : "?") + "; the model's root " + F(model.position.y - GroundUnder(me)) + " above it)";
            int header = problems.Count;
            foreach (var (name, f) in new[] { ("left", standL), ("right", standR) })
            {
                if (f.toe > .07f) problems.Add("standing, the " + name + " toes are " + F(f.toe) + " m above the ground (floating)");
                if (f.toe < -.03f) problems.Add("standing, the " + name + " toes are " + F(-f.toe) + " m into the ground");
                if (f.yaw > 30) problems.Add("standing, the " + name + " foot is turned " + f.yaw.ToString("0") + " degrees");
            }
            // Walking, then running, then a running jump and the run after it.
            EncounterInput.Scripted = new Vector2(0, 1); AdventurerMotor.Running = false;
            for (int i = 0; i < 40; i++) yield return null;
            var walk = new List<(float t, Foot l, Foot r)>(); yield return Sample(2, walk); Check("walking", walk, standL, standR, problems);
            AdventurerMotor.Running = true; for (int i = 0; i < 40; i++) yield return null;
            var run = new List<(float t, Foot l, Foot r)>(); yield return Sample(2, run); Check("running", run, standL, standR, problems);
            motor.Jump(); float deadline = Time.time + 2; while (Time.time < deadline && !motor.Airborne) yield return null;
            while (Time.time < deadline && motor.Airborne) yield return null;
            var after = new List<(float t, Foot l, Foot r)>(); yield return Sample(1.5f, after); Check("running after a jump", after, standL, standR, problems);
            EncounterInput.Scripted = null;
            // the moving checks measure against standing, so standing's own problems count too; the first line is the standing numbers
            Assert.AreEqual(header, problems.Count, string.Join("\n", problems.Distinct().Take(30)));
        }
    }
}
#endif
