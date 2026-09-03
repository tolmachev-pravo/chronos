using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Web.Shared;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogItem : ComponentBase
    {
        [Parameter] public WorkingDayWorklog Entity { get; set; }        
        [Parameter] public EventCallback<WorkingDayWorklog> OnAddPressed { get; set; }

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        private bool _isAdding;

        private async Task AddAsync()
        {
            _isAdding = true;
            StateHasChanged();
            try
            {
                var worklog = WorkingDayWorklog.CreateActualByEstimated(Entity);
                await OnAddPressed.InvokeAsync(worklog);
            }
            finally
            {
                _isAdding = false;
            }
        }

        private async Task AddCustomAsync(WorkingDayWorklog worklog)
        {
            await OnAddPressed.InvokeAsync(worklog);
        }

        public Color Color => Entity.Source switch
		{
			EventSource.Assignee => Color.Primary,
			EventSource.Tester => Color.Secondary,
			_ => Color.Info
		};

        public string Icon => Entity.Source switch
        {
            EventSource.Assignee => Icons.Material.Filled.Assignment,
            EventSource.Tester => Icons.Material.Filled.BugReport,
            EventSource.Comment => Icons.Material.Filled.Comment,
            _ => Icons.Material.Filled.Assignment
        };
	}
}
