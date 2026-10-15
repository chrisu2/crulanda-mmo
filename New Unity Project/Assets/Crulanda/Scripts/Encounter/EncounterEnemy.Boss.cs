using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>A boss's moment at a health mark (DUNGEON_DESIGN.md section 9 step 2, D2): "stamp", "terrify", "reset", "rally" or "loose".</summary>
    public sealed class BossPhase
    {
        public float at; public string kind, text;
        public float seconds = 2;   // the stun of a stamp or a terrify
        /// <summary>A "loose" phase's camp, by name (the Adit rework, ADIT_REWORK.md S4, 2026-10-15): a camp released later (ZoneCamp.after
        /// "loose:&lt;this boss&gt;") is brought now, or a live idle camp's mobs are sent in (Corvane's hound). EncounterSession.Loose.</summary>
        public string camp;
    }

    /// <summary>
    /// Boss mechanics (D2, 2026-10-08; all GAME-ONLY): what an elite's EliteMove adds beyond the heavy blow, enrage and call.
    /// - phases at health marks: a stamp (the party near it is stunned and it comes back with a heavier weapon), a terrify (the party
    ///   near it cowers, stunned), a reset (it forgets whom it was fighting and turns on someone else), a rally (it calls again);
    /// - a call when first attacked (callOnPull), and kin that answer when it dies (deathCall);
    /// - a knockback on its heavy blow, never off the walkable ground (knock); a disarm (disarm seconds, on you);
    /// - a burst round it every so often (ringEvery); a blink behind whoever it fights (blinkEvery).
    /// Only you are stunned or disarmed (Mira and sims are not yet).
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        readonly HashSet<int> phasesDone = new HashSet<int>();
        float nextRingAt, nextBlinkAt, nextHoldAt, weaponUp = 1; bool calledOnPull, saidHold; Actor lastFoe;
        /// <summary>Its hold on the party (EliteMove.cc): on everyone within ccReach, or on whoever it fights.</summary>
        void HoldParty()
        {
            var who = Move.ccReach > 0 ? PartyNear(Move.ccReach).ToList() : new List<Actor> { Victim };
            int held = 0;
            foreach (var a in who)
            {
                if (a == null || !a.IsAlive) continue;
                if (a == session.Player) session.HoldPlayer(Move.cc, Move.ccSeconds, transform.position, Name + "'s " + (Move.ccName ?? Move.cc));
                else { var c = a.GetComponent<SimCompanion>(); if (c != null) c.HoldUntil(Move.cc, Move.ccSeconds, transform.position); else continue; }
                held++;
            }
            if (held == 0) return;
            session.FloatText(transform.position + Vector3.up * .5f, (Move.ccName ?? Move.cc) + "!", BlowColor);
            if (!saidHold && !string.IsNullOrEmpty(Move.ccText)) { saidHold = true; session.Message(Move.ccText); }
        }
        /// <summary>How much heavier its weapon is now (a stamp's weapon change).</summary>
        public float WeaponUp { get { return weaponUp; } }

        /// <summary>The boss's part of a frame in a fight (called from TickElite).</summary>
        void TickBoss()
        {
            if (Victim != null) lastFoe = Victim;
            float ratio = actor.Health.Pool.Ratio, now = Time.time;
            if (Move.callOnPull && !calledOnPull && Victim != null) { calledOnPull = true; session.Rally(this, Victim); }
            if (Move.phases != null)
                for (int i = 0; i < Move.phases.Length; i++)
                    if (!phasesDone.Contains(i) && ratio <= Move.phases[i].at) { phasesDone.Add(i); RunPhase(Move.phases[i]); }
            if (Move.ringEvery > 0 && !WindingUp)
            {
                if (nextRingAt <= 0) nextRingAt = now + Move.ringEvery * .6f;
                else if (now >= nextRingAt) { nextRingAt = now + Move.ringEvery; Ring(); }
            }
            if (!string.IsNullOrEmpty(Move.cc) && Move.ccEvery > 0 && !WindingUp && Victim != null)
            {
                if (nextHoldAt <= 0) nextHoldAt = now + Move.ccEvery * .7f;
                else if (now >= nextHoldAt) { nextHoldAt = now + Move.ccEvery; HoldParty(); }
            }
            if (Move.blinkEvery > 0 && !WindingUp && Victim != null)
            {
                if (nextBlinkAt <= 0) nextBlinkAt = now + Move.blinkEvery * .5f;
                else if (now >= nextBlinkAt) { nextBlinkAt = now + Move.blinkEvery; Blink(); }
            }
        }
        IEnumerable<Actor> PartyNear(float reach)
        {
            var all = new List<Actor> { session.Player };
            if (session.Companion != null && session.Progress != null && session.Progress.recruited) all.Add(session.Companion.actor);
            foreach (var c in session.PartySims) if (c != null) all.Add(c.actor);
            return all.Where(a => a != null && a.IsAlive && Vector3.Distance(a.transform.position, transform.position) <= reach);
        }
        void RunPhase(BossPhase p)
        {
            if (!string.IsNullOrEmpty(p.text)) session.Message(p.text);
            switch (p.kind)
            {
                case "stamp":
                    if (PartyNear(12).Contains(session.Player)) session.StunPlayer(p.seconds, Name + " stamps");
                    weaponUp *= 1.25f; session.FloatText(transform.position + Vector3.up * .5f, "Heavier weapon!", RageColor);
                    break;
                case "terrify":
                    if (PartyNear(12).Contains(session.Player)) session.StunPlayer(p.seconds, "Terrified");
                    session.FloatText(transform.position + Vector3.up * .5f, "Terrify", new Color(.78f, .5f, 1));
                    break;
                case "reset":
                    var others = PartyNear(30).Where(a => a != Victim).ToList();
                    threat.Clear();
                    var next = others.Count > 0 ? others[Random.Range(0, others.Count)] : Victim;
                    if (next != null) threat.Taunt(next.EntityId.Value, Time.time, 4);
                    session.FloatText(transform.position + Vector3.up * .5f, "Forgets", new Color(.75f, .75f, .8f));
                    break;
                case "rally":
                    if (Victim != null) session.Rally(this, Victim);
                    break;
                case "loose":   // (the Adit rework) a camp let loose at this mark: the urn-risen, the hound (EncounterSession.Release.cs)
                    session.Loose(this, p.camp, Victim);
                    break;
            }
        }
        /// <summary>A burst round it: everyone in its ring is hit.</summary>
        void Ring()
        {
            foreach (var who in PartyNear(Move.ringReach).ToList())
            {
                int dealt = session.Kit.ResolveEnemyHit(this, who, CappedBlow(who, Mathf.RoundToInt(HitBase * Move.ringBlow * OverHitNow * GroupDamageScale * BlowScale)));
                if (Move.knock > 0) Knock(who, Move.knock * .6f);
                session.FloatText(who.transform.position, (Move.ringName ?? "Burst") + " " + dealt, RageColor);
            }
            session.FloatText(transform.position + Vector3.up * .5f, (Move.ringName ?? "Burst") + "!", BlowColor);
        }
        /// <summary>Steps out of the air behind whoever it fights.</summary>
        void Blink()
        {
            var v = Victim; if (v == null || agent == null || !agent.isOnNavMesh) return;
            var behind = v.transform.position - v.transform.forward * 2.2f;
            if (!NavMesh.SamplePosition(behind, out var hit, 2.5f, NavMesh.AllAreas)) return;
            agent.Warp(hit.position); transform.rotation = Quaternion.LookRotation(Flat(v.transform.position - hit.position));
            session.FloatText(transform.position + Vector3.up * .5f, "Blinks", new Color(.75f, .75f, .8f));
        }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < .001f ? Vector3.forward : v; }
        /// <summary>Throws <paramref name="who"/> back from it, only ever onto walkable ground (never under the world or into a void).</summary>
        public void Knock(Actor who, float metres)
        {
            if (who == null || !who.IsAlive || metres <= 0) return;
            var dir = Flat(who.transform.position - transform.position).normalized;
            // Bodies stand about a metre above the walkable mesh: trace along the mesh from under their feet, never through a wall or off an
            // edge. Short of walkable ground the full distance (a cliff, deep water, a cave wall), the throw stops at the edge.
            if (!NavMesh.SamplePosition(who.transform.position, out var from, 2.5f, NavMesh.AllAreas)) return;
            NavMeshHit hit;
            if (NavMesh.Raycast(from.position, from.position + dir * metres, out var edge, NavMesh.AllAreas)) { if ((edge.position - from.position).magnitude < .5f) return; hit = edge; }
            else if (!NavMesh.SamplePosition(from.position + dir * metres, out hit, 2.5f, NavMesh.AllAreas)) return;
            var motor = who.GetComponent<AdventurerMotor>();
            if (motor != null) motor.Teleport(hit.position + Vector3.up * (who.transform.position.y - from.position.y));
            else { var a = who.GetComponent<NavMeshAgent>(); if (a != null && a.isOnNavMesh) a.Warp(hit.position); }
            session.FloatText(who.transform.position, "Knocked back", new Color(1, .8f, .5f));
        }
        /// <summary>After its heavy blow lands on <paramref name="v"/>: the knockback and the disarm.</summary>
        void AfterBlow(Actor v)
        {
            if (Move.knock > 0) Knock(v, Move.knock);
            if (Move.disarm > 0 && v == session.Player) session.DisarmPlayer(Move.disarm, Name + "'s " + Move.name);
        }
        /// <summary>When it dies: kin of its kind within reach answer, the patrol that walks in after a boss.</summary>
        void DeathCall()
        {
            var foe = lastFoe != null && lastFoe.IsAlive ? lastFoe : session.Player;
            // (The Adit rework, 2026-10-15) A camp released by its death (ZoneCamp.after "kill:<this mob>": Lusk's relief shift) comes in its
            // place a few seconds on, with the move's deathText as its line; it stands in for the kin search (ADIT_REWORK.md S3; EncounterSession.Release.cs).
            if (Camp && !Game && session.ReleaseAfter("kill:" + MobName, foe, this, Move != null && !string.IsNullOrEmpty(Move.deathText) ? Move.deathText : "Boots on the stone: more of them come.", EncounterSession.ReleaseDelay) > 0) return;
            if (Move == null || Move.deathCall <= 0) return;
            var kin = session.Enemies.Where(e => e != null && e != this && e.CanAnswer && !e.Engaged && !string.IsNullOrEmpty(Kin) && e.Kin == Kin
                && Vector3.Distance(e.transform.position, transform.position) <= Move.deathReach).OrderBy(e => Vector3.Distance(e.transform.position, transform.position)).Take(Move.deathCall).ToList();
            for (int i = 0; i < kin.Count; i++) kin[i].Answer(this, foe, SocialAggro.CallBeat + SocialAggro.CallStagger * i);
            if (kin.Count > 0) session.Message(string.IsNullOrEmpty(Move.deathText) ? "Boots on the stone: more of them come." : Move.deathText);
        }
        void ResetBoss() { phasesDone.Clear(); weaponUp = 1; calledOnPull = false; nextRingAt = nextBlinkAt = 0; }
    }
}
