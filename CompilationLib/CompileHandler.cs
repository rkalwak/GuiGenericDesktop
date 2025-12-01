using CompilationLib;
using System.Diagnostics;
using System.Linq;
using System.IO;

public class CompileHandler : ICompileHandler
{
    public CompileHandler()
    {

    }

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        var compileResponse = new CompileResponse();
        string buildFlagsString = FormatBuildFlags(request.BuildFlags);
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

            compileResponse.ExitCode = process.ExitCode;
            compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            compileResponse.OutputDirectory = $"{request.ProjectDirectory}/build";
        }

        return compileResponse;
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
