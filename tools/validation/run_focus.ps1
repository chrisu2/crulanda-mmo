param([string]$Filter = 'Crulanda.Tests.VillageErrandTests', [string]$Platform = 'PlayMode', [string]$Tag = 'focus')
# Runs only the tests matching $Filter (NUnit full names, ';' separated) in the validation copy: a fast check before the full run.
$v = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
# Register zone, quest and item JSON on the scene and content first, as run_tests.ps1 does: the mirror just put the repo's
# scene back, and without this the session has no item database (two false failures on 2026-10-01).
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.ZoneSceneBuilder.BuildOakhaven', '-logFile', ('"' + (Join-Path $v 'q-register.log') + '"'))
$p | Wait-Process -Timeout 600
$r = Join-Path $v "$Tag-results.xml"
if (Test-Path -LiteralPath $r) { Remove-Item -LiteralPath $r }
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $v + '"'), '-runTests', '-testPlatform', $Platform, '-testFilter', ('"' + $Filter + '"'), '-testResults', ('"' + $r + '"'), '-logFile', ('"' + (Join-Path $v "$Tag.log") + '"'))
$p | Wait-Process -Timeout 1800
if (Test-Path -LiteralPath $r) {
    $x = [xml](Get-Content -LiteralPath $r); $t = $x.'test-run'
    "$Platform total=$($t.total) passed=$($t.passed) failed=$($t.failed)"
    foreach ($c in $x.SelectNodes('//test-case')) { if ($c.result -eq 'Failed') { "FAIL $($c.name): $($c.failure.message.InnerText)" } }
} else {
    "$Platform NO RESULTS"
    Select-String -LiteralPath (Join-Path $v "$Tag.log") -Pattern 'error CS' | Select-Object -First 15 | ForEach-Object Line
}
exit 0
