using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilationLib
{
    /// <summary>
    /// Generates ESP32 partition CSV files with OTA support for different flash sizes
    /// </summary>
    public class PartitionGenerator
    {
        /// <summary>
        /// Ensures all required partition files exist in the repository
        /// </summary>
        /// <param name="repositoryPath">Path to GUI-Generic repository</param>
        /// <returns>Number of partition files created</returns>
        public static int EnsurePartitionFilesExist(string repositoryPath)
        {
            if (string.IsNullOrEmpty(repositoryPath) || !Directory.Exists(repositoryPath))
            {
                throw new DirectoryNotFoundException($"Repository path not found: {repositoryPath}");
            }

            var partitionsDir = Path.Combine(repositoryPath, "partitions");
            if (!Directory.Exists(partitionsDir))
            {
                Directory.CreateDirectory(partitionsDir);
                Console.WriteLine($"Created partitions directory: {partitionsDir}");
            }

            int filesCreated = 0;

            // Generate partition files for all supported flash sizes
            var partitionConfigs = new Dictionary<string, PartitionConfig>
            {
                ["4MB"] = new PartitionConfig 
                { 
                    FileName = "min_spiffs_4mb.csv",
                    FlashSize = 0x400000, // 4MB = 4 * 1024 * 1024
                    AppSize = 0x180000,   // 1.5MB per app (for OTA)
                    SpiffsSize = 0x90000  // 576KB for SPIFFS
                },
                ["8MB"] = new PartitionConfig 
                { 
                    FileName = "min_spiffs_8mb.csv",
                    FlashSize = 0x800000, // 8MB
                    AppSize = 0x300000,   // 3MB per app (for OTA)
                    SpiffsSize = 0x140000 // 1.25MB for SPIFFS
                },
                ["16MB"] = new PartitionConfig 
                { 
                    FileName = "min_spiffs_16mb.csv",
                    FlashSize = 0x1000000, // 16MB
                    AppSize = 0x500000,    // 5MB per app (for OTA)
                    SpiffsSize = 0x590000  // 5.6MB for SPIFFS (adjusted to fit)
                },
                ["32MB"] = new PartitionConfig 
                { 
                    FileName = "min_spiffs_32mb.csv",
                    FlashSize = 0x2000000, // 32MB
                    AppSize = 0xA00000,    // 10MB per app (for OTA)
                    SpiffsSize = 0xBE0000  // 11.9MB for SPIFFS (adjusted to fit)
                }
            };

            foreach (var config in partitionConfigs.Values)
            {
                var filePath = Path.Combine(partitionsDir, config.FileName);
                
                if (!File.Exists(filePath))
                {
                    GeneratePartitionFile(filePath, config);
                    filesCreated++;
                    Console.WriteLine($"? Created partition file: {config.FileName}");
                }
                else
                {
                    Console.WriteLine($"  Partition file already exists: {config.FileName}");
                }
            }

            return filesCreated;
        }

        /// <summary>
        /// Generates a partition CSV file with OTA support
        /// </summary>
        private static void GeneratePartitionFile(string filePath, PartitionConfig config)
        {
            var sb = new StringBuilder();
            
            // CSV Header
            sb.AppendLine("# Name,   Type, SubType, Offset,  Size, Flags");
            
            // NVS - Non-Volatile Storage (20KB)
            sb.AppendLine("nvs,      data, nvs,     0x9000,  0x5000,");
            
            // OTA Data - OTA selection (8KB)
            sb.AppendLine("otadata,  data, ota,     0xe000,  0x2000,");
            
            // App0 - First OTA partition (starts at 0x10000)
            sb.AppendLine($"app0,     app,  ota_0,   0x10000, 0x{config.AppSize:X},");
            
            // App1 - Second OTA partition (for OTA updates)
            var app1Offset = 0x10000 + config.AppSize;
            sb.AppendLine($"app1,     app,  ota_1,   0x{app1Offset:X}, 0x{config.AppSize:X},");
            
            // SPIFFS - File system (remaining space)
            var spiffsOffset = app1Offset + config.AppSize;
            sb.AppendLine($"spiffs,   data, spiffs,  0x{spiffsOffset:X}, 0x{config.SpiffsSize:X},");
            
            // Verify partition layout doesn't exceed flash size
            var totalUsed = spiffsOffset + config.SpiffsSize;
            if (totalUsed > config.FlashSize)
            {
                throw new InvalidOperationException(
                    $"Partition layout exceeds flash size! Used: 0x{totalUsed:X} ({totalUsed / 1024.0 / 1024.0:F2} MB), " +
                    $"Available: 0x{config.FlashSize:X} ({config.FlashSize / 1024.0 / 1024.0:F2} MB)");
            }
            
            File.WriteAllText(filePath, sb.ToString());
        }

        /// <summary>
        /// Validates that a partition file exists and is correctly formatted
        /// </summary>
        /// <param name="filePath">Path to partition CSV file</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidatePartitionFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                var lines = File.ReadAllLines(filePath);
                
                // Check for required partitions
                var hasNvs = false;
                var hasOtaData = false;
                var hasApp0 = false;
                var hasApp1 = false;
                var hasSpiffs = false;

                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        continue;

                    var lower = line.ToLowerInvariant();
                    if (lower.Contains("nvs")) hasNvs = true;
                    if (lower.Contains("otadata")) hasOtaData = true;
                    if (lower.Contains("app") && lower.Contains("ota_0")) hasApp0 = true;
                    if (lower.Contains("app") && lower.Contains("ota_1")) hasApp1 = true;
                    if (lower.Contains("spiffs")) hasSpiffs = true;
                }

                // Must have all required partitions for OTA
                return hasNvs && hasOtaData && hasApp0 && hasApp1 && hasSpiffs;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets partition layout information from a partition file
        /// </summary>
        /// <param name="filePath">Path to partition CSV file</param>
        /// <returns>Partition layout info</returns>
        public static PartitionLayout GetPartitionLayout(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Partition file not found: {filePath}");

            var layout = new PartitionLayout { FilePath = filePath };
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 5)
                    continue;

                var name = parts[0].Trim();
                var type = parts[1].Trim();
                var subType = parts[2].Trim();
                var offsetStr = parts[3].Trim();
                var sizeStr = parts[4].Trim();

                // Parse offset and size (handle hex format)
                if (TryParseHex(offsetStr, out var offset) && TryParseHex(sizeStr, out var size))
                {
                    layout.Partitions.Add(new PartitionEntry
                    {
                        Name = name,
                        Type = type,
                        SubType = subType,
                        Offset = offset,
                        Size = size
                    });

                    // Track specific partitions for merge_bin
                    if (name.Equals("app0", StringComparison.OrdinalIgnoreCase) || 
                        subType.Contains("ota_0", StringComparison.OrdinalIgnoreCase))
                    {
                        layout.App0Offset = offset;
                        layout.App0Size = size;
                    }
                }
            }

            return layout;
        }

        private static bool TryParseHex(string value, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            
            // Remove 0x prefix if present
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(2);

            return int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out result);
        }
    }

    /// <summary>
    /// Configuration for generating a partition file
    /// </summary>
    public class PartitionConfig
    {
        public string FileName { get; set; }
        public int FlashSize { get; set; }
        public int AppSize { get; set; }
        public int SpiffsSize { get; set; }
    }

    /// <summary>
    /// Represents the layout of partitions in a partition table
    /// </summary>
    public class PartitionLayout
    {
        public string FilePath { get; set; }
        public List<PartitionEntry> Partitions { get; set; } = new();
        public int App0Offset { get; set; }
        public int App0Size { get; set; }
    }

    /// <summary>
    /// Represents a single partition entry
    /// </summary>
    public class PartitionEntry
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public int Offset { get; set; }
        public int Size { get; set; }

        public override string ToString()
        {
            return $"{Name}: 0x{Offset:X} (Size: 0x{Size:X})";
        }
    }
}
