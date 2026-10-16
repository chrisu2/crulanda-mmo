# Keeps the HDD (disk 2: D: and E:) spinning: a tiny write + flush every 15 min (Chris 20:47). No power settings touched.
while ($true) {
  foreach ($v in 'D:\crulanda-work','E:\') {
    try { $f = Join-Path $v '.crulanda-keepdisk'; $s = [IO.File]::Open($f,'Create','Write'); $b=[Text.Encoding]::ASCII.GetBytes((Get-Date).ToString('o')); $s.Write($b,0,$b.Length); $s.Flush($true); $s.Close() } catch {}
  }
  Start-Sleep 900
}

