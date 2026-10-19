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
        // The Unraveller's numbers for the same rules, scaled (WORLD_BOSS_1.md 3.3; UnravellerTuning); every other Weave-Eater's as the book's.
        float EaterBlinkEvery { get { return RaidBoss ? T.blinkEvery : WeaveEaterRules.BlinkEvery; } }
        float EaterBlinkReach { get { return RaidBoss ? T.blinkReach : WeaveEaterRules.BlinkReach; } }
        float EaterBeat { get { return RaidBoss ? T.lighthouseBeat : WeaveEaterRules.LighthouseBeat; } }
        float EaterDraw { get { return RaidBoss ? T.lighthouseReach : WeaveEaterRules.LighthouseReach; } }
        float EaterShare { get { return RaidBoss ? T.lighthouseShare : WeaveEaterRules.LighthouseShare; } }
        void ResetWeaveEater() { blinkReadyAt = Time.time + EaterBlinkEvery * .5f; nextLighthouseAt = nextWardBiteAt = 0; saidWardEaten = false; pinnedUntil = 0; }

        /// <summary>One frame of a book Weave-Eater's fight with <see cref="Victim"/>.</summary>
        void TickWeaveEater()
        {
            var v = Victim; if (v == null || !v.IsAlive) return;
            float now = Time.time;
            if (blinkReadyAt <= 0) ResetWeaveEater();
            // The lighthouse: those who work magic near it draw it.
            if (now >= nextLighthouseAt)
            {
                nextLighthouseAt = now + EaterBeat;
                float top = threat.Top;
                foreach (var a in session.PartyActors())
                {
                    if (a == null || !a.IsAlive || Vector3.Distance(a.transform.position, transform.position) > EaterDraw) continue;
                    var c = a.GetComponent<Combatant>();
                    bool drawn = RaidBoss ? session.WorksMagic(a) : WeaveEaterRules.DrawnTo(a.Resource != null ? a.Resource.Kind : Crulanda.Core.ResourceKind.None, c != null ? c.Barrier : 0);   // her lighthouse knows a sim's craft by its class
                    if (drawn) threat.Add(a.EntityId.Value, WeaveEaterRules.LighthouseThreat(top, EaterShare));
                }
            }
            float gap = Vector3.Distance(v.transform.position, transform.position);
            // The ward eaten off whoever it fights, within reach (the Unraveller: off everyone of the party within 10 m).
            if (RaidBoss && now >= nextWardBiteAt)
            {
                nextWardBiteAt = now + 1;
                foreach (var a in session.PartyActors())
                {
                    if (a == null || !a.IsAlive || Vector3.Distance(a.transform.position, transform.position) > T.wardReach || session.AmberWarded(a)) continue;   // amber resists the Unmaking (book1 ch.20:181)
                    var ward = a.GetComponent<Combatant>(); int b = ward != null ? WeaveEaterRules.WardBite(ward.Barrier) : 0;
                    if (b > 0 && ward.EatBarrier(b) > 0) session.FloatText(a.transform.position, "Ward eaten " + b, new Color(.62f, .55f, .8f));
                }
            }
            else if (gap <= WeaveEaterRules.MeleeReach && now >= nextWardBiteAt)
            {
                nextWardBiteAt = now + 1;
                var guard = v.GetComponent<Combatant>();
                int bite = guard != null && !session.AmberWarded(v) ? WeaveEaterRules.WardBite(guard.Barrier) : 0;   // amber: not eaten
                if (bite > 0 && guard.EatBarrier(bite) > 0)
                {
                    session.FloatText(v.transform.position, "Ward eaten " + bite, new Color(.62f, .55f, .8f));
                    if (!saidWardEaten && v == session.Player) { saidWardEaten = true; session.Message(WeaveEaterRules.WardEatenLine); }
                }
            }
            // The blink: a jerk through space toward its foe.
            if (WeaveEaterRules.Blinks(gap, Rooted, Pinned, now, blinkReadyAt) && !(Move != null && WindingUp) && !Feeding) { blinkReadyAt = now + EaterBlinkEvery; if (BlinkToward(v, gap)) RaidTouch(v); }
        }
        /// <summary>Warps a short way toward <paramref name="v"/>, only onto walkable ground at the foe's own level (never onto a roof over the
        /// lane or down into the court below).</summary>
        bool BlinkToward(Actor v, float gap)
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            var dir = v.transform.position - transform.position; dir.y = 0; if (dir.sqrMagnitude < .01f) return false;
            var to = transform.position + dir.normalized * WeaveEaterRules.BlinkStep(gap, EaterBlinkReach);
            if (!NavMesh.SamplePosition(to, out var hit, 2, NavMesh.AllAreas)) return false;
            var land = hit.position;
            // Hers lands on the dry, never in the river (Chris: "try to keep her out of water"): short of the water, or not at all.
            if (RaidBoss && session.WetForHer(land + Vector3.up))
            {
                bool dry = false; float step = WeaveEaterRules.BlinkStep(gap, EaterBlinkReach);
                for (float f = .8f; f > .1f && !dry; f -= .2f)
                    if (NavMesh.SamplePosition(transform.position + dir.normalized * step * f, out var h2, 2, NavMesh.AllAreas) && !session.WetForHer(h2.position + Vector3.up)) { land = h2.position; dry = true; }
                if (!dry) return false;
            }
            if (!NavMesh.SamplePosition(v.transform.position, out var foe, 2.5f, NavMesh.AllAreas) || Mathf.Abs(land.y - foe.position.y) > 2) return false;
            var from = transform.position;
            agent.Warp(land); transform.rotation = Quaternion.LookRotation(dir);
            session.FloatText(from + Vector3.up * .3f, "Displaces", new Color(.55f, .5f, .7f));
            // Her touch only where she lands on them (within touching), not a blink that falls short.
            return Vector3.Distance(land, v.transform.position) <= 4.5f;
        }
    }
}
