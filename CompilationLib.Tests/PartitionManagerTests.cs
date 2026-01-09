using FluentAssertions;
using Xunit;

namespace CompilationLib.Tests
{
    public class PartitionManagerTests
    {
        [Theory]
        [InlineData("GUI_Generic_ESP32", "esp32", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("GUI_Generic_ESP32", "esp32", "8MB", "min_spiffs_8mb.csv")]
        [InlineData("GUI_Generic_ESP32C6", "esp32c6", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("GUI_Generic_ESP32C6", "esp32c6", "16MB", "min_spiffs_16mb.csv")]
        [InlineData("GUI_Generic_ESP32S3", "esp32s3", "32MB", "min_spiffs_32mb.csv")]
        [InlineData("esp32", "esp32", "4MB", "min_spiffs_4mb.csv")]
        [InlineData("esp32c3", "esp32c3", "8MB", "min_spiffs_8mb.csv")]
        public void GetPartitionScheme_ReturnsCorrectScheme(string platform, string board, string flashSize, string expectedFileName)
        {
            // Act
            var scheme = PartitionManager.GetPartitionScheme(flashSize, board);

            // Assert
            scheme.Should().NotBeNull();
            scheme.FileName.Should().Be(expectedFileName);
            scheme.FlashSize.Should().Be(flashSize);
            scheme.HasOTA.Should().BeTrue();
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "esp32", "64MB")] // Unsupported flash size for ESP32
        [InlineData("GUI_Generic_ESP32C6", "esp32c6", "32MB")] // Unsupported flash size for C6
        [InlineData("", "", "4MB")] // Empty platform
        [InlineData("GUI_Generic_ESP32", "esp32", "")] // Empty flash size
        [InlineData("unknown_platform", "unknown_platform", "4MB")] // Unknown platform
        public void GetPartitionScheme_ReturnsNull_WhenInvalid(string platform, string board, string flashSize)
        {
            // Act
            var scheme = PartitionManager.GetPartitionScheme(flashSize, board);

            // Assert
            scheme.Should().BeNull();
        }

        [Theory]
        [InlineData("esp32", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("esp32c3", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("esp32c6", new[] { "4MB", "8MB", "16MB" })]
        [InlineData("esp32s3", new[] { "4MB", "8MB", "16MB", "32MB" })]
        public void GetSupportedFlashSizes_ReturnsCorrectSizes(string board, string[] expectedSizes)
        {
            // Act
            var sizes = PartitionManager.GetSupportedFlashSizes(board);

            // Assert
            sizes.Should().BeEquivalentTo(expectedSizes);
        }

        [Theory]
        [InlineData("esp32", "4MB", true)]
        [InlineData("esp32", "8MB", true)]
        [InlineData("esp32", "16MB", true)]
        [InlineData("esp32", "32MB", false)] // Not supported for ESP32
        [InlineData("esp32s3", "32MB", true)]
        [InlineData("esp32c6", "32MB", false)] // Not supported for C6
        [InlineData("", "4MB", false)] // Empty platform
        [InlineData("unknown", "4MB", false)] // Unknown platform
        public void ValidateFlashSize_ReturnsCorrectResult(string board, string flashSize, bool expected)
        {
            // Act
            var result = PartitionManager.ValidateFlashSize(board, flashSize);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("esp32", "4MB")]
        [InlineData("esp32c3", "4MB")]
        [InlineData("esp32c6", "4MB")]
        [InlineData("esp32s3", "4MB")]
        [InlineData("", "4MB")] // Fallback for empty
        [InlineData("unknown", "4MB")] // Fallback for unknown
        public void GetDefaultFlashSize_ReturnsCorrectDefault(string board, string expectedDefault)
        {
            // Act
            var defaultSize = PartitionManager.GetDefaultFlashSize(board);

            // Assert
            defaultSize.Should().Be(expectedDefault);
        }

        [Theory]
        [InlineData("esp32")]
        [InlineData("esp32c3")]
        [InlineData("esp32c6")]
        [InlineData("esp32s3")]
        public void GetPartitionScheme_AllSchemes_HaveOTASupport(string board)
        {
            // Act & Assert
            var sizes = PartitionManager.GetSupportedFlashSizes(board);
            foreach (var size in sizes)
            {
                var scheme = PartitionManager.GetPartitionScheme(size, board);
                scheme.Should().NotBeNull($"scheme for {board}/{size} should exist");
                scheme.HasOTA.Should().BeTrue($"scheme for {board}/{size} should have OTA support");
            }
        }

        [Theory]
        [InlineData("GUI_Generic_ESP32", "esp32", "esp32")]
        [InlineData("GUI_GENERIC_ESP32", "esp32", "esp32")] // Case insensitive
        [InlineData("esp32", "esp32", "esp32")]
        [InlineData("ESP32", "esp32", "esp32")]
        [InlineData("GUI_Generic_ESP32C6", "esp32c6", "esp32c6")]
        [InlineData("esp32c6", "esp32c6", "esp32c6")]
        public void ChipNameNormalization_WorksCorrectly(string input, string board, string expectedChip)
        {
            // Act - test normalization indirectly through GetPartitionScheme
            var scheme = PartitionManager.GetPartitionScheme("4MB", board);

            // Assert
            scheme.Should().NotBeNull($"platform {input} should normalize to {expectedChip}");
            board.Should().Be(expectedChip, $"platform {input} should normalize to {expectedChip}");
        }
    }
}
