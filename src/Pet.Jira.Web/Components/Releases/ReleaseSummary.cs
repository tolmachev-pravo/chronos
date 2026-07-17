using System;

namespace Pet.Jira.Web.Components.Releases
{
    /// <summary>
    /// A single application release fetched from the GitHub Releases API.
    /// </summary>
    public sealed class ReleaseSummary
    {
        /// <summary>
        /// Git tag the release points at (e.g. <c>v1.4.0</c>).
        /// </summary>
        public string TagName { get; init; }

        /// <summary>
        /// Human-readable release title. Falls back to the tag when GitHub returns an empty name.
        /// </summary>
        public string Title { get; init; }

        /// <summary>
        /// Release notes in Markdown.
        /// </summary>
        public string BodyMarkdown { get; init; }

        /// <summary>
        /// Link to the release page on GitHub.
        /// </summary>
        public string HtmlUrl { get; init; }

        /// <summary>
        /// When the release was published (or created, for unpublished ones).
        /// </summary>
        public DateTimeOffset? PublishedAt { get; init; }

        /// <summary>
        /// Whether GitHub flagged the release as a pre-release.
        /// </summary>
        public bool IsPrerelease { get; init; }
    }
}
