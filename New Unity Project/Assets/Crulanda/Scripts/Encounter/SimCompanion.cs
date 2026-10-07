using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A sim in your party (Phase 5.2b, 2026-10-06; Chris chose a basic invite ahead of the full party system of 5.6): it keeps
    /// at your side like Mira, fights what you fight in its class's way, and is a party member the enemies can turn on
    /// (EncounterSession.PartyActor). Warriors and Paladins close to melee (the Warrior's blows hold attention like a tank's);
    /// Rangers shoot arrows and Mages throw fire from range; Druids throw thorns from range; Druids and Paladins also mend the
    /// most hurt of the party. Its numbers follow its level. It falls when beaten and gets up again once the fight is over.
    /// Not saved: a party is for the session (the sim goes back to the world when you leave or it is dismissed).
    /// </summary>
    public sealed class SimCompanion : MonoBehaviour
    {
        public Actor actor; public EncounterSession session; public SimAdventurer sim; public int slot;
        NavMeshAgent agent; ActorVisual visual;
        float nextAct, nextHeal, nextCatchUp, downSince = -1;
        public string Activity { get; private set; } = "Following you";
        public bool Melee { get { return sim.classId == "class.warrior" || sim.classId == "class.paladin"; } }
        public bool Healer { get { return sim.classId == "class.druid" || sim.classId == "class.paladin"; } }
        public float Reach { get { return Melee ? 2.4f : 20; } }
        public float Interval { get { return Melee ? 2.0f : 2.4f; } }
        public int Hit { get { return Mathf.RoundToInt((Melee ? 6 : 5) + (Melee ? 2.4f : 2.2f) * sim.level); } }
        public int HealAmount { get { return Mathf.RoundToInt(12 + 5 * sim.level); } }
        /// <summary>Experience from the party's kills (EncounterSession.EnemyDied): a level when it has enough, said in Party.</summary>
        public void GainXp(int xp)
        {
            if (xp <= 0 || sim.level >= EncounterProgress.LevelCap) return;
            sim.experience = Mathf.Max(sim.experience, EncounterProgress.XpForLevel(sim.level)) + xp;
            if (sim.experience >= EncounterProgress.XpForLevel(sim.level + 1))
            {
                sim.level++; actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, MaxHealthFor(sim)); actor.Health.ApplyHealing(actor.Health.Pool.Max);
                SimGear.Dress(GetComponent<ActorVisual>(), sim, session.Items); SimChatter.Active?.Ding(sim, true);
            }
        }
        public static int MaxHealthFor(SimAdventurer s) { return (s.classId == "class.warrior" || s.classId == "class.paladin" ? 150 : 115) + 22 * Mathf.Max(1, s.level); }

        public void Init(Actor a, EncounterSession s, SimAdventurer data, int partySlot)
        {
            actor = a; session = s; sim = data; slot = partySlot; agent = GetComponent<NavMeshAgent>(); visual = GetComponent<ActorVisual>();
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, MaxHealthFor(sim)); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            nextAct = Time.time + .5f + slot * .3f;
        }
        /// <summary>The enemy it should be fighting: your target when it is in a fight, otherwise whatever is on one of the party.</summary>
        public EncounterEnemy Quarry
        {
            get
            {
                var t = session.Target;
                if (t != null && t.actor.IsAlive && !t.Game && (t.FightingParty || session.AutoAttack)) return t;
                EncounterEnemy best = null; float bestD = 30;
                foreach (var e in session.Enemies)
                {
                    if (e == null || !e.actor.IsAlive || !e.FightingParty) continue;
                    float d = Vector3.Distance(e.transform.position, transform.position); if (d < bestD) { best = e; bestD = d; }
                }
                return best;
            }
        }
        void Update()
        {
            if (session == null || session.Paused || actor == null) return;
            var player = session.Player; if (player == null) return;
            if (!actor.IsAlive)
            {
                Activity = "Fallen"; if (agent.isOnNavMesh) agent.isStopped = true;
                if (downSince < 0) downSince = Time.time;
                // Up again once the fight is over (a sim runs back from its own graveyard in a later phase).
                if (!session.InCombat && Time.time - downSince > 6) { actor.Health.Revive(Mathf.RoundToInt(actor.Health.Pool.Max * .4f)); downSince = -1; session.Message(sim.name + " gets back up."); }
                return;
            }
            float toPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (Time.time >= nextCatchUp)
            {
                nextCatchUp = Time.time + 1;
                bool stuck = !agent.isOnNavMesh || (toPlayer > 25 && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete) || toPlayer > 60;
                if (stuck && NavMesh.SamplePosition(player.transform.position - player.transform.forward * 2.5f, out var near, 12, NavMesh.AllAreas)) agent.Warp(near.position);
            }
            if (!agent.isOnNavMesh) return;
            if (!session.InCombat && Time.time >= nextHeal) { nextHeal = Time.time + 1; actor.Health.ApplyHealing(Mathf.Max(3, actor.Health.Pool.Max / 40)); }
            // Mending first, for the healers: the most hurt of the party under 60%.
            if (Healer && session.InCombat && Time.time >= nextHeal)
            {
                var hurt = MostHurt();
                if (hurt != null && hurt.Health.Pool.Ratio < .6f && Vector3.Distance(hurt.transform.position, transform.position) <= 25)
                {
                    nextHeal = Time.time + 6; nextAct = Mathf.Max(nextAct, Time.time + 1.2f);
                    int healed = hurt.GetComponent<Combatant>().Heal(HealAmount);
                    session.FloatText(hurt.transform.position, "+" + healed, new Color(.3f, 1, .7f)); session.HealThreat(actor, healed);
                    visual?.CastRelease(); Activity = "Mending " + (hurt == player ? "you" : hurt.DisplayName);
                    return;
                }
            }
            var quarry = Quarry;
            if (quarry != null)
            {
                float d = Vector3.Distance(transform.position, quarry.transform.position);
                if (d > Reach) { agent.isStopped = false; agent.speed = 5.2f; agent.SetDestination(quarry.transform.position); Activity = "Closing on " + quarry.actor.DisplayName; return; }
                agent.isStopped = true;
                var face = quarry.transform.position - transform.position; face.y = 0;
                if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8);
                Activity = (Melee ? "Fighting " : "Shooting at ") + quarry.actor.DisplayName;
                if (Time.time >= nextAct)
                {
                    nextAct = Time.time + Interval;
                    int dmg = Hit;
                    if (Melee) visual?.Strike();
                    else
                    {
                        visual?.CastRelease();
                        var hand = visual != null ? visual.RightHandle : null; var from = hand != null ? hand.position : transform.position + Vector3.up * 1.3f;
                        bool arrow = sim.classId == "class.ranger";
                        var colour = sim.classId == "class.mage" ? new Color(1, .55f, .15f) : sim.classId == "class.druid" ? new Color(.45f, .85f, .3f) : new Color(.9f, .88f, .8f);
                        Bolt.Fire(from, quarry.transform, colour, arrow ? .1f : .18f, arrow ? .22f : .28f, arrow);
                    }
                    quarry.Receive(dmg, actor);
                    if (sim.classId == "class.warrior") quarry.threat.Add(actor.EntityId.Value, dmg * 1.5f);   // a warrior's blows hold attention
                }
                return;
            }
            // At your side: a pace behind, each in its own place so they do not stack.
            Activity = toPlayer > 4 ? "Following you" : "At your side";
            var side = (slot % 2 == 0 ? -1 : 1) * (1.3f + .6f * (slot / 2));
            var spot = player.transform.position - player.transform.forward * (2.2f + .5f * slot) + player.transform.right * side;
            agent.isStopped = toPlayer < 2.8f;
            if (!agent.isStopped) { agent.SetDestination(spot); agent.speed = AdventurerMotor.Running ? 5.6f : toPlayer > 9 ? 4.2f : 2.3f; }
        }
        Actor MostHurt()
        {
            Actor best = null; float low = 1;
            void Consider(Actor a) { if (a != null && a.IsAlive && a.Health.Pool.Ratio < low) { best = a; low = a.Health.Pool.Ratio; } }
            Consider(session.Player);
            if (session.Progress.recruited && session.Companion != null) Consider(session.Companion.actor);
            foreach (var p in session.PartySims) if (p != null) Consider(p.actor);
            return best;
        }
    }
}
