param([string]$Exe = 'D:\crulanda-work\encounter-validation-b\Builds\Crulanda\Crulanda.exe', [string]$Out = 'D:\crulanda-work\run-captures\adit-group',
      [string]$Zone = 'zone.adit', [string]$Class = 'class.paladin', [int]$TimeoutSec = 3300, [string]$Mode = '',
      [switch]$Raid, [int]$Pulls = 10)
# A group's run of a dungeon on the screen, timed and photographed (RunCapture; Chris, 2026-10-11: "need to show a test run of the
# dungeon with a group"): you as $Class at level 11 with four sims (a Warrior tanking and leading, a Druid, a Ranger and its wolf, a
# Rogue), the gates open, at three times speed. Writes run-log.txt (every camp, death, mend, item), run-summary.txt (the whole, with the
# meter) and a shot every four game minutes and at each elite into $Out. Run it on lane B's build (release_build.ps1 -Sync -NoPublish or
# build_and_tour.ps1), windowed, while nothing else uses the screen; Chris's screen prefs are saved and restored round it.
# -Mode quick|full: the leader's quick run (only what must be done) or full run (every side room and side boss); empty: the standard run.
# (The Adit rework: the bosses' doors are left to the bosses; the log has a line per wing, regroup and gate wait.)
# -Raid (WORLD_BOSS_1.md 3.6, build step 6): the group run at 10 against the Unraveller in the Lowtowns instead: you at 20 and nine sims
# (2 tanks, 3 healers, 5 damage with you), salt and harpoons in every bag, a raid on the Drain Reach's apron, her brought up at once
# (--crulanda-unraveller-now), -Pulls pulls (10), a seed each. raid-log.txt reads each wipe round its first death; run-summary.txt the kill rate.
# Out to D:\crulanda-work\run-captures\unraveller-raid unless -Out says otherwise.
if ($Raid) { $Zone = 'zone.lowtowns'; if (-not $PSBoundParameters.ContainsKey('Out')) { $Out = 'D:\crulanda-work\run-captures\unraveller-raid' } }
New-Item -ItemType Directory -Force $Out | Out-Null
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Save
$log = Join-Path $Out 'run-player.log'
$runArgs = @('--crulanda-run-capture', ('"' + $Out + '"'), '--crulanda-zone', $Zone, '--crulanda-class', $Class, '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"'))
if ($Mode) { $runArgs += @('--crulanda-run-mode', $Mode) }
if ($Raid) { $runArgs += @('--crulanda-raid', '--crulanda-unraveller-now', '--crulanda-raid-pulls', $Pulls) }
$g = Start-Process $Exe -PassThru -ArgumentList $runArgs
$g | Wait-Process -Timeout $TimeoutSec
if (-not $g.HasExited) { 'TIMED OUT (the game is still running)' }
Select-String -LiteralPath $log -Pattern 'RUN_CAPTURE_DONE|Exception|887a' | Select-Object -First 4 | ForEach-Object Line
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Restore
