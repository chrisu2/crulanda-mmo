using Crulanda.Combat;
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

        IEnumerator Shot(string name, string what, Transform also, bool clear = true)
        {
            yield return new WaitForSecondsRealtime(.4f);
            var player = session.Player.transform; var motor = session.Player.GetComponent<AdventurerMotor>(); var cam = Camera.main;
            List<Renderer> InWay()
            {
                var l = CaptureFraming.InTheWay(cam.transform.position, player.position + Vector3.up, player, also);
                if (also != null) l.AddRange(CaptureFraming.InTheWay(cam.transform.position, also.position + Vector3.up * .3f, also, player));
                return l;
            }
            var blocked = clear ? InWay() : new List<Renderer>();
            for (int k = 0; k < 8 && blocked.Count > 0 && motor.enabled; k++)
            {
                var e = cam.transform.eulerAngles; motor.SetView(e.y + 45, e.x > 180 ? e.x - 360 : e.x, Vector3.Distance(cam.transform.position, player.position));
                yield return null; yield return null; blocked = InWay();
            }
            blocked.RemoveAll(r => r == null || r.transform.IsChildOf(player) || (also != null && r.transform.IsChildOf(also)) || (session.Pet != null && r.transform.IsChildOf(session.Pet.transform)));   // never hide what the frame is of
            foreach (var a in AuraSpin.All) if (a != null && a.Follow != null) report.AppendLine("  ring " + a.name + ": root " + a.Follow.position.ToString("F2") + ", body " + a.BodyPoint.ToString("F2") + ", ring at " + a.transform.position.ToString("F2"));
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
        void FaceFoe(EncounterEnemy foe) { var d = foe.transform.position - session.Player.transform.position; d.y = 0; session.Player.GetComponent<AdventurerMotor>().SetView(Quaternion.LookRotation(d).eulerAngles.y + 15, 16, 5.5f); }
        int Dithered() { int n = 0; foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader.name == "Crulanda/Fade") n++; return n; }
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
            s.Progress.experience = EncounterProgress.XpForLevel(15); s.Player.SetLevel(15);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 4000); s.Player.Health.ApplyHealing(100000);   // after the level, which resets the base
            yield return new WaitForSeconds(3);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 4000); s.Player.Health.ApplyHealing(100000);
            report.AppendLine("health " + s.Player.Health.Pool.Current + "/" + s.Player.Health.Pool.Max);
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
                    var side = vis.transform.right; if (vis.Figure != null && vis.Figure.Animator != null) { side = vis.Figure.Animator.transform.right; side.y = 0; side.Normalize(); } motor.Teleport(farmer.transform.position + side * 2.4f + Vector3.up * .2f); yield return null;
                    var mine = s.Player.GetComponentsInChildren<Renderer>().Where(x => x.enabled).ToList(); foreach (var x in mine) x.enabled = false;   // the Mender out of the shot: the farmer close
                    s.Player.transform.rotation = Quaternion.LookRotation(-side);
                    for (int i = 0; i < 3; i++)
                    {
                        float want = new[] { .1f, .45f, .7f }[i];
                        for (float t = 0; t < 3 && Mathf.Abs(vis.StrokePhase - want) > .04f; t += Time.deltaTime) yield return null;
                        Time.timeScale = 0;
                        motor.SetView(Quaternion.LookRotation(farmer.transform.position - s.Player.transform.position).eulerAngles.y, 4, .5f);
                        yield return Shot("step4-0" + (5 + i) + "-hoe-hands-phase-" + want.ToString("0.00"), "a farmer hoeing, hands " + (vis.HandSpread * 100).ToString("0") + " cm apart on the shaft (45-60 wanted), stroke phase " + vis.StrokePhase.ToString("0.00"), farmer.transform);
                        Time.timeScale = 1;
                    }
                    foreach (var x in mine) x.enabled = true;
                }
                            // Step 5 (13:22): the open village with nothing dithered, the view through a wall, feet side on, Mira's invite.
                var innP = Crulanda.World.ZoneBuilder.Active.Zone.props.FirstOrDefault(p => p != null && p.kind == "inn");
                if (innP != null)
                {
                    var inn = Crulanda.World.RoofFade.All.OrderBy(r => Vector2.Distance(new Vector2(r.transform.position.x, r.transform.position.z), innP.at)).First();
                    motor.Teleport(s.Zone.Ground(innP.at + new Vector2(0, -22), 1.1f)); s.Player.transform.rotation = Quaternion.LookRotation(Vector3.forward); yield return new WaitForSeconds(1.5f);
                    motor.SetView(180, 8, 7); yield return new WaitForSeconds(.8f);
                    yield return Shot("step5-01-village-open-view", "the village from the road: " + Dithered() + " renderers dithered (0 wanted)", null, false);
                    float reach = Mathf.Max(inn.Half.x, inn.Half.y) + 2.5f;
                    for (int k = 0; k < 16; k++)
                    {
                        var dir = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.right; var at = inn.transform.TransformPoint(dir * reach);
                        if (!UnityEngine.AI.NavMesh.SamplePosition(at, out var nh, 3, UnityEngine.AI.NavMesh.AllAreas)) continue;
                        motor.Teleport(nh.position + Vector3.up); s.Player.transform.rotation = Quaternion.LookRotation(-dir); yield return null;
                        motor.SetView(Quaternion.LookRotation(dir).eulerAngles.y + 180, 15, 9); yield return new WaitForSeconds(1f);
                        var cam = Camera.main.transform.position; var head = s.Player.transform.position + Vector3.up * .8f;
                        if (Physics.Raycast(cam, (head - cam).normalized, out var hit, Vector3.Distance(cam, head), ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Crulanda.World.RoofFade>() == inn)
                        { yield return Shot("step5-02-behind-a-wall", "behind the inn: its blocking parts faded " + inn.FadedBigParts + " of " + inn.BigParts + ", everything else solid (" + Dithered() + " renderers dithered in all)", null, false); break; }
                    }
                }
                motor.Teleport(s.Zone.Ground(new Vector2(14, 14), 1.1f)); yield return new WaitForSeconds(2);
                var pv = s.Player.GetComponentInChildren<ActorVisual>();
                View(90, 20, 3.2f); yield return Shot("step5-03-feet-side-standing", "the Mender's feet side on, standing: lower sole " + (pv != null ? (pv.SoleGap * 100).ToString("0.0") : "?") + " cm off the ground", null);
                if (s.Companion != null)
                {
                    s.Progress.recruited = false; motor.Teleport(s.Companion.transform.position + s.Companion.transform.forward * 2 + Vector3.up); yield return new WaitForSeconds(1);
                    s.SelectFriendly(null, true); bool ok = s.InviteTarget(); yield return new WaitForSeconds(.5f);
                    var d = s.Companion.transform.position - s.Player.transform.position; d.y = 0; motor.SetView(Quaternion.LookRotation(d).eulerAngles.y + 20, 10, 4);
                    yield return Shot("step5-04-mira-invite", "Mira targeted and invited: " + (ok ? "she joins" : "REFUSED") + ", in party " + s.Progress.recruited, s.Companion.transform);
                }}
            else if (cls == "class.mage")
            {
                Buy("bc-");
                var foe = NearFoe(); yield return new WaitForSeconds(1);

                if (foe != null)
                {
                    Use("mage.raise_thrall"); yield return new WaitForSeconds(2f);
                // Step 6 (Chris 1:29, "meter should be movable in the ui. pet bar should be movable in the ui."): both shown moved.
                EncounterHud.ShowPanelMoved("meter", new Vector2(-640, -330)); EncounterHud.ShowPanelMoved("pet", new Vector2(520, -420)); s.MeterOpen = true;
                View(0, 14, 6); yield return Shot("step6-01-meter-and-pet-bar-moved", "the meter moved to the middle left and the pet bar up to the middle (pet " + (s.Pet != null ? "out" : "NONE") + "); default " + EncounterHud.PanelRect("meter") + " / " + EncounterHud.PanelRect("pet"), null, false);
                EncounterHud.ShowPanelMoved("meter", null); EncounterHud.ShowPanelMoved("pet", null);

                    var thrall = s.Pet != null ? s.Pet.transform : null;
                    View(110, 10, 3.4f); yield return Shot("step4-08-bonecaller-thrall", "the Bonecaller's bone thrall raised beside the Mage (" + (thrall != null ? "up" : "NOT RAISED") + ")", thrall);
                    // Note 173 (Chris 17:06, "id like to see how he moves"): 15 s of video frames at 24 fps, the thrall idling, following and attacking (encoded to MP4 outside).
                    if (thrall != null)
                    {
                        var vdir = Path.Combine(dir, "thrall-video"); Directory.CreateDirectory(vdir); int vf = 0;
                        IEnumerator Rec(float seconds) { int n = Mathf.RoundToInt(seconds * 24); for (int i = 0; i < n; i++) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(vdir, "f" + (vf++).ToString("0000") + ".png")); yield return null; } }
                        Time.captureFramerate = 24;
                        var pmot = s.Player.GetComponent<AdventurerMotor>();
                        var away = -(foe.transform.position - s.Player.transform.position); away.y = 0; s.Player.transform.rotation = Quaternion.LookRotation(away);
                        pmot.SetView(Quaternion.LookRotation(away).eulerAngles.y + 120, 12, 4.5f);
                        yield return Rec(4);                                                   // idle beside the Mage
                        EncounterInput.Scripted = new Vector2(0, 1); yield return Rec(5);      // follows as the Mage walks off
                        EncounterInput.Scripted = null;
                        s.Player.transform.rotation = Quaternion.LookRotation(-away); FaceFoe(foe); s.Select(foe); s.PetAttack();
                        yield return Rec(6);                                                   // sent in: runs to the foe and attacks
                        Time.captureFramerate = 0; report.AppendLine("thrall-video: " + vf + " frames at 24 fps (idle 4 s, follow 5 s, attack 6 s)");
                        s.PetOrder(RangerPet.Order.Assist); foe = NearFoe(); yield return new WaitForSeconds(1);
                    }
                    s.Select(foe); Use("mage.marrow_drain"); yield return new WaitForSeconds(2.4f);
                    FaceFoe(foe); yield return Shot("step4-09-bonecaller-marrow-drain", "Marrow Drain on the foe; target: " + s.Kit.TargetStatus(foe), foe.transform);
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
                    FaceFoe(foe); yield return Shot("step4-11-unwriting-dot", "the Unwriting's damage over time on the foe; target: " + s.Kit.TargetStatus(foe), foe.transform);
                    yield return new WaitForSeconds(1.6f); Use("archivist.erasure"); yield return new WaitForSeconds(.3f);
                    View(200, 14, 6f); yield return Shot("step4-12-unwriting-erasure", "Erasure on the unwritten foe; target: " + s.Kit.TargetStatus(foe), foe.transform);
                    foreach (var e in s.Enemies) if (e != null) e.ResetFight();
                }
                Buy("vw-");
                motor.Teleport(s.Zone.Ground(Vector2.zero) + new Vector3(10, 1, -10)); yield return new WaitForSeconds(2);
                var hp = s.Player.Health; hp.ApplyDamage(hp.Pool.Max / 2); yield return new WaitForSeconds(1.6f);
                Use("archivist.stillness"); yield return new WaitForSeconds(.5f);
                View(160, 12, 3.2f); yield return Shot("step4-13-still-vow-shield", "the Still Vow's Vow of Stillness barrier on the most hurt (you, at " + hp.Pool.Current + "/" + hp.Pool.Max + "); barrier " + s.Player.GetComponent<Combatant>().Barrier + ", ward ring " + (AuraSpin.All.Exists(x => x.Follow == s.Player.transform) ? "shown" : "MISSING"), null);
            }
            else report.AppendLine("class " + cls + " has no step-4 frames");
            File.WriteAllText(Path.Combine(dir, "step4-" + cls.Replace("class.", "") + ".txt"), report.ToString());
            Debug.Log("STEP4_CAPTURE_DONE"); yield return new WaitForSecondsRealtime(1);
            Application.Quit();
        }
    }
}