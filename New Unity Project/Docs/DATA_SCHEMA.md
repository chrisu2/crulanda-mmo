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


## Items (`EncounterContent/Items/*.json`, `ItemDatabase`)
One file may hold `items`, `vendors` and `loot`. Registered on `Encounter.asset` (`itemFiles`) by Crulanda > World > Build Oakhaven.

`ItemDef` fields: id, name, kind, slot, description, canonStatus, quality (0 poor to 4 epic), level, value (sell price; vendors
charge 4x), stack, armor, stamina, strength, agility, intellect, spirit, weaponDamage, heal, food, and for the trades:

| Field | On | Meaning |
|---|---|---|
| `kind: "material"` | ore, logs, herbs, charcoal, flour, salt, vials | Something gathered or refined that recipes use. Quality 1, stack 20. Using it from the bags says "A crafting material. Press K." "Sell junk" never takes it (`Inventory.IsJunk`: kind junk, or quality 0). |
| `kind: "tool"` | `tool.pick`, `tool.hatchet` | Used once from the bags: teaches the trade in `teaches` at skill 1 and is used up. A second one is refused and kept. Stack 1. |
| `teaches` | tools | A profession id. `ProfessionDatabase.Parse` refuses a tool that teaches an unknown trade. |
| `trade` | materials | The village stock key a sale of it feeds: `forge.ore`, `forge.wood`, `stall.herbs` (used from the gathering step on). |
| `pouch` | materials | The class of trade bag that holds it: `ore`, `timber`, `herb`, `larder` (used from the bags step on). |

Kinds are `gear`, `junk`, `consumable`, `material`, `tool`. `VendorDef`: `npc` or `role`, `items`, `gearForZone`. A role entry leaves
`npc` out (never `""`). Vendors never sell ore, logs or herbs: those come from the world only.

## Trades (`EncounterContent/Professions/*.json`, `ProfessionDatabase`)
Registered on `Encounter.asset` (`professionFiles`) by the same menu step, which logs "Registered N profession files.". Read
against the item database; every problem in a file is reported in one `ArgumentException`, the session logs "Profession content
invalid" and runs without the trades (items are unaffected).

```json
{
  "craftSlots": 2,
  "professions": [
    { "id": "mining", "name": "Mining", "kind": "gather", "tool": "tool.pick", "verb": "Mine",
      "taught": "You hang the pick at your belt. You can now mine.", "description": "...", "canonStatus": "GAME-ONLY" },
    { "id": "cooking", "name": "Cooking", "kind": "free", "station": "fire" },
    { "id": "blacksmithing", "name": "Blacksmithing", "kind": "craft", "station": "forge", "trainerRole": "blacksmith" }
  ],
  "nodes": [
    { "id": "node.copper", "name": "Copper seam", "profession": "mining", "skill": 1, "item": "mat.copper_ore", "min": 1, "max": 3,
      "respawn": 180, "seconds": 2, "look": "ore", "variant": 0, "prompt": "Mine the copper seam" }
  ],
  "recipes": [
    { "id": "recipe.charcoal_oak", "name": "Charcoal", "profession": "woodcutting", "skill": 1, "station": "forge|fire",
      "inputs": [ { "item": "mat.oak_log", "count": 1 } ], "output": "mat.charcoal", "count": 1 }
  ]
}
```

- `ProfessionDef`: `kind` is `gather` (free to everyone, no limit), `free` (everyone has it from the start) or `craft` (at most
  `craftSlots` at once). `tool` is an item of kind `tool` whose `teaches` is this id; a `gather` trade with no tool is known from the
  start. `station` is `forge`, `bench` or `fire`. `taught` is the line said when the tool is first used; `description` is the
  trade's page in the Trades window (K).
- `NodeDef`: a kind of thing worked in the world. `profession` must be a `gather` trade, `item` a known item, `skill` 1-100 (the
  skill at which it comes easily: 1, 20, 40, 60, 80 by zone), `min`-`max` the yield, `respawn` and `seconds` above zero. `look` and
  `variant` choose the prop (`ore`, `ore_rich`, `windfall`, `herb`). Zones place nodes from the gathering step on.
