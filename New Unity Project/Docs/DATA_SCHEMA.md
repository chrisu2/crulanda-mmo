# Data Schema (Phase 0)

## DefinitionBase (ScriptableObject)
| Field | Type | Notes |
|---|---|---|
| id | ContentId | required, unique, lowercase `a-z0-9_-.`, starts with a letter, max 64, permanent once shipped |
| displayName | string | falls back to asset name |
| description | string | |
| canonStatus | CanonStatus | Provisional (default) / GameOnly / CanonExpanded / Canon. Never promote silently (brief s.64) |
| tags | TagSet | validated against `GameTags` |

## ActorArchetypeDefinition : DefinitionBase
level (>=1), disposition, classification, resourceKind, baseStats[StatType,value].
`MaxHealth` and `MaxPower` are plain stats for now; formulas deriving them come with the combat system.

## ContentDatabase
Explicit list of all definitions. `BuildRegistry()` -> `ContentRegistry` (duplicate/invalid ids rejected and logged).

## Conventions
Enum values are explicit and append-only (`StatType`, `LogCategory`) because they serialize into assets/saves.
Bulk content later: CSV/JSON -> importer editor tools generating definitions (brief s.55). Not built yet.

## Ability definitions and runtime
AbilityDefinition is a serializable record embedded in EncounterContent. AbilitySpec is the encounter alias.
Fields: id, name, description, effect, power, cost, cooldown, range, castTime, globalCooldown, duration.
Stable AbilityEffect values: Damage=0, Taunt=1, Guard=2, Heal=3.
Player records: ability.strike, ability.challenge, ability.guard.
Companion records: ability.restorative_weave, ability.light_bolt.
Per-actor AbilityRuntime holds transient timers/cast callbacks; never serialized in content assets.
Guard is deliberately off the global cooldown; offensive abilities use a 1.5-second global cooldown.
Healing costs/cooldowns commit at cast start; interruption cancels the effect without refunding resources.

## Shared combat rules
DerivedStatRules defines health/attack power per level and coefficients for Stamina/Strength/Intellect/Agility.
DerivedStatsController reacts to primary-attribute changes, equipment changes, actor level changes and rebuilds.
StatusEffectDefinition fields: id, name, duration, incomingDamageMultiplier, modifiers[stat,operation,value].
StatusModifierDefinition uses existing stable StatType and ModifierOp values.
AbilityEffect.ApplyStatus=4 is append-only; Guard=2 remains a legacy enum value. AbilityDefinition.statusId
selects a status from the encounter's data asset. The guard status uses a 0.4 incoming damage multiplier.

## Class/build foundation
EncounterContent.playerClass is now authoritative for player derived stats, resource and action ordering. The legacy playerStats field is retained for asset compatibility; its current authored values were copied into playerClass.stats. Unlock entries resolve abilityId plus level (1–10). Current saves still imply the single Warrior reference class; no selectable identity or allocated talents are persisted yet. A versioned migration is required before those features ship.

