using CompilationLib;
using FluentAssertions;
using Xunit;

namespace CompilationLib.Tests
{
    public class PartitionManagerTests
    {
        [Theory]
        [InlineData("GUI_Generic_ESP32", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("GUI_Generic_ESP32", "8MB", "min_spiffs_8mb.csv")]
        [InlineData("GUI_Generic_ESP32C6", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("GUI_Generic_ESP32C6", "16MB", "min_spiffs_16mb.csv")]
        [InlineData("GUI_Generic_ESP32S3", "32MB", "min_spiffs_32mb.csv")]
        [InlineData("esp32", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("esp32c3", "8MB", "min_spiffs_8mb.csv")]
        public void GetPartitionScheme_ReturnsCorrectScheme(string platform, string flashSize, string expectedFileName)
        {
            // Act
            var scheme = PartitionManager.GetPartitionScheme(platform, flashSize);

            // Assert
            scheme.Should().NotBeNull();
            scheme.FileName.Should().Be(expectedFileName);
            scheme.FlashSize.Should().Be(flashSize);
            scheme.HasOTA.Should().BeTrue();
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "64MB")] // Unsupported flash size for ESP32
        [InlineData("GUI_Generic_ESP32C6", "32MB")] // Unsupported flash size for C6
        [InlineData("", "4MB")] // Empty platform
        [InlineData("GUI_Generic_ESP32", "")] // Empty flash size
        [InlineData("unknown_platform", "4MB")] // Unknown platform
        public void GetPartitionScheme_ReturnsNull_WhenInvalid(string platform, string flashSize)
        {
            // Act
            var scheme = PartitionManager.GetPartitionScheme(platform, flashSize);

            // Assert
            scheme.Should().BeNull();
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("esp32", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("GUI_Generic_ESP32C3", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("GUI_Generic_ESP32C6", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("GUI_Generic_ESP32S3", new[] { "4MB", "8MB", "16MB", "32MB" })]
        public void GetSupportedFlashSizes_ReturnsCorrectSizes(string platform, string[] expectedSizes)
        {
            // Act
            var sizes = PartitionManager.GetSupportedFlashSizes(platform);

            // Assert
            sizes.Should().BeEquivalentTo(expectedSizes);
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "4MB", true)]
        [InlineData("GUI_Generic_ESP32", "8MB", true)]
        [InlineData("GUI_Generic_ESP32", "16MB", true)]
        [InlineData("GUI_Generic_ESP32", "32MB", false)] // Not supported for ESP32
        [InlineData("GUI_Generic_ESP32S3", "32MB", true)]
        [InlineData("GUI_Generic_ESP32C6", "32MB", false)] // Not supported for C6
        [InlineData("", "4MB", false)] // Empty platform
        [InlineData("unknown", "4MB", false)] // Unknown platform
        public void ValidateFlashSize_ReturnsCorrectResult(string platform, string flashSize, bool expected)
        {
            // Act
            var result = PartitionManager.ValidateFlashSize(platform, flashSize);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "4MB")]
        [InlineData("esp32", "4MB")]
        [InlineData("GUI_Generic_ESP32C3", "4MB")]
        [InlineData("GUI_Generic_ESP32C6", "4MB")]
        [InlineData("GUI_Generic_ESP32S3", "4MB")]
        [InlineData("", "4MB")] // Fallback for empty
        [InlineData("unknown", "4MB")] // Fallback for unknown
        public void GetDefaultFlashSize_ReturnsCorrectDefault(string platform, string expectedDefault)
        {
            // Act
            var defaultSize = PartitionManager.GetDefaultFlashSize(platform);

            // Assert
            defaultSize.Should().Be(expectedDefault);
        }

        [Fact]
        public void GetPartitionScheme_AllSchemes_HaveOTASupport()
        {
            // Arrange
            var platforms = new[] { "esp32", "esp32c3", "esp32c6", "esp32s3" };
            
            // Act & Assert
            foreach (var platform in platforms)
            {
                var sizes = PartitionManager.GetSupportedFlashSizes(platform);
                foreach (var size in sizes)
                {
                    var scheme = PartitionManager.GetPartitionScheme(platform, size);
                    scheme.Should().NotBeNull($"scheme for {platform}/{size} should exist");
                    scheme.HasOTA.Should().BeTrue($"scheme for {platform}/{size} should have OTA support");
                }
            }
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "esp32")]
        [InlineData("GUI_GENERIC_ESP32", "esp32")] // Case insensitive
        [InlineData("esp32", "esp32")]
        [InlineData("ESP32", "esp32")]
        [InlineData("GUI_Generic_ESP32C6", "esp32c6")]
        [InlineData("esp32c6", "esp32c6")]
        public void ChipNameNormalization_WorksCorrectly(string input, string expectedChip)
        {
            // Act - test normalization indirectly through GetPartitionScheme
            var scheme = PartitionManager.GetPartitionScheme(input, "4MB");

            // Assert
            scheme.Should().NotBeNull($"platform {input} should normalize to {expectedChip}");
        }
    }
}
