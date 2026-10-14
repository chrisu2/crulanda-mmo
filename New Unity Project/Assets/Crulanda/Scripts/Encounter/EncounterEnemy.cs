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
        /// <summary>Elite-strength without being an elite (a group zone's mobs, round 29): an elite's health, blows and kill XP.</summary>
        public bool Tough;
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
        /// <summary>The beast it rides (Nix on the Rock-Eater, D5): in the saddle, seen but out of reach and deaf to calls, until it falls.</summary>
        public EncounterEnemy Inside { get; private set; }
        /// <summary>Climbs into <paramref name="mount"/>'s saddle (ActorVisual.RiderSeat): seen, seated, deaf to calls and beyond harm until
        /// the session brings it down (Rise). (Chris, 2026-10-11, no machines: Nix had sat hidden inside a boring-engine.)</summary>
        public void HideInside(EncounterEnemy mount)
        {
            if (mount == null || !actor.IsAlive) return;
            Inside = mount; Hide(); Shown(true);
            var seated = transform.Find("Body"); if (seated != null) seated.localPosition = Vector3.zero;   // sitting up in the saddle, not crouched as in grass
            if (agent != null) agent.enabled = false;
            var look = GetComponent<ActorVisual>(); if (look != null) { look.LyingLow = false; look.Pose = ActorPose.Sit; look.Riding = true; }
            Ride();
        }
        /// <summary>In the saddle: where the mount's seat is, facing its way (each frame while riding).</summary>
        void Ride()
        {
            var seat = Inside != null ? Inside.GetComponent<ActorVisual>()?.RiderSeat : null; if (seat == null) return;
            transform.SetPositionAndRotation(seat.position + Vector3.up * RiderLift, Quaternion.Euler(0, seat.eulerAngles.y, 0));
        }
        /// <summary>How far above the seat a rider's centre sits (an actor's centre is a metre over its feet; seated, its hips are half that).</summary>
        public const float RiderLift = .45f;
        /// <summary>Up out of hiding when the session says so (Rail-Captain Danner at the Weaver's half-matched lock; Nix out of the broken
        /// Rock-Eater), and after <paramref name="at"/> if it is not in a fight already.</summary>
        public void Rise(Actor at)
        {
            if (!actor.IsAlive) return;
            if (Inside != null)
            {
                Inside = null; Shown(true);
                var look = GetComponent<ActorVisual>(); if (look != null) { look.Riding = false; if (look.Pose == ActorPose.Sit) look.Pose = ActorPose.None; }
                if (agent != null && !agent.enabled) { agent.enabled = true; if (NavMesh.SamplePosition(transform.position, out var hit, 4, NavMesh.AllAreas)) agent.Warp(hit.position); }
            }
            if (Hidden) Unhide();
            if (at != null && !Engaged) threat.Add(at.EntityId.Value, JoinThreat);
        }
        /// <summary>The figure's parts shown or hidden: never the Body's own placeholder capsule, which ActorVisual.Build switched off for good
        /// (the boars came back as grey capsules, 2026-10-09).</summary>
        void Shown(bool on) { var body = transform.Find("Body"); if (body != null) foreach (var r in body.GetComponentsInChildren<Renderer>(true)) if (r.gameObject != body.gameObject) r.enabled = on; }
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
        /// <summary>Fighting you or one of your party (not a sim out on its own: Phase 5.3a).</summary>
        public bool FightingParty { get { return Engaged && session.PartyActor(Victim.EntityId.Value) != null; } }
        /// <summary>Whose kill it is: the first to hit it (your party, or a sim on its own). Cleared when the fight resets.</summary>
        public string TappedBy { get; private set; }
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
            idleUntil = Time.time + Random.Range(2f, 8f);   // they do not all set off at once
            actor.Health.Died += OnDeath;
        }
        // ---------- with nobody to fight (playtest note 131, Chris, 2026-10-11: "shouldnt spawn almost on top of each other. should all
        // patrol. all mobs just stand in one place"): every mob had walked back to the spot it was spawned on, every frame ----------
        /// <summary>A patrol's road, walked back and forth while it has nobody to fight (ZoneCamp.route); null: it strolls about its camp.</summary>
        public Vector3[] Patrol;
        /// <summary>How far apart two of a camp keep (a bear's length, a man's shoulders; EncounterSession.GapFor).</summary>
        public float Gap = 1.4f;
        /// <summary>A stroll's pace against its run: about 1.2 m/s for most.</summary>
        public const float StrollPace = .42f;
        float idleUntil; int patrolAt, patrolStep = 1; bool strolling; static NavMeshPath strollPath;
        /// <summary>Walking somewhere of its own (a stroll or a patrol's leg), not fighting.</summary>
        public bool Strolling { get { return strolling && Victim == null; } }
        static float Across(Vector3 v) { v.y = 0; return v.magnitude; }
        /// <summary>A camp's mobs stroll about their camp, a few seconds at each spot, a body's width from each other; a patrol walks its
        /// road back and forth (its leash held from where it is met, not where it set out); on the way home from a broken leash it only goes
        /// home. A mob of no camp (a story mob, a wave of the escort) and one riding inside another keep their place.</summary>
        /// <summary>Beyond this from the player a camp's mob stands rather than strolls (the CPU, below); patrols walk at any range.</summary>
        public const float StrollRange = 70; float homeAskAt;
        void Idle()
        {
            if (!agent.isOnNavMesh) return;
            // The walk home from a broken leash counts as a stroll: it is finished, then a rest, before the camp is strolled again
            // (SocialPullTests, 2026-10-13: a wolf whose three seconds of evading ran out short of home strolled off from there).
            if (!Camp || Evading || Inside != null) { agent.SetDestination(home); strolling = Camp && Evading; return; }
            // Far from everyone (no party member within 70 m): it stands; the strolling's path-finding for a hundred and fifty mobs at once pegged
            // the CPU (Chris, 2026-10-11, watching the group run: "pegging cpu 100% alot of times").
            if (Patrol == null && session.Player != null && (transform.position - session.Player.transform.position).sqrMagnitude > StrollRange * StrollRange)
            {
                // Not yet home (back from a chase): it walks home first, then stands (SocialPullTests, 2026-10-13: a linked group's second mob stopped short of home).
                // Asked at most every 2 s and only when not already on its way: a beast halts 2.1 m short, and asking every frame cost a
                // path search a frame for every mob out there (the CPU the 70 m rule was for).
                var toHome = home - transform.position; toHome.y = 0; float near = Mathf.Max(1.5f, agent.stoppingDistance + .5f);
                if (toHome.sqrMagnitude > near * near)
                {
                    strolling = false; var heading = agent.destination - home; heading.y = 0;
                    if (Time.time >= homeAskAt && !agent.pathPending && (!agent.hasPath || heading.sqrMagnitude > 1)) { agent.SetDestination(home); homeAskAt = Time.time + 2; }
                    return;
                }
                if (strolling) { strolling = false; agent.ResetPath(); }
                return;
            }
            if (Patrol != null) home = transform.position;
            agent.speed = baseSpeed * StrollPace * (Slowed ? slowFactor : 1);
            bool there = !agent.pathPending && agent.remainingDistance <= Mathf.Max(.5f, agent.stoppingDistance);
            if (strolling && !there) return;
            if (strolling) { strolling = false; idleUntil = Time.time + (Patrol != null ? Random.Range(1.5f, 4f) : Random.Range(3f, 9f)); }
            if (Time.time < idleUntil) return;
            Vector3 to;
            if (Patrol != null && Patrol.Length > 1)
            {
                if (patrolAt + patrolStep < 0 || patrolAt + patrolStep >= Patrol.Length) patrolStep = -patrolStep;
                patrolAt += patrolStep; to = Patrol[patrolAt];
            }
            else if (!StrollSpot(out to)) { idleUntil = Time.time + 2; return; }
            if (NavMesh.SamplePosition(to, out var hit, Patrol != null ? 4 : 2, NavMesh.AllAreas)) { agent.isStopped = false; agent.SetDestination(hit.position); strolling = true; }
            else idleUntil = Time.time + 2;
        }
        /// <summary>A spot to stroll to in its camp (within the camp's radius, two and a half metres at the least; an elite paces a round of
        /// its own about where it stands), a step or more from where it is, clear of the others of its camp, where they stand and where they
        /// are going, by their width. (A lord's guards and the lord, their camps a metre or so across, had barely shifted: still standing.)</summary>
        bool StrollSpot(out Vector3 to)
        {
            var centre = Elite ? new Vector2(home.x, home.z) : CampCenter;
            float reach = Elite ? 2.5f : Mathf.Max(2.5f, CampRadius);
            for (int k = 0; k < 8; k++)
            {
                var p = centre + Random.insideUnitCircle * reach;
                var point = session.Zone != null ? session.Zone.StandAt(p, home.y) : new Vector3(p.x, home.y, p.y);
                if (Across(point - transform.position) < 1.2f || !ClearOfCamp(point)) continue;
                // On the floor itself, and straight there: not over lava or in a wall, and not the long way round a lake (the Ember Vent's
                // camps by its lava had walked round it and out of their camps).
                if (!NavMesh.SamplePosition(point, out var at, 1, NavMesh.AllAreas) || Mathf.Abs(at.position.y - home.y) > 1) continue;   // on its own floor, not a ledge or a column's top
                if (strollPath == null) strollPath = new NavMeshPath();
                if (!NavMesh.CalculatePath(transform.position, at.position, NavMesh.AllAreas, strollPath) || strollPath.status != NavMeshPathStatus.PathComplete) continue;
                float length = 0; var corners = strollPath.corners; for (int c = 1; c < corners.Length; c++) length += Vector3.Distance(corners[c - 1], corners[c]);
                if (length > Across(at.position - transform.position) * 1.5f + 1.5f) continue;
                to = at.position; return true;
            }
            to = home; return false;
        }
        /// <summary>No other living mob of its camp stands within its width of <paramref name="point"/>, or is walking there.</summary>
        bool ClearOfCamp(Vector3 point)
        {
            foreach (var e in session.Enemies)
                if (e != null && e != this && e.actor.IsAlive && e.Camp && e.CampCenter == CampCenter &&
                    (Across(e.transform.position - point) < Gap || (e.strolling && e.agent != null && e.agent.isOnNavMesh && Across(e.agent.destination - point) < Gap))) return false;
            return true;
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
            if (ControlTick()) return;   // held, stunned or fleeing: nothing else this frame (EncounterEnemy.Control)
            if (joinAt >= 0 && Time.time >= joinAt) Join();   // it heard a call a beat ago: now it comes
            if (Hidden)
            {
                if (Inside != null) { if (Inside.actor.IsAlive) Ride(); return; }   // in the saddle: down when the session says (its mount fell), not before
                // Noticing the player: about 8 m normally, 3 m if they sneak (Ctrl), never while they are dead.
                var motor = session.Player.GetComponent<AdventurerMotor>();
                float notice = Mathf.Max(1.5f, (motor != null && motor.Sneaking ? 3 : 8) - session.Kit.NoticeShrink);
                if (session.Player.IsAlive && !EncounterSession.Unseen && Time.time >= lostUntil && Vector3.Distance(transform.position, session.Player.transform.position) < notice) Pounce();
                return;
            }
            if (agent != null) agent.speed = Rooted ? 0 : baseSpeed * (Slowed ? slowFactor : 1) * (Time.time < lungeUntil ? 2.2f : 1);
            if (!session.Player.IsAlive) { ResetFight(); return; }
            // The leash. A mob fighting alone is held by its own home. A linked group (EncounterEnemy.Social) is held by where its
            // pull began, by its target only: those who came from across the camp are not sent back for having come.
            // Chris 13:05, "leash is too short on fighting boards. they keep reseting even while fighting": a mob in a fight (struck or striking
            // within the last few seconds, ranged kiting included) never breaks off; only past the far leash and quiet for a while.
            if (Group == null && Vector3.Distance(transform.position, home) > (inFight ? FarLeash : session.Leash) && (!inFight || Quiet)) { if (inFight) GoHome(); else ResetFight(); return; }
            // Noticing you: within 5 m and, until a fight is on, only with a clear line to you, so a mob inside a rock cave or
            // behind a wall doesn't come for you through it. On its way home from a broken leash it notices nobody.
            if (!Evading && !EncounterSession.Unseen && Time.time >= lostUntil && Vector3.Distance(transform.position, session.Player.transform.position) < 5 && (Victim != null || Sees(session.Player.transform.position)))
                threat.AddProximity(session.Player.EntityId.Value, Time.deltaTime);
            var id = threat.Choose(Time.time, session.IsLivingCombatant);
            Victim = session.CombatActor(id);   // your party, or a sim that came for it (Phase 5.3a)
            if (Victim == null) { if (inFight) ClearFight(); Idle(); return; }
            strolling = false;
            if (Vector3.Distance(Victim.transform.position, Group != null ? Group.anchor : home) > FarLeash + 6 && Quiet) { Disengage(); return; }
            if (!inFight) Engage();   // the camp hears of it (EncounterSession.RaiseAlarm)
            if (Move != null && TickElite()) return;   // an elite drawing back for its heavy blow stands still and does not swing
            if (TickFlee()) return;   // bolting at low health (EncounterEnemy.Flee)
            if (Cast != null && TickCast()) return;   // a caster casting stands still and does not swing (EncounterEnemy.Casts)
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
            if (inReach && Time.time >= swing && MarkSwing())
            {
                if (Move != null && Time.time >= nextBlowAt) { BeginBlow(); return; }
                swing = Time.time + SwingInterval;
                if (Figure != null) Figure.Strike();
                int damage = session.Kit.ResolveEnemyHit(this, Victim, CappedBlow(Victim, Mathf.RoundToInt(HitBase * OverHitNow * GroupDamageScale * BlowScale)));
                session.FloatText(Victim.transform.position, "−" + damage, new Color(1, .45f, .35f));
                if (damage > 0) Grey(Victim);   // a Hollow Man's blow greys (EncounterEnemy.Grey)
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
            threat.Clear(); Victim = null; slowUntil = rootUntil = 0; unreachableSince = -1; TappedBy = null;
            UnscaleFromGroup(); ResetCasting(); ResetFlee();
            if (!actor.IsAlive) return;
            actor.Health.ApplyHealing(actor.Health.Pool.Max);
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(home); }
        }
        /// <summary>Seconds until this enemy may swing again (0 = ready as soon as it is in reach).</summary>
        public float NextSwingIn { get { return Mathf.Max(0, swing - Time.time); } }
        public void Stagger(float seconds) { if (seconds > 0 && actor.IsAlive) swing = Mathf.Max(swing, Time.time) + seconds; }
        /// <summary>No single blow takes more than three fifths of a party member's health (2026-10-11, the group run: Gang-Boss Haddo Lusk's
        /// Tally-Stick came to about 1,050 against people of 439, a one-shot every cycle; a fight should be lost over blows, not in one).</summary>
        public const float MaxBlowShare = .6f;
        /// <summary>Its blows scaled by what drives it (the Rock-Eater goaded by Nix: 1.5; EncounterSession.TickDuo).</summary>
        public float BlowScale = 1;
        public int CappedBlow(Actor victim, int raw)
        {
            if (victim == null || victim.Health == null || session.PartyActor(victim.EntityId.Value) == null) return raw;
            return Mathf.Min(raw, Mathf.Max(1, Mathf.RoundToInt(victim.Health.Pool.Max * MaxBlowShare)));
        }
        /// <summary>How far from home a mob in a fight may be drawn (40 m at least), and how long without a blow before it gives up.</summary>
        public float FarLeash { get { return Mathf.Max(40, session != null ? session.Leash : 30); } }
        public const float QuietSeconds = 5;
        float lastBlowAt = -100;
        /// <summary>No blow taken or struck for QuietSeconds.</summary>
        public bool Quiet { get { return Time.time - lastBlowAt > QuietSeconds; } }
        public void MarkBlow() { lastBlowAt = Time.time; }
        bool MarkSwing() { lastBlowAt = Time.time; return true; }
        public void Receive(int damage, Actor source)
        {
            if (damage > 0) lastBlowAt = Time.time;
            if (!actor.IsAlive || Inside != null) return;   // inside its machine, nothing reaches it
            SpellFx.Struck(transform);   // struck by an ability resolving: its effect shows here
            if (Hidden) Pounce();
            if (TappedBy == null && source != null && !Game) TappedBy = source.EntityId.Value;
            int actual = actor.GetComponent<Combatant>().Damage(Mathf.RoundToInt(damage * session.Kit.PartyDamageMultiplier(this) * OverTakenNow * session.DamageScaleFor(source)));
            if (!Game && source != null) session.Meter.Damage(source, actual);
            if (!Game) session.SpreadBlow(this, source, actual);   // an area build's blows splash onto foes near this one (EncounterSession.Spread)
            if (actor.IsAlive && Figure != null) Figure.Flinch();
            if (actor.IsAlive) ControlStruck();   // a hold breaks on damage (EncounterEnemy.Control)
            if (Game) { if (actor.IsAlive) GetComponent<GameAnimal>()?.Bolt(source != null ? source.transform.position : transform.position); }
            else { if (!inFight) struck = true; threat.Add(source.EntityId.Value, actual); if (inFight) ScaleToGroup(); }   // a sim joining mid-fight raises it   // struck: it was hit before it noticed anyone, so sneaking does not quieten its alarm
            session.FloatText(transform.position, actual.ToString(), new Color(1, .86f, .4f));
        }
        void OnDeath(Health health)
        {
            RaidMarked = RaidMark.None;   // the mark dies with it (Chris, 2026-10-11: "once the mob that was cc dies. take off the icon")
            ClearFight();   // before the death pose: a blow half drawn leaves the body leaning
            Victim = null;
            if (agent.isOnNavMesh) agent.isStopped = true;
            var visual = transform.Find("Body");
            if (Game) GetComponent<GameAnimal>()?.Fall();   // on its side on the ground, not tipped from an actor's height
            else if (Figure != null && Figure.Look == ActorLook.RockEater) { }   // a modelled beast: it falls in its own death where it stood
            else if (visual != null) { visual.localRotation = Quaternion.Euler(0, 0, 90); visual.localPosition = new Vector3(0, -.6f, 0); }
            respawnAt = Time.time + RespawnSeconds;
            DeathCall();   // a boss's relief patrol (EncounterEnemy.Boss)
            session.EnemyDied(this);
        }
        /// <summary>Camp mobs come back somewhere in their camp, whole, forgetting the fight.</summary>
        void Respawn()
        {
            respawnAt = float.MaxValue;
            var at = session.Zone != null ? session.Zone.StandAt(CampCenter + Random.insideUnitCircle * CampRadius * (Ambusher ? AmbushSpread(CampRadius) : 1), home.y) : home;   // a cave camp respawns on its floor
            for (int k = 0; k < 5 && session.Zone != null && !ClearOfCamp(at); k++) at = session.Zone.StandAt(CampCenter + Random.insideUnitCircle * Mathf.Max(CampRadius, Gap), home.y);   // not on top of another (note 131)
            if (Patrol != null && Patrol.Length > 0) { at = Patrol[0]; patrolAt = 0; patrolStep = 1; }   // a patrol comes back at the head of its road
            if (NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
            if (agent.isOnNavMesh) agent.Warp(at); else transform.position = at + Vector3.up;
            home = transform.position;
            var visual = transform.Find("Body");
            if (visual != null) { visual.localRotation = Quaternion.identity; visual.localPosition = Vector3.zero; }
            UnscaleFromGroup(); TappedBy = null; actor.Health.Revive(actor.Health.Pool.Max); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            ClearFight(); evadeUntil = 0;
            threat.Clear(); Victim = null; Looted = false; slowUntil = rootUntil = 0; ResetFlee();
            Drops = null; Coins = 0; LootBeacon.Clear(this);
            if (agent.isOnNavMesh) agent.isStopped = false;
            if (Game) GetComponent<GameAnimal>()?.Stand();
            if (Ambusher) Hide();
            if (Inside != null) { Inside = null; Shown(true); if (agent != null) agent.enabled = true; var look = GetComponent<ActorVisual>(); if (look != null) look.Riding = false; }   // it died in the saddle: down off its mount
            session.EnemyRespawned(this);   // a rider and its machine pair up again (EncounterSession.RockEater)
        }
        public void RestoreDead()
        {
            actor.Health.ApplyDamage(actor.Health.Pool.Max);
        }
    }
}
