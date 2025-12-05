using CompilationLib;
using System.Threading.Tasks;
using Xunit;

namespace CompilationLib.Tests
{
    public class DeviceDetectorTests
    {
        private class FakeEsptoolWrapper : IEsptoolWrapper
        {
            private readonly string _chipStdOut;
            private readonly string _chipStdErr;
            private readonly string _flashStdOut;
            private readonly string _flashStdErr;

            public FakeEsptoolWrapper(string chipStdOut, string flashStdOut, string chipStdErr = "", string flashStdErr = "")
            {
                _chipStdOut = chipStdOut;
                _chipStdErr = chipStdErr;
                _flashStdOut = flashStdOut;
                _flashStdErr = flashStdErr;
            }

            public Task<EsptoolResult> ReadChipId(string comPort, System.Threading.CancellationToken cancellation = default)
            => Task.FromResult(new EsptoolResult { Success = true, ExitCode = 0, StdOut = _chipStdOut, StdErr = _chipStdErr, Command = "esptool --chip-id" });

            public Task<EsptoolResult> ReadFlashId(string comPort, System.Threading.CancellationToken cancellation = default)
            => Task.FromResult(new EsptoolResult { Success = true, ExitCode = 0, StdOut = _flashStdOut, StdErr = _flashStdErr, Command = "esptool --flash-id" });
        }

        [Fact]
        public async Task DetectEspModelAsync_Parses_Esp32C6_Output()
        {
            string chipOut = "esptool v5.1.0\r\nSerial port COM5:\r\nConnecting....Detecting chip type... ESP32-C6\r\nConnected to ESP32-C6 on COM5:\r\nChip type:          ESP32-C6 (QFN40) (revision v0.1)\r\nFeatures:           Wi-Fi 6, BT 5 (LE), IEEE802.15.4, Single Core + LP Core, 160MHz\r\nMAC:                40:4c:ca:ff:fe:5e:a7:48\r\n";
            string flashOut = "Flash Memory Information:\r\n=========================\r\nManufacturer: 68\r\nDevice: 4018\r\nDetected flash size: 16MB\r\n";

            var fake = new FakeEsptoolWrapper(chipOut, flashOut);
            var detector = new DeviceDetector(fake);

            var info = await detector.DetectEspModelAsync("COM5");

            Assert.NotNull(info);
            Assert.True(info.IsRecognized);
            Assert.Contains("ESP32-C6", info.ChipType);
            Assert.Equal("16MB", info.FlashSize);
            Assert.Contains("40:4c:ca", info.Mac);
            Assert.Contains("Wi-Fi 6", info.Features);
        }

        [Fact]
        public async Task DetectEspModelAsync_Parses_Esp32_Output()
        {
            string chipOut = "esptool v3.1.0\r\nSerial port COM3:\r\nConnecting....\r\nDetecting chip type... ESP32\r\nChip type:          ESP32-D0WDQ6 (revision 1)\r\nFeatures:           Wi-Fi, BT, Dual Core, 240MHz\r\nCrystal frequency:  40MHz\r\nMAC:                24:6F:28:AA:BB:CC\r\n";
            string flashOut = "Manufacturer: ef\r\nDevice: 4016\r\nFlash size: 4MB\r\n";

            var fake = new FakeEsptoolWrapper(chipOut, flashOut);
            var detector = new DeviceDetector(fake);

            var info = await detector.DetectEspModelAsync("COM3");

            Assert.NotNull(info);
            Assert.True(info.IsRecognized);
            Assert.Contains("ESP32", info.ChipType);
            Assert.Equal("4MB", info.FlashSize);
            Assert.Contains("24:6F:28", info.Mac);
            Assert.Contains("Wi-Fi", info.Features);
        }
    }
}
