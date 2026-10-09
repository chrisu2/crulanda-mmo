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
        /// <summary>Time and kills with you this grouping (5.5: SimMemory on leaving).</summary>
        public float joinedAt; public int killsTogether;
        NavMeshAgent agent; ActorVisual visual;
        float nextAct, nextHeal, nextCatchUp, downSince = -1, healAt; Actor healing;
        public string Activity { get; private set; } = "Following you";
        public enum PartyRole { Tank, Healer, Damage }
        /// <summary>Its part in the party (5.6): Warriors and Paladins tank (a Paladin heals when no Druid is along), Druids heal, Rangers and Mages deal damage.</summary>
        public PartyRole Role
        {
            get
            {
                if (sim.classId == "class.warrior") return PartyRole.Tank;
                if (sim.classId == "class.druid") return PartyRole.Healer;
                if (sim.classId == "class.paladin") return session.PartySims.Exists(c => c != null && c != this && c.sim.classId == "class.druid") || session.PartySims.Exists(c => c != null && c != this && c.sim.classId == "class.warrior") == false ? PartyRole.Tank : PartyRole.Healer;
                return PartyRole.Damage;
            }
        }
        /// <summary>Where it is leading the party (5.6, a run to a camp), or null.</summary>
        public Vector3? Leading { get; private set; }
        public string LeadingName { get; private set; }
        float nextTaunt;
        public bool Melee { get { return sim.classId == "class.warrior" || sim.classId == "class.paladin" || sim.classId == "class.rogue"; } }
        /// <summary>A Rogue or an Archivist (CC_DESIGN section 3): holds the moon in a fight of two or more.</summary>
        public bool Controller { get { return sim.classId == "class.rogue" || sim.classId == "class.archivist"; } }
        float nextHold, nextKick;
        /// <summary>Has an interrupt (Kick, Hush, Shield Bash, Censure, Quench): stops a cast in its reach every 12 s.</summary>
        public bool Interrupter { get { return sim.classId == "class.rogue" || sim.classId == "class.archivist" || sim.classId == "class.warrior" || sim.classId == "class.paladin" || sim.classId == "class.mage"; } }
        public bool Healer { get { return sim.classId == "class.druid" || sim.classId == "class.paladin"; } }
        public float Reach { get { return Melee ? 2.4f : 20; } }
        public float Interval { get { return Melee ? 2.0f : 2.4f; } }
        public int Hit { get { return Mathf.RoundToInt((Melee ? 6 : 5) + (Melee ? 2.4f : 2.2f) * sim.level); } }
        public int HealAmount { get { return Mathf.RoundToInt(12 + 5 * sim.level); } }
        /// <summary>Experience from the party's kills (EncounterSession.EnemyDied): a level when it has enough, said in Party.</summary>
        public void GainXp(int xp)
        {
            killsTogether++;
            if (xp <= 0 || sim.level >= EncounterProgress.LevelCap) return;
            sim.experience = Mathf.Max(sim.experience, EncounterProgress.XpForLevel(sim.level)) + xp;
            if (sim.experience >= EncounterProgress.XpForLevel(sim.level + 1))
            {
                sim.level++; actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, MaxHealthFor(sim)); actor.Health.ApplyHealing(actor.Health.Pool.Max);
                SimGear.Dress(GetComponent<ActorVisual>(), sim, session.Items); SimChatter.Active?.Ding(sim, true);
            }
        }
        /// <summary>Its choice in a roll (Round 25, note 72): Need for gear of its weight and slot that beats what it wears, Greed for
        /// anything it could sell, Pass for what is no use to it (a potion it cannot use goes to Greed too: coin is coin).</summary>
        public RollChoice RollFor(ItemDef d)
        {
            if (d == null) return RollChoice.Pass;
            if (d.kind == "gear")
            {
                bool mine = d.slot == "mainhand" || d.slot == "offhand" ? SimGear.CarriesWeapon(sim.classId) : SimGear.Suits(sim.classId, d.name);
                if (mine && d.level <= sim.level)
                {
                    var worn = session.Items?.Get(SimEconomy.Worn(sim, d.slot));
                    if (worn == null || d.quality > worn.quality || d.quality == worn.quality && d.level > worn.level) return RollChoice.Need;
                }
                return RollChoice.Greed;
            }
            return d.value > 0 ? RollChoice.Greed : RollChoice.Pass;
        }
        public static int MaxHealthFor(SimAdventurer s) { return (s.classId == "class.warrior" || s.classId == "class.paladin" ? 150 : 115) + 22 * Mathf.Max(1, s.level); }

        public void Init(Actor a, EncounterSession s, SimAdventurer data, int partySlot)
        {
            actor = a; session = s; sim = data; slot = partySlot; agent = GetComponent<NavMeshAgent>(); visual = GetComponent<ActorVisual>();
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, MaxHealthFor(sim)); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            nextAct = Time.time + .5f + slot * .3f; joinedAt = Time.time;
        }
        /// <summary>What this grouping was worth to it (5.5): a point a minute, one for every three kills.</summary>
        public void Remember(bool kicked)
        {
            int minutes = Mathf.FloorToInt((Time.time - joinedAt) / 60);
            if (minutes > 0) SimMemory.Note(sim, SimMemory.Deed.MinuteTogether, Mathf.Min(minutes, 30));
            if (killsTogether >= 3) SimMemory.Note(sim, SimMemory.Deed.KillsTogether, killsTogether / 3);
            if (kicked) SimMemory.Note(sim, SimMemory.Deed.Kicked);
        }
        /// <summary>The enemy it should be fighting: your target when it is in a fight, otherwise whatever is on one of the party.</summary>
        public EncounterEnemy Quarry
        {
            get
            {
                // A tank's first care (5.6): whatever is on someone who is not a tank.
                if (Role == PartyRole.Tank)
                {
                    EncounterEnemy peel = null; float peelD = 30;
                    foreach (var e in session.Enemies)
                    {
                        if (e == null || !e.actor.IsAlive || !e.FightingParty || e.LeaveAlone || e.Victim == null || e.Victim == actor) continue;
                        var vc = e.Victim.GetComponent<SimCompanion>(); if (vc != null && vc.Role == PartyRole.Tank) continue;
                        float d = Vector3.Distance(e.transform.position, transform.position); if (d < peelD) { peel = e; peelD = d; }
                    }
                    if (peel != null) return peel;
                }
                foreach (var e in session.Enemies)   // the skull first (CC step C3)
                    if (e != null && e.RaidMarked == RaidMark.Skull && e.actor.IsAlive && e.FightingParty && !e.Incapacitated && Vector3.Distance(e.transform.position, transform.position) < 30) return e;
                var t = session.Target;
                if (t != null && t.actor.IsAlive && !t.Game && !t.LeaveAlone && (t.FightingParty || session.AutoAttack)) return t;
                EncounterEnemy best = null; float bestD = 30;
                foreach (var e in session.Enemies)
                {
                    if (e == null || !e.actor.IsAlive || !e.FightingParty || e.LeaveAlone) continue;
                    float d = Vector3.Distance(e.transform.position, transform.position); if (d < bestD) { best = e; bestD = d; }
                }
                if (best == null && Leading.HasValue && waitingAt == null && Vector3.Distance(transform.position, Leading.Value) < 18)
                {
                    // At the camp it leads to: the nearest of its mobs still standing (not while it waits at a shut gate: the camp past it is not its to pull, D7).
                    foreach (var e in session.Enemies)
                    {
                        if (e == null || !e.actor.IsAlive || !e.Camp || e.Game) continue;
                        float d = Vector3.Distance(e.transform.position, Leading.Value); if (d < 16 && d < bestD) { best = e; bestD = d; }
                    }
                }
                return best;
            }
        }
        /// <summary>
        /// A sim Rogue or Archivist in a fight with two or more mobs on the party: the moon (marked by you, or else by it on a mob that
        /// isn't the skull or your target) is held 20 s, and it says so in Party. Every 25 s at most; never a boss.
        /// </summary>
        void HoldTheMoon()
        {
            nextHold = Time.time + 2;
            int fighting = 0; EncounterEnemy moon = null, pick = null; float far = 0;
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.actor.IsAlive || !e.FightingParty) continue;
                fighting++;
                if (e.RaidMarked == RaidMark.Moon) moon = e;
                else if (e.RaidMarked == RaidMark.None && e != session.Target && !e.Controlled && !(e.Elite && e.Move != null && e.Move.boss))
                { float d = Vector3.Distance(e.transform.position, transform.position); if (d > far) { far = d; pick = e; } }
            }
            if (fighting < 2) return;
            bool marked = false;
            if (moon == null && pick != null) { moon = pick; moon.RaidMarked = RaidMark.Moon; marked = true; }
            if (moon == null || moon.Incapacitated || Vector3.Distance(moon.transform.position, transform.position) > 25) return;
            if (moon.Apply("incap", actor, 20) != null) { nextHold = Time.time + 6; return; }
            nextHold = Time.time + 25;
            bool rogue = sim.classId == "class.rogue";
            string what = rogue ? (sim.chatty > .65f ? "gouged moon, dont touch it" : "Moon is held. Leave it be.") : (sim.chatty > .65f ? "lulling moon" : "Moon's asleep. Don't wake it.");
            session.ChatSay(ChatChannel.Party, sim.name, marked ? (sim.chatty > .65f ? "moon on " + moon.actor.DisplayName.ToLower() + ", " + what : "Moon on the " + moon.actor.DisplayName + ". " + what) : what);
        }
        /// <summary>Leads the party to a camp of about its level (5.6, "dungeon runs"): the nearest, or an elite when the party is three
        /// or more. Returns what it heads for, or null when there is nothing fit.</summary>
        public string Lead()
        {
            EncounterEnemy pick = null; float best = float.MaxValue; bool big = session.PartySims.Count >= 3;
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.actor.IsAlive || !e.Camp || e.Game || Mathf.Abs(e.actor.Level - sim.level) > (big ? 3 : 2) || (e.Elite && !big)) continue;
                float d = Vector3.Distance(e.transform.position, transform.position) - (big && e.Elite ? 60 : 0); if (d < 12 || d >= best) continue;
                best = d; pick = e;
            }
            if (pick == null) { Leading = null; return null; }
            Leading = pick.transform.position; LeadingName = pick.Elite ? pick.Name : SimChatter.Plural(pick.Name);
            session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "follow me, " + LeadingName.ToLower() + " this way" : "Follow me. The " + LeadingName.ToLower() + " are this way.");
            return LeadingName;
        }
        public void StopLeading() { Leading = null; LeadingName = null; route.Clear(); Dungeon = null; StoppedBy = null; waitingAt = null; }
        /// <summary>A dungeon run (round 27): the camps still to clear, in the order the passage meets them, each with the gate across the
        /// way before it (D7: shut, the run waits at it), and the dungeon's name.</summary>
        readonly System.Collections.Generic.List<(Vector3 at, string name, bool boss, Crulanda.World.ZoneGate gate)> route = new System.Collections.Generic.List<(Vector3, string, bool, Crulanda.World.ZoneGate)>();
        public string Dungeon { get; private set; }
        /// <summary>The shut gate ahead that the run will wait at (the Sealed Adit's cage-lift or platform gate), or null once it is open or there is none.</summary>
        public string StoppedBy { get; private set; }
        public int CampsLeft { get { return route.Count; } }
        Crulanda.World.ZoneGate waitingAt;
        /// <summary>Waiting at a shut gate for you to open it (D7).</summary>
        public bool WaitingAtGate { get { return waitingAt != null; } }
        /// <summary>A shut gate before the next camp: the run goes to it and waits, saying what it wants; open, the run goes on (D7).</summary>
        void TickGate()
        {
            if (Dungeon == null || route.Count == 0) return;
            var g = route[0].gate;
            if (g != null && !g.Open)
            {
                if (waitingAt == g) return;
                waitingAt = g; StoppedBy = g.Title; Leading = g.transform.position - g.transform.forward * 2.5f; LeadingName = g.Title.StartsWith("The ") ? g.Title.Substring(4) : g.Title;   // "Waiting at the cage-lift gate"
                var thing = session.Zone.Interactables.Find(i => i.gate == g); var prompt = thing != null ? session.InteractPromptFor(thing) : null;
                session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? g.Title.ToLower() + " is shut. " + (prompt ?? "open it").ToLower() + "?" : g.Title + " is shut against us. " + (prompt ?? "It wants opening") + ".");
            }
            else if (waitingAt != null)
            {
                waitingAt = null; StoppedBy = null; Leading = route[0].at; LeadingName = route[0].name;
                session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "open! on we go" : "It's open. On we go.");
            }
        }
        /// <summary>
        /// Leads the party through the zone's dungeon (a walk-in cave with camps in it): every camp whose ground is the cave's floor, in
        /// the order the passage reaches them from the mouth, the boss (an elite at the far end) last. Refuses (null, and why) when there
        /// is none, or when you are more than a level under its first camp (its camps are harder).
        /// </summary>
        public string LeadDungeon(out string why)
        {
            why = null; route.Clear(); Dungeon = null; StoppedBy = null;
            if (session.Zone == null) { why = "There is nowhere to go."; return null; }
            // The caves with their own mouths; a branch's camps come where the passage meets the branch (the Sealed Adit: the
            // Workings, then each way off the Gallery in the order you pass it, then the stair and the Rail Hall).
            foreach (var h in Crulanda.World.Hollow.All)
            {
                if (h.Parent != null) continue;
                var camps = new System.Collections.Generic.List<(float along, Vector3 at, string name, bool boss, int lo, Crulanda.World.ZoneGate gate)>();
                Gather(h, camps, true);
                if (camps.Count == 0) continue;
                int first = camps[0].lo; if (session.Progress.Level < first - 1) { why = h.Name + " is too much for us yet (level " + first + " at the door)."; return null; }
                foreach (var cp in camps) { route.Add((cp.at, cp.name, cp.boss, cp.gate)); if (StoppedBy == null && cp.gate != null && !cp.gate.Open) StoppedBy = cp.gate.Title; }   // the first shut gate: the run waits there (D7)
                Dungeon = session.Zone.Zone.dungeon ? session.Zone.Zone.displayName : h.Name; Leading = route[0].at; LeadingName = route[0].name;
                session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "ok, " + Dungeon + " run. " + route.Count + " camps to the bottom, stay close" : "Follow me into " + Dungeon + ". " + route.Count + " camps between us and the end. Stay close.");
                return Dungeon;
            }
            why = "There is no dungeon in these parts."; return null;
        }
        /// <summary>A cave's camps in the order the way through meets them, each branch's run spliced in where it opens.</summary>
        void Gather(Crulanda.World.Hollow h, System.Collections.Generic.List<(float along, Vector3 at, string name, bool boss, int lo, Crulanda.World.ZoneGate gate)> into, bool root)
        {
            var mine = new System.Collections.Generic.List<(float along, Vector3 at, string name, bool boss, int lo, Crulanda.World.ZoneGate gate)>();
            foreach (var cp in session.Zone.Zone.camps)
            {
                if (cp == null) continue;
                if (!string.IsNullOrEmpty(cp.cave))
                {
                    if (cp.cave != h.Name) continue;
                    float y = 0; if (!h.FloorSmooth(cp.center, out y)) h.FloorAt(cp.center, out y);
                    mine.Add((cp.along, new Vector3(cp.center.x, y, cp.center.y), cp.name, cp.elite, cp.levelMin, session.GateAhead(cp))); continue;   // past a gate: the run waits at it while it is shut (D7)
                }
                bool inOther = false; foreach (var o in Crulanda.World.Hollow.All) if (o != h && o.FloorAt(cp.center, out _)) { inOther = true; break; }
                if (!inOther && h.FloorAt(cp.center, out float fy)) { int i = h.Nearest(cp.center, out _); mine.Add((h.Along[i], new Vector3(cp.center.x, fy, cp.center.y), cp.name, cp.elite, cp.levelMin, null)); continue; }
                // A camp guarding the mouth from outside (the Hollow lookouts) is the run's first: before the passage, by its distance.
                if (!root || h.Centre.Count == 0) continue; var mouth = new Vector2(h.Centre[0].x, h.Centre[0].z); float out_ = Vector2.Distance(cp.center, mouth);
                if (out_ < 14) mine.Add((-out_, session.Zone.Ground(cp.center), cp.name, cp.elite, cp.levelMin, null));
            }
            mine.Sort((a, b) => a.along.CompareTo(b.along));
            var kids = Crulanda.World.Hollow.All.FindAll(o => o.Parent == h); kids.Sort((a, b) => a.ParentAlong.CompareTo(b.ParentAlong));
            int k = 0;
            foreach (var cp in mine)
            {
                while (k < kids.Count && kids[k].ParentAlong < cp.along) Gather(kids[k++], into, false);
                into.Add(cp);
            }
            while (k < kids.Count) Gather(kids[k++], into, false);
        }
        /// <summary>No mob of the camp left near where it led to: the run is done.</summary>
        bool CampCleared()
        {
            if (!Leading.HasValue) return false;
            foreach (var e in session.Enemies) if (e != null && e.actor.IsAlive && e.Camp && !e.Game && Vector3.Distance(e.transform.position, Leading.Value) < 16) return false;
            return true;
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
            // Mending first, for the healers: the most hurt of the party under 60%. A cast of a second (the spell pose held, playtest
            // note 68), then the release, the heal and its glow.
            if (healing != null)
            {
                if (Time.time < healAt) { Activity = "Mending " + (healing == player ? "you" : healing.DisplayName); return; }
                var who = healing; healing = null; if (visual != null) visual.Casting = false;
                if (who.IsAlive && Vector3.Distance(who.transform.position, transform.position) <= 28)
                {
                    int healed = who.GetComponent<Combatant>().Heal(HealAmount);
                    session.FloatText(who.transform.position, "+" + healed, new Color(.3f, 1, .7f)); session.HealThreat(actor, healed);
                    visual?.CastRelease(); if (healed > 0) HealFx.Show(who.transform);
                }
            }
            else if (Healer && session.InCombat && Time.time >= nextHeal)
            {
                var hurt = MostHurt();
                if (hurt != null && hurt.Health.Pool.Ratio < .6f && Vector3.Distance(hurt.transform.position, transform.position) <= 25)
                {
                    nextHeal = Time.time + 6; nextAct = Mathf.Max(nextAct, Time.time + 2.2f);
                    healing = hurt; healAt = Time.time + 1; if (visual != null) visual.Casting = true;
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    Activity = "Mending " + (hurt == player ? "you" : hurt.DisplayName);
                    return;
                }
            }
            TickGate();   // a shut gate ahead: the run waits at it and goes on when it opens (D7)
            var quarry = Quarry;
            if (Leading.HasValue && quarry == null && waitingAt == null && CampCleared())
            {
                if (route.Count > 1)
                {
                    // A dungeon run (round 27): on to the next camp down the passage.
                    route.RemoveAt(0); Leading = route[0].at; LeadingName = route[0].name;
                    session.ChatSay(ChatChannel.Party, sim.name, route[0].boss ? (sim.chatty > .65f ? "boss next. " + LeadingName + ". buff up" : "That's the last before " + LeadingName + ". Ready yourselves.") : (sim.chatty > .65f ? "clear. next: " + LeadingName.ToLower() : "Clear. On to " + LeadingName + "."));
                }
                else if (Dungeon != null)
                {
                    session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? Dungeon + " done!! gg all" : Dungeon + " is cleared. That was well done, all of you.");
                    session.Message(sim.name + ": " + Dungeon + " cleared."); foreach (var p in session.PartySims) if (p != null) SimMemory.Note(p.sim, SimMemory.Deed.KillsTogether, 3);
                    StopLeading();
                }
                else
                {
                    session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "camp cleared, gg. where next?" : "That's the camp cleared. Well fought. Where next?");
                    session.Message(sim.name + " calls the run done: " + LeadingName.ToLower() + " cleared.");
                    StopLeading();
                }
            }
            if (Controller && Time.time >= nextHold) HoldTheMoon();
            if (Interrupter && Time.time >= nextKick)
                foreach (var e in session.Enemies)
                    if (e != null && e.actor.IsAlive && e.Casting && e.FightingParty && e.CastProgress > .25f && Vector3.Distance(e.transform.position, transform.position) <= Reach + 1.5f)
                    {
                        if (e.Apply("silence", actor, 3) == null)
                        {
                            nextKick = Time.time + 12;
                            if (sim.chatty > .5f) session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .75f ? "kicked" : "Interrupted the " + e.actor.DisplayName + ".");
                        }
                        break;
                    }
            if (quarry != null)
            {
                // A tank taunts what is on someone else (5.6): the mob turns to it for a few seconds.
                if (Role == PartyRole.Tank && quarry.Victim != null && quarry.Victim != actor && Time.time >= nextTaunt && Vector3.Distance(transform.position, quarry.transform.position) < 12)
                { nextTaunt = Time.time + 8; quarry.threat.Taunt(actor.EntityId.Value, Time.time, 4); session.FloatText(transform.position + Vector3.up * 2, "Taunt", new Color(1, .7f, .3f)); }
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
                        var colour = sim.classId == "class.mage" ? new Color(1, .55f, .15f) : sim.classId == "class.druid" ? new Color(.45f, .85f, .3f) : sim.classId == "class.archivist" ? new Color(.6f, .8f, 1) : new Color(.9f, .88f, .8f);
                        Bolt.Fire(from, quarry.transform, colour, arrow ? .1f : .18f, arrow ? .22f : .28f, arrow);
                    }
                    quarry.Receive(dmg, actor);
                    if (Role == PartyRole.Tank) quarry.threat.Add(actor.EntityId.Value, dmg * 1.5f);   // a tank's blows hold attention
                }
                return;
            }
            // The Weaver's escort (D7): while Mother Quillet walks to the lock, the party keeps to her, not to you.
            var weaver = session.Weaver;
            if (weaver != null && weaver.actor.IsAlive && (weaver.Now == AditWeaver.Stage.Walking || weaver.Now == AditWeaver.Stage.Matching) && Vector3.Distance(transform.position, weaver.transform.position) < 45)
            {
                Activity = "Guarding " + AditWeaver.Name;
                var post = weaver.transform.position + Quaternion.Euler(0, 90 + slot * 120, 0) * Vector3.forward * 2.2f;
                float toHer = Vector3.Distance(transform.position, weaver.transform.position);
                agent.isStopped = toHer < 3.2f;
                if (!agent.isStopped) { agent.speed = toHer > 8 ? 5.6f : 3.4f; agent.SetDestination(post); }
                return;
            }
            // Leading a run (5.6): on to the camp while you keep up; waits when you fall behind. At a shut gate it waits for you to open it (D7).
            if (Leading.HasValue)
            {
                float toCamp = Vector3.Distance(transform.position, Leading.Value);
                if (toPlayer > 18) { Activity = "Waiting for you"; agent.isStopped = true; return; }
                if (toCamp > 6) { Activity = (waitingAt != null ? "Going to the " : "Leading you to the ") + LeadingName.ToLower(); agent.isStopped = false; agent.speed = 4.2f; agent.SetDestination(Leading.Value); return; }
                Activity = (waitingAt != null ? "Waiting at the " : "At the ") + LeadingName.ToLower(); agent.isStopped = true; return;
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
            if (session.Weaver != null) Consider(session.Weaver.actor);   // Mother Quillet (D4)
            return best;
        }
    }
}
