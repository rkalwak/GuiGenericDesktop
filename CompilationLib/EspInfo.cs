namespace CompilationLib
{
    /// <summary>
    /// Result of ESP detection run.
    /// </summary>
    public class EspInfo
    {
        public bool IsRecognized { get; init; }
        public string? Model { get; init; }
        public string? ChipType { get; init; }           // E.g. "ESP32"
        public string? ChipFullName { get; init; }       // E.g. "ESP32-D0WD-V3"
        public string? ChipRevision { get; init; }       // E.g. "v3.1"
        public string? Features { get; init; }           // Full features line
        public string? Mac { get; init; }                // MAC address
        public string? Manufacturer { get; init; }       // Manufacturer id/text
        public string? Device { get; init; }             // Device id/text
        public string? FlashSize { get; init; }
        public string? RawOutput { get; init; }
        public string? RawError { get; init; }
        public string? Message { get; init; }

        public override string ToString()
        {
            return $"Model: {Model}, ChipType: {ChipType}, Chip: {ChipFullName} ({ChipRevision}), FlashSize: {FlashSize}, MAC: {Mac}, Features: {Features}";
        }
    }
}
