using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Worklogs;
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
			WorklogSource.Assignee => Color.Primary,
			WorklogSource.Tester => Color.Secondary,
			_ => Color.Info
		};

        public string Icon => Entity.Source switch
        {
            WorklogSource.Assignee => Icons.Material.Filled.Assignment,
            WorklogSource.Tester => Icons.Material.Filled.BugReport,
            WorklogSource.Comment => Icons.Material.Filled.Comment,
            _ => Icons.Material.Filled.Assignment
        };
	}
}
