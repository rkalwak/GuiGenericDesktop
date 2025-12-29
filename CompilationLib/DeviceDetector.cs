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
        /// Detects microcontroller devices by USB Bridge VID/PID identifiers.
        /// This method can detect devices even if COM port drivers are not fully loaded.
        /// Uses the comprehensive USB device database from UsbDeviceRecognition.
        /// </summary>
        /// <returns>COM port name if detected, null otherwise</returns>
        public string DetectByUsbBridge()
        {
            _logger.Debug("Starting USB Bridge detection using UsbDeviceRecognition database");
            
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.Debug("USB Bridge detection not implemented for non-Windows platforms");
                return null;
            }

            try
            {
                // Query all USB devices
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB\\\\VID_%'");
                var detectedDevices = new List<DetectedUsbDevice>();
                
                foreach (ManagementObject mo in searcher.Get())
                {
                    var deviceId = (mo["PNPDeviceID"] as string) ?? string.Empty;
                    var name = (mo["Name"] as string) ?? string.Empty;
                    
                    // Parse VID and PID from device ID
                    // Format: USB\VID_1A86&PID_7523\...
                    var vidMatch = Regex.Match(deviceId, @"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    var pidMatch = Regex.Match(deviceId, @"PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    
                    if (!vidMatch.Success || !pidMatch.Success)
                        continue;
                    
                    var vid = Convert.ToInt32(vidMatch.Groups[1].Value, 16);
                    var pid = Convert.ToInt32(pidMatch.Groups[1].Value, 16);
                    
                    // Check if this is a supported vendor
                    if (!UsbDeviceRecognition.IsVendorSupported(vid))
                        continue;
                    
                    // Get device information from the database
                    var deviceInfo = UsbDeviceRecognition.GetUsbDeviceInfo(vid, pid);
                    
                    if (deviceInfo != null)
                    {
                        var description = UsbDeviceRecognition.GetDeviceDescription(vid, pid);
                        _logger.Debug("Found supported USB bridge: {Description}, Device: {Name}, ID: {DeviceId}", 
                            description, name, deviceId);
                        
                        // Try to extract COM port from the device name
                        var portMatch = Regex.Match(name, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
                        if (portMatch.Success)
                        {
                            var port = portMatch.Groups[1].Value;
                            detectedDevices.Add(new DetectedUsbDevice
                            {
                                Port = port,
                                VendorId = vid,
                                ProductId = pid,
                                VendorName = deviceInfo.VendorName,
                                ProductName = deviceInfo.ProductName ?? "Unknown Product",
                                MaxBaudrate = deviceInfo.MaxBaudrate ?? UsbDeviceRecognition.DefaultFlashBaud,
                                DeviceId = deviceId,
                                DeviceName = name
                            });
                            
                            _logger.Information("USB Bridge detected: {VendorName} {ProductName} on {Port} (Max baudrate: {MaxBaudrate:N0})", 
                                deviceInfo.VendorName, deviceInfo.ProductName, port, deviceInfo.MaxBaudrate);
                        }
                        else
                        {
                            // Device found but COM port not yet assigned or visible
                            _logger.Debug("USB Bridge {VendorName} {ProductName} found but COM port not assigned: {Name}", 
                                deviceInfo.VendorName, deviceInfo.ProductName, name);
                        }
                    }
                }
                
                // Return the first detected device with a valid COM port
                // Prioritize by vendor: Espressif > Silicon Labs > FTDI > Others
                if (detectedDevices.Any())
                {
                    var espressifDevice = detectedDevices.FirstOrDefault(d => d.VendorId == 0x303a);
                    var selectedDevice = espressifDevice ?? detectedDevices.First();
                    
                    _logger.Information("Selecting USB Bridge device: {VendorName} {ProductName} on {Port} (VID:0x{VendorId:X4} PID:0x{ProductId:X4})", 
                        selectedDevice.VendorName, selectedDevice.ProductName, selectedDevice.Port, 
                        selectedDevice.VendorId, selectedDevice.ProductId);
                    
                    return selectedDevice.Port;
                }
                
                _logger.Debug("No supported USB Bridge devices detected");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during USB Bridge detection");
                return null;
            }
        }

        /// <summary>
        /// Combined detection method that tries both USB Bridge and standard COM port detection.
        /// First attempts USB Bridge detection for more reliable identification, then falls back to standard detection.
        /// </summary>
        /// <returns>COM port name if detected, null otherwise</returns>
        public string DetectCOMPortWithUsbBridge()
        {
            _logger.Debug("Starting combined COM port detection (USB Bridge + Standard)");
            
            // First try USB Bridge detection (more reliable)
            var usbBridgePort = DetectByUsbBridge();
            if (!string.IsNullOrEmpty(usbBridgePort))
            {
                _logger.Information("Device detected via USB Bridge: {Port}", usbBridgePort);
                return usbBridgePort;
            }
            
            // Fall back to standard detection
            _logger.Debug("USB Bridge detection found no devices, falling back to standard detection");
            var standardPort = DetectCOMPort();
            if (!string.IsNullOrEmpty(standardPort))
            {
                _logger.Information("Device detected via standard detection: {Port}", standardPort);
                return standardPort;
            }
            
            _logger.Warning("No device detected by either USB Bridge or standard detection");
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

        /// <summary>
        /// Internal class to hold detected USB device information
        /// </summary>
        private class DetectedUsbDevice
        {
            public string Port { get; set; }
            public int VendorId { get; set; }
            public int ProductId { get; set; }
            public string VendorName { get; set; }
            public string ProductName { get; set; }
            public int MaxBaudrate { get; set; }
            public string DeviceId { get; set; }
            public string DeviceName { get; set; }
        }
    }
}
