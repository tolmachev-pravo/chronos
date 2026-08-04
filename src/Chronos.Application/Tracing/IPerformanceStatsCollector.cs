using System;
using System.Collections.Generic;

namespace Chronos.Application.Tracing
{
    /// <summary>
    /// Collects aggregated timing and allocation statistics (count / sum / min / max /
    /// average) per category. Registered as a singleton so the dev debug panel can read
    /// process-wide measures.
    /// </summary>
    public interface IPerformanceStatsCollector
    {
        void Record(string category, TimeSpan elapsed, long allocatedBytes);

        IReadOnlyCollection<Measure> Measures { get; }

        void Reset();
    }
}
