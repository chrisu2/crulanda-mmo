param([string]$Since = '')
# Which PlayMode fixtures a change needs (Phase 5.0, 2026-10-05; Chris: "the most efficient playmode, whatever that takes").
# Prints one line: ALL, NONE, or the fixtures' full names joined by ';' (a -testFilter for run_tests.ps1).
# The change is everything that differs from $Since (default: the commit of the last green full run, kept in
# hel\work\last-green.txt; with none on record, ALL), committed or not, plus untracked files.
#   - a script under Scripts/: every fixture whose test file names one of the script's classes (its file name before the
#     first '.', so ActorVisual.Gear.cs counts as ActorVisual); a core class (EncounterSession, ZoneBuilder, ActorVisual,
#     EncounterProgress, ...) is named by nearly every fixture, so it runs nearly all of them;
#   - a PlayMode test file: that fixture;
#   - zone data or quest JSON: the fixtures that check the zones' layout and content (World set below);
#   - item, loot, profession or look JSON: the fixtures that use items (by name in the test files);
#   - an asmdef, the scene, ProjectSettings or Packages: ALL;
#   - art, shaders, prefabs, models, textures, docs and tools only: NONE (the build and the tours cover them).
$repo = 'D:\code\mmo'; $git = 'C:\Program Files\Git\cmd\git.exe'
$tests = Join-Path $repo 'New Unity Project\Assets\Crulanda\Tests\PlayMode'
if (-not $Since) { $f = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\last-green.txt'; if (Test-Path $f) { $Since = (Get-Content $f -TotalCount 1).Trim() } }
if (-not $Since) { 'ALL'; exit 0 }
$changed = @(& $git -C $repo diff --name-only $Since) + @(& $git -C $repo ls-files --others --exclude-standard)
$changed = $changed | Where-Object { $_ } | Sort-Object -Unique
$files = Get-ChildItem $tests -Filter '*.cs'
$fixtures = New-Object System.Collections.Generic.HashSet[string]
$World = 'ZoneContentTests', 'NodePlacementTests', 'SecretPlacementTests', 'NodeStreamTests', 'CaveTests', 'WaterTests', 'BuildingGroundTests', 'TreeLimbTests', 'ZoneExitTests', 'DiscoveryTests', 'SocialPullTests', 'HollowQuestTests'
function AddNaming([string]$word) { foreach ($t in $files) { if (Select-String -LiteralPath $t.FullName -Pattern ('\b' + [regex]::Escape($word) + '\b') -Quiet) { [void]$fixtures.Add($t.BaseName) } } }
foreach ($c in $changed) {
    $p = $c -replace '\\', '/'
    if ($p -match '\.asmdef$|/Scenes/|ProjectSettings/|Packages/') { 'ALL'; exit 0 }
    if ($p -match '/Tests/PlayMode/(\w+)\.cs$') { [void]$fixtures.Add($Matches[1]); continue }
    if ($p -match '/Tests/EditMode/') { continue }
    if ($p -match '/(Scripts|Editor)/.*?/?(\w+)(\.[\w.]+)?\.cs$' -and $p -match '/Scripts/') { AddNaming $Matches[2]; continue }
    if ($p -match '/EncounterContent/(Zones|Quests)/') { foreach ($w in $World) { [void]$fixtures.Add($w) }; AddNaming 'Quests'; continue }
    if ($p -match '/EncounterContent/(Items|Professions)/|/Resources/Gear/') { AddNaming 'Items'; AddNaming 'Inventory'; continue }
}
if ($fixtures.Count -eq 0) { 'NONE'; exit 0 }
if ($fixtures.Count -ge $files.Count - 2) { 'ALL'; exit 0 }
($fixtures | Sort-Object | ForEach-Object { 'Crulanda.Tests.' + $_ }) -join ';'
