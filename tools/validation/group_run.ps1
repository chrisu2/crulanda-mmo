param([string]$Exe = 'D:\crulanda-work\encounter-validation-b\Builds\Crulanda\Crulanda.exe', [string]$Out = 'D:\crulanda-work\run-captures\adit-group',
      [string]$Zone = 'zone.adit', [string]$Class = 'class.paladin', [int]$TimeoutSec = 3300)
# A group's run of a dungeon on the screen, timed and photographed (RunCapture; Chris, 2026-10-11: "need to show a test run of the
# dungeon with a group"): you as $Class at level 11 with four sims (a Warrior tanking and leading, a Druid, a Ranger and its wolf, a
# Rogue), the gates open, at three times speed. Writes run-log.txt (every camp, death, mend, item), run-summary.txt (the whole, with the
# meter) and a shot every four game minutes and at each elite into $Out. Run it on lane B's build (release_build.ps1 -Sync -NoPublish or
# build_and_tour.ps1), windowed, while nothing else uses the screen; Chris's screen prefs are saved and restored round it.
New-Item -ItemType Directory -Force $Out | Out-Null
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Save
$log = Join-Path $Out 'run-player.log'
$g = Start-Process $Exe -PassThru -ArgumentList @('--crulanda-run-capture', ('"' + $Out + '"'), '--crulanda-zone', $Zone, '--crulanda-class', $Class, '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"'))
$g | Wait-Process -Timeout $TimeoutSec
if (-not $g.HasExited) { 'TIMED OUT (the game is still running)' }
Select-String -LiteralPath $log -Pattern 'RUN_CAPTURE_DONE|Exception|887a' | Select-Object -First 4 | ForEach-Object Line
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Restore
