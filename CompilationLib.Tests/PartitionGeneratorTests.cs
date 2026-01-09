using CompilationLib;
using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace CompilationLib.Tests
{
    public class PartitionGeneratorTests : IDisposable
    {
        private readonly string _testDirectory;

        public PartitionGeneratorTests()
        {
            // Create a temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"PartitionGeneratorTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
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
        public void EnsurePartitionFilesExist_CreatesPartitionsDirectory()
        {
            // Act
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);

            // Assert
            var partitionsDir = Path.Combine(_testDirectory, "partitions");
            Directory.Exists(partitionsDir).Should().BeTrue();
        }

        [Fact]
        public void EnsurePartitionFilesExist_CreatesAllRequiredFiles()
        {
            // Act
            var filesCreated = PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);

            // Assert
            filesCreated.Should().Be(4); // 4MB, 8MB, 16MB, 32MB
            
            var partitionsDir = Path.Combine(_testDirectory, "partitions");
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_4mb.csv")).Should().BeTrue();
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_8mb.csv")).Should().BeTrue();
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_16mb.csv")).Should().BeTrue();
            File.Exists(Path.Combine(partitionsDir, "min_spiffs_32mb.csv")).Should().BeTrue();
        }

        [Fact]
        public void EnsurePartitionFilesExist_DoesNotRecreateExistingFiles()
        {
            // Arrange - Create files first
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);

            // Act - Call again
            var filesCreated = PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);

            // Assert - Should not create any new files
            filesCreated.Should().Be(0);
        }

        [Fact]
        public void EnsurePartitionFilesExist_ThrowsForInvalidPath()
        {
            // Act & Assert
            Action act = () => PartitionGenerator.EnsurePartitionFilesExist("C:\\NonExistentDirectory12345");
            act.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void ValidatePartitionFile_ReturnsTrueForValidFile()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);
            var partitionFile = Path.Combine(_testDirectory, "partitions", "min_spiffs_4mb.csv");

            // Act
            var isValid = PartitionGenerator.ValidatePartitionFile(partitionFile);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void ValidatePartitionFile_ReturnsFalseForNonExistentFile()
        {
            // Act
            var isValid = PartitionGenerator.ValidatePartitionFile("nonexistent.csv");

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidatePartitionFile_RequiresOTAPartitions()
        {
            // Arrange - Create a partition file without OTA support
            var invalidPartitionFile = Path.Combine(_testDirectory, "invalid.csv");
            File.WriteAllText(invalidPartitionFile, "# Name, Type, SubType, Offset, Size\nnvs, data, nvs, 0x9000, 0x5000\n");

            // Act
            var isValid = PartitionGenerator.ValidatePartitionFile(invalidPartitionFile);

            // Assert - Should be invalid because it's missing OTA partitions
            isValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("min_spiffs_4mb.csv")]
        [InlineData("min_spiffs_8mb.csv")]
        [InlineData("min_spiffs_16mb.csv")]
        [InlineData("min_spiffs_32mb.csv")]
        public void GeneratedPartitionFiles_ContainRequiredPartitions(string fileName)
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);
            var partitionFile = Path.Combine(_testDirectory, "partitions", fileName);

            // Act
            var content = File.ReadAllText(partitionFile);

            // Assert
            content.Should().Contain("nvs");      // NVS storage
            content.Should().Contain("otadata");  // OTA data
            content.Should().Contain("ota_0");    // First app partition
            content.Should().Contain("ota_1");    // Second app partition (OTA)
            content.Should().Contain("spiffs");   // File system
        }

        [Fact]
        public void GetPartitionLayout_ParsesPartitionFile()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);
            var partitionFile = Path.Combine(_testDirectory, "partitions", "min_spiffs_4mb.csv");

            // Act
            var layout = PartitionGenerator.GetPartitionLayout(partitionFile);

            // Assert
            layout.Should().NotBeNull();
            layout.Partitions.Should().HaveCountGreaterThanOrEqualTo(5); // nvs, otadata, app0, app1, spiffs
            layout.App0Offset.Should().Be(0x10000); // Standard offset for app0
            layout.App0Size.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetPartitionLayout_ThrowsForNonExistentFile()
        {
            // Act & Assert
            Action act = () => PartitionGenerator.GetPartitionLayout("nonexistent.csv");
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void GeneratedPartitions_HaveCorrectSizes()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);

            // Act & Assert - Verify each partition file has reasonable sizes
            var partitionsDir = Path.Combine(_testDirectory, "partitions");
            
            // 4MB partition
            var layout4mb = PartitionGenerator.GetPartitionLayout(Path.Combine(partitionsDir, "min_spiffs_4mb.csv"));
            layout4mb.App0Size.Should().BeGreaterThan(0x100000); // At least 1MB for app
            
            // 8MB partition
            var layout8mb = PartitionGenerator.GetPartitionLayout(Path.Combine(partitionsDir, "min_spiffs_8mb.csv"));
            layout8mb.App0Size.Should().BeGreaterThan(layout4mb.App0Size); // Should have more space
            
            // 16MB partition
            var layout16mb = PartitionGenerator.GetPartitionLayout(Path.Combine(partitionsDir, "min_spiffs_16mb.csv"));
            layout16mb.App0Size.Should().BeGreaterThan(layout8mb.App0Size);
            
            // 32MB partition
            var layout32mb = PartitionGenerator.GetPartitionLayout(Path.Combine(partitionsDir, "min_spiffs_32mb.csv"));
            layout32mb.App0Size.Should().BeGreaterThan(layout16mb.App0Size);
        }

        [Fact]
        public void GeneratedPartitions_SupportOTA()
        {
            // Arrange
            PartitionGenerator.EnsurePartitionFilesExist(_testDirectory);
            var partitionsDir = Path.Combine(_testDirectory, "partitions");

            // Act & Assert - Verify all generated files support OTA
            var files = new[] { "min_spiffs_4mb.csv", "min_spiffs_8mb.csv", "min_spiffs_16mb.csv", "min_spiffs_32mb.csv" };
            
            foreach (var file in files)
            {
                var filePath = Path.Combine(partitionsDir, file);
                var layout = PartitionGenerator.GetPartitionLayout(filePath);
                
                // Should have two app partitions for OTA
                var appPartitions = layout.Partitions.FindAll(p => 
                    p.Type.Equals("app", StringComparison.OrdinalIgnoreCase));
                
                appPartitions.Should().HaveCount(2, $"{file} should have 2 app partitions for OTA");
                appPartitions[0].SubType.Should().Contain("ota_0");
                appPartitions[1].SubType.Should().Contain("ota_1");
            }
        }
    }
}
