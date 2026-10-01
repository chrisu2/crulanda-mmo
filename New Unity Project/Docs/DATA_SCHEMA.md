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

## Gear looks (loot step A1)
What worn gear looks like. Data: `Assets/Crulanda/Resources/Gear/looks.json`, loaded with `Resources.Load` (no registration in `Encounter.asset`; it must not go in `EncounterContent/Items`, where every JSON is read as an item file). Code: `GearLooks` (resolver), `GearMeshes`, `GearMats`, `ActorVisual.Gear.cs` and `ActorVisual.GearWeapons.cs`, `GearBinder`. Nothing about looks is saved: a look is worked out from the item id and its `ItemDef` each time.
- A look string is `family[:variant]/palette[+glow]`, for example `shield.round:hide/sandthrone` or `knife:glass/pale+glow`. No variant means the family's first; `+glow` lights one accent whatever the quality. `GearLooks.Families` lists the 54 families and 146 variants (loot `DESIGN.md` 2.3); the first `gen` variants of a family may go to generated gear, the rest are for named items.
- `palettes`: id and seven `#RRGGBB` colours (cloth, cloth2, leather, metal, trim, wood, glow).
- `materials`: a generated name's first word (Homespun ... Sap-steeped): its palette, a `shade` (0 as is, 1 darker and cooler, 2 lighter and warmer) and a `detail` (a small ring on the grip or haft: rivets, band, cord, bone, ember, leaf...).
- `words`: a generated name's piece word in a slot (Blade, Hatchet, Cudgel, Buckler, Shield, Lantern, Cap ... Torc): one family, or five by level band (1-2, 3-5, 6-8, 9-10, 11-13). Generated variant = (seed / 7) % gen. If the Materials or Pieces words in `Items.cs` change, change this file in the same commit (`GearLookTests` fails otherwise).
- `fallbacks`: each slot's family for anything else (in the oakhaven palette). Nothing renders as missing.
- `tints`: an id prefix whose items have their metal replaced (the Blacksmith's metal tiers, `craft.copper_` ... `craft.heartwood_`).
- `looks`: items with a look of their own: the 21 crafted pieces and the 12 named items already in the game. The loot database (step A3) registers the rest with `GearLooks.Register`.
- Quality: poor is faded with a rust mark and no trim; common has plain trim; uncommon the palette's trim and one extra part; rare polished trim and one glowing accent; epic gold-toned trim and two accents that pulse (`GearGlow`, 0.6 Hz, through a property block). No lights on gear.
- Shown so far: main hand and off hand. Empty slots show empty: once gear drives a figure the Warrior's sword, shield and pads go; the Druid keeps her staff while the main hand is empty and her hood until head pieces show (step A2). A session without an item database keeps the class kit.
