using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Chris's new creatures in a living Oakhaven (2026-10-14: "check in game frames that the new boars, the undead thrall, the birds and the
    /// squirrels behave in a live world, not just in the picture tool"): the wild boars at their camp and in a fight, a bird on the ground and
    /// taking off as you come, a squirrel in a grove, and a Bonecaller's bone thrall summoned, fighting, and falling into a heap of bones.
    /// Crulanda.exe --crulanda-creature-tour &lt;folder&gt; --crulanda-class class.mage --crulanda-zone zone.oakhaven --crulanda-temp-save.
    /// Writes ct-01..ct-08 and creatures.txt (what each frame should show and the numbers behind it); CREATURE_TOUR_DONE in the log.
    /// </summary>
    public sealed class CreatureTourCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-creature-tour";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        string dir; readonly System.Text.StringBuilder report = new System.Text.StringBuilder(); EncounterSession session; float keptScale = -1;

        IEnumerator Shot(string name, string what)
        {
            yield return new WaitForSecondsRealtime(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png")); report.AppendLine(name + ".png: " + what);
            Debug.Log("Creature tour: " + name + " " + what); yield return new WaitForSecondsRealtime(.5f);
        }
        /// <summary>A close-up from a camera of its own a few metres from <paramref name="target"/> (a bird or a squirrel flees from you, and
        /// small things are lost behind your back in the game's view): rendered to a texture and saved, the game's view untouched.</summary>
        IEnumerator CloseUp(Vector3 target, float dist, float height, string name, string what)
        {
            yield return new WaitForEndOfFrame();
            var go = new GameObject("Close-up camera"); var cam = go.AddComponent<Camera>(); var main = Camera.main;
            if (main != null) { cam.CopyFrom(main); }
            cam.fieldOfView = 35; cam.nearClipPlane = .05f;
            var away = session.Player.transform.position - target; away.y = 0; if (away.sqrMagnitude < .01f) away = Vector3.back;
            var side = Quaternion.Euler(0, 35, 0) * away.normalized;   // a little to one side of the line to you
            go.transform.position = target + side * dist + Vector3.up * height; go.transform.LookAt(target + Vector3.up * height * .25f);
            var rt = new RenderTexture(1440, 900, 24); cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1440, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); tex.Apply();
            RenderTexture.active = prev; cam.targetTexture = null; Destroy(go); rt.Release();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); Destroy(tex);
            report.AppendLine(name + ".png: " + what); Debug.Log("Creature tour: " + name + " " + what);
        }
        static float Yaw(Vector3 from, Vector3 to) { var d = to - from; d.y = 0; return d.sqrMagnitude < .001f ? 0 : Quaternion.LookRotation(d).eulerAngles.y; }
        /// <summary>Stands you <paramref name="off"/> metres from <paramref name="at"/>, looking at it from <paramref name="height"/> up.</summary>
        IEnumerator Look(Vector3 at, float off, float pitch, float dist)
        {
            var motor = session.Player.GetComponent<AdventurerMotor>();
            var dir0 = at - session.Player.transform.position; dir0.y = 0; if (dir0.sqrMagnitude < .01f) dir0 = Vector3.forward;
            var stand = at - dir0.normalized * off; stand = session.Zone != null ? session.Zone.Ground(new Vector2(stand.x, stand.z)) : stand;
            motor.Teleport(stand + Vector3.up * .3f); motor.SetView(Yaw(stand, at), pitch, dist);
            yield return new WaitForSeconds(1.2f);
        }
        IEnumerator Use(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                if (!session.PlayerCasting)
                    for (int i = 0; i < session.Kit.ActionCount; i++)
                    {
                        var a = session.Kit.ActionAt(i); if (a == null || session.Kit.ActionLockLabel(i) != null || a.effect != AbilityEffect.Damage) continue;
                        if (session.UseAbility(i)) break;
                    }
                yield return null;
            }
        }

        public IEnumerator Run(EncounterSession s, string directory)
        {
            dir = directory; Directory.CreateDirectory(dir); session = s;
            keptScale = EncounterHud.UiScale; EncounterHud.UiScale = 1; EncounterHud.DefaultPanels = true;
            Crulanda.World.WorldClock.Hour = 11;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            yield return new WaitForSeconds(3);
            s.Progress.experience = Math.Max(s.Progress.experience, EncounterProgress.XpForLevel(6)); s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max);

            // 1-2. The wild boars at a camp, then one in a fight.
            var boar = s.Enemies.Where(e => e != null && e.Camp && !e.Game && e.actor.IsAlive && e.actor.DisplayName.IndexOf("boar", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(e => Vector3.Distance(e.transform.position, s.Player.transform.position)).FirstOrDefault();
            if (boar != null)
            {
                var bv = boar.GetComponent<ActorVisual>();
                report.AppendLine("boar: " + boar.actor.DisplayName + ", model " + (bv != null && bv.BeastModel != null ? bv.BeastModel.Kind : "none"));
                yield return Look(boar.transform.position, 7, 18, 6);
                yield return Shot("ct-01-boars", "a wild boar camp (the Rip Vertices boar, idling and rooting)");
                yield return CloseUp(boar.transform.position, 3.2f, 1.1f, "ct-01b-boar-close", "a wild boar, close");
                s.Select(boar); yield return Use(4);
                yield return Shot("ct-02-boar-fight", "a wild boar charging or goring you (its own attack clips), " + boar.actor.Health.Pool.Current + " of " + boar.actor.Health.Pool.Max);
                boar.actor.Health.ApplyDamage(boar.actor.Health.Pool.Max * 10); yield return new WaitForSeconds(1.6f);
                yield return Shot("ct-03-boar-down", "the boar dead on its side (its dying clip)");
            }
            else report.AppendLine("boar: none found in this zone");

            // 4-5. A bird on the ground, then taking off as you walk at it.
            var life = VillageLife.Active;
            var birds = life != null ? life.Critters.Where(c => c != null && c.Kind == "crow").ToList() : new System.Collections.Generic.List<Critter>();
            var squirrels = life != null ? life.Critters.Where(c => c != null && c.Kind == "squirrel").ToList() : new System.Collections.Generic.List<Critter>();
            report.AppendLine("birds in the zone: " + birds.Count + "; squirrels: " + squirrels.Count);
            var bird = birds.OrderBy(c => Vector3.Distance(c.transform.position, s.Player.transform.position)).FirstOrDefault();
            if (bird != null)
            {
                report.AppendLine("bird model: " + string.Join(", ", bird.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterial != null && r.sharedMaterial.mainTexture != null ? r.sharedMaterial.mainTexture.name : r.name).Distinct().Take(3)));
                yield return Look(bird.transform.position, 12f, 14, 4f);   // out of its flight distance
                yield return CloseUp(bird.transform.position, 1.6f, .45f, "ct-04-bird", "a wild bird on the ground, close (a Living Birds model: hopping, pecking or watching)");
                var motor = s.Player.GetComponent<AdventurerMotor>(); var to = bird.transform.position;
                for (int k = 0; k < 14; k++) { var p = Vector3.MoveTowards(s.Player.transform.position, to, .25f); motor.Teleport(p + Vector3.up * .05f); yield return new WaitForSeconds(.05f); }
                yield return new WaitForSeconds(.35f);
                yield return CloseUp(bird.transform.position, 2.4f, .8f, "ct-05-bird-flies", "the bird taking off as you come near (its flight clip)");
            }
            // 6. A squirrel in a grove.
            var squirrel = squirrels.OrderBy(c => Vector3.Distance(c.transform.position, s.Player.transform.position)).FirstOrDefault();
            if (squirrel != null) { yield return Look(squirrel.transform.position, 12f, 16, 4f); yield return CloseUp(squirrel.transform.position, 1.4f, .5f, "ct-06-squirrel", "a squirrel in a grove, close (Rip Vertices' squirrel: idling, sitting up or running)"); }

            // 7-9. The bone thrall: summoned, fighting, and falling into bones.
            var master = s.Kit as IPetMaster;
            if (master != null)
            {
                var pet = s.SummonPet(master, "Bone thrall", "thrall", ActorLook.Skeleton); yield return new WaitForSeconds(2);
                var pv = pet != null ? pet.GetComponent<ActorVisual>() : null;
                report.AppendLine("thrall model: " + (pv != null && pv.BeastModel != null ? pv.BeastModel.Kind + ", crumbles " + pv.BeastModel.Crumbles : "none"));
                if (pet != null)
                {
                    yield return Look(pet.transform.position, 4, 14, 4.5f);
                    yield return Shot("ct-07-thrall", "the Bonecaller's bone thrall at your side (the Toon RTS undead, sword and shield)");
                    var foe = s.Enemies.Where(e => e != null && e.Camp && !e.Game && e.actor.IsAlive).OrderBy(e => Vector3.Distance(e.transform.position, s.Player.transform.position)).FirstOrDefault();
                    if (foe != null) { s.Select(foe); yield return Use(3); yield return Look(pet.transform.position, 4, 14, 4.5f); yield return Shot("ct-08-thrall-fights", "the thrall fighting (its attack clip)"); }
                    var pa = pet.GetComponent<Crulanda.Gameplay.Actor>();
                    if (pa != null) { var at = pet.transform.position; pa.Health.ApplyDamage(pa.Health.Pool.Max * 10); yield return new WaitForSeconds(1.8f); yield return CloseUp(at, 2.6f, 1.4f, "ct-09-thrall-bones", "the thrall fallen into a heap of bones, close"); }
                }
            }
            else report.AppendLine("thrall: this class has no pet (run as class.mage)");
            Finish();
        }
        void Finish()
        {
            if (keptScale > 0) EncounterHud.UiScale = keptScale;
            File.WriteAllText(Path.Combine(dir, "creatures.txt"), report.ToString());
            Debug.Log("CREATURE_TOUR_DONE"); Application.Quit(0);
        }
    }
}
