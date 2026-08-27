using Chronos.Domain.Models.Abstract;
using System;

namespace Chronos.Application.Worklogs.Dto
{
    /// <summary>
    /// Legacy: the worklog filter no longer stores anything. Everything it used to ask for
    /// moved to settings of its own - the working day to the profile (issue #241), the
    /// comment duration to the Jira extension (issue #242). The type is kept so the values
    /// users answered before the move can be read from local storage once and migrated by
    /// EnsureUserSettings and EnsureJiraExtension.
    /// </summary>
    public class UserWorklogFilter : IEntity<string>
    {
        public string Username { get; set; }
        public TimeSpan? DailyWorkingStartTime { get; set; }
        public TimeSpan? DailyWorkingEndTime { get; set; }
        public TimeSpan? CommentWorklogTime { get; set; } = TimeSpan.Zero;
        public TimeSpan? LunchTime { get; set; } = TimeSpan.FromHours(1);

        public string Key => Username;
    }
}
