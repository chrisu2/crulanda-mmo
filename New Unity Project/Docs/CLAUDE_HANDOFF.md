# Crulanda — handoff (current state)

Updated 2026-10-11, 00:40 (America/New_York). One page on purpose: what is true now, where things are, and everything left to
do, phase by phase. History lives in CHANGELOG.md and HANDOFF_ARCHIVE.md. Replace this page when the state changes; do not stack.

## Read first
1. `CLAUDE.md` (repo root): the rules and where everything is.
2. `GAME_BRIEF.md`: what the game is and the bar it is held to.
3. This page, then `PLAYTEST_NOTES.md` (Chris's reports and their status), `DUNGEON_DESIGN.md` section 0 (the dungeon brief,
   verbatim), `CHANGELOG.md`, `WORLD_ZONES.md`, `QUEST_DESIGN.md`, `SAVE_FORMAT.md`.
4. The memory notes (C:\Users\ChrisWin11\.claude\projects\D--code-mmo\memory): reports stay open until Chris confirms them in his
   own build; record his briefs verbatim the same day; **check the clip import before touching animation**; nothing is done until
   verified (Chris, 2026-10-11).

## Where things are (CLAUDE.md is the authority)
- Repo `D:\code\mmo` (branch `main`; tonight's work is on `feet-bow-adit-kit`), the Unity project `D:\code\mmo\New Unity Project`.
- GitHub `https://github.com/chrisu2/crulanda-mmo`; PR #2 (`feet-bow-adit-kit` into `main`) is open. **Do not merge it until Chris
  has played the build.**
- Work folder `D:\crulanda-work`: validation lanes `encounter-validation` (A) and `encounter-validation-b` (B), the playable build
  `outputs\Crulanda-Playable\Crulanda.exe`, captures in `world-captures` and `ui-captures`, downloads in `downloads`.
- Backup: `tools\Backup.ps1` to `E:\claude\unity projects\mmo` (E: is the backup drive; J: is gone).
- Chris's save: `C:\Users\ChrisWin11\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter` (back it up to
  `D:\crulanda-work\save-backups` before any save-format change).
- Lore: `D:\code\crulanda` (books 1-3, the world and series bibles).

## The game now (from the code)
- Zones: Oakhaven (1-5), Khaven Village (5-9), the Shattered Peaks (9-12), the Ashland Rim (12-15), the Verdant Shore (13-15), and
  the first dungeon, the Sealed Adit (10-12, its own zone, entered from the Peaks).
- Classes: Warrior, Druid, Paladin, Ranger, Rogue, Mage, Archivist (seven, with talent trees). Level cap 15 (30 in the end).
- Save format 10 (`EncounterSave.FormatVersion`).
- Characters: Superhero bodies with outfit models, Kevin Iglesias's clips (locomotion, jump, archery), Morro Motion's idles.

## How status is written here (Chris, 2026-10-11: "dont count it done until verified")
- **VERIFIED**: Chris has confirmed it, in his build or in a capture he looked at.
- **TESTED**: automated tests pass on it (the date says when); Chris has not checked it.
- **BUILT**: in the code; no test run on it since and Chris has not checked it. The older docs' "DONE" and "FIXED" mostly mean this.
- **OPEN**: not done.
Nothing counts as done until it is VERIFIED. Read the clock (`Get-Date`) before writing any time.

## Where the work stands (2026-10-11, 00:33)
- **Published 00:33** (release build, 0 shader errors, from commit 9fc71fc; the new code and the new layout were checked in the
  published files): the drawn-out Sealed Adit. **Chris has not walked it yet.**
- VERIFIED in captures only (Chris, 2026-10-10): the feet (the clip import; "finally looks fixed"), the jump's landing (notes 107 and
  111), the bow facing its target ("bow looks better"). Still to see in his own build.
- TESTED (2026-10-11, 00:10-00:32): the Adit drawn out three times (1,353 m of cave, the zone 800 m square; its built halls walked by
  AditTests); the Weaver's escort on it (waves by her distance to the lock, Danner, the geodes going dark and kept after a reload);
  saves deep in the Adit (a save in the Rail Hall, 80-90 m down, came back a new character: the limit was 60 m, now 250); the
  Quartermaster's call at 30 m (it may not bring Danner); quest gear at turn-in (QuestGearTests). EditMode 445/445; AditWeaverTests 3/3,
  AditTests 5/5, AditGateTests 8/8, RockEaterTests 2/2, CaveTests 9/9, EliteFightTests 6/6.
