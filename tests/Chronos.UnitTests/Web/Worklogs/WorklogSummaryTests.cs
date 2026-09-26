using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using Chronos.Web.Components.Worklogs.Summary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.UnitTests.Web.Worklogs
{
    /// <summary>
    /// The summary of a period is worked out from the days already read: what is logged,
    /// what Chronos suggests and where the time went. See issue #173.
    /// </summary>
    [TestFixture]
    public class WorklogSummaryTests
    {
        // Monday.
        private static readonly DateTime Monday = new(2026, 9, 21);

        private static WorkingDay Day(DateTime date, IReadOnlyList<IEvent>? blocked = null, params WorkingDayWorklog[] worklogs)
        {
            var settings = new WorkingDaySettings(TimeSpan.FromHours(10), TimeSpan.FromHours(19), TimeSpan.FromHours(1));
            var day = new WorkingDay(date, settings, worklogs.ToList())
            {
                BlockedEvents = blocked ?? new List<IEvent>()
            };
            day.Refresh();
            return day;
        }

        private static WorkingDayWorklog Actual(DateTime date, double from, double to, string? key = "PROJ-1") =>
            new(date.AddHours(from), date.AddHours(to), key is null ? null! : new Issue { Key = key }, WorklogType.Actual, null);

        private static WorkingDayWorklog Estimated(DateTime date, double from, double to, EventSource source, string key = "PROJ-1") =>
            new(date.AddHours(from), date.AddHours(to), new Issue { Key = key }, WorklogType.Estimated, source);

        private static UserEvent CalendarEvent(DateTime date, double from, double to) =>
            new()
            {
                StartDate = date.AddHours(from),
                CompleteDate = date.AddHours(to),
                Summary = "Standup",
                Source = EventSource.Calendar
            };

        [Test]
        public void Day_Should_CountLoggedSuggestedAndWhatIsLeftToTheNorm()
        {
            var day = Day(Monday, null,
                Actual(Monday, 10, 15, "PROJ-1"),
                Estimated(Monday, 15, 16, EventSource.Calendar, "PROJ-2"));

            var summary = WorklogSummary.Create(new[] { day });
            var summaryDay = summary.Days.Single();

            Assert.That(summaryDay.Logged, Is.EqualTo(TimeSpan.FromHours(5)));
            Assert.That(summaryDay.Suggested, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(summary.Shortfall, Is.EqualTo(TimeSpan.FromHours(3)));
            Assert.That(summary.Suggested, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Shortfall_Should_NotBeFilledByOvertimeOnAnotherDay()
        {
            var tuesday = Monday.AddDays(1);
            var summary = WorklogSummary.Create(new[]
            {
                Day(Monday, null, Actual(Monday, 8, 20)),
                Day(tuesday, null, Actual(tuesday, 10, 14)),
            });

            Assert.That(summary.Shortfall, Is.EqualTo(TimeSpan.FromHours(4)));
        }

        [Test]
        public void Weekend_Should_BeLeftOut_UnlessSomethingHappenedOnIt()
        {
            var saturday = Monday.AddDays(5);
            var sunday = Monday.AddDays(6);

            var summary = WorklogSummary.Create(new[]
            {
                Day(Monday),
                Day(saturday, null, Actual(saturday, 11, 12)),
                Day(sunday),
            });

            Assert.That(summary.Days.Select(day => day.Date), Is.EqualTo(new[] { Monday, saturday }));
            Assert.That(summary.Days.Last().Norm, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Sources_Should_FollowTheEventAWorklogWasLoggedFor()
        {
            var calendarEvent = CalendarEvent(Monday, 12, 13);
            var day = Day(Monday, new List<IEvent> { calendarEvent },
                Estimated(Monday, 10, 19, EventSource.Assignee, "PROJ-1"),
                Actual(Monday, 10, 12, "PROJ-1"),
                Actual(Monday, 12, 13, "PROJ-9"),
                Actual(Monday, 14, 15, "PROJ-3"));

            var sources = WorklogSummary.Create(new[] { day }).Sources.ToDictionary(source => source.Kind, source => source.Logged);

            Assert.That(sources[SummaryKind.Work], Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(sources[SummaryKind.Calendar], Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(sources[SummaryKind.Manual], Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Timeline_Should_PlaceOnlySuggestionsWithATimeOfTheirOwn()
        {
            var day = Day(Monday, new List<IEvent> { CalendarEvent(Monday, 10, 10.5) },
                Estimated(Monday, 10, 19, EventSource.Assignee, "PROJ-1"),
                Estimated(Monday, 15, 16, EventSource.Calendar, "PROJ-2"));

            var segments = WorklogSummary.Create(new[] { day }).Days.Single().Segments;

            Assert.That(segments.Select(segment => (segment.Start.TotalHours, segment.Kind, segment.IsSuggestion)),
                Is.EqualTo(new[]
                {
                    (10.0, SummaryKind.Calendar, true),
                    (15.0, SummaryKind.Calendar, true),
                }));
        }

        [Test]
        public void Timeline_Should_WidenTheDayForWhatHappenedOutsideIt()
        {
            var day = Day(Monday, new List<IEvent> { CalendarEvent(Monday, 19, 20) },
                Actual(Monday, 8, 11),
                Estimated(Monday, 9, 9.5, EventSource.Calendar, "PROJ-2"));

            var summaryDay = WorklogSummary.Create(new[] { day }).Days.Single();

            Assert.That(summaryDay.WindowStart, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(summaryDay.WindowEnd, Is.EqualTo(TimeSpan.FromHours(20)));
            Assert.That(summaryDay.Segments.Select(segment => segment.Start.TotalHours), Is.EqualTo(new[] { 8.0, 9.0, 19.0 }),
                "a calendar event before the working day keeps its own time");
        }

        [Test]
        public void Sources_Should_CountAWorklogThatRunsPastMidnightInFull()
        {
            var day = Day(Monday, null, Actual(Monday, 22, 26));

            var summary = WorklogSummary.Create(new[] { day });

            Assert.That(summary.Days.Single().Segments.Single().End, Is.EqualTo(TimeSpan.FromDays(1)));
            Assert.That(summary.Sources.Single().Logged, Is.EqualTo(TimeSpan.FromHours(4)));
        }

        [Test]
        public void CalendarShare_Should_BeTheLoggedPartThatCameFromTheCalendar()
        {
            var calendarEvent = CalendarEvent(Monday, 10, 12);
            var day = Day(Monday, new List<IEvent> { calendarEvent, CalendarEvent(Monday, 16, 17) },
                Actual(Monday, 10, 12, "PROJ-9"),
                Actual(Monday, 12, 16, "PROJ-1"));

            var summary = WorklogSummary.Create(new[] { day });

            Assert.That(summary.CalendarShare, Is.EqualTo(1d / 3).Within(1e-9));
            Assert.That(summary.Days.Single().CalendarTime, Is.EqualTo(TimeSpan.FromHours(3)),
                "an event not logged yet is still on the calendar");
        }

        [Test]
        public void Issues_Should_BeOrderedByLoggedTime_WithTheirDays()
        {
            var tuesday = Monday.AddDays(1);
            var summary = WorklogSummary.Create(new[]
            {
                Day(Monday, null, Actual(Monday, 10, 12, "PROJ-1"), Actual(Monday, 12, 13, "PROJ-2")),
                Day(tuesday, null, Actual(tuesday, 10, 11, "PROJ-1")),
            });

            Assert.That(summary.Issues.Select(issue => issue.Key), Is.EqualTo(new[] { "PROJ-1", "PROJ-2" }));
            Assert.That(summary.Issues[0].Logged, Is.EqualTo(TimeSpan.FromHours(3)));
            Assert.That(summary.Issues[1].Days.Select(issueDay => issueDay.Logged),
                Is.EqualTo(new[] { TimeSpan.FromHours(1), TimeSpan.Zero }));
            Assert.That(summary.IssueCount, Is.EqualTo(2));
        }

        [Test]
        public void Issues_Should_LeaveOutIssuesWithNothingLogged()
        {
            var day = Day(Monday, null, Estimated(Monday, 10, 19, EventSource.Assignee, "PROJ-1"));

            var summary = WorklogSummary.Create(new[] { day });

            Assert.That(summary.Issues, Is.Empty);
            Assert.That(summary.IssueCount, Is.EqualTo(1), "a suggested issue is still an issue of the period");
        }

        [Test]
        public void HeatMap_Should_AverageEachHourOverTheSameWeekday()
        {
            var nextMonday = Monday.AddDays(7);
            var summary = WorklogSummary.Create(new[]
            {
                Day(Monday, new List<IEvent> { CalendarEvent(Monday, 11, 12) }),
                Day(nextMonday, new List<IEvent> { CalendarEvent(nextMonday, 11, 11.5) }),
            });

            var map = summary.HeatMap(calendar: true);

            Assert.That(map.FirstHour, Is.EqualTo(10));
            Assert.That(map.Hours, Is.EqualTo(9));
            Assert.That(map.Cells[0, 1], Is.EqualTo(0.75).Within(1e-9));
            Assert.That(map.Cells[0, 0], Is.Zero);
            Assert.That(map.Cells[1, 1], Is.Zero, "no Tuesday was read");
        }
    }
}
