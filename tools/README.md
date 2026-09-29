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
- The finished game goes to `...\Codex\2026-09-28\hel\outputs\Crulanda-Playable\Crulanda.exe`
  (left alone if the game is running at the time). Logs: `...\hel\work\test-builds\<time>`.

## Inside the Unity editor
Menu **Crulanda > Test Build**: *Build Test Player*, *Build and Run Test Player*, *Run Last Test Player*.
These build the project you have open (into `New Unity Project\Builds\Crulanda`).

## Game flags (for shortcuts or the command line)
`--crulanda-temp-save` throwaway save · `--crulanda-class class.druid` pick the character ·
`--crulanda-world-capture <folder> [--crulanda-zone zone.khaven]` scenic screenshots ·
`--crulanda-ui-capture <folder>` talent/HUD screenshots. In development builds, **F10** jumps to level 10.
