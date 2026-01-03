# Changelog

## [2025-12-21] - Version 2.0.1


**New Feature: Initial Configuration Mode**

Added `SUPLA_INITIAL_CONFIG_MODE` build flag that allows configuring how the device behaves on first boot with factory settings. This flag includes an enumeration parameter with 4 modes:

- **Mode 0 (StartInCfgMode)**: Legacy behavior - Enable AP and enter config mode
- **Mode 1 (StartOffline)**: Enter offline mode immediately, no AP started
- **Mode 2 (StartWithCfgModeThenOffline)**: Enable AP and config mode for 1 hour, then fall back to offline
- **Mode 3 (StartInNotConfiguredMode)**: Enter not configured mode (default behavior)

This feature provides better control over device security and deployment scenarios, allowing production devices to start silently without exposing WiFi access points.

## [2025-12-25] - Version 2.0.2

**New Feature: Backup of installed firmware**

Added "Backup" checkbox next to "Deploy" checkbox in the UI to allow users to create a backup of the currently installed firmware before deploying a new one. The backup is saved in the `backup/` directory with a timestamped filename. This feature helps users safeguard their existing firmware in case they need to revert back after deployment.
- Backup checkbox is enabled by default (checked)
- Backup only occurs when both "Deploy" and "Backup" are checked
- Backup is stored in application directory and subdirectory `backup`
- There are two files, `*.backup` storing firmware and `*.info` storing metadata.
- Backup uses esptool command: `read-flash 0x000000 0x400000` at 921600 baud
- Backup files are raw flash dumps (4-16MB depending on device)

## [2025-12-29] - Version 2.0.3

### Added

**Repository Validation Before Compilation**
- Added validation to check if GUI-Generic repository exists before compilation
- **Added check for empty repository directory** - prevents compilation when directory exists but is empty
- Shows user-friendly message if repository is missing: "Please click '1. Update Gui-Generic' button first"
- Shows specific message if directory is empty: "GUI-Generic repository directory is empty!"
- Validates essential files (platformio.ini) to detect corrupted/incomplete repositories
- Prevents compilation errors by catching missing repository early
- Clear error messages guide users to download repository first
- Uses `Directory.EnumerateFileSystemEntries()` for efficient empty directory detection

**CI/CD GitHub Actions Workflows**
- Added comprehensive CI/CD pipeline using GitHub Actions
- Three workflows for different purposes:
  - **CI Workflow**: Runs on every push, validates build and tests
  - **Build and Publish**: Creates artifacts for master/main branches, handles nightly builds
  - **Release Workflow**: Automates release creation from version tags
- Release workflow features:
  - Builds two variants: self-contained and framework-dependent
  - Automatic version extraction from git tags
  - Creates GitHub releases with ZIP archives
  - Includes release notes and installation instructions
- Nightly builds retained for 90 days
- Release artifacts include builder.json and documentation
- Supports semantic versioning (v*.*.*) and prerelease tags (alpha, beta, rc)
- Comprehensive documentation in `.github/workflows/README.md`

**Backup Path Display in Compilation Results**
- Shows backup file path in compilation results window when backup is created
- Displays both backup file name and full path
- Includes "Copy Path" and "Open Folder" buttons for easy access
- Visual confirmation that backup was successfully created

**Loading Indicators for Long Operations**
- Added status text indicator for repository download operation
  - Shows "⏳ Downloading GUI-Generic repository..." during download
  - Shows "✓ Repository updated successfully!" on success (green)
  - Shows "✗ Repository update failed" on error (red)
  - Button is disabled during operation to prevent multiple clicks
  - Status auto-hides after 3 seconds
- Added status text indicator for device detection operation
  - Shows "⏳ Detecting device..." during detection
  - Shows "✓ Device detected: [chip] on [port]" on success (green)
  - Shows "✗ No device detected" when no device found (orange-red)
  - Shows "✗ Device detection error" on error (red)
  - Button is disabled during operation
  - Status auto-hides after 3 seconds
