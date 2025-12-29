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

