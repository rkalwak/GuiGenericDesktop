# Testing the Auto-Update Feature

## Quick Test Guide

### Current State
- **Your Latest GitHub Release**: v2.0.10
- **Your Current Version**: 2.1.0.0 (in development)
- **Repository**: https://github.com/rkalwak/GuiGenericDesktop

### Option 1: Test with a Temporary Version Downgrade (Recommended)

This allows you to test the full update flow without publishing a new release.

1. **Temporarily Change Version**:
   ```xml
   <!-- In GuiGenericBuilderDesktop.csproj, change: -->
   <Version>2.0.5.0</Version>
   <!-- This is lower than v2.0.10 on GitHub -->
   ```

2. **Build and Run**:
   ```bash
   dotnet build
   dotnet run --project GuiGenericBuilderDesktop
   ```

3. **What Should Happen**:
   - ? On startup, you'll see a notification about v2.0.10 being available
   - ? Click "Yes" to view update details
   - ? The UpdateWindow shows:
     - Current Version: 2.0.5
     - New Version: 2.0.10
     - Release notes from GitHub
   - ? Click "Install Update" to test the download

4. **Expected Download**:
   - Downloads: `GuiGenericBuilder-v2.0.10-win-x64.zip` (self-contained version)
   - Shows progress bar
   - Creates PowerShell update script
   - Prompts to restart

5. **After Testing**:
   ```xml
   <!-- Restore version back to: -->
   <Version>2.1.0.0</Version>
   ```

### Option 2: Test Update Check Only (No Download)

1. **Current Setup** (2.1.0.0 > 2.0.10):
   - Build and run normally
   - Click "?? Check for Updates" button
   - Should say: "You are running the latest version (2.1.0.0)"
   - ? This confirms the GitHub API connection works

2. **Test Manual Check**:
   - The button is in the top toolbar
   - Should complete within 1-2 seconds
   - No errors should appear

### Option 3: Test with a New Release

When you're ready to test the full flow:

1. **Create a New Release**:
   ```bash
   # Update version in .csproj first
   <Version>2.1.1.0</Version>
   
   # Build release package
   .\build-release.ps1
   
   # Create GitHub release
   # Tag: v2.1.1
   # Upload: GuiGenericBuilder-v2.1.1-win-x64.zip
   ```

2. **Test from Previous Version**:
   ```xml
   <!-- Set version to test from -->
   <Version>2.1.0.0</Version>
   ```

3. **Run and Update**:
   - App should detect v2.1.1
   - Test full download and install
   - Verify app restarts with new version

## Testing Checklist

### Startup Update Check
- [ ] App checks for updates on launch (non-blocking)
- [ ] If update available, shows notification
- [ ] Can dismiss notification and continue working
- [ ] Notification doesn't block app startup

### Manual Update Check
- [ ] "?? Check for Updates" button is visible
- [ ] Button shows status text while checking
- [ ] Shows "Up to date" when no update
- [ ] Shows UpdateWindow when update available
- [ ] Handles network errors gracefully

### Update Window
- [ ] Shows current version correctly
- [ ] Shows new version from GitHub
- [ ] Displays release notes
- [ ] "Install Update" button works
- [ ] "Remind Me Later" button closes window
- [ ] Progress bar shows during download

### Download & Install
- [ ] Downloads correct file (self-contained ZIP)
- [ ] Progress bar updates during download
- [ ] Creates update script successfully
- [ ] Shows "restart" confirmation
- [ ] App exits after confirmation

### Error Handling
- [ ] Handles no internet connection
- [ ] Handles GitHub rate limit (60/hour for unauthenticated)
- [ ] Handles missing release assets
- [ ] Logs errors to Serilog
- [ ] Shows user-friendly error messages

## Expected Results

### Successful Update Flow:
```
1. User starts app (version 2.0.5)
2. App checks GitHub in background
3. Finds v2.0.10 available
4. Shows notification: "A new version (v2.0.10) is available!"
5. User clicks "Yes"
6. UpdateWindow opens showing version info and release notes
7. User clicks "Install Update"
8. Progress bar shows: "Downloading update... 45%"
9. Download completes (GuiGenericBuilder-v2.0.10-win-x64.zip)
10. PowerShell script created
11. Message: "Update ready. App will restart."
12. App exits
13. PowerShell script runs (hidden window)
14. Files extracted and updated
15. App restarts automatically
16. User is now on v2.0.10
```

