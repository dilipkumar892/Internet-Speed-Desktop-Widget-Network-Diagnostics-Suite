<#
.SYNOPSIS
    Verifies that all generated installer artifacts exist and performs basic sanity checks.
#>
param (
    [string]$Version = "1.0.0",
    [string]$DistDir = "..\artifacts\dist"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedDistDir = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $DistDir))

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " NetSpeedWidget Installer Verification   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$artifacts = @(
    "NetSpeedWidget-$Version-win-x64-portable.zip",
    "NetSpeedWidget-Setup-$Version.exe",
    "NetSpeedWidget-Setup-$Version.msi",
    "SHA256SUMS.txt"
)

$allPassed = $true

foreach ($artifact in $artifacts) {
    $path = Join-Path $resolvedDistDir $artifact
    if (Test-Path $path) {
        Write-Host "[PASS] Found: $artifact" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Missing: $artifact" -ForegroundColor Red
        $allPassed = $false
    }
}

if (-not $allPassed) {
    Write-Error "One or more expected artifacts are missing."
}

Write-Host ""
Write-Host "Artifact verification complete." -ForegroundColor Cyan
Write-Host "Note: To perform destructive installation, upgrade, and uninstallation tests, please run them manually in a VM or Sandbox environment to protect your primary system configuration." -ForegroundColor Yellow
