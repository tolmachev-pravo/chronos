using MediatR;
using Pet.Jira.Domain.Models.Worklogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pet.Jira.Application.Worklogs.Queries
{
    /// <summary>
    /// Estimated worklogs derived from the current user's comments on issues they watch
    /// but are not assigned to. Split out from the former GetRawIssueWorklogs so its
    /// performance can be measured independently. See issue #258.
    /// </summary>
    public class GetCommentJiraEvents
    {
        public class Query : IRequest<IEnumerable<IWorklog>>
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public TimeSpan CommentWorklogTime { get; set; }
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
                => _worklogDataSource.GetCommentRawIssueWorklogsAsync(request, cancellationToken);
        }
    }
}
