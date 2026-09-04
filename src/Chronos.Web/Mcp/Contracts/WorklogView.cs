using System;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// Time recorded in Jira — a fact, unlike an event. Flat on purpose: the day model it
    /// comes from carries UI state and refers back to its day, neither of which belongs in a
    /// tool answer. Durations are minutes, so that nothing has to be parsed out of a
    /// formatted string.
    /// </summary>
    /// <param name="EventId">
    /// The event this worklog appears to have been logged for, or null when it matches none —
    /// time logged by hand for something Chronos never saw. The tie is Chronos matching an
    /// issue key and an interval, not something Jira records, so treat it as a reading of the
    /// day rather than a fact about it.
    /// </param>
    public record WorklogView(
        string IssueKey,
        string Summary,
        string Link,
        DateTime StartedAt,
        int Minutes,
        string Comment,
        string EventId);
}
