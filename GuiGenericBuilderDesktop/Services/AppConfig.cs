namespace GuiGenericBuilderDesktop.Services
{
    /// <summary>
    /// Application-wide configuration loaded from appsettings.json.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Whether the automatic update check is enabled.
        /// </summary>
        public bool AutoUpdateEnabled { get; set; }

        /// <summary>
        /// Maximum version the auto-updater is allowed to install (raw string from config).
        /// </summary>
        public string AutoUpdateMaxVersion { get; set; }

        /// <summary>
        /// Parsed <see cref="AutoUpdateMaxVersion"/>. Null when the value is missing or invalid.
        /// </summary>
        public Version MaxVersion => Version.TryParse(AutoUpdateMaxVersion, out var v) ? v : null;

        /// <summary>
        /// Whether the GUI-Generic builder update check is enabled.
        /// When enabled, the application checks for newer builder.json versions on GitHub at startup.
        /// </summary>
        public bool GGUpdateCheckEnabled { get; set; }

        /// <summary>
        /// Optional local path to the GUI-Generic repository.
        /// When set, the application uses this path instead of downloading the repository.
        /// </summary>
        public string GGLocal { get; set; }

        /// <summary>
        /// How many of the most recent Z2S Gateway firmware releases to show in the version picker.
        /// Can also be overridden via the Z2S_VERSION_HISTORY_COUNT environment variable.
        /// Defaults to 10.
        /// </summary>
        public int Z2SVersionHistoryCount { get; set; } = 10;

        /// <summary>
        /// GitHub repository owner for the Zigbee (Z2S) firmware.
        /// </summary>
        public string ZigbeeGitHubOwner { get; set; } = "rkalwak";

        /// <summary>
        /// GitHub repository name for the Zigbee (Z2S) firmware.
        /// </summary>
        public string ZigbeeGitHubRepo { get; set; } = "Z2S_Library";

        /// <summary>
        /// GitHub Personal Access Token used to authenticate API requests.
        /// Set via the GITHUB_PAT environment variable or appsettings.json.
        /// When provided, authenticated requests have a higher rate limit.
        /// </summary>
        public string GitHubPat { get; set; }
    }
}
