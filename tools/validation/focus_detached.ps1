param([string]$Runs, [string]$Log = 'D:\crulanda-work\focus.log', [string]$Lane = 'A')
# Runs run_focus.ps1 once per "Platform|Filter|Tag" in $Runs (separated by '#') outside this shell's process tree (through WMI, as
# start_detached.ps1 does), writing everything to $Log and "DONE" at the end. Watch $Log.
# -Lane B: in the second copy (encounter-validation-b), while lane A is busy with a full run's tests.
$inner = "Remove-Item -LiteralPath '$Log' -ErrorAction SilentlyContinue; "
if ($Lane -eq 'B') { $inner += "`$env:CRULANDA_VCOPY = 'D:\crulanda-work\encounter-validation-b'; " }
foreach ($run in $Runs.Split('#')) {
    $p = $run.Split('|')
    $inner += "& 'D:\code\mmo\tools\validation\run_focus.ps1' -Platform '$($p[0])' -Filter '$($p[1])' -Tag '$($p[2])' *>&1 | Out-File -LiteralPath '$Log' -Append -Encoding utf8; "
}
$inner += "Add-Content -LiteralPath '$Log' 'DONE' -Encoding utf8"
$cmd = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command "' + $inner.Replace('"', '\"') + '"'
$si = New-CimInstance -ClassName Win32_ProcessStartup -ClientOnly -Property @{ ShowWindow = [uint16]0 }
$r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $cmd; CurrentDirectory = 'D:\code\mmo'; ProcessStartupInformation = $si }
"started pid $($r.ProcessId) (return $($r.ReturnValue)), log $Log"
