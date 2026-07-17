using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Components.Releases
{
    /// <summary>
    /// Fetches releases from the public GitHub Releases API with caching and transient-fault handling.
    /// </summary>
    public sealed class GitHubReleaseService : IReleaseService
    {
        private const string FreshCacheKey = "releases:fresh";
        private const string LastKnownGoodCacheKey = "releases:last-known-good";

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GitHubReleaseService> _logger;
        private readonly ReleaseOptions _options;

        public GitHubReleaseService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<GitHubReleaseService> logger,
            IOptions<ReleaseOptions> options)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<ReleasesResult> GetReleasesAsync(CancellationToken cancellationToken = default)
        {
            // Public releases page the user can always open in a browser as a fallback.
            var releasesUrl = $"https://github.com/{_options.Repository}/releases";

            if (_cache.TryGetValue(FreshCacheKey, out IReadOnlyList<ReleaseSummary> fresh))
            {
                return new ReleasesResult { Releases = fresh, ReleasesUrl = releasesUrl };
            }

            try
            {
                var releases = await FetchWithRetryAsync(cancellationToken);
                CacheReleases(releases);
                return new ReleasesResult { Releases = releases, ReleasesUrl = releasesUrl };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Corporate proxies/firewalls occasionally reset the TLS handshake to api.github.com.
                // Never fail the page: serve the last releases we managed to load if we have them,
                // otherwise report the failure so the UI can point the user at GitHub directly.
                if (_cache.TryGetValue(LastKnownGoodCacheKey, out IReadOnlyList<ReleaseSummary> lastGood))
                {
                    _logger.LogWarning(exception,
                        "Could not refresh GitHub releases; serving {Count} cached release(s).", lastGood.Count);
                    return new ReleasesResult { Releases = lastGood, LoadFailed = true, ReleasesUrl = releasesUrl };
                }

                if (IsNetworkFailure(exception))
                {
                    _logger.LogWarning(exception,
                        "Could not reach GitHub releases API; offering the user a direct link.");
                }
                else
                {
                    _logger.LogError(exception,
                        "Unexpected error loading GitHub releases; offering the user a direct link.");
                }

                return new ReleasesResult { LoadFailed = true, ReleasesUrl = releasesUrl };
            }
        }

        private async Task<IReadOnlyList<ReleaseSummary>> FetchWithRetryAsync(CancellationToken cancellationToken)
        {
            var perPage = Math.Clamp(_options.MaxCount, 1, 100);
            var requestUri = $"repos/{_options.Repository}/releases?per_page={perPage}";
            var attempts = Math.Max(1, _options.MaxRetries + 1);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(requestUri, cancellationToken)
                                   ?? new List<GitHubRelease>();

                    return releases
                        .Where(release => !release.Draft)
                        .OrderByDescending(release => release.PublishedAt ?? release.CreatedAt ?? DateTimeOffset.MinValue)
                        .Take(_options.MaxCount)
                        .Select(Map)
                        .ToList();
                }
                catch (Exception exception)
                    when (attempt < attempts && !cancellationToken.IsCancellationRequested && IsRetriable(exception))
                {
                    var delay = TimeSpan.FromMilliseconds(300 * attempt);
                    _logger.LogDebug(exception,
                        "GitHub releases request failed (attempt {Attempt}/{Attempts}); retrying in {Delay}ms.",
                        attempt, attempts, delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        private void CacheReleases(IReadOnlyList<ReleaseSummary> releases)
        {
            var freshFor = TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes));
            _cache.Set(FreshCacheKey, releases, freshFor);
            // No expiry: a fallback to show while GitHub is unreachable.
            _cache.Set(LastKnownGoodCacheKey, releases);
        }

        /// <summary>Any network/HTTP failure worth degrading gracefully for (rather than throwing).</summary>
        private static bool IsNetworkFailure(Exception exception) =>
            exception is HttpRequestException or IOException or TaskCanceledException;

        /// <summary>A transient failure worth retrying: connection resets, timeouts, 5xx, throttling.</summary>
        private static bool IsRetriable(Exception exception) => exception switch
        {
            IOException => true,
            TaskCanceledException => true, // request timeout
            HttpRequestException { StatusCode: null } => true, // connection/TLS failure — no response
            HttpRequestException { StatusCode: HttpStatusCode.RequestTimeout } => true,
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => true,
            HttpRequestException { StatusCode: >= HttpStatusCode.InternalServerError } => true,
            _ => false
        };

        private static ReleaseSummary Map(GitHubRelease release) => new()
        {
            TagName = release.TagName,
            Title = string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name,
            BodyMarkdown = release.Body ?? string.Empty,
            HtmlUrl = release.HtmlUrl,
            PublishedAt = release.PublishedAt ?? release.CreatedAt,
            IsPrerelease = release.Prerelease
        };

        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; init; }

            [JsonPropertyName("name")]
            public string Name { get; init; }

            [JsonPropertyName("body")]
            public string Body { get; init; }

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; init; }

            [JsonPropertyName("published_at")]
            public DateTimeOffset? PublishedAt { get; init; }

            [JsonPropertyName("created_at")]
            public DateTimeOffset? CreatedAt { get; init; }

            [JsonPropertyName("draft")]
            public bool Draft { get; init; }

            [JsonPropertyName("prerelease")]
            public bool Prerelease { get; init; }
        }
    }
}
