using Newtonsoft.Json;

namespace CompilationLib
{
    /// <summary>
    /// Represents a saved build configuration
    /// </summary>
    public class SavedBuildConfiguration
    {
        /// <summary>
        /// Encoded configuration string (reversible, can decode to get flags)
        /// </summary>
        public string EncodedConfig { get; set; } = string.Empty;

        public string ConfigurationName { get; set; } = string.Empty;
        public DateTime SavedDate { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string ComPort { get; set; } = string.Empty;

        /// <summary>
        /// Flash size selected for this configuration (e.g., "4MB", "8MB", "16MB")
        /// </summary>
        public string FlashSize { get; set; } = string.Empty;

        /// <summary>
        /// Filename of the associated firmware binary (if available)
        /// </summary>
        public string FirmwareFileName { get; set; } = string.Empty;

        /// <summary>
        /// Filename of the merged ZIP file containing firmware.bin, bootloader.bin, and partitions.bin (if available)
        /// </summary>
        public string MergedZipFileName { get; set; } = string.Empty;

        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Dictionary of flag parameters. Key is the flag key, value is a dictionary of parameter name to value.
        /// Example: { "SUPLA_DHT22": { "Pin": "5", "Type": "DHT22" } }
        /// Flags without parameters will have an empty dictionary: { "SUPLA_DEVICE": { } }
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> BuildFlagsParameters { get; set; } = new();

        /// <summary>
        /// Gets the list of enabled flag keys from BuildFlagsParameters.
        /// This is a computed property for backward compatibility.
        /// </summary>
        [JsonIgnore]
        public List<string> EnabledFlagKeys => BuildFlagsParameters.Keys.ToList();
    }
}
