using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// Turns a working day into the shape a client is given. The day model is built for the
    /// page — it holds UI state, points back at its own day and knows about events — so
    /// nothing of it travels further than this mapper.
    /// </summary>
    public static class WorkingDayMapper
    {
        private const string LoggedSource = "logged";

        public static IReadOnlyList<WorkingDayView> ToViews(IEnumerable<WorkingDay> days)
        {
            return days
                .Select(ToView)
                .ToList();
        }

        public static WorkingDayView ToView(WorkingDay day)
        {
            return new WorkingDayView(
                Date: day.Date,
                IsWeekend: day.IsWeekend,
                PlannedMinutes: Minutes(day.Settings.WorkingTime),
                LoggedMinutes: Minutes(day.ActualWorklogTimeSpent),
                SuggestedMinutes: Minutes(day.EstimatedWorklogTimeSpent),
                BlockedMinutes: Minutes(day.BlockedEventsTime),
                ProgressPercent: day.Progress,
                Logged: day.ActualWorklogs.Select(ToView).ToList(),
                // A suggestion of zero is one that the day already found logged: showing it
                // would invite the client to log the same time twice.
                Suggested: day.EstimatedWorklogs
                    .Where(worklog => worklog.RemainingTimeSpent > TimeSpan.Zero)
                    .Select(ToView)
                    .ToList(),
                // An event with no issue never becomes a suggestion — there is nothing to log
                // it against. Reporting only its minutes would leave the client wondering
                // where the day went, so it is named: the client can ask which issue it
                // belongs to and log it with add_worklog. Once it is logged it stops
                // blocking and appears among the logged rows instead.
                Blocked: day.BlockedEvents
                    .Where(blockedEvent => !day.IsEventLogged(blockedEvent))
                    .Select(ToView)
                    .ToList());
        }

        public static WorklogView ToView(WorkingDayWorklog worklog)
        {
            return new WorklogView(
                IssueKey: worklog.Issue?.Key,
                Summary: worklog.Issue?.Summary,
                Link: worklog.Issue?.Link,
                StartedAt: worklog.StartDate,
                Minutes: Minutes(worklog.Type == WorklogType.Actual
                    ? worklog.TimeSpent
                    : worklog.RemainingTimeSpent),
                Comment: worklog.Comment,
                Source: worklog.Source?.ToString().ToLowerInvariant() ?? LoggedSource);
        }

        public static BlockedEventView ToView(IEvent blockedEvent)
        {
            return new BlockedEventView(
                Summary: blockedEvent.Summary,
                StartedAt: blockedEvent.StartDate,
                Minutes: Minutes(blockedEvent.Duration),
                Source: blockedEvent.Source.ToString().ToLowerInvariant());
        }

        private static int Minutes(TimeSpan time) => (int)Math.Round(time.TotalMinutes);
    }
}
