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
                            new Parameter{ Name = "Altitude" , Value= "253.3" , Type="number"}
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
    }
}
