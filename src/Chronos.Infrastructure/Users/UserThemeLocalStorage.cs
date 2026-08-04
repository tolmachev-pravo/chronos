using Blazored.LocalStorage;
using Chronos.Application.Users;
using Chronos.Infrastructure.Storage;

namespace Chronos.Infrastructure.Users
{
    public class UserThemeLocalStorage : BaseLocalStorage<UserTheme>
    {
        public UserThemeLocalStorage(ILocalStorageService localStorage) : base(localStorage)
        {
        }
    }
}
