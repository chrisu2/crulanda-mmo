# Crulanda: Claude handoff (Start here rewritten 2026-10-15 ~16:00; covers the Adit finished on the afternoon of Oct 15)

## Start here

**Current state: 2026-10-15 afternoon. Chris: "i told you to finish adit before shutting down" and a deadline at 18:00.** The Adit
rework is finished in code and merged into `feet-bow-adit-kit` (the working branch, PR #2); `adit-rework` (worktree `D:\code\mmo-adit`)
is the same. Nothing is VERIFIED until Chris sees it; talk to him in game terms (memory: plain-words-no-run-names, finish-the-named-task).
- **Chris's answers (ADIT_REWORK section 1):** respawn 20 minutes; keep the ~75 minute run; build the Cove; add the Foundry's buckets.
  Raids: the Spire at 30 is the first, twenty players. Class sets: "6 pieces. 2/6 give 1 special, 4/6 another ... 6/6 ... 3 special
  abilities and these are class specific"; the Adit's are tier 1 (dungeon), higher tiers from raids (loot DESIGN.md 3.6, 3.8).
- **What landed today (CHANGELOG "2026-10-15, 12:40-18:00"):** the Cove (S6), the polish (S7), eleven class sets, the deck-placement bug
  and the rails it stood up, the far-prop culling kept off cave walls, the dungeon map (`tools/wip/dungeon/adit_map.py`, `/loc`), the
  sim leader's give-up timer and long walk back, the run bot's unsearchable bodies.
- **Speed:** the Adit with the Cove 100 fps at the yard and 125 in the Tally Hall (from 13 and 17 before PropCull and sleeping mobs).
- **Tests:** the Adit play set 27/27 at 16:15 (AditPolish incl. the leader sent from the yard to Nix, AditCove, AditTests, AditCaveBuild, AditNav); the party tests 22/22 at 16:39 (SimPartyTests, GroupRunTests) with damage by role. **Group run:** The standard group run (a Paladin bot, a Warrior tank, a Druid healer, a Rogue, a Ranger) was played three times on Chris's screen: the first stood an hour (the leader gave Nix up after a wipe, the bot at a body it could not search), the second stood in water and fell off the rope bridge (rails opened by R129's CarriesOn rule beside the bridge tower), the third (16:22-16:31, every fix in) cleared 15 of 51 camps in 21 minutes, walked back from the yard after a wipe without stalling, and was stopped at its third death at Nix (Nix and the Rock-Eater stun the tank every 14 s; the party cannot beat him): Nix is a balance item, not a broken dungeon. The bot now takes each path corner (it cut the decks' turns and stepped off their open junctions) and counts swimming as stuck..
- **Open, in order:** (1) **Nix and the Rock-Eater: half fixed (17:21 build).** Four group runs wiped at the duo. Found (logs and a helper's sums): the healer mended whoever had the lowest share of health, never the tank first (now: the tank first under 60%); the beast's every blow sat at the 60% cap (235 on a 392 tank, every 2.6 s, against ~30 healing a second), so easing Rock-Crush did nothing (now its hit is halved, health .9, its Shaken on its own foe only). `AditNixFightTests` (new, PlayMode: the run's party placed at the duo, ~2 min) then killed the beast in 61 s with the tank up, but lost to Nix on foot (.8 of an elite, 7,392 health, the party ~35 damage a second: 140 s, his enrage killed the tank at 204 s). Nix at .5 broke the duo's design range (SocialRulesTests: Nix .7-1, both 1.5-2.2) and the solo-elite rules, so he is back at .8. Tried at 17:25 and reverted: Nix's hit .6 with a later, milder enrage let a lone Warrior and a lone Barkhide Druid beat him (the solo rules: an elite of your level wants Mira), so Nix's own numbers cannot come down; the fix is the sim party (its damage on him, ~35 a second, and the tank's survival: the healer's 31 a second). Next: raise the party's damage on him (the sims' role damage, the bot's kit use) rather than Nix, and make AditNixFightTests reliable (its second run the sims never engaged and you died alone at 15 s: check the pull and the placement); then the group run to the Cove; (2) monsters' and sims' crowd control and area moves (Chris asked: most mobs have none; sims only the Rogue/Archivist hold and interrupts): one signature move a mob type, a control move for the classes without one, an area move for sims at three or more mobs; (3) the Cove's water still shows bright streaks in the tour frame near the camera (probably the tour lamp; check in play); (4) sims wear class sets for looks only; the action bar shows the old cooldowns; (5) the fixture and tour captures for Chris (the Cove, the buckets) and his play-through.
- **Helpers' worktrees** (`D:\code\mmo-cove` adit-cove, `D:\code\mmo-polish` adit-polish, `D:\code\mmo-sets` class-sets) are merged and
  can be removed (`git worktree remove`), their branches deleted, once Chris is happy.
- **Phase 8.6 and Phase 9 wait:** the 14 decisions at the end of PHASE8_6_LIVING_WORLD.md (defaults proposed in chat 2026-10-15 ~13:00:
  8.6 now, looks wait on 8.5, three craft slots with Leatherworking and Weaving, coin-only recipes, 30-minute buffs, bigger inns with
  double doors and taprooms everywhere, step aside and shelter in rain); Phase 9 first zone to be picked by Chris; the orc race playable
  next (Races.cs has the rule, no figure or pick yet).

## Oct 15 small hours (history; Start here as it stood at 04:10)

**Current state: 2026-10-15, about 04:30, the PC shut down at Chris's word** ("once all is done and clear. shutdown the computer").
Chris played the Adit build through the small hours; his words verbatim in PLAYTEST_NOTES 199-225, what was done in CHANGELOG's
2026-10-15 entries. Nothing is VERIFIED until Chris has seen it in his build. Talk to him in game terms, not run names
(memory: plain-words-no-run-names).
- **Branches:** `feet-bow-adit-kit` (PR #2) and `adit-rework` (worktree `D:\code\mmo-adit`, main merged in to the last commit), both
  pushed. Do not merge either to `main` without Chris. **The published build comes from `adit-rework`.**
- **Published:** 04:28 from `adit-rework` with everything below (the save backed up to `save-backups/20261015-0428-before-closing-publish`). Save backups in `D:/crulanda-work/save-backups/20261015-*`.
- **The Adit's lag (notes 218, 225) is the night's big fix:** with the frame cap off, the yard went from 13 to 124 frames a second and the
  Tally Hall from 17 to 166 (the probe in the Adit camera tour: `D:/crulanda-work/world-captures/adit-tour/adit-perf.txt`). The cost
  was the zone's props (the whole dungeon drawn from anywhere: 15,857 renderers, 56 million triangles) and its 186 mobs. Now in a zone
  with caves a prop more than 70 m from the camera is not drawn (`PropCull`), and a camp's idle mob more than 85 m from you sleeps
  (`EncounterEnemy.Dormant`; wakes at 75 m, or the moment a call, threat or foe reaches it; road patrols never sleep). Watch for pop-in in
  the biggest halls (the Foundry) and for any event whose mobs should move while you are far: they stand until you come or call them.
- **New tonight, for Chris to look at (BUILT, OPEN):** the walk-in barns' walls no longer flicker; Nerve stays at 0 out of a fight; Mira
  runs out of mana in long fights, warns, drinks between fights, and casts Briar Lash and Kindled Seed when nobody needs healing; cave
  water without sunlit streaks, glow or foam lines; roamers outside half the open-air camps; worked nodes vanish whole and return sooner
  with more people about; the Sound panel in the pause menu; the chapter-house doors open; the chapel's tour frame fixed.
- **Asked of Chris, not answered:** whether "slow the progress down" (note 219) also means slower levelling (less XP); the 14
  decisions at the end of `PHASE8_6_LIVING_WORLD.md` and the 13 at the end of `DUNGEON2_DESIGN.md`; the bandit camp, the Bandit King and
  loot.
- **Open work, in order:** Chris's reactions (the last tests: EditMode 22/22, PlayMode 35/36 at 04:10 with the one red fixed and BuildingGroundTests 2/2 at 04:27; every footing now 3 cm further out, so check a few buildings' looks); the fixture cave's two reds in `AditCaveBuildTests` (the test cave's rail and
  loop, not the Adit's); sound effects and ambient sounds for the Sound panel's two empty channels (`AudioMix.Effective`); the Adit
  played through; Phase 8.6 once Chris decides.
- **How work runs:** up to three helpers (Chris: "you can run 3 bots at most"), none running Unity; compile with the scratch Roslyn
  check (`scratchpad/ccf/build.py` for main, `scratchpad/cc/build.py` for the worktree; a new session must rebuild these, they live in
  the old session's temp folder), commit by path; one Unity job at a time through hidden chains `D:/crulanda-work/chainRNN.ps1`
  (R124 the last). Chains wait while the game is open. Never write a chain's paths through Python without raw strings: a backslash-a in
  "\adit-perf" became a bell character tonight.

## Oct 14 night (history; Start here as it stood at 23:00)

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
  Pen, the Cutting Drift, the Foundry, the Muster Yard, the Chapel or the Breach: the R87 map (21:12) then showed most "unreached" floor was PathComplete from a
  reached sample metres away: one `NavMesh.CalculatePath` from the yard gives up part way through the drawn-out dungeon, so the
  camps test, the breadcrumbs and the run order were measuring the search budget, not the caves. `NavReach.Length` now searches
  leg by leg without giving up on a leg that ends farther off as the crow flies, and the camps test and the map use it (R88: Adit classes 16/23, AditGateTests green); still a leg-by-leg search ends in pockets,
  so the map now also floods by hops (R90, the measure of the mesh itself): the River, the Dry Ledge, the Breach, the Quiet Miner's
  Drift, the Pitch Pit and the Scriptorium are wholly reachable; **really cut off were four places;** the Pen was opened at
  21:48 (the Spoil heaps camp's dressing blocks had walled the strip beside Lusk's stage: the camp is on the west side now, R92).
  The Cutting Drift and the whole Geode Foundry opened at 21:56 (a knoll face wider than the drift's tube: `Cut` now tries each
  edge's middle). The chapel opened at 23:41 (the altar stood in the doorway to the cloister stair) and the island under Lusk's stage
  went at 00:13 (shelves inside a solid deck's body): **every floor of the Adit is reachable from the yard** (R99), the camps,
  gates and breadcrumbs tests green, the Adit nav set 19/23 (R100, 00:24). The "solid" with mesh on its top was a buried cave's knoll poking into the hall (fixed 00:49).
  Three helpers worked the small hours (Chris: "you can run 3 bots at most"): `AditDataTests` (every reference in the Adit's data
  resolves), the Adit camera tour (`Crulanda.exe --crulanda-adit-tour <dir> --crulanda-zone zone.adit --crulanda-temp-save`,
  28 frames: `D:/crulanda-work/world-captures/adit-tour` once it has run), and the release of `after` camps (bosses' relief shifts,
  the Wake's waves), which the data named and nothing spawned. **The Phase 8.6 plan** is written:
  `Docs/PHASE8_6_LIVING_WORLD.md` (PROVISIONAL; fourteen one-line decisions for Chris at its end; the inn-door jam's cause is in it:
  a 1.6 m door and a navmesh baked at agent radius .5 leave a 0.6 m strip, one body at a time). **The Adit's nav, camps, gate and release tests are 26/26 (01:41):** every floor reachable, the way home
  down the cloister stair (its foot was 1.5 m below the Gallery's floor), the bosses' relief shifts, the Wake's waves and the keg's
  release written and tested. Left: the fixture test's knoll ray and rail check; the built rooms on the dungeon packs (a helper);
  then play the run. The Adit camera tour ran once (28 frames, too dark: a lamp on its camera now, re-run pending); the release of `after`
  camps is being written (ADIT_REWORK.md section 18).
  The real short blocks the map names: the Pen's floor from the hall beside Lusk's stage (a "Solid" at (7, -24, -183)), and the
  Cutting Drift's own floor past its Company door. Start the next Adit session from `adit-nav-diagnostics-R88.txt`. The diagnostics (`AditNavDiagnostics`, lane A) write
  `D:/crulanda-work/encounter-validation/adit-nav-diagnostics.txt`: the deck tables, a floor map of every hall (reached from the yard
  or not, what stands by the first unreached sample), each gate's crossing. Run the Adit set with a copy of `D:\crulanda-work\chainR86.ps1`.
- **The small hours (00:40-01:45; CHANGELOG's top entry):** three helpers at a time (Chris: "you can run 3 bots at most"): Oakhaven's open
  ground filled (eleven places, ten secrets), secrets' clues and villagers' stories in every zone with a Lore tab, the Phase 8.6 and
  Hallow's Creek plans (decisions for Chris at their ends), the Adit's releases, camera tour and data check. In flight when this was
  written: the dungeon packs into the builder (worktree), the water/grass/candle shaders, zone music and the Fantasy props (main).
  Their files land uncommitted in each repo: compile with the scratch Roslyn check, commit by path, run the touched tests.
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