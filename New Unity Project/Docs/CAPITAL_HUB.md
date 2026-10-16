# The capital: a castle as the central point (PROVISIONAL; Phase 9)

## Chris's brief (2026-10-15 ~22:25, verbatim)
"we need to implement a castle (central point) for auction house, library (lore), leader of crulanda (canon lore), guild halls, bank,
etc more as we come up with it. check maps for location it should be located"

## What the maps and the books say (checked 2026-10-15)
- **The overworld map** (D:\code\crulanda\maps\crulanda_overworld_map.png) puts **Argentis, the Sky Bastion** at the centre of the land,
  above Mount Umbra, with the Iron Citadel to the north, the Corrupted Forests west and east and the Ashlands to the south-east. CANON.
- **The city map** (maps\argentis_city_map.png): the walled **High City** fills the north half, a crown of white-and-gold towers, a
  domed hall and a cathedral-like spire, on a raised wall; **the Spire** stands below its south wall; the **Lowtowns** spread south and
  west of it and **the Warrens** coil in the south-east. world_bible.md: "The Crown: the highest tier, built of white stone and gold,
  housing the Council and the wealthy". CANON.
- **CRULANDA_LOCATIONS.md** already lists Argentis as the "City hub candidate".
- **Who rules:** the High Council. **High Councilor Aethelgard**, "the face of the Council" (world_bible.md, High Concord), ancient,
  "pale skin pulled tight against his skull", speaking of "The Greater Good" (b1 ch.12:31, 365-393); **Lysara Velthorne**, the High
  Concord's leader, "likely the power behind Aethelgard" (world_bible.md). There is no king or queen in canon. Aethelgard is Book 1's
  antagonist and alive in the game's time (set before the books): he may be shown, never killed (shared rules).
- **The library:** the High City's archives (Master Phelan seeks "to use the archives", world_bible.md) and the Silent Pilgrims' Archive
  of Silence (south; CRULANDA_LOCATIONS.md). The game's Lore tab (Phase 8.6) gives the library its purpose.

**So the castle is the Crown of Argentis**, in the High City at the centre of the map, north of the Spire and above the Lowtowns
(ZONE_LOWTOWNS.md, 16-19), which become the district at its feet.

## What goes in it (Chris's list, then the open "more")
Auction house; library (the lore: the Lore tab's pages, the book of lore, the Chronicle); the leader of Crulanda (canon); guild halls;
a bank; "etc more as we come up with it".

## Chris's answers (2026-10-15 ~22:30, the question prompt, verbatim)
- Location: "Yes, the Crown of Argentis (Recommended)".
- Access: "Everyone, from level 1 (Recommended)": a safe city zone with a road from Oakhaven, so the auction house, bank and guilds
  work for every character now; the Lowtowns below stay a 16-19 danger zone.
- The leader: "High Councilor Aethelgard (Recommended)": visited, never fought.
- Order: "Castle first (Recommended)": the Crown before the Lowtowns.

## The design (2026-10-15 ~22:35; PROVISIONAL until Chris sees the frames)

### The zone: `zone.crown`, "The Crown of Argentis" (CANON place; a safe city)
- **Safe:** no hostile camps; the Council's wardens on the gates and in pairs on the streets (the Concord's warden look, CANON-EXPANDED);
  levels "all" on the map (levelMin 1); no pvp, no pulls; the death rules unchanged elsewhere.
- **The way in:** Oakhaven's North road gets an exit, "North road to Argentis (the Crown)", arriving at the **Gilt Gate** (GAME-ONLY name)
  in the High City's outer wall; from the gate the **Processional Way** climbs in long ramps and stairs (no lifts, no gondola: no machines)
  between white stone and gold to the **Crown Square**, the central point. A second gate, the **Low Stair**, goes down to the Lowtowns
  (ZONE_LOWTOWNS.md; "dangerous, 16-19"; it opens when that zone does).
- **Size:** about 320 m square, dense; the map's crown of towers on the skyline (the castle keep, the domed Council Hall, the cathedral
  spire), the Spire below the south wall, the Lowtowns' smoke beyond it.
- **The look:** white stone, gilt roofs and finials, blue-grey slate, banners of the Council (a white field, a gold crown: GAME-ONLY
  device), clipped gardens, fountains, colonnades; the Village kit and KayKit pieces where they fit, new kinds where not (below).

