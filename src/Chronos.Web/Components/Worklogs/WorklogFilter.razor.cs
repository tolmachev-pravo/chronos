using Microsoft.AspNetCore.Components;
using MudBlazor;
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
