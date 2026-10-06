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
            yield return new WaitForSeconds(90);   // their day gets going
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
