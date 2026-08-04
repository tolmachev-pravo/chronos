using System;

namespace Chronos.Application.Worklogs.Dto
{
    /// <summary>
    /// A calendar event without a Jira key. It blocks day time until logged; in the day
    /// view it can be logged by picking an Issue, after which it no longer blocks.
    /// </summary>
    public record BlockedCalendarEvent(DateTime Start, DateTime End, string Title)
    {
        public TimeSpan Duration => End - Start;
    }
}
