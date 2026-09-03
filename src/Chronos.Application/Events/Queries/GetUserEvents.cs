using MediatR;
using Chronos.Application.Authentication;
using Chronos.Domain.Models.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Events.Queries
{
    /// <summary>
    /// Every event of the current user for the period, from all enabled sources.
    /// Replaces GetAssigneeJiraEvents / GetTesterJiraEvents / GetCommentJiraEvents and
    /// the inline calendar branch of GetWorklogCollection. See issue #299.
    /// </summary>
    public class GetUserEvents
    {
        public class Query : IRequest<IEnumerable<IEvent>>
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public class QueryHandler : IRequestHandler<Query, IEnumerable<IEvent>>
        {
            private readonly IEventDataSource _eventDataSource;
            private readonly IIdentityService _identityService;

            public QueryHandler(
                IEventDataSource eventDataSource,
                IIdentityService identityService)
            {
                _eventDataSource = eventDataSource;
                _identityService = identityService;
            }

            public async Task<IEnumerable<IEvent>> Handle(
                Query request,
                CancellationToken cancellationToken)
            {
                var user = await _identityService.GetCurrentUserAsync();
                return await _eventDataSource.GetEventsAsync(
                    new EventQuery(user?.Username, request.StartDate, request.EndDate),
                    cancellationToken);
            }
        }
    }
}
