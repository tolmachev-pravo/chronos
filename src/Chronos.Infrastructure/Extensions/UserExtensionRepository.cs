using Microsoft.EntityFrameworkCore;
using Chronos.Application.Extensions;
using Chronos.Domain.Entities.Extensions;
using Chronos.Infrastructure.Data.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Extensions
{
    public class UserExtensionRepository : IUserExtensionRepository
    {
        private readonly ApplicationDbContext _db;

        public UserExtensionRepository(ApplicationDbContext db) => _db = db;

        public Task<UserExtension?> GetAsync(string username, ExtensionType type, CancellationToken ct = default)
            => _db.Set<UserExtension>()
                  .FirstOrDefaultAsync(e => e.Username == username && e.Type == type, ct);

        public async Task<IReadOnlyList<UserExtension>> GetAllAsync(string username, CancellationToken ct = default)
            => await _db.Set<UserExtension>()
                        .Where(e => e.Username == username)
                        .ToListAsync(ct);

        public async Task UpsertAsync(UserExtension extension, CancellationToken ct = default)
        {
            var exists = await _db.Set<UserExtension>()
                .AnyAsync(e => e.Username == extension.Username && e.Type == extension.Type, ct);

            if (exists)
                _db.Set<UserExtension>().Update(extension);
            else
                _db.Set<UserExtension>().Add(extension);

            await _db.SaveChangesAsync(ct);
        }
    }
}
