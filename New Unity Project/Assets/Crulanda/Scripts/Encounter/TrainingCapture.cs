using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Frames of Oakhaven's training field (EncounterSession.Training) for Chris to look at before anything is published. Run the player
    /// windowed, as a Cleric (it strikes and it heals), on a temp save:
    /// Crulanda.exe --crulanda-training-capture &lt;folder&gt; --crulanda-class class.cleric --crulanda-zone zone.oakhaven --crulanda-temp-save.
    /// Writes tf-01..tf-06 and training.txt (what each frame should show, and the numbers behind it); TRAINING_CAPTURE_DONE in the log.
    /// </summary>
    public sealed class TrainingCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-training-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        string dir; readonly System.Text.StringBuilder report = new System.Text.StringBuilder(); EncounterSession session; float keptScale = -1;

        IEnumerator Shot(string name, string what, Transform also, bool clear = true)
        {
            yield return new WaitForSecondsRealtime(.6f);
            var player = session.Player.transform; var motor = session.Player.GetComponent<AdventurerMotor>(); var cam = Camera.main;
            System.Collections.Generic.List<Renderer> InWay()
            {
                var l = CaptureFraming.InTheWay(cam.transform.position, player.position + Vector3.up * 1.0f, player, also);
                if (also != null) l.AddRange(CaptureFraming.InTheWay(cam.transform.position, also.position + Vector3.up * .3f, also, player));
                return l;
            }
            var blocked = clear ? InWay() : new System.Collections.Generic.List<Renderer>();   // the yard's overview hides nothing: its fence is what to see
            for (int k = 0; k < 8 && blocked.Count > 0 && motor.enabled; k++)
            {
                var e = cam.transform.eulerAngles; motor.SetView(e.y + 45, e.x > 180 ? e.x - 360 : e.x, Vector3.Distance(cam.transform.position, player.position));
                yield return null; yield return null; blocked = InWay();
            }
            CaptureFraming.Hide(blocked);
            yield return new WaitForSecondsRealtime(.1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png")); report.AppendLine(name + ".png: " + what);
            Debug.Log("Training capture: " + name + " " + what); yield return new WaitForSecondsRealtime(.5f);
            CaptureFraming.Show(blocked);
        }
        /// <summary>Uses the bar for a while: the abilities whose effect is <paramref name="want"/>, the first ready one each moment.</summary>
        IEnumerator Use(float seconds, Func<AbilityDefinition, bool> want)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                if (!session.PlayerCasting)
                    for (int i = 0; i < session.Kit.ActionCount; i++)
                    {
                        var a = session.Kit.ActionAt(i); if (a == null || session.Kit.ActionLockLabel(i) != null || !want(a)) continue;
                        if (session.UseAbility(i)) break;
                    }
                yield return null;
            }
        }
        static float Yaw(Vector3 from, Vector3 to) { var d = to - from; d.y = 0; return d.sqrMagnitude < .001f ? 0 : Quaternion.LookRotation(d).eulerAngles.y; }

        public IEnumerator Run(EncounterSession s, string directory)
        {
            dir = directory; Directory.CreateDirectory(dir); session = s;
            // The HUD in frame at 1440x900, as the group run does (the 13:27 frames: the target frame cut off at the left, the player frame
            // gone, under the player's own saved layout); the player's UI scale is put back at the end.
            keptScale = EncounterHud.UiScale; EncounterHud.UiScale = 1; EncounterHud.DefaultPanels = true;
            Crulanda.World.WorldClock.Hour = 11;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            yield return new WaitForSeconds(3);
            var motor = s.Player.GetComponent<AdventurerMotor>(); s.MeterOpen = true;
            var dummy = s.TrainingDummy; var mend = s.MendingDummy;
            report.AppendLine("Class " + s.ClassDef.displayName + ", level " + s.Progress.Level + "; training dummy " + (dummy != null) + ", mending dummy " + (mend != null));
            if (dummy == null || mend == null) { Finish(); yield break; }

            // 1. The field: both dummies from a few metres off.
            var mid = (dummy.transform.position + mend.transform.position) / 2; var stand = mid + Quaternion.Euler(0, Yaw(mid, s.Zone.Ground(Vector2.zero)), 0) * Vector3.forward * 13;
            motor.Teleport(stand + Vector3.up * .3f); motor.SetView(Yaw(stand, mid), 32, 14); yield return new WaitForSeconds(1);
            // A whole orc in play (2026-10-14, Chris: "the only thing on orc that move is belt, staff and teeth"): what turns in two seconds of
            // its idle, and what its animator is doing.
            var orcVis = s.Player.GetComponent<ActorVisual>(); var bm = orcVis != null ? orcVis.BeastModel : null;
            if (bm != null)
            {
                var bones = bm.Model.GetComponentsInChildren<Transform>(); var was = bones.Select(t => t.localRotation).ToArray();
                // And whether it holds its staff (2026-10-14, Chris: its arms at its sides, its staff floating by the ground): the Orc Shaman's
                // staff (a rigid mesh on a control of its own, not on the hand) in its right hand's frame, frame by frame; held, it barely shifts.
                var hand = bones.FirstOrDefault(t => t.name == "Character1_RightHand"); var staff = bm.Renderers.FirstOrDefault(r => r != null && r.name == "polySurface238");
                var staffMesh = staff != null ? staff.GetComponent<MeshFilter>()?.sharedMesh : null; Vector3 centre = Vector3.zero;
                if (staffMesh != null && staffMesh.isReadable && staffMesh.vertexCount > 0) { foreach (var p in staffMesh.vertices) centre += p; centre /= staffMesh.vertexCount; } else staff = null;
                var held = new System.Collections.Generic.List<Vector3>();
                for (float t = 0; t < 2; t += Time.deltaTime) { yield return null; if (hand != null && staff != null) held.Add(hand.InverseTransformPoint(staff.transform.TransformPoint(centre))); }
                var turned = bones.Where((t, i) => Quaternion.Angle(t.localRotation, was[i]) > 2).Select(t => t.name).ToList();
                string holds = "";
                if (held.Count > 0)
                {
                    var mean = Vector3.zero; foreach (var o in held) mean += o; mean /= held.Count;
                    float shift = held.Max(o => hand.TransformVector(o - mean).magnitude), away = hand.TransformVector(mean).magnitude;
                    holds = "; its staff in its right hand's frame over those 2 s (" + held.Count + " frames): " + away.ToString("0.000") + " m from the hand bone, shifting at most " + shift.ToString("0.000") + " m (held: a few cm)";
                }
                report.AppendLine("orc in play: " + bm.Kind + ", animator on " + bm.Animator.enabled + ", culling " + bm.Animator.cullingMode + ", avatar " + (bm.Animator.avatar != null ? bm.Animator.avatar.name + " valid " + bm.Animator.avatar.isValid + " human " + bm.Animator.avatar.isHuman : "none")
                    + ", renderers seen " + bm.Renderers.Count(r => r != null && r.isVisible) + " of " + bm.Renderers.Count + ", motion " + bm.Moving + "; clips: idle " + bm.ClipLength("idle").ToString("0.000") + " s, walk " + bm.ClipLength("walk").ToString("0.000")
                    + " s, cast " + bm.ClipLength("cast").ToString("0.000") + " s, death " + bm.ClipLength("death").ToString("0.000") + " s; " + turned.Count + " of " + bones.Length + " bones turned in 2 s: " + string.Join(", ", turned.Take(24)) + holds);
            }
            yield return Shot("tf-01-yard", "the training yard east of Oakhaven from its gate: the fence, the tents, the banners, the dummies down the left, the butts down the right, the healer's corner, the training and mending dummies", dummy.transform, false);

            // 2. Striking the training dummy, the meter timing it.
            var front = dummy.transform.position + dummy.transform.forward * 3;
            motor.Teleport(front + Vector3.up * .3f); motor.SetView(Yaw(front, dummy.transform.position) + 25, 12, 6); s.Select(dummy); s.Meter.Reset();
            yield return Use(10, a => a.effect == AbilityEffect.Damage);
            var pool = dummy.actor.Health.Pool; float dps = 0; string mine = s.Player.EntityId.Value;
            foreach (var r in s.Meter.Rows) if (r.id == mine) dps = r.Dps(s.Meter.CombatSeconds);
            yield return Shot("tf-02-training-dummy-struck", "the training dummy targeted and struck: " + pool.Current + " of " + pool.Max + "; the meter " + dps.ToString("0") +
                " a second over " + s.Meter.CombatSeconds.ToString("0") + " s; in a fight " + s.InCombat + "; your health " + s.Player.Health.Pool.Current + " of " + s.Player.Health.Pool.Max + " (it never strikes back)", dummy.transform);
            yield return new WaitForSeconds(EncounterSession.TrainingQuiet + 2);
            yield return Shot("tf-03-training-dummy-whole", "five quiet seconds on: whole again, " + pool.Current + " of " + pool.Max, dummy.transform);

            // 4. The mending dummy, losing health while you are near.
            s.Select(null); var by = mend.transform.position + mend.transform.forward * 3;
            motor.Teleport(by + Vector3.up * .3f); motor.SetView(Yaw(by, mend.transform.position) + 25, 12, 6); s.SelectMending(); s.Meter.Reset();
            yield return new WaitForSeconds(5);
            var mp = mend.Health.Pool;
            yield return Shot("tf-04-mending-dummy-hurt", "the mending dummy selected (its friend frame): " + mp.Current + " of " + mp.Max + " after five seconds near it", mend.transform);

            // 5. Healing it: every heal on the bar, the dummy counted as one of your party.
            yield return Use(10, a => a.effect == AbilityEffect.Heal);
            float hps = 0; foreach (var r in s.Meter.Rows) if (r.id == mine) hps = r.Hps(s.Meter.CombatSeconds);
            yield return Shot("tf-05-mending-dummy-healed", "healed: " + mp.Current + " of " + mp.Max + "; the meter " + hps.ToString("0") + " healing a second; in a fight " + s.InCombat, mend.transform);
            // 6. Sims at practice (Chris, 2026-10-13: "have sims wonder into the fighting arena (dummies) to practice and improve their dps").
            // They go of their own accord; here up to four of the village's are sent to the yard's marks, so the frame does not wait on
            // their choices, and it is taken from the gate once two are at work.
            var pop = SimPopulation.Active; int sent = 0;
            if (pop != null)
                foreach (var f in pop.Figures.ToList())
                {
                    if (f == null || f.Hidden || sent >= 4) continue;
                    var m = f.FindMark(400); if (m == null) continue;
                    f.Begin(SimFigure.Doing.Practice, mark: m); sent++;
                }
            int AtWork() { return pop == null ? 0 : pop.Figures.Count(f => f != null && f.Activity == SimFigure.Doing.Practice && f.Mark != null && Vector3.Distance(f.transform.position, f.Mark.stand) < 1.6f); }
            for (float w = 0; w < 75 && AtWork() < Mathf.Min(2, sent); w += 1) yield return new WaitForSeconds(1);
            motor.Teleport(stand + Vector3.up * .3f); motor.SetView(Yaw(stand, mid), 32, 14); yield return new WaitForSeconds(5);
            yield return Shot("tf-06-sims-practising", sent + " sims sent to the yard; " + AtWork() + " at a dummy or a butt, striking or shooting (grey numbers over it)", dummy.transform);
            Finish();
        }
        void Finish()
        {
            if (keptScale > 0) EncounterHud.UiScale = keptScale;
            File.WriteAllText(Path.Combine(dir, "training.txt"), report.ToString());
            Debug.Log("TRAINING_CAPTURE_DONE"); Application.Quit(0);
        }
    }
}
