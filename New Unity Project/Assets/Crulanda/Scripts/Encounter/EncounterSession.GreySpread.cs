using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Grey Spreads (Docs/ADIT_REWORK.md E6, stage S7; GAME-ONLY: the Wasting and what it unmakes are CANON, this event is not). Three
    /// lines across the Grey Breach (the zone's anchors "grey line 1".."grey line 3", in the quiet between its fights). When you or one of your
    /// party first walks over one, the grey comes on behind you: the Breach's lamps short of the line go grey and dim (ZoneBuilder.GreyCaveLights),
    /// the static hidden on its walls there shows (ZoneBuilder.ShowGreySeep), and that line's seep of crawlers, waiting 25 m back (ZoneCamp.after
    /// "event:greylineN", raised here: EncounterSession.Release.cs), comes up the drift after whoever crossed. The Foreman Who Forgot's death
    /// stops it: no line after that does anything, and the lamps come back a little. Once a visit, nothing saved (the zone is built afresh on
    /// the next). The file is GreySpread, its members GreyLine and GreySeep: Spread is the area-damage code's (EncounterSession.Spread.cs).
    /// </summary>
    public sealed partial class EncounterSession
    {
        public const string GreyCave = "The Grey Breach", GreyForeman = "The Foreman Who Forgot";
        /// <summary>How much of their light the lamps behind a crossed line keep, and how much of what they lost comes back when the Foreman falls.</summary>
        public static float GreyKeep = .4f, GreyBack = .5f;
        /// <summary>The release key of grey line n (1-based): "event:greyline1".</summary>
        public static string GreyLineKey(int n) { return "event:greyline" + n; }
        /// <summary>How many grey lines have been crossed this visit, and whether the Foreman's death has stopped the seep.</summary>
        public int GreyLinesCrossed { get; private set; }
        public bool GreySeepStopped { get; private set; }
        /// <summary>The lines' places along the Breach, in order (from the zone's anchors; empty in any zone without them).</summary>
        public readonly List<float> GreyLines = new List<float>();
        Hollow greyCave; Coroutine greyWatch;

        /// <summary>On arrival (StartAdit): the lines read from the zone's anchors and watched.</summary>
        void StartGreySeep()
        {
            GreyLinesCrossed = 0; GreySeepStopped = false; GreyLines.Clear(); greyCave = null;
            if (greyWatch != null) { StopCoroutine(greyWatch); greyWatch = null; }
            if (Zone == null || Zone.Zone.anchors == null) return;
            var lines = new List<(int n, float along)>();
            foreach (var a in Zone.Zone.anchors)
                if (a != null && a.cave == GreyCave && a.name != null && a.name.StartsWith(ZoneBuilder.GreyLineAnchor) && int.TryParse(a.name.Substring(ZoneBuilder.GreyLineAnchor.Length), out int n)) lines.Add((n, a.along));
            lines.Sort((x, y) => x.n.CompareTo(y.n));
            foreach (var (_, along) in lines) GreyLines.Add(along);
            greyCave = Hollow.All.Find(h => h.Name == GreyCave);
            if (GreyLines.Count > 0 && greyCave != null) greyWatch = StartCoroutine(WatchGreyLines());
        }
        /// <summary>Four times a second: whether you or a party member stands in the Breach past the next line.</summary>
        IEnumerator WatchGreyLines()
        {
            var wait = new WaitForSeconds(.25f);
            while (!GreySeepStopped && GreyLinesCrossed < GreyLines.Count)
            {
                yield return wait;
                if (Zone == null || Paused || greyCave == null) continue;
                var who = GreyCrosser(GreyLines[GreyLinesCrossed]);
                if (who != null) CrossGreyLine(who);
            }
            greyWatch = null;
        }
        /// <summary>The first of you, your party and Mira (alive, on the Breach's own floor) past <paramref name="line"/> metres along it; else null.</summary>
        Actor GreyCrosser(float line)
        {
            foreach (var a in PartyActors())
            {
                if (a == null || !a.IsAlive) continue;
                var p = a.transform.position; var flat = new Vector2(p.x, p.z);
                if (!greyCave.FloorAt(flat, out float floor) || Mathf.Abs(p.y - floor) > 3.5f) continue;   // in another cave over or under it
                greyCave.Local(flat, out float along, out _);
                if (along >= line) return a;
            }
            return null;
        }
        /// <summary>The next grey line crossed by <paramref name="who"/>: the lamps behind it grey, its static shows, its seep comes. Returns the line (1-based), or 0 when the seep is stopped or every line is crossed.</summary>
        public int CrossGreyLine(Actor who)
        {
            if (GreySeepStopped || Zone == null || GreyLinesCrossed >= GreyLines.Count) return 0;
            int n = ++GreyLinesCrossed; float line = GreyLines[n - 1];
            Zone.GreyCaveLights(GreyCave, line, GreyKeep);
            Zone.ShowGreySeep(n);
            ReleaseAfter(GreyLineKey(n), who != null && who.IsAlive ? who : LiveFoe(), null, GreySeepLine(n), 0);
            return n;
        }
        static string GreySeepLine(int n)
        {
            switch (n)
            {
                case 1: return "Behind you the lamps go grey, one after another, and the rock you walked past is not the colour it was. Something comes up the drift after you.";
                case 2: return "The grey comes on behind you again, closer this time. The lamps you passed are pale as ash, and more crawlers come out of them.";
                default: return "The drift behind you goes grey to the last lamp. The crawlers come out of it as if the rock had let them go.";
            }
        }
        /// <summary>A mob's death (EncounterSession's kill): the Foreman's stops the seep and brings the lamps back a little.</summary>
        void GreySeepOnDeath(EncounterEnemy enemy)
        {
            if (enemy == null || !enemy.Camp || enemy.MobName != GreyForeman || GreySeepStopped || greyCave == null) return;
            GreySeepStopped = true;
            if (GreyLinesCrossed > 0 && Zone != null) Zone.BringBackCaveLights(GreyCave, GreyBack);
            Message(GreyLinesCrossed > 0 ? "With the Foreman gone the grey stops creeping. Behind you the lamps come back, a little, and stay." : "With the Foreman gone the grey in the walls goes still.");
        }
    }
}
