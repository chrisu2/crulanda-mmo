# Crulanda MMO (Unity 6)

Single-player simulated MMORPG. Read `New Unity Project/Docs/GAME_BRIEF.md` first (what the game is and the bar it is held to), then `Docs/CLAUDE_HANDOFF.md` (current state), then
`Docs/CHANGELOG.md`, `Docs/WORLD_ZONES.md`, `Docs/QUEST_DESIGN.md` and `Docs/SAVE_FORMAT.md`.

## Where everything is
| What | Location |
|---|---|
| Project and git repo (branch `main`) | `D:\code\mmo`; the Unity project is `D:\code\mmo\New Unity Project` |
| Backup (full copy including `.git`) | `J:\claude\unity projects\mmo`; run `tools\Backup.ps1` |
| Lore (novels, world bible, maps) | `D:\code\crulanda` |
| Unity 6000.6.3f1 | `D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe` |
| Validation copy (tests and builds run here) | `C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation` |
| Playable build | `C:\Users\chris\Documents\Codex\2026-09-28\hel\outputs\Crulanda-Playable\Crulanda.exe` |
| Chris's save | `C:\Users\chris\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter` |
| Save backups | `C:\Users\chris\Documents\Codex\2026-09-28\hel\work\save-backups` |
| Screenshots | `...\hel\work\world-captures`, `...\hel\work\ui-captures` |
| Handoff copy | `...\hel\outputs\Crulanda-Claude-Handoff.md` |

## Rules
- Every file made for this game goes inside `D:\code\mmo`.
- After a meaningful piece of work: commit, then run `tools\Backup.ps1`.
  - The backup is additive. `-Full` includes Library, and `-Mirror` makes an exact copy.
  - J: is slow (about 2 MB/s), so a full copy takes about 10 minutes. If J: is missing, say so.
- Git:
  - It's at `C:\Program Files\Git\cmd\git.exe` and not on the agent shell's PATH.
  - The author is set in this repo's local config (`Chris Underwood <chrisu2@gmail.com>`), so a plain `git commit` works. There is no global identity.
  - Git LFS is enabled for this repo only (`.gitattributes` sends images, models and audio through it).
- `D:\code` is a separate, unrelated repo (other projects, VM images and model weights, owned by an old Windows account).
  Never commit this game there.
- Don't run Unity against the user's open project. Sync into the validation copy and test there.
- Back up Chris's save before any save-format change.
- Preserve `.meta` GUIDs.
- Label lore honestly: CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL. Don't copy WoW names or assets.
- Chris skims. When you need a decision, ask it first and short, or use the question prompt.
