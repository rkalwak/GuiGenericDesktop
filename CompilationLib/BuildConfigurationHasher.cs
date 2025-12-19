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

        /// <summary>
        /// Encodes selected options into a reversible, compact string representation.
        /// Uses base64 encoding with compression for space efficiency.
        /// </summary>
        /// <param name="options">Array of build flag keys/options.</param>
        /// <returns>Reversible encoded string representation.</returns>
        public static string EncodeOptions(string[] options)
        {
            if (options == null || options.Length == 0)
            {
                return string.Empty;
            }

            // Normalize to uppercase and sort to ensure consistent encoding
            var sortedOptions = options
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.ToUpperInvariant())
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
            
            // Join with a delimiter
            var combinedString = string.Join("|", sortedOptions);
            
            // Convert to bytes
            var bytes = Encoding.UTF8.GetBytes(combinedString);
            
            // Compress using GZip
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new System.IO.Compression.GZipStream(outputStream, System.IO.Compression.CompressionMode.Compress))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }
                
                var compressed = outputStream.ToArray();
                
                // Encode as base64url (URL-safe)
                return ToBase64Url(compressed);
            }
        }

        /// <summary>
        /// Decodes a reversible encoded string back to the original options array.
        /// </summary>
        /// <param name="encoded">The encoded string from EncodeOptions.</param>
        /// <returns>Array of original options, or null if decoding fails.</returns>
        public static string[] DecodeOptions(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            try
            {
                // Decode from base64url
                var compressed = FromBase64Url(encoded);
                
                // Decompress
                using (var inputStream = new MemoryStream(compressed))
                using (var gzipStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress))
                using (var outputStream = new MemoryStream())
                {
                    gzipStream.CopyTo(outputStream);
                    var decompressed = outputStream.ToArray();
                    
                    // Convert back to string
                    var combinedString = Encoding.UTF8.GetString(decompressed);
                    
                    // Split by delimiter
                    return combinedString.Split('|', StringSplitOptions.RemoveEmptyEntries);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Encodes options from BuildFlagItem objects into a reversible string.
        /// </summary>
        /// <param name="buildFlags">List of build flags.</param>
        /// <returns>Reversible encoded string representation.</returns>
        public static string EncodeOptions(IEnumerable<BuildFlagItem> buildFlags)
        {
            if (buildFlags == null)
            {
                return string.Empty;
            }

            var keys = buildFlags
                .Where(f => !string.IsNullOrEmpty(f?.Key))
                .Select(f => f.Key)
                .ToArray();

            return EncodeOptions(keys);
        }

        /// <summary>
        /// Converts byte array to URL-safe base64 string.
        /// </summary>
        private static string ToBase64Url(byte[] data)
        {
            var base64 = Convert.ToBase64String(data);
            
            // Make URL-safe by replacing characters
            return base64
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Converts URL-safe base64 string back to byte array.
        /// </summary>
        private static byte[] FromBase64Url(string base64Url)
        {
            // Restore standard base64
            var base64 = base64Url
                .Replace('-', '+')
                .Replace('_', '/');
            
            // Add padding if needed
            var padding = (4 - (base64.Length % 4)) % 4;
            if (padding > 0)
            {
                base64 += new string('=', padding);
            }
            
            return Convert.FromBase64String(base64);
        }
    }
}
