using Blazored.LocalStorage;
using MediatR;
using Microsoft.AspNetCore.Components;
using Chronos.Application.Authentication;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Dto;
using Chronos.Web.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// A period is read from Jira and the calendar on every search — nothing is stored on
    /// our side — so the page never reads one on its own. Choosing a period only selects
    /// it; «Показать» reads it, lists each source while they answer, and can be cancelled.
    /// A new search cancels the one still running.
    /// </summary>
    public partial class WorklogListPage : ComponentBase, IDisposable
    {
        /// <summary>
        /// Which of the offered periods the user read last. Someone who closes the
        /// previous week every Monday finds it selected, so «Показать» is one click.
        /// </summary>
        private const string LastKindStorageKey = "chronos.worklogs.last-period";

        [Inject] private IMediator Mediator { get; set; }
        [Inject] private IIdentityService IdentityService { get; set; }
        [Inject] private ILocalStorageService LocalStorage { get; set; }
        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        private UserSettingsDto _settings = UserSettingsDto.Default;
        private WorklogPeriod _selected = WorklogPeriod.LastWeek(DateTime.Today);

        /// <summary>The search running now.</summary>
        private WorklogLoadLog _loadingLog;

        /// <summary>The search whose days are on screen.</summary>
        private WorklogLoadLog _loadedLog;
        private IEnumerable<WorkingDay> _items;

        private CancellationTokenSource _search;

        private bool IsLoading => _loadingLog is not null;

        private WorklogLoadLog StatusLog => _loadingLog ?? _loadedLog;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var user = await IdentityService.GetCurrentUserAsync();
                _settings = await Mediator.Send(new GetUserSettings.Query(user?.Username));
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            var remembered = await ReadLastKindAsync();
            if (remembered == "this-week" && _loadedLog is null && !IsLoading)
            {
                _selected = WorklogPeriod.ThisWeek(DateTime.Today);
                StateHasChanged();
            }
        }

        private void Select(WorklogPeriod period) => _selected = period;

        private async Task ShowAsync()
        {
            _search?.Cancel();
            var search = new CancellationTokenSource();
            _search = search;

            var log = new WorklogLoadLog(_selected, DateTime.Now);
            _loadingLog = log;
            await RememberKindAsync(_selected);

            // Not Progress<T>: it posts every report, even one made on the renderer's own
            // context, and the last source settles there — its report was queued behind
            // the end of the search and dropped, leaving the source spinning in the log.
            // InvokeAsync runs a report made on the context at once and queues the rest in
            // order. A report is dropped only once its search is neither running nor shown.
            var progress = new RendererProgress<WorklogCollectionProgress>(InvokeAsync, report =>
            {
                if (log != _loadingLog && log != _loadedLog)
                    return;
                log.Apply(report);
                StateHasChanged();
            });

            try
            {
                var query = log.Period.ToQuery();
                query.Progress = progress;
                var result = await Mediator.Send(query, search.Token);
                if (_search != search)
                    return;

                log.Finish(DateTime.Now);
                _items = result.WorkingDays;
                _loadedLog = log;
            }
            catch (OperationCanceledException) when (search.IsCancellationRequested)
            {
                // Cancelled by the user or by a newer search; whatever was on screen
                // before stays there.
            }
            catch (JiraAuthenticationException e)
            {
                // The period was never read: Jira refused the user halfway through. What
                // an earlier search left on screen goes with it, so the page does not
                // keep showing a day as if nothing had happened. See issue #305.
                if (_search != search)
                    return;
                _items = null;
                _loadedLog = null;
                ErrorHandler.ProcessError(e);
            }
            catch (Exception e)
            {
                if (_search != search)
                    return;
                ErrorHandler.ProcessError(e);
            }
            finally
            {
                if (_search == search)
                {
                    _search = null;
                    _loadingLog = null;
                }
                search.Dispose();
            }
        }

        private void Cancel() => _search?.Cancel();

        private async Task<string> ReadLastKindAsync()
        {
            try
            {
                return await LocalStorage.GetItemAsStringAsync(LastKindStorageKey);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Only the two offered weeks are remembered: a custom range is a one-off.</summary>
        private async Task RememberKindAsync(WorklogPeriod period)
        {
            var today = DateTime.Today;
            var kind = period == WorklogPeriod.ThisWeek(today) ? "this-week"
                : period == WorklogPeriod.LastWeek(today) ? "last-week"
                : null;
            if (kind is null)
                return;
            try
            {
                await LocalStorage.SetItemAsStringAsync(LastKindStorageKey, kind);
            }
            catch (Exception)
            {
                // Not remembered in this browser; last week stays the default.
            }
        }

        public void Dispose()
        {
            _search?.Cancel();
        }

        private sealed class RendererProgress<T> : IProgress<T>
        {
            private readonly Func<Action, Task> _invoke;
            private readonly Action<T> _report;

            public RendererProgress(Func<Action, Task> invoke, Action<T> report)
            {
                _invoke = invoke;
                _report = report;
            }

            public void Report(T value) => _ = _invoke(() => _report(value));
        }
    }
}
