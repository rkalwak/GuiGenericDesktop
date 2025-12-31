using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CompilationLib
{
    /// <summary>
    /// Extracts version information from library files (library.json, library.properties)
    /// </summary>
    public class LibraryVersionExtractor
    {
        /// <summary>
        /// Extracts SuplaDevice library version from the repository
        /// </summary>
        /// <param name="repositoryPath">Path to the GUI-Generic repository</param>
        /// <returns>Version string or empty string if not found</returns>
        public static string GetSuplaDeviceVersion(string repositoryPath)
        {
            if (string.IsNullOrEmpty(repositoryPath) || !Directory.Exists(repositoryPath))
                return string.Empty;

            // Try library.json first (PlatformIO format)
            var libraryJsonPath = Path.Combine(repositoryPath, "lib", "SuplaDevice", "library.json");
            if (File.Exists(libraryJsonPath))
            {
                var version = ExtractVersionFromLibraryJson(libraryJsonPath);
                if (!string.IsNullOrEmpty(version))
                    return version;
            }

            // Try library.properties (Arduino format)
            var libraryPropertiesPath = Path.Combine(repositoryPath, "lib", "SuplaDevice", "library.properties");
            if (File.Exists(libraryPropertiesPath))
            {
                var version = ExtractVersionFromLibraryProperties(libraryPropertiesPath);
                if (!string.IsNullOrEmpty(version))
                    return version;
            }

            // Try to extract from header file (fallback)
            var headerPath = Path.Combine(repositoryPath, "lib", "SuplaDevice", "src", "supla", "device", "sw_version.h");
            if (File.Exists(headerPath))
            {
                var version = ExtractVersionFromHeader(headerPath);
                if (!string.IsNullOrEmpty(version))
                    return version;
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts GUI-Generic version from platformio.ini file
        /// </summary>
        /// <param name="repositoryPath">Path to the GUI-Generic repository</param>
        /// <returns>Version string or empty string if not found</returns>
        public static string GetGuiGenericVersion(string repositoryPath)
        {
            if (string.IsNullOrEmpty(repositoryPath) || !Directory.Exists(repositoryPath))
                return string.Empty;

            var platformioIniPath = Path.Combine(repositoryPath, "platformio.ini");
            if (!File.Exists(platformioIniPath))
                return string.Empty;

            try
            {
                var lines = File.ReadAllLines(platformioIniPath);
                foreach (var line in lines)
                {
                    // Look for BUILD_VERSION='"25.02.11"' pattern
                    if (line.Contains("BUILD_VERSION=", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the version using regex to handle various quote patterns
                        // Patterns: BUILD_VERSION='"25.02.11"' or BUILD_VERSION="25.02.11" or BUILD_VERSION='25.02.11'
                        var match = Regex.Match(line, @"BUILD_VERSION\s*=\s*['""]?(['""]?)([^'""]+)\1['""]?", RegexOptions.IgnoreCase);
                        if (match.Success && match.Groups.Count >= 3)
                        {
                            return match.Groups[2].Value.Trim();
                        }
                    }
                }
            }
            catch
            {
                // Ignore file reading errors
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts version from library.json file
        /// </summary>
        private static string ExtractVersionFromLibraryJson(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("version", out var versionElement))
                {
                    return versionElement.GetString();
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts version from library.properties file
        /// </summary>
        private static string ExtractVersionFromLibraryProperties(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("version=", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring("version=".Length).Trim();
                    }
                }
            }
            catch
            {
                // Ignore file reading errors
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts version from C++ header file (sw_version.h)
        /// </summary>
        private static string ExtractVersionFromHeader(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                
                // Look for patterns like:
                // #define SW_VERSION "2.4.0"
                // #define SUPLA_DEVICE_SW_VERSION "2.4.0"
                var patterns = new[]
                {
                    @"#define\s+SW_VERSION\s+""([^""]+)""",
                    @"#define\s+SUPLA_DEVICE_SW_VERSION\s+""([^""]+)""",
                    @"#define\s+VERSION\s+""([^""]+)""",
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch
            {
                // Ignore file reading errors
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets all library versions from the repository
        /// </summary>
        /// <param name="repositoryPath">Path to the GUI-Generic repository</param>
        /// <returns>Dictionary of library names and their versions</returns>
        public static Dictionary<string, string> GetAllLibraryVersions(string repositoryPath)
        {
            var versions = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(repositoryPath) || !Directory.Exists(repositoryPath))
                return versions;

            var libPath = Path.Combine(repositoryPath, "lib");
            if (!Directory.Exists(libPath))
                return versions;

            foreach (var dir in Directory.GetDirectories(libPath))
            {
                var libraryName = Path.GetFileName(dir);
                
                // Try library.json
                var libraryJsonPath = Path.Combine(dir, "library.json");
                if (File.Exists(libraryJsonPath))
                {
                    var version = ExtractVersionFromLibraryJson(libraryJsonPath);
                    if (!string.IsNullOrEmpty(version))
                    {
                        versions[libraryName] = version;
                        continue;
                    }
                }

                // Try library.properties
                var libraryPropertiesPath = Path.Combine(dir, "library.properties");
                if (File.Exists(libraryPropertiesPath))
                {
                    var version = ExtractVersionFromLibraryProperties(libraryPropertiesPath);
                    if (!string.IsNullOrEmpty(version))
                    {
                        versions[libraryName] = version;
                    }
                }
            }

            return versions;
        }
    }
}
