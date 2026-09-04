using System;
using System.Security.Authentication;

namespace Chronos.Application.Authentication
{
    /// <summary>
    /// Jira refused the credentials of the current user: the password kept in their
    /// authentication cookie no longer matches the one in Jira, or the personal access
    /// token a client request came with has been revoked.
    ///
    /// It is told apart from every other failure because nothing works after it — every
    /// scenario reads Jira as the user — and because the way out is not a retry but a
    /// new sign-in. A source that merely fell over is skipped and the day is still
    /// assembled; a refused user is told, instead of being handed a silently emptier
    /// day. See issue #305.
    /// </summary>
    public class JiraAuthenticationException : Exception
    {
        private const string DefaultMessage = "Jira refused the credentials of the current user";

        public JiraAuthenticationException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }

        /// <summary>
        /// True when a refusal by Jira is what really happened. The Jira client answers a
        /// 401 with <see cref="AuthenticationException"/>, which reaches the caller
        /// wrapped in whatever awaited it, so the whole chain is looked at. What this
        /// class already describes is not described twice — nested scenarios would
        /// otherwise wrap the same refusal once per level.
        /// </summary>
        public static bool Describes(Exception exception)
        {
            if (exception is JiraAuthenticationException)
            {
                return false;
            }

            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is AuthenticationException)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
