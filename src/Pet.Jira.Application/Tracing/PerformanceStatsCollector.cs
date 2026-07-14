using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Pet.Jira.Application.Tracing
{
    public class PerformanceStatsCollector : IPerformanceStatsCollector
    {
        private readonly ConcurrentDictionary<string, Measure> _measures =
            new ConcurrentDictionary<string, Measure>(StringComparer.InvariantCultureIgnoreCase);

        public void Record(string category, TimeSpan elapsed, long allocatedBytes)
        {
            _measures.GetOrAdd(category, c => new Measure(c)).Update(elapsed, allocatedBytes);
        }

        public IReadOnlyCollection<Measure> Measures => _measures.Values.ToArray();

        public void Reset() => _measures.Clear();
    }
}
