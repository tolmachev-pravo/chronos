using Chronos.Domain.Models.Issues;
using System;

namespace Chronos.Domain.Models.Events
{
    /// <summary>
    /// A trace of the user's activity from which an estimated worklog is derived.
    /// Unlike <see cref="Worklogs.IWorklog"/> it carries no logged time — only the
    /// interval it occupied — and its issue is optional: a meeting without a Jira key
    /// is still an event. See issue #299.
    /// </summary>
    public interface IEvent
    {
        DateTime StartDate { get; }
        DateTime CompleteDate { get; }
        TimeSpan Duration { get; }
        IIssue Issue { get; }
        string Author { get; }
        EventSource Source { get; }

        /// <summary>
        /// Title of the event — the calendar summary. Null for Jira events, whose
        /// description comes from the issue.
        /// </summary>
        string Summary { get; }

        /// <summary>
        /// What the source knows about this event beyond the interval above. Null is
        /// normal: a source with nothing to add leaves it unset. See issue #156.
        /// </summary>
        IEventDetails Details { get; }
    }
}
