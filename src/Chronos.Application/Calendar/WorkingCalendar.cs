using Chronos.Domain.Entities.Calendar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar
{
    /// <summary>
    /// Lays the two layers over the weekday rule, the user's own first: an absence wins
    /// over the production calendar, and the calendar over the weekday. A holiday inside a
    /// vacation is still part of the vacation here — the norm is nothing either way.
    /// See issue #310.
    /// </summary>
    public class WorkingCalendar : IWorkingCalendar
    {
        private readonly ICalendarDayRepository _calendarDays;
        private readonly IUserAbsenceRepository _absences;

        public WorkingCalendar(
            ICalendarDayRepository calendarDays,
            IUserAbsenceRepository absences)
        {
            _calendarDays = calendarDays;
            _absences = absences;
        }

        public async Task<IReadOnlyDictionary<DateTime, WorkingCalendarDay>> GetDaysAsync(
            string username, DateTime from, DateTime to, CancellationToken ct = default)
        {
            var first = from.Date;
            var last = to.Date;

            var calendarDays = (await _calendarDays.GetAsync(first, last, ct))
                .GroupBy(day => day.Date.Date)
                .ToDictionary(group => group.Key, group => group.First());

            IReadOnlyList<UserAbsence> absences = string.IsNullOrEmpty(username)
                ? Array.Empty<UserAbsence>()
                : await _absences.GetAsync(username, first, last, ct);

            var days = new Dictionary<DateTime, WorkingCalendarDay>();
            for (var date = first; date <= last; date = date.AddDays(1))
            {
                var absence = absences.FirstOrDefault(item =>
                    item.StartDate.Date <= date && date <= item.EndDate.Date);

                if (absence is not null)
                    days[date] = new WorkingCalendarDay(date, WorkingDayKind.Absence, absence.Comment, absence.Kind);
                else if (calendarDays.TryGetValue(date, out var calendarDay))
                    days[date] = new WorkingCalendarDay(date, KindOf(calendarDay.Kind), calendarDay.Title);
                else
                    days[date] = WorkingCalendarDay.ByWeekday(date);
            }

            return days;
        }

        private static WorkingDayKind KindOf(CalendarDayKind kind) => kind switch
        {
            CalendarDayKind.Holiday => WorkingDayKind.Holiday,
            CalendarDayKind.ShortDay => WorkingDayKind.ShortDay,
            _ => WorkingDayKind.Workday
        };
    }
}
