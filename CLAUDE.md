# Crulanda MMO (Unity 6)

Single-player simulated MMORPG. Read `New Unity Project/Docs/GAME_BRIEF.md` first (what the game is and the bar it is held to), then `Docs/CLAUDE_HANDOFF.md` (current state), then
`Docs/CHANGELOG.md`, `Docs/WORLD_ZONES.md`, `Docs/QUEST_DESIGN.md` and `Docs/SAVE_FORMAT.md`.

## Where everything is
| What | Location |
|---|---|
| Project and git repo (branch `main`) | `D:\code\mmo`; the Unity project is `D:\code\mmo\New Unity Project` |
| GitHub (private, for Claude Code on the web; pushed 2026-10-08) | `https://github.com/chrisu2/crulanda-mmo` (remote `origin`, LFS on) |
| Backup (full copy including `.git`) | `E:\claude\unity projects\mmo`; run `tools\Backup.ps1` |
| Spare clone of GitHub (Chris's extra backup, 2026-10-09) | `D:\mmo clone\crulanda-mmo`: leave it alone; all work happens in `D:\code\mmo` |
| Lore (novels, world bible, maps) | `D:\code\crulanda` |
| Unity 6000.6.3f1 (editor) | `D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`; Unity Hub is `C:\Program Files\Unity Hub\Unity Hub.exe` |
| Work folder (not in git; replaces the old `C:\...\Codex\2026-09-28\hel`, lost with C: on 2026-10-09) | `D:\crulanda-work` |
| Validation copy (tests and builds run here) | `D:\crulanda-work\encounter-validation` |
| Playable build | `D:\crulanda-work\outputs\Crulanda-Playable\Crulanda.exe` |
| Chris's save (Unity's default place, so it stays on C:; the Windows account is `ChrisWin11` since 2026-10-09, and the old save was lost with C:) | `C:\Users\ChrisWin11\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter` |
| Save backups | `D:\crulanda-work\save-backups` |
| Screenshots | `D:\crulanda-work\world-captures`, `D:\crulanda-work\ui-captures` |
| Handoff copy | `D:\crulanda-work\outputs\Crulanda-Claude-Handoff.md` |

## Rules
- Every file made for this game goes inside `D:\code\mmo` (working copies, builds and captures go in `D:\crulanda-work`).
- Keep nothing for this game on C: except Chris's save: C: was lost on 2026-10-09.
- After a meaningful piece of work: commit, then run `tools\Backup.ps1`.
  - The backup is additive. `-Full` includes Library, and `-Mirror` makes an exact copy.
  - E: is the backup drive (J: was the old one). If E: is missing, say so.
- Git:
  - It's at `C:\Program Files\Git\cmd\git.exe` (reinstalled 2026-10-09; may not be on the agent shell's PATH).
  - `D:/code/mmo` is in the global `safe.directory` list (the folder belongs to the old Windows account).
  - The author is set in this repo's local config (`Chris Underwood <chrisu2@gmail.com>`), so a plain `git commit` works. There is no global identity.
  - Git LFS is enabled for this repo only (`.gitattributes` sends images, models and audio through it).
- `D:\code` is a separate, unrelated repo (other projects, VM images and model weights, owned by an old Windows account).
  Never commit this game there.
- Don't run Unity against the user's open project. Sync into the validation copy and test there.
- Back up Chris's save before any save-format change.
- Preserve `.meta` GUIDs.
- Label lore honestly: CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL. Don't copy WoW names or assets.
- Chris skims. When you need a decision, ask it first and short, or use the question prompt.

## In a cloud session (Claude Code on the web)
If there is no `D:` drive (a Linux sandbox from claude.ai/code), Chris's PC is off and the rules above that need it don't
apply: no Unity, no `tools\Backup.ps1`, no validation copies, no builds. Follow `New Unity Project/Docs/WEB_HANDOFF.md`
instead: a `web/<topic>` branch and a draft PR (never push to `main`), compile-safe code with tests, new `.meta` files
with fresh guids, and an entry in `Docs/WEB_LOG.md` saying what the local session must run and look at.
