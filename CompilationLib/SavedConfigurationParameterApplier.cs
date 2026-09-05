using System;
using System.Collections.Generic;
using System.Linq;

namespace CompilationLib
{
    public static class SavedConfigurationParameterApplier
    {
        public static void Apply(BuildFlagItem flag, IReadOnlyDictionary<string, string> savedParameters)
        {
            if (flag == null || savedParameters == null || savedParameters.Count == 0)
            {
                return;
            }

            if (flag.Parameters == null)
            {
                flag.Parameters = new List<Parameter>();
            }

            foreach (var savedParameter in savedParameters)
            {
                var normalizedKey = SavedParameterKeyNormalizer.Normalize(savedParameter.Key ?? string.Empty, flag.Key);
                if (string.IsNullOrWhiteSpace(normalizedKey))
                {
                    continue;
                }

                var parameter = flag.Parameters.FirstOrDefault(p =>
                    string.Equals((p?.Identifier ?? string.Empty), normalizedKey, StringComparison.OrdinalIgnoreCase));

                if (parameter != null)
                {
                    parameter.Value = savedParameter.Value ?? string.Empty;
                    continue;
                }

                flag.Parameters.Add(new Parameter
                {
                    Key = normalizedKey,
                    Name = normalizedKey,
                    Type = DetectType(normalizedKey),
                    Value = savedParameter.Value ?? string.Empty
                });
            }
        }

        private static string DetectType(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return "string";
            }

            if (identifier.Equals("Count", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("GPIO", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("Pullup", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("Type", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("MainTempChannel", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("AdditionalTempChannel", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("State", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("ReactionAfterReset", StringComparison.OrdinalIgnoreCase) ||
                identifier.EndsWith("LightControl", StringComparison.OrdinalIgnoreCase))
            {
                return identifier.Equals("Count", StringComparison.OrdinalIgnoreCase)
                    ? "number"
                    : "string";
            }

            return "string";
        }
    }
}
