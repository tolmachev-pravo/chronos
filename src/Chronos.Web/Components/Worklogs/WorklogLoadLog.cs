using Chronos.Application.Events;
using Chronos.Application.Worklogs.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Web.Components.Worklogs
{
    /// <summary>
    /// What one search read: each source in the order it started, with what it gave and
    /// how long it took. While the search runs it is the progress; afterwards it is the
    /// log — the only place a skipped source shows up as skipped rather than as empty.
    /// </summary>
    public sealed class WorklogLoadLog
    {
        private readonly List<WorklogCollectionProgress> _entries = new();

        public WorklogLoadLog(WorklogPeriod period, DateTime startedAt)
        {
            Period = period;
            StartedAt = startedAt;
        }

        public WorklogPeriod Period { get; }

        public DateTime StartedAt { get; }

        /// <summary>Wall-clock time of the whole search; set once it finished.</summary>
        public TimeSpan? Duration { get; private set; }

        public IReadOnlyList<WorklogCollectionProgress> Entries => _entries;

        public int Count => _entries.Sum(entry => entry.Count ?? 0);

        public IEnumerable<WorklogCollectionProgress> Failed =>
            _entries.Where(entry => entry.State == EventSourceState.Failed);

        public void Apply(WorklogCollectionProgress report)
        {
            var index = _entries.FindIndex(entry => entry.Source == report.Source);
            if (index < 0)
                _entries.Add(report);
            else
                _entries[index] = report;
        }

        public void Finish(DateTime finishedAt) => Duration = finishedAt - StartedAt;

        public static string Title(WorklogCollectionSource source) => source switch
        {
            WorklogCollectionSource.Worklogs => "Списанное время",
            WorklogCollectionSource.Assignee => "Исполнитель",
            WorklogCollectionSource.Tester => "Тестировщик",
            WorklogCollectionSource.Comment => "Комментарии",
            WorklogCollectionSource.Calendar => "Календарь",
            _ => source.ToString()
        };

        public static string Seconds(TimeSpan value) =>
            value.TotalSeconds < 10 ? $"{value.TotalSeconds:0.0} с" : $"{value.TotalSeconds:0} с";
    }
}
