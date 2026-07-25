using CompilationLib.GithubInteractions;
using FluentAssertions;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        private const string GitHubOwner = "lsroka76";
        private const string GitHubRepo = "Z2S_Library";

        // Minimum expected firmware size: 512 KB
        private const long MinFirmwareSizeBytes = 512 * 1024;
        // Maximum expected firmware size: 8 MB (the target flash chip size)
        private const long MaxFirmwareSizeBytes = 8 * 1024 * 1024;

        private static bool ShouldSkipNetworkTests =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SKIP_Z2S_INTEGRATION_TESTS"));

        // ── Firmware filename generation ──────────────────────────────────────

        [Theory]
        [InlineData(true, true, "8MB", "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(true, false, "8MB", "Z2S_Gateway.8MB.OTA.logs.update_only.bin")]
        [InlineData(false, true, "8MB", "Z2S_Gateway.8MB.OTA.no_logs.full_version.bin")]
        [InlineData(false, false, "8MB", "Z2S_Gateway.8MB.OTA.no_logs.update_only.bin")]
        [InlineData(true, true, "4MB", "Z2S_Gateway.4MB.no_OTA.logs.full_version.WARNING_NEW_SIZE.bin")]
        [InlineData(true, false, "4MB", "Z2S_Gateway.4MB.no_OTA.logs.update_only.WARNING_NEW_SIZE.bin")]
        [InlineData(false, false, "4MB", "Z2S_Gateway.4MB.no_OTA.no_logs.update_only.WARNING_NEW_SIZE.bin")]
        [InlineData(true, true, "16MB", "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(false, false, "16MB", "Z2S_Gateway.8MB.OTA.no_logs.update_only.bin")]
        [InlineData(true, true, "32MB", "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(true, true, "", "Z2S_Gateway.8MB.OTA.logs.full_version.bin")]
        [InlineData(false, false, null, "Z2S_Gateway.8MB.OTA.no_logs.update_only.bin")]
        public void GetFirmwareFileName_ReturnsExpectedName(bool withLogs, bool fullVersion, string flashSize, string expected)
        {
            PartitionManager.GetZigbeeFirmwareFileName(withLogs, fullVersion, flashSize).Should().Be(expected);
        }

        [Theory]
        [InlineData("4MB", "4MB")]
        [InlineData("8MB", "8MB")]
        [InlineData("16MB", "8MB")]
        [InlineData("32MB", "8MB")]
        [InlineData("", "8MB")]
        [InlineData(null, "8MB")]
        public void NormalizeFirmwareFlashSize_ReturnsExpectedSize(string input, string expected)
        {
            PartitionManager.NormalizeFirmwareFlashSize(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void GetFirmwareFileName_HasDotBinExtension(bool withLogs, bool fullVersion)
        {
            PartitionManager.GetZigbeeFirmwareFileName(withLogs, fullVersion, "8MB").Should().EndWith(".bin");
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

            GitHubReleasesClient gitHubReleasesClient = new GitHubReleasesClient(null, Log.ForContext<GitHubReleasesClient>());
            var releases = await gitHubReleasesClient.GetLatestStableReleasesAsync(GitHubOwner, GitHubRepo, 1);
            var release = releases.FirstOrDefault();
            if (release == null) return; // network unavailable — skip gracefully

            release.Assets.Should().NotBeEmpty($"release {release.TagName} must contain firmware assets");

            foreach (bool withLogs in new[] { true, false })
                foreach (bool fullVersion in new[] { true, false })
                {
                    var fileName = PartitionManager.GetZigbeeFirmwareFileName(withLogs, fullVersion, "8MB");
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
            GitHubReleasesClient gitHubReleasesClient = new GitHubReleasesClient(null, Log.ForContext<GitHubReleasesClient>());
            var releases = await gitHubReleasesClient.GetLatestStableReleasesAsync(GitHubOwner, GitHubRepo, 1);
            var release = releases.FirstOrDefault();
         
            if (release == null) return;

            var fileName = PartitionManager.GetZigbeeFirmwareFileName(withLogs: false, fullVersion: false, flashSize: "8MB");
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
    }
}
