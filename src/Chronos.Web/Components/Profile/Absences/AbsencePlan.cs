using Chronos.Application.Calendar;
using Chronos.Application.Calendar.Dto;
using Chronos.Domain.Entities.Calendar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Web.Components.Profile.Absences
{
    /// <summary>
    /// What a stretch of days costs: how long it is, how many working days it skips and how
    /// many vacation days it takes. A vacation is counted in calendar days, but a public
    /// holiday inside it does not use one up (Labour Code, art. 120). See issue #310.
    /// </summary>
    public sealed class AbsencePlan
    {
        /// <summary>The yearly vacation of the Labour Code, art. 115.</summary>
        public const int YearlyVacationDays = 28;

        private readonly IReadOnlyDictionary<DateTime, WorkingCalendarDay> _calendar;

        /// <param name="calendar">The days as the calendar has them, without the user's absences.</param>
        public AbsencePlan(IReadOnlyDictionary<DateTime, WorkingCalendarDay> calendar)
        {
            _calendar = calendar ?? new Dictionary<DateTime, WorkingCalendarDay>();
        }

        public WorkingCalendarDay DayOf(DateTime date) =>
            _calendar.GetValueOrDefault(date.Date) ?? WorkingCalendarDay.ByWeekday(date);

        public Cost CostOf(DateTime start, DateTime end)
        {
            var days = Days(start, end).ToList();
            var publicHolidays = days.Count(PublicHolidays.Contains);
            return new Cost(
                CalendarDays: days.Count,
                WorkingDays: days.Count(date => DayOf(date).IsWorking),
                PublicHolidays: publicHolidays,
                VacationDays: days.Count - publicHolidays);
        }

        /// <summary>Vacation days of <paramref name="year"/>, split into the ones already taken and the ones ahead.</summary>
        public Balance BalanceOf(int year, IEnumerable<UserAbsenceDto> absences, DateTime today)
        {
            var vacationDays = absences
                .Where(absence => absence.Kind == AbsenceKind.Vacation)
                .SelectMany(absence => Days(absence.StartDate, absence.EndDate))
                .Where(date => date.Year == year && !PublicHolidays.Contains(date))
                .ToList();
            var sickDays = absences
                .Where(absence => absence.Kind == AbsenceKind.SickLeave)
                .SelectMany(absence => Days(absence.StartDate, absence.EndDate))
                .Count(date => date.Year == year);

            return new Balance(
                Taken: vacationDays.Count(date => date < today.Date),
                Planned: vacationDays.Count(date => date >= today.Date),
                SickDays: sickDays);
        }

        public static IEnumerable<DateTime> Days(DateTime start, DateTime end)
        {
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
                yield return date;
        }

        public sealed record Cost(int CalendarDays, int WorkingDays, int PublicHolidays, int VacationDays);

        public sealed record Balance(int Taken, int Planned, int SickDays)
        {
            public int Used => Taken + Planned;
            public int Left => Math.Max(0, YearlyVacationDays - Used);
        }
    }
}
