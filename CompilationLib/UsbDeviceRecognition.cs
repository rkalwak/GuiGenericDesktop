namespace CompilationLib
{
    /// <summary>
    /// Handles USB device recognition based on USB Bridge capabilities for ESP32 devices
    /// </summary>
    public class UsbDeviceRecognition
    {
        // Connection timeout in milliseconds
        public const int TimeoutConnect = 1500;

        // Default and supported baud rates
        public static readonly int[] SupportedBaudrates = { 115200, 230400, 460800, 921600, 1_500_000, 2_000_000 };
        public const int MaxSupportedBaudrate = 2_000_000;
        public const int DefaultRomBaud = 115200;
        public const int DefaultFlashBaud = 921600;
        public const int MonitorBaud = 115200;
        public const bool DebugSerial = false;

        // Supported vendor IDs
        public static readonly int[] SupportedVendorIds = { 0x303a, 0x1a86, 0x10c4, 0x0403 };

        // USB Bridge capabilities database
        private static readonly Dictionary<int, UsbVendorInfo> UsbBridgeCapabilities;

        static UsbDeviceRecognition()
        {
            UsbBridgeCapabilities = new Dictionary<int, UsbVendorInfo>
            {
                // QinHeng Electronics
                {
                    0x1a86, new UsbVendorInfo("QinHeng Electronics")
                    {
                        Products = new Dictionary<int, UsbBridgeInfo>
                        {
                            { 0x7522, new UsbBridgeInfo("CH340", 460_800) },
                            { 0x7523, new UsbBridgeInfo("CH340", 460_800) },
                            { 0x7584, new UsbBridgeInfo("CH340", 460_800) },
                            { 0x5523, new UsbBridgeInfo("CH341", 2_000_000) },
                            { 0x55d3, new UsbBridgeInfo("CH343", 6_000_000) },
                            { 0x55d4, new UsbBridgeInfo("CH9102", 6_000_000) },
                            { 0x55d8, new UsbBridgeInfo("CH9101", 3_000_000) }
                        }
                    }
                },

                // Silicon Labs
                {
                    0x10c4, new UsbVendorInfo("Silicon Labs")
                    {
                        Products = new Dictionary<int, UsbBridgeInfo>
                        {
                            { 0xea60, new UsbBridgeInfo("CP2102(n)", 3_000_000) },
                            { 0xea70, new UsbBridgeInfo("CP2105", 2_000_000) },
                            { 0xea71, new UsbBridgeInfo("CP2108", 2_000_000) }
                        }
                    }
                },

                // FTDI
                {
                    0x0403, new UsbVendorInfo("FTDI")
                    {
                        Products = new Dictionary<int, UsbBridgeInfo>
                        {
                            { 0x6001, new UsbBridgeInfo("FT232R", 3_000_000) },
                            { 0x6010, new UsbBridgeInfo("FT2232", 3_000_000) },
                            { 0x6011, new UsbBridgeInfo("FT4232", 3_000_000) },
                            { 0x6014, new UsbBridgeInfo("FT232H", 12_000_000) },
                            { 0x6015, new UsbBridgeInfo("FT230X", 3_000_000) }
                        }
                    }
                },

                // Espressif Systems
                {
                    0x303a, new UsbVendorInfo("Espressif Systems")
                    {
                        Products = new Dictionary<int, UsbBridgeInfo>
                        {
                            { 0x0002, new UsbBridgeInfo("ESP32-S2 Native USB", 2_000_000) },
                            { 0x1001, new UsbBridgeInfo("ESP32 Native USB", 2_000_000) },
                            { 0x1002, new UsbBridgeInfo("ESP32 Native USB", 2_000_000) },
                            { 0x4002, new UsbBridgeInfo("ESP32 Native USB (CDC)", 2_000_000) },
                            { 0x1000, new UsbBridgeInfo("ESP32 Native USB", 2_000_000) }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gets USB device information based on Vendor ID and Product ID
        /// </summary>
        /// <param name="vid">USB Vendor ID</param>
        /// <param name="pid">USB Product ID</param>
        /// <returns>USB device information or null if not found</returns>
        public static UsbDeviceInfo GetUsbDeviceInfo(int vid, int pid)
        {
            if (!UsbBridgeCapabilities.TryGetValue(vid, out var vendor))
            {
                return null;
            }

            if (!vendor.Products.TryGetValue(pid, out var product))
            {
                return new UsbDeviceInfo(vendor.VendorName);
            }

            return new UsbDeviceInfo(vendor.VendorName, product.Name, product.MaxBaudrate);
        }

        /// <summary>
        /// Checks if a vendor ID is supported
        /// </summary>
        /// <param name="vid">USB Vendor ID</param>
        /// <returns>True if vendor is supported</returns>
        public static bool IsVendorSupported(int vid)
        {
            return SupportedVendorIds.Contains(vid);
        }

        /// <summary>
        /// Gets the optimal baud rate for a device, considering its hardware capabilities
        /// </summary>
        /// <param name="vid">USB Vendor ID</param>
        /// <param name="pid">USB Product ID</param>
        /// <param name="desiredBaudrate">Desired baud rate (default: DefaultFlashBaud)</param>
        /// <returns>Optimal baud rate for the device</returns>
        public static int GetOptimalBaudrate(int vid, int pid, int desiredBaudrate = DefaultFlashBaud)
        {
            var deviceInfo = GetUsbDeviceInfo(vid, pid);

            if (deviceInfo?.MaxBaudrate == null)
            {
                return desiredBaudrate;
            }

            // Return the desired rate if device supports it, otherwise return device's max
            return desiredBaudrate <= deviceInfo.MaxBaudrate.Value
                ? desiredBaudrate
                : deviceInfo.MaxBaudrate.Value;
        }

        /// <summary>
        /// Gets all supported vendor IDs with their names
        /// </summary>
        /// <returns>Dictionary of vendor IDs and names</returns>
        public static Dictionary<int, string> GetSupportedVendors()
        {
            return UsbBridgeCapabilities.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.VendorName
            );
        }

        /// <summary>
        /// Gets all products for a specific vendor
        /// </summary>
        /// <param name="vid">USB Vendor ID</param>
        /// <returns>Dictionary of product IDs and their information, or null if vendor not found</returns>
        public static Dictionary<int, UsbBridgeInfo> GetVendorProducts(int vid)
        {
            return UsbBridgeCapabilities.TryGetValue(vid, out var vendor)
                ? vendor.Products
                : null;
        }

        /// <summary>
        /// Formats device information as a human-readable string
        /// </summary>
        /// <param name="vid">USB Vendor ID</param>
        /// <param name="pid">USB Product ID</param>
        /// <returns>Formatted device description</returns>
        public static string GetDeviceDescription(int vid, int pid)
        {
            var deviceInfo = GetUsbDeviceInfo(vid, pid);

            if (deviceInfo == null)
            {
                return $"Unknown device (VID: 0x{vid:X4}, PID: 0x{pid:X4})";
            }

            if (string.IsNullOrEmpty(deviceInfo.ProductName))
            {
                return $"{deviceInfo.VendorName} (Unknown Product, PID: 0x{pid:X4})";
            }

            return $"{deviceInfo.VendorName} - {deviceInfo.ProductName} (Max: {deviceInfo.MaxBaudrate:N0} baud)";
        }
    }
}
