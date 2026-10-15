# Crulanda: Claude handoff (Start here rewritten 2026-10-13 19:10; covers the work to the evening of Oct 13)

## Start here

**Current state: the night of 2026-10-14, 23:00** (Chris away from 20:30: "keep working till 11pm then backup, publish, and create a
handoff. i wont be here to approve anything so you have full reign. shutdown at 11pm"). His words of the day verbatim in
PLAYTEST_NOTES 179-199; what was done, in order, in `Docs/CHANGELOG.md` (2026-10-14, 11:40 on, and "The evening's reports").
Nothing is VERIFIED until Chris has seen it in his build.
- **Branch:** `feet-bow-adit-kit`, PR #2, pushed. Do not merge to `main` without Chris. The Adit rework is on `adit-rework` (worktree
  `D:\code\mmo-adit`, pushed, not merged).
- **Published (the playable build):** 19:54, the build of 19:51; the same build again at 20:12 (nothing newer). Saves backed up to
  `D:/crulanda-work/save-backups/20261014-1954-before-shepherd-publish` and `-2012-before-night-publish`. It holds everything below.
- **New today, for Chris to look at first (all BUILT, OPEN until he sees them):**
  - **The wild woods** in every zone (the "Elwynn" brief): the zone's own trees as one canopy, old oaks (the broadleaf grown large,
    not the half giant) only in the dense old-forest patches, fallen trunks the kit's dead trees, fuller glades. Frames:
    `D:/crulanda-work/world-captures/oakhaven-21-the-mastwood.png`, `-37-lark-hill.png`, `-38-sallow-bottom.png`. Tour 111 a second.
  - **Kevin's melee, spell and work motions** and **every spell's effect**: `D:/crulanda-work/ui-captures/combat3/<class>/sheet-b.png`;
    the Scoundrel inside the yard: `ui-captures/combat5/scoundrel`.
  - **The buff row** by the frames and **the Nerve panel** (movable): `ui-captures/combat4/scoundrel/10-scoundrel_rallying_verse-b.png`.
  - **The inn's tables and benches standing** (the Fantasy pack's root turn kept in ModelProp; every table, bench, stool and mug of
    that pack had lain on its side): `world-captures/oakhaven-99-inn-drinkers-night.png`.
  - **The shepherds** Tam and Nell Sedge at Oakhaven's two flocks (the count at the gate, the shearing, the fleeces to the stall, the
    afternoon feed; the merchant offers fleeces), the Old Fold's flock inside its walls: `world-captures/oakhaven-23-the-old-fold-night.png`.
  - **Quit**: the pause menu's "Quit game", `/exit` and `/quit` (saved first, out of combat).
  - Also: the orc's legs run in the air, the giants' roots, the birds' hops, the Morro villagers on their own avatars (the farmer's
    sack and the skinner's bundle in their hands), the forge's sparks off, the barn's sill and turn, a poor drop's glint fading.
- **Tests (TESTED):** the full PlayMode suite 345/349 at 16:33 (chainR72; see CHANGELOG); after that only the classes each change
  touched (test-budget: Chris, "you just tested for 7 hours"): chainR76 14/14, R78 17/17, R79 EditMode 16/16 and PlayMode 15/16
  (the home test's count, fixed). Balance 49 of 49 builds in range (`D:/crulanda-work/outputs/balance/balance.html`, the page Chris sorts).
- **The Adit rework (`adit-rework`; ADIT_REWORK.md section 18):** the evening's runs took it from 18/29 Adit PlayMode classes to
  20/29 at 20:06 and 19/29 after (one test, the fixture trestle's rail raycast, went red when the deck blocks were
  named and not yet looked at); EditMode 44/44. The decks are whole and joined; the big find at 20:55 was that every branch cave's floor was cut off a few metres past
  its mouth by its parent's knoll and shell (faces metres wide crossing the passage with their middles in the rock): `ZoneBuilder.Cut`
  now drops a face with a corner inside another passage. That lifted the Sunless River (9 to 59 of 341 samples reached) and the Dry Ledge (0 to 54) but not the
  Pen, the Cutting Drift, the Foundry, the Muster Yard, the Chapel or the Breach: the next session starts by reading the R87 map
  (`adit-nav-diagnostics-R87.txt`), which names what the line from the nearest reached sample to the first unreached one meets. The diagnostics (`AditNavDiagnostics`, lane A) write
  `D:/crulanda-work/encounter-validation/adit-nav-diagnostics.txt`: the deck tables, a floor map of every hall (reached from the yard
  or not, what stands by the first unreached sample), each gate's crossing. Run the Adit set with a copy of `D:\crulanda-work\chainR86.ps1`.
- **Next, in order:** Chris's reactions to the above; the Adit (section 18's "still red"); the Stylized Newbie Weapons Pack (skinned
  meshes: a bake before they can be held); the fixture test's knoll ray; the older list below.

## Oct 12 night (history; these runs were stopped and replaced on Oct 13)

