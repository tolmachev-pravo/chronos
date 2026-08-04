using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Full-width horizontal hero banner for the most prominent feature at the top
    /// of the catalog. Shares the click-to-open-detail behaviour with
    /// <see cref="FeatureCard"/> via <see cref="FeatureDialogs"/>.
    /// </summary>
    public partial class FeatureHero : ComponentBase
    {
        private const int NewThresholdDays = 14;

        [Parameter] public FeatureSummary Feature { get; set; } = default!;

        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [Inject] private IDialogService DialogService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private bool IsNew =>
            Feature.Metadata.Date >= DateOnly.FromDateTime(DateTime.Today).AddDays(-NewThresholdDays);

        private Task OpenDetailAsync() =>
            FeatureDialogs.OpenDetailAsync(Feature, FeatureCatalogService, DialogService, ErrorHandler);
    }
}
