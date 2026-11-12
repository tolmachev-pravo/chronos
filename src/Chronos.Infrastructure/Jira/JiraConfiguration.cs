using System;

namespace Chronos.Infrastructure.Jira
{
    public class JiraConfiguration : IJiraConfiguration
    {
        public string Url { get; set; }

        public string ApiUrl { get; set; }

        public string ResolvedApiUrl => string.IsNullOrWhiteSpace(ApiUrl) ? Url : ApiUrl;

        public string ToApiUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(Url))
            {
                return url;
            }

            var publicBase = Url.TrimEnd('/');
            if (!url.StartsWith(publicBase, StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var rest = url.Substring(publicBase.Length);
            if (rest.Length > 0 && rest[0] != '/' && rest[0] != '?')
            {
                // https://jira.test2/... is another host, not a path under https://jira.test.
                return url;
            }

            return ApiUrl.TrimEnd('/') + rest;
        }

        public string[] CachedIssues { get; set; } = Array.Empty<string>();
        public TempoConfiguration Tempo { get; set; } = new TempoConfiguration();
        public ScriptRunnerConfiguration ScriptRunner { get; set; } = new ScriptRunnerConfiguration();
    }
}
