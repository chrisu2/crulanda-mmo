# Crulanda: Claude handoff (2026-10-12 01:05 ET)

Read first: `D:\code\mmo\CLAUDE.md` (rules), `Docs\PLAYTEST_NOTES.md` (Chris's words), `Docs\HANDOFF_ARCHIVE.md` (older handoffs). Branch `feet-bow-adit-kit`. Never publish or merge without Chris. One Unity job at a time (lane A tests, lane B builds).

## State now
| Item | Tag | Evidence | Chris saw? |
|---|---|---|---|
| 119 farmer grip/stance (shaft through closed fist, relaxed arm) | TESTED, BUILT | f4400b6; VillageDayTests staff test passes; frames /workspace/playtest/step2c | Yes, 00:28 "yes better now" |
| 150 hoeing stroke (two hands, tines into soil, puff, steps along row) | TESTED, BUILT (WIP commit) | FieldWorkTests hoe test passes; frames note150-hoeing-side-strike/raised | No |
| 150 plough team (horse, wooden plough, ploughman, furrow trail, 07-18) | TESTED, BUILT (WIP commit) | FieldWorkTests plough test passes (>20 m/min, stays in field, hands <12 cm of handles) | No |
| 151-154 class requests (Mage necro, Archivist DPS + support, Salt-Mender healer/sims/figure) | OPEN | PLAYTEST_NOTES 151-154 | n/a |
| 112b Morro clips on the pack's avatar | OPEN | pack source not on disk | n/a |

Latest candidate (NoPublish, release): `D:\crulanda-work\candidate-20261012-field` (built 01:00 with all of tonight's code). Earlier: `candidate-20261012-0012` (note 119).

## Committed
- f4400b6 (00:18): note 119 grip/stance, StaffHoldReport test, tight farmer capture (`--crulanda-farmers-only`).
- This commit (01:05): note 150 WIP: `ActorVisual.Field.cs` (hoe stroke, two-bone reach, finger curl, soil puff, HandsOn), `PlowTeam.cs` (tended from VillageLife), farmer FaceRow/FieldStep in WorldLife, left finger bones in ModelFigure, `FieldWorkTests.cs`, field capture (`--crulanda-field-work`) in LifeCapture. Compiles (release build OK); the 2 FieldWorkTests and the staff test pass in lane A. Full suites not run.

## In progress: exactly where it stopped
Note 150 frame review. Hoeing frames are good (both hands on the shaft, tines in the soil). The first plough frames had props and villagers in the way, so I added code to LifeCapture that hides whatever stands between the camera and the team, and passers-by. The candidate with that change was built, but its capture run was stopped (Chris's save request) before any frames were taken. Next: run the capture on `candidate-20261012-field` with `--crulanda-life-capture <dir> --crulanda-farmers-only --crulanda-field-work`, review `note150-plough-side/three-quarter` and `note150-stroke-seq-1..8`, then copy the jpgs to /workspace/playtest/step3/ (4 earlier frames are there; the plough ones are the old, obstructed ones). Known gaps: the plough shots are framed at 5.2 m (the team is 4.5 m long); the furrow trail can read as a pole at low angles.

## Next, in order
1. Finish note 150 (capture review above, then Chris checks it in play).
2. Salt-Mender: bard-like healer with shamanistic salt cairns (154; CANON-EXPANDED), sims (SimAdventurers.ClassIds + AI rotation), its own figure.
3. Mage necro build (151).
4. Archivist shadow-priest DPS + Discipline-like support (152).
   For each build: talent rows 0-3 with >=16 ranks against the cap of 15, abilities, icons, a sim AI rotation, rules and loop tests; update CLASS_STATUS and CLASS_BUILD_MATRIX; commit per class.
5. Remaining plan: the Adit at 60-75 min, walls, sound, the canon doc, the HUD and /invite fixes (114, 118).

## Open decisions (Chris)
- 112b: copying the pack's own avatar onto the Morro clips needs the Morro pack downloaded again (its source avatar isn't on disk).
- See-through houses outdoors (the screen-door fade shows in the frames): keep it, or limit it.