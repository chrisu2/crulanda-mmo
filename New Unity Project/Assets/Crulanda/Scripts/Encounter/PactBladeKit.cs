using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The Pact-Blade (Docs/CLASSES_9_2.md section 4; Chris, 2026-10-19, play note 298: first class "Pact-Blade"; CANON-EXPANDED: Veyra's
    /// Iron Pact sword-work and her Vital-Aether, "the incredibly hot spark of Vital-Aether that every Iron-Born fiercely carried in their
    /// blood" (book 1 ch. 4), her whirl of white light and gold afterimages (book 1 ch. 10); the world bible's dual-sword master and Talira
    /// Frostveil, sword master; names PROVISIONAL). Two swords and heavy armour, pure damage: no healing, no oaths. No machines.
    /// Vital-Aether (a Focus pool of 100, named on the HUD) is built by the cuts only (never by a plain swing) and spent on the aether-cuts;
    /// it starts a fight at nothing and drains after. Above <see cref="BurningAt"/> the blood burns: the cuts land harder and you lose a
    /// little health each second.
    /// Bar: 1 Measured Cut, 2 Second Blade, 3 Minimum Movement, 4 Hilt Blow, 5 Iron Vow, 6 Sentinel's Stop (every path); the Twin-Blade's
    /// 7 Aether Edge, 8 Afterimage, 9 Blood-Vow; the White Whirl's 10 White Whirl, 11 Afterglow, 12 Turning Steel; the Sentinel's
    /// 13 Answering Cut, 14 Disarming Cut, 15 Iron Ring. Each path's abilities are learnt by its own talents (Talents/pactblade.json).
    /// </summary>
    public sealed class PactBladeKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "tb-aether-edge", "tb-keen-pair", "tb-quick-blood", "tb-afterimage", "tb-bright-wake", "tb-blood-vow", "tb-hot-blood", "tb-measured-end", "tb-redclaw", "tb-no-wasted", "tb-twin-vow",
            "ww-whirl", "ww-wide-turn", "ww-reach", "ww-afterglow", "ww-lingering", "ww-turning-steel", "ww-blinding", "ww-white-light", "ww-long-turn", "ww-steady-feet", "ww-ring-of-gold",
            "sn-answering-cut", "sn-watchful", "sn-long-stop", "sn-disarming-cut", "sn-heavy-hand", "sn-iron-ring", "sn-held-ground", "sn-sentinel", "sn-patient", "sn-goad", "sn-iron-vow-kept" };
        public static readonly string[] BarIds = { "pactblade.measured_cut", "pactblade.second_blade", "pactblade.minimum_movement", "pactblade.hilt_blow",
            "pactblade.iron_vow", "pactblade.sentinels_stop", "pactblade.aether_edge", "pactblade.afterimage", "pactblade.blood_vow", "pactblade.white_whirl",
            "pactblade.afterglow", "pactblade.turning_steel", "pactblade.answering_cut", "pactblade.disarming_cut", "pactblade.iron_ring" };
        /// <summary>The path each gated ability belongs to (its talent is in that branch of Talents/pactblade.json).</summary>
        public static readonly Dictionary<string, string> PathOf = new Dictionary<string, string>(StringComparer.Ordinal) {
            { "pactblade.aether_edge", "twin-blade" }, { "pactblade.afterimage", "twin-blade" }, { "pactblade.blood_vow", "twin-blade" },
            { "pactblade.white_whirl", "white-whirl" }, { "pactblade.afterglow", "white-whirl" }, { "pactblade.turning_steel", "white-whirl" },
            { "pactblade.answering_cut", "sentinel" }, { "pactblade.disarming_cut", "sentinel" }, { "pactblade.iron_ring", "sentinel" } };
        public const int MeasuredAether = 12, SecondAether = 10, MovementAether = 10, QuickBloodPerRank = 2, EdgeLeast = 30, MeasuredEndBack = 20, WhiteLightAether = 3;
        public const float BurningAt = 80, BurningLift = .1f, HotBloodPerRank = .03f, BurningDrain = .005f, BurningFloor = .1f;
        public const float MeasuredWeapon = .35f, SecondWeapon = .55f, HiltWeapon = .2f, StopWeapon = .3f, StopSlow = .4f, EdgeWeapon = .5f, AfterWeapon = .9f, AfterShare = .6f, BrightWakePerRank = .1f, TwinVowShare = .4f, AfterDelay = 2;
        public const float KeenPerRank = .03f, RedclawPerRank = .03f, RedclawBelow = .35f, NoWastedSeconds = 2, VowCut = .2f, BloodLift = .2f, BloodDrain = .01f;
        public const float WhirlWeapon = .55f, WhirlReach = 5, ReachPerRank = .5f, GlowWeapon = .12f, GlowReach = 4, TurningShare = .6f, TurningReach = 4, BlindingPerRank = .1f, SteadyFeetCut = .05f, RingOfGoldSeconds = 12;
        public const int TurningMore = 2;
        public const float AnswerWeapon = .8f, AnswerWindow = 4, AnswerSlow = .3f, DisarmWeapon = .5f, DisarmCut = .25f, DisarmBossCut = .1f, HeavyHandPerRank = .03f, RingReach = 6, RingSlow = .3f, SentinelLift = .05f, GoadSeconds = 3, VowRingReach = 5, VowRingSeconds = 3;
        public const float MovementLeast = 4, MovementStop = 1.4f;
        static readonly Color Gold = new Color(1, .86f, .45f), White = new Color(.95f, .95f, 1);

        readonly Dictionary<string, AbilityDefinition> abilities = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, ClassAbilityUnlock> unlocks = new Dictionary<string, ClassAbilityUnlock>(StringComparer.Ordinal);
        readonly PeriodicEffects periodic = new PeriodicEffects();
        /// <summary>Afterimages waiting to strike: the foe, when, and how hard.</summary>
        readonly List<(EncounterEnemy e, float at, int blow)> afterimages = new List<(EncounterEnemy, float, int)>();
        /// <summary>Disarmed foes, and when their blows come back to full.</summary>
        readonly Dictionary<EncounterEnemy, float> disarmed = new Dictionary<EncounterEnemy, float>();
        float vowUntil, bloodUntil, turningUntil, glowUntil, lastStruckAt = -100, nextRingOfGold, fightAt, drain, burnCarry; Vector3 glowAt;
        VitalPool wokePool;

        public PactBladeKit(EncounterSession session, ClassDefinition definition, IEnumerable<AbilityDefinition> catalog) : base(session)
        {
            if (definition == null || definition.unlocks == null || catalog == null) throw new ArgumentException("Pact-Blade content missing.");
            foreach (var a in catalog) if (a != null && !string.IsNullOrEmpty(a.id) && !abilities.ContainsKey(a.id)) abilities.Add(a.id, a);
            foreach (var u in definition.unlocks)
                if (u == null || string.IsNullOrEmpty(u.abilityId) || u.level < 1 || u.level > 10 || unlocks.ContainsKey(u.abilityId)) throw new ArgumentException("Invalid or duplicate Pact-Blade unlock.");
                else unlocks.Add(u.abilityId, u);
            foreach (var id in BarIds) if (!abilities.ContainsKey(id) || !unlocks.ContainsKey(id)) throw new ArgumentException("Pact-Blade ability '" + id + "' is missing from content or unlocks.");
        }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        /// <summary>Its standing party buff at a buff-up before a boss (GAME-ONLY): the Iron-Born's heat shared round the party. Half an
        /// hour, +3% Strength on everyone (BossBuffs).</summary>
        public static readonly ClassBuff Standing = new ClassBuff("class.pactblade", "Iron-Born Blood", Crulanda.Core.StatType.Strength);
        public override ClassBuff PartyBuff { get { return Standing; } }
        int R(string id) { return s.TalentRank(id); }
        float Now { get { return Time.time; } }
        int Dmg(float amount) { return Mathf.Max(1, Mathf.RoundToInt(amount)); }
        bool Need(bool ok, string message) { if (!ok) s.Message(message); return ok; }
        bool Start(AbilityDefinition a, Action effect) { return s.StartAbility(a, () => { if (s.Player.IsAlive) effect(); }) == AbilityStartResult.Started; }
        static AbilityDefinition With(AbilityDefinition a, int cost, float cooldown)
        {
            return new AbilityDefinition { id = a.id, name = a.name, description = a.description, effect = a.effect, power = a.power, cost = Mathf.Max(0, cost),
                cooldown = Mathf.Max(a.cooldown > 0 ? 1 : 0, cooldown), range = a.range, castTime = a.castTime, globalCooldown = a.globalCooldown, duration = a.duration, statusId = a.statusId };
        }
        bool Held(EncounterEnemy t, string why) { if (why == null) return true; s.Message(t.actor.DisplayName + ": " + why + "."); return false; }
        VitalPool Pool { get { return s.Player != null ? s.Player.Resource.Pool : null; } }
        public int Aether { get { var p = Pool; return p != null ? p.Current : 0; } }
        void Gain(int n) { var p = Pool; if (n > 0 && p != null) p.SetCurrent(Mathf.Min(p.Max, p.Current + n)); }
        List<EncounterEnemy> FoesNear(Vector3 at, float r) { return s.Enemies.Where(e => e != null && e.actor.IsAlive && !e.Game && !e.Hidden && Vector3.Distance(e.transform.position, at) <= r).ToList(); }

        // ---------- the numbers (static, for the tests) ----------
        /// <summary>Burning Blood: above 80 Vital-Aether the cuts land a tenth harder (Hot Blood: more).</summary>
        public static float BurningScale(int aether, int hotBlood) { return aether > BurningAt ? 1 + BurningLift + HotBloodPerRank * hotBlood : 1; }
        /// <summary>Aether Edge's blow: its power and half the weapon, for every 30 Vital-Aether spent.</summary>
        public static float EdgeBlow(float power, float weapon, int spent) { return (power + weapon * EdgeWeapon) * spent / (float)EdgeLeast; }
        /// <summary>The share of a cut its afterimage strikes again for (Bright Wake: more).</summary>
        public static float AfterimageShare(int brightWake) { return AfterShare + BrightWakePerRank * brightWake; }
        /// <summary>A disarmed foe's blows: a quarter lighter (a boss's a tenth; Heavy Hand: more off each).</summary>
        public static float DisarmScale(bool boss, int heavyHand) { return 1 - (boss ? DisarmBossCut : DisarmCut) - HeavyHandPerRank * heavyHand; }

        /// <summary>Every cut of yours: the talents and the blood's heat on it.</summary>
        float CutScale(EncounterEnemy t, bool keen)
        {
            float k = BurningScale(Aether, R("tb-hot-blood"));
            if (keen) k *= 1 + KeenPerRank * R("tb-keen-pair");
            if (Now < bloodUntil) k *= 1 + BloodLift;
            if (R("tb-redclaw") > 0 && t != null && t.actor.Health.Pool.Ratio < RedclawBelow) k *= 1 + RedclawPerRank * R("tb-redclaw");
            if (R("sn-sentinel") > 0 && t != null && (t.Rooted || t.Slowed)) k *= 1 + SentinelLift;
            return k;
        }
        int Cut(EncounterEnemy t, float amount) { if (t == null || !t.actor.IsAlive) return 0; int d = Dmg(amount); t.Receive(d, s.Player); return d; }
        /// <summary>Turning Steel: the cut also falls on up to two more foes near the target, at 60%.</summary>
        void Turning(EncounterEnemy t, int dealt)
        {
            if (Now >= turningUntil || t == null || dealt <= 0) return;
            foreach (var e in FoesNear(t.transform.position, TurningReach).Where(e => e != t).OrderBy(e => Vector3.Distance(e.transform.position, t.transform.position)).Take(TurningMore))
                Cut(e, dealt * TurningShare);
        }
        void Afterimage(EncounterEnemy t, float share, int dealt) { if (t != null && dealt > 0 && share > 0) afterimages.Add((t, Now + AfterDelay, Dmg(dealt * share))); }

        // ---------- action bar ----------
        public override int ActionCount { get { return BarIds.Length; } }
        public override AbilityDefinition ActionAt(int slot) { return slot >= 0 && slot < BarIds.Length && abilities.TryGetValue(BarIds[slot], out var a) ? a : null; }
        public override string ActionLockLabel(int slot)
        {
            var a = ActionAt(slot); if (a == null || s.Player == null) return "Unavailable";
            var u = unlocks[a.id];
            if (Level < u.level) return "Level " + u.level;
            if (!string.IsNullOrEmpty(u.talentId) && R(u.talentId) == 0) return "Talent";
            return null;
        }
        int Level { get { return s.Player.Level; } }
        public override bool Use(int slot)
        {
            var a = ActionAt(slot); if (a == null) return false;
            switch (a.id)
            {
                case "pactblade.measured_cut": return MeasuredCut(a);
                case "pactblade.second_blade": return SecondBlade(a);
                case "pactblade.minimum_movement": return MinimumMovement(a);
                case "pactblade.hilt_blow": return HiltBlow(a);
                case "pactblade.iron_vow": return IronVow(a);
                case "pactblade.sentinels_stop": return SentinelsStop(a);
                case "pactblade.aether_edge": return AetherEdge(a);
                case "pactblade.afterimage": return AfterimageCut(a);
                case "pactblade.blood_vow": return BloodVow(a);
                case "pactblade.white_whirl": return WhiteWhirl(a);
                case "pactblade.afterglow": return Afterglow(a);
                case "pactblade.turning_steel": return TurningSteel(a);
                case "pactblade.answering_cut": return AnsweringCut(a);
                case "pactblade.disarming_cut": return DisarmingCut(a);
                case "pactblade.iron_ring": return IronRing(a);
            }
            return false;
        }

        // ---------- every path ----------
        /// <summary>Measured Cut: the right sword, no wasted movement; builds Vital-Aether (Quick Blood: more) and starts the swings.</summary>
        bool MeasuredCut(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                s.BeginAutoAttack();
                int d = Cut(t, (a.power + s.WeaponDamage * MeasuredWeapon) * CutScale(t, true)); Turning(t, d);
                Gain(MeasuredAether + QuickBloodPerRank * R("tb-quick-blood"));
            });
        }
        /// <summary>Second Blade: the left sword follows (No Wasted Motion: sooner; the Twin Vow: an afterimage too).</summary>
        bool SecondBlade(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - (R("tb-no-wasted") > 0 ? NoWastedSeconds : 0)), () => {
                s.BeginAutoAttack();
                int d = Cut(t, (a.power + s.WeaponDamage * SecondWeapon) * CutScale(t, true)); Turning(t, d);
                if (R("tb-twin-vow") > 0) Afterimage(t, TwinVowShare, d);
                Gain(SecondAether);
            });
        }
        /// <summary>Minimum Movement: one stride to the foe, 4 to 15 m (Veyra's "minimum wasted movement", book 1 ch. 10).</summary>
        bool MinimumMovement(AbilityDefinition a)
        {
            var t = s.Target;
            if (!Need(t != null && t.actor.IsAlive, "No target.")) return false;
            float far = Vector3.Distance(s.Player.transform.position, t.transform.position);
            if (!Need(far >= MovementLeast, "Too close to stride.")) return false;
            if (!s.RequireEnemyInRange(a.range)) return false;
            return Start(a, () => {
                if (!t.actor.IsAlive) return;
                var me = s.Player.transform; var toward = t.transform.position - me.position; toward.y = 0;
                if (toward.magnitude > MovementStop + .3f)
                {
                    var landing = t.transform.position - toward.normalized * MovementStop;
                    if (UnityEngine.AI.NavMesh.SamplePosition(landing, out var hit, 2, UnityEngine.AI.NavMesh.AllAreas)) { landing = hit.position + Vector3.up * 1.05f; s.Player.GetComponent<AdventurerMotor>().Teleport(landing); }
                }
                if (toward.sqrMagnitude > .01f) me.rotation = Quaternion.LookRotation(toward.normalized);
                s.BeginAutoAttack(); Gain(MovementAether);
            });
        }
        /// <summary>Hilt Blow: the cast stopped, silenced (the interrupt; Patient Hilt: sooner).</summary>
        bool HiltBlow(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(With(a, a.cost, a.cooldown - R("sn-patient")), () => {
                if (!s.LandsOn(t, a.range)) return;
                Cut(t, (a.power + s.WeaponDamage * HiltWeapon) * CutScale(t, false));
                if (t.actor.IsAlive) Held(t, t.Apply("silence", s.Player, a.duration));
            });
        }
        /// <summary>The Iron Vow: 6 s of a fifth less taken (The Iron Vow Kept: every foe within 5 m held where it stands, 3 s).</summary>
        bool IronVow(AbilityDefinition a)
        {
            return Start(a, () => {
                vowUntil = Now + a.duration; s.FloatText(s.Player.transform.position, "Iron Vow", White);
                if (R("sn-iron-vow-kept") > 0) foreach (var e in FoesNear(s.Player.transform.position, VowRingReach)) e.Root(VowRingSeconds);
            });
        }
        /// <summary>Sentinel's Stop: a low cut; the foe held where it stands 4 s (Long Stop: longer) and slowed (the hold).</summary>
        bool SentinelsStop(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                if (!s.LandsOn(t, a.range)) return;
                float seconds = a.duration + R("sn-long-stop");
                Cut(t, (a.power + s.WeaponDamage * StopWeapon) * CutScale(t, false));
                if (!t.actor.IsAlive) return;
                t.Root(seconds); t.Slow(StopSlow, seconds);
                s.FloatText(t.transform.position + Vector3.up * .4f, "Stopped", White);
            });
        }

        // ---------- the Twin-Blade (dps, single target) ----------
        /// <summary>Aether Edge: all your Vital-Aether (30 at least) into one cut (Measured to the End: 20 back when spent at 80 or more).</summary>
        bool AetherEdge(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                float scale = CutScale(t, true);   // the heat as it was, before the spending
                var pool = Pool; int extra = pool.Current; pool.Change(-extra); int spent = a.cost + extra;
                int d = Cut(t, EdgeBlow(a.power, s.WeaponDamage, spent) * scale); Turning(t, d);
                if (R("tb-measured-end") > 0 && spent >= BurningAt) Gain(MeasuredEndBack);
                s.FloatText(t.transform.position + Vector3.up * .4f, "Aether Edge", Gold);
            });
        }
        /// <summary>Afterimage: a bright cut, and its gold afterimage strikes again 2 s later (Bright Wake: harder).</summary>
        bool AfterimageCut(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                int d = Cut(t, (a.power + s.WeaponDamage * AfterWeapon) * CutScale(t, false)); Turning(t, d);
                Afterimage(t, AfterimageShare(R("tb-bright-wake")), d);
            });
        }
        /// <summary>The Blood-Vow: 12 s of cuts a fifth harder, for 1% of your health a second.</summary>
        bool BloodVow(AbilityDefinition a)
        {
            return Start(a, () => { bloodUntil = Now + a.duration; s.FloatText(s.Player.transform.position, "Blood-Vow", new Color(.9f, .2f, .2f)); });
        }

        // ---------- the White Whirl (dps, area) ----------
        /// <summary>The White Whirl: every foe within 5 m (Long Reach: further) cut (Wide Turn: harder; White Light: Aether for each;
        /// Blinding Turn: slowed; Ring of Gold: a free Afterglow).</summary>
        bool WhiteWhirl(AbilityDefinition a)
        {
            float reach = WhirlReach + ReachPerRank * R("ww-reach");
            if (!Need(FoesNear(s.Player.transform.position, reach).Count > 0, "No foe within reach of the Whirl.")) return false;
            return Start(a, () => {
                var foes = FoesNear(s.Player.transform.position, reach);
                foreach (var e in foes)
                {
                    Cut(e, (a.power + s.WeaponDamage * WhirlWeapon) * CutScale(e, false) * (1 + KeenPerRank * R("ww-wide-turn")));
                    if (R("ww-blinding") > 0) e.Slow(BlindingPerRank * R("ww-blinding"), 3);
                }
                if (R("ww-white-light") > 0) Gain(WhiteLightAether * foes.Count);
                if (R("ww-ring-of-gold") > 0 && Now >= nextRingOfGold) { nextRingOfGold = Now + RingOfGoldSeconds; Glow(abilities["pactblade.afterglow"]); }
                s.BeginAutoAttack(); s.FloatText(s.Player.transform.position, "White Whirl", White);
            });
        }
        /// <summary>Afterglow: gold light on the ground round you for 6 s (Lingering Gold: longer), burning the foes in it each second.</summary>
        bool Afterglow(AbilityDefinition a) { return Start(a, () => Glow(a)); }
        void Glow(AbilityDefinition a)
        {
            glowAt = s.Player.transform.position; int ticks = Mathf.Max(1, Mathf.RoundToInt(a.duration)) + R("ww-lingering"); glowUntil = Now + ticks;
            int blow = Dmg(a.power + s.WeaponDamage * GlowWeapon);
            periodic.RemoveKey("glow");
            periodic.Add("glow", s.Player, Now, 1, ticks, blow, (e, i) => { foreach (var f in FoesNear(glowAt, GlowReach)) Cut(f, e.value * CutScale(f, false)); return true; });
            s.FloatText(glowAt, "Afterglow", Gold);
        }
        /// <summary>Turning Steel: 8 s (Long Turn: longer) of Measured Cut and Second Blade falling on two more foes.</summary>
        bool TurningSteel(AbilityDefinition a)
        {
            return Start(a, () => { turningUntil = Now + a.duration + R("ww-long-turn"); s.FloatText(s.Player.transform.position, "Turning Steel", White); });
        }

        // ---------- the Sentinel (control) ----------
        /// <summary>Whether a blow landed on you within the Answering Cut's window.</summary>
        public bool CanAnswer { get { return Now - lastStruckAt <= AnswerWindow; } }
        /// <summary>The Answering Cut: only within 4 s of a blow on you; the foe slowed (Watchful: harder; Goading: it turns on you).</summary>
        bool AnsweringCut(AbilityDefinition a)
        {
            if (!Need(CanAnswer, "Nothing to answer: no blow has landed on you.")) return false;
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                Cut(t, (a.power + s.WeaponDamage * AnswerWeapon) * CutScale(t, false) * (1 + KeenPerRank * R("sn-watchful")));
                if (!t.actor.IsAlive) return;
                t.Slow(AnswerSlow, a.duration);
                if (R("sn-goad") > 0) t.threat.Taunt(s.Player.EntityId.Value, Now, GoadSeconds);
            });
        }
        /// <summary>The Disarming Cut: the foe's blows lighter for 8 s (Heavy Hand: lighter again).</summary>
        bool DisarmingCut(AbilityDefinition a)
        {
            if (!s.RequireEnemyInRange(a.range)) return false; var t = s.Target;
            return Start(a, () => {
                Cut(t, (a.power + s.WeaponDamage * DisarmWeapon) * CutScale(t, false));
                if (!t.actor.IsAlive) return;
                disarmed[t] = Now + a.duration; s.FloatText(t.transform.position + Vector3.up * .4f, "Disarmed", White);
            });
        }
        public bool Disarmed(EncounterEnemy e) { return e != null && disarmed.TryGetValue(e, out var until) && Now < until; }
        /// <summary>The Iron Ring: every foe within 6 m held where it stands 3 s (Held Ground: longer) and slowed.</summary>
        bool IronRing(AbilityDefinition a)
        {
            if (!Need(FoesNear(s.Player.transform.position, RingReach).Count > 0, "No foe within the ring.")) return false;
            return Start(a, () => {
                float seconds = a.duration + R("sn-held-ground"); int n = 0;
                foreach (var e in FoesNear(s.Player.transform.position, RingReach)) { e.Root(seconds); e.Slow(RingSlow, seconds); n++; }
                s.Message("Iron Ring: " + n + (n == 1 ? " foe" : " foes") + " held.");
            });
        }

        // ---------- the blows on the party, the swings ----------
        /// <summary>A plain swing builds no Vital-Aether (as Chris ruled for the Scoundrel's Nerve, 2026-10-14): the cuts earn it.</summary>
        public override void OnAutoHit() { }
        /// <summary>The Iron Vow and Steady Feet on you; a disarmed foe's blows lighter on anyone; a blow on you opens the Answering Cut.</summary>
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            float mult = 1;
            if (Disarmed(enemy)) mult *= DisarmScale(enemy.Boss, R("sn-heavy-hand"));
            if (victim == s.Player)
            {
                if (Now < vowUntil) mult *= 1 - VowCut;
                if (Now < turningUntil && R("ww-steady-feet") > 0) mult *= 1 - SteadyFeetCut;
                if (raw > 0) lastStruckAt = Now;
            }
            return victim.GetComponent<Combatant>().Damage(Mathf.RoundToInt(raw * mult));
        }

        // ---------- per frame / lifecycle ----------
        public override void Tick(bool inCombat)
        {
            periodic.Tick(Now);
            var pool = Pool;
            if (pool != null)
            {
                // Vital-Aether is a fight's own: nothing when the kit wakes, drained away six seconds after the last blow (as the Nerve).
                if (pool != wokePool) { wokePool = pool; pool.SetCurrent(0); fightAt = Now; }
                if (inCombat || s.FightingTarget) { fightAt = Now; drain = 0; }
                else if (Now - fightAt > 6 && pool.Current > 0)
                {
                    drain += Time.deltaTime * 10; int d = (int)drain;
                    if (d > 0) { pool.Change(-Mathf.Min(d, pool.Current)); drain -= d; }
                }
                // The blood burns: above 80 (Burning Blood) and under the Blood-Vow, a little health each second, never under a tenth.
                float burn = (pool.Current > BurningAt ? BurningDrain : 0) + (Now < bloodUntil ? BloodDrain : 0);
                var hp = s.Player.Health.Pool;
                if (burn > 0 && s.Player.IsAlive && hp.Ratio > BurningFloor)
                {
                    burnCarry += burn * hp.Max * Time.deltaTime; int b = (int)burnCarry;
                    if (b > 0) { burnCarry -= b; hp.Change(-Mathf.Min(b, Mathf.Max(0, hp.Current - Mathf.CeilToInt(hp.Max * BurningFloor)))); }
                }
            }
            for (int i = afterimages.Count - 1; i >= 0; i--)
            {
                var (e, at, blow) = afterimages[i];
                if (e == null || e.actor == null || !e.actor.IsAlive) { afterimages.RemoveAt(i); continue; }
                if (Now < at) continue;
                afterimages.RemoveAt(i); Cut(e, blow); s.FloatText(e.transform.position + Vector3.up * .6f, "Afterimage", Gold);
            }
            if (disarmed.Count > 0) foreach (var kv in disarmed.ToList()) if (kv.Key == null || !kv.Key.actor.IsAlive || Now >= kv.Value) disarmed.Remove(kv.Key);
        }
        public override bool RestingResourceRegen { get { return false; } }
        public override void ResetState()
        {
            periodic.Clear(); afterimages.Clear(); disarmed.Clear(); wokePool = null; drain = 0; burnCarry = 0;
            vowUntil = bloodUntil = turningUntil = glowUntil = nextRingOfGold = 0; lastStruckAt = -100;
        }
        public override string StatusLine
        {
            get
            {
                var parts = new List<string>();
                if (Aether > BurningAt) parts.Add("Burning Blood");
                if (Now < bloodUntil) parts.Add("Blood-Vow " + (bloodUntil - Now).ToString("0") + "s");
                if (Now < vowUntil) parts.Add("Iron Vow " + (vowUntil - Now).ToString("0") + "s");
                if (Now < turningUntil) parts.Add("Turning Steel " + (turningUntil - Now).ToString("0") + "s");
                if (Now < glowUntil) parts.Add("Afterglow");
                return parts.Count == 0 ? null : string.Join("  ·  ", parts);
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            if (enemy == null) return null;
            var parts = new List<string>(); var held = enemy.ControlLabel; if (held != null) parts.Add(held);
            if (enemy.Rooted) parts.Add("Stopped " + enemy.RootRemaining.ToString("0.0") + "s");
            if (Disarmed(enemy)) parts.Add("Disarmed");
            return parts.Count == 0 ? null : string.Join("  ·  ", parts);
        }
    }
}
