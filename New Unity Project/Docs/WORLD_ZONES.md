# World zones — generated from data

Status 2026-09-28. The game opens in **Oakhaven** (`Assets/Crulanda/Scenes/Oakhaven.unity`). The Quiet Trail
(`PlayableEncounter.unity`) stays in the build as the automated test map.

## How a zone is made
- A zone is a JSON file in `Assets/Crulanda/EncounterContent/Zones/` (format: `Scripts/World/ZoneDefinition.cs`).
  Metres, x = east, z = north, origin at the zone centre. Everything is text, so zones can be authored and diffed like talents.
- `ZoneBuilder` (runs first, execution order -800) generates at load:
  sculpted ground (flat village core, rolling hills outside `flatRadius`, carved creek beds, dead-flat unmade land),
  a painted ground texture (grass variation, dirt roads with wheel ruts, tilled/stubble fields, clearings, muddy banks,
  grey unmade ground), water ribbons, props, forest edges on three sides, the Wasting, lighting/fog/sky and boundaries.
  Scenery is static-batched after generation.
- Prop kinds: house, inn (walk-in), barn, mill, coop, forge, stall, oven, tannery, woodpile, leathershop, dryhut, kitchen, gamerack, well, dead_oak, great_oak (Oakhaven's living Great Oak, with a stone bench ring), tree, pine, fence, hedge, haystack, cart, barrels, crates, lamp, grave,
  rock, bridge, signpost, ruin. Solid props carry `NavBlocker`; bridge decks carry `NavWalkable`.
- `EncounterNavigation` builds the navmesh from the generated ground + bridge decks, cuts out blockers and blocks water
  so enemies path over bridges. Without a zone it falls back to the old name-based Quiet Trail logic.
- Art palette: `Assets/Crulanda/World/Art/ZoneArt.asset` + materials/textures made by **Crulanda > World > Build Oakhaven**
  (`Editor/ZoneSceneBuilder.cs`). They are real assets so transparent/emissive shader variants survive player builds.
  Rerunning the menu item never overwrites existing materials or the scene.
- The session reads spawns (player, companion, recovery, enemies, leash), objectives and the zone title from the JSON.
  Saves store `zoneId`; arriving from another zone (or the old test map) starts you at the zone entrance.
- Characters: `ActorVisual` builds a placeholder humanoid (Warrior, Druid, Healer/Mira, Concord collector/warden, legacy
  sentry) with movement-driven walk/idle animation. Stand-in until authored models exist.

## Zone size (grown 2026-09-30)
- Oakhaven is 380 m across, Khaven 340 m, the Peaks, the Ash Rim and the Verdant Shore 360 m each (they were 240-260 m). Chris, 2026-09-29:
  "zones do need to be bigger with more places and secrets to explore".
- Each village core kept its coordinates. What belonged to the edge moved out with it: the exits and the arrival points into
  them, the roads to them, the creeks off the map, the Wasting's curtain (the same distance from the east edge), and the
  player start in the Peaks and on the Rim (they stood at the entry). The forest edge, boundaries and backdrop follow `Half`.
- The new ground carries new places, camps, groves, fields, tall grass, critters and secrets in every zone (sections below).
  New entries were appended, so the old props keep their random draws and the old camps their mob ids; the old groves and
  the forest edge re-scatter inside their own areas (they draw from the zone's stream after the new props).
- Villagers keep their errands within 125 m of the village's middle (`WorldLife.VillageReach`): the outer farms and woods
  are the wild's.
- The ground paint keeps about 8 px a metre (up to 3072 px) and the map about 4 px a metre (up to 2048). Each build logs its
  time by phase ("Zone zone.oakhaven (380 m) built in ... ms (prepare ..., ground ..., ...)").

## Checking visuals without playing
- `Crulanda.exe --crulanda-world-capture <dir>`: scenic tour screenshots (HUD hidden), isolated temp save.
- `Crulanda.exe --crulanda-ui-capture <dir> [--crulanda-class class.druid]`: talent panel + combat HUD screenshots.
- `Crulanda.exe --crulanda-wardrobe-capture <dir>`: every main-hand and off-hand family and variant on bare mannequins, labelled, seven a shot (`01-weapon-rack-a`..`g`, `02-shield-wall-a`..`c`), then poor-to-epic ladders at dusk (`03-quality-ladder-a` blades, `-b` shields, `-c` lanterns); full generated kits at levels 2, 5, 8, 10 and 13, front and back (`04-sets-front-a`..`d`, `05-sets-back-a`..`d`: a common, b uncommon, c rare in mail and plate, d rare in cloth and leather); every armour family and variant up close, four a shot (`06-helms-a`..`e`, `07-shoulders-chests-a`..`f`, `08-hands-legs-feet-neck-a`..`l`); one kit in all ten palettes (`09-palettes-a`, `-b`); the Blacksmith's pieces by metal tier (`10-crafted`); a rare level-13 kit standing, walking, sneaking, sitting and swimming (`11-in-motion-front`, `-back`); the Druid in it in each form and in her hood (`12-druid-forms`). Isolated temp save; HUD, player, enemies and village hidden. Latest shots: workspace `work\ui-captures\wardrobe`.
All three need a visible window (not -batchmode). Latest shots: workspace `work\world-captures`, `work\ui-captures`. A landmark that sets `view` (where to stand, facing it) with `viewPitch`/`viewZoom` is shot from there as authored. Other landmarks, and the first exit's waystone, are shot from a searched viewpoint with the player hidden (`LandmarkView`: a named prop is framed on its own bounds, a building from its front). When no spot is clear, the tour falls back to the orbit from the south-west and logs it.

## Oakhaven (CANON-EXPANDED)
Canon: an eastern agricultural hub erased by an accelerated, localized Wasting that hid the Council's abduction of
resonance candidates (source: `D:\code\crulanda\maps\interactive_map.html`, `maps\oakhaven_village_map.png`).
The zone is Oakhaven before that erasure (the game is set before it happens): the Great Oak on the green, the Golden Cask inn (canon name, book1\chapter_4.md), the communal well,
fields, Oak creek with two bridges, and the Wasting eating the eastern edge as grey static.
GAME-ONLY / PROVISIONAL: the Concord collectors and warden, Mira, exact building placement, the chapel ruin and graves, and the old wayshrine at the edge of the grey.
The outer ring (grown to 380 m): **Crowsfoot Ridge** over the dungeon (its name GAME-ONLY; the hill above Oakhaven's valley,
from whose crest the village is first seen, is CANON, book1 ch.4), the Mastwood, Moss's lodge, the Old Fold and Old
Whitefoot's den, the Bound Stone, the Cider Barn and Withy pool (CANON-EXPANDED: Oakhaven's cider and deep springs), the Old
Barrow (CANON-EXPANDED: the First Kin), Hollin farm, the Hallow's Creek milestone (CANON-EXPANDED: Hallow's Creek, the first
settlement the Wasting took, world_bible.md) and the Watchtower on the east road. Each carries its `canonStatus` in the data.
The trades' buildings (GAME-ONLY, 2026-10-01; `tools/wip/professions/BUILD_PLAN.md` step 1): the ten village houses carry
names, so a door reads "Knock · Tanner house" (Reed, Farrow, Crane, the Elder's, Rusk, Jory's, Vell, Pell, Thorne, Tanner); two
new houses, the **Carder farmhouse** west of the Tithe barn and the **Crisp cottage** on the Mill lane; **Tanner's leather shop**
on the South road below the Cask; **Lisbet's drying hut** by the Last harvest field; **the Cask's kitchen**, a lean-to on the
inn's back wall; and **Moss's game rack** in the lodge yard. All six are appended to `props` and checked to move nothing:
`WorkshopDataTests` (the placement rules) and `VillageStreamTests` (Oakhaven built with and without them: trunks, scenery and
the creek's points are the same). Khaven's four village houses are named too (Grane, Vey, Jenn, Tabor).
Trades in Oakhaven (GAME-ONLY, 2026-10-01; BUILD_PLAN step 4; tier 1, every node at skill 1, so a new character gathers at once):
- **Ore** (Crowsfoot copper): four seams at the foot of the Crowsfoot scarps either side of the Hollow's mouth, one by the big rock
  south of the North pines, one on Crowsfoot Ridge's crown over the Drop; four rich seams on the Hollow's floor, against the
  walls between the deserters' posts, never in a camp or among the camp's things: the low passage west of the camp chamber,
  the foot of the Store Caves where the Deep Stair begins, half-way down the Deep Stair, and at its foot before the Echoing
  Hall (the Drop's stepped floor has none). Mining needs a pick (merchants, the smith).
- **Timber** (Harrow oak windfalls): at the edges of the broadleaf woods, the stump on the wood's side: the Grey-edge copse, the
  Harrow wood (east and south edges), the South copse, the Southwood, the Mastwood, the Brook spinney and the Bound copse.
  Woodcutting needs a hatchet.
- **Herbs** (yarrow): the eight Yarrow props on the meadows (still the quest's yarrow while Lisbet's quest wants it) and two more,
  on the Harrow downs west of the farm and on the meadow north of Brook pond. Bare hands.
- Selling ore or timber in the village is the forge's delivery for the day, herbs the stall's: Brannoc Vell talks of the
  Crowsfoot ore, the merchant of the fresh herbs.

## Khaven Village (CANON-EXPANDED)
Canon names from `maps\khaven_village_map.png`: the Cracked Hearth, two Fallen Smithies, the Blood-Stone Well, the Gallows
Tree, the Crypt-Keeper's Hovel, the Whispering Wood, Gloom Creek and the Carrion Cliffs. Built as a walled village at dusk
with gate towers, a dead grey wood, dark creek water and an old crypt under the cliffs.
Biome `gloom`: hard grey-brown earth, thin dry grass and no flowers, grey dead woods, withered pines and brush, and a drained
rose-violet grade. The dusk holds all day: `lighting.sunHigh` keeps the sun low, and `skyTint`/`skyExposure`/`skyHaze` give a
dusky sky, while night still falls on the global clock. No Wasting here: only zones whose JSON has a `wasting` block get one.
GAME-ONLY / PROVISIONAL: the Sandthrone outriders holding it (Sandthrone is a canon mercenary faction), the Pale watcher
(canon "Pale Things" as cosmic auditors), the crypt and all exact placement. GAME-ONLY: the beacon mound east of the walls (a
`Beacon mound` shape, 3.6 m, its old beacon a lookout secret). GAME-ONLY looks: the Cracked Hearth's split chimney with the fire showing through (a reading of the canon name; the map draws it as the inn), the drowned graveyard's flood pool and heaved graves, and the creek barrow's turf mound.
Grown to 340 m (2026-09-30): the Gibbet Crossroads and the Corpse Road (a new bridge over Gloom Creek), the Plague Pit, the
Drowned Fields, the Carrion Heights above the canon Carrion Cliffs and the Charnel Barrow on them, the Old Bound Wall and the
Fallen Watch on it, the Listener's Hut (CANON-EXPANDED: a Silent Pilgrim's) and the Hush, the densest of the Whispering Wood.
Trades in Khaven (GAME-ONLY, 2026-10-01; BUILD_PLAN step 8; tier 2, every node at skill 20):
- **Ore** (Carrion bog-iron): at the feet of the Carrion Cliffs (two on the long cliff's creek side, one off the east cliff,
  one under the low cliff, one behind the far cliff, clear of the Carrion boars), two under the Carrion Heights' face, two at
  the Grey scarp's foot and one behind the North ridge (clear of the outriders' camp).
