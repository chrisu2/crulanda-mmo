using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.Abilities;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    public sealed class HealerCompanion : MonoBehaviour
    {
        public Actor actor;
        public EncounterSession session;
        public string Activity { get; private set; } = "Waiting for you";
        public float CastProgress { get { return abilities.CastProgress(Time.time); } }
        readonly AbilityRuntime abilities = new AbilityRuntime();
        NavMeshAgent agent;
        Actor castingTarget;
        float nextRegen, nextCatchUp;
        /// <summary>
        /// Mira keeps pace with you (playtest note 2: an elite of your level wants her, at every level): each of your levels past
        /// the first adds <see cref="HealPerLevel"/> of her heal and <see cref="HealthPerLevel"/> health. Her mana and its cost stay.
        /// </summary>
        public const float HealPerLevel = .15f; public const int HealthPerLevel = 22;
        public static int HealFor(int power, int playerLevel, float perLevel = HealPerLevel) { return Mathf.RoundToInt(power * (1 + perLevel * (Mathf.Max(1, playerLevel) - 1))); }
        int baseHealth;
        /// <summary>Her health at level 1 (what every save held for a whole Mira before she grew with your level).</summary>
        public int BaseHealth { get { return baseHealth; } }
        /// <summary>Sets her health for the player's level (her own level stays 1: nothing reads it).</summary>
        public void MatchLevel(int playerLevel)
        {
            if (baseHealth <= 0) baseHealth = actor.Health.Pool.Max;
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, baseHealth + HealthPerLevel * (Mathf.Max(1, playerLevel) - 1));
        }
        void Awake() { agent = GetComponent<NavMeshAgent>(); }
        bool Spend(int cost)
        {
            if (actor.Resource.Pool.Current < cost) return false;
            actor.Resource.Pool.Change(-cost); return true;
        }
        void Update()
        {
            if (session == null || session.Paused) return;
            if (!actor.IsAlive || !session.Player.IsAlive)
            {
                abilities.Interrupt(); castingTarget = null; Activity = "Recovering";
                if (agent.isOnNavMesh) agent.isStopped = true;
                return;
            }
            if (!session.Progress.recruited) return;
            if (Time.time >= nextRegen) { nextRegen = Time.time + 1; actor.Resource.Pool.Change(session.InCombat ? 1 : 8); }
            var heal = Hasted(session.content.healingAbility);
            if (abilities.IsCasting)
            {
                bool valid = castingTarget != null && castingTarget.IsAlive &&
                    Vector3.Distance(castingTarget.transform.position, transform.position) <= heal.range;
                abilities.Tick(Time.time, valid);
                if (!abilities.IsCasting) castingTarget = null;
                return;
            }
            float distance = Vector3.Distance(transform.position, session.Player.transform.position);
            // Never stranded: off the navmesh, or left far behind with no way through (deep water), she catches up.
            if (Time.time >= nextCatchUp)
            {
                nextCatchUp = Time.time + 1;
                bool stuck = !agent.isOnNavMesh || (distance > 25 && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete);
                if (stuck && NavMesh.SamplePosition(session.Player.transform.position - session.Player.transform.forward * 2, out var near, 12, NavMesh.AllAreas)) agent.Warp(near.position);
            }
            if (agent.isOnNavMesh)
            {
                agent.isStopped = distance < 3;
                if (!agent.isStopped) agent.SetDestination(session.Player.transform.position - session.Player.transform.forward * 2);
                // She keeps your pace: a walk while you walk (hurrying when she has fallen behind), a run while you run.
                agent.speed = AdventurerMotor.Running ? 5.6f : distance > 9 ? 4.2f : 2.3f;
            }
            var recipient = actor.Health.Pool.Ratio < session.Player.Health.Pool.Ratio ? actor : session.Player;
            if (recipient.Health.Pool.Ratio < .78f && Vector3.Distance(recipient.transform.position, transform.position) <= heal.range)
            {
                var result = abilities.TryStart(heal, Time.time, Spend, () => {
                    if (!recipient.IsAlive) return;
                    int healed = recipient.GetComponent<Combatant>().Heal(HealFor(heal.power, session.Player.Level)); if (healed > 0) HealFx.Show(recipient.transform);
                    session.FloatText(recipient.transform.position, "+" + healed, new Color(.3f,1,.7f));
                    session.HealThreat(actor, healed);   // shared out among the mobs in the fight (EncounterSession.Social)
                });
                if (result == AbilityStartResult.Started)
                {
                    castingTarget = recipient; Activity = "Casting " + heal.name.ToLowerInvariant();
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    return;
                }
            }
            Activity = actor.Resource.Pool.Current < heal.cost ? "Recovering mana" : distance > 3 ? "Following you" : "Watching your flank";
            var target = session.Target;
            var bolt = Hasted(session.content.boltAbility);
            if (target != null && target.FightingParty && target.actor.IsAlive && recipient.Health.Pool.Ratio > .8f &&
                Vector3.Distance(target.transform.position, transform.position) < bolt.range)
            {
                if (abilities.TryStart(bolt, Time.time, Spend, () => {
                    if (target != null && target.actor.IsAlive) target.Receive(bolt.power, actor);
                }) == AbilityStartResult.Started) Activity = "Casting " + bolt.name.ToLowerInvariant();
            }
        }
        /// <summary>Called Cadence: shorter cast, cooldown and global cooldown while the Warrior's rally lasts.</summary>
        AbilitySpec Hasted(AbilitySpec a)
        {
            float h = session.Kit == null ? 0 : session.Kit.CompanionHaste;
            if (h <= 0) return a;
            float f = 1 / (1 + h);
            return new AbilitySpec { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = a.cost,
                cooldown = a.cooldown * f, range = a.range, castTime = a.castTime * f, globalCooldown = a.globalCooldown * f,
                duration = a.duration, statusId = a.statusId };
        }
        void OnDisable() { abilities.Interrupt(); castingTarget = null; }
    }
}
