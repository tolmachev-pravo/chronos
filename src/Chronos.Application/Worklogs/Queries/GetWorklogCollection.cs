using MediatR;
using Chronos.Application.Authentication;
using Chronos.Application.Common.Extensions;
using Chronos.Application.Events.Queries;
using Chronos.Application.Users.Dto;
using Chronos.Application.Users.Queries;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Events;
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
        /// <summary>
        /// Only the period is asked for: the working day comes from the user's own settings
        /// (issue #241), so every search uses the same frame.
        /// </summary>
        public class Query : IRequest<Model>
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
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

            /// <summary>
            /// A day is built from two independent streams: worklogs — the time actually
            /// logged — and events, the traces of activity an estimate is derived from.
            /// Which event sources exist and whether they are enabled is the concern of
            /// IEventDataSource, not of this handler. See issue #299.
            /// </summary>
            private async Task<IEnumerable<WorkingDay>> CalculateWorklogCollection(Query query,
                CancellationToken cancellationToken)
            {
                var user = await _identityService.GetCurrentUserAsync();

                // The working day the user set in their profile (issue #241): it frames the
                // estimated worklogs and gives every day its planned working time.
                var userSettings = await _mediator.Send(
                    new GetUserSettings.Query(user?.Username), cancellationToken);

                // Events and worklogs are fetched concurrently to cut wall-clock. Trade-off:
                // PerformanceBehavior's per-request allocation stats are not representative
                // while these run concurrently; the event orchestrator records its own
                // per-provider measures. See issue #258.
                var eventsTask = _mediator.Send(
                    new GetUserEvents.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate
                    }, cancellationToken);

                var issueWorklogsTask = _mediator.Send(
                    new GetIssueWorklogs.Query()
                    {
                        StartDate = query.StartDate,
                        EndDate = query.EndDate
                    }, cancellationToken);

                await Task.WhenAll(eventsTask, issueWorklogsTask);

                var events = (await eventsTask).ToList();
                var issueWorklogs = await issueWorklogsTask;

                // An event without an issue is time the user spent but cannot log against a
                // task: it blocks the day instead of becoming an estimated worklog.
                var keyedEvents = events.Where(userEvent => userEvent.Issue is not null);
                var blockedEventsByDay = events
                    .Where(userEvent => userEvent.Issue is null)
                    .GroupBy(userEvent => userEvent.StartDate.Date)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlyList<IEvent>)group.ToList());

                var days = CalculateDays(issueWorklogs, keyedEvents, query, userSettings).ToList();
                foreach (var day in days)
                {
                    day.BlockedEvents = blockedEventsByDay.GetValueOrDefault(day.Date) ?? new List<IEvent>();
                    day.Refresh();
                }

                return days;
            }

            private static IEnumerable<WorkingDay> CalculateDays(
                IEnumerable<IWorklog> issueWorklogs,
                IEnumerable<IEvent> events,
                Query query,
                UserSettingsDto userSettings)
            {
                var day = query.EndDate.Date;
                var splitedEvents = events.SplitByDays(
                    firstDate: query.StartDate,
                    lastDate: query.EndDate);

                while (day >= query.StartDate.Date)
                {
                    var dailyActualWorklogs = issueWorklogs
                        .Where(worklog => worklog.StartDate.Date == day)
                        .Select(worklog => WorkingDayWorklog.CreateActual(worklog));

                    var dailyEstimatedWorklogs = splitedEvents
                        .Where(userEvent => userEvent.StartDate.Date == day)
                        .Select(userEvent =>
                            WorkingDayWorklog.CreateEstimated(
                                userEvent: userEvent,
                                day: day,
                                dailyWorkingStartTime: userSettings.WorkingStartTime,
                                dailyWorkingEndTime: userSettings.WorkingEndTime));

                    var dailyWorklogs = dailyActualWorklogs.Union(dailyEstimatedWorklogs)
                            .OrderBy(record => record.StartDate)
                            .ThenBy(record => record.CompleteDate)
                            .ToList();

                    yield return new WorkingDay(
                        date: day,
                        settings: new WorkingDaySettings(
                            workingStartTime: userSettings.WorkingStartTime,
                            workingEndTime: userSettings.WorkingEndTime,
                            lunchTime: userSettings.LunchTime),
                        worklogs: dailyWorklogs);

                    day = day.AddDays(-1);
                }
            }
        }
    }
}
