using Chronos.Application.Common.Extensions;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Chronos.Application.Worklogs.Dto
{
    public class WorkingDayWorklog
    {
        public DateTime RawStartDate { get; set; }
        public DateTime RawCompleteDate { get; set; }
        public TimeSpan RawTimeSpent => RawCompleteDate - RawStartDate;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime CompleteDate { get; set; }

        public TimeSpan TimeSpent => CompleteDate - StartDate;

        public TimeSpan RemainingTimeSpent { get; set; }

        /// <summary>
        /// Issue
        /// </summary>
        [Required]
        public IIssue Issue { get; set; }

        [Required]
        public WorklogType Type { get; set; }

        [Required]
        public WorklogSource Source { get; set; }

        public WorkingDay WorkingDay { get; set; }

        public IWorklog Worklog { get; set; }

        public IList<WorkingDayWorklog> Children { get; set; }
        public WorkingDayWorklog Parent { get; set; }

        public TimeSpan ChildrenTimeSpent => Children.TimeSpent();
        public bool IsEmpty => RemainingTimeSpent == TimeSpan.Zero;

        public string Comment { get; set; }

        public WorkingDayWorklog()
        {
            Children = new List<WorkingDayWorklog>();
        }

        public WorkingDayWorklog(
            DateTime startDate,
            DateTime completeDate,
            IIssue issue,
            WorklogType type,
            WorklogSource source) : this()
        {
            RawStartDate = startDate;
            RawCompleteDate = completeDate;
            StartDate = startDate;
            CompleteDate = completeDate;
            Issue = issue;
            Type = type;
            Source = source;
            Comment = DefaultComment();
            UpdateRemainingTimeSpent(TimeSpent);
        }

        /// <summary>
        /// Update remaining time spent
        /// </summary>
        /// <param name="timeSpan"></param>
        public void UpdateRemainingTimeSpent(TimeSpan timeSpan)
        {
            if (timeSpan > TimeSpan.Zero
                && timeSpan < TimeSpan.FromMinutes(1))
            {
                timeSpan = TimeSpan.FromMinutes(1);
            }

            RemainingTimeSpent = timeSpan.Round();
        }

        public static WorkingDayWorklog CreateActual(
            IWorklog worklog)
        {
            var result = new WorkingDayWorklog
            {
                RawStartDate = worklog.StartDate,
                RawCompleteDate = worklog.CompleteDate,
                StartDate = worklog.StartDate,
                CompleteDate = worklog.CompleteDate,
                Issue = worklog.Issue,
                Type = WorklogType.Actual,
                Source = worklog.Source
            };

            result.UpdateRemainingTimeSpent(worklog.TimeSpent);

            return result;
        }

        public static WorkingDayWorklog CreateEstimated(
            IWorklog worklog,
            DateTime day,
            TimeSpan dailyWorkingStartTime,
            TimeSpan dailyWorkingEndTime)
        {
            var startOfWorkingDay = day.Add(dailyWorkingStartTime);
            var endOfWorkingDay = day.Add(dailyWorkingEndTime);

            DateTime startDate = AdaptWorkingTime(worklog.StartDate, startOfWorkingDay, endOfWorkingDay);
            DateTime completeDate = AdaptWorkingTime(worklog.CompleteDate, startOfWorkingDay, endOfWorkingDay);

            var result = new WorkingDayWorklog
            {
                RawStartDate = worklog.StartDate,
                RawCompleteDate = worklog.CompleteDate,
                StartDate = startDate,
                CompleteDate = completeDate,
                Issue = worklog.Issue,
                Type = WorklogType.Estimated,
                Source = worklog.Source
            };

            result.UpdateRemainingTimeSpent(result.TimeSpent);

            if (result.Source == WorklogSource.Calendar && result.Issue?.Summary != null)
                result.Comment = result.Issue.Summary;

            return result;
        }

        public static WorkingDayWorklog CreateActualByEstimated(
            WorkingDayWorklog source)
        {
            var timeSpent = source.RemainingTimeSpent;
            var completeDate = source.RawCompleteDate != source.RawCompleteDate.EndOfDay()
                ? source.RawCompleteDate
                : source.CompleteDate;
            var startDate = completeDate.Add(-timeSpent);
            return new WorkingDayWorklog(
                startDate: startDate,
                completeDate: completeDate,
                issue: source.Issue,
                type: WorklogType.Actual,
                source: source.Source);
        }

        private static DateTime AdaptWorkingTime(
            DateTime value,
            DateTime startOfWorkingDay,
            DateTime endOfWorkingDay)
        {
            if (value > endOfWorkingDay)
            {
                return endOfWorkingDay;
            }
            else if (value < startOfWorkingDay)
            {
                return startOfWorkingDay;
            }
            else
            {
                return value;
            }
        }

        public string DefaultComment()
        {
            return Source switch
            {
                WorklogSource.Assignee => $"Working on task {Issue?.Key}",
                WorklogSource.Comment => $"Task discussion {Issue?.Key}",
                WorklogSource.Calendar => $"Discussion {Issue?.Key}",
				WorklogSource.Tester => $"Testing task {Issue?.Key}",
				_ => "Default worklog",
            };
        }
    }
}
