# Changelog

## [2025-12-21] - Version 2.0.0.1


**New Feature: Initial Configuration Mode**

Added `SUPLA_INITIAL_CONFIG_MODE` build flag that allows configuring how the device behaves on first boot with factory settings. This flag includes an enumeration parameter with 4 modes:

- **Mode 0 (StartInCfgMode)**: Legacy behavior - Enable AP and enter config mode
- **Mode 1 (StartOffline)**: Enter offline mode immediately, no AP started
- **Mode 2 (StartWithCfgModeThenOffline)**: Enable AP and config mode for 1 hour, then fall back to offline
- **Mode 3 (StartInNotConfiguredMode)**: Enter not configured mode (default behavior)

This feature provides better control over device security and deployment scenarios, allowing production devices to start silently without exposing WiFi access points.

## [2025-12-25] - Version 2.0.0.2

**New Feature: Backup of installed firmware**

Added "Backup" checkbox next to "Deploy" checkbox in the UI to allow users to create a backup of the currently installed firmware before deploying a new one. The backup is saved in the `backup/` directory with a timestamped filename. This feature helps users safeguard their existing firmware in case they need to revert back after deployment.
- Backup checkbox is enabled by default (checked)
- Backup only occurs when both "Deploy" and "Backup" are checked
- Backup is stored in application directory and subdirectory `backup`
- There are two files, `*.backup` storing firmware and `*.info` storing metadata.
- Backup uses esptool command: `read-flash 0x000000 0x400000` at 921600 baud
- Backup files are raw flash dumps (4-16MB depending on device)

