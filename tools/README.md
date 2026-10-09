# Crulanda tools

## Test builds
**Easiest:** double-click `Build-TestPlayer.cmd`. It builds the latest project and starts the game with a
throwaway character (your real save is not touched). Takes about 3–4 minutes.

From a terminal (PowerShell or cmd) in this folder:

| Command | What it does |
|---|---|
| `.\Build-TestPlayer.cmd -Run` | Build, then play with your normal save |
| `.\Build-TestPlayer.cmd -Run -Fresh` | Build, then play a throwaway character |
| `.\Build-TestPlayer.cmd -Run -Fresh -Class druid` | Same, as a Druid |
| `.\Build-TestPlayer.cmd -Tests` | Run the automated EditMode + PlayMode tests first; stops if anything fails |
| `.\Build-TestPlayer.cmd -Tour` | Build, then screenshot every zone (HUD hidden) and open the folder |
| `.\Build-TestPlayer.cmd` + no switches from a terminal | Build only |

- It builds from a **copy** of the project, so it works while the Unity editor is open.
- The finished game goes to `D:\crulanda-work\outputs\Crulanda-Playable\Crulanda.exe`
  (left alone if the game is running at the time). Logs: `D:\crulanda-work\test-builds\<time>`.

## Backup
`.\Backup.ps1` copies all of `D:\code\mmo` (project, docs, tools and the `.git` history) to
`E:\claude\unity projects\mmo`.
- It's additive: new and changed files are copied, and nothing in the backup is deleted.
- By default it skips Unity's caches (Library, Temp, Logs). `-Full` includes them.
- `-Mirror` makes the backup an exact copy.
- Run it after committing. The backup drive is slow, so a full copy takes about 10 minutes.

## Inside the Unity editor
Menu **Crulanda > Test Build**: *Build Test Player*, *Build and Run Test Player*, *Run Last Test Player*.
These build the project you have open (into `New Unity Project\Builds\Crulanda`).

## Game flags (for shortcuts or the command line)
`--crulanda-temp-save` throwaway save · `--crulanda-class class.druid` pick the character ·
`--crulanda-world-capture <folder> [--crulanda-zone zone.khaven]` scenic screenshots ·
`--crulanda-ui-capture <folder>` talent/HUD screenshots. In development builds, **F10** jumps to level 10.
