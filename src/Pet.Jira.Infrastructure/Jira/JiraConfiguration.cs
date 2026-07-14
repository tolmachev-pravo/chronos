using System;

namespace Pet.Jira.Infrastructure.Jira
{
    public class JiraConfiguration : IJiraConfiguration
    {
        public string Url { get; set; }
        public string[] CachedIssues { get; set; } = Array.Empty<string>();
        public TempoConfiguration Tempo { get; set; } = new TempoConfiguration();
    }
}
