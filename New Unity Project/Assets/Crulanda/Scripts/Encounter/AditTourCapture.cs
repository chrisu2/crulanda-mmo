using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Crulanda.Core;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A camera tour of the Sealed Adit (2026-10-15: the whole dungeon looked at from frames, without playing it), modelled on the
    /// world tour (EncounterCapture.CaptureWorldTour): the yard and the cut, then one frame per named place cave by cave in the order
    /// a run takes them, each from a spot a few metres up looking along the cave, in the cave's own terms (Hollow.At: metres in from
    /// the mouth, metres to the right, metres over the floor). Every gate stands open for it (ZoneBuilder.OpenGates), the HUD is
    /// hidden, the weather is the calm tour kind, the camera is set by hand with the orbit off (the clock reads the camera's depth in
    /// the cave for its light and fog), the player stands hidden on the floor under it, no mob notices them (EncounterSession.Unseen,
    /// the /invis switch) and they have health enough to stand anything that does. Mira is warped along so she is not left pathing
    /// across three kilometres of cave.
    /// Crulanda.exe --crulanda-adit-tour &lt;folder&gt; --crulanda-zone zone.adit --crulanda-temp-save.
    /// Writes adit-NN-&lt;place&gt;.png and adit-tour.txt (what each frame should show and where the camera stood); ADIT_TOUR_DONE in
    /// the log. A place whose cave is not in the zone's data is skipped and the text says so.
    /// </summary>
    public sealed class AditTourCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-adit-tour";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        string dir; EncounterSession session; readonly System.Text.StringBuilder report = new System.Text.StringBuilder(); int shot;

        const string Adit = "The Sealed Adit", Flooded = "The Flooded Working", Pen = "The Pen", Gallery = "The Singing Gallery", Drift = "The Cutting Drift",
            Foundry = "The Geode Foundry", Ash = "The Ash Garden", Scriptorium = "The Scriptorium", Muster = "The Muster Yard", Chapel = "The Chapel of Embers",
            River = "The Sunless River", Ledge = "The Dry Ledge", Breach = "The Grey Breach", Deep = "The Deep Way", Pit = "The Pitch Pit";

        /// <summary>A cave by its name (the "cavern" props of adit.json), or null.</summary>
        static Hollow Cave(string name) { return Hollow.All.Find(h => h.Name == name); }
        /// <summary>
        /// A point in a cave: <paramref name="along"/> metres in, <paramref name="aside"/> to the right (kept a metre inside the walls) and
        /// <paramref name="up"/> over the floor (kept under the roof, which comes down toward the walls as Hollow.Open has it; a deck's
        /// rise is part of up). Over water the point is lifted clear of the surface, as the orbit camera keeps itself.
        /// </summary>
        static Vector3 In(Hollow h, float along, float aside, float up)
        {
            int i = 0; while (i + 1 < h.Along.Count && h.Along[i + 1] < along) i++;
            float half = h.Half[i]; aside = Mathf.Clamp(aside, -(half - 1), half - 1);
            float u = Mathf.Abs(aside) / half, roof = h.Height[i] * Mathf.Pow(Mathf.Max(0, 1 - Mathf.Pow(u, 8f / 3)), .425f);
            var p = h.At(along, aside) + Vector3.up * Mathf.Min(up, roof * .8f);
            var zone = ZoneBuilder.Active;
            if (zone != null && zone.WaterAtPoint(p, out float surface, out _) && p.y < surface + 1.2f) p.y = surface + 1.2f;
            return p;
        }
        static string V(Vector3 p) { return p.x.ToString("0.0") + ", " + p.y.ToString("0.0") + ", " + p.z.ToString("0.0"); }

        /// <summary>
        /// One frame: the mobs' fights dropped, the player (hidden) and Mira put at <paramref name="stand"/>, the camera held at
        /// <paramref name="eye"/> looking at <paramref name="at"/> for a second and a bit (the fog and the light settle to the cave's depth),
        /// then the shot, named adit-NN-<paramref name="place"/>.png, and a line in the report.
        /// </summary>
        IEnumerator Frame(string place, Vector3 eye, Vector3 at, Vector3 stand, string where)
        {
            var motor = session.Player.GetComponent<AdventurerMotor>(); var view = session.View.transform;
            foreach (var e in session.Enemies) if (e != null && e.actor.IsAlive) e.ResetFight();
            motor.Teleport(stand);
            if (session.Companion != null) { var agent = session.Companion.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent != null && agent.isActiveAndEnabled) agent.Warp(stand - Vector3.up * .1f); }
            var look = Quaternion.LookRotation(at - eye);
            for (float w = 0; w < 1.2f; w += Time.deltaTime) { view.SetPositionAndRotation(eye, look); yield return null; }
            view.SetPositionAndRotation(eye, look);
            string file = "adit-" + (++shot).ToString("00") + "-" + place + ".png";
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, file));
            report.AppendLine(file + ": " + where + " (camera at " + V(eye) + ", looking at " + V(at) + ")");
            Debug.Log("Adit tour: " + file + " " + where);
            for (float w = 0; w < .5f; w += Time.deltaTime) { view.SetPositionAndRotation(eye, look); yield return null; }
        }

        public IEnumerator Run(EncounterSession s, string directory)
        {
            dir = directory; Directory.CreateDirectory(dir); session = s;
            // The zone and the player first (the Adit's caves take a while to build), then a few seconds for the camps and the navmesh.
            float deadline = Time.time + 120;
            while ((ZoneBuilder.Active == null || s.Player == null || Hollow.All.Count == 0) && Time.time < deadline) yield return null;
            var zone = ZoneBuilder.Active;
            if (zone == null || s.Player == null) { Debug.LogError("Adit tour: no zone or no player after two minutes."); Application.Quit(1); yield break; }
            yield return new WaitForSeconds(3);
            EncounterHud.Hidden = true; EncounterSession.Unseen = true;
            WorldClock.Hour = 11; WorldClock.Advance(0);
            var weather = WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            s.Player.Stats.SetBase(StatType.MaxHealth, 100000); s.Player.Health.ApplyHealing(100000);
            zone.OpenGates(); yield return new WaitForSeconds(1);   // every door, bar and grille up, for the tour's eye
            var motor = s.Player.GetComponent<AdventurerMotor>(); motor.enabled = false;   // the camera is ours until the end
            // A lamp on the camera for the frames' sake: the caves' own lamps do not carry at the tour's eye, and the first run's frames
            // came out near black (2026-10-15 00:55). The frames are for looking at the dungeon, not at its lighting. (The ambient is the
            // clock's, set again every frame: a lamp, not a lifted ambient.)
            var lamp = new GameObject("Tour lamp").AddComponent<Light>(); lamp.transform.SetParent(s.View.transform, false);
            lamp.type = LightType.Point; lamp.range = 60; lamp.intensity = .8f;   // a quarter of the first try: at 3.2 the chapel's stone came out white (Chris, 2026-10-15 01:10: "looks bad") lamp.color = new Color(1, .93f, .8f); lamp.shadows = LightShadows.None;
            var body = Array.FindAll(s.Player.GetComponentsInChildren<Renderer>(), r => r.enabled); foreach (var r in body) r.enabled = false;
            report.AppendLine("The Sealed Adit, camera tour (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "): zone " + zone.Zone.id + ", " + Hollow.All.Count + " caves, " + zone.Gates.Count + " gates opened.");

            var shots = new List<(string place, Vector3 eye, Vector3 at, Vector3 stand, string where)>();
            // A frame in a cave: the camera along/aside metres in, rise (a deck's) plus up over the floor, looking at a point in a cave
            // (the same one, or another: the Gallery from the overlook, the Foundry from the Drift's lip).
            void Shot(string place, string cave, float along, float aside, float rise, float up, string toCave, float toAlong, float toAside, float toUp, string where)
            {
                var h = Cave(cave); var t = Cave(toCave);
                if (h == null || t == null) { report.AppendLine(place + ": no cave called \"" + (h == null ? cave : toCave) + "\" in this zone, skipped"); return; }
                shots.Add((place, In(h, along, aside, rise + up), In(t, toAlong, toAside, toUp), In(h, along, aside, rise + 1.1f), where));
            }
            var adit = Cave(Adit);
            // The yard outside: from behind the waystone at the cut back up to the Peaks, over the yard to the Adit's mouth.
            var mouth = adit != null ? adit.At(0) : zone.Ground(new Vector2(0, -370));
            var exitAt = zone.Zone.exits.Length > 0 ? zone.Zone.exits[0].at : new Vector2(0, -393);
            var yardAt = exitAt + new Vector2(-5, -5);
            shots.Add(("yard", zone.Ground(yardAt, 4.5f), mouth + Vector3.up * 1.5f, zone.Ground(yardAt, 1.1f), "the yard outside, from behind the waystone of the cut back up to the Peaks, the Adit's mouth ahead"));
            // The cut: the Sealed Adit's mouth from ten metres out in the yard, looking in.
            if (adit != null)
            {
                var fwd = adit.At(6) - adit.At(0); fwd.y = 0; fwd.Normalize();
                var eye = mouth - fwd * 10 + Vector3.up * 2.2f; eye.y = Mathf.Max(eye.y, zone.HeightAt(eye.x, eye.z) + 1.6f);
                shots.Add(("cut", eye, adit.At(8) + Vector3.up * 1.6f, zone.Ground(new Vector2(eye.x, eye.z), 1.1f), "the cut: the Sealed Adit's mouth from ten metres out in the yard"));
            }
            else report.AppendLine("cut: no cave called \"" + Adit + "\" in this zone, skipped");
            // The workings (the Sealed Adit's own passage), the Pen and the Flooded Working off it.
            Shot("cage-bowl", Adit, 47, 7, 0, 5, Adit, 84, -3, 1.5f, "the Cage Bowl from its south lip: the overseers' walk along the west wall, the cages ahead");
            Shot("overseers-walk", Adit, 56, -10.5f, 2.3f, 1.7f, Adit, 82, -4, 2, "the overseers' walk from its south steps, looking along it to the cages");
            Shot("tally-hall", Adit, 172, 0, 0, 3.6f, Adit, 194, 0, 5, "Lusk's tally hall from the drift at its door: the south ramp and the stage");
            Shot("tally-stage", Adit, 183, -8, 0, 3.5f, Adit, 195, 1, 4.5f, "Lusk's tally stage from the hall floor, Lusk and his guards on it");
            Shot("pen", Pen, 30, 0, 0, 3, Pen, 52, 0, 1.2f, "the Pen past its gate: the pit-hounds and Old Bristle");
            Shot("flooded-working", Flooded, 42, 0, 0, 3.2f, Flooded, 72, 0, 1, "the Flooded Working: the bailers' chain and Brack the Bailer");
            // The Singing Gallery: from the overlook at the Adit's end, then its trestle, bridge and island from the floor.
            Shot("overlook", Adit, 375, 2, 0, 1.8f, Gallery, 196.7f, 0, 3, "the Singing Gallery from the overlook balcony at the Adit's end");
            Shot("miners-trestle", Gallery, 200, -8, 0, 2.8f, Gallery, 177, -27, 12.5f, "the Miners' Trestle from the Gallery floor: its landing and the long way down");
            Shot("rope-bridge", Gallery, 175.2f, -26, 12, 1.7f, Gallery, 175.2f, 8, 12.3f, "the rope bridge from the trestle landing, its tower midway");
            Shot("island", Gallery, 233, 7, 0, 3, Gallery, 212.7f, -4, 2.4f, "the island and its causeways, from the Gallery's east floor");
            // The foundry wing: the Cutting Drift, the Foundry from the Drift's lip and from its floor.
            var drift = Cave(Drift);
            Shot("cutting-drift", Drift, 48, 0, 0, 3.4f, Drift, 78, 0, 1.5f, "the Cutting Drift: the cutting floor");
            Shot("foundry-from-lip", Drift, drift != null ? drift.Length - 6 : 0, 0, 0, 1.8f, Foundry, 95, 0, 5, "the Geode Foundry from the haul-drift's lip: the furnace below, the catwalks");
            Shot("foundry-floor", Foundry, 142, 2, 0, 3.5f, Foundry, 95, 0, 5, "the Geode Foundry floor from the berth end: the furnace, the catwalk above, the walkway");
            // The vent wing: the Ash Garden, the Scriptorium, the Muster Yard, the Chapel and its cloister stair.
            Shot("ash-garden", Ash, 50, 0, 0, 3.5f, Ash, 82, 0, 1, "the Ash Garden past its gatehouse: the urns, the Urn-Keeper at the far end");
            Shot("scriptorium", Scriptorium, 16, 0, 0, 3, Scriptorium, 48, 0, 1.2f, "the Scriptorium: the copyists' desks and the Loremaster");
            Shot("muster-yard", Muster, 42, 0, 0, 3.5f, Muster, 82, 0, 1.2f, "the Muster Yard: the zealots in ranks, the armourers and Corvane");
            Shot("chapel-of-embers", Chapel, 26, 0, 0, 3.5f, Chapel, 63, 0, 1, "the Chapel of Embers from inside its east door: the nave, the lake of fire, the altar at the far end");   // inside the nave (from its door the frame was the east wall with the fire through the arch: Chris, 01:10)
            Shot("cloister-stair", Chapel, 88, 0, 0, 2.6f, Chapel, 118, 0, 1.2f, "the cloister stair past the cloister door, on toward the Gallery");
            // The river wing and the Breach.
            Shot("sunless-river", River, 30, 0, 0, 3, River, 72, 0, .5f, "the Sunless River from near its mouth, looking downstream");
            Shot("lurkers-pool", River, 136, 0, 0, 3.5f, River, 160, 0, .5f, "the lurker's pool on the Sunless River");
            Shot("dry-ledge", Ledge, 14, 0, 0, 3, Ledge, 52, 0, 1, "the Dry Ledge: the Hollow sump and the pillar whisperers");
            Shot("grey-breach", Breach, 100, 0, 0, 3.5f, Breach, 140, 0, 1, "the Grey Breach: the grey den and the Whisperer's hollow");
            Shot("grey-breach-foreman", Breach, 152, 0, 0, 3.2f, Breach, 178, 0, 1.2f, "the Foreman's working at the Breach's end, the sluice-boards beyond");
            // The deep: the Deep Way's stair, the Rail Hall and the Pitch Pit off it.
            Shot("deep-way", Deep, 20, 0, 0, 3.5f, Deep, 60, 0, .5f, "the Deep Way past the singing door: the stair down");
            Shot("rail-hall", Deep, 178, 0, 0, 4.5f, Deep, 232, 0, 3, "the Rail Hall from the platform gate: the platform, the carriage and its line");
            Shot("pitch-pit", Pit, 8, 0, 0, 2.6f, Pit, 42, 0, .5f, "the Pitch Pit from its mouth off the Rail Hall");

            foreach (var f in shots) yield return Frame(f.place, f.eye, f.at, f.stand, f.where);

            motor.enabled = true; foreach (var r in body) r.enabled = true;
            EncounterHud.Hidden = false; EncounterSession.Unseen = false;
            File.WriteAllText(Path.Combine(dir, "adit-tour.txt"), report.ToString());
            Debug.Log("ADIT_TOUR_DONE"); Application.Quit(0);
        }
    }
}
