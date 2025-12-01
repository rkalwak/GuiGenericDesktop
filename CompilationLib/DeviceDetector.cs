using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CompilationLib
{
    public class DeviceDetector
    {
        private EsptoolWrapper _esptoolWrapper;
        public DeviceDetector(EsptoolWrapper esptoolWrapper)
        {
            _esptoolWrapper = esptoolWrapper;
        }

        public string DetectCOMPort()
        {
            // Common keywords found in USB-to-UART adapters used by ESP32 boards
            var vendorKeywords = new[]
            {
                    "ESP", "ESP32", "CP210", "CP210x", "CH340", "CH341", "CH910", "FT232", "FTDI", "Silicon", "WCH", "Prolific", "usb serial"
                };

            // 1) Fast heuristic: list available serial ports
            var availablePorts = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            if (availablePorts.Length == 0)
                return (string?)null;

            // Platform-specific richer detection
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Query PnP entities that include "(COMn)" in the name
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        var name = (mo["Name"] as string) ?? string.Empty;
                        var deviceId = (mo["PNPDeviceID"] as string) ?? string.Empty;

                        // Extract the COM port token from the device name: "USB Serial Device (COM3)"
                        var m = Regex.Match(name, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
                        if (!m.Success) continue;
                        var port = m.Groups[1].Value;

                        // Filter by known vendor/driver keywords
                        if (vendorKeywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                   deviceId.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            // Ensure the port is in the available list
                            if (availablePorts.Contains(port, StringComparer.OrdinalIgnoreCase))
                                return port;
                        }
                    }
                }
                catch
                {
                    // If WMI isn't available or fails, fall back to simpler matching below.
                }
                return null;
            }

            return null;
        }

        /// <summary>
        /// Runs esptool.exe (--port {comPort} chip-id and flash-id), parses output and returns model + flash size.
        /// </summary>
        /// <param name="comPort">COM port to query (e.g. "COM3" or "/dev/ttyUSB0").</param>
        /// <returns>EspInfo with parsed values and raw output for diagnostics.</returns>
        public async Task<EspInfo> DetectEspModelAsync(string comPort, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(comPort))
                throw new ArgumentException("comPort must be provided", nameof(comPort));

            var chipExit = await _esptoolWrapper.ReadChipId(comPort, cancellationToken);

            var flashExit = await _esptoolWrapper.ReadFlashId(comPort, cancellationToken);

            // Prepare fields
            string? model = null;
            string? chipType = null;
            string? features = null;
            string? mac = null;
            string? manufacturer = null;
            string? device = null;
            string? flashSize = null;
            // Lines may contain the following patterns - use Regex to extract them robustly.
            var chipLineRegex = new Regex(@"Detecting chip type... (?<chiptype>[A-Z0-9]+)", RegexOptions.IgnoreCase);
            var featuresRegex = new Regex(@"Features:\s*(?<features>[^\r\n]+)", RegexOptions.IgnoreCase);
            var macRegex = new Regex(@"MAC:\s*(?<mac>[0-9a-fA-F:]{11,})", RegexOptions.IgnoreCase);
            var manufacturerRegex = new Regex(@"Manufacturer:\s*(?<man>[^\r\n]+)", RegexOptions.IgnoreCase);
            var deviceRegex = new Regex(@"Device:\s*(?<dev>[^\r\n]+)", RegexOptions.IgnoreCase);
            var flashRegex = new Regex(@"Detected flash size[:\s]+(?<size>\S+)", RegexOptions.IgnoreCase);
            var altFlashRegex = new Regex(@"Flash size[:\s]*?(?<size>\S+)", RegexOptions.IgnoreCase);
            var modelRegex = new Regex(@"Chip type: \s+(?<chip>[A-Z0-9-]+)", RegexOptions.IgnoreCase);


            var m2 = chipLineRegex.Match(chipExit.StdOut);
            if (m2.Success)
            {
                chipType = m2.Groups["chiptype"].Value.Trim();
            }

            var mf = featuresRegex.Match(chipExit.StdOut);
            if (mf.Success) features = mf.Groups["features"].Value.Trim();

            var mm = macRegex.Match(chipExit.StdOut);
            if (mm.Success) mac = mm.Groups["mac"].Value.Trim();

            var manu = manufacturerRegex.Match(chipExit.StdOut);
            if (manu.Success) manufacturer = manu.Groups["man"].Value.Trim();

            var devm = deviceRegex.Match(chipExit.StdOut);
            if (devm.Success) device = devm.Groups["dev"].Value.Trim();

            var fm = flashRegex.Match(chipExit.StdOut);
            if (fm.Success) flashSize = fm.Groups["size"].Value.Trim();
            else
            {
                var af = altFlashRegex.Match(chipExit.StdOut);
                if (af.Success) flashSize = af.Groups["size"].Value.Trim();
            }

            var mm2 = modelRegex.Match(chipExit.StdOut);
            if (mm2.Success) model = mm2.Groups["chip"].Value.Trim();

            // determine recognition heuristics
            var isRecognized = false;
            if (!string.IsNullOrEmpty(model) && model.IndexOf("ESP", StringComparison.OrdinalIgnoreCase) >= 0) isRecognized = true;
            if (!string.IsNullOrEmpty(chipType) && chipType.IndexOf("ESP", StringComparison.OrdinalIgnoreCase) >= 0) isRecognized = true;

            var info = new EspInfo
            {
                IsRecognized = isRecognized,
                Model = model,
                ChipType = chipType,
                Features = features,
                Mac = mac,
                Manufacturer = manufacturer,
                Device = device,
                FlashSize = flashSize,
                Message = isRecognized ? "ESP device detected." : "Model not detected."
            };

            return info;
        }
    }
}