- **Timber** (black-pine windfalls): at the pine woods' edges: two below the North pines, the Ridge pines' west and south
  edges, the east edge of the eastern Ridge pines, below the northern Ridge pines and two by the Bound-wall pines.
- **Herbs** (mourner's cap): the seven Mourner's cap props on Gloom Creek's banks are worked as nodes (still the quest's caps
  while Wenna's quest wants them), and three more round the Drowned graveyard (west, south and east of it, outside the
  hollows' camp), which count for the quest too.

## The Shattered Peaks (CANON-EXPANDED), levels 6-8
Grown to 360 m (2026-09-30): the Signal Tower, Goatherd's Shieling, the Sealed Adit (CANON-EXPANDED: old mining tunnels under
the Peaks, book2 ch.19), the Cold Tarn (the Peaks' first water, swimmable), the Listening Shrine (CANON-EXPANDED: the Silent
Pilgrims), the Avalanche scar with a patch of unmade grey (CANON-EXPANDED: the void eating the stone) and the Broken Post.
Two new inner scarps (the East wall, the Goat-path scarp) keep it a pass between walls, not a plain.
Canon: a jagged range on the eastern border that holds back the Wasting while the void slowly eats the stone (world_bible.md).
Sandthrone mercenaries hold it and charge a toll (book1 ch.5).
GAME-ONLY / PROVISIONAL:
- this pass, its toll gate and toll-house;
- the pilgrims' rest and its people, the ruined waystation and the cairns;
- all camps and placement.
Biome `mountain`: rock and scree painting (rock on steep ground), sparse tufted grass, pines, strewn boulders.
Relief (`ZoneBuilder.Crag`): steep knolls, rock ribs and walls climbing to the zone edge rise only off the keep grid, which
holds roads, clearings, camps, props, people, landmarks, exits, arrivals and a 6 m way from each to its nearest road, so all
of it stays reachable. A cliff's `lift` raises a shelf behind it (negative: on its -z side): the Umbra scarp, the High ledge
and its wall, the Eyrie crag, the North wall and the South and East scarps.
Trades in the Peaks (GAME-ONLY, 2026-10-01; BUILD_PLAN step 8; tier 3, every node at skill 40):
- **Ore** (Adit iron): two at the Sealed Adit's face (north and south of the secret and the jambs), one by the boulder at the
  edge of the Adit yard, two under the Umbra scarp, two under the Goat-path scarp, one each at the feet of the East wall, the
  South scarp and the Switchback crag. None on the spoil heap (its ring of boulders fills it) or in the Avalanche scar (the
  rockhides and Old Scree-Tusk fill it).
- **Timber** (stone-pine windfalls): at the pine woods' edges (the Wolf pines' south edge, clear of the pack; the High, Gate,
  Ore-road, East, South and Umbra pines) and one at the Avalanche deadfall's north edge.
- **Herbs** (tarnwort): seven round the Cold Tarn's shore and three on the Shieling's hay meadow, off the mown field.

