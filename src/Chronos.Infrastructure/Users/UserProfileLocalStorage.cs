using Blazored.LocalStorage;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Storage;

namespace Chronos.Infrastructure.Users
{
    public class UserProfileLocalStorage : BaseLocalStorage<UserProfile>
    {
        public UserProfileLocalStorage(ILocalStorageService localStorage) : base(localStorage)
        {
        }
    }
}