- BUILT (2026-10-10, not seen by Chris): the room builder's halls (the Geode Floor's foundry hall and the Rock-Eater's berth, the Ember
  Vent's chapter-house); the Hollow Men as apparitions; Chris's lore decisions in the data (the Salt-Menders as forerunners, the
  Stillwood, the toll CANON-EXPANDED, Klyther as in the novels).
- **The full PlayMode set has not run on this branch.** WEB_LOG.md records 245/245 on 2026-10-09, but no results file for that run
  is on disk (the oldest is 2026-10-10 21:52), so even that is unproven. Tonight only the Adit suites ran; the feet import, the bow,
  the room builder, the scale-up and the lore renames reach further than those.

## To do, every phase
Phases 0-7 are built (ROADMAP.md: foundation, combat, seven classes, items, quests, sims, groups, the first two dungeons); what of
them is not verified is under "Verify first". Phase 8 is now; 8.5 and 9 follow.

### Verify first
1. Chris walks the drawn-out Adit (published 00:33). His word before PR #2 (`feet-bow-adit-kit`) is merged.
2. The full PlayMode run on the branch, then the zone tours (`build_and_tour.ps1`); fix what fails before the merge.
3. In Chris's build: the feet, the jump and the bow (verified only in captures); note 108 (the bow in play); 109 (tools carried).
4. Reports worked on that Chris never confirmed: 23 (frame rate: 145 fps measured in Oakhaven at 1440x900; the target is 144+ and he
   plays at 3440x1440), 24 (dead-tree joins), 25 (swings: an upper-body layer only; real swings need the Melee pack), 26 (roofs fade),
   27 (crowns, not "gold"), 28 (the horse kept out of the inn), 29 and 30 (drinking, sitting), 39 (crags, "to look at again"), 43 (red
   mobs hit harder), 57-61 (round 23, marked UNTESTED: straw hats, cows' colliders, the character sheet, sign text, farmers working),
   99's done parts (the Adit's first look), 106 (the horizon, a first pass). **In all, 79 notes in PLAYTEST_NOTES.md are marked DONE or
   FIXED by the agent and only 2 are confirmed by Chris (107 and 111, in a capture):** the real list to verify is those 79, the ones
   above first. Go through them in play with Chris.
5. Docs that disagree with the code: DUNGEON_DESIGN section 9 step 6 says quest gear is not handed out (it is: QuestLog.GearRewards,
   TESTED); CLASS_STATUS.md lists Mira's Hush as left (it is in: HealerCompanion.Hush, C5); ROADMAP 8.5 says the game has no archery
   clips (Kevin's Archer clips are in, in part); the ROADMAP's status column is dated 2026-10-09; KNOWN_ISSUES.md's top half is from
   Phase 0; GAME_BRIEF.md's header still says "Updated 2026-09-30" (it was changed 2026-10-10), its Verdant Shore paragraph still says
   levels 11-13 and the cap rising to 13 (corrected only in a parenthesis), and "the painted style pass is next" is dated 2026-09-30.

### Phase 8: the vertical slice (now)
**8A. The Sealed Adit (DUNGEON_DESIGN.md)**
1. **"More to do"** (note 113, section 0.25), a wing at a time, frames shown to Chris before the next:
   - [1] the Old Workings: patrols on the long drifts (no mob walks a patrol yet: build that first), ambushes from side galleries, a
     flooded lower working with a mini-boss, a collapsed-shaft shortcut back. OPEN.
   - [2] the Singing Gallery: a multi-level water cavern (ledges, pools, a fall), sleep-casters and corrupted beasts, an optional boss
     off the path, a jump-down shortcut to the hub. OPEN.
   - [3a] the Geode Floor: a refinery of crushers and stampers to time your way past; an overseer before Nix and the Rock-Eater. OPEN.
   - [3b] the Ember Vent: four wings with an end boss each (an urn-keeper who raises the dead, a loremaster, a drillmaster, a priestess
     who raises Ysolt once); packs of fanatics that heal each other. OPEN (the halls are BUILT).
   - [3c] the Grey Breach: the grey spreading deeper in; the Foreman; the Quiet Miner (rare). OPEN.
   - [4] the Great Cove: the line on into an underground harbour, a moored ore barge, its cannon fired the loud way, the last boss on
     deck, a hidden boss below. OPEN.
2. Note 110: dress the Adit and rebuild its built rooms from Chris's 41 modular pieces (raw cave stays rock). In progress.
3. Loose ends of the build steps (section 9), all OPEN: the Foreman's short way back to the Gallery; the carriage moving on screen on
   the ride out; the carriage's roof; sims joining the escort on their own; a mob that sings a sleep on you (with the Gallery's
   sleep-casters); Geode Shards (trash material and a smithing recipe); the hidden and rare bosses' pieces (Old Kettle, the Quiet
   Miner); the dungeon's own achievements (to decide); the Sandthrone commander's canon name in the letters (PROVISIONAL).
