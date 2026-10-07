using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Chronos.Application.Authentication;
using Chronos.Web.Authentication;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Chronos.Web.Shared
{
    public partial class ErrorHandler
    {
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

            // A plain string is shown as text, tags and all; the markup has to say it is markup,
            // and what goes into it comes from the exception, so it is encoded.
            var message = new MarkupString(
                $"<b>{WebUtility.HtmlEncode(ex.Source)}</b><br>{WebUtility.HtmlEncode(ex.Message)}");
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
                new MarkupString($"<b>{RefusedCredentials.Title}</b><br>{RefusedCredentials.Explanation}"),
                Severity.Error,
                config =>
                {
                    config.Action = "Войти заново";
                    config.ActionColor = Color.Error;
                    config.RequireInteraction = true;
                    config.OnClick = _ =>
                    {
                        Navigation.NavigateTo(RefusedCredentials.LogoutPath, forceLoad: true);
                        return Task.CompletedTask;
                    };
                });
            _logger.LogWarning(exception, "Jira refused the credentials of the current user");
        }
    }
}