## The Ashland Rim (CANON-EXPANDED / PROVISIONAL), levels 9-10
Grown to 360 m (2026-09-30): Wain's Rest on the salt road, the Last Orchard, the Drowned Leviathan (a spine and ribs half in
the ash), the Ash Pit (the cult's), the Walled Mouth (an Ash-Walker cave walled up), the Fraying, the Silent Statue
(CANON: the Silent Statues, world_bible.md), the Hunters' Knoll and the Reach-Stones (rows of stones marking the grey's yearly
advance). Canon labels on each in the data.
Canon (book1 ch.20): the Ash-Walkers (Chieftain Grohl, Mother Vane the Salt-Speaker) live in caves along the Wasting and fight
Weave-Eaters with Salt of the First Sea. The Cult of Ash and its iron-wired bone masks are canon (book1 ch.10).
The canon Ashlands only take their full form later (after the Spoke fires), so this pre-erasure rim is PROVISIONAL.
GAME-ONLY:
- the zone's name and roads;
- the shrine's location, Cinderfold, the ash hounds and the brood;
- all placement.
Biome `ash`: grey cracked ground with no grass, dead trees and falling ash.
Trades on the Rim (GAME-ONLY, 2026-10-01; BUILD_PLAN step 8; tier 4, every node at skill 60): thin pickings, spread out and
one at a time.
- **Ore** (cinder): at the feet of the Eastern Ridge (its north, south, far north and far south crags, on the enclave's side),
  the South rim, the West scarp, the North rim and the Walled Mouth scarp (west of the Mouth). None on the Ash Pit's rim: the
  cultists' camp fills it.
- **Timber** (fallen ash-snags): one at the edge of each of eight dead woods (the Ashen snags south of the hounds, Cinderfold
  orchard, the Rim, South, West and Ridge-back snags, the Grey thicket, the North snags). None in the Last Orchard, where the
  orchard hounds lie.
- **Herbs** (cinder-thistle): single plants in the open ash between the woods, the ridge and the pit, none by Last-Light.

## The Verdant Shore (CANON-EXPANDED), levels 11-13
Canon (Book 3, *The Verdant Shore*): the western coast's "Verdant Ocean", "a forest that didn't know when to stop", giant trees
"their trunks as wide as houses", broad flat leaves swaying "like a green tide", "a thousand shades" of green; the **Veridian
Keepers** (native wood-beings; Oak-Bane and Willow-Whisper are named), the **Veridian Temple** and the Root-Mother, Veridian sap
"the color of liquid emerald", the coast's salt-flats. Chris, 2026-09-30, with lush style references (style only): "next zone we
need a truly lush zone". The game is set before the books' exodus, so the survivors, the First Seed and the Salt-Wall are not
here; the Keepers, the Temple and the forest are. The fifth zone, 360 m, reached from the Ash Rim by the Old west road over the
ash-mountains (the breadcrumb `main.ashrim.5`); the level cap is 13 (talent points stop at 10).
- **The arrival:** the Ridge of Long Shadows runs along the whole east edge, 15 m up, basalt crags on its face. You arrive on its
  crest and eleven metres on is the lookout at the lip (the Last salt-road shrine, twin of the Rim's): the Verdant Ocean below,
  giant trees standing out of the canopy, Rootfast's lanterns 165 m off, Mossveil Falls to the right. The Ash-road winds down in
  four legs.
- **Places (12 landmarks, canon labels in the data):** the Ridge of Long Shadows; the Verdant Ocean (the first view); **Rootfast**,
  five Keeper treehouses round a lamp-lit green (Willow-Whisper's at its head), glowing caps in the gaps, a rope bridge over the
  Wending; **the Veridian Temple**, a sunk root-stair under a cage of six calcified root-arcs with giant glowing caps, approached but
  not entered; **Mossveil Falls** into its pool, with a lookout on the ledge above (a stepped way up its east side); the Mistmere
  and its reed-beds; the Whispering Glade (glowing caps and stags, best at night); the Fern Hollow; the Fallen Ghost-Oak (a
  `fallen_giant`, spiders in its crown); the Salt-Flats on the coast, with tidal pools and the glass-ship scout's camp; and the
  Pale's touch shown early: **the Greying** (a grove going grey, withered Keepers, Greyheart) and **Palemist Hollow** (mist-walkers
  and pale shadows round an unmade heart); and under the Temple, **the Root-Mother's Deep**, the zone's dungeon (GAME-ONLY; see
  "Caves you walk into").
- **The Wending**, a slow river from the falls' pool west past Rootfast to the flats.
- **People:** Willow-Whisper (elder), Oak-Bane (warden), Moss-Lantern (merchant), Alder-Knot (bark-carver) and Reed-Song (hunter), all
  Keepers with the Keeper body and their own lines (`life.mood: keepers`); Sister Iselle, a Silent Pilgrim (`pilgrim` role: ochre
  linen, a silver mask, a brass listening-tube, CANON look) and Ondine Varro, a glass-ship scout from Port Caelum (merchant).
- **Camps (9):** Mistmere reed-boars (11, ambush), Antler Meadow stags (11-12), Fallen Ghost-Oak spiders (11-12), the Briar Way
  (12-13), the Greying's withered Keepers (12-13), Palemist mist-walkers (12-13) and pale shadows (13), and two elites, **Greyheart**
  (13) and **Old Ninebranch** (13), and six more down the deep (see below). New looks: `keeper`, `stag`, `spider`, `bramble`.
- **Quests:** the breadcrumb from Grohl; a main chain of five (Let the Wood Learn You, The Briar Way, Grey at the Heart, A Fog That
  Tastes of Lightning, The Root-Mother's Deep); seven side and NPC quests; five Chronicle pages; a new faction, the Veridian Keepers.
  `QUEST_DESIGN.md`.
- **The lush art** (`ZoneBuilder.Verdant`, `PlantField`): see "Painted plants and lush props" below.
- **Trades** (GAME-ONLY, 2026-10-01; BUILD_PLAN step 8; tier 5, every node at skill 80; a level-13 character with a low skill
  works them at once, as "hard going"): rich pickings.
  - Ore (Veridian): at the feet of the four basalt crags and on the Ridge of Long Shadows' south and north shoulders; four
    rich seams on the Root-Mother's Deep's floor: past the Gallery, at the Sap Well's far end, and either side of the Heart,
    each clear of the deep's withered, walkers and briars.
  - Timber (ghost-oak windfalls): at the edges of the Ridge-foot, Westbank, Riverbank, Rootfast and north Ridge-foot
    canopies, two by the Tappers' wood and one by the Mere canopy. None by the Fallen Ghost-Oak: its spiders' camp fills it.
  - Herbs (dewfern): four round the Mistmere's shore and six round the Fern Hollow, among the ferns' edge, clear of
    Lantern-Moss.

## Painted plants and lush props (`PlantField`, `ZoneBuilder.Verdant.cs`)
- **PlantField:** painted fern, broad-leaf and reed cards (ZoneSceneBuilder paints them; `Crulanda/Grass` draws them, with `_Wither`
  to drain the paint's green and `_Glow` for night-glowing flowers) and flower drifts, baked into 24 m cells and instanced near the
  camera. Ferns in the shade round every trunk, in the groves and along the forest edge; broad leaves along the banks; reeds at the
  waterline; flowers in drifts of one colour. A mix per biome: the meadow's, the mountain's few ferns, Khaven's withered ferns and
  dark reeds, the verdant shore's riot (glowing flowers after dark); nothing on the ash. Its own random stream.
- **Dead trees** (`DeadTree`, 2026-09-30, Chris: they looked like umbrellas): a tapered, bent bole on a root flare (`Bole`), three to
  five limbs leaving it at different heights, crooked, tapering to points, branches off their length; a massive one for `dead_oak`.
  The zone's draws are taken as the old tree took them, so nothing else moves.
- **Props:** `giant_tree` (about 28 m at scale 1, a buttressed bole, a crown of some forty leaf clusters; variants green,
  yellow-green, autumn); `treehouse` (a Keeper dwelling grown into a giant tree: a round house with porch, door and lantern in
  its flare, a platform round the trunk with a small house, a stair winding up); `waterfall` (`at` its foot, falling toward -Z,
  `size.x` wide, `lift` high: a mossy rock face, two sliding sheets, foam, mist; pair it with a pool, whose shore carving leaves the
  ledge behind the fall alone); `mushrooms` (`size.x` radius; variant 0 red caps, 1 glowing teal, 2 pale; giant from scale 2, then
  solid); `fallen_giant` (a giant lying where it fell, `size.x` long: root-plate at -x, broken crown at +x); `monolith` (a Silent
  Statue); a `bridge` with `variant` 1 is a rope-and-post footbridge. The grove kind `giant` mixes giants among broadleaf.
- **The verdant biome:** deep painted greens and moss, lush tall grass, giant trees among the forest edge (no pines), the
  `salt` shape paint (a cracked grey-white crust, no grass, no edge trees) and a saturated emerald post grade.

## The painted style pass (2026-10-01; `ZoneSceneBuilder.Painted*.cs`, `Crulanda/PaintedRock`)
Chris: "keep going with the painted style pass". The older zones take the hand-painted look the Verdant Shore set; each keeps its
mood. How it was made: a first draft was pre-checked by agents who ported the generated textures to Python and looked at them
(the draft did not tile and did not read as painted), then restaged as patch scripts verified on a scratch copy
(`tools/wip/painted/v2`, parts p1 to p5), applied one part at a time, each toured and looked at.
- **Painted textures** (generated once by the editor like the rest of the art; PNG names end in a number because a generated PNG
  is written once and then reused): plaster in broad brush patches with elongated strokes and a few stains; thatch in seven ragged
  layers a tile; slate in staggered shingles of uneven value with a lit edge and moss; coursed stone (`masonry`, its own material,
  so natural rock is untouched) in five uneven courses with rounded blocks; timber in planks with grain and knots; bedded rock.
  All seamless (`Tiled` blends the noise across the tile's edges; any stretch or offset goes inside the sample).
- **Metre UVs:** walls, plinths and chimneys are `ZoneMeshes.Box` (through `ZoneBuilder.BoxPart`), 2 m a tile with the texture's
  origin at the building's, so plaster runs unbroken across an inn's wall pieces. Roofs tile 2.5 m, v running the true distance
  up the slope (`GableRoof`; |z| + y hardly changes up a 45 degree roof, which had smeared one row over the whole slope).
- **Building trim** (`Eaves`, `Window`, `PlankDoor`): fascia boards and rafter ends under the eaves, a ridge cap, framed windows
  with sills and shutters (dropped where a corner post or the door is in the way), plank doors with iron bands and a ring,
  masonry plinths, chimneys with caps.
- **Natural rock** (`RockTint`, `art.rock`, shader `Crulanda/PaintedRock`): the bedded-rock paint projected in world space on three
  axes, so strata run level across a cliff of separate lumps; upward faces take a top light (`_Top`, set per biome: warm in the
  meadow, near neutral on ash and in gloom), undersides sink into shade. On cliffs, crags, boulders, edge rocks, the cave knoll,
  secret stones, the waterfall and the backdrop tors (their own clone, 22 m a tile). Cave interiors stay on plain stone.
- **Sky, light and ground** (zone JSON `lighting`, `ZonePost` grades, the meadow ground palette): Oakhaven a blue sky, a greener
  meadow and bluer distance; the Peaks crisp alpine blue with a warm sun; Khaven keeps its dusk with the warm sun split from the
  violet shade; the Ash Rim keeps its grey with deeper shadow and a little more distance.
- **Built stone** (part 5; `Dressed`, `Ashlar`, `Stonework`, `ZoneMeshes.Spire`): towers as one turned masonry drum with a plinth
  course and a corbel ring under the battlements, slate spire roofs, arrow slits; curtain walls, the gate and the keep as one
  joined masonry mesh each with coping and merlons; crypts, ruins, wayshrines and the Cracked Hearth's chimney in coursed stone;
  headstones, waystones and altars as painted rock (five courses on a headstone read as a toy pillar).
- **Chunky props** (part 4; `PropMesh`, `Turned`, `Cutout`, `Stake`, `Barrel`, `Crate`, `Wheel`, `Signpost`): hewn capped fence
  posts with lapped rails that follow each post's lean; carts with spoked wheels, side boards and shafts; lamp posts on a masonry
  footing with an iron bracket and a roofed lantern; the well as a turned masonry ring with a roofed windlass, rope and bucket;
  stalls with a scalloped valance, thicker posts and turned baskets and pots; turned barrels with hoops; battened crates; a
  woodpile with pale cut ends and a sawhorse; stone bridges with coping and corner piers; leaning signposts.
- **The rules every part kept:** the zone's random stream is untouched (each prop method makes the draws it always made, in the
  same order), every collider, navmesh blocker, workplace, light and registered door or usable prop is where it was, and the
  tests ran green after each part.
- **The art is in the repo now:** `tools/validation/build_art.ps1` builds new generated art in the validation copy and brings it
  home (before, each run's mirror deleted it and the register step made it again with new GUIDs).
- **Buildings on slopes** (visual review item 10; `ZoneBuilder.Buildings.cs`: `Footing`, `DoorSteps`): houses (and the mill),
  barns, the inns and fallen houses stand on a stepped masonry footing instead of one stone box under them. It is laid in runs
  of about 1.4 m round the walls, each reaching a quarter metre into the ground under it. Where the ground rises against a wall
  the run climbs in whole .3 m courses, so the stone steps up the hillside and the grass never cuts the wall. Where a run
  stands well out of the ground on the downhill side, its foot steps out a course or two (a terraced base) and every other
  tall run has a dark cellar vent under a lintel, so there are no blank stone wedges. A darker coping caps every run. The
  ground itself is not levelled (the height function is untouched, so no tree, rock, grass or creek moves). A house's or
  barn's door stands on a stone threshold at the highest ground across its doorway (the barn's doors reach the ground on
  the downhill side), with up to five steps out from it where the ground falls away in front. A fallen house keeps its
  floor slab inside the footing, so no slab overhangs a slope.
- **Hero buildings** (visual review item 11): the inns (`InnFront`; the Golden Cask and the Cracked Hearth) keep their walls,
  colliders, door and taproom, and gain:
  - timber framing on every face (studs between the windows, sill rails, a head plate);
  - the upper front jettied .35 m out on joist ends and brackets, with four larger mullioned windows;
  - a gabled porch on two solid posts (each cuts its own small hole in the navmesh) under a solid roof (no rain falls under
    it), with the lantern hung from its ceiling;
  - a sign twice the size, chained to a crossbar, with the inn's device: a gilded cask on green, or a black pot over a
    glowing crack;
  - window boxes (dry stalks in Khaven's gloom), a bench, barrels and two dormers.
  The kitchen lean-to's stretch of the back wall stays clear. Vell's smithy (`Forge`) is a hearth house under a slate gable
  (metre UVs, eaves, boarded gables with king posts) on a stone end wall and a plank half wall, with a slate lean-to over the
  anvil. It has a heap of glowing coals on the hearth, a tapered hood and a chimney stack through the roof, leather bellows,
  a horned anvil with a hammer and tongs, a stone quench trough and a tool rail. Outside stand a rack of finished work
  (horseshoes, sickles, axe heads), a barrel of bar iron and a grindstone. Its hearth, back wall and anvil colliders and both
  workplaces are where they were; the end walls and the trough are solid too, and no grass grows on its floor. The capture
  tour shoots `99-inn-front`, `99-inn-front-night` and `99-smithy-front` at eye level (`BuildingGroundTests` checks the footings, thresholds, the
  smithy's places and the way into the inn). Buildings on slopes and the hero buildings are compile-checked only: not yet run
  in PlayMode, not toured and not looked at. Unlike the earlier parts they add solid colliders (the porch posts and roof,
  the smithy's end walls and the quench trough).

## Level ladder and camps (GAME-ONLY)
- Zones carry `levelMin`/`levelMax`, which drive con colours, the map exit labels ("Khaven Village 3-5") and the world map pins.
- `camps` (`ZoneCamp`) fields:
  - `name`, `mob` (display name), `tag` (the loot table and default look) and `look`;
  - `center`, `radius` and `count`;
  - `levelMin`/`levelMax`, `elite`, `ambush` and `respawn` (seconds, default 75). Camp mobs have ids `mob.<tag>.<zone>.<camp>.<n>`, respawn after dying, and are not saved.
- Mob stats scale by level (`EncounterEnemy.MobHealth` / `MobHit`), with separate multipliers for beasts, story enemies and elites.
- **Ambush camps** hide their mobs lowered into tall grass: no nameplate, no map dot, and they can't be targeted.
  They pounce when you come within 8 m, or 3 m while sneaking (hold Ctrl).
- Mobs that can't reach you for 4 s give up the chase and reset.

| Zone | Camps (level) |
|---|---|
| Oakhaven 1-2 | Harrow wood wolves (1-2), South copse boars (1-2), North pines wolves (2), Tall-grass stalkers (1-2, ambush), Brookside boars (2), Mastwood boars (1-2), Whitefoot's pack (2), Withy pool boars (2), Hollin farm wolves (2), Old Whitefoot (3, elite, harder); the Crowsfoot Hollow dungeon, all `harder`: Sandthrone deserters (3-5), Quartermaster Hesk (4, elite), Caddock, the Bandit King (5, elite) |
| Khaven 3-5 | Whispering Wood wolves (3-4), Carrion boars (3-4, ambush), Sandthrone outrider camp (4-5), Gloom Creek hollows (5), The Grey Sexton (5, elite), Hush wolves (4-5), Plague pit hollows (4-5), Sandthrone picket (5), Mire boars (4-5), The Pale Reckoner (7, elite, harder) |
| Peaks 6-8 | Toll-gate guards (6-7), Wolf pines pack (6-7, ambush), Rockhide wallow (7), The High Ledge (8), Captain's eyrie (8, elite), Signal-tower pickets (6-7), Shieling wolves (6-7), Scar rockhides (7-8), Old Scree-Tusk (8, elite) |
| Ash Rim 9-10 | Ash hound pack (9), Unwoven Flats eaters (9-10), Tear-marked shrine (9-10), Cinderfold hollows (10), The Weave-Eater brood (10, elite), Ash-pit cultists (9-10), The Ash-Deacon (10, elite), Fraying eaters (9-10), Orchard hounds (9-10) |
| Verdant Shore 11-13 | Mistmere reed-boars (11, ambush), Antler Meadow stags (11-12), Fallen Ghost-Oak spiders (11-12), The Briar Way (12-13), The Greying (12-13), Greyheart (13, elite), Palemist mist-walkers (12-13), Palemist shadows (13), Old Ninebranch (13, elite) |

## Water (`Scripts/World/ZoneWater.cs`)
One model answers all of these, so they always agree: what is drawn (meshes), what is carved (terrain) and what is felt (motor, navmesh).
- **Creeks** (the zone's `water` paths):
  - `depth` is the channel depth below the bank (default 1.1 m). The water sits `BankDrop` (0.42 m) below the bank, so
    the default creek is 0.68 m deep for wading. Swimming needs a depth of about 1.9 m.
  - Water levels run downhill from the higher end.
  - The terrain is carved into a terraced valley around the channel.
  - Meshes are drawn in chunks, with vertex alpha giving shallowness from the real depth.
  - Creeks meander gently and their width breathes. Bridges and the mill pin the line, and nearby roads and props limit the swing.
- **Lakes** (`lakes`: name, center, radius, depth):
  - A flat level, a bowl with a ramped underwater bank, and a gentle shore.
  - `radius` is the mean waterline. The shore wanders up to about a fifth in and out around it (`ZoneWater.Lake.RadiusAt`).
- The bank paint (`ZoneWater.Shore`) and the grass line follow the real waterline.
- **Feel:**
  - `ZoneBuilder.WaterAt(p, out surface, out depth)`.
  - The motor wades when the water is above the feet, and swims past 1.45 m (out below 1.25 m).
  - A damped float spring keeps the head up. Pushing against a bank climbs out, and Space jumps out of shallows.
  - Splashes are sized by the impact.
- **Navmesh:** wading water is area 3 (cost 6), so agents prefer bridges. Swim-depth water is not walkable. Bridges are seated
  above both banks and clear the water.
- **Rendering (`World/Shaders/Water.shader`):** stylized, hand-painted clear water (art direction, Chris 2026-09-29).
  - See-through: a named GrabPass (`_WaterBackground`) and the camera depth texture (turned on by ZoneBuilder) give the metres
    of water the view crosses and the depth straight down. The bed shows through, a little refracted (`_Refract`), lifted by
    light in the shallows (`_Glow`), and fades into the depth colour at `_Murk` per metre (sooner at a slant).
  - Colour by depth: `_ShallowColor` (pale teal) -> `_MidColor` (turquoise, by `_MidDepth`) -> `_DeepColor` (deep teal-blue,
    by `_DeepDepth`). Lit like the scene and dimmed a little faster than it, so dusk and night water stay dark.
  - Foam: a soft band with a slowly wobbling edge wherever the water touches something (banks, rocks, posts, legs), plus on
    still water a thinner line washing in toward the shore (`_FoamWidth`, `_FoamWaves`; `_FoamColor` alpha is its opacity).
    Only the foam is lit (`LightingWater`, lanterns too); everything else is emission.
  - Painted highlights (`_Sparkle`): soft noise blobs, sparse and faint away from the sun, crowded and bright along its (or the
    moon's) reflection. Capped at `_Glare`.
  - Sky: only a gentle tint at a slant (`_SkyTint`) from the realtime sky-only probe (`WorldClock.Reflections`, re-rendered
    every 2 s, dimmed at night). No mirror.
  - The surface writes the composite opaquely (`finalcolor`). The zone map (orthographic) shows the depth colours, taking
    depth from the vertex shallowness.
  - Zone `waterTint` turns the whole ramp into that murky colour with a dull scum for foam; `waterReflect` (0..1) sets
    `_SkyTint` and damps highlights, ripples and foam. Khaven uses #1C2624 and 0.3. Unset keeps the turquoise defaults.
  - Ripple normals scroll downstream along creeks. There is a small swell that fades toward the shore.
  - The project is in **Gamma** colour space. Keep that in mind for any lighting maths.
- While swimming, weapons and shields are slung on the back.
- **Current:** `ZoneWater.FlowAt` gives the downstream direction times a strength (1 mid-channel, 0 at the edge).
  The motor adds it: up to 0.9 m/s wading (scaled by depth) and 1.2 m/s swimming. Lakes are still.
- **Ripples** (`NatureFx.Splashes`: `At` for a splash, `Ring` for a ring alone): moving in water throws small splashes, and
  standing or treading water sends slow rings.
- Critters, crows and trees are kept out of the water.
- Tests: `WaterTests` (drawn/carved/felt agreement in every zone; bridges; wading and swimming; saving while swimming).

## World edge backdrop
`ZoneBuilder.BuildBackdrop` (built last, with its own random stream so the zone layout doesn't change):
- A skirt of ground continues past each side of the zone: `ZoneMeshes.Backdrop`, four sides with rings out to `BackdropWidth`.
  - Its inner row shares the ground's edge vertices (`GroundSegments`), so no crack can open.
  - UVs mirror the painted ground, so roads run on over the seam.
- The skirt rises into hills (`Rise`) coloured by biome, with tree and rock silhouettes. Next to the Wasting it falls away to the flat unmade. There the skirt spreads the unmade strip's own paint instead of mirroring (no meadow, roads or cracks come back), and `BuildWasting` lays haze in the fog's own colour over the unmade (`WastingStatic`): clear at the curtain, opaque before the skirt ends, with a bank of it on the horizon that thins upward into the sky. Linear fog alone left the skirt's edge half-fogged at the sides of the view, which drew a straight horizon line. The Wasting's front (`Unmade`) is ragged for paint and grass alike; its curtain is unlit static in the fog's colour, thin at the ground.
- The sky's `_GroundColor` is set to the fog colour each frame, divided by sqrt(exposure) to suit gamma space. Anything seen
  past the backdrop fades into fog, not into a brown plane.
- The camera's far plane covers the backdrop. Shadow distance is unchanged.
- It is scenery only: no navmesh, colliders or gameplay.

## Night
- Moonlit ambient floor: sky, equator and ground are roughly double the old values, the moon is at 0.26, the fog is a dark blue haze, and the sky exposure stays at 0.3.
- `Post.shader` contrast is linear above mid-grey.
  - By day there is only a small quadratic toe where the line would clip, so the day look stays crisp.
  - With darkness (`_Lift`), it blends to a power toe that lifts the darks, so shadows never go pure black.

## Nature and post-processing
- Grass (`Grass.shader`): instanced with wind sway, density 2.3 in meadows. `tallGrass` patches hide ambushers.
- Falling leaves under broadleaf trees, and falling ash in the `ash` biome (`NatureFx.cs`).
- Blob-mesh canopies, bushes and boulders (`ZoneMeshes.Blob`), and edge rocks.
- `ZonePost` + `Post.shader`: bloom (Karis prefilter), sun shafts, ACES tone mapping, and a biome grade (exposure, contrast,
  saturation, tint), plus a vignette.

## Weather (`Scripts/World/WorldWeather.cs`)
- **Data:** a zone's `weather` array lists kinds with weights, calmest first: `clear`, `fair`, `windy`, `overcast`, `mist`,
  `rain`, `flurries` (snow), `ashsquall`, `storm`. With no array, the biome's default applies (`WeatherSchedule.Defaults`).
  | Zone | Weather (weights) | About how often (share of spells) |
  |---|---|---|
  | Oakhaven | clear 2, fair 3, windy 1.5, overcast 2.5, rain 3, storm 1.2 | calm half the time; overcast 24%, rain 20%, storm 5% |
  | Khaven Village | fair 1, windy 1, overcast 3, mist 4, rain 2 (it never clears; the dusk stays) | mist 39%, overcast 29%, rain 16% |
  | The Shattered Peaks | clear 2, fair 3, windy 2.5, overcast 2, flurries 3 | flurries 14%, overcast 20% |
  | The Ashland Rim | fair 1, windy 2, overcast 3, ash squall 3 (it never rains on the ash) | ash squall 28%, overcast 43% |
  - The weights are relative, and the one-step rule reshapes them (calm kinds sit next to more neighbours), so the last column
    comes from simulating the schedule, not from the weights alone.
- **Schedule:** spells of 7 minutes (a 40-minute day has five or six).
  - Each spell is picked from the table by a seeded random (the zone's seed and the spell number): the same zone always has
    the same weather at the same weather time.
  - A spell moves one step of severity at most: clear, then fair or windy, then overcast or mist, then rain, flurries or ash
    squall, then storm.
  - A new spell turns in from the last over 50 s.
  - The weather clock is static like the hour, so it carries across zone travel. A new session starts inside a calm spell.
  - Weather is not saved.
- **What it does**, blended through `WeatherLook` (clouds, dim, rain, snow, ash, mist, wind, fog):
  - **Light:** cloud takes the sun (`WorldClock`), taking less of the moon so nights stay readable. Ambient flattens toward
    grey with a little more skylight. Shadows soften.
  - **Air:** the fog greys, cools, pales (mist), dulls (ash) or brightens (snow), and closes in (never nearer than 55 m at
    its far end). The sky's tint greys and its haze thickens. Lamps light early under heavy cloud.
  - **Grade:** `ZonePost` takes colour and contrast down a little, with no glare, sun shafts only through breaks, and a
    cool rain tint.
  - **Clouds:** `Clouds.shader` draws a painted cloud layer on a dome round the camera. Each view ray meets a flat sheet
    900 m up, so clouds stay put in the world, drift with the wind and crowd toward the horizon. Cover goes from a few
    puffs (clear) to a grey lid (overcast), with lit tops, shaded undersides and a silver rim round the sun.
  - **Cloud shadows:** the post composite finds each pixel's ground point from the depth texture and darkens it under the
    same noise, cover and drift. They are strongest under broken cloud.
  - **Wind:** the global `_WeatherWind` (direction and strength) sends gust waves through the grass (`Grass.shader`) and
    leaf crowns (`Leaf.shader`; `Fade.shader` matches it exactly), with a downwind lean. Falling leaves and ash drift with it.
  - **Rain:**
    - streaks from a box above the view;
    - splashes where drops land;
    - rings on water (`Splashes.Drop`);
    - the ground darkens and turns glossy as it soaks, then dries over about three minutes.
    - Nothing falls under a roof (`ZoneBuilder.UnderRoof`, the building footprints), and a camera indoors sees none.
  - **Storm:** rain, gales and lightning flashes every 8-26 s.
  - **Flurries:** snow through the air column. **Mist:** big soft puffs low on the ground. **Ash squall:** dun dust and a
    blizzard of ash.
- **Chat and HUD:** a turn is announced in the chat ("It starts to rain."). The minimap shows the weather under the hour
  (not when clear or fair).
- **Testing:**
  - F8 (development builds) cycles through the zone's kinds, then back to its own weather.
  - `--crulanda-weather <kind>` starts in that weather.
  - Capture tours hold fair weather for comparable shots, and add `80-weather-<kind>` shots (plus `81-...-pond` for rain).

## Caves you walk into (`Scripts/World/Hollow.cs`, `ZoneBuilder.Cavern`)
- **Crowsfoot Hollow** (GAME-ONLY) is Oakhaven's cave, at the end of the North road in the north hills: the Sandthrone
  deserters' hideout, and the game's first dungeon (Chris, 2026-09-30: "the cave should be deep and the first foray into
  dungeon crawling"). There is no loading; you walk in, and down.
- **Hidden in the hill** (Chris, 2026-10-01: "the dungeon in the first zone kind of stick out as just a rock. it needs to be
  built into a mountain or something and kinda hidden..no so obvious. they are bandits"): the mouth is a slot in a cliff face,
  not a rock on the grass. Two cliff scarps (`Crowsfoot scarp, west` and `east`, 18 m, lifted 10 m) stand either side of it and
  the Crowsfoot brow (two `shapes`, 11-11.5 m high) rises over it, so the knoll's crag lumps heap onto a hill that was already
  there. The North road ends at (-13, 72); from there the Crowsfoot track (2.2 m wide) bends through a pine thicket and round
  three boulders to the mouth, which you only see from the last bend. The landmark's view is from the track's start.
- **Data:** a `cavern` prop (`at` is the mouth, which faces -Z turned by `rotation`; `variant` picks the plan in
  `ZoneBuilder.CavernPlan`) plus a level `shapes` pad at its mouth.
  - The passage (`Hollow`) comes from the plan's rows (x, z, half-width, height, drop): x, z, width and height smoothed into
    rings about every 0.7 m, the floor's drop below the mouth's ground straight between rows (the steepest row is the
    steepest floor; none is over about 34 degrees, and feet and agents climb 45).
  - It is worked out before the ground is built, so everything else can ask: is a point inside it (`Inside`), what floor is
    under it (`FloorAt`, `ZoneBuilder.StandAt`), how deep in it is (`Depth`), and how much of the land it covers (`Cover`, only
    where it runs within 3 m of the surface; deep under the land it leaves the land above alone).
- **Build:**
  - The ground mesh leaves out every triangle with a corner inside the passage (`ZoneMeshes.Ground`'s cut); the rock shell,
    thicker than a cell, covers the hole.
  - A lofted rock shell: faceted, faces both in and out, casting shadow both ways, one mesh collider, in the navmesh.
  - Its own floor mesh from wall foot to wall foot, earth over rock, walkable (navmesh), with a lip out of the mouth onto the
    ground so the way in has no seam. The navmesh's bounds grow to reach it past the zone's edge (`Hollow.Reach`).
  - Crag lumps heap over and round it into a knoll where it runs near the surface. Each is checked to stay clear of the
    passage (tests hold it to that), and has a collider and a navmesh obstacle.
- **The way down** (plan frame: mouth at the origin; the zone is at plus (-16, 90)):
  - **The upper hollow:** the entrance passage north into the deserters' camp round a fire (bedrolls, stolen grain, crates,
    barrels, a torn Sandthrone banner), then a low passage bending west.
  - **The Drop:** a steep descent north, eleven metres down, with plank treads pegged across the floor and a rope rail on
    posts down one wall. It takes the passage north under Crowsfoot Ridge, the hills on the zone's north side (raised over
    the dungeon as shapes; 8 m and more of rock over its roof). The key to the strongbox hangs on a nail here.
  - **The Store Caves**, 12.5 m down: the stolen stores stacked along the walls, the Quartermaster's desk (a plank on two
    barrels, his ledger, a candle), a brazier, a side way choked with fallen rock, and the Quartermaster's strongbox.
  - **The Deep Stair:** a winding passage east, down another three metres.
  - **The Echoing Hall**, sixteen metres down under the northern hills, 18 m wide and 9 m high: stalagmites round its edges,
    stalactites overhead, braziers, and at the far end Caddock's plank throne on a stone dais facing the way in, his banner
    behind it, and "The deserters' plunder" (usable, for the quest).
  - Flickering torches down the walls all the way (every twelfth one in the deep left dark); below 8 m, a faint blue-green
    glow of fungus on the walls and puddles where water seeps.
  - Braziers, the desk, the crate stacks and the plunder are solid: you and the deserters walk round them.
- **Coming in:** a few metres in, the cave names itself on the banner over the levels of its camps ("LEVELS 3-5"), once each
  time you go in.
- **Light and air:**
  - With the camera's depth (0 at the mouth, 1 from about 10 m in), WorldClock cuts the skylight to a fifth, thickens the
    air to a smoky brown haze (fog 1.5-34 m), and drops sky reflections.
  - ZonePost lets the eye adjust (+30% exposure), makes firelight bloom, and adds vignette. Cloud shadows stop at the mouth.
- **Weather:** nothing falls inside: the camera anywhere in the passage is sheltered, and no leaves or ash drift down through
  the rock. `UnderRoof` counts the passage where it runs near the surface.
- **Scatter:** no grass grows on the floor. Trees, bushes and edge rocks that land in it are built (their draws taken) and
  dropped, so the zone's layout elsewhere is unchanged.
- **Standing in it:** camps, their respawns, secrets, and a save made down there all stand on the passage floor, not on the
  land above (`ZoneBuilder.StandAt`). Nothing is used (E) through the rock: a prop or find inside can't be reached from the
  hill over it, nor one outside from within.
- **Camps** (all `harder`: up to three levels over Oakhaven's band):
  - Hollow lookouts: 2 on the road at the mouth, level 3.
  - Deserters' camp: 4 at the fire, levels 3-4.
  - Drop sentries: 2 on the Drop, levels 3-4.
  - Store Caves: 2, level 4, with **Quartermaster Hesk** (elite, level 4) at his desk.
  - Deep Stair watch: 2, levels 4-5.
  - King's guard: 3 in the hall, levels 4-5, and **Caddock, the Bandit King** (elite, level 5) before his throne.
  - They come back slowly, as a dungeon's should: the lookouts in 5 minutes, the rest in 8 to 15.
- **Loot:** deserters drop filed company badges; Hesk his stores; Caddock always drops **Caddock's Tin Crown** (head). The
  Quartermaster's strongbox (a secret chest; its key is on the Drop) holds company silver, Hesk's Shuttered Lantern
  (off-hand) and the King's orders (a Chronicle page).

### The Root-Mother's Deep (the Verdant Shore; GAME-ONLY, under a CANON Temple)
- A `cavern` with `variant: 1` (`ZoneBuilder.RootDeepPlan`, `RootDeepInterior`): the same `Hollow` passage model, but grown through
  earth and root instead of cut through rock. Its mouth is the Veridian Temple's root-stair at (-112, 130), facing south into the
  Temple's sunk hollow; the passage runs 100 m north under the Emerald Cathedral and the Temple's barrow (a `shapes` mound),
  sixteen metres down. No crag lumps show on the land over it (a root deep shows nothing); the walls are bark-tinted and the
  floor loam.
- **The way down** (plan frame: the mouth at the origin, north is +z): the **root-stair** down into the **Root Gallery** (a hall
  of living root columns, floor to roof; solid); a root-choked passage east; the **Sap Well**, a chamber with a pool of emerald
  sap in a rim of pale root against its east wall (the way through stays open along the west), drips of sap from the roof and a
  Keeper's votive stones round it; the **Cold Stair** west and down; and the **Heart**, 18 m wide, where the Root-Mother is a
  vast knot of root grown out of the back wall with a hollow where a face would be and sap-light in it, fourteen root limbs off
  her into floor and roof, and before her, lodged in a root gone pale, **the cold in the root**: a black many-faced rod,
  hoarfrost spreading from it and a violet light (usable: "Salt the cold root", for the finale quest). Bones of what came down
  before, and a Silent Pilgrim's abandoned pack (a secret, by the sap-pool).
- **Light:** no torches. Veins of glowing sap on the walls every six metres or so with a dim green light at every third, fungus
  round them and drips of sap on the floor; the Sap Well's pool and the Root-Mother's eyes are the bright places. Dark enough
  that the Heart is a fifth of the daylight.
- **Camps** (all `harder`): Root-stair briars (2, 12-13), the Gallery's withered Keepers (3, 12-13), the Sap Well's mist-walkers
  (3, 13), Cold Stair briars (2, 13), the Heart's withered (2, 13) and **the Hollow Root-Warden** (elite, 13; always drops the
  Root-Warden's Crown, head). Respawns 8-15 minutes. The deep's Keepers and walkers carry their own tags (`deepwithered`,
  `deepwalker`, with the surface camps' loot), so the finale counts only kills made down here.
- **The Heart's roof** stays 8 m high to its back wall (the plan's rows at z 95 and 98), then closes at once: the Root-Mother
  stands seven metres tall against it, face and all under the roof.
- **Salting the cold** is once and for good (saved): the rod, its violet light and the hoarfrost go; the pale root stays.
- **A place down a cave is visited from down in the cave** (`EncounterSession.OnItsLevel`): where a passage floor lies under a
  quest's `visit` point, the visitor must stand within four metres of that floor's height, so the Root Gallery is not "reached"
  from the barrow over it. At a cave's mouth the two heights are the same.
- Tests: `RootDeepTests` (walkable leg by leg from the zone's start to the Heart; earth and root overhead and underfoot the
  whole way; dark inside, the camps on the floor, the Warden in the Heart, the cold root usable and the quest pointing at it).

## Secrets (hidden finds; `ZoneBuilder.BuildSecrets`)
- **Data:** a zone's `secrets` (`ZoneSecret`, see ZoneDefinition.cs). Found and paid out by the encounter's DiscoveryLog: a lookout
  (`vista`) by standing within `radius`, anything else by searching with E within 2.8 m (ground distance).
- **Built after the map is rendered**, so none shows on the minimap or the zone map. No colliders and nothing in the navmesh.
  Each prop draws from its own stream keyed on where it stands, so the zone's layout is unchanged.
- **Looks** (small and subtle: metal glints, pages are pale, herbs glow faintly; never a marker's glow):
  - cache: a dented tin, or an oilcloth bundle when the prompt or name says bundle, oilcloth, sack or pack (salt-crusted with
    red cord when it says salt);
  - note: a folded page standing out of a crack, or lying under a stone; herb: a patch of pale bells (colour by biome);
  - chest: an iron-banded chest with a padlock; key: a brass key on a thong from a nail;
  - vista: nothing, or a cairn ("stones"/"cairn"), a beacon ring ("beacon") or a salt line turned by `rotation` ("salt").
- **Scenes:** a word in a searchable's prompt or name dresses the spot: "camp" (cold fire ring and bedroll), "log"/"trunk",
  "stump" (a hollow stump), "rocks"/"cleft" (two flanking stones), "stone" (a flat stone half over it), "scrape".
  - The frame faces -Z toward you; the hiding place is behind (+Z). `height` lifts the find (a key on a post, a page in a wall).
  - Notes, keys and herbs vanish when found, so `ZoneSecretSpot.root` holds only the thing; its scene stays.
  - A herb, or a find with a scene of its own, standing in a trunk or rock moves clear (up to 2.5 m). A find tucked against a prop
    stays put.
- **Placement rules** (tested by `SecretPlacementTests`):
  - reachable on the navmesh from the player start (a searchable within reach of E);
  - not in a building, water, a camp, on a road, on a quest prop, at an exit, spawn or arrival;
  - keep 4 m or more off roads and bridge decks so walking past never raises the prompt;
  - keep 5 m or more from the foot of free crags (their rock lumps and scree reach past the colliders);
  - nothing in or on Crowsfoot Hollow but its own strongbox and key, and nothing on the surface within 12 m of its passage
    (a camp or find over it would be seated on its floor).
- **Per zone** (GAME-ONLY placement; canonStatus on each):
  - Oakhaven: the Road's End (vista, now where the ruts run into the grey), the Poacher's Cold Camp, a Salt-Mender's Drop behind
    the old wayshrine, a Leaf from the Chapel Book (hollow stump), Moonbells; in the dungeon a Key on the Drop and the
    Quartermaster's Strongbox; in the outer ring the Overlook (vista on Crowsfoot Ridge), the Shepherd's Tin, a Grave-Robber's
    Bundle, a Letter from Hallow's Creek and a Page from the Watch Book.
  - Khaven: the Old Beacon (vista, on a new `Beacon mound` shape east of the walls), a Key Under the Gallows, the Outrider's Hoard
    (chest; needs the key), a Page from the Sexton's Register (drowned chapel wall), Widow's-Lamps; in the outer ring Where the
    Crows Wait (vista), the Listener's Daybook and Coins for the Hanged.
  - Peaks: Above the East Road (vista on the High Ledge), the Goat-Path Camp, a Silent Pilgrim's Echo-Jar, a Letter Never Sent,
    Frostbells; in the outer ring the Picket's Skim, Scored in the Mortar (the Sealed Adit) and the Spur's End (vista).
  - Ashland Rim: the Salt Line (vista in the southern gap of the Eastern Ridge), an Ash-Walker Cache, a Cultist's Hidden Letter,
    Last-Light, the Cinderfold Tin; in the outer ring a Page from the Waybook, a Bone-Carver's Bundle and Last-Light in the Grey.
  - Verdant Shore: Above Mossveil Falls and the Mossy Outcrop (lookouts), the Tapper's Stump, a Glass-Ship's Log, Lantern-Moss.
- The capture tour shoots one find per zone up close (`87-secret-<slug>`; the first chest, cache, herb, note or key).

## Things to gather (`ZoneBuilder.BuildNodes`, `ZoneBuilder.Nodes.cs`; GAME-ONLY)
- **Data:** a zone's `nodes` (`ZoneNode`) and herb props with a `node` (see DATA_SCHEMA.md); the kinds of node (name, prompt,
  look, tier) are the trades' content (`Professions/professions.json`).
- **Built after the secrets**, each from its own stream keyed on where it stands, with no colliders and nothing in the navmesh:
  every tree, prop, secret and creek point stands where it stood (`NodeStreamTests`).
- **Looks** (in the painted style, from the crags' and boulders' own meshes and stone):
  - ore seam: an outcrop of the crag stone a metre high, leaning back, one shoulder stained the ore's colour; on its face veins of
    ore, crystal shards and flecks, and broken ore at its foot (tier colours: copper red-brown with verdigris, bog-iron rust,
    Adit iron dark with a glint, cinder ember-orange and glowing, Veridian blue-green and glowing). The ore is matte (the metal
    material with no metalness), so it keeps its colour in a cave and at night, where reflections fade. Rich seams are a third
    larger. Mined out, the ore goes and the rock stays. In a cave the rock turns to the nearer wall and stands on the floor as
    its mesh runs between the rings (`Hollow.FloorSmooth`), so nothing floats on a slope.
  - windfall: a snapped stump with a splintered top (it stays) and the trunk lying beside it with stub limbs (cut away while it
    rests); bark by tier (oak, black pine, stone-pine, the ash's charred snag, pale ghost-oak).
  - herb: the herb patches (`Herb`), with the trades' tarnwort, cinder-thistle and dewfern as variants 3-5.
- **Placement** (moved clear at build, up to 3 m, and tested by `NodePlacementTests`): reachable from the player start to within
  reach of E; out of water, buildings and off roads; 5 m from secrets and from each other, 2 m from trunks; rich seams only on a
  cave floor. Seams by crags and rocks or on a ridge's crown, windfalls at a wood's edge (Oakhaven's oak at broadleaf woods),
  herbs in the open (a herb prop worked as a node stays where the zone had it). Every zone has ten ore, eight windfalls and ten
  herbs at its tier (`ProfessionDataTests.EveryZone_HasTenOreEightTimberTenHerbNodes`). A node's footprint
  (`ZoneBuilder.NodeFootprint`: a seam 2.1 m round, a rich one 2.8 m, a windfall 3 m, a herb 0.6 m) stays out of every camp's
  spread (its radius out to the square's corners, x 1.42) and off a cave's furnishings, which have no colliders, so `Cavern`
  marks each one it places (`ZoneBuilder.KeepClearSpots`: the camp's fire, bedrolls, grain, crates and barrels, the Drop's
  treads and rail posts, the Store Caves' stacks, desk, brazier and fallen rock, the throne, braziers, banner and plunder).
- **Working one** (E; the rules are in DATA_SCHEMA.md, "Trades"): the work bar is the cast bar; moving, a blow, a fight or dying
  stops it. A worked node rests (`respawn`), also across a zone hop; a restart refills it.
- The world capture tour shoots every node from a few metres off its face (`<zone>-80-node-NN-<name>`).
- **Placing more:** `tools/wip/professions/place_nodes.py` checks candidate nodes for Khaven, the Peaks, the Rim and the Verdant
  Shore against the zone data by these rules with a margin, over a model of the ground (the height function ported, the walk
  from the player's start), and writes them (`--write`); `--probe <zone> <x> <y>` lists clear spots near a point. It cannot
  see the random grove trees or the strewn boulders, so the PlayMode tests stay the judge.

## Travel
- Zones list `exits` (to, name, at, arrive, radius). Standing at an exit shows "[E] <name>"; E travels (not in combat):
  the character saves with the new zone and arrival point, and the scene rebuilds the other zone. A recruited Mira comes along.
- One scene (`Oakhaven.unity`) builds any registered zone (`ZoneBuilder.zones`, filled by the editor menu item); on load,
  a character saved in another zone is rebuilt there. `--crulanda-zone <id>` picks a zone for capture tools.
- Roads: Oakhaven west ↔ Khaven, Khaven north ↔ Peaks, Peaks east ↔ Ash Rim, and Ash Rim north ↔ Oakhaven south (marked dangerous, 9-10).
  Minimap and zone-map exits show the destination's level band. The world map draws the roads. Grove areas (`groves`: dead / pine / broadleaf) fill forests and darken
  the ground beneath them. New prop kinds: ruined_house, wall (polyline), tower, gallows, crypt, cliff, wayshrine (a wayside shrine); Peaks and Ash Rim landmarks: gate, keep, perch, wallow, cave, shelter, brazier, brood, rib, spine, shrine (the Cult of Ash's), idol; well variant 1 = blood-stone, inn 1 = the Cracked Hearth (split, glowing chimney), crypt 1 = a turfed barrow, grave 1 = heaved over and half sunk.

## Life, day and night (GAME-ONLY)
- `life` in the zone JSON: villager count, mood (`wary` / `afraid`) and critter groups (chicken, rabbit, crow, deer,
  sheep, cat). `WorldLife.cs`: villagers have roles (farmer, gossip, drinker, child, elder, miller, henwife), homes
  (their household's door, below), places from the zone (fields, well, green, inn seats, mill), chat in pairs, bark at the player,
  and flee home when fighting starts nearby. Critters never fight and scatter from the player; crows fly.
- **Households** (GAME-ONLY; `life.households`, `ZoneHousehold`; tools/wip/professions/ADDENDUM.md A): each household names
  its house (a prop of kind house, mill, barn or inn) and its members with their kin ("head", "husband", "daughter",
  "lodger"); families share one house, and no two households share one. A barn lived in gets a barred door of its own
  (`ZoneDoor.kind` "home", `ZoneBuilder.BuildHomeDoors`, no random draws); an inn named as a house means its rooms door. Every
  villager, hen-wife and working resident goes home to their household's door (`Villager.Home`, `Villager.Household`,
  `VillageLife.Households`, `HouseholdOf`); a resident who keeps a post can be named in a household (so a knock names them)
  and still keeps the post day and night. `stipend` (default 6) and `needs` (default bread, firewood, eggs) are read by the
  purses, a later step.
  - Oakhaven, 15 households: Vell, Thorne, **Tanner** (Maud Tanner, leatherworker; Fen Walker, skinner, her husband; Nettie,
    their daughter), Rusk, Reed, Farrow (Osk and his son Pim), Crane, Ashby (the Elder's house), Pell (Old Tobin and his wife
    Edda), Jory (alone, Jory's house), Harrow farm (Sel Harrow, Ilse Brandt the hand, Goody Marl their aunt), **Carder farm**
    (Wil Carder and Hettie Brook, his aunt, in the new Carder farmhouse), Brook farm (Grete Lowe, Nan Pennock lodging), **Crisp**
    (Aldo Crisp in the new Crisp cottage; nobody sleeps at the mill) and **Moss** (Garet Moss at his lodge, a barn with a home
    door). Quill and Warden Ivel keep their posts. The Golden Cask's household comes with its innkeeper.
  - Khaven, 5: Grane, Vey (Dorra Vey and Old Kestrel, her father), Jenn (Mattock Jenn and Siv Harl, his wife), Tabor, and
    Morrow at the Crypt-Keeper's Hovel (Ansel Morrow keeps his post at the crypt; the hovel stands empty).
  - A zone with villagers and no `households` uses the derived rule: villager i takes house i (the barred doors in the order
    they were built) while houses last, the rest lodge behind the inn's rooms door, and a hen-wife joins the household of the
    house nearest her coop. Nobody is given a door by modulo any more. A household whose house is not built (a missing prop,
    or a test's filtered zone) lodges behind the inn's rooms door, and the log says so.
  - **Knocking** (`EncounterSession.Knock`): someone at home with wares or quest business for you (a gold `!` or `?`) opens
    up and deals with you at the door ("A shutter opens. "At this hour? Go on, then.""). Otherwise the household answers
    (`VillageLife.KnockLine`): all abed ("The Tanners are abed. A child coughs, and somebody hushes her."), the head out and
    someone in ("Maud's at the shop by the South road. Try there."), the head in ("Not now. Since the collectors came, this door
    stays barred."), or nobody home ("No answer. The Tanner house is empty till supper."). A farm's folk go by the head's family
    name (the Brook farm's are "The Lowes"), and a house named for somebody outside the household takes the article ("No
    answer. The Crypt-Keeper's Hovel stands empty; Ansel keeps watch elsewhere, day and night."). A door no household lives behind
    gives the old barred-door lines; the inn's rooms door its own line.
  - Tests: `HouseholdDataTests` (EditMode: every villager in exactly one household, houses are props and not shared, Khaven's
    six, zones without households parse) and `VillageHomeTests` (PlayMode: everyone walks home to their own door by 23:30, the
    Tanners share a house, farmers live with their hen-wife, the hunter makes it from the inn to his lodge in time, every home
    door is on the navmesh, knocking names the household).
- **Day/night** (`Scripts/World/WorldClock.cs`): one game day lasts 40 real minutes and starts at 08:30. The hour
  continues across zone travel (it is not saved yet). The directional light is the sun by day and a pale moon at night.
  Dawn, day, dusk and night keyframes blend with the zone's own `lighting` palette: ambient, fog, sky tint and exposure.
  Lamps (`NightLights`) light up after dark. The minimap rim shows a sun or moon and the time. In development builds,
  F11 skips an hour.
- **Night routine:** villagers go home to bed from about 20:00, children first. A hunter who lives at a lodge (Oakhaven's
  Garet Moss) sets off at 19:48 and spends his evening in the lodge yard (his lodge is about a game hour's walk from the
  green); in a village without a lodge (Khaven) the hunter keeps the inn and the green and goes to bed with the rest
  (`VillageWork.PlacesFor`). Drinkers stay at the inn until 23:00 or later. Everyone wakes between about 05:50 and 07:30.
- **Hen coops** (prop kind `coop`, registered in `ZoneBuilder.Coops`) are raised huts with a ramp, a pop-hole door on a
  hinge, nest boxes and a feed trough. Each coop gets a hen-wife (Oakhaven: Goody Marl at Harrow, Hettie Brook at the West
  field coop by the Carder farm, Nan Pennock at Brook farm), and chickens belong to the nearest coop. Her day (Chris, 2026-10-01: "the hen
  maiden/mother should feed the chickens in the morning. water during the day. collect eggs ... she should take some eggs to
  the merchants to sell. some home to eat. some eggs to the inn for food for the village"):
  - 05:45: opens the coop, and the hens come down the ramp one by one from 06:00; then the morning feed at the trough, and
    every hen in the yard comes running.
  - 08:24: collects the eggs from the nest boxes (hens lay up to one each through the day) and carries the basket, eggs
    showing in it, to the inn's kitchen.
  - 09:36 and 13:30: draws water at the well and carries the bucket back to fill the water pan by the ramp; the hens come to
    drink, and the water sinks as the day dries it (about four hours).
  - 12:12-13:30: dinner at home.
  - 14:36: the afternoon feed.
  - 15:24: the next eggs go to the produce stall, where the merchant sells them on ("Oakhaven eggs", food, while they last).
  - 17:12: the last eggs go home for the pot.
  - From 18:36: hens head in on their own; from 19:18: she herds the stragglers (walks round behind the farthest one), shuts
    the door once all are in, and goes to bed.
  - Night: hens roost inside and are hidden.
- **Daily schedules and errands** (`Scripts/Encounter/VillageWork.cs`; Chris, 2026-10-01: "each profession should have a daily
  ai job schedule with tasks ... all jobs are intertwined"):
  - Every trade has a `WorkDay`: shifts (from-to hours, the kinds of place to work at, a repeat weighting the choice) and
    errands (once a day from an hour, dropped if not started by a later one: walk to a place, pick up a load, carry it to
    another, hand it over and say so). `Villager.ChooseNext` picks the shift's places; a due errand comes first. Off the
    schedule (a role without a day) they potter as before.
  - The loads are built from primitives on the arm or shoulder (`LoadProps`): a basket of eggs (as many as were laid), a bucket,
    sacks of grain and flour, a tray of loaves, logs, a hide, a bundle of herbs, a hare on a string, a crate of goods.
  - What is delivered where goes into the village's stock (`VillageLife.Stock`, keyed "place.good": inn.eggs, mill.grain,
    oven.flour, forge.wood...). Whoever is working at the place answers ("Ta, Goody. Warm, are they?"); the trades talk about
    each other's goods ("The miller's flour came in. Thin stuff, but it rises."); and the merchant's wares gain the eggs.
  - The chain: the farmer's barley to the mill (10:00) and seed-corn home; the miller's flour to the bakehouse (11:00) and the
    stall; the baker (up at 04:36) takes the first loaves to the inn (07:00) and a second batch to the stall; the woodcutter's logs
    to the woodyard, then firewood to the inn and the forge; the smith fetches oak for the forge and takes ironwork to the
    stall; the hunter's hide to the tannery and his hares to the inn's pot; the skinner's pelts from the snares; the
    leatherworker's belts to the stall; the herbalist's herbs to the stall, marigold for Mira at the inn, and herbs home to dry;
    the merchant shutters up at 17:48 and carries what didn't sell home; gossips and children fetch water from the well
    (and a child fetches a loaf for Mum); the elders sit on the green and at the inn; drinkers drink.
  - A trade whose errand needs a place the village lacks (no mill, no tannery) skips it for the day. Deliveries go to the
    place where someone of that trade is working now (the stall with the merchant behind it), else any of that kind.
  - **The out of work drink** (Chris, 2026-10-01: "have unemployed npc show up at the inn and drink till gone or passed out"):
    the drinkers, and anyone whose trade this village has no place for (a smith with no forge, a farmer with no fields: in a
    village with an inn they become drinkers), loiter the morning away and are at the inn from 11:00. Each round is a tankard off
    the day's cask (`inn.ale` in the village's stock: ten left from yesterday each dawn, twelve more when the merchant brings "a
    cask for the inn" at 08:36), drunk sitting with the tankard in hand (`ActorPose.Drink`), and leaves them a little further gone;
    their talk slurs with it. When the cask is dry they grumble and call it a day. Past their limit (it differs by person) some
    fold over the table and snore for three to five hours (`ActorPose.Slump`; "Zzz..." is all you get from them), the rest say
    goodnight and reel home (`ActorVisual.Stagger`); spent, they stay indoors until morning. `VillageDrinkTests`.
  - Tests: `VillageWorkTests` (every trade's day covers its hours; every errand runs between known places in a window and
    carries a good; the hen-wife's seven) and `VillageErrandTests` (eggs reach the inn in a basket you can see and the merchant
    sells them on; water fills the pan and the hens come; the farmer's barley reaches the mill).
- **Trades** (GAME-ONLY):
  - Villagers take trades in this order: blacksmith, farmer, merchant, baker, gossip, hunter, drinker, child,
    woodcutter, herbalist, farmer, elder, miller, leatherworker, skinner, and so on.
  - A trade whose workplace the zone lacks falls back to farmer or gossip.
  - Each trade has its own outfit and tool (`ActorVisual.Dress`) and a `<Title>` under its nameplate.
  - Workplaces are new prop kinds registered in `ZoneBuilder.Workplaces` (a stand point, a look target and the prop's name):
    `forge` (smithy: a hearth house and a lean-to, a lit hearth, anvil, quench trough and grindstone), `stall` (awning and goods; variant 0 produce, 1 cloth and
    pots, 2 bread), `oven` (clay bake oven), `tannery` (hide frames, vat, scraping beam) and `woodpile`.
  - The trades' workshops (`ZoneBuilder.Workshops.cs`, GAME-ONLY; each draws from its own stream, carries no `size`, and is in
    `Footprint` (no rain) and the grass's bare list):
    - `leathershop`, 6 x 5 m: plank walls on a stone sill under a thatched gable, the front open; a counter under a hide
      pentice with the four trade bags hung over it and a lantern; a rail of belts and a bridle, a cutting bench with a hide on
      it, a stitching horse, rolled and stacked hides; a satchel on the hanging sign. Places: the counter (weighted twice), the
      bench, the stitching.
    - `dryhut`, 5 x 4.5 m: wattle in a timber frame on a stone footing, steep thatch, half the front open; bunches of herbs
      under the eave and from a pole inside; two drying racks hung with herbs out front; a bench with a mortar, jars and a small
      still over a pan of coals (a glow). Places: the bench, the racks.
    - `kitchen`, 5 x 3 m: a lean-to whose back edge meets an inn's back wall (`KitchenBehind`; the inn then leaves out the two
      ground-floor windows it covers and shows the kitchen's door in the taproom): a slate roof on a beam and post, a stone end
      wall with the range against it (fire mouth, plate, pot on a crane, a hood and a smoking chimney), firewood, a work table
      under a shelf of crocks, the back door, two hares from the beam, onions and a water butt. Places: `kitchen` (range,
      table) and three `kitchendoor` hand-over spots out front.
    - `gamerack`, 3 x 2 m on a hillside: forked poles and a crossbar hung with a deer, two hares and a brace of pheasants; a
      rail of pelts; a hide laced in a frame; a butcher's block with a cleaver; a ring of cold stones. Place: `lodge`.
    - The inn adds a `bar` place at the open end of the bar and a barred door to the rooms upstairs (`ZoneDoor.kind` "rooms";
      nobody in Oakhaven lodges there yet; trying it says the stair is kept for the inn's lodgers). `ZoneDoor.smoke` is the
      house's chimney smoke.
    - The Carder farmhouse and the Crisp cottage are lived in (see Households).
    - A villager at stand points close together (the kitchen's range and table) works and looks at the nearest one.
    - A village with a tannery yard and no leather shop has its leatherworker work the yard (`leathershop` shares the
      `tannery` places).
  - The leatherworker keeps her shop 9 to 12 and 2 to 6 (the yard either side) and takes her wares from the shop to the stall
    after her midday meal (12.2 to 14); the herbalist calls at her drying hut late morning and evening. `VillageWorkshopTests`: every workshop place is on the navmesh and can be walked to, and Maud keeps
    shop apart from the tannery yard.
  - Hunters and woodcutters work the groves. Herbalists gather on the open meadow.
  - Work poses: Hammer, Chop, Gather, Knead. Each trade has its own lines of talk.
  - Oakhaven has Vell's smithy, Market row (three stalls), Thorne's bakehouse, the Tannery yard, the Woodyard, Tanner's leather
    shop, Lisbet's drying hut, the Cask's kitchen and Moss's game rack, with 20 villagers.
  - The capture tour shoots each trade at work, plus a line-up of every trade on the green.
- Tests: `Tests/PlayMode/VillageDayTests.cs` cover three cases: night (coops shut, hens roosting, village abed), morning
  (coops open, feed scattered) and dusk (every hen in, door shut).

## Known limits / next
- Villagers and hens are simulated only while the player is in the zone. There is no off-screen catch-up, and the clock is not saved.
- Five zones (levels 1-13). More candidates with canon maps: Deep Veins, Argentis/Lowtowns, Forge District, Glass Coast,
  Iron North (see `CRULANDA_LOCATIONS.md`).
- Water:
  - It doesn't receive shadows (a transparent surface shader in the built-in pipeline).
  - The camera stays above the surface, so there is no underwater view.
  - Swimmable water: Oakhaven's Brook pond and Withy pool, and the Peaks' Cold Tarn.
- The crypt has no interior/dungeon yet; the Whispering Wood does not whisper (no audio).
- Primitive-built art, no authored models/animation/audio; windows are flat emissive panes; no interiors.
- The camera pulls in on scenery collisions but can still clip very thin geometry.
