using MediatR;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Common.Extensions;
using Pet.Jira.Application.Extensions.YandexCalendar.Dto;
using Pet.Jira.Application.Extensions.YandexCalendar.Queries;
using Pet.Jira.Application.Worklogs.Dto;
using Pet.Jira.Domain.Models.Issues;
using Pet.Jira.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Worklogs.Queries
{
    public class GetWorklogCollection
    {
        public class Query : IRequest<Model>
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public TimeSpan DailyWorkingStartTime { get; set; }
            public TimeSpan DailyWorkingEndTime { get; set; }
            public string IssueStatusId { get; set; }
            public TimeSpan CommentWorklogTime { get; set; }
            public TimeSpan LunchTime { get; set; }
        }

        public class Model
        {
            public IEnumerable<WorkingDay> WorkingDays { get; set; }
        }

        public class QueryHandler : IRequestHandler<Query, Model>
        {
            private readonly IMediator _mediator;
            private readonly IIdentityService _identityService;

            public QueryHandler(
                IMediator mediator,
                IIdentityService identityService)
            {
                _mediator = mediator;
                _identityService = identityService;
            }

            public async Task<Model> Handle(
                Query query,
                CancellationToken cancellationToken = default)
            {
                var worklogCollection = await CalculateWorklogCollection(query, cancellationToken);
                return new Model { WorkingDays = worklogCollection };
            }

            private async Task<IEnumerable<WorkingDay>> CalculateWorklogCollection(Query query,
                CancellationToken cancellationToken)
            {
                // Kick off the independent data fetches concurrently to cut wall-clock.
                // Safe against the scoped, non-thread-safe ApplicationDbContext: only the
                // calendar path touches it, and it runs as a single task, so there is no
                // concurrent DbContext access; the Jira queries use transient services with
                // no shared state. Trade-off: PerformanceBehavior's per-request allocation
                // stats are not representative while these run concurrently. See issue #258.
                var assigneeTask = _mediator.Send(
                    new GetAssigneeJiraEvents.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate
                    }, cancellationToken);

                var testerTask = _mediator.Send(
                    new GetTesterJiraEvents.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate
                    }, cancellationToken);

                var commentTask = _mediator.Send(
                    new GetCommentJiraEvents.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate,
                        CommentWorklogTime = query.CommentWorklogTime
                    }, cancellationToken);

                var issueWorklogsTask = _mediator.Send(
                    new GetIssueWorklogs.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate
                    }, cancellationToken);

                var calendarTask = GetCalendarWorklogsAsync(query, cancellationToken);

                await Task.WhenAll(
                    assigneeTask, testerTask, commentTask, issueWorklogsTask, calendarTask);

                var rawIssueWorklogs = (await assigneeTask)
                    .Concat(await testerTask)
                    .Concat(await commentTask);

                var issueWorklogs = await issueWorklogsTask;

                var (calendarWorklogs, blockedEventsByDay) = await calendarTask;

                var allRawWorklogs = rawIssueWorklogs.Concat(calendarWorklogs);

                var days = CalculateDays(issueWorklogs, allRawWorklogs, query).ToList();
                foreach (var day in days)
                {
                    day.BlockedCalendarEvents = blockedEventsByDay.GetValueOrDefault(day.Date) ?? new List<BlockedCalendarEvent>();
                    day.Refresh();
                }

                return days;
            }

            /// <summary>
            /// Fetches calendar events for the query range and splits them into estimated
            /// worklogs (events with a Jira key) and per-day blocked time (events without).
            /// Calendar failures degrade silently — the collection is returned without calendar.
            /// </summary>
            private async Task<(List<IWorklog> CalendarWorklogs, Dictionary<DateTime, List<BlockedCalendarEvent>> BlockedEventsByDay)>
                GetCalendarWorklogsAsync(Query query, CancellationToken cancellationToken)
            {
                var calendarWorklogs = new List<IWorklog>();
                var blockedEventsByDay = new Dictionary<DateTime, List<BlockedCalendarEvent>>();

                try
                {
                    var user = await _identityService.GetCurrentUserAsync();
                    for (var date = query.StartDate.Date; date <= query.EndDate.Date; date = date.AddDays(1))
                    {
                        var events = await _mediator.Send(
                            new GetYandexCalendarEvents.Query(user.Username, DateOnly.FromDateTime(date)),
                            cancellationToken);

                        foreach (var calendarEvent in events)
                        {
                            if (!string.IsNullOrEmpty(calendarEvent.JiraIssueKeyHint))
                            {
                                calendarWorklogs.Add(new RawIssueWorklog
                                {
                                    StartDate = calendarEvent.Start,
                                    CompleteDate = calendarEvent.End,
                                    Issue = new Issue
                                    {
                                        Key = calendarEvent.JiraIssueKeyHint,
                                        Identifier = calendarEvent.JiraIssueKeyHint,
                                        Summary = calendarEvent.Summary
                                    },
                                    Author = user.Username,
                                    Source = WorklogSource.Calendar
                                });
                            }
                            else
                            {
                                var dayKey = calendarEvent.Start.Date;
                                if (!blockedEventsByDay.TryGetValue(dayKey, out var dayEvents))
                                {
                                    dayEvents = new List<BlockedCalendarEvent>();
                                    blockedEventsByDay[dayKey] = dayEvents;
                                }
                                dayEvents.Add(new BlockedCalendarEvent(
                                    calendarEvent.Start, calendarEvent.End, calendarEvent.Summary));
                            }
                        }
                    }
                }
                catch
                {
                    // Calendar unavailable — return worklogs without calendar.
                }

                return (calendarWorklogs, blockedEventsByDay);
            }

            private static IEnumerable<WorkingDay> CalculateDays(
                IEnumerable<IWorklog> issueWorklogs,
                IEnumerable<IWorklog> rawIssueWorklogs,
                Query query)
            {
                var day = query.EndDate.Date;
                var splitedRawIssueWorklogs = rawIssueWorklogs.SplitByDays(
                    firstDate: query.StartDate,
                    lastDate: query.EndDate);

                while (day >= query.StartDate.Date)
                {
                    var dailyActualWorklogs = issueWorklogs
                        .Where(worklog => worklog.StartDate.Date == day)
                        .Select(worklog => WorkingDayWorklog.CreateActual(worklog));

                    var dailyEstimatedWorklogs = splitedRawIssueWorklogs
                        .Where(worklog => worklog.StartDate.Date == day)
                        .Select(worklog =>
                            WorkingDayWorklog.CreateEstimated(
                                worklog: worklog,
                                day: day,
                                dailyWorkingStartTime: query.DailyWorkingStartTime,
                                dailyWorkingEndTime: query.DailyWorkingEndTime));

                    var dailyWorklogs = dailyActualWorklogs.Union(dailyEstimatedWorklogs)
                            .OrderBy(record => record.StartDate)
                            .ThenBy(record => record.CompleteDate)
                            .ToList();

                    yield return new WorkingDay(
                        date: day,
                        settings: new WorkingDaySettings(
                            workingStartTime: query.DailyWorkingStartTime,
                            workingEndTime: query.DailyWorkingEndTime,
                            lunchTime: query.LunchTime),
                        worklogs: dailyWorklogs);

                    day = day.AddDays(-1);
                }
            }
        }
    }
}
