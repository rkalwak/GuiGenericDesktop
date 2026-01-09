using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;

namespace CompilationLib
{
    /// <summary>
    /// Manages saving and loading build configurations
    /// </summary>
    public class BuildConfigurationManager
    {
        private readonly string _configurationsDirectory;

        public BuildConfigurationManager(string configurationsDirectory)
        {
            _configurationsDirectory = configurationsDirectory;
            
            if (!Directory.Exists(_configurationsDirectory))
            {
                Directory.CreateDirectory(_configurationsDirectory);
            }
        }

        /// <summary>
        /// Saves a build configuration
        /// </summary>
        public void SaveConfiguration(
            IEnumerable<BuildFlagItem> enabledFlags, 
            string configName = null, 
            string platform = null, 
            string comPort = null, 
            string firmwareFilePath = null, 
            string buildOutputDirectory = null,
            string flashSize = null,
            string repositoryPath = null,
            IEsptoolWrapper esptoolWrapper = null)
        {
            if (enabledFlags == null)
                return;

            // Build the BuildFlagsParameters dictionary with all enabled flags
            var flagsParameters = new Dictionary<string, Dictionary<string, string>>();
            foreach (var flag in enabledFlags.Where(f => !string.IsNullOrEmpty(f?.Key)))
            {
                var paramValues = new Dictionary<string, string>();
                
                if (flag.Parameters != null && flag.Parameters.Any())
                {
                    foreach (var param in flag.Parameters.Where(p => !string.IsNullOrEmpty(p?.Identifier)))
                    {
                        paramValues[param.Identifier!] = param.Value ?? string.Empty;
                    }
                }
                
                // Add flag even if it has no parameters (empty dictionary)
                flagsParameters[flag.Key!] = paramValues;
            }
            
            // Generate encoded configuration (reversible)
            var encodedConfig = BuildConfigurationHasher.EncodeOptions(enabledFlags);
            
            var config = new SavedBuildConfiguration
            {
                EncodedConfig = encodedConfig,
                ConfigurationName = configName ?? $"Config_{DateTime.Now:yyyyMMdd_HHmmss}",
                SavedDate = DateTime.Now,
                Platform = platform ?? string.Empty,
                ComPort = comPort ?? string.Empty,
                BuildFlagsParameters = flagsParameters
            };

            // Use configName for filename if provided, otherwise use timestamp
            string fileName;
            string sanitizedName;
            if (!string.IsNullOrEmpty(configName))
            {
                // Manual save: use custom name, sanitize it
                sanitizedName = configName;
                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (var c in invalidChars)
                {
                    sanitizedName = sanitizedName.Replace(c, '_');
                }
                fileName = $"{sanitizedName}.json";
            }
            else
            {
                // Auto-save: use timestamp as filename
                sanitizedName = $"Config_{DateTime.Now:yyyyMMdd_HHmmss}";
                fileName = $"{sanitizedName}.json";
            }
             
            var filePath = Path.Combine(_configurationsDirectory, fileName);
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
            
            // Copy firmware files if provided
            if (!string.IsNullOrEmpty(firmwareFilePath) && File.Exists(firmwareFilePath))
            {
                try
                {
                    // Copy main firmware.bin
                    var firmwareFileName = $"{sanitizedName}.bin";
                    var firmwareDestPath = Path.Combine(_configurationsDirectory, firmwareFileName);
                    File.Copy(firmwareFilePath, firmwareDestPath, overwrite: true);
                    
                    // Create merged ZIP file with all necessary files
                    string mergedZipPath = null;
                    string mergedBinPath = null;
                    
                    if (!string.IsNullOrEmpty(buildOutputDirectory) && Directory.Exists(buildOutputDirectory))
                    {
                        mergedZipPath = CreateMergedZipFile(
                            sanitizedName, 
                            buildOutputDirectory, 
                            platform, 
                            flashSize, 
                            repositoryPath, 
                            esptoolWrapper,
                            out mergedBinPath);
                    }
                    
                    // Update config with firmware file references
                    config.FirmwareFileName = firmwareFileName;
                    config.MergedZipFileName = mergedZipPath != null ? Path.GetFileName(mergedZipPath) : string.Empty;
                    json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the entire save operation
                    Console.WriteLine($"Failed to copy firmware files: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Loads a build configuration by its encoded config string
        /// </summary>
        public SavedBuildConfiguration LoadConfiguration(string encodedConfig)
        {
            if (string.IsNullOrEmpty(encodedConfig))
                return null;

            // Search through all configurations by encoded config
            var allConfigs = GetAllConfigurations();
            
            // Try matching by encoded config
            return allConfigs.FirstOrDefault(c => string.Equals(c.EncodedConfig, encodedConfig, StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets all saved configurations
        /// </summary>
        public List<SavedBuildConfiguration> GetAllConfigurations()
        {
            var configurations = new List<SavedBuildConfiguration>();

            if (!Directory.Exists(_configurationsDirectory))
                return configurations;

            foreach (var file in Directory.GetFiles(_configurationsDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var config = JsonConvert.DeserializeObject<SavedBuildConfiguration>(json);
                    if (config != null)
                    {
                        config.FileName = Path.GetFileName(file);
                        configurations.Add(config);
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            return configurations.OrderByDescending(c => c.SavedDate).ToList();
        }

        /// <summary>
        /// Deletes a configuration by filename
        /// </summary>
        public bool DeleteConfiguration(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            // Add .json extension if not present
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            var filePath = Path.Combine(_configurationsDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
             
            return false;
        }

        /// <summary>
        /// Creates a merged ZIP file containing firmware.bin, bootloader.bin, partitions.bin, and a complete merged binary
        /// </summary>
        private string CreateMergedZipFile(
            string sanitizedName, 
            string buildOutputDirectory, 
            string platform, 
            string flashSize, 
            string repositoryPath, 
            IEsptoolWrapper esptoolWrapper,
            out string mergedBinPath)
        {
            mergedBinPath = null;
            
            try
            {
                var zipFileName = $"{sanitizedName}_merged.zip";
                var zipFilePath = Path.Combine(_configurationsDirectory, zipFileName);

                // Delete existing zip if it exists
                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                // Create merged bin file if esptool wrapper is provided and we have platform/flash size info
                if (esptoolWrapper != null && 
                    !string.IsNullOrEmpty(platform) && 
                    !string.IsNullOrEmpty(flashSize) &&
                    !flashSize.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        mergedBinPath = Path.Combine(_configurationsDirectory, $"{sanitizedName}_complete.bin");
                        
                        Console.WriteLine($"Creating merged firmware binary...");
                        var result = esptoolWrapper.MergeFirmwareFiles(
                            buildOutputDirectory, 
                            mergedBinPath, 
                            platform, 
                            flashSize, 
                            repositoryPath).GetAwaiter().GetResult();
                        
                        if (string.IsNullOrEmpty(result))
                        {
                            Console.WriteLine($"⚠ Warning: Failed to create merged bin file");
                            mergedBinPath = null;
                        }
                        else
                        {
                            Console.WriteLine($"✓ Created merged bin file: {Path.GetFileName(mergedBinPath)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Warning: Error creating merged bin: {ex.Message}");
                        mergedBinPath = null;
                    }
                }

                // Create ZIP file
                using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    // Add firmware.bin
                    var firmwarePath = Path.Combine(buildOutputDirectory, "firmware.bin");
                    if (File.Exists(firmwarePath))
                    {
                        zip.CreateEntryFromFile(firmwarePath, "firmware.bin", CompressionLevel.Optimal);
                    }

                    // Add bootloader.bin
                    var bootloaderPath = Path.Combine(buildOutputDirectory, "bootloader.bin");
                    if (File.Exists(bootloaderPath))
                    {
                        zip.CreateEntryFromFile(bootloaderPath, "bootloader.bin", CompressionLevel.Optimal);
                    }

                    // Add partitions.bin
                    var partitionsPath = Path.Combine(buildOutputDirectory, "partitions.bin");
                    if (File.Exists(partitionsPath))
                    {
                        zip.CreateEntryFromFile(partitionsPath, "partitions.bin", CompressionLevel.Optimal);
                    }
                    
                    // Add merged complete binary if it was created successfully
                    if (!string.IsNullOrEmpty(mergedBinPath) && File.Exists(mergedBinPath))
                    {
                        zip.CreateEntryFromFile(mergedBinPath, $"firmware_merged.bin", CompressionLevel.Optimal);
                        Console.WriteLine($"✓ Added merged bin to ZIP: {Path.GetFileName(mergedBinPath)}");
                    }
                    
                    // Add README with flashing instructions
                    var readmeEntry = zip.CreateEntry("README.txt");
                    using (var writer = new StreamWriter(readmeEntry.Open()))
                    {
                        writer.WriteLine("=== Supla Firmware Package ===");
                        writer.WriteLine($"Configuration: {sanitizedName}");
                        writer.WriteLine($"Platform: {platform}");
                        writer.WriteLine($"Flash Size: {flashSize}");
                        writer.WriteLine($"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        writer.WriteLine();
                        writer.WriteLine("=== Files Included ===");
                        writer.WriteLine("1. firmware.bin      - Application firmware");
                        writer.WriteLine("2. bootloader.bin    - ESP32 bootloader");
                        writer.WriteLine("3. partitions.bin    - Partition table");
                        
                        if (!string.IsNullOrEmpty(mergedBinPath) && File.Exists(mergedBinPath))
                        {
                            writer.WriteLine($"4. {sanitizedName}_complete.bin - Complete merged firmware (RECOMMENDED)");
                            writer.WriteLine();
                            writer.WriteLine("=== RECOMMENDED: Flash Complete Binary ===");
                            writer.WriteLine($"Flash the complete binary to address 0x0:");
                            writer.WriteLine();
                            var chipType = GetChipFromPlatform(platform);
                            writer.WriteLine($"esptool --chip {chipType} --port COM_PORT write_flash 0x0 {sanitizedName}_complete.bin");
                            writer.WriteLine();
                            writer.WriteLine("This single file contains bootloader, partitions, and firmware.");
                            writer.WriteLine();
                        }
                        
                        writer.WriteLine("=== Alternative: Flash Individual Files ===");
                        writer.WriteLine("If you need to flash individual files:");
                        writer.WriteLine();
                        var chip = GetChipFromPlatform(platform);
                        writer.WriteLine($"esptool --chip {chip} --port COM_PORT write_flash \\ ");
                        writer.WriteLine("  0x1000 bootloader.bin \\ ");
                        writer.WriteLine("  0x8000 partitions.bin \\ ");
                        writer.WriteLine("  0x10000 firmware.bin");
                        writer.WriteLine();
                        writer.WriteLine("=== Notes ===");
                        writer.WriteLine("- Replace COM_PORT with your device's COM port (e.g., COM3)");
                        writer.WriteLine("- Ensure esptool is installed: pip install esptool");
                        writer.WriteLine("- OTA updates are supported after initial flash");
                    }
                }

                Console.WriteLine($"✓ Created merged firmware ZIP: {zipFileName}");
                return zipFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create merged ZIP file: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Extracts chip type from platform name
        /// </summary>
        private static string GetChipFromPlatform(string platform)
        {
            if (string.IsNullOrEmpty(platform))
                return "esp32";

            var platformLower = platform.ToLowerInvariant();
            
            if (platformLower.Contains("esp32c6"))
                return "esp32c6";
            if (platformLower.Contains("esp32c3"))
                return "esp32c3";
            if (platformLower.Contains("esp32s3"))
                return "esp32s3";
            if (platformLower.Contains("esp32s2"))
                return "esp32s2";
            
            return "esp32";
        }
    }

    /// <summary>
    /// Represents a saved build configuration
    /// </summary>
    public class SavedBuildConfiguration
    {
        /// <summary>
        /// Encoded configuration string (reversible, can decode to get flags)
        /// </summary>
        public string EncodedConfig { get; set; } = string.Empty;
        
        public string ConfigurationName { get; set; } = string.Empty;
        public DateTime SavedDate { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string ComPort { get; set; } = string.Empty;
        
        /// <summary>
        /// Filename of the associated firmware binary (if available)
        /// </summary>
        public string FirmwareFileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Filename of the merged ZIP file containing firmware.bin, bootloader.bin, and partitions.bin (if available)
        /// </summary>
        public string MergedZipFileName { get; set; } = string.Empty;
        
        [JsonIgnore]
        public string FileName { get; set; }
        
        /// <summary>
        /// Dictionary of flag parameters. Key is the flag key, value is a dictionary of parameter name to value.
        /// Example: { "SUPLA_DHT22": { "Pin": "5", "Type": "DHT22" } }
        /// Flags without parameters will have an empty dictionary: { "SUPLA_DEVICE": { } }
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> BuildFlagsParameters { get; set; } = new();
        
        /// <summary>
        /// Gets the list of enabled flag keys from BuildFlagsParameters.
        /// This is a computed property for backward compatibility.
        /// </summary>
        [JsonIgnore]
        public List<string> EnabledFlagKeys => BuildFlagsParameters.Keys.ToList();
    }
}
