using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A sim in the world (Phase 5.2; its life 5.3a-c, 2026-10-06): its class's look and gear, an Actor the mobs can fight, and a
    /// day of its own. When one thing is done it chooses the next by its personality and its state (<see cref="Choose"/>): hunting
    /// the camp mobs of its level (bold ones more), working the nodes of its trade (cautious ones more), selling at the stall and
    /// forging or brewing at the station when it has the makings (5.3c, SimEconomy), resting at the inn (when hurt, or now and
    /// then), taking the road to another zone when it has outgrown this one or feels like it (5.3b), or standing about the named
    /// places (chatty ones more). It fights in its class's way, falls when beaten, comes to at the zone's recovery point and runs
    /// back for its corpse (5.3b), and levels up from its kills (dinging in the chat). A mob it hits first is its kill.
    /// </summary>
    public sealed class SimFigure : MonoBehaviour
    {
        public enum Doing { Loiter, Hunt, Gather, Inn, Down, CorpseRun, Travel, Trade, Craft }
        public SimAdventurer sim; public SimPopulation population; public ActorVisual visual; public Actor actor; NavMeshAgent agent;
        public Doing Activity { get; private set; } = Doing.Loiter;
        /// <summary>Inside the inn: not drawn, not targetable, no plate.</summary>
        public bool Hidden { get; private set; }
        public EncounterEnemy Quarry { get; private set; }
        public ZoneInteractable Node { get; private set; }
        public ZoneExit Exit { get; private set; }
        public RecipeDef Recipe { get; private set; }
        public Vector3 CorpseAt { get; private set; }
        public int Kills { get; private set; }
        float until, nextAct, nextHeal, stuckSince, workUntil, downSince, lastTrade, healAt; int pick, goal; Renderer[] renderers; Transform corpse;
        EncounterSession Session { get { return population.Session; } }
        public bool Walking { get { return agent != null && agent.isOnNavMesh && !agent.isStopped && agent.remainingDistance > agent.stoppingDistance + .1f; } }
        public bool Melee { get { return sim.classId == "class.warrior" || sim.classId == "class.paladin" || sim.classId == "class.rogue"; } }
        public bool Healer { get { return sim.classId == "class.druid" || sim.classId == "class.paladin"; } }
        public string Doings
        {
            get
            {
                switch (Activity)
                {
                    case Doing.Hunt: return Quarry != null ? "fighting " + Quarry.actor.DisplayName : "hunting";
                    case Doing.Gather: return "gathering"; case Doing.Inn: return "at the inn"; case Doing.Down: return "fallen"; case Doing.CorpseRun: return "running back";
                    case Doing.Travel: return Exit != null ? "off to " + Session.ZoneName(Exit.to) : "travelling"; case Doing.Trade: return "trading"; case Doing.Craft: return Recipe != null && Recipe.profession == "alchemy" ? "brewing" : "at the forge";
                    default: return "about";
                }
            }
        }

        public static SimFigure Spawn(SimPopulation pop, SimAdventurer s)
        {
            var go = new GameObject("Sim " + s.name); go.SetActive(false); go.transform.SetParent(pop.transform, false);
            var start = s.x != 0 || s.z != 0 ? new Vector3(s.x, 0, s.z) : pop.Spot(s, 0);
            if (s.x != 0 || s.z != 0) { start.y = pop.Session.Zone != null ? pop.Session.Zone.HeightAt(start.x, start.z) : 0; if (NavMesh.SamplePosition(start, out var hit, 6, NavMesh.AllAreas)) start = hit.position; }
            go.transform.position = start + Vector3.up;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Body"; Destroy(body.GetComponent<Collider>()); body.transform.SetParent(go.transform, false);
            var f = go.AddComponent<SimFigure>(); f.sim = s; f.population = pop;
            // A fighter: an Actor of its level with its sim id (the mobs' threat names it), health as a party sim's (SimCompanion).
            f.actor = go.AddComponent<Actor>(); f.actor.Initialize(pop.Session.content.player, s.name, s.level, new Crulanda.Core.EntityId(s.id)); go.AddComponent<Combatant>();
            f.agent = go.AddComponent<NavMeshAgent>(); f.agent.speed = 1.7f; f.agent.angularSpeed = 360; f.agent.acceleration = 8; f.agent.stoppingDistance = .5f; f.agent.radius = .3f; f.agent.height = 2; f.agent.baseOffset = 1;
            f.agent.avoidancePriority = 80;   // villagers (60) go first: a sim steps aside
            f.visual = ActorVisual.Attach(go, EncounterSession.LookForClass(s.classId), s.variant);
            go.SetActive(true);   // ActorVisual hides the placeholder capsule itself; the figure is built under "Body", so nothing else is touched here
            f.Refit();
            SimGear.Dress(f.visual, s, pop.Session.Items);   // its own gear by its level and class (Phase 5.2 round 2)
            f.renderers = go.GetComponentsInChildren<Renderer>();
            f.until = Time.time + 2 + (s.variant % 7); f.lastTrade = Time.time;
            return f;
        }
        /// <summary>Its health for its level (whole when it grows).</summary>
        public void Refit() { actor.Stats.SetBase(StatType.MaxHealth, SimCompanion.MaxHealthFor(sim)); if (actor.IsAlive) actor.Health.ApplyHealing(actor.Health.Pool.Max); }

        // ---------- choosing ----------
        /// <summary>A camp mob of its level (two under to one over), alive, not an elite, not already fought by someone else, nearest first.</summary>
        public EncounterEnemy FindPrey(float within = 160)
        {
            EncounterEnemy best = null; float bestD = within;
            foreach (var e in Session.Enemies)
            {
                if (e == null || !e.Camp || e.Game || e.Elite || !e.actor.IsAlive || e.Hidden || e.Engaged) continue;   // no elite alone (Old Hazelmaw drew a level-5 Ranger)
                int lv = e.actor.Level; if (lv < sim.level - 2 || lv > sim.level + 1) continue;
                float d = Vector3.Distance(e.transform.position, transform.position); if (d < bestD) { best = e; bestD = d; }
            }
            return best;
        }
        /// <summary>A node of its trade, ready, not being worked by another sim, nearest first.</summary>
        public ZoneInteractable FindNode(float within = 140)
        {
            var zb = Session.Zone; if (zb == null || Session.Professions == null) return null;
            ZoneInteractable best = null; float bestD = within;
            foreach (var i in zb.Interactables)
            {
                if (i == null || i.node == null || Time.time < i.hiddenUntil || population.Figures.Exists(o => o != null && o != this && o.Node == i)) continue;
                if (!SimEconomy.Gathers(sim, Session.Professions.Db.Node(i.node))) continue;
                float d = Vector3.Distance(i.position, transform.position); if (d < bestD) { best = i; bestD = d; }
            }
            return best;
        }
        ZoneDoor Inn { get { var zb = Session.Zone; return zb == null ? null : zb.Doors.Find(d => d != null && d.kind == "inn"); } }
        Vector3? Place(string kind) { var life = VillageLife.Active; if (life == null || !life.Places.TryGetValue(kind, out var list) || list.Count == 0) return null; return list[Mathf.Abs(sim.variant) % list.Count]; }
        /// <summary>The road out it would take: to a zone whose levels suit it (its own home best when it has outgrown this one).</summary>
        public ZoneExit FindExit()
        {
            var zb = Session.Zone; if (zb == null || zb.Zone.exits == null) return null;
            ZoneExit best = null; int bestScore = int.MinValue;
            foreach (var e in zb.Zone.exits)
            {
                if (e == null || string.IsNullOrEmpty(e.to) || SimPopulation.Dungeon(zb, e.to)) continue;   // never into a dungeon on its own
                int score = SimPopulation.Suits(e.to, sim.level) + (e.to == sim.homeZone ? 2 : 0) + Mathf.Abs((sim.variant + e.to.Length) % 3);
                if (score > bestScore) { best = e; bestScore = score; }
            }
            return best;
        }
        /// <summary>What next, by score: its personality, its health, what is near, what it carries, and a little chance.</summary>
        public void Choose()
        {
            Quarry = null; Node = null; Exit = null; Recipe = null; float health = actor.Health.Pool.Ratio;
            if (!population.Lively) { Begin(Doing.Loiter); return; }
            float Noise() { return Random.value * .3f; }
            var prey = health > .6f ? FindPrey() : null; var node = FindNode(); var inn = Inn; var exit = FindExit();
            int goods = SimEconomy.GoodsCount(sim); var recipe = SimEconomy.Craftable(sim, Session.Professions?.Db, Session.Items);
            float hunt = prey != null ? .35f + .6f * sim.bold + Noise() : -1;
            float gather = node != null ? .3f + .5f * (1 - sim.bold) + Noise() : -1;
            float rest = inn != null ? (health < .6f ? 1.2f : .12f + .2f * sim.chatty) + Noise() : -1;
            float trade = (goods >= 6 || SimEconomy.CanUpgrade(sim)) && Place("stall") != null ? .5f + goods * .04f + Noise() : -1;
            float craft = recipe != null && Place(recipe.profession == "alchemy" ? "bench" : "forge") != null ? .75f + Noise() : -1;
            int fit = SimPopulation.Suits(Session.ZoneId, sim.level);
            float travel = exit != null && Time.time - lastTrade > 60 ? (fit < 0 ? .9f : .05f + .1f * sim.bold) + Noise() * .5f : -1;
            float loiter = .25f + .45f * sim.chatty + Noise();
            float top = Mathf.Max(Mathf.Max(Mathf.Max(hunt, gather), Mathf.Max(rest, loiter)), Mathf.Max(Mathf.Max(trade, craft), travel));
            if (top == hunt) Begin(Doing.Hunt, prey: prey);
            else if (top == gather) Begin(Doing.Gather, node: node);
            else if (top == craft) Begin(Doing.Craft, recipe: recipe);
            else if (top == trade) Begin(Doing.Trade);
            else if (top == travel) Begin(Doing.Travel, exit: exit);
            else if (top == rest) Begin(Doing.Inn);
            else Begin(Doing.Loiter);
        }
        /// <summary>Starts an activity (tests and the choice).</summary>
        public void Begin(Doing what, EncounterEnemy prey = null, ZoneInteractable node = null, ZoneExit exit = null, RecipeDef recipe = null)
        {
            Activity = what; Quarry = prey; Node = node; Exit = exit; Recipe = recipe; stuckSince = Time.time; goal = 0; workUntil = 0;
            switch (what)
            {
                case Doing.Hunt: goal = 2 + Mathf.Abs(sim.gearSeed) % 3; until = Time.time + 240; if (prey == null) Quarry = FindPrey(); if (Quarry == null) { Activity = Doing.Loiter; until = Time.time + 20; } break;
                case Doing.Gather: goal = 1 + Mathf.Abs(sim.gearSeed / 7) % 3; until = Time.time + 200; if (node == null) Node = FindNode(); if (Node == null) { Activity = Doing.Loiter; until = Time.time + 20; } else Go(Node.position); break;
                case Doing.Inn: { var inn = Inn; until = Time.time + 150; if (inn == null) { Activity = Doing.Loiter; until = Time.time + 20; } else Go(inn.position); break; }
                case Doing.Trade: { var at = Place("stall"); until = Time.time + 120; if (at == null) { Activity = Doing.Loiter; until = Time.time + 20; } else Go(at.Value); break; }
                case Doing.Craft: { if (Recipe == null) Recipe = SimEconomy.Craftable(sim, Session.Professions?.Db, Session.Items); var at = Recipe != null ? Place(Recipe.profession == "alchemy" ? "bench" : "forge") : null; until = Time.time + 120; if (at == null) { Activity = Doing.Loiter; until = Time.time + 20; } else Go(at.Value); break; }
                case Doing.Travel: { if (Exit == null) Exit = FindExit(); until = Time.time + 300; if (Exit == null) { Activity = Doing.Loiter; until = Time.time + 20; } else { Go(Session.Zone.Ground(Exit.at)); SimChatter.Active?.Leaving(sim, Exit); } break; }
                case Doing.CorpseRun: until = Time.time + 180; Go(CorpseAt); break;
                default: until = Time.time + 30 + (sim.variant % 5) * 6 + sim.chatty * 30; pick++; Go(population.Spot(sim, pick)); break;
            }
        }
        void Go(Vector3 to)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            if (NavMesh.SamplePosition(to, out var hit, 4, NavMesh.AllAreas)) to = hit.position;
            agent.isStopped = false; agent.speed = 1.9f; agent.SetDestination(to); stuckSince = Time.time;
        }
        bool Near(Vector3 p, float d) { return Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(p.x, 0, p.z)) < d; }
        void Face(Vector3 p) { var f = p - transform.position; f.y = 0; if (f.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(f); }

        // ---------- each frame ----------
        void Update()
        {
            if (population == null || Session == null || Session.Paused || agent == null) return;
            if (!actor.IsAlive) { Fallen(); return; }
            if (Activity == Doing.Down) { Activity = Doing.Loiter; until = Time.time; }
            switch (Activity)
            {
                case Doing.Hunt: Hunt(); break;
                case Doing.Gather: Gather(); break;
                case Doing.Inn: AtInn(); break;
                case Doing.Trade: Trade(); break;
                case Doing.Craft: Craft(); break;
                case Doing.Travel: Travel(); break;
                case Doing.CorpseRun: CorpseRun(); break;
                default: Loiter(); break;
            }
            if (!Session.InCombat && !InFight && Time.time >= nextHeal) { nextHeal = Time.time + 1; actor.Health.ApplyHealing(Mathf.Max(2, actor.Health.Pool.Max / 60)); }
        }
        public bool InFight { get { return Quarry != null && Quarry.actor.IsAlive && Quarry.Victim == actor; } }
        void Loiter()
        {
            if (Walking && Time.time - stuckSince > 25) agent.ResetPath();
            if (sim.friendly > .5f && !Walking)
            {
                var player = Session.Player;
                if (player != null)
                {
                    var to = player.transform.position - transform.position; to.y = 0;
                    if (to.sqrMagnitude < 9 && to.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 3);
                }
            }
            if (Time.time >= until) Choose();
        }
        void Hunt()
        {
            var q = Quarry;
            if (q == null || !q.actor.IsAlive)
            {
                if (q != null && !q.actor.IsAlive && q.TappedBy == sim.id) { Kills++; GainXp(EncounterProgress.KillXp(q.actor.Level, sim.level, q.Elite)); }
                if (q != null) goal--;
                Quarry = null;
                // Another, while it has the health and the will; then a rest.
                if (goal > 0 && actor.Health.Pool.Ratio > .45f && Time.time < until) Quarry = FindPrey(60);
                if (Quarry == null) { if (actor.Health.Pool.Ratio < .6f) Begin(Doing.Inn); else Choose(); }
                return;
            }
            // Someone else has it (you, or another sim): leave it to them.
            if (q.Engaged && q.Victim != actor && !(q.TappedBy == sim.id)) { Quarry = null; return; }
            if (actor.Health.Pool.Ratio < .3f && !Healer) { Quarry = null; goal = 0; return; }   // breaks off: the mob may follow (its leash)
            float reach = Melee ? 2.4f : 18, d = Vector3.Distance(transform.position, q.transform.position);
            // A healer mending itself: the spell pose held a second (playtest note 68), then the release, the heal and its glow.
            if (healAt > 0)
            {
                if (Time.time < healAt) return;
                healAt = 0; if (visual != null) visual.Casting = false;
                int healed = actor.GetComponent<Combatant>().Heal(Mathf.RoundToInt(12 + 5 * sim.level)); visual?.CastRelease();
                if (healed > 0) { Session.FloatText(transform.position, "+" + healed, new Color(.3f, 1, .7f)); HealFx.Show(transform); }
                return;
            }
            if (Healer && actor.Health.Pool.Ratio < .5f && Time.time >= nextHeal)
            {
                nextHeal = Time.time + 8; nextAct = Mathf.Max(nextAct, Time.time + 2.2f); healAt = Time.time + 1; if (visual != null) visual.Casting = true;
                agent.isStopped = true; return;
            }
            if (d > reach) { if (Time.time - stuckSince > .5f || !Walking) { Go(q.transform.position); agent.speed = Melee ? 4.6f : 3.2f; stuckSince = Time.time; } return; }
            agent.isStopped = true;
            var face = q.transform.position - transform.position; face.y = 0;
            if (face.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 8);
            if (Time.time < nextAct) return;
            nextAct = Time.time + (Melee ? 2.0f : 2.4f);
            int dmg = Mathf.RoundToInt((Melee ? 6 : 5) + (Melee ? 2.4f : 2.2f) * sim.level);
            if (Melee) visual?.Strike();
            else
            {
                visual?.CastRelease();
                var hand = visual != null ? visual.RightHandle : null; var from = hand != null ? hand.position : transform.position + Vector3.up * 1.3f;
                bool arrow = sim.classId == "class.ranger";
                var colour = sim.classId == "class.mage" ? new Color(1, .55f, .15f) : sim.classId == "class.druid" ? new Color(.45f, .85f, .3f) : sim.classId == "class.archivist" ? new Color(.6f, .8f, 1) : new Color(.9f, .88f, .8f);
                Bolt.Fire(from, q.transform, colour, arrow ? .1f : .18f, arrow ? .22f : .28f, arrow);
            }
            q.Receive(dmg, actor);
            if (sim.classId == "class.warrior") q.threat.Add(actor.EntityId.Value, dmg * 1.5f);
        }
        /// <summary>Experience from a kill (its own, or the party's): a level when it has enough, said in the chat.</summary>
        public void GainXp(int xp)
        {
            if (xp <= 0 || sim.level >= EncounterProgress.LevelCap) return;
            sim.experience = Mathf.Max(sim.experience, EncounterProgress.XpForLevel(sim.level)) + xp;
            if (sim.experience >= EncounterProgress.XpForLevel(sim.level + 1))
            {
                sim.level++; Refit(); SimGear.Dress(visual, sim, Session.Items);
                SimChatter.Active?.Ding(sim);
            }
        }
        void Gather()
        {
            var n = Node;
            if (n == null || Time.time >= until) { Choose(); return; }
            if (workUntil > 0)
            {
                if (Time.time < workUntil) return;
                workUntil = 0; visual.Pose = ActorPose.None;
                var def = Session.SimGathered(n); if (def != null) SimEconomy.Add(sim, def.item, 1 + Mathf.Abs(sim.gearSeed + Kills) % 2);
                goal--; Node = null;
                if (goal > 0) { Node = FindNode(80); if (Node != null) { Go(Node.position); return; } }
                Choose(); return;
            }
            if (Time.time < n.hiddenUntil) { Node = FindNode(80); if (Node != null) Go(Node.position); else Choose(); return; }   // someone took it first
            if (!Near(n.position, 1.8f)) { if (!Walking && Time.time - stuckSince > 1) Go(n.position); if (Time.time - stuckSince > 40) Choose(); return; }
            agent.isStopped = true; Face(n.position);
            visual.Pose = ActorPose.Gather; workUntil = Time.time + 4 + (sim.variant % 3);
        }
        void Trade()
        {
            var at = Place("stall"); if (at == null || Time.time >= until) { Choose(); return; }
            if (!Near(at.Value, 2.2f)) { if (!Walking && Time.time - stuckSince > 1) Go(at.Value); return; }
            agent.isStopped = true; Face(at.Value); lastTrade = Time.time;
            int coin = SimEconomy.SellAll(sim, Session.Items);
            if (coin > 0) SimChatter.Active?.Sold(sim, coin);
            if (SimEconomy.Upgrade(sim)) { SimGear.Dress(visual, sim, Session.Items); SimChatter.Active?.Upgraded(sim); }
            Choose();
        }
        void Craft()
        {
            var r = Recipe; if (r == null || Time.time >= until) { Choose(); return; }
            var at = Place(r.profession == "alchemy" ? "bench" : "forge"); if (at == null) { Choose(); return; }
            if (workUntil > 0)
            {
                if (Time.time < workUntil) return;
                workUntil = 0; visual.Pose = ActorPose.None;
                string made = SimEconomy.Craft(sim, r, Session.Items); var d = Session.Items?.Get(made);
                if (d != null && d.kind == "gear") { SimGear.Dress(visual, sim, Session.Items); SimChatter.Active?.Crafted(sim, d.name, true); }
                else if (made != null && !made.StartsWith("mat.")) SimChatter.Active?.Crafted(sim, Session.ItemName(made), false);
                Recipe = SimEconomy.Craftable(sim, Session.Professions?.Db, Session.Items);   // the next, while the makings last
                if (Recipe == null) Choose();
                return;
            }
            if (!Near(at.Value, 2.2f)) { if (!Walking && Time.time - stuckSince > 1) Go(at.Value); return; }
            agent.isStopped = true; Face(at.Value);
            visual.Pose = r.profession == "alchemy" ? ActorPose.Work : ActorPose.Hammer; workUntil = Time.time + 5;
        }
        void Travel()
        {
            var e = Exit; if (e == null || Time.time >= until) { Choose(); return; }
            var at = Session.Zone.Ground(e.at);
            if (!Near(at, Mathf.Max(3, e.radius)))
            {
                if (!Walking && Time.time - stuckSince > 1) Go(at);
                if (Time.time - stuckSince > 90 && !Walking) { Choose(); }
                return;
            }
            // Over the zone line: in the other zone now, at its arrival point; the figure goes (SimPopulation.Refresh).
            population.Depart(this, e);
        }
        void AtInn()
        {
            var inn = Inn; if (inn == null) { Choose(); return; }
            if (Hidden)
            {
                actor.Health.ApplyHealing(Mathf.Max(3, actor.Health.Pool.Max / 30));
                if (Time.time >= until) { Show(true); Choose(); }
                return;
            }
            // In at the door; or close by and no longer getting anywhere (playtest note 98, "people get stuck in inn door": a crowd at the
            // doorway, or the door's own frame, held them on the step).
            bool jammed = Near(inn.position, 5f) && agent.velocity.sqrMagnitude < .04f && Time.time - stuckSince > 2;
            if (Near(inn.position, 2.6f) || jammed) { Show(false); until = Time.time + 60 + (sim.variant % 5) * 15; return; }
            if (!Walking && Time.time - stuckSince > 1) Go(inn.position);
            if (Time.time - stuckSince > 60) Choose();
        }
        /// <summary>Goes in (hidden: no body, no plate, no NavMesh) or comes out at the door.</summary>
        void Show(bool on)
        {
            Hidden = !on;
            foreach (var r in renderers) if (r != null && r.name != "Body") r.enabled = on;
            if (agent != null) agent.enabled = on;
        }
        /// <summary>Beaten: down where it fell; after a while it comes to at the zone's recovery point and runs back for its corpse (5.3b).</summary>
        void Fallen()
        {
            if (Activity != Doing.Down) { Activity = Doing.Down; downSince = Time.time; Quarry = null; if (agent.isOnNavMesh) agent.isStopped = true; visual.Pose = ActorPose.None; }
            if (Time.time - downSince < 10) return;
            CorpseAt = transform.position; MarkCorpse();
            var back = Session.RecoveryPoint; if (NavMesh.SamplePosition(back, out var hit, 6, NavMesh.AllAreas)) back = hit.position;
            if (agent.isOnNavMesh) agent.Warp(back); else transform.position = back + Vector3.up;
            actor.Health.Revive(Mathf.RoundToInt(actor.Health.Pool.Max * .35f));
            SimChatter.Active?.Died(sim, CorpseAt);
            Begin(Doing.CorpseRun);
        }
        void CorpseRun()
        {
            if (Time.time >= until) { ClearCorpse(); Begin(Doing.Inn); return; }
            if (!Near(CorpseAt, 2.5f)) { if (!Walking && Time.time - stuckSince > 1) Go(CorpseAt); return; }
            ClearCorpse(); actor.Health.ApplyHealing(Mathf.RoundToInt(actor.Health.Pool.Max * .35f));
            SimChatter.Active?.Recovered(sim);
            Choose();
        }
        /// <summary>A sim's corpse: a dark mound on the ground where it fell, named for it, until it is recovered.</summary>
        void MarkCorpse()
        {
            ClearCorpse();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Corpse of " + sim.name; Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(population.transform, false); go.transform.position = CorpseAt - Vector3.up * .8f; go.transform.localScale = new Vector3(1.1f, .35f, 1.7f);
            go.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(.24f, .2f, .17f) }; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            corpse = go.transform;
        }
        void ClearCorpse() { if (corpse != null) { corpse.gameObject.SetActive(false); Destroy(corpse.gameObject); corpse = null; } }   // gone at once, not at the frame's end
        void OnDisable() { if (visual != null && (Activity == Doing.Gather || Activity == Doing.Craft)) visual.Pose = ActorPose.None; }
        void OnDestroy() { ClearCorpse(); }
    }
}
