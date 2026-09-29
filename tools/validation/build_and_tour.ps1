param([string[]]$Zones = @('zone.khaven', 'zone.oakhaven'))
$v = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.EncounterBuildPlayer.Build', '-logFile', ('"' + (Join-Path $v 'tour-build.log') + '"'))
$p | Wait-Process -Timeout 580
Select-String -LiteralPath (Join-Path $v 'tour-build.log') -Pattern 'Build Finished|error CS' | Select-Object -Last 3 | ForEach-Object Line
# A failed build leaves the previous player in place: never photograph that as if it were this change.
if (-not (Select-String -LiteralPath (Join-Path $v 'tour-build.log') -Pattern 'Build Finished, Result: Success' -Quiet)) { 'BUILD FAILED - tour skipped'; exit 1 }
$cap = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\world-captures'
New-Item -ItemType Directory -Force $cap | Out-Null
foreach ($z in $Zones) {
    $log = Join-Path $cap ("tour-" + $z + ".log")
    $g = Start-Process (Join-Path $v 'Builds\Crulanda\Crulanda.exe') -PassThru -ArgumentList @('--crulanda-world-capture', ('"' + $cap + '"'), '--crulanda-zone', $z, '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"'))
    $g | Wait-Process -Timeout 420
    Select-String -LiteralPath $log -Pattern 'WORLD_CAPTURE_DONE|Exception' | Select-Object -First 3 | ForEach-Object Line
}
Get-ChildItem $cap -Filter '*.png' | Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-10) } | Select-Object -ExpandProperty Name
