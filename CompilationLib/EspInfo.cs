namespace CompilationLib
{
    /// <summary>
    /// Result of ESP detection run.
    /// </summary>
    public class EspInfo
    {
        public bool IsRecognized { get; init; }
        public string Model { get; init; }
        public string ChipType { get; init; }           // E.g. "ESP32"
        public string ChipFullName { get; init; }       // E.g. "ESP32-D0WD-V3"
        public string ChipRevision { get; init; }       // E.g. "v3.1"
        public string Features { get; init; }           // Full features line
        public string Mac { get; init; }                // MAC address
        public string Manufacturer { get; init; }       // Manufacturer id/text
        public string Device { get; init; }             // Device id/text
        public string FlashSize { get; init; }
        public string RawOutput { get; init; }
        public string RawError { get; init; }
        public string Message { get; init; }

        public override string ToString()
        {
            return $"Model: {Model}, ChipType: {ChipType}, Chip: {ChipFullName} ({ChipRevision}), FlashSize: {FlashSize}, MAC: {Mac}, Features: {Features}";
        }
    }
}

/*
esptool v5.1.0
Serial port COM5:
Connecting....Detecting chip type... ESP32 - C6
Connected to ESP32-C6 on COM5:
Chip type:          ESP32 - C6(QFN40)(revision v0.1)
Features: Wi - Fi 6, BT 5 (LE), IEEE802.15.4, Single Core + LP Core, 160MHz
Crystal frequency:  40MHz
MAC:                40:4c: ca: ff: fe: 5e:a7: 48
BASE MAC:           40:4c: ca: 5e:a7: 48
MAC_EXT: ff: fe

Uploading stub flasher...
Running stub flasher...
Stub flasher running.

Flash Memory Information:
=========================
Manufacturer: 68
Device: 4018
Detected flash size: 16MB

Hard resetting via RTS pin...




esptool v5.1.0
Serial port COM5:
Connecting....
Detecting chip type... ESP32-C6
Connected to ESP32-C6 on COM5:
Chip type:          ESP32-C6 (QFN40) (revision v0.1)
Features:           Wi-Fi 6, BT 5 (LE), IEEE802.15.4, Single Core + LP Core, 160MHz
Crystal frequency:  40MHz
MAC:                40:4c:ca:ff:fe:5e:a7:48
BASE MAC:           40:4c:ca:5e:a7:48
MAC_EXT:            ff:fe

Uploading stub flasher...
Running stub flasher...
Stub flasher running.

Warning: ESP32-C6 has no chip ID. Reading MAC address instead.
MAC:                40:4c:ca:ff:fe:5e:a7:48
BASE MAC:           40:4c:ca:5e:a7:48
MAC_EXT:            ff:fe

Hard resetting via RTS pin...



*/