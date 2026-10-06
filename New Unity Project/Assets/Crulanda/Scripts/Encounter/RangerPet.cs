using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Ranger's wolf (Phase 5.1b): a grey wolf on the pack's model (ActorLook.Wolf) that keeps to heel like Mira does, hunts
    /// what the Ranger sets it on (Sic; with Pack Sense whatever the Ranger shoots), bites every two seconds with threat of its
    /// own, is a party member the enemies can turn on (EncounterSession.PartyActor) and falls in its own death. Its health and
    /// bite follow the Ranger's level and talents (Thick Coat, Sharp Teeth, Alpha, Howl). Not saved: Call Companion whistles it
    /// up each session.
    /// </summary>
    public sealed class RangerPet : MonoBehaviour
    {
        public Actor actor;
        public EncounterSession session;
        public RangerKit kit;
        NavMeshAgent agent; ActorVisual visual;
        float nextBite, nextCatchUp, fury = 1; int baseHealth;
        /// <summary>What it is hunting (null at heel).</summary>
        public EncounterEnemy Quarry { get; private set; }
        public const float BiteInterval = 2, BiteReach = 2.2f;

        public void Init(Actor a, EncounterSession s, RangerKit k)
        {
            actor = a; session = s; kit = k; agent = GetComponent<NavMeshAgent>(); visual = GetComponent<ActorVisual>();
            baseHealth = 90 + 18 * Mathf.Max(1, session.Progress.Level);
            Refit(); actor.Health.ApplyHealing(actor.Health.Pool.Max);
        }
        /// <summary>Its health for the Ranger's level and talents (kept whole when it grows).</summary>
        public void Refit()
        {
            if (actor == null || kit == null) return;
            int max = Mathf.RoundToInt(baseHealth * kit.WolfHealthScale);
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, max);
        }
        public int Bite { get { return Mathf.Max(1, Mathf.RoundToInt((6 + 2 * Mathf.Max(1, session.Progress.Level)) * kit.WolfBiteScale)); } }
        /// <summary>Sent at a target; its first bite lands <paramref name="firstBite"/> times as hard (Sic).</summary>
        public void Hunt(EncounterEnemy target, float firstBite)
        {
            if (target == null || !target.actor.IsAlive || !actor.IsAlive) return;
            if (Quarry != target) { Quarry = target; fury = Mathf.Max(fury, firstBite); } else fury = Mathf.Max(fury, firstBite);
        }
        void Update()
        {
            if (session == null || session.Paused || actor == null || !actor.IsAlive) return;
            var player = session.Player; if (player == null) return;
            if (Quarry != null && (!Quarry.actor.IsAlive || !Quarry.Engaged && session.Distance(Quarry) > 40)) { Quarry = null; fury = 1; }
            float toPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (Time.time >= nextCatchUp)
            {
                nextCatchUp = Time.time + 1;
                bool stuck = !agent.isOnNavMesh || (toPlayer > 30 && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete);
                if (stuck && NavMesh.SamplePosition(player.transform.position - player.transform.forward * 2, out var near, 12, NavMesh.AllAreas)) agent.Warp(near.position);
            }
            if (!agent.isOnNavMesh) return;
            if (Quarry != null)
            {
                float reach = Vector3.Distance(transform.position, Quarry.transform.position);
                agent.speed = 5.5f; agent.isStopped = reach <= BiteReach;
                if (!agent.isStopped) agent.SetDestination(Quarry.transform.position);
                else
                {
                    var face = Quarry.transform.position - transform.position; face.y = 0;
                    if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8);
                    if (Time.time >= nextBite)
                    {
                        nextBite = Time.time + BiteInterval;
                        int bite = Mathf.RoundToInt(Bite * fury); fury = 1;
                        Quarry.Receive(bite, actor);
                        if (kit.WolfHowls) Quarry.threat.Add(actor.EntityId.Value, bite);   // Howl: its bites hold attention like a tank's
                        visual?.Strike();
                    }
                }
                return;
            }
            // At heel: a pace behind the Ranger's shoulder, at the Ranger's own pace.
            agent.isStopped = toPlayer < 2.5f;
            if (!agent.isStopped) { agent.SetDestination(player.transform.position - player.transform.forward * 1.5f + player.transform.right * 1.2f); agent.speed = AdventurerMotor.Running ? 5.8f : toPlayer > 9 ? 4.5f : 2.4f; }
        }
    }
}
