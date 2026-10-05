using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Calendar
{
    /// <summary>
    /// The one place that says what a day is for a user. Asked once for a whole period,
    /// not per day. See issue #310.
    /// </summary>
    public interface IWorkingCalendar
    {
        /// <summary>Every day from <paramref name="from"/> to <paramref name="to"/>, both inclusive.</summary>
        /// <param name="includeAbsences">
        /// False to see the days as the calendar has them, under the user's absences — what
        /// planning a vacation needs: which of its days are holidays anyway.
        /// </param>
        Task<IReadOnlyDictionary<DateTime, WorkingCalendarDay>> GetDaysAsync(
            string username, DateTime from, DateTime to, CancellationToken ct = default, bool includeAbsences = true);
    }
}
