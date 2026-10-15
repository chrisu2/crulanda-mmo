using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Dormant mobs (Chris, 2026-10-15: "whole zone needs optimized laggy"; the Adit's probe: 186 mobs everywhere cost some 30 ms a frame
    /// at the yard and in the Tally Hall). A camp's mob alive, fighting nobody and standing still, more than <see cref="SleepRange"/> from
    /// the player sleeps: its own update, its figure's and its animators stop and its renderers draw nothing (forceRenderingOff, a flag
    /// apart from the game's own hiding, so a hidden ambusher stays hidden). It wakes inside <see cref="WakeRange"/>. Checked twice a
    /// second by EncounterSession.TickDormant.
    /// </summary>
    public sealed partial class EncounterEnemy
    {
        public const float SleepRange = 85, WakeRange = 75;
        public bool Dormant { get; private set; }
        Animator[] sleepAnimators; Renderer[] sleepRenderers; ActorVisual sleepVisual;
        /// <summary>Whether it may sleep now: a camp's mob, alive, with no one to fight, not walking anywhere.</summary>
        public bool CanSleep { get { return Camp && actor != null && actor.IsAlive && Victim == null && !threat.Any && !Evading && (agent == null || !agent.enabled || !agent.hasPath || agent.remainingDistance < .5f); } }
        public void SetDormant(bool sleep)
        {
            if (sleep == Dormant) return;
            if (sleep)
            {
                sleepAnimators = GetComponentsInChildren<Animator>(); sleepRenderers = GetComponentsInChildren<Renderer>(); sleepVisual = GetComponent<ActorVisual>();
                if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
                strolling = false;
            }
            Dormant = sleep; enabled = !sleep;
            if (sleepVisual != null) sleepVisual.enabled = !sleep;
            if (sleepAnimators != null) foreach (var a in sleepAnimators) if (a != null) a.enabled = !sleep;
            if (sleepRenderers != null) foreach (var r in sleepRenderers) if (r != null) r.forceRenderingOff = sleep;
            if (!sleep) idleUntil = Time.time + Random.Range(.5f, 3f);
        }
    }
}
