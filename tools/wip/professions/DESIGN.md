# Trades of Crulanda: gathering, refining and professions (final design)

Status: design only. Nothing was run or changed in the repo. All new names are **GAME-ONLY**. Every coordinate is **PROVISIONAL** until the placement tests pass in the validation copy.

## 0. Judgement of the three designs, and what was taken from each

| Design | Verdict | Taken | Left out |
|---|---|---|---|
| **Lean** (backbone) | Smallest route to the whole request: one saved list, recipes known by skill, skill 1-100 with one tier per zone, three or four steps. | Skill scale, tool-on-the-belt, derived recipes, node and station arrays, step order. | Hard skill gates on nodes; smelting under Mining. |
| **Classic** | Most rigorous on data and economy, but about 55 items, 53 recipes, trainer ranks, recipe purchase and two windows: too much for a 13-level game. | Economy tests, gear authored on the generated-gear curve, existing potions made craftable, respawn table that survives a zone hop, "stream undisturbed" test, proximity crafting. | Ranks, recipe buying, 1-150, sawing and tinctures. |
| **Village** | Best fit with the game's strength, but stock-based prices and forge fees add friction and tuning risk. | "The station is the interface, the NPC is the colour" (nothing ever waits on an NPC), selling feeds `VillageLife.Stock` and the trades remark on it, the smith helps when he is there, gathering skill never refuses. | Falling prices, heat fees, lifetime-supply recipes. |

