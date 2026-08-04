using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Worklogs.Dto;
using Chronos.Web.Shared;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class CalendarWorklogMenu : ComponentBase
    {
        [Parameter] public WorkingDayWorklog Entity { get; set; } = default!;
        [Parameter] public EventCallback<WorkingDayWorklog> OnCreatedPressed { get; set; }
        [Parameter] public Color Color { get; set; } = Color.Tertiary;

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        [Inject] private IDialogService DialogService { get; set; } = default!;

        private async Task CreateWorklogAsync()
        {
            var parameters = new DialogParameters
            {
                { nameof(WorklogDayItemDialog.WorklogTemplate), Entity }
            };
            var dialog = await DialogService.ShowAsync<WorklogDayItemDialog>("Add worklog", parameters);
            var result = await dialog.Result;
            if (result?.Data is WorkingDayWorklog created)
                await OnCreatedPressed.InvokeAsync(created);
        }
    }
}
