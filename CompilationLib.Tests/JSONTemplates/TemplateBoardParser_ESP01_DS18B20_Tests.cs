using FluentAssertions;
using SuplaTemplateBoard;
using System.Linq;
using Xunit;

namespace CompilationLib.Tests.JSONTemplates
{
    /// <summary>
    /// Tests ESP-01-01S-DS18B20-v1.0 template parsing, old format
    /// JSON: {"NAME":"ESP-01-01S-DS18B20-v1.0","GPIO":[255,255,4,255,0,0,0,0,0,0,0,0,0],"FLAG":0,"BASE":18}
    /// Expected configuration after conversion:
    /// GPIO00 - User (255 → 1)
    /// GPIO01 - User (255 → 1)
    /// GPIO02 - DS18x20 (4 → 1216)
    /// GPIO03 - User (255 → 1)
    /// GPIO04 - None (0 → 0)
    /// GPIO05 - None (0 → 0)
    /// GPIO09 - None (0 → 0)
    /// GPIO10 - None (0 → 0)
    /// GPIO12 - None (0 → 0)
    /// GPIO13 - None (0 → 0)
    /// GPIO14 - None (0 → 0)
    /// GPIO15 - None (0 → 0)
    /// GPIO16 - None (0 → 0)
    /// </summary>
    public class TemplateBoardParser_ESP01_DS18B20_Tests
    {
        private const string TestJsonTemplate = @"{""NAME"":""ESP-01-01S-DS18B20-v1.0"",""GPIO"":[255,255,4,255,0,0,0,0,0,0,0,0,0],""FLAG"":0,""BASE"":18}";
        private const string TestJsonTemplateNew = @"{""NAME"":""ESP-01-01S-DS18B20-v1.0"",""GPIO"":[1,1,1216,1,0,0,0,0,0,0,0,0,0,0]}";

        [Fact]
        public void ParseTemplate_ShouldSetCorrectDeviceName()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.HostName.Should().Be("ESP-01-01S-DS18B20-v1.0");
            config.HostName.Should().NotBeNullOrWhiteSpace();
            config.HostName.Should().HaveLength(23);
        }

