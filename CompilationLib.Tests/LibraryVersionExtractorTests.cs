using Xunit;
using FluentAssertions;
using System;
using System.IO;
using CompilationLib;

namespace CompilationLib.Tests
{
    public class LibraryVersionExtractorTests
    {
        [Fact]
        public void GetSuplaDeviceVersion_WithNonExistentPath_ReturnsEmptyString()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_directory_" + Guid.NewGuid());

            // Act
            var version = LibraryVersionExtractor.GetSuplaDeviceVersion(nonExistentPath);

            // Assert
            version.Should().BeEmpty();
        }

        [Fact]
        public void GetSuplaDeviceVersion_WithEmptyPath_ReturnsEmptyString()
        {
            // Act
            var version = LibraryVersionExtractor.GetSuplaDeviceVersion(string.Empty);

            // Assert
            version.Should().BeEmpty();
        }

        [Fact]
        public void GetSuplaDeviceVersion_WithNullPath_ReturnsEmptyString()
        {
            // Act
            var version = LibraryVersionExtractor.GetSuplaDeviceVersion(null);

            // Assert
            version.Should().BeEmpty();
        }

        [Fact]
        public void GetSuplaDeviceVersion_WithLibraryJson_ExtractsVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            var libDir = Path.Combine(tempDir, "lib", "SuplaDevice");
            Directory.CreateDirectory(libDir);

            var libraryJsonPath = Path.Combine(libDir, "library.json");
            File.WriteAllText(libraryJsonPath, @"{
                ""name"": ""SuplaDevice"",
                ""version"": ""2.4.5"",
                ""description"": ""SUPLA Device library""
            }");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetSuplaDeviceVersion(tempDir);

                // Assert
                version.Should().Be("2.4.5");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetSuplaDeviceVersion_WithLibraryProperties_ExtractsVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            var libDir = Path.Combine(tempDir, "lib", "SuplaDevice");
            Directory.CreateDirectory(libDir);

            var libraryPropertiesPath = Path.Combine(libDir, "library.properties");
            File.WriteAllText(libraryPropertiesPath, @"name=SuplaDevice
version=2.3.1
author=SUPLA Team
description=SUPLA Device library for ESP32");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetSuplaDeviceVersion(tempDir);

                // Assert
                version.Should().Be("2.3.1");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetSuplaDeviceVersion_WithHeaderFile_ExtractsVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            var srcDir = Path.Combine(tempDir, "lib", "SuplaDevice", "src", "supla", "device");
            Directory.CreateDirectory(srcDir);

            var headerPath = Path.Combine(srcDir, "sw_version.h");
            File.WriteAllText(headerPath, @"#ifndef SUPLA_DEVICE_SW_VERSION_H
#define SUPLA_DEVICE_SW_VERSION_H

#define SW_VERSION ""2.5.0""

#endif");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetSuplaDeviceVersion(tempDir);

                // Assert
                version.Should().Be("2.5.0");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetSuplaDeviceVersion_PrefersLibraryJsonOverOthers()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            var libDir = Path.Combine(tempDir, "lib", "SuplaDevice");
            var srcDir = Path.Combine(libDir, "src", "supla", "device");
            Directory.CreateDirectory(srcDir);

            // Create multiple version sources
            File.WriteAllText(Path.Combine(libDir, "library.json"), @"{""version"": ""3.0.0""}");
            File.WriteAllText(Path.Combine(libDir, "library.properties"), "version=2.0.0");
            File.WriteAllText(Path.Combine(srcDir, "sw_version.h"), @"#define SW_VERSION ""1.0.0""");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetSuplaDeviceVersion(tempDir);

                // Assert - should prefer library.json
                version.Should().Be("3.0.0");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetAllLibraryVersions_ReturnsMultipleLibraries()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            var libDir = Path.Combine(tempDir, "lib");
            
            // Create multiple libraries
            var suplaDeviceDir = Path.Combine(libDir, "SuplaDevice");
            var otherLibDir = Path.Combine(libDir, "OtherLib");
            Directory.CreateDirectory(suplaDeviceDir);
            Directory.CreateDirectory(otherLibDir);

            File.WriteAllText(Path.Combine(suplaDeviceDir, "library.json"), @"{""version"": ""2.4.0""}");
            File.WriteAllText(Path.Combine(otherLibDir, "library.properties"), "version=1.2.3");

            try
            {
                // Act
                var versions = LibraryVersionExtractor.GetAllLibraryVersions(tempDir);

                // Assert
                versions.Should().ContainKey("SuplaDevice");
                versions["SuplaDevice"].Should().Be("2.4.0");
                versions.Should().ContainKey("OtherLib");
                versions["OtherLib"].Should().Be("1.2.3");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetGuiGenericVersion_WithValidPlatformioIni_ExtractsVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var platformioIniPath = Path.Combine(tempDir, "platformio.ini");
            File.WriteAllText(platformioIniPath, @"[platformio]
default_envs = GUI_Generic_ESP32

[common]
BUILD_VERSION='""25.02.11""'
platform = espressif32");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetGuiGenericVersion(tempDir);

                // Assert
                version.Should().Be("25.02.11");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetGuiGenericVersion_WithDoubleQuotes_ExtractsVersion()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var platformioIniPath = Path.Combine(tempDir, "platformio.ini");
            File.WriteAllText(platformioIniPath, @"BUILD_VERSION=""24.12.01""");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetGuiGenericVersion(tempDir);

                // Assert
                version.Should().Be("24.12.01");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetGuiGenericVersion_WithNonExistentFile_ReturnsEmptyString()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());

            // Act
            var version = LibraryVersionExtractor.GetGuiGenericVersion(tempDir);

            // Assert
            version.Should().BeEmpty();
        }

        [Fact]
        public void GetGuiGenericVersion_WithMissingBuildVersion_ReturnsEmptyString()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var platformioIniPath = Path.Combine(tempDir, "platformio.ini");
            File.WriteAllText(platformioIniPath, @"[platformio]
default_envs = GUI_Generic_ESP32");

            try
            {
                // Act
                var version = LibraryVersionExtractor.GetGuiGenericVersion(tempDir);

                // Assert
                version.Should().BeEmpty();
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void GetBothVersions_WithFullRepository_ExtractsBothVersions()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_repo_" + Guid.NewGuid());
            var libDir = Path.Combine(tempDir, "lib", "SuplaDevice");
            Directory.CreateDirectory(libDir);

            // Create SuplaDevice library.json
            File.WriteAllText(Path.Combine(libDir, "library.json"), @"{""version"": ""2.4.5""}");

            // Create platformio.ini with GUI-Generic version
            File.WriteAllText(Path.Combine(tempDir, "platformio.ini"), @"BUILD_VERSION='""25.02.11""'");

            try
            {
                // Act
                var suplaVersion = LibraryVersionExtractor.GetSuplaDeviceVersion(tempDir);
                var ggVersion = LibraryVersionExtractor.GetGuiGenericVersion(tempDir);

                // Assert
                suplaVersion.Should().Be("2.4.5");
                ggVersion.Should().Be("25.02.11");
            }
            finally
            {
                // Cleanup
                Directory.Delete(tempDir, true);
            }
        }
    }
}