**Read the "2026-10-13, 00:03 on" section below first: it is the current state.** (The bullets about suiteQ, chainR, editR and balC
further down this section are from the night of Oct 12 and are history: those runs were stopped, and Oct 13's runs replaced them.)
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

## 2026-10-13, 00:03 on (Claude): pickup, then balance on fixed dummies
- EditMode 456/460 at first (the Cleric and Shaman compile); the four failures fixed (icons for their 24 abilities and 115 talents, the
  class count of ten), commit 05c78ff. After the refresh fix: **EditMode 461/461** (00:51).
- **Balance (Chris: "dps and healing per sec on test runs ... on all classes", fixed dummies, "62-72 is good range", "a number for each
  build ... tank/dps/support/healer/CC", "why is the druid healing so low").** All in `Docs/BALANCE_REPORT.md`, round 5. The table is
  `D:\crulanda-work\outputs\balance\BALANCE_ALL.md` (`python tools\validation\balance_merge.py`); `balance_retune.py [--apply]
  [--skip=a,b]` moves builds outside their target. Older runs in `outputs\balance\archive-*`.
  - Fixed: refreshed heals/damage over time keep their tick schedule (the Druid's Seedling never healed); the Druid and Paladin heal the
    whole party; the Shaman's charge; the heal scale in the Archivist, Paladin and Salt-Mender kits; the harness (no heals in damage
    fights, Brand/Unwrite/Marrow Drain kept up, songs started once, heals taking turns in the heal tests).
  - 35 of 40 builds in range after the first retune (01:15 run); the second retune and the harness's healing fixes are in the confirm run.
  - Targets settled by Chris for level 15 (02:07: "the #s you proposed are good til lvl 15 and will change as we progress to 30"):
    DPS 62-72, CC and Support 45-52, Tank 42-55, Healer at most 35 DPS and 40-50 HPS. Set them again for levels 16-30 (Phase 9).
- **After 02:00 (Chris's briefs, all in BALANCE_REPORT.md):** spirits (SpiritCompanion.cs: aether butterfly for the Stormcaller, glade-fire
  for the Rootmend); Oakhaven's training field (EncounterSession.Training.cs, ZoneBuilder.Training.cs, oakhaven.json; TrainingFieldTests);
  area by specialization (EncounterSession.Spread.cs; AoE targets PROPOSED: x1.7 or more against three for the five area builds, x1.25 or
  less for the rest); the tank test in the harness. None of these have been run yet: chainR12 (balance) and chainR13 (training tests)
  run them. Then: tune the spread shares and the spirits so pet and player land on 62-72 and the area builds on x1.7; read the tank
  ranking; keep one class (the Paladin) from leading all three roles; show Chris the field in a build.
- **Next class, designed not built:** the Scoundrel (`Docs/CLASS_SCOUNDREL.md`; Chris: an assassin-type rogue with charisma for songs,
  merchants and dealing; a single-target DPS path, a group-CC path, a support path; canon root Valen, "yes good example"). Chris settled
  the questions (Scoundrel; three paths for now; races once all races are defined; song, special weapons later). Build when he says go.
- **The grey (TODO 8) done:** Hollow Men's blows grey (EncounterEnemy.Grey.cs), EditMode test. GAME-ONLY mechanic, CANON creature.
- **Adit NavMesh tests 3/3 (01:12):** the column and plinth tops are off the navmesh (TODO 14's code is done); the group run to the end
  is still to do (`tools\validation\group_run.ps1` on Chris's screen, with the lane B build the chain makes).
- SocialPull 7/8 at 01:14 ("The first is home"): fixed in 31cad5d (a mob finishes the walk home before strolling; far-off mobs ask
  for the way home at most every 2 s). The full suite from 01:34 (`suiteS.xml`) includes it.
- **Stopped at 02:29 (Chris: "once ... full playmode suite's result is finish we need to wrap it up"):** only the full suite from 01:34
  ran to the end (`D:\crulanda-work\chainR9.ps1`, `encounter-validation\suiteS.xml`). **Result (01:34-03:00, code as of 01:34):
  PlayMode 315/318.** SocialPull passed (the walk-home fix). The three failures are the known animation items: the hoe hand spread (hands
  37-43 cm apart, 45-60 wanted; TODO 5), the foot flake (running after a jump, toes pulled up; TODO 4), and one new: a long tool's shaft
  through a villager's leg while walking (Grete Lowe, 0.07 m; `A_long_tool_is_held_in_the_fist_like_a_staff_clear_of_the_arm`). The
  work after 01:34 (spirits, training field, spread, tank test, harness fixes, the grey) is not in this run; chainR12/R13 test it. **Next session, first:** run
  `D:\crulanda-work\chainR12.ps1` (all ten classes on the dummies with every Oct 13 change: the spirits, area spread, tank test, toggles
  and healing fixes; the merge; EditMode `editT`; the lane B release build with -NoPublish) and then `chainR13.ps1` (TrainingFieldTests).
  Both wait for `CHAIN9_DONE` in chainR9.out, which is there. Then tune: `balance_retune.py --apply`, the spread shares
  (`EncounterSession.Spread.cs`) to x1.7 against three, and the spirits so pet and player land on 62-72; report the table and the tank
  ranking to Chris; show him the training field in a build (republish only with his OK).

- **2026-10-13, 12:42 on (Claude, after the restart):** chainR12 failed to compile (a comment had swallowed the end of a harness line);
  fixed. Then the harness itself was found unfair, four ways (BALANCE_REPORT.md round 6): the test scene's other sentries took the area
  spells (single-target numbers padded), the dummies were re-picked each fight (once off the navmesh: the Druid cast nothing), the
  spread cache never switched on, the tank test hit for a flat 40 with threat piling up. All fixed; the honest table at 13:23 (27 of 40
  in range), the tank ranking (Warrior, Shaman Stonebound, Druid Barkhide, Mason, Paladin Oathguard), the retune applied (d2a4880).
  EditMode 461/462 (WorkshopDataTests insisted on "the last six" props; now the append rule, 37106f1). TrainingFieldTests 3/3. The
  13:27 training frames (`D:\crulanda-work\ui-captures\training-20261013-1327`) showed the training dummy's nameplate floating off its
  model (its agent snapped it away) and the HUD cut off under the saved layout: each dummy's look now on its own actor, the capture
  frames the HUD (37106f1, 740f779).
  **Confirm run 13:55: 40 of 40 builds in range** (BALANCE_REPORT.md; the Stormcaller's splash eased once more after it, untested).
  Party-of-five healing reported (the Paladin, Salt-Mender and the Cantor leave a party low). **Open for Chris:** a party target for
  healers; the single-target builds whose own spells hit several foes (the Unwriting, Thornsong, Bonecaller, Sifter, the Shaman's kit).
- **The training field became a training yard** (Chris, ~13:40: "why is one in the river? sparring area needs to resemble something like
  this? try the small props pack"; then "two archer targets upside down", "two dummies upside down", "area is not closed in"):
  `Docs/TRAINING_YARD.md`. On dry ground at (58, 26), a closed ring of plank panels, tents, banners, dummies, butts, a healer's corner; the
  Small Props Pack (CC0) imported to `Resources/Props/SmallProps` with an atlas material and half turns for its upside-down models.
  TrainingFieldTests 4/4, EditMode 462/462; frames `D:\crulanda-work\ui-captures\training-20261013-1417` sent to Chris. Lane B holds a
  release build of all this (not published: republish only with Chris's OK, saves backed up first).

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
1. **Chris's reactions** to the woods, the motions, the effects, the butts and the trap (the frames named under Start here).
2. **The Adit rework** (Chris: "adit dungeon need to be more complex like wow's wailing caves or the deadmine"; "you can start expanding
   adit so not so empty next on the agenda"): branch `adit-rework`, worktree `D:\code\mmo-adit`, `Docs/ADIT_REWORK.md`. Build steps 1-3
   written, the twelve new elites' moves and loot written (sections 14-17). Its first Unity run: EditMode 488/495, Adit PlayMode 19/30.
   Red, in the order to fix (section 16): the cave web's joins (the Cutting Drift's lip into the Foundry), rock tops the navmesh bakes
   on, the Sandthrone pickets 12 m up with no way there, two overseers 1.2 m apart, the run order from the bridge's tower, Lusk's door
   after his death, the planner's modes test. Then S4-S7 (the releases, the Wake, the NEW moves, the cave map), then Chris's decisions
   (section 1), then merge. Test with `rt.ps1` against the worktree's Assets (chainR68.ps1 is the pattern).
3. **The Stylized Newbie Weapons Pack** (downloaded 12:01): its weapons are skinned meshes driven by the pack's own character script;
   bake each to a plain mesh (an editor method) before `model.weapon` looks can hold them. Low-level gear.
4. The hoe stroke's measures under the ploughing clip (FieldWorkTests: a stroke every 1-3 s), if the final run shows it red.
5. Spell effects, second pass: each class's own colours and shapes where the packs' stock looks repeat (the healing glow is one green
   for every healer; a cast's hand glow is the same sparks for all).
6. Bonecaller tiers 4-6.
7. An enemy that applies the grey (Adit/Wasting).
8. The sim-follow flake.
9. GroundingTests' sim 2.8 cm up; the parked tool shaft (VillageDayTests); EliteFightTests under -nographics.
10. The three later dungeons (DUNGEON_DESIGN.md) when the level cap rises past 15.
11. Republish only with Chris's OK; back up the save first (`chainR61.ps1` is the pattern).

## Key docs (in `Docs\`)
- `BALANCE_REPORT.md`: harness rounds v1-v8, caps, retunes.
- `RACES.md`: canon check, race storage, orc plan.
- `CLASS_CLERIC_SHAMAN.md`: design and status.
- `PLAYTEST_NOTES.md`: Chris's words, numbered notes, TODOs.
- `ZONE_FILL_PLAN.md`: note 172.
- `HANDOFF_ARCHIVE.md`: older handoffs.
- `D:\code\mmo\CLAUDE.md`: the rules.