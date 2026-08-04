using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// Shared helper for opening a feature's detail dialog. Used by both
    /// <see cref="FeatureCard"/> and <see cref="FeatureHero"/> so the load-and-show
    /// logic lives in one place.
    /// </summary>
    internal static class FeatureDialogs
    {
        public static async Task OpenDetailAsync(
            FeatureSummary feature,
            IFeatureCatalogService catalog,
            IDialogService dialogService,
            ErrorHandler errorHandler)
        {
            try
            {
                var detail = await catalog.GetFeatureAsync(feature.Metadata.Id);
                if (detail is null)
                {
                    return;
                }

                var parameters = new DialogParameters
                {
                    { nameof(FeatureDetailDialog.Detail), detail }
                };
                var options = new DialogOptions
                {
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true,
                    CloseButton = true
                };
                await dialogService.ShowAsync<FeatureDetailDialog>(detail.Metadata.Title, parameters, options);
            }
            catch (Exception e)
            {
                errorHandler.ProcessError(e);
            }
        }
    }
}
