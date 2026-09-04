using Chronos.Application.Authentication;
using Chronos.Domain.Models.Users;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Authentication
{
    /// <summary>
    /// Resolves the current user for both ways into Chronos (issue #298): a request that
    /// authenticated with a personal access token carries its user in
    /// <see cref="RequestUserAccessor"/>, everything else is a browser and comes from the
    /// Blazor circuit.
    ///
    /// The circuit service is resolved lazily on purpose: asking a
    /// <c>AuthenticationStateProvider</c> for state outside a circuit throws, and the
    /// token path must never reach it — the personal access token handler itself calls
    /// Jira through services that resolve the current user.
    /// </summary>
    public class CompositeIdentityService : IIdentityService
    {
        private readonly RequestUserAccessor _requestUserAccessor;
        private readonly Func<IIdentityService> _circuitIdentityServiceFactory;

        public CompositeIdentityService(
            RequestUserAccessor requestUserAccessor,
            Func<IIdentityService> circuitIdentityServiceFactory)
        {
            _requestUserAccessor = requestUserAccessor;
            _circuitIdentityServiceFactory = circuitIdentityServiceFactory;
        }

        public User CurrentUser => _requestUserAccessor.User ?? _circuitIdentityServiceFactory().CurrentUser;

        public Task<User> GetCurrentUserAsync()
        {
            return _requestUserAccessor.User is not null
                ? Task.FromResult(_requestUserAccessor.User)
                : _circuitIdentityServiceFactory().GetCurrentUserAsync();
        }
    }
}
