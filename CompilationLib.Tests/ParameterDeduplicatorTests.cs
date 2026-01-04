using System;
using System.Collections.Generic;
using System.Linq;
using CompilationLib;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace CompilationLib.Tests
{
    public class ParameterDeduplicatorTests
    {
        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithNullBuildFlag_ShouldThrowArgumentNullException()
        {
            // Arrange
            var globalParams = new List<Parameter>();

            // Act & Assert
            BuildFlagItem nullFlag = null;
            Action act = () => ParameterDeduplicator.RemoveDuplicatedGlobalParameters(nullFlag, globalParams);
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*buildFlag*");
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithNullGlobalParameters_ShouldReturnOriginalFlag()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                FlagName = "BME280 Sensor",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "SCL", Name = "SCL Pin", Type = "number", Value = "22" },
                    new Parameter { Key = "SDA", Name = "SDA Pin", Type = "number", Value = "21" }
                }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, null);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().HaveCount(2);
            result.Parameters.Should().Contain(p => p.Key == "SCL");
            result.Parameters.Should().Contain(p => p.Key == "SDA");
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithEmptyGlobalParameters_ShouldReturnOriginalFlag()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                FlagName = "BME280 Sensor",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "SCL", Name = "SCL Pin", Type = "number", Value = "22" },
                    new Parameter { Key = "SDA", Name = "SDA Pin", Type = "number", Value = "21" }
                }
            };
            var globalParams = new List<Parameter>();

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            result.Should().NotBeNull();
            result.Parameters.Should().HaveCount(2);
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithMatchingGlobalParameters_ShouldRemoveDuplicates()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                FlagName = "BME280 Sensor",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "SCL", Name = "SCL Pin", Type = "number", Value = "22" },
                    new Parameter { Key = "SDA", Name = "SDA Pin", Type = "number", Value = "21" },
                    new Parameter { Key = "Address", Name = "I2C Address", Type = "string", Value = "0x76" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL", Type = "number", Value = "22" },
                new Parameter { Key = "SDA", Name = "Global SDA", Type = "number", Value = "21" }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Parameters.Should().HaveCount(1, "SCL and SDA should be removed");
                result.Parameters.Should().Contain(p => p.Key == "Address", "Address is not a global parameter");
                result.Parameters.Should().NotContain(p => p.Key == "SCL", "SCL is a global parameter");
                result.Parameters.Should().NotContain(p => p.Key == "SDA", "SDA is a global parameter");
            }
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_ShouldBeCaseInsensitive()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                FlagName = "BME280 Sensor",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "scl", Name = "SCL Pin", Type = "number", Value = "22" },
                    new Parameter { Key = "Sda", Name = "SDA Pin", Type = "number", Value = "21" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL", Type = "number", Value = "22" },
                new Parameter { Key = "SDA", Name = "Global SDA", Type = "number", Value = "21" }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            result.Parameters.Should().BeEmpty("scl and Sda should match SCL and SDA case-insensitively");
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithNoMatchingParameters_ShouldReturnAllParameters()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_DS18B20",
                FlagName = "DS18B20 Sensor",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "Pin", Name = "Data Pin", Type = "number", Value = "4" },
                    new Parameter { Key = "MaxSensors", Name = "Max Sensors", Type = "number", Value = "10" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL", Type = "number", Value = "22" },
                new Parameter { Key = "SDA", Name = "Global SDA", Type = "number", Value = "21" }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            using (new AssertionScope())
            {
                result.Parameters.Should().HaveCount(2);
                result.Parameters.Should().Contain(p => p.Key == "Pin");
                result.Parameters.Should().Contain(p => p.Key == "MaxSensors");
            }
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithEmptyParameters_ShouldReturnEmptyParameters()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_RELAY",
                FlagName = "Relay",
                Parameters = new List<Parameter>()
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL", Type = "number", Value = "22" }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_ShouldPreserveOtherProperties()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                FlagName = "BME280 Sensor",
                Description = "Temperature and humidity sensor",
                Section = "I2C Sensors",
                IsEnabled = true,
                EnabledByFlags = new List<string> { "SUPLA_I2C" },
                DependenciesToEnable = new List<string> { "SUPLA_SENSOR" },
                DisabledOnPlatforms = new List<string> { "ESP8266" },
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "SCL", Name = "SCL Pin", Type = "number", Value = "22" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL", Type = "number", Value = "22" }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            using (new AssertionScope())
            {
                result.Key.Should().Be("SUPLA_BME280");
                result.FlagName.Should().Be("BME280 Sensor");
                result.Description.Should().Be("Temperature and humidity sensor");
                result.Section.Should().Be("I2C Sensors");
                result.IsEnabled.Should().BeTrue();
                result.EnabledByFlags.Should().ContainSingle().Which.Should().Be("SUPLA_I2C");
                result.DependenciesToEnable.Should().ContainSingle().Which.Should().Be("SUPLA_SENSOR");
                result.DisabledOnPlatforms.Should().ContainSingle().Which.Should().Be("ESP8266");
                result.Parameters.Should().BeEmpty();
            }
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_MultipleFlags_ShouldProcessAll()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_BME280",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Type = "number" },
                        new Parameter { Key = "SDA", Type = "number" },
                        new Parameter { Key = "Address", Type = "string" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_SHT3x",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Type = "number" },
                        new Parameter { Key = "SDA", Type = "number" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_DS18B20",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "Pin", Type = "number" }
                    }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL" },
                new Parameter { Key = "SDA", Name = "Global SDA" }
            };

            // Act
            var results = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlags, globalParams);

            // Assert
            using (new AssertionScope())
            {
                results.Should().HaveCount(3);
                results[0].Parameters.Should().HaveCount(1).And.Contain(p => p.Key == "Address");
                results[1].Parameters.Should().BeEmpty();
                results[2].Parameters.Should().HaveCount(1).And.Contain(p => p.Key == "Pin");
            }
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_MultipleFlags_WithNullBuildFlags_ShouldThrowArgumentNullException()
        {
            // Arrange
            var globalParams = new List<Parameter>();

            // Act & Assert
            Action act = () => ParameterDeduplicator.RemoveDuplicatedGlobalParameters((IEnumerable<BuildFlagItem>)null, globalParams);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetDuplicatedParameterIdentifiers_ShouldReturnMatchingParameters()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "SCL", Name = "SCL Pin" },
                    new Parameter { Key = "SDA", Name = "SDA Pin" },
                    new Parameter { Key = "Address", Name = "I2C Address" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL" },
                new Parameter { Key = "SDA", Name = "Global SDA" }
            };

            // Act
            var duplicates = ParameterDeduplicator.GetDuplicatedParameterIdentifiers(buildFlag, globalParams);

            // Assert
            using (new AssertionScope())
            {
                duplicates.Should().HaveCount(2);
                duplicates.Should().Contain("SCL");
                duplicates.Should().Contain("SDA");
                duplicates.Should().NotContain("Address");
            }
        }

        [Fact]
        public void GetDuplicatedParameterIdentifiers_WithNoMatches_ShouldReturnEmpty()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_DS18B20",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "Pin", Name = "Data Pin" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL" },
                new Parameter { Key = "SDA", Name = "Global SDA" }
            };

            // Act
            var duplicates = ParameterDeduplicator.GetDuplicatedParameterIdentifiers(buildFlag, globalParams);

            // Assert
            duplicates.Should().BeEmpty();
        }

        [Fact]
        public void GetDuplicatedParameterIdentifiers_WithNullBuildFlag_ShouldReturnEmpty()
        {
            // Arrange
            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL" }
            };

            // Act
            var duplicates = ParameterDeduplicator.GetDuplicatedParameterIdentifiers(null, globalParams);

            // Assert
            duplicates.Should().BeEmpty();
        }

        [Fact]
        public void GetDuplicatedParameterIdentifiers_WithNullGlobalParameters_ShouldReturnEmpty()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "SCL", Name = "SCL Pin" }
                }
            };

            // Act
            var duplicates = ParameterDeduplicator.GetDuplicatedParameterIdentifiers(buildFlag, null);

            // Assert
            duplicates.Should().BeEmpty();
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_WithParameterUsingNameIdentifier_ShouldWork()
        {
            // Arrange
            var buildFlag = new BuildFlagItem
            {
                Key = "SUPLA_BME280",
                Parameters = new List<Parameter>
                {
                    new Parameter { Name = "SCL", Type = "number" }, // No Key, uses Name as Identifier
                    new Parameter { Name = "SDA", Type = "number" }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "Global SCL" },
                new Parameter { Key = "SDA", Name = "Global SDA" }
            };

            // Act
            var result = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlag, globalParams);

            // Assert
            result.Parameters.Should().BeEmpty("Name-based identifiers should also be matched");
        }

        [Fact]
        public void RemoveDuplicatedGlobalParameters_RealWorldScenario_MultipleI2CDevices()
        {
            // Arrange - Simulate multiple I2C sensors with SCL/SDA parameters
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_BME280",
                    FlagName = "BME280",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Value = "22" },
                        new Parameter { Key = "SDA", Value = "21" },
                        new Parameter { Key = "Address", Value = "0x76" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_SHT3x",
                    FlagName = "SHT3x",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Value = "22" },
                        new Parameter { Key = "SDA", Value = "21" },
                        new Parameter { Key = "Precision", Value = "HIGH" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_HDC1080",
                    FlagName = "HDC1080",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "SCL", Value = "22" },
                        new Parameter { Key = "SDA", Value = "21" }
                    }
                }
            };

            var globalParams = new List<Parameter>
            {
                new Parameter { Key = "SCL", Name = "I2C SCL Pin", Value = "22" },
                new Parameter { Key = "SDA", Name = "I2C SDA Pin", Value = "21" }
            };

            // Act
            var results = ParameterDeduplicator.RemoveDuplicatedGlobalParameters(buildFlags, globalParams);

            // Assert
            using (new AssertionScope())
            {
                results[0].Key.Should().Be("SUPLA_BME280");
                results[0].Parameters.Should().HaveCount(1).And.Contain(p => p.Key == "Address");

                results[1].Key.Should().Be("SUPLA_SHT3x");
                results[1].Parameters.Should().HaveCount(1).And.Contain(p => p.Key == "Precision");

                results[2].Key.Should().Be("SUPLA_HDC1080");
                results[2].Parameters.Should().BeEmpty();
            }
        }
    }
}
