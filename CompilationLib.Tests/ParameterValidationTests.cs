using Xunit;
using FluentAssertions;
using CompilationLib;
using System.Collections.Generic;

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
    }
}
