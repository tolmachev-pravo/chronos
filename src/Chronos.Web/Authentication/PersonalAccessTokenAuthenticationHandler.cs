using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Chronos.Domain.Models.Users;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ChronosAuthenticationService = Chronos.Application.Authentication.IAuthenticationService;
using BearerLoginRequest = Chronos.Application.Authentication.BearerLoginRequest;

namespace Chronos.Web.Authentication
{
    /// <summary>
    /// Authenticates a request by the caller's own Jira personal access token, sent as
    /// <c>Authorization: Bearer &lt;token&gt;</c> (issue #298). This is the way in for
    /// clients with no browser and no cookie — an MCP client, first of all.
    ///
    /// The token is never stored: it lives in <see cref="RequestUserAccessor"/> for the
    /// length of the request, and revoking it in Jira revokes the access here. What Jira
    /// answers is the username, which every scenario needs to know whose activity it is
    /// reading.
    /// </summary>
    public class PersonalAccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string AuthorizationHeader = "Authorization";
        private const string BearerPrefix = "Bearer ";

        /// <summary>
        /// A validated token is remembered briefly, so a burst of tool calls costs one
        /// round trip to Jira instead of one per call. The flip side: a token revoked in
        /// Jira keeps working here until the entry expires.
        /// </summary>
        private static readonly TimeSpan ValidationLifetime = TimeSpan.FromMinutes(5);

        private readonly IServiceProvider _requestServices;
        private readonly RequestUserAccessor _requestUserAccessor;
        private readonly IMemoryCache _validatedTokens;

        public PersonalAccessTokenAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            IServiceProvider requestServices,
            RequestUserAccessor requestUserAccessor,
            IMemoryCache validatedTokens)
            : base(options, loggerFactory, encoder)
        {
            _requestServices = requestServices;
            _requestUserAccessor = requestUserAccessor;
            _validatedTokens = validatedTokens;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(AuthorizationHeader, out var headerValues))
            {
                return AuthenticateResult.NoResult();
            }

            var header = headerValues.ToString();
            if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Some other scheme: leave the request to whoever handles it.
                return AuthenticateResult.NoResult();
            }

            var token = header.Substring(BearerPrefix.Length).Trim();
            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail("The Authorization header carries no personal access token");
            }

            var username = await ResolveUsernameAsync(token);
            if (string.IsNullOrEmpty(username))
            {
                return AuthenticateResult.Fail("Jira rejected the personal access token");
            }

            _requestUserAccessor.User = new User
            {
                Username = username,
                PersonalAccessToken = token
            };

            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, username) },
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers.WWWAuthenticate = "Bearer";
            return base.HandleChallengeAsync(properties);
        }

        /// <summary>
        /// Asks Jira who the token belongs to. The user is put into the accessor before the
        /// call, because the services that talk to Jira build their client from the current
        /// user — without it this very call would look for a Blazor circuit that a plain
        /// HTTP request does not have. For the same reason the service is resolved here and
        /// not taken in the constructor: building it eagerly resolves the user too early.
        /// </summary>
        private async Task<string> ResolveUsernameAsync(string token)
        {
            var cacheKey = CacheKey(token);
            if (_validatedTokens.TryGetValue<string>(cacheKey, out var cachedUsername))
            {
                return cachedUsername;
            }

            _requestUserAccessor.User = new User { PersonalAccessToken = token };
            try
            {
                var authenticationService = _requestServices.GetRequiredService<ChronosAuthenticationService>();
                var response = await authenticationService.LoginAsync(new BearerLoginRequest { Token = token });
                if (!response.IsSuccess || string.IsNullOrEmpty(response.Username))
                {
                    return null;
                }

                _validatedTokens.Set(cacheKey, response.Username, ValidationLifetime);
                return response.Username;
            }
            catch (Exception exception)
            {
                Logger.LogWarning(exception, "Failed to authenticate a request by personal access token");
                return null;
            }
            finally
            {
                _requestUserAccessor.User = null;
            }
        }

        /// <summary>
        /// The token is hashed so that nothing in the cache — a key included — is the secret
        /// itself.
        /// </summary>
        private static string CacheKey(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return $"{PersonalAccessTokenDefaults.AuthenticationScheme}:{Convert.ToHexString(hash)}";
        }
    }
}
