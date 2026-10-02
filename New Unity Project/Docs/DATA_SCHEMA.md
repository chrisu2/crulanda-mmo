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
| `kind: "material"` | ore, logs, herbs, charcoal, flour, salt, vials, meat | Something gathered or refined that recipes use. Quality 1, stack 20. Using it from the bags says "A crafting material. Press K." "Sell junk" never takes it (`Inventory.IsJunk`: kind junk, or quality 0). |
| `kind: "tool"` | `tool.pick`, `tool.hatchet` | Used once from the bags: teaches the trade in `teaches` at skill 1 and is used up. A second one is refused and kept. Stack 1. |
| `teaches` | tools | A profession id. `ProfessionDatabase.Parse` refuses a tool that teaches an unknown trade. |
| `trade` | materials | The village stock key a sale of it feeds: `forge.ore`, `forge.wood`, `stall.herbs` (used from the gathering step on), `tannery.hides`, `inn.meat` (the inn's pot: the innkeeper and the village talk of "somebody's hunting"). |
| `pouch` | materials, eggs, cheese | The class of trade bag that holds it: `ore`, `timber`, `herb`, `larder`. A picked-up item tops up its stacks first, then fills an empty slot of a worn bag that holds its class, then an ordinary slot. |
| `kind: "bag"` | `bag.simples_wallet`, `bag.log_sling`, `bag.larder_scrip`, `bag.ore_poke` | A trade bag (the leatherworker's). Used once from the bags it is worn for good: it leaves the bags, its id joins the save's `pouches`, and its `slots` are added after the 24 (`Inventory.Wear`). One of each: a second is refused and kept ("You already carry one."). Stack 1. |
| `holds`, `slots` | bags | The class its slots take (a `pouch` value) and how many (1-24). Nothing else goes in ("Only ore, bars and charcoal go in the ore-poke."). `ItemDatabase.Parse` refuses a bag without both. |
| meat | `junk.boar_meat` (Tough boar meat; id, name and value kept from when it was junk), `mat.wolf_haunch`, `mat.hound_flank`, `mat.mossback_chop`, `mat.venison` | Kind `material`, quality 1, stack 20, `trade: "inn.meat"`, `pouch: "larder"`; Cooking's makings (BUILD_PLAN step 10). The boar drops its meat as before (0.5); the wolf, hound, mossboar and stag tables drop theirs (0.4, 0.45, 0.5, 0.5). No vendor sells meat. "Sell junk" keeps it. |
| foods | `food.*` | Kind `consumable`, `food: true`: eaten out of combat, `heal` over 10 s. The ten cooked foods (`food.griddle_bread` ... `food.venison_pie`) heal more than any food a vendor lists at their level and go in no trade bag. |
| hides | `junk.wolf_pelt`, `junk.ash_hide`, `junk.moss_hide`, `junk.dappled_hide` | Kind `material`, quality 1, `trade: "tannery.hides"` (ids, names, values and the stack of 10 kept from when they were junk). "Sell junk" keeps them; using one says "Leather. Maud Tanner in Oakhaven works it." Sold in a village, the tannery and the leatherworker notice. |

Kinds are `gear`, `junk`, `consumable`, `material`, `tool`, `bag`. `VendorDef`: `npc` or `role`, `items`, `gearForZone`. A role entry leaves
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
    { "id": "recipe.charcoal_oak", "name": "Charcoal from oak", "profession": "woodcutting", "skill": 1, "station": "forge|fire",
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
  `variant` choose the prop (`ore`, `ore_rich`, `windfall`, `herb`; variant is the tier, 0-4, and for herbs the `Herb` look, 0-5:
  yarrow, feverfew, comfrey, tarnwort, cinder-thistle, dewfern). Zones place nodes from the gathering step on (below).
- Gathering (`ProfessionLog`): low skill never refuses a node. Under the node's `skill` the work is hard going: twice `seconds`,
  a yield of exactly 1, a skill point every time. Otherwise the yield is `min`-`max`, plus one a quarter of the time from 20 points
  over. A skill point is certain under 20 over the node, half the time under 40 over, never after; skill never passes 100. Mining
  and Woodcutting need their tool at the belt; Herbalism needs nothing. A worked node rests `respawn` seconds (kept in memory by
  zone and place, so a zone hop does not refill it; not saved, so a restart does).

### Nodes in a zone (`Zones/*.json`)
```json
"nodes": [
  { "node": "node.copper", "at": { "x": -37.5, "y": 88.2 } },
  { "node": "node.copper_rich", "at": { "x": -18.5, "y": 111.5 }, "under": true },
  { "node": "node.oak", "at": { "x": -78.5, "y": 104 }, "rotation": 0 },
  { "node": "node.yarrow", "item": "item.yarrow", "at": { "x": -104, "y": 30 } }
],
"props": [
  { "kind": "herb", "name": "Yarrow", "at": { "x": -70, "y": 44 }, "interact": "Gather yarrow", "item": "item.yarrow", "node": "node.yarrow" }
]
```
- `ZoneNode`: `node` names a `NodeDef`; `rotation` its yaw (a seam's rock lies on its +Z side and its ore faces -Z; a windfall's
  stump is at its -X end, its trunk lying along +X); `under: true` stands it on a cave's floor (`Hollow`), its rock turned to the
  nearer wall; `item` an optional quest item it also gives while a quest wants it. Built by `ZoneBuilder.BuildNodes` after the map
  and the secrets, each from a stream of its own, with no colliders: no tree, rock, prop or creek moves and the navmesh is
  unchanged. A node in a trunk, a rock, a road, water, a building, within 5.5 m of a secret or 5.2 m of another node, or whose
  footprint reaches into a camp's spread (radius x 1.42, the square's corners) or a cave's furnishings, is moved clear (up to
  3 m), with a warning when nothing near is clear; a windfall turns in 30 degree steps until it lies clear. Place them clear in
  the data: 3 m is a nudge, not a search.
- `ZoneProp.node`: a herb prop worked as a node (it keeps its `interact` prompt and quest `item`).

### Stations in a zone (`Zones/*.json`, BUILD_PLAN step 9)
```json
"stations": [
  { "kind": "forge", "name": "Oska's bone-anvil", "at": { "x": 56, "y": 22 }, "rotation": 225, "variant": 1, "canonStatus": "GAME-ONLY" },
  { "kind": "bench", "name": "Mother Vane's salt-bench", "at": { "x": 64, "y": -6 }, "rotation": 315, "canonStatus": "GAME-ONLY: ..." },
  { "kind": "fire", "name": "Enclave cookfire", "at": { "x": 60, "y": -18 }, "rotation": 140, "canonStatus": "GAME-ONLY" }
]
```
- A station is where recipes are made: `kind` is `forge`, `bench` or `fire`; `name` is what the Trades window calls it ("Forge:
  Oska's bone-anvil"); `rotation` its yaw (its front, where you stand, faces -Z); `variant` a forge's look (0 a field anvil on an oak
  stump with a pan of coals, 1 on a block of pale stone bound with bone, 2 on a mossed stone with a stone basin of embers). A bench
  is a herbalist's bench with a drying rail; a fire a cookfire in a ring of stones with a pot on a tripod.
- Built by `ZoneBuilder.BuildStations` after the nodes, under "Zone stations", each from a stream of its own keyed on where it
  stands, with no colliders and nothing in the navmesh, so nothing else moves (`NodeStreamTests` builds every zone with and
  without them). Placed in the data clear of roads, water, buildings, nodes, secrets and trunks (`NodePlacementTests`).
- Villagers' workplaces are stations too, with no data: a `forge` prop (forge, at its anvil), an `oven` (fire, at its mouth), a
  `dryhut` (bench, at the bench), a `kitchen` (fire, at the range) and every `inn` (fire, at its hearth, named after the inn).
  `ZoneBuilder.StationKind` maps them; `ZoneBuilder.Stations` lists every station of the zone.
- A recipe can be made within 5 m (ground distance; one in a cave and one out of it never reach each other) of a station of one of
  its kinds. E there (when no one is in reach to talk to, nothing to pick up and no door at hand) reads "Work at the forge", "Work at
  the bench" or "Cook at the fire", and opens the Trades window (K) at the recipes of the trade the station serves (a fire opens on
  Cooking, a forge on Woodcutting's charcoal until Blacksmithing is taken up). Nobody need be there, day or night.
- The zone builder learns what a node is (name, prompt, look) from the component beside it that implements `IZoneNodeKinds`
  (the encounter session, from the trades' content). Without one it builds no nodes and warns.
- `RecipeDef`: `profession`, `skill` 1-100, `station` (alternatives joined with `|`), at least one input, `output`, `count`
  (default 1); an item named on two input lines is summed. The file has the five charcoal recipes (BUILD_PLAN step 9, DESIGN 5.1):
  one log of each tier's wood (oak, black pine, stone-pine, ash-snag, ghost-oak) makes 1 to 5 charcoal at a forge or a fire, at
  Woodcutting 1, 20, 40, 60 and 80. Cooking's ten (BUILD_PLAN step 10, DESIGN 5.4), at a fire, from Cooking 1, which everyone has:
  Griddle bread (1 flour), Boar stew (2 boar meat) at 1; Harrow hearth-cake (flour and an egg, makes 2) at 5; Wolf-haunch skewer
  (2 wolf haunch, a mourner's cap) at 20; Harrow pasty (flour, Harrow cheese, boar meat) at 30; Pass-smoked loin (2 boar meat,
  tarnwort, salt) at 40; Salt-baked hound flank (2 hound flank, salt) at 60; Cinder-crust loaf (2 flour, cinder-thistle, charcoal)
  at 70; Dewfern-glazed mossback chop (2 mossback chop, dewfern) at 80; Shore venison pie (2 venison, flour) at 90. Rules pinned by `ProfessionDataTests`: what a recipe makes (value x count) is worth at most 1.5
  times its inputs; a recipe from vendor goods alone never makes more than they cost to buy; every recipe's station stands somewhere.
- Making (`ProfessionLog.CanCraft`, `Craft`; `EncounterSession.Make`): refused, in this order, without the trade (a gathering
  skill's tool: "You need a woodcutter's hatchet. Merchants sell them."; a craft: "You have not taken up Blacksmithing."), under the
  recipe's skill ("That wants Woodcutting 20."), with no station of its kind within 5 m and on its side of any walls ("You need a forge or a fire nearby."), short
  of an input ("You need Harrow oak log." or "... x2."), or with no room for what it makes ("Your bags are full."); then nothing
  changes. Otherwise the inputs leave the bags, the output goes in (onto its stacks, then a worn trade bag that holds it: charcoal
  goes in the ore-poke), and the skill rises: every time under 10 points over the recipe (orange), half the time under 20 (yellow),
  one in ten under 30 (green), never after (grey); never past 100. Each takes 2 s on the work bar ("You make Charcoal."); Make all
  goes one at a time, stops with the reason when the next cannot be made ("Your bags are full."), and moving, a blow, a fight or
  dying stops the rest. Known recipes are not saved: they follow from the skill.
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
- Shown: every slot. Weapons and shields in the hands (slung on the back while swimming); armour on the body, with limb pieces on roots under the arm and leg pivots (`Gear Hands (Arm R)`), so it swings and poses with them. Parts of one material on one limb are joined into one mesh (`GearMeshes.Join`), so a full kit stays under 70 parts. Armour replaces what the bare body shows: chest pieces recolour the torso, chest and shoulders (and the sleeves, by family) and take the belt's place; legs and feet recolour the breeches and boots; hoods, coifs, barbutes and masks hide the hair, caps, kettle hats and the head-wrap tuck it under a brim that sits above the brows; neck pieces lie on the shoulders, on a chest shell's collar or over a mantle or a hood's cape, and a pendant hangs in front of a tabard or surcoat; boots go under greaves and hide leggings (no folded top); the Druid's cloak, hung back over chest armour, stands in for a mantle's back drape. Everything is given back when the piece comes off.
- Empty slots show empty: once gear drives a figure the Warrior's sword, shield and pads go; the Druid keeps her staff while the main hand is empty and her hood until a head piece is worn, and her cloak hangs further back over chest armour. Her forms still tint her own cloth wherever armour leaves it showing. A session without an item database keeps the class kit. Enemies, villagers and Mira are never gear-driven.

## Loot database (loot step A3: drafted, not yet loaded by the game)
Named gear by zone and boss (loot `DESIGN.md` 3, `ITEMS_V1.md`). Data: `EncounterContent/Loot/loot.oakhaven.json`, `loot.khaven.json`, `loot.peaks.json`, `loot.ashrim.json`, `loot.verdant.json` and `loot.world.json`. Code: `Loot.cs` (`LootDatabase`, `LootContext`, `GearEffects`, `LootJudge`), `LootDraft`. Until step L2 nothing in the running game reads these files: they are not in `Encounter.asset`, and only the tests and the wardrobe capture load them. Step L2 moves the six files (with their `.meta` files) into `EncounterContent/Items`, where the scene builder registers them as item files. Nothing about loot is saved in A3.
- Each file is an ordinary item file (`items`, `vendors`, which `ItemDatabase.Parse` reads) plus `gear`, `drops` and `sets` (which `LootDatabase.Parse` reads from the same texts; JsonUtility ignores fields a class does not have). Every file parses alone beside the core items.
- Ids: `loot.<oak|kha|pea|ash|ver|world>.<snake_name>`, each in its zone's file. Permanent once shipped: saves store ids only, so an item is retired by removing its sources, never renamed or deleted. `Tests/EditMode/LootIdBaseline.txt` lists them; add new ids to it.
- `items`: plain `ItemDef`s, quality 2 to 4, `canonStatus` GAME-ONLY, `description` the line of flavour. `level` is the level needed to wear it; its curve level is `level + 1`, as for generated gear, and armour, weapon damage and value are exactly generated gear's at that level and quality. Stat points: uncommon 4k, rare 5k, a boss's signature rare 6k, epic 6k at epic power (k = max(1, power / 4)); the two luck charms spend fewer.
- `vendors`: `VendorDef` by NPC name (stacks on the role's stock): five merchants sell one named piece each.
- `gear`: one entry per named item: `id`, `look` (a gear look string, registered with `GearLooks.Register` when parsed with the looks), `source`, and optionally `boss`, `unique`, `set`, `effects`, `legacy`. `source` is `boss:<mob>`, `mob:<tag>@<zone>` (tag `any` for any camp of the zone), `quest:<quest id>`, `vendor:<npc>`, `world:<min>-<max>` or `secret:<secret id>`. `loot.world.json` also holds the twelve named items already in the game (`legacy: true`: look, source and set only; their stats are untouched).
- `drops`: lists for camp mobs, matched on any of `zone` (the zone id without `zone.`), `tag`, `mob` (the camp's mob name), `rank` (`elite`, `normal` or absent for both), `levelMin`/`levelMax` (default 1 and 13). `groups`: `chance` (0 to 1), `signature` (a boss's own list: a piece not owned while there is one; all owned, the list pays at 35% of its chance), `lucky` (worn luck raises it), `pity` (an epic certain on that many kills in a row without it while unowned), `pick` (`item`, `weight` default 1).
- `LootDatabase.Roll(context, items, owned, luck, pity, rng)`: the base roll (`ItemDatabase.RollLoot`, unchanged) less anything on the mob's signature list, then each matching group in file order; luck multiplies lucky chances by 1 + luck (luck capped at +30%); an owned epic drops a quarter as often; at most one named item from a normal kill and two from an elite (epics do not count); an elite whose body would hold no gear gets a generated piece at its level (rare 35%, else uncommon); 6% of an elite's generated rares come out epic (same slot, level and seed). `LootContext.From(persistentId, camps, level, elite)` reads the zone, tag and camp from a camp mob's id (`mob.<tag>.<zone>.<camp>.<n>`).
- `sets`: `id`, `name`, `pieces` (one per slot), `bonuses` (`count` rising from 2, `effects`). Two sets: The Deserter King's Due (`set.crowsfoot`) and Vigil of the Root-Mother (`set.vigil`), each with its boss's existing crown.
- Effects (on gear and set bonuses): `kind` `stat` (`stat` a StatType: MaxHealth, MaxPower, AttackPower, Armor or a primary stat; `percent` makes `amount` a percentage), `onKillHeal`, `onKillPower`, `restRegen`, `coins` or `luck` (percent); `text` is the tooltip line. `GearEffects.Compute` totals what is worn and the set bonuses reached, with caps: coins and luck +30%, restRegen +20, onKillHeal 60.
- `LootDatabase.Validate` checks the loot against the zones and quests: every source exists, every elite camp has a signature list, every list names a real zone, tag and mob, and sets and their pieces agree.
- The wardrobe capture reads the drafted files through `Resources/Gear/LootDraft.asset` (a `LootDraft` holding the six TextAssets), which Crulanda > World > Build Oakhaven, and so every player build, writes; in the editor it falls back to the folder.

## Loot on bodies (loot step L1)
How loot reaches the bags (loot `DESIGN.md` 4, features 1-5). Code: `EncounterSession.Loot.cs` (a partial of `EncounterSession`), `LootBeacon.cs`, `EncounterHud.Loot.cs`; `LootJudge` in `Loot.cs`. No data file and nothing saved: a body's loot lives on the `EncounterEnemy` (`Drops`, `Coins`) until it is emptied or the mob respawns, and the open window is not saved.
- Roll at death: a camp mob's coins (1 + level x 2 + 0 to level + 1; an elite's x3) and things (`ItemDatabase.RollLoot` by its tag, level and rank; from step L2 `LootDatabase.Roll`) are decided in `EnemyDied` (`RollCorpse`), so the body can show them. Story enemies loot as before. Until L2 loads the named loot, a normal mob's body is at best green (generated gear on about 2 to 4.5% of kills, or a cultist's salt potion); blue beams come from elites (about 1 kill in 4) and always from the named bosses' signature pieces (such as the Bandit King's, Greyheart's, Ninebranch's and the Rootwarden's); purple does not drop until L2. Loot capture 01 shows all four through `PutLoot`.
- Beacon (`LootBeacon`, on the enemy's root, on the ground, no light or collider): a white twinkle for coins, junk and common things; a green glint and a 1.2 m column for uncommon; a blue 4 m beam for rare; a purple 7 m beam with a ring on the ground for epic, pulsing. It shows the best quality left on the body and goes out when the body is emptied or the mob respawns.
- E on a body: one that holds gear or anything better than common opens the loot window (coins row, one row per thing with its quality border, coloured name and upgrade arrow); a click takes one, and E or "Take all [E]" takes everything that fits and shuts the window; Esc or walking off (more than 5 m) shuts it. A body of coins, junk and common things is emptied with the one press. Things go in through `Inventory.Add` (onto stacks, then a worn trade bag of their class, then the bags); what does not fit stays on the body ("Your bags are full. The rest stays on the body.") and its beacon stays lit. A body with nothing that fits gives E up to Mira, villagers, nodes and doors until there is room. Opening the window shuts a conversation, a merchant, Trades, the quest book and the map (they are drawn under it and would take its clicks), and it shuts if one of them opens.
- Better or worse (`LootJudge`): score = weapon damage + Strength + half the Stamina + a quarter of the armour and Agility (Intellect and Spirit score nothing yet). A gear tooltip lists the differences against the worn piece stat by stat ("+3 weapon damage" in green, "-2 Stamina" in red) and says "An upgrade" when it scores higher and you can wear it; the same pieces get a green arrow on their squares in the bags, at merchants and in the loot window.
- Call-outs: every thing taken is a chat line ("Looted: Tanned Blade of the Bear.") in its quality's colour; a rare or epic piece also raises a "RARE" or "EPIC" toast over its name.
- Capture: `--crulanda-loot-capture <dir>` (windowed) writes `01-beams-day`, `01-beams-night`, `02-loot-window`, `03-compare-tooltip` and `04-upgrade-arrows`.
