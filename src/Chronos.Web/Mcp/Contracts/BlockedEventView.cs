using System;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// Time the user spent on something with no Jira key of its own — a meeting whose title
    /// names no issue, most of all. It cannot be logged as it is, but it did take the hours
    /// out of the day, which is why the day offers less than its full length.
    ///
    /// The client is shown these on purpose: what they need is a person to say which issue
    /// the time belongs to, and asking is exactly what a client can do.
    /// </summary>
    public record BlockedEventView(
        string Summary,
        DateTime StartedAt,
        int Minutes,
        string Source);
}
