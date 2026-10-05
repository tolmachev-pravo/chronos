using Chronos.Domain.Entities.Calendar;
using System;

namespace Chronos.Application.Calendar
{
    /// <summary>
    /// A day as the working calendar sees it: its kind, what it is called, and the norm it
    /// leaves of a full working day. See issue #310.
    /// </summary>
    /// <param name="Title">
    /// The holiday's name, or the comment of an absence; null for an ordinary day.
    /// </param>
    /// <param name="Absence">Which absence it is; set only for <see cref="WorkingDayKind.Absence"/>.</param>
    public sealed record WorkingCalendarDay(
        DateTime Date,
        WorkingDayKind Kind,
        string Title = null,
        AbsenceKind? Absence = null)
    {
        /// <summary>A day before a holiday is this much shorter.</summary>
        public static readonly TimeSpan ShortDayReduction = TimeSpan.FromHours(1);

        public bool IsWorking => Kind is WorkingDayKind.Workday or WorkingDayKind.ShortDay;

        /// <summary>
        /// What is left of a full working day: all of it on a working day, an hour less
        /// before a holiday, nothing on a day off.
        /// </summary>
        public TimeSpan NormOf(TimeSpan workingTime) => Kind switch
        {
            WorkingDayKind.Workday => workingTime,
            WorkingDayKind.ShortDay => workingTime > ShortDayReduction
                ? workingTime - ShortDayReduction
                : TimeSpan.Zero,
            _ => TimeSpan.Zero
        };

        /// <summary>The day by its weekday alone — what a day is when nothing else is known.</summary>
        public static WorkingCalendarDay ByWeekday(DateTime date) => new(
            date.Date,
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? WorkingDayKind.Weekend
                : WorkingDayKind.Workday);
    }
}
