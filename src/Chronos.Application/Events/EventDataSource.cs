using Microsoft.Extensions.Logging;
using Chronos.Application.Tracing;
using Chronos.Domain.Models.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Events
{
    /// <summary>
    /// Collects the events of every enabled provider. Providers are prepared one by one
    /// (settings, database) and only then fetched concurrently, so the scoped DbContext
    /// is never used from two tasks at once — the constraint the hand-written
    /// orchestration in GetWorklogCollection relied on. See issues #258 and #299.
    /// </summary>
    public class EventDataSource : IEventDataSource
    {
        private readonly IEnumerable<IEventProvider> _providers;
        private readonly IPerformanceStatsCollector _stats;
        private readonly ILogger<EventDataSource> _logger;

        public EventDataSource(
            IEnumerable<IEventProvider> providers,
            IPerformanceStatsCollector stats,
            ILogger<EventDataSource> logger)
        {
            _providers = providers;
            _stats = stats;
            _logger = logger;
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(
            EventQuery query,
            CancellationToken cancellationToken = default)
        {
            var prepared = new List<IEventProvider>();
            foreach (var provider in _providers)
            {
                if (await PrepareAsync(provider, query, cancellationToken))
                {
                    prepared.Add(provider);
                }
            }

            var results = await Task.WhenAll(
                prepared.Select(provider => FetchAsync(provider, cancellationToken)));

            return results.SelectMany(events => events).ToList();
        }

        private async Task<bool> PrepareAsync(
            IEventProvider provider,
            EventQuery query,
            CancellationToken cancellationToken)
        {
            try
            {
                return await provider.PrepareAsync(query, cancellationToken);
            }
            catch (AuthenticationException)
            {
                // A refused user, not a source to skip — see FetchAsync. Issue #305.
                throw;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception,
                    "The {Source} event provider could not be prepared; its events are skipped.",
                    provider.Source);
                return false;
            }
        }

        /// <summary>
        /// A failing source is skipped rather than failing the whole collection — the
        /// behaviour the calendar always had, now applied to every provider. The one
        /// failure that is not skipped is Jira refusing the user's credentials, which
        /// empties every Jira source at once (issue #305). Per-provider timings keep the
        /// source breakdown that separate MediatR requests used to give. See issue #258.
        /// </summary>
        private async Task<IEnumerable<IEvent>> FetchAsync(
            IEventProvider provider,
            CancellationToken cancellationToken)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            var startAllocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
            try
            {
                return await provider.GetEventsAsync(cancellationToken);
            }
            catch (AuthenticationException)
            {
                // Not a failing source but a refused user: Jira answers every provider
                // the same 401, and a day assembled from what is left is wrong without
                // saying so. It leaves the collection and reaches the caller. See
                // issue #305.
                throw;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception,
                    "The {Source} event provider failed; its events are skipped.",
                    provider.Source);
                return Enumerable.Empty<IEvent>();
            }
            finally
            {
                _stats.Record(
                    provider.GetType().Name,
                    Stopwatch.GetElapsedTime(startTimestamp),
                    Math.Max(0, GC.GetTotalAllocatedBytes(precise: false) - startAllocatedBytes));
            }
        }
    }
}
