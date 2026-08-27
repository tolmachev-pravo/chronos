using Chronos.Domain.Entities.Users;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Users
{
    public interface IUserSettingsRepository
    {
        Task<UserSettings?> GetAsync(string username, CancellationToken ct = default);
        Task UpsertAsync(UserSettings settings, CancellationToken ct = default);
    }
}
