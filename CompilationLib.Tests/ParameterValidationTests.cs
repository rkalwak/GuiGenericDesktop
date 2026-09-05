using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using FluentAssertions;

namespace CompilationLib.Tests
{
    public class ParameterValidationTests
    {
        [Fact]
        public void Parameter_WithIsRequiredTrue_ShouldHaveProperty()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "TestParam",
                Type = "string",
                IsRequired = true
            };

            // Assert
            parameter.IsRequired.Should().BeTrue();
        }

        [Fact]
        public void Parameter_WithIsRequiredFalse_ShouldHaveProperty()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "TestParam",
                Type = "string",
                IsRequired = false
            };

            // Assert
            parameter.IsRequired.Should().BeFalse();
        }

        [Fact]
        public void Parameter_DefaultIsRequired_ShouldBeFalse()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "TestParam",
                Type = "string"
            };

            // Assert
            parameter.IsRequired.Should().BeFalse();
        }

        [Fact]
        public void BuildFlagItem_WithRequiredParameters_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""name"": ""Test Flag"",
                ""Parameters"": [
                    {
                        ""Name"": ""Mode"",
                        ""Type"": ""enum"",
                        ""IsRequired"": true,
                        ""DefaultValue"": ""0"",
                        ""EnumValues"": [
                            {
                                ""Value"": ""0"",
                                ""Name"": ""Option1"",
                                ""Description"": ""First option""
                            }
                        ]
                    },
                    {
                        ""Name"": ""Timeout"",
                        ""Type"": ""number"",
                        ""IsRequired"": false,
                        ""DefaultValue"": ""5""
                    }
                ]
            }";

            // Act
            var flag = Newtonsoft.Json.JsonConvert.DeserializeObject<BuildFlagItem>(json);

            // Assert
            flag.Should().NotBeNull();
            flag.Parameters.Should().HaveCount(2);
            flag.Parameters[0].IsRequired.Should().BeTrue();
            flag.Parameters[1].IsRequired.Should().BeFalse();
        }

        [Fact]
        public void Parameter_RequiredWithValue_ShouldBeValid()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "TestParam",
                Type = "string",
                IsRequired = true,
                Value = "SomeValue"
            };

            // Assert
            parameter.IsRequired.Should().BeTrue();
            parameter.Value.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void Parameter_RequiredWithoutValue_ShouldIndicateInvalid()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "TestParam",
                Type = "string",
                IsRequired = true,
                Value = null
            };

            // Assert
            parameter.IsRequired.Should().BeTrue();
            parameter.Value.Should().BeNullOrWhiteSpace();
        }

        [Fact]
        public void Parameter_NotRequiredWithoutValue_ShouldBeValid()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "TestParam",
                Type = "string",
                IsRequired = false,
                Value = null
            };

            // Assert
            parameter.IsRequired.Should().BeFalse();
            parameter.Value.Should().BeNullOrWhiteSpace();
        }

        [Fact]
        public void Parameter_WithKey_ShouldUseKeyAsIdentifier()
        {
            // Arrange
            var parameter = new Parameter
            {
                Key = "MyKey",
                Name = "My Parameter Name",
                Type = "string"
            };

            // Assert
            parameter.Identifier.Should().Be("MyKey");
        }

        [Fact]
        public void Parameter_WithoutKey_ShouldUseNameAsIdentifier()
        {
            // Arrange
            var parameter = new Parameter
            {
                Name = "MyName",
                Type = "string"
            };

            // Assert
            parameter.Identifier.Should().Be("MyName");
        }

        [Fact]
        public void Parameter_WithEmptyKey_ShouldFallBackToName()
        {
            // Arrange
            var parameter = new Parameter
            {
                Key = "",
                Name = "MyName",
                Type = "string"
            };

            // Assert
            parameter.Identifier.Should().Be("MyName");
        }

        [Fact]
        public void Parameter_WithKeyAndName_ShouldPreferKey()
        {
            // Arrange
            var parameter = new Parameter
            {
                Key = "KeyValue",
                Name = "NameValue",
                Type = "string"
            };

            // Assert
            parameter.Identifier.Should().Be("KeyValue");
        }

        [Fact]
        public void GlobalSettings_GetOptionsForBoard_ShouldUsePlatformSpecificGpioMap()
        {
            // Arrange
            var settings = new GlobalSettings
            {
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "GPIO_P_GUI_Generic_ESP32",
                        Type = "enum",
                        EnumValues = new List<EnumValue>
                        {
                            new EnumValue { Value = "-1", Name = "OFF" },
                            new EnumValue { Value = "0", Name = "0-IO" },
                            new EnumValue { Value = "4", Name = "4-IO" },
                            new EnumValue { Value = "12", Name = "12-IO" }
                        }
                    }
                }
            };

            // Act
            var result = settings.GetOptionsForBoard("GUI_Generic_ESP32");

            // Assert
            result.Should().HaveCount(4);
            result[0].Value.Should().Be(-1);
            result[0].Display.Should().Be("OFF");
            result[2].Value.Should().Be(4);
            result[2].Display.Should().Be("4-IO");
        }

        [Fact]
        public void Parameter_GpioType_ShouldPopulateBoardSpecificEnumValues_AndKeepIntegerValue()
        {
            // Arrange
            var settings = new GlobalSettings
            {
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "GPIO_P_GUI_Generic_ESP32",
                        Type = "enum",
                        EnumValues = new List<EnumValue>
                        {
                            new EnumValue { Value = "0", Name = "0-IO" },
                            new EnumValue { Value = "2", Name = "2-IO" },
                            new EnumValue { Value = "4", Name = "4-IO" }
                        }
                    }
                }
            };

            var parameter = new Parameter
            {
                Key = "GPIO1",
                Name = "GPIO 1",
                Type = "gpio",
                Value = "4"
            };

            // Act
            parameter.PopulateGpioEnumValues(settings, "GUI_Generic_ESP32");

            // Assert
            parameter.EnumValues.Should().HaveCount(3);
            parameter.EnumValues.Select(x => x.Value).Should().Contain("4");
            parameter.Value.Should().Be("4");
            int.TryParse(parameter.Value, out var gpioNumber).Should().BeTrue();
            gpioNumber.Should().Be(4);
        }

        [Fact]
        public void CompileHandler_GpioType_ShouldSerializeAsIntegerValue()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "ESP32",
                    IsEnabled = true,
                    Parameters = new List<Parameter>
                    {
                        new Parameter
                        {
                            Key = "GPIO1",
                            Name = "GPIO 1",
                            Type = "gpio",
                            Value = "12"
                        }
                    }
                }
            };

            var method = typeof(CompileHandler).GetMethod("BuildFlagsStringForCompilation", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            // Act
            var result = (string)method.Invoke(null, new object[] { buildFlags });

            // Assert
            result.Should().Contain("-D ESP32_GPIO1=12");
            result.Should().NotContain("ESP32_GPIO1=\"12\"");
            result.Should().NotContain("ESP32_GPIO1='\"12\"'");
        }

        [Fact]
        public void CompileHandler_EnumType_WithNumericValue_ShouldSerializeAsIntegerValue()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_RELAY",
                    IsEnabled = true,
                    Parameters = new List<Parameter>
                    {
                        new Parameter
                        {
                            Key = "GPIO2",
                            Name = "GPIO 2",
                            Type = "enum",
                            Value = "15"
                        }
                    }
                }
            };

            var method = typeof(CompileHandler).GetMethod("BuildFlagsStringForCompilation", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            // Act
            var result = (string)method.Invoke(null, new object[] { buildFlags });

            // Assert
            result.Should().Contain("-D SUPLA_RELAY_GPIO2=15");
            result.Should().NotContain("SUPLA_RELAY_GPIO2='\"15\"'");
        }
    }
}
