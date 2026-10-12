using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Frames of the fixes from Chris's play of the 00:33 build that a test measures but nobody has looked at (notes 120-124 and the
    /// tester's 116), for Chris to judge before anything is published. Run the player windowed, as a Ranger in Oakhaven, on a temp save:
    /// Crulanda.exe --crulanda-fixes-capture &lt;folder&gt; --crulanda-class class.ranger --crulanda-zone zone.oakhaven --crulanda-temp-save.
    /// Writes fix-01..fix-10 and fixes.txt (what each frame should show, and the numbers behind it); FIXES_CAPTURE_DONE in the log.
    /// </summary>
    public sealed class FixesCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-fixes-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        string dir; readonly System.Text.StringBuilder report = new System.Text.StringBuilder(); EncounterSession session;
        /// <summary>A frame with the player (and <paramref name="also"/>, the thing to look at) in view: the orbit turned round the player
        /// until nothing stands between the camera and them (Chris on the step-2 frames: "a tree blocks the view", "my character isn't
        /// visible"), what is still in the way hidden for the shot only.</summary>
        IEnumerator Shot(string name, string what, Transform also = null, bool clear = true)
        {
            // Real time: the menu's pause stops the game clock (time scale 0), and a game-time wait would never end.
            yield return new WaitForSecondsRealtime(.6f);
            var player = session.Player.transform; var motor = session.Player.GetComponent<AdventurerMotor>(); var cam = Camera.main;
            System.Collections.Generic.List<Renderer> InWay()
            {
                var l = CaptureFraming.InTheWay(cam.transform.position, player.position + Vector3.up * 1.0f, player, also);
                if (also != null) l.AddRange(CaptureFraming.InTheWay(cam.transform.position, also.position + Vector3.up * .3f, also, player));
                return l;
            }
            var blocked = clear ? InWay() : new System.Collections.Generic.List<Renderer>();   // inside the inn nothing is hidden: its walls are what to look at
            for (int k = 0; k < 8 && blocked.Count > 0 && motor.enabled; k++)
            {
                var e = cam.transform.eulerAngles; motor.SetView(e.y + 45, e.x > 180 ? e.x - 360 : e.x, Vector3.Distance(cam.transform.position, player.position));
                yield return null; yield return null; blocked = InWay();
            }
            CaptureFraming.Hide(blocked);
            yield return new WaitForSecondsRealtime(.1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png")); report.AppendLine(name + ".png: " + what + (blocked.Count > 0 ? " (" + blocked.Count + " things hidden for the shot)" : ""));
            Debug.Log("Fixes capture: " + name + " " + what); yield return new WaitForSecondsRealtime(.5f);
            CaptureFraming.Show(blocked);
        }

        public IEnumerator Run(EncounterSession s, string directory)
        {
            dir = directory; Directory.CreateDirectory(dir); session = s;
            Crulanda.World.WorldClock.Hour = 11;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 2000); s.Player.Health.ApplyHealing(100000);
            s.Progress.experience = EncounterProgress.XpForLevel(10); s.Player.SetLevel(10);
            yield return new WaitForSeconds(3);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            report.AppendLine("Screen: " + Screen.fullScreenMode + " " + Screen.width + "x" + Screen.height + " (this run is windowed on purpose; the menu's modes: "
                + string.Join(", ", EncounterHud.DisplayModes) + ", so no exclusive full screen; note 121 is measured by EncounterRulesTests, not shown here)");

            // Note 123, the Snare Trap on the ground at your feet.
            int trapSlot = -1; for (int i = 0; i < s.Kit.ActionCount; i++) if (s.Kit.ActionAt(i).id == "ranger.snare_trap") trapSlot = i;
            if (trapSlot < 0) report.AppendLine("no Snare Trap on the bar (not a Ranger?)");
            else
            {
                motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(12, 1, 12)); yield return new WaitForSeconds(1);
                bool set = s.UseAbility(trapSlot); yield return new WaitForSeconds(.5f);
                var trap = GameObject.Find("Snare trap");
                if (!set || trap == null) report.AppendLine("the Snare Trap was not set (" + set + ")");
                else
                {
                    var feet = s.Player.GetComponent<CharacterController>().bounds.min.y;
                    motor.SetView(s.Player.transform.eulerAngles.y + 90, 25, 4.5f);
                    yield return Shot("fix-01-trap-at-feet", "note 123: the trap lies on the ground at the Ranger's feet (" + (trap.transform.position.y - feet).ToString("0.00") + " m from the feet)", trap.transform);
                    motor.Teleport(trap.transform.position + Vector3.back * 3 + Vector3.up * .3f); motor.SetView(0, 20, 5);
                    yield return Shot("fix-02-trap-stepped-back", "note 123: the same trap from three metres, on the ground, not in the air", trap.transform);
                }
            }

            // Note 124, Esc stops the auto shot.
            var foe = s.Enemies.Find(x => x != null && x.actor.IsAlive && x.Camp);
            if (foe == null) report.AppendLine("no enemy for the auto shot");
            else
            {
                motor.Teleport(foe.transform.position + Vector3.back * 15 + Vector3.up * .2f); yield return null;
                motor.SetView(Quaternion.LookRotation(foe.transform.position - s.Player.transform.position).eulerAngles.y + 20, 14, 7);
                s.Select(foe); s.UseAbility(0); yield return new WaitForSeconds(2.2f);
                yield return Shot("fix-03-auto-shot-on", "note 124: the auto shot running; the bar reads: " + s.AutoAttackStatus + " (auto " + s.AutoAttack + ")", foe.transform);
                s.Escape(); bool stopped = !s.AutoAttack; bool kept = s.Target == foe;
                yield return Shot("fix-04-esc-stops-auto-shot", "note 124: one Esc: auto shot " + (stopped ? "stopped" : "STILL ON") + ", target " + (kept ? "kept" : "dropped") + ", menu " + (s.Paused ? "OPEN" : "shut"));
                s.Escape(); bool dropped = s.Target == null; s.Escape(); bool paused = s.Paused;
                yield return Shot("fix-05-esc-third-opens-menu", "note 124: Esc again drops the target (" + dropped + "), a third opens the menu (" + paused + ")");
                if (s.Paused) s.Resume();
                foreach (var e in s.Enemies) if (e != null) e.ResetFight();
                s.Player.Health.ApplyHealing(100000);
            }

            // Note 122, a Druid in the party mends you.
            var pop = SimPopulation.Active;
            if (pop == null) report.AppendLine("no sims for the Druid");
            else
            {
                motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(10, 1, -10)); yield return new WaitForSeconds(1);
                var one = pop.World.sims.FirstOrDefault(x => x.zone == s.ZoneId) ?? pop.World.sims.First();
                one.zone = s.ZoneId; one.onlineFrom = 0; one.onlineHours = 24; one.friendly = .9f; one.level = s.Progress.Level; one.classId = "class.druid";
                pop.Refresh(); yield return null;
                if (!s.Invite(one.id)) report.AppendLine("the Druid would not join: " + s.InviteRefusal(one));
                else
                {
                    var druid = s.PartySim(one.id);
                    druid.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(s.Player.transform.position + Vector3.left * 3); yield return new WaitForSeconds(1);
                    var hp = s.Player.Health; hp.ApplyDamage(Mathf.RoundToInt(hp.Pool.Max * (hp.Pool.Ratio - .6f)));
                    int before = hp.Pool.Current; bool saw = false;
                    motor.SetView(s.Player.transform.eulerAngles.y + 150, 18, 7);
                    for (float t = 0; t < 8 && !saw; t += Time.deltaTime) { if (druid.Activity.StartsWith("Mending")) saw = true; yield return null; }
                    yield return Shot("fix-06-druid-mending-you", "note 122: the party's Druid (" + one.name + ") " + druid.Activity + "; your health " + before + " to " + hp.Pool.Current + " of " + hp.Pool.Max + (saw ? "" : " (NOT SEEN MENDING in 8 s)"), druid.transform);
                    yield return new WaitForSeconds(3);
                    yield return Shot("fix-07-druid-after", "note 122: three seconds on: your health " + hp.Pool.Current + " of " + hp.Pool.Max, druid.transform);
                    s.LeaveParty(one.id);
                }
            }

            // The tester's note 116, Goodbye shuts the shop and the bags it opened.
            var life = VillageLife.Active; var maud = life == null ? null : life.Find("Maud Tanner");
            if (maud == null) report.AppendLine("no Maud Tanner here (not Oakhaven?)");
            else
            {
                motor.Teleport(maud.transform.position + maud.transform.forward * 2.5f + Vector3.up * .3f); yield return null;
                motor.SetView(Quaternion.LookRotation(maud.transform.position - s.Player.transform.position).eulerAngles.y + 25, 14, 5);
                s.InventoryOpen = false; s.OpenVendor(maud);
                yield return Shot("fix-08-shop-open", "note 116: Maud's shop open (bags opened with it: " + s.InventoryOpen + ")", maud.transform);
                s.CloseVendor();
                yield return Shot("fix-09-shop-goodbye", "note 116: after Goodbye: shop " + (s.VendorNpc == null ? "shut" : "STILL OPEN") + ", bags " + (s.InventoryOpen ? "STILL OPEN" : "put away"), maud.transform);
            }

            // Note 120, the inn: inside, its walls stay (the camera inside the walls).
            var zone = Crulanda.World.ZoneBuilder.Active; var door = zone == null ? null : zone.Doors.Find(d => d.openable);
            if (door == null) report.AppendLine("no openable door here");
            else
            {
                door.SetOpen(true);
                var outward = door.position - door.hinge.parent.position; outward.y = 0; outward.Normalize();
                float yaw = Quaternion.LookRotation(-outward).eulerAngles.y;
                motor.Teleport(door.position + outward * 4 + Vector3.up * .2f); motor.SetView(yaw, 12, 5);
                yield return Shot("fix-10-inn-door", "note 120: " + door.name + " from its doorstep", null, false);
                motor.Teleport(door.hinge.parent.position + Vector3.up * 1.1f - outward * .5f); motor.SetView(yaw + 30, 18, 4);
                yield return Shot("fix-11-inn-inside", "note 120: inside " + door.name + ": its walls should stand, not fade away", null, false);
                motor.SetView(yaw + 200, 18, 6);
                yield return Shot("fix-12-inn-inside-turned", "note 120 and 117: inside, turned toward a wall: the camera stays within the walls", null, false);
            }
            File.WriteAllText(Path.Combine(dir, "fixes.txt"), report.ToString());
            Debug.Log("FIXES_CAPTURE_DONE"); Application.Quit(0);
        }
    }
}