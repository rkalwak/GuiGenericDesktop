# Changelog

## [2026-02-12] - Version 2.0.13

**ESP32-S2 Support**
- Added support for ESP32-S2 platform

## [2026-02-10] - Vertion 2.0.12

**Optimized memory usage due to displaying logs online**
- Used StringBuilder and removed --verbose logging of compilation process

## [2026-02-09] - Version 2.0.11

**Live Compilation Progress**
- Added real-time compilation output display.
- Timer shows elapsed compilation time.
- Logs stream live during build process.
- Final status displays success or failure with color coding.
- Added CC1101 flag support, with parameters.
- Added missing build flags.

## [2026-01-15] - Version 2.0.10

**Changelog & Help**
- Added "Changelog" and "Help" buttons in the main window

## [2026-01-11] - Version 2.0.9

**Bug fixes**
- Related to partition selection and firmware merging

## [2026-01-11] - Version 2.0.8

**Language support**
- Added Polish language support

## [2026-01-05] - Version 2.0.7

**Merging bin files into ZIP**
- During compilation, creates a merged ZIP file containing:
  - `firmware.bin` - Main firmware binary
  - `bootloader.bin` - ESP32 bootloader
  - `partitions.bin` - Partition table
  - `firmware_mergd.bin` - Merged firmware binary for esptool flashing

## [2026-01-04] - Version 2.0.6

**UI Layout Reorganization - Sticky Control Panels**
- Completely reorganized MainWindow UI layout for better usability

## [2026-01-03] - Version 2.0.5

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
  - Example: `"DisabledOnPlatforms": ["esp32-c3", "esp32-s2", "esp32-s3", "esp32-c6"]`
  - First implementation: `SUPLA_WT32_ETH01_LAN8720` flag disabled on C3/C6/S2/S3

**Stop Compilation Feature**
- Added ability to stop ongoing compilation process

**PlatformIO Process Cancellation**
- Implemented proper process cancellation in `PlatformioCliHandler`

**Automatic Firmware Backup with Configuration**
- Firmware binary is now automatically copied to configurations directory when compilation succeeds
- Firmware file is named using the sanitized configuration name (e.g., `MyConfig.bin`)
- Firmware path is stored in the configuration JSON as `FirmwareFileName` property

**Configuration Manager**
- Added information about configurations and access to compiled files

**Merged Firmware ZIP Files**
- Automatically creates merged ZIP file during compilation
- ZIP file contains all necessary files for manual flashing:
  - `firmware.bin` - Main firmware binary
  - `bootloader.bin` - ESP32 bootloader
  - `partitions.bin` - Partition table
- ZIP filename format: `{ConfigName}_merged.zip`
- Stored alongside configuration in configurations directory

**Firmware Management in Configurations Directory**
- All firmware files now stored in configurations directory
- Firmware persists between rebuilds
- Files organized with configuration names
- **Stored files**:
  - `{ConfigName}.bin` - Main firmware binary
  - `{ConfigName}_merged.zip` - Complete firmware package
- **Configuration JSON tracking**:
  - `FirmwareFileName` property tracks main firmware
  - `MergedZipFileName` property tracks ZIP package

## [2025-12-31] - Version 2.0.4

**EncodedConfig introduction instead of GUID from old GG**
- Replaced GUID with EncodedConfig string for configuration identification

**Live Compilation Time Display**
- Added real-time compilation time display that updates during compilation
- Timer updates every 100ms for smooth visual feedback
- Shows elapsed time in format: "⏳ Compiling firmware... Elapsed: 45.3s"
- Color-coded results:
  - Black text with oblique font during compilation
  - Green for success
  - Red for failures

**Library Version Extraction**
- Versions now displayed only in window title: `GUI-Generic Builder - GG v25.02.11 - SD v25.11`

## [2025-12-29] - Version 2.0.3

**Repository Validation Before Compilation**
- Added validation to check if GUI-Generic repository exists before compilation
- **Added check for empty repository directory** - prevents compilation when directory exists but is empty
- Shows user-friendly message if repository is missing: "Please click '1. Update Gui-Generic' button first"
- Shows specific message if directory is empty: "GUI-Generic repository directory is empty!"
- Validates essential files (platformio.ini) to detect corrupted/incomplete repositories
- Prevents compilation errors by catching missing repository early

**CI/CD GitHub Actions Workflows**
- Added comprehensive CI/CDB pipeline using GitHub Actions

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

**Test Organization**

**Enhanced Device Detection with USB Bridge Support**
- Recognizes common USB-to-UART bridges:
  - **QinHeng Electronics**: CH340, CH341, CH343, CH9102, CH9101 (7 variants, 460K-6M baud)
  - **Silicon Labs**: CP2102(n), CP2105, CP2108 (3 variants, 2M-3M baud)
  - **FTDI**: FT232R, FT2232, FT4232, FT232H, FT230X (5 variants, 3M-12M baud)
  - **Espressif Systems**: ESP32-S2/S3/C3 Native USB (5 variants, 2M baud)
  - **Prolific**: PL2303
- Improved logging with detailed device specifications:
  - Device descriptions with max baudrate
  - VID:PID hex values (e.g., VID:0x1A86 PID:0x7523)
  - Full vendor and product identification

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

## [2025-12-25] - Version 2.0.2

**New Feature: Backup of installed firmware**

Added "Backup" checkbox next to "Deploy" checkbox in the UI to allow users to create a backup of the currently installed firmware before deploying a new one. The backup is saved in the `backup/` directory with a timestamped filename. This feature helps users safeguard their existing firmware in case they need to revert back after deployment.
- Backup checkbox is enabled by default (checked)
- Backup only occurs when both "Deploy" and "Backup" are checked
- Backup is stored in application directory and subdirectory `backup`
- There are two files, `*.backup` storing firmware and `*.info` storing metadata.
- Backup uses esptool command: `read-flash`
- Backup files are raw flash dumps (4-16MB depending on device)

## [2025-12-21] - Version 2.0.1

**New Feature: Initial Configuration Mode**

Added `SUPLA_INITIAL_CONFIG_MODE` build flag that allows configuring how the device behaves on first boot with factory settings. This comprehensive flag includes multiple parameters for complete device initialization control:

### Mode Parameter (enum)
Defines the startup behavior of the device:

- **Mode 0 (StartInCfgMode)**: Legacy behavior - Enable AP and enter config mode
- **Mode 1 (StartOffline)**: Enter offline mode immediately, no AP started
- **Mode 2 (StartWithCfgModeThenOffline)**: Enable AP and config mode for configurable duration, then fall back to offline
- **Mode 3 (StartInNotConfiguredMode)**: Enter not configured mode (default behavior)

### Additional Configuration Parameters

- **UseBuildConfiguration** (enum): Toggle to use settings from compilation
  - Value 0: Don't use build configuration
  - Value 1: Use build configuration
  - Default: 0 (No)

- **TimeoutInMin** (number): Config mode timeout duration in minutes
  - Specifies how long the device remains in configuration mode when Mode 2 is selected
  - Default: 5 minutes

- **WIFISsid** (string): WIFI network SSID for initial connection
  - Default: empty (not set)

- **WIFIPass** (string): WIFI network password
  - Default: empty (not set)

- **Server** (string): Supla server address
  - Default: empty (not set)

- **Email** (string): Supla account email address
  - Default: empty (not set)

- **Login** (string): GUI login username
  - Default: "admin"

- **Password** (string): GUI login password
  - Default: "pass"

- **DeviceName** (string): Device name in Supla cloud
  - Default: "GG BD"


## [2025-11-28 - 2025-12-21] - Initial Development