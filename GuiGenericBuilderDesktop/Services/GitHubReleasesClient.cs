using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace GuiGenericBuilderDesktop.Services
{
    public class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class GitHubRelease
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = [];
    }

    /// <summary>
    /// Thin wrapper around the GitHub REST Releases API.
    /// Uses a single HTTP call to retrieve the latest N stable releases,
    /// explicitly requesting newest-first ordering.
    /// </summary>
    public class GitHubReleasesClient
    {
        private readonly HttpClient _http;
        private readonly ILogger _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GitHubReleasesClient(string? pat, ILogger logger)
        {
            _logger = logger;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GuiGenericBuilderDesktop", "1.0"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            if (!string.IsNullOrWhiteSpace(pat))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        }

        /// <summary>
        /// Returns the <paramref name="count"/> most recent stable (non-draft, non-prerelease) releases
        /// from the given repository in a single API call.
        /// </summary>
        public async Task<IReadOnlyList<GitHubRelease>> GetLatestStableReleasesAsync(
            string owner,
            string repo,
            int count = 15,
            CancellationToken cancellationToken = default)
        {
            // GitHub REST API supports per_page up to 100 and always returns newest-first
            // when direction is not specified, but we make it explicit with direction=desc.
            var perPage = Math.Clamp(count * 2, count, 100);
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page={perPage}&direction=desc";

            _logger.Information("Fetching releases from {Url}", url);

            var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var releases = await JsonSerializer.DeserializeAsync<List<GitHubRelease>>(stream, JsonOptions, cancellationToken)
                           ?? [];

            _logger.Information("GitHub returned {Total} releases (raw) for {Owner}/{Repo}", releases.Count, owner, repo);
            foreach (var r in releases)
                _logger.Debug("  [{Date}] {Tag} draft={Draft} prerelease={Pre}",
                    (r.PublishedAt ?? r.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"), r.TagName, r.Draft, r.Prerelease);

            var result = releases
                .Where(r => !r.Draft && !r.Prerelease)
                .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                .Take(count)
                .ToList();

            _logger.Information("After filter+sort, returning {Count} stable releases:", result.Count);
            foreach (var r in result)
                _logger.Information("  [{Date}] {Tag}", (r.PublishedAt ?? r.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss"), r.TagName);

            return result;
        }
    }
}
