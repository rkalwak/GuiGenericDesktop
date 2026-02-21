# Quick Start: Auto-Update Feature

## For Users

### Enabling Auto-Update

Auto-update is **off by default**. To turn it on, set the environment variable before launching the app:

```powershell
$env:GUI_GENERIC_AUTO_UPDATE_ENABLED = "true"
.\GuiGenericBuilderDesktop.exe
```

To enable it permanently for your Windows user account:
```powershell
[System.Environment]::SetEnvironmentVariable("GUI_GENERIC_AUTO_UPDATE_ENABLED", "true", "User")
```

### Checking for Updates

1. **Automatic Check**: The app checks for updates when you start it
2. **Manual Check**: Click the "?? Check for Updates" button in the top toolbar

### Installing Updates

When an update is available:

1. A popup will show the new version and release notes
2. Click **"Install Update"** to download and install
3. The app will restart automatically with the new version
4. Or click **"Remind Me Later"** to skip

### Requirements

- Internet connection
- Windows 10 or later (PowerShell 5.1+)
- `GUI_GENERIC_AUTO_UPDATE_ENABLED` environment variable set to `true`

## For Developers

### Quick Setup

1. **Add NuGet Package** (already done):
   ```bash
   dotnet add package Octokit
   ```

2. **Files Created**:
   - `Services/AutoUpdateService.cs` - Update logic
   - `UpdateWindow.xaml` + `.cs` - Update UI
   - `AUTO_UPDATE_GUIDE.md` - Full documentation

3. **Configuration**:
   ```csharp
   // In MainWindow.xaml.cs
   _autoUpdateService = new AutoUpdateService("username", "repo-name", _logger);
   // Feature is gated by GUI_GENERIC_AUTO_UPDATE_ENABLED env var
   ```

### Publishing a Release

```powershell
# 1. Update version in .csproj
<Version>2.2.0.0</Version>

# 2. Build and package (reads version automatically, outputs to releases\)
.\build-release.ps1
# Creates: releases\GuiGenericBuilder-v2.2.0-win-x64.zip

# 3. Create GitHub Release
- Tag: v2.2.0
- Upload: releases\GuiGenericBuilder-v2.2.0-win-x64.zip
- Include release notes
```

### Testing Locally

```csharp
// Temporarily lower version to test
<Version>1.0.0.0</Version>

// Run app - it should detect "newer" version on GitHub
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Update check silently skipped | Set `GUI_GENERIC_AUTO_UPDATE_ENABLED=true` env var |
| "Failed to check for updates" | Check internet connection |
| "Rate limit exceeded" | Wait 1 hour or add GitHub token |
| "No update package found" | Ensure release has a `.zip` or `.exe` with `win` in the name |
| "Bundled update script not found" | Reinstall the app; `Scripts/update.ps1` is missing |
| Update doesn't apply | Check antivirus isn't blocking the temp PowerShell script |

## Next Steps

- Read the full documentation: `AUTO_UPDATE_GUIDE.md`
- Configure code signing for production
- Set up automated release builds
- Add update verification (checksums)

---

**Need Help?** Check the full guide at `AUTO_UPDATE_GUIDE.md`
