using System.IO;
using System.Net.Http;
using System.Text;
using CompilationLib;
using Octokit;
using Serilog;

namespace GuiGenericBuilderDesktop.Services
{
    public class Z2SVersionResult
    {
        public string DeviceVersion { get; set; }
        public string LatestVersion { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string Error { get; set; }
    }

    public class Z2SBackupResult
    {
        public bool Success { get; set; }
        public string BackupFilePath { get; set; }
        public string Error { get; set; }
    }

    public class Z2SFlashResult
    {
        public bool Success { get; set; }
        public string FlashedVersion { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Checks the Z2S Library firmware version on a connected device against the latest GitHub release.
    /// Reads version.dat from the SPIFFS/LittleFS partition via ESPtool.
    /// </summary>
    public class Z2SUpdateService
    {
        private readonly ILogger _logger;
        private readonly IEsptoolWrapper _esptoolWrapper;
        private readonly GitHubClient _githubClient;

        private const string GitHubOwner = "lsroka76";
        private const string GitHubRepo = "Z2S_Library";

        // Standard SPIFFS offsets as fallbacks when partition table cannot be read
        private const long DefaultSpiffsOffset = 0x290000;
        private const long DefaultSpiffsSize = 0x170000;

        // How many bytes to read from SPIFFS when scanning for version.dat (64 KB is enough)
        private const long SpiffsScanSize = 0x10000;

        /// <summary>
        /// Builds the firmware asset filename based on the selected options.
        /// Pattern: Z2S_Gateway.8MB.OTA.{logs|no_logs}.{full_version|update_only}.bin
        /// </summary>
        public static string GetFirmwareFileName(bool withLogs, bool fullVersion)
        {
            var logsPart = withLogs ? "logs" : "no_logs";
            var versionPart = fullVersion ? "full_version" : "update_only";
            return $"Z2S_Gateway.8MB.OTA.{logsPart}.{versionPart}.bin";
        }

        public Z2SUpdateService(IEsptoolWrapper esptoolWrapper, ILogger logger)
        {
            _esptoolWrapper = esptoolWrapper;
            _logger = logger;
            _githubClient = new GitHubClient(new ProductHeaderValue("GuiGenericBuilderDesktop"));
            _logger.Information("Z2SUpdateService initialized");
        }

        /// <summary>
        /// Reads the firmware version from the device SPIFFS and compares with the latest GitHub release.
        /// </summary>
        public async Task<Z2SVersionResult> CheckVersionAsync(string comPort, CancellationToken cancellationToken = default)
        {
            _logger.Information("Checking Z2S version on port {Port}", comPort);

            string deviceVersion = null;
            string latestVersion = null;
            string error = null;

            try
            {
                deviceVersion = await ReadDeviceVersionAsync(comPort, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read Z2S device version");
                error = ex.Message;
            }

            try
            {
                latestVersion = await GetLatestGitHubVersionAsync();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to fetch Z2S latest version from GitHub");
                if (error == null) error = $"GitHub: {ex.Message}";
            }

            bool updateAvailable = false;
            if (deviceVersion != null && latestVersion != null)
            {
                var deviceVer = ExtractVersion(deviceVersion);
                var latestVer = ExtractVersion(latestVersion);

                if (Version.TryParse(deviceVer, out var dv) && Version.TryParse(latestVer, out var lv))
                    updateAvailable = lv > dv;
                else
                    updateAvailable = string.Compare(latestVer, deviceVer, StringComparison.OrdinalIgnoreCase) > 0;
            }

            return new Z2SVersionResult
            {
                DeviceVersion = deviceVersion,
                LatestVersion = latestVersion,
                IsUpdateAvailable = updateAvailable,
                Error = error
            };
        }

        private async Task<string> ReadDeviceVersionAsync(string comPort, CancellationToken cancellationToken)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "z2s_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);

            try
            {
                // Read partition table to find the SPIFFS/LittleFS partition offset
                long spiffsOffset = DefaultSpiffsOffset;
                long spiffsSize = DefaultSpiffsSize;

                var ptableFile = Path.Combine(tempDir, "ptable.bin");
                var ptableResult = await _esptoolWrapper.ReadFlashRegion(comPort, 0x8000, 0xC00, ptableFile, cancellationToken);

                if (ptableResult.Success && File.Exists(ptableFile))
                {
                    var (foundOffset, foundSize) = ParsePartitionTable(ptableFile);
                    if (foundOffset > 0)
                    {
                        spiffsOffset = foundOffset;
                        spiffsSize = foundSize;
                        _logger.Information("SPIFFS partition: offset=0x{Offset:X}, size=0x{Size:X}", spiffsOffset, spiffsSize);
                    }
                    else
                    {
                        _logger.Warning("SPIFFS partition not found in table, using default offset 0x{Offset:X}", spiffsOffset);
                    }
                }
                else
                {
                    _logger.Warning("Could not read partition table ({Error}), using default SPIFFS offset 0x{Offset:X}",
                        ptableResult.StdErr, spiffsOffset);
                }

                // Read the first SpiffsScanSize bytes of the SPIFFS partition
                var scanSize = Math.Min(spiffsSize, SpiffsScanSize);
                var spiffsFile = Path.Combine(tempDir, "spiffs_chunk.bin");
                var spiffsResult = await _esptoolWrapper.ReadFlashRegion(comPort, spiffsOffset, scanSize, spiffsFile, cancellationToken);

                if (!spiffsResult.Success || !File.Exists(spiffsFile))
                {
                    _logger.Warning("Failed to read SPIFFS region: {Error}", spiffsResult.StdErr);
                    return null;
                }

                var bytes = File.ReadAllBytes(spiffsFile);
                var version = FindVersionInBytes(bytes);

                if (version == null)
                    _logger.Warning("version.dat pattern not found in SPIFFS region");

                return version;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        /// <summary>
        /// Parses the ESP32 partition table binary to find the SPIFFS/LittleFS data partition.
        /// Each entry is 32 bytes: [0xAA][0x50][type][subtype][offset:4LE][size:4LE][name:16][flags:4]
        /// </summary>
        private (long offset, long size) ParsePartitionTable(string ptableFile)
        {
            var bytes = File.ReadAllBytes(ptableFile);

            for (int i = 0; i + 32 <= bytes.Length; i += 32)
            {
                if (bytes[i] != 0xAA || bytes[i + 1] != 0x50)
                    continue;

                byte type = bytes[i + 2];
                if (type != 0x01) // 0x01 = data partition
                    continue;

                var name = Encoding.ASCII.GetString(bytes, i + 12, 16).TrimEnd('\0');
                byte subtype = bytes[i + 3];

                bool isSpiffs = name.Contains("spiff", StringComparison.OrdinalIgnoreCase)
                             || name.Contains("littlefs", StringComparison.OrdinalIgnoreCase)
                             || (subtype == 0x82 && (name.Contains("data") || name.Length == 0));

                if (!isSpiffs)
                    continue;

                long offset = BitConverter.ToUInt32(bytes, i + 4);
                long size = BitConverter.ToUInt32(bytes, i + 8);
                _logger.Information("Found partition '{Name}' (type=0x{Type:X2}, subtype=0x{Sub:X2}) offset=0x{Offset:X} size=0x{Size:X}",
                    name, type, subtype, offset, size);
                return (offset, size);
            }

            return (0, 0);
        }

        /// <summary>
        /// Scans raw SPIFFS/LittleFS bytes for the "Z2S-" prefix of version.dat content.
        /// Returns the version segment (e.g. "1.5.1") from "Z2S-1.5.1-07/04/26".
        /// </summary>
        private string FindVersionInBytes(byte[] data)
        {
            var pattern = Encoding.ASCII.GetBytes("Z2S-");

            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j]) { match = false; break; }
                }
                if (!match) continue;

                // Read ahead for the full line (max 50 chars, stop at control chars or null)
                int end = i + pattern.Length;
                while (end < data.Length && end < i + 50
                       && data[end] >= 0x20 && data[end] < 0x7F)
                    end++;

                var versionLine = Encoding.ASCII.GetString(data, i, end - i);
                _logger.Information("Found Z2S version string: {Line}", versionLine);

                // "Z2S-1.5.1-07/04/26" → split('-') → [0]="Z2S" [1]="1.5.1" [2]="07/04/26"
                var parts = versionLine.Split('-');
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    return parts[1].Trim();
            }

            return null;
        }

