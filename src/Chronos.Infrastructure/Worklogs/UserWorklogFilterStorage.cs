using Chronos.Application.Storage;
using Chronos.Application.Worklogs.Dto;
using Chronos.Infrastructure.Storage;

namespace Chronos.Infrastructure.Worklogs
{
    public class UserWorklogFilterStorage : BaseStorage<string, UserWorklogFilter>
    {
        public UserWorklogFilterStorage(
            ILocalStorage<UserWorklogFilter> localStorage,
            IMemoryCache<string, UserWorklogFilter> memoryCache) : base(localStorage, memoryCache)
        {
        }
    }
}
