param([string[]]$Zones = @('zone.oakhaven', 'zone.khaven', 'zone.peaks', 'zone.ashrim', 'zone.verdant'), [switch]$NoTests, [switch]$NoExtra, [switch]$NoTour, [switch]$NoBuild, [switch]$AllTests)
# The whole check before a publish, meant to be started DETACHED (start_detached.ps1) so a tool's time limit cannot kill Unity
# part-way. Everything it prints goes to D:\crulanda-work\full-run.log; it writes full-run.done (with the summary lines) when finished.
# Two lanes at once (Phase 5.0, 2026-10-05: about 110 minutes became about 60):
#   lane A (encounter-validation): EditMode, then the PlayMode fixtures the change needs (select_tests.ps1: those that name a
#     changed class, the world set for zone data, none for art and docs; -AllTests runs every one);
#   lane B (encounter-validation-b, a second copy of the project): the player build, the zone tours and the HUD, wardrobe and
#     loot captures, in a process of its own.
# A green run (no failures) records the commit it ran on in D:\crulanda-work\last-green.txt, which the next selection starts from.
# -NoTour: build and capture but tour no zone (the look is unchanged). -NoBuild: tests only. -Zones: one zone or a comma list.
$Zones = @($Zones | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
if ($NoTour) { $Zones = @() }
$work = 'D:\crulanda-work'
$laneA = Join-Path $work 'encounter-validation'; $laneB = Join-Path $work 'encounter-validation-b'
$log = Join-Path $work 'full-run.log'; $done = Join-Path $work 'full-run.done'; $logB = Join-Path $work 'full-run-b.log'
if (Test-Path $done) { Remove-Item $done }
"started $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Out-File $log -Encoding utf8
function Step([scriptblock]$b) { & $b 2>&1 | ForEach-Object { "$_" } | Tee-Object -FilePath $log -Append }
$git = 'C:\Program Files\Git\cmd\git.exe'; $head = (& $git -C 'D:\code\mmo' rev-parse HEAD).Trim()

# Lane B: its own copy of the project (made once from lane A, Library and all), then build, tours and captures.
$b = $null
if (-not $NoBuild) {
    if (-not (Test-Path (Join-Path $laneB 'Library'))) { Step { "lane B: copying the project (first time)"; robocopy $laneA $laneB /MIR /XD Builds /NFL /NDL /NJH /NJS /NP | Out-Null } }
    $zoneArg = '@(' + (($Zones | ForEach-Object { "'" + $_ + "'" }) -join ',') + ')'
    $cmd = "`$env:CRULANDA_VCOPY = '$laneB'; " +
           "& 'D:\code\mmo\tools\validation\build_and_tour.ps1' -Zones $zoneArg; " +
           "'shader errors: ' + (Select-String -LiteralPath '$laneB\tour-build.log' -Pattern 'Shader error' | Measure-Object).Count; " +
           $(if (-not $NoExtra) { "& 'D:\code\mmo\tools\validation\capture_extra.ps1'" } else { "" })
    $b = Start-Process powershell.exe -WindowStyle Hidden -PassThru -RedirectStandardOutput $logB -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $cmd)
    Step { "lane B started (build, tours, captures): pid $($b.Id)" }
}

# Lane A: the tests.
if (-not $NoTests) {
    $filter = if ($AllTests) { 'ALL' } else { & 'D:\code\mmo\tools\validation\select_tests.ps1' }
    Step { "PlayMode selection: " + $(if ($filter.Length -gt 300) { ($filter -split ';').Count.ToString() + ' fixtures' } else { $filter }) }
    Step { & 'D:\code\mmo\tools\validation\run_tests.ps1' -PlayFilter $filter }
}
if ($b -ne $null) { $b | Wait-Process -Timeout 5400; Step { Get-Content $logB } }
"finished $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Out-File $log -Append -Encoding utf8
# The log mixes UTF-8 (Out-File) and UTF-16 (Tee-Object) lines, which Select-String misreads (full-run.done came out empty on
# 2026-10-03): read it raw with the NULs dropped.
$summary = ([System.IO.File]::ReadAllText($log) -replace "`0", '') -split "`n" | Where-Object { $_ -match 'total=|FAIL|Build Finished|BUILD FAILED|CAPTURE_DONE|shader errors|NO RESULTS|Exception|selection' } | ForEach-Object { $_.Trim() }
$summary | Out-File $done -Encoding utf8
if (-not $NoTests -and -not ($summary | Where-Object { $_ -match 'FAIL|NO RESULTS|BUILD FAILED' })) { $head | Out-File (Join-Path $work 'last-green.txt') -Encoding ascii }
