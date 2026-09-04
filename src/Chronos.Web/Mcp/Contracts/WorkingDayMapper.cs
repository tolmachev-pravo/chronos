using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// Turns a working day into the shape a client is given: events on one side, worklogs on
    /// the other, tied by an id where Chronos believes one was logged for the other.
    ///
    /// The day model itself still keeps both in a single list of rows with an Estimated or
    /// Actual type — a shape the page needs and a client does not. Splitting it back apart
    /// happens here, and nothing of the model travels further than this mapper: it holds UI
    /// state and points back at its own day.
    /// </summary>
    public static class WorkingDayMapper
    {
        public static IReadOnlyList<WorkingDayView> ToViews(IEnumerable<WorkingDay> days)
        {
            return days
                .Select(ToView)
                .ToList();
        }

        public static WorkingDayView ToView(WorkingDay day)
        {
            var (events, eventIdsByRow, eventIdsByLoggedWorklog) = MapEvents(day);

            return new WorkingDayView(
                Date: day.Date,
                IsWeekend: day.IsWeekend,
                PlannedMinutes: Minutes(day.Settings.WorkingTime),
                LoggedMinutes: Minutes(day.ActualWorklogTimeSpent),
                SuggestedMinutes: Minutes(day.EstimatedWorklogTimeSpent),
                BlockedMinutes: Minutes(day.BlockedEventsTime),
                ProgressPercent: day.Progress,
                Events: events,
                Worklogs: day.ActualWorklogs
                    .Select(worklog => ToView(worklog, EventIdOf(worklog, eventIdsByRow, eventIdsByLoggedWorklog)))
                    .ToList());
        }

        /// <summary>
        /// Both kinds of event in one list: those Chronos could tie to an issue, which reached
        /// the day as estimated rows, and those it could not, which the day keeps aside as
        /// events blocking its time. Ids are handed out along the way, and both maps are keyed
        /// by reference — two events at the same time are still two events.
        /// </summary>
        private static (
            IReadOnlyList<EventView> Events,
            Dictionary<object, string> ByRow,
            Dictionary<object, string> ByLoggedWorklog) MapEvents(WorkingDay day)
        {
            var events = new List<EventView>();
            var byRow = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
            var byLoggedWorklog = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

            foreach (var row in day.EstimatedWorklogs)
            {
                var id = NextId(events.Count);
                byRow[row] = id;
                events.Add(new EventView(
                    Id: id,
                    Source: Name(row.Source),
                    IssueKey: row.Issue?.Key,
                    Summary: row.Issue?.Summary,
                    StartedAt: row.StartDate,
                    Minutes: Minutes(row.TimeSpent),
                    // Zero here does not mean an idle event: it means the day found the time
                    // already logged, and the worklog that covers it points back at this id.
                    SuggestedMinutes: Minutes(row.RemainingTimeSpent)));
            }

            foreach (var blockedEvent in day.BlockedEvents)
            {
                var id = NextId(events.Count);
                events.Add(new EventView(
                    Id: id,
                    Source: Name(blockedEvent.Source),
                    IssueKey: null,
                    Summary: blockedEvent.Summary,
                    StartedAt: blockedEvent.StartDate,
                    Minutes: Minutes(blockedEvent.Duration),
                    // Nothing can be suggested for an event with no issue: there is nothing to
                    // log it against until somebody names one.
                    SuggestedMinutes: 0));

                var loggedWorklog = day.GetLoggedWorklog(blockedEvent);
                if (loggedWorklog is not null)
                {
                    byLoggedWorklog[loggedWorklog] = id;
                }
            }

            return (events, byRow, byLoggedWorklog);
        }

        /// <summary>
        /// Which event a worklog was logged for. A keyless event claims its worklog outright —
        /// the two match to the minute — and everything else comes from the day's matching by
        /// issue key and interval, which is what made the worklog a child of its row.
        /// </summary>
        private static string EventIdOf(
            WorkingDayWorklog worklog,
            Dictionary<object, string> byRow,
            Dictionary<object, string> byLoggedWorklog)
        {
            if (byLoggedWorklog.TryGetValue(worklog, out var claimedEventId))
            {
                return claimedEventId;
            }

            return worklog.Parent is not null && byRow.TryGetValue(worklog.Parent, out var parentEventId)
                ? parentEventId
                : null;
        }

        public static WorklogView ToView(WorkingDayWorklog worklog, string eventId)
        {
            return new WorklogView(
                IssueKey: worklog.Issue?.Key,
                Summary: worklog.Issue?.Summary,
                Link: worklog.Issue?.Link,
                StartedAt: worklog.StartDate,
                Minutes: Minutes(worklog.TimeSpent),
                Comment: worklog.Comment,
                EventId: eventId);
        }

        private static string NextId(int index) => $"e{index + 1}";

        private static string Name(EventSource? source) => source?.ToString().ToLowerInvariant();

        private static int Minutes(TimeSpan time) => (int)Math.Round(time.TotalMinutes);
    }
}
