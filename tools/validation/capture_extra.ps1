# After build_and_tour.ps1 has built the player: the HUD captures (the Trades window and the rest) and the wardrobe line-up
# (every weapon and shield, loot step A1). Runs the built player windowed, never against the owner's save (the captures use a
# throwaway character directory).
$v = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation'
$exe = Join-Path $v 'Builds\Crulanda\Crulanda.exe'
if (-not (Test-Path $exe)) { 'NO PLAYER BUILD'; exit 1 }
$ui = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\ui-captures'
$log = Join-Path $ui 'hud.log'
$g = Start-Process $exe -PassThru -ArgumentList @('--crulanda-ui-capture', ('"' + $ui + '"'), '--crulanda-class', 'class.warrior', '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"'))
$g | Wait-Process -Timeout 240
Select-String -LiteralPath $log -Pattern 'CAPTURE_DONE|Exception|could not' | Select-Object -First 5 | ForEach-Object Line
$wd = Join-Path $ui 'wardrobe'
New-Item -ItemType Directory -Force $wd | Out-Null
$wlog = Join-Path $wd 'wardrobe.log'
$w = Start-Process $exe -PassThru -ArgumentList @('--crulanda-wardrobe-capture', ('"' + $wd + '"'), '--crulanda-zone', 'zone.oakhaven', '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $wlog + '"'))
$w | Wait-Process -Timeout 420
Select-String -LiteralPath $wlog -Pattern 'WARDROBE_CAPTURE_DONE|Exception|spot' | Select-Object -First 6 | ForEach-Object Line
Get-ChildItem $wd -Filter '*.png' | Select-Object -ExpandProperty Name
