using MediatR;
using Chronos.Application.Authentication;
using Chronos.Application.Common.Extensions;
using Chronos.Application.Extensions.Jira.Dto;
using Chronos.Application.Extensions.Jira.Queries;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Extensions.YandexCalendar.Queries;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Users;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Worklogs.Queries
{
    public class GetWorklogCollection
    {
        public class Query : IRequest<Model>
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public TimeSpan DailyWorkingStartTime { get; set; }
            public TimeSpan DailyWorkingEndTime { get; set; }
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

            private static readonly Task<IEnumerable<IWorklog>> NoWorklogs =
                Task.FromResult(Enumerable.Empty<IWorklog>());

            private async Task<IEnumerable<WorkingDay>> CalculateWorklogCollection(Query query,
                CancellationToken cancellationToken)
            {
                var user = await _identityService.GetCurrentUserAsync();

                // Which kinds of Jira events the user wants (issue #242). A disabled
                // extension loads none of them; a disabled kind is not requested at all,
                // so Jira is not queried for it.
                var extension = await _mediator.Send(
                    new GetJiraExtension.Query(user?.Username), cancellationToken);
                var jiraSettings = extension.IsEnabled
                    ? extension.Settings
                    : JiraExtensionSettingsDto.Disabled;

                // Kick off the independent data fetches concurrently to cut wall-clock.
                // Safe against the scoped, non-thread-safe ApplicationDbContext: only the
                // calendar path touches it, and it runs as a single task, so there is no
                // concurrent DbContext access; the Jira queries use transient services with
                // no shared state. Trade-off: PerformanceBehavior's per-request allocation
                // stats are not representative while these run concurrently. See issue #258.
                var assigneeTask = jiraSettings.AssigneeEventsEnabled
                    ? _mediator.Send(
                        new GetAssigneeJiraEvents.Query()
                        {
                            StartDate = query.StartDate,
                            EndDate = query.EndDate
                        }, cancellationToken)
                    : NoWorklogs;

                var testerTask = jiraSettings.TesterEventsEnabled
                    ? _mediator.Send(
                        new GetTesterJiraEvents.Query()
                        {
                            StartDate = query.StartDate,
                            EndDate = query.EndDate
                        }, cancellationToken)
                    : NoWorklogs;

                var commentTask = jiraSettings.CommentEventsEnabled
                    ? _mediator.Send(
                        new GetCommentJiraEvents.Query()
                        {
                            StartDate = query.StartDate,
                            EndDate = query.EndDate,
                            CommentWorklogTime = jiraSettings.CommentWorklogTime
                        }, cancellationToken)
                    : NoWorklogs;

                var issueWorklogsTask = _mediator.Send(
                    new GetIssueWorklogs.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate
                    }, cancellationToken);

                var calendarTask = GetCalendarWorklogsAsync(query, user, cancellationToken);

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
                GetCalendarWorklogsAsync(Query query, User user, CancellationToken cancellationToken)
            {
                var calendarWorklogs = new List<IWorklog>();
                var blockedEventsByDay = new Dictionary<DateTime, List<BlockedCalendarEvent>>();

                try
                {
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