- `RecipeDef`: `profession`, `skill` 1-100, `station` (alternatives joined with `|`), at least one input, `output`, `count`
  (default 1). The file has none yet; they arrive with the stations step.
- Skill is 1-100 (`ProfessionDatabase.MaxSkill`). What a character has learned is saved as `EncounterProgress.professions`
  (save format 8, see SAVE_FORMAT.md) and handled by `ProfessionLog`.

## Gear looks (loot steps A1 and A2)
What worn gear looks like. Data: `Assets/Crulanda/Resources/Gear/looks.json`, loaded with `Resources.Load` (no registration in `Encounter.asset`; it must not go in `EncounterContent/Items`, where every JSON is read as an item file). Code: `GearLooks` (resolver), `GearMeshes`, `GearMats`, `ActorVisual.Gear.cs`, `ActorVisual.GearWeapons.cs` and `ActorVisual.GearArmor.cs`, `GearBinder`. Nothing about looks is saved: a look is worked out from the item id and its `ItemDef` each time.
- A look string is `family[:variant]/palette[+glow]`, for example `shield.round:hide/sandthrone` or `knife:glass/pale+glow`. No variant means the family's first; `+glow` lights one accent whatever the quality. `GearLooks.Families` lists the 54 families and 146 variants (loot `DESIGN.md` 2.3); the first `gen` variants of a family may go to generated gear, the rest are for named items.
- `palettes`: id and seven `#RRGGBB` colours (cloth, cloth2, leather, metal, trim, wood, glow).
- `materials`: a generated name's first word (Homespun ... Sap-steeped): its palette, a `shade` (0 as is, 1 darker and cooler, 2 lighter and warmer) and a `detail` (a small mark: a ring on a grip or haft, a mark on a helm's band, a belt or a pauldron: rivets, band, cord, bone, ember, leaf...; a Riveted jerkin is studded all over, the brigandine look).
- `words`: a generated name's piece word in a slot (Blade, Hatchet, Cudgel, Buckler, Shield, Lantern, Cap ... Torc): one family, or five by level band (1-2, 3-5, 6-8, 9-10, 11-13). Generated variant = (seed / 7) % gen. If the Materials or Pieces words in `Items.cs` change, change this file in the same commit (`GearLookTests` fails otherwise).
- `fallbacks`: each slot's family for anything else (in the oakhaven palette). Nothing renders as missing.
- `tints`: an id prefix whose items have their metal replaced (the Blacksmith's metal tiers, `craft.copper_` ... `craft.heartwood_`).
- `looks`: items with a look of their own: the 21 crafted pieces and the 12 named items already in the game. The loot database (step A3) registers the rest with `GearLooks.Register`.
- Quality: poor is faded with a rust mark and no trim; common has plain trim; uncommon the palette's trim and one extra part; rare polished trim and one glowing accent; epic gold-toned trim and two accents that pulse (`GearGlow`, 0.6 Hz, through a property block). No lights on gear.
- Tier: the item's level band (generated gear: its level; named and crafted: required level + 1) is the look's `tier`, 0 to 4. Armour grows with it: boiled leather becomes plate, then rims, rivets, second lames, ridges and combs, then spikes, flutes and wider flares. Weapons ignore it.
- Shown: every slot. Weapons and shields in the hands (slung on the back while swimming); armour on the body, with limb pieces on roots under the arm and leg pivots (`Gear Hands (Arm R)`), so it swings and poses with them. Parts of one material on one limb are joined into one mesh (`GearMeshes.Join`), so a full kit stays under 70 parts. Armour replaces what the bare body shows: chest pieces recolour the torso, chest and shoulders (and the sleeves, by family) and take the belt's place; legs and feet recolour the breeches and boots; hoods, coifs, barbutes and masks hide the hair, caps, kettle hats and the head-wrap tuck it under the brim; neck pieces lie on the shoulders or on a chest shell's collar. Everything is given back when the piece comes off.
- Empty slots show empty: once gear drives a figure the Warrior's sword, shield and pads go; the Druid keeps her staff while the main hand is empty and her hood until a head piece is worn, and her cloak hangs further back over chest armour. Her forms still tint her own cloth wherever armour leaves it showing. A session without an item database keeps the class kit. Enemies, villagers and Mira are never gear-driven.
