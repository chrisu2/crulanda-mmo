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

## Status (p9-crown, 2026-10-15): step 1 written, not yet run
Written and compile-checked on branch p9-crown; **not built, toured or looked at** (no Unity run). `zone.crown` (EncounterContent/Zones/crown.json),
its kinds in `Scripts/World/ZoneBuilder.Crown.cs` (terraces, paving, the walk-in halls, the walls and gates with the Modular Castle Kit's
towers, tall wall and wall entrance, the Spire and the Lowtowns below), the North road from Oakhaven, the people at their posts with the
roles the halls, bank and Exchange branches hook, every trade's trainer and station in the Crafters' Court. Tests: CrownZoneTests
(EditMode), CrownZonePlayTests (PlayMode); the general zone tests take the Crown in. Captures: `--crulanda-world-capture` with
`--crulanda-zone zone.crown` (a view per place; "The Crown of Argentis" from the south-east, as the map).
**Chris, 2026-10-15 ~23:30 (verbatim): "i really want the castle done asap so double down on it".** Three helpers on it from 23:35: the
zone and buildings (p9-crown), the bank and the Exchange (p9-exchange), the halls' people and systems (p9-halls: Aethelgard's audience,
the Archive's reading room, the guild halls, the trainers, the dresser); the talent tiers for 19 paused.

## Status (p9-halls, 2026-10-15; code and data written and compile-checked, NOT yet run in Unity)
The halls' people, hooked by the resident's role (CapitalHalls.cs, EncounterSession.Halls.cs, EncounterHud.Halls.cs; data in
Quests/halls.json). E on a hall person with no quest business opens their window; Esc or walking away shuts it.
- **Aethelgard** (role `councilor`): an audience, a line for each topic picked by your level, your deeds (quests done, provinces
  reached, the Bureau's ledger read) and your standing with **The High Council** (a new faction, `council`, start 200, apart from the
  Concord's fixed -4000); "Has the Council work for me?" opens his clerk's errands. No body to strike (HallRoles.Untouchable).
- **His clerk** (`council-clerk`, named exactly "Clerk Odo Vantry"): three repeatable Council errands (levels 2, 6, 8), once a game day
  each; handing one in raises the Council's standing, refusing one lowers it by 50 (once a day each).
- **The Archive** (`archivist`; `/archive` opens it anywhere): a shelf a zone, the pages found to read, pages still lost, finds still
  hidden and one clue; a found page lent to be copied (once each) buys a word of a find not found and not shut.
- **Guild halls** (`guildmaster:<slug>`: lantern-watch, oak-and-ember, long-road, scree-hounds): motto, roster (about now first),
  Join for one in no guild, the notice board of two asks a guild (halls.json); members idle at the zone's `hall.<slug>` props, else
  round the master's post.
- **Trainers** (`trainer` or `trainer:<class id>`): the talent reset for coin, (10 + 5 a level) times the resets bought plus one, at
  most eight times; the class's abilities by level. **Dresser** (`dresser`): the Armoury tab. **Trades** (`profession-trainer:<id>`
  for mining, woodcutting, herbalism, cooking, blacksmithing, alchemy): take up or put aside a craft beside them, buy a gathering tool.
- Open: the talent window's free "Refund all" is still there (Chris to say whether resets become trainer-only); the residents
  themselves are the zone's (p9-crown).
