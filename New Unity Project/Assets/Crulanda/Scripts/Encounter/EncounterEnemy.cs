using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    public sealed partial class EncounterEnemy : MonoBehaviour
    {
        public string persistentId;
        public Actor actor;
        public EncounterSession session;
        /// <summary>Camp mob: respawns in its camp and is never saved (story enemies stay dead).</summary>
        public bool Camp;
        /// <summary>
        /// A game animal (a deer, a rabbit: GameAnimal drives it): it never fights, bolts when hit, gives no experience and no coin,
        /// and is in the session's Game list, not its Enemies, so the village and "zone clear" never count it. Camp is set too: it
        /// comes back like a camp mob and its body is searched like one.
        /// </summary>
        public bool Game;
        /// <summary>A beast with a hide or pelt (a wolf, hound, boar or stag camp mob, and all game): E at its body reads "Skin the body".</summary>
        public bool Skinnable;
        public bool Elite;
        /// <summary>Base damage per swing before the class kit resolves it (scales with level).</summary>
        public float HitBase = 13;
        public float RespawnSeconds = 75;
        public Vector2 CampCenter; public float CampRadius = 8;
        /// <summary>Camp corpses: searched already (story corpses record this in the save instead).</summary>
        public bool Looted;
        /// <summary>Camp corpses: what lies on the body, rolled when it died (EncounterSession.RollCorpse); null while it lives. Not saved.</summary>
        public System.Collections.Generic.List<LootDrop> Drops;
        /// <summary>Camp corpses: the coins on the body, rolled with the drops.</summary>
        public int Coins;
        float respawnAt;
        // ---------- ambush ----------
        /// <summary>Lies low in tall grass: no nameplate, no map dot, can't be targeted, until you come close.</summary>
        public bool Ambusher;
        public bool Hidden { get; private set; }
        float lungeUntil, unreachableSince = -1;
        ActorVisual figure;
        /// <summary>Its figure (a modelled beast attacks and flinches; see ActorVisual.Beasts.cs).</summary>
        ActorVisual Figure { get { if (figure == null) figure = GetComponent<ActorVisual>(); return figure; } }
        public void Hide()
        {
            Hidden = true; var body = transform.Find("Body"); var look = GetComponent<ActorVisual>();
            // Crouched down in the grass; beasts lie with the belly on the ground and the legs folded, not sunk into it.
            if (body != null) body.localPosition = new Vector3(0, -(look != null && look.LieDepth > 0 ? look.LieDepth : .55f), 0);
            if (look != null) look.LyingLow = true;
        }
        /// <summary>Up out of the grass with a fast lunge (its own pounce, or a packmate's fight drawing it out).</summary>
        void Unhide()
        {
            Hidden = false; var body = transform.Find("Body"); if (body != null) body.localPosition = Vector3.zero;
            var look = GetComponent<ActorVisual>(); if (look != null) look.LyingLow = false;
            lungeUntil = Time.time + 1.1f;
            session.FloatText(transform.position + Vector3.up * .6f, "!", new Color(1, .35f, .2f));
        }
        /// <summary>Springs out at the player: a fast lunge, a snarl, and straight into the fight.</summary>
        void Pounce()
        {
            Unhide();
            threat.Add(session.Player.EntityId.Value, 40);
            session.Message(actor.DisplayName + " springs out of the grass!");
        }
        /// <summary>
        /// How far from its camp's centre (as a fraction of the camp radius) an ambusher lies. Tall grass fills its patch
        /// (camp radius + 4) only where the ground is open; elsewhere only the inner 30% is sure to be grass (GrassField).
        /// </summary>
        public static float AmbushSpread(float campRadius) { return Mathf.Min(.7f, .3f * (campRadius + 4) / Mathf.Max(1, campRadius)); }
        // ---------- level scaling ----------
        /// <summary>
        /// A camp elite against a normal mob of its level (playtest note 2; it was 2.2 times the health and 1.4 times the hit): its
        /// health, its hit at level 1, and how much more of the hit each level adds (a player's armour grows with level, a mob's
        /// swing barely does). EliteBalanceTests holds what these must give; a dungeon's end boss adds EliteMove.health and hit.
        /// </summary>
        public const float EliteHealth = 5.5f, EliteHit = 3, EliteHitPerLevel = .09f;
        /// <summary>Health for a mob of this level. Story enemies are sturdier; veterans 1.4x; camp elites <see cref="EliteHealth"/>; beasts a little lighter.</summary>
        public static int MobHealth(int level, bool story, bool elite, bool beast)
        {
            float h = 120 + 30 * (Mathf.Max(1, level) - 1);
            if (beast) h *= .85f; if (story) h *= 1.6f; if (elite) h *= story ? 1.4f : EliteHealth;
            return Mathf.RoundToInt(h);
        }
        public static float MobHit(int level, bool story, bool elite)
        {
            float d = 9 + 2.2f * (Mathf.Max(1, level) - 1);
            if (story) d *= 1.2f; if (elite) d *= story ? 1.4f : EliteHit * (1 + EliteHitPerLevel * (Mathf.Max(1, level) - 1));
            return d;
        }
        /// <summary>
        /// Overmatched: for each level a camp elite stands above the player (five at most) it hits <see cref="OverHit"/> harder and
        /// takes <see cref="OverTough"/> less from the party, so one two levels up is not a solo kill at any level. Normal mobs: see OvermatchHitNormal.
        /// </summary>
        public const float OverHit = .12f, OverTough = .06f;
        public static float OvermatchHit(int eliteLevel, int playerLevel) { return 1 + OverHit * Mathf.Clamp(eliteLevel - playerLevel, 0, 5); }
        public static float OvermatchTaken(int eliteLevel, int playerLevel) { return 1 - OverTough * Mathf.Clamp(eliteLevel - playerLevel, 0, 5); }
        /// <summary>A normal mob above the player (2026-10-05): a quarter harder a level and a tenth tougher, five levels at most, so
        /// a red mob three up hits three quarters harder and takes a third less: not a fight to take alone.</summary>
        public const float OverHitNormal = .25f, OverToughNormal = .1f;
        public static float OvermatchHitNormal(int mobLevel, int playerLevel) { return 1 + OverHitNormal * Mathf.Clamp(mobLevel - playerLevel, 0, 5); }
        public static float OvermatchTakenNormal(int mobLevel, int playerLevel) { return 1 - OverToughNormal * Mathf.Clamp(mobLevel - playerLevel, 0, 5); }
        public readonly EncounterThreat threat = new EncounterThreat();
        public Actor Victim { get; private set; }
        public bool Engaged { get { return Victim != null && actor.IsAlive; } }
        NavMeshAgent agent;
        Vector3 home;
        float swing, baseSpeed, slowFactor = 1, slowUntil, rootUntil;
        public bool Rooted { get { return Time.time < rootUntil; } }
        public bool Slowed { get { return Time.time < slowUntil && slowFactor < 1; } }
        /// <summary>What its pace is multiplied by now (1 when not slowed).</summary>
        public float SlowFactor { get { return Slowed ? slowFactor : 1; } }
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
        // Timed for the performance probe (playtest note 23).
        static readonly Unity.Profiling.ProfilerMarker perfMark = new Unity.Profiling.ProfilerMarker("PERF.Enemy");
        void Update() { using (perfMark.Auto()) UpdateTimed(); }
        void UpdateTimed()
        {
            if (session == null || session.Paused) return;
            if (!actor.IsAlive) { if (Camp && Time.time >= respawnAt) Respawn(); return; }
            if (Game) return;   // no threat, no aggro, no swing: GameAnimal grazes, wanders and bolts
            if (joinAt >= 0 && Time.time >= joinAt) Join();   // it heard a call a beat ago: now it comes
            if (Hidden)
            {
                // Noticing the player: about 8 m normally, 3 m if they sneak (Ctrl), never while they are dead.
                var motor = session.Player.GetComponent<AdventurerMotor>();
                float notice = motor != null && motor.Sneaking ? 3 : 8;
                if (session.Player.IsAlive && Vector3.Distance(transform.position, session.Player.transform.position) < notice) Pounce();
                return;
            }
            if (agent != null) agent.speed = Rooted ? 0 : baseSpeed * (Slowed ? slowFactor : 1) * (Time.time < lungeUntil ? 2.2f : 1);
            if (!session.Player.IsAlive) { ResetFight(); return; }
            // The leash. A mob fighting alone is held by its own home. A linked group (EncounterEnemy.Social) is held by where its
            // pull began, by its target only: those who came from across the camp are not sent back for having come.
            if (Group == null && Vector3.Distance(transform.position, home) > session.Leash) { if (inFight) GoHome(); else ResetFight(); return; }
            // Noticing you: within 5 m and, until a fight is on, only with a clear line to you, so a mob inside a rock cave or
            // behind a wall doesn't come for you through it. On its way home from a broken leash it notices nobody.
            if (!Evading && Vector3.Distance(transform.position, session.Player.transform.position) < 5 && (Victim != null || Sees(session.Player.transform.position)))
                threat.AddProximity(session.Player.EntityId.Value, Time.deltaTime);
            var id = threat.Choose(Time.time, session.IsLivingPartyMember);
            Victim = session.PartyActor(id);
            if (Victim == null) { if (inFight) ClearFight(); if (agent.isOnNavMesh) agent.SetDestination(home); return; }
            if (Vector3.Distance(Victim.transform.position, Group != null ? Group.anchor : home) > session.Leash + 6) { Disengage(); return; }
            if (!inFight) Engage();   // the camp hears of it (EncounterSession.RaiseAlarm)
            if (Move != null && TickElite()) return;   // an elite drawing back for its heavy blow stands still and does not swing
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(Victim.transform.position); }
            // Reach: 2.6 m across the ground and 1.6 m of height (a wading target sits lower than one on the bank).
            var gap = Victim.transform.position - transform.position; float across = new Vector2(gap.x, gap.z).magnitude;
            bool inReach = across < 2.6f && Mathf.Abs(gap.y) < 1.6f;
            // No coasting into (and through) the target after a charge or a lunge out of the grass: stop dead on arrival.
            if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) agent.velocity = Vector3.zero;
            // Evade: a target it cannot reach (swimming, across deep water) for 4 s makes it give up and go home whole.
            bool blocked = agent.isOnNavMesh && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete;
            if (!inReach && blocked) { if (unreachableSince < 0) unreachableSince = Time.time; else if (Time.time - unreachableSince > 4) { session.Message(actor.DisplayName + " gives up the chase."); GoHome(); return; } }
            else unreachableSince = -1;
            if (inReach && Time.time >= swing)
            {
                if (Move != null && Time.time >= nextBlowAt) { BeginBlow(); return; }
                swing = Time.time + SwingInterval;
                if (Figure != null) Figure.Strike();
                int damage = session.Kit.ResolveEnemyHit(this, Victim, Mathf.RoundToInt(HitBase * OverHitNow));
                session.FloatText(Victim.transform.position, "−" + damage, new Color(1, .45f, .35f));
            }
        }
        static readonly RaycastHit[] sightHits = new RaycastHit[16];
        /// <summary>
        /// A clear line from this mob's eyes to the point's chest height. Static scenery (ground, rock, walls, trunks) blocks
        /// it; actors and triggers don't. Cast both ways, because a one-sided mesh (a cave's rock skin) only blocks from its
        /// front.
        /// </summary>
        bool Sees(Vector3 target)
        {
            Vector3 eye = transform.position + Vector3.up * .6f, chest = target + Vector3.up * .4f;
            return Clear(eye, chest) && Clear(chest, eye);
        }
        static bool Clear(Vector3 from, Vector3 to)
        {
            var line = to - from; float length = line.magnitude; if (length < .01f) return true;
            int n = Physics.RaycastNonAlloc(from, line / length, sightHits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (sightHits[i].collider.GetComponentInParent<Actor>() == null) return false;
            return true;
        }
        public void ResetFight()
        {
            ClearFight();
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
            int actual = actor.GetComponent<Combatant>().Damage(Mathf.RoundToInt(damage * session.Kit.PartyDamageMultiplier(this) * OverTakenNow));
            if (actor.IsAlive && Figure != null) Figure.Flinch();
            if (Game) { if (actor.IsAlive) GetComponent<GameAnimal>()?.Bolt(source != null ? source.transform.position : transform.position); }
            else { if (!inFight) struck = true; threat.Add(source.EntityId.Value, actual); }   // struck: it was hit before it noticed anyone, so sneaking does not quieten its alarm
            session.FloatText(transform.position, actual.ToString(), new Color(1, .86f, .4f));
        }
        void OnDeath(Health health)
        {
            ClearFight();   // before the death pose: a blow half drawn leaves the body leaning
            Victim = null;
            if (agent.isOnNavMesh) agent.isStopped = true;
            var visual = transform.Find("Body");
            if (Game) GetComponent<GameAnimal>()?.Fall();   // on its side on the ground, not tipped from an actor's height
            else if (visual != null) { visual.localRotation = Quaternion.Euler(0, 0, 90); visual.localPosition = new Vector3(0, -.6f, 0); }
            respawnAt = Time.time + RespawnSeconds;
            session.EnemyDied(this);
        }
        /// <summary>Camp mobs come back somewhere in their camp, whole, forgetting the fight.</summary>
        void Respawn()
        {
            respawnAt = float.MaxValue;
            var at = session.Zone != null ? session.Zone.StandAt(CampCenter + Random.insideUnitCircle * CampRadius * (Ambusher ? AmbushSpread(CampRadius) : 1), home.y) : home;   // a cave camp respawns on its floor
            if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
            if (agent.isOnNavMesh) agent.Warp(at); else transform.position = at + Vector3.up;
            home = transform.position;
            var visual = transform.Find("Body");
            if (visual != null) { visual.localRotation = Quaternion.identity; visual.localPosition = Vector3.zero; }
            actor.Health.Revive(actor.Health.Pool.Max); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            ClearFight(); evadeUntil = 0;
            threat.Clear(); Victim = null; Looted = false; slowUntil = rootUntil = 0;
            Drops = null; Coins = 0; LootBeacon.Clear(this);
            if (agent.isOnNavMesh) agent.isStopped = false;
            if (Game) GetComponent<GameAnimal>()?.Stand();
            if (Ambusher) Hide();
        }
        public void RestoreDead()
        {
            actor.Health.ApplyDamage(actor.Health.Pool.Max);
        }
    }
}
