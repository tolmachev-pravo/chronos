using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    /// <summary>
    /// News on the page users actually open. What is new for this browser gets the
    /// attention; once the user has seen it, the widget goes quiet. «Seen» is one date —
    /// the newest feature the user dismissed — so anything published later is new again.
    /// </summary>
    public partial class LatestFeaturesViewer : ComponentBase
    {
        /// <summary>
        /// How many features the widget shows — the head of the same list the catalog page
        /// renders, so both agree on what comes first.
        /// </summary>
        private const int MaxFeatures = 3;

        /// <summary>
        /// On a first visit the whole catalog is not news: only what was published within
        /// this many days counts as unread.
        /// </summary>
        private const int UnreadWindowDays = 30;

        private const string SeenStorageKey = "chronos.features.seen";

        /// <summary>
        /// Shown when there are no features to display.
        /// </summary>
        [Parameter] public string FallbackMessage { get; set; } = string.Empty;

        /// <summary>
        /// A small menu instead of the cards, for the status line of a page busy with the
        /// days: it takes no room of its own.
        /// </summary>
        [Parameter] public bool Inline { get; set; }

        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; init; } = default!;
        [Inject] private IDialogService DialogService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        private IReadOnlyList<FeatureSummary> _features = Array.Empty<FeatureSummary>();
        private DateOnly? _seen;
        private bool _isReady;

        /// <summary>Unread features, newest first.</summary>
        private IReadOnlyList<FeatureSummary> Unread
        {
            get
            {
                var since = _seen ?? DateOnly.FromDateTime(DateTime.Today).AddDays(-UnreadWindowDays);
                return _features
                    .Where(feature => feature.Metadata.Date > since)
                    .OrderByDescending(feature => feature.Metadata.Date)
                    .ToList();
            }
        }

        /// <summary>
        /// Local storage is reachable only once the circuit is up, so both reads wait for
        /// the first render.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            try
            {
                _features = await FeatureCatalogService.GetFeaturesAsync();
                _seen = await ReadSeenAsync();
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
            finally
            {
                _isReady = true;
                StateHasChanged();
            }
        }

        private async Task<DateOnly?> ReadSeenAsync()
        {
            try
            {
                var value = await LocalStorage.GetItemAsStringAsync(SeenStorageKey);
                return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var seen)
                    ? seen
                    : null;
            }
            catch (Exception)
            {
                // A blocked or cleared storage only means everything recent is new again.
                return null;
            }
        }

        private async Task MarkSeenAsync()
        {
            if (_features.Count == 0)
                return;

            _seen = _features.Max(feature => feature.Metadata.Date);
            try
            {
                await LocalStorage.SetItemAsStringAsync(SeenStorageKey, _seen.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                // Not remembered in this browser; the widget is still quiet until reload.
            }
        }

        private async Task OpenAsync(FeatureSummary feature)
        {
            await FeatureDialogs.OpenDetailAsync(feature, FeatureCatalogService, DialogService, ErrorHandler);
            if (Unread.Count == 1)
            {
                await MarkSeenAsync();
                StateHasChanged();
            }
        }
    }
}
