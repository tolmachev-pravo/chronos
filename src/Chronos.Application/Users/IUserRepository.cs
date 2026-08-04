using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Users
{
    public interface IUserRepository
    {
        Task EnsureUserExistsAsync(string username, CancellationToken cancellationToken = default);
    }
}
