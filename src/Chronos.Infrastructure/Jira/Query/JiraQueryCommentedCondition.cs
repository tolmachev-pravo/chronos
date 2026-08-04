using System;
using System.Globalization;

namespace Chronos.Infrastructure.Jira.Query
{
    /// <summary>
    /// The ScriptRunner "commented" issue function, which narrows a search down to the
    /// issues commented by an author within a period. Plain JQL cannot express this, so
    /// the comment events had to scan every recently updated issue instead. See issue #259.
    ///
    /// Renders as: issueFunction in commented("by currentUser() after 2026/07/01 before 2026/07/15").
    /// Both bounds are exclusive midnights, so <see cref="Before"/> must already account
    /// for the last day of the requested period.
    /// </summary>
    public class JiraQueryCommentedCondition
    {
        public JiraQueryMacros Author { get; set; }
        public DateTime After { get; set; }
        public DateTime Before { get; set; }

        public override string ToString()
        {
            var format = JiraQueryConstants.Date.DefaultFormat;
            var author = JiraQueryConstants.Macroses[Author];
            var after = After.ToString(format, CultureInfo.InvariantCulture);
            var before = Before.ToString(format, CultureInfo.InvariantCulture);
            return $"issueFunction in commented(\"by {author} after {after} before {before}\")";
        }
    }
}
