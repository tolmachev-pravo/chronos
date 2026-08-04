using Chronos.Application.Storage;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Users
{
    internal class UserProfileDataSource : IDataSource<string, UserProfile>
    {
        private readonly IJiraService _jiraService;

        public UserProfileDataSource(
            IJiraService jiraService)
        {
            _jiraService = jiraService;
        }

        public async Task<UserProfile> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var user = await _jiraService.GetCurrentUserAsync(cancellationToken);
            return user?.ConvertToUserProfile();
        }
    }
}
