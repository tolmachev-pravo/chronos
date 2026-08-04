using Microsoft.AspNetCore.Components;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    public partial class LatestFeaturesViewer : ComponentBase
    {
        private const int MaxFeatures = 3;

        /// <summary>
        /// Shown when there are no features to display.
        /// </summary>
        [Parameter] public string FallbackMessage { get; set; } = string.Empty;

        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private IReadOnlyList<FeatureSummary> _features = Array.Empty<FeatureSummary>();
        private bool _isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var features = await FeatureCatalogService.GetFeaturesAsync();
                _features = features.Take(MaxFeatures).ToList();
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
