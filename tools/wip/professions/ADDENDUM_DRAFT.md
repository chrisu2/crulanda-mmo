# Trades of Crulanda, addendum: households, workshops, purses and the leatherworker's bags (draft)

Status: design only. Nothing was run or changed in the repo; no Unity, build or test was run. It extends `tools/wip/professions/DESIGN.md` (called DESIGN below). All new names, kin and lines are **GAME-ONLY**. Every coordinate is **PROVISIONAL** until the placement tests pass in the validation copy. Line numbers are from today's working tree, which has uncommitted village work in it (and `ZoneBuilder.cs` was changing while this was written), so treat them as approximate.

## 0. What was checked in the code, and what it means

| Fact (file) | Consequence for this design |
|---|---|
| `VillageLife.Init` gives homes by `Homes[i % Homes.Count]`; a hen-wife gets the house nearest her coop (WorldLife.cs 73, 94). `Homes` is every non-openable `ZoneDoor`, in prop order. | Replaced by named households. The old rule stays only as the fallback for a zone with no `households` block. |
| Oakhaven roles by index: 0 Brannoc Vell blacksmith, 1 Wil Carder farmer, 2 Ama Rusk merchant, 3 Hedda Thorne baker, 4 Sel Harrow gossip, 5 Garet Moss hunter, 6 Old Tobin drinker, 7 Pim child, 8 Osk Farrow lumberjack, 9 Lisbet Crane herbalist, 10 Ilse Brandt farmer, 11 Corwin Ashby elder, 12 Aldo Crisp miller, 13 **Maud Tanner leatherworker**, 14 Fen Walker skinner, 15 Edda Pell gossip, 16 Nettie child, 17 Tamsin Reed merchant, 18 Jory drinker, 19 Grete Lowe farmer. Hen-wives by coop: Goody Marl (Harrow coop), Hettie Brook (West field coop), Nan Pennock (Brook coop). Residents: Quill (inn), Warden Ivel (post). | The leatherworker in the published build is a woman, Maud Tanner. See open question 1. No quest or test names Maud, Fen, Nettie, Pim, Jory, Edda, Ilse, Grete, Osk or Tamsin, so their households are free to set. |
| `House()` draws nothing from the zone's random stream (no `R01` between `Eaves` and `Barn`). `BuildProps` has a private-stream list (`legacy == 0`) for new kinds. `NearProp` makes a grove tree `continue` **before** its draws when it is within `max(size)/2 + 5` m of any prop. | New buildings are appended to the end of `props`, new kinds go in the private-stream list, and every new prop is kept more than `max(size)/2 + 5.5` m from every non-orchard grove rectangle. Then no tree, bush or rock moves. A test pins it. |
| `Moss's lodge` exists: prop 131, kind `barn`, (-93,143), 7x5, rot 45, with a `Lodge yard` clearing (-100,135) and a landmark. Garet Moss is the hunter. `Barn()` registers no door. | It is the hunter's lodge already. It becomes his home (a door is registered for it) and gets a small game rack as his workplace. It is 170 m from the green: about one game hour's walk (a game hour is 100 s). |
| `Inn()` has a bar with barrels 0.2 m behind it (nobody can stand there), a hearth on the +X wall, three tables, no stairs, and adds only table seats to `Places["inn"]`. | The innkeeper stands at the bar's open end. A "rooms" door on the inner back wall is where inn-dwellers go to bed. The kitchen is a separate lean-to prop on the back wall. |
| Existing lines: `Reply("meat")` says "Round the back, before the drinkers see."; the egg errand says "eggs for the Cask's kitchen"; the hunter says "Pay me for the hide." and the taker "I'll pay fair." | A kitchen round the back and coin for deliveries are already in the village's voice. |
| `VillageWorkTests` pins a fixed `Places` set, the errand goods keys (`inn.eggs`, `stall.eggs`, `tannery.hides`, `stall.goods`...), the hen-wife's errand ids and that every errand ends by 21:00. `VillageErrandTests` waits on `Count("inn.eggs")` and `Count("stall.eggs")`, and expects fresh eggs first in the merchant's stock. `VillageDayTests` (23:30) requires every villager who is not a drinker and not a post-resident to be hidden. `VillageDrinkTests` needs drinkers to drink until `inn.ale` is 0. | Errand `to` strings and stock keys do not change (the kitchen is a preferred hand-over spot of `inn`, not a new destination). The innkeeper needs a home and must be abed by 23:30. Drinkers never need coin. The one test edit needed is adding the new place names to the `Places` set. |
| Quests: one-time only (`questsDone`); `collect` and `deliver` count the quest bag (`questItems`), not the bags; `rewards.items` are quest items. Wolf pelts and hides are bag items (`junk.*`, quality 0), so "Sell junk" sells them. | The bag quests need a new objective type that hands over bag items, a bag-item reward, and the four hides must stop being junk. |
| `EncounterSave.Read` already accepts `bag.Count` up to `BagSize * 4` (96). The HUD draws only the first 24 slots. `ItemTests` pins a fresh bag at 24 and the wolf pelt's stack of 10. | Trade-bag slots are appended to `progress.bag` after slot 24. Fresh characters still have 24. Pelt id and stack stay. |
| Vendors: `StockFor(name, role, band)`; the till is whoever you talked to (`VendorNpc` is a name). | A purchase is credited to that person's household. No vendor-system rewrite. |
| No canon innkeeper for the Golden Cask (only the name is canon, book1 ch.4). | The innkeeper is invented: **Hob Linden** (name 20 of the default list, unused in Oakhaven), GAME-ONLY. |

## A. Households and homes

### A.1 Data shape (zone JSON, `life`)

```json
"life": {
  "villagers": 20,
  "households": [
    { "name": "Tanner", "house": "Tanner house", "shopper": "Nettie", "canonStatus": "GAME-ONLY",
      "members": [ { "name": "Maud Tanner", "kin": "head" }, { "name": "Fen Walker", "kin": "husband" }, { "name": "Nettie", "kin": "daughter" } ] }
  ],
  "workshops": [ { "who": "Maud Tanner", "prop": "Tanner's leather shop" } ],
  "residents": [ { "name": "Hob Linden", "role": "innkeeper", "title": "Innkeeper", "works": true } ]
}
```

- `house` is a prop **name**. A `house` prop's door already carries the prop's name. For a `barn`, `mill` or `inn` named as a home, the builder registers a home door (A.6).
- `members[].name` matches the spawned villager's name (default list by index, hen-wife names by coop index, residents by name). `kin` is free text for lines: head, wife, husband, daughter, son, father, aunt, hand, lodger.
- `shopper` (optional): who fetches bread and eggs. Default: the first child, else the first member who is not the head, else the head.
- `works: true` on a resident: an ordinary villager with that role's day and a home, not a post (used for the innkeeper). Residents with `place` or `at` keep their post day and night as now and need no household.
- `workshops` says which workplace prop is whose (section B).

Rules checked by a data test: every villager who is not a post-resident is in exactly one household; every `house` names an existing prop of kind house, barn, mill or inn; no two households share a house; household names are unique in a zone.

### A.2 Oakhaven: the household list (27 people: 20 villagers, 3 hen-wives, the new innkeeper, Quill, Warden Ivel, plus Mira)

