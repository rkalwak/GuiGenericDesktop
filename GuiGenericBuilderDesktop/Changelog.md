# Changelog

## [2025-12-21] - Version 2.0.0.1

### Version 2.0.0.1 (2025-12-21)

**New Feature: Initial Configuration Mode**

Added `SUPLA_INITIAL_CONFIG_MODE` build flag that allows configuring how the device behaves on first boot with factory settings. This flag includes an enumeration parameter with 4 modes:

- **Mode 0 (StartInCfgMode)**: Legacy behavior - Enable AP and enter config mode
- **Mode 1 (StartOffline)**: Enter offline mode immediately, no AP started
- **Mode 2 (StartWithCfgModeThenOffline)**: Enable AP and config mode for 1 hour, then fall back to offline
- **Mode 3 (StartInNotConfiguredMode)**: Enter not configured mode (default behavior)

This feature provides better control over device security and deployment scenarios, allowing production devices to start silently without exposing WiFi access points.
