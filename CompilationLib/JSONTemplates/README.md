# Supla Template Board - C# Implementation

C# implementation of the Supla GUI-Generic template board parsing logic, based on the original C++ code.

## Overview

This library provides functionality to:
- Parse board template JSON configurations
- Apply GPIO mappings to device configurations
- Manage template libraries from `template_boards.json`
- Convert template configurations to device settings

## Architecture

### Core Components

1. **Models (`TemplateBoardModels.cs`)**
   - `BoardTemplate`: Represents a board template from JSON
   - `GpioFunction`: Enum mapping GPIO function codes
   - `SensorType`, `ConditionType`, `ButtonAction`: Supporting enums
   - `Condition`: Configuration for automated actions

2. **Parser (`TemplateBoardParser.cs`)**
   - Parses JSON template strings
   - Applies GPIO functions to device configuration
   - Handles version detection (old/new format)
   - Generates warnings for invalid configurations

3. **Device Configuration (`DeviceConfiguration.cs`)**
   - Stores complete device configuration
   - Manages relays, buttons, LEDs, RGBW, sensors, etc.
   - Provides dictionary export for serialization

4. **Template Library (`TemplateLibrary.cs`)**
   - Loads templates from `template_boards.json`
   - Provides search and filtering capabilities
   - Manages template selection

## Usage Examples

### Basic Template Parsing

```csharp
using SuplaTemplateBoard;

// Parse a JSON template string
string jsonTemplate = @"{
    ""NAME"": ""Shelly 2.5"",
    ""GPIO"": [320,0,32,0,224,193,0,0,640,192,608,225,3456,4736],
    ""FLASH"": ""2M64""
}";

var parser = new TemplateBoardParser();
var config = parser.ParseTemplate(jsonTemplate);

Console.WriteLine($"Device: {config.HostName}");
Console.WriteLine($"Relays: {config.MaxRelays}");
Console.WriteLine($"Buttons: {config.MaxButtons}");

// Check for warnings
foreach (var warning in parser.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

### Using Template Library

```csharp
using SuplaTemplateBoard;

// Load all templates from file
var library = new TemplateLibrary("template_boards.json");
library.LoadTemplates();

// Get available templates
var names = library.GetTemplateNames();
foreach (var name in names)
{
    Console.WriteLine(name);
}

// Search for specific templates
var sonoffTemplates = library.SearchTemplates("Sonoff");

// Get template by name
var template = library.GetTemplateByName("Gosund P1");
string json = library.GetTemplateJson("Gosund P1");

// Parse and use
var parser = new TemplateBoardParser();
var config = parser.ParseTemplate(json);
```

### Processing Configuration

```csharp
// Get full configuration as dictionary
var configDict = config.ToConfigDictionary();

// Access specific components
foreach (var relay in config.Relays)
{
    Console.WriteLine($"Relay {relay.Number}: GPIO {relay.GPIO}");
}

foreach (var button in config.Buttons)
{
    Console.WriteLine($"Button {button.Number}: {button.EventType}");
}

// Check conditions
foreach (var condition in config.Conditions)
{
    Console.WriteLine($"Condition: {condition.SensorType} -> {condition.ExecutiveType}");
}
```

### Web API Integration

```csharp
// Example ASP.NET Core controller action
[HttpPost("apply-template")]
public IActionResult ApplyTemplate([FromBody] string templateJson)
{
    try
    {
        var parser = new TemplateBoardParser();
        var config = parser.ParseTemplate(templateJson);
        
        return Ok(new
        {
            success = true,
            deviceName = config.HostName,
            configuration = config.ToConfigDictionary(),
            warnings = parser.Warnings
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new { success = false, error = ex.Message });
    }
}
```

## GPIO Function Codes

The library uses numeric codes to identify GPIO functions:

- **I2C**: 544 (SCL), 640 (SDA), 608 (SCL2), 576 (SDA2)
- **Relays**: 224-231 (normal), 320-327 (inverted)
- **Switches**: 32-38 (pullup), 160-166 (no pullup)
- **Buttons**: 192-195 (pullup), 3232-3235 (no pullup)
- **LEDs**: 288-291 (active low), 352-355 (active high)
- **PWM/RGBW**: 416-420 (normal), 448-452 (inverted)
- **Power Monitoring**: 2656 (CF), 2624 (CF1), 2720 (SEL)
- **Sensors**: 1248 (SI7021), 1216 (DS18x20), 4736 (NTC)

See `GpioFunction` enum in `TemplateBoardModels.cs` for complete mapping.

## Template JSON Format

```json
{
  "NAME": "Device Name",
  "GPIO": [320, 0, 32, 0, 224, ...],
  "BTNADC": [250, 500, 750],
  "BTNACTION": [0, 1, 2],
  "COND": [[0, 0, 10, 0, 0, "", 90]],
  "FLASH": "2M64",
  "MCP23017": [[[0, 1], 1, 2, 3, ...]],
  "PCF8574": [[[0, 1], 1, 2, 3, ...]]
}
```

### Fields:
- **NAME**: Device name/hostname
- **GPIO**: Array of GPIO function codes (13 for ESP8266, 36 for ESP32)
- **BTNADC**: Analog button expected voltage values
- **BTNACTION**: Button actions (0=TurnOn, 1=TurnOff, 2=Toggle)
- **COND**: Conditions array `[execType, execNum, sensorType, sensorNum, condType, valOn, valOff]`
- **FLASH**: Flash memory configuration
- **MCP23017/PCF8574/PCF8575**: I2C expander configurations

## Condition Configuration

Conditions enable automated actions based on sensor readings:

```csharp
// COND array format: [execType, execNum, sensorType, sensorNum, condType, valOn, valOff]
// Example: Turn on relay 0 when NTC sensor reads below 90
[[0, 0, 10, 0, 0, "", 90]]
```

**Executive Types**: 0=Relay, 1=RGBW  
**Sensor Types**: See `SensorType` enum  
**Condition Types**: 0=OnLess, 1=OnGreater, 2=OnLessHumidity, etc.

## Version Compatibility

The parser automatically detects template version:
- **Version 1**: 13 GPIO pins (ESP8266 old format)
- **Version 2**: 13+ GPIO pins (current format, ESP32 support)

Old version codes are automatically converted to new format.

## Requirements

- .NET 5.0 or higher
- System.Text.Json for JSON parsing

## Integration Notes

This C# implementation mirrors the C++ logic from:
- `SuplaTemplateBoard.cpp`
- `SuplaTemplateBoard.h`
- `SuplaWebPageTools.cpp`

It can be used in:
- Desktop configuration tools
- Web-based template builders
- Server-side template management
- Device configuration APIs

## License

Based on the original GUI-Generic code by krycha88, licensed under GPL v2.
