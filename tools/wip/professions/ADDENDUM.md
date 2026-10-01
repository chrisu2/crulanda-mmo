# Trades of Crulanda, addendum: households, workshops, purses, the leatherworker's bags, and hunting for hides

Status: design only. Nothing was run in Unity; no build or test was run. It extends `tools/wip/professions/DESIGN.md` (DESIGN below) and replaces `ADDENDUM_DRAFT.md`. The single build order for both documents is `BUILD_PLAN.md`. All new names, kin and lines are **GAME-ONLY**. Every coordinate is **PROVISIONAL**: checked by script against `oakhaven.json`, not yet in the engine. Line numbers are from the tree at commit 078d276.

What changed from the draft (after two reviews, each claim re-checked in the code):
- Two new houses, not one (a Crisp cottage by the mill as well as the Carder farmhouse).
- The purse rules are simpler and their numbers now follow from the rules: no coin changes hands for deliveries between trades, needs are bought strictly in order, and only the Tanners are short with no player. One worn bag keeps them warm for good.
- Nobody is sent home early for being poor (it emptied the inn and broke the drinkers' test). Chimneys smoke as today; only a house that went without firewood goes cold.
- Bags come before purses in the build, cost 12 to 24 gold, and can be bought by knocking when Maud is at home.
- The placement rule now covers the creek, a tree pushed off a neighbour, grass and rain. The mill already has a door. The inn's back windows move out of the way of the kitchen and the rooms door.
- New section G: wild animals are huntable for their hides (the owner's later request, `OWNER_NOTES.md` items 4 and 5).

## 0. What was checked in the code, and what it means

| Fact (file) | Consequence |
|---|---|
| `VillageLife.Init` gives homes by `Homes[i % Homes.Count]`; a hen-wife gets the house nearest her coop (WorldLife.cs 73, 94). `Homes` is every non-openable `ZoneDoor`. `Mill()` calls `House()` (ZoneBuilder.cs 3131), so the mill already registers a door: Oakhaven has **13** home doors today (props 4-13, 61, 62, 64). | Replaced by named households. The modulo rule is deleted; the fallback is the derived rule in A.5. No extra door is added for a mill. |
| Oakhaven roles by index: 0 Brannoc Vell blacksmith, 1 Wil Carder farmer, 2 Ama Rusk merchant, 3 Hedda Thorne baker, 4 Sel Harrow gossip, 5 Garet Moss hunter, 6 Old Tobin drinker, 7 Pim child, 8 Osk Farrow lumberjack, 9 Lisbet Crane herbalist, 10 Ilse Brandt farmer, 11 Corwin Ashby elder, 12 Aldo Crisp miller, 13 **Maud Tanner leatherworker**, 14 Fen Walker skinner, 15 Edda Pell gossip, 16 Nettie child, 17 Tamsin Reed merchant, 18 Jory drinker, 19 Grete Lowe farmer. Hen-wives by coop: Goody Marl, Hettie Brook, Nan Pennock. Residents: Quill (inn), Warden Ivel (post). Name 20 of the default list, "Hob Linden", is unused in Oakhaven. | The leatherworker in the published build is a woman (open question 1). No quest or test names Maud, Fen, Nettie, Pim, Jory, Edda, Ilse, Grete, Osk or Tamsin. |
| `House()` and its helpers draw nothing from the zone's random stream. `BuildProps` has a private-stream list (`legacy == 0`, line 1048). `NearProp` (3030) drops a grove tree, after its position draw, when it is within `max(size)/2 + 5` m of a prop; `Spot()` (2909) can push a tree up to 1.85 m off a neighbouring trunk and tests the pushed position too. | Rule 3 in A.3. |
| `ZoneWater.Room` (ZoneWater.cs 300-317) loops over every prop: one whose centre is within `creek width + 10 + max(size)/2` m of the creek's authored line limits the meander, which moves the creek, its banks and then trees zone-wide. | Rule 4 in A.3 (the draft missed it). |
| Grass is cut by a separate list in `Openness()` (3073: house, inn, barn, mill, ruined_house). `Footprint()` (1122) only stops rain and moves ruins. | New workshop kinds join both lists, or grass grows through their floors. |
| `Inn()` puts ground-floor windows on both long walls at local x = -4.5, -1.9, 0.7, 3.3 (1388-1397); the bar top spans x -5.5 to -1.3; it adds only table seats to `Places["inn"]`. `Window()` draws nothing random. | The rooms door goes at x = -0.6. The two back-wall windows under the kitchen are left out when the zone has a kitchen. |
| `Smoke()` returns void and is skipped when `art.particle` is null (1331, 3111). Every house and the Golden Cask smoke all day today. | `Smoke()` returns the particle system; chimneys keep smoking by default (C.2). |
| `FindPlaces` (WorldLife.cs 130) has a fixed key list and throws on an unknown workplace kind; it keeps only the sampled position, not the workplace's name (132-133). | New kinds are added to the key list in the same step as the props; ownership needs a `placeName` map filled in that loop. |
| `Villager.StartErrand` detects a new day with `hour < lastHour - 6` (518). `VillageLife.Update` clears stock with `WorldClock.Between(4, 5)` inside a 20 s tick (113). | The purse uses the first form. |
| Tests: `VillageWorkTests` pins a `Places` set, goods keys, the hen-wife's errand ids and that every errand ends by 21:00. `VillageErrandTests` waits on `inn.eggs`, `stall.eggs`, `mill.grain`. `VillageDayTests` (23:30) needs everyone who is not a drinker and not a post-resident hidden. `VillageDrinkTests` needs the drinkers to drink until `inn.ale` is 0. | No stock key changes. The innkeeper has a home and is abed by 23:30. Nothing in this design stops a drinker drinking. |
| `items.json` has vendor entries for merchant, blacksmith, baker, herbalist and two named Keepers only. A villager with quests and wares opens on a list with "Browse wares" (`QuestTalk`, EncounterSession.cs 107). The till is whoever you talked to (`VendorNpc`). | The leatherworker sells nothing until the bags step adds her entry. "Quest or buy outright" works with the existing talk. |
| Quests are one-time; `collect` and `deliver` count the quest bag, not the bags; `rewards.items` are quest items. Hides are `junk.*`, quality 0, so "Sell junk" (274) sells them. | A `bring` objective, a bag-item reward, and hides become materials. |
| `EncounterSave.Read` accepts `bag.Count` up to `BagSize * 4` (96). The HUD draws 24 slots. Saves are per class (`EncounterSave.SlotFor`). | Trade-bag slots are appended after slot 24. Purses are keyed by save slot. |
| Critters (WorldLife.cs 867 on) have no actor, health or collider. `Session.Enemies` is read by `VillageLife.EnemiesCleared`, `EnemyNear` and `Danger`. Camp mobs respawn (`EncounterEnemy.Respawn`). | Huntable animals are passive camp-style actors in their own list (section G). |

## A. Households and homes

### A.1 Data shape (zone JSON, `life`)

```json
"life": {
  "villagers": 20,
  "households": [
    { "name": "Tanner", "house": "Tanner house", "stipend": 3, "canonStatus": "GAME-ONLY",
      "members": [ { "name": "Maud Tanner", "kin": "head" }, { "name": "Fen Walker", "kin": "husband" }, { "name": "Nettie", "kin": "daughter" } ] }
  ],
  "workshops": [ { "who": "Maud Tanner", "prop": "Tanner's leather shop" } ],
  "residents": [ { "...": "Quill and Warden Ivel stay first, unchanged" },
                 { "name": "Hob Linden", "role": "innkeeper", "title": "Innkeeper", "works": true } ]
}
```

- `house` is a prop name. A `house` or `mill` prop's door already carries the prop's name. For a `barn` named as a home the builder adds a home door (A.6). An `inn` named as a home resolves to its rooms door (B.3).
- `members[].name` matches the spawned villager (default list by index, hen-wives by coop index, residents by name). `kin` is free text for lines.
- `stipend` (default 6) and `needs` (default `["bread", "firewood", "eggs"]`) are used by section C.
- `works: true` on a resident: an ordinary villager with that role's day and a home, not a post. **Hob Linden is appended after Quill and Warden Ivel** (a resident's spawn index is 60 + its position, and the index seeds the look).
- `ZoneLife.households`, `ZoneLife.workshops` and `ZoneHousehold.members` are declared with empty-array initialisers, as `residents` is, because `JsonUtility` leaves a missing array at its initialiser and three zones have no block.

Data test: every villager who is not a post-resident is in exactly one household; every `house` names a prop of kind house, barn, mill or inn; no two households share a house; household names are unique in a zone.

### A.2 Oakhaven: the households

The ten village houses are unnamed today (props 4-13); they get names (a name changes no random draw).

| Household | House (prop, position) | Members (kin) | Stipend |
|---|---|---|---|
| Vell | prop 10 (17,-25) "Vell house" | Brannoc Vell, blacksmith | 6 |
| Thorne | prop 12 (-26,-4) "Thorne house" | Hedda Thorne, baker | 6 |
| **Tanner** | prop 13 (-29,-21) "Tanner house" | Maud Tanner, leatherworker (head); Fen Walker, skinner (husband); Nettie, child (daughter) | **3** |
| Rusk | prop 8 (36,5) "Rusk house" | Ama Rusk, merchant | 6 |
| Reed | prop 4 (-22,14) "Reed house" | Tamsin Reed, merchant | 6 |
| Farrow | prop 5 (-16,28) "Farrow house" | Osk Farrow, woodcutter (head); Pim, child (son) | 6 |
| Crane | prop 6 (12,25) "Crane house" | Lisbet Crane, herbalist | 6 |
| Ashby | prop 7 (28,19) "The Elder's house" | Corwin Ashby, elder | 6 |
| Pell | prop 11 (8,-31) "Pell house" | Old Tobin, drinker (head); Edda Pell, gossip (wife) | 6 |
| Jory | prop 9 (30,-12) "Jory's house" | Jory, drinker; lives alone | 2, needs bread only |
| Harrow farm | prop 62 "Harrow farmhouse" (-94,48) | Sel Harrow, gossip (head); Ilse Brandt, farmer (hand); Goody Marl, hen-wife (aunt) | 6 |
| **Carder farm** | **NEW** "Carder farmhouse" (-61,47) | Wil Carder, farmer (head); Hettie Brook, hen-wife (aunt) | 6 |
| Brook farm | prop 64 "Brook farmhouse" (58,-102) | Grete Lowe, farmer (head); Nan Pennock, hen-wife (lodger) | 6 |
| **Crisp** | **NEW** "Crisp cottage" (-60,-37) | Aldo Crisp, miller | 6 |
| Moss | prop 131 "Moss's lodge" (-93,143) | Garet Moss, hunter | 6 |
| The Golden Cask | prop 1 | Hob Linden, innkeeper (head); Quill (lodger); Mira (lodger, not a villager) | 6 |
| (none) | none | Warden Ivel, a post-resident at (-80,83), as now | none |

Every villager, hen-wife and the innkeeper has a named home. **Two new houses**: the Carder farmhouse and the Crisp cottage. The ten village houses fit the ten village households because some invented families share (open question 2). The miller now has a house apart from his mill and the hunter a workplace (the game rack) apart from his lodge, so no trade's home and workshop are the same spot.

The hunter lives 170 m from the green (a game hour is 100 s; he walks 1.6 m/s, so about 1.1 game hours). To keep him under the 23:30 check: his evening shift (18 to night) becomes `lodge, lodge, inn`, his bed hour is 19.8, and his "hares to the inn" errand keeps its 13:30 end. This is decided, not asked: the lodge is already "Moss's lodge" on the map.

### A.3 The new houses, and the rule for adding any building

```json
{ "kind": "house", "name": "Carder farmhouse", "at": { "x": -61, "y": 47 }, "rotation": 330, "size": { "x": 8, "y": 5.5 }, "variant": 2 },
{ "kind": "house", "name": "Crisp cottage", "at": { "x": -60, "y": -37 }, "rotation": 10, "size": { "x": 7, "y": 5 }, "variant": 3 }
```

**The rule (all new props, here and in section B):**
1. Append to the end of `props`.
2. A new kind goes in the `legacy == 0` private-stream list in `BuildProps`.
3. The centre stays more than `max(size)/2 + 7` m from every non-orchard grove rectangle (5 m for `NearProp`, 2 m for a tree pushed off a neighbouring trunk).
4. The centre stays more than `creek width + 10 + max(size)/2` m from every water line as authored (Oak creek: 14 m plus half the size).
5. Not within 16 m of a secret, not in a field, camp or tall-grass patch, and not where `ClearOfBuildings` would move a ruin.
6. A test (`VillageStreamTests`) compares trunks, static scenery and the creek's points with and without the new props.

Checked by script (edge to edge unless noted):

| | Carder farmhouse (-61,47), 8x5.5 | Crisp cottage (-60,-37), 7x5 |
|---|---|---|
| Nearest props | Yarrow 3.8 m, pine 4.8, Harrow barn 5.5, Tithe barn 6.8 | tree (-60,-30) 7.0 centre to centre (4.5 m off the north wall), lamp 9.0, barrels 10.8, the mill 18.0 |
| Roads | Harrow track about 20 m | Mill lane 11.1 m |
| Fields | Harrow stubble 10.9 | Creek field 7.9 from the centre (about 4 m from the wall), West furrows 11.3 |
| Non-orchard grove (rule 3) | 41.6 m (needs 11) | 52 m (needs 10.5) |
| Creek line (rule 4) | 104 m (needs 18) | **19.9 m (needs 17.5): must not move south of y = -39** |
| Other | collector at (-84,43) 23 m; Barn yard clearing 18 m | Mill yard clearing 8.2 m; **the collector at (-58,-46) stands 9 m from the centre** until the player kills him |

Both stand outside the flat 58 m, so `House()` builds the plinth down the slope as it does for the Harrow farmhouse. Side effect: a random "meadow" or "wander" stand point may fall under a new house, which shifts where some villagers idle (the village's life stream, seed + 7; no test pins it).

### A.4 Names, knocking, map

- Prop names are door names, so the prompt becomes "Knock · Tanner house" with no change to the prompt code.
- Knocking answers by household (`VillageLife.KnockLine(door)`), falling back to today's four barred-door lines for a door with no household:
  - someone in, head out: "A voice through the planks: 'Maud's at the shop by the South road. Try there.'"
  - all abed: "The Tanners are abed. A child coughs, and somebody hushes her."
  - nobody home: "No answer. The Tanner house is empty till supper."
- **Knocking reaches a vendor or quest-giver who is behind the door.** If a member who has wares or quest business for you is home, the knock opens that talk as if you had found them: "A shutter opens. \"At this hour? Go on, then.\"" So a bag can be bought, or pelts handed in, at night.
- Map: no labels for village houses. New landmarks (GAME-ONLY): "Carder farm" (-61,45), "Tanner's leather shop", "Lisbet's drying hut". The cottage sits inside the mill's landmark.

### A.5 Khaven, and zones with no houses

Khaven (5 houses, 6 villagers, 3 post-residents; no new building, default stipends, nothing to buy so no purse behaviour):

| Household | House | Members |
|---|---|---|
| Grane | prop 11 (-4,-16) "Grane house" | Hollis Grane |
| Vey | prop 12 (11,-14) "Vey house" | Dorra Vey (head); Old Kestrel (father) |
| Jenn | prop 13 (-12,-19) "Jenn house" | Mattock Jenn (head); Siv Harl (wife) |
| Tabor | prop 14 (3,22) "Tabor house" | Wynn Tabor |
| Crypt-Keeper's Hovel | prop 10 (21,7) | Ansel Morrow (keeps his post; listed so the knock names him) |

A zone with villagers but no `households` block uses the derived rule: villager `i` gets house `i` while houses last; the rest lodge at the inn; a hen-wife joins the household of the house nearest her coop. Nobody is given a door by modulo. No current zone relies on this (the other three have 0 villagers).

### A.6 Code changes (homes)

| File | Change |
|---|---|
| `Scripts/World/ZoneDefinition.cs` | `ZoneLife.households`, `.workshops` (both initialised empty); `ZoneHousehold { name, house, canonStatus; int stipend = 6; string[] needs; ZoneMember[] members = new ZoneMember[0] }`; `ZoneMember { name, kin }`; `ZoneWorkshop { who, prop }`; `ZoneResident.works`. |
| `Scripts/World/ZoneDoor.cs` | `ZoneDoor.kind` ("house" default, "rooms", "home"); `ParticleSystem smoke`. |
| `Scripts/World/ZoneBuilder.cs` | `Smoke()` returns the `ParticleSystem`; `House()` sets it on its door inside the `art.particle` check. `BuildHomeDoors()` after `BuildProps()`: for each household whose `house` is a **barn** prop, add `ZoneDoor { name, kind = "home", openable = false }` at local (0, 1, -d/2 - .6). Test hook `public static Func<ZoneDefinition, ZoneDefinition> DefinitionFilter`, applied in `Awake` after the zone is chosen, cleared in test teardown (also used by DESIGN's `NodeStreamTests`). |
| `Scripts/Encounter/WorldLife.cs` | `Households`, `Household { name, def, ZoneDoor house, List<Villager> members }`, `HouseholdOf(name)`. `Init` builds households first and gives `home` from them; the derived rule otherwise; residents with `works` spawn with no fixed place; a hen-wife takes her household's house. Hunter bed hour 19.8. `Villager.Home`, `Villager.Household` getters. `KnockLine`, `AtHome(door)` (members hidden behind it). `Paid(string npc, int coin)` as an empty method until section C. |
| `Scripts/Encounter/EncounterSession.cs` | Knock: a vendor or quest-giver at home opens `QuestTalk` or `OpenVendor` at the door; otherwise `KnockLine` or the barred-door lines. |
| `Zones/oakhaven.json`, `Zones/khaven.json` | House names, the two houses, `life.households`, the landmark. |

## B. A workshop per trade

### B.1 The table

| Trade (who) | Workshop | Prop | Place key | Player station |
|---|---|---|---|---|
| Blacksmith (Brannoc Vell) | Vell's smithy (8,-17) | existing `forge` | `forge` | forge |
| Baker (Hedda Thorne) | Thorne's bakehouse (-35,-11); the Bread stall (-12,19) | existing `oven`, `stall` | `oven`, `stall` (owned) | fire |
| Merchant (Ama Rusk) | Produce stall (2,15) | existing `stall` | `stall` (owned) | none |
| Merchant (Tamsin Reed) | Cloth and pots (-12,12) | existing `stall` | `stall` (owned) | none |
| Skinner (Fen Walker) | Tannery yard (-58,-16) | existing `tannery` | `tannery` | none |
| **Leatherworker (Maud Tanner)** | **NEW Tanner's leather shop** (-20,-33) | kind `leathershop` | `leathershop` | reserved for a later leather craft; where bags are sold |
| **Herbalist (Lisbet Crane)** | **NEW Lisbet's drying hut** (23,37) | kind `dryhut` | `dryhut` | **bench** (replaces DESIGN's "Lisbet's drying bench (6,19)") |
| **Innkeeper (Hob Linden)** | the bar, and the **NEW Cask's kitchen** (-20,-18.3) | `Inn()` addition; kind `kitchen` | `bar`, `kitchen` | **fire** (as well as the taproom hearth) |
| **Hunter (Garet Moss)** | **NEW game rack** (-96,137) by his lodge | kind `gamerack` | `lodge` | none |
| Woodcutter (Osk Farrow) | Woodyard (-24,60) | existing `woodpile` | `woodpile` | none |
| Miller (Aldo Crisp) | Oak creek mill | existing `mill` | `mill` | none |
| Farmers | the fields nearest their farm | fields | `field` | none |
| Hen-wives | their coop | `coop` | yard, nest, trough, pan | none |

### B.2 New props

Appended after the two houses, no `size`, kinds in the `legacy == 0` list, in `Footprint()` (rain) and in `Openness()`'s building list with a fixed 4.5 m radius (grass; the game rack stands in a clearing and needs neither).

```json
{ "kind": "leathershop", "name": "Tanner's leather shop", "at": { "x": -20, "y": -33 }, "rotation": 270 },
{ "kind": "dryhut", "name": "Lisbet's drying hut", "at": { "x": 23, "y": 37 }, "rotation": 315 },
{ "kind": "kitchen", "name": "The Cask's kitchen", "at": { "x": -20, "y": -18.3 }, "rotation": 41 },
{ "kind": "gamerack", "name": "Moss's game rack", "at": { "x": -96, "y": 137 }, "rotation": 45 }
```

| Prop | Checked | Build | Stand / look points (local) |
|---|---|---|---|
| **Leather shop**, 6x5, front faces east to the South road | South road edge 5.9 m; Tanner house 6.8; the inn 8.1; Creek field 9.1; nearest grove 74 m; **creek line 19.9 m (needs 14): must not move south of y = -37 or be given a `size`** | Stone sill; plank walls on three sides; thatched gable with a deep front eave; open front with a counter 2.6 x 0.6; a rail hung with belts, a bridle and four bags; a cutting bench with a hide; a stitching clamp; rolled hides; a hanging sign (a satchel). `Solid` on walls and counter. | counter: stand (0,0,-.9) look (0,1.1,-2.6); bench: stand (-1.2,0,.5); stitching: stand (1.2,0,-.3) |
| **Drying hut**, 5x4.5, faces south-east | fence 3.6 m; road 3.8; Last harvest field 4.1; tree (15,38) trunk 4.8; Crane house 9.4; nearest grove 36 m; secret 19 m; creek 76 m | Wattle panels in a timber frame, steep thatch, open doorway; eight hanging herb bundles; two drying racks; a bench with a mortar, three jars and a small still (`Glow`). `Solid` on the hut and bench. | bench: stand (1.4,0,-2.3); racks: stand (-1.6,0,-2.2) |
| **Kitchen**, 5x3 lean-to on the inn's back wall (inn-local x 0.5 to 5.5) | abuts the inn at exactly this point (a data test checks within 0.3 m); hedge 3.0 m; Tanner house 3.5 m, leaving a lane past the Tanners' door; creek 34 m | Two posts, a mono-pitch slate roof; a stone range against the wall with embers, a pot on a crane, a short stack with `Smoke`, `Glow`; a work table; two hares from the rafter; a water butt; a firewood stack. `Solid` on range, table and posts. | range: stand (.9,0,-.2); table: stand (-1.2,0,-.3); **three hand-over spots** at (-1,0,-2.2), (0,0,-2.2), (1,0,-2.2) outside the front (three hen-wives arrive together) |
| **Game rack**, 3x2, in the Lodge yard | lodge wall 2.1 m; Ridge path 2.3; lamp 1.2; **Mastwood rectangle 7.8 m from the centre (needs 7)**; creek 197 m | Two forked poles and a crossbar with hares and a brace of birds; a laced hide; a chopping stump; a ring of cold stones. Collider on the stump only. | stand (0,0,-1.1) |

### B.3 The inn: bar, rooms, kitchen wall, innkeeper

`Inn()` additions (no random draws; Khaven's inn gets the bar spot and rooms door too, with nobody to use them):
- `Workplace(t, "bar", stand (-w/2 + 5.3, 0, d/2 - 1.4), look (0, 1, -1))`: the open end of the bar.
- A plank door on the inner back wall at local **x = -0.6** (clear of the glass at -1.45 and 0.25 and of the bar top, which ends at -1.3), registered as `ZoneDoor { name = t.name + ", upstairs", kind = "rooms", openable = false, position = local (-0.6, 1, d/2 - .7) }`.
- When the zone has a `kitchen` prop, the back-wall ground-floor windows, frames and shutters at x = 0.7 and 3.3 are not built (the lean-to covers them).

**Hob Linden, Innkeeper** (role `innkeeper`, GAME-ONLY; up at 5.4, abed at 22.8 through the rooms door):

| Hours | Where |
|---|---|
| 5.5 - 7 | kitchen |
| 7 - 11.5 | kitchen, kitchen, bar |
| 11.5 - 14 | bar, bar, kitchen |
| 14 - 17 | kitchen, bar, well |
| 17 - 22.8 | bar, bar, bar, kitchen |

Errands (all inside 4.5 to 21): "water for the pot" (6.0-7.5, well to kitchen, bucket, good `water`): "Bitter or not, it boils."; "the pot on" (10.5-12.5, at the kitchen, 14 s): "Pot's on. Hare if Garet's been by; barley if he hasn't."; "dinner to the tables" (12.2-14, kitchen to inn, bread board, good `dinner`): "Dinner. Mind the bowl; it's hotter than it looks."

Hand-over: errands keep `to = "inn"` and their stock keys. A new optional errand field `door` names a preferred spot: "eggs to the inn", "first loaves to the inn" and "hares to the inn" get `door = "kitchen"` (the kitchen's hand-over spots when the zone has them, else an inn seat as now); the cask gets `door = "bar"`.

Lines (GAME-ONLY): "Ale's thin and the stew's thinner. Sit where you like."; "Mira's in the corner. Don't crowd her; she's the only mender we've got."; "The Cask stood before the Concord and it'll stand after."; with `inn.meat`: "Garet's hares are in the pot. Don't tell the out-of-work."; ale at 0: "Dry. Ama's cask never sees the night out." Vendor: role `innkeeper` sells `food.brown_loaf`, `food.harrow_cheese`.

### B.4 The leatherworker's, herbalist's and hunter's days

- **Leatherworker**: shifts 7-9 `tannery, leathershop`; **9-12 `leathershop`**; 12-13 `inn`; 13-14 `tannery, leathershop`; **14-18 `leathershop`**; 18 to night `green, inn`. So she is at her counter for two fixed blocks. New errand "tanned hides from the yard" (7.5-9, tannery to leathershop, hide, good `leather`): "Three sides, dry enough. Fen says mind the grey one." **The one existing errand that changes is "leather to the stall": it starts from `leathershop` and runs 12.2-14** (was tannery, 11.2-13.5); its id and stock key `stall.goods` are unchanged and no test pins its window. Where she is now is shown in the bag quests' summary line and the bags-window hint.
- **Herbalist**: "herbs to dry" ends at `dryhut` (was home); the 10.5-12.5 and 16.5 shifts gain `dryhut`.
- **Hunter**: dawn shift gains `lodge`; evening shift is `lodge, lodge, inn` (A.2).
- No other existing errand's id, places, window or stock key changes.

### B.5 Code changes (workshops)

| File | Change |
|---|---|
| `Scripts/World/ZoneBuilder.Workshops.cs` (new partial) | `LeatherShop`, `DryingHut`, `Kitchen`, `GameRack`, each ending in its `Workplace(...)` calls; the kitchen registers three `kitchendoor` workplaces. |
| `Scripts/World/ZoneBuilder.cs` | Four `case` lines; the kinds in the `legacy == 0` list, `Footprint` and `Openness`; the `Inn()` additions. |
| `Scripts/Encounter/WorldLife.cs` | `FindPlaces` key list gains `leathershop`, `dryhut`, `kitchen`, `kitchendoor`, `bar`, `lodge`; in the workplaces loop also `placeName[wh.position] = w.name`. An empty `leathershop` shares the `tannery` list. `Needs`: leatherworker `leathershop`, innkeeper `inn`. `TitleFor`, hours and trade lines for the innkeeper. `PlaceFor(Villager, kind)`: places whose `placeName` is one of this villager's `life.workshops`, else any of the kind; for `field`, points within 60 m of the farmer's home when there are any. `GoTo`, `Resolve` and `WorkedPlace` use it (so a delivery to `stall` with `toRole = "merchant"` goes to a merchant's stall, and the baker's loaves to her own). `PoseFor`: leathershop, dryhut, kitchen Knead; bar Talk; lodge Work. |
| `Scripts/Encounter/VillageWork.cs` | `Errand.door`, `Errand.toRole`; the innkeeper's day; the changes of B.4. |
| `Zones/oakhaven.json` | The four props, `life.workshops`, the innkeeper resident (third), two landmarks. |
| `Items/items.json` | Vendor `{ "role": "innkeeper", "items": ["food.brown_loaf", "food.harrow_cheese"] }`. |
| DESIGN section 6.1 | Oakhaven's bench station is the drying hut; the kitchen is a second `fire` station. `Workplace()` maps `dryhut` to bench and `kitchen` to fire beside DESIGN's forge and oven. |

## C. Purses and spending

### C.1 Decisions

- **One purse per household**, spent on the household. For a single tradesperson it is a personal purse; for the Tanners it is "for his family".
- **Not saved.** Purses live in an in-memory table keyed by **save slot + zone + household**. It is cleared by a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` method (the pattern in `EventBus.cs` 66), when a session loads a different character, and by `VillageEconomy.ResetAll()` in every village test's teardown. A restart can never load a stuck economy and the owner's save is not touched.
- **The lasting effect is carried by the saved bags.** Each trade bag the character wears adds **2 coin a day** to the leatherworker's household stipend ("Folk have seen your poke and asked who made it."), and a session's starting purse there is the stipend plus 6 per worn bag. **One bag keeps the Tanners warm for good.**
- Coin is the player's gold, one for one.
- **No coin changes hands for deliveries between trades** in this version (the draft's price list made the outcome depend on errand order). Money enters by stipend and by the player; it moves between households only when one buys bread, firewood or eggs.

### C.2 The day of a purse

`VillageEconomy` (new file, pure C#, no scene; `VillageLife` owns one):

1. **New day** (detected as `Villager.StartErrand` does: the hour has dropped by more than 6): yesterday's "met" flags are kept for lines, today's cleared, then `coin = min(coin + stipend, 40)`. On first use in a session a purse starts at its stipend (plus the bag bonus).
2. **Needs, strictly in order:** bread 2, firewood 3, eggs 1. **A need is bought only if every earlier need was met today; buying stops at the first one the purse cannot cover.** A household never buys what its own trade makes (baker: bread; woodcutter and hunter: firewood; hen-wife and merchant: eggs). The inn does not shop. A need whose seller the zone lacks (no baker, no woodyard, no merchant) counts as met, so Khaven is silent.
3. **Planning:** at the start of the day, and again whenever the purse gains coin, the household walks its unmet needs in order and **claims** each one it can afford (the coin is reserved). The claim is paid, and the need marked met, **at pick-up**, when the goods are in hand. An errand cancelled before pick-up (a flee, bedtime) releases its claim.
4. **Who gets the money:** bread, the baker's household; firewood, the woodcutter's; eggs, Ama Rusk's. It does not take from `stall.eggs`, so the player's eggs and the existing test are untouched.
5. **Errands you can see** (`VillageWork.Shopping`), run only by a household with a spare pair of hands (a member who is not its only tradesperson: the Tanners, the Pells, the Farrows), and only when the need is claimed:
   - the child's existing "a loaf for Mum" (oven, 11-13) is unchanged except that it is skipped with "Mum says there's no loaf today." when bread is not claimed;
   - "bread for the house", 9-18, the baker's stall to home, the bread board, for a household with no child or when the child's loaf was missed: "A loaf, Hedda. The small one.";
   - "firewood for the hearth", 14-19.5, the Woodyard to home, logs on the shoulder, carried by an adult who is not the head (Fen Walker for the Tanners): "A bundle, Osk. Dry, if you've any.";
   - "eggs for the house", 15.5-18.5, the Produce stall to home, the egg basket.
   A single-person household, and any household more than 100 m from the green, is settled on the books at the hour its window opens (no walk), so smiths and merchants stay at their shops.
6. **Chimneys:** every chimney smokes as it does today. A household whose firewood need went unmet has its smoke switched off (emission, not the object) from 17:00 until its next firewood. A door with no smoke (the lodge, the inn's rooms) is skipped.
7. **Short of coin:** the errand is not run, and the member who would have gone says so once ("Can't stretch to firewood today."). Lines to the player (`PurseLine`, tried one talk in three) are spoken **only by a household the player can pay** (a member has wares or a quest): "We've bread. No fire; Nettie sleeps in her coat."; "Cold hearth again tonight. Wood's three coppers we haven't got." Nobody is sent home early; drinkers and the inn are untouched. Nothing the player needs ever waits on a purse.
8. **When the player pays** (a purchase from that villager, or a bag-quest hand-in): the coin goes to the villager's household and it plans again. Inside the errand windows the family sets out: "That's the fire lit tonight. Bless you." After the windows: "That's tomorrow's fire. Bless you.", and the coin is there at the next day's plan. If the purse is over the cap: "More than we'll spend. The tithe-man will have the rest." (GAME-ONLY, in keeping with the Concord's tithes.)

### C.3 What the rules produce (pinned by `VillageEconomyTests`)

No player, purse at the end of each day:

| Household | Stipend | Live needs | Day 1, 2, 3 | Thirty days |
|---|---|---|---|---|
| **Tanner** | 3 | bread 2, firewood 3, eggs 1 | bread; bread; bread and firewood (purse 1, 2, 0) | bread every day, firewood every third day, eggs never |
| Tanner, one bag worn | 5 | same | bread and firewood every day (purse 0) | the same every day; eggs never |
| Tanner, two bags worn | 7 | same | all three, 1 saved a day | all three every day |
| Vell, Crane, Ashby, Pell, Crisp | 6 | all three (6) | all three, purse 0 | every need every day |
| Jory | 2 | bread only | bread, purse 0 | bread every day; his chimney smokes as now |
| Thorne | 6 | firewood, eggs (4) | both; purse grows with the village's bread money | every need; reaches the cap of 40 |
| Farrow | 6 | bread, eggs (3) | both; grows with firewood money | every need; reaches the cap |
| Rusk, Reed | 6 | bread, firewood (5) | both | every need; Rusk reaches the cap sooner (egg money) |
| Harrow, Carder, Brook farms | 6 | bread, firewood (5) | both, on the books | every need |
| Moss | 6 | bread, eggs (3) | both, on the books | every need |
| The Golden Cask | 6 | none | nothing bought | reaches the cap |

So with no player exactly one household, the Tanners, goes cold two nights in three, and it is the one the player can help.

### C.4 Guard rails

| Risk | Rail |
|---|---|
| Deadlock | Stipends come from outside the village; deliveries between trades carry no price. |
| Runaway | Stipend first, then the cap of 40. |
| Negative coin | A claim reserves coin; `Settle` refuses rather than go below 0. |
| The player draining a purse | Selling to a vendor never draws on a purse. |
| Shopping blocked by missing goods | Buying needs coin only, never stock. |
| Test leakage | The table is keyed by slot and zone and reset in teardown; the day is detected by the clock dropping, not by an hour window. |
| Existing tests | No stock key changes; drinkers never pay and are never sent home; `VillageDrinkTests` must pass with every purse at 0. |

### C.5 Code changes (purses)

| File | Change |
|---|---|
| `Scripts/Encounter/VillageEconomy.cs` (new) | `Purse { int coin, stipend; string[] needs; met, claimed, metYesterday; bool cold }`; `VillageEconomy { NewDay(); Plan(household); bool Settle(household, need); Release(household, need); Earn(household, coin); bool Claimed(household, need); const Cap = 40; static ResetAll() }`; the static table and its reset method. |
| `Scripts/Encounter/VillageWork.cs` | `Errand.need`; `VillageWork.Shopping`; `need = "bread"` on "a loaf for Mum". |
| `Scripts/Encounter/WorldLife.cs` | `Economy`; the new-day check in `Update`; `Paid(npc, coin)`; `StartErrand` offers the household's claimed shopping errands after the role's own; `ErrandArrive` settles at pick-up; `CancelErrand` releases; books settlement for households without hands; `PurseLine` in `LineFor`; `Household.ApplyHearth()` toggles `door.smoke` emission. |
| `Scripts/Encounter/EncounterSession.cs` | `Buy`: after a successful purchase, `VillageLife.Active?.Paid(VendorNpc, price)`. `CompleteQuest`: a bag quest pays the bag's price the same way. |
| `Zones/oakhaven.json` | `stipend: 3` on Tanner; `stipend: 2, needs: ["bread"]` on Jory. |

## D. The leatherworker's bags

### D.1 What a trade bag is

A trade bag is an item you **wear** (use it once; it leaves your bags and is recorded on the character, like DESIGN's tools). It adds slots that take one class of material. The 24 ordinary slots are untouched.

| Item id | Name (GAME-ONLY) | Holds (class) | Slots | Value / price | Quest leathers |
|---|---|---|---|---|---|
| `bag.simples_wallet` | Simples-wallet | `herb`: the five herbs, vials | 6 | 3 / **12 gold** | 3 Grey wolf pelts |
| `bag.log_sling` | Log-sling | `timber`: the five logs | 6 | 4 / **16 gold** | 3 Hill-deer hides |
| `bag.larder_scrip` | Larder-scrip | `larder`: raw meats, flour, salt, eggs, cheese | 8 | 5 / **20 gold** | 5 Coney skins |
| `bag.ore_poke` | Ore-poke | `ore`: ores, bars, charcoal | 8 | 6 / **24 gold** | 3 Boar hides |

At these prices buying is a real shortcut at level 2 (a common weapon is 24 gold), and either route pays the Tanners the same: the purse is credited with the bag's **price** on a purchase and on a quest hand-in ("what's left over is my profit").

Rules:
- New `ItemDef` fields: `pouch` (the class, on materials); on bags `holds` and `slots`. New kind `bag`.
- A picked-up material tops up an existing stack first, then an empty slot of a worn bag that holds its class, then an ordinary slot.
- Nothing else enters a trade-bag slot ("Only ore, bars and charcoal go in the ore-poke.").
- One of each. A bag you wear **or carry** is left off Maud's list. Using a second: "You already carry one."
- Wearing is permanent. "N free" counts ordinary slots only.

### D.2 Storage

Trade-bag slots are appended to `progress.bag` after index 23 in the order worn, and `progress.pouches` (a list of bag ids) records the order. Index-based operations work unchanged. Changes are confined to `Inventory`: `EnsurePouches` (pads, never truncates), `Accepts(index, ItemDef)`, `Add` and `Room` (stacks, then accepted trade-bag slots, then ordinary), `Move` (refused into a slot that does not accept), `Unequip` and `FreeSlots` (first 24 only), `Wear`.

### D.3 Leathers

All hides become `"kind": "material", "quality": 1, "trade": "tannery.hides"` so "Sell junk" cannot eat them (this overrides DESIGN's "pelts and hides stay junk"). Existing ids, names, values and stacks are unchanged, so `ItemTests` keeps its pelt and wolf-loot asserts. A hide's tooltip and right-click line: "Leather. Maud Tanner in Oakhaven works it." (not DESIGN's "A crafting material. Press K."). Selling hides in a village counts as a delivery to the tannery and the skinner remarks on it. The full list of hides and animals is in section G.

### D.4 Getting one

**Buy it:** vendor `{ "role": "leatherworker", "items": ["bag.simples_wallet", "bag.log_sling", "bag.larder_scrip", "bag.ore_poke"] }`. Talk to Maud at her counter (9-12 and 14-18), wherever else she is, or knock at the Tanner house when she is home (A.4).

**Earn it:** four one-time quests, giver and turn-in the leatherworker, zone Oakhaven, GAME-ONLY. Ids are keyed on the role so the answer to open question 1 is a data change.

| Quest id | Title | Level | Bring | Reward | Ships with |
|---|---|---|---|---|---|
| `npc.leatherworker.wallet` | A Wallet for Simples | 1 | 3 Grey wolf pelts | Simples-wallet, 20 XP, Oakhaven +75 | the bags step |
| `npc.leatherworker.sling` | A Strap for the Woodyard | 1 | 3 Hill-deer hides | Log-sling, 25 XP, +75 | the hunting step |
| `npc.leatherworker.scrip` | The Cook's Scrip | 2 | 5 Coney skins | Larder-scrip, 25 XP, +75 | the hunting step |
| `npc.leatherworker.poke` | Ore Wants a Stout Bag | 2 | 3 Boar hides | Ore-poke, 30 XP, +75 | the hunting step |

Text for the first (the others follow it):
- Offer: "\"You've the look of someone who picks things up. Herbs, is it? They'll bruise to nothing loose in a pack.\"\n\n\"Bring me three wolf pelts, whole, not grey at the edges, and I'll cut you a wallet for them. No charge but the hides; what's left over is my profit, and the gods know I need one.\""
- Summary: "Bring Maud Tanner three grey wolf pelts. Wolves run in the Harrow wood and the North pines. She keeps shop by the South road, 9 to 12 and 2 to 6."
- Progress: "\"Three, I said. The wolves won't skin themselves.\""
- Complete: "Maud turns each pelt to the light and runs a thumb along the edge. \"These'll do. Sit. It's an hour's work if Nettie holds the lamp.\" The wallet she hands you smells of oak bark and smoke. \"The offcuts will buy the bread this week. So that's both of us better off.\""

Quest-system additions (`Quests.cs`):
- objective type **`bring`**: `item` is a bag item id, `count`, `target` an NPC. Progress is `min(count, Inventory.Count)`; talking to the target with enough hands them over and completes it; the NPC shows a gold `?` when you carry enough;
- **`rewards.bagItems`**: bag items given at turn-in. `CompleteQuest` first checks `Inventory.Room` and refuses with "Make room in your bags first." (nothing is removed);
- **`unlessWorn`**: a quest is not offered while its bag is worn or carried. If an accepted quest's bag is worn by turn-in, the reward is the bag's value in gold instead: "You've one already. Take the coin.";
- the session and a data test cross-check `bring` items, `bagItems` and `unlessWorn` against the item database.

### D.5 Save format

- New saved list `pouches`; `bag` may be longer than 24 (the reader allows 96; four bags make 52).
- **Carried in DESIGN's format 8**: `AddProfessionsMigration` inserts `"pouches":[]` together with `"professions":[]`, so the owner's save is backed up once and migrated once. The build plan puts both in the same step.
- `Read` guards: null becomes empty; blanks and later duplicates dropped; more than 8 entries or a bag over 96 slots refuses the save ("Invalid item data.", file untouched). An unknown bag id is kept and ignored; slots past the known ranges accept nothing new but can be emptied.
- Not saved: purses, household flags, chimneys, hunted animals.

### D.6 UI

- Bags window: under the 24 slots, one labelled row per worn bag ("Ore-poke 3/8"). With none worn: "Trade bags: Maud Tanner makes them, by the South road in Oakhaven."
- Tooltips: "Trade bag: 8 slots for ore, bars and charcoal. Use it to wear it."; on materials "Goes in an ore-poke." Glyph "Bg".
- Right-click a bag with no vendor open: wear it ("You sling the ore-poke at your hip.").
- The Trades window shows under each gathering skill "Ore-poke: worn" or "No ore-poke (Maud Tanner, Oakhaven)".

### D.7 Code changes (bags)

| File | Change |
|---|---|
| `Scripts/Encounter/Items.cs` | `ItemDef.pouch`, `holds`, `slots`; the `Inventory` changes of D.2. |
| `Scripts/Encounter/EncounterProgress.cs` | `public List<string> pouches = new List<string>();` |
| `Scripts/Encounter/EncounterSave.cs` | the migration text and guards of D.5. |
| `Scripts/Encounter/Quests.cs` | `bring`, `rewards.bagItems`, `unlessWorn`. |
| `Scripts/Encounter/EncounterSession.cs` | `EquipFromBag`: kind `bag` calls `Inventory.Wear`; `OpenVendor` leaves worn or carried bags off the list; `CompleteQuest` room check, bag rewards, the gold fallback and the `Paid` call; `EnsurePouches` after load; the hide line. |
| `Scripts/Encounter/EncounterHud.Items.cs` | trade-bag rows, tooltips, glyph. |
| `Items/items.json`, `Quests/oakhaven.json` | four bags, `pouch` on DESIGN's materials, hides as materials, the leatherworker vendor, the quests. |

## G. Hunting for hides (the owner's later request)

"All animals are huntable for their leather"; "farm animals stay, not huntable."

### G.1 What changes for the player

- **Deer and rabbits can be targeted and killed.** They still bolt when you come near (sneaking with Ctrl halves the distance at which they notice you, so a hunt is a short stalk), bolt again when hit, and come back after about four minutes.
- **Chickens, sheep and the village cats are never huntable.** Crows are left alone too (no leather).
- **Every beast gives its own hide** when you search the body (the existing E on a corpse; the prompt for a beast reads "Skin the body"). No knife and no skinning skill in this version (open question 3).
- No experience and no coin from game animals, so nobody levels on rabbits.

| Animal | Where | Hide (GAME-ONLY) | Value | Chance |
|---|---|---|---|---|
| Rabbit (game) | Oakhaven, Khaven, the Peaks, Verdant Shore | `hide.coney` Coney skin (new) | 1 | always |
| Deer (game) | Oakhaven, Khaven, the Peaks, Verdant Shore | `hide.hill_deer` Hill-deer hide (new) | 2 | always |
| Grey wolf | Oakhaven, Khaven, the Peaks | `junk.wolf_pelt` Grey wolf pelt (existing id) | 2 | 0.7 as now |
| Wild boar | Oakhaven, Khaven, the Peaks | `hide.boar` Boar hide (new entry in the `boar` table) | 2 | 0.7 |
| Ash-hound | Ashland Rim | `junk.ash_hide` (existing) | 5 | as now |
| Mossback boar | Verdant Shore | `junk.moss_hide` (existing) | 7 | as now |
| Stag | Verdant Shore | `junk.dappled_hide` (existing) | 8 | as now |

Hides sell to any vendor at value; the leatherworker is a vendor from the bags step, and the skinner becomes one here (role `skinner` sells `mat.salt`), so both buy them and the sale feeds `tannery.hides`.

### G.2 How it is built

- The `deer` and `rabbit` groups in each zone's `life.critters` are spawned as **game animals**: an `EncounterEnemy` with `Game = true` (an actor with health, a collider and a NavMesh agent, so targeting, abilities and the corpse search all work as for a camp mob), wearing the body `Critter` builds today (its body and gait code is lifted into a shared `CritterBody`). Health: rabbit 15, deer 70. Respawn 240 s inside the group's circle, through the existing camp respawn.
- A game animal never fights: no threat, no proximity aggro, no swing; its only behaviour is the critter's graze, wander and bolt.
- Game animals live in a new list, `Session.Game`, **not** in `Session.Enemies`, so `VillageLife.EnemiesCleared`, `EnemyNear`, `Danger`, "zone is clear" and `InCombat` do not see them. Targeting (click, Tab) and `LootableCorpse` look through both lists.
- Loot tables: new tags `rabbit` and `deer` with `gearChance` 0 and the hide at chance 1; `hide.boar` added to `boar`.
- The first task of the step is a short read of `SpawnCamps`, `TalkTarget`, the Tab-target code and `EnemyDied` to confirm the second list is enough; this part of the design was written from `EncounterEnemy.cs` and `WorldLife.cs` only.

### G.3 Code changes (hunting)

| File | Change |
|---|---|
| `Scripts/Encounter/EncounterEnemy.cs` | `Game` flag; in `Update` a game animal skips threat and swings and runs the bolt and wander behaviour; `Receive` makes it bolt. |
| `Scripts/Encounter/WorldLife.cs` | `CritterBody` (the body and gait lifted out of `Critter`); `Init` hands `deer` and `rabbit` groups to the session instead of spawning critters. |
| `Scripts/Encounter/EncounterSession.cs` | `Game` list; `SpawnGame(group)`; targeting and `LootableCorpse` over both lists; `EnemyDied` and `LootCamp` give a game animal no XP or coin; the "Skin the body" prompt for beasts. |
| `Items/items.json`, `Quests/oakhaven.json` | three hides, two loot tables, the boar entry, the skinner vendor, the three remaining bag quests. |

## F. Open questions for the owner (recommended default in bold)

1. **She is already in your published build as Maud Tanner, a woman; you wrote "him".** **Default: keep Maud; her husband is the skinner, Fen Walker, and their child is Nettie, so it is "her family".** Alternative: a male leatherworker with Maud as his wife at the counter (a data change: quest ids and tests are keyed on the role).
2. **To give every villager a household I invented kin: Old Tobin and Edda Pell are married; Pim is Osk Farrow's son; Hettie Brook is Wil Carder's aunt; Goody Marl is Sel Harrow's aunt; Ilse Brandt is the Harrows' farmhand; Jory lives alone.** **Default: accept as GAME-ONLY; two new houses are built and the invented families share.** Alternative: tell me who lives with whom; each household split off needs one more new house.
3. **Skinning.** **Default: a hide comes from searching the body, with no knife and no skinning skill.** Alternative: a skinning knife from the skinner and a Skinning skill 1-100 like Mining (about one more build step).

## Known risks

- All seven new positions were checked against the zone data by script, not in the engine: slopes, the lane between the kitchen and the Tanner house, the drying hut's 2.8 m to the nearest canopy and the cottage's 4.5 m to a tree need eyes in the validation copy.
- Three positions sit close to a limit: the leather shop and the Crisp cottage to the creek rule (5.9 m and 2.4 m to spare) and the game rack to the Mastwood (0.8 m to spare). `VillageStreamTests` is the check; nudge away from the limit, never toward it.
- A Concord collector stands 9 m from the Crisp cottage until killed. The miller still walks home past him, as he passes him at the mill today.
- A new house may cover a random meadow or wander point, which shifts where some villagers idle. No test pins those positions.
- Farm and lodge households walk 75 to 170 m home; a villager who flees a fight runs that far.
- The purse numbers are few and pinned by a table, but the feel (how often you see Nettie with a loaf, how the cold chimney reads at dusk) is unplayed.
- Hunting was designed from two files; the step begins with a short read of the targeting code (G.2).
- The loot and gear design in `tools/wip/loot` shares `Items.cs`, `items.json`, the save and the HUD's item panels with the bags and professions steps; it should land between steps, not beside them.
