using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace CompilationLib.Tests
{
    /// <summary>
    /// Tests for sensitive data filtering in BuildConfigurationHasher
    /// Uses ONLY real flag names and parameters from builder.json
    /// Main test subject: SUPLA_INITIAL_CONFIG_MODE with sensitive parameters (WIFISsid, WIFIPass, Email, Password)
    /// </summary>
    public class SensitiveDataFilteringTests
    {
        #region IsSensitiveFlag Tests - Real Flags Only

        [Fact]
        public void IsSensitiveFlag_WithPasswordTypeParameter_ReturnsTrue()
        {
            // Arrange - Real SUPLA_INITIAL_CONFIG_MODE flag from builder.json
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "Password",
                        Name = "GUI Password",
                        Type = "password"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Real flag with password type parameter should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithEmailTypeParameter_ReturnsTrue()
        {
            // Arrange - Real SUPLA_INITIAL_CONFIG_MODE flag from builder.json
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "Email",
                        Name = "Supla Account Email",
                        Type = "email"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Real flag with email type parameter should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithWifiSsidParameter_ReturnsTrue()
        {
            // Arrange - Real SUPLA_INITIAL_CONFIG_MODE flag from builder.json with WIFISsid parameter
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "WIFISsid",
                        Name = "WIFI SSID",
                        Type = "string"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Real flag with WIFISsid parameter should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithWifiPasswordParameter_ReturnsTrue()
        {
            // Arrange - Real SUPLA_INITIAL_CONFIG_MODE flag from builder.json with WIFIPass parameter
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "WIFIPass",
                        Name = "WIFI Password",
                        Type = "string"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Real flag with WIFIPass parameter should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithEmailParameter_ReturnsTrue()
        {
            // Arrange - Real example from builder.json SUPLA_INITIAL_CONFIG_MODE
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "Email",
                        Name = "Supla Account Email Address",
                        Type = "string"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Flag with Email parameter should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithPasswordParameter_ReturnsTrue()
        {
            // Arrange - Real SUPLA_INITIAL_CONFIG_MODE flag from builder.json
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "Password",
                        Name = "GUI Password",
                        Type = "string"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Real flag with Password parameter should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithMultipleSensitiveParameters_ReturnsTrue()
        {
            // Arrange - Complete SUPLA_INITIAL_CONFIG_MODE flag with all sensitive parameters from builder.json
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "WIFISsid", Name = "WIFI SSID", Type = "string" },
                    new Parameter { Key = "WIFIPass", Name = "WIFI Password", Type = "string" },
                    new Parameter { Key = "Email", Name = "Supla Account Email", Type = "string" },
                    new Parameter { Key = "Password", Name = "GUI Password", Type = "string" },
                    new Parameter { Key = "Login", Name = "GUI Login", Type = "string" },
                    new Parameter { Key = "Server", Name = "Supla Server Address", Type = "string" }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "Real flag with multiple sensitive parameters should be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithNonSensitiveFlag_ReturnsFalse()
        {
            // Arrange - Real non-sensitive flag from builder.json
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_RELAY",
                FlagName = "Relays",
                Parameters = new List<Parameter>
                {
                    new Parameter
                    {
                        Key = "Pin",
                        Name = "Relay Pin",
                        Type = "number"
                    }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.False(result, "Non-sensitive flag should not be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithNullFlag_ReturnsFalse()
        {
            // Arrange
            BuildFlagItem flag = null;

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.False(result, "Null flag should return false");
        }

        [Fact]
        public void IsSensitiveFlag_WithEmptyParameters_ReturnsFalse()
        {
            // Arrange - Real SUPLA_BUTTON flag with no parameters
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_BUTTON",
                FlagName = "Button",
                Parameters = new List<Parameter>()
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.False(result, "Flag with no parameters should not be marked as sensitive");
        }

        [Fact]
        public void IsSensitiveFlag_WithInitialConfigModeFlag_ReturnsTrue()
        {
            // Arrange - Real SUPLA_INITIAL_CONFIG_MODE flag from builder.json (the main real-world case)
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_INITIAL_CONFIG_MODE",
                FlagName = "Initial Configuration Mode",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "WIFISsid", Type = "string" },
                    new Parameter { Key = "WIFIPass", Type = "string" }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.True(result, "SUPLA_INITIAL_CONFIG_MODE should be marked as sensitive due to WiFi parameters");
        }

        #endregion

        #region GetSensitiveFlags Tests - Real Flags Only

        [Fact]
        public void GetSensitiveFlags_WithMixedFlags_ReturnsOnlySensitive()
        {
            // Arrange - Mix of real flags from builder.json
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_INITIAL_CONFIG_MODE",
                    FlagName = "Initial Configuration Mode",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "WIFISsid", Type = "string" },
                        new Parameter { Key = "WIFIPass", Type = "string" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_RELAY",
                    FlagName = "Relays",
                    Parameters = new List<Parameter>()
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_BUTTON",
                    FlagName = "Button"
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_LED",
                    FlagName = "LED"
                }
            };

            // Act
            var sensitiveFlags = BuildConfigurationHasher.GetSensitiveFlags(flags);

            // Assert
            Assert.Single(sensitiveFlags);
            Assert.Contains(sensitiveFlags, f => f.Key == "SUPLA_INITIAL_CONFIG_MODE");
            Assert.DoesNotContain(sensitiveFlags, f => f.Key == "SUPLA_RELAY");
            Assert.DoesNotContain(sensitiveFlags, f => f.Key == "SUPLA_BUTTON");
            Assert.DoesNotContain(sensitiveFlags, f => f.Key == "SUPLA_LED");
        }

        [Fact]
        public void GetSensitiveFlags_WithNullInput_ReturnsEmptyList()
        {
            // Arrange
            IEnumerable<BuildFlagItem> flags = null;

            // Act
            var result = BuildConfigurationHasher.GetSensitiveFlags(flags);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetSensitiveFlags_WithEmptyList_ReturnsEmptyList()
        {
            // Arrange
            var flags = new List<BuildFlagItem>();

            // Act
            var result = BuildConfigurationHasher.GetSensitiveFlags(flags);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetSensitiveFlags_WithAllNonSensitive_ReturnsEmptyList()
        {
            // Arrange - Real non-sensitive flags from builder.json
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_RELAY" },
                new BuildFlagItem { Key = "SUPLA_BUTTON" },
                new BuildFlagItem { Key = "SUPLA_LED" }
            };

            // Act
            var result = BuildConfigurationHasher.GetSensitiveFlags(flags);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region EncodeOptionsWithoutSensitiveData Tests - Real Flags Only

        [Fact]
        public void EncodeOptionsWithoutSensitiveData_WithSensitiveFlags_ExcludesThem()
        {
            // Arrange - Real scenario with SUPLA flags from builder.json
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_RELAY",
                    IsEnabled = true
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_INITIAL_CONFIG_MODE",
                    IsEnabled = true,
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "WIFISsid", Value = "MyNetwork" },
                        new Parameter { Key = "WIFIPass", Value = "SecretPassword123" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_BUTTON",
                    IsEnabled = true
                }
            };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);

            // Assert
            Assert.NotEmpty(encoded);
            
            // Decode and verify
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);
            Assert.NotNull(decoded);
            Assert.Equal(2, decoded.Length);
            Assert.Contains("SUPLA_RELAY", decoded);
            Assert.Contains("SUPLA_BUTTON", decoded);
            Assert.DoesNotContain("SUPLA_INITIAL_CONFIG_MODE", decoded);
        }

        [Fact]
        public void EncodeOptionsWithoutSensitiveData_WithOnlyNonSensitive_EncodesAll()
        {
            // Arrange - Real non-sensitive flags from builder.json
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_RELAY" },
                new BuildFlagItem { Key = "SUPLA_BUTTON" },
                new BuildFlagItem { Key = "SUPLA_LED" },
                new BuildFlagItem { Key = "SUPLA_DHT22" }
            };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);

            // Assert
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);
            Assert.Equal(4, decoded.Length);
            Assert.Contains("SUPLA_RELAY", decoded);
            Assert.Contains("SUPLA_BUTTON", decoded);
            Assert.Contains("SUPLA_LED", decoded);
            Assert.Contains("SUPLA_DHT22", decoded);
        }

        [Fact]
        public void EncodeOptionsWithoutSensitiveData_WithOnlySensitive_ReturnsEmptyString()
        {
            // Arrange - Only SUPLA_INITIAL_CONFIG_MODE (real sensitive flag)
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_INITIAL_CONFIG_MODE",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "WIFISsid", Value = "MyNetwork" },
                        new Parameter { Key = "WIFIPass", Value = "MyPassword" }
                    }
                }
            };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);

            // Assert
            Assert.Empty(encoded);
        }

        [Fact]
        public void EncodeOptionsWithoutSensitiveData_WithNullInput_ReturnsEmptyString()
        {
            // Arrange
            IEnumerable<BuildFlagItem> flags = null;

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);

            // Assert
            Assert.Empty(encoded);
        }

        [Fact]
        public void EncodeOptionsWithoutSensitiveData_WithEmptyList_ReturnsEmptyString()
        {
            // Arrange
            var flags = new List<BuildFlagItem>();

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);

            // Assert
            Assert.Empty(encoded);
        }

        #endregion

        #region Integration Tests - Real World Scenarios

        [Fact]
        public void FullScenario_ConfigWithWifiAndEmail_OnlySafeDataEncoded()
        {
            // Arrange - Complete real-world scenario from builder.json with SUPLA_INITIAL_CONFIG_MODE
            var allFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_INITIAL_CONFIG_MODE",
                    FlagName = "Initial Configuration Mode",
                    Parameters = new List<Parameter>
                    {
                        new Parameter
                        {
                            Key = "WIFISsid",
                            Name = "WIFI SSID",
                            Type = "string",
                            Value = "MyHomeNetwork"
                        },
                        new Parameter
                        {
                            Key = "WIFIPass",
                            Name = "WIFI Password",
                            Type = "string",
                            Value = "SuperSecret123!"
                        },
                        new Parameter
                        {
                            Key = "Email",
                            Name = "Supla Account Email",
                            Type = "string",
                            Value = "user@example.com"
                        },
                        new Parameter
                        {
                            Key = "Password",
                            Name = "GUI Password",
                            Type = "string",
                            Value = "admin123"
                        },
                        new Parameter
                        {
                            Key = "Login",
                            Name = "GUI Login",
                            Type = "string",
                            Value = "admin"
                        },
                        new Parameter
                        {
                            Key = "Server",
                            Name = "Supla Server Address",
                            Type = "string",
                            Value = "svr.supla.org"
                        }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_RELAY",
                    FlagName = "Relays",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "Pin", Value = "5" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_BUTTON",
                    FlagName = "Button",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "Pin", Value = "13" }
                    }
                },
                new BuildFlagItem
                {
                    Key = "SUPLA_DHT22",
                    FlagName = "DHT22 Sensor",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "Pin", Value = "4" }
                    }
                }
            };

            // Act - Get sensitive flags
            var sensitiveFlags = BuildConfigurationHasher.GetSensitiveFlags(allFlags);
            
            // Act - Encode without sensitive data
            var safeEncoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(allFlags);
            
            // Act - Decode the safe string
            var decodedSafe = BuildConfigurationHasher.DecodeOptions(safeEncoded);

            // Assert - Verify sensitive flags were identified
            Assert.Single(sensitiveFlags);
            Assert.Equal("SUPLA_INITIAL_CONFIG_MODE", sensitiveFlags[0].Key);

            // Assert - Verify safe encoded string doesn't contain sensitive data
            Assert.NotEmpty(safeEncoded);
            Assert.Equal(3, decodedSafe.Length);
            Assert.Contains("SUPLA_RELAY", decodedSafe);
            Assert.Contains("SUPLA_BUTTON", decodedSafe);
            Assert.Contains("SUPLA_DHT22", decodedSafe);
            Assert.DoesNotContain("SUPLA_INITIAL_CONFIG_MODE", decodedSafe);
        }

        [Fact]
        public void FullScenario_CompareFullEncodingVsSafeEncoding()
        {
            // Arrange - Using real SUPLA_INITIAL_CONFIG_MODE flag
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem
                {
                    Key = "SUPLA_INITIAL_CONFIG_MODE",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "WIFISsid", Value = "MyNetwork" },
                        new Parameter { Key = "WIFIPass", Value = "MyPassword123" }
                    }
                },
                new BuildFlagItem { Key = "SUPLA_RELAY" },
                new BuildFlagItem { Key = "SUPLA_BUTTON" }
            };

            // Act - Encode with all data (for local storage)
            var fullEncoded = BuildConfigurationHasher.EncodeOptions(flags);
            
            // Act - Encode without sensitive data (for sharing)
            var safeEncoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);

            // Act - Decode both
            var fullDecoded = BuildConfigurationHasher.DecodeOptions(fullEncoded);
            var safeDecoded = BuildConfigurationHasher.DecodeOptions(safeEncoded);

            // Assert - Full encoding contains everything
            Assert.Equal(3, fullDecoded.Length);
            Assert.Contains("SUPLA_INITIAL_CONFIG_MODE", fullDecoded);
            Assert.Contains("SUPLA_RELAY", fullDecoded);
            Assert.Contains("SUPLA_BUTTON", fullDecoded);

            // Assert - Safe encoding excludes sensitive data
            Assert.Equal(2, safeDecoded.Length);
            Assert.DoesNotContain("SUPLA_INITIAL_CONFIG_MODE", safeDecoded);
            Assert.Contains("SUPLA_RELAY", safeDecoded);
            Assert.Contains("SUPLA_BUTTON", safeDecoded);

            // Assert - Encoded strings are different
            Assert.NotEqual(fullEncoded, safeEncoded);
        }

        #endregion

        #region Edge Cases - Real Flags Only

        [Fact]
        public void IsSensitiveFlag_WithMultipleNonSensitiveParameters_ReturnsFalse()
        {
            // Arrange - Real SUPLA_RELAY flag with non-sensitive parameters
            var flag = new BuildFlagItem
            {
                Key = "SUPLA_RELAY",
                Parameters = new List<Parameter>
                {
                    new Parameter { Key = "Pin", Type = "number" },
                    new Parameter { Key = "Level", Type = "number" },
                    new Parameter { Key = "Name", Type = "string" }
                }
            };

            // Act
            var result = BuildConfigurationHasher.IsSensitiveFlag(flag);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EncodeOptionsWithoutSensitiveData_ResultIsReversible()
        {
            // Arrange - Real flags only
            var flags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_RELAY" },
                new BuildFlagItem
                {
                    Key = "SUPLA_INITIAL_CONFIG_MODE",
                    Parameters = new List<Parameter>
                    {
                        new Parameter { Key = "WIFISsid", Value = "test" }
                    }
                },
                new BuildFlagItem { Key = "SUPLA_BUTTON" }
            };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptionsWithoutSensitiveData(flags);
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);
            var reEncoded = BuildConfigurationHasher.EncodeOptions(decoded);
            var reDecoded = BuildConfigurationHasher.DecodeOptions(reEncoded);

            // Assert - Verify round-trip produces same result (only non-sensitive flags)
            Assert.Equal(decoded.Length, reDecoded.Length);
            Assert.Equal(decoded.OrderBy(x => x), reDecoded.OrderBy(x => x));
            Assert.Equal(2, decoded.Length); // Only SUPLA_RELAY and SUPLA_BUTTON
        }

        #endregion
    }
}

