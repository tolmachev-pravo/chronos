using System;
using System.Collections.Generic;

namespace Pet.Jira.Application.Tracing
{
    /// <summary>
    /// Collects aggregated timing statistics (count / sum / min / max / average) per category.
    /// Registered as a singleton so the dev debug panel can read process-wide measures.
    /// </summary>
    public interface IPerformanceStatsCollector
    {
        void Record(string category, TimeSpan elapsed);

        IReadOnlyCollection<Measure> Measures { get; }

        void Reset();
    }
}
