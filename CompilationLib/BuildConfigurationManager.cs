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
        private readonly IEsptoolWrapper _esptoolWrapper;

        public BuildConfigurationManager(string configurationsDirectory, IEsptoolWrapper esptoolWrapper)
        {
            _configurationsDirectory = configurationsDirectory;
            _esptoolWrapper = esptoolWrapper;
            if (!Directory.Exists(_configurationsDirectory))
            {
                Directory.CreateDirectory(_configurationsDirectory);
            }
        }

        /// <summary>
        /// Saves a build configuration
        /// </summary>
        public async Task SaveConfigurationAsync(
            IEnumerable<BuildFlagItem> enabledFlags,
            string configName,
            string board,
            string platform,
            string comPort = null,
            string firmwareFilePath = null,
            string buildOutputDirectory = null,
            string flashSize = null,
            string repositoryPath = null,
            GlobalSettings globalSettings = null
            )
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

            var globalParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (globalSettings?.Parameters != null)
            {
                foreach (var parameter in globalSettings.Parameters.Where(p => p != null && !string.IsNullOrWhiteSpace(p.Identifier)))
                {
                    var value = parameter.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(value) || parameter.IsRequired)
                    {
                        globalParameters[parameter.Identifier] = value;
                    }
                }
            }

            var config = new SavedBuildConfiguration
            {
                EncodedConfig = encodedConfig,
                ConfigurationName = configName ?? $"Config_{DateTime.Now:yyyyMMdd_HHmmss}",
                SavedDate = DateTime.Now,
                Platform = platform ?? string.Empty,
                ComPort = comPort ?? string.Empty,
                FlashSize = flashSize ?? string.Empty,
                BuildFlagsParameters = flagsParameters,
                GlobalParameters = globalParameters
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
            await File.WriteAllTextAsync(filePath, json);

            // Copy firmware files if provided
            if (!string.IsNullOrEmpty(firmwareFilePath) && File.Exists(firmwareFilePath))
            {
                try
                {
                    // Copy main firmware.bin
                    var firmwareFileName = $"{sanitizedName}.bin";
                    var firmwareDestPath = Path.Combine(_configurationsDirectory, firmwareFileName);
                    File.Copy(firmwareFilePath, firmwareDestPath, overwrite: true);

                    (string mergedZipPath, string mergedBinPath) paths = (null, null);
                    if (!string.IsNullOrEmpty(buildOutputDirectory) && Directory.Exists(buildOutputDirectory))
                    {
                        paths = await CreateMergedZipFileAsync(
                            sanitizedName,
                            buildOutputDirectory,
                            firmwareFilePath,
                            platform,
                            flashSize,
                            repositoryPath,
                            board);
                    }

                    // Update config with firmware file references
                    config.FirmwareFileName = firmwareFileName;
                    config.MergedZipFileName = paths.mergedZipPath != null ? Path.GetFileName(paths.mergedZipPath) : string.Empty;
                    json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    await File.WriteAllTextAsync(filePath, json);
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

            var files = Directory.GetFiles(_configurationsDirectory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json =  File.ReadAllText(file);
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
        private async Task<(string zipFilePath, string mergedBinPath)> CreateMergedZipFileAsync(
            string sanitizedName,
            string buildOutputDirectory,
            string firmwareFilePath,
            string platform,
            string flashSize,
            string repositoryPath,
            string board)
        {
            string mergedBinPath = null;

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
                if (_esptoolWrapper != null &&
                    !string.IsNullOrEmpty(platform) &&
                    !string.IsNullOrEmpty(flashSize) &&
                    !flashSize.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(firmwareFilePath))
                {
                    try
                    {
                        var bootloaderPath = FindBuildArtifact(buildOutputDirectory, "bootloader.bin", ".bootloader.bin");
                        var partitionsPath = FindBuildArtifact(buildOutputDirectory, "partitions.bin", ".partitions.bin");
                        if (bootloaderPath == null || partitionsPath == null)
                        {
                            throw new FileNotFoundException("Bootloader and partition binaries are required to create a merged firmware image.");
                        }

                        mergedBinPath = Path.Combine(_configurationsDirectory, $"{sanitizedName}_complete.bin");
                        var mergeInputDirectory = Path.Combine(Path.GetTempPath(), $"GuiGenericBuilder_{Guid.NewGuid():N}");
                        Directory.CreateDirectory(mergeInputDirectory);

                        try
                        {
                            File.Copy(firmwareFilePath, Path.Combine(mergeInputDirectory, "firmware.bin"));
                            File.Copy(bootloaderPath, Path.Combine(mergeInputDirectory, "bootloader.bin"));
                            File.Copy(partitionsPath, Path.Combine(mergeInputDirectory, "partitions.bin"));

                            Console.WriteLine("Creating merged firmware binary...");
                            var result = await _esptoolWrapper.MergeFirmwareFiles(
                                mergeInputDirectory,
                                mergedBinPath,
                                platform,
                                flashSize,
                                board,
                                repositoryPath);

                            if (string.IsNullOrEmpty(result))
                            {
                                Console.WriteLine("⚠ Warning: Failed to create merged bin file");
                                mergedBinPath = null;
                            }
                            else
                            {
                                Console.WriteLine($"✓ Created merged bin file: {Path.GetFileName(mergedBinPath)}");
                            }
                        }
                        finally
                        {
                            Directory.Delete(mergeInputDirectory, recursive: true);
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
                    AddBuildArtifactToZip(zip, firmwareFilePath, "firmware.bin");
                    AddBuildArtifactToZip(zip, FindBuildArtifact(buildOutputDirectory, "bootloader.bin", ".bootloader.bin"), "bootloader.bin");
                    AddBuildArtifactToZip(zip, FindBuildArtifact(buildOutputDirectory, "partitions.bin", ".partitions.bin"), "partitions.bin");

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
                            writer.WriteLine($"esptool --chip {board} --port COM_PORT write_flash 0x0 {sanitizedName}_complete.bin");
                            writer.WriteLine();
                            writer.WriteLine("This single file contains bootloader, partitions, and firmware.");
                            writer.WriteLine();
                        }

                        writer.WriteLine("=== Alternative: Flash Individual Files ===");
                        writer.WriteLine("If you need to flash individual files:");
                        writer.WriteLine();
                        writer.WriteLine($"esptool --chip {board} --port COM_PORT write_flash \\ ");
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
                return (zipFilePath, mergedBinPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create merged ZIP file: {ex.Message}");
                return (null, null);
            }
        }

        private static string FindBuildArtifact(string buildOutputDirectory, string expectedFileName, string arduinoCliSuffix)
        {
            var expectedPath = Path.Combine(buildOutputDirectory, expectedFileName);
            if (File.Exists(expectedPath))
            {
                return expectedPath;
            }

            return Directory.EnumerateFiles(buildOutputDirectory, "*.bin", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => Path.GetFileName(path).EndsWith(arduinoCliSuffix, StringComparison.OrdinalIgnoreCase));
        }

        private static void AddBuildArtifactToZip(ZipArchive zip, string sourcePath, string entryName)
        {
            if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
            {
                zip.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal);
            }
        }
    }
}
