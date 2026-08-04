using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogTimeRange : ComponentBase
    {
        [Parameter] public DateTime StartDate { get; set; }
        [Parameter] public DateTime CompleteDate { get; set; }
        [Parameter] public Color Color { get; set; } = Color.Info;
        [Parameter] public Variant Variant { get; set; } = Variant.Filled;

        private readonly string _defaultTimeFormat = "HH:mm";
    }
}
