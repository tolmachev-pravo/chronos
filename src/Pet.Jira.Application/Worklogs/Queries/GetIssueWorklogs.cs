using MediatR;
using Pet.Jira.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Worklogs.Queries
{
    public class GetIssueWorklogs
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
                => _worklogDataSource.GetIssueWorklogsAsync(request, cancellationToken);
        }
    }
}
