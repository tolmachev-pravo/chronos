using Chronos.Domain.Models.Abstract;
using System;

namespace Chronos.Application.Worklogs.Dto
{
    public class UserWorklogFilter : IEntity<string>
    {
        public string Username { get; set; }
        public TimeSpan? DailyWorkingStartTime { get; set; }
        public TimeSpan? DailyWorkingEndTime { get; set; }

        /// <summary>
        /// Legacy: the comment duration moved to the Jira extension (issue #242). Kept so
        /// EnsureJiraExtension can migrate the value stored before the move.
        /// </summary>
        public TimeSpan? CommentWorklogTime { get; set; } = TimeSpan.Zero;

        public TimeSpan? LunchTime { get; set; } = TimeSpan.FromHours(1);

        public string Key => Username;
    }
}
