using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CompilationLib.Tests.IntegrationTests
{
    public static class PlatformProviders
    {
        public static IEnumerable<object[]> PlatformsData()
        {
            return BuildFlags.Platforms.Select(p => new object[] { p });
        }
    }

    // Batch 1: flags 1..10
    // All tests in this file are integration tests that require Platform.IO

    public class SUPLA_CONFIG_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_CONFIG" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DEEP_SLEEP_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DEEP_SLEEP" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_OLED_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_OLED" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_WAKE_ON_LAN_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_WAKE_ON_LAN" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_WT32_ETH01_LAN8720_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_WT32_ETH01_LAN8720" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_ETH01_LAN8720_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_ETH01_LAN8720" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MS5611_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MS5611" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms_WithParameter(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p,
                    new List<BuildFlagItem>
                    {
                        new BuildFlagItem
                        {
                            Key = "SUPLA_MS5611",
                            Parameters=new List<Parameter>
                            {
                                new Parameter
                                {
                                    Name="Altitude",
                                    Type="number",
                                    Value="250"
                                }
                            }
                        }
                    },
                temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_THERMOSTAT_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_THERMOSTAT" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_ROLLERSHUTTER_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_ROLLERSHUTTER" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_LED_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_LED" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_PUSHOVER_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_PUSHOVER" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DIRECT_LINKS_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DIRECT_LINKS" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MODBUS_SDM_ONE_PHASE_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MODBUS_SDM_ONE_PHASE" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MODBUS_SDM_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MODBUS_SDM" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_RGBW_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_RGBW" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_HC_SR04_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_HC_SR04" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_IMPULSE_COUNTER_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_IMPULSE_COUNTER" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DIRECT_LINKS_SENSOR_THERMOMETR_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DIRECT_LINKS_SENSOR_THERMOMETR" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DIRECT_LINKS_MULTI_SENSOR_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DIRECT_LINKS_MULTI_SENSOR" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_VINDRIKTNING_IKEA_KPOP_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_VINDRIKTNING_IKEA_KPOP" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_PMSX003_KPOP_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_PMSX003_KPOP" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_BONEIO_32x10A_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_BONEIO_32x10A" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_BONEIO_24x16A_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_BONEIO_24x16A" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_SPS30_KPOP_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_SPS30_KPOP" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MAX6675_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MAX6675" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MAX31855_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MAX31855" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_ANALOG_READING_KPOP_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_ANALOG_READING_KPOP" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_NTC_10K_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_NTC_10K" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MPX_5XXX_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MPX_5XXX" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_HLW8012_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_HLW8012" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_PZEM_V_3_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_PZEM_V_3" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    

    public class SUPLA_CSE7766_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_CSE7766" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_ADE7953_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_ADE7953" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_MCP23017_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_MCP23017" }
                }, temp);
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

    public class SUPLA_PCF8575_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_PCF8575" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_PCF8574_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_PCF8574" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DS18B20_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DS18B20" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DHT11_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DHT11" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_DHT22_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_DHT22" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_SI7021_SONOFF_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_SI7021_SONOFF" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_BME280_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_BME280" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_BMP280_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_BMP280" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_SHT3x_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_SHT3x" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INITIAL_CONFIG_MODE_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms_WithMode0(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p,
                    new List<BuildFlagItem>
                    {
                        new BuildFlagItem
                        {
                            Key = "SUPLA_INITIAL_CONFIG_MODE",
                            Parameters = new List<Parameter>
                            {
                                new Parameter
                                {
                                    Name = "Mode",
                                    Type = "enum",
                                    Value = "0"
                                }
                            }
                        }
                    },
                    temp);
                using (new AssertionScope())
                {
                    res.IsSuccessful.Should().BeTrue();
                    if (!res.IsSuccessful)
                        res.Logs.Should().Be("");
                }
            }
            finally { CleanupTempRepo(temp); }
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms_WithMode1(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p,
                    new List<BuildFlagItem>
                    {
                        new BuildFlagItem
                        {
                            Key = "SUPLA_INITIAL_CONFIG_MODE",
                            Parameters = new List<Parameter>
                            {
                                new Parameter
                                {
                                    Name = "Mode",
                                    Type = "enum",
                                    Value = "1"
                                }
                            }
                        }
                    },
                    temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms_WithMode2(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p,
                    new List<BuildFlagItem>
                    {
                        new BuildFlagItem
                        {
                            Key = "SUPLA_INITIAL_CONFIG_MODE",
                            Parameters = new List<Parameter>
                            {
                                new Parameter
                                {
                                    Name = "Mode",
                                    Type = "enum",
                                    Value = "2"
                                }
                            }
                        }
                    },
                    temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms_WithMode3(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p,
                    new List<BuildFlagItem>
                    {
                        new BuildFlagItem
                        {
                            Key = "SUPLA_INITIAL_CONFIG_MODE",
                            Parameters = new List<Parameter>
                            {
                                new Parameter
                                {
                                    Name = "Mode",
                                    Type = "enum",
                                    Value = "3"
                                }
                            }
                        }
                    },
                    temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms_WithoutParameter(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INITIAL_CONFIG_MODE" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA219_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA219" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA226_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA226" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA228_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA228" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA229_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA229" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA236_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA236" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA238_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA238" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA239_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA239" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }

    public class SUPLA_INA260_Tests : PlatformioTestBase
    {
        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(PlatformProviders.PlatformsData), MemberType = typeof(PlatformProviders))]
        public async Task Runs_On_All_Platforms(string p)
        {
            var temp = CreateTempRepoCopy();
            try
            {
                var res = await RunHandlerAsync(p, new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_INA260" }
                }, temp);
                AssertBuildSuccessful(res);
            }
            finally { CleanupTempRepo(temp); }
        }
    }
}
