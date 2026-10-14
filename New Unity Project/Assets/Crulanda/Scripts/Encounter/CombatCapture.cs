using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Frames of every ability a class has, played at the training field's dummy, for looking at the new motions and effects
    /// (2026-10-14, Chris: "we need more spell animations and melee animations also", "we need new spell effects also for ALL spells").
    /// Run the player windowed, on a temp save: Crulanda.exe --crulanda-combat-capture &lt;folder&gt; --crulanda-class class.mage. At level
    /// 15, each talent path bought in turn (as the balance harness buys them), every ability the path opens is used once on the dummy
    /// from its range (a mend with the caster hurt), and framed from the side at three moments after it is used (NN-id-a/b/c), with a
    /// few auto-attack swings first (swing-a/b/c). combat.txt lists each ability, its motion, element and how many effects played.
    /// COMBAT_CAPTURE_DONE in the log.
    /// </summary>
    public sealed class CombatCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-combat-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        Camera side; Transform a, b; readonly StringBuilder report = new StringBuilder();
        void LateUpdate()
        {
            if (side == null || a == null || b == null) return;
            var mid = (a.position + b.position) / 2 + Vector3.up * 1.1f; var across = Vector3.Cross(Vector3.up, b.position - a.position).normalized;
            float span = Vector3.Distance(a.position, b.position);
            // The clear side (a yard post or a fence between the camera and the pair spoiled frames: 2026-10-14), a little higher if neither is.
            float off = 4.5f + span * .55f; var at = mid + across * off + Vector3.up * 1.2f;
            if (Physics.Linecast(mid, at)) { var other = mid - across * off + Vector3.up * 1.2f; at = !Physics.Linecast(mid, other) ? other : mid + across * off * .7f + Vector3.up * 4; }
            side.transform.position = at; side.transform.LookAt(mid);
        }
        IEnumerator Shot(string directory, string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
            yield return null;
        }
        public IEnumerator Run(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            WorldClock.Hour = 11; var weather = WorldWeather.Active; if (weather != null) weather.Force(WeatherKind.Clear, true);
            yield return new WaitForSeconds(3);
            var dummy = s.TrainingDummy; var motor = s.Player.GetComponent<AdventurerMotor>();
            if (dummy == null || motor == null) { report.AppendLine("no training dummy"); Done(directory); yield break; }
            s.Progress.experience = EncounterProgress.XpForLevel(15); s.Player.SetLevel(15);
            s.Player.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, 100000); s.Player.Health.ApplyHealing(100000);
            var main = Camera.main; side = new GameObject("Combat camera").AddComponent<Camera>(); side.CopyFrom(main); side.depth = main.depth + 1; side.fieldOfView = 42;
            a = s.Player.transform; b = dummy.transform;
            report.AppendLine("Class " + s.ClassDef.id + "; " + s.Kit.ActionCount + " abilities; effects " + (SpellFx.Has("Fx/Magic/Prefabs/Character auras/Healing") ? "in the build" : "MISSING"));
            // The swings first.
            Stand(s, motor, dummy, s.Kit.MeleeAutoAttacks ? 2.4f : 14);
            s.Select(dummy); s.BeginAutoAttack(); yield return new WaitForSeconds(2.2f);
            foreach (var k in "abc") { yield return new WaitForSeconds(.35f); yield return Shot(directory, "swing-" + k); }
            s.StopAutoAttack(); yield return new WaitForSeconds(1.6f);
            var done = new HashSet<int>(); int n = 0;
            var branches = s.Talents.Branches.Select(br => br.id).ToList(); branches.Insert(0, null);   // no talents first: what is open from the start
            foreach (var branch in branches)
            {
                if (branch != null) Buy(s, branch);
                for (int slot = 0; slot < s.Kit.ActionCount; slot++)
                {
                    if (done.Contains(slot) || s.Kit.ActionLockLabel(slot) != null) continue;
                    var ab = s.Kit.ActionAt(slot); if (ab == null) continue;
                    done.Add(slot); n++;
                    Stand(s, motor, dummy, ab.range <= .5f ? 6 : Mathf.Clamp(ab.range - 1, 2.2f, 14));
                    s.Player.Resource.Pool.Fill();
                    if (ab.effect == Crulanda.Abilities.AbilityEffect.Heal) s.Player.Health.ApplyDamage(Mathf.RoundToInt(s.Player.Health.Pool.Max * .5f));
                    s.Select(dummy); yield return new WaitForSeconds(1.7f);   // the global cooldown of the one before
                    int before = SpellFx.Played; float t0 = Time.time;
                    bool used = s.UseAbility(slot);
                    float cast = Mathf.Max(0, ab.castTime);
                    string id = (n < 10 ? "0" : "") + n + "-" + (ab.id ?? "slot" + slot).Replace('.', '_');
                    foreach (var (k, at) in new[] { ('a', cast * .6f + .1f), ('b', cast + .3f), ('c', cast + .75f) })
                    {
                        while (Time.time - t0 < at) yield return null;
                        yield return Shot(directory, id + "-" + k);
                    }
                    report.AppendLine(id + "  " + ab.name + "  used=" + used + "  motion=" + (ActorVisual.MotionFor(ab) ?? "-") + "  element=" + SpellFx.Element(ab) + "  effects=" + (SpellFx.Played - before)
                        + "  range=" + ab.range + " cast=" + ab.castTime + (used ? "" : "  (" + s.Kit.ActionLockLabel(slot) + ")"));
                    s.Player.Health.ApplyHealing(100000); yield return new WaitForSeconds(1);
                }
            }
            Done(directory);
        }
        static void Stand(EncounterSession s, AdventurerMotor motor, EncounterEnemy dummy, float distance)
        {
            var d = dummy.transform.position - s.Player.transform.position; d.y = 0; if (d.sqrMagnitude < .01f) d = Vector3.forward;
            var at = dummy.transform.position - d.normalized * distance;
            motor.Teleport(s.Zone.Ground(new Vector2(at.x, at.z), 1.1f)); motor.SetView(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 10, 6);
        }
        /// <summary>Every point in one path, as the balance harness buys them.</summary>
        static void Buy(EncounterSession s, string branchId)
        {
            s.Progress.talents = new List<TalentRank>();
            var ids = s.Talents.Branches.First(x => x.id == branchId).nodes.Select(x => x.id).ToList();
            for (int pass = 0; pass < 40 && s.Talents.Available(s.Progress) > 0; pass++)
                foreach (var id in ids) if (s.Talents.Propose(s.Progress, id, 1, out var next, out _)) s.Progress.talents = next;
            s.Kit.ApplyStats();
        }
        void Done(string directory)
        {
            File.WriteAllText(Path.Combine(directory, "combat.txt"), report.ToString());
            Debug.Log("COMBAT_CAPTURE_DONE"); Application.Quit(0);
        }
    }
}