### What the PowerShell Script Does:
```powershell
1. Wait 2 seconds (ensures app closed)
2. Backup current GuiGenericBuilderDesktop.exe ? .exe.backup
3. Extract ZIP to application directory (overwrites files)
4. Start GuiGenericBuilderDesktop.exe
5. Delete backup file
6. Delete downloaded ZIP
7. Delete itself
```

## Troubleshooting Test Issues

### "Failed to check for updates"
**Cause**: Network issue or GitHub unavailable
**Test**: Check internet, try: https://api.github.com/repos/rkalwak/GuiGenericDesktop/releases

### "Rate limit exceeded"
**Cause**: Made more than 60 API calls in 1 hour
**Solution**: Wait 1 hour or add GitHub token (see AUTO_UPDATE_GUIDE.md)

### "No suitable update package found"
**Cause**: Release doesn't have matching ZIP file
**Solution**: Ensure release has `*-win-*.zip` file

### Update doesn't apply after download
**Cause**: PowerShell execution policy or antivirus
**Check**: 
```powershell
Get-ExecutionPolicy
# If Restricted, run as admin:
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Version comparison wrong
**Issue**: "Up to date" when you know there's a newer version
**Check**: 
- GitHub tag format: `v2.0.10` (starts with 'v')
- Project version: `2.1.0.0` (4-part semantic version)
- Comparison: 2.1.0.0 > 2.0.10.0 (correctly shows up to date)

## Mock Testing (Without Real Release)

To test UI without GitHub:

1. **Temporarily Modify CheckForUpdatesAsync**:
   ```csharp
   // In AutoUpdateService.cs, add at start of method:
   #if DEBUG
   var mockRelease = new Release(...); // Create mock
   return (true, mockRelease);
   #endif
   ```

2. **Test UI Only**:
   - UpdateWindow displays correctly
   - Buttons work
   - Progress bar animates
   - Don't click "Install" (no real file to download)

## Performance Testing

### Startup Impact
- Update check should be non-blocking
- Should take < 2 seconds
- Shouldn't delay window appearance
- Runs on background thread

### Network Usage
- Single API call: ~5 KB
- ZIP download: ~150 MB (self-contained) or ~10 MB (framework-dependent)
- Total bandwidth per update: ~150 MB

## Security Testing

### Verify HTTPS
- All downloads use HTTPS
- GitHub API uses HTTPS
- Check with Fiddler/Wireshark

### Check File Integrity
- Downloaded file size matches GitHub
- ZIP file is valid (can be extracted)
- No corruption during download

## Real-World Testing Scenarios

### Scenario 1: First-Time User
```
User downloads v2.0.10 from GitHub
? Installs and runs
? Sees "Up to date" (correct)
```

### Scenario 2: Existing User (Outdated)
```
User has v2.0.5 installed
? Starts app
? Notification: "v2.0.10 available"
? Updates successfully
```

### Scenario 3: Offline User
```
User has v2.0.5, no internet
? Starts app
? Update check fails silently (logged)
? App works normally
? Manual check shows error
```

### Scenario 4: Fast Release Cycle
```
User updates from 2.0.5 ? 2.0.10
? Next day, 2.0.11 released
? App notifies again
? Can update again
```

## Next Steps After Testing

1. ? Verify update check works
2. ? Test full download and install
3. ? Check logs in Serilog output
4. ? Verify version comparison logic
5. ?? Decide on update frequency (startup only, or periodic checks?)
6. ?? Consider adding "Check for updates on startup" option in settings
7. ?? Add telemetry/analytics (optional)

## Production Checklist

Before releasing with auto-update:

- [ ] Test on clean Windows installation
- [ ] Test on Windows 10 and Windows 11
- [ ] Test with antivirus enabled
- [ ] Test with limited user account (not admin)
- [ ] Verify code signing (if certificate available)
- [ ] Test rate limiting (make 61 requests)
- [ ] Document for users (in README)
- [ ] Add to release notes: "Auto-update feature added"

---

**Ready to test!** Start with Option 1 (temporary version downgrade) for the most complete test.
