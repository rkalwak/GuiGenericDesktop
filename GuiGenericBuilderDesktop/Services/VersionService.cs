using System.Reflection;
using CompilationLib;
using Serilog;

namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Handles version detection and window title updates
    /// </summary>
    public class VersionService
    {
        private readonly ILogger _logger;
        private readonly string _repositoryPath;

        public VersionService(string repositoryPath, ILogger logger)
        {
            _repositoryPath = repositoryPath;
            _logger = logger;
        }

        /// <summary>
        /// Gets version information from the repository
        /// </summary>
        public (string SuplaVersion, string GGVersion) GetVersions()
        {
            try
            {
                // Get SuplaDevice version
                var suplaVersion = LibraryVersionExtractor.GetSuplaDeviceVersion(_repositoryPath);

                if (!string.IsNullOrEmpty(suplaVersion))
                {
                    _logger.Information("SuplaDevice version detected: {Version}", suplaVersion);
                }
                else
                {
                    _logger.Debug("SuplaDevice version not found in repository");
                }

                // Get GUI-Generic version
                var ggVersion = LibraryVersionExtractor.GetGuiGenericVersion(_repositoryPath);

                if (!string.IsNullOrEmpty(ggVersion))
                {
                    _logger.Information("GUI-Generic version detected: {Version}", ggVersion);
                }
                else
                {
                    _logger.Debug("GUI-Generic version not found in repository");
                }

                return (suplaVersion, ggVersion);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to extract library versions");
                return (null, null);
            }
        }

        /// <summary>
        /// Generates window title with version information
        /// </summary>
        public string GenerateWindowTitle(string suplaVersion, string ggVersion)
        {
            const string baseTitle = "GUI-Generic Builder";

            var titleParts = new List<string> { baseTitle };

            var appVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            if (appVersion is not null)
            {
                titleParts.Add($"GGBD v{appVersion.ToString(3)}");
            }

            if (!string.IsNullOrEmpty(ggVersion))
            {
                titleParts.Add($"GG v{ggVersion}");
            }

            if (!string.IsNullOrEmpty(suplaVersion))
            {
                titleParts.Add($"SD v{suplaVersion}");
            }

            // If no versions found, just show base title
            var title = titleParts.Count == 1 ? baseTitle : string.Join(" - ", titleParts);
            
            _logger.Debug("Window title generated: {Title}", title);
            
            return title;
        }
    }
}
