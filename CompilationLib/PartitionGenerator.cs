namespace CompilationLib
{
    /// <summary>
    /// Validates and parses ESP32 partition CSV files with OTA support
    /// </summary>
    public class PartitionGenerator
    {
        /// <summary>
        /// Verifies that required partition files exist in the repository
        /// </summary>
        /// <param name="repositoryPath">Path to GUI-Generic repository</param>
        /// <returns>Number of partition files found</returns>
        public static int EnsurePartitionFilesExist(string repositoryPath)
        {
            if (string.IsNullOrEmpty(repositoryPath) || !Directory.Exists(repositoryPath))
            {
                throw new DirectoryNotFoundException($"Repository path not found: {repositoryPath}");
            }

            var partitionsDir = Path.Combine(repositoryPath, "partitions");
            if (!Directory.Exists(partitionsDir))
            {
                Console.WriteLine($"? Warning: Partitions directory not found: {partitionsDir}");
                return 0;
            }

            // Check for expected partition files
            var expectedFiles = new[]
            {
                "min_spiffs_4mb.csv",
                "min_spiffs_8mb.csv",
                "min_spiffs_16mb.csv",
                "min_spiffs_32mb.csv"
            };

            int filesFound = 0;

            foreach (var fileName in expectedFiles)
            {
                var filePath = Path.Combine(partitionsDir, fileName);
                
                if (File.Exists(filePath))
                {
                    filesFound++;
                    Console.WriteLine($"? Found partition file: {fileName}");
                }
                else
                {
                    Console.WriteLine($"? Warning: Partition file not found: {fileName}");
                }
            }

            if (filesFound == 0)
            {
                Console.WriteLine($"? Warning: No partition files found in {partitionsDir}");
            }
            else
            {
                Console.WriteLine($"? Found {filesFound} of {expectedFiles.Length} expected partition files");
            }

            return filesFound;
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
