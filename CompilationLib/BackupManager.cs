namespace CompilationLib
{
    /// <summary>
    /// Manages backup operations for ESP devices before flashing
    /// </summary>
    public class BackupManager
    {
        private readonly string _backupDirectory;
        private readonly IEsptoolWrapper _esptoolWrapper;

        public BackupManager(string backupDirectory, IEsptoolWrapper esptoolWrapper)
        {
            _backupDirectory = backupDirectory ?? throw new ArgumentNullException(nameof(backupDirectory));
            _esptoolWrapper = esptoolWrapper ?? throw new ArgumentNullException(nameof(esptoolWrapper));

            // Ensure backup directory exists
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        /// <summary>
        /// Creates a backup of the ESP device flash memory before deployment
        /// </summary>
        /// <param name="comPort">COM port where the device is connected</param>
        /// <param name="chipType">ESP chip type (e.g., "esp32", "esp32c3", "esp32c6")</param>
        /// <param name="encodedConfig">Encoded configuration string for the new firmware</param>
        /// <param name="configTimestamp">Configuration timestamp in format yyyyMMdd_HHmmss for consistent naming</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the created backup file, or null if backup failed</returns>
        public async Task<string> CreateBackupAsync(
            string comPort, 
            string chipType, 
            string encodedConfig,
            string configTimestamp = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(comPort))
                throw new ArgumentException("COM port cannot be null or empty", nameof(comPort));

            if (string.IsNullOrEmpty(chipType))
                throw new ArgumentException("Chip type cannot be null or empty", nameof(chipType));

            if (string.IsNullOrEmpty(encodedConfig))
                throw new ArgumentException("Encoded config cannot be null or empty", nameof(encodedConfig));

            try
            {
                var fileName = $"Backup_{configTimestamp}.backup";
                var backupFilePath = Path.Combine(_backupDirectory, fileName);

                Console.WriteLine($"Creating backup to: {backupFilePath}");
                Console.WriteLine($"Backup may take several minutes depending on flash size...");

                // Read flash using esptool
                var result = await _esptoolWrapper.ReadFlush(comPort, chipType, backupFilePath, cancellation: cancellationToken);

                if (result.Success)
                {
                    Console.WriteLine($"? Backup created successfully: {backupFilePath}");
                    
                    // Create backup metadata file
                    await CreateBackupMetadataAsync(backupFilePath, comPort, chipType, encodedConfig);
                    
                    return backupFilePath;
                }
                else
                {
                    Console.WriteLine($"? Backup failed: {result.StdErr}");
                    
                    // Clean up partial backup file if it exists
                    if (File.Exists(backupFilePath))
                    {
                        try
                        {
                            File.Delete(backupFilePath);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Backup creation error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a metadata file alongside the backup with information about the backup
        /// </summary>
        private async Task CreateBackupMetadataAsync(
            string backupFilePath, 
            string comPort, 
            string chipType, 
            string encodedConfig)
        {
            try
            {
                var metadataPath = backupFilePath + ".info";
                var metadata = new
                {
                    BackupDate = DateTime.Now,
                    ComPort = comPort,
                    ChipType = chipType,
                    EncodedConfig = encodedConfig,
                    BackupFile = Path.GetFileName(backupFilePath),
                    FileSize = new FileInfo(backupFilePath).Length
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(metadata, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(metadataPath, json);
                
                Console.WriteLine($"? Backup metadata created: {metadataPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Failed to create backup metadata: {ex.Message}");
                // Non-critical error, don't fail the backup operation
            }
        }

        /// <summary>
        /// Sanitizes the encoded config string to be safe for use in filenames
        /// </summary>
       
        /// <summary>
        /// Gets the backup directory path
        /// </summary>
        public string BackupDirectory => _backupDirectory;

        /// <summary>
        /// Lists all backups in the backup directory
        /// </summary>
        public Task<List<BackupInfo>> ListBackupsAsync()
        {
            return Task.Run(() =>
            {
                var backups = new List<BackupInfo>();

                if (!Directory.Exists(_backupDirectory))
                    return backups;

                var backupFiles = Directory.GetFiles(_backupDirectory, "*.backup");
                
                foreach (var backupFile in backupFiles)
                {
                    try
                    {
                        var info = new BackupInfo
                        {
                            FilePath = backupFile,
                            FileName = Path.GetFileName(backupFile),
                            CreatedDate = File.GetCreationTime(backupFile),
                            FileSize = new FileInfo(backupFile).Length
                        };

                        // Try to load metadata if available
                        var metadataPath = backupFile + ".info";
                        if (File.Exists(metadataPath))
                        {
                            try
                            {
                                var json = File.ReadAllText(metadataPath);
                                var metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                                info.ComPort = metadata?.ComPort;
                                info.ChipType = metadata?.ChipType;
                                info.EncodedConfig = metadata?.EncodedConfig;
                            }
                            catch
                            {
                                // Ignore metadata parsing errors
                            }
                        }

                        backups.Add(info);
                    }
                    catch
                    {
                        // Ignore individual file errors
                    }
                }

                return backups.OrderByDescending(b => b.CreatedDate).ToList();
            });
        }
    }

    /// <summary>
    /// Information about a backup file
    /// </summary>
    public class BackupInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public long FileSize { get; set; }
        public string ComPort { get; set; }
        public string ChipType { get; set; }
        public string EncodedConfig { get; set; }

        public string FileSizeFormatted => FormatFileSize(FileSize);

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
