# Crulanda — handoff (current state)

Updated 2026-10-11, 00:35 (America/New_York; the work it describes ends with the build published at 00:33). One page on purpose: what is true now, where things are, what is open, what is
next. History lives in CHANGELOG.md and HANDOFF_ARCHIVE.md (the old resume notes of 2026-09-28 to 2026-10-10, archived tonight
because they had grown to 146 KB of notes that contradicted each other). Replace this page when the state changes; do not stack.

## Read first
1. `CLAUDE.md` (repo root): the rules and where everything is.
2. `GAME_BRIEF.md`: what the game is and the bar it is held to.
3. This page, then `PLAYTEST_NOTES.md` (Chris's reports and their status), `DUNGEON_DESIGN.md` section 0 (the dungeon brief,
   verbatim), `CHANGELOG.md`, `WORLD_ZONES.md`, `QUEST_DESIGN.md`, `SAVE_FORMAT.md`.
4. The memory notes (C:\Users\ChrisWin11\.claude\projects\D--code-mmo\memory): reports stay open until Chris confirms them in his
   own build; record his briefs verbatim the same day; **check the clip import before touching animation** (tonight's lesson).

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

## Done tonight (branch feet-bow-adit-kit)
- **Feet and bow, fixed at the root; Chris confirmed both in the captures.** The cause was the clip import: `Editor/CharacterImport.cs`
  built every vendor clip's avatar from its own file. Kevin's clips now copy his HumanM/HumanF avatars. Every runtime foot fix of the
  evening is gone. Bow: the archer faces her target while fighting standing, and the bow is set by its own mesh (rounded side to the
  mark, string to the archer).
- **Built halls in the Adit (room builder, D9):** the Geode Floor's foundry hall and the Rock-Eater's berth; the Ember Vent as the Cult of
  Ash's chapter-house (ash-garden, a nave with a scriptorium and a drill yard, a chapel with a great window).
- **Hollow Men are apparitions** (see-through ashen grey, violet eyes). GAME-ONLY look: book 1 ch.4 has them as solid stone.
- **The Adit drawn out three times** (Chris: "3x, full-length run"): 1,353 m of cave, the zone 800 m square
  (`tools/wip/dungeon/adit_layout.py`; the Adit code's places are in the first Adit's metres times X=3, CW=2.5, DW=1.5, CH=2).
- **Machine words replaced** in the Adit's item texts ("engine oil" became "drill-grease"; Chris: replace them).
- Docs: this handoff rewritten, the old one archived; GAME_BRIEF brought up to the code.
- **After midnight (2026-10-11): the escort works on the drawn-out Adit, and saves deep in it load.** The Weaver's waves come by her
  distance to the lock (27 m, then 13 m); the carriage stands by Danner; the Quartermaster's call is 30 m (Danner is 32 m from her).
  A save in the Rail Hall (80-90 m down) used to come back a new character (the limit was 60 m down; now 250, `EncounterSave.Placeable`).

## Tests and builds
- EditMode 445/445 (00:28, 2026-10-11); the escort test again after the call change, 3/3 (00:32).
- Adit suites on the drawn-out layout, all green (00:10-00:28): AditWeaverTests 3/3, AditTests 5/5, AditGateTests 8/8,
  RockEaterTests 2/2, CaveTests 9/9, EliteFightTests 6/6. The escort test waits for all six of the waves, checks where each comes
  out and that the second can reach her inside half the game's lock time, and saves and reloads with the fight over.
- **Published playable build: 00:33 on 2026-10-11** (release, 0 shader errors, from commit 9fc71fc): the drawn-out Adit with the escort
  and save fixes, for Chris to walk. Checked in the published files: the new code and the new layout are in it. The one before was
  20:41 on 2026-10-10 (feet and bow).

## Open (see PLAYTEST_NOTES.md for each)
- 113: the Adit bigger **and "more to do"**: the scale is done and tested; the content is next (DUNGEON_DESIGN 0.25, wing by wing).
- **114-118, a tester's report Chris pasted at about 22:20 (on the 20:41 build), all open:** the HUD clipped at the left and top (chat
  and party frame cut, no player frame or minimap seen); after alt-tab or chat, keys ignored until a click in the window; the shop not
  closing on "Goodbye"; the camera going inside walls in buildings; "/invite Thatch" not working (the Who list's Invite does).
- 112: the bowstring is not drawn back at full draw and no arrow shows; Morro's idles turn a foot out (import them from a source avatar
  like Kevin's); the walk's toes dip 3-6 cm in places.
- 99 (parts): a mob answering a call fights with its weapon sheathed. 101: the helmets are untextured code shapes.
- 10 baked cave lighting, 14 a PDF of the work, 23 frame rate, 24 tree seams.
- **Kevin Iglesias's packs** (Chris added them 2026-10-10; copies in `D:\crulanda-work\downloads\kevin-iglesias`): **Basic Motions**
  is in the game (walk, run, sprint, jump, idles, talk; his two models since tonight); **Archer** only in part (BowShot01 Load, Hold,
  Release and BowIdle01); **Melee** is not imported yet (real attack swings for every class: import it into a validation lane, never
  Chris's open project, into Resources/Characters/Animations/KI with its metas, the clips copying his avatars like the rest).
- **Out of date, not yet refreshed:** `marketing/steam-blurb.md` (2026-09-30: four zones, levels 1-10, Warrior or Druid; it also
  names the Salt-Menders, see the lore audit). `CLAUDE.md` now names lane B.
- **Lore (`LORE_AUDIT.md`), all decided by Chris on 2026-10-10 and done in the data:** the Salt-Menders as book 3's forerunners,
  Khaven's wood renamed the Stillwood, the Peaks toll CANON-EXPANDED (the draft chapter_5.md), Klyther as the novels have him; the
  classes are GAME-ONLY archetypes; when sources disagree, **novels, then the world bible, then the ledger**. These rules and the
  no-machine-words rule head CRULANDA_CANON.md (the rest of it is still the Phase 0 stub: fill it from the books).

## Next, in order
1. Chris walks the drawn-out Adit (published 00:33, 2026-10-11); his word on it before PR #2 is merged.
2. Notes 114-118 (the tester's report), the two that spoil every session first: **115, keys ignored after alt-tab or chat until a
   click in the window** (the character stands still), and **117, the camera going inside walls in buildings**; then 114 (the HUD
   clipped at the left and top: chat, party frame; no player frame or minimap seen), 116 (the shop not closing on "Goodbye"), and
   118 ("/invite Thatch" fails, the Who list's Invite works). Each with a test that measures it, frames, and Chris's word before DONE.
3. "More to do" in the Adit, a wing at a time, frames shown to Chris before the next (DUNGEON_DESIGN 0.25).
4. Note 112's three items.
- Leads for 115 (not yet tested): the chat's "typing" flag is only cleared while the chat box is drawn; and the input system's default
  focus behaviour turns the keyboard off when the window loses focus (no InputSettings asset; runInBackground off).

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
