using System.Collections.Generic;
using UnityEngine;
using Crulanda.Core;
using Crulanda.Gameplay;
using Crulanda.Combat;
using Crulanda.Abilities;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Warrior talent effects for tiers 0-2 of the data-driven tree (EncounterContent/Talents/warrior.json).
    /// Owns transient combat state only (weapon pressure, Exposed windows, intercept, standard); nothing here is saved.
    /// Shared rule: an Exposed enemy takes +5% damage from the party (Scatter Their Guard adds more).
    /// </summary>
    public sealed class WarriorKit : ClassKit
    {
        public static readonly string[] ImplementedIds = {
            "tk-hardened-grip", "tk-tempered-armor", "tk-timed-guard", "tk-scarred-veteran", "tk-steady-challenge", "tk-intercept",
            "tk-bulwark", "tk-long-reach", "tk-grudge-ledger",
            "dp-read-the-opening", "dp-weapon-pressure", "dp-edge-discipline", "dp-breaching-blow", "dp-finishing-strike", "dp-honed-edge",
            "dp-momentum", "dp-deep-breach", "dp-sweeping-strike",
            "sp-called-cadence", "sp-rally-reserve", "sp-mark-the-gap", "sp-steady-voice", "sp-rallying-challenge", "sp-muster",
            "sp-shared-shelter", "sp-planted-standard", "sp-scatter-their-guard"
        };
        public const int MaxPressure = 5;
        public const float GuardExposeSeconds = 3, BreachExposeSeconds = 4, ExposedBonus = .05f;
        const float PressureDecayDelay = 8, TimedGuardWindow = .5f, GrudgeMemory = 10, InterceptWindow = 3;

        readonly ClassLoadout loadout;
        readonly IRandomSource random;
        readonly Dictionary<EncounterEnemy, float> exposedUntil = new Dictionary<EncounterEnemy, float>();
        readonly Dictionary<EncounterEnemy, float> challengedAt = new Dictionary<EncounterEnemy, float>();
        readonly object talentSource = new object();
        float pressureKeptUntil, guardRaisedAt = -99, interceptUntil, cadenceUntil, companionCadenceUntil, nextStandardTick;
        public int Pressure { get; private set; }
        public Vector3 StandardPoint { get; private set; }
        public float StandardUntil { get; private set; }

        public WarriorKit(EncounterSession session, ClassLoadout loadout, IRandomSource random) : base(session)
        { this.loadout = loadout ?? throw new System.ArgumentNullException(nameof(loadout)); this.random = random; }
        public override string[] ImplementedTalents { get { return ImplementedIds; } }
        int R(string id) { return s.TalentRank(id); }

        // ---------- action bar ----------
        public override int ActionCount { get { return loadout.Count; } }
        public override AbilityDefinition ActionAt(int slot) { return loadout.At(slot); }
        public override string ActionLockLabel(int slot)
        {
            if (s.Player == null || loadout.At(slot) == null) return "Unavailable";
            if (!loadout.CanUse(slot, s.Player.Level)) return "Level " + loadout.UnlockLevel(slot);
            var talent = loadout.RequiredTalent(slot);
            return talent != null && R(talent) == 0 ? "Talent" : null;
        }
        public override bool Use(int slot)
        {
            var a = loadout.At(slot);
            bool needsEnemy = a.effect == AbilityEffect.Damage || a.effect == AbilityEffect.Taunt || a.effect == AbilityEffect.Breach;
            if (needsEnemy && !s.RequireEnemyInRange(a.range)) return false;
            if (a.effect == AbilityEffect.Breach && Pressure < 1) { s.Message("Build weapon pressure with Strike first."); return false; }
            if (a.effect == AbilityEffect.Intercept) { var why = InterceptBlocker(); if (why != null) { s.Message(why); return false; } }
            var target = s.Target;
            var status = a.effect == AbilityEffect.Guard || a.effect == AbilityEffect.ApplyStatus ? s.content.FindStatus(a.statusId) : null;
            if ((a.effect == AbilityEffect.Guard || a.effect == AbilityEffect.ApplyStatus) && status == null)
            { s.Message("Ability status definition is missing."); return false; }
            return s.StartAbility(a, () => {
                if (!s.Player.IsAlive) return;
                if (status != null) { OnGuard(status); return; }
                if (a.effect == AbilityEffect.Intercept) { if (InterceptBlocker() == null) Intercept(); return; }
                if (a.effect == AbilityEffect.Rally) { Muster(); return; }
                if (!s.LandsOn(target, a.range)) return;
                s.BeginAutoAttack();
                if (a.effect == AbilityEffect.Damage)
                {
                    int damage = StrikeDamage(target, a.power + s.WeaponDamage);
                    target.Receive(damage, s.Player);
                    AfterStrike(target, damage);
                }
                if (a.effect == AbilityEffect.Breach) Breach(target);
                if (a.effect == AbilityEffect.Taunt)
                {
                    OnChallenge(target); target.threat.Taunt(s.Player.EntityId.Value, Time.time, a.duration);
                    s.Message("Challenge: enemy attention forced for " + a.duration + " seconds.");
                }
            }) == AbilityStartResult.Started;
        }
        public override string StatusLine
        {
            get
            {
                int barrier = PlayerCombat.Barrier;
                return "Weapon pressure  " + EncounterHud.Pips(Pressure, MaxPressure) + (barrier > 0 ? "   Â·   Barrier " + barrier : "");
            }
        }
        public override string TargetStatus(EncounterEnemy enemy)
        {
            float exposed = ExposedRemaining(enemy);
            return exposed > 0 ? "EXPOSED " + exposed.ToString("0.0") + "s" : null;
        }
        float Now { get { return Time.time; } }
        Combatant PlayerCombat { get { return s.Player.GetComponent<Combatant>(); } }
        public float RallyRange(float baseRange) { return baseRange + 2 * R("sp-steady-voice"); }
        public bool CompanionWithin(float range)
        {
            return s.Progress.recruited && s.Companion != null && s.Companion.actor.IsAlive &&
                Vector3.Distance(s.Player.transform.position, s.Companion.transform.position) <= range;
        }

        // ---------- passive stats ----------
        public override void ApplyStats()
        {
            s.Player.Stats.RemoveModifiersFromSource(talentSource);
            s.Player.Stats.AddModifiers(new[] {
                new StatModifier(StatType.Armor, ModifierOp.Flat, 8 * R("tk-tempered-armor"), talentSource),
                new StatModifier(StatType.MaxHealth, ModifierOp.PercentAdd, .03f * R("tk-scarred-veteran"), talentSource),
                new StatModifier(StatType.AttackPower, ModifierOp.Flat, 2 * R("dp-weapon-pressure"), talentSource),
                new StatModifier(StatType.MaxPower, ModifierOp.Flat, 10 * R("sp-rally-reserve"), talentSource)
            });
        }
        /// <summary>Respec/load: drop every temporary benefit so talents never leave effects behind.</summary>
        public override void ClearTransient()
        {
            ResetState();
            if (s.Player != null) { PlayerCombat.Statuses.Remove("status.guard"); PlayerCombat.ClearBarrier(); }
            if (s.Companion != null)
            {
                var c = s.Companion.actor.GetComponent<Combatant>();
                c.Statuses.Remove("status.shared_shelter"); c.ClearBarrier();
            }
        }

        /// <summary>Data-only reset for respawn/load, when the previous actors are being destroyed.</summary>
        public override void ResetState()
        {
            Pressure = 0; exposedUntil.Clear(); challengedAt.Clear();
            guardRaisedAt = -99; interceptUntil = cadenceUntil = companionCadenceUntil = StandardUntil = 0;
        }

        // ---------- Exposed ----------
        public bool IsExposed(EncounterEnemy e) { return e != null && exposedUntil.TryGetValue(e, out var t) && Now < t; }
        public float ExposedRemaining(EncounterEnemy e) { return e != null && exposedUntil.TryGetValue(e, out var t) ? Mathf.Max(0, t - Now) : 0; }
        public void Expose(EncounterEnemy e, float seconds)
        {
            if (e == null || !e.actor.IsAlive || seconds <= 0) return;
            float until = Now + seconds + .5f * R("dp-read-the-opening");
            if (!exposedUntil.TryGetValue(e, out var current) || current < until) exposedUntil[e] = until;
        }
        public override float PartyDamageMultiplier(EncounterEnemy e)
        { return IsExposed(e) ? 1 + ExposedBonus + .03f * R("sp-scatter-their-guard") : 1; }

        // ---------- weapon pressure / tempo ----------
        public void GainPressure(int amount)
        {
            if (amount <= 0) return;
            Pressure = Mathf.Min(MaxPressure, Pressure + amount); pressureKeptUntil = Now + PressureDecayDelay;
        }
        public override float SwingInterval(float baseInterval)
        {
            float speed = 1 + .01f * R("dp-momentum") * Pressure + (Now < cadenceUntil ? .04f * R("sp-called-cadence") : 0);
            return baseInterval / speed;
        }
        public override float CompanionHaste { get { return Now < companionCadenceUntil ? .04f * R("sp-called-cadence") : 0; } }
        public override void OnAutoHit()
        {
            int edge = R("dp-edge-discipline");
            if (edge > 0 && random.NextFloat() < .05f * edge) GainPressure(1);
        }

        // ---------- base abilities ----------
        public int StrikeDamage(EncounterEnemy target, int raw)
        {
            int damage = raw + (R("dp-finishing-strike") > 0 && target.actor.Health.Pool.Ratio < .4f ? 8 : 0);
            if (IsExposed(target)) damage = Mathf.RoundToInt(damage * (1 + .04f * R("dp-honed-edge")));
            return damage;
        }
        public void AfterStrike(EncounterEnemy target, int damage)
        {
            GainPressure(1);
            if (R("sp-mark-the-gap") > 0) Expose(target, R("sp-mark-the-gap"));
            if (R("dp-sweeping-strike") > 0)
                foreach (var other in s.Enemies)
                    if (other != target && other.actor.IsAlive && Vector3.Distance(other.transform.position, target.transform.position) <= 4)
                        other.Receive(Mathf.Max(1, damage / 2), s.Player);
        }
        public void OnChallenge(EncounterEnemy target)
        {
            challengedAt[target] = Now;
            if (R("tk-steady-challenge") > 0) s.Player.Resource.Pool.Change(10);
            if (R("sp-rallying-challenge") > 0 && CompanionWithin(RallyRange(13))) s.Companion.actor.Resource.Pool.Change(12);
            if (R("sp-called-cadence") > 0)
            {
                cadenceUntil = Now + 6;
                if (CompanionWithin(RallyRange(10))) companionCadenceUntil = Now + 6;
            }
        }
        public float GuardMultiplier(float baseMultiplier) { return R("tk-bulwark") > 0 ? .25f : baseMultiplier; }
        public void OnGuard(StatusEffectDefinition status)
        {
            var applied = status;
            if (R("tk-bulwark") > 0)
                applied = new StatusEffectDefinition { id = status.id, name = status.name, duration = 7, incomingDamageMultiplier = .25f, modifiers = status.modifiers };
            if (PlayerCombat.Apply(applied)) { guardRaisedAt = Now; s.Message(applied.name + " active for " + applied.duration + " seconds."); }
            if (R("sp-shared-shelter") > 0 && CompanionWithin(RallyRange(13)))
            {
                var c = s.Companion.actor.GetComponent<Combatant>();
                c.Apply(new StatusEffectDefinition { id = "status.shared_shelter", name = "Shared shelter", duration = 5, incomingDamageMultiplier = .7f });
                int healed = c.Heal(20);
                s.AddHealingThreat(healed);
                s.FloatText(s.Companion.transform.position, "+" + healed, new Color(.3f, 1, .7f));
            }
        }

        // ---------- talent actions ----------
        public float InterceptRange { get { return 10 + 4 * R("tk-long-reach"); } }
        public string InterceptBlocker()
        {
            if (!s.Progress.recruited || !s.Companion.actor.IsAlive) return "Intercept needs a living, recruited Mira.";
            return CompanionWithin(InterceptRange) ? null : "Mira is beyond Intercept range (" + InterceptRange + "m).";
        }
        public void Intercept()
        {
            var from = s.Player.transform.position; var to = s.Companion.transform.position;
            var offset = from - to; offset.y = 0;
            var landing = to + (offset.sqrMagnitude > .01f ? offset.normalized : Vector3.back) * 1.2f;
            landing.y = from.y;
            s.Player.GetComponent<AdventurerMotor>().Teleport(landing);
            interceptUntil = Now + InterceptWindow;
            s.Message("Intercept: the next blow aimed at Mira within 3 seconds lands on you.");
        }
        public void Breach(EncounterEnemy target)
        {
            int spent = Pressure; Pressure = 0;
            target.Receive(12 * spent, s.Player);
            Expose(target, BreachExposeSeconds);
            if (spent == MaxPressure && R("dp-deep-breach") > 0) target.Stagger(.5f * R("dp-deep-breach"));
            s.Message("Breaching Blow spends " + spent + " pressure. Target Exposed.");
        }
        public void Muster()
        {
            PlayerCombat.AddBarrier(20, 6);
            if (CompanionWithin(RallyRange(12))) s.Companion.actor.GetComponent<Combatant>().AddBarrier(20, 6);
            if (R("sp-planted-standard") > 0)
            {
                StandardPoint = s.Player.transform.position; StandardUntil = Now + 4 * R("sp-planted-standard"); nextStandardTick = Now + 1;
            }
            s.Message("Muster: barriers raised" + (R("sp-planted-standard") > 0 ? " and standard planted." : "."));
        }

        // ---------- incoming blows ----------
        /// <summary>Resolves an enemy blow, applying intercept, grudge, Guard reactions and barriers. Returns health lost.</summary>
        public override int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        {
            if (victim == null || !victim.IsAlive) return 0;
            if (s.Companion != null && victim == s.Companion.actor && Now < interceptUntil && s.Player.IsAlive)
            {
                interceptUntil = 0;
                if (R("tk-long-reach") > 0) enemy.threat.Taunt(s.Player.EntityId.Value, Now, 2 * R("tk-long-reach"));
                bool guarding = PlayerCombat.Statuses.Remaining("status.guard", Now) > 0;
                int redirected = guarding ? raw : Mathf.CeilToInt(raw * GuardMultiplier(.4f));
                s.FloatText(s.Player.transform.position, "Intercepted", new Color(.55f, .8f, 1));
                return HitPlayer(enemy, redirected);
            }
            if (victim == s.Player) return HitPlayer(enemy, raw);
            return victim.GetComponent<Combatant>().Damage(raw);
        }
        int HitPlayer(EncounterEnemy enemy, int raw)
        {
            float mult = 1;
            if (challengedAt.TryGetValue(enemy, out var at) && Now - at <= GrudgeMemory) mult -= .02f * R("tk-grudge-ledger");
            if (PlayerCombat.Statuses.Remaining("status.guard", Now) > 0)
            {
                Expose(enemy, GuardExposeSeconds);
                int vigor = R("tk-hardened-grip");
                if (R("tk-timed-guard") > 0 && Now - guardRaisedAt <= TimedGuardWindow)
                {
                    vigor += 3 * R("tk-timed-guard"); guardRaisedAt = -99;
                    s.FloatText(s.Player.transform.position, "Timed guard", new Color(.95f, .8f, .45f));
                }
                if (vigor > 0) s.Player.Resource.Pool.Change(vigor);
            }
            return PlayerCombat.Damage(Mathf.RoundToInt(raw * mult));
        }

        // ---------- per frame ----------
        public override void Tick(bool inCombat)
        {
            if (Pressure > 0 && !inCombat && Now > pressureKeptUntil) Pressure = 0;
            if (Now < StandardUntil && Now >= nextStandardTick)
            {
                nextStandardTick = Now + 1; float reach = RallyRange(8);
                if (Vector3.Distance(s.Player.transform.position, StandardPoint) <= reach) s.Player.Resource.Pool.Change(2);
                if (s.Progress.recruited && s.Companion.actor.IsAlive && Vector3.Distance(s.Companion.transform.position, StandardPoint) <= reach)
                    s.Companion.actor.Resource.Pool.Change(2);
            }
        }
    }
}
