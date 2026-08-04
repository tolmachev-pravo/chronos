using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    public partial class FeatureCard : ComponentBase
    {
        private const int NewThresholdDays = 14;

        [Parameter] public FeatureSummary Feature { get; set; } = default!;

        /// <summary>
        /// When <c>true</c>, the card renders as a large hero panel (used for the
        /// first feature in the bento layout): bigger icon/title, decorative glyph
        /// and tag chips.
        /// </summary>
        [Parameter] public bool Featured { get; set; }

        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [Inject] private IDialogService DialogService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private bool IsNew =>
            Feature.Metadata.Date >= DateOnly.FromDateTime(DateTime.Today).AddDays(-NewThresholdDays);

        private string CardClass =>
            Featured ? "extv-glow__card extv-glow__card--hero" : "extv-glow__card";

        private Task OpenDetailAsync() =>
            FeatureDialogs.OpenDetailAsync(Feature, FeatureCatalogService, DialogService, ErrorHandler);
    }
}
