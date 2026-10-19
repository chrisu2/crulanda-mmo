using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Marked ground, for the sims to step out of (Docs/WORLD_BOSS_1.md 3.5, "Out of the ground", shared with the dungeons): every mark the
    /// game draws on the ground (BlowMark.Live: an elite's heavy blow, a warned hold, a camp's trick ring, a Hollow's dust, the loop's snap
    /// ring, the Unraveller's unmade ground and her hunt's five metres) is ground a party member should not stand in. A sim inside one walks to
    /// the nearest spot clear of all of them first (SimCompanion.Raid). GAME-ONLY. The pure part (<see cref="Inside"/>, <see cref="ClearSpot"/>)
    /// works on circles on the flat, so it is tested without a scene (RaidJobsTests).
    /// </summary>
    public static class GroundMarks
    {
        /// <summary>How far past a mark's edge a sim makes for, and how much of a mark's edge it may stand on without counting as in it.</summary>
        public const float ClearTo = 1.5f, Slack = .3f;
        /// <summary>How far above or below a mark's ground someone still counts as on it (not on the floor over it).</summary>
        public const float Height = 2.5f;
        /// <summary>The marks showing now, as circles on the flat (centre, radius), with the ground height of each.</summary>
        public static List<(Vector2 c, float r, float y)> Now()
        {
            var l = new List<(Vector2, float, float)>();
            foreach (var m in BlowMark.Live) if (m != null && m.Showing && m.Reach > 0 && m.Kind == GroundKind.Harmful) l.Add((new Vector2(m.Feet.x, m.Feet.z), m.Reach, m.Feet.y));
            return l;
        }
        /// <summary>The deepest mark <paramref name="p"/> stands in (its index), or -1 for none.</summary>
        public static int Inside(Vector2 p, IList<(Vector2 c, float r)> marks)
        {
            int at = -1; float deepest = 0;
            if (marks != null) for (int i = 0; i < marks.Count; i++) { float into = marks[i].r - Slack - Vector2.Distance(p, marks[i].c); if (into > deepest) { deepest = into; at = i; } }
            return at;
        }
        /// <summary>
        /// Where to step to from <paramref name="p"/>, out of every mark: straight away from the deepest one's centre to its edge and a little
        /// past, or turned up to 150 degrees either way, the first spot that is in none of them and that <paramref name="walkable"/> allows
        /// (null: any). Null when <paramref name="p"/> is in none, or there is nowhere clear.
        /// </summary>
        public static Vector2? ClearSpot(Vector2 p, IList<(Vector2 c, float r)> marks, System.Func<Vector2, bool> walkable = null)
        {
            int k = Inside(p, marks); if (k < 0) return null;
            var c = marks[k].c; var away = p - c; away = away.sqrMagnitude > .0001f ? away.normalized : Vector2.right;
            foreach (float extra in new[] { 0f, 2f, 4f, 8f, 12f })   // far enough even past a run of patches (raid: no safe spot near)
                foreach (float turn in new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 180f })
                {
                    float rad = turn * Mathf.Deg2Rad;
                    var dir = new Vector2(away.x * Mathf.Cos(rad) - away.y * Mathf.Sin(rad), away.x * Mathf.Sin(rad) + away.y * Mathf.Cos(rad));
                    var to = c + dir * (marks[k].r + ClearTo + extra);
                    if (Inside(to, marks) >= 0) continue;
                    if (walkable != null && !walkable(to)) continue;
                    return to;
                }
            return null;
        }
        /// <summary>Whether <paramref name="at"/> (a body's position, a metre over its feet) stands in harmful ground now.</summary>
        public static bool InHarm(Vector3 at)
        {
            var feet = at - Vector3.up; var marks = Near(feet.y);
            return marks.Count > 0 && Inside(new Vector2(feet.x, feet.z), marks) >= 0;
        }
        /// <summary>A spot from which to strike <paramref name="target"/> (within <paramref name="reach"/> of its edge, <paramref name="body"/>)
        /// out of harmful ground, the nearest to <paramref name="from"/> on the walkable ground; null for none (stand off, then).</summary>
        public static Vector3? StrikeSpot(Vector3 from, Vector3 target, float body, float reach)
        {
            var marks = Near(target.y - 1); if (marks.Count == 0) return null;
            Vector3? best = null; float bestD = float.MaxValue;
            for (int k = 0; k < 16; k++)
                foreach (float r in new[] { body + reach * .6f, body + reach * .95f })
                {
                    var p = target + Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward * r;
                    if (!NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                    if (Inside(new Vector2(hit.position.x, hit.position.z), marks) >= 0) continue;
                    float d = Vector3.Distance(hit.position, from); if (d < bestD) { bestD = d; best = hit.position; }
                }
            return best;
        }
        /// <summary>The marks showing now that lie at about <paramref name="y"/>'s height, as flat circles.</summary>
        public static List<(Vector2 c, float r)> Near(float y)
        {
            var l = new List<(Vector2 c, float r)>();
            foreach (var m in Now()) if (Mathf.Abs(m.y - y) < Height) l.Add((m.c, m.r));
            return l;
        }
        /// <summary>Where someone standing at <paramref name="feet"/> (the navmesh under them) should step to now, out of the marks there, on the
        /// walkable ground of their own floor; null when they stand in none (or there is nowhere clear).</summary>
        public static Vector3? StepOut(Vector3 feet)
        {
            var marks = Near(feet.y); if (marks.Count == 0) return null;
            var p = new Vector2(feet.x, feet.z);
            if (Inside(p, marks) < 0) return null;
            Vector3 found = Vector3.zero;
            var spot = ClearSpot(p, marks, q => { if (!NavMesh.SamplePosition(new Vector3(q.x, feet.y, q.y), out var hit, 1.5f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - feet.y) > 1.5f) return false; found = hit.position; return true; });
            return spot.HasValue ? found : (Vector3?)null;
        }
    }
}
