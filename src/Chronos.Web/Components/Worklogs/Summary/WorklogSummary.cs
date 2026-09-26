using Chronos.Application.Common.Extensions;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Web.Components.Worklogs.Summary
{
    /// <summary>
    /// Where the time of a period went, worked out from the days already on screen: no
    /// new read from Jira or the calendar. Rebuilt whenever the days change, since a day
    /// changes in place once something is logged. See issue #173.
    /// </summary>
    public sealed class WorklogSummary
    {
        public IReadOnlyList<SummaryDay> Days { get; }

        public TimeSpan Logged { get; }

        /// <summary>
        /// What the days are short of their norm, day by day: an evening of overtime does
        /// not fill a day left empty — both still have to be logged as they are.
        /// </summary>
        public TimeSpan Shortfall { get; }

        /// <summary>Suggestions and unlogged calendar events that are still waiting to be logged.</summary>
        public TimeSpan Suggested { get; }

        public int IssueCount { get; }

        /// <summary>
        /// Share of logged time that came from calendar events, 0 to 1. The calendar holds
        /// whatever the user put there — meetings, but also blocks of work — so this is
        /// where the time was planned, not how much of it went to talking.
        /// </summary>
        public double CalendarShare { get; }

        public IReadOnlyList<SummaryIssue> Issues { get; }

        public IReadOnlyList<SummarySource> Sources { get; }

        private WorklogSummary(IReadOnlyList<SummaryDay> days, IReadOnlyList<SummaryIssue> issues)
        {
            Days = days;
            Issues = issues;
            Logged = days.Select(day => day.Logged).Sum();
            Shortfall = days.Select(day => day.Shortfall).Sum();
            Suggested = days.Select(day => day.Suggested).Sum();
            IssueCount = days.SelectMany(day => day.IssueKeys).Distinct().Count();

            Sources = days
                .SelectMany(day => day.LoggedSegments)
                .GroupBy(segment => segment.Kind)
                .Select(group => new SummarySource(group.Key, group.Select(segment => segment.Length).Sum()))
                .Where(source => source.Logged > TimeSpan.Zero)
                .OrderByDescending(source => source.Logged)
                .ToList();

            var calendar = Sources.FirstOrDefault(source => source.Kind == SummaryKind.Calendar)?.Logged ?? TimeSpan.Zero;
            CalendarShare = Logged > TimeSpan.Zero ? calendar / Logged : 0;
        }

        public static WorklogSummary Create(IEnumerable<WorkingDay> workingDays)
        {
            var days = (workingDays ?? Enumerable.Empty<WorkingDay>())
                .OrderBy(day => day.Date)
                .Select(SummaryDay.Create)
                // A weekend is left out unless something happened on it: an empty column
                // for every Saturday would only push the working days apart.
                .Where(day => !day.IsWeekend || day.Logged > TimeSpan.Zero || day.Suggested > TimeSpan.Zero)
                .ToList();

            var issues = days
                .SelectMany(day => day.IssueTime.Select(time => (Day: day, Time: time)))
                .GroupBy(entry => entry.Time.Key)
                .Select(group =>
                {
                    var byDay = group.ToDictionary(entry => entry.Day.Date, entry => entry.Time);
                    var first = group.First().Time;
                    return new SummaryIssue(
                        first.Key,
                        first.Title,
                        first.Link,
                        group.Select(entry => entry.Time.Logged).Sum(),
                        days.Select(day => byDay.TryGetValue(day.Date, out var time)
                                ? new SummaryIssueDay(day.Date, time.Logged, time.Suggested)
                                : new SummaryIssueDay(day.Date, TimeSpan.Zero, TimeSpan.Zero))
                            .ToList());
                })
                .Where(issue => issue.Logged > TimeSpan.Zero)
                .OrderByDescending(issue => issue.Logged)
                .ThenBy(issue => issue.Key)
                .ToList();

            return new WorklogSummary(days, issues);
        }

        /// <summary>The working days: what averages and the heat map are taken over.</summary>
        public IEnumerable<SummaryDay> WorkingDays => Days.Where(day => !day.IsWeekend);

        public double AverageIssueCount => WorkingDays.Any() ? WorkingDays.Average(day => day.IssueKeys.Count) : 0;

        public TimeSpan AverageCalendar => Average(WorkingDays.Select(day => day.CalendarTime));

        /// <summary>
        /// How much of each hour of the working day, weekday by weekday, was taken by
        /// calendar events or by Jira work — an average over the days of the period. Meaningful only once there
        /// are several of each weekday, which is why the week shows its days instead.
        /// </summary>
        public SummaryHeatGrid HeatMap(bool calendar)
        {
            var working = WorkingDays.ToList();
            if (working.Count == 0)
                return new SummaryHeatGrid(0, 0, new double[5, 0]);

            var firstHour = (int)Math.Floor(working.Min(day => day.WindowStart).TotalHours);
            var lastHour = (int)Math.Ceiling(working.Max(day => day.WindowEnd).TotalHours);
            var hours = Math.Max(0, lastHour - firstHour);
            var cells = new double[5, hours];

            for (var weekday = 0; weekday < 5; weekday++)
            {
                var sameWeekday = working.Where(day => day.Weekday == weekday).ToList();
                if (sameWeekday.Count == 0)
                    continue;

                for (var hour = 0; hour < hours; hour++)
                {
                    var from = TimeSpan.FromHours(firstHour + hour);
                    var to = from + TimeSpan.FromHours(1);
                    var busy = sameWeekday
                        .SelectMany(day => day.Segments)
                        .Where(segment => (segment.Kind == SummaryKind.Calendar) == calendar)
                        // Work is what was logged; a calendar event counts even before it is.
                        .Where(segment => calendar || !segment.IsSuggestion)
                        .Select(segment => Overlap(segment.Start, segment.End, from, to))
                        .Sum();
                    cells[weekday, hour] = Math.Min(1, busy / TimeSpan.FromHours(1) / sameWeekday.Count);
                }
            }

            return new SummaryHeatGrid(firstHour, hours, cells);
        }

        internal static TimeSpan Overlap(TimeSpan start, TimeSpan end, TimeSpan from, TimeSpan to)
        {
            var overlap = Min(end, to) - Max(start, from);
            return overlap > TimeSpan.Zero ? overlap : TimeSpan.Zero;
        }

        private static TimeSpan Average(IEnumerable<TimeSpan> values)
        {
            var list = values.ToList();
            return list.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((long)list.Average(value => value.Ticks));
        }

        internal static TimeSpan Min(TimeSpan left, TimeSpan right) => left < right ? left : right;

        internal static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;
    }

    /// <summary>What a stretch of the day was spent on.</summary>
    public enum SummaryKind
    {
        /// <summary>An issue the user had in progress.</summary>
        Work,
        Testing,
        Comment,

        /// <summary>A calendar event — a meeting or anything else the user put there.</summary>
        Calendar,

        /// <summary>A worklog that no event of the day explains — logged by hand.</summary>
        Manual
    }

    /// <summary>
    /// A stretch of the day on the timeline. A suggestion is something that happened and
    /// is not logged yet: a calendar event or a comment, which have a time of their own.
    /// </summary>
    public sealed record SummarySegment(TimeSpan Start, TimeSpan End, SummaryKind Kind, string Title, bool IsSuggestion)
    {
        public TimeSpan Length => End - Start;
    }

    public sealed class SummaryDay
    {
        public DateTime Date { get; private init; }
        public bool IsWeekend { get; private init; }

        /// <summary>Monday is 0.</summary>
        public int Weekday => ((int)Date.DayOfWeek + 6) % 7;

        public TimeSpan Norm { get; private init; }
        public TimeSpan Logged { get; private init; }
        public TimeSpan Suggested { get; private init; }

        public TimeSpan Shortfall => Norm > Logged ? Norm - Logged : TimeSpan.Zero;

        /// <summary>
        /// The working day, widened to whatever happened before or after it: a calendar
        /// event at nine is on the calendar all the same, and so is time logged late.
        /// </summary>
        public TimeSpan WindowStart { get; private init; }
        public TimeSpan WindowEnd { get; private init; }

        /// <summary>Stretches with a time of their own, within the calendar day, by start.</summary>
        public IReadOnlyList<SummarySegment> Segments { get; private init; }

        /// <summary>
        /// Logged time as it was logged, not cut at midnight: what the sources add up. A
        /// worklog running into the next day is still this day's worklog.
        /// </summary>
        internal IReadOnlyList<SummarySegment> LoggedSegments { get; private init; }

        public IReadOnlyCollection<string> IssueKeys { get; private init; }

        internal IReadOnlyList<IssueTime> IssueTime { get; private init; }

        public TimeSpan CalendarTime => Segments
            .Where(segment => segment.Kind == SummaryKind.Calendar)
            .Select(segment => segment.Length)
            .Sum();

        internal static SummaryDay Create(WorkingDay day)
        {
            var eventWorklogs = day.LoggedEventWorklogs.ToHashSet();

            var logged = day.ActualWorklogs
                .Select(worklog => new SummarySegment(
                    worklog.StartDate - day.Date.Date,
                    worklog.CompleteDate - day.Date.Date,
                    eventWorklogs.Contains(worklog) ? SummaryKind.Calendar : KindOf(worklog.Parent?.Source ?? worklog.Source),
                    Title(worklog),
                    IsSuggestion: false))
                .ToList();

            // Calendar events and comments keep their own time; work and testing are spread
            // over what is left of the day and have no time of their own to place. The raw
            // time is taken: the row's own is pulled inside the working day, and an event at
            // nine would be squeezed to nothing at ten.
            var suggestedEvents = day.EstimatedWorklogs
                .Where(worklog => worklog.ChildrenTimeSpent == TimeSpan.Zero && worklog.RemainingTimeSpent > TimeSpan.Zero)
                .Where(worklog => worklog.Source is EventSource.Calendar or EventSource.Comment)
                .Select(worklog => new SummarySegment(
                    worklog.RawStartDate - day.Date.Date,
                    worklog.RawCompleteDate - day.Date.Date,
                    KindOf(worklog.Source),
                    Title(worklog),
                    IsSuggestion: true));

            var blockedEvents = day.BlockedEvents
                .Where(blockedEvent => !day.IsEventLogged(blockedEvent))
                .Select(blockedEvent => new SummarySegment(
                    blockedEvent.StartDate - day.Date.Date,
                    blockedEvent.CompleteDate - day.Date.Date,
                    SummaryKind.Calendar,
                    blockedEvent.Summary,
                    IsSuggestion: true));

            var segments = logged
                .Concat(suggestedEvents)
                .Concat(blockedEvents)
                .Select(segment => segment with
                {
                    Start = WorklogSummary.Max(segment.Start, TimeSpan.Zero),
                    End = WorklogSummary.Min(segment.End, TimeSpan.FromDays(1))
                })
                .Where(segment => segment.End > segment.Start)
                .OrderBy(segment => segment.Start)
                .ToList();

            var windowStart = segments.Select(segment => segment.Start)
                .Append(day.Settings.WorkingStartTime)
                .Min();
            var windowEnd = segments.Select(segment => segment.End)
                .Append(day.Settings.WorkingEndTime)
                .Max();

            var issueTime = day.ActualWorklogs
                .Where(worklog => worklog.Issue?.Key is not null)
                .Select(worklog => (worklog.Issue, Logged: worklog.TimeSpent, Suggested: TimeSpan.Zero))
                .Concat(day.EstimatedWorklogs
                    .Where(worklog => worklog.Issue?.Key is not null)
                    .Select(worklog => (worklog.Issue, Logged: TimeSpan.Zero, Suggested: worklog.RemainingTimeSpent)))
                .GroupBy(entry => entry.Issue.Key)
                .Select(group => new IssueTime(
                    group.Key,
                    group.Select(entry => entry.Issue.Summary).FirstOrDefault(summary => summary is not null),
                    group.Select(entry => entry.Issue.Link).FirstOrDefault(link => link is not null),
                    group.Select(entry => entry.Logged).Sum(),
                    group.Select(entry => entry.Suggested).Sum()))
                .ToList();

            return new SummaryDay
            {
                Date = day.Date.Date,
                IsWeekend = day.IsWeekend,
                Norm = day.IsWeekend ? TimeSpan.Zero : day.Settings.WorkingTime,
                Logged = day.ActualWorklogTimeSpent,
                Suggested = day.EstimatedWorklogTimeSpent,
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                Segments = segments,
                LoggedSegments = logged,
                IssueKeys = issueTime.Select(time => time.Key).ToList(),
                IssueTime = issueTime,
            };
        }

        private static SummaryKind KindOf(EventSource? source) => source switch
        {
            EventSource.Assignee => SummaryKind.Work,
            EventSource.Tester => SummaryKind.Testing,
            EventSource.Comment => SummaryKind.Comment,
            EventSource.Calendar => SummaryKind.Calendar,
            _ => SummaryKind.Manual
        };

        private static string Title(WorkingDayWorklog worklog) =>
            worklog.Issue?.Key is { } key
                ? $"{key} {worklog.Issue.Summary}".TrimEnd()
                : worklog.Comment;
    }

    internal sealed record IssueTime(string Key, string Title, string Link, TimeSpan Logged, TimeSpan Suggested);

    public sealed record SummaryIssue(
        string Key,
        string Title,
        string Link,
        TimeSpan Logged,
        IReadOnlyList<SummaryIssueDay> Days);

    public sealed record SummaryIssueDay(DateTime Date, TimeSpan Logged, TimeSpan Suggested);

    public sealed record SummarySource(SummaryKind Kind, TimeSpan Logged);

    /// <summary>Weekday (Monday first) × hour of the working day, each cell 0 to 1.</summary>
    public sealed record SummaryHeatGrid(int FirstHour, int Hours, double[,] Cells);
}
