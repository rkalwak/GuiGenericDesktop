using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CompilationLib.Tests
{
    public class PlatformioCliHandlerUnitTests
    {
        private string _tempIniPath = "initestfile.ini";

        private static void AssertOnlyAllowedFlagsRemain(string result, params string[] allowedFlags)
        {
            foreach (var allowedFlag in allowedFlags)
            {
                result.Should().Contain($" -D {allowedFlag}");
            }

            result.Should().NotContain("Parameter_");
            result.Should().NotContain("GLOBALPARAMETERS_");
        }

        private static BuildFlagItem CreateCc1101BuildFlagItem()
        {
            var parameters = new List<Parameter>
            {
                new Parameter { Key = "MISO", Value = "12", Type = "number" },
                new Parameter { Key = "MOSI", Value = "13", Type = "number" },
                new Parameter { Key = "CLK", Value = "14", Type = "number" },
                new Parameter { Key = "CS", Value = "15", Type = "number" },
                new Parameter { Key = "GDO0", Value = "16", Type = "number" },
                new Parameter { Key = "GDO2", Value = "17", Type = "number" }
            };

            for (var i = 1; i <= 10; i++)
            {
                parameters.Add(new Parameter { Key = $"Enabled{i}", Value = i == 1 ? "1" : "0", Type = "enum" });
                parameters.Add(new Parameter { Key = $"SensorType{i}", Value = ((i % 25)).ToString(), Type = "enum" });
                parameters.Add(new Parameter { Key = $"SensorID{i}", Value = $"meter-{i}", Type = "string" });
                parameters.Add(new Parameter { Key = $"SensorKey{i}", Value = $"key-{i}", Type = "string" });
                parameters.Add(new Parameter { Key = $"SensorProperty{i}", Value = ((i - 1) % 3).ToString(), Type = "enum" });
                parameters.Add(new Parameter { Key = $"SensorChannel{i}", Value = ((i - 1) % 3).ToString(), Type = "enum" });
            }

            return new BuildFlagItem
            {
                Key = "SUPLA_CC1101",
                Parameters = parameters
            };
        }

        [Fact]
        public void CommentUnlistedFlagsBetweenMarkers_DisablesEveryOtherBuildFlagAndParameter_WhenOnlyOneFlagIsAllowed()
        {
            var iniPath = Path.Combine(Path.GetTempPath(), $"platformio-{Guid.NewGuid():N}.ini");

            var initialLines = new[]
            {
            ";flagsstart",
            " -D SUPLA_AHTX0",
            " -D SUPLA_RELAY",
            " -D Parameter_SUPLA_AHTX0_SDA=22",
            " -D Parameter_SUPLA_RELAY_SDA=27",
            ";flagsend"
        };

            File.WriteAllText(iniPath, string.Join(Environment.NewLine, initialLines) + Environment.NewLine);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowedFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_AHTX0",
                    Parameters = new List<Parameter>
                    {
                        new Parameter
                        {
                            Key = "SDA",
                            Type = "number",
                            Value = "22",
                            IsRequired = true
                        }
                    }
                }
            };

                handler.CommentUnlistedFlagsBetweenMarkers(iniPath, allowedFlags, new GlobalSettings());

                var lines = File.ReadAllLines(iniPath);
                using (new AssertionScope())
                {
                    lines[0].Should().Be(";flagsstart");
                    lines[1].Should().Be(" -D SUPLA_AHTX0");
                    lines[2].Should().StartWith(";").And.Contain("SUPLA_RELAY");
                    lines[3].Should().Be(" -D Parameter_SUPLA_AHTX0_SDA=22");
                    lines[4].Should().StartWith(";").And.Contain("Parameter_SUPLA_RELAY_SDA");

                    lines.Count(line => line.Contains("SUPLA_AHTX0", StringComparison.OrdinalIgnoreCase) && !line.TrimStart().StartsWith(";")).Should().Be(2);
                    lines.Count(line => line.Contains("SUPLA_RELAY", StringComparison.OrdinalIgnoreCase) && !line.TrimStart().StartsWith(";")).Should().Be(0);
                    lines.Count(line => line.Contains("Parameter_SUPLA_RELAY_SDA", StringComparison.OrdinalIgnoreCase) && !line.TrimStart().StartsWith(";")).Should().Be(0);
                }
            }
            finally
            {
                File.Delete(iniPath);
            }
        }

        [Fact]
        public void CommentUnlistedFlags_CC1101_OnlyEnabledSensorParametersRemainActive()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_CC1101
 -D Parameter_SUPLA_CC1101_MISO=12
 -D Parameter_SUPLA_CC1101_MOSI=13
 -D Parameter_SUPLA_CC1101_Enabled1=1
 -D Parameter_SUPLA_CC1101_SensorType1=0
 -D Parameter_SUPLA_CC1101_SensorID1=meter-1
 -D Parameter_SUPLA_CC1101_SensorKey1=key-1
 -D Parameter_SUPLA_CC1101_SensorProperty1=0
 -D Parameter_SUPLA_CC1101_SensorChannel1=0
 -D Parameter_SUPLA_CC1101_Enabled2=1
 -D Parameter_SUPLA_CC1101_SensorType2=1
 -D Parameter_SUPLA_CC1101_SensorID2=meter-2
 -D Parameter_SUPLA_CC1101_SensorKey2=key-2
 -D Parameter_SUPLA_CC1101_SensorProperty2=1
 -D Parameter_SUPLA_CC1101_SensorChannel2=1
 -D Parameter_SUPLA_CC1101_Enabled3=0
 -D Parameter_SUPLA_CC1101_SensorType3=2
 -D Parameter_SUPLA_CC1101_SensorID3=meter-3
 -D Parameter_SUPLA_CC1101_SensorKey3=key-3
 -D Parameter_SUPLA_CC1101_SensorProperty3=2
 -D Parameter_SUPLA_CC1101_SensorChannel3=2
 -D Parameter_SUPLA_CC1101_Enabled10=0
 -D Parameter_SUPLA_CC1101_SensorType10=9
 -D Parameter_SUPLA_CC1101_SensorID10=meter-10
 -D Parameter_SUPLA_CC1101_SensorKey10=key-10
 -D Parameter_SUPLA_CC1101_SensorProperty10=0
 -D Parameter_SUPLA_CC1101_SensorChannel10=0
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_CC1101",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "MISO", Value = "12", Type = "number" },
                            new Parameter { Key = "MOSI", Value = "13", Type = "number" },
                            new Parameter { Key = "Enabled1", Value = "1", Type = "enum" },
                            new Parameter { Key = "SensorType1", Value = "0", Type = "enum" },
                            new Parameter { Key = "SensorID1", Value = "meter-1", Type = "string" },
                            new Parameter { Key = "SensorKey1", Value = "key-1", Type = "string" },
                            new Parameter { Key = "SensorProperty1", Value = "0", Type = "enum" },
                            new Parameter { Key = "SensorChannel1", Value = "0", Type = "enum" },
                            new Parameter { Key = "Enabled2", Value = "1", Type = "enum" },
                            new Parameter { Key = "SensorType2", Value = "1", Type = "enum" },
                            new Parameter { Key = "SensorID2", Value = "meter-2", Type = "string" },
                            new Parameter { Key = "SensorKey2", Value = "key-2", Type = "string" },
                            new Parameter { Key = "SensorProperty2", Value = "1", Type = "enum" },
                            new Parameter { Key = "SensorChannel2", Value = "1", Type = "enum" },
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D Parameter_SUPLA_CC1101_Enabled1=1");
                    result.Should().Contain(" -D Parameter_SUPLA_CC1101_SensorProperty1=0");
                    result.Should().Contain(" -D Parameter_SUPLA_CC1101_Enabled2=1");
                    result.Should().Contain(" -D Parameter_SUPLA_CC1101_SensorProperty2=1");
                    result.Should().Contain("; -D Parameter_SUPLA_CC1101_Enabled3=0");
                    result.Should().Contain("; -D Parameter_SUPLA_CC1101_SensorProperty3=2");
                    result.Should().Contain("; -D Parameter_SUPLA_CC1101_Enabled10=0");
                    result.Should().Contain("; -D Parameter_SUPLA_CC1101_SensorProperty10=0");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_CC1101_CommentedAllowedFlagsAreUncommented_AndUncommentedUnlistedFlagsAreCommented()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_CC1101
 ; -D SUPLA_CC1101
 -D Parameter_SUPLA_CC1101_MISO=12
 ; -D Parameter_SUPLA_CC1101_GDO2=17
 -D SUPLA_ENABLE_GUI
 ; -D SUPLA_ENABLE_SSL
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_CC1101",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "MISO", Value = "12", Type = "number" },
                            new Parameter { Key = "GDO2", Value = "17", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_CC1101");
                    result.Should().NotContain("; -D SUPLA_CC1101");
                    result.Should().Contain("; -D SUPLA_ENABLE_GUI");
                    result.Should().Contain(" ; -D SUPLA_ENABLE_SSL");
                    result.Should().Contain(" -D Parameter_SUPLA_CC1101_MISO=12");
                    result.Should().Contain(" -D Parameter_SUPLA_CC1101_GDO2=17");
                    result.Should().NotContain(" ; -D Parameter_SUPLA_CC1101_GDO2=17");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void EnabledFlagShouldStayEnabled()
        {

            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_TEST
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_TEST" },
                    /*
                    new BuildFlagItem { Key = "TEMPLATE_BOARD_JSON"},
                    new BuildFlagItem
                    {
                        Key = "SUPLA_MS5611",
                        Parameters=new List<Parameter>
                        {
                            new Parameter{ Key = "Altitude", Name = "Wysokość n.p.m." , Value= "253.3" , Type="number"}
                        }
                    }
                    */
                };

                // Don't use global settings for this test to avoid affecting line positions
                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllLines(temp);

                // Find indices
                var start = Array.IndexOf(result, ";flagsstart");
                var end = Array.IndexOf(result, ";flagsend");

                using (new AssertionScope())
                {
                    var line1 = result[start + 1];
                    line1.Should().Be(" -D SUPLA_TEST");

                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void DisabledFlagShouldBeEnabled()
        {

            var iniContent = @"[env:test]
;flagsstart
; -D SUPLA_TEST
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_TEST" },
                };

                // Don't use global settings for this test to avoid affecting line positions
                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllLines(temp);

                // Find indices
                var start = Array.IndexOf(result, ";flagsstart");
                var end = Array.IndexOf(result, ";flagsend");

                using (new AssertionScope())
                {
                    var line1 = result[start + 1];
                    line1.Should().Be(" -D SUPLA_TEST");

                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void DisabledFlagShouldStayDisabled()
        {

            var iniContent = @"[env:test]
;flagsstart
; -D SUPLA_TEST
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                };

                // Don't use global settings for this test to avoid affecting line positions
                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllLines(temp);

                // Find indices
                var start = Array.IndexOf(result, ";flagsstart");
                var end = Array.IndexOf(result, ";flagsend");

                using (new AssertionScope())
                {
                    var line1 = result[start + 1];
                    line1.Should().Be("; -D SUPLA_TEST");

                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }


        [Fact]
        public void CommentUnlistedFlags_PreservesUnmanagedParameterEntries()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_LIMIT_SWITCH
 -D Parameter_SUPLA_LIMIT_SWITCH_GPIO1=12
 -D Parameter_SUPLA_LIMIT_SWITCH_GPIO1Pullup=1
 -D Parameter_SUPLA_LIMIT_SWITCH_GPIO15=15
 -D Parameter_SUPLA_LIMIT_SWITCH_GPIO15Pullup=0
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_LIMIT_SWITCH",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "Count", Value = "1", Type = "number" },
                            new Parameter { Key = "GPIO1", Value = "12", Type = "number" },
                            new Parameter { Key = "GPIO1Pullup", Value = "1", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D Parameter_SUPLA_LIMIT_SWITCH_GPIO1=12");
                    result.Should().Contain(" -D Parameter_SUPLA_LIMIT_SWITCH_GPIO1Pullup=1");
                    result.Should().Contain("; -D Parameter_SUPLA_LIMIT_SWITCH_GPIO15=15");
                    result.Should().Contain("; -D Parameter_SUPLA_LIMIT_SWITCH_GPIO15Pullup=0");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_CommentsOutUnlistedNonParameterFlags()

        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_CONFIG
 -D TEMPLATE_BOARD_JSON
 -D SUPLA_LIMIT_SWITCH
 -D SUPLA_ENABLE_GUI
 -D SUPLA_ENABLE_SSL
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_CONFIG" },
                    new BuildFlagItem { Key = "TEMPLATE_BOARD_JSON"},
                    new BuildFlagItem
                    {
                        Key = "SUPLA_MS5611",
                        Parameters=new List<Parameter>
                        {
                            new Parameter{ Key = "Altitude", Name = "Wysoko�� n.p.m." , Value= "253.3" , Type="number"}
                        }
                    }
                };

                // Don't use global settings for this test to avoid affecting line positions
                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllLines(temp);

                // Find indices
                var start = Array.IndexOf(result, ";flagsstart");
                var end = Array.IndexOf(result, ";flagsend");

                using (new AssertionScope())
                {
                    var line1 = result[start + 1];
                    line1.Should().StartWith(" ").And.Contain("SUPLA_CONFIG");
                    var line2 = result[start + 2];
                    line2.Should().StartWith(" ").And.Contain("TEMPLATE_BOARD_JSON");
                    var line5 = result[start + 4];
                    line5.Should().StartWith(";").And.Contain("SUPLA_ENABLE_GUI");

                    var line6 = result[start + 5];
                    line6.Should().StartWith(";").And.Contain("SUPLA_ENABLE_SSL");

                    var lineBeforeLast = result[end - 1];
                    lineBeforeLast.Should().StartWith(";").And.Contain("SUPLA_ENABLE_SSL");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }


        [Fact]
        public void CommentUnlistedFlags_NoMarkers_DoesNothing()
        {
            var iniContent = string.Join("\n", new[] {
                "[env:whatever]",
                "-D SUPLA_X",
                ""
            });

            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var original = File.ReadAllText(temp);

                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem { FlagName = "SUPLA_X" }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var after = File.ReadAllText(temp);

                after.Should().Be(original);
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }
       
        [Fact]
        public void CommentUnlistedFlags_ReusesExistingParameterLineWithinFlagBlock()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_SENSOR
 -D Parameter_SUPLA_SENSOR_Temperature=21
 -D Parameter_SUPLA_SENSOR_TemperatureEnabled=1
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_SENSOR",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "Temperature", Value = "21", Type = "number" },
                            new Parameter { Key = "TemperatureEnabled", Value = "1", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);
                var start = result.IndexOf(";flagsstart", StringComparison.OrdinalIgnoreCase);
                var end = result.IndexOf(";flagsend", StringComparison.OrdinalIgnoreCase);

                using (new AssertionScope())
                {
                    result.IndexOf("Parameter_SUPLA_SENSOR_Temperature=21", StringComparison.OrdinalIgnoreCase)
                        .Should().BeGreaterThan(start).And.BeLessThan(end);
                    result.IndexOf("Parameter_SUPLA_SENSOR_TemperatureEnabled=1", StringComparison.OrdinalIgnoreCase)
                        .Should().BeGreaterThan(start).And.BeLessThan(end);
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }


        [Fact]
        public async Task DisabledGlobalSettingsParametersShouldBeEnabled()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"buildcfg-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var iniContent = @"[env:test]
;flagsstart
; -D GLOBALPARAMETERS_SCL=0
;flagsend
";
            var iniPath = Path.Combine(tempDir, "platformio.ini");
            File.WriteAllText(iniPath, iniContent);
            try
            {
                var handler = new PlatformioCliHandler();
                var enabledFlags = new List<BuildFlagItem>
                {
                    
                };
                var globalSettings = new GlobalSettings
                {
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Value = "22", Type = "number", IsRequired = true },
                        new Parameter { Key = "SDA", Value = "21", Type = "number", IsRequired = true }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(iniPath, enabledFlags, globalSettings);

                var lines = File.ReadAllLines(iniPath).ToList();
                lines[2].Should().Be(" -D GLOBALPARAMETERS_SCL=22");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void CommentUnlistedFlags_EnumParameter_FormattedAsNumber()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_INITIAL_CONFIG_MODE
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_INITIAL_CONFIG_MODE",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "Mode", Name = "Tryb", Value = "2", Type = "enum" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_INITIAL_CONFIG_MODE");
                result.Should().NotContain("'\"2\"'");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_NumberParameter_FormattedAsNumber()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_MS5611
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_MS5611",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "Altitude", Name = "Wysoko�� n.p.m.", Value = "150", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_MS5611");
                result.Should().NotContain("'\"150\"'");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_TextParameter_FormattedWithQuotes()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_DEVICE
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_DEVICE",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "NAME", Name = "Device Name", Value = "MyDevice", Type = "text" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_DEVICE");
                result.Should().NotContain("\"MyDevice\"");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_EmptyNumberValue_DefaultsToZero()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "TIMEOUT", Name = "Timeout", Value = null, Type = "number", IsRequired = true }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_FLAG");
                result.Should().NotContain("Parameter_SUPLA_FLAG_TIMEOUT");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_UpdateExistingEnumParameter()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
 -D Parameter_SUPLA_FLAG_MODE=1
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "MODE", Name = "Mode", Value = "3", Type = "enum" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_FLAG");
                    result.Should().Contain(" -D Parameter_SUPLA_FLAG_MODE=3");
                    result.Should().NotContain(" -D Parameter_SUPLA_FLAG_MODE=1");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_MixedParameterTypes_AllFormattedCorrectly()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_COMPLEX_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_COMPLEX_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "MODE", Name = "Mode", Value = "2", Type = "enum" },
                            new Parameter { Key = "TIMEOUT", Name = "Timeout", Value = "500", Type = "number" },
                            new Parameter { Key = "NAME", Name = "Name", Value = "Device1", Type = "text" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_COMPLEX_FLAG");
                result.Should().NotContain("Parameter_SUPLA_COMPLEX_FLAG_");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GpioParameter_IsWrittenAsInteger()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_LED
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_LED",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "GPIO", Name = "GPIO", Value = "12", Type = "gpio" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_LED");
                result.Should().NotContain("Parameter_SUPLA_LED_GPIO='\"12\"'");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_EnumParameterUsesDefaultValueWhenValueIsEmpty()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_INITIAL_CONFIG_MODE
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_INITIAL_CONFIG_MODE",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "Mode",
                                Name = "Tryb",
                                Value = "",
                                DefaultValue = "3",
                                Type = "enum",
                                IsRequired = true  // Required parameters get default value of 0
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_INITIAL_CONFIG_MODE");
                result.Should().NotContain("Parameter_SUPLA_INITIAL_CONFIG_MODE_");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_EnumParameterWithDefaultValueSet()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_INITIAL_CONFIG_MODE
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_INITIAL_CONFIG_MODE",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "Mode",
                                Name = "Tryb",
                                Value = "3",  // Value initialized from DefaultValue
                                DefaultValue = "3",
                                Type = "enum"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_INITIAL_CONFIG_MODE");
                result.Should().NotContain("Parameter_SUPLA_INITIAL_CONFIG_MODE_");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_ParameterWithoutKey_UsesNameForBackwardCompatibility()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_TEST_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_TEST_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            { 
                                // No Key set - should fall back to Name
                                Name = "OldParamName",
                                Value = "123",
                                Type = "number"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_TEST_FLAG");
                result.Should().NotContain("Parameter_SUPLA_TEST_FLAG_");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_OptionalParameterWithoutValue_NotAdded()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "OptionalParam",
                                Name = "Optional Parameter",
                                Value = "",
                                IsRequired = false,
                                Type = "number"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_FLAG");
                    result.Should().NotContain("Parameter_SUPLA_FLAG_OptionalParam");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_OptionalParameterWithValue_IsAdded()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "OptionalParam",
                                Name = "Optional Parameter",
                                Value = "42",
                                IsRequired = false,
                                Type = "number"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    AssertOnlyAllowedFlagsRemain(result, "SUPLA_FLAG");
                    result.Should().NotContain("Parameter_SUPLA_FLAG_OptionalParam");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_RequiredParameterWithoutValue_IsAddedWithDefault()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "RequiredParam",
                                Name = "Required Parameter",
                                Value = "",
                                IsRequired = true,
                                Type = "number"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    AssertOnlyAllowedFlagsRemain(result, "SUPLA_FLAG");
                    result.Should().NotContain("Parameter_SUPLA_FLAG_RequiredParam");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_OptionalParameterExistsInFile_GetsCommentedOut()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
 -D Parameter_SUPLA_FLAG_OptionalParam=100
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "OptionalParam",
                                Name = "Optional Parameter",
                                Value = "",  // No value provided
                                IsRequired = false,
                                Type = "number"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_FLAG");
                    result.Should().Contain("; -D Parameter_SUPLA_FLAG_OptionalParam=100");
                    result.Should().NotContain("\n -D Parameter_SUPLA_FLAG_OptionalParam=");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_OptionalParameterAlreadyCommented_StaysCommented()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;-D Parameter_SUPLA_FLAG_OptionalParam=100
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "OptionalParam",
                                Name = "Optional Parameter",
                                Value = "",  // No value provided
                                IsRequired = false,
                                Type = "number"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_FLAG");
                    result.Should().Contain(";-D Parameter_SUPLA_FLAG_OptionalParam=100");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_MixedRequiredAndOptionalParameters()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_COMPLEX
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_COMPLEX",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "RequiredMode",
                                Value = "1",
                                IsRequired = true,
                                Type = "enum"
                            },
                            new Parameter
                            {
                                Key = "OptionalTimeout",
                                Value = "",  // No value
                                IsRequired = false,
                                Type = "number"
                            },
                            new Parameter
                            {
                                Key = "OptionalName",
                                Value = "MyName",  // Has value
                                IsRequired = false,
                                Type = "text"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    AssertOnlyAllowedFlagsRemain(result, "SUPLA_COMPLEX");
                    result.Should().NotContain("Parameter_SUPLA_COMPLEX_OptionalTimeout");
                    result.Should().NotContain("Parameter_SUPLA_COMPLEX_RequiredMode");
                    result.Should().NotContain("Parameter_SUPLA_COMPLEX_OptionalName");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_OptionalEnumWithoutValue_NotAdded()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "OptionalMode",
                                Value = null,
                                IsRequired = false,
                                Type = "enum"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_FLAG");
                result.Should().NotContain("Parameter_SUPLA_FLAG_OptionalMode");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_OptionalTextWithoutValue_NotAdded()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_FLAG
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_FLAG",
                        Parameters = new List<Parameter>
                        {
                            new Parameter
                            {
                                Key = "OptionalText",
                                Value = "",
                                IsRequired = false,
                                Type = "text"
                            }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                AssertOnlyAllowedFlagsRemain(result, "SUPLA_FLAG");
                result.Should().NotContain("Parameter_SUPLA_FLAG_OptionalText");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void DirectLinkWithoutParameter_DoesNotEnableTemperatureSensor()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_DIRECT_LINK
 -D SUPLA_DIRECT_LINK_TEMPERATURE_SENSOR
 -D SUPLA_DIRECT_LINK_TEMPERATURE_SENSOR
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_DIRECT_LINK"
                        // No parameters specified
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_DIRECT_LINK");
                    result.Should().Contain("; -D SUPLA_DIRECT_LINK_TEMPERATURE_SENSOR");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void GlobalParametersShouldBeNotWrittenWhenNoI2CFlagsPresent()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_RELAY
 -D SUPLA_BUTTON
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var globalSettings = new GlobalSettings
                {
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Value = "22", Type = "number" },
                        new Parameter { Key = "SDA", Value = "21", Type = "number" }
                    }
                };

                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_RELAY" },
                    new BuildFlagItem { Key = "SUPLA_BUTTON" }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    AssertOnlyAllowedFlagsRemain(result, "SUPLA_RELAY", "SUPLA_BUTTON");
                    result.Should().NotContain("GLOBALPARAMETERS_");
                    result.Should().NotContain("Parameter_SUPLA_RELAY_");
                    result.Should().NotContain("Parameter_SUPLA_BUTTON_");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void EnabledParameterCoveringGlobalParametersShouldEnableGlobalParametersButNotItself()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
 -D SUPLA_SHT3x
 -D Parameter_SUPLA_BME280_SCL=0
 -D Parameter_SUPLA_BME280_SDA=0
 -D GLOBALPARAMETERS_SCL=0
 -D GLOBALPARAMETERS_SDA=0
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var globalSettings = new GlobalSettings
                {
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Name = "I2C SCL Pin", DefaultValue = "22", Type = "number" },
                        new Parameter { Key = "SDA", Name = "I2C SDA Pin", DefaultValue = "21", Type = "number" }
                    }
                };

                var handler = new PlatformioCliHandler();
                var allowed = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_BME280",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    },
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var lines = File.ReadAllLines(temp);
                using (new AssertionScope())
                {
                    lines[2].Should().Be(" -D SUPLA_BME280");
                    lines[3].Should().Be("; -D SUPLA_SHT3x");
                    lines[4].Should().Be("; -D Parameter_SUPLA_BME280_SCL=0");
                    lines[5].Should().Be("; -D Parameter_SUPLA_BME280_SDA=0");
                    lines[6].Should().Be(" -D GLOBALPARAMETERS_SCL=22");
                    lines[7].Should().Be(" -D GLOBALPARAMETERS_SDA=21");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlagsBetweenMarkers_KeepsExcludedFlagsEnabled()
        {
            var iniPath = Path.Combine(Path.GetTempPath(), $"platformio-{Guid.NewGuid():N}.ini");

            var initialLines = new[]
            {
                ";flagsstart",
                " -D SUPLA_RELAY",
                " -D BUILD_VERSION=42",
                " -D OPTIONS_HASH=abc123",
                " -D SUPLA_EXCLUDE_LITTLEFS_CONFIG=1",
                ";flagsend"
            };

            File.WriteAllText(iniPath, string.Join(Environment.NewLine, initialLines) + Environment.NewLine);

            try
            {
                var handler = new PlatformioCliHandler();
                var allowedFlags = new List<BuildFlagItem>
                {
                    new BuildFlagItem { Key = "SUPLA_AHTX0" }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(iniPath, allowedFlags, new GlobalSettings());

                var lines = File.ReadAllLines(iniPath);
                using (new AssertionScope())
                {
                    lines.Should().Contain(line => line.Contains("BUILD_VERSION", StringComparison.OrdinalIgnoreCase) && !line.TrimStart().StartsWith(";"));
                    lines.Should().Contain(line => line.Contains("OPTIONS_HASH", StringComparison.OrdinalIgnoreCase) && !line.TrimStart().StartsWith(";"));
                    lines.Should().Contain(line => line.Contains("SUPLA_EXCLUDE_LITTLEFS_CONFIG", StringComparison.OrdinalIgnoreCase) && !line.TrimStart().StartsWith(";"));
                    lines.Should().Contain(line => line.Contains("SUPLA_RELAY", StringComparison.OrdinalIgnoreCase) && line.TrimStart().StartsWith(";"));
                }
            }
            finally
            {
                File.Delete(iniPath);
            }
        }
    }
}