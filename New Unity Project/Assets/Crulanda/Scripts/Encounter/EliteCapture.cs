using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Crulanda.Core;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Shots of social aggro and an elite's moves, for looking at what the tests cannot see (playtest notes 2 and 3). Run the
    /// player windowed (IMGUI is in the shots), on a temp save: Crulanda.exe --crulanda-elite-capture &lt;folder&gt;
    /// [--crulanda-zone zone.oakhaven]. On a clear patch by the zone's recovery point, with the village put away:
    /// 01-pack-pull: three pack beasts a second after one was struck, all coming; 02-shout: a caller's word over his head and
    /// his line in the chat, his campmates not yet moving; 03-camp-comes: the same two seconds on; 04-heavy-blow: the zone's
    /// elite (a dungeon boss if it has one) part way through its wind-up, the mark on the ground and the cast bar under the
    /// target frame; 05-enraged: the same elite under its enrage mark, "ENRAGED" in the target frame.
    /// </summary>
    public sealed class EliteCapture : MonoBehaviour
    {
        public const string Flag = "--crulanda-elite-capture";
        public static bool Requested { get { return Array.IndexOf(Environment.GetCommandLineArgs(), Flag) >= 0; } }
        static readonly Vector2[] Spots = { new Vector2(0, 0), new Vector2(2.6f, 1.2f), new Vector2(-2.4f, 1.8f) };
        static readonly Dictionary<EncounterEnemy, Renderer[]> drawn = new Dictionary<EncounterEnemy, Renderer[]>();

        public IEnumerator Run(EncounterSession s, string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSeconds(2);
            var zone = Crulanda.World.ZoneBuilder.Active;
            var pack = s.Enemies.FindAll(e => e != null && e.Camp && !e.Elite && !e.Hidden && e.actor.IsAlive && e.Social == SocialKind.Pack);
            var people = new List<EncounterEnemy>();
            foreach (var e in s.Enemies)
                if (e != null && e.Camp && !e.Elite && e.actor.IsAlive && e.Social == SocialKind.Call && (people.Count == 0 || e.CampIndex == people[0].CampIndex)) people.Add(e);
            var elite = s.Enemies.Find(e => e != null && e.Camp && e.Elite && e.Move != null && e.Move.boss && e.actor.IsAlive) ?? s.Enemies.Find(e => e != null && e.Camp && e.Elite && e.Move != null && e.actor.IsAlive);
            if (zone == null || pack.Count < 3 || people.Count < 2 || elite == null) { Debug.LogError("Elite capture: needs a zone with three pack beasts, a camp of two people and an elite."); Application.Quit(1); yield break; }
            // The elite's level, and health enough to stand through all of it.
            s.Progress.experience = EncounterProgress.XpForLevel(elite.actor.Level); s.Player.SetLevel(elite.actor.Level);
            s.Player.Stats.SetBase(StatType.MaxHealth, 100000); s.Player.Health.ApplyHealing(100000);
            var weather = Crulanda.World.WorldWeather.Active; if (weather != null) weather.Force(weather.TourKind(), true);
            Crulanda.World.WorldClock.Hour = 12;
            foreach (var e in s.Enemies) if (e != null) Away(e);
            if (VillageLife.Active != null) VillageLife.Active.gameObject.SetActive(false);
            var spot = WardrobeCapture.FindSpot(zone, out _);
            var motor = s.Player.GetComponent<AdventurerMotor>();

            // 01: a pack comes together.
            var wolves = pack.GetRange(0, 3);
            Stage(zone, wolves, spot); Stand(zone, s, motor, spot);
            yield return new WaitForSeconds(.6f);
            s.Select(wolves[0]); wolves[0].Receive(1, s.Player);
            yield return new WaitForSeconds(.9f);
            yield return Shot(directory, "01-pack-pull.png");
            foreach (var e in wolves) Away(e);

            // 02 and 03: a call, and the camp coming after the beat.
            var band = people.GetRange(0, Mathf.Min(3, people.Count));
            Stage(zone, band, spot); Stand(zone, s, motor, spot);
            yield return new WaitForSeconds(.6f);
            s.Select(band[0]); band[0].Receive(1, s.Player);
            yield return new WaitForSeconds(.45f);
            yield return Shot(directory, "02-shout.png");
            yield return new WaitForSeconds(1.6f);
            yield return Shot(directory, "03-camp-comes.png");
            foreach (var e in band) Away(e);

            // 04: the elite part way through its heavy blow, targeted, the player in the mark.
            Stage(zone, new List<EncounterEnemy> { elite }, spot);
            motor.Teleport(zone.Ground(spot + new Vector2(0, -2.2f), 1.1f)); motor.SetView(0, 30, 9);
            yield return new WaitForSeconds(.6f);
            s.Select(elite); elite.Receive(1, s.Player);
            for (float t = 0; t < 20 && !elite.WindingUp; t += Time.deltaTime) yield return null;
            if (!elite.WindingUp) Debug.LogError("Elite capture: " + elite.Name + " never drew back for its heavy blow.");
            yield return new WaitForSeconds(elite.Move.windup * .6f);
            yield return Shot(directory, "04-heavy-blow.png");
            // 05: enraged.
            for (float t = 0; t < 4 && elite.WindingUp; t += Time.deltaTime) yield return null;
            elite.actor.Health.ApplyDamage(Mathf.RoundToInt(elite.actor.Health.Pool.Max * (1 - elite.Move.enrageAt + .05f)));
            yield return new WaitForSeconds(.7f);
            if (!elite.Enraged) Debug.LogError("Elite capture: " + elite.Name + " did not enrage.");
            yield return Shot(directory, "05-enraged.png");
            Debug.Log("ELITE_CAPTURE_DONE");
            Application.Quit(0);
        }
        /// <summary>Out of the picture: fight forgotten, script off, not drawn.</summary>
        static void Away(EncounterEnemy e)
        {
            e.ResetFight(); e.enabled = false;
            var on = Array.FindAll(e.GetComponentsInChildren<Renderer>(true), r => r.enabled);
            if (on.Length > 0) drawn[e] = on;   // only what was drawn is drawn again (Stage)
            foreach (var r in on) r.enabled = false;
        }
        /// <summary>Back in the picture, stood on the patch (the first in the middle) with that as home.</summary>
        static void Stage(Crulanda.World.ZoneBuilder zone, List<EncounterEnemy> cast, Vector2 spot)
        {
            for (int i = 0; i < cast.Count; i++)
            {
                var e = cast[i]; e.enabled = true;
                if (drawn.TryGetValue(e, out var on)) { foreach (var r in on) if (r != null) r.enabled = true; drawn.Remove(e); }
                e.Rehome(zone.Ground(spot + Spots[i % Spots.Length], 0));
            }
        }
        /// <summary>The player 7 m south of the patch, out of everyone's notice, the camera looking north over the shoulder.</summary>
        static void Stand(Crulanda.World.ZoneBuilder zone, EncounterSession s, AdventurerMotor motor, Vector2 spot)
        {
            motor.Teleport(zone.Ground(spot + new Vector2(0, -7), 1.1f)); motor.SetView(0, 28, 11);
        }
        static IEnumerator Shot(string directory, string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name));
            yield return new WaitForSeconds(.4f);
        }
    }
}
