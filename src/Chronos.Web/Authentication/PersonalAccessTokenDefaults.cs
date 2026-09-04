namespace Chronos.Web.Authentication
{
    /// <summary>
    /// Names of the scheme and the policy that let a non-browser client — an MCP client,
    /// a script — authenticate with its own Jira personal access token (issue #298).
    /// The browser keeps using cookies; nothing here touches that path.
    /// </summary>
    public static class PersonalAccessTokenDefaults
    {
        public const string AuthenticationScheme = "PersonalAccessToken";

        /// <summary>
        /// Endpoints guarded by this policy answer 401 to a request without a valid token
        /// instead of running as nobody.
        /// </summary>
        public const string AuthorizationPolicy = "PersonalAccessToken";
    }
}
