using System.Security.Cryptography;
using System.Text;

namespace CompilationLib
{
    /// <summary>
    /// Provides hash calculation utilities for build configurations.
    /// </summary>
    public class BuildConfigurationHasher
    {
        /// <summary>
        /// Calculates SHA256 hash of the selected build flag options.
        /// </summary>
        /// <param name="options">Array of build flag keys/options.</param>
        /// <returns>Hexadecimal string representation of the hash.</returns>
        public static string CalculateHash(string[] options)
        {
            if (options == null || options.Length == 0)
            {
                return string.Empty;
            }

            // Normalize to lowercase and sort to ensure consistent hash regardless of order or case
            var sortedOptions = options
                .Select(s => s.ToLowerInvariant())
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
            
            // Combine all strings into a single string separated by a delimiter
            var combinedString = string.Join("|", sortedOptions);
            
            // Calculate SHA256 hash
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedString));
                
                // Convert hash bytes to hexadecimal string
                var hashString = new StringBuilder(hashBytes.Length * 2);
                foreach (var b in hashBytes)
                {
                    hashString.Append(b.ToString("x2"));
                }
                
                return hashString.ToString();
            }
        }

        /// <summary>
        /// Calculates SHA256 hash from a list of BuildFlagItem objects.
        /// </summary>
        /// <param name="buildFlags">List of build flags.</param>
        /// <returns>Hexadecimal string representation of the hash.</returns>
        public static string CalculateHash(IEnumerable<BuildFlagItem> buildFlags)
        {
            if (buildFlags == null)
            {
                return string.Empty;
            }

            var keys = buildFlags
                .Where(f => !string.IsNullOrEmpty(f?.Key))
                .Select(f => f.Key)
                .ToArray();

            return CalculateHash(keys);
        }
    }
}
