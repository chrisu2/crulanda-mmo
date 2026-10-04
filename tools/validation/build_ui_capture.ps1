$v = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.EncounterBuildPlayer.Build', '-logFile', ('"' + (Join-Path $v 'hud-build.log') + '"'))
$p | Wait-Process -Timeout 900
Select-String -LiteralPath (Join-Path $v 'hud-build.log') -Pattern 'Build Finished|error CS' | Select-Object -First 6 | ForEach-Object Line
$cap = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\ui-captures'
$log = Join-Path $cap 'hud.log'
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Save   # the captures must not leave Chris's game windowed
$g = Start-Process (Join-Path $v 'Builds\Crulanda\Crulanda.exe') -PassThru -ArgumentList @('--crulanda-ui-capture', ('"' + $cap + '"'), '--crulanda-class', 'class.warrior', '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"'))
$g | Wait-Process -Timeout 180
Select-String -LiteralPath $log -Pattern 'CAPTURE_DONE|Exception|could not' | Select-Object -First 5 | ForEach-Object Line
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Restore
