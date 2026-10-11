param([string]$Exe = 'D:\crulanda-work\encounter-validation-b\Builds\Crulanda\Crulanda.exe', [switch]$BowOnly)
# The motion capture alone (MotionCapture: the jump standing and running, the bow), against a built player, its frames and motion.txt
# in D:\crulanda-work\ui-captures\motion. Saves and restores Chris's screen settings round the windowed run (screen_prefs.ps1).
$md = 'D:\crulanda-work\ui-captures\motion'
New-Item -ItemType Directory -Force $md | Out-Null
Remove-Item -Path (Join-Path $md '*.png') -ErrorAction SilentlyContinue
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Save
$log = Join-Path $md 'motion.log'
$extra = if ($BowOnly) { @('--crulanda-motion-bow-only') } else { @() }
$m = Start-Process $Exe -PassThru -ArgumentList (@('--crulanda-motion-capture', ('"' + $md + '"'), '--crulanda-class', 'class.ranger', '-screen-width', '1440', '-screen-height', '900', '-screen-fullscreen', '0', '-logFile', ('"' + $log + '"')) + $extra)
$m | Wait-Process -Timeout 240
& (Join-Path $PSScriptRoot 'screen_prefs.ps1') -Restore
Select-String -LiteralPath $log -Pattern 'MOTION_CAPTURE_DONE|Exception' | Select-Object -First 5 | ForEach-Object Line
