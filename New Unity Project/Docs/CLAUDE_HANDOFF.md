# Crulanda — handoff (current state)

Updated 2026-10-11, 01:21 (America/New_York). One page on purpose: what is true now, where things are, and everything left to
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

## Chris, 2026-10-11, 01:20
"record it all and . create a handoff ill probably not let you touch my project again after today". Before that: "after all this work i
feel less optimistic after all the crap i experienced today", "ive also had to repeat the same issues over and over and over with no
resolutions", and on the dungeon: "all you did in the dungeon was make it bigger which in turn has less mobs and would not take 60-75mins
to complete". Whoever works on this next: verify everything yourself, show Chris pictures before anything is published, and never call a
report fixed until he has seen it in his own build.

## 2026-10-11, 16:50 on: Chris watching group runs of the Adit (PLAYTEST_NOTES 133-141; CHANGELOG 16:50)
- **The group run tool:** `toolsalidation\group_run.ps1` runs lane B's build with `--crulanda-run-capture` (RunCapture.cs): a party of
  five led by a sim through the Adit at 3x, on the screen, everything logged to `D:\crulanda-workun-captures\<name>un-log.txt` and
  summed in run-summary.txt with the meter. The runs of the evening are kept beside it (adit-group-1707-scaling-too-hard,
  -1709-no-gear, -1722-geared, -1727-boss-wall, and the one running).
- **Balance changes made from what the runs showed (all TESTED only by the runs; the full set to run):** group scaling .6/.2 a share
  (was 1/.45); sims wear their class's armour (they had none); sims hit like a player (they hit 40%); no single blow over 60% of a party
  member's health (the Gang-Boss's Tally-Stick one-shot everyone). The Druid healed all along (on the record, 155 mends in nine minutes).
- New: the party of five (MaxPartySims 4), the sim Ranger's wolf, the meter (/meter), marks and holds as icons, captures' panels at their
  defaults. The Rock-Eater stays the bear Chris preferred.
- Since then (17:40-18:30, Chris still watching): the scorecard, the party leaving held mobs alone, the leader waiting for the whole party
  (16 m) and giving up unreachable camps, the duo (Nix held, the beast goaded or calm), bosses that stun/fear/root/disorient, the Rogue's
  dirks and Throw Knife, far mobs standing still (CPU), the run's bags. All TESTED by today's suites (EditMode 10/10, 8/8; PlayMode
  51/53 then 14/14 with the two fixed) and the runs; **the full PlayMode set has not finished on today's code** (the 16:26 run hit the
  runner's time-out with the machine shared). The Gang-Boss falls (14:33 in the 17:56 run); the Rock-Eater and Nix fell at 22-23
  minutes in the 17:37 run (before the duo rework). **Run the full set on a quiet machine before publishing.** Nothing published.

## 2026-10-11, 15:00-17:00: the Rock-Eater fixed in the game, the fourth paths, the Salt-Mender, Direct3D 11 (Chris away; "keep developing")
Status words as Chris asked: VERIFIED (he saw it), TESTED (a test measures it), BUILT (made, no test yet), OPEN.
- **The Rock-Eater's saddle, crystals and girth: TESTED and seen in the built game's captures** (16:26, adit-94 front, side, back: Nix in
  the saddle on its back, the crystal ridge behind him, shards at the shoulders, a girth down the flank). Two game-only causes found and
  fixed (CHANGELOG). Not VERIFIED until Chris sees it.
- **Direct3D 11 is now the game's first graphics API.** From 15:45 the Adit crashed every D3D12 run on this PC, the published 00:33 build
  too (NVIDIA driver reset, nvlddmkm events 14/153; a fresh copy ran once, then crashed: the driver's cached build of something in the
  Adit). D3D11 ran it cleanly twice. **If Chris's game crashes entering the Adit, this is why; the next published build avoids it.** A
  reboot may also clear the driver's state. OPEN: whether D3D12 recovers after a reboot.
- **The three missing fourth paths: TESTED** (EditMode and PlayMode suites green), not played: the Ranger's Skirmisher, the Rogue's
  Swashbuckler, the Mage's Cinderwright (an ember hound; the forge-golem dropped, no machines). TALENT_DEPTH.md.
