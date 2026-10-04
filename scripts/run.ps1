<#
.SYNOPSIS
    Launches NetSpeedWidget locally.
#>
param (
    [switch]$Build = $false
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "src\NetSpeedWidget\NetSpeedWidget.csproj"

$dotnetDir = "C:\Users\Dilip\AppData\Local\Microsoft\dotnet"
if (Test-Path $dotnetDir) {
    $env:Path = "$dotnetDir;$env:Path"
}

Write-Host "Starting NetSpeedWidget..." -ForegroundColor Cyan
& dotnet run --project "$projectPath"
