using Chronos.Application.Common.Extensions;
using Chronos.Application.Worklogs.Dto;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Application.Worklogs
{
    public static class WorklogMatching
    {
        /// <summary>
        /// Match worklogs by issue key and date range.
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="children"></param>
        public static void Match(
            IEnumerable<WorkingDayWorklog> parents,
            IEnumerable<WorkingDayWorklog> children)
        {
            if (parents.IsEmpty()
                || children.IsEmpty())
            {
                return;
            }

            foreach (var child in children)
            {
                if (child.Issue == null) continue;

                // Find all suggested parents by issue key.
                var issueParents = parents
                    .Where(worklog => worklog.Issue?.Key == child.Issue.Key)
                    .ToList();

                if (issueParents.IsEmpty()
                    || TrySetParent(child, issueParents))
                {
                    continue;
                }

                // Find all suggested parents by "start date" and "complete date".
                // Start date and complete date of child should be between start date and complete date of parent.
                var suggestedParents = issueParents
                    .Where(worklog => worklog.RawStartDate <= child.RawStartDate
                        && worklog.RawCompleteDate >= child.RawCompleteDate)
                    .ToList();

                if (TrySetParent(child, suggestedParents))
                {
                    continue;
                }

                // Find all suggested parents by "complete date".
                // Complete date of child should be between start date and complete date of parent.
                suggestedParents = issueParents
                    .Where(worklog => worklog.RawStartDate <= child.RawCompleteDate
                        && worklog.RawCompleteDate >= child.RawCompleteDate)
                    .ToList();

                if (TrySetParent(child, suggestedParents))
                {
                    continue;
                }

                // Find all suggested parents by "start date".
                // Start date of child should be between start date and complete date of parent.
                suggestedParents = issueParents
                    .Where(worklog => worklog.RawStartDate <= child.RawStartDate
                        && worklog.RawCompleteDate >= child.RawStartDate)
                    .ToList();

                if (TrySetParent(child, suggestedParents))
                {
                    continue;
                }

                // Find all suggested parents by date range nesting.
                // The case when child date range include parent date range. 
                suggestedParents = issueParents
                    .Where(worklog => worklog.RawStartDate >= child.RawStartDate
                        && worklog.RawCompleteDate <= child.RawCompleteDate)
                    .ToList();

                if (TrySetParent(child, suggestedParents))
                {
                    continue;
                }

                child.Parent = issueParents.First();
            }

            foreach (var parent in parents)
            {
                parent.Children = children
                    .Where(worklog => worklog.Parent == parent)
                    .ToList();
            }
        }

        /// <summary>
        /// Try to set parent for child.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="suggestedParents"></param>
        /// <returns></returns>
        private static bool TrySetParent(
            WorkingDayWorklog child,
            List<WorkingDayWorklog> suggestedParents)
        {
            if (suggestedParents.Count == 1)
            {
                child.Parent = suggestedParents.First();
                return true;
            }

            return false;
        }
    }
}
