using System.Diagnostics;
using System.Text;
/// <summary>
/// Simple wrapper to run esptool commands for install / backup / restore operations.
/// Designed to mirror the provided batch logic but as cancellable async methods.
/// </summary>
public static class EsptoolRunner
{
    /// <summary>
    /// Runs esptool --chip esp32c6 --port {comPort} write-flash {code} 0x160000 {binFile}
    /// </summary>
    public static Task<EsptoolResult> InstallAsync(string comPort, string code, string binFile, string? esptoolPath = null, bool pauseAfter = false, CancellationToken cancellation = default)
        => RunEsptoolAsync(esptoolPath ?? "esptool.exe",
                           $"--chip esp32c6 --port {EscapeArgument(comPort)} write-flash {code} 0x160000 {EscapeArgument(binFile)}",
                           showCommand: true, pauseAfter: pauseAfter, cancellation: cancellation);

    /// <summary>
    /// Runs esptool --chip esp32c6 --port {comPort} read-flash {code} 0x160000 {backupFile}
    /// </summary>
    public static Task<EsptoolResult> BackupAsync(string comPort, string code, string backupFile, string? esptoolPath = null, CancellationToken cancellation = default)
        => RunEsptoolAsync(esptoolPath ?? "esptool.exe",
                           $"--chip esp32c6 --port {EscapeArgument(comPort)} read-flash {code} 0x160000 {EscapeArgument(backupFile)}",
                           showCommand: true, pauseAfter: false, cancellation: cancellation);

    /// <summary>
    /// RESTORE: by default performs write-flash (typical restore). To exactly reproduce the original batch which used read-flash in :RESTORE,
    /// set useReadFlash = true.
    /// </summary>
    public static Task<EsptoolResult> RestoreAsync(string comPort, string espType, string code, string fileName, string? esptoolPath = null, bool useReadFlash = false, bool pauseAfter = false, CancellationToken cancellation = default)
    {
        var cmd = useReadFlash
            ? $"--chip {EscapeArgument(espType)} --port {EscapeArgument(comPort)} read-flash {code} 0x160000 {EscapeArgument(fileName)}"
            : $"--chip {EscapeArgument(espType)} --port {EscapeArgument(comPort)} write-flash {code} 0x160000 {EscapeArgument(fileName)}";

        return RunEsptoolAsync(esptoolPath ?? "esptool.exe", cmd, showCommand: true, pauseAfter: pauseAfter, cancellation: cancellation);
    }

    private static async Task<EsptoolResult> RunEsptoolAsync(string esptoolPath, string arguments, bool showCommand, bool pauseAfter, CancellationToken cancellation)
    {
        if (showCommand)
        {
            Console.WriteLine();
            Console.WriteLine("********************************************************************************");
            Console.WriteLine();
            Console.WriteLine("Uruchamianie polecenia:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{esptoolPath} {arguments}");
            Console.ResetColor();
            Console.WriteLine();
        }

        var sbOut = new StringBuilder();
        var sbErr = new StringBuilder();

        var psi = new ProcessStartInfo
        {
            FileName = esptoolPath,
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
                Command = $"{esptoolPath} {arguments}",
                ExitCode = proc.ExitCode,
                StdOut = sbOut.ToString(),
                StdErr = sbErr.ToString(),
                Success = proc.ExitCode == 0
            };

            Console.WriteLine(result.StdOut);
            if (!string.IsNullOrEmpty(result.StdErr))
                Console.Error.WriteLine(result.StdErr);

            if (pauseAfter)
            {
                Console.WriteLine();
                Console.WriteLine("Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return new EsptoolResult { Command = $"{esptoolPath} {arguments}", Success = false, ExitCode = -1, StdErr = "Operation canceled." };
        }
        catch (Exception ex)
        {
            return new EsptoolResult { Command = $"{esptoolPath} {arguments}", Success = false, ExitCode = -1, StdErr = ex.ToString() };
        }
    }

    private static string EscapeArgument(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(' ') || value.Contains('\t') || value.Contains('"'))
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        return value;
    }
}

