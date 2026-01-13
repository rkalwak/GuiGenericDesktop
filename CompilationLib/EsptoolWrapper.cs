using System.Diagnostics;
using System.Text;

namespace CompilationLib
{
    public  class EsptoolWrapper : IEsptoolWrapper
    {
        private string _esptoolPath = "esptool.exe";
        string errors = string.Empty;
        string logs = string.Empty;
        public event EventHandler<string> OutputLine;
        public event EventHandler<string> ErrorLine;
        public async Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default)
            => await RunEsptoolAsync($"--port {EscapeArgument(comPort)} chip-id", cancellation);


        public async Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default)
            => await RunEsptoolAsync($"--port {EscapeArgument(comPort)} flash-id", cancellation);

        /// <summary>
        /// Runs esptool --chip esp32c6 --port {comPort} write-flash 0x000000 0x4000000 {binFile}
        /// </summary>
        public async Task<EsptoolResult> WriteFlush(string comPort, string chip, string binFile, CancellationToken cancellation = default)
            => await RunEsptoolAsync($"--chip {chip} --port {EscapeArgument(comPort)} --baud 921600 write-flash 0x000000 {EscapeArgument(binFile)}",
                                cancellation);

        /// <summary>
        /// Runs esptool --chip esp32c6 --port {comPort} read-flash 0x000000 ALL {backupFile}
        /// </summary>
        public async Task<EsptoolResult> ReadFlush(string comPort, string chip, string backupFile, CancellationToken cancellation = default)
            => await RunEsptoolAsync($"--chip {chip} --port {EscapeArgument(comPort)} --baud 921600 read-flash 0x000000 ALL {EscapeArgument(backupFile)}",
                               cancellation);

        /// <summary>
        /// Merges partition.bin, bootloader.bin and firmware.bin into a single complete firmware file using esptool merge_bin.
        /// The merged file can be flashed directly to address 0x0000 for a complete firmware installation.
        /// Addresses are parsed from the partition CSV file to ensure accuracy.
        /// </summary>
        /// <param name="buildOutputDirectory">Directory containing bootloader.bin, partitions.bin, and firmware.bin</param>
        /// <param name="outputFilePath">Path where the merged complete firmware file should be saved</param>
        /// <param name="platform">Platform name (e.g., "GUI_Generic_ESP32", "GUI_Generic_ESP32C6")</param>
        /// <param name="board">Board name (e.g., "ESP32", "ESP32-C3")</param>
        /// <param name="flashSize">Flash size (e.g., "4MB", "8MB")</param>
        /// <param name="repositoryPath">Path to GUI-Generic repository (to find partition CSV)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Path to the merged file if successful, null otherwise</returns>
        public async Task<string> MergeFirmwareFiles(string buildOutputDirectory, string outputFilePath, string platform, string flashSize, string board, string repositoryPath, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
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
;
                // Parse partition addresses from CSV if available
                int bootloaderOffset = 0x1000;  // Standard bootloader offset
                int partitionsOffset = 0x8000;  // Standard partitions offset
                int firmwareOffset = 0x10000;   // Default firmware offset

                // Try to get actual firmware offset from partition CSV
                if (!string.IsNullOrEmpty(repositoryPath) && !string.IsNullOrEmpty(flashSize) && 
                    !flashSize.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var partitionFilePath = PartitionManager.GetPartitionFilePath(platform, flashSize, repositoryPath, board);
                        if (!string.IsNullOrEmpty(partitionFilePath) && File.Exists(partitionFilePath))
                        {
                            Console.WriteLine($"  Reading partition layout from: {Path.GetFileName(partitionFilePath)}");
                            var layout = PartitionGenerator.GetPartitionLayout(partitionFilePath);
                            
                            if (layout != null && layout.App0Offset > 0)
                            {
                                firmwareOffset = layout.App0Offset;
                                Console.WriteLine($"  Firmware offset from partition CSV: 0x{firmwareOffset:X}");
                            }
                            else
                            {
                                Console.WriteLine($"  Using default firmware offset: 0x{firmwareOffset:X}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  Partition CSV not found, using default offsets");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: Failed to parse partition CSV: {ex.Message}");
                        Console.WriteLine($"  Using default firmware offset: 0x{firmwareOffset:X}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Using standard ESP32 offsets");
                }

                // Build esptool merge_bin command with parsed addresses
                // Format: esptool.py --chip {chip} merge_bin -o output.bin --flash_mode dio --flash_size 4MB 0x1000 bootloader.bin 0x8000 partitions.bin 0x10000 firmware.bin
                var arguments = $"--chip {board} merge-bin " +
                               $"-o {EscapeArgument(outputFilePath)} " +
                               $"--flash-mode dio " +
                               $"--flash-size {flashSize} " +
                               $"0x{bootloaderOffset:X} {EscapeArgument(bootloaderPath)} " +
                               $"0x{partitionsOffset:X} {EscapeArgument(partitionsPath)} " +
                               $"0x{firmwareOffset:X} {EscapeArgument(firmwarePath)}";

                Console.WriteLine($"  Running: esptool {arguments}");
                Console.WriteLine($"  Merge started at: {DateTime.Now:HH:mm:ss}");

                // Run esptool merge_bin command
                var mergeStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = await RunEsptoolAsync(arguments, cancellationToken);
                mergeStopwatch.Stop();

                Console.WriteLine($"  Merge operation completed in: {mergeStopwatch.Elapsed.TotalSeconds:F2}s");

                if (result.Success && File.Exists(outputFilePath))
                {
                    stopwatch.Stop();
                    
                    var fileInfo = new FileInfo(outputFilePath);
                    Console.WriteLine($"✓ Successfully created merged firmware: {Path.GetFileName(outputFilePath)}");
                    Console.WriteLine($"  Total size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0:F2} KB)");
                    Console.WriteLine($"  Bootloader: 0x{bootloaderOffset:X}");
                    Console.WriteLine($"  Partitions: 0x{partitionsOffset:X}");
                    Console.WriteLine($"  Firmware:   0x{firmwareOffset:X}");
                    Console.WriteLine($"  Total time: {stopwatch.Elapsed.TotalSeconds:F2}s");
                    Console.WriteLine($"  Flash command: esptool --chip {platform} write_flash 0x0 \"{Path.GetFileName(outputFilePath)}\"");

                    return outputFilePath;
                }
                else
                {
                    stopwatch.Stop();
                    Console.WriteLine($"⚠ Failed to merge firmware files (took {stopwatch.Elapsed.TotalSeconds:F2}s)");
                    if (!string.IsNullOrEmpty(result.StdErr))
                    {
                        Console.WriteLine($"  Error: {result.StdErr}");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"⚠ Error merging firmware files: {ex.Message} (took {stopwatch.Elapsed.TotalSeconds:F2}s)");
                return null;
            }
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            EsptoolResult result = null;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Executing esptool: {_esptoolPath} {arguments}");
            
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
                using (var process = new Process { StartInfo = psi })
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Process started");

                    process.EnableRaisingEvents = true;

                    // Capture output data
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            sbOut.AppendLine(e.Data);
                            Console.WriteLine($"[STDOUT] {e.Data}");
                            Debug.WriteLine($"[STDOUT] {e.Data}");
                            OutputLine?.Invoke(this, e.Data);
                        }
                    };

                    // Capture error data
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            sbErr.AppendLine(e.Data);
                            Console.WriteLine($"[STDERR] {e.Data}");
                            Debug.WriteLine($"[STDERR] {e.Data}");
                            ErrorLine?.Invoke(this, e.Data);
                        }
                    };

                    process.Start();
                    // Start async reading
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for process to exit
                    await process.WaitForExitAsync(cancellation);

                    stopwatch.Stop();

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Process exited with code: {process.ExitCode}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Total execution time: {stopwatch.Elapsed.TotalSeconds:F2}s");

                    result = new EsptoolResult
                    {
                        Command = $"{_esptoolPath} {arguments}",
                        ExitCode = process.ExitCode,
                        StdOut = sbOut.ToString(),
                        StdErr = sbErr.ToString(),
                        Success = process.ExitCode == 0
                    };

                    if (!result.Success)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Command failed with exit code: {process.ExitCode}");
                        if (!string.IsNullOrEmpty(result.StdErr))
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error output: {result.StdErr}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Command completed successfully");
                    }
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Operation canceled after {stopwatch.Elapsed.TotalSeconds:F2}s");
                return new EsptoolResult 
                { 
                    Command = $"{_esptoolPath} {arguments}", 
                    Success = false, 
                    ExitCode = -1, 
                    StdErr = "Operation canceled.",
                    StdOut = sbOut.ToString()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Exception after {stopwatch.Elapsed.TotalSeconds:F2}s: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stack trace: {ex.StackTrace}");
                return new EsptoolResult 
                { 
                    Command = $"{_esptoolPath} {arguments}", 
                    Success = false, 
                    ExitCode = -1, 
                    StdErr = ex.ToString(),
                    StdOut = sbOut.ToString()
                };
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
