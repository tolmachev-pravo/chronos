using Chronos.Domain.Models.Issues;
using System;

namespace Chronos.Domain.Models.Events
{
    public class UserEvent : IEvent
    {
        public DateTime StartDate { get; init; }
        public DateTime CompleteDate { get; init; }
        public IIssue Issue { get; init; }
        public string Author { get; init; }
        public EventSource Source { get; init; }
        public string Summary { get; init; }

        public TimeSpan Duration => CompleteDate - StartDate;

        /// <summary>
        /// True when the event overlaps the period. Carried over from
        /// RawIssueWorklog.IsBetween — the Jira providers filter on it.
        /// </summary>
        public bool IsBetween(DateTime from, DateTime to) =>
            StartDate < to && CompleteDate > from;
    }
}
