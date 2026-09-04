using System;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// A trace of what the user did: time in an issue, an issue they tested, a comment they
    /// left, a meeting from their calendar. An event is not logged time — it is the reason
    /// Chronos believes time was spent, and <see cref="SuggestedMinutes"/> is how much of the
    /// day it proposes to log for it.
    ///
    /// The issue is optional. A meeting whose title names none cannot be logged as it is:
    /// somebody has to say which issue the hour belongs to.
    /// </summary>
    /// <param name="Id">
    /// Names the event inside this answer only, so a worklog can point at it. It is not an
    /// identifier Chronos or Jira keeps — the next answer numbers its events afresh.
    /// </param>
    /// <param name="Minutes">How long the event ran, within the day's own bounds.</param>
    /// <param name="SuggestedMinutes">
    /// What Chronos proposes to log for it now. Zero when the time is already covered by a
    /// worklog — that worklog points back at this event.
    /// </param>
    public record EventView(
        string Id,
        string Source,
        string IssueKey,
        string Summary,
        DateTime StartedAt,
        int Minutes,
        int SuggestedMinutes);
}
