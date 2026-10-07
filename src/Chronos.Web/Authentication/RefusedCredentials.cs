namespace Chronos.Web.Authentication
{
    /// <summary>
    /// The way out for a user Jira refused: signing out clears the cookie the stale
    /// credentials live in, and the login page that follows says why it is there. The
    /// reason travels as a query flag through <see cref="AuthenticationMiddleware"/>,
    /// because the sign-out is a full page load that leaves nothing else behind.
    /// See issues #305 and #322.
    /// </summary>
    public static class RefusedCredentials
    {
        public const string ReasonParameter = "reason";
        public const string Reason = "refused";

        /// <summary>Signs out and lands on the login page with the explanation.</summary>
        public const string LogoutPath = "/logout?" + ReasonParameter + "=" + Reason;

        /// <summary>Where <see cref="LogoutPath"/> ends up once the cookie is gone.</summary>
        public const string LoginPath = "/?" + ReasonParameter + "=" + Reason;

        public const string Title = "Сессия Jira недействительна";

        public const string Explanation =
            "Jira отклонила ваши учётные данные — скорее всего, изменился пароль или отозван " +
            "токен. Войдите заново: вход по personal access token переживает смену пароля.";
    }
}
