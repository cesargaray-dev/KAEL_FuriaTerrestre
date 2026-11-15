<#
.SYNOPSIS
    Root shortcut runner for Vorst Codex E2E Test Suite.
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild = $false,
    [string]$LogPath = "TestResults_CodexE2E.log"
)

$targetScript = Join-Path $PSScriptRoot "Tests\Run-CodexE2ETests.ps1"
& $targetScript -SkipBuild:$SkipBuild -LogPath $LogPath
exit $LASTEXITCODE
