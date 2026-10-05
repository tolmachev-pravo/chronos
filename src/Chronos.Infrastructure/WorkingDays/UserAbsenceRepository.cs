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
    public class UserAbsenceRepository : IUserAbsenceRepository
    {
        private readonly ApplicationDbContext _db;

        public UserAbsenceRepository(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<UserAbsence>> GetAsync(
            string username, DateTime from, DateTime to, CancellationToken ct = default)
        {
            var first = from.Date;
            var last = to.Date;

            // Absences are stored as dates without time, so an interval touches the period
            // when it starts no later than its last day and ends no earlier than its first.
            return await _db.Set<UserAbsence>()
                .AsNoTracking()
                .Where(absence => absence.Username == username
                    && absence.StartDate <= last
                    && absence.EndDate >= first)
                .OrderBy(absence => absence.StartDate)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<UserAbsence>> ListAsync(string username, CancellationToken ct = default)
        {
            return await _db.Set<UserAbsence>()
                .AsNoTracking()
                .Where(absence => absence.Username == username)
                .OrderByDescending(absence => absence.StartDate)
                .ToListAsync(ct);
        }

        public async Task AddAsync(UserAbsence absence, CancellationToken ct = default)
        {
            _db.Set<UserAbsence>().Add(absence);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> UpdateAsync(UserAbsence absence, CancellationToken ct = default)
        {
            var updated = await _db.Set<UserAbsence>()
                .Where(stored => stored.Id == absence.Id && stored.Username == absence.Username)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(stored => stored.StartDate, absence.StartDate)
                    .SetProperty(stored => stored.EndDate, absence.EndDate)
                    .SetProperty(stored => stored.Kind, absence.Kind)
                    .SetProperty(stored => stored.Comment, absence.Comment)
                    .SetProperty(stored => stored.UpdatedAt, absence.UpdatedAt), ct);
            return updated > 0;
        }

        public async Task<bool> DeleteAsync(string username, Guid id, CancellationToken ct = default)
        {
            var deleted = await _db.Set<UserAbsence>()
                .Where(absence => absence.Id == id && absence.Username == username)
                .ExecuteDeleteAsync(ct);
            return deleted > 0;
        }
    }
}
