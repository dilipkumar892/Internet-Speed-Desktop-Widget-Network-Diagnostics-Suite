<#
.SYNOPSIS
    Builds the NetSpeedWidget installers (EXE and MSI) using Inno Setup and WiX v4.
#>
param (
    [string]$Version = "1.0.0",
    [string]$PublishDir = "..\artifacts\publish",
    [string]$DistDir = "..\artifacts\dist"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

$resolvedPublishDir = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $PublishDir))
$resolvedDistDir = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $DistDir))

if (-not (Test-Path $resolvedDistDir)) {
    New-Item -ItemType Directory -Force -Path $resolvedDistDir | Out-Null
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " NetSpeedWidget Installer Build Script   " -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Check for Inno Setup
$innoScript = Join-Path $scriptDir "installer.iss"
$cmd = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
$iscc = if ($null -ne $cmd) { $cmd.Source } else { $null }

if (-not $iscc) {
    $possiblePaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
        "${env:ProgramFiles}\Inno Setup 6\iscc.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\iscc.exe"
    )
    foreach ($p in $possiblePaths) {
        if (Test-Path $p) { $iscc = $p; break; }
    }
}

# 2. Check for WiX v4
$wixFound = $false
try {
    $wixVersion = wix --version 2>&1
    if ($wixVersion -match "^4\.") {
        $wixFound = $true
    }
} catch {
    $wixFound = $false
}

if (-not $wixFound) {
    Write-Host "WiX v4 not found globally. Attempting to check local dotnet tools..." -ForegroundColor Yellow
    $wixToolPath = Join-Path $env:USERPROFILE ".dotnet\tools\wix.exe"
    if (Test-Path $wixToolPath) {
        $env:Path = "$($env:USERPROFILE)\.dotnet\tools;$env:Path"
        $wixFound = $true
    } else {
        try {
            $wixVersion = dotnet tool run wix --version 2>&1
            if ($wixVersion -match "^(4|5)\.") {
                $wixFound = $true
            }
        } catch { }
    }
}

$exeOutput = Join-Path $resolvedDistDir "NetSpeedWidget-Setup-$Version.exe"
$msiOutput = Join-Path $resolvedDistDir "NetSpeedWidget-Setup-$Version.msi"

# 3. Build Inno Setup EXE
if ($iscc) {
    Write-Host "`n---> Building Inno Setup EXE..." -ForegroundColor Cyan
    & "$iscc" "/DMyAppVersion=$Version" "/O$resolvedDistDir" "/FNetSpeedWidget-Setup-$Version" "$innoScript"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "     [OK] EXE Installer generated." -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup compilation failed!"
    }
} else {
    Write-Warning "Inno Setup compiler (iscc.exe) not found. Skipping EXE installer."
}

# 4. Build WiX MSI
if ($wixFound) {
    Write-Host "`n---> Building WiX MSI..." -ForegroundColor Cyan
    $wxsFile = Join-Path $scriptDir "wix\Package.wxs"
    
    # We use wix build
    $wixArgs = @(
        "build",
        "-out", $msiOutput,
        "-d", "Version=$Version",
        "-d", "PublishDir=$resolvedPublishDir",
        $wxsFile
    )
    
    $wixCmd = Get-Command "wix" -ErrorAction SilentlyContinue
    if ($wixCmd) {
        & wix @wixArgs
    } else {
        & dotnet tool run wix @wixArgs
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "     [OK] MSI Installer generated." -ForegroundColor Green
    } else {
        Write-Warning "WiX compilation failed!"
    }
} else {
    Write-Warning "WiX Toolset v4 not found. Skipping MSI installer. (Run 'dotnet tool install --global wix' to install)"
}

Write-Host "`nInstaller generation complete." -ForegroundColor Cyan
