using CompilationLib;
using Xunit;
using System.Collections.Generic;

namespace CompilationLib.Tests
{
    public class BuildConfigurationHasherTests
    {
        [Fact]
        public void CalculateHash_WithNullArray_ReturnsEmptyString()
        {
            // Arrange
            string[]? options = null;

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
            IEnumerable<BuildFlagItem>? buildFlags = null;

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
    }
}
