using System.IO;
using System.Net.Http;
using CompilationLib;
using Newtonsoft.Json.Linq;
using Serilog;

namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Checks for updates of the GUI-Generic builder from GitHub
    /// </summary>
    public class GGUpdateService
    {
        private readonly ILogger _logger;
        private readonly string _repositoryPath;
        private readonly AppConfig _config;
        private const string GG_GITHUB_OWNER = "rkalwak";
        private const string GG_GITHUB_REPO = "GUI-Generic";

        /// <summary>
        /// Gets whether GG update check feature is enabled via configuration.
        /// Default is enabled for convenience.
        /// </summary>
        public bool IsGGUpdateCheckEnabled => _config.GGUpdateCheckEnabled;

        public GGUpdateService(string repositoryPath, AppConfig config, ILogger logger)
        {
            _repositoryPath = repositoryPath;
            _config = config;
            _logger = logger;
            _logger.Information("GGUpdateService initialized. Update check enabled: {Enabled}", IsGGUpdateCheckEnabled);
        }

        /// <summary>
        /// Checks if a newer version of GUI-Generic builder is available on GitHub
        /// </summary>
        /// <returns>Tuple with update availability flag and the remote version string</returns>
        public async Task<(bool UpdateAvailable, string RemoteVersion, string CurrentVersion)> CheckForGGUpdatesAsync()
        {
            if (!IsGGUpdateCheckEnabled)
            {
                _logger.Information("GG update check is disabled. Set GGUpdateCheckEnabled=true in appsettings.json to enable.");
                return (false, null, null);
            }

            try
            {
                _logger.Information("Checking for GUI-Generic builder updates from GitHub");

                // Get local version from builder.json
                var localVersion = GetLocalBuilderVersion();
                if (localVersion == null)
                {
                    _logger.Warning("Could not read local builder.json version");
                    return (false, null, null);
                }

                _logger.Information("Local GUI-Generic version: {Version}", localVersion);

                // Get remote version from GitHub
                var remoteVersion = await GetRemoteBuilderVersionAsync();
                if (remoteVersion == null)
                {
                    _logger.Warning("Could not read remote builder.json version from GitHub");
                    return (false, null, localVersion);
                }

                _logger.Information("Remote GUI-Generic version: {Version}", remoteVersion);

                // Compare versions
                if (Version.TryParse(localVersion, out var localVer) && 
                    Version.TryParse(remoteVersion, out var remoteVer))
                {
                    var updateAvailable = remoteVer > localVer;
                    _logger.Information("GUI-Generic update available: {Available} (Local: {Local}, Remote: {Remote})", 
                        updateAvailable, localVersion, remoteVersion);
                    return (updateAvailable, remoteVersion, localVersion);
                }
                else
                {
                    _logger.Warning("Could not parse versions for comparison. Local: {Local}, Remote: {Remote}", 
                        localVersion, remoteVersion);
                    return (false, remoteVersion, localVersion);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to check for GUI-Generic updates");
                return (false, null, null);
            }
        }

        /// <summary>
        /// Gets the version from local builder.json file
        /// </summary>
        private string GetLocalBuilderVersion()
        {
            try
            {
                var builderJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "builder.json");
                
                if (!File.Exists(builderJsonPath))
                {
                    _logger.Debug("Local builder.json not found at: {Path}", builderJsonPath);
                    return null;
                }

                var jsonContent = File.ReadAllText(builderJsonPath);
                var json = JObject.Parse(jsonContent);
                var version = json["version"]?.ToString();

                return version;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to read local builder.json version");
                return null;
            }
        }

        /// <summary>
        /// Gets the version from remote builder.json file on GitHub
        /// </summary>
        private async Task<string> GetRemoteBuilderVersionAsync()
        {
            try
            {
                // GitHub raw content URL for builder.json in master branch
                var url = $"https://raw.githubusercontent.com/{GG_GITHUB_OWNER}/{GG_GITHUB_REPO}/master/builder.json";
                
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "GuiGenericBuilderDesktop");
                    
                    var jsonContent = await httpClient.GetStringAsync(url);
                    var json = JObject.Parse(jsonContent);
                    var version = json["version"]?.ToString();

                    return version;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to read remote builder.json version from GitHub");
                return null;
            }
        }
    }
}
