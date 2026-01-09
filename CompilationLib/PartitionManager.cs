namespace CompilationLib
{
    /// <summary>
    /// Manages partition scheme selection based on ESP model and flash size
    /// </summary>
    public class PartitionManager
    {
        // Mapping of ESP models to their default partition schemes
        private static readonly Dictionary<string, PlatformPartitionInfo> PlatformPartitions = new()
        {
            ["esp32"] = new PlatformPartitionInfo
            {
                ChipName = "esp32",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true }
                }
            },
            ["esp32c3"] = new PlatformPartitionInfo
            {
                ChipName = "esp32c3",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true }
                }
            },
            ["esp32c6"] = new PlatformPartitionInfo
            {
                ChipName = "esp32c6",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true }
                }
            },
            ["esp32s3"] = new PlatformPartitionInfo
            {
                ChipName = "esp32s3",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB", "32MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true },
                    ["32MB"] = new PartitionScheme { FileName = "min_spiffs_32mb.csv", FlashSize = "32MB", HasOTA = true }
                }
            }
        };

        /// <summary>
        /// Gets partition scheme for a specific platform and flash size
        /// </summary>
        /// <param name="platform">Platform name (e.g., "GUI_Generic_ESP32", "esp32c6")</param>
        /// <param name="flashSize">Flash size (e.g., "4MB", "8MB")</param>
        /// <returns>PartitionScheme or null if not found</returns>
        public static PartitionScheme GetPartitionScheme(string platform, string flashSize)
        {
            var chipName = NormalizeChipName(platform);
            
            if (string.IsNullOrEmpty(chipName) || string.IsNullOrEmpty(flashSize))
                return null;

            if (!PlatformPartitions.TryGetValue(chipName, out var platformInfo))
                return null;

            if (!platformInfo.PartitionSchemes.TryGetValue(flashSize, out var scheme))
                return null;

            return scheme;
        }

        /// <summary>
        /// Gets all supported flash sizes for a platform
        /// </summary>
        /// <param name="platform">Platform name</param>
        /// <returns>Array of supported flash sizes</returns>
        public static string[] GetSupportedFlashSizes(string platform)
        {
            var chipName = NormalizeChipName(platform);
            
            if (string.IsNullOrEmpty(chipName))
                return Array.Empty<string>();

            if (!PlatformPartitions.TryGetValue(chipName, out var platformInfo))
                return Array.Empty<string>();

            return platformInfo.SupportedFlashSizes;
        }

        /// <summary>
        /// Validates if a flash size is compatible with the selected platform
        /// </summary>
        /// <param name="platform">Platform name</param>
        /// <param name="flashSize">Flash size to validate</param>
        /// <returns>True if compatible, false otherwise</returns>
        public static bool ValidateFlashSize(string platform, string flashSize)
        {
            var supportedSizes = GetSupportedFlashSizes(platform);
            return supportedSizes.Contains(flashSize, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets default flash size for a platform
        /// </summary>
        /// <param name="platform">Platform name</param>
        /// <returns>Default flash size</returns>
        public static string GetDefaultFlashSize(string platform)
        {
            var chipName = NormalizeChipName(platform);
            
            if (string.IsNullOrEmpty(chipName))
                return "4MB";

            if (!PlatformPartitions.TryGetValue(chipName, out var platformInfo))
                return "4MB";

            return platformInfo.DefaultFlashSize;
        }

        /// <summary>
        /// Normalizes platform/chip name to standard format (e.g., "GUI_Generic_ESP32" -> "esp32")
        /// </summary>
        private static string NormalizeChipName(string platform)
        {
            if (string.IsNullOrEmpty(platform))
                return string.Empty;

            var normalized = platform.ToLowerInvariant().Trim();
            
            // Remove common prefixes
            if (normalized.StartsWith("gui_generic_"))
            {
                normalized = normalized.Substring("gui_generic_".Length);
            }

            // Map variations to standard names
            if (normalized.Contains("esp32c6"))
                return "esp32c6";
            if (normalized.Contains("esp32c3"))
                return "esp32c3";
            if (normalized.Contains("esp32s3"))
                return "esp32s3";
            if (normalized.Contains("esp32s2"))
                return "esp32s2";
            if (normalized.Contains("esp32"))
                return "esp32";

            return normalized;
        }

        /// <summary>
        /// Gets partition file path for merge_bin operation
        /// </summary>
        /// <param name="platform">Platform name</param>
        /// <param name="flashSize">Flash size</param>
        /// <param name="repositoryPath">Path to GUI-Generic repository</param>
        /// <returns>Full path to partition CSV file, or null if not found</returns>
        public static string GetPartitionFilePath(string platform, string flashSize, string repositoryPath)
        {
            var scheme = GetPartitionScheme(platform, flashSize);
            if (scheme == null)
                return null;

            // Partition files should be in the repository's partitions directory
            var partitionsDir = Path.Combine(repositoryPath, "partitions");
            if (!Directory.Exists(partitionsDir))
                return null;

            var partitionFile = Path.Combine(partitionsDir, scheme.FileName);
            return File.Exists(partitionFile) ? partitionFile : null;
        }
    }

    /// <summary>
    /// Information about platform-specific partition support
    /// </summary>
    public class PlatformPartitionInfo
    {
        public string ChipName { get; set; }
        public string DefaultFlashSize { get; set; }
        public string[] SupportedFlashSizes { get; set; }
        public Dictionary<string, PartitionScheme> PartitionSchemes { get; set; }
    }

    /// <summary>
    /// Information about a specific partition scheme
    /// </summary>
    public class PartitionScheme
    {
        public string FileName { get; set; }
        public string FlashSize { get; set; }
        public bool HasOTA { get; set; }

        public override string ToString()
        {
            return $"{FileName} ({FlashSize}, OTA: {HasOTA})";
        }
    }
}
