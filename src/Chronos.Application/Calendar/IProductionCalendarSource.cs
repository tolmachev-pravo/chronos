using Chronos.Domain.Entities.Calendar;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar
{
    /// <summary>
    /// Where the production calendar comes from. It is read into our own table, so a source
    /// that is down never stops a day from loading. See issue #310.
    /// </summary>
    public interface IProductionCalendarSource
    {
        /// <summary>The year's exceptions to the weekday rule, or null while the year is not published.</summary>
        Task<IReadOnlyList<CalendarDay>> GetYearAsync(int year, CancellationToken ct = default);
    }
}