### Around the Crown Square (each a walk-in building with its people)
| Place | What it does | Label |
|---|---|---|
| **The Council Keep** (the castle, the central point) | the leader's audience hall; the Council's edicts board (lore and foreshadowing: the Soul Tax, resonance registers) | CANON (the Council in the Crown); the keep's name GAME-ONLY |
| **High Councilor Aethelgard** in the audience hall | the leader of Crulanda: an audience (dialogue that changes with your level and deeds), his clerks give the Council's errands (you may refuse; the Council's standing goes down); never fought, never killed | CANON (world_bible.md; b1 ch.12:31, 365-393) |
| **The Exchange** (the auction house) | list items for coin; buy what sims and you list; bids and buyouts; runs by the world clock while you are away | GAME-ONLY building; its factor from Port Caelum's Trade Princes is CANON-EXPANDED (world_bible.md, the Glittering Coast) |
| **The Treasury Vault** (the bank) | a vault of 48 slots, more bought with coin (four rows of 12); shared by all your characters? (decision 1) | GAME-ONLY |
| **The Archive** (the library) | every lore page and Chronicle page you have found, read at desks; shelves by zone showing how many are still lost there and one clue each (Phase 8.6's Lore tab); the archivist trades a found page for a clue | CANON-EXPANDED (the High City's archives, world_bible.md) |
| **The Guild Row** (guild halls) | a hall for each guild (the Lantern Watch, Oak and Ember, the Long Road, the Scree Hounds): its banner, its master, its notice board, its members about when online; your own guild's hall opens to you | GAME-ONLY (the guilds of round 27) |
| **More (Chris: "etc more as we come up with it")** | the Gilded Lantern inn; class trainers (talent resets for coin); the Armoury's dresser (looks); the best vendors; a Chronicle hall (achievements and titles); a stable is not built (no mounts yet) | GAME-ONLY |

