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

## Phase 5 plan (Chris chose 2026-10-05: Phase 5 now; classes Paladin, Ranger, Mage; faster tests first)

| Step | What gets done | Size |
|---|---|---|
| 5.0 Groundwork (DONE 2026-10-05) | Faster test runs: run only the tests a change touches; one scene load per test fixture; wait-until instead of fixed waits; PlayMode split across two validation copies. Landed as: select_tests.ps1 (only the fixtures a change names; NONE for art and docs) and two lanes (tests in encounter-validation, build, tours and captures in encounter-validation-b at the same time); a docs-only round took 12 min. Shared scene loads and wait-until left for later (most fixture time is simulated village days, not loading). | 1 round |
| 5.1 Class kits (DONE 2026-10-05: Paladin, Ranger, Mage) | Paladin, Ranger, Mage: 8-10 abilities each, an AI rotation, gear rules and looks, playable by the player too (full talent trees later). Names provisional until checked against the books. | 3 rounds |
| 5.2 Sim profiles (round 1 DONE 2026-10-06: the roster, the world slot, figures in the zone by the clock; round 2: gear on them, their own levels and names shown in a who list) | ~20 persistent SimAdventurers (stable ids; name, race, class, level, gear, personality, home zone), saved; materialised as figures in the player's zone, dematerialised when away. | 2 rounds |
| 5.3 In-world life | Utility-AI activities: questing (camp mobs), gathering, travelling the roads, shopping, resting at inns, dying and the corpse run, levelling and gearing up; personality weights the choices. | 3 rounds |
| 5.4 Offscreen world | Coarse simulation of the unloaded: levels, zone moves, online and offline hours by the world clock. | 1-2 rounds |
| 5.5 Chat and memory | Say, Zone, Whisper and System channels from personality and event templates; a who list and friends; compact social memory and relationships (Stranger to Friend or Rival). | 2 rounds |
| 5.6 Groups (Phase 6) | Invite, accept, leave, party frames, roles, assist, shared kill credit, need and greed, sims inviting the player, dungeon runs with sims. | 3 rounds |

About 15-17 rounds. No external language model: local state machines and utility AI only (brief section 13).

## Queued around Phase 5
- The Ash-Walker enclave's interior (playtest note 40).
- Art rounds from Chris's assets: Stylized Nature MegaKit, Medieval Village MegaKit, Fantasy Props MegaKit, Stylized Megapack
  2in1, Medieval props, HQ Rock Pack, the Ashen Marches sentinel helm.
- Sound: ambience, weapon hits (slash, pierce, blunt each different), spells, mobs; music later.
- Standing: playtest notes as they come (Docs/PLAYTEST_NOTES.md), frame rate, cave lighting (note 10).

## Validation policy
Every round: compile offline, the tests the change touches (all of them once 5.0 lands for core changes), a player build and
the tours the change can be seen in, then publish, commit and back up (CLAUDE.md). Chris plays the published build; his notes
open the next round.
