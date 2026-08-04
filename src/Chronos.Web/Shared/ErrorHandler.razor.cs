using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System;

namespace Chronos.Web.Shared
{
    public partial class ErrorHandler
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] private ILogger<ErrorHandler> _logger { get; set; }

        public void ProcessError(Exception ex)
        {
            var message = $"<b>{ex.Source}</b><br>{ex.Message}";
            Snackbar.Add(
                message,
                Severity.Error,
                config => { config.ActionColor = Color.Error; });
            _logger.LogError(ex, ex.Source);
        }
    }
}
