$v = if ($env:CRULANDA_VCOPY) { $env:CRULANDA_VCOPY } else { 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation' }   # lane B sets CRULANDA_VCOPY (full_run.ps1)
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
foreach ($m in 'EditMode', 'PlayMode') {
    $r = Join-Path $v "world-$m.xml"
    if (Test-Path -LiteralPath $r) { Remove-Item -LiteralPath $r }
    $p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $v + '"'), '-runTests', '-testPlatform', $m, '-testResults', ('"' + $r + '"'), '-logFile', ('"' + (Join-Path $v "world-$m.log") + '"'))
    $p | Wait-Process -Timeout 900
    if (Test-Path -LiteralPath $r) {
        $x = [xml](Get-Content -LiteralPath $r); $t = $x.'test-run'
        "$m total=$($t.total) passed=$($t.passed) failed=$($t.failed)"
        foreach ($case in $x.SelectNodes('//test-case')) { if ($case.result -eq 'Failed') { "FAIL $($case.name): $($case.failure.message.InnerText)" } }
    } else { "$m NO RESULTS" }
}
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.EncounterBuildPlayer.Build', '-logFile', ('"' + (Join-Path $v 'world-build.log') + '"'))
$p | Wait-Process -Timeout 580
Select-String -LiteralPath (Join-Path $v 'world-build.log') -Pattern 'Build Finished' | Select-Object -Last 1 | ForEach-Object Line
