using Chronos.Domain.Models.Users;

namespace Chronos.Web.Authentication
{
    /// <summary>
    /// The user a single HTTP request acts as, when that request has no Blazor circuit
    /// behind it (issue #298). Filled in by <see cref="PersonalAccessTokenAuthenticationHandler"/>
    /// and read back by <see cref="CompositeIdentityService"/>, so every existing handler
    /// keeps resolving its user through <c>IIdentityService</c> and needs no change.
    ///
    /// Registered as scoped: the credentials live as long as the request and are never
    /// written anywhere — Chronos still keeps no store of anybody's Jira secrets.
    /// </summary>
    public class RequestUserAccessor
    {
        public User User { get; set; }
    }
}
