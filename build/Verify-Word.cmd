@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Smoke-Word.ps1"
if errorlevel 1 (echo Word acceptance failed.) else (echo Word acceptance passed.)
pause
