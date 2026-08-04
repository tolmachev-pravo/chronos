using Chronos.Domain.Models.Users;
using System.Threading.Tasks;

namespace Chronos.Application.Authentication
{
    public interface IIdentityService
    {
        Task<User> GetCurrentUserAsync();
        User CurrentUser { get; }
    }
}
