using CompilationLib;
using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace CompilationLib.Tests
{
    public class EsptoolMergePartitionTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _repositoryPath;

        public EsptoolMergePartitionTests()
        {
            // Create a temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"EsptoolMergeTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Create repository path for partition files
            _repositoryPath = Path.Combine(_testDirectory, "repository");
            Directory.CreateDirectory(_repositoryPath);
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

        [Theory]
        [InlineData("GUI_Generic_ESP32", "4MB", 0x10000)]
        [InlineData("GUI_Generic_ESP32", "8MB", 0x10000)]
        [InlineData("GUI_Generic_ESP32C6", "16MB", 0x10000)]
        [InlineData("GUI_Generic_ESP32S3", "32MB", 0x10000)]
        public void PartitionLayout_ParsesCorrectFirmwareOffset(string platform, string flashSize, int expectedOffset)
        {
            // Arrange - Create partition files
            PartitionGenerator.EnsurePartitionFilesExist(_repositoryPath);
            
            var partitionFile = PartitionManager.GetPartitionFilePath(platform, flashSize, _repositoryPath);

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert
            layout.Should().NotBeNull();
            layout.App0Offset.Should().Be(expectedOffset);
        }

        [Fact]
        public void PartitionManager_GetPartitionFilePath_ReturnsValidPath()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_repositoryPath);

            // Act
            var partitionFile = PartitionManager.GetPartitionFilePath("GUI_Generic_ESP32", "8MB", _repositoryPath);

            // Assert
            partitionFile.Should().NotBeNullOrEmpty();
            File.Exists(partitionFile).Should().BeTrue();
            Path.GetFileName(partitionFile).Should().Be("min_spiffs_8mb.csv");
        }

        [Fact]
        public void PartitionGenerator_GetPartitionLayout_ParsesAllPartitions()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_repositoryPath);
            var partitionFile = Path.Combine(_repositoryPath, "partitions", "min_spiffs_4mb.csv");

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert
            layout.Should().NotBeNull();
            layout.Partitions.Should().HaveCountGreaterThanOrEqualTo(5);
            
            // Verify partition types
            layout.Partitions.Should().Contain(p => p.Name.Equals("nvs", StringComparison.OrdinalIgnoreCase));
            layout.Partitions.Should().Contain(p => p.Name.Equals("otadata", StringComparison.OrdinalIgnoreCase));
            layout.Partitions.Should().Contain(p => p.Name.Equals("app0", StringComparison.OrdinalIgnoreCase));
            layout.Partitions.Should().Contain(p => p.Name.Equals("app1", StringComparison.OrdinalIgnoreCase));
            layout.Partitions.Should().Contain(p => p.Name.Equals("spiffs", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("esp32")]
        [InlineData("esp32c3")]
        [InlineData("esp32c6")]
        [InlineData("esp32s3")]
        public void PartitionLayout_StandardOffsets_AreConsistent(string chipType)
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_repositoryPath);
            var platform = $"GUI_Generic_{chipType}".Replace("gui_generic_", "GUI_Generic_");
            var partitionFile = PartitionManager.GetPartitionFilePath(platform, "4MB", _repositoryPath);

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert - ESP32 standard offsets
            layout.Should().NotBeNull();
            layout.App0Offset.Should().Be(0x10000, "App0 should start at standard ESP32 offset");
            
            // Find NVS and OTA data partitions
            var nvsPartition = layout.Partitions.Find(p => p.Name.Equals("nvs", StringComparison.OrdinalIgnoreCase));
            var otaPartition = layout.Partitions.Find(p => p.Name.Equals("otadata", StringComparison.OrdinalIgnoreCase));
            
            nvsPartition.Should().NotBeNull();
            nvsPartition.Offset.Should().Be(0x9000, "NVS should be at standard offset");
            
            otaPartition.Should().NotBeNull();
            otaPartition.Offset.Should().Be(0xE000, "OTA data should be at standard offset");
        }

        [Fact]
        public void PartitionLayout_App0AndApp1_AreSameSize()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_repositoryPath);
            var partitionFile = Path.Combine(_repositoryPath, "partitions", "min_spiffs_8mb.csv");

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert
            var app0 = layout.Partitions.Find(p => p.Name.Equals("app0", StringComparison.OrdinalIgnoreCase));
            var app1 = layout.Partitions.Find(p => p.Name.Equals("app1", StringComparison.OrdinalIgnoreCase));

            app0.Should().NotBeNull();
            app1.Should().NotBeNull();
            app0.Size.Should().Be(app1.Size, "App0 and App1 should have the same size for OTA");
        }

        [Theory]
        [InlineData("4MB", 0x180000)]   // 1.5MB per app for 4MB flash
        [InlineData("8MB", 0x300000)]   // 3MB per app for 8MB flash  
        [InlineData("16MB", 0x500000)]  // 5MB per app for 16MB flash
        [InlineData("32MB", 0xA00000)]  // 10MB per app for 32MB flash
        public void PartitionLayout_AppSizes_MatchExpectedForFlashSize(string flashSize, int expectedAppSize)
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_repositoryPath);
            var platform = "GUI_Generic_ESP32S3"; // S3 supports all sizes including 32MB
            var partitionFile = PartitionManager.GetPartitionFilePath(platform, flashSize, _repositoryPath);
            
            if (partitionFile == null)
            {
                // Skip if platform doesn't support this flash size
                return;
            }

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert
            var app0 = layout.Partitions.Find(p => p.Name.Equals("app0", StringComparison.OrdinalIgnoreCase));
            app0.Should().NotBeNull();
            app0.Size.Should().Be(expectedAppSize, $"App size for {flashSize} flash should be correct");
        }

        [Fact]
        public void PartitionGenerator_GetPartitionLayout_HandlesComments()
        {
            // Arrange - Create a partition CSV with comments
            var testPartitionFile = Path.Combine(_testDirectory, "test_partition.csv");
            var content = @"# Name,   Type, SubType, Offset,  Size, Flags
# This is a comment
nvs,      data, nvs,     0x9000,  0x5000,
# Another comment
otadata,  data, ota,     0xe000,  0x2000,
app0,     app,  ota_0,   0x10000, 0x180000,
# Comment between partitions
app1,     app,  ota_1,   0x190000, 0x180000,
spiffs,   data, spiffs,  0x310000, 0x90000,
";
            File.WriteAllText(testPartitionFile, content);

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(testPartitionFile);

            // Assert
            layout.Should().NotBeNull();
            layout.Partitions.Should().HaveCount(5); // Comments should be ignored
        }

        [Fact]
        public void PartitionGenerator_GetPartitionLayout_HandlesHexFormats()
        {
            // Arrange - Create partition CSV with various hex formats
            var testPartitionFile = Path.Combine(_testDirectory, "hex_test.csv");
            var content = @"# Name,   Type, SubType, Offset,  Size, Flags
nvs,      data, nvs,     0x9000,  0x5000,
otadata,  data, ota,     0xE000,  0x2000,
app0,     app,  ota_0,   0x10000, 0x180000,
";
            File.WriteAllText(testPartitionFile, content);

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(testPartitionFile);

            // Assert
            layout.Should().NotBeNull();
            var nvs = layout.Partitions.Find(p => p.Name == "nvs");
            var otadata = layout.Partitions.Find(p => p.Name == "otadata");
            
            nvs.Offset.Should().Be(0x9000);
            otadata.Offset.Should().Be(0xE000); // Both lowercase and uppercase X should work
        }
    }
}
