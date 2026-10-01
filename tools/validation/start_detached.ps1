param([string]$Arguments = '')
# Starts full_run.ps1 outside this shell's process tree (through WMI), so it survives the calling tool being stopped.
# Watch C:\Users\chris\Documents\Codex\2026-09-28\hel\work\full-run.log and wait for full-run.done.
$cmd = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "D:\code\mmo\tools\validation\full_run.ps1" ' + $Arguments
$r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $cmd; CurrentDirectory = 'D:\code\mmo' }
"started pid $($r.ProcessId) (return $($r.ReturnValue))"