- **The Salt-Mender, an eighth class from the canon: BUILT**, its tests written (SaltMenderRulesTests, SaltMenderLoopTests) and to run after
  the full run. CLASS_BUILD_MATRIX.md "Phase 9.2 and 9.3 from the canon" has the reading of the books behind it, the Tinker and the
  Engineer set aside as machinery, the Knight (no "Stormlight": undefined in the books and another author's term), and the races
  (goblins first; no race layer exists yet, every figure is human: OPEN).
- The Adit's floors darker and their dressing denser: TESTED (AditTests 25/25 with it), in the 16:12-16:26 captures.
- Full run on the afternoon's code started 16:26: EditMode 449/449; PlayMode running.
- Commits 2f2330c, b51ebb4 pushed to feet-bow-adit-kit; backup 16:27.

## 2026-10-11 afternoon: the Adit pass (Chris, 12:23: "get busy and do better than yesterday"; "keep working until i say stop . dont stop
to ask questions"; he was away from about 12:33)
- The full run on the night's code (80045ff, 12:24-13:46): EditMode 447/447, PlayMode 279/280; the one failure is note 112(b), Morro
  Motion's standing idles turning a foot out (48 degrees here), known and open (below).
- Built today (CHANGELOG, DUNGEON_DESIGN 0.05-0.06; status by line in "Where the work stands"): mobs stroll their camps and patrols walk
  their roads in every zone, spawned a body's width apart; the Adit filled (67 camps, 148 mobs, eight patrols) and its wide floors
  dressed; no machines (the Rock-Eater a great beast with Nix in its saddle, the carriage mule-hauled ore wagons, the winch gate); the built
  halls' walls turned to face in and the floors darkened; the cages whole with a pressed goblin in each.
- **Note 112(b), Morro's feet:** the Morro idles have no source character in the project (only the clips were imported), so each builds its
  avatar from its own file, as the feet did before 2026-10-10. Two ways, Chris to choose: get the pack's own character model and copy its
  avatar to every Morro clip (CharacterImport.SourceModel), or stand figures on Kevin's idles (straight feet, less variety). FootTests
  measures it.
- Daniel Gruginski's Medieval Village Kit is downloaded (`D:\crulanda-work\downloads\MedievalVillageKit_FBX.zip`); no import is recorded.

## After midnight: Chris's play of the 00:33 build (notes 119-132)
- **Fixed, TESTED and committed, NOT published, not seen by Chris** (see "Repeated reports" below for the causes and tests): 119 the fork,
  120 the building fading and 117 the camera in walls, 121 full screen and 115 alt-tab keys, 122 druids healing, 123 the trap, 124 auto
  shot (Esc), 116 the shop's bags. To publish: run the repeat set, `release_build.ps1 -Sync -NoPublish`, run the build's life capture
  (`Crulanda.exe --crulanda-life-capture <folder> --crulanda-temp-save`, windowed) and look at the farmer shots 07-11 front, side and back,
  then publish (`release_build.ps1` copies to outputs unless the game is running) and tell Chris.
- **Recorded, OPEN, the Adit pass** (8A below): 125, 126, 127, 128, 129, 130, 131, 132.

## Where the work stands (2026-10-11, 00:33)
- **Published 00:33** (release build, 0 shader errors, from commit 9fc71fc; the new code and the new layout were checked in the
  published files): the drawn-out Sealed Adit. **Chris walked it after midnight and reported notes 119-132** (above).
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

## Repeated reports (Chris, 2026-10-11: "ive also had to repeat the same issues over and over and over with no resolutions")
Each was reported before and came back. The cause found on 2026-10-11 and the test that now measures what Chris saw. **Run the
"repeat set" before every publish**:
`focus_detached.ps1 -Runs 'PlayMode|Crulanda.Tests.VillageDayTests.A_long_tool_is_held_in_the_fist_like_a_staff_clear_of_the_arm;Crulanda.Tests.CameraAndTargetTests;Crulanda.Tests.SimPartyTests;Crulanda.Tests.RangerLoopTests;Crulanda.Tests.TradeBagTests|repeat#EditMode|Crulanda.Tests.EncounterRulesTests|repeatE'`.

| Note (his words) | Before | Cause found | Measured by |
|---|---|---|---|
| 119 "farmers hoes still going thru the arm and not held like a staff" | 95, 109 | the farmer's fork was never on the tool holder note 109 added; the old test measured only the woodcutter's axe | VillageDayTests: fork in the fist, upright, clear of arm, body and legs, walking, standing, talking, at work |
| 120 "GOING IN A BUILDING MAKES IT DISAPPEAR" | 26, 65; tester 117 | the fade boxed walls square to the world: the angled inn's walls boxed the room, so the whole inn faded; the camera's 1.2 m minimum put it in walls | CameraAndTargetTests: inside, the walls stay; from outside the wall hit fades and the far ones stay; the camera stays within the walls |
| 122 "the druids are still not healing party members!" | Mira's, 2026-10-09 | sims healed only under 60%, in a fight, every 6 s, none while the most hurt was out of reach, never out of a fight (a timer clash) | SimPartyTests: a Druid mends you and a sim, and the near one with a worse one 40 m off |
| 121 blurry full screen, desktop text after alt-tab | 94; tester 115 | "Fullscreen" was the exclusive mode at the window's size (1920x1080 first); the chat's typing flag could stay on | EncounterRulesTests: full screen is borderless at the monitor's size; the typing flag lapses |
| 116 the shop not closing on Goodbye (tester) | | the bags the shop opened stayed open | TradeBagTests |
| 123 "trap set up in the air" | | set at the middle of the player's 2 m capsule | RangerLoopTests: the trap on the ground at your feet |
| 124 "no way to turn off auto shot" | | Esc went past it to the menu | RangerLoopTests: Esc stops it, then the target, then the menu |

## To do, every phase
Phases 0-7 are built (ROADMAP.md: foundation, combat, seven classes, items, quests, sims, groups, the first two dungeons); what of
them is not verified is under "Verify first". Phase 8 is now; 8.5 and 9 follow.

### Verify first
1. Chris walked the drawn-out Adit (00:33 build) and reported 119-132. PR #2 (`feet-bow-adit-kit`) is not to be merged until the
   fixes above are published and verified by him and the Adit pass is done.
2. The full PlayMode run on the branch, then the zone tours (`build_and_tour.ps1`); fix what fails before the merge. (Chris, 00:46:
   not tonight; first thing next session.)
3. In Chris's build: the feet, the jump and the bow (verified only in captures); note 108 (the bow in play); 109 (tools carried).
4. Reports worked on that Chris never confirmed: 23 (frame rate: 145 fps measured in Oakhaven at 1440x900; the target is 144+ and he
   plays at 3440x1440), 24 (dead-tree joins), 25 (swings: an upper-body layer only; real swings need the Melee pack), 26 (roofs fade),
   27 (crowns, not "gold"), 28 (the horse kept out of the inn), 29 and 30 (drinking, sitting), 39 (crags, "to look at again"), 43 (red
   mobs hit harder), 57-61 (round 23, marked UNTESTED: straw hats, cows' colliders, the character sheet, sign text, farmers working),
   99's done parts (the Adit's first look), 106 (the horizon, a first pass). **In all, 79 notes in PLAYTEST_NOTES.md are marked DONE or
   FIXED by the agent and only 2 are confirmed by Chris (107 and 111, in a capture):** the real list to verify is those 79, the ones
   above first. Go through them in play with Chris.
5. Docs that disagreed with the code: fixed 2026-10-11, 00:47 (Chris said yes): CLASS_STATUS (Mira's Hush), DUNGEON_DESIGN (quest
   gear), ROADMAP (the archery clips; the status column points here), GAME_BRIEF (its date, the Shore's band, the style pass), KNOWN_ISSUES
   (its Phase 0 sections marked as such).

### Phase 8: the vertical slice (now)
**8A. The Sealed Adit (DUNGEON_DESIGN.md)**
0. **First, what Chris saw in the 00:33 build (notes 125-132), all OPEN:** a run must take **60-75 minutes** (0.06) and the drawn-out
   caves are empty: more camps along every drift and chamber, mobs that **wander their ground and walk patrols**, spawned a body's width
   apart (131); the halls dressed to their new size (rocks, dripstone, crystals, timber, rubble: 126, 127, 130), or the chambers made
   narrower than 2.5 times; the built halls' **flat black walls** made from the modular stone kit (128, note 110); **no machines** (129;
   0.05): the Rock-Eater a great burrowing beast Nix rides, the carriage an ore wagon hauled by a beast or by hand, the cage-lift a
   rope-and-winch cage worked by people; the **cages** whole, with doors and someone in them (125: needs a goblin figure). Frames to
   Chris before each publish.
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

**8C. Sound: there is none** (no audio code in the game; Chris, 2026-10-11: right after the tester's bugs). Ambience per zone, weather (rain, thunder), weapon hits (slash, pierce and
blunt each different), spells, mobs; music later. OPEN.

**8D. Animation**
- Note 112, OPEN: (a) at full draw the bowstring is not drawn to the hand and no arrow shows; (b) Morro Motion's idles turn a foot out
  about 50 degrees (import them from a source avatar, as Kevin's are); (c) the walk's toes dip 3-6 cm in places.
- Kevin Iglesias's Melee pack: not imported (copies in `D:\crulanda-work\downloads\kevin-iglesias`). Real swings for every class (the
  rest of note 25): into a validation lane first, the clips copying his avatars, never into Chris's open project.
- Kevin's Archer pack: only BowShot01 (load, hold, release) and BowIdle01 are in.

**8E. Classes and crowd control (CLASS_STATUS.md, CC_DESIGN.md, TALENT_DEPTH.md)**
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

**8F. World and art**
- The painted style pass (GAME_BRIEF's art direction), zone by zone. OPEN.
- Note 40: the Ash-Walker enclave's interior (a firelit hall behind the middle arch; only its cave mouths exist). OPEN.
- Khaven with the Village kit (imported 2026-10-07 into Resources/Props/Village; check what Khaven uses before calling it done).
- Daniel Gruginski's Medieval Village Kit (CC0; ROADMAP): no record of its import.
- Note 14: the PDF of the work (last brought up to date 2026-10-02, `tools/docs/make_history_pdf.py`). OPEN.

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
