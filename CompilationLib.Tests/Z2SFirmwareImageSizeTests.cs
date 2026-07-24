using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CompilationLib.Tests
{
    /// <summary>
    /// Integration tests that verify the Z2S firmware asset naming convention
    /// and that released binary files have a plausible size (not zero / not corrupt).
    ///
    /// Set the SKIP_Z2S_INTEGRATION_TESTS environment variable to any value to skip
    /// the network-dependent tests in CI environments without internet access.
    /// </summary>
    public class Z2SFirmwareImageSizeTests
    {
        private const string GitHubOwner = "rkalwak";
        private const string GitHubRepo  = "Z2S_Library";

        // Minimum expected firmware size: 512 KB
        private const long MinFirmwareSizeBytes = 512 * 1024;
        // Maximum expected firmware size: 8 MB (the target flash chip size)
        private const long MaxFirmwareSizeBytes = 8 * 1024 * 1024;

        private static bool ShouldSkipNetworkTests =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SKIP_Z2S_INTEGRATION_TESTS"));

        // ── Firmware filename generation ──────────────────────────────────────

        [Theory]
        [InlineData(true,  true,  "8MB",  "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(true,  false, "8MB",  "Z2S_Gateway.8MB.OTA.logs.update_only.bin")]
        [InlineData(false, true,  "8MB",  "Z2S_Gateway.8MB.OTA.no_logs.full_version.bin")]
        [InlineData(false, false, "8MB",  "Z2S_Gateway.8MB.OTA.no_logs.update_only.bin")]
        [InlineData(true,  true,  "4MB",  "Z2S_Gateway.4MB.OTA.logs.full_version.bin")]
        [InlineData(false, false, "4MB",  "Z2S_Gateway.4MB.OTA.no_logs.update_only.bin")]
        [InlineData(true,  true,  "16MB", "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(false, false, "16MB", "Z2S_Gateway.8MB.OTA.no_logs.update_only.bin")]
        [InlineData(true,  true,  "32MB", "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(true,  true,  "",     "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(false, false, null,   "Z2S_Gateway.8MB.OTA.no_logs.update_only.bin")]
        public void GetFirmwareFileName_ReturnsExpectedName(bool withLogs, bool fullVersion, string flashSize, string expected)
        {
            GetFirmwareFileName(withLogs, fullVersion, flashSize).Should().Be(expected);
        }

        [Theory]
        [InlineData("4MB",  "4MB")]
        [InlineData("8MB",  "8MB")]
        [InlineData("16MB", "8MB")]
        [InlineData("32MB", "8MB")]
        [InlineData("",     "8MB")]
        [InlineData(null,   "8MB")]
        public void NormalizeFirmwareFlashSize_ReturnsExpectedSize(string input, string expected)
        {
            NormalizeFirmwareFlashSize(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(true,  true)]
        [InlineData(true,  false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void GetFirmwareFileName_HasDotBinExtension(bool withLogs, bool fullVersion)
        {
            GetFirmwareFileName(withLogs, fullVersion, "8MB").Should().EndWith(".bin");
        }

        // ── Integration: GitHub asset metadata size check ─────────────────────

        /// <summary>
        /// Reads the latest Z2S release asset metadata from the GitHub API and asserts
        /// that each expected .bin file has a declared size within the plausible range.
        /// No full download is performed — only the size field from the API is checked.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public async Task LatestRelease_AllFirmwareAssets_HavePlausibleSizeInMetadata()
        {
            if (ShouldSkipNetworkTests) return;

            var release = await FetchLatestReleaseAsync();
            if (release == null) return; // network unavailable — skip gracefully

            release.Assets.Should().NotBeEmpty($"release {release.TagName} must contain firmware assets");

            foreach (bool withLogs in new[] { true, false })
            foreach (bool fullVersion in new[] { true, false })
            {
                var fileName = GetFirmwareFileName(withLogs, fullVersion, "8MB");
                var asset = release.Assets.Find(a =>
                    string.Equals(a.Name, fileName, StringComparison.OrdinalIgnoreCase));

                asset.Should().NotBeNull($"asset '{fileName}' should exist in release {release.TagName}");
                asset!.Size.Should().BeGreaterThanOrEqualTo(MinFirmwareSizeBytes,
                    $"firmware '{fileName}' must be at least {MinFirmwareSizeBytes / 1024} KB");
                asset.Size.Should().BeLessThanOrEqualTo(MaxFirmwareSizeBytes,
                    $"firmware '{fileName}' must fit within {MaxFirmwareSizeBytes / 1024 / 1024} MB flash");
            }
        }

        /// <summary>
        /// Downloads the smallest firmware variant (update_only, no_logs) and verifies
        /// that the number of downloaded bytes matches the size reported by the GitHub API.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public async Task LatestRelease_UpdateOnlyNoLogsAsset_DownloadedSizeMatchesMetadata()
        {
            if (ShouldSkipNetworkTests) return;

            var release = await FetchLatestReleaseAsync();
            if (release == null) return;

            var fileName = GetFirmwareFileName(withLogs: false, fullVersion: false, flashSize: "8MB");
            var asset = release.Assets.Find(a =>
                string.Equals(a.Name, fileName, StringComparison.OrdinalIgnoreCase));

            if (asset == null) return; // asset not published yet — skip

            var tempFile = Path.Combine(Path.GetTempPath(), $"z2s_test_{Guid.NewGuid():N}.bin");
            try
            {
                using var http = CreateHttpClient();
                var bytes = await http.GetByteArrayAsync(asset.BrowserDownloadUrl);
                await File.WriteAllBytesAsync(tempFile, bytes);

                bytes.LongLength.Should().Be(asset.Size,
                    $"downloaded byte count of '{fileName}' must match GitHub API reported size");

                bytes.LongLength.Should().BeGreaterThanOrEqualTo(MinFirmwareSizeBytes,
                    $"downloaded '{fileName}' must be at least {MinFirmwareSizeBytes / 1024} KB");

                bytes.LongLength.Should().BeLessThanOrEqualTo(MaxFirmwareSizeBytes,
                    $"downloaded '{fileName}' must fit within {MaxFirmwareSizeBytes / 1024 / 1024} MB flash");
            }
            finally
            {
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Mirrors Z2SUpdateService.GetFirmwareFileName — kept in sync manually.
        /// Pattern: Z2S_Gateway.{firmwareSize}.OTA.{logs|no_logs}.{full_version|update_only}.bin
        /// </summary>
        private static string GetFirmwareFileName(bool withLogs, bool fullVersion, string flashSize = "8MB")
        {
            var sizePart    = NormalizeFirmwareFlashSize(flashSize);
            var logsPart    = withLogs    ? "logs"         : "no_logs";
            var versionPart = fullVersion ? "full_version" : "update_only";
            return $"Z2S_Gateway.{sizePart}.OTA.{logsPart}.{versionPart}.bin";
        }

        private static string NormalizeFirmwareFlashSize(string flashSize)
        {
            if (string.IsNullOrWhiteSpace(flashSize))
                return "8MB";
            return flashSize.Trim().ToUpperInvariant() switch
            {
                "4MB" => "4MB",
                _     => "8MB",
            };
        }

        private static HttpClient CreateHttpClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("GuiGenericBuilderDesktop-Test", "1.0"));
            http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            return http;
        }

        private static async Task<Z2SReleaseInfo> FetchLatestReleaseAsync()
        {
            try
            {
                using var http = CreateHttpClient();
                var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases?per_page=10&direction=desc";
                var response = await http.GetAsync(url, CancellationToken.None);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                var releases = await JsonSerializer.DeserializeAsync<List<Z2SReleaseInfo>>(stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return releases?.Find(r => !r.Draft && !r.Prerelease);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private sealed class Z2SReleaseInfo
        {
            [JsonPropertyName("tag_name")]   public string TagName    { get; set; }
            [JsonPropertyName("draft")]      public bool   Draft      { get; set; }
            [JsonPropertyName("prerelease")] public bool   Prerelease { get; set; }
            [JsonPropertyName("assets")]     public List<Z2SAssetInfo> Assets { get; set; } = [];
        }

        private sealed class Z2SAssetInfo
        {
            [JsonPropertyName("name")]                 public string Name               { get; set; }
            [JsonPropertyName("size")]                 public long   Size               { get; set; }
            [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; }
        }
    }
}