The ten village houses are unnamed today (props 4-13). They get names (adding `name` to a prop changes no random draw).

| Household | House (prop, position) | Members (kin) | Notes |
|---|---|---|---|
| Vell | prop 10 (17,-25) "Vell house" | Brannoc Vell, blacksmith (head) | 12 m from his smithy. |
| Thorne | prop 12 (-26,-4) "Thorne house" | Hedda Thorne, baker (head) | 11 m from the bakehouse. |
| **Tanner** | prop 13 (-29,-21) "Tanner house" | Maud Tanner, leatherworker (head); Fen Walker, skinner (husband); Nettie, child (daughter) | The owner's example family. Nettie fetches the loaf; Fen carries the firewood. See open question 1. |
| Rusk | prop 8 (36,5) "Rusk house" | Ama Rusk, merchant (head) | By the well. |
| Reed | prop 4 (-22,14) "Reed house" | Tamsin Reed, merchant (head) | Beside her Cloth and pots stall. |
| Farrow | prop 5 (-16,28) "Farrow house" | Osk Farrow, woodcutter (head); Pim, child (son) | On the North road, toward the Woodyard. |
| Crane | prop 6 (12,25) "Crane house" | Lisbet Crane, herbalist (head) | Her drying hut stands 9 m away. |
| Ashby | prop 7 (28,19) "The Elder's house" | Corwin Ashby, elder (head) | The largest house (9x6). |
| Pell | prop 11 (8,-31) "Pell house" | Old Tobin, drinker (head); Edda Pell, gossip (wife) | Makes the drinkers' lines true ("what I tell the wife", "before she locks the door"). |
| Jory | prop 9 (30,-12) "Jory's house" | Jory, drinker (head) | Lives alone; the poorest purse in the village. |
| Harrow farm | prop 62 "Harrow farmhouse" (-94,48) | Sel Harrow, gossip (head); Ilse Brandt, farmer (hand); Goody Marl, hen-wife (aunt) | "The Harrows" of the quest text; Goody Marl keeps the Harrow coop. |
| **Carder farm** | **NEW** "Carder farmhouse" (-61,47) | Wil Carder, farmer (head); Hettie Brook, hen-wife (aunt) | West field coop is 19 m south. Wil's quest says the raiders "had my barn" down the North road: the Tithe barn is 17 m east. |
| Brook farm | prop 64 "Brook farmhouse" (58,-102) | Grete Lowe, farmer (head); Nan Pennock, hen-wife (lodger) | Brook coop is beside it. |
| Crisp | prop 61 "Oak creek mill" (-70,-52) | Aldo Crisp, miller (head) | He lives over the stones (the quests' Tam sleeps there; Tam is not spawned). |
| Moss | prop 131 "Moss's lodge" (-93,143) | Garet Moss, hunter (head) | See open question 3. |
| The Golden Cask | prop 1 "The Golden Cask" | Hob Linden, innkeeper (head); Quill (lodger); Mira (lodger, not a villager) | Hob sleeps through the "rooms" door. Quill keeps his post as now. |
| (none) | none | Warden Ivel | A post-resident at (-80,83); stands his post day and night as now. |

Result: every one of the 20 villagers, the 3 hen-wives and the innkeeper has a named home; nobody shares a door by index. **Only one new house is needed** (the Carder farmhouse), not a couple: the ten village houses exactly fit the ten village households once the farmers move to their farms, the miller to the mill and the hunter to his lodge.

### A.3 The new house: Carder farmhouse

`{ "kind": "house", "name": "Carder farmhouse", "at": { "x": -61, "y": 47 }, "rotation": 330, "size": { "x": 8, "y": 5.5 }, "variant": 2 }` appended to the end of `props`. Door faces south-south-east, toward the Tithe barn yard and the West field coop.

Checked with a script against `oakhaven.json` (footprint 9.6 x 7.1 with eaves; clearances are edge to edge):

| Checked | Result |
|---|---|
| All 182 props within 45 m (houses, barns, trees, rocks, herbs, fences, hedges, coops, carts, crates, lamps) | nearest: Yarrow herb (-70,44) 4.0 m; pine (-52,52) 4.0 m; Harrow barn 5.5 m; Harrow grain bin 5.9 m; Tithe barn 6.8 m; tree (-58,34) 7.2 m |
| 13 roads, Oak creek, both ponds | all more than 10 m (Harrow track about 20 m) |
| 13 fields | all more than 9 m (Stubble field, Harrow stubble) |
| 20 groves | nearest non-orchard grove (Harrow wood) 41.6 m: no `NearProp` effect on any tree. The Old Orchard starts at y 77 (orchard trees do not test props) |
| Clearings, shapes, tall grass, secrets, camps, the five story enemies, exits, spawns | Barn yard clearing 13 m; collector at (-37,33) 22 m; nothing else within 20 m |
| Ruins and graves (`ClearOfBuildings` moves a ruin that stands in a building) | chapel wall and graves are at (1..6, 46..53): 60 m away |

It stands at radius 77, outside the flat 58 m, so `House()` builds its plinth down the slope as it does for the Harrow farmhouse. Known side effect: one of the village's random "meadow" stand points may fall under it, which changes where some villagers idle (not layout, not pinned by a test).

### A.4 Naming, door prompt, map

- Prop names are the door names, so the prompt becomes "Knock · Tanner house" with no code change to the prompt.
- Knocking answers by household (`VillageLife.KnockLine(door)`), falling back to today's four barred-door lines for a door with no household:
  - someone is in: "A voice through the planks: 'Maud's at the shop by the South road. Try there.'" (names where the head is at this hour: their activity's place)
  - all abed: "The Tanners are abed. A child coughs, and somebody hushes her."
  - nobody home: "No answer. The Tanner house is empty till supper."
- Map: no labels for houses (ten more labels would bury the green). New landmark labels only for "Carder farm" (-61,45, radius 9) and the new workshops (section B), each `canonStatus: "GAME-ONLY"`.

### A.5 Khaven, and zones with no houses

Khaven (5 houses, 6 villagers, 3 post-residents; no new building):

| Household | House | Members |
|---|---|---|
| Grane | prop 11 (-4,-16) "Grane house" | Hollis Grane (the smith with no forge; drinks) |
| Vey | prop 12 (11,-14) "Vey house" | Dorra Vey (farmer with no field; drinks) (head); Old Kestrel, gossip (father) |
| Jenn | prop 13 (-12,-19) "Jenn house" | Mattock Jenn, merchant (head); Siv Harl (baker with no oven; drinks) (wife) |
| Tabor | prop 14 (3,22) "Tabor house" | Wynn Tabor, hunter (head) |
| Crypt-Keeper's Hovel | prop 10 (21,7) | Ansel Morrow (keeps his post day and night as now; listed so the knock line names him) |

Wenna Coyle and Cato Brisk keep their posts day and night as now (nominally lodging at the Cracked Hearth). Khaven gets no innkeeper, purse errands or new workshop in this addendum; its purses exist but have nothing to buy.

Zones with no `households` block (Peaks, Ashland Rim, Verdant Shore today: 0 villagers, post-residents only): nothing changes. The derived rule for any future zone with villagers but no block: villager `i` gets house `i` while houses last; the rest lodge at the inn (today's "a bench at the inn"); a hen-wife joins the household of the house nearest her coop. Nobody is ever given a door by modulo again.

### A.6 Code changes (homes)

| File | Change |
|---|---|
| `Scripts/World/ZoneDefinition.cs` | `ZoneLife.households`, `ZoneLife.workshops`; `[Serializable] ZoneHousehold { string name, house, shopper, canonStatus; ZoneMember[] members }`, `ZoneMember { string name, kin }`, `ZoneWorkshop { string who, prop }`; `ZoneResident.works`. |
| `Scripts/World/ZoneDoor.cs` | `ZoneDoor.kind` ("house" default, "rooms", "home"); `public ParticleSystem smoke` (used in section C). |
| `Scripts/World/ZoneBuilder.cs` | `House()`: keep the `Smoke` it makes on the door (`smoke`). New `BuildHomeDoors()` after `BuildProps()`: for each household whose `house` is a barn or mill prop, add `ZoneDoor { name, kind = "home", openable = false }` at the prop's front (local (0, 1, -d/2 - .6)), draws nothing. `Inn()`: the rooms door (B.3). |
| `Scripts/Encounter/WorldLife.cs` | `public readonly List<Household> Households`; `Household { name, def, ZoneDoor house, List<Villager> members }`; `HouseholdOf(string name)`. `FindPlaces`: `Homes` keeps only doors of kind "house" (so the fallback is unchanged by the new door kinds). `Init`: build households first; `home = HomeFor(name)`; without a block, the derived rule above; residents with `works` spawn with `fixedPlace = null` and their household's home; the hen-wife takes her household's house, else the nearest as now. `Villager.Home` and `Villager.Household` getters (tests). `KnockLine`. `Paid(string npc, int coin)` as an empty method (section C fills it; lets the bags step call it early). |
| `Scripts/Encounter/EncounterSession.cs` | Knock: `VillageLife.Active?.KnockLine(door) ?? BarredDoorLines[...]`. |
| `Zones/oakhaven.json`, `Zones/khaven.json` | House names, the Carder farmhouse, `life.households`, the "Carder farm" landmark. |

Bed and meals at home: bed is unchanged code (everyone already walks to `home` and hides); it now leads to the right door. Children and hen-wives already dine at home. One addition in section C: a household that has bread today takes its midday at home half the time.

## B. A workshop per trade

### B.1 The table

| Trade (who) | Workshop | Prop | Place key(s) | Player station |
|---|---|---|---|---|
| Blacksmith (Brannoc Vell) | Vell's smithy (8,-17) | existing `forge` | `forge` | **forge** (DESIGN) |
| Baker (Hedda Thorne) | Thorne's bakehouse (-35,-11) and the Bread stall (-12,19) | existing `oven`, `stall` | `oven`, `stall` (owned) | fire (the oven, DESIGN) |
| Merchant (Ama Rusk) | Produce stall (2,15) | existing `stall` | `stall` (owned) | none |
| Merchant (Tamsin Reed) | Cloth and pots (-12,12) | existing `stall` | `stall` (owned) | none |
| Skinner (Fen Walker) | Tannery yard (-58,-16) | existing `tannery` | `tannery` | none |
| **Leatherworker (Maud Tanner)** | **NEW Tanner's leather shop** (-20,-33) | new kind `leathershop` | `leathershop` | **leather** (reserved for a future leather craft; where bags are sold) |
| **Herbalist (Lisbet Crane)** | **NEW Lisbet's drying hut** (23,37) | new kind `dryhut` | `dryhut` | **bench** (the alchemy bench; replaces DESIGN's "Lisbet's drying bench (6,19)" station) |
| **Innkeeper (Hob Linden)** | The Golden Cask: the bar, and the **NEW Cask's kitchen** (-20,-18.3) | `Inn()` addition; new kind `kitchen` | `bar`, `kitchen` | **fire** (the kitchen range, as well as the taproom hearth of DESIGN) |
| **Hunter (Garet Moss)** | Moss's lodge (-93,143) with the **NEW game rack** (-96,137) | existing `barn` + new kind `gamerack` | `lodge` | none |
| Woodcutter (Osk Farrow) | Woodyard (-24,60) | existing `woodpile` | `woodpile` | none (charcoal burns at a forge or fire) |
| Miller (Aldo Crisp) | Oak creek mill | existing `mill` | `mill` | none |
| Farmers | their farm's fields | existing fields | `field` (nearest the farm, B.4) | none |
| Hen-wives | their coop | existing `coop` | yard, nest, trough, pan | none |

### B.2 New props: position, checks, build notes

All four are appended to the end of `props` after the Carder farmhouse, have no `size` (so `NearProp` treats them as a 5 m circle), and their kinds join the private-stream list in `BuildProps` (`legacy = 0`) and `Footprint()` (so no rain falls inside). Front faces -Z as for the other trades.

```json
{ "kind": "leathershop", "name": "Tanner's leather shop", "at": { "x": -20, "y": -33 }, "rotation": 270 },
{ "kind": "dryhut", "name": "Lisbet's drying hut", "at": { "x": 23, "y": 37 }, "rotation": 315 },
{ "kind": "kitchen", "name": "The Cask's kitchen", "at": { "x": -20, "y": -18.3 }, "rotation": 41 },
{ "kind": "gamerack", "name": "Moss's game rack", "at": { "x": -96, "y": 137 }, "rotation": 45 }
```

| Prop | Checked (same script and lists as A.3) | Build (boxes, timber, masonry, thatch) | Stand / look points (local) |
|---|---|---|---|
| **Leather shop**, 6x5 footprint, front faces east to the South road | tree (-30,-33) 5.5 m; South road edge 5.9 m; Tanner house 6.8 m; signpost 7.7 m; the inn 8.1 m; fence 9.2 m; Creek field 9.6 m; Mill lane 10.4 m; nearest grove 74 m. It is the first building a new character passes coming up the South road. | Stone sill; three plank walls 2.4 m (back and sides) on corner posts; thatched gable roof with a deep front eave; open front with a plank counter 2.6 x 0.6; a rail under the eave hung with belts, a bridle and four bags (cloth-tinted boxes and capsules); a cutting bench at the back with a hide on it; a stitching clamp on a stool; rolled hides in a side rack; a hanging sign: a leather satchel on a bracket. `Solid` on the walls and the counter; a 0.9 m gap at each end of the counter. | counter: stand (0,0,-.9) look (0,1.1,-2.6); cutting bench: stand (-1.2,0,.5) look (-1.2,.9,1.2); stitching: stand (1.2,0,-.3) look (1.2,.8,.4) |
| **Drying hut**, 5x4.5, front faces south-east to the Road into the grey | tree (15,38) 2.8 m (trunk 4.8 m); fence 3.6 m; road edge 3.8 m; rock (30,42) 4.4 m; Last harvest field 4.6 m; Crane house 9.4 m; Concord warden's spawn 13 m; nearest grove 37 m. | Wattle panels (plaster tint) in a timber frame, 4 x 3.5, steep thatch with the front eave carried on two posts; an open doorway; a rail under the eave with eight hanging herb bundles (the `Herb` stems and flower colours, upside down); two A-frame drying racks with trays outside on the left; a bench on the right with a mortar, three jars (the stall's `Pot` mesh) and a small still (a copper-tinted pot on a stone ring, `Glow` 3 m). `Solid` on the hut and the bench. | bench: stand (1.4,0,-2.3) look (1.4,.9,-1.6); racks: stand (-1.6,0,-2.2) look (-1.6,1,-1.4) |
| **Kitchen**, 5x3 lean-to on the inn's back wall (inn-local x 0.5..5.5, z 4.15..7.15, on the hearth side) | touches the inn by design (a data test checks it abuts within 0.3 m and does not enter the taproom); hedge (-21,-9) 2.7 m; Tanner house 3.7 m edge to edge, which leaves a lane about 3.5 m wide past the Tanners' door; Mira's spawn (inside the inn) 4.8 m; South road 12 m. The two gables were rejected: the north-west one runs into the hedge, the south-east one is 1.8 m from the South road. | Open-fronted: two posts, a mono-pitch slate roof from 3.0 m at the wall to 2.3 m; a stone range 1.8 x 0.8 x 0.9 against the wall with glass embers, an iron pot on a crane, a short stack and `Smoke`, `Glow`; a work table with a bread board and an egg basket; two hares hung from the rafter (the `Load.Game` shapes); a water butt; a firewood stack. `Solid` on the range, the table and the posts. | range: stand (.9,0,-.2) look (.9,1,.9); table: stand (-1.2,0,-.3) look (-1.2,.8,.6); **hand-over point** (0,0,-2.2), outside the open front |
| **Game rack**, 3x2, in the Lodge yard | lodge wall 2.1 m; Ridge path 2.3 m; lamp 1.2 m; barrels 5.8 m; Mastwood grove rectangle 7.8 m from the prop's centre (the tree test needs more than 5 m, so no Mastwood tree is skipped); Harrow wood 17 m; Harrow wood wolves camp 22 m. | Two forked poles and a crossbar with two hares and a brace of birds; a hide laced in a frame leaning on the lodge wall; a chopping stump with a knife; a ring of stones with cold ash. No collider except the stump. | stand (0,0,-1.1) look (0,1.2,0) |

Landmarks added (GAME-ONLY): "Tanner's leather shop" (-20,-33), "Lisbet's drying hut" (23,37). The kitchen and the rack sit inside existing landmarks.

### B.3 The inn: the bar, the rooms and the innkeeper

`Inn()` additions (no random draws; both inns get them, Khaven's simply has nobody to use them):
- `Workplace(t, "bar", stand (-w/2 + 5.3, 0, d/2 - 1.4), look (0, 1, -1))`: the open end of the bar, facing the room. It is 2.9 m from the nearest stool.
- A plank door on the inner back wall at local (0.6, 1.2, d/2 - .2) with two steps, registered as `ZoneDoor { name = t.name, kind = "rooms", openable = false, position = local (0.6, 1, d/2 - .7) }`. Prompt: "Knock · The Golden Cask" is avoided by naming it "The Golden Cask, upstairs".

**Hob Linden, Innkeeper** (role `innkeeper`, GAME-ONLY; title "Innkeeper"; needs an `inn`; up at 5.4, abed at 23.0, which satisfies `VillageDayTests` at 23:30 because he has a home to hide in):

| Hours | Where (a repeat weights the choice) |
|---|---|
| 5.5 - 7 | kitchen |
| 7 - 11.5 | kitchen, kitchen, bar (the morning's deliveries come to the kitchen) |
| 11.5 - 14 | bar, bar, kitchen |
| 14 - 17 | kitchen, bar, well |
| 17 - 23 | bar, bar, bar, kitchen |

Errands (all inside 4.5 - 21, as the test requires):
- "water for the pot", 6.0 - 7.5, well to kitchen, bucket, good `water`: "Bitter or not, it boils."
- "the pot on", 10.5 - 12.5, at the kitchen (no destination, 14 s): "Pot's on. Hare if Garet's been by; barley if he hasn't."
- "dinner to the tables", 12.2 - 14, kitchen to inn, the bread board, good `dinner` (stock key `inn.dinner`): "Dinner. Mind the bowl; it's hotter than it looks."

Where the village's goods are handed over: the errands keep `to = "inn"` and their stock keys (`inn.eggs`, `inn.bread`, `inn.meat`, `inn.ale`, `inn.wood`, `inn.herbs`), so every existing test and line holds. A new optional errand field `door` names a preferred spot: the hen-wife's "eggs to the inn", the baker's "first loaves to the inn" and the hunter's "hares to the inn" get `door = "kitchen"` and are carried to the kitchen's hand-over point when the zone has one (else to an inn seat as now). The merchant's cask goes to the bar (`door = "bar"`); firewood and Mira's marigold go inside as now. The innkeeper is in the kitchen through the morning, so he is the one who answers ("Lovely. Put them by the hearth.").

Lines (GAME-ONLY): trade lines "Ale's thin and the stew's thinner. Sit where you like.", "Mira's in the corner. Don't crowd her; she's the only mender we've got.", "The Cask stood before the Concord and it'll stand after."; stock lines: `inn.meat > 0` "Garet's hares are in the pot. Don't tell the out-of-work."; eggs and bread in: "Eggs from Goody, bread from Hedda. I can do you a plate."; ale at 0: "Dry. Ama's cask never sees the night out." Vendor: role `innkeeper` sells `food.brown_loaf` and `food.harrow_cheese` (income from the player for his purse).

### B.4 Code changes (workshops)

| File | Change |
|---|---|
| `Scripts/World/ZoneBuilder.Workshops.cs` (new partial) | `LeatherShop(t)`, `DryingHut(t)`, `Kitchen(t)`, `GameRack(t)`, each ending in its `Workplace(...)` calls; the kitchen also registers `Workplace(t, "kitchendoor", ...)` for the hand-over point. |
| `Scripts/World/ZoneBuilder.cs` | Four `case` lines in `BuildProps`; the four kinds in the `legacy == 0` list and in `Footprint`; the `Inn()` additions. |
| `Scripts/Encounter/WorldLife.cs` | `FindPlaces` key list gains `leathershop`, `dryhut`, `kitchen`, `kitchendoor`, `bar`, `lodge` (without them the workplace loop throws). After the loop: an empty `leathershop` shares the `tannery` list, so a zone with only a tannery works as today. `Needs`: leatherworker `leathershop`, innkeeper `inn`. `TitleFor`, bed and rising hours and trade lines for the innkeeper. `PlaceFor(Villager, kind)`: the stand points of workplaces this villager owns (`life.workshops`, matched on `ZoneWorkplace.name`), else any of the kind; for `field`, points within 60 m of the farmer's home when there are any. `GoTo` and `Resolve` call it. `Resolve(place, door, toRole)`. `PoseFor`: leathershop and dryhut Knead, kitchen Knead, bar Talk, lodge Work. |
| `Scripts/Encounter/VillageWork.cs` | `Errand.door`, `Errand.toRole`. The innkeeper's day. Leatherworker: shifts use `leathershop`; new errand "tanned hides from the yard" (8.5 - 10.5, tannery to leathershop, hide, good `leather`): "Three sides, dry enough. Fen says mind the grey one."; "leather to the stall" starts from `leathershop`. Herbalist: "herbs to dry" ends at `dryhut` (was home); the 10.5 - 12.5 and 16.5 shifts gain `dryhut`. Hunter: dawn and evening shifts gain `lodge`. Deliveries to `stall` of eggs, flour, herbs and goods get `toRole = "merchant"` (the baker's loaves go to her own stall by ownership). |
| `Zones/oakhaven.json` | The four props, `life.workshops` (Vell, Thorne x2, Rusk, Reed, Walker, Tanner, Crane, Linden x2, Moss, Farrow, Crisp), the innkeeper resident, two landmarks. |
| `Items/items.json` | Vendor `{ "role": "innkeeper", "items": ["food.brown_loaf", "food.harrow_cheese"] }`. |
| DESIGN, section 6.1 | Oakhaven's bench station is the drying hut (drop the `stations` entry at (6,19)); the kitchen adds a second `fire` station; the leather shop registers a `leather` station with no recipes yet. `Workplace()` maps `dryhut` to bench, `kitchen` to fire, `leathershop` to leather, beside DESIGN's forge and oven. |

## C. Purses and spending

### C.1 Decisions

- **One purse per household**, earned by every working member, spent on the household. For a single tradesperson it is the same thing as a personal purse; for the Tanners it is what the owner asked for ("for his family").
- **Not saved.** Purses live in a static in-memory table keyed by zone and household, like DESIGN's node respawn table: they survive a zone hop and reset when the game restarts. Reasons: the village's stock is already "today's state" and unsaved; no save-format risk for the owner's save; each class has its own save slot and would otherwise see a different village; and a restart can never load a stuck economy. The lasting effect is carried by something already saved: **each trade bag the character wears adds 1 coin a day to the Tanners' custom** ("Folk have seen your poke and asked who made it."), read from `progress.pouches`.
- Coin is the player's gold, one for one.

### C.2 The day of a purse

`VillageEconomy` (new file, pure C#, no scene; `VillageLife` owns one):

1. **Dawn (the 04:00 stock reset, once per day):** `coin = min(coin, 40)` (GAME-ONLY: "the tithe-man takes what's over", in keeping with the Concord's tithes), then `coin += stipend`, then yesterday's flags (bread, wood, eggs) are remembered and today's cleared. On first use in a session a purse starts as if dawn had just run.
2. **Stipend** is "the day's custom from folk we don't see" and is paid at dawn regardless of anything, so no chain of events can starve a household. Coin a day by role: merchant 6, innkeeper 6, blacksmith 6, baker 5, miller 5, elder 5, farmer 4, woodcutter 4, hunter 4, herbalist 4, hen-wife 2, skinner 2, gossip 2, leatherworker 1, drinker 1, child 0. A household's stipend is the sum.
3. **Needs, in order:** bread 2, firewood 3, eggs 1 (6 a day for full comfort). A household never buys what its own trade makes (baker bread; woodcutter and hunter firewood; hen-wife eggs). The inn does not shop: its bread, eggs and wood are the trades' deliveries.
4. **Income:**
   - the player's purchases: the whole price paid goes to the household of the villager whose wares were open (`EncounterSession.Buy` calls `VillageLife.Active.Paid(VendorNpc, price)`); bag-quest hand-ins pay the hides' value the same way (section D);
   - sales between villagers: a delivery errand has a price, paid by the household that owns the destination workshop to the carrier's household when it can: grain to the mill 2; flour to the bakehouse 2, to the stall 1; first loaves to the inn 2; ironwork to the stall 2; firewood to the inn 2, to the forge 1; the hide to the tannery 2; hares to the inn 2; leather to the stall 2; herbs to the stall 1; the cask to the inn 3; eggs to the inn 1, to the stall 1. Same-household moves are free;
   - the inn gets 1 coin a tankard, "on the slate" (new money; the drinker pays nothing).
5. **Spending as errands you can see** (`VillageWork.Shopping`, run by household members between their own errands):
   - "bread for the house", 9 - 18, the baker's own stall to home, the bread board, 2 coin to the baker's household: "A loaf, Hedda. The small one." A child who has a household does it as the existing "a loaf for Mum" (11 - 17), which now costs 2 and is skipped when the purse is short;
   - "firewood for the hearth", 14 - 19.5, the Woodyard to home, logs on the shoulder, 3 coin to the woodcutter's household, carried by an adult: "A bundle, Osk. Dry, if you've any.";
   - "eggs for the house", 15.5 - 18.5, a merchant's stall to home, the egg basket, 1 coin. It does **not** take from `stall.eggs`, so the player's eggs and the existing test are untouched.
   - At home the load goes indoors (the existing "goods put away") and the household's flag is set. With firewood in, **that house's chimney smokes from 17:00 to dawn; without, it stays cold.** With bread in, the family takes its midday at home half the time.
6. **Short of coin:**
   - the errand is not run, and the member who would have gone says so once: "Can't stretch to firewood today."; a child: "Mum says there's no loaf today.";
   - lines to the player (a `PurseLine`, tried one talk in three, before the trade lines): no bread yesterday and none today: "No bread in the house since yesterday. Trade's that thin."; no firewood: "Cold hearth again tonight. Wood's three coppers we haven't got."; Maud's own: "We've bread. No fire; Nettie sleeps in her coat.";
   - a thinner day: the cold chimney; and the household's adults skip the evening at the inn and go home at 18:00;
   - nothing the player needs ever waits on a purse (DESIGN's rule): shops still open, stations still work.
7. **When the player pays:** the villager thanks them once if a need was unmet ("Maud Tanner: That's the fire lit tonight. Bless you."), and the next free household member sets out within the errand's hours. For a day after: "You paid for that loaf, you know. And the fire."

With these numbers: the Tanners (1 + 2 + 0 = 3 a day) buy bread every day, firewood about one day in three and eggs never, until the player buys or brings hides; a 48-gold bag keeps them warm and fed for about two weeks of game days. Jory (1 a day) eats every other day. Vell, Rusk, Ashby, the farms and the inn want for nothing.

### C.3 Guard rails

| Risk | Rail |
|---|---|
| Deadlock (nobody can pay, so nobody earns) | Stipends arrive at dawn from outside the village; a delivery is never blocked by an empty purse: the goods are handed over and stock counted exactly as today, and the buyer says "On the slate till Friday." |
| Runaway (coin piles up) | The dawn cap of 40. The player can pour gold in; the next dawn trims it. |
| Negative coin | `Pay` and `Spend` refuse rather than go below 0. |
| The player draining a purse | Selling to a vendor never draws on a purse (vendors buy at value from an unmodelled float, as now). |
| Shopping blocked by missing goods | Buying needs coin only, never stock. |
| Tests that watch errands and ale | Errand ids, order, windows and stock keys of the existing trades are unchanged; drinkers never pay; payments happen after `Deliver`. |
| A stuck session | Restart resets every purse to its dawn state. |

### C.4 Code changes (purses)

| File | Change |
|---|---|
| `Scripts/Encounter/VillageEconomy.cs` (new) | `Purse { int coin, stipend; bool bread, wood, eggs, hadBread, hadWood; int fromPlayerToday }`; `VillageEconomy { NewDay(); bool Pay(from, to, coin); void Earn(name, coin); bool Wants(name, need); bool CanAfford(name, cost); static int Stipend(role); const Cap = 40 }` over household names; a static table by zone id. |
| `Scripts/Encounter/VillageWork.cs` | `Errand.pays`, `Errand.need`, `Errand.cost`, `Errand.sellerRole`; `VillageWork.Shopping`; prices on the existing delivery errands; the child's loaf gets `need`, `cost`, `sellerRole`. |
| `Scripts/Encounter/WorldLife.cs` | `Economy`; dawn settle beside `Stock.Clear()` (guarded so it runs once a day); `Paid(npc, coin)`; `Villager.StartErrand` offers the household's shopping errands after the role's own; `ErrandArrive` pays at pick-up (shopping) or hand-over (deliveries) and sets the flags; `DrinkRound` credits the inn; `PurseLine` in `LineFor`; the 18:00 "home" override for a short household; `Household.ApplyHearth()` switches the home door's `smoke` on or off. |
| `Scripts/Encounter/EncounterSession.cs` | `Buy`: after a successful purchase, `VillageLife.Active?.Paid(VendorNpc, Inventory.Price(d))`. |

## D. The leatherworker's bags

### D.1 What a trade bag is

A trade bag is a bought or earned item that you **wear** (use it once; it leaves your bags and is recorded on the character, like DESIGN's tools). It adds slots that take only one class of material. Your 24 ordinary slots are untouched.

| Item id | Name (GAME-ONLY) | Holds (class) | Slots | Value / price | Quest hides |
|---|---|---|---|---|---|
| `bag.simples_wallet` | Simples-wallet | `herb`: the five herbs, vials | 6 | 6 / 24 gold | 3 wolf pelts |
| `bag.ore_poke` | Ore-poke | `ore`: ores, bars, charcoal | 8 | 12 / 48 gold | 5 wolf pelts |
| `bag.log_sling` | Log-sling | `timber`: the five logs | 6 | 8 / 32 gold | 4 wolf pelts |
| `bag.larder_scrip` | Larder-scrip | `larder`: raw meats, flour, salt, eggs, cheese | 8 | 10 / 40 gold | 4 wolf pelts |

Names avoid the usual game words on purpose (a poke and a scrip are old words for a small bag; simples are healing herbs). Prices sit beside a common weapon for the zone (24 gold at level 2) and DESIGN's gathering income (about 50 gold for an Oakhaven circuit).

Rules:
- New `ItemDef` fields: `pouch` (the class, on materials), and on bags `holds` (class) and `slots`. New kind `bag`.
- Picking up a material: it tops up an existing stack anywhere first, then an empty slot of a worn bag that holds its class, then an ordinary slot.
- Nothing else can enter a trade-bag slot (drag is refused: "Only ore, bars and charcoal go in the ore-poke."). Taking gear off never lands in one.
- One of each kind. Using a second: "You already carry one." (the item stays and can be sold).
- Wearing is permanent; there is no taking it off (so no "where do the contents go").
- "N free" in the bags window counts ordinary slots only.

### D.2 How they are stored

Trade-bag slots are appended to `progress.bag` after index 23, in the order the bags were worn, and `progress.pouches` (new, a list of bag ids) records that order: with `["bag.ore_poke", "bag.simples_wallet"]`, slots 24-31 are the ore-poke and 32-37 the wallet. Every index-based operation (sell, move, destroy, use, count, remove) then works unchanged. Changes are confined to `Inventory`:

| Method | Change |
|---|---|
| `EnsurePouches(p, db)` (new) | pads `p.bag` to 24 + the worn bags' slots (never truncates). |
| `Accepts(p, db, index, ItemDef)` (new) | true under 24; above, true only if the owning bag holds the item's `pouch` class. |
| `Add`, `Room` | stacks first, then accepted trade-bag slots, then ordinary slots. |
| `Move` | refused when either item would land in a slot that does not accept it. |
| `Unequip` | looks for an empty slot among the first 24 only. |
| `FreeSlots` | first 24 only. |
| `Wear(p, db, bagIndex, out why)` (new) | adds the id to `pouches`, pads, clears the item. |

### D.3 Leathers, and where they come from

The four hides stop being junk so that "Sell junk" cannot eat them: `junk.wolf_pelt`, `junk.ash_hide`, `junk.moss_hide`, `junk.dappled_hide` become `"kind": "material", "quality": 1, "trade": "tannery.hides"` (ids, names, values and stacks unchanged; `ItemTests` still passes its pelt stacking and wolf loot asserts). This overrides DESIGN's "pelts and hides stay junk". Selling them in a village now counts as a delivery to the tannery, and the skinner remarks on it.

| Hide | Drops from (chance) | Zones | Value |
|---|---|---|---|
| Grey wolf pelt | wolves (0.7) | Oakhaven (Harrow wood, North pines, Whitefoot's pack, Hollin farm), Khaven, the Peaks | 2 |
| Ash-matted hide | ash-hounds (0.7) | Ashland Rim | 5 |
| Moss-matted hide | mossback boars (0.7) | Verdant Shore | 7 |
| Dappled stag hide | stags (0.5) | Verdant Shore | 8 |

All four bags are made from wolf pelts so a level 1-2 character can earn them where gathering starts: 16 pelts in all, about 23 wolves. The other hides are sold or kept for a later leather craft and bigger bags (not in this addendum).

### D.4 Getting one

**Buy it:** vendor entry `{ "role": "leatherworker", "items": ["bag.simples_wallet", "bag.log_sling", "bag.larder_scrip", "bag.ore_poke"] }`. Talk to Maud wherever she is (her counter in shop hours). A bag you already wear is left off her list. The gold goes to the Tanners' purse (section C).

**Earn it:** four one-time quests, giver and turn-in Maud Tanner, kind `npc`, zone Oakhaven, `canonStatus` GAME-ONLY. A quest is not offered once you wear its bag.

| Quest id | Title | Level | Bring | Reward |
|---|---|---|---|---|
| `npc.maud.wallet` | A Wallet for Simples | 1 | 3 Grey wolf pelts | Simples-wallet, 20 XP, Oakhaven +75 |
| `npc.maud.sling` | A Strap for the Woodyard | 1 | 4 Grey wolf pelts | Log-sling, 25 XP, Oakhaven +75 |
| `npc.maud.scrip` | The Cook's Scrip | 2 | 4 Grey wolf pelts | Larder-scrip, 25 XP, Oakhaven +75 |
| `npc.maud.poke` | Ore Wants a Stout Bag | 2 | 5 Grey wolf pelts | Ore-poke, 30 XP, Oakhaven +75 |

Text for the first (the others follow its pattern):
- Offer: "\"You've the look of someone who picks things up. Herbs, is it? They'll bruise to nothing loose in a pack.\"\n\n\"Bring me three wolf pelts, whole, not grey at the edges, and I'll cut you a wallet for them. Soft side in. No charge but the hides; what's left over is my profit, and the gods know I need one.\""
- Summary: "Bring Maud Tanner three grey wolf pelts. Wolves run in the Harrow wood and the North pines."
- Progress: "\"Three, I said. The wolves won't skin themselves.\""
- Complete: "Maud turns each pelt to the light and runs a thumb along the edge. \"These'll do. Sit. It's an hour's work if Nettie holds the lamp.\" The wallet she hands you smells of oak bark and smoke. \"The offcuts will buy the bread this week. So that's both of us better off.\""

On hand-in the Tanners' purse gains the pelts' value (2 each) and the same thank-you and errands follow as for a purchase.

Quest-system additions (`Quests.cs`):
- objective type **`bring`**: `item` is a bag item id, `count`, `target` an NPC. Progress is `min(count, Inventory.Count)`; talking to the target with enough hands them over (`Inventory.Remove`) and completes it; the NPC shows a gold `?` when you carry enough. `QuestDatabase.Check` accepts the type without testing the item against quest items;
- **`rewards.bagItems`**: bag item ids put in the bags at turn-in. `EncounterSession.CompleteQuest` first checks `Inventory.Room` for each and refuses with "Make room in your bags first." (nothing is removed or paid);
- **`unlessWorn`** on a quest: not offered while `progress.pouches` contains that id;
- the session cross-checks `bring` items, `bagItems` and `unlessWorn` against the item database when both are loaded and logs any unknown id; a data test does the same.

### D.5 Save format

- New saved list `pouches`, and `bag` may be longer than 24 (the reader already allows up to 96; the four bags make 52).
- **Recommended: carry it in DESIGN's format 8.** `AddProfessionsMigration` inserts `"pouches":[]` together with `"professions":[]`, so the owner's save is backed up once and migrated once for the whole feature. If format 8 has already shipped without it when the bags are built, it is format 9 with its own text-insert step (`AddPouchesMigration`, a copy of `AddDiscoveriesMigration`).
- `Read` guards: null becomes empty; blanks and later duplicates dropped; more than 8 entries or a bag over 96 slots refuses the save ("Invalid item data.", file untouched). An unknown bag id is kept in the file and ignored, and any slots past the known ranges stay as slots that accept nothing new but can be emptied, so content changes never lose an item.
- Not saved: purses, household flags, chimneys.
- Back up the owner's save folder to `...\hel\work\save-backups\<date>-pre-format-8` before the first run, as DESIGN says; every new test sets `SaveDirectoryOverride`.

### D.6 UI

- Bags window: under the 24 slots, one labelled row per worn bag ("Ore-poke 3/8") with its slots; the window grows by a row per bag. With none worn, one small line: "Trade bags: Maud Tanner makes them, by the South road in Oakhaven."
- Tooltip type lines: "Trade bag: 8 slots for ore, bars and charcoal. Use it to wear it." and, on materials, "Goes in an ore-poke." Glyph "Bg".
- Right-click a bag item with no vendor open: wear it ("You sling the ore-poke at your hip.").
- DESIGN's Trades window gains one line under each gathering skill: "Ore-poke: worn" or "No ore-poke (Maud Tanner, Oakhaven)".

### D.7 Code changes (bags)

| File | Change |
|---|---|
| `Scripts/Encounter/Items.cs` | `ItemDef.pouch`, `holds`, `slots`; the `Inventory` changes of D.2. |
| `Scripts/Encounter/EncounterProgress.cs` | `public List<string> pouches = new List<string>();` |
| `Scripts/Encounter/EncounterSave.cs` | the migration text and guards of D.5. |
| `Scripts/Encounter/Quests.cs` | `bring`, `rewards.bagItems`, `unlessWorn` (D.4). |
| `Scripts/Encounter/EncounterSession.cs` | `EquipFromBag`: kind `bag` calls `Inventory.Wear`; `OpenVendor` leaves worn bags off the list; `CompleteQuest` room check, bag rewards and the purse credit; `EnsurePouches` after load; cross-check of ids. |
| `Scripts/Encounter/EncounterHud.Items.cs` | the trade-bag rows, tooltips, glyph. |
| `Items/items.json`, `Quests/oakhaven.json` | four bags, `pouch` on DESIGN's materials, the four hides as materials, the leatherworker vendor, four quests. |
| Docs | `SAVE_FORMAT.md`, `QUEST_DESIGN.md` (`bring`), `WORLD_ZONES.md` (households and workshops per zone), `DATA_SCHEMA.md`, `CHANGELOG.md`, `CLAUDE_HANDOFF.md`. |

## E. The integrated build order

Before step 1: commit or park the uncommitted work in the tree (it touches `oakhaven.json`, `ZoneBuilder.cs`, `VillageWork.cs`, `WorldLife.cs` and adds `VillageDrinkTests.cs`), back up the owner's save, sync to the validation copy. After every step: full EditMode and PlayMode run in the validation copy, a playable build, commit, `tools\Backup.ps1`. "V" steps are this addendum; "P" steps are DESIGN's.

| # | Step | Contents | Acceptance check | Tests |
|---|---|---|---|---|
| 1 | **V1 Households and homes** | Section A: data shape, Oakhaven and Khaven households, house names, the Carder farmhouse, home doors for the mill and the lodge, `Init`, knock lines, the empty `Paid` hook. | At 23:30 every Oakhaven villager is behind their own named door; "Knock · Tanner house" answers with the Tanners; Wil Carder and Hettie Brook walk home to the new farmhouse; the map shows it; no tree has moved. | EditMode `HouseholdDataTests`: `Oakhaven_every_villager_is_in_exactly_one_household`, `Every_household_house_names_a_prop_and_no_house_is_shared`, `Khaven_households_cover_its_six_villagers`, `Zones_without_households_still_parse`. PlayMode `VillageHomeTests`: `Every_villager_sleeps_behind_their_own_named_door`, `The_Tanners_share_one_house`, `Farmers_live_with_the_hen_wife_at_their_farm`, `Home_doors_are_on_the_navmesh`, `Knocking_names_the_household`. PlayMode `VillageStreamTests.NewVillageProps_LeaveTreesAndSceneryUnmoved` (Oakhaven built with and without the appended props: `ZoneBuilder.Trunks` and the static scenery positions are identical). Green: `VillageDayTests`, `VillageErrandTests`, `VillageDrinkTests`, `VillageWorkTests`, `OakhavenQuestTests`, `ZoneContentTests`. |
| 2 | **V2 A workshop per trade** | Section B: the four props, the bar and the rooms door, the innkeeper and his day, owned workplaces, the kitchen hand-over, the leatherworker's, herbalist's and hunter's days. | Maud works at her shop by the South road while Fen scrapes hides in the yard; Ama, Tamsin and Hedda each keep their own stall; the hen-wife hands her eggs over at the kitchen and Hob answers; Hob serves at the bar by evening and is abed by 23:30; Garet starts and ends his day at the lodge. | EditMode `VillageWorkTests`: add the six new place names to its `Places` set; new `The_innkeeper_has_a_day_from_first_light_to_the_last_table`, `Every_trade_names_its_own_workshop`. EditMode `WorkshopDataTests`: `Kitchen_abuts_the_inn`, `Every_workshop_owner_is_a_villager`. PlayMode `VillageWorkshopTests`: `Each_trade_stands_at_its_own_workshop`, `The_leatherworker_keeps_shop_apart_from_the_tannery_yard`, `Eggs_loaves_and_hares_are_handed_over_at_the_kitchen`, `The_innkeeper_is_abed_before_the_last_drinkers`, `Every_workshop_stand_is_reachable`; `VillageStreamTests` extended to the four props. Green: all of step 1's. |
| 3 | **P1 Gather and sell** | DESIGN step 1, with two changes: the format 8 migration also inserts `"pouches":[]` (field and guards only), and materials carry their `pouch` class. | DESIGN's check. The owner's save loads as format 8 with empty `professions` and `pouches` and nothing else changed. | DESIGN section 12 for step 1; `SaveMigratorTests.V7Payload_MigratesTo8_AddsProfessionsAndPouches_KeepsEveryOtherCharacter`. |
| 4 | **V3 Purses and spending** | Section C. | With the Tanners' purse emptied at 14:00 nobody fetches firewood, Maud says so and their chimney is cold at dusk; buy anything from her and within a game hour Nettie carries a loaf home and Fen a bundle of firewood, and the chimney smokes. Thirty simulated days leave every purse between 0 and 40. | EditMode `VillageEconomyTests`: `Stipends_and_the_tithe_keep_every_purse_between_zero_and_the_cap_for_thirty_days`, `A_short_purse_buys_bread_before_firewood`, `A_delivery_is_never_blocked_by_an_empty_purse`, `The_Tanners_go_cold_until_the_player_buys`, `A_purchase_reaches_the_sellers_household`, `A_household_never_buys_its_own_trade`. PlayMode `VillagePurseTests`: `Buying_from_the_leatherworker_sends_her_family_for_bread_and_firewood`, `Short_of_coin_they_go_without_and_say_so`, `Firewood_lights_the_chimney`. Green: `VillageErrandTests`, `VillageDrinkTests`, `VillageDayTests`. |
| 5 | **V4 The leatherworker's bags** | Section D (needs step 3's materials; uses step 4's `Paid`, which is a harmless empty call if step 4 is not in yet). | Kill wolves, bring Maud three pelts, wear the wallet, pick yarrow and see it go into the wallet's row; buy the ore-poke for 48 gold; "Sell junk" keeps the pelts; drag a sword onto a poke slot and it is refused; save, quit, reload: the rows and their contents are there. | EditMode `PouchTests`: `Wearing_a_trade_bag_adds_its_slots_and_uses_up_the_item`, `A_second_bag_of_a_kind_is_refused_and_kept`, `Materials_fill_their_trade_bag_before_the_ordinary_slots`, `Nothing_else_enters_a_trade_bag`, `Count_Remove_and_Room_see_trade_bag_slots`, `FreeSlots_counts_ordinary_slots_only`. `ItemTests`: `Hides_are_materials_and_SellJunk_keeps_them`. `QuestLogTests`: `Bring_hands_over_bag_items_on_talk`, `Bring_without_enough_changes_nothing`, `A_bag_quest_is_not_offered_once_the_bag_is_worn`, `TurnIn_with_full_bags_is_refused_and_nothing_is_lost`. `SaveMigratorTests`: `Pouches_round_trip_with_their_slots`, `An_unknown_pouch_is_kept_and_ignored`, `Too_many_pouches_refuses_the_save_file_unchanged`. `QuestDataTests.Bring_and_bag_rewards_name_real_items`. PlayMode `TradeBagTests`: `Maud_sells_the_four_bags_and_a_worn_one_leaves_her_list`, `TradeBagUiTests.Capture`. |
| 6 | **P2 Refine and cook** | DESIGN step 2. Stations: the kitchen range counts as fire. | DESIGN's check, plus: cook boar stew at the Cask's kitchen range. | DESIGN's, plus `CraftStationTests.CaskKitchen_CountsAsFire`. |
| 7 | **P3 Two crafts** | DESIGN step 3. Oakhaven's bench is Lisbet's drying hut. | DESIGN's check, with "take up Alchemy at Lisbet's drying hut". | DESIGN's, plus `CraftStationTests.DryingHut_IsTheBench`. |
| 8 | **P4 Depth** | DESIGN step 4. | DESIGN's check. | DESIGN's. |

**Who can work in parallel**

| Engineer | Steps, in order | Files that are theirs |
|---|---|---|
| A (village) | 1, 2, 4 | `WorldLife.cs`, `VillageWork.cs`, `VillageEconomy.cs`, `ZoneBuilder.Workshops.cs`, `ZoneDoor.cs`, the `life` block and the tail of `props` in the zone JSON, the `Village*` tests |
| B (professions) | 3, 5, 6, 7, 8 | `Professions.cs`, `Items.cs`, `Quests.cs`, `EncounterProgress.cs`, `EncounterSave.cs`, `EncounterHud*.cs`, `ZoneBuilder.Nodes.cs`, `items.json`, `professions.json`, the quest JSON, the `nodes` and `stations` arrays in the zone JSON |

Steps 1 and 3 can be built at the same time, then 2 beside 5 (once 3 is in), then 4 beside 6. The touch points to merge by hand, each a few lines: `ZoneBuilder.cs` (A: four `case` lines, `Inn()`, the home doors; B: the `BuildNodes` call and the station hooks in `Workplace()` and `Inn()`); `ZoneDefinition.cs` (A: `ZoneLife`; B: nodes and stations); `EncounterSession.cs` (A: the knock line and the one-line `Paid` call in `Buy`; B: everything else); `WorldLife.StockLine` (B adds DESIGN's three cases in step 3; A adds `PurseLine` as a separate method); `oakhaven.json` (one engineer at a time: step 1's JSON lands before step 3's). Step 5 is B's because it lives in `Items.cs`, the save and the HUD.

## F. Open questions for the owner (recommended default in bold)

1. **The leatherworker in the game today is a woman, Maud Tanner; you wrote "him".** **Default: keep Maud. Her husband is the skinner, Fen Walker, and their child is Nettie, so it is "her family"**: he tans in the yard, she cuts and sews in the new shop. Alternative: make the leatherworker a man (a renamed villager) with Maud as his wife minding the counter.
2. **Kin I invented so every villager has a household: Old Tobin and Edda Pell are married; Pim is Osk Farrow's son; Hettie Brook is Wil Carder's aunt; Goody Marl is Sel Harrow's aunt; Ilse Brandt is the Harrows' farmhand; Jory lives alone.** **Default: accept as GAME-ONLY** (it is one block of JSON, easy to change). This is also why only one new house is needed, not a couple. Alternative: tell me who should live with whom; a household moved out of a shared house needs one more new house each.
3. **The hunter lives out at Moss's lodge, 170 m from the green, about a game hour's walk each way.** **Default: yes: it is already "Moss's lodge" on the map, and he hunts the Harrow wood beside it.** Alternative: he keeps a house in the village and the lodge is only his workplace (needs one more new house).

## Known risks

- All five new positions were checked against the zone data by script, not in the engine: slopes, the NavMesh lane between the kitchen and the Tanner house (about 3.5 m), and the drying hut's 2.8 m to the nearest tree canopy need eyes in the validation copy.
- The Carder farmhouse may cover one random meadow stand point, which shifts where some villagers idle and where critters start (the village's life stream, not the layout). No test pins those positions.
- Farm and lodge households now walk 75 to 170 m home; a villager who flees a fight runs that far.
- The purse numbers (stipends, prices, cap) are estimates; `VillageEconomyTests` pins the shape (nobody starves, nothing runs away, the Tanners are cold until helped), not the feel.
- Sixteen wolf pelts for all four bags is a guess at the right effort beside 144 gold to buy them.
- The quest hides cannot be told from "any leather": only grey wolf pelts count. Hides from later zones have no use yet beyond selling.
