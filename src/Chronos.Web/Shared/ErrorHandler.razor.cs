using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Chronos.Application.Authentication;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Shared
{
    public partial class ErrorHandler
    {
        /// <summary>
        /// Where a refused user is sent: signing out clears the cookie their stale
        /// credentials live in, and the login page is what follows. See issue #305.
        /// </summary>
        private const string LogoutPath = "/logout";

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ILogger<ErrorHandler> _logger { get; set; }

        public void ProcessError(Exception ex)
        {
            if (ex is JiraAuthenticationException)
            {
                ProcessAuthenticationError(ex);
                return;
            }

            var message = $"<b>{ex.Source}</b><br>{ex.Message}";
            Snackbar.Add(
                message,
                Severity.Error,
                config => { config.ActionColor = Color.Error; });
            _logger.LogError(ex, ex.Source);
        }

        /// <summary>
        /// Jira refused the credentials this session works with, so nothing on the page
        /// will work until the user signs in again. Before issue #305 this ended as a
        /// skipped event source and a day that looked complete while it was not — now it
        /// is said, together with the one thing that helps. The message stays until it is
        /// dismissed: it outlives the page the user is looking at.
        /// </summary>
        private void ProcessAuthenticationError(Exception exception)
        {
            Snackbar.Add(
                "<b>Сессия Jira недействительна</b><br>Jira отклонила ваши учётные данные — " +
                "скорее всего, изменился пароль. Войдите заново: вход по personal access " +
                "token переживает смену пароля.",
                Severity.Error,
                config =>
                {
                    config.Action = "Войти заново";
                    config.ActionColor = Color.Error;
                    config.RequireInteraction = true;
                    config.OnClick = _ =>
                    {
                        Navigation.NavigateTo(LogoutPath, forceLoad: true);
                        return Task.CompletedTask;
                    };
                });
            _logger.LogWarning(exception, "Jira refused the credentials of the current user");
        }
    }
}