Claims checked against the code (line numbers are from today's working tree):
- `EncounterSession.UseInteractable` (354) is quest-only and returns early with "You don't need any of this right now."
- The eggs hook is hard-coded in `OpenVendor` (258) and `Buy` (280-284). This design leaves it alone.
- `SellJunk` (274) sells `kind == "junk" || quality == 0`. Materials at quality 1 with kind `material` are safe without a code change.
- `Interact` (1106): a villager always wins E over a usable prop, so crafting must not depend on E at a workplace.
- `PlayerCasting / PlayerCastProgress / PlayerCastName` (613-615) are plain getters, so a work channel can share the cast bar.
- `VillageLife.FindPlaces` (130) has a fixed key list and would throw on a new workplace kind. Stations are therefore not Workplaces.
- `VillageLife.Stock` is cleared 04:00-05:00 (113); `StockLine` (341) is where remarks go. Errand stock keys are `<place>.<good>`.
- `EncounterSave.FormatVersion = 7`; `AddDiscoveriesMigration` (130) is the template for a text-insert migration.
- `BuildSecrets()` runs after `RenderMap` (ZoneBuilder 93-95) with a private `System.Random` per position and `StandAt(at, -inf)`. Nodes copy this.
- Trade NPCs: Oakhaven uses the default list (Brannoc Vell blacksmith, Ama Rusk merchant, Hedda Thorne baker, Lisbet Crane herbalist). Khaven: Wenna Coyle herbalist (-15,-12), Cato Brisk merchant (22,-9). Peaks: Yara Quell merchant (-28,-2), Tarsk hunter. Ashrim: Oska blacksmith (54,20), Mother Vane herbalist (62,-4), Sefa Brine merchant (57,-14). Verdant: Alder-Knot blacksmith (-17,35), Moss-Lantern merchant (9,36), Ondine Varro merchant.
- Beasts by zone JSON tag: wolf and boar in Oakhaven, Khaven and Peaks; hound in Ashrim; mossboar and stag in Verdant.
- Keys K and P are unused (I, C, B, L, M, R, Tab, F-keys are taken).
- No test mentions `junk.boar_meat`.

## 1. What the player can do

1. **Gather.** Mine ore seams (needs a pick), chop windfall timber (needs a hatchet) and pick herbs (bare hands) in all five zones. Everything goes into the normal bags. Nodes regrow. No limit, no profession slot.
2. **Sell.** Any vendor buys materials at their value. Selling in a village feeds that day's village stock, and the trades talk about it.
3. **Refine and cook (everyone).** Burn logs to charcoal at a forge or fire. Cook meat, eggs and flour at any fire (inn hearth, bake oven, cookfire).
4. **Choose two crafts.** Blacksmith (ore to bars to weapons and armour, at a forge) and Alchemist (herbs to potions, at a herbalist's bench). Taken up at the trade's workplace, forgotten any time.
5. **Skills 1-100** for Mining, Woodcutting, Herbalism, Cooking and each craft, one tier per zone, shown in a new Trades window (K).

## 2. Materials, nodes and tools

### 2.1 Materials (kind `material`, quality 1, stack 20)

| Tier | Skill | Zone (levels) | Ore | Log | Herb | Raw value |
|---|---|---|---|---|---|---|
| 1 | 1 | Oakhaven (1-2) | `mat.copper_ore` Crowsfoot copper ore | `mat.oak_log` Harrow oak log | `mat.yarrow` Meadow yarrow | 1 |
| 2 | 20 | Khaven (3-5) | `mat.bogiron_ore` Carrion bog-iron ore | `mat.blackpine_log` Black pine log | `mat.mourners_cap` Mourner's cap | 2 |
| 3 | 40 | Shattered Peaks (6-8) | `mat.adit_ore` Adit iron ore | `mat.stonepine_log` Stone-pine log | `mat.tarnwort` Tarnwort | 3 |
| 4 | 60 | Ashland Rim (9-10) | `mat.cinder_ore` Cinder ore | `mat.snag_wood` Ash-snag wood | `mat.cinder_thistle` Cinder-thistle | 4 |
| 5 | 80 | Verdant Shore (11-13) | `mat.veridian_ore` Veridian ore | `mat.ghostoak_log` Ghost-oak heartwood | `mat.dewfern` Dewfern frond | 6 |

Other materials:

| Id | Name | Value | Source |
|---|---|---|---|
| `mat.charcoal` | Charcoal | 1 | refined from logs; blacksmith vendors (4 gold) |
| `mat.copper_bar`, `mat.bogiron_bar`, `mat.ridgesteel_bar`, `mat.ashsteel_bar`, `mat.veridian_bar` | bars | 4, 7, 10, 13, 19 | Blacksmith smelting |
| `mat.flour` | Mill flour | 1 | merchants (4 gold), always in stock |
| `mat.salt` | Rim salt | 1 | merchants (4 gold) |
| `mat.vial` | Stoppered vial | 1 | merchants, herbalists (4 gold) |
| `junk.boar_meat` | Tough boar meat (existing id) | 1 | becomes kind `material`, quality 1; drop unchanged |
| `mat.wolf_haunch` | Lean wolf haunch | 1 | new entry in the existing `wolf` loot table, chance 0.4 |
| `mat.hound_flank` | Ash-hound flank | 4 | `hound` table, 0.45 |
| `mat.mossback_chop` | Mossback chop | 5 | `mossboar` table, 0.5 |
| `mat.venison` | Shore venison | 6 | `stag` table, 0.5 |

The quest herbs (`item.yarrow`, `item.mourners_cap`, quest bag) and the found-once healing herbs (`herb.*`) keep their ids and meaning. Pelts, hides and tusks stay junk; no recipe uses them in this version.

### 2.2 Tools

| Id | Name | Value (buy) | Sold by | Use |
|---|---|---|---|---|
| `tool.pick` | Miner's pick | 2 (8 gold) | merchants, blacksmiths | right-click: "You hang the pick at your belt. You can now mine." |
| `tool.hatchet` | Woodcutter's hatchet | 2 (8 gold) | merchants, blacksmiths | right-click: "...You can now cut timber." |

Using a tool consumes the item and creates the saved skill entry (`mining` or `woodcutting`). No bag slot is spent on tools. Using a second one says "You already carry one." and keeps the item. Herbalism and Cooking exist for everyone from the start.

### 2.3 Node types

| Node id | Name | Skill | Yield | Respawn | Look |
|---|---|---|---|---|---|
| `node.copper`, `node.bogiron`, `node.adit`, `node.cinder`, `node.veridian` | "... seam" | mining, tier skill | 1-3 ore | 180 s | rock lumps (`Boulder`, `RockTint`) plus a child named `full` of metal-tinted facets; tints red-brown, grey-brown, dark iron, ember orange, blue-green |
| `node.copper_rich`, `node.veridian_rich` | "Rich ... seam" (cave floors) | mining, tier skill | 2-4 | 180 s | same, larger |
| `node.oak`, `node.blackpine`, `node.stonepine`, `node.snag`, `node.ghostoak` | "Windfall ..." | woodcutting, tier skill | 1-3 logs | 180 s | a wind-felled trunk with stub branches as child `full`, a cut stump that stays |
| `node.yarrow`, `node.mourners_cap`, `node.tarnwort`, `node.cinder_thistle`, `node.dewfern` | herb name | herbalism, tier skill | 1-2 | 90 s | existing `Herb(t, variant)`; new variants 3 (tarnwort, pale blue), 4 (thistle, grey-orange), 5 (dewfern, fronds). `Herb` must keep drawing no random numbers |

Nodes have **no colliders** (like herbs and secrets), so they cannot block paths or the NavMesh. Standing grove trees stay scenery: they are static-batched and cannot be hidden one by one.

### 2.4 Nodes by zone (minimum counts are pinned by a test)

| Zone | Ore (10) | Timber (8) | Herbs (10) |
|---|---|---|---|
| Oakhaven, tier 1 | 6 outside: Crowsfoot mouth cliffs (-34,90) (-26,93) (-6,92) (3,94), Overlook rocks (-47,134) (-54,139). 4 rich on the Crowsfoot Hollow floor (`under`): (-13,99) (-26,116) (-18,137) (-4,150) | Woodyard (-30,64); Harrow wood margin (-92,94) (-108,96) (-96,112); North pines edge (36,108); South copse (-88,-96); Southwood (-80,-152); Mastwood (-122,154) | the 8 existing Yarrow props gain `"node":"node.yarrow"`; 2 new at Brook pond (33,-78) (40,-86) |
| Khaven, tier 2 | along the Carrion Cliffs: (46,-37) (60,-15) (74,-63) (98,-37) (126,-37) (-90,97) (46,107) and 3 more where a cliff has room | North pines (6,50) (12,56); Ridge pines (76,66) (82,72) (114,50) (122,126); Bound-wall pines (-58,146) (-64,152) | the 7 existing Mourner's cap props gain `"node"`; 3 new on the graveyard and Drowned graves margin |
| Peaks, tier 3 | Spoil heap (-140,-58) (-131,-52) (-136,-55); by the Sealed Adit (-153,-50), 5 m clear of the secret at (-148,-41); Umbra scarp (-106,24) (-100,16); scarps (127,-38) (120,-46) (-108,-71); Avalanche scar (145,28) | Wolf pines (-72,52), High pines (-44,96), Gate pines (-20,40), Ore-road pines (-126,-18), East pines (84,-62), South pines (-12,-72), Avalanche deadfall (146,34) (150,24) | Cold Tarn shore (-30,128) (-16,130) (-34,140) (-12,142) and 3 more; Shieling hay meadow x3, 5 m clear of the Frostbell secret (-8,100.8) |
| Ashland Rim, tier 4 | Ash Pit rim (-144,-92) (-152,-100) (-148,-88) (-140,-100); cliffs (-50,-108) (-110,-26) (-80,112) (98,-124) (79,29) (79,-29) | Ashen snags (-80,16) x3, Last Orchard (54,150) x2, Cinderfold orchard x2, one more snag grove | open ash between the snag groves and the pit x10, clear of the Last-Light secrets (40,62) (133,-60) |
| Verdant Shore, tier 5 | Ridge of Long Shadows (144,-20) (148,10) (146,40) (150,-50) (143,70) (152,-34); 4 rich on the Root-Mother's Deep floor (`under`): (-111,150) (-112,160) (-100,186) (-106,200) | Fallen Ghost-Oak (-94,36) (-102,28) (-98,40); Ridge-foot canopy (100,26) (106,20); Tappers' wood (-116,-64) (-110,-58); Mere canopy x1 | Mistmere shore (-50,-86) (-62,-98) (-48,-98) (-56,-92); fern hollow x6, clear of the Lantern-moss secret (38,-80) |

A wip script (`tools/wip/professions/place_nodes.py`) should check each candidate against the zone JSON (roads, water, secrets, grove rectangles) before the PlayMode placement test runs.

## 3. Gathering skills and progression

- Skills: `mining`, `woodcutting`, `herbalism`. Range 1-100. Tier skill 1 / 20 / 40 / 60 / 80.
- **Skill never refuses a node** (the owner's "free for everyone, no limit"). When `skill < node.skill` the work is "hard going": 4 s instead of 2 s (herbs 3 s instead of 1.5 s), the yield is exactly 1, and the skill point is certain. A level-13 character can therefore start mining in Verdant at once.
- Skill-up per gather: certain while `skill < node.skill + 20`; 50% while `skill < node.skill + 40`; otherwise none. Tier-5 nodes take the skill to 100.
- Bonus yield: when `skill >= node.skill + 20`, 25% chance of +1.
- Expected pace: about 20 gathers per tier, 8-10 minutes for a zone circuit.
- Each point prints "Mining 12." At 20/40/60/80 a toast: kicker "MINING", name "Bog-iron seams come easily now".
- No character XP from gathering or crafting.

Gather flow (E on a node, prompt "Mine the copper seam" / "Cut the windfall oak" / "Gather yarrow"):
1. Refusals, in order: in combat ("You can't do that while fighting."); no tool ("You need a miner's pick. Merchants sell them."); bags full and no quest wants the item ("Your bags are full.").
2. Work bar on the cast-bar slot. Moving more than 0.3 m, taking damage, entering combat or dying cancels it.
3. On completion: add the yield to the bags (float text "+2 Crowsfoot copper ore"), roll the skill point, run the existing quest branch unchanged (a wanted quest yarrow is still given), hide the node's `full` child (the whole prop for herbs) until respawn, `Save(false)`.

Respawn is kept in a static in-memory table keyed by `ZoneInteractable.Key(zoneId)` on `Time.realtimeSinceStartup`, so leaving and re-entering a zone does not refill nodes. It is not saved; a restart refills them.

## 4. Crafts: the choice of two, trainers, Cooking

- `cooking`: everyone, from the start, cannot be forgotten.
- `blacksmithing` and `alchemy`: crafts. At most `craftSlots` (2) at once. The slot rule, Learn and Forget all ship even though only two crafts exist (see open question 2).
- **Taking up a craft** happens in the Trades window. The row for an unlearned craft shows "Take up Blacksmithing" and is enabled when a station of that craft's kind (forge / bench) is within 5 m, or a villager of the trainer role (blacksmith / herbalist) is within 6 m. Free. If the tradesperson is near they answer ("Vell: Mind the scale. Copper first; it forgives you."); if not: "You look over the tools and begin."
- **Forget** (button on the craft's page, confirm box "Forget Blacksmithing? Skill 47 will be lost."): frees the slot and removes the entry.
- **Recipes are known by skill.** No recipe list, no recipe purchase, no ranks.
- Recipe skill-up: certain while `skill < recipe.skill + 10` (orange); 50% under `+20` (yellow); 10% under `+30` (green); otherwise none (grey). The list is coloured by this.
- Trainers by zone (colour and convenience only): Blacksmithing: Brannoc Vell, Oska, Alder-Knot. Alchemy: Lisbet Crane, Wenna Coyle, Mother Vane.

## 5. Recipes (first version, complete)

Rules for every recipe, enforced by tests: output value x count is at most 1.5 x the summed value of the inputs; and if every input is sold by some vendor, output value x count is at most the inputs' buy price (4 x value). Every craft takes 2 s on the work bar (1 s when the trade's own NPC is within 8 m and awake: "Vell works the bellows for you"). Inputs and bag room are checked before anything is removed.

### 5.1 Refining, free for everyone

| Recipe id | Skill | Station | Reagents | Result (value each) |
|---|---|---|---|---|
| `recipe.charcoal_oak` | Woodcutting 1 | forge or fire | 1 Harrow oak log | 1 Charcoal (1) |
| `recipe.charcoal_blackpine` | Woodcutting 20 | forge or fire | 1 Black pine log | 2 Charcoal |
| `recipe.charcoal_stonepine` | Woodcutting 40 | forge or fire | 1 Stone-pine log | 3 Charcoal |
| `recipe.charcoal_snag` | Woodcutting 60 | forge or fire | 1 Ash-snag wood | 4 Charcoal |
| `recipe.charcoal_ghostoak` | Woodcutting 80 | forge or fire | 1 Ghost-oak heartwood | 5 Charcoal |

There is no sawing in this version: logs go straight into recipes as hafts and boards.

### 5.2 Blacksmithing (forge)

Smelting:

| Recipe id | Skill | Reagents | Result (value) |
|---|---|---|---|
| `recipe.copper_bar` | 1 | 2 copper ore, 1 charcoal | Copper bar (4) |
| `recipe.bogiron_bar` | 20 | 2 bog-iron ore, 1 charcoal | Bog-iron bar (7) |
| `recipe.ridgesteel_bar` | 40 | 2 Adit iron ore, 1 charcoal | Ridge-steel bar (10) |
| `recipe.ashsteel_bar` | 60 | 2 cinder ore, 1 charcoal | Ash-steel bar (13) |
| `recipe.veridian_bar` | 80 | 2 Veridian ore, 1 charcoal | Veridian bar (19) |

Gear. Authored items (`craft.*`, kind gear, quality 2), stats from `ItemDatabase.Generate` at uncommon quality for the zone's top level (power = level x 1.35; weapon = round(3 + power x 1.6); armour = power x 2.2 chest and legs, 2.6 off hand, 1.4 others; bonus = max(1, power / 4)). `level` (required) is zone top minus 1. Value follows the generated rule (level x 3, x 1.4 for weapons). For comparison, vendor commons do 6 / 11 / 16 / 19 / 24 weapon damage; rares do 30 at level 10 and 38 at 13.

| Skill | Item id | Name (slot) | Req level | Reagents | Stats | Value |
|---|---|---|---|---|---|---|
| 5 | `craft.copper_cudgel` | Copper-shod cudgel (main hand) | 1 | 2 copper bar, 1 oak log | 7 damage, +1 Str | 8 |
| 8 | `craft.copper_buckler` | Copper-banded buckler (off hand) | 1 | 1 copper bar, 2 oak log | 7 armour, +1 Sta | 6 |
| 12 | `craft.copper_gauntlets` | Copper-scale gauntlets (hands) | 1 | 2 copper bar | 4 armour, +1 Str | 6 |
| 16 | `craft.copper_jerkin` | Copper-scale jerkin (chest) | 1 | 3 copper bar | 6 armour, +1 Sta | 6 |
| 24 | `craft.bogiron_hatchet` | Bog-iron hatchet (main hand) | 4 | 3 bog-iron bar, 1 black pine log | 14 damage, +2 Str | 21 |
| 28 | `craft.bogiron_helm` | Bog-iron helm (head) | 4 | 2 bog-iron bar | 9 armour, +2 Sta | 15 |
| 32 | `craft.bogiron_greaves` | Bog-iron greaves (legs) | 4 | 3 bog-iron bar | 15 armour, +2 Sta | 15 |
| 36 | `craft.bogiron_hauberk` | Bog-iron hauberk (chest) | 4 | 4 bog-iron bar | 15 armour, +2 Str | 15 |
| 44 | `craft.ridgesteel_blade` | Ridge-steel blade (main hand) | 7 | 3 ridge-steel bar, 1 stone-pine log | 20 damage, +3 Str | 34 |
| 48 | `craft.ridgesteel_shield` | Ridge-steel shield (off hand) | 7 | 2 ridge-steel bar, 2 stone-pine log | 28 armour, +3 Sta | 24 |
| 52 | `craft.ridgesteel_pauldrons` | Ridge-steel pauldrons (shoulders) | 7 | 3 ridge-steel bar | 15 armour, +3 Str | 24 |
| 56 | `craft.ridgesteel_cuirass` | Ridge-steel cuirass (chest) | 7 | 4 ridge-steel bar | 24 armour, +3 Sta | 24 |
| 64 | `craft.ashsteel_cleaver` | Ash-steel cleaver (main hand) | 9 | 3 ash-steel bar, 1 ash-snag wood | 25 damage, +3 Str | 42 |
| 68 | `craft.ashsteel_helm` | Ash-steel helm (head) | 9 | 3 ash-steel bar | 19 armour, +3 Sta | 30 |
| 72 | `craft.ashsteel_sabatons` | Ash-steel sabatons (feet) | 9 | 3 ash-steel bar | 19 armour, +3 Agi | 30 |
| 76 | `craft.ashsteel_hauberk` | Ash-steel hauberk (chest) | 9 | 4 ash-steel bar | 30 armour, +3 Sta | 30 |
| 84 | `craft.veridian_warblade` | Veridian war-blade (main hand) | 12 | 4 Veridian bar, 1 ghost-oak heartwood | 31 damage, +4 Str | 55 |
| 88 | `craft.veridian_shield` | Veridian tower shield (off hand) | 12 | 3 Veridian bar, 2 ghost-oak heartwood | 46 armour, +4 Sta | 39 |
| 92 | `craft.veridian_legplates` | Veridian legplates (legs) | 12 | 4 Veridian bar | 39 armour, +4 Str | 39 |
| 96 | `craft.veridian_breastplate` | Veridian breastplate (chest) | 12 | 5 Veridian bar | 39 armour, +4 Sta | 39 |
| 100 | `craft.heartwood_greatblade` | Heartwood greatblade (main hand, rare) | 12 | 8 Veridian bar, 2 ghost-oak heartwood, 4 charcoal | 36 damage, +5 Str, +5 Sta | 60 |

Placement: crafted gear beats the vendor's commons, equals a lucky uncommon drop, and stays under the rare quest and elite pieces (the capstone sits just under the best authored weapon at 38).

### 5.3 Alchemy (herbalist's bench)

Steps 1-3 make the existing potions and one new one, so no new effect code is needed.

| Skill | Result | Reagents | Effect | Value |
|---|---|---|---|---|
| 1 | `potion.minor` (existing) | 2 yarrow, 1 vial | heals 90 | 3 |
| 20 | `potion.healing` (existing) | 2 Mourner's cap, 1 yarrow, 1 vial | heals 200 | 8 |
| 40 | `potion.tarn` Tarnwater draught (new, level 6) | 2 tarnwort, 1 Mourner's cap, 1 vial | heals 280 | 11 |
| 60 | `potion.salt` (existing) | 2 cinder-thistle, 1 Rim salt, 1 vial | heals 360 | 15 |
| 80 | `potion.dewfern` (existing) | 2 dewfern frond, 1 tarnwort, 1 vial | heals 520 | 18 |

Step 4 adds crafted-only elixirs (one elixir at a time, 10 minutes, not saved):

| Skill | Result | Reagents | Effect | Value |
|---|---|---|---|---|
| 10 | `elixir.yarrow` Yarrow tonic | 3 yarrow, 1 vial | +2 Stamina | 4 |
| 30 | `elixir.mourner` Mourner's philtre | 3 Mourner's cap, 1 vial | +3 Strength | 7 |
| 50 | `elixir.tarn` Tarn-cold draught | 3 tarnwort, 1 vial | +4 Agility | 12 |
| 70 | `elixir.cinder` Cinder elixir | 3 cinder-thistle, 1 Rim salt, 1 vial | +5 Strength | 16 |
| 90 | `elixir.veridian` Veridian elixir | 3 dewfern frond, 1 cinder-thistle, 1 vial | +8 Stamina | 30 |

### 5.4 Cooking (any fire; free for everyone)

All results are kind `consumable`, `food: true` (heal over 10 s, out of combat). Vendor food heals 150 / 260 / 420 / 640 by tier, so cooked food is about a third better.

| Skill | Result id | Name | Reagents | Heals | Level | Value |
|---|---|---|---|---|---|---|
| 1 | `food.griddle_bread` | Griddle bread | 1 Mill flour | 160 | 1 | 1 |
| 1 | `food.boar_stew` | Boar stew | 2 Tough boar meat | 220 | 1 | 3 |
| 5 | `food.hearth_cake` | Harrow hearth-cake (makes 2) | 1 Mill flour, 1 Fresh eggs | 200 | 1 | 2 |
| 20 | `food.wolf_skewer` | Wolf-haunch skewer | 2 Lean wolf haunch, 1 Mourner's cap | 340 | 3 | 5 |
| 30 | `food.harrow_pasty` | Harrow pasty | 1 Mill flour, 1 Harrow cheese, 1 Tough boar meat | 380 | 4 | 5 |
| 40 | `food.smoked_loin` | Pass-smoked loin | 2 Tough boar meat, 1 tarnwort, 1 Rim salt | 520 | 6 | 8 |
| 60 | `food.salt_flank` | Salt-baked hound flank | 2 Ash-hound flank, 1 Rim salt | 700 | 9 | 12 |
| 70 | `food.cinder_loaf` | Cinder-crust loaf | 2 Mill flour, 1 cinder-thistle, 1 charcoal | 660 | 9 | 8 |
| 80 | `food.mossback_chop` | Dewfern-glazed mossback chop | 2 Mossback chop, 1 dewfern frond | 820 | 11 | 18 |
| 90 | `food.venison_pie` | Shore venison pie | 2 Shore venison, 1 Mill flour | 900 | 12 | 16 |

Step 4 gives the foods from skill 20 up "Well fed" (+2 / +2 / +3 / +4 / +4 / +5 / +5 Stamina, 10 minutes, one meal buff at a time).

## 6. Stations and village tie-ins

### 6.1 Stations

A station is a point with a kind (`forge`, `bench`, `fire`) and a name. A recipe can be made when a station of its kind is within **5 m** ground distance (same Hollow rule as `GroundDistance`). Crafting is done from the Trades window, never through E on the workplace, because a villager at the anvil would always win E.

Sources, all gathered into `ZoneBuilder.Stations`:
- every existing `Workplace` of kind `forge` becomes a `forge` station, kind `oven` a `fire` station (added inside `Workplace()`; `Workplaces` itself is unchanged);
- `Inn()` adds a `fire` station at its "Hearth fire" light, named after the inn;
- the zone JSON `stations` array adds the rest as small new props built after the map render (no colliders, private random stream, not `ZoneProp`s, not Workplaces).

| Zone | Forge | Bench | Fire |
|---|---|---|---|
| Oakhaven | Vell's smithy (existing workplace) | **new** Lisbet's drying bench (6,19), by the Produce stall | The Golden Cask hearth; Thorne's bakehouse oven (both existing) |
| Khaven | none: carry ore to Oakhaven | **new** Wenna Coyle's bench (-17,-12) | The Cracked Hearth (existing inn) |
| Shattered Peaks | **new** Pass-trader's field anvil (-24,0) | none: carry herbs to Khaven or the Rim | **new** Pass cookfire (-31,2) |
| Ashland Rim | **new** Oska's bone-anvil (56,22) | **new** Mother Vane's salt-bench (64,-6) | **new** Enclave cookfire (60,-18) |
| Verdant Shore | **new** Alder-Knot's ember-stone (-19,37) | **new** Moss-Lantern's bench (11,38) | **new** Hearth-Tree fire (17,55) |

New prop looks: field anvil with a coal pan; drying rack with jars and a mortar; ring of stones with a pot.

### 6.2 Village tie-ins

1. **Selling feeds the day.** Materials carry an optional `trade` key. `SellBag` calls `VillageLife.Active.Deliver(trade, count)` when a village is live. Keys: ores and bars `forge.ore`; logs and charcoal `forge.wood` (existing key, so Vell's existing "Hearth's drawing well" line fires); herbs `stall.herbs`; meat `inn.game`. Stock still clears at 04:00.
2. **The trades notice.** New `StockLine` cases: blacksmith, `forge.ore > 0`: "Someone's been up the Crowsfoot with a pick. First ore I've not had to beg for."; merchant, `stall.herbs > 0`: "Fresh-cut herbs on the stall. Somebody's been in the meadow."; drinker, elder, gossip, farmer, `inn.game > 0`: "Boar in the Cask's pot tonight. Somebody's been hunting."
3. **Working beside the trade.** With the trade's NPC within 8 m and awake, crafts take 1 s instead of 2 s and the NPC says a line once per visit. With nobody there, everything still works.
4. **Cooking runs on the village's eggs.** The hearth-cake needs Fresh eggs, which the merchant only has after the hen-wife's round (existing hook, untouched). Flour is always in stock, so Cooking never waits on the mill.
5. **Step 4: the smith's piece.** When `forge.ore >= 6`, a blacksmith vendor's stock gains one uncommon main-hand piece for the day (`GearId("mainhand", band, 2, 700 + band)`), with the line "Made this from the ore that came in. Better than I've managed all year."

## 7. Selling and prices

- Unchanged rule: every vendor buys anything at `value` and sells at 4 x `value`. Sell is by whole stack.
- "Sell junk" does not touch materials (kind `material`, quality 1). Boar meat stops being junk.
- Income check: an Oakhaven circuit (28 nodes, about 9 minutes) pays about 20 ore + 16 logs + 15 herbs = about 50 gold raw, about 75 with bars, against roughly 100 gold from killing for the same time at level 2. A Verdant circuit pays about 300 raw against roughly 600. Gathering is a second income at about half the combat rate. All numbers are data.
- No gold loop: ore, logs, herbs and meat are never sold by vendors; the six vendor-sold inputs (charcoal, flour, salt, vial, cheese, eggs) cost 4 x, and the tests in section 12 enforce both rules.
- Vendor additions (role entries must leave `npc` absent, never `""`): merchant + `tool.pick`, `tool.hatchet`, `mat.flour`, `mat.salt`, `mat.vial`; blacksmith + `tool.pick`, `tool.hatchet`, `mat.charcoal`; herbalist + `mat.vial`. The role `stranger` still sells nothing and the merchant still sells `potion.minor` (both pinned by `ItemTests`).

## 8. Data schema

### 8.1 `EncounterContent/Items/items.json` additions

New optional `ItemDef` fields: `trade` (village stock key), `teaches` (profession id, tools), and in step 4 `buff` (`"stamina" | "strength" | "agility"`), `buffAmount`, `buffMinutes`. New kinds: `material`, `tool`.

```json
{ "id": "mat.copper_ore", "name": "Crowsfoot copper ore", "kind": "material", "quality": 1, "level": 1, "value": 1, "stack": 20,
  "trade": "forge.ore", "description": "Red-veined rock from the Crowsfoot cliffs. A smith can smelt it.", "canonStatus": "GAME-ONLY" },
{ "id": "mat.copper_bar", "name": "Copper bar", "kind": "material", "quality": 1, "value": 4, "stack": 20, "trade": "forge.ore", "canonStatus": "GAME-ONLY" },
{ "id": "mat.oak_log", "name": "Harrow oak log", "kind": "material", "quality": 1, "value": 1, "stack": 20, "trade": "forge.wood", "canonStatus": "GAME-ONLY" },
{ "id": "mat.yarrow", "name": "Meadow yarrow", "kind": "material", "quality": 1, "value": 1, "stack": 20, "trade": "stall.herbs", "canonStatus": "GAME-ONLY" },
{ "id": "tool.pick", "name": "Miner's pick", "kind": "tool", "quality": 1, "value": 2, "stack": 1, "teaches": "mining",
  "description": "Use it to hang it at your belt. It takes no room in your bags after that.", "canonStatus": "GAME-ONLY" },
{ "id": "craft.copper_cudgel", "name": "Copper-shod cudgel", "kind": "gear", "slot": "mainhand", "quality": 2, "level": 1, "value": 8,
  "weaponDamage": 7, "strength": 1, "description": "Oak, with a copper shoe hammered on at Vell's anvil. Yours.", "canonStatus": "GAME-ONLY" },
{ "id": "food.boar_stew", "name": "Boar stew", "kind": "consumable", "food": true, "quality": 1, "level": 1, "value": 3, "stack": 20, "heal": 220, "canonStatus": "GAME-ONLY" },
{ "id": "elixir.yarrow", "name": "Yarrow tonic", "kind": "consumable", "quality": 2, "level": 1, "value": 4, "stack": 10,
  "buff": "stamina", "buffAmount": 2, "buffMinutes": 10, "canonStatus": "GAME-ONLY" }
```

Edits to existing entries: `junk.boar_meat` becomes `"kind": "material", "quality": 1, "stack": 20, "trade": "inn.game"` (id, name, value unchanged). Loot: one entry added to each of the existing `wolf`, `hound`, `mossboar`, `stag` tables (for example `{ "item": "mat.wolf_haunch", "chance": 0.4 }`). Vendors: the lists in section 7.

### 8.2 `EncounterContent/Professions/professions.json` (new; the recipes file)

```json
{
  "craftSlots": 2,
  "professions": [
    { "id": "mining",        "name": "Mining",        "kind": "gather", "tool": "tool.pick",    "verb": "Mine" },
    { "id": "woodcutting",   "name": "Woodcutting",   "kind": "gather", "tool": "tool.hatchet", "verb": "Cut" },
    { "id": "herbalism",     "name": "Herbalism",     "kind": "gather", "verb": "Gather" },
    { "id": "cooking",       "name": "Cooking",       "kind": "free",   "station": "fire" },
    { "id": "blacksmithing", "name": "Blacksmithing", "kind": "craft",  "station": "forge", "trainerRole": "blacksmith" },
    { "id": "alchemy",       "name": "Alchemy",       "kind": "craft",  "station": "bench", "trainerRole": "herbalist" }
  ],
  "nodes": [
    { "id": "node.copper", "name": "Copper seam", "profession": "mining", "skill": 1, "item": "mat.copper_ore", "min": 1, "max": 3,
      "respawn": 180, "seconds": 2, "look": "ore", "variant": 0, "prompt": "Mine the copper seam" },
    { "id": "node.yarrow", "name": "Yarrow", "profession": "herbalism", "skill": 1, "item": "mat.yarrow", "min": 1, "max": 2,
      "respawn": 90, "seconds": 1.5, "look": "herb", "variant": 0, "prompt": "Gather yarrow" }
  ],
  "recipes": [
    { "id": "recipe.charcoal_oak", "name": "Charcoal", "profession": "woodcutting", "skill": 1, "station": "forge|fire",
      "inputs": [ { "item": "mat.oak_log", "count": 1 } ], "output": "mat.charcoal", "count": 1 },
    { "id": "recipe.copper_bar", "name": "Copper bar", "profession": "blacksmithing", "skill": 1, "station": "forge",
      "inputs": [ { "item": "mat.copper_ore", "count": 2 }, { "item": "mat.charcoal", "count": 1 } ], "output": "mat.copper_bar", "count": 1 },
    { "id": "recipe.hearth_cake", "name": "Harrow hearth-cake", "profession": "cooking", "skill": 5, "station": "fire",
      "inputs": [ { "item": "mat.flour", "count": 1 }, { "item": "food.fresh_eggs", "count": 1 } ], "output": "food.hearth_cake", "count": 2 }
  ]
}
```

`station` may list alternatives with `|`. `count` defaults to 1.

### 8.3 Zone JSON (`Zones/*.json`)

Two new optional top-level arrays and one new optional `ZoneProp` field:

```json
"nodes": [
  { "node": "node.copper", "at": { "x": -34, "y": 90 }, "rotation": 20 },
  { "node": "node.copper_rich", "at": { "x": -13, "y": 99 }, "under": true }
],
"stations": [
  { "kind": "bench", "name": "Lisbet's drying bench", "at": { "x": 6, "y": 19 }, "rotation": 90 }
],
"props": [
  { "kind": "herb", "name": "Yarrow", "at": { "x": -70, "y": 44 }, "variant": 0, "interact": "Gather yarrow", "item": "item.yarrow", "node": "node.yarrow" }
]
```

`under: true` places on a cave floor with `StandAt(at, float.NegativeInfinity)`. Because `nodes` and `stations` are not `props`, `NearProp`, the groves and the forest edge see nothing new and no tree or rock moves.

### 8.4 Save payload addition

```json
"professions": [ { "id": "mining", "skill": 37 }, { "id": "blacksmithing", "skill": 12 } ]
```

## 9. Code changes by file

All under `D:\code\mmo\New Unity Project\Assets\Crulanda\`.

| File | Change |
|---|---|
| `Scripts/Encounter/Professions.cs` (new) | Data: `ProfessionFile { int craftSlots; ProfessionDef[] professions; NodeDef[] nodes; RecipeDef[] recipes }`, `ProfessionDef { id, name, kind, tool, verb, station, trainerRole }`, `NodeDef { id, name, profession, item, look, prompt; int skill, min, max, variant; float respawn, seconds }`, `RecipeDef { id, name, profession, station, output; int skill, count = 1; RecipeInput[] inputs }`, `RecipeInput { item; int count = 1 }`, `[Serializable] ProfessionSkill { string id; int skill }`. `ProfessionDatabase.Parse(IEnumerable<string> json, ItemDatabase items)`: collects all errors and throws one `ArgumentException` (unknown item, profession or station kind; duplicate ids; skill outside 1-100; `min > max`; tool that is not kind `tool`); `Node(id)`, `Recipe(id)`, `RecipesFor(profession)`, `Profession(id)`. `ProfessionLog` (pure logic over `EncounterProgress`, no scene, same pattern as `DiscoveryLog`): `Bind(progress)` (null-guards, adds `herbalism` and `cooking` at 1, clamps to 1-100, drops unknown ids and crafts beyond `craftSlots`); `Action<string> Say`; `Action<string,int> SkillUp`; `bool Has(id)`; `int Skill(id)`; `int CraftSlotsUsed`; `bool CanLearn(id, out why)`; `bool Learn(id, out why)`; `bool Forget(id)`; `bool UseTool(ItemDef tool, out why)`; `bool CanGather(NodeDef, out bool hard, out string why)`; `(string item, int count) RollGather(NodeDef, System.Random)` (yield only); `void Gathered(NodeDef, System.Random)` (skill roll); `enum Difficulty { Locked, Orange, Yellow, Green, Grey }`; `Difficulty DifficultyOf(RecipeDef)`; `static float UpChance(int skill, int recipeSkill)`; `int CanMake(RecipeDef)` (from the bags, across split stacks); `bool CanCraft(RecipeDef, Func<string,bool> stationNear, out why)`; `bool Craft(RecipeDef, System.Random, out why)` (checks `Inventory.Count` for every input and `Inventory.Room` for the output first, then `Remove` and `Add`, then the skill roll). |
| `Scripts/Encounter/Items.cs` | `ItemDef` + `trade`, `teaches` (step 4: `buff`, `buffAmount`, `buffMinutes`). Doc comment lists kinds `material`, `tool`. No new `Parse` rules. |
| `Scripts/Encounter/EncounterContent.cs`, `EncounterContent/Encounter.asset` | New `public TextAsset[] professionFiles = new TextAsset[0];` and one asset edit referencing `Professions/professions.json` (made in the validation copy; commit the new `.meta` files; no existing GUID changes). |
| `Scripts/Encounter/EncounterProgress.cs` | `public List<ProfessionSkill> professions = new List<ProfessionSkill>();` after `discoveries`. |
| `Scripts/Encounter/EncounterSave.cs` | `FormatVersion = 8`; `AddProfessionsMigration` (section 10) registered in `CreateSteps()`; guards in `Read`. |
| `Scripts/Encounter/EncounterSession.cs` | `LoadProfessions()` beside `LoadItems()` (a bad file logs "Profession content invalid", leaves `Professions` null and must not null `Items`); `public ProfessionLog Professions`, bound in start-up and in `Load()` next to `Discoveries.Bind`. **Gathering:** at the top of `UseInteractable`: `if (i.node != null && Professions != null) { TryGather(i); return; }`; `void TryGather(ZoneInteractable)` (refusals, then `StartWork`); `public bool GatherNow(ZoneInteractable)` (the completion step, callable from tests: bag add, float text, `Gathered`, the old quest branch moved into `bool QuestUse(i)`, `RestNode(i, respawn)`, `Save(false)`); `static readonly Dictionary<string,float> nodeReadyAt` on `Time.realtimeSinceStartup`; `RestNode` sets `i.hiddenUntil` and hides the `full` child (or all renderers); the `emptiedHidden` block in `TickQuests` re-hides resting nodes after a zone load. **Work bar:** `StartWork(string name, float seconds, Action done)`, `CancelWork()`, `TickWork()` (cancel on move over 0.3 m, damage, combat, death); `PlayerCasting`, `PlayerCastProgress`, `PlayerCastName` also report work. **Crafting:** `public bool TradesOpen`; `ZoneStationSpot StationNear(string kind)` (5 m, `GroundDistance`); `Villager TradeNpcNear(string role, float range)`; `public void Make(RecipeDef, int count)` (queue through `StartWork`, 1 s when the trade NPC is near; `Save(false)` after each); `public void LearnCraft(string id)`, `ForgetCraft(string id)`. **Items:** `EquipFromBag`: kind `tool` calls `Professions.UseTool` and removes one on success; kind `material` says "A crafting material. Press K."; `SellBag`: `if (d.trade != null && VillageLife.Active != null) VillageLife.Active.Deliver(d.trade, count)`. **Prompt:** `InteractPrompt` and `Interact`, after usable and before door: a station in reach gives "Work at the forge" and opens the Trades window on the matching profession. `public void ShowToast(string kicker, string name)` over the private queue. Key K and the Esc chain in `Update`. Step 4: buff timers and totals in `ApplyEquipment`, `UseItem` branch for `buff`, the smith's piece in `OpenVendor`. |
| `Scripts/Encounter/EncounterInput.cs` | `KeyCode.K` in `Press`. |
| `Scripts/Encounter/EncounterHud.Professions.cs` (new partial) | `static Rect TradesRect`, `DrawTrades()`, `TradesUiBlocks(Vector2)`, lazily built styles. |
| `Scripts/Encounter/EncounterHud.cs`, `EncounterHud.Items.cs` | Mirror flag and draw call, `BlocksPointer`, micro-menu button "Trades", help string "K trades"; `Glyph()`: material "Mt", tool "Tl"; `ItemTooltip()` type line "Crafting material" / "Tool". |
| `Scripts/World/ZoneDefinition.cs` | `ZoneNode { node, at, rotation, under }`, `ZoneStation { kind, name, at, rotation }`, `ZoneDefinition.nodes`, `.stations`; `ZoneProp.node`; `ZoneInteractable.node`; `Vanishes` also true when `node != null`; runtime `ZoneStationSpot { kind, name, position }`. |
| `Scripts/World/ZoneBuilder.cs` + new partial `ZoneBuilder.Nodes.cs` | `BuildNodes()` and `BuildStations()` called right after `BuildSecrets()`; each thing from `new System.Random(Zone.seed ^ hash(at) ^ 0x2f6e2b)`, parented under the unbatched props transform, registered in `Interactables` with `kind = "node"`, `prompt` from the node def. Helpers `OreSeam`, `Windfall`, `FieldAnvil`, `DryingBench`, `Cookfire`; `Herb` variants 3-5. `public readonly List<ZoneStationSpot> Stations`; `Workplace()` adds forge and oven stations; `Inn()` adds its hearth. `BuildProps` copies `node` onto the interactable. `BuildProps`' random draws, `BuildGroves`, `BuildForestEdge`, `NearProp` and `VillageLife.FindPlaces` are not touched. The zone builder needs the node defs for looks and prompts: `ZoneBuilder` reads `professionFiles` through the same content reference the session uses, or the session passes a `Func<string, NodeDef>` before `Awake` builds (engineer's choice; the test builds zones with the real file). |
| `Scripts/Encounter/WorldLife.cs` | `StockLine` cases from section 6.2. Nothing else. |
| Data | `Items/items.json` (section 8.1), `Professions/professions.json` (+ `.meta`, folder `.meta`), the five zone files (land the uncommitted `oakhaven.json` edits first). |
| Docs | `SAVE_FORMAT.md` (format 8), `DATA_SCHEMA.md`, `WORLD_ZONES.md` ("Trades" per zone), `CHANGELOG.md`, `CLAUDE_HANDOFF.md`. |

## 10. Save format 8 and migration

- **Before the first run of the new code: copy Chris's save folder (`...\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter`) to `...\hel\work\save-backups\<date>-pre-format-8`.** Every new test sets `SaveDirectoryOverride`; no test may touch the real save.
- One bump for the whole feature, made in step 1: `FormatVersion = 8`, adding `professions`.
- `AddProfessionsMigration : ISaveMigration` (`FromVersion 7`, `ToVersion 8`), a copy of `AddDiscoveriesMigration`: require a JSON object, return the payload unchanged if `"professions"\s*:` is present, otherwise insert `"professions":[]` before the closing brace. Register after `AddDiscoveriesMigration` in `CreateSteps()`.
- `Read`, after the discoveries guard: null becomes an empty list; null entries, blank ids and later duplicates are removed; a `skill` below 0 or above 100 throws `InvalidOperationException("Invalid profession data.")` (save refused, file untouched). Unknown ids are kept in the file and ignored by `ProfessionLog.Bind`, so content can change.
- No catch-up is needed for existing characters, because skill never refuses a node.
- Not saved: node respawn timers, buffs, known recipes (derived from skill).
- Update `SAVE_FORMAT.md` and the tests that pin 7 or the payload tail.

## 11. UI

**Trades window.** Key K, micro-menu button "Trades". `TradesRect (190,110,660,600)`; opening it closes the character sheet and opens the bags beside it. Closes on K, Esc or its button.
- Left column (190 wide): one row per skill with a bar in the style of `BookStanding`: Mining, Woodcutting, Herbalism, Cooking, then "Crafts 1 of 2" with Blacksmithing and Alchemy. A gathering skill without its tool reads "Needs a pick (merchants)". An unlearned craft reads "Take up Blacksmithing" (enabled as in section 4) or, when both slots are full, "Two crafts already. Forget one first."
- Middle (220 wide, scroll): for a gathering skill, the five node tiers with zone names, coloured by your skill, then its refine recipes; for Cooking and crafts, the recipe list coloured orange / yellow / green / grey with the number makeable ("Bog-iron helm x2"); recipes above your skill are shown locked with "Blacksmithing 44". A "Can make" toggle.
- Right (page): result square with the normal item tooltip (and worn comparison for gear), reagents as have / need (red when short), the station line ("Forge: Vell's smithy" or red "Needs a forge nearby"), "Make" and "Make all (N)", and the Forget button for crafts.

**Prompts and messages.**
- Nodes: the node's prompt through the existing E prompt. Stations: "Work at the forge" / "Work at the bench" / "Cook at the fire" when nothing with higher priority is in reach.
- Work bar: the existing cast bar, labelled "Mining", "Cutting", "Gathering", or the recipe name.
- Chat: "Mining 12."; "+2 Crowsfoot copper ore" as float text; "You make a Copper bar."
- Toasts: tier thresholds, "BLACKSMITHING / Taken up".
- Bags: right-click a material without a vendor: "A crafting material. Press K."; glyphs "Mt" and "Tl".

## 12. Tests

EditMode, new `ProfessionDataTests.cs` (real JSON):
- `ProfessionsFile_Parses`: `professions.json` parses against the item database; `Encounter.asset` references it.
- `EveryNodeAndRecipe_UsesKnownItems`: all inputs, outputs and node items exist; skills 1-100; stations are forge, bench or fire.
- `Recipes_NeverBeatTheirInputs`: output value x count is at most 1.5 x summed input value.
- `Recipes_FromVendorGoods_NeverProfit`: if all inputs are vendor-sold, output value is at most the buy price of the inputs.
- `CraftedGear_SitsOnTheGeneratedCurve`: each `craft.*` item's damage or armour is within 1 of the `Generate` formula at quality 2 for its tier level (the capstone at quality 3 is allowed up to 36).
- `EveryZone_HasTenOreEightTimberTenHerbNodes`: counts from the zone JSON (props with `node` included); every `node` id exists; node keys are unique per zone.
- `EveryRecipeStation_ExistsSomewhere`: forge, bench and fire each exist in at least one zone's data or workplaces.

EditMode, new `ProfessionLogTests.cs` (`EncounterSession.FreshProgress()`):
- `Tool_TeachesSkill_AndIsConsumed`; `SecondTool_IsKept`.
- `Gather_WithoutTool_IsRefused`; `Gather_UnderSkill_IsHardGoing_YieldsOne_AlwaysSkillsUp`; `Gather_FullBags_GivesNothing_NoSkill`.
- `SkillUp_Bands_WithSeededRng`: certain, half, none; never above 100.
- `Craft_MissingReagent_ChangesNothing`; `Craft_FullBags_ChangesNothing`; `Craft_RemovesInputs_AddsOutput`; `CanMake_CountsAcrossSplitStacks`; `Craft_BelowSkill_IsRefused`; `Craft_WithoutStation_IsRefused`.
- `ThirdCraft_IsRefused`; `Forget_FreesSlot_AndDropsSkill`; `Cooking_CannotBeForgotten`.

EditMode, existing files:
- `ItemTests`: add `SellJunk_KeepsMaterials` and `BoarMeat_IsMaterial`; existing asserts (wolf pelt stacks, static glass 24, merchant and stranger stock, wolf loot ranges) must still pass with the new wolf entry.
- `SaveMigratorTests`: `V7Payload_MigratesTo8_AddsProfessions_KeepsEveryOtherCharacter`; `V8_RoundTrips`; `SkillOutOfRange_RefusesSave_FileUnchanged`; update the tests that pin format 7.

PlayMode (each sets `SaveDirectoryOverride`):
- `NodeStreamTests.TreesAndScenery_Unmoved`: for each zone, `ZoneBuilder.Trunks` and the static scenery child positions are identical with and without `nodes` / `stations` in the zone data.
- `NodePlacementTests.EveryNodeAndStation_IsReachable`: within 2.5 m of the NavMesh, not in water, at least 5 m from every secret and from any other node, 2 m from every trunk; `under` nodes are inside a Hollow.
- `GatherTests.CopperSeam_WithPick_FillsBag_HidesAndReturns`; `GatherTests.Respawn_SurvivesZoneReload`; `GatherTests.Yarrow_GivesBagHerbAlways_AndQuestItemWhenWanted`; `GatherTests.Moving_CancelsWork`.
- `CraftStationTests.Charcoal_AtVellsSmithy_Works_TenMetresAway_Refused`; `CraftStationTests.GoldenCaskHearth_CountsAsFire`; `CraftStationTests.TakeUpBlacksmithing_AtTheForge_AtNight_Works`.
- `VillageSupplyTests.SellingOre_DeliversForgeOre_AndVellRemarks`.
- `TradesUiTests.Capture`: Trades window at 1440x900 into `...\hel\work\ui-captures`.
- Must stay green: `OakhavenQuestTests`, `SecretPlacementTests`, `ZoneContentTests`, `VillageErrandTests`, `VillageDrinkTests`, `EncounterLoopTests`.

## 13. Build order (four publishable steps)

Before step 1: commit or park the uncommitted painted-pass and village work in the tree (it touches `oakhaven.json`, `ZoneBuilder.cs`, `VillageWork.cs`, `WorldLife.cs`), back up Chris's save, sync to the validation copy. After each step: full EditMode and PlayMode run in the validation copy, playable build, commit, `tools\Backup.ps1`.

| Step | Contents | Acceptance check |
|---|---|---|
| **1. Gather and sell** | Save format 8; material and tool items; tools, flour, salt, vials, charcoal at vendors; `professions.json` with professions and nodes; `ProfessionLog` gathering half; `BuildNodes` in all five zones; `node` on existing herb props; work bar; Trades window with the skill column and the node guide; item kinds in bags and tooltips; selling delivers to village stock and the remark lines. | In the build: buy a pick from Ama Rusk, mine a Crowsfoot seam, see "Mining 2.", sell the ore, hear Vell remark on it. Chris's real save loads as format 8 with every skill absent and nothing else changed. Stream and placement tests green in all five zones. |
| **2. Refine and cook** | Recipe engine (`Craft`, `CanMake`, difficulty colours); stations (workplaces, inn hearths, the new props); the recipe pane with Make / Make all; charcoal; the ten Cooking recipes; meat drops; boar meat becomes a material. | Chop a windfall, burn charcoal at Vell's forge at night with nobody there; cook boar stew at the Golden Cask hearth and eat it; "Sell junk" keeps the meat. Economy tests green. |
| **3. Two crafts** | Blacksmithing (5 smelts, 21 pieces), Alchemy (5 potions), take up and forget, the two-slot rule, the trade NPC speeding the work. | Take up Blacksmithing at the smithy, smelt copper, make and equip the Copper-shod cudgel; take up Alchemy at Lisbet's bench and make a minor potion; a third craft is refused; Forget frees the slot. Curve test green. |
| **4. Depth** | Timed buffs (five elixirs, "Well fed"); the smith's daily piece from `forge.ore`; pacing and price tuning from one play pass; optionally the third craft (open question 2). | Drink a Yarrow tonic and see +2 Stamina on the character sheet for 10 minutes; supply Vell 6 ore and find his uncommon piece on sale that day. |

## 14. Open questions for the owner (recommended default in bold)

1. **Who can smelt ore into bars?** **Default: only a Blacksmith**, as your decision states ("Blacksmith: ore -> bars -> weapons"); everyone else sells ore raw and can still refine logs into charcoal and cook. Alternative: everyone smelts (a one-word change per recipe in the data).
2. **Two crafts out of two is not yet a choice.** **Default: ship Blacksmith and Alchemist now and add a Tanner (hides to leather armour, at the tannery) as the optional end of step 4**, which makes the choice real. Alternative: leave it at two until more are wanted.
3. **Should low skill block a node?** **Default: no. Low skill only makes it slow with a yield of 1**, so an existing level-13 character can gather anywhere at once. Alternative: classic hard gates ("Requires Mining 40"), which send that character back to Oakhaven first.

## Known risks

- Bags stay at 24 slots; a gathering trip uses 5-8 of them. No bank or stack split in this design.
- Pacing and gold rates are estimates from the value tables, not played numbers; step 4 includes a tuning pass.
- Khaven has no forge and the Peaks no bench: materials are carried one zone over.
- Selling a material to any vendor counts as a village delivery, even if that vendor is not the trade named.
- Node coordinates are unverified; expect to nudge several, especially the cave-floor ones.
- `ZoneInteractable.Key` rounds to a metre: two nodes of one name within a metre would share a respawn timer (the data test forbids it).
