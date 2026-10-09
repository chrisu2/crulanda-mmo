param([Parameter(Mandatory = $true)][string]$Method, [string]$Tag = 'method', [switch]$Graphics)
# Mirrors the repo's Assets into the validation copy and runs one editor method there in batch mode (e.g. an import report).
# -Graphics keeps the graphics device (for a method that renders, e.g. FigureCapture). Prints the log's lines that mention the method's done-marker or errors. Never touches the user's open project.
$v = if ($env:CRULANDA_VCOPY) { $env:CRULANDA_VCOPY } else { 'D:\crulanda-work\encounter-validation' }   # lane B sets CRULANDA_VCOPY (full_run.ps1)
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
$log = Join-Path $v "$Tag.log"
$gfx = if ($Graphics) { @() } else { @('-nographics') }
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList (@('-batchmode') + $gfx + @('-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', $Method, '-logFile', ('"' + $log + '"')))
$p | Wait-Process -Timeout 1500
"exit $($p.ExitCode)"
if (Test-Path -LiteralPath $log) { Select-String -LiteralPath $log -Pattern '_DONE|error CS|Exception|Error:' | Select-Object -First 25 | ForEach-Object Line }
exit 0
