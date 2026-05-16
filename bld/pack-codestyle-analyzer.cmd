@echo off
:: Windows .cmd shim that delegates to the PowerShell script. PowerShell 5.1 (Windows PowerShell)
:: and 7+ (pwsh) are both supported.
setlocal
set "ScriptDir=%~dp0"
where pwsh >NUL 2>&1
if %ERRORLEVEL%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%ScriptDir%pack-codestyle-analyzer.ps1" %*
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ScriptDir%pack-codestyle-analyzer.ps1" %*
)
endlocal & exit /b %ERRORLEVEL%
