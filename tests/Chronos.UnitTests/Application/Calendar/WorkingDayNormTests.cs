using Chronos.Application.Calendar;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Worklogs;
using Chronos.UnitTests.Application.Mock;

namespace Chronos.UnitTests.Application.Calendar
{
    /// <summary>
    /// The day spreads its estimates over the norm the calendar leaves it: all of the
    /// working day, an hour less before a holiday, nothing on a day off. See issue #310.
    /// </summary>
    [TestFixture]
    public class WorkingDayNormTests
    {
        private static readonly WorkingDaySettings Settings = new(
            workingStartTime: TimeSpan.FromHours(9),
            workingEndTime: TimeSpan.FromHours(18),
            lunchTime: TimeSpan.FromHours(1));

        private static WorkingDay Day(DateTime date, WorkingDayKind? kind)
        {
            var estimated = new WorkingDayWorklog
            {
                Type = WorklogType.Estimated,
                Source = EventSource.Assignee,
                StartDate = date.AddHours(9),
                CompleteDate = date.AddHours(18),
                RawStartDate = date.AddHours(9),
                RawCompleteDate = date.AddHours(18),
                Issue = new MockIssue("1")
            };
            var day = new WorkingDay(
                date,
                Settings,
                new List<WorkingDayWorklog> { estimated },
                kind is null ? null : new WorkingCalendarDay(date, kind.Value));
            day.Refresh();
            return day;
        }

        [Test]
        public void Refresh_OnWorkday_SpreadsTheFullNorm()
        {
            var day = Day(new DateTime(2026, 6, 2), WorkingDayKind.Workday);

            Assert.That(day.Norm, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(day.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
        }

        [Test]
        public void Refresh_OnShortDay_SpreadsAnHourLess()
        {
            var day = Day(new DateTime(2026, 6, 11), WorkingDayKind.ShortDay);

            Assert.That(day.Norm, Is.EqualTo(TimeSpan.FromHours(7)));
            Assert.That(day.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(7)));
        }

        [TestCase(WorkingDayKind.Holiday)]
        [TestCase(WorkingDayKind.Absence)]
        public void Refresh_OnDayOff_KeepsTheRowsWithNothingToSuggest(WorkingDayKind kind)
        {
            var day = Day(new DateTime(2026, 6, 12), kind);

            Assert.That(day.IsWorking, Is.False);
            Assert.That(day.Norm, Is.EqualTo(TimeSpan.Zero));
            Assert.That(day.EstimatedWorklogs.Count(), Is.EqualTo(1));
            Assert.That(day.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Refresh_OnWorkingSaturday_SpreadsTheFullNorm()
        {
            var day = Day(new DateTime(2026, 6, 13), WorkingDayKind.Workday);

            Assert.That(day.IsWorking, Is.True);
            Assert.That(day.EstimatedWorklogTimeSpent, Is.EqualTo(TimeSpan.FromHours(8)));
        }

        [Test]
        public void Constructor_WithoutCalendar_TellsTheDayByWeekday()
        {
            var saturday = Day(new DateTime(2026, 6, 13), kind: null);
            var monday = Day(new DateTime(2026, 6, 15), kind: null);

            Assert.That(saturday.Kind, Is.EqualTo(WorkingDayKind.Weekend));
            Assert.That(saturday.Norm, Is.EqualTo(TimeSpan.Zero));
            Assert.That(monday.Kind, Is.EqualTo(WorkingDayKind.Workday));
            Assert.That(monday.Norm, Is.EqualTo(TimeSpan.FromHours(8)));
        }
    }
}
