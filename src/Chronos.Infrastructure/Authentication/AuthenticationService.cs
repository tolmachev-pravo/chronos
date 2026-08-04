using Chronos.Application.Authentication;
using Chronos.Infrastructure.Jira;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IJiraService _jiraService;

        public AuthenticationService(IJiraService jiraService)
        {
            _jiraService = jiraService;
        }

        public async Task<LoginResponse> LoginAsync(BasicLoginRequest request)
        {
            return await _jiraService.LoginAsync(request);
        }

        public async Task<LoginResponse> LoginAsync(BearerLoginRequest request)
        {
            return await _jiraService.LoginAsync(request);
        }
    }
}
