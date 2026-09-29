param([string]$File, [string[]]$Keys, [switch]$DryRun)
# Applies the diagnose/critique workflow's edits (corrected edits when the critic said 'fixable').
# Each edit must match exactly once; otherwise it is skipped and reported. Line endings: the file's own (LF or CRLF).
$all = Get-Content $File -Raw | ConvertFrom-Json
$enc = New-Object System.Text.UTF8Encoding $false
function Unescape([string]$s) { return $s.Replace('&lt;', '<').Replace('&gt;', '>').Replace('&amp;', '&') }
function CountOf([string]$hay, [string]$needle) { $n = 0; $i = 0; while (($i = $hay.IndexOf($needle, $i, [StringComparison]::Ordinal)) -ge 0) { $n++; $i += [Math]::Max(1, $needle.Length) }; return $n }
foreach ($c in $all | Where-Object { $Keys -contains $_.key }) {
    $edits = if ($c.review.verdict -eq 'fixable' -and $c.review.correctedEdits.Count -gt 0) { $c.review.correctedEdits } else { $c.diagnosis.edits }
    Write-Output "=== $($c.key) ($($edits.Count) edits)"
    $k = 0
    foreach ($e in $edits) {
        $k++
        $path = $e.file
        if (-not (Test-Path -LiteralPath $path)) { Write-Output "  [$k] MISSING FILE $path"; continue }
        $bytes = [IO.File]::ReadAllBytes($path)
        $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $text = [IO.File]::ReadAllText($path)
        $crlf = $text.Contains("`r`n")
        $old = ([string]$e.oldString).Replace("`r`n", "`n"); $new = ([string]$e.newString).Replace("`r`n", "`n")
        $work = $text.Replace("`r`n", "`n")
        $n = CountOf $work $old
        if ($n -ne 1) { $o2 = Unescape $old; $n2 = CountOf $work $o2; if ($n2 -eq 1) { $old = $o2; $new = Unescape $new; $n = 1 } }
        if ($n -ne 1) { Write-Output ("  [$k] SKIP ({0} matches) {1}: {2}" -f $n, (Split-Path $path -Leaf), ($old.Substring(0, [Math]::Min(90, $old.Length)) -replace "`n", ' / ')); continue }
        $at = $work.IndexOf($old, [StringComparison]::Ordinal)
        $work = $work.Substring(0, $at) + $new + $work.Substring($at + $old.Length)
        if ($crlf) { $work = $work.Replace("`n", "`r`n") }
        if (-not $DryRun) { if ($bom) { [IO.File]::WriteAllText($path, $work, (New-Object System.Text.UTF8Encoding $true)) } else { [IO.File]::WriteAllText($path, $work, $enc) } }
        Write-Output ("  [$k] ok {0}" -f (Split-Path $path -Leaf))
    }
}
