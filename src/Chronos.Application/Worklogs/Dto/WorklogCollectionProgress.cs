using Chronos.Application.Events;
using Chronos.Domain.Models.Events;
using System;

namespace Chronos.Application.Worklogs.Dto
{
    /// <summary>
    /// Everything a worklog collection is read from: the logged time itself and every
    /// event source. The page lists these while a period loads.
    /// </summary>
    public enum WorklogCollectionSource
    {
        Worklogs,
        Assignee,
        Tester,
        Comment,
        Calendar
    }

    /// <summary>
    /// One source of a worklog collection starting or settling.
    /// </summary>
    public record WorklogCollectionProgress(WorklogCollectionSource Source, EventSourceState State)
    {
        /// <summary>How many records the source gave; set once it completed.</summary>
        public int? Count { get; init; }

        /// <summary>How long the source took; set once it settled.</summary>
        public TimeSpan? Elapsed { get; init; }

        /// <summary>Why the source was skipped or failed.</summary>
        public string Error { get; init; }

        public static WorklogCollectionProgress From(EventSourceProgress progress) =>
            new(ToCollectionSource(progress.Source), progress.State)
            {
                Count = progress.Count,
                Elapsed = progress.Elapsed,
                Error = progress.Error
            };

        private static WorklogCollectionSource ToCollectionSource(EventSource source) => source switch
        {
            EventSource.Assignee => WorklogCollectionSource.Assignee,
            EventSource.Tester => WorklogCollectionSource.Tester,
            EventSource.Comment => WorklogCollectionSource.Comment,
            EventSource.Calendar => WorklogCollectionSource.Calendar,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
