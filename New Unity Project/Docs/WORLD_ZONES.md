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
- Prop kinds: house, inn (walk-in), barn, mill, coop, forge, stall, oven, tannery, woodpile, well, dead_oak, great_oak (Oakhaven's living Great Oak, with a stone bench ring), tree, pine, fence, hedge, haystack, cart, barrels, crates, lamp, grave,
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

## Checking visuals without playing
- `Crulanda.exe --crulanda-world-capture <dir>`: scenic tour screenshots (HUD hidden), isolated temp save.
- `Crulanda.exe --crulanda-ui-capture <dir> [--crulanda-class class.druid]`: talent panel + combat HUD screenshots.
Both need a visible window (not -batchmode). Latest shots: workspace `work\world-captures`, `work\ui-captures`. A landmark that sets `view` (where to stand, facing it) with `viewPitch`/`viewZoom` is shot from there as authored. Other landmarks, and the first exit's waystone, are shot from a searched viewpoint with the player hidden (`LandmarkView`: a named prop is framed on its own bounds, a building from its front). When no spot is clear, the tour falls back to the orbit from the south-west and logs it.

## Oakhaven (CANON-EXPANDED)
Canon: an eastern agricultural hub erased by an accelerated, localized Wasting that hid the Council's abduction of
resonance candidates (source: `D:\code\crulanda\maps\interactive_map.html`, `maps\oakhaven_village_map.png`).
The zone is Oakhaven before that erasure (the game is set before it happens): the Great Oak on the green, the Golden Cask inn (canon name, book1\chapter_4.md), the communal well,
fields, Oak creek with two bridges, and the Wasting eating the eastern edge as grey static.
GAME-ONLY / PROVISIONAL: the Concord collectors and warden, Mira, exact building placement, the chapel ruin and graves, and the old wayshrine at the edge of the grey.

## Khaven Village (CANON-EXPANDED)
Canon names from `maps\khaven_village_map.png`: the Cracked Hearth, two Fallen Smithies, the Blood-Stone Well, the Gallows
Tree, the Crypt-Keeper's Hovel, the Whispering Wood, Gloom Creek and the Carrion Cliffs. Built as a walled village at dusk
with gate towers, a dead grey wood, dark creek water and an old crypt under the cliffs.
Biome `gloom`: hard grey-brown earth, thin dry grass and no flowers, grey dead woods, withered pines and brush, and a drained
rose-violet grade. The dusk holds all day: `lighting.sunHigh` keeps the sun low, and `skyTint`/`skyExposure`/`skyHaze` give a
dusky sky, while night still falls on the global clock. No Wasting here: only zones whose JSON has a `wasting` block get one.
GAME-ONLY / PROVISIONAL: the Sandthrone outriders holding it (Sandthrone is a canon mercenary faction), the Pale watcher
(canon "Pale Things" as cosmic auditors), the crypt and all exact placement. GAME-ONLY looks: the Cracked Hearth's split chimney with the fire showing through (a reading of the canon name; the map draws it as the inn), the drowned graveyard's flood pool and heaved graves, and the creek barrow's turf mound.

## The Shattered Peaks (CANON-EXPANDED), levels 6-8
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

## The Ashland Rim (CANON-EXPANDED / PROVISIONAL), levels 9-10
Canon (book1 ch.20): the Ash-Walkers (Chieftain Grohl, Mother Vane the Salt-Speaker) live in caves along the Wasting and fight
Weave-Eaters with Salt of the First Sea. The Cult of Ash and its iron-wired bone masks are canon (book1 ch.10).
The canon Ashlands only take their full form later (after the Spoke fires), so this pre-erasure rim is PROVISIONAL.
GAME-ONLY:
- the zone's name and roads;
- the shrine's location, Cinderfold, the ash hounds and the brood;
- all placement.
Biome `ash`: grey cracked ground with no grass, dead trees and falling ash.

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
| Oakhaven 1-2 | Harrow wood wolves (1-2), South copse boars (1-2), North pines wolves (2), Tall-grass stalkers (1-2, ambush), Brookside boars (2) |
| Khaven 3-5 | Whispering Wood wolves (3-4), Carrion boars (3-4, ambush), Sandthrone outrider camp (4-5), Gloom Creek hollows (5), The Grey Sexton (5, elite) |
| Peaks 6-8 | Toll-gate guards (6-7), Wolf pines pack (6-7, ambush), Rockhide wallow (7), The High Ledge (8), Captain's eyrie (8, elite) |
| Ash Rim 9-10 | Ash hound pack (9), Unwoven Flats eaters (9-10), Tear-marked shrine (9-10), Cinderfold hollows (10), The Weave-Eater brood (10, elite) |

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
  (the houses' doors), places from the zone (fields, well, green, inn seats, mill), chat in pairs, bark at the player,
  and flee home when fighting starts nearby. Critters never fight and scatter from the player; crows fly.
- **Day/night** (`Scripts/World/WorldClock.cs`): one game day lasts 40 real minutes and starts at 08:30. The hour
  continues across zone travel (it is not saved yet). The directional light is the sun by day and a pale moon at night.
  Dawn, day, dusk and night keyframes blend with the zone's own `lighting` palette: ambient, fog, sky tint and exposure.
  Lamps (`NightLights`) light up after dark. The minimap rim shows a sun or moon and the time. In development builds,
  F11 skips an hour.
- **Night routine:** villagers go home to bed from about 20:00, children first. Drinkers stay at the inn until 23:00 or
  later. Everyone wakes between about 05:50 and 07:30.
- **Hen coops** (prop kind `coop`, registered in `ZoneBuilder.Coops`) are raised huts with a ramp, a pop-hole door on a
  hinge, nest boxes and a feed trough. Each coop gets a hen-wife (Oakhaven: Goody Marl at Harrow, Hettie Brook on the
  green, Nan Pennock at Brook farm), and chickens belong to the nearest coop. Her day runs like this:
  - 05:45: opens the coop, and the hens come down the ramp one by one from 06:00.
  - Morning and afternoon: scatters feed at the trough, and every hen in the yard comes running.
  - During the day: hens lay up to one egg each. She collects them from the nest boxes and carries the basket home.
  - From 18:36: hens head in on their own.
  - From 19:18: she herds the stragglers (walks round behind the farthest one), shuts the door once all are in, and goes to bed.
  - Night: hens roost inside and are hidden.
- **Trades** (GAME-ONLY):
  - Villagers take trades in this order: blacksmith, farmer, merchant, baker, gossip, hunter, drinker, child,
    woodcutter, herbalist, farmer, elder, miller, leatherworker, skinner, and so on.
  - A trade whose workplace the zone lacks falls back to farmer or gossip.
  - Each trade has its own outfit and tool (`ActorVisual.Dress`) and a `<Title>` under its nameplate.
  - Workplaces are new prop kinds registered in `ZoneBuilder.Workplaces` (a stand point and a look target):
    `forge` (smithy with lit hearth, anvil and quench barrel), `stall` (awning and goods; variant 0 produce, 1 cloth and
    pots, 2 bread), `oven` (clay bake oven), `tannery` (hide frames, vat, scraping beam) and `woodpile`.
  - Hunters and woodcutters work the groves. Herbalists gather on the open meadow.
  - Work poses: Hammer, Chop, Gather, Knead. Each trade has its own lines of talk.
  - Oakhaven has Vell's smithy, Market row (three stalls), Thorne's bakehouse, the Tannery yard and the Woodyard, with 20 villagers.
  - The capture tour shoots each trade at work, plus a line-up of every trade on the green.
- Tests: `Tests/PlayMode/VillageDayTests.cs` cover three cases: night (coops shut, hens roosting, village abed), morning
  (coops open, feed scattered) and dusk (every hen in, door shut).

## Known limits / next
- Villagers and hens are simulated only while the player is in the zone. There is no off-screen catch-up, and the clock is not saved.
- Four zones (levels 1-10). More candidates with canon maps: Deep Veins, Argentis/Lowtowns, Forge District, Glass Coast,
  Iron North (see `CRULANDA_LOCATIONS.md`).
- Water:
  - It doesn't receive shadows (a transparent surface shader in the built-in pipeline).
  - The camera stays above the surface, so there is no underwater view.
  - Only Oakhaven has a swimmable lake.
- The crypt has no interior/dungeon yet; the Whispering Wood does not whisper (no audio).
- Primitive-built art, no authored models/animation/audio; windows are flat emissive panes; no interiors.
- The camera pulls in on scenery collisions but can still clip very thin geometry.
