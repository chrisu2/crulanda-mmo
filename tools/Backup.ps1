# Back up D:\code\mmo (project, docs, tools and the .git history) to E:\claude\unity projects\mmo.
# Additive by default: copies new and changed files and never deletes anything from the backup.
#   .\Backup.ps1            skip Unity's regenerable caches (Library, Temp, Logs); fast
#   .\Backup.ps1 -Full      include Library and Logs too
#   .\Backup.ps1 -Mirror    make the backup an exact copy (deletes backup files that no longer exist here)
param([switch]$Full, [switch]$Mirror, [string]$Destination = "E:\claude\unity projects\mmo")

$source = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Split-Path -Qualifier $Destination))) { Write-Error "Backup drive $(Split-Path -Qualifier $Destination) is not available; nothing was copied."; exit 2 }
New-Item -ItemType Directory -Force $Destination | Out-Null

$project = Join-Path $source "New Unity Project"
$skip = @((Join-Path $project "Temp"))
if (-not $Full) { $skip += (Join-Path $project "Library"), (Join-Path $project "Logs") }

$mode = if ($Mirror) { "/MIR" } else { "/E" }
robocopy $source $Destination $mode /COPY:DAT /DCOPY:DAT /R:1 /W:1 /XJ /XD $skip /NFL /NDL /NJH /NP
$code = $LASTEXITCODE
# Robocopy: 0-7 is success (1 = files copied), 8 and up is a failure.
if ($code -ge 8) { Write-Error "Backup failed (robocopy exit $code)."; exit $code }
Write-Host "Backup OK -> $Destination (robocopy exit $code)"
exit 0
