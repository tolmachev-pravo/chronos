using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Authentication;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Queries;
using Chronos.Web.Shared;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// Asks for the period alone: the working day is a profile setting now (issue #241),
    /// so there is nothing here left to remember between searches.
    /// </summary>
    public partial class WorklogFilter : ComponentBase
    {
        private readonly ComponentModel _model = ComponentModel.Create();

        [Parameter] public EventCallback<GetWorklogCollection.Query> OnSearchPressed { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }
        [Inject] private IMediator Mediator { get; set; }
        [Inject] private IIdentityService IdentityService { get; set; }

        private UserSettingsDto _workingDay = UserSettingsDto.Default;

        /// <summary>
        /// The working day the search will use, shown next to the period so the numbers in
        /// the result have a visible source (issue #241).
        /// </summary>
        private string WorkingDayDisplay =>
            $"{Time(_workingDay.WorkingStartTime)}–{Time(_workingDay.WorkingEndTime)} · обед {Duration(_workingDay.LunchTime)}";

        private static string Time(TimeSpan value) => value.ToString(@"hh\:mm");

        private static string Duration(TimeSpan value)
        {
            if (value == TimeSpan.Zero)
                return "нет";
            if (value.Minutes == 0)
                return $"{(int)value.TotalHours} ч";
            if (value.TotalHours < 1)
                return $"{value.Minutes} мин";
            return $"{(int)value.TotalHours} ч {value.Minutes} мин";
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var user = await IdentityService.GetCurrentUserAsync();
                _workingDay = await Mediator.Send(new GetUserSettings.Query(user?.Username));
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        protected async Task Search()
        {
            try
            {
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
            public DateRange DateRange { get; set; } = new DateRange(DateTime.Now.AddDays(-7).Date, DateTime.Now.Date);

            [Required]
            public DateTime? StartDate => DateRange.Start;

            [Required]
            public DateTime? EndDate => DateRange.End;
        }
    }
}
