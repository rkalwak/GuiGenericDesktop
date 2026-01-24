# Quick Start: Auto-Update Feature

## For Users

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
- Windows 7 or later
- Administrator rights (for the first update only)

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
   ```

### Publishing a Release

```bash
# 1. Update version in .csproj
<Version>2.2.0.0</Version>

# 2. Build
dotnet build -c Release

# 3. Create ZIP from bin/Release/net10.0-windows/

# 4. Create GitHub Release
- Tag: v2.2.0
- Upload ZIP file
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
| "Failed to check for updates" | Check internet connection |
| "Rate limit exceeded" | Wait 1 hour or add GitHub token |
| "No update package found" | Ensure release has `.zip` or `.exe` file |
| Update doesn't install | Check PowerShell execution policy |

## Next Steps

- Read the full documentation: `AUTO_UPDATE_GUIDE.md`
- Configure code signing for production
- Set up automated release builds
- Add update verification (checksums)

---

**Need Help?** Check the full guide at `AUTO_UPDATE_GUIDE.md`
