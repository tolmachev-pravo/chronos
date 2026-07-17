using Microsoft.AspNetCore.Components;

namespace Pet.Jira.Web.Components.Releases
{
    public partial class ReleaseCard : ComponentBase
    {
        [Parameter] public ReleaseSummary Release { get; set; } = default!;
    }
}
