using MediatR;
using Chronos.Domain.Models.Issues;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Issues.Queries
{
    public class GetIssueStatuses
    {
        public class Query : IRequest<Model>
        {
        }

        public class Model
        {
            public IEnumerable<IssueStatus> IssueStatuses { get; set; }
        }

        public class QueryHandler(IIssueDataSource issueDataSource) : IRequestHandler<Query, Model>
        {
            private readonly IIssueDataSource _issueDataSource = issueDataSource;

			public async Task<Model> Handle(
                Query query,
                CancellationToken cancellationToken)
            {
                var issueStatuses = await _issueDataSource.GetIssueStatusesAsync(query, cancellationToken);
                return new Model { IssueStatuses = issueStatuses.OrderBy(record => record.Name) };
            }
        }
    }
}
