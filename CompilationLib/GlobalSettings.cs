namespace CompilationLib
{
    public class GlobalSettings
    {
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

       
    }

    public static class GlobalSettingsExtensions
    {
        public static IReadOnlyList<BoardGpioOption> GetOptionsForBoard(this GlobalSettings globalSettings, string boardName)
        {
            if (globalSettings == null || string.IsNullOrWhiteSpace(boardName))
            {
                return Array.Empty<BoardGpioOption>();
            }

            var normalized = boardName.Trim();
            var keySuffix = normalized.StartsWith("GPIO_P_", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring("GPIO_P_".Length)
                : normalized;

            var maps = globalSettings.Parameters
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Key) && x.Key.StartsWith("GPIO_P_", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var matches = maps
                .Where(x => string.Equals(x.Key.Substring("GPIO_P_".Length), keySuffix, StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.EnumValues ?? new List<EnumValue>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) && int.TryParse(x.Value, out _))
                .Select(x => new BoardGpioOption
                {
                    Value = int.Parse(x.Value),
                    Display = string.IsNullOrWhiteSpace(x.Name) ? x.Value : x.Name
                })
                .ToList();

            return matches;
        }
    }

    public static class GlobalSettingsParameterExtensions
    {
        public static void PopulateGpioEnumValues(this Parameter parameter, GlobalSettings globalSettings, string boardName)
        {
            if (parameter == null || !string.Equals(parameter.Type, "gpio", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            parameter.EnumValues = globalSettings
                .GetOptionsForBoard(boardName)
                .Select(option => new EnumValue
                {
                    Value = option.Value.ToString(),
                    Name = option.Display
                })
                .ToList();
        }

        public static bool HasBoardGpioOptions(this Parameter parameter, GlobalSettings globalSettings, string boardName)
        {
            if (parameter == null || !string.Equals(parameter.Type, "gpio", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return globalSettings
                .GetOptionsForBoard(boardName)
                .Any();
        }
    }

    public sealed class BoardGpioOption
    {
        public int Value { get; init; }
        public string Display { get; init; } = string.Empty;
    }
}