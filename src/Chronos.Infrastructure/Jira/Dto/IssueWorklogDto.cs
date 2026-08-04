using Chronos.Application.Time;
using Chronos.Domain.Models.Worklogs;
using System;

namespace Chronos.Infrastructure.Jira.Dto
{
    public class IssueWorklogDto
    {
        public DateTime? StartDate { get; set; }
        public long TimeSpentInSeconds { get; set; }
        public IssueDto Issue { get; set; }

        public TimeSpan TimeSpent => TimeSpan.FromSeconds(TimeSpentInSeconds);
        public DateTime? EndDate => StartDate != null ? StartDate.Value.AddSeconds(TimeSpentInSeconds) : default;
        
        public IssueWorklog Adapt(ITimeProvider timeProvider, TimeZoneInfo userTimeZone)
        {
            return new IssueWorklog
            {
                StartDate = timeProvider.ConvertToUserTimezone(StartDate.Value, userTimeZone),
                TimeSpent = TimeSpent,
                CompleteDate = timeProvider.ConvertToUserTimezone(EndDate.Value, userTimeZone),
                Issue = Issue.Adapt()
            };
        }

        public static IssueWorklogDto Create(
            Atlassian.Jira.Worklog worklog,
            IssueDto issue)
        {
            return new IssueWorklogDto
            {
                StartDate = worklog.StartDate,
                TimeSpentInSeconds = worklog.TimeSpentInSeconds,
                Issue = issue
            };
        }
    }
}
