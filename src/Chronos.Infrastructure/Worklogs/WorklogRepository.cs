using Chronos.Application.Authentication;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Jira;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Worklogs
{
    public class WorklogRepository : IWorklogRepository
    {
        private readonly IJiraService _jiraService;
        private readonly ITimeProvider _timeProvider;
        private readonly IStorage<string, UserProfile> _userProfileStorage;
        private readonly IIdentityService _identityService;

        public WorklogRepository(
            IJiraService jiraService,
            ITimeProvider timeProvider,
            IStorage<string, UserProfile> userProfileStorage,
            IIdentityService identityService)
        {
            _jiraService = jiraService;
            _timeProvider = timeProvider;
            _userProfileStorage = userProfileStorage;
            _identityService = identityService;
        }

        public async Task AddAsync(AddedWorklogDto worklog, CancellationToken cancellationToken = default)
        {
            var user = await _identityService.GetCurrentUserAsync();
            var userProfile = await _userProfileStorage.GetValueAsync(user.Key, cancellationToken);
            worklog.StartedAt = _timeProvider.ConvertToServerTimezone(worklog.StartedAt, userProfile.TimeZoneInfo);
            await _jiraService.AddWorklogAsync(worklog);
        }
    }
}
