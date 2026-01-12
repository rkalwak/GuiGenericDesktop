using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace CompilationLib.Tests
{
    public class PlatformioPartitionConfigurationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _testIniPath;

        public PlatformioPartitionConfigurationTests()
        {
            // Create a temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"PlatformioPartitionTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _testIniPath = Path.Combine(_testDirectory, "platformio.ini");
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void CommentUnlistedFlagsBetweenMarkers_PreservesPartitionConfiguration()
        {
            // Arrange - Create a platformio.ini with partition configuration
            var iniContent = @"
[env:GUI_Generic_ESP32]
platform = espressif32
board = esp32dev
board_build.partitions = partitions/min_spiffs_8mb.csv
;flagsstart
 -D SUPLA_CONFIG
 -D SUPLA_RELAY
;flagsend
";
            File.WriteAllText(_testIniPath, iniContent);

            var buildFlags = new System.Collections.Generic.List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_CONFIG" }
            };

            var handler = new PlatformioCliHandler();

            // Act
            handler.CommentUnlistedFlagsBetweenMarkers(_testIniPath, buildFlags, new GlobalSettings());

            // Assert
            var result = File.ReadAllText(_testIniPath);
            result.Should().Contain("board_build.partitions = partitions/min_spiffs_8mb.csv");
            result.Should().Contain(" -D SUPLA_CONFIG"); // Uncommented
            result.Should().Contain(";-D SUPLA_RELAY"); // Commented out (no space after semicolon)
        }

        [Theory]
        [InlineData("esp32", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("esp32", "8MB", "min_spiffs_8mb.csv")]
        [InlineData("esp32-c6", "16MB", "min_spiffs_16mb.csv")]
        [InlineData("esp32-s3", "32MB", "min_spiffs_32mb.csv")]
        public void PartitionManager_ReturnsCorrectSchemeForBoardAndFlashSize(string board, string flashSize, string expectedFile)
        {
            // Act
            var scheme = PartitionManager.GetPartitionScheme(flashSize, board);

            // Assert
            scheme.Should().NotBeNull();
            scheme.FileName.Should().Be(expectedFile);
            scheme.FlashSize.Should().Be(flashSize);
            scheme.HasOTA.Should().BeTrue();
        }

        [Theory]
        [InlineData("esp32", "4MB", true)]
        [InlineData("esp32", "32MB", false)] // ESP32 doesn't support 32MB
        [InlineData("esp32-s3", "32MB", true)] // ESP32-S3 supports 32MB
        [InlineData("esp32-c6", "32MB", false)] // ESP32-C6 doesn't support 32MB
        public void PartitionManager_ValidatesFlashSizeCorrectly(string board, string flashSize, bool expected)
        {
            // Act
            var isValid = PartitionManager.ValidateFlashSize(board, flashSize);

            // Assert
            isValid.Should().Be(expected);
        }

        [Fact]
        public void CompileRequest_IncludesFlashSizeProperty()
        {
            // Arrange & Act
            var request = new CompileRequest
            {
                EnvironmentName = "GUI_Generic_ESP32",
                FlashSize = "8MB"
            };

            // Assert
            request.FlashSize.Should().Be("8MB");
        }
    }
}
