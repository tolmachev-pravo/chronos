using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Features
{
    public partial class FeaturesPage : ComponentBase, IBrowserViewportObserver, IAsyncDisposable
    {
        /// <summary>Card width the grid is designed around, and the gap between cards.</summary>
        private const int CardWidth = 300;
        private const int CardGap = 20;

        /// <summary>Width taken away from the viewport: the drawer (see --mud-drawer-width-left) and the page margins.</summary>
        private const int DrawerWidth = 180;
        private const int PageMargins = 32;

        /// <summary>Breakpoint at which MudDrawer with Responsive behaviour stays open.</summary>
        private const int DrawerBreakpoint = 960;

        private const int MaxColumns = 5;

        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [Inject] private IBrowserViewportService BrowserViewportService { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        /// <summary>Feature shown in the full-width banner; excluded from the carousel.</summary>
        private FeatureSummary _hero;

        /// <summary>Everything except <see cref="_hero"/>, split into one-row slides.</summary>
        private IReadOnlyList<FeatureSummary> _rest = Array.Empty<FeatureSummary>();

        private bool _isLoading = true;
        private int _page;

        /// <summary>Cards per slide — the number of columns the current viewport fits.</summary>
        private int _columns = 1;

        /// <summary>Column count the last completed render used; see <see cref="OnAfterRenderAsync"/>.</summary>
        private int _renderedColumns;

        /// <summary>One slide per row of cards, so a slide never wraps onto a second row.</summary>
        private IEnumerable<FeatureSummary[]> Pages => _rest.Chunk(_columns);

        private int PageCount => (_rest.Count + _columns - 1) / _columns;

        /// <summary>
        /// Lines of preview text a card shows. A single card spans the whole row and fits the
        /// teaser in fewer lines, so that layout also needs less height.
        /// </summary>
        private int PreviewLines => _columns == 1 ? 4 : 5;

        /// <summary>
        /// MudCarousel positions its slides absolutely, so the track needs an explicit height.
        /// A card is at its tallest with a two-line title and a full preview — both are clamped
        /// (see .feat-card__title and --feat-preview-lines), so that is a real maximum:
        /// 44px padding + 48px icon row + 12px + 2x32px title + 4px + PreviewLines x 21.6px
        /// + 48px footer, plus the slide padding and the 5px under the card. Rounded up with a
        /// little slack for font metrics: a card that runs out of room does not scroll or grow,
        /// it clips the preview mid-line.
        /// </summary>
        private int TrackHeight => _columns == 1 ? 344 : 396;

        /// <summary>
        /// Overlaid arrows on a one-card slide leave the card no width on a phone, and a bullet
        /// per article turns into a dozen dots. That layout gets the counter row instead, and the
        /// swipe gesture, which MudCarousel provides either way.
        /// </summary>
        private bool ShowOverlayControls => PageCount > 1 && _columns > 1;

        /// <summary>Compact "current of total" under the track, in place of the bullets.</summary>
        private bool ShowCounter => PageCount > 1 && _columns == 1;

        /// <summary>Room the slide leaves for the arrows and the bullets it actually shows.</summary>
        private int SidePadding => ShowOverlayControls ? 46 : 10;
        private int BottomPadding => ShowOverlayControls ? 42 : 12;

        private string CarouselStyle =>
            $"--feat-cols:{_columns};--feat-preview-lines:{PreviewLines};" +
            $"--feat-side-pad:{SidePadding}px;--feat-bottom-pad:{BottomPadding}px;height:{TrackHeight}px";

        Guid IBrowserViewportObserver.Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Breakpoints alone are too coarse here: "lg" spans 1280–1920px, where the row goes
        /// from three cards to five. Report every resize (throttled) and measure the width.
        /// One instance, not a fresh one per read: the service keeps its JS listener per options.
        /// </summary>
        ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
        {
            ReportRate = 200,
            NotifyOnBreakpointOnly = false
        };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var features = await FeatureCatalogService.GetFeaturesAsync();
                _hero = features.FirstOrDefault();
                _rest = features.Skip(1).ToList();
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
            }

            // MudCarousel counts its bullets from the MudCarouselItem children, and those only
            // register themselves while the render that changed their number is running. One more
            // render once they have, otherwise the bullets keep the previous slide count.
            if (_renderedColumns != _columns)
            {
                _renderedColumns = _columns;
                StateHasChanged();
            }
        }

        public Task NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
        {
            var columns = ColumnsFor(browserViewportEventArgs.BrowserWindowSize.Width);
            if (columns == _columns)
            {
                return Task.CompletedTask;
            }

            _columns = columns;

            // Fewer columns means more slides and vice versa; keep the selection in range.
            if (PageCount > 0)
            {
                _page = Math.Clamp(_page, 0, PageCount - 1);
            }

            return InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync() => await BrowserViewportService.UnsubscribeAsync(this);

        private void ShowPage(int page) => _page = Math.Clamp(page, 0, PageCount - 1);

        private void ShowPreviousPage() => ShowPage(_page - 1);

        private void ShowNextPage() => ShowPage(_page + 1);

        /// <summary>
        /// How many cards fit in one row — the same arithmetic
        /// <c>repeat(auto-fill, minmax(300px, 1fr))</c> does, on the width left over
        /// once the drawer and the page margins are taken out.
        /// </summary>
        private static int ColumnsFor(int viewportWidth)
        {
            var drawer = viewportWidth >= DrawerBreakpoint ? DrawerWidth : 0;
            var available = viewportWidth - drawer - PageMargins;
            var columns = (available + CardGap) / (CardWidth + CardGap);

            return Math.Clamp(columns, 1, MaxColumns);
        }
    }
}
