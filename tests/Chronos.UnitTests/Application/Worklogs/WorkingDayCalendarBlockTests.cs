using NUnit.Framework;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;

namespace Chronos.UnitTests.Application.Worklogs
{
    [TestFixture]
    public class WorkingDayCalendarBlockTests
    {
        private static WorkingDay DayWith(
            IReadOnlyList<IEvent> blocked,
            IList<WorkingDayWorklog>? worklogs = null)
        {
            var settings = new WorkingDaySettings(
                TimeSpan.FromHours(10), TimeSpan.FromHours(19), TimeSpan.FromHours(1));
            return new WorkingDay(new DateTime(2026, 6, 1), settings, worklogs)
            {
                BlockedEvents = blocked
            };
        }

        private static UserEvent Meeting() =>
            new()
            {
                StartDate = new DateTime(2026, 6, 1, 12, 0, 0),
                CompleteDate = new DateTime(2026, 6, 1, 13, 0, 0),
                Summary = "Meeting",
                Source = EventSource.Calendar
            };

        private static WorkingDayWorklog Actual(int fromHour, int fromMinute, int toHour, int toMinute) =>
            new(
                new DateTime(2026, 6, 1, fromHour, fromMinute, 0),
                new DateTime(2026, 6, 1, toHour, toMinute, 0),
                new Issue { Key = "PROJ-1" },
                WorklogType.Actual,
                null);

        private static WorkingDayWorklog CalendarEstimate(int fromHour, int fromMinute, int toHour, int toMinute) =>
            new(
                new DateTime(2026, 6, 1, fromHour, fromMinute, 0),
                new DateTime(2026, 6, 1, toHour, toMinute, 0),
                new Issue { Key = "PROJ-1" },
                WorklogType.Estimated,
                EventSource.Calendar);

        private static UserEvent KeylessEvent(int fromHour, int fromMinute, int toHour, int toMinute) =>
            new()
            {
                StartDate = new DateTime(2026, 6, 1, fromHour, fromMinute, 0),
                CompleteDate = new DateTime(2026, 6, 1, toHour, toMinute, 0),
                Summary = "Unmapped meeting",
                Source = EventSource.Calendar
            };

