using Microsoft.EntityFrameworkCore;
using Chronos.Application.Calendar;
using Chronos.Domain.Entities.Calendar;
using Chronos.Infrastructure.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.WorkingDays
{
    public class CalendarDayRepository : ICalendarDayRepository
    {
        private readonly ApplicationDbContext _db;

        public CalendarDayRepository(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<CalendarDay>> GetAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var first = from.Date;
            var afterLast = to.Date.AddDays(1);

            return await _db.Set<CalendarDay>()
                .AsNoTracking()
                .Where(day => day.Date >= first && day.Date < afterLast)
                .OrderBy(day => day.Date)
                .ToListAsync(ct);
        }

        public async Task ReplaceYearAsync(int year, IReadOnlyCollection<CalendarDay> days, CancellationToken ct = default)
        {
            var first = new DateTime(year, 1, 1);
            var afterLast = first.AddYears(1);

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            await _db.Set<CalendarDay>()
                .Where(day => day.Date >= first && day.Date < afterLast)
                .ExecuteDeleteAsync(ct);

            _db.Set<CalendarDay>().AddRange(days);
            await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
    }
}
