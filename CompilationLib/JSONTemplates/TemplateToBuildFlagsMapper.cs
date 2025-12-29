using System.Collections.Generic;
using System.Linq;
using SuplaTemplateBoard.Models;

namespace SuplaTemplateBoard
{
    /// <summary>
    /// Automatically selects build flags based on template configuration
    /// </summary>
    public static class TemplateToBuildFlagsMapper
    {
        /// <summary>
        /// Selects appropriate build flags based on parsed template configuration
        /// </summary>
        /// <param name="config">Device configuration from parsed template</param>
        /// <param name="availableFlags">List of all available build flags (from builder.json)</param>
        /// <returns>List of build flag keys that should be enabled</returns>
        public static List<string> SelectBuildFlags(DeviceConfiguration config, List<CompilationLib.BuildFlagItem> availableFlags = null)
        {
            if (config == null)
                return new List<string>();

            var selectedFlags = new HashSet<string>();

            // Core flags that are typically always needed
            selectedFlags.Add("SUPLA_CONFIG");

            // Relays
            if (config.MaxRelays > 0)
            {
                selectedFlags.Add("SUPLA_RELAY");
            }

            // Buttons
            if (config.MaxButtons > 0)
            {
                selectedFlags.Add("SUPLA_BUTTON");
            }

            // LEDs
            if (config.MaxLeds > 0)
            {
                selectedFlags.Add("SUPLA_LED");
            }

            // RGBW channels
            if (config.MaxRGBW > 0)
            {
                selectedFlags.Add("SUPLA_RGBW");
            }

            // Limit switches (binary sensors)
            if (config.MaxLimitSwitches > 0)
            {
                selectedFlags.Add("SUPLA_LIMIT_SWITCH");
            }

            // Conditions
            if (config.MaxConditions > 0)
            {
                selectedFlags.Add("SUPLA_CONDITIONS");
            }

            // DS18B20 temperature sensor
            if (config.MaxDS18B20 > 0)
            {
                selectedFlags.Add("SUPLA_DS18B20");
            }

            // Check GPIO functions for specific sensors/modules
            var gpioDict = config.ToConfigDictionary();
            if (gpioDict.ContainsKey("GPIOFunctions"))
            {
                var gpioFunctions = gpioDict["GPIOFunctions"] as Dictionary<int, string>;
                if (gpioFunctions != null)
                {
                    foreach (var gpio in gpioFunctions.Values)
                    {
                        MapGpioFunctionToFlag(gpio, selectedFlags);
                    }
                }
            }

            // I2C Expanders
            if (config.Expanders.Any(e => e.Type == "MCP23017"))
            {
                selectedFlags.Add("SUPLA_MCP23017");
            }
            if (config.Expanders.Any(e => e.Type == "PCF8574"))
            {
                selectedFlags.Add("SUPLA_PCF8574");
            }
            if (config.Expanders.Any(e => e.Type == "PCF8575"))
            {
                selectedFlags.Add("SUPLA_PCF8575");
            }

            return selectedFlags.ToList();
        }

