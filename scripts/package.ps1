<#
.SYNOPSIS
    Packages NetSpeedWidget into Portable ZIP, Windows Installer (EXE), and MSI, generating SHA256 sums.
#>
param (
    [string]$Version = "1.0.0",
    [switch]$SelfContained = $false,
    [switch]$CreateInstaller = $true,
    [switch]$CreateMsi = $true
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$publishDir = Join-Path $rootDir "artifacts\publish"
$distDir = Join-Path $rootDir "artifacts\dist"

# Ensure dotnet in PATH
$dotnetDir = "C:\Users\Dilip\AppData\Local\Microsoft\dotnet"
if (Test-Path $dotnetDir) {
    $env:Path = "$dotnetDir;$env:Path"
}

# 1. Build
Write-Host "===> 1. Publishing release binaries..." -ForegroundColor Cyan
if ($SelfContained) {
    & "$scriptDir\build.ps1" -Configuration "Release" -SelfContained
} else {
    & "$scriptDir\build.ps1" -Configuration "Release"
}

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Force -Path $distDir | Out-Null
}

# 2. Create Portable ZIP
Write-Host "===> 2. Creating Portable ZIP..." -ForegroundColor Cyan
$zipPath = Join-Path $distDir "NetSpeedWidget-$Version-win-x64-portable.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath

# 3. Compile Installers (EXE & MSI)
if ($CreateInstaller -or $CreateMsi) {
    Write-Host "===> 3. Packaging Windows Installers..." -ForegroundColor Cyan
    & "$rootDir\installer\build-installer.ps1" -Version $Version -PublishDir "..\artifacts\publish" -DistDir "..\artifacts\dist"
}

# 4. Generate SHA256 Checksums
Write-Host "===> 4. Calculating SHA256 Checksums..." -ForegroundColor Cyan
$checksumFile = Join-Path $distDir "SHA256SUMS.txt"
$hashEntries = @()

Get-ChildItem -Path $distDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.ToLower()
    $entry = "$hash  $($_.Name)"
    $hashEntries += $entry
    Write-Host "  $entry" -ForegroundColor White
}
$hashEntries | Out-File -FilePath $checksumFile -Encoding utf8

# 5. Update Winget Manifest SHA256
$manifestFile = Join-Path $rootDir "winget\manifests\n\NetSpeedWidget\$Version\NetSpeedWidget.NetSpeedWidget.installer.yaml"
if (Test-Path $manifestFile) {
    $installerTarget = Get-ChildItem -Path $distDir -Filter "*Setup*.exe" | Select-Object -First 1
    if (-not $installerTarget) {
        $installerTarget = Get-ChildItem -Path $distDir -Filter "*.zip" | Select-Object -First 1
    }
    if ($installerTarget) {
        $hash = (Get-FileHash -Path $installerTarget.FullName -Algorithm SHA256).Hash.ToUpper()
        $content = Get-Content $manifestFile -Raw
        $content = $content -replace "INSERT_INSTALLER_SHA256_HERE", $hash
        Set-Content -Path $manifestFile -Value $content
        Write-Host "Updated Winget Installer manifest with SHA256: $hash" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "[OK] Packaging complete! All release artifacts in: $distDir" -ForegroundColor Green
