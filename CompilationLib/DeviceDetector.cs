using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace CompilationLib
{
    public class DeviceDetector
    {
        private readonly IEsptoolWrapper _esptoolWrapper;
        private readonly ILogger _logger;
        
        public DeviceDetector(IEsptoolWrapper esptoolWrapper)
        {
            _esptoolWrapper = esptoolWrapper;
            _logger = Log.ForContext<DeviceDetector>();
        }

        public string DetectCOMPort()
        {
            _logger.Debug("Starting COM port detection");
            
            // Common keywords found in USB-to-UART adapters used by ESP32 boards
            var vendorKeywords = new[]
            {
                    "ESP", "ESP32", "CP210", "CP210x", "CH340", "CH341", "CH343", "CH910", "FT232", "FTDI", "Silicon", "WCH", "Prolific", "usb serial"
                };

            // 1) Fast heuristic: list available serial ports
            var availablePorts = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            _logger.Debug("Available serial ports: {Ports}", string.Join(", ", availablePorts));
            
            if (availablePorts.Length == 0)
            {
                _logger.Warning("No serial ports found on the system");
                return null;
            }

            // Platform-specific richer detection
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.Debug("Running on Windows platform, using WMI for device detection");
                try
                {
                    // Query PnP entities that include "(COMn)" in the name
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                    var deviceCount = 0;
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        deviceCount++;
                        var name = (mo["Name"] as string) ?? string.Empty;
                        var deviceId = (mo["PNPDeviceID"] as string) ?? string.Empty;

                        // Extract the COM port token from the device name: "USB Serial Device (COM3)"
                        var m = Regex.Match(name, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
                        if (!m.Success) continue;
                        var port = m.Groups[1].Value;

                        _logger.Debug("Found device: {Name}, Device ID: {DeviceId}, Port: {Port}", name, deviceId, port);
                        
                        // Filter by known vendor/driver keywords
                        if (vendorKeywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                   deviceId.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            // Ensure the port is in the available list
                            if (availablePorts.Contains(port, StringComparer.OrdinalIgnoreCase))
                            {
                                _logger.Information("ESP device detected on port: {Port}, Device: {Name}", port, name);
                                return port;
                            }
                        }
                    }
                    _logger.Debug("Scanned {DeviceCount} COM devices via WMI, no ESP device found", deviceCount);
                }
                catch (Exception ex)
                {
                    // If WMI isn't available or fails, fall back to simpler matching below.
                    _logger.Warning(ex, "WMI query failed, unable to detect devices via Windows Management");
                }
                _logger.Warning("No ESP-compatible COM port detected");
                return null;
            }

            _logger.Debug("Not running on Windows, COM port detection not implemented for this platform");
            return null;
        }

        /// <summary>
        /// Runs esptool.exe (--port {comPort} chip-id and flash-id), parses output and returns model + flash size.
        /// </summary>
        /// <param name="comPort">COM port to query (e.g. "COM3" or "/dev/ttyUSB0").</param>
        /// <returns>EspInfo with parsed values and raw output for diagnostics.</returns>
        public async Task<EspInfo> DetectEspModelAsync(string comPort, CancellationToken cancellationToken = default)
        {
            _logger.Information("Starting ESP device detection on port: {ComPort}", comPort);
            
            if (string.IsNullOrWhiteSpace(comPort))
            {
                _logger.Error("COM port parameter is null or empty");
                throw new ArgumentException("comPort must be provided", nameof(comPort));
            }

            _logger.Debug("Reading chip ID from {ComPort}", comPort);
            var chipExit = await _esptoolWrapper.ReadChipId(comPort, cancellationToken);
            _logger.Debug("Chip ID read completed. Exit code: {ExitCode}, Success: {Success}", chipExit.ExitCode, chipExit.Success);

            _logger.Debug("Reading flash ID from {ComPort}", comPort);
            var flashExit = await _esptoolWrapper.ReadFlashId(comPort, cancellationToken);
            _logger.Debug("Flash ID read completed. Exit code: {ExitCode}, Success: {Success}", flashExit.ExitCode, flashExit.Success);

            // Prepare fields
            string model = null;
            string chipType = null;
            string features = null;
            string mac = null;
            string manufacturer = null;
            string device = null;
            string flashSize = null;
            
            // Lines may contain the following patterns - use Regex to extract them robustly.
            var chipLineRegex = new Regex(@"Detecting chip type... (?<chiptype>[A-Z0-9\- ]+)", RegexOptions.IgnoreCase);
            var featuresRegex = new Regex(@"Features:\s*(?<features>[^\r\n]+)", RegexOptions.IgnoreCase);
            var macRegex = new Regex(@"MAC:\s*(?<mac>[0-9a-fA-F:]{11,})", RegexOptions.IgnoreCase);
            var manufacturerRegex = new Regex(@"Manufacturer:\s*(?<man>[^\r\n]+)", RegexOptions.IgnoreCase);
            var deviceRegex = new Regex(@"Device:\s*(?<dev>[^\r\n]+)", RegexOptions.IgnoreCase);
            var flashRegex = new Regex(@".*size[:\s]+?(?<size>[a-zA-Z0-9]{1,2}MB)", RegexOptions.IgnoreCase);
            var modelRegex = new Regex(@"Chip type:\s*(?<chip>[A-Z0-9\- ]+)", RegexOptions.IgnoreCase);


            var m2 = chipLineRegex.Match(chipExit.StdOut ?? string.Empty);
            if (m2.Success)
            {
                chipType = m2.Groups["chiptype"].Value.Trim();
                _logger.Debug("Detected chip type: {ChipType}", chipType);
            }

            var mf = featuresRegex.Match(chipExit.StdOut ?? string.Empty);
            if (mf.Success)
            {
                features = mf.Groups["features"].Value.Trim();
                _logger.Debug("Detected features: {Features}", features);
            }

            var mm = macRegex.Match(chipExit.StdOut ?? string.Empty);
            if (mm.Success)
            {
                mac = mm.Groups["mac"].Value.Trim();
                _logger.Debug("Detected MAC address: {Mac}", mac);
            }

            var manu = manufacturerRegex.Match(flashExit.StdOut ?? string.Empty);
            if (manu.Success)
            {
                manufacturer = manu.Groups["man"].Value.Trim();
                _logger.Debug("Detected manufacturer: {Manufacturer}", manufacturer);
            }

            var devm = deviceRegex.Match(flashExit.StdOut ?? string.Empty);
            if (devm.Success)
            {
                device = devm.Groups["dev"].Value.Trim();
                _logger.Debug("Detected device: {Device}", device);
            }

            var fm = flashRegex.Match(flashExit.StdOut ?? string.Empty);
            if (fm.Success)
            {
                flashSize = fm.Groups["size"].Value.Trim();
                _logger.Debug("Detected flash size: {FlashSize}", flashSize);
            }

            var mm2 = modelRegex.Match(chipExit.StdOut ?? string.Empty);
            if (mm2.Success)
            {
                model = mm2.Groups["chip"].Value.Trim();
                _logger.Debug("Detected model: {Model}", model);
            }

            // determine recognition heuristics
            var isRecognized = false;
            if (!string.IsNullOrEmpty(model) && model.IndexOf("ESP", StringComparison.OrdinalIgnoreCase) >= 0) isRecognized = true;
            if (!string.IsNullOrEmpty(chipType) && chipType.IndexOf("ESP", StringComparison.OrdinalIgnoreCase) >= 0) isRecognized = true;

            if (isRecognized)
            {
                _logger.Information("ESP device successfully detected. Model: {Model}, Chip: {ChipType}, Flash: {FlashSize}, MAC: {Mac}", 
                    model, chipType, flashSize, mac);
            }
            else
            {
                _logger.Warning("Device detection completed but ESP model not recognized. ChipType: {ChipType}, Model: {Model}", 
                    chipType, model);
            }
            
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
                Message = isRecognized ? "ESP device detected." : "Model not detected.",
                RawOutput = (chipExit.StdOut ?? string.Empty) + "\n" + (flashExit.StdOut ?? string.Empty),
                RawError = (chipExit.StdErr ?? string.Empty) + "\n" + (flashExit.StdErr ?? string.Empty)
            };

            return info;
        }
    }
}

