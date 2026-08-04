using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Chronos.Application.Users;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Chronos.Web.Authentication
{
    public class UserProvisioningMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserProvisioningMiddleware> _logger;
        private readonly ConcurrentDictionary<string, byte> _provisionedUsernames = new();

        public UserProvisioningMiddleware(
            RequestDelegate next,
            ILogger<UserProvisioningMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
        {
            var username = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity.Name
                : null;

            if (!string.IsNullOrEmpty(username) && !_provisionedUsernames.ContainsKey(username))
            {
                try
                {
                    await userRepository.EnsureUserExistsAsync(username, context.RequestAborted);
                    _provisionedUsernames.TryAdd(username, 0);
                }
                catch (Exception exception)
                {
                    // Username намеренно НЕ кладём в кэш — при ошибке попытка повторится на следующем запросе.
                    _logger.LogError(exception, "Failed to provision user {Username} in the database", username);
                }
            }

            await _next(context);
        }
    }
}
