using UnityEngine;
using Crulanda.Data;
using Crulanda.Abilities;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    [System.Serializable]
    public class AbilitySpec : AbilityDefinition { }

    /// <summary>A selectable class: profile plus its talent tree JSON (same format as the design calculator).</summary>
    [System.Serializable]
    public sealed class PlayableClass
    {
        public ClassDefinition definition = new ClassDefinition();
        public TextAsset talentTree;
    }

    [CreateAssetMenu(menuName = "Crulanda/Encounter Content")]
    public sealed class EncounterContent : ScriptableObject
    {
        public ActorArchetypeDefinition player;
        public ActorArchetypeDefinition healer;
        public ActorArchetypeDefinition enemy;
        public AbilitySpec[] abilities;
        public ClassDefinition playerClass = new ClassDefinition();
        [Tooltip("Class talent tree JSON, same format as the design calculator. Only nodes flagged impl are playable.")]
        public TextAsset talentTree;
        [Tooltip("Further selectable classes. playerClass/talentTree above remain the default (Warrior) for older saves.")]
        public PlayableClass[] additionalClasses = new PlayableClass[0];
        [Tooltip("Quest content JSON (factions, items, documents, quests). See Docs/QUEST_DESIGN.md.")]
        public TextAsset[] questFiles = new TextAsset[0];
        [Tooltip("Item content JSON (items, merchants, loot tables). Registered by Crulanda > World > Build Oakhaven.")]
        public TextAsset[] itemFiles = new TextAsset[0];
        [Tooltip("Trades JSON (professions, node kinds, recipes). Registered by Crulanda > World > Build Oakhaven.")]
        public TextAsset[] professionFiles = new TextAsset[0];
        public PlayableClass FindClass(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (playerClass != null && playerClass.id == id) return new PlayableClass { definition = playerClass, talentTree = talentTree };
            return additionalClasses == null ? null : System.Array.Find(additionalClasses, c => c != null && c.definition != null && c.definition.id == id);
        }
        public DerivedStatRules playerStats = new DerivedStatRules();
        public StatusEffectDefinition[] statuses = {
            new StatusEffectDefinition { id = "status.guard", name = "Guard", duration = 5, incomingDamageMultiplier = .4f }
        };
        public StatusEffectDefinition FindStatus(string id) { return System.Array.Find(statuses, s => s != null && s.id == id); }
        public AbilitySpec healingAbility = new AbilitySpec {
            id = "ability.restorative_weave", name = "Restorative weave", effect = AbilityEffect.Heal,
            power = 42, cost = 18, cooldown = 3.5f, castTime = 1.5f, globalCooldown = 1.5f, range = 13
        };
        public AbilitySpec boltAbility = new AbilitySpec {
            id = "ability.light_bolt", name = "Light bolt", effect = AbilityEffect.Damage,
            power = 7, cost = 0, cooldown = 4, castTime = 0, globalCooldown = 1.5f, range = 12
        };
        [Header("Combat pacing")]
        [Min(.1f)] public float playerSwingInterval = 2.6f;
        [Min(.1f)] public float enemySwingInterval = 2.6f;
        [Min(1)] public int veteranHealth = 340;
        public string itemId = "item.training_blade";
        public string itemName = "Tempered Trailblade";
        public int weaponBonus = 7;
    }
}

