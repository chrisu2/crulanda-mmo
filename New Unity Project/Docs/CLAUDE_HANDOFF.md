# Crulanda: Claude handoff (rewritten 2026-10-12 23:59 ET; covers Grok Bot's work on Oct 11-12)

## Start here
- **Branch:** `feet-bow-adit-kit`, PR #2. Do not merge it, and do not publish, without Chris.
- **Published build:** commit 8dbb69d, published at 1:42 PM on Oct 12. Everything after that is committed and pushed but not published.
- **Save backups:**
  - `D:\crulanda-work\save-backups\20261012-1325-before-publish`
  - `D:\crulanda-work\save-backups\20261012-1040-before-path-rework`
  - `D:\crulanda-work\save-backups\grok-2155`
- **Not compiled yet:** the Cleric, Shaman, race and race-UI code (from ae04cd6 onward). It is all committed but has not compiled once. The first compile comes from `editR` (below).
- **Running now** in lane A, `D:\crulanda-work\encounter-validation`:
  - `suiteQ`: the full PlayMode suite, started at 23:13. Results go to `suiteQ.xml` and `suiteQ.log` in lane A. It tests the code from before ae04cd6; the lane was copied when it started.
  - Then `chainR` (`D:\crulanda-work\chainR.ps1`, PID 26280) runs three jobs one at a time. Each copies the current repo into the lane first.
    - `editR`: the EditMode suite, which is the first compile of the new code. Output: `editR.xml`, `editR.log` and `editR.out`.
    - `socialR`: SocialPullTests. Output: `socialR.xml`.
    - `balC`: the balance harness with `CRULANDA_BALANCE_CLASSES=cleric,shaman`. Output: `balC.xml` and `D:\crulanda-work\outputs\balance\balance-cleric-shaman.csv` and `party_heal.csv`.
- **First 3 steps:**
  1. Check whether `chainR` (PID 26280) is still alive. Read `suiteQ.xml`, then `editR.xml`. If there's no xml, look for `error CS` in the log.
  2. Fix the compile and test failures. The new code is in `ClericKit.cs`, `ShamanKit.cs`, `Races.cs`, `RaceTests.cs` and `BalanceHarness.cs`, plus the Cleric and Shaman blocks in `Encounter.asset`. Then rerun the failing test groups alone.
  3. Read `balC` and report Cleric and Shaman DPS and HPS against the role medians and the caps. Check that the party heal rows now say "3 sims joined".

## Rules (from CLAUDE.md and Chris)
- Run Unity only in the validation lanes: lane A (`D:\crulanda-work\encounter-validation`, tests) and lane B (`encounter-validation-b`, builds and captures).
- Never run Unity on `D:\code\mmo`.
- Run one Unity job at a time. Tests go through `C:\Users\ChrisWin11\crulanda-cap\rt.ps1 <PlayMode|EditMode> <filter> <name>`, which copies the repo Assets into lane A first.
- After every chunk: commit, push, then `pwsh tools\Backup.ps1`, which copies to `E:\claude\unity projects\mmo`.
- Keep `.meta` GUIDs. Write new metas yourself and never let assets re-import with new GUIDs.
- Back up saves to `D:\crulanda-work\save-backups\<stamp>` before changing any save format.
- Label all lore CANON, CANON-EXPANDED or GAME-ONLY. The canon source is `D:\code\crulanda`, which is read-only.
- No machines, in lore or in kits.
- Keep at most 3 `candidate-*` folders in `D:\crulanda-work`. Delete the oldest first, and never the published build.
- Movement keys are ESDF.
- Nothing is VERIFIED until Chris has seen it. Write TESTED or BUILT otherwise.
- Hard stops: 35-minute chunks, each ending with commit, push, backup and a report.

## Done on Oct 11-12, with commits
- **Farmers and the hoe:**
  - f4400b6: the farmer's grip and stance (note 119).
  - 6feb10e and 5b5d931: the hoe stroke and the plough team (notes 150 and 155).
  - 394ad33: the hoe hand spread (47-58 cm).
  - The spread regressed later. dcd45d8 tried a margin change, which dfe50ff reverted.
