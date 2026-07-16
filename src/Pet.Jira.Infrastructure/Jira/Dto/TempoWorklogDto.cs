using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Pet.Jira.Infrastructure.Jira.Dto
{
    /// <summary>
    /// Worklog record returned by the Tempo Timesheets Server/DC REST API
    /// (GET /rest/tempo-timesheets/3/worklogs?dateFrom=&amp;dateTo=). See issue #245.
    /// Shape confirmed against the live instance (jira.parcsis.org, Tempo v3).
    /// </summary>
    public class TempoWorklogDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("jiraWorklogId")]
        public long JiraWorklogId { get; set; }

        [JsonProperty("timeSpentSeconds")]
        public long TimeSpentInSeconds { get; set; }

        /// <summary>Worklog start as a local ISO datetime, e.g. "2026-07-13T17:25:00".</summary>
        [JsonProperty("dateStarted")]
        public string DateStarted { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("issue")]
        public TempoIssue Issue { get; set; }

        [JsonProperty("author")]
        public TempoAuthor Author { get; set; }

        /// <summary>
        /// Parses <see cref="DateStarted"/> into a local <see cref="DateTime"/>.
        /// Parsed manually (not via the serializer) to avoid timezone normalization.
        /// Returns null when the value is missing or unparsable.
        /// </summary>
        public DateTime? GetStartDateTime()
        {
            if (string.IsNullOrWhiteSpace(DateStarted))
            {
                return null;
            }

            return DateTime.TryParse(
                DateStarted,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result)
                ? result
                : null;
        }
    }

    public class TempoIssue
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }
    }

    public class TempoAuthor
    {
        /// <summary>Machine login (Jira username), e.g. "d.tolmachev".</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Jira user key; equals the username on this instance.</summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>Machine login regardless of which field the instance populates.</summary>
        public string Login => !string.IsNullOrWhiteSpace(Name) ? Name : Key;
    }
}
