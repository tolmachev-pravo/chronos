using Chronos.Domain.Models.Events;
using System;

namespace Chronos.Application.Events
{
    /// <summary>
    /// What happened to one event source during a fetch. A period takes long to read
    /// because nothing is stored on our side; reporting each source as it settles lets
    /// the page say what it is waiting for, and afterwards what each source gave — a
    /// skipped source is otherwise indistinguishable from one that had nothing.
    /// </summary>
    public record EventSourceProgress(EventSource Source, EventSourceState State)
    {
        /// <summary>How many events the source gave; set once it completed.</summary>
        public int? Count { get; init; }

        /// <summary>How long the source took; set once it settled.</summary>
        public TimeSpan? Elapsed { get; init; }

        /// <summary>Why the source was skipped; set when it failed.</summary>
        public string Error { get; init; }
    }

    public enum EventSourceState
    {
        /// <summary>The source is enabled and is being queried.</summary>
        Started,

        /// <summary>The source answered.</summary>
        Completed,

        /// <summary>The source failed and its events are skipped.</summary>
        Failed
    }
}
