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
- **Umbra Scarp** became a stepped course of blocks. This REGRESSED: it reads as a wall of smooth grey slabs, and is redone in the next round.
- Tests: EditMode 166/166, PlayMode 51/51, 0 shader errors.
