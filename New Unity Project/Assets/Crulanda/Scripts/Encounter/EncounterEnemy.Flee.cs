using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Bolting at low health (dungeon step D5; DUNGEON_DESIGN.md section 5, the Grey Breach's crawlers): a mob whose camp sets
    /// <c>flee</c> runs from whoever it fights once its health is under that fraction, for <see cref="BoltSeconds"/>, then comes back for
    /// them (its threat is kept). Once a fight: a mob that has bolted fights on to the end. It is not a hold: it answers no call while it
    /// runs, but it is not Controlled, so the party's helpers may chase it. The nameplate says "Fleeing".
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        /// <summary>The fraction of its health it bolts at (0 = never; EncounterSession sets it from the camp).</summary>
        public float FleeAt;
        public static float BoltSeconds = 6;
        float boltUntil; bool bolted; Vector3 boltFrom;
        public bool Bolting { get { return Time.time < boltUntil; } }
        /// <summary>Each frame once it has a target: true while it runs (it neither chases nor swings).</summary>
        bool TickFlee()
        {
            if (FleeAt <= 0 || !actor.IsAlive) return false;
            if (Bolting)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    var away = transform.position - boltFrom; away.y = 0; if (away.sqrMagnitude < .01f) away = -transform.forward;
                    agent.isStopped = false; agent.speed = baseSpeed * 1.15f; agent.SetDestination(transform.position + away.normalized * 6);
                }
                return true;
            }
            if (bolted || Victim == null || actor.Health.Pool.Ratio > FleeAt) return false;
            bolted = true; boltUntil = Time.time + BoltSeconds; boltFrom = Victim.transform.position;
            CancelBlow(); CancelCast(false);
            session.FloatText(transform.position + Vector3.up * .5f, "Flees!", new Color(.8f, .85f, 1));
            session.Message(Name + " breaks and runs.");
            return true;
        }
        void ResetFlee() { bolted = false; boltUntil = 0; }
    }
}
