using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Components.Releases
{
    /// <summary>
    /// Fetches releases from the public GitHub Releases API.
    /// </summary>
    public sealed class GitHubReleaseService : IReleaseService
    {
        private readonly HttpClient _httpClient;
        private readonly ReleaseOptions _options;

        public GitHubReleaseService(
            HttpClient httpClient,
            IOptions<ReleaseOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<ReleaseSummary>> GetReleasesAsync(CancellationToken cancellationToken = default)
        {
            // GitHub REST API: https://docs.github.com/en/rest/releases/releases#list-releases
            var perPage = Math.Clamp(_options.MaxCount, 1, 100);
            var requestUri = $"repos/{_options.Repository}/releases?per_page={perPage}";

            var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(requestUri, cancellationToken)
                           ?? new List<GitHubRelease>();

            return releases
                .Where(release => !release.Draft)
                .OrderByDescending(release => release.PublishedAt ?? release.CreatedAt ?? DateTimeOffset.MinValue)
                .Take(_options.MaxCount)
                .Select(Map)
                .ToList();
        }

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
