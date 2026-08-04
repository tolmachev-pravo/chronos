using Microsoft.AspNetCore.Components;
using Chronos.Application.Worklogs.Dto;

namespace Chronos.Web.Components.Worklogs
{
    public partial class ActualWorklogItem : ComponentBase
    {
        [Parameter] public WorkingDayWorklog Entity { get; set; }
    }
}
