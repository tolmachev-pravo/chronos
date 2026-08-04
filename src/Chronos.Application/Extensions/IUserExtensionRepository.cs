using Chronos.Domain.Entities.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Extensions
{
    public interface IUserExtensionRepository
    {
        Task<UserExtension?> GetAsync(string username, ExtensionType type, CancellationToken ct = default);
        Task<IReadOnlyList<UserExtension>> GetAllAsync(string username, CancellationToken ct = default);
        Task UpsertAsync(UserExtension extension, CancellationToken ct = default);
    }
}