- Added status text indicator for compilation operation
  - Shows "⏳ Compiling firmware..." during compilation (black text)
  - Shows "✓ Compilation successful!" on success (green)
  - Shows "✗ Compilation failed" on error (red)
  - Shows "✗ Compilation error" on exception (red)
  - Button is disabled during operation
  - Status auto-hides after 3 seconds
  - Extended compilation timer from 90 to 120 seconds (2 minutes)
  - Timer now displays "02:00" initially and counts down
- Improved user feedback with emoji indicators (⏳, ✓, ✗)
- Color-coded status messages for quick visual feedback
- Non-intrusive auto-hiding status messages



### Changed

**Test Organization and CI/CD Updates**
- Marked all Platform.IO integration tests with `[Trait("Category", "Integration")]`
- Updated CI workflow to exclude integration tests from regular builds
  - CI now only runs unit tests: `--filter "Category!=Integration"`
  - Faster feedback loop for developers (< 5 minutes vs 60+ minutes)
- Updated build-and-publish and release workflows to exclude integration tests
- Created dedicated `integration-tests.yml` workflow for running full integration tests
  - Runs on manual trigger or daily schedule (2 AM UTC)
  - Runs automatically on changes to CompilationLib
  - Installs Platform.IO and clones GUI-Generic repository
  - Timeout set to 120 minutes for long-running tests
  - Test results uploaded as artifacts (30-day retention)

**CI/CD Configuration Updates**
- Updated all workflow files to use correct solution name: `GuiGenericBuilderDesktop.sln` (was incorrectly set to `GuiGenericV2.sln`)
- Verified .NET 10.0 target framework configuration
- Added solution structure documentation to workflow README

**Enhanced Device Detection with USB Bridge Support**
- Added `DetectByUsbBridge()` method to detect microcontrollers via USB VID/PID identifiers
- Refactored to use comprehensive `UsbDeviceRecognition` database with 27+ USB bridge variants
- Enhanced device information with vendor name, product name, and maximum baudrate
- Supports detection even when COM port drivers are not fully loaded
- Recognizes common USB-to-UART bridges:
  - **QinHeng Electronics**: CH340, CH341, CH343, CH9102, CH9101 (7 variants, 460K-6M baud)
  - **Silicon Labs**: CP2102(n), CP2105, CP2108 (3 variants, 2M-3M baud)
  - **FTDI**: FT232R, FT2232, FT4232, FT232H, FT230X (5 variants, 3M-12M baud)
  - **Espressif Systems**: ESP32-S2/S3/C3 Native USB (5 variants, 2M baud)
  - **Prolific**: PL2303
- Added `DetectCOMPortWithUsbBridge()` method that combines both detection approaches
- Detection now tries USB Bridge first (more reliable), then falls back to standard method
- Updated "Check Device" button to use combined detection for better reliability
- Added vendor prioritization (Espressif > Silicon Labs > FTDI > Others)
- Automatic VID/PID parsing from Windows device IDs
- Baudrate optimization based on bridge capabilities
- Improved logging with detailed device specifications:
  - Device descriptions with max baudrate
  - VID:PID hex values (e.g., VID:0x1A86 PID:0x7523)
  - Full vendor and product identification
- Cleaner separation of detection logic with internal `DetectedUsbDevice` class
- More robust device identification by matching against 27+ known VID/PID combinations

**Compilation Results Window - Now Shows Encoded Configuration String**
- Changed compilation results window to display encoded configuration string instead of SHA256 hash
- Encoded string is reversible and can be decoded to restore the exact build configuration
- Updated UI labels:
  - "Build Configuration Hash" → "Build Configuration String"
  - Description now mentions it's encoded and can be shared/reused
  - "Copy Hash" button → "Copy Configuration" button
- Encoded string can be:
  - Copied to clipboard for sharing
  - Pasted into "Load from Encoded String" tab to recreate configuration
  - Decoded to view all build flags
- Maintains backward compatibility with hash-based configuration files
- More user-friendly than hash - users can actually restore their configuration from this string

## [2025-01-08] - Version 2.0.4

### Changed

**Complete Hash Removal - EncodedConfig as Primary Identifier**
- Completely removed SHA256 hash functionality from codebase
- No backward compatibility maintained - clean slate approach
- **SaveConfiguration API simplified**:
  - Removed obsolete `SaveConfiguration(string hash, ...)` method
  - Single method signature: `SaveConfiguration(enabledFlags, configName, platform, comPort)`
  - Auto-save filenames now use timestamps instead of hash: `Config_20250108_143022.json`
  - Manual save filenames use sanitized custom names: `MyConfig.json`
