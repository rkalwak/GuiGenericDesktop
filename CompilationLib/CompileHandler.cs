using CompilationLib;
using System.Diagnostics;
using System.Globalization;

public class CompileHandler : ICompileHandler
{
    public CompileHandler()
    {

    }

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        var compileResponse = new CompileResponse();
        string buildFlagsString = BuildFlagsStringForCompilation(request.BuildFlags);
        var buildDir = Path.Combine(request.ProjectDirectory, "build");
        string arguments = $"compile {request.ProjectPath}{(request.LibrariesPath != null ? " --libraries " + request.LibrariesPath : string.Empty)} --fqbn {request.EnvironmentName} --verbose --log --build-property build.flags=\"{buildFlagsString}\" --output-dir \"{buildDir}\"";
        
        // Arduino CLI executable name depends on platform
        var arduinoCliExe = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) 
            ? "arduino-cli.exe" 
            : "arduino-cli";
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = arduinoCliExe,
            WorkingDirectory = request.ProjectDirectory,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        Console.WriteLine($"Compiling:{processStartInfo.FileName} {arguments}");

        if (Directory.Exists(buildDir))
        {
            Directory.Delete(buildDir, recursive: true);
        }

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
            Stopwatch stopwatch = new Stopwatch();
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
         
            process.WaitForExit();
            stopwatch.Stop();

            compileResponse.IsSuccessful = process.ExitCode==0;
            compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            compileResponse.OutputDirectory = Path.Combine(request.ProjectDirectory, "build");
        }

        return compileResponse;
    }

    /// <summary>
    /// Produce the final build.flags string for the compiler.
    /// This merges previous BuildFlagsForCompilation + FormatBuildFlags into one method.
    /// Each enabled flag contributes:
    ///   - a plain token for the flag key (formatted as "-D FLAG")
    ///   - for each parameter a define "FLAG_PARAM=VALUE" which is also prefixed with "-D"
    /// The returned string is the space-separated sequence of "-D ..." tokens.
    /// </summary>
    private static string BuildFlagsStringForCompilation(List<BuildFlagItem> userBuildFlags)
    {
        if (userBuildFlags == null)
            return string.Empty;

        var parts = new List<string>();

        foreach (var flag in userBuildFlags.Where(f => f.IsEnabled))
        {
            var flagKey = (flag.Key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(flagKey))
                continue;

            // Add canonical flag token
            parts.Add($"-D {flagKey}");

            if (flag.Parameters == null)
                continue;

            foreach (var p in flag.Parameters)
            {
                if (p == null)
                    continue;

                var parameterKey = (p.Key ?? p.Name ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(parameterKey))
                    continue;

                var raw = (p.Value?.ToString() ?? string.Empty).Trim();
                var type = p.Type ?? string.Empty;

                string value;
                if (string.Equals(type, "number", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "gpio", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "enum", StringComparison.OrdinalIgnoreCase) ||
                    IsNumericLike(raw))
                {
                    value = string.IsNullOrEmpty(raw) ? "0" : raw;
                }
                else if (string.Equals(type, "bool", StringComparison.OrdinalIgnoreCase))
                {
                    value = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) || raw == "1" ? "1" : "0";
                }
                else
                {
                    value = string.IsNullOrEmpty(raw) ? "\"\"" : $"'\"{raw}\"'";
                }

                string define;
                if (parameterKey.StartsWith("Parameter_", StringComparison.OrdinalIgnoreCase))
                {
                    define = $"{parameterKey}={value}";
                }
                else
                {
                    define = $"{flagKey}_{parameterKey}={value}";
                }

                parts.Add($"-D {define}");
            }
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static bool IsNumericLike(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
               decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out _);
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
