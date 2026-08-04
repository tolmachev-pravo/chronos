using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Worklogs.Dto;
using Chronos.Web.Shared;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogDayMenu : ComponentBase
    {
        [Parameter] public EventCallback<WorkingDayWorklog> OnCreatedPressed { get; set; }
        [Parameter] public WorkingDay Entity { get; set; }
        [Parameter] public Color Color { get; set; } = Color.Default;
        [Parameter] public string Icon { get; set; } = Icons.Material.Filled.MoreVert;
        [Parameter] public string Label { get; set; } = "Default";

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        [Inject] IDialogService DialogService { get; set; }

        private async Task AddCustomWorklog()
        {
            var options = new DialogOptions { };
            var parameters = new DialogParameters
            {
                { "WorkingDay", Entity }
            };
            var dialog = await DialogService.ShowAsync<WorklogDayItemDialog>("New worklog", parameters, options);
            var result = await dialog.Result;
            if (result.Data is WorkingDayWorklog worklog)
            {
                await OnCreatedPressed.InvokeAsync(worklog);
            }            
        }
    }
}
