using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace CompilationLib.Tests
{
    public class PlatformioCliHandlerUnitTests
    {
        private string _tempIniPath = "initestfile.ini";

        [Fact]
        public void CommentUnlistedFlags_BehavesAsExpected()
        {
            var iniContent = File.ReadAllText(_tempIniPath);
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
                            new Parameter{ Key = "Altitude", Name = "Wysokoœæ n.p.m." , Value= "253.3" , Type="number"}
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
                    var line2 = result[start + 2];
                    line2.Should().StartWith(" ").And.Contain("TEMPLATE_BOARD_JSON");
                    // Line for SUPLA_ENABLE_GUI should now be commented and contain the flag
                    var line5 = result[start + 5];
                    line5.Should().StartWith(";").And.Contain("SUPLA_ENABLE_GUI");

                    // Line for SUPLA_ENABLE_SSL should stay commented and contain the flag
                    var line6 = result[start + 6];
                    line6.Should().StartWith(";").And.Contain("SUPLA_ENABLE_SSL");

                    // Line for SUPLA_CONFIG should be enabled
                    var line7 = result[start + 7];
                    line7.Should().StartWith(" ").And.Contain("SUPLA_CONFIG");

                    var lineBeforeLast = result[end - 1];
                    lineBeforeLast.Should().StartWith(" ").And.Contain("SUPLA_MS5611_Altitude=253.3");
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

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_INITIAL_CONFIG_MODE_Mode=2");
                    result.Should().NotContain("'\"2\"'");
                }
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
                            new Parameter { Key = "Altitude", Name = "Wysokoœæ n.p.m.", Value = "150", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_MS5611_Altitude=150");
                    result.Should().NotContain("'\"150\"'");
                }
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

                result.Should().Contain(" -D SUPLA_DEVICE_NAME='\"MyDevice\"'");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_EmptyEnumValue_DefaultsToZero()
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
                            new Parameter { Key = "MODE", Name = "Mode", Value = "", Type = "enum", IsRequired = true }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                result.Should().Contain(" -D SUPLA_FLAG_MODE=0");
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

                result.Should().Contain(" -D SUPLA_FLAG_TIMEOUT=0");
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
 -D SUPLA_FLAG_MODE=1
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
                    result.Should().Contain(" -D SUPLA_FLAG_MODE=3");
                    result.Should().NotContain(" -D SUPLA_FLAG_MODE=1");
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

                using (new AssertionScope())
                {
                    result.Should().Contain(" -D SUPLA_COMPLEX_FLAG_MODE=2");
                    result.Should().Contain(" -D SUPLA_COMPLEX_FLAG_TIMEOUT=500");
                    result.Should().Contain(" -D SUPLA_COMPLEX_FLAG_NAME='\"Device1\"'");
                }
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

                // When Value is empty and IsRequired is true, it should use "0" not DefaultValue in the handler
                // The DefaultValue should be used to initialize Value in the UI layer
                result.Should().Contain(" -D SUPLA_INITIAL_CONFIG_MODE_Mode=0");
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

                result.Should().Contain(" -D SUPLA_INITIAL_CONFIG_MODE_Mode=3");
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

                result.Should().Contain(" -D SUPLA_TEST_FLAG_OldParamName=123");
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
                    result.Should().NotContain("SUPLA_FLAG_OptionalParam");
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
                    result.Should().Contain(" -D SUPLA_FLAG");
                    result.Should().Contain(" -D SUPLA_FLAG_OptionalParam=42");
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
                    result.Should().Contain(" -D SUPLA_FLAG");
                    result.Should().Contain(" -D SUPLA_FLAG_RequiredParam=0");
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
 -D SUPLA_FLAG_OptionalParam=100
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
                    result.Should().Contain(";-D SUPLA_FLAG_OptionalParam=100");
                    result.Should().NotContain("\n -D SUPLA_FLAG_OptionalParam=");
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
;-D SUPLA_FLAG_OptionalParam=100
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
                    result.Should().Contain(";-D SUPLA_FLAG_OptionalParam=100");
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
                    result.Should().Contain(" -D SUPLA_COMPLEX_RequiredMode=1");
                    result.Should().NotContain("SUPLA_COMPLEX_OptionalTimeout");
                    result.Should().Contain(" -D SUPLA_COMPLEX_OptionalName='\"MyName\"'");
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

                result.Should().NotContain("SUPLA_FLAG_OptionalMode");
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

                result.Should().NotContain("SUPLA_FLAG_OptionalText");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_DirectLinkWithoutParameter_DoesNotEnableTemperatureSensor()
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
                    result.Should().Contain(";-D SUPLA_DIRECT_LINK_TEMPERATURE_SENSOR");
                    result.Should().NotContain("\n -D SUPLA_DIRECT_LINK_TEMPERATURE_SENSOR");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GlobalParameters_HaveCorrectNamingFormat()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
 -D SUPLA_SHT3x
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
                        new Parameter { Key = "SCL", Name = "I2C SCL Pin", Value = "22", Type = "number" },
                        new Parameter { Key = "SDA", Name = "I2C SDA Pin", Value = "21", Type = "number" }
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
                    new BuildFlagItem
                    {
                        Key = "SUPLA_SHT3x",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // Global parameters SHOULD be written with GlobalParameter_ prefix when I2C flags are present
                    result.Should().Contain(" -D GlobalParameter_SCL=22", "global SCL parameter should use GlobalParameter_ prefix when I2C flags are present");
                    result.Should().Contain(" -D GlobalParameter_SDA=21", "global SDA parameter should use GlobalParameter_ prefix when I2C flags are present");

                    // Individual flag parameters should NOT be written (deduplicated)
                    result.Should().NotContain("SUPLA_BME280_SCL", "SCL is a global parameter and should not be duplicated per flag");
                    result.Should().NotContain("SUPLA_BME280_SDA", "SDA is a global parameter and should not be duplicated per flag");
                    result.Should().NotContain("SUPLA_SHT3x_SCL", "SCL is a global parameter and should not be duplicated per flag");
                    result.Should().NotContain("SUPLA_SHT3x_SDA", "SDA is a global parameter and should not be duplicated per flag");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GlobalParameters_NotWrittenWhenNoI2CFlagsPresent()
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
                    // Global parameters should still be written (they're in globalSettings)
                    result.Should().Contain(" -D GlobalParameter_SCL=22", "global parameters should be written when defined in globalSettings");
                    result.Should().Contain(" -D GlobalParameter_SDA=21", "global parameters should be written when defined in globalSettings");

                    // Non-I2C flags should not have SCL/SDA parameters
                    result.Should().NotContain("SUPLA_RELAY_SCL");
                    result.Should().NotContain("SUPLA_RELAY_SDA");
                    result.Should().NotContain("SUPLA_BUTTON_SCL");
                    result.Should().NotContain("SUPLA_BUTTON_SDA");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GlobalParameters_OnlyI2CDevicesUseThem()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
 -D SUPLA_RELAY
 -D SUPLA_SHT3x
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
                    new BuildFlagItem
                    {
                        Key = "SUPLA_BME280",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    },
                    new BuildFlagItem { Key = "SUPLA_RELAY" },
                    new BuildFlagItem
                    {
                        Key = "SUPLA_SHT3x",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // Global parameters written once
                    result.Should().Contain(" -D GlobalParameter_SCL=22");
                    result.Should().Contain(" -D GlobalParameter_SDA=21");

                    // I2C devices should NOT have individual SCL/SDA (they use global)
                    result.Should().NotContain("SUPLA_BME280_SCL");
                    result.Should().NotContain("SUPLA_BME280_SDA");
                    result.Should().NotContain("SUPLA_SHT3x_SCL");
                    result.Should().NotContain("SUPLA_SHT3x_SDA");

                    // Non-I2C device should NOT have SCL/SDA parameters at all
                    result.Should().NotContain("SUPLA_RELAY_SCL");
                    result.Should().NotContain("SUPLA_RELAY_SDA");

                    // Verify only one occurrence of each global parameter
                    var sclCount = System.Text.RegularExpressions.Regex.Matches(result, @"-D GlobalParameter_SCL=").Count;
                    var sdaCount = System.Text.RegularExpressions.Regex.Matches(result, @"-D GlobalParameter_SDA=").Count;
                    sclCount.Should().Be(1);
                    sdaCount.Should().Be(1);
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_NoGlobalSettings_I2CDevicesWriteOwnParameters()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
 -D SUPLA_SHT3x
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
                        Key = "SUPLA_BME280",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    },
                    new BuildFlagItem
                    {
                        Key = "SUPLA_SHT3x",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    }
                };

                // No global settings provided
                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, null);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // Without global settings, each I2C device should write its own parameters
                    result.Should().Contain(" -D SUPLA_BME280_SCL=22", "without global settings, flags should write own SCL parameter");
                    result.Should().Contain(" -D SUPLA_BME280_SDA=21", "without global settings, flags should write own SDA parameter");
                    result.Should().Contain(" -D SUPLA_SHT3x_SCL=22", "without global settings, flags should write own SCL parameter");
                    result.Should().Contain(" -D SUPLA_SHT3x_SDA=21", "without global settings, flags should write own SDA parameter");

                    // Global parameters should NOT be written
                    result.Should().NotContain("GlobalParameter_SCL", "no global parameters without global settings");
                    result.Should().NotContain("GlobalParameter_SDA", "no global parameters without global settings");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_EmptyGlobalSettings_I2CDevicesWriteOwnParameters()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
;flagsend
";
            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, iniContent);

            try
            {
                var emptyGlobalSettings = new GlobalSettings
                {
                    Parameters = new List<Parameter> () // Empty list
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
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, emptyGlobalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // With empty global settings, flag should write its own parameters
                    result.Should().Contain(" -D SUPLA_BME280_SCL=22", "with empty global settings, flag should write own parameters");
                    result.Should().Contain(" -D SUPLA_BME280_SDA=21", "with empty global settings, flag should write own parameters");

                    // No global parameters should be written
                    result.Should().NotContain("GlobalParameter_", "no global parameters with empty settings");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GlobalParameters_UseValueFromBuildFlags()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
 -D SUPLA_SHT3x
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
                        new Parameter { Key = "SCL", Name = "I2C SCL Pin", DefaultValue = "99", Type = "number" },
                        new Parameter { Key = "SDA", Name = "I2C SDA Pin", DefaultValue = "88", Type = "number" }
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
                    new BuildFlagItem
                    {
                        Key = "SUPLA_SHT3x",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // Global parameters should use values from BuildFlags (22, 21), not DefaultValue (99, 88)
                    result.Should().Contain(" -D GlobalParameter_SCL=22", "should use value from BuildFlag, not DefaultValue");
                    result.Should().Contain(" -D GlobalParameter_SDA=21", "should use value from BuildFlag, not DefaultValue");
                    
                    // Should not use the default values
                    result.Should().NotContain("GlobalParameter_SCL=99", "should not use DefaultValue from GlobalSettings");
                    result.Should().NotContain("GlobalParameter_SDA=88", "should not use DefaultValue from GlobalSettings");

                    // Individual flag parameters should NOT be written
                    result.Should().NotContain("SUPLA_BME280_SCL");
                    result.Should().NotContain("SUPLA_BME280_SDA");
                    result.Should().NotContain("SUPLA_SHT3x_SCL");
                    result.Should().NotContain("SUPLA_SHT3x_SDA");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GlobalParameters_FallbackToGlobalSettingsWhenNoBuildFlagValue()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_RELAY
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
                    new BuildFlagItem { Key = "SUPLA_RELAY" } // No SCL/SDA parameters
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // Global parameters should use values from GlobalSettings since no BuildFlag provides them
                    result.Should().Contain(" -D GlobalParameter_SCL=22", "should fallback to GlobalSettings.Value");
                    result.Should().Contain(" -D GlobalParameter_SDA=21", "should fallback to GlobalSettings.Value");

                    // Non-I2C flag should not have SCL/SDA
                    result.Should().NotContain("SUPLA_RELAY_SCL");
                    result.Should().NotContain("SUPLA_RELAY_SDA");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }

        [Fact]
        public void CommentUnlistedFlags_GlobalParameters_UseFirstMatchingBuildFlagValue()
        {
            var iniContent = @"[env:test]
;flagsstart
 -D SUPLA_BME280
 -D SUPLA_SHT3x
 -D SUPLA_HDC1080
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
                        new Parameter { Key = "SCL", DefaultValue = "99", Type = "number" },
                        new Parameter { Key = "SDA", DefaultValue = "88", Type = "number" }
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
                    new BuildFlagItem
                    {
                        Key = "SUPLA_SHT3x",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "33", Type = "number" }, // Different value
                            new Parameter { Key = "SDA", Value = "44", Type = "number" }  // Different value
                        }
                    },
                    new BuildFlagItem
                    {
                        Key = "SUPLA_HDC1080",
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "SCL", Value = "22", Type = "number" },
                            new Parameter { Key = "SDA", Value = "21", Type = "number" }
                        }
                    }
                };

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed, globalSettings);

                var result = File.ReadAllText(temp);

                using (new AssertionScope())
                {
                    // Should use the first matching BuildFlag value (from SUPLA_BME280)
                    result.Should().Contain(" -D GlobalParameter_SCL=22", "should use first BuildFlag value found");
                    result.Should().Contain(" -D GlobalParameter_SDA=21", "should use first BuildFlag value found");

                    // Should not use values from second flag or defaults
                    result.Should().NotContain("GlobalParameter_SCL=33");
                    result.Should().NotContain("GlobalParameter_SDA=44");
                    result.Should().NotContain("GlobalParameter_SCL=99");
                    result.Should().NotContain("GlobalParameter_SDA=88");

                    // No flag-specific parameters should be written
                    result.Should().NotContain("SUPLA_BME280_SCL");
                    result.Should().NotContain("SUPLA_SHT3x_SCL");
                    result.Should().NotContain("SUPLA_HDC1080_SCL");
                }
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }
}
