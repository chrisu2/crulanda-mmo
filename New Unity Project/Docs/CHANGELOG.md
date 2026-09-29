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
