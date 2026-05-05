#!/usr/bin/env pwsh
# Downloads pinned Z2S OTA firmware files listed in manifest.json from lsroka76/Z2S_Library.
# Run this script when you want to refresh or initially populate the firmware fixtures.
# Usage:  .\Download-Firmware.ps1          # skip files that already exist at the correct size
#         .\Download-Firmware.ps1 -Force   # re-download even if file is present

param([switch]$Force)

$ErrorActionPreference = "Stop"

$dir      = $PSScriptRoot
$manifest = Get-Content (Join-Path $dir "manifest.json") | ConvertFrom-Json

foreach ($fw in $manifest.firmwareFiles) {
    $dest = Join-Path $dir $fw.localFile
    if (-not $Force -and (Test-Path $dest) -and (Get-Item $dest).Length -eq $fw.expectedSize) {
        Write-Host "[$($fw.version)] $($fw.localFile) already present ($($fw.expectedSize) bytes) -- skipping."
        continue
    }
    Write-Host "[$($fw.version)] Downloading $($fw.assetName) ..."
    Invoke-WebRequest -Uri $fw.downloadUrl -OutFile $dest -UseBasicParsing
    $actual = (Get-Item $dest).Length
    if ($actual -ne $fw.expectedSize) {
        Write-Error "[$($fw.version)] Size mismatch: expected $($fw.expectedSize), got $actual"
    } else {
        Write-Host "[$($fw.version)] OK -- $actual bytes"
    }
}
Write-Host "All firmware files ready."