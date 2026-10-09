param([string[]]$Modes = @('EditMode', 'PlayMode'), [string]$PlayFilter = 'ALL')   # PlayFilter: ALL, NONE or fixtures joined by ';' (select_tests.ps1)
$v = if ($env:CRULANDA_VCOPY) { $env:CRULANDA_VCOPY } else { 'D:\crulanda-work\encounter-validation' }   # lane B sets CRULANDA_VCOPY (full_run.ps1)
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
# Register zone and quest JSON on the scene and content first (tests load the saved scene and asset).
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.ZoneSceneBuilder.BuildOakhaven', '-logFile', ('"' + (Join-Path $v 'q-register.log') + '"'))
$p | Wait-Process -Timeout 600
Select-String -LiteralPath (Join-Path $v 'q-register.log') -Pattern 'Registered|error CS' | Select-Object -First 6 | ForEach-Object Line
foreach ($m in $Modes) {
    if ($m -eq 'PlayMode' -and $PlayFilter -eq 'NONE') { 'PlayMode skipped: the change touches no fixture (select_tests.ps1)'; continue }
    $r = Join-Path $v "q-$m.xml"
    if (Test-Path -LiteralPath $r) { Remove-Item -LiteralPath $r }
    $args2 = @('-batchmode', '-nographics', '-projectPath', ('"' + $v + '"'), '-runTests', '-testPlatform', $m)
    if ($m -eq 'PlayMode' -and $PlayFilter -ne 'ALL') { $args2 += @('-testFilter', ('"' + $PlayFilter + '"')) }
    $args2 += @('-testResults', ('"' + $r + '"'), '-logFile', ('"' + (Join-Path $v "q-$m.log") + '"'))
    $p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList $args2
    $p | Wait-Process -Timeout 5400   # PlayMode loads every zone many times: ninety minutes (an hour ran out on 2026-10-07)
    if (Test-Path -LiteralPath $r) {
        $x = [xml](Get-Content -LiteralPath $r); $t = $x.'test-run'
        "$m total=$($t.total) passed=$($t.passed) failed=$($t.failed)"
        foreach ($c in $x.SelectNodes('//test-case')) { if ($c.result -eq 'Failed') { "FAIL $($c.name): $($c.failure.message.InnerText)" } }
    } else {
        "$m NO RESULTS"
        Select-String -LiteralPath (Join-Path $v "q-$m.log") -Pattern 'error CS' | Select-Object -First 15 | ForEach-Object Line
    }
}
exit 0
