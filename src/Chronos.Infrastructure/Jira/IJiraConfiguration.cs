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
        string[] CachedIssues { get; set; }
        TempoConfiguration Tempo { get; set; }
        ScriptRunnerConfiguration ScriptRunner { get; set; }
    }
}