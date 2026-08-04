using Chronos.Application.Authentication;
using Chronos.Application.Authentication.Dto;
using Chronos.Application.Storage;
using System;

namespace Chronos.Infrastructure.Authentication
{
    public class LoginMemoryCache : BaseMemoryCache<Guid, LoginDto>, ILoginMemoryCache
    {
    }
}
