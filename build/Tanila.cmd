@echo off
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0Probe-Startup.ps1" -ShadowCopy
echo Rapor bu klasordeki AkademikParafraz-Tani.json dosyasindadir.
pause
