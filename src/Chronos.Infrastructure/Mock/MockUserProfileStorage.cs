using Chronos.Application.Storage;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Storage;
using Chronos.Infrastructure.Users;

namespace Chronos.Infrastructure.Mock
{
    internal class MockUserProfileStorage : UserProfileStorage
    {
        public MockUserProfileStorage(
            ILocalStorage<UserProfile> localStorage, 
            IMemoryCache<string, UserProfile> memoryCache) : base(localStorage, memoryCache, null)
        {
        }
    }
}
