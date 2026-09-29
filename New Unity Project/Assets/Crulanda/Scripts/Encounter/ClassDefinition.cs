using System;
using System.Collections.Generic;
using Crulanda.Abilities;
using Crulanda.Combat;
using Crulanda.Core;

namespace Crulanda.Encounter
{
    public enum ClassRole { Tank, Healer, Ranged, Caster, Hybrid }
    [Serializable]
    public sealed class ClassAbilityUnlock
    {
        public string abilityId;
        public int level = 1;
        /// <summary>Optional talent that grants this action; empty means the class always owns it.</summary>
        public string talentId;
    }
    /// <summary>Provisional game-only class mechanics; action-bar order follows the unlock table.</summary>
    [Serializable]
    public sealed class ClassDefinition
    {
        public string id = "class.warrior";
        public string displayName = "Warrior";
        public ClassRole role = ClassRole.Tank;
        public ResourceKind resource = ResourceKind.Vigor;
        public int maxResource = 100;
        public int combatRegen = 5;
        public int restingRegen = 12;
        public DerivedStatRules stats = new DerivedStatRules();
        public ClassAbilityUnlock[] unlocks = {
            new ClassAbilityUnlock { abilityId = "ability.strike" },
            new ClassAbilityUnlock { abilityId = "ability.challenge" },
            new ClassAbilityUnlock { abilityId = "ability.guard" }
        };
    }
    /// <summary>Validated class ownership and ordered abilities. Reject invalid data before spawning actors.</summary>
    public sealed class ClassLoadout
    {
        readonly ClassDefinition definition;
        readonly AbilityDefinition[] actions;
        public int Count { get { return actions.Length; } }
        public ClassLoadout(ClassDefinition definition, IEnumerable<AbilityDefinition> catalog)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || string.IsNullOrWhiteSpace(definition.displayName) ||
                definition.stats == null || !Enum.IsDefined(typeof(ClassRole), definition.role) ||
                !Enum.IsDefined(typeof(ResourceKind), definition.resource) || definition.maxResource < 0 ||
                definition.combatRegen < 0 || definition.restingRegen < 0 || definition.unlocks == null ||
                definition.unlocks.Length == 0 || definition.unlocks.Length > 8 || catalog == null)
                throw new ArgumentException("Invalid class profile.");
            this.definition = definition;
            var byId = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
            foreach (var ability in catalog)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.id) || byId.ContainsKey(ability.id))
                    throw new ArgumentException("Ability catalog contains a missing or duplicate identity.");
                byId.Add(ability.id, ability);
            }
            var owned = new HashSet<string>(StringComparer.Ordinal);
            actions = new AbilityDefinition[definition.unlocks.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                var unlock = definition.unlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.abilityId) || unlock.level < 1 || unlock.level > 10 ||
                    !owned.Add(unlock.abilityId) || !byId.TryGetValue(unlock.abilityId, out actions[i]))
                    throw new ArgumentException("Invalid, duplicate, or unresolved class unlock.");
            }
        }
        public AbilityDefinition At(int slot) { return slot >= 0 && slot < actions.Length ? actions[slot] : null; }
        public int UnlockLevel(int slot) { return slot >= 0 && slot < actions.Length ? definition.unlocks[slot].level : int.MaxValue; }
        public string RequiredTalent(int slot) { return slot >= 0 && slot < actions.Length && !string.IsNullOrEmpty(definition.unlocks[slot].talentId) ? definition.unlocks[slot].talentId : null; }
        public bool CanUse(int slot, int level) { return At(slot) != null && level >= UnlockLevel(slot); }
        public bool Owns(string id, int level)
        {
            for (int i = 0; i < actions.Length; i++) if (actions[i].id == id && CanUse(i, level)) return true;
            return false;
        }
    }
}