        [Fact]
        public void ParseTemplate_ShouldDetectOldVersion()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            parser.Warnings.Should().Contain(w => w.Contains("Wersja: 1"));
        }

        [Fact]
        public void ParseTemplate_ShouldConfigureDS18B20Sensor()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.MaxDS18B20.Should().Be(1, "one DS18B20 sensor is configured on GPIO2");
                config.GetGPIO(2).Should().Be("DS18B20", "GPIO2 has DS18x20 function (old code 4 → new 1216)");
            }
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoRelays()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.Relays.Should().BeEmpty();
            config.MaxRelays.Should().Be(0);
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoButtons()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.Buttons.Should().BeEmpty();
            config.MaxButtons.Should().Be(0);
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoLEDs()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.Leds.Should().BeEmpty();
            config.MaxLeds.Should().Be(0);
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoConditions()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.Conditions.Should().BeEmpty();
            config.MaxConditions.Should().Be(0);
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoAnalogButtons()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.AnalogButtons.Should().BeEmpty();
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoExpanders()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.Expanders.Should().BeEmpty();
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoRGBWChannels()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.RGBWs.Should().BeEmpty();
            config.MaxRGBW.Should().Be(0);
        }

        [Fact]
        public void ParseTemplate_ShouldHaveNoLimitSwitches()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.LimitSwitches.Should().BeEmpty();
            config.MaxLimitSwitches.Should().Be(0);
        }

        [Fact]
        public void ParseTemplate_ShouldHaveDefaultConfigMode()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.ConfigMode.Should().Be("CONFIG_MODE_10_ON_PRESSES");
        }

        [Fact]
        public void ParseTemplate_ShouldHandleFlagAndBaseProperties()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.Should().NotBeNull();
                // FLAG and BASE are currently not processed by parser
                parser.Warnings.Should().NotContain(w => w.Contains("FLAG"));
                parser.Warnings.Should().NotContain(w => w.Contains("BASE"));
            }
        }

        [Fact]
        public void ParseTemplate_ConfigDictionary_ShouldHaveCorrectValues()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();

            using (new FluentAssertions.Execution.AssertionScope())
            {
                configDict["HostName"].Should().Be("ESP-01-01S-DS18B20-v1.0");
                configDict["MaxRelays"].Should().Be(0);
                configDict["MaxButtons"].Should().Be(0);
                configDict["MaxLeds"].Should().Be(0);
                configDict["MaxRGBW"].Should().Be(0);
                configDict["MaxLimitSwitches"].Should().Be(0);
                configDict["MaxConditions"].Should().Be(0);
                configDict["ConfigMode"].Should().Be("CONFIG_MODE_10_ON_PRESSES");
                configDict.Should().ContainKey("MaxDS18B20");
                configDict["MaxDS18B20"].Should().Be(1);
            }
        }

        [Fact]
        public void ParseTemplate_ShouldValidateAllMaxCounters()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.MaxRelays.Should().Be(0, "no relays are configured");
                config.MaxButtons.Should().Be(0, "no buttons are configured");
                config.MaxLeds.Should().Be(0, "no LEDs are configured");
                config.MaxRGBW.Should().Be(0, "no RGBW channels are configured");
                config.MaxLimitSwitches.Should().Be(0, "no limit switches are configured");
                config.MaxConditions.Should().Be(0, "no conditions are configured");
                config.MaxDS18B20.Should().Be(1, "one DS18B20 sensor is configured");
            }
        }

        [Fact]
        public void ParseTemplate_UnusedGPIOsShouldBeOff()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // GPIO 0, 1, 3 are marked as "User" (255) which maps to Users function
                // GPIO 2 is DS18B20
                // All others (4, 5, 9, 10, 12, 13, 14, 15, 16) are None (0) and should be OFF
                config.GetGPIO(4).Should().Be("OFF", "GPIO4 is None");
                config.GetGPIO(5).Should().Be("OFF", "GPIO5 is None");
                config.GetGPIO(9).Should().Be("OFF", "GPIO9 is None");
                config.GetGPIO(10).Should().Be("OFF", "GPIO10 is None");
                config.GetGPIO(12).Should().Be("OFF", "GPIO12 is None");
                config.GetGPIO(13).Should().Be("OFF", "GPIO13 is None");
                config.GetGPIO(14).Should().Be("OFF", "GPIO14 is None");
                config.GetGPIO(15).Should().Be("OFF", "GPIO15 is None");
                config.GetGPIO(16).Should().Be("OFF", "GPIO16 is None");
            }
        }

        [Fact]
        public void ParseTemplate_UserGPIOsShouldBeOff()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // GPIO 0, 1, 3 are marked as User (255 → 1)
                // User function doesn't create any configuration entries, so they're effectively OFF
                config.GetGPIO(0).Should().Be("OFF", "GPIO0 is User function");
                config.GetGPIO(1).Should().Be("OFF", "GPIO1 is User function");
                config.GetGPIO(3).Should().Be("OFF", "GPIO3 is User function");
            }
        }

        [Fact]
        public void ParseTemplate_ShouldNotGenerateErrors()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            parser.Warnings.Should().NotContain(w => w.Contains("Error:") || w.Contains("JSON Parse Error:"));
        }

        [Fact]
        public void ParseTemplate_ParserWarnings_ShouldBeValid()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                parser.Warnings.Should().NotBeNull();
                parser.Warnings.Should().HaveCountGreaterThan(0, "at least version warning should be present");
                parser.Warnings.Should().Contain(w => w.Contains("Wersja: 1"));
                parser.Warnings.Should().NotContain(w => w.Contains("Błąd wczytania"));
                parser.Warnings.Should().NotContain(w => w.Contains("JSON Parse Error"));
            }
        }

        [Fact]
        public void ParseTemplate_DS18B20OnGPIO2_ShouldBeCorrectlyMapped()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();
            var gpioFunctions = configDict["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Verify DS18B20 sensor is on GPIO2
                gpioFunctions.Should().ContainKey(2);
                gpioFunctions[2].Should().Be("DS18B20", "GPIO2 has DS18x20 temperature sensor");
            }
        }

        [Fact]
        public void ParseTemplate_ShouldBeTemperatureSensorConfiguration()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // ESP-01 module with DS18B20 sensor - minimal configuration
                config.MaxRelays.Should().Be(0, "temperature sensor doesn't need relays");
                config.MaxButtons.Should().Be(0, "no physical buttons on ESP-01");
                config.Leds.Should().BeEmpty("no status LEDs configured");
                config.MaxDS18B20.Should().Be(1, "has one DS18B20 temperature sensor");
            }
        }

        [Fact]
        public void ParseTemplate_OldFormatConversion_ShouldWork()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Verify old format code was successfully converted
                // Old code 4 → New 1216 (DS18x20)
                config.MaxDS18B20.Should().Be(1);
                config.GetGPIO(2).Should().Be("DS18B20");
                
                // Old code 255 → New 1 (User) - these don't create configuration entries
                // So GPIO 0, 1, 3 should be OFF
                config.GetGPIO(0).Should().Be("OFF");
                config.GetGPIO(1).Should().Be("OFF");
                config.GetGPIO(3).Should().Be("OFF");
            }
        }

        [Fact]
        public void ParseTemplate_ConfigDictionary_ShouldBeSerializable()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();

            using (new FluentAssertions.Execution.AssertionScope())
            {
                configDict.Should().NotBeNull();
                configDict.Keys.Should().AllBeOfType<string>();
                configDict.Values.Should().NotContainNulls();
                
                var expectedKeys = new[] 
                { 
                    "HostName", "MaxRelays", "MaxButtons", "MaxLeds", "MaxRGBW", 
                    "MaxLimitSwitches", "MaxConditions", "ConfigMode", "Relays", 
                    "Buttons", "Leds", "RGBWs", "LimitSwitches", "Conditions", 
                    "AnalogButtons", "Expanders", "GPIOFunctions", "MaxDS18B20"
                };
                configDict.Keys.Should().Contain(expectedKeys);
            }
        }

        [Fact]
        public void ParseTemplate_GPIOMapping_ShouldMatchExpectedConfiguration()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();
            var gpioFunctions = configDict["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

            using (new FluentAssertions.Execution.AssertionScope())
            {
                gpioFunctions.Should().NotBeNull();
                
                // Only GPIO2 should have a function assigned (DS18B20)
                gpioFunctions.Should().HaveCount(1, "only DS18B20 sensor is configured");
                gpioFunctions.Should().ContainKey(2, "DS18B20 sensor");
                gpioFunctions[2].Should().Be("DS18B20");
            }
        }

        [Fact]
        public void ParseTemplate_MinimalConfiguration_ShouldBeValid()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // ESP-01 with DS18B20 is a minimal configuration
                config.Relays.Should().BeEmpty();
                config.Buttons.Should().BeEmpty();
                config.Leds.Should().BeEmpty();
                config.RGBWs.Should().BeEmpty();
                config.LimitSwitches.Should().BeEmpty();
                config.Conditions.Should().BeEmpty();
                config.AnalogButtons.Should().BeEmpty();
                config.Expanders.Should().BeEmpty();
                
                // Only DS18B20 sensor is configured
                config.MaxDS18B20.Should().Be(1);
            }
        }

        [Fact]
        public void ParseTemplate_MultipleParses_ShouldClearPreviousConfiguration()
        {
            var parser = new TemplateBoardParser();
            string template1 = @"{""NAME"":""Device1"",""GPIO"":[224,0,0,0,0,0,0,0,0,0,0,0,0]}";
            string template2 = TestJsonTemplate;

            var config1 = parser.ParseTemplate(template1);
            var config2 = parser.ParseTemplate(template2);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config2.HostName.Should().Be("ESP-01-01S-DS18B20-v1.0");
                config2.MaxRelays.Should().Be(0, "second template has no relays");
                config2.MaxDS18B20.Should().Be(1, "second template has DS18B20");
                config2.Relays.Should().BeEmpty();
            }
        }

        [Fact]
        public void ParseTemplate_WithDifferentName_ShouldStillParseCorrectly()
        {
            string customTemplate = @"{""NAME"":""Custom ESP-01"",""GPIO"":[255,255,4,255,0,0,0,0,0,0,0,0,0],""FLAG"":0,""BASE"":18}";
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(customTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.HostName.Should().Be("Custom ESP-01");
                config.MaxDS18B20.Should().Be(1, "DS18B20 configuration is the same");
                config.GetGPIO(2).Should().Be("DS18B20");
            }
        }

        [Fact]
        public void ParseTemplate_OldCode255_ShouldConvertToUser()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Old code 255 represents "User" pins (available for user configuration)
                // These pins are GPIO 0, 1, and 3 in this template
                // User function (enum value 1) doesn't create any configuration entries
                // so these GPIOs should be OFF in the configuration
                config.GetGPIO(0).Should().Be("OFF", "User GPIO doesn't create config entry");
                config.GetGPIO(1).Should().Be("OFF", "User GPIO doesn't create config entry");
                config.GetGPIO(3).Should().Be("OFF", "User GPIO doesn't create config entry");
            }
        }

        [Fact]
        public void ParseTemplate_ESP01Module_ShouldHaveCorrectGPIOCount()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // ESP-01 module has limited GPIOs: 0, 1, 2, 3
                // Template has 13 elements (old format)
                parser.Warnings.Should().Contain(w => w.Contains("Wersja: 1"));
                
                // Only GPIO2 should have an actual function (DS18B20)
                var configDict = config.ToConfigDictionary();
                var gpioFunctions = configDict["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;
                gpioFunctions.Should().HaveCount(1);
            }
        }

        [Fact]
        public void ParseTemplate_OldAndNewFormat_ShouldProduceSameConfiguration()
        {
            var parserOld = new TemplateBoardParser();
            var parserNew = new TemplateBoardParser();

            var configOld = parserOld.ParseTemplate(TestJsonTemplate);
            var configNew = parserNew.ParseTemplate(TestJsonTemplateNew);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Verify version detection
                parserOld.Warnings.Should().Contain(w => w.Contains("Wersja: 1"), "old format has 13 GPIO elements");
                parserNew.Warnings.Should().Contain(w => w.Contains("Wersja: 2"), "new format has 14 GPIO elements");

                // Both should have same device name
                configOld.HostName.Should().Be(configNew.HostName);

                // Both should detect DS18B20 sensor
                configOld.MaxDS18B20.Should().Be(1);
                configNew.MaxDS18B20.Should().Be(1);
                configOld.GetGPIO(2).Should().Be("DS18B20");
                configNew.GetGPIO(2).Should().Be("DS18B20");

                // Both should have no relays, buttons, LEDs
                configOld.MaxRelays.Should().Be(configNew.MaxRelays).And.Be(0);
                configOld.MaxButtons.Should().Be(configNew.MaxButtons).And.Be(0);
                configOld.MaxLeds.Should().Be(configNew.MaxLeds).And.Be(0);
                configOld.MaxRGBW.Should().Be(configNew.MaxRGBW).And.Be(0);
                configOld.MaxLimitSwitches.Should().Be(configNew.MaxLimitSwitches).And.Be(0);
                configOld.MaxConditions.Should().Be(configNew.MaxConditions).And.Be(0);

                // Both should have same relays count
                configOld.Relays.Should().HaveCount(configNew.Relays.Count);
                configOld.Buttons.Should().HaveCount(configNew.Buttons.Count);
                configOld.Leds.Should().HaveCount(configNew.Leds.Count);
                configOld.RGBWs.Should().HaveCount(configNew.RGBWs.Count);
                configOld.LimitSwitches.Should().HaveCount(configNew.LimitSwitches.Count);
                configOld.Conditions.Should().HaveCount(configNew.Conditions.Count);
                configOld.AnalogButtons.Should().HaveCount(configNew.AnalogButtons.Count);
                configOld.Expanders.Should().HaveCount(configNew.Expanders.Count);

                // Both should have same GPIO configuration
                var configDictOld = configOld.ToConfigDictionary();
                var configDictNew = configNew.ToConfigDictionary();
                
                var gpioFunctionsOld = configDictOld["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;
                var gpioFunctionsNew = configDictNew["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

                gpioFunctionsOld.Should().HaveCount(gpioFunctionsNew.Count, "both formats should configure same number of GPIOs");
                gpioFunctionsOld.Should().ContainKey(2);
                gpioFunctionsNew.Should().ContainKey(2);
                gpioFunctionsOld[2].Should().Be(gpioFunctionsNew[2]).And.Be("DS18B20");

                // Verify all GPIO functions match
                foreach (var gpio in gpioFunctionsOld.Keys)
                {
                    gpioFunctionsNew.Should().ContainKey(gpio, $"new format should have GPIO{gpio} configured");
                    gpioFunctionsOld[gpio].Should().Be(gpioFunctionsNew[gpio], $"GPIO{gpio} function should match");
                }

                // Both should have same config mode
                configOld.ConfigMode.Should().Be(configNew.ConfigMode);
            }
        }
    }
}