- **LoadConfiguration simplified**:
  - Only works with `EncodedConfig` parameter (no hash lookup)
  - Method signature: `LoadConfiguration(string encodedConfig)`
- **DeleteConfiguration simplified**:
  - Now works with filename instead of hash
  - Method signature: `DeleteConfiguration(string fileName)`
  - Automatically adds `.json` extension if missing
- **SavedBuildConfiguration class cleaned up**:
  - Removed `Hash` property completely
  - EncodedConfig is now the sole identifier
  - Cleaner JSON structure without obsolete hash field
- **ConfigurationManagerWindow updated**:
  - Delete button now uses `FileName` property instead of `Hash`
  - No more obsolete property warnings
- **CompileResponse cleaned up**:
  - Removed `HashOfOptions` property completely
- **Benefits**:
  - 76 lines of code removed
  - More readable auto-save filenames with timestamps
  - Simpler API with no obsolete methods
  - Focus on reversible EncodedConfig strings
  - Easier to debug with timestamp-based filenames
  - No SHA256 complexity

**Live Compilation Time Display with Permanent Status**
- Added real-time compilation time display that updates during compilation
- Timer updates every 100ms for smooth visual feedback
- Shows elapsed time in format: "⏳ Compiling firmware... Elapsed: 45.3s"
- **Permanent status display** - compilation results stay visible after completion
  - Removed auto-hide behavior (previously hid after 3 seconds)
  - Success message stays visible: "✓ Compilation successful! Time: 45.3s"
  - Failure message stays visible: "✗ Compilation failed! Time: 37.8s"
  - Error message stays visible: "✗ Compilation error! Time: 5.2s"
- Status messages remain until next user action
- Color-coded results:
  - Black text with oblique font during compilation
  - Green for success
  - Red for failures
- Time displayed with 1 decimal place precision (e.g., "45.3s")
- Background timer task properly managed with cancellation tokens
- Thread-safe UI updates using Dispatcher.Invoke
- Provides immediate feedback on compilation duration
- Helps users track compilation performance over time

**Library Version Extraction - Return Empty String Instead of Null**
- Updated `LibraryVersionExtractor` to return empty strings instead of null
- **Public methods updated**:
  - `GetSuplaDeviceVersion()` returns `string.Empty` instead of `null`
  - `GetGuiGenericVersion()` returns `string.Empty` instead of `null`
- **Private helper methods updated**:
  - `ExtractVersionFromLibraryJson()` returns `string.Empty`
  - `ExtractVersionFromLibraryProperties()` returns `string.Empty`
  - `ExtractVersionFromHeader()` returns `string.Empty`
- **Benefits**:
  - No null reference exceptions
  - Simpler null checks: `if (!string.IsNullOrEmpty(version))`
  - No need for null coalescing operators
  - Cleaner code throughout the codebase
  - Consistent behavior across all methods
- **All tests updated**:
  - 5 test methods renamed to reflect empty string expectations
  - Changed assertions from `BeNull()` to `BeEmpty()`
  - All 13 LibraryVersionExtractor tests passing

**Version Display Improvements**
- Removed version label TextBlocks from UI (SuplaDevice and GUI-Generic versions)
- Versions now displayed only in window title: `GUI-Generic Builder - GG v25.02.11 - SD v2.4.5`
- Cleaner UI with less visual clutter
- More space for status messages and compilation feedback
- Versions still logged for debugging purposes
- Permanent display in window title (always visible)
- Simpler information hierarchy

**Compilation Progress Bar Removed**
- Removed visual progress bar and countdown timer from compilation UI
- Replaced with simpler status text messages
- Cleaner, less cluttered interface
- No time pressure or anxiety from countdown
- Focus on compilation status rather than time remaining
- Status messages provide clear feedback:
  - "⏳ Compiling firmware..." during compilation
  - "✓ Compilation successful!" on success
  - "✗ Compilation failed!" on failure

## [2025-01-09] - Version 2.0.5

