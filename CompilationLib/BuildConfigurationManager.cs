using System.IO;
using Newtonsoft.Json;

namespace CompilationLib
{
    /// <summary>
    /// Manages saving and loading build configurations with their hashes
    /// </summary>
    public class BuildConfigurationManager
    {
        private readonly string _configurationsDirectory;

        public BuildConfigurationManager(string configurationsDirectory)
        {
            _configurationsDirectory = configurationsDirectory;
            
            if (!Directory.Exists(_configurationsDirectory))
            {
                Directory.CreateDirectory(_configurationsDirectory);
            }
        }

        /// <summary>
        /// Saves a build configuration with its hash
        /// </summary>
        public void SaveConfiguration(string hash, IEnumerable<BuildFlagItem> enabledFlags, string? configName = null, string? platform = null, string? comPort = null)
        {
            if (string.IsNullOrEmpty(hash) || enabledFlags == null)
                return;

            // Build the BuildFlagsParameters dictionary with all enabled flags
            var flagsParameters = new Dictionary<string, Dictionary<string, string>>();
            foreach (var flag in enabledFlags.Where(f => !string.IsNullOrEmpty(f?.Key)))
            {
                var paramValues = new Dictionary<string, string>();
                
                if (flag.Parameters != null && flag.Parameters.Any())
                {
                    foreach (var param in flag.Parameters.Where(p => !string.IsNullOrEmpty(p?.Name)))
                    {
                        paramValues[param.Name!] = param.Value ?? string.Empty;
                    }
                }
                
                // Add flag even if it has no parameters (empty dictionary)
                flagsParameters[flag.Key!] = paramValues;
            }
            
            var config = new SavedBuildConfiguration
            {
                Hash = hash,
                ConfigurationName = configName ?? $"Config_{DateTime.Now:yyyyMMdd_HHmmss}",
                SavedDate = DateTime.Now,
                Platform = platform ?? string.Empty,
                ComPort = comPort ?? string.Empty,
                BuildFlagsParameters = flagsParameters
            };

            // If configName was provided (manual save), use it as filename
            // Otherwise (auto-save after compilation), use hash as filename
            string fileName;
            if (!string.IsNullOrEmpty(configName))
            {
                // Manual save: use custom name, sanitize it
                var sanitizedName = configName;
                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (var c in invalidChars)
                {
                    sanitizedName = sanitizedName.Replace(c, '_');
                }
                fileName = $"{sanitizedName}.json";
            }
            else
            {
                // Auto-save: use hash as filename
                fileName = $"{hash}.json";
            }
             
            var filePath = Path.Combine(_configurationsDirectory, fileName);
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads a build configuration by its hash
        /// </summary>
        public SavedBuildConfiguration? LoadConfiguration(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return null;

            // Try direct hash-based filename first (auto-saved configs)
            var filePath = Path.Combine(_configurationsDirectory, $"{hash}.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<SavedBuildConfiguration>(json);
            }
            
            // If not found, search through all configurations (manually-saved configs with custom names)
            var allConfigs = GetAllConfigurations();
            return allConfigs.FirstOrDefault(c => string.Equals(c.Hash, hash, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all saved configurations
        /// </summary>
        public List<SavedBuildConfiguration> GetAllConfigurations()
        {
            var configurations = new List<SavedBuildConfiguration>();

            if (!Directory.Exists(_configurationsDirectory))
                return configurations;

            foreach (var file in Directory.GetFiles(_configurationsDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var config = JsonConvert.DeserializeObject<SavedBuildConfiguration>(json);
                    if (config != null)
                    {
                        config.FileName = Path.GetFileName(file);
                        configurations.Add(config);
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            return configurations.OrderByDescending(c => c.SavedDate).ToList();
        }

        /// <summary>
        /// Deletes a configuration by hash
        /// </summary>
        public bool DeleteConfiguration(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            // Try direct hash-based filename first
            var filePath = Path.Combine(_configurationsDirectory, $"{hash}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            
            // If not found, search through all configurations to find matching hash
            var config = GetAllConfigurations()
                .FirstOrDefault(c => string.Equals(c.Hash, hash, StringComparison.OrdinalIgnoreCase));
            
            if (config == null || string.IsNullOrEmpty(config.FileName))
                return false;
             
            filePath = Path.Combine(_configurationsDirectory, config.FileName);
             if (File.Exists(filePath))
             {
                 File.Delete(filePath);
                 return true;
             }
             
             return false;
        }
    }

    /// <summary>
    /// Represents a saved build configuration
    /// </summary>
    public class SavedBuildConfiguration
    {
        public string Hash { get; set; } = string.Empty;
        public string ConfigurationName { get; set; } = string.Empty;
        public DateTime SavedDate { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string ComPort { get; set; } = string.Empty;
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
