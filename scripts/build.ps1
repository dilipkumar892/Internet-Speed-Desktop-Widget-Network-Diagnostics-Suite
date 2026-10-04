<#
.SYNOPSIS
    Builds the NetSpeedWidget project in Release mode.
#>
param (
    [string]$Configuration = "Release",
    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "src\NetSpeedWidget\NetSpeedWidget.csproj"
$outputDir = Join-Path $rootDir "artifacts\publish"

# Ensure dotnet in PATH
$dotnetDir = "C:\Users\Dilip\AppData\Local\Microsoft\dotnet"
if (Test-Path $dotnetDir) {
    $env:Path = "$dotnetDir;$env:Path"
}

Write-Host "===> Building NetSpeedWidget ($Configuration)..." -ForegroundColor Cyan

if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir | Out-Null
}

$publishArgs = @(
    "publish",
    $projectPath,
    "-c", $Configuration,
    "-r", "win-x64",
    "-o", $outputDir,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true"
)

if ($SelfContained) {
    $publishArgs += "--self-contained", "true", "-p:EnableCompressionInSingleFile=true"
} else {
    $publishArgs += "--self-contained", "false"
}

& dotnet @publishArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "[OK] Build succeeded! Output binaries located at: $outputDir" -ForegroundColor Green
} else {
    Write-Error "Build failed with exit code $LASTEXITCODE"
}
