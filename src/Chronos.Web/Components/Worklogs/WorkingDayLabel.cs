using Chronos.Application.Calendar;
using Chronos.Domain.Entities.Calendar;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// How a day's kind is put into words on the page: a short word for the day strip, and
    /// a line for the day's header. See issue #310.
    /// </summary>
    public static class WorkingDayLabel
    {
        /// <summary>«выходной», «праздник», «отпуск»… — for a day off, where hours would say nothing.</summary>
        public static string Short(WorkingCalendarDay day) => day.Kind switch
        {
            WorkingDayKind.Holiday => "праздник",
            WorkingDayKind.Absence => Absence(day.Absence).ToLowerInvariant(),
            WorkingDayKind.Weekend => "выходной",
            _ => null
        };

        /// <summary>
        /// «Отпуск · Турция», «День России», «Сокращённый день — на час короче». Null for
        /// an ordinary working day or weekend: there is nothing to say about them.
        /// </summary>
        public static string Describe(WorkingCalendarDay day) => day.Kind switch
        {
            WorkingDayKind.Absence => string.IsNullOrEmpty(day.Title)
                ? Absence(day.Absence)
                : $"{Absence(day.Absence)} · {day.Title}",
            WorkingDayKind.Holiday => day.Title ?? "Праздник",
            WorkingDayKind.ShortDay => "Сокращённый день — на час короче",
            // A working Saturday is worth pointing out; an ordinary Tuesday is not.
            WorkingDayKind.Workday => day.Title,
            _ => null
        };

        public static string Absence(AbsenceKind? kind) => kind switch
        {
            AbsenceKind.Vacation => "Отпуск",
            AbsenceKind.SickLeave => "Больничный",
            AbsenceKind.DayOff => "Отгул",
            _ => "Отсутствие"
        };
    }
}
