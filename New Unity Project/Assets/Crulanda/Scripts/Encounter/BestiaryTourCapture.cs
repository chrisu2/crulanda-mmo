using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Bestiary kit's monsters in a living zone (Chris, 2026-10-18, Docs/BESTIARY_MONSTERS.md): the Sealed Adit's Ember-bound (the Imp)
    /// and the Lowtowns' Bank-snouts (the Puglin). At each of their camps: one mob close up from the front, the back and the side while it
    /// idles; then pulled (you take its blows, healed whole every frame), a frame mid-swing, one on its move (the Chain-Lash's root, the Tusk
    /// Butt's knock) if it comes, one on its death; and a wider frame of the camp (the river's bank and water, or the cave's walk and the lake
    /// of fire's rim). Close-ups come from a camera of their own (CreatureTourCapture.CloseUp's way), the game's view untouched.
    /// Crulanda.exe --crulanda-bestiary-tour &lt;folder&gt; --crulanda-zone zone.adit (or zone.lowtowns) --crulanda-temp-save.
    /// Writes bt-*.png and bestiary.txt (what each frame should show, and the numbers: where the mob stands, its feet over the ground, how far
    /// from the water's edge and the nearest road or from the lake, its facing against its way while it walks); BESTIARY_TOUR_DONE in the log.
    /// </summary>
    public sealed class BestiaryTourCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-bestiary-tour";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        static readonly string[] Mobs = { "Ember-bound", "Bank-snout" };
        string dir; readonly System.Text.StringBuilder report = new System.Text.StringBuilder(); EncounterSession session; float keptScale = -1;

        /// <summary>A close-up from its own camera <paramref name="dist"/> m from <paramref name="target"/> in the direction <paramref name="from"/>,
        /// <paramref name="height"/> up, looking at a point <paramref name="aim"/> m above it; saved as a PNG.</summary>
        IEnumerator Frame(Vector3 target, Vector3 from, float dist, float height, float aim, string name, string what)
        {
            yield return new WaitForEndOfFrame();
            var go = new GameObject("Bestiary camera"); var cam = go.AddComponent<Camera>(); var main = Camera.main;
            if (main != null) cam.CopyFrom(main);
            cam.fieldOfView = 35; cam.nearClipPlane = .05f;
            from.y = 0; if (from.sqrMagnitude < .001f) from = Vector3.back;
            go.transform.position = target + from.normalized * dist + Vector3.up * height; go.transform.LookAt(target + Vector3.up * aim);
            var rt = new RenderTexture(1440, 900, 24); cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1440, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); tex.Apply();
            RenderTexture.active = prev; cam.targetTexture = null; Destroy(go); rt.Release();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); Destroy(tex);
            report.AppendLine(name + ".png: " + what); Debug.Log("Bestiary tour: " + name + " " + what);
        }
        static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var d = b - a; float t = d.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude) : 0; return Vector2.Distance(p, a + d * t); }
        static float PathDist(Vector2 p, Vector2[] pts) { float m = float.MaxValue; for (int i = 0; i + 1 < pts.Length; i++) m = Mathf.Min(m, SegDist(p, pts[i], pts[i + 1])); return m; }
        static Vector2 XZ(Vector3 v) { return new Vector2(v.x, v.z); }
        /// <summary>The lowest point of the mob's model now (its soles), from its renderers.</summary>
        static float Soles(EncounterEnemy e)
        {
            float low = float.MaxValue; foreach (var r in e.GetComponentsInChildren<Renderer>()) if (r.enabled && r.gameObject.activeInHierarchy) low = Mathf.Min(low, r.bounds.min.y);
            return low == float.MaxValue ? e.transform.position.y : low;
        }
        /// <summary>Where the lake of fire is (the Adit's "lake" anchor in its cave), or null.</summary>
        Vector3? Lake()
        {
            var z = session.Zone != null ? session.Zone.Zone : null; var a = z?.anchors?.FirstOrDefault(x => x != null && x.name == "lake"); if (a == null) return null;
            var h = Crulanda.World.Hollow.All.FirstOrDefault(x => x.Name == a.cave); return h != null ? h.At(a.along, a.aside) : (Vector3?)null;
        }
        /// <summary>The numbers for a mob where it stands: its place, its soles over the floor under it, and how far it is from what bounds
        /// its camp (the river's waterline and the nearest road in the Lowtowns; the lake of fire in the Adit).</summary>
        string Numbers(EncounterEnemy e)
        {
            var p = e.transform.position; var z = session.Zone.Zone;
            float floor = session.Zone.StandAt(XZ(p), p.y - 1).y, soles = Soles(e);
            var s = "at (" + p.x.ToString("0.0") + ", " + p.z.ToString("0.0") + "), soles " + (soles - floor).ToString("+0.00;-0.00") + " m over the floor";
            var river = z.water?.FirstOrDefault(w => w != null && w.name == "The Chemical River");
            if (river != null) s += ", " + (PathDist(XZ(p), river.points) - river.width / 2).ToString("0.0") + " m from the waterline";
            if (z.roads != null && z.roads.Length > 0)
            {
                var near = z.roads.Where(r => r != null && r.points != null && r.points.Length > 1).OrderBy(r => PathDist(XZ(p), r.points) - r.width / 2).FirstOrDefault();
                if (near != null) s += ", " + (PathDist(XZ(p), near.points) - near.width / 2).ToString("0.0") + " m from " + near.name;
            }
            var lake = Lake(); if (lake.HasValue) s += ", " + Vector2.Distance(XZ(p), XZ(lake.Value)).ToString("0.0") + " m from the lake's middle";
            return s;
        }

        public IEnumerator Run(EncounterSession s, string directory)
        {
            dir = directory; Directory.CreateDirectory(dir); session = s;
            keptScale = EncounterHud.UiScale; EncounterHud.UiScale = 1; EncounterHud.DefaultPanels = true;
            Crulanda.World.WorldClock.Hour = 11;
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            yield return new WaitForSeconds(3);
            var camps = s.Zone != null && s.Zone.Zone != null ? s.Zone.Zone.camps : new Crulanda.World.ZoneCamp[0];
            report.AppendLine("zone " + (s.Zone != null && s.Zone.Zone != null ? s.Zone.Zone.id : "none"));
            var byCamp = s.Enemies.Where(e => e != null && e.Camp && e.actor.IsAlive && e.CampIndex >= 0 && e.CampIndex < camps.Length && Mobs.Contains(camps[e.CampIndex].mob))
                .GroupBy(e => e.CampIndex).OrderBy(g => g.Key).ToList();
            if (byCamp.Count == 0) report.AppendLine("no Ember-bound or Bank-snout camp in this zone (run with --crulanda-zone zone.adit or zone.lowtowns)");
            int n = 0;
            foreach (var group in byCamp)
            {
                n++; var camp = camps[group.Key]; string tag = "bt-" + n.ToString("00") + "-";
                var e = group.OrderBy(x => Vector2.Distance(XZ(x.transform.position), camp.center)).First();
                var bv = e.GetComponent<ActorVisual>(); var bm = bv != null ? bv.BeastModel : null;
                report.AppendLine(); report.AppendLine("== " + camp.name + " (" + camp.mob + ", camp " + group.Key + ", " + group.Count() + " up, levels " + camp.levelMin + "-" + camp.levelMax + ")");
                report.AppendLine("model " + (bm != null ? bm.Kind + ", " + bm.Height.ToString("0.00") + " m, human avatar " + bm.OnHumanAvatar : "none (a plain figure: the kit or its avatar is missing)")
                    + ", skins " + string.Join(", ", group.Select(x => x.GetComponent<ActorVisual>()?.BeastModel?.Renderers.FirstOrDefault()?.sharedMaterial?.mainTexture?.name ?? "-").Distinct()));
                // Level to the camp's band and stand off to one side, out of its reach.
                s.Progress.experience = Math.Max(s.Progress.experience, EncounterProgress.XpForLevel(camp.levelMax)); s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max);
                var motor = s.Player.GetComponent<AdventurerMotor>();
                var mp = e.transform.position; var away = Quaternion.Euler(0, 90, 0) * e.transform.forward; var stand = mp + away * 22;
                stand = s.Zone.StandAt(XZ(stand), mp.y - 1, 0); motor.Teleport(stand + Vector3.up * .3f);
                var look = mp - stand; look.y = 0; motor.SetView(look.sqrMagnitude > .01f ? Quaternion.LookRotation(look).eulerAngles.y : 0, 16, 7);
                yield return new WaitForSeconds(2.5f);
                mp = e.transform.position; float h = bm != null ? bm.Height : 1.7f; var fwd = e.transform.forward;
                report.AppendLine("idle: " + Numbers(e));
                yield return Frame(mp, fwd, h * 1.9f + 1, h * .6f, h * .5f, tag + "front", camp.mob + " idling, from the front (faces the camera; feet on the ground; " + (camp.mob == "Ember-bound" ? "mace in the right hand, chains and collar" : "stick in the hand, tusks") + ")");
                yield return Frame(mp, -fwd, h * 1.9f + 1, h * .6f, h * .5f, tag + "back", camp.mob + " idling, from behind");
                yield return Frame(mp, Vector3.Cross(Vector3.up, fwd), h * 1.9f + 1, h * .6f, h * .5f, tag + "side", camp.mob + " idling, from its right side (stands upright, feet on the ground)");
                // The camp from further off: what bounds it.
                var lake = Lake(); Vector3 wideFrom;
                var river = s.Zone.Zone.water?.FirstOrDefault(w => w != null && w.name == "The Chemical River");
                if (river != null)
                {
                    // From the land side, so the bank and the water lie behind the pack.
                    var c2 = camp.center; var best = river.points[0]; float bd = float.MaxValue;
                    for (int i = 0; i + 1 < river.points.Length; i++) for (float t = 0; t <= 1; t += .1f) { var q = Vector2.Lerp(river.points[i], river.points[i + 1], t); float d = Vector2.Distance(q, c2); if (d < bd) { bd = d; best = q; } }
                    var land = c2 - best; wideFrom = new Vector3(land.x, 0, land.y);
                }
                else if (lake.HasValue) wideFrom = mp - lake.Value;   // from the far side of the camp from the lake: the walk, the pair and the rim behind
                else wideFrom = -fwd;
                var centre = s.Zone.StandAt(camp.center, mp.y - 1, 0);
                yield return Frame(centre, wideFrom, 15, 7, .5f, tag + "camp", camp.name + ": the camp from " + (river != null ? "the land side, the bank and the river behind it (nobody in the water)" : "its walk, the cave's floor and " + (lake.HasValue ? "the lake of fire's rim" : "its walls")));
                // The fight: pulled, you healed whole every frame (you take its blows), until its move comes or 24 s pass.
                s.Select(e); e.Receive(1, s.Player);
                bool swung = false, cast = false, castAfter = false; float worst = 0, walked = 0; int walkSamples = 0; var last = e.transform.position;
                for (float t = 0; t < 24 && e.actor.IsAlive; t += Time.deltaTime)
                {
                    s.Player.Health.ApplyHealing(s.Player.Health.Pool.Max);
                    var p = e.transform.position; var v = (p - last) / Mathf.Max(Time.deltaTime, 1e-4f); v.y = 0; last = p;
                    if (v.magnitude > .5f && Time.deltaTime > 0) { float off = Vector3.Angle(v, e.transform.forward); worst = Mathf.Max(worst, off); walked += off; walkSamples++; }
                    var to = s.Player.transform.position - p; to.y = 0;
                    if (!swung && bm != null && bm.Busy && !e.Casting)
                    { swung = true; yield return Frame(p, Quaternion.Euler(0, 70, 0) * (to.sqrMagnitude > .01f ? to : fwd), h * 2 + 1.2f, h * .7f, h * .5f, tag + "swing", camp.mob + " mid-swing at you (its weapon in the blow, not floating)"); }
                    if (!cast && e.Casting && e.CastProgress > .6f)
                    { cast = true; yield return Frame(p, Quaternion.Euler(0, 70, 0) * (to.sqrMagnitude > .01f ? to : fwd), h * 2 + 1.5f, h * .8f, h * .5f, tag + "move", camp.mob + " winding up its " + (e.Cast != null ? e.Cast.name : "move") + " (the cast bar over it)"); }
                    if (cast && !castAfter && !e.Casting)
                    {
                        castAfter = true; yield return new WaitForSeconds(.25f);
                        yield return Frame(s.Player.transform.position, Quaternion.Euler(0, 60, 0) * (to.sqrMagnitude > .01f ? -to : fwd), 6, 2.5f, 1, tag + "move-landed", camp.mob + "'s " + (e.Cast != null ? e.Cast.name : "move") + " landed on you (" + (camp.mob == "Ember-bound" ? "rooted: the root on your bar" : "knocked back a step or two") + ")");
                    }
                    if (swung && castAfter) break;
                    yield return null;
                }
                report.AppendLine("fight: swing " + (swung ? "seen" : "NOT seen") + ", move " + (cast ? (castAfter ? "seen and landed" : "wound up") : "NOT seen in 24 s") + "; walking: facing off its way by " + (walkSamples > 0 ? (walked / walkSamples).ToString("0") + " degrees on average, " + worst.ToString("0") + " at worst (" + walkSamples + " frames)" : "no walking frames"));
                report.AppendLine("in the fight: " + Numbers(e));
                // Its death.
                var at = e.transform.position; e.actor.Health.ApplyDamage(e.actor.Health.Pool.Max * 10); yield return new WaitForSeconds(2.2f);
                yield return Frame(at, Quaternion.Euler(0, 40, 0) * fwd, h * 1.9f + 1, h * .9f, .2f, tag + "death", camp.mob + " dead where it fell (the death clip held; the body on the ground, not floating or sunk)");
                report.AppendLine("dead: soles " + (Soles(e) - s.Zone.StandAt(XZ(at), at.y - 1).y).ToString("+0.00;-0.00") + " m over the floor");
                // The rest of the pack (who came in) put down, so the next camp starts clean.
                foreach (var other in s.Enemies.Where(x => x != null && x.actor.IsAlive && x.Engaged).ToList()) other.actor.Health.ApplyDamage(other.actor.Health.Pool.Max * 10);
                yield return new WaitForSeconds(1);
            }
            Finish();
        }
        void Finish()
        {
            if (keptScale > 0) EncounterHud.UiScale = keptScale;
            File.WriteAllText(Path.Combine(dir, "bestiary.txt"), report.ToString());
            Debug.Log("BESTIARY_TOUR_DONE"); Application.Quit(0);
        }
    }
}
