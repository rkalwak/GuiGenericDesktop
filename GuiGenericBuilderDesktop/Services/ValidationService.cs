using CompilationLib;
using GuiGenericBuilderDesktop.Localization;
using Serilog;
using System.Runtime.InteropServices;

namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Handles validation logic for platform compatibility, I2C parameters, and other build-related validations
    /// </summary>
    public class ValidationService
    {
        private readonly ILogger _logger;
        private static bool _platformioWarningShown = false;

        public ValidationService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Disables flags that are incompatible with the specified platform
        /// </summary>
        public List<string> DisableIncompatibleFlags(string platformTag, List<BuildFlagItem> allFlags)
        {
            if (string.IsNullOrEmpty(platformTag))
                return new List<string>();

            var disabledFlags = new List<string>();

            foreach (var flag in allFlags)
            {
                if (flag.DisabledOnPlatforms != null &&
                    flag.DisabledOnPlatforms.Any(p => string.Equals(p, platformTag, StringComparison.OrdinalIgnoreCase)))
                {
                    if (flag.IsEnabled)
                    {
                        flag.IsEnabled = false;
                        disabledFlags.Add(flag.GetLocalizedName());
                        _logger.Information("Disabled flag {Flag} - incompatible with platform {Platform}", flag.Key, platformTag);
                    }
                }
            }

            return disabledFlags;
        }

        /// <summary>
        /// Validates that all enabled flags are compatible with the selected platform
        /// </summary>
        public List<string> ValidatePlatformCompatibility(string platformTag, List<BuildFlagItem> enabledFlags)
        {
            var incompatibleFlags = new List<string>();

            if (string.IsNullOrEmpty(platformTag) || platformTag.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                // No platform selected, skip validation
                return incompatibleFlags;
            }

            foreach (var flag in enabledFlags)
            {
                if (flag.DisabledOnPlatforms != null &&
                    flag.DisabledOnPlatforms.Any(p => string.Equals(p, platformTag, StringComparison.OrdinalIgnoreCase)))
                {
                    incompatibleFlags.Add($"{flag.GetLocalizedName()} (disabled on {platformTag})");
                    _logger.Warning("Flag {Flag} is incompatible with platform {Platform}", flag.Key, platformTag);
                }
            }

            return incompatibleFlags;
        }

        /// <summary>
        /// Validates that all I2C devices have the same SCL and SDA parameter values
        /// </summary>
        public string ValidateI2CParameters(List<BuildFlagItem> enabledFlags)
        {
            string expectedScl = null;
            string expectedSda = null;
            var i2cDevices = new List<string>();

            foreach (var flag in enabledFlags)
            {
                if (flag.Parameters == null || !flag.Parameters.Any())
                    continue;

                // Check if this flag has SCL and SDA parameters (I2C device)
                var sclParam = flag.Parameters.FirstOrDefault(p =>
                    string.Equals(p.Identifier, "SCL", StringComparison.OrdinalIgnoreCase));
                var sdaParam = flag.Parameters.FirstOrDefault(p =>
                    string.Equals(p.Identifier, "SDA", StringComparison.OrdinalIgnoreCase));

                if (sclParam != null && sdaParam != null)
                {
                    // This is an I2C device
                    var currentScl = sclParam.Value?.Trim();
                    var currentSda = sdaParam.Value?.Trim();

                    // Skip if values are not set
                    if (string.IsNullOrEmpty(currentScl) || string.IsNullOrEmpty(currentSda))
                        continue;

                    if (expectedScl == null && expectedSda == null)
                    {
                        // First I2C device found - set expected values
                        expectedScl = currentScl;
                        expectedSda = currentSda;
                        i2cDevices.Add($"{flag.GetLocalizedName()} (SCL={currentScl}, SDA={currentSda})");
                    }
                    else
                    {
                        // Validate against expected values
                        if (!string.Equals(currentScl, expectedScl, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(currentSda, expectedSda, StringComparison.OrdinalIgnoreCase))
                        {
                            i2cDevices.Add($"{flag.GetLocalizedName()} (SCL={currentScl}, SDA={currentSda})");

                            // Build error message
                            var errorMessage = "All I2C devices must use the same SCL and SDA pins.\n\n" +
                                             "Conflicting configurations detected:\n\n" +
                                             string.Join("\n", i2cDevices.Select(d => $"� {d}")) +
                                             "\n\nPlease ensure all I2C devices have matching SCL and SDA values.";

                            _logger.Warning("I2C parameter mismatch detected: {Devices}", string.Join(", ", i2cDevices));
                            return errorMessage;
                        }
                        else
                        {
                            i2cDevices.Add($"{flag.GetLocalizedName()} (SCL={currentScl}, SDA={currentSda})");
                        }
                    }
                }
            }

            // All I2C devices have consistent parameters (or no I2C devices found)
            if (i2cDevices.Any())
            {
                _logger.Information("I2C validation passed: {Count} devices with SCL={SCL}, SDA={SDA}",
                    i2cDevices.Count, expectedScl, expectedSda);
            }

            return null; // No errors
        }

        /// <summary>
        /// Validates that PlatformIO is installed in the default location
        /// Returns true if PlatformIO is found, false otherwise
        /// </summary>
        public bool ValidatePlatformIOInstallation()
        {
            try
            {
                var platformioPath = GetPlatformIOPath();
                
                if (!System.IO.File.Exists(platformioPath))
                {
                    _logger.Warning("PlatformIO executable not found at: {Path}", platformioPath);
                    return false;
                }
                
                _logger.Information("PlatformIO found at: {Path}", platformioPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error checking PlatformIO installation");
                return false;
            }
        }

        /// <summary>
        /// Gets the expected PlatformIO executable path based on the operating system
        /// </summary>
        private string GetPlatformIOPath()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return System.IO.Path.Combine(userProfile, ".platformio", "penv", "Scripts", "platformio.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return System.IO.Path.Combine(userProfile, ".platformio", "penv", "bin", "platformio");
            }
            else
            {
                // Fallback for unknown platforms - try Linux-style path
                return System.IO.Path.Combine(userProfile, ".platformio", "penv", "bin", "platformio");
            }
        }

        /// <summary>
        /// Shows a warning message if PlatformIO is not installed (only once per session)
        /// </summary>
        public void ShowPlatformIOWarningIfNeeded()
        {
            if (_platformioWarningShown)
                return;

            if (!ValidatePlatformIOInstallation())
            {
                _platformioWarningShown = true;
                var platformioPath = GetPlatformIOPath();
                
                System.Windows.MessageBox.Show(
                    LocalizationManager.GetFormat("PlatformIONotFoundMessage", platformioPath),
                    LocalizationManager.Get("PlatformIONotFoundTitle"),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
    }
}
