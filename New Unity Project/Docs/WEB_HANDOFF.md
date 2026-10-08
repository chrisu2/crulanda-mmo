# Working on Crulanda from Claude Code on the web

Written 2026-10-08 for cloud sessions (claude.ai/code) that pick the game up while Chris's PC is off. Read this first, then
`GAME_BRIEF.md`, `CLAUDE_HANDOFF.md` (RESUME HERE), `CC_DESIGN.md`, `DUNGEON_DESIGN.md` and `ROADMAP.md` in this folder.

## 1. What you can and can't do here
- **You have:** the git repo (`chrisu2/crulanda-mmo`, private), all C# source, content JSON, docs and Python tools.
- **You don't have:**
  - Unity: no editor, no compile against UnityEngine, no tests, no builds, no screenshots.
  - The playable build.
  - The lore folder (the novels and world bible live only on Chris's PC at `D:\code\crulanda`).
  - The validation copies and the E: backup drive.
- Art files are Git LFS objects. In a cloud clone they may be pointer files: never edit, move or delete images, models or audio.
- So: write code, data, tests and docs carefully, then leave verification to the local session (section 5).

## 2. Rules (Chris's, plus what the cloud changes)
1. **Never push to `main`.** Work on a branch `web/<short-topic>` (one per task), push it, and open a draft PR into `main`
   titled with the task. The local session tests and merges it.
2. **Compile-safe without a compiler.**
   - Copy the patterns of the files around you; Unity 6 uses C# 9, so no newer syntax.
   - No new packages or assembly definitions.
   - Double-check every type and member you use exists (grep for it); the code is split across `partial` classes
     (`EncounterSession*.cs`, `EncounterEnemy*.cs`).
3. **`.meta` files.**
   - Every new file under `New Unity Project/Assets` needs a `.meta` with a fresh random 32-hex `guid` (copy a neighbour's
     `.meta` and change only the guid).
   - Never change an existing `.meta` guid.
4. **Tests.** Add or update NUnit tests next to the feature:
   - `Assets/Crulanda/Tests/EditMode` for data and rules; `Tests/PlayMode` for play.
   - A play test that loads a scene sets `SaveDirectoryOverride` in `sceneLoaded`, as every existing play test does, so it
     never touches Chris's save.
   - Read every test a data change affects and fix them in the same change (counts, lists, names); see `CHANGELOG.md` for
     how past changes cascaded.
5. **Content rules.**
   - Label lore CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL. Without the books, anything you invent is GAME-ONLY or
     PROVISIONAL; don't call it CANON.
   - Don't copy WoW names or assets.
   - Back up and bump the save format only with a migration (`SAVE_FORMAT.md`); Chris doesn't need his save kept, but old
     saves must still load.
6. **Log what to verify.** Append to `Docs/WEB_LOG.md` for each branch: what changed, which test fixtures to run locally
   (exact class names), and anything that needs eyes in the game (looks, layout, feel). Update `CHANGELOG.md` as the local
   session does.
7. **Chris skims.** Decisions go first and short. Ask before any multi-agent workflow and give a cost estimate (his usage
   budget is tight).

## 3. The game in one paragraph
A single-player simulated classic MMORPG in Unity 6. You and Mira (a healer companion) level 1-15 through Oakhaven 1-5,
Khaven 5-9, the Peaks 9-12, the Ash Rim 12-15 and the Verdant Shore 13-15 (all group mobs). Forty to fifty simulated
adventurers ("sims") live in the world and can join your party. Slow, grindy, difficult on purpose. Seven classes:
- **Warrior, Druid (four forms), Paladin, Ranger (wolf), Mage:** the first five.
- **Rogue and Archivist:** the 2026-10-08 crowd-control classes.

Crowd control (`EncounterEnemy.Control`): hold, stun, fear and silence, with diminishing returns, raid marks (Ctrl+1-4), mob
casts to interrupt, and boss phases (`EncounterEnemy.Boss`). The Sealed Adit (levels 10-12) is the first dungeon.

## 4. Good tasks for a cloud session (no Unity needed to write them)
In rough order of value; check `CLAUDE_HANDOFF.md` for anything newer.
1. **Sealed Adit quests and loot** (`DUNGEON_DESIGN.md` sections 6-7; quest JSON as in
   `EncounterContent/Quests/*.json`, loot as in `loot.adit.json`): the seven quests given in the Peaks and Khaven, and the
   boss loot tables. *Pure data plus QuestDataTests.*
2. **Dungeon D3, gates and keys** (`DUNGEON_DESIGN.md` section 9 step 3): a gate opened by a key item, a freed NPC or
   powder. *Code plus tests; the gate's look is checked locally.*
3. **Dungeon D4, the Weaver's escort** (section 4 [4]): Mother Quillet walks to the lock; two waves; Danner rises at
   half-matched. *Code plus tests.*
4. **Nix's two-part fight** (the Rock-Eater, then Nix on foot). *Code plus tests.*
5. **Mira's Hush on request**: a `/hush` chat command, or a button on her frame, that silences your target (4 s, 15 s
   cooldown; `HealerCompanion.Hush`). *Small.*
6. **Talent rows 4-6** for every class: designs and code that unlock when the cap rises to 30 (talent points are level + 1,
   so they can't be bought at 15). *Data and code; mark them as needing the cap.*
7. **Sim chatter for Rogues and Archivists** (`SimChatter.cs`): lines about sapping, lulling, marks. *Text.*
8. **Phase 9 design docs** (`ROADMAP.md`: the 16-30 road): zone outlines. *Without the novels, mark everything
   PROVISIONAL and list questions for Chris.*

Not for the cloud: anything about looks (zones, models, lighting, UI layout), the Medieval Village Kit import, or tuning
numbers that need play to judge.

## 5. How the local session picks your work up
When Chris's PC is back:
1. `git fetch`, then list the `web/*` branches and their PRs.
2. For each: read `WEB_LOG.md`, check the branch out into the validation copy (never Chris's open project), compile, and run
   the listed fixtures.
3. Fix, merge to `main`, build, publish when `Crulanda.exe` is closed, back up (`tools\Backup.ps1`), push.

## 6. Where things are in the repo
| What | Path |
|---|---|
| Code | `New Unity Project/Assets/Crulanda/Scripts/Encounter` (most of the game), `.../World` (zones, caves), `.../Gameplay`, `.../Combat` |
| Content | `New Unity Project/Assets/Crulanda/EncounterContent` (`Encounter.asset` classes and abilities, `Talents/*.json`, `Zones/*.json`, quests, items, loot) |
| Tests | `New Unity Project/Assets/Crulanda/Tests/EditMode`, `.../PlayMode` |
| Docs | `New Unity Project/Docs` |
| Tools | `tools/` (Python: icons `tools/art/make_icons.py`, dungeon layout `tools/wip/dungeon`; PowerShell: local only) |
