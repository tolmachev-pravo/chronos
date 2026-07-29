using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Extensions.Jira.Commands;
using Pet.Jira.Application.Extensions.Jira.Dto;
using Pet.Jira.Application.Extensions.Jira.Queries;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Components.Extensions.Jira
{
    public partial class JiraExtensionCard : ComponentBase
    {
        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IIdentityService IdentityService { get; set; } = default!;

        [Parameter] public EventCallback<bool> StateChanged { get; set; }

        private bool _isEnabled;
        private JiraExtensionSettingsDto _settings = JiraExtensionSettingsDto.Default;
        private string _username = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            _username = IdentityService.CurrentUser?.Username ?? string.Empty;
            var extension = await Mediator.Send(new GetJiraExtension.Query(_username));
            _isEnabled = extension.IsEnabled;
            _settings = extension.Settings;
            await NotifyStateChangedAsync();
        }

        private async Task NotifyStateChangedAsync()
        {
            if (StateChanged.HasDelegate)
                await StateChanged.InvokeAsync(_isEnabled);
        }

        private async Task OnToggleChangedAsync(bool value)
        {
            await Mediator.Send(new UpsertJiraExtension.Command(_username, _settings, value));
            _isEnabled = value;
            await NotifyStateChangedAsync();
        }

        private async Task OpenSettingsDialog()
        {
            var parameters = new DialogParameters
            {
                { nameof(JiraExtensionSettingsDialog.Username), _username },
                { nameof(JiraExtensionSettingsDialog.Settings), _settings },
                { nameof(JiraExtensionSettingsDialog.IsEnabled), _isEnabled }
            };
            var dialog = await DialogService.ShowAsync<JiraExtensionSettingsDialog>(
                "Jira — настройки событий", parameters);
            var result = await dialog.Result;
            if (!result.Canceled && result.Data is JiraExtensionSettingsDto saved)
            {
                _settings = saved;
                StateHasChanged();
            }
        }
    }
}
