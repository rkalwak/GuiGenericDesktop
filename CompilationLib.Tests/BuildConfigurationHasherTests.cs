using CompilationLib;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace CompilationLib.Tests
{
    public class BuildConfigurationHasherTests
    {
        [Fact]
        public void CalculateHash_WithNullArray_ReturnsEmptyString()
        {
            // Arrange
            string[] options = null;

            // Act
            var result = BuildConfigurationHasher.CalculateHash(options);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CalculateHash_WithEmptyArray_ReturnsEmptyString()
        {
            // Arrange
            var options = new string[0];

            // Act
            var result = BuildConfigurationHasher.CalculateHash(options);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CalculateHash_WithSingleOption_ReturnsValidHash()
        {
            // Arrange
            var options = new[] { "SUPLA_DEVICE" };

            // Act
            var result = BuildConfigurationHasher.CalculateHash(options);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length); // SHA256 produces 64 hex characters
            Assert.Matches("^[0-9a-f]{64}$", result); // Verify it's lowercase hex
        }

        [Fact]
        public void CalculateHash_WithMultipleOptions_ReturnsValidHash()
        {
            // Arrange
            var options = new[] { "SUPLA_DEVICE", "SUPLA_RELAY", "SUPLA_DHT22" };

            // Act
            var result = BuildConfigurationHasher.CalculateHash(options);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length);
            Assert.Matches("^[0-9a-f]{64}$", result);
        }

        [Fact]
        public void CalculateHash_WithSameOptionsInDifferentOrder_ReturnsSameHash()
        {
            // Arrange
            var options1 = new[] { "SUPLA_DEVICE", "SUPLA_RELAY", "SUPLA_DHT22" };
            var options2 = new[] { "SUPLA_DHT22", "SUPLA_DEVICE", "SUPLA_RELAY" };

            // Act
            var hash1 = BuildConfigurationHasher.CalculateHash(options1);
            var hash2 = BuildConfigurationHasher.CalculateHash(options2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CalculateHash_WithDifferentOptions_ReturnsDifferentHash()
        {
            // Arrange
            var options1 = new[] { "SUPLA_DEVICE", "SUPLA_RELAY" };
            var options2 = new[] { "SUPLA_DEVICE", "SUPLA_DHT22" };

            // Act
            var hash1 = BuildConfigurationHasher.CalculateHash(options1);
            var hash2 = BuildConfigurationHasher.CalculateHash(options2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void CalculateHash_IsCaseInsensitive_ReturnsSameHash()
        {
            // Arrange
            var options1 = new[] { "SUPLA_DEVICE", "supla_relay" };
            var options2 = new[] { "supla_device", "SUPLA_RELAY" };

            // Act
            var hash1 = BuildConfigurationHasher.CalculateHash(options1);
            var hash2 = BuildConfigurationHasher.CalculateHash(options2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CalculateHash_WithBuildFlagItems_ReturnsValidHash()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_DEVICE" },
                new BuildFlagItem { Key = "SUPLA_RELAY" },
                new BuildFlagItem { Key = "SUPLA_DHT22" }
            };

            // Act
            var result = BuildConfigurationHasher.CalculateHash(buildFlags);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length);
            Assert.Matches("^[0-9a-f]{64}$", result);
        }

        [Fact]
        public void CalculateHash_WithNullBuildFlagItems_ReturnsEmptyString()
        {
            // Arrange
            IEnumerable<BuildFlagItem> buildFlags = null;

            // Act
            var result = BuildConfigurationHasher.CalculateHash(buildFlags);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CalculateHash_WithEmptyBuildFlagItems_ReturnsEmptyString()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>();

            // Act
            var result = BuildConfigurationHasher.CalculateHash(buildFlags);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void CalculateHash_WithBuildFlagItemsContainingNulls_IgnoresNulls()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_DEVICE" },
                null,
                new BuildFlagItem { Key = "SUPLA_RELAY" }
            };

            // Act
            var result = BuildConfigurationHasher.CalculateHash(buildFlags);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length);
        }

        [Fact]
        public void CalculateHash_WithBuildFlagItemsContainingEmptyKeys_IgnoresEmpty()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_DEVICE" },
                new BuildFlagItem { Key = "" },
                new BuildFlagItem { Key = "SUPLA_RELAY" }
            };

            // Act
            var result = BuildConfigurationHasher.CalculateHash(buildFlags);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length);
        }

        [Fact]
        public void CalculateHash_ProducesDeterministicResults()
        {
            // Arrange
            var options = new[] { "SUPLA_DEVICE", "SUPLA_RELAY", "SUPLA_DHT22" };

            // Act
            var hash1 = BuildConfigurationHasher.CalculateHash(options);
            var hash2 = BuildConfigurationHasher.CalculateHash(options);
            var hash3 = BuildConfigurationHasher.CalculateHash(options);

            // Assert
            Assert.Equal(hash1, hash2);
            Assert.Equal(hash2, hash3);
        }

        [Fact]
        public void CalculateHash_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var options = new[] { "SUPLA_DEVICE_V2", "SUPLA_RELAY-1", "SUPLA_DHT22.5" };

            // Act
            var result = BuildConfigurationHasher.CalculateHash(options);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(64, result.Length);
            Assert.Matches("^[0-9a-f]{64}$", result);
        }

        #region Encoding/Decoding Tests

        [Fact]
        public void EncodeOptions_WithValidOptions_ReturnsEncodedString()
        {
            // Arrange
            var options = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22" };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(options);

            // Assert
            Assert.NotEmpty(encoded);
            Assert.DoesNotContain("+", encoded); // URL-safe check
            Assert.DoesNotContain("/", encoded); // URL-safe check
            Assert.DoesNotContain("=", encoded); // No padding
        }

        [Fact]
        public void DecodeOptions_WithValidEncoding_ReturnsOriginalOptions()
        {
            // Arrange
            var originalOptions = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22" };
            var encoded = BuildConfigurationHasher.EncodeOptions(originalOptions);

            // Act
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal(3, decoded.Length);
            Assert.Contains("SUPLA_CONFIG", decoded);
            Assert.Contains("SUPLA_RELAY", decoded);
            Assert.Contains("SUPLA_DHT22", decoded);
        }

        [Fact]
        public void EncodeAndDecode_RoundTrip_PreservesData()
        {
            // Arrange
            var originalOptions = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22", "SUPLA_RGBW" };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(originalOptions);
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal(originalOptions.Length, decoded.Length);
            
            // Sort both arrays for comparison (encoding normalizes order and case to uppercase)
            var sortedOriginal = originalOptions.Select(s => s.ToUpperInvariant()).OrderBy(s => s).ToArray();
            var sortedDecoded = decoded.OrderBy(s => s).ToArray();
            
            Assert.Equal(sortedOriginal, sortedDecoded);
        }

        [Fact]
        public void EncodeOptions_WithLargeConfiguration_CompressesEffectively()
        {
            // Arrange
            var options = Enumerable.Range(0, 100)
                .Select(i => $"SUPLA_FLAG_{i}")
                .ToArray();
            
            var uncompressedSize = string.Join("|", options).Length;

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(options);

            // Assert
            Assert.NotEmpty(encoded);
            // Compressed + base64 should be smaller than raw data for repetitive content
            Assert.True(encoded.Length < uncompressedSize);
        }

        [Fact]
        public void DecodeOptions_WithInvalidEncoding_ReturnsNull()
        {
            // Arrange
            var invalidEncoded = "this-is-not-valid-base64-encoding!!!";

            // Act
            var decoded = BuildConfigurationHasher.DecodeOptions(invalidEncoded);

            // Assert
            Assert.Null(decoded);
        }

        [Fact]
        public void DecodeOptions_WithNullOrEmpty_ReturnsNull()
        {
            // Act & Assert
            Assert.Null(BuildConfigurationHasher.DecodeOptions(null));
            Assert.Null(BuildConfigurationHasher.DecodeOptions(""));
            Assert.Null(BuildConfigurationHasher.DecodeOptions(string.Empty));
        }

        [Fact]
        public void EncodeOptions_WithNullOptions_ReturnsEmpty()
        {
            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions((string[])null);

            // Assert
            Assert.Empty(encoded);
        }

        [Fact]
        public void EncodeOptions_WithEmptyOptions_ReturnsEmpty()
        {
            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(new string[0]);

            // Assert
            Assert.Empty(encoded);
        }

        [Fact]
        public void EncodeOptions_WithBuildFlagItems_ReturnsValidEncoding()
        {
            // Arrange
            var buildFlags = new List<BuildFlagItem>
            {
                new BuildFlagItem { Key = "SUPLA_CONFIG" },
                new BuildFlagItem { Key = "SUPLA_RELAY" }
            };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(buildFlags);

            // Assert
            Assert.NotEmpty(encoded);
        }

        [Fact]
        public void EncodeOptions_IsDeterministic_SameInputProducesSameOutput()
        {
            // Arrange
            var options = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22" };

            // Act
            var encoded1 = BuildConfigurationHasher.EncodeOptions(options);
            var encoded2 = BuildConfigurationHasher.EncodeOptions(options);

            // Assert
            Assert.Equal(encoded1, encoded2);
        }

        [Fact]
        public void EncodeOptions_WithDifferentOrder_ProducesSameEncoding()
        {
            // Arrange
            var options1 = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22" };
            var options2 = new[] { "SUPLA_DHT22", "SUPLA_CONFIG", "SUPLA_RELAY" };

            // Act
            var encoded1 = BuildConfigurationHasher.EncodeOptions(options1);
            var encoded2 = BuildConfigurationHasher.EncodeOptions(options2);

            // Assert
            Assert.Equal(encoded1, encoded2);
        }

        [Fact]
        public void EncodeOptions_WithDifferentCase_ProducesSameEncoding()
        {
            // Arrange
            var options1 = new[] { "SUPLA_CONFIG", "SUPLA_RELAY" };
            var options2 = new[] { "supla_config", "supla_relay" };

            // Act
            var encoded1 = BuildConfigurationHasher.EncodeOptions(options1);
            var encoded2 = BuildConfigurationHasher.EncodeOptions(options2);

            // Assert
            Assert.Equal(encoded1, encoded2);
        }

        [Fact]
        public void HashAndEncode_ProduceDifferentOutputs()
        {
            // Arrange
            var options = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22" };

            // Act
            var hash = BuildConfigurationHasher.CalculateHash(options);
            var encoded = BuildConfigurationHasher.EncodeOptions(options);

            // Assert
            Assert.NotEqual(hash, encoded);
            Assert.Equal(64, hash.Length); // SHA256 is always 64 characters
            Assert.NotEqual(64, encoded.Length); // Encoding length varies
        }

        [Fact]
        public void EncodeAndDecode_WithSpecialCharacters_PreservesData()
        {
            // Arrange
            var options = new[] { "SUPLA_CONFIG", "FLAG_WITH_UNDERSCORE", "flag-with-dash" };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(options);
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Contains("SUPLA_CONFIG", decoded);
            Assert.Contains("FLAG_WITH_UNDERSCORE", decoded);
            Assert.Contains("FLAG-WITH-DASH", decoded); // Normalized to uppercase
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public void EncodeAndDecode_WithVariousSizes_WorksCorrectly(int flagCount)
        {
            // Arrange
            var options = Enumerable.Range(0, flagCount)
                .Select(i => $"SUPLA_FLAG_{i}")
                .ToArray();

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(options);
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal(flagCount, decoded.Length);
        }

        [Fact]
        public void VerifyHashMatchesAfterDecoding()
        {
            // Arrange
            var originalOptions = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22" };
            var originalHash = BuildConfigurationHasher.CalculateHash(originalOptions);

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(originalOptions);
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);
            var decodedHash = BuildConfigurationHasher.CalculateHash(decoded);

            // Assert
            Assert.Equal(originalHash, decodedHash);
        }

        [Fact]
        public void EncodeOptions_ProducesUrlSafeString()
        {
            // Arrange
            var options = new[] { "SUPLA_CONFIG", "SUPLA_RELAY", "SUPLA_DHT22", "SUPLA_RGBW", "SUPLA_LED" };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(options);

            // Assert
            Assert.Matches("^[A-Za-z0-9_-]+$", encoded); // Only URL-safe characters
        }

        [Fact]
        public void EncodeAndDecode_WithEmptyStringsInArray_IgnoresEmptyStrings()
        {
            // Arrange
            var options = new[] { "SUPLA_CONFIG", "", "SUPLA_RELAY", null, "SUPLA_DHT22" };

            // Act
            var encoded = BuildConfigurationHasher.EncodeOptions(options);
            var decoded = BuildConfigurationHasher.DecodeOptions(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.DoesNotContain("", decoded);
            Assert.Contains("SUPLA_CONFIG", decoded);
            Assert.Contains("SUPLA_RELAY", decoded);
            Assert.Contains("SUPLA_DHT22", decoded);
        }

        #endregion
    }
}
