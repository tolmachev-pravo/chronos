using Moq;
using Chronos.Application.Calendar;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.UnitTests.Application.Calendar
{
    /// <summary>
    /// An absence wins over the production calendar, and the calendar over the weekday.
    /// See issue #310.
    /// </summary>
    [TestFixture]
    public class WorkingCalendarTests
    {
        private Mock<ICalendarDayRepository> _calendarDays;
        private Mock<IUserAbsenceRepository> _absences;
        private WorkingCalendar _sut;

        [SetUp]
        public void SetUp()
        {
            _calendarDays = new Mock<ICalendarDayRepository>();
            _absences = new Mock<IUserAbsenceRepository>();
            SetUpCalendar();
            SetUpAbsences();
            _sut = new WorkingCalendar(_calendarDays.Object, _absences.Object);
        }

        private void SetUpCalendar(params CalendarDay[] days) =>
            _calendarDays
                .Setup(repository => repository.GetAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(days);

        private void SetUpAbsences(params UserAbsence[] absences) =>
            _absences
                .Setup(repository => repository.GetAsync("john", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(absences);

        [Test]
        public async Task GetDaysAsync_WithoutExceptions_TellsDaysByWeekday()
        {
            // Friday 5 June 2026 to Monday 8 June 2026.
            var days = await _sut.GetDaysAsync("john", new DateTime(2026, 6, 5), new DateTime(2026, 6, 8));

            Assert.That(days.Keys, Is.EqualTo(new[]
            {
                new DateTime(2026, 6, 5), new DateTime(2026, 6, 6), new DateTime(2026, 6, 7), new DateTime(2026, 6, 8)
            }));
            Assert.That(days.Values.Select(day => day.Kind), Is.EqualTo(new[]
            {
                WorkingDayKind.Workday, WorkingDayKind.Weekend, WorkingDayKind.Weekend, WorkingDayKind.Workday
            }));
        }

        [Test]
        public async Task GetDaysAsync_WithCalendarExceptions_AppliesThem()
        {
            SetUpCalendar(
                new CalendarDay { Date = new DateTime(2026, 6, 11), Kind = CalendarDayKind.ShortDay, Title = "Предпраздничный день" },
                new CalendarDay { Date = new DateTime(2026, 6, 12), Kind = CalendarDayKind.Holiday, Title = "День России" },
                new CalendarDay { Date = new DateTime(2026, 6, 13), Kind = CalendarDayKind.Workday, Title = "Рабочий выходной" });

            var days = await _sut.GetDaysAsync("john", new DateTime(2026, 6, 11), new DateTime(2026, 6, 13));

            Assert.That(days[new DateTime(2026, 6, 11)].Kind, Is.EqualTo(WorkingDayKind.ShortDay));
            Assert.That(days[new DateTime(2026, 6, 12)].Kind, Is.EqualTo(WorkingDayKind.Holiday));
            Assert.That(days[new DateTime(2026, 6, 12)].Title, Is.EqualTo("День России"));
            Assert.That(days[new DateTime(2026, 6, 13)].Kind, Is.EqualTo(WorkingDayKind.Workday));
            Assert.That(days[new DateTime(2026, 6, 13)].IsWorking, Is.True);
        }

        [Test]
        public async Task GetDaysAsync_WithAbsence_WinsOverCalendarAndWeekday()
        {
            SetUpCalendar(new CalendarDay { Date = new DateTime(2026, 6, 12), Kind = CalendarDayKind.Holiday, Title = "День России" });
            SetUpAbsences(new UserAbsence
            {
                Username = "john",
                StartDate = new DateTime(2026, 6, 10),
                EndDate = new DateTime(2026, 6, 14),
                Kind = AbsenceKind.Vacation,
                Comment = "Море"
            });

            var days = await _sut.GetDaysAsync("john", new DateTime(2026, 6, 9), new DateTime(2026, 6, 15));

            Assert.That(days[new DateTime(2026, 6, 9)].Kind, Is.EqualTo(WorkingDayKind.Workday));
            foreach (var date in new[] { 10, 11, 12, 13, 14 }.Select(day => new DateTime(2026, 6, day)))
            {
                Assert.That(days[date].Kind, Is.EqualTo(WorkingDayKind.Absence), date.ToString("d"));
                Assert.That(days[date].Absence, Is.EqualTo(AbsenceKind.Vacation));
                Assert.That(days[date].Title, Is.EqualTo("Море"));
            }
            Assert.That(days[new DateTime(2026, 6, 15)].Kind, Is.EqualTo(WorkingDayKind.Workday));
        }

        [Test]
        public async Task GetDaysAsync_WithoutUser_DoesNotAskForAbsences()
        {
            var days = await _sut.GetDaysAsync(null!, new DateTime(2026, 6, 1), new DateTime(2026, 6, 1));

            Assert.That(days.Single().Value.Kind, Is.EqualTo(WorkingDayKind.Workday));
            _absences.Verify(repository => repository.GetAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase(WorkingDayKind.Workday, 8)]
        [TestCase(WorkingDayKind.ShortDay, 7)]
        [TestCase(WorkingDayKind.Weekend, 0)]
        [TestCase(WorkingDayKind.Holiday, 0)]
        [TestCase(WorkingDayKind.Absence, 0)]
        public void NormOf_FullDay_LeavesWhatTheKindAllows(WorkingDayKind kind, int expectedHours)
        {
            var day = new WorkingCalendarDay(new DateTime(2026, 6, 1), kind);

            Assert.That(day.NormOf(TimeSpan.FromHours(8)), Is.EqualTo(TimeSpan.FromHours(expectedHours)));
        }

        [Test]
        public void NormOf_ShortDayShorterThanAnHour_IsZero()
        {
            var day = new WorkingCalendarDay(new DateTime(2026, 6, 11), WorkingDayKind.ShortDay);

            Assert.That(day.NormOf(TimeSpan.FromMinutes(30)), Is.EqualTo(TimeSpan.Zero));
        }
    }
}