### The new systems
1. **The bank:** `Progress.bank` (48 slots, buy more), opened at the Treasury's clerk; the save format goes up once with Phase 8.6's
   recipes (format 11: one migration, Chris's save backed up first). Decision 1: per character, or shared by all your characters.
2. **The Exchange:** listings live in the world save (sims list their crafted goods and gathered materials from SimEconomy at value times
   a markup by rarity; you list from your bags with a small deposit; durations 12/24/48 world-hours; sims buy what is fairly priced, more
   often the more sims are online; your coin is waiting at the clerk). Search by slot, level and quality; your listings and history.
   No real money, no cross-player trade (single player).
3. **Guild halls:** each guild gets a home in `zone.crown` (its members spawn there when online and idle), a guild notice board
   (the guild's runs and asks), and later a guild vault.
4. **The Archive:** a reading room over the Lore tab's data (DocumentDef, ZoneLore), with the clue rule of DiscoveryLog.
5. **Aethelgard:** a person with a dialogue tree (the Council's "Greater Good", what he says to a level 1 and to a level 15), and the
   Council faction's standing (factions.json already has the Council: "Order at any cost").

### Kept out on purpose
- Aethelgard is never fought and never dies (shared rules); the Spoke, the Prism and Kaelen are not shown or named; the purges and the
  burning of the Lowtowns are after the game's time.
- No gondola, lift or any machine on the climb.
- Lysara Velthorne is not placed (Chris chose Aethelgard).

### Order of work
| # | Step | Size | Tests |
|---|---|---|---|
| 1 | The zone and its buildings (new prop kinds: keep, dome_hall, cathedral_spire, mansion, colonnade, fountain, statue, city_wall with gates; white-and-gold art) and the North road from Oakhaven | L | BuildingGroundTests, ZoneContentTests, ZoneNavTests; captures |
| 2 | The bank (with format 11) | M | BankTests, SaveMigratorTests |
| 3 | The Exchange (listings, sims buying and selling, the UI) | L | ExchangeTests (prices, durations, sims' buys, coin) |
| 4 | Guild halls, the Archive, Aethelgard and the inn, trainers, dresser, vendors | L | GuildHallTests, LoreBookTests, ZoneLoreTests |
| 5 | Captures for Chris (the gate, the square, each hall, Aethelgard) | S | the Crown tour |

### Open decisions (recommended first)
1. **The bank:** shared by all your characters (recommended: the classes are separate characters, and moving things between them is
   what a bank is for), or one per character?
2. **The Exchange's sims:** sims list and buy (recommended: the market is alive), or only you list and a factor buys at a fixed price?

## Chris, 2026-10-15 ~22:40 (verbatim, while the design was written)
- "use the map for reference design"
- "also make sure all professions are located in the castle"

**So:**
- **The layout follows maps\argentis_city_map.png:** the High City as the map draws it, a walled plateau on a high curtain wall with
  square towers, the land falling to the sea on the west; inside, the crown of gold-roofed towers at the north-centre (the Council Keep,
  the tallest spire), the **domed hall** east of it (the Council Hall, where Aethelgard sits) and the many-spired cathedral-like mass west
  of it (the Archive); streets of tall gilt houses down to the south wall; the **Spire** standing just outside the south wall's middle,
  wreathed in cloud; from the south wall the Lowtowns spread south-west and the Warrens coil south-east (both below, seen from the wall).
  The capture tour puts a frame from the south-east like the map's view, to compare side by side.
- **Every profession has its trainer and its station in the castle** (a Crafters' Court off the Crown Square): the gathering trades
  (Mining, Herbalism, Woodcutting, Skinning, Fishing if it exists) with their trainers, and the crafts (Blacksmithing at a forge,
  Simpling (Alchemy) at a still-table, Cooking at the inn's kitchen, Leatherworking, Weaving at a loom, and whatever Phase 8.6 adds) with
  their stations and trainers, all usable from level 1; the Treasury beside them for materials.

**Decisions answered (2026-10-15 ~22:45, the question prompt, verbatim):** the bank "Shared by all (Recommended)": the vault lives in
the profile save (profile.save.json, beside the characters), not on a character, so the character format is untouched by it (the
profile's format goes up one, Chris's saves backed up first); the Exchange "Sims list and buy (Recommended)".

**Chris, 2026-10-15 ~23:30 (verbatim): "i really want the castle done asap so double down on it".** Three helpers on it from 23:35: the
zone and buildings (p9-crown), the bank and the Exchange (p9-exchange), the halls' people and systems (p9-halls: Aethelgard's audience,
the Archive's reading room, the guild halls, the trainers, the dresser); the talent tiers for 19 paused.

**Chris, 2026-10-16 ~00:30 (verbatim): "dont need the same exact inn in the castle. needs a redesign".** The Gilded Lantern gets its own
design: a city inn of the High City (white stone and gilt, not the village's timber inn), its own shape and rooms.
**Chris, 2026-10-16 ~00:35 (verbatim): "will need lots more npcs in the castle with real jobs like in oakhaven".** The Crown gets a
working population as Oakhaven has (VillageWork: carriers, servants, guards' rounds, lamplighters, gardeners, clerks, cooks, grooms,
messengers, merchants' errands), many more than the first pass's six walkers.
**Chris, 2026-10-16 ~01:05 (verbatim): "maybe see a castle off in the distance when looking north".** From Oakhaven (whose North road
leads to the Crown) the Crown of Argentis shows on the northern skyline: the walled plateau, the keep's spire, the dome, faint in the haze.
**Chris, 2026-10-16 ~01:15 (verbatim, with a screenshot of the Gilt Gate's stair): "will need to add more detail. search online for
vines/ivy or something so it is not so white . maybe add some iron or something".** Searched (CC0 sources: Quaternius, Kenney, Poly
Pizza); chosen: ivy grown by the builder (painted style, any wall), weathering, and ironwork; a CC0 ivy pack only if the grown ivy reads badly.
**Chris, 2026-10-16 ~01:30 (verbatim, with a screenshot of the parterres' red and yellow balls in plain boxes): "we need a real garden.
need to use some of our texture packs".** And of the inn (screenshot): "table and mug still not meeting. hovering over tables" (fixed in
the inn code: the dressing sits on the table's own top), "all trainers standing backwards" (fixed: a post in a city faces the room),
"floor is blocked in armory" (fixed: no plinth across a hall's door).
**Chris, 2026-10-16 ~01:40 (verbatim, a screenshot of the audience hall): "falling through the floor. he needs more regal attire and
sitting on a throne".**
