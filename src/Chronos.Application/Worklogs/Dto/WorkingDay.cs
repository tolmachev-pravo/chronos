using Chronos.Application.Common.Extensions;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Chronos.Application.Worklogs.Dto
{
    public class WorkingDay
    {
        /// <summary>
        /// Date of day without time
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Settings of working day
        /// </summary>
        [Required]
        public WorkingDaySettings Settings { get; set; }

        /// <summary>
        /// Worklog items
        /// </summary>
        public IList<WorkingDayWorklog> Worklogs { get; set; }

        public WorkingDay(
            DateTime date,
            WorkingDaySettings settings,
            IList<WorkingDayWorklog>? worklogs = null)
        {
            Date = date;
            Settings = settings;
            Worklogs = worklogs ?? new List<WorkingDayWorklog>();
        }

        /// <summary>
        /// Determines that day is weekend
        /// </summary>
        public bool IsWeekend => Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday;

        /// <summary>
        /// Actual worklogs.
        /// Include manual and auto worklogs
        /// </summary>
        public IEnumerable<WorkingDayWorklog> ActualWorklogs => 
            Worklogs.Where(item => item.Type == WorklogType.Actual);

        /// <summary>
        /// Estimated worklogs
        /// </summary>
        public IEnumerable<WorkingDayWorklog> EstimatedWorklogs => 
            Worklogs.Where(item => item.Type == WorklogType.Estimated);

        /// <summary>
        /// Actual worklog time spent
        /// </summary>
        public TimeSpan ActualWorklogTimeSpent => ActualWorklogs.TimeSpent();

        /// <summary>
        /// Estimated worklog time spent
        /// </summary>
        public TimeSpan EstimatedWorklogTimeSpent => EstimatedWorklogs.RemainingTimeSpent() + BlockedEventsTime;

        /// <summary>
        /// Worklog time spent
        /// </summary>
        public TimeSpan WorklogTimeSpent => ActualWorklogTimeSpent + EstimatedWorklogTimeSpent;

        /// <summary>
        /// Percent of progress time spent by day
        /// </summary>
        public int Progress => WorklogTimeSpent > TimeSpan.Zero
            ? Convert.ToInt32(ActualWorklogTimeSpent * 100 / WorklogTimeSpent)
            : 0;

        public int RawEstimatedWorklogCount => EstimatedWorklogs.Count(item => item.RemainingTimeSpent > TimeSpan.Zero);
        public bool HasRawEstimatedWorklogs => RawEstimatedWorklogCount > 0;

        /// <summary>
        /// Time blocked by events without an issue that are not yet logged — subtracted from
        /// available day time during distribution. Logged events (matched by exact start/end to an
        /// actual worklog) do not block, since their time is already accounted for.
        /// </summary>
        public TimeSpan BlockedEventsTime => BlockedEvents
            .Where(calendarEvent => !IsEventLogged(calendarEvent))
            .Aggregate(TimeSpan.Zero, (acc, calendarEvent) => acc + calendarEvent.Duration);

        /// <summary>
        /// True when the event has already been logged — an actual worklog matches its
        /// start and end exactly. Filled in by <see cref="Refresh"/>.
        /// </summary>
        public bool IsEventLogged(IEvent calendarEvent) =>
            GetLoggedWorklog(calendarEvent) is not null;

        /// <summary>
        /// The worklog the event was logged as, or null while it is not logged.
        /// </summary>
        public WorkingDayWorklog GetLoggedWorklog(IEvent calendarEvent) =>
            calendarEvent is not null && _loggedEventWorklogs.TryGetValue(calendarEvent, out var worklog)
                ? worklog
                : null;

        /// <summary>
        /// Worklogs already claimed by a keyless event: they belong to that event's row and
        /// to no other.
        /// </summary>
        public IReadOnlyCollection<WorkingDayWorklog> LoggedEventWorklogs => _loggedEventWorklogs.Values;

        // Keyed by reference: two events with the same time are still two events, and each
        // of them may have a worklog of its own.
        private readonly Dictionary<IEvent, WorkingDayWorklog> _loggedEventWorklogs =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Events with no issue of their own — shown for context; they block time until the
        /// user logs them against a task they pick. See issue #299.
        /// </summary>
        public IReadOnlyList<IEvent> BlockedEvents { get; set; } = new List<IEvent>();

        public void Refresh()
        {
            ClaimLoggedEventWorklogs();

            // A worklog claimed by a keyless event is left out of the matching: it is shown
            // under that event, and matching it by issue key as well would put the same time
            // entry under two rows.
            WorklogMatching.Match(
                parents: EstimatedWorklogs,
                children: ActualWorklogs.Except(_loggedEventWorklogs.Values));

            var unmatchedEstimated = EstimatedWorklogs
                .Where(worklog => worklog.ChildrenTimeSpent == TimeSpan.Zero)
                .ToList();

            // Calendar and Comment events use their own time directly — not scaled to day capacity
            var fixedTimeUnmatched = unmatchedEstimated
                .Where(w => w.Source == EventSource.Calendar || w.Source == EventSource.Comment)
                .ToList();

            // Assignee and Tester events are scaled proportionally from remaining day time
            var proportionalUnmatched = unmatchedEstimated
                .Where(w => w.Source != EventSource.Calendar && w.Source != EventSource.Comment)
                .ToList();

            foreach (var estimatedWorklog in EstimatedWorklogs.Where(w => w.ChildrenTimeSpent > TimeSpan.Zero))
            {
                estimatedWorklog.UpdateRemainingTimeSpent(TimeSpan.Zero);
            }

            foreach (var worklog in fixedTimeUnmatched)
            {
                worklog.UpdateRemainingTimeSpent(worklog.RawTimeSpent);
            }

            var remainingWorklogTimeSpent = proportionalUnmatched
                .Select(w => w.TimeSpent)
                .Sum();

            // Fixed events and blocked events reduce the pool available for proportional events.
            // Raw time is used for fixed events — they may fall outside working hours but still consume time.
            var fixedRawTimeSpent = fixedTimeUnmatched.Select(w => w.RawTimeSpent).Sum();
            var remainingDayTimeSpent = Settings.WorkingTime
                - ActualWorklogs.TimeSpent()
                - BlockedEventsTime
                - fixedRawTimeSpent;

            foreach (var estimatedWorklog in proportionalUnmatched)
            {
                if (remainingWorklogTimeSpent > TimeSpan.Zero && remainingDayTimeSpent > TimeSpan.Zero)
                {
                    var percent = estimatedWorklog.TimeSpent / remainingWorklogTimeSpent;
                    var estimatedTimeSpent = percent * remainingDayTimeSpent;
                    estimatedWorklog.UpdateRemainingTimeSpent(estimatedTimeSpent);
                }
                else
                {
                    estimatedWorklog.UpdateRemainingTimeSpent(TimeSpan.Zero);
                }
            }
        }

        /// <summary>
        /// Hands every keyless event the worklog it was logged as — the one whose start and
        /// end match it exactly. An event claims at most one worklog and a worklog is claimed
        /// by at most one event, so two events at the same time cannot show the same entry.
        /// </summary>
        private void ClaimLoggedEventWorklogs()
        {
            _loggedEventWorklogs.Clear();

            if (BlockedEvents.IsEmpty())
                return;

            var unclaimed = ActualWorklogs.ToList();

            foreach (var calendarEvent in BlockedEvents)
            {
                var index = unclaimed.FindIndex(worklog =>
                    worklog.StartDate == calendarEvent.StartDate
                    && worklog.CompleteDate == calendarEvent.CompleteDate);

                if (index < 0)
                    continue;

                var claimed = unclaimed[index];
                unclaimed.RemoveAt(index);

                // Drop the tie to an estimated worklog a previous refresh may have made.
                claimed.Parent?.Children.Remove(claimed);
                claimed.Parent = null;

                _loggedEventWorklogs[calendarEvent] = claimed;
            }
        }

        public void AddWorklog(WorkingDayWorklog worklog)
        {
            Worklogs.Add(worklog);
            Refresh();
        }
    }
}
