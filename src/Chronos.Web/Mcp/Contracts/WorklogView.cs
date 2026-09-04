using System;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// One row of a day as a client sees it. Flat on purpose: the day model it comes from
    /// carries UI state and refers back to its day, neither of which survives — or belongs
    /// in — a tool answer.
    ///
    /// Durations are minutes, so that nothing has to be parsed out of a formatted string.
    /// </summary>
    public record WorklogView(
        string IssueKey,
        string Summary,
        string Link,
        DateTime StartedAt,
        int Minutes,
        string Comment,
        string Source);
}