4. Note 99's open part: a mob answering a call fights with its weapon sheathed.
5. Note 10: cave lighting, baked and more atmospheric.

**8B. The tester's report, notes 114-118 (Chris pasted it 2026-10-10, about 22:20), all OPEN.** Each with a test that measures it,
frames, and Chris's word:
- **115** keys ignored after alt-tab or chat until a click in the window (leads, not tested: the chat's "typing" flag is only cleared
  while the chat box is drawn; the input system's default turns the keyboard off when the window loses focus: no InputSettings asset,
  runInBackground off).
- **117** the camera going inside walls in buildings.
- **114** the HUD clipped at the left and top (chat and party frame cut; no player frame or minimap seen).
- **116** the shop not closing on "Goodbye".
- **118** "/invite Thatch" fails (the Who list's Invite works).

**8C. Animation**
- Note 112, OPEN: (a) at full draw the bowstring is not drawn to the hand and no arrow shows; (b) Morro Motion's idles turn a foot out
  about 50 degrees (import them from a source avatar, as Kevin's are); (c) the walk's toes dip 3-6 cm in places.
- Kevin Iglesias's Melee pack: not imported (copies in `D:\crulanda-work\downloads\kevin-iglesias`). Real swings for every class (the
  rest of note 25): into a validation lane first, the clips copying his avatars, never into Chris's open project.
- Kevin's Archer pack: only BowShot01 (load, hold, release) and BowIdle01 are in.

**8D. Classes and crowd control (CLASS_STATUS.md, CC_DESIGN.md, TALENT_DEPTH.md)**
- Fourth paths to design and build: the Ranger's Skirmisher (melee), the Mage's Cinderwright (pets), the Rogue's Swashbuckler
  (support). The Paladin's Vanguard, the Warrior's Jailer and the Archivist's Cantor: TESTED in EditMode 00:25 tonight
  (PaladinRulesTests, WarriorTalentTests, ArchivistRulesTests); their PlayMode tests (PaladinLoopTests, CrowdControlTests,
  ArchivistLoopTests) have not run tonight (the full run covers them); Chris has not played them. The Druid has four.
- Own figures for the Rogue and the Archivist (they borrow the Ranger's and the Mage's). OPEN.
- Turn the Unmade (a Paladin fear against the Wasting's things). OPEN.
- Crowd control: C1-C4 and C5's casts to interrupt BUILT (2026-10-08), with Mira's Hush; their tests are PlayMode
  (CrowdControlTests, ArchivistLoopTests), not run tonight. C5's "pulls re-tuned around CC" has no record: check it.
  C5's "the dungeon deepened" is the scale-up (TESTED).
- Note 42 (a better shield off wolves than off the Brood Weave-Eater): waits for Chris to name the two shields.

**8E. World and art**
- The painted style pass (GAME_BRIEF's art direction), zone by zone. OPEN.
- Note 40: the Ash-Walker enclave's interior (a firelit hall behind the middle arch; only its cave mouths exist). OPEN.
- Khaven with the Village kit (imported 2026-10-07 into Resources/Props/Village; check what Khaven uses before calling it done).
- Daniel Gruginski's Medieval Village Kit (CC0; ROADMAP): no record of its import.
- Note 14: the PDF of the work (last brought up to date 2026-10-02, `tools/docs/make_history_pdf.py`). OPEN.

**8F. Sound: there is none** (no audio code in the game). Ambience per zone, weather (rain, thunder), weapon hits (slash, pierce and
blunt each different), spells, mobs; music later. OPEN.

**8G. Performance and tech**
- Note 23's target (144+ fps), measured at Chris's 3440x1440.
- KNOWN_ISSUES.md, still true: the world clock and the village's stock and errands are not saved. Go through the rest of that file.

**8H. Lore and copy**
- CRULANDA_CANON.md: fill it from the books (only its rules are written).
- `marketing/steam-blurb.md`: out of date (four zones, levels 1-10, Warrior or Druid).
- Pib is a goblin but spawns as a human villager (no goblin figure yet; see 9.3).

### Phase 8.5: gear and figures overhaul (queued; ROADMAP.md)
- The choice to make: (a) build everyone on CHOSAN's Modular Hero rig so its pieces fit by construction (the lasting fix: the full
  package re-downloaded, our clips retargeted, the items' looks mapped to its parts), or (b) rewrite our fitting rules piece by piece.
- It covers notes 31 (the helm orbiting the head), 88 (helms: hidden by default since round 31, Esc > Display > Show helms) and 101
  (a level-1 sim's mismatched armour).
- Done when the wardrobe line-up shows every slot's every family fitting on every class's figure, and Chris has looked.

### Phase 9: the wider world (planned, PROVISIONAL; Chris picks the first zone, class and race when Phase 8 closes)
- 9.1 Zones, the 16-30 road: the Lowtowns and the Rust Market (16-19), the Forge District (19-22), the Warrens (22-25), the Crown and
  the Transit Hub (25-28), the Spire (28-30); the bible's others (Port Caelum, the Iron Citadel, the Archive of Silence, Hallow's
  Creek, the Tide-Watched Shores). The level cap rises to 30 with them.
- Three more dungeons, each designed before it is built (DUNGEON_DESIGN 0.4): the Sludge-Tunnels (18-20), the Deep Veins (23-25),
  the Transit Hub (27-29; its last boss drops the letter that opens the Spire).
- 9.2 Classes: the Tinker (Klyther Forgeheart, as the novels have him), the Knight (overlaps the Paladin's Oathguard: resolve), the
  Salt-Mender; then which of CLASS_BUILD_MATRIX's 20 candidates make the brief's 15. Talent rows 4-6 for all seven built classes (the
  Warrior's and the Druid's designed, five to design), needed only when the cap rises.
- 9.3 Races: the goblins, the Forgeborn, the Veridian Keepers (playable only if the books allow), the human peoples by region.
- 9.4 The sims in the wider world: more bands and homes; guilds and trades reach the new places.
- Endgame at the cap (a cap-15 version on the Verdant Shore first): raids (the Spire of Agony first, ten players), world bosses,
  timed challenges, set gear, titles.

### Later (Chris's ideas)
- Seasons (note 85): summer drains stamina and water; winter brings snow and a cold that stops healing until you are warm.

## How to work here (short)
- Never run Unity against Chris's open project: sync into a validation lane (the tools do it).
- **Run one Unity job at a time.** Lane A tests while lane B built broke the test lane's registration tonight (ILPPTrigger pipe
  error: every test fell back to Oakhaven).
- Tests: `tools\validation\focus_detached.ps1 -Runs 'PlayMode|Crulanda.Tests.<Fixture>|tag' -Log D:\crulanda-work\tag.log -Lane A`
  (namespace `Crulanda.Tests`, not `Crulanda.Tests.PlayMode`).
- Build: `release_build.ps1 -Sync` (publishes unless `-NoPublish` or the game is running).
- Frames: `build_and_tour.ps1 -Zones zone.adit` (set `CRULANDA_VCOPY` for lane B) shoots every cave's halls both ways and a Hollow
  Man close up; `capture_motion.ps1 [-BowOnly]` for the run and the bow. Chris often watches a capture as it runs.
- After meaningful work: commit, push the branch, `tools\Backup.ps1`, and copy this page to `D:\crulanda-work\outputs\Crulanda-Claude-Handoff.md`.
