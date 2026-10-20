using System.Collections.Generic;
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
        /// <summary>Slots that sit on a small bar of their own, not the chosen ten (the Druid's four forms, Shift+1-4; Docs/ACTION_BAR.md).</summary>
        public virtual int[] StanceSlots { get { return System.Array.Empty<int>(); } }
        /// <summary>The key the chosen bar keeps for a slot: its ability's id, or the kit's own key where the slot's ability changes (the
        /// Druid's form abilities).</summary>
        public virtual string BookKey(int slot) { var a = ActionAt(slot); return a != null ? a.id : null; }

        public virtual void ApplyStats() { }
        /// <summary>Data-only reset for respawn/load, when the previous actors are being destroyed.</summary>
        public virtual void ResetState() { }
        /// <summary>Respec: drop every temporary benefit so talents never leave effects behind.</summary>
        public virtual void ClearTransient() { ResetState(); }
        public virtual void Tick(bool inCombat) { }

        public virtual bool MeleeAutoAttacks { get { return true; } }
        /// <summary>Whether the class's resource fills on its own out of combat (the class's restingRegen). The Scoundrel's Nerve does
        /// not: it is built by fighting and drains after (Chris, 2026-10-15: "never reset to 0 and should not fill up until i start fighting").</summary>
        public virtual bool RestingResourceRegen { get { return true; } }
        /// <summary>A bow's auto-shot (the Ranger): automatic attacks at <see cref="AutoAttackRange"/> instead of in melee.</summary>
        public virtual bool RangedAutoAttacks { get { return false; } }
        public virtual float AutoAttackRange { get { return 3.2f; } }
        public virtual float MoveSpeedMultiplier { get { return 1; } }
        public virtual float SwingInterval(float baseInterval) { return baseInterval; }
        public virtual void OnAutoHit() { }
        /// <summary>The scale on a weapon swing, given the build's scale on your blows (EncounterSession.PlayerDamageScale). The same for
        /// every class but the Shaman, whose build scales were tuned on its spells alone (ShamanKit, 2026-10-17).</summary>
        public virtual float SwingDamageScale(float buildScale) { return buildScale; }
        /// <summary>A foe that counts as your kill (yours or your party's) has just fallen (Catch Your Breath, Crowd Pleaser).</summary>
        public virtual void OnFoeDown(EncounterEnemy enemy) { }
        public virtual int ResolveEnemyHit(EncounterEnemy enemy, Actor victim, int raw)
        { return victim == null || !victim.IsAlive ? 0 : victim.GetComponent<Combatant>().Damage(raw); }
        public virtual float PartyDamageMultiplier(EncounterEnemy enemy) { return 1; }
        /// <summary>An ability whose blow the kit shows itself (the Ranger's spear work: a thrust, not the bow's release), so starting it
        /// shows nothing of its own.</summary>
        public virtual bool OwnBlow(AbilityDefinition a) { return false; }
        public virtual float CompanionHaste { get { return 0; } }
        /// <summary>Metres off the range at which mobs notice you (the Archivist's Quiet Step).</summary>
        public virtual float NoticeShrink { get { return 0; } }
        /// <summary>Extra HUD line under the resource bar (e.g. weapon pressure, current form pool).</summary>
        public virtual string StatusLine { get { return null; } }
        /// <summary>The kit's own buffs on the whole party, with the seconds left (0: for as long as it is kept up): the buff row by the
        /// party frames (Chris, 2026-10-14: "we need a buff ui so i know what buff/songs are on the party").</summary>
        public virtual IEnumerable<(string name, float remaining)> PartyBuffs() { yield break; }
        /// <summary>The class's standing party buff, laid on the party at a buff-up before a boss (/buff, the run bot; a sim of the class lays
        /// the same: BossBuffs, ClassBuffs.For). Half an hour, a small share of one stat, named in the class's voice; GAME-ONLY.</summary>
        public virtual ClassBuff PartyBuff { get { return null; } }
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
                case "class.archivist": return new ArchivistKit(session, definition, catalog);
                case "class.saltmender": return new SaltMenderKit(session, definition, catalog);   // Phase 9.2, the first class from the canon (2026-10-11)
                case "class.cleric": return new ClericKit(session, definition, catalog);
                case "class.scoundrel": return new ScoundrelKit(session, definition, catalog);   // 2026-10-14, Docs/CLASS_SCOUNDREL.md
                case "class.shaman": return new ShamanKit(session, definition, catalog);   // orc-only (Races), 2026-10-12   // 2026-10-12, Docs/CLASS_CLERIC_SHAMAN.md
                default: throw new ArgumentException("No class kit for '" + classId + "'.");
            }
        }
    }
}
