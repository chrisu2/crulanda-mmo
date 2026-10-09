# Changelog

## Phase 0 — Foundation (unreleased)
- Project layout, six runtime asmdefs, editor asmdef, EditMode + PlayMode test asmdefs.
- Core: categorised logging with ring buffer, EventBus, Services, ContentId/EntityId, hierarchical GameTags + TagSet,
  vocabulary enums, deterministic SeededRandom.
- Data: DefinitionBase, ContentRegistry, ContentDatabase, ActorArchetypeDefinition.
- Gameplay: StatBlock, VitalPool, LevelCon, Actor/Health/ResourcePool, ActorRegistry, ITargetable, ActorDiedEvent.
- Persistence: SaveEnvelope, SaveMigrator, atomic SaveFileStore.
- DevTools: command registry, built-in commands, IMGUI console/HUD.
- Game: GameBootstrap composition root.
- Editor: Crulanda > Phase 0 > Build Test Map; Crulanda > Validate Content.
- Docs, .gitignore, .gitattributes.
- Unity editor version used: **TBD (record on first open)**.

## 2026-09-28 — Integrated in Unity 6000.6.3f1
See VALIDATION.md for fixes, regression tests, real editor results and remaining limitations.

## Playable encounter implementation
Added the Crulanda.Encounter assembly, separate PlayableEncounter scene builder, input system, progression/persistence and automated end-to-end tests. See PLAYABLE_ENCOUNTER.md for scope and controls.

## Shared ability runtime milestone
Added Crulanda.Abilities: stable ability definitions, per-actor cooldown/GCD/cast timing,
interruption and resource-commit rules. Player and healer use the shared runtime.
Updated ROADMAP, ARCHITECTURE, DATA_SCHEMA, SAVE_FORMAT, SIMPLAYER_DESIGN, KNOWN_ISSUES and ADR.
Combat timing remains tuned toward the user's requested slower pacing. Validation tracked separately.

## Phase 1 combat systems completion pass
Added shared damage resolution, source-owned timed statuses, reactive derived stats and actor lifecycle hooks.
Moved Guard/equipment/level math onto shared systems. Fixed frame-dependent proximity threat and
batched multi-stat updates. Added PHASE1_EXIT and PHASE2_CLASS_LOOPS planning documents.
Final validation evidence is recorded in COMBAT_SYSTEMS_VALIDATION.md.

## Playtest feedback: auto-attacks and scenery
- Added a visible automatic swing countdown/progress bar and range state; activation remains unchanged. Weapon swings continue between ability uses.
- Solid camp props, trunks, rocks and ruins now collide and contribute blocked navigation footprints in existing scenes.


Validation: existing 15 PlayMode regressions and 2 targeted tests passed (automatic swing during ability cooldown; scenery collision and navigation detour).

## Phase 2 foundation
- Added class profiles and validated ownership/unlock tables; player resource/stats and action bar now consume class data.
- Added hybrid build graph rules and regression tests.
- Adopted Warrior Tank/DPS/Support and Druid Tank/Melee/Healer/Ranged matrix; playable tree UI/effects are the next milestone.


## Full class build matrix
Expanded CLASS_BUILD_MATRIX.md to all initial classes and later Crulanda candidates: 20 entries / 61 proposed paths. Added per-build loops/tradeoffs, class resources, hybrid examples, overlap decisions, source status and role coverage. Documentation only; no new playable classes claimed.

## 2026-09-28 — All-class talent calculator (design tool, not game code)
- Built `Crulanda-Talent-Calculator.html` (workspace `outputs`), a self-contained classic-style talent calculator: 20 classes, 61 trees, 875 proposed talents, plus the implemented 9-node Warrior prototype.
- Replaced the earlier generated placeholder nodes (`work\calculator\data.json`) with per-path authored trees in `work\calculator\trees\*.json`, reviewed by `tweak.py` (dedupe names, fewer cheat-death effects, meters, capstone reachability).
- Prototype mode reuses BuildTree.Validate's reachable-purchase rule; no Unity code changed. Game-side talents remain the 9 Warrior nodes.

