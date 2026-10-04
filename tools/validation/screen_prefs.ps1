param([switch]$Save, [switch]$Restore)
# The capture runs launch the same game (company Crulanda, product "Crulanda - The Quiet Trail") windowed at 1440x900, and
# Unity remembers the last screen mode per game in the registry, so Chris's playable build then opened windowed (2026-10-04:
# "can't get it to full screen"). The capture scripts call this with -Save before they launch the game and -Restore after, so
# his own settings come back as they were (or are cleared, if he had none, and the game opens at its default: full screen).
$key = 'HKCU:\Software\Crulanda\Crulanda - The Quiet Trail'
$keep = Join-Path $env:TEMP 'crulanda-screen-prefs.json'
if ($Save) {
    $vals = @{}
    if (Test-Path $key) { $p = Get-ItemProperty $key; foreach ($n in $p.PSObject.Properties.Name) { if ($n -like 'Screenmanager*') { $vals[$n] = $p.$n } } }
    $vals | ConvertTo-Json | Set-Content -Encoding utf8 $keep
}
if ($Restore -and (Test-Path $keep)) {
    $vals = Get-Content $keep -Raw | ConvertFrom-Json
    if (Test-Path $key) {
        $p = Get-ItemProperty $key
        foreach ($n in $p.PSObject.Properties.Name) { if ($n -like 'Screenmanager*') { Remove-ItemProperty -Path $key -Name $n -ErrorAction SilentlyContinue } }
        if ($vals) { foreach ($n in $vals.PSObject.Properties.Name) { $v = $vals.$n; if ($v -is [array]) { New-ItemProperty -Path $key -Name $n -PropertyType Binary -Value ([byte[]]$v) -Force | Out-Null } else { New-ItemProperty -Path $key -Name $n -PropertyType DWord -Value ([int]$v) -Force | Out-Null } } }
    }
    Remove-Item $keep -ErrorAction SilentlyContinue
}
