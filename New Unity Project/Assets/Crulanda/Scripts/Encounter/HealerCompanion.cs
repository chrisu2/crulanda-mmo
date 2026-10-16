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
        /// <summary>
        /// Her mana is a limit (Chris, 2026-10-15: "mira never runs out of mana healing. need to slow the progress down"): her pool grows
        /// with your level as her heal does, but each heal costs its mana grown by the same and a quarter more, so a long fight empties
        /// her, and she drinks between fights at 8 a second (some 40 s from empty at level 11) rather than being full in 15.
        /// </summary>
        public const float HealCostMore = 1.25f;
        public static int HealCost(int cost, int playerLevel) { return Mathf.RoundToInt(cost * (1 + HealPerLevel * (Mathf.Max(1, playerLevel) - 1)) * HealCostMore); }
        public static int ManaFor(int baseMana, int playerLevel) { return Mathf.RoundToInt(baseMana * (1 + HealPerLevel * (Mathf.Max(1, playerLevel) - 1))); }
        int baseHealth, baseMana; bool warnedLow, warnedOut;
        bool SpendHeal(int cost) { bool ok = Spend(HealCost(cost, session.Player.Level)); if (!ok && session.InCombat && !warnedOut) { warnedOut = true; session.Message("Mira: I'm out of mana!"); } return ok; }
        /// <summary>Her health at level 1 (what every save held for a whole Mira before she grew with your level).</summary>
        public int BaseHealth { get { return baseHealth; } }
        /// <summary>Sets her health for the player's level (her own level stays 1: nothing reads it).</summary>
        public void MatchLevel(int playerLevel)
        {
            if (baseHealth <= 0) baseHealth = actor.Health.Pool.Max;
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, baseHealth + HealthPerLevel * (Mathf.Max(1, playerLevel) - 1));
            if (baseMana <= 0) baseMana = actor.Resource.Pool.Max;
            if (baseMana > 0) actor.Stats.SetBase(Crulanda.Core.StatType.MaxPower, ManaFor(baseMana, playerLevel));
        }
        void Awake() { agent = GetComponent<NavMeshAgent>(); }
        float nextHush;
        public const float HushCooldown = 15, HushSeconds = 4;
        /// <summary>Her longest wait in seconds (Hush, or a spell's own cooldown), for a Scoundrel's favour (ScoundrelKit, 2026-10-14).</summary>
        public float LongestWait { get { return Mathf.Max(0, Mathf.Max(nextHush - Time.time, abilities.Longest(Time.time, out _))); } }
        /// <summary>A Scoundrel's favour called in: her longest wait comes up to <paramref name="seconds"/> sooner. The seconds taken off.</summary>
        public float CallInFavour(float seconds)
        {
            float now = Time.time, hush = nextHush - now, spell = abilities.Longest(now, out var id);
            if (seconds <= 0 || (hush <= 0 && spell <= 0)) return 0;
            if (hush >= spell) { float cut = Mathf.Min(seconds, hush); nextHush -= cut; return cut; }
            float less = Mathf.Min(seconds, spell); abilities.Shorten(id, less, now); return less;
        }
        /// <summary>Hush (CC_DESIGN section 2, step C5): a mob casting at your party, past the first third of its cast, within 25 m, is silenced 4 s; every 15 s.</summary>
        void Hush()
        {
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.actor.IsAlive || !e.Casting || !e.FightingParty || e.CastProgress < .35f || Vector3.Distance(e.transform.position, transform.position) > 25) continue;
                string what = e.Cast != null ? e.Cast.name : "cast";
                if (e.Apply("silence", actor, HushSeconds) == null) { nextHush = Time.time + HushCooldown; session.Message("Mira: Hush! " + e.actor.DisplayName + "'s " + what + " is stopped."); }
                return;
            }
        }
        bool Spend(int cost)
        {
            if (actor.Resource.Pool.Current < cost) return false;
            actor.Resource.Pool.Change(-cost); return true;
        }
        void Update()
        {
            if (session == null || session.Paused) return;
            // While you are down she fights on if the party is still in a fight (2026-10-15: mobs no longer reset when you fall while a
            // party member is near them; she had stood idle, "Recovering", while they turned on her).
            bool partyFighting = !session.Player.IsAlive && session.Enemies.Exists(e => e != null && e.actor.IsAlive && e.Victim != null && e.Victim.IsAlive && session.PartyNear(e));
            if (!actor.IsAlive || (!session.Player.IsAlive && !partyFighting))
            {
                abilities.Interrupt(); castingTarget = null; Activity = "Recovering";
                if (agent.isOnNavMesh) agent.isStopped = true;
                return;
            }
            if (!session.Progress.recruited) return;
            if (Time.time >= nextRegen) { nextRegen = Time.time + 1; actor.Resource.Pool.Change(session.InCombat ? 1 : 8); }
            if (!session.InCombat) { warnedLow = warnedOut = false; }
            else if (!warnedLow && actor.Resource.Pool.Ratio < .2f) { warnedLow = true; session.Message("Mira: I'm running low on mana!"); }
            if (Time.time >= nextHush) Hush();
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
                if (stuck && NavMesh.SamplePosition(session.Player.transform.position - session.Player.transform.forward * 2, out var near, 12, NavMesh.AllAreas) && Mathf.Abs(near.position.y - (session.Player.transform.position.y - 1.05f)) < 2f) agent.Warp(near.position);   // your level only
            }
            if (agent.isOnNavMesh)
            {
                agent.isStopped = distance < 3;
                if (!agent.isStopped) agent.SetDestination(session.Player.transform.position - session.Player.transform.forward * 2);
                // She keeps your pace: a walk while you walk (hurrying when she has fallen behind), a run while you run.
                agent.speed = AdventurerMotor.Running ? 5.6f : distance > 9 ? 4.2f : 2.3f;
            }
            // Whom to mend: the most hurt of the party within reach: you, herself, the sims in your party (Chris, 2026-10-09: "Mira not
            // healing a sim in our party"), and Mother Quillet on her way to the lock (D4). You, when it is even.
            var recipient = session.Player.IsAlive ? session.Player : actor;   // you down: herself first, then the most hurt of the party
            void Consider(Actor who) { if (who != null && who.IsAlive && who.Health.Pool.Ratio < recipient.Health.Pool.Ratio && Vector3.Distance(who.transform.position, transform.position) <= heal.range) recipient = who; }
            Consider(actor);
            foreach (var c in session.PartySims) if (c != null) Consider(c.actor);
            if (session.Weaver != null) Consider(session.Weaver.actor);
            if (recipient.Health.Pool.Ratio < .78f && Vector3.Distance(recipient.transform.position, transform.position) <= heal.range)
            {
                var result = abilities.TryStart(heal, Time.time, SpendHeal, () => {
                    if (!recipient.IsAlive) return;
                    int healed = recipient.GetComponent<Combatant>().Heal(HealFor(heal.power, session.Player.Level), actor); if (healed > 0) HealFx.Show(recipient.transform);
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
            Activity = actor.Resource.Pool.Current < HealCost(heal.cost, session.Player.Level) ? "Out of mana" : !session.InCombat && actor.Resource.Pool.Ratio < .95f ? "Drinking: mana " + Mathf.RoundToInt(actor.Resource.Pool.Ratio * 100) + "%" : distance > 3 ? "Following you" : "Watching your flank";
            var target = session.Target;
            var bolt = Hasted(session.content.boltAbility);
            // Her own spells while nobody needs mending (Chris, 2026-10-15: "mira/druid should have some damage spells when not healing";
            // GAME-ONLY names): Briar Lash, instant, every 8 s, and Kindled Seed, a 2 s cast. Both cost mana grown with your level as her
            // heal's does and hit harder with it; she casts them only above half mana, so a heal is always left.
            if (target != null && target.FightingParty && target.actor.IsAlive && !target.LeaveAlone && recipient.Health.Pool.Ratio > .8f &&
                actor.Resource.Pool.Ratio > .5f && Vector3.Distance(target.transform.position, transform.position) < bolt.range)
            {
                var foe = target;
                foreach (var spell in new[] { Hasted(Lash(bolt)), Hasted(Seed(bolt)) })
                {
                    var r = abilities.TryStart(spell, Time.time, c => Spend(HealCost(c, session.Player.Level)), () => {
                        if (foe != null && foe.actor.IsAlive) foe.Receive(HealFor(spell.power, session.Player.Level), actor);
                    });
                    if (r == AbilityStartResult.Started) { Activity = "Casting " + spell.name.ToLowerInvariant(); if (spell.castTime > 0) { castingTarget = foe.actor; if (agent.isOnNavMesh) agent.isStopped = true; } return; }
                }
            }
            if (target != null && target.FightingParty && target.actor.IsAlive && !target.LeaveAlone && recipient.Health.Pool.Ratio > .8f &&
                Vector3.Distance(target.transform.position, transform.position) < bolt.range)
            {
                if (abilities.TryStart(bolt, Time.time, Spend, () => {
                    if (target != null && target.actor.IsAlive) target.Receive(bolt.power, actor);
                }) == AbilityStartResult.Started) Activity = "Casting " + bolt.name.ToLowerInvariant();
            }
        }
        /// <summary>Briar Lash: thorns whipped at your target, instant, every 8 s (GAME-ONLY).</summary>
        public static AbilitySpec Lash(AbilitySpec bolt) { return new AbilitySpec { id = "mira.briar_lash", name = "Briar Lash", description = "Thorns whipped at the foe.", effect = bolt.effect, power = 16, cost = 8, cooldown = 8, range = bolt.range, castTime = 0, globalCooldown = 1.5f }; }
        /// <summary>Kindled Seed: a seed of fire cast at your target over 2 s (GAME-ONLY).</summary>
        public static AbilitySpec Seed(AbilitySpec bolt) { return new AbilitySpec { id = "mira.kindled_seed", name = "Kindled Seed", description = "A seed of fire flung at the foe.", effect = bolt.effect, power = 30, cost = 12, cooldown = 0, range = bolt.range, castTime = 2, globalCooldown = 1.5f }; }
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
