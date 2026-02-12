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
            ["esp32-c3"] = new PlatformPartitionInfo
            {
                ChipName = "esp32-c3",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true }
                }
            },
            ["esp32-c6"] = new PlatformPartitionInfo
            {
                ChipName = "esp32-c6",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true }
                }
            },
            ["esp32-s3"] = new PlatformPartitionInfo
            {
                ChipName = "esp32-s3",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB", "32MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true },
                    ["32MB"] = new PartitionScheme { FileName = "min_spiffs_32mb.csv", FlashSize = "32MB", HasOTA = true }
                }
            },
            ["esp32-s2"] = new PlatformPartitionInfo
            {
                ChipName = "esp32-s2",
                DefaultFlashSize = "4MB",
                SupportedFlashSizes = new[] { "4MB", "8MB", "16MB" },
                PartitionSchemes = new Dictionary<string, PartitionScheme>
                {
                    ["4MB"] = new PartitionScheme { FileName = "min_spiffs_4mb.csv", FlashSize = "4MB", HasOTA = true },
                    ["8MB"] = new PartitionScheme { FileName = "min_spiffs_8mb.csv", FlashSize = "8MB", HasOTA = true },
                    ["16MB"] = new PartitionScheme { FileName = "min_spiffs_16mb.csv", FlashSize = "16MB", HasOTA = true }
                }
            }
        };

        /// <summary>
        /// Gets partition scheme for a specific platform and flash size
        /// </summary>
        /// <param name="flashSize">Flash size (e.g., "4MB", "8MB")</param>
        /// <param name="board">Board name (e.g., "esp32", "esp32c3", "esp32c6", "esp32s3")</param>
        /// <returns>PartitionScheme or null if not found</returns>
        public static PartitionScheme GetPartitionScheme(string flashSize, string board)
        {            
            if (string.IsNullOrEmpty(board) || string.IsNullOrEmpty(flashSize))
                return null;

            if (!PlatformPartitions.TryGetValue(board, out var platformInfo))
                return null;

            if (!platformInfo.PartitionSchemes.TryGetValue(flashSize, out var scheme))
                return null;

            return scheme;
        }

        /// <summary>
        /// Gets all supported flash sizes for a platform
        /// </summary>
        /// <param name="board">Platform name</param>
        /// <returns>Array of supported flash sizes</returns>
        public static string[] GetSupportedFlashSizes(string board)
        {            
            if (string.IsNullOrEmpty(board))
                return Array.Empty<string>();

            if (!PlatformPartitions.TryGetValue(board, out var platformInfo))
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
        public static string GetDefaultFlashSize(string board)
        {
            if (string.IsNullOrEmpty(board))
                return "4MB";

            if (!PlatformPartitions.TryGetValue(board, out var platformInfo))
                return "4MB";

            return platformInfo.DefaultFlashSize;
        }

        /// <summary>
        /// Gets partition file path for merge_bin operation
        /// </summary>
        /// <param name="platform">Platform name</param>
        /// <param name="flashSize">Flash size</param>
        /// <param name="repositoryPath">Path to GUI-Generic repository</param>
        /// <param name="board">Board name (e.g., "esp32", "esp32c3", "esp32c6", "esp32s3")</param>
        /// <returns>Full path to partition CSV file, or null if not found</returns>
        public static string GetPartitionFilePath(string platform, string flashSize, string repositoryPath, string board)
        {
            var scheme = GetPartitionScheme(flashSize, board);
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
