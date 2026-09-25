using Microsoft.AspNetCore.Components;
using Chronos.Application.Worklogs.Dto;
using System.Collections.Generic;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogList : ComponentBase
    {
        private readonly ComponentModel Model = ComponentModel.Create();

        [Parameter] public IEnumerable<WorkingDay> Items { get; set; }

        /// <summary>Raised when a day changed in place, after a worklog was added to it.</summary>
        [Parameter] public EventCallback<WorkingDay> OnDayChanged { get; set; }

        private class ComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }
        }
    }
}
