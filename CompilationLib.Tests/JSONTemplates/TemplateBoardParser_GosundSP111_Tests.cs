using FluentAssertions;
using SuplaTemplateBoard;
using System.Linq;
using Xunit;

namespace CompilationLib.Tests.JSONTemplates
{
    /// <summary>
    /// Tests Gosund SP111 v1.1 template parsing, old format
    /// JSON: {"NAME":"Gosund SP111 v1.1","GPIO":[56,0,158,0,132,134,0,0,131,17,0,21,0],"FLAG":0,"BASE":45}
    /// Expected configuration after conversion:
    /// GPIO00 - Led1i
    /// GPIO01 - None
    /// GPIO02 - LedLinki
    /// GPIO03 - None
    /// GPIO04 - HLWBL CF1
    /// GPIO05 - BL0937 CF
    /// GPIO09 - None
    /// GPIO10 - None
    /// GPIO12 - HLWBL SELi
    /// GPIO13 - Button1
    /// GPIO14 - None
    /// GPIO15 - Relay1
    /// GPIO16 - None
    /// </summary>
    public class TemplateBoardParser_GosundSP111_Tests
    {
        private const string TestJsonTemplate = @"{""NAME"":""Gosund SP111 v1.1"",""GPIO"":[56,0,158,0,132,134,0,0,131,17,0,21,0],""FLAG"":0,""BASE"":45}";
        private const string TestJsonTemplateNew = @"{""NAME"":""Gosund SP111 v1.1"",""GPIO"":[320,0,576,0,2656,2720,0,0,2624,32,0,224,0,0]}";

        [Fact]
        public void ParseTemplate_ShouldSetCorrectDeviceName()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.HostName.Should().Be("Gosund SP111 v1.1");
            config.HostName.Should().NotBeNullOrWhiteSpace();
            config.HostName.Should().HaveLength(17);
        }

