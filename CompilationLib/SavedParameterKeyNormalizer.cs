using System;

namespace CompilationLib
{
    public static class SavedParameterKeyNormalizer
    {
        public static string Normalize(string key, string flagKey = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var trimmed = key.Trim();
            if (!trimmed.StartsWith("Parameter_", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var suffix = trimmed.Substring("Parameter_".Length);
            if (!string.IsNullOrWhiteSpace(flagKey))
            {
                var normalizedFlagKey = flagKey.Trim();
                if (!string.IsNullOrEmpty(normalizedFlagKey) && suffix.StartsWith(normalizedFlagKey, StringComparison.OrdinalIgnoreCase))
                {
                    var remaining = suffix.Substring(normalizedFlagKey.Length);
                    return remaining.TrimStart('_');
                }
            }

            var firstUnderscore = suffix.IndexOf('_');
            if (firstUnderscore >= 0)
            {
                return suffix.Substring(firstUnderscore + 1);
            }

            return suffix;
        }
    }
}
