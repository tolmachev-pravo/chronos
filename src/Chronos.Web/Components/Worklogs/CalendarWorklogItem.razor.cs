using Microsoft.AspNetCore.Components;
using Chronos.Application.Worklogs.Dto;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class CalendarWorklogItem : ComponentBase
    {
        [Parameter] public WorkingDayWorklog Entity { get; set; } = default!;
        [Parameter] public bool IsLogged { get; set; }
        [Parameter] public EventCallback<WorkingDayWorklog> OnAddPressed { get; set; }
        [Parameter] public EventCallback<WorkingDayWorklog> OnMenuCreatedPressed { get; set; }

        private string Title => Entity.Issue?.Summary ?? Entity.Comment ?? string.Empty;

        private bool IsReadyToLog =>
            Entity.Issue != null &&
            !string.IsNullOrEmpty(Entity.Issue.Key) &&
            !Entity.IsEmpty;

        private bool _isAdding;

        private async Task AddAsync()
        {
            _isAdding = true;
            StateHasChanged();
            try
            {
                var worklog = IsReadyToLog
                    ? WorkingDayWorklog.CreateActualByEstimated(Entity)
                    : Entity;
                await OnAddPressed.InvokeAsync(worklog);
            }
            finally
            {
                _isAdding = false;
            }
        }
    }
}
