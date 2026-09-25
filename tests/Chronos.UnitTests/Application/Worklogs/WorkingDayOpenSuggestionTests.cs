using NUnit.Framework;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;

namespace Chronos.UnitTests.Application.Worklogs
{
    /// <summary>
    /// The count on a day of the period column: every row still waiting for the user,
    /// whether or not the day had time left to hand it.
    /// </summary>
    [TestFixture]
    public class WorkingDayOpenSuggestionTests
    {
        private static readonly DateTime Day = new(2026, 9, 21);

        private static WorkingDay DayWith(IList<WorkingDayWorklog> worklogs, params IEvent[] blocked)
        {
            var settings = new WorkingDaySettings(
                TimeSpan.FromHours(10), TimeSpan.FromHours(19), TimeSpan.FromHours(1));
            var day = new WorkingDay(Day, settings, worklogs) { BlockedEvents = blocked };
            day.Refresh();
            return day;
        }

        private static WorkingDayWorklog Estimate(string key, EventSource source, int fromHour, int toHour) =>
            new(Day.AddHours(fromHour), Day.AddHours(toHour), new Issue { Key = key }, WorklogType.Estimated, source);

        private static WorkingDayWorklog Actual(string key, int fromHour, int toHour) =>
            new(Day.AddHours(fromHour), Day.AddHours(toHour), new Issue { Key = key }, WorklogType.Actual, null);

        private static UserEvent Meeting(int fromHour, int toHour) => new()
        {
            StartDate = Day.AddHours(fromHour),
            CompleteDate = Day.AddHours(toHour),
            Summary = "Meeting",
            Source = EventSource.Calendar
        };

        [Test]
        public void OpenSuggestionCount_Should_CountASuggestion_TheDayHadNoTimeFor()
        {
            // Meetings take the whole norm, so the task is handed no time — but its row
            // is still on screen and still waiting.
            var day = DayWith(
                new List<WorkingDayWorklog> { Estimate("PROJ-1", EventSource.Assignee, 10, 19) },
                Meeting(10, 19));

            Assert.That(day.EstimatedWorklogs, Has.All.Property(nameof(WorkingDayWorklog.RemainingTimeSpent)).EqualTo(TimeSpan.Zero));
            Assert.That(day.OpenSuggestionCount, Is.EqualTo(2), "the task and the meeting");
        }

        [Test]
        public void OpenSuggestionCount_Should_LeaveOut_WhatIsAlreadyLogged()
        {
            var day = DayWith(
                new List<WorkingDayWorklog>
                {
                    Estimate("PROJ-1", EventSource.Assignee, 10, 12),
                    Actual("PROJ-1", 10, 12),
                    Estimate("PROJ-2", EventSource.Assignee, 12, 14),
                    Actual("PROJ-9", 15, 16),
                },
                Meeting(15, 16));

            Assert.That(day.OpenSuggestionCount, Is.EqualTo(1), "only PROJ-2: the meeting was logged as PROJ-9");
        }
    }
}
