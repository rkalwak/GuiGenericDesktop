# PowerShell script to generate strong-name key for GUI Generic Builder

Write-Host "Generating Strong-Name Key for GUI Generic Builder..." -ForegroundColor Cyan

$projectDir = $PSScriptRoot
$snkFile = Join-Path $projectDir "GuiGenericBuilder.snk"

# Check if sn.exe exists
$snPath = "sn.exe"
try {
    $null = & $snPath -? 2>&1
} catch {
    Write-Host "sn.exe not found in PATH. Searching for Visual Studio installation..." -ForegroundColor Yellow
    
    # Try to find sn.exe in common VS locations
    $possiblePaths = @(
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\sn.exe",
        "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\sn.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\sn.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\Common7\Tools\sn.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\Tools\sn.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $snPath = $path
            Write-Host "Found sn.exe at: $snPath" -ForegroundColor Green
            break
        }
    }
}

if (Test-Path $snkFile) {
    $response = Read-Host "GuiGenericBuilder.snk already exists. Overwrite? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Skipping key generation." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "Generating strong-name key file..." -ForegroundColor Cyan
& $snPath -k $snkFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "Strong-name key generated successfully: $snkFile" -ForegroundColor Green
    Write-Host "`nYou can now build the project with strong-name signing enabled." -ForegroundColor Cyan
} else {
    Write-Host "Failed to generate strong-name key. Exit code: $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
