using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CompilationLib
{
    /// <summary>
    /// Handles Platform.io installation on demand
    /// </summary>
    public class PlatformioInstaller
    {
        private const string GetPlatformioUrl = "https://raw.githubusercontent.com/ivankravets/platformio/master/scripts/get-platformio.py";
        private const string GetPlatformioScript = "get-platformio.py";

        /// <summary>
        /// Checks if Platform.io is installed
        /// </summary>
        /// <returns>True if Platform.io is installed, false otherwise</returns>
        public async Task<bool> IsPlatformioInstalledAsync()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pio",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Installs Platform.io by downloading and running the installation script
        /// </summary>
        /// <param name="progress">Optional progress callback for reporting installation status</param>
        /// <returns>True if installation was successful, false otherwise</returns>
        public async Task<bool> InstallPlatformioAsync(IProgress<string> progress = null)
        {
            try
            {
                progress?.Report("Checking if Python is installed...");
                if (!await IsPythonInstalledAsync())
                {
                    progress?.Report("Python is not installed. Please install Python first.");
                    return false;
                }

                progress?.Report("Downloading Platform.io installation script...");
                var scriptPath = await DownloadInstallationScriptAsync();

                if (string.IsNullOrEmpty(scriptPath))
                {
                    progress?.Report("Failed to download installation script.");
                    return false;
                }

                progress?.Report("Running Platform.io installation script...");
                var result = await RunInstallationScriptAsync(scriptPath, progress);

                // Clean up the downloaded script
                try
                {
                    if (File.Exists(scriptPath))
                    {
                        File.Delete(scriptPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                if (result)
                {
                    progress?.Report("Platform.io installed successfully!");
                }
                else
                {
                    progress?.Report("Platform.io installation failed.");
                }

                return result;
            }
            catch (Exception ex)
            {
                progress?.Report($"Installation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if Python is installed
        /// </summary>
        private async Task<bool> IsPythonInstalledAsync()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Downloads the Platform.io installation script
        /// </summary>
        private async Task<string> DownloadInstallationScriptAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var scriptContent = await httpClient.GetStringAsync(GetPlatformioUrl);

                var tempPath = Path.Combine(Path.GetTempPath(), GetPlatformioScript);
                await File.WriteAllTextAsync(tempPath, scriptContent);

                return tempPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Runs the Platform.io installation script
        /// </summary>
        private async Task<bool> RunInstallationScriptAsync(string scriptPath, IProgress<string> progress = null)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    return false;
                }

                // Read output asynchronously
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        progress?.Report(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        progress?.Report($"Error: {args.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                progress?.Report($"Error running installation script: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the Platform.io installation path
        /// </summary>
        public string GetPlatformioPath()
        {
            // Common Platform.io installation paths
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var possiblePaths = new[]
            {
                Path.Combine(userProfile, ".platformio"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "platformio"),
                Path.Combine(userProfile, ".local", "share", "platformio")
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
/*
 * var installer = new PlatformioInstaller();

// Check installation
if (!await installer.IsPlatformioInstalledAsync())
{
    // Install with progress reporting
    var progress = new Progress<string>(msg => Console.WriteLine(msg));
    bool success = await installer.InstallPlatformioAsync(progress);
}
*/