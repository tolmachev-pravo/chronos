using Microsoft.AspNetCore.Components;
using Pet.Jira.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Components.Releases
{
    public partial class ReleasesPage : ComponentBase
    {
        [Inject] private IReleaseService ReleaseService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private ReleasesResult _result = new();
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _result = await ReleaseService.GetReleasesAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
