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
