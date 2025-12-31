# Gui-Generic Builder Desktop

## Overview

Gui-Generic Desktop is a desktop application written in .NET WPF that allows users to build Supla firmware with different options.

## Prerequisites

Platform.io must be installed in its default location.
Install Vistual Studio Code and then install the Platform.io extension or just Platform.io CLI.

## Supported devices

- ESP32
- ESP32-C6
- ESP32-C3
- ESP32-S3

## Not supported devices

- ESP8266

## Development Guidelines

### Testing

**Important:** When making code changes, **DO NOT run integration tests** during development.

Integration tests can take a very long time (50+ seconds) as they compile actual firmware for multiple ESP32 platforms. Use unit tests for rapid feedback during development.

**Run unit tests only:**
```bash
dotnet test CompilationLib.Tests\CompilationLib.Tests.csproj --filter "Category!=Integration" --verbosity minimal
```

**Run all tests (including integration) only for final validation:**
```bash
dotnet test CompilationLib.Tests\CompilationLib.Tests.csproj --verbosity minimal
```

**Test categories:**
- **Unit Tests**: Fast, isolated tests (<1 second total)
- **Integration Tests**: Slow, compile actual firmware (50+ seconds per platform)

### Code Changes

When implementing features or fixes:
1. Write/update unit tests first
2. Run unit tests frequently (`Category!=Integration`)
3. Make incremental changes
4. Only run integration tests before committing
5. Use `--verbosity minimal` for cleaner output

## How to

## To do

- sound when compilation is done
- Niestety ca³kowicie wirtualny termostat (oparty na linkach bezpoœrednich) nie dzia³a. To znaczy dzia³a odczyt temperatury, ale jeœli dodamy linki bezpoœrednie do przekaŸnika (w³¹cznika) to modu³ odmawia wspó³pracy. 
Zawiesza siê, nie loguje do cloud i trzeba go przeflashowaæ na nowo, bo nawet w tryb config wejœæ nie chce. Krystian nie da³ z tym rady, ale mia³em nadziejê, ¿e siê "cudownie" naprawi³o. Niestety nie ;-)