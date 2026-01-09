using CompilationLib;
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

        [Fact]
        public void PartitionGenerator_CreatesFilesInTestDirectory()
        {
            // Arrange
            var partitionsDir = Path.Combine(_testDirectory, "partitions");

            // Act
            var filesCreated = PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);

            // Assert
            filesCreated.Should().Be(4); // 4MB, 8MB, 16MB, 32MB
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_4mb.csv")).Should().BeTrue();
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_8mb.csv")).Should().BeTrue();
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_16mb.csv")).Should().BeTrue();
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_32mb.csv")).Should().BeTrue();
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("GUI_Generic_ESP32", "8MB", "min_spiffs_8mb.csv")]
        [InlineData("GUI_Generic_ESP32C6", "16MB", "min_spiffs_16mb.csv")]
        [InlineData("GUI_Generic_ESP32S3", "32MB", "min_spiffs_32mb.csv")]
        public void PartitionManager_ReturnsCorrectSchemeForPlatformAndFlashSize(string platform, string flashSize, string expectedFile)
        {
            // Act
            var scheme = PartitionManager.GetPartitionScheme(platform, flashSize);

            // Assert
            scheme.Should().NotBeNull();
            scheme.FileName.Should().Be(expectedFile);
            scheme.FlashSize.Should().Be(flashSize);
            scheme.HasOTA.Should().BeTrue();
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "4MB", true)]
        [InlineData("GUI_Generic_ESP32", "32MB", false)] // ESP32 doesn't support 32MB
        [InlineData("GUI_Generic_ESP32S3", "32MB", true)] // ESP32-S3 supports 32MB
        [InlineData("GUI_Generic_ESP32C6", "32MB", false)] // ESP32-C6 doesn't support 32MB
        public void PartitionManager_ValidatesFlashSizeCorrectly(string platform, string flashSize, bool expected)
        {
            // Act
            var isValid = PartitionManager.ValidateFlashSize(platform, flashSize);

            // Assert
            isValid.Should().Be(expected);
        }

        [Fact]
        public void CompileRequest_IncludesFlashSizeProperty()
        {
            // Arrange & Act
            var request = new CompileRequest
            {
                Platform = "GUI_Generic_ESP32",
                FlashSize = "8MB"
            };

            // Assert
            request.FlashSize.Should().Be("8MB");
        }

        [Fact]
        public void PartitionGenerator_ValidatesGeneratedFiles()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);
            var partitionFile = Path.Combine(_testDirectory, "partitions", "min_spiffs_8mb.csv");

            // Act
            var isValid = PartitionGenerator.ValidatePartitionFile(partitionFile);
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert
            isValid.Should().BeTrue();
            layout.Should().NotBeNull();
            layout.App0Offset.Should().Be(0x10000);
            layout.Partitions.Should().HaveCountGreaterThanOrEqualTo(5); // nvs, otadata, app0, app1, spiffs
        }

        [Fact]
        public void PartitionManager_GetSupportedFlashSizes_ReturnsCorrectSizes()
        {
            // Act
            var esp32Sizes = PartitionManager.GetSupportedFlashSizes("GUI_Generic_ESP32");
            var esp32s3Sizes = PartitionManager.GetSupportedFlashSizes("GUI_Generic_ESP32S3");

            // Assert
            esp32Sizes.Should().BeEquivalentTo(new[] { "4MB", "8MB", "16MB" });
            esp32s3Sizes.Should().BeEquivalentTo(new[] { "4MB", "8MB", "16MB", "32MB" });
        }
    }
}
