using Chronos.Application.Storage;
using Chronos.Application.Users;
using Chronos.Infrastructure.Storage;

namespace Chronos.Infrastructure.Users
{
    public class UserThemeStorage : BaseStorage<string, UserTheme>
    {
        public UserThemeStorage(
            ILocalStorage<UserTheme> localStorage,
            IMemoryCache<string, UserTheme> memoryCache) : base(localStorage, memoryCache)
        {
        }
    }
}
