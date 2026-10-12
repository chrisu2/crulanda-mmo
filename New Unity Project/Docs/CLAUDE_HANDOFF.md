# Crulanda handoff (current state)

Updated 2026-10-11, 22:50 (America/New_York). One page on purpose: what is true now, where things are, and what is next. History
lives in CHANGELOG.md and HANDOFF_ARCHIVE.md (earlier versions of this page are in git history). Replace this page when the
state changes; do not stack.

## Read first
1. `CLAUDE.md` (repo root): the rules and where everything is.
2. `GAME_BRIEF.md`: what the game is and the bar it is held to.
3. This page, then `PLAYTEST_NOTES.md` (Chris's reports and their status), `DUNGEON_DESIGN.md` section 0, `CHANGELOG.md`.

## Rules (Chris, 2026-10-11)
- Nothing counts as done until Chris has seen it: **VERIFIED** (Chris confirmed it), **TESTED** (a test passes on it), **BUILT** (in the
  code, untested), **OPEN**. Never write VERIFIED for him.
- Never publish (no `release_build.ps1` without `-NoPublish`) and never merge PR #2 until Chris has looked at the frames and played.
- One Unity job at a time. Never run Unity against `D:\code\mmo`; the tools sync into the validation lanes.

## Where things are
- Repo `D:\code\mmo`, branch `feet-bow-adit-kit` (PR #2 into `main`, open, not to be merged yet). Unity project `New Unity Project`.
- Lanes: `D:\crulanda-work\encounter-validation` (A, tests) and `encounter-validation-b` (B, builds).
- Chris's playable build: still the 00:33 one in `D:\crulanda-work\outputs\Crulanda-Playable` (nothing published since).
- **Candidate build (not published):** `D:\crulanda-work\candidate-20261011-2230\Crulanda.exe`, a release build of this branch
  (0 shader errors), with its frames in `captures\life` and `captures\fixes` (`fixes.txt` says what each frame should show).
- Backup: `tools\Backup.ps1` to `E:\claude\unity projects\mmo`. Lore: `D:\code\crulanda`.

## Step 1, tests: done except FootTests
- Full run 20:09-21:38 (lane A): EditMode 449/453, PlayMode 303/305. Its EditMode half ran before 7a65f4c, which fixed three of the four.
- EditMode rerun on 7a65f4c (22:0x): 452/453; the last (GearLookTests counting 146 variants, not the dirks' 150) fixed in c9e4e67.
- PlayMode `Mobs_stand_apart_stroll...` (127 of 148 stood still): not a game bug. Far camps stand on purpose since f6deab9 (the CPU
  fix); the test now watches the patrols and the camps within `EncounterEnemy.StrollRange` (70 m) of the player. AditTests 7/7 after.
- PlayMode `Feet_meet_the_ground...` still fails: **note 112(b), waiting on Chris's choice**: (a) get Morro Motion's own character model
  and copy its avatar to every Morro clip, or (b) stand figures on Kevin's idles (straight feet, less variety).
- Not yet rerun as a whole: the full set on c9e4e67 and later (the two fixes above were checked fixture by fixture).

## Step 2, the seven fixes: frames taken, Chris to judge (none VERIFIED)
From the candidate build, 1440x900 windowed, temp saves (Chris's save untouched):
- 123 the trap: `fix-01`, `fix-02`. The trap lies at the Ranger's feet (0.05 m). Looks right.
- 124 Esc and the auto shot: `fix-03` to `fix-05`. One Esc stops it and keeps the target, the second drops the target, the third opens
  the menu. The frames are hidden by trees; the log lines prove it, the pictures less so.
- 122 the Druid: `fix-06`, `fix-07`. A party Druid (Wren Sallow) mends you: 218 to 308 of 363 health in three seconds.
- 116 the shop: `fix-08`, `fix-09`. Goodbye shuts Maud's wares and the bags it opened.
- 120 the inn: `fix-10` to `fix-12`. Inside the Golden Cask the walls stand, also turned toward a wall.
- 121 full screen: no frame (a windowed capture cannot show it); EncounterRulesTests measures it. Chris must check it on his monitor.
- 119 the fork: **not shown.** The life capture's farmer frames (07-11) are taken from behind the player with something large in front
  of the lens; the camera override in LifeCapture does not hold. Fix LifeCapture's camera, retake, then show Chris.
- **New, seen in the frames, OPEN:** outdoors, whole houses away from the camera's line are drawn dithered or see-through (life 08,
  09, 11; fixes 01, 06, 08). Probably the note 120 fade reaching too far. Check `CameraAndTargetTests` against these views.

## Next, in order
1. Chris looks at the frames and answers 112(b). Fix the life capture's camera and the outdoor fade; retake.
2. The full PlayMode set on the latest commit (quiet machine), then the repeat set before any publish:
   `focus_detached.ps1 -Runs 'PlayMode|Crulanda.Tests.VillageDayTests.A_long_tool_is_held_in_the_fist_like_a_staff_clear_of_the_arm;Crulanda.Tests.CameraAndTargetTests;Crulanda.Tests.SimPartyTests;Crulanda.Tests.RangerLoopTests;Crulanda.Tests.TradeBagTests|repeat#EditMode|Crulanda.Tests.EncounterRulesTests|repeatE'`.
3. Only with Chris's yes: `release_build.ps1 -Sync` to publish, then he plays it.
4. Then the Adit pass (DUNGEON_DESIGN 0.06: a 60-75 minute run, no machines; notes 125-132), the tester's 114, 115, 117, 118,
   sound (none yet), and the rest of ROADMAP Phase 8.

## How to work here
- Tests: `tools\validation\focus_detached.ps1 -Runs 'PlayMode|Crulanda.Tests.<Fixture>|tag' -Log D:\crulanda-work\tag.log`
  (an empty filter runs the whole platform).
- Build without publishing: `release_build.ps1 -Sync -NoPublish` (lane B, about eight minutes).
- Frames: `Crulanda.exe --crulanda-fixes-capture <folder> --crulanda-class class.ranger --crulanda-zone zone.oakhaven
  --crulanda-temp-save` and `--crulanda-life-capture <folder> --crulanda-temp-save`, windowed; save and restore Chris's screen
  settings round them with `screen_prefs.ps1 -Save` / `-Restore`.
- After meaningful work: commit, push the branch, `tools\Backup.ps1`, copy this page to
  `D:\crulanda-work\outputs\Crulanda-Claude-Handoff.md` and check the copy's hash.
## Note 119 (2026-10-12, 00:30): TESTED, BUILT, OPEN until Chris checks
- Grip: a long tool's shaft now passes through the closed right hand (ActorVisual.StaffGrip: amid the middle finger's knuckle and joints; StaffArm curls all four fingers 72/88/55 degrees about the knuckle row, knuckle row upright, palm in).
- Stance: upper arm hangs near straight down, forearm down-and-forward (~35 degrees under level), shaft vertical, butt by the foot.
- Test: VillageDayTests.A_long_tool_is_held_in_the_fist_like_a_staff_clear_of_the_arm now also needs the shaft within 3 cm of the knuckles, fingertip within 4 cm, upper arm within 30 degrees of down, elbow 20-80 degrees (ActorVisual.StaffHoldReport). Passes in lane A.
- Candidate (NoPublish): D:\crulanda-work\candidate-20261012-0012. Capture: --crulanda-life-capture <dir> --crulanda-farmers-only (one farmer, 2 m, hips-level camera, HUD hidden; side/front/three-quarter, walking and standing).
- 112b (Morro clips on the pack's own avatar) not done here: the pack's source avatar is not in the project or downloads.
