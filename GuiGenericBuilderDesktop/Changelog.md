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

**PlatformioCliHandler**

