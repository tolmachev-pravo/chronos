using Chronos.Application.Storage;
using Chronos.Domain.Models.Users;

namespace Chronos.Application.Users
{
    public class UserProfileMemoryCache : BaseMemoryCache<string, UserProfile>
    {
    }
}
