param(
    [Parameter(Mandatory=$true)]
    [string]$AppDirectory,

    [Parameter(Mandatory=$true)]
    [string]$DownloadedFilePath,

    [Parameter(Mandatory=$true)]
    [string]$AssetName,

    [Parameter(Mandatory=$true)]
    [string]$ExeName
)

# Directories and files to preserve across updates (relative to AppDirectory)
# builder.json is intentionally excluded - it is part of the release and must be updated
$preservedDirs  = @("logs", "configurations", "repo", "backup")
$preservedFiles = @()

$exePath        = Join-Path $AppDirectory $ExeName
$backupExePath  = "$exePath.backup"
$tempPreserveDir = Join-Path $env:TEMP "guigeneric_preserve_$(Get-Random)"

Write-Host "Waiting for application to close..."
Start-Sleep -Seconds 3

# ── Preserve user data ────────────────────────────────────────────────────────
Write-Host "Preserving user data..."
New-Item -ItemType Directory -Path $tempPreserveDir -Force | Out-Null

foreach ($dir in $preservedDirs) {
    $src = Join-Path $AppDirectory $dir
    if (Test-Path $src) {
        Write-Host "  Preserving directory: $dir"
        Copy-Item $src (Join-Path $tempPreserveDir $dir) -Recurse -Force
    }
}

# ── Backup current executable
Write-Host "Creating backup of current executable..."
Copy-Item $exePath $backupExePath -Force

try {
    # ── Install update ────────────────────────────────────────────────────────
    if ($AssetName.EndsWith(".zip", [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "Extracting update..."
        Expand-Archive -Path $DownloadedFilePath -DestinationPath $AppDirectory -Force
    } else {
        Write-Host "Installing update..."
        Copy-Item $DownloadedFilePath $exePath -Force
    }

    # ── Restore preserved data ────────────────────────────────────────────────
    Write-Host "Restoring user data..."
    foreach ($dir in $preservedDirs) {
        $src  = Join-Path $tempPreserveDir $dir
        $dest = Join-Path $AppDirectory $dir
        if (Test-Path $src) {
            Write-Host "  Restoring directory: $dir"
            Copy-Item $src $dest -Recurse -Force
        }
    }

    # ── Cleanup
    Write-Host "Cleaning up..."
    Remove-Item $DownloadedFilePath -Force -ErrorAction SilentlyContinue
    Remove-Item $backupExePath      -Force -ErrorAction SilentlyContinue

    Write-Host "Update installed successfully! Restarting application..."
    Start-Sleep -Seconds 1
    Start-Process $exePath

} catch {
    Write-Host "Update failed: $($_.Exception.Message)"
    Write-Host "Restoring previous version..."

    if (Test-Path $backupExePath) {
        Copy-Item $backupExePath $exePath -Force -ErrorAction SilentlyContinue
    }

    # Restore preserved data even after failure
    foreach ($dir in $preservedDirs) {
        $src  = Join-Path $tempPreserveDir $dir
        $dest = Join-Path $AppDirectory $dir
        if (Test-Path $src) {
            Copy-Item $src $dest -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Process $exePath

} finally {
    Remove-Item $tempPreserveDir -Recurse -Force -ErrorAction SilentlyContinue
    # Self-delete the temp copy of this script
    Remove-Item $PSCommandPath -Force -ErrorAction SilentlyContinue
}