        /// <summary>
        /// Creates enabled BuildFlagItem list from selected flag keys
        /// </summary>
        /// <param name="config">Device configuration from parsed template</param>
        /// <param name="availableFlags">List of all available build flags (from builder.json)</param>
        /// <returns>List of enabled BuildFlagItem objects</returns>
        public static List<CompilationLib.BuildFlagItem> CreateEnabledBuildFlags(
            DeviceConfiguration config, 
            List<CompilationLib.BuildFlagItem> availableFlags)
        {
            if (config == null || availableFlags == null)
                return new List<CompilationLib.BuildFlagItem>();

            var selectedFlagKeys = SelectBuildFlags(config, availableFlags);
            var enabledFlags = new List<CompilationLib.BuildFlagItem>();

            foreach (var flagKey in selectedFlagKeys)
            {
                var flag = availableFlags.FirstOrDefault(f => 
                    string.Equals(f.Key, flagKey, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.FlagName, flagKey, System.StringComparison.OrdinalIgnoreCase));

                if (flag != null)
                {
                    // Create a copy to avoid modifying the original
                    var enabledFlag = new CompilationLib.BuildFlagItem
                    {
                        Key = flag.Key,
                        FlagName = flag.FlagName,
                        Description = flag.Description,
                        Section = flag.Section,
                        IsEnabled = true,
                        EnabledByFlags = flag.EnabledByFlags,
                        DependenciesToDisable = flag.DependenciesToDisable,
                        DependenciesToEnable = flag.DependenciesToEnable,
                        BlockedByDisabledFlags = flag.BlockedByDisabledFlags,
                        Parameters = flag.Parameters?.Select(p => new CompilationLib.Parameter
                        {
                            Name = p.Name,
                            Type = p.Type,
                            DefaultValue = p.DefaultValue,
                            Value = p.Value,
                            IsRequired = p.IsRequired,
                            EnumValues = p.EnumValues
                        }).ToList()
                    };
                    enabledFlags.Add(enabledFlag);
                }
            }

            return enabledFlags;
        }

        /// <summary>
        /// Maps GPIO function strings to build flag names
        /// </summary>
        private static void MapGpioFunctionToFlag(string gpioFunction, HashSet<string> selectedFlags)
        {
            if (string.IsNullOrEmpty(gpioFunction))
                return;

            var function = gpioFunction.ToUpperInvariant();

            // I2C
            if (function.Contains("SCL") || function.Contains("SDA"))
            {
                // I2C is often used with expanders or sensors, flag may vary
                // Could add SUPLA_MCP23017, SUPLA_PCF8574, etc. based on specific needs
            }

            // Power monitoring
            if (function.Contains("CF") || function.Contains("SEL"))
            {
                selectedFlags.Add("SUPLA_HLW8012");
            }

            // Sensors
            if (function.Contains("DS18B20"))
            {
                selectedFlags.Add("SUPLA_DS18B20");
            }
            if (function.Contains("SI7021"))
            {
                selectedFlags.Add("SUPLA_SI7021_SONOFF");
            }
            if (function.Contains("CSE7766"))
            {
                selectedFlags.Add("SUPLA_CSE7766");
            }
            if (function.Contains("NTC_10K") || function.Contains("NTC"))
            {
                selectedFlags.Add("SUPLA_NTC_10K");
            }
            if (function.Contains("ADE7953"))
            {
                selectedFlags.Add("SUPLA_ADE7953");
            }

            // RGBW
            if (function.Contains("RGBW"))
            {
                selectedFlags.Add("SUPLA_RGBW");
            }
        }

        /// <summary>
        /// Gets a summary of detected features from the configuration
        /// </summary>
        public static string GetConfigurationSummary(DeviceConfiguration config)
        {
            if (config == null)
                return "No configuration available";

            var features = new List<string>();

            if (config.MaxRelays > 0)
                features.Add($"{config.MaxRelays} relay(s)");
            if (config.MaxButtons > 0)
                features.Add($"{config.MaxButtons} button(s)");
            if (config.MaxLeds > 0)
                features.Add($"{config.MaxLeds} LED(s)");
            if (config.MaxRGBW > 0)
                features.Add($"{config.MaxRGBW} RGBW channel(s)");
            if (config.MaxLimitSwitches > 0)
                features.Add($"{config.MaxLimitSwitches} limit switch(es)");
            if (config.MaxConditions > 0)
                features.Add($"{config.MaxConditions} condition(s)");
            if (config.MaxDS18B20 > 0)
                features.Add($"{config.MaxDS18B20} DS18B20 sensor(s)");
            if (config.AnalogButtons.Count > 0)
                features.Add($"{config.AnalogButtons.Count} analog button(s)");
            if (config.Expanders.Count > 0)
                features.Add($"{config.Expanders.Count} expander(s)");

            return features.Count > 0 
                ? $"{config.HostName}: {string.Join(", ", features)}"
                : $"{config.HostName}: No features detected";
        }
    }
}
