using MediatR;
using Chronos.Application.Events;
using Chronos.Application.Extensions.YandexCalendar.Queries;
using Chronos.Domain.Models.Events;
using Chronos.Domain.Models.Issues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Events
{
    /// <summary>
    /// Events from the user's Yandex calendar. An event carrying a Jira key becomes an
    /// estimated worklog; one without a key is still an event — it blocks day time.
    /// Moved out of GetWorklogCollection. See issue #299.
    ///
    /// This is the only provider that reaches the database during the parallel fetch
    /// phase: GetYandexCalendarEvents owns the extension lookup, the issue mapping and
    /// the time-zone conversion. It stays the only one, which is what keeps the scoped
    /// DbContext free of concurrent access. See issue #258.
    /// </summary>
    public class YandexCalendarEventProvider : IEventProvider
    {
        private readonly IMediator _mediator;

        private EventQuery _query;

        public YandexCalendarEventProvider(IMediator mediator)
        {
            _mediator = mediator;
        }

        public EventSource Source => EventSource.Calendar;

        /// <summary>
        /// A disconnected extension makes GetYandexCalendarEvents return nothing, so
        /// there is no separate enabled check to make here.
        /// </summary>
        public Task<bool> PrepareAsync(EventQuery query, CancellationToken cancellationToken = default)
        {
            _query = query;
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<IEvent>> GetEventsAsync(CancellationToken cancellationToken = default)
        {
            var events = new List<IEvent>();

            for (var date = _query.StartDate.Date; date <= _query.EndDate.Date; date = date.AddDays(1))
            {
                var calendarEvents = await _mediator.Send(
                    new GetYandexCalendarEvents.Query(_query.Username, DateOnly.FromDateTime(date)),
                    cancellationToken);

                events.AddRange(calendarEvents.Select(calendarEvent => new UserEvent
                {
                    StartDate = calendarEvent.Start,
                    CompleteDate = calendarEvent.End,
                    Issue = CreateIssue(calendarEvent.JiraIssueKeyHint, calendarEvent.Summary),
                    Author = _query.Username,
                    Source = EventSource.Calendar,
                    Summary = calendarEvent.Summary
                }));
            }

            return events;
        }

        private static IIssue CreateIssue(string jiraIssueKeyHint, string summary) =>
            string.IsNullOrEmpty(jiraIssueKeyHint)
                ? null
                : new Issue
                {
                    Key = jiraIssueKeyHint,
                    Identifier = jiraIssueKeyHint,
                    Summary = summary
                };
    }
}
