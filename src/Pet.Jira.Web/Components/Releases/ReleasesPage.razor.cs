using Microsoft.AspNetCore.Components;
using Pet.Jira.Web.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pet.Jira.Web.Components.Releases
{
    public partial class ReleasesPage : ComponentBase
    {
        [Inject] private IReleaseService ReleaseService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private IReadOnlyList<ReleaseSummary> _releases = Array.Empty<ReleaseSummary>();
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _releases = await ReleaseService.GetReleasesAsync();
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
