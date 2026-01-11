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

        [Fact]
        public void GetPartitionLayout_ThrowsForNonExistentFile()
        {
            // Act & Assert
            Action act = () => PartitionGenerator.GetPartitionLayout("nonexistent.csv");
            act.Should().Throw<FileNotFoundException>();
        }
    }
}