/*
 * esptool v5.1.0
Serial port COM3:
Connecting...................
Detecting chip type... ESP32
Connected to ESP32 on COM3:
Chip type:          ESP32-D0WD-V3 (revision v3.1)
Features:           Wi-Fi, BT, Dual Core + LP Core, 240MHz, Vref calibration in eFuse, Coding Scheme None
Crystal frequency:  40MHz
MAC:                c4:d8:d5:96:1f:30

Uploading stub flasher...
Running stub flasher...
Stub flasher running.

Warning: ESP32 has no chip ID. Reading MAC address instead.
MAC:                c4:d8:d5:96:1f:30

Hard resetting via RTS pin...

esptool v5.1.0
Serial port COM3:
Connecting....
Detecting chip type... ESP32
Connected to ESP32 on COM3:
Chip type:          ESP32-D0WD-V3 (revision v3.1)
Features:           Wi-Fi, BT, Dual Core + LP Core, 240MHz, Vref calibration in eFuse, Coding Scheme None
Crystal frequency:  40MHz
MAC:                c4:d8:d5:96:1f:30

Uploading stub flasher...
Running stub flasher...
Stub flasher running.

Flash Memory Information:
=========================
Manufacturer: 68
Device: 4016
Detected flash size: 4MB
Flash voltage set by a strapping pin: 3.3V

Hard resetting via RTS pin...


c3
[08:59:57 DBG] Starting COM port detection
[08:59:57 DBG] Available serial ports: COM1
[08:59:57 DBG] Running on Windows platform, using WMI for device detection
[08:59:57 DBG] Found device: Port komunikacyjny (COM1), Device ID: ACPI\PNP0501\0, Port: COM1
[08:59:57 DBG] Scanned 1 COM devices via WMI, no ESP device found
[08:59:57 WRN] No ESP-compatible COM port detected
[08:59:57 WRN] No COM port detected
[08:59:57 WRN] Device detection completed but no port found
[09:00:00 INF] === Device Detection Completed ===
[09:00:04 INF] === Device Detection Started ===
[09:00:04 DBG] Detecting COM port...
[09:00:04 DBG] Starting COM port detection
[09:00:04 DBG] Available serial ports: COM1, COM6
[09:00:04 DBG] Running on Windows platform, using WMI for device detection
[09:00:04 DBG] Found device: Port komunikacyjny (COM1), Device ID: ACPI\PNP0501\0, Port: COM1
[09:00:04 DBG] Found device: USB-SERIAL CH340 (COM6), Device ID: USB\VID_1A86&PID_7523\8&129DF91D&0&8, Port: COM6
[09:00:04 INF] ESP device detected on port: COM6, Device: USB-SERIAL CH340 (COM6)
[09:00:04 INF] COM port detected: COM6
[09:00:04 DBG] Detecting ESP model on port COM6...
[09:00:04 INF] Starting ESP device detection on port: COM6
[09:00:04 DBG] Reading chip ID from COM6
'GuiGenericBuilderDesktop.exe' (CoreCLR: clrhost): Loaded 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.1\System.Console.dll'. Skipped loading symbols. Module is optimized and the debugger option 'Just My Code' is enabled.
[09:00:05 DBG] Chip ID read completed. Exit code: 2, Success: false
[09:00:05 DBG] Reading flash ID from COM6
[09:00:05 DBG] Flash ID read completed. Exit code: 2, Success: false
[09:00:05 WRN] Device detection completed but ESP model not recognized. ChipType: null, Model: null
[09:00:05 INF] Device detected: ChipType=null, Model=null, FlashSize=null, MAC=null
[09:00:05 DBG] COM port selector updated to: COM6
[09:00:05 INF] === Device Detection Completed ===

*/