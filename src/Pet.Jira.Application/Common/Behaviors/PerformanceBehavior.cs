using MediatR;
using Microsoft.Extensions.Logging;
using Pet.Jira.Application.Tracing;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Common.Behaviors
{
    /// <summary>
    /// Measures how long each MediatR request takes. Records aggregated stats for the dev
    /// debug panel and logs a warning when a request runs longer than the slow threshold.
    /// </summary>
    public class PerformanceBehavior<TRequest, TResponse> :
        IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const int SlowRequestThresholdMs = 500;

        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly IPerformanceStatsCollector _stats;

        public PerformanceBehavior(
            ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
            IPerformanceStatsCollector stats)
        {
            _logger = logger;
            _stats = stats;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            // Process-wide allocation counter: captures allocations on parallel worker
            // threads too (e.g. Parallel.ForEachAsync over issues), unlike the per-thread
            // counter. It measures allocation throughput, not retained memory, and is only
            // representative when requests do not run concurrently.
            var startAllocatedBytes = GC.GetTotalAllocatedBytes(precise: false);

            var response = await next();

            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var allocatedBytes = Math.Max(0,
                GC.GetTotalAllocatedBytes(precise: false) - startAllocatedBytes);

            // Queries/commands are nested types (e.g. GetWorklogCollection.Query), so the
            // outer type gives a meaningful, collision-free category name.
            var requestName = typeof(TRequest).DeclaringType?.Name ?? typeof(TRequest).Name;

            _stats.Record(requestName, elapsed, allocatedBytes);

            if (elapsed.TotalMilliseconds > SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Long running request {RequestName} took {ElapsedMilliseconds} ms",
                    requestName,
                    elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "Request {RequestName} handled in {ElapsedMilliseconds} ms",
                    requestName,
                    elapsed.TotalMilliseconds);
            }

            return response;
        }
    }
}
