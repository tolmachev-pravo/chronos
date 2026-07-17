namespace Pet.Jira.Web.Components.Releases
{
    /// <summary>
    /// Configuration for the GitHub-backed Releases section (bound from the <c>GitHub:Releases</c> section).
    /// </summary>
    public sealed class ReleaseOptions
    {
        public const string SectionName = "GitHub:Releases";

        /// <summary>
        /// Repository in <c>owner/name</c> form whose releases are displayed.
        /// </summary>
        public string Repository { get; set; } = "tolmachev-pravo/pet-jira-copilot";

        /// <summary>
        /// Maximum number of releases to show.
        /// </summary>
        public int MaxCount { get; set; } = 20;

        /// <summary>
        /// How long a successful response is served from cache before GitHub is queried again.
        /// </summary>
        public int CacheMinutes { get; set; } = 10;

        /// <summary>
        /// Extra attempts on top of the first when a request fails with a transient network error
        /// (the corporate proxy occasionally resets the TLS handshake to api.github.com).
        /// </summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>
        /// Per-request timeout in seconds, so a stalled proxy cannot hang the Releases page.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;
    }
}