        private async Task<string> GetLatestGitHubVersionAsync()
        {
            _logger.Information("Fetching latest Z2S_Library release from GitHub");

            var releases = await _githubClient.Repository.Release.GetAll(GitHubOwner, GitHubRepo);
            var latest = releases
                .Where(r => !r.Prerelease && !r.Draft)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (latest == null)
            {
                _logger.Warning("No releases found for {Owner}/{Repo}", GitHubOwner, GitHubRepo);
                return null;
            }

            _logger.Information("Latest Z2S_Library release tag: {Tag}", latest.TagName);
            return latest.TagName;
        }

        /// <summary>
        /// Extracts a plain version number from various tag formats:
        /// "v1.5.1", "1.5.1", "Z2S-1.5.1-07/04/26"
        /// </summary>
        private static string ExtractVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return tag;

            var trimmed = tag.TrimStart('v', 'V');
            if (Version.TryParse(trimmed, out _))
                return trimmed;

            // Try each segment separated by '-'
            foreach (var part in trimmed.Split('-'))
            {
                if (Version.TryParse(part.Trim(), out _))
                    return part.Trim();
            }

            return trimmed;
        }

        /// <summary>
        /// Creates a full-flash backup of the connected device to the specified directory.
        /// </summary>
        public async Task<Z2SBackupResult> BackupAsync(string comPort, string chip, string backupDirectory, CancellationToken cancellationToken = default)
        {
            _logger.Information("Starting Z2S backup on port {Port}, chip {Chip}", comPort, chip);

            try
            {
                if (!Directory.Exists(backupDirectory))
                    Directory.CreateDirectory(backupDirectory);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(backupDirectory, $"Z2S_Backup_{timestamp}.bin");

                var result = await _esptoolWrapper.ReadFlush(comPort, chip, backupFile, cancellationToken);

                if (result.Success && File.Exists(backupFile))
                {
                    _logger.Information("Z2S backup created: {File}", backupFile);
                    return new Z2SBackupResult { Success = true, BackupFilePath = backupFile };
                }

                _logger.Warning("Z2S backup failed: {Error}", result.StdErr);
                return new Z2SBackupResult { Success = false, Error = result.StdErr };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Z2S backup exception");
                return new Z2SBackupResult { Success = false, Error = ex.Message };
            }
        }

