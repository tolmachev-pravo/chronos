using System;
using System.Collections.Generic;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// A day told as the two things it is made of: what the user was seen doing
    /// (<see cref="Events"/>) and what is recorded in Jira (<see cref="Worklogs"/>). An event
    /// with time left to suggest is work not logged yet; a worklog pointing at no event is
    /// time logged for something Chronos never saw.
    ///
    /// The totals are the day's own arithmetic, kept here so that nobody has to add minutes
    /// up to learn whether the day is closed.
    /// </summary>
    /// <param name="BlockedMinutes">
    /// Time taken by events with no issue and no worklog. This is the answer to why a day of
    /// eight hours offers less than eight: those minutes are spent, but nothing can be logged
    /// against them until somebody names an issue.
    /// </param>
    public record WorkingDayView(
        DateTime Date,
        bool IsWeekend,
        int PlannedMinutes,
        int LoggedMinutes,
        int SuggestedMinutes,
        int BlockedMinutes,
        int ProgressPercent,
        IReadOnlyList<EventView> Events,
        IReadOnlyList<WorklogView> Worklogs);
}
