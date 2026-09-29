@echo off
rem Double-click: build a test player and play it with a throwaway character.
rem From a terminal you can pass options instead, e.g.  Build-TestPlayer.cmd -Run -Class druid
if "%~1"=="" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-TestPlayer.ps1" -Run -Fresh
  pause
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-TestPlayer.ps1" %*
)