### Added

**Platform Compatibility Validation**
- Added platform-specific build flag compatibility checking across the application
- New `DisabledOnPlatforms` property in `BuildFlagItem` class to define platform restrictions
- **Automatic validation on board selection**:
  - When user selects a board manually (e.g., ESP32-C6), incompatible flags are automatically disabled
  - Shows notification listing all flags that were disabled
  - Prevents accidental selection of incompatible configurations
- **Real-time validation when enabling flags**:
  - When user tries to enable a flag incompatible with selected platform, it's immediately prevented
  - Checkbox is automatically unchecked
  - Shows warning message explaining incompatibility
  - Lists platforms where the flag is disabled
- **Pre-compilation validation**:
  - Validates all enabled flags before starting compilation
  - Blocks compilation if incompatible flags are found
  - Shows error message with list of incompatible flags
  - Prevents wasted compilation time on invalid configurations
- **Device detection integration**:
  - When device is auto-detected via "Check Device" button, incompatible flags are disabled
  - Platform chip type is normalized (e.g., "ESP32-C6" → "esp32-c6") for validation
- **Configuration in builder.json**:
  - Platform restrictions defined per-flag using lowercase chip identifiers
  - Example: `"DisabledOnPlatforms": ["esp8266", "esp32-c3", "esp32-s2", "esp32-s3", "esp32-c6"]`
  - First implementation: `SUPLA_WT32_ETH01_LAN8720` flag disabled on C3/C6/S2/S3/ESP8266
- **Helper methods**:
  - `DisableIncompatibleFlags(string platformTag)` - Disables incompatible flags for given platform
  - `ValidatePlatformCompatibility(string platformTag, List<BuildFlagItem> enabledFlags)` - Returns list of incompatible flags
- **Benefits**:
  - Prevents compilation errors from platform-specific hardware limitations
  - Guides users to valid configurations
  - Clear feedback at every interaction point
  - Reduces support burden from invalid configurations
  - Better user experience with immediate validation

### Changed

**Board Selection Handler Simplified**
- Simplified `boardSelector.SelectionChanged` event handler
- Now uses `Content` property instead of complex tag-to-chip-type conversion
- Direct conversion: "ESP32 (default)" → "esp32 (default)", then calls `DisableIncompatibleFlags`
- Cleaner, more maintainable code
- Consistent with chip type format used in `builder.json`

### Technical Details

**Implementation Details**:
- Platform tags converted to lowercase chip identifiers for matching
- Case-insensitive string comparison for platform names
- Validation occurs at 4 points: board selection, device detection, flag enabling, pre-compilation
- All validation actions are logged for debugging
- User-friendly messages with bullet-point lists of affected flags
- Thread-safe UI updates using proper WPF data binding

### Fixed

**PlatformIO Handler - Exact Flag Matching**
- Fixed bug in `CommentUnlistedFlagsBetweenMarkers` method where substring matching caused incorrect flag enabling
- **Problem**: `SUPLA_DIRECT_LINK_TEMPERATURE_SENSOR` was being enabled when only `SUPLA_DIRECT_LINK` was in the allowed list
- **Root cause**: Code used `Contains()` for flag matching, which matched partial strings
- **Solution**: Implemented exact flag name matching with parameter validation
- **New logic**:
  - Extracts exact flag name from each line (format: `-D FLAG_NAME` or `-D FLAG_NAME=value`)
  - Uses `Equals()` for exact string comparison instead of `Contains()`
  - Properly identifies flag parameters by checking if suffix matches a defined parameter identifier
  - Example: `SUPLA_MS5611_Altitude` is recognized as parameter of `SUPLA_MS5611` only if `Altitude` is in the Parameters list
- **Benefits**:
  - Prevents unintended flag enabling from partial name matches
  - Correctly distinguishes between base flags and separate flags with similar names
  - Maintains support for flag parameters (e.g., `SUPLA_INITIAL_CONFIG_MODE_Mode`)
  - More predictable and reliable flag management
- **Test coverage**: Added test `CommentUnlistedFlags_DirectLinkWithoutParameter_DoesNotEnableTemperatureSensor` to verify fix
- All 21 PlatformIO handler unit tests passing


