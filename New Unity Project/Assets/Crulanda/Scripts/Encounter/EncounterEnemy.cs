using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    public sealed class EncounterEnemy : MonoBehaviour
    {
        public string persistentId;
        public Actor actor;
        public EncounterSession session;
        /// <summary>Camp mob: respawns in its camp and is never saved (story enemies stay dead).</summary>
        public bool Camp;
        public bool Elite;
        /// <summary>Base damage per swing before the class kit resolves it (scales with level).</summary>
        public float HitBase = 13;
        public float RespawnSeconds = 75;
        public Vector2 CampCenter; public float CampRadius = 8;
        /// <summary>Camp corpses: searched already (story corpses record this in the save instead).</summary>
        public bool Looted;
        float respawnAt;
        // ---------- ambush ----------
        /// <summary>Lies low in tall grass: no nameplate, no map dot, can't be targeted, until you come close.</summary>
        public bool Ambusher;
        public bool Hidden { get; private set; }
        float lungeUntil, unreachableSince = -1;
        public void Hide()
        {
            Hidden = true; var body = transform.Find("Body");
            if (body != null) body.localPosition = new Vector3(0, -.55f, 0);   // crouched down in the grass
        }
        /// <summary>Springs out at the player: a fast lunge, a snarl, and straight into the fight.</summary>
        void Pounce()
        {
            Hidden = false; var body = transform.Find("Body"); if (body != null) body.localPosition = Vector3.zero;
            threat.Add(session.Player.EntityId.Value, 40);
            lungeUntil = Time.time + 1.1f;
            session.FloatText(transform.position + Vector3.up * .6f, "!", new Color(1, .35f, .2f));
            session.Message(actor.DisplayName + " springs out of the grass!");
        }
        // ---------- level scaling ----------
        /// <summary>Health for a mob of this level. Story enemies are sturdier; veterans 1.4x; camp elites 2.2x; beasts a little lighter.</summary>
        public static int MobHealth(int level, bool story, bool elite, bool beast)
        {
            float h = 120 + 30 * (Mathf.Max(1, level) - 1);
            if (beast) h *= .85f; if (story) h *= 1.6f; if (elite) h *= story ? 1.4f : 2.2f;
            return Mathf.RoundToInt(h);
        }
        public static float MobHit(int level, bool story, bool elite)
        {
            float d = 9 + 2.2f * (Mathf.Max(1, level) - 1);
            if (story) d *= 1.2f; if (elite) d *= 1.4f;
            return d;
        }
        public readonly EncounterThreat threat = new EncounterThreat();
        public Actor Victim { get; private set; }
        public bool Engaged { get { return Victim != null && actor.IsAlive; } }
        NavMeshAgent agent;
        Vector3 home;
        float swing, baseSpeed, slowFactor = 1, slowUntil, rootUntil;
        public bool Rooted { get { return Time.time < rootUntil; } }
        public bool Slowed { get { return Time.time < slowUntil && slowFactor < 1; } }
        public float RootRemaining { get { return Mathf.Max(0, rootUntil - Time.time); } }
        public void Initialize()
        {
            agent = GetComponent<NavMeshAgent>();
            home = transform.position; baseSpeed = agent.speed;
            actor.Health.Died += OnDeath;
        }
        /// <summary>Movement slow: the strongest active slow wins; it never stacks.</summary>
        public void Slow(float fraction, float seconds)
        {
            if (!actor.IsAlive || fraction <= 0 || seconds <= 0) return;
            float factor = Mathf.Clamp01(1 - fraction);
            if (!Slowed || factor < slowFactor) slowFactor = factor;
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }
        /// <summary>Root: cannot move, can still strike whatever is in reach. Leash distance still applies.</summary>
        public void Root(float seconds) { if (actor.IsAlive && seconds > 0) rootUntil = Mathf.Max(rootUntil, Time.time + seconds); }
        void Update()
        {
            if (session == null || session.Paused) return;
            if (!actor.IsAlive) { if (Camp && Time.time >= respawnAt) Respawn(); return; }
            if (Hidden)
            {
                // Noticing the player: about 8 m normally, 3 m if they sneak (Ctrl), never while they are dead.
                var motor = session.Player.GetComponent<AdventurerMotor>();
                float notice = motor != null && motor.Sneaking ? 3 : 8;
                if (session.Player.IsAlive && Vector3.Distance(transform.position, session.Player.transform.position) < notice) Pounce();
                return;
            }
            if (agent != null) agent.speed = Rooted ? 0 : baseSpeed * (Slowed ? slowFactor : 1) * (Time.time < lungeUntil ? 2.2f : 1);
            if (Vector3.Distance(transform.position, home) > session.Leash || !session.Player.IsAlive) { ResetFight(); return; }
            if (session.Player.IsAlive && Vector3.Distance(transform.position, session.Player.transform.position) < 5)
                threat.AddProximity(session.Player.EntityId.Value, Time.deltaTime);
            var id = threat.Choose(Time.time, session.IsLivingPartyMember);
            Victim = session.PartyActor(id);
            if (Victim == null) { if (agent.isOnNavMesh) agent.SetDestination(home); return; }
            if (Vector3.Distance(Victim.transform.position, home) > session.Leash + 2) { ResetFight(); return; }
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(Victim.transform.position); }
            // Reach: 2.6 m across the ground and 1.6 m of height (a wading target sits lower than one on the bank).
            var gap = Victim.transform.position - transform.position; float across = new Vector2(gap.x, gap.z).magnitude;
            bool inReach = across < 2.6f && Mathf.Abs(gap.y) < 1.6f;
            // Evade: a target it cannot reach (swimming, across deep water) for 4 s makes it give up and go home whole.
            bool blocked = agent.isOnNavMesh && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete;
            if (!inReach && blocked) { if (unreachableSince < 0) unreachableSince = Time.time; else if (Time.time - unreachableSince > 4) { session.Message(actor.DisplayName + " gives up the chase."); ResetFight(); return; } }
            else unreachableSince = -1;
            if (inReach && Time.time >= swing)
            {
                swing = Time.time + session.content.enemySwingInterval;
                int damage = session.Kit.ResolveEnemyHit(this, Victim, Mathf.RoundToInt(HitBase));
                session.FloatText(Victim.transform.position, "−" + damage, new Color(1, .45f, .35f));
            }
        }
        public void ResetFight()
        {
            threat.Clear(); Victim = null; slowUntil = rootUntil = 0; unreachableSince = -1;
            if (!actor.IsAlive) return;
            actor.Health.ApplyHealing(actor.Health.Pool.Max);
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(home); }
        }
        /// <summary>Seconds until this enemy may swing again (0 = ready as soon as it is in reach).</summary>
        public float NextSwingIn { get { return Mathf.Max(0, swing - Time.time); } }
        public void Stagger(float seconds) { if (seconds > 0 && actor.IsAlive) swing = Mathf.Max(swing, Time.time) + seconds; }
        public void Receive(int damage, Actor source)
        {
            if (!actor.IsAlive) return;
            if (Hidden) Pounce();
            int actual = actor.GetComponent<Combatant>().Damage(Mathf.RoundToInt(damage * session.Kit.PartyDamageMultiplier(this)));
            threat.Add(source.EntityId.Value, actual);
            session.FloatText(transform.position, actual.ToString(), new Color(1, .86f, .4f));
        }
        void OnDeath(Health health)
        {
            Victim = null;
            if (agent.isOnNavMesh) agent.isStopped = true;
            var visual = transform.Find("Body");
            if (visual != null) { visual.localRotation = Quaternion.Euler(0, 0, 90); visual.localPosition = new Vector3(0, -.6f, 0); }
            respawnAt = Time.time + RespawnSeconds;
            session.EnemyDied(this);
        }
        /// <summary>Camp mobs come back somewhere in their camp, whole, forgetting the fight.</summary>
        void Respawn()
        {
            respawnAt = float.MaxValue;
            var at = session.Zone != null ? session.Zone.Ground(CampCenter + Random.insideUnitCircle * CampRadius, 0) : home;
            if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
            if (agent.isOnNavMesh) agent.Warp(at); else transform.position = at + Vector3.up;
            home = transform.position;
            var visual = transform.Find("Body");
            if (visual != null) { visual.localRotation = Quaternion.identity; visual.localPosition = Vector3.zero; }
            actor.Health.Revive(actor.Health.Pool.Max); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            threat.Clear(); Victim = null; Looted = false; slowUntil = rootUntil = 0;
            if (agent.isOnNavMesh) agent.isStopped = false;
            if (Ambusher) Hide();
        }
        public void RestoreDead()
        {
            actor.Health.ApplyDamage(actor.Health.Pool.Max);
        }
    }
}
