using System.Diagnostics;
using System.Text;

namespace CompilationLib
{
    public  class EsptoolWrapper : IEsptoolWrapper
    {
        private string _esptoolPath = "esptool.exe";

        public Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--port {EscapeArgument(comPort)} chip-id", cancellation);


        public Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--port {EscapeArgument(comPort)} flash-id", cancellation);

        /// <summary>
        /// Runs esptool --chip esp32c6 --port {comPort} write-flash 0x000000 0x4000000 {binFile}
        /// </summary>
        public Task<EsptoolResult> WriteFlush(string comPort, string chip, string binFile, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--chip {chip} --port {EscapeArgument(comPort)} --baud 921600 write-flash 0x000000 {EscapeArgument(binFile)}",
                                cancellation);

        /// <summary>
        /// Runs esptool --chip esp32c6 --port {comPort} read-flash 0x000000 0x4000000 {backupFile}
        /// </summary>
        public Task<EsptoolResult> ReadFlush(string comPort, string chip, string backupFile, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--chip {chip} --port {EscapeArgument(comPort)} --baud 921600 read-flash 0x000000 0x400000 {EscapeArgument(backupFile)}",
                               cancellation);

        /// <summary>
        /// Merges partition.bin, bootloader.bin and firmware.bin into a single complete firmware file using esptool merge_bin.
        /// The merged file can be flashed directly to address 0x0000 for a complete firmware installation.
        /// </summary>
        /// <param name="buildOutputDirectory">Directory containing bootloader.bin, partitions.bin, and firmware.bin</param>
        /// <param name="outputFilePath">Path where the merged complete firmware file should be saved</param>
        /// <param name="platform">Platform name (e.g., "GUI_Generic_ESP32", "GUI_Generic_ESP32C6")</param>
        /// <returns>Path to the merged file if successful, null otherwise</returns>
        public async Task<string> MergeFirmwareFiles(string buildOutputDirectory, string outputFilePath, string platform, string flashSize)
        {
            try
            {
                Console.WriteLine($"=== Merging firmware files for {platform} using esptool ===");

                if (!Directory.Exists(buildOutputDirectory))
                {
                    Console.WriteLine($"Build output directory not found: {buildOutputDirectory}");
                    return null;
                }

                // Get file paths
                var bootloaderPath = Path.Combine(buildOutputDirectory, "bootloader.bin");
                var partitionsPath = Path.Combine(buildOutputDirectory, "partitions.bin");
                var firmwarePath = Path.Combine(buildOutputDirectory, "firmware.bin");

                // Check if all required files exist
                if (!File.Exists(bootloaderPath))
                {
                    Console.WriteLine($"⚠ Warning: bootloader.bin not found at {bootloaderPath}");
                    return null;
                }

                if (!File.Exists(partitionsPath))
                {
                    Console.WriteLine($"⚠ Warning: partitions.bin not found at {partitionsPath}");
                    return null;
                }

                if (!File.Exists(firmwarePath))
                {
                    Console.WriteLine($"⚠ Warning: firmware.bin not found at {firmwarePath}");
                    return null;
                }

                // Get chip type for target-specific handling
                var chipType = GetChipFromPlatform(platform);

                // Build esptool merge_bin command
                // Format: esptool.py --chip {chip} merge_bin -o output.bin --flash_mode dio --flash_size 4MB 0x1000 bootloader.bin 0x8000 partitions.bin 0x10000 firmware.bin
                var arguments = $"--chip {chipType} merge_bin " +
                               $"-o {EscapeArgument(outputFilePath)} " +
                               $"--flash_mode dio " +
                               $"--flash_size {flashSize} " +
                               $"0x1000 {EscapeArgument(bootloaderPath)} " +
                               $"0x8000 {EscapeArgument(partitionsPath)} " +
                               $"0x10000 {EscapeArgument(firmwarePath)}";

                Console.WriteLine($"  Running: esptool {arguments}");

                // Run esptool merge_bin command
                var result = await RunEsptoolAsync(arguments, CancellationToken.None);

                if (result.Success && File.Exists(outputFilePath))
                {
                    var fileInfo = new FileInfo(outputFilePath);
                    Console.WriteLine($"✓ Successfully created merged firmware: {Path.GetFileName(outputFilePath)}");
                    Console.WriteLine($"  Total size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0:F2} KB)");
                    Console.WriteLine($"  Flash command: esptool --chip {chipType} write_flash 0x0 \"{Path.GetFileName(outputFilePath)}\"");

                    return outputFilePath;
                }
                else
                {
                    Console.WriteLine($"⚠ Failed to merge firmware files");
                    if (!string.IsNullOrEmpty(result.StdErr))
                    {
                        Console.WriteLine($"  Error: {result.StdErr}");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error merging firmware files: {ex.Message}");
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
            
            return "esp32"; // Default
        }

        private static string EscapeArgument(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            if (value.Contains(' ') || value.Contains('\t') || value.Contains('"'))
                return "\"" + value.Replace("\"", "\\\"") + "\"";
            return value;
        }

        private async Task<EsptoolResult> RunEsptoolAsync(string arguments, CancellationToken cancellation)
        {

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            Console.WriteLine(arguments);
            var psi = new ProcessStartInfo
            {
                FileName = _esptoolPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start esptool process.");
                var outTask = proc.StandardOutput.ReadToEndAsync();
                var errTask = proc.StandardError.ReadToEndAsync();
                await Task.WhenAll(outTask, errTask).ConfigureAwait(false);
                sbOut.AppendLine(outTask.Result);
                sbErr.AppendLine(errTask.Result);

                // Wait for exit with cancellation support
                using var reg = cancellation.Register(() =>
                {
                    try { if (!proc.HasExited) proc.Kill(true); } catch { }
                });

                await proc.WaitForExitAsync(cancellation).ConfigureAwait(false);

                var result = new EsptoolResult
                {
                    Command = $"{_esptoolPath} {arguments}",
                    ExitCode = proc.ExitCode,
                    StdOut = sbOut.ToString(),
                    StdErr = sbErr.ToString(),
                    Success = proc.ExitCode == 0
                };

                Console.WriteLine(result.StdOut);
                if (!string.IsNullOrEmpty(result.StdErr))
                    Console.Error.WriteLine(result.StdErr);

                return result;
            }
            catch (OperationCanceledException)
            {
                return new EsptoolResult { Command = $"{_esptoolPath} {arguments}", Success = false, ExitCode = -1, StdErr = "Operation canceled." };
            }
            catch (Exception ex)
            {
                return new EsptoolResult { Command = $"{_esptoolPath} {arguments}", Success = false, ExitCode = -1, StdErr = ex.ToString() };
            }
        }
    }

    public record EsptoolResult
    {
        public bool Success { get; init; }
        public string Command { get; init; }
        public int ExitCode { get; init; }
        public string StdOut { get; init; }
        public string StdErr { get; init; }

        public override string ToString()
        {
            return $"EsptoolResult: Success={Success}, ExitCode={ExitCode}, Command={Command}, Output={StdOut}, Error={StdErr}";
        }
    }
}
