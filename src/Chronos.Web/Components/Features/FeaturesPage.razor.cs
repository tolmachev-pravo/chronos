using Microsoft.AspNetCore.Components;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    public partial class FeaturesPage : ComponentBase
    {
        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private IReadOnlyList<FeatureSummary> _features = Array.Empty<FeatureSummary>();
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                _features = await FeatureCatalogService.GetFeaturesAsync();
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
