using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using System;

namespace Chronos.Application.Worklogs.Dto
{
    public class AddedWorklogDto
    {
        public DateTime StartedAt { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public string IssueKey { get; set; }
        public string Comment { get; set; }
        public IIssue Issue { get; set; }

        public static AddedWorklogDto Create(WorkingDayWorklog worklog)
        {
            return new AddedWorklogDto
            {
                StartedAt = worklog.StartDate,
                IssueKey = worklog.Issue.Key,
                ElapsedTime = worklog.RemainingTimeSpent,
                Issue = worklog.Issue,
                Comment = WorklogComment(worklog)
            };
        }

        private static string WorklogComment(WorkingDayWorklog worklog)
        {
            if (!string.IsNullOrEmpty(worklog.Comment))
            {
                return worklog.Comment;
            }

            return worklog.DefaultComment();
        }
    }
}
