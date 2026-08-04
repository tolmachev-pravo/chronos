using Chronos.Application.Authentication.Dto;
using Chronos.Application.Storage;
using System;

namespace Chronos.Application.Authentication
{
    public interface ILoginMemoryCache : IMemoryCache<Guid, LoginDto>
    {
    }
}
