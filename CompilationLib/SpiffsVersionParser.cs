using System.Text;

namespace CompilationLib
{
    /// <summary>
    /// Stateless helper for scanning raw SPIFFS/LittleFS bytes for a firmware version marker.
    /// </summary>
    public static class SpiffsVersionParser
    {
        /// <summary>
        /// Scans <paramref name="data"/> for the first occurrence of <paramref name="versionMarker"/>
        /// and returns the segment immediately following it (up to the next '-' separator or end of the
        /// printable token), which represents the firmware version string.
        ///
        /// Supported patterns (marker = "SV-"):
        ///   "SV-Z2S-1.5.1-07/04/26"  → "Z2S"
        ///   "SV-1.5.1"               → "1.5.1"
        ///   "SV-26.04.17"            → "26.04.17"
        ///
        /// Returns <c>null</c> when the marker is not found or the version segment is empty.
        /// </summary>
        public static string FindVersion(byte[] data, string versionMarker = "SV-")
        {
            if (data == null || data.Length == 0 || string.IsNullOrEmpty(versionMarker))
                return null;

            var pattern = Encoding.ASCII.GetBytes(versionMarker);

            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j]) { match = false; break; }
                }
                if (!match) continue;

                // Read ahead for the full printable token (max 50 chars)
                int end = i + pattern.Length;
                while (end < data.Length && end < i + 50
                       && data[end] >= 0x20 && data[end] < 0x7F)
                    end++;

                var versionLine = Encoding.ASCII.GetString(data, i, end - i);

                // Strip the marker prefix (e.g. "SV-") and return the rest as-is
                // "SV-Z2S-1.5.1-07/04/26" → "Z2S-1.5.1-07/04/26"
                // "SV-1.5.1"              → "1.5.1"
                var version = versionLine.Substring(pattern.Length).Trim();
                if (!string.IsNullOrEmpty(version))
                    return version;
            }

            return null;
        }
    }
}
