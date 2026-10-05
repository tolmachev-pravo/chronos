using Chronos.Application.Calendar;
using Chronos.Application.Calendar.Dto;
using Chronos.Domain.Entities.Calendar;
using Chronos.Web.Components.Profile.Absences;

namespace Chronos.UnitTests.Web.Profile
{
    /// <summary>
    /// A vacation is counted in calendar days, and a public holiday inside it does not use
    /// one up; a carried-over day off does. See issue #310.
    /// </summary>
    [TestFixture]
    public class AbsencePlanTests
    {
        // 2–8 November 2026: Tuesday the 3rd is short, Wednesday the 4th a public holiday.
        private static readonly AbsencePlan Plan = new(new Dictionary<DateTime, WorkingCalendarDay>
        {
            [new DateTime(2026, 11, 3)] = new(new DateTime(2026, 11, 3), WorkingDayKind.ShortDay),
            [new DateTime(2026, 11, 4)] = new(new DateTime(2026, 11, 4), WorkingDayKind.Holiday, "День народного единства"),
            [new DateTime(2026, 12, 31)] = new(new DateTime(2026, 12, 31), WorkingDayKind.Holiday, "Перенесённый выходной")
        });

        private static UserAbsenceDto Absence(AbsenceKind kind, DateTime start, DateTime end) =>
            new(Guid.NewGuid(), start, end, kind, null!);

        [Test]
        public void CostOf_WeekWithPublicHoliday_DoesNotChargeTheHoliday()
        {
            var cost = Plan.CostOf(new DateTime(2026, 11, 2), new DateTime(2026, 11, 8));

            Assert.That(cost, Is.EqualTo(new AbsencePlan.Cost(
                CalendarDays: 7, WorkingDays: 4, PublicHolidays: 1, VacationDays: 6)));
        }

        [Test]
        public void CostOf_CarriedOverDayOff_IsCharged()
        {
            var cost = Plan.CostOf(new DateTime(2026, 12, 31), new DateTime(2026, 12, 31));

            Assert.That(cost.WorkingDays, Is.EqualTo(0));
            Assert.That(cost.VacationDays, Is.EqualTo(1));
        }

        [Test]
        public void BalanceOf_SplitsTakenAndPlanned_AndCountsOnlyTheYear()
        {
            var absences = new[]
            {
                Absence(AbsenceKind.Vacation, new DateTime(2026, 7, 13), new DateTime(2026, 7, 26)),
                Absence(AbsenceKind.Vacation, new DateTime(2026, 11, 2), new DateTime(2026, 11, 8)),
                Absence(AbsenceKind.Vacation, new DateTime(2026, 12, 28), new DateTime(2027, 1, 10)),
                Absence(AbsenceKind.SickLeave, new DateTime(2026, 3, 16), new DateTime(2026, 3, 18)),
                Absence(AbsenceKind.DayOff, new DateTime(2026, 10, 16), new DateTime(2026, 10, 16))
            };

            var balance = Plan.BalanceOf(2026, absences, today: new DateTime(2026, 10, 5));

            Assert.Multiple(() =>
            {
                Assert.That(balance.Taken, Is.EqualTo(14));
                Assert.That(balance.Planned, Is.EqualTo(6 + 4));
                Assert.That(balance.SickDays, Is.EqualTo(3));
                Assert.That(balance.Left, Is.EqualTo(AbsencePlan.YearlyVacationDays - 24));
            });
        }

        [Test]
        public void BalanceOf_MoreThanAYear_LeavesNothingButNotLessThanNothing()
        {
            var balance = Plan.BalanceOf(2026,
                new[] { Absence(AbsenceKind.Vacation, new DateTime(2026, 6, 1), new DateTime(2026, 7, 31)) },
                today: new DateTime(2026, 10, 5));

            Assert.That(balance.Left, Is.EqualTo(0));
        }

        [Test]
        public void DayOf_DateOutsideTheCalendar_IsToldByWeekday()
        {
            Assert.That(Plan.DayOf(new DateTime(2026, 11, 7)).Kind, Is.EqualTo(WorkingDayKind.Weekend));
            Assert.That(Plan.DayOf(new DateTime(2026, 11, 9)).Kind, Is.EqualTo(WorkingDayKind.Workday));
        }
    }
}
