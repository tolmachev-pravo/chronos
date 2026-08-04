using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Issues;
using Chronos.Application.Storage;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;
using Chronos.Infrastructure.Jira;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogDayItemDialog : ComponentBase
    {
        private readonly ComponentModel _model = ComponentModel.Create();
        private MudForm _form;

        [Parameter] public WorkingDay WorkingDay { get; set; }
        [Parameter] public WorkingDayWorklog WorklogTemplate { get; set; }
        [Parameter] public Color Color { get; set; } = Color.Default;
        [Parameter] public string Icon { get; set; } = Icons.Material.Filled.MoreVert;
        [Parameter] public string Label { get; set; } = "Default";

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

        [Inject] private IMemoryCache<string, Issue> IssueCache { get; set; }
		[Inject] private IIssueDataSource IssueDataSource { get; set; }

		public class ComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }

            public WorklogModel Worklog { get; set; } = new WorklogModel();
        }

        public class WorklogModel
        {
            [Required]
            public DateTime? Date { get; set; }

            [Required]
            public TimeSpan? StartTime { get; set; }

            [Required]
            public TimeSpan? CompleteTime { get; set; }

            [Required]
            public string Comment { get; set; }

            [Required]
            public Issue Issue { get; set; }

            public bool IsInitialized { get; set; }

            public void Initialize(WorkingDay workingDay, WorkingDayWorklog worklogTemplate)
            {
                if (worklogTemplate != null)
                {
                    Date = worklogTemplate.StartDate.Date;
                    StartTime = worklogTemplate.RawStartDate.TimeOfDay;
                    CompleteTime = worklogTemplate.RawCompleteDate.TimeOfDay;
                    Comment = worklogTemplate.Comment;
                    Issue = (Issue)worklogTemplate.Issue;
                }
                else if (workingDay != null)
                {
                    Date = workingDay.Date;
                    StartTime = workingDay.Settings.WorkingStartTime;
                    CompleteTime = workingDay.Settings.WorkingStartTime.Add(new TimeSpan(0,15,0));
                }
            }

            public WorkingDayWorklog Convert()
            {
                return new WorkingDayWorklog
                {
                    StartDate = Date.Value.Add(StartTime.Value),
                    CompleteDate = Date.Value.Add(CompleteTime.Value),
                    RawStartDate = Date.Value.Add(StartTime.Value),
                    RawCompleteDate = Date.Value.Add(CompleteTime.Value),
                    RemainingTimeSpent = CompleteTime.Value - StartTime.Value,
                    Issue = Issue,
                    Type = Domain.Models.Worklogs.WorklogType.Actual,
                    Source = Domain.Models.Worklogs.WorklogSource.Calendar,
                    Comment = Comment
                };
            }
        }

        protected override async Task OnInitializedAsync()
        {
            _model.Worklog.Initialize(WorkingDay, WorklogTemplate);

            if (_model.Worklog.Issue is { Key: not null, Summary: null })
            {
                var full = await IssueDataSource.GetIssueAsync(_model.Worklog.Issue.Key);
                if (full is not null)
                    _model.Worklog.Issue = full;
            }
        }

        private async Task<IEnumerable<Issue>> SearchIssuesAsync(string value, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(value)
					&& value.IsJiraKey())
                {
                    var issue = await IssueDataSource.GetIssueAsync(value, cancellationToken);
                    return issue is null
                        ? []
						: new[] { issue };
				}
                else
                {
					var issues = await IssueCache.GetValuesAsync();
					return issues.Values.OrderBy(value => value.Identifier);
				}
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
                return _model.Worklog.Issue is null
                    ? Array.Empty<Issue>()
                    : new[] { _model.Worklog.Issue };
            }
        }

        private async Task SubmitAsync()
        {
            await _form.ValidateAsync();
            if (!_form.IsValid)
                return;

            var worklog = _model.Worklog.Convert();
            MudDialog.Close(DialogResult.Ok(worklog, typeof(WorkingDayWorklog)));
        }

        void Cancel() => MudDialog.Cancel();
    }
}
