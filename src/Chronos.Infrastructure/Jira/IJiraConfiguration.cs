namespace Chronos.Infrastructure.Jira
{
    public interface IJiraConfiguration
    {
        /// <summary>
        /// Public Jira address. Users' browsers follow links built from it.
        /// </summary>
        string Url { get; set; }

        /// <summary>
        /// Address the server calls the Jira API on, when it differs from <see cref="Url"/>:
        /// for example an internal host, when the public one is not reachable from the server. Optional.
        /// </summary>
        string ApiUrl { get; set; }

        /// <summary>
        /// <see cref="ApiUrl"/> when set, otherwise <see cref="Url"/>. Use it for every server-side request.
        /// </summary>
        string ResolvedApiUrl { get; }

        /// <summary>
        /// Moves an absolute link Jira returned (self, avatar) from the public <see cref="Url"/>
        /// onto <see cref="ApiUrl"/>, so the server follows it on the address it can reach.
        /// Any other link, or every link when <see cref="ApiUrl"/> is not set, comes back as is.
        /// </summary>
        string ToApiUrl(string url);
        string[] CachedIssues { get; set; }
        TempoConfiguration Tempo { get; set; }
        ScriptRunnerConfiguration ScriptRunner { get; set; }
    }
}