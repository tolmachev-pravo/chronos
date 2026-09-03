using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Worklogs.Commands;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogDay : ComponentBase
    {
        [Parameter] public WorkingDay Entity { get; set; } = default!;

        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        public Color Color => Entity.IsWeekend ? Color.Error : Color.Default;

        private string DayHeaderClass
        {
            get
            {
                if (Entity.IsWeekend) return "chr-day-header chr-day-header-weekend";
                if (Entity.Progress >= 100) return "chr-day-header chr-day-header-done";
                if (Entity.Progress > 0) return "chr-day-header chr-day-header-progress";
                return "chr-day-header";
            }
        }

        private string DayDateText => Entity.Date.ToString("ddd, dd MMM");
        private string ProgressPercent => Entity.IsWeekend && Entity.Progress == 0 ? "—" : $"{Entity.Progress}%";

        private static string FormatTime(TimeSpan ts)
        {
            var hours = (int)ts.TotalHours;
            var minutes = ts.Minutes;
            if (hours == 0 && minutes == 0) return "0ч";
            if (minutes == 0) return $"{hours}ч";
            return $"{hours}ч {minutes}м";
        }

        private List<DayRow> _dayRows = new();
        private WorkingDay? _previousEntity;

        protected override void OnParametersSet()
        {
            if (_previousEntity == Entity) return;
            _previousEntity = Entity;
            RebuildDayRows();
        }

        private void RebuildDayRows()
        {
            // Actual worklogs matched to keyless blocked events are shown as children
            // of the blocked event row, so exclude them from standalone worklog rows.
            var blockedMatchedWorklogs = Entity.BlockedEvents
                .Select(e => Entity.ActualWorklogs
                    .FirstOrDefault(w => w.StartDate == e.StartDate && w.CompleteDate == e.CompleteDate))
                .Where(w => w != null)
                .ToHashSet();

            var worklogRows = Entity.ActualWorklogs
                .Where(w => w.Parent == null && !blockedMatchedWorklogs.Contains(w))
                .Select(w => new DayRow(w.RawStartDate, Worklog: w));

            var regularEstimatedRows = Entity.EstimatedWorklogs
                .Where(w => w.Source != Domain.Models.Events.EventSource.Calendar)
                .Select(w => new DayRow(w.RawStartDate, EstimatedWorklog: w));

            var calendarEstimatedRows = Entity.EstimatedWorklogs
                .Where(w => w.Source == Domain.Models.Events.EventSource.Calendar)
                .Select(w => new DayRow(w.RawStartDate, CalendarWorklog: w));

            var blockedRows = Entity.BlockedEvents.Select(e =>
            {
                var template = CreateBlockedTemplate(e);
                var matched = Entity.ActualWorklogs
                    .FirstOrDefault(w => w.StartDate == e.StartDate && w.CompleteDate == e.CompleteDate);
                if (matched != null)
                {
                    template.Children.Add(matched);
                    template.UpdateRemainingTimeSpent(TimeSpan.Zero);
                }
                return new DayRow(e.StartDate, CalendarWorklog: template, BlockedEventRef: e);
            });

            _dayRows = worklogRows
                .Concat(regularEstimatedRows)
                .Concat(calendarEstimatedRows)
                .Concat(blockedRows)
                .OrderBy(r => r.Time)
                .ToList();
        }

        private static WorkingDayWorklog CreateBlockedTemplate(IEvent e)
        {
            var template = new WorkingDayWorklog
            {
                RawStartDate = e.StartDate,
                RawCompleteDate = e.CompleteDate,
                StartDate = e.StartDate,
                CompleteDate = e.CompleteDate,
                Comment = e.Summary,
                Issue = null,
                Type = Domain.Models.Worklogs.WorklogType.Actual,
                Source = Domain.Models.Events.EventSource.Calendar
            };
            template.UpdateRemainingTimeSpent(e.Duration);
            return template;
        }

        private async Task AddWorklogAsync(WorkingDayWorklog entity)
        {
            try
            {
                await Mediator.Send(new AddWorklog.Command(AddedWorklogDto.Create(entity)));
                Entity.AddWorklog(entity);
                Snackbar.Add(
                    $"Worklog {entity.Issue.Key} added successfully!",
                    Severity.Success,
                    config => { config.ActionColor = Color.Success; });
                RebuildDayRows();
                StateHasChanged();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        private async Task CalendarAddAsync(WorkingDayWorklog worklog)
        {
            if (worklog.Issue == null || string.IsNullOrEmpty(worklog.Issue.Key))
            {
                var parameters = new DialogParameters
                {
                    { nameof(WorklogDayItemDialog.WorkingDay), Entity },
                    { nameof(WorklogDayItemDialog.WorklogTemplate), worklog }
                };
                var dialog = await DialogService.ShowAsync<WorklogDayItemDialog>("Add worklog", parameters);
                var result = await dialog.Result;
                if (result.Data is WorkingDayWorklog created)
                    await AddWorklogAsync(created);
            }
            else
            {
                await AddWorklogAsync(worklog);
            }
        }

        private record DayRow(
            DateTime Time,
            WorkingDayWorklog? Worklog = null,
            WorkingDayWorklog? EstimatedWorklog = null,
            WorkingDayWorklog? CalendarWorklog = null,
            IEvent? BlockedEventRef = null);
    }
}
