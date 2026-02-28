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
        /// Optional local path to the GUI-Generic repository.
        /// When set, the application uses this path instead of downloading the repository.
        /// </summary>
        public string GGLocal { get; set; }
    }
}
