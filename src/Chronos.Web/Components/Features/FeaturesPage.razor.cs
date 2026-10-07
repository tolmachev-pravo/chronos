using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Interop;
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

        /// <summary>
        /// A card without its preview: 44px padding + 48px icon row + 12px + 2x32px title + 4px
        /// + 48px footer, the 5px under the card and a little slack for font metrics. Every
        /// preview line adds <see cref="PreviewLineHeight"/> (.9rem x 1.5).
        /// </summary>
        private const int CardChrome = 240;
        private const double PreviewLineHeight = 21.6;

        /// <summary>Vertical gap between two rows of cards — the .extv-glow__grid gap.</summary>
        private const int RowGap = 20;

        private const int MaxRows = 3;

        /// <summary>The shortest preview a card shows when that buys the slide another row.</summary>
        private const int CompactPreviewLines = 4;
        private const int MaxPreviewLines = 12;

        [Inject] private IFeatureCatalogService FeatureCatalogService { get; init; } = default!;
        [Inject] private IBrowserViewportService BrowserViewportService { get; init; } = default!;
        [Inject] private IResizeObserver ResizeObserver { get; init; } = default!;
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; } = default!;

        /// <summary>Feature shown in the full-width banner; excluded from the carousel.</summary>
        private FeatureSummary _hero;

        /// <summary>Everything except <see cref="_hero"/>, split into one-row slides.</summary>
        private IReadOnlyList<FeatureSummary> _rest = Array.Empty<FeatureSummary>();

        private bool _isLoading = true;
        private int _page;

        /// <summary>Cards per slide — the number of columns the current viewport fits.</summary>
        private int _columns = 1;

        /// <summary>Rows of cards per slide — as many as the height under the banner fits.</summary>
        private int _rows = 1;

        /// <summary>Lines of preview text a card shows; grows with the height a row gets.</summary>
        private int _previewLines = 4;

        /// <summary>Slide size the last completed render used; see <see cref="OnAfterRenderAsync"/>.</summary>
        private int _renderedPageSize;

        /// <summary>The frame the carousel fills, and its last measured height (0 until measured).</summary>
        private ElementReference _track;
        private bool _isTrackObserved;
        private double _trackHeight;

        private int PageSize => _columns * _rows;

        /// <summary>One slide per <see cref="_rows"/> rows of cards, so a slide never wraps further.</summary>
        private IEnumerable<FeatureSummary[]> Pages => _rest.Chunk(PageSize);

        private int PageCount => (_rest.Count + PageSize - 1) / PageSize;

        /// <summary>
        /// The fewest preview lines a card is allowed. A single card spans the whole row and fits
        /// the teaser in fewer lines, so that layout also needs less height.
        /// </summary>
        private int MinPreviewLines => _columns == 1 ? 4 : 5;

        /// <summary>
        /// A card is at its tallest with a two-line title and a full preview — both are clamped
        /// (see .feat-card__title and --feat-preview-lines), so that is a real maximum. A card that
        /// runs out of room does not scroll or grow, it clips the preview mid-line.
        /// </summary>
        private static int RowHeightFor(int previewLines) =>
            CardChrome + (int)Math.Ceiling(previewLines * PreviewLineHeight);

        /// <summary>
        /// The least height the carousel gets: one row of the shortest cards. MudCarousel positions
        /// its slides absolutely, so below this the page scrolls instead of the cards shrinking.
        /// </summary>
        private int MinTrackHeight => RowHeightFor(MinPreviewLines) + VerticalPadding;

        /// <summary>
        /// Slide padding above and below the cards. Decided by the column count alone: whether the
        /// bullets show depends on the page count, which depends on the rows worked out from this.
        /// </summary>
        private int VerticalPadding => 6 + (_columns > 1 ? 42 : 12);

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
            $"--feat-cols:{_columns};--feat-rows:{_rows};--feat-preview-lines:{_previewLines};" +
            $"--feat-side-pad:{SidePadding}px;--feat-bottom-pad:{BottomPadding}px";

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

            // The frame appears once the catalog has loaded; from then on its height follows the
            // window, and every change of it may change the rows and the preview length.
            if (!_isTrackObserved && _track.Id is not null)
            {
                _isTrackObserved = true;
                ResizeObserver.OnResized += OnTrackResized;
                var rect = await ResizeObserver.Observe(_track);
                if (rect is not null && ApplyTrackHeight(rect.Height))
                {
                    StateHasChanged();
                }
            }

            // MudCarousel counts its bullets from the MudCarouselItem children, and those only
            // register themselves while the render that changed their number is running. One more
            // render once they have, otherwise the bullets keep the previous slide count.
            if (_renderedPageSize != PageSize)
            {
                _renderedPageSize = PageSize;
                StateHasChanged();
            }
        }

        private void OnTrackResized(IDictionary<ElementReference, BoundingClientRect> changes)
        {
            if (changes.TryGetValue(_track, out var rect) && ApplyTrackHeight(rect.Height))
            {
                InvokeAsync(StateHasChanged);
            }
        }

        private bool ApplyTrackHeight(double height)
        {
            _trackHeight = height;
            return Relayout();
        }

        /// <summary>
        /// Fits the cards into the measured height: as many rows as fit with the shortest preview,
        /// then the room a row has left goes to longer previews.
        /// </summary>
        /// <returns>Whether anything the page renders has changed.</returns>
        private bool Relayout()
        {
            var rows = 1;
            var previewLines = MinPreviewLines;

            if (_trackHeight > 0)
            {
                // Another row of cards is worth more than longer previews: it is added as soon as
                // it fits with the shortest preview a card can have at all.
                var available = _trackHeight - VerticalPadding;
                var compactRow = RowHeightFor(CompactPreviewLines);
                while (rows < MaxRows && (rows + 1) * compactRow + rows * RowGap <= available)
                {
                    rows++;
                }

                var rowHeight = (available - (rows - 1) * RowGap) / rows;
                var fitting = (int)Math.Floor((rowHeight - CardChrome) / PreviewLineHeight);
                var least = rows > 1 ? CompactPreviewLines : MinPreviewLines;
                previewLines = Math.Clamp(fitting, least, MaxPreviewLines);
            }

            if (rows == _rows && previewLines == _previewLines)
            {
                return false;
            }

            _rows = rows;
            _previewLines = previewLines;
            ClampPage();
            return true;
        }

        /// <summary>Fewer cards per slide means more slides and vice versa; keep the selection in range.</summary>
        private void ClampPage()
        {
            if (PageCount > 0)
            {
                _page = Math.Clamp(_page, 0, PageCount - 1);
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

            // The column count also moves the shortest preview and the slide padding.
            Relayout();
            ClampPage();

            return InvokeAsync(StateHasChanged);
        }

        public async ValueTask DisposeAsync()
        {
            await BrowserViewportService.UnsubscribeAsync(this);
            ResizeObserver.OnResized -= OnTrackResized;
            await ResizeObserver.DisposeAsync();
        }

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
