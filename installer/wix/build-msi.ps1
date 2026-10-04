<#
.SYNOPSIS
    Builds the WiX MSI independently.
#>
param (
    [string]$Version = "1.0.0",
    [string]$PublishDir = "..\..\artifacts\publish",
    [string]$DistDir = "..\..\artifacts\dist"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedPublishDir = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $PublishDir))
$resolvedDistDir = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $DistDir))
$msiOutput = Join-Path $resolvedDistDir "NetSpeedWidget-Setup-$Version.msi"
$wxsFile = Join-Path $scriptDir "Package.wxs"

if (-not (Test-Path $resolvedDistDir)) {
    New-Item -ItemType Directory -Force -Path $resolvedDistDir | Out-Null
}

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
