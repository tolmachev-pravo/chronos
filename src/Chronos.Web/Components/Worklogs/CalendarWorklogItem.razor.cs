using Microsoft.AspNetCore.Components;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class CalendarWorklogItem : ComponentBase
    {
        [Parameter] public WorkingDayWorklog Entity { get; set; } = default!;
        [Parameter] public bool IsLogged { get; set; }
        [Parameter] public EventCallback<WorkingDayWorklog> OnAddPressed { get; set; }
        [Parameter] public EventCallback<WorkingDayWorklog> OnMenuCreatedPressed { get; set; }

        /// <summary>
        /// The meeting's own name comes first. The issue whose key was found in it has a
        /// summary of its own, and it is not what the hour was spent on. See issue #156.
        /// </summary>
        private string Title =>
            (Entity.Details as CalendarEventDetails)?.Title
            ?? Entity.Issue?.Summary
            ?? Entity.Comment
            ?? string.Empty;

        private bool _detailsShown;

        private bool HasDetails => Entity.Details is not null;

        private string ToggleClass => _detailsShown
            ? "chr-source-toggle chr-source-toggle-open"
            : "chr-source-toggle";

        private void ToggleDetails() => _detailsShown = !_detailsShown;

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
