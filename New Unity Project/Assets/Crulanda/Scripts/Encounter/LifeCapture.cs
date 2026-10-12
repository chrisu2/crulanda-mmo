using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Shots of the sims living their day (Phase 5.3a), for looking at what the tests cannot see. Run the player windowed, on a temp
    /// save: Crulanda.exe --crulanda-life-capture &lt;folder&gt;. The sims are alive here (SimPopulation.LifeOverride) and all
    /// online; after a minute and a half of their day, the camera stands behind one hunting (01, 02), one gathering (03), one going
    /// to or at the inn (04), one about the village (05), and a wide view over the green (06). LIFE_CAPTURE_DONE in the log.
    /// </summary>
    public sealed class LifeCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-life-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }

        /// <summary>The capture camera at <paramref name="at"/> on the subject. Tight (note 119): level with the hips, which it looks at,
        /// so head and feet are both in frame at 2 m.</summary>
        static void Aim(Camera cam, Vector3 at, Transform subject, Animator anim, bool tight, float look)
        {
            var hips = tight && anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (hips == null) { cam.transform.position = at; cam.transform.LookAt(subject.position + Vector3.up * look); return; }
            var target = hips.position + Vector3.up * .02f; var flat = at - target; flat.y = 0;
            cam.transform.position = target + flat.normalized * 2f + Vector3.up * .08f; cam.transform.LookAt(target);
        }        /// <summary>Note 150 (Chris, 2026-10-12: he needs to see farmers digging and ploughing): a farmer hoeing in a field from the side
        /// at the top of the stroke and at the strike, eight frames through one stroke, then the plough team from the side and three-quarters;
        /// close (2.6 m), HUD hidden, what is in the way hidden. Named note150-*.png.</summary>
        IEnumerator FieldWork(EncounterSession s, Camera cam, System.Collections.Generic.List<Villager> holders, string directory)
        {
            EncounterHud.Hidden = true;
            var field = Crulanda.World.CropField.All.Count > 0 ? Crulanda.World.CropField.All[0] : null;
            Villager farmer = null; foreach (var v in holders) if (v.Role == "farmer") { farmer = v; break; }
            if (field != null && farmer != null)
            {
                var fv = farmer.GetComponent<ActorVisual>(); var anim = farmer.GetComponentInChildren<Animator>(); if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var agent = farmer.GetComponent<UnityEngine.AI.NavMeshAgent>(); farmer.enabled = false; if (farmer.Carrying) farmer.PutDownLoad();
                if (agent != null && agent.isOnNavMesh) { agent.Warp(field.transform.position); agent.isStopped = true; }
                farmer.transform.rotation = Quaternion.LookRotation(field.transform.forward);
                fv.Pose = ActorPose.Work; var ft = farmer.transform;
                for (float t = 0; t < 2.5f; t += Time.deltaTime) yield return null;
                System.Collections.Generic.List<Renderer> hidden;
                var at = CaptureFraming.ClearSpot(ft.position, ft.eulerAngles.y + 90, 2.6f, 1.1f, 40, ft, out hidden); CaptureFraming.Hide(hidden);
                Aim(cam, at, ft, anim, true, .9f);
                foreach (var (phase, name) in new[] { (.44f, "note150-hoeing-side-raised.png"), (.63f, "note150-hoeing-side-strike.png") })
                {
                    while (Mathf.Abs(fv.StrokePhase - phase) > .02f) yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, name)); yield return null; yield return null;
                }
                while (fv.StrokePhase > .05f) yield return null;
                for (int i = 0; i < 8; i++)
                {
                    float want = i / 8f + .02f; while (Mathf.Abs(fv.StrokePhase - want) > .02f) yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, "note150-stroke-seq-" + (i + 1) + ".png")); yield return null;
                }
                CaptureFraming.Show(hidden); Debug.Log("Life capture: note150 hoeing " + farmer.Name + ", strokes " + fv.StrokeCount + ", strike gap " + fv.StrikeGap.ToString("0.00"));
                fv.Pose = ActorPose.None; farmer.enabled = true; if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
            }
            PlowTeam.Forced = true;
            for (int i = 0; i < 60 && (PlowTeam.Active == null || !PlowTeam.Active.Out); i++) { if (PlowTeam.Active != null) PlowTeam.Active.CheckNow(); yield return null; }
            var team = PlowTeam.Active;
            if (team != null && team.Out)
            {
                for (float t = 0; t < 4; t += Time.deltaTime) yield return null;   // on a straight row
                foreach (var (angle, name) in new[] { (90f, "note150-plough-side.png"), (40f, "note150-plough-three-quarter.png") })
                {
                    System.Collections.Generic.List<Renderer> hidden = null;
                    for (float t = 0; t < .8f; t += Time.deltaTime)
                    {
                        CaptureFraming.Show(hidden);
                        var mid = (team.Horse.position + team.Ploughman.transform.position) / 2 - Vector3.up * .0f;
                        var yaw = Quaternion.LookRotation(team.Horse.forward).eulerAngles.y + angle;
                        var at = CaptureFraming.ClearSpot(mid, yaw, 5.2f, 1.6f, 40, team.Ploughman.transform, out hidden, team.Horse);
                        // Anything else standing in the corridor from the camera to the team, or a passer-by near it, is hidden too.
                        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        {
                            if (r == null || !r.enabled || r.transform.IsChildOf(team.transform) || r is ParticleSystemRenderer || r is TrailRenderer) continue;
                            var c = r.bounds.center; var ab = mid - at; float u = Mathf.Clamp01(Vector3.Dot(c - at, ab) / ab.sqrMagnitude);
                            bool inWay = Vector3.Distance(c, at + ab * u) < 1.8f + r.bounds.extents.magnitude * .5f && u < .97f && r.bounds.size.magnitude < 12;
                            bool passer = r.GetComponentInParent<Villager>() != null && Vector3.Distance(c, mid) < 7;
                            if (inWay || passer) hidden.Add(r);
                        }
                        CaptureFraming.Hide(hidden);
                        cam.transform.position = at; cam.transform.LookAt(mid + Vector3.up * .8f);
                        yield return null;
                    }
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, name)); yield return new WaitForSeconds(.3f);
                    CaptureFraming.Show(hidden); Debug.Log("Life capture: " + name);
                }
            }
            else Debug.Log("Life capture: no plough team");
            PlowTeam.Forced = false;
        }
        public IEnumerator Run(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            Crulanda.World.WorldClock.Hour = 11;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 100000); s.Player.Health.ApplyHealing(100000);
            var pop = SimPopulation.Active;
            if (pop == null) { Debug.LogError("Life capture: no sims."); Application.Quit(1); yield break; }
            foreach (var x in pop.World.sims) if (x.homeZone == s.ZoneId) { x.zone = s.ZoneId; x.onlineFrom = 0; x.onlineHours = 24; }
            pop.Refresh();
            yield return new WaitForSeconds(Array.IndexOf(Environment.GetCommandLineArgs(), "--crulanda-farmers-only") >= 0 ? 8 : 90);   // their day gets going
            var motor = s.Player.GetComponent<AdventurerMotor>();
            int n = 1;
            foreach (var (doing, name) in new[] { (SimFigure.Doing.Hunt, "hunting"), (SimFigure.Doing.Hunt, "hunting-2"), (SimFigure.Doing.Gather, "gathering"), (SimFigure.Doing.Inn, "inn"), (SimFigure.Doing.Loiter, "about") })
            {
                var f = pop.Figures.Find(x => x != null && x.Activity == doing && !x.Hidden && (n != 2 || x != pop.Figures.Find(y => y != null && y.Activity == doing)));
                if (f == null) f = pop.Figures.Find(x => x != null && x.Activity == doing);
                string file = (n < 10 ? "0" : "") + n + "-" + name + ".png"; n++;
                if (f == null) { Debug.Log("Life capture: nobody " + name); continue; }
                var t = f.Quarry != null ? f.Quarry.transform : f.transform;
                var back = (f.transform.position - t.position); back.y = 0; if (back.sqrMagnitude < .1f) back = -f.transform.forward;
                motor.Teleport(f.transform.position + back.normalized * 5 + Vector3.right * 2 + Vector3.up * .3f);
                motor.SetView(Quaternion.LookRotation(t.position - (f.transform.position + back.normalized * 5)).eulerAngles.y, 14, 7);
                yield return new WaitForSeconds(2.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, file)); yield return new WaitForSeconds(.5f);
                Debug.Log("Life capture: " + file + " " + f.sim.name + " " + f.Doings);
            }
            // Farmers and the others who carry a trade tool (playtest notes 95, 109 and 119: "farmers hoes still going thru the arm and
            // not held like a staff"; Chris on the step-2 frames: "can't see the farmers"): up to three, each close up (3 m) from the
            // side, the front and the back while walking, then standing, then at work, on the capture's own camera with nothing between
            // (CaptureFraming: a clear bearing, else what is in the way hidden for the shot). Named farmer-<name>-<what>.png.
            var holders = new System.Collections.Generic.List<Villager>();
            foreach (var v in UnityEngine.Object.FindObjectsByType<Villager>(FindObjectsSortMode.None))
            {
                if (!v.gameObject.activeInHierarchy) continue;
                bool tool = false; foreach (var tr in v.GetComponentsInChildren<Transform>(true)) if (tr.name == "Trade tool") { tool = true; break; }
                if (tool) holders.Add(v);
            }
            holders.Sort((a, b) => ((b.Role == "farmer" ? 2 : 0) + (b.Carrying ? 0 : 1)).CompareTo((a.Role == "farmer" ? 2 : 0) + (a.Carrying ? 0 : 1)));   // farmers first, those with free hands first
            if (holders.Count == 0) Debug.Log("Life capture: nobody carries a trade tool");
            motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(0, 1, 60)); yield return null;   // the player well out of these shots
            // --crulanda-farmers-only (note 119, Chris 2026-10-12): one farmer, close (2 m, head to feet in frame), HUD hidden, from the
            // side, the front and three-quarters, walking then standing.
            bool tight = Array.IndexOf(Environment.GetCommandLineArgs(), "--crulanda-farmers-only") >= 0;
            float dist = tight ? 2f : 3.6f, high = tight ? 1.05f : 1.7f, look = tight ? .9f : .55f; int most = tight ? 1 : 3;
            bool hudWas = EncounterHud.Hidden; if (tight) EncounterHud.Hidden = true;
            var walkShots = tight ? new[] { (90f, "walking-side"), (0f, "walking-front"), (45f, "walking-three-quarter") } : new[] { (90f, "walking-side"), (0f, "walking-front"), (180f, "walking-back") };
            var cam = CaptureFraming.Own(); int taken = 0;
            foreach (var farmer in holders)
            {
                if (taken >= most) break; taken++;
                var fv = farmer.GetComponent<ActorVisual>(); var ft = farmer.transform; var anim = farmer.GetComponentInChildren<Animator>();
                if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                string who = farmer.Name.Replace(' ', '-').ToLowerInvariant(); string tag = "farmer-" + who + "-" + farmer.Role;
                // Walking: follow alongside for a moment, then the shot.
                farmer.enabled = true;
                foreach (var (angle, what) in walkShots)
                {
                    System.Collections.Generic.List<Renderer> hidden = null;
                    for (float t = 0; t < 1.2f; t += Time.deltaTime)
                    {
                        if (farmer.Carrying) farmer.PutDownLoad();   // a load puts the tool away: hands free for these frames
                        CaptureFraming.Show(hidden);
                        var at = CaptureFraming.ClearSpot(ft.position, ft.eulerAngles.y + angle, dist, high, 60, ft, out hidden);
                        CaptureFraming.Hide(hidden);
                        Aim(cam, at, ft, anim, tight, look);
                        yield return null;
                    }
                    string file = tag + "-" + what + ".png";
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, file)); yield return new WaitForSeconds(.4f);
                    CaptureFraming.Show(hidden);
                    Debug.Log("Life capture: " + file + " " + farmer.Name + " (" + farmer.Role + "), moving " + (farmer.GetComponent<UnityEngine.AI.NavMeshAgent>() != null ? farmer.GetComponent<UnityEngine.AI.NavMeshAgent>().velocity.magnitude.ToString("0.0") : "?") + " m/s");
                }
                // Standing, then at work: stopped where it is.
                farmer.enabled = false; var agent = farmer.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                foreach (var (angle, what, pose) in tight ? new[] { (90f, "standing-side", ActorPose.None), (0f, "standing-front", ActorPose.None), (45f, "standing-three-quarter", ActorPose.None) } : new[] { (90f, "standing-side", ActorPose.Talk), (0f, "standing-front", ActorPose.Talk), (180f, "standing-back", ActorPose.Talk), (60f, "at-work", ActorPose.Work), (-90f, "at-work-side", ActorPose.Work) })
                {
                    if (fv != null) fv.Pose = pose;
                    if (farmer.Carrying) farmer.PutDownLoad();
                    var at = CaptureFraming.ClearSpot(ft.position, ft.eulerAngles.y + angle, dist, high, 60, ft, out var hidden);
                    CaptureFraming.Hide(hidden);
                    Aim(cam, at, ft, anim, tight, look);
                    yield return new WaitForSeconds(1.5f);
                    string file = tag + "-" + what + ".png";
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, file)); yield return new WaitForSeconds(.4f);
                    CaptureFraming.Show(hidden);
                    Debug.Log("Life capture: " + file + " " + farmer.Name + " (" + farmer.Role + "), " + hidden.Count + " things hidden");
                }
                if (fv != null) fv.Pose = ActorPose.None;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false; farmer.enabled = true;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--crulanda-field-work") >= 0) yield return FieldWork(s, cam, holders, directory);
            CaptureFraming.Release(); EncounterHud.Hidden = hudWas;
            motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(18, 1, -18)); motor.SetView(-45, 22, 30);
            yield return new WaitForSeconds(2.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "06-wide.png")); yield return new WaitForSeconds(.5f);
            var report = "";
            foreach (var f in pop.Figures) if (f != null) report += f.sim.name + " (" + SimRoster.ClassName(f.sim.classId) + " " + f.sim.level + "): " + f.Doings + ", kills " + f.Kills + "\n";
            File.WriteAllText(Path.Combine(directory, "sims.txt"), report);
            Debug.Log("LIFE_CAPTURE_DONE"); Application.Quit(0);
        }
    }
}
