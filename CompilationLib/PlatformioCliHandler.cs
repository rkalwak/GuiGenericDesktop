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

        // PlatformIO uses 'run' command for compilation
        CommentUnlistedFlagsBetweenMarkers($"{request.ProjectDirectory}/platformio.ini", request.BuildFlags);
        
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
                
                // Determine chip type from platform
                var chipType = request.Platform?.ToLowerInvariant() ?? "esp32";
                
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
        string arguments = $"run -d \"{request.ProjectDirectory}\" -e {request.Platform}";
        
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

            await process.WaitForExitAsync();
            stopwatch.Stop();
            compileResponse.HashOfOptions = BuildConfigurationHasher.CalculateHash(request.BuildFlags);
            compileResponse.IsSuccessful = process.ExitCode == 0;
            compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            compileResponse.OutputDirectory = $"{request.ProjectDirectory}/.pio/build/{request.Platform}";
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
    public void CommentUnlistedFlagsBetweenMarkers(string iniPath, List<BuildFlagItem> allowedFlags)
    {
        var lines = File.ReadAllLines(iniPath).ToList();
        var startIndex = lines.FindIndex(line => line.Trim().Equals(";flagsstart", StringComparison.OrdinalIgnoreCase));
        var endIndex = lines.FindIndex(line => line.Trim().Equals(";flagsend", StringComparison.OrdinalIgnoreCase));

        for (int i = startIndex + 1; i < endIndex; i++)
        {
            string lineContent = lines[i];
            string lineContentWithoutComment = lineContent.Contains(";") ? lineContent.Substring(1) : lineContent;
            bool isFlagEnabled = !lineContent.Contains(";");
            // flag already enabled, check if it should be enabled
            if (!string.IsNullOrWhiteSpace(lineContent) && isFlagEnabled)
            {
                // lineContent has format -D but collection doesn't
                if (!allowedFlags.Any(flag => !string.IsNullOrEmpty(flag?.Key) && lineContentWithoutComment.Contains(flag.Key)) && !_excludedBuildFlagsFromManipulation.Any(x => lineContentWithoutComment.Contains(x)))
                {
                    //comment out the line - remove one space
                    lines[i] = ";" + lines[i].Substring(1);
                }
            }
            // flag is commented out, check if it should be enabled
            else
            {
                if (allowedFlags.Any(flag => !string.IsNullOrEmpty(flag?.Key) && lineContentWithoutComment.Contains(flag.Key)))
                {
                    // Uncomment the line: replace first ';' with ' ' to preserve spacing
                    lines[i] = lines[i].Replace(';', ' ');
                }
            }
        }
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

                // Convert value to string safely
                var raw = (p.Value ?? string.Empty).Trim();

                // Format based on declared type: numbers as-is, strings quoted
                string value;
                if (string.Equals(p.Type, "number", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Type, "enum", StringComparison.OrdinalIgnoreCase))
                    value = string.IsNullOrEmpty(raw) ? "0" : raw;
                else // treat everything else as string
                    value = $"'\"{raw}\"'";

                // define is FLAGNAME_ParamIdentifier=Value
                var define = $" -D {flag.Key}_{identifier}={value}";
                var indexOfNewParameter = lines.FindIndex(line => line.Contains($"{flag.Key}_{identifier}"));
                if (indexOfNewParameter != -1)
                {
                    lines[indexOfNewParameter] = define;
                }
                else
                {
                    lines.Insert(endIndex, define);
                }
            }
        }

        File.WriteAllText(iniPath, string.Join("\n", lines) + "\n");
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
        logs += line+ "\r\n";
        OutputLine?.Invoke(this, line);
    }
}