## Status (p9-exchange, 2026-10-15)
Written, compile-checked, not yet run in Unity (EditMode `BankTests`, `ExchangeTests` to run; Chris's saves backed up first):
- **The Treasury Vault** (`TreasuryVault.cs`, `CharacterProfile.cs`): 48 slots shared by every character in profile.save.json
  (format 2), four more rows of 12 at 100, 250, 600 and 1,500 crowns; right-click or drag between the bags and the vault, stacks
  merge, quest items refused. Opened by a resident with role `banker`, or `/bank` where you stand.
- **The Exchange** (`Exchange.cs`; the world save, format 2): your lots from the bags for 12/24/48 world-hours with a buyout, an
  optional starting bid and a deposit (a tenth of value per 12 hours, back on sale); the sims list their goods at value times a
  markup by quality (1.5x poor to 5x legendary), buy your fair lots (more often the more are online; never at 2.5x fair), bid on lots
  with a starting bid, and now and then outbid you; coin, returns and lots won wait at the clerk (Collect). It runs each world-hour
  wherever you are and for the time away (two world days at most). Window: Browse (name, slot, level range, quality), My lots,
  Collect. Opened by a resident with role `auctioneer`, or `/exchange`.
- Not yet: the clerks placed in the castle (the zone step), sims buying gear or each other's lots, a listing's own bid amount typed in
  (Bid places the least next bid).

## Status (p9-cityinn, 2026-10-16; written and compile-checked, NOT yet run in Unity)
Chris (verbatim): "dont need the same exact inn in the castle. needs a redesign"; "will need lots more npcs in the castle with real jobs like in oakhaven"; "maybe see a castle off in the distance when looking north".
- **The Gilded Lantern** (inn variant 3, `ZoneBuilder.CityInn.cs`; GAME-ONLY): 22 x 16 at (31, 31), white stone, gilt courses, slate roof and a lit gilt lantern cupola on the ridge; a portico over a grand double door (3.2 m, the doorway lanes), step-free; inside a two-storey taproom with a gallery round three walls on columns, a walked stair up to it, the long bar with six stools, a hearth with a carved mantel, three booths, five round tables, the kitchen's range. Its plan is `InnPlan.For(size, 3)`; registered as ZoneInn "city" (staffed).
- **39 working residents** (GAME-ONLY names and lines) with days in `VillageWork` (clerks, scribes, pages, lamplighters, gardeners, sweepers, water-carriers, the Lantern's cook, servers and pot-boy, porters, the Treasury's day and night guards, stewards, traders, patrons, nobles, servants), homes in the gilt houses (18 households) or the Lantern's rooms. Their places are registered by the halls (desks in the Keep, the Council Hall, the Archive, the Exchange's floor, the Treasury, the guild halls, the Gilt Gate's yard, the lamps, the parterres; fountains are wells; streets along the roads). New loads: papers, a besom, a lamplighter's pole.
- **Frames**: "The Crown of Argentis" from a free camera 110 m over the south-east (`viewHeight`/`viewAim` on a landmark); the Council Hall from over the colonnade; the Lantern's front.
- **Oakhaven's skyline**: the Crown on its mountain to the north (`skyline` in the zone file, `DistantSkyline.cs`; the forest ring dips for it), capture `oakhaven-83-skyline-crown.png`.
- To run: EditMode `CrownZoneTests`; PlayMode `CrownLifeTests`, `CrownZonePlayTests`, `BuildingGroundTests`; the world capture of zone.crown and of zone.oakhaven.
## Status (p9-throne, 2026-10-16)
Written, compile-checked, not yet run in Unity (EditMode `CrownThroneTests`, PlayMode `CrownThronePlayTests` to run; captures of the
audience hall front, side and from the door to take). Chris: "falling through the floor. he needs more regal attire and sitting on a throne".
- **The floor:** the halls' floors were a 5 cm marble slab with no collider over the ground, and the keep's dais a 16 cm step with none:
  you (and Aethelgard) stood sunk in them. Every hall's floor now has a solid box under it at its top (`HallFloor`; the inn's too); the
  ground under every hall was checked level with its foot (it is: all inside the terraces).
- **The throne (GAME-ONLY):** a dais of three solid, walkable steps (in the navmesh), the runner carried up them; the throne of white stone
  and gilt, a cushion at a stool's height, gilt-capped arms, a high back with the Council's crown, a pointed crest and finials; a cloth of
  estate behind under a canopy with a gilt valance (`ZoneBuilder.ThroneDais`).
- **Aethelgard seated:** a seated post (`seated`, `facing` on a resident): the inn's sitting on the throne's seat, facing the hall, still
  seated and facing the hall when spoken to; spoken to from the foot of the dais. His clerk now stands at his right hand on the dais.
- **Robes of state** (look `councilor`; CANON: ancient, pale, white silk worked in geometric patterns, b1 ch.12:365; GAME-ONLY: the
  gold, the high collar, the chain of office, the rings; no crown). The clerk's look `scholar`: a dark robe, bands, a ledger. The wardens
  keep the Concord look.
