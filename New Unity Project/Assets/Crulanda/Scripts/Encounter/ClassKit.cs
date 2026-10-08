using System;
using Crulanda.Abilities;
using Crulanda.Combat;
using Crulanda.Gameplay;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Everything class-specific about the player: action bar, ability effects, talent effects and transient
    /// combat state. EncounterSession, enemies, the companion and the HUD talk only to this base class, so adding
    /// a class means adding a kit and its content, not editing shared systems. Nothing in a kit is saved.
    /// </summary>
    public abstract class ClassKit
    {
        protected readonly EncounterSession s;
        protected ClassKit(EncounterSession session) { s = session ?? throw new ArgumentNullException(nameof(session)); }

        /// <summary>Talent ids with effect code in this kit. TalentTree refuses "impl" nodes missing from this list.</summary>
        public abstract string[] ImplementedTalents { get; }
        public abstract int ActionCount { get; }
        public abstract AbilityDefinition ActionAt(int slot);
        /// <summary>Why a slot cannot be used right now ("Level 4", "Talent", "Barkhide"...), or null when usable.</summary>
        public abstract string ActionLockLabel(int slot);
        /// <summary>Runs after the session's generic checks (alive, unpaused, unlocked, off cooldown).</summary>
        public abstract bool Use(int slot);

        public virtual void ApplyStats() { }
        /// <summary>Data-only reset for respawn/load, when the previous actors are being destroyed.</summary>
        public virtual void ResetState() { }
        /// <summary>Respec: drop every temporary benefit so talents never leave effects behind.</summary>
        public virtual void ClearTransient() { ResetState(); }
        public virtual void Tick(bool inCombat) { }

        public virtual bool MeleeAutoAttacks { get { return true; } }
        /// <summary>A bow's auto-shot (the Ranger): automatic attacks at <see cref="AutoAttackRange"/> instead of in melee.</summary>
        public virtual bool RangedAutoAttacks { get { return false; } }
        public virtual float AutoAttackRange { get { return 3.2f; } }
        public virtual float MoveSpeedMultiplier { get { return 1; } }
        public virtual float SwingInterval(float baseInterval) { return baseInterval; }
        public virtual void OnAutoHit() { }
        public virtual int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        { return victim == null || !victim.IsAlive ? 0 : victim.GetComponent<Combatant>().Damage(raw); }
        public virtual float PartyDamageMultiplier(EncounterEnemy enemy) { return 1; }
        public virtual float CompanionHaste { get { return 0; } }
        /// <summary>Extra HUD line under the resource bar (e.g. weapon pressure, current form pool).</summary>
        public virtual string StatusLine { get { return null; } }
        /// <summary>Extra target-frame text (e.g. EXPOSED 2.1s, Rooted).</summary>
        public virtual string TargetStatus(EncounterEnemy enemy) { return null; }

        public static ClassKit Create(string classId, EncounterSession session, ClassDefinition definition, AbilityDefinition[] catalog)
        {
            switch (classId)
            {
                case "class.warrior": return new WarriorKit(session, new ClassLoadout(definition, catalog), new Crulanda.Core.SeededRandom(Environment.TickCount));
                case "class.druid": return new DruidKit(session, definition, catalog);
                case "class.paladin": return new PaladinKit(session, definition, catalog);
                case "class.ranger": return new RangerKit(session, definition, catalog);
                case "class.mage": return new MageKit(session, definition, catalog);
                case "class.rogue": return new RogueKit(session, definition, catalog);
                default: throw new ArgumentException("No class kit for '" + classId + "'.");
            }
        }
    }
}
