using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CompilationLib
{
    public  class ArduinoCliDetector
    {
        /// <summary>
        /// Looks for an arduino-cli executable in PATH. Returns the full path if found, otherwise null.
        /// </summary>
        public string FindArduinoCliInPath()
        {
            var exeNames = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "arduino-cli.exe", "arduino-cli" }
                : new[] { "arduino-cli" };

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv))
                return null;

            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    foreach (var name in exeNames)
                    {
                        var candidate = Path.Combine(dir.Trim(), name);
                        if (File.Exists(candidate))
                            return Path.GetFullPath(candidate);
                    }
                }
                catch
                {
                    // ignore individual path errors
                }
            }
            return null;
        }

        /// <summary>
        /// Attempts to detect arduino-cli and query its version.
        /// Returns a tuple: (Found, ResolvedPathOrCommand, VersionText, ErrorText).
        /// </summary>
        public  async Task<(bool Found, string PathOrCommand, string Version, string Error)> TryGetArduinoCliAsync(CancellationToken cancellation = default)
        {
            // Prefer exact path from PATH
            var path = FindArduinoCliInPath() ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "arduino-cli.exe" : "arduino-cli");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start process");
                var outputTask = proc.StandardOutput.ReadToEndAsync();
                var errorTask = proc.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask, proc.WaitForExitAsync(cancellation)).ConfigureAwait(false);

                var stdout = (await outputTask).Trim();
                var stderr = (await errorTask).Trim();

                var success = proc.ExitCode == 0;
                var version = !string.IsNullOrEmpty(stdout) ? stdout : (!string.IsNullOrEmpty(stderr) ? stderr : null);
                return (success, path, version, success ? null : (string.IsNullOrEmpty(stderr) ? "Non-zero exit code" : stderr));
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }
    }
}