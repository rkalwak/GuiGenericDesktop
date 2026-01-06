using CompilationLib;
using Serilog;
using System.Windows.Controls;

namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Handles device detection and management operations
    /// </summary>
    public class DeviceManagementService
    {
        private readonly DeviceDetector _deviceDetector;
        private readonly ILogger _logger;

        public DeviceManagementService(DeviceDetector deviceDetector, ILogger logger)
        {
            _deviceDetector = deviceDetector;
            _logger = logger;
        }

        /// <summary>
        /// Detects connected ESP device and returns device information
        /// </summary>
        public async Task<(string Port, EspInfo DeviceInfo)> DetectDeviceAsync()
        {
            _logger.Information("=== Device Detection Started ===");
            
            _logger.Debug("Detecting COM port...");
            var port = _deviceDetector.DetectCOMPortWithUsbBridge();

            if (port != null)
            {
                _logger.Information("COM port detected: {Port}", port);
            }
            else
            {
                _logger.Warning("No COM port detected");
                return (null, null);
            }

            EspInfo deviceModel = null;
            _logger.Debug("Detecting ESP model on port {Port}...", port);
            deviceModel = await _deviceDetector.DetectEspModelAsync(port);

            if (deviceModel != null)
            {
                _logger.Information("Device detected: ChipType={ChipType}, Model={Model}, FlashSize={FlashSize}, MAC={Mac}",
                    deviceModel.ChipType, deviceModel.Model, deviceModel.FlashSize, deviceModel.Mac);
            }

            _logger.Information("=== Device Detection Completed ===");
            return (port, deviceModel);
        }

        /// <summary>
        /// Maps ESP chip type to platform tag
        /// </summary>
        public string GetPlatformTagFromChip(string chipType)
        {
            if (string.IsNullOrEmpty(chipType))
                return null;

            var chipLower = chipType.ToLowerInvariant();
            
            if (chipLower.Contains("c6") || chipLower.Contains("c-6"))
                return "GUI_Generic_ESP32C6";
            else if (chipLower.Contains("c3") || chipLower.Contains("c-3"))
                return "GUI_Generic_ESP32C3";
            else if (chipLower.Contains("s3") || chipLower.Contains("s-3"))
                return "GUI_Generic_ESP32S3";
            else if (chipLower.Contains("esp32"))
                return "GUI_Generic_ESP32";

            return null;
        }

        /// <summary>
        /// Finds ComboBoxItem by tag value
        /// </summary>
        public ComboBoxItem FindComboBoxItemByTag(ComboBox comboBox, string tagValue)
        {
            return comboBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(ci => string.Equals(ci.Tag as string, tagValue, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(ci.Content as string, tagValue, StringComparison.OrdinalIgnoreCase));
        }
    }
}
