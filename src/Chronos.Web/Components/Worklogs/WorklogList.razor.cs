using Microsoft.AspNetCore.Components;
using Chronos.Application.Worklogs.Dto;
using System.Collections.Generic;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogList : ComponentBase
    {
        private readonly ComponentModel Model = ComponentModel.Create();

        [Parameter] public IEnumerable<WorkingDay> Items { get; set; }

        private class ComponentModel
        {
            public static ComponentModel Create()
            {
                return new ComponentModel();
            }
        }
    }
}
