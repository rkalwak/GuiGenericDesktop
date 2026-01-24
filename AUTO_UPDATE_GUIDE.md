# Auto-Update Feature Documentation

## Overview

The GUI Generic Builder Desktop application now includes an automatic update feature that checks for new releases from GitHub and allows users to install updates with one click.

## Features

- **Automatic Update Check on Startup**: The application checks for updates when it starts (non-intrusive)
- **Manual Update Check**: Users can click the "?? Check for Updates" button at any time
- **Download and Install**: Updates are downloaded and installed automatically
- **Release Notes Display**: Users can view the changelog before installing
- **Safe Update Process**: Creates backups before updating and can rollback if something goes wrong
- **Progress Indicator**: Shows download progress during update installation

## How It Works

### Architecture

The auto-update system consists of three main components:

1. **AutoUpdateService** (`Services/AutoUpdateService.cs`):
   - Interfaces with GitHub API using Octokit library
   - Checks for new releases
   - Downloads update files
   - Creates update scripts for safe installation

2. **UpdateWindow** (`UpdateWindow.xaml` + `UpdateWindow.xaml.cs`):
   - Displays update information and release notes
   - Allows users to install or skip updates
   - Shows download progress

3. **MainWindow Integration**:
   - Automatic check on startup
   - Manual "Check for Updates" button

### Update Process Flow

1. **Check for Updates**:
   - Queries GitHub API for latest release
   - Compares with current application version
   - Shows notification if update is available

2. **Download Update**:
   - Downloads the release asset (ZIP or EXE file)
   - Shows progress bar during download
   - Saves to temporary location

3. **Apply Update**:
   - Creates PowerShell script to handle the update
   - Backs up current executable
   - Extracts/replaces files after application closes
   - Restarts application automatically
   - Cleans up temporary files

4. **Rollback on Failure**:
   - If update fails, restores from backup
   - Restarts the application with the previous version

## Configuration

### GitHub Repository Settings

The auto-update service is configured in `MainWindow.xaml.cs` constructor:

```csharp
_autoUpdateService = new AutoUpdateService("rkalwak", "GuiGenericDesktop", _logger);
```

To change the repository:
1. Update the owner: `"rkalwak"`
2. Update the repository name: `"GuiGenericDesktop"`

### Version Management

The application version is set in `GuiGenericBuilderDesktop.csproj`:

```xml
<Version>2.1.0.0</Version>
```

**Important**: 
- Always update this version number when releasing a new version
- Use semantic versioning (MAJOR.MINOR.PATCH)
- The version in the project file must match the release tag format

## Creating GitHub Releases

To enable auto-updates, you need to create proper GitHub releases:

### Step 1: Tag Your Release

```bash
git tag -a v2.1.0 -m "Release version 2.1.0"
git push origin v2.1.0
```

### Step 2: Create GitHub Release

1. Go to your GitHub repository
2. Click "Releases" ? "Create a new release"
3. Choose the tag you just created (e.g., `v2.1.0`)
4. Fill in the release title and description (this becomes the release notes)
5. Upload your build artifacts:
   - **Option A**: Upload a ZIP file (e.g., `GuiGenericBuilder-v2.1.0-windows.zip`)
   - **Option B**: Upload the EXE file directly
6. Make sure "Set as the latest release" is checked
7. Click "Publish release"

### Release Asset Naming Conventions

The auto-update service looks for ZIP or EXE files with these patterns:
- Must contain "win" (case-insensitive)
- Prefers self-contained over framework-dependent releases

**Your Current Release Pattern:**
- ? `GuiGenericBuilder-v2.0.10-win-x64.zip` (self-contained - **Preferred**)
- ? `GuiGenericBuilder-v2.0.10-win-x64-framework-dependent.zip` (framework-dependent)

The auto-update will automatically select the self-contained version as it doesn't require users to install .NET separately.

