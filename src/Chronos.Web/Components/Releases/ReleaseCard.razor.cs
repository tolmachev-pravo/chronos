using Microsoft.AspNetCore.Components;

namespace Chronos.Web.Components.Releases
{
    public partial class ReleaseCard : ComponentBase
    {
        [Parameter] public ReleaseSummary Release { get; set; } = default!;
    }
}
