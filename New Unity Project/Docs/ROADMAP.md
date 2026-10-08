# Project roadmap

Updated 2026-10-05 (rewritten: the 2026-09-28 version had fallen behind). PROJECT_MASTER.md remains the design brief; this file
records where each of its phases stands and the order of the work. Update it every round that moves a phase.

## Phases (PROJECT_MASTER.md)

| Phase | Goal | Status (2026-10-05) |
|---|---|---|
| 0 | Foundation | Done |
| 1 | Combat sandbox | Done: shared abilities, threat, elites, level scaling (mobs above you hit harder, red at +3) |
| 2 | Class loops | Warrior, Druid, Paladin, Ranger and Mage with talent trees; Mira's healer kit |
| 3 | Inventory, items, vendors | Done: bags, gear and looks, named loot and legendaries, vendors, professions and crafting, chests |
| 4 | Questing | Done: five zones of quests, the chronicle, notice boards and bounties, discoveries, achievements |
| 5 | **SimAdventurers** (the brief's "most important feature") | **Now** (see below) |
| 6 | Group gameplay | You and Mira only; full parties come with 5.6 |
| 7 | First dungeon | Done: Crowsfoot Hollow (Oakhaven) and the Root-Mother's Deep (Verdant Shore) |
| 8 | Vertical slice | In progress alongside: world art, polish and the playtest-note rounds |
| 9 | **The wider world** (Chris, 2026-10-07: "more zones, more classes, more races based on the canon of the novels, after phase 8") | Planned (see below) |

## Phase 5 plan (Chris chose 2026-10-05: Phase 5 now; classes Paladin, Ranger, Mage; faster tests first)

| Step | What gets done | Size |
|---|---|---|
| 5.0 Groundwork (DONE 2026-10-05) | Faster test runs: run only the tests a change touches; one scene load per test fixture; wait-until instead of fixed waits; PlayMode split across two validation copies. Landed as: select_tests.ps1 (only the fixtures a change names; NONE for art and docs) and two lanes (tests in encounter-validation, build, tours and captures in encounter-validation-b at the same time); a docs-only round took 12 min. Shared scene loads and wait-until left for later (most fixture time is simulated village days, not loading). | 1 round |
| 5.1 Class kits (DONE 2026-10-05: Paladin, Ranger, Mage) | Paladin, Ranger, Mage: 8-10 abilities each, an AI rotation, gear rules and looks, playable by the player too (full talent trees later). Names provisional until checked against the books. | 3 rounds |
| 5.2 Sim profiles (DONE 2026-10-06: the roster, the world slot, figures in the zone by the clock; gear on them and a who list; mobs scale to your group) | ~20 persistent SimAdventurers (stable ids; name, race, class, level, gear, personality, home zone), saved; materialised as figures in the player's zone, dematerialised when away. | 2 rounds |
| 5.2b Invite (Chris moved it up, 2026-10-06) | Click a sim, Invite: it joins like Mira (up to three), follows, fights your target in its class's way (melee closes, Ranger and Mage shoot, Druid and Paladin also heal), is in the party frames and can be turned on; Leave party sends it back. Not saved. The full party system stays in 5.6. | 1 round |
| 5.3 In-world life (DONE 2026-10-06: 5.3a choosing, hunting, gathering, the inn, tagging; 5.3b levelling, death and the corpse run, roads between zones; 5.3c trades, the stall, upgrades, the forge) | Utility-AI activities: questing (camp mobs), gathering, travelling the roads, shopping, resting at inns, dying and the corpse run, levelling and gearing up; personality weights the choices. | 3 rounds |
| 5.4 Offscreen world (DONE 2026-10-07: the unseen hunt, gather, trade and level by the clock, and take the roads between zones) | Coarse simulation of the unloaded: levels, zone moves, online and offline hours by the world clock. | 1-2 rounds |
| 5.5 Chat and memory (DONE: zone chat 2026-10-06; whispers, friends and memory 2026-10-07) | Say, Zone, Whisper and System channels from personality and event templates; a who list and friends; compact social memory and relationships (Stranger to Friend or Rival). | 2 rounds |
| 5.6 Groups (DONE 2026-10-07: invite, frames, need/greed, roles, assist, sims asking you, runs to camps) | Invite, accept, leave, party frames, roles, assist, shared kill credit, need and greed, sims inviting the player, dungeon runs with sims. | 3 rounds |
| 5.7 Sims complete (Chris, 2026-10-07: "I want the sims complete first", before the enclave and Khaven) | 1. the party travels with you and is kept (DONE round 27); 2. sims invite you (DONE round 27); 3. dungeon runs led by a sim (DONE round 27); 4. trading with sims (DONE round 27); 5. guilds (chat, tags, joining) (DONE round 27); 6. a bigger population (about 40, more at the high zones) (DONE round 27). | 4-6 rounds |

About 15-17 rounds. No external language model: local state machines and utility AI only (brief section 13).

## Phase 9 plan: the wider world (after Phase 8; PROVISIONAL until each item is checked against the books)
Everything here is drawn from the lore folder (D:\code\crulanda: world_bible.md, the three books) and labelled as the game labels lore:
CANON where the books name it, CANON-EXPANDED where the game fills in round a canon place, GAME-ONLY for the rest.

| Step | What gets done | Canon source |
|---|---|---|
| 9.1 Zones | New zones on the world map, each with its band, quests, camps, trades and a dungeon or landmark: the Lowtowns of Argentis (industry and poverty under the Crown), the Crown itself (white stone and gold), Port Caelum and the Glittering Coast (glass ships, the Gilded Bay), the Iron Citadel (the Iron Pact's fortress-monastery in a dormant volcano), the Archive of Silence (the Silent Pilgrims' library-temple), Hallow's Creek (the first town the Wasting took), the Warrens (the goblins' tunnels under the High City), the Tide-Watched Shores (blue water, no ash). Order and bands to be chosen with Chris. | world_bible.md Geography; book1 (the Lowtowns, the Deep Veins, the Warrens); book2 (the Wasting) |
| 9.2 Classes | New class kits with talents, AI rotations, looks and sim support, modelled on the books' people: the Tinker (Klyther Forgeheart: drones and scorpions, an engineer), the Rogue (Valen: Thief's Grace, shards, vibration-locking), the Knight (Talira Frostveil, Kaelith Dawnstrike: swordmaster, Stormlight), the Archivist (the Silent Pilgrims: the Original Song, echo-jars), the Salt-Mender (salt-magic of the West). Names provisional. | world_bible.md Characters and Factions |
| 9.3 Races | Playable and sim folk beyond the human regions: the goblins (the Weavers of the Warrens: vibration-locking, static-charting), the Forgeborn (Tynara Embercoil), the Veridian Keepers (beings of wood and moss; a Keeper playable only if the books allow), and the human peoples by region (Lowtowners, Coast folk, the North's survivors). Each with models, looks, voices in chat and a home zone. | world_bible.md Factions; the character sheets in chars/ |
| 9.4 The sims in the wider world | The roster grows with the zones (more bands, more homes); guilds and trades reach the new places. | ROADMAP 5.7 |

Each step is its own set of rounds; Chris picks the first zone, class and race when Phase 8 closes.

**Level cap (Chris, 2026-10-07): 30 in the end, 15 now; "difficult and slow leveling, grind it out".** Round 29 (after round 28):
the cap goes from 13 to 15 and the zones' bands stretch to Oakhaven 1-5, Khaven 5-9, Peaks 9-12, Ash Rim 12-15, Verdant Shore 13-15
(two zones at the cap, "so endgame has more to do"; the Shore "all epic mobs, group required": every mob there elite-strength, a zone flag; the Rim "more solo friendly"); every camp, quest and loot list moves with its zone, Crowsfoot Hollow to about
4-6 and the Sealed Adit to 10-12 (DUNGEON_DESIGN.md); the XP curve steepens (about twice the XP a level, growing faster at the top);
the sim roster's home bands, gear tiers, the wardrobe captures and the tests follow. **Breadcrumbs (Chris, 2026-10-07): each zone's last main quest already turns in to someone in the next zone (QUEST_DESIGN.md: Oakhaven to Khaven to the Peaks to the Rim to the Shore); round 29 sets each of those at the top of its new band, checks the chain reaches it, and makes sure the next zone's first quest offers itself on arrival, so the trail leads through the bands in order.** Levels 16-30 come with the Phase 9 zones, each
new zone carrying its own band, so the cap rises as the world does.

## Queued around Phase 5
- The Ash-Walker enclave's interior (playtest note 40).
- Art rounds from Chris's assets (Fantasy Props MegaKit, Medieval props and the Stylized Nature MegaKit's trees in, 2026-10-07): Medieval Village MegaKit, Stylized Megapack
  2in1, Medieval props, HQ Rock Pack, the Ashen Marches sentinel helm.
- Sound: ambience, weapon hits (slash, pierce, blunt each different), spells, mobs; music later.
- Standing: playtest notes as they come (Docs/PLAYTEST_NOTES.md), frame rate, cave lighting (note 10).

## Validation policy
Every round: compile offline, the tests the change touches (all of them once 5.0 lands for core changes), a player build and
the tours the change can be seen in, then publish, commit and back up (CLAUDE.md). Chris plays the published build; his notes
open the next round.

## Ideas for later (Chris)
- **Seasons (Chris, 2026-10-07, playtest note 85):** a year turning through the days. Summer: stamina and water
  drain faster (a water supply to keep up). Winter: snow on the ground in the high and northern places; the cold takes health and
  stops it coming back until you are warm (a fire, an inn, warm clothes). Builds on the crops' round (CropField), WorldWeather and
  WorldClock.
