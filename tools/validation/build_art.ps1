# Generates new art assets (textures, materials: ZoneSceneBuilder.EnsureArt) in the validation copy and brings them back into the
# repo, so the next mirror does not delete them. Run after adding or changing generated art. Runs the art build twice: a new
# shader is only found by Shader.Find once Unity has imported it.
$v = if ($env:CRULANDA_VCOPY) { $env:CRULANDA_VCOPY } else { 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation' }   # lane B sets CRULANDA_VCOPY (full_run.ps1)
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
$repo = 'D:\code\mmo\New Unity Project\Assets'
robocopy $repo (Join-Path $v 'Assets') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
foreach ($pass in 1, 2) {
    $log = Join-Path $v "art-$pass.log"
    $p = Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $v + '"'), '-executeMethod', 'Crulanda.EditorTools.ZoneSceneBuilder.BuildOakhaven', '-logFile', ('"' + $log + '"'))
    $p | Wait-Process -Timeout 900
    "pass ${pass}:"
    Select-String -LiteralPath $log -Pattern 'error CS|Shader error|Exception|Painted rock|not imported|Registered \d' | Select-Object -First 12 | ForEach-Object { '  ' + $_.Line }
}
# Bring the generated art home (new and changed files only; nothing is deleted from the repo).
# (Never pipe robocopy into Select-Object -First: that ends the pipeline and kills the copy part-way, as it did on 2026-10-01.)
$copied = robocopy (Join-Path $v 'Assets\Crulanda\World\Art') (Join-Path $repo 'Crulanda\World\Art') /E /NDL /NJH /NJS /NP
"copied or updated: $(($copied | Where-Object { $_ -match 'New File|Newer|Older' } | Measure-Object).Count) files (robocopy exit $LASTEXITCODE)"
$copied | Where-Object { $_ -match 'New File' } | ForEach-Object { '  ' + ($_ -replace '^.*\\', '') } | Select-Object -First 30
exit 0