- **Class path reworks:**
  - a0d3ca0: the Salt-Mender's fourth path, the Mender (healer, CANON-EXPANDED), with its sims and its own figure.
  - cd9ca8d: the Mage's Cinderwright reworked into the Bonecaller (necromancer, GAME-ONLY), with a save migration.
  - 4f5d5ca and f600363: the Archivist's Unwriting (damage over time) and Still Vow (shields).
- **Auras:** efdef7f, 4756505 and 89488b0. The rings follow the drawn body, and the pet frame says "Thrall".
- **Houses and trees fade:**
  - 5819e26: houses are never see-through outdoors (note 159).
  - 8f67fe5 and 8dbb69d: only the part that's in the way fades, and trees never vanish.
- **Mira invites, the leash, the meter:** 8f67fe5 and 8dbb69d cover the invite target and Mira, and the leash. A group breaks off only past the far leash and after 5 s with no blows. 8dbb69d is the published build.
- **Movable meter and pet bar:** d0b9aef (MovablePanelTests). Frames in c3ba8b3.
- **Thrall:**
  - ec8a817: built by hand in Blender (7.8k tris, same 18-bone rig and GUID).
  - 7bd9f7e: narrower skull, angular jaw and a grave-cloth hood (note 175), plus an in-game video.
  - Hunyuan3D wasn't used; its venv is broken (see PC notes).
- **Balance harness** (`Tests/PlayMode/BalanceHarness.cs`, `Docs/BALANCE_REPORT.md`):
  - da65872 (v2), 173c5d7 (v3) and 53e9161 (v4) fixed HPS, the seeded openings, the cooldown-first rotation, `foes_hit`, gear and the class filter.
  - 7089d8b: round 1 retune with build damage scales (`EncounterSession.RoleScale.cs`) and role DPS caps (note 177). Healers must stay at or under 50% of the DPS median, control and support at 70-75%, enforced by `CapViolations`.
  - c17f429: pet damage scale (Beastbond .75, Bonecaller 1.6).
  - ae61e0b: v6.
  - c6715af: v7, the Druid retune (Thornsong x2.1, Rootmend heals x2.2).
  - dfe50ff: v8 confirm run. The casters are back to their numbers and the Druid retune holds.
- **Party heal test (note 178):**
  - ae61e0b and dcd45d8 added it, and 065561c made it wait for the sims and log why none joined.
  - bb082f3: the test now makes its own SimPopulation and sim figures. Until then 0 sims joined, because PlayableEncounter has no zone. This hasn't been tested yet; `balC` is the first run.
  - Healers in it: Paladin sanctuary, Salt-Mender mender, Archivist cantor, Druid rootmend, Cleric lantern and choir, Shaman rainspeaker.
- **Zone-loading fix:** 512e056. The Oakhaven scene listed only 2 of its 6 zone files. This caused about 35 of the 42 PlayMode failures, and the reruns are now green (46/46 and 12/13).
- **SocialPull test fix:** bb082f3. The test waits out QuietSeconds; it had been out of date since 8f67fe5. Not rerun yet; `socialR` is the rerun.
- **Feet:** 0f81dbe. Standing, the body sits to the higher sole, which took FootTests from 12 problems to 1. GroundingTests passes. The rest is parked (see the TODO).
- **Cleric and Shaman** (design in `Docs/CLASS_CLERIC_SHAMAN.md`):
  - f264e07, ae04cd6 and 2c896da: ClericKit, with 12 abilities in `Encounter.asset` and `Talents/cleric.json` (Lantern, Choir, Censure and Warden, 17 ranks each). It's a healer that uses mana, labelled GAME-ONLY.
  - 8b0d6bc and 323fa03: ShamanKit, with 12 abilities and `Talents/shaman.json` (Rainspeaker, Stormcaller, Stonebound and Gravewind, 17 ranks each). It's orc-only and CANON-EXPANDED, signed off by Chris.
  - Both kits are switched on in `ClassKit.Create`. 96cb8be and 323fa03 add them to the harness.
  - f526f6e: both count as healers in the sim hooks.
