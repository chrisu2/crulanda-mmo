using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's Glitches that change the way and its wings chosen first (Docs/REWORK_LOWTOWNS_HALLOWS.md "Hallow's Creek", picks 9
    /// and 11; Chris's decisions):
    /// - (pick 9, CANON-EXPANDED: the bakery's bell and its minute of sun, b1 ch.21:82-112; "The timeline ... has begun to loop", ch.21:351;
    ///   each moment GAME-ONLY) besides the bakery's Glitch, the town's resonant things (ZoneDefinition.glitches): E on one, or the Waking
    ///   Hour's bell, sets its minute going (colour and sun round it, the paused near it awake) and changes the way for the rest of the visit:
    ///   the gates it names open and stay open (the handcart rolls out of the lane, the slates fall into a ramp, the creek's splash runs on
    ///   and drains the dark cellar's crawl, the well-boy drops the trapdoor's key). A Glitch only ever opens a way, never shuts one, so the
    ///   run never depends on it: the leader's paths are the navmesh's, and take an opened way when it is shorter.
    /// - (pick 11, GAME-ONLY) the Granary, the Cellars and the Archive open together from the Square; as each wing's lord falls
    ///   (ZoneWing.lord), the tower's camp it names (ZoneWing.eases) loses people (never its last), and the chat says how many wings still
    ///   stand between you and the tower.
    /// No machines (DUNGEON_DESIGN 0.05): a handcart pushed by hand, slates, water, a key.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The kind of a Glitch spot's interactable.</summary>
        public const string GlitchSpotKind = "glitchspot";
        /// <summary>How far round a Glitch spot the paused wake for its minute (metres).</summary>
        public const float GlitchWakeReach = 30;
        /// <summary>The route Glitches set going this visit (their ids): their ways stay open.</summary>
        public IReadOnlyCollection<string> GlitchesFired { get { return glitchesFired; } }
        readonly HashSet<string> glitchesFired = new HashSet<string>();
        readonly Dictionary<ZoneInteractable, ZoneGlitch> glitchSpots = new Dictionary<ZoneInteractable, ZoneGlitch>();
        readonly HashSet<string> wingLordsSeen = new HashSet<string>(), wingsDown = new HashSet<string>();
        bool wingsCounted, wingsTold; float nextWingCheck;

        /// <summary>On arrival (StartHallows): the Glitch spots stand still and grey, their ways shut; the wings not yet counted.</summary>
        void StartGlitches()
        {
            glitchesFired.Clear(); glitchSpots.Clear(); wingLordsSeen.Clear(); wingsDown.Clear(); wingsCounted = wingsTold = false; nextWingCheck = 0;
            if (Zone == null || Zone.Zone.glitches == null) return;
            for (int k = 0; k < Zone.Zone.glitches.Length; k++)
            {
                var g = Zone.Zone.glitches[k]; if (g == null || string.IsNullOrEmpty(g.id)) continue;
                var at = GlitchFeet(g); var facing = Quaternion.Euler(0, g.rotation, 0);
                Transform root = null;
                // The well-boy is one of the paused (a child caught running); the ford's splash is water stood still in the air.
                if (g.look == "boy") { var boy = PausedFigure(at, facing, 5, g.name, 700 + k); pausedFolk.Add(boy); root = boy.transform; }
                else if (g.look == "splash") root = StoneSplash(at, g.name);
                var spot = new ZoneInteractable { name = g.name, prompt = string.IsNullOrEmpty(g.prompt) ? "Touch it" : g.prompt, kind = GlitchSpotKind, position = at + Vector3.up * .5f, root = root };
                Zone.Interactables.Add(spot); glitchSpots[spot] = g;
            }
        }
        /// <summary>Where a Glitch spot stands: on the land, or on the deck at its rise over the yard.</summary>
        Vector3 GlitchFeet(ZoneGlitch g)
        {
            // A rise of 0 is the land; any other is a deck's height over the yard (in the valley, 2026-10-18, a floor's rise can be under 0).
            if (Mathf.Abs(g.rise) < .01f) return Zone.Ground(g.at, 0);
            // Up on a deck: the first floor under a point just over its height (never a roof above it).
            if (Physics.Raycast(new Vector3(g.at.x, Zone.Datum + g.rise + 1.2f, g.at.y), Vector3.down, out var hit, 5.2f, ~0, QueryTriggerInteraction.Ignore)) return hit.point;
            return new Vector3(g.at.x, Zone.Datum + g.rise, g.at.y);
        }
        /// <summary>The ford's splash, stopped: a crown of water gone to pale stone in the air (no collider).</summary>
        Transform StoneSplash(Vector3 at, string name)
        {
            var root = new GameObject(string.IsNullOrEmpty(name) ? "A splash stood still" : name).transform; root.position = at;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = new Color(.7f, .74f, .78f) };
            for (int k = 0; k < 9; k++)
            {
                float a = k * 40 * Mathf.Deg2Rad, r = .35f + (k % 3) * .18f;
                var drop = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(drop.GetComponent<Collider>()); drop.transform.SetParent(root, false);
                drop.transform.localPosition = new Vector3(Mathf.Cos(a) * r, .3f + (k % 4) * .28f, Mathf.Sin(a) * r); drop.transform.localScale = new Vector3(.16f, .3f, .16f);
                drop.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return root;
        }
        /// <summary>What E offers at a Glitch spot: to set it going, or that its minute has passed.</summary>
        string GlitchPrompt(ZoneInteractable i)
        {
            if (!glitchSpots.TryGetValue(i, out var g)) return null;
            return glitchesFired.Contains(g.id) ? g.name + " (its minute has run; the way stays open)" : i.prompt;
        }
        /// <summary>E at a Glitch spot (true: handled).</summary>
        bool UseGlitchSpot(ZoneInteractable i)
        {
            if (!glitchSpots.TryGetValue(i, out var g)) return false;
            if (glitchesFired.Contains(g.id)) { Message("Grey and still again, as it was; but the way it opened stays open."); return true; }
            FireGlitch(g, false);
            return true;
        }
        /// <summary>
        /// A Glitch goes: its ways open for the rest of the visit (and a bar that drops behind never drops on them again), and, set going by
        /// hand while the town sleeps, its minute of colour round it (the paused near it awake, the shadows shifted) until its hold runs out.
        /// The Waking Hour sets them all going together (the Hour has the colour already).
        /// </summary>
        public bool FireGlitch(ZoneGlitch g, bool byHour)
        {
            if (g == null || Zone == null || !glitchesFired.Add(g.id)) return false;
            if (!string.IsNullOrEmpty(g.opens))
                foreach (var raw in g.opens.Split(','))
                {
                    var title = raw.Trim(); if (title.Length == 0) continue;
                    var gate = Zone.Gates.Find(x => x != null && x.Title == title); if (gate == null) continue;
                    gate.ShutsBehind = false;
                    if (!gate.Open) gate.SetOpen(true);
                }
            if (byHour) { Message("Far off across the town, " + Lower(g.name) + " moves with the bell: " + (string.IsNullOrEmpty(g.endText) ? "a way stands open." : g.endText)); return true; }
            if (!string.IsNullOrEmpty(g.text)) Message(g.text);
            if (wokenBy == null && running == null)
            {
                string id = "glitch:" + g.id; wokenBy = id; swingTarget = 1;
                var at = new Vector3(g.at.x, Player != null ? Player.transform.position.y : 0, g.at.y);
                foreach (var f in pausedFolk) if (f != null && Flat(f.transform.position - at) < GlitchWakeReach) f.Wake();
                Zone.ShiftHazards(ShadowShift);
                StartCoroutine(GlitchMinute(g, id));
            }
            return true;
        }
        System.Collections.IEnumerator GlitchMinute(ZoneGlitch g, string id)
        {
            yield return new WaitForSeconds(Mathf.Max(5, g.hold));
            if (wokenBy != id) yield break;   // an event woke the town over it: the event's snap ends the colour
            if (!string.IsNullOrEmpty(g.endText)) Message("The colour drains out of the street round " + Lower(g.name) + ", and the grey comes back. But " + g.endText);
            SleepTown();
        }
        /// <summary>The Waking Hour (EventBegun): every Glitch not yet gone goes with the great bell.</summary>
        void GlitchesWithTheHour()
        {
            if (Zone == null || Zone.Zone.glitches == null) return;
            foreach (var g in Zone.Zone.glitches) if (g != null && !string.IsNullOrEmpty(g.id) && !glitchesFired.Contains(g.id)) FireGlitch(g, true);
        }
        static string Lower(string s) { return string.IsNullOrEmpty(s) ? "it" : char.ToLowerInvariant(s[0]) + s.Substring(1); }

        // ---------- the wings chosen first (pick 11) ----------
        /// <summary>The wings that ease the tower (a lord named), and which of them are done.</summary>
        IEnumerable<ZoneWing> EasingWings()
        {
            if (Zone == null || Zone.Zone.wings == null) yield break;
            foreach (var w in Zone.Zone.wings) if (w != null && !string.IsNullOrEmpty(w.lord)) yield return w;
        }
        /// <summary>How many of the wings that ease the tower still stand (their lord alive this visit).</summary>
        public int WingsStanding()
        {
            int n = 0; foreach (var w in EasingWings()) if (!wingsDown.Contains(w.id)) n++;
            return n;
        }
        /// <summary>Each second: a wing's lord fallen eases the tower; once Main Street is done, the chat says what stands before the tower.
        /// A lord not standing when the visit's mobs first stand (cleared under the lockout) counts as done from the start.</summary>
        void TickWings()
        {
            if (Time.time < nextWingCheck || Zone == null) return;
            nextWingCheck = Time.time + 1;
            bool any = false; foreach (var w in EasingWings()) { any = true; break; }
            if (!any) return;
            if (!wingsCounted)
            {
                bool spawned = false; foreach (var e in Enemies) if (e != null && e.Camp && e.actor != null && e.actor.IsAlive) { spawned = true; break; }
                if (!spawned) return;
                wingsCounted = true;
                foreach (var w in EasingWings()) { if (MobStanding(w.lord)) wingLordsSeen.Add(w.id); else { wingsDown.Add(w.id); EaseTower(w, false); } }
            }
            foreach (var w in EasingWings())
            {
                if (wingsDown.Contains(w.id)) continue;
                if (MobStanding(w.lord)) { wingLordsSeen.Add(w.id); continue; }
                if (!wingLordsSeen.Contains(w.id)) continue;
                wingsDown.Add(w.id); EaseTower(w, true);
                Message(WingsLine());
            }
            // The Square reached (Main Street done): the three wings open together.
            if (!wingsTold && WingOpen("granary") && WingOpen("archive")) { wingsTold = true; Message("From the Square three ways open: the Granary over the footbridge, the Cellars under the town, the Old Archive past the crater. " + WingsLine()); }
        }
        /// <summary>"Two wings stand between you and the tower", by how many still stand.</summary>
        public string WingsLine()
        {
            int n = WingsStanding();
            string[] words = { "No wing", "One wing", "Two wings", "Three wings", "Four wings" };
            string count = n < words.Length ? words[n] : n + " wings";
            if (n == 0) return "No wing stands between you and the tower now; its defenders are fewer for every one you took.";
            return count + (n == 1 ? " stands" : " stand") + " between you and the tower.";
        }
        /// <summary>A wing's lord fallen: the tower's camp it names loses people at rest (easeCount, never its last), with the wing's line.</summary>
        void EaseTower(ZoneWing w, bool say)
        {
            if (string.IsNullOrEmpty(w.eases) || w.easeCount <= 0) return;
            int c = CampNamed(w.eases); if (c < 0) return;
            var mine = new List<EncounterEnemy>();
            foreach (var e in Enemies) if (e != null && e.Camp && e.CampIndex == c && e.actor != null && e.actor.IsAlive) mine.Add(e);
            int gone = 0;
            foreach (var e in mine)
            {
                if (gone >= w.easeCount || mine.Count - gone <= 1) break;
                if (!e.CanAnswer) continue;   // one already fighting stays to fight
                if (Target == e) { Target = null; AutoAttack = false; }
                Enemies.Remove(e); Destroy(e.gameObject); gone++;
            }
            if (say && gone > 0 && !string.IsNullOrEmpty(w.easeText)) Message(w.easeText);
        }
    }
}
