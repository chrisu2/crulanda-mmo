using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Step-4 frames for Chris (10:34 and 11:14 orders): the Salt-Mender figure, salt circle and cairn, the new hoe hands, the Bonecaller's
    /// thrall, drain and spikes, the Unwriting's damage over time and the Still Vow's shield. One run per class, on a temp save:
    /// Crulanda.exe --crulanda-step4-capture &lt;folder&gt; --crulanda-class class.saltmender|class.mage|class.archivist --crulanda-zone zone.oakhaven --crulanda-temp-save.
    /// Writes step4-*.png and step4-&lt;class&gt;.txt; STEP4_CAPTURE_DONE in the log.
    /// </summary>
    public sealed class Step4Capture : MonoBehaviour
    {
        public const string Flag = "--crulanda-step4-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        string dir; readonly System.Text.StringBuilder report = new System.Text.StringBuilder(); EncounterSession session;

        IEnumerator Shot(string name, string what, Transform also)
        {
            yield return new WaitForSecondsRealtime(.4f);
            var player = session.Player.transform; var motor = session.Player.GetComponent<AdventurerMotor>(); var cam = Camera.main;
            List<Renderer> InWay()
            {
                var l = CaptureFraming.InTheWay(cam.transform.position, player.position + Vector3.up, player, also);
                if (also != null) l.AddRange(CaptureFraming.InTheWay(cam.transform.position, also.position + Vector3.up * .3f, also, player));
                return l;
            }
            var blocked = InWay();
            for (int k = 0; k < 8 && blocked.Count > 0 && motor.enabled; k++)
            {
                var e = cam.transform.eulerAngles; motor.SetView(e.y + 45, e.x > 180 ? e.x - 360 : e.x, Vector3.Distance(cam.transform.position, player.position));
                yield return null; yield return null; blocked = InWay();
            }
            CaptureFraming.Hide(blocked);
            yield return new WaitForSecondsRealtime(.1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
            report.AppendLine(name + ".png: " + what + (blocked.Count > 0 ? " (" + blocked.Count + " things hidden for the shot)" : ""));
            Debug.Log("Step4 capture: " + name + " " + what); yield return new WaitForSecondsRealtime(.5f);
            CaptureFraming.Show(blocked);
        }

        void Buy(string prefix)
        {
            var s = session; s.Progress.talents = new List<TalentRank>();
            var ids = s.Talents.Branches.SelectMany(b => b.nodes).Where(n => n.id.StartsWith(prefix, StringComparison.Ordinal)).Select(n => n.id).ToList();
            for (int pass = 0; pass < 40 && s.Talents.Available(s.Progress) > 0; pass++)
                foreach (var id in ids)
                    if (s.Talents.Find(id) != null && s.Talents.Propose(s.Progress, id, 1, out var next, out _)) s.Progress.talents = next;
            report.AppendLine("talents (" + prefix + "): " + string.Join(", ", s.Progress.talents.Select(t => t.id + " " + t.rank)));
        }
        int Slot(string id) { for (int i = 0; i < session.Kit.ActionCount; i++) { var a = session.Kit.ActionAt(i); if (a != null && a.id == id) return i; } return -1; }
        bool Use(string id)
        {
            int slot = Slot(id); bool ok = slot >= 0 && session.UseAbility(slot);
            if (!ok) report.AppendLine(id + " NOT USED (slot " + slot + ", lock " + (slot >= 0 ? session.Kit.ActionLockLabel(slot) : "-") + ")");
            return ok;
        }
        void View(float turn, float pitch, float dist) { session.Player.GetComponent<AdventurerMotor>().SetView(session.Player.transform.eulerAngles.y + turn, pitch, dist); }
        EncounterEnemy NearFoe()
        {
            var s = session; var motor = s.Player.GetComponent<AdventurerMotor>();
            var foe = s.Enemies.Find(x => x != null && x.actor.IsAlive && x.Camp) ?? s.Enemies.Find(x => x != null && x.actor.IsAlive && !x.Game);
            if (foe == null) { report.AppendLine("no enemy"); return null; }
            motor.Teleport(foe.transform.position + Vector3.back * 7 + Vector3.up * .2f);
            s.Player.transform.rotation = Quaternion.LookRotation(Vector3.forward); s.Select(foe); return foe;
        }

        public IEnumerator Run(EncounterSession s, string directory)
        {
            dir = directory; Directory.CreateDirectory(dir); session = s;
            Crulanda.World.WorldClock.Hour = 11;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 4000); s.Player.Health.ApplyHealing(100000);
            s.Progress.experience = EncounterProgress.XpForLevel(15); s.Player.SetLevel(15);
            yield return new WaitForSeconds(3);
            var motor = s.Player.GetComponent<AdventurerMotor>();
            string cls = s.Progress.classId;
            if (cls == "class.saltmender")
            {
                Buy("md-");
                motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(10, 1, 10)); yield return new WaitForSeconds(1);
                View(180, 8, 2.6f); yield return Shot("step4-01-saltmender-figure-front", "the Salt-Mender figure, front, 2.6 m", null);
                View(120, 10, 2.8f); yield return Shot("step4-02-saltmender-figure-three-quarter", "the Salt-Mender figure, three-quarter", null);
                Use("salt.circle"); yield return new WaitForSeconds(1.2f);
                var circle = GameObject.Find("Salt circle");
                View(150, 35, 5f); yield return Shot("step4-03-salt-circle", "the Salt Circle on the ground round the Mender (" + (circle != null ? "found" : "NOT FOUND") + ")", circle != null ? circle.transform : null);
                motor.Teleport(s.Player.transform.position + Vector3.right * 4); yield return new WaitForSeconds(1.6f);
                Use("salt.cairn"); yield return new WaitForSeconds(1.2f);
                var cairn = GameObject.Find("Salt cairn");
                View(150, 18, 3.5f); yield return Shot("step4-04-salt-cairn", "the Salt Cairn beside the Mender (" + (cairn != null ? "found" : "NOT FOUND") + ")", cairn != null ? cairn.transform : null);
                // The new hoe hands (note 157): a farmer at work in a field, side on and close.
                var farmer = UnityEngine.Object.FindObjectsByType<Villager>(FindObjectsSortMode.None).FirstOrDefault(v => v.Role == "farmer" && v.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Hoe blade"));
                var field = Crulanda.World.CropField.All.FirstOrDefault();
                if (farmer == null || field == null) report.AppendLine("no hoeing farmer");
                else
                {
                    var vis = farmer.GetComponent<ActorVisual>(); var agent = farmer.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    farmer.enabled = false; if (farmer.Carrying) farmer.PutDownLoad();
                    if (agent != null && agent.isOnNavMesh) { agent.Warp(field.transform.position); agent.isStopped = true; }
                    yield return new WaitForSeconds(.5f); vis.Pose = ActorPose.Work; yield return new WaitForSeconds(2);
                    var side = farmer.transform.right; motor.Teleport(farmer.transform.position + side * 2.6f + Vector3.up * .2f); yield return null;
                    s.Player.transform.rotation = Quaternion.LookRotation(-side);
                    for (int i = 0; i < 3; i++)
                    {
                        float want = new[] { .1f, .45f, .7f }[i];
                        for (float t = 0; t < 3 && Mathf.Abs(vis.StrokePhase - want) > .04f; t += Time.deltaTime) yield return null;
                        Time.timeScale = 0;
                        motor.SetView(Quaternion.LookRotation(farmer.transform.position - s.Player.transform.position).eulerAngles.y - 25, 6, 2.2f);
                        yield return Shot("step4-0" + (5 + i) + "-hoe-hands-phase-" + want.ToString("0.00"), "a farmer hoeing, hands " + (vis.HandSpread * 100).ToString("0") + " cm apart on the shaft (45-60 wanted), stroke phase " + vis.StrokePhase.ToString("0.00"), farmer.transform);
                        Time.timeScale = 1;
                    }
                }
            }
            else if (cls == "class.mage")
            {
                Buy("bc-");
                var foe = NearFoe(); yield return new WaitForSeconds(1);
                if (foe != null)
                {
                    Use("mage.raise_thrall"); yield return new WaitForSeconds(2f);
                    var thrall = s.Pet != null ? s.Pet.transform : null;
                    View(150, 12, 4f); yield return Shot("step4-08-bonecaller-thrall", "the Bonecaller's bone thrall raised beside the Mage (" + (thrall != null ? "up" : "NOT RAISED") + ")", thrall);
                    s.Select(foe); Use("mage.marrow_drain"); yield return new WaitForSeconds(2.4f);
                    View(200, 14, 6f); yield return Shot("step4-09-bonecaller-marrow-drain", "Marrow Drain on the foe; target: " + s.Kit.TargetStatus(foe), foe.transform);
                    yield return new WaitForSeconds(1.6f);
                    if (thrall != null) { var a = s.Pet.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (a != null) a.Warp(foe.transform.position + Vector3.back * 1.5f); }
                    yield return new WaitForSeconds(.3f);
                    Use("mage.bone_spikes"); yield return new WaitForSeconds(.2f);
                    var spikes = GameObject.Find("Bone spikes");
                    View(160, 25, 6f); yield return Shot("step4-10-bonecaller-bone-spikes", "Bone Spikes bursting round the thrall (" + (spikes != null ? "shown" : "NOT SHOWN") + ")", spikes != null ? spikes.transform : foe.transform);
                }
            }
            else if (cls == "class.archivist")
            {
                Buy("uw-");
                var foe = NearFoe(); yield return new WaitForSeconds(1);
                if (foe != null)
                {
                    Use("archivist.unwrite"); yield return new WaitForSeconds(2.6f);
                    View(200, 14, 6f); yield return Shot("step4-11-unwriting-dot", "the Unwriting's damage over time on the foe; target: " + s.Kit.TargetStatus(foe), foe.transform);
                    yield return new WaitForSeconds(1.6f); Use("archivist.erasure"); yield return new WaitForSeconds(.3f);
                    View(200, 14, 6f); yield return Shot("step4-12-unwriting-erasure", "Erasure on the unwritten foe; target: " + s.Kit.TargetStatus(foe), foe.transform);
                    foreach (var e in s.Enemies) if (e != null) e.ResetFight();
                }
                Buy("vw-");
                motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(10, 1, -10)); yield return new WaitForSeconds(2);
                var hp = s.Player.Health; hp.ApplyDamage(hp.Pool.Max / 2); yield return new WaitForSeconds(1.6f);
                Use("archivist.stillness"); yield return new WaitForSeconds(.5f);
                View(160, 12, 3.2f); yield return Shot("step4-13-still-vow-shield", "the Still Vow's Vow of Stillness barrier on the most hurt (you, at " + hp.Pool.Current + "/" + hp.Pool.Max + "); status: " + s.Kit.StatusLine, null);
            }
            else report.AppendLine("class " + cls + " has no step-4 frames");
            File.WriteAllText(Path.Combine(dir, "step4-" + cls.Replace("class.", "") + ".txt"), report.ToString());
            Debug.Log("STEP4_CAPTURE_DONE"); yield return new WaitForSecondsRealtime(1);
            Application.Quit();
        }
    }
}