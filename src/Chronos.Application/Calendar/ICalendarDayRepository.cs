using Chronos.Domain.Entities.Calendar;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar
{
    public interface ICalendarDayRepository
    {
        /// <summary>The exceptions from <paramref name="from"/> to <paramref name="to"/>, both inclusive.</summary>
        Task<IReadOnlyList<CalendarDay>> GetAsync(DateTime from, DateTime to, CancellationToken ct = default);

        /// <summary>Puts <paramref name="days"/> in place of whatever the year held.</summary>
        Task ReplaceYearAsync(int year, IReadOnlyCollection<CalendarDay> days, CancellationToken ct = default);
    }
}
