using Newtonsoft.Json;

namespace Pet.Jira.Infrastructure.Jira.Dto
{
    /// <summary>
    /// Subset of GET /rest/api/2/serverInfo. Used to learn the Jira server's UTC
    /// offset so naive Tempo worklog timestamps can be interpreted correctly. See #245.
    /// </summary>
    public class ServerInfoDto
    {
        /// <summary>Current server time with offset, e.g. "2026-07-14T18:00:00.000+0300".</summary>
        [JsonProperty("serverTime")]
        public string ServerTime { get; set; }
    }
}
