using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Authentication;
using Chronos.Application.Storage;
using Chronos.Application.Worklogs.Dto;
using Chronos.Application.Worklogs.Queries;
using Chronos.Web.Shared;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogFilter : ComponentBase
    {
        private readonly ComponentModel _model = ComponentModel.Create();

        [Parameter] public EventCallback<GetWorklogCollection.Query> OnSearchPressed { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }
        [Inject] private IStorage<string, UserWorklogFilter> _filterStorage { get; set; }
        [Inject] private IIdentityService _identityService { get; set; }

        protected async Task Search()
        {
            try
            {
                await SaveFilterAsync();
                await OnSearchPressed.InvokeAsync(new GetWorklogCollection.Query()
                {
                    StartDate = _model.Filter.StartDate.Value,
                    EndDate = _model.Filter.EndDate.Value.AddDays(1).AddMinutes(-1),
                });
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var user = await _identityService.GetCurrentUserAsync();
            if (user != null)
            {
                var filter = await _filterStorage.GetValueAsync(user.Key);
                _model.Filter.Initialize(filter);
            }
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RenderFilterAsync();
                StateHasChanged();
            }
            else
            {
                await SaveFilterAsync();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task RenderFilterAsync()
        {
            if (_model.Filter.IsInitialized)
            {
                return;
            }
            var user = await _identityService.GetCurrentUserAsync();
            var filter = await _filterStorage.GetValueAsync(user?.Key);
            _model.Filter.Initialize(filter);
            await _filterStorage.UpdateAsync(user?.Key, filter);
        }

        private async Task SaveFilterAsync()
        {
            var user = await _identityService.GetCurrentUserAsync();
            var filter = _model.Filter.Convert();
            filter.Username = user?.Key;
            await _filterStorage.UpdateAsync(user?.Key, filter);
        }

        public class ComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }

            public FilterModel Filter { get; set; } = new FilterModel();
        }

        public class FilterModel
        {
            [Required]
            [JsonIgnore]
            public DateRange DateRange { get; set; } = new DateRange(DateTime.Now.AddDays(-7).Date, DateTime.Now.Date);

            [Required] 
            public DateTime? StartDate => DateRange.Start;

            [Required] 
            public DateTime? EndDate => DateRange.End;

            [Required] 
            public TimeSpan? DailyWorkingStartTime { get; set; } = TimeSpan.FromHours(10);

            [Required] 
            public TimeSpan? DailyWorkingEndTime { get; set; } = TimeSpan.FromHours(19);

            /// <summary>
            /// Legacy: the comment duration now lives in the Jira extension (issue #242).
            /// The value is still round-tripped through local storage so that
            /// EnsureJiraExtension can migrate it for users who set it before the move.
            /// </summary>
            public TimeSpan? CommentWorklogTime { get; set; } = TimeSpan.Zero;

            [Required]
            public TimeSpan? LunchTime { get; set; } = TimeSpan.FromHours(1);

            public bool IsInitialized { get; set; }

            public void Initialize(UserWorklogFilter filter)
            {
                if (filter != null)
                {
                    DailyWorkingStartTime = filter.DailyWorkingStartTime;
                    DailyWorkingEndTime = filter.DailyWorkingEndTime;
                    CommentWorklogTime = filter.CommentWorklogTime;
                    LunchTime = filter.LunchTime;
                }
            }

            public UserWorklogFilter Convert()
            {
                return new UserWorklogFilter
                {
                    DailyWorkingStartTime = DailyWorkingStartTime,
                    DailyWorkingEndTime = DailyWorkingEndTime,
                    CommentWorklogTime = CommentWorklogTime,
                    LunchTime = LunchTime
                };
            }
        }
    }
}
