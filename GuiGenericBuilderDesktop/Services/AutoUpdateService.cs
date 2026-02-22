using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Octokit;
using Serilog;

namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Handles automatic application updates from GitHub releases
    /// </summary>
    public class AutoUpdateService
    {
        private const string AutoUpdateEnvironmentVariable = "GUI_GENERIC_AUTO_UPDATE_ENABLED";

        private readonly ILogger _logger;
        private readonly GitHubClient _githubClient;
        private readonly string _repositoryOwner;
        private readonly string _repositoryName;
        private readonly Version _currentVersion;

        /// <summary>
        /// Gets whether auto-update feature is enabled via environment variable.
        /// Default is disabled for safety.
        /// </summary>
        public bool IsAutoUpdateEnabled
        {
            get
            {
                var envValue = Environment.GetEnvironmentVariable(AutoUpdateEnvironmentVariable);
                return !string.IsNullOrEmpty(envValue) && 
                       (envValue.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                        envValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        envValue.Equals("yes", StringComparison.OrdinalIgnoreCase));
            }
        }

        public AutoUpdateService(string repositoryOwner, string repositoryName, ILogger logger)
        {
            _repositoryOwner = repositoryOwner;
            _repositoryName = repositoryName;
            _logger = logger;

            // Initialize GitHub client
            _githubClient = new GitHubClient(new ProductHeaderValue("GuiGenericBuilderDesktop"));

            // Get current version from assembly
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            _logger.Information("AutoUpdateService initialized. Current version: {Version}", _currentVersion);
            _logger.Information("Auto-update feature enabled: {Enabled} (Environment variable: {EnvVar})", 
                IsAutoUpdateEnabled, AutoUpdateEnvironmentVariable);
        }

        /// <summary>
        /// Checks if a new version is available on GitHub
        /// </summary>
        public async Task<(bool UpdateAvailable, Release LatestRelease)> CheckForUpdatesAsync()
        {
            if (!IsAutoUpdateEnabled)
            {
                _logger.Information("Auto-update is disabled. Set environment variable {EnvVar}=true to enable.", 
                    AutoUpdateEnvironmentVariable);
                return (false, null);
            }

            try
            {
                _logger.Information("Checking for updates from GitHub repository: {Owner}/{Repo}", _repositoryOwner, _repositoryName);

                var releases = await _githubClient.Repository.Release.GetAll(_repositoryOwner, _repositoryName);
                var latestRelease = releases
                    .Where(r => !r.Prerelease && !r.Draft)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefault();

                if (latestRelease == null)
                {
                    _logger.Information("No releases found on GitHub");
                    return (false, null);
                }

                // Parse version from tag (assuming format like "v1.0.0" or "1.0.0")
                var versionString = latestRelease.TagName.TrimStart('v', 'V');
                if (Version.TryParse(versionString, out var latestVersion))
                {
                    var updateAvailable = latestVersion > _currentVersion;
                    _logger.Information(
                        "Latest version: {LatestVersion}, Current version: {CurrentVersion}, Update available: {UpdateAvailable}",
                        latestVersion, _currentVersion, updateAvailable);

                    return (updateAvailable, updateAvailable ? latestRelease : null);
                }
                else
                {
                    _logger.Warning("Could not parse version from tag: {TagName}", latestRelease.TagName);
                    return (false, null);
                }
            }
            catch (RateLimitExceededException ex)
            {
                _logger.Error(ex, "GitHub API rate limit exceeded");
                throw new InvalidOperationException("GitHub API rate limit exceeded. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to check for updates");
                return (false, null);
            }
        }

        /// <summary>
        /// Downloads and installs the update
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync(Release release, IProgress<int> progress = null)
        {
            if (!IsAutoUpdateEnabled)
            {
                _logger.Warning("Auto-update is disabled. Cannot download update.");
                return false;
            }

            try
            {
                _logger.Information("Starting update download for version {Version}", release.TagName);

                // Find the appropriate asset (prefer self-contained over framework-dependent)
                // Look for: GuiGenericBuilder-vX.X.X-win-x64.zip (self-contained, larger but no .NET required)
                var asset = release.Assets
                    .Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || 
                               a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .Where(a => a.Name.Contains("win", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => !a.Name.Contains("framework-dependent", StringComparison.OrdinalIgnoreCase)) // Prefer self-contained
                    .FirstOrDefault();

                if (asset == null)
                {
                    _logger.Warning("No suitable update package found in release assets");
                    return false;
                }

                // Download the update
                var tempPath = Path.Combine(Path.GetTempPath(), asset.Name);
                _logger.Information("Downloading update to: {TempPath}", tempPath);

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "GuiGenericBuilderDesktop");
                    
                    using (var response = await httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var downloadedBytes = 0L;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(tempPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            int bytesRead;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;

                                if (totalBytes > 0 && progress != null)
                                {
                                    var percentComplete = (int)((downloadedBytes * 100) / totalBytes);
                                    progress.Report(percentComplete);
                                }
                            }
                        }
                    }
                }

                _logger.Information("Download completed: {TempPath}", tempPath);

                // Create update script that will replace the current executable
                await CreateUpdateScriptAsync(tempPath, asset.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to download and install update");
                return false;
            }
        }

        /// <summary>
        /// Copies the bundled update script to a temp location and launches it with all required parameters.
        /// </summary>
        private Task CreateUpdateScriptAsync(string downloadedFilePath, string assetName)
        {
            var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            var appDirectory   = Path.GetDirectoryName(currentExePath);
            var exeName        = Path.GetFileName(currentExePath);

            var bundledScriptPath = Path.Combine(appDirectory, "Scripts", "update.ps1");
            if (!File.Exists(bundledScriptPath))
            {
                _logger.Error("Bundled update script not found at: {ScriptPath}", bundledScriptPath);
                throw new FileNotFoundException(
                    "Update script not found. Please reinstall the application.",
                    bundledScriptPath);
            }

            // Copy the script to a temp location so it is not overwritten when the ZIP is extracted
            var tempScriptPath = Path.Combine(Path.GetTempPath(), $"update_guigeneric_{Guid.NewGuid():N}.ps1");
            File.Copy(bundledScriptPath, tempScriptPath, overwrite: true);
            _logger.Information("Update script copied to temp: {ScriptPath}", tempScriptPath);

            var arguments = $"-ExecutionPolicy Bypass -WindowStyle Normal -File \"{tempScriptPath}\"" +
                            $" -AppDirectory \"{appDirectory}\"" +
                            $" -DownloadedFilePath \"{downloadedFilePath}\"" +
                            $" -AssetName \"{assetName}\"" +
                            $" -ExeName \"{exeName}\"";

            var psi = new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = arguments,
                UseShellExecute = false,
                CreateNoWindow  = true
            };

            Process.Start(psi);
            _logger.Information("Update script launched. Application will now exit.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Launches the update process and exits the application
        /// </summary>
        public void ApplyUpdateAndRestart()
        {
            _logger.Information("Applying update and restarting application...");
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Gets the current application version
        /// </summary>
        public Version GetCurrentVersion() => _currentVersion;
    }
}
