using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Extensions.Jira.Commands;
using Chronos.Application.Extensions.Jira.Dto;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Extensions.Jira
{
    public partial class JiraExtensionSettingsDialog : ComponentBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string Username { get; set; } = string.Empty;
        [Parameter] public JiraExtensionSettingsDto Settings { get; set; } = JiraExtensionSettingsDto.Default;

        /// <summary>
        /// Current state of the extension. Saving settings must not connect a disconnected
        /// extension, so the value is passed through to the upsert unchanged.
        /// </summary>
        [Parameter] public bool IsEnabled { get; set; }

        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private bool _assigneeEventsEnabled;
        private bool _commentEventsEnabled;
        private bool _testerEventsEnabled;
        private TimeSpan? _commentWorklogTime;

        protected override void OnParametersSet()
        {
            _assigneeEventsEnabled = Settings.AssigneeEventsEnabled;
            _commentEventsEnabled = Settings.CommentEventsEnabled;
            _testerEventsEnabled = Settings.TesterEventsEnabled;
            _commentWorklogTime = Settings.CommentWorklogTime;
        }

        private async Task Save()
        {
            var settings = new JiraExtensionSettingsDto(
                _assigneeEventsEnabled,
                _commentEventsEnabled,
                _commentWorklogTime ?? TimeSpan.Zero,
                _testerEventsEnabled);

            await Mediator.Send(new UpsertJiraExtension.Command(Username, settings, IsEnabled));
            Snackbar.Add("Настройки Jira сохранены", Severity.Success);
            MudDialog.Close(DialogResult.Ok(settings));
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
