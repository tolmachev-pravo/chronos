namespace Pet.Jira.Infrastructure.Jira
{
    /// <summary>
    /// Tempo Timesheets integration settings (issue #245).
    /// </summary>
    public class TempoConfiguration
    {
        /// <summary>
        /// When enabled (the default), actual worklogs are retrieved via the date-filtered
        /// Tempo Timesheets REST API instead of the per-issue Jira worklog endpoint, which
        /// pulls every worklog of an issue into memory. Set to false to fall back to the
        /// Jira worklog source.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
