using Chronos.Application.Worklogs.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Application.Common.Extensions
{
    public static class WorkingDayWorklogExtensions
    {
        public static TimeSpan TimeSpent(this IEnumerable<WorkingDayWorklog> worklogs)
        {
            if (worklogs.IsEmpty())
            {
                return TimeSpan.Zero;
            }

            return worklogs
                .Select(worklog => worklog.TimeSpent)
                .Sum();
        }

        public static TimeSpan RemainingTimeSpent(this IEnumerable<WorkingDayWorklog> worklogs)
        {
            if (worklogs.IsEmpty())
            {
                return TimeSpan.Zero;
            }

            return worklogs
                .Select(worklog => worklog.RemainingTimeSpent)
                .Sum();
        }
    }
}
