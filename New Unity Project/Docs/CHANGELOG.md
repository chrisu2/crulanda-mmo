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
