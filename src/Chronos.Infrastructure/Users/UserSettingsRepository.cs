using Microsoft.EntityFrameworkCore;
using Chronos.Application.Users;
using Chronos.Domain.Entities.Users;
using Chronos.Infrastructure.Data.Contexts;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Users
{
    public class UserSettingsRepository : IUserSettingsRepository
    {
        private readonly ApplicationDbContext _db;

        public UserSettingsRepository(ApplicationDbContext db) => _db = db;

        public Task<UserSettings?> GetAsync(string username, CancellationToken ct = default)
            => _db.Set<UserSettings>()
                  .FirstOrDefaultAsync(settings => settings.Username == username, ct);

        public async Task UpsertAsync(UserSettings settings, CancellationToken ct = default)
        {
            var exists = await _db.Set<UserSettings>()
                .AnyAsync(stored => stored.Username == settings.Username, ct);

            if (exists)
                _db.Set<UserSettings>().Update(settings);
            else
                _db.Set<UserSettings>().Add(settings);

            await _db.SaveChangesAsync(ct);
        }
    }
}
