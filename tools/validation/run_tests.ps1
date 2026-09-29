param([string[]]$Modes = @('EditMode', 'PlayMode'))
$v = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
robocopy 'D:\code\mmo\New Unity Project\Assets' (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
# Register zone and quest JSON on the scene and content first (tests load the saved scene and asset).
$p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.ZoneSceneBuilder.BuildOakhaven', '-logFile', ('"' + (Join-Path $v 'q-register.log') + '"'))
$p | Wait-Process -Timeout 600
Select-String -LiteralPath (Join-Path $v 'q-register.log') -Pattern 'Registered|error CS' | Select-Object -First 6 | ForEach-Object Line
foreach ($m in $Modes) {
    $r = Join-Path $v "q-$m.xml"
    if (Test-Path -LiteralPath $r) { Remove-Item -LiteralPath $r }
    $p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-projectPath', ('"' + $v + '"'), '-runTests', '-testPlatform', $m, '-testResults', ('"' + $r + '"'), '-logFile', ('"' + (Join-Path $v "q-$m.log") + '"'))
    $p | Wait-Process -Timeout 1200
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
