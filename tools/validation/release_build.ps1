param([switch]$NoPublish, [switch]$Sync)
# The release build to publish after a green full run (full_run.ps1's lane B builds a Development player for the tours and captures:
# it carries the "Development Build" watermark). This builds the release player in lane B (--crulanda-release, read by
# EncounterBuildPlayer.Build), then mirrors it into D:\crulanda-work\outputs\Crulanda-Playable unless the game is running or -NoPublish.
# Run it in the foreground (about eight minutes) or through start_detached-style WMI if the shell may be cut off.
$v = 'D:\crulanda-work\encounter-validation-b'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$log = Join-Path $v ('release-build-' + (Get-Date -Format 'yyyyMMdd-HHmm') + '.log')
# -Sync: mirror the working tree's Assets into lane B first (the full run does it at its start; a fix after the run needs it again).
if ($Sync) { robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null; if ($LASTEXITCODE -ge 8) { "sync failed (robocopy $LASTEXITCODE)"; exit 1 } }
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.EncounterBuildPlayer.Build', '--crulanda-release', '-logFile', ('"' + $log + '"'))
$p | Wait-Process -Timeout 1800
if (-not (Select-String -LiteralPath $log -Pattern 'Build Finished, Result: Success' -Quiet)) {
    Select-String -LiteralPath $log -Pattern 'error CS' | Select-Object -First 10 | ForEach-Object { $_.Line }
    "BUILD FAILED; see $log"; exit 1
}
"built (release); shader errors: $((Select-String -LiteralPath $log -Pattern 'Shader error' | Measure-Object).Count)"
if ($NoPublish) { exit 0 }
if (Get-Process Crulanda -ErrorAction SilentlyContinue) { 'The game is running: not published. Close it and run again with the build in place.'; exit 2 }
robocopy (Join-Path $v 'Builds\Crulanda') 'D:\crulanda-work\outputs\Crulanda-Playable' /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { "publish failed (robocopy $LASTEXITCODE)"; exit 1 }
'published D:\crulanda-work\outputs\Crulanda-Playable\Crulanda.exe'
exit 0
