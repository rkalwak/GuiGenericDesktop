using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CompilationLib.Tests
{
    // Batch 1: flags 1..10

    public class SUPLA_CONFIG_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_CONFIG" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DEEP_SLEEP_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DEEP_SLEEP" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_OLED_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_OLED" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_WAKE_ON_LAN_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_WAKE_ON_LAN" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_WT32_ETH01_LAN8720_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_WT32_ETH01_LAN8720" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_ETH01_LAN8720_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_ETH01_LAN8720" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MS5611_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MS5611" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_THERMOSTAT_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_THERMOSTAT" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_ROLLERSHUTTER_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_ROLLERSHUTTER" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_LED_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_LED" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_PUSHOVER_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_PUSHOVER" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DIRECT_LINKS_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DIRECT_LINKS" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MODBUS_SDM_ONE_PHASE_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MODBUS_SDM_ONE_PHASE" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MODBUS_SDM_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MODBUS_SDM" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_RGBW_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_RGBW" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_HC_SR04_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_HC_SR04" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_IMPULSE_COUNTER_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_IMPULSE_COUNTER" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DIRECT_LINKS_SENSOR_THERMOMETR_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DIRECT_LINKS_SENSOR_THERMOMETR" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DIRECT_LINKS_MULTI_SENSOR_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DIRECT_LINKS_MULTI_SENSOR" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_VINDRIKTNING_IKEA_KPOP_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_VINDRIKTNING_IKEA_KPOP" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_PMSX003_KPOP_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_PMSX003_KPOP" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_BONEIO_32x10A_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_BONEIO_32x10A" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_BONEIO_24x16A_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_BONEIO_24x16A" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_SPS30_KPOP_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_SPS30_KPOP" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MAX6675_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MAX6675" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MAX31855_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MAX31855" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_ANALOG_READING_KPOP_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_ANALOG_READING_KPOP" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_NTC_10K_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_NTC_10K" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MPX_5XXX_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MPX_5XXX" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_HLW8012_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_HLW8012" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_PZEM_V_3_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_PZEM_V_3" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_CSE7766_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_CSE7766" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_ADE7953_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_ADE7953" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_MCP23017_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try
                {
                    var res = await RunHandlerAsync(p, new List<string> { "SUPLA_MCP23017" }, temp);
                    using (new AssertionScope())
                    {
                        res.IsSuccessful.Should().BeTrue();
                        if (!res.IsSuccessful)
                            res.Logs.Should().Be("");
                    }
                }
                finally
                {
                    CleanupTempRepo(temp);
                }
            }
        }
    }

    public class SUPLA_PCF8575_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_PCF8575" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_PCF8574_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_PCF8574" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DS18B20_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DS18B20" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DHT11_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DHT11" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_DHT22_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_DHT22" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_SI7021_SONOFF_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_SI7021_SONOFF" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_BME280_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_BME280" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_BMP280_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_BMP280" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }

    public class SUPLA_SHT3x_Tests : PlatformioTestBase
    {
        [Fact]
        public async Task Runs_On_All_Platforms()
        {
            foreach (var p in BuildFlags.Platforms)
            {
                var temp = CreateTempRepoCopy();
                try { var res = await RunHandlerAsync(p, new List<string> { "SUPLA_SHT3x" }, temp); Assert.True(res.IsSuccessful); }
                finally { CleanupTempRepo(temp); }
            }
        }
    }
}
