using Microsoft.AspNetCore.Components.Authorization;
using Chronos.Application.Authentication;
using Chronos.Application.Authentication.Dto;
using Chronos.Domain.Models.Users;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Chronos.Web.Authentication
{
    public class IdentityService : IIdentityService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public IdentityService(AuthenticationStateProvider authenticationStateProvider)
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
