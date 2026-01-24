# Build and Package Release Script
# This script builds the application and creates a release package

param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild = $false,
    [switch]$OpenFolder = $true
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "GUI Generic Builder - Release Build" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Get the project file
$projectFile = "GuiGenericBuilderDesktop\GuiGenericBuilderDesktop.csproj"

if (-not (Test-Path $projectFile)) {
    Write-Host "Error: Project file not found at $projectFile" -ForegroundColor Red
    exit 1
}

# Extract version from project file
Write-Host "Reading version from project file..." -ForegroundColor Yellow
$xml = [xml](Get-Content $projectFile)
$version = $xml.Project.PropertyGroup.Version

if ([string]::IsNullOrEmpty($version)) {
    Write-Host "Error: Version not found in project file" -ForegroundColor Red
    exit 1
}

Write-Host "Version: $version" -ForegroundColor Green
Write-Host ""

# Build the application
if (-not $SkipBuild) {
    Write-Host "Building application in $Configuration mode..." -ForegroundColor Yellow
    dotnet build $projectFile -c $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host ""
}

# Define paths
$buildOutput = "GuiGenericBuilderDesktop\bin\$Configuration\net10.0-windows"
$releaseFolder = "releases"
$versionFolder = "$releaseFolder\v$version"
$zipFileName = "GuiGenericBuilder-v$version-win-x64.zip"
$zipFilePath = "$releaseFolder\$zipFileName"

# Create release folder
Write-Host "Creating release folder..." -ForegroundColor Yellow
if (-not (Test-Path $releaseFolder)) {
    New-Item -ItemType Directory -Path $releaseFolder | Out-Null
}

if (Test-Path $versionFolder) {
    Write-Host "Cleaning existing version folder..." -ForegroundColor Yellow
    Remove-Item $versionFolder -Recurse -Force
}

New-Item -ItemType Directory -Path $versionFolder | Out-Null

# Copy files to release folder
Write-Host "Copying build output..." -ForegroundColor Yellow
Copy-Item "$buildOutput\*" -Destination $versionFolder -Recurse -Force

# Remove unnecessary files
Write-Host "Cleaning up unnecessary files..." -ForegroundColor Yellow
$filesToRemove = @(
    "*.pdb",
    "*.xml",
    "ref\*",
    "*.deps.json"
)

foreach ($pattern in $filesToRemove) {
    Get-ChildItem -Path $versionFolder -Filter $pattern -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
}

# Create ZIP file
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath -Force
}

Compress-Archive -Path "$versionFolder\*" -DestinationPath $zipFilePath -CompressionLevel Optimal

# Get file size
$zipSize = (Get-Item $zipFilePath).Length / 1MB
$zipSizeStr = [math]::Round($zipSize, 2)

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "Release package created successfully!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package Information:" -ForegroundColor Cyan
Write-Host "  Version:  $version" -ForegroundColor White
Write-Host "  File:     $zipFilePath" -ForegroundColor White
Write-Host "  Size:     $zipSizeStr MB" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test the application:" -ForegroundColor White
Write-Host "   - Extract the ZIP file to a test location" -ForegroundColor Gray
Write-Host "   - Run GuiGenericBuilderDesktop.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Create GitHub Release:" -ForegroundColor White
Write-Host "   - Go to: https://github.com/rkalwak/GuiGenericDesktop/releases/new" -ForegroundColor Gray
Write-Host "   - Tag: v$version" -ForegroundColor Gray
Write-Host "   - Upload: $zipFileName" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Create Git Tag (if not already done):" -ForegroundColor White
Write-Host "   git tag -a v$version -m `"Release version $version`"" -ForegroundColor Gray
Write-Host "   git push origin v$version" -ForegroundColor Gray
Write-Host ""

# Open the releases folder
if ($OpenFolder) {
    Write-Host "Opening releases folder..." -ForegroundColor Yellow
    Start-Process explorer.exe -ArgumentList (Resolve-Path $releaseFolder)
}

Write-Host "Done!" -ForegroundColor Green