        [Fact]
        public void ParseTemplate_ShouldDetectOldVersion()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            parser.Warnings.Should().Contain(w => w.Contains("Wersja: 1"));
        }

        [Fact]
        public void ParseTemplate_ShouldConfigureOneRelay()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.Relays.Should().HaveCount(1);
                config.MaxRelays.Should().Be(1);
                config.Relays[0].Number.Should().Be(0);
                config.Relays[0].GPIO.Should().Be(15, "GPIO15 is mapped to Relay1");
                config.Relays[0].Inverted.Should().BeFalse();
            }
        }

        [Fact]
        public void ParseTemplate_ShouldConfigureOneButton()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.Buttons.Should().HaveCount(1);
                config.MaxButtons.Should().Be(1);
                config.Buttons[0].Number.Should().Be(0);
                config.Buttons[0].GPIO.Should().Be(13, "GPIO13 is mapped to Button1");
                config.Buttons[0].EventType.Should().Be("ON_PRESS");
                config.Buttons[0].Pullup.Should().BeTrue();
                config.Buttons[0].Inverted.Should().BeTrue();
            }
        }

        [Fact]
        public void ParseTemplate_ShouldConfigureTwoInvertedLEDs()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.Leds.Should().HaveCount(2, "device has Led1i and LedLinki");
                config.MaxLeds.Should().Be(1);
                
                // Led1i on GPIO0 (old code 56 → new 352)
                var led1 = config.Leds.FirstOrDefault(l => l.GPIO == 0);
                led1.Should().NotBeNull();
                led1.Number.Should().Be(0);
                led1.Inverted.Should().BeTrue();
                
                // LedLinki on GPIO2 (old code 158 → new 356)
                var ledLink = config.Leds.FirstOrDefault(l => l.GPIO == 2);
                ledLink.Should().NotBeNull();
                ledLink.Number.Should().Be(0);
                ledLink.Inverted.Should().BeTrue();
            }
        }

        [Fact]
        public void ParseTemplate_ShouldConfigurePowerMonitoring()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();
            var gpioFunctions = configDict["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // BL0937 CF on GPIO5 (old code 134 → new 2688)
                gpioFunctions.Should().ContainKey(5);
                gpioFunctions[5].Should().Be("CF", "GPIO5 is CF pin for power monitoring");
                
                // HLWBL CF1 on GPIO4 (old code 132 → new 2624)
                gpioFunctions.Should().ContainKey(4);
                gpioFunctions[4].Should().Be("CF1", "GPIO4 is CF1 pin for power monitoring");
                
                // HLWBL SELi on GPIO12 (old code 131 → new 2720)
                gpioFunctions.Should().ContainKey(12);
                gpioFunctions[12].Should().Be("SEL", "GPIO12 is SEL pin for power monitoring");
            }
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
        public void ParseTemplate_ShouldHaveNoDS18B20Sensors()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            config.MaxDS18B20.Should().Be(0);
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
                configDict["HostName"].Should().Be("Gosund SP111 v1.1");
                configDict["MaxRelays"].Should().Be(1);
                configDict["MaxButtons"].Should().Be(1);
                configDict["MaxLeds"].Should().Be(1);
                configDict["MaxRGBW"].Should().Be(0);
                configDict["MaxLimitSwitches"].Should().Be(0);
                configDict["MaxConditions"].Should().Be(0);
                configDict["ConfigMode"].Should().Be("CONFIG_MODE_10_ON_PRESSES");
            }
        }

        [Fact]
        public void ParseTemplate_ShouldValidateAllMaxCounters()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.MaxRelays.Should().Be(1, "one relay is configured");
                config.MaxButtons.Should().Be(1, "one button is configured");
                config.MaxLeds.Should().Be(1, "LEDs are configured");
                config.MaxRGBW.Should().Be(0, "no RGBW channels are configured");
                config.MaxLimitSwitches.Should().Be(0, "no limit switches are configured");
                config.MaxConditions.Should().Be(0, "no conditions are configured");
                config.MaxDS18B20.Should().Be(0, "no DS18B20 sensors are configured");
            }
        }

        [Fact]
        public void ParseTemplate_UnusedGPIOsShouldBeOff()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.GetGPIO(1).Should().Be("OFF", "GPIO1 is unused");
                config.GetGPIO(3).Should().Be("OFF", "GPIO3 is unused");
                config.GetGPIO(9).Should().Be("OFF", "GPIO9 is unused");
                config.GetGPIO(10).Should().Be("OFF", "GPIO10 is unused");
                config.GetGPIO(14).Should().Be("OFF", "GPIO14 is unused");
                config.GetGPIO(16).Should().Be("OFF", "GPIO16 is unused");
            }
        }

        [Fact]
        public void ParseTemplate_ButtonShouldAlsoBeConfigButton()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();
            var gpioFunctions = configDict["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Button1 also acts as config button
                gpioFunctions.Should().ContainKey(13);
                gpioFunctions[13].Should().Be("BUTTON_CFG");
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
        public void ParseTemplate_PowerMonitoringPins_ShouldBeCorrectlyMapped()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Verify power monitoring GPIO assignments after conversion
                config.GetGPIO(4).Should().Be("CF1", "GPIO4 has CF1 function (old code 132)");
                config.GetGPIO(5).Should().Be("CF", "GPIO5 has CF function (old code 134)");
                config.GetGPIO(12).Should().Be("SEL", "GPIO12 has SEL function (old code 131)");
            }
        }

        [Fact]
        public void ParseTemplate_LEDs_ShouldBothBeInverted()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                config.Leds.Should().HaveCount(2);
                config.Leds.Should().OnlyContain(led => led.Inverted == true, "both LEDs are inverted");
            }
        }

        [Fact]
        public void ParseTemplate_ShouldBeSmartPlugConfiguration()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);
            var configDict = config.ToConfigDictionary();
            var gpioFunctions = configDict["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Smart plug typically has: relay, button, LEDs, and power monitoring
                config.MaxRelays.Should().Be(1, "smart plug has one relay");
                config.MaxButtons.Should().Be(1, "smart plug has one button");
                config.Leds.Should().NotBeEmpty("smart plug has status LEDs");
                
                // Power monitoring capability
                gpioFunctions.Should().ContainKey(5, "has CF pin for power measurement");
                gpioFunctions.Should().ContainKey(4, "has CF1 pin for current/voltage measurement");
                gpioFunctions.Should().ContainKey(12, "has SEL pin for mode selection");
            }
        }

        [Fact]
        public void ParseTemplate_OldFormatConversion_ShouldWork()
        {
            var parser = new TemplateBoardParser();

            var config = parser.ParseTemplate(TestJsonTemplate);

            using (new FluentAssertions.Execution.AssertionScope())
            {
                // Verify old format codes were successfully converted
                // Old code 21 → New 224 (Relay1)
                config.Relays.Should().ContainSingle(r => r.GPIO == 15);
                
                // Old code 17 → New 192 (Button1)
                config.Buttons.Should().ContainSingle(b => b.GPIO == 13);
                
                // Old code 56 → New 352 (Led1i)
                // Old code 158 → New 356 (LedLinki)
                config.Leds.Should().HaveCount(2);
                config.Leds.Should().Contain(l => l.GPIO == 0 && l.Inverted);
                config.Leds.Should().Contain(l => l.GPIO == 2 && l.Inverted);
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
                    "AnalogButtons", "Expanders", "GPIOFunctions" 
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
                gpioFunctions.Should().HaveCountGreaterThan(0);
                
                // Verify key GPIO assignments match expected configuration
                gpioFunctions.Should().ContainKey(4, "CF1 pin");
                gpioFunctions.Should().ContainKey(5, "CF pin");
                gpioFunctions.Should().ContainKey(12, "SEL pin");
                gpioFunctions.Should().ContainKey(13, "Button/Config");
            }
        }

        [Fact]
        public void ParseTemplate_OldAndNewFormat_ShouldProduceSameConfiguration()
        {
            // Old format: [56,0,158,0,132,134,0,0,131,17,0,21,0] (13 elements)
            // New format: [320,0,576,0,2656,2720,0,0,2624,32,0,224,0,0] (14 elements)
            // Note: This test validates that WHEN both templates represent the same device,
            // they should produce equivalent configurations after parsing
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
                configOld.HostName.Should().Be(configNew.HostName, "device names should match");

                // If both configs have relays, they should match
                if (configOld.MaxRelays > 0 || configNew.MaxRelays > 0)
                {
                    configOld.MaxRelays.Should().Be(configNew.MaxRelays, "relay counts should match");
                    
                    if (configOld.Relays.Any() && configNew.Relays.Any())
                    {
                        configOld.Relays.Should().HaveCount(configNew.Relays.Count, "number of relay configurations should match");
                        
                        // Compare relay GPIO pins
                        var relayGpiosOld = configOld.Relays.Select(r => r.GPIO).OrderBy(g => g).ToList();
                        var relayGpiosNew = configNew.Relays.Select(r => r.GPIO).OrderBy(g => g).ToList();
                        relayGpiosOld.Should().Equal(relayGpiosNew, "relay GPIO pins should match");
                        
                        // Verify inverted flags match for each relay GPIO
                        foreach (var gpio in relayGpiosOld)
                        {
                            var relayOld = configOld.Relays.First(r => r.GPIO == gpio);
                            var relayNew = configNew.Relays.First(r => r.GPIO == gpio);
                            relayOld.Inverted.Should().Be(relayNew.Inverted, $"Relay on GPIO{gpio} inverted flag should match");
                        }
                    }
                }

                // If both configs have buttons, they should match
                if (configOld.MaxButtons > 0 || configNew.MaxButtons > 0)
                {
                    configOld.MaxButtons.Should().Be(configNew.MaxButtons, "button counts should match");
                    
                    if (configOld.Buttons.Any() && configNew.Buttons.Any())
                    {
                        configOld.Buttons.Should().HaveCount(configNew.Buttons.Count, "number of button configurations should match");
                        
                        // Compare button GPIO pins
                        var buttonGpiosOld = configOld.Buttons.Select(b => b.GPIO).OrderBy(g => g).ToList();
                        var buttonGpiosNew = configNew.Buttons.Select(b => b.GPIO).OrderBy(g => g).ToList();
                        buttonGpiosOld.Should().Equal(buttonGpiosNew, "button GPIO pins should match");
                        
                        // Verify properties match for each button GPIO
                        foreach (var gpio in buttonGpiosOld)
                        {
                            var buttonOld = configOld.Buttons.First(b => b.GPIO == gpio);
                            var buttonNew = configNew.Buttons.First(b => b.GPIO == gpio);
                            buttonOld.EventType.Should().Be(buttonNew.EventType, $"Button on GPIO{gpio} event type should match");
                            buttonOld.Pullup.Should().Be(buttonNew.Pullup, $"Button on GPIO{gpio} pullup setting should match");
                            buttonOld.Inverted.Should().Be(buttonNew.Inverted, $"Button on GPIO{gpio} inverted flag should match");
                        }
                    }
                }

                // If both configs have LEDs, they should match
                if (configOld.MaxLeds > 0 || configNew.MaxLeds > 0)
                {
                    configOld.MaxLeds.Should().Be(configNew.MaxLeds, "LED counts should match");
                    
                    if (configOld.Leds.Any() && configNew.Leds.Any())
                    {
                        configOld.Leds.Should().HaveCount(configNew.Leds.Count, "number of LED configurations should match");
                        
                        // Compare LED GPIO pins
                        var ledGpiosOld = configOld.Leds.Select(l => l.GPIO).OrderBy(g => g).ToList();
                        var ledGpiosNew = configNew.Leds.Select(l => l.GPIO).OrderBy(g => g).ToList();
                        ledGpiosOld.Should().Equal(ledGpiosNew, "LED GPIO pins should match");

                        // Verify inverted flags match for each LED GPIO
                        foreach (var gpio in ledGpiosOld)
                        {
                            var ledOld = configOld.Leds.First(l => l.GPIO == gpio);
                            var ledNew = configNew.Leds.First(l => l.GPIO == gpio);
                            ledOld.Inverted.Should().Be(ledNew.Inverted, $"LED on GPIO{gpio} inverted flag should match");
                        }
                    }
                }

                // Compare other component counts
                configOld.MaxRGBW.Should().Be(configNew.MaxRGBW, "RGBW counts should match");
                configOld.MaxLimitSwitches.Should().Be(configNew.MaxLimitSwitches, "limit switch counts should match");
                configOld.MaxConditions.Should().Be(configNew.MaxConditions, "condition counts should match");
                configOld.MaxDS18B20.Should().Be(configNew.MaxDS18B20, "DS18B20 counts should match");

                // Compare GPIO functions that are present in both configs
                var configDictOld = configOld.ToConfigDictionary();
                var configDictNew = configNew.ToConfigDictionary();
                
                var gpioFunctionsOld = configDictOld["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;
                var gpioFunctionsNew = configDictNew["GPIOFunctions"] as System.Collections.Generic.Dictionary<int, string>;

                if (gpioFunctionsOld != null && gpioFunctionsNew != null)
                {
                    gpioFunctionsOld.Should().HaveCount(gpioFunctionsNew.Count, "both formats should configure same number of GPIOs");
                    
                    // Verify GPIO functions that exist in old config also exist in new config with same function
                    foreach (var gpio in gpioFunctionsOld.Keys)
                    {
                        gpioFunctionsNew.Should().ContainKey(gpio, $"new format should have GPIO{gpio} configured");
                        gpioFunctionsOld[gpio].Should().Be(gpioFunctionsNew[gpio], $"GPIO{gpio} function should match between old and new format");
                    }
                }

                // Both should have same config mode
                configOld.ConfigMode.Should().Be(configNew.ConfigMode, "config modes should match");
            }
        }
    }
}
