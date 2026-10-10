using System.Collections.Generic;
using UnityEngine;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>Raid marks (Docs/CC_DESIGN.md section 3): skull, kill first; moon and cross, held by crowd control, left alone.</summary>
    public enum RaidMark { None, Skull, Moon, Cross }
    /// <summary>
    /// Crowd control on a mob (Docs/CC_DESIGN.md section 1, step C1; Chris 2026-10-08: "we don't have any CC classes yet"):
    /// - "incap" (a hex, a sleep, a trap, a sap): out of the fight, no swings, no calls; any damage breaks it;
    /// - "stun": can't act; damage doesn't break it;
    /// - "fear": runs from whoever feared it; three hits or the time end it;
    /// - "silence": its casts are stopped (an interrupt).
    /// The same kind again within 18 s lasts half as long, a third time not at all; a non-boss elite holds 70% as long; a boss takes
    /// no incap and no fear. When an incap or a fear ends, the mob comes back for whoever put it on (threat on them).
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        public const float DrWindow = 18, EliteHold = .7f, ReleaseThreat = 400;
        float incapUntil, stunUntil, fearUntil, silenceUntil; int fearHits; Vector3 fearFrom; Actor heldBy; bool wasHeld;
        readonly Dictionary<string, (float at, int n)> diminish = new Dictionary<string, (float, int)>();
        public bool Incapacitated { get { return Time.time < incapUntil; } }
        public bool Stunned { get { return Time.time < stunUntil; } }
        public bool Feared { get { return Time.time < fearUntil; } }
        public bool Silenced { get { return Time.time < silenceUntil; } }
        /// <summary>Out of the fight for now (held, stunned or running).</summary>
        public bool Controlled { get { return Incapacitated || Stunned || Feared; } }
        /// <summary>The target frame's line: "Held 18 s", "Stunned 3 s", "Fleeing 5 s", "Silenced 4 s"; null when free.</summary>
        public string ControlLabel
        {
            get
            {
                if (Incapacitated) return "Held " + Left(incapUntil) + " s";
                if (Stunned) return "Stunned " + Left(stunUntil) + " s";
                if (Feared) return "Fleeing " + Left(fearUntil) + " s";
                if (Silenced) return "Silenced " + Left(silenceUntil) + " s";
                if (Bolting) return "Fleeing " + Left(boltUntil) + " s";   // bolting at low health (EncounterEnemy.Flee): not a hold
                return null;
            }
        }
        /// <summary>Whole seconds left until <paramref name="until"/>, rounded up, with a hair of slack: now + 10 read back in float can be 10.000008, and that must say 10, not 11.</summary>
        static int Left(float until) { return Mathf.CeilToInt(until - Time.time - .005f); }
        bool IsBoss { get { return Elite && Move != null && Move.boss; } }
        /// <summary>Seconds left on whatever control shows (0 when free).</summary>
        public float ControlSecondsLeft
        {
            get
            {
                float now = Time.time;
                if (Incapacitated) return incapUntil - now; if (Stunned) return stunUntil - now; if (Feared) return fearUntil - now; if (Silenced) return silenceUntil - now;
                return 0;
            }
        }
        /// <summary>
        /// Puts a CC on it: kind "incap", "stun", "fear" or "silence", from <paramref name="by"/>, for about <paramref name="seconds"/>.
        /// Null when it took (the seconds it got are in the float text), else why not ("Immune", "Immune (too soon)").
        /// </summary>
        public string Apply(string kind, Actor by, float seconds)
        {
            if (!actor.IsAlive || Game || seconds <= 0) return "Nothing to hold";
            if (IsBoss && (kind == "incap" || kind == "fear")) { session.FloatText(transform.position, "Immune", Color.white); return "Immune"; }
            var d = diminish.TryGetValue(kind, out var was) && Time.time - was.at < DrWindow ? (Time.time, was.n + 1) : (Time.time, 0);
            if (d.Item2 >= 2) { diminish[kind] = (was.at, was.n); session.FloatText(transform.position, "Immune", Color.white); return "Immune (too soon)"; }
            diminish[kind] = d;
            if (d.Item2 == 1) seconds *= .5f;
            if (Elite && (kind == "incap" || kind == "fear")) seconds *= EliteHold;
            float until = Time.time + seconds;
            switch (kind)
            {
                case "incap": incapUntil = until; heldBy = by; break;
                case "stun": stunUntil = Mathf.Max(stunUntil, until); break;
                case "fear": fearUntil = until; fearHits = 0; heldBy = by; fearFrom = by != null ? by.transform.position : transform.position; break;
                case "silence": silenceUntil = Mathf.Max(silenceUntil, until); break;
                default: return "No such hold";
            }
            CancelBlow(); CancelCast(true); wasHeld = Controlled;
            session.FloatText(transform.position, kind == "incap" ? "Held" : kind == "stun" ? "Stunned" : kind == "fear" ? "Fleeing" : "Silenced", new Color(.75f, .85f, 1));
            return null;
        }
        float lostUntil;
        /// <summary>Its raid mark (Ctrl+1 skull, Ctrl+2 moon, Ctrl+3 cross, Ctrl+4 clears; one mob a mark).</summary>
        public RaidMark RaidMarked { get; set; }
        /// <summary>What the party's helpers (sims, Mira, the Ranger's companion) don't hit: a held mob, or one marked moon or cross.</summary>
        public bool LeaveAlone { get { return Incapacitated || RaidMarked == RaidMark.Moon || RaidMarked == RaidMark.Cross; } }
        /// <summary>Lets a hold or a fear go early (an Archivist binding another mob).</summary>
        public void Release() { incapUntil = 0; fearUntil = 0; }
        /// <summary>A Rogue's Vanish: this mob forgets <paramref name="who"/> (its threat on them is gone) and doesn't notice them again
        /// for <paramref name="seconds"/>. With nobody else on its list it gives up the fight and goes home.</summary>
        public void LoseSight(Actor who, float seconds)
        {
            if (who == null || !actor.IsAlive) return;
            threat.Remove(who.EntityId.Value); lostUntil = Time.time + seconds;
            if (threat.IsEmpty) ResetFight();
        }
        /// <summary>Damage on a held mob: an incap breaks at once, a fear after three hits.</summary>
        void ControlStruck()
        {
            if (Incapacitated) { incapUntil = 0; session.FloatText(transform.position + Vector3.up * .4f, "Broken!", new Color(1, .6f, .4f)); }
            if (Feared && ++fearHits >= 3) fearUntil = 0;
        }
        /// <summary>Each frame, before it acts: true while it is held (it does nothing else this frame).</summary>
        bool ControlTick()
        {
            bool held = Controlled;
            if (!held)
            {
                if (wasHeld) { wasHeld = false; if (heldBy != null && heldBy.IsAlive) threat.Add(heldBy.EntityId.Value, ReleaseThreat); heldBy = null; }   // back for whoever held it
                return false;
            }
            wasHeld = true;
            if (agent == null || !agent.isOnNavMesh) return true;
            if (Feared && !Incapacitated && !Stunned)
            {
                var away = transform.position - fearFrom; away.y = 0; if (away.sqrMagnitude < .01f) away = transform.forward;
                agent.isStopped = false; agent.speed = baseSpeed * .9f; agent.SetDestination(transform.position + away.normalized * 6);
            }
            else { agent.isStopped = true; agent.velocity = Vector3.zero; }
            return true;
        }
    }
}
