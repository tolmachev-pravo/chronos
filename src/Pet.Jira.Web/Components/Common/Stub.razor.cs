using Microsoft.AspNetCore.Components;
using MudBlazor;
using Pet.Jira.Web.Common;

namespace Pet.Jira.Web.Components.Common
{
    public partial class Stub : ComponentBase
    {
        [Parameter] public Color Color { get; set; } = Color.Default;
        [Parameter] public string Icon { get; set; } = WebConstants.Icons.Favicon;
        [Parameter] public string Message { get; set; }
        [Parameter] public string ViewBox { get; set; } = "0 0 260 260";

        /// <summary>
        /// When <c>true</c>, the stub fills the whole viewport height below the app bar
        /// (standalone screens like 404). Leave <c>false</c> for empty states beneath a page
        /// header, so the stub sizes to its content and never overflows the screen.
        /// </summary>
        [Parameter] public bool FillViewport { get; set; }
    }
}
