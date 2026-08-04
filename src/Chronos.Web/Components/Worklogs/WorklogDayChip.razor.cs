using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogDayChip : ComponentBase
    {
        [Parameter] public Color Color { get; set; } = Color.Default;
        [Parameter] public string Caption { get; set; }
        [Parameter] public string Value { get; set; }
        [Parameter] public Variant Variant { get; set; } = Variant.Outlined;
        [Parameter] public Size Size { get; set; } = Size.Small;
    }
}
