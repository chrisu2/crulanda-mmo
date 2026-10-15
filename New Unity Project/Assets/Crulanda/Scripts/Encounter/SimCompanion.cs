using System.Collections.Generic;
using System.Linq;
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
        float nextAct, nextHeal, nextRegen, nextCatchUp, downSince = -1, healAt; Actor healing;
        public string Activity { get; private set; } = "Following you";
        public enum PartyRole { Tank, Healer, Damage }
        /// <summary>Its part in the party (5.6): Warriors and Paladins tank (a Paladin heals when no Druid is along), Druids heal, Rangers and Mages deal damage.</summary>
        public PartyRole Role
        {
            get
            {
                if (sim.classId == "class.warrior") return PartyRole.Tank;
                if (sim.classId == "class.druid") return PartyRole.Healer;
                // A Salt-Mender heals when no Druid is along (the Mender path, 2026-10-12), else throws salt with the others.
                if (sim.classId == "class.cleric" || sim.classId == "class.shaman") return PartyRole.Healer;   // note 178: the new healers (classes 9-10)
                if (sim.classId == "class.saltmender") return session.PartySims.Exists(c => c != null && c != this && c.sim.classId == "class.druid") ? PartyRole.Damage : PartyRole.Healer;
                if (sim.classId == "class.paladin") return session.PartySims.Exists(c => c != null && c != this && c.sim.classId == "class.druid" || c.sim.classId == "class.saltmender") || session.PartySims.Exists(c => c != null && c != this && c.sim.classId == "class.warrior") == false ? PartyRole.Tank : PartyRole.Healer;
                return PartyRole.Damage;
            }
        }
        /// <summary>Where it is leading the party (5.6, a run to a camp), or null.</summary>
        public Vector3? Leading { get; private set; }
        public string LeadingName { get; private set; }
        float nextTaunt;
        public bool Melee { get { return sim.classId == "class.warrior" || sim.classId == "class.paladin" || sim.classId == "class.rogue" || sim.classId == "class.scoundrel"; } }
        /// <summary>A Rogue or an Archivist (CC_DESIGN section 3): holds the moon in a fight of two or more.</summary>
        public bool Controller { get { return sim.classId == "class.rogue" || sim.classId == "class.archivist" || sim.classId == "class.scoundrel"; } }
        float nextHold, nextKick;
        /// <summary>Has an interrupt (Kick, Hush, Shield Bash, Censure, Quench): stops a cast in its reach every 12 s.</summary>
        public bool Interrupter { get { return sim.classId == "class.rogue" || sim.classId == "class.archivist" || sim.classId == "class.warrior" || sim.classId == "class.paladin" || sim.classId == "class.mage" || sim.classId == "class.scoundrel"; } }
        /// <summary>Its longest wait in seconds (a hold, a kick, a heal, a taunt), for a Scoundrel's favour (ScoundrelKit, 2026-10-14).</summary>
        public float LongestWait { get { return Mathf.Max(0, Mathf.Max(Mathf.Max(nextHold, nextKick), Mathf.Max(nextHeal, nextTaunt)) - Time.time); } }
        /// <summary>A Scoundrel's favour called in: its longest wait comes up to <paramref name="seconds"/> sooner. The seconds taken off.</summary>
        public float CallInFavour(float seconds)
        {
            float now = Time.time, wait = LongestWait; if (wait <= 0 || seconds <= 0) return 0;
            float cut = Mathf.Min(seconds, wait);
            if (nextHold - now >= wait - .001f) nextHold -= cut;
            else if (nextKick - now >= wait - .001f) nextKick -= cut;
            else if (nextHeal - now >= wait - .001f) nextHeal -= cut;
            else nextTaunt -= cut;
            return cut;
        }
        public bool Healer { get { return sim.classId == "class.druid" || sim.classId == "class.paladin" || sim.classId == "class.saltmender" || sim.classId == "class.cleric" || sim.classId == "class.shaman"; } }   // the Cleric and Shaman (note 178) heal when they come
        /// <summary>A Salt-Mender sim's rotation (2026-10-12): the mend above (Close the Cut) on the most hurt; the Mender-Craft chant over the
        /// party within 12 m every 4 s of a fight (or while anyone near it is hurt); a salt cairn every 20 s when two or more are hurt; salt shards between.</summary>
        /// <summary>Which of the reworked paths a sim plays (2026-10-12), by its variant: a Mage with an odd variant is a Bonecaller (it
        /// brings a bone thrall); an Archivist with an odd one plays the Unwriting (it unwrites its quarry), with an even one the Still
        /// Vow (it wards the most hurt).</summary>
        public static bool Bonecaller(SimAdventurer s) { return s != null && s.classId == "class.mage" && (s.variant & 1) == 1; }
        public static bool Unwriter(SimAdventurer s) { return s != null && s.classId == "class.archivist" && (s.variant & 1) == 1; }
        public static bool StillVow(SimAdventurer s) { return s != null && s.classId == "class.archivist" && (s.variant & 1) == 0; }
        float nextUnwrite, nextWard, unwriteNext; EncounterEnemy unwriting; int unwriteLeft;
        public int Unwrites { get; private set; }
        public int Wards { get; private set; }
        void PathRotation()
        {
            if (!session.InCombat) return;
            if (Unwriter(sim))
            {
                var q = Quarry;
                if (q != null && q.actor.IsAlive && Time.time >= nextUnwrite && Vector3.Distance(q.transform.position, transform.position) <= 30)
                {
                    nextUnwrite = Time.time + 12; unwriting = q; unwriteLeft = 6; unwriteNext = Time.time + 2; Unwrites++;
                    session.FloatText(q.transform.position + Vector3.up * 1.8f, "Unwritten", new Color(.62f, .58f, .7f));
                }
                if (unwriting != null && Time.time >= unwriteNext)
                {
                    if (!unwriting.actor.IsAlive || unwriteLeft <= 0) unwriting = null;
                    else { unwriteNext = Time.time + 2; unwriteLeft--; unwriting.Receive(Mathf.Max(2, Hit / 3), actor); }
                }
            }
            else if (StillVow(sim) && Time.time >= nextWard)
            {
                var hurt = MostHurt(30);
                if (hurt != null && hurt.Health.Pool.Ratio < .9f)
                {
                    nextWard = Time.time + 8; Wards++;
                    int size = Mathf.RoundToInt(hurt.Health.Pool.Max * .12f);
                    hurt.GetComponent<Combatant>().AddBarrier(size, 10); HealFx.Show(hurt.transform);
                    session.FloatText(hurt.transform.position + Vector3.up * 1.6f, "Stillness " + size, new Color(.85f, .92f, 1));
                }
            }
        }
        float nextChant, nextCairn;
        public int Chants { get; private set; }
        public int Cairns { get; private set; }
        void SaltRotation()
        {
            if (sim.classId != "class.saltmender" || !(session.InCombat || PartyNear(12).Exists(a => a.Health.Pool.Ratio < .9f))) return;   // in a fight, or anyone near it hurt
            if (Time.time >= nextChant)
            {
                nextChant = Time.time + 4; Chants++;
                foreach (var who in PartyNear(12)) { int healed = who.GetComponent<Combatant>().Heal(Mathf.Max(3, Mathf.RoundToInt(who.Health.Pool.Max * .04f)), actor); if (healed > 0) { session.FloatText(who.transform.position, "+" + healed, new Color(.3f, 1, .7f)); session.HealThreat(actor, healed); } }
                if (Chants == 1 && sim.chatty > .4f) session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .7f ? "chanting, stay close" : "I seal the Wound! Keep near me.");
            }
            if (Time.time >= nextCairn && PartyNear(25).Count(a => a.Health.Pool.Ratio < .75f) >= 2)
            {
                nextCairn = Time.time + 20; Cairns++;
                foreach (var who in PartyNear(10)) { who.GetComponent<Combatant>().Statuses.RemoveHarmful(); int healed = who.GetComponent<Combatant>().Heal(Mathf.RoundToInt(who.Health.Pool.Max * .1f), actor); if (healed > 0) { HealFx.Show(who.transform); session.HealThreat(actor, healed); } }
                session.FloatText(transform.position + Vector3.up * 2, "Salt Cairn", new Color(.95f, .95f, .9f));
            }
        }
        List<Actor> PartyNear(float reach)
        {
            var near = new List<Actor>();
            void Consider(Actor a) { if (a != null && a.IsAlive && Vector3.Distance(a.transform.position, transform.position) <= reach && !near.Contains(a)) near.Add(a); }
            Consider(session.Player); if (session.Progress.recruited && session.Companion != null) Consider(session.Companion.actor);
            foreach (var p in session.PartySims) if (p != null) Consider(p.actor);
            return near;
        }
        public float Reach { get { return Melee ? 2.4f : 20; } }
        public float Interval { get { return Melee ? 2.0f : 2.4f; } }
        /// <summary>A sim's blow: a player's of its level, near enough (2026-10-11, the group run's meter: sims at 8-15 a second against you at 30,
        /// "still low for most"; they hit (6 + 2.4 a level) before, 40% of a player's).</summary>
        public int Hit { get { return sim.Practised((Melee ? 12 : 10) + (Melee ? 4f : 3.6f) * sim.level); } }   // its yard practice too (SimFigure)
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
        /// <summary>A tester's shortcut (/adit; Chris, 2026-10-09: "the /adit command did not level all the party members"): brought up to
        /// <paramref name="level"/> at once, whole, dressed for it. Nothing for a sim already there or above.</summary>
        public void MatchLevel(int level)
        {
            level = Mathf.Min(level, EncounterProgress.LevelCap); if (sim.level >= level) return;
            sim.level = level; sim.experience = EncounterProgress.XpForLevel(level);
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, MaxHealthFor(sim)); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            SimGear.Dress(GetComponent<ActorVisual>(), sim, session.Items);
        }
        /// <summary>Its choice in a roll (Round 25, note 72): Need for gear of its weight and slot that beats what it wears, Greed for
        /// anything it could sell, Pass for what is no use to it (a potion it cannot use goes to Greed too: coin is coin).</summary>
        public RollChoice RollFor(ItemDef d)
        {
            if (d == null) return RollChoice.Pass;
            if (d.kind == "gear" && !string.IsNullOrEmpty(d.classOnly))   // a class set's piece (loot DESIGN.md 3.8): Need it for its own class (unless worn already), never for another's
                return d.classOnly == sim.classId && d.level <= sim.level && SimEconomy.Worn(sim, d.slot) != d.id ? RollChoice.Need : RollChoice.Greed;
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
        /// <summary>The armour of the pieces a sim wears (SimGear.For: its class's weight at its level), summed as the player's is.</summary>
        public static int ArmourFor(SimAdventurer s, ItemDatabase items)
        {
            int armour = 0; if (s == null || items == null) return 0;
            foreach (var id in SimGear.For(s, items)) { var d = items.Get(id); if (d != null) armour += d.armor; }
            return armour;
        }

        public void Init(Actor a, EncounterSession s, SimAdventurer data, int partySlot)
        {
            actor = a; session = s; sim = data; slot = partySlot; agent = GetComponent<NavMeshAgent>(); visual = GetComponent<ActorVisual>();
            actor.Stats.SetBase(Crulanda.Core.StatType.MaxHealth, MaxHealthFor(sim)); actor.Health.ApplyHealing(actor.Health.Pool.Max);
            actor.Stats.SetBase(Crulanda.Core.StatType.Armor, ArmourFor(sim, session.Items));   // what it wears turns blows as yours does (2026-10-11; it had no armour at all)
            actor.Health.Died += h => session.Meter.Death(actor);   // the scorecard's deaths
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
        float heldUntil, campIdleSince = -1; string heldKind; Vector3 fearFrom;
        // Headway toward the camp it leads to (2026-10-13): the nearest it has come, when, and the last frame it was leading.
        float leadBest, leadBestAt, leadSeen; Vector3 leadFor;
        /// <summary>A boss's hold on this sim (EliteMove.cc): "stun", "root", "fear" or "disorient", for seconds, from where (fear runs from it).</summary>
        public void HoldUntil(string kind, float seconds, Vector3 from)
        {
            if (!actor.IsAlive || seconds <= 0) return;
            heldKind = kind; heldUntil = Mathf.Max(heldUntil, Time.time + seconds); fearFrom = from;
            session.FloatText(transform.position + Vector3.up * .4f, kind == "fear" ? "Fleeing" : kind == "root" ? "Rooted" : kind == "disorient" ? "Disoriented" : "Stunned", new Color(1, .85f, .3f));
        }
        public bool HeldNow { get { return Time.time < heldUntil; } }
        /// <summary>How far behind the leader its farthest living party member is (you, the sims, the wolves), in metres.</summary>
        public float PartyBehind()
        {
            float worst = 0; var me = transform.position;
            var you = session.Player; if (you != null && you.IsAlive) worst = Mathf.Max(worst, Vector3.Distance(me, you.transform.position));
            foreach (var c in session.PartySims) if (c != null && c != this && c.actor != null && c.actor.IsAlive) worst = Mathf.Max(worst, Vector3.Distance(me, c.transform.position));
            return worst;
        }
        /// <summary>A mob the party's holds have put out of the fight (sapped, gouged, bound, blinded): nobody strikes it while anything else
        /// stands (Chris, 2026-10-11: "party doesnt ignore the CC mobs"). A stunned one is fair game: the stun does not break.</summary>
        public static bool Held(EncounterEnemy e) { return e != null && (e.Incapacitated || e.Feared); }
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
                        if (e == null || !e.actor.IsAlive || !e.FightingParty || e.LeaveAlone || e.Victim == null || e.Victim == actor || Held(e)) continue;
                        var vc = e.Victim.GetComponent<SimCompanion>(); if (vc != null && vc.Role == PartyRole.Tank) continue;
                        float d = Vector3.Distance(e.transform.position, transform.position); if (d < peelD) { peel = e; peelD = d; }
                    }
                    if (peel != null) return peel;
                }
                foreach (var e in session.Enemies)   // the skull first (CC step C3)
                    if (e != null && e.RaidMarked == RaidMark.Skull && e.actor.IsAlive && e.FightingParty && !e.Incapacitated && Vector3.Distance(e.transform.position, transform.position) < 30) return e;
                var t = session.Target;
                if (t != null && t.actor.IsAlive && !t.Game && !t.LeaveAlone && !Held(t) && (t.FightingParty || session.AutoAttack)) return t;
                EncounterEnemy best = null; float bestD = 30;
                foreach (var e in session.Enemies)
                {
                    if (e == null || !e.actor.IsAlive || !e.FightingParty || e.LeaveAlone || Held(e)) continue;
                    float d = Vector3.Distance(e.transform.position, transform.position); if (d < bestD) { best = e; bestD = d; }
                }
                if (best == null && Leading.HasValue && waitingAt == null && !Regrouping && Vector3.Distance(transform.position, Leading.Value) < 18 && PartyBehind() <= 16)   // no pull till the party is up
                {
                    // At the camp it leads to: the nearest of its mobs still standing (not while it waits at a shut gate: the camp past it is not its to pull, D7;
                    // not while the party gathers at a wing's home; on the camp's own floor or deck, not one 12 m below it: the Adit rework).
                    foreach (var e in session.Enemies)
                    {
                        if (e == null || !e.actor.IsAlive || !e.Camp || e.Game) continue;
                        float d = Vector3.Distance(e.transform.position, Leading.Value); if (d < 16 && d < bestD && SameFloor(e.transform.position, Leading.Value)) { best = e; bestD = d; }
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
            bool rogue = sim.classId == "class.rogue", scoundrel = sim.classId == "class.scoundrel";
            string what = rogue ? (sim.chatty > .65f ? "gouged moon, dont touch it" : "Moon is held. Leave it be.") : scoundrel ? (sim.chatty > .65f ? "sang moon still, dont touch it" : "Moon's dazed. Leave it be.") : (sim.chatty > .65f ? "lulling moon" : "Moon's asleep. Don't wake it.");
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
        /// <summary>Where it leads, with "the" once ("the vent-hound", not "the the vent-hound").</summary>
        string ThePlace { get { var n = (LeadingName ?? "").ToLower(); return n.StartsWith("the ") ? n : "the " + n; } }
        /// <summary>A camp out of reach, left behind: on to the next on the route, or the run ends. (A regroup nobody comes to, and a side
        /// room behind a gate nobody opens, are left the same way, each with its own words.)</summary>
        void LeaveCamp()
        {
            var step = route.Count > 0 ? route[0] : null; bool chatty = sim.chatty > .65f;
            string say = step != null && step.regroup ? (chatty ? "cant wait here all day. on we go" : "We cannot wait here all day. On we go.")
                : step != null && step.optional && waitingAt != null ? (chatty ? "still shut. leave it, on we go" : "That stays shut. We leave it be and go on.")
                : (chatty ? "cant reach that one. leave it, on we go" : "Leave that one, it is out of our reach. On we go.");
            session.ChatSay(ChatChannel.Party, sim.name, say);
            waitingAt = null; StoppedBy = null;
            if (route.Count > 0) route.RemoveAt(0);
            PassRegroups();
            if (route.Count > 0) BeginStep(); else StopLeading();
        }
        public void StopLeading() { Leading = null; LeadingName = null; route.Clear(); Dungeon = null; StoppedBy = null; waitingAt = null; wingSeen.Clear(); }
        /// <summary>A dungeon run (round 27; the Adit rework: RouteStep, DungeonPlan): the steps still to go, in order, each with the gate across
        /// the way before it (D7: shut, the run waits at it), and the dungeon's name.</summary>
        readonly List<RouteStep> route = new List<RouteStep>();
        /// <summary>The run's steps still to go, the one it leads to now first (read only: the tests and the run capture).</summary>
        public IReadOnlyList<RouteStep> Route { get { return route; } }
        /// <summary>The wings of the run in the order it takes them (DungeonPlan; empty in a dungeon without wings).</summary>
        public readonly List<string> WingOrder = new List<string>();
        /// <summary>Which run it leads (/dungeon quick, /dungeon, /dungeon full).</summary>
        public DungeonPlan.Mode RunMode { get; private set; }
        public string Dungeon { get; private set; }
        /// <summary>The shut gate ahead that the run will wait at (the Sealed Adit's singing door or platform gate), or null once it is open or there is none.</summary>
        public string StoppedBy { get; private set; }
        /// <summary>The fights still to go (a regroup is not one).</summary>
        public int CampsLeft { get { int n = 0; foreach (var s in route) if (!s.regroup) n++; return n; } }
        /// <summary>The name of the wing of the step it leads to now, or null (a dungeon without wings).</summary>
        public string Wing { get { return route.Count > 0 ? route[0].wingName : null; } }
        /// <summary>The fights still to go in the wing it is in now.</summary>
        public int WingCampsLeft { get { int n = 0; if (route.Count == 0) return 0; foreach (var s in route) if (s.wing == route[0].wing && !s.regroup) n++; return n; } }
        /// <summary>Gathering the party at a wing's home before the next (the Adit rework): nothing is pulled meanwhile.</summary>
        public bool Regrouping { get { return route.Count > 0 && route[0].regroup && waitingAt == null; } }
        Crulanda.World.ZoneGate waitingAt; float waitingSince;
        /// <summary>Waiting at a shut gate for you to open it (D7).</summary>
        public bool WaitingAtGate { get { return waitingAt != null; } }
        /// <summary>The wings it has fought its way into this run (a regroup is only for a wing the party went through).</summary>
        readonly HashSet<string> wingSeen = new HashSet<string>();
        /// <summary>A place's name inside a sentence: "the Geode Foundry", not "The Geode Foundry".</summary>
        static string Within(string name) { return name != null && name.StartsWith("The ") ? "the " + name.Substring(4) : name; }
        /// <summary>A shut gate before the next step: the run goes to it and waits on its own side of it, saying what it wants; open, the run
        /// goes on (D7). A side room's gate (the pen's) is waited at a while, then left.</summary>
        void TickGate()
        {
            if (Dungeon == null || route.Count == 0) return;
            var g = route[0].gate;
            if (g != null && !g.Open)
            {
                if (waitingAt == g) { if (route[0].optional && Time.time - waitingSince > 45) LeaveCamp(); return; }
                waitingAt = g; waitingSince = Time.time; StoppedBy = g.Title; Leading = GateSide(g); LeadingName = g.Title.StartsWith("The ") ? g.Title.Substring(4) : g.Title;   // "Waiting at the singing door"
                var thing = session.Zone.Interactables.Find(i => i.gate == g); var prompt = thing != null ? session.InteractPromptFor(thing) : null;
                // A boss's door (the Adit rework) says whose death opens it: the run comes to one shut only when he was left standing.
                bool door = prompt == null && !string.IsNullOrEmpty(g.OpensOn);
                session.ChatSay(ChatChannel.Party, sim.name, door
                    ? (sim.chatty > .65f ? g.Title.ToLower() + " is shut till " + g.OpensOn.ToLower() + " drops" : g.Title + " is shut against us. It opens when " + g.OpensOn + " falls.")
                    : (sim.chatty > .65f ? g.Title.ToLower() + " is shut. " + (prompt ?? "open it").ToLower() + "?" : g.Title + " is shut against us. " + (prompt ?? "It wants opening") + "."));
            }
            else if (waitingAt != null)
            {
                waitingAt = null; StoppedBy = null; Leading = route[0].at; LeadingName = route[0].name;
                session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "open! on we go" : "It's open. On we go.");
            }
        }
        /// <summary>Where to wait at a shut gate: a step from it on the side the leader is on (a door the run comes to from its far end, as the
        /// Geode Foundry's chute door from the hall, is waited at from the hall).</summary>
        Vector3 GateSide(Crulanda.World.ZoneGate g)
        {
            var f = g.transform.forward; float side = Vector3.Dot(transform.position - g.transform.position, f) > 0 ? 1 : -1;
            var at = g.transform.position + f * (2.5f * side);
            return NavMesh.SamplePosition(at, out var hit, 3, NavMesh.AllAreas) ? hit.position : at;
        }
        /// <summary>
        /// Leads the party through the zone's dungeon (a walk-in cave with camps in it). A dungeon of wings (ZoneDefinition.wings: the Sealed
        /// Adit reworked) is run as DungeonPlan orders it: wing by wing, the nearest open one next, each wing's fights in its own order, a
        /// regroup at a wing's home where its way out comes down; <paramref name="mode"/> "quick" takes only what must be done, "full" every
        /// side room and both sides of a fork, anything else the standard run (with the side bosses in the wings it goes through). Any other
        /// dungeon: every camp whose ground is the cave's floor, in the order the passage reaches them from the mouth, the boss (an elite at
        /// the far end) last. Refuses (null, and why) when there is none, or when you are more than a level under its first camp (its camps
        /// are harder).
        /// </summary>
        public string LeadDungeon(out string why) { return LeadDungeon(null, out why); }
        public string LeadDungeon(string mode, out string why)
        {
            why = null; route.Clear(); Dungeon = null; StoppedBy = null; waitingAt = null; wingSeen.Clear(); WingOrder.Clear(); walks.Clear(); RunMode = DungeonPlan.ParseMode(mode);
            if (session.Zone == null) { why = "There is nowhere to go."; return null; }
            var zone = session.Zone.Zone;
            if (zone.wings != null && zone.wings.Length > 0 && zone.camps != null && Crulanda.World.Hollow.All.Count > 0)
            {
                var steps = DungeonPlan.Plan(zone, RunMode, transform.position, CampStand, AnchorAt, WalkLength, CampStanding, QuestWants, WingOrder);
                int firstFight = steps.FindIndex(s => !s.regroup);
                if (firstFight < 0) { why = "There is nothing left standing in " + zone.displayName + "."; return null; }
                int first = steps[firstFight].level; if (session.Progress.Level < first - 1) { why = zone.displayName + " is too much for us yet (level " + first + " at the door)."; return null; }
                foreach (var s in steps) if (!s.regroup) s.gate = session.GateAhead(zone.camps[s.Camp]);   // past a gate: the run waits at it while it is shut (D7)
                foreach (var s in steps) if (s.regroup) s.gate = ExitGate(s, steps);   // a wing's way out, behind its boss's door
                route.AddRange(steps);
                // The first shut gate the run will wait at: not a boss's door it opens on its way (his death opens it).
                for (int k = 0; k < route.Count && StoppedBy == null; k++)
                {
                    var g = route[k].gate; if (g == null || g.Open) continue;
                    bool opensOnTheWay = false;
                    if (!string.IsNullOrEmpty(g.OpensOn)) for (int j = 0; j < k && !opensOnTheWay; j++) foreach (int c in route[j].camps) if (zone.camps[c].mob == g.OpensOn) opensOnTheWay = true;
                    if (!opensOnTheWay) StoppedBy = g.Title;
                }
                Dungeon = zone.dungeon || WingOrder.Count == 0 ? zone.displayName : WingOrder[0];
                Begin();
                return Dungeon;
            }
            // The caves with their own mouths; a branch's camps come where the passage meets the branch (a dungeon without wings: Crowsfoot
            // Hollow's, the first Sealed Adit's).
            foreach (var h in Crulanda.World.Hollow.All)
            {
                if (h.Parent != null) continue;
                var camps = new System.Collections.Generic.List<(float along, Vector3 at, string name, bool boss, int lo, Crulanda.World.ZoneGate gate)>();
                Gather(h, camps, true);
                if (camps.Count == 0) continue;
                int first = camps[0].lo; if (session.Progress.Level < first - 1) { why = h.Name + " is too much for us yet (level " + first + " at the door)."; return null; }
                foreach (var cp in camps) { route.Add(new RouteStep { at = cp.at, name = cp.name, boss = cp.boss, gate = cp.gate, level = cp.lo }); if (StoppedBy == null && cp.gate != null && !cp.gate.Open) StoppedBy = cp.gate.Title; }   // the first shut gate: the run waits there (D7)
                Dungeon = session.Zone.Zone.dungeon ? session.Zone.Zone.displayName : h.Name;
                Begin();
                return Dungeon;
            }
            why = "There is no dungeon in these parts."; return null;
        }
        /// <summary>The run sets out: its mark on the first step, the scorecard counting from here, and the word to the party.</summary>
        void Begin()
        {
            BeginStep();
            session.Meter.BeginRun(Dungeon);   // the scorecard counts from here
            int n = CampsLeft; string way = RunMode == DungeonPlan.Mode.Quick ? " (the quick way)" : RunMode == DungeonPlan.Mode.Full ? " (every side room too)" : "";
            session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? "ok, " + Dungeon + " run. " + n + " camps to the bottom" + way + ", stay close" : "Follow me into " + Dungeon + ". " + n + " camps between us and the end" + way + ". Stay close.");
        }
        /// <summary>Its mark on the step it leads to now.</summary>
        void BeginStep()
        {
            var s = route[0]; Leading = s.at; LeadingName = s.name;
        }
        /// <summary>A regroup after a wing the run never fought through (its camps were down before the leader came to them) is passed by:
        /// there is nobody to gather.</summary>
        void PassRegroups() { while (route.Count > 0 && route[0].regroup && !wingSeen.Contains(route[0].wing)) route.RemoveAt(0); }
        /// <summary>The door on a wing's way out that its own boss's death opens (the Geode Foundry's chute door, the cloister door, the
        /// sluice-boards), not one across the way to a fight (Lusk's door, before the Workings' last camps): the regroup at the wing's home
        /// waits at it while it is shut. Null when there is none.</summary>
        Crulanda.World.ZoneGate ExitGate(RouteStep regroup, List<RouteStep> steps)
        {
            var camps = session.Zone.Zone.camps;
            foreach (var g in session.Zone.Gates)
            {
                if (g == null || string.IsNullOrEmpty(g.OpensOn)) continue;
                bool mine = false, onTheWay = false;
                foreach (var s in steps)
                {
                    if (s.gate == g) onTheWay = true;
                    if (!s.regroup && s.wing == regroup.wing) foreach (int c in s.camps) if (camps[c].mob == g.OpensOn) mine = true;
                }
                if (mine && !onTheWay) return g;
            }
            return null;
        }
        /// <summary>Where a camp stands, as it spawns: on its cave's floor, or up on its deck (ZoneCamp.rise; the Adit rework).</summary>
        Vector3 CampStand(int c)
        {
            var cp = session.Zone.Zone.camps[c]; var h = string.IsNullOrEmpty(cp.cave) ? null : Crulanda.World.Hollow.All.Find(o => o.Name == cp.cave);
            float y;
            if (h != null && (h.FloorSmooth(cp.center, out y) || h.FloorAt(cp.center, out y)))
            {
                if (cp.rise > 0) y = session.Zone.StandAt(cp.center, y + cp.rise).y;
                return new Vector3(cp.center.x, y, cp.center.y);
            }
            return session.Zone.StandAt(cp.center, float.NegativeInfinity);
        }
        Vector3? AnchorAt(string name) { return session.Zone.Anchors.TryGetValue(name, out var p) ? (Vector3?)p : null; }
        /// <summary>Whether anything of a camp is alive (a camp cleared, or a rare one not here this visit, is not).</summary>
        bool CampStanding(int c) { foreach (var e in session.Enemies) if (e != null && e.Camp && !e.Game && e.CampIndex == c && e.actor.IsAlive) return true; return false; }
        bool AnyStanding(int[] cs) { foreach (int c in cs) if (CampStanding(c)) return true; return false; }
        /// <summary>Whether a live quest still wants a camp's elite dead (a kill objective of its current step that matches the elite's id).</summary>
        bool QuestWants(int c)
        {
            var log = session.Quests; if (log == null) return false;
            var cp = session.Zone.Zone.camps[c]; string tag = string.IsNullOrEmpty(cp.tag) ? (cp.look ?? "mob") : cp.tag;
            string id = "mob." + tag + "." + session.Zone.Zone.id.Replace("zone.", "") + "." + c + ".0";   // its elite (EncounterSession.SpawnCamps)
            foreach (var (quest, state) in log.Active())
            {
                var step = log.CurrentStep(state); if (step == null) continue;
                for (int i = 0; i < step.objectives.Length; i++) { var o = step.objectives[i]; if (o.type == "kill" && QuestLog.Matches(o.target, id) && log.Count(state, i) < o.count) return true; }
            }
            return false;
        }
        /// <summary>How far it is to walk from a to b (the navmesh, the drops taken down, the gates as they stand): leg after leg where the
        /// search gives up part way (NavMesh.CalculatePath stops short on a long way), and the rest of the way twice over where a leg gets
        /// no nearer (a shut gate, or no way at all).</summary>
        float WalkLength(Vector3 a, Vector3 b)
        {
            if (walks.TryGetValue((a, b), out var known)) return known;
            float total = 0;
            if (!NavMesh.SamplePosition(a, out var ha, 4, NavMesh.AllAreas) || !NavMesh.SamplePosition(b, out var hb, 4, NavMesh.AllAreas)) return walks[(a, b)] = 3 * Vector3.Distance(a, b);
            var from = ha.position; var to = hb.position; var path = new NavMeshPath();
            for (int leg = 0; leg < 8; leg++)
            {
                if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.corners.Length == 0) break;
                for (int i = 1; i < path.corners.Length; i++) total += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                var end = path.corners[path.corners.Length - 1];
                if (path.status == NavMeshPathStatus.PathComplete) return walks[(a, b)] = total;
                bool nearer = (end - from).sqrMagnitude >= 1 && (end - to).sqrMagnitude < (from - to).sqrMagnitude - .25f;
                from = end; if (!nearer) break;
            }
            return walks[(a, b)] = total + 2 * Vector3.Distance(from, to);
        }
        readonly Dictionary<(Vector3, Vector3), float> walks = new Dictionary<(Vector3, Vector3), float>();
        /// <summary>Whether a mob is on the same floor or deck as a place (the Adit rework: a camp up on a deck is not held up by the mobs 12 m
        /// under it, nor pulled from there); a slope's fall over the distance is allowed for.</summary>
        static bool SameFloor(Vector3 mob, Vector3 place) { var d = mob - place; return Mathf.Abs(d.y) < 5 + .45f * new Vector2(d.x, d.z).magnitude; }
        /// <summary>A step after a patrolling side boss (the Old Burrower round its pool): its mark follows the nearest of it.</summary>
        void TrackStep()
        {
            if (route.Count == 0 || !route[0].follow || waitingAt != null || Time.time < nextTrack) return;
            nextTrack = Time.time + .5f; EncounterEnemy near = null; float best = float.MaxValue;
            foreach (var e in session.Enemies)
            {
                if (e == null || !e.Camp || !e.actor.IsAlive || System.Array.IndexOf(route[0].camps, e.CampIndex) < 0) continue;
                float d = Vector3.Distance(e.transform.position, transform.position); if (d < best) { best = d; near = e; }
            }
            if (near != null) { route[0].at = near.transform.position; Leading = route[0].at; }
        }
        float nextTrack;
        /// <summary>The step it leads to is done: a fight when none of its mobs is left near it (a patrolling side boss: when it is down), a
        /// regroup when the leader stands there and the party has gathered.</summary>
        bool StepDone()
        {
            if (!Leading.HasValue) return false;
            if (route.Count > 0 && route[0].regroup) return Vector3.Distance(transform.position, Leading.Value) < 7 && PartyBehind() < 12 && !session.InCombat;
            if (route.Count > 0 && route[0].follow) return !AnyStanding(route[0].camps);
            return CampCleared();
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
        /// <summary>No mob of the camp left near where it led to (on its floor or deck: the Adit rework): the run is done.</summary>
        bool CampCleared()
        {
            if (!Leading.HasValue) return false;
            foreach (var e in session.Enemies) if (e != null && e.actor.IsAlive && e.Camp && !e.Game && Vector3.Distance(e.transform.position, Leading.Value) < 16 && SameFloor(e.transform.position, Leading.Value)) return false;
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
            // Held by a boss (HoldUntil): stunned, rooted or disoriented it stands and does nothing; feared it runs from what frightened it.
            if (Time.time < heldUntil)
            {
                Activity = heldKind == "fear" ? "Fleeing" : heldKind == "root" ? "Rooted" : heldKind == "disorient" ? "Disoriented" : "Stunned";
                if (visual != null) visual.Casting = false; healing = null;
                if (agent.isOnNavMesh)
                {
                    if (heldKind == "fear") { var away = transform.position - fearFrom; away.y = 0; agent.isStopped = false; agent.speed = 4.5f; agent.SetDestination(transform.position + (away.sqrMagnitude > .01f ? away.normalized : transform.forward) * 6); }
                    else agent.isStopped = true;
                }
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
            if (!session.InCombat && Time.time >= nextRegen) { nextRegen = Time.time + 1; actor.Health.ApplyHealing(Mathf.Max(3, actor.Health.Pool.Max / 40)); }
            // Mending first, for the healers: the most hurt of the party under 60%. A cast of a second (the spell pose held, playtest
            // note 68), then the release, the heal and its glow.
            if (healing != null)
            {
                if (Time.time < healAt) { Activity = "Mending " + (healing == player ? "you" : healing.DisplayName); return; }
                var who = healing; healing = null; if (visual != null) visual.Casting = false;
                if (who.IsAlive && Vector3.Distance(who.transform.position, transform.position) <= 28)
                {
                    int healed = who.GetComponent<Combatant>().Heal(MendFor(who), actor);
                    session.FloatText(who.transform.position, "+" + healed, new Color(.3f, 1, .7f)); session.HealThreat(actor, healed);
                    visual?.CastRelease("boltL"); if (healed > 0) HealFx.Show(who.transform);
                }
            }
            // The party's healer (a Druid, or a Paladin with no Druid along) mends as Mira does: whoever is most hurt within reach, under
            // 80%, in a fight or out of it, every 2.5 s, for a sixth or so of their health. A Paladin tanking only steadies someone under
            // 60% in a fight, every 6 s. (Playtest note 122, Chris, 2026-10-11: "the druids are still not healing party members!". They
            // had mended only under 60%, only in a fight, every 6 s and for a small flat sum; nobody at all while the most hurt was out of
            // reach; and never out of a fight, the sim's own mending each second holding the same timer back.)
            else if (Healer && Time.time >= nextHeal && (session.InCombat || Role == PartyRole.Healer))
            {
                bool mender = Role == PartyRole.Healer;
                var hurt = MostHurt(HealReach);
                if (hurt != null && hurt.Health.Pool.Ratio < (mender ? .8f : .6f))
                {
                    nextHeal = Time.time + (mender ? MendEvery : 6); nextAct = Mathf.Max(nextAct, Time.time + (mender ? 1.2f : 2.2f));
                    healing = hurt; healAt = Time.time + 1; if (visual != null) visual.Casting = true;
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    Activity = "Mending " + (hurt == player ? "you" : hurt.DisplayName);
                    return;
                }
            }
            SaltRotation(); PathRotation();
            TrackStep();   // a patrolling side boss: the mark follows it (the Adit rework)
            TickGate();   // a shut gate ahead: the run waits at it and goes on when it opens (D7)
            var quarry = Quarry;
            // In a wing: at one of its fights, or fighting near one (the Adit rework: a regroup at a wing's home is for a wing the party went through,
            // not one whose camps were down before the leader came to them).
            if (Leading.HasValue && route.Count > 0 && !route[0].regroup && route[0].wing != null && waitingAt == null)
            { float near = Vector3.Distance(transform.position, Leading.Value); if (near < 20 || session.InCombat && near < 35) wingSeen.Add(route[0].wing); }
            // A camp nobody can get at (2026-10-11, the group run: an Ash mender up where no one reached it held the run): forty-five seconds
            // at the camp with nothing to fight and it is left behind (and a regroup the party never comes to).
            if (Leading.HasValue && quarry == null && waitingAt == null && !StepDone() && Vector3.Distance(transform.position, Leading.Value) < 18 && !session.InCombat)
            {
                if (campIdleSince < 0) campIdleSince = Time.time;
                else if (Time.time - campIdleSince > 45) { campIdleSince = -1; LeaveCamp(); }
            }
            else campIdleSince = -1;
            if (Leading.HasValue && quarry == null && waitingAt == null && StepDone())
            {
                var done = route.Count > 0 ? route[0] : null;
                if (done != null) { route.RemoveAt(0); PassRegroups(); }
                if (route.Count > 0)
                {
                    // A dungeon run (round 27): on to the next step, a regroup at a wing's home or the next wing as it comes (the Adit rework).
                    BeginStep(); var next = route[0]; bool chatty = sim.chatty > .65f;
                    string say;
                    if (next.regroup) say = chatty ? "clear. regroup at " + ThePlace : "Clear. Gather at " + ThePlace + " before we go on.";
                    else if (done != null && done.regroup) say = chatty ? "all here. next: " + (next.wingName ?? LeadingName).ToLower() : "All here. On to " + Within(next.wingName ?? LeadingName) + ".";
                    else if (done != null && next.wingName != null && next.wing != done.wing) say = chatty ? "clear. on to " + next.wingName.ToLower() : "Clear. On to " + Within(next.wingName) + ".";
                    else say = next.boss ? (chatty ? "boss next. " + LeadingName + ". buff up" : "That's the last before " + LeadingName + ". Ready yourselves.") : (chatty ? "clear. next: " + LeadingName.ToLower() : "Clear. On to " + LeadingName + ".");
                    session.ChatSay(ChatChannel.Party, sim.name, say);
                }
                else if (Dungeon != null)
                {
                    session.ChatSay(ChatChannel.Party, sim.name, sim.chatty > .65f ? Dungeon + " done!! gg all" : Dungeon + " is cleared. That was well done, all of you.");
                    session.Message(sim.name + ": " + Dungeon + " cleared."); foreach (var p in session.PartySims) if (p != null) SimMemory.Note(p.sim, SimMemory.Deed.KillsTogether, 3);
                    session.ShowScorecard(Dungeon + " cleared");
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
                        // A kick that does not take (immune, or too soon after the last) waits too (2026-10-13: it was tried again every frame,
                        // and "Immune" stacked into a white column over the Cinder-Warden in the evening group run).
                        nextKick = Time.time + 4;
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
                        var colour = sim.classId == "class.mage" ? new Color(1, .55f, .15f) : sim.classId == "class.druid" ? new Color(.45f, .85f, .3f) : sim.classId == "class.archivist" ? new Color(.6f, .8f, 1) : sim.classId == "class.saltmender" ? new Color(.97f, .97f, .92f) : new Color(.9f, .88f, .8f);
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
            // No headway for 45 s out of a fight (2026-10-13, the evening group run: the leader stood "leading you to the lava ledge" for
            // eleven minutes, the camp up where no path reaches and never within the 18 m of the rule above): that camp is left behind.
            if (Leading.HasValue)
            {
                float toCamp = Vector3.Distance(transform.position, Leading.Value);
                if (toPlayer > 18) { Activity = "Waiting for you"; agent.isStopped = true; return; }
                // The whole party, not only you (Chris, 2026-10-11, watching the run: "warrior needs to wait on party"): it stops while
                // anyone alive is more than 12 m behind, and pulls nothing until they are all within reach.
                float behind = PartyBehind();
                if (behind > 16) { Activity = "Waiting for the party"; agent.isStopped = true; return; }
                // A long way through the caves (2026-10-15, the adit-final run: woken at the yard after a wipe at Nix, the leader was sent at his
                // camp 400 m off, its path came back partial (the navmesh's search gives up on a long way), it stood at the path's end, and the
                // camp was left as "out of reach" 45 s on: Nix never fell and his chute door never opened). Once the path to the camp comes back
                // partial, it walks the caves' own way leg by leg (CaveWay) until it is within 30 m, and its headway is that way's length.
                if (leadLegFor != Leading.Value) { leadLegFor = Leading.Value; legging = false; nextLeg = 0; }
                if (toCamp <= 30) legging = false;
                else if (!legging && agent.hasPath && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete) legging = true;
                if (legging && Time.time >= nextLeg)
                {
                    nextLeg = Time.time + 1.5f;
                    if (Crulanda.World.CaveWay.Next(session.Zone, transform.position, Leading.Value, 30, out var wp, out float wayLeft)) { legTo = wp; legLeft = wayLeft; }
                    else legging = false;
                }
                if (toCamp > 6 && waitingAt == null)
                {
                    // Headway along the path it walks (a cave's way winds: the straight line can grow while it goes the right way).
                    float left = legging ? legLeft : agent.hasPath && !agent.pathPending && !float.IsInfinity(agent.remainingDistance) ? agent.remainingDistance : toCamp;
                    if (leadFor != Leading.Value || Time.time - leadSeen > 1) { leadFor = Leading.Value; leadBest = left; leadBestAt = Time.time; }
                    else if (left < leadBest - 1.5f) { leadBest = left; leadBestAt = Time.time; }
                    else if (!session.InCombat && Time.time - leadBestAt > 45) { leadSeen = 0; LeaveCamp(); return; }
                    leadSeen = Time.time;
                }
                if (toCamp > 6) { Activity = (waitingAt != null ? "Going to " : "Leading you to ") + ThePlace; agent.isStopped = false; agent.speed = 4.2f; agent.SetDestination(legging ? legTo : Leading.Value); return; }
                Activity = (waitingAt != null ? "Waiting at " : "At ") + ThePlace; agent.isStopped = true; return;
            }
            // At your side: a pace behind, each in its own place so they do not stack.
            Activity = toPlayer > 4 ? "Following you" : "At your side";
            var side = (slot % 2 == 0 ? -1 : 1) * (1.3f + .6f * (slot / 2));
            var spot = player.transform.position - player.transform.forward * (2.2f + .5f * slot) + player.transform.right * side;
            agent.isStopped = toPlayer < 2.8f;
            if (!agent.isStopped) { agent.SetDestination(spot); agent.speed = AdventurerMotor.Running ? 5.6f : toPlayer > 9 ? 4.2f : 2.3f; }
        }
        /// <summary>How far a party healer reaches, and how often the party's healer mends (a Paladin tanking: every 6 s).</summary>
        public const float HealReach = 25, MendEvery = 2.5f;
        /// <summary>Walking a long way leg by leg (CaveWay): the camp it is for, whether it is, the leg's end, the way left, the next re-plan.</summary>
        Vector3 leadLegFor, legTo; bool legging; float legLeft, nextLeg;
        /// <summary>Whether it is walking the caves' way leg by leg to its camp now (a long way the navmesh's search gave up on).</summary>
        public bool Legging { get { return legging; } }
        /// <summary>A mend: the party's healer gives a sixth of the healed one's health (or its old flat sum, if more); a tank's is the flat sum.</summary>
        public int MendFor(Actor who) { return Role == PartyRole.Healer ? Mathf.Max(HealAmount, Mathf.RoundToInt(who.Health.Pool.Max * .16f)) : HealAmount; }
        /// <summary>The most hurt of the party within <paramref name="reach"/>: you, Mira (when recruited), the sims, Mother Quillet.</summary>
        Actor MostHurt(float reach)
        {
            Actor best = null; float low = 1;
            void Consider(Actor a) { if (a != null && a.IsAlive && a.Health.Pool.Ratio < low && Vector3.Distance(a.transform.position, transform.position) <= reach) { best = a; low = a.Health.Pool.Ratio; } }
            Consider(session.Player);
            if (session.Progress.recruited && session.Companion != null) Consider(session.Companion.actor);
            foreach (var p in session.PartySims) if (p != null) Consider(p.actor);
            if (session.Weaver != null) Consider(session.Weaver.actor);   // Mother Quillet (D4)
            return best;
        }
    }
}
