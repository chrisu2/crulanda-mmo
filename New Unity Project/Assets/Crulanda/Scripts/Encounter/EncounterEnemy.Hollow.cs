using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's Hollow as Book 1 chapter 4 shows them (Docs/REWORK_LOWTOWNS_HALLOWS.md, Hallow's Creek pick 6; HollowRules has the
    /// rules and the citations): the slow, silent shift toward whoever it fights (ch.4:109), the choking dust warned on the ground
    /// (ch.4:177), the half that crawls on when it is cut down (ch.4:181), and standing up again beside the anchor-well while the well pulses
    /// (ch.4:191-197; EncounterSession.HollowCellars). Only the ordinary Hollow of Hallow's Creek; called each frame of a fight from
    /// UpdateTimed after its target is chosen, never while it is held (ControlTick comes first).
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        // 0 not looked yet, 1 a book Hollow, 2 not
        int hollowKnown;
        float glideReadyAt, glideUntil, dustReadyAt = -1, dustAt = -1; Vector3 glideFrom, glideTo; BlowMark dustMark;
        bool risenFromWell; float wellRiseAt = -1; Actor wellFoe; float hitBeforeHalf;
        /// <summary>A crawling half (ch.4:181): what is left of a Hollow cut down, still coming.</summary>
        public bool Half { get; private set; }
        /// <summary>Gliding now (no walking: ActorVisual.Gliding).</summary>
        public bool HollowGliding { get { return Time.time < glideUntil; } }
        /// <summary>Drawing up its dust now (the ring on the ground filling).</summary>
        public bool PuffingDust { get { return dustAt >= 0; } }
        /// <summary>Lying where it fell, about to stand up again beside the anchor-well.</summary>
        public bool RisingAtWell { get { return wellRiseAt >= 0; } }
        /// <summary>A Hollow of Hallow's Creek that fights as chapter 4 shows them.</summary>
        public bool BookHollow
        {
            get
            {
                if (hollowKnown == 0) hollowKnown = session != null && HollowRules.BookHollow(session.ZoneId, MobName) ? 1 : 2;
                return hollowKnown == 1;
            }
        }
        /// <summary>Its pace: a crawling half drags itself at a little over half.</summary>
        float HollowPace { get { return Half ? HollowRules.HalfPace : 1; } }

        /// <summary>A fight begun or ended: the glide half ready, the dust from its first mark, no dust drawn, no glide under way.</summary>
        void ResetHollow()
        {
            glideReadyAt = Time.time + HollowRules.GlideEvery * .5f; dustReadyAt = Time.time + HollowRules.DustFirst; glideUntil = 0;
            CancelDust();
        }
        void CancelDust() { dustAt = -1; if (dustMark != null) dustMark.Hide(); if (Figure != null) Figure.Gliding = false; }

        /// <summary>One frame of a book Hollow's fight. True: it is gliding or drawing up dust, and does nothing else this frame.</summary>
        bool TickHollow()
        {
            var v = Victim; if (v == null || !v.IsAlive) return false;
            float now = Time.time;
            if (dustReadyAt < 0) ResetHollow();
            // The glide under way: carried along the ground, feet still (ActorVisual.Gliding), a grey blur about it.
            if (glideUntil > 0)
            {
                if (now < glideUntil && agent != null && agent.isOnNavMesh)
                {
                    float t = 1 - (glideUntil - now) / HollowRules.GlideSeconds;
                    var want = Vector3.Lerp(glideFrom, glideTo, Mathf.SmoothStep(0, 1, t));
                    var step = want - transform.position; step.y = 0;
                    agent.Move(step);
                    return true;
                }
                glideUntil = 0; if (Figure != null) Figure.Gliding = false;
                if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
            }
            float gap = Vector3.Distance(v.transform.position, transform.position);
            bool busy = Casting || (Move != null && WindingUp);
            // The dust drawn up: the ring fills, then the puff.
            if (dustAt >= 0)
            {
                if (dustMark != null) dustMark.Show(transform.position - Vector3.up, HollowRules.DustReach, 1 - (dustAt - now) / HollowRules.DustWindup);
                if (now < dustAt) return true;
                CancelDust(); PuffDust();
                return false;
            }
            if (HollowRules.Puffs(gap, Rooted, busy, now, dustReadyAt) && !Half)
            {
                dustReadyAt = now + HollowRules.DustEvery; dustAt = now + HollowRules.DustWindup;
                if (dustMark == null) dustMark = BlowMark.Create(GroundKind.Harmful, HollowRules.DustWindup + 2, "Choking dust");
                dustMark.Show(transform.position - Vector3.up, HollowRules.DustReach, 0);
                if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.velocity = Vector3.zero; }
                session.FloatText(transform.position, "Grey dust rising", new Color(.7f, .7f, .68f));
                return true;
            }
            if (HollowRules.Glides(gap, Rooted, busy, now, glideReadyAt)) { glideReadyAt = now + HollowRules.GlideEvery; return GlideToward(v, gap); }
            return false;
        }
        /// <summary>Starts a glide toward <paramref name="v"/>, only to walkable ground at the foe's own level.</summary>
        bool GlideToward(Actor v, float gap)
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            var dir = v.transform.position - transform.position; dir.y = 0; if (dir.sqrMagnitude < .01f) return false;
            var to = transform.position + dir.normalized * HollowRules.GlideStep(gap);
            if (!NavMesh.SamplePosition(to, out var hit, 2, NavMesh.AllAreas)) return false;
            // on the foe's own floor (never up onto a deck or down a stair's well)
            if (!NavMesh.SamplePosition(v.transform.position, out var foe, 2.5f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - foe.position.y) > 1.2f) return false;
            glideFrom = transform.position; glideTo = hit.position; glideTo.y = glideFrom.y; glideUntil = Time.time + HollowRules.GlideSeconds;
            agent.isStopped = true; agent.velocity = Vector3.zero; transform.rotation = Quaternion.LookRotation(dir);
            if (Figure != null) Figure.Gliding = true;
            session.FloatText(transform.position, "Shifts closer", new Color(.6f, .6f, .62f));
            return true;
        }
        /// <summary>The puff: everyone of the party within reach and on its level is blinded (EncounterSession.Blind).</summary>
        void PuffDust()
        {
            int caught = 0;
            foreach (var a in session.PartyActors())
            {
                if (a == null || !a.IsAlive) continue;
                var d = a.transform.position - transform.position; if (Mathf.Abs(d.y) > 2) continue; d.y = 0;
                if (d.magnitude > HollowRules.DustReach) continue;
                session.Blind(a, HollowRules.BlindSeconds); caught++;
            }
            session.FloatText(transform.position, "Choking dust", new Color(.62f, .62f, .6f));
            if (caught > 0) session.DustLine();
        }

        /// <summary>
        /// Its death, before anything else is done with it: beside the pulsing anchor-well it lies a moment and stands up again (once a visit);
        /// else, now and then, it splits and its half crawls on (ch.4:181). True: it is not dead yet (no kill, no body, no loot until it is).
        /// </summary>
        bool HollowDeath()
        {
            if (!BookHollow || session == null) return false;
            CancelDust(); glideUntil = 0;
            var foe = Victim;
            float toWell = session.DistanceToWell(transform.position);
            bool raises = HollowRules.WellRaises(session.WellWhole, toWell, risenFromWell, Half);
            if (raises)
            {
                risenFromWell = true; wellRiseAt = Time.time + HollowRules.WellRiseSeconds; wellFoe = foe;
                ClearFight(); Victim = null; threat.Clear();
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
                var body = transform.Find("Body"); if (body != null) { body.localRotation = Quaternion.Euler(0, 0, 90); body.localPosition = new Vector3(0, -.6f, 0); }
                session.FloatText(transform.position, "The well pulses", new Color(.72f, .4f, .95f));
                return true;
            }
            if (!HollowRules.Splits(Half, false, HollowRules.Dice())) return false;
            // The half: up at once, low, slow, its blow halved, still on whoever it fought.
            Half = true; hitBeforeHalf = HitBase; HitBase *= HollowRules.HalfHit;
            actor.Health.Revive(HollowRules.Share(actor.Health.Pool.Max, HollowRules.HalfHealth));
            var visual = transform.Find("Body"); if (visual != null) { visual.localRotation = Quaternion.Euler(75, 0, 0); visual.localPosition = new Vector3(0, -.65f, 0); }
            if (foe != null && foe.IsAlive) threat.Add(foe.EntityId.Value, 10);
            session.FloatText(transform.position, "Its half crawls on", new Color(.65f, .65f, .62f));
            session.HalfLine();
            return true;
        }
        /// <summary>Each frame while it lies beside the well: up again when its time comes (the well still whole), on whoever it fought.</summary>
        void TickWellRise()
        {
            if (wellRiseAt < 0 || Time.time < wellRiseAt) return;
            wellRiseAt = -1;
            if (!session.WellWhole) { FinishDeath(); return; }
            actor.Health.Revive(HollowRules.Share(actor.Health.Pool.Max, HollowRules.WellRiseHealth));
            var body = transform.Find("Body"); if (body != null) { body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; }
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
            var foe = wellFoe != null && wellFoe.IsAlive ? wellFoe : session.Player;
            if (foe != null && foe.IsAlive) threat.Add(foe.EntityId.Value, session.JoinThreat);
            session.FloatText(transform.position, "Stands up again", new Color(.72f, .4f, .95f));
            session.WellRiseLine();
        }
        /// <summary>The well broken while it lay: it does not get up; its death is done now (the kill, the body, the loot).</summary>
        void FinishDeath()
        {
            respawnAt = Time.time + RespawnSeconds;
            WalkThrough(true);
            DeathCall();
            session.EnemyDied(this);
        }
        /// <summary>A fight given up (ResetFight heals it): a crawling half or a Hollow the well raised goes back no higher than it stood up
        /// (2026-10-18, the PlayMode run: a half on a player who died reset to full health).</summary>
        void CapHollowHealth()
        {
            if (!actor.IsAlive || (!Half && !risenFromWell)) return;
            int cap = HollowRules.Share(actor.Health.Pool.Max, Half ? HollowRules.HalfHealth : HollowRules.WellRiseHealth);
            if (actor.Health.Pool.Current > cap) actor.Health.Pool.SetCurrent(cap);
        }
        /// <summary>Back whole at its camp (Respawn): no half, not risen, its blow its own again.</summary>
        void RespawnHollow()
        {
            if (Half) { Half = false; HitBase = hitBeforeHalf > 0 ? hitBeforeHalf : HitBase; }
            risenFromWell = false; wellRiseAt = -1; wellFoe = null; glideUntil = 0; dustReadyAt = -1; CancelDust();
        }
        /// <summary>The anchor-well breaks (EncounterSession.BreakWell): a Hollow within reach freezes and crumbles, losing a share of its health;
        /// one lying beside it, about to rise, stays down.</summary>
        public void WellBroken(Actor by)
        {
            if (!BookHollow) return;
            if (RisingAtWell) { wellRiseAt = -1; FinishDeath(); return; }
            if (!actor.IsAlive) return;
            // Straight off its stone (no dust's miss, no party scaling): what the breaking does, not a blow.
            int crumble = actor.GetComponent<Crulanda.Combat.Combatant>().Damage(HollowRules.Share(actor.Health.Pool.Max, HollowRules.WellCrumble));
            session.FloatText(transform.position, "Crumbles " + crumble, new Color(.85f, .85f, .95f));
        }
    }
}
