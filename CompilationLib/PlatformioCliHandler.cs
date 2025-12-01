using CompilationLib;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

public class PlatformioCliHandler : ICompileHandler
{
    public PlatformioCliHandler()
    {

    }

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        var compileResponse = new CompileResponse();
        string buildFlagsString = FormatBuildFlags(request.BuildFlags);

        // PlatformIO uses 'run' command for compilation
        CommentUnlistedFlagsBetweenMarkers($"{request.ProjectDirectory}/platformio.ini", request.BuildFlags);
        string arguments = $"run -d \"{request.ProjectDirectory}\" -e {request.Platform} --target upload --upload-port {request.PortCom}  --verbose";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = $"{Environment.ExpandEnvironmentVariables("%USERPROFILE%")}/.platformio/penv/Scripts/platformio.exe",
            WorkingDirectory = request.ProjectDirectory,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
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

            compileResponse.ExitCode = process.ExitCode;
            compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            compileResponse.OutputDirectory = $"{request.ProjectDirectory}/.pio/build/{request.Platform}";
            compileResponse.OutputFile = $"firmware.bin";
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
            string lineContent = lines[i].TrimStart();
            string lineContentWithoutComment = lineContent.StartsWith(";") ? lineContent.Substring(1) : lineContent;
            bool isFlagEnabled = !lineContent.StartsWith(";");
            // flag already enabled, check if it should be enabled
            if (!string.IsNullOrWhiteSpace(lineContent) && isFlagEnabled)
            {
                if (!allowedFlags.Any(flag => lineContentWithoutComment.Contains(flag)))
                {
                    //comment out the line
                    lines[i] = ";" + lines[i];
                }
            }
            // flag is commented out, check if it should be enabled
            else
            {
                if (allowedFlags.Any(flag => lineContentWithoutComment.Contains(flag)))
                {
                    // Uncomment the line
                    lines[i] = lines[i].TrimStart().Substring(1); 
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
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data); // Log the output to the console
        Debug.WriteLine(e.Data);
    }
}