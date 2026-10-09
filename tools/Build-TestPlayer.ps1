<#
.SYNOPSIS
  Builds a Crulanda test player without disturbing the Unity editor you have open.

.DESCRIPTION
  Copies the project's Assets into the validation copy, builds a Development player there with Unity in batch mode,
  and (unless the game is running) refreshes outputs\Crulanda-Playable. Logs go to D:\crulanda-work\test-builds.

.EXAMPLE
  .\Build-TestPlayer.ps1                 # build only
  .\Build-TestPlayer.ps1 -Run            # build, then play with your normal save
  .\Build-TestPlayer.ps1 -Run -Fresh     # build, then play a throwaway character (real save untouched)
  .\Build-TestPlayer.ps1 -Run -Fresh -Class druid
  .\Build-TestPlayer.ps1 -Tests          # run EditMode + PlayMode tests first; stop if anything fails
  .\Build-TestPlayer.ps1 -Tour           # after building, screenshot every zone (HUD hidden) and open the folder
#>
param(
    [switch]$Run,
    [switch]$Fresh,
    [ValidateSet('warrior', 'druid')][string]$Class,
    [switch]$Tests,
    [switch]$Tour
)
$ErrorActionPreference = 'Stop'
$project = 'D:\code\mmo\New Unity Project'
$workspace = 'D:\crulanda-work'
$validation = Join-Path $workspace 'encounter-validation'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$logs = Join-Path $workspace ('test-builds\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$playable = Join-Path $workspace 'outputs\Crulanda-Playable'
New-Item -ItemType Directory -Force $logs | Out-Null

function Step($text) { Write-Host ''; Write-Host "== $text" -ForegroundColor Cyan }
function Unity([string[]]$arguments, [string]$log, [int]$timeoutSeconds) {
    $p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList ($arguments + @('-logFile', ('"' + $log + '"')))
    if (-not $p.WaitForExit($timeoutSeconds * 1000)) { $p.Kill(); throw "Unity timed out; see $log" }
}

Step 'Syncing project into the validation copy (your open editor is not touched)'
robocopy (Join-Path $project 'Assets') (Join-Path $validation 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Asset sync failed (robocopy $LASTEXITCODE)." }
Copy-Item (Join-Path $project 'ProjectSettings\EditorBuildSettings.asset') (Join-Path $validation 'ProjectSettings\EditorBuildSettings.asset') -Force

if ($Tests) {
    foreach ($mode in 'EditMode', 'PlayMode') {
        Step "Running $mode tests"
        $results = Join-Path $logs "$mode.xml"
        Unity @('-batchmode', '-nographics', '-projectPath', ('"' + $validation + '"'), '-runTests', '-testPlatform', $mode, '-testResults', ('"' + $results + '"')) (Join-Path $logs "$mode.log") 1200
        if (-not (Test-Path -LiteralPath $results)) { throw "$mode produced no results (compile error?). See $logs\$mode.log" }
        $run = ([xml](Get-Content -LiteralPath $results)).'test-run'
        Write-Host "$mode : $($run.passed)/$($run.total) passed"
        if ([int]$run.failed -gt 0) { throw "$mode had $($run.failed) failing test(s). See $results" }
    }
}

Step 'Building the Development player'
Unity @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $validation + '"'), '-executeMethod', 'Crulanda.EditorTools.EncounterBuildPlayer.Build') (Join-Path $logs 'build.log') 900
$built = Select-String -LiteralPath (Join-Path $logs 'build.log') -Pattern 'Build Finished, Result: Success' -Quiet
if (-not $built) {
    Select-String -LiteralPath (Join-Path $logs 'build.log') -Pattern 'error CS' | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
    throw "Build failed. See $logs\build.log"
}
$exe = Join-Path $validation 'Builds\Crulanda\Crulanda.exe'
Write-Host "Built: $exe" -ForegroundColor Green

if (Get-Process Crulanda -ErrorAction SilentlyContinue) {
    Write-Host 'The game is running, so outputs\Crulanda-Playable was left as it was. The new build is in the validation copy.' -ForegroundColor Yellow
} else {
    robocopy (Split-Path $exe) $playable /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    $exe = Join-Path $playable 'Crulanda.exe'
    Write-Host "Published: $exe" -ForegroundColor Green
}

if ($Tour) {
    Step 'Photographing every zone'
    $shots = Join-Path $logs 'tour'
    foreach ($zone in 'zone.oakhaven', 'zone.khaven') {
        $g = Start-Process $exe -PassThru -ArgumentList @('--crulanda-world-capture', ('"' + $shots + '"'), '--crulanda-zone', $zone, '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + (Join-Path $logs "tour-$zone.log") + '"'))
        $g.WaitForExit(180000) | Out-Null
    }
    Write-Host "Screenshots: $shots" -ForegroundColor Green
    Invoke-Item $shots
}

if ($Run) {
    Step 'Launching'
    $gameArgs = @()
    if ($Fresh) { $gameArgs += '--crulanda-temp-save' }
    if ($Class) { $gameArgs += @('--crulanda-class', "class.$Class") }
    if ($gameArgs.Count -gt 0) { Start-Process $exe -ArgumentList $gameArgs } else { Start-Process $exe }
}
Write-Host ''; Write-Host "Logs: $logs"
exit 0   # robocopy leaves non-zero success codes behind; failures above already threw
