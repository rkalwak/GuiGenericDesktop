using System.IO.Compression;

namespace CompilationLib
{
    /// <summary>
    /// Downloads public GitHub repository archive (zip) and extracts it to a destination folder.
    /// </summary>
    public class GitHubRepoDownloader
    {
        private readonly HttpClient _httpClient;

        public GitHubRepoDownloader()
        {
            _httpClient = new HttpClient();
            // GitHub requires a User-Agent header for API requests
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubRepoDownloader/1.0");
        }

        /// <summary>
        /// Downloads and extracts a public GitHub repository. Returns the path to the extracted repo folder.
        /// </summary>
        /// <param name="owner">Repository owner or organization.</param>
        /// <param name="repo">Repository name.</param>
        /// <param name="destinationRoot">Directory where the repository content will be extracted. The method creates a subfolder.</param>
        /// <param name="destinationSubdir">Subdirectory name inside destinationRoot where the repo will be placed.</param>
        /// <param name="branch">Optional branch or tag to download. If null, the repository's default branch is used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<string> DownloadRepositoryAsync(string owner, string repo, string destinationRoot, string destinationSubdir, string branch, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("owner is required", nameof(owner));
            if (string.IsNullOrWhiteSpace(repo)) throw new ArgumentException("repo is required", nameof(repo));
            if (string.IsNullOrWhiteSpace(destinationRoot)) throw new ArgumentException("destinationRoot is required", nameof(destinationRoot));
            if (string.IsNullOrWhiteSpace(branch)) throw new ArgumentException("branch is required", nameof(branch));


            // Ensure destination root exists
            Directory.CreateDirectory(destinationRoot);

            // Download zipball from GitHub API
            string zipUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball/{Uri.EscapeDataString(branch)}";

            string tempZip = Path.Combine(Path.GetTempPath(), $"{repo}.zip");

            try
            {
                using (var resp = await _httpClient.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    resp.EnsureSuccessStatusCode();

                    using (var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                    using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                    }
                }

                if (Directory.Exists(destinationRoot))
                {
                    Directory.Delete(destinationRoot, true);
                }
                Directory.CreateDirectory(destinationRoot);
                ZipFile.ExtractToDirectory(tempZip, destinationRoot, overwriteFiles: true);
                var childRepo = Directory.GetDirectories(destinationRoot);
                if (childRepo.Length == 1)
                {
                    Directory.Move(childRepo[0], Path.Combine(destinationRoot, destinationSubdir));
                    return childRepo[0];
                }

                return Path.Combine(destinationRoot, destinationSubdir);
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { /* swallow */ }
            }
        }
    }
}
