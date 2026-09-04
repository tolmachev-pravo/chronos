using System;
using System.Collections.Generic;

namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// A day of the period: what is already logged in Jira, what Chronos suggests logging
    /// on top of it, and how much of the day the two of them cover.
    /// </summary>
    public record WorkingDayView(
        DateTime Date,
        bool IsWeekend,
        int PlannedMinutes,
        int LoggedMinutes,
        int SuggestedMinutes,
        int BlockedMinutes,
        int ProgressPercent,
        IReadOnlyList<WorklogView> Logged,
        IReadOnlyList<WorklogView> Suggested);
}
