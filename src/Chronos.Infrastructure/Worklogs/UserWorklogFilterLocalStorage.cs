using Blazored.LocalStorage;
using Chronos.Application.Worklogs.Dto;
using Chronos.Infrastructure.Storage;

namespace Chronos.Infrastructure.Worklogs
{
    public class UserWorklogFilterLocalStorage : BaseLocalStorage<UserWorklogFilter>
    {
        public UserWorklogFilterLocalStorage(ILocalStorageService localStorage) : base(localStorage)
        {
        }
    }
}
