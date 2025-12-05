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
    public PlatformioCliHandler()
    {
        _platformioCliPath = $"{Environment.ExpandEnvironmentVariables("%USERPROFILE%")}/.platformio/penv/Scripts/platformio.exe";
    }

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        var compileResponse = new CompileResponse();
        string buildFlagsString = FormatBuildFlags(request.BuildFlags);

        // PlatformIO uses 'run' command for compilation
        CommentUnlistedFlagsBetweenMarkers($"{request.ProjectDirectory}/platformio.ini", request.BuildFlags);
        string arguments = $"run -d \"{request.ProjectDirectory}\" -e {request.Platform} {(request.ShouldDeploy ? ("--target upload--upload-port " + request.PortCom): " ")}--verbose";

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

        /*
        if (Directory.Exists($"{request.ProjectDirectory}/.pio/build"))
        {
            Directory.Delete($"{request.ProjectDirectory}/.pio/build", recursive: true);
        }
        */

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

            process.WaitForExit();
            stopwatch.Stop();

            compileResponse.IsSuccessful = process.ExitCode==0;
            compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            compileResponse.OutputDirectory = $"{request.ProjectDirectory}/.pio/build/{request.Platform}";
            compileResponse.OutputFile = $"firmware.bin";
            compileResponse.Logs ="Errors:\r\n"+ errors;
        }

        return compileResponse;
    }



    /// <summary>
    /// Comments out all entries between ;flagsstart and ;flagsend that are not in the allowedFlags list.
    /// </summary>
    /// <param name="iniPath">Path to platformio.ini file.</param>
    /// <param name="allowedFlags">List of allowed build flag strings (e.g., "-D SUPLA_AHTX0").</param>
    public void CommentUnlistedFlagsBetweenMarkers(string iniPath, List<string> allowedFlags)
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
                if (!allowedFlags.Any(flag => lineContentWithoutComment.Contains(flag)) && !_excludedBuildFlagsFromManipulation.Any(x=> lineContentWithoutComment.Contains(x)))
                {
                    //comment out the line - remove one space
                    lines[i] = ";" + lines[i].Substring(1);
                }
            }
            // flag is commented out, check if it should be enabled
            else
            {
                if (allowedFlags.Any(flag => lineContentWithoutComment.Contains(flag)))
                {
                    // Uncomment the line
                    lines[i] = lines[i].Replace(';',' ');
                }
            }
        }

        File.WriteAllText(iniPath, string.Join("\n", lines) + "\n");
    }


    private static string FormatBuildFlags(IEnumerable<string> flags)
    {
        if (flags == null)
            return string.Empty;

        var parts = flags
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => $"-D {f.Trim()}");

        return string.Join(" ", parts);
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data); // Log the output to the console
        Debug.WriteLine(e.Data);
        errors += e.Data;
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data); // Log the output to the console
        Debug.WriteLine(e.Data);
        logs += e.Data;
    }
}