using CompilationLib;
using System.Diagnostics;

public class PlatformioCliHandler : ICompileHandler
{
    string errors = string.Empty;
    string logs = string.Empty;
    string _platformioCliPath = string.Empty;
    List<string> _excludedBuildFlagsFromManipulation = new List<string>
                {
                    "SUPLA_EXCLUDE_LITTLEFS_CONFIG",
                    "TEMPLATE_BOARD_JSON",
                    "OPTIONS_HASH",
                    "BUILD_VERSION",
                };

    public event EventHandler<string> OutputLine;
    public event EventHandler<string> ErrorLine;

    public PlatformioCliHandler()
    {
        _platformioCliPath = $"{Environment.ExpandEnvironmentVariables("%USERPROFILE%")}/.platformio/penv/Scripts/platformio.exe";
    }

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        var compileResponse = new CompileResponse();
        
        // Ensure partition files exist in the repository
        try
        {
            Console.WriteLine("=== Checking Partition Files ===");
            var filesCreated = PartitionGenerator.EnsurePartitionFilesExist(request.ProjectDirectory);
            if (filesCreated > 0)
            {
                Console.WriteLine($"? Created {filesCreated} partition file(s)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Warning: Failed to create partition files: {ex.Message}");
            // Continue anyway - may use existing partitions
        }
        
        // Set partition scheme based on flash size (before modifying flags)
        SetPartitionScheme(request.ProjectDirectory, request.EnvironmentName, request.FlashSize, request.Board);
      
        // PlatformIO uses 'run' command for compilation
        CommentUnlistedFlagsBetweenMarkers($"{request.ProjectDirectory}/platformio.ini", request.BuildFlags, request.GlobalSettings);

        // Create backup before deployment if both deploying and backup are enabled
        if (request.ShouldDeploy && request.ShouldBackup && !string.IsNullOrEmpty(request.PortCom))
        {
            try
            {
                Console.WriteLine("=== Creating Flash Backup ===");

                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                var backupManager = new BackupManager(backupDir, new EsptoolWrapper());

                // Generate encoded config for this build
                var encodedConfig = BuildConfigurationHasher.EncodeOptions(request.BuildFlags);

                // Determine chip type from board
                var chipType = request.Board?.ToLowerInvariant() ?? "esp32";

                var backupPath = await backupManager.CreateBackupAsync(
                    request.PortCom,
                    chipType,
                    encodedConfig,
                    cancellationToken);

                if (!string.IsNullOrEmpty(backupPath))
                {
                    Console.WriteLine($"? Backup saved to: {backupPath}");
                    compileResponse.BackupFilePath = backupPath;
                }
                else
                {
                    Console.WriteLine("? Warning: Backup creation failed, but continuing with compilation...");
                    // Don't fail the build if backup fails - it's a nice-to-have feature
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Backup error: {ex.Message}");
                Console.WriteLine("? Continuing with compilation despite backup failure...");
                // Don't fail the build if backup fails
            }
        }
        else if (request.ShouldDeploy && !request.ShouldBackup)
        {
            Console.WriteLine("? Backup skipped (Backup checkbox is unchecked)");
        }

        // Build the arguments for PlatformIO run command
        string arguments = $"run -d \"{request.ProjectDirectory}\" -e {request.EnvironmentName}";

        // Add erase target if enabled (before upload)
        if (request.ShouldDeploy && request.ShouldEraseFlash)
        {
            arguments += " --target erase";
            Console.WriteLine("? Flash will be erased before upload");
        }

        // Add upload target if deploying
        if (request.ShouldDeploy)
        {
            arguments += $" --target upload --upload-port {request.PortCom}";
        }

        // Add verbose flag
        arguments += " --verbose";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _platformioCliPath,
            WorkingDirectory = request.ProjectDirectory,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Console.WriteLine($"Compiling: {processStartInfo.FileName} {arguments}");

        using (var process = new Process { StartInfo = processStartInfo })
        {
            process.EnableRaisingEvents = true;

            process.Exited += (sender, e) =>
            {
                Console.WriteLine($"Process exited with code: {process.ExitCode}");
                Debug.WriteLine($"Process exited with code: {process.ExitCode}");
            };
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            Stopwatch stopwatch = Stopwatch.StartNew();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            stopwatch.Stop();
            compileResponse.IsSuccessful = process.ExitCode == 0;
            compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            compileResponse.OutputDirectory = $"{request.ProjectDirectory}/.pio/build/{request.EnvironmentName}";
            compileResponse.OutputFile = $"firmware.bin";
            compileResponse.Logs = "Errors:\r\n" + errors;
        }

        return compileResponse;
    }

    /// <summary>
    /// Comments out all entries between ;flagsstart and ;flagsend that are not in the allowedFlags list.
    /// </summary>
    /// <param name="iniPath">Path to platformio.ini file.</param>
    /// <param name="allowedFlags">List of allowed build flag strings (e.g., "-D SUPLA_AHTX0").</param>
    /// <param name="globalSettings">Global settings containing globally defined parameters (e.g., SCL/SDA for I2C devices).</param>
    public void CommentUnlistedFlagsBetweenMarkers(string iniPath, List<BuildFlagItem> allowedFlags, GlobalSettings globalSettings)
    {
        var lines = File.ReadAllLines(iniPath).ToList();
        var startIndex = lines.FindIndex(line => line.Trim().Equals(";flagsstart", StringComparison.OrdinalIgnoreCase));
        var endIndex = lines.FindIndex(line => line.Trim().Equals(";flagsend", StringComparison.OrdinalIgnoreCase));

        for (int i = startIndex + 1; i < endIndex; i++)
        {
            string lineContent = lines[i];
            string lineContentWithoutComment = lineContent.Contains(";") ? lineContent.Substring(1) : lineContent;
            lineContentWithoutComment = lineContentWithoutComment.Replace("-D ", "").Replace(";","").Trim();
            bool isFlagEnabled = !lineContent.Contains(";");
            // flag already enabled, check if it should be enabled
            if (!string.IsNullOrWhiteSpace(lineContent) && isFlagEnabled)
            {
                // lineContent has format -D but collection doesn't
                if (!allowedFlags.Any(flag => !string.IsNullOrEmpty(flag?.Key) && lineContentWithoutComment == flag.Key) && !_excludedBuildFlagsFromManipulation.Any(x => lineContentWithoutComment.Contains(x)))
                {
                    //comment out the line - remove one space
                    lines[i] = ";" + lines[i].Substring(1);
                }
            }
            // flag is commented out, check if it should be enabled
            else
            {
                if (allowedFlags.Any(flag => !string.IsNullOrEmpty(flag?.Key) && lineContentWithoutComment == flag.Key))
                {
                    // Uncomment the line: replace first ';' with ' ' to preserve spacing
                    lines[i] = lines[i].Replace(';', ' ');
                }
            }
        }

        // Process global parameters first (only once)
        // Collect values from BuildFlags for parameters that match global parameter definitions
        var globalParametersWritten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (globalSettings?.Parameters != null && globalSettings.Parameters.Any())
        {
            foreach (var globalParam in globalSettings.Parameters)
            {
                if (globalParam == null || string.IsNullOrEmpty(globalParam.Identifier))
                    continue;

                var identifier = globalParam.Identifier.Trim();
                
                // Try to find the value from any BuildFlag that has this parameter
                string valueToUse = null;
                Parameter matchingFlagParameter = null;
                
                // Search through all enabled flags to find a parameter with matching identifier
                foreach (var flag in allowedFlags)
                {
                    if (flag.Parameters != null)
                    {
                        matchingFlagParameter = flag.Parameters.FirstOrDefault(p => 
                            p != null && 
                            !string.IsNullOrEmpty(p.Identifier) &&
                            string.Equals(p.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
                        
                        if (matchingFlagParameter != null && !string.IsNullOrEmpty(matchingFlagParameter.Value))
                        {
                            valueToUse = matchingFlagParameter.Value.Trim();
                            break; // Use the first matching value found
                        }
                    }
                }
                
                // If no value found in BuildFlags, fall back to GlobalSettings value
                if (string.IsNullOrEmpty(valueToUse))
                {
                    valueToUse = (globalParam.Value ?? string.Empty).Trim();
                }

                // Skip optional global parameters without values
                if (!globalParam.IsRequired && string.IsNullOrEmpty(valueToUse))
                    continue;

                // Format value based on type (use matchingFlagParameter type if found, otherwise globalParam type)
                var paramType = matchingFlagParameter?.Type ?? globalParam.Type;
                string value;
                if (string.Equals(paramType, "number", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(paramType, "enum", StringComparison.OrdinalIgnoreCase))
                    value = string.IsNullOrEmpty(valueToUse) ? "0" : valueToUse;
                else
                    value = $"'\"{valueToUse}\"'";// Global parameters use GlobalParameter_ prefix
                var paramDefineName = $"GlobalParameter_{identifier}";
                var indexOfExistingParameter = lines.FindIndex(line => line.Contains(paramDefineName));
                var define = $" -D {paramDefineName}={value}";

                if (indexOfExistingParameter != -1)
                {
                    lines[indexOfExistingParameter] = define;
                }
                else
                {
                    lines.Insert(endIndex, define);
                    endIndex++; // Adjust endIndex since we inserted a line
                }

                globalParametersWritten.Add(identifier);
            }
        }

        // Process flag-specific parameters (skip global ones)
        foreach (var flag in allowedFlags)
        {
            if (flag.Parameters is null || flag.Parameters.Count == 0)
            {
                continue;
            }

            foreach (var p in flag.Parameters)
            {
                if (p == null)
                    continue;

                // Use Identifier property which prefers Key over Name
                var identifier = (p.Identifier ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(identifier))
                    continue;

                // Skip this parameter if it's defined as a global parameter
                if (globalParametersWritten.Contains(identifier))
                    continue;

                // Convert value to string safely
                var raw = (p.Value ?? string.Empty).Trim();

                // Check if parameter is optional and has no value
                bool isOptionalWithoutValue = !p.IsRequired && string.IsNullOrEmpty(raw);

                var paramDefineName = $"{flag.Key}_{identifier}";
                var indexOfExistingParameter = lines.FindIndex(line => line.Contains(paramDefineName));

                if (isOptionalWithoutValue)
                {
                    // If optional parameter has no value:
                    // - Comment it out if it exists in the file
                    // - Don't add it if it doesn't exist
                    if (indexOfExistingParameter != -1)
                    {
                        var existingLine = lines[indexOfExistingParameter];
                        // Comment out if not already commented
                        if (!existingLine.TrimStart().StartsWith(";"))
                        {
                            // Find the first non-whitespace character and add ; before it
                            var trimmedStart = existingLine.TrimStart();
                            var leadingWhitespace = existingLine.Substring(0, existingLine.Length - trimmedStart.Length);
                            lines[indexOfExistingParameter] = leadingWhitespace + ";" + trimmedStart;
                        }
                    }
                    // If it doesn't exist, don't add it (skip to next parameter)
                    continue;
                }

                // Parameter has a value or is required, process it normally
                // Format based on declared type: numbers as-is, strings quoted
                string value;
                if (string.Equals(p.Type, "number", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Type, "enum", StringComparison.OrdinalIgnoreCase))
                    value = string.IsNullOrEmpty(raw) ? "0" : raw;
                else // treat everything else as string
                    value = $"'\"{raw}\"'";

                // define is FLAGNAME_ParamIdentifier=Value
                var define = $" -D {paramDefineName}={value}";

                if (indexOfExistingParameter != -1)
                {
                    lines[indexOfExistingParameter] = define;
                }
                else
                {
                    lines.Insert(endIndex, define);
                    endIndex++; // Adjust endIndex since we inserted a line
                }
            }
        }

        File.WriteAllText(iniPath, string.Join("\n", lines) + "\n");
    }

    /// <summary>
    /// Sets the partition scheme in platformio.ini based on platform and flash size
    /// </summary>
    /// <param name="projectDirectory">Path to project directory</param>
    /// <param name="platform">Platform name (e.g., "GUI_Generic_ESP32")</param>
    /// <param name="flashSize">Flash size (e.g., "4MB", "8MB")</param>
    /// <param name="board">Board name (e.g., "ESP32", "ESP32-C3")</param>
    private void SetPartitionScheme(string projectDirectory, string platform, string flashSize, string board)
    {
        if (string.IsNullOrEmpty(flashSize) || flashSize.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("? No flash size specified, using default partition scheme");
            return;
        }

        // Get partition scheme for this platform/flash size combination
        var scheme = PartitionManager.GetPartitionScheme(flashSize, board);
        if (scheme == null)
        {
            Console.WriteLine($"? Warning: No partition scheme found for {platform}/{flashSize}, using default");
            return;
        }

        Console.WriteLine($"=== Setting Partition Scheme ===");
        Console.WriteLine($"  Platform: {platform}");
        Console.WriteLine($"  Flash Size: {flashSize}");
        Console.WriteLine($"  Partition File: {scheme.FileName}");

        var iniPath = Path.Combine(projectDirectory, "platformio.ini");
        if (!File.Exists(iniPath))
        {
            Console.WriteLine($"? Warning: platformio.ini not found at {iniPath}");
            return;
        }

        try
        {
            var lines = File.ReadAllLines(iniPath).ToList();
            
            // Look for the environment section for this platform
            var envSection = $"[env:{platform}]";
            var envIndex = lines.FindIndex(line => line.Trim().Equals(envSection, StringComparison.OrdinalIgnoreCase));
            
            if (envIndex == -1)
            {
                Console.WriteLine($"? Warning: Environment section {envSection} not found in platformio.ini");
                return;
            }

            // Find the next section or end of file
            var nextSectionIndex = lines.FindIndex(envIndex + 1, line => line.TrimStart().StartsWith("["));
            if (nextSectionIndex == -1)
                nextSectionIndex = lines.Count;

            // Look for existing board_build.partitions line
            var partitionsKey = "board_build.partitions";
            var partitionsIndex = -1;
            
            for (int i = envIndex + 1; i < nextSectionIndex; i++)
            {
                if (lines[i].TrimStart().StartsWith(partitionsKey, StringComparison.OrdinalIgnoreCase))
                {
                    partitionsIndex = i;
                    break;
                }
            }

            var partitionsValue = $"board_build.partitions = partitions/{scheme.FileName}";

            if (partitionsIndex != -1)
            {
                // Update existing line
                lines[partitionsIndex] = partitionsValue;
                Console.WriteLine($"  Updated existing partition configuration");
            }
            else
            {
                // Add new line after environment header
                lines.Insert(envIndex + 1, partitionsValue);
                Console.WriteLine($"  Added partition configuration");
            }

            File.WriteAllText(iniPath, string.Join("\n", lines) + "\n");
            Console.WriteLine($"? Partition scheme configured successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Warning: Failed to set partition scheme: {ex.Message}");
            // Continue compilation with default partitions
        }
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data); // Log the output to the console
        Debug.WriteLine(e.Data);
        var line = e.Data ?? string.Empty;
        errors += line + "\r\n";
        ErrorLine?.Invoke(this, line);
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data); // Log the output to the console
        Debug.WriteLine(e.Data);
        var line = e.Data ?? string.Empty;
        logs += line + "\r\n";
        OutputLine?.Invoke(this, line);
    }
}