- **Races** (`Docs/RACES.md`, `Scripts/Encounter/Races.cs`):
  - 323fa03: Human and Orc.
  - 391032e: orcs play Warrior, Ranger, Rogue, Mage and Shaman (no Druid). The Shaman is orc-only and the Cleric is human-only.
  - Saves get a `raceId`; an old save reads as human.
  - `EncounterSession.FreshProgress` takes a race and corrects a class/race clash.
  - EditMode `RaceTests` has 5 tests.
- **Orc sims and race in the UI:** ecf1238. About 1 in 4 sims whose class orcs can play is an orc (by roster position, so the rolls are unchanged). Race shows on a sim's card and on the character sheet.

## PC notes
- **Ethernet power saving:** with Chris's approval, Energy-Efficient Ethernet, Green Ethernet and Power Saving Mode on the Realtek 5GbE adapter were disabled (ffde5f1). The original values are in `D:\code\mmo\tools\nic_power_original.txt` for reverting.
- **Disconnects:** the ~8:12 PM drops were on the agent connection, not the PC. The event logs showed no sleep, shutdown or NIC errors.
- **Keep-awakes:**
  - PID 19320 is the sleep keep-awake (`keepawake.ps1`, SetThreadExecutionState).
  - PID 26952 is the disk keep-awake (`D:\code\mmo\tools\keepdisk.ps1`), which writes a tiny file to D: and E: every 15 min.
  - Leave both running.
- **Hunyuan3D-2 mini:** its venv under `D:\crulanda-work\tools` is broken and was never used.

## Staged, not imported
- Orc packs are in `D:\crulanda-work\imports\orcs`: "3D Stylized Orc - animations+PBR (13.6k faces)" and "Orc Army Pack" (includes an Orc Shaman).
- Their rigs and scale haven't been checked against the humanoid/ModelFigure rig yet.

## TODO, in order
1. Read `suiteQ`, `editR`, `socialR` and `balC`. Fix compile and test failures.
2. Cleric and Shaman DPS and HPS: retune against the medians and caps, and check the party heal test with 3 sims.
3. Orcs:
   - Check the rig and scale of the orc packs.
   - Build the orc figure. Gear fit, feet and the hoe then need re-checking at the new size.
   - Add a race pick at character creation, restricted to the classes each race allows.
4. Foot flake: "running after a jump, left foot down but turned 178 degrees" (parked by Chris 23:13).
5. Hoe hand spread: hands are 33-43 cm apart (45-60 wanted), and the left hand is 6-7 cm off the shaft.
6. Spell effects: cast poses, trails, impacts and lingering effects for the Mage/Bonecaller, Archivist and Salt-Mender, with frames.
7. Bonecaller tiers 4-6.
8. An enemy that applies the grey (Adit/Wasting).
9. HUD cutoff (note 114), with a test that measures it.
10. The sim-follow flake.
11. Elwynn-style zone fill (note 172, `Docs/ZONE_FILL_PLAN.md`).
12. 112b, the Morro clips on the pack's avatar: waiting on Chris (the pack needs downloading again).
13. Republish, only with Chris's OK. Back up saves first.

## Key docs (in `Docs\`)
- `BALANCE_REPORT.md`: harness rounds v1-v8, caps, retunes.
- `RACES.md`: canon check, race storage, orc plan.
- `CLASS_CLERIC_SHAMAN.md`: design and status.
- `PLAYTEST_NOTES.md`: Chris's words, numbered notes, TODOs.
- `ZONE_FILL_PLAN.md`: note 172.
- `HANDOFF_ARCHIVE.md`: older handoffs.
- `D:\code\mmo\CLAUDE.md`: the rules.