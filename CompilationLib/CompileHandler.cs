using CompilationLib;
using System.Diagnostics;

public class CompileHandler : ICompileHandler
{
    public CompileHandler()
    {

    }

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        var compileResponse = new CompileResponse();
        string buildFlagsString = BuildFlagsStringForCompilation(request.BuildFlags);
        string arguments = $"compile {request.ProjectPath}{(request.LibrariesPath != null ? " --libraries " + request.LibrariesPath : string.Empty)} --fqbn {request.Platform} --verbose --log --build-property build.flags=\"{buildFlagsString}\" --output-dir \"{request.ProjectDirectory}/build\"";
        var processStartInfo = new ProcessStartInfo
        {
            FileName = @"arduino-cli.exe",
            WorkingDirectory = request.ProjectDirectory,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        Console.WriteLine($"Compiling:{processStartInfo.FileName} {arguments}");

        if (Directory.Exists($"{request.ProjectDirectory}/build"))
        {
            Directory.Delete($"{request.ProjectDirectory}/build", recursive: true);
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
            compileResponse.OutputDirectory = $"{request.ProjectDirectory}/build";
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
            if (string.IsNullOrWhiteSpace(flag.Key))
                continue;

            // Add canonical flag token
            parts.Add($"-D {flag.Key.Trim()}");

            if (flag.Parameters == null)
                continue;

            foreach (var p in flag.Parameters)
            {
                if (p == null)
                    continue;

                var name = (p.Name ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                // Convert value to string safely
                var raw = (p.Value?.ToString() ?? string.Empty).Trim();

                // Format based on declared type: numbers as-is, strings quoted
                string value;
                if (string.Equals(p.Type, "number", StringComparison.OrdinalIgnoreCase))
                    value = string.IsNullOrEmpty(raw) ? "0" : raw;
                else // treat everything else as string
                    value = string.IsNullOrEmpty(raw) ? "\"\"" : $"'\"{raw}\"'";

                // define is FLAGNAME_ParamName=Value
                var define = $"{flag.FlagName}_{p.Name}={value}";

                parts.Add($"-D {define}");
            }
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
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
