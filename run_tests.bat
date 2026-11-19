@echo off
REM ============================================================================
REM KAEL: Furia Terrestre - Vorst Codex Automated E2E Test Suite Runner
REM Unattended CLI execution returning exit code 0 on PASS, 1 on FAIL.
REM ============================================================================

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tests\Run-CodexE2ETests.ps1" %*
exit /b %ERRORLEVEL%
