namespace Chronos.Infrastructure.Jira
{
    /// <summary>
    /// ScriptRunner addon integration settings (issue #259).
    /// </summary>
    public class ScriptRunnerConfiguration
    {
        /// <summary>
        /// When enabled (the default), the comment events are searched with the
        /// ScriptRunner "commented" issue function, which filters by comment author and
        /// period on the Jira side instead of scanning every issue updated since the
        /// start of the period. On an instance without the addon the search fails and
        /// the plain JQL is used instead, so leaving this on is safe; set it to false to
        /// skip the failing attempt altogether.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
