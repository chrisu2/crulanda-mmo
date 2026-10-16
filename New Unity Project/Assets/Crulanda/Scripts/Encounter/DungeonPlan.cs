using System;
using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A step of a dungeon run a sim leads (SimCompanion.LeadDungeon): one fight (a camp, with the camps of the same wing and step: a boss
    /// and his guards are one fight), or a regroup at a wing's home (where the way out of a wing comes down, the party gathered before the
    /// next). GAME-ONLY (the Adit rework, Docs/ADIT_REWORK.md section 9).
    /// </summary>
    public sealed class RouteStep
    {
        /// <summary>Where the leader goes: where the camp stands (on its floor, or on its deck for a camp up on one), the anchor for a regroup.</summary>
        public Vector3 at;
        /// <summary>What it is called: the camp's name (the boss's camp for a boss's fight), the anchor's for a regroup.</summary>
        public string name;
        /// <summary>The wing it belongs to (ZoneWing.id) and the wing's name; null in a dungeon without wings.</summary>
        public string wing, wingName;
        /// <summary>An event's id (the Wake, stage S4 of the rework): not yet planned.</summary>
        public string ev;
        /// <summary>Its camps (indices into ZoneDefinition.camps), the main one first (the boss of the fight); none for a regroup.</summary>
        public int[] camps = new int[0];
        /// <summary>boss: an elite among its camps. optional: a side room or side boss the run may leave (ZoneCamp.optional). regroup: the
        /// party gathers here (no fight). follow: its camp walks a road (a patrolling side boss), so the leader follows it.</summary>
        public bool boss, optional, regroup, follow;
        /// <summary>The lowest level among its camps.</summary>
        public int level;
        /// <summary>The gate across the way before it (D7: shut, the run waits at it), or null.</summary>
        public ZoneGate gate;
        /// <summary>The main camp (the boss's), or -1.</summary>
        public int Camp { get { return camps.Length > 0 ? camps[0] : -1; } }
    }

    /// <summary>
    /// The order of a dungeon run through a web of wings (the Adit rework, Docs/ADIT_REWORK.md section 9; GAME-ONLY; lessons from the
    /// Wailing Caverns and the Deadmines, nothing copied). The dungeon's data (ZoneDefinition.wings, ZoneCamp.wing and step) says which
    /// camps are each wing's fights and in what order, which wings open after which (a group: one side of a fork is enough), where each
    /// wing's way home comes down (its home anchor) and what a run may leave (optional camps and wings). The planner:
    /// - takes the wings in turn: of those open, the nearest from where the last one ended (by the way there, and keeping to the height
    ///   the party is at: enter high, work down), a fork's side that a live quest wants first;
    /// - runs each wing's fights in its own order, so no wing is walked twice, and regroups at its home when its way out is a drop or comes
    ///   down beyond its last fight;
    /// - in the standard run takes the side bosses inside the wings it goes through (an elite off the way: a kennel, a beast round a pool);
    ///   in the full run every optional camp and wing and both sides of a fork; in the quick run only what must be done;
    /// - fits each such side fight into the wing it is reached from where it costs the least walking (a side room is cleared as the run
    ///   passes its mouth, not walked back to).
    /// Pure: the caller gives where things stand and how far it is between them (SimCompanion: the navmesh), so tests can drive it.
    /// </summary>
    public static class DungeonPlan
    {
        /// <summary>quick: what must be done; standard: that and the side bosses in the wings it takes; full: everything that stands.</summary>
        public enum Mode { Quick, Standard, Full }
        /// <summary>The run a word asks for ("/dungeon quick", "/dungeon full"; anything else the standard run).</summary>
        public static Mode ParseMode(string word)
        {
            var w = (word ?? "").Trim().ToLowerInvariant();
            if (w == "quick" || w == "short" || w == "fast") return Mode.Quick;
            if (w == "full" || w == "all" || w == "everything") return Mode.Full;
            return Mode.Standard;
        }
        /// <summary>How the leader names the run in words.</summary>
        public static string Describe(Mode m) { return m == Mode.Quick ? "the quick run" : m == Mode.Full ? "the full run" : "the run"; }
        /// <summary>A camp that is one of its wing's fights: a step of its wing's run (1, 2...), there from the start, not optional, not a patrol
        /// (a patrol comes to the party).</summary>
        public static bool RequiredCamp(ZoneCamp cp) { return cp != null && string.IsNullOrEmpty(cp.after) && cp.step > 0 && !cp.optional && !Patrols(cp); }
        static bool Patrols(ZoneCamp cp) { return cp.route != null && cp.route.Length > 1; }
        /// <summary>Each metre up or down from where the party stands counts as this many metres walked, choosing the next wing (keep to the
        /// height you are at: a door off the bridge before the trestle down).</summary>
        public const float HeightWeight = 3;
        /// <summary>Side fights within this many metres of each other in one cave and wing are one fight (a side boss and his people).</summary>
        public const float SameFight = 12;

        /// <summary>
        /// The run's steps in order (and, into <paramref name="wingOrder"/> when given, the names of the wings it goes through in turn).
        /// campAt: where a camp stands; anchorAt: where a named anchor stands (null when the zone has none of that name); distance: how far
        /// to walk between two places; standing: whether a camp has anything alive (a camp cleared, or a rare one not here, is left out);
        /// wanted: whether a live quest wants a camp's elite.
        /// </summary>
        public static List<RouteStep> Plan(ZoneDefinition zone, Mode mode, Vector3 from, Func<int, Vector3> campAt, Func<string, Vector3?> anchorAt,
            Func<Vector3, Vector3, float> distance, Func<int, bool> standing, Func<int, bool> wanted, List<string> wingOrder)
        {
            var route = new List<RouteStep>(); if (wingOrder != null) wingOrder.Clear();
            if (zone == null || zone.wings == null || zone.camps == null) return route;
            var camps = zone.camps; bool full = mode == Mode.Full;
            var wings = new List<ZoneWing>(); foreach (var w in zone.wings) if (w != null && !string.IsNullOrEmpty(w.id)) wings.Add(w);
            ZoneWing WingOf(string id) { return string.IsNullOrEmpty(id) ? null : wings.Find(w => w.id == id); }

            // Each wing's own fights, in its order: its camps with a step, the camps of one step one fight.
            var steps = new Dictionary<string, List<RouteStep>>();
            foreach (var w in wings)
            {
                var byStep = new SortedDictionary<int, List<int>>();
                for (int c = 0; c < camps.Length; c++)
                {
                    var cp = camps[c]; if (cp == null || cp.wing != w.id || !RequiredCamp(cp) || !standing(c)) continue;
                    if (!byStep.TryGetValue(cp.step, out var list)) byStep[cp.step] = list = new List<int>();
                    list.Add(c);
                }
                var mine = new List<RouteStep>(); foreach (var kv in byStep) mine.Add(Step(camps, w, kv.Value, campAt, false));
                steps[w.id] = mine;
            }

            // What a run may leave, by the wing it is reached from (the camp's own wing, or the wing an optional wing opens after): the
            // standard run takes an optional elite in a wing it goes through; the full run takes everything that stands.
            var extras = new Dictionary<string, List<int>>();
            for (int c = 0; c < camps.Length; c++)
            {
                var cp = camps[c]; if (cp == null || !string.IsNullOrEmpty(cp.after) || RequiredCamp(cp) || !standing(c)) continue;
                var w = WingOf(cp.wing); if (w == null) continue;
                if (!cp.optional && !w.optional) continue;   // a patrol of the way: it comes to the party
                bool take = full || (mode == Mode.Standard && !w.optional && cp.elite);
                if (!take) continue;
                string host = w.optional ? FirstAfter(w) : w.id; if (WingOf(host) == null) continue;   // (an optional wing that opens after nothing has no way in to fit it to)
                if (!extras.TryGetValue(host, out var list)) extras[host] = list = new List<int>();
                list.Add(c);
            }

            // The wings in turn.
            var placed = new HashSet<string>(); var groupsDone = new HashSet<string>();
            var pending = new List<ZoneWing>(); foreach (var w in wings) if (!w.optional || string.IsNullOrEmpty(FirstAfter(w)) && full) pending.Add(w);
            bool Met(ZoneWing w)
            {
                if (string.IsNullOrEmpty(w.after)) return true;
                foreach (var raw in w.after.Split(','))
                {
                    var id = raw.Trim(); if (id.Length == 0) continue;
                    var other = WingOf(id);
                    if (other != null) { if (!placed.Contains(id) && !other.optional) return false; continue; }   // an optional wing (fitted into another, or left out) holds nothing up
                    if (wings.Exists(o => o.group == id) && !groupsDone.Contains(id)) return false;   // a group: one of its wings is enough
                }
                return true;
            }
            bool Wanted(ZoneWing w) { if (wanted == null) return false; foreach (var s in steps[w.id]) foreach (int c in s.camps) if (camps[c].elite && wanted(c)) return true; return false; }
            // A wing's fights into the run (its side fights fitted in), its regroup after them; returns where the run is then.
            Vector3 Place(ZoneWing w, Vector3 entry)
            {
                var mine = new List<RouteStep>(steps[w.id]);
                // Its home: where its way out comes down (a drop: the party lands scattered), or a home beyond its last fight (not one back
                // toward its first: a run does not walk back to regroup).
                Vector3? home = !string.IsNullOrEmpty(w.home) && anchorAt != null ? anchorAt(w.home) : null;
                if (mine.Count == 0) home = null;
                else if (home.HasValue && w.exit != "drop" && Flat(mine[0].at, home.Value) <= Flat(mine[0].at, mine[mine.Count - 1].at)) home = null;
                if (extras.TryGetValue(w.id, out var extra))
                    foreach (var fight in Fights(camps, extra, campAt))
                        Insert(mine, Step(camps, WingOf(camps[fight[0]].wing) ?? w, fight, campAt, true), entry, home, distance);
                // (Hallow's Creek) An optional wing fitted in here whose way out is a gate of its own (ZoneWing.gate: the Cellars come up through
                // the well-house trapdoor, barred from below): after its last fight the party regroups at its home, through that gate, and is
                // never walked back through it.
                foreach (var ow in wings)
                {
                    if (!ow.optional || string.IsNullOrEmpty(ow.gate) || string.IsNullOrEmpty(ow.home) || anchorAt == null) continue;
                    int last = mine.FindLastIndex(s => s.wing == ow.id && !s.regroup); if (last < 0) continue;
                    var at = anchorAt(ow.home); if (!at.HasValue) continue;
                    mine.Insert(last + 1, new RouteStep { at = at.Value, name = ow.home, wing = ow.id, wingName = ow.name, regroup = true });
                }
                if (mine.Count == 0) return entry;
                if (wingOrder != null) wingOrder.Add(w.name);
                route.AddRange(mine);
                if (!home.HasValue) return mine[mine.Count - 1].at;
                route.Add(new RouteStep { at = home.Value, name = w.home, wing = w.id, wingName = w.name, regroup = true });
                return home.Value;
            }
            var origin = from; string lastGroup = null;
            while (pending.Count > 0)
            {
                if (!full) pending.RemoveAll(w => !string.IsNullOrEmpty(w.group) && groupsDone.Contains(w.group));   // one side of a fork is enough
                if (pending.Count == 0) break;
                var open = pending.FindAll(Met);
                if (open.Count == 0) open.Add(pending[0]);   // (data naming a wing it has not got: on in the data's order)
                // A side of a fork a live quest wants decides the fork (The Margins: the Loremaster's library); its other sides wait.
                foreach (var w in open.ToArray()) if (!string.IsNullOrEmpty(w.group) && open.Contains(w) && Wanted(w)) open.RemoveAll(o => o != w && o.group == w.group && !Wanted(o));
                // The full run takes a fork's other side before it goes on.
                if (full && lastGroup != null && open.Exists(o => o.group == lastGroup)) open.RemoveAll(o => o.group != lastGroup);
                // The nearest: by the way there, and keeping to the height the party is at. A wing with nothing standing costs nothing.
                ZoneWing pick = null; float best = float.MaxValue;
                foreach (var w in open)
                {
                    var s = steps[w.id];
                    float d = s.Count == 0 ? 0 : distance(origin, s[0].at) + HeightWeight * Mathf.Abs(origin.y - s[0].at.y);
                    if (d < best) { best = d; pick = w; }
                }
                pending.Remove(pick); placed.Add(pick.id); if (!string.IsNullOrEmpty(pick.group)) groupsDone.Add(pick.group); lastGroup = pick.group;
                origin = Place(pick, origin);
            }
            return route;
        }
        /// <summary>The first wing an optional wing opens after (the wing it is reached from), or null.</summary>
        static string FirstAfter(ZoneWing w)
        {
            if (string.IsNullOrEmpty(w.after)) return null;
            foreach (var raw in w.after.Split(',')) { var id = raw.Trim(); if (id.Length > 0) return id; }
            return null;
        }
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        /// <summary>A step of these camps (one fight): led by its boss (the first elite), or else the first camp.</summary>
        static RouteStep Step(ZoneCamp[] camps, ZoneWing w, List<int> list, Func<int, Vector3> campAt, bool optional)
        {
            int main = list[0]; foreach (int c in list) if (camps[c].elite && !camps[main].elite) main = c;
            var ordered = new List<int> { main }; foreach (int c in list) if (c != main) ordered.Add(c);
            bool boss = false; int lo = int.MaxValue; foreach (int c in list) { boss |= camps[c].elite; lo = Mathf.Min(lo, camps[c].levelMin); }
            var cp = camps[main];
            return new RouteStep { at = campAt(main), name = cp.name, wing = w.id, wingName = w.name, camps = ordered.ToArray(), boss = boss, optional = optional, level = lo, follow = Patrols(cp) };
        }
        /// <summary>Side camps grouped into fights: those of one wing in one cave within <see cref="SameFight"/> m of each other (a patrol
        /// alone).</summary>
        static List<List<int>> Fights(ZoneCamp[] camps, List<int> list, Func<int, Vector3> campAt)
        {
            var fights = new List<List<int>>();
            foreach (int c in list)
            {
                List<int> with = null;
                if (!Patrols(camps[c]))
                    foreach (var f in fights)
                        if (f.Exists(o => !Patrols(camps[o]) && camps[o].wing == camps[c].wing && camps[o].cave == camps[c].cave && Vector3.Distance(campAt(o), campAt(c)) < SameFight)) { with = f; break; }
                if (with != null) with.Add(c); else fights.Add(new List<int> { c });
            }
            return fights;
        }
        /// <summary>Fits a side fight into a wing's fights where it adds the least walking (cheapest insertion: from the fight before it, or
        /// from where the run came in, to it and on to the next, or to the wing's home after the last).</summary>
        static void Insert(List<RouteStep> into, RouteStep x, Vector3 entry, Vector3? tail, Func<Vector3, Vector3, float> distance)
        {
            int at = into.Count; float best = float.MaxValue;
            for (int k = 0; k <= into.Count; k++)
            {
                var prev = k == 0 ? entry : into[k - 1].at; Vector3? next = k < into.Count ? into[k].at : tail;
                float cost = distance(prev, x.at) + (next.HasValue ? distance(x.at, next.Value) - distance(prev, next.Value) : 0);
                if (cost < best - .01f) { best = cost; at = k; }
            }
            into.Insert(at, x);
        }
    }
}
