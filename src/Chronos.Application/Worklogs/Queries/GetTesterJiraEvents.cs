using MediatR;
using Chronos.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Worklogs.Queries
{
    /// <summary>
    /// Estimated worklogs derived from the "In Testing" status changes of issues where
    /// the current user is the tester. Split out from the former GetRawIssueWorklogs so
    /// its performance can be measured independently. See issue #258.
    /// </summary>
    public class GetTesterJiraEvents
    {
        public class Query : IRequest<IEnumerable<IWorklog>>
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public class QueryHandler : IRequestHandler<Query, IEnumerable<IWorklog>>
        {
            private readonly IWorklogDataSource _worklogDataSource;

            public QueryHandler(IWorklogDataSource worklogDataSource)
            {
                _worklogDataSource = worklogDataSource;
            }

            public Task<IEnumerable<IWorklog>> Handle(
                Query request,
                CancellationToken cancellationToken)
                => _worklogDataSource.GetTesterRawIssueWorklogsAsync(request, cancellationToken);
        }
    }
}