Examples that will work:
- ? `GuiGenericBuilder-v2.1.0-win-x64.zip`
- ? `GuiGenericBuilder-win-x64.zip`
- ? `GuiGenericBuilderDesktop.exe`
- ? `source-code.zip` (won't be recognized - no "win" in name)

## Building and Publishing a Release

### Automated Build Script (Recommended)

Create a `build-release.ps1` script:

```powershell
# Build the application in Release mode
dotnet build GuiGenericBuilderDesktop/GuiGenericBuilderDesktop.csproj -c Release

# Get version from project file
$version = (Select-Xml -Path "GuiGenericBuilderDesktop/GuiGenericBuilderDesktop.csproj" -XPath "//Version").Node.InnerText

# Create output directory
$outputDir = "release-v$version"
New-Item -ItemType Directory -Force -Path $outputDir

# Copy build output
Copy-Item "GuiGenericBuilderDesktop/bin/Release/net10.0-windows/*" -Destination $outputDir -Recurse

# Create ZIP file
Compress-Archive -Path "$outputDir/*" -DestinationPath "GuiGenericBuilder-v$version-windows.zip" -Force

Write-Host "Release package created: GuiGenericBuilder-v$version-windows.zip"
```

Run the script:
```powershell
.\build-release.ps1
```

### Manual Build Steps

1. **Build in Release Mode**:
   ```bash
   dotnet build -c Release
   ```

2. **Locate Build Output**:
   ```
   GuiGenericBuilderDesktop\bin\Release\net10.0-windows\
   ```

3. **Create ZIP Archive**:
   - Include all files from the build output directory
   - Name it appropriately (e.g., `GuiGenericBuilder-v2.1.0-windows.zip`)

4. **Upload to GitHub Release**

## GitHub API Rate Limits

The auto-update feature uses the GitHub API, which has rate limits:

- **Unauthenticated requests**: 60 requests per hour per IP
- **Authenticated requests**: 5,000 requests per hour

For most users, the unauthenticated limit is sufficient since the app only checks once on startup and when manually triggered.

### Adding Authentication (Optional)

To increase rate limits, you can add GitHub token authentication:

1. Create a Personal Access Token (PAT) on GitHub with `public_repo` scope
2. Modify `AutoUpdateService.cs` constructor:

```csharp
public AutoUpdateService(string repositoryOwner, string repositoryName, ILogger logger, string githubToken = null)
{
    // ...existing code...
    
    _githubClient = new GitHubClient(new ProductHeaderValue("GuiGenericBuilderDesktop"));
    
    if (!string.IsNullOrEmpty(githubToken))
    {
        _githubClient.Credentials = new Credentials(githubToken);
    }
}
```

**Note**: Never hardcode tokens in your application. Use secure configuration or environment variables.

## Testing the Auto-Update Feature

### Local Testing

1. **Change Version Number**: In `.csproj`, set a lower version (e.g., `1.0.0.0`)
2. **Build**: `dotnet build`
3. **Run Application**: The app should detect the newer version on GitHub
4. **Test Update Flow**: Try downloading and installing the update

### Testing Without Publishing

1. Create a draft release on GitHub
2. Upload your test build
3. Mark as "pre-release" if needed
4. Modify `CheckForUpdatesAsync()` to include pre-releases:

```csharp
var releases = await _githubClient.Repository.Release.GetAll(_repositoryOwner, _repositoryName);
var latestRelease = releases
    .Where(r => !r.Draft)  // Remove the !r.Prerelease filter
    .OrderByDescending(r => r.CreatedAt)
    .FirstOrDefault();
```

## Troubleshooting

### Update Check Fails

**Problem**: "Failed to check for updates"

**Solutions**:
- Check internet connection
- Verify GitHub repository is accessible
- Check if rate limit is exceeded
- Verify repository owner and name are correct

### Download Fails

**Problem**: "No suitable update package found"

**Solutions**:
- Ensure release has a properly named asset (`.zip` or `.exe`)
- Check asset name contains "windows" or "win"
- Verify the file was uploaded correctly to the release

### Update Doesn't Apply

**Problem**: Update downloads but doesn't install

**Solutions**:
- Check PowerShell execution policy: `Get-ExecutionPolicy`
- Run PowerShell as administrator if needed
- Check antivirus isn't blocking the update script
- Verify disk space is available

### Application Doesn't Restart

**Problem**: After update, application doesn't auto-restart

**Solutions**:
- Check if process has required permissions
- Look for error messages in Windows Event Viewer
- Try restarting manually

## Security Considerations

### Code Signing (Recommended)

To avoid Windows SmartScreen warnings:

1. **Get a Code Signing Certificate**
2. **Sign Your Executable**:
   ```powershell
   signtool sign /f certificate.pfx /p password /tr http://timestamp.digicert.com /td sha256 /fd sha256 GuiGenericBuilderDesktop.exe
   ```

3. **Update Project File**:
   The project already has code signing configuration:
   ```xml
   <CodeSigningEnabled>true</CodeSigningEnabled>
   <CertificateFile>$(ProjectDir)GuiGenericBuilder.pfx</CertificateFile>
   ```

### Update Verification

The current implementation trusts GitHub releases. For production:

1. **Verify Release Source**: Only download from official repository
2. **Check File Integrity**: Add SHA-256 checksums to releases
3. **Digital Signatures**: Sign update packages

Example enhancement:
```csharp
// In AutoUpdateService.cs
private async Task<bool> VerifyDownloadIntegrity(string filePath, string expectedHash)
{
    using (var sha256 = SHA256.Create())
    using (var stream = File.OpenRead(filePath))
    {
        var hash = await sha256.ComputeHashAsync(stream);
        var hashString = BitConverter.ToString(hash).Replace("-", "");
        return hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
```

## Future Enhancements

Potential improvements for the auto-update system:

1. **Update Channels**: Support stable/beta/nightly channels
2. **Incremental Updates**: Download only changed files (delta updates)
3. **Background Downloads**: Download updates in the background
4. **Scheduled Checks**: Check for updates on a schedule (daily/weekly)
5. **Update History**: Show previously installed versions
6. **Rollback Feature**: Allow users to revert to previous versions
7. **Auto-Install**: Option to install updates automatically without user confirmation

## Example Release Workflow

Here's a complete workflow for releasing a new version:

1. **Update Version**:
   ```xml
   <!-- GuiGenericBuilderDesktop.csproj -->
   <Version>2.2.0.0</Version>
   ```

2. **Update Changelog**:
   ```markdown
   <!-- Changelog.md -->
   ## Version 2.2.0 - 2025-01-15
   - Added auto-update feature
   - Fixed compilation issues
   - Improved UI responsiveness
   ```

3. **Build Release**:
   ```bash
   dotnet build -c Release
   ```

4. **Create Archive**:
   ```bash
   cd GuiGenericBuilderDesktop/bin/Release/net10.0-windows/
   7z a GuiGenericBuilder-v2.2.0-windows.zip *
   ```

5. **Create Git Tag**:
   ```bash
   git tag -a v2.2.0 -m "Version 2.2.0"
   git push origin v2.2.0
   ```

6. **Create GitHub Release**:
   - Go to repository ? Releases ? New Release
   - Tag: `v2.2.0`
   - Title: `GUI Generic Builder v2.2.0`
   - Description: Copy from Changelog.md
   - Upload: `GuiGenericBuilder-v2.2.0-windows.zip`
   - Publish Release

7. **Test Update**:
   - Run previous version
   - Click "Check for Updates"
   - Verify update installs correctly

## License and Credits

The auto-update feature uses:
- **Octokit**: GitHub API client library (MIT License)
- **PowerShell**: For update script execution (built into Windows)

## Support

For issues or questions about the auto-update feature:
1. Check this documentation
2. Review GitHub Issues
3. Contact the development team

---

**Last Updated**: 2025-01-15  
**Version**: 1.0
