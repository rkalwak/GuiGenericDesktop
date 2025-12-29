using FluentAssertions;
using SuplaTemplateBoard;
using SuplaTemplateBoard.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CompilationLib.Tests
{
    public class TemplateToBuildFlagsMapper_Tests
    {
        [Fact]
        public void SelectBuildFlags_WithNullConfig_ReturnsEmptyList()
        {
            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(null);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void SelectBuildFlags_WithEmptyConfig_ReturnsOnlyCoreFlags()
        {
            var config = new DeviceConfiguration();

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_CONFIG", "core flag should always be included");
        }

        [Fact]
        public void SelectBuildFlags_GosundSP111_SelectsCorrectFlags()
        {
            // Arrange: Parse Gosund SP111 template
            var parser = new TemplateBoardParser();
            var template = @"{""NAME"":""Gosund SP111 v1.1"",""GPIO"":[56,0,158,0,132,134,0,0,131,17,0,21,0],""FLAG"":0,""BASE"":45}";
            var config = parser.ParseTemplate(template);

            // Act
            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            // Assert
            using (new FluentAssertions.Execution.AssertionScope())
            {
                result.Should().Contain("SUPLA_CONFIG", "core configuration support");
                result.Should().Contain("SUPLA_RELAY", "device has 1 relay");
                result.Should().Contain("SUPLA_BUTTON", "device has 1 button");
                result.Should().Contain("SUPLA_LED", "device has 2 LEDs");
                result.Should().Contain("SUPLA_HLW8012", "device has power monitoring (CF, CF1, SEL pins)");
                result.Should().NotContain("SUPLA_DS18B20", "no temperature sensor");
                result.Should().NotContain("SUPLA_RGBW", "no RGBW channels");
            }
        }

        [Fact]
        public void SelectBuildFlags_ESP01DS18B20_SelectsCorrectFlags()
        {
            // Arrange: Parse ESP-01 with DS18B20 template
            var parser = new TemplateBoardParser();
            var template = @"{""NAME"":""ESP-01-01S-DS18B20-v1.0"",""GPIO"":[255,255,4,255,0,0,0,0,0,0,0,0,0],""FLAG"":0,""BASE"":18}";
            var config = parser.ParseTemplate(template);

            // Act
            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            // Assert
            using (new FluentAssertions.Execution.AssertionScope())
            {
                result.Should().Contain("SUPLA_CONFIG", "core configuration support");
                result.Should().Contain("SUPLA_DS18B20", "device has DS18B20 temperature sensor");
                result.Should().NotContain("SUPLA_RELAY", "no relays");
                result.Should().NotContain("SUPLA_BUTTON", "no buttons");
                result.Should().NotContain("SUPLA_LED", "no LEDs");
                result.Should().NotContain("SUPLA_HLW8012", "no power monitoring");
            }
        }

        [Fact]
        public void SelectBuildFlags_WithRelays_IncludesRelayFlag()
        {
            var config = new DeviceConfiguration();
            config.AddRelay(0, 15, false);

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_RELAY");
        }

        [Fact]
        public void SelectBuildFlags_WithButtons_IncludesButtonFlag()
        {
            var config = new DeviceConfiguration();
            config.AddButton(0, 13, "ON_PRESS", true, true, null);

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_BUTTON");
        }

        [Fact]
        public void SelectBuildFlags_WithLEDs_IncludesLedFlag()
        {
            var config = new DeviceConfiguration();
            config.AddLed(0, 2, false);

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_LED");
        }

        [Fact]
        public void SelectBuildFlags_WithRGBW_IncludesRgbwFlag()
        {
            var config = new DeviceConfiguration();
            config.AddRGBW(5, "BRIGHTNESS", false);

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_RGBW");
        }

        [Fact]
        public void SelectBuildFlags_WithLimitSwitches_IncludesLimitSwitchFlag()
        {
            var config = new DeviceConfiguration();
            config.AddLimitSwitch(0, 12);

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_LIMIT_SWITCH");
        }

        [Fact]
        public void SelectBuildFlags_WithConditions_IncludesConditionsFlag()
        {
            var config = new DeviceConfiguration();
            var condition = new Condition
            {
                ExecutiveType = 0,
                ExecutiveNumber = 0,
                SensorType = 1,
                SensorNumber = 0,
                ConditionType = 0,
                ValueOn = "25.0",
                ValueOff = "20.0"
            };
            config.AddCondition(condition);

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_CONDITIONS");
        }

        [Fact]
        public void SelectBuildFlags_WithDS18B20_IncludesDS18B20Flag()
        {
            var config = new DeviceConfiguration();
            config.SetGPIO(2, "DS18B20");
            config.MaxDS18B20 = 1;

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_DS18B20");
        }

        [Fact]
        public void SelectBuildFlags_WithMCP23017_IncludesMCP23017Flag()
        {
            var config = new DeviceConfiguration();
            config.AddExpander("MCP23017", new List<object> { 0x20 });

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_MCP23017");
        }

        [Fact]
        public void SelectBuildFlags_WithPCF8574_IncludesPCF8574Flag()
        {
            var config = new DeviceConfiguration();
            config.AddExpander("PCF8574", new List<object> { 0x20 });

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_PCF8574");
        }

        [Fact]
        public void SelectBuildFlags_WithPCF8575_IncludesPCF8575Flag()
        {
            var config = new DeviceConfiguration();
            config.AddExpander("PCF8575", new List<object> { 0x20 });

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_PCF8575");
        }

        [Fact]
        public void SelectBuildFlags_WithPowerMonitoring_IncludesHLW8012Flag()
        {
            var config = new DeviceConfiguration();
            config.SetGPIO(4, "CF1");
            config.SetGPIO(5, "CF");
            config.SetGPIO(12, "SEL");

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_HLW8012", "CF/CF1/SEL pins indicate HLW8012 power monitoring");
        }

        [Fact]
        public void CreateEnabledBuildFlags_WithNullParameters_ReturnsEmptyList()
        {
            var result = TemplateToBuildFlagsMapper.CreateEnabledBuildFlags(null, null);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void CreateEnabledBuildFlags_CreatesEnabledFlagObjects()
        {
            // Arrange
            var config = new DeviceConfiguration();
            config.AddRelay(0, 15, false);
            config.AddButton(0, 13, "ON_PRESS", true, true, null);

            var availableFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_CONFIG", FlagName = "SUPLA_CONFIG", Description = "Configuration support" },
                new BuildFlagItem { Key = "SUPLA_RELAY", FlagName = "SUPLA_RELAY", Description = "Relay support" },
                new BuildFlagItem { Key = "SUPLA_BUTTON", FlagName = "SUPLA_BUTTON", Description = "Button support" },
                new BuildFlagItem { Key = "SUPLA_LED", FlagName = "SUPLA_LED", Description = "LED support" }
            };

            // Act
            var result = TemplateToBuildFlagsMapper.CreateEnabledBuildFlags(config, availableFlags);

            // Assert
            using (new FluentAssertions.Execution.AssertionScope())
            {
                result.Should().NotBeEmpty();
                result.Should().Contain(f => f.FlagName == "SUPLA_CONFIG");
                result.Should().Contain(f => f.FlagName == "SUPLA_RELAY");
                result.Should().Contain(f => f.FlagName == "SUPLA_BUTTON");
                result.Should().NotContain(f => f.FlagName == "SUPLA_LED");
                result.Should().OnlyContain(f => f.IsEnabled == true, "all returned flags should be enabled");
            }
        }

        [Fact]
        public void GetConfigurationSummary_WithNullConfig_ReturnsNoConfiguration()
        {
            var result = TemplateToBuildFlagsMapper.GetConfigurationSummary(null);

            result.Should().Be("No configuration available");
        }

        [Fact]
        public void GetConfigurationSummary_WithEmptyConfig_ReturnsNoFeatures()
        {
            var config = new DeviceConfiguration { HostName = "Test Device" };

            var result = TemplateToBuildFlagsMapper.GetConfigurationSummary(config);

            result.Should().Contain("Test Device");
            result.Should().Contain("No features detected");
        }

        [Fact]
        public void GetConfigurationSummary_GosundSP111_ReturnsCorrectSummary()
        {
            // Arrange: Parse Gosund SP111 template
            var parser = new TemplateBoardParser();
            var template = @"{""NAME"":""Gosund SP111 v1.1"",""GPIO"":[56,0,158,0,132,134,0,0,131,17,0,21,0],""FLAG"":0,""BASE"":45}";
            var config = parser.ParseTemplate(template);

            // Act
            var result = TemplateToBuildFlagsMapper.GetConfigurationSummary(config);

            // Assert
            using (new FluentAssertions.Execution.AssertionScope())
            {
                result.Should().Contain("Gosund SP111 v1.1");
                result.Should().Contain("1 relay(s)");
                result.Should().Contain("1 button(s)");
                result.Should().Contain("1 LED(s)");
            }
        }

        [Fact]
        public void GetConfigurationSummary_ESP01DS18B20_ReturnsCorrectSummary()
        {
            // Arrange: Parse ESP-01 with DS18B20 template
            var parser = new TemplateBoardParser();
            var template = @"{""NAME"":""ESP-01-01S-DS18B20-v1.0"",""GPIO"":[255,255,4,255,0,0,0,0,0,0,0,0,0],""FLAG"":0,""BASE"":18}";
            var config = parser.ParseTemplate(template);

            // Act
            var result = TemplateToBuildFlagsMapper.GetConfigurationSummary(config);

            // Assert
            using (new FluentAssertions.Execution.AssertionScope())
            {
                result.Should().Contain("ESP-01-01S-DS18B20-v1.0");
                result.Should().Contain("1 DS18B20 sensor(s)");
                result.Should().NotContain("relay");
                result.Should().NotContain("button");
            }
        }

        [Fact]
        public void SelectBuildFlags_WithMultipleExpanders_IncludesAllExpanderFlags()
        {
            var config = new DeviceConfiguration();
            config.AddExpander("MCP23017", new List<object> { 0x20 });
            config.AddExpander("PCF8574", new List<object> { 0x21 });
            config.AddExpander("PCF8575", new List<object> { 0x22 });

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                result.Should().Contain("SUPLA_MCP23017");
                result.Should().Contain("SUPLA_PCF8574");
                result.Should().Contain("SUPLA_PCF8575");
            }
        }

        [Fact]
        public void SelectBuildFlags_WithSI7021Sensor_IncludesSI7021Flag()
        {
            var config = new DeviceConfiguration();
            config.SetGPIO(4, "SI7021_SONOFF");

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_SI7021_SONOFF");
        }

        [Fact]
        public void SelectBuildFlags_WithCSE7766Sensor_IncludesCSE7766Flag()
        {
            var config = new DeviceConfiguration();
            config.SetGPIO(3, "CSE7766_RX");

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_CSE7766");
        }

        [Fact]
        public void SelectBuildFlags_WithNTCSensor_IncludesNTCFlag()
        {
            var config = new DeviceConfiguration();
            config.SetGPIO(36, "NTC_10K");

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().Contain("SUPLA_NTC_10K");
        }

        [Fact]
        public void CreateEnabledBuildFlags_PreservesParameters()
        {
            var config = new DeviceConfiguration();
            config.AddRelay(0, 15, false);

            var availableFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem 
                { 
                    Key = "SUPLA_RELAY", 
                    FlagName = "SUPLA_RELAY",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Name = "Count", Type = "number", DefaultValue = "1" }
                    }
                }
            };

            var result = TemplateToBuildFlagsMapper.CreateEnabledBuildFlags(config, availableFlags);

            var relayFlag = result.FirstOrDefault(f => f.FlagName == "SUPLA_RELAY");
            relayFlag.Should().NotBeNull();
            relayFlag.Parameters.Should().HaveCount(1);
            relayFlag.Parameters[0].Name.Should().Be("Count");
        }

        [Fact]
        public void SelectBuildFlags_DoesNotIncludeDuplicates()
        {
            var config = new DeviceConfiguration();
            config.AddRelay(0, 15, false);
            config.AddRelay(1, 16, false);
            config.SetGPIO(5, "CF"); // HLW8012
            config.SetGPIO(4, "CF1"); // HLW8012

            var result = TemplateToBuildFlagsMapper.SelectBuildFlags(config);

            result.Should().OnlyHaveUniqueItems("no duplicate flags should be included");
            result.Count(f => f == "SUPLA_RELAY").Should().Be(1);
            result.Count(f => f == "SUPLA_HLW8012").Should().Be(1);
        }
    }
}
