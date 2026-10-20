# The Forge District (Phase 9, the zone after the Lowtowns; mobs 19-22, the cap stays 20; no dungeon; PROVISIONAL)

Written 2026-10-19. The first playable pass is built from this (section 9); everything in it is open to Chris's answers below.
It keeps DUNGEON_DESIGN.md's shared rules (the game is set shortly before Book 1; **no machines**; no canon person dies; **Seekers,
Inquisitors and the Silencers belong to the Transit Hub**; **the Copper-Tithe are never enemies**; **the Cult of Ash and the Iron
Teeth as foes belong to the Sludge-Tunnels**; short Anglo-Saxon or Germanic names; no WoW names) and labels every line CANON,
CANON-EXPANDED, GAME-ONLY or PROVISIONAL.

## Chris's answers (2026-10-19, the question prompt, verbatim where he wrote them)
- Band: "19-22 (Recommended)". The Spoke: "Seen, not entered (Recommended)". The inn: "Fully its own now (Recommended)". Rook: "Not
  yet (Recommended)". The other six (5-10 below) take the recommended answers: the Unraveller stays where he is; the Lowtowns' top
  nodes are reused; no imps; the elites as level-20 elites (next pass: their moves and loot); new music tracks for the Forge (to find);
  the world-map pin east of the Lowtowns (built: .5, .46, its label clear of the others by WorldMapLabelTests' layout).
- Built since: the Rust & Ruin is its own inn (variant 5, ZoneBuilder.RustRuin; section 9).

## The questions as asked (the recommended answer first, and what the first pass built)
1. **Band:** mobs **19-22** (Recommended; ROADMAP's road and the Lowtowns' gate sign say 19-22), or 18-22? Built: 19-22.
2. **The Spoke in the middle** (the author's map draws it there; book 1 ch.7 builds it in the Forge District): a rune-carved stone
   monolith on a walled yard, glowing runes, **never entered** (Recommended), or leave it out until the Spire? Built: in, not entered.
3. **The inn, the Rust & Ruin** (the tavern of book 1 ch.24, its iron-banded oak door): its own soot-stone look on the shared inn
   floor plan for now, a fully own inn (like the Rusty Spoon) next pass (Recommended), or a full own inn now? Built: own look, shared plan.
4. **Rook** (book 1 ch.25, the one-eyed man of the Copper-Tithe defectors) as a quest giver? No, a GAME-ONLY Copper-Tithe tally-woman
   gives the chain (Recommended: canon people only on your yes); or yes, Rook himself.
5. **The world boss** (the Unraveller, "in the highest zone"): leave him where he is (Recommended: the Forge is harder ground but no
   higher in the cap), or move him to the Forge?
6. **Trades:** reuse the Lowtowns' top nodes (slag-iron, cinder-beam, soot-wort) here (Recommended for the cap of 20), or add a new top
   tier (skill 100) for the Forge? Built: reused.
7. **Imps** (the Bestiary kit's): as "cinder-imps" in the fire pits? No for now (Recommended: what they are is your pick, BESTIARY_MONSTERS.md).
8. **Elites:** Captain Hroth Mallin and Overseer Vant need their own moves, and the loot tables stop at 20 (an elite over 20 drops
   nothing named). Make them level-20 elites now (Recommended), or wait until loot reaches 22? Built: none yet; the camps are there.
9. **Music:** every track in the packs already plays somewhere; the Forge has its own mix of them. Add new tracks for it (Recommended), or keep the mix?
10. **World map pin:** the Forge District has no pin yet (the Lowtowns' and the Crown's plates are close). Pin it just east of the Lowtowns (Recommended)?

## 0. Chris's words (verbatim)
- 2026-10-19 ~20:05-20:08 (ROADMAP "Next, after the Unraveller's tuning"): "just want everything up to level 20 for now with classes.
  can do forge district but no dungeon for now", "new zone still up to lvl 22", and, asked whether the cap goes to 22, "Keep cap 20".
- 2026-10-07 (ROADMAP, the 16-30 road): "the Lowtowns and the Rust Market (16-19), the Forge District (19-22), the Warrens with the
  Deep Veins dungeon (22-25), the Crown and the Transit Hub (25-28), the Spire (28-30)."
- 2026-10-11: "more machines!!!!! i told you NO Machines!" (CRULANDA_CANON.md): no engines, drills, gears, contraptions, steam.
- Every zone unique (memory): no other zone's inn or houses; the Lowtowns' MedievalVillage1 pack is the Lowtowns' only.
- Breadcrumbs from each zone's last main quest (2026-10-07).

## 1. What the books give (the books first)
- **Where it is** (CANON): the Rust Market stands "against the crumbling stone walls of the Forge District" (b1 ch.3:7); past the
  Lowtowns "the streets gave way to the Forge District. Here, the air turned bitter cold, cast in the immense shadow of the Spire's
  iron support pillars" (b1 ch.1:147). So it is entered from the Lowtowns' east wall, as the Lowtowns' gate already stands.
- **What it is like** (CANON): "the towering manufactories of the Forge District rose like canyon walls, bristling with guard
  towers" (b1 ch.16:131); "the heavy, choking soot of the Forge District's endless industry", "the sharply metallic tang of hot,
  smelting copper" (b1 ch.9:5); a room "deep within the industrial bowels of The Forge District ... smelled perpetually of sulfur",
  a home "before the High Council's industrialization swallowed it whole" (b1 ch.17:296); "the toxic Forge District" (ch.17:316).
- **Its people** (CANON): the poor live there too. Valen grew up "in the gutters of The Forge District" (b1 ch.25:175); Jareth was
  "foolish enough to steal a loaf of bread in the Forge District" (b1 ch.2:19); there are **the Forge District Warrens** where Rill
  roams (b1 ch.3:159); the Concord handed out pamphlets about "the necessary friction of the great monolith" (ch.25:175).
- **The Spoke** (CANON): "the heavy Foundry District. That is precisely where the Spoke is actively being constructed" (b1 ch.7:237);
  the Spoke is "a massive, rune-carved stone monolith" that pins the Weave and needs living Conduits (world_bible.md Magic). The
  Council's list: "Project Spoke. Primary Resonance Target List - The Forge District" (b1 ch.3:149). Aethelgard: "Identify
  high-resonance targets within the Forge and Warren Districts" (b1 ch.2:197).
- **A fortress** (CANON): "Layered magical wards. War-Golems patrolling every major intersection. A full legion of Seekers. It is
  hands-down the most heavily secure square mile in this entire accursed city" (Valen, b1 ch.7:239). The game takes the wards and the
  guard towers; **the War-Golems are left out (machines)** and **the Seekers are the Transit Hub's** (DUNGEON_DESIGN.md), so the
  Council's guard here is the Iron Pact and its own overseers (shared freely).
- **The drain** (CANON): "a massive, heavily restricted drainage output pipe" venting the "industrialized sludge" (b1 ch.7:299): the
  Lowtowns' Outfall, the Sludge-Tunnels' door (not built: no dungeon now).
- **The Grand Forge** (CANON): "the lowest level of the Spire of Agony, reserved for smelting armaments and breaking spirits ...
  molten slag, sulfur, and copper" (b1 ch.24:7). In the Spire, not here; the Forge District's furnaces are its yard.
- **The Copper-Tithe** (CANON): a syndicate (b1 ch.1:145) whose people "had grown sick of paying extortionate taxes to a Concord that
  fed them nothing but ash and propaganda" and turn to the rebels (b1 ch.24:140, ch.25:13). Never enemies (DUNGEON_DESIGN.md).
- **Captain Horgus** pulled Gorm "out of the slag-pile" and "hammered and welded that iron leg right back onto [his] knee" (b1 ch.16:349): slag-piles and hand smithing are canon.
- **The author's map** (D:\code\crulanda\maps\forge_district_map.png): the Spoke in the middle, black and jagged with glowing veins;
  two rivers of molten metal, **the River Aureum** and **the Molten Vein**, running north to south past it; **the Great Crucible**
  (two: north-east and south-west); **the Fire Pits** (east and west); stone bridges over the molten rivers. The map also draws
  cog-wheels, chains and hanging ladles: **left out (machines)**. Colours: black stone, soot, ember orange and molten gold.
- **No machines** (CRULANDA_CANON.md): the Forge District is hearths, anvils, bellows worked by hand, furnaces, crucibles, smelting,
  slag, salt-stone craft; loads carried and ladled by hand; no engines, gears, drills, cranes, lifts, pumps or steam.

## 2. The premise and the thread
The Lowtowns' chain ends with Wren and the soldier's boy gone **up**, "past the Crown" (main.lowtowns.5). The Forge District is where
the Council's list of resonant children is kept and where the Spoke is being raised (CANON). The Copper-Tithe, who tax the furnaces
for the Concord and hate it, let you through their gate (GAME-ONLY reading of CANON). You follow the list: find it at the overseers'
fire pits, learn from an old crucible-hand what "Moved up" means, get a foundry pass off the Great Crucible's overseer, and walk to
the Spoke's yard, where the Iron Pact's captain keeps the way to the Spire's pillars. The trail ends at the Spoke's foot, looking up:
the Spire is 28-30, later (PROVISIONAL). Nobody canon dies; the Spoke is not touched.

## 3. The place (360 m square; x east, z north; layout PROVISIONAL)
- **The Tithe Gate (west, the arrival):** the gate from the Lowtowns in the district wall; the Copper-Tithe's tally-booth; a notice
  board. Exit west to the Lowtowns (arriving at their Forge District Gate).
- **The Rust & Ruin (west bank):** the zone's inn (CANON-EXPANDED: the tavern of b1 ch.24:81, "iron-banded oak door", "perpetually
  shadowed alleyway"; here on the Forge side of the wall). Its own look: soot-black stone, iron bands, an ember chimney.
- **The Forge Warrens (south-west):** the smelters' lodgings, low stone rows under iron roofs, the "gutters" Valen grew up in (CANON
  name, b1 ch.3:159; their look GAME-ONLY, never the Lowtowns' tenements).
- **The West Fire Pits (south-west) and the south-west Great Crucible:** the first fights.
- **The River Aureum (west) and the Molten Vein (east):** channels of molten metal behind waist-high stone kerbs, crossed by stone
  bridges (CANON names, the author's map). Nobody walks in them.
- **The Foundry (the middle, round the Spoke):** the Foundry halls to the north (long soot-stone halls, their arches glowing), the
  **Spoke's yard** (a walled ring round the monolith, the Iron Pact at its gate).
- **The Spoke:** a rune-carved black stone monolith, forty metres and more, its runes glowing (CANON; its size and look GAME-ONLY from
  the map). Seen from the whole zone. Not entered, not climbed.
- **The east bank:** the north-east **Great Crucible** (a great stone bowl on a stepped plinth, molten at the brim, poured by ladle
  and hand), the **East Fire Pits**, and the **Slag Fields** (south-east): black heaps cracked with ember.
- **The wall:** the district wall closes the zone; past it, chimney stacks and the Spire's pillars in the smog; the Crown far off in
  the north-east sky (the Lowtowns' CrownSkyline).

## 4. Camps and mobs (19-22; the cap is 20, so 21-22 is harder ground for geared level-20s)
| Camp | Mob (look) | Levels | Where | Label |
|---|---|---|---|---|
| Slag-hounds on the Warrens' tips | Slag-hound (wolf), pack | 19-20 x4 | west bank | GAME-ONLY |
| Slag-hounds by the west pits | Slag-hound | 19-20 x4 | south-west | GAME-ONLY |
| Blacklegs at the west pits | Blackleg (deserter), call | 19-20 x4 | West Fire Pits | GAME-ONLY: thugs the foremen hire against the smelters |
| Crucible overseers (south-west) | Foundry overseer (warden) | 20-21 x3 | SW Great Crucible | CANON-EXPANDED: the Council's foremen (shared freely) |
| Foundry overseers | Foundry overseer | 20-21 x4 | the Foundry halls | CANON-EXPANDED |
| The Spoke's leak | Weave-Eater (weaveeater), pack | 21-22 x3 | north of the Spoke | CANON creature (b1 ch.1:251, "born of the High City's magical pollution"); here GAME-ONLY |
| Iron Pact at the yard gate | Iron Pact soldier (warden) | 21-22 x4 | the Spoke's yard | CANON faction (world_bible.md); shared freely |
| Iron Pact crossbowmen | Iron Pact crossbowman (deserter) | 21-22 x3 | the Spoke's yard | CANON faction |
| **Captain Hroth Mallin** (elite; not built yet, question 8) | warden | 22 | the yard gate | GAME-ONLY |
| Blacklegs at the east pits | Blackleg | 20-21 x4 | East Fire Pits | GAME-ONLY |
| Slag-hounds in the Slag Fields | Slag-hound | 20-21 x5 | south-east | GAME-ONLY |
| Crucible overseers (north-east) | Foundry overseer | 21-22 x4 | NE Great Crucible | CANON-EXPANDED |
| **Overseer Vant** (elite; not built yet, question 8) | banditking | 22 | NE Great Crucible | GAME-ONLY |
| **Old Clinker** (rare, 30%) | wolf | 20 (beasts' hides stop at 20) | the Slag Fields | GAME-ONLY |
| **The Spoke-Leech** (rare, 25%) | weaveeater | 22 | the Foundry | CANON-EXPANDED |

Every named elite holds more than its target (the 0.07 rule): Mallin's shield-bash stuns everyone in front of him; Vant's ladle-throw
roots whoever he names in a splash of cooling slag. Built with the zone's existing mob kit; their own moves are a later pass (OPEN).

## 5. Quests (all GAME-ONLY unless marked)
**Breadcrumb (the Lowtowns' last turn-in hands on):** `main.forge.0` "The Forge Gate" (19): Edda Holt, after "Down the Outfall":
the Forge District's gate opened for carters this week; a Copper-Tithe tally-woman there writes down who goes in. Turn in to Hesta Wode.

**The main chain (19-22):**
1. **Under the Tithe** (19, Hesta Wode): the Tithe lets you in if you earn it: clear the slag-hounds off the Warrens' tips.
2. **The Resonance List** (20, Hesta Wode): the overseers at the West Fire Pits keep a list (CANON: "Primary Resonance Target List -
   The Forge District", b1 ch.3:149). Put down the Blacklegs, take the list. Wren's name is on it: "Moved up." Take it to Old Aelric.
3. **What Moved Up Means** (21, Old Aelric): the old crucible-hand says the moved go through the Foundry to the Spoke's yard, and only
   with a foundry pass. Clear the Foundry's overseers, then take Overseer Vant's pass at the north-east Great Crucible.
4. **The Foot of the Spoke** (22, Old Aelric -> Hesta Wode): use the pass at the yard, face Captain Mallin and his Iron Pact, stand at
   the Spoke's foot. A chalk W low on the yard's wall, where a child would sit. The way goes up the Spire's pillars (later). Hesta
   takes the list to the Tithe ("the Tithe keeps a list too, now").

**Side quests:** Ade Heath's bellows (the Blacklegs took his hand-bellows), Elfa Raike's soot-wort for ash-lung, Hamo Glaze's slag-glass
(gather from slag-iron heaps), Berrow Orr's cellar (slag-hounds in the inn's yard); **bounties** on the gate's board (hounds,
Blacklegs, overseers).

## 6. People (GAME-ONLY unless marked)
Hesta Wode (Copper-Tithe tally-woman, the Tithe Gate); Berrow Orr (keeper of the Rust & Ruin); Old Aelric (a crucible-hand forty years
at the pour); Elfa Raike (herb-woman of the Warrens); Ade Heath (a bellows-boy); Hamo Glaze (a slag-glass blower). Names checked
against the game's people and the books on 2026-10-19.

## 7. Trades, nodes, stations
Slag-iron heaps on the Slag Fields and the tips; cinder-beams where lodgings fell; soot-wort in the gutters (the Lowtowns' tier-6 nodes,
question 6). Stations: a field forge at the Warrens (a smith's hearth, bellows worked by hand), a cookfire at the Tithe Gate, the inn's.

## 8. Sky, music, mood
Smog by day (fog close and amber-brown), orange underlight; by night the fog thins a little and the molten channels light the
smoke from below. Soot falls. Music: its own day and night tracks from the packs, none another zone plays (ZoneMusic). Weather:
overcast, mist, ash squalls, rain.

## 8a. Tynara Embercoil's night forge (a note for the merge)
Branch wb-loot puts Tynara Embercoil's night forge (CANON: Tynara, a Forgeborn engineer, world_bible.md; the night forge GAME-ONLY) in
the Lowtowns at (197.5, -14), under the Forge District's wall. **When wb-loot and forge-district are both merged, it moves inside the
Forge District's gate**: into the Forge District near the Tithe Gate (about (-150, -20), clear of the gate's road, the camps and the
nodes; make_forge.py's checks place it), and the Lowtowns keep only a sign by their gate pointing through. Until then it stays where
wb-loot put it. (Her explosives are not used: no machines; her craft here is the forge.)

## 9. Built 2026-10-19 (the first playable pass; not yet run in Unity)
See CHANGELOG. **The Rust & Ruin** (2026-10-19, Chris: "Fully its own now"; inn variant 5, ZoneBuilder.RustRuin, InnPlan.Ruin): a long low house of soot-black stone, 16 x 10, a shallow iron roof, barred slits, the iron-banded oak double door (book1 ch.24:81) under a stone lintel, a broken rusted crucible for its sign, an iron flue through the roof; inside, under low beams, the bar down the +x end with casks behind and the bell by the keeper (ch.24: "Ring the bell"), a round fire pit out in the floor under an iron hood with its bench, the crews' long table, two stone booths, three small tables; 27 seats, 4 stools and the bench, every one with its seat mark; a 0.9 m way from every place to the door (ForgeDistrictTests). Differences from the plan above: no elites yet (question 8); the main chain's quests are levels 19-21 (the
quest log offers a quest up to three levels early, and the cap is 20); quests use talking, visiting and fighting only (no quest
items: each new item needs an icon); no bounties yet (they need a notice board and a rare courier posting); three hidden finds only
(the world's total is held at 126 by DiscoveryLogTests). The zone data is written by `tools/wip/forge/make_forge.py`; its buildings by `ZoneBuilder.Forge.cs` (its own kinds:
foundry halls, furnaces, the Great Crucibles, fire pits, slag heaps, smelters' lodgings, the district wall and gate, the molten
channels and their bridges, cinder lamps, ingot stacks, the Spoke); the Lowtowns' Forge District Gate now leads here; `/forge`.
