using Microsoft.AspNetCore.Components.Authorization;
using Chronos.Application.Authentication;
using Chronos.Application.Authentication.Dto;
using Chronos.Domain.Models.Users;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Chronos.Web.Authentication
{
    /// <summary>
    /// The user behind a Blazor circuit: credentials come from the authentication cookie
    /// claims the browser signed in with. Only usable where a circuit exists, hence the
    /// name — a plain HTTP request is served by <see cref="CompositeIdentityService"/>
    /// from <see cref="RequestUserAccessor"/> instead (issue #298).
    /// </summary>
    public class CircuitIdentityService : IIdentityService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public CircuitIdentityService(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        public User CurrentUser => GetCurrentUserAsync().GetAwaiter().GetResult();

        public async Task<User> GetCurrentUserAsync()
        {
            var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authenticationState.User;
            return user.Identity.IsAuthenticated
                ? new User
                {
                    Username = user.Identity.Name,
                    Password = user.Claims
                        .FirstOrDefault(claim => claim.Type == ClaimTypes.UserData)?
                        .Value,
                    PersonalAccessToken = user.Claims
                        .FirstOrDefault(claim => claim.Type == nameof(LoginDto.PersonalAccessToken))?
                        .Value
                }
                : null;
        }
    }
}