## Status (p9-crowndetail, 2026-10-16; written and compile-checked, NOT yet run in Unity)
Chris (verbatim, of the Gilt Gate's stair): "will need to add more detail. search online for vines/ivy or something so it is not so white
. maybe add some iron or something"; then "we need a real garden. need to use some of our texture packs". `ZoneBuilder.CrownDetail.cs`
(all GAME-ONLY looks), hooked from Crown.cs in three lines (the end of BuildPaving; `parterre` builds `Garden`; Paved leaves a garden's
lawns to the grass): ivy clumps up the curtain wall, its towers, the gate's towers, the stair and ramp parapets and some houses and halls
(more facing north, by the gardens and round the Gilt Gate); damp bands, tinted stone panels in the masonry's own courses, rain streaks,
moss on the stair, dirt along the paving; iron studs, strap hinges and rings on the gate leaves, braziers at the gates, lanterns on
brackets and posts, handrails up the Grand Stair and the Gilt Ramp, a cresting on the Crown terrace's edge, grilles, door bands, banner
chains; a string course on corbels and slate caps. The gardens: leaf-carded box hedges, gravel walks, the packs' flowers in beds, lawns
of the zone's grass, topiary, small trees, rose arches, stone benches, urns. No colliders on any of it. To run: CrownZoneTests,
CrownZonePlayTests (the new detail test), then the Crown tour's gate, stair and garden frames for Chris.

**Chris, 2026-10-16 (verbatim, screenshots of a street and the Gilt Gate's north wall): "plants not even on the wall.only a few plants. inn does look better".** The ivy remade as dense mats set on the wall's real face, leaves flat on the stone, about twice as many clumps.

**Chris, 2026-10-16 (verbatim): "still sparse looking"** (the ivy made sheets, 74 fps at noon) **and "the signs in the castle need to be improved. the font on the building bleeds into the rock"** (each sign on a dark oak plaque with a gilt frame, the letters bright gilt in front). The Crafters' Court went to a helper (p9-court).
## Status (p9-court, 2026-10-16; written and compile-checked, NOT yet run in Unity)
Chris (verbatim, of the court's frame): "need more profession tools/small huts/vendored items". `ZoneBuilder.CrownCourt.cs` (GAME-ONLY; hooked by
one line in CityProp's default and one in BuildStations): eight open-fronted workshops round the Crafters' Court (`court_workshop`, variant =
trade: the Smithy, Leather-shop, Loom-house, Still-room, Cookshop, Bowyer's, Ore-house, Herbary), white stone and timber under slate or striped
canvas, the trade's name in gilt and its hung sign; each trade's station inside (the zone's station on the workshop's station spot, drawn as the
trade's own: anvil and quench tub, cutting bench, hand loom with clay warp weights, still-table, raised hearth, bowyer's bench); the trainer at
it facing out, a stall-keeper behind a counter (`court-vendor:<trade>`, stock by name in Items/court.json), five apprentices at the workshops'
benches; a well with a rope over a bar and a trough, a board of orders, benches, goods, two lamps. No machines. Stock is tools and makings
only (no ore, logs, herbs, hides, meat or bags: the existing no-coin-loop tests). **After main (p86-craft) is merged:** add `mat.fleece` to
Prue Calvert, and leaves to the keepers (e.g. the smith's, tanner's, weaver's, bowyer's, cook's and herbalist's leaves sold by name).
To run: EditMode `CrownCourtTests`, `CrownZoneTests`, `ProfessionDataTests`, `ItemTests`; PlayMode `CrownCourtPlayTests`, `CrownZonePlayTests`,
`NodePlacementTests`, `CrownLifeTests`; the Crown tour's frames "The Crafters' Court", "The Court Smithy", "The Court Loom-house" and the
station frames.
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
**Chris, 2026-10-16 ~01:55 (verbatim): "need a limited shared space. maybe 3 slots and maybe they can buy more in the future. the
remainder is character only".** So the Treasury Vault is two parts: a shared shelf of 3 slots for all your characters (more bought
later), kept in the profile; and the character's own vault (48 slots, rows bought with coin), kept on the character.
**Chris, 2026-10-16 ~02:05 (verbatim, a screenshot of the Crafters' Court: trainers on bare paving, one stall and a cookfire): "need more
profession tools/small huts/vendored items".** The Crafters' Court gets a workshop for every trade: small open-fronted huts or stalls
with each craft's tools and stock (a smith's forge, anvil, quench tub and racks; the leatherworker's stretching frames and hides; the
weaver's loom, spindles and bolts; the herbalist's drying racks and jars; the cook's hearth and pots; the miner's and woodcutter's tool
racks), and vendors selling each trade's tools and materials.
## Status (p9-exchange2, 2026-10-16; written and compile-checked, NOT yet run in Unity)
Chris (verbatim): "is the auction house fully functional?", then "id rather it be finished before testing". The Exchange finished:
- **Your own bid:** Browse has a bid box on each lot, filled with the least next bid; refused under it or over your coin; at the buyout it buys outright.
- **Sims buy gear:** your piece (or another sim's) goes to a sim it fits (its class's weight: mail, leather or cloth; a blade and shield
  only for Warriors and Paladins; its level) and betters in that slot, at a fair price for its purse; it then wears it (seen on its figure),
  the old piece a spare it may list.
- **Sims trade among themselves:** they buy the makings their craft's recipes take, what their pack for the bosses lacks (so a sim stocks
  its buff-up from the Exchange) and gear upgrades, at no more than a quarter over fair; an unsold lot goes up again 15% cheaper (three
  times at most, never under the stall's price); their lots stay between 60 and 120.
- **Crafted goods:** sims list spare leather and cloth pieces, dishes (hunters now cook their meat at the hearth), simples and draughts,
  and leaves they turn up hunting (never their pack).
- **Notices and prices:** a message when your lot sells, runs out or you are outbid or win (and on login, for the time away); "last sold"
  per item (three an item, 150 in all) in Browse and when listing ("Fair: about N; last sold for M").
- The world save's format is unchanged (only fields added that read as empty from an older world).
To run: EditMode `ExchangeTests`, `SimEconomyTests`, `BuffUpRulesTests`; PlayMode `ExchangeLiveTests`.
## Status (2026-10-17: facing, the Hall of Arms, the Lantern's door, the frame rate; written and compile-checked, NOT yet run in Unity)
**Chris, 2026-10-17 (verbatim, a frame of the Hall of Arms' trainers with their backs to him): "still people standing backwards at /crown also still laggy"**, then **"people stuck getting into inn"** (a frame of a knot of folk against the Gilded Lantern's column under the bay tree).
- **Facing:** the trainers had no workplace by their posts, so the 10-16 rule (face away from the prop at your back) never ran and they kept the yaw they were made with. Now anyone at a fixed post with nothing to look at, and anyone at a fixed post in a city, turns to the open side: 24 looks at chest height (1.5 m, over counters and desks) out to 8 m, each way scored over the half-circle it faces (`VillageLife.OpenSide`), so a row along a back wall faces the door, not along the row. Seated posts (Aethelgard) keep their own yaw; villages' workers still face their work.
- **The Hall of Arms dressed (GAME-ONLY):** a banner for each calling over its trainer (eleven: the calling's colour and a mark of primitives: crossed blades, a sun, an arrow, a leaf, a star, a dagger, a book, waves, a ring, a coin and die, a flame), the Council's two banners moved to the end walls, a red runner with a gilt edge from the door to the row, a bench along each end wall, iron candle-stands in the back corners (no lights of their own). No colliders.
- **The Lantern's door:** the portico's columns stood 0.95 m off the jambs with the bay trees' tubs a metre off the wall behind them: whoever came along the front was fed through a one-body gap across the lane of those coming out. The columns now stand 2.2 m off the jambs (the entablature wider), the tubs outside them in their line; the floor before the door is open. And nobody is sent to loiter in any inn door's apron (4.5 m either way of the door line, 1.5 m past the jambs): a street, green or wander spot that falls there is dropped (`VillageLife.InDoorApron`; posts and seats are kept).
- **The frame rate:** the capture rig measured 64 fps at noon (24.7 M triangles; 127 fps with no figures, 81 with no shadows). Expected effect, to be measured on the rig: (1) figures past 30 m cast no shadow and past 95 m are not drawn (their animation then culls too; 12 looked at a frame, 4 m of give; `VillageLife.FigureLod`): from most places in the city that is most of its 105 figures, and the figures were half the frame; (2) the Crown's small detail is cut into 24 m cells (`ZoneMeshes.SplitByCell`) and each cell is not drawn past 70 m (ivy, leaves, flowers, iron, the gardens' plants) or 130 m (the weathering on the walls) (`DetailCull`, distances in metres so the quality level's LOD bias doesn't move them); the trim and the gardens' ground always drawn; the walk-in halls' detail (it fades with its walls) and the gate's leaves left alone; (3) the iron and the gardens' plants cast no shadow (only the trim's courses and caps do). Built triangles are unchanged (the budget stays 3 M / 800k leaves); the drawn share from the Gilt Approach is held to half or less by the new test. Aim: the noon frame from 64 to about 100 fps on the rig.
To run: PlayMode `PostFacingPlayTests` (posts face the open side in the Crown, Oakhaven, Khaven and the Lowtowns; every inn's lane ends, line places and way up on the navmesh with 0.9 m of floor and nothing solid between), `CrownZonePlayTests` (new: `Far_detail_and_far_figures_are_not_drawn`), `CrownLifeTests`, `VillageDoorTests`; then the Crown's capture (crown-perf.txt: PERF noon all) and the Hall of Arms' and the Lantern's frames for Chris.
**First run in Unity (2026-10-17 18:34):** the Crown at noon 87 fps (was 64-74), 105 with no shadows. Fixed after it: the detail's culler hid nothing (a CullingGroup, whose distance bands are only worked out when its camera renders): now it measures a share of its cells each frame from the main camera itself (all in a quarter second, 3 m of give); the test measures from a camera at the Gilt Approach. The Golden Cask's first places in line stood in its porch post's hole in the navmesh and the Cracked Hearth's on its edge: each inn's two lines are now fitted to the floor round the door when the life starts (`VillageLife.FitDoorLines`: 0.3 to 1.2 m aside, straight back or turned up to 60 degrees, the first set whose six places are all open floor with a straight way to the lane's end). A post now faces the clearest way within 30 degrees of its open side (a court apprentice looked at a post 2.8 m off).
