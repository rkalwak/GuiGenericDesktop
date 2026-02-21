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
- **Opt-in by Default**: Auto-update is disabled unless explicitly enabled via an environment variable
- **User Data Preservation**: Update preserves user directories (`logs`, `configurations`, `repo`, `backup`)

## Enabling Auto-Update

Auto-update is **disabled by default**. To enable it, set the environment variable:

```
GUI_GENERIC_AUTO_UPDATE_ENABLED=true
```

Accepted values: `1`, `true`, `yes` (case-insensitive). Any other value (or absence of the variable) keeps the feature disabled.

### Setting the Variable

**Per-session (PowerShell):**
```powershell
$env:GUI_GENERIC_AUTO_UPDATE_ENABLED = "true"
.\GuiGenericBuilderDesktop.exe
```

**Persistently (System or User scope):**
```powershell
[System.Environment]::SetEnvironmentVariable("GUI_GENERIC_AUTO_UPDATE_ENABLED", "true", "User")
```

When disabled, all update checks silently return "no update available" and nothing is downloaded.

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
   - Copies the **bundled** `Scripts/update.ps1` script to a unique temp file
   - Launches the script and exits the application
   - Script waits 3 seconds for the app to close
   - Preserves user data directories (`logs`, `configurations`, `repo`, `backup`)
   - **`builder.json` is intentionally NOT preserved** — it is part of the release and gets updated
   - Backs up the current executable before replacing files
   - Extracts/replaces files, restores preserved data, then restarts the app
   - Temp script self-deletes after completion

4. **Rollback on Failure**:
   - If anything fails, restores the backed-up executable
   - Restores preserved user data even on failure
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
<Version>2.0.1.0</Version>
```

**Important**:
- Always update this version number when releasing a new version
- Use semantic versioning (MAJOR.MINOR.PATCH)
- The version in the project file must match the release tag format (e.g., version `2.0.1.0` maps to tag `v2.0.1`)

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

The auto-update service looks for release assets matching these rules:
- File extension must be `.zip` or `.exe`
- Asset name must contain `win` (case-insensitive)
- Self-contained packages are preferred over framework-dependent ones

**Current release pattern:**
- ? `GuiGenericBuilder-v2.0.1-win-x64.zip` (self-contained — **Preferred**)
- ? `GuiGenericBuilder-v2.0.1-win-x64-framework-dependent.zip` (also supported)
- ? `source-code.zip` (rejected — no `win` in name)

The self-contained build is preferred because it does not require users to have .NET installed separately.

## Building and Publishing a Release

### Automated Build Script (Recommended)

A `build-release.ps1` script is included at the repository root. It:

- Reads the version from `GuiGenericBuilderDesktop.csproj` automatically
- Builds in Release mode
- Removes debug artifacts (`.pdb`, `.xml`, `.deps.json`)
- Creates a properly named ZIP in `releases\vX.Y.Z\`
- Prints next steps (tagging, GitHub release upload)
- Opens the `releases\` folder in Explorer

Run it:
```powershell
.\build-release.ps1
```

Optional parameters:
```powershell
.\build-release.ps1 -SkipBuild        # Skip the dotnet build step
.\build-release.ps1 -OpenFolder:$false # Don't open Explorer after packaging
```

The output ZIP is named `GuiGenericBuilder-vX.Y.Z-win-x64.zip` and is placed under `releases\`.

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

## Update Script Details

The update is applied by `Scripts/update.ps1`, which is bundled with every release. During the update, `AutoUpdateService` copies this script to a unique temp file (so it is not overwritten when the ZIP is extracted) and launches it with the following parameters:

| Parameter | Description |
|-----------|-------------|
| `-AppDirectory` | Directory where the app is installed |
| `-DownloadedFilePath` | Full path to the downloaded ZIP/EXE |
| `-AssetName` | File name of the downloaded asset |
| `-ExeName` | File name of the main executable |

The script execution policy is bypassed automatically (`-ExecutionPolicy Bypass`). After completion the temp script **self-deletes**.

### Directories preserved across updates

| Directory | Contents |
|-----------|----------|
| `logs` | Application log files |
| `configurations` | Saved build configurations |
| `repo` | Downloaded platform/library repositories |
| `backup` | Previous backup files |

**`builder.json` is intentionally overwritten** — it is part of the release and contains the latest firmware parameter definitions.

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
- Verify the environment variable `GUI_GENERIC_AUTO_UPDATE_ENABLED=true` is set
- Check that `Scripts/update.ps1` exists in the application directory (it is bundled with the installer)
- Check antivirus isn't blocking PowerShell or the temp script
- Verify disk space is available
- Check application logs for `"Bundled update script not found"`

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
```powershell
.\build-release.ps1
# Output: releases\GuiGenericBuilder-v2.2.0-win-x64.zip
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

**Last Updated**: 2026-02-21  
**Version**: 2.0
