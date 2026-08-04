using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Chronos.Web.Components.Features
{
    public partial class FeatureDetailDialog : ComponentBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public FeatureDetail Detail { get; set; } = default!;

        private void Close() => MudDialog.Close();
    }
}