## 2026-09-28 — Data-driven Warrior talents (27 playable), save format 3
- Added TalentTree (JSON-loaded tree; replaces static WarriorTalents) and WarriorKit (all Warrior talent effects).
- New actions via talents: Intercept, Breaching Blow, Muster. New mechanics: weapon pressure, Exposed, barriers, stagger.
- HUD: talent tree panel, pressure/barrier readout, enemy swing timer + Exposed on the target frame, 6-slot action bar.
- EncounterSave format 3 with v1/v2 migration; fixed Encounter.asset line where `playerClass` was merged into the
  previous line (Unity had silently used C# defaults for the class).
- Opt-in `--crulanda-ui-capture <dir>` HUD screenshot mode; F10 level-10 shortcut in development builds.
- Validation: 145/145 EditMode, 26/26 PlayMode, Windows build OK. The full-encounter test now uses Challenge,
  because Mira's healing threat could legitimately pull a sentry off a non-taunting player.

## 2026-09-28 — Druid, multi-class characters
- ClassKit refactor (shared systems no longer reference the Warrior), Druid kit with four forms and 28 talents,
  PeriodicEffects, enemy snares/roots, player cast bars, per-class saves and character switching, 10-slot action bar.

## 2026-09-28 — Oakhaven: first real zone and character visuals
- Crulanda.World assembly: ZoneDefinition (JSON), ZoneBuilder (ground, painted terrain, water, buildings, trees,
  the Wasting, lighting), ZoneArt palette, zone-aware navigation. Oakhaven is the opening scene, laid out from the
  canon village map. Humanoid placeholder characters with walk animation; camera collision; zone title/objectives in HUD.
- Save: `zoneId` on progress; position bounds widened for larger zones.

## 2026-09-28 — Day/night cycle, hen-wives and coops
- WorldClock:
  - A 40-minute game day, with the sun turning into the moon at night.
  - Dawn, dusk and night blend ambient, fog and sky with each zone's lighting. Lamps light after dark.
  - A sun or moon icon and the time on the minimap. F11 skips an hour in development builds.
- Villagers go home to bed at night (drinkers stay late) and come out at dawn.
- New `coop` prop (raised hut, ramp, hinged pop-hole door, nest boxes, trough). Oakhaven has three coops, each with a
  hen-wife who opens up, feeds, collects eggs, and herds the hens in at dusk before shutting the door. Hens roost at night.
- Capture tour adds coop morning, dusk, round-up, shut and night shots plus village night and dawn shots. New PlayMode VillageDayTests.

## 2026-09-28 — Village trades
- Trade outfits and tools for villagers:
  - Blacksmith, merchant, baker, hen-wife, farmer and hunter.
  - Leatherworker, skinner, woodcutter, herbalist and miller.
  - Elder and drinker.
- WoW-style `<Trade>` nameplate subtitles with a text shadow.
- New workplace props (forge, stall, oven, tannery, woodpile) and work poses (Hammer, Chop, Gather, Knead).
- Oakhaven has a smithy, three market stalls, a bakehouse, a tannery yard and a woodyard, with 20 villagers. Each trade has its own lines.
- Fixed: villagers who came back out of a house showed their hidden placeholder capsule.

## 2026-09-29 — Quest system, save format 4
- Data-driven quests (EncounterContent/Quests/*.json) with validation.
  - Kinds: main (Chronicle), side, NPC and faction.
  - Objectives: talk, deliver, collect, kill, interact, visit and flag.
- New interface:
  - Head markers and map markers (! and ?).
  - Conversation window.
  - Quest book [L] with Quests, Chronicle and Standing tabs.
  - A new tracker.
- Six factions with standing tiers. Readable Chronicle pages.
- Oakhaven: 10 quests. The black-iron wagon and the Bureau ledger (CANON-EXPANDED).
  - New interactable props: tithe crates, supply cart, grain bin, grey-hearted oaks, yarrow patches.
  - Two residents: Quill the Salt-Mender and Warden Ivel.
- The inn is renamed the Golden Cask (the book's name). The game is set before Oakhaven's erasure.
- Save format 4: quest state, standing, pages, quest bag and emptied props. Older saves upgrade on load.

## 2026-09-29 — Zones 1-10, camps, items, water and a graphics pass (save formats 5 and 6)
- **Level ladder (cap 10):**
  - Oakhaven 1-2 → Khaven 3-5 → the Shattered Peaks 6-8 → the Ashland Rim 9-10.
  - Peaks and Ash Rim are new zones.
  - New XP curve: `XpToNext(L) = 200 + 90*(L-1)`.
  - Kill XP scales with mob level and con colour, and mobs 4 or more levels below give none.
- **Camps:** five respawning grinding camps in every zone, some with an elite.
  - Ambush camps hide in tall grass and pounce. Hold Ctrl to sneak past.
  - Mobs give up a chase they can't reach.
- **Breadcrumbs:** quest chains lead from each zone to the next (`main.oakhaven.3` → Khaven → Peaks → Ash Rim).
  - The tracker shows "→ Travel to X (via Y)".
  - Exits are labelled with their level band on the minimap and zone map. The world map shows roads.
- **Targeting:** left-click selects NPCs. E talks to the selected NPC, otherwise to the nearest one you're facing. Quest givers stop while you talk to them.
- **Items:**
  - A 24-slot bag (I): drag to move, drag out to destroy (with a confirmation).
  - Character sheet (C): a paperdoll with eight slots and totalled stats.
  - Merchants by trade: buy, sell and buy back.
  - Loot tables per mob type, generated gear, food and potions (`EncounterContent/Items/items.json`).
- **Water:** one model (`ZoneWater`) for what is drawn, carved and felt.
  - Creeks run downhill in carved channels. You can wade them; they cost agents extra.
  - Lakes (Oakhaven's Brook pond) are swimmable, with floating, a Swim pose, climbing out and splashes.
  - Bridges are walkable and clear the water.
  - Critters and trees stay out of the water.
  - A save made while swimming loads you on dry land.
- **Nature:**
  - Denser wind-swayed grass, with tall-grass patches.
  - Falling leaves under broadleaf trees, and falling ash on the Rim.
  - New tree, bush and boulder shapes.
  - Mountain and ash ground painting.
- **Lighting:**
  - HDR post pass: bloom with a Karis prefilter, sun shafts, ACES and a per-biome grade.
  - A live sky reflection probe, and night sky tinting.
- **Characters:** new looks for Hollows, cultists, wolves, boars and Weave-Eaters. Villagers get faces and hair.
- Moved the chicken coop out of the village centre.
- **Saves:**
  - Format 5 moves experience onto the new curve (level and progress into it are kept).
  - Format 6 adds the bag and equipment lists. Older saves upgrade on load.

## 2026-09-29 — Water, world edges and night (first fixes from the visual review)
These fix the "water, edges, night" group of `Docs/VISUAL_REVIEW_2026-09-29.md`.
- **Bridges:** removed the old centre pier. After bridges were seated on their banks it showed as a grey slab on the water under every arch.
- **Water shading:**
  - The water shader now has its own lighting (`LightingWater`). Sky reflection is fresnel-weighted: `_Reflect` face-on, rising to full at grazing.
  - Reflections fade where they dip under the horizon, and highlights are capped (`_Glare`).
  - A new zone field, `waterReflect`, sets how much the water reflects. Khaven's Gloom Creek uses 0.3, so it stays dark instead of chrome.
  - Foam is a broken, noise-driven line rather than a uniform ring.
  - Creek ripples now flow downstream.
  - Night reflections actually dim: the zone's sky probe intensity follows `reflectionIntensity`.
- **Banks and shapes:**
  - Lakes have an irregular shoreline: the radius varies smoothly with angle, and carving, drawing, feel, navmesh and tests all share it.
  - Creeks meander gently and vary in width.
  - The bank paint follows the real waterline.
  - Ground normals come from central differences, which removes the sawtooth shading on curved slopes.
- **Mill:** the mill wheel snaps to the creek surface so its paddles dip in.
- **Swimming:** weapon and shield (and the Druid staff) are slung on the back while swimming.
- **World edges:** a backdrop skirt continues the ground past every zone edge and rises into biome-coloured hills with tree and rock silhouettes.
  - The sky below the horizon now matches the fog at every hour, so there is no brown plane or void past the exits.
  - The camera's far plane is extended to cover the backdrop.
- **Night:**
  - A moonlit blue ambient floor, and the moon at 0.26.
  - The post contrast curve gets a soft toe below mid-grey, so shadows deepen without clipping to pure black.
  - The Great Oak's bark is paler.
- Tests: EditMode 166/166, PlayMode 47/47. `WaterTests` now checks each creek row at its own width, and lakes along their irregular edge.

## 2026-09-29 — See-through water, currents and ripples (Chris: "more water-like, more see-through, better physics")
- **Water shader:**
  - A named GrabPass plus the camera depth texture: the bed and anything underwater show through shallow water, bent slightly by the ripples.
  - The water fades into its own colour with the depth the view crosses (`_Murk` per metre).
  - The water's own colour is lit by ambient plus sun or moon, so it darkens at night.
  - Foam is thin and broken, and appears only where the water touches something: banks, legs, posts. There are no more foam bands, wedges or blotches.
  - The zone map (an orthographic camera) shows plain water colour.
- Murky zone water (`waterReflect`) is also harder to see into and calmer (Khaven: murk 2.6, ripples x0.55), so it no longer reads as tar.
- **Currents:** creeks push you gently downstream (`ZoneWater.FlowAt`). The push is strongest mid-channel and in deeper water, and weak enough that you can always wade across.
- **Ripples:**
  - Ripple rings now use a real ring sprite (they drew as white discs).
  - Standing or treading water sends out slow rings.
- **Backdrop:** pines have bark trunks under lifted boughs. Mountain backdrops climb more gently near the edge, so the Peaks exit no longer faces a fog wall.
- **Grade:** daytime keeps the original crisp contrast. The shadow lift (a power toe) now fades in only with darkness (`_Lift`).
- **Clear water, no black blotches** (Chris: "still looks dark and not clear"):
  - Ripples flatten at grazing angles and in the distance.
  - Below-horizon reflections keep 45% instead of going black.
  - Grazing angles cost less light through the water.
  - Clear water has murk 0.8.
- **Trees:** dead trees (the Great Oak and the grey husks) get a root flare, a broad swell with half-sunk buttress ridges, in place of five thin tilted cylinders that read as sticks laid round the base.

## 2026-09-29 — Camera looks through trees; clear target confirmation (Chris's playtest)
- **Trees fade instead of blocking the camera** (`World/TreeFade.cs`, `World/Shaders/Fade.shader`):
  - Every tree (broadleaf, pine, orchard, dead tree, the gallows tree) fades while one of its parts actually hides you: the trunk, a leaf clump, a bough tier or a limb. It fades back about 0.25 s after the view clears.
  - The fade is a screen-door dither with the same Standard lighting, so there is no brightness jump. Its shadow stays on the ground.
  - Tree trunks no longer pull the camera in.
  - Clicks pass through faded trees and through your own character.
- **Target ring** (`Encounter/TargetRing.cs`): a ring on the ground under whatever is selected.
  - Colours: red for a living enemy, gold for a body with loot (grey once looted), green for Mira or a villager.
  - It pops on selection, breathes gently, and lies along sloped ground.
  - The selected enemy's nameplate is boxed in gold, and all enemy nameplates are now centred.
- The tour captures `99-target-ring` and `99-tree-fade`. New tests are in `CameraAndTargetTests` (PlayMode).

## 2026-09-29 — Step 2: zone looks (Khaven dusk, Peaks mountains, grey Ash Rim, the Wasting edge)
- **Bug fix: the phantom Wasting.**
  - JsonUtility never leaves a nested class null, so zones without a `wasting` block (Khaven, the Peaks) got a default Wasting at x = 58.
  - That meant flat grey unmade ground, husks, a curtain, and an invisible wall across Khaven's Carrion boars camp and the Peaks' east side, including the Captain's eyrie, The High Ledge and the east exit.
  - `ZoneBuilder.ParseZone` now keeps a Wasting only when the zone declares one.
  - ZoneContentTests now path-checks every exit and camp from the player's start.
- **Khaven**, new biome `gloom`, a permanent dusk:
  - A low orange sun, capped by the new lighting field `sunHigh`, and violet ambient.
  - Rose-mauve haze and a zone sky (`skyTint`, `skyExposure`, `skyHaze`).
  - A drained dusk grade.
  - Grey-brown hard earth with thin dry grass and no flowers.
  - A grey dead Whispering Wood, and withered pines and brush.
  - A dead, pine and rock forest edge on the east too, and a grey backdrop wood.
- **The Shattered Peaks** as real mountains:
  - Steep knolls, rock ribs and jagged walls climbing to the edge. A keep-grid holds roads, camps, props, landmarks, exits and spawns at walkable height and connected.
  - Cliff shelves (new prop field `lift`).
  - Slope-aware rock, scree and turf paint, sparse tufted grass and scattered boulders.
  - Darker, cooler light with blue distance fog and a crisper grade. Tall boundary colliders.
- **The Ashland Rim** as grey petrified ash:
  - A neutral grey ground with angular cracks (not worm trails).
  - Grey ruins and rocks, and a bruised violet sky.
  - Fog lighter than the ground, so distance reads correctly. A lower, less walled-in backdrop and a lighter vignette.
  - Falling ash made visible: bigger, paler flakes, prewarmed, facing the camera.
- **The Wasting edge:**
  - A ragged, fraying front for paint and grass. The unmade ground drains to flat grey static.
  - The curtain is an unlit fog-coloured sheet with crawling bands (not glass).
  - A far haze bank in the fog colour, so the flat unmade fades into haze instead of meeting the sky in a hard line.
  - The Oakhaven Wasting landmark moves to the front.

## 2026-09-29 — Step 2 finished: fix round and zone travel check
- **Step 2 fix round:**
  - Khaven roads and clearings are browner and darker than the rose ground, with dry verge grass.
  - Khaven's tall grass is dark grey and lower, so boars and wolves read against it.
  - Peaks pines on steep ground are refused, sunk into the ground or swapped for rocks.
  - Peaks rock and scree are darker and shaded toward the midday sun, and the close-up grain is softened.
  - Ash Rim's large cracks are thin and light, the crazing no longer covers the unmade ground, and near ash flakes are small.
  - The Wasting curtain spans 1.3 times the zone and fades out at its ends. Its static only ever lightens, and it flattens at night.
  - Oakhaven's unmade ground is greyer, with fine static.
- **Zone travel** (Chris couldn't travel in the `76ed4eb` build):
  - New `ZoneExitTests` walks the player into every exit of every zone with the real character controller, checks the E prompt, travels, and checks the arrival.
  - Everything passes on this build. The invisible phantom-Wasting wall, fixed in step 2, blocked the Peaks' road east and Khaven's east side.
  - Travel is now refused only by a fight nearby (an enemy engaged within 40 m), and the message names who is still after you.
- Tests: EditMode 166/166, PlayMode 50/50.
- Still to polish, to be done alongside step 3:
  - Khaven roads need more contrast.
  - A pine cluster above the Peaks exit still floats.
  - Peaks rock is still a little pale.
  - A straight sky seam in `oakhaven-18`, probably the sun shafts rather than the curtain.

## 2026-09-29 — Step 3: props, landmarks and creatures
- **The Great Oak** (new prop kind `great_oak`) is the living centrepiece of Oakhaven's green.
  - CANON: book1 ch.4 describes it as massive and ancient, the heart of the square, later petrified mid-bloom. The game is set before that, so it is alive and in full leaf.
  - Built as:
    - one lofted, tapered trunk with gnarls, fluting and burls, on a twisting buttress flare, with surface roots;
    - five sprawling limbs that merge smoothly into the bole;
    - a layered crown of about 45 leaf clumps (about 21 m tall and 24 m across);
    - a low dry-stone bench ring.
  - The zone's random draws are kept, so the village layout doesn't move.
- **Trees:** broadleaf and orchard trunks taper, lean slightly, and have a buttress root flare instead of the pipe collar. There are six bark shades, and limbs grow out of the trunk toward the crown. Trees keep a minimum spacing.
- **Critters have legs:**
  - The cat has four legs and paws, sometimes white socks. The chicken and crow have thin legs with toes. The rabbit has haunches and forepaws.
  - The legs swing with the stride, and flying crows glide and fold their wings.
- **Creatures:**
  - The Pale's head is attached, and they read as tall, gaunt figures in robes.
  - The Weave-Eater is remodelled to canon (book1 ch.20): a drifting, frayed, flickering mass of unravelling threads that moves in jerks. It is not a dog.
- **Floating and clipping props fixed:**
  - the inn sign's bracket, broadleaf limbs, the tannery beam, the bakehouse oven's woodstack and peel;
  - house doors, rails and plinths on slopes, the stacked crates, ruin beams and roofs;
  - hill-seated footings, and crows' wings in flight.
- **Landmarks that match their names:**
  - Oakhaven: the wayshrine, the Wasting view, and the Golden Cask front.
  - Khaven: the drowned graveyard, a flooded pool with heaved-over graves; the Cracked Hearth, a split chimney breast with the fire showing through; and the Creek barrow, a mound with a stone doorway and passage.
  - Peaks: the toll gate, gate leaves on a carved road; the Captain's Eyrie, a keep on a rocky perch; Umbra Scarp; and Rockhide Wallow, a mud pool.
  - Ash Rim:
    - the Ash-Walker cave enclave, cave mouths in a cliff with hide-and-bone shelters;
    - the Brood nest;
    - the Old Ribcage, paired bone ribs along a spine;
    - the Tear-marked shrine, an altar, bone-mask idols and braziers.
- **Capture tour:** landmark shots search for a clear viewpoint, or use an authored `view`.
- **Step 2 polish:**
  - Khaven roads and clearings are now a neutral dark earth.
  - Peaks backdrop trees and tors stand on the drawn ground, and pines aren't placed on near-vertical slopes.
  - Peaks rock is darker.
- **The oakhaven-18 sky seam is fixed.** A faded tree was still written whole into the camera's depth texture, cutting the soft haze and curtain along its silhouette. Its ShadowCaster pass now dithers like the tree does.

## 2026-09-29 — Step 4: HUD overlaps and readability
- **World labels** (nameplates, ! and ? markers, speech bubbles, place names) are placed as a set each frame.
  - They stay off the HUD panels (frames, minimap, tracker, chat, prompt, bottom bars) and inside the screen.
  - They hide behind solid scenery (one ray each, ignoring people and trees), and stack instead of overprinting: the nearest keeps its place.
  - A bubble that a panel would cover slides off it, and never covers a quest marker.
  - Enemy plates sit just above each model's real top, so wolves and boars get low plates.
- **Readability:**
  - A 1 px dark outline on nameplates, tracker lines and place names, so yellow and green con colours read on grass.
  - The quest tracker and message log draw on a subtle ink plate sized to their text.
  - With nothing tracked, the zone name and the "look for !" hint fade out after 14 s.
  - Enemy nameplates are centred over their bars.
- **Road signs:** exits show a label from 60 m ("Road to Khaven Village (3-5)", in the band colour), so the way out is never a mystery.

## 2026-09-29 — Step 5: stylized turquoise water (Chris's art direction)
Chris shared a reference of a stylized fantasy cove and chose this look for Oakhaven, the Peaks and the Ash Rim; Khaven's Gloom Creek stays murky. It is a style reference only, with no assets or names copied.
- **New `Water.shader`:** hand-painted water.
  - Colour by depth: pale shallows (`_ShallowColor`) to turquoise (`_MidColor`, by 0.55 m) to deep teal-blue (`_DeepColor`, by 2.2 m), lit like the scene, so night water stays dark.
  - The bed shows through the shallows, lifted by scattered light (`_Glow`), fading out with murk.
  - Soft, slowly wobbling white foam at shores and around anything in the water, plus a thinner line washing in every few seconds on still water.
  - Painted highlights (`_Sparkle`): sparse away from the sun, crowded and bright along its path. Faint moon path at night.
  - Only a gentle sky tint at a slant (`_SkyTint`), with no mirror.
  - The zone map shows the depth colours only.
- **Zone settings** (`ZoneBuilder.WaterMaterial`): `waterTint` recolours the whole depth ramp dark, with dull scum foam; `waterReflect` sets the sky tint and damps sparkle, foam and ripples for murky water.
- **Sandy bed:** the shore paint at the waterline and under water is pale sand in non-gloom biomes, so the shallows show a sandy bottom. Khaven keeps its dark silt.
- The earlier `step5-water-realism` patch (planar mirror) is dropped; it conflicts with this direction.

## 2026-09-29 — Fix: a temp-save run lost its character on zone travel
- Chris reported "talents reset when I zoned into the Ashland Rim" on a `--crulanda-temp-save` run.
- Cause: the throwaway save folder was chosen in `Start`, and travel reloads the scene, so each zone minted a new folder and found no save. The whole character (level, gear, talents) started fresh, not just the talents.
- Fix: the temp folder is static and shared by the whole play session. New test `TempSaveTravelTests` spends a talent, earns a level, travels, and checks everything arrives. Real saves were never affected.

## 2026-09-30 — Step 3 fix round published (with the temp-save travel fix)
- **Temp-save travel:** a `--crulanda-temp-save` run keeps one folder for the whole session, so level, gear and talents survive zone travel. New test `TempSaveTravelTests`.
- **Weave-Eater** is a drifting tangle of 30 curved violet threads round a dim core, with glowing tips, a slow bob and wander, and the old jerk. No head lobe, no legs.
- **The Pale** have a deep cowl (no gold sliver from behind), a narrower, longer robe and a slight stoop.
- **Crows** fold their wings on the ground; they spread only in flight.
- **Khaven's Fallen Smithy** ruin moved off the Cracked Hearth's sight line and out of the house's doorway.
- **Backdrop trees** stand on the drawn skirt mesh (exact triangle interpolation), skip ledges and crests, and sink into slopes. The Peaks exit no longer shows floating pines.
- **Rockhide Wallow** has a wet, sheened pool with a raised mud rim. The **Great Oak's** bench ring is mitred arc segments, and its crown is layered: big squashed masses over the limbs, a middle ring, a low skirt and small tops.
- **Umbra Scarp** became a stepped course of blocks. This REGRESSED: it read as a wall of smooth grey slabs. Redone the same night (below).
- Tests: EditMode 166/166, PlayMode 51/51, 0 shader errors.


## 2026-09-30 — Cliffs as rock, not slabs
- Every cliff step (lifted scarps and free crags, in the Peaks and Khaven) is now a stack of overlapping faceted rock lumps: a wide base, an upper mass leaning into the shelf, and a crest knob on most steps, so the crest is ragged and nothing has a flat top.
- The stone texture tiles at about 2 m across each lump (blob meshes gained UV repeat and flat-shaded faceting).
- Fallen blocks and scree at the foot, sunk into the ground, kept off roads, water, camps, exits, arrivals, spawns and other props. No colliders, so the navmesh is unchanged; the player can brush into foot rubble.
- The lip course on the shelf edge is lumps too. All colliders and the zone's random draws are unchanged.

## 2026-09-30 — Tree crowns: painted leaf cards (Chris: "trees still look bad")
- The trunks were fixed earlier; the crowns were smooth blob balls and solid cones. Crowns are now **painted leaf cards on boughs**.
  - Three generated leaf-cluster textures (green, yellow-green, autumn) and a pine-bough texture, alpha cut-out with ragged edges and gaps, so sky shows through.
  - Broadleaf and orchard crowns: 7-10 clusters of crossed cards on thin bowed boughs from the limbs, a dark squashed core inside so the crown isn't hollow from below, and low filler clusters round the rim.
  - Pines: a full-height bark-grain pole with five tiers of drooping bough cards crossed by hanging fins, darker at the foot, a crossed leader at the tip. No cones.
  - The Great Oak keeps its clump crown and gains a card fringe round the skirt, rim and top.
- New `Crulanda/Leaf` shader: Standard lighting, alpha test, drawn two-sided; the cards bake **crown normals** (away from the crown's heart, lifted toward up) so both faces shade like the outside of one round crown and nothing goes black; per-vertex wind sway keyed to world position; a shadow-caster pass with the same sway and cut-out.
- `Fade.shader` and `TreeFade` fade cut-out leaves too, so a card tree still goes see-through for the camera and keeps its ragged shape.
- The zone's random draws are unchanged draw-by-draw, so nothing else in any zone moves. Colliders and the navmesh are untouched.
- **Second pass (same day), after the first tour:** the first cards left pines as sparse poles and near crowns thin.
  - Pines: 5-6 tiers of 6-8 wide drooping boughs, overlapping down the trunk, widest at the foot, darker at the foot, with a dense crossed leader; a fuller bough texture (`pine_bough_full`). A full conifer triangle.
  - Broadleafs: three cards per cluster, 10-14 clusters, a core about 60% of the crown radius, and 3-4 low underside clusters, so looking up shows leaf mass. Orchard trees unchanged (they already read well).
  - Bushes: 4-6 leaf cards round a dark core, on the same leaf material, same footprint, still no collider.
  - Backdrop silhouettes: far broadleafs are three crossed cards and far pines three tiers of boughs, so the horizon matches the near trees.
  - Zone random draws verified unchanged site by site; colliders and the navmesh untouched.

## 2026-09-30 — Weather (Chris: "move weather up after the trees")
- **A weather system** (`WorldWeather.cs`): every zone has its own weather table (zone JSON `weather`, calmest first), turned
  into a seeded schedule of 7-minute spells that moves one step of severity at a time (clear, fair or windy, overcast or mist,
  rain or flurries or ash squall, storm) and blends over 50 s. The weather clock carries across zone travel; a session starts
  calm. Nothing is saved: the same zone at the same weather time always has the same weather.
- **Per zone:**
  - Oakhaven: clear to fair, overcast, rain and storms, about a fifth of the time rain.
  - Khaven: mostly mist, overcast and rain; it never clears.
  - The Peaks: flurries.
  - The Rim: ash squalls; it never rains on the ash.
- **Sky:** a painted cloud layer (`Clouds.shader`) on a dome round the camera. Clouds hold still in the world, drift with
  the wind, crowd toward the horizon, have lit tops and shaded undersides, and close into a grey lid under overcast that
  runs down into the fog. Matching **cloud shadows** sweep the land (post composite, from the depth texture and the same noise).
- **Light and air:**
  - Cloud takes the sun (less of the moon, so nights stay readable), flattens the ambient and softens shadows.
  - The fog greys, pales in mist, dulls in ash and closes in (never nearer than 55 m).
  - The sky's haze thins under cloud, so there's no orange horizon under rain.
  - The grade cools and loses its glare. Lamps light early under heavy cloud.
- **Wind:** gusts roll through the grass and leaf crowns as travelling waves, with a downwind lean (`_WeatherWind`; Leaf
  and Fade identical). Falling leaves and ash follow the wind.
- **Rain:**
  - streaks slanting with the wind, and splashes where drops land;
  - rings on ponds and creeks, and the water dulls and roughens;
  - the ground darkens as it soaks, then dries over about three minutes;
  - nothing falls under a roof, and a camera indoors sees none.
- **Storm:** rain, gales and lightning every 8-26 s. **Flurries** in the Peaks, **drifting mist** in Khaven, **ash squalls**
  (dust and an ash blizzard) on the Rim.
- **Player-facing:** a chat line when the weather turns ("It starts to rain."), and the weather word under the minimap clock.
- **For testing:**
  - F8 (development builds) cycles through the zone's weather, then back to its own.
  - `--crulanda-weather <kind>` starts in that weather.
  - Capture tours hold fair weather for comparable shots and add `80-weather-*` shots per zone.
- **Tests:**
  - EditMode `WeatherScheduleTests` (8): deterministic under a seed, only the zone's own kinds, one step at a time, smooth
    turns, a calm start, names that parse, sane looks.
  - PlayMode `WeatherTests` (5): rain falls, wets and dries the ground; none indoors; every kind keeps the fog in its band
    and the light readable, day and night; the schedule turns and is announced; the wind reaches the shaders and the
    clouds are up.
- **First tour, fixed before shipping:**
  - A yellow horizon band under every cloud lid, from the sky haze thickening under cloud.
  - A patchy, sunny-looking overcast.
  - A bleached storm frame: the lightning was far too strong, and the first bolt fell exactly when the tour shot.
  - Pale wet ground: too glossy, so it mirrored the sky.
  - Faint rain streaks, and invisible raindrop rings.
  - Turquoise water glowing under rain.
  - A per-spell System.Random that gave correlated draws, so one test seed went 600 spells without a storm; now a
    SplitMix hash.
- **Independent code review (a second agent read the whole diff), all fixed:**
  - Cloud drift wrapped every 2600 m, which jumped the finer noise layer; it now wraps over 10 tiles.
  - Rain flickered off whenever the camera brushed a wall or rose above the roofs. "Indoors" now also needs a ceiling overhead.
  - Cloud shadows darkened fully fogged land and distant hills. The shade now falls on the land only.
  - Rain splash droplets were soft-faded into the ground; they now have their own material.
  - Mist and ash haze were not refilled after travel or a teleport; they are now.
  - The sky's cloud sheet sat at camera height + 900 m while the shadows used 900 m; both are now at 900 m.
  - The sky below the horizon used the unweathered exposure.
  - The cloud dome could lag the camera by a frame; it now updates after the camera moves.
  - The capture tour's storm shot now waits out any lightning flash.
  - Rain streaks right at the lens are capped thin.

## 2026-09-30 — Crowsfoot Hollow and "The Tin Crown" (Chris: a level-3 quest to clear out the bandits and their king in a cave)
- **Crowsfoot Hollow** (GAME-ONLY) is Oakhaven's first walk-in cave, with no loading. The North road, which used to run to the
  zone's edge and stop at nothing, now ends at its mouth in the north hills.
  - A lofted rock passage (`Hollow`): an entrance tunnel, the deserters' camp chamber, a low passage bending west, and the
    deep hall.
  - A thick outer shell heaps it into a rocky knoll, closed at the back with a cut rock lip at the mouth; crest rocks and
    buttresses are seated into it.
  - The floor is the levelled ground (a new `shapes` pad), painted bare earth.
  - Torches flicker down the walls. The camp has a fire and pot, bedrolls, stolen grain, crates, barrels and a torn
    Sandthrone banner.
  - The hall has Caddock's plank throne on a stone dais between braziers, his banner, bones, and the plunder pile.
  - Inside, the skylight and air darken with depth: a fifth of the skylight, smoky brown haze, no sky reflections. The eye
    adjusts a little and firelight blooms. Nothing falls inside, no grass grows, and trees or bushes that would land in it are
    dropped (their draws still taken, so the zone's layout elsewhere is unchanged).
  - Collisions: the passage and knoll have mesh colliders and are navmesh obstacles. The camera and your feet stop at the
    rock, and agents path in through the mouth.
  - First build: a single-precision sin(pi) came out a hair negative and turned a column of the shell NaN (every Oakhaven load
    asserted). The first knoll also floated a flat slab over the tunnel; it is now the outer shell.
- **The Sandthrone deserters** (GAME-ONLY band; the company is CANON), new `deserter` look:
  - a torn sand tabard over grey mail, a red-ochre face-scarf, a hood or head-wrap, and a falchion or club;
  - each one assembled differently, so a camp reads as a ragged band.
  - They stand as 2 lookouts at the mouth, 4 at the camp fire, and 2 guards in the hall, levels 3-4.
- **Caddock, the Bandit King** (GAME-ONLY), new `banditking` look, elite level 4, in his hall:
  - a near-black long coat, a torn Sandthrone sash, a two-handed cleaver;
  - a crooked crown of hammered tin with a faint glint so it reads in the dark.
- **Quest "The Tin Crown"** (`side.oakhaven.crowsfoot`, level 3, from Wil Carder, whose barn they raided):
  - kill six deserters and Caddock, search the deserters' plunder, and bring back the north farms' stores;
  - rewards: 110 XP, 20 gold, +300 Oakhaven.
  - New quest field `minLevel`: the quest is hidden entirely (no offer, no "!") until level 3.
  - Villagers talk about the raids before, and thank you after.
- **Line of sight for aggro:** a mob out of a fight needs a clear line to you before it notices you, so nothing aggroes through
  rock or a house wall. Fights already on are unchanged.
- **Fixes along the way:**
  - A kill objective's `say` line printed its target pattern as the speaker, e.g. "mob.sexton.khaven*: ...". It is now narration.
  - The Peaks outrider's axe head floated a quarter metre off its haft.
- **Camps:** a camp may now be `harder` on purpose (up to three levels past its zone's band); the maps show every camp's
  levels in their level colour.
- **Tests:**
  - `CaveTests` (4): walkable from the green to the hall through the mouth; walled and roofed with nothing in the passage;
    dark and dry inside, rained on outside; nothing grows in it, and the deserters hold it.
  - `HollowQuestTests` (5): the minLevel gate, the targets, the looks, a full play-through, and line-of-sight aggro.

## 2026-09-30 — Secrets (Chris's order: after the cave)
- **Hidden finds in every zone**: five per zone, twenty in all (GAME-ONLY placement, canonStatus on each). None is on any map;
  you find them by going off the road and looking.
  - **Lookouts** (`vista`): stand on the spot and it counts, for XP. The Road's End, the Old Beacon, Above the East Road,
    the Salt Line.
  - **Caches:** an oilcloth bundle or a dented tin tucked behind rocks, in a log, under a stone or at a cold camp. Gold, and
    a piece of gear or supplies.
  - **Pages:** a folded page in a crack, a hollow stump or a drowned chapel wall, read in the Chronicle.
  - **Herbs:** a patch of pale bells glowing faintly, a healing herb of the zone.
  - **A key and its chest** in Khaven: the key under the gallows opens the Outrider's Hoard. Without it: "Locked. The key must
    be somewhere near."
- **Finding one:** search with E within reach (a lookout needs only standing there). "Discovered: ..." shows as a toast and a
  chat line with what it paid. Each find pays once, ever: XP, gold, an item in the bags, and a Chronicle page. With full bags
  it waits: "Your bags are full. Make room, then search again."
- **Quest book: a fourth tab, Discoveries.** It shows each zone's finds found out of total, with the current zone first. The
  found ones are listed by name and text; the rest only as "3 more lie hidden in Khaven."
- **New things to find:**
  - 4 gear pieces: the Poacher's Oilskin Hood, the Outrider's Hooked Knife, the Silent Pilgrim's Echo-Jar (neck) and the
    Leviathan-Bone Harpoon.
  - 4 healing herbs: Moonbell, Widow's-lamp, Frostbell and Last-light.
  - 7 documents, among them the Acorn Oath from Oakhaven's chapel book, the Khaven sexton's Low Row, a Sandthrone soldier's
    letter home and a cultist's doubting letter.
- **The look** is small and subtle: metal glints, pages are pale, herbs glow faintly, and nothing wears a marker's glow. A
  word in the prompt dresses the spot: a camp gets a cold fire ring and bedroll, a stump is hollow, rocks flank a cleft.
- **Khaven** has a beacon mound east of the walls (a new `Beacon mound` shape), the only raised ground in the zone, with the
  old beacon's ring on top.
- **Save format 7:** a `discoveries` list of found ids, with a 6 → 7 SaveMigrator step (`AddDiscoveriesMigration`) that adds
  only the empty list. Chris's save was backed up first (`work\save-backups\20260930-1739-before-format7`). The step was run
  read-only on a copy of it: all 26 fields came through byte-identical.
- **Built after the maps are rendered**, each prop on its own position-keyed random stream, so the maps and the rest of each
  zone's layout are unchanged.
- **Placement rules**, tested: reachable on foot from the start; not in a building, water, a camp, on a road, a quest prop, an
  exit or a spawn; 4 m or more off roads, so walking past never raises the prompt; clear of crags' scree.
- **First build:** the Peaks' Goat-Path Camp sat on a shelf above the waystation that you couldn't walk to. It moved down to
  the planner's checked spot.
- **Tests:**
  - EditMode `DiscoveryLogTests` (8), plus the 6 → 7 migration in `SaveMigratorTests`.
  - PlayMode `DiscoveryTests` (5): a lookout by walking on and paying once; a cache paying XP, gold, its item and its page;
    the key and chest; save, load and reload never paying twice; the book's counts, and no map mark.
  - PlayMode `SecretPlacementTests` (2): every secret placed by the rules, and ids, keys, items and pages all resolve.

## 2026-09-30 — Crowsfoot Hollow goes deep (Chris: "the cave should be deep and the first foray into dungeon crawling")
- **The first dungeon.** Crowsfoot Hollow now runs 114 m from its mouth and 16 m down, under Oakhaven's north edge and into
  the hills (layout in `WORLD_ZONES.md` "Caves you walk into"):
  - the upper hollow and the deserters' camp, as before, then the low passage west;
  - **the Drop:** a steep descent, eleven metres down, with plank treads pegged across it and a rope rail on posts;
  - **the Store Caves:** the stolen stores stacked along the walls, the Quartermaster's desk with his ledger and a candle, a
    brazier, a side way choked with fallen rock;
  - **the Deep Stair:** a winding passage down another three metres;
  - **the Echoing Hall:** 18 m across and 9 m high, stalagmites and stalactites, braziers, and Caddock's throne at the far end
    with the plunder beside it.
  - Torches all the way down; below 8 m a faint blue-green glow of fungus on the walls, and puddles where water seeps.
  - A few metres in, the cave names itself on the banner: "LEVELS 3-5" over "Crowsfoot Hollow".
  - The maps don't draw the cave's camps over the hills above them; the "Crowsfoot Hollow  3-5" landmark stands for them.
- **Five encounters, getting harder:** lookouts (3), the camp (3-4), sentries on the Drop (3-4), the Store Caves with
  **Quartermaster Hesk** (a new GAME-ONLY elite, level 4), the Deep Stair watch (4-5), and Caddock (now elite level 5) with
  three guards (4-5). The band comes back in 5 to 15 minutes, not 90 seconds, so the way stays clear while you go on down
  and walk back out.
- **Loot:**
  - Caddock always drops **Caddock's Tin Crown** (head, rare).
  - The Quartermaster's strongbox is a locked secret chest; its key hangs on a nail on the Drop. Inside: company silver,
    **Hesk's Shuttered Lantern** (off-hand, rare) and a Chronicle page, **By Order of the King** (Caddock's orders, copied fair
    by Hesk: "The crown is not tin.").
  - The deserters drop filed company badges, and Hesk his stores. New loot tables for the band.
- **The Tin Crown** is now level 4 (still offered from level 3) and pays 190 XP and 25 gold; its text sends you down to the hall.
- **How it's built:**
  - The passage plan gained a floor drop per row. The floor is its own mesh (earth, collider, navmesh) with a lip onto the road.
  - The ground mesh leaves out every triangle inside the passage; the rock shell covers the hole.
  - The knoll's crest rocks stand only where the passage runs near the surface.
  - The navmesh reaches past the zone's edge to follow it.
  - Braziers, the desk, the crate stacks and the plunder are solid now: you and the deserters walk round them.
- **Standing underground** (`ZoneBuilder.StandAt`): camps and their respawns, secrets, and a save made in the cave all stand on
  the passage floor, not on the hill far above. A key or a page in a cave hangs on the rock itself. Nothing is used (E)
  through the rock, from the hill over the Store Caves or the other way. Rain and falling leaves stop anywhere underground.
- **Found by the tests and the tour, and fixed:**
  - The ground's cut bulged out in front of the mouth and left a 2 m gap between the floor's lip and the road, so nothing
    could walk in. The cut now stops at the mouth.
  - The fall-out-of-the-world catch (anything under y -5 goes back to the village) caught you on the way down. It now sits
    5 m under the deepest cave floor. The save's sanity check allows positions down to y -60 for the same reason.
  - The tour's own character was chased down the dungeon; the band now holds still for the pictures.
- **Tests:** `CaveTests` grows to 8. It checks walkability leg by leg from the road to the hall, and adds four tests: it runs
  deep under the northern hills; no land shows inside; you can stand on every floor down to the hall; every camp stands on
  the passage floor, Hesk included. `HollowQuestTests` checks the new level. `ZoneContentTests` finds a camp where it
  stands (a cave camp on its floor).

## 2026-09-30 — The zones grow (Chris: "zones do need to be bigger with more places and secrets to explore")
- **Bigger:** Oakhaven 260 → 380 m, Khaven 240 → 340 m, the Shattered Peaks and the Ashland Rim 260 → 360 m. Every village
  core keeps its place. The exits, the roads to them, the arrival points, the creeks, the Wasting's curtain and the Peaks' and
  Rim's entries moved out with the edge.
- **New places in the new ground**, each a landmark with props and a canon label:
  - Oakhaven (11): Crowsfoot Ridge over the dungeon, the Mastwood, Moss's lodge, the Old Fold, the Bound Stone, the Cider
    Barn, Withy pool, the Old Barrow, Hollin farm, the Hallow's Creek milestone, the Watchtower.
  - Khaven (10): the Gibbet Crossroads, the Corpse Road and its bridge, the Plague Pit, the Drowned Fields, the Carrion
    Heights and the Charnel Barrow, the Old Bound Wall and the Fallen Watch, the Listener's Hut, the Hush.
  - The Peaks (7): the Signal Tower, Goatherd's Shieling, the Sealed Adit, the Cold Tarn (the Peaks' first water), the
    Listening Shrine, the Avalanche scar, the Broken Post; and two inner scarps that keep it a pass.
  - The Ash Rim (9): Wain's Rest, the Last Orchard, the Drowned Leviathan, the Ash Pit, the Walled Mouth, the Fraying, the
    Silent Statue (a new `monolith` prop: a robed, hooded figure of pale stone, ten metres tall, one lathed body with nothing
    in its hood but shadow), the Hunters' Knoll, the Reach-Stones.
- **18 new camps**, among them four named elites: Old Whitefoot (Oakhaven, 3), the Pale Reckoner (Khaven, 7), Old
  Scree-Tusk (the Peaks, 8) and the Ash-Deacon (the Rim, 10). New loot tables for hounds, the brood, the deacon, the toll
  guards, the captain and the sexton.
- **14 new secrets** (Oakhaven 5, Khaven 3, the Peaks 3, the Rim 3), with five new Chronicle pages (a letter from Hallow's
  Creek, the Watch Book, the Listener's daybook, the foreman's notice at the sealed adit, the waybook at Wain's Rest) and a
  Leviathan-Tooth Charm.
- Groves, fields, tall grass, clearings and critters fill the rest. Crowsfoot Ridge keeps 8 m and more of rock over the dungeon.
- **Villagers** keep their work and walks within 125 m of the village, so the core stays busy.
- **Loading stayed fast.** A 380 m zone's 3072 px ground paint took 20 s on one core; it now paints its rows in parallel, and
  the grass's openness test caches the roads' boxes and the buildings. Zones load faster than the smaller ones did before:
  Oakhaven 5.6 s (was 8.8), Khaven 3.7 (7.3), the Peaks 3.2 (7.1), the Rim 1.6 (3.8), in the built game. Each build logs its
  time by phase.
- **The maps:** about 4 px a metre (up to 2048), and a bigger zone's places list goes into two columns.
- **Data:** landmarks carry `canonStatus`.
- **Found by the tests and the tours, and fixed:**
  - A patch put a comment before the ground's material assignment and drew every zone's ground magenta. It never shipped;
    `ZoneContentTests` now checks that the ground wears its painted material.
  - Khaven's flood pools were asked for 0.55-0.6 m of water; a pond is always at least 0.8 m deep.
  - The Silent Statue was a two-metre cliff scaled up, and its rocks floated apart. Stacked primitives then read as a column
    with things on it; it is now one lathed figure.
  - The Picket's Skim sat west of the Signal Tower, in ground closed off by the tower, its rock ring and the woodpile (a
    navmesh island). It moved to the tower's east side, where the way up arrives. The placement test also tries the ground round
    a find now, the way you'd walk up to it.
  - A quest-giver test met a villager passing by. The test now has passers-by step clear first.
  - Two throwaway diagnostic tests run in the validation copy loaded and re-saved Chris's real save: the editor's save folder
    is the game's. Only the save's format and height changed; it is restored byte for byte from the backup taken before
    format 7. An editor test run now never touches the real folder unless a test points it there.
- **Tests:** all the zone tests (content, exits, travel, water, secrets) run on the grown zones.

## 2026-09-30 — The Verdant Shore: a truly lush fifth zone, levels 11-13 (Chris: "next zone we need a truly lush zone as pictured here")
- **Chris's call** (with lush style references, style only): a fifth zone after the Ash Rim, the canon **Verdant Shore** of Book 3,
  so the level cap rises to 13 (talent points stop at 10).
- **The lush art, built for it and shared by every zone:**
  - Painted undergrowth (`PlantField`): ferns in the shade of every trunk, in the woods and along the forest edge; broad leaves along
    the banks; reeds at the waterline; flowers in drifts of one colour; glowing flowers after dark. Each biome its own mix: Khaven's
    ferns withered brown, nothing on the ash.
  - **Dead trees no longer look like umbrellas** (Chris): a tapered, bent bole on a root flare, limbs at different heights, crooked,
    tapering to points, branches off their length. The Hush's great dead tree is a tall old snag.
  - Giant trees (28 m, buttressed, crowns of forty leaf clusters), Keeper treehouses grown into them (a round house with a lit
    porch, a platform and a stair up the trunk), waterfalls (rock face, sliding sheets, foam, mist), mushroom clusters (red,
    glowing teal, pale; giant ones), a fallen giant, rope-and-post footbridges, a `giant` grove kind, the `salt` paint.
  - New creatures and people: the Veridian Keeper (bark over heartwood, sap-light in the seams, a leafy crown; withered and
    violet-lit when corrupted), a great forest stag, a forest spider, a bramble-thing, and the Silent Pilgrim's outfit.
- **The zone** (360 m, designed by a zone agent from Book 3 and checked against the engine's own numbers): you arrive on the Ridge of
  Long Shadows and see the Verdant Ocean below; the Ash-road winds down to **Rootfast**, five treehouses round a lamp-lit green; the
  Veridian Temple under its root-arcs; Mossveil Falls and its pool; the Mistmere; the Whispering Glade of glowing caps; the Fern
  Hollow; the Fallen Ghost-Oak; the Salt-Flats on the coast; and the Pale's touch shown early at the Greying and Palemist Hollow.
  Nine camps (two elites, Greyheart and Old Ninebranch), five Keepers with their own talk, a Silent Pilgrim and a glass-ship scout,
  twelve quests (the breadcrumb from Grohl, a main chain of four, seven side quests), four pages, five secrets, a new faction, 17
  items, 9 loot tables, 2 vendors. The Ash Rim gains the Old west road, its exit and signpost.
- **Found by the tests and the tour, and fixed:** the Wending (a 10 m river) was carved straight through its pool's rim, so the pool's
  edge floated: a creek never cuts under a pool's rim now, and the river starts just past the pool's edge; the lookout above the
  falls stood on a 66-degree wall: a stepped way up the ledge's east side; a pool's shore carving no longer flattens the ledge
  behind its waterfall.
- Zones load in 2-8 s (the Verdant Shore 7.6 s: its 1,100 props and giant groves).

## 2026-10-01 — The Root-Mother's Deep, and Crowsfoot's mouth hidden in the hill (Chris: "can you also create a dungeon for the new zone? the dungeon in the first zone kind of stick out as just a rock. it needs to be built into a mountain or something and kinda hidden..no so obvious. they are bandits")
- **Crowsfoot Hollow's mouth** is a slot in a cliff face now, not a rock on the grass: two cliff scarps either side, the Crowsfoot
  brow raised over it, the North road ending short and a track bending through a pine thicket and round boulders to a mouth you
  only see from the last bend.
- **The Root-Mother's Deep**, the Verdant Shore's dungeon (GAME-ONLY, under the CANON Temple): a second cavern plan (`variant: 1`)
  grown through earth and root. The Temple's root-stair down into the Root Gallery (living root columns), the Sap Well (a pool
  of emerald sap against the wall, votive stones, drips from the roof), the Cold Stair, and the Heart: the Root-Mother, a vast
  knot of root with a hollow face and sap-light in it, and the cold lodged in her root (a black rod, hoarfrost, violet light).
  Sap veins for light instead of torches; no knoll shows on the land over it. Six camps down it (briars, withered Keepers,
  mist-walkers) and the Hollow Root-Warden (elite 13, drops the Root-Warden's Crown). The finale quest *The Root-Mother's Deep*
  (`main.verdant.5`), a fifth page, a Pilgrim's abandoned pack (a secret). Three new PlayMode tests (`RootDeepTests`).
- **Found by the tests:** the sap pool sat in the middle of the passage and cut the way (and a camp) off the navmesh; it hugs
  the east wall now and the way through runs along the west.

## 2026-10-01 — Every trade has a day, and the goods go round (Chris: "the hen maiden/mother should feed the chickens in the morning. water during the day. collect eggs. each profession should have a daily ai job schedule with tasks ... she should take some eggs to the merchants to sell. some home to eat. some eggs to the inn for food for the village, etc. all job are intertwined.")
- **Daily schedules** (`VillageWork.cs`): each trade's day is shifts (where to be, hour by hour) and errands (once a day, from an
  hour: pick something up at one place, carry it to another, hand it over, say so). Villagers follow the shift for the hour and
  run a due errand first; a trade whose errand needs a place the village lacks skips it.
- **The hen-wife:** opens up and feeds at first light; eggs to the inn's kitchen at 08:24; water from the well to a new pan by the
  ramp at 09:36 and 13:30 (the hens come to drink; the water dries over four hours); dinner at home; the afternoon feed; eggs to
  the produce stall at 15:24; the last eggs home for the pot; the hens at dusk as before.
- **The goods go round:** barley from the fields to the mill, flour to the bakehouse and the stall, the first loaves (the baker is
  up at 04:36) to the inn, logs to the woodyard and firewood to the inn and the forge, the hunter's hide to the tannery and his
  hares to the inn's pot, pelts from the snares, belts and ironwork to the stall, herbs to the stall and marigold for Mira, water
  from the well for every house, a loaf for Mum. Every load shows in their hands: a basket of eggs (as many as were laid), a
  bucket, sacks, a tray of loaves, logs on the shoulder, a hide, a bundle of herbs, a hare on a string, a crate.
- **Intertwined:** deliveries fill the village's stock; whoever is at the place answers; the trades talk about each other's goods
  ("The miller's flour came in. Thin stuff, but it rises."); and the merchant sells **Oakhaven eggs** (a new food) while the
  hen-wife's eggs last at the stall.
- Tests: `VillageWorkTests` (the schedules) and `VillageErrandTests` (eggs to the inn and sold on by the merchant; water to the
  pan; barley to the mill). The capture tour adds an errands line-up (`99-errands-lineup`).

## 2026-10-01 — A review of the deep and the trades' days, before publishing
A read-only review by five reviewers, each finding checked by a second who tried to refute it, found real defects. Fixed:
- **Grey wolves were about to become ash hounds:** a new name rule for the Verdant creatures ("Grey ") caught every Grey wolf in
  Oakhaven and Khaven. Removed.
- **The hens' water never showed:** the disc sat inside a solid pan. A shallow pan, the water lying in its top.
- **The finale never gave its page** (`rewards.documents` is a list), its kills could be made at the surface camps (the deep's
  camps have their own tags now), and its first step completed from the barrow over the Gallery (a place down a cave is
  visited from down in the cave).
- **The Root-Mother:** her roots "into the floor" went 14 m up to the mouth's level; her upper knot, face and eyes were above
  the roof at the Heart's tapered end (the Heart stays tall to its back wall now); the salted cold came back after 90 seconds
  (it stays gone, the pale root stays).
- **The trades:** the village's stock is the day's (cleared before dawn); a delivery home says its line at the door, then goes
  in; the hen-wife speaks of eggs at the inn or the stall only once they got there; nobody could draw water with a collector
  seven metres from the well (villagers keep 7 m from enemies, not 12, and a blocked errand says so).
- **Tests:** three flaky spots in `VillageErrandTests`; `run_focus.ps1` (a fast filtered run) registers the content as the full
  run does.

## 2026-10-01 — The painted style pass (Chris: "keep going with the painted style pass")
- **Buildings:** hand-painted plaster, thatch in ragged layers, slate shingles, coursed masonry and planked timber, on walls and
  roofs whose textures now tile per metre; eaves with fascia and rafter ends, ridge caps, framed windows with sills and shutters,
  plank doors with iron bands, masonry plinths and capped chimneys.
- **Rock:** cliffs, crags and boulders in bedded painted rock with lit tops (a new shader, `Crulanda/PaintedRock`).
- **Colour:** Oakhaven under a blue sky on a green meadow; the Peaks crisp and blue; Khaven and the Ash Rim keep their moods
  with more depth.
- **Props:** chunky, hand-made fences, carts, lamps, wells, stalls, barrels, crates, woodpiles, bridges and signposts.
- **How:** drafted, pre-checked by agents that rendered and looked at the textures and recomputed the geometry, restaged as
  verified patches, then applied a part at a time with the tests and a capture tour after each. Found on the first tour and
  fixed: roof textures smeared up the slope (the roof's UV used |z| + y, which is constant up a 45 degree roof); the rock's
  joints made cliffs read as dry-stone walls.
- The zone layouts are unchanged (the random stream is untouched) and so is everything gameplay stands on.

## 2026-10-01 — Painted masonry, and the out of work at the inn
- **Built stone:** towers, curtain walls, the toll gate, the keep, crypts, ruins and wayshrines in painted coursed masonry (turned
  drums with plinth and corbel courses, slate spires, coping); headstones and waystones in painted rock.
- **The out of work drink at the inn** (Chris: "have unemployed npc show up at the inn and drink till gone or passed out"): the
  drinkers, and any trade a village has no workplace for, are at the inn from 11:00, a tankard in hand, drinking the day's cask
  (the merchant brings a fresh one each morning). When the ale is gone they go home; past their limit some pass out over the table
  and the rest reel home to sleep it off.
- **The first batch of the visual review's worklist** (four reviewers looked at every capture of the published painted pass and
  ranked what still falls short; `tools/wip/painted/visual_review.md`): Oakhaven's haze turned to blue air; storms that darken
  the day; night with firelit windows and a moonlit blue base instead of bleached white on mud; broadleaf crowns built of leafy
  lumps instead of one brown ball; a greener, denser meadow whose tufts fade out with distance; hedges and haystacks with shape;
  gable ends that are wall, with barge boards and a tie beam, instead of roof.
- **The second batch of the worklist:** the Ash Rim's ground is drifted ash over broken crust instead of paving, its flakes soft
  and pale; the land carries on past the zone's edge at exits (grass, ferns and flowers thinning out over the backdrop's near
  slope) and the waystone is a leaning carved stone with a lantern house; ruins have broken silhouettes (stepped stubs, a gable
  standing alone, rubble); the Peaks' steep ground reads as bedded rock and scree with crags seated in aprons; cave walls are
  painted rock (Crowsfoot) and earth packed with roots (the Root-Mother's Deep), with dripstone and ledges, and the deep's capture
  shots stand in the right places.

## 2026-10-01 — Trades begin, the village grows, and your weapon shows (Chris: "we need to gather ore, lumber, herbs etc and sell or refine. also have a profession for the character"; "each npc has their own house and each profession has their own workshop"; "improve armor and weapons and each has a different visual appearance when worn")
- **Trades, the groundwork** (BUILD_PLAN step 2): the save moves to format 8 (what you have learned of the trades, and room for
  trade bags), safely migrated from format 7. Nineteen materials (ores, logs, herbs, charcoal, flour, salt, vials), a miner's pick
  and a woodcutter's hatchet to buy from Ama Rusk or the smith, and the **Trades window (K)**: Mining, Woodcutting and Herbalism
  free to everyone, Cooking for everyone, and two crafts (Blacksmithing and Alchemy) to choose between. Gathering itself arrives
  with the next step.
- **The village grows** (step 1): the Carder farmhouse and the Crisp cottage; **Maud Tanner's leather shop** by the South road,
  apart from the tannery yard, where she now works 9-12 and 14-18; **Lisbet's drying hut**, herbs on the racks; **the Golden Cask's
  kitchen** lean-to with a range and a back door; **the game rack** at Moss's lodge; and every house carries its name ("Knock ·
  Tanner house"). Who lives where follows in the households step.
- **Weapons and shields show in your hands** (loot step A1): every weapon and shield has its own built shape (short and arming
  swords, falchions, knives, cleavers, hand and bearded and crescent axes, clubs, flanged maces, war hammers, staves, polearms;
  bucklers, round, kite and heater shields; lanterns and censers), coloured by where it comes from, held a little larger than life
  in the painted style; better quality shows in the trim and a glowing accent. Slung on the back when you swim. **Empty hands show
  empty**: a new character starts with nothing in hand until the Trailblade is equipped.
- **Found by the first full check and fixed:** the Crisp cottage's door point sat in a pocket of navmesh nobody could reach (every
  house's door point now stands on its step); held weapons drawn life-size read as twigs (now 1.35x, shields 1.15x).
- How it was built: each piece on its own branch, reviewed through three lenses with every finding refuted or confirmed by a
  second reviewer (21 defects fixed before merging), then the full tests, five zone tours and a wardrobe line-up of every weapon.

## 2026-10-01 — Homes, gathering, armour you can see, and buildings that sit in the land (Chris: "each npc has their own house"; "we need to gather ore, lumber, herbs etc"; "each has a different visual appearance when worn")
- **Everyone has a home** (BUILD_PLAN step 3): every Oakhaven villager lives behind their own household's named door (15
  households; Khaven 5). Families share a house (the Tanners: Maud, Fen and Nettie), farmers live with their hen-wife, Aldo Crisp
  has his cottage and Garet Moss his lodge, where he goes to bed earlier. Knocking gets an answer: someone home with wares or
  quest business opens the door; otherwise the household tells you where the head of the house is, or that they're abed.
- **Gathering in Oakhaven** (step 4): copper seams on the rocks (and rich ones in Crowsfoot Hollow), windfall oaks to cut and
  yarrow to pick. A work bar, a skill that rises as you work, nodes that rest and come back (even across a zone hop), and a
  village that notices what you sell: Vell talks about the ore, the merchant about herbs. A shot of every node in the tour.
- **Armour you can see** (loot step A2): every head, neck, shoulder, chest, hand, leg and foot piece has its own built shape
  (caps, hoods, coifs, kettle hats, barbutes, masks, circlets and crowns; torcs and pendants; mantles and pauldrons; tunics,
  jerkins, hauberks, coats, cuirasses and robes; gloves and gauntlets; breeches, leggings and greaves; shoes, boots and
  sabatons). Better pieces gain surcoats, edging and badges. Armour recolours what it covers, hides or tucks the hair under a
  helm, and gives the bare body back exactly when it comes off. The wardrobe capture now lines up sets, helms, chests, limbs
  and palettes (shots 04-12).
- **Buildings sit into the land** (visual worklist items 10 and 11): houses, the mill and barns stand on a stepped stone
  footing that climbs and terraces with the ground, with a threshold and steps at each door. The Golden Cask and Khaven's
  Cracked Hearth are hero buildings: a jettied upper front, dormers, window boxes, a porch with a hanging lantern and a sign
  twice the size. Vell's smithy is a slate-roofed hearth house with a lean-to over the anvil (glowing coals, hood and chimney,
  bellows, quench trough, racks and a grindstone).
- **From Chris's look and the full check:** copper ore was round orange lumps with green beads ("what are the green
  peas/circles on the ore?"); it's now faceted red-brown ore with the verdigris pressed flat on the face. Yarrow drew smaller
  than the meadow's own wildflowers; the herbs are now knee-high clumps (yarrow's flat white heads over feathery leaves,
  comfrey's nodding bells). Mourner's cap drew yellow flowers but Wenna's quest calls it a black-gilled mushroom, so it is now
  a cluster of grey caps. One armour test was wrong, not the armour (it skipped the whole body because the test figure's own
  name starts with "Gear").
- Tests: EditMode 240/240 (after the test fix), PlayMode 108/108; five zone tours and the wardrobe.

## 2026-10-01 — Trade bags, the innkeeper, gathering in every zone, and the loot ledger drafted (Chris: "have the leatherworker make bags for professions by quest (gather leathers) or just buy them outright from him"; "each profession has their own workshop"; "start building a database of loot")
- **Trade bags from Maud** (BUILD_PLAN step 5): four bags, the Simples-wallet (herbs, 6 slots, 12 gold), the Log-sling
  (timber, 6, 16), the Larder-scrip (eggs, cheese and the larder, 8, 20) and the Ore-poke (ore, bars and charcoal, 8, 24).
  Buy them from Maud Tanner, or earn the wallet with "A Wallet for Simples" (bring her three grey wolf pelts). Use a bag to
  wear it; its slots show as their own labelled row under your bags, and what you gather goes into it first. The coin goes to
  Maud's household. Hides are now materials Maud works, and she notices when someone sells good pelts.
- **The Golden Cask has an innkeeper** (step 6): Hob Linden is up before dawn and behind the bar till late, selling brown
  loaves and Harrow cheese. The hen-wives hand eggs over at the kitchen's back door, the baker brings the first loaves, the
  hunter his hares, and Hob comes out to take them; dinner goes out to the tables. Every trade works its own workshop:
  Ama, Tamsin and Hedda keep their own stalls, the tanned hides go from the yard to Maud's shop, herbs to the drying hut.
- **Gathering in every zone** (step 8): ten ore seams, eight windfalls and ten herbs each in Khaven (bog-iron, black pine,
  mourner's cap), the Shattered Peaks (Adit iron, stone pine, tarnwort), the Ashland Rim (cinder ore, ash-snags and single
  cinder-thistles: thin pickings) and the Verdant Shore (Veridian ore, ghost-oak, dewfern, and rich seams down in the
  Root-Mother's Deep). Every node was placed by a script that checks it against roads, water, buildings, camps, secrets and the
  caves, and walks to it from the zone's start.
- **The loot ledger, drafted** (loot step A3): 104 named items across the five zones and the world, each with its look, its
  stats on the gear curve, where it drops and a line of flavour; boss and elite drop lists, two sets, and the roll rules (one
  named item per kill, two per elite, luck capped). Nothing drops yet: the wardrobe shows them on mannequins (shots 13-18) so
  the looks can be judged first.
- **Smaller:** herbs drawn a third larger than life (yarrow's head all white); the hauberk's surcoat wider, slit at the hem
  and belted.
- **Found by the full check and fixed:** one workshop test waited for Lisbet to choose her drying errand of her own accord and
  missed the window once in the full run (never alone); she now chooses at once when the test moves the clock.
- Tests: EditMode 285/286 (one skipped until crafted gear exists), PlayMode 115/116 with the one failure fixed (its fixture 6/6
  after); five zones toured with a shot of every node; the bag UI shots 19-23 and the named-loot wardrobe 13-18 viewed.

## 2026-10-01 — Your coin feeds a family, the first thing to make, and loot that shines (Chris: "so he can buy food/lumber for his family"; "gather ore, lumber, herbs etc and sell or refine"; "start catering to the people who love loot")
- **Purses and spending** (BUILD_PLAN step 7): every household has a purse. What you pay Maud for a bag reaches the Tanner
  house, and her family spends it where you can see: Nettie fetches a loaf from the baker's stall, Fen carries firewood home
  from the woodyard, and the chimney smokes again. Short of coin they go without and say so ("Can't stretch to firewood
  today."); with no trade bag sold, the Tanners' hearth is cold at dusk two days in three while the rest of the village smokes.
  After 18:00 the coin waits for tomorrow ("That's tomorrow's fire."). The out of work still drink with empty purses.
- **Stations and charcoal** (step 9): "Work at the forge" at Vell's smithy (at night too, with nobody there), the bakehouse
  oven, Lisbet's bench and the Golden Cask's kitchen range and hearth all count; Khaven, the Peaks, the Ash Rim and the Verdant
  Shore have their own field anvils, herbalist's benches and cookfires. A recipe pane in the Trades window (Make, Make all) and
  the first five recipes: charcoal from each tier's wood, into your ore-poke if you wear one. Recipes teach every time while
  orange, half the time yellow, one in ten green, never grey.
- **Loot feel** (loot step L1): loot is rolled when a mob dies and lies on the body. Bodies show what they hold: a white
  twinkle for coins and junk, a green glint and column for uncommon, a blue beam for rare, a purple beam with a turning ring for
  epic, bright enough to read at night. E opens a loot window (Take all [E]); what doesn't fit stays on the body instead of
  being lost. Tooltips compare against what you wear ("+9 weapon damage" in green, losses in red, "An upgrade"), with green
  arrows on better pieces in your bags, at vendors and in the loot window, and a coloured call-out for every rare or epic.
- **Smaller:** the rarer herbs (tarnwort, cinder-thistle, dewfern) drawn a third larger like yarrow; vendor item names in dark
  ink on the parchment (common names were faint grey).
- **Found by the full check and fixed:** Khaven's Cracked Hearth had a front door the navmesh closed (the walls' padding
  and the agent's width left a one-voxel thread that broke at that inn's angle), so nobody could walk into its taproom; the
  jambs' colliders now stop short of the opening (nothing visible changes). Two tests were wrong, not the game: one expected
  "Work at the fire" with Mira standing beside the hearth (talking to her rightly comes first), one put the "grey" skill-up
  case at 20 points over instead of 30. Dewfern's fronds ran into the ground (their pitch was the wrong way round); it now
  rises and arches, silvery blue-green with lit dew.
- Tests: EditMode 310/312 (one skipped until crafted gear exists; the one failure was the wrong grey case, fixed), PlayMode
  133/135 (both failures fixed; their fixtures 6/6 after); all five zones toured with a shot of every station, then Khaven and
  the Verdant Shore toured again after the door and dewfern fixes; the HUD shots 24-25 (recipes, charcoal made) and the loot
  shots 01-04 viewed.

## 2026-10-01 — Cooking, hunting for hides, and named loot goes live (Chris: "blacksmith, alchemist, cook"; "this also means all animals are huntable for their leather"; "farm animals stay . not huntable"; "start building a database of loot")
- **Cooking** (BUILD_PLAN step 10): everyone cooks, at any fire: E at the Golden Cask's hearth or kitchen range, the bakehouse
  oven or a camp's cookfire reads "Cook at the fire". Ten recipes, from griddle bread and boar stew to venison pie, each better
  than what a vendor sells at its level. Wolves, hounds, mossboars and stags now drop meat, boar meat is a cook's material
  ("Sell junk" keeps it), and meat goes in the larder-scrip. Sell meat in the village and Hob and the drinkers talk about the
  pot.
- **Hunting** (step 11): deer and rabbits are game. They graze and wander, look up when you come near and bolt if you keep
  coming; sneaking (Ctrl) gets you close; a hit sends them off, and a badly hurt one limps and can be run down. No experience
  and no coin: E at the body reads "Skin the body" and gives its hide. Wolves, boar, hounds and stags give theirs the same way.
  **Hens, sheep, cats and crows can never be targeted.** The village does not flee from a deer. Fen the skinner sells salt, and
  Maud's other three bags can now be earned: the log-sling for three hill-deer hides, the larder-scrip for five coney skins,
  the ore-poke for three boar hides.
- **Named loot is live** (loot step L2): the 104 named pieces drop from their mobs, elites and bosses. Each boss gives one
  piece of its list you do not hold yet; a unique you hold is never offered twice; set pieces count toward their bonus;
  tooltips show Unique, the effects, the set and where it comes from; five merchants sell a named piece each; rare gear from
  elites can come out epic. A new character starts with the Trailblade in hand (existing saves are untouched).
- Tests: EditMode 319/320 (one skipped until crafted gear exists), PlayMode 150/151. The one failure,
  `HuntTests.Game_bolts_and_sneaking_gets_closer`, is intermittent (it failed in the full run and once in its fixture, and passed
  twice after): at some spots a rabbit that should bolt stands and watches instead. Published with that known; the fix follows.
  Oakhaven toured only (Chris: skip the tours on rounds that don't change the world's look); the HUD shots 26-27 (cooking) viewed.

## 2026-10-01 — A blacksmith and an alchemist, the Armoury, and three loose ends (Chris: "have a profession for the character. blacksmith, alchemist, cook"; "start building a database of loot")
- **Blacksmithing and the two-craft rule** (BUILD_PLAN step 12): take up Blacksmithing at a forge (E at Vell's smithy; the
  Trades window's craft page has a Take up button). Five smelts turn ore and charcoal into bars, and 21 pieces can be made and
  worn, from the Copper-shod cudgel up to a rare capstone; crafted gear sits on the same curve as found gear and stays under
  the named rares of its level. Two crafts at most: a third is refused until you Forget one (with a confirm; the skill is lost).
  With the trade's own person near and awake, each piece takes one second instead of two ("Brannoc Vell works the bellows for
  you."), and he has words for it.
- **Alchemy** (step 13): take it up at a herbalist's bench (Lisbet's drying hut, Wenna's in Khaven). Five potions from the
  herbs of each zone and a bought vial, among them the new Tarnwater draught.
- **The Armoury** (loot step L3; save format 9): a fifth tab in the quest book lists every named piece by zone and source:
  silhouettes for what you have not found, grey names once you know the source, full colour with its tooltip once found, with
  a found / total count per zone. A "NEW LOOK" toast the first time an appearance enters your bags. The purple piece of a boss
  is now certain by the 10th dry kill in a dungeon and the 25th outdoors. Chris's save was backed up first
  (`save-backups/20261001-2315-before-format9`); it migrates on its next save, with what he holds marked found.
- **Rare and epic gear glows on the body:** the accents now carry their palette's colour at full strength (rare a clear gleam,
  epic brighter and pulsing); uncommon and below stay unlit.
- **No talking through walls:** talking needs a clear line between heads, so Mira is no longer offered from outside the Golden
  Cask's wall; stall counters, the bar and doorsteps do not block it.
- **Herbs follow the ground:** a clump on a slope leans with it and sits on it, instead of floating on the downhill side.
- **Game animals bolt only to ground they can reach** (the intermittent rabbit of the last publish).
- **Found by the full check and fixed:** two of Khaven's mourner's-cap clusters on the creek's bank were sunk until their small
  caps were buried (the new slope rule sank the cluster's plane; a cluster on stalks now settles by its stalks' feet and its
  gills). Three new tests searched the save file for text that is stored escaped (the game saved correctly); fixed, and a
  check that passed without testing anything is now real.
- Tests: EditMode 341/341, PlayMode 154/158, the four failures fixed and their fixtures 11/11 after; the Peaks, Oakhaven and
  then Khaven toured; the HUD shots 28-33 (crafts), the Armoury shot and the quality ladder viewed.
- **Chris's notes from playing the earlier builds are in `PLAYTEST_NOTES.md`** (14 by the end of the night): the bandit camp
  too close, elites and the Bandit King too easy, mobs not social, the sheep, the ore's green, the lumber trees, cats' tails,
  sun and bloom, cave lighting, icons, blocky characters, pale colours, and a PDF of everything built. None of their fixes
  is in this build except what the rounds above already held; they follow.

## 2026-10-02 — The bandits pushed out, mobs that pull together, elites worth their loot (Chris, playing: "the bandit camp is way too close to the village. i asked before to expand the zone"; "some mobs need to be more social. i can pull them easy 1 at a time"; "elite was too easy for the loot obtain"; "bandit king way too easy for the loot obtained")
- **Oakhaven grows to 560 m and Crowsfoot Hollow moves out:** the cave's mouth is now 272 m from the green (it was 100 m),
  hidden in the west heel of Crowsfoot Ridge along the north edge and facing away from the village; the North road and the
  Crowsfoot track lead to it round the ridge. Between: the farms and the north fields, then wolf and boar woods, then the
  hills. Every camp in Oakhaven now stands 125 m or more from the village's houses. New ground: Carder's field barn,
  Thornshaw, the Carter's Rest, Lark Hill, Sallow Bottom, the Bound wood, ten woods and three secrets. The village itself did
  not move. (The other four zones still have camps within 120 m of homes: Chris's call whether to grow them too.)
- **Social pulls:** wolves and hounds come as a pack; deserters, cultists and the Concord shout and bring those in earshot
  (and the next camp of their kind) a moment later; boar and stags stay single. Sneaking still lets a careful player peel
  one from the edge of a camp. An elite always fights with its guards.
- **Elites are a fight:** five and a half times a normal mob's health, a harder hit, and a named heavy blow each that they
  wind up with a mark on the ground and a cast bar (step out of it, or Guard it); they enrage when low and call for help once.
  Caddock and the Root-Warden, the dungeons' end bosses, are harder still. Mira's heals and health now grow with your level.
  On paper: alone and careless you die; with Mira and care you win; two levels under, you don't.
- **Found by the check and fixed:** the reach tests asked for one path across the whole 560 m zone in a single search, which
  the pathfinder gives up on (the cave was fine, walkable leg by leg); they now follow a long path on from where it stops.
  Lark Hill's map mark sat on its own secret; moved down the hill. The zone-exit test needed longer (Oakhaven now takes
  about 13 s to build). One social test recruited Mira while she stood in the Golden Cask.
- Tests: EditMode 369/369, PlayMode 167/173 in the full run, the six failures fixed (four were the tests) and their fixtures
  34/34 after; Oakhaven, Khaven and the Ash Rim toured; the fight shots (pack pull, shout, heavy blow, enrage) viewed.

## 2026-10-02 — The other four zones grow, and their camps move back from the homes (Chris: "yes. grow about 20%")
- **Bigger:** Khaven 340 to 410 m; the Shattered Peaks, the Ashland Rim and the Verdant Shore 360 to 430 m. The edges, the
  exits, the roads' ends and the creeks went out 35 m; every village stayed exactly where it was. The exits and the points
  where you arrive stand on their roads as before, 9 to 12 m apart. Coming over the mountains into the Verdant Shore, you now
  arrive on the crest of the Ridge of Long Shadows.
- **Camps back from the homes:** every camp that stood within about 120 m of a home moved out whole with its place, to 125 m
  and more. Khaven: the drowned graveyard with the Grey Sexton, the Sandthrone outriders, the picket at the Fallen Watch, the
  Deep Whispering Wood wolves and the Carrion boars. The Peaks: the Wolf pines pack and the rockhide wallow. The Ash Rim: the
  Fraying, the Weave-Eater brood and the Unwoven Flats eaters. The Verdant Shore: the Greying with Greyheart, the Mistmere
  reed-boars, Old Ninebranch on his knoll, the Fallen Ghost-Oak with its spiders, and the Antler Meadow with its stags.
- **Left by design:** the Sandthrone's own toll-gate and keep on the Peaks, the hollows by the Ash Rim's empty hunters' hide,
  and the Briar Way at the door of the Root-Mother's Deep. The Ash Rim's Wasting stays where it stands (its places line it).
- **Found by the check and fixed:** the tool that wrote the gathering nodes had dropped six crafting stations (Wenna Coyle's
  bench, the Pass-trader's anvil and cookfire, the Verdant Shore's ember-stone, bench and Hearth-Tree fire): back. The exits
  had gone straight out while their roads' last bends changed, leaving four exits and arrivals off the road (one arrival in
  the Peaks stood on the pass's steep side): each back on its road. The Fraying had gone out past the Ash Rim's Wasting into
  the unmade, where nothing walks: it now lies 21 m inside it, south-east, by the grey husks. A rabbit that came back in the
  Brook pond could stay in the water (a group circle over a pond missed the dry part six times running): it now finds the
  nearest dry ground. New tests hold every exit to its road, nothing in the unmade, and the creeks to their old line.
- Tests: EditMode 367/369 in the full run, then 372/372 with three new tests; PlayMode 169/173, the four failures fixed (one
  was the stations, one the rabbit, two the data) and their fixtures 10/10; the four zones toured, the Ash Rim again after
  the Fraying moved.

## 2026-10-02 afternoon — Felled lumber trees, joined limbs, less green ore (playtest notes 6, 15 and 5)
- **Lumber trees look chopped down** (Chris: "need to look chopped down"): an axe-cut stump with a pale face and darker
  heartwood, the axe's chips round it; the trunk limbed (short stubs with pale cuts, nothing poking up) and cut in two, each
  cut end pale; the lopped branches heaped low beside it. Chopping takes the logs; the stump, chips and branches stay.
- **Limbs meet their trees** (Chris: "a few trees are disjointed in the limb area"): the Verdant giants' branches stood up to
  a metre above their great limbs, and the dead trees' twigs floated off theirs; every branch now grows out of its limb.
- **Less green in the ore:** copper shows a hint of dull patina on one vein in three; the seam reads ore-red.
- Tests: EditMode 372/372, PlayMode 174/174 (a new test checks every limb in all five zones); all five zones toured.

## 2026-10-02 afternoon — Sheep and cats (playtest notes 4 and 7)
- **Sheep** (Chris: "looks like bugs on sticks"): a deep fleece of wool lumps, a dark wedge of a face with ears out sideways and
  a wool cap, short sturdy legs on hooves; some white-faced, now and then a dark sheep, big ewes and small. They graze nose in
  the grass, tugging at it, then lift their heads to look about, and nod as they walk.
- **Cats' tails** (Chris: "cats tails need to be more flexible"): a tail of six joints that hangs in a curve and sways when
  the cat stands (the tip flicks now and then), swings low behind it when it walks, and stands straight up with the tip
  hooked over at a trot.
- Tests: the hunting and village fixtures 14/14; Oakhaven toured, with a new line-up shot of the animals.

## 2026-10-02 afternoon — Icons (playtest note 11)
- **A picture for everything** (Chris: "can't tell what anything is"): 693 painted icons. Every item (each ore, bar, log,
  herb, hide, meat, food, potion, tool, bag, junk and quest item its own), every ability and talent, the trades, and gear by
  what it is in its own colours, the quality still on the border. On the action bar, in the bags and trade bags, on worn
  gear, in the Armoury, the loot window and the recipes, on the talent tiles and the trades list.
- Tests: EditMode 377/377 (a new test keeps every item, ability, talent and trade covered); the bag, loot, Armoury and
  trades fixtures 22/22; the HUD shots looked at.

## 2026-10-02 afternoon — High-fantasy colour, and a lantern under Khaven's sign (playtest notes 13 and 16)
- **Colour** (Chris: "need high fantasy not pale"): the gear's ten region palettes re-dyed in jewel tones (Oakhaven green
  and russet, the Sandthrone saffron and crimson, the Concord royal blue on white, Khaven deep violet, the Toll road sky
  blue...), a second and third dye for the material words, rarer cloth richer and deeper, poor drained grey. The classes and
  enemies, the Concord's white tabard and gold sigil, and every villager's trade clothes in madder, weld green, woad, ochre and
  heather. The village: painted shutters and doors (withered in Khaven, drained under the ash), bolder awnings with pennants
  on the stalls, bright produce and terracotta, window boxes in six colours, the Golden Cask's green sign in a gold frame.
- **The sign's light** (Chris: "looks like neon. it should probably be laterns or candles"): Khaven's inn sign lost its four
  glowing bars; a candle lantern hangs under it, and the pot's cracks only just glow.
- Tests: EditMode 378/378, PlayMode 173/174 in the full run (a villager walking home cut it fine; the fixture passed twice
  after); all five zones toured, the wardrobe and the village looked at; Khaven toured again with the lantern.

## 2026-10-02 evening — Real people: every person is a model (playtest note 12)
- **The smooth figure** (16:58: one skinned body for every person, knees and elbows; limb armour and held gear bending with
  them) was still "REALLY blocky" (Chris). He chose real models: the Quaternius kits (CC0), men and women.
- **People as models**: textured heads with faces, eyes and brows, real hair (short, parted, buzzed, long, buns) and beards,
  peasant and ranger outfits in each trade's dyes, the outfit's hood for the hooded trades and classes, skin tones and hair
  colours from the old palettes, women by name and trade (the hen-wives, Hedda, Maud, Lisbet, the Druid...). The Warrior
  and the Druid wear the ranger's leathers; Collectors and Wardens Concord white.
- **They move**: a real walk, jog and sprint matched to their speed, an idle that breathes, sitting at the inn, kneeling to
  gather, crouching to sneak, swimming with the face out of the water, talking with their hands, falling dead. Hammering,
  chopping, hoeing, kneading, drinking and slumping over the table use the old poses' arms on the new bodies.
- **Gear refit, not redone**: every weapon, tool, shield, lantern and armour piece keeps its design and sits on the model:
  helmets on the head, chest pieces on the chest, vambraces and greaves bending at the elbows and knees (slimmed to the
  model's limbs); armour dyes the clothes under it and hides the outfit's belts and bracers. Hats, masks, crowns, satchels,
  sigils and the hunter's bow stay; the old flat aprons, tabards, capes and ball hoods are gone from the models.
- **Rounder armour on the models**: chest plate, mail, coats, jerkins, tunics, robes and belts are built round (they were
  square to hold the old figure's cube torso), with their own meshes; the smooth figure keeps the old ones.
- **Carrying**: a villager with a load holds it: the right arm out to a basket, bucket, hide, herbs or game at the hip, both
  arms forward under bread or goods, a hand up to steady a sack or logs on the shoulder.
- Fixes found by the first full run: parts a model leaves off are unparented before they are destroyed (a villager caches
  its renderers as it is built, and coming out of a house touched a destroyed one); an elder's stoop and a sleeper's slump
  bend the back only on frames the figure's clips posed it (off screen they would have bent it again every frame).
- **True colours on the leathers**: the ranger outfit's green cloth has a bleached copy, so dyes come out true: the Warrior
  in his blue, the Concord's Collectors and Wardens in white, the Sandthrone outriders in sand, cultists in soot black,
  deserters in their faded sand, Caddock in near-black; the Druid and the hunters keep the green.
- Published 2026-10-02 19:37 (d618963): the models, rounder armour, carrying and true colours. Full run: EditMode 383/383,
  PlayMode 173/174 (a test looked for Caddock's crown at its old place; fixed), Oakhaven toured, wardrobe, loot and fight
  shots taken.
- **Round 3** (published 2026-10-02 20:38; full run EditMode 383/383, PlayMode 174/174, Oakhaven toured, no errors in any game log): masks and mouth scarves sit on the model's face (a face frame), balls on the body (a pelt, bark
  and leather pauldrons) are left off, hoods on peasant outfits take their colour true (Mira's cream, a hen-wife's red
  kerchief), the merchant's collar box is gone.
- **Round 4** (published 2026-10-02 21:38; full run EditMode 383/383, PlayMode 174/174, Oakhaven toured, no errors in any game log): villagers vary a little in height (a village is not all one size); children have a child's larger head; the
  Hollow Men are grey stone statues through and through (one matte stone, the cracks and the violet shard on it).

## 2026-10-02 night — Armour that follows the form, hats that fit (playtest note 12, part two; published 23:44)
- **Armour follows the form** (Chris: "armor is way too blocky. needs to feel flowing"): every worn piece on a model but the
  head's is warped from the old figure's body onto the model's clothes (ModelArmour: the old torso, arm and leg shapes against
  the model's outline per height; a share of the old clearance kept) and skinned to the model's own bones with the weights of
  the clothes under it, so mail, plate, leather and cloth hug the torso and limbs and bend, twist and sway with them; a skirt's
  hem keeps some of the hips' weight so it does not tear between the legs. Pauldrons sit on the shoulders, greaves on the shins.
- **Hats fit the heads** (Chris: "the hats definitely do not fit properly on the heads"): the old hats perched on the old
  figure's big round head. On a model, a hat comes down until its brim sits just above the brows and its crown widens to clear
  the head; caps (the smith's skullcap, the knit cap, the fur hat, the leatherworker's and miller's caps, the Concord helmet)
  are made over as shells of the model's own head and hair. The armour's caps, kettle hats, wraps, circlets and crowns are worn
  the same way; hoods, coifs, barbutes and masks stay drawn round the whole head.
- **Swimming still** is the slow stroke (the treading clip stood with arms straight out, like a figure with no pose); Mira's
  hood is her teal; the miller's flour sack no longer turns into a hood.
- Tests: EditMode 384/384, PlayMode 173/174 (the crown test looked one level too high: on a model it sits in the fitted
  hat; fixed); Oakhaven toured, wardrobe, loot and fight shots; no errors in any game log.

## 2026-10-03 small hours — Sabatons shaped like the feet (playtest note 12, part three; published 00:44)
- **Foot armour follows the foot**: sabatons, shoes and boots were warped onto the model's foot as a box, so they came out
  square-toed and boxy. Now the old boot is taken section by section from heel to toe (its oval at each point along the
  foot) onto the model's own boot at the same point (ModelArmour: the foot's cross-sections in the cage), so the armour
  narrows to the toe, rises over the instep and rounds at the heel like the shoe under it.
- Tests: EditMode 384/384, PlayMode 174/174; Oakhaven toured; no errors in any game log.

## 2026-10-03 — Real animals: wolves, ash hounds, stags and deer (published 01:59)
- **The beasts are models now** (Chris chose real animal models after the people): Quaternius's Ultimate Animated Animal
  Pack (CC0; the wolf, the stag and the deer) replaces the block bodies of wolves, ash hounds, the forest stags and does, and
  the hill deer you hunt. Grey wolves are grey (darker in Khaven's dusk); ash hounds are charcoal with eyes like embers; Old
  Whitefoot is a size bigger, grizzled, his muzzle, chest and feet gone white (GAME-ONLY); camp elites stand a size bigger
  than their packs. The stag wears a russet coat and a wide crown of antlers, the doe a warm brown.
- **They move like animals**: a real walk and gallop matched to their speed, no two of a pack in step; standing, wolves look
  about and deer graze; a wolf lying in wait puts its head down; a blow lands with a bite, an antler charge or a doe's kick;
  struck, they flinch one way then the other; killed, they fall in their own death where they stood (no more tipping over).
- **Rounder than the file**: the pack is low-poly and flat-shaded; every triangle is split in four with the new corners set
  out on the curve of the surface, so outlines and shading come out soft among the painted ones.
- Boars, spiders, the bramble-things and the village's hens, sheep, cats, crows and rabbits keep their bodies: the pack has
  none of them (the farm pack's sheep has no walk, the only animated pig is cube-styled).
- Tests: EditMode 390/390, PlayMode 174/174; ModelBeastTests (6 new); Oakhaven toured; wardrobe, loot and fight shots; no errors in any game log.

## 2026-10-03 — Farm animals in Oakhaven (published 13:06)
- **Horses, a donkey and cows** (Chris chose them next): the same Quaternius pack's horse, donkey and cow (CC0) live in the
  village as critters. A horse stands tethered before the Golden Cask and two graze by Carder's field barn; the miller's
  donkey waits by Oak creek mill; four cows graze the Brook farm pasture and three below the Harrow fence (all GAME-ONLY).
- They come in coats: horses chestnut, bay, grey or black; cows brown and cream, red, dun, or black with a white belly;
  donkeys grey or brown. They graze two thirds of the time and look about the rest, walk to a new spot now and then, and
  amble a few steps off when you walk into them. They are never hunted.
- Tests: EditMode 391/391, PlayMode 174/174; ModelBeastTests (farm animals); Oakhaven toured; no errors in any game log.

## 2026-10-03 — Boars (published 14:42)
- **Real boars** (Chris found CraftPix's free "Wild Animal 3D Low Poly Models"; free for commercial games, kept with its licence):
  the eleven boar camps in Oakhaven, Khaven, the Peaks and the Verdant wear CraftPix's wild boar. Wild boars are dark
  grey-brown, carrion boars ashen, mire boars muddy, the Peaks' rockhides stone grey; Old Scree-Tusk stands a size bigger.
- **Animated in code**: the free models come rigged but with no animations, so the game moves the skeleton itself (ModelBeast,
  procedural mode): a trot in diagonal pairs matched to its speed, the knees folding as each foot comes forward, the body
  bobbing; standing it looks about or roots with its head down; lying in wait it crouches; its blow is a charge with a toss of
  the tusks; struck it flinches; killed it rolls onto its side. The same code can move the set's hare, fox, bear, owl,
  squirrel and hedgehog later.
- Still their old bodies: spiders, the bramble-things, and the village's hens, sheep, cats, crows and rabbits.
- Tests: EditMode 392/392, PlayMode 173/174 (Sel Harrow's walk home timed out once; the fixture passed alone 8/8); ModelBeastTests (the boar); Oakhaven toured; no errors in any game log.

## 2026-10-03 — Khaven filled out: four camps and five quests (published 17:25 with the boards round, below)
- Chris: "lets work on quests and camps", zone by zone, Khaven first (it had 10 camps and 9 quests for levels 3-5; Oakhaven 21
  and 16). Khaven has no book canon (only its map), so everything here is GAME-ONLY, leaning on canon where it can: Hollow Men,
  the Sandthrone company, the Pale Kings' counting.
- **Camps:** the Scarp hollows (Hollow Men walking the grey scarp above the Old Bound Wall), the Thicket deserters (Sandthrone
  deserters lying up in the blackthorn south of the Corpse Road, an ambush camp with a strongbox), the Heights scree spiders
  (charnel spiders in the scree under the Carrion Heights), and the Hush-Mother (one old she-wolf, level 6, at the west edge
  of the Hush).
- **Quests:** The Bound Wall Breaks (Ansel: see the wall, put down the scarp hollows); Toll Without a Captain (Cato: clear the
  thicket, search the deserters' strongbox and its captainless toll-book); Silk on the Heights (Dorra Vey: five skeins of
  charnel silk from the spiders, for a dye she will not name); What the Listener Heard (Old Kestrel: the Listener's Hut, the
  Hush after dark, then Wenna); The Mother of the Hush (Wenna: the Hush-Mother, after her wolves).
- Tests: EditMode 392/392 after the silk icon (391/392 before it), PlayMode 174/174; Khaven toured; no errors in any game log.

## 2026-10-03 — Notice boards, rare postings and silver crowns (published 17:25)
- **Silver crowns** (Chris: "we need book canon"): the coin is the books' silver crown ("forty crowns", "a thousand crowns")
  everywhere it is named: bags, vendors, loot, quest rewards. The save keeps the same number.
- **Notice boards** (Chris: "zone bounty boards, rare quests appear 1-2% of the time giving valuable components for building
  gear"): a board stands by each zone's inn or hall, read with E like a stall. Each game day (the clock passing six) it draws
  three postings from the zone's pool: village bounties (kill six of a camp, bring skins or herbs, walk somewhere after dark)
  paid in crowns, and Sandthrone contracts (CANON company) that pay more and cost Salt-Mender standing. Bounties are
  repeatable: paid, one leaves the board for the day and may be drawn again another day. A reload shows the same notices.
- **The rare posting** (1.5% a slot a day, about one in sixty-seven): a Bureau courier (CANON: the Investigation Bureau) is
  crossing the zone's first road with an Aether-Geode shard in a lead box (CANON: the Council's resonance fuel, fossilised
  memory). Taking the posting puts the courier on the road, two levels over the zone; his death pays the shard, bars of the
  tier above the zone's, crowns and Salt-Mender standing. The **Aether-Geode shard** is the component for five
  **resonance-tempered** weapons at the forge (three bars, a wood and a shard a tier: rare-quality gear).
- Tests: EditMode 396/396, PlayMode 175/175 (OakhavenQuestTests and ZoneContentTests taught that a bounty is the board's, re-run green); BountyTests (4), BountyBoardTests (1); Oakhaven toured; no errors in any game log.

## 2026-10-03 — Dead trees: snags with heavy crooked limbs, not spears (published 21:05)
- Chris, with a shot of the Hush's great dead oak: "this tree is still broke... i brought it up quite some time ago." The
  limbs were five-to-eight-metre needles leaving the top of the bole and all climbing, so the tree read as an antler or a
  broom, and the small husks' boles ended in long spear points.
- `ZoneBuilder.DeadTree`: five or six limbs from a third of the way up, heavy where they leave the bole, three crooked
  lengths that each turn in plan and pitch, forks off every length (some drooping, most forked again), ending in snapped
  stubs; the boles a little shorter and ending blunt and broken (`Bole(close)`, default unchanged for living trees). Every
  dead tree uses it: Khaven's Whispering Wood and the Hush, the Ash Rim's husks, Oakhaven's dead oaks. Playtest note 16.
- Also today: the UI capture photographs the notice board, its postings and a bounty taken (shots 34-36).
- Tests: EditMode 396/396, PlayMode 175/175; Khaven toured; no errors in any game log.

## 2026-10-03 — The Shattered Peaks filled out: four camps and five quests (published 22:25)
- Chris: quests and camps zone by zone (Khaven first, now the Peaks: 9 camps and 9 quests for levels 6-8 before). The
  grown ring's empty landmarks get their camps. CANON leaned on: the Hollow Men, the old mining tunnels under the Peaks
  (book2 ch.19), the Pale Things as watchers and auditors, the Sandthrone holding the pass; everything placed is GAME-ONLY.
- **Camps:** Adit hollows (Hollow Men out of the Sealed Adit, L7-8), Tarn shadows (pale shadows at the Cold Tarn, L8),
  False pilgrims (grey robes over Company mail at the Broken Post, an ambush camp with their packs to search, L7-8), and
  the Umbra Watcher (one Pale Thing under the south Umbra scarp, L8; moved from the brief's spot, 23 m from the Wolf pines
  pack, which it would have called).
- **Quests:** What the Mine Gave Back (Maddoc: the bricked adit, scratched from inside; the hollows); Shadows on the Cold
  Tarn (Tarsk); Grey Robes on the East Road (Hadrik Sull, a Sandthrone contract: kill the false pilgrims, find the gate's
  tally in a Sandthrone hand; +Sandthrone, -Salt-Menders); What the Shrine Hears (Maddoc, then Tarsk: the Listening Shrine
  at night reads the toll-gate's tally back); The Thing Under the Umbra Scarp (Yara Quell).
- Tests: EditMode 396/396, PlayMode 175/175; the Peaks toured; no errors in any game log.

## 2026-10-03 — A sun that reads; hats, the wheel, a staff on the run (published 23:35)
- **The sun (playtest note 9):** ZonePost draws the sun's disc and a warm halo onto the sky in HDR (Post.shader pass 5)
  before the bloom and the shafts take the frame, so trees and roofs cut it and the bloom spreads it over their edges; gold
  to orange as it sinks, its halo wider low in the sky; shafts strongest at dawn and dusk; a faint veil of glare looking
  into it. The water's sun path goes past the paint's cap so the bloom catches a few soft glints. Tuned on new tour shots,
  `<zone>-82-sun-dawn|noon|dusk.png` (the first pass washed the frame milky).
- **Hats (note 17):** a brim is sized from its crown (no wider than 1.65 crowns) before the hat is fitted to the model's head.
- **The wheel (note 18):** Input System 1.20 reports a notch as 1 and the camera divided it by 120: a notch now zooms 1.2 m,
  2.5 to 22 m.
- **A staff on the run (note 19):** running out of a fight, a staff or a polearm goes on the back; stopping or fighting
  brings it to hand (ActorVisual.Fighting, set by the session: an enemy while engaged, anyone else while the player fights).
- Tests: EditMode 396/396, PlayMode 175/175; Oakhaven toured; no errors in any game log.

## 2026-10-04 — Casts land at the edge of range; grounded boulders; a grain sack; deer that flee (published 00:48)
- Playtest notes 20-22 (PLAYTEST_NOTES.md): EncounterSession.LandsOn (range + 5 m, "Out of range." when not); perch and
  rock boulders seated on the lowest ground under their footprint; the coop's grain sack; deer bolt 25-35 m and a hurt
  animal keeps fleeing while the hunter is within 35 m.
- Tests: EditMode 396/396, PlayMode 175/175; Oakhaven toured; no errors in any game log.

## 2026-10-04 — Points of interest and achievements (published 02:48)
- Chris: "need to implement POI in game for discovery and achievement points"; chose every landmark a POI, an Achievements
  tab with points and titles.
- **POIs:** every zone's landmarks. Walking into one the first time (within its radius, 6 to 18 m) shows "DISCOVERED" and
  pays exploration experience (15 + 8 x the zone's level: 23 at Oakhaven, 63 in the Peaks). Unexplored places are a "?" on
  the minimap and zone map, and "? ? ?" in the map's PLACES list (with "x / y" explored).
- **Achievements** (Achievements.cs), worked out from the content: in each zone Explorer of (every place), Secrets of (every
  secret), The Deeds of (every quest, bounties aside), Terror of (every camp elite); across Crulanda The Wayfarer, Keeper of
  Secrets, The Steadfast, The Unbowed (all of each, with titles), Paid in Crowns (10 bounties), Bounty Hunter (50, title),
  The Courier's Bane (a rare posting, title), Journeyman (a trade at 75), Master of a Trade (100, title). Points shown in the
  quest book's new Achievements tab and on the character sheet; a title can be worn from the tab and shows over the player
  frame. A save from before records past deeds at once, quietly, in one line.
- Exploring and earning are off in test runs and capture tours (no surprise toasts in tests or shots); a test turns them on.
- Tests: AchievementTests (3, EditMode); focused EditMode 34/34 and PlayMode 22/22 (Discovery, Armoury, Bounty board,
  Encounter loop); full run EditMode 399/399, PlayMode 175/175, Oakhaven toured, no errors in any game log.

## 2026-10-04 — Quick playtest fixes: crowns, Mira, drinking, the Tin crown (published 04:44)
- Notes 27, 29, 31, 32: the wares' prices and the loot window's coins say crowns; a recruited, living Mira answers E only
  when selected; drinkers bring the tankard to the mouth and rest the other forearm on the table; crowns and circlets on the
  models are drawn a fifth smaller and nearly level, and the hat fit widens them at most 1.2x.
- Tests: focused EditMode 38/38, PlayMode 20/20; full run (no tour) EditMode, PlayMode, build and captures green.

## 2026-10-04 — Swings and casts, see-through buildings, sitting, the horse, tree joins, fps, the camera (published 15:50)
- **Combat animations (note 25):** the figures' graph has an upper-body layer (spine up, AvatarMask) over the walk and the
  poses: a blow swings (Sword_Attack armed; a jab or a cross bare-handed), a hit flinches (Hit_Chest / Hit_Head), a cast holds
  the spell pose and lets fly (Spell_Simple_Idle_Loop / Shoot), all from the Universal Animation Library already in the game.
  Enemies' blows and hits, the player's auto-attack swings, instant abilities on a target and casts drive it. FigureCapture
  has a "fight" row.
- **See-through buildings (note 26):** RoofFade: any building whose big parts (roof, walls) come between the camera and the
  player fades like a tree.
- **Sitting (note 29/30):** the sitting clip puts the hips 0.42 m behind the figure, so a seated body is moved forward onto its
  seat; inn seats face their tables.
- **The horse (note 28):** critters other than cats never pick a point inside a building.
- **Dead-tree joins (note 24):** a crooked limb's continuing lengths have no collar and no pinched end; they run on into the next.
- **FPS (note 23):** measured first (a performance probe at the end of each zone tour: fps with each suspect switched off,
  Unity's frame counters, renderer and material counts, in `<zone>-perf.txt`). The game was CPU-bound (lights, post and MSAA
  each changed nothing). Static scenery merged per thing and material (18,772 renderers to 7,539 in Oakhaven; shadow casters
  3,266 to 1,346; camera 3.4 to 2.3 ms); the HUD's layout pass switched off and its labels' widths and sight lines cached; world
  tints rounded to 1/48. Oakhaven at 1440x900: 94 to 104 fps (development build), 109 in a release build. Still the largest:
  the IMGUI HUD (about 4.3 ms, 2.1 of it nameplates and place names) and scripts (about 3.8 ms).
- **The camera:** orbits down to 40 degrees below level (it stopped at 12 above): out of doors it rides along the ground and
  looks up past you.
- **Full screen by default:** the build set the game to windowed; it is full screen now, and the capture runs put the player's
  own screen setting back after them (tools/validation/screen_prefs.ps1).
- Tests: EditMode 399/399, PlayMode 175/175; Oakhaven and Khaven toured; no errors in any game log; published as a release build.

## 2026-10-04 — FPS: the HUD's hidden cost found; your figure in the character sheet; hens queue up the ramp (published 19:20)
- **FPS (note 23; Chris chose "rebuild hud"):** a UI Toolkit layer for the nameplates was tried and dropped (without a theme
  and with the built-in font unusable as a signed-distance font it cost 20 ms a frame, and the nameplates' drawing was never
  the cost). Timing each part of the label pass found it: `ZoneBuilder.FindZone` parsed every zone's JSON at every call, and
  the place names asked for each road out every frame (2 ms and most of 770 KB of garbage a frame). FindZone now uses the
  zones parsed once (the zone being built still parses its own fresh copy); landmarks', camps' and exits' heights for the
  HUD and the minimap are worked out once (`GroundFixed`); quest markers are re-asked every 0.4 s; critters more than 90 m
  away hold still and moving ones sample the ground every 20 cm. Oakhaven at 1440x900: 104 to 145 fps (development build),
  HUD 4.4 to 1.8 ms, garbage 770 KB to 38 KB a frame. The probe now times the HUD's parts and the main scripts.
- **Paper doll (Chris: "fix the paper doll in the character sheet so he looks like your character"):** the character sheet
  shows your own figure, rendered live by a camera of its own (your parts on a spare layer for the one render), framed on
  the figure's bounds and lit by its own lamp. UI capture shot 38.
- **Hens (Chris: "can they walk up the plank", "wait in line"):** at dusk they queue behind the ramp's foot in the order they
  come, climb it one at a time and go in at the pop-hole; in the morning they come down one at a time.
- Tests: EditMode 399/399, PlayMode 175/175; Oakhaven toured; no errors in any game log; published as a release build.

## 2026-10-04 — The Ash Rim filled out: four camps and five quests (published 20:45)
- Chris: zone by zone (Khaven, the Peaks, now the Ash Rim: 9 camps and 10 quests for levels 9-10 before). CANON leaned on:
  the Sandthrone company, the Hollow Men, the Pale Things as watchers, the faceless Silent Statues in the Wasting, the
  Ash-Walkers and their salt (book1 ch.20). All placement GAME-ONLY.
- **Camps:** Salt-road raiders (Sandthrone, on the old salt road by Wain's Rest, with an overturned salt cart to search),
  Walled Mouth hollows, Reach-Stone shadows (three Pale Things among the stones that mark the Wasting's advance), Old
  Cinder-Jaw (a lone old ash hound in the far west; level 10, the hounds' loot band; "look": "wolf" as the other hounds, so
  it packs).
- **Quests:** Salt on the Old Road (Sefa Brine: a Peaks tally-stick shows the toll-gate buys the stolen salt), What Comes
  Out of the Wall (Grohl), The Reach-Stones (Mother Vane, at night), Cinder-Jaw (Oska), The Silent Statue (Grohl, then Vane:
  the statue and the Wasting's edge).
- Tests: EditMode 399/399, PlayMode 175/175; the Ash Rim toured (235 fps); no errors in any game log; release build.

## 2026-10-04 — The Verdant Shore filled out: four camps and five quests; the zone-by-zone pass done (published 22:40)
- **Camps:** Fern Hollow briars, Ridge crawlers (basalt spiders on the Ridge of Long Shadows' shoulder above Mossveil Falls),
  Void-Touchers below the ridge (CANON: the violet fog brings them, book3 ch.2-4; shown early, PROVISIONAL), Old Brine-Tusk.
- **Quests:** Brine-Tusk (Reed-Song), Lanterns on the Ridge (Moss-Lantern: clear the shoulder, look west over the Verdant
  Ocean), The Violet Fog (Ondine Varro, at night), Briars in the Fern Hollow (Oak-Bane), What the Glade Keeps (Sister Iselle
  writes it; the Whispering Glade at night, then Willow-Whisper).
- The zone-by-zone camps and quests (Khaven, the Peaks, the Ash Rim, the Verdant Shore) are done: 16 camps and 20 quests added.
- Tests: EditMode 399/399, PlayMode 175/175; the Verdant Shore toured (204 fps); no errors in any game log; release build.

## 2026-10-04 — Treant Keepers, better motion, classic MMO controls, bears, the drowned dead, model spiders and crows (published 2026-10-05)
- **Motion (Chris: "could we use this for better motion?"):** Kevin Iglesias's Human Basic Motions FREE (Asset Store EULA;
  Resources/Characters/Animations/KI, 64 in-place clips, tools/wip/characters/ki_import.py). The figures stand in two idles,
  talk with their own talk clip, and walk, run and sprint in eight directions: the walk cycle blends the two directions nearest
  the way a figure moves against the way it faces, so strafing and backing up no longer slide. UI capture shots 39-40.
- **Classic MMO controls (Chris's pick):** on foot you face where the camera looks and move any way from there (strafe, back
  up at a little over half speed); swimming still turns you into the stroke.
- **Treant Keepers (Chris: "we need some better models for the tree people"; he picked Tennessippi Studios' Treant Pack, CC0):**
  every Veridian Keeper is a treant (two builds, 2.7-3 m, Greyheart and the Root-Warden 3.75 m), with its idle, a heavy walk,
  three attacks taken in turn and three deaths. Coloured by kind (Chris: "color them differently based on their type";
  tools/wip/treants/skins.py): living Keepers warm dark bark with a faint green sap-light in the moss; the Greying's withered
  ash grey; Greyheart bleached to bone with a cold pale light; the withered down in the Root-Mother's Deep blackened with
  violet in the moss; the Hollow Root-Warden rotted black, burning violet. The old block Keeper is gone (Chris: "we dont need
  this guy any longer").
- **Bears (Blink's FREE Stylized Bear, Asset Store; Chris: Oakhaven woods + Peaks):** brown bears in the hill hazels under
  Crowsfoot Ridge (L2) with Old Hazelmaw (L3, bigger and grizzled), and grey mountain bears in the Peaks' high pines (L7-8).
  Solitary, skinnable; they drop a bear pelt, claws and a haunch. Quests: Bears in the Hazels (Garet Moss) and Bears in the
  High Pines (Yara Quell). All GAME-ONLY.
- **The drowned dead (ChillLands' Ashen Marches Free, its ossuary knight; Chris: Khaven graveyard dead):** the old bones the
  creek gave back, by the drowned chapel wall (L5, four). They crumble into a heap of bones when they die (Chris: "maybe
  crumble into a pile of bones?"). Quest: The Old Bones (Ansel Morrow, after The Ones Who Came Back). GAME-ONLY.
- **Spiders and crows (Ashen Marches):** every spider is the crypt spider, coloured by where it lives (Khaven's charnel spiders
  bone and ash, the ridge's basalt spiders black, the canopy spiders moss green); the village crows are carrion ravens that hop
  and fly on their own wingbeat; a dead crow keels over onto its side.
- Tests: EditMode 399/399; PlayMode 175/175 after the Peaks bears moved off a stone-pine node and the drowned dead off the Sexton's page (both rerun); Oakhaven, Khaven, the Peaks and the Verdant Shore toured; release build.

## 2026-10-05 — Round 21: fixes from the morning's play, model weapons and legendaries, chests, twin moons (published 2026-10-05 midday)
- **Fixes (notes 33-43):** everyone faced and walked backwards (Human Basic Motions and the Idle MoCap clips turned 180 at
  import); animations slowed toward their own pace; the Tin crown snug on the head; the paper doll framed feet to head;
  distant hill and mountain ranges on the horizon (DistantRanges); cattails in clumps; crags as one face, no balanced knobs;
  gathering above the skill refuses ("Requires Mining 60."); three levels up is red, and every mob above you hits 25% harder
  and takes 10% less a level; common gear always has level-scaled Stamina.
- **Walking by default** (1.9 m/s); "/" toggles running; villagers and children walk; Mira keeps your pace.
- **Mocap idles** (Morro Motion's Idle MoCap): stances, looking about, a cough; the cold idles on the Peaks.
- **One Menu button** in place of the row of HUD buttons.
- **Model weapons and shields** (Blink's swords and RPG weapons, Lumo-Art's cartoon weapons, SICS low-poly weapons, Ashen
  Marches' equipment): generated uncommon, rare and epic pieces wear them by quality, the glowing ones only at epic; bag icons
  rendered from the models (Editor/ModelIcons).
- **Legendary quality** (orange): five legendary weapons, one per zone's chief boss: Kingsbane, the Last Tithe (Caddock),
  The Reckoner's Frost (the Pale Reckoner), The Toll Unpaid (the Sandthrone captain; Sergi Nicols' Staff of Pain), Cinderheart
  (the Ash-Deacon), The Green Wrath (the Hollow Root-Warden). 0.5% by day, 0.65% by night. All GAME-ONLY.
- **Treasure chests** (quiArt's animated chest) beside the outdoor elite camps: crowns, a piece of gear, sometimes a potion;
  they refill after 20 minutes.
- **The twin moons, The Eye and The Tear** (CANON, book1 ch.1; their looks GAME-ONLY): The Eye large and pale with a grey iris
  ring, its light the night's; The Tear smaller and silver-blue, an hour and a half behind on a lower arc. Tour shot 97-moons.
- Tests: EditMode 401/401 and PlayMode 175/175 (the last few count and walking-speed expectations fixed and rerun: EditMode in full, EncounterLoop and NamedLoot); all five zones toured, no shader errors; release build.

## 2026-10-05 — Phase 5.1a: the Paladin (published, evening)
- **A third playable class (Chris chose Paladin, Ranger, Mage for Phase 5; provisional GAME-ONLY name and mechanics, after
  CLASS_BUILD_MATRIX's Oathguard tank, Judicator melee, Sanctuary healer):** a mailed hybrid on Mana with Conviction (three
  pips) built by Smite in melee and by blows taken under the Ward, spent by Judgement (a thrown light, 8 m) or Lay On (a
  quarter of the target's health at once). Nine actions: Smite, Oath of Ward (taunt), Mend (1.5 s heal), Ward (-40% damage
  5 s), Judgement, Consecrate (burning ground 5 m for 6 s), Lay On, Aegis (barrier) and the talent action Censure (holds the
  target's swing 2 s). 21 talents in three rows of Oathguard, Judicator and Sanctuary (EncounterContent/Talents/paladin.json),
  all with effects (PaladinKit). Its own character and save, chosen from the pause menu, which now lists every other class.
- **Looks:** the Paladin in ivory and gold with a beard and the Warrior's sword and shield until gear takes over; the Ranger
  (green, hooded) and the Mage (violet, hooded, a woman) are drawn ready for their rounds. Icons painted for the nine
  actions and the 21 talents; the figure line-up (people-2) shows the three.
- Tests: EditMode PaladinRulesTests (tree, gates, content), PlayMode PaladinLoopTests (own save, Conviction, Mend and Lay On,
  Ward, Consecrate). Full run: EditMode 405/405, PlayMode 179/180 (VillageHomeTests' bedtime walk timed out once under lane B's load and passed twice after: flaky under load, noted), build, five tours, the Paladin's own HUD captures (paladin-01-talents, 03-combat). Phase 5.0's two-lane run: 60 min.

## 2026-10-05 — Phase 5.1b: the Ranger (published, night)
- **A fourth playable class (provisional GAME-ONLY name and mechanics, after CLASS_BUILD_MATRIX's Marksman ranged, Beastbond
  pet, Pathfinder control):** a bow at 25 m on Focus (100, back fast in and out of a fight) with a wolf at heel. Nine actions:
  Quick Shot (instant; starts the bow's own auto-shot, the game's first ranged auto-attack: ClassKit.RangedAutoAttacks and
  AutoAttackRange, a release pose instead of a swing), Aimed Shot (1.5 s draw, hits hard), Barbed Arrow (bleeds four ticks),
  Hunter's Mark (the target takes 10% more from the whole party for 15 s), Snare (half speed 6 s), Call Companion (the wolf
  comes or goes), Sic (the wolf runs in, first bite half again as hard), Disengage (a 6 m leap away, no global cooldown) and
  the talent action Pin (roots 3 s). 21 talents in three rows of Marksman, Beastbond and Pathfinder
  (EncounterContent/Talents/ranger.json), all with effects (RangerKit): Steady Hand, Quick Draw, Piercing, Bleeding Wounds,
  Snap Shot (a free Quick Shot after an Aimed Shot), Headshot, Double Nock; Thick Coat, Sharp Teeth, Mending Bond (shots heal
  the wolf), Sic Fury, Pack Sense (the wolf goes for whatever you shoot), Howl (its bites hold attention), Alpha; Fleet,
  Tangling Snare, Keen Eye, Pin, Cover of Leaves (a barrier on Disengage), Trapper, Pathfinder (the Mark also slows).
- **The wolf (RangerPet):** a grey wolf on the pack's model, 90 + 18/level health (talents scale it), bites every 2 s with
  threat of its own, keeps to heel a pace behind the Ranger's shoulder at the Ranger's pace, hunts what it is sent at until
  the quarry dies or breaks off, and is a party member the enemies can turn on (EncounterSession.PartyActor). Not saved: a
  whistle each session.
- **Looks:** the Ranger in forest green and a hood with the packs' Bow_Basic in the left hand, gripped at its centre
  (ActorVisual.ClassModels.cs: a class kit piece that is a model; ModelLength knows bows), slung across the back when swimming,
  yielding to a main-hand item like the Druid's staff. Icons painted for the nine actions and the 21 talents (make_icons.py:
  ash arrows, fletching, paw prints). HUD class colour green; the pause menu offers the Ranger like the other classes.
- Tests: EditMode RangerRulesTests (tree, gates, content; four classes now), PlayMode RangerLoopTests (own character, Quick
  Shot at 15 m and the auto-shot's second arrow, the drawn Aimed Shot, Mark and Snare, the wolf called and sent, Disengage).
  capture_extra.ps1 takes the Ranger's HUD shots (prefix ranger-).

## 2026-10-05 — Phase 5.1c: the Mage (published, late night)
- **A fifth playable class (provisional GAME-ONLY name and mechanics, after CLASS_BUILD_MATRIX's Combustion ranged, Heatweaver
  control, Spellbinder support):** a caster on Mana with a second, visible gauge, Heat (0-100). Ember Bolt (a 1.5 s cast, 15
  Heat), Scorch (an instant burn that keeps burning, 10) and Smoulder (a slow, 5) build it; Flare releases all of it at once (the
  more Heat, the harder; needs 30); reaching 100 is an Overload: a burn of 8% of your own health, the gauge emptied and four
  seconds in which nothing builds. Cinder Field burns the ground under the target (4 m, 8 s), Ember Ward is a barrier, Bind makes
  the next Ember Bolt 40% weaker and banks a Charge, Unbind spends it so the whole party hits 15% harder for 10 s, and the
  talent action Quench holds the target's swing 2 s and ends an Overload's stall. 21 talents in three rows of Combustion,
  Heatweaver and Spellbinder (EncounterContent/Talents/mage.json), all with effects (MageKit): Kindling, Hot Hands, Stoked,
  Flashpoint, Backdraft (an instant bolt after Flare), Inferno, White Heat (Overload no longer burns); Cinders, Slow Burn, Wide
  Field, Embers Underfoot (the field slows), Heat Haze (enemies in it hit softer), Long Burn, Firestorm (Smoulder in the field
  roots); Reservoir, Ward Weave, Quench, Shared Flame (Unbind heals), Steady Mind, Quickening (Unbind hastes Mira), Binding
  Oath (25%).
- **Looks:** the Mage in violet and a hood, a woman, with the packs' Cartoon_Staff_01 in the right hand gripped 40% up like the
  Druid's staff (ActorVisual.ClassModels.cs now knows a staff's own grip), slung across the back when swimming, yielding to a
  main-hand item. Icons painted for the nine actions and the 21 talents (embers, a cold snap for Quench, violet bindings).
  capture_extra.ps1 takes the Mage's HUD shots (prefix mage-).
- Tests: EditMode MageRulesTests (tree, gates, content; five classes now), PlayMode MageLoopTests (own character, the cast and
  the Heat it builds, Flare's release, the Overload and its stall, Cinder Field and Smoulder, Bind, Unbind and Ember Ward).

## 2026-10-05 — Round 22: the first playtest of the three classes (notes 47-53)
- **Weapons in hand:** the held mount scaled 1.05 (was 1.35: "sword too big"). The Mage starts with an Apprentice's Wand and the
  Ranger with a Hunter's Bow (the packs' Wand_Basic and Bow_Basic; bows join the model-weapon looks, gripped at the middle with
  the limbs up and down); the others keep the Trailblade.
- **Spells and arrows seen flying (Bolt.cs):** a glowing ball with a trail and a point light flies from the hand to the target in
  a quarter second and bursts into embers (Ember Bolt, Scorch, Flare, Smoulder, Quench; Seedshot and Thornbolt; Judgement); an
  arrow for the Ranger's shots and the bow's auto-shot. Cosmetic: the blow lands as before.
- **The cast pose:** a cast being drawn holds the end of Spell_Simple_Enter (hands gathering) instead of the loop with the arms
  held out; the release is unchanged.
- **The jump:** Kevin Iglesias's Jump01 (take-off, the air, the landing) on the player's figure (AdventurerMotor.Airborne).
- **Signposts** say where the road goes (ZoneProp.name on each of the 19 posts, burnt into both faces of the board) and none
  stands in the road (three moved a metre past the verge: two in Oakhaven, one in Khaven).
- **The leash:** 30 m from where the pull began (was 17) and 6 m of give (was 2), so backing a few steps no longer sends a
  wolf home.

## 2026-10-06 — Phase 5.2 round 1: the sims' roster (published)
- **Twenty SimAdventurers** (Scripts/Encounter/SimAdventurers.cs; GAME-ONLY names, folk labels PROVISIONAL): stable ids, a class
  from the five kits, a level with a home zone to match, a personality (bold, friendly, chatty), hours they are online by the
  world clock, and where they were last seen. Made once from a seed and kept in a new world save slot beside the characters'
  (world.save.json; SAVE_FORMAT.md), shared by every character and written with each autosave.
- **In the zone:** those of this zone who are online stand about its named places as figures in their class's look
  (SimPopulation, SimFigure), wander between them, and the friendly turn to face you when you come close; they go when they
  log off and come back when they log on. Nameplates in the class's colour with the class and level beneath.
- Tests: EditMode SimRosterTests (twenty for a seed, names unique, every class, levels to homes, hours wrapping midnight, the
  world slot round-trips, falls back to its .bak and rejects rubbish), PlayMode SimPopulationTests (Oakhaven's online sims stand
  there as figures on the NavMesh, none from other zones, gone when they log off; their places saved with the character).

## 2026-10-06 — Phase 5.2b: inviting sims (Chris moved a basic invite ahead of the rest of Phase 5)
- **Click a sim and Invite** (its frame where the target frame goes). It joins unless your party is full (three sims, a party
  of five with Mira), you are fighting, it is more than five levels from you, or it is busy. Leave party (its frame, or the
  button on its party row) sends it back to the world where it stands.
- **In your party** (SimCompanion): follows a pace behind in its own place, fights your target in its class's way (Warriors and
  Paladins in melee, the Warrior holding attention; Rangers shoot arrows, Mages fire, Druids thorns; Druids and Paladins heal
  the most hurt of you), can be turned on by the enemies, falls and gets up after the fight. Its row under Mira's frame shows
  health and what it is doing. The party is not saved yet.
- Tests: PlayMode SimPartyTests (joins, follows and leaves; the busy and the far-off decline and the party holds three; fights
  your target). HUD captures 40-sim-invite and 41-sim-party.

## 2026-10-06 — T-pose fix and visible sims (playtest notes 54-55)
- **No more T-poses:** CharacterImport imported the Morro Motion idles (Round 21) from the first frame of each take, which is the
  capture's calibration T-pose; every figure standing still went into a T for a few seconds of each loop. It keeps the vendor's
  trimmed range now (importer version 7). ModelFigure always gives a figure the body's avatar (the Ranger outfit's own avatar
  calls itself humanoid but is not mapped).
- **Sims visible:** their figures were hidden along with the placeholder capsule.
- Tests: PlayMode FigureAnimationTests (you as Warrior and as Mage, Mira, party sims, world sims, enemies and villagers: no hands
  more than 1.1 m apart in ten seconds of watching), SimPopulationTests checks a figure is drawn.

## 2026-10-06 — Phase 5.2 round 2: the sims dressed, the who list, mobs scaled to your group
- **Gear on the sims** (SimGear): generated gear at their level from their gear seed, the same every time; mail for Warriors and
  Paladins, leather for Rangers, cloth for Mages and Druids; a blade and shield for Warriors and Paladins, the class staff, bow
  or wand for the rest; uncommon from level 4, some rare from level 8. Shown on their figures and in your party.
- **The who list (O):** everyone online by the world clock, your zone first: name, class in its colour, level, zone, "(party)",
  and Invite for those standing in your zone.
- **Mobs scale to your group** (Chris chose strength over level): a mob keeps its zone level; each sim in your party adds a share
  (its level over the mob's, 0.25 to 1.25), each share +60% health and +15% damage. You with Mira are the baseline. Applied when
  the fight starts (raised if a sim joins mid-fight), undone when it resets; the target frame says "Scaled for your group of N".
- Tests: EditMode SimGearTests; PlayMode SimPartyTests.Mobs_grow_with_the_group_by_its_levels. HUD capture 42-who.

## 2026-10-06 — Helms seated by measurement (playtest note 56)
- Every head piece is fitted to the head it is on (ActorVisual.GearArmor SeatHead, using the villagers' hat fit, HatFit, now
  with a choice of outline, walls and limits): caps and kettle hats with the rim just above the brows and no wider than 1.3
  (the flaps cap and the half kettle resting on the hair's top); hoods, coifs, barbutes, masks and the scarf by their crown
  just over the hair, sized from the skull only; circlets and crowns by their band, not their spikes. Any class hood (Druid,
  Ranger, Mage) comes off under a head piece. FigureCapture helms-fit rows: all 18 on a man and a woman, front and side.

## 2026-10-06 — Phase 5.3a: the sims live their day
- **They choose what to do** (SimFigure.Choose) when the last thing is done, by personality, health and what is near: hunt the
  camp mobs of their level (bold ones more), work a herb or ore node (cautious ones more), rest at the inn (when hurt, or now
  and then), or stand about the named places (chatty ones more), with a little chance in it. Their plate says what they are at
  ("Warrior 5 · fighting Grey wolf", "· gathering", "· at the inn").
- **They fight real mobs, and the mobs fight back** (EncounterSession.CombatActor: your party, or a sim that came for it). They
  fight in their class's way; healers mend themselves; a hurt sim breaks off; one beaten falls and gets up after a while (the
  run back from a graveyard is 5.3b). A mob fights a sim at the sim's level, and is not scaled to your group.
- **Tagging:** a mob first hit by a sim on its own is its kill: no experience, coin, loot or quest credit for you. A sim's fight
  elsewhere does not put you in combat (you can still save, invite, rest).
- **Nodes are shared:** a node a sim works rests on its own timer, as when you work it. The inn: they go in (out of sight and
  reach) and come out mended. The Golden Cask's door is now known as the inn's (ZoneDoor.kind "inn").
- In editor test runs and the HUD captures the sims only stand about (SimPopulation.Lively), so none tags a mob a test is
  fighting; SimLifeTests turn their life on.
- Tests: PlayMode SimLifeTests (a hunt the mob answers, the sim's kill gives you nothing, a node worked and resting, the inn,
  the bold choose to hunt).

## 2026-10-06 — Round 23 (UNTESTED, NOT PUBLISHED: Chris stopped the test run; playtest notes 57-61)
- Brimmed (straw) hats removed from modelled figures; cows, horses, donkeys and sheep solid (box colliders); class attributes by
  level shown on the character sheet, balance-neutral (EncounterSession.Innate + cancelling modifiers); the empty-slot tooltip
  beside its slot; sign letters in a depth-tested shader; farmhands for the fields no farmer works (VillageLife.SpawnFarmhands).
- Zone chat researched: Docs/CHAT_RESEARCH.md.

## 2026-10-06 — Zone chat (playtest note 62; Phase 5.5 brought forward)
- **The chat window** (EncounterHud.DrawChat, ZoneChat): tabs All, Zone, Trade, LFG, Party and System (the game's own
  messages); each channel in its colour. **Enter** to type, Enter to send, Esc to stop; /s say, /z zone (the default), /t trade,
  /lfg, /p party. No key moves you while you type (EncounterInput.Typing). Clicks on the chat stay in it.
- **The sims talk** (SimChatter), every few seconds (sooner the more are here), the chatty ones far more, the quiet ones never,
  and what they say is true: LF1M for a real camp at their level, LF2M for an elite that is up, an elite up near a named place,
  WTS and WTB the ore and herbs of the zone, where-is questions that another sim answers with the real direction ("The Old
  Barrow's south of the village, past Brook pond"), the road to another zone, grumbles about the mobs they are fighting, the
  inn, the dark and the rain, goodnights as they log off. In your party they call out in Party ("inc", "need a heal").
- **They answer you:** where a place is, a group wanted (a sim of your level offers to join), something for sale, thanks,
  hello. Built from Docs/CHAT_RESEARCH.md; all lines GAME-ONLY.
- Tests: EditMode ZoneChatTests; PlayMode SimChatTests (they talk; directions answered; a group call answered; messages in System).

## 2026-10-06 — Phase 5.3b and 5.3c: the sims travel, die and get up, level, trade and craft
- **Levelling (5.3b):** a sim's own kills (and half shares of your party's) are experience on the player's curve; at a level it
  says "ding N!" in Zone (Party for a party sim, and Mira says grats) and others say grats; its health grows and its gear is
  made again for the level.
- **Death and the corpse run (5.3b):** a beaten sim lies ten seconds where it fell, its corpse marked there ("Corpse of ..."),
  comes to at the zone's recovery point at a third of its health, runs back, and is whole again on reaching it (or gives up
  after three minutes and rests at the inn). Chatty ones say where they died.
- **Travel (5.3b):** a sim that has outgrown the zone's levels (SimRoster.Homes bands), or now and then a bold one, takes the
  road to a zone that suits it (its home weighted) and arrives at that road's far end (ZoneExit.arrive). The unseen move too
  (a first step of 5.4): every hour or three a sim in another zone may take a road toward its level; one arriving where you
  are appears at the road's end.
- **Trades and the purse (5.3c, playtest note 63):** Warriors and Paladins mine, Rangers cut wood, Druids and Mages pick herbs
  (nodes of their trade at their skill, 8 a level); what they gather is theirs (goods) and is sold at the merchant's stall for
  the items' values; coin buys up to two gear upgrades (25 x step x level), which raise the quality of what SimGear makes.
  Warriors and Paladins smith at the forge from what they mined (bars first, the merchant's charcoal bought as needed, then
  the cudgel, buckler, gauntlets, jerkin, helm, greaves, hauberk and so on of the recipes at their skill), and wear what they
  forge (wornSlots/wornIds). Druids and Mages would brew, but no village has an alchemy bench as a place yet: they sell herbs.
  Chat tells of it: WTS of what they really carry, "just forged a copper cudgel", "finally afforded better gear".
- World slot: experience, coin, gearBonus, goods, worn pieces and nextTravelHour added to SimAdventurer (format 1 still: an
  older slot reads them empty).
- Tests: EditMode SimEconomyTests; PlayMode SimWorldTests (ding and grats; the corpse run; the road out and the unseen move;
  the stall, an upgrade, the forge).

## 2026-10-07 — Round 24 (playtest notes 64-71)
- The Golden Cask's horse stabled at a hitching rail in the back yard (new prop "hitch"); the camera held inside the building
  you are in; one chat window coloured by channel with filter chips, drag and resize (PlayerPrefs); a sticky chat channel
  (/lfg alone switches it; the input shows the channel); /invite <name> across zones (arrives at the nearest road's end after
  InviteTravelSeconds, 30), /leave, /who, /help; healing seen (a second's cast pose, then HealFx: green glow and motes);
  the coop's trough, sack and pan seated on the ground (ZoneBuilder.Seat); the orchard's apples in the leaves.
- Tests: ZoneChatTests (sticky channel, prefixes), SimChatTests (/lfg switch, /invite from another zone, unknown command).

## 2026-10-07 — Round 25: party loot (playtest note 72; a piece of 5.6)
- **Coins split** round the party (the sims' shares into their purses). **Uncommon and better is rolled for** when you are in a
  party (LootRoll): a panel under the target frame with Need / Greed / Pass for you; each sim chooses by what it can use (Need
  for gear of its weight and slot that beats what it wears, Greed for what it could sell); Need beats Greed, the highest roll
  of that tier wins, everyone passing leaves it to you; twenty seconds, then it resolves. The winner takes it (a sim wearing
  what it needed, or keeping it to sell); rolls and the win are said in Party. Common and junk are taken as before.
- Tests: PlayMode PartyLootTests.

## 2026-10-07 — To-do items 2 and 3, and Phase 5.4: the unseen live on
- **An alchemy bench:** the herbalist's drying hut stands in for it (VillageLife Places["bench"]), so Druid and Mage sims brew
  potions there from the herbs they pick, and sell them.
- **The sentinel helm** (the one helm in the Ashen Marches pack) is the first model head piece (GearLooks model.helm,
  ActorVisual.GearArmor ModelHelm, seated by SeatHead like the drawn ones), worn by a new rare drop of the Rim's cultists, the
  Sentinel's Helm (loot.ashrim.json). Thirteen named items now (LootDataTests).
- **5.4, the unseen living on** (SimPopulation.LiveAway): each world-clock hour a sim online in another zone hunts (experience
  and levels, the bold more), gathers its trade's first material (the careful more), and when it has goods enough forges what
  it can or sells them and buys gear upgrades; nothing while offline; a gap longer than six hours counts as six. With the roads
  taken between zones (5.3b) the world keeps moving while you look elsewhere. Tests: SimEconomyTests.The_unseen_live_on_by_the_clock.

## 2026-10-07 — Phase 5.5: whispers, friends and the sims' memory of you
- **Whispers:** a Whisper channel (pink, its own chip), `/w <name> <words>` to anyone online anywhere (first names work; two-word
  names too), `/r` answers the last whisperer, and the sim frame has a Whisper button. The sims answer by what you said and what
  they make of you (SimChatter.Whispered): directions, "sure, inv", a hello back; a stranger who is not the friendly sort may
  ignore you; a rival never answers.
- **Memory (SimMemory):** every sim keeps a regard score for you, saved with it. A point a minute grouped and one for every
  three kills together, three for passing on loot it needed, minus four for needing over its need, one for an answered whisper,
  minus three for /kick. Standings: stranger, acquaintance (6), friend (25), rival (-10). The who list and the sim frame show
  them; crossing one is said.
- **Friends:** `/friend <name>` lists one (`/friends` shows them and who is online). A friend greets you by whisper when you
  meet (once an hour of the clock), now and then asks you to group ("inv me"), stretches the level gap by two, is never too busy
  to join, and hands you loot it won on Greed when you wanted it. A rival refuses your invites ("Not with you").
- Tests: SimMemoryTests, SimChatTests.A_whisper_is_answered_and_a_friend_is_listed.

## 2026-10-07 — Phase 5.6: the party system
- **Roles** (SimCompanion.Role, shown in the party frames): Warriors tank, Druids heal, Paladins tank unless a Warrior is along
  and no Druid is, Rangers and Mages deal damage. A tank takes first whatever mob is on someone who is not a tank, taunts it
  (EncounterThreat.Taunt, every eight seconds, "Taunt" floated) and its blows hold attention; the rest fight your target.
- **/assist [name]** targets what a party member is fighting.
- **Runs:** `/lead [name]` (or the Lead button on a party frame) has a sim lead the party to a camp near its level (an elite when
  the party is three or more): "Follow me", it walks there at a trot, waits when you fall more than eighteen metres behind,
  fights what it finds at the camp, and calls the run done when no mob of the camp stands; Stop ends it. A friend who asked
  you to group (5.5) names the camp it has in mind.
- Tests: SimPartyTests.A_warrior_tanks_and_leads_a_run_to_a_camp.

## 2026-10-07 — Art round 1, the props round: the Fantasy Props MegaKit and the Medieval props
- **Two kits in the project** (Chris's asset queue): Quaternius's Fantasy Props MegaKit (CC0; 94 FBX on four trim sheets, under
  Resources/Props/Fantasy with their base colour and normal maps; ThirdPartyImport gives each material its sheet on the Standard
  shader) and Lukas Bobor's Medieval props (Asset Store EULA; 40 prefabs with their own materials, under Resources/Props/Medieval).
- **ZoneBuilder.ModelProp** stands any kit model at a height, its bottom on the spot, turned, only its meshes kept, a box collider
  the navmesh respects when solid; GroundProp sets an outdoor one on the land. A builder keeps its painted stand-in for when the
  kit is missing (the tests' bare scenes), so nothing depends on the kits being there.
- **Where they stand:** every barrel and crate in the game is the kit's now (Barrel, Crate); the inn's taproom has the kit's
  tables with stools, a candlestick and a mug on each, barrels behind the bar, mugs, a bottle and a candle along it, a chandelier
  over the room, a bench and a woodpile by the hearth; the smithy has a workbench against its back wall, a weapon stand at the
  stone end, a whetstone and a bucket by the anvil; a produce stall has crates of apples and carrots out front, the other stalls a
  barrel. Two new prop kinds for the zone files: "dummy" (a training dummy, solid) and "banner".
- **PropCapture.Render** (run_method, -Graphics) draws every kit model the builders use in a row with a metre rule, three-quarter
  and front, to hel/work/ui-captures/props-row*.png: the check on size and facing before a model goes into a village.
- Left for the next art rounds: the Stylized Nature MegaKit (trees, bushes, rocks for the outdoors), the Stylized Megapack, the
  HQ Rock Pack, and the Medieval Village MegaKit when it is in Downloads.

## 2026-10-07 — Art round 2, the nature round: the Stylized Nature MegaKit
- **The kit in the project** (Quaternius, CC0; 68 FBX under Resources/Props/Nature with their bark, leaf, rock and grass textures;
  ThirdPartyImport names each material's texture after it, bark with its normal map, grass and flowers cut out).
- **Kit trees** (ZoneBuilder.NatureKit, KitTree): every broadleaf "tree" prop and forest-edge tree is one of the kit's five
  common trees (6-8 m), every pine one of its five pines (8-13 m), every meadow bush its bush (flowering now and then), each on
  the prop root with its turn and scale, a trunk collider, a NavBlocker and TreeFade as before. The builders take the same draws
  from the zone's stream as the painted trees did, so nothing else in a zone moved. The painted trees remain the fallback
  (tests' bare scenes) and still build the great oak, the giant trees, dead trees and twisted woods.
- **Leaf cards:** the kit's leaf, grass and flower sheets are alpha cards whose colour is in the "_C" sheets; they go on the
  game's own painted-leaf shader (cut out, both faces, swaying, faded by TreeFade), set by the importer and again at run time
  (ZoneBuilder.DressNature). Crowns take the painted trees' leaf-family tints (and grey in gloom); meadow bushes get the green
  sheet (the model ships with the twisted tree's autumn red). Pitfall found on the way: the importer's normal-map test matched
  "NormalTree" in the colour sheets' names, which imported the bark and leaf colour as normal maps (pink bark, no crowns); it
  now matches only files ending in _Normal.
- The props row (PropCapture) now includes the kit's trees, bush, dead tree, rock, grass, flowers and twisted tree, and logs
  each model's materials (shader, texture) beside its size.
- Left: dead trees and twisted trees from the kit, its rocks and pebbles (the HQ Rock Pack is next for crags), ground plants.
- Full run c39: EditMode 427/427, PlayMode 212/214, five tours, 0 shader errors. The two failures were races in the sim tests,
  fixed: a recovered corpse is hidden at once (it was destroyed only at the frame's end, so the test still found it), and the
  ding test now counts "gratz" (one of the five grats lines did not contain "grats").

## 2026-10-07 — Art round 3: the Stylized Megapack's medieval half, kit rocks, haystacks and carts
- **The Stylized Megapack 2in1** (Asset Store EULA; tools/wip/art/megapack_import.py): its "Medieval Kingdom" half only (207
  models, materials, textures under Resources/Props/Megapack; the Asia half is off the world's look). Its materials are URP Lit and
  the project is the built-in pipeline, so the import rewrites each .mat to Standard, keeping its maps, colour and smoothness;
  the FBX metas keep their GUIDs, so every model still finds its material.
- **Rocks:** in the green zones every forest-edge rock and every loose rock (one you do not interact with) is one of the nature
  kit's three mossy rocks, with the same collider; the mountains, the ash and gloom keep the painted rock and its skirts. The zone's
  draws are taken in the same order as before, so nothing else moves.
- **Haystacks** are the Megapack's corded rick (2.1-2.4 m), often with a rolled bale lying by it; **carts** are its hay cart, shafts
  where the painted one's were. A new zone prop kind, "target", is its archery butt.
- **Left out:** the HQ Rock Pack. Its three rocks are photoreal scans and clash with the painted world (props row); the import is
  kept in the script, its files deleted. The Megapack's white rock formations and buildings are not placed yet.

## 2026-10-07 — Art round 4: the kit's dead and twisted trees
- **Dead woods:** every dead tree in the forest edges of gloom and ash, the dead groves (Khaven's Whispering Wood) and the
  Wasting's ash trees is one of the nature kit's five dead trees, as tall as the painted one would stand, darker in the ash and
  in gloom; trunk collider, NavBlocker and TreeFade as before; the zone's draws untouched. The massive dead oaks (landmarks) stay
  painted.
- **Autumn trees:** the broadleaf's autumn family (the red-brown one) is the kit's twisted tree in its own autumn red.
- TreeLimbTests counts only the painted trees left (the kit's are models without recorded wood).
- Full run c43: EditMode 427/427, PlayMode 213/214, five tours, 0 shader errors. The failure: SimChatTests waited forty seconds
  for three zone lines when six sims talk every 8-23 s; it waits seventy-five now (rerun green). Editor/HouseCapture.cs renders the
  Megapack's buildings for comparison (Docs/art).

## 2026-10-07 — Art round 5: Megapack outbuildings (Chris chose "outbuildings only")
- Chris compared the Megapack's buildings with Oakhaven's houses (Docs/art/megapack-houses.png, oakhaven-houses-now.png;
  Editor/HouseCapture.cs renders them) and chose to keep the village houses (doors, interiors, lit windows) and use the pack for
  the outbuildings.
- **Barns:** every barn nobody sleeps in (the Tithe barn, Harrow, Brook and Hollin barns, the Cider Barn, Carder's field barn) is
  the pack's double-gabled timber barn, one of its two finishes by name, fitted inside the old footprint with the same collider.
  Moss's lodge and any barn a household lives in stay painted, with their doors.
  Each kit barn is stretched to its footprint and the painted barn's height and stands on the same stone sill with a step at
  its door (BuildingGroundTests: every building on stone down to the ground). ZoneBuilder.ModelProp takes a box for that.
- **The Golden Cask's stable:** the pack's open stable stands over the hitching rail, so the inn's horse has a roof (playtest
  note 64 asked for a home or a stall).
- **The Watchtower** is the pack's wooden lookout on its trestle legs (prop variant 2); the gate and toll towers stay stone.
- Full run c46: EditMode 427/427, PlayMode 213/214, five tours, 0 shader errors. The failure: SimPartyTests gave a party sim a
  fixed four seconds to close three metres under the run's load; it polls up to eight now (rerun 5/5).

## 2026-10-07 — Art round 6: loose ends (mountain crags, Carder's windmill) and the Village kit imported
- **Mountain rocks:** in the Peaks the forest-edge rocks and the loose rocks you do not interact with are the Megapack's rock
  formations (big ones) and standing stones (small ones), their pale stone shaded to the mountain's, sunk a little and still on
  the painted rubble skirts; same colliders, the zone's draws untouched.
- **Carder's windmill** (GAME-ONLY): the Megapack's post mill on its stone podest west of Carder's field barn (-20, 126), clear of
  the road, the north acre and the pines; a collider round the podest; its sails turn slowly (ZoneBuilder.Sails). New prop kind
  "windmill".
- **The Medieval Village MegaKit** (Quaternius, CC0; 176 modular pieces: plaster, brick and timber walls with doors and windows,
  floors, stairs, tiled roofs, chimneys, balconies) is under Resources/Props/Village with its textures (the Unity-style normals);
  ThirdPartyImport gives each material its sheet. Its pieces go into use next round.
- **The Great Oak** (playtest note 73: "mightier, taller", then "not that huge, just mightier and noticeable from a distance"):
  the prop scale 1.9 to 2.2 makes the bole stouter; in its own metres its limbs reach 1.4 times further and its crown
  spreads 1.4 times wider and rises 1.2 times higher, with a ring of great masses between the first and the skirt and more skirt
  and rim clumps: about 30 m tall (was 20), the crown about 20 m from the bole (was about 11), standing over the roofs from any
  approach. Its own stream draws it: nothing else in the zone moves.
- **Nothing stands in a road** (playtest note 74: "still lanterns/stonehenges/markers in the middle of the road"): the exit
  waystones (BuildExits) stood exactly where each road leaves the zone; they now stand on the verge. Small props (lamps, signposts,
  carts, loose rocks, stones, shrines, boards, barrels, crates and the like, unless you interact with them where they are) are moved
  out sideways from any road to its edge plus their own reach (ZoneBuilder.Verge, OffRoad): the lamp on the green, Khaven's cart
  in the gate road, the lamps and the Harrow farm sign whose arms reached over the edge, a rock by the west road.

## 2026-10-07 — Round 26: playtest notes 75-82
- **75. Key bindings:** pause (Esc) > Controls lists every game key; click one and press the new key (a key in use swaps over); WASD
  and ESDF (G interacts) presets; saved at once (KeyBindings, PlayerPrefs keys.*). The help line and the [key] prompts name the
  keys as bound.
- **76. Crops that grow, farmers that work:** every tilled field has a crop on the ridges between its furrows (CropField, the nature
  kit's tall grass in one mesh, Crulanda/Crop grows it), running a six-day round from its own day: bare, sown, sprouting, growing
  green, ripening gold, reaped; fields a day or two apart. A farmer's work follows his field: kneeling to sow, hoeing while it grows
  (a real lift and stroke now, leaning in), cutting when it is ripe, gathering when it is reaped.
- **77.** The farmer's fork is carried tines down; on a modelled figure the bib and its straps no longer float off the chest.
- **78.** Autumn trees are russet, red-brown, amber and rust (the twisted tree's grey leaf mask tinted), not the pink-red sheet.
- **79.** Weapons are sheathed (on the back or at the hip) out of a fight and drawn when it starts or to cast; mobs while engaged, sims
  out hunting while they fight, villagers and guards never draw.
- **80.** Barns 8 m and wider are the Megapack's walled barn (Building 3Base) fitted by its walls to the stone sill and door step
  (ZoneBuilder.WallProp measures the vertices in its lowest metre); smaller barns are the painted barn again.
- **81.** Horses, cows, donkeys and sheep keep a body length apart: they stop short of one another and choose spots clear of the herd.
- **82.** Signposts point at what they name: a zone's road end, a landmark or a named place, along the road where one runs by.
- **83. Hail** (H, rebindable): greets whoever you have selected, or the nearest person within 15 m in front; you say it, they
  turn and answer: a villager by their trade, a sim by what it makes of you (a rival only glares; an answer counts a little toward
  its regard), Mira as Mira.
- **86.** No grass or flowers in the hens' water pan: a coop's yard, a hitching rail's trough and a well's step stay bare.
- **84. The open ground dressed** (ZoneBuilder.Meadow): the green zones' open grass gets the nature kit in clusters on an 18 m grid,
  thickets (bushes, often round a young tree), lone trees (now and then a russet one), mossy rocks with pebbles, flower patches and
  ferns, stumps and fallen logs; the mountains a stone or a lone pine now and then. Only where the grass grows open, out of the
  village's heart, clear of camps, nodes, secrets, exits, landmarks, props and trunks, on gentle ground; its own stream.
- Colours after Chris's notes during the round ("gold is too light and too yellow", "add some red to the trees"): every kit crown
  and bush is the kit's grey leaf mask tinted deep green, fresh green or old gold (the kit's own sheet is a lime that reads yellow);
  the autumn set is russet-red, crimson, old gold, rust and deep red; ripe crops are amber.
- Full run c49: EditMode 427/427, PlayMode 212/214, five tours, 0 shader errors. Fixed: the meadow kept clear of gathering nodes,
  so adding the nodes moved scenery (NodeStreamTests); it ignores them now. A whisper test waited for any line instead of the
  whispered reply. Reruns 5/5.

## 2026-10-07 — Round 27: sims complete (Chris chose the sims before the enclave and Khaven; the Village kit is for Khaven)
- **Your party travels with you and is kept:** its sims are saved with the world (WorldSave.party) on every save and at travel; in
  the next zone or the next session they are at your side again without being asked (SimPopulation.RestoreParty). One who has
  logged off in the meantime has, and the chat says so.
- **Sims invite you:** a friend who wants to group, and a bold sim answering your LFG call, sends a real invitation: "X invites you"
  with Accept and Decline over the action bars for thirty seconds. Accepting brings the sim to you, from another zone by the road.
- **Dungeon runs:** `/dungeon` (or the Lead button with two or more sims along) has the boldest sim lead you into the zone's
  dungeon, Crowsfoot Hollow or the Root-Mother's Deep, camp by camp in the order the passage meets them, the boss last: "Clear. On
  to ...", "That's the last before ...", "... is cleared"; it waits when you fall behind. Too far under the first camp's level, it says so.
- **Trading with sims:** a Trade button on a sim's frame (within 6 m) opens the merchant window on its goods. A sim sells at
  twice an item's value (a merchant asks four times) and buys whatever you bring at its value, half as much again for what its own
  trade uses (ore to a smith), with the coin it has; the goods and the coin really move (SimEconomy). A rival will not trade.
- **Guilds:** four GAME-ONLY guilds (the Lantern Watch, Oak and Ember, the Long Road, the Scree Hounds), each with its own
  character; about two sims in three belong to one, chosen by what they are like (SimGuilds). A guild chat channel (`/g`, green,
  its own filter chip; `/p` stays Party): your guild-mates online anywhere talk in it, welcome you, answer you, say grats on your
  levels and come when you ask for a group there. Joining: a guild sim who knows you (acquaintance or better) whispers an offer
  and sends an invitation (the Accept / Decline panel), and asking "lf guild" in Zone or LFG brings one; recruiting calls in
  Zone. Tags: `<Guild>` on the sims' plates and a Guild column in the who list (O). A guild-mate counts as a friend for joining
  your group. `/guild` lists the members (online first, the guild master marked), `/gquit` leaves. Kept on the character
  (EncounterProgress.guild).
- **Forty sims:** the roster is forty (SimRoster.Count), the new twenty four to each band, so the higher zones are far busier
  (12/9/7/6/6 from Oakhaven to the Shore). An older world keeps its twenty as they are and gains the rest from its seed
  (SimRoster.Grow), and its sims get their guilds once (WorldSave.guildedUpTo).
- Tests: SimPartyTests.The_party_is_kept_and_a_sim_can_invite_you, A_sim_leads_a_dungeon_run_camp_by_camp, Trading_with_a_sim_moves_goods_and_coin, Joining_a_guild_and_talking_in_it;
  SimRosterTests.Forty_sims_the_same_for_a_seed_with_unique_names_and_every_class, An_old_world_grows_to_forty_and_each_sim_gets_its_guild_once.

## 2026-10-07 — Round 28: playtest notes 87 and 89-93 (key hints, chat focus, the wolf and its bar, harder groups, fuller land)
- **87, the key hints:** the line of keys along the foot of the screen is gone. Esc > Controls lists them all, with a line for the
  fixed ones (right-drag to look, wheel to zoom, Enter to chat, Esc for the menu).
- **89, chat focus:** Enter opens the chat box with the keys in it; it keeps taking focus until it has them, the cursor at the end.
- **90, the wolf does nothing:** called, the Ranger's wolf now joins whatever the Ranger fights, and whatever is after the party,
  unasked. Pack Sense turns it at once to what you shoot, its first bite half again as hard.
- **93, the companion bar:** while the wolf is out, a bar left of the action bar shows its health and three orders: Attack (your
  target), Assist (at heel, joining your fights; the default) and Stay (holds its ground, biting only what it is sent at or what bites it).
- **91, groups too easy:** each sim in your party adds a full share of a mob's health (was 60%) and 45% to its blows (was 15%),
  and a call carries half as far again against a party, so a group's pulls are bigger. Alone with Mira, nothing changes.
- **92, deserted land:** every outdoor camp of two or more has one or two outlying groups of its own kind 22-40 m out on open
  ground (never near a village, a road, water, an exit or another camp): the land between camps has mobs on it. Their own stream,
  so the camps stand where they did.
- Tests: RangerLoopTests.The_wolf_joins_the_fight_unasked, The_wolf_stays_attacks_and_assists_on_order.
- **Note 88 (the helms), in hand:** Chris brought Chosan's Modular Hero pack (Asset Store; Modular Hero.unitypackage). Its models
  and palette material are in Resources/Props/ModularHero (the demo scene, prefabs, poses and the editor-only Customiser script left
  out: the script used UnityEditor at runtime and would have broken the player build). Twenty-five helmets (Headgear.B/DS/G/I/M.011-016)
  are model-helm variants (GearLooks.ModelHelms; ActorVisual ModelHelm loads a node of the armour FBX by name) and in the wardrobe
  line-up (06-helms) for Chris to judge before any item wears one. The pack's README names no licence.

## 2026-10-08 — Round 29: the level cap 15 and the bands stretched (Chris: "difficult and slow leveling, grind it out")
- **Cap 13 to 15; the zones Oakhaven 1-5, Khaven 5-9, Peaks 9-12, Ash Rim 12-15, Verdant Shore 13-15** (tools/wip/levels/rescale.py): the
  ordinary camps spread over each band by distance from the village, the nearest lowest; elites a level over their neighbours; cave
  camps past the band's top (Crowsfoot Hollow 6-7, the Root-Mother's Deep 15); quests, bounties, loot lists and items move with their
  zones, the old cap's things to the new cap. The Sealed Adit is 10-12 (adit_layout.py).
- **The Shore is a group zone** ("all epic mobs, group required"): `groupZone` in the zone data; every camp mob there has an elite's
  health and blows and kill XP (EncounterEnemy.Tough) without being an elite. The Rim stays solo-friendly.
- **Slower levelling:** XpToNext is 400 + 170 a level (was 200 + 90): about twice the XP a level, growing faster at the top.
- **Breadcrumbs (Chris):** each zone's last main quest already turns in at the next zone; they now sit at the top of their bands
  (Oakhaven 4, Khaven 8, Peaks 11, the Rim 15 with a level-14 floor), and each next zone's first main quest follows at the band's start.
- **Named and crafted gear re-made for its new level** (tools/wip/levels/regen_named.py): every named piece's weapon damage, armour
  and value are the generated curve's at its new level, its stat points scaled to the budget there (102 pieces); crafted gear is
  required at its tier zone's top level less one (bog-iron 8, ridge-steel 11, ash-steel and veridian 14; the heartwood capstone 14)
  and on the uncommon curve; the skinning and junk tables' level bands and the world drop lists' bands moved with the global map.
  The smiths' chain keeps its rule (no recipe worth more than half again its inputs): bog-iron ore 3 and bars 9, Adit ore 4 and
  ridge-steel bars 11. Crowsfoot Hollow's quest sits at level 6 (offered at 5). The dungeon stays out of the Achievements' zones until
  it has quests and deeds (D6); the Armoury lists its pieces under the Sealed Adit.
- **Save format 10** (SteeperCurveMigration, 9 → 10): a character keeps its level and the way into it under the steeper curve (Chris's
  save backed up first: hel/work/save-backups/2026-10-07-r29-before-curve).
- **The Sealed Adit, first loot and moves** (dungeon D6/D2 first pass, pulled forward by the data tests): loot.adit.json, seventeen
  boss pieces in seven signature lists (DUNGEON_DESIGN.md section 7), the Vent-Hound's and the greyed bears' skinning tables, and a
  provisional EliteMoves entry for each of the seven elites until D2 gives them their mechanics. The Peaks' way in stands on the Ore
  road's end and the Adit has a cart track from the cut to the mouth (the exits-on-roads rule). The Gallery is 21 m down, so no
  knoll heaps over it (a buttress had stood in the Ember Vent's mouth).
- Sim homes, gear bands (GearLooks.BandOf), the wardrobe's set levels (3, 7, 10, 13, 15) and the docs (WORLD_ZONES camp table,
  QUEST_DESIGN section 8, DUNGEON_DESIGN) follow.

## 2026-10-08 — Crowd control: the engine (C1) and the Rogue (Chris: "Archivist and Rogue both sound good")
- The engine (EncounterEnemy.Control): hold, stun, fear and silence on any mob; the same hold again within 18 s is half as long and a
  third time doesn't take; non-boss elites hold 70% as long; bosses take no hold or fear; a held mob answers no call; when a hold
  ends the mob comes back for whoever put it on. Tests: CrowdControlTests.
- The Rogue, the sixth class (GAME-ONLY kit on Valen's CANON line): Focus and combo points. Sinister Strike builds, Eviscerate spends;
  Sap holds a mob 40 s before the pull (out of combat, the mob not yet fighting); Gouge holds 4 s; Kick silences 3 s; Blind sends one
  off 10 s; Vanish makes every mob after you lose sight of you for 6 s (with nobody else to fight it goes home). Unlocks at levels
  1, 1, 2, 3, 4, 6 and 8. Talents: Thief's Grace, Shardwork and Locksmith, 7 each, 6 implemented (Long Sap, Quiet Hands, Keen Edge,
  Ruthless, Quick Kick, Fine Dust). Leather, the Ranger's figure for now, gold class colour, its own icons. Play it from the pause
  menu as a separate character. Tests: RogueRulesTests, RogueLoopTests.
- Not yet: sims as Rogues, pick-lock, a Rogue look of its own; then the Archivist.

## 2026-10-08 — The Archivist, the seventh class (CC step C2b)
- A song-caster of the Silent Pilgrims on Mana (GAME-ONLY kit on CANON roots: the Original Song, the echo-jars), robed like the Mage,
  with a wand. Shard Note (its one damage cast); Lull (the target and up to two more within 6 m sleep 20 s; damage wakes one); Hush
  (silence 5 s); Echo Bind (one mob held 30 s, one at a time); Cadence (you move and Mira casts 10% faster) or Dirge (fighting mobs
  within 10 m slowed), one song at a time; Echo-jar (the target and up to two more within 8 m flee 6 s). Unlocks at 1, 2, 3, 4, 5, 6, 8.
- Talents: the Silent Vow, the Lull and the Archive, 7 each, 6 implemented (Quick Hush, Long Hush, Deep Sleep, Unsong, Resonance,
  A Fuller Jar). Unsong: a sleeper woken by damage is slowed by half for 6 s. Its own icons; play it from the pause menu.
- Tests: ArchivistRulesTests, ArchivistLoopTests.

## 2026-10-08 — Raid marks and a party that leaves your crowd control alone (CC step C3, first part)
- Ctrl+1 skull, Ctrl+2 moon, Ctrl+3 cross on your target, Ctrl+4 clears (one mob a mark); the mark shows above its name.
- Party sims go for the skull first; sims, Mira's bolt and the Ranger's companion leave a held mob, a moon and a cross alone, so a
  Sap, a Lull or an Echo Bind isn't broken by your own party. Tests: CrowdControlTests (marks).
- Still to come in C3: sims marking a pull and putting their own CC on the moon.

## 2026-10-08 — Crowd control for the first five classes (CC step C2)
- Mage: Ash Hex (slot 0, level 6): a 1.5 s cast, a person or beast held as smouldering ash 25 s, one at a time; Quench now also
  silences 4 s (an interrupt).
- Paladin: Rebuke (slot 0, level 6): a 4 s stun, which works on bosses; Censure now also silences 3 s.
- Ranger: Snare Trap (slot 0, level 6): set at your feet for a minute, the first mob to step in is held 20 s; Pin now also silences 2 s.
- Warrior: Shield Bash (level 4): silences 3 s; Shout (level 7): every mob within 8 m flees 6 s.
- Not yet: the Druid's Sleep of the Wood (its ten slots are full; it needs a place on the bar), Mira's Hush, Turn the Unmade.

## 2026-10-08 — Sims as Rogues and Archivists, holding the moon (CC step C3)
- The world has fifty sims: the forty as before, and ten more, five Rogues and five Archivists, spread over the bands (an old world
  gains them, as it gained the second twenty). Rogues fight in melee in leather; Archivists at range in cloth with a pale note.
- In your party, a sim Rogue or Archivist in a fight of two or more holds the moon 20 s: the one you marked, or else it marks one that
  isn't the skull or your target, and says so in Party ("lulling moon", "Moon is held. Leave it be."). Every 25 s at most; never a boss.
- Tests: SimRosterTests (fifty, the first forty unchanged), SimPartyTests (a sim Rogue marks and holds the moon).
- The Druid: Sleep of the Wood (level 6) on a new eleventh slot, the minus key: a 1.5 s cast, the target sleeps 25 s in any form.
- EncounterLoopTests: since round 30 Mira counts as a member of your group, so the tutorial sentries have twice the health and hit 45%
  harder (fights about 40 s, up from 21); the test now rests the Warrior before each fight and allows 120 s.

## 2026-10-08 — The Rogue's and the Archivist's talent trees complete (21 of 21 each)
- Every talent now works, ranks set so each row's gate (5 points a row) can be reached and both capstones bought. Talents that named
  things the game doesn't have yet (locks, wards, mob spell casts) now say what they do today. Highlights: Thief's Grace (once a fight
  a blow that would drop you misses), Cut Deep (a five-point Eviscerate bleeds), Shard Poison, Light Fingers (a Sap lifts coins), The
  Hum (Kick stuns 1 s); the Great Silence (Hush silences everything within 6 m), Mesmer (Echo Bind stuns a boss), Chorus (Cadence makes
  the party hit harder), Quiet Step (mobs notice you closer), the Original Song (the first Lull of a fight takes everything within 10 m).
- Chris (2026-10-08): fights of about 40 s with Mira counted as a member are "not too long"; the balance stays.

## 2026-10-08 — The Warrior's and the Druid's row 3 (the deepest a level-15 character reaches)
- Talent points are your level plus one, so at cap 15 a branch reaches row 3 (15 points in it). Rows 4-6 of the Warrior's and
  Druid's trees wait for the cap-30 levels (ROADMAP Phase 9); every row a level-15 character can reach now works.
- Warrior: Held Line (Guard blows give Vigor and threat), Iron Reserve (Vigor below half health), Press the Breach (free 2-pressure
  Strikes after a Breach), Battle Rhythm, Answering Step (an Intercepted attacker is Exposed), Second Wind (Muster gives Vigor).
- Druid: Deep Grain, Bristle Roar (roared enemies hit you softer), Tracker's Shift, Open Veins, Rain Root, Heartwood Reading,
  Deep-root Snare, Quartzine Seed. Each says what it does now (some were written for systems not built yet).
- The four talent files keep one talent a line.

## 2026-10-08 — Crowd control you can see (C4) and casts worth interrupting (C5, first part)
- A ring on the ground under every held (pale blue), stunned (gold), fleeing (violet) or silenced (white) mob, fading in its last
  three seconds; the state and its seconds under the mob's nameplate (not just your target's).
- Mob spells: the Sealed Adit's Ash menders cast Ember Mend (heals a hurt ally 35%), Ash initiates Cinder Bolt (2.5 blows), the
  Sandthrone sapper Short Fuse (a blast on everyone within 4.5 m) and the Hollow Man Grey Drain (heals itself by what it takes). A cast
  bar on the nameplate and the target frame ("interrupt it"); any silence, stun, hold or fear interrupts it, and a silenced mob can't
  begin one. Mira hushes a cast past its first third every 15 s; party sims with an interrupt (Rogue, Archivist, Warrior, Paladin,
  Mage) stop casts in their reach every 12 s and say so. Tests: CrowdControlTests (a ring, an interrupt, a mender's heal).

## 2026-10-08 — The Sealed Adit's bosses get their mechanics (dungeon step D2)
- A boss-phase system (EncounterEnemy.Boss): moments at health marks (stamp, terrify, reset, rally), a call when first attacked, a
  relief patrol when it dies, a knockback on its heavy blow (only ever onto walkable ground), a disarm, a burst round it, a blink.
- You can now be stunned (no moving, no abilities, no swings) and disarmed (no swings or weapon moves; spells still work).
- Gang-Boss Lusk: when he dies, three of his kind walk in. Nix: Spanner-Lock disarms you 4 s. Cinder-Warden Ysolt: Ember Blow
  knocks you back 5 m; Flame Ring bursts round her every 15 s. The Foreman Who Forgot: blinks behind you every 14 s, forgets whom he
  fought at half health, terrifies (a 3 s stun) at a quarter. Quartermaster Sorrel: stamps at two thirds and one third (a 2 s stun)
  and comes back each time with a heavier weapon (+25%). Rail-Captain Danner: his guards step out when he is attacked, and more at half.
- Not yet: Nix's Rock-Eater (the two-part fight), the Weaver's escort, gates and the cage-lift (D3-D4). Tests: BossPhaseTests.
- Knockback fix: it traced from the body a metre above the ground and never moved anyone; it now traces along the ground, and
  short of walkable ground the full distance (a cliff, deep water, a cave wall) it throws you to the edge instead of not at all
  (2026-10-09, first run after losing C:; the test now stands you on open ground beside the elite).
- (The local branch also had six Adit quests and its own Adit-Runner's Kit; Chris chose the cloud branch's seven quests and kit
  instead on 2026-10-09, so those were dropped in the merge.)

## 2026-10-09 — The Sealed Adit's gates, sigils, spirit stone and rare (dungeon step D3)
- Chris's calls: the cage-lift is a gate on the stair (not a new shaft), and the sigils are kept once won.
- **Rail sigils:** Nix carries the Amber Sigil, Cinder-Warden Ysolt the Ember and the Foreman Who Forgot the Grey (`ZoneCamp.key`).
  The first kill by you or your party takes it from the body, kept for good (`Progress.keys`).
- **The cage-lift gate** at the head of the stair under the Gallery: an iron grille that stops you, Mira, the sims and the mobs
  (`ZoneGate`: a collider and a carved navmesh obstacle). The frame on its left post has three sockets; with all three sigils, E sets
  them, they light, and the grille climbs into the frame. It stays open on every later visit ("adit.lift").
- **The platform gate** at the stair's foot, shut again on every visit:
  - quiet: with "The Pressed" handed in, a goblin you freed hums its lock open and the hall never hears;
  - loud: a keg of blasting powder from up the stair blows it, and the Quartermaster sends his guards and the platform guards at you.
- **The Gallery's spirit stone** (right wall, just inside): touched once, you wake beside it after a fall in the Adit, not out in
  the yard (`Progress.spiritStones`, `RecoveryPoint`).
- **The Quiet Miner** (12, elite, GAME-ONLY): a rare camp in the Grey Breach's drift past the Hollow Men, there one visit in five
  (`ZoneCamp.rare`; last in the list, so no other camp moves; no achievement waits on it). No loot of his own yet.
- A sim leading a run stops at a shut gate and says so; with the gates open it leads the whole way as before.
- Tests: AditGateTests (new: shut gates and the sim's stop, the sigils and the lift kept over a reload, loud and quiet ways, the
  stone, the rare); AditTests and ZoneContentTests open the gates before walking the whole zone and allow for a rare camp.

## 2026-10-09 — Quest gear handed out at turn-in (Chris: "do quest gear rewards first")
- The eighteen named quest pieces were never given (each named its quest as its source; nothing granted it). Now a quest's piece goes
  into the bags when you hand it in, and The Dead Line offers a choice of three (Sleeper-Splitter, Coupler's Hook, Weaver's
  Tuning-Drop): the conversation's Rewards show each piece's square, name, slot and level with its tooltip (and the compare lines),
  a click picks one, and Complete waits for the pick. The quest book lists them in words ("Your choice of: ...").
- A piece already held is paid in crowns ("You've one already. Take the coin."); full bags make the quest wait. The Tempered
  Trailblade (legacy; every new character has it) is not given again by the first Chronicle chapter.
- Tests: QuestGearTests (EditMode: the eighteen and their quests, a single piece, the choice, held, full bags).

## 2026-10-08 — The Sealed Adit's quests and loot (dungeon step D6; cloud branch web/adit-quests-loot, not yet run in Unity)
- Quests/adit.json: the seven quests of DUNGEON_DESIGN.md section 6, all side quests from level 10:
  - Under the Toll (Yara Quell, 11): Gang-Boss Lusk and his tally book (a Chronicle page) to Yara.
  - The Pressed (Pib, 11): open the five goblin cages in the Workings.
  - Echoes in the Stone (Brother Cael, 11): six echo-jars from the Singing Gallery; his page of what they held.
  - Embers Below (Tamsin Rook in Khaven, 11): Cinder-Warden Ysolt's brand.
  - What the Grey Takes (Lisle Tamber, 11): a shard of unmade stone from the Foreman Who Forgot.
  - The Dead Line (Yara, 12, after Under the Toll): the Quartermaster and the Rail-Captain; Danner drops his sealed letter.
  - A Letter Under Seal (Yara to Lisle, 12): the letter's page, the hook for the next dungeon.
- New people (GAME-ONLY, residents keeping a post): Pib, Brother Cael and Lisle Tamber at the Pilgrims' Rest and the Listening Shrine
  (peaks.json); Tamsin Rook by Khaven's gallows tree (khaven.json). Five quest items with icons; three Chronicle pages.
- ZoneBuilder.Adit.cs: the Workings' three cages are now five (two on the right wall) and usable ("Open the cage", once each); seven
  echo-jars stand round the Gallery's floor ("Take the echo-jar", back after 90 s). No draws from the cave's random stream, so nothing
  else in the cave moves.
- loot.adit.json: the Adit-Runner's Kit, a five-piece rare set (hood from Nix, mantle from Ysolt, boots from the Foreman, gloves from
  the Quartermaster, jerkin from Danner), each a 20% lucky roll beside the boss's signature list; +50 health at three pieces, +10
  attack power and 20 health a kill at five. The Dead Line's three rewards (Sleeper-Splitter, Coupler's Hook, Weaver's
  Tuning-Drop), quest-sourced like the other zones' quest pieces. Icons for all eight and the five quest items are in
  tools/art/make_icons.py, to be painted locally (the cloud can't push Git LFS).
- Tests: QuestDataTests (the seven quests' data, and a play-through on the quest log); LootDataTests and NamedLootTests counts
  (135 named, 147 gear entries, 74 rares, three sets, 18 quest and 62 boss pieces); the sets test allows a five-piece set from three.

## 2026-10-09 — The Weaver's unlocking: Mother Quillet's escort in the Rail Hall (dungeon step D4)
- **Mother Quillet** (GAME-ONLY; the goblins are CANON people): with "The Pressed" handed in, the goblin who hums the platform gate open
  on the quiet way is the old one from the fifth cage, and she stays by the gate. Spoken to, she walks down the platform to the
  carriage's resonance lock (an iron plate with a violet eye on the cab's platform side) and matches it, about half a minute's work.
  - She is one of your party for every mob, for Mira and for the sims (`PartyActor`): mobs on her count as your fight, sims attack
    them, Mira and a sim healer heal her. A row under the party frame shows her health and the walk or the lock in per cent; she has a
    name over her head.
  - She waits while you are more than 16 m behind or something is on her within 5 m, and stops matching while you are down.
- **Two waves** (`EncounterSession.Weaver.cs`): three platform guards drop off the back of the carriage a third of the way down,
  three dockers come out of the far end at two thirds. They come for her; they belong to no camp, so they call nobody and answer
  nobody, and they carry their camp's loot.
- **Rail-Captain Danner** lies in the dark while the escort can still happen (a mob lying in wait, not in grass, is now deaf to calls,
  so neither his guards' fight nor the Quartermaster's dying call draws him out); he stands up when the lock is half matched and comes
  for her (`EncounterEnemy.Rise`). Walk up to him and he still springs out.
- **Matched:** the carriage's geodes and its light go dark (`ZoneBuilder.QuietCarriage`), and "adit.lock" is kept for good: dark on
  every later visit, Danner on his feet, nobody at the gate. If she falls, whatever was on her lets go, the waves still standing go back
  into the dark, and 30 s later she is by the gate, whole, to start again.
- **A new quest, The Weaver's Lock** (Pib, 12, after The Pressed; 760 XP, 60 crowns): its step is a flag objective, `key:adit.lock`
  (the session's quest flags now read any `key:<id>` in `Progress.keys`). Eight Adit quests now.
- Tests: AditWeaverTests (new, PlayMode: the quiet gate brings her and Danner lies in wait; the walk with both waves on her, Danner at
  half the lock, the geodes dark and kept over a reload; her fall and return); QuestDataTests for the eighth quest and its flag.
- Full run: EditMode 440/440, PlayMode 254/254 (every fixture), build and the Adit's tour clean, 0 shader errors. The full run's lane B
  builds a Development player (the tours need it); tools/validation/release_build.ps1 now makes the release build that gets published.

## 2026-10-09 — Nix and the Rock-Eater: the two-part fight (dungeon step D5, first part)
- **The Rock-Eater** (GAME-ONLY): the goblin boring-engine is no longer a prop in the Geode Floor's workshop but a second elite of
  Nix's camp, with a figure of its own (`ActorLook.RockEater`: a rust hull on tracks, the pilot's seat, a stack, a turning drill) and
  its own move, Bore (a drill blow that throws you back 4 m; its horn calls the workshop guards). Nix's camp spawns it beside him.
- **Nix rides inside it** (`EncounterEnemy.HideInside`): unseen, untargetable, beyond harm and deaf to calls; walking up to the engine
  brings nothing out. When the engine breaks he jumps out at the wreck with his spanner and comes for whoever broke it (Spanner-Lock,
  the 4 s disarm, as before). Together the two are about one and three-quarter elites of health (the engine .95, Nix .8).
- The Amber Sigil and the camp's deed come from Nix alone: a camp's key and deed now go to its *named* elite (`NamedElite`), and the
  machine's id names no camp, so its corpse rolls plain loot, not Nix's pieces. Both come back with the camp, Nix inside again
  (`EncounterSession.EnemyRespawned`). A dead machine stays where it stopped, tipped a little.
- Tests: RockEaterTests (new, PlayMode: inside, out at the wreck and on you, the sigil and deed from Nix, both back with Nix inside);
  BossPhaseTests checks the Bore's knockback.
- Docs: CLASS_STATUS.md (new): the seven classes' state, complete for cap 15, what the cap of 30 needs.

## 2026-10-09 — Mobs that bolt, shooters that shoot (dungeon step D5, the rest of it)
- **Flee at low health** (`ZoneCamp.flee`; `EncounterEnemy.Flee.cs`): a camp's mobs bolt once their health is under that fraction, running
  from whoever they fight for 6 s, then come back for them (their threat is kept). Once a fight only. Not a hold: the nameplate says
  "Fleeing", they answer no call while they run, and the party's helpers may chase them. The Grey Breach's crawlers bolt at a quarter
  (tools/wip/dungeon/adit_layout.py writes it); elites never do.
- **Shots:** the yard's Sandthrone pickets open with a Crossbow Bolt at 20 m (every 9 s), and the carriage gunners fire a Rail-shot at
  25 m (every 7 s): MobCasts, interruptible like any cast. They still close to melee between shots: a mob that keeps its distance is
  not built.
- Of DUNGEON_DESIGN.md's section 5 that leaves: a sleep put on you (no mob sings it yet), a mob's fear on you (the Foreman's terrify
  stands for it), the picket hound, cave-bats and the ember elemental (not in the camps).
- Mira's Hush was already built with C5 (HealerCompanion.Hush); the handoff's "not yet" was stale.
- Tests: MobBehaviourTests (new: the crawlers' bolt and return, the shots); the camp field in the zone data tests.

## 2026-10-09 — Sims in the dungeon: waiting at gates, guarding the Weaver (dungeon step D7)
- A sim-led run (/dungeon, Lead) no longer ends at a shut gate: its route keeps every camp, each with the gate across the way before
  it (`EncounterSession.GateAhead`); at a shut one the sim walks to the near side, waits ("Waiting at the cage-lift gate") and says what
  the gate wants (the E prompt: "Set the rail sigils (2/3)"); when it opens the run goes on to the first camp past it. `StoppedBy` now
  names the shut gate ahead (null once open); `CampsLeft` counts every camp. A sim waiting at a gate pulls nothing: the camp past it is
  not its to start (it used to close on any camp mob within 16 m of where it stood, through the rock).
- While Mother Quillet walks to the lock, every party sim guards her (a post round her, "Guarding Mother Quillet") instead of
  following you; mobs on her were already theirs to fight (D4). The run's camps resume after.
- Tests: AditGateTests (the run waits at the lift and goes on when it opens; every camp on the route); AditWeaverTests (a sim guards
  her on the way).

## 2026-10-09 — The rail ride out (dungeon step D8)
- With the lock matched, E at the carriage's gangway ("Ride the carriage up the line (one ride out)") puts "Riding the line" on the bar
  for five seconds (moving or a blow stops it, as with any work), then you, Mira and the party's sims stand at the siding under the
  Adit yard, the fights behind you reset, and the game saves. Before the lock is matched the carriage is the Company's and says so.
  The carriage itself does not move on screen yet (a later look).
- Tests: AditGateTests (the ride refused while the lock sings, taken once it is matched; the party arrives at the yard).
