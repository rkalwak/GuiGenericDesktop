using System;
using System.Text.Json;
using SuplaTemplateBoard;
using SuplaTemplateBoard.Models;

namespace SuplaTemplateBoard.Examples
{
    /// <summary>
    /// Example usage of the Template Board library
    /// </summary>
    public class UsageExample
    {
        public static void Main(string[] args)
        {
            // Example 1: Parse a JSON template string directly
            ExampleParseJsonString();

            Console.WriteLine("\n" + new string('-', 80) + "\n");

            // Example 2: Load templates from file and select one
            ExampleLoadFromLibrary();

            Console.WriteLine("\n" + new string('-', 80) + "\n");

            // Example 3: Process specific board template
            ExampleProcessShelly25();
        }

        static void ExampleParseJsonString()
        {
            Console.WriteLine("=== Example 1: Parse JSON Template String ===\n");

            string jsonTemplate = @"{
                ""NAME"": ""Shelly 2.5"",
                ""GPIO"": [320,0,32,0,224,193,0,0,640,192,608,225,3456,4736],
                ""COND"": [[0,0,10,0,0,"""",90],[0,1,10,0,0,"""",90]],
                ""FLASH"": ""2M64""
            }";

            var parser = new TemplateBoardParser();
            var config = parser.ParseTemplate(jsonTemplate);

            Console.WriteLine($"Device Name: {config.HostName}");
            Console.WriteLine($"Flash Size: {config.FlashSize}");
            Console.WriteLine($"Max Relays: {config.MaxRelays}");
            Console.WriteLine($"Max Buttons: {config.MaxButtons}");
            Console.WriteLine($"Max Conditions: {config.MaxConditions}");
            
            Console.WriteLine("\nWarnings:");
            foreach (var warning in parser.Warnings)
            {
                Console.WriteLine($"  - {warning}");
            }

            Console.WriteLine("\nRelays:");
            foreach (var relay in config.Relays)
            {
                Console.WriteLine($"  Relay {relay.Number}: GPIO {relay.GPIO}, Inverted: {relay.Inverted}");
            }

            Console.WriteLine("\nButtons:");
            foreach (var button in config.Buttons)
            {
                Console.WriteLine($"  Button {button.Number}: GPIO {button.GPIO}, Event: {button.EventType}, Action: {button.Action}");
            }

            Console.WriteLine("\nConditions:");
            foreach (var condition in config.Conditions)
            {
                Console.WriteLine($"  Executive: {condition.ExecutiveType}#{condition.ExecutiveNumber}, " +
                                $"Sensor: {condition.SensorType}#{condition.SensorNumber}, " +
                                $"Type: {condition.ConditionType}, Value: {condition.ValueOff}");
            }
        }

        static void ExampleLoadFromLibrary()
        {
            Console.WriteLine("=== Example 2: Load Templates from Library ===\n");

            try
            {
                var library = new TemplateLibrary("../template_boards.json");
                library.LoadTemplates();

                Console.WriteLine($"Total templates loaded: {library.Count}\n");

                // List all templates
                Console.WriteLine("Available templates:");
                var names = library.GetTemplateNames();
                for (int i = 0; i < Math.Min(10, names.Count); i++)
                {
                    Console.WriteLine($"  {i + 1}. {names[i]}");
                }

                if (names.Count > 10)
                {
                    Console.WriteLine($"  ... and {names.Count - 10} more");
                }

                // Search for Sonoff devices
                Console.WriteLine("\nSearching for 'Sonoff' templates:");
                var sonoffTemplates = library.SearchTemplates("Sonoff");
                foreach (var template in sonoffTemplates.Take(5))
                {
                    Console.WriteLine($"  - {template.Name}");
                }

                // Get a specific template
                var gosundTemplate = library.GetTemplateByName("Gosund P1");
                if (gosundTemplate != null)
                {
                    Console.WriteLine($"\nGosund P1 template:");
                    Console.WriteLine($"  GPIO pins: {gosundTemplate.GPIO.Count}");
                    if (gosundTemplate.AnalogButtons != null)
                    {
                        Console.WriteLine($"  Analog buttons: {gosundTemplate.AnalogButtons.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading library: {ex.Message}");
            }
        }

        static void ExampleProcessShelly25()
        {
            Console.WriteLine("=== Example 3: Process Shelly 2.5 Template ===\n");

            try
            {
                var library = new TemplateLibrary("../template_boards.json");
                library.LoadTemplates();

                string jsonTemplate = library.GetTemplateJson("Shelly 2.5");
                var parser = new TemplateBoardParser();
                var config = parser.ParseTemplate(jsonTemplate);

                Console.WriteLine($"Processing: {config.HostName}\n");

                // Get configuration as dictionary
                var configDict = config.ToConfigDictionary();

                Console.WriteLine("Full configuration:");
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string configJson = JsonSerializer.Serialize(configDict, jsonOptions);
                Console.WriteLine(configJson);

                // Demonstrate GPIO mapping access
                Console.WriteLine("\n\nGPIO Function Mappings:");
                var gpioFunctions = (Dictionary<int, string>)configDict["GPIOFunctions"];
                foreach (var kvp in gpioFunctions.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"  GPIO {kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Example: Generate configuration for web interface
        /// </summary>
        public static string GenerateWebConfig(string templateName)
        {
            var library = new TemplateLibrary("template_boards.json");
            library.LoadTemplates();

            string jsonTemplate = library.GetTemplateJson(templateName);
            var parser = new TemplateBoardParser();
            var config = parser.ParseTemplate(jsonTemplate);

            var webConfig = new
            {
                success = true,
                deviceName = config.HostName,
                configuration = config.ToConfigDictionary(),
                warnings = parser.Warnings
            };

            return JsonSerializer.Serialize(webConfig, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        // add test for given json: {"NAME":"ESP-01-01S-DS18B20-v1.0","GPIO":[255,255,4,255,0,0,0,0,0,0,0,0,0],"FLAG":0,"BASE":18}
        
        /// <summary>
        /// Example: Test ESP-01-01S-DS18B20-v1.0 template parsing (old format with conversion)
        /// </summary>
        static void ExampleParseESP01DS18B20()
        {
            Console.WriteLine("=== Example: ESP-01-01S-DS18B20-v1.0 Template (Old Format) ===\n");

            string jsonTemplate = @"{
                ""NAME"": ""ESP-01-01S-DS18B20-v1.0"",
                ""GPIO"": [255, 255, 4, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0],
                ""FLAG"": 0,
                ""BASE"": 18
            }";

            var parser = new TemplateBoardParser();
            var config = parser.ParseTemplate(jsonTemplate);

            Console.WriteLine($"Device Name: {config.HostName}");
            Console.WriteLine($"Format Version: Old (13 GPIO elements)");
            Console.WriteLine($"Max DS18B20 Sensors: {config.MaxDS18B20}");
            Console.WriteLine($"GPIO 2 Function: {config.GetGPIO(2)}");
            Console.WriteLine($"Device Type: ESP-01 with DS18B20 Temperature Sensor");
            
            Console.WriteLine("\nWarnings:");
            foreach (var warning in parser.Warnings)
            {
                Console.WriteLine($"  - {warning}");
            }

            Console.WriteLine("\nConfiguration Summary:");
            Console.WriteLine($"  Relays: {config.MaxRelays}");
            Console.WriteLine($"  Buttons: {config.MaxButtons}");
            Console.WriteLine($"  LEDs: {config.MaxLeds}");
            Console.WriteLine($"  Temperature Sensors: {config.MaxDS18B20}");

            var configDict = config.ToConfigDictionary();
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            Console.WriteLine("\nFull Configuration JSON:");
            string configJson = JsonSerializer.Serialize(configDict, jsonOptions);
            Console.WriteLine(configJson);
        }

        /// <summary>
        /// Example: Test Gosund SP111 v1.1 template parsing (old format with conversion)
        /// </summary>
        static void ExampleParseGosundSP111()
        {
            Console.WriteLine("=== Example: Gosund SP111 v1.1 Smart Plug (Old Format) ===\n");

            string jsonTemplate = @"{
                ""NAME"": ""Gosund SP111 v1.1"",
                ""GPIO"": [56, 0, 158, 0, 132, 134, 0, 0, 131, 17, 0, 21, 0],
                ""FLAG"": 0,
                ""BASE"": 45
            }";

            var parser = new TemplateBoardParser();
            var config = parser.ParseTemplate(jsonTemplate);

            Console.WriteLine($"Device Name: {config.HostName}");
            Console.WriteLine($"Format Version: Old (13 GPIO elements)");
            Console.WriteLine($"Device Type: Smart Plug with Power Monitoring\n");

            Console.WriteLine("GPIO Code Conversion Examples:");
            Console.WriteLine("  Old code 56 ? New 352 (Led1i)");
            Console.WriteLine("  Old code 158 ? New 356 (LedLinki)");
            Console.WriteLine("  Old code 17 ? New 192 (Button1)");
            Console.WriteLine("  Old code 21 ? New 224 (Relay1)");
            Console.WriteLine("  Old code 132 ? New 2624 (HLWBL CF1)");
            Console.WriteLine("  Old code 134 ? New 2688 (BL0937 CF)");
            Console.WriteLine("  Old code 131 ? New 2720 (HLWBL SELi)\n");

            Console.WriteLine("Configuration Summary:");
            Console.WriteLine($"  Relays: {config.MaxRelays}");
            Console.WriteLine($"  Buttons: {config.MaxButtons}");
            Console.WriteLine($"  LEDs: {config.MaxLeds}");

            Console.WriteLine("\nPower Monitoring:");
            Console.WriteLine($"  GPIO 4 (CF1): {config.GetGPIO(4)}");
            Console.WriteLine($"  GPIO 5 (CF): {config.GetGPIO(5)}");
            Console.WriteLine($"  GPIO 12 (SEL): {config.GetGPIO(12)}");

            Console.WriteLine("\nRelays:");
            foreach (var relay in config.Relays)
            {
                Console.WriteLine($"  Relay {relay.Number}: GPIO {relay.GPIO}, Inverted: {relay.Inverted}");
            }

            Console.WriteLine("\nButtons:");
            foreach (var button in config.Buttons)
            {
                Console.WriteLine($"  Button {button.Number}: GPIO {button.GPIO}, Event: {button.EventType}, Inverted: {button.Inverted}");
            }

            Console.WriteLine("\nLEDs:");
            foreach (var led in config.Leds)
            {
                Console.WriteLine($"  LED {led.Number}: GPIO {led.GPIO}, Inverted: {led.Inverted}");
            }

            // NEW: Auto-select build flags based on template
            Console.WriteLine("\n=== Auto-Selected Build Flags ===");
            var selectedFlags = TemplateToBuildFlagsMapper.SelectBuildFlags(config);
            Console.WriteLine($"Automatically detected {selectedFlags.Count} required build flags:");
            foreach (var flag in selectedFlags.OrderBy(f => f))
            {
                Console.WriteLine($"  • {flag}");
            }

            // Show summary
            Console.WriteLine($"\n{TemplateToBuildFlagsMapper.GetConfigurationSummary(config)}");

            var configDict = config.ToConfigDictionary();
            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            Console.WriteLine("\nFull Configuration JSON:");
            string configJson = JsonSerializer.Serialize(configDict, jsonOptions);
            Console.WriteLine(configJson);
        }

        /// <summary>
        /// Example: Automatically create enabled BuildFlagItem list from template
        /// </summary>
        static void ExampleAutoSelectBuildFlagsFromTemplate()
        {
            Console.WriteLine("=== Example: Auto-Select Build Flags from Template ===\n");

            // Parse a template
            string jsonTemplate = @"{
                ""NAME"": ""Gosund SP111 v1.1"",
                ""GPIO"": [56, 0, 158, 0, 132, 134, 0, 0, 131, 17, 0, 21, 0],
                ""FLAG"": 0,
                ""BASE"": 45
            }";

            var parser = new TemplateBoardParser();
            var config = parser.ParseTemplate(jsonTemplate);

            Console.WriteLine($"Parsed Device: {config.HostName}\n");

            // Create a mock list of available build flags (normally loaded from builder.json)
            var availableFlags = new List<CompilationLib.BuildFlagItem>
            {
                new CompilationLib.BuildFlagItem { Key = "SUPLA_CONFIG", FlagName = "SUPLA_CONFIG", Description = "Configuration support" },
                new CompilationLib.BuildFlagItem { Key = "SUPLA_RELAY", FlagName = "SUPLA_RELAY", Description = "Relay support" },
                new CompilationLib.BuildFlagItem { Key = "SUPLA_BUTTON", FlagName = "SUPLA_BUTTON", Description = "Button support" },
                new CompilationLib.BuildFlagItem { Key = "SUPLA_LED", FlagName = "SUPLA_LED", Description = "LED support" },
                new CompilationLib.BuildFlagItem { Key = "SUPLA_HLW8012", FlagName = "SUPLA_HLW8012", Description = "HLW8012 power monitoring" },
                new CompilationLib.BuildFlagItem { Key = "SUPLA_DS18B20", FlagName = "SUPLA_DS18B20", Description = "DS18B20 temperature sensor" },
                new CompilationLib.BuildFlagItem { Key = "SUPLA_RGBW", FlagName = "SUPLA_RGBW", Description = "RGBW LED support" }
            };

            // Auto-select and create enabled build flags
            var enabledFlags = TemplateToBuildFlagsMapper.CreateEnabledBuildFlags(config, availableFlags);

            Console.WriteLine($"Automatically enabled {enabledFlags.Count} build flags:\n");
            foreach (var flag in enabledFlags)
            {
                Console.WriteLine($"  ? {flag.FlagName}");
                Console.WriteLine($"      {flag.Description}");
                Console.WriteLine($"      Enabled: {flag.IsEnabled}");
                Console.WriteLine();
            }

            Console.WriteLine("\nThese flags can now be used for compilation:");
            Console.WriteLine("var compileRequest = new CompileRequest");
            Console.WriteLine("{");
            Console.WriteLine("    BuildFlags = enabledFlags,");
            Console.WriteLine("    Platform = \"GUI_Generic_ESP32\",");
            Console.WriteLine("    ...");
            Console.WriteLine("};");
        }
    }
}
