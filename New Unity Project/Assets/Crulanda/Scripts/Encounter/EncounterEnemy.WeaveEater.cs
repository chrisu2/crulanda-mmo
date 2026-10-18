using UnityEngine;
using UnityEngine.AI;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Lowtowns' Weave-Eaters as Book 1 chapter 20 shows them (Docs/REWORK_LOWTOWNS_HALLOWS.md, Lowtowns pick 4; WeaveEaterRules has the
    /// rules and the citations): the blink toward whoever it fights (ch.20:131), the lighthouse that turns it on the party's magic-workers
    /// (ch.20:121), the ward eaten off its foe (ch.20:143), and no blink while a bone harpoon pins it (ch.20:191). Salt's calcifying is the
    /// stun the session puts on (EncounterSession.Lowtowns). Only in WeaveEaterRules.BookZones; called each frame of a fight from
    /// EncounterEnemy.UpdateTimed, after its target is chosen, and never while it is held (ControlTick comes first).
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        // 0 not looked yet, 1 a book Weave-Eater, 2 not
        int weaveEaterKnown;
        float blinkReadyAt, pinnedUntil, nextLighthouseAt, nextWardBiteAt; bool saidWardEaten;
        /// <summary>A Weave-Eater of a zone whose Weave-Eaters fight as the book shows them.</summary>
        public bool BookWeaveEater
        {
            get
            {
                if (weaveEaterKnown == 0) weaveEaterKnown = session != null && WeaveEaterRules.BookZone(session.ZoneId) && WeaveEaterRules.IsWeaveEater(this) ? 1 : 2;
                return weaveEaterKnown == 1;
            }
        }
        /// <summary>Pinned by a bone harpoon: no blink until then.</summary>
        public bool Pinned { get { return Time.time < pinnedUntil; } }
        /// <summary>A bone harpoon's anchor (ch.20:191): it cannot displace for <paramref name="seconds"/>.</summary>
        public void Pin(float seconds) { pinnedUntil = Mathf.Max(pinnedUntil, Time.time + seconds); }
        /// <summary>A fight begun or ended: its blink half ready, the lighthouse and the ward-eating from now, its line unsaid.</summary>
        void ResetWeaveEater() { blinkReadyAt = Time.time + WeaveEaterRules.BlinkEvery * .5f; nextLighthouseAt = nextWardBiteAt = 0; saidWardEaten = false; pinnedUntil = 0; }

        /// <summary>One frame of a book Weave-Eater's fight with <see cref="Victim"/>.</summary>
        void TickWeaveEater()
        {
            var v = Victim; if (v == null || !v.IsAlive) return;
            float now = Time.time;
            if (blinkReadyAt <= 0) ResetWeaveEater();
            // The lighthouse: those who work magic near it draw it.
            if (now >= nextLighthouseAt)
            {
                nextLighthouseAt = now + WeaveEaterRules.LighthouseBeat;
                float top = threat.Top;
                foreach (var a in session.PartyActors())
                {
                    if (a == null || !a.IsAlive || Vector3.Distance(a.transform.position, transform.position) > WeaveEaterRules.LighthouseReach) continue;
                    var c = a.GetComponent<Combatant>();
                    if (WeaveEaterRules.DrawnTo(a.Resource != null ? a.Resource.Kind : Crulanda.Core.ResourceKind.None, c != null ? c.Barrier : 0)) threat.Add(a.EntityId.Value, WeaveEaterRules.LighthouseThreat(top));
                }
            }
            float gap = Vector3.Distance(v.transform.position, transform.position);
            // The ward eaten off whoever it fights, within reach.
            if (gap <= WeaveEaterRules.MeleeReach && now >= nextWardBiteAt)
            {
                nextWardBiteAt = now + 1;
                var guard = v.GetComponent<Combatant>();
                int bite = guard != null ? WeaveEaterRules.WardBite(guard.Barrier) : 0;
                if (bite > 0 && guard.EatBarrier(bite) > 0)
                {
                    session.FloatText(v.transform.position, "Ward eaten " + bite, new Color(.62f, .55f, .8f));
                    if (!saidWardEaten && v == session.Player) { saidWardEaten = true; session.Message(WeaveEaterRules.WardEatenLine); }
                }
            }
            // The blink: a jerk through space toward its foe.
            if (WeaveEaterRules.Blinks(gap, Rooted, Pinned, now, blinkReadyAt) && !(Move != null && WindingUp)) { blinkReadyAt = now + WeaveEaterRules.BlinkEvery; BlinkToward(v, gap); }
        }
        /// <summary>Warps a short way toward <paramref name="v"/>, only onto walkable ground at the foe's own level (never onto a roof over the
        /// lane or down into the court below).</summary>
        void BlinkToward(Actor v, float gap)
        {
            if (agent == null || !agent.isOnNavMesh) return;
            var dir = v.transform.position - transform.position; dir.y = 0; if (dir.sqrMagnitude < .01f) return;
            var to = transform.position + dir.normalized * WeaveEaterRules.BlinkStep(gap);
            if (!NavMesh.SamplePosition(to, out var hit, 2, NavMesh.AllAreas)) return;
            if (!NavMesh.SamplePosition(v.transform.position, out var foe, 2.5f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - foe.position.y) > 2) return;
            var from = transform.position;
            agent.Warp(hit.position); transform.rotation = Quaternion.LookRotation(dir);
            session.FloatText(from + Vector3.up * .3f, "Displaces", new Color(.55f, .5f, .7f));
        }
    }
}
