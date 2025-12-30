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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

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

                handler.CommentUnlistedFlagsBetweenMarkers(temp, allowed);

                var result = File.ReadAllText(temp);

                result.Should().NotContain("SUPLA_FLAG_OptionalText");
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }
}