        /// <summary>
        /// Downloads the latest Z2S firmware .bin from GitHub and flashes it to the device.
        /// </summary>
        /// <param name="withLogs">True to use a firmware build with serial logs enabled.</param>
        /// <param name="fullVersion">True to flash the full image (erases SPIFFS); false for OTA-update-only image.</param>
        public async Task<Z2SFlashResult> DownloadAndFlashLatestAsync(
            string comPort,
            string chip,
            bool withLogs,
            bool fullVersion,
            Action<string> progress,
            CancellationToken cancellationToken = default)
        {
            _logger.Information("Starting Z2S upgrade on port {Port}, chip {Chip}", comPort, chip);

            string latestVersion = null;
            string downloadUrl = null;

            // 1. Find latest release asset
            try
            {
                progress?.Invoke("Pobieranie informacji o najnowszej wersji z GitHub…");
                var releases = await _githubClient.Repository.Release.GetAll(GitHubOwner, GitHubRepo);
                var latest = releases
                    .Where(r => !r.Prerelease && !r.Draft)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();

                if (latest == null)
                    return new Z2SFlashResult { Success = false, Error = "Nie znaleziono wydania w repozytorium GitHub." };

                latestVersion = latest.TagName;

                var targetFileName = GetFirmwareFileName(withLogs, fullVersion);
                _logger.Information("Looking for asset: {FileName}", targetFileName);

                var asset = latest.Assets
                    .FirstOrDefault(a => a.Name.Equals(targetFileName, StringComparison.OrdinalIgnoreCase));

                if (asset == null)
                    return new Z2SFlashResult { Success = false, Error = $"Nie znaleziono pliku '{targetFileName}' w wydaniu {latestVersion}." };

                downloadUrl = asset.BrowserDownloadUrl;
                _logger.Information("Z2S asset to download: {Name} ({Url})", asset.Name, downloadUrl);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to query GitHub releases");
                return new Z2SFlashResult { Success = false, Error = $"Błąd GitHub: {ex.Message}" };
            }

            // 2. Download to temp file
            var tempFile = Path.Combine(Path.GetTempPath(), $"z2s_fw_{Guid.NewGuid():N}.bin");
            try
            {
                progress?.Invoke($"Pobieranie firmware {latestVersion}…");
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromMinutes(5);
                var bytes = await http.GetByteArrayAsync(downloadUrl, cancellationToken);
                await File.WriteAllBytesAsync(tempFile, bytes, cancellationToken);
                _logger.Information("Z2S firmware downloaded to {File} ({Size} bytes)", tempFile, bytes.Length);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to download Z2S firmware");
                return new Z2SFlashResult { Success = false, Error = $"Błąd pobierania: {ex.Message}" };
            }

            // 3. Flash
            try
            {
                progress?.Invoke($"Wgrywanie firmware {latestVersion} na urządzenie…");
                var flashResult = await _esptoolWrapper.WriteFlush(comPort, chip, tempFile, cancellationToken);

                if (flashResult.Success)
                {
                    _logger.Information("Z2S firmware flashed successfully, version {Version}", latestVersion);
                    return new Z2SFlashResult { Success = true, FlashedVersion = latestVersion };
                }

                _logger.Warning("Z2S flash failed: {Error}", flashResult.StdErr);
                return new Z2SFlashResult { Success = false, Error = flashResult.StdErr };
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.Error(ex, "Z2S flash exception");
                return new Z2SFlashResult { Success = false, Error = ex.Message };
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }
    }
}
