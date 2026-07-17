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
    }
}
