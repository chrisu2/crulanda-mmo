param([string[]]$Zones = @('zone.oakhaven', 'zone.khaven', 'zone.peaks', 'zone.ashrim', 'zone.verdant'), [switch]$NoTests, [switch]$NoExtra, [switch]$NoTour, [switch]$NoBuild)
# The whole check before a publish, meant to be started DETACHED (start_detached.ps1) so a tool's time limit cannot kill Unity
# part-way: all tests, the player build and the zone tours, then the HUD, wardrobe and loot captures. Everything it prints goes
# to hel\work\full-run.log; it writes full-run.done (with the summary lines) when it is finished.
# -NoTour: build the player and take the HUD, wardrobe and loot captures, but tour no zone (for a round that does not change
# how the world looks). -NoBuild: tests only. -Zones takes one zone or a comma list ("zone.khaven,zone.verdant"; through
# start_detached a list arrives as one string).
$Zones = @($Zones | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
if ($NoTour) { $Zones = @() }
$work = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work'
$log = Join-Path $work 'full-run.log'; $done = Join-Path $work 'full-run.done'
if (Test-Path $done) { Remove-Item $done }
"started $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Out-File $log -Encoding utf8
function Step([scriptblock]$b) { & $b 2>&1 | ForEach-Object { "$_" } | Tee-Object -FilePath $log -Append }
if (-not $NoTests) { Step { & 'D:\code\mmo\tools\validation\run_tests.ps1' } }
if (-not $NoBuild) { Step { & 'D:\code\mmo\tools\validation\build_and_tour.ps1' -Zones $Zones } }
if (-not $NoBuild) { Step { 'shader errors: ' + (Select-String -LiteralPath (Join-Path $work 'encounter-validation\tour-build.log') -Pattern 'Shader error' | Measure-Object).Count } }
if (-not $NoExtra -and -not $NoBuild) { Step { & 'D:\code\mmo\tools\validation\capture_extra.ps1' } }
"finished $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Out-File $log -Append -Encoding utf8
Select-String -LiteralPath $log -Pattern 'total=|FAIL|Build Finished|BUILD FAILED|CAPTURE_DONE|shader errors|NO RESULTS|Exception' | ForEach-Object Line | Out-File $done -Encoding utf8
