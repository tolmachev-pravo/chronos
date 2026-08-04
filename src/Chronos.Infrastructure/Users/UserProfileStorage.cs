using Chronos.Application.Storage;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Storage;

namespace Chronos.Infrastructure.Users
{
    public class UserProfileStorage : BaseStorage<string, UserProfile>
    {
        public UserProfileStorage(
            ILocalStorage<UserProfile> localStorage, 
            IMemoryCache<string, UserProfile> memoryCache,
            IDataSource<string, UserProfile> dataSource) : base(localStorage, memoryCache, dataSource)
        {
        }
    }
}
