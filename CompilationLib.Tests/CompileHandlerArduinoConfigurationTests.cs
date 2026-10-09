using FluentAssertions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace CompilationLib.Tests
{
    public class CompileHandlerArduinoConfigurationTests : IDisposable
    {
        private const string PartitionCsvContent = "app0, app, ota_0, 0x10000, 0x180000,\n";
        private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "CompileHandlerArduinoConfigurationTests_" + Guid.NewGuid().ToString("N"));

        public CompileHandlerArduinoConfigurationTests()
        {
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        [Fact]
        public void GetPartitionBuildProperties_UsesSelectedFlashAndAppPartitionSize()
        {
            var partitionPath = Path.Combine(_testDirectory, "min_spiffs_4mb.csv");
            File.WriteAllText(partitionPath, PartitionCsvContent);
            var method = typeof(CompileHandler).GetMethod("GetPartitionBuildProperties", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            var result = (System.Collections.Generic.List<string>)method.Invoke(null, new object[] { "4MB", partitionPath });

            result.Should().Equal(
                "build.flash_size=4MB",
                "build.partitions=min_spiffs_4mb",
                "upload.maximum_size=1572864");
        }

        [Fact]
        public void SetEnglishLocale_SetsEnglishProcessLocale()
        {
            var processStartInfo = new ProcessStartInfo();
            var method = typeof(CompileHandler).GetMethod("SetEnglishLocale", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            method.Invoke(null, new object[] { processStartInfo });

            processStartInfo.Environment["LANG"].Should().Be("C");
            processStartInfo.Environment["LC_ALL"].Should().Be("C");
            processStartInfo.Environment["LC_MESSAGES"].Should().Be("C");
            processStartInfo.Environment["LANGUAGE"].Should().Be("en");
        }

        [Fact]
        public void GetUploadArguments_WhenDeploymentIsSelected_IncludesUploadAndPort()
        {
            var request = new CompileRequest
            {
                ShouldDeploy = true,
                PortCom = "COM7"
            };
            var method = typeof(CompileHandler).GetMethod("GetUploadArguments", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            var result = (System.Collections.Generic.List<string>)method.Invoke(null, new object[] { request });

            result.Should().Equal("--upload", "--port", "COM7");
        }

        [Fact]
        public void GetUploadArguments_WhenDeploymentIsNotSelected_IsEmpty()
        {
            var request = new CompileRequest
            {
                ShouldDeploy = false,
                PortCom = "COM7"
            };
            var method = typeof(CompileHandler).GetMethod("GetUploadArguments", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            var result = (System.Collections.Generic.List<string>)method.Invoke(null, new object[] { request });

            result.Should().BeEmpty();
        }

        [Fact]
        public void Z2SUpdateService_GetFirmwareFlashOffset_UsesAppPartitionForUpdateUpload()
        {
            var method = typeof(Z2SUpdateService).GetMethod("GetFirmwareFlashOffset", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            var updateOffset = (long)method.Invoke(null, new object[] { false });
            var fullImageOffset = (long)method.Invoke(null, new object[] { true });

            updateOffset.Should().Be(0x10000);
            fullImageOffset.Should().Be(0x0);
        }

        [Fact]
        public void StagePartitionCsv_RestoresExistingSketchPartitionFile()
        {
            var sourceDirectory = Path.Combine(_testDirectory, "partitions");
            var sketchDirectory = Path.Combine(_testDirectory, "sketch");
            Directory.CreateDirectory(sourceDirectory);
            Directory.CreateDirectory(sketchDirectory);
            var sourcePath = Path.Combine(sourceDirectory, "min_spiffs_4mb.csv");
            var targetPath = Path.Combine(sketchDirectory, "partitions.csv");
            File.WriteAllText(sourcePath, PartitionCsvContent);
            File.WriteAllText(targetPath, "original partition table\n");
            var method = typeof(CompileHandler).GetMethod("StagePartitionCsv", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            var partitionOverride = (IDisposable)method.Invoke(null, new object[] { sourcePath, sketchDirectory });
            File.ReadAllText(targetPath).Should().Be(PartitionCsvContent);

            partitionOverride.Dispose();

            File.ReadAllText(targetPath).Should().Be("original partition table\n");
        }
    }
}
