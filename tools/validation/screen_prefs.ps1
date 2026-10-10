param([switch]$Save, [switch]$Restore)
# The capture runs launch the same game (company Crulanda, product "Crulanda - The Quiet Trail") windowed at 1440x900, and
# Unity remembers the last screen mode per game in the registry, so Chris's playable build then opened windowed (2026-10-04:
# "can't get it to full screen"). The capture scripts call this with -Save before they launch the game and -Restore after, so
# his own settings come back as they were (or are cleared, if he had none, and the game opens at its default: full screen).
# Read and written through .NET's registry API (2026-10-10): Get-ItemProperty threw "Unable to cast Int64 to Int32" on this key,
# so -Save kept nothing and -Restore cleared nothing, and the game was left windowed after every capture run.
$sub = 'Software\Crulanda\Crulanda - The Quiet Trail'
$keep = Join-Path $env:TEMP 'crulanda-screen-prefs.json'
if ($Save) {
    $vals = @{}
    $k = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($sub, $false)
    if ($k) {
        foreach ($n in $k.GetValueNames()) {
            if ($n -like 'Screenmanager*') {
                $v = $k.GetValue($n, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                $vals[$n] = @{ kind = $k.GetValueKind($n).ToString(); value = $(if ($v -is [byte[]]) { [int[]]$v } else { $v }) }
            }
        }
        $k.Close()
    }
    $vals | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 $keep
}
if ($Restore -and (Test-Path $keep)) {
    $vals = Get-Content $keep -Raw | ConvertFrom-Json
    $k = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($sub, $true)
    if ($k) {
        foreach ($n in $k.GetValueNames()) { if ($n -like 'Screenmanager*') { $k.DeleteValue($n, $false) } }
        if ($vals) {
            foreach ($n in $vals.PSObject.Properties.Name) {
                $e = $vals.$n; if ($null -eq $e -or $null -eq $e.kind) { continue }
                $kind = [Microsoft.Win32.RegistryValueKind]$e.kind
                $v = switch ($e.kind) { 'Binary' { [byte[]]$e.value } 'QWord' { [long]$e.value } 'DWord' { [int]$e.value } default { [string]$e.value } }
                $k.SetValue($n, $v, $kind)
            }
        }
        $k.Close()
    }
    Remove-Item $keep -ErrorAction SilentlyContinue
}