        [Test]
        public void KeylessEvent_WithoutMatchingWorklog_BlocksTimeAndNotLogged()
        {
            var meeting = Meeting();
            var day = DayWith(new List<IEvent> { meeting });

            day.Refresh();

            Assert.That(day.IsEventLogged(meeting), Is.False);
            Assert.That(day.BlockedEventsTime, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void KeylessEvent_WithMatchingWorklog_NotBlockedAndLogged()
        {
            var meeting = Meeting();
            var logged = new WorkingDayWorklog(
                new DateTime(2026, 6, 1, 12, 0, 0),
                new DateTime(2026, 6, 1, 13, 0, 0),
                new Issue { Key = "PROJ-1" },
                WorklogType.Actual,
                EventSource.Calendar);
            var day = DayWith(new List<IEvent> { meeting }, new List<WorkingDayWorklog> { logged });

            day.Refresh();

            Assert.That(day.IsEventLogged(meeting), Is.True);
            Assert.That(day.BlockedEventsTime, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void KeylessEvent_Unlogged_ContributesToEstimatedWorklogTimeSpent()
        {
            var meeting = Meeting();
            var day = DayWith(new List<IEvent> { meeting });

            day.Refresh();

            Assert.That(day.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void KeylessEvent_Logged_DoesNotContributeToEstimatedWorklogTimeSpent()
        {
            var meeting = Meeting();
            var logged = new WorkingDayWorklog(
                new DateTime(2026, 6, 1, 12, 0, 0),
                new DateTime(2026, 6, 1, 13, 0, 0),
                new Issue { Key = "PROJ-1" },
                WorklogType.Actual,
                EventSource.Calendar);
            var day = DayWith(new List<IEvent> { meeting }, new List<WorkingDayWorklog> { logged });

            day.Refresh();

            Assert.That(day.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void KeylessEvent_TakesItsWorklogAwayFromAnEstimateWithTheSameIssue()
        {
            // A mapped event at 09:00 and an unmapped one at 12:20, both logged against the
            // same issue. The 12:20 worklog belongs to the event it was logged for — before
            // the fix it was also matched by issue key and shown under the 09:00 estimate.
            var mapped = CalendarEstimate(9, 0, 9, 30);
            var loggedForMapped = Actual(9, 0, 9, 30);
            var loggedForKeyless = Actual(12, 20, 12, 50);
            var keyless = KeylessEvent(12, 20, 12, 50);

            var day = DayWith(
                new List<IEvent> { keyless },
                new List<WorkingDayWorklog> { loggedForMapped, loggedForKeyless, mapped });

            day.Refresh();

            Assert.Multiple(() =>
            {
                Assert.That(day.GetLoggedWorklog(keyless), Is.SameAs(loggedForKeyless));
                Assert.That(loggedForKeyless.Parent, Is.Null);
                Assert.That(mapped.Children, Is.EqualTo(new[] { loggedForMapped }));
                Assert.That(day.LoggedEventWorklogs, Is.EqualTo(new[] { loggedForKeyless }));
            });
        }

        [Test]
        public void KeylessEvent_AddedAfterAnEarlierRefresh_TakesItsWorklogOffTheEstimate()
        {
            var mapped = CalendarEstimate(9, 0, 9, 30);
            var loggedForKeyless = Actual(12, 20, 12, 50);
            var keyless = KeylessEvent(12, 20, 12, 50);

            var day = DayWith(
                new List<IEvent>(),
                new List<WorkingDayWorklog> { loggedForKeyless, mapped });

            day.Refresh();
            Assert.That(mapped.Children, Is.EqualTo(new[] { loggedForKeyless }), "precondition");

            day.BlockedEvents = new List<IEvent> { keyless };
            day.Refresh();

            Assert.Multiple(() =>
            {
                Assert.That(day.GetLoggedWorklog(keyless), Is.SameAs(loggedForKeyless));
                Assert.That(loggedForKeyless.Parent, Is.Null);
                Assert.That(mapped.Children, Is.Empty);
            });
        }

        [Test]
        public void TwoKeylessEventsAtTheSameTime_ShareNoWorklog()
        {
            var first = KeylessEvent(12, 0, 13, 0);
            var second = KeylessEvent(12, 0, 13, 0);
            var logged = Actual(12, 0, 13, 0);

            var day = DayWith(
                new List<IEvent> { first, second },
                new List<WorkingDayWorklog> { logged });

            day.Refresh();

            Assert.Multiple(() =>
            {
                Assert.That(day.GetLoggedWorklog(first), Is.SameAs(logged));
                Assert.That(day.GetLoggedWorklog(second), Is.Null);
                Assert.That(day.LoggedEventWorklogs, Has.Count.EqualTo(1));
                Assert.That(day.BlockedEventsTime, Is.EqualTo(TimeSpan.FromHours(1)));
            });
        }

        [Test]
        public void TwoEvents_OneLogged_OnlyUnloggedEventBlocksTime()
        {
            var logged = Meeting(); // 12:00-13:00
            var other = new UserEvent
            {
                StartDate = new DateTime(2026, 6, 1, 14, 0, 0),
                CompleteDate = new DateTime(2026, 6, 1, 15, 0, 0),
                Summary = "Standup",
                Source = EventSource.Calendar
            };

            var actualWorklog = new WorkingDayWorklog(
                new DateTime(2026, 6, 1, 12, 0, 0),
                new DateTime(2026, 6, 1, 13, 0, 0),
                new Issue { Key = "PROJ-1" },
                WorklogType.Actual,
                EventSource.Calendar);

            var day = DayWith(
                new List<IEvent> { logged, other },
                new List<WorkingDayWorklog> { actualWorklog });

            day.Refresh();

            Assert.That(day.IsEventLogged(logged), Is.True);
            Assert.That(day.IsEventLogged(other), Is.False);
            Assert.That(day.BlockedEventsTime, Is.EqualTo(TimeSpan.FromHours(1)));
        }
    }
